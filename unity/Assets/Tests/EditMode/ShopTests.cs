using System;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.Slice.Gear;
using GameDev.Slice.Shops;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.3: one maker for every piece by 19 §3 and §2.4, a morning's stock, buying, selling and lessons.</summary>
    public sealed class ShopTests
    {
        private ItemCatalogue _items;
        private ShopCatalogue _rules;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceShops _shops;

        [SetUp]
        public void SetUp()
        {
            _items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            _rules = AssetDatabase.LoadAssetAtPath<ShopCatalogue>("Assets/Data/Slice/Shops.asset");
            _sheet = new CharacterSheet();
            _sheet.Set(CharacterAttribute.Strength, 20);
            _bag = SliceWorldDriver.StartingBag(_items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _shops = new SliceShops(_rules, _items, new WorldSeed(110201), _clock, _bag, _state);
        }

        [Test]
        public void TheChanceOfEachAdditionIs19s()
        {
            Assert.AreEqual(0.101d, ItemMaker.Chance(1, 1, 1), 1e-9, "10.1 % at 1 / 1 / 1");
            Assert.AreEqual(0.99d, ItemMaker.Chance(100, 100, 100), 1e-9, "the cap");
            Assert.AreEqual(0.10d + (0.60d * 25d / 99d), ItemMaker.ShopChance(26), 1e-9, "a shop: no Perception or Luck");
        }

        [TestCase(26, new[] { 17.6, 65.2, 13.3, 3.4, 0.5 })]
        [TestCase(50, new[] { 4.8, 50.3, 27.4, 13.6, 4.0 })]
        public void AShopsQualitySpreadIs19sTable(int skill, double[] shares)
        {
            var sword = _items.Find("sword");
            var rolls = new Random(7);
            var counts = new int[5];
            const int Pieces = 40000;
            for (var i = 0; i < Pieces; i++)
            {
                counts[(int)ItemMaker.Make(sword, 9, ItemMaker.ShopChance(skill), rolls).Quality]++;
            }

            for (var q = 0; q < 5; q++)
            {
                Assert.AreEqual(shares[q], 100d * counts[q] / Pieces, 0.8d, $"{(ItemQuality)q} at Shop Skill {skill}");
            }
        }

        [Test]
        public void ModifiersKeepTheirGroupsTiersAndValues()
        {
            var rolls = new Random(11);
            var favoured = 0;
            var total = 0;
            foreach (var item in _items.Items.Where(i => i.kind == ItemKind.Equipment && i.price > 0))
            {
                for (var n = 0; n < 200; n++)
                {
                    var piece = ItemMaker.Make(item, 9, 0.7d, rolls);
                    Assert.LessOrEqual(piece.modifiers.Count, 6);
                    Assert.LessOrEqual(piece.modifiers.Count(m => m.major), 3, item.id);
                    Assert.LessOrEqual(piece.modifiers.Count(m => !m.major), 3, item.id);
                    foreach (var m in piece.modifiers)
                    {
                        Assert.IsTrue(ItemMaker.TryType(m, out var type), m.type);
                        Assert.That(m.tier, Is.InRange(7, 9));
                        var centre = ItemMaker.MajorValue(type) * (1d + (0.5d * (9 - m.tier))) * (m.major ? 1d : 0.5d);
                        Assert.That(m.value, Is.InRange(centre * 0.8d - 1e-4, centre * 1.2d + 1e-4), $"{item.id} {m.type}");
                        favoured += ItemMaker.Favoured(item).Contains(type) ? 1 : 0;
                        total++;
                    }
                }
            }

            Assert.Greater(favoured / (double)total, 0.78d, "75 % favoured, and some of the rest falls on a favoured type too");
        }

        [Test]
        public void AMorningFillsEachShelfByTheSeed()
        {
            var tobias = _shops.Shelf("tobias");
            var bases = _items.Items.Count(i => i.kind == ItemKind.Equipment && i.price > 0 && !SliceShops.Withheld(i));
            Assert.AreEqual(bases, tobias.Count(s => s.Piece != null), "one piece of each base");
            Assert.IsFalse(tobias.Any(s => SliceShops.Withheld(s.Item)), "10.2 §5d: no melee weapon or shield on the shelf");
            Assert.IsTrue(tobias.Any(s => s.Item.id == "pistol") && tobias.Any(s => s.Item.id == "rifle") && tobias.Any(s => s.Item.id == "cartridge_pouch"), "the firearms and their pouch");
            Assert.IsTrue(SliceShops.Withheld(_items.Find("sword")) && SliceShops.Withheld(_items.Find("dagger")) && SliceShops.Withheld(_items.Find("metal_shield")), "the sword, the dagger and the shield stay as data");
            Assert.That(tobias.Single(s => s.Piece == null).Count, Is.InRange(1, 3), "Lockpicks, one to three");
            Assert.AreEqual(6, _shops.Shelf("edda").Count);
            Assert.IsTrue(_shops.Shelf("edda").All(s => s.Count >= 1 && s.Count <= 3));
            Assert.AreEqual(26, _shops.ShopSkill);

            var again = new SliceShops(_rules, _items, new WorldSeed(110201), _clock, _bag, _state).Shelf("tobias");
            CollectionAssert.AreEqual(tobias.Select(s => s.Piece?.Quality), again.Select(s => s.Piece?.Quality), "the same seed fills the same shelf");

            _clock.AdvanceTicks(24 * 60);
            Assert.AreEqual(27, _shops.ShopSkill, "one more a day");
            Assert.AreNotSame(tobias[0], _shops.Shelf("tobias")[0], "a new morning makes a new shelf");
        }

        [Test]
        public void ABuyASaleAndALesson()
        {
            var herbs = _shops.Shelf("ada").Single(s => s.Item.id == "herb");
            var had = herbs.Count;
            Assert.IsNull(_shops.Buy("ada", herbs));
            Assert.AreEqual(400, _bag.Bronze);
            Assert.AreEqual(1, _bag.Count("herb"));
            Assert.AreEqual(had - 1, herbs.Count);

            Assert.IsNull(_shops.Sell("ada", _items.Find("herb"), null));
            Assert.AreEqual(425, _bag.Bronze, "a quarter of 100");
            Assert.AreEqual(had, herbs.Count, "what is sold stays on the shelf until the morning");
            Assert.AreEqual("ui.shop.fault.gone", _shops.Sell("edda", _items.Find("herb"), null), "Edda buys no herbs");

            Assert.AreEqual("ui.shop.fault.money", _shops.Learn("ada", "firstaid"));
            _bag.Earn(1000);
            Assert.IsNull(_shops.Learn("ada", "firstaid"));
            Assert.IsTrue(_state.IsSet(SliceShops.LessonKey("firstaid")));
            Assert.AreEqual(SliceConfront.LessonKey, SliceShops.LessonKey("firstaid"), "the lesson the fight's First Aid reads");
            Assert.AreEqual("ui.shop.fault.known", _shops.Learn("ada", "firstaid"));
            Assert.AreEqual("ui.shop.fault.gone", _shops.Learn("ada", "skinning"), "Ada does not teach Skinning");

            var essence = _shops.Shelf("silas").Single(s => s.Item.id == "glow_essence_8");
            Assert.AreEqual("ui.shop.fault.money", _shops.Buy("silas", essence));
            _bag.Add("glow_essence_8");
            var before = _bag.Bronze;
            Assert.IsNull(_shops.Sell("silas", _items.Find("glow_essence_8"), null));
            Assert.AreEqual(before + 4000, _bag.Bronze, "19 §6.3: the bright essence sells for 4 000");

            _clock.AdvanceTicks(12 * 60);
            Assert.IsFalse(_shops.IsOpen, "21:00");
            Assert.AreEqual("ui.shop.fault.closed", _shops.Buy("ada", _shops.Shelf("ada")[0]));
        }

        [Test]
        public void AWornPieceIsNotSoldAndABoughtPieceKeepsItsModifiers()
        {
            _bag.Earn(30000);
            var outfit = new Outfit(_bag);
            // A piece worn alone: a firearm needs its pouch beside it (10.2 §5b), so a piece of armour or jewellery.
            var piece = _shops.Shelf("tobias").First(s => s.Piece != null && s.Piece.modifiers.Count > 0 && s.Item.slot > EquipSlot.TwoHands && s.Item.slot != EquipSlot.OffHand);
            Assert.IsNull(_shops.Buy("tobias", piece));
            CollectionAssert.Contains(_bag.Pieces, piece.Piece);
            outfit.Wear(piece.Piece);
            Assert.IsFalse(_shops.Sellable("tobias", outfit.IsWorn).Any(s => s.piece == piece.Piece), "a worn piece is not offered");
            Assert.IsNull(outfit.TakeOff(piece.Piece));
            Assert.IsTrue(_shops.Sellable("tobias", outfit.IsWorn).Any(s => s.piece == piece.Piece));
            Assert.AreEqual(piece.Piece.Price / 4, SliceShops.SalePrice(piece.Item, piece.Piece));
        }

        [Test]
        public void APiecesModifiersCountInAFightByWhatItsWearerMeets()
        {
            var sword = new ItemPiece("sword");
            sword.modifiers.Add(new ItemModifier { type = nameof(ModifierType.Damage), major = true, tier = 9, value = 0.12f });
            sword.modifiers.Add(new ItemModifier { type = nameof(ModifierType.Accuracy), major = false, tier = 9, value = 2.5f });
            _bag.AddPiece(sword);
            var outfit = new Outfit(_bag);
            outfit.Wear(sword);
            _sheet.Set(CharacterAttribute.Dexterity, 20);
            var mods = outfit.Modifiers(_sheet);
            Assert.AreEqual(0.12f, mods.damage, 1e-5f);
            Assert.AreEqual(2.5f, mods.accuracy, 1e-5f);

            var fit = (float)outfit.FitOf(sword, new CharacterSheet());
            Assert.Less(fit, 1f, "a sword asks Strength and Dexterity");
            Assert.AreEqual(0.12f * fit, outfit.Modifiers(new CharacterSheet()).damage, 1e-5f);
            var rules = AssetDatabase.LoadAssetAtPath<FightRules>("Assets/Data/Confront/FightRules.asset");
            var fists = AssetDatabase.LoadAssetAtPath<FightWeapon>("Assets/Data/Confront/Weapons/Fists.asset");
            Assert.AreEqual(0.12f, outfit.Fighter(rules, _sheet, fists).Modifiers.damage, 1e-5f);
        }
    }
}
