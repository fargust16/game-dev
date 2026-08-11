using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadPathfinderTests
    {
        private static RoadGridSpec MakeGrid() => new RoadGridSpec(0f, 0f, 10, 10, 10f);

        [Test]
        public void TryFindPath_NoObstacles_ReturnsPathFromStartToGoal()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            var found = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(5f, 5f), new Vector2(95f, 95f), path);

            Assert.IsTrue(found);
            Assert.Greater(path.Count, 1);
            Assert.Less(Vector2.Distance(path[0], new Vector2(5f, 5f)), 10f);
            Assert.Less(Vector2.Distance(path[path.Count - 1], new Vector2(95f, 95f)), 10f);
        }

        [Test]
        public void TryFindPath_WallWithGap_RoutesThroughTheGap()
        {
            // Wide grid so the blocked band (220m) exceeds RoadCost.MaxBridgeSpanMeters (200m) -
            // bridging can't skip across it, so the physical gap at low z is the only route.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var path = new List<Vector2>();

            float Cost(float x, float z) => x > 50f && x < 270f && z > 20f ? float.PositiveInfinity : 1f;

            var found = RoadPathfinder.TryFindPath(grid, Cost, new Vector2(5f, 95f), new Vector2(345f, 95f), path);

            Assert.IsTrue(found);
            var passedThroughGap = false;
            foreach (var p in path)
            {
                if (p.x > 50f && p.x < 270f)
                {
                    Assert.LessOrEqual(p.y, 20f, "the only open crossing of the wall is at low z - the route must not cross elsewhere");
                    passedThroughGap = true;
                }
            }

            Assert.IsTrue(passedThroughGap, "a path connecting the two sides of the wall must actually cross through the gap");
        }

        [Test]
        public void TryFindPath_NarrowWaterBand_BridgesAcrossEvenWithNoPhysicalGap()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            // 20m-wide band spanning every z - no physical gap to detour through anywhere on this
            // grid, so the only way across is a bridge.
            float Cost(float x, float z) => x > 40f && x < 60f ? float.PositiveInfinity : 1f;

            var found = RoadPathfinder.TryFindPath(grid, Cost, new Vector2(5f, 50f), new Vector2(95f, 50f), path);

            Assert.IsTrue(found, "a 20m gap is well within MaxBridgeSpanMeters and should be bridged");

            var maxGap = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                maxGap = Mathf.Max(maxGap, Vector2.Distance(path[i - 1], path[i]));
            }

            Assert.Greater(maxGap, grid.CellSize * 1.5f,
                "expected one long bridge jump in the reconstructed path, skipping the submerged cells between");
        }

        [Test]
        public void TryFindPath_WaterWiderThanMaxBridgeSpan_NoRouteFound()
        {
            // Band (220m) exceeds RoadCost.MaxBridgeSpanMeters (200m) and spans every z - neither a
            // bridge nor a walk-around detour exists, so this must fail honestly.
            var grid = new RoadGridSpec(0f, 0f, 35, 10, 10f);
            var path = new List<Vector2>();

            float Cost(float x, float z) => x > 50f && x < 270f ? float.PositiveInfinity : 1f;

            var found = RoadPathfinder.TryFindPath(grid, Cost, new Vector2(5f, 50f), new Vector2(345f, 50f), path);

            Assert.IsFalse(found);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void TryFindPath_PrefersCheaperDetourOverExpensiveShortcut()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            // Expensive only in the low-z strip directly between start and goal; open (cost 1)
            // everywhere at z >= 40, including the same x range. Crossing directly costs 20 units
            // of x-width at 50x each (~1000); detouring up to z >= 40 first costs roughly 70 extra
            // meters at base cost (~70) plus the 20-unit crossing at base cost - far cheaper overall.
            float Cost(float x, float z) => x > 40f && x < 60f && z < 40f ? 50f : 1f;

            var found = RoadPathfinder.TryFindPath(grid, Cost, new Vector2(5f, 5f), new Vector2(95f, 5f), path);

            Assert.IsTrue(found);
            foreach (var p in path)
            {
                if (p.x > 40f && p.x < 60f)
                {
                    Assert.GreaterOrEqual(p.y, 40f, "crossing the expensive band should happen up in the open strip, not straight through at low z");
                }
            }
        }

        [Test]
        public void TryFindPath_StartOutsideGrid_ReturnsFalse()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            var found = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(-50f, 5f), new Vector2(95f, 95f), path);

            Assert.IsFalse(found);
        }

        [Test]
        public void TryFindPath_HeightDelegate_IncreasesCostForAModeratelySteepStep()
        {
            var grid = MakeGrid();
            var pathWithGrade = new List<Vector2>();
            var pathWithoutGrade = new List<Vector2>();

            // Grade here is 0.4 (4m rise over one 10m step) - above MaxComfortableGrade (0.15) but
            // below MaxWalkableGrade (0.6), so this is penalized but not hard-blocked. Same step
            // everywhere (height ignores z), so both runs are forced onto the identical physical
            // route - isolates "does grade increase the reported cost" from any routing choice.
            float Height(float x, float z) => x < 40f ? 0f : 4f;

            var foundWithGrade = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(5f, 5f), new Vector2(45f, 5f), pathWithGrade, out var costWithGrade, Height);
            var foundWithoutGrade = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(5f, 5f), new Vector2(45f, 5f), pathWithoutGrade, out var costWithoutGrade);

            Assert.IsTrue(foundWithGrade);
            Assert.IsTrue(foundWithoutGrade);
            Assert.Greater(costWithGrade, costWithoutGrade,
                "a 40% grade step should cost more once real grade is factored in, without being so steep it's blocked outright");
        }

        [Test]
        public void TryFindPath_HeightDelegate_HardBlocksAGradeAtOrAboveMaxWalkable()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            // Grade 3.0 (30m rise over one 10m step, ignoring z) is far past MaxWalkableGrade (0.6)
            // in every direction on this grid - no detour can avoid it, so this must fail honestly
            // instead of drawing a physically-impossible near-vertical "road".
            float Height(float x, float z) => x < 40f ? 0f : 30f;

            var found = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(5f, 5f), new Vector2(95f, 5f), path, out _, Height);

            Assert.IsFalse(found, "a step steeper than MaxWalkableGrade must block the route entirely, not just cost more");
        }

        [Test]
        public void TryFindPath_WithHeightDelegate_PrefersGentleRouteOverSteepDirectClimb()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            // Below z=50: a sheer step (0 -> 30 in one 10m grid step, grade 3.0) - well past
            // MaxWalkableGrade (0.6), genuinely impassable. At/above z=50: the same 30m rise spread
            // evenly across the full 100m width (grade ~0.3) - above MaxComfortableGrade (0.15, so
            // still penalized) but comfortably under the hard cap, so it's a real, walkable detour.
            float Height(float x, float z) => z < 50f ? (x < 40f ? 0f : 30f) : (x / 100f) * 30f;

            var found = RoadPathfinder.TryFindPath(grid, (x, z) => 1f, new Vector2(5f, 5f), new Vector2(95f, 5f), path, out _, Height);

            Assert.IsTrue(found, "the gentle lane is fully walkable even though the direct lane is hard-blocked");
            var usedGentleLane = false;
            foreach (var p in path)
            {
                if (p.y >= 50f)
                {
                    usedGentleLane = true;
                }
            }

            Assert.IsTrue(usedGentleLane,
                "the sheer direct climb exceeds MaxWalkableGrade and is blocked outright, so the only route is through the gentle lane");
        }

        [Test]
        public void TryFindPath_WithoutHeightDelegate_GradeIsNotConsidered()
        {
            var grid = MakeGrid();
            var path = new List<Vector2>();

            // Same two-lane setup as the grade-aware test above, but height is never passed in.
            float Cost(float x, float z) => 1f;

            var found = RoadPathfinder.TryFindPath(grid, Cost, new Vector2(5f, 5f), new Vector2(95f, 5f), path);

            Assert.IsTrue(found);
            foreach (var p in path)
            {
                Assert.Less(p.y, 50f, "with no height delegate, grade is never evaluated, so the shortest (direct, low-z) route wins as usual");
            }
        }
    }
}
