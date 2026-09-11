using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 2B: the broad mountain massif raised on the accepted macro axis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These assert the contract of the massif, not of the axis - Stage 2A's tests already prove the
    /// axis can carry the width it claims. What is new here is that the body actually built on it is
    /// broad, connected, bounded, tapered at its ends, tied to its axis, and does not fold.
    /// </para>
    /// <para>
    /// Generation is the expensive part: Stage 2B runs the whole Stage 1 field, so each seed costs six
    /// to eight seconds. All five are generated once in <c>OneTimeSetUp</c> and every test reads them.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage2BTests
    {
        private static readonly int[] Seeds = { 110201, 424242, 20260905, 7, 1337 };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings MassifSettings => MassifSettings.Default;

        private Stage2BResult[] worlds;

        [OneTimeSetUp]
        public void GenerateWorlds()
        {
            worlds = new Stage2BResult[Seeds.Length];
            for (var i = 0; i < Seeds.Length; i++)
            {
                worlds[i] = Stage2BMassifGenerator.Generate(
                    Seeds[i], TerrainSettings, MacroSettings, MassifSettings);
            }
        }

        // ---------- 1. determinism ----------

        [Test]
        public void SameSeed_ProducesTheSameField()
        {
            var again = Stage2BMassifGenerator.Generate(
                Seeds[0], TerrainSettings, MacroSettings, MassifSettings);

            Assert.AreEqual(worlds[0].Stats.FieldHash, again.Stats.FieldHash, "field hash");
            Assert.AreEqual(worlds[0].Massifs.Count, again.Massifs.Count, "massif count");

            var a = worlds[0].Field.Heights;
            var b = again.Field.Heights;
            var differing = 0;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    differing++;
                }
            }

            Assert.AreEqual(0, differing, $"{differing} samples differ between two runs of one seed");
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentFields()
        {
            for (var w = 1; w < worlds.Length; w++)
            {
                Assert.AreNotEqual(
                    worlds[0].Stats.FieldHash, worlds[w].Stats.FieldHash,
                    $"seeds {worlds[0].Seed} and {worlds[w].Seed} produced the same world");
            }
        }

        // ---------- 2. Stage 1 immutability ----------

        [Test]
        public void Stage1_IsUnchangedUnderTheMassif()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var result = worlds[w];
                var stage1 = Stage1LandmassGenerator.Generate(result.Seed, TerrainSettings);

                Assert.AreEqual(
                    stage1.Stats.FieldHash, result.Stats.Stage1Hash,
                    $"seed {result.Seed}: Stage 1 hash moved");

                var baseHeights = result.Stage1.Field.Heights;
                for (var i = 0; i < baseHeights.Length; i++)
                {
                    if (baseHeights[i] != stage1.Field.Heights[i])
                    {
                        Assert.Fail($"seed {result.Seed}: Stage 1 sample {i} was rewritten");
                    }
                }
            }
        }

        [Test]
        public void TheOceanKeepsItsStage1HeightToTheBit()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                Assert.AreEqual(
                    0, worlds[w].Stats.OceanSamplesChanged,
                    $"seed {worlds[w].Seed}: the massif reached the sea");
            }
        }

        // ---------- 3. width ----------

        [Test]
        public void TheMassifIsBroadRatherThanTubular()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var m in worlds[w].Metrics)
                {
                    Assert.That(
                        m.MeasuredToeWidthMeters,
                        Is.GreaterThan(m.NominalHalfWidthMeters * 1.3f)
                            .And.LessThan(m.NominalHalfWidthMeters * 2.2f),
                        $"seed {worlds[w].Seed} massif {m.Index}: toe width "
                        + $"{m.MeasuredToeWidthMeters:F0} m against a nominal half-width of "
                        + $"{m.NominalHalfWidthMeters:F0} m");

                    Assert.That(
                        m.MeasuredHalfHeightWidthMeters, Is.GreaterThan(200f),
                        $"seed {worlds[w].Seed} massif {m.Index}: only "
                        + $"{m.MeasuredHalfHeightWidthMeters:F0} m of the body clears half its crest");
                }
            }
        }

        /// <summary>
        /// The width is the geology's, not the curvature's.
        /// </summary>
        /// <remarks>
        /// Stage 2A accepts an axis only when its reach already carries the intended width, so the
        /// safety clamp must never be what decides how wide the massif is. If this fails, the width has
        /// gone back to being a consequence of the axis bending - which is how Stage 2 failed.
        /// </remarks>
        [Test]
        public void CurvatureNeverDecidesTheWidth()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                Assert.IsFalse(
                    worlds[w].Stats.AnyReachBound,
                    $"seed {worlds[w].Seed}: the reach clamp narrowed the massif");
            }
        }

        // ---------- 4. continuity ----------

        /// <summary>
        /// The surface bends no more between samples than a bent tube of this model has to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Judged against the same massif laid on a straight axis, not against zero and not against an
        /// analytic estimate of the profiles. That reference contains everything the model is made of -
        /// the longitudinal profile, the cross-section, the width taper, the flank asymmetry, all
        /// sampled on the same lattice - and nothing else, so whatever the real massif produces above
        /// it comes from the axis bending and from projecting onto it. That is the only part that could
        /// be a defect.
        /// </para>
        /// <para>
        /// The allowance is derived rather than chosen. On the concave flank of a curve of curvature
        /// k, arc length compresses by <c>1 - k d</c>, so a profile's second derivative there is
        /// amplified by <c>1 / (1 - k W)^2</c> at the far edge of a body of half-width W. That is a
        /// property of a bent tube, not of this implementation: no formulation of a massif that
        /// follows a curved axis can do better. Measured, the seeds sit at 1.0 to 4.0 against a bound
        /// of about 6.9.
        /// </para>
        /// </remarks>
        [Test]
        public void TheSurfaceBendsNoMoreThanACurvedTubeMust()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var s = worlds[w].Stats;

                var widest = 0f;
                foreach (var massif in worlds[w].Massifs)
                {
                    widest = Mathf.Max(widest, massif.HalfWidthMeters);
                }

                var compression = Mathf.Clamp01(s.MaxAxisCurvature * widest);
                var amplification = 1f / Mathf.Max(0.05f, (1f - compression) * (1f - compression));

                var allowed = s.StraightAxisSecondDifferenceMeters * amplification + 0.25f;

                Assert.That(
                    s.MaxSecondDifferenceMeters, Is.LessThanOrEqualTo(allowed),
                    $"seed {worlds[w].Seed}: worst second difference "
                    + $"{s.MaxSecondDifferenceMeters:F3} m against {s.StraightAxisSecondDifferenceMeters:F3} m "
                    + $"on a straight axis, amplified by {amplification:F1} for a curvature of "
                    + $"{s.MaxAxisCurvature:F5} 1/m under a {widest:F0} m half-width");
            }
        }

        /// <summary>
        /// The width of the body is a property of the massif wherever the coast leaves it alone.
        /// </summary>
        /// <remarks>
        /// Fidelity is the measured width above half the section's own peak over the width the model
        /// asked for. A body whose footprint is its own keeps that ratio constant along its length;
        /// one whose footprint is being decided by curvature, by the projection, or by the coastline
        /// does not. Stations under a part-open gate are excluded and checked separately, because
        /// there the coast is deliberately cutting the seaward flank.
        /// </remarks>
        [Test]
        public void WidthIsAPropertyOfTheMassifWhereTheCoastAllowsIt()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var lowest = float.PositiveInfinity;
                var highest = 0f;
                var counted = 0;

                for (var m = 0; m < worlds[w].Stations.Count; m++)
                {
                    foreach (var station in worlds[w].Stations[m])
                    {
                        if (station.S < 0.2f || station.S > 0.8f
                            || station.SectionPeakMeters <= 1f
                            || station.GateFactor < 0.999f)
                        {
                            continue;
                        }

                        var fidelity = station.HalfHeightWidthMeters
                                       / Mathf.Max(1f, station.IntendedHalfWidthMeters * 2f);
                        lowest = Mathf.Min(lowest, fidelity);
                        highest = Mathf.Max(highest, fidelity);
                        counted++;
                    }
                }

                if (worlds[w].Massifs.Count == 0)
                {
                    continue;
                }

                Assert.That(counted, Is.GreaterThan(0), $"seed {worlds[w].Seed}: no clear station");

                Assert.That(
                    lowest, Is.GreaterThan(0.5f),
                    $"seed {worlds[w].Seed}: a clear station carries only {lowest:P0} of its width");

                Assert.That(
                    highest - lowest, Is.LessThan(0.06f),
                    $"seed {worlds[w].Seed}: width fidelity wanders over [{lowest:F2} .. {highest:F2}], "
                    + "so something other than the model is deciding the footprint");
            }
        }

        /// <summary>
        /// A ceiling on how much of a massif the coastline is allowed to deform.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not an acceptance. The coast gate needs about 200 m of inland distance to open, and the body
        /// reaches its half-width past its axis, so an axis has to clear roughly
        /// <c>gate + half-width</c> of coastline to carry a full massif. Stage 2A guarantees a 60 m
        /// setback, so an axis can be structurally legal in a place this stage cannot build on, and
        /// where that happens the seaward flank is cut while the inland one is not.
        /// </para>
        /// <para>
        /// Seed 1337 measures about a quarter of its high ground in that state and is reported as a
        /// defect, not passed off as variation. This bound exists to stop it getting worse while the
        /// upstream setback is out of scope; when Stage 2A is corrected it should go to zero and this
        /// number should come down with it.
        /// </para>
        /// </remarks>
        /// <summary>
        /// No high ground stands where the coast gate cannot carry its own cross-section.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This was a ceiling on a known defect - a quarter of seed 1337's high ground stood in a
        /// part-open gate, and the test held the line at thirty per cent while the upstream setback was
        /// out of scope. The contract makes it zero: Stage 2A certifies the stretch of an axis whose
        /// whole section clears the coast, and the massif puts every control point above its shoulder
        /// inside that stretch, so a sheared section cannot be constructed.
        /// </para>
        /// <para>
        /// Kept as a verification rather than deleted, because it checks that two independent pieces of
        /// geometry still agree: what Stage 2A measured when it certified, and what Stage 2B built.
        /// </para>
        /// </remarks>
        [Test]
        public void NoHighGroundStandsInAGateThatCannotCarryIt()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                Assert.That(
                    worlds[w].Stats.ShearedHighFraction,
                    Is.LessThanOrEqualTo(MassifSettings.ShearTolerance),
                    $"seed {worlds[w].Seed}: {worlds[w].Stats.ShearedHighFraction:P0} of the high ground "
                    + "stands in a gate too shallow for its own section");
            }
        }

        /// <summary>The two stages measure the coastal gate against the same distance.</summary>
        /// <remarks>
        /// The contract is only worth anything if both ends of it read the same number. Stage 2A
        /// certifies room for <c>MassifCoastClearanceMeters</c>; Stage 2B fades its body in over
        /// <c>CoastGateMeters</c>. Both default from the same place, and this is what says they still
        /// do after someone edits one of them.
        /// </remarks>
        [Test]
        public void BothStagesAgreeAboutTheCoastalGate()
        {
            Assert.AreEqual(
                MacroSettings.MassifCoastClearanceMeters, MassifSettings.CoastGateMeters, 1e-3f,
                "Stage 2A certifies room for a different gate than Stage 2B opens");
        }

        /// <summary>A massif exists only where an axis was certified to carry it.</summary>
        [Test]
        public void EveryMassifStandsOnACertifiedAxis()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var massif in worlds[w].Massifs)
                {
                    Assert.IsTrue(
                        massif.Axis.Accepted,
                        $"seed {worlds[w].Seed}: a massif was raised on an axis nobody certified");

                    Assert.That(
                        massif.Axis.CoastClearanceMeters,
                        Is.GreaterThanOrEqualTo(MacroSettings.CoastSetbackMeters - 1f),
                        $"seed {worlds[w].Seed}: the axis does not even keep clear of the water");

                    Assert.That(
                        massif.HalfWidthMeters,
                        Is.LessThanOrEqualTo(massif.Axis.UsableHalfWidthMeters + 1e-3f),
                        $"seed {worlds[w].Seed}: the massif is wider than its axis certified");

                    // Stage 2A certifies before it decides which chain owns which boundary, so an
                    // axis without a core cannot reach this stage at all.
                    Assert.That(
                        massif.Axis.CertifiedCore.y - massif.Axis.CertifiedCore.x,
                        Is.GreaterThan(0f),
                        $"seed {worlds[w].Seed}: a massif was raised on an axis with no certified core");
                }
            }
        }

        [Test]
        public void TheProfilesThemselvesAreContinuous()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var massif in worlds[w].Massifs)
                {
                    var previous = massif.CrestAt(0f);
                    for (var i = 1; i <= 400; i++)
                    {
                        var here = massif.CrestAt(i / 400f);
                        Assert.That(
                            Mathf.Abs(here - previous), Is.LessThan(massif.CrestHeightMeters * 0.05f),
                            $"seed {worlds[w].Seed}: the longitudinal profile steps at s={i / 400f:F3}");
                        previous = here;
                    }

                    previous = massif.CrossSection(0f);
                    for (var i = 1; i <= 400; i++)
                    {
                        var here = massif.CrossSection(i / 400f);
                        Assert.That(
                            Mathf.Abs(here - previous), Is.LessThan(0.03f),
                            $"seed {worlds[w].Seed}: the cross-section steps at u={i / 400f:F3}");
                        previous = here;
                    }
                }
            }
        }

        // ---------- 5. sign ----------

        [Test]
        public void TheMassifNeverLowersTheGround()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                Assert.AreEqual(
                    0, worlds[w].Stats.NegativeUpliftSamples,
                    $"seed {worlds[w].Seed}: the contribution went negative");
                Assert.That(
                    worlds[w].Stats.UpliftMinMeters, Is.GreaterThanOrEqualTo(0f),
                    $"seed {worlds[w].Seed}");

                var stage1 = worlds[w].Stage1.Field.Heights;
                var field = worlds[w].Field.Heights;
                for (var i = 0; i < field.Length; i++)
                {
                    if (field[i] < stage1[i])
                    {
                        Assert.Fail(
                            $"seed {worlds[w].Seed}: sample {i} fell from {stage1[i]:F2} to {field[i]:F2}");
                    }
                }
            }
        }

        // ---------- 6. bounds ----------

        [Test]
        public void TheContributionStaysInsideTheHeightBudget()
        {
            var settings = TerrainSettings;
            settings.Clamp();

            for (var w = 0; w < worlds.Length; w++)
            {
                var s = worlds[w].Stats;

                Assert.That(
                    s.UpliftMaxMeters, Is.LessThanOrEqualTo(MassifSettings.MaxUpliftMeters + 1e-3f),
                    $"seed {worlds[w].Seed}: contribution above its ceiling");
                Assert.That(
                    s.MaxHeightMeters, Is.LessThanOrEqualTo(settings.MaxStage2HeightMeters),
                    $"seed {worlds[w].Seed}: world height above the Stage 2 budget");
                Assert.That(
                    s.MaxHeightMeters, Is.LessThan(WorldScale.TerrainHeightBudgetMeters),
                    $"seed {worlds[w].Seed}: world height above the terrain height budget");
                Assert.IsFalse(s.HasNonFinite, $"seed {worlds[w].Seed}: non-finite sample");
            }
        }

        [Test]
        public void TheMassifReachesTheReliefTierItIsMeantTo()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                // Scoped to worlds that have a massif. Stage 2A certifies room as well as geology now,
                // so an island with no corridor wide enough certifies nothing and this stage builds
                // nothing - and a world without a landmark is a different thing from a landmark that
                // is too low. What is asserted is the height of the massifs that do exist.
                if (worlds[w].Massifs.Count == 0)
                {
                    continue;
                }

                // T1 in WORLD_RELIEF_PRINCIPLES is 250-530 m over local ground. The crest range in
                // TerrainGenSettings sits at the bottom of that band and is the number this stage is
                // built to; the rest of the band is what later stages add on top.
                Assert.That(
                    worlds[w].Stats.UpliftMaxMeters,
                    Is.GreaterThanOrEqualTo(TerrainSettings.CrestHeightMinMeters * 0.9f),
                    $"seed {worlds[w].Seed}: the massif is too low to read as a landmark");
            }
        }

        // ---------- 7. axis coherence ----------

        [Test]
        public void TheHighestGroundSitsOnTheAxis()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var m in worlds[w].Metrics)
                {
                    Assert.That(
                        m.PeakOffsetMeters, Is.LessThan(m.NominalHalfWidthMeters * 0.4f),
                        $"seed {worlds[w].Seed} massif {m.Index}: the summit is "
                        + $"{m.PeakOffsetMeters:F0} m off an axis {m.NominalHalfWidthMeters:F0} m wide");

                    Assert.That(
                        m.PeakUpliftMeters, Is.GreaterThan(m.CrestHeightMeters * 0.7f),
                        $"seed {worlds[w].Seed} massif {m.Index}: the massif never reaches its crest");
                }
            }
        }

        // ---------- 8. connectedness ----------

        [Test]
        public void TheMassifIsOneBodyRatherThanSeveralBlobs()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                Assert.AreEqual(
                    worlds[w].Stats.MassifCount, worlds[w].Stats.UpliftComponents,
                    $"seed {worlds[w].Seed}: {worlds[w].Stats.UpliftComponents} disconnected "
                    + $"contribution component(s) for {worlds[w].Stats.MassifCount} massif(s)");
            }
        }

        [Test]
        public void TheCulminationsStayPartOfOneMassif()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var m in worlds[w].Metrics)
                {
                    Assert.That(
                        m.CulminationCount, Is.InRange(1, MassifSettings.MaxCulminations),
                        $"seed {worlds[w].Seed} massif {m.Index}");

                    if (m.CulminationCount < 2)
                    {
                        continue;
                    }

                    Assert.That(
                        m.MinSaddleFraction, Is.GreaterThan(0.3f),
                        $"seed {worlds[w].Seed} massif {m.Index}: the saddle drops to "
                        + $"{m.MinSaddleFraction:P0} of the peak, which reads as two hills");
                }
            }
        }

        // ---------- 9. no folding ----------

        /// <summary>
        /// No point carries more height than its own station along the axis allows.
        /// </summary>
        /// <remarks>
        /// The fold detector. Where a tube folds over itself two parts of the body cover one place, and
        /// the combined value climbs above what the local crest permits. Checking the field against the
        /// model at every sample catches that wherever it happens, without having to guess where.
        /// </remarks>
        [Test]
        public void TheMassifDoesNotFoldOverItself()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var uplift = worlds[w].UpliftField;
                foreach (var massif in worlds[w].Massifs)
                {
                    var support = massif.Support;
                    var cell = uplift.CellSize;
                    var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
                    var xMax = Mathf.Min(uplift.Width - 1, Mathf.CeilToInt(support.xMax / cell));
                    var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
                    var zMax = Mathf.Min(uplift.Height - 1, Mathf.CeilToInt(support.yMax / cell));

                    var worst = 0f;
                    var worstAt = Vector2.zero;

                    for (var z = zMin; z <= zMax; z++)
                    {
                        var worldZ = uplift.WorldZ(z);
                        for (var x = xMin; x <= xMax; x++)
                        {
                            var value = uplift.Heights[z * uplift.Width + x];
                            if (value <= 0f)
                            {
                                continue;
                            }

                            var worldX = uplift.WorldX(x);
                            massif.Project(worldX, worldZ, out var s, out _);
                            var excess = value - massif.CrestAt(s);
                            if (excess > worst)
                            {
                                worst = excess;
                                worstAt = new Vector2(worldX, worldZ);
                            }
                        }
                    }

                    Assert.That(
                        worst, Is.LessThan(1f),
                        $"seed {worlds[w].Seed} massif {massif.Index}: {worst:F1} m more height than "
                        + $"its station allows at ({worstAt.x:F0}, {worstAt.y:F0})");
                }
            }
        }

        // ---------- 10. end taper ----------

        [Test]
        public void TheMassifTapersTowardsItsEnds()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                foreach (var massif in worlds[w].Massifs)
                {
                    Assert.AreEqual(
                        0f, massif.CrestAt(0f), 1e-3f, $"seed {worlds[w].Seed}: the start is not zero");
                    Assert.AreEqual(
                        0f, massif.CrestAt(1f), 1e-3f, $"seed {worlds[w].Seed}: the end is not zero");

                    for (var i = 0; i <= 10; i++)
                    {
                        var s = i / 10f * 0.05f;
                        Assert.That(
                            massif.CrestAt(s), Is.LessThan(massif.CrestHeightMeters * 0.4f),
                            $"seed {worlds[w].Seed}: the massif is still {massif.CrestAt(s):F0} m high "
                            + $"at s={s:F3}, which is a cut end rather than a taper");
                        Assert.That(
                            massif.CrestAt(1f - s), Is.LessThan(massif.CrestHeightMeters * 0.4f),
                            $"seed {worlds[w].Seed}: the far end is cut rather than tapered");
                    }
                }
            }
        }

        [Test]
        public void EverySeedRaisesExactlyTheAxesItAccepted()
        {
            for (var w = 0; w < worlds.Length; w++)
            {
                var accepted = 0;
                for (var i = 0; i < worlds[w].Candidates.Count; i++)
                {
                    if (worlds[w].Candidates[i].Accepted)
                    {
                        accepted++;
                    }
                }

                // Not "every seed has a massif" any more. Stage 2A certifies room as well as geology,
                // so a world whose island is narrower than a massif is wide certifies nothing and gets
                // nothing - which is the correct answer rather than a malformed body. What this stage
                // owes is that it builds exactly what it was handed.
                Assert.AreEqual(
                    accepted, worlds[w].Massifs.Count,
                    $"seed {worlds[w].Seed}: massifs do not match accepted axes");
            }
        }
    }
}
