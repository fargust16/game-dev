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
    }
}
