using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// VI-1 (VICTORIAN_INTERIORS_PLAN §8): every Victorian Interiors block builds as a kit block, the bookcases carry
    /// their books, and the sample rooms stand without a kit fault. The wrappers on disk are read, not rebuilt: a rebuild
    /// gives a set's parts new file IDs, and the kit houses' rooms turn those parts off by ID (VEILMOUTH_CITY_PLAN §24.7).
    /// </summary>
    public sealed class VictorianKitTests
    {
        [Test]
        public void EveryVictorianBlockBuildsAndTheSamplesAreClean()
        {
            var kit = SliceKitCatalogue.LoadVictorian();
            var specs = SliceKitCatalogue.ViSpecs().Select(s => s.Key).ToList();
            CollectionAssert.AreEquivalent(specs, kit.Keys, "a Victorian spec built no block");

            foreach (var entry in kit.Values)
            {
                Assert.IsNotNull(entry.Prefab, entry.Key);
                Assert.IsNotNull(entry.Prefab.GetComponent<KitBlock>(), entry.Key);
            }

            foreach (var key in new[] { "ViBookcase", "ViBookcaseLow" })
            {
                var books = kit[key].Prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Book");
                Assert.Greater(books, 20, key + " has too few books");
            }

            var faults = VictorianInteriorsSamples.Build(kit, out var blocks);
            Assert.Greater(blocks, 250);
            CollectionAssert.IsEmpty(faults.Select(f => f.ToString()));
        }

        /// <summary>The rooms behind the windows go dark from 22:00 to 01:00 and stay so until dawn (reference plan R3).</summary>
        [Test]
        public void TheNightGrowsLateFromTenToOne()
        {
            float Late(int hour, int minute = 0) => GameDev.VictorianKit.VicStreetLighting.Late((hour * 60) + minute);
            Assert.AreEqual(0f, Late(12), 1e-5f);
            Assert.AreEqual(0f, Late(21, 59), 1e-5f);
            Assert.AreEqual(0f, Late(22), 1e-5f);
            Assert.AreEqual(0.5f, Late(23, 30), 1e-5f);
            Assert.AreEqual(1f, Late(1), 1e-5f);
            Assert.AreEqual(1f, Late(5, 59), 1e-5f);
            Assert.AreEqual(0f, Late(6), 1e-5f);
            Assert.AreEqual(Late(23), GameDev.VictorianKit.VicStreetLighting.Late((23 * 60) + 1440), 1e-5f);
        }
    }
}
