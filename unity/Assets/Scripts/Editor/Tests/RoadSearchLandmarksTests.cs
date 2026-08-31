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
    /// Guards the contract <see cref="RoadSearchLandmarks"/> exists to keep: the bound never
    /// overestimates, never rises faster than an edge costs, and never survives a repaint.
    /// </summary>
    public sealed class RoadSearchLandmarksTests
    {
        private const float CellSize = 25f;
        private const int Width = 40;
        private const int Height = 30;

        private static RoadGridSpec MakeGrid() => new RoadGridSpec(0f, 0f, Width, Height, CellSize);

        /// <summary>Rolling terrain plus a discounted corridor - the case the bound is for.</summary>
        private static LandscapeCostField MakeField(float discount, bool paint)
        {
            var field = new LandscapeCostField(
                0f, 0f, Width * CellSize, Height * CellSize, 10f,
                (x, z) => 100f + (25f * Mathf.PerlinNoise(x * 0.01f, z * 0.01f)),
                (x, z) => BiomeId.Meadow);
            field.ExistingCorridorDiscount = discount;
            if (paint)
            {
                var grid = MakeGrid();
                field.PaintExistingRoad(new List<Vector2>
                {
                    grid.CellCenter(3, Height - 4),
                    grid.CellCenter(3, 3),
                    grid.CellCenter(Width - 4, 3),
                });
            }

            return field;
        }

        private static Func<float, float, float> Height10 => (x, z) => 10f;

        /// <summary>Exact cost from every cell to <paramref name="source"/>, by Dijkstra.</summary>
        private static double[] ExactDistances(RoadGridSpec grid, LandscapeCostField field, int source)
        {
            var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
            var n = grid.CellCount;
            var dist = new double[n];
            var settled = new bool[n];
            for (var i = 0; i < n; i++)
            {
                dist[i] = double.PositiveInfinity;
            }

            dist[source] = 0.0;
            var open = new SortedSet<(double Key, int Index)>();
            open.Add((0.0, source));
            while (open.Count > 0)
            {
                var cur = open.Min;
                open.Remove(cur);
                var ci = cur.Index;
                if (settled[ci])
                {
                    continue;
                }

                settled[ci] = true;
                var cx = ci % grid.Width;
                var cz = ci / grid.Width;
                var cc = grid.CellCenter(cx, cz);
                var cCost = field.EvaluateAtTerrain(cc.x, cc.y);
                foreach (var (dx, dz) in steps)
                {
                    var nx = cx + dx;
                    var nz = cz + dz;
                    if (nx < 0 || nx >= grid.Width || nz < 0 || nz >= grid.Height)
                    {
                        continue;
                    }

                    var ni = grid.Index(nx, nz);
                    if (settled[ni])
                    {
                        continue;
                    }

                    var nc = grid.CellCenter(nx, nz);
                    var nCost = field.EvaluateAtTerrain(nc.x, nc.y);
                    var step = RoadPathfinder.StepCost(grid, dx, dz, cCost, nCost, cc, nc, Height10);
                    if (float.IsPositiveInfinity(step))
                    {
                        continue;
                    }

                    var tentative = dist[ci] + step;
                    if (tentative >= dist[ni])
                    {
                        continue;
                    }

                    if (!double.IsPositiveInfinity(dist[ni]))
                    {
                        open.Remove((dist[ni], ni));
                    }

                    dist[ni] = tentative;
                    open.Add((tentative, ni));
                }
            }

            return dist;
        }

        [Test]
        public void LandmarkSelection_IsDeterministic()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: true);
            var first = new RoadSearchLandmarks(grid, field, Height10);
            var second = new RoadSearchLandmarks(grid, field, Height10);
            first.EnsureFresh();
            second.EnsureFresh();

            Assert.AreEqual(RoadSearchLandmarks.LandmarkCount, first.LandmarkCells.Count);
            CollectionAssert.AreEqual(first.LandmarkCells, second.LandmarkCells,
                "landmark placement must not depend on anything but the field");
        }

        [Test]
        public void LowerBound_NeverExceedsTheTrueRemainingCost()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: true);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();

            var goal = grid.Index(grid.Width - 3, grid.Height - 3);
            var exact = ExactDistances(grid, field, goal);
            var checkedCells = 0;
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (double.IsPositiveInfinity(exact[i]))
                {
                    continue;
                }

                checkedCells++;
                Assert.LessOrEqual(landmarks.LowerBound(i, goal), exact[i] * (1f + 1e-4f) + 1e-3f,
                    $"the bound overestimated the true cost from cell {i}");
            }

            Assert.Greater(checkedCells, grid.CellCount / 2, "the sample must actually cover the grid");
        }

        [Test]
        public void LowerBound_NeverRisesFasterThanAnEdgeCosts()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: true);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();

            var goal = grid.Index(2, 2);
            var steps = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
            var checkedEdges = 0;
            for (var cz = 0; cz < grid.Height; cz++)
            {
                for (var cx = 0; cx < grid.Width; cx++)
                {
                    var from = grid.Index(cx, cz);
                    var fromCentre = grid.CellCenter(cx, cz);
                    var fromCost = field.EvaluateAtTerrain(fromCentre.x, fromCentre.y);
                    foreach (var (dx, dz) in steps)
                    {
                        var nx = cx + dx;
                        var nz = cz + dz;
                        if (nx < 0 || nx >= grid.Width || nz < 0 || nz >= grid.Height)
                        {
                            continue;
                        }

                        var to = grid.Index(nx, nz);
                        var toCentre = grid.CellCenter(nx, nz);
                        var toCost = field.EvaluateAtTerrain(toCentre.x, toCentre.y);
                        var step = RoadPathfinder.StepCost(
                            grid, dx, dz, fromCost, toCost, fromCentre, toCentre, Height10);
                        if (float.IsPositiveInfinity(step))
                        {
                            continue;
                        }

                        checkedEdges++;
                        var drop = landmarks.LowerBound(from, goal) - landmarks.LowerBound(to, goal);
                        Assert.LessOrEqual(drop, step * (1f + 1e-4f) + 1e-3f,
                            $"monotonicity broken across {from} -> {to}; the closed set would stop being sound");
                    }
                }
            }

            Assert.Greater(checkedEdges, 1000);
        }

        [Test]
        public void Search_WithLandmarks_ReturnsTheSameCostAsDijkstra()
        {
            foreach (var discount in new[] { 0.05f, 0.15f, 0.50f })
            {
                var grid = MakeGrid();
                var field = MakeField(discount, paint: true);
                var landmarks = new RoadSearchLandmarks(grid, field, Height10);
                landmarks.EnsureFresh();
                Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

                var goalCell = grid.Index(grid.Width - 3, grid.Height - 3);
                var exact = ExactDistances(grid, field, goalCell);
                var goal = grid.CellCenter(grid.Width - 3, grid.Height - 3);

                foreach (var start in new[] { grid.Index(2, 2), grid.Index(2, grid.Height - 3), grid.Index(grid.Width / 2, 4) })
                {
                    var path = new List<Vector2>();
                    var from = grid.CellCenter(start % grid.Width, start / grid.Width);
                    Assert.IsTrue(RoadPathfinder.TryFindPath(
                        grid, cost, from, goal, path, out var found, Height10, null, discount, landmarks));
                    Assert.AreEqual(exact[start], found, exact[start] * 1e-4,
                        $"discount {discount}: the landmark search must still return the cheapest route");
                }
            }
        }

        [Test]
        public void PaintingACorridor_InvalidatesTheTables()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: false);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();

            var versionBefore = landmarks.TableVersion;
            var rebuildsBefore = landmarks.RebuildCount;
            var from = grid.Index(2, 2);
            var goal = grid.Index(grid.Width - 3, grid.Height - 3);
            Assert.Greater(landmarks.LowerBound(from, goal), 0f, "a fresh table should have something to say");

            field.PaintExistingRoad(new List<Vector2>
            {
                grid.CellCenter(3, grid.Height - 4), grid.CellCenter(3, 3),
            });

            Assert.AreNotEqual(versionBefore, field.Version, "painting must move the cost field's version");
            Assert.AreEqual(0f, landmarks.LowerBound(from, goal),
                "a table measured against the old field must refuse to answer, not answer stale");

            landmarks.EnsureFresh();
            Assert.AreEqual(field.Version, landmarks.TableVersion);
            Assert.AreEqual(rebuildsBefore + 1, landmarks.RebuildCount);
            Assert.Greater(landmarks.LowerBound(from, goal), 0f);
        }

        [Test]
        public void SearchAfterPainting_IsStillExact()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: false);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

            field.PaintExistingRoad(new List<Vector2>
            {
                grid.CellCenter(3, grid.Height - 4), grid.CellCenter(3, 3),
                grid.CellCenter(grid.Width - 4, 3),
            });
            landmarks.EnsureFresh();

            var startCell = grid.Index(2, grid.Height - 3);
            var goalCell = grid.Index(grid.Width - 3, grid.Height - 3);
            var exact = ExactDistances(grid, field, goalCell);

            var path = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost,
                grid.CellCenter(startCell % grid.Width, startCell / grid.Width),
                grid.CellCenter(goalCell % grid.Width, goalCell / grid.Width),
                path, out var found, Height10, null, 0.15f, landmarks));
            Assert.AreEqual(exact[startCell], found, exact[startCell] * 1e-4);
        }

        [Test]
        public void LowerBound_ReadsTheGoalOnlyAsACell_SoGoalCellMemoStaysValid()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: true);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);

            var goalCellX = grid.Width - 3;
            var goalCellZ = grid.Height - 3;
            var centre = grid.CellCenter(goalCellX, goalCellZ);
            var start = grid.CellCenter(2, 2);

            float? firstCost = null;
            List<Vector2> firstPath = null;
            foreach (var ox in new[] { -0.45f, 0f, 0.45f })
            {
                foreach (var oz in new[] { -0.45f, 0f, 0.45f })
                {
                    var goal = new Vector2(centre.x + (ox * CellSize), centre.y + (oz * CellSize));
                    Assert.IsTrue(grid.TryWorldToCell(goal.x, goal.y, out var cx, out var cz));
                    Assert.AreEqual(grid.Index(goalCellX, goalCellZ), grid.Index(cx, cz),
                        "the probe must stay inside the intended cell");

                    var path = new List<Vector2>();
                    Assert.IsTrue(RoadPathfinder.TryFindPath(
                        grid, cost, start, goal, path, out var found, Height10, null, 0.15f, landmarks));
                    if (firstCost == null)
                    {
                        firstCost = found;
                        firstPath = path;
                        continue;
                    }

                    Assert.AreEqual(
                        BitConverter.SingleToInt32Bits(firstCost.Value), BitConverter.SingleToInt32Bits(found),
                        "two goals in one cell must give one answer, or caching by goal cell is unsound");
                    Assert.AreEqual(firstPath.Count, path.Count);
                }
            }
        }

        [Test]
        public void RepeatedSearches_AreBitwiseIdentical()
        {
            var grid = MakeGrid();
            var field = MakeField(0.15f, paint: true);
            var landmarks = new RoadSearchLandmarks(grid, field, Height10);
            landmarks.EnsureFresh();
            Func<float, float, float> cost = (x, z) => field.EvaluateAtTerrain(x, z);
            var start = grid.CellCenter(2, 2);
            var goal = grid.CellCenter(grid.Width - 3, grid.Height - 3);

            var first = new List<Vector2>();
            Assert.IsTrue(RoadPathfinder.TryFindPath(
                grid, cost, start, goal, first, out var firstCost, Height10, null, 0.15f, landmarks));

            for (var run = 0; run < 2; run++)
            {
                var again = new List<Vector2>();
                Assert.IsTrue(RoadPathfinder.TryFindPath(
                    grid, cost, start, goal, again, out var againCost, Height10, null, 0.15f, landmarks));
                Assert.AreEqual(
                    BitConverter.SingleToInt32Bits(firstCost), BitConverter.SingleToInt32Bits(againCost));
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
