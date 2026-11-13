using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.World.Poi;
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
    /// Since S5c.7 it also holds POI A's and B's <see cref="Events"/> (SLICE_TRIAL §6), told by the board, the bartender
    /// and Street Rumour, each in its own words: one that lasts is told in the present tense while it lasts, an outcome
    /// in the past tense for 8 hours from the tick it happened. Every item told is heard, and a heard item stays in the
    /// Character Notes (09 reading 5).
    /// </summary>
    public sealed class SliceNews
    {
        /// <summary>One of the slice's POI events the pool tells (SLICE_TRIAL §6).</summary>
        public sealed class Event
        {
            public Event(string id, string key, bool present, params TipSource[] tellers)
            {
                Id = id;
                Key = key;
                Present = present;
                Tellers = tellers;
            }

            /// <summary>Its words are <c>news.&lt;id&gt;.&lt;teller&gt;</c> and its note <c>news.&lt;id&gt;.note</c>.</summary>
            public string Id { get; }

            /// <summary>Present: its POI's phase key, the event lasting while the POI is open. Past: the Aftermath key that makes it.</summary>
            public string Key { get; }

            /// <summary>Told in the present tense while it lasts, and not after: its outcome is an item of its own.</summary>
            public bool Present { get; }

            public IReadOnlyList<TipSource> Tellers { get; }
        }

        /// <summary>SLICE_TRIAL §6's table: POI A's and B's items and who tells each.</summary>
        public static readonly IReadOnlyList<Event> Events = new[]
        {
            new Event("a.arrest", "poi.a.phase", true, TipSource.NoticeBoard, TipSource.Bartender, TipSource.StreetRumour),
            new Event("a.sealed", "aftermath.harrow.shop_sealed", false, TipSource.NoticeBoard, TipSource.Bartender, TipSource.StreetRumour),
            new Event("a.defended", "aftermath.harrow.defended", false, TipSource.Bartender, TipSource.StreetRumour),
            new Event("a.gone", "aftermath.harrow.vanished", false, TipSource.NoticeBoard, TipSource.Bartender, TipSource.StreetRumour),
            new Event("b.snatch", "poi.b.phase", true, TipSource.StreetRumour),
            new Event("b.freed", "aftermath.loom_lane.girls_freed", false, TipSource.Bartender, TipSource.StreetRumour),
            new Event("b.taken", "aftermath.loom_lane.girls_taken", false, TipSource.NoticeBoard, TipSource.Bartender, TipSource.StreetRumour),
            new Event("b.spoiled", "aftermath.loom_lane.snatch_spoiled", false, TipSource.Bartender, TipSource.StreetRumour),
        };

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

        /// <summary>A news item was heard for the first time; its note.</summary>
        public event Action<string> Heard;

        /// <summary>
        /// The next place in the order things are heard, shared with the heard Tips so the Character Notes keep one
        /// order (set by the world: <see cref="TipBook.TakePlace"/>); the pool's own count when unset.
        /// </summary>
        public Func<int> Place { get; set; }

        /// <summary>Puts every outcome whose Aftermath key has been raised into the pool, entering now; the world calls it each tick.</summary>
        public void Notice(long tick)
        {
            foreach (var e in Events)
            {
                if (!e.Present && _state.IsSet(e.Key) && !_state.IsSet(EnteredKey(e)))
                {
                    _state.Set(EnteredKey(e), (int)tick + 1);
                }
            }
        }

        /// <summary>
        /// What <paramref name="teller"/> tells now, the newest first, a lasting event before them, each a line that
        /// follows the language; every item told is heard. The board also tells the deaths. An item is left out
        /// while <paramref name="toldByTip"/> says the teller already says a tip of it (<see cref="ToldBy"/>), a lasting one too.
        /// </summary>
        public IReadOnlyList<Func<string>> Tell(TipSource teller, long tick, Func<string, bool> toldByTip = null)
        {
            Notice(tick);
            var told = new List<(long At, Func<string> Line)>();
            foreach (var e in Events)
            {
                if (!e.Tellers.Contains(teller))
                {
                    continue;
                }

                long at;
                if (e.Present)
                {
                    if (_state.Get(e.Key) != (int)PoiPhase.Open || (toldByTip != null && toldByTip(e.Key)))
                    {
                        continue;
                    }

                    at = long.MaxValue;
                }
                else
                {
                    at = _state.Get(EnteredKey(e)) - 1L;
                    if (at < 0 || tick < at || tick >= at + StaysTicks || (toldByTip != null && toldByTip(e.Key)))
                    {
                        continue;
                    }
                }

                Hear(EventHeardKey(e), EventNoteKey(e));
                var key = LineKey(e, teller);
                told.Add((at, () => Loc.Get(key)));
            }

            if (teller == TipSource.NoticeBoard)
            {
                foreach (var item in Told(tick))
                {
                    var i = item;
                    Hear(Key(i.Index, "heard"), null, i);
                    told.Add((i.Enters, () => Line(i, tick)));
                }
            }

            return told.OrderByDescending(t => t.At).Select(t => t.Line).ToList();
        }

        /// <summary>Every heard item's note with its place in the hearing order, for the Character Notes (09 reading 5).</summary>
        public IEnumerable<(int Place, string Line)> Notes()
        {
            foreach (var e in Events)
            {
                var place = _state.Get(EventHeardKey(e));
                if (place > 0)
                {
                    yield return (place, Loc.Get(EventNoteKey(e)));
                }
            }

            for (var n = 1; n <= Count; n++)
            {
                var place = _state.Get(Key(n, "heard"));
                if (place > 0)
                {
                    yield return (place, Note(Get(n)));
                }
            }
        }

        /// <summary>Whether the event <paramref name="id"/> has been heard.</summary>
        public bool HasHeard(string id) => _state.IsSet($"news.ev.{id}.heard");

        /// <summary>A heard death's note: who, where, and the day and hour it happened.</summary>
        public static string Note(Item item)
        {
            var minute = item.Happened % 1440;
            var hour = (int)Math.Round(minute / 60d, MidpointRounding.AwayFromZero) % 24;
            return Loc.Format("news.death.note", Loc.Get(VictimKey(item.Victim)), SlicePlaces.Name(item.Place), (item.Happened / 1440) + 1,
                hour.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + ":00");
        }

        public static string LineKey(Event e, TipSource teller) => $"news.{e.Id}.{TellerName(teller)}";

        /// <summary>
        /// Whether one of the tips <paramref name="said"/> already tells the item of world key <paramref name="key"/>: a tip
        /// of that outcome, or a tip of the POI whose scene the item tells while it lasts (Nell's tip of the arrest tonight).
        /// </summary>
        public static bool ToldBy(IEnumerable<Tip> said, string key) =>
            said != null && said.Any(t => t != null && (t.requiresKey == key || (t.poi != null && key == "poi." + t.poi.Id + ".phase")));

        public static string EventNoteKey(Event e) => $"news.{e.Id}.note";

        private static string TellerName(TipSource teller) => teller switch
        {
            TipSource.NoticeBoard => "board",
            TipSource.Bartender => "nell",
            _ => "kit",
        };

        private void Hear(string heardKey, string noteKey, Item? death = null)
        {
            if (_state.IsSet(heardKey))
            {
                return;
            }

            int place;
            if (Place != null)
            {
                place = Place();
            }
            else
            {
                place = _state.Get(OwnPlaceKey) + 1;
                _state.Set(OwnPlaceKey, place);
            }

            _state.Set(heardKey, place);
            Heard?.Invoke(death.HasValue ? Note(death.Value) : Loc.Get(noteKey));
        }

        private static string EnteredKey(Event e) => $"news.ev.{e.Id}.enters";

        private static string EventHeardKey(Event e) => $"news.ev.{e.Id}.heard";

        private const string OwnPlaceKey = "news.heard";

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
