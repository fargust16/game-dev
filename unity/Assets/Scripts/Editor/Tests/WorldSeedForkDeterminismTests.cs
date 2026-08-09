using GameDev.World.Seed;
using NUnit.Framework;

namespace GameDev.World.Seed.Tests
{
    public sealed class WorldSeedForkDeterminismTests
    {
        [Test]
        public void SameSeedAndStream_ProducesIdenticalSequence()
        {
            const int seed = 110201;
            const int sampleCount = 32;

            var a = new WorldSeed(seed).Fork("terrain");
            var b = new WorldSeed(seed).Fork("terrain");

            for (var i = 0; i < sampleCount; i++)
            {
                Assert.AreEqual(a.Next(), b.Next());
            }
        }

        [Test]
        public void DifferentStreamIds_ProduceDifferentSequences()
        {
            const int seed = 110201;

            var terrain = new WorldSeed(seed).Fork("terrain");
            var poi = new WorldSeed(seed).Fork("poi");

            Assert.AreNotEqual(terrain.Next(), poi.Next());
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var a = new WorldSeed(110201).Fork("terrain");
            var b = new WorldSeed(220301).Fork("terrain");

            Assert.AreNotEqual(a.Next(), b.Next());
        }
    }
}
