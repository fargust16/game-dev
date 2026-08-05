using System;

namespace GameDev.Grid
{
    [Flags]
    public enum GridCellFlags
    {
        None = 0,
        Walkable = 1 << 0,
        BlocksLoS = 1 << 1,
        Cover = 1 << 2,
        Climbable = 1 << 3,
        Vaultable = 1 << 4,
        Window = 1 << 5,
        Door = 1 << 6,
        Stairs = 1 << 7
    }

    [Serializable]
    public struct GridCoord : IEquatable<GridCoord>
    {
        public int x;
        public int y;

        public GridCoord(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(GridCoord other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"({x},{y})";
    }

    [Serializable]
    public struct GridCell
    {
        public GridCoord coord;
        public int height;
        public GridCellFlags flags;
        public bool occupied;
    }
}
