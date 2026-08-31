using System;
using System.Collections.Generic;
using GameDev.World.Landscape;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    /// <summary>
    /// Pins the heuristic's lower-bound contract: A* is only optimal while the heuristic never
    /// prices the remaining distance above what it can actually cost, and
    /// <see cref="LandscapeCostField"/>'s existing-corridor discount takes the cheapest metre below
    /// the 1.0 that plain Euclidean distance assumes.
    /// </summary>
    public sealed class RoadPathfinderHeuristicTests
    {
        private const float CellSize = 25f;

        /// <summary>Dead-flat, uniform world: the terrain term of Evaluate is exactly 1.</summary>
        private static LandscapeCostField FlatField(int width, int height, float discount)
        {
            var field = new LandscapeCostField(
                0f, 0f, width * CellSize, height * CellSize, 10f,
                (x, z) => 100f,
                (x, z) => BiomeId.Meadow);
            field.ExistingCorridorDiscount = discount;
            return field;
        }

        private static RoadGridSpec MakeGrid(int width, int height) =>
            new RoadGridSpec(0f, 0f, width, height, CellSize);

        /// <summary>
        /// Exact shortest path over the same 8-neighbour graph, expanded by g alone. Every edge cost
        /// is strictly positive, so this is an oracle regardless of any heuristic.
        /// </summary>
        private static bool ExactCost(
            RoadGridSpec grid, Func<float, float, float> costAtWorld, Vector2 start, Vector2 goal,
            out float totalCost)
        {
            totalCost = 0f;
            if (!grid.TryWorldToCell(start.x, start.y, out var sx, out var sz) ||
                !grid.TryWorldToCell(goal.x, goal.y, out var gx, out var gz))
            {
                return false;
            }

            var n = grid.CellCount;
            var g = new float[n];
            var done = new bool[n];
            for (var i = 0; i < n; i++)
            {
                g[i] = float.PositiveInfinity;
            }

            var startIndex = grid.Index(sx, sz);
            var goalIndex = grid.Index(gx, gz);
            g[startIndex] = 0f;
            var open = new SortedSet<(float Key, int Index)>();
            open.Add((0f, startIndex));

            var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                var ci = cur.Index;
                if (done[ci])
                {
                    continue;
                }

                done[ci] = true;
                if (ci == goalIndex)
                {
                    break;
                }

                var cx = ci % grid.Width;
                var cz = ci / grid.Width;
                var cc = grid.CellCenter(cx, cz);
                var currentCost = costAtWorld(cc.x, cc.y);
                if (float.IsPositiveInfinity(currentCost))
                {
                    continue;
                }

                foreach (var (dx, dz) in steps)
                {
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (nx < 0 || nx >= grid.Width || nz < 0 || nz >= grid.Height)
                    {
                        continue;
                    }

                    var ni = grid.Index(nx, nz);
                    if (done[ni])
                    {
                        continue;
                    }

                    var nc = grid.CellCenter(nx, nz);
                    var neighborCost = costAtWorld(nc.x, nc.y);
                    if (float.IsPositiveInfinity(neighborCost))
                    {
                        continue;
                    }

                    var stepDistance = dx != 0 && dz != 0 ? grid.CellSize * 1.41421356f : grid.CellSize;
                    var tentative = g[ci] + (stepDistance * 0.5f * (currentCost + neighborCost));
                    if (tentative >= g[ni])
                    {
                        continue;
                    }

                    if (!float.IsPositiveInfinity(g[ni]))
                    {
                        open.Remove((g[ni], ni));
                    }

                    g[ni] = tentative;
                    open.Add((tentative, ni));
                }
            }

            if (float.IsPositiveInfinity(g[goalIndex]))
            {
                return false;
            }

            totalCost = g[goalIndex];
            return true;
        }

        /// <summary>A cheap detour: down the west side, along the south, back up the east.</summary>
        private static void PaintDetour(LandscapeCostField field, RoadGridSpec grid, int w, int h)
        {
            field.PaintExistingRoad(new List<Vector2>
            {
                grid.CellCenter(4, h - 10),
                grid.CellCenter(4, 6),
                grid.CellCenter(w - 5, 6),
                grid.CellCenter(w - 5, h - 10),
            });
        }

        [Test]
        public void Heuristic_WithoutCorridor_MatchesPlainDistanceBehaviour()
        {
            const int w = 60;
            const int h = 40;
            var grid = MakeGrid(w, h);
            var field = FlatField(w, h, 0.15f);
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

            var scaled = new List<Vector2>();
            var plain = new List<Vector2>();
            var start = grid.CellCenter(4, 20);
            var goal = grid.CellCenter(w - 5, 20);

            // minCostPerMetre = 1 is the historical heuristic; nothing on an unpainted field can
            // cost less than 1 per metre, so the two must agree exactly.
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, plain, out var plainCost, null, null, 1f));
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, scaled, out var scaledCost, null, null, 0.15f));

            Assert.AreEqual(plainCost, scaledCost, 1e-3f);
            Assert.AreEqual(plain.Count, scaled.Count);
            Assert.IsTrue(ExactCost(grid, cost, start, goal, out var optimal));
            Assert.AreEqual(optimal, plainCost, optimal * 1e-4f, "no discount means the plain heuristic is already admissible");
        }

        [Test]
        public void Heuristic_ScaledByDiscount_MatchesExactOptimumAcrossCorridor()
        {
            const int w = 60;
            const int h = 40;
            var grid = MakeGrid(w, h);
            var field = FlatField(w, h, 0.15f);
            PaintDetour(field, grid, w, h);
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

            var start = grid.CellCenter(4, h - 10);
            var goal = grid.CellCenter(w - 5, h - 10);
            Assert.IsTrue(ExactCost(grid, cost, start, goal, out var optimal));

            var corrected = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, corrected, out var correctedCost, null, null,
                field.ExistingCorridorDiscount));
            Assert.AreEqual(optimal, correctedCost, optimal * 1e-4f,
                "scaled by the cheapest metre the field can charge, the heuristic is admissible and A* is exact");

            // And the defect this guards against: the unscaled heuristic overprices the corridor.
            var unscaled = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, unscaled, out var unscaledCost, null, null, 1f));
            Assert.Greater(unscaledCost, optimal * 1.0001f,
                "the plain-distance heuristic is inadmissible once a corridor is painted");
        }

        [TestCase(0.50f)]
        [TestCase(0.15f)]
        [TestCase(0.05f)]
        public void Heuristic_ScaledByDiscount_IsExactAtEveryClampedDiscount(float discount)
        {
            const int w = 60;
            const int h = 40;
            var grid = MakeGrid(w, h);
            var field = FlatField(w, h, discount);
            PaintDetour(field, grid, w, h);
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

            var start = grid.CellCenter(4, h - 10);
            var goal = grid.CellCenter(w - 5, h - 10);
            Assert.IsTrue(ExactCost(grid, cost, start, goal, out var optimal));

            var path = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, path, out var actual, null, null, discount));
            Assert.AreEqual(optimal, actual, optimal * 1e-4f);
        }

        [Test]
        public void Heuristic_Scaled_DoesNotMakeAnUnreachableGoalReachable()
        {
            // A blocked band wider than RoadCost.MaxBridgeSpanMeters, so nothing can cross it.
            var grid = new RoadGridSpec(0f, 0f, 40, 10, 25f);
            float Cost(float x, float z) => x > 200f && x < 700f ? float.PositiveInfinity : 1f;

            var plain = new List<Vector2>();
            var scaled = new List<Vector2>();
            var start = new Vector2(12.5f, 125f);
            var goal = new Vector2(987.5f, 125f);

            Assert.IsFalse(RoadPathfinder.TryFindPath(grid, Cost, start, goal, plain, out _, null, null, 1f));
            Assert.IsFalse(RoadPathfinder.TryFindPath(grid, Cost, start, goal, scaled, out _, null, null, 0.05f));
        }

        [Test]
        public void Heuristic_Scaled_IsDeterministicAcrossRepeatedRuns()
        {
            const int w = 60;
            const int h = 40;
            var grid = MakeGrid(w, h);
            var field = FlatField(w, h, 0.15f);
            PaintDetour(field, grid, w, h);
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);
            var start = grid.CellCenter(4, h - 10);
            var goal = grid.CellCenter(w - 5, h - 10);

            var first = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, first, out var firstCost, null, null, 0.15f));

            for (var run = 0; run < 2; run++)
            {
                var again = new List<Vector2>();
                Assert.IsTrue(RoadPathfinder.TryFindPath(
                    grid, cost, start, goal, again, out var againCost, null, null, 0.15f));
                Assert.AreEqual(
                    BitConverter.SingleToInt32Bits(firstCost),
                    BitConverter.SingleToInt32Bits(againCost));
                Assert.AreEqual(first.Count, again.Count);
                for (var i = 0; i < first.Count; i++)
                {
                    Assert.AreEqual(BitConverter.SingleToInt32Bits(first[i].x), BitConverter.SingleToInt32Bits(again[i].x));
                    Assert.AreEqual(BitConverter.SingleToInt32Bits(first[i].y), BitConverter.SingleToInt32Bits(again[i].y));
                }
            }
        }
    }
}
