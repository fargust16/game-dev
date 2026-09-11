using System.Collections.Generic;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 1 of the terrain generator: landmass, coastline and the coastal height field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert the invariants the design document names for this stage — determinism, the
    /// lattice, land connectivity, coastline validity, the height budget — and nothing about how the
    /// world looks. There are no mountains, ridges, basins or rivers at Stage 1, so there is nothing
    /// here about relief quality; that arrives with the stages that produce relief.
    /// </para>
    /// <para>
    /// Generation runs once per seed in <c>OneTimeSetUp</c>: a full world is a second or two, and a
    /// fixture that regenerates per test would spend a minute proving the same thing twenty times.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage1Tests
    {
        private const int SeedA = 110201;
        private const int SeedB = 424242;

        private static TerrainGenSettings Settings => TerrainGenSettings.Default;

        private Stage1Result a;
        private Stage1Result b;

        [OneTimeSetUp]
        public void GenerateWorlds()
        {
            a = Stage1LandmassGenerator.Generate(SeedA, Settings);
            b = Stage1LandmassGenerator.Generate(SeedB, Settings);
        }

        private static float Margin => WorldScale.IslandOceanMarginMeters;

        // ---------- determinism ----------

        [Test]
        public void SameSeed_ProducesAnIdenticalField()
        {
            var repeat = Stage1LandmassGenerator.Generate(SeedA, Settings);

            Assert.AreEqual(a.Stats.FieldHash, repeat.Stats.FieldHash, "field hash differs on a repeat run");
            Assert.AreEqual(a.Structure.CoastRings.Count, repeat.Structure.CoastRings.Count);
            Assert.AreEqual(a.Structure.CoastVertexCount(), repeat.Structure.CoastVertexCount());
            CollectionAssert.AreEqual(a.Structure.LandSites, repeat.Structure.LandSites);

            for (var i = 0; i < a.Field.Heights.Length; i++)
            {
                if (a.Field.Heights[i] != repeat.Field.Heights[i])
                {
                    Assert.Fail($"height differs at sample {i}");
                }
            }
        }

        [Test]
        public void DifferentSeed_ProducesADifferentWorld()
        {
            // Without this the determinism test above would pass on a generator that ignores its seed.
            Assert.AreNotEqual(a.Stats.FieldHash, b.Stats.FieldHash);
        }

        [Test]
        public void PointQuery_AgreesWithTheBulkField()
        {
            // D2. The bulk path fills a row from one set of scanline crossings; the point path
            // computes crossings for its own row. They share the predicate, so they must agree
            // exactly - a tolerance here would hide the drift it exists to catch.
            var field = a.Field;
            var mismatches = 0;
            var firstMessage = string.Empty;

            var step = 17; // coprime with the row width, so the samples are not a lattice of their own
            for (var i = 0; i < field.Heights.Length; i += step)
            {
                var x = i % field.Width;
                var z = i / field.Width;
                var bulk = field.Heights[i];
                var point = a.HeightAt(field.WorldX(x), field.WorldZ(z));
                if (bulk == point)
                {
                    continue;
                }

                if (mismatches == 0)
                {
                    firstMessage = $"({x},{z}) bulk={bulk} point={point}";
                }

                mismatches++;
            }

            Assert.AreEqual(0, mismatches, $"point query disagreed with the field {mismatches} times; first {firstMessage}");
        }

        [Test]
        public void RowOrder_DoesNotChangeTheField()
        {
            // S2 in spirit: nothing about the evaluation may depend on the order rows are visited.
            // Guards against a future optimisation that carries state between rows.
            var forward = a.Field;
            var reverse = WorldHeightField.ForWorld(Settings.FieldResolution);
            var crossings = new List<float>(16);

            for (var z = reverse.Height - 1; z >= 0; z--)
            {
                var worldZ = reverse.WorldZ(z);
                a.Coast.RowCrossings(worldZ, crossings);
                for (var x = reverse.Width - 1; x >= 0; x--)
                {
                    var worldX = reverse.WorldX(x);
                    var inside = CoastQuery.InsideFromSortedCrossings(crossings, worldX);
                    var d = a.Coast.NearestDistance(worldX, worldZ);
                    reverse.Heights[z * reverse.Width + x] = a.Model.HeightAt(inside ? d : -d);
                }
            }

            Assert.AreEqual(forward.Hash(), reverse.Hash());
        }

        [Test]
        public void LandSites_AreSortedAndUnique()
        {
            // D4: ordering is part of the output, never an artefact of hash iteration.
            for (var i = 1; i < a.Structure.LandSites.Count; i++)
            {
                Assert.Less(
                    a.Structure.LandSites[i - 1],
                    a.Structure.LandSites[i],
                    "land sites must be strictly ascending");
            }
        }

        [Test]
        public void CoastRings_AreOrderedLargestFirst()
        {
            var previous = float.PositiveInfinity;
            for (var i = 0; i < a.Structure.CoastRings.Count; i++)
            {
                var area = Mathf.Abs(Town.Geometry.Geo2.PolygonArea(a.Structure.CoastRings[i]));
                Assert.LessOrEqual(area, previous + 0.01f, "rings are not ordered largest first");
                previous = area;
            }
        }

        // ---------- the lattice ----------

        [Test]
        public void FieldLattice_IsTheLatticeTheUnityTilesShare()
        {
            // The whole tile-seam argument rests on this identity: four 513 tiles sharing their edge
            // rows are exactly one 1025 world lattice. If it stops holding, seams stop being free.
            var tileResolution = OverworldTerrainGrid.DefaultHeightmapResolution;
            var expected = WorldScale.OverworldTileCountX * (tileResolution - 1) + 1;

            Assert.AreEqual(expected, a.Field.Width);
            Assert.AreEqual(expected, a.Field.Height);
            Assert.AreEqual(
                WorldScale.OverworldTileMeters / (tileResolution - 1),
                a.Field.CellSize,
                1e-6f);
            Assert.AreEqual(
                WorldScale.WorldExtentXMeters,
                a.Field.CellSize * (a.Field.Width - 1),
                1e-3f);
        }

        // ---------- the field ----------

        [Test]
        public void Field_IsFiniteAndInsideTheHeightBudget()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.IsFalse(result.Stats.HasNonFinite, $"seed {result.Structure.Seed} produced NaN or infinity");
                Assert.GreaterOrEqual(result.Stats.MinHeightMeters, 0f);
                Assert.LessOrEqual(result.Stats.MaxHeightMeters, WorldScale.TerrainHeightBudgetMeters);

                // Stage 1 has no relief beyond the coastal model, so the field must also stay inside
                // what that model can produce. A value outside it means something else wrote height.
                Assert.GreaterOrEqual(result.Stats.MinHeightMeters, result.Settings.MinSeaFloorMeters - 0.01f);
                Assert.LessOrEqual(result.Stats.MaxHeightMeters, result.Settings.MaxLandHeightMeters + 0.01f);
            }
        }

        [Test]
        public void Coastline_SitsAtSeaLevel()
        {
            var ring = a.Structure.CoastRings[0];
            for (var i = 0; i < ring.Length; i += Mathf.Max(1, ring.Length / 64))
            {
                var h = a.HeightAt(ring[i].x, ring[i].y);
                Assert.AreEqual(WorldScale.SeaLevelMeters, h, 0.5f,
                    $"coast vertex {i} is not at sea level");
            }
        }

        [Test]
        public void WorldBorder_IsSubmergedBelowTheMinimum()
        {
            // The ocean margin guarantees the coast is at least that far from the border, so the
            // border must be well under water - otherwise terrain and the ocean plane would meet at
            // the world edge.
            var extentX = WorldScale.WorldExtentXMeters;
            var extentZ = WorldScale.WorldExtentZMeters;
            var samples = 0;

            for (var t = 0f; t <= 1f; t += 0.05f)
            {
                foreach (var p in new[]
                         {
                             new Vector2(t * extentX, 0f),
                             new Vector2(t * extentX, extentZ),
                             new Vector2(0f, t * extentZ),
                             new Vector2(extentX, t * extentZ),
                         })
                {
                    var h = a.HeightAt(p.x, p.y);
                    Assert.LessOrEqual(
                        h,
                        WorldScale.SeaLevelMeters - WorldScale.MinimumSubmersionMeters,
                        $"world border at ({p.x:F0},{p.y:F0}) is not submerged");
                    samples++;
                }
            }

            Assert.Greater(samples, 0);
        }

        // ---------- geography ----------

        [Test]
        public void CoastRings_AreSimpleClosedPolygonsInsideTheOceanMargin()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.Greater(result.Structure.CoastRings.Count, 0, "no coastline was produced");

                for (var r = 0; r < result.Structure.CoastRings.Count; r++)
                {
                    var ring = result.Structure.CoastRings[r];
                    Assert.GreaterOrEqual(ring.Length, 3, $"ring {r} is not a polygon");
                    Assert.IsTrue(CoastOutline.IsSimple(ring), $"ring {r} self-intersects");

                    for (var i = 0; i < ring.Length; i++)
                    {
                        Assert.GreaterOrEqual(ring[i].x, Margin - 0.01f);
                        Assert.GreaterOrEqual(ring[i].y, Margin - 0.01f);
                        Assert.LessOrEqual(ring[i].x, WorldScale.WorldExtentXMeters - Margin + 0.01f);
                        Assert.LessOrEqual(ring[i].y, WorldScale.WorldExtentZMeters - Margin + 0.01f);
                    }
                }
            }
        }

        [Test]
        public void Land_IsOneMainlandPlusItsIslets()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.AreEqual(
                    1 + result.Structure.IsletCount,
                    result.Stats.FieldLandComponents,
                    $"seed {result.Structure.Seed}: dry land in the field is not one mainland plus "
                    + $"{result.Structure.IsletCount} islet(s)");

                Assert.AreEqual(
                    result.Stats.FieldLandComponents,
                    result.Structure.CoastRings.Count,
                    "every land component must have exactly one published ring");
            }
        }

        [Test]
        public void LandFraction_IsWithinTheBudgetedBand()
        {
            foreach (var result in new[] { a, b })
            {
                Assert.IsFalse(result.Stats.LandBudgetShort,
                    $"seed {result.Structure.Seed} ran out of eligible cells before its land budget");

                Assert.GreaterOrEqual(result.Stats.PolygonLandFraction, 0.20f);
                Assert.LessOrEqual(result.Stats.PolygonLandFraction, 0.42f);

                // The field is the polygon sampled on a lattice; a large gap between the two means
                // the height model and the outline disagree about where land is.
                Assert.AreEqual(
                    result.Stats.PolygonLandFraction,
                    result.Stats.FieldLandFraction,
                    0.02f,
                    "polygon and field disagree about how much land there is");
            }
        }

        [Test]
        public void Coastline_IsMoreCrenulatedThanItsConvexHull()
        {
            // The point of a partition-boundary coast rather than a radius: bays and peninsulas.
            // A convex blob scores 1.0.
            foreach (var result in new[] { a, b })
            {
                Assert.Greater(
                    result.Stats.CoastCrenulation,
                    1.15f,
                    $"seed {result.Structure.Seed} coastline is nearly convex "
                    + $"({result.Stats.CoastCrenulation:F2})");
            }
        }

        // ---------- primitives ----------

        [Test]
        public void ProfileCurve_ClampsOutsideItsRangeAndRisesMonotonically()
        {
            var curve = new ProfileCurve(new[] { 0f, 0.5f, 1f }, new[] { 0f, 0.3f, 1f });

            Assert.AreEqual(0f, curve.Evaluate(-1f), 1e-6f);
            Assert.AreEqual(1f, curve.Evaluate(2f), 1e-6f);
            Assert.IsTrue(curve.IsMonotoneRising());

            var previous = float.NegativeInfinity;
            for (var t = 0f; t <= 1f; t += 0.01f)
            {
                var v = curve.Evaluate(t);
                Assert.GreaterOrEqual(v, previous - 1e-5f, $"curve dipped at t={t}");
                previous = v;
            }
        }

        [Test]
        public void CoastHeightModel_IsMonotoneInDistanceFromTheShore()
        {
            var model = new CoastHeightModel(Settings);
            var previous = float.NegativeInfinity;

            for (var d = -Settings.ShelfWidthMeters * 1.5f;
                 d <= Settings.CoastalTransitionMeters + Settings.InteriorScaleMeters * 1.5f;
                 d += 5f)
            {
                var h = model.HeightAt(d);
                Assert.GreaterOrEqual(h, previous - 1e-4f, $"height dipped at signed distance {d}");
                previous = h;
            }

            Assert.AreEqual(WorldScale.SeaLevelMeters, model.HeightAt(0f), 1e-4f);
        }

        [Test]
        public void CrustalSites_RespectTheMinimumSeparation()
        {
            var partition = a.Structure.Partition;
            var spacingSq = Settings.CrustSiteSpacingMeters * Settings.CrustSiteSpacingMeters;

            Assert.Greater(partition.SiteCount, 20, "too few crustal sites to partition a world");

            for (var i = 0; i < partition.SiteCount; i++)
            {
                for (var j = i + 1; j < partition.SiteCount; j++)
                {
                    var sq = (partition.Sites[i] - partition.Sites[j]).sqrMagnitude;
                    Assert.GreaterOrEqual(sq, spacingSq - 1e-2f,
                        $"sites {i} and {j} are closer than the minimum separation");
                }
            }
        }

        [Test]
        public void CrustalPartition_AssignsEveryRasterCellToItsNearestSite()
        {
            // The partition raster and the point query must describe the same partition, or the
            // structure image and the geography disagree.
            var partition = a.Structure.Partition;
            for (var z = 3; z < partition.RasterHeight; z += 37)
            {
                for (var x = 5; x < partition.RasterWidth; x += 41)
                {
                    var expected = partition.SiteAt(partition.CellCenterX(x), partition.CellCenterZ(z));
                    Assert.AreEqual(expected, partition.Cells[z * partition.RasterWidth + x]);
                }
            }
        }

        [Test]
        public void Displace_AddsDetailAndStaysInsideTheInset()
        {
            var square = new[]
            {
                new Vector2(600f, 600f),
                new Vector2(2200f, 600f),
                new Vector2(2200f, 2200f),
                new Vector2(600f, 2200f),
            };

            var rnd = new System.Random(12345);
            var displaced = CoastOutline.Displace(
                square, 0.15f, 40f, rnd,
                Margin,
                WorldScale.WorldExtentXMeters - Margin,
                WorldScale.WorldExtentZMeters - Margin);

            Assert.Greater(displaced.Length, square.Length, "displacement added no vertices");
            Assert.Greater(
                CoastOutline.Perimeter(displaced),
                CoastOutline.Perimeter(square),
                "a displaced ring must be longer than the ring it came from");

            foreach (var p in displaced)
            {
                Assert.GreaterOrEqual(p.x, Margin - 0.01f);
                Assert.GreaterOrEqual(p.y, Margin - 0.01f);
                Assert.LessOrEqual(p.x, WorldScale.WorldExtentXMeters - Margin + 0.01f);
                Assert.LessOrEqual(p.y, WorldScale.WorldExtentZMeters - Margin + 0.01f);
            }
        }

        [Test]
        public void FillEnclosedSea_ClosesAPocketThatCannotReachTheBorder()
        {
            const int w = 7;
            const int h = 7;
            var land = new bool[w * h];
            for (var z = 1; z < h - 1; z++)
            {
                for (var x = 1; x < w - 1; x++)
                {
                    land[z * w + x] = true;
                }
            }

            land[3 * w + 3] = false; // a hole in the middle of the block

            var filled = CoastOutline.FillEnclosedSea(land, w, h);

            Assert.AreEqual(1, filled);
            Assert.IsTrue(land[3 * w + 3]);
            Assert.IsFalse(land[0], "the outer sea must not be filled");
        }
    }
}
