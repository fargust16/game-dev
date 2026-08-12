using System;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// D8 flow direction + flow accumulation over a sampled heightmap grid - the input
    /// <see cref="RiverTracer"/> needs to tell "this is just a damp hollow" from "this is where
    /// half the mountain's runoff funnels through" before it decides where a river channel starts.
    /// Same sampling convention as <see cref="LakeBasinFinder"/> (a <c>Func&lt;float,float,float&gt;</c>
    /// world-space height sampler over a rectangular region), so callers can feed both from the
    /// same tile/margin bounds and get grids that line up cell-for-cell.
    /// <para>
    /// Deliberately 8-neighbour (not <see cref="LakeBasinFinder"/>'s 4) - flow direction is a
    /// direction a river actually travels in, and D4 flow forces every non-axis-aligned slope into
    /// a blocky staircase; D8 is the standard choice for DEM flow routing (O'Callaghan &amp; Mark
    /// 1984) and costs nothing extra here (still one pass over a fixed neighbour list).
    /// </para>
    /// <para>
    /// Two-pass, O(n log n), pure function of the sampled heights (no RNG): pass one picks each
    /// cell's steepest-descent neighbour (ties broken by a fixed neighbour iteration order, so the
    /// same heights always pick the same direction); pass two visits cells from highest to lowest
    /// and pushes each cell's running accumulation onto its downstream neighbour - by the time a
    /// cell is visited, every cell that flows into it (all strictly higher, so already visited) has
    /// already added its own contribution, so one descending sweep is enough to total the whole
    /// upstream catchment at every cell.
    /// </para>
    /// </summary>
    public static class FlowNetwork
    {
        private static readonly (int Dx, int Dz)[] Neighbors8 =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1),
        };

        public static FlowGrid Compute(
            Func<float, float, float> worldHeightSampler,
            float minX, float minZ, float sizeX, float sizeZ,
            float cellSize)
        {
            var width = Mathf.Max(2, Mathf.RoundToInt(sizeX / cellSize));
            var depth = Mathf.Max(2, Mathf.RoundToInt(sizeZ / cellSize));

            var heights = new float[width, depth];
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    heights[x, z] = worldHeightSampler(minX + (x + 0.5f) * cellSize, minZ + (z + 0.5f) * cellSize);
                }
            }

            var flowDx = new sbyte[width, depth];
            var flowDz = new sbyte[width, depth];
            var order = new (float H, int X, int Z)[width * depth];

            var i = 0;
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    order[i++] = (heights[x, z], x, z);

                    var h = heights[x, z];
                    var bestDx = (sbyte)0;
                    var bestDz = (sbyte)0;
                    var bestH = h;

                    foreach (var (dx, dz) in Neighbors8)
                    {
                        var nx = x + dx;
                        var nz = z + dz;
                        if (nx < 0 || nz < 0 || nx >= width || nz >= depth)
                        {
                            continue;
                        }

                        var nh = heights[nx, nz];
                        if (nh < bestH)
                        {
                            bestH = nh;
                            bestDx = (sbyte)dx;
                            bestDz = (sbyte)dz;
                        }
                    }

                    flowDx[x, z] = bestDx;
                    flowDz[x, z] = bestDz;
                }
            }

            // Descending height order: every upstream contributor of a cell is strictly higher, so
            // by the time we reach that cell here every contribution has already landed in its
            // accumulation slot - one sweep totals the whole catchment at every cell.
            Array.Sort(order, (a, b) => b.H.CompareTo(a.H));

            var accumulation = new int[width, depth];
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    accumulation[x, z] = 1;
                }
            }

            foreach (var cell in order)
            {
                var dx = flowDx[cell.X, cell.Z];
                var dz = flowDz[cell.X, cell.Z];
                if (dx == 0 && dz == 0)
                {
                    continue; // sink - no lower neighbour to push into
                }

                accumulation[cell.X + dx, cell.Z + dz] += accumulation[cell.X, cell.Z];
            }

            return new FlowGrid(heights, flowDx, flowDz, accumulation, minX, minZ, cellSize);
        }
    }

    /// <summary>Result of <see cref="FlowNetwork.Compute"/> - sampled heights plus, per cell, its
    /// steepest-descent neighbour offset and total upstream cell count (including itself).</summary>
    public sealed class FlowGrid
    {
        public int Width { get; }
        public int Depth { get; }
        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }

        private readonly float[,] heights;
        private readonly sbyte[,] flowDx;
        private readonly sbyte[,] flowDz;
        private readonly int[,] accumulation;

        internal FlowGrid(
            float[,] heights, sbyte[,] flowDx, sbyte[,] flowDz, int[,] accumulation,
            float originX, float originZ, float cellSize)
        {
            this.heights = heights;
            this.flowDx = flowDx;
            this.flowDz = flowDz;
            this.accumulation = accumulation;
            OriginX = originX;
            OriginZ = originZ;
            CellSize = cellSize;
            Width = heights.GetLength(0);
            Depth = heights.GetLength(1);
        }

        public float HeightAt(int x, int z) => heights[x, z];

        /// <summary>Total cell count draining through (x,z), including the cell itself - a cheap
        /// stand-in for discharge/catchment area, used to decide "is this a real channel" and to
        /// scale river width/depth.</summary>
        public int AccumulationAt(int x, int z) => accumulation[x, z];

        public bool IsBorderCell(int x, int z) => x == 0 || z == 0 || x == Width - 1 || z == Depth - 1;

        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;

        /// <summary>Steepest-descent neighbour of (x,z), if any. Returns false at a sink (local
        /// minimum within the sampled grid - a real basin, a dip too shallow to register, or a cell
        /// whose true downhill neighbour lies outside the sampled bounds).</summary>
        public bool TryGetDownstream(int x, int z, out int nx, out int nz)
        {
            var dx = flowDx[x, z];
            var dz = flowDz[x, z];
            if (dx == 0 && dz == 0)
            {
                nx = 0;
                nz = 0;
                return false;
            }

            nx = x + dx;
            nz = z + dz;
            return true;
        }

        public Vector2 CellToWorld(int x, int z) =>
            new Vector2(OriginX + (x + 0.5f) * CellSize, OriginZ + (z + 0.5f) * CellSize);
    }
}
