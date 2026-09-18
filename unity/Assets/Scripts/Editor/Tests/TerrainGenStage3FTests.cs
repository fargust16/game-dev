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
        private CarveField[] models;

        /// <summary>
        /// The trajectory list each carve was built from, captured here because two tests below rebuild
        /// Stage 3 on the shared world and replace <c>w.Trajectories</c> with fresh instances.
        /// </summary>
        private IReadOnlyList<FinalValleyTrajectory>[] built;

        private RidgeSettings settings;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            settings = RidgeSettings.Default;
            settings.Clamp();

            worlds = new IslandReliefWorld[Seeds.Length];
            surfaces = new WorldHeightField[Seeds.Length];
            carves = new WorldHeightField[Seeds.Length];
            models = new CarveField[Seeds.Length];
            built = new IReadOnlyList<FinalValleyTrajectory>[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                worlds[i] = IslandReliefWorld.Build(Seeds[i], IslandReliefSettings.Default, withStage3: true);

                // F1: the carve is keyed to Slice C's settled trajectories, not to the corridors.
                built[i] = worlds[i].Trajectories.Trajectories;
                models[i] = worlds[i].Trajectories.BuildCarve(
                    worlds[i].Stage1.Coast, settings, worlds[i].Stage2B.MassifSettings);
                surfaces[i] = CarvedElevation.EvaluateField(
                    worlds[i].Stage1, worlds[i].H4AField, models[i], settings, out var carve);
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
                        w.Stage1, w.Uplift, w.Relief, models[s], settings, worldX, worldZ);
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
                    w.Stage1, w.H4AField, models[index], settings, out var carveAgain);
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
                            w.Stage1, w.Uplift, w.Relief, models[s], settings, field.WorldX(x), field.WorldZ(z));
                        if (value != surfaces[s].Heights[i])
                        {
                            mismatches++;
                        }
                    }
                }

                Assert.AreEqual(0, mismatches, $"seed {w.Seed}: {mismatches} tile-edge samples disagree with the lattice");
            }
        }

        // ---------- F1 ----------

        [Test]
        public void F1_TheCarveIsKeyedToSliceCTrajectoriesNotToCorridors()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var curves = models[s].Curves;
                Assert.AreEqual(
                    built[s].Count, curves.Count,
                    $"seed {w.Seed}: the carve holds {curves.Count} curves for {built[s].Count} trajectories");

                for (var i = 0; i < curves.Count; i++)
                {
                    Assert.IsInstanceOf<FinalValleyTrajectory>(
                        curves[i], $"seed {w.Seed}: curve {i} is not a Slice C trajectory");
                    Assert.AreSame(
                        built[s][i], curves[i],
                        $"seed {w.Seed}: curve {i} is not the trajectory Slice C built");
                    Assert.IsNotInstanceOf<ValleyCorridor>(
                        curves[i], $"seed {w.Seed}: curve {i} is still a legacy corridor");
                }
            }
        }

        [Test]
        public void F1_CorridorAndTrajectoryStayOneToOneWithTheSameIdentity()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var corridors = w.Ridges.Valleys;
                var trajectories = w.Trajectories.Trajectories;
                Assert.AreEqual(
                    corridors.Count, trajectories.Count,
                    $"seed {w.Seed}: {corridors.Count} corridors but {trajectories.Count} trajectories");

                for (var i = 0; i < corridors.Count; i++)
                {
                    Assert.AreEqual(corridors[i].Id, trajectories[i].Id, $"seed {w.Seed}: id moved at {i}");
                    Assert.AreEqual(
                        corridors[i].MassifIndex, trajectories[i].MassifIndex, $"seed {w.Seed}: massif moved at {i}");
                    Assert.AreEqual(
                        corridors[i].FlankSign, trajectories[i].FlankSign, $"seed {w.Seed}: flank moved at {i}");
                    Assert.AreEqual(
                        corridors[i].HeadS, trajectories[i].HeadS, 0f,
                        $"seed {w.Seed}: HeadS was not carried across unchanged at {i}");
                }
            }
        }

        [Test]
        public void F1_TrajectoryProjectionIsDeterministicAndInsideItsOwnArc()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var random = new System.Random(4711 + s);
                foreach (var traj in w.Trajectories.Trajectories)
                {
                    if (!traj.HasGeometry)
                    {
                        continue;
                    }

                    for (var k = 0; k < 200; k++)
                    {
                        var p = new Vector2(
                            (float)random.NextDouble() * WorldScale.WorldExtentXMeters,
                            (float)random.NextDouble() * WorldScale.WorldExtentZMeters);

                        var ok = traj.Project(p, out var arc, out var across);
                        Assert.IsTrue(ok, $"seed {w.Seed}: trajectory {traj.Id} refused to project a world point");
                        Assert.IsTrue(traj.Project(p, out var arc2, out var across2));
                        Assert.AreEqual(arc, arc2, 0f, $"seed {w.Seed}: projection arc is not deterministic");
                        Assert.AreEqual(across, across2, 0f, $"seed {w.Seed}: projection across is not deterministic");

                        Assert.GreaterOrEqual(arc, 0f, $"seed {w.Seed}: arc {arc} is before the head");
                        Assert.LessOrEqual(
                            arc, traj.LengthMeters + 1e-3f,
                            $"seed {w.Seed}: arc {arc} runs past the trajectory end {traj.LengthMeters}");
                        Assert.GreaterOrEqual(across, 0f, $"seed {w.Seed}: across {across} is negative");
                    }
                }
            }
        }

        [Test]
        public void F1_CarveIsConfinedToTheTrajectorySupport()
        {
            // Nothing outside a trajectory's own half-width may be carved: the projection is a local
            // frame, not a nearest-point field, so a point no curve covers has to come back zero.
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var carve = carves[s].Heights;
                var field = w.Stage1.Field;
                var outside = 0;
                for (var i = 0; i < carve.Length; i += 13)
                {
                    if (carve[i] <= 0.01f)
                    {
                        continue;
                    }

                    var p = new Vector2(field.WorldX(i % field.Width), field.WorldZ(i / field.Width));
                    var covered = false;
                    foreach (var traj in w.Trajectories.Trajectories)
                    {
                        if (!traj.HasGeometry || !traj.Project(p, out _, out var across))
                        {
                            continue;
                        }

                        if (across < traj.HalfWidthMeters)
                        {
                            covered = true;
                            break;
                        }
                    }

                    if (!covered)
                    {
                        outside++;
                    }
                }

                Assert.AreEqual(
                    0, outside, $"seed {w.Seed}: {outside} carved samples lie outside every trajectory's half-width");
            }
        }

        [Test]
        public void F1_MassifSeedsCarveAndMassifFreeSeedsDoNot()
        {
            var report = new List<string>();
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var max = 0f;
                foreach (var v in carves[s].Heights)
                {
                    max = Mathf.Max(max, v);
                }

                report.Add($"seed {w.Seed}: massifs {w.Stage2B.Massifs.Count}, trajectories {w.Trajectories.Trajectories.Count}, max carve {max:F2} m");

                if (w.Stage2B.Massifs.Count == 0)
                {
                    Assert.AreEqual(
                        0, w.Trajectories.Trajectories.Count,
                        $"seed {w.Seed}: a world with no massif grew {w.Trajectories.Trajectories.Count} trajectories");
                    Assert.AreEqual(0f, max, $"seed {w.Seed}: a world with no corridor was carved");
                    Assert.AreEqual(
                        w.H4AField.Hash(), surfaces[s].Hash(),
                        $"seed {w.Seed}: H3F moved on a world with no valleys");
                }
                else
                {
                    Assert.Greater(max, 0f, $"seed {w.Seed}: a world with corridors was not carved");
                }
            }

            TestContext.WriteLine(string.Join("\n", report));
        }

        [Test]
        public void F1_TheAllowanceIsUnchangedFromF0()
        {
            // F1 moved where the carve is evaluated, not how much carving is allowed. The allowance is
            // still Stage3Field's, and the raw depth is still what the budget clamps.
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var field = w.Stage1.Field;
                var bound = settings.CorridorIncisionMeters;
                for (var i = 0; i < w.H4A.Length; i += 997)
                {
                    var worldX = field.WorldX(i % field.Width);
                    var worldZ = field.WorldZ(i / field.Width);
                    var inland = w.Stage1.SignedDistance[i];
                    var raw = models[s].Depth(worldX, worldZ, models[s].GateFor(inland), inland > 0f);
                    var expected = CarvedElevation.CarveAt(raw, w.H4A[i], field.Heights[i], settings);

                    Assert.AreEqual(expected, carves[s].Heights[i], 0f, $"seed {w.Seed}: allowance moved at sample {i}");

                    var allowance = Mathf.Max(0f, w.H4A[i] - field.Heights[i]) + bound;
                    Assert.LessOrEqual(carves[s].Heights[i], allowance + 1e-3f, $"seed {w.Seed}: over allowance at {i}");
                }
            }
        }

        [Test]
        public void F1_Stage3CTopologyAndTrajectoryGeometryAreUnchanged()
        {
            // The scoped unfreeze added queries and carried HeadS across. It must not have moved a
            // single trajectory sample, a termination, a width or the merge structure.
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var before = Checksum(w.Trajectories);
                IslandReliefWorld.BuildStage3(w);
                Assert.AreEqual(
                    before, Checksum(w.Trajectories), $"seed {w.Seed}: Slice C geometry moved when rebuilt");
            }
        }

        private static string Checksum(ValleyTrajectoryNetwork network)
        {
            var h = 17L;
            foreach (var t in network.Trajectories)
            {
                h = unchecked(h * 31 + t.Id);
                h = unchecked(h * 31 + t.MassifIndex);
                h = unchecked(h * 31 + t.FlankSign);
                h = unchecked(h * 31 + t.ToeIndex);
                h = unchecked(h * 31 + t.DownstreamId);
                h = unchecked(h * 31 + (int)t.Termination);
                h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(t.HalfWidthMeters));
                h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(t.HeadS));
                foreach (var c in t.ConfluenceIndices)
                {
                    h = unchecked(h * 31 + c);
                }

                foreach (var p in t.Points)
                {
                    h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(p.x));
                    h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(p.y));
                }
            }

            return $"{network.Trajectories.Count}:{h}";
        }
    }
}
