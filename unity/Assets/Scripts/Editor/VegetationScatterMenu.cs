#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using GameDev.World.Vegetation;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 vegetation (WORLD_GENERATION_PIPELINE.md Iteration 07): scatters
    /// biome-appropriate Low Poly Environment prefabs (see ASSET_LIBRARY_REFERENCE.md) using the
    /// same <see cref="MapMagicBiomeSampler"/> IBiomeSampler wired up for W0.3. Still no painted
    /// density-map asset, but acceptance is no longer a flat per-biome probability: it's scaled by
    /// <see cref="VegetationDensity"/>'s Perlin-blob patchiness (so growth reads as groves/clearings,
    /// not uniform static) and its slope falloff (so cliffs within a "lush" biome still thin out).
    /// </summary>
    public static class VegetationScatterMenu
    {
        private const string PrefabRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string RootObjectName = "OverworldVegetation";

        // Samples per reference 1000x1000m tile; scaled by actual tile area. Raised from the
        // original 4000 - VegetationDensity's patch/slope multipliers average roughly 0.4-0.5x on
        // top of the per-biome acceptance now, so attempts needed roughly doubling to keep the
        // densest patches reading as "full" instead of the whole map looking thinned out.
        private const int ReferenceAttempts = 8000;

        private struct Entry
        {
            public string PrefabPath;
            public float Weight;
        }

        // Acceptance probability per attempt (0..1) - qualitative "how lush", not a calibrated
        // ecological density.
        private static readonly Dictionary<BiomeId, float> BiomeAcceptance = new Dictionary<BiomeId, float>
        {
            { BiomeId.Meadow, 0.35f },
            { BiomeId.Forest, 0.45f },
            { BiomeId.Highland, 0.20f },
            { BiomeId.Mountain, 0.08f },
            { BiomeId.Swamp, 0.15f },
        };

        private static readonly Dictionary<BiomeId, Entry[]> BiomeVegetation = new Dictionary<BiomeId, Entry[]>
        {
            {
                BiomeId.Meadow, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Plants/PT_Grass_02.prefab", Weight = 0.55f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Flowers/PT_Poppy_02.prefab", Weight = 0.35f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_green.prefab", Weight = 0.10f },
                }
            },
            {
                BiomeId.Forest, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Pine_Tree_03_green.prefab", Weight = 0.55f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_green.prefab", Weight = 0.20f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_green.prefab", Weight = 0.25f },
                }
            },
            {
                BiomeId.Highland, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Generic_Rock_01.prefab", Weight = 0.40f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_green.prefab", Weight = 0.35f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Pine_Tree_03_green.prefab", Weight = 0.25f },
                }
            },
            {
                BiomeId.Mountain, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Generic_Rock_01.prefab", Weight = 0.5f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Ore_Rock_01.prefab", Weight = 0.3f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Menhir_Rock_02.prefab", Weight = 0.2f },
                }
            },
            {
                BiomeId.Swamp, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_dead.prefab", Weight = 0.35f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_dead.prefab", Weight = 0.35f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Mushrooms/PT_Caesars_Mushroom_01.prefab", Weight = 0.30f },
                }
            },
        };

        [MenuItem("GameDev/Overworld/Scatter Vegetation")]
        public static void ScatterVegetation()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                Debug.LogWarning("[Vegetation] No generated terrain - run GameDev/Overworld/Generate Terrain From Seed first.");
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                Debug.LogWarning("[Vegetation] Origin tile has no generated Terrain yet.");
                return;
            }

            if (!MapMagicTileSync.WaitForTileReady(tile))
            {
                Debug.LogWarning("[Vegetation] Tile did not finish generating within the timeout - scatter may use stale terrain data.");
            }

            var terrain = tile.main?.terrain;
            if (terrain == null)
            {
                Debug.LogWarning("[Vegetation] Origin tile has no generated Terrain yet.");
                return;
            }

            var holder = terrain.GetComponent<DirectMatricesHolder>();
            if (holder == null)
            {
                Debug.LogWarning("[Vegetation] No DirectMatricesHolder on the terrain - regenerate with OverworldTerrainMenu first.");
                return;
            }

            ClearVegetation();
            var root = new GameObject(RootObjectName);
            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var rng = worldSeed.Fork("vegetation");
            var waterZones = Object.FindFirstObjectByType<OverworldWaterZones>();

            // Own fork (not the main scatter rng) so the noise offset roll doesn't perturb the
            // attempt/accept/prefab-pick sequence - only the density field's position shifts per
            // seed, everything else about the scatter stays reproducible the same way it always was.
            var densityRng = worldSeed.Fork("vegetation-density");
            var noiseOffsetX = (float)densityRng.NextDouble() * 10000f;
            var noiseOffsetZ = (float)densityRng.NextDouble() * 10000f;

            var minX = tileCoord.x * mapMagic.tileSize.x;
            var minZ = tileCoord.z * mapMagic.tileSize.z;
            var sizeX = mapMagic.tileSize.x;
            var sizeZ = mapMagic.tileSize.z;
            var attempts = Mathf.RoundToInt(ReferenceAttempts * (sizeX * sizeZ) / (1000f * 1000f));

            var placed = 0;
            for (var i = 0; i < attempts; i++)
            {
                var x = minX + (float)rng.NextDouble() * sizeX;
                var z = minZ + (float)rng.NextDouble() * sizeZ;

                var biome = sampler.SampleBiome(x, z);
                if (!BiomeAcceptance.TryGetValue(biome, out var acceptance))
                {
                    continue;
                }

                var slope = sampler.SampleSlope01(x, z);
                var density = VegetationDensity.SamplePatch(x, z, noiseOffsetX, noiseOffsetZ);
                var slopeMultiplier = VegetationDensity.SlopeMultiplier(slope);
                var finalAcceptance = acceptance * density * slopeMultiplier;
                if (rng.NextDouble() > finalAcceptance)
                {
                    continue;
                }

                if (!TryFindSurface(x, z, out var y))
                {
                    continue;
                }

                if (waterZones != null && waterZones.TryGetWaterLevel(x, z, out var waterY) && y <= waterY)
                {
                    continue;
                }

                var prefab = PickPrefab(biome, rng);
                if (prefab == null)
                {
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(root.transform, worldPositionStays: false);
                instance.transform.position = new Vector3(x, y, z);
                instance.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                placed++;
            }

            Debug.Log($"[Vegetation] Placed {placed} instances from {attempts} attempts (seed {OverworldTerrainMenu.LastSeed}).");
        }

        [MenuItem("GameDev/Overworld/Clear Vegetation")]
        public static void ClearVegetation()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool TryFindSurface(float x, float z, out float y)
        {
            const float rayStartY = 400f;
            if (Physics.Raycast(new Vector3(x, rayStartY, z), Vector3.down, out var hit, rayStartY * 2f))
            {
                y = hit.point.y;
                return true;
            }

            y = 0f;
            return false;
        }

        private static GameObject PickPrefab(BiomeId biome, System.Random rng)
        {
            if (!BiomeVegetation.TryGetValue(biome, out var entries) || entries.Length == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            foreach (var entry in entries)
            {
                totalWeight += entry.Weight;
            }

            var roll = (float)rng.NextDouble() * totalWeight;
            var accum = 0f;
            foreach (var entry in entries)
            {
                accum += entry.Weight;
                if (roll <= accum)
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
                }
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(entries[entries.Length - 1].PrefabPath);
        }
    }
}
#endif
