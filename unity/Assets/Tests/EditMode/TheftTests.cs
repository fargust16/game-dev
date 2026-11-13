using System;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Theft;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.8: the Reaction Check's bar and theft from a person by 10.3 §6: the zone's width, the bar's results, a purse lifted or a theft noticed.</summary>
    public sealed class TheftTests
    {
        private CharacterSheet _sheet;
        private Bag _bag;
        private WorldState _state;
        private GameClock _clock;
        private SocialStanding _standing;
        private StandingRules _rules;
        private SliceTheft _theft;

        [SetUp]
        public void SetUp()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            _rules = settings.Standing;
            _sheet = new CharacterSheet();
            _bag = SliceWorldDriver.StartingBag(settings.Items, _sheet);
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            var seed = new WorldSeed(110208);
            _standing = new SocialStanding(_state, _rules, _clock, seed, SliceSeedStreams.Lies);
            _theft = new SliceTheft(_bag, _state, _clock, _sheet, _standing, seed);
        }

        [Test]
        public void TheZonesWidthIs103s()
        {
            Assert.AreEqual(0.20d, ReactionCheck.TheftWidth(3, 1, 2, false), 1e-9, "Dexterity 3 against a townsman: 20 %");
            Assert.AreEqual(0.128d, ReactionCheck.TheftWidth(3, 1, 4, false), 1e-9, "against a guard of Perception 4: 13 %");
            Assert.AreEqual(0.35d, ReactionCheck.TheftWidth(5, 5, 2, false), 1e-9, "a practised hand, Dexterity and Theft 5: 39 %, held at 35 %");
            Assert.AreEqual(0.20d * Math.Pow(1.25d, -0.5d), ReactionCheck.TheftWidth(3, 1, 2, true), 1e-9, "an Alert townsman's Perception × 1.25");
            Assert.AreEqual(0.05d, ReactionCheck.TheftWidth(1, 1, 20, false), 1e-9, "never under 5 %");
            Assert.AreEqual(0.35d, ReactionCheck.TheftWidth(60, 60, 2, false), 1e-9, "never over 35 % (07.1 §8a, the owner's play test of S5a)");

            _sheet.Set(CharacterAttribute.Dexterity, 4);
            _sheet.Set(CharacterSkill.Theft, 4);
            Assert.AreEqual(0.3125d, _theft.Width(2, false), 1e-9, "the character's own values");
        }

        [Test]
        public void TheMarkersPaceFollows071s8a()
        {
            Assert.AreEqual(1.5d, ReactionCheck.Pace(0d), 1e-9, "even odds: 1.5 s, the slowest");
            Assert.AreEqual(1.5d, ReactionCheck.Pace(6d), 1e-9, "never slower, however strong");
            Assert.AreEqual(1.5d / 1.15d, ReactionCheck.Pace(-1d), 1e-9, "each point below speeds it");
            Assert.AreEqual(1.5d * Math.Pow(1.15d, -4d), ReactionCheck.Pace(-4d), 1e-9, "0.86 s at Δ −4");
            Assert.AreEqual(0.6d, ReactionCheck.Pace(-12d), 1e-9, "never faster than 0.6 s");

            // The time to stop the marker in the zone: 0.3 s at even odds, 0.53 s at best, 0.07 s at Δ −4.
            Assert.AreEqual(0.3d, ReactionCheck.Width(0d) * ReactionCheck.Pace(0d), 1e-9);
            Assert.AreEqual(0.525d, ReactionCheck.Width(9d) * ReactionCheck.Pace(9d), 1e-9);
            Assert.AreEqual(0.0703d, ReactionCheck.Width(-4d) * ReactionCheck.Pace(-4d), 1e-4);

            var rolls = new Random(9);
            for (var i = 0; i < 100; i++)
            {
                var bar = ReactionCheck.For(-2d, rolls, perfect: true);
                Assert.AreEqual(ReactionCheck.Width(-2d), bar.GoodWidth, 1e-9);
                Assert.AreEqual(ReactionCheck.Pace(-2d), bar.Seconds, 1e-9, "the bar keeps the pace");
                Assert.AreEqual(bar.GoodWidth / 4d, bar.PerfectWidth, 1e-9, "a perfect zone a quarter of the good one");
                Assert.AreEqual(bar.GoodStart + (bar.GoodWidth / 2d), bar.PerfectStart + (bar.PerfectWidth / 2d), 1e-9, "in its middle");
                Assert.That(bar.GoodStart, Is.GreaterThanOrEqualTo(ReactionCheck.Lead));
            }

            Assert.AreEqual(0d, ReactionCheck.For(0d, rolls).PerfectWidth, "no perfect zone unless the use has one");
        }

        [Test]
        public void TheBarsMarkerStopsInOrOutOfItsZones()
        {
            var bar = new ReactionBar(0.4d, 0.2d);
            Assert.AreEqual(1.5d, bar.Seconds, "the marker crosses in 1.5 seconds");
            Assert.AreEqual(0.5d, bar.MarkerAt(0.75d), 1e-9);
            Assert.AreEqual(ReactionResult.Good, bar.At(0.5d));
            Assert.AreEqual(ReactionResult.Bad, bar.At(0.39d));
            Assert.AreEqual(ReactionResult.Bad, bar.At(0.61d));
            Assert.AreEqual(ReactionResult.Bad, bar.At(1.01d), "the marker gone past the bar");

            var brewing = new ReactionBar(0.3d, 0.4d, 0.45d, 0.1d);
            Assert.AreEqual(ReactionResult.Perfect, brewing.At(0.5d), "a perfect zone inside the good one");
            Assert.AreEqual(ReactionResult.Good, brewing.At(0.35d));

            var rolls = new Random(5);
            for (var i = 0; i < 200; i++)
            {
                var placed = ReactionCheck.Place(0.3d, rolls);
                Assert.That(placed.GoodStart, Is.InRange(ReactionCheck.Lead, 1d - 0.3d), "the zone lies whole on the bar, after its lead");
                Assert.AreEqual(0.3d, placed.GoodWidth);
                Assert.AreEqual(0d, placed.PerfectWidth, "theft has a good zone only");
            }
        }

        [Test]
        public void AGoodResultLiftsThePurseUnseen()
        {
            var bronze = _bag.Bronze;
            var purse = _theft.Purse("Townsman 3");
            Assert.That(purse, Is.InRange(SliceTheft.PurseLeast, SliceTheft.PurseMost), "a townsman's purse by 19 §6.3");
            Assert.AreEqual(purse, _theft.Purse("Townsman 3"), "the same purse all day");

            Assert.IsFalse(_theft.TriedToday("Townsman 3"));
            Assert.AreEqual(TheftOutcome.Lifted, _theft.Resolve("Townsman 3", ReactionResult.Good, out var taken));
            Assert.AreEqual(purse, taken);
            Assert.AreEqual(2, _sheet.Get(CharacterSkill.Theft), "every try grows Theft (S5b.2): a townsman of Perception 1 is a stage 9 task");
            Assert.AreEqual(bronze + purse, _bag.Bronze, "its bronze in the bag");
            Assert.AreEqual(0, _standing.ReputationPoints, "unseen, nothing counts against Reputation");
            Assert.IsTrue(_theft.TriedToday("Townsman 3"), "once a day");

            _clock.AdvanceTicks(24 * 60);
            Assert.IsFalse(_theft.TriedToday("Townsman 3"), "and again the next day");
        }

        [Test]
        public void ABadResultIsNoticedAndCountsAgainstReputation()
        {
            var bronze = _bag.Bronze;
            Assert.AreEqual(TheftOutcome.Noticed, _theft.Resolve("Townswoman 1", ReactionResult.Bad, out var taken));
            Assert.AreEqual((0, bronze), (taken, _bag.Bronze), "nothing taken");
            Assert.AreEqual(_rules.Points(Deed.TheftNoticed), _standing.ReputationPoints, "the Deeds Record keeps the noticed theft");
            Assert.Less(_rules.Points(Deed.TheftNoticed), 0, "against Reputation");
            Assert.IsTrue(_theft.TriedToday("Townswoman 1"), "no second try today");
        }
    }
}
