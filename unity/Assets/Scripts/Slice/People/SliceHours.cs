using System.Collections.Generic;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The named people's hours (SLICE_PEOPLE §4, C5 authoring defaults; 18 *Townsfolk in the slice*): where each one is
    /// at each minute of the day, by its posts. A post is a place where the person stands in a scene, its work post the
    /// one the roster placed it at; any other hour it is out of sight, at home, asleep, or somewhere the slice does not
    /// show (the Brothers' House, the dormitory). No scene code: <see cref="PersonHours"/> reads it.
    /// </summary>
    public static class SliceHours
    {
        /// <summary>The post a Master keeps its shop at, and every person's place of work.</summary>
        public const string Work = "work";

        /// <summary>A table of the Soot &amp; Bell in the evening.</summary>
        public const string Tavern = "tavern";

        /// <summary>Kit Mallow's evening, by the Soot &amp; Bell's street door.</summary>
        public const string TavernDoor = "tavern-door";

        /// <summary>Lottie Wren's day by her tenement's door on Draught Lane; Dace's night by Veinmarket's den.</summary>
        public const string Lane = "lane";

        /// <summary>Dace's afternoon on Fishlip Quay.</summary>
        public const string Quay = "quay";

        /// <summary>Madame Ilse's hour before the old chapel's door.</summary>
        public const string Forecourt = "forecourt";

        /// <summary>The nameless old market trader of Bawling Row's food stall (S5-9), who is no named person.</summary>
        public const string MarketTrader = "market_trader";

        /// <summary>One stretch of a day: from a minute of the day to another, past midnight when it is smaller.</summary>
        public readonly struct Stint
        {
            public Stint(int from, int to, string post, bool asleep = false)
            {
                From = from;
                To = to;
                Post = post;
                Asleep = asleep;
            }

            public int From { get; }

            public int To { get; }

            /// <summary>The post it keeps then; null when it is out of sight.</summary>
            public string Post { get; }

            public bool Asleep { get; }

            public bool Holds(int minute) => From <= To ? minute >= From && minute < To : minute >= From || minute < To;
        }

        private static Stint At(int fromHour, int fromMinute, int toHour, int toMinute, string post) =>
            new Stint((fromHour * 60) + fromMinute, (toHour * 60) + toMinute, post);

        private static Stint Sleep(int fromHour, int fromMinute, int toHour, int toMinute) =>
            new Stint((fromHour * 60) + fromMinute, (toHour * 60) + toMinute, null, true);

        /// <summary>SLICE_PEOPLE §4: each person's day; an hour none covers is spent at home, out of sight.</summary>
        public static readonly IReadOnlyDictionary<string, Stint[]> Days = new Dictionary<string, Stint[]>
        {
            { "nell", new[] { At(10, 0, 2, 0, Work), Sleep(2, 0, 10, 0) } },
            { "harrow", new[] { At(8, 0, 20, 0, Work), Sleep(22, 0, 6, 0) } },
            { "pip", new[] { At(8, 0, 20, 0, Work), At(20, 0, 22, 30, Tavern), Sleep(23, 0, 7, 0) } },
            { "edda", new[] { At(8, 0, 20, 0, Work), Sleep(22, 0, 6, 0) } },
            { "silas", new[] { At(8, 0, 20, 0, Work), At(20, 0, 23, 0, Tavern), Sleep(23, 30, 7, 0) } },
            { "ada", new[] { At(8, 0, 20, 0, Work), Sleep(22, 30, 6, 30) } },
            { "tobias", new[] { At(8, 0, 20, 0, Work), At(20, 0, 22, 0, Tavern), Sleep(22, 30, 6, 30) } },
            { "kettle", new[] { At(9, 0, 19, 0, Work), Sleep(23, 0, 7, 0) } },
            { "ilse", new[] { At(8, 0, 20, 0, Work), At(20, 0, 21, 0, Forecourt), Sleep(23, 0, 7, 0) } },
            { "coyle", new[] { At(6, 0, 18, 0, Work), Sleep(22, 30, 5, 30) } },
            { "ashgrave", new[] { At(7, 0, 23, 0, Work), Sleep(23, 0, 7, 0) } },
            { "lottie", new[] { At(8, 0, 17, 30, Lane), At(18, 0, 22, 0, Tavern), Sleep(22, 30, 7, 0) } },
            { "dace", new[] { At(14, 0, 18, 0, Quay), At(18, 0, 23, 0, Tavern), At(23, 0, 5, 0, Lane), Sleep(5, 0, 13, 0) } },
            { "kit", new[] { At(7, 0, 19, 0, Work), At(19, 0, 21, 0, TavernDoor), Sleep(21, 30, 6, 30) } },
            { "hale", new[] { At(6, 0, 22, 0, Work), Sleep(22, 0, 5, 30) } },
            { MarketTrader, new[] { At(8, 0, 18, 0, Work), Sleep(21, 0, 6, 0) } },
        };

        /// <summary>The posts a person keeps somewhere in the slice, its work's first where it has one.</summary>
        public static IEnumerable<string> PostsOf(string person)
        {
            if (!Days.TryGetValue(person, out var day))
            {
                yield break;
            }

            var seen = new HashSet<string>();
            foreach (var stint in day)
            {
                if (stint.Post != null && seen.Add(stint.Post))
                {
                    yield return stint.Post;
                }
            }
        }

        /// <summary>The post <paramref name="person"/> keeps at <paramref name="minuteOfDay"/>; null when it is out of sight, or keeps no hours.</summary>
        public static string PostAt(string person, int minuteOfDay)
        {
            var stint = StintAt(person, minuteOfDay);
            return stint?.Post;
        }

        /// <summary>Whether it sleeps at that minute, at home.</summary>
        public static bool Asleep(string person, int minuteOfDay) => StintAt(person, minuteOfDay)?.Asleep == true;

        /// <summary>Whether it keeps hours here at all: one who does not stands where its scene placed it.</summary>
        public static bool Keeps(string person) => person != null && Days.ContainsKey(person);

        /// <summary>The hour its work begins, as a shop's closed sign shows it; null for one with no work post.</summary>
        public static int? WorkOpens(string person)
        {
            if (person == null || !Days.TryGetValue(person, out var day))
            {
                return null;
            }

            foreach (var stint in day)
            {
                if (stint.Post == Work)
                {
                    return stint.From / 60;
                }
            }

            return null;
        }

        private static Stint? StintAt(string person, int minuteOfDay)
        {
            if (person == null || !Days.TryGetValue(person, out var day))
            {
                return null;
            }

            var minute = ((minuteOfDay % 1440) + 1440) % 1440;
            foreach (var stint in day)
            {
                if (stint.Holds(minute))
                {
                    return stint;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------------------------------- the walkers at night

        /// <summary>18 *Townsfolk in the slice*: fewer walk at night, from this minute of the day (authoring default, 22:00) …</summary>
        public const int NightFrom = 22 * 60;

        /// <summary>… to this one (06:00).</summary>
        public const int NightTo = 6 * 60;

        /// <summary>Authoring default: one walker in this many walks on through the night; the others are at home.</summary>
        public const int NightWalkersOneIn = 3;

        public static bool IsNight(int minuteOfDay)
        {
            var minute = ((minuteOfDay % 1440) + 1440) % 1440;
            return minute >= NightFrom || minute < NightTo;
        }

        /// <summary>Whether the walker of the route with this seed (<see cref="TownsfolkWalker.SeedOf"/>) is at home at night.</summary>
        public static bool HomeAtNight(uint routeSeed) => (routeSeed % NightWalkersOneIn) != 0;

        /// <summary>
        /// The person who keeps a place's hours for the people sitting there with no route: the Soot &amp; Bell's drinkers
        /// drink while Nell Garrow serves, the old chapel's worshippers stand while Father Ambrose Hale keeps it; null where
        /// no one does.
        /// </summary>
        public static string PlaceKeeper(string sceneName) => sceneName switch
        {
            SliceRoster.Tavern => "nell",
            SliceRoster.Chapel => "hale",
            _ => null,
        };
    }
}
