using System;
using System.Collections.Generic;

namespace GameDev.Player.Progression
{
    /// <summary>The three outcomes of an action check (07.1 §8).</summary>
    public enum CheckOutcome
    {
        Failure,
        Normal,
        Critical,
    }

    /// <summary>
    /// The chances of one action check: of any success, of a critical one given success, and the overflow above the
    /// critical cap that the checking system may turn into potency (07.1 §7–8).
    /// </summary>
    public readonly struct ActionCheckChances
    {
        public ActionCheckChances(double success, double critical, double overflow)
        {
            Success = success;
            Critical = critical;
            Overflow = overflow;
        }

        public double Success { get; }

        /// <summary>The chance that a success is critical; zero where the action has no critical outcome.</summary>
        public double Critical { get; }

        public double Overflow { get; }

        public double CriticalOutcome => Success * Critical;
        public double NormalOutcome => Success * (1d - Critical);
        public double Failure => 1d - Success;
    }

    /// <summary>
    /// The shared action check of 07.1 (§4–8), used by every check but the Social Checks of 10.4: preparation against
    /// difficulty on the curve, secondary attributes, Luck, and the three outcomes from one draw.
    /// </summary>
    public static class ActionCheck
    {
        /// <summary>07.1 §4: the primary attribute and skill weigh the same. A check without a skill passes the attribute alone.</summary>
        public static double Preparation(int attribute, int skill) => (attribute + skill) / 2d;

        /// <summary>07.1 §4: the curve at Δ = preparation − difficulty, linear between whole Δ, flat beyond its ends.</summary>
        public static double BaseChance(ActionCheckRules rules, double delta)
        {
            var curve = rules.Curve;
            var at = delta - rules.FirstDelta;
            if (at <= 0d)
            {
                return curve[0];
            }

            if (at >= curve.Count - 1)
            {
                return curve[curve.Count - 1];
            }

            var low = (int)Math.Floor(at);
            return curve[low] + (curve[low + 1] - curve[low]) * (at - low);
        }

        /// <summary>07.1 §5: the mean of each secondary attribute's share of the difficulty, as a share of the most they add.</summary>
        public static double SecondaryBonus(ActionCheckRules rules, int difficulty, IReadOnlyList<int> secondaries)
        {
            if (secondaries == null || secondaries.Count == 0)
            {
                return 0d;
            }

            var sum = 0d;
            foreach (var level in secondaries)
            {
                sum += difficulty <= 0 ? 1d : Math.Min(1d, (double)level / difficulty);
            }

            return rules.SecondaryBonus * sum / secondaries.Count;
        }

        /// <summary>07.1 §6: Luck multiplies a chance, Luck 100 doubling it, and nothing passes the cap.</summary>
        public static double ApplyLuck(double chance, int luck, double cap) => Math.Min(cap, chance * (1d + luck / 100d));

        /// <summary>07.1 §7: the critical chance by Perception, times Luck, before its cap.</summary>
        public static double RawCritical(ActionCheckRules rules, int perception, int luck)
        {
            var share = (perception - CharacterSheet.Lowest) / (double)(CharacterSheet.Highest - CharacterSheet.Lowest);
            var byPerception = rules.CriticalAtLowest + (rules.CriticalAtHighest - rules.CriticalAtLowest) * share;
            return byPerception * (1d + luck / 100d);
        }

        public static ActionCheckChances Chances(
            ActionCheckRules rules,
            double preparation,
            int difficulty,
            int perception,
            int luck,
            bool hasCritical = true,
            IReadOnlyList<int> secondaries = null)
        {
            var chance = BaseChance(rules, preparation - difficulty) * (1d + SecondaryBonus(rules, difficulty, secondaries));
            var success = ApplyLuck(chance, luck, rules.ChanceCap);
            if (!hasCritical)
            {
                return new ActionCheckChances(success, 0d, 0d);
            }

            var raw = RawCritical(rules, perception, luck);
            return new ActionCheckChances(success, Math.Min(rules.CriticalCap, raw), Math.Max(0d, raw - rules.CriticalCap));
        }

        /// <summary>07.1 §8: one draw decides the outcome; a critical is part of success, not a second roll.</summary>
        public static CheckOutcome Roll(ActionCheckChances chances, Random rolls)
        {
            var draw = rolls.NextDouble();
            if (draw < chances.CriticalOutcome)
            {
                return CheckOutcome.Critical;
            }

            return draw < chances.Success ? CheckOutcome.Normal : CheckOutcome.Failure;
        }
    }
}
