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

        private string Treat(out Treatment treatment, out bool success) => _aid.TreatOutside(() => _life, f => _life = f, out treatment, out success);

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
                Assert.IsNull(Treat(out var given, out var success), item);
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
            Assert.AreEqual("ui.life.nosupply", Treat(out _, out _), "neither without the lesson");
            Assert.AreEqual(start, _clock.Tick, "and no time spent");

            _life.Bleeding = true;
            Assert.AreEqual("ui.life.nosupply", Treat(out _, out _), "bleeding with no bandage");
            _bag.Add("bandage");
            Assert.IsNull(Treat(out var given, out _));
            Assert.AreEqual(Treatment.Bandage, given, "a bandage needs no lesson");

            Learn();
            Assert.AreEqual(Treatment.Splint, _aid.Choose(_life, _aid.Count, _aid.Knows), "the leg first once it is learned");
        }

        [Test]
        public void AFailedCheckKeepsTheItemAndSpendsTheTicks()
        {
            Learn();
            const int Tries = 200;
            var failed = 0;
            _bag.Add("splint");
            for (var n = 0; n < Tries; n++)
            {
                _life.LegBroken = true;
                var from = _clock.Tick;
                Assert.IsNull(Treat(out _, out var success));
                Assert.AreEqual(3L, _clock.Tick - from);
                if (!success)
                {
                    failed++;
                    Assert.IsTrue(_life.LegBroken, "a failure leaves the leg");
                    Assert.AreEqual(1, _bag.Count("splint"), "and keeps the splint");
                }
                else
                {
                    Assert.AreEqual(0, _bag.Count("splint"));
                    _bag.Add("splint");
                }
            }

            Assert.AreEqual(1d - _rules.TreatmentChance(_sheet, Treatment.Splint), failed / (double)Tries, 0.08d, "at Dexterity and First Aid 1, a splint fails about half the time");
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
