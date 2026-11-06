using System.Collections.Generic;
using UnityEngine;

namespace GameDev.Slice.Dwellings
{
    /// <summary>What a room of a dwelling is for; it chooses the furniture (city plan C1 §8).</summary>
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

    public readonly struct DwellingRoom
    {
        public DwellingRoom(string name, RoomKind kind, int x, int z, int width, int depth, bool stone)
        {
            Name = name;
            Kind = kind;
            X = x;
            Z = z;
            Width = width;
            Depth = depth;
            Stone = stone;
        }

        public string Name { get; }
        public RoomKind Kind { get; }
        public int X { get; }
        public int Z { get; }
        public int Width { get; }
        public int Depth { get; }

        /// <summary>A stone room has a stone floor and brick walls; the rest have wooden floors and plaster.</summary>
        public bool Stone { get; }

        public bool Contains(float x, float z) => x > X && x < X + Width && z > Z && z < Z + Depth;
    }

    /// <summary>
    /// One piece of furniture. <see cref="PlacementMode.Put"/>: the turned footprint's south-west corner at (X, Z).
    /// <see cref="PlacementMode.Against"/>: the back on the wall line <see cref="Line"/> (an x for east and west, a z
    /// for north and south), facing into the room, from <see cref="Along"/> along the wall.
    /// <see cref="PlacementMode.Hang"/>: on the wall's face at height <see cref="Y"/>, spanning from <see cref="Along"/>.
    /// </summary>
    public readonly struct Placement
    {
        public Placement(string key, PlacementMode mode, int x, int z, int turns, Vector2Int facing, int line, int along, float y, bool stash)
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
    }

    /// <summary>
    /// A dwelling's rooms on BK's 4 m module (owner's play test, 2026-11-06), the openings of its wall segments, and its furniture. Plain data: the
    /// planner writes it from a key and a seed stream, and the assembler builds it from the kit.
    /// </summary>
    public sealed class DwellingPlan
    {
        public const int Module = 4;

        public readonly List<DwellingRoom> Rooms = new List<DwellingRoom>();

        /// <summary>The segment of one module from node (X, Z) along +X, or along +Z.</summary>
        public readonly Dictionary<(int X, int Z, bool AlongZ), Opening> Openings = new Dictionary<(int, int, bool), Opening>();

        public readonly List<Placement> Furniture = new List<Placement>();

        /// <summary>Furniture the rooms call for that found no room, as "room: key"; for review, never shown in play.</summary>
        public readonly List<string> Unplaced = new List<string>();

        /// <summary>The street segment's start; it always runs along +X on the front, z = 0.</summary>
        public (int X, int Z) Street;

        public int Width { get; set; }

        public bool HasStash => Furniture.Exists(f => f.Stash);

        public DwellingPlan Room(string name, RoomKind kind, int x, int z, int width, int depth, bool stone = false)
        {
            Rooms.Add(new DwellingRoom(name, kind, x, z, width, depth, stone));
            Width = Mathf.Max(Width, x + width);
            return this;
        }

        public DwellingPlan Open(Opening kind, int x, int z, bool alongZ = false)
        {
            Openings[(x, z, alongZ)] = kind;
            if (kind == Opening.Street)
            {
                Street = (x, z);
            }

            return this;
        }

        public Opening OpeningAt(int x, int z, bool alongZ) =>
            Openings.TryGetValue((x, z, alongZ), out var kind) ? kind : Opening.Wall;
    }
}
