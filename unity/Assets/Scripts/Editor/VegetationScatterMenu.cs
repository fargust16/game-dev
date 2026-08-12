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

        private const int TreeReferenceAttempts = 16000;
        private const int ShrubReferenceAttempts = 28000;
        private const int GrassReferenceAttempts = 42000;
        private const int RockReferenceAttempts = 3500;

        private const float ShoreBufferMeters = 1.5f;

        private const float RoadClearRadius = 3.5f;
        private const float RoadFalloff = 8f;
        private const float SettlementClearRadius = 28f;
        private const float SettlementFalloff = 22f;
        private const float PoiClearRadius = 10f;
        private const float PoiFalloff = 12f;
        private const float TownApproachClearRadius = 22f;
        private const float TownApproachFalloff = 18f;

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

            var treeCount = ScatterLayer(
                LayerKind.Tree, TreeByBiome, treesRoot.transform,
                Mathf.RoundToInt(TreeReferenceAttempts * areaScale),
                minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                density => density.Forest);

            var shrubCount = ScatterLayer(
                LayerKind.Shrub, ShrubByBiome, shrubsRoot.transform,
                Mathf.RoundToInt(ShrubReferenceAttempts * areaScale),
                minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                density => Mathf.Max(density.Shrub, density.Thicket * 0.85f),
                allowThicketClusters: true);

            var grassCount = ScatterLayer(
                LayerKind.Grass, GrassByBiome, grassRoot.transform,
                Mathf.RoundToInt(GrassReferenceAttempts * areaScale),
                minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, offsets, rng,
                density => density.Grass);

            var rockCount = ScatterRocks(
                rocksRoot.transform,
                Mathf.RoundToInt(RockReferenceAttempts * areaScale),
                minX, minZ, sizeX, sizeZ, sampler, terrain, waterZones, clearance, rng);

            Debug.Log(
                $"[Vegetation] Ecology scatter seed={OverworldTerrainMenu.LastSeed}: " +
                $"trees={treeCount}, shrubs={shrubCount}, grass={grassCount}, rocks={rockCount}");
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
                    var isTown = child.name.IndexOf("TownApproach", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    disks.Add(new VegetationClearance.Disk(
                        new Vector2(pos.x, pos.z),
                        isTown ? TownApproachClearRadius : PoiClearRadius,
                        isTown ? TownApproachFalloff : PoiFalloff));
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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
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
                // Density field drives coverage; local scale varies thick/medium/sparse look.
                var cover = Mathf.Lerp(0.7f, 1.45f, Mathf.Clamp01(density.Grass));
                var jitter = Mathf.Lerp(0.9f, 1.15f, (float)rng.NextDouble());
                instance.transform.localScale = Vector3.one * (cover * jitter);
            }
            else if (layer == LayerKind.Shrub)
            {
                var scale = Mathf.Lerp(0.9f, 1.35f, (float)rng.NextDouble());
                instance.transform.localScale = Vector3.one * scale;
            }

            instance.transform.position = new Vector3(x, groundY, z);
            GroundPrefabOnTerrain(instance, groundY);

            if (layer == LayerKind.Tree)
            {
                TreePlacement.EnsureTrunkCollider(instance);
            }

            return true;
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
