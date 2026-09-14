using GameDev.World.Hydrology;
using NUnit.Framework;

namespace GameDev.World.Hydrology.Tests
{
    /// <summary>
    /// Priority-flood is what makes the ocean-outlet invariant structural rather than hoped for, so
    /// these tests check the property the rest of the system leans on: the filled surface descends
    /// to the sea from everywhere.
    /// </summary>
    public sealed class DepressionFillTests
    {
        [Test]
        public void Fill_NeverLowersTerrain()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var filled = DepressionFill.Fill(grid, grid.BuildOceanMask());

            for (var i = 0; i < grid.CellCount; i++)
            {
                Assert.GreaterOrEqual(
                    filled[i], grid.Heights[i] - 1e-4f, $"cell {i} was lowered by the fill");
            }
        }

        [Test]
        public void Fill_LeavesOpenSlopeUntouched()
        {
            var grid = HydrologyTestTerrain.Ramp();
            var filled = DepressionFill.Fill(grid, grid.BuildOceanMask());

            for (var i = 0; i < grid.CellCount; i++)
            {
                Assert.AreEqual(
                    grid.Heights[i], filled[i], 0.01f,
                    $"cell {i} was raised although nothing dams it");
            }
        }

        [Test]
        public void Fill_RaisesAPitToItsSpillLevel()
        {
            const float depth = 8f;
            var grid = HydrologyTestTerrain.RampWithPit(depth: depth);
            var filled = DepressionFill.Fill(grid, grid.BuildOceanMask());

            var raised = 0;
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (filled[i] - grid.Heights[i] > 0.25f)
                {
                    raised++;
                }
            }

            Assert.Greater(raised, 0, "the pit was not filled at all");

            // The pit is 8 m deep on a ramp, so the flood cannot raise anything above that plus the
            // epsilon it accumulates crossing the basin.
            for (var i = 0; i < grid.CellCount; i++)
            {
                Assert.LessOrEqual(
                    filled[i] - grid.Heights[i], depth + 0.5f,
                    $"cell {i} was filled deeper than the pit it sits in");
            }
        }

        [Test]
        public void Fill_LeavesEveryLandCellWithADownhillNeighbour()
        {
            // The property everything downstream depends on: no pits survive, so D8 always finds a
            // step down and flow can never stall inland.
            var grid = HydrologyTestTerrain.RampWithPit();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);

            Assert.AreEqual(
                0, HydrologyTestTerrain.CountLandSinks(grid, directions, ocean),
                "a land cell had nowhere to drain after the fill");
        }

        [Test]
        public void Fill_IsDeterministic()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var first = DepressionFill.Fill(grid, grid.BuildOceanMask());
            var second = DepressionFill.Fill(grid, grid.BuildOceanMask());

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void OceanMask_ExcludesAnInlandPitBelowSeaLevel()
        {
            // A pit whose floor happens to sit below the waterline is not the sea. Treating it as
            // one would hand the network a false outlet in the middle of the island, quietly
            // satisfying the one invariant the system exists to guarantee.
            var grid = HydrologyTestTerrain.RampWithPit(depth: 40f);
            var ocean = grid.BuildOceanMask();

            var submergedButNotSea = 0;
            for (var i = 0; i < grid.CellCount; i++)
            {
                if (grid.IsSubmerged(i) && !ocean[i])
                {
                    submergedButNotSea++;
                }
            }

            Assert.Greater(
                submergedButNotSea, 0,
                "the deep inland pit should be below sea level yet not part of the ocean");
        }
    }
}
