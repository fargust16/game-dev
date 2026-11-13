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
            _sheet = new CharacterSheet();
            _sheet.Set(CharacterAttribute.Strength, 20);
            _bag = SliceWorldDriver.StartingBag(items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _stamina = new SliceStamina(_rules, _state, _clock, _sheet);
            _gathering = new SliceGathering(_rules, _bag, _state, _clock, _sheet, _stamina, new WorldSeed(110201));
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
            _stamina.Spend(0.3d);
            _stamina.Spend(0.3d);
            Assert.AreEqual(0.4d, _stamina.Share, 1e-9);
            _clock.AdvanceTicks(3);
            Assert.AreEqual(0.4d, _stamina.Share, 1e-9, "no return while exerting");
            _stamina.Exerting = false;
            _clock.AdvanceTicks(1);
            Assert.AreEqual(0.6d, _stamina.Share, 1e-9, "20 % a tick (the owner's change after the play test of S5a)");
            _clock.AdvanceTicks(3);
            Assert.AreEqual(1d, _stamina.Share, 1e-9, "never above the maximum");
            _stamina.Spend(2d);
            Assert.IsTrue(_stamina.Empty);
            Assert.IsTrue(_stamina.Tired, "run out, the character is tired");
            _clock.AdvanceTicks(5);
            Assert.AreEqual(0.95d, _stamina.Share, 1e-9, "5 ticks less the rest of 2.5 s, a quarter of a tick");
            Assert.IsFalse(_stamina.Tired);
            _clock.AdvanceTicks(1);
            Assert.AreEqual(1d, _stamina.Share, 1e-9, "an empty pool in about 5 ticks");
        }

        [Test]
        public void StaminaComesBackOverTheTicksRealTimeAndRestsAfterRunningOut()
        {
            // Its own state, so the fixture's Stamina does not reckon the same pool; a tick of 1 s, as this clock's,
            // so the rest of 2.5 s is 2.5 ticks.
            var stamina = new SliceStamina(_rules, new WorldState(), _clock, _sheet, 1d);
            stamina.Spend(0.5d);
            _clock.AdvanceRealTime(0.5d);
            stamina.CatchUp();
            Assert.AreEqual(0.6d, stamina.Share, 1e-9, "half a tick gives back half of 20 %");
            using (_clock.Pause())
            {
                _clock.AdvanceRealTime(5d);
                stamina.CatchUp();
                Assert.AreEqual(0.6d, stamina.Share, 1e-9, "none while the world is paused");
            }

            stamina.Sprinting = true;
            stamina.Spend(SliceStamina.SprintShare);
            _clock.AdvanceRealTime(0.25d);
            stamina.CatchUp();
            Assert.AreEqual(0.58d, stamina.Share, 1e-9, "a second of sprint spends 2 %, and none comes back meanwhile");
            stamina.Sprinting = false;

            stamina.Spend(1d);
            Assert.IsTrue(stamina.Empty);
            Assert.IsTrue(stamina.Tired);
            _clock.AdvanceRealTime(2d);
            stamina.CatchUp();
            Assert.AreEqual(0d, stamina.Share, 1e-9, "2.5 s of rest after running out");
            _clock.AdvanceRealTime(0.75d);
            stamina.CatchUp();
            Assert.AreEqual(0.05d, stamina.Share, 1e-9);
            Assert.IsTrue(stamina.Tired, "tired until a tenth has come back");
            _clock.AdvanceRealTime(0.25d);
            stamina.CatchUp();
            Assert.AreEqual(0.1d, stamina.Share, 1e-9);
            Assert.IsFalse(stamina.Tired);

            stamina.Exerting = true;
            stamina.Spend(0.05d);
            stamina.Exerting = false;
            _clock.AdvanceRealTime(0.2d);
            stamina.CatchUp();
            Assert.AreEqual(0.09d, stamina.Share, 1e-9, "work stopped before the pool ran out lets it come back at once");
        }

        [Test]
        public void ACyclesResultFollowsItsBarAndWearsTheTool()
        {
            Assert.AreEqual(0, SliceGathering.UnitsOf(CheckOutcome.Failure));
            Assert.AreEqual(1, SliceGathering.UnitsOf(CheckOutcome.Normal));
            Assert.AreEqual(2, SliceGathering.UnitsOf(CheckOutcome.Critical));

            _bag.Add("woodcutting_axe");
            var tree = Spot(GatherKind.Tree);
            Assert.IsNull(_gathering.Begin(tree));
            _clock.AdvanceTicks(3);
            Assert.AreEqual(0, _bag.Count("wood"), "ticks take nothing: only a swing's check does (AN3)");

            // The bar by 07.1 §8a: Woodcutting 1 and Strength 20 against D 2 is Δ 8.5, the widest zone and the slowest marker.
            var bar = _gathering.NextBar();
            Assert.AreEqual(ReactionCheck.MostWidth, bar.GoodWidth, 1e-9);
            Assert.AreEqual(ReactionCheck.Seconds, bar.Seconds, 1e-9);
            Assert.AreEqual(bar.GoodWidth / 4d, bar.PerfectWidth, 1e-9, "a perfect zone for two units");
            _sheet.Set(CharacterAttribute.Strength, 1);
            Assert.AreEqual(ReactionCheck.Width(-1d), _gathering.NextBar().GoodWidth, 1e-9, "a weak arm: Δ −1");
            Assert.Less(_gathering.NextBar().Seconds, ReactionCheck.Seconds, "and a faster marker");

            Assert.AreEqual(CheckOutcome.Critical, _gathering.Cycle(ReactionResult.Perfect));
            Assert.AreEqual(2, _bag.Count("wood"), "a perfect stop: two units");
            Assert.AreEqual(2, _sheet.Get(CharacterSkill.Woodcutting), "and every swing grows the skill, a perfect one twice: 20 XP (S5b.2)");
            Assert.AreEqual(0.2d, _sheet.Xp(CharacterAttribute.Strength), 1e-9, "and Strength a hundredth of what its own level takes");
            Assert.AreEqual(SliceGathering.WearGood, _gathering.Wear("woodcutting_axe"));
            Assert.AreEqual(CheckOutcome.Normal, _gathering.Cycle(_clock.Tick, ReactionResult.Good));
            Assert.AreEqual(3, _bag.Count("wood"), "a good one: one unit");
            Assert.AreEqual(CheckOutcome.Failure, _gathering.Cycle(_clock.Tick, ReactionResult.Bad));
            Assert.AreEqual(3, _bag.Count("wood"), "a bad one: nothing");
            Assert.AreEqual((2 * SliceGathering.WearGood) + SliceGathering.WearBad, _gathering.Wear("woodcutting_axe"), "and the tool wears three times as much");
            Assert.AreEqual(3, _gathering.TakenNow);

            // A worn-out tool stays in the bag broken, and the work ends without another.
            _state.Set(SliceGathering.WearKey("woodcutting_axe"), SliceGathering.ToolDurability - 1);
            _gathering.Cycle(_clock.Tick, ReactionResult.Good);
            Assert.AreEqual(0, _bag.Count("woodcutting_axe"));
            Assert.AreEqual(1, _bag.Count("woodcutting_axe" + SliceGathering.Broken), "the broken axe kept");
            Assert.AreEqual(0, _gathering.Wear("woodcutting_axe"), "the next axe begins unworn");
            Assert.AreEqual("ui.gather.stop.broken", _gathering.Stopped);
            Assert.AreEqual("ui.gather.fault.tool", _gathering.Begin(tree), "a broken axe fells nothing");

            var herb = Spot(GatherKind.Herb);
            Assert.IsNull(_gathering.Begin(herb));
            _gathering.Harvest(ReactionResult.Bad);
            Assert.AreEqual(0, _gathering.Wear(""), "hands do not wear");
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
            Assert.AreEqual(CheckOutcome.Normal, _gathering.Harvest(ReactionResult.Good));

            Assert.AreEqual(0, _gathering.Left(herb));
            Assert.AreEqual(3, _bag.Count("herb"), "a good check picks what the patch held, in one go");
            Assert.AreEqual(false, shown, "a spent patch leaves the world at once");
            Assert.AreEqual("ui.gather.stop.picked", _gathering.Stopped);
            Assert.IsFalse(_stamina.Exerting);
            Assert.AreEqual(1d - 0.03d, _stamina.Share, 1e-9, "3 % a check");
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
        public void APatchIsPickedInOneCheck()
        {
            // The owner's second direction of 2026-11-11: one check empties the patch; a perfect one gives one more, a bad one nothing.
            var flax = Spot(GatherKind.Flax);
            Assert.IsNull(_gathering.Begin(flax));
            Assert.AreEqual(CheckOutcome.Critical, _gathering.Harvest(ReactionResult.Perfect));
            Assert.AreEqual(4, _bag.Count("flax"), "what the patch held and one more");
            Assert.AreEqual(0, _gathering.Left(flax));

            var herb = Spot(GatherKind.Herb, 1);
            Assert.IsNull(_gathering.Begin(herb));
            Assert.AreEqual(CheckOutcome.Failure, _gathering.Harvest(ReactionResult.Bad));
            Assert.AreEqual(0, _bag.Count("herb"), "a bad check picks nothing");
            Assert.AreEqual(0, _gathering.Left(herb), "and the patch is spent all the same");
            Assert.AreEqual("ui.gather.stop.lost", _gathering.Stopped);
            Assert.AreEqual("ui.gather.fault.spent", _gathering.Fault(herb));
        }

        [Test]
        public void GatheringIsRefusedOrEndsForItsReasons()
        {
            var tree = Spot(GatherKind.Tree);
            Assert.AreEqual("ui.gather.fault.tool", _gathering.Begin(tree), "a tree needs an axe");
            Assert.IsNull(_gathering.Begin(Spot(GatherKind.Flax)), "flax needs only hands");
            _gathering.Stop("ui.gather.stop.bag");
            Assert.AreEqual("ui.gather.stop.bag", _gathering.Stopped);
            Assert.IsFalse(_stamina.Exerting);

            _bag.Add("woodcutting_axe");
            _stamina.Spend(0.97d);
            Assert.IsNull(_gathering.Begin(tree));
            _gathering.Cycle(ReactionResult.Good);
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
                _gathering.Cycle(ReactionResult.Perfect);
            }

            Assert.AreEqual("ui.gather.stop.heavy", _gathering.Stopped, "a bag that can carry no more ends it");
        }
    }
}
