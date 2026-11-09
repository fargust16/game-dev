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
    /// stays for reactions. A melee blow goes across a cell's side, a shot within the weapon's range. A combatant with
    /// no will of its own walks toward the nearest enemy and strikes while its AP last, until Target Rules V0 (S3a.8);
    /// line of sight, cover, reactions and the Reaction Attack come with S3a.6, conditions with S3a.7.
    /// </summary>
    public sealed class TurnEconomy : ITurnEconomy
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

        public void BeginEncounter(IReadOnlyList<Combatant> combatants, Random rolls)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("An encounter is already running.");
            }

            _rolls = rolls;
            _order.Clear();
            _joining.Clear();
            Round = 1;
            _turn = -1;
            _turnsThisRound = 0;
            Acting = null;
            IsRunning = true;
            foreach (var combatant in combatants)
            {
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
                    var steps = _grid.FindPath(c.Cell, intent.Cell, _path);
                    if (steps <= 0 || steps > ApLeft)
                    {
                        return false;
                    }

                    Walk(c, _path, steps, null);
                    return true;
                case IntentKind.Attack:
                    var target = Find(intent.Target);
                    return target != null && Strike(c, target);
                default:
                    EndTurn(c);
                    return true;
            }
        }

        public void PlayTurn()
        {
            var c = Acting;
            while (c != null && IsRunning && Acting == c)
            {
                var target = Nearest(c, out var steps);
                if (target == null)
                {
                    break;
                }

                if (steps == 0)
                {
                    if (!Strike(c, target))
                    {
                        break;
                    }

                    continue;
                }

                if (Walk(c, _best, _best.Count, target) == 0 || !Reaches(c, target))
                {
                    break;
                }
            }

            if (c != null && IsRunning && Acting == c)
            {
                EndTurn(c);
            }
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

        private void NextTurn()
        {
            Acting = null;
            while (IsRunning)
            {
                _turn++;
                if (_turn >= _order.Count)
                {
                    EndRound();
                    if (!IsRunning)
                    {
                        return;
                    }

                    _turn = 0;
                }

                var c = _order[_turn];
                if (!c.Standing)
                {
                    continue;
                }

                _turnsThisRound++;
                c.Fighter.Reserve = 0; // the AP kept for reactions expire as the new turn begins (10.2 §5)
                if (c.Fighter.LostTurns > 0)
                {
                    c.Fighter.LostTurns--;
                    continue;
                }

                ApLeft = _rules.TurnAp(c.Fighter);
                Acting = c;
                TurnAdvanced?.Invoke(c);
                return;
            }
        }

        private void EndRound()
        {
            var round = Round;
            _turnsThisRound = 0;
            RoundEnded?.Invoke(round);
            if (!IsRunning)
            {
                return;
            }

            Round++;
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
                RoundEnded?.Invoke(Round);
            }

            Ended?.Invoke(side);
        }

        private void EndTurn(Combatant c)
        {
            c.Fighter.Reserve = Math.Max(0, ApLeft);
            NextTurn();
        }

        /// <summary>Walks a path a step at a time while AP last, stopping once <paramref name="target"/> is reached.</summary>
        private int Walk(Combatant c, List<CombatCell> path, int steps, Combatant target)
        {
            var walked = 0;
            for (var i = 0; i < steps && i < path.Count && ApLeft > 0; i++)
            {
                if (!_grid.Occupy(c.Id, path[i]))
                {
                    break;
                }

                c.Cell = path[i];
                ApLeft--;
                walked++;
                if (target != null && Reaches(c, target))
                {
                    break;
                }
            }

            c.Position = _grid.Center(c.Cell);
            return walked;
        }

        private bool Strike(Combatant c, Combatant target)
        {
            var weapon = c.Fighter.Weapon;
            if (weapon == null || !target.Standing || target.Side == c.Side || ApLeft < weapon.AttackAp || !Reaches(c, target))
            {
                return false;
            }

            ApLeft -= weapon.AttackAp;
            _rules.Strike(c.Fighter, target.Fighter, new StrikeContext { Beyond = Distance(c.Cell, target.Cell) > 1 }, _rolls);
            if (target.Fighter.Down)
            {
                _grid.Vacate(target.Id);
            }

            CheckEnd();
            return true;
        }

        private bool Reaches(Combatant c, Combatant target)
        {
            var weapon = c.Fighter.Weapon;
            if (weapon == null)
            {
                return false;
            }

            if (weapon.Ranged)
            {
                return Distance(c.Cell, target.Cell) <= weapon.RangeCells;
            }

            // A melee blow goes across a cell's side only (10.2 §5b *Melee position*), and never through a wall.
            return Math.Abs(c.Cell.X - target.Cell.X) + Math.Abs(c.Cell.Z - target.Cell.Z) == 1 && _grid.CanStep(c.Cell, target.Cell);
        }

        /// <summary>The enemy fewest steps away, the weaker on a tie, with the path to it in <see cref="_best"/>; 0 steps when it is reached.</summary>
        private Combatant Nearest(Combatant c, out int steps)
        {
            Combatant chosen = null;
            steps = int.MaxValue;
            _best.Clear();
            foreach (var target in _order)
            {
                if (!target.Standing || target.Side == c.Side)
                {
                    continue;
                }

                var own = Reaches(c, target) ? 0 : StepsBeside(c, target);
                if (own < steps || (own == steps && own != int.MaxValue && target.Fighter.Hp < chosen.Fighter.Hp))
                {
                    chosen = target;
                    steps = own;
                    _best.Clear();
                    if (own > 0)
                    {
                        _best.AddRange(_candidate);
                    }
                }
            }

            return chosen;
        }

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
