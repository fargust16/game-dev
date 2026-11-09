using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using Random = System.Random;

namespace GameDev.Confront.Turns
{
    public enum IntentKind
    {
        Move,
        Attack,
        EndTurn,
    }

    /// <summary>What the acting combatant does next: walk to a cell, strike a combatant, or end its turn.</summary>
    public readonly struct TurnIntent
    {
        private TurnIntent(IntentKind kind, CombatCell cell, int target)
        {
            Kind = kind;
            Cell = cell;
            Target = target;
        }

        public IntentKind Kind { get; }

        public CombatCell Cell { get; }

        /// <summary>The struck combatant's id.</summary>
        public int Target { get; }

        public static TurnIntent MoveTo(CombatCell cell) => new TurnIntent(IntentKind.Move, cell, -1);

        public static TurnIntent Attack(int target) => new TurnIntent(IntentKind.Attack, default, target);

        public static TurnIntent EndTurn => new TurnIntent(IntentKind.EndTurn, default, -1);
    }

    /// <summary>
    /// The fight's turns (17 C.5; 10.2 §5, §5b): Initiative sets the order, each turn spends AP on moving and striking,
    /// and what is left stays for reactions. A round ends when every combatant in it has acted; a combatant who drops
    /// out leaves at once, and one who joins comes in at the next round. The encounter ends when no two hostile sides
    /// stand in it.
    /// </summary>
    public interface ITurnEconomy
    {
        bool IsRunning { get; }

        /// <summary>The round under way, from 1.</summary>
        int Round { get; }

        /// <summary>The combatant whose turn it is, or null between encounters.</summary>
        Combatant Acting { get; }

        /// <summary>The acting combatant's AP left in this turn.</summary>
        int ApLeft { get; }

        /// <summary>Every combatant in the fight, in the order Initiative set.</summary>
        IReadOnlyList<Combatant> Order { get; }

        void BeginEncounter(IReadOnlyList<Combatant> combatants, Random rolls);

        /// <summary>The acting combatant's next intent; false when it cannot be done, and nothing is spent.</summary>
        bool SubmitIntent(TurnIntent intent);

        /// <summary>Plays the acting combatant's whole turn by the rule that stands until Target Rules V0 (S3a.8).</summary>
        void PlayTurn();

        /// <summary>A combatant who entered the grid; it comes in at the next round.</summary>
        void Join(Combatant combatant);

        /// <summary>A combatant who left the grid; it drops out at once.</summary>
        void Leave(Combatant combatant);

        event Action<Combatant> TurnAdvanced;

        /// <summary>Raised with each round as it ends, the last one included, before the next begins.</summary>
        event Action<int> RoundEnded;

        /// <summary>Raised once when the encounter ends, with the side left standing or −1.</summary>
        event Action<int> Ended;
    }
}
