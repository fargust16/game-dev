#if UNITY_EDITOR
using Den.Tools;
using GameDev.World.Streaming;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Builds overworld lakes as <b>filled 2D areas</b> and rivers as separate line corridors.
    /// Nearby floods are merged into one lake / one plane / one pit. Basin dig is an absolute
    /// smooth bowl at half mountain height (does not preserve MapMagic terraces inside the mask).
    /// </summary>
    public static class WaterZoneMenu
    {
        private const string RootObjectName = "OverworldWater";
        private const string RiverRootObjectName = "OverworldRivers";
        private const float CellSize = 3f;
        private const int MinCellsPerZone = 28;
        private const int MaxZones = 20;
        /// <summary>Deep enough to fill MapMagic pits; cell cap still blocks plains mega-lakes.</summary>
        private const float MaxLakeDepthMeters = 14f;
        /// <summary>~2400 cells @ 3m ≈ 93m equivalent radius — one visual lake, not a tile flood.</summary>
        private const int MaxCellsPerZone = 2400;
        private const int CellCountSafetyCap = 2400;
        private const float LakeMergeGapMeters = 6f;
        private const string BankLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";

        [MenuItem("GameDev/Overworld/Generate Water Zones")]
        public static void GenerateWaterZonesMenuItem()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            GenerateForTile(mapMagic, new Coord(0, 0));
        }

        public static void GenerateForTile(MapMagicObject mapMagic, Coord tileCoord)
        {
            ClearWaterZones();
            ClearRiverZones();

            var tile = mapMagic.tiles[tileCoord];
            if (tile == null)
            {
                return;
            }

            MapMagicTileSync.WaitForTileReady(tile);

            var terrain = tile.main?.terrain;
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return;
            }

            // Authoritative world Y from the live Terrain — DirectMatrices*globals.height drifts
            // from SampleHeight when the tile pivot is elevated or size.y ≠ globals.height
            // (that mismatch buried rivers ~370m under the mesh).
            float HeightSampler(float x, float z)
            {
                if (terrain != null)
                {
                    return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
                }

                var worldHeight = mapMagic.globals.height;
                return holder.ContainsPosition(x, z)
                    ? holder.ValueAtPositionInterpolated("height", x, z) * worldHeight
                    : float.PositiveInfinity;
            }

            var inset = OverworldTileChunks.DefaultMarginMeters;
            var minX = (float)(tileCoord.x * mapMagic.tileSize.x) + inset;
            var minZ = (float)(tileCoord.z * mapMagic.tileSize.z) + inset;
            var sizeX = (float)mapMagic.tileSize.x - 2f * inset;
            var sizeZ = (float)mapMagic.tileSize.z - 2f * inset;

            // Guaranteed scattered lakes (jittered 3×3 strata). Finder ponds are optional extras
            // that must stay away from forced sites so we never glue the map into one mega-lake.
            var lakeRng = new GameDev.World.Seed.WorldSeed(OverworldTerrainMenu.LastSeed).Fork("water.lakes");
            var forced = DistributedLakePlacer.Place(
                HeightSampler, minX, minZ, sizeX, sizeZ, CellSize, lakeRng,
                minLakes: DistributedLakePlacer.DefaultMinLakes,
                targetLakes: DistributedLakePlacer.DefaultTargetLakes);
            var natural = LakeBasinFinder.FindZones(
                HeightSampler, minX, minZ, sizeX, sizeZ, CellSize,
                MaxLakeDepthMeters, CellCountSafetyCap, MinCellsPerZone, MaxZones);
            var ponds = FilterDistantZones(natural, forced, minSeparationMeters: 180f);
            var zones = ConcatZones(forced, ponds);
            // Only collapse true nested duplicates — never MergeNearby across the tile.
            zones = LakeZoneMerger.CullOverlapping(zones, maxOverlapFraction: 0.25f, MaxCellsPerZone);
            if (zones.Length < DistributedLakePlacer.DefaultMinLakes)
            {
                zones = LakeZoneMerger.EnsureMinDistinct(
                    zones, forced, DistributedLakePlacer.DefaultMinLakes, minCenterSeparationMeters: 160f);
            }

            var mountainLocalHeight = MeasureMountainLocalHeight(terrain, HeightSampler, minX, minZ, sizeX, sizeZ);
            var halfMountain = Mathf.Max(LakeTerrainCarver.MinShoreDepthMeters, mountainLocalHeight * 0.5f);

            var terrainBaseY = terrain != null ? terrain.transform.position.y : 0f;
            var heightScale = terrain != null
                ? Mathf.Max(terrain.terrainData.size.y, mountainLocalHeight)
                : Mathf.Max(mapMagic.globals.height, mountainLocalHeight);
            var riverSettings = RiverTracerSettings.Default(terrainBaseY, heightScale);
            var flowGrid = FlowNetwork.Compute(HeightSampler, minX, minZ, sizeX, sizeZ, CellSize);
            var rivers = RiverTracer.Trace(flowGrid, zones, riverSettings);

            if (terrain != null && zones.Length > 0)
            {
                var lakeBankLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BankLayerPath);
                LakeTerrainCarver.Carve(terrain, zones, halfMountain, lakeBankLayer);
            }

            System.Func<float, float, float> sampleTerrainY = null;
            if (terrain != null)
            {
                sampleTerrainY = (x, z) =>
                    terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
            }

            if (terrain != null && rivers.Length > 0)
            {
                var bankLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(BankLayerPath);
                RiverTerrainCarver.Carve(terrain, rivers, bankLayer);

                for (var i = 0; i < rivers.Length; i++)
                {
                    rivers[i].SyncWaterSurfaceAfterCarve(sampleTerrainY, riverSettings.WaterFillFraction, zones);
                }
            }

            var root = new GameObject(RootObjectName);
            var holderComponent = root.AddComponent<OverworldWaterZones>();
            holderComponent.SetZones(zones);

            var waterMaterial = CreateWaterMaterial();
            var planesCreated = 0;
            Rect? terrainClip = null;
            if (terrain != null)
            {
                var tp = terrain.transform.position;
                var ts = terrain.terrainData.size;
                terrainClip = new Rect(tp.x, tp.z, ts.x, ts.z);
            }

            foreach (var zone in zones)
            {
                var zoneObject = new GameObject($"WaterZone_{zone.Id}");
                zoneObject.transform.SetParent(root.transform, worldPositionStays: true);
                var meshFilter = zoneObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = WaterMeshBuilder.Build(zone, zones, sampleTerrainY, terrainClip);
                var renderer = zoneObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = waterMaterial;
                planesCreated++;
            }

            var riverRoot = new GameObject(RiverRootObjectName);
            var riverHolder = riverRoot.AddComponent<OverworldRiverZones>();
            riverHolder.SetRivers(rivers);

            foreach (var river in rivers)
            {
                var riverObject = new GameObject($"River_{river.Id}");
                riverObject.transform.SetParent(riverRoot.transform, worldPositionStays: true);
                var meshFilter = riverObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = RiverMeshBuilder.Build(river, uvTileLength: 8f);
                var renderer = riverObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = waterMaterial;
            }

            LogOverworldSummary(
                terrain, zones, rivers,
                mapMagic.globals.height, mountainLocalHeight, halfMountain, planesCreated, forced.Length);
        }

        private static WaterZone[] FilterDistantZones(
            WaterZone[] candidates,
            WaterZone[] anchors,
            float minSeparationMeters)
        {
            if (candidates == null || candidates.Length == 0)
            {
                return System.Array.Empty<WaterZone>();
            }

            if (anchors == null || anchors.Length == 0)
            {
                return candidates;
            }

            var kept = new System.Collections.Generic.List<WaterZone>();
            var minSepSq = minSeparationMeters * minSeparationMeters;
            foreach (var c in candidates)
            {
                var cc = c.GetFloodCentroidXZ();
                var far = true;
                foreach (var a in anchors)
                {
                    var ac = a.GetFloodCentroidXZ();
                    var dx = cc.x - ac.x;
                    var dz = cc.z - ac.z;
                    if (dx * dx + dz * dz < minSepSq)
                    {
                        far = false;
                        break;
                    }
                }

                if (far)
                {
                    kept.Add(c);
                }
            }

            return kept.ToArray();
        }

        private static WaterZone[] ConcatZones(WaterZone[] a, WaterZone[] b)
        {
            var ac = a?.Length ?? 0;
            var bc = b?.Length ?? 0;
            var result = new WaterZone[ac + bc];
            if (ac > 0)
            {
                System.Array.Copy(a, 0, result, 0, ac);
            }

            if (bc > 0)
            {
                System.Array.Copy(b, 0, result, ac, bc);
            }

            return result;
        }

        private static float MeasureMountainLocalHeight(
            UnityEngine.Terrain terrain,
            System.Func<float, float, float> heightSampler,
            float minX, float minZ, float sizeX, float sizeZ)
        {
            var peakWorld = 0f;
            for (var i = 0; i < 48; i++)
            {
                for (var j = 0; j < 48; j++)
                {
                    var x = minX + sizeX * (i + 0.5f) / 48f;
                    var z = minZ + sizeZ * (j + 0.5f) / 48f;
                    var y = heightSampler(x, z);
                    if (!float.IsInfinity(y) && y > peakWorld)
                    {
                        peakWorld = y;
                    }
                }
            }

            var baseY = terrain != null ? terrain.transform.position.y : 0f;
            return Mathf.Max(peakWorld - baseY, 1f);
        }

        private static void LogOverworldSummary(
            UnityEngine.Terrain terrain,
            WaterZone[] zones,
            RiverPath[] rivers,
            float globalsHeight,
            float mountainLocalHeight,
            float basinDepth,
            int waterPlaneCount,
            int rawZoneCount)
        {
            Debug.Log(
                $"[Overworld] 1) Mountain height: {mountainLocalHeight:F1}m above terrain base " +
                $"(globals.height {globalsHeight:F0}m, large-lake dig cap {basinDepth:F1}m, ponds size-scaled)");

            var digs = LakeTerrainCarver.LastPerLakeMaxDigMeters;
            if (zones.Length == 0)
            {
                Debug.Log("[Overworld] 2) Lake basin depths: none (no lakes)");
            }
            else
            {
                var parts = new System.Text.StringBuilder();
                for (var i = 0; i < zones.Length; i++)
                {
                    var dig = i < digs.Count ? digs[i] : 0f;
                    var c = zones[i].GetFloodCentroidXZ();
                    var aabb = zones[i].GetFloodAabbXZ();
                    if (i > 0)
                    {
                        parts.Append("; ");
                    }

                    parts.Append(
                        $"{zones[i].Id} center=({c.x:F0},{c.z:F0}) " +
                        $"r≈{zones[i].EquivalentRadiusMeters:F0}m " +
                        $"aabb=({aabb.xMin:F0},{aabb.yMin:F0})-({aabb.xMax:F0},{aabb.yMax:F0}) " +
                        $"maxDig={dig:F1}m waterY={zones[i].WaterLevelY:F1}m");
                }

                Debug.Log(
                    $"[Overworld] 2) Lake basin depths: max={LakeTerrainCarver.LastMaxDigMeters:F1}m " +
                    $"(raw floods {rawZoneCount} → culled {zones.Length}) | {parts}");
            }

            Debug.Log($"[Overworld] 3) Lakes: {zones.Length}, water planes: {waterPlaneCount}");

            if (rivers.Length == 0)
            {
                Debug.Log("[Overworld] 4) Rivers: 0");
            }
            else
            {
                var parts = new System.Text.StringBuilder();
                for (var i = 0; i < rivers.Length; i++)
                {
                    var river = rivers[i];
                    var start = river.Centerline[0];
                    var end = river.Centerline[river.Centerline.Count - 1];
                    var startSurf = river.WaterSurfaceY[0];
                    var endSurf = river.WaterSurfaceY[river.WaterSurfaceY.Count - 1];
                    var startTerrain = SampleTerrainOrNaN(terrain, start.x, start.z);
                    var endTerrain = SampleTerrainOrNaN(terrain, end.x, end.z);
                    var pathLen = ApproximatePathLength(river);
                    if (i > 0)
                    {
                        parts.Append("; ");
                    }

                    parts.Append(
                        $"{river.Id} len={pathLen:F0}m " +
                        $"start=({start.x:F0},{start.z:F0}) surf={startSurf:F1} vsTerrain={DescribeVsTerrain(startSurf, startTerrain)} | " +
                        $"end=({end.x:F0},{end.z:F0}) surf={endSurf:F1} vsTerrain={DescribeVsTerrain(endSurf, endTerrain)} " +
                        $"→ {river.Termination}");
                }

                Debug.Log($"[Overworld] 4) Rivers: {rivers.Length} | {parts}");
            }
        }

        private static float ApproximatePathLength(RiverPath river)
        {
            var len = 0f;
            for (var i = 1; i < river.Centerline.Count; i++)
            {
                var a = river.Centerline[i - 1];
                var b = river.Centerline[i];
                var dx = b.x - a.x;
                var dz = b.z - a.z;
                len += Mathf.Sqrt(dx * dx + dz * dz);
            }

            return len;
        }

        private static float SampleTerrainOrNaN(UnityEngine.Terrain terrain, float x, float z)
        {
            if (terrain == null)
            {
                return float.NaN;
            }

            return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        }

        private static string DescribeVsTerrain(float waterY, float terrainY)
        {
            if (float.IsNaN(terrainY))
            {
                return "n/a";
            }

            var delta = waterY - terrainY;
            if (Mathf.Abs(delta) < 0.25f)
            {
                return $"on({terrainY:F1}m)";
            }

            return delta > 0f
                ? $"+{delta:F1}m above({terrainY:F1}m)"
                : $"{delta:F1}m below({terrainY:F1}m)";
        }

        public static void ClearWaterZones()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        public static void ClearRiverZones()
        {
            var root = GameObject.Find(RiverRootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private const string WaterMaterialPath = "Assets/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Water_mat.mat";

        private static Material CreateWaterMaterial()
        {
            var preset = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            if (preset != null)
            {
                var instance = new Material(preset) { name = "OverworldWater (generated)" };

                if (instance.HasProperty("_Mettalic"))
                {
                    instance.SetFloat("_Mettalic", 0.05f);
                }

                if (instance.HasProperty("_Smootness"))
                {
                    instance.SetFloat("_Smootness", Mathf.Max(instance.GetFloat("_Smootness"), 0.85f));
                }

                if (instance.HasProperty("_Cull"))
                {
                    instance.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
                }

                return instance;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var fallback = new Material(shader) { name = "OverworldWater (generated, fallback)" };
            fallback.color = new Color(0.15f, 0.35f, 0.55f, 0.75f);
            if (fallback.HasProperty("_Cull"))
            {
                fallback.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            }

            return fallback;
        }
    }
}
#endif
