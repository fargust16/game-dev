using System;
using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// A regular world-space height field, and the only thing the drainage analysis ever reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Samples, not cells: sample <c>(x, z)</c> sits exactly at
    /// <c>origin + (x, z) * CellSize</c>, which is how Unity's heightmap is laid out. A 2x2 grid of
    /// 513-resolution tiles therefore assembles into 1025 x 1025 samples spanning 2800 m at
    /// 2.734375 m spacing, with the tile seams sharing a column rather than duplicating one.
    /// </para>
    /// <para>
    /// Holding the whole world in one array is deliberate. Drainage is a global property — a river
    /// that starts on one tile and reaches the sea on another is the normal case — so any per-tile
    /// analysis would have to stitch basins back together afterwards, which is exactly the class of
    /// bug the single-tile assumption caused elsewhere in this project.
    /// </para>
    /// </remarks>
    public sealed class HydrologyGrid
    {
        /// <summary>Fixed 8-neighbour offsets. The order is part of the determinism contract.</summary>
        /// <remarks>
        /// D8 ties are broken by taking the first neighbour in this order, so reordering it changes
        /// generated geography. Cardinals first, then diagonals, so a tie prefers the shorter step.
        /// </remarks>
        public static readonly int[] NeighborDx = { 1, 0, -1, 0, 1, -1, -1, 1 };

        public static readonly int[] NeighborDz = { 0, 1, 0, -1, 1, 1, -1, -1 };

        public const int NeighborCount = 8;

        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Height { get; }

        /// <summary>World Y per sample, row-major: <c>index = z * Width + x</c>.</summary>
        public float[] Heights { get; }

        public float SeaLevel { get; }

        public HydrologyGrid(
            float originX,
            float originZ,
            int width,
            int height,
            float cellSize,
            float[] heights,
            float seaLevel)
        {
            if (width < 2 || height < 2)
            {
                throw new ArgumentException($"Grid must be at least 2x2, got {width}x{height}.");
            }

            if (heights == null || heights.Length != width * height)
            {
                throw new ArgumentException(
                    $"Height array must be {width * height} long, got {heights?.Length ?? -1}.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentException($"Cell size must be positive, got {cellSize}.");
            }

            OriginX = originX;
            OriginZ = originZ;
            Width = width;
            Height = height;
            CellSize = cellSize;
            Heights = heights;
            SeaLevel = seaLevel;
        }

        public int CellCount => Width * Height;

        /// <summary>Area one sample stands for, in square metres. The unit of flow accumulation.</summary>
        public float CellAreaSqm => CellSize * CellSize;

        public int Index(int x, int z) => z * Width + x;

        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Height;

        public float WorldX(int x) => OriginX + x * CellSize;

        public float WorldZ(int z) => OriginZ + z * CellSize;

        public Vector2 PositionXZ(int x, int z) => new Vector2(WorldX(x), WorldZ(z));

        public Vector2 PositionXZ(int index) => PositionXZ(index % Width, index / Width);

        /// <summary>True where the raw terrain is at or below sea level.</summary>
        /// <remarks>
        /// Submersion alone, which is not the same thing as being the sea — see
        /// <see cref="BuildOceanMask"/>. Reads the raw height, never the filled surface, because a
        /// lake sitting above sea level must not be mistaken for the coast.
        /// </remarks>
        public bool IsSubmerged(int index) => Heights[index] <= SeaLevel;

        /// <summary>
        /// Marks the cells that are actually the sea: submerged <b>and</b> reachable from the world
        /// border through other submerged cells.
        /// </summary>
        /// <remarks>
        /// Connectivity is the whole point. A closed inland pit whose floor happens to sit below
        /// 24 m is submerged but is not the ocean, and treating it as such would hand the network
        /// builder a false outlet — a river that "reaches the sea" in the middle of the island,
        /// silently satisfying the one invariant the system exists to guarantee. Such a pit is a
        /// depression like any other and becomes a lake.
        /// </remarks>
        public bool[] BuildOceanMask()
        {
            var ocean = new bool[CellCount];
            var stack = new System.Collections.Generic.Stack<int>();

            for (var z = 0; z < Height; z++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (!IsBorder(x, z))
                    {
                        continue;
                    }

                    var i = Index(x, z);
                    if (!IsSubmerged(i) || ocean[i])
                    {
                        continue;
                    }

                    ocean[i] = true;
                    stack.Push(i);
                }
            }

            while (stack.Count > 0)
            {
                var index = stack.Pop();
                var cx = index % Width;
                var cz = index / Width;
                for (var n = 0; n < NeighborCount; n++)
                {
                    var nx = cx + NeighborDx[n];
                    var nz = cz + NeighborDz[n];
                    if (!InBounds(nx, nz))
                    {
                        continue;
                    }

                    var ni = Index(nx, nz);
                    if (ocean[ni] || !IsSubmerged(ni))
                    {
                        continue;
                    }

                    ocean[ni] = true;
                    stack.Push(ni);
                }
            }

            return ocean;
        }

        public bool IsBorder(int x, int z) => x == 0 || z == 0 || x == Width - 1 || z == Height - 1;

        /// <summary>Distance between a sample and its neighbour <paramref name="n"/>, in metres.</summary>
        public float NeighborDistance(int n) =>
            n < 4 ? CellSize : CellSize * 1.41421356f;

        /// <summary>
        /// Builds a grid from a world-space height function. Used by tests and by any caller that
        /// has a sampler rather than a terrain.
        /// </summary>
        public static HydrologyGrid FromFunction(
            float originX,
            float originZ,
            int width,
            int height,
            float cellSize,
            float seaLevel,
            Func<float, float, float> heightAtWorld)
        {
            if (heightAtWorld == null)
            {
                throw new ArgumentNullException(nameof(heightAtWorld));
            }

            var heights = new float[width * height];
            for (var z = 0; z < height; z++)
            {
                var wz = originZ + z * cellSize;
                for (var x = 0; x < width; x++)
                {
                    heights[z * width + x] = heightAtWorld(originX + x * cellSize, wz);
                }
            }

            return new HydrologyGrid(originX, originZ, width, height, cellSize, heights, seaLevel);
        }

        /// <summary>
        /// Bilinear height at an arbitrary world position, clamped to the grid.
        /// </summary>
        /// <remarks>
        /// Used when a resampled river centreline no longer lands on sample positions. Clamping
        /// rather than failing keeps a centreline that grazes the world edge usable.
        /// </remarks>
        public float SampleHeightWorld(float worldX, float worldZ)
        {
            var fx = Mathf.Clamp((worldX - OriginX) / CellSize, 0f, Width - 1.0001f);
            var fz = Mathf.Clamp((worldZ - OriginZ) / CellSize, 0f, Height - 1.0001f);
            var x0 = (int)fx;
            var z0 = (int)fz;
            var x1 = Mathf.Min(x0 + 1, Width - 1);
            var z1 = Mathf.Min(z0 + 1, Height - 1);
            var tx = fx - x0;
            var tz = fz - z0;

            var h00 = Heights[z0 * Width + x0];
            var h10 = Heights[z0 * Width + x1];
            var h01 = Heights[z1 * Width + x0];
            var h11 = Heights[z1 * Width + x1];

            return Mathf.Lerp(Mathf.Lerp(h00, h10, tx), Mathf.Lerp(h01, h11, tx), tz);
        }
    }
}
