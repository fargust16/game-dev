using System.Collections.Generic;
using GameDev.World.Hydrology;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Hydrology.Tests
{
    /// <summary>
    /// A waterfall must be readable straight off the river's elevation profile — that is what keeps
    /// it from ever becoming a decorative object scattered near water.
    /// </summary>
    public sealed class WaterfallDetectorTests
    {
        private static HydrologySettings Settings(float minDrop = 6f, float maxRun = 30f)
        {
            var settings = HydrologySettings.Default;
            settings.WaterfallMinDropMeters = minDrop;
            settings.WaterfallMaxRunMeters = maxRun;
            settings.NodeSpacingMeters = 10f;
            settings.Clamp();
            return settings;
        }

        private static RiverReach ReachWithProfile(params float[] bedElevations)
        {
            var nodes = new List<RiverNode>(bedElevations.Length);
            for (var i = 0; i < bedElevations.Length; i++)
            {
                nodes.Add(new RiverNode(
                    new Vector2(i * 10f, 0f),
                    bedElevations[i],
                    bedElevations[i] + 0.5f,
                    i * 10f,
                    120_000f));
            }

            return new RiverReach(
                new ReachId(0),
                ReachId.None,
                LakeId.None,
                nodes,
                2,
                120_000f,
                12f,
                ReachTermination.Ocean,
                true);
        }

        [Test]
        public void GentleGradient_ProducesNoCandidates()
        {
            var reaches = new[] { ReachWithProfile(60f, 59f, 58f, 57f, 56f, 55f) };

            Assert.AreEqual(0, WaterfallDetector.Detect(reaches, Settings()).Count);
        }

        [Test]
        public void SingleStep_ProducesOneCandidate()
        {
            var reaches = new[] { ReachWithProfile(60f, 59f, 44f, 43f, 42f) };
            var candidates = WaterfallDetector.Detect(reaches, Settings());

            Assert.AreEqual(1, candidates.Count);

            // The steepest window inside the run limit, which is the 16 m cliff from node 0 to
            // node 2 — not the 17 m that includes the gentle tail below it.
            Assert.AreEqual(16f, candidates[0].DropMeters, 0.01f);
            Assert.AreEqual(new ReachId(0), candidates[0].Reach);
        }

        [Test]
        public void LongStaircase_IsReportedOnceNotOncePerPair()
        {
            // Windows overlap by design, so without the skip past the fall a single drop would be
            // reported from every node inside it.
            var reaches = new[] { ReachWithProfile(60f, 52f, 44f, 36f, 28f, 27f, 26f) };
            var candidates = WaterfallDetector.Detect(reaches, Settings(minDrop: 6f, maxRun: 30f));

            Assert.LessOrEqual(candidates.Count, 3);
            for (var i = 1; i < candidates.Count; i++)
            {
                Assert.Greater(
                    candidates[i].NodeIndex, candidates[i - 1].NodeIndex,
                    "candidates overlap the same fall");
            }
        }

        [Test]
        public void DropSpreadOverTooLongARun_IsNotAWaterfall()
        {
            var reaches = new[] { ReachWithProfile(60f, 57f, 54f, 51f, 48f, 45f, 42f) };

            // 18 m of fall, but never more than 9 m inside any 30 m window.
            Assert.AreEqual(0, WaterfallDetector.Detect(reaches, Settings(minDrop: 10f)).Count);
        }

        [Test]
        public void Candidate_CarriesUsableSlopeAndPosition()
        {
            var reaches = new[] { ReachWithProfile(60f, 40f, 39f) };
            var candidate = WaterfallDetector.Detect(reaches, Settings())[0];

            Assert.AreEqual(20f, candidate.DropMeters, 0.01f);
            Assert.AreEqual(10f, candidate.RunMeters, 0.01f);
            Assert.AreEqual(2f, candidate.Slope, 0.01f);
            Assert.AreEqual(new Vector2(0f, 0f), candidate.PositionXZ);
        }

        [Test]
        public void EmptyOrTinyReaches_AreHandled()
        {
            Assert.AreEqual(0, WaterfallDetector.Detect(null, Settings()).Count);
            Assert.AreEqual(0, WaterfallDetector.Detect(new RiverReach[0], Settings()).Count);
            Assert.AreEqual(0, WaterfallDetector.Detect(new[] { ReachWithProfile(50f) }, Settings()).Count);
        }
    }
}
