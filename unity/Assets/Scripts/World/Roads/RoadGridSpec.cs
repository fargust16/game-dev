using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// A regular XZ sampling grid covering a world-space rectangle, used by
    /// <see cref="RoadPathfinder"/> to turn continuous terrain cost into a searchable graph.
    /// </summary>
    public readonly struct RoadGridSpec
    {
        public float MinX { get; }
        public float MinZ { get; }
        public int Width { get; }
        public int Height { get; }
        public float CellSize { get; }

        public RoadGridSpec(float minX, float minZ, int width, int height, float cellSize)
        {
            MinX = minX;
            MinZ = minZ;
            Width = width;
            Height = height;
            CellSize = cellSize;
        }

        public int CellCount => Width * Height;

        public Vector2 CellCenter(int cx, int cz) =>
            new Vector2(MinX + (cx + 0.5f) * CellSize, MinZ + (cz + 0.5f) * CellSize);

        public bool TryWorldToCell(float worldX, float worldZ, out int cx, out int cz)
        {
            cx = Mathf.FloorToInt((worldX - MinX) / CellSize);
            cz = Mathf.FloorToInt((worldZ - MinZ) / CellSize);
            return cx >= 0 && cx < Width && cz >= 0 && cz < Height;
        }

        public int Index(int cx, int cz) => cz * Width + cx;
    }
}
