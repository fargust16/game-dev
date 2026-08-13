using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    public enum RoadTier
    {
        /// <summary>TownApproach ↔ TownApproach primary network.</summary>
        Main,

        /// <summary>Spur from a non-town POI to the nearest hub.</summary>
        Branch,
    }

    public readonly struct RoadSegment
    {
        public int NodeA { get; }
        public int NodeB { get; }
        public IReadOnlyList<Vector2> Points { get; }
        public RoadTier Tier { get; }

        public RoadSegment(int nodeA, int nodeB, IReadOnlyList<Vector2> points, RoadTier tier = RoadTier.Main)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Points = points;
            Tier = tier;
        }
    }

    /// <summary>
    /// Connects world-space nodes into a road graph. <see cref="BuildNetwork"/> builds a flat MST
    /// over every node. <see cref="BuildHierarchicalNetwork"/> first solves an open TSP path among
    /// hub nodes (towns) as <see cref="RoadTier.Main"/>, then attaches remaining POIs as
    /// <see cref="RoadTier.Branch"/> spurs.
    /// </summary>
    public static class RoadNetworkBuilder
    {
        private const int NeighborCandidateCount = 6;
        private const int MaxRepairAttemptsPerRound = 20;
        // Exact open-path TSP over A* costs; beyond this fall back to MST (factorial blow-up).
        private const int MaxExactTspHubs = 9;

        public static List<RoadSegment> BuildNetwork(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null)
        {
            return BuildMst(nodes, grid, costAtWorld, heightMetersAtWorld, RoadTier.Main);
        }

        /// <param name="isHub">Per-node hub flag (true = TownApproach). Non-hubs become branches.</param>
        public static List<RoadSegment> BuildHierarchicalNetwork(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<bool> isHub,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null)
        {
            var segments = new List<RoadSegment>();
            var n = nodes.Count;
            if (n < 2 || isHub == null || isHub.Count != n)
            {
                return BuildNetwork(nodes, grid, costAtWorld, heightMetersAtWorld);
            }

            var hubLocal = new List<Vector2>();
            var hubGlobal = new List<int>();
            for (var i = 0; i < n; i++)
            {
                if (!isHub[i])
                {
                    continue;
                }

                hubLocal.Add(nodes[i]);
                hubGlobal.Add(i);
            }

            if (hubLocal.Count >= 2)
            {
                var hubSegments = hubLocal.Count <= MaxExactTspHubs
                    ? BuildOpenTspPath(hubLocal, grid, costAtWorld, heightMetersAtWorld, RoadTier.Main)
                    : BuildMst(hubLocal, grid, costAtWorld, heightMetersAtWorld, RoadTier.Main);

                foreach (var seg in hubSegments)
                {
                    segments.Add(new RoadSegment(
                        hubGlobal[seg.NodeA], hubGlobal[seg.NodeB], seg.Points, RoadTier.Main));
                }
            }
            else if (hubLocal.Count == 0)
            {
                return BuildNetwork(nodes, grid, costAtWorld, heightMetersAtWorld);
            }

            for (var i = 0; i < n; i++)
            {
                if (isHub[i])
                {
                    continue;
                }

                if (!TryAttachBranch(nodes, hubGlobal, i, grid, costAtWorld, heightMetersAtWorld, out var branch))
                {
                    continue;
                }

                segments.Add(branch);
            }

            return segments;
        }

        /// <summary>
        /// Open traveling-salesman path among hubs: visit each once, minimize sum of A* edge costs
        /// (no return-to-start). Exact for small hub counts used by the overworld demo.
        /// </summary>
        private static List<RoadSegment> BuildOpenTspPath(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            RoadTier tier)
        {
            var n = nodes.Count;
            var segments = new List<RoadSegment>();
            if (n < 2)
            {
                return segments;
            }

            var costs = new float[n, n];
            var paths = new List<Vector2>[n, n];
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var path = new List<Vector2>();
                    if (RoadPathfinder.TryFindPath(
                            grid, costAtWorld, nodes[i], nodes[j], path, out var cost, heightMetersAtWorld))
                    {
                        costs[i, j] = cost;
                        costs[j, i] = cost;
                        paths[i, j] = path;
                        paths[j, i] = ReversePath(path);
                    }
                    else
                    {
                        costs[i, j] = float.PositiveInfinity;
                        costs[j, i] = float.PositiveInfinity;
                    }
                }
            }

            var order = new int[n];
            for (var i = 0; i < n; i++)
            {
                order[i] = i;
            }

            var bestOrder = (int[])order.Clone();
            var bestCost = float.PositiveInfinity;
            PermuteOpenTsp(order, 0, costs, ref bestCost, bestOrder);

            if (float.IsPositiveInfinity(bestCost))
            {
                return BuildMst(nodes, grid, costAtWorld, heightMetersAtWorld, tier);
            }

            for (var i = 0; i < n - 1; i++)
            {
                var a = bestOrder[i];
                var b = bestOrder[i + 1];
                var path = paths[a, b];
                if (path == null || path.Count < 2)
                {
                    continue;
                }

                segments.Add(new RoadSegment(a, b, path, tier));
            }

            return segments;
        }

        private static void PermuteOpenTsp(
            int[] order, int start, float[,] costs, ref float bestCost, int[] bestOrder)
        {
            var n = order.Length;
            if (start >= n - 1)
            {
                var total = 0f;
                for (var i = 0; i < n - 1; i++)
                {
                    var edge = costs[order[i], order[i + 1]];
                    if (float.IsPositiveInfinity(edge))
                    {
                        return;
                    }

                    total += edge;
                    if (total >= bestCost)
                    {
                        return;
                    }
                }

                if (total < bestCost)
                {
                    bestCost = total;
                    Array.Copy(order, bestOrder, n);
                }

                return;
            }

            for (var i = start; i < n; i++)
            {
                (order[start], order[i]) = (order[i], order[start]);
                PermuteOpenTsp(order, start + 1, costs, ref bestCost, bestOrder);
                (order[start], order[i]) = (order[i], order[start]);
            }
        }

        private static List<Vector2> ReversePath(IReadOnlyList<Vector2> path)
        {
            var rev = new List<Vector2>(path.Count);
            for (var i = path.Count - 1; i >= 0; i--)
            {
                rev.Add(path[i]);
            }

            return rev;
        }

        private static bool TryAttachBranch(
            IReadOnlyList<Vector2> nodes,
            IReadOnlyList<int> hubGlobals,
            int branchIndex,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            out RoadSegment segment)
        {
            segment = default;
            if (hubGlobals.Count == 0)
            {
                return false;
            }

            var candidates = new List<(float Dist, int Hub)>(hubGlobals.Count);
            for (var h = 0; h < hubGlobals.Count; h++)
            {
                var hub = hubGlobals[h];
                candidates.Add((Vector2.Distance(nodes[branchIndex], nodes[hub]), hub));
            }

            candidates.Sort((a, b) => a.Dist.CompareTo(b.Dist));

            for (var c = 0; c < candidates.Count; c++)
            {
                var hub = candidates[c].Hub;
                var path = new List<Vector2>();
                if (!RoadPathfinder.TryFindPath(
                        grid, costAtWorld, nodes[branchIndex], nodes[hub], path, out _, heightMetersAtWorld))
                {
                    continue;
                }

                segment = new RoadSegment(branchIndex, hub, path, RoadTier.Branch);
                return true;
            }

            return false;
        }

        private static List<RoadSegment> BuildMst(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            RoadTier tier)
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
                if (RoadPathfinder.TryFindPath(grid, costAtWorld, nodes[a], nodes[b], path, out var cost, heightMetersAtWorld))
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
                segments.Add(new RoadSegment(edge.A, edge.B, edge.Path, tier));
            }

            RepairDisconnectedComponents(nodes, grid, costAtWorld, heightMetersAtWorld, union, segments, tier);

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
            RoadTier tier)
        {
            while (TryMergeOneComponent(nodes, grid, costAtWorld, heightMetersAtWorld, union, segments, tier))
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
            RoadTier tier)
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
                        grid, costAtWorld, nodes[candidate.A], nodes[candidate.B], path, out _, heightMetersAtWorld))
                {
                    continue;
                }

                union.Union(candidate.A, candidate.B);
                segments.Add(new RoadSegment(candidate.A, candidate.B, path, tier));
                return true;
            }

            return false;
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
