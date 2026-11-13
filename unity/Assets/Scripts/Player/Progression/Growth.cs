using System;

namespace GameDev.Player.Progression
{
    /// <summary>
    /// Progression Numbers V0 (07.1 §3; owner, 2026-11-13, S5-1): every use of a skill gives XP to it and to the
    /// attributes in its formula, a failure half and a critical success twice a normal success, the zero floor the only
    /// brake. The numbers are authoring defaults checked by <c>tools/balance/progress_sim.py</c>; the code follows it.
    /// </summary>
    public static class Growth
    {
        /// <summary>
        /// One use by a character the system knows only by its values: a sheet is trained, anything else is not, as an
        /// NPC is made as it is (07.1 §3).
        /// </summary>
        public static void Train(this ICharacterValues character, CharacterSkill? skill, int taskStage, CheckOutcome outcome,
            System.Collections.Generic.IReadOnlyList<CharacterAttribute> primary, System.Collections.Generic.IReadOnlyList<CharacterAttribute> secondary = null)
        {
            if (character is CharacterSheet sheet)
            {
                sheet.Train(skill, taskStage, outcome, primary, secondary);
            }
        }

        /// <summary>Ordinary growth ends here; 100 stands apart (07.1 §1).</summary>
        public const int Top = 99;

        /// <summary>A primary attribute takes a hundredth of what its skill would take (07.1 §3).</summary>
        public const double AttributeShare = 0.01;

        /// <summary>A secondary attribute takes half a primary's share (authoring default, 2026-11-13).</summary>
        public const double SecondaryShare = 0.005;

        /// <summary>Each stage a task stands above the track keeps 0.8 of the matched award.</summary>
        public const double Above = 0.8;

        /// <summary>The XP full exhaustion gives Will, once an episode (07.2 §3.3).</summary>
        public const double ExhaustionWillXp = 2;

        /// <summary>The XP a Hidden Cache found gives Luck, beside Perception's check (07.1 §3).</summary>
        public const double CacheLuckXp = 2;

        /// <summary>The XP a searched body's recovery roll gives Luck, its only attribute (19 §6; S5c.7).</summary>
        public const double RecoveryLuckXp = 2;

        /// <summary>Levels 1–9 are stage 9, 10–19 stage 8, and so on to 90–99 at stage 0.</summary>
        public static int Stage(int level) => 9 - Math.Min(9, Math.Max(0, level) / 10);

        /// <summary>10 at stage 9, 20 at stage 8, 5,120 at stage 0.</summary>
        public static double BaseXp(int stage) => 10 * Math.Pow(2, 9 - Math.Clamp(stage, 0, 9));

        /// <summary>A task at or below the track: 1 at Δ 0, 0.4 at −1, 0.1 at −2, nothing at −3 or lower.</summary>
        public static double Below(int delta) => delta switch
        {
            >= 0 => 1,
            -1 => 0.4,
            -2 => 0.1,
            _ => 0
        };

        public static double Of(CheckOutcome outcome) => outcome switch
        {
            CheckOutcome.Failure => 0.5,
            CheckOutcome.Critical => 2,
            _ => 1
        };

        /// <summary>A Reaction Check's three results are §8's three outcomes (07.1 §8a).</summary>
        public static CheckOutcome Of(ReactionResult result) => result switch
        {
            ReactionResult.Perfect => CheckOutcome.Critical,
            ReactionResult.Good => CheckOutcome.Normal,
            _ => CheckOutcome.Failure
        };

        /// <summary>
        /// The award for one use of a track at <paramref name="level"/> on a task of <paramref name="taskStage"/>.
        /// Δ is the track's stage minus the task's, positive when the task is harder; a harder task gives the matched
        /// award × 0.8 a stage, so never more, and an easier one its own BaseXP × K.
        /// </summary>
        public static double Award(int level, int taskStage, CheckOutcome outcome, double share = 1)
        {
            var track = Stage(level);
            var task = Math.Clamp(taskStage, 0, 9);
            var delta = track - task;
            var xp = delta > 0 ? BaseXp(track) * Math.Pow(Above, delta) : BaseXp(task) * Below(delta);
            return xp * Of(outcome) * share;
        }

        /// <summary>XP from <paramref name="level"/> to the next.</summary>
        public static double ToNext(int level) => 10 * Math.Pow(1.1, Math.Max(1, level) - 1);

        /// <summary>A check's difficulty read on the bands of §1: 1–9 stage 9, 10–19 stage 8, and so on.</summary>
        public static int StageOfDifficulty(int difficulty) => Stage(difficulty);

        /// <summary>
        /// An attack's or targeted ability's stage is its target's Limit: 9 for an Uninitiated person or an ordinary
        /// beast (null), 8 for a glowing beast (authoring default, 2026-11-13).
        /// </summary>
        public static int StageOfTarget(int? limit, bool glowing = false) =>
            limit.HasValue ? Math.Clamp(limit.Value, 0, 9) : glowing ? 8 : 9;
    }
}
