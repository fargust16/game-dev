using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.News;
using GameDev.World.Seed;

namespace GameDev.Slice.People
{
    /// <summary>
    /// The named people's homes (S5c.5; SLICE_PEOPLE §4, VS-HOURS (f)): each is a dwelling of the class its row names, in
    /// its district or street, chosen by the seed among the doors the loaded city knows, two people never in one. A person
    /// at home or asleep by its hours is there, in the Dwelling scene, where Чужой сон reaches it. Josiah Harrow lives
    /// behind his shop and is not among them.
    /// </summary>
    public static class SliceHomes
    {
        /// <summary>Authoring default, m: how far outside its place's rectangle a home's door may stand (a street's doors face its pavement).</summary>
        public const float Margin = 4f;

        /// <summary>SLICE_PEOPLE §4: who lives where, the place's key in <see cref="SlicePlaces"/>, and the wealth (0 poor, 1 middling, 2 rich).</summary>
        public static readonly (string Person, string Place, int Wealth)[] Table =
        {
            ("edda", "map.sleepy_quarter", 1),
            ("silas", "map.salt_end", 1),
            ("ada", "map.parsons_end", 1),
            ("tobias", "map.lower_veil", 0),
            ("kettle", "map.salt_end", 2),
            ("ilse", "map.parsons_end", 0),
            ("coyle", "map.sleepy_quarter", 1),
            ("lottie", "map.draught_lane", 0),
            ("dace", "map.the_reek", 0),
            (SliceHours.MarketTrader, "map.lower_veil", 0),
        };

        private static readonly SortedDictionary<string, (float X, float Z, int Wealth)> Known = new SortedDictionary<string, (float, float, int)>(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> Homes = new Dictionary<string, string>();
        private static int _resolvedFor = -1;
        private static int _seedOf;

        /// <summary>Forgets every door known, as a new run's first load does.</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Forget()
        {
            Known.Clear();
            Homes.Clear();
            _resolvedFor = -1;
        }

        /// <summary>A home a person could live in: a dwelling's street door at (x, z) metres, of the wealth (0–2).</summary>
        public static void Know(string dwellingId, float x, float z, int wealth)
        {
            if (!Known.ContainsKey(dwellingId))
            {
                Known[dwellingId] = (x, z, wealth);
                _resolvedFor = -1;
            }
        }

        /// <summary>
        /// 19 §6.3: what a household with a trade keeps in its Stash, an item of the catalogue: the Master of Item
        /// Creation her ore, the Master of First Aid her bandages, Veinmarket's contact his picks, the traders their herbs.
        /// </summary>
        public static string TradeOf(string person) => person switch
        {
            "edda" => "iron_ore",
            "ada" => "bandage",
            "dace" => "lockpicks",
            "tobias" => "herb",
            SliceHours.MarketTrader => "herb",
            _ => null,
        };

        /// <summary>Who lives in the dwelling, or null.</summary>
        public static string PersonAt(string dwellingId, IWorldSeed seed)
        {
            Resolve(seed);
            foreach (var pair in Homes)
            {
                if (pair.Value == dwellingId)
                {
                    return pair.Key;
                }
            }

            return null;
        }

        /// <summary>The person's home's dwelling id, or null when the city's doors are not known yet.</summary>
        public static string HomeOf(string person, IWorldSeed seed)
        {
            Resolve(seed);
            return Homes.TryGetValue(person, out var id) ? id : null;
        }

        private static void Resolve(IWorldSeed seed)
        {
            var seedKey = seed != null ? seed.Value : 0;
            if (_resolvedFor == Known.Count && _seedOf == seedKey)
            {
                return;
            }

            _resolvedFor = Known.Count;
            _seedOf = seedKey;
            Homes.Clear();
            if (seed == null)
            {
                return;
            }

            var taken = new HashSet<string>();
            foreach (var (person, place, wealth) in Table)
            {
                var area = SlicePlaces.All.FirstOrDefault(p => p.key == place);
                if (area == null)
                {
                    continue;
                }

                bool In((float X, float Z, int Wealth) d) =>
                    d.X >= area.x0 - Margin && d.X <= area.x1 + Margin && d.Z >= area.z0 - Margin && d.Z <= area.z1 + Margin;

                var near = Known.Where(k => !taken.Contains(k.Key) && In(k.Value)).ToList();
                var match = near.Where(k => k.Value.Wealth == wealth).ToList();
                var candidates = match.Count > 0 ? match : near;
                if (candidates.Count == 0)
                {
                    continue;
                }

                var pick = candidates[seed.Fork(SliceSeedStreams.Residents, StableHash.Of("home." + person)).Next(candidates.Count)].Key;
                taken.Add(pick);
                Homes[person] = pick;
            }
        }
    }
}
