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
