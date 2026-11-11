using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Player.Progression;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// The fight's turns on the Combat Grid (S3a.4; 17 C.5), on the rules of <see cref="FightRules"/>. Initiative is rolled
    /// once, as 10.2 §5b sets it, and a combatant who joins later rolls its own. Each turn gets a fresh AP pool, the AP
    /// kept from the last one expiring; a step to a cell beside costs 1 AP, a blow its weapon's AP, and what is left
    /// stays for reactions. A melee blow goes across a cell's side, a shot within the weapon's range and in sight. A
    /// combatant with no will of its own plays its turn by Target Rules V0 (S3a.8, <c>TurnEconomy.Npc.cs</c>). Since S3a.6 (10.2 §5b *Position*, *Wait and Defensive Stance*): leaving the side of a melee enemy draws
    /// its Reaction Attack, once a round from each enemy; Surrounded, higher ground, cover and the Point-Blank Shot set a
    /// blow's or a shot's context; a combatant can wait to the end of the round, end its turn in a Defensive Stance that
    /// answers a melee attack with a Counterattack, or change its Weapon Set. Since S3a.7 (10.1, 10.2): an Environment
    /// Action crosses a tagged edge for its AP and check, a failed climb falls; Bleeding and Poison from a blow tick at
    /// each round's end; a broken leg takes no step; and a bandage, an antidote or a splint ends its condition.
    /// </summary>
    public sealed partial class TurnEconomy : ITurnEconomy
    {
        // A combatant whose own cell is taken or unwalkable takes the nearest free one this many cells away (authoring default).
        private const int PlaceRing = 2;
        private static readonly int[] SideX = { 1, -1, 0, 0 };
        private static readonly int[] SideZ = { 0, 0, 1, -1 };

        private readonly FightRules _rules;
        private readonly ICombatGrid _grid;
        private readonly List<Combatant> _order = new List<Combatant>();
        private readonly List<Combatant> _joining = new List<Combatant>();
        private readonly List<CombatCell> _path = new List<CombatCell>();
        private readonly List<CombatCell> _candidate = new List<CombatCell>();
        private readonly List<CombatCell> _best = new List<CombatCell>();
        private readonly List<CombatCell> _walked = new List<CombatCell>();
        private readonly List<Combatant> _waiting = new List<Combatant>();
        private Random _rolls;
        private int _turn;
        private int _turnsThisRound;

        public TurnEconomy(FightRules rules, ICombatGrid grid)
        {
            _rules = rules;
            _grid = grid;
        }

        public bool IsRunning { get; private set; }

        public int Round { get; private set; }

        public Combatant Acting { get; private set; }

        public int ApLeft { get; private set; }

        public IReadOnlyList<Combatant> Order => _order;

        public event Action<Combatant> TurnAdvanced;

        public event Action<int> RoundEnded;

        public event Action<int> Ended;

        public event Action<Combatant, Combatant, StrikeResult> Struck;

        public event Action<Combatant, IReadOnlyList<CombatCell>> Walked;

        public event Action<Combatant> GuardChanged;

        public event Action<Combatant, TraverseOption, bool, double> Traversed;

        public event Action<Combatant, Combatant, Treatment, bool> Treated;

        public event Action<Combatant, ConditionTick> Conditioned;

        public void BeginEncounter(IReadOnlyList<Combatant> combatants, Random rolls)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("An encounter is already running.");
            }

            _rolls = rolls;
            _order.Clear();
            _joining.Clear();
            _waiting.Clear();
            Round = 1;
            _turn = -1;
            _turnsThisRound = 0;
            Acting = null;
            IsRunning = true;
            foreach (var combatant in combatants)
            {
                combatant.Fired = false;
                combatant.Intent = null;
                if (Place(combatant))
                {
                    _order.Add(combatant);
                }
            }

            Sort();
            if (!CheckEnd())
            {
                NextTurn();
            }
        }

        public bool SubmitIntent(TurnIntent intent)
        {
            var c = Acting;
            if (c == null || !c.TakesIntents)
            {
                return false;
            }

            switch (intent.Kind)
            {
                case IntentKind.Move:
                    var steps = c.Fighter.LegBroken ? -1 : _grid.FindPath(c.Cell, intent.Cell, _path);
                    if (steps <= 0 || steps > ApLeft)
                    {
                        return false;
                    }

                    Walk(c, _path, steps, null);
                    return true;
                case IntentKind.Attack:
                    var target = Find(intent.Target);
                    return target != null && Strike(c, target);
                case IntentKind.Traverse:
                    return Traverse(c, intent.Cell);
                case IntentKind.Treat:
                    return Treat(c, intent.Treatment, Find(intent.Target));
                case IntentKind.Wait:
                    return Wait(c);
                case IntentKind.Stance:
                    return Stand(c);
                case IntentKind.ChangeSet:
                    return ChangeSet(c);
                default:
                    EndTurn(c);
                    return true;
            }
        }

        public int PathToStrike(Combatant attacker, Combatant target, List<CombatCell> path)
        {
            path?.Clear();
            if (!IsRunning || attacker == null || target == null || attacker.Fighter.Weapon == null || !attacker.Standing
                || !target.Standing || target.Side == attacker.Side)
            {
                return -1;
            }

            if (Reaches(attacker, target))
            {
                return 0;
            }

            if (attacker.Fighter.LegBroken)
            {
                return -1;
            }

            var steps = StepsBeside(attacker, target);
            if (steps == int.MaxValue)
            {
                return -1;
            }

            // A shot is taken from the first cell on the way that has the target in range and in sight.
            if (attacker.Fighter.Weapon.Ranged)
            {
                for (var i = 0; i < _candidate.Count; i++)
                {
                    if (ReachesFrom(attacker, _candidate[i], target))
                    {
                        steps = i + 1;
                        _candidate.RemoveRange(steps, _candidate.Count - steps);
                        break;
                    }
                }
            }

            path?.AddRange(_candidate);
            return steps;
        }

        public StrikeContext ContextFor(Combatant attacker, CombatCell from, Combatant target)
        {
            var context = new StrikeContext { Beyond = Distance(from, target.Cell) > 1 };
            var bonus = _rules.SurroundedBonus(EnemiesBeside(target, attacker, from));
            if (attacker.Fighter.Weapon != null && attacker.Fighter.Weapon.Ranged)
            {
                if (EnemyBeside(attacker, from))
                {
                    bonus -= _rules.PointBlank;
                }

                if (context.Beyond)
                {
                    bonus -= _rules.CoverPenalty(CoverFacing(target.Cell, from));
                }
            }

            if (_grid.Center(from).y - _grid.Center(target.Cell).y >= _rules.HigherGroundRise)
            {
                bonus += _rules.HigherGround;
                context.CriticalBonus += _rules.HigherGroundCritical;
            }

            context.HitBonus = bonus;
            return context;
        }

        public bool ReachesFrom(Combatant attacker, CombatCell from, Combatant target)
        {
            var weapon = attacker.Fighter.Weapon;
            if (weapon == null)
            {
                return false;
            }

            return weapon.Ranged
                ? Distance(from, target.Cell) <= weapon.RangeCells && _grid.CanSee(from, target.Cell)
                : Beside(from, target.Cell);
        }

        public void Join(Combatant combatant)
        {
            if (IsRunning && combatant.State != CombatantState.Fighting && !_joining.Contains(combatant))
            {
                combatant.State = CombatantState.Joining;
                _joining.Add(combatant);
            }
        }

        public void Leave(Combatant combatant)
        {
            _joining.Remove(combatant);
            if (!IsRunning || combatant.State != CombatantState.Fighting)
            {
                return;
            }

            combatant.State = CombatantState.Out;
            _grid.Vacate(combatant.Id);
            if (!CheckEnd() && Acting == combatant)
            {
                NextTurn();
            }
        }

        private bool Place(Combatant c)
        {
            if (!_grid.TryCellAt(c.Position, out var cell) || !Free(cell, c.Id))
            {
                if (!NearestFree(c, out cell))
                {
                    c.State = CombatantState.Out;
                    return false;
                }
            }

            _grid.Occupy(c.Id, cell);
            c.Cell = cell;
            c.Position = _grid.Center(cell);
            c.State = CombatantState.Fighting;
            c.Waited = c.Waiting = c.Attacked = c.Reacted = false;
            c.Fighter.Stance = false;
            _rules.Enter(c.Fighter, _rolls);
            c.Initiative = _rules.Initiative(c.Fighter, _rolls);
            c.Tie = _rolls.NextDouble();
            return true;
        }

        private bool Free(CombatCell cell, int id)
        {
            var occupant = _grid.OccupantAt(cell);
            return _grid.IsReachable(cell) && (occupant < 0 || occupant == id);
        }

        private bool NearestFree(Combatant c, out CombatCell cell)
        {
            var size = _grid.CellSize;
            var x0 = Mathf.FloorToInt(c.Position.x / size);
            var z0 = Mathf.FloorToInt(c.Position.z / size);
            for (var ring = 1; ring <= PlaceRing; ring++)
            {
                for (var dz = -ring; dz <= ring; dz++)
                {
                    for (var dx = -ring; dx <= ring; dx++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dz)) == ring
                            && _grid.TryCellAt(new Vector3((x0 + dx + 0.5f) * size, c.Position.y, (z0 + dz + 0.5f) * size), out cell)
                            && Free(cell, c.Id))
                        {
                            return true;
                        }
                    }
                }
            }

            cell = default;
            return false;
        }

        private void Sort()
        {
            _order.Sort((a, b) =>
            {
                var by = b.Initiative.CompareTo(a.Initiative);
                if (by == 0)
                {
                    by = b.Fighter.Get(CharacterAttribute.Dexterity).CompareTo(a.Fighter.Get(CharacterAttribute.Dexterity));
                }

                return by != 0 ? by : a.Tie.CompareTo(b.Tie);
            });
        }

        /// <summary>
        /// The next turn in Initiative's order; after the last, those who waited, the lowest Initiative first, each with
        /// the AP it kept less what its reactions spent; then the round ends.
        /// </summary>
        private void NextTurn()
        {
            Acting = null;
            while (IsRunning)
            {
                Combatant c;
                var resumed = false;
                if (_turn + 1 < _order.Count)
                {
                    _turn++;
                    c = _order[_turn];
                }
                else if (TakeWaiting(out c))
                {
                    resumed = true;
                    c.Waiting = false;
                }
                else
                {
                    EndRound();
                    if (!IsRunning)
                    {
                        return;
                    }

                    _turn = -1;
                    continue;
                }

                if (!c.Standing)
                {
                    continue;
                }

                _turnsThisRound++;
                c.Reacted = false;
                c.Attacked = false;
                if (c.Fighter.Stance)
                {
                    c.Fighter.Stance = false; // a stance holds until the fighter's next turn
                    GuardChanged?.Invoke(c);
                }

                var held = resumed ? Math.Min(c.HeldAp, c.Fighter.Reserve) : 0;
                c.Fighter.Reserve = 0; // the AP kept for reactions expire as the new turn begins (10.2 §5)
                if (c.Fighter.LostTurns > 0)
                {
                    c.Fighter.LostTurns--;
                    continue;
                }

                ApLeft = resumed ? held : _rules.TurnAp(c.Fighter);
                Acting = c;
                TurnAdvanced?.Invoke(c);
                return;
            }
        }

        /// <summary>The waiting combatant with the lowest Initiative, the lower Dexterity on a tie (10.2 §5b Wait).</summary>
        private bool TakeWaiting(out Combatant next)
        {
            next = null;
            foreach (var c in _waiting)
            {
                if (next == null || c.Initiative < next.Initiative
                    || (c.Initiative == next.Initiative && c.Fighter.Get(CharacterAttribute.Dexterity) < next.Fighter.Get(CharacterAttribute.Dexterity)))
                {
                    next = c;
                }
            }

            return next != null && _waiting.Remove(next);
        }

        private void EndRound()
        {
            var round = Round;
            _turnsThisRound = 0;
            TickConditions();
            RoundEnded?.Invoke(round);
            if (!IsRunning)
            {
                return;
            }

            Round++;
            _waiting.Clear();
            foreach (var c in _order)
            {
                c.Waited = c.Waiting = false;
            }

            foreach (var c in _joining)
            {
                if (c.State == CombatantState.Joining && Place(c))
                {
                    _order.Add(c);
                }
            }

            _joining.Clear();
            _order.RemoveAll(c => c.State == CombatantState.Out);
            Sort();
            CheckEnd();
        }

        /// <summary>Ends the encounter when no two sides stand in it; true when it ended.</summary>
        private bool CheckEnd()
        {
            var any = false;
            var side = -1;
            foreach (var c in _order)
            {
                if (!c.Standing)
                {
                    continue;
                }

                if (!any)
                {
                    any = true;
                    side = c.Side;
                }
                else if (c.Side != side)
                {
                    return false;
                }
            }

            Finish(side);
            return true;
        }

        private void Finish(int side)
        {
            if (!IsRunning)
            {
                return;
            }

            // The round the fight ended in is its last, and it costs its tick like any other.
            var played = _turnsThisRound > 0;
            IsRunning = false;
            Acting = null;
            ApLeft = 0;
            _turnsThisRound = 0;
            foreach (var c in _order)
            {
                _grid.Vacate(c.Id);
            }

            _joining.Clear();
            if (played)
            {
                TickConditions();
                RoundEnded?.Invoke(Round);
            }

            Ended?.Invoke(side);
        }

        private void EndTurn(Combatant c)
        {
            c.Fighter.Reserve = Math.Max(0, ApLeft);
            NextTurn();
        }

        /// <summary>10.2 §5b Wait: once a round, the turn goes to the end of the round, and its AP wait in the pool for reactions.</summary>
        private bool Wait(Combatant c)
        {
            if (c.Waited)
            {
                return false;
            }

            c.Waited = c.Waiting = true;
            c.HeldAp = ApLeft;
            c.Fighter.Reserve = ApLeft;
            _waiting.Add(c);
            NextTurn();
            return true;
        }

        /// <summary>10.2 §5b Defensive Stance: a combatant that has not attacked ends its turn in a stance, its AP left held.</summary>
        private bool Stand(Combatant c)
        {
            if (c.Attacked)
            {
                return false;
            }

            c.Fighter.Stance = true;
            GuardChanged?.Invoke(c);
            EndTurn(c);
            return true;
        }

        /// <summary>10.2 §5b: the second Weapon Set to hand, for its AP.</summary>
        private bool ChangeSet(Combatant c)
        {
            if (ApLeft < _rules.WeaponSetAp || !c.Fighter.SwapSets())
            {
                return false;
            }

            ApLeft -= _rules.WeaponSetAp;
            GuardChanged?.Invoke(c);
            return true;
        }

        /// <summary>
        /// Walks a path a step at a time while AP last, stopping once <paramref name="target"/> is reached. Leaving the
        /// side of a melee enemy draws its Reaction Attack first; a walker who falls to one stops, and its turn ends.
        /// </summary>
        private int Walk(Combatant c, List<CombatCell> path, int steps, Combatant target)
        {
            var walked = 0;
            _walked.Clear();
            if (c.Fighter.LegBroken)
            {
                return 0;
            }

            for (var i = 0; i < steps && i < path.Count && ApLeft > 0; i++)
            {
                var occupant = _grid.OccupantAt(path[i]);
                if ((occupant >= 0 && occupant != c.Id) || !ReactionAttacks(c) || !_grid.Occupy(c.Id, path[i]))
                {
                    break;
                }

                Face(c, path[i]);
                c.Cell = path[i];
                c.Position = _grid.Center(c.Cell);
                _walked.Add(path[i]);
                ApLeft--;
                walked++;
                if (target != null && Reaches(c, target))
                {
                    break;
                }
            }

            c.Position = _grid.Center(c.Cell);
            FlushWalk(c);
            if (!c.Standing && IsRunning && !CheckEnd() && Acting == c)
            {
                NextTurn();
            }

            return walked;
        }

        /// <summary>Tells the cells walked since the last telling, so a blow on the way is shown where it falls.</summary>
        private void FlushWalk(Combatant c)
        {
            if (_walked.Count > 0)
            {
                Walked?.Invoke(c, _walked);
                _walked.Clear();
            }
        }

        /// <summary>
        /// 10.2 §5b Position: each enemy with a melee weapon at a side of the walker's cell strikes as it leaves, once a
        /// round, for no AP; the walker may parry or block with the AP it has left. False if the walker fell.
        /// </summary>
        private bool ReactionAttacks(Combatant c)
        {
            foreach (var e in _order)
            {
                if (!c.Standing)
                {
                    break;
                }

                var weapon = e.Fighter.Weapon;
                if (e == c || !e.Standing || e.Side == c.Side || e.Reacted || e.Fighter.LostTurns > 0 || weapon == null || weapon.Ranged
                    || !Beside(e.Cell, c.Cell))
                {
                    continue;
                }

                FlushWalk(c);
                e.Reacted = true;
                Face(e, c.Cell);
                c.Fighter.Reserve = ApLeft;
                var result = _rules.Strike(e.Fighter, c.Fighter, ContextFor(e, e.Cell, c), _rolls);
                Afflict(c, result);
                ApLeft = c.Fighter.Reserve;
                c.Fighter.Reserve = 0;
                if (c.Fighter.Down)
                {
                    _grid.Vacate(c.Id);
                }

                Struck?.Invoke(e, c, result);
            }

            return c.Standing;
        }

        private bool Strike(Combatant c, Combatant target)
        {
            var weapon = c.Fighter.Weapon;
            if (weapon == null || !target.Standing || target.Side == c.Side || ApLeft < weapon.AttackAp || !Reaches(c, target))
            {
                return false;
            }

            ApLeft -= weapon.AttackAp;
            c.Attacked = true;
            c.Fired |= weapon.Ranged;
            Face(c, target.Cell);
            var result = _rules.Strike(c.Fighter, target.Fighter, ContextFor(c, c.Cell, target), _rolls);
            Afflict(target, result);
            if (target.Fighter.Down)
            {
                _grid.Vacate(target.Id);
            }

            Struck?.Invoke(c, target, result);
            Counterattack(target, c);
            if (!CheckEnd() && !c.Standing && Acting == c)
            {
                NextTurn();
            }

            return true;
        }

        /// <summary>
        /// 10.2 §5b Defensive Stance: a defender in a stance answers a melee attack, hit or miss, with a blow for its
        /// weapon's AP plus 2 from those it holds; a Counterattack is not itself answered.
        /// </summary>
        private void Counterattack(Combatant defender, Combatant attacker)
        {
            var weapon = defender.Fighter.Weapon;
            if (!defender.Fighter.Stance || !defender.Standing || !attacker.Standing || defender.Fighter.LostTurns > 0 || weapon == null
                || weapon.Ranged || attacker.Fighter.Weapon.Ranged)
            {
                return;
            }

            var price = weapon.AttackAp + _rules.CounterattackAp;
            if (defender.Fighter.Reserve < price || !Reaches(defender, attacker))
            {
                return;
            }

            defender.Fighter.Reserve -= price;
            Face(defender, attacker.Cell);
            var result = _rules.Strike(defender.Fighter, attacker.Fighter, ContextFor(defender, defender.Cell, attacker), _rolls);
            Afflict(attacker, result);
            if (attacker.Fighter.Down)
            {
                _grid.Vacate(attacker.Id);
            }

            Struck?.Invoke(defender, attacker, result);
        }

        /// <summary>10.2 *Beasts of the slice*: a beast's critical hit makes its target bleed, and a glowing bite may poison it.</summary>
        private static void Afflict(Combatant target, StrikeResult result)
        {
            target.Fighter.Bleeding |= result.Bleeds;
            target.Fighter.Poisoned |= result.Poisoned;
        }

        /// <summary>
        /// One world tick of every standing combatant's conditions, at the round's end (10.2: a round is one tick). One
        /// whom Bleeding brings down leaves its cell.
        /// </summary>
        private void TickConditions()
        {
            foreach (var c in _order)
            {
                if (!c.Standing)
                {
                    continue;
                }

                var tick = _rules.Tick(c.Fighter, _rolls, false);
                if (c.Fighter.Down)
                {
                    _grid.Vacate(c.Id);
                }

                if (tick.Any)
                {
                    Conditioned?.Invoke(c, tick);
                }
            }
        }

        public bool TraverseFrom(Combatant combatant, CombatCell to, out TraverseOption option)
        {
            option = default;
            var from = combatant.Cell;
            var dx = to.X - from.X;
            var dz = to.Z - from.Z;
            if (Math.Abs(dx) + Math.Abs(dz) != 1 || !_grid.TryGetEnvironmentAction(from, to, out var tag))
            {
                return false;
            }

            option.Kind = tag.Action;
            option.Band = tag.Band;
            option.Tag = tag;
            option.Ap = _rules.EnvironmentAp(tag.Action);
            option.Chance = _rules.EnvironmentChance(combatant.Fighter.Values, tag.Action, tag.Band);
            option.Landing = from;
            if (tag.Action == EnvironmentActionKind.KickBreach)
            {
                return true;
            }

            // A vault clears a low wall into the cell beside, or a crate line into the cell past it.
            var rise = _rules.LandingRise(tag.Action);
            var landed = _grid.TryLand(from, dx, dz, rise.x, rise.y, out var landing) && _grid.OccupantAt(landing) < 0;
            if (!landed && tag.Action == EnvironmentActionKind.VaultJump)
            {
                landed = _grid.TryLand(from, dx * 2, dz * 2, rise.x, rise.y, out landing) && _grid.OccupantAt(landing) < 0;
            }

            if (!landed)
            {
                return false;
            }

            option.Landing = landing;
            option.Rise = _grid.Center(landing).y - _grid.Center(from).y;
            return true;
        }

        /// <summary>
        /// 10.1 Environment Actions: the AP are spent whatever comes. A success leaves the cell, drawing its Reaction
        /// Attacks as a step does, and lands; a breach that works leaves the block for the scene to clear. A failed vault,
        /// window or breach stays where it stood; a failed climb or mantle falls back the height it rose and ends the turn.
        /// </summary>
        private bool Traverse(Combatant c, CombatCell to)
        {
            if (c.Fighter.LegBroken || !TraverseFrom(c, to, out var option) || ApLeft < option.Ap)
            {
                return false;
            }

            ApLeft -= option.Ap;
            Face(c, to);
            var success = _rolls.NextDouble() < option.Chance;
            var fallen = 0d;
            if (success && option.Kind != EnvironmentActionKind.KickBreach && ReactionAttacks(c) && _grid.Occupy(c.Id, option.Landing))
            {
                c.Cell = option.Landing;
                c.Position = _grid.Center(c.Cell);
                _walked.Clear();
                _walked.Add(option.Landing);
                FlushWalk(c);
            }
            else if (!success && (option.Kind == EnvironmentActionKind.ClimbUp || option.Kind == EnvironmentActionKind.MantleOnto))
            {
                fallen = _rules.Fall(c.Fighter, option.Rise);
                if (c.Fighter.Down)
                {
                    _grid.Vacate(c.Id);
                }
            }

            Traversed?.Invoke(c, option, success, fallen);
            if (CheckEnd() || Acting != c)
            {
                return true;
            }

            if (!c.Standing)
            {
                NextTurn();
            }
            else if (fallen > 0d)
            {
                EndTurn(c); // a fall ends the turn (10.1)
            }

            return true;
        }

        /// <summary>
        /// 10.2 First Aid in the slice: a combatant treats itself or an ally in the next cell for the treatment's AP. An
        /// antidote and a splint need the lesson. A success ends the condition and uses the item; a failure spends the
        /// AP and keeps it.
        /// </summary>
        private bool Treat(Combatant c, Treatment treatment, Combatant patient)
        {
            if (patient == null || !patient.Standing || patient.Side != c.Side || Distance(c.Cell, patient.Cell) > 1
                || !FightRules.Needs(patient.Fighter, treatment) || c.Supplies[(int)treatment] <= 0
                || (_rules.TreatmentNeedsLesson(treatment) && !c.KnowsFirstAid) || ApLeft < _rules.TreatmentAp(treatment))
            {
                return false;
            }

            ApLeft -= _rules.TreatmentAp(treatment);
            var success = _rolls.NextDouble() < _rules.TreatmentChance(c.Fighter.Values, treatment);
            if (success)
            {
                FightRules.Cure(patient.Fighter, treatment);
                c.Supplies[(int)treatment]--;
            }

            Treated?.Invoke(c, patient, treatment, success);
            return true;
        }

        private bool Reaches(Combatant c, Combatant target) => ReachesFrom(c, c.Cell, target);

        /// <summary>Two cells side by side with no wall between: a melee blow goes across a side only (10.2 §5b Position).</summary>
        private bool Beside(CombatCell a, CombatCell b) => Math.Abs(a.X - b.X) + Math.Abs(a.Z - b.Z) == 1 && _grid.CanStep(a, b);

        /// <summary>Enemies of the target at its sides able to act, with the attacker counted where it would strike from.</summary>
        private int EnemiesBeside(Combatant target, Combatant attacker, CombatCell from)
        {
            var n = 0;
            foreach (var other in _order)
            {
                if (other != attacker && other != target && other.Standing && other.Side != target.Side && other.Fighter.LostTurns == 0
                    && Beside(other.Cell, target.Cell))
                {
                    n++;
                }
            }

            return attacker.Side != target.Side && Beside(from, target.Cell) ? n + 1 : n;
        }

        /// <summary>An enemy at a side of the cell a shooter would shoot from (10.2 §5b Point-Blank Shot).</summary>
        private bool EnemyBeside(Combatant shooter, CombatCell from)
        {
            foreach (var other in _order)
            {
                if (other != shooter && other.Standing && other.Side != shooter.Side && Beside(from, other.Cell))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The best cover on the sides of the target's cell that face the shooter: a side counts when the shooter stands
        /// at least half as far along its axis as across it (S3a.6 working reading).
        /// </summary>
        private CoverLevel CoverFacing(CombatCell cell, CombatCell from)
        {
            var dx = from.X - cell.X;
            var dz = from.Z - cell.Z;
            var best = CoverLevel.None;
            if (dx != 0 && Math.Abs(dx) * 2 >= Math.Abs(dz))
            {
                best = Higher(best, _grid.CoverOn(cell, dx > 0 ? GridSide.PlusX : GridSide.MinusX));
            }

            if (dz != 0 && Math.Abs(dz) * 2 >= Math.Abs(dx))
            {
                best = Higher(best, _grid.CoverOn(cell, dz > 0 ? GridSide.PlusZ : GridSide.MinusZ));
            }

            return best;
        }

        private static CoverLevel Higher(CoverLevel a, CoverLevel b) => (int)a >= (int)b ? a : b;

        /// <summary>The fewest steps to a free cell beside the target from which a blow reaches it, its path in <see cref="_candidate"/>.</summary>
        private int StepsBeside(Combatant c, Combatant target)
        {
            var best = int.MaxValue;
            _candidate.Clear();
            var size = _grid.CellSize;
            var y = target.Position.y;
            for (var k = 0; k < SideX.Length; k++)
            {
                var x = target.Cell.X + SideX[k];
                var z = target.Cell.Z + SideZ[k];
                if (!_grid.TryCellAt(new Vector3((x + 0.5f) * size, y, (z + 0.5f) * size), out var beside) || !_grid.CanStep(beside, target.Cell))
                {
                    continue;
                }

                var n = _grid.FindPath(c.Cell, beside, _path);
                if (n > 0 && n < best)
                {
                    best = n;
                    _candidate.Clear();
                    _candidate.AddRange(_path);
                }
            }

            return best;
        }

        private Combatant Find(int id)
        {
            foreach (var c in _order)
            {
                if (c.Id == id)
                {
                    return c;
                }
            }

            return null;
        }

        private static int Distance(CombatCell a, CombatCell b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Z - b.Z));
    }
}
