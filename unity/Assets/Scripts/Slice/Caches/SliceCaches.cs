using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using Random = System.Random;

namespace GameDev.Slice.Caches
{
    /// <summary>What a Hidden Cache holds (SLICE_TRIAL §5).</summary>
    public enum CacheGift
    {
        Coins,
        Supplies,
        Jewel,
        Note,
    }

    /// <summary>A cache's contents, rolled from the World Seed and its spot, so a save keeps them.</summary>
    public readonly struct CacheContents
    {
        public CacheContents(CacheGift gift, int bronze, IReadOnlyList<(string Id, int Count)> supplies, ItemPiece jewel, int note)
        {
            Gift = gift;
            Bronze = bronze;
            Supplies = supplies;
            Jewel = jewel;
            Note = note;
        }

        public CacheGift Gift { get; }

        /// <summary>The coins before Luck multiplies them, or what was earned once taken.</summary>
        public int Bronze { get; }

        public IReadOnlyList<(string Id, int Count)> Supplies { get; }

        public ItemPiece Jewel { get; }

        /// <summary>The note's number, 1–<see cref="SliceCaches.Notes"/>, or 0.</summary>
        public int Note { get; }
    }

    /// <summary>
    /// Hidden Caches (owner, 2026-11-13; 11 *Hidden Caches*, SLICE_TRIAL §5; S5c.6), with no scene code. The seed chooses
    /// twenty of the forty spots for a run, at least six outside the walls and at most three in one district, and locks a
    /// third of them. A cache is noticed by a Perception check at its spot's stage, which trains Perception, and Luck a
    /// little when it notices; a locked one opens with Lockpicks by a Reaction Check of Theft and Dexterity, trained as any
    /// lock. Its contents are rolled from the seed and its spot, so a save keeps them, and Luck multiplies the coins.
    /// Authoring defaults.
    /// </summary>
    public sealed class SliceCaches
    {
        public const int Chosen = 20;
        public const int OutsideAtLeast = 6;
        public const int PerDistrictAtMost = 3;

        /// <summary>A third of the twenty, rounded.</summary>
        public const int LockedCount = 7;

        /// <summary>SLICE_TRIAL §5: noticed within 3 m; a failed roll is rolled again only after 10 m away and back.</summary>
        public const float NoticeReach = 3f;

        public const float ReturnDistance = 10f;

        /// <summary>Authoring default, m: within this the character takes or picks a noticed cache.</summary>
        public const float OpenReach = 2f;

        /// <summary>The notes a cache may hold, each a Tip of its own (<c>cache.note.N</c>).</summary>
        public const int Notes = 4;

        /// <summary>The shares of coins, supplies and a jewel; a note is the rest (SLICE_TRIAL §5).</summary>
        public const double CoinsShare = 0.60d;
        public const double SuppliesShare = 0.25d;
        public const double JewelShare = 0.10d;

        /// <summary>A locked cache's note comes with this much more.</summary>
        public const int NoteBronze = 100;

        /// <summary>One time in four a locked cache's jewel is Rare.</summary>
        public const double RareShare = 0.25d;

        private static readonly (int Min, int Max) OpenCoins = (50, 300);
        private static readonly (int Min, int Max) LockedCoins = (150, 600);

        /// <summary>A bandage, an antidote, a splint, lockpicks, or three herbs.</summary>
        public static readonly IReadOnlyList<(string Id, int Count)> SupplyKinds = new[]
        {
            ("bandage", 1), ("antidote", 1), ("splint", 1), ("lockpicks", 1), ("herb", 3),
        };

        public static readonly IReadOnlyList<string> Jewels = new[] { "ring", "necklace", "belt" };

        private readonly IWorldState _state;
        private readonly Bag _bag;
        private readonly ICharacterValues _character;
        private readonly IWorldSeed _seed;
        private readonly ItemCatalogue _items;
        private readonly TipBook _tips;
        private readonly Func<long> _tick;
        private readonly HashSet<int> _chosen;
        private readonly HashSet<int> _locked;

        public SliceCaches(IWorldState state, Bag bag, ICharacterValues character, IWorldSeed seed, ItemCatalogue items, TipBook tips, Func<long> tick)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _seed = seed ?? throw new ArgumentNullException(nameof(seed));
            _items = items;
            _tips = tips;
            _tick = tick ?? (() => 0);
            var chosen = Choose(seed);
            _chosen = new HashSet<int>(chosen);
            _locked = new HashSet<int>(Locked(seed, chosen));
        }

