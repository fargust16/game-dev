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
    /// Overworld roads (WORLD_GENERATION_PIPELINE.md Iteration 09): terrain-only worn paths.
    /// Wide <see cref="RoadTier.Main"/> open-TSP tract among TownApproach hubs; thinner
    /// <see cref="RoadTier.Branch"/> trails to other POIs. No road mesh/material — carve + dirt
    /// alphamap only. Bridge prefabs still span river gaps. A* avoids mountains/lakes.
    /// </summary>
    public static class RoadNetworkMenu
    {
        private const string RootObjectName = "OverworldRoads";
        private const float GridCellSizeMeters = 25f;

        // Main tract: wide, deeper trampled groove. Branches: narrow footpaths.
        private const float MainCarveWidthMeters = 16f;
        private const float MainCarveDepthMeters = 1.0f;
        private const float MainPaintWidthMeters = 12f;
        private const float BranchCarveWidthMeters = 6f;
        private const float BranchCarveDepthMeters = 0.35f;
        private const float BranchPaintWidthMeters = 4.5f;

        private const string GrooveLayerPath = "Assets/Settings/TerrainLayers/Dirt_Scaled.terrainlayer";
        private const float BridgeSpanDetectionThresholdMeters = GridCellSizeMeters * 1.8f;
        private const string BridgePrefabPath =
            "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Bridge/PT_Wooden_Bridge_02.prefab";
        private const float BridgeModuleLengthMeters = 4.47f;
        private const float RoadSurfaceLift = 0.08f;
        private const float ClearanceSampleStepMeters = 6f;

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
            var candidateChunks = OverworldTileChunks.CoveringTile(
                tileCoord, mapMagic.tileSize, OverworldTileChunks.DefaultMarginMeters);
            var placer = new StubPoiPlacer(worldSeed, sampler, candidateChunks, PoiDemoScenario.BuildRequests());

            var lakeZones = Object.FindFirstObjectByType<OverworldWaterZones>();
            var riverZones = Object.FindFirstObjectByType<OverworldRiverZones>();
            IWaterSampler waterZones = new CompositeWaterSampler(lakeZones, riverZones);
            var heightSampler = new TerrainWorldHeightSampler(terrain);
            var costSampler = new RoadCostSampler(sampler, sampler, waterZones, heightSampler);

            var settlementAnchor = Object.FindFirstObjectByType<OverworldSettlementAnchor>();

            var nodes = new List<Vector2>();
            var isHub = new List<bool>();
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
                isHub.Add(marker.Kind == PoiKind.TownApproach);
            }

            if (nodes.Count < 2)
            {
                Debug.LogWarning("[Roads] fewer than 2 dry POI nodes — skipping network.");
                return;
            }

            var margin = OverworldTileChunks.DefaultMarginMeters;
            var minX = tileCoord.x * mapMagic.tileSize.x + margin;
            var minZ = tileCoord.z * mapMagic.tileSize.z + margin;
            var usableSizeX = mapMagic.tileSize.x - 2f * margin;
            var usableSizeZ = mapMagic.tileSize.z - 2f * margin;
            var width = Mathf.Max(1, Mathf.FloorToInt(usableSizeX / GridCellSizeMeters));
            var height = Mathf.Max(1, Mathf.FloorToInt(usableSizeZ / GridCellSizeMeters));
            var grid = new RoadGridSpec(minX, minZ, width, height, GridCellSizeMeters);

            var worldHeightScale = mapMagic.globals.height;
            float HeightMeters(float x, float z) => sampler.SampleHeight(x, z) * worldHeightScale;

            var segments = RoadNetworkBuilder.BuildHierarchicalNetwork(
                nodes, isHub, grid, costSampler.SampleCost, HeightMeters);
            if (segments.Count == 0)
            {
                Debug.LogWarning("[Roads] no routable segments.");
                return;
            }

            var strokes = new List<RoadCarveStroke>();
            var clearanceSamples = new List<Vector2>();
            var bridgeSpans = new List<(Vector2 From, Vector2 To)>();
            var mainCount = 0;
            var branchCount = 0;

            foreach (var segment in segments)
            {
                if (segment.Tier == RoadTier.Main)
                {
                    mainCount++;
                }
                else
                {
                    branchCount++;
                }

                var (dryRuns, bridges) = SplitSegmentXZ(segment.Points);
                bridgeSpans.AddRange(bridges);

                var carveW = segment.Tier == RoadTier.Main ? MainCarveWidthMeters : BranchCarveWidthMeters;
                var carveD = segment.Tier == RoadTier.Main ? MainCarveDepthMeters : BranchCarveDepthMeters;
                var paintW = segment.Tier == RoadTier.Main ? MainPaintWidthMeters : BranchPaintWidthMeters;
                foreach (var run in dryRuns)
                {
                    strokes.Add(new RoadCarveStroke(run, carveW, carveD, paintW));
                    AppendClearanceSamples(clearanceSamples, run, ClearanceSampleStepMeters);
                }
            }

            if (terrain != null && strokes.Count > 0)
            {
                var grooveLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrooveLayerPath);
                RoadTerrainCarver.Carve(terrain, strokes, grooveLayer);
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
                $"[Roads] terrain-only main={mainCount} branch={branchCount} " +
                $"bridges={bridgeSpans.Count} (nodes={nodes.Count})");
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
            IReadOnlyList<Vector2> points)
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
                if (Vector2.Distance(points[i - 1], points[i]) < BridgeSpanDetectionThresholdMeters)
                {
                    continue;
                }

                if (i - runStart >= 2)
                {
                    dryRuns.Add(SubRange(points, runStart, i - runStart));
                }

                bridges.Add((points[i - 1], points[i]));
                runStart = i;
            }

            if (points.Count - runStart >= 2)
            {
                dryRuns.Add(SubRange(points, runStart, points.Count - runStart));
            }

            return (dryRuns, bridges);
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
    }
}
#endif
