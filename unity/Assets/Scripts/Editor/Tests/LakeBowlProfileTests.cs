using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeBowlProfileTests
    {
        [Test]
        public void AnalyticBasin_ShoreIsShallow_DeepFloorIsFlat()
        {
            const float maxDepth = 40f;
            Assert.AreEqual(0f, LakeBathymetry.AnalyticBasinDepth01(0f, maxDepth), 0.05f);
            Assert.Less(LakeBathymetry.AnalyticBasinDepth01(0.10f, maxDepth), maxDepth * 0.12f);
            Assert.Greater(LakeBathymetry.AnalyticBasinDepth01(0.40f, maxDepth), maxDepth * 0.3f);
            Assert.Greater(LakeBathymetry.AnalyticBasinDepth01(0.80f, maxDepth), maxDepth * 0.9f);
            Assert.AreEqual(maxDepth, LakeBathymetry.AnalyticBasinDepth01(1f, maxDepth), 0.05f);
        }

        [Test]
        public void AnalyticBasin_IsMonotonic()
        {
            const float maxDepth = 36f;
            var prev = -1f;
            for (var i = 0; i <= 40; i++)
            {
                var u = i / 40f;
                var dig = LakeBathymetry.AnalyticBasinDepth01(u, maxDepth);
                Assert.GreaterOrEqual(dig, prev - 1e-4f);
                prev = dig;
            }
        }

        [Test]
        public void BaseDepthFromShoreDistance_UsesAnalyticBowl()
        {
            const float radius = 100f;
            const float maxDepth = 40f;
            var s = LakeBathymetry.Settings.NaturalDefault;
            s.NoiseAmplitude = 0f;

            Assert.AreEqual(0f, LakeBathymetry.BaseDepthFromShoreDistance(0f, radius, maxDepth, s), 0.05f);
            var mid = LakeBathymetry.BaseDepthFromShoreDistance(radius * 0.4f, radius, maxDepth, s);
            var deep = LakeBathymetry.BaseDepthFromShoreDistance(radius * 0.9f, radius, maxDepth, s);
            Assert.Greater(mid, maxDepth * 0.2f);
            Assert.Greater(deep, mid);
            Assert.Greater(deep, maxDepth * 0.85f);
        }

        [Test]
        public void WallArc_P2_JoinsFloorWithZeroSlopeHint()
        {
            var a = LakeBathymetry.WallArc01(0.95f, 2f);
            var b = LakeBathymetry.WallArc01(1f, 2f);
            Assert.Greater(b, a);
            Assert.AreEqual(1f, b, 1e-5f);
            Assert.Less(b - a, 0.1f);
        }

        [Test]
        public void LegacyFlatBottomWrapper_StillProducesDepth()
        {
            const float radius = 100f;
            const float maxDepth = 40f;
            var midWall = LakeTerrainCarver.FlatBottomBowlDigMeters(25f, radius, maxDepth, 0.5f);
            var centre = LakeTerrainCarver.FlatBottomBowlDigMeters(100f, radius, maxDepth, 0.5f);
            Assert.Greater(midWall, 0f);
            Assert.GreaterOrEqual(centre, midWall - 0.05f);
        }

        [Test]
        public void MaxDepthForLake_ClampsToConfiguredBand()
        {
            var d = LakeBathymetry.MaxDepthForLake(120f, 1f, 22f, 12f, 26f);
            Assert.GreaterOrEqual(d, 12f);
            Assert.LessOrEqual(d, 26f);
        }

        [Test]
        public void LakeDefinition_BasinExpandedBeyondWater()
        {
            var sil = new LakeSilhouette(
                0f, 0f, 50f, 1f, 0f,
                0f, 0f, 0f, 0f, 0f, 0f, 1,
                false, 0f, 0f, 0f);
            var flooded = new bool[4, 4];
            flooded[1, 1] = true;
            var heights = new float[4, 4];
            var zone = new WaterZone("t", 10f, -10f, -10f, 5f, flooded, heights, sil);
            var def = new LakeDefinition(zone, 36f, basinExpandMeters: 2.5f, waterOverlapMeters: 0.25f);
            // Point just outside water shore should still be inside expanded basin.
            var outsideWater = sil.SignedDistance(52f, 0f);
            Assert.Less(outsideWater, 0f);
            Assert.Greater(def.BasinSignedDistance(52f, 0f), 0f);
        }
    }
}
