using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadSampleDistanceTests
    {
        [Test]
        public void PointToSegment_MidpointOffset_IsPerpendicularDistance()
        {
            var a = new Vector2(0f, 0f);
            var b = new Vector2(10f, 0f);
            var d = RoadSampleDistance.PointToSegmentMeters(new Vector2(5f, 3f), a, b);
            Assert.AreEqual(3f, d, 1e-4f);
        }

        [Test]
        public void NearestMeters_UsesSegmentsNotDiskScallops()
        {
            // Sparse samples along a straight road — disk nearest would be farther off-axis mid-span.
            var samples = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(6f, 0f),
                new Vector2(12f, 0f),
            };
            var midOffAxis = new Vector2(3f, 2f);
            var seg = RoadSampleDistance.NearestMeters(midOffAxis, samples);
            Assert.AreEqual(2f, seg, 1e-3f);

            // Nearest sample point would be (0,0) or (6,0) → ~3.6 m, not 2 m.
            var toPoint = Mathf.Min(
                Vector2.Distance(midOffAxis, samples[0]),
                Vector2.Distance(midOffAxis, samples[1]));
            Assert.Greater(toPoint, seg + 0.5f);
        }

        [Test]
        public void NearestMeters_BreaksAcrossLargeGaps()
        {
            var samples = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(2f, 0f),
                new Vector2(100f, 0f), // gap > MaxSegmentGapMeters
                new Vector2(102f, 0f),
            };
            var betweenRuns = new Vector2(50f, 0f);
            var d = RoadSampleDistance.NearestMeters(betweenRuns, samples);
            // Must not treat 2→100 as a valid segment.
            Assert.Greater(d, 40f);
        }
    }
}
