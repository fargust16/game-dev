using System;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.Mind;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Alchemy
{
    /// <summary>
    /// Drinking a Limit potion and its Assimilation (S5b.5; 20 §3 *Drinking a Limit potion*; 05.1 *What a Limit grants*,
    /// *Requirements and Sanity in the slice*; 05 *No skipping*). An Uninitiated character drinks a Limit 9 potion of either
    /// Arcana and enters it; one at Limit 9 drinks its own Arcana's Limit 8 potion once its Conduct Progress holds 8 points;
    /// V1's player stops at Limit 8. Drinking starts 60 ticks of Assimilation; when they are spent the new Limit, its rise
    /// of the Arcana's attributes, its abilities and its Sanity cost take effect together. Everything is kept in the world
    /// state, so a save keeps it, and a save is refused while a potion settles.
    /// </summary>
    public sealed class SliceAdvancement
    {
        /// <summary>20 §3 (owner, 2026-11-13): the ticks a drunk Limit potion takes to settle.</summary>
        public const int AssimilationTicks = 60;

        /// <summary>05.1 *Conduct Progress in the slice*: the points Limit 8 needs.</summary>
        public const int ConductForLimit8 = 8;

        /// <summary>05 *V1 Limit scope*: the deepest Limit V1's player reaches.</summary>
        public const int DeepestLimit = 8;

        /// <summary>World state key of the Conduct Progress points toward the next Limit (S5b.7 earns them).</summary>
        public const string ConductKey = "player.conduct";

        // World state keys of a potion settling: the tick its Assimilation ends, and what it was.
        public const string UntilKey = "player.assimilation.until";
        public const string ArcanaKey = "player.assimilation.arcana";
        public const string LimitKey = "player.assimilation.limit";
        public const string PerfectKey = "player.assimilation.perfect";
        public const string BadKey = "player.assimilation.bad";

        // 05.1 *What a Limit grants* (owner, 2026-11-02): each primary attribute's and the secondary's rise, by target Limit 9 … 0.
        private static readonly int[] PrimaryRise = { 3, 4, 5, 6, 7, 8, 8, 9, 9, 10 };
        private static readonly int[] SecondaryRise = { 1, 2, 2, 2, 2, 3, 3, 3, 3, 3 };

        // 05.1 *Requirements and Sanity in the slice* (authoring defaults, 2026-11-04): Limits 9 and 8.
        private const int PrimaryNeed9 = 3, SecondaryNeed9 = 2, Cost9 = 10;
        private const int PrimaryNeed8 = 7, SecondaryNeed8 = 4, Cost8 = 15;
        private const int PrimaryShortCost = 2, SecondaryShortCost = 1;

        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly Bag _bag;
        private readonly CharacterSheet _character;
        private readonly SliceSanity _sanity;
        private readonly SocialStanding _standing;
        private readonly Func<bool> _inFight;

        public SliceAdvancement(IWorldState state, IWorldClock clock, Bag bag, CharacterSheet character, SliceSanity sanity,
            SocialStanding standing = null, Func<bool> inFight = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _bag = bag ?? throw new ArgumentNullException(nameof(bag));
            _character = character ?? throw new ArgumentNullException(nameof(character));
            _sanity = sanity ?? throw new ArgumentNullException(nameof(sanity));
            _standing = standing;
            _inFight = inFight ?? (() => false);
        }

        /// <summary>A potion settled: the Arcana and the Limit the character reached, and the Sanity it cost.</summary>
        public event Action<Arcana, int, int> Advanced;

        public Arcana CurrentArcana => (Arcana)Math.Max(0, Math.Min((int)Arcana.Emperor, _state.Get(SliceConfront.ArcanaKey)));

        /// <summary>The character's Limit; null while it is Uninitiated.</summary>
        public int? CurrentLimit => CurrentArcana == Arcana.None ? (int?)null : _state.Get(SliceConfront.LimitKey);

        /// <summary>Whether a drunk potion is settling.</summary>
        public bool Assimilating => _state.IsSet(UntilKey) && _clock.Tick < _state.Get(UntilKey);

        /// <summary>The ticks of Assimilation left; 0 when none.</summary>
        public int AssimilationLeft => _state.IsSet(UntilKey) ? (int)Math.Max(0L, _state.Get(UntilKey) - _clock.Tick) : 0;

        public int Conduct => _state.Get(ConductKey);

        /// <summary>05.1: the Arcana's two primary attributes and its secondary one.</summary>
        public static (CharacterAttribute First, CharacterAttribute Second, CharacterAttribute Secondary) Profile(Arcana arcana) =>
            arcana == Arcana.Emperor
                ? (CharacterAttribute.Will, CharacterAttribute.Strength, CharacterAttribute.Endurance)
                : (CharacterAttribute.Perception, CharacterAttribute.Intelligence, CharacterAttribute.Will);

        /// <summary>05.1: each primary attribute's rise into <paramref name="limit"/>.</summary>
        public static int PrimaryRiseOf(int limit) => PrimaryRise[9 - Math.Max(0, Math.Min(9, limit))];

        public static int SecondaryRiseOf(int limit) => SecondaryRise[9 - Math.Max(0, Math.Min(9, limit))];

        /// <summary>05.1: the Limit's mandatory Sanity cost, before the Brewing Factor.</summary>
        public static int MandatoryCost(int limit) => limit == 9 ? Cost9 : Cost8;

        /// <summary>
        /// 05.1: the Preparation Penalty read before drinking: 2 Sanity for each point a primary attribute falls short of the
        /// Limit's need, 1 for each point of the secondary.
        /// </summary>
        public int PreparationPenalty(Arcana arcana, int limit)
        {
            var (a, b, s) = Profile(arcana);
            var primaryNeed = limit == 9 ? PrimaryNeed9 : PrimaryNeed8;
            var secondaryNeed = limit == 9 ? SecondaryNeed9 : SecondaryNeed8;
            return (PrimaryShortCost * (Short(a, primaryNeed) + Short(b, primaryNeed))) + (SecondaryShortCost * Short(s, secondaryNeed));
        }

        private int Short(CharacterAttribute attribute, int need) => Math.Max(0, need - _character.Get(attribute));

        /// <summary>
        /// 05.1: `Mandatory_Transition_Cost × Brewing_Factor + Preparation_Penalty`, rounded to whole points; the Brewing
        /// Factor multiplies the mandatory cost only.
        /// </summary>
        public int SanityCost(Arcana arcana, int limit, int perfect, int bad) =>
            (int)Math.Round((MandatoryCost(limit) * SliceAlchemy.BrewingFactor(perfect, bad)) + PreparationPenalty(arcana, limit), MidpointRounding.AwayFromZero);

        /// <summary>The key of the reason <paramref name="piece"/> cannot be drunk now, or null when it can.</summary>
        public string Fault(ItemPiece piece, ItemDefinition potion)
        {
            if (piece == null || potion == null || potion.kind != ItemKind.Potion || potion.arcana == Arcana.None)
            {
                return "ui.drink.fault.potion";
            }

            if (_inFight())
            {
                return "ui.drink.fault.fight";
            }

            if (Assimilating)
            {
                return "ui.drink.fault.settling";
            }

            var arcana = CurrentArcana;
            var next = arcana == Arcana.None ? 9 : _state.Get(SliceConfront.LimitKey) - 1;
            if (arcana != Arcana.None && potion.arcana != arcana)
            {
                return "ui.drink.fault.arcana";
            }

            if (next < DeepestLimit || potion.stage != next)
            {
                return "ui.drink.fault.order";
            }

            return next == 8 && Conduct < ConductForLimit8 ? "ui.drink.fault.ready" : null;
        }

        /// <summary>Drinks <paramref name="piece"/>: it leaves the bag and its 60 ticks of Assimilation begin. False when it cannot.</summary>
        public bool Drink(ItemPiece piece, ItemDefinition potion)
        {
            if (Fault(piece, potion) != null || !_bag.RemovePiece(piece))
            {
                return false;
            }

            _state.Set(UntilKey, (int)(_clock.Tick + AssimilationTicks));
            _state.Set(ArcanaKey, (int)potion.arcana);
            _state.Set(LimitKey, potion.stage);
            _state.Set(PerfectKey, piece.perfect);
            _state.Set(BadKey, piece.bad);
            return true;
        }

        /// <summary>
        /// Called each tick: once a potion's Assimilation is spent, the character enters its Arcana at its Limit, the
        /// Arcana's attributes rise by 05.1's curve, the perfect results adding to the primary ones and the bad ones taking
        /// a share of the rise away (20 §3), the Sanity cost is paid, and the Limit is counted in the Deeds Record. True when
        /// a potion settled now.
        /// </summary>
        public bool Settle()
        {
            // A potion whose ticks run out in a fight settles on the first tick after it, so a fighter never changes mid-fight.
            if (!_state.IsSet(UntilKey) || _clock.Tick < _state.Get(UntilKey) || _inFight())
            {
                return false;
            }

            var arcana = (Arcana)_state.Get(ArcanaKey);
            var limit = _state.Get(LimitKey);
            var perfect = _state.Get(PerfectKey);
            var bad = _state.Get(BadKey);
            foreach (var key in new[] { UntilKey, ArcanaKey, LimitKey, PerfectKey, BadKey })
            {
                _state.Set(key, 0);
            }

            if (arcana == Arcana.None)
            {
                return false;
            }

            var cost = SanityCost(arcana, limit, perfect, bad);
            var share = SliceAlchemy.RiseShare(bad);
            var primary = (int)Math.Round(PrimaryRiseOf(limit) * share, MidpointRounding.AwayFromZero) + SliceAlchemy.PerfectBonus(perfect);
            var secondary = (int)Math.Round(SecondaryRiseOf(limit) * share, MidpointRounding.AwayFromZero);
            var (a, b, s) = Profile(arcana);
            _character.Set(a, _character.Get(a) + primary);
            _character.Set(b, _character.Get(b) + primary);
            _character.Set(s, _character.Get(s) + secondary);

            var entering = CurrentArcana != arcana;
            _state.Set(SliceConfront.ArcanaKey, (int)arcana);
            _state.Set(SliceConfront.LimitKey, limit);
            if (entering)
            {
                _state.Set(SliceConfront.PreparedKey, 0); // a new Arcana prepares its first abilities
                _state.Set(SliceConfront.WillPowerLostKey, 0); // and comes into its full Will Power (07.2 §3.3)
            }

            _state.Set(ConductKey, 0); // Conduct Progress counts toward the next Limit afresh (05.1)
            _sanity.Lose(cost);
            _standing?.RecordLimitReached();
            Advanced?.Invoke(arcana, limit, cost);
            return true;
        }
    }
}
