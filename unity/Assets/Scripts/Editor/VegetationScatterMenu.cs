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
    /// Trees get a capsule collider; shrubs/grass stay pass-through.
    /// </summary>
    public static class VegetationScatterMenu
    {
        private const string PrefabRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string RootObjectName = "OverworldVegetation";

        // Samples per reference 1000x1000m tile; scaled by actual tile area.
        private const int ReferenceAttempts = 14000;

        // Keep trunks off the wet mask edge when grid sampling is coarse relative to prefab size.
        private const float ShoreBufferMeters = 1.5f;

        private const float TreeScaleMin = 1.25f;
        private const float TreeScaleMax = 1.45f;

        private struct Entry
        {
            public string PrefabPath;
            public float Weight;
        }

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
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                return;
            }

            MapMagicTileSync.WaitForTileReady(tile);

            var terrain = tile.main?.terrain;
            if (terrain == null)
            {
                return;
            }

            var holder = terrain.GetComponent<DirectMatricesHolder>();
            if (holder == null)
            {
                return;
            }

            ClearVegetation();
            var root = new GameObject(RootObjectName);
            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var rng = worldSeed.Fork("vegetation");
            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);

            var densityRng = worldSeed.Fork("vegetation-density");
            var noiseOffsetX = (float)densityRng.NextDouble() * 10000f;
            var noiseOffsetZ = (float)densityRng.NextDouble() * 10000f;

            var minX = tileCoord.x * mapMagic.tileSize.x;
            var minZ = tileCoord.z * mapMagic.tileSize.z;
            var sizeX = mapMagic.tileSize.x;
            var sizeZ = mapMagic.tileSize.z;
            var attempts = Mathf.RoundToInt(ReferenceAttempts * (sizeX * sizeZ) / (1000f * 1000f));

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

                if (!TrySampleTerrainHeight(terrain, x, z, out var groundY))
                {
                    continue;
                }

                if (WaterProximity.IsUnderOrTooCloseToWater(waterZones, x, z, groundY, ShoreBufferMeters))
                {
                    continue;
                }

                if (!TryPickEntry(biome, rng, out var entry))
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.PrefabPath);
                if (prefab == null)
                {
                    continue;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(root.transform, worldPositionStays: false);
                instance.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

                var isTree = entry.PrefabPath.Contains("/Trees/");
                var scale = 1f;
                if (isTree)
                {
                    scale = Mathf.Lerp(TreeScaleMin, TreeScaleMax, (float)rng.NextDouble());
                    instance.transform.localScale = Vector3.one * scale;
                }

                // Pivot is often at the mesh centre on Polytope trees — plant the mesh bottom on terrain.
                instance.transform.position = new Vector3(x, groundY, z);
                GroundPrefabOnTerrain(instance, groundY);

                if (isTree)
                {
                    EnsureTreeCollider(instance);
                }
            }
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

        private static void EnsureTreeCollider(GameObject instance)
        {
            if (instance.GetComponentInChildren<Collider>() != null)
            {
                return;
            }

            var renderer = instance.GetComponentInChildren<Renderer>();
            var bounds = renderer != null
                ? renderer.bounds
                : new Bounds(instance.transform.position, new Vector3(1f, 4f, 1f));

            var capsule = instance.AddComponent<CapsuleCollider>();
            var localCenter = instance.transform.InverseTransformPoint(bounds.center);
            var height = Mathf.Max(1f, bounds.size.y / Mathf.Max(instance.transform.lossyScale.y, 0.01f));
            var radius = Mathf.Max(0.2f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.2f /
                                         Mathf.Max(instance.transform.lossyScale.x, 0.01f));
            capsule.height = height;
            capsule.radius = radius;
            capsule.center = new Vector3(localCenter.x, height * 0.5f, localCenter.z);
            capsule.direction = 1; // Y-axis
        }

        /// <summary>
        /// Terrain height only — never Physics.Raycast. Raycasts hit previously-placed tree
        /// capsules and stack the next tree on top (classic floating-forest bug).
        /// </summary>
        private static bool TrySampleTerrainHeight(UnityEngine.Terrain terrain, float x, float z, out float y)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                y = 0f;
                return false;
            }

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            if (x < pos.x || z < pos.z || x > pos.x + size.x || z > pos.z + size.z)
            {
                y = 0f;
                return false;
            }

            y = terrain.SampleHeight(new Vector3(x, 0f, z)) + pos.y;
            return true;
        }

        /// <summary>
        /// Moves the instance so the lowest renderer bound sits on <paramref name="groundY"/>
        /// (Polytope tree pivots are often at the canopy centre, not the trunk base).
        /// </summary>
        private static void GroundPrefabOnTerrain(GameObject instance, float groundY)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var minY = float.PositiveInfinity;
            foreach (var r in renderers)
            {
                if (r.bounds.min.y < minY)
                {
                    minY = r.bounds.min.y;
                }
            }

            if (float.IsInfinity(minY))
            {
                return;
            }

            var delta = groundY - minY;
            if (Mathf.Abs(delta) > 0.001f)
            {
                instance.transform.position += new Vector3(0f, delta, 0f);
            }
        }

        private static bool TryPickEntry(BiomeId biome, System.Random rng, out Entry entry)
        {
            entry = default;
            if (!BiomeVegetation.TryGetValue(biome, out var entries) || entries.Length == 0)
            {
                return false;
            }

            var totalWeight = 0f;
            foreach (var e in entries)
            {
                totalWeight += e.Weight;
            }

            var roll = (float)rng.NextDouble() * totalWeight;
            var accum = 0f;
            foreach (var e in entries)
            {
                accum += e.Weight;
                if (roll <= accum)
                {
                    entry = e;
                    return true;
                }
            }

            entry = entries[entries.Length - 1];
            return true;
        }
    }
}
#endif
