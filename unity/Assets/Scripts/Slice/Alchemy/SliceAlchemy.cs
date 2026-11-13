using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Slice.Shops;
using GameDev.World.Poi;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Alchemy
{
    /// <summary>The three benches of 20 §3 *Where brewing is done* (SLICE_TRIAL §4).</summary>
    public enum BenchKind
    {
        /// <summary>The back room of Harrow &amp; Tincture: anyone, for a fee, while the shop is open.</summary>
        Harrow,

        /// <summary>The faction's house: members, free.</summary>
        Faction,

        /// <summary>Veinmarket's den in Lower Veil: one who heard of it, at night, at a higher fee.</summary>
        Den,
    }

    /// <summary>How a brewing ended.</summary>
    public enum BrewOutcome
    {
        Refused,

        /// <summary>The world took an ingredient or the fee during the checks: nothing made, nothing spent.</summary>
        Interrupted,
        Brewed,
    }

    /// <summary>
    /// Alchemy in the slice (S5b.4; 20 §3; SLICE_TRIAL §2, §4). One who learned it from Harrow and carries a potion's formula
    /// brews it at a bench from the formula's ingredients: six Reaction Checks of 07.1 §8a on Intelligence and Alchemy against
    /// the potion's difficulty by its Limit, the world paused at each, with brewing's own floor, a good zone of at least 12 %
    /// and a marker of at least 1.0 s, and a perfect zone a quarter of the good one in its middle. The sixth makes the potion,
    /// which keeps its perfect and bad results for its drinking (S5b.5), takes the ingredients and the bench's fee, and grows
    /// Alchemy and Intelligence six times at the stage of the potion's Limit (07.1 §3). Then 6 ticks pass with the world
    /// running, and a fight that begins in them stops them, the potion kept.
    /// </summary>
    public sealed class SliceAlchemy
    {
        public const string Lesson = "alchemy";

        /// <summary>20 §3: a brewing has six Reaction Checks.</summary>
        public const int Checks = 6;

        /// <summary>20 §3: after the sixth check, 6 ticks pass with the world running.</summary>
        public const int BrewTicks = 6;

        /// <summary>20 §3: brewing's own floor, the narrowest good zone, a share of the bar.</summary>
        public const double LeastWidth = 0.12d;

        /// <summary>20 §3: brewing's own floor, the marker's fastest crossing, s.</summary>
        public const double FastestSeconds = 1.0d;

        /// <summary>Authoring defaults, 20 §3 and SLICE_TRIAL §4: each bench's fee, bronze.</summary>
        public const int HarrowFee = 100;
        public const int DenFee = 300;

        /// <summary>Authoring defaults, SLICE_TRIAL §4: the faction's bench from 07:00 to 23:00, the den's from 22:00 to 05:00.</summary>
        public const int FactionOpens = 7;
        public const int FactionCloses = 23;
        public const int DenOpens = 22;
        public const int DenCloses = 5;

        /// <summary>World state: the character is a member of the faction (S5c.4 sets it at Faction Entry).</summary>
        public const string MemberKey = "player.faction.member";

        /// <summary>World state: the character went against Veinmarket in POI B, which shuts the den for good (SLICE_TRIAL §4).</summary>
        public const string AgainstVeinmarketKey = "against.veinmarket";

        /// <summary>World state: how many potions the character has brewed, which keys each brewing's bars.</summary>
        public const string BrewedKey = "player.alchemy.brewed";

        /// <summary>
        /// The den's warehouse (SLICE_TRIAL §4): the Lower Veil warehouse on the quay nearest Tipsy Pier, its door at
        /// world (148.0, 31.5) m (C5's layout; authoring default).
        /// </summary>
        public const string DenDwelling = "dwelling.1480.315";

        /// <summary>The tips that tell of the den (SLICE_TRIAL §4): Kit's word on the street and Dace's.</summary>
        public static readonly string[] DenTips = { "kit.den", "dace.den" };

        /// <summary>20 §3 *Difficulty by Limit*: D by the potion's Limit, 0 to 9 (authoring defaults).</summary>
        private static readonly int[] DifficultyByLimit = { 55, 45, 36, 28, 21, 15, 10, 6, 3, 1 };

        private readonly SliceShops _shops;
        private readonly ItemCatalogue _items;
        private readonly IWorldSeed _seed;
        private readonly IWorldClock _clock;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly ICharacterValues _character;

        public SliceAlchemy(SliceShops shops, ItemCatalogue items, IWorldSeed seed, IWorldClock clock, Bag bag, IWorldState state,
            ICharacterValues character)
        {
            _shops = shops;
            _items = items;
            _seed = seed;
            _clock = clock;
            _bag = bag;
            _state = state;
            _character = character;
        }

        /// <summary>A potion brewed.</summary>
        public event Action<ItemPiece> Brewed;

        public bool Knows => _shops.Knows(Lesson);

        /// <summary>Every Limit potion with a formula, in the catalogue's order.</summary>
        public IEnumerable<ItemDefinition> Potions => _items.Items.Where(i => i.kind == ItemKind.Potion && i.recipe != null && i.recipe.Length > 0);

        /// <summary>The note that teaches a potion: <c>potion_priestess_9</c>'s is <c>formula_priestess_9</c>.</summary>
        public static string FormulaOf(string potion) => "formula_" + (potion.StartsWith("potion_", StringComparison.Ordinal) ? potion.Substring(7) : potion);

        /// <summary>A formula is known while its note is carried (11: a note of weight 0).</summary>
        public bool HasFormula(ItemDefinition potion) => potion != null && _bag.Count(FormulaOf(potion.id)) > 0;

        /// <summary>The potions whose formulas the character carries.</summary>
        public IEnumerable<ItemDefinition> Known => Potions.Where(HasFormula);

        /// <summary>20 §3: D by a potion's Limit, 0 to 9.</summary>
        public static int DifficultyOf(int limit) => DifficultyByLimit[Math.Max(0, Math.Min(9, limit))];

        /// <summary>07.1 §4's Δ of a brewing check: `(Intelligence + Alchemy) / 2 − D`.</summary>
        public double Delta(int limit) => ReactionCheck.Delta(_character.Get(CharacterAttribute.Intelligence), _character.Get(CharacterSkill.Alchemy),
            DifficultyOf(limit));

        /// <summary>
        /// 20 §3: a brewing check's bar at <paramref name="delta"/>: 07.1 §8a's zone and pace held at brewing's own floor, 12 %
        /// and 1.0 s, with a perfect zone a quarter of the good one in its middle, the zone placed by <paramref name="rolls"/>.
        /// </summary>
        public static ReactionBar BarFor(double delta, Random rolls)
        {
            var bar = ReactionCheck.Place(Math.Max(LeastWidth, ReactionCheck.Width(delta)), rolls, Math.Max(FastestSeconds, ReactionCheck.Pace(delta)));
            var inner = bar.GoodWidth * ReactionCheck.PerfectShare;
            return new ReactionBar(bar.GoodStart, bar.GoodWidth, bar.GoodStart + ((bar.GoodWidth - inner) / 2d), inner, bar.Seconds, LeastWidth);
        }

        /// <summary>The bar of a brewing's check <paramref name="check"/>, 0 to 5, placed by the seed and the potions brewed.</summary>
        public ReactionBar Bar(ItemDefinition potion, int check) =>
            BarFor(Delta(potion.stage), _seed.Fork(SliceSeedStreams.Alchemy, _state.Get(BrewedKey), check));

        /// <summary>Authoring default (20 §3): how much each perfect result lowers the Limit's mandatory Sanity cost.</summary>
        public const double PerfectCostShare = 0.10d;

        /// <summary>Authoring default (20 §3): how much each bad result raises it.</summary>
        public const double BadCostShare = 0.25d;

        /// <summary>Authoring default (20 §3): how much of the attribute rise each bad result takes away.</summary>
        public const double BadRiseShare = 0.15d;

        /// <summary>
        /// The Brewing Factor (20 §3; 05 Progression; 05.1: it multiplies the mandatory cost only, never the preparation
        /// penalty): six perfect results 0.40 of the Limit's mandatory Sanity cost, six good ones 1, six bad ones 2.50.
        /// </summary>
        public static double BrewingFactor(int perfect, int bad) => 1d - (PerfectCostShare * perfect) + (BadCostShare * bad);

        /// <summary>20 §3: what the perfect results add to each of the Arcana's primary attributes above the standard rise: 1 for three, 2 for six.</summary>
        public static int PerfectBonus(int perfect) => perfect >= Checks ? 2 : perfect >= Checks / 2 ? 1 : 0;

        /// <summary>20 §3: the share of the standard attribute rise the bad results leave: 0.85 for one, a tenth for six.</summary>
        public static double RiseShare(int bad) => Math.Max(0d, 1d - (BadRiseShare * bad));

        public static int Fee(BenchKind bench) => bench == BenchKind.Harrow ? HarrowFee : bench == BenchKind.Den ? DenFee : 0;

        /// <summary>Whether the bench is open at this hour: Harrow's with the shop, the faction's by day, the den's by night.</summary>
        public bool IsOpen(BenchKind bench)
        {
            var hour = _clock.MinuteOfDay / 60;
            return bench switch
            {
                BenchKind.Harrow => _shops.IsOpen,
                BenchKind.Faction => hour >= FactionOpens && hour < FactionCloses,
                _ => hour >= DenOpens || hour < DenCloses,
            };
        }

        /// <summary>The hour a closed bench opens.</summary>
        public int Opens(BenchKind bench) => bench == BenchKind.Harrow ? _shops.Rules.Opens : bench == BenchKind.Faction ? FactionOpens : DenOpens;

        public bool IsMember => _state.IsSet(MemberKey);

        /// <summary>The den is known once either of its tips is heard (SLICE_TRIAL §4).</summary>
        public bool KnowsDen => DenTips.Any(id => _state.IsSet(TipBook.HeardKeyOf(id)));

        public bool BarredFromDen => _state.IsSet(AgainstVeinmarketKey);

        /// <summary>The key of the reason a bench cannot be used at all now, or null: whose it is, its hours, the lesson.</summary>
        public string BenchFault(BenchKind bench)
        {
            if (bench == BenchKind.Faction && !IsMember)
            {
                return "ui.brew.fault.member";
            }

            if (bench == BenchKind.Den && BarredFromDen)
            {
                return "ui.brew.fault.barred";
            }

            if (bench == BenchKind.Den && !KnowsDen)
            {
                return "ui.brew.fault.unknown";
            }

            if (!IsOpen(bench))
            {
                return "ui.brew.fault.closed";
            }

            return Knows ? null : "ui.brew.fault.lesson";
        }

        /// <summary>The key of the reason a potion cannot be begun at a bench, or null.</summary>
        public string Fault(BenchKind bench, ItemDefinition potion)
        {
            var fault = BenchFault(bench);
            if (fault != null)
            {
                return fault;
            }

            if (potion == null || potion.kind != ItemKind.Potion || potion.recipe == null || potion.recipe.Length == 0)
            {
                return "ui.shop.fault.gone";
            }

            if (!HasFormula(potion))
            {
                return "ui.brew.fault.formula";
            }

            if (_bag.Bronze < Fee(bench))
            {
                return "ui.shop.fault.money";
            }

            if (potion.recipe.Any(p => _bag.Count(p.item) < p.count))
            {
                return "ui.brew.fault.ingredients";
            }

            var change = potion.weight - potion.recipe.Sum(p => (_items.Find(p.item)?.weight ?? 0f) * p.count);
            return change > 0f && !_bag.CanCarry(change) ? "ui.shop.fault.heavy" : null;
        }

        /// <summary>
        /// Settles a brewing by its six checks' <paramref name="results"/>: takes the ingredients and the fee, makes the potion
        /// with its perfect and bad results, and grows Alchemy and Intelligence once a check at the potion's Limit (07.1 §3).
        /// The bench's hours were asked when the brewing began, so one that closes during the checks still makes it.
        /// </summary>
        public BrewOutcome Brew(BenchKind bench, ItemDefinition potion, IReadOnlyList<ReactionResult> results, out ItemPiece made)
        {
            made = null;
            if (potion == null || results == null || results.Count != Checks || potion.recipe == null || potion.recipe.Length == 0)
            {
                return BrewOutcome.Refused;
            }

            if (!HasFormula(potion) || _bag.Bronze < Fee(bench) || potion.recipe.Any(p => _bag.Count(p.item) < p.count))
            {
                return BrewOutcome.Interrupted;
            }

            _bag.Spend(Fee(bench));
            foreach (var part in potion.recipe)
            {
                _bag.Remove(part.item, part.count);
            }

            foreach (var result in results)
            {
                _character.Train(CharacterSkill.Alchemy, potion.stage, Growth.Of(result), new[] { CharacterAttribute.Intelligence });
            }

            _state.Set(BrewedKey, _state.Get(BrewedKey) + 1);
            made = new ItemPiece(potion.id, potion.stage)
            {
                perfect = results.Count(r => r == ReactionResult.Perfect),
                bad = results.Count(r => r == ReactionResult.Bad),
            };
            _bag.AddPiece(made);
            Brewed?.Invoke(made);
            return BrewOutcome.Brewed;
        }

        /// <summary>
        /// The brewing's time after its checks: 6 ticks with the world running, tick by tick. <paramref name="interrupted"/> is
        /// asked after each, and a fight that begins stops the time passing (20 §3). Returns the ticks passed.
        /// </summary>
        public int PassTicks(Func<bool> interrupted)
        {
            for (var t = 0; t < BrewTicks; t++)
            {
                _clock.AdvanceTicks(1);
                if (interrupted != null && interrupted())
                {
                    return t + 1;
                }
            }

            return BrewTicks;
        }
    }
}
