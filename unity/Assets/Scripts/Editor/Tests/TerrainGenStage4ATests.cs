using System;
using System.Collections.Generic;
using GameDev.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 4A: the island-floor relief (PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md §19.2,
    /// revision R1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert contracts, never expected metric values: zero at and seaward of the shore, the land
    /// mask untouched, bounds, point query equal to the lattice, determinism, the frozen stages
    /// untouched, stream isolation, no closed depression, the knoll and apron bounds, apron
    /// monotonicity, and compatibility with the frozen Stage 3C trajectories and Stage 3E outlets. The
    /// §11 numerical targets are measured by <see cref="TerrainGenIslandReliefDump"/>.
    /// </para>
    /// <para>
    /// Each canonical seed costs a Stage 2B generation (six to eight seconds) and one relief lattice, so
    /// all seven are built once in <c>OneTimeSetUp</c>.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage4ATests
    {
        private static readonly int[] Seeds = TerrainGenIslandReliefDump.CanonicalSeeds;

        /// <summary>Stage 2B field hashes recorded before Stage 4A existed (preflight Stage 2B report).</summary>
        private static readonly Dictionary<int, string> RecordedStage2BHashes = new Dictionary<int, string>
        {
            { 110201, "1de91e469513054b" },
            { 424242, "9d9d7b3c2560bc36" },
        };

        private IslandReliefWorld[] worlds;

        [OneTimeSetUp]
        public void BuildWorlds()
        {
            worlds = new IslandReliefWorld[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                worlds[i] = IslandReliefWorld.Build(Seeds[i], IslandReliefSettings.Default, withStage3: true);
            }
        }

        // ---------- S1 ----------

        [Test]
        public void S1_ReliefIsExactlyZeroAtAndSeawardOfTheShore()
        {
            foreach (var w in worlds)
            {
                var sd = w.Stage1.SignedDistance;
                var nonZero = 0;
                var sampleNonZero = 0;
                var sea = 0;
                for (var i = 0; i < sd.Length; i++)
                {
                    if (sd[i] > 0f)
                    {
                        continue;
                    }

                    sea++;
                    if (w.ReliefField.Heights[i] != 0f)
                    {
                        nonZero++;
                    }

                    if (w.SampleTotal[i] != 0f || w.Apron[i] != 0f || w.Gate[i] != 0f)
                    {
                        sampleNonZero++;
                    }
                }

                Assert.Greater(sea, 0, $"seed {w.Seed}: no sea samples");
                Assert.AreEqual(0, nonZero, $"seed {w.Seed}: {nonZero} of {sea} samples at or seaward of the shore carry relief");
                Assert.AreEqual(0, sampleNonZero, $"seed {w.Seed}: {sampleNonZero} sea samples have a non-zero gate, apron or total");
            }
        }

        // ---------- S2 ----------

        [Test]
        public void S2_TheLandMaskIsUnchanged()
        {
            var sea = WorldScale.SeaLevelMeters;
            foreach (var w in worlds)
            {
                var changed = 0;
                for (var i = 0; i < w.H2.Length; i++)
                {
                    if ((w.H4A[i] > sea) != (w.H2[i] > sea))
                    {
                        changed++;
                    }
                }

                Assert.AreEqual(0, changed, $"seed {w.Seed}: the relief moved the coastline at {changed} samples");
            }
        }

        // ---------- S3 ----------

        [Test]
        public void S3_ReliefIsBoundedAndFinite()
        {
            foreach (var w in worlds)
            {
                var max = w.Relief.Settings.MaxReliefMeters;
                for (var i = 0; i < w.H4A.Length; i++)
                {
                    var r = w.ReliefField.Heights[i];
                    if (float.IsNaN(r) || float.IsInfinity(r) || r < 0f || r > max)
                    {
                        Assert.Fail($"seed {w.Seed}: relief {r} at sample {i} outside [0, {max}]");
                    }

                    var h = w.H4A[i];
                    if (float.IsNaN(h) || float.IsInfinity(h) || h > WorldScale.TerrainHeightBudgetMeters)
                    {
                        Assert.Fail($"seed {w.Seed}: H4A {h} at sample {i} outside the height budget");
                    }
                }
            }
        }

        // ---------- S4 ----------

        [Test]
        public void S4_PointQueryEqualsTheLatticeToTheBit()
        {
            for (var s = 0; s < worlds.Length; s++)
            {
                var w = worlds[s];
                var field = w.Stage1.Field;
                var count = field.SampleCount;
                var samples = s == 0 ? count : 10000;
                var mismatches = 0;
                var firstMismatch = string.Empty;

                for (var k = 0; k < samples; k++)
                {
                    var i = s == 0 ? k : (int)((k * 104729L + 7919L * (s + 1)) % count);
                    var x = i % field.Width;
                    var z = i / field.Width;
                    var worldX = field.WorldX(x);
                    var worldZ = field.WorldZ(z);

                    var signedDistance = w.Stage1.Coast.SignedDistance(worldX, worldZ);
                    var h1 = w.Stage1.Model.HeightAt(signedDistance);
                    var u2 = w.Uplift.UpliftAt(worldX, worldZ, signedDistance);
                    var point = w.Relief.At(worldX, worldZ, signedDistance, h1, u2);
                    var elevation = IslandElevation.At(w.Stage1, w.Uplift, w.Relief, worldX, worldZ);
                    var sample = w.Relief.Sample(worldX, worldZ, signedDistance, h1, u2);
                    var floor = h1 <= WorldScale.SeaLevelMeters
                        ? 0f
                        : w.Relief.CoastalFloorAt(signedDistance);

                    var ok = h1 == field.Heights[i]
                             && u2 == w.Stage2B.UpliftField.Heights[i]
                             && point == w.ReliefField.Heights[i]
                             && point == w.SampleTotal[i]
                             && point == sample.Total
                             && floor == sample.CoastalFloor
                             && elevation == w.H4A[i];
                    if (ok)
                    {
                        continue;
                    }

                    if (mismatches == 0)
                    {
                        firstMismatch =
                            $"sample {i}: h1 {h1} vs {field.Heights[i]}, u2 {u2} vs {w.Stage2B.UpliftField.Heights[i]}, "
                            + $"relief {point} vs lattice {w.ReliefField.Heights[i]} vs sample {w.SampleTotal[i]}, "
                            + $"H4A {elevation} vs {w.H4A[i]}";
                    }

                    mismatches++;
                }

                Assert.AreEqual(0, mismatches, $"seed {w.Seed}: {mismatches} of {samples} samples differ; first {firstMismatch}");
            }
        }

        [Test]
        public void S4_TotalIsCoastalFloorPlusGateTimesFloorPlusApron()
        {
            foreach (var w in worlds)
            {
                for (var i = 0; i < w.H2.Length; i += 7)
                {
                    var expected = w.CoastalFloor[i] + w.Gate[i] * (w.Plate[i] + w.Knoll[i]) + w.Apron[i];
                    if (expected != w.SampleTotal[i])
                    {
                        Assert.Fail($"seed {w.Seed} sample {i}: total {w.SampleTotal[i]} is not M+Gate*(Plate+Knoll)+Apron {expected}");
                    }
                }
            }
        }

        // ---------- S5 ----------

        [Test]
        public void S5_TwoBuildsInOneProcessAreIdentical()
        {
            foreach (var index in new[] { 0, 4 })
            {
                var w = worlds[index];
                var again = IslandRelief.Build(
                    new GameDev.World.Seed.WorldSeed(w.Seed), w.Stage1, w.Stage2B.Plates, w.Stage2B.Massifs,
                    IslandReliefSettings.Default);
                var field = again.EvaluateField(w.Stage1.Field, w.Stage2B.UpliftField, w.Stage1.SignedDistance);
                Assert.AreEqual(w.ReliefField.Hash(), field.Hash(), $"seed {w.Seed}: relief hash differs between two builds");
                AssertSameStructure(w.Relief, again, $"seed {w.Seed}");
            }

            // A fully fresh world, Stage 1 onward, for a no-massif seed.
            var fresh = IslandReliefWorld.Build(worlds[4].Seed, IslandReliefSettings.Default, withStage3: false);
            Assert.AreEqual(worlds[4].ReliefField.Hash(), fresh.ReliefField.Hash(), $"seed {fresh.Seed}: relief differs after a fresh generation");
            Assert.AreEqual(worlds[4].H4AField.Hash(), fresh.H4AField.Hash(), $"seed {fresh.Seed}: H4A differs after a fresh generation");
        }

        private static void AssertSameStructure(IslandRelief a, IslandRelief b, string label)
        {
            Assert.AreEqual(a.Plates.Count, b.Plates.Count, label + " plate count");
            for (var k = 0; k < a.Plates.Count; k++)
            {
                Assert.AreEqual(a.Plates[k].Level, b.Plates[k].Level, $"{label} plate {k} level");
                Assert.AreEqual(a.Plates[k].EffectiveIndex, b.Plates[k].EffectiveIndex, $"{label} plate {k} merge");
            }

            Assert.AreEqual(a.Junctions.Count, b.Junctions.Count, label + " junction count");
            for (var j = 0; j < a.Junctions.Count; j++)
            {
                Assert.AreEqual(a.Junctions[j].Index, b.Junctions[j].Index, $"{label} junction {j}");
                Assert.AreEqual(a.Junctions[j].MeanLevel, b.Junctions[j].MeanLevel, $"{label} junction {j} mean");
            }

            Assert.AreEqual(a.Knolls.Count, b.Knolls.Count, label + " knoll count");
            for (var k = 0; k < a.Knolls.Count; k++)
            {
                Assert.AreEqual(a.Knolls[k].SiteIndex, b.Knolls[k].SiteIndex, $"{label} knoll {k} site");
                Assert.AreEqual(a.Knolls[k].HeightMeters, b.Knolls[k].HeightMeters, $"{label} knoll {k} height");
                Assert.AreEqual(a.Knolls[k].YawRadians, b.Knolls[k].YawRadians, $"{label} knoll {k} yaw");
            }
        }

        // ---------- S6 ----------

        [Test]
        public void S6_FrozenStagesAreUntouched()
        {
            foreach (var w in worlds)
            {
                Assert.AreEqual(w.Stage1HashBefore, w.Stage1.Field.Hash(), $"seed {w.Seed}: Stage 1 field changed");
                Assert.AreEqual(w.Stage1.Stats.FieldHash, w.Stage1.Field.Hash(), $"seed {w.Seed}: Stage 1 hash disagrees with its own stats");
                Assert.AreEqual(w.Stage2BHashBefore, w.Stage2B.Field.Hash(), $"seed {w.Seed}: Stage 2B field changed");
                Assert.AreEqual(w.UpliftHashBefore, w.Stage2B.UpliftField.Hash(), $"seed {w.Seed}: Stage 2B uplift changed");

                if (RecordedStage2BHashes.TryGetValue(w.Seed, out var recorded))
                {
                    Assert.AreEqual(recorded, w.Stage2B.Stats.FieldHash, $"seed {w.Seed}: Stage 2B hash moved from its recorded value");
                }

                // Stage 3C and 3E rebuilt after the relief exists must be exactly what they were before it.
                var before = TrajectoryChecksum(w);
                var basinsBefore = BasinChecksum(w);
                IslandReliefWorld.BuildStage3(w);
                Assert.AreEqual(before, TrajectoryChecksum(w), $"seed {w.Seed}: Stage 3C trajectories changed");
                Assert.AreEqual(basinsBefore, BasinChecksum(w), $"seed {w.Seed}: Stage 3E basins changed");
            }
        }

        private static string TrajectoryChecksum(IslandReliefWorld w)
        {
            var h = 17L;
            foreach (var t in w.Trajectories.Trajectories)
            {
                h = unchecked(h * 31 + t.Id);
                foreach (var p in t.Points)
                {
                    h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(p.x));
                    h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(p.y));
                }
            }

            return $"{w.Trajectories.Trajectories.Count}:{h}";
        }

        private static string BasinChecksum(IslandReliefWorld w)
        {
            var h = 17L;
            foreach (var b in w.Basins.Basins)
            {
                h = unchecked(h * 31 + b.Id);
                h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(b.OutletPointXZ.x));
                h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(b.OutletPointXZ.y));
                h = unchecked(h * 31 + BitConverter.SingleToInt32Bits(b.OutletElevationMeters));
            }

            return $"{w.Basins.Basins.Count}:{h}";
        }

        // ---------- S7 ----------

        /// <summary>Forwards every stream unchanged except one, which it re-keys.</summary>
        private sealed class OneStreamPerturbed : GameDev.World.Seed.IWorldSeed
        {
            private readonly GameDev.World.Seed.IWorldSeed inner;
            private readonly string stream;

            public OneStreamPerturbed(GameDev.World.Seed.IWorldSeed inner, string stream)
            {
                this.inner = inner;
                this.stream = stream;
            }

            public int Value => inner.Value;

            public System.Random Fork(string streamId) =>
                streamId == stream ? inner.Fork(streamId + "#perturbed") : inner.Fork(streamId);

            public System.Random Fork(string streamId, int a, int b = 0) =>
                streamId == stream ? inner.Fork(streamId + "#perturbed", a, b) : inner.Fork(streamId, a, b);
        }

        [Test]
        public void S7_OnlyTheKnollStreamMovesTheKnolls()
        {
            foreach (var index in new[] { 0, 4 })
            {
                var w = worlds[index];
                var perturbed = IslandRelief.Build(
                    new OneStreamPerturbed(new GameDev.World.Seed.WorldSeed(w.Seed), IslandRelief.KnollStream),
                    w.Stage1, w.Stage2B.Plates, w.Stage2B.Massifs, IslandReliefSettings.Default);

                Assert.AreEqual(w.Relief.Plates.Count, perturbed.Plates.Count);
                for (var k = 0; k < w.Relief.Plates.Count; k++)
                {
                    Assert.AreEqual(w.Relief.Plates[k].Level, perturbed.Plates[k].Level, $"seed {w.Seed}: plate {k} level moved");
                }

                Assert.AreEqual(w.Relief.Junctions.Count, perturbed.Junctions.Count, $"seed {w.Seed}: junctions moved");

                var knollChanged = false;
                var field = w.Stage1.Field;
                for (var i = 0; i < field.SampleCount; i += 11)
                {
                    var x = field.WorldX(i % field.Width);
                    var z = field.WorldZ(i / field.Width);
                    var original = w.Relief.Sample(x, z, w.Stage1.Coast.SignedDistance(x, z), field.Heights[i], w.Stage2B.UpliftField.Heights[i]);
                    var moved = perturbed.Sample(x, z, w.Stage1.Coast.SignedDistance(x, z), field.Heights[i], w.Stage2B.UpliftField.Heights[i]);
                    Assert.AreEqual(original.Plate, moved.Plate, $"seed {w.Seed}: plate term moved at sample {i}");
                    Assert.AreEqual(original.PlateLevel, moved.PlateLevel, $"seed {w.Seed}: level moved at sample {i}");
                    Assert.AreEqual(original.Apron, moved.Apron, $"seed {w.Seed}: apron moved at sample {i}");
                    Assert.AreEqual(original.Gate, moved.Gate, $"seed {w.Seed}: gate moved at sample {i}");
                    Assert.AreEqual(original.CoastalFloor, moved.CoastalFloor, $"seed {w.Seed}: coastal floor moved at sample {i}");
                    knollChanged |= original.Knoll != moved.Knoll;
                }

                var drawsChanged = false;
                for (var k = 0; k < w.Relief.KnollCandidates.Count; k++)
                {
                    drawsChanged |= w.Relief.KnollCandidates[k].Resistance != perturbed.KnollCandidates[k].Resistance;
                }

                Assert.IsTrue(drawsChanged, $"seed {w.Seed}: re-keying the knoll stream changed no draw");
                if (w.Relief.Knolls.Count > 0 || perturbed.Knolls.Count > 0)
                {
                    Assert.IsTrue(knollChanged, $"seed {w.Seed}: re-keying the knoll stream moved no knoll");
                }
            }
        }

        // ---------- S8 ----------

        [Test]
        public void S8A_NoQualifyingDepressionsOutsideMassifOwnership()
        {
            var report = new List<string>();
            var failures = 0;
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                report.Add($"seed {w.Seed}: S8-A {checks.S8ACount} (deepest {checks.S8AMaxDepth:F2} m, {checks.S8AAreaSqm:F0} m2)");
                if (checks.S8ACount > 0)
                {
                    failures++;
                    report.AddRange(IslandReliefChecks.DepressionAttribution(w));
                }
            }

            TestContext.WriteLine(string.Join("\n", report));
            Assert.AreEqual(0, failures, "S8-A qualifying depressions outside Z_M ∪ L_M:\n  " + string.Join("\n  ", report));
        }

        [Test]
        public void S8M_NoQualifyingDepressionsInsideMassifOwnershipOnCanonicalSeeds()
        {
            var report = new List<string>();
            var failures = 0;
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                report.Add($"seed {w.Seed}: S8-M {checks.S8MCount} (deepest {checks.S8MMaxDepth:F2} m, {checks.S8MAreaSqm:F0} m2)");
                if (checks.S8MCount > 0)
                {
                    failures++;
                    report.AddRange(IslandReliefChecks.DepressionAttribution(w));
                }
            }

            TestContext.WriteLine(string.Join("\n", report));
            Assert.AreEqual(0, failures, "S8-M qualifying depressions inside Z_M ∪ L_M on canonical seeds:\n  " + string.Join("\n  ", report));
        }

        [Test]
        public void S8C_CoastNormalDerivativeOutsideOwnership()
        {
            var failures = new List<string>();
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                TestContext.WriteLine(
                    $"seed {w.Seed}: S8-C min deriv {checks.S8CMinDerivative:E3} over {checks.S8CSamples} samples, beyond-800 {checks.LandBeyond800Percent:F2}%");
                if (checks.S8CViolations > 0)
                {
                    failures.Add($"seed {w.Seed}: {checks.S8CViolations} samples with ∂B/∂n < -1e-3 (min {checks.S8CMinDerivative:E3})");
                }
            }

            Assert.IsEmpty(failures, string.Join("\n  ", failures));
        }

        // ---------- S9 ----------

        [Test]
        public void S9_KnollsAndApronStayWithinTheirBounds()
        {
            foreach (var w in worlds)
            {
                var cap = w.Relief.Settings.KnollHeightCapMeters;
                Assert.LessOrEqual(cap, RidgeSettings.Default.CorridorIncisionMeters + 1e-5f, "knoll cap above the corridor incision");
                foreach (var knoll in w.Relief.Knolls)
                {
                    Assert.LessOrEqual(knoll.HeightMeters, cap, $"seed {w.Seed}: knoll at site {knoll.SiteIndex}");
                }

                var apronCap = 0f;
                foreach (var m in w.Relief.ApronMassifs)
                {
                    apronCap = Mathf.Max(apronCap, w.Relief.Settings.ApronCapPerCrest * m.CrestHeightMeters);
                }

                for (var i = 0; i < w.H2.Length; i++)
                {
                    if (w.Knoll[i] > cap)
                    {
                        Assert.Fail($"seed {w.Seed}: knoll term {w.Knoll[i]} above the cap at sample {i}");
                    }

                    if (w.Apron[i] > apronCap + 1e-3f)
                    {
                        Assert.Fail($"seed {w.Seed}: apron {w.Apron[i]} above 0.18 x crest ({apronCap}) at sample {i}");
                    }
                }

                if (!w.Relief.HasMassif)
                {
                    Assert.AreEqual(0f, Max(w.Apron), $"seed {w.Seed}: apron on a world without a massif");
                }
            }
        }

        private static float Max(float[] values)
        {
            var max = 0f;
            foreach (var v in values)
            {
                max = Mathf.Max(max, v);
            }

            return max;
        }

        // ---------- S10 ----------

        [Test]
        public void S10_UngatedApronPhysicalProfile()
        {
            var failures = new List<string>();
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                TestContext.WriteLine(
                    $"seed {w.Seed}: S10 beyond-domain {checks.ApronBeyondDomainMax:F4} m ({checks.ApronBeyondDomainSamples} samples); "
                    + $"normals {checks.ApronNormalViolations}/{checks.ApronNormalRays} worst {checks.ApronNormalWorstRise:F4} m; "
                    + $"gated-fill (report) {checks.ApronRayViolations} worst {checks.ApronRayWorstRise:F4} m");
                if (checks.ApronBeyondDomainMax > 0.001f || checks.ApronNormalWorstRise > 0.001f)
                {
                    failures.Add(
                        $"seed {w.Seed}: P_A beyond domain {checks.ApronBeyondDomainMax:F4} m, "
                        + $"normal rises {checks.ApronNormalViolations} worst {checks.ApronNormalWorstRise:F4} m");
                }
            }

            Assert.IsEmpty(failures, string.Join("\n  ", failures));
        }

        // ---------- S11 ----------

        [Test]
        public void S11T_ValleyTrajectoriesStayCarvable()
        {
            var failures = new List<string>();
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                TestContext.WriteLine(
                    $"seed {w.Seed}: C(R4A) {checks.TrajectoryWorstClimb:F2} m, plate+apron {checks.TrajectoryPlateApronRise:F2} m, "
                    + $"I1 Δ {checks.TrajectoryI1Increase:F4} m, R4A adverse sum {checks.TrajectoryReliefRise:F2} m (report)");
                var pass = checks.TrajectoryPlateApronRise <= 1.5f
                           && checks.TrajectoryWorstClimb <= 7.2f + 1e-3f
                           && checks.TrajectoryI1Increase <= 0.001f;
                if (!pass)
                {
                    failures.Add(
                        $"seed {w.Seed}: C(R4A) {checks.TrajectoryWorstClimb:F2} m (<= 7.2), plate+apron {checks.TrajectoryPlateApronRise:F2} m (<= 1.5), "
                        + $"I1 increase {checks.TrajectoryI1Increase:F4} m\n    " + string.Join("\n    ", checks.TrajectoryLines));
                }
            }

            Assert.IsEmpty(failures, string.Join("\n  ", failures));
        }

        [Test]
        public void S11O_BasinOutletsStayOutlets()
        {
            var failures = new List<string>();
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                TestContext.WriteLine(
                    $"seed {w.Seed}: O-1 fail {checks.OutletO1Failures}/{checks.Outlets}; "
                    + $"O-2 {(checks.CarveMeasurable ? checks.OutletO2Failures.ToString() : checks.CarveNote)} / {checks.OutletO2Checked}; "
                    + $"O-3 fail {checks.OutletO3Failures}; O-4 shift {checks.OutletMinShift:F1} m");
                if (checks.OutletO1Failures > 0 || checks.OutletO3Failures > 0
                    || (checks.CarveMeasurable && checks.OutletO2Failures > 0))
                {
                    failures.Add(
                        $"seed {w.Seed}: O-1 {checks.OutletO1Failures}, O-2 {checks.OutletO2Failures} (measurable {checks.CarveMeasurable}), O-3 {checks.OutletO3Failures}\n    "
                        + string.Join("\n    ", checks.OutletLines));
                }
            }

            Assert.IsEmpty(failures, string.Join("\n  ", failures));
        }

        [Test]
        public void S12_JunctionCertificatesPass()
        {
            var failures = new List<string>();
            foreach (var w in worlds)
            {
                var checks = IslandReliefChecks.Run(w);
                TestContext.WriteLine($"seed {w.Seed}: S12 {checks.S12Failures} fail / {w.Relief.Junctions.Count} junctions");
                if (checks.S12Failures > 0)
                {
                    failures.Add($"seed {w.Seed}: {checks.S12Failures} junction certificates failed");
                }
            }

            Assert.IsEmpty(failures, string.Join("\n  ", failures));
        }
    }
}
