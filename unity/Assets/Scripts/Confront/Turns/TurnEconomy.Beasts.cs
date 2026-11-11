using System.Collections.Generic;
using GameDev.Confront.Grid;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// S3b.2: beasts in a fight (10.2 *Beasts of the slice*). A beast takes no Wait and no Defensive Stance. It keeps to its
    /// territory: it walks into no cell farther from the territory's centre than its range, and on its turn it goes back
    /// when no enemy stands inside the territory, so leaving the territory is the Withdraw of a beast fight. When the first
    /// beast of a pack falls, the rest of the pack flee, those still to join with them.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        private readonly List<Combatant> _fleeing = new List<Combatant>();
        private bool _routing;

        /// <summary>Whether a place lies inside a combatant's territory, measured on the ground; true for one with none.</summary>
        public static bool Keeps(Combatant c, Vector3 at)
        {
            if (c.Range <= 0f)
            {
                return true;
            }

            var way = at - c.Home;
            way.y = 0f;
            return way.sqrMagnitude <= c.Range * c.Range;
        }

        private bool MayEnter(Combatant c, CombatCell cell) => Keeps(c, _grid.Center(cell));

        /// <summary>A beast with no enemy standing inside its territory leaves the fight for it.</summary>
        private bool Withdraws(Combatant c)
        {
            if (!c.Beast || c.Range <= 0f)
            {
                return false;
            }

            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side && Keeps(c, f.Position))
                {
                    return false;
                }
            }

            Depart(c, Departure.Withdrew);
            return true;
        }

        /// <summary>A pack breaks when its first beast falls: the rest of it flee, standing or still to join.</summary>
        private void Rout()
        {
            if (_routing)
            {
                return;
            }

            _fleeing.Clear();
            foreach (var fallen in _order)
            {
                if (fallen.Pack < 0 || !fallen.Fighter.Down)
                {
                    continue;
                }

                Mates(fallen, _order);
                Mates(fallen, _joining);
            }

            if (_fleeing.Count == 0)
            {
                return;
            }

            _routing = true;
            try
            {
                foreach (var f in _fleeing.ToArray())
                {
                    Depart(f, Departure.Fled);
                }
            }
            finally
            {
                _routing = false;
            }
        }

        private void Mates(Combatant fallen, List<Combatant> among)
        {
            foreach (var f in among)
            {
                if (f != fallen && f.Pack == fallen.Pack && !f.Fighter.Down && f.State != CombatantState.Out && !_fleeing.Contains(f))
                {
                    _fleeing.Add(f);
                }
            }
        }
    }
}
