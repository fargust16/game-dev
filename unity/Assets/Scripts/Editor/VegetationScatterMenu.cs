#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 vegetation, first slice (WORLD_GENERATION_PIPELINE.md Iteration 07): scatters
    /// biome-appropriate Low Poly Environment prefabs (see ASSET_LIBRARY_REFERENCE.md) using the
    /// same <see cref="MapMagicBiomeSampler"/> IBiomeSampler wired up for W0.3. No density map
    /// asset yet - straight WorldSeed-forked random sampling with a per-biome acceptance
    /// probability, which is enough to prove biome-driven placement before building the real
    /// thing.
    /// </summary>
    public static class VegetationScatterMenu
    {
        private const string PrefabRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string RootObjectName = "OverworldVegetation";

        // Samples per reference 1000x1000m tile; scaled by actual tile area.
        private const int ReferenceAttempts = 4000;

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
            var terrain = tile?.main?.terrain;
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
            var rng = new WorldSeed(OverworldTerrainMenu.LastSeed).Fork("vegetation");
            var waterZones = Object.FindFirstObjectByType<OverworldWaterZones>();

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
                if (!BiomeAcceptance.TryGetValue(biome, out var acceptance) || rng.NextDouble() > acceptance)
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
