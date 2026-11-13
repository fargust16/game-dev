using System;

namespace GameDev.Player.Progression
{
    /// <summary>
    /// The Social Check of 10.4 §2, the deliberate exception to 07.1's curve: <c>2d6 + Character Value + Modifiers</c>
    /// against <c>Request + Listener Value</c>, +2 for an enemy; 1 + 1 always fails and 6 + 6 always succeeds, so
    /// the chance lies between 1/36 and 35/36. Luck multiplies it under 07.1 §6, up to the dice's own ceiling, which
    /// is 10.4's "same 97 %". No critical outcome in V1.
    /// </summary>
    public static class SocialCheck
    {
        /// <summary>The chance of anything but 1 + 1.</summary>
        public const double Ceiling = 35d / 36d;

        /// <summary>10.4 §2: the two attributes both sides use for a skill.</summary>
        public static (CharacterAttribute First, CharacterAttribute Second) Attributes(CharacterSkill skill) => skill switch
        {
            CharacterSkill.Persuasion => (CharacterAttribute.Intelligence, CharacterAttribute.Will),
            CharacterSkill.Deception => (CharacterAttribute.Intelligence, CharacterAttribute.Will),
            CharacterSkill.Intimidation => (CharacterAttribute.Strength, CharacterAttribute.Will),
            _ => throw new ArgumentOutOfRangeException(nameof(skill), skill, null),
        };

        /// <summary><c>floor(((A + B) / 2 + Skill) / step)</c>; values are never negative, so whole division floors.</summary>
        public static int CharacterValue(SocialCheckRules rules, ICharacterValues character, CharacterSkill skill)
        {
            var (first, second) = Attributes(skill);
            return (character.Get(first) + character.Get(second) + 2 * character.Get(skill)) / (2 * rules.ValueStep);
        }

        /// <summary><c>floor((A + B) / step)</c>: the listener's own skill does not count.</summary>
        public static int ListenerValue(SocialCheckRules rules, ICharacterValues listener, CharacterSkill skill)
        {
            var (first, second) = Attributes(skill);
            return (listener.Get(first) + listener.Get(second)) / rules.ValueStep;
        }

        public static int Difficulty(SocialCheckRules rules, RequestSize request, int listenerValue, bool enemy) =>
            rules.Request(request) + listenerValue + (enemy ? rules.Enemy : 0);

        /// <summary>The sum of the modifiers, held within the cap.</summary>
        public static int Modifiers(SocialCheckRules rules, int sum) => Math.Clamp(sum, -rules.ModifierCap, rules.ModifierCap);

        /// <summary>The share of the 36 throws that succeed with this bonus.</summary>
        public static double DiceChance(int bonus, int difficulty)
        {
            var wins = 0;
            for (var first = 1; first <= 6; first++)
            {
                for (var second = 1; second <= 6; second++)
                {
                    var sum = first + second;
                    if (sum == 12 || (sum != 2 && sum + bonus >= difficulty))
                    {
                        wins++;
                    }
                }
            }

            return wins / 36d;
        }

        /// <summary>The chance of success, the one the player sees from Intelligence 10 (10.4 §3).</summary>
        public static double Chance(SocialCheckRules rules, int characterValue, int modifiers, int difficulty, int luck) =>
            ActionCheck.ApplyLuck(DiceChance(characterValue + Modifiers(rules, modifiers), difficulty), luck, Ceiling);

        /// <summary>
        /// One draw against the chance, so Luck lifts it as it lifts any chance; without Luck it is the same as
        /// throwing the dice.
        /// </summary>
        // lean-ctx: no dice faces are drawn; when a panel shows them, pick a throw that gives the drawn outcome.
        public static bool Roll(double chance, Random rolls) => rolls.NextDouble() < chance;

        /// <summary>
        /// Every Social Check grows its skill and both its attributes, of equal weight, each the full hundredth; a failure
        /// half, and no critical in V1 (07.1 §3, S5-1). Its difficulty on §1's bands is the listener's own two attributes,
        /// <c>(A + B) / 2</c>, since the check's sum lies on the dice's scale (the agent's reading, S5b.2). A character that
        /// is no sheet, or an NPC's, is not trained.
        /// </summary>
        public static void Train(ICharacterValues character, CharacterSkill skill, ICharacterValues listener, bool success)
        {
            if (character is not CharacterSheet sheet)
            {
                return;
            }

            var (first, second) = Attributes(skill);
            var difficulty = listener == null ? 1 : (listener.Get(first) + listener.Get(second)) / 2;
            sheet.Train(skill, Growth.StageOfDifficulty(difficulty), success ? CheckOutcome.Normal : CheckOutcome.Failure,
                new[] { first, second });
        }
    }
}
