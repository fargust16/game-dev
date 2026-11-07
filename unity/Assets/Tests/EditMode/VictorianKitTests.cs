using System.Linq;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// VI-1 (VICTORIAN_INTERIORS_PLAN §8): every Victorian Interiors block builds as a kit block, the bookcases carry
    /// their books, and the sample rooms stand without a kit fault. The wrappers are rebuilt the same each time.
    /// </summary>
    public sealed class VictorianKitTests
    {
        [Test]
        public void EveryVictorianBlockBuildsAndTheSamplesAreClean()
        {
            var kit = SliceKitCatalogue.BuildVictorian();
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
    }
}
