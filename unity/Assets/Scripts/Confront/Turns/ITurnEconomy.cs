using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using Random = System.Random;

namespace GameDev.Confront.Turns
{
    public enum IntentKind
    {
        Move,
        Attack,
        EndTurn,
        Wait,
        Stance,
        ChangeSet,
    }

    /// <summary>
    /// What the acting combatant does next: walk to a cell, strike a combatant, end its turn, wait to the end of the
    /// round, end its turn in a Defensive Stance, or change its Weapon Set (10.2 §5b, S3a.6).
    /// </summary>
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

        /// <summary>Hands the turn to the end of the round, once a round, the AP kept.</summary>
        public static TurnIntent Wait => new TurnIntent(IntentKind.Wait, default, -1);

        /// <summary>Ends the turn in a Defensive Stance, if it has not attacked.</summary>
        public static TurnIntent Stance => new TurnIntent(IntentKind.Stance, default, -1);

        /// <summary>The second Weapon Set to hand, for its AP.</summary>
        public static TurnIntent ChangeSet => new TurnIntent(IntentKind.ChangeSet, default, -1);
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

        /// <summary>
        /// The fewest steps the attacker takes to a cell from which its weapon reaches the target, the cells in
        /// <paramref name="path"/>: 0 when it reaches it now, −1 when there is no way or no blow (S3a.5).
        /// </summary>
        int PathToStrike(Combatant attacker, Combatant target, List<CombatCell> path);

        /// <summary>
        /// What the place adds to the attacker's blow or shot from a cell at the target (10.2 §5b Position, Firearms):
        /// Surrounded, higher ground, and for a shot the Point-Blank Shot and the target's cover (S3a.6).
        /// </summary>
        StrikeContext ContextFor(Combatant attacker, CombatCell from, Combatant target);

        /// <summary>Whether the attacker's weapon reaches the target from a cell: across a side for a blow, in range and in sight for a shot.</summary>
        bool ReachesFrom(Combatant attacker, CombatCell from, Combatant target);

        /// <summary>A combatant who entered the grid; it comes in at the next round.</summary>
        void Join(Combatant combatant);

        /// <summary>A combatant who left the grid; it drops out at once.</summary>
        void Leave(Combatant combatant);

        event Action<Combatant> TurnAdvanced;

        /// <summary>Raised with each round as it ends, the last one included, before the next begins.</summary>
        event Action<int> RoundEnded;

        /// <summary>Raised once when the encounter ends, with the side left standing or −1.</summary>
        event Action<int> Ended;

        /// <summary>Raised after each attack with the attacker, its target and what the blow did, before the end is checked.</summary>
        event Action<Combatant, Combatant, StrikeResult> Struck;

        /// <summary>
        /// Raised after a combatant walks, with the cells it stepped into in order (S3a.5), so the scene can walk its body
        /// along them. The list is the economy's own and changes with the next walk: read it at once.
        /// </summary>
        event Action<Combatant, IReadOnlyList<CombatCell>> Walked;

        /// <summary>Raised when a combatant changes its Weapon Set, or takes or leaves a Defensive Stance, so the scene shows its guard.</summary>
        event Action<Combatant> GuardChanged;
    }
}
