using System;
using System.Collections.Generic;
using GameDev.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3F: the declared valley corridors cut into the Stage 4A island, dump-first.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contracts, never expected metric values: the carve is subtractive and bounded, exactly zero at
    /// and seaward of the shore, the land mask is untouched, the point query equals the lattice to the
    /// bit, two builds agree, the frozen stages are unmoved, and Stage 4A's own field is bit-identical
    /// with and without the carve on top. The numbers Stage 3F is judged by are measured by
    /// <see cref="TerrainGenStage3FDump"/>.
    /// </para>
    /// <para>
    /// Each seed costs a Stage 2B generation, a Stage 3 build and a Stage 4A relief build, so the
    /// worlds are built once in <c>OneTimeSetUp</c> and shared, as the Stage 4A suite does.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3FTests
    {
        private static readonly int[] Seeds = TerrainGenStage3FDump.CanonicalSeeds;

        private IslandReliefWorld[] worlds;
        private WorldHeightField[] surfaces;
        private WorldHeightField[] carves;
        private RidgeSettings settings;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            settings = RidgeSettings.Default;
            settings.Clamp();

            worlds = new IslandReliefWorld[Seeds.Length];
            surfaces = new WorldHeightField[Seeds.Length];
            carves = new WorldHeightField[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                worlds[i] = IslandReliefWorld.Build(Seeds[i], IslandReliefSettings.Default, withStage3: true);
                surfaces[i] = CarvedElevation.EvaluateField(
                    worlds[i].Stage1, worlds[i].H4AField, worlds[i].Carve, settings, out var carve);
                carves[i] = carve;
            }
        }

        // ---------- T1 ----------

        [Test]
        public void T1_CarveIsExactlyZeroAtAndSeawardOfTheShore()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var sd = w.Stage1.SignedDistance;
                var carve = carves[s].Heights;
                var surface = surfaces[s].Heights;
                var sea = 0;
                var nonZero = 0;
                var moved = 0;
                for (var i = 0; i < sd.Length; i++)
                {
                    if (sd[i] > 0f)
                    {
                        continue;
                    }

                    sea++;
                    if (carve[i] != 0f)
                    {
                        nonZero++;
                    }

                    if (surface[i] != w.H4A[i])
                    {
                        moved++;
                    }
                }

                Assert.Greater(sea, 0, $"seed {w.Seed}: no sea samples");
                Assert.AreEqual(0, nonZero, $"seed {w.Seed}: {nonZero} of {sea} samples at or seaward of the shore were carved");
                Assert.AreEqual(0, moved, $"seed {w.Seed}: {moved} sea samples moved");
            }
        }

        // ---------- T2 ----------

        [Test]
        public void T2_TheLandMaskIsUnchanged()
        {
            var sea = WorldScale.SeaLevelMeters;
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var surface = surfaces[s].Heights;
                var changed = 0;
                for (var i = 0; i < surface.Length; i++)
                {
                    if ((surface[i] > sea) != (w.H4A[i] > sea))
                    {
                        changed++;
                    }
                }

                Assert.AreEqual(0, changed, $"seed {w.Seed}: the carve moved the coastline at {changed} samples");
            }
        }

        // ---------- T3 ----------

        [Test]
        public void T3_CarveIsSubtractiveFiniteAndInsideItsAllowance()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var carve = carves[s].Heights;
                var surface = surfaces[s].Heights;
                var floorField = w.Stage1.Field.Heights;
                var bound = settings.CorridorIncisionMeters;
                for (var i = 0; i < carve.Length; i++)
                {
                    var cut = carve[i];
                    if (float.IsNaN(cut) || float.IsInfinity(cut) || cut < 0f)
                    {
                        Assert.Fail($"seed {w.Seed}: carve {cut} at sample {i} is not a non-negative finite depth");
                    }

                    if (surface[i] > w.H4A[i] + 1e-4f)
                    {
                        Assert.Fail($"seed {w.Seed}: sample {i} rose from {w.H4A[i]} to {surface[i]}; the carve only removes");
                    }

                    var allowance = Mathf.Max(0f, w.H4A[i] - floorField[i]) + bound;
                    if (cut > allowance + 1e-3f)
                    {
                        Assert.Fail($"seed {w.Seed}: carve {cut} at sample {i} exceeds its allowance {allowance}");
                    }
                }
            }
        }

        // ---------- T4 ----------

        [Test]
        public void T4_NoLandIsCarvedBelowTheWaterline()
        {
            var sea = WorldScale.SeaLevelMeters;
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var surface = surfaces[s].Heights;
                var sd = w.Stage1.SignedDistance;
                for (var i = 0; i < surface.Length; i++)
                {
                    if (sd[i] <= 0f || w.H4A[i] <= sea)
                    {
                        continue;
                    }

                    Assert.GreaterOrEqual(
                        surface[i], sea,
                        $"seed {w.Seed}: sample {i} was carved from {w.H4A[i]} to {surface[i]}, under the waterline");
                }
            }
        }

        // ---------- T5 ----------

        [Test]
        public void T5_PointQueryEqualsTheLatticeToTheBit()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var field = w.Stage1.Field;
                var count = field.SampleCount;
                var samples = s == 0 ? count : 10000;
                var mismatches = 0;
                var first = string.Empty;

                for (var k = 0; k < samples; k++)
                {
                    var i = s == 0 ? k : (int)((k * 104729L + 7919L * (s + 1)) % count);
                    var x = i % field.Width;
                    var z = i / field.Width;
                    var worldX = field.WorldX(x);
                    var worldZ = field.WorldZ(z);

                    var point = CarvedElevation.At(
                        w.Stage1, w.Uplift, w.Relief, w.Carve, settings, worldX, worldZ);
                    if (point == surfaces[s].Heights[i])
                    {
                        continue;
                    }

                    if (mismatches == 0)
                    {
                        first = $"sample {i} at {worldX},{worldZ}: point {point} vs lattice {surfaces[s].Heights[i]}";
                    }

                    mismatches++;
                }

                Assert.AreEqual(0, mismatches, $"seed {w.Seed}: {mismatches} of {samples} samples differ; first {first}");
            }
        }

        // ---------- T6 ----------

        [Test]
        public void T6_TwoBuildsInOneProcessAreIdentical()
        {
            foreach (var index in new[] { 0, 4 })
            {
                var w = worlds[index];
                var again = CarvedElevation.EvaluateField(
                    w.Stage1, w.H4AField, w.Carve, settings, out var carveAgain);
                Assert.AreEqual(surfaces[index].Hash(), again.Hash(), $"seed {w.Seed}: the surface moved between builds");
                Assert.AreEqual(carves[index].Hash(), carveAgain.Hash(), $"seed {w.Seed}: the carve moved between builds");
            }
        }

        // ---------- T7 ----------

        [Test]
        public void T7_Stage4AAndTheFrozenStagesAreUntouched()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];

                // Stage 4A's field is the input, and composing on top of it must not have moved it.
                Assert.AreEqual(w.Stage1HashBefore, w.Stage1.Field.Hash(), $"seed {w.Seed}: Stage 1 field changed");
                Assert.AreEqual(w.Stage2BHashBefore, w.Stage2B.Field.Hash(), $"seed {w.Seed}: Stage 2B field changed");
                Assert.AreEqual(w.UpliftHashBefore, w.Stage2B.UpliftField.Hash(), $"seed {w.Seed}: Stage 2B uplift changed");

                var relief = w.Relief.EvaluateField(w.Stage1.Field, w.Stage2B.UpliftField, w.Stage1.SignedDistance);
                Assert.AreEqual(
                    w.ReliefField.Hash(), relief.Hash(), $"seed {w.Seed}: Stage 4A relief changed under the carve");

                for (var i = 0; i < w.H4A.Length; i++)
                {
                    // The sum goes through a float local on purpose. C# may evaluate a float
                    // expression at higher precision and is only obliged to round at an assignment,
                    // so comparing the stored H4A against an inline sum compares a rounded value
                    // against an unrounded one and disagrees on about a third of the lattice.
                    float sum = w.H2[i] + w.ReliefField.Heights[i];
                    if (w.H4A[i] != sum)
                    {
                        Assert.Fail($"seed {w.Seed}: H4A is no longer H2 + relief at sample {i}");
                    }
                }
            }
        }

        // ---------- T8 ----------

        [Test]
        public void T8_TheCarveCutsSomethingOnEverySeedThatHasACorridor()
        {
            var report = new List<string>();
            for (var index = 0; index < worlds.Length; index++)
            {
                var w = worlds[index];
                var carve = carves[index].Heights;
                var max = 0f;
                var touched = 0;
                for (var i = 0; i < carve.Length; i++)
                {
                    max = Mathf.Max(max, carve[i]);
                    if (carve[i] > 0.01f)
                    {
                        touched++;
                    }
                }

                report.Add($"seed {w.Seed}: {w.Ridges.Valleys.Count} corridors, max carve {max:F2} m, {touched} samples touched");
                if (w.Ridges.Valleys.Count > 0)
                {
                    Assert.Greater(
                        max, 0f,
                        $"seed {w.Seed} has {w.Ridges.Valleys.Count} corridors but the carve removed nothing");
                }
            }

            TestContext.WriteLine(string.Join("\n", report));
        }

        // ---------- T9 ----------


        [Test]
        public void T9_TileIndependence_EdgeSamplesAgreeFromEitherSide()
        {
            // The production lattice is the Stage 1 lattice, and a tile edge is a shared column of it.
            // Evaluating those columns as a pure function of world position must give the lattice value,
            // which is what makes two tiles that meet there write the same height.
            const int tileCells = 512;
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var field = w.Stage1.Field;
                var mismatches = 0;
                for (var z = 0; z < field.Height; z++)
                {
                    foreach (var x in new[] { tileCells, field.Width - 1 })
                    {
                        if (x >= field.Width)
                        {
                            continue;
                        }

                        var i = z * field.Width + x;
                        var value = CarvedElevation.At(
                            w.Stage1, w.Uplift, w.Relief, w.Carve, settings, field.WorldX(x), field.WorldZ(z));
                        if (value != surfaces[s].Heights[i])
                        {
                            mismatches++;
                        }
                    }
                }

                Assert.AreEqual(0, mismatches, $"seed {w.Seed}: {mismatches} tile-edge samples disagree with the lattice");
            }
        }
    }
}
