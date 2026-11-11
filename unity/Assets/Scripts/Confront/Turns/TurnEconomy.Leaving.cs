using System;
using GameDev.Confront.Grid;

namespace GameDev.Confront.Turns
{
    /// <summary>
    /// S3a.9: leaving a fight (08 §7; 10.2 §9). A step that ends <see cref="Rules.FightRules.BreakAwayCells"/> or more from
    /// every enemy, and farther from the nearest than the fighter stood, breaks away: the fighter is out of the fight,
    /// the Reaction Attacks it drew on the way already struck. A Yield to people costs its AP and ends the fight at once;
    /// people only, so a Yield with the cult or a beast among the enemies standing is refused. A fighter of people asks
    /// for mercy on its turn below a share of its HP when its side has fewer fighters standing; it strikes no more, and
    /// the other side spares it, and it leaves, or does not. A fighter with no will of its own spares those asking once
    /// nobody else stands against it.
    /// </summary>
    public sealed partial class TurnEconomy
    {
        public event Action<Combatant> Departed;

        public event Action<Combatant> MercyAsked;

        public void Stop() => Finish(-1);

        /// <summary>10.2 §9: a Yield, for its AP, to enemies who all accept one; the fight ends at once.</summary>
        private bool Yield(Combatant c)
        {
            if (ApLeft < _rules.YieldAp)
            {
                return false;
            }

            Foes(c, _foes);
            if (_foes.Count == 0 || !_foes.TrueForAll(f => f.AcceptsYield))
            {
                return false;
            }

            ApLeft -= _rules.YieldAp;
            var side = _foes[0].Side;
            c.Departure = Departure.Yielded;
            Departed?.Invoke(c);
            Finish(side);
            return true;
        }

        /// <summary>08 §7: an enemy asking for mercy is spared, for no AP, and leaves the fight.</summary>
        private bool Spare(Combatant c, Combatant asking)
        {
            if (asking == null || !asking.Standing || !asking.AsksMercy || asking.Side == c.Side)
            {
                return false;
            }

            Depart(asking, Departure.Spared);
            return true;
        }

        /// <summary>A combatant leaves the fight before its end: told first, then out, the end checked.</summary>
        private void Depart(Combatant c, Departure departure)
        {
            c.Departure = departure;
            Departed?.Invoke(c);
            Leave(c);
        }

        /// <summary>Whether the step from <paramref name="from"/> to the fighter's cell broke away from every enemy.</summary>
        private bool BreaksAway(Combatant c, CombatCell from)
        {
            if (!c.Standing)
            {
                return false;
            }

            var now = NearestFoe(c, c.Cell);
            return now >= _rules.BreakAwayCells && now != int.MaxValue && now > NearestFoe(c, from);
        }

        /// <summary>Cells, as moves count them, from a cell to the nearest enemy standing; int.MaxValue with none.</summary>
        private int NearestFoe(Combatant c, CombatCell cell)
        {
            var nearest = int.MaxValue;
            foreach (var f in _order)
            {
                if (f.Standing && f.Side != c.Side)
                {
                    nearest = Math.Min(nearest, Distance(cell, f.Cell));
                }
            }

            return nearest;
        }

        /// <summary>
        /// 08 §7 in the slice: a fighter of people below the mercy share of its HP, its side fewer than the enemies
        /// standing, those who have asked counted out on both sides.
        /// </summary>
        private bool AsksForMercy(Combatant c)
        {
            if (!c.AcceptsYield || c.AsksMercy || c.Fighter.Hp >= c.Fighter.MaxHp * _rules.MercyShare)
            {
                return false;
            }

            var ours = 0;
            var theirs = 0;
            foreach (var f in _order)
            {
                if (!f.Standing || f.AsksMercy)
                {
                    continue;
                }

                if (f.Side == c.Side)
                {
                    ours++;
                }
                else
                {
                    theirs++;
                }
            }

            return ours < theirs;
        }

        /// <summary>A fighter with no will of its own spares every enemy asking for mercy once nobody else stands against it.</summary>
        private bool SparesTheAsking(Combatant c)
        {
            Foes(c, _foes);
            if (_foes.Count == 0 || !_foes.TrueForAll(f => f.AsksMercy))
            {
                return false;
            }

            var asking = _foes.ToArray();
            foreach (var f in asking)
            {
                if (IsRunning && f.Standing)
                {
                    Depart(f, Departure.Spared);
                }
            }

            return true;
        }
    }
}
