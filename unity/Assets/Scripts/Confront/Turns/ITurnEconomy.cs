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
        Traverse,
        Treat,
        Yield,
        Spare,
        Hide,
    }

    /// <summary>An Environment Action open across the edge to a cell beside (10.1, S3a.7): what it is, where it lands, and its AP and chance.</summary>
    public struct TraverseOption
    {
        public EnvironmentActionKind Kind;
        public EnvironmentActionBand Band;
        public IEnvironmentAction Tag;

        /// <summary>The cell it lands in; a breach lands nowhere and keeps its own cell.</summary>
        public CombatCell Landing;

        /// <summary>Metres from the cell left up to the landing; a failed climb or mantle falls this far.</summary>
        public float Rise;

        public int Ap;
        public double Chance;
    }

    /// <summary>
    /// What the acting combatant does next: walk to a cell, strike a combatant, end its turn, wait to the end of the
    /// round, end its turn in a Defensive Stance, or change its Weapon Set (10.2 §5b, S3a.6).
    /// </summary>
    public readonly struct TurnIntent
    {
        private TurnIntent(IntentKind kind, CombatCell cell, int target, Treatment treatment = default, bool? success = null, bool? offHand = null)
        {
            Kind = kind;
            Cell = cell;
            Target = target;
            Treatment = treatment;
            Success = success;
            OffHand = offHand;
        }

        public IntentKind Kind { get; }

        public CombatCell Cell { get; }

        /// <summary>The struck or treated combatant's id.</summary>
        public int Target { get; }

        public Treatment Treatment { get; }

        /// <summary>A treatment's result already decided, by the player's Reaction Check (07.1 §8a); null rolls its Action Check.</summary>
        public bool? Success { get; }

        /// <summary>
        /// 10.2 §5b *Two weapons*: the hand an attack strikes with, true for the other hand's weapon; null lets the turns
        /// choose the hand whose blow opens the most harmful mix of blows the AP left can buy.
        /// </summary>
        public bool? OffHand { get; }

        /// <summary>Entering Stealth Mode for 3 AP when no enemy's cone covers the fighter (10.3 §8, S3b.1).</summary>
        public static TurnIntent Hide => new TurnIntent(IntentKind.Hide, default, -1);

        /// <summary>The Environment Action across the edge to a cell beside (10.1, S3a.7).</summary>
        public static TurnIntent Traverse(CombatCell cell) => new TurnIntent(IntentKind.Traverse, cell, -1);

        /// <summary>
        /// A First Aid treatment of itself or of an ally in the next cell (10.2, S3a.7); <paramref name="success"/> is the
        /// player's Reaction Check's result, and an NPC's treatment, null, rolls its Action Check.
        /// </summary>
        public static TurnIntent Treat(Treatment treatment, int patient, bool? success = null) =>
            new TurnIntent(IntentKind.Treat, default, patient, treatment, success);

        public static TurnIntent MoveTo(CombatCell cell) => new TurnIntent(IntentKind.Move, cell, -1);

        public static TurnIntent Attack(int target, bool? offHand = null) => new TurnIntent(IntentKind.Attack, default, target, offHand: offHand);

        public static TurnIntent EndTurn => new TurnIntent(IntentKind.EndTurn, default, -1);

        /// <summary>Hands the turn to the end of the round, once a round, the AP kept.</summary>
        public static TurnIntent Wait => new TurnIntent(IntentKind.Wait, default, -1);

        /// <summary>Ends the turn in a Defensive Stance, if it has not attacked.</summary>
        public static TurnIntent Stance => new TurnIntent(IntentKind.Stance, default, -1);

        /// <summary>The second Weapon Set to hand, for its AP.</summary>
        public static TurnIntent ChangeSet => new TurnIntent(IntentKind.ChangeSet, default, -1);

        /// <summary>A Yield to people, for its AP: the fight ends at once (10.2 §9, S3a.9).</summary>
        public static TurnIntent Yield => new TurnIntent(IntentKind.Yield, default, -1);

        /// <summary>Spares an enemy that asks for mercy: it leaves the fight (08 §7, S3a.9).</summary>
        public static TurnIntent Spare(int target) => new TurnIntent(IntentKind.Spare, default, target);
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

        /// <summary>The AP a walk of this many steps costs the fighter: 3 for two cells crouching (10.3 §8, S3b.1).</summary>
        int WalkAp(Combatant c, int steps);

        void BeginEncounter(IReadOnlyList<Combatant> combatants, Random rolls);

        /// <summary>An encounter opened by a hidden fighter's stealth attack before Initiative (10.3 §7).</summary>
        void BeginEncounter(IReadOnlyList<Combatant> combatants, Random rolls, Combatant opener, Combatant target);

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

        /// <summary>10.2 §5b *Two weapons*: whether a fighter holding two weapons would strike from a cell with the other hand's weapon, with the AP given left.</summary>
        bool OffHandFor(Combatant c, Combatant target, CombatCell from, int ap);

        /// <summary>The forecast of a blow from a cell, with the other hand's weapon when asked.</summary>
        StrikeForecast ForecastFor(Combatant c, Combatant target, CombatCell from, bool offHand);

        /// <summary>Whether the attacker's weapon reaches the target from a cell: across a side for a blow, in range and in sight for a shot.</summary>
        bool ReachesFrom(Combatant attacker, CombatCell from, Combatant target);

        /// <summary>The Environment Action open to the combatant across the edge to the cell beside it, if a tagged block closes that edge (10.1, S3a.7).</summary>
        bool TraverseFrom(Combatant combatant, CombatCell to, out TraverseOption option);

        /// <summary>A combatant who entered the grid; it comes in at the next round.</summary>
        void Join(Combatant combatant);

        /// <summary>A combatant who left the grid; it drops out at once.</summary>
        void Leave(Combatant combatant);

        /// <summary>Ends the encounter at once with no side standing, −1: the place the fight was kept round has left it (S3a.9).</summary>
        void Stop();

        /// <summary>
        /// Raised as a combatant leaves the fight before its end, its <see cref="Combatant.Departure"/> saying how: broken
        /// away, spared, or yielded (S3a.9). A yield ends the fight next.
        /// </summary>
        event Action<Combatant> Departed;

        /// <summary>Raised when a combatant of people asks for mercy (08 §7, S3a.9).</summary>
        event Action<Combatant> MercyAsked;

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

        /// <summary>
        /// Raised after an Environment Action (S3a.7): who, what, whether it worked, and the HP a fall took. A walk to the
        /// landing is told by <see cref="Walked"/> first; a breach that worked leaves the scene to clear the block.
        /// </summary>
        event Action<Combatant, TraverseOption, bool, double> Traversed;

        /// <summary>Raised after a treatment: who treated, whom, with what, and whether it worked (10.2, S3a.7).</summary>
        event Action<Combatant, Combatant, Treatment, bool> Treated;

        /// <summary>Raised at a round's end for each combatant whose conditions did something on that world tick (S3a.7).</summary>
        event Action<Combatant, ConditionTick> Conditioned;
    }
}
