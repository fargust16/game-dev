using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Beasts;
using GameDev.Slice.Shops;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.6: what a fallen beast leaves by 19 §6.4 and 11 C3, its ingredients to anyone and its hide by Skinning after the lesson.</summary>
    public sealed class RemainsTests
    {
        private BeastRules _rules;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceRemains _remains;
        private long _start;

        [SetUp]
        public void SetUp()
        {
            _rules = AssetDatabase.LoadAssetAtPath<BeastRules>("Assets/Data/Slice/BeastRules.asset");
            var items = AssetDatabase.LoadAssetAtPath<ItemCatalogue>("Assets/Data/Items/Items.asset");
            var checks = AssetDatabase.LoadAssetAtPath<ActionCheckRules>("Assets/Data/Progression/ActionCheckRules.asset");
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _remains = new SliceRemains(_rules, _bag, _state, _clock, _sheet, checks, new WorldSeed(110206));
            _start = _clock.Tick;
        }

        private void Learn() => _state.Set(SliceShops.LessonKey(SliceRemains.Lesson), 1);

        [Test]
        public void EachBeastLeaves19sParts()
        {
            CollectionAssert.AreEqual(new[] { ("wolf_fang", 1) }, _remains.PartsOf(BeastKind.Wolf, false).ToArray());
            CollectionAssert.AreEqual(new[] { ("boar_tusk", 1) }, _remains.PartsOf(BeastKind.Boar, false).ToArray());
            CollectionAssert.AreEqual(new[] { ("wolf_fang", 1), ("glow_essence_8", 1) }, _remains.PartsOf(BeastKind.Wolf, true).ToArray(),
                "a glowing beast's magical ingredient, the stage 8 rare one");
            CollectionAssert.AreEqual(new[] { ("boar_tusk", 1), ("glow_essence_8", 1) }, _remains.PartsOf(BeastKind.Boar, true).ToArray());

            Assert.AreEqual(1, _remains.LeatherOf(BeastKind.Wolf, false, CheckOutcome.Normal), "a wolf 1 leather");
            Assert.AreEqual(2, _remains.LeatherOf(BeastKind.Boar, false, CheckOutcome.Normal), "a boar 2");
            Assert.AreEqual(2, _remains.LeatherOf(BeastKind.Boar, true, CheckOutcome.Critical), "a critical gives no more than the hide");
            Assert.AreEqual(0, _remains.LeatherOf(BeastKind.Wolf, false, CheckOutcome.Failure), "a failure half, rounded down");
            Assert.AreEqual(1, _remains.LeatherOf(BeastKind.Boar, false, CheckOutcome.Failure));
            Assert.AreEqual((3, 2), (_rules.SkinningTicks, _rules.SkinningDifficulty));
        }

        [Test]
        public void TheIngredientsNeedNoLessonAndAreTakenOnce()
        {
            Assert.IsFalse(_remains.KnowsSkinning);
            Assert.IsNull(_remains.TakeParts("wolf.1.a", BeastKind.Wolf, true));
            Assert.AreEqual((1, 1), (_bag.Count("wolf_fang"), _bag.Count("glow_essence_8")));
            Assert.IsTrue(_remains.PartsTaken("wolf.1.a"), "the run remembers it");
            Assert.AreEqual("ui.remains.fault.taken", _remains.TakeParts("wolf.1.a", BeastKind.Wolf, true));
            Assert.AreEqual(1, _bag.Count("wolf_fang"), "nothing more");
            Assert.AreEqual(_start, _clock.Tick, "taking them takes no time");
        }

        [Test]
        public void TheHideNeedsTheLesson()
        {
            Assert.AreEqual(SkinOutcome.Refused, _remains.Skin("boar.7.a", BeastKind.Boar, false, null, out var leather, out var fault));
            Assert.AreEqual("ui.remains.fault.lesson", fault);
            Assert.AreEqual((0, _start), (_bag.Count("leather"), _clock.Tick), "nothing taken, no time spent");

            Learn();
            _sheet.Set(CharacterAttribute.Dexterity, 60);
            _sheet.Set(CharacterSkill.Skinning, 60);
            Assert.AreEqual(SkinOutcome.Done, _remains.Skin("boar.7.a", BeastKind.Boar, false, null, out leather, out fault));
            Assert.IsNull(fault);
            Assert.AreEqual(3L, _clock.Tick - _start, "3 ticks with the world running");
            Assert.That(leather, Is.EqualTo(2).Or.EqualTo(1));
            Assert.AreEqual(leather, _bag.Count("leather"));
            Assert.IsTrue(_remains.Skinned("boar.7.a"));
            Assert.AreEqual(SkinOutcome.Refused, _remains.Skin("boar.7.a", BeastKind.Boar, false, null, out _, out fault));
            Assert.AreEqual("ui.remains.fault.skinned", fault, "a hide comes off once");
        }

        [Test]
        public void ASkinningsOutcomesFollowTheCheck()
        {
            Learn();
            const int Boars = 400;
            var spoiled = 0;
            for (var n = 0; n < Boars; n++)
            {
                Assert.AreEqual(SkinOutcome.Done, _remains.Skin($"boar.{n}", BeastKind.Boar, false, null, out var leather, out _));
                spoiled += leather == 1 ? 1 : 0;
                _bag.Remove("leather", _bag.Count("leather"));
            }

            Assert.AreEqual(_remains.Chances().Failure, spoiled / (double)Boars, 0.07d, "half the hide on a failed check");
        }

        [Test]
        public void AFightInTheTicksLeavesTheHideOn()
        {
            Learn();
            var asked = 0;
            Assert.AreEqual(SkinOutcome.Interrupted, _remains.Skin("wolf.2.b", BeastKind.Wolf, false, () => ++asked == 2, out var leather, out _));
            Assert.AreEqual((0, 0, 2L), (leather, _bag.Count("leather"), _clock.Tick - _start));
            Assert.IsFalse(_remains.Skinned("wolf.2.b"), "the hide is still on");
            Assert.AreEqual(SkinOutcome.Done, _remains.Skin("wolf.2.b", BeastKind.Wolf, false, () => false, out _, out _));
        }

        [Test]
        public void AFullBagTakesNothing()
        {
            Learn();
            _bag.Add("iron_ore", (int)((Bag.HardLimit * _bag.Comfortable) - _bag.Weight));
            while (_bag.Add("glow_essence_9"))
            {
                // to within 0.1 kg of the limit
            }

            var before = _bag.Weight;
            Assert.AreEqual("ui.remains.fault.heavy", _remains.TakeParts("boar.3.a", BeastKind.Boar, true));
            Assert.AreEqual("ui.remains.fault.heavy", _remains.SkinFault("boar.3.a", BeastKind.Boar, false));
            Assert.AreEqual(before, _bag.Weight, "nothing taken");
            Assert.IsFalse(_remains.PartsTaken("boar.3.a"));
        }
    }
}
