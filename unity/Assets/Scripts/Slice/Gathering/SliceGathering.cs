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
    /// Gathering (S5a.5; 19 §6.4). The character begins at a node with its tool; then each world tick is one cycle, an
    /// action check of 07.1 §4 on the node's attribute and skill against its D: a success gives one unit, a critical two,
    /// a failure none, and every cycle spends 3 % of maximum Stamina. Gathering goes on until the character cancels it,
    /// moves, opens the bag, a beast notices or attacks, the bag can carry no more, Stamina runs out, or the node is
    /// spent; a spent node leaves the world at once, and a herb or flax patch comes back 24 hours later. What is taken
    /// lives in the world state, keyed by the node's place.
    /// </summary>
    public sealed class SliceGathering
    {
        private readonly GatherRules _rules;
        private readonly Bag _bag;
        private readonly IWorldState _state;
        private readonly ICharacterValues _character;
        private readonly ActionCheckRules _checks;
        private readonly IWorldSeed _seed;
        private readonly Dictionary<string, GatherSpot> _spots = new Dictionary<string, GatherSpot>();

        public SliceGathering(GatherRules rules, Bag bag, IWorldState state, IWorldClock clock, ICharacterValues character,
            SliceStamina stamina, ActionCheckRules checks, IWorldSeed seed)
        {
            _rules = rules;
            _bag = bag;
            _state = state;
            _character = character;
            _checks = checks;
            _seed = seed;
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

            return Stamina.Empty ? "ui.gather.fault.tired" : null;
        }

        /// <summary>Begins gathering the node; the key of why it cannot, or null. The first cycle comes at the next tick.</summary>
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

        /// <summary>19 §6.4: a success gives one unit, a critical success two, a failure none.</summary>
        public static int UnitsOf(CheckOutcome outcome) => outcome == CheckOutcome.Critical ? 2 : outcome == CheckOutcome.Normal ? 1 : 0;

        /// <summary>The chances of one cycle at <paramref name="spot"/> for the character as it is.</summary>
        public ActionCheckChances Chances(GatherSpot spot)
        {
            var rule = spot.Rule;
            var preparation = ActionCheck.Preparation(_character.Get(rule.attribute), _character.Get(rule.skill));
            return ActionCheck.Chances(_checks, preparation, rule.difficulty, _character.Get(CharacterAttribute.Perception),
                _character.Get(CharacterAttribute.Luck));
        }

        /// <summary>One cycle at the active node, at <paramref name="tick"/>; the clock's tick does it during play.</summary>
        public CheckOutcome Cycle(long tick)
        {
            var spot = Active;
            if (spot == null)
            {
                return CheckOutcome.Failure;
            }

            var outcome = ActionCheck.Roll(Chances(spot), _seed.Fork(SliceSeedStreams.Gathering, (int)tick, Hash(spot.Id)));
            var units = Math.Min(Left(spot), UnitsOf(outcome));
            if (units > 0 && !_bag.Add(spot.Rule.resource, units))
            {
                Stop("ui.gather.stop.heavy");
                return outcome;
            }

            _state.Set(spot.TakenKey, _state.Get(spot.TakenKey) + units);
            TakenNow += units;
            Stamina.Spend(_rules.CycleStamina);
            Cycled?.Invoke(spot, outcome, units);
            if (Left(spot) <= 0)
            {
                _state.Set(spot.EmptiedKey, (int)tick + 1);
                spot.Shown?.Invoke(false);
                Stop("ui.gather.stop.spent");
            }
            else if (Stamina.Empty)
            {
                Stop("ui.gather.stop.tired");
            }

            return outcome;
        }

        private void OnTick(long tick)
        {
            if (Active != null)
            {
                Cycle(tick);
            }

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
