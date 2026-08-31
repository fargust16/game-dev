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
        public void SoftenNatural_CannotCrossImpassableBand()
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
        public void LimitHeadingChange_KeepsCornerWhenTheInsertedPointIsNotClear()
        {
            // The corner isolated from the band case above. Splitting it used to be decided on the
            // chord a -> mid -> c, but the point that actually gets inserted sits 45 % of the way
            // back towards the corner and dips into the band, so the road shipped a segment that
            // nothing had ever checked.
            var a = new Vector2(32.5f, 25f);
            var b = new Vector2(37.5f, 17.5f);
            var c = new Vector2(42.5f, 13.188f);
            var path = new List<Vector2> { a, b, c };

            float Cost(float x, float z) =>
                x > 35f && x < 65f && z > 20f ? float.PositiveInfinity : 1f;

            RoadPathSimplifier.ActiveMinTurnRadiusMeters = 18f;
            RoadPathSimplifier.ActiveMaxHeadingChangeDegrees = 28f;

            // The chord the old code measured is clear both ways — that is what fooled it.
            var mid = (a + c) * 0.5f;
            Assert.IsTrue(RoadPathSimplifier.SegmentClear(a, mid, Cost, 4f), "chord a -> mid is clear");
            Assert.IsTrue(RoadPathSimplifier.SegmentClear(mid, c, Cost, 4f), "chord mid -> c is clear");

            // The point it emitted instead is not reachable from a.
            var inserted = Vector2.Lerp(b, mid, 0.55f);
            Assert.IsFalse(
                RoadPathSimplifier.SegmentClear(a, inserted, Cost, 4f),
                "the segment into the inserted point is blocked — this is the case under test");

            var limited = RoadPathSimplifier.LimitHeadingChange(path, Cost, 4f);

            Assert.AreEqual(b, limited[1], "an unvalidated point must not replace the corner");
            for (var i = 0; i < limited.Count - 1; i++)
            {
                Assert.IsTrue(
                    RoadPathSimplifier.SegmentClear(limited[i], limited[i + 1], Cost, 4f),
                    "every emitted segment must be one that was validated");
            }
        }

        [Test]
        public void LimitHeadingChange_StillSoftensWhenTheInsertedPointIsClear()
        {
            // Same corner and thresholds with the obstacle removed: the stage must still soften.
            var a = new Vector2(32.5f, 25f);
            var b = new Vector2(37.5f, 17.5f);
            var c = new Vector2(42.5f, 13.188f);
            var path = new List<Vector2> { a, b, c };

            float Cost(float x, float z) => 1f;

            RoadPathSimplifier.ActiveMinTurnRadiusMeters = 18f;
            RoadPathSimplifier.ActiveMaxHeadingChangeDegrees = 28f;

            var limited = RoadPathSimplifier.LimitHeadingChange(path, Cost, 4f);

            var expected = Vector2.Lerp(b, (a + c) * 0.5f, 0.55f);
            Assert.AreEqual(3, limited.Count);
            Assert.AreEqual(a, limited[0]);
            Assert.AreEqual(c, limited[limited.Count - 1]);
            Assert.AreEqual(expected.x, limited[1].x, 0.001f, "the softened point still ships");
            Assert.AreEqual(expected.y, limited[1].y, 0.001f, "the softened point still ships");
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
