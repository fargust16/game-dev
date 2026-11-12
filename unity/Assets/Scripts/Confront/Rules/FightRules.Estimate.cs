using System;
using System.Collections.Generic;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// 21 §8's Force Estimate on Combat Numbers V0, as the slice's residents weigh a fight (10.2 §5a, PB6): each side's
    /// fighters' kills per turn of the other side, summed, the first side's over the second's. Above 1, the first should
    /// win. A blow is an ordinary one through armour, a firearm's from beyond the next cell; place and cover are left out.
    /// </summary>
    public sealed partial class FightRules
    {
        /// <summary>One fighter's kills per turn of another: its blows in a turn times a blow's mean harm, over the target's HP.</summary>
        public double KillsPerTurn(Fighter attacker, Fighter target)
        {
            if (attacker.Weapon == null || attacker.Down || target.Down)
            {
                return 0d;
            }

            var forecast = Forecast(attacker, target, new StrikeContext { Beyond = attacker.Weapon.Ranged });
            var blows = Math.Max(1, FullAp(attacker) / Math.Max(1, attacker.BlowAp));
            return blows * forecast.Chance * (forecast.Low + forecast.High) * 0.5d / Math.Max(1d, target.Hp);
        }

        /// <summary>The Force Estimate of one side against another; without bound against a side that can do no harm.</summary>
        public double ForceEstimate(IReadOnlyList<Fighter> ours, IReadOnlyList<Fighter> theirs)
        {
            var against = Kills(theirs, ours);
            return against <= 0d ? double.PositiveInfinity : Kills(ours, theirs) / against;
        }

        private double Kills(IReadOnlyList<Fighter> side, IReadOnlyList<Fighter> others)
        {
            if (others.Count == 0)
            {
                return 0d;
            }

            var sum = 0d;
            foreach (var a in side)
            {
                foreach (var t in others)
                {
                    sum += KillsPerTurn(a, t);
                }
            }

            return sum / others.Count;
        }
    }
}
