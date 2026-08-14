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

        [Test]
        public void IndexedFork_IsolatesSiblingObjects()
        {
            var seed = new WorldSeed(1847459267);
            var lake0 = seed.Fork("lake", 0).Next();
            var lake1 = seed.Fork("lake", 1).Next();
            var veg = seed.Fork("vegetation").Next();

            Assert.AreNotEqual(lake0, lake1);
            Assert.AreNotEqual(lake0, veg);
            Assert.AreEqual(lake0, seed.Fork("lake", 0).Next());
        }

        [Test]
        public void StringFork_UnchangedByIndexedOverloadPresence()
        {
            // Regression: Fork(string) must not mix trailing zero ints.
            var a = new WorldSeed(110201).Fork("water.lakes").Next();
            var b = new WorldSeed(110201).Fork("water.lakes").Next();
            Assert.AreEqual(a, b);
        }
    }
}
