using System;
using System.Linq;
using GameDev.Confront.Grid;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Readings of the Combat Grid against the kit's blocks, at 1 m cells, shared by S0's check
    /// (<c>VeilmouthGridCheck</c>) and the slice's grid tests (S3a.3): whether the grid steps across a block, and the
    /// block's solid body.
    /// </summary>
    public static class SliceGridReadings
    {
        /// <summary>A stoop of three 0.17 m steps still counts as the street's level (authoring default, 2026-11-06).</summary>
        public const float LevelTolerance = 0.6f;

        /// <summary>1 when the grid steps from the cell under <paramref name="a"/> to the one under <paramref name="b"/>
        /// at about height <paramref name="y"/>, 0 when it cannot, and −1 when either has no standable surface there.</summary>
        public static int Passes(CombatGridReader reader, Vector3 a, Vector3 b, float y)
        {
            int ax = Mathf.FloorToInt(a.x), az = Mathf.FloorToInt(a.z), bx = Mathf.FloorToInt(b.x), bz = Mathf.FloorToInt(b.z);
            if (ax == bx && az == bz)
            {
                return -1;
            }

            var la = LevelNear(reader.Column(ax, az), y);
            if (la < 0)
            {
                return -1;
            }

            var column = reader.Column(bx, bz);
            var any = false;
            for (var lb = 0; lb < column.Length; lb++)
            {
                if (!column[lb].Standable || Mathf.Abs(column[lb].Y - y) > 1.5f)
                {
                    continue;
                }

                any = true;
                if (reader.EdgeBetween(ax, az, la, bx, bz, lb))
                {
                    return 1;
                }
            }

            // A house cell with no standable floor near the street is closed: nothing in it can be stood on.
            return any || column.Length > 0 ? 0 : -1;
        }

        /// <summary>
        /// Whether the grid, built on the street 1.6 m before a house door, reaches a cell behind the house's front: 1
        /// when it does, 0 when the front closes it, −1 when no street cell stands before the door. The reading starts
        /// from the street's own surface, since a door's threshold stands 0.6–1.7 m above the street in the C3 city.
        /// </summary>
        public static int EntersHouse(CombatGridReader reader, Transform door)
        {
            var street = door.position + (door.forward * 1.6f);
            if (!reader.TryFindStart(street, out var ix, out var iz, out var level)
                || Mathf.Abs(ix - Mathf.FloorToInt(street.x)) > 1 || Mathf.Abs(iz - Mathf.FloorToInt(street.z)) > 1)
            {
                return -1;
            }

            reader.Build(ix, iz, level);
            foreach (var depth in new[] { 0.6f, 1.6f, 2.6f })
            {
                var inside = door.position - (door.forward * depth);
                int x = Mathf.FloorToInt(inside.x), z = Mathf.FloorToInt(inside.z);
                for (var l = 0; l < reader.Column(x, z).Length; l++)
                {
                    if (reader.IsReachable(x, z, l))
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        /// <summary>The standable surface nearest a height, within <see cref="LevelTolerance"/>, or −1.</summary>
        public static int LevelNear(ReadOnlySpan<CombatGridSurface> column, float y)
        {
            var best = -1;
            for (var l = 0; l < column.Length; l++)
            {
                if (column[l].Standable && Mathf.Abs(column[l].Y - y) <= LevelTolerance
                    && (best < 0 || Mathf.Abs(column[l].Y - y) < Mathf.Abs(column[best].Y - y)))
                {
                    best = l;
                }
            }

            return best;
        }

        /// <summary>The bounds of a block's enabled solid colliders.</summary>
        public static bool TryBody(Transform block, out Bounds body)
        {
            body = default;
            var colliders = block.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
            if (colliders.Length == 0)
            {
                return false;
            }

            body = colliders[0].bounds;
            foreach (var c in colliders.Skip(1))
            {
                body.Encapsulate(c.bounds);
            }

            return true;
        }

        /// <summary>A doorway wall: the Victorian Interiors' paper, dark and stone ones since VI (2026-11-07), and BK's
        /// before. A stone doorway's face (<c>Kit_ViStone_DoorwayFace_</c>) is its trim, not a wall of its own.</summary>
        public static bool IsDoorway(string name) =>
            name.StartsWith("Kit_ViRoom_PaperDoorway_") || name.StartsWith("Kit_ViRoom_DarkDoorway_")
            || name.StartsWith("Kit_ViStone_Doorway_")
            || name.StartsWith("Kit_BkRoom_Doorway_") || name.StartsWith("Kit_BkRoom_PlasterDoorway_");

        /// <summary>A shut door leaf seated in a doorway.</summary>
        public static bool IsDoorLeaf(string name) =>
            name.StartsWith("Kit_ViRoom_Door_") || name.StartsWith("Kit_ViRoom_DoorB_") || name.StartsWith("Kit_BkRoom_Door_");

        /// <summary>The side of a cell that faces along a grid direction.</summary>
        public static GridSide Side(Vector3 toward) =>
            Mathf.Abs(toward.x) > 0.5f ? (toward.x > 0 ? GridSide.PlusX : GridSide.MinusX) : (toward.z > 0 ? GridSide.PlusZ : GridSide.MinusZ);
    }
}
