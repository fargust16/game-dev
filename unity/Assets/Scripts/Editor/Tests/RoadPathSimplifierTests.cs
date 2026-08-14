using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadPathSimplifierTests
    {
        [Test]
        public void SoftenNatural_OpenField_KeepsEndpointsAndCurves()
        {
            var path = new List<Vector2>();
            for (var i = 0; i <= 10; i++)
            {
                path.Add(new Vector2(i * 10f, (i % 2) * 8f));
            }

            float Cost(float x, float z) => 1f;
            var soft = RoadPathSimplifier.SoftenNatural(path, Cost, sampleStepMeters: 5f);

            Assert.GreaterOrEqual(soft.Count, 2);
            Assert.AreEqual(path[0], soft[0]);
            Assert.AreEqual(path[path.Count - 1], soft[soft.Count - 1]);
            // Must not collapse to a single artificial chord.
            Assert.Greater(soft.Count, 2, "natural soften keeps intermediate curve samples");
        }

        [Test]
        public void SoftenNatural_CannotCrossBlockedLakeBand()
        {
            var path = new List<Vector2>
            {
                new Vector2(0f, 50f),
                new Vector2(20f, 50f),
                new Vector2(40f, 10f),
                new Vector2(60f, 10f),
                new Vector2(80f, 50f),
                new Vector2(100f, 50f),
            };

            float Cost(float x, float z) =>
                x > 35f && x < 65f && z > 20f ? float.PositiveInfinity : 1f;

            var soft = RoadPathSimplifier.SoftenNatural(path, Cost, sampleStepMeters: 4f);

            Assert.GreaterOrEqual(soft.Count, 3);
            for (var i = 0; i < soft.Count - 1; i++)
            {
                Assert.IsTrue(
                    RoadPathSimplifier.SegmentClear(soft[i], soft[i + 1], Cost, 4f),
                    "every softened segment must stay dry");
            }

            Assert.IsFalse(RoadPathSimplifier.SegmentClear(path[0], path[path.Count - 1], Cost, 4f));
        }

        [Test]
        public void LimitHeadingChange_SoftensSharpCorner()
        {
            var path = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(50f, 0f),
                new Vector2(50f, 50f),
            };

            RoadPathSimplifier.ActiveMinTurnRadiusMeters = 20f;
            RoadPathSimplifier.ActiveMaxHeadingChangeDegrees = 20f;
            float Cost(float x, float z) => 1f;
            var limited = RoadPathSimplifier.LimitHeadingChange(path, Cost, 5f);

            Assert.Greater(limited.Count, path.Count - 1);
            Assert.AreEqual(path[0], limited[0]);
            Assert.AreEqual(path[path.Count - 1], limited[limited.Count - 1]);
        }

        [Test]
        public void ClosestPointOnPolyline_ProjectsOntoSegment()
        {
            var line = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(100f, 0f),
            };
            var p = RoadPathSimplifier.ClosestPointOnPolyline(line, new Vector2(40f, 25f));
            Assert.AreEqual(40f, p.x, 0.01f);
            Assert.AreEqual(0f, p.y, 0.01f);
        }
    }
}
