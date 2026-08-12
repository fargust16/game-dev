using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// One lake: a flood-filled region of already-generated terrain. <see cref="WaterLevelY"/> starts
    /// as the flood spill elevation from <see cref="LakeBasinFinder"/>, then becomes the
    /// authoritative post-carve rim level once <see cref="LakeTerrainCarver"/> finalises the basin
    /// (single source of truth for mesh Y, depth, and shoreline).
    /// </summary>
    public sealed class WaterZone
    {
        public string Id { get; }
        public float WaterLevelY { get; private set; }
        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Depth { get; }

        /// <summary>Number of cells the flood actually claimed - diagnostic mainly (e.g. spotting a
        /// flood that ran all the way out to <c>LakeBasinFinder.FindZones</c>'s
        /// <c>cellCountSafetyCap</c> instead of stopping at a real rim).</summary>
        public int FloodedCellCount { get; }

        /// <summary>Approx equivalent radius in metres from flooded area (π r² ≈ area).</summary>
        public float EquivalentRadiusMeters =>
            Mathf.Sqrt(FloodedCellCount * CellSize * CellSize / Mathf.PI);

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

            var count = 0;
            for (var x = 0; x < Width; x++)
            {
                for (var z = 0; z < Depth; z++)
                {
                    if (flooded[x, z])
                    {
                        count++;
                    }
                }
            }

            FloodedCellCount = count;
        }

        /// <summary>
        /// Replaces flood-spill Y with the post-carve authoritative water level (rim of the final
        /// TerrainData basin). Mesh / depth / vegetation all read this property afterward.
        /// </summary>
        public void SetWaterLevel(float waterLevelY)
        {
            WaterLevelY = waterLevelY;
        }

        public bool IsFlooded(int x, int z) =>
            x >= 0 && z >= 0 && x < Width && z < Depth && flooded[x, z];

        /// <summary>
        /// Pre-carve sampled height at a grid cell (flood-time DirectMatrices). Prefer a live
        /// terrain sampler for skirts after <see cref="LakeTerrainCarver"/> has run.
        /// </summary>
        public float HeightAt(int x, int z) =>
            x >= 0 && z >= 0 && x < Width && z < Depth ? heights[x, z] : WaterLevelY - 1f;

        public bool Contains(float worldX, float worldZ)
        {
            var x = Mathf.FloorToInt((worldX - OriginX) / CellSize);
            var z = Mathf.FloorToInt((worldZ - OriginZ) / CellSize);
            return IsFlooded(x, z);
        }

        /// <summary>XZ AABB of flooded cells (metres). Empty flood → zero rect at origin.</summary>
        public Rect GetFloodAabbXZ()
        {
            var minX = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;
            var any = false;
            for (var x = 0; x < Width; x++)
            {
                for (var z = 0; z < Depth; z++)
                {
                    if (!IsFlooded(x, z))
                    {
                        continue;
                    }

                    any = true;
                    var x0 = OriginX + x * CellSize;
                    var z0 = OriginZ + z * CellSize;
                    if (x0 < minX) minX = x0;
                    if (z0 < minZ) minZ = z0;
                    if (x0 + CellSize > maxX) maxX = x0 + CellSize;
                    if (z0 + CellSize > maxZ) maxZ = z0 + CellSize;
                }
            }

            return any
                ? Rect.MinMaxRect(minX, minZ, maxX, maxZ)
                : new Rect(OriginX, OriginZ, 0f, 0f);
        }

        /// <summary>Centroid of flooded cells in world XZ (Y unused / 0).</summary>
        public Vector3 GetFloodCentroidXZ()
        {
            double sx = 0;
            double sz = 0;
            var n = 0;
            for (var x = 0; x < Width; x++)
            {
                for (var z = 0; z < Depth; z++)
                {
                    if (!IsFlooded(x, z))
                    {
                        continue;
                    }

                    sx += OriginX + (x + 0.5f) * CellSize;
                    sz += OriginZ + (z + 0.5f) * CellSize;
                    n++;
                }
            }

            if (n == 0)
            {
                return new Vector3(OriginX, 0f, OriginZ);
            }

            return new Vector3((float)(sx / n), 0f, (float)(sz / n));
        }
    }
}