        /// <summary>The twenty spots that hold a cache this run, by number.</summary>
        public IEnumerable<int> ChosenSpots => _chosen.OrderBy(n => n);

        public bool Holds(int number) => _chosen.Contains(number);

        public bool IsLocked(int number) => _locked.Contains(number);

        public static string NoticedKey(int number) => $"cache.{number}.noticed";

        public static string OpenKey(int number) => $"cache.{number}.open";

        public static string TakenKey(int number) => $"cache.{number}.taken";

        public static string NoteTip(int note) => $"cache.note.{note}";

        public static string NoteFoundKey(int note) => $"cache.note.{note}.found";

        public bool Noticed(int number) => _state.IsSet(NoticedKey(number));

        public bool Taken(int number) => _state.IsSet(TakenKey(number));

        /// <summary>Whether a locked cache still needs its lock picked.</summary>
        public bool Shut(int number) => IsLocked(number) && !_state.IsSet(OpenKey(number));

        /// <summary>Lock 2 at stage 9 and 4 at stage 8, a poor and a middling home's (10.3 §6.1, 07.1 §8a's stage difficulties).</summary>
        public static int Lock(int stage) => stage <= 8 ? 4 : 2;

        /// <summary>The notice check's difficulty: 2 at stage 9, as the trial's loose board's, and 4 at stage 8.</summary>
        public static int NoticeDifficulty(int stage) => stage <= 8 ? 4 : 2;

        // ------------------------------------------------------------------------------------------------- the choice

        /// <summary>
        /// The twenty of the run: the forty in the seed's order, each taken while its district holds fewer than three and,
        /// for a spot inside the walls, while fewer than fourteen are inside, so at least six are outside.
        /// </summary>
        public static IReadOnlyList<int> Choose(IWorldSeed seed)
        {
            var order = Shuffled(CacheSpots.All.Select(s => s.Number).ToList(), seed.Fork(SliceSeedStreams.Caches, 1));
            var perDistrict = new Dictionary<string, int>();
            var inside = 0;
            var chosen = new List<int>();
            foreach (var number in order)
            {
                if (chosen.Count == Chosen)
                {
                    break;
                }

                var spot = CacheSpots.Of(number);
                if (!spot.Outside)
                {
                    perDistrict.TryGetValue(spot.District, out var count);
                    if (count >= PerDistrictAtMost || inside >= Chosen - OutsideAtLeast)
                    {
                        continue;
                    }

                    perDistrict[spot.District] = count + 1;
                    inside++;
                }

                chosen.Add(number);
            }

            return chosen;
        }

        /// <summary>A third of the chosen, by the seed.</summary>
        public static IReadOnlyList<int> Locked(IWorldSeed seed, IReadOnlyList<int> chosen) =>
            Shuffled(chosen.OrderBy(n => n).ToList(), seed.Fork(SliceSeedStreams.Caches, 2)).Take(LockedCount).ToList();

