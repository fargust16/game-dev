using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadSurfacePaintTests
    {
        [Test]
        public void Transition_IsWithinMemoBand()
        {
            Assert.GreaterOrEqual(RoadSurfaceStyle.TransitionMeters, 1.5f);
            Assert.LessOrEqual(RoadSurfaceStyle.TransitionMeters, 3.0f);
        }

        [Test]
        public void TransitionCoverage_IsSofterThanBed()
        {
            Assert.Less(RoadSurfaceStyle.TransitionCoverage, RoadSurfaceStyle.BedCoverage);
            Assert.Greater(RoadSurfaceStyle.TransitionCoverage, 0.35f);
        }

        [Test]
        public void CenterMask_IsFullNearCenterline()
        {
            var m = RoadSurfacePaint.SampleCenterMask(0f, halfRoadMeters: 2f, blendEndMeters: 4.5f);
            Assert.Greater(m, 0.95f);
        }

        [Test]
        public void CenterMask_FallsOffTowardBlend()
        {
            var near = RoadSurfacePaint.SampleCenterMask(0.5f, 2f, 4.5f);
            var mid = RoadSurfacePaint.SampleCenterMask(2.0f, 2f, 4.5f);
            var far = RoadSurfacePaint.SampleCenterMask(4.5f, 2f, 4.5f);
            Assert.Greater(near, mid);
            Assert.Less(far, 0.05f);
        }

        [Test]
        public void EdgeMask_PeaksOnShoulder()
        {
            var half = 2f;
            var packed = 3.2f;
            var blend = 5.5f;
            var center = RoadSurfacePaint.SampleEdgeMask(0.2f, half, packed, blend);
            var shoulder = RoadSurfacePaint.SampleEdgeMask(2.6f, half, packed, blend);
            var outer = RoadSurfacePaint.SampleEdgeMask(5.4f, half, packed, blend);
            Assert.Less(center, shoulder);
            Assert.Greater(shoulder, 0.35f);
            Assert.Less(outer, 0.15f);
        }

        [Test]
        public void WriteRoadPixel_RenormalizesToOne()
        {
            var maps = new float[1, 1, 4];
            maps[0, 0, 0] = 0.5f; // grass
            maps[0, 0, 1] = 0.5f; // rock
            maps[0, 0, 2] = 0f;   // center
            maps[0, 0, 3] = 0f;   // edge

            RoadSurfacePaint.WriteRoadPixel(
                maps, 0, 0, 4,
                centerIndex: 2, centerW: 0.7f,
                edgeIndex: 3, edgeW: 0.2f,
                gravelIndex: -1, gravelW: 0f);

            var sum = maps[0, 0, 0] + maps[0, 0, 1] + maps[0, 0, 2] + maps[0, 0, 3];
            Assert.AreEqual(1f, sum, 1e-4f);
            Assert.AreEqual(0.7f, maps[0, 0, 2], 1e-4f);
            Assert.AreEqual(0.2f, maps[0, 0, 3], 1e-4f);
        }
    }
}
