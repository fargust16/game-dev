using GameDev.World.Water;
using NUnit.Framework;

namespace GameDev.World.Water.Tests
{
    public sealed class WaterZoneSamplerTests
    {
        private static WaterZone MakeZone(float waterLevelY)
        {
            var flooded = new bool[2, 2];
            flooded[0, 0] = true;
            var heights = new float[2, 2];
            return new WaterZone("z", waterLevelY, 0f, 0f, 1f, flooded, heights);
        }

        [Test]
        public void TryGetWaterLevel_InsideZone_ReturnsZoneLevel()
        {
            var sampler = new WaterZoneSampler(new[] { MakeZone(12f) });

            Assert.IsTrue(sampler.TryGetWaterLevel(0.5f, 0.5f, out var y));
            Assert.AreEqual(12f, y);
        }

        [Test]
        public void TryGetWaterLevel_OutsideZone_ReturnsFalse()
        {
            var sampler = new WaterZoneSampler(new[] { MakeZone(12f) });

            Assert.IsFalse(sampler.TryGetWaterLevel(1.5f, 1.5f, out _));
        }

        [Test]
        public void TryGetWaterLevel_MultipleZonesAtDifferentHeights_EachReportsItsOwnLevel()
        {
            var flat = new bool[1, 1];
            flat[0, 0] = true;
            var heights = new float[1, 1];

            var low = new WaterZone("low", 3f, 0f, 0f, 1f, flat, heights);
            var high = new WaterZone("high", 90f, 10f, 10f, 1f, flat, heights);
            var sampler = new WaterZoneSampler(new[] { low, high });

            Assert.IsTrue(sampler.TryGetWaterLevel(0.5f, 0.5f, out var lowY));
            Assert.AreEqual(3f, lowY);

            Assert.IsTrue(sampler.TryGetWaterLevel(10.5f, 10.5f, out var highY));
            Assert.AreEqual(90f, highY);
        }
    }
}
