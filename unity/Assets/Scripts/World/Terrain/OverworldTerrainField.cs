using System;
using GameDev.World.Hydrology;
using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// The final overworld relief on one world-spanning vertex lattice, plus the world-space quantities
    /// the surface and vegetation rules read from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One lattice for the whole world rather than one array per tile. A tile edge vertex is the same
    /// lattice point for both tiles that share it, and every derived quantity — slope, convexity,
    /// drainage — is computed across the edge instead of clamped at it. Anything evaluated from this
    /// field is therefore a function of world position alone, which is what makes heights, alphamaps,
    /// trees and details agree at every tile seam without a stitching pass.
    /// </para>
    /// <para>
    /// Every quantity is in world units: metres, degrees, square metres. There is no normalised height
    /// and no per-biome vertical scale anywhere in this type.
    /// </para>
    /// </remarks>
    public sealed class OverworldTerrainField
    {
        private readonly float[] m_height;
        private readonly float[] m_uplift;
        private readonly float[] m_coastDistance;
        private float[] m_slope;
        private float[] m_convexity;
        private float[] m_drainage;

        private OverworldTerrainField(
            OverworldTerrainGrid.Spec spec, int width, int depth, float step,
            float[] height, float[] uplift, float[] coastDistance)
        {
            Spec = spec;
            Width = width;
            Depth = depth;
            StepMeters = step;
            m_height = height;
            m_uplift = uplift;
            m_coastDistance = coastDistance;
        }

        public OverworldTerrainGrid.Spec Spec { get; }

        /// <summary>Lattice vertices along X: <c>tilesX · (heightmapResolution − 1) + 1</c>.</summary>
        public int Width { get; }

        /// <summary>Lattice vertices along Z.</summary>
        public int Depth { get; }

        /// <summary>World distance between neighbouring vertices, metres.</summary>
        public float StepMeters { get; }

        /// <summary>Samples whose authoritative height fell outside the tile height budget.</summary>
        public int ClampedSamples { get; private set; }

        /// <summary>Row-major final heights in metres, <c>[z · Width + x]</c>.</summary>
        public float[] Heights => m_height;

        /// <summary>
        /// Evaluates the authoritative field at every lattice vertex.
        /// </summary>
        public static OverworldTerrainField FromAuthority(TerrainAuthorityField authority, OverworldTerrainGrid.Spec spec)
        {
            if (authority == null)
            {
                throw new ArgumentNullException(nameof(authority));
            }

            return FromSamples(spec, (float x, float z, out float up, out float sd) => authority.Sample(x, z, out up, out sd));
        }

        /// <summary>One lattice vertex: height, massif uplift and signed coast distance at a world point.</summary>
        public delegate float VertexSampler(float worldX, float worldZ, out float upliftMeters, out float coastSignedDistanceMeters);

        /// <summary>
        /// Evaluates <paramref name="sample"/> at every lattice vertex. <see cref="FromAuthority"/> is this
        /// with TerrainGen as the sampler; tests use it with synthetic relief.
        /// </summary>
        public static OverworldTerrainField FromSamples(OverworldTerrainGrid.Spec spec, VertexSampler sample)
        {
            if (sample == null)
            {
                throw new ArgumentNullException(nameof(sample));
            }

            var cells = spec.HeightmapResolution - 1;
            var width = spec.TileCountX * cells + 1;
            var depth = spec.TileCountZ * cells + 1;
            var step = spec.TileMeters / cells;
            var height = new float[width * depth];
            var uplift = new float[width * depth];
            var coast = new float[width * depth];

            for (var z = 0; z < depth; z++)
            {
                var worldZ = z * step;
                var row = z * width;
                for (var x = 0; x < width; x++)
                {
                    height[row + x] = sample(x * step, worldZ, out var up, out var sd);
                    uplift[row + x] = up;
                    coast[row + x] = sd;
                }
            }

            var field = new OverworldTerrainField(spec, width, depth, step, height, uplift, coast);
            field.CountClamped();
            return field;
        }

        /// <summary>
        /// Lowers the lattice by a carve depth, then invalidates every derived quantity.
        /// </summary>
        /// <param name="carveMetersAt">
        /// Carve depth in metres at a lattice vertex (x, z). Must be non-negative; it only ever lowers.
        /// </param>
        /// <returns>Number of vertices the carve moved.</returns>
        public int ApplyCarve(Func<int, int, float> carveMetersAt)
        {
            if (carveMetersAt == null)
            {
                return 0;
            }

            var moved = 0;
            for (var z = 0; z < Depth; z++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var carve = carveMetersAt(x, z);
                    if (carve > 0f)
                    {
                        m_height[z * Width + x] -= carve;
                        moved++;
                    }
                }
            }

            m_slope = null;
            m_convexity = null;
            m_drainage = null;
            CountClamped();
            return moved;
        }

        /// <summary>
        /// The tile's heightmap as Unity stores it: <c>[row, column]</c>, normalised by the tile height.
        /// </summary>
        public float[,] TileHeights01(int tileX, int tileZ)
        {
            var res = Spec.HeightmapResolution;
            var ox = tileX * (res - 1);
            var oz = tileZ * (res - 1);
            var budget = Spec.HeightBudgetMeters;
            var result = new float[res, res];
            for (var j = 0; j < res; j++)
            {
                var row = (oz + j) * Width;
                for (var i = 0; i < res; i++)
                {
                    result[j, i] = Mathf.Clamp01(m_height[row + ox + i] / budget);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------------------------------
        // World-space point queries (bilinear over the lattice)
        // ------------------------------------------------------------------------------------------

        public float HeightAt(float worldX, float worldZ) => Bilinear(m_height, worldX, worldZ);

        public float UpliftAt(float worldX, float worldZ) => Bilinear(m_uplift, worldX, worldZ);

        public float CoastDistanceAt(float worldX, float worldZ) => Bilinear(m_coastDistance, worldX, worldZ);

        /// <summary>True ground slope in degrees.</summary>
        public float SlopeDegreesAt(float worldX, float worldZ)
        {
            EnsureDerived();
            return Bilinear(m_slope, worldX, worldZ);
        }

        /// <summary>
        /// Height above the local mean in metres: positive on ridges and crests, negative in hollows.
        /// </summary>
        public float ConvexityAt(float worldX, float worldZ)
        {
            EnsureDerived();
            return Bilinear(m_convexity, worldX, worldZ);
        }

        /// <summary>Upstream drainage area in square metres; zero at sea.</summary>
        public float DrainageAreaAt(float worldX, float worldZ)
        {
            EnsureDerived();
            return Bilinear(m_drainage, worldX, worldZ);
        }

        // ------------------------------------------------------------------------------------------

        /// <summary>Radius of the convexity neighbourhood, metres.</summary>
        public const float ConvexityRadiusMeters = 20f;

        /// <summary>Lattice stride of the drainage analysis. 2 keeps a million-vertex world under a second.</summary>
        public const int DrainageStride = 2;

        private void EnsureDerived()
        {
            if (m_slope != null)
            {
                return;
            }

            m_slope = ComputeSlope();
            m_convexity = ComputeConvexity();
            m_drainage = ComputeDrainage();
        }

        private float[] ComputeSlope()
        {
            var slope = new float[m_height.Length];
            for (var z = 0; z < Depth; z++)
            {
                var z0 = Mathf.Max(0, z - 1);
                var z1 = Mathf.Min(Depth - 1, z + 1);
                for (var x = 0; x < Width; x++)
                {
                    var x0 = Mathf.Max(0, x - 1);
                    var x1 = Mathf.Min(Width - 1, x + 1);
                    var gx = (m_height[z * Width + x1] - m_height[z * Width + x0]) / ((x1 - x0) * StepMeters);
                    var gz = (m_height[z1 * Width + x] - m_height[z0 * Width + x]) / ((z1 - z0) * StepMeters);
                    slope[z * Width + x] = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
                }
            }

            return slope;
        }

        /// <summary>Height minus the box mean over <see cref="ConvexityRadiusMeters"/>, via a summed-area table.</summary>
        private float[] ComputeConvexity()
        {
            var r = Mathf.Max(1, Mathf.RoundToInt(ConvexityRadiusMeters / StepMeters));
            var w = Width + 1;
            var sat = new double[w * (Depth + 1)];
            for (var z = 0; z < Depth; z++)
            {
                double rowSum = 0;
                for (var x = 0; x < Width; x++)
                {
                    rowSum += m_height[z * Width + x];
                    sat[(z + 1) * w + x + 1] = sat[z * w + x + 1] + rowSum;
                }
            }

            var convexity = new float[m_height.Length];
            for (var z = 0; z < Depth; z++)
            {
                var za = Mathf.Max(0, z - r);
                var zb = Mathf.Min(Depth - 1, z + r) + 1;
                for (var x = 0; x < Width; x++)
                {
                    var xa = Mathf.Max(0, x - r);
                    var xb = Mathf.Min(Width - 1, x + r) + 1;
                    var sum = sat[zb * w + xb] - sat[za * w + xb] - sat[zb * w + xa] + sat[za * w + xa];
                    var mean = sum / ((xb - xa) * (zb - za));
                    convexity[z * Width + x] = (float)(m_height[z * Width + x] - mean);
                }
            }

            return convexity;
        }

        /// <summary>
        /// D8 upstream area on the depression-filled surface, using the project's hydrology primitives.
        /// </summary>
        private float[] ComputeDrainage()
        {
            var s = DrainageStride;
            var cw = (Width - 1) / s + 1;
            var cd = (Depth - 1) / s + 1;
            var coarse = new float[cw * cd];
            for (var z = 0; z < cd; z++)
            {
                for (var x = 0; x < cw; x++)
                {
                    coarse[z * cw + x] = m_height[Mathf.Min(Depth - 1, z * s) * Width + Mathf.Min(Width - 1, x * s)];
                }
            }

            var grid = new HydrologyGrid(0f, 0f, cw, cd, StepMeters * s, coarse, WorldScale.SeaLevelMeters);
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var directions = FlowRouting.ComputeD8(grid, filled, ocean);
            var area = FlowRouting.Accumulate(grid, directions, ocean, out _);

            var drainage = new float[m_height.Length];
            for (var z = 0; z < Depth; z++)
            {
                var fz = (float)z / s;
                var cz0 = Mathf.Min(cd - 1, (int)fz);
                var cz1 = Mathf.Min(cd - 1, cz0 + 1);
                var tz = fz - cz0;
                for (var x = 0; x < Width; x++)
                {
                    var fx = (float)x / s;
                    var cx0 = Mathf.Min(cw - 1, (int)fx);
                    var cx1 = Mathf.Min(cw - 1, cx0 + 1);
                    var tx = fx - cx0;
                    var a = Mathf.Lerp(area[cz0 * cw + cx0], area[cz0 * cw + cx1], tx);
                    var b = Mathf.Lerp(area[cz1 * cw + cx0], area[cz1 * cw + cx1], tx);
                    drainage[z * Width + x] = Mathf.Lerp(a, b, tz);
                }
            }

            return drainage;
        }

        private float Bilinear(float[] values, float worldX, float worldZ)
        {
            var fx = Mathf.Clamp(worldX / StepMeters, 0f, Width - 1);
            var fz = Mathf.Clamp(worldZ / StepMeters, 0f, Depth - 1);
            var x0 = Mathf.Min(Width - 2, (int)fx);
            var z0 = Mathf.Min(Depth - 2, (int)fz);
            var tx = fx - x0;
            var tz = fz - z0;
            var a = values[z0 * Width + x0] + (values[z0 * Width + x0 + 1] - values[z0 * Width + x0]) * tx;
            var b = values[(z0 + 1) * Width + x0] + (values[(z0 + 1) * Width + x0 + 1] - values[(z0 + 1) * Width + x0]) * tx;
            return a + (b - a) * tz;
        }

        private void CountClamped()
        {
            var budget = Spec.HeightBudgetMeters;
            var clamped = 0;
            for (var i = 0; i < m_height.Length; i++)
            {
                if (m_height[i] < 0f || m_height[i] > budget)
                {
                    clamped++;
                }
            }

            ClampedSamples = clamped;
        }
    }
}
