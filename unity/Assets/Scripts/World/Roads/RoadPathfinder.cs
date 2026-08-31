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
            Func<float, float, float> heightMetersAtWorld = null,
            Func<Vector2, Vector2, bool> canBridgeSpan = null,
            float minCostPerMetre = 1f,
            RoadSearchLandmarks landmarks = null)
        {
            pathOut.Clear();
            totalCost = 0f;

            // A caller that knows its cost function can go below 1 per metre says so; anything else
            // keeps the plain-distance heuristic. Clamped because a value above 1 would overestimate
            // (the defect this parameter exists to fix) and a negative one is meaningless.
            minCostPerMetre = Mathf.Clamp(minCostPerMetre, 0f, 1f);

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
            open.Push(startIndex, Heuristic(grid, startX, startZ, goalX, goalZ, minCostPerMetre, landmarks));

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
                        TryBridge(grid, costAtWorld, canBridgeSpan, cx, cz, dx, dz, currentIndex, currentCenter,
                            goalX, goalZ, gScore, cameFrom, visited, open, minCostPerMetre, landmarks);
                        continue;
                    }

                    var stepCost = StepCost(
                        grid, dx, dz, currentCost, neighborCost, currentCenter, neighborCenter,
                        heightMetersAtWorld);

                    var tentative = gScore[currentIndex] + stepCost;
                    if (tentative >= gScore[neighborIndex])
                    {
                        continue;
                    }

                    gScore[neighborIndex] = tentative;
                    cameFrom[neighborIndex] = currentIndex;
                    open.Push(neighborIndex, tentative + Heuristic(grid, nx, nz, goalX, goalZ, minCostPerMetre, landmarks));
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

        /// <summary>Cost of one grid step, and the single place that formula lives.</summary>
        /// <remarks>
        /// <see cref="RoadSearchLandmarks"/> measures its distance tables with this same method, so
        /// the tables and the search can never drift onto two different cost models.
        /// </remarks>
        public static float StepCost(
            RoadGridSpec grid,
            int dx,
            int dz,
            float currentCost,
            float neighborCost,
            Vector2 currentCenter,
            Vector2 neighborCenter,
            Func<float, float, float> heightMetersAtWorld)
        {
            var stepDistance = dx != 0 && dz != 0 ? grid.CellSize * 1.41421356f : grid.CellSize;
            var stepCost = stepDistance * 0.5f * (currentCost + neighborCost);
            if (heightMetersAtWorld == null)
            {
                return stepCost;
            }

            var currentHeight = heightMetersAtWorld(currentCenter.x, currentCenter.y);
            var neighborHeight = heightMetersAtWorld(neighborCenter.x, neighborCenter.y);
            var grade = (neighborHeight - currentHeight) / stepDistance;
            return stepCost * RoadCost.GradeMultiplier(grade);
        }

        // Euclidean distance priced at the cheapest metre the caller's cost function can produce.
        //
        // The scale is not decoration. A* is only guaranteed to return the cheapest route when the
        // heuristic never overestimates the remaining cost, and distance in metres only satisfies
        // that while no metre costs less than 1. LandscapeCostField breaks exactly that: within
        // ExistingCorridorHalfWidth of an accepted road it multiplies the (>= 1) terrain cost by
        // ExistingCorridorDiscount, which production sets to 0.15. Unscaled, the heuristic then
        // overprices any route that would reuse a corridor, and the search settles for the straight
        // line instead - measured at up to 46 % above the true optimum on a painted field, and it
        // defeated the corridor reuse the discount exists to create.
        //
        // Scaling by that same floor restores both properties. Admissible: every metre costs at
        // least minCostPerMetre, so minCostPerMetre * distance can never exceed what remains.
        // Consistent: for one step, h(a) - h(b) <= minCostPerMetre * |a - b| <= the step's cost,
        // because per-point cost >= minCostPerMetre and GradeMultiplier only ever multiplies up.
        // Consistency is the part that matters here - it is why the closed set below stays sound
        // and no node ever needs reopening.
        private static float Heuristic(
            RoadGridSpec grid, int ax, int az, int bx, int bz, float minCostPerMetre,
            RoadSearchLandmarks landmarks)
        {
            var dx = (ax - bx) * grid.CellSize;
            var dz = (az - bz) * grid.CellSize;
            var distanceBound = Mathf.Sqrt(dx * dx + dz * dz) * minCostPerMetre;
            if (landmarks == null)
            {
                return distanceBound;
            }

            // Both bounds are admissible, so the larger of the two is the better admissible one.
            var landmarkBound = landmarks.LowerBound(grid.Index(ax, az), grid.Index(bx, bz));
            return landmarkBound > distanceBound ? landmarkBound : distanceBound;
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
            Func<Vector2, Vector2, bool> canBridgeSpan,
            int cx, int cz, int dx, int dz,
            int currentIndex, Vector2 currentCenter,
            int goalX, int goalZ,
            float[] gScore, int[] cameFrom, bool[] visited, BinaryHeap open,
            float minCostPerMetre,
            RoadSearchLandmarks landmarks)
        {
            for (var span = 2; ; span++)
            {
                var fx = cx + dx * span;
                var fz = cz + dz * span;
                if (fx < 0 || fx >= grid.Width || fz < 0 || fz >= grid.Height)
                {
                    return;
                }

                var farCenter = grid.CellCenter(fx, fz);
                var bridgeDistance = Vector2.Distance(currentCenter, farCenter);
                if (bridgeDistance > RoadCost.MaxBridgeSpanMeters)
                {
                    return;
                }

                var farIndex = grid.Index(fx, fz);
                if (visited[farIndex])
                {
                    return;
                }

                var farCost = costAtWorld(farCenter.x, farCenter.y);
                if (float.IsPositiveInfinity(farCost))
                {
                    continue;
                }

                if (canBridgeSpan != null && !canBridgeSpan(currentCenter, farCenter))
                {
                    // Landing is dry but span crosses a lake (or other non-bridgeable water) — skip.
                    return;
                }

                var bridgeCost = bridgeDistance * RoadCost.BridgeCostPerMeter;
                var tentative = gScore[currentIndex] + bridgeCost;
                if (tentative < gScore[farIndex])
                {
                    gScore[farIndex] = tentative;
                    cameFrom[farIndex] = currentIndex;
                    open.Push(farIndex, tentative + Heuristic(grid, fx, fz, goalX, goalZ, minCostPerMetre, landmarks));
                }

                return;
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
