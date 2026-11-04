using System;
using System.Collections.Generic;

namespace GameDev.World.Timekeeping
{
    /// <summary>What the save keeps of the clock: the tick and the progress toward the next one (17 X.1).</summary>
    [Serializable]
    public struct GameClockState
    {
        public long tick;
        public double tickProgress;
    }

    /// <summary>Plain <see cref="IWorldClock"/> model. Only its driver feeds it real time.</summary>
    public sealed class GameClock : IWorldClock
    {
        public const int MinutesPerDay = 1440;

        private readonly double _realSecondsPerTick;
        private readonly HashSet<PauseHolder> _pauseHolders = new HashSet<PauseHolder>();
        private double _secondsIntoTick;

        public GameClock(double realSecondsPerTick, long startTick)
        {
            if (!(realSecondsPerTick > 0d))
            {
                throw new ArgumentOutOfRangeException(nameof(realSecondsPerTick), "A tick must last more than zero real seconds.");
            }

            if (startTick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startTick), "The clock cannot start before day 1, 00:00.");
            }

            _realSecondsPerTick = realSecondsPerTick;
            Tick = startTick;
        }

        public long Tick { get; private set; }
        public double TickProgress => _secondsIntoTick / _realSecondsPerTick;
        public int Day => (int)(Tick / MinutesPerDay) + 1;
        public int MinuteOfDay => (int)(Tick % MinutesPerDay);
        public bool IsPaused => _pauseHolders.Count > 0;

        public event Action<long> TickBoundary;
        public event Action<long, long> TimeAdvanced;

        public IDisposable Pause()
        {
            var holder = new PauseHolder(this);
            _pauseHolders.Add(holder);
            return holder;
        }

        /// <summary>Feeds real seconds; ignored while paused or for a non-positive amount.</summary>
        public void AdvanceRealTime(double seconds)
        {
            if (IsPaused || !(seconds > 0d))
            {
                return;
            }

            _secondsIntoTick += seconds;
            if (_secondsIntoTick < _realSecondsPerTick)
            {
                return;
            }

            var whole = (int)Math.Floor(_secondsIntoTick / _realSecondsPerTick);
            _secondsIntoTick -= whole * _realSecondsPerTick;
            AdvanceTicks(whole);
        }

        public void AdvanceTicks(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The clock never runs backwards.");
            }

            if (count == 0)
            {
                return;
            }

            var from = Tick;
            for (var i = 0; i < count; i++)
            {
                Tick++;
                TickBoundary?.Invoke(Tick);
            }

            TimeAdvanced?.Invoke(from, Tick);
        }

        public GameClockState Capture()
        {
            return new GameClockState { tick = Tick, tickProgress = TickProgress };
        }

        /// <summary>Puts the clock back to a saved state without raising events; pause holders stay.</summary>
        public void Restore(GameClockState state)
        {
            if (state.tick < 0 || state.tickProgress < 0d || state.tickProgress >= 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(state), "A saved clock needs a tick of at least 0 and progress in [0, 1).");
            }

            Tick = state.tick;
            _secondsIntoTick = state.tickProgress * _realSecondsPerTick;
        }

        private sealed class PauseHolder : IDisposable
        {
            private GameClock _clock;

            public PauseHolder(GameClock clock)
            {
                _clock = clock;
            }

            public void Dispose()
            {
                _clock?._pauseHolders.Remove(this);
                _clock = null;
            }
        }
    }
}
