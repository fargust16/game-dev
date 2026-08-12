using GameDev.World.Water;
using NUnit.Framework;

namespace GameDev.World.Water.Tests
{
    public sealed class WaterProximityTests
    {
        private sealed class PointWater : IWaterSampler
        {
            private readonly float wetX;
            private readonly float wetZ;
            private readonly float radius;

            public PointWater(float wetX, float wetZ, float radius)
            {
                this.wetX = wetX;
                this.wetZ = wetZ;
                this.radius = radius;
            }

            public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
            {
                var dx = worldX - wetX;
                var dz = worldZ - wetZ;
                if (dx * dx + dz * dz <= radius * radius)
                {
                    waterY = 10f;
                    return true;
                }

                waterY = 0f;
                return false;
            }
        }

        [Test]
        public void SampleWetness01_OnWater_IsOne()
        {
            var water = new PointWater(0f, 0f, 5f);
            Assert.AreEqual(1f, WaterProximity.SampleWetness01(water, 0f, 0f, 40f), 1e-5f);
        }

        [Test]
        public void SampleWetness01_FarFromWater_IsZero()
        {
            var water = new PointWater(0f, 0f, 5f);
            Assert.AreEqual(0f, WaterProximity.SampleWetness01(water, 200f, 200f, 40f), 1e-5f);
        }

        [Test]
        public void SampleWetness01_NearShore_IsBetween()
        {
            var water = new PointWater(0f, 0f, 5f);
            var wetness = WaterProximity.SampleWetness01(water, 12f, 0f, 40f);
            Assert.Greater(wetness, 0f);
            Assert.Less(wetness, 1f);
        }

        [Test]
        public void IsUnderOrTooCloseToWater_ShoreBuffer_BlocksNearEdge()
        {
            var water = new PointWater(0f, 0f, 5f);
            Assert.IsTrue(WaterProximity.IsUnderOrTooCloseToWater(water, 6.2f, 0f, 20f, 1.5f));
            Assert.IsFalse(WaterProximity.IsUnderOrTooCloseToWater(water, 20f, 0f, 20f, 1.5f));
        }
    }
}
