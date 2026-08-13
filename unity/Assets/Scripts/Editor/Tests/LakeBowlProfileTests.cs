using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeBowlProfileTests
    {
        [Test]
        public void InsetContour_ShoreZero_FlatFloorBeyondWall()
        {
            const float radius = 100f;
            const float maxDepth = 40f;
            var s = LakeBathymetry.Settings.NaturalDefault;
            s.InnerContourScale = 0.67f;
            s.NoiseAmplitude = 0f;

            var wall = LakeBathymetry.WallWidthMeters(radius, s);
            Assert.AreEqual(33f, wall, 0.05f);

            Assert.AreEqual(0f, LakeBathymetry.BaseDepthFromShoreDistance(0f, radius, maxDepth, s), 0.05f);
            Assert.Greater(
                LakeBathymetry.BaseDepthFromShoreDistance(wall * 0.5f, radius, maxDepth, s),
                maxDepth * 0.4f);
            Assert.AreEqual(
                maxDepth,
                LakeBathymetry.BaseDepthFromShoreDistance(wall, radius, maxDepth, s),
                0.05f,
                "at inner contour depth is fully Dmax");
            Assert.AreEqual(
                maxDepth,
                LakeBathymetry.BaseDepthFromShoreDistance(radius, radius, maxDepth, s),
                0.05f,
                "inside inner contour floor stays flat");
        }

        [Test]
        public void InsetContour_IsMonotonicOnWall_ThenConstantPlateau()
        {
            const float radius = 80f;
            const float maxDepth = 36f;
            var s = LakeBathymetry.Settings.NaturalDefault;
            s.InnerContourScale = 0.55f;
            s.NoiseAmplitude = 0f;
            var wall = LakeBathymetry.WallWidthMeters(radius, s);
            var prev = -1f;
            for (var i = 0; i <= 20; i++)
            {
                var d = wall * (i / 20f);
                var dig = LakeBathymetry.BaseDepthFromShoreDistance(d, radius, maxDepth, s);
                Assert.GreaterOrEqual(dig, prev - 1e-4f);
                prev = dig;
            }

            Assert.AreEqual(
                maxDepth,
                LakeBathymetry.BaseDepthFromShoreDistance(wall * 1.1f, radius, maxDepth, s),
                0.05f);
            Assert.AreEqual(
                maxDepth,
                LakeBathymetry.BaseDepthFromShoreDistance(radius * 0.95f, radius, maxDepth, s),
                0.05f);
        }

        [Test]
        public void WallArc_P2_JoinsFloorWithZeroSlopeHint()
        {
            // 1-(1-t)^2 → at t→1 slope → 0 (plate join). Check values near floor.
            var a = LakeBathymetry.WallArc01(0.95f, 2f);
            var b = LakeBathymetry.WallArc01(1f, 2f);
            Assert.Greater(b, a);
            Assert.AreEqual(1f, b, 1e-5f);
            Assert.Less(b - a, 0.1f);
        }

        [Test]
        public void LegacyFlatBottomWrapper_UsesInsetFloor()
        {
            const float radius = 100f;
            const float maxDepth = 40f;
            var midWall = LakeTerrainCarver.FlatBottomBowlDigMeters(25f, radius, maxDepth, 0.5f);
            var floor = LakeTerrainCarver.FlatBottomBowlDigMeters(50f, radius, maxDepth, 0.5f);
            var centre = LakeTerrainCarver.FlatBottomBowlDigMeters(100f, radius, maxDepth, 0.5f);
            Assert.Less(midWall, maxDepth);
            Assert.AreEqual(maxDepth, floor, 0.05f);
            Assert.AreEqual(maxDepth, centre, 0.05f);
        }

        [Test]
        public void MaxDepthForLake_SmallShallow_LargeCapped_NotLinearInR()
        {
            var small = LakeBathymetry.MaxDepthForLake(12f, 1f, 40f, 36f, 48f);
            var large = LakeBathymetry.MaxDepthForLake(80f, 1f, 40f, 36f, 48f);
            var huge = LakeBathymetry.MaxDepthForLake(200f, 1f, 40f, 36f, 48f);
            Assert.Less(small, 10f);
            Assert.Greater(large, small);
            Assert.AreEqual(huge, large, 0.05f);
            Assert.LessOrEqual(huge, 48f);
        }
    }
}
