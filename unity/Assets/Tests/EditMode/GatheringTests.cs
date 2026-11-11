using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Gathering;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.5: gathering by 19 §6.4 and Stamina by 07.2 §3.2: a cycle's outcomes, Stamina's cost and return, the end of a node.</summary>
    public sealed class GatheringTests
    {
        private GatherRules _rules;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceStamina _stamina;
        private SliceGathering _gathering;

        [SetUp]
        public void SetUp()
        {
            _rules = AssetDatabase.LoadAssetAtPath<GatherRules>("Assets/Data/Slice/Gathering.asset");
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            var checks = AssetDatabase.LoadAssetAtPath<ActionCheckRules>("Assets/Data/Progression/ActionCheckRules.asset");
            _sheet = new CharacterSheet();
            _sheet.Set(CharacterAttribute.Strength, 20);
            _bag = SliceWorldDriver.StartingBag(items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _stamina = new SliceStamina(_rules, _state, _clock, _sheet);
            _gathering = new SliceGathering(_rules, _bag, _state, _clock, _sheet, _stamina, checks, new WorldSeed(110201));
        }

        private GatherSpot Spot(GatherKind kind, int n = 0) => _gathering.Spot($"{kind}.{n}", _rules.RuleFor(kind));

        [Test]
        public void TheNodesAre19sTable()
        {
            var herb = _rules.RuleFor(GatherKind.Herb);
            Assert.AreEqual(("herb", 3, "", CharacterSkill.Herbalism, CharacterAttribute.Perception, 1, 24),
                (herb.resource, herb.units, herb.tool ?? "", herb.skill, herb.attribute, herb.difficulty, herb.returnsAfterHours));
            var flax = _rules.RuleFor(GatherKind.Flax);
            Assert.AreEqual(("flax", 3, CharacterAttribute.Dexterity, 1, 24), (flax.resource, flax.units, flax.attribute, flax.difficulty, flax.returnsAfterHours));
            var tree = _rules.RuleFor(GatherKind.Tree);
            Assert.AreEqual(("wood", 6, "woodcutting_axe", CharacterSkill.Woodcutting, CharacterAttribute.Strength, 2, 0),
                (tree.resource, tree.units, tree.tool, tree.skill, tree.attribute, tree.difficulty, tree.returnsAfterHours));
            var ore = _rules.RuleFor(GatherKind.Ore);
            Assert.AreEqual(("iron_ore", 8, "pickaxe", CharacterSkill.Mining, CharacterAttribute.Strength, 3, 0),
                (ore.resource, ore.units, ore.tool, ore.skill, ore.attribute, ore.difficulty, ore.returnsAfterHours));
            Assert.AreSame(tree, _rules.RuleFor("Kit_Resource_FellableBirch2_1x13 (4)"));
            Assert.IsNull(_rules.RuleFor("Kit_Tree_Birch2_1x13"), "a cover tree is no node");
            Assert.AreSame(ore, _rules.RuleFor("Iron ore vein 7"), "the name the city gave a vein");
        }

        [Test]
        public void StaminaIs07sAndComesBackWithoutExertion()
        {
            Assert.AreEqual(21.6d, _stamina.Max, 1e-6, "(20 + 1.6 × 1) × 1");
            _sheet.Set(CharacterAttribute.Endurance, 10);
            Assert.AreEqual(36d, _stamina.Max, 1e-6);
            Assert.AreEqual(1d, _stamina.Share);

            _stamina.Exerting = true;
            _stamina.Spend(0.03d);
            _stamina.Spend(0.03d);
            Assert.AreEqual(0.94d, _stamina.Share, 1e-9);
            _clock.AdvanceTicks(3);
            Assert.AreEqual(0.94d, _stamina.Share, 1e-9, "no return while exerting");
            _stamina.Exerting = false;
            _clock.AdvanceTicks(1);
            Assert.AreEqual(0.99d, _stamina.Share, 1e-9, "5 % a tick");
            _clock.AdvanceTicks(1);
            Assert.AreEqual(1d, _stamina.Share, 1e-9, "never above the maximum");
            _stamina.Spend(2d);
            Assert.IsTrue(_stamina.Empty);
            _clock.AdvanceTicks(20);
            Assert.AreEqual(1d, _stamina.Share, 1e-9, "an empty pool in 20 ticks");
        }

        [Test]
        public void ACyclesOutcomesFollowTheCheck()
        {
            Assert.AreEqual(0, SliceGathering.UnitsOf(CheckOutcome.Failure));
            Assert.AreEqual(1, SliceGathering.UnitsOf(CheckOutcome.Normal));
            Assert.AreEqual(2, SliceGathering.UnitsOf(CheckOutcome.Critical));

            _bag.Add("woodcutting_axe");
            const int Trees = 400;
            var counts = new int[3];
            var units = 0;
            for (var n = 0; n < Trees; n++)
            {
                var tree = Spot(GatherKind.Tree, n);
                Assert.IsNull(_gathering.Begin(tree));
                counts[(int)_gathering.Cycle(1000 + n)]++;
                units += _gathering.TakenNow;
                Assert.AreEqual(_gathering.TakenNow, _bag.Count("wood"), "what is taken goes into the bag");
                _bag.Remove("wood", _bag.Count("wood"));
                _state.Set(SliceStamina.SpentKey, 0);
            }

            var chances = _gathering.Chances(Spot(GatherKind.Tree));
            Assert.AreEqual(chances.Failure, counts[0] / (double)Trees, 0.07d, "failures");
            Assert.AreEqual(chances.CriticalOutcome, counts[2] / (double)Trees, 0.05d, "criticals");
            Assert.AreEqual(counts[1] + (2 * counts[2]), units, "a success one unit, a critical two");
        }

        [Test]
        public void ANodeEndsLeavesAndAPatchComesBackADayLater()
        {
            _sheet.Set(CharacterAttribute.Perception, 60);
            _sheet.Set(CharacterSkill.Herbalism, 60);
            var herb = Spot(GatherKind.Herb);
            bool? shown = null;
            herb.Shown = s => shown = s;

            Assert.IsNull(_gathering.Begin(herb));
            Assert.IsTrue(_stamina.Exerting);
            var cycles = 0;
            while (_gathering.Active != null && cycles < 20)
            {
                _clock.AdvanceTicks(1);
                cycles++;
            }

            Assert.AreEqual(0, _gathering.Left(herb));
            Assert.AreEqual(3, _bag.Count("herb"), "three units, and no more than the patch held");
            Assert.AreEqual(false, shown, "a spent patch leaves the world at once");
            Assert.AreEqual("ui.gather.stop.spent", _gathering.Stopped);
            Assert.IsFalse(_stamina.Exerting);
            Assert.AreEqual(1d - (0.03d * cycles), _stamina.Share, 1e-9, "3 % a cycle");
            Assert.AreEqual("ui.gather.fault.spent", _gathering.Fault(herb));

            _clock.AdvanceTicks((24 * 60) - 2);
            Assert.AreEqual(false, shown, "not before 24 hours");
            _clock.AdvanceTicks(2);
            Assert.AreEqual(true, shown, "it comes back where it was");
            Assert.AreEqual(3, _gathering.Left(herb));

            _bag.Add("woodcutting_axe");
            var tree = Spot(GatherKind.Tree);
            _state.Set(tree.TakenKey, 6);
            _state.Set(tree.EmptiedKey, (int)_clock.Tick + 1);
            _clock.AdvanceTicks(3 * 24 * 60);
            Assert.AreEqual(0, _gathering.Left(tree), "a felled tree does not come back within a run");
        }

        [Test]
        public void GatheringIsRefusedOrEndsForItsReasons()
        {
            var tree = Spot(GatherKind.Tree);
            Assert.AreEqual("ui.gather.fault.tool", _gathering.Begin(tree), "a tree needs an axe");
            Assert.IsNull(_gathering.Begin(Spot(GatherKind.Flax)), "flax needs only hands");
            _gathering.Stop("ui.gather.stop.moved");
            Assert.AreEqual("ui.gather.stop.moved", _gathering.Stopped);
            Assert.IsFalse(_stamina.Exerting);

            _bag.Add("woodcutting_axe");
            _stamina.Spend(0.97d);
            Assert.IsNull(_gathering.Begin(tree));
            _gathering.Cycle(_clock.Tick + 1);
            Assert.AreEqual("ui.gather.stop.tired", _gathering.Stopped, "Gathering stops at 0 Stamina");
            Assert.AreEqual("ui.gather.fault.tired", _gathering.Begin(tree));

            _state.Set(SliceStamina.SpentKey, 0);
            _bag.Add("pickaxe");
            Assert.IsTrue(_bag.Add("iron_ore", (int)((Bag.HardLimit * _bag.Comfortable) - _bag.Weight)), "the bag filled to within a kilo of its limit");
            var ore = Spot(GatherKind.Ore);
            _sheet.Set(CharacterSkill.Mining, 100);
            Assert.IsNull(_gathering.Begin(ore));
            for (var i = 0; i < 10 && _gathering.Active != null; i++)
            {
                _gathering.Cycle(_clock.Tick + 10 + i);
            }

            Assert.AreEqual("ui.gather.stop.heavy", _gathering.Stopped, "a bag that can carry no more ends it");
        }
    }
}
