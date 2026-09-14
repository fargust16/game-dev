using System;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// D8 flow directions over the filled surface, and the contributing area that follows from them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// D8 — every cell drains to exactly one of its eight neighbours — was chosen over D-infinity and
    /// the multiple-flow-direction family for one practical reason: it produces a <b>tree</b>. River
    /// polylines, tributary topology, Strahler order and the downstream walk that proves every river
    /// reaches the sea all fall straight out of a tree and all get materially harder without one. At
    /// 2800 m across a 2.7 m grid the extra realism of dispersed flow would not survive rasterisation
    /// into an 8 m channel anyway. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §6.
    /// </para>
    /// <para>
    /// Run this on the surface from <see cref="DepressionFill"/>, never on the raw heights: on the
    /// filled surface every non-ocean cell is guaranteed a strictly lower neighbour — the one the
    /// flood reached it from — so there are no pits to special-case and no cycles to detect.
    /// </para>
    /// </remarks>
    public static class FlowRouting
    {
        /// <summary>A cell with nowhere to drain: the sea, or a sink that should not exist.</summary>
        public const byte NoDirection = 255;

        /// <summary>
        /// Steepest-descent direction per cell, as an index into
        /// <see cref="HydrologyGrid.NeighborDx"/>.
        /// </summary>
        /// <remarks>
        /// Steepest by gradient, not by drop: the fall is divided by the step length, so a diagonal
        /// has to be about 1.41x deeper to beat a cardinal. Without that, flow on a uniform slope
        /// biases toward diagonals and the network acquires a visible 45-degree grain.
        /// </remarks>
        public static byte[] ComputeD8(HydrologyGrid grid, float[] filled, bool[] oceanMask)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (filled == null || filled.Length != grid.CellCount)
            {
                throw new ArgumentException("Filled surface size mismatch.", nameof(filled));
            }

            if (oceanMask == null || oceanMask.Length != grid.CellCount)
            {
                throw new ArgumentException("Ocean mask size mismatch.", nameof(oceanMask));
            }

            var directions = new byte[grid.CellCount];
            for (var z = 0; z < grid.Height; z++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var i = grid.Index(x, z);
                    if (oceanMask[i])
                    {
                        directions[i] = NoDirection;
                        continue;
                    }

                    var best = NoDirection;
                    var bestGradient = 0f;
                    var here = filled[i];

                    for (var n = 0; n < HydrologyGrid.NeighborCount; n++)
                    {
                        var nx = x + HydrologyGrid.NeighborDx[n];
                        var nz = z + HydrologyGrid.NeighborDz[n];
                        if (!grid.InBounds(nx, nz))
                        {
                            continue;
                        }

                        var drop = here - filled[grid.Index(nx, nz)];
                        if (drop <= 0f)
                        {
                            continue;
                        }

                        // Strictly greater, so a tie keeps the earlier neighbour. The neighbour
                        // order is fixed, which is what makes divides between equal slopes land in
                        // the same place on every run.
                        var gradient = drop / grid.NeighborDistance(n);
                        if (gradient > bestGradient)
                        {
                            bestGradient = gradient;
                            best = (byte)n;
                        }
                    }

                    directions[i] = best;
                }
            }

            return directions;
        }

        /// <summary>Index of the cell <paramref name="index"/> drains into, or -1.</summary>
        public static int Downstream(HydrologyGrid grid, int index, byte direction)
        {
            if (direction == NoDirection)
            {
                return -1;
            }

            var x = index % grid.Width + HydrologyGrid.NeighborDx[direction];
            var z = index / grid.Width + HydrologyGrid.NeighborDz[direction];
            return grid.InBounds(x, z) ? grid.Index(x, z) : -1;
        }

        /// <summary>
        /// Upstream contributing area per cell, in square metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Square metres, not cell counts, on purpose: every threshold downstream of this is a
        /// physical quantity, so changing the analysis resolution changes the cost of the run and
        /// not the shape of the river network.
        /// </para>
        /// <para>
        /// Accumulated by Kahn's algorithm over the flow tree rather than by sorting a million cells
        /// by elevation — linear instead of n log n, and the sum is order-independent, so no
        /// tie-break rule is needed here to stay deterministic.
        /// </para>
        /// </remarks>
        /// <param name="processedCount">
        /// How many cells the traversal actually visited. Anything less than the land cell count
        /// means a cycle survived, which the filled surface makes impossible — the caller reports it
        /// rather than silently producing a partial network.
        /// </param>
        public static float[] Accumulate(
            HydrologyGrid grid, byte[] directions, bool[] oceanMask, out int processedCount)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var count = grid.CellCount;
            var area = grid.CellAreaSqm;
            var accumulation = new float[count];
            var inDegree = new int[count];

            for (var i = 0; i < count; i++)
            {
                accumulation[i] = oceanMask[i] ? 0f : area;
                var target = Downstream(grid, i, directions[i]);
                if (target >= 0)
                {
                    inDegree[target]++;
                }
            }

            var stack = new int[count];
            var top = 0;
            for (var i = 0; i < count; i++)
            {
                if (inDegree[i] == 0)
                {
                    stack[top++] = i;
                }
            }

            processedCount = 0;
            while (top > 0)
            {
                var index = stack[--top];
                processedCount++;

                var target = Downstream(grid, index, directions[index]);
                if (target < 0)
                {
                    continue;
                }

                accumulation[target] += accumulation[index];
                if (--inDegree[target] == 0)
                {
                    stack[top++] = target;
                }
            }

            return accumulation;
        }
    }
}
