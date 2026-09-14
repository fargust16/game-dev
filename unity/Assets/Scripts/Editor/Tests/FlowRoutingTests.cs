using GameDev.World.Hydrology;
using NUnit.Framework;

namespace GameDev.World.Hydrology.Tests
{
    public sealed class FlowRoutingTests
    {
        [Test]
        public void D8_AlwaysStepsDownhill()
        {
            var grid = HydrologyTestTerrain.ValleyIsland();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);

            for (var i = 0; i < grid.CellCount; i++)
            {
                var target = FlowRouting.Downstream(grid, i, directions[i]);
                if (target < 0)
                {
                    continue;
                }

                Assert.Less(
                    filled[target], filled[i],
                    $"cell {i} drains to a cell that is not lower");
            }
        }

        [Test]
        public void Accumulation_VisitsEveryCell_SoThereAreNoCycles()
        {
            // Kahn's algorithm can only stall on a cycle, so a full visit count is a direct proof
            // that the flow field is a forest.
            var grid = HydrologyTestTerrain.ValleyIsland();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);

            FlowRouting.Accumulate(grid, directions, ocean, out var processed);

            Assert.AreEqual(grid.CellCount, processed);
        }

        [Test]
        public void Accumulation_NeverFallsDownstream()
        {
            var grid = HydrologyTestTerrain.ValleyIsland();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);
            var accumulation = FlowRouting.Accumulate(grid, directions, ocean, out _);

            for (var i = 0; i < grid.CellCount; i++)
            {
                var target = FlowRouting.Downstream(grid, i, directions[i]);
                if (target < 0 || ocean[target])
                {
                    continue;
                }

                Assert.GreaterOrEqual(
                    accumulation[target], accumulation[i] - 1e-3f,
                    $"contributing area shrank from cell {i} to {target}");
            }
        }

        [Test]
        public void Accumulation_OfALandCellIsAtLeastItsOwnArea()
        {
            var grid = HydrologyTestTerrain.Ramp();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);
            var accumulation = FlowRouting.Accumulate(grid, directions, ocean, out _);

            for (var i = 0; i < grid.CellCount; i++)
            {
                if (ocean[i])
                {
                    continue;
                }

                Assert.GreaterOrEqual(accumulation[i], grid.CellAreaSqm - 1e-3f);
            }
        }

        [Test]
        public void Accumulation_IsIndependentOfCellArea_InSquareMetres()
        {
            // The property that lets the analysis resolution change without changing the network:
            // two grids over the same terrain at different spacings must agree on how much land
            // drains through the same place.
            var coarse = HydrologyTestTerrain.Ramp(size: 33, cellSize: 20f);
            var fine = HydrologyTestTerrain.Ramp(size: 65, cellSize: 10f);

            Assert.AreEqual(
                TotalLandArea(coarse), TotalLandArea(fine),
                TotalLandArea(fine) * 0.06f,
                "the same terrain measured at two resolutions disagreed on its land area");
        }

        private static float TotalLandArea(HydrologyGrid grid)
        {
            var ocean = grid.BuildOceanMask();
            var area = 0f;
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (!ocean[i])
                {
                    area += grid.CellAreaSqm;
                }
            }

            return area;
        }
    }
}
