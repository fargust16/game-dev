using System;
using System.Collections.Generic;
using GameDev.World.Landscape;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// ALT lower bounds for <see cref="RoadPathfinder"/>: shortest-path distances from a handful of
    /// fixed landmark cells, turned into an admissible estimate by the triangle inequality.
    /// </summary>
    /// <remarks>
    /// Why this exists. A* only returns the cheapest route while its heuristic never overestimates,
    /// and straight-line distance stops qualifying the moment <see cref="LandscapeCostField"/>
    /// discounts an existing corridor below one per metre. Scaling the distance by that discount
    /// restores admissibility but throws away most of the heuristic's strength: on the real terrain
    /// only 1 % of cells cost less than 1 per metre, yet every estimate is divided by 6.67 to
    /// accommodate them, and the search expands half again as many nodes as it used to.
    /// <para>
    /// Landmarks give the strength back. For any landmark L, |d(L,n) - d(L,goal)| never exceeds
    /// d(n,goal) - that is the triangle inequality, and it holds because the edge cost is symmetric
    /// (the per-point terms are averaged, and <c>RoadCost.GradeMultiplier</c> takes the absolute
    /// grade). The maximum over several landmarks is still a lower bound, and each term is monotone,
    /// so the closed set in the search stays sound and no node needs reopening.
    /// </para>
    /// <para>
    /// The tables measure a specific <see cref="LandscapeCostField.Version"/>. Painting a corridor
    /// only makes cells cheaper, which shrinks real distances while the stored ones stay put - a
    /// stale table would quietly start overestimating. <see cref="EnsureFresh"/> is the guard, and
    /// every search path in <c>RoadNetworkBuilder</c> goes through it.
    /// </para>
    /// </remarks>
    public sealed class RoadSearchLandmarks
    {
        /// <summary>
        /// Four is where farthest-point placement starts paying: measured on the real terrain, two
        /// landmarks leave the search at 0.25x the scalar bound's expansions and four take it to
        /// 0.09x, which more than covers the extra table each rebuild has to fill.
        /// </summary>
        public const int LandmarkCount = 4;

        /// <summary>
        /// Relative slack subtracted from the bound, covering the error the search itself
        /// accumulates: <c>gScore</c> adds up in <c>float</c>, so over a path of N steps it drifts
        /// by roughly N*2^-24 - about 7e-4 at this grid's worst case of 12 100 cells. Shrinking the
        /// bound by 1e-3 stays under that with room to spare and costs nothing measurable.
        /// </summary>
        private const double AccumulationSlack = 1e-3;

        /// <summary>Absolute slack for the double rounding in the tables themselves.</summary>
        private const double TableSlack = 1e-12;

        private static readonly (int Dx, int Dz)[] Neighbors =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        private readonly RoadGridSpec grid;
        private readonly LandscapeCostField costField;
        private readonly Func<float, float, float> costAtWorld;
        private readonly Func<float, float, float> heightMetersAtWorld;
        private readonly double[][] tables;
        private int[] landmarkCells;
        private int tableVersion = int.MinValue;

        public RoadSearchLandmarks(
            RoadGridSpec grid,
            LandscapeCostField costField,
            Func<float, float, float> heightMetersAtWorld)
        {
            this.grid = grid;
            this.costField = costField ?? throw new ArgumentNullException(nameof(costField));
            this.heightMetersAtWorld = heightMetersAtWorld;
            costAtWorld = (x, z) => costField.EvaluateAtTerrain(x, z);
            tables = new double[LandmarkCount][];
        }

        /// <summary>Landmark cell indices, in selection order. Empty until the first build.</summary>
        public IReadOnlyList<int> LandmarkCells => landmarkCells ?? Array.Empty<int>();

        /// <summary>How many times the tables have been rebuilt.</summary>
        public int RebuildCount { get; private set; }

        /// <summary>The cost-field version the current tables describe.</summary>
        public int TableVersion => tableVersion;

        /// <summary>
        /// Brings the tables up to the cost field's current version, rebuilding them if it moved.
        /// Must be called before any search that passes this object to
        /// <see cref="RoadPathfinder.TryFindPath"/>.
        /// </summary>
        public void EnsureFresh()
        {
            if (tableVersion == costField.Version && landmarkCells != null)
            {
                return;
            }

            // Positions are chosen once, against whatever the field looked like the first time this
            // ran, and then left alone. Any cell is a valid landmark - moving them later would only
            // change which valid bound you get, at the price of an extra sweep per repaint.
            landmarkCells ??= SelectLandmarks();

            for (var i = 0; i < landmarkCells.Length; i++)
            {
                tables[i] = Distances(landmarkCells[i]);
            }

            tableVersion = costField.Version;
            RebuildCount++;
        }

        /// <summary>
        /// A lower bound on the cheapest route from <paramref name="fromCell"/> to
        /// <paramref name="goalCell"/>, or 0 when nothing is known.
        /// </summary>
        /// <remarks>
        /// Reads the goal only as a cell index, which is what lets <c>TryAttachToNetwork</c> keep
        /// caching search results by goal cell.
        /// </remarks>
        public float LowerBound(int fromCell, int goalCell)
        {
            if (tableVersion != costField.Version)
            {
                // Stale tables are not merely weaker, they can overestimate. Fall back to nothing
                // rather than hand the search a heuristic it is not allowed to trust.
                return 0f;
            }

            var best = 0.0;
            for (var i = 0; i < tables.Length; i++)
            {
                var table = tables[i];
                if (table == null)
                {
                    continue;
                }

                var fromDistance = table[fromCell];
                var goalDistance = table[goalCell];
                if (double.IsPositiveInfinity(fromDistance) || double.IsPositiveInfinity(goalDistance))
                {
                    // One of them cannot reach this landmark; that says nothing usable here.
                    continue;
                }

                var bound = (Math.Abs(fromDistance - goalDistance)
                             - ((fromDistance + goalDistance) * TableSlack))
                            * (1.0 - AccumulationSlack);
                if (bound > best)
                {
                    best = bound;
                }
            }

            return (float)best;
        }

        /// <summary>
        /// Greedy farthest-point placement: start at the grid centre, take the cell furthest from
        /// it, then repeatedly take the cell furthest from everything picked so far.
        /// </summary>
        /// <remarks>
        /// Deterministic end to end - a fixed seed cell, and ties resolved by the lowest cell index
        /// because the comparison below is strict. Four corner cells were the obvious alternative
        /// and measured 27 % more expansions on the real terrain, so the extra sweeps pay for
        /// themselves.
        /// </remarks>
        private int[] SelectLandmarks()
        {
            var picked = new int[LandmarkCount];
            var seed = grid.Index(grid.Width / 2, grid.Height / 2);
            var fromSeed = Distances(seed);
            picked[0] = FurthestFinite(fromSeed);

            var nearest = Distances(picked[0]);
            for (var i = 1; i < LandmarkCount; i++)
            {
                picked[i] = FurthestFinite(nearest);
                if (i == LandmarkCount - 1)
                {
                    break;
                }

                var latest = Distances(picked[i]);
                for (var c = 0; c < nearest.Length; c++)
                {
                    if (double.IsPositiveInfinity(latest[c]))
                    {
                        continue;
                    }

                    if (double.IsPositiveInfinity(nearest[c]) || latest[c] < nearest[c])
                    {
                        nearest[c] = latest[c];
                    }
                }
            }

            return picked;
        }

        /// <summary>Lowest-indexed cell at the greatest finite distance; 0 when none are reachable.</summary>
        private static int FurthestFinite(double[] distances)
        {
            var best = 0;
            var bestDistance = -1.0;
            for (var i = 0; i < distances.Length; i++)
            {
                if (double.IsPositiveInfinity(distances[i]))
                {
                    continue;
                }

                if (distances[i] > bestDistance)
                {
                    bestDistance = distances[i];
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// Dijkstra from one cell over the same graph and the same edge cost the search walks -
        /// <see cref="RoadPathfinder.StepCost"/> is shared rather than restated.
        /// </summary>
        private double[] Distances(int source)
        {
            var count = grid.CellCount;
            var distance = new double[count];
            var settled = new bool[count];
            for (var i = 0; i < count; i++)
            {
                distance[i] = double.PositiveInfinity;
            }

            distance[source] = 0.0;
            var open = new MinHeap(count);
            open.Push(source, 0.0);

            while (open.Count > 0)
            {
                var current = open.Pop();
                if (settled[current])
                {
                    continue;
                }

                settled[current] = true;

                var cx = current % grid.Width;
                var cz = current / grid.Width;
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

                    var neighbor = grid.Index(nx, nz);
                    if (settled[neighbor])
                    {
                        continue;
                    }

                    var neighborCenter = grid.CellCenter(nx, nz);
                    var neighborCost = costAtWorld(neighborCenter.x, neighborCenter.y);
                    if (float.IsPositiveInfinity(neighborCost))
                    {
                        continue;
                    }

                    var step = RoadPathfinder.StepCost(
                        grid, dx, dz, currentCost, neighborCost, currentCenter, neighborCenter,
                        heightMetersAtWorld);
                    if (float.IsPositiveInfinity(step))
                    {
                        continue;
                    }

                    var tentative = distance[current] + step;
                    if (tentative >= distance[neighbor])
                    {
                        continue;
                    }

                    distance[neighbor] = tentative;
                    open.Push(neighbor, tentative);
                }
            }

            return distance;
        }

        private sealed class MinHeap
        {
            private readonly List<(double Priority, int Value)> items;

            public MinHeap(int capacityHint)
            {
                items = new List<(double, int)>(Mathf.Max(16, capacityHint / 4));
            }

            public int Count => items.Count;

            public void Push(int value, double priority)
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
                    var left = (i * 2) + 1;
                    var right = (i * 2) + 2;
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
