#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Seed;
using GameDev.World.Settlement;
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
    /// Overworld flora scatter (WORLD_GENERATION_PIPELINE.md Iteration 07): layered placement driven
    /// by <see cref="VegetationEcology"/> density fields (forest regions → trees; correlated shrub /
    /// grass fields; soft clearance around roads/POIs/settlements). Prefabs remain Low Poly
    /// Environment (ASSET_LIBRARY_REFERENCE.md). MapMagic Objects pack is not installed — ecology
    /// and placement stay in C# on top of MapMagic height/slope/moisture + water samplers.
    /// </summary>
    public static class VegetationScatterMenu
    {
        private const string PrefabRoot = "Assets/Polytope Studio/Lowpoly_Environments/Prefabs";
        private const string RootObjectName = "OverworldVegetation";
        private const string DebugOverlayName = "OverworldVegetationDebug";

        // Lab density: enough coverage without ~175k PrefabUtility instantiates per tile.
        private const int TreeReferenceAttempts = 6000;
        private const int ShrubReferenceAttempts = 9000;
        private const int GrassReferenceAttempts = 12000;
        private const int RockReferenceAttempts = 1800;

        private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>(64);

        private const float ShoreBufferMeters = 1.5f;

        private const float RoadClearRadius = 3.5f;
        private const float RoadFalloff = 8f;
        private const float SettlementClearRadius = 28f;
        private const float SettlementFalloff = 22f;
        // Hard empty pocket around POI props/buildings so forest/grass does not sit on the marker;
        // short falloff lets flora resume just outside that pocket.
        private const float PoiClearRadius = 14f;
        private const float PoiFalloff = 8f;
        private const float CampClearRadius = 18f;
        private const float CampFalloff = 10f;
        private const float TownApproachClearRadius = 24f;
        private const float TownApproachFalloff = 14f;

        private struct Entry
        {
            public string PrefabPath;
            public float Weight;
        }

        private enum LayerKind
        {
            Tree,
            Shrub,
            Grass,
            Rock,
        }

        private static readonly Dictionary<BiomeId, Entry[]> TreeByBiome = new Dictionary<BiomeId, Entry[]>
        {
            {
                BiomeId.Forest, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Pine_Tree_03_green.prefab", Weight = 0.7f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_green.prefab", Weight = 0.3f },
                }
            },
            {
                BiomeId.Meadow, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_green.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Highland, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Pine_Tree_03_green.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Swamp, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Trees/PT_Fruit_Tree_01_dead.prefab", Weight = 1f },
                }
            },
        };

        private static readonly Dictionary<BiomeId, Entry[]> ShrubByBiome = new Dictionary<BiomeId, Entry[]>
        {
            {
                BiomeId.Forest, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_green.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Meadow, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_green.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Highland, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_green.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Swamp, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Shrubs/PT_Generic_Shrub_01_dead.prefab", Weight = 0.7f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Mushrooms/PT_Caesars_Mushroom_01.prefab", Weight = 0.3f },
                }
            },
        };

        private static readonly Dictionary<BiomeId, Entry[]> GrassByBiome = new Dictionary<BiomeId, Entry[]>
        {
            {
                BiomeId.Meadow, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Plants/PT_Grass_02.prefab", Weight = 0.7f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Flowers/PT_Poppy_02.prefab", Weight = 0.3f },
                }
            },
            {
                BiomeId.Forest, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Plants/PT_Grass_02.prefab", Weight = 1f },
                }
            },
            {
                BiomeId.Swamp, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Plants/PT_Grass_02.prefab", Weight = 0.6f },
                    new Entry { PrefabPath = $"{PrefabRoot}/Mushrooms/PT_Caesars_Mushroom_01.prefab", Weight = 0.4f },
                }
            },
            {
                BiomeId.Highland, new[]
                {
                    new Entry { PrefabPath = $"{PrefabRoot}/Plants/PT_Grass_02.prefab", Weight = 1f },
                }
            },
        };

        private static readonly Entry[] MountainRocks =
        {
            new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Generic_Rock_01.prefab", Weight = 0.5f },
            new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Ore_Rock_01.prefab", Weight = 0.3f },
            new Entry { PrefabPath = $"{PrefabRoot}/Rocks/PT_Menhir_Rock_02.prefab", Weight = 0.2f },
        };

        [MenuItem("GameDev/Overworld/Scatter Vegetation")]
        public static void ScatterVegetation()
        {
            var total = OverworldGenTiming.Start();
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
            ClearDebugOverlay();
            var root = new GameObject(RootObjectName);
            var treesRoot = new GameObject("Trees");
            var shrubsRoot = new GameObject("Shrubs");
            var grassRoot = new GameObject("Grass");
            var rocksRoot = new GameObject("Rocks");
            treesRoot.transform.SetParent(root.transform, false);
            shrubsRoot.transform.SetParent(root.transform, false);
            grassRoot.transform.SetParent(root.transform, false);
            rocksRoot.transform.SetParent(root.transform, false);

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var rng = worldSeed.Fork("vegetation");
            var densityRng = worldSeed.Fork("vegetation-density");
            var offsets = VegetationEcology.NoiseOffsets.FromRng(densityRng);

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);
            var clearance = BuildClearanceFromScene();

            var minX = tileCoord.x * mapMagic.tileSize.x;
            var minZ = tileCoord.z * mapMagic.tileSize.z;
            var sizeX = mapMagic.tileSize.x;
            var sizeZ = mapMagic.tileSize.z;
            var areaScale = (sizeX * sizeZ) / (1000f * 1000f);

            var treeAttempts = Mathf.RoundToInt(TreeReferenceAttempts * areaScale);
            var shrubAttempts = Mathf.RoundToInt(ShrubReferenceAttempts * areaScale);
            var grassAttempts = Mathf.RoundToInt(GrassReferenceAttempts * areaScale);
            var rockAttempts = Mathf.RoundToInt(RockReferenceAttempts * areaScale);

            var treeCount = 0;
            var shrubCount = 0;
            var grassCount = 0;
            var rockCount = 0;
            OverworldGenTiming.Time("vegetation.trees", () =>
            {
                treeCount = ScatterLayer(
                    LayerKind.Tree, TreeByBiome, treesRoot.transform, treeAttempts,
                    minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                    density => density.Forest);
            }, $"attempts={treeAttempts}");

            OverworldGenTiming.Time("vegetation.shrubs", () =>
            {
                shrubCount = ScatterLayer(
                    LayerKind.Shrub, ShrubByBiome, shrubsRoot.transform, shrubAttempts,
                    minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                    density => Mathf.Max(density.Shrub, density.Thicket * 0.85f),
                    allowThicketClusters: true);
            }, $"attempts={shrubAttempts}");

            OverworldGenTiming.Time("vegetation.grass", () =>
            {
                grassCount = ScatterLayer(
                    LayerKind.Grass, GrassByBiome, grassRoot.transform, grassAttempts,
                    minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                    density => density.Grass);
            }, $"attempts={grassAttempts}");

            OverworldGenTiming.Time("vegetation.rocks", () =>
            {
                rockCount = ScatterRocks(
                    rocksRoot.transform, rockAttempts,
                    minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, rng);
            }, $"attempts={rockAttempts}");

            Debug.Log(
                $"[Vegetation] Ecology scatter seed={OverworldTerrainMenu.LastSeed}: " +
                $"trees={treeCount}, shrubs={shrubCount}, grass={grassCount}, rocks={rockCount}");
            LogTrunkColliderSamples(treesRoot.transform);
            OverworldGenTiming.Log(
                "vegetation.total",
                OverworldGenTiming.StopMs(total),
                $"spawned trees={treeCount} shrubs={shrubCount} grass={grassCount} rocks={rockCount}");
        }

        private static void LogTrunkColliderSamples(Transform treesRoot)
        {
            if (treesRoot == null)
            {
                return;
            }

            var pineN = 0;
            var fruitN = 0;
            var pineR = 0f;
            var fruitR = 0f;
            var pineCaps = 0;
            var fruitCaps = 0;
            for (var i = 0; i < treesRoot.childCount; i++)
            {
                var t = treesRoot.GetChild(i);
                var name = t.name;
                var caps = t.GetComponents<CapsuleCollider>();
                if (caps == null || caps.Length == 0)
                {
                    continue;
                }

                var r = caps[0].radius * t.lossyScale.x;
                if (name.Contains("Pine") || name.Contains("pine"))
                {
                    pineN++;
                    pineR += r;
                    pineCaps += caps.Length;
                }
                else if (name.Contains("Fruit") || name.Contains("fruit"))
                {
                    fruitN++;
                    fruitR += r;
                    fruitCaps += caps.Length;
                }
            }

            Debug.Log(
                $"[Vegetation] trunk colliders: " +
                $"pine n={pineN} avgR={(pineN > 0 ? pineR / pineN : 0f):F3}m caps/tree={(pineN > 0 ? pineCaps / (float)pineN : 0f):F1} | " +
                $"fruit n={fruitN} avgR={(fruitN > 0 ? fruitR / fruitN : 0f):F3}m caps/tree={(fruitN > 0 ? fruitCaps / (float)fruitN : 0f):F1} " +
                $"(fitted from mid-trunk mesh verts; MeshCollider on full crown is intentionally avoided)");
        }

        [MenuItem("GameDev/Overworld/Clear Vegetation")]
        public static void ClearVegetation()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            ClearDebugOverlay();
        }

        [MenuItem("GameDev/Overworld/Debug/Vegetation Density Overlay")]
        public static void BuildDensityDebugOverlay()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            var tileCoord = new Coord(0, 0);
            var tile = mapMagic.tiles[tileCoord];
            var terrain = tile?.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return;
            }

            ClearDebugOverlay();

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var offsets = VegetationEcology.NoiseOffsets.FromRng(worldSeed.Fork("vegetation-density"));
            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);

            var minX = tileCoord.x * mapMagic.tileSize.x;
            var minZ = tileCoord.z * mapMagic.tileSize.z;
            var sizeX = mapMagic.tileSize.x;
            var sizeZ = mapMagic.tileSize.z;

            const int res = 256;
            var tex = new Texture2D(res, res, TextureFormat.RGB24, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var wx = minX + (x + 0.5f) / res * sizeX;
                    var wz = minZ + (z + 0.5f) / res * sizeZ;
                    var biome = sampler.SampleBiome(wx, wz);
                    var slope = sampler.SampleSlope01(wx, wz);
                    var moisture = sampler.SampleMoisture01(wx, wz);
                    var wetness = WaterProximity.SampleWetness01(
                        waterZones, wx, wz, VegetationEcology.WetBoostMeters);
                    var d = VegetationEcology.Evaluate(wx, wz, biome, slope, moisture, wetness, offsets);
                    tex.SetPixel(x, z, new Color(d.Forest, d.Shrub, d.Grass, 1f));
                }
            }

            tex.Apply(false, false);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = DebugOverlayName;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position = new Vector3(minX + sizeX * 0.5f, mapMagic.globals.height + 40f, minZ + sizeZ * 0.5f);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(sizeX, sizeZ, 1f);

            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Texture")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", Color.white);
            }

            mat.mainTexture = tex;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            Debug.Log("[Vegetation] Density overlay RGB = Forest / Shrub / Grass (0..1).");
        }

        private static int ScatterLayer(
            LayerKind layer,
            Dictionary<BiomeId, Entry[]> catalog,
            Transform parent,
            int attempts,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            MapMagicBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            IWaterSampler waterZones,
            VegetationClearance clearance,
            VegetationEcology.NoiseOffsets offsets,
            System.Random rng,
            System.Func<VegetationEcology.Sample, float> densitySelector,
            bool allowThicketClusters = false)
        {
            var placed = 0;
            for (var i = 0; i < attempts; i++)
            {
                var x = minX + (float)rng.NextDouble() * sizeX;
                var z = minZ + (float)rng.NextDouble() * sizeZ;

                if (!TryEvaluatePoint(
                        x, z, sampler, terrain, waterZones, clearance, offsets,
                        out var biome, out var density, out var groundY, out var clearMul))
                {
                    continue;
                }

                var localDensity = densitySelector(density) * clearMul;
                if (localDensity <= 0.01f || rng.NextDouble() > localDensity)
                {
                    continue;
                }

                if (!catalog.TryGetValue(biome, out var entries) || !TryPickEntry(entries, rng, out var entry))
                {
                    continue;
                }

                if (!SpawnPrefab(entry.PrefabPath, parent, x, groundY, z, layer, density, rng))
                {
                    continue;
                }

                placed++;

                if (allowThicketClusters && density.Thicket > 0.55f && rng.NextDouble() < density.Thicket)
                {
                    placed += SpawnThicketExtras(
                        entries, parent, x, z, terrain, waterZones, clearance, layer, density, rng);
                }
            }

            return placed;
        }

        private static int ScatterRocks(
            Transform parent,
            int attempts,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            MapMagicBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            IWaterSampler waterZones,
            VegetationClearance clearance,
            System.Random rng)
        {
            var placed = 0;
            for (var i = 0; i < attempts; i++)
            {
                var x = minX + (float)rng.NextDouble() * sizeX;
                var z = minZ + (float)rng.NextDouble() * sizeZ;
                var biome = sampler.SampleBiome(x, z);
                if (biome != BiomeId.Mountain && biome != BiomeId.Highland)
                {
                    continue;
                }

                var slope = sampler.SampleSlope01(x, z);
                // Rocks prefer steeper / thinner plant cover.
                var chance = biome == BiomeId.Mountain ? 0.35f : 0.12f;
                chance *= Mathf.Lerp(0.4f, 1.2f, Mathf.Clamp01(slope / 0.6f));
                chance *= clearance.Multiplier(x, z);
                if (rng.NextDouble() > chance)
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

                if (!TryPickEntry(MountainRocks, rng, out var entry))
                {
                    continue;
                }

                if (SpawnPrefab(entry.PrefabPath, parent, x, groundY, z, LayerKind.Rock, default, rng))
                {
                    placed++;
                }
            }

            return placed;
        }

        private static int SpawnThicketExtras(
            Entry[] entries,
            Transform parent,
            float centerX,
            float centerZ,
            UnityEngine.Terrain terrain,
            IWaterSampler waterZones,
            VegetationClearance clearance,
            LayerKind layer,
            VegetationEcology.Sample density,
            System.Random rng)
        {
            var extra = 2 + rng.Next(0, 3);
            var placed = 0;
            for (var i = 0; i < extra; i++)
            {
                var angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                var radius = 2.5f + (float)rng.NextDouble() * 6f;
                var x = centerX + Mathf.Cos(angle) * radius;
                var z = centerZ + Mathf.Sin(angle) * radius;
                if (clearance.Multiplier(x, z) < 0.35f)
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

                if (!TryPickEntry(entries, rng, out var entry))
                {
                    continue;
                }

                if (SpawnPrefab(entry.PrefabPath, parent, x, groundY, z, layer, density, rng))
                {
                    placed++;
                }
            }

            return placed;
        }

        private static bool TryEvaluatePoint(
            float x,
            float z,
            MapMagicBiomeSampler sampler,
            UnityEngine.Terrain terrain,
            IWaterSampler waterZones,
            VegetationClearance clearance,
            VegetationEcology.NoiseOffsets offsets,
            out BiomeId biome,
            out VegetationEcology.Sample density,
            out float groundY,
            out float clearMul)
        {
            biome = sampler.SampleBiome(x, z);
            density = default;
            groundY = 0f;
            clearMul = clearance.Multiplier(x, z);
            if (clearMul <= 0.01f)
            {
                return false;
            }

            if (!TrySampleTerrainHeight(terrain, x, z, out groundY))
            {
                return false;
            }

            if (WaterProximity.IsUnderOrTooCloseToWater(waterZones, x, z, groundY, ShoreBufferMeters))
            {
                return false;
            }

            var slope = sampler.SampleSlope01(x, z);
            var moisture = sampler.SampleMoisture01(x, z);
            var wetness = WaterProximity.SampleWetness01(waterZones, x, z, VegetationEcology.WetBoostMeters);
            density = VegetationEcology.Evaluate(x, z, biome, slope, moisture, wetness, offsets);
            return true;
        }

        private static VegetationClearance BuildClearanceFromScene()
        {
            var disks = new List<VegetationClearance.Disk>();

            var settlement = Object.FindFirstObjectByType<OverworldSettlementAnchor>();
            if (settlement != null)
            {
                var p = settlement.Position;
                disks.Add(new VegetationClearance.Disk(
                    new Vector2(p.x, p.z), SettlementClearRadius, SettlementFalloff));
            }

            var poiRoot = GameObject.Find("OverworldPoi");
            if (poiRoot != null)
            {
                foreach (Transform child in poiRoot.transform)
                {
                    var pos = child.position;
                    var name = child.name;
                    float clearR;
                    float falloff;
                    if (name.IndexOf("TownApproach", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        clearR = TownApproachClearRadius;
                        falloff = TownApproachFalloff;
                    }
                    else if (name.IndexOf("Camp", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        clearR = CampClearRadius;
                        falloff = CampFalloff;
                    }
                    else
                    {
                        clearR = PoiClearRadius;
                        falloff = PoiFalloff;
                    }

                    disks.Add(new VegetationClearance.Disk(new Vector2(pos.x, pos.z), clearR, falloff));
                }
            }

            var roadSamples = new List<Vector2>(512);
            var roads = GameObject.Find("OverworldRoads");
            if (roads != null)
            {
                foreach (var filter in roads.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null)
                    {
                        continue;
                    }

                    var verts = mesh.vertices;
                    var step = Mathf.Max(1, verts.Length / 40);
                    for (var i = 0; i < verts.Length; i += step)
                    {
                        var w = filter.transform.TransformPoint(verts[i]);
                        roadSamples.Add(new Vector2(w.x, w.z));
                    }
                }
            }

            return new VegetationClearance(disks, roadSamples, RoadClearRadius, RoadFalloff);
        }

        private static bool SpawnPrefab(
            string path,
            Transform parent,
            float x,
            float groundY,
            float z,
            LayerKind layer,
            VegetationEcology.Sample density,
            System.Random rng)
        {
            if (!PrefabCache.TryGetValue(path, out var prefab) || prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                PrefabCache[path] = prefab;
            }

            if (prefab == null)
            {
                return false;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            if (layer == LayerKind.Tree)
            {
                var young = density.Edge > 0.55f || density.Forest < 0.35f;
                var targetH = TreePlacement.PickTargetHeightMeters(young, rng);
                TreePlacement.ApplyWorldHeight(instance, targetH);
            }
            else if (layer == LayerKind.Grass)
            {
                var isFlower = path.Contains("/Flowers/");
                float minH;
                float maxH;
                if (isFlower)
                {
                    minH = GameDev.World.WorldScale.FlowerMinMeters;
                    maxH = GameDev.World.WorldScale.FlowerMaxMeters;
                }
                else
                {
                    // Meadow grass reads taller; forest understory shorter.
                    minH = GameDev.World.WorldScale.GrassShortMinMeters;
                    maxH = Mathf.Lerp(
                        GameDev.World.WorldScale.GrassShortMaxMeters,
                        GameDev.World.WorldScale.GrassTallMaxMeters,
                        Mathf.Clamp01(density.Grass));
                }

                var targetH = Mathf.Lerp(minH, maxH, (float)rng.NextDouble());
                ApplyTargetHeight(instance, targetH);
            }
            else if (layer == LayerKind.Shrub)
            {
                var targetH = Mathf.Lerp(
                    GameDev.World.WorldScale.ShrubMinMeters,
                    GameDev.World.WorldScale.ShrubMaxMeters,
                    (float)rng.NextDouble());
                ApplyTargetHeight(instance, targetH);
            }

            instance.transform.position = new Vector3(x, groundY, z);
            GroundPrefabOnTerrain(instance, groundY);

            if (layer == LayerKind.Tree)
            {
                TreePlacement.EnsureTrunkCollider(instance);
            }

            return true;
        }

        private static void ApplyTargetHeight(GameObject instance, float targetHeightMeters)
        {
            if (instance == null || targetHeightMeters <= 0.01f)
            {
                return;
            }

            instance.transform.localScale = Vector3.one;
            var height = TreePlacement.MeasureRendererHeight(instance);
            if (height < 0.02f)
            {
                instance.transform.localScale = Vector3.one * targetHeightMeters;
                return;
            }

            instance.transform.localScale = Vector3.one * (targetHeightMeters / height);
        }

        private static void ClearDebugOverlay()
        {
            var overlay = GameObject.Find(DebugOverlayName);
            if (overlay != null)
            {
                Object.DestroyImmediate(overlay);
            }
        }

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

        private static bool TryPickEntry(Entry[] entries, System.Random rng, out Entry entry)
        {
            entry = default;
            if (entries == null || entries.Length == 0)
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
