using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.BreakIns
{
    /// <summary>
    /// Break-ins (S5c.5; 10.3 §6.1–6.2, 10.4 §6 *Arrest*, 19 §6.3), with no scene code. A lock is one Reaction Check of
    /// Theft and Dexterity against the Lock, with Lockpicks: a good result opens the door until the next 06:00, a bad one
    /// breaks a pick, and each try trains Theft and Dexterity. A break-in someone notices costs Reputation once a place a
    /// day and makes the character Wanted until the next 08:00, when any Warden who sees it stops it with an Arrest.
    /// What was taken from a Stash is the night's loot, which the Arrest asks back. Everything lives in the world state, so
    /// a save keeps it.
    /// </summary>
    public sealed class SliceBreakIns
    {
        /// <summary>The item a lock is picked with (10.3 §6.1).</summary>
        public const string LockpicksItem = "lockpicks";

        /// <summary>10.3 §6.2: the Wardens look for the character until 08:00 the next morning (authoring default).</summary>
        public const int WantedUntilHour = 8;

        /// <summary>10.3 §6.1: a picked door stays open until morning, read as 06:00 (authoring default).</summary>
        public const int OpenUntilHour = 6;

        /// <summary>World state: whether the character is Wanted, as the tick until which the Wardens look for it.</summary>
        public const string WantedKey = "breakin.wanted";

        /// <summary>World state: a Warden saw the crime itself, so the Arrest's request is Large (10.4 §6).</summary>
        public const string SeenKey = "breakin.wanted.seen";

        /// <summary>World state: the tick until which the night's loot counts, and the bronze taken.</summary>
        public const string LootUntilKey = "breakin.loot.until";

        public const string LootBronzeKey = "breakin.loot.bronze";

        private const string LootItemPrefix = "breakin.loot.item.";

        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly ICharacterValues _character;
        private readonly SocialStanding _standing;
        private readonly IWorldSeed _seed;

        public SliceBreakIns(Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character, SocialStanding standing, IWorldSeed seed)
        {
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _standing = standing;
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
        }

        /// <summary>The character's Conduct Progress (S5b.7): a lock tried is against Император, as a theft is.</summary>
        public GameDev.Slice.Alchemy.SliceConduct Conduct { get; set; }

        /// <summary>Raised when a crime is noticed and the character becomes Wanted, or stays so.</summary>
        public event Action WantedChanged;

        /// <summary>World state: the tick until which the place's door stands open; 0 or past for shut.</summary>
        public static string OpenKey(string place) => $"lock.{place}.open";

        /// <summary>World state: the day, plus one, a break-in at the place last cost Reputation.</summary>
        public static string NoticedKey(string place) => $"breakin.{place}.noticed";

        public static string LootItemKey(string item) => LootItemPrefix + item;

        // ---------------------------------------------------------------- the lock

        public int Picks => _bag.Count(LockpicksItem);

        public bool IsOpen(string place) => _state.Get(OpenKey(place)) > _clock.Tick;

        /// <summary>Opens the place's door until the next 06:00, as a good try or a key does.</summary>
        public void Open(string place) => _state.Set(OpenKey(place), (int)Next(OpenUntilHour));

        /// <summary>07.1 §4: `Δ = (Dexterity + Theft) / 2 − Lock`.</summary>
        public double Delta(int lockValue) =>
            ReactionCheck.Delta(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Theft), lockValue);

        /// <summary>The bar of a try at the place's lock now: its zone and pace by 07.1 §8a, placed by the seed.</summary>
        public ReactionBar Bar(string place, int lockValue) =>
            ReactionCheck.For(Delta(lockValue), _seed.Fork(SliceSeedStreams.Locks, StableHash.Of(place), (int)_clock.Tick));

        /// <summary>
        /// Settles a try by the Reaction Check's <paramref name="result"/>: a good one opens the door, a bad one breaks a
        /// pick. Every try grows Theft and Dexterity, the Lock its difficulty on §1's bands (07.1 §3), and is against
        /// Император. True when the door opened.
        /// </summary>
        public bool Resolve(string place, int lockValue, ReactionResult result)
        {
            Conduct?.Against(Arcana.Emperor);
            _character.Train(CharacterSkill.Theft, Growth.StageOfDifficulty(lockValue), Growth.Of(result), new[] { CharacterAttribute.Dexterity });
            if (result == ReactionResult.Bad)
            {
                _bag.Remove(LockpicksItem);
                return false;
            }

            Open(place);
            return true;
        }

        // ---------------------------------------------------------------- noticed, Wanted

        /// <summary>Whether the Wardens look for the character now.</summary>
        public bool Wanted => _state.Get(WantedKey) > _clock.Tick;

        /// <summary>Whether a Warden saw the crime itself, while the character is Wanted.</summary>
        public bool Seen => Wanted && _state.IsSet(SeenKey);

        /// <summary>
        /// A crime at <paramref name="place"/> was noticed: Reputation falls by the noticed break-in once a place a day
        /// (10.4 §4), and the character is Wanted until the next 08:00; <paramref name="byWarden"/> when a Warden saw it.
        /// </summary>
        public void Noticed(string place, bool byWarden, Deed deed = Deed.BreakInNoticed)
        {
            if (place != null && _state.Get(NoticedKey(place)) != _clock.Day + 1)
            {
                _state.Set(NoticedKey(place), _clock.Day + 1);
                _standing?.Record(deed);
            }

            if (!Wanted)
            {
                _state.Set(SeenKey, 0);
            }

            _state.Set(WantedKey, (int)Next(WantedUntilHour));
            if (byWarden)
            {
                _state.Raise(SeenKey);
            }

            WantedChanged?.Invoke();
        }

        /// <summary>The Arrest is settled: the Wardens look no more.</summary>
        public void ClearWanted()
        {
            _state.Set(WantedKey, 0);
            _state.Set(SeenKey, 0);
            WantedChanged?.Invoke();
        }

        // ---------------------------------------------------------------- loot

        private bool LootLive => _state.Get(LootUntilKey) > _clock.Tick;

        /// <summary>The bronze taken from Stashes this night, until the next 08:00.</summary>
        public int LootBronze => LootLive ? _state.Get(LootBronzeKey) : 0;

        /// <summary>The items taken from Stashes this night, and how many.</summary>
        public IEnumerable<(string Item, int Count)> LootItems()
        {
            if (!LootLive)
            {
                yield break;
            }

            foreach (var key in _state.Keys.Where(k => k.StartsWith(LootItemPrefix, StringComparison.Ordinal)).ToList())
            {
                yield return (key.Substring(LootItemPrefix.Length), _state.Get(key));
            }
        }

        /// <summary>Something taken: <paramref name="bronze"/> and <paramref name="count"/> of <paramref name="item"/> join the night's loot.</summary>
        public void Took(int bronze, string item = null, int count = 0)
        {
            if (!LootLive)
            {
                ForgetLoot();
                _state.Set(LootUntilKey, (int)Next(WantedUntilHour));
            }

            _state.Set(LootBronzeKey, _state.Get(LootBronzeKey) + Math.Max(0, bronze));
            if (!string.IsNullOrEmpty(item) && count > 0)
            {
                _state.Set(LootItemKey(item), _state.Get(LootItemKey(item)) + count);
            }
        }

        /// <summary>
        /// 10.4 §6 *Arrest*, Deception's Argument: nothing stolen on the character. Coins are not told apart, so it holds
        /// while the character carries none of the night's items and less bronze than the night took.
        /// </summary>
        public bool NothingStolenCarried => !LootItems().Any(l => _bag.Count(l.Item) > 0) && (LootBronze == 0 || _bag.Bronze < LootBronze);

        /// <summary>Persuasion's Argument of giving back what was stolen: the character carries all of the night's loot.</summary>
        public bool CanGiveBack => (LootBronze > 0 || LootItems().Any()) && _bag.Bronze >= LootBronze && LootItems().All(l => _bag.Count(l.Item) >= l.Count);

        /// <summary>The night's loot goes back to its owners, as much of it as the character carries.</summary>
        public void GiveBack()
        {
            foreach (var (item, count) in LootItems().ToList())
            {
                _bag.Remove(item, Math.Min(count, _bag.Count(item)));
            }

            _bag.Spend(Math.Min(LootBronze, _bag.Bronze));
            ForgetLoot();
        }

        private void ForgetLoot()
        {
            foreach (var key in _state.Keys.Where(k => k.StartsWith(LootItemPrefix, StringComparison.Ordinal)).ToList())
            {
                _state.Set(key, 0);
            }

            _state.Set(LootBronzeKey, 0);
            _state.Set(LootUntilKey, 0);
        }

        // ---------------------------------------------------------------- the Stash (19 §6.3)

        /// <summary>19 §6.3: a poor, middling and rich Stash's bronze (owner-approved ranges).</summary>
        public static readonly (int Least, int Most)[] StashBronze = { (100, 400), (300, 1000), (1000, 3000) };

        /// <summary>Authoring default of 19 §6.3: one Stash in seven also holds papers.</summary>
        public const int PapersOneIn = 7;

        /// <summary>The kinds of papers a Stash may hold, each a note on the screen (VS-BREAKIN).</summary>
        public const int PaperKinds = 3;

        /// <summary>Authoring default: a household with a trade keeps its goods in the Stash, instead of money, one time in two.</summary>
        public const double TradeGoodsChance = 0.5;

        /// <summary>World state: the Stash at the place was taken; nothing fills again during a run (10.3 §6.1).</summary>
        public static string StashKey(string place) => $"stash.{place}.taken";

        public bool StashTaken(string place) => _state.IsSet(StashKey(place));

        /// <summary>What a Stash holds: bronze, or so many of an item, and papers (0 for none, else their kind).</summary>
        public readonly struct StashContents
        {
            public StashContents(int bronze, string item, int count, int papers)
            {
                Bronze = bronze;
                Item = item;
                Count = count;
                Papers = papers;
            }

            public int Bronze { get; }
            public string Item { get; }
            public int Count { get; }
            public int Papers { get; }
        }

        /// <summary>
        /// The Stash of the place, read from who lives there (19 §6.3): bronze in its wealth's range, or for a household
        /// with a trade (<paramref name="tradeItem"/> at <paramref name="tradePrice"/>) as much of its goods by value; the
        /// same in every look of a run.
        /// </summary>
        public StashContents StashOf(string place, int wealth, string tradeItem = null, int tradePrice = 0)
        {
            var rng = _seed.Fork(SliceSeedStreams.Stash, StableHash.Of(place));
            var (least, most) = StashBronze[Math.Max(0, Math.Min(2, wealth))];
            var bronze = rng.Next(least, most + 1);
            var papers = rng.Next(PapersOneIn) == 0 ? 1 + rng.Next(PaperKinds) : 0;
            if (!string.IsNullOrEmpty(tradeItem) && tradePrice > 0 && rng.NextDouble() < TradeGoodsChance)
            {
                return new StashContents(0, tradeItem, Math.Max(1, bronze / tradePrice), papers);
            }

            return new StashContents(bronze, null, 0, papers);
        }

        /// <summary>Takes the place's Stash into the bag and the night's loot; what it held, or null when taken before.</summary>
        public StashContents? TakeStash(string place, int wealth, string tradeItem = null, int tradePrice = 0)
        {
            if (StashTaken(place))
            {
                return null;
            }

            var held = StashOf(place, wealth, tradeItem, tradePrice);
            _state.Raise(StashKey(place));
            _bag.Earn(held.Bronze);
            if (held.Count > 0)
            {
                _bag.Add(held.Item, held.Count);
            }

            Took(held.Bronze, held.Item, held.Count);
            Conduct?.Against(Arcana.Emperor); // a theft as any (S5b.7)
            return held;
        }

        // ---------------------------------------------------------------- a resident's report (10.3 §6.2)

        private const string ReportPlaceKey = "breakin.report.place";
        private const string ReportXKey = "breakin.report.x";
        private const string ReportZKey = "breakin.report.z";
        private const string ReportTickKey = "breakin.report.tick";

        /// <summary>
        /// A resident who noticed the character inside ran out of the house to the Wardens: the street door at
        /// (<paramref name="doorX"/>, <paramref name="doorZ"/>) decimetres, reported to the nearest Warden once the street is
        /// loaded again.
        /// </summary>
        public void ReportLater(string place, int doorX, int doorZ)
        {
            _state.Set(ReportPlaceKey, StableHash.Of(place) | 1);
            _state.Set(ReportXKey, doorX);
            _state.Set(ReportZKey, doorZ);
            _state.Set(ReportTickKey, (int)_clock.Tick);
        }

        /// <summary>Whether a resident's report waits for a Warden.</summary>
        public bool HasReport => _state.IsSet(ReportPlaceKey);

        /// <summary>Whether the waiting report is of the street door at (<paramref name="doorX"/>, <paramref name="doorZ"/>) decimetres.</summary>
        public bool ReportedAt(int doorX, int doorZ) => HasReport && _state.Get(ReportXKey) == doorX && _state.Get(ReportZKey) == doorZ;

        /// <summary>The report waiting for a Warden, taken once; false when none waits or the character is no longer Wanted.</summary>
        public bool TakeReport(out (float X, float Z) door, out long since)
        {
            door = default;
            since = 0;
            if (!_state.IsSet(ReportPlaceKey))
            {
                return false;
            }

            door = (_state.Get(ReportXKey) / 10f, _state.Get(ReportZKey) / 10f);
            since = _clock.Tick - _state.Get(ReportTickKey);
            _state.Set(ReportPlaceKey, 0);
            return Wanted;
        }

        /// <summary>The first tick at <paramref name="hour"/>:00 after now.</summary>
        public long Next(int hour)
        {
            var at = _clock.Tick - _clock.MinuteOfDay + (hour * 60L);
            return at > _clock.Tick ? at : at + 1440L;
        }
    }
}
