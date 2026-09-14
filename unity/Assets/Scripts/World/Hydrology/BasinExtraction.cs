using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Hydrology
{
    /// <summary>
    /// A lake found in the terrain, before the river network has told it which reaches it connects to.
    /// </summary>
    public sealed class LakeCandidate
    {
        public LakeId Id { get; }

        /// <summary>Cell indices this lake covers, ascending.</summary>
        public int[] Cells { get; }

        /// <summary>World Y of the flat water surface — the elevation at which the basin spills.</summary>
        public float SurfaceElevation { get; }

        public float MaxDepth { get; }
        public float AreaSqm { get; }

        /// <summary>The cell the outflow leaves through.</summary>
        public int SpillCell { get; }

        public Vector2 SpillPointXZ { get; }

        /// <summary>Shoreline polygon in world XZ, counter-clockwise, implicitly closed.</summary>
        public Vector2[] Boundary { get; }

        public LakeCandidate(
            LakeId id,
            int[] cells,
            float surfaceElevation,
            float maxDepth,
            float areaSqm,
            int spillCell,
            Vector2 spillPointXZ,
            Vector2[] boundary)
        {
            Id = id;
            Cells = cells;
            SurfaceElevation = surfaceElevation;
            MaxDepth = maxDepth;
            AreaSqm = areaSqm;
            SpillCell = spillCell;
            SpillPointXZ = spillPointXZ;
            Boundary = boundary;
        }
    }

    /// <summary>Lakes, plus the per-cell lookup that says which lake a cell belongs to.</summary>
    public sealed class LakeField
    {
        public static readonly LakeField Empty =
            new LakeField(Array.Empty<LakeCandidate>(), Array.Empty<int>());

        /// <summary>Lake index per cell, or -1. Index into <see cref="Lakes"/>, not a <see cref="LakeId"/>.</summary>
        public int[] Labels { get; }

        public IReadOnlyList<LakeCandidate> Lakes { get; }

        public LakeField(IReadOnlyList<LakeCandidate> lakes, int[] labels)
        {
            Lakes = lakes;
            Labels = labels;
        }

        public bool IsLake(int cellIndex) =>
            Labels.Length > cellIndex && cellIndex >= 0 && Labels[cellIndex] >= 0;
    }

    /// <summary>
    /// Turns the depressions that <see cref="DepressionFill"/> flooded into lakes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here places a lake. A lake is exactly the set of cells where the filled surface ended
    /// up above the original terrain: the terrain itself decided both the outline and the water
    /// level, and the level is the elevation at which the basin spills. There is no radius, no site
    /// selection and no random component — which is the specific failure of the removed
    /// <c>LakeDefinition</c> approach this system is written to avoid.
    /// </para>
    /// <para>
    /// Because every basin is filled until it spills, no lake is endorheic: each one has an outlet,
    /// and that is what lets the ocean-outlet invariant hold for the whole network. See
    /// <c>HYDROLOGY_GENERATION_PLAN.md</c> §9.
    /// </para>
    /// </remarks>
    public static class BasinExtraction
    {
        public static LakeField Extract(
            HydrologyGrid grid, float[] filled, bool[] oceanMask, HydrologySettings settings)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            var count = grid.CellCount;
            var labels = new int[count];
            for (var i = 0; i < count; i++)
            {
                labels[i] = -1;
            }

            var lakes = new List<LakeCandidate>();
            var component = new List<int>();
            var stack = new Stack<int>();
            var tolerance = settings.LakeFillToleranceMeters;

            for (var seed = 0; seed < count; seed++)
            {
                if (labels[seed] >= 0 || !IsSubmergedByFill(grid, filled, oceanMask, seed, tolerance))
                {
                    continue;
                }

                // 4-connected on purpose: a "lake" joined to another only through a diagonal pinch
                // is two lakes, and a 4-connected region is the only kind whose outline the crack
                // follower below can trace unambiguously.
                component.Clear();
                stack.Clear();
                stack.Push(seed);
                labels[seed] = lakes.Count;

                while (stack.Count > 0)
                {
                    var index = stack.Pop();
                    component.Add(index);

                    var cx = index % grid.Width;
                    var cz = index / grid.Width;
                    for (var n = 0; n < 4; n++)
                    {
                        var nx = cx + HydrologyGrid.NeighborDx[n];
                        var nz = cz + HydrologyGrid.NeighborDz[n];
                        if (!grid.InBounds(nx, nz))
                        {
                            continue;
                        }

                        var ni = grid.Index(nx, nz);
                        if (labels[ni] >= 0
                            || !IsSubmergedByFill(grid, filled, oceanMask, ni, tolerance))
                        {
                            continue;
                        }

                        labels[ni] = lakes.Count;
                        stack.Push(ni);
                    }
                }

                var cells = component.ToArray();
                Array.Sort(cells);

                // The epsilon gradient rises as the flood works inward, so the lowest filled value
                // in the basin is the cell the flood entered through — i.e. the spill. That is the
                // water level, and the rest of the basin's filled values are an artefact of the
                // algorithm, not a sloping lake.
                var surface = float.MaxValue;
                var spillCell = cells[0];
                for (var c = 0; c < cells.Length; c++)
                {
                    var v = filled[cells[c]];
                    if (v < surface)
                    {
                        surface = v;
                        spillCell = cells[c];
                    }
                }

                var maxDepth = 0f;
                for (var c = 0; c < cells.Length; c++)
                {
                    var depth = surface - grid.Heights[cells[c]];
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
                }

                var areaSqm = cells.Length * grid.CellAreaSqm;
                if (areaSqm < settings.MinLakeAreaSqm || maxDepth < settings.MinLakeDepthMeters)
                {
                    // Too small or too shallow to read as a lake. Unlabel it so nothing downstream
                    // treats it as water; the depression stays filled, so drainage is unaffected.
                    for (var c = 0; c < cells.Length; c++)
                    {
                        labels[cells[c]] = -1;
                    }

                    continue;
                }

                var boundary = TraceBoundary(grid, labels, lakes.Count, cells[0]);
                lakes.Add(new LakeCandidate(
                    new LakeId(lakes.Count),
                    cells,
                    surface,
                    maxDepth,
                    areaSqm,
                    spillCell,
                    grid.PositionXZ(spillCell),
                    boundary));
            }

            return lakes.Count == 0 ? LakeField.Empty : new LakeField(lakes, labels);
        }

        private static bool IsSubmergedByFill(
            HydrologyGrid grid, float[] filled, bool[] oceanMask, int index, float tolerance)
        {
            return !oceanMask[index] && filled[index] - grid.Heights[index] > tolerance;
        }

        /// <summary>
        /// Walks the outline of one labelled region along the cracks between cells.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Crack following rather than Moore tracing, because it produces a proper closed polygon on
        /// the corner lattice instead of a chain of cell centres, and needs no special case where
        /// the region is one cell wide. The walk keeps the region on its left, so the result comes
        /// out counter-clockwise.
        /// </para>
        /// <para>
        /// Only the outer boundary is traced. An island inside a lake is not represented — the
        /// water surface simply covers it, which at this scale is invisible and is cheaper than
        /// carrying polygon holes through the mesh builder.
        /// </para>
        /// </remarks>
        private static Vector2[] TraceBoundary(
            HydrologyGrid grid, int[] labels, int label, int firstCell)
        {
            // Direction 0 = +x, 1 = +z, 2 = -x, 3 = -z.
            var dx = new[] { 1, 0, -1, 0 };
            var dz = new[] { 0, 1, 0, -1 };

            // Cells lying ahead-left and ahead-right of a corner for each direction. This table is
            // what encodes "region stays on the left".
            var leftDx = new[] { 0, -1, -1, 0 };
            var leftDz = new[] { 0, 0, -1, -1 };
            var rightDx = new[] { 0, 0, -1, -1 };
            var rightDz = new[] { -1, 0, 0, -1 };

            // The lowest-index cell of the component: the row-major first, so the cell below it and
            // the cell left of it are both outside and heading +x with the region on the left is a
            // valid starting state.
            var startCornerX = firstCell % grid.Width;
            var startCornerZ = firstCell / grid.Width;

            var cornerX = startCornerX;
            var cornerZ = startCornerZ;
            var direction = 0;
            var startDirection = -1;

            var corners = new List<Vector2>(64);
            var guard = grid.CellCount * 4 + 16;

            while (guard-- > 0)
            {
                var left = IsLabelled(
                    grid, labels, label, cornerX + leftDx[direction], cornerZ + leftDz[direction]);
                var right = IsLabelled(
                    grid, labels, label, cornerX + rightDx[direction], cornerZ + rightDz[direction]);

                if (!left && !right)
                {
                    direction = (direction + 1) & 3;
                }
                else if (left && right)
                {
                    direction = (direction + 3) & 3;
                }
                else if (!left)
                {
                    // Diagonal pinch. Turning right splits it consistently, which keeps the walk
                    // closed instead of doubling back over an edge it already used.
                    direction = (direction + 3) & 3;
                }

                // Closed when the walk is about to leave the starting corner the same way it first
                // did. Tested here, before the step, because the state that identifies a lap is the
                // corner plus the direction the walk departs in — comparing after the step instead
                // compares the arrival direction, never matches, and runs the walk until the guard
                // trips.
                if (startDirection >= 0
                    && cornerX == startCornerX && cornerZ == startCornerZ
                    && direction == startDirection)
                {
                    break;
                }

                if (startDirection < 0)
                {
                    startDirection = direction;
                }

                corners.Add(CornerToWorld(grid, cornerX, cornerZ));
                cornerX += dx[direction];
                cornerZ += dz[direction];
            }

            if (guard <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Hydrology] Shoreline trace for lake label {label} did not close and was cut "
                    + "short. The outline is approximate.");
            }

            return Simplify(corners, grid.CellSize * 0.75f);
        }

        private static bool IsLabelled(HydrologyGrid grid, int[] labels, int label, int x, int z) =>
            grid.InBounds(x, z) && labels[grid.Index(x, z)] == label;

        /// <summary>Corner (cx, cz) is the lower-left corner of cell (cx, cz).</summary>
        private static Vector2 CornerToWorld(HydrologyGrid grid, int cornerX, int cornerZ) =>
            new Vector2(
                grid.OriginX + (cornerX - 0.5f) * grid.CellSize,
                grid.OriginZ + (cornerZ - 0.5f) * grid.CellSize);

        /// <summary>Douglas-Peucker on a closed ring, iterative so a long shoreline cannot recurse deep.</summary>
        private static Vector2[] Simplify(List<Vector2> points, float tolerance)
        {
            if (points.Count < 4)
            {
                return points.ToArray();
            }

            var keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;

            var ranges = new Stack<(int from, int to)>();
            ranges.Push((0, points.Count - 1));

            while (ranges.Count > 0)
            {
                var (from, to) = ranges.Pop();
                if (to <= from + 1)
                {
                    continue;
                }

                var worst = 0f;
                var worstIndex = -1;
                for (var i = from + 1; i < to; i++)
                {
                    var d = PerpendicularDistance(points[i], points[from], points[to]);
                    if (d > worst)
                    {
                        worst = d;
                        worstIndex = i;
                    }
                }

                if (worstIndex < 0 || worst <= tolerance)
                {
                    continue;
                }

                keep[worstIndex] = true;
                ranges.Push((from, worstIndex));
                ranges.Push((worstIndex, to));
            }

            var result = new List<Vector2>(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                if (keep[i])
                {
                    result.Add(points[i]);
                }
            }

            // A ring simplified down to a degenerate sliver is worse than a coarse one; fall back to
            // the unsimplified outline rather than emitting something a mesh builder cannot use.
            return result.Count >= 3 ? result.ToArray() : points.ToArray();
        }

        private static float PerpendicularDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lengthSq = ab.sqrMagnitude;
            if (lengthSq < 1e-6f)
            {
                return Vector2.Distance(point, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return Vector2.Distance(point, a + ab * t);
        }
    }
}
