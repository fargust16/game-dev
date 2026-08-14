using System.Collections.Generic;
using System.Reflection;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeTerrainCarverSpillTests
    {
        [Test]
        public void SpillWaterLevel_UsesLowRimPercentileMinusDepthOffset()
        {
            var method = typeof(LakeTerrainCarver).GetMethod(
                "SpillWaterLevel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "SpillWaterLevel helper must exist");

            var rim = new List<float>(100);
            for (var i = 0; i < 50; i++)
            {
                rim.Add(20f);
            }

            for (var i = 0; i < 50; i++)
            {
                rim.Add(30f);
            }

            var waterY = (float)method.Invoke(null, new object[] { rim, 99f });
            var offset = Mathf.Max(
                GameDev.World.WorldScale.WaterDepthOffsetMeters,
                GameDev.World.WorldScale.WaterInsetDepthMeters);
            // Sorted: [20×50, 30×50]; p18 → still in the 20m block.
            var expected = 20f - offset;
            Assert.AreEqual(expected, waterY, 0.05f);
            Assert.Less(waterY, 25f - offset, "must sit below average-rim spill");
        }

        [Test]
        public void WaterInset_IsWideEnoughToHideEdges()
        {
            Assert.GreaterOrEqual(GameDev.World.WorldScale.WaterOverlapMeters, 1.0f);
            Assert.LessOrEqual(GameDev.World.WorldScale.WaterOverlapMeters, 2.5f);
            Assert.GreaterOrEqual(GameDev.World.WorldScale.WaterInsetDepthMeters, 0.5f);
            Assert.LessOrEqual(GameDev.World.WorldScale.WaterInsetDepthMeters, 1.5f);
            Assert.Greater(
                GameDev.World.WorldScale.LakeBasinExpandMeters,
                GameDev.World.WorldScale.WaterOverlapMeters);
            Assert.GreaterOrEqual(GameDev.World.WorldScale.LakeShoreLipFillMaxMeters, 8f);
            Assert.GreaterOrEqual(LakeTerrainCarver.ShoreStripBlurMeters, 8f);
        }

        [Test]
        public void WaterFreeboard_MatchesInsetDepth()
        {
            Assert.AreEqual(
                GameDev.World.WorldScale.WaterInsetDepthMeters,
                LakeTerrainCarver.WaterFreeboardMeters,
                0.01f);
        }

        [Test]
        public void BankBlend_IsShortExteriorOnly()
        {
            Assert.LessOrEqual(LakeTerrainCarver.BankBlendMeters, 10f,
                "wide dry moats create gaps under the water sheet and berms");
            Assert.Greater(LakeTerrainCarver.BankBlendMeters, 1f);
        }

        [Test]
        public void LakeMinCount_IsAtLeastThree()
        {
            Assert.GreaterOrEqual(GameDev.World.WorldScale.LakeMinCount, 3);
            Assert.AreEqual(DistributedLakePlacer.DefaultMinLakes, GameDev.World.WorldScale.LakeMinCount);
        }
    }
}
