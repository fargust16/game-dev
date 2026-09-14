using GameDev.World.Hydrology;
using NUnit.Framework;

namespace GameDev.World.Hydrology.Tests
{
    /// <summary>
    /// Lakes must come out of the terrain, not out of a placement pass. These tests pin that: a pit
    /// makes a lake, an open slope does not, and a pit too small or too shallow to read as water is
    /// filtered even though the flood still levels it.
    /// </summary>
    public sealed class BasinExtractionTests
    {
        private static LakeField Extract(HydrologyGrid grid, HydrologySettings settings)
        {
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            return BasinExtraction.Extract(grid, filled, ocean, settings);
        }

        private static HydrologySettings Settings()
        {
            var settings = HydrologySettings.Default;
            settings.Clamp();
            return settings;
        }

        [Test]
        public void OpenSlope_ProducesNoLakes()
        {
            var lakes = Extract(HydrologyTestTerrain.Ramp(), Settings());
            Assert.AreEqual(0, lakes.Lakes.Count);
        }

        [Test]
        public void Pit_BecomesOneLake()
        {
            var lakes = Extract(HydrologyTestTerrain.RampWithPit(), Settings());
            Assert.AreEqual(1, lakes.Lakes.Count);
        }

        [Test]
        public void Lake_SurfaceSitsAboveItsFloorAndCoversEveryCell()
        {
            var grid = HydrologyTestTerrain.RampWithPit(depth: 8f);
            var lake = Extract(grid, Settings()).Lakes[0];

            Assert.Greater(lake.MaxDepth, 0f);
            Assert.Greater(lake.AreaSqm, 0f);

            // A flat water surface is the whole point: no cell of the basin may poke through it.
            for (var i = 0; i < lake.Cells.Length; i++)
            {
                Assert.LessOrEqual(
                    grid.Heights[lake.Cells[i]], lake.SurfaceElevation + 1e-3f,
                    "a lake cell's terrain rises above the water surface");
            }
        }

        [Test]
        public void Lake_DepthMatchesThePitItWasDugFrom()
        {
            const float depth = 8f;
            var lake = Extract(HydrologyTestTerrain.RampWithPit(depth: depth), Settings()).Lakes[0];

            // The pit sits on a ramp, so the deepest point is the pit floor at its downhill corner;
            // the measured depth must be in that neighbourhood and never deeper than the pit.
            Assert.LessOrEqual(lake.MaxDepth, depth + 0.5f);
            Assert.Greater(lake.MaxDepth, depth * 0.4f);
        }

        [Test]
        public void Lake_TooShallowIsRejected()
        {
            var settings = Settings();
            settings.MinLakeDepthMeters = 20f;
            settings.Clamp();

            var lakes = Extract(HydrologyTestTerrain.RampWithPit(depth: 8f), settings);
            Assert.AreEqual(0, lakes.Lakes.Count);
        }

        [Test]
        public void Lake_TooSmallIsRejected()
        {
            var settings = Settings();
            settings.MinLakeAreaSqm = 1_000_000f;
            settings.Clamp();

            var lakes = Extract(HydrologyTestTerrain.RampWithPit(), settings);
            Assert.AreEqual(0, lakes.Lakes.Count);
        }

        [Test]
        public void Lake_BoundaryIsAUsablePolygon()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var lake = Extract(grid, Settings()).Lakes[0];

            Assert.GreaterOrEqual(lake.Boundary.Length, 3, "the shoreline is not a polygon");
            for (var i = 0; i < lake.Boundary.Length; i++)
            {
                Assert.IsFalse(float.IsNaN(lake.Boundary[i].x));
                Assert.IsFalse(float.IsNaN(lake.Boundary[i].y));
            }

            // A square basin's outline is four corners plus whatever the simplifier keeps, and it can
            // never be a function of how big the grid is. Asserting only "at least three vertices"
            // let a shoreline walk that never closed pass for a whole implementation: it ran until
            // its guard tripped, produced hundreds of thousands of corners, and the simplifier
            // reduced them to something that still looked like a polygon.
            Assert.Less(
                lake.Boundary.Length, 64,
                "the shoreline has far more vertices than the basin has edges, which means the "
                + "boundary walk did not close");
        }

        [Test]
        public void Lake_BoundaryCostDoesNotGrowWithTheGrid()
        {
            // The same basin measured on a four-times-denser grid. A closed walk costs the basin's
            // perimeter; a walk that only stops at its guard costs the whole grid.
            var coarse = HydrologyTestTerrain.RampWithPit(size: 64, cellSize: 10f);
            var fine = HydrologyTestTerrain.RampWithPit(
                size: 256, cellSize: 2.5f, pitMin: 180, pitMax: 220);

            var coarseLake = Extract(coarse, Settings()).Lakes[0];
            var fineLake = Extract(fine, Settings()).Lakes[0];

            Assert.Less(
                fineLake.Boundary.Length, coarseLake.Boundary.Length * 8,
                "shoreline vertex count scales with the grid, not with the shoreline");
        }

        [Test]
        public void Lake_BoundaryEnclosesTheBasin()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var field = Extract(grid, Settings());
            var lake = field.Lakes[0];

            // Every cell the flood submerged has to lie inside the outline drawn around it.
            for (var c = 0; c < lake.Cells.Length; c++)
            {
                var position = grid.PositionXZ(lake.Cells[c]);
                Assert.IsTrue(
                    Contains(lake.Boundary, position.x, position.y),
                    $"lake cell at {position} is outside its own shoreline");
            }
        }

        private static bool Contains(UnityEngine.Vector2[] polygon, float x, float z)
        {
            var inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if (a.y > z != b.y > z && x < (b.x - a.x) * (z - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        [Test]
        public void Lake_SpillCellIsTheLowestPointOfTheFilledSurface()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var lake = BasinExtraction.Extract(grid, filled, ocean, Settings()).Lakes[0];

            for (var i = 0; i < lake.Cells.Length; i++)
            {
                Assert.GreaterOrEqual(
                    filled[lake.Cells[i]], filled[lake.SpillCell] - 1e-4f,
                    "a lake cell sits below the cell reported as the spill");
            }
        }

        [Test]
        public void Extraction_IsDeterministic()
        {
            var grid = HydrologyTestTerrain.RampWithPit();
            var first = Extract(grid, Settings());
            var second = Extract(grid, Settings());

            Assert.AreEqual(first.Lakes.Count, second.Lakes.Count);
            for (var i = 0; i < first.Lakes.Count; i++)
            {
                Assert.AreEqual(first.Lakes[i].AreaSqm, second.Lakes[i].AreaSqm);
                Assert.AreEqual(first.Lakes[i].SurfaceElevation, second.Lakes[i].SurfaceElevation);
                Assert.AreEqual(first.Lakes[i].SpillCell, second.Lakes[i].SpillCell);
            }
        }
    }
}
