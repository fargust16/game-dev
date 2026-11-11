using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Shops;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.4: Item Creation at Edda Brann's workshop by 19 §3: the recipes, a piece made at the character's skill, and an interruption.</summary>
    public sealed class WorkshopTests
    {
        private ItemCatalogue _items;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceShops _shops;
        private SliceWorkshop _workshop;

        [SetUp]
        public void SetUp()
        {
            _items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            var rules = AssetDatabase.LoadAssetAtPath<ShopCatalogue>("Assets/Data/Slice/Shops.asset");
            _sheet = new CharacterSheet();
            _sheet.Set(CharacterAttribute.Strength, 20);
            _bag = SliceWorldDriver.StartingBag(_items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _shops = new SliceShops(rules, _items, new WorldSeed(110201), _clock, _bag, _state);
            _workshop = new SliceWorkshop(_shops, _items, new WorldSeed(110201), _clock, _bag, _state, _sheet);
        }

        [Test]
        public void EveryPricedBaseHas19sRecipe()
        {
            var bases = _items.Items.Where(i => i.kind == ItemKind.Equipment && i.price > 0).ToList();
            CollectionAssert.AreEquivalent(bases, _workshop.Bases.ToList(), "every base a shop sells is made at the workshop");
            CollectionAssert.AreEqual(new[] { ("iron_ore", 3), ("wood", 1) }, _items.Find("sword").recipe.Select(p => (p.item, p.count)));
            CollectionAssert.AreEqual(new[] { ("iron_ore", 9) }, _items.Find("plate_body").recipe.Select(p => (p.item, p.count)));
            CollectionAssert.AreEqual(new[] { ("flax", 4) }, _items.Find("cloth_body").recipe.Select(p => (p.item, p.count)));
            CollectionAssert.AreEqual(new[] { ("leather", 2) }, _items.Find("cartridge_pouch").recipe.Select(p => (p.item, p.count)));
            Assert.IsEmpty(_items.Find("plain_clothes").recipe);

            CollectionAssert.AreEqual(new[] { ("iron_ore", 5), ("wood", 2), ("glow_essence_8", 1) },
                _workshop.Recipe(_items.Find("sword"), 8).Select(p => (p.item, p.count)), "stage 8: half again, rounded up, and the rare ingredient");
            Assert.AreEqual(10, _workshop.Ticks(9));
            Assert.AreEqual(20, _workshop.Ticks(8));
            Assert.IsTrue(_workshop.Has("edda"));
            Assert.IsFalse(_workshop.Has("ada"));
        }

        [Test]
        public void APieceIsRefusedForEachReason()
        {
            var sword = _items.Find("sword");
            Assert.AreEqual("ui.shop.fault.gone", _workshop.Fault("ada", sword, 9), "no workshop at Ada's");
            Assert.AreEqual("ui.craft.fault.lesson", _workshop.Fault("edda", sword, 9));
            _state.Set(SliceShops.LessonKey(SliceWorkshop.Lesson), 1);
            Assert.AreEqual("ui.craft.fault.resources", _workshop.Fault("edda", sword, 9));
            _bag.Add("iron_ore", 3);
            _bag.Add("wood");
            Assert.IsNull(_workshop.Fault("edda", sword, 9));
            Assert.AreEqual("ui.craft.fault.resources", _workshop.Fault("edda", sword, 8), "stage 8 asks more and the rare ingredient");
            Assert.AreEqual("ui.shop.fault.gone", _workshop.Fault("edda", _items.Find("plain_clothes"), 9));
            _bag.Spend(_bag.Bronze);
            Assert.AreEqual("ui.shop.fault.money", _workshop.Fault("edda", sword, 9));
            _bag.Earn(100);
            _clock.AdvanceTicks(12 * 60);
            Assert.AreEqual("ui.shop.fault.closed", _workshop.Fault("edda", sword, 9), "21:00");
        }

        [Test]
        public void APieceIsMadeAtTheCharactersSkill()
        {
            _state.Set(SliceShops.LessonKey(SliceWorkshop.Lesson), 1);
            _bag.Add("iron_ore", 10);
            _bag.Add("wood", 4);
            var start = _clock.Tick;

            Assert.AreEqual(CraftOutcome.Made, _workshop.Make("edda", _items.Find("dagger"), 9, () => false, out var low));
            Assert.AreEqual(start + 10, _clock.Tick, "ten ticks, the world running");
            Assert.AreEqual(400, _bag.Bronze, "the workshop's 100 bronze");
            Assert.AreEqual(8, _bag.Count("iron_ore"));
            Assert.AreEqual(3, _bag.Count("wood"));
            CollectionAssert.Contains(_bag.Pieces, low);
            Assert.AreEqual("dagger", low.item);
            Assert.AreEqual(9, low.stage);
            Assert.LessOrEqual(low.modifiers.Count, 3, "at Item Creation 1 a piece averages under one modifier");

            foreach (var a in new[] { CharacterAttribute.Perception, CharacterAttribute.Luck })
            {
                _sheet.Set(a, 100);
            }

            _sheet.Set(CharacterSkill.ItemCreation, 100);
            Assert.AreEqual(CraftOutcome.Made, _workshop.Make("edda", _items.Find("dagger"), 9, () => false, out var high));
            Assert.GreaterOrEqual(high.modifiers.Count, 5, "at 100 / 100 / 100 each addition is 99 %");
            Assert.AreEqual(2, _state.Get(SliceWorkshop.MadeKey));
            Assert.AreNotEqual(low.modifiers.Count, high.modifiers.Count);
        }

        [Test]
        public void AStageEightPieceTakesItsRareIngredient()
        {
            _state.Set(SliceShops.LessonKey(SliceWorkshop.Lesson), 1);
            _bag.Add("iron_ore", 5);
            _bag.Add("wood", 2);
            _bag.Add("glow_essence_8");
            var start = _clock.Tick;
            Assert.AreEqual(CraftOutcome.Made, _workshop.Make("edda", _items.Find("sword"), 8, () => false, out var piece));
            Assert.AreEqual(start + 20, _clock.Tick);
            Assert.AreEqual(8, piece.stage);
            Assert.AreEqual(0, _bag.Count("glow_essence_8"));
            Assert.AreEqual(0, _bag.Count("iron_ore"));
            Assert.IsTrue(piece.modifiers.All(m => m.tier <= 8 && m.tier >= 6), "a stage 8 piece rolls tiers 8 to 6");
        }

        [Test]
        public void AFightInTheWorkInterruptsItAndNothingIsSpent()
        {
            _state.Set(SliceShops.LessonKey(SliceWorkshop.Lesson), 1);
            _bag.Add("iron_ore", 3);
            _bag.Add("wood");
            var start = _clock.Tick;
            var fight = false;
            _clock.TickBoundary += tick => fight |= tick >= start + 4;

            Assert.AreEqual(CraftOutcome.Interrupted, _workshop.Make("edda", _items.Find("sword"), 9, () => fight, out var piece));
            Assert.IsNull(piece);
            Assert.AreEqual(start + 4, _clock.Tick, "the work stops at the tick the fight begins");
            Assert.AreEqual(500, _bag.Bronze, "nothing paid");
            Assert.AreEqual(3, _bag.Count("iron_ore"), "the resources kept");
            Assert.AreEqual(1, _bag.Count("wood"));
            Assert.AreEqual(1, _bag.Pieces.Count, "only the plain clothes");
            Assert.AreEqual(0, _state.Get(SliceWorkshop.MadeKey));
        }
    }
}
