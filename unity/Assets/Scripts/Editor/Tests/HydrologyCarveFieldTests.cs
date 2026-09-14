using System;
using GameDev.World.Hydrology;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Hydrology.Tests
{
    // Inside the namespace on purpose: these tests live under GameDev.World, where the
    // legacy GameDev.World.WorldSeed would otherwise win the simple name over a
    // file-scoped alias, and silently bind to the class without IWorldSeed.
    using WorldSeed = GameDev.World.Seed.WorldSeed;

    /// <summary>
    /// The carve field is where hydrology becomes terrain, so these tests pin the properties the
    /// terrain bake depends on: it is zero away from water, it is deepest on the centreline, it never
    /// exceeds the configured maximum depth, and it does not touch lake basins.
    /// </summary>
    public sealed class HydrologyCarveFieldTests
    {
        private const int Resolution = 64;

        private static HydrologySettings Settings()
        {
            var settings = HydrologySettings.Default;
            settings.ChannelInitiationAreaSqm = 6_000f;
            settings.FullSizeAreaSqm = 120_000f;
            settings.MinRiverSystemAreaSqm = 8_000f;
            settings.MinRiverSystemFractionOfLargest = 0.12f;
            settings.NodeSpacingMeters = 10f;
            settings.Clamp();
            return settings;
        }

        private static HydrologyDefinition Plan(HydrologyGrid grid)
        {
            var result = HydrologyPlanner.Plan(grid, Settings(), new WorldSeed(110201));
            Assert.IsTrue(result.Succeeded, result.Message);
            return result.Definition;
        }

        private static Rect WorldRect(HydrologyGrid grid) =>
            new Rect(
                grid.OriginX,
                grid.OriginZ,
                (grid.Width - 1) * grid.CellSize,
                (grid.Height - 1) * grid.CellSize);

        [Test]
        public void Carve_IsZeroFarFromAnyRiver()
        {
            var grid = HydrologyTestTerrain.ValleyIsland();
            var definition = Plan(grid);
            var carve = HydrologyCarveField.Rasterize(
                definition, WorldRect(grid), Resolution, grid.SampleHeightWorld);

            var zero = 0;
            for (var i = 0; i < carve.Length; i++)
            {
                if (carve[i] == 0f)
                {
                    zero++;
                }
            }

            Assert.Greater(
                zero, carve.Length / 2,
                "most of the world should be untouched — the carve is not a global displacement");
        }

        [Test]
        public void Carve_IsNeverNegativeAndNeverExceedsTheCarveBudget()
        {
            // A negative value would raise the terrain, and the bake subtracts whatever it is given,
            // so the budget has to hold in the field itself.
            var grid = HydrologyTestTerrain.ValleyIsland();
            var definition = Plan(grid);
            var carve = HydrologyCarveField.Rasterize(
                definition, WorldRect(grid), Resolution, grid.SampleHeightWorld);

            for (var i = 0; i < carve.Length; i++)
            {
                Assert.GreaterOrEqual(carve[i], 0f);
                Assert.LessOrEqual(carve[i], definition.Settings.MaxCarveDepthMeters + 1e-3f);
            }
        }

        [Test]
        public void Carve_IsDeepestOnTheCentreline()
        {
            var grid = HydrologyTestTerrain.ValleyIsland();
            var definition = Plan(grid);
            var rect = WorldRect(grid);

            // A dense raster, so the centreline actually lands inside a pixel.
            const int fine = 256;
            var carve = HydrologyCarveField.Rasterize(
                definition, rect, fine, grid.SampleHeightWorld);

            var reach = definition.Reaches[0];
            var node = reach.Nodes[reach.Nodes.Count / 2];
            var settings = definition.Settings;
            var offset = settings.ValleyHalfWidth(node.ContributingAreaSqm) * 1.5f;

            var onCentre = Sample(carve, rect, fine, node.PositionXZ);
            var awayA = Sample(carve, rect, fine, node.PositionXZ + new Vector2(offset, 0f));
            var awayB = Sample(carve, rect, fine, node.PositionXZ + new Vector2(0f, offset));

            Assert.Greater(onCentre, awayA);
            Assert.Greater(onCentre, awayB);
        }

        [Test]
        public void Carve_LeavesLakeBasinsAlone()
        {
            // The lake surface was measured from the un-carved basin, so cutting into it would move
            // the ground out from under the water plane the same analysis produced.
            var grid = HydrologyTestTerrain.RampWithPit(size: 96, cellSize: 12f, depth: 10f);
            var definition = Plan(grid);
            Assert.AreEqual(1, definition.Lakes.Count);

            var rect = WorldRect(grid);
            const int fine = 256;
            var carve = HydrologyCarveField.Rasterize(
                definition, rect, fine, grid.SampleHeightWorld);

            var lake = definition.Lakes[0];
            var centre = Vector2.zero;
            for (var i = 0; i < lake.Boundary.Count; i++)
            {
                centre += lake.Boundary[i];
            }

            centre /= lake.Boundary.Count;

            Assert.AreEqual(0f, Sample(carve, rect, fine, centre), 1e-4f);
        }

        [Test]
        public void EmptyHydrology_RasterizesToNothing()
        {
            var carve = HydrologyCarveField.Rasterize(
                HydrologyDefinition.Empty, new Rect(0f, 0f, 1400f, 1400f), 32, (_, _) => 50f);

            foreach (var value in carve)
            {
                Assert.AreEqual(0f, value);
            }
        }

        [Test]
        public void Rasterize_RejectsBadArguments()
        {
            var rect = new Rect(0f, 0f, 1400f, 1400f);

            Assert.Throws<ArgumentNullException>(
                () => HydrologyCarveField.Rasterize(null, rect, 32, (_, _) => 0f));
            Assert.Throws<ArgumentNullException>(
                () => HydrologyCarveField.Rasterize(HydrologyDefinition.Empty, rect, 32, null));
            Assert.Throws<ArgumentException>(
                () => HydrologyCarveField.Rasterize(
                    HydrologyDefinition.Empty, rect, 1, (_, _) => 0f));
        }

        private static float Sample(float[] carve, Rect rect, int resolution, Vector2 world)
        {
            var px = Mathf.Clamp(
                Mathf.FloorToInt((world.x - rect.xMin) / rect.width * resolution), 0, resolution - 1);
            var pz = Mathf.Clamp(
                Mathf.FloorToInt((world.y - rect.yMin) / rect.height * resolution), 0, resolution - 1);
            return carve[pz * resolution + px];
        }
    }
}
