using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>What a room of a dwelling is for; it chooses the furniture (VICTORIAN_INTERIORS_PLAN §4.2).</summary>
    public enum RoomKind
    {
        Hall,
        Parlour,
        Kitchen,
        Bedroom,
        Room,
        ShopRoom,
        BackRoom,
        DiningRoom,
        Study,
        Store,
        Office,
        DrawingRoom,
        Scullery,
        Bathroom,
        WC,
        DressingRoom,
        Nursery,
        ServantRoom,
        BoxRoom,
        Landing,
        Pantry,
    }

    /// <summary>What closes a wall segment of one module.</summary>
    public enum Opening
    {
        Wall,
        Door,   // an open doorway between rooms
        Street, // the doorway to the street, with the portal back
        Window,
    }

    /// <summary>How a piece of furniture is placed: by its footprint's corner, against a wall, or hung on a wall's face.</summary>
    public enum PlacementMode
    {
        Put,
        Against,
        Hang,
    }

    /// <summary>The wall finishes of Victorian Interiors (plan §4): patterned paper, the plain dark finish, panelling.</summary>
    public enum Finish
    {
        Paper,
        Dark,
        Panel,
    }

    public readonly struct DwellingRoom
    {
        public DwellingRoom(string name, RoomKind kind, int x, int z, int width, int depth, int level, Finish finish)
        {
            Name = name;
            Kind = kind;
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
            Level = level;
            Finish = finish;
        }

        public string Name { get; }
        public RoomKind Kind { get; }
        public int X { get; }
        public int Z { get; }
        public int Width { get; }
        public int Depth { get; }

        /// <summary>The storey, 0 on the ground; its floor stands <see cref="DwellingPlan.Storey"/> higher for each.</summary>
        public int Level { get; }

        public Finish Finish { get; }

        public bool Contains(float x, float z) => x > X && x < X + Width && z > Z && z < Z + Depth;
    }

    /// <summary>
    /// A stair well: the floor tiles left out of storey <see cref="Level"/>, 6 × 3 m along X, over the stair that climbs to
    /// it from the storey below, toward +X when <see cref="ClimbsEast"/>, else toward −X (VI-3's rule, plan §8).
    /// </summary>
    public readonly struct DwellingWell
    {
        public DwellingWell(int x, int z, int level, bool climbsEast)
        {
            X = x;
            Z = z;
            Level = level;
            ClimbsEast = climbsEast;
        }

        public const int Width = 6, Depth = 3;

        public int X { get; }
        public int Z { get; }
        public int Level { get; }
        public bool ClimbsEast { get; }

        public bool Holds(int cx, int cz, int level) => level == Level && cx >= X && cx < X + Width && cz >= Z && cz < Z + Depth;
    }

    /// <summary>
    /// One piece of furniture on storey <see cref="Level"/>. <see cref="PlacementMode.Put"/>: the turned footprint's
    /// south-west corner at (X, Z). <see cref="PlacementMode.Against"/>: the back on the wall line <see cref="Line"/> (an
    /// x for east and west, a z for north and south), facing into the room, from <see cref="Along"/> along the wall.
    /// <see cref="PlacementMode.Hang"/>: on the wall's face at height <see cref="Y"/>, spanning from <see cref="Along"/>.
    /// </summary>
    public readonly struct Placement
    {
        public Placement(string key, PlacementMode mode, int x, int z, int turns, Vector2Int facing, int line, int along, float y, bool stash, int level = 0, string name = null)
        {
            Key = key;
            Mode = mode;
            X = x;
            Z = z;
            Turns = turns;
            Facing = facing;
            Line = line;
            Along = along;
            Y = y;
            Stash = stash;
            Level = level;
            Name = name;
        }

        public string Key { get; }
        public PlacementMode Mode { get; }
        public int X { get; }
        public int Z { get; }
        public int Turns { get; }
        public Vector2Int Facing { get; }
        public int Line { get; }
        public int Along { get; }
        public float Y { get; }

        /// <summary>The household's Stash (19 §6.3), a chest whose contents come from who lives there.</summary>
        public bool Stash { get; }

        public int Level { get; }

        /// <summary>The block's name in the scene when it has one of its own ("Stair", "Balustrade"), else null.</summary>
        public string Name { get; }
    }

    /// <summary>
    /// A dwelling's rooms on Victorian Interiors' 3 m module, storey by storey (VICTORIAN_INTERIORS_PLAN §3, §4), the
    /// openings of its wall segments, its stair wells, and its furniture. Plain data: the planner writes it from a key
    /// and a seed stream, and the assembler builds it from the kit.
    /// </summary>
    public sealed class DwellingPlan
    {
        public const int Module = 3;
        public const float Storey = 3.5f;

        public readonly List<DwellingRoom> Rooms = new List<DwellingRoom>();

        /// <summary>The segment of one module from node (X, Z) along +X, or along +Z, on a storey.</summary>
        public readonly Dictionary<(int X, int Z, bool AlongZ, int Level), Opening> Openings = new Dictionary<(int, int, bool, int), Opening>();

        public readonly List<DwellingWell> Wells = new List<DwellingWell>();

        public readonly List<Placement> Furniture = new List<Placement>();

        /// <summary>Furniture the rooms call for that found no room, as "room: key"; for review, never shown in play.</summary>
        public readonly List<string> Unplaced = new List<string>();

        /// <summary>The street segment's start; it always runs along +X on the front, z = 0, on the ground floor.</summary>
        public (int X, int Z) Street;

        public int Width { get; set; }

        public int Floors { get; private set; } = 1;

        public bool HasStash => Furniture.Exists(f => f.Stash);

        public DwellingPlan Room(string name, RoomKind kind, int x, int z, int width, int depth, Finish finish, int level = 0)
        {
            Rooms.Add(new DwellingRoom(name, kind, x, z, width, depth, level, finish));
            Width = Mathf.Max(Width, x + width);
            Floors = Mathf.Max(Floors, level + 1);
            return this;
        }

        public DwellingPlan Open(Opening kind, int x, int z, bool alongZ = false, int level = 0)
        {
            Openings[(x, z, alongZ, level)] = kind;
            if (kind == Opening.Street)
            {
                Street = (x, z);
            }

            return this;
        }

        public DwellingPlan Well(int x, int z, int level, bool climbsEast)
        {
            Wells.Add(new DwellingWell(x, z, level, climbsEast));
            return this;
        }

        public Opening OpeningAt(int x, int z, bool alongZ, int level) =>
            Openings.TryGetValue((x, z, alongZ, level), out var kind) ? kind : Opening.Wall;

        /// <summary>True when the module cell at (cx, cz) on <paramref name="level"/> is open to the storey below.</summary>
        public bool InWell(int cx, int cz, int level) => Wells.Exists(w => w.Holds(cx, cz, level));
    }
}
