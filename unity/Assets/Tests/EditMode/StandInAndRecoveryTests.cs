using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.People;
using GameDev.Slice.Shops;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S5c.7: Pip keeps Harrow's shop once Harrow is lost (11, SLICE_PEOPLE §1), and a fallen person's equipment is recovered by 19 §6.</summary>
    public sealed class StandInAndRecoveryTests
    {
        private const int Day = 1440;

        private ItemCatalogue _items;
        private WorldState _state;

        [SetUp]
        public void SetUp()
        {
            _items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            _state = new WorldState();
        }

        [Test]
        public void PipOpensTheShopTheMorningAfterHarrowIsLost()
        {
            var standIn = new ShopStandIn(_state, null);
            var afternoon = (14 * 60) + 30L;
            Assert.AreEqual("harrow", standIn.KeptBy("harrow", afternoon), "Harrow keeps his shop while he is there");

            _state.Raise(ShopStandIn.VanishedKey);
            standIn.Notice(afternoon);
            Assert.AreEqual(Day + (8 * 60), ShopStandIn.NextMorning(afternoon));
            Assert.AreEqual("harrow", standIn.KeptBy("harrow", Day + (8 * 60) - 1), "the shop stays as it was until the morning");
            Assert.AreEqual("pip", standIn.KeptBy("harrow", Day + (8 * 60)), "Pip keeps it from 08:00");
            Assert.AreEqual("ada", standIn.KeptBy("ada", Day + (8 * 60)), "no other shop changes hands");

            Assert.AreEqual(8 * 60, ShopStandIn.NextMorning(7 * 60), "at 07:00 the same morning");
            Assert.AreEqual(Day + (8 * 60), ShopStandIn.NextMorning(8 * 60), "at 08:00 itself the next morning");
        }

        [Test]
        public void ASealedShopStaysShutAndAFallenHarrowCountsAsLost()
        {
            _state.Raise(ShopStandIn.TakenKey);
            _state.Raise(ShopStandIn.SealedKey);
            var sealedShop = new ShopStandIn(_state, null);
            Assert.IsTrue(sealedShop.Lost);
            Assert.IsFalse(sealedShop.Keeps(3 * Day), "the Wardens sealed it: nobody keeps it");

            var fallen = new ShopStandIn(new WorldState(), person => person == "harrow");
            fallen.Notice(10 * 60);
            Assert.IsTrue(fallen.Keeps(Day + (8 * 60)), "Harrow fell: Pip opens the next morning");
        }

        [Test]
        public void PipTeachesAtDoubleThePriceAndKeepsHisOwnHours()
        {
            var rules = AssetDatabase.LoadAssetAtPath<ShopCatalogue>("Assets/Data/Slice/Shops.asset");
            var sheet = new CharacterSheet();
            var bag = SliceWorldDriver.StartingBag(_items, sheet);
            var clock = new GameClock(1d, Day + (9 * 60));
            var shops = new SliceShops(rules, _items, new WorldSeed(110201), clock, bag, _state);
            var standIn = new ShopStandIn(_state, null);
            shops.KeptBy = keeper => standIn.KeptBy(keeper, clock.Tick);
            shops.KeeperAtWork = keeper => keeper == "pip";

            Assert.AreEqual(rules.LessonPrice, shops.LessonPrice("harrow"));
            Assert.IsFalse(shops.IsOpenFor("harrow"), "Harrow is not at work, and Pip does not keep the shop yet");

            _state.Raise(ShopStandIn.VanishedKey);
            standIn.Notice((14 * 60) + 30);
            Assert.AreEqual("pip", shops.Keeper("harrow"));
            Assert.IsTrue(shops.IsOpenFor("harrow"), "the shop is open while Pip is at work");
            Assert.AreEqual(rules.LessonPrice * ShopStandIn.LessonFactor, shops.LessonPrice("harrow"));
            Assert.AreEqual(rules.LessonPrice, shops.LessonPrice("ada"), "Ada still teaches at the price");
            Assert.AreEqual(SliceHours.WorkOpens("pip"), shops.Opens("harrow"), "the shop opens at Pip's hour");

            bag.Earn((rules.LessonPrice * ShopStandIn.LessonFactor) - bag.Bronze - 1);
            Assert.AreEqual("ui.shop.fault.money", shops.Learn("harrow", "alchemy"), "a bronze short of the double price");
            bag.Earn(1);
            Assert.IsNull(shops.Learn("harrow", "alchemy"));
            Assert.AreEqual(0, bag.Bronze, "the lesson cost the double price");
        }

        [Test]
        public void TheChanceOfEachPieceIs19s()
        {
            Assert.AreEqual(0.35d, SliceRecovery.Chance(0), 1e-9);
            Assert.AreEqual(0.3535d, SliceRecovery.Chance(1), 1e-9);
            Assert.AreEqual(0.70d, SliceRecovery.Chance(100), 1e-9);
            Assert.AreEqual(0.99d, SliceRecovery.Chance(300), 1e-9, "the cap");
        }

        [Test]
        public void AFallenPersonsGearIsSearchedOnceTheSameWayEachTime()
        {
            var body = new GameObject("fallen warden").transform;
            try
            {
                var first = Recovery(out var sheet, out _);
                Assert.IsFalse(first.CanSearch(body), "nothing was written down: no body to search");
                first.Leave(body, new[] { "rifle", "fists", "cartridge_pouch", "pistol" });
                CollectionAssert.AreEqual(new[] { "rifle", "cartridge_pouch", "pistol" }, first.Gear(body).ToArray(), "what the catalogue makes, in order");
                Assert.IsTrue(first.CanSearch(body));

                var luck = (sheet.Get(CharacterAttribute.Luck), sheet.Xp(CharacterAttribute.Luck));
                var (taken, heavy) = first.Search(body);
                Assert.IsFalse(first.CanSearch(body), "a body is searched once");
                Assert.AreEqual(0, first.Search(body).Taken.Count);
                Assert.AreEqual(0, heavy.Count);
                Assert.AreNotEqual(luck, (sheet.Get(CharacterAttribute.Luck), sheet.Xp(CharacterAttribute.Luck)), "the roll trains Luck");

                // The same seed, body and Luck recover the same pieces: a save keeps the roll.
                var again = Recovery(out _, out _, new WorldState());
                again.Leave(body, new[] { "rifle", "fists", "cartridge_pouch", "pistol" });
                CollectionAssert.AreEqual(taken.Select(p => p.item).ToArray(), again.Search(body).Taken.Select(p => p.item).ToArray());
            }
            finally
            {
                Object.DestroyImmediate(body.gameObject);
            }
        }

        [Test]
        public void APieceTooHeavyStaysAndABareBodyGivesNothing()
        {
            var body = new GameObject("fallen rifleman").transform;
            var bare = new GameObject("fallen walker").transform;
            try
            {
                var recovery = Recovery(out var sheet, out var bag);
                sheet.Set(CharacterAttribute.Luck, 100); // each piece at 70 %
                while (bag.CanCarry(_items.Find("rifle").weight))
                {
                    Assert.IsTrue(bag.AddPiece(new ItemPiece("rifle")));
                }

                var pieces = bag.Pieces.Count;
                recovery.Leave(body, Enumerable.Repeat("rifle", 6));
                var (taken, heavy) = recovery.Search(body);
                Assert.AreEqual(0, taken.Count);
                Assert.IsTrue(heavy.Count > 0 && heavy.All(id => id == "rifle"), "the rifles rolled are too heavy and stay");
                Assert.AreEqual(pieces, bag.Pieces.Count);

                recovery.Leave(bare, new string[0]);
                var luck = sheet.Xp(CharacterAttribute.Luck);
                var nothing = recovery.Search(bare);
                Assert.AreEqual(0, nothing.Taken.Count + nothing.TooHeavy.Count);
                Assert.AreEqual(luck, sheet.Xp(CharacterAttribute.Luck), "nothing to roll, nothing trained");
            }
            finally
            {
                Object.DestroyImmediate(body.gameObject);
                Object.DestroyImmediate(bare.gameObject);
            }
        }

        private SliceRecovery Recovery(out CharacterSheet sheet, out Bag bag, WorldState state = null)
        {
            sheet = new CharacterSheet();
            bag = new Bag(_items, sheet);
            return new SliceRecovery(state ?? _state, new WorldSeed(110201), bag, sheet, _items);
        }
    }
}
