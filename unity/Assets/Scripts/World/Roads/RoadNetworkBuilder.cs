using System;
using System.Collections.Generic;
using GameDev.World.Landscape;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Roads
{
    public enum RoadTier
    {
        /// <summary>TownApproach growing Prim network.</summary>
        Primary = 0,

        /// <summary>Non-town POI spur onto the existing network.</summary>
        Secondary = 1,

        /// <summary>Short stubs (widths reserved; generation deferred).</summary>
        Local = 2,
    }

    public readonly struct RoadSegment
    {
        public int NodeA { get; }
        public int NodeB { get; }
        public IReadOnlyList<Vector2> Points { get; }
        public RoadTier Tier { get; }

        public RoadSegment(int nodeA, int nodeB, IReadOnlyList<Vector2> points, RoadTier tier = RoadTier.Primary)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Points = points;
            Tier = tier;
        }
    }

    /// <summary>
    /// Connects world-space nodes into a road graph. Hierarchical build grows a Primary Prim
    /// network among town hubs (snapping onto accepted corridors), then attaches other POIs as
    /// Secondary spurs onto the whole network. Painting accepted polylines into
    /// <see cref="LandscapeCostField"/> prevents parallel A* corridors.
    /// </summary>
    public static class RoadNetworkBuilder
    {
        private const int NeighborCandidateCount = 6;
        private const int MaxRepairAttemptsPerRound = 20;
        private const float JunctionSampleStepMeters = 20f;

        public static List<RoadSegment> BuildNetwork(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null,
            Func<Vector2, Vector2, bool> canBridgeSpan = null)
        {
            return BuildMst(nodes, grid, costAtWorld, heightMetersAtWorld, RoadTier.Primary, canBridgeSpan);
        }

        /// <param name="isHub">Per-node hub flag (true = TownApproach). Non-hubs become Secondary.</param>
        public static List<RoadSegment> BuildHierarchicalNetwork(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<bool> isHub,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null,
            Func<Vector2, Vector2, bool> canBridgeSpan = null)
        {
            return BuildHierarchicalNetwork(
                nodes, isHub, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan, costField: null);
        }

        public static List<RoadSegment> BuildHierarchicalNetwork(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<bool> isHub,
            RoadGridSpec grid,
            LandscapeCostField costField,
            Func<float, float, float> heightMetersAtWorld = null,
            Func<Vector2, Vector2, bool> canBridgeSpan = null)
        {
            if (costField == null)
            {
                return new List<RoadSegment>();
            }

            Func<float, float, float> costAtWorld = (x, z) => costField.EvaluateAtTerrain(x, z);
            return BuildHierarchicalNetwork(
                nodes, isHub, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan, costField);
        }

        private static List<RoadSegment> BuildHierarchicalNetwork(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<bool> isHub,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            Func<Vector2, Vector2, bool> canBridgeSpan,
            LandscapeCostField costField)
        {
            var segments = new List<RoadSegment>();
            var n = nodes.Count;
            if (n < 2 || isHub == null || isHub.Count != n)
            {
                return BuildNetwork(nodes, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan);
            }

            var hubs = new List<int>();
            for (var i = 0; i < n; i++)
            {
                if (isHub[i])
                {
                    hubs.Add(i);
                }
            }

            if (hubs.Count >= 2)
            {
                GrowPrimNetwork(
                    hubs, nodes, segments, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan,
                    costField, RoadTier.Primary);
            }
            else if (hubs.Count == 0)
            {
                return BuildNetwork(nodes, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan);
            }

            for (var i = 0; i < n; i++)
            {
                if (isHub[i])
                {
                    continue;
                }

                if (!TryAttachToNetwork(
                        nodes[i], i, segments, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan,
                        RoadTier.Secondary, acceptAnyTier: true, out var branch))
                {
                    continue;
                }

                segments.Add(branch);
                Paint(costField, branch.Points);
            }

            return segments;
        }

        /// <summary>
        /// Constraint: every POI node must have degree ≥ 1. Connect orphans to the nearest
        /// network point (existing polyline or node) when a path exists.
        /// </summary>
        public static void EnsureMinimumConnectivity(
            IReadOnlyList<Vector2> nodes,
            List<RoadSegment> segments,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null,
            Func<Vector2, Vector2, bool> canBridgeSpan = null,
            int minimumDegree = 1,
            LandscapeCostField costField = null)
        {
            if (nodes == null || segments == null || nodes.Count == 0 || minimumDegree < 1)
            {
                return;
            }

            var degree = new int[nodes.Count];
            for (var i = 0; i < segments.Count; i++)
            {
                var a = segments[i].NodeA;
                var b = segments[i].NodeB;
                if (a >= 0 && a < degree.Length)
                {
                    degree[a]++;
                }

                if (b >= 0 && b < degree.Length)
                {
                    degree[b]++;
                }
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                if (degree[i] >= minimumDegree)
                {
                    continue;
                }

                if (TryAttachToNetwork(
                        nodes[i], i, segments, grid, costAtWorld, heightMetersAtWorld, canBridgeSpan,
                        RoadTier.Secondary, acceptAnyTier: true, out var branch))
                {
                    segments.Add(branch);
                    degree[i]++;
                    Paint(costField, branch.Points);
                    continue;
                }

                var bestCost = float.PositiveInfinity;
                List<Vector2> bestPath = null;
                var bestJ = -1;
                for (var j = 0; j < nodes.Count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var path = new List<Vector2>();
                    if (!RoadPathfinder.TryFindPath(
                            grid, costAtWorld, nodes[i], nodes[j], path, out var cost,
                            heightMetersAtWorld, canBridgeSpan))
                    {
                        continue;
                    }

                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestPath = path;
                        bestJ = j;
                    }
                }

                if (bestPath != null && bestJ >= 0)
                {
                    var seg = new RoadSegment(
                        i, bestJ, FinalizePath(bestPath, costAtWorld, grid.CellSize), RoadTier.Secondary);
                    segments.Add(seg);
                    degree[i]++;
                    Paint(costField, seg.Points);
                }
            }
        }

        private static void GrowPrimNetwork(
            List<int> hubs,
            IReadOnlyList<Vector2> nodes,
            List<RoadSegment> segments,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            Func<Vector2, Vector2, bool> canBridgeSpan,
            LandscapeCostField costField,
            RoadTier tier)
        {
            var connected = new HashSet<int> { hubs[0] };
            var remaining = new HashSet<int>();
            for (var i = 1; i < hubs.Count; i++)
            {
                remaining.Add(hubs[i]);
            }

            while (remaining.Count > 0)
            {
                var bestCost = float.PositiveInfinity;
                var bestHub = -1;
                RoadSegment bestSeg = default;
                var found = false;

                foreach (var hub in remaining)
                {
                    // Prefer snap onto existing Primary corridors once any exist.
                    if (segments.Count > 0 &&
                        TryAttachToNetwork(
                            nodes[hub], hub, segments, grid, costAtWorld, heightMetersAtWorld,
                            canBridgeSpan, tier, acceptAnyTier: false, out var snapSeg))
                    {
                        var snapCost = EstimatePathCost(snapSeg.Points, costAtWorld);
                        // Also compare a direct route to the nearest connected hub.
                        var directOk = TryBestHubLink(
                            hub, connected, nodes, grid, costAtWorld, heightMetersAtWorld,
                            canBridgeSpan, tier, out var directSeg, out var directCost);

                        var useSnap = true;
                        if (directOk)
                        {
                            var detour = costField != null
                                ? costField.DetourTolerance
                                : WorldScale.RoadDetourTolerance;
                            // Independent route only wins if clearly cheaper than snap×tolerance.
                            if (directCost * detour < snapCost)
                            {
                                useSnap = false;
                            }
                        }

                        if (useSnap && snapCost < bestCost)
                        {
                            bestCost = snapCost;
                            bestHub = hub;
                            bestSeg = snapSeg;
                            found = true;
                        }
                        else if (!useSnap && directOk && directCost < bestCost)
                        {
                            bestCost = directCost;
                            bestHub = hub;
                            bestSeg = directSeg;
                            found = true;
                        }

                        continue;
                    }

                    if (TryBestHubLink(
                            hub, connected, nodes, grid, costAtWorld, heightMetersAtWorld,
                            canBridgeSpan, tier, out var linkSeg, out var linkCost) &&
                        linkCost < bestCost)
                    {
                        bestCost = linkCost;
                        bestHub = hub;
                        bestSeg = linkSeg;
                        found = true;
                    }
                }

                if (!found || bestHub < 0)
                {
                    break;
                }

                segments.Add(bestSeg);
                Paint(costField, bestSeg.Points);
                connected.Add(bestHub);
                remaining.Remove(bestHub);
            }
        }

        private static bool TryBestHubLink(
            int hub,
            HashSet<int> connected,
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            Func<Vector2, Vector2, bool> canBridgeSpan,
            RoadTier tier,
            out RoadSegment segment,
            out float cost)
        {
            segment = default;
            cost = float.PositiveInfinity;
            var bestPath = (List<Vector2>)null;
            var bestOther = -1;

            foreach (var other in connected)
            {
                var path = new List<Vector2>();
                if (!RoadPathfinder.TryFindPath(
                        grid, costAtWorld, nodes[hub], nodes[other], path, out var c,
                        heightMetersAtWorld, canBridgeSpan))
                {
                    continue;
                }

                if (c < cost)
                {
                    cost = c;
                    bestPath = path;
                    bestOther = other;
                }
            }

            if (bestPath == null || bestOther < 0)
            {
                return false;
            }

            segment = new RoadSegment(
                hub, bestOther, FinalizePath(bestPath, costAtWorld, grid.CellSize), tier);
            return true;
        }

        /// <summary>
        /// Spur from a POI to the best connection point on the existing network (min connection cost).
        /// </summary>
        private static bool TryAttachToNetwork(
            Vector2 poi,
            int branchIndex,
            IReadOnlyList<RoadSegment> network,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            Func<Vector2, Vector2, bool> canBridgeSpan,
            RoadTier tier,
            bool acceptAnyTier,
            out RoadSegment segment)
        {
            segment = default;
            var bestCost = float.PositiveInfinity;
            List<Vector2> bestPath = null;
            var bestJunction = poi;
            var bestAnchor = branchIndex;

            for (var s = 0; s < network.Count; s++)
            {
                var seg = network[s];
                if (!acceptAnyTier && seg.Tier != RoadTier.Primary)
                {
                    continue;
                }

                if (seg.Points == null || seg.Points.Count < 2)
                {
                    continue;
                }

                foreach (var sample in SamplePolyline(seg.Points, JunctionSampleStepMeters))
                {
                    var path = new List<Vector2>();
                    if (!RoadPathfinder.TryFindPath(
                            grid, costAtWorld, poi, sample, path, out var cost,
                            heightMetersAtWorld, canBridgeSpan))
                    {
                        continue;
                    }

                    // Soft preference for shorter Euclidean connectors (anti-parallel).
                    var euclid = Vector2.Distance(poi, sample);
                    var scored = cost + euclid * 0.05f;
                    if (scored < bestCost)
                    {
                        bestCost = scored;
                        bestPath = path;
                        bestJunction = sample;
                        bestAnchor = seg.NodeA;
                    }
                }
            }

            if (bestPath == null)
            {
                return false;
            }

            if (bestPath.Count > 0)
            {
                bestPath[bestPath.Count - 1] = bestJunction;
            }

            segment = new RoadSegment(
                branchIndex,
                bestAnchor,
                FinalizePath(bestPath, costAtWorld, grid.CellSize),
                tier);
            return true;
        }

        private static IEnumerable<Vector2> SamplePolyline(IReadOnlyList<Vector2> points, float stepMeters)
        {
            yield return points[0];
            var remaining = stepMeters;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var len = Vector2.Distance(a, b);
                var t = 0f;
                while (t + remaining <= len)
                {
                    t += remaining;
                    yield return Vector2.Lerp(a, b, t / Mathf.Max(len, 1e-4f));
                    remaining = stepMeters;
                }

                remaining -= len - t;
            }

            yield return points[points.Count - 1];
        }

        private static float EstimatePathCost(IReadOnlyList<Vector2> points, Func<float, float, float> costAtWorld)
        {
            if (points == null || points.Count < 2)
            {
                return float.PositiveInfinity;
            }

            var sum = 0f;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var dist = Vector2.Distance(a, b);
                var c = (costAtWorld(a.x, a.y) + costAtWorld(b.x, b.y)) * 0.5f;
                if (float.IsPositiveInfinity(c))
                {
                    return float.PositiveInfinity;
                }

                sum += dist * c;
            }

            return sum;
        }

        private static void Paint(LandscapeCostField costField, IReadOnlyList<Vector2> points)
        {
            costField?.PaintExistingRoad(points);
        }

        private static List<RoadSegment> BuildMst(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            RoadTier tier,
            Func<Vector2, Vector2, bool> canBridgeSpan = null)
        {
            var segments = new List<RoadSegment>();
            var n = nodes.Count;
            if (n < 2)
            {
                return segments;
            }

            var union = new UnionFind(n);
            var candidateEdges = BuildCandidateEdges(nodes, NeighborCandidateCount);
            var weighted = new List<(float Cost, int A, int B, List<Vector2> Path)>(candidateEdges.Count);
            foreach (var (a, b) in candidateEdges)
            {
                var path = new List<Vector2>();
                if (RoadPathfinder.TryFindPath(
                        grid, costAtWorld, nodes[a], nodes[b], path, out var cost,
                        heightMetersAtWorld, canBridgeSpan))
                {
                    weighted.Add((cost, a, b, path));
                }
            }

            weighted.Sort((x, y) => x.Cost.CompareTo(y.Cost));

            foreach (var edge in weighted)
            {
                if (union.Find(edge.A) == union.Find(edge.B))
                {
                    continue;
                }

                union.Union(edge.A, edge.B);
                segments.Add(new RoadSegment(
                    edge.A, edge.B, FinalizePath(edge.Path, costAtWorld, grid.CellSize), tier));
            }

            RepairDisconnectedComponents(
                nodes, grid, costAtWorld, heightMetersAtWorld, union, segments, tier, canBridgeSpan);

            return segments;
        }

        private static List<(int A, int B)> BuildCandidateEdges(IReadOnlyList<Vector2> nodes, int k)
        {
            var n = nodes.Count;
            var edgeSet = new HashSet<(int, int)>();
            var neighbors = new List<(float Dist, int Index)>(n);

            for (var i = 0; i < n; i++)
            {
                neighbors.Clear();
                for (var j = 0; j < n; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    neighbors.Add((Vector2.Distance(nodes[i], nodes[j]), j));
                }

                neighbors.Sort((a, b) => a.Dist.CompareTo(b.Dist));
                var take = Mathf.Min(k, neighbors.Count);
                for (var t = 0; t < take; t++)
                {
                    var j = neighbors[t].Index;
                    edgeSet.Add(i < j ? (i, j) : (j, i));
                }
            }

            return new List<(int, int)>(edgeSet);
        }

        private static void RepairDisconnectedComponents(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            UnionFind union,
            List<RoadSegment> segments,
            RoadTier tier,
            Func<Vector2, Vector2, bool> canBridgeSpan)
        {
            while (TryMergeOneComponent(
                       nodes, grid, costAtWorld, heightMetersAtWorld, union, segments, tier, canBridgeSpan))
            {
            }
        }

        private static bool TryMergeOneComponent(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            UnionFind union,
            List<RoadSegment> segments,
            RoadTier tier,
            Func<Vector2, Vector2, bool> canBridgeSpan)
        {
            var n = nodes.Count;
            var groups = new Dictionary<int, List<int>>();
            for (var i = 0; i < n; i++)
            {
                var root = union.Find(i);
                if (!groups.TryGetValue(root, out var list))
                {
                    groups[root] = list = new List<int>();
                }

                list.Add(i);
            }

            if (groups.Count <= 1)
            {
                return false;
            }

            var groupList = new List<List<int>>(groups.Values);
            var candidates = new List<(float Dist, int A, int B)>();
            for (var gi = 0; gi < groupList.Count; gi++)
            {
                for (var gj = gi + 1; gj < groupList.Count; gj++)
                {
                    foreach (var a in groupList[gi])
                    {
                        foreach (var b in groupList[gj])
                        {
                            candidates.Add((Vector2.Distance(nodes[a], nodes[b]), a, b));
                        }
                    }
                }
            }

            candidates.Sort((x, y) => x.Dist.CompareTo(y.Dist));

            var tried = 0;
            foreach (var candidate in candidates)
            {
                if (tried >= MaxRepairAttemptsPerRound)
                {
                    break;
                }

                tried++;

                var path = new List<Vector2>();
                if (!RoadPathfinder.TryFindPath(
                        grid, costAtWorld, nodes[candidate.A], nodes[candidate.B], path, out _,
                        heightMetersAtWorld, canBridgeSpan))
                {
                    continue;
                }

                union.Union(candidate.A, candidate.B);
                segments.Add(new RoadSegment(
                    candidate.A, candidate.B, FinalizePath(path, costAtWorld, grid.CellSize), tier));
                return true;
            }

            return false;
        }

        private static List<Vector2> FinalizePath(
            List<Vector2> path,
            Func<float, float, float> costAtWorld,
            float cellSizeMeters)
        {
            if (path == null || path.Count < 2)
            {
                return path ?? new List<Vector2>();
            }

            return RoadPathSimplifier.SoftenNatural(path, costAtWorld, cellSizeMeters * 0.5f);
        }

        private sealed class UnionFind
        {
            private readonly int[] parent;
            private readonly int[] rank;

            public UnionFind(int n)
            {
                parent = new int[n];
                rank = new int[n];
                for (var i = 0; i < n; i++)
                {
                    parent[i] = i;
                }
            }

            public int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }

                return x;
            }

            public void Union(int a, int b)
            {
                var ra = Find(a);
                var rb = Find(b);
                if (ra == rb)
                {
                    return;
                }

                if (rank[ra] < rank[rb])
                {
                    (ra, rb) = (rb, ra);
                }

                parent[rb] = ra;
                if (rank[ra] == rank[rb])
                {
                    rank[ra]++;
                }
            }
        }
    }
}
