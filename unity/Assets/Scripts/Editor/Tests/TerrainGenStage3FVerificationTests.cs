using System.Collections.Generic;
using GameDev.EditorTools;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.TerrainGen.Tests
{
    /// <summary>
    /// Stage 3F F3: the verification layer itself, checked before its verdicts are trusted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A verifier that cannot fail its subject is worth nothing, so the load-bearing test here is
    /// <see cref="V1_ASyntheticClosedHollowIsReported"/>: a surface with a hollow deliberately put in it
    /// has to come back <see cref="DrainageState.Closed"/>, and the same surface without the hollow has
    /// to come back drained. The rest pin determinism, the toe range S11-T is measured over, and the
    /// fact that the carve is derived from the two surfaces rather than read from the model.
    /// </para>
    /// <para>
    /// Verification only. Nothing here asserts a terrain quality target: what the canonical seeds
    /// actually score is measured by <see cref="TerrainGenStage3FVerifyDump"/> and read by a human.
    /// </para>
    /// </remarks>
    public sealed class TerrainGenStage3FVerificationTests
    {
        private const int Seed = 110201;

        private static IslandReliefWorld world;
        private static CarveField carve;
        private static RidgeSettings settings;

        [OneTimeSetUp]
        public void BuildWorld()
        {
            settings = RidgeSettings.Default;
            settings.Clamp();
            world ??= IslandReliefWorld.Build(Seed, IslandReliefSettings.Default, withStage3: true);
            carve ??= world.Trajectories.BuildCarve(world.Stage1.Coast, settings, world.Stage2B.MassifSettings);
        }

        private FinalValleyTrajectory FirstWithGeometry()
        {
            foreach (var t in world.Trajectories.Trajectories)
            {
                if (t.HasGeometry && t.LengthMeters > 200f)
                {
                    return t;
                }
            }

            Assert.Fail($"seed {Seed} has no trajectory long enough to profile");
            return null;
        }

        private TrajectoryProfile Profile(Stage3FVerification.Surface finished) =>
            Stage3FVerification.Profile(
                FirstWithGeometry(), world.Stage1, world.Uplift, world.Relief, finished,
                world.Basins.Basins, settings);

        // ---------- V1 ----------

        [Test]
        public void V1_ASyntheticClosedHollowIsReported()
        {
            var trajectory = FirstWithGeometry();

            // Along-ness is arc length on the trajectory's own curve, not a projection onto the straight
            // line from head to end: the curve is not straight, and mixing the two collapses the surface.
            float Arc(float x, float z)
            {
                trajectory.Project(new Vector2(x, z), out var arc, out _);
                return arc;
            }

            // A ramp gentle enough that a hollow in it genuinely impounds: it falls 0.6 m across the
            // hollow's exit, so a 5 m hollow leaves ~4.4 m of ground to climb out over.
            const float slope = 0.01f;
            const float depth = 5f;
            const float radius = 60f;
            float Ramp(float x, float z) => 200f - Arc(x, z) * slope;

            var clean = Stage3FVerification.Profile(
                trajectory, world.Stage1, world.Uplift, world.Relief, Ramp, null, settings);
            Assert.AreEqual(
                0, Count(clean, DrainageState.Closed), "a monotone ramp was reported as holding a closed hollow");

            var centre = trajectory.LengthMeters * 0.5f;

            float Hollowed(float x, float z)
            {
                var d = Mathf.Abs(Arc(x, z) - centre);
                return Ramp(x, z) - (d < radius ? depth * (1f - d / radius) : 0f);
            }

            var dug = Stage3FVerification.Profile(
                trajectory, world.Stage1, world.Uplift, world.Relief, Hollowed, null, settings);

            Assert.Greater(
                Count(dug, DrainageState.Closed), 0,
                "a hollow cut into a draining ramp was not reported as closed; the verifier cannot fail its subject");

            var worst = 0f;
            foreach (var f in dug.Findings)
            {
                if (f.State == DrainageState.Closed)
                {
                    worst = Mathf.Max(worst, f.DepthMeters);
                }
            }

            Assert.Greater(worst, 1f, $"the hollow was found but measured only {worst} m deep against {depth} m dug");
        }

        // ---------- V2 ----------

        [Test]
        public void V2_LowerGroundBeyondABarrierIsNotDrainage()
        {
            // The defect this pins: a hollow with lower ground further downstream is still a hollow if
            // a barrier stands between. Reading "there is lower ground later" as drainage is what made
            // an earlier pass report 1 closed low over forty seeds instead of 96.
            var trajectory = FirstWithGeometry();
            var length = trajectory.LengthMeters;

            float Staircase(float x, float z)
            {
                trajectory.Project(new Vector2(x, z), out var along, out _);

                // Ground, then a hollow, then a hump standing above the hollow, then ground below it.
                // Each step tilts slightly so the profile has real extrema rather than plateaus.
                if (along < length * 0.3f)
                {
                    return 100f - along * 0.001f;
                }

                if (along < length * 0.5f)
                {
                    return 90f - along * 0.001f;
                }

                if (along < length * 0.7f)
                {
                    return 98f - along * 0.001f;
                }

                return 80f - along * 0.001f;
            }

            var profile = Stage3FVerification.Profile(
                trajectory, world.Stage1, world.Uplift, world.Relief, Staircase, null, settings);

            var found = false;
            foreach (var f in profile.Findings)
            {
                if (f.State != DrainageState.Closed)
                {
                    continue;
                }

                found = true;
                Assert.IsTrue(
                    f.ReachesLowerGround,
                    "this hollow does reach lower ground; the point is that a barrier stands in the way");
                Assert.Greater(f.DepthMeters, 5f, "the barrier stands 8 m above the hollow");
            }

            Assert.IsTrue(found, "a hollow behind a barrier was not reported closed");
        }

        // ---------- V3 ----------

        [Test]
        public void V3_TheVerificationIsDeterministic()
        {
            Stage3FVerification.Surface finished = (x, z) => CarvedElevation.At(
                world.Stage1, world.Uplift, world.Relief, carve, settings, x, z);

            var a = Profile(finished);
            var b = Profile(finished);

            Assert.AreEqual(a.Arc.Length, b.Arc.Length);
            for (var i = 0; i < a.Arc.Length; i++)
            {
                Assert.AreEqual(a.H3F[i], b.H3F[i], 0f, $"sample {i} moved between runs");
                Assert.AreEqual(a.AcrossMin[i], b.AcrossMin[i], 0f, $"cross-section {i} moved between runs");
            }

            Assert.AreEqual(a.AdverseRiseFinished, b.AdverseRiseFinished, 0f);
            Assert.AreEqual(a.S11TClimb, b.S11TClimb, 0f);
            Assert.AreEqual(a.Findings.Count, b.Findings.Count);
        }

        // ---------- V4 ----------

        [Test]
        public void V4_TheCarveIsDerivedFromTheTwoSurfaces()
        {
            Stage3FVerification.Surface finished = (x, z) => CarvedElevation.At(
                world.Stage1, world.Uplift, world.Relief, carve, settings, x, z);
            var p = Profile(finished);

            for (var i = 0; i < p.Arc.Length; i++)
            {
                Assert.AreEqual(
                    p.H4A[i] - p.H3F[i], p.Carve[i], 1e-4f,
                    $"sample {i}: the reported carve is not the difference between the two surfaces");
                Assert.AreEqual(
                    p.H4A[i] - p.H2[i], p.Relief[i], 1e-4f,
                    $"sample {i}: the reported relief is not H4A - H2");
            }

            Assert.AreEqual(0, p.CompositionViolations, "the finished ground stood above the island it was cut from");
        }

        // ---------- V5 ----------

        [Test]
        public void V5_S11TIsMeasuredFromTheToeNotTheHead()
        {
            // Stage 4A's S11-T loop starts at ToeIndex. Measured over the whole arc the same expression
            // is dominated by the massif crest and reads several times higher, which is a different
            // quantity and must not be reported as S11-T.
            Stage3FVerification.Surface finished = (x, z) => CarvedElevation.At(
                world.Stage1, world.Uplift, world.Relief, carve, settings, x, z);

            var report = new List<string>();
            var anyToeInsideBudget = false;
            foreach (var t in world.Trajectories.Trajectories)
            {
                if (!t.HasGeometry)
                {
                    continue;
                }

                var p = Stage3FVerification.Profile(
                    t, world.Stage1, world.Uplift, world.Relief, finished, world.Basins.Basins, settings);
                report.Add($"traj {p.TrajectoryId}: S11T(toe) {p.S11TClimb:F2} m, full arc {p.ReliefRiseFullArc:F2} m");

                Assert.LessOrEqual(
                    p.S11TClimb, p.ReliefRiseFullArc + 1e-3f,
                    $"traj {p.TrajectoryId}: the toe range cannot exceed the whole arc it sits inside");
                if (p.S11TClimb <= settings.CorridorIncisionMeters)
                {
                    anyToeInsideBudget = true;
                }
            }

            TestContext.WriteLine(string.Join("\n", report));
            Assert.IsTrue(anyToeInsideBudget, "no trajectory on the canonical seed measured a toe-range climb inside budget");
        }

        // ---------- V6 ----------

        [Test]
        public void V6_AMassifFreeWorldHasNothingToVerify()
        {
            var bare = IslandReliefWorld.Build(1337, IslandReliefSettings.Default, withStage3: true);
            Assert.AreEqual(
                0, bare.Trajectories.Trajectories.Count,
                "seed 1337 certified no massif, so it should carry no corridors to verify");
        }

        private static int Count(TrajectoryProfile p, DrainageState state)
        {
            var n = 0;
            foreach (var f in p.Findings)
            {
                if (f.State == state)
                {
                    n++;
                }
            }

            return n;
        }
    }
}
