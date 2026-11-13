using GameDev.World.Seed;
using GameDev.World.Timekeeping;
using UnityEngine;

namespace GameDev.Slice.Mind
{
    /// <summary>Where the character sleeps (18 §3; 11 *Sleep*).</summary>
    public enum SleepPlace
    {
        /// <summary>The Soot &amp; Bell's let room, paid each sleep; safe.</summary>
        Room,

        /// <summary>A street inside the walls, where a Veinmarket thief may come.</summary>
        Street,

        /// <summary>Outside the walls, where a beast may find the sleeper.</summary>
        Wild,
    }

    /// <summary>How long the character sleeps (owner, 2026-11-13, the answer to S5-6).</summary>
    public enum SleepLength
    {
        /// <summary>Until the next 07:00.</summary>
        Morning,

        /// <summary>For 6 hours.</summary>
        SixHours,
    }

    /// <summary>
    /// Sleep in the slice (S5c.2; 18 §3, 11 *Sleep*), with no scene code: when a sleep ends, where the walls stand, and
    /// who comes to a sleeper in the street, each drawn once a sleep from the world's seed. Recovery is
    /// <see cref="SliceMind.Slept"/>'s and the scene's (<see cref="SleepRun"/>). Numbers are authoring defaults but where
    /// a document gives them.
    /// </summary>
    public static class SliceSleep
    {
        /// <summary>19 §6.1: a tavern room for a night, bronze.</summary>
        public const int RoomPrice = 300;

        /// <summary>The owner's 07:00 (S5-6), minute of the day.</summary>
        public const int WakeMinute = 7 * 60;

        /// <summary>The owner's 6 hours (S5-6), minutes; as long as a full sleep.</summary>
        public const int ShortMinutes = SliceMind.FullSleepMinutes;

        /// <summary>18 §3: in the city a thief comes about one night in three.</summary>
        public const int ThiefOneIn = 3;

        /// <summary>Authoring default, 18 §3: outside the walls a beast finds one sleep in two.</summary>
        public const int BeastOneIn = 2;

        // The walls of C4 as VeilmouthC5Layout.json has them, measured: the town and works walls' west face at plan x 420,
        // the north wall's inner face at plan y 260, the works' south wall's inner face at plan y 702; the world is
        // (x − 500, 500 − y) of the plan. The sea closes the east.
        public const float WestWall = -80f;
        public const float NorthWall = 240f;
        public const float SouthWall = -202f;

        /// <summary>Whether a place in Veilmouth lies inside its walls, the works' too.</summary>
        public static bool InsideWalls(Vector3 world) => world.x > WestWall && world.z < NorthWall && world.z > SouthWall;

        /// <summary>The place a sleep in the city scene at <paramref name="world"/> is: the street inside the walls, the wild outside.</summary>
        public static SleepPlace Outdoors(Vector3 world) => InsideWalls(world) ? SleepPlace.Street : SleepPlace.Wild;

        /// <summary>The tick a sleep begun at <paramref name="now"/> ends: the next 07:00, or 6 hours on.</summary>
        public static long EndOf(long now, SleepLength length) => length == SleepLength.SixHours ? now + ShortMinutes : NextWake(now);

        /// <summary>The next 07:00 after <paramref name="now"/>.</summary>
        public static long NextWake(long now)
        {
            var wake = ((now / GameClock.MinutesPerDay) * GameClock.MinutesPerDay) + WakeMinute;
            return wake > now ? wake : wake + GameClock.MinutesPerDay;
        }

        /// <summary>07.2 §3.3: a sleep of 6 hours or more is full.</summary>
        public static bool Full(long minutes) => minutes >= SliceMind.FullSleepMinutes;

        /// <summary>One in so many sleeps in <paramref name="place"/> brings a visitor: the thief in the street, a beast in the wild.</summary>
        public static int VisitOneIn(SleepPlace place) => place == SleepPlace.Street ? ThiefOneIn : place == SleepPlace.Wild ? BeastOneIn : 0;

        /// <summary>
        /// The tick between <paramref name="start"/> and <paramref name="end"/> at which a visitor comes to a sleeper in
        /// <paramref name="place"/>, or null for a quiet sleep: one draw a sleep, from the stream of sleep forked with its start.
        /// </summary>
        public static long? Visit(IWorldSeed seed, SleepPlace place, long start, long end, double? chance = null)
        {
            var oneIn = VisitOneIn(place);
            if (seed == null || oneIn <= 0 || end - start < 2)
            {
                return null;
            }

            var rng = seed.Fork(SliceSeedStreams.Sleep, (int)(start % int.MaxValue), (int)place);
            if (rng.NextDouble() >= (chance ?? (1d / oneIn)))
            {
                return null;
            }

            return start + 1 + rng.Next((int)System.Math.Min(int.MaxValue, end - start - 1));
        }

        /// <summary>18 §3: what the thief takes, half the coins rounded down, never an item.</summary>
        public static int ThiefTakes(int bronze) => System.Math.Max(0, bronze) / 2;
    }
}
