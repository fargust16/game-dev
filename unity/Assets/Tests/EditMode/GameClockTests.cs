using System.Collections.Generic;
using GameDev.World.Timekeeping;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    public sealed class GameClockTests
    {
        private const double SecondsPerTick = 10d;
        private const long Start = 480; // day 1, 08:00

        private static GameClock NewClock() => new GameClock(SecondsPerTick, Start);

        [Test]
        public void StartsAtTheGivenTick()
        {
            var clock = NewClock();
            Assert.AreEqual(Start, clock.Tick);
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(480, clock.MinuteOfDay);
            Assert.AreEqual(0d, clock.TickProgress);
        }

        [Test]
        public void TenRealSecondsMakeOneTick()
        {
            var clock = NewClock();
            var boundaries = new List<long>();
            clock.TickBoundary += boundaries.Add;

            clock.AdvanceRealTime(9.5d);
            Assert.AreEqual(Start, clock.Tick);
            Assert.IsEmpty(boundaries);

            clock.AdvanceRealTime(0.5d);
            Assert.AreEqual(Start + 1, clock.Tick);
            CollectionAssert.AreEqual(new[] { Start + 1 }, boundaries);
        }

        [Test]
        public void ALongFrameCrossesSeveralTicksAndKeepsTheRest()
        {
            var clock = NewClock();
            var advances = new List<(long, long)>();
            var boundaries = 0;
            clock.TickBoundary += _ => boundaries++;
            clock.TimeAdvanced += (from, to) => advances.Add((from, to));

            clock.AdvanceRealTime(25d);

            Assert.AreEqual(Start + 2, clock.Tick);
            Assert.AreEqual(0.5d, clock.TickProgress, 1e-9);
            Assert.AreEqual(2, boundaries);
            CollectionAssert.AreEqual(new[] { (Start, Start + 2) }, advances);
        }

        [Test]
        public void PauseStopsRealTimeUntilEveryHolderIsReleased()
        {
            var clock = NewClock();
            var dialogue = clock.Pause();
            var menu = clock.Pause();

            clock.AdvanceRealTime(30d);
            Assert.AreEqual(Start, clock.Tick);

            dialogue.Dispose();
            dialogue.Dispose();
            Assert.IsTrue(clock.IsPaused);

            menu.Dispose();
            Assert.IsFalse(clock.IsPaused);
            clock.AdvanceRealTime(10d);
            Assert.AreEqual(Start + 1, clock.Tick);
        }

        [Test]
        public void WholeTicksAdvanceWhilePausedAndKeepPartialProgress()
        {
            var clock = NewClock();
            clock.AdvanceRealTime(4d);
            using (clock.Pause())
            {
                clock.AdvanceTicks(1); // one combat round, or the end of a dialogue
            }

            Assert.AreEqual(Start + 1, clock.Tick);
            Assert.AreEqual(0.4d, clock.TickProgress, 1e-9);
        }

        [Test]
        public void DaysRollOverAtMidnight()
        {
            var clock = new GameClock(SecondsPerTick, GameClock.MinutesPerDay - 1);
            clock.AdvanceTicks(1);
            Assert.AreEqual(2, clock.Day);
            Assert.AreEqual(0, clock.MinuteOfDay);
        }

        [Test]
        public void CaptureAndRestoreKeepTickAndProgress()
        {
            var clock = NewClock();
            clock.AdvanceRealTime(37d);
            var saved = clock.Capture();

            var loaded = NewClock();
            loaded.Restore(saved);

            Assert.AreEqual(clock.Tick, loaded.Tick);
            Assert.AreEqual(clock.TickProgress, loaded.TickProgress, 1e-9);
            loaded.AdvanceRealTime(3.5d);
            Assert.AreEqual(Start + 4, loaded.Tick);
        }

        [Test]
        public void InvalidInputIsRefused()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameClock(0d, Start));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new GameClock(SecondsPerTick, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => NewClock().AdvanceTicks(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => NewClock().Restore(new GameClockState { tick = Start, tickProgress = 1d }));
        }
    }
}
