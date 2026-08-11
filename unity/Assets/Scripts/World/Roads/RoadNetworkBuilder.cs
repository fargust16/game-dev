using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    public readonly struct RoadSegment
    {
        public int NodeA { get; }
        public int NodeB { get; }
        public IReadOnlyList<Vector2> Points { get; }

        public RoadSegment(int nodeA, int nodeB, IReadOnlyList<Vector2> points)
        {
            NodeA = nodeA;
            NodeB = nodeB;
            Points = points;
        }
    }

    /// <summary>
    /// Connects a set of world-space nodes (POI markers) into a minimum-spanning-tree road graph -
    /// every node reachable, no redundant loops. Built for a demo scenario with dozens of POIs
    /// (WORLD_GENERATION_PIPELINE.md Iteration 08's PoiDemoScenario) and expected to keep growing,
    /// so this deliberately avoids an all-pairs A* search (an earlier version ran real pathfinding
    /// between every node pair - fine for ~5 nodes, but O(n^2) real grid searches stops being cheap
    /// well before node counts reach the dozens this now needs to handle). Instead: build a sparse
    /// k-nearest-neighbour candidate graph (O(n^2) in plain Euclidean distance, which is nearly
    /// free - not O(n^2) in expensive A* searches), route only those candidate edges, then run
    /// Kruskal's MST over the real routed costs. A repair pass handles the rare case where the KNN
    /// candidates leave the graph in more than one connected component (e.g. a POI whose nearest
    /// neighbours by straight-line distance all happen to be across a lake).
    /// </summary>
    public static class RoadNetworkBuilder
    {
        // Average node degree in the candidate graph before MST pruning - large enough that a
        // spanning tree almost always exists in the candidate set alone (the repair pass covers the
        // rest), small enough that the number of real A* searches stays linear in node count.
        private const int NeighborCandidateCount = 6;

        // Safety cap on cross-component bridge attempts per repair round, so a map with many
        // separate landmasses can't blow up into an all-pairs search by another name.
        private const int MaxRepairAttemptsPerRound = 20;

        /// <param name="heightMetersAtWorld">Forwarded to <see cref="RoadPathfinder.TryFindPath"/> -
        /// real elevation (metres) for grade costing, so climbs prefer a longer traverse over a
        /// short direct ascent. Optional (null skips grade costing entirely).</param>
        public static List<RoadSegment> BuildNetwork(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld = null)
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
                    continue; // would close a cycle - MST keeps only the cheaper edge already added
                }

                union.Union(edge.A, edge.B);
                segments.Add(new RoadSegment(edge.A, edge.B, edge.Path));
            }

            RepairDisconnectedComponents(nodes, grid, costAtWorld, heightMetersAtWorld, union, segments);

            return segments;
        }

        // KNN by plain Euclidean distance - O(n^2) comparisons, but each is just a subtraction and
        // a square root, not a grid search, so this stays cheap even for hundreds of nodes.
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

        // Merges any components the KNN candidate graph left disconnected, by trying real routes
        // between the geometrically closest cross-component node pairs first. Runs one merge per
        // round (regrouping after each success, since a merge can change which pair is closest) and
        // stops once everything is connected or no candidate pair yields a route.
        private static void RepairDisconnectedComponents(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            UnionFind union,
            List<RoadSegment> segments)
        {
            while (TryMergeOneComponent(nodes, grid, costAtWorld, heightMetersAtWorld, union, segments))
            {
            }
        }

        private static bool TryMergeOneComponent(
            IReadOnlyList<Vector2> nodes,
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Func<float, float, float> heightMetersAtWorld,
            UnionFind union,
            List<RoadSegment> segments)
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
                if (!RoadPathfinder.TryFindPath(grid, costAtWorld, nodes[candidate.A], nodes[candidate.B], path, out _, heightMetersAtWorld))
                {
                    continue;
                }

                union.Union(candidate.A, candidate.B);
                segments.Add(new RoadSegment(candidate.A, candidate.B, path));
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
