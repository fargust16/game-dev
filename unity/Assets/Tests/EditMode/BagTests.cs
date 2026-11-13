using System.Linq;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.1: the items as data, the bag's weight and load bands, money in coins, and 11's start.</summary>
    public sealed class BagTests
    {
        private const string CataloguePath = "Assets/Data/Items/Items.asset";
        private ItemCatalogue _catalogue;
        private string _language;

        [SetUp]
        public void SetUp()
        {
            _catalogue = AssetDatabase.LoadAssetAtPath<ItemCatalogue>(CataloguePath);
            Assert.IsNotNull(_catalogue, CataloguePath);
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void TearDown()
        {
            Loc.Language = _language;
        }

        [Test]
        public void TheCatalogueIsWholeAndEveryItemIsNamed()
        {
            CollectionAssert.IsEmpty(_catalogue.Problems());
            foreach (var item in _catalogue.Items)
            {
                Assert.IsTrue(Loc.Exists(item.NameKey), item.NameKey);
            }

            Assert.AreEqual(71, _catalogue.Items.Count, "19's resources, ingredients, tools, supplies and equipment bases, Lockpicks, plain clothes, the two broken tools (19 §4.3), the four formulas and four Limit potions (S5b.4), Thomas Wren's key and the letter of introduction (S5c.4), and the paid tasks' locket, letter, crate and censer (S5c.7)");
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>("Assets/Data/Slice/SliceSettings.asset");
            Assert.AreSame(_catalogue, settings.Items, "the slice's settings carry the catalogue");
        }

        [TestCase("iron_ore", 1f)]
        [TestCase("wood", 1.5f)]
        [TestCase("herb", 0.1f)]
        [TestCase("lockpicks", 0.1f)]
        [TestCase("plain_clothes", 1.5f)]
        [TestCase("maul", 6f)]
        [TestCase("cloth_body", 2f)]
        [TestCase("plate_body", 11f)]
        [TestCase("chain_trousers", 4.5f)]
        public void WeightsAre19s(string id, float kg)
        {
            Assert.AreEqual(kg, _catalogue.Find(id).weight, 1e-5f);
        }

        [Test]
        public void ThePlainClothesAreABodyPieceWithNoFamilyNeverSold()
        {
            var clothes = _catalogue.Find(SliceWorldDriver.PlainClothes);
            Assert.AreEqual(ItemKind.Equipment, clothes.kind);
            Assert.AreEqual(EquipSlot.Body, clothes.slot);
            Assert.IsTrue(string.IsNullOrEmpty(clothes.family), "no armour family, so no defence");
            Assert.AreEqual(0, clothes.price);
        }

        [Test]
        public void TheLoadIsReadAgainstComfortableCapacityAndItsBands()
        {
            var bag = new Bag(_catalogue, new CharacterSheet());
            Assert.AreEqual(21f, bag.Comfortable, 1e-5f, "20 kg + Strength 1");
            Assert.IsTrue(bag.Add("wood", 14));
            Assert.AreEqual(21f, bag.Weight, 1e-4f);
            Assert.AreEqual(LoadBand.Normal, bag.Band, "100 % is still Normal");
            Assert.IsTrue(bag.Add("iron_ore", 1));
            Assert.AreEqual(LoadBand.High, bag.Band);
            Assert.IsTrue(bag.Add("iron_ore", 5));
            Assert.AreEqual(LoadBand.Overloaded, bag.Band, "27 kg of 21 is 129 %");
            Assert.IsTrue(bag.Add("iron_ore", 5));
            Assert.AreEqual(LoadBand.CriticalOverload, bag.Band, "32 kg of 21 is 152 %");
        }

        [Test]
        public void APickupBeyondTwiceComfortableCapacityIsRefusedWhole()
        {
            var sheet = new CharacterSheet();
            var bag = new Bag(_catalogue, sheet);
            Assert.IsTrue(bag.Add("iron_ore", 40));
            Assert.IsFalse(bag.Add("iron_ore", 3), "43 kg is past 42 kg");
            Assert.AreEqual(40, bag.Count("iron_ore"), "nothing of a refused pickup is taken");
            Assert.IsTrue(bag.Add("iron_ore", 2), "exactly 42 kg is carried");
            Assert.IsFalse(bag.AddPiece(new ItemPiece("dagger")));
            sheet.Set(CharacterAttribute.Strength, 10);
            Assert.IsTrue(bag.AddPiece(new ItemPiece("dagger")), "Strength 10 carries up to 60 kg");
        }

        [Test]
        public void TakingOutOrPayingMoreThanThereIsChangesNothing()
        {
            var bag = new Bag(_catalogue, new CharacterSheet());
            var changes = 0;
            bag.Changed += () => changes++;
            bag.Add("bandage", 2);
            bag.Earn(150);
            Assert.IsFalse(bag.Remove("bandage", 3));
            Assert.IsFalse(bag.Spend(151));
            Assert.AreEqual(2, bag.Count("bandage"));
            Assert.AreEqual(150, bag.Bronze);
            Assert.AreEqual(2, changes, "a refusal raises no change");
            Assert.IsTrue(bag.Remove("bandage", 2));
            Assert.AreEqual(0, bag.Count("bandage"));
            Assert.IsTrue(bag.Spend(150));
            Assert.AreEqual(0, bag.Bronze);
            Assert.IsFalse(bag.Add("dagger"), "equipment is carried as pieces, never stacked");
        }

        [TestCase(0, "0 bronze")]
        [TestCase(75, "75 bronze")]
        [TestCase(375, "3 silver 75 bronze")]
        [TestCase(500, "5 silver")]
        [TestCase(25000, "2 gold 50 silver")]
        [TestCase(10001, "1 gold 1 bronze")]
        public void MoneyIsShownInCoins(int bronze, string shown)
        {
            Assert.AreEqual(shown, Coins.Show(bronze));
        }

        [Test]
        public void TheStartIsPlainClothesAndFiveHundredBronze()
        {
            var bag = SliceWorldDriver.StartingBag(_catalogue, new CharacterSheet());
            Assert.AreEqual(500, bag.Bronze);
            Assert.AreEqual(new[] { "plain_clothes" }, bag.Pieces.Select(p => p.item).ToArray());
            CollectionAssert.IsEmpty(bag.Stacks, "no weapon, tool or supply");
            Assert.AreEqual(ItemQuality.Normal, bag.Pieces[0].Quality);
        }

        [Test]
        public void ThePageReadsTheBag()
        {
            var bag = SliceWorldDriver.StartingBag(_catalogue, new CharacterSheet());
            bag.Add("herb", 3);
            Assert.AreEqual("Money: 5 silver", BagPage.Money(bag));
            Assert.AreEqual("Load: 1.8 of 21 kg, normal", BagPage.Load(bag));
            CollectionAssert.AreEqual(new[] { "Herbs × 3, 0.3 kg", "Plain clothes (Normal), 1.5 kg" }, BagPage.Lines(bag));
            Loc.Language = Loc.Russian;
            Assert.AreEqual("Деньги: 5 сер.", BagPage.Money(bag));
        }
    }
}
