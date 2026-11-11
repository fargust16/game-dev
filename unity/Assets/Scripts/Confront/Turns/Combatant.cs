using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using UnityEngine;

namespace GameDev.Confront.Turns
{
    /// <summary>Where a combatant stands toward a fight: waiting to join at the next round, in it, or out of it.</summary>
    public enum CombatantState
    {
        Joining,
        Fighting,
        Out,
    }

    /// <summary>How a combatant left a fight before its end, or <see cref="None"/> (08 §7; 10.2 §9; S3a.9).</summary>
    public enum Departure
    {
        None,
        BrokeAway,
        Spared,
        Yielded,

        /// <summary>A beast whose pack broke when its first fell (10.2 *Beasts of the slice*, S3b.2).</summary>
        Fled,

        /// <summary>A beast with no enemy left inside its territory, gone back to it (10.2, S3b.2).</summary>
        Withdrew,
    }

    /// <summary>
    /// One fighter in an encounter (17 C.5): its <see cref="Rules.Fighter"/>, its side, and its place. Every two sides
    /// are hostile (S3a.4 authoring default, until factions' standing decides it). The economy moves it a cell at a time
    /// and writes its <see cref="Position"/>, which the scene mirrors.
    /// </summary>
    public sealed class Combatant
    {
        public Combatant(int id, int side, Fighter fighter, Vector3 position, bool takesIntents = false)
        {
            Id = id;
            Side = side;
            Fighter = fighter;
            Position = position;
            TakesIntents = takesIntents;
        }

        /// <summary>The occupant id on the grid, 0 or more, unique in the encounter.</summary>
        public int Id { get; }

        public int Side { get; }

        public Fighter Fighter { get; }

        /// <summary>True when its turns wait for <see cref="ITurnEconomy.SubmitIntent"/>, as the player's will from S3a.5.</summary>
        public bool TakesIntents { get; }

        /// <summary>Its place in the world: the centre of its cell while it fights.</summary>
        public Vector3 Position { get; set; }

        public CombatCell Cell { get; internal set; }

        public CombatantState State { get; internal set; } = CombatantState.Joining;

        public double Initiative { get; internal set; }

        internal double Tie { get; set; }

        public bool Standing => State == CombatantState.Fighting && !Fighter.Down;

        /// <summary>It has handed its turn to the end of this round (10.2 §5b Wait), once a round.</summary>
        public bool Waited { get; internal set; }

        /// <summary>It waits now: its turn has not yet come back.</summary>
        public bool Waiting { get; internal set; }

        /// <summary>It has attacked in this turn, so it cannot take a Defensive Stance.</summary>
        public bool Attacked { get; internal set; }

        /// <summary>It gave its Reaction Attack since its own turn began, once a round from each enemy (10.2 §5b Position).</summary>
        internal bool Reacted { get; set; }

        /// <summary>The AP it kept when it waited, which reactions spend before its turn comes back.</summary>
        internal int HeldAp { get; set; }

        /// <summary>
        /// The way it faces, one cell's step along X and Z: the way it last moved or struck (S3a.8). The Vision Cones of
        /// 10.3 start from it; zero until it has done either.
        /// </summary>
        public Vector2Int Facing { get; internal set; }

        /// <summary>It has fired in this fight, so a firearm carried to open the fight gives way to its melee set (10.2 §5b).</summary>
        internal bool Fired { get; set; }

        /// <summary>The enemy its Target Rules chose this turn, kept while the enemy stands (10.2 §5a).</summary>
        internal Combatant Intent { get; set; }

        /// <summary>It has taken the First Aid lesson, which an antidote and a splint need (10.2, S3a.7).</summary>
        public bool KnowsFirstAid { get; set; }

        /// <summary>The bandages, antidotes and splints it carries, by <see cref="Rules.Treatment"/>; a treatment that works uses one.</summary>
        public int[] Supplies { get; } = new int[3];

        /// <summary>
        /// It is of people who bargain (10.2 §9): it accepts a Yield and may ask for mercy. Never the cult, which takes no
        /// surrender, nor beasts, which do not bargain (S3a.9).
        /// </summary>
        public bool AcceptsYield { get; set; }

        /// <summary>It has asked for mercy in this fight (08 §7): it strikes no more and waits to be spared or not.</summary>
        public bool AsksMercy { get; internal set; }

        /// <summary>How it left the fight before its end, if it did.</summary>
        public Departure Departure { get; internal set; }

        /// <summary>It loses its turns or has asked for mercy, so Target Rules weigh it as no threat (10.2 §5a).</summary>
        internal bool Helpless => Fighter.LostTurns > 0 || AsksMercy;

        /// <summary>
        /// In Stealth Mode (10.3 §8): enemies do not target it, and its next attack is a stealth attack. Set before the
        /// encounter begins for a fighter that enters it hidden; the economy sets it after.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>The pack it runs with, or −1 (10.2 *Beasts of the slice*): when the first of a pack falls, the rest flee.</summary>
        public int Pack { get; set; } = -1;

        /// <summary>A beast (10.2): it takes no Wait and no Defensive Stance, and keeps to its territory.</summary>
        public bool Beast { get; set; }

        /// <summary>The centre of the territory it keeps to, with <see cref="Range"/>.</summary>
        public Vector3 Home { get; set; }

        /// <summary>How far from <see cref="Home"/> it goes, in metres on the ground; 0 for no territory.</summary>
        public float Range { get; set; }
    }
}
