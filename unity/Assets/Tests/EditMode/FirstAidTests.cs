using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.7: First Aid with the bag's bandages, antidotes and splints (10.2): each one's condition and cost, the lesson, a failed check.</summary>
    public sealed class FirstAidTests
    {
        private FightRules _rules;
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SliceFirstAid _aid;
        private Fighter _life;

        [SetUp]
        public void SetUp()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            _rules = settings.FightRules;
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(settings.Items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _aid = new SliceFirstAid(_rules, _bag, _state, _clock, _sheet, new WorldSeed(110207));
            _life = _rules.Fighter(_sheet, null, new WeaponSet(settings.Fists));
        }

        /// <summary>First Aid outside a fight as the character kneels: its fault, its ticks, then its check's <paramref name="result"/>.</summary>
        private string Treat(ReactionResult result, out Treatment treatment, out bool success)
        {
            success = false;
            var fault = _aid.Fault(_life, out treatment);
            if (fault != null)
            {
                return fault;
            }

            _aid.Work(treatment);
            success = _aid.Finish(() => _life, f => _life = f, treatment, result);
            return null;
        }

        private void Learn() => _state.Set(SliceFirstAid.LessonKey, 1);

        [Test]
        public void EachSupplyEndsItsConditionForItsTicks()
        {
            Learn();
            _sheet.Set(CharacterAttribute.Dexterity, 100);
            _sheet.Set(CharacterSkill.FirstAid, 100);
            var cases = new[] { (Treatment.Bandage, "bandage", 1), (Treatment.Antidote, "antidote", 1), (Treatment.Splint, "splint", 3) };
            foreach (var (treatment, item, ticks) in cases)
            {
                Assert.IsTrue(_bag.Add(item));
                _life.Bleeding = treatment == Treatment.Bandage;
                _life.Poisoned = treatment == Treatment.Antidote;
                _life.LegBroken = treatment == Treatment.Splint;
                var from = _clock.Tick;
                Assert.IsNull(Treat(ReactionResult.Good, out var given, out var success), item);
                Assert.AreEqual(treatment, given);
                Assert.AreEqual((long)ticks, _clock.Tick - from, $"a {item} takes {ticks} ticks");
                Assert.AreEqual(!success, FightRules.Needs(_life, treatment), $"a {item} that works ends its condition");
                Assert.AreEqual(success ? 0 : 1, _bag.Count(item), "and is used; a failure keeps it");
            }

            Assert.AreEqual(new[] { 3, 3, 6 }, new[] { _rules.TreatmentAp(Treatment.Bandage), _rules.TreatmentAp(Treatment.Antidote), _rules.TreatmentAp(Treatment.Splint) },
                "in a fight its AP instead");
        }

        [Test]
        public void AnAntidoteAndASplintNeedTheLessonAndEveryTreatmentItsSupply()
        {
            _life.Poisoned = true;
            _life.LegBroken = true;
            _bag.Add("antidote");
            _bag.Add("splint");
            var start = _clock.Tick;
            Assert.AreEqual("ui.life.nosupply", Treat(ReactionResult.Good, out _, out _), "neither without the lesson");
            Assert.AreEqual(start, _clock.Tick, "and no time spent");

            _life.Bleeding = true;
            Assert.AreEqual("ui.life.nosupply", Treat(ReactionResult.Good, out _, out _), "bleeding with no bandage");
            _bag.Add("bandage");
            Assert.IsNull(Treat(ReactionResult.Good, out var given, out _));
            Assert.AreEqual(Treatment.Bandage, given, "a bandage needs no lesson");

            Learn();
            Assert.AreEqual(Treatment.Splint, _aid.Choose(_life, _aid.Count, _aid.Knows), "the leg first once it is learned");
        }

        [Test]
        public void ABadCheckKeepsTheItemAndSpendsTheTicks()
        {
            Learn();
            _bag.Add("splint");
            _life.LegBroken = true;
            var from = _clock.Tick;
            Assert.IsNull(Treat(ReactionResult.Bad, out _, out var success));
            Assert.IsFalse(success);
            Assert.AreEqual(3L, _clock.Tick - from, "the ticks are spent all the same");
            Assert.IsTrue(_life.LegBroken, "a bad stop leaves the leg");
            Assert.AreEqual(1, _bag.Count("splint"), "and keeps the splint");

            // 07.1 §8a: Δ = (Dexterity + First Aid) / 2 − D, a splint's D 3; the bar has no perfect zone.
            _sheet.Set(CharacterAttribute.Dexterity, 3);
            _sheet.Set(CharacterSkill.FirstAid, 1);
            Assert.AreEqual(2d - _rules.TreatmentDifficulty(Treatment.Splint), _aid.Delta(Treatment.Splint), 1e-9);
            var bar = _aid.Bar(Treatment.Splint);
            Assert.AreEqual(ReactionCheck.Width(_aid.Delta(Treatment.Splint)), bar.GoodWidth, 1e-9);
            Assert.AreEqual(0d, bar.PerfectWidth, 1e-9);
        }

        [Test]
        public void AFightTakesTheBagsSuppliesAndTheBagLosesWhatItUsed()
        {
            Learn();
            _bag.Add("bandage", 2);
            _bag.Add("antidote");
            var player = new Combatant(0, 0, _life, Vector3.zero);
            _aid.Load(player);
            Assert.IsTrue(player.KnowsFirstAid);
            Assert.AreEqual(new[] { 2, 1, 0 }, player.Supplies);

            player.Supplies[(int)Treatment.Bandage]--;
            _aid.Spend(player);
            Assert.AreEqual((1, 1, 0), (_bag.Count("bandage"), _bag.Count("antidote"), _bag.Count("splint")));

            _aid.TopUp(Treatment.Splint, 1);
            _aid.TopUp(Treatment.Bandage, 1);
            Assert.AreEqual((1, 1), (_bag.Count("splint"), _bag.Count("bandage")), "the development fight's kit tops up, never adds past it");
        }
    }
}
