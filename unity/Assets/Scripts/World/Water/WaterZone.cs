using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// One lake: a flood-filled region of already-generated terrain up to its own spill
    /// elevation - never a plane placed independently of the heightmap. <see cref="WaterLevelY"/>
    /// is derived per zone by <see cref="LakeBasinFinder"/> from real sampled terrain, so two
    /// zones on the same map can sit at completely different world heights (a valley pond and a
    /// mountain tarn both come out correct without either one assuming the other's Y). All zones
    /// from one generation share the same sampling grid (<see cref="OriginX"/>/<see cref="OriginZ"/>/
    /// <see cref="CellSize"/>) so <see cref="WaterZoneSampler"/> can test a world position against
    /// each zone with the same coordinate math.
    /// </summary>
    public sealed class WaterZone
    {
        public string Id { get; }
        public float WaterLevelY { get; }
        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Depth { get; }

        private readonly bool[,] flooded;
        private readonly float[,] heights;

        public WaterZone(string id, float waterLevelY, float originX, float originZ, float cellSize,
            bool[,] flooded, float[,] heights)
        {
            Id = id;
            WaterLevelY = waterLevelY;
            OriginX = originX;
            OriginZ = originZ;
            CellSize = cellSize;
            this.flooded = flooded;
            this.heights = heights;
            Width = flooded.GetLength(0);
            Depth = flooded.GetLength(1);
        }

        public bool IsFlooded(int x, int z) =>
            x >= 0 && z >= 0 && x < Width && z < Depth && flooded[x, z];

        /// <summary>
        /// Sampled terrain height at a grid cell, used to anchor shoreline skirts to the real
        /// surface. Falls back to a metre below water just past the sampled grid's edge - that
        /// only clips an unrendered skirt face, never the flooded top surface itself.
        /// </summary>
        public float HeightAt(int x, int z) =>
            x >= 0 && z >= 0 && x < Width && z < Depth ? heights[x, z] : WaterLevelY - 1f;

        public bool Contains(float worldX, float worldZ)
        {
            var x = Mathf.FloorToInt((worldX - OriginX) / CellSize);
            var z = Mathf.FloorToInt((worldZ - OriginZ) / CellSize);
            return IsFlooded(x, z);
        }
    }
}
