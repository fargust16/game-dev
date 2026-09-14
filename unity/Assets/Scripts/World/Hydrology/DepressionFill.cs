using System;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// Priority-flood depression filling with an epsilon gradient (Barnes, Lehman &amp; Mulla 2014).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the load-bearing step of the whole hydrology system, and it is here for one reason:
    /// it makes the working surface <b>monotonically descending to the ocean by construction</b>.
    /// The flood starts at the coast and works inward, and every cell it reaches is raised to just
    /// above the cell it was reached from. Four of the required guarantees then stop being things
    /// the code has to check for and start being properties it cannot violate:
    /// </para>
    /// <list type="bullet">
    /// <item>every river reaches the ocean — a descending path has nowhere else to end;</item>
    /// <item>no cycles — D8 on a strictly descending surface cannot revisit a cell;</item>
    /// <item>no river terminates inland;</item>
    /// <item>no river crosses a ridgeline — the epsilon is microscopic against real relief, so flow
    /// still only ever runs downhill on the terrain the player sees.</item>
    /// </list>
    /// <para>
    /// It also hands the lakes over for free: wherever the filled surface ended up above the
    /// original one, the terrain holds water. See <see cref="BasinExtraction"/>.
    /// </para>
    /// <para>
    /// Depression <i>breaching</i> was rejected for the opposite reason: it cuts an outlet through
    /// the basin rim, which is a watershed, and «rivers must not cross ridgelines» is a hard
    /// requirement. See <c>HYDROLOGY_GENERATION_PLAN.md</c> §6.
    /// </para>
    /// </remarks>
    public static class DepressionFill
    {
        /// <summary>
        /// The uphill step applied to each newly flooded cell, in metres.
        /// </summary>
        /// <remarks>
        /// One millimetre. It has to clear single-precision resolution at the top of the height
        /// budget — a float near 700 m resolves to about 0.06 mm — or two neighbours could compare
        /// equal and D8 would find no downhill step. It also has to stay far below
        /// <see cref="HydrologySettings.LakeFillToleranceMeters"/>, because the epsilon accumulates
        /// across a flat basin and must never inflate a dimple into a lake: 250 cells of it comes to
        /// 0.25 m, and a basin that wide is a real lake anyway.
        /// </remarks>
        public const float EpsilonMeters = 0.001f;

        /// <summary>
        /// Returns the filled surface. The input grid is never modified.
        /// </summary>
        /// <param name="grid">Raw terrain heights.</param>
        /// <param name="oceanMask">
        /// From <see cref="HydrologyGrid.BuildOceanMask"/>. Must be the connectivity-checked mask,
        /// not a bare "below sea level" test, or an inland pit below the waterline becomes a false
        /// outlet.
        /// </param>
        /// <returns>
        /// One value per sample, always at or above the original height.
        /// </returns>
        public static float[] Fill(HydrologyGrid grid, bool[] oceanMask)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (oceanMask == null || oceanMask.Length != grid.CellCount)
            {
                throw new ArgumentException(
                    $"Ocean mask must be {grid?.CellCount ?? -1} long.", nameof(oceanMask));
            }

            var count = grid.CellCount;
            var filled = new float[count];
            var closed = new bool[count];
            var heap = new MinHeap(Math.Max(64, count / 4));

            // Seed: the coast, plus the world border. The border matters only for a world whose edge
            // is above sea level — it cannot happen on the island, but without it such a grid would
            // have no outlet at all and the flood would fill the whole map into one lake.
            for (var z = 0; z < grid.Height; z++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var i = grid.Index(x, z);
                    if (!oceanMask[i] && !grid.IsBorder(x, z))
                    {
                        continue;
                    }

                    filled[i] = grid.Heights[i];
                    closed[i] = true;
                    heap.Push(filled[i], i);
                }
            }

            while (heap.TryPop(out var index))
            {
                var cx = index % grid.Width;
                var cz = index / grid.Width;
                var cv = filled[index];

                for (var n = 0; n < HydrologyGrid.NeighborCount; n++)
                {
                    var nx = cx + HydrologyGrid.NeighborDx[n];
                    var nz = cz + HydrologyGrid.NeighborDz[n];
                    if (!grid.InBounds(nx, nz))
                    {
                        continue;
                    }

                    var ni = grid.Index(nx, nz);
                    if (closed[ni])
                    {
                        continue;
                    }

                    var raised = cv + EpsilonMeters;
                    filled[ni] = grid.Heights[ni] > raised ? grid.Heights[ni] : raised;
                    closed[ni] = true;
                    heap.Push(filled[ni], ni);
                }
            }

            return filled;
        }

        /// <summary>
        /// Binary min-heap keyed on elevation, with insertion order as the tie-break.
        /// </summary>
        /// <remarks>
        /// The sequence tie-break is not a detail — a heap that breaks ties arbitrarily makes the
        /// flood order depend on array layout, and the drainage divides between equal-height cells
        /// would then move between runs. Determinism is a requirement, so the comparison is total.
        /// </remarks>
        private sealed class MinHeap
        {
            private float[] _keys;
            private long[] _sequence;
            private int[] _values;
            private int _count;
            private long _nextSequence;

            public MinHeap(int capacity)
            {
                _keys = new float[capacity];
                _sequence = new long[capacity];
                _values = new int[capacity];
            }

            public void Push(float key, int value)
            {
                if (_count == _keys.Length)
                {
                    Grow();
                }

                var i = _count++;
                _keys[i] = key;
                _sequence[i] = _nextSequence++;
                _values[i] = value;

                while (i > 0)
                {
                    var parent = (i - 1) >> 1;
                    if (!IsLess(i, parent))
                    {
                        break;
                    }

                    Swap(i, parent);
                    i = parent;
                }
            }

            public bool TryPop(out int value)
            {
                if (_count == 0)
                {
                    value = -1;
                    return false;
                }

                value = _values[0];
                _count--;
                if (_count > 0)
                {
                    _keys[0] = _keys[_count];
                    _sequence[0] = _sequence[_count];
                    _values[0] = _values[_count];
                    SiftDown(0);
                }

                return true;
            }

            private void SiftDown(int i)
            {
                while (true)
                {
                    var left = (i << 1) + 1;
                    if (left >= _count)
                    {
                        return;
                    }

                    var smallest = left;
                    var right = left + 1;
                    if (right < _count && IsLess(right, left))
                    {
                        smallest = right;
                    }

                    if (!IsLess(smallest, i))
                    {
                        return;
                    }

                    Swap(i, smallest);
                    i = smallest;
                }
            }

            private bool IsLess(int a, int b)
            {
                if (_keys[a] < _keys[b])
                {
                    return true;
                }

                if (_keys[a] > _keys[b])
                {
                    return false;
                }

                return _sequence[a] < _sequence[b];
            }

            private void Swap(int a, int b)
            {
                (_keys[a], _keys[b]) = (_keys[b], _keys[a]);
                (_sequence[a], _sequence[b]) = (_sequence[b], _sequence[a]);
                (_values[a], _values[b]) = (_values[b], _values[a]);
            }

            private void Grow()
            {
                var capacity = _keys.Length * 2;
                Array.Resize(ref _keys, capacity);
                Array.Resize(ref _sequence, capacity);
                Array.Resize(ref _values, capacity);
            }
        }
    }
}
