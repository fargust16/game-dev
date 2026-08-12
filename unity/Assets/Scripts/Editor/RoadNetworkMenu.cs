#if UNITY_EDITOR
using System.Collections.Generic;
using Den.Tools;
using GameDev.World.Poi;
using GameDev.World.Roads;
using GameDev.World.Seed;
using GameDev.World.Settlement;
using GameDev.World.Streaming;
using GameDev.World.Terrain;
using GameDev.World.Water;
using MapMagic.Core;
using MapMagic.Terrains;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Overworld V0 roads, first slice (WORLD_GENERATION_PIPELINE.md Iteration 09): connects the
    /// same <see cref="StubPoiPlacer"/> markers <see cref="PoiMarkerMenu"/> visualizes into a
    /// minimum-spanning-tree road graph (<see cref="RoadNetworkBuilder"/>), routed with a
    /// slope/grade/biome/water-aware A* (<see cref="RoadPathfinder"/>, <see cref="RoadCost"/>)
    /// instead of straight lines - climbs favour a longer traverse over a short, steep direct
    /// ascent (grade costing, see <see cref="RoadCost.GradeMultiplier"/>), and bridge a narrow lake
    /// crossing (<see cref="RoadCost.MaxBridgeSpanMeters"/>) rather than always detouring around it.
    /// A wide lake still has no route across it - no real river/strait system exists yet (Iteration
    /// 05's "no flow-based river carving yet" note). Rendered geometry hugs the real terrain (dense
    /// re-sampling between the sparse pathfinding waypoints, see <see cref="RoadPathDensifier"/>)
    /// and presses a worn-track groove into the heightmap along its route
    /// (<see cref="RoadTerrainCarver"/>) instead of resting on top of it.
    /// </summary>
    public static class RoadNetworkMenu
    {
        private const string RootObjectName = "OverworldRoads";
        private const float GridCellSizeMeters = 25f;
        private const float RoadWidthMeters = 3.5f;

        // Small - the carved groove (CarveDepthMeters) already gives the road its visual "sunken
        // into the ground" depth; this is only enough to dodge z-fighting with the trench floor.
        private const float RoadSurfaceLift = 0.08f;

        // Re-sampling interval for the rendered ribbon - much finer than the pathfinding grid's own
        // ~25-35m waypoint spacing, so the mesh follows real terrain undulation between waypoints
        // instead of cutting a straight line across every dip and rise in between.
        private const float RoadDensifyStepMeters = 4f;

        // Wider than the road mesh itself so the groove's banks show past the road's own edges.
        // Both raised from an initial (RoadWidthMeters+3, 0.5m) pass that turned out invisible in
        // practice - the terrain heightmap/alphamap resolution is coarse enough (a few metres per
        // texel on a ~1400m tile) that a narrow, shallow carve barely registered as more than a
        // couple of pixels, reading as a flat ribbon laid on top of the ground rather than a real
        // sunken track. This aims for a genuine holloway depth instead; RoadTerrainCarver's own
        // MinRadiusPixels floor guards against under-resolving it regardless of the map's actual
        // heightmap/alphamap resolution.
        private const float CarveWidthMeters = RoadWidthMeters + 10.5f;
        private const float CarveDepthMeters = 1.4f;

        // Terrain layer painted into the alphamap along the same footprint as the height carve, so
        // the groove reads as trampled bare dirt, not just a dip in the grass - reuses the same
        // scaled Dirt layer OverworldTerrainMenu.BuildGraph already paints onto wet-low ground
        // (Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer), which is guaranteed to exist by
        // the time roads are built (terrain must already be generated).
        private const string GrooveLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";

        // A normal A* step is at most one diagonal grid cell (~35.4m); a bridge edge always skips
        // at least 2 cells (~50m+, see RoadPathfinder.TryBridge). This gap safely tells the two
        // apart in the reconstructed path without RoadSegment needing any extra bridge metadata.
        private const float BridgeSpanDetectionThresholdMeters = GridCellSizeMeters * 1.8f;

        private const string BridgePrefabPath = "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab";

        // The prefab's own LODGroup.m_Size (~4.47) is the closest thing to a real-world length
        // available without opening the mesh in Unity - used directly as the tiling pitch so
        // consecutive modules sit edge-to-edge instead of gapping or overlapping.
        private const float BridgeModuleLengthMeters = 4.47f;

        [MenuItem("GameDev/Overworld/Build Road Network")]
        public static void BuildRoadNetwork()
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
            var holder = terrain != null ? terrain.GetComponent<DirectMatricesHolder>() : null;
            if (holder == null)
            {
                return;
            }

            var sampler = new MapMagicBiomeSampler(holder);
            var worldSeed = new WorldSeed(OverworldTerrainMenu.LastSeed);
            var candidateChunks = OverworldTileChunks.CoveringTile(tileCoord, mapMagic.tileSize, OverworldTileChunks.DefaultMarginMeters);
            var placer = new StubPoiPlacer(worldSeed, sampler, candidateChunks, PoiDemoScenario.BuildRequests());

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);
            var heightSampler = new RaycastWorldHeightSampler();
            var costSampler = new RoadCostSampler(sampler, sampler, waterZones, heightSampler);

            // The placed settlement can land well away from its raw TownApproach marker (see
            // OverworldSettlementAnchor - the dry-ground search moves it up to ~192m), so route to
            // where it actually is, not the original marker coordinate, or the road can end up
            // stopping short of the real town fabric.
            var settlementAnchor = Object.FindFirstObjectByType<OverworldSettlementAnchor>();

            var nodes = new List<Vector2>();
            foreach (var marker in placer.AllMarkers())
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
            }

            if (nodes.Count < 2)
            {
                return;
            }

            // Stop short of the raw tile edge by the same margin POI candidates and water sampling
            // respect (OverworldTileChunks) - the border strip beyond it has no reliable water/height
            // data, so a route through there previously read as free, dry ground and hugged the tile
            // boundary straight across what was actually still lake.
            var margin = OverworldTileChunks.DefaultMarginMeters;
            var minX = tileCoord.x * mapMagic.tileSize.x + margin;
            var minZ = tileCoord.z * mapMagic.tileSize.z + margin;
            var usableSizeX = mapMagic.tileSize.x - 2f * margin;
            var usableSizeZ = mapMagic.tileSize.z - 2f * margin;
            var width = Mathf.Max(1, Mathf.FloorToInt(usableSizeX / GridCellSizeMeters));
            var height = Mathf.Max(1, Mathf.FloorToInt(usableSizeZ / GridCellSizeMeters));
            var grid = new RoadGridSpec(minX, minZ, width, height, GridCellSizeMeters);

            // Real elevation (metres) for grade costing - BiomeSampler.SampleHeight is normalized
            // 0-1, so it needs the world's height scale to mean anything as a rise-over-run. Without
            // this a climb only ever costs by how steep the ground reads AT a point (RoadCost.Evaluate's
            // slope01 term), which can't tell "walking along a mountainside's contour" from "walking
            // straight up it" - both read equally steep as a per-point value - so a road just took the
            // direct, near-vertical way up any climb.
            var worldHeightScale = mapMagic.globals.height;
            float HeightMeters(float x, float z) => sampler.SampleHeight(x, z) * worldHeightScale;

            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, costSampler.SampleCost, HeightMeters);
            if (segments.Count == 0)
            {
                return;
            }

            // Split each segment's XZ path into ordinary-road runs and bridge spans before any Y
            // sampling or terrain carving - a bridge shows up as one unusually long jump between
            // consecutive waypoints (RoadPathfinder.TryBridge skips the submerged cells between
            // entirely, it never records them), and only the ordinary runs should carve a groove or
            // get densified against the terrain (a bridge stays a straight, level span over water).
            var plans = new List<(RoadSegment Segment, List<List<Vector2>> RibbonRuns, List<(Vector2 From, Vector2 To)> Bridges)>(segments.Count);
            var allRibbonCenterlines = new List<IReadOnlyList<Vector2>>();
            foreach (var segment in segments)
            {
                var (ribbonRuns, bridgeSpans) = SplitSegmentXZ(segment.Points);
                plans.Add((segment, ribbonRuns, bridgeSpans));
                allRibbonCenterlines.AddRange(ribbonRuns);
            }

            if (terrain != null)
            {
                var grooveLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrooveLayerPath);
                RoadTerrainCarver.Carve(terrain, allRibbonCenterlines, CarveWidthMeters, CarveDepthMeters, grooveLayer);
            }

            ClearRoadNetwork();
            var root = new GameObject(RootObjectName);
            var material = CreateRoadMaterial();
            var bridgePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BridgePrefabPath);

            foreach (var (segment, ribbonRuns, bridgeSpans) in plans)
            {
                foreach (var run in ribbonRuns)
                {
                    BuildRoadRibbon(root.transform, segment, run, heightSampler, material);
                }

                foreach (var (from, to) in bridgeSpans)
                {
                    BuildBridgeSpan(root.transform, segment, from, to, heightSampler, bridgePrefab);
                }
            }
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

        // Splits a segment's XZ path into ordinary-road runs and bridge spans, purely by geometry
        // (no Y involved yet) - a bridge is one unusually long jump between consecutive waypoints
        // (RoadPathfinder.TryBridge skips the submerged cells between entirely, it never records
        // them). Doing this split before any terrain work means only real, drivable ground ever
        // gets densified or carved - a bridge stays a straight, level span over water.
        private static (List<List<Vector2>> RibbonRuns, List<(Vector2 From, Vector2 To)> Bridges) SplitSegmentXZ(
            IReadOnlyList<Vector2> points)
        {
            var ribbonRuns = new List<List<Vector2>>();
            var bridges = new List<(Vector2, Vector2)>();

            if (points.Count < 2)
            {
                return (ribbonRuns, bridges);
            }

            var runStart = 0;
            for (var i = 1; i < points.Count; i++)
            {
                if (Vector2.Distance(points[i - 1], points[i]) < BridgeSpanDetectionThresholdMeters)
                {
                    continue;
                }

                if (i - runStart >= 2)
                {
                    ribbonRuns.Add(SubRange(points, runStart, i - runStart));
                }

                bridges.Add((points[i - 1], points[i]));
                runStart = i;
            }

            if (points.Count - runStart >= 2)
            {
                ribbonRuns.Add(SubRange(points, runStart, points.Count - runStart));
            }

            return (ribbonRuns, bridges);
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

        // Densifies the coarse waypoint run and re-samples real ground Y at every dense point
        // (RoadTerrainCarver.Carve has already run by this point, so this hits the carved groove,
        // not the pre-carve surface) - this is what actually makes the ribbon follow the terrain's
        // undulation instead of cutting a straight line between sparse pathfinding waypoints.
        private static void BuildRoadRibbon(
            Transform parent,
            RoadSegment segment,
            List<Vector2> run,
            RaycastWorldHeightSampler heightSampler,
            Material material)
        {
            var dense = RoadPathDensifier.Densify(run, RoadDensifyStepMeters);
            var points = new List<Vector3>(dense.Count);
            foreach (var p in dense)
            {
                var y = heightSampler.TrySampleWorldY(p.x, p.y, out var groundY) ? groundY : 0f;
                points.Add(new Vector3(p.x, y + RoadSurfaceLift, p.y));
            }

            if (points.Count < 2)
            {
                return;
            }

            var go = new GameObject($"Road_{segment.NodeA}_{segment.NodeB}_{parent.childCount}");
            go.transform.SetParent(parent, worldPositionStays: false);

            var mesh = RoadMeshBuilder.Build(points, RoadWidthMeters, RoadWidthMeters);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        // Tiles the modular bridge prefab edge-to-edge across the gap instead of stretching a
        // single instance - the source asset is a short (~4.5m) repeatable module, not a
        // full-length span.
        private static void BuildBridgeSpan(
            Transform parent,
            RoadSegment segment,
            Vector2 from2D,
            Vector2 to2D,
            RaycastWorldHeightSampler heightSampler,
            GameObject bridgePrefab)
        {
            if (bridgePrefab == null)
            {
                return; // no asset available - nothing else renders this gap, it's simply skipped
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

            // PT_Wooden_Bridge_02's deck runs along its local X axis, not the Z axis
            // Quaternion.LookRotation aligns to forward (Z) - without correction every module lands
            // rotated 90 degrees off the crossing direction, so consecutive modules sit side-by-side
            // instead of end-to-end (exactly the "standing in a row facing the wrong way" look this
            // fixes). The extra -90 deg turn re-points local X at the crossing direction instead.
            var rotation = Quaternion.LookRotation(delta / length, Vector3.up) * Quaternion.Euler(0f, -90f, 0f);
            var moduleCount = Mathf.Max(1, Mathf.RoundToInt(length / BridgeModuleLengthMeters));

            var bridgeRoot = new GameObject($"Bridge_{segment.NodeA}_{segment.NodeB}_{parent.childCount}");
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

        // Reuses MapMagic's own Dirt diffuse texture (Assets/MapMagic/Demo/LandTextures/Dirt.tif) -
        // no dedicated road texture exists in the project yet, and this reads as a worn dirt path
        // without needing a new imported asset. UV tiling is baked into RoadMeshBuilder's own UVs
        // (traveled distance / RoadWidthMeters), not this material's texture scale.
        private static Material CreateRoadMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { color = new Color(0.55f, 0.45f, 0.3f) };
            var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/MapMagic/Demo/LandTextures/Dirt.tif");
            if (diffuse != null)
            {
                material.mainTexture = diffuse;
            }

            return material;
        }

        /// <summary>Editor height probe - prefers a real TerrainCollider hit over any other
        /// collider (props, leftover markers) that might sit above the ground at the same XZ.</summary>
        private sealed class RaycastWorldHeightSampler : IWorldHeightSampler
        {
            private const float RayStartY = 400f;

            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
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

                if (found)
                {
                    return true;
                }

                for (var i = 0; i < hits.Length; i++)
                {
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
    }
}
#endif
