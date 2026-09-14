#if UNITY_EDITOR
using System.Collections.Generic;
using GameDev.World;
using GameDev.World.Landscape;
using GameDev.World.Poi;
using GameDev.World.Roads;
using GameDev.World.Settlement;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using UnityEditor;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld roads via LandscapePlanner: nonlinear cost field, Prim Primary network among
    /// TownApproach hubs (with existing-road paint), Secondary spurs onto the growing network,
    /// vertical profiles, corridor conform (no canyon cut). Bridges rivers only.
    /// </summary>
    public static class RoadNetworkMenu
    {
        private const string RootObjectName = "OverworldRoads";
        private const float GridCellSizeMeters = 25f;
        private const string RoadCenterLayerPath = "Assets/Settings/TerrainLayers/Dirt_Road_Center.terrainlayer";
        private const string RoadEdgeLayerPath = "Assets/Settings/TerrainLayers/Dirt_Road_Edge.terrainlayer";
        private const string RoadGravelLayerPath = "Assets/Settings/TerrainLayers/Road_Gravel.terrainlayer";
private const float BridgeSpanDetectionThresholdMeters = GridCellSizeMeters * 1.8f;
        private const string BridgePrefabPath =
            "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab";
        private const float BridgeModuleLengthMeters = 4.47f;
        private const float RoadSurfaceLift = 0.08f;
        // Must stay well below road half-width + shoulder so radial fallbacks don't scallop;
        // lawn/paint prefer segment distance between these samples.
        private const float ClearanceSampleStepMeters = 1.25f;

        [MenuItem("GameDev/Overworld/Build Road Network")]
        public static void BuildRoadNetwork() => BuildRoadNetworkWithLandscapePlanner();

        /// <summary>
        /// Plans roads on LandscapeCostField, ensures POI degree ≥ 1, applies road corridor deform.
        /// </summary>
        public static void BuildRoadNetworkWithLandscapePlanner()
        {
            OverworldTerrainReady.WaitForTerrainReady();

            var terrain = OverworldTerrainAccess.FindTerrain();
            var sampler = OverworldTerrainAccess.CreateSampler(OverworldTerrainMenu.LastSeed);
            if (terrain == null || sampler == null)
            {
                return;
            }

            IWaterSampler waterZones = OverworldWaterSamplers.ForCurrentWorld(terrain);
            var heightSampler = new TerrainWorldHeightSampler(terrain);

            // World extent, not one tile — the road grid has to reach every POI, and those are
            // planned across the whole 2x2 world. A tile-sized grid silently clipped the network to
            // the bottom-left quadrant. See the note in OverworldWorldPlan.BuildAndStorePois.
            var margin = OverworldTileChunks.DefaultMarginMeters;
            var minX = margin;
            var minZ = margin;
            var usableSizeX = WorldScale.WorldExtentXMeters - 2f * margin;
            var usableSizeZ = WorldScale.WorldExtentZMeters - 2f * margin;
            // Height memo for the A* grid. RoadPathfinder asks for elevation only at
            // RoadGridSpec.CellCenter points, twice per neighbour edge, and every one of those
            // calls used to run OverworldTerrainAccess.SampleHeightWorld - which scans the scene
            // with FindObjectsByType<Terrain> before it samples anything. One measured plan made
            // ~152 M of those calls resolving to at most 12 100 distinct cells.
            //
            // The memo is keyed on an EXACT coordinate match against the cell centre, never on
            // "the point falls inside the grid". LandscapeCostField samples the same rectangle on
            // its own 10 m lattice, and answering those from a 25 m cell would collapse 6.25
            // samples onto one height and wreck the slope, curvature and roughness it derives.
            // Anything that is not bitwise a cell centre - cost field lattice, softened polyline
            // points in RoadVerticalProfile - falls through to the sampler unchanged.
            var memoGrid = default(RoadGridSpec);
            float[] memoHeights = null;

            float HeightMeters(float x, float z) =>
                memoHeights != null && TryExactCellIndex(memoGrid, x, z, out var memoIndex)
                    ? memoHeights[memoIndex]
                    : sampler.SampleWorldHeight(x, z);

            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();

            var markers = OverworldWorldPlan.GetOrBuildPois();

            var settlementAnchor = Object.FindAnyObjectByType<OverworldSettlementAnchor>();

            var nodes = new List<Vector2>();
            var isHub = new List<bool>();
            foreach (var marker in markers)
            {
                var x = marker.Position.x;
                var z = marker.Position.z;
                if (settlementAnchor != null && settlementAnchor.ApproachId == marker.Id.Value)
                {
                    x = settlementAnchor.Position.x;
                    z = settlementAnchor.Position.z;
                }

                if (!heightSampler.TrySampleWorldY(x, z, out var y))
                {
                    continue;
                }
if (waterZones != null && waterZones.TryGetWaterLevel(x, z, out var waterY) && y <= waterY)
                {
                    continue;
                }

                nodes.Add(new Vector2(x, z));
                isHub.Add(marker.Kind == PoiKind.TownApproach);
            }

            if (nodes.Count < 2)
            {
                Debug.LogWarning("[Roads] fewer than 2 dry POI nodes — skipping network.");
                return;
            }

            var width = Mathf.Max(1, Mathf.FloorToInt(usableSizeX / GridCellSizeMeters));
            var height = Mathf.Max(1, Mathf.FloorToInt(usableSizeZ / GridCellSizeMeters));
            var grid = new RoadGridSpec(minX, minZ, width, height, GridCellSizeMeters);

            // Arm the memo described above. Until this point HeightMeters is the plain sampler, so
            // nothing before it can read a half-filled table.
            memoGrid = grid;
            memoHeights = BuildCellHeightMemo(grid, sampler);

            bool CanBridgeSpan(Vector2 from, Vector2 to) => false;

            // The roads step is ~90 % of Generate All, and the per-step timing above only says
            // "roads". These sub-timings say which half: searching for routes, or writing them into
            // the terrain. Same [Gen] format, so they read inline with the rest.
            var planner = new LandscapePlanner();
            OverworldGenTiming.Time(
                "roads.costfield",
                () => planner.BuildCostField(
                    minX, minZ, usableSizeX, usableSizeZ,
                    HeightMeters,
                    (x, z) => sampler.SampleBiome(x, z),
                    WorldScale.LandscapeCostCellMeters),
                $"{usableSizeX:F0}x{usableSizeZ:F0}m at {WorldScale.LandscapeCostCellMeters}m cells");

            ApplyRoadSettings(planner.CostField, settings);
            RoadPathSimplifier.ActiveMinTurnRadiusMeters = settings.TurnRadiusMeters;
            RoadPathSimplifier.ActiveMaxHeadingChangeDegrees =
                Mathf.Clamp(180f / Mathf.Max(settings.TurnRadiusMeters, 1f) * 8f, 12f, 40f);

            OverworldGenTiming.Time(
                "roads.plan",
                () => planner.PlanRoads(nodes, isHub, grid, HeightMeters, CanBridgeSpan),
                $"grid {width}x{height} cells, {nodes.Count} nodes");

            var segments = planner.Segments;
            if (segments.Count == 0)
            {
                Debug.LogWarning("[Roads] no routable segments.");
                return;
            }

            OverworldWorldPlan.StoreRoads(planner.Roads);

            var clearanceSamples = new List<Vector2>();
            var bridgeSpans = new List<(Vector2 From, Vector2 To)>();
            var mainCount = 0;
            var branchCount = 0;

            bool IsSubmerged(float x, float z)
            {
                if (waterZones == null || !waterZones.TryGetWaterLevel(x, z, out var waterY))
                    return false;
                if (!heightSampler.TrySampleWorldY(x, z, out var groundY))
                    return true;
                return groundY <= waterY;
            }

            var dryRoadDefs = new List<RoadDefinition>();
            var profileTimer = OverworldGenTiming.Start();
            for (var si = 0; si < segments.Count; si++)
            {
                var segment = segments[si];
                if (segment.Tier == RoadTier.Primary)
                {
                    mainCount++;
                }
                else
                {
                    branchCount++;
                }

                var (dryRuns, bridges) = SplitSegmentXZ(segment.Points, IsSubmerged);
                bridgeSpans.AddRange(bridges);

                var isMain = segment.Tier == RoadTier.Primary;
                foreach (var run in dryRuns)
                {
                    if (run.Count < 2)
                    {
                        continue;
                    }

                    var profile = RoadVerticalProfile.Build(run, HeightMeters);
                    dryRoadDefs.Add(new RoadDefinition(
                        run,
                        profile,
                        isMain ? WorldScale.RoadMainWidthMeters : WorldScale.RoadBranchWidthMeters,
                        isMain ? WorldScale.RoadMainShoulderMeters : WorldScale.RoadBranchShoulderMeters,
                        isMain ? WorldScale.RoadMainBlendMeters : WorldScale.RoadBranchBlendMeters,
                        segment.Tier));
                    AppendClearanceSamples(clearanceSamples, run, ClearanceSampleStepMeters);
                }
            }

            OverworldGenTiming.Log(
                "roads.profiles",
                OverworldGenTiming.StopMs(profileTimer),
                $"{segments.Count} segments -> {dryRoadDefs.Count} corridors, " +
                $"{clearanceSamples.Count} clearance samples");

            // Every tile a corridor crosses, not just tile (0,0) — the network spans the whole
            // 2800 m world, so a single-tile deform left three quarters of the roads unsurfaced.
            var terrains = OverworldTerrainAccess.FindTerrains();
            if (terrains.Length > 0)
            {
                var centerLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadCenterLayerPath);
                var edgeLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadEdgeLayerPath);
                var gravelLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadGravelLayerPath);
                OverworldGenTiming.Time(
                    "roads.deform+paint",
                    () => TerrainDeformer.Apply(
                        terrains,
                        dryRoadDefs,
                        centerLayer,
                        gravelLayer,
                        edgeLayer),
                    $"{terrains.Length} tile(s)");
            }

            ClearRoadNetwork();
            var root = new GameObject(RootObjectName);
            var clearance = root.AddComponent<OverworldRoadClearance>();
            clearance.SetSamples(clearanceSamples);

            var bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BridgePrefabPath);
            for (var i = 0; i < bridgeSpans.Count; i++)
            {
                var (from, to) = bridgeSpans[i];
                BuildBridgeSpan(root.transform, from, to, heightSampler, bridgePrefab, i);
            }

            Debug.Log(
                $"[Roads] landscape-planner main={mainCount} branch={branchCount} " +
                $"bridges={bridgeSpans.Count} corridors={dryRoadDefs.Count} (nodes={nodes.Count}); " +
                "packed-earth alphamap painted (center/edge/gravel).");
        }

        [MenuItem("GameDev/Overworld/Repaint Road Surface")]
        public static void RepaintRoadSurface()
        {
            var terrain = UnityEngine.Terrain.activeTerrains != null &&
                          UnityEngine.Terrain.activeTerrains.Length > 0
                ? UnityEngine.Terrain.activeTerrains[0]
                : Object.FindAnyObjectByType<UnityEngine.Terrain>();
            if (terrain == null)
            {
                Debug.LogError("[Roads] No terrain found for road surface repaint.");
                return;
            }

            var clearance = Object.FindAnyObjectByType<OverworldRoadClearance>();
            if (clearance == null || clearance.Samples == null || clearance.Samples.Count == 0)
            {
                Debug.LogError(
                    "[Roads] No OverworldRoadClearance samples. Build Road Network first, then Repaint.");
                return;
            }

            var centerLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadCenterLayerPath);
            var edgeLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadEdgeLayerPath);
            var gravelLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadGravelLayerPath);
            if (centerLayer == null)
            {
                Debug.LogError($"[Roads] Missing terrain layer at {RoadCenterLayerPath}");
                return;
            }

            RoadSurfacePaint.PaintFromClearanceSamples(
                terrain,
                clearance.Samples,
                WorldScale.RoadMainWidthMeters,
                WorldScale.RoadMainShoulderMeters,
                centerLayer,
                edgeLayer,
                gravelLayer);
            SceneView.RepaintAll();
        }

        [MenuItem("GameDev/Overworld/Clear Road Network")]
        public static void ClearRoadNetwork()
        {
            var root = GameObject.Find(RootObjectName);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AppendClearanceSamples(List<Vector2> dst, IReadOnlyList<Vector2> run, float step)
        {
            if (run == null || run.Count == 0)
            {
                return;
            }

            dst.Add(run[0]);
            var stepMeters = Mathf.Max(step, 1f);
            for (var i = 1; i < run.Count; i++)
            {
                var a = run[i - 1];
                var b = run[i];
                var dist = Vector2.Distance(a, b);
                var count = Mathf.Max(1, Mathf.CeilToInt(dist / stepMeters));
                for (var s = 1; s <= count; s++)
                {
                    dst.Add(Vector2.Lerp(a, b, s / (float)count));
                }
            }
        }

        private static (List<List<Vector2>> DryRuns, List<(Vector2 From, Vector2 To)> Bridges) SplitSegmentXZ(
            IReadOnlyList<Vector2> points,
            System.Func<float, float, bool> isSubmerged)
        {
            var dryRuns = new List<List<Vector2>>();
            var bridges = new List<(Vector2, Vector2)>();

            if (points.Count < 2)
            {
                return (dryRuns, bridges);
            }

            var runStart = 0;
            for (var i = 1; i < points.Count; i++)
            {
                var a = points[i - 1];
                var b = points[i];
                var dist = Vector2.Distance(a, b);
                // Long gap alone is not a bridge (string-pull collapses dry A* stairs into long
                // chords). Only split when the chord actually crosses submerged ground.
                if (dist < BridgeSpanDetectionThresholdMeters ||
                    !SegmentCrossesWater(a, b, isSubmerged))
                {
                    continue;
                }

                if (i - runStart >= 2)
                {
                    dryRuns.Add(SubRange(points, runStart, i - runStart));
                }

                bridges.Add((a, b));
                runStart = i;
            }

            if (points.Count - runStart >= 2)
            {
                dryRuns.Add(SubRange(points, runStart, points.Count - runStart));
            }

            return (dryRuns, bridges);
        }

        private static bool SegmentCrossesWater(
            Vector2 a,
            Vector2 b,
            System.Func<float, float, bool> isSubmerged)
        {
            if (isSubmerged == null)
            {
                return false;
            }

            var dist = Vector2.Distance(a, b);
            var step = Mathf.Max(GridCellSizeMeters * 0.5f, 4f);
            var samples = Mathf.Max(2, Mathf.CeilToInt(dist / step));
            for (var s = 1; s < samples; s++)
            {
                var t = s / (float)samples;
                var p = Vector2.Lerp(a, b, t);
                if (isSubmerged(p.x, p.y))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Vector2> SubRange(IReadOnlyList<Vector2> points, int start, int count)
        {
            var result = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                result.Add(points[start + i]);
            }

            return result;
        }

        private static void BuildBridgeSpan(
            Transform parent,
            Vector2 from2D,
            Vector2 to2D,
            IWorldHeightSampler heightSampler,
            GameObject bridgePrefab,
            int index)
        {
            if (bridgePrefab == null)
            {
                return;
            }

            var fromY = heightSampler.TrySampleWorldY(from2D.x, from2D.y, out var fy) ? fy : 0f;
            var toY = heightSampler.TrySampleWorldY(to2D.x, to2D.y, out var ty) ? ty : 0f;
            var from = new Vector3(from2D.x, fromY + RoadSurfaceLift, from2D.y);
            var to = new Vector3(to2D.x, toY + RoadSurfaceLift, to2D.y);

            var delta = to - from;
            var length = delta.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            var rotation = Quaternion.LookRotation(delta / length, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
            var moduleCount = Mathf.Max(1, Mathf.RoundToInt(length / BridgeModuleLengthMeters));

            var bridgeRoot = new GameObject($"Bridge_{index}");
            bridgeRoot.transform.SetParent(parent, worldPositionStays: false);

            for (var i = 0; i < moduleCount; i++)
            {
                var t = (i + 0.5f) / moduleCount;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(bridgePrefab);
                instance.transform.SetParent(bridgeRoot.transform, worldPositionStays: false);
                instance.transform.position = Vector3.Lerp(from, to, t);
                instance.transform.rotation = rotation;
            }
        }

        private sealed class TerrainWorldHeightSampler : IWorldHeightSampler
        {
            private const float RayStartY = 4000f;
            private readonly UnityEngine.Terrain terrain;

            public TerrainWorldHeightSampler(UnityEngine.Terrain terrain)
            {
                this.terrain = terrain;
            }

            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                if (terrain != null && terrain.terrainData != null)
                {
                    var pos = terrain.transform.position;
                    var size = terrain.terrainData.size;
                    if (worldX >= pos.x && worldZ >= pos.z &&
                        worldX <= pos.x + size.x && worldZ <= pos.z + size.z)
                    {
                        worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + pos.y;
                        return true;
                    }
                }

                var origin = new Vector3(worldX, RayStartY, worldZ);
                var hits = Physics.RaycastAll(origin, Vector3.down, RayStartY * 2f);
                var bestDist = float.PositiveInfinity;
                var found = false;
                worldY = 0f;

                for (var i = 0; i < hits.Length; i++)
                {
                    if (!(hits[i].collider is TerrainCollider))
                    {
                        continue;
                    }

                    if (hits[i].distance >= bestDist)
                    {
                        continue;
                    }

                    bestDist = hits[i].distance;
                    worldY = hits[i].point.y;
                    found = true;
                }

                return found;
            }
        }

        /// <summary>Terrain height at every <see cref="RoadGridSpec"/> cell centre, sampled once.</summary>
        private static float[] BuildCellHeightMemo(RoadGridSpec grid, OverworldTerrainSampler sampler)
        {
            var heights = new float[grid.CellCount];
            for (var cz = 0; cz < grid.Height; cz++)
            {
                for (var cx = 0; cx < grid.Width; cx++)
                {
                    var center = grid.CellCenter(cx, cz);
                    heights[grid.Index(cx, cz)] = sampler.SampleWorldHeight(center.x, center.y);
                }
            }

            return heights;
        }

        /// <summary>
        /// Index of the cell whose centre is <em>exactly</em> (<paramref name="x"/>,
        /// <paramref name="z"/>), or false when the point is anything else.
        /// </summary>
        /// <remarks>
        /// Bitwise equality against <see cref="RoadGridSpec.CellCenter"/>, deliberately: a
        /// containment test would answer for every point in the cell, and the cost field samples
        /// this same rectangle on a finer lattice. Every rejection - out of range, off centre, a
        /// NaN that fails the comparison - is a fall-through to the real sampler, so a miss costs
        /// time and never an answer.
        /// </remarks>
        private static bool TryExactCellIndex(RoadGridSpec grid, float x, float z, out int index)
        {
            index = -1;
            var cx = Mathf.FloorToInt((x - grid.MinX) / grid.CellSize);
            var cz = Mathf.FloorToInt((z - grid.MinZ) / grid.CellSize);
            if (cx < 0 || cx >= grid.Width || cz < 0 || cz >= grid.Height)
            {
                return false;
            }

            var center = grid.CellCenter(cx, cz);
            if (center.x != x || center.y != z)
            {
                return false;
            }

            index = grid.Index(cx, cz);
            return true;
        }

        private static void ApplyRoadSettings(LandscapeCostField field, WorldGenSettings settings)
        {
            if (field == null)
            {
                return;
            }

            field.MaxGrade = settings.MaxGrade;
            field.MaxCutDepth = settings.MaxCutDepthMeters;
            field.MaxFillHeight = settings.MaxFillHeightMeters;
            field.SlopeWeight *= settings.PrimaryRoadCostWeight;
            field.ExcavationWeight *= settings.PrimaryRoadCostWeight;
            // Higher ParallelRoadPenalty → stronger preference for existing corridors
            // (lower discount = more penalty for leaving the corridor).
            field.ExistingCorridorDiscount = Mathf.Clamp(
                1f / Mathf.Max(settings.ParallelRoadPenalty, 0.1f),
                0.05f,
                0.5f);
            field.DetourTolerance = WorldScale.RoadDetourTolerance;
        }
    }
}
#endif
