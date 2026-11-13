using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// 10.1 *Shot Line* in a fight (CR2): the people on a shot's line are read from the cells it crosses, nearest first,
    /// friend, enemy or bystander; each takes the shot in <see cref="Rules.FightRules.Intercept"/> of shots before the
    /// target is rolled, and the shot is then rolled against whoever took it, with that person's own Evasion and cover.
    /// The fallen take none. A line with nobody on it draws no roll, so such a fight rolls as before.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        // Authoring default, m: a person whose cell stands farther than this above or below the line is on another floor.
        private const float LineHeightBand = 1.5f;

        private static readonly Comparison<(float Along, Combatant Who)> Nearer = (a, b) => a.Along.CompareTo(b.Along);
        private readonly List<(float Along, Combatant Who)> _onLine = new List<(float Along, Combatant Who)>();

        public int PeopleOnLine(Combatant attacker, CombatCell from, Combatant target) => OnLine(attacker, from, target);

        /// <summary>10.2 §5f *The AI*: how many of <paramref name="attacker"/>'s own people stand on its line from <paramref name="from"/>.</summary>
        private int FriendsOnLine(Combatant attacker, CombatCell from, Combatant target)
        {
            var count = OnLine(attacker, from, target);
            var friends = 0;
            for (var i = 0; i < count; i++)
            {
                friends += _onLine[i].Who.Side == attacker.Side ? 1 : 0;
            }

            return friends;
        }

        /// <summary>
        /// 10.2 §5f *The AI*: a shooter whose line to its target has its own people on it steps to a free cell beside it
        /// with no friend on the line, the shot clear and in range from there and no enemy at its sides or corners, when
        /// the AP still buy the shot after the step (`tactical_sim.py` clear_step). The step draws a Reaction Attack as
        /// any step does. False when no such cell is found or the AP do not stretch.
        /// </summary>
        private bool StepAside(Combatant c, Combatant target)
        {
            var weapon = c.Fighter.Weapon;
            if (!weapon.Ranged || c.Fighter.LegBroken || ApLeft < 1 + weapon.AttackAp || FriendsOnLine(c, c.Cell, target) == 0)
            {
                return false;
            }

            var here = c.Cell;
            var size = _grid.CellSize;
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dz = -1; dz <= 1; dz++)
                {
                    if ((dx == 0 && dz == 0)
                        || !_grid.TryCellAt(new UnityEngine.Vector3((here.X + dx + 0.5f) * size, _grid.Center(here).y, (here.Z + dz + 0.5f) * size), out var cell)
                        || !_grid.CanStep(here, cell) || _grid.OccupantAt(cell) >= 0 || NearAnEnemy(c, cell)
                        || !ReachesFrom(c, cell, target) || FriendsOnLine(c, cell, target) > 0)
                    {
                        continue;
                    }

                    _route.Clear();
                    _route.Add(cell);
                    return Walk(c, _route, 1, null) == 1;
                }
            }

            return false;
        }

        /// <summary>An enemy standing at a side or a corner of <paramref name="cell"/>.</summary>
        private bool NearAnEnemy(Combatant c, CombatCell cell)
        {
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side && Math.Max(Math.Abs(f.Cell.X - cell.X), Math.Abs(f.Cell.Z - cell.Z)) <= 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whoever takes <paramref name="attacker"/>'s shot from <paramref name="from"/> at <paramref name="target"/>: someone on the line, or the target.</summary>
        private Combatant ShotTaker(Combatant attacker, CombatCell from, Combatant target)
        {
            var count = OnLine(attacker, from, target);
            for (var i = 0; i < count; i++)
            {
                if (_rolls.NextDouble() < _rules.Intercept)
                {
                    return _onLine[i].Who;
                }
            }

            return target;
        }

        /// <summary>
        /// Fills the line's people, nearest first: every standing combatant but the two whose cell the line between the
        /// cells' middles crosses, a corner touched not counting.
        /// </summary>
        private int OnLine(Combatant attacker, CombatCell from, Combatant target)
        {
            _onLine.Clear();
            if (target == null)
            {
                return 0;
            }

            var a = _grid.Center(from);
            var b = _grid.Center(target.Cell);
            var dx = b.x - a.x;
            var dz = b.z - a.z;
            var length = (float)Math.Sqrt((dx * dx) + (dz * dz));
            if (length < 1e-4f)
            {
                return 0;
            }

            var ux = dx / length;
            var uz = dz / length;
            // A cell's half-width across the line, less a millimetre, so that a corner the line only touches never counts.
            var reach = (_grid.CellSize * 0.5f * (Math.Abs(ux) + Math.Abs(uz))) - 1e-3f;
            foreach (var c in _order)
            {
                if (c == attacker || c == target || !c.Standing)
                {
                    continue;
                }

                var p = _grid.Center(c.Cell);
                var along = ((p.x - a.x) * ux) + ((p.z - a.z) * uz);
                if (along <= 0f || along >= length || Math.Abs(((p.x - a.x) * uz) - ((p.z - a.z) * ux)) >= reach)
                {
                    continue;
                }

                var lineY = a.y + ((b.y - a.y) * along / length);
                if (Math.Abs(p.y - lineY) <= LineHeightBand)
                {
                    _onLine.Add((along, c));
                }
            }

            _onLine.Sort(Nearer);
            return _onLine.Count;
        }
    }
}
