using System;
using System.Collections.Generic;
using GameDev.Player.Standing;
using GameDev.World.Poi;

namespace GameDev.Dialogue
{
    /// <summary>How a conversation ended.</summary>
    public enum DialogueEnd
    {
        /// <summary>An option led nowhere further.</summary>
        Finished,

        /// <summary>The character walked away.</summary>
        Left,

        /// <summary>A Final Check failed against an enemy who can fight (10.4 §6).</summary>
        Fight,
    }

    /// <summary>What a chosen option's check did.</summary>
    public enum CheckResult
    {
        /// <summary>The option was not a check.</summary>
        None,
        Success,
        Failure,
    }

    /// <summary>
    /// A Social Check before it is made: its chance, and what the player reads behind it from Intelligence 20 (10.4 §3).
    /// </summary>
    public readonly struct CheckPreview
    {
        public CheckPreview(double chance, int characterValue, int listenerValue, int difficulty, int attitude, StandingReading standing, int arguments, int modifiers)
        {
            Chance = chance;
            CharacterValue = characterValue;
            ListenerValue = listenerValue;
            Difficulty = difficulty;
            Attitude = attitude;
            Standing = standing;
            Arguments = arguments;
            Modifiers = modifiers;
        }

        public double Chance { get; }
        public int CharacterValue { get; }
        public int ListenerValue { get; }
        public int Difficulty { get; }
        public int Attitude { get; }
        public StandingReading Standing { get; }

        /// <summary>The further Arguments' +1 each, at most +2.</summary>
        public int Arguments { get; }

        /// <summary>The sum of the modifiers, held within ±4.</summary>
        public int Modifiers { get; }
    }

    /// <summary>A running conversation (17 A.3): what is said now, what may be answered, and the answer.</summary>
    public interface IDialogueRunner
    {
        /// <summary>Raised once, when the conversation ends, after its tick.</summary>
        event Action<DialogueEnd> Ended;

        DialogueDefinition Dialogue { get; }

        bool IsOpen { get; }

        /// <summary>The node shown, or null once ended.</summary>
        DialogueNode Node { get; }

        /// <summary>The tips said on arriving at the node (09; S2.4); with none, the node's own line is said.</summary>
        IReadOnlyList<Tip> Said { get; }

        /// <summary>The News items said after the tips on arriving at the node (09 § News Pool; S5c.7), each read when shown.</summary>
        IReadOnlyList<Func<string>> NewsSaid { get; }

        /// <summary>How it ended, once it has.</summary>
        DialogueEnd? EndedHow { get; }

        /// <summary>The listener a Confront or a failed Final Check turned into a fight, or null.</summary>
        DialogueListener FightWith { get; }

        /// <summary>The option whose fight has not yet been decided, or null (S3b.3).</summary>
        DialogueOption Fought { get; }

        /// <summary>The options offered now, in their order.</summary>
        IReadOnlyList<DialogueOption> Options { get; }

        CheckPreview Preview(DialogueOption option);

        CheckResult Choose(DialogueOption option);

        void Leave();

        /// <summary>
        /// The fight the conversation ended in is decided: writes its option's Won when the listener's side was beaten,
        /// its Lost otherwise. Once only.
        /// </summary>
        void AfterFight(bool won);
    }
}
