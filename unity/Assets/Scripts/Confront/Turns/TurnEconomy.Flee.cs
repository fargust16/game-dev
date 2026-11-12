using System.Collections.Generic;
using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// 10.2 §5a, residents in a fight (owner, 2026-11-12; PB6): a fighter with a courage, <see cref="Combatant.FightsAt"/>,
    /// weighs 21 §8's Force Estimate at the start of each of its turns. Below it, it strikes no more: it walks away from
    /// the nearest enemy, toward the help it runs for if it has any, keeping the AP of Сбежать, and breaks away as soon as
    /// it may (§9). The estimate counts everyone standing who is not helpless.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        private readonly List<Fighter> _ours = new List<Fighter>();
        private readonly List<Fighter> _theirs = new List<Fighter>();

        /// <summary>21 §8's Force Estimate of the fighter's side against everyone standing on the others, as the fight stands.</summary>
        public double ForceEstimate(Combatant c)
        {
            _ours.Clear();
            _theirs.Clear();
            foreach (var f in _order)
            {
                if (f.Standing && !f.Helpless)
                {
                    (f.Side == c.Side ? _ours : _theirs).Add(f.Fighter);
                }
            }

            return _rules.ForceEstimate(_ours, _theirs);
        }

        /// <summary>Whether the fighter's courage fails it now: its side's estimate is below the one it needs.</summary>
        public bool Flees(Combatant c) => c != null && c.FightsAt.HasValue && c.Standing && ForceEstimate(c) < c.FightsAt.Value;

        private void Flee(Combatant c)
        {
            c.Intent = null;
            if (!BreakAway(c) && !c.Fighter.LegBroken && NearestStandingFoe(c) is Combatant nearest)
            {
                var away = new Vector2(c.Cell.X - nearest.Cell.X, c.Cell.Z - nearest.Cell.Z);
                away = away.sqrMagnitude > 0f ? away.normalized : Vector2.right;
                if (c.HelpAt is Vector3 help)
                {
                    var from = _grid.Center(c.Cell);
                    var to = new Vector2(help.x - from.x, help.z - from.z);
                    var both = to.sqrMagnitude > 1f ? away + to.normalized : away;
                    away = both.sqrMagnitude > 0.01f ? both.normalized : away;
                }

                WalkToward(c, c.Cell.X + Mathf.RoundToInt(away.x * RetreatGoal), c.Cell.Z + Mathf.RoundToInt(away.y * RetreatGoal), ApLeft - _rules.BreakAwayAp);
                if (IsRunning && Acting == c)
                {
                    BreakAway(c);
                }
            }

            if (IsRunning && Acting == c)
            {
                EndTurn(c);
            }
        }

        private Combatant NearestStandingFoe(Combatant c)
        {
            Combatant nearest = null;
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side && !f.Helpless && (nearest == null || Distance(c.Cell, f.Cell) < Distance(c.Cell, nearest.Cell)))
                {
                    nearest = f;
                }
            }

            return nearest;
        }
    }
}
