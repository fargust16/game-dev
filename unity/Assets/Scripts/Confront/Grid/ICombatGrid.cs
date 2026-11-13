using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Confront.Grid
{
    /// <summary>A side of a cell, in the order of the grid's cover bits.</summary>
    public enum GridSide
    {
        PlusX = 0,
        MinusX = 1,
        PlusZ = 2,
        MinusZ = 3
    }

    /// <summary>
    /// 10.1 *Cover and Elevation*: cover counts against ranged attacks only, −15 points to hit behind Half Cover and −30
    /// behind Full Cover (10.2 §5b). A side with something solid at 0.7 m but not at 1.5 m is Half Cover, as a fence;
    /// one with something at 1.5 m is Full Cover, as a wall.
    /// </summary>
    public enum CoverLevel
    {
        None = 0,
        Half = 1,
        Full = 2
    }

    /// <summary>One cell of the grid at one of its surfaces: a column can hold several, as a landing over a hall.</summary>
    public readonly struct CombatCell : IEquatable<CombatCell>
    {
        public readonly int X;
        public readonly int Z;
        public readonly int Level;

        public CombatCell(int x, int z, int level)
        {
            X = x;
            Z = z;
            Level = level;
        }

        public bool Equals(CombatCell other) => X == other.X && Z == other.Z && Level == other.Level;

        public override bool Equals(object obj) => obj is CombatCell other && Equals(other);

        public override int GetHashCode() => (X * 73856093) ^ (Z * 19349663) ^ Level;

        public static bool operator ==(CombatCell a, CombatCell b) => a.Equals(b);

        public static bool operator !=(CombatCell a, CombatCell b) => !a.Equals(b);

        public override string ToString() => $"({X}, {Z}, level {Level})";
    }

    /// <summary>
    /// The Combat Grid during Confront (17 C.2, 10.1): square cells around the player, read from the place and never
    /// from the characters in it, who are a layer of their own. Its queries never allocate.
    /// </summary>
    public interface ICombatGrid
    {
        float CellSize { get; }

        /// <summary>The cell the grid is centred on, the player's.</summary>
        CombatCell Origin { get; }

        /// <summary>Inside the window, at most <see cref="CombatGridReader.Reach"/> cells from the origin on each axis.</summary>
        bool InBounds(int x, int z);

        /// <summary>In bounds, with a surface a person can stand on.</summary>
        bool IsWalkable(CombatCell cell);

        /// <summary>Walkable and joined to the origin by steps.</summary>
        bool IsReachable(CombatCell cell);

        /// <summary>One step to a cell beside, diagonal included, never across a corner.</summary>
        bool CanStep(CombatCell from, CombatCell to);

        CoverLevel CoverOn(CombatCell cell, GridSide side);

        /// <summary>
        /// A clear line from a person standing in one cell to a person standing in the other, chest to chest, past every
        /// body (S3a.6): a shot needs it.
        /// </summary>
        bool CanSee(CombatCell from, CombatCell to);

        /// <summary>
        /// 10.1 *Shot Line*: the line of an attack from a person standing in one cell to a person standing in the other,
        /// to the chest, the head or the hips, solid obstacles stopping it and Partial Obstacles making it Obscured.
        /// </summary>
        ShotLine Shot(CombatCell from, CombatCell to);

        /// <summary>The 10.1 tag of the block that closes the way from one cell toward another.</summary>
        bool TryGetEnvironmentAction(CombatCell from, CombatCell to, out IEnvironmentAction action);

        /// <summary>
        /// Where an Environment Action lands (S3a.7): in the column <paramref name="dx"/>, <paramref name="dz"/> from a cell,
        /// the walkable surface whose rise above the cell lies within <paramref name="lowest"/> and
        /// <paramref name="highest"/> m, the one nearest the cell's own height.
        /// </summary>
        bool TryLand(CombatCell from, int dx, int dz, float lowest, float highest, out CombatCell landing);

        Vector3 Center(CombatCell cell);

        /// <summary>The walkable cell under a position, at the surface within 2 m of its height (S3a.4).</summary>
        bool TryCellAt(Vector3 position, out CombatCell cell);

        /// <summary>The occupant standing in the cell, or −1.</summary>
        int OccupantAt(CombatCell cell);

        /// <summary>Puts an occupant (an id of 0 or more) in a cell, moving it if it stood elsewhere. False when another
        /// occupant holds the cell.</summary>
        bool Occupy(int occupant, CombatCell cell);

        void Vacate(int occupant);

        /// <summary>Recentres the grid on the cell at a position. True when the grid moved.</summary>
        bool Follow(Vector3 position);

        /// <summary>
        /// The fewest steps from one cell to another inside the window, around occupied cells, each step 1 AP with a
        /// diagonal (10.1). Fills <paramref name="path"/>, if given, with the cells after <paramref name="from"/>, and
        /// returns the steps, or −1 when there is no way.
        /// </summary>
        int FindPath(CombatCell from, CombatCell to, List<CombatCell> path);
    }
}
