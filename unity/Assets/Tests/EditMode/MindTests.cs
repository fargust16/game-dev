using GameDev.Player.Progression;
using GameDev.Slice.Mind;
using GameDev.World.State;
using GameDev.World.Timekeeping;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.6 (07.2 §3.3 *Fatigue*, *Full exhaustion*, §3.4 *Sanity in the slice*): Sanity's bands and what each takes away,
    /// each keeping the penalties of those above it, Fatigue's added to them; full exhaustion's damage and Will's XP once
    /// an episode that spent 30 %; sleep's return; and the end of the run at 0.
    /// </summary>
    public sealed class MindTests
    {
        private WorldState _state;
        private GameClock _clock;
        private SliceSanity _sanity;
        private SliceMind _mind;

        [SetUp]
        public void SetUp()
        {
            _state = new WorldState();
            _clock = new GameClock(1d, 9 * 60);
            _sanity = new SliceSanity(_state);
            _mind = new SliceMind(_state, _clock, _sanity);
        }

        private void SanityAt(int value) => _sanity.Lose(SliceSanity.Max - value - (SliceSanity.Max - _sanity.Value));

        [TestCase(100, SanityBand.Stable)]
        [TestCase(99, SanityBand.Mild)]
        [TestCase(70, SanityBand.Mild)]
        [TestCase(69, SanityBand.Significant)]
        [TestCase(40, SanityBand.Significant)]
        [TestCase(39, SanityBand.Severe)]
        [TestCase(10, SanityBand.Severe)]
        [TestCase(9, SanityBand.Critical)]
        [TestCase(1, SanityBand.Critical)]
        [TestCase(0, SanityBand.Lost)]
        public void SanityFallsIntoItsBand(int sanity, SanityBand band) => Assert.AreEqual(band, SliceMind.BandOf(sanity));

        [Test]
        public void EachBandKeepsThePenaltiesAboveItAndAddsItsOwn()
        {
            Assert.AreEqual((0, 0, 0, 0f, 1d), Penalties());
            SanityAt(80);
            Assert.AreEqual((0, 0, 0, 0f, 1d), Penalties(), "Mild: only Will Power's slower return");
            Assert.IsFalse(_mind.WillPowerReturns(4));
            Assert.IsTrue(_mind.WillPowerReturns(5));
            SanityAt(50);
            Assert.AreEqual((-5, -1, -1, 0f, 1d), Penalties());
            Assert.IsFalse(_mind.WillPowerReturns(8), "the slower return stays");
            SanityAt(20);
            Assert.AreEqual((-10, -2, -2, -0.25f, 1d), Penalties());
            SanityAt(5);
            Assert.AreEqual((-15, -3, -3, -0.5f, 0.5d), Penalties(), "Critical: the zones half as wide");
        }

        private (int, int, int, float, double) Penalties() =>
            (_mind.HitPenalty, _mind.CheckPenalty, _mind.SocialPenalty, _mind.WillPowerShift, _mind.ReactionShare);

        [Test]
        public void FatigueComesWithTheHoursAwakeAndAddsToSanity()
        {
            Assert.AreEqual(FatigueState.Rested, _mind.Fatigue, "a run starts rested");
            _clock.AdvanceTicks((SliceMind.TiredHours * 60) - 1);
            Assert.AreEqual(FatigueState.Rested, _mind.Fatigue);
            _clock.AdvanceTicks(1);
            Assert.AreEqual(FatigueState.Tired, _mind.Fatigue);
            Assert.AreEqual((-5, 0.8d, 0.5d), (_mind.HitPenalty, _mind.ReactionShare, _mind.StaminaShare));

            SanityAt(50);
            Assert.AreEqual(-10, _mind.HitPenalty, "Sanity's and Fatigue's add up");
            _clock.AdvanceTicks((SliceMind.VeryTiredHours - SliceMind.TiredHours) * 60);
            Assert.AreEqual(FatigueState.VeryTired, _mind.Fatigue);
            Assert.AreEqual((-15, -2, 0.6d), (_mind.HitPenalty, _mind.SocialPenalty, _mind.ReactionShare));
            Assert.IsFalse(_mind.WillPowerReturns(5), "no return Very tired");

            var awake = _mind.MinutesAwake;
            _mind.Asleep = true;
            _clock.AdvanceTicks(120);
            Assert.AreEqual(awake, _mind.MinutesAwake, "sleep is not waking time");
            _mind.Asleep = false;
        }

        [Test]
        public void AFullSleepClearsFatigueAndGivesBackSanityAndAShortOneTakesOffTwiceItsLength()
        {
            _clock.AdvanceTicks(20 * 60);
            _mind.Slept(60);
            Assert.AreEqual(18 * 60, _mind.MinutesAwake);
            SanityAt(60);
            _mind.Slept(SliceMind.FullSleepMinutes);
            Assert.AreEqual((0, 63), (_mind.MinutesAwake, _sanity.Value));
        }

        [Test]
        public void FullExhaustionHurtsSanityAndRewardsWillOnceAnEpisodeSpentThirtyPercent()
        {
            Assert.AreEqual(5, SliceMind.ExhaustionDamageOf(6, 1d), "Limit 9, Will 6: about 5");
            Assert.AreEqual(5, SliceMind.ExhaustionDamageOf(12, 1.15d), "Limit 8, Will 12: about 5 as well");

            _mind.Spent(5d, 20d);
            _mind.Spent(1d, 20d);
            var (damage, reward) = _mind.Exhaust(6, 1d);
            Assert.AreEqual((5, true, 95), (damage, reward, _sanity.Value));

            _mind.Spent(5d, 20d);
            Assert.IsFalse(_mind.Exhaust(6, 1d).Reward, "a trivial episode earns nothing");
            Assert.AreEqual(90, _sanity.Value, "though it still hurts");
        }

        [Test]
        public void SanityZeroEndsTheRun()
        {
            var lost = 0;
            _mind.Lost += () => lost++;
            SanityAt(3);
            Assert.AreEqual(0, lost);
            _sanity.Lose(5);
            Assert.AreEqual((1, SanityBand.Lost), (lost, _mind.Band));
        }

        [Test]
        public void ANarrowedReactionBarKeepsItsCentreAndItsFloor()
        {
            var bar = new ReactionBar(0.4d, 0.2d, 0.45d, 0.1d);
            var half = bar.Narrowed(0.5d);
            Assert.AreEqual(0.45d, half.GoodStart, 1e-9);
            Assert.AreEqual(0.1d, half.GoodWidth, 1e-9);
            Assert.AreEqual(0.475d, half.PerfectStart, 1e-9);
            Assert.AreEqual(0.05d, half.PerfectWidth, 1e-9);
            Assert.AreEqual(ReactionCheck.LeastWidth, bar.Narrowed(0.1d).GoodWidth, 1e-9, "never under the bar's floor");
            Assert.AreEqual(bar.GoodWidth, bar.Narrowed(1d).GoodWidth, "a whole share leaves it as it is");
        }

        [Test]
        public void EachBandShowsTheSymptomsAboveItAndItsOwn()
        {
            Assert.IsFalse(MindSymptoms.Allows(SanityBand.Stable, MindSymptom.Whisper));
            Assert.IsTrue(MindSymptoms.Allows(SanityBand.Mild, MindSymptom.Whisper));
            Assert.IsFalse(MindSymptoms.Allows(SanityBand.Mild, MindSymptom.Glimpse));
            Assert.IsTrue(MindSymptoms.Allows(SanityBand.Significant, MindSymptom.Glimpse));
            Assert.IsTrue(MindSymptoms.Allows(SanityBand.Severe, MindSymptom.Whisper));
            Assert.IsTrue(MindSymptoms.Allows(SanityBand.Severe, MindSymptom.FalseAlarm));
            Assert.IsFalse(MindSymptoms.Allows(SanityBand.Severe, MindSymptom.DialogueWhisper));
            Assert.IsTrue(MindSymptoms.Allows(SanityBand.Critical, MindSymptom.DialogueWhisper));
        }
    }
}
