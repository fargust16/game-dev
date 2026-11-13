using System.Collections.Generic;
using GameDev.Core.Localization;
using GameDev.World.Seed;
using GameDev.World.State;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.News
{
    /// <summary>Whose death a News item tells (10.2 §5e): what the city calls the one who died.</summary>
    public enum NewsVictim
    {
        Townsman = 1,
        Townswoman = 2,
        Warden = 3,
        Church = 4,
        Stranger = 5,
    }

    /// <summary>Who killed, as a witness tells it; <see cref="Unseen"/> when nobody saw it (10.2 §5e).</summary>
    public enum NewsKiller
    {
        Unseen = 0,
        Character = 1,
        Warden = 2,
        Resident = 3,
        Beast = 4,
        Outsider = 5,
    }

    /// <summary>
    /// 09 *News Pool* (owner, 2026-11-09), Veilmouth's, its first items the deaths of 10.2 §5e (owner, 2026-11-12; PB8d).
    /// A death enters the pool 1–2 game hours after it, the delay drawn by the seed, and stays 8 game hours from entering;
    /// meanwhile the Notice Board tells it, with the place, how long ago and who killed, if someone saw it. A death is an
    /// instant event, so it is told in the past tense only. The pool lives in the world state, so a save keeps it.
    /// </summary>
    public sealed class SliceNews
    {
        /// <summary>09: an item stays this many ticks, 8 game hours, from entering (authoring default).</summary>
        public const int StaysTicks = 480;

        /// <summary>10.2 §5e: a death enters 1 to 2 game hours after it (authoring default).</summary>
        public const int DelayLeast = 60;

        public const int DelayMost = 120;

        /// <summary>The ago lines run from about an hour to about this many hours.</summary>
        public const int MostHours = 10;

        private const string CountKey = "news.count";
        private const int Death = 1;

        private readonly IWorldState _state;
        private readonly IWorldSeed _seed;

        public SliceNews(IWorldState state, IWorldSeed seed)
        {
            _state = state;
            _seed = seed;
        }

        /// <summary>One item of the pool.</summary>
        public readonly struct Item
        {
            public Item(int index, long happened, long enters, string place, NewsVictim victim, NewsKiller killer)
            {
                Index = index;
                Happened = happened;
                Enters = enters;
                Place = place;
                Victim = victim;
                Killer = killer;
            }

            public int Index { get; }

            /// <summary>The tick of the death.</summary>
            public long Happened { get; }

            /// <summary>The tick it entered, or enters, the pool.</summary>
            public long Enters { get; }

            /// <summary>The map key of its place.</summary>
            public string Place { get; }

            public NewsVictim Victim { get; }

            public NewsKiller Killer { get; }

            /// <summary>Whether the pool holds it at <paramref name="tick"/>.</summary>
            public bool In(long tick) => tick >= Enters && tick < Enters + StaysTicks;
        }

        /// <summary>How many items have ever been made, those still to enter and those gone too.</summary>
        public int Count => _state.Get(CountKey);

        /// <summary>Adds a death at <paramref name="tick"/>: it enters the pool 1–2 game hours later. Its index.</summary>
        public int AddDeath(long tick, Vector3 at, NewsVictim victim, NewsKiller killer)
        {
            var n = Count + 1;
            var rolls = _seed != null ? _seed.Fork(SliceSeedStreams.News, n) : new Random(n);
            var enters = tick + DelayLeast + rolls.Next(DelayMost - DelayLeast + 1);
            _state.Set(Key(n, "kind"), Death);
            _state.Set(Key(n, "happened"), (int)tick);
            _state.Set(Key(n, "enters"), (int)enters);
            _state.Set(Key(n, "place"), StableHash.Of(SlicePlaces.KeyAt(at)));
            _state.Set(Key(n, "victim"), (int)victim);
            _state.Set(Key(n, "killer"), (int)killer);
            _state.Set(CountKey, n);
            return n;
        }

        /// <summary>The item of this index.</summary>
        public Item Get(int n) => new Item(n, _state.Get(Key(n, "happened")), _state.Get(Key(n, "enters")), SlicePlaces.KeyOf(_state.Get(Key(n, "place"))),
            (NewsVictim)_state.Get(Key(n, "victim")), (NewsKiller)_state.Get(Key(n, "killer")));

        /// <summary>The items the pool holds at <paramref name="tick"/>, the newest first, as the board pins them.</summary>
        public IReadOnlyList<Item> Told(long tick)
        {
            var items = new List<Item>();
            for (var n = Count; n >= 1; n--)
            {
                var item = Get(n);
                if (item.In(tick))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        /// <summary>What the board says of <paramref name="item"/> at <paramref name="tick"/>, in the language shown.</summary>
        public static string Line(Item item, long tick) =>
            Loc.Format("news.death.line", Loc.Get(VictimKey(item.Victim)), SlicePlaces.Name(item.Place), Loc.Get(AgoKey(tick - item.Happened)),
                item.Killer == NewsKiller.Unseen ? Loc.Get("news.death.unseen") : Loc.Format("news.death.seen", Loc.Get(KillerKey(item.Killer))));

        /// <summary>The line of how long ago, by whole hours rounded, from about one to about ten.</summary>
        public static string AgoKey(long minutes) => $"news.ago.{Mathf.Clamp(Mathf.RoundToInt(minutes / 60f), 1, MostHours)}";

        public static string VictimKey(NewsVictim victim) => $"news.victim.{victim.ToString().ToLowerInvariant()}";

        public static string KillerKey(NewsKiller killer) => $"news.killer.{killer.ToString().ToLowerInvariant()}";

        private static string Key(int n, string part) => $"news.{n}.{part}";
    }
}
