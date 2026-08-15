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
            Assert.GreaterOrEqual(RoadSurfaceStyle.TransitionMeters, RoadSurfaceStyle.TransitionMinMeters);
            Assert.LessOrEqual(RoadSurfaceStyle.TransitionMeters, RoadSurfaceStyle.TransitionMaxMeters);
        }

        [Test]
        public void TransitionCoverage_IsSofterThanBed()
        {
            Assert.Less(RoadSurfaceStyle.TransitionCoverage, RoadSurfaceStyle.BedCoverage);
            Assert.Greater(RoadSurfaceStyle.TransitionCoverage, 0.35f);
            Assert.LessOrEqual(RoadSurfaceStyle.TransitionCoverage, 0.45f);
        }

        [Test]
        public void SoftRim_ExpandsOnCoarseAlphamap()
        {
            const float mPerPx = 1400f / 512f;
            var rim = RoadSurfaceStyle.SoftRimMeters(mPerPx);
            Assert.GreaterOrEqual(rim, mPerPx * RoadSurfaceStyle.MinSoftRimAlphamapTexels - 1e-4f);
            Assert.Greater(rim, RoadSurfaceStyle.TransitionMeters);
        }

        [Test]
        public void BedCoverage_IsReadableNotBinary()
        {
            Assert.GreaterOrEqual(RoadSurfaceStyle.BedCoverage, 0.85f);
            Assert.LessOrEqual(RoadSurfaceStyle.BedCoverage, 0.95f);
        }

        [Test]
        public void CenterMask_IsFullNearCenterline()
        {
            var m = RoadSurfacePaint.SampleCenterMask(0f, halfRoadMeters: 2f, blendEndMeters: 3.2f);
            Assert.Greater(m, 0.95f);
        }

        [Test]
        public void CenterMask_FallsOffTowardBlend()
        {
            var near = RoadSurfacePaint.SampleCenterMask(0.5f, 2f, 3.2f);
            var mid = RoadSurfacePaint.SampleCenterMask(2.0f, 2f, 3.2f);
            var far = RoadSurfacePaint.SampleCenterMask(3.2f, 2f, 3.2f);
            Assert.Greater(near, mid);
            Assert.Less(far, 0.05f);
        }

        [Test]
        public void EdgeMask_PeaksOnShoulder()
        {
            var half = 2f;
            var packed = 2f;
            var blend = 3.2f;
            var center = RoadSurfacePaint.SampleEdgeMask(0.2f, half, packed, blend);
            var shoulder = RoadSurfacePaint.SampleEdgeMask(1.9f, half, packed, blend);
            var outer = RoadSurfacePaint.SampleEdgeMask(3.15f, half, packed, blend);
            Assert.Less(center, shoulder);
            Assert.Greater(shoulder, 0.35f);
            Assert.Less(outer, 0.15f);
        }

        [Test]
        public void WriteRoadPixel_RenormalizesToOne()
        {
            var maps = new float[1, 1, 4];
            maps[0, 0, 0] = 0.5f;
            maps[0, 0, 1] = 0.5f;
            maps[0, 0, 2] = 0f;
            maps[0, 0, 3] = 0f;
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