        private static List<int> Shuffled(List<int> list, Random rolls)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rolls.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }

        // ----------------------------------------------------------------------------------------------- the contents

        /// <summary>What the cache at <paramref name="number"/> holds this run.</summary>
        public CacheContents ContentsOf(int number) => Contents(_seed, _items, number, IsLocked(number));

        /// <summary>SLICE_TRIAL §5's table, from the stream forked with the spot's number.</summary>
        public static CacheContents Contents(IWorldSeed seed, ItemCatalogue items, int number, bool locked)
        {
            var rolls = seed.Fork(SliceSeedStreams.Caches, 3, number);
            var share = rolls.NextDouble();
            if (share < CoinsShare)
            {
                var range = locked ? LockedCoins : OpenCoins;
                return new CacheContents(CacheGift.Coins, rolls.Next(range.Min, range.Max + 1), Array.Empty<(string, int)>(), null, 0);
            }

            if (share < CoinsShare + SuppliesShare)
            {
                var kinds = SupplyKinds.ToList();
                var supplies = new List<(string Id, int Count)>();
                for (var i = 0; i < (locked ? 2 : 1); i++)
                {
                    var pick = kinds[rolls.Next(kinds.Count)];
                    kinds.Remove(pick);
                    supplies.Add(pick);
                }

                return new CacheContents(CacheGift.Supplies, 0, supplies, null, 0);
            }

            if (share < CoinsShare + SuppliesShare + JewelShare)
            {
                var id = Jewels[rolls.Next(Jewels.Count)];
                var rare = locked && rolls.NextDouble() < RareShare;
                var count = rare ? 3 : rolls.NextDouble() < 0.5d ? 1 : 2;
                var item = items != null ? items.Find(id) : null;
                var jewel = item != null ? ItemMaker.MakeWith(item, 9, count, rolls) : new ItemPiece(id);
                return new CacheContents(CacheGift.Jewel, 0, Array.Empty<(string, int)>(), jewel, 0);
            }

            return new CacheContents(CacheGift.Note, locked ? NoteBronze : 0, Array.Empty<(string, int)>(), null, 1 + rolls.Next(Notes));
        }

        /// <summary>SLICE_TRIAL §5: Luck multiplies the coins by (1 + Luck / 100), as in 19's recovery.</summary>
        public static int WithLuck(int bronze, int luck) => (int)Math.Round(bronze * (1d + (luck / 100d)));

        // -------------------------------------------------------------------------------------------- notice and open

        /// <summary>
        /// One notice check at the spot's stage, a success by one draw from <paramref name="rolls"/> at
        /// <paramref name="chance"/>: it trains Perception, with Intelligence its formula's second attribute, a failure half
        /// (07.1 §3), and a cache noticed gives Luck <see cref="Growth.CacheLuckXp"/>. True when noticed.
        /// </summary>
        public bool TryNotice(int number, double chance, Random rolls)
        {
            if (!Holds(number) || Noticed(number) || Taken(number))
            {
                return false;
            }

            var noticed = rolls.NextDouble() < chance;
            _character.Train(null, CacheSpots.Of(number).Stage, noticed ? CheckOutcome.Normal : CheckOutcome.Failure,
                new[] { CharacterAttribute.Perception }, new[] { CharacterAttribute.Intelligence });
            if (!noticed)
            {
                return false;
            }

            _state.Raise(NoticedKey(number));
            if (_character is CharacterSheet sheet)
            {
                sheet.Gain(CharacterAttribute.Luck, Growth.CacheLuckXp);
            }

            return true;
        }

        public int Picks => _bag.Count(GameDev.Slice.BreakIns.SliceBreakIns.LockpicksItem);

        /// <summary>The bar of a try at the cache's lock now (07.1 §8a), placed by the seed.</summary>
        public ReactionBar Bar(int number, long tick) =>
            ReactionCheck.For(
                ReactionCheck.Delta(_character.Get(CharacterAttribute.Dexterity), _character.Get(CharacterSkill.Theft), Lock(CacheSpots.Of(number).Stage)),
                _seed.Fork(SliceSeedStreams.CacheLocks, number, (int)tick));

        /// <summary>
        /// Settles a try at the cache's lock as any lock: Theft and Dexterity grow by the Lock's difficulty, a bad result
        /// breaks a pick, a good one opens the cache for good. A cache has no owner, so no Arcana's conduct counts it.
        /// </summary>
        public bool Resolve(int number, ReactionResult result)
        {
            var lockValue = Lock(CacheSpots.Of(number).Stage);
            _character.Train(CharacterSkill.Theft, Growth.StageOfDifficulty(lockValue), Growth.Of(result), new[] { CharacterAttribute.Dexterity });
            if (result == ReactionResult.Bad)
            {
                _bag.Remove(GameDev.Slice.BreakIns.SliceBreakIns.LockpicksItem);
                return false;
            }

            _state.Raise(OpenKey(number));
            return true;
        }

        /// <summary>
        /// Takes a noticed, open cache whole: its coins with Luck, its supplies, its jewel, or its note, a Tip heard.
        /// Returns what was taken, the coins as earned, or null when it cannot be taken.
        /// </summary>
        public CacheContents? Take(int number)
        {
            if (!Holds(number) || !Noticed(number) || Taken(number) || Shut(number))
            {
                return null;
            }

            var contents = ContentsOf(number);
            var bronze = WithLuck(contents.Bronze, _character.Get(CharacterAttribute.Luck));
            if (bronze > 0)
            {
                _bag.Earn(bronze);
            }

            foreach (var (id, count) in contents.Supplies)
            {
                _bag.Add(id, count);
            }

            if (contents.Jewel != null)
            {
                _bag.AddPiece(contents.Jewel);
            }

            if (contents.Note > 0)
            {
                _state.Raise(NoteFoundKey(contents.Note));
                _tips?.Tell(NoteTip(contents.Note), _tick());
            }

            _state.Raise(TakenKey(number));
            return new CacheContents(contents.Gift, bronze, contents.Supplies, contents.Jewel, contents.Note);
        }
    }
}
