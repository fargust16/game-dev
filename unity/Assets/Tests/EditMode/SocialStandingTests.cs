using System.Linq;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>The character's standing against 10.4 §4–5, on a clock driven by hand and the project's own rules asset.</summary>
    public sealed class SocialStandingTests
    {
        private const string RulesPath = "Assets/Data/Progression/StandingRules.asset";
        private const int Seed = 110201;
        private const long Evening = 20 * 60;

        private StandingRules _rules;
        private WorldState _state;
        private GameClock _clock;
        private SocialStanding _standing;

        [SetUp]
        public void Build()
        {
            _rules = AssetDatabase.LoadAssetAtPath<StandingRules>(RulesPath);
            Assert.IsNotNull(_rules, RulesPath);
            _state = new WorldState();
            _clock = new GameClock(10d, Evening);
            _standing = new SocialStanding(_state, _rules, _clock, new WorldSeed(Seed), SliceSeedStreams.Lies);
        }

        [TearDown]
        public void Dispose()
        {
            _standing.Dispose();
        }

        [TestCase(-10, -2)]
        [TestCase(-9, -1)]
        [TestCase(-3, -1)]
        [TestCase(-2, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(14, 1)]
        [TestCase(15, 2)]
        [TestCase(40, 2)]
        public void ReputationReadsAs10_4(int points, int modifier)
        {
            _state.Set(SocialStanding.ReputationKey, points);
            Assert.AreEqual(modifier, _standing.ReputationModifier);
        }

        [TestCase(0, 0, -1)]
        [TestCase(2, 0, -1)]
        [TestCase(3, 0, 0)]
        [TestCase(9, 0, 0)]
        [TestCase(0, 2, 1)]
        [TestCase(19, 0, 1)]
        [TestCase(10, 2, 2)]
        public void DangerIsKillsAndFiveForEachLimit(int kills, int limits, int modifier)
        {
            for (var i = 0; i < kills; i++)
            {
                _standing.RecordKill();
            }

            for (var i = 0; i < limits; i++)
            {
                _standing.RecordLimitReached();
            }

            Assert.AreEqual(kills + 5 * limits, _standing.Danger);
            Assert.AreEqual(modifier, _standing.DangerModifier);
        }

        [Test]
        public void DeedsAddTheirPoints()
        {
            _standing.Record(Deed.FactionTask);
            _standing.Record(Deed.FactionTask);
            _standing.Record(Deed.NpcHelped);
            Assert.AreEqual(7, _standing.ReputationPoints);
            _standing.Record(Deed.TheftNoticed);
            _standing.Record(Deed.BreakInNoticed);
            Assert.AreEqual(-1, _standing.ReputationPoints);
            _standing.Record(Deed.CityLiberated);
            Assert.AreEqual(9, _standing.ReputationPoints);
        }

        /// <summary>10.2 §5c, 10.4 §4 and §6 *Accusation*: −3 for an Assault, a point for every 5 HP of harm, −10 for a kill; a Warden accuses at −10, a talk lets off until 3 lower, and paying settles at −5.</summary>
        [Test]
        public void AnAssaultItsHarmAndAKillCostReputationAndPayingSettlesIt()
        {
            _standing.Record(Deed.Assault);
            Assert.AreEqual(-3, _standing.ReputationPoints);
            _standing.RecordHarm(12d);
            Assert.AreEqual(-6, _standing.ReputationPoints, "12 HP of harm, rounded up to 3 points");
            _standing.RecordHarm(0d);
            _standing.RecordHarm(10d);
            Assert.AreEqual(-8, _standing.ReputationPoints, "none for no harm, 2 for 10 HP");
            Assert.IsFalse(_standing.Accused, "above −10 no Warden accuses");
            _standing.Record(Deed.InnocentKilled);
            Assert.AreEqual(-18, _standing.ReputationPoints);
            Assert.IsTrue(_standing.Accused, "at −10 or lower a Warden accuses");
            _standing.LetOff();
            Assert.IsFalse(_standing.Accused, "a talk lets the character off");
            _standing.RecordHarm(10d);
            Assert.IsFalse(_standing.Accused, "while its Reputation stands within 3 points");
            _standing.RecordHarm(1d);
            Assert.IsTrue(_standing.Accused, "and no longer once it falls 3 lower");
            _standing.Settle();
            Assert.AreEqual(-5, _standing.ReputationPoints, "paid, the name rises to −5");
            Assert.IsFalse(_standing.Accused);
        }

        [Test]
        public void EachSkillAndListenerTakesItsReading()
        {
            _state.Set(SocialStanding.ReputationKey, 15);
            _state.Set(SocialStanding.KillsKey, 3);

            AssertReading(CharacterSkill.Persuasion, false, StandingSource.Reputation, 2);
            AssertReading(CharacterSkill.Persuasion, true, StandingSource.Danger, 0);
            AssertReading(CharacterSkill.Intimidation, false, StandingSource.Danger, 0);
            AssertReading(CharacterSkill.Intimidation, true, StandingSource.Danger, 0);
            AssertReading(CharacterSkill.Deception, false, StandingSource.Reputation, 2);
            AssertReading(CharacterSkill.Deception, true, StandingSource.Reputation, -2);

            _state.Set(SocialStanding.ReputationKey, -10);
            AssertReading(CharacterSkill.Deception, false, StandingSource.Reputation, -2);
            AssertReading(CharacterSkill.Deception, true, StandingSource.Reputation, 0);
        }

        [Test]
        public void AttitudeStaysWithinTwoAndIntimidationCostsAStep()
        {
            Assert.AreEqual(0, _standing.Attitude("nell"));
            _standing.ChangeAttitude("nell", 5);
            Assert.AreEqual(2, _standing.Attitude("nell"));
            _standing.Intimidated("nell");
            Assert.AreEqual(1, _standing.Attitude("nell"));
            _standing.ChangeAttitude("nell", -9);
            Assert.AreEqual(-2, _standing.Attitude("nell"));
            Assert.AreEqual(0, _standing.Attitude("kit"), "Each person has an Attitude of its own.");
        }

        [Test]
        public void AFailedLieCostsFiveReputation()
        {
            _standing.LieFailed();
            Assert.AreEqual(-5, _standing.ReputationPoints);
        }

        [Test]
        public void ALieComesOutOnlyAtMidnight()
        {
            var people = Enumerable.Range(0, 1000).Select(i => "listener" + i).ToList();
            foreach (var person in people)
            {
                _standing.LieTold(person, RequestSize.Large);
            }

            _clock.AdvanceTicks((int)(GameClock.MinutesPerDay - Evening - 1));
            Assert.AreEqual(0, _standing.ReputationPoints, "Nothing comes out before the day boundary.");

            var found = 0;
            _standing.LieFoundOut += _ => found++;
            _clock.AdvanceTicks(1);

            Assert.AreEqual(0.4, found / (double)people.Count, 0.05, "A Large lie comes out at 40 %.");
            Assert.AreEqual(-5 * found, _standing.ReputationPoints);
            Assert.AreEqual(found, people.Count(p => _standing.Attitude(p) == -2));
            Assert.AreEqual(people.Count - found, people.Count(p => _standing.Attitude(p) == 0));
            Assert.IsFalse(_state.Keys.Any(k => k.StartsWith("lie.")), "Each lie has its one chance.");

            _clock.AdvanceTicks(GameClock.MinutesPerDay);
            Assert.AreEqual(-5 * found, _standing.ReputationPoints, "The next midnight finds nothing more.");
        }

        [Test]
        public void OrdinaryLiesComeOutAtTwentyPercentAndSmallOnesNever()
        {
            for (var i = 0; i < 1000; i++)
            {
                _standing.LieTold("ordinary" + i, RequestSize.Ordinary);
                _standing.LieTold("small" + i, RequestSize.Small);
            }

            var found = 0;
            _standing.LieFoundOut += _ => found++;
            _clock.AdvanceTicks(GameClock.MinutesPerDay);

            Assert.AreEqual(0.2, found / 1000d, 0.04);
            Assert.IsTrue(Enumerable.Range(0, 1000).All(i => _standing.Attitude("small" + i) == 0));
        }

        [Test]
        public void TheSameSeedFindsTheSameLies()
        {
            string[] Run()
            {
                var state = new WorldState();
                var clock = new GameClock(10d, Evening);
                var standing = new SocialStanding(state, _rules, clock, new WorldSeed(Seed), SliceSeedStreams.Lies);
                for (var i = 0; i < 50; i++)
                {
                    standing.LieTold("listener" + i, RequestSize.Large);
                }

                clock.AdvanceTicks(GameClock.MinutesPerDay);
                standing.Dispose();
                return state.Keys.Where(k => k.StartsWith("attitude.")).ToArray();
            }

            CollectionAssert.AreEqual(Run(), Run());
        }

        [Test]
        public void ALieToSomeoneGoneNeverComesOut()
        {
            for (var i = 0; i < 20; i++)
            {
                _standing.LieTold("conductor", RequestSize.Large);
            }

            _standing.ListenerGone("conductor");
            _clock.AdvanceTicks(GameClock.MinutesPerDay);
            Assert.AreEqual(0, _standing.ReputationPoints);
            Assert.AreEqual(0, _standing.Attitude("conductor"));
        }

        [Test]
        public void TheWorldStateSaveKeepsALieUntilMidnight()
        {
            for (var i = 0; i < 20; i++)
            {
                _standing.LieTold("harrow", RequestSize.Large);
            }

            _standing.Record(Deed.NpcHelped);
            var saved = _state.Capture();

            var state = new WorldState();
            state.Restore(saved);
            var clock = new GameClock(10d, Evening);
            var standing = new SocialStanding(state, _rules, clock, new WorldSeed(Seed), SliceSeedStreams.Lies);
            var found = 0;
            standing.LieFoundOut += _ => found++;
            clock.AdvanceTicks(GameClock.MinutesPerDay);
            standing.Dispose();

            Assert.Greater(found, 0, "Twenty Large lies to one listener: at least one comes out.");
            Assert.AreEqual(1 - 5 * found, standing.ReputationPoints);
            Assert.AreEqual(-2, standing.Attitude("harrow"));
        }

        private void AssertReading(CharacterSkill skill, bool enemy, StandingSource source, int modifier)
        {
            var reading = _standing.Reading(skill, enemy);
            var who = enemy ? "an enemy" : "a friend";
            Assert.AreEqual(source, reading.Source, $"{skill} with {who}");
            Assert.AreEqual(modifier, reading.Modifier, $"{skill} with {who}");
        }

        [Test]
        public void ThePromiseIsGivenOnceALifeAndABelievedOneDoublesAnAssault()
        {
            Assert.IsTrue(_standing.PromiseOpen);
            Assert.AreEqual(1, _standing.AssaultFactor);
            _standing.Promise(false);
            Assert.IsFalse(_standing.PromiseOpen, "given, it is spent even when not believed");
            Assert.AreEqual(1, _standing.AssaultFactor, "a Promise not believed doubles nothing");

            var believed = new SocialStanding(new WorldState(), _rules, _clock, new WorldSeed(Seed), SliceSeedStreams.Lies);
            believed.Promise(true);
            Assert.AreEqual(2, believed.AssaultFactor);
            believed.Record(Deed.Assault);
            Assert.AreEqual(2 * _rules.Points(Deed.Assault), believed.ReputationPoints, "an Assault after it costs double");
            believed.RecordHarm(10d);
            Assert.AreEqual(2 * (_rules.Points(Deed.Assault) - _rules.HarmPoints(10d)), believed.ReputationPoints, "its harm too");
            var before = believed.ReputationPoints;
            believed.Record(Deed.InnocentKilled);
            Assert.AreEqual(before + (2 * _rules.Points(Deed.InnocentKilled)), believed.ReputationPoints, "and a kill");
            before = believed.ReputationPoints;
            believed.Record(Deed.TheftNoticed);
            Assert.AreEqual(before + _rules.Points(Deed.TheftNoticed), believed.ReputationPoints, "other deeds count once");
            believed.Dispose();
        }
    }
}
