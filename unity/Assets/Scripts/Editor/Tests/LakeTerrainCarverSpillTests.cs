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
        public void SpillWaterLevel_UsesLowRimNotMedian()
        {
            var method = typeof(LakeTerrainCarver).GetMethod(
                "SpillWaterLevel",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "SpillWaterLevel helper must exist");

            // 100 samples: low cluster at 10, high cluster at 40 — median ~40, spill ~10.
            var rim = new List<float>(100);
            for (var i = 0; i < 20; i++)
            {
                rim.Add(10f + i * 0.05f);
            }

            for (var i = 0; i < 80; i++)
            {
                rim.Add(40f + i * 0.01f);
            }

            var waterY = (float)method.Invoke(null, new object[] { rim, 99f });
            Assert.Less(waterY, 15f, "spill must track the low outlet, not the high bank");
            Assert.Greater(waterY, 8f);
        }

        [Test]
        public void WaterFreeboard_IsPositiveAndSmall()
        {
            Assert.Greater(LakeTerrainCarver.WaterFreeboardMeters, 0.05f);
            Assert.Less(LakeTerrainCarver.WaterFreeboardMeters, 1f);
        }

        [Test]
        public void RimSpillPercentile_IsLowOutlet()
        {
            Assert.Less(LakeTerrainCarver.RimSpillPercentile, 0.35f);
            Assert.Greater(LakeTerrainCarver.RimSpillPercentile, 0.05f);
        }
    }
}
