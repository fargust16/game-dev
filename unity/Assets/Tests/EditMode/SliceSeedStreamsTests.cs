using System.Linq;
using GameDev.World.Seed;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    public sealed class SliceSeedStreamsTests
    {
        private const int Seed = 110201;

        [Test]
        public void StreamNamesAreDistinct()
        {
            Assert.AreEqual(SliceSeedStreams.All.Count, SliceSeedStreams.All.Distinct().Count());
        }

        [Test]
        public void EachStreamGivesItsOwnSequence()
        {
            var seed = new WorldSeed(Seed);
            var firsts = SliceSeedStreams.All.Select(name => seed.Fork(name).Next()).ToList();
            Assert.AreEqual(firsts.Count, firsts.Distinct().Count());
        }

        [Test]
        public void TheSameSeedRepeatsTheRun()
        {
            foreach (var name in SliceSeedStreams.All)
            {
                var a = new WorldSeed(Seed).Fork(name);
                var b = new WorldSeed(Seed).Fork(name);
                for (var i = 0; i < 16; i++)
                {
                    Assert.AreEqual(a.Next(), b.Next(), name);
                }
            }
        }

        [Test]
        public void FightsForkApartByNumber()
        {
            var seed = new WorldSeed(Seed);
            Assert.AreNotEqual(seed.Fork(SliceSeedStreams.Combat, 1).Next(), seed.Fork(SliceSeedStreams.Combat, 2).Next());
        }
    }
}
