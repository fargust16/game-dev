using System;
using System.Collections.Generic;
using GameDev.Items;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Gathering
{
    /// <summary>One node in the world: its id, read from where it stands, and its kind's rule. Its view shows or hides it.</summary>
    public sealed class GatherSpot
    {
        public GatherSpot(string id, GatherNodeRule rule)
        {
            Id = id;
            Rule = rule;
        }

        public string Id { get; }

        public GatherNodeRule Rule { get; }

        public string TakenKey => $"node.{Id}.taken";

        /// <summary>World state: the tick it was emptied, plus one; 0 while it stands.</summary>
        public string EmptiedKey => $"node.{Id}.emptied";

        /// <summary>The node's view, told to show (true) or hide (false) it.</summary>
        public Action<bool> Shown { get; set; }
    }

    /// <summary>
    /// Gathering (S5a.5; 19 §6.4). The character begins at a node with its tool; then each swing of its animation is one
    /// cycle, a Reaction Check of 07.1 §8a on the node's attribute and skill against its D at the swing's key frame (the
    /// owner's second direction of 2026-11-11): a perfect result gives two units, a good one one, and a bad one none and
    /// wears the tool three times as much. A herb or flax patch is picked in one go, one check that empties it: a good result
    /// gives what it holds, a perfect one that and one more, a bad one nothing. Every check spends 3 % of maximum Stamina,
    /// and a worn-out tool stays in the bag broken (19 §4.3). Gathering goes on until the character cancels it,
    /// moves, opens the bag, a beast notices or attacks, the bag can carry no more, Stamina runs out, or the node is
    /// spent; a spent node leaves the world at once, and a herb or flax patch comes back 24 hours later. What is taken
    /// lives in the world state, keyed by the node's place.
    /// </summary>
    public sealed class SliceGathering
    {
        /// <summary>Authoring default, 19 §4.3: what a gathering tool holds before it breaks.</summary>
        public const int ToolDurability = 100;

        /// <summary>Authoring default, 19 §4.3: the wear of a good or perfect cycle, and of a bad one.</summary>
        public const int WearGood = 1;
        public const int WearBad = 3;

        /// <summary>A broken tool's catalogue id: the tool's, with this after it.</summary>
        public const string Broken = "_broken";

        private readonly GatherRules _rules;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly ICharacterValues _character;
        private readonly IWorldSeed _seed;
        private readonly IWorldClock _clock;
        private readonly Dictionary<string, GatherSpot> _spots = new Dictionary<string, GatherSpot>();
        private int _strokes;

        public SliceGathering(GatherRules rules, Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character,
            SliceStamina stamina, IWorldSeed seed)
        {
            _rules = rules;
            _bag = bag;
            _state = state;
            _character = character;
            _seed = seed;
            _clock = clock;
            Stamina = stamina;
            clock.TickBoundary += OnTick;
        }

        /// <summary>A cycle done: the node, its outcome, and the units it gave.</summary>
        public event Action<GatherSpot, CheckOutcome, int> Cycled;

        public GatherRules Rules => _rules;

        public SliceStamina Stamina { get; }

        /// <summary>The node being gathered, or null.</summary>
        public GatherSpot Active { get; private set; }

        /// <summary>The units taken since this gathering began.</summary>
        public int TakenNow { get; private set; }

        /// <summary>The key of why the last gathering ended, or null when the character simply walked off.</summary>
        public string Stopped { get; private set; }

        public GatherSpot StoppedAt { get; private set; }

        /// <summary>How many times gathering has ended, so a view can tell a new end from an old one.</summary>
        public int Stops { get; private set; }

        /// <summary>World state: the wear on the character's tool of kind <paramref name="tool"/> in use.</summary>
        public static string WearKey(string tool) => $"tool.{tool}.wear";

        public int Wear(string tool) => _state.Get(WearKey(tool));

        public IEnumerable<GatherSpot> Spots => _spots.Values;

        /// <summary>The node <paramref name="id"/>, made the first time a scene shows it.</summary>
        public GatherSpot Spot(string id, GatherNodeRule rule)
        {
            if (!_spots.TryGetValue(id, out var spot))
            {
                spot = new GatherSpot(id, rule);
                _spots[id] = spot;
            }

            return spot;
        }

        public int Left(GatherSpot spot) => Math.Max(0, spot.Rule.units - _state.Get(spot.TakenKey));

        public bool HasTool(GatherSpot spot) => string.IsNullOrEmpty(spot.Rule.tool) || _bag.Count(spot.Rule.tool) > 0;

        /// <summary>The key of why the node cannot be gathered now, or null.</summary>
        public string Fault(GatherSpot spot)
        {
            if (Left(spot) <= 0)
            {
                return "ui.gather.fault.spent";
            }

            if (!HasTool(spot))
            {
                return "ui.gather.fault.tool";
            }

            return Stamina.Tired ? "ui.gather.fault.tired" : null;
        }

        /// <summary>Begins gathering the node; the key of why it cannot, or null. The first swing begins at once.</summary>
        public string Begin(GatherSpot spot)
        {
            var fault = Fault(spot);
            if (fault != null)
            {
                return fault;
            }

            Stop(null);
            Active = spot;
            TakenNow = 0;
            Stamina.Exerting = true;
            return null;
        }

        /// <summary>Ends the gathering, for the reason <paramref name="reason"/> (a key), if any is going on.</summary>
        public void Stop(string reason)
        {
            if (Active == null)
            {
                return;
            }

            StoppedAt = Active;
            Stopped = reason;
            Active = null;
            Stamina.Exerting = false;
            Stops++;
        }

        /// <summary>19 §6.4: a perfect result gives two units, a good one one, a bad one none.</summary>
        public static int UnitsOf(CheckOutcome outcome) => outcome == CheckOutcome.Critical ? 2 : outcome == CheckOutcome.Normal ? 1 : 0;

        /// <summary>07.1 §8a: a Reaction Check's results are §8's outcomes.</summary>
        public static CheckOutcome OutcomeOf(ReactionResult result) =>
            result == ReactionResult.Perfect ? CheckOutcome.Critical : result == ReactionResult.Good ? CheckOutcome.Normal : CheckOutcome.Failure;

        /// <summary>The Reaction Check's Δ at <paramref name="spot"/> for the character as it is (07.1 §8a).</summary>
        public double Delta(GatherSpot spot) =>
            ReactionCheck.Delta(_character.Get(spot.Rule.attribute), _character.Get(spot.Rule.skill), spot.Rule.difficulty);

        /// <summary>The bar of the active node's check keyed by <paramref name="tick"/>: its zones and pace by Δ, placed by the seed.</summary>
        public ReactionBar Bar(long tick)
        {
            var spot = Active;
            return spot == null
                ? default
                : ReactionCheck.For(Delta(spot), _seed.Fork(SliceSeedStreams.Gathering, (int)tick, Hash(spot.Id)), perfect: true);
        }

        /// <summary>The bar of the active node's next check: several come in a tick, each placed afresh by the seed.</summary>
        public ReactionBar NextBar() => Bar((_clock.Tick * 64L) + (_strokes++ & 63));

        /// <summary>Settles the active node's swing by its Reaction Check's <paramref name="result"/>, now.</summary>
        public CheckOutcome Cycle(ReactionResult result) => Cycle(_clock.Tick, result);

        /// <summary>
        /// Settles the active herb or flax patch in one go by its Reaction Check's <paramref name="result"/> (19 §6.4, the
        /// owner's second direction of 2026-11-11): a good result gives what it holds, a perfect one that and one more, a bad
        /// one nothing, and the patch is empty whatever came of it, its check costing a cycle's Stamina. A bag that cannot
        /// carry the units ends the gathering with the patch as it was.
        /// </summary>
        public CheckOutcome Harvest(ReactionResult result)
        {
            var spot = Active;
            if (spot == null)
            {
                return CheckOutcome.Failure;
            }

            var outcome = OutcomeOf(result);
            var left = Left(spot);
            var units = outcome == CheckOutcome.Critical ? left + 1 : outcome == CheckOutcome.Normal ? left : 0;
            if (units > 0 && !_bag.Add(spot.Rule.resource, units))
            {
                Stop("ui.gather.stop.heavy");
                return outcome;
            }

            _state.Set(spot.TakenKey, spot.Rule.units);
            TakenNow += units;
            Stamina.Spend(_rules.CycleStamina);
            Cycled?.Invoke(spot, outcome, units);
            _state.Set(spot.EmptiedKey, (int)_clock.Tick + 1);
            spot.Shown?.Invoke(false);
            Stop(units > 0 ? "ui.gather.stop.picked" : "ui.gather.stop.lost");
            return outcome;
        }

        /// <summary>
        /// Settles the active node's cycle at <paramref name="tick"/> by its Reaction Check's <paramref name="result"/>:
        /// the units it gives, the tool's wear and the Stamina it spends.
        /// </summary>
        public CheckOutcome Cycle(long tick, ReactionResult result)
        {
            var spot = Active;
            if (spot == null)
            {
                return CheckOutcome.Failure;
            }

            var outcome = OutcomeOf(result);
            var units = Math.Min(Left(spot), UnitsOf(outcome));
            if (units > 0 && !_bag.Add(spot.Rule.resource, units))
            {
                Stop("ui.gather.stop.heavy");
                return outcome;
            }

            _state.Set(spot.TakenKey, _state.Get(spot.TakenKey) + units);
            TakenNow += units;
            Stamina.Spend(_rules.CycleStamina);
            var broke = WearTool(spot.Rule.tool, outcome == CheckOutcome.Failure ? WearBad : WearGood);
            Cycled?.Invoke(spot, outcome, units);
            if (Left(spot) <= 0)
            {
                _state.Set(spot.EmptiedKey, (int)tick + 1);
                spot.Shown?.Invoke(false);
                Stop("ui.gather.stop.spent");
            }
            else if (broke && !HasTool(spot))
            {
                Stop("ui.gather.stop.broken");
            }
            else if (Stamina.Empty)
            {
                Stop("ui.gather.stop.tired");
            }

            return outcome;
        }

        /// <summary>
        /// 19 §4.3: wears the tool of kind <paramref name="tool"/> in use by <paramref name="wear"/>; one worn out stays in
        /// the bag as a broken tool, and the next one begins unworn. True when it broke.
        /// </summary>
        public bool WearTool(string tool, int wear)
        {
            if (string.IsNullOrEmpty(tool) || _bag.Count(tool) <= 0)
            {
                return false;
            }

            var worn = Wear(tool) + wear;
            if (worn < ToolDurability)
            {
                _state.Set(WearKey(tool), worn);
                return false;
            }

            _state.Set(WearKey(tool), 0);
            _bag.Remove(tool);
            _bag.Add(tool + Broken);
            return true;
        }

        private void OnTick(long tick)
        {
            foreach (var spot in _spots.Values)
            {
                var emptied = _state.Get(spot.EmptiedKey);
                if (emptied > 0 && spot.Rule.returnsAfterHours > 0 && tick >= emptied - 1 + (spot.Rule.returnsAfterHours * 60L))
                {
                    _state.Set(spot.TakenKey, 0);
                    _state.Set(spot.EmptiedKey, 0);
                    spot.Shown?.Invoke(true);
                }
            }
        }

        /// <summary>FNV-1a of the node's id: the same on every machine and run, unlike string.GetHashCode.</summary>
        private static int Hash(string id)
        {
            unchecked
            {
                var h = 2166136261u;
                foreach (var c in id)
                {
                    h = (h ^ c) * 16777619u;
                }

                return (int)(h & 0x7fffffff);
            }
        }
    }
}
