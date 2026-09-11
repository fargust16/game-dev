using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 2B against a wide spread of seeds, on geometry alone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The five canonical seeds prove the field is right; they cannot prove the model is. Two dozen
    /// more are run here to answer the other question - whether the massif's construction holds up on
    /// axes it has never seen: short ones, long ones, straight ones, bent ones, one culmination and
    /// two. The one real fragility this stage had was found exactly this way: the longitudinal profile
    /// placed its control points at absolute positions, and a culmination pushed towards an end
    /// collapsed a span into a cliff. No canonical seed produced that; a different one did.
    /// </para>
    /// <para>
    /// No height field is evaluated. Stage 2A costs about a fifth of a second a seed and the massif is
    /// built from its axis in under a millisecond, so two dozen seeds cost a few seconds rather than
    /// the three minutes a field sweep would. Everything asserted here is a property of the model, and
    /// the model is what a field would be sampled from.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage2BRobustnessTests
    {
        private static readonly int[] Seeds =
        {
            1, 2, 3, 17, 42, 99, 256, 777, 1024, 4096, 9091, 12345,
            31337, 65535, 101010, 202020, 314159, 500001, 654321, 888888,
            1000003, 1234567, 7654321, 20261231,
        };

        private static TerrainGenSettings TerrainSettings => TerrainGenSettings.Default;
        private static MacroStructureSettings MacroSettings => MacroStructureSettings.Default;
        private static MassifSettings Settings => MassifSettings.Default;

        private readonly List<MountainMassif> massifs = new List<MountainMassif>();
        private readonly List<int> seedOf = new List<int>();

        [OneTimeSetUp]
        public void BuildMassifs()
        {
            for (var i = 0; i < Seeds.Length; i++)
            {
                var stage2a = Stage2AMacroStructure.Run(Seeds[i], TerrainSettings, MacroSettings);
                var worldSeed = new Seed.WorldSeed(Seeds[i]);

                var index = 0;
                foreach (var axis in stage2a.AcceptedAxes())
                {
                    // On the certified core, exactly as the generator does it, so this spread
                    // exercises the contract rather than a construction only the tests use.
                    massifs.Add(MountainMassif.Build(
                        worldSeed, index++, axis, TerrainSettings, Settings, axis.CertifiedCore));
                    seedOf.Add(Seeds[i]);
                }
            }
        }

        [Test]
        public void TheSpreadCoversTheCasesItIsMeantTo()
        {
            Assert.That(massifs.Count, Is.GreaterThan(15), "too few massifs to call this a spread");

            var shortest = float.PositiveInfinity;
            var longest = 0f;
            var narrowest = float.PositiveInfinity;
            var widest = 0f;
            var straightest = float.PositiveInfinity;
            var bendiest = 0f;
            var single = 0;
            var twin = 0;

            for (var i = 0; i < massifs.Count; i++)
            {
                shortest = Mathf.Min(shortest, massifs[i].LengthMeters);
                longest = Mathf.Max(longest, massifs[i].LengthMeters);
                narrowest = Mathf.Min(narrowest, massifs[i].HalfWidthMeters);
                widest = Mathf.Max(widest, massifs[i].HalfWidthMeters);

                var curvature = massifs[i].MaxCurvature();
                straightest = Mathf.Min(straightest, curvature);
                bendiest = Mathf.Max(bendiest, curvature);

                if (massifs[i].CulminationT.Length == 1)
                {
                    single++;
                }
                else
                {
                    twin++;
                }
            }

            Assert.That(single, Is.GreaterThan(0), "no single-culmination massif in the spread");
            Assert.That(twin, Is.GreaterThan(0), "no two-culmination massif in the spread");
            Assert.That(
                longest / Mathf.Max(1f, shortest), Is.GreaterThan(1.5f),
                $"lengths only span {shortest:F0}-{longest:F0} m");
            Assert.That(
                bendiest, Is.GreaterThan(0.001f),
                $"no meaningfully curved axis in the spread (worst curvature {bendiest:F5} 1/m)");

            TestContext.WriteLine(
                $"{massifs.Count} massifs: length {shortest:F0}-{longest:F0} m, "
                + $"half-width {narrowest:F0}-{widest:F0} m, "
                + $"curvature {straightest:F5}-{bendiest:F5} 1/m, "
                + $"{single} single / {twin} twin culmination");
        }

        /// <summary>
        /// No span of the longitudinal profile is ever degenerate.
        /// </summary>
        /// <remarks>
        /// The regression guard for the one real fragility this stage had. Control points placed at
        /// absolute positions collided when a culmination moved towards an end, leaving a smoothstep
        /// to cross most of the crest in a hundredth of the length - an eight metre cliff in a three
        /// hundred metre massif. The points are proportional now, so this cannot recur; the test is
        /// what says so.
        /// </remarks>
        [Test]
        public void TheLongitudinalProfileNeverStepsHard()
        {
            const int samples = 600;

            for (var i = 0; i < massifs.Count; i++)
            {
                var massif = massifs[i];
                var crest = Mathf.Max(1f, massif.CrestHeightMeters);
                var previous = massif.CrestAt(0f);
                var worst = 0f;
                var worstAt = 0f;

                for (var k = 1; k <= samples; k++)
                {
                    var s = k / (float)samples;
                    var here = massif.CrestAt(s);
                    var step = Mathf.Abs(here - previous);
                    if (step > worst)
                    {
                        worst = step;
                        worstAt = s;
                    }

                    previous = here;
                }

                // A smooth profile crosses its crest over the whole length, so a single step of a six
                // hundredth of it can never be more than a few per cent of the crest.
                Assert.That(
                    worst / crest, Is.LessThan(0.04f),
                    $"seed {seedOf[i]} massif {massif.Index}: the profile steps {worst:F1} m of a "
                    + $"{crest:F0} m crest at s={worstAt:F3}");
            }
        }

        [Test]
        public void TheCrossSectionFallsSmoothlyFromCrownToToe()
        {
            const int samples = 600;

            for (var i = 0; i < massifs.Count; i++)
            {
                var massif = massifs[i];

                Assert.AreEqual(1f, massif.CrossSection(0f), 1e-4f, $"seed {seedOf[i]}: crown");
                Assert.AreEqual(0f, massif.CrossSection(1f), 1e-4f, $"seed {seedOf[i]}: toe");

                var previous = massif.CrossSection(0f);
                for (var k = 1; k <= samples; k++)
                {
                    var here = massif.CrossSection(k / (float)samples);

                    Assert.That(
                        here, Is.LessThanOrEqualTo(previous + 1e-5f),
                        $"seed {seedOf[i]}: the cross-section rises again at u={k / (float)samples:F3}");
                    Assert.That(
                        previous - here, Is.LessThan(0.02f),
                        $"seed {seedOf[i]}: the cross-section steps at u={k / (float)samples:F3}");

                    previous = here;
                }
            }
        }

        [Test]
        public void EveryMassifTapersToNothingAtBothEnds()
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                Assert.AreEqual(0f, massifs[i].CrestAt(0f), 1e-3f, $"seed {seedOf[i]}: start");
                Assert.AreEqual(0f, massifs[i].CrestAt(1f), 1e-3f, $"seed {seedOf[i]}: end");
                // The middle of the core, not the middle of the axis. Certification places the core
                // wherever the island has room, so a massif whose high ground sits in one half of its
                // length is correct rather than defective.
                var core = massifs[i].CoreInterval;
                var middle = (core.x + core.y) * 0.5f;
                Assert.That(
                    massifs[i].CrestAt(middle), Is.GreaterThan(massifs[i].CrestHeightMeters * 0.3f),
                    $"seed {seedOf[i]}: the middle of the certified core is not high ground");
            }
        }

        [Test]
        public void EveryMassifKeepsAWidthAllAlongItsLength()
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                var massif = massifs[i];
                var floor = massif.HalfWidthMeters * Settings.WidthTaperFloor * 0.99f;

                for (var k = 0; k <= 200; k++)
                {
                    var width = massif.HalfWidthAt(k / 200f);
                    Assert.That(
                        width, Is.GreaterThanOrEqualTo(floor),
                        $"seed {seedOf[i]}: the body closes to {width:F0} m at s={k / 200f:F2}");
                    Assert.That(
                        width, Is.LessThanOrEqualTo(massif.HalfWidthMeters + 0.5f),
                        $"seed {seedOf[i]}: the body swells past its nominal width at s={k / 200f:F2}");
                }
            }
        }

        [Test]
        public void CulminationsStayFewUnequalAndSeparated()
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                var massif = massifs[i];
                var t = massif.CulminationT;

                Assert.That(
                    t.Length, Is.InRange(1, Settings.MaxCulminations),
                    $"seed {seedOf[i]}: {t.Length} culminations");

                for (var k = 0; k < t.Length; k++)
                {
                    Assert.That(
                        t[k], Is.InRange(Settings.MinRampFraction - 1e-3f, 1f - Settings.MinRampFraction + 1e-3f),
                        $"seed {seedOf[i]}: culmination {k} sits at s={t[k]:F3}");
                }

                if (t.Length < 2)
                {
                    continue;
                }

                // Measured against the certified core, which is where culminations are placed, not
                // against the whole axis. An absolute threshold was right when the profile was laid
                // out over the whole length; now a world whose core is half its axis would fail it
                // while its culminations sit as far apart as the construction allows.
                var core = massif.CoreInterval;
                var span = Mathf.Max(1e-3f, core.y - core.x);
                // 0.28 is the construction's own guarantee, not a guess: the culminations are placed
                // at fractions clamped to [0.20, 0.36] and [0.64, 0.80] of the core, so the closest
                // they can ever come is 0.64 - 0.36.
                Assert.That(
                    (t[1] - t[0]) / span, Is.GreaterThanOrEqualTo(0.28f - 1e-3f),
                    $"seed {seedOf[i]}: culminations are {t[1] - t[0]:F2} apart over a core of "
                    + $"{span:F2}");

                // Metres, so the running minimum starts at infinity: seeding it with 1 would floor
                // every measurement at one metre and report a sound saddle as a collapse.
                var saddle = float.PositiveInfinity;
                for (var k = 0; k <= 60; k++)
                {
                    saddle = Mathf.Min(saddle, massif.CrestAt(Mathf.Lerp(t[0], t[1], k / 60f)));
                }

                var peak = Mathf.Max(massif.CrestAt(t[0]), massif.CrestAt(t[1]));
                Assert.That(
                    saddle / Mathf.Max(1f, peak), Is.GreaterThan(0.3f),
                    $"seed {seedOf[i]}: the saddle falls to {saddle / peak:P0} of the peak, "
                    + "which reads as two hills rather than one massif");
            }
        }

        [Test]
        public void TheWidthTheModelCarriesNeverDependsOnTheCurvature()
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                Assert.IsFalse(
                    massifs[i].ReachBound,
                    $"seed {seedOf[i]}: the curvature clamp narrowed a massif from "
                    + $"{massifs[i].HalfWidthMeters:F0} m to "
                    + $"{massifs[i].ReachLimitedHalfWidthMeters:F0} m");
            }
        }

        [Test]
        public void TheModelIsDeterministic()
        {
            for (var i = 0; i < Seeds.Length; i += 6)
            {
                var stage2a = Stage2AMacroStructure.Run(Seeds[i], TerrainSettings, MacroSettings);
                var worldSeed = new Seed.WorldSeed(Seeds[i]);

                var index = 0;
                foreach (var axis in stage2a.AcceptedAxes())
                {
                    var again = MountainMassif.Build(
                        worldSeed, index, axis, TerrainSettings, Settings, axis.CertifiedCore);

                    var original = Find(Seeds[i], index);
                    Assert.IsNotNull(original, $"seed {Seeds[i]} massif {index} vanished on a repeat");

                    Assert.AreEqual(original.CrestHeightMeters, again.CrestHeightMeters, 1e-4f, "crest");
                    Assert.AreEqual(original.HalfWidthMeters, again.HalfWidthMeters, 1e-4f, "half-width");
                    Assert.AreEqual(original.Asymmetry, again.Asymmetry, 1e-4f, "asymmetry");
                    Assert.AreEqual(original.SteepSide, again.SteepSide, "steep side");

                    for (var k = 0; k <= 100; k++)
                    {
                        Assert.AreEqual(
                            original.CrestAt(k / 100f), again.CrestAt(k / 100f), 1e-4f,
                            $"seed {Seeds[i]}: profile at s={k / 100f:F2}");
                    }

                    index++;
                }
            }
        }

        private MountainMassif Find(int seed, int index)
        {
            for (var i = 0; i < massifs.Count; i++)
            {
                if (seedOf[i] == seed && massifs[i].Index == index)
                {
                    return massifs[i];
                }
            }

            return null;
        }
    }
}
