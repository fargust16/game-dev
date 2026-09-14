using System;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// One world height field in world coordinates: the generator's only intermediate
    /// representation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A header and a <c>float[]</c>. It exists for three reasons and no others: the drainage
    /// analysis takes exactly this shape (<c>HydrologyGrid</c> is constructed from origin, size,
    /// cell size and a <c>float[]</c>), the carve has to modify the field between analysis and
    /// write, and evaluating one world lattice instead of four tile fields is what makes tile seams
    /// identical rather than matched.
    /// </para>
    /// <para>
    /// <b>Row-major, indexed [z * Width + x]</b>, matching <c>HydrologyGrid.Index</c>. Note that
    /// Unity's <c>TerrainData.GetHeights</c> / <c>SetHeights</c> are indexed <c>[z, x]</c> as well,
    /// so the two agree; nothing here transposes.
    /// </para>
    /// <para>
    /// Heights are world metres, not normalised. Normalisation against the tile height budget
    /// belongs to whoever writes the terrain, which is not this type.
    /// </para>
    /// </remarks>
    public sealed class WorldHeightField
    {
        public float OriginX { get; }
        public float OriginZ { get; }

        /// <summary>Metres between adjacent samples on both axes.</summary>
        public float CellSize { get; }

        public int Width { get; }
        public int Height { get; }

        /// <summary>World metres, row-major.</summary>
        public float[] Heights { get; }

        public WorldHeightField(
            float originX, float originZ, int width, int height, float cellSize, float[] heights)
        {
            if (width < 2 || height < 2)
            {
                throw new ArgumentException($"Field must be at least 2x2, got {width}x{height}.");
            }

            if (cellSize <= 0f)
            {
                throw new ArgumentException($"Cell size must be positive, got {cellSize}.");
            }

            if (heights == null || heights.Length != width * height)
            {
                throw new ArgumentException(
                    $"Height array must be {width * height} long, got {heights?.Length ?? -1}.");
            }

            OriginX = originX;
            OriginZ = originZ;
            Width = width;
            Height = height;
            CellSize = cellSize;
            Heights = heights;
        }

        /// <summary>Allocates an all-zero field covering the whole overworld at the given resolution.</summary>
        public static WorldHeightField ForWorld(int resolution)
        {
            var cell = WorldScale.WorldExtentXMeters / (resolution - 1);
            return new WorldHeightField(0f, 0f, resolution, resolution, cell, new float[resolution * resolution]);
        }

        public int SampleCount => Width * Height;

        public int Index(int x, int z) => z * Width + x;

        public float WorldX(int x) => OriginX + x * CellSize;

        public float WorldZ(int z) => OriginZ + z * CellSize;

        public float this[int x, int z]
        {
            get => Heights[z * Width + x];
            set => Heights[z * Width + x] = value;
        }

        /// <summary>Slope as <c>1 - normal.y</c>, matching <c>OverworldTerrainSampler.SampleSlope01</c>.</summary>
        /// <remarks>
        /// Central differences, one-sided at the border. Reported for diagnostics only - the
        /// canonical slope for downstream systems still comes from Unity Terrain once the field is
        /// written, and the two agree to the resampling error.
        /// </remarks>
        public float Slope01(int x, int z)
        {
            var xm = Mathf.Max(0, x - 1);
            var xp = Mathf.Min(Width - 1, x + 1);
            var zm = Mathf.Max(0, z - 1);
            var zp = Mathf.Min(Height - 1, z + 1);

            var dx = (Heights[z * Width + xp] - Heights[z * Width + xm]) / ((xp - xm) * CellSize);
            var dz = (Heights[zp * Width + x] - Heights[zm * Width + x]) / ((zp - zm) * CellSize);

            // normal = normalize(-dx, 1, -dz); normal.y = 1 / sqrt(dx^2 + dz^2 + 1)
            var normalY = 1f / Mathf.Sqrt(dx * dx + dz * dz + 1f);
            return Mathf.Clamp01(1f - normalY);
        }

        /// <summary>
        /// FNV-1a over the header and the raw float bits. The determinism test's whole content.
        /// </summary>
        /// <remarks>
        /// Hashing the bits rather than a rounded value is deliberate: a change of one ULP is a
        /// change of geography that a tolerance-based comparison would hide, and the point of D1 is
        /// to notice exactly that. Negative zero is folded to positive zero first, because the two
        /// are numerically equal and differ only in their bit pattern.
        /// </remarks>
        public string Hash()
        {
            unchecked
            {
                const ulong offset = 14695981039346656037UL;
                const ulong prime = 1099511628211UL;

                var h = offset;
                h = MixInt(h, Width, prime);
                h = MixInt(h, Height, prime);
                h = MixInt(h, BitConverter.SingleToInt32Bits(CellSize), prime);
                h = MixInt(h, BitConverter.SingleToInt32Bits(OriginX), prime);
                h = MixInt(h, BitConverter.SingleToInt32Bits(OriginZ), prime);

                for (var i = 0; i < Heights.Length; i++)
                {
                    var v = Heights[i];
                    if (v == 0f)
                    {
                        v = 0f;
                    }

                    h = MixInt(h, BitConverter.SingleToInt32Bits(v), prime);
                }

                return h.ToString("x16");
            }
        }

        private static ulong MixInt(ulong h, int value, ulong prime)
        {
            unchecked
            {
                for (var b = 0; b < 4; b++)
                {
                    h ^= (byte)(value >> (b * 8));
                    h *= prime;
                }

                return h;
            }
        }
    }
}
