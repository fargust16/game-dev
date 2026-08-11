using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Grid-based A* over a continuous world-space cost function (see <see cref="RoadCost"/>).
    /// Deliberately generic over the cost query (a plain delegate, not <see cref="RoadCostSampler"/>
    /// directly) so tests can drive it with a synthetic cost field instead of a live MapMagic
    /// terrain.
    /// </summary>
    public static class RoadPathfinder
    {
        private static readonly (int Dx, int Dz)[] Neighbors =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        public static bool TryFindPath(
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Vector2 startWorld,
            Vector2 goalWorld,
            List<Vector2> pathOut)
        {
            return TryFindPath(grid, costAtWorld, startWorld, goalWorld, pathOut, out _);
        }

        /// <param name="heightMetersAtWorld">Optional real-world elevation (metres, not the 0-1
        /// biome-sampler channel) at a world XZ point. When supplied, each ordinary step's cost is
        /// additionally scaled by <see cref="RoadCost.GradeMultiplier"/> using the real rise-over-run
        /// between the two cells - <see cref="RoadCost.Evaluate"/>'s slope01 term alone can't
        /// distinguish "walking along a mountainside's contour" from "walking straight up it" (both
        /// read equally steep as a per-point value), so without this a road still takes the
        /// direct, near-vertical route up any climb. Omit (null) to skip grade costing entirely
        /// (e.g. in tests that don't care about switchback behaviour).</param>
        public static bool TryFindPath(
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            Vector2 startWorld,
            Vector2 goalWorld,
            List<Vector2> pathOut,
            out float totalCost,
            Func<float, float, float> heightMetersAtWorld = null)
        {
            pathOut.Clear();
            totalCost = 0f;

            if (!grid.TryWorldToCell(startWorld.x, startWorld.y, out var startX, out var startZ) ||
                !grid.TryWorldToCell(goalWorld.x, goalWorld.y, out var goalX, out var goalZ))
            {
                return false;
            }

            var count = grid.CellCount;
            var gScore = new float[count];
            var cameFrom = new int[count];
            var visited = new bool[count];
            for (var i = 0; i < count; i++)
            {
                gScore[i] = float.PositiveInfinity;
                cameFrom[i] = -1;
            }

            var startIndex = grid.Index(startX, startZ);
            var goalIndex = grid.Index(goalX, goalZ);
            gScore[startIndex] = 0f;

            var open = new BinaryHeap(count);
            open.Push(startIndex, Heuristic(grid, startX, startZ, goalX, goalZ));

            var found = false;
            while (open.Count > 0)
            {
                var currentIndex = open.Pop();
                if (visited[currentIndex])
                {
                    continue;
                }

                visited[currentIndex] = true;

                if (currentIndex == goalIndex)
                {
                    found = true;
                    break;
                }

                var cx = currentIndex % grid.Width;
                var cz = currentIndex / grid.Width;
                var currentCenter = grid.CellCenter(cx, cz);
                var currentCost = costAtWorld(currentCenter.x, currentCenter.y);
                if (float.IsPositiveInfinity(currentCost))
                {
                    continue;
                }

                foreach (var (dx, dz) in Neighbors)
                {
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (nx < 0 || nx >= grid.Width || nz < 0 || nz >= grid.Height)
                    {
                        continue;
                    }

                    var neighborIndex = grid.Index(nx, nz);
                    if (visited[neighborIndex])
                    {
                        continue;
                    }

                    var neighborCenter = grid.CellCenter(nx, nz);
                    var neighborCost = costAtWorld(neighborCenter.x, neighborCenter.y);
                    if (float.IsPositiveInfinity(neighborCost))
                    {
                        // The immediate step is blocked (submerged) - see if a short bridge can
                        // skip over it to the nearest dry landing in the same direction, instead of
                        // giving up on this direction entirely.
                        TryBridge(grid, costAtWorld, cx, cz, dx, dz, currentIndex, currentCenter,
                            goalX, goalZ, gScore, cameFrom, visited, open);
                        continue;
                    }

                    var stepDistance = dx != 0 && dz != 0 ? grid.CellSize * 1.41421356f : grid.CellSize;
                    var stepCost = stepDistance * 0.5f * (currentCost + neighborCost);

                    if (heightMetersAtWorld != null)
                    {
                        var currentHeight = heightMetersAtWorld(currentCenter.x, currentCenter.y);
                        var neighborHeight = heightMetersAtWorld(neighborCenter.x, neighborCenter.y);
                        var grade = (neighborHeight - currentHeight) / stepDistance;
                        stepCost *= RoadCost.GradeMultiplier(grade);
                    }

                    var tentative = gScore[currentIndex] + stepCost;
                    if (tentative >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    gScore[neighborIndex] = tentative;
                    cameFrom[neighborIndex] = currentIndex;
                    open.Push(neighborIndex, tentative + Heuristic(grid, nx, nz, goalX, goalZ));
                }
            }

            if (!found)
            {
                return false;
            }

            totalCost = gScore[goalIndex];

            var reversed = new List<Vector2>();
            var idx = goalIndex;
            while (idx >= 0)
            {
                var cx = idx % grid.Width;
                var cz = idx / grid.Width;
                reversed.Add(grid.CellCenter(cx, cz));
                idx = cameFrom[idx];
            }

            reversed.Reverse();
            pathOut.AddRange(reversed);
            return true;
        }

        // Admissible: BaseCost=1 (flat, unblocked ground) is the cheapest possible per-meter cost
        // (see RoadCost - every other biome/slope combination, including bridging, costs strictly
        // more), so plain Euclidean distance never overestimates the true remaining cost.
        private static float Heuristic(RoadGridSpec grid, int ax, int az, int bx, int bz)
        {
            var dx = (ax - bx) * grid.CellSize;
            var dz = (az - bz) * grid.CellSize;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        // Scans cell-by-cell from (cx,cz) in direction (dx,dz) - skipping the already-known-
        // submerged immediate neighbor (span=1) - looking for the first dry landing within
        // RoadCost.MaxBridgeSpanMeters. If found, relaxes a single long edge straight to that
        // landing, priced by real distance * RoadCost.BridgeCostPerMeter - independent of the
        // (irrelevant, underwater) per-cell cost of whatever lies between, since a bridge's cost is
        // about span length, not what's beneath it.
        private static void TryBridge(
            RoadGridSpec grid,
            Func<float, float, float> costAtWorld,
            int cx, int cz, int dx, int dz,
            int currentIndex, Vector2 currentCenter,
            int goalX, int goalZ,
            float[] gScore, int[] cameFrom, bool[] visited, BinaryHeap open)
        {
            for (var span = 2; ; span++)
            {
                var fx = cx + dx * span;
                var fz = cz + dz * span;
                if (fx < 0 || fx >= grid.Width || fz < 0 || fz >= grid.Height)
                {
                    return; // ran off the grid before finding dry land - no bridge here
                }

                var farCenter = grid.CellCenter(fx, fz);
                var bridgeDistance = Vector2.Distance(currentCenter, farCenter);
                if (bridgeDistance > RoadCost.MaxBridgeSpanMeters)
                {
                    return; // too far to bridge - let the pathfinder go around instead
                }

                var farIndex = grid.Index(fx, fz);
                if (visited[farIndex])
                {
                    return; // already settled via a cheaper route - no point bridging to it
                }

                var farCost = costAtWorld(farCenter.x, farCenter.y);
                if (float.IsPositiveInfinity(farCost))
                {
                    continue; // still over water, keep extending the span
                }

                var bridgeCost = bridgeDistance * RoadCost.BridgeCostPerMeter;
                var tentative = gScore[currentIndex] + bridgeCost;
                if (tentative < gScore[farIndex])
                {
                    gScore[farIndex] = tentative;
                    cameFrom[farIndex] = currentIndex;
                    open.Push(farIndex, tentative + Heuristic(grid, fx, fz, goalX, goalZ));
                }

                return; // found the first dry landing - stop scanning, a farther one only costs more
            }
        }

        private sealed class BinaryHeap
        {
            private readonly List<(float Priority, int Value)> items;

            public BinaryHeap(int capacityHint)
            {
                items = new List<(float Priority, int Value)>(Mathf.Max(16, capacityHint / 4));
            }

            public int Count => items.Count;

            public void Push(int value, float priority)
            {
                items.Add((priority, value));
                var i = items.Count - 1;
                while (i > 0)
                {
                    var parent = (i - 1) / 2;
                    if (items[parent].Priority <= items[i].Priority)
                    {
                        break;
                    }

                    (items[parent], items[i]) = (items[i], items[parent]);
                    i = parent;
                }
            }

            public int Pop()
            {
                var root = items[0];
                var last = items.Count - 1;
                items[0] = items[last];
                items.RemoveAt(last);

                var i = 0;
                while (true)
                {
                    var left = i * 2 + 1;
                    var right = i * 2 + 2;
                    var smallest = i;
                    if (left < items.Count && items[left].Priority < items[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right < items.Count && items[right].Priority < items[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest == i)
                    {
                        break;
                    }

                    (items[i], items[smallest]) = (items[smallest], items[i]);
                    i = smallest;
                }

                return root.Value;
            }
        }
    }
}
