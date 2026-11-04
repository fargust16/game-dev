using System;

// The namespace is Timekeeping, not Time: a GameDev.World.Time namespace would hide
// UnityEngine.Time from every script in GameDev.World.
namespace GameDev.World.Timekeeping
{
    /// <summary>
    /// The one authoritative game clock (17 X.1; 18 World Time). One tick is one game minute.
    /// Real time moves it only while nothing holds a pause; combat rounds, the end of a dialogue,
    /// and sleep move it by whole ticks through <see cref="AdvanceTicks"/>, paused or not.
    /// </summary>
    public interface IWorldClock
    {
        /// <summary>Whole game minutes since day 1, 00:00.</summary>
        long Tick { get; }

        /// <summary>Progress toward the next tick, 0 inclusive to 1 exclusive.</summary>
        double TickProgress { get; }

        /// <summary>Day number, starting at 1.</summary>
        int Day { get; }

        /// <summary>Minute of the day, 0 to 1439.</summary>
        int MinuteOfDay { get; }

        bool IsPaused { get; }

        /// <summary>Raised once for every tick crossed, with the new tick.</summary>
        event Action<long> TickBoundary;

        /// <summary>Raised once per advance, after its tick boundaries, with the first and last tick.</summary>
        event Action<long, long> TimeAdvanced;

        /// <summary>Stops real time until the returned holder is disposed. Holders stack.</summary>
        IDisposable Pause();

        /// <summary>Moves the clock by whole ticks, keeping partial progress. Works while paused.</summary>
        void AdvanceTicks(int count);
    }
}
