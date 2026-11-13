using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Player.Progression;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// S3a.8: a combatant with no will of its own plays its turn by Target Rules V0 (10.2 §5a), as
    /// `tools/balance/tactical_sim.py` plays them. It scores each enemy by Target Impact and goes for the best. A melee
    /// fighter strikes the enemy beside it when it will not reach its target this turn. A shooter shoots its best target
    /// in range and steps back with the AP left. A melee fighter waits when an enemy still to act will walk into its
    /// reach, and holds back in a Defensive Stance rather than walk into such an enemy's reach. Since the combat rework
    /// (10.2 §5f *The AI*) a firearm comes first: a shooter takes its melee set only beside an enemy and when it pays, weighs
    /// its own people on a Shot Line and steps aside from them, and a Warden takes its baton beside an enemy.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // A turn's decisions are bounded, so a rule that changes nothing can never hold the fight (authoring default).
        private const int MaxDecisions = 64;

        // A shooter steps back toward a point this many cells away from the melee enemy (tactical_sim.py).
        private const int RetreatGoal = 40;

        private readonly List<Combatant> _foes = new List<Combatant>();
        private readonly List<Combatant> _beside = new List<Combatant>();
        private readonly List<CombatCell> _route = new List<CombatCell>();

        public void PlayTurn()
        {
            var c = Acting;
            if (c == null || !IsRunning || c.Fighter.Weapon == null)
            {
                if (c != null && IsRunning)
                {
                    EndTurn(c);
                }

                return;
            }

            // S3a.9: a fighter of people who is losing asks for mercy and strikes no more; one facing only those asking spares them.
            if (c.AsksMercy || AsksForMercy(c))
            {
                if (!c.AsksMercy)
                {
                    c.AsksMercy = true;
                    c.Intent = null;
                    MercyAsked?.Invoke(c);
                }

                EndTurn(c);
                return;
            }

            if (SparesTheAsking(c))
            {
                if (IsRunning && Acting == c)
                {
                    EndTurn(c);
                }

                return;
            }

            // 10.2 §5a: a resident whose courage fails it flees instead of striking (PB6).
            if (Flees(c))
            {
                Flee(c);
                return;
            }

            // S3b.2: a beast with no enemy inside its territory goes back to it; a beast takes no Wait.
            if (Withdraws(c) || (!c.Waited && !c.Beast && Waits(c) && Wait(c)))
            {
                return;
            }

            var first = true;
            var stepped = false;
            for (var decision = 0; decision < MaxDecisions && IsRunning && Acting == c && ApLeft > 0; decision++)
            {
                Foes(c, _foes);
                if (_foes.Count == 0)
                {
                    break;
                }

                if (ChangesSet(c))
                {
                    if (!ChangeSet(c))
                    {
                        break;
                    }

                    continue;
                }

                var cost = c.Fighter.BlowAp;
                if (first || c.Intent == null || !c.Intent.Standing || c.Intent.Side == c.Side || c.Intent.Hidden)
                {
                    c.Intent = Best(c, _foes);
                }

                var target = c.Intent;
                if (Reaches(c, target))
                {
                    // 10.2 §5f: a shooter's own people on the line, once a turn, before its first shot.
                    if (first && !stepped && c.Fighter.Weapon.Ranged)
                    {
                        stepped = true;
                        if (StepAside(c, target))
                        {
                            continue;
                        }
                    }

                    if (!Strike(c, target))
                    {
                        break;
                    }

                    first = false;
                    continue;
                }

                if (c.Fighter.Weapon.Ranged)
                {
                    var steps = PathToStrike(c, target, _route);
                    if (steps <= 0 || Walk(c, _route, steps, target) == 0)
                    {
                        break;
                    }

                    first = false;
                    continue;
                }

                Beside(c, _beside);
                if (_beside.Count == 0 && Holds(c, c.Cell, ApLeft))
                {
                    Stand(c);
                    return;
                }

                if (_beside.Count > 0)
                {
                    var near = Best(c, _beside);
                    var need = PathToStrike(c, target, null);
                    var reachIt = need > 0 && ApLeft - need >= cost;
                    var worthIt = first && Score(c, target) > _rules.BreakOffFactor * Score(c, near);
                    if (!(reachIt || worthIt))
                    {
                        if (!Strike(c, near))
                        {
                            break;
                        }

                        first = false;
                        continue;
                    }
                }

                var walk = PathToStrike(c, target, _route);
                if (walk <= 0)
                {
                    if (_beside.Count > 0 && Strike(c, Best(c, _beside)))
                    {
                        first = false;
                        continue;
                    }

                    break;
                }

                // Holding Back is weighed before each step, so the walk stops at the first cell where it applies.
                var n = Math.Min(walk, ApLeft);
                for (var i = 0; i < n && _beside.Count == 0; i++)
                {
                    if (Holds(c, _route[i], ApLeft - i - 1))
                    {
                        n = i + 1;
                        break;
                    }
                }

                if (Walk(c, _route, n, target) == 0)
                {
                    break;
                }

                first = false;
            }

            if (IsRunning && Acting == c)
            {
                Retreat(c);
            }

            if (IsRunning && Acting == c)
            {
                EndTurn(c);
            }
        }

        /// <summary>
        /// 10.2 §5a Target Impact: the harm the enemy does to the fighter's side in a turn, times the share of its HP the
        /// fighter can take off in one, lowered by each full turn of walking; a kill now, a helpless enemy and one already
        /// crowded by the fighter's allies weigh as Target Rules V0 say.
        /// </summary>
        public double Score(Combatant c, Combatant e)
        {
            var reach = PathToStrike(c, e, null);
            if (reach < 0)
            {
                return 0d;
            }

            var from = reach == 0 || _candidate.Count == 0 ? c.Cell : _candidate[_candidate.Count - 1];
            var turns = Math.Max(0, (int)Math.Ceiling((reach - ApLeft) / (double)Math.Max(1, _rules.FullAp(c.Fighter))));
            var expected = Blows(c) * ExpectedBlow(c, from, e);
            var value = (Threat(e) + _rules.ThreatFloor) * Math.Min(1d, expected / Math.Max(1d, e.Fighter.Hp)) * Math.Pow(_rules.ReachDiscount, turns);
            if (turns == 0 && e.Fighter.Hp <= expected)
            {
                value *= _rules.KillBonus;
            }

            if (e.Helpless)
            {
                value *= _rules.HelplessFactor;
            }

            var crowd = 0;
            foreach (var ally in _order)
            {
                if (ally != c && ally.Standing && ally.Side == c.Side && Beside(ally.Cell, e.Cell))
                {
                    crowd++;
                }
            }

            if (crowd >= _rules.CrowdAllies)
            {
                value *= _rules.CrowdFactor;
            }

            // 10.2 §5f *The AI*: a target behind the shooter's own people counts (1 - the interception share) as much for each.
            if (c.Fighter.Weapon != null && c.Fighter.Weapon.Ranged && _rules.Intercept > 0f)
            {
                var friends = FriendsOnLine(c, from, e);
                if (friends > 0)
                {
                    value *= Math.Pow(1d - _rules.Intercept, friends);
                }
            }

            return value;
        }

        /// <summary>The enemy that scores highest, the first in Initiative's order on a tie.</summary>
        private Combatant Best(Combatant c, List<Combatant> enemies)
        {
            Combatant best = null;
            var top = double.NegativeInfinity;
            foreach (var e in enemies)
            {
                var score = Score(c, e);
                if (score > top)
                {
                    top = score;
                    best = e;
                }
            }

            return best;
        }

        /// <summary>The harm a fighter does in a turn: its blows times the mean expected blow at each enemy standing.</summary>
        private double Threat(Combatant e)
        {
            var sum = 0d;
            var n = 0;
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != e.Side)
                {
                    sum += ExpectedBlow(e, e.Cell, f);
                    n++;
                }
            }

            return n == 0 ? 0d : Blows(e) * sum / n;
        }

        private int Blows(Combatant c) => Math.Max(1, _rules.FullAp(c.Fighter) / Math.Max(1, c.Fighter.BlowAp));

        /// <summary>A blow's or a shot's chance times an ordinary hit's mean damage through the target's armour.</summary>
        private double ExpectedBlow(Combatant a, CombatCell from, Combatant t)
        {
            if (a.Fighter.Weapon == null)
            {
                return 0d;
            }

            var forecast = _rules.Forecast(a.Fighter, t.Fighter, ContextFor(a, from, t));
            return forecast.Chance * (forecast.Low + forecast.High) * 0.5d;
        }

        /// <summary>
        /// 10.2 §5a Waiting: a melee fighter with nobody in reach this turn waits, if an enemy with a melee weapon still to
        /// act this round will walk into its reach.
        /// </summary>
        private bool Waits(Combatant c)
        {
            var weapon = c.Fighter.Weapon;
            if (weapon.Ranged || c.Fighter.LostTurns > 0)
            {
                return false;
            }

            var reach = _rules.FullAp(c.Fighter) - c.Fighter.BlowAp;
            var later = false;
            foreach (var f in _order)
            {
                if (!f.Standing || f.Side == c.Side)
                {
                    continue;
                }

                if (StepsToSide(c.Cell, f.Cell) <= reach)
                {
                    return false;
                }

                later |= Pending(f) && Melee(f) && StepsToSide(f.Cell, c.Cell) - _rules.FullAp(f.Fighter) <= reach;
            }

            return later;
        }

        /// <summary>
        /// 10.2 §5a Hold Back: from this cell, with these AP, a melee fighter that has not struck can strike nobody this
        /// turn, and an enemy with a melee weapon still to act this round could walk to it and strike.
        /// </summary>
        private bool Holds(Combatant c, CombatCell cell, int ap)
        {
            if (c.Attacked || c.Beast)
            {
                return false;
            }

            var cost = c.Fighter.BlowAp;
            var threatened = false;
            foreach (var f in _order)
            {
                if (!f.Standing || f.Side == c.Side)
                {
                    continue;
                }

                if (StepsToSide(cell, f.Cell) + cost <= ap)
                {
                    return false;
                }

                // One cell short of that enemy's reach (tactical_sim.py: steps − 1).
                threatened |= Pending(f) && Melee(f) && !f.Helpless
                    && StepsToSide(f.Cell, cell) - 1 <= _rules.FullAp(f.Fighter) - f.Fighter.BlowAp;
            }

            return threatened;
        }

        /// <summary>
        /// 10.2 §5f *The AI: a firearm first*: a shooter takes its melee set only when an enemy stands beside it and that
        /// set's harm over the AP left, the change paid, beats its point-blank shots, and takes the firearm back when none
        /// does; a Warden takes its baton beside an enemy and its firearm back when free; a melee build that opened with a
        /// firearm gives way to its melee set after its first shot (§5b). A shooter is a fighter whose Light Firearms is
        /// above its Light Melee (S3a.8 working reading).
        /// </summary>
        private bool ChangesSet(Combatant c)
        {
            var second = c.Fighter.Second;
            if (second.IsEmpty || second.Weapon == null)
            {
                return false;
            }

            Combatant near = null;
            foreach (var f in _foes)
            {
                if (!f.Helpless && Beside(c.Cell, f.Cell) && (near == null || Threat(f) > Threat(near)))
                {
                    near = f;
                }
            }

            var shooter = Shooter(c);
            if (c.Fighter.Weapon.Ranged && !second.Weapon.Ranged)
            {
                if (second.Weapon.Subdues)
                {
                    return near != null;
                }

                return shooter ? near != null && MeleePays(c, near) : c.Fired;
            }

            return !c.Fighter.Weapon.Ranged && second.Weapon.Ranged && (shooter || c.Fighter.Weapon.Subdues) && near == null;
        }

        /// <summary>
        /// 10.2 §5f: whether the melee set's blows with the AP left after the change beat the firearm's shots at
        /// <paramref name="near"/> from here, which aim worse beside an enemy and meet its armour plain
        /// (<see cref="ContextFor"/>), as `tactical_sim.py` changes_set weighs them.
        /// </summary>
        private bool MeleePays(Combatant c, Combatant near)
        {
            var shots = (ApLeft / Math.Max(1, c.Fighter.Weapon.AttackAp)) * ExpectedBlow(c, c.Cell, near);
            if (!c.Fighter.SwapSets())
            {
                return false;
            }

            var blows = (Math.Max(0, ApLeft - _rules.WeaponSetAp) / Math.Max(1, c.Fighter.BlowAp)) * ExpectedBlow(c, c.Cell, near);
            c.Fighter.SwapSets();
            return blows > shots;
        }

        /// <summary>
        /// 10.2 §5b Firearms: a shooter spends the AP left after its shot stepping away from the nearest enemy with a
        /// melee weapon, never out of a side an enemy touches, which would draw a Reaction Attack.
        /// </summary>
        private void Retreat(Combatant c)
        {
            if (!c.Fighter.Weapon.Ranged || !Shooter(c) || ApLeft < 1 || c.Fighter.LegBroken)
            {
                return;
            }

            Combatant nearest = null;
            foreach (var f in _order)
            {
                if (!f.Standing || f.Side == c.Side)
                {
                    continue;
                }

                if (Beside(c.Cell, f.Cell))
                {
                    return;
                }

                if (Melee(f) && !f.Helpless && (nearest == null || Distance(c.Cell, f.Cell) < Distance(c.Cell, nearest.Cell)))
                {
                    nearest = f;
                }
            }

            if (nearest == null)
            {
                return;
            }

            var goalX = c.Cell.X + (RetreatGoal * Math.Sign(c.Cell.X - nearest.Cell.X));
            var goalZ = c.Cell.Z + (RetreatGoal * Math.Sign(c.Cell.Z - nearest.Cell.Z));
            WalkToward(c, goalX, goalZ, ApLeft);
        }

        /// <summary>
        /// Walks up to <paramref name="steps"/> cells toward a goal cell, each step to the free cell beside that comes
        /// nearest it, stopping where none comes nearer: a shooter's step back (§5b), a resident's flight (§5a).
        /// </summary>
        private void WalkToward(Combatant c, int goalX, int goalZ, int steps)
        {
            _route.Clear();
            var here = c.Cell;
            var size = _grid.CellSize;
            for (var step = 0; step < steps; step++)
            {
                var hereCost = (Chebyshev(here.X, here.Z, goalX, goalZ), Square(here.X, here.Z, goalX, goalZ));
                var found = false;
                var next = here;
                var nextCost = hereCost;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if ((dx == 0 && dz == 0)
                            || !_grid.TryCellAt(new Vector3((here.X + dx + 0.5f) * size, _grid.Center(here).y, (here.Z + dz + 0.5f) * size), out var cell)
                            || !_grid.CanStep(here, cell) || (_grid.OccupantAt(cell) >= 0 && _grid.OccupantAt(cell) != c.Id) || _route.Contains(cell))
                        {
                            continue;
                        }

                        var cost = (Chebyshev(cell.X, cell.Z, goalX, goalZ), Square(cell.X, cell.Z, goalX, goalZ));
                        if (cost.CompareTo(nextCost) < 0)
                        {
                            next = cell;
                            nextCost = cost;
                            found = true;
                        }
                    }
                }

                if (!found)
                {
                    break;
                }

                _route.Add(next);
                here = next;
            }

            if (_route.Count > 0)
            {
                Walk(c, _route, _route.Count, null);
            }
        }

        private void Foes(Combatant c, List<Combatant> into)
        {
            into.Clear();
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side && !f.Hidden)
                {
                    into.Add(f);
                }
            }
        }

        /// <summary>The standing enemies at a side of the fighter's cell.</summary>
        private void Beside(Combatant c, List<Combatant> into)
        {
            into.Clear();
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side && !f.Hidden && Beside(c.Cell, f.Cell))
                {
                    into.Add(f);
                }
            }
        }

        /// <summary>Its turn is still to come this round: after the one acting in Initiative's order, or waiting.</summary>
        private bool Pending(Combatant f) => _order.IndexOf(f) > _turn || _waiting.Contains(f);

        private static bool Melee(Combatant f) => f.Fighter.Weapon != null && !f.Fighter.Weapon.Ranged;

        private static bool Shooter(Combatant c) => c.Fighter.Get(CharacterSkill.LightFirearms) > c.Fighter.Get(CharacterSkill.LightMelee);

        /// <summary>
        /// Steps from a cell to a side of another in open ground, a diagonal step costing one: the estimate Waiting and
        /// Hold Back use, as `tactical_sim.py` does, walls left out (S3a.8 working reading).
        /// </summary>
        private static int StepsToSide(CombatCell from, CombatCell to)
        {
            var best = int.MaxValue;
            for (var k = 0; k < SideX.Length; k++)
            {
                best = Math.Min(best, Chebyshev(from.X, from.Z, to.X + SideX[k], to.Z + SideZ[k]));
            }

            return best;
        }

        private static int Chebyshev(int x, int z, int gx, int gz) => Math.Max(Math.Abs(x - gx), Math.Abs(z - gz));

        private static int Square(int x, int z, int gx, int gz) => ((x - gx) * (x - gx)) + ((z - gz) * (z - gz));

        /// <summary>The way from a cell to the next, one step along each axis.</summary>
        private static void Face(Combatant c, CombatCell toward)
        {
            var dx = Math.Sign(toward.X - c.Cell.X);
            var dz = Math.Sign(toward.Z - c.Cell.Z);
            if (dx != 0 || dz != 0)
            {
                c.Facing = new Vector2Int(dx, dz);
            }
        }
    }
}
