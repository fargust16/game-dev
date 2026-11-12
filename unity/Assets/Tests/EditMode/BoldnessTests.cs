using GameDev.Slice.People;
using GameDev.World.Seed;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.2 §5e (owner, 2026-11-12, VS-ALARM (f); PB8c): a person's Boldness is drawn once by the seed from its name, and
    /// it goes to look at an Alarm when its Boldness is higher than 1 minus its role's share, so about that share of a
    /// role's people go.
    /// </summary>
    public sealed class BoldnessTests
    {
        [Test]
        public void TheSharesAreTheRolesOwn()
        {
            Assert.AreEqual(1d / 2d, ResidentRoles.LookShare(ResidentRole.OldPriest));
            Assert.AreEqual(1d / 2d, ResidentRoles.LookShare(ResidentRole.Acolyte));
            Assert.AreEqual(1d / 2d, ResidentRoles.LookShare(ResidentRole.Dockworker));
            Assert.AreEqual(2d / 5d, ResidentRoles.LookShare(ResidentRole.Vagrant));
            Assert.AreEqual(1d / 4d, ResidentRoles.LookShare(ResidentRole.FactoryWorker));
            Assert.AreEqual(1d / 4d, ResidentRoles.LookShare(ResidentRole.OldDockworker));
            Assert.AreEqual(1d / 4d, ResidentRoles.LookShare(ResidentRole.Innkeeper));
            Assert.AreEqual(1d / 5d, ResidentRoles.LookShare(ResidentRole.OldBeliever));
            Assert.AreEqual(1d / 6d, ResidentRoles.LookShare(ResidentRole.Townsfolk));
            Assert.AreEqual(1d / 10d, ResidentRoles.LookShare(ResidentRole.ChurchMaid));
            Assert.AreEqual(1d / 10d, ResidentRoles.LookShare(ResidentRole.Moneylender));
            Assert.AreEqual(1d / 10d, ResidentRoles.LookShare(ResidentRole.Tailor));
            Assert.AreEqual(1d / 4d, ResidentRoles.LookShare(ResidentRole.Townsfolk, otherResident: true), "a Master or a servant");
        }

        [Test]
        public void BoldnessIsTheSameForTheSameNameAndWorld()
        {
            var seed = new WorldSeed(1234);
            var a = ResidentRoles.Boldness("Gentleman#3", seed);
            Assert.AreEqual(a, ResidentRoles.Boldness("Gentleman#3", new WorldSeed(1234)), "the same in every run of a world");
            Assert.That(a, Is.InRange(0d, 1d));
            Assert.AreNotEqual(a, ResidentRoles.Boldness("Gentleman#4", seed), "another person, another Boldness");
            Assert.AreNotEqual(a, ResidentRoles.Boldness("Gentleman#3", new WorldSeed(99)), "another world, another Boldness");
        }

        [Test]
        public void AboutTheRolesShareGoToLook()
        {
            var seed = new WorldSeed(77);
            foreach (var role in new[] { ResidentRole.OldPriest, ResidentRole.Vagrant, ResidentRole.Townsfolk, ResidentRole.Tailor })
            {
                var share = ResidentRoles.LookShare(role);
                var go = 0;
                const int people = 4000;
                for (var i = 0; i < people; i++)
                {
                    go += ResidentRoles.GoesToLook(ResidentRoles.Boldness($"{role}#{i}", seed), share) ? 1 : 0;
                }

                Assert.AreEqual(share, go / (double)people, 0.03, $"{role}: about its share go");
            }

            Assert.IsFalse(ResidentRoles.GoesToLook(0.5, 0.5), "exactly 1 minus the share stays");
            Assert.IsTrue(ResidentRoles.GoesToLook(0.51, 0.5), "higher goes");
        }
    }
}
