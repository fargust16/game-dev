using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Turns a land raster into published coastline polygons: enclosed-sea fill, component
    /// labelling, boundary trace, simplification, and constrained midpoint displacement.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The raster is scaffolding; the polygon is the truth. Everything coastal downstream — the
    /// signed distance field, the land mask used for statistics, and later the coastal profile
    /// selection — is a function of the refined polygon, never of the raster it came from.
    /// </para>
    /// <para>
    /// The boundary walk is the same square-tracing algorithm as
    /// <c>BasinExtraction.TraceBoundary</c>, which is private to the hydrology package and bound to
    /// <c>HydrologyGrid</c>. Extracting a shared version would mean editing that file; the two
    /// should be merged when hydrology and terrain generation next move together, and that is
    /// recorded here rather than done silently.
    /// </para>
    /// </remarks>
    public static class CoastOutline
    {
        /// <summary>Bucket size for the self-intersection check, metres.</summary>
        private const float IntersectionBucketMeters = 32f;

        /// <summary>
        /// Fills sea cells that cannot reach the raster border into land.
        /// </summary>
        /// <remarks>
        /// A pocket of sea fully enclosed by land would make the traced ring a polygon with a hole,
        /// and the outline model has no holes. Filling is the honest resolution: an enclosed hollow
        /// at this scale is land the growth simply did not claim, and turning it into a lake is the
        /// drainage stage's decision to make on the height field, not the outline's.
        /// </remarks>
        public static int FillEnclosedSea(bool[] land, int width, int height)
        {
            var reached = new bool[land.Length];
            var stack = new Stack<int>(width * 2);

            void Push(int x, int z)
            {
                if (x < 0 || z < 0 || x >= width || z >= height)
                {
                    return;
                }

                var i = z * width + x;
                if (land[i] || reached[i])
                {
                    return;
                }

                reached[i] = true;
                stack.Push(i);
            }

            for (var x = 0; x < width; x++)
            {
                Push(x, 0);
                Push(x, height - 1);
            }

            for (var z = 0; z < height; z++)
            {
                Push(0, z);
                Push(width - 1, z);
            }

            while (stack.Count > 0)
            {
                var i = stack.Pop();
                var x = i % width;
                var z = i / width;
                Push(x + 1, z);
                Push(x - 1, z);
                Push(x, z + 1);
                Push(x, z - 1);
            }

            var filled = 0;
            for (var i = 0; i < land.Length; i++)
            {
                if (!land[i] && !reached[i])
                {
                    land[i] = true;
                    filled++;
                }
            }

            return filled;
        }

        /// <summary>
        /// Labels 4-connected land components, numbered in row-major order of their first cell.
        /// </summary>
        /// <returns>Component count. <paramref name="labels"/> is -1 on sea cells.</returns>
        public static int LabelComponents(bool[] land, int width, int height, out int[] labels)
        {
            // Built into a local and assigned out at the end: an out parameter cannot be captured
            // by the local function the flood fill uses.
            var result = new int[land.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = -1;
            }

            var count = 0;
            var stack = new Stack<int>(64);
            var label = 0;

            void Visit(int nx, int nz)
            {
                if (nx < 0 || nz < 0 || nx >= width || nz >= height)
                {
                    return;
                }

                var n = nz * width + nx;
                if (!land[n] || result[n] >= 0)
                {
                    return;
                }

                result[n] = label;
                stack.Push(n);
            }

            for (var seed = 0; seed < land.Length; seed++)
            {
                if (!land[seed] || result[seed] >= 0)
                {
                    continue;
                }

                label = count++;
                result[seed] = label;
                stack.Push(seed);

                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    var x = i % width;
                    var z = i / width;
                    Visit(x + 1, z);
                    Visit(x - 1, z);
                    Visit(x, z + 1);
                    Visit(x, z - 1);
                }
            }

            labels = result;
            return count;
        }

        /// <summary>
        /// Traces the outer boundary of one labelled component as a closed ring of cell corners,
        /// counter-clockwise, in world metres.
        /// </summary>
        public static Vector2[] TraceComponent(
            int[] labels, int width, int height, int label, float cellMeters)
        {
            var firstCell = -1;
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] == label)
                {
                    firstCell = i;
                    break;
                }
            }

            if (firstCell < 0)
            {
                return System.Array.Empty<Vector2>();
            }

            // Direction 0 = +x, 1 = +z, 2 = -x, 3 = -z.
            var stepX = new[] { 1, 0, -1, 0 };
            var stepZ = new[] { 0, 1, 0, -1 };

            // The cells ahead-left and ahead-right of a corner for each direction. This table is
            // what encodes "the component stays on the left", which is what makes the ring CCW.
            var leftX = new[] { 0, -1, -1, 0 };
            var leftZ = new[] { 0, 0, -1, -1 };
            var rightX = new[] { 0, 0, -1, -1 };
            var rightZ = new[] { -1, 0, 0, -1 };

            bool Labelled(int x, int z) =>
                x >= 0 && z >= 0 && x < width && z < height && labels[z * width + x] == label;

            // Row-major first cell: the cell below it and the cell left of it are both outside the
            // component, so starting at its lower-left corner heading +x is a valid state.
            var startX = firstCell % width;
            var startZ = firstCell / width;

            var cornerX = startX;
            var cornerZ = startZ;
            var direction = 0;
            var startDirection = -1;

            var corners = new List<Vector2>(256);
            var guard = labels.Length * 4 + 16;

            while (guard-- > 0)
            {
                var left = Labelled(cornerX + leftX[direction], cornerZ + leftZ[direction]);
                var right = Labelled(cornerX + rightX[direction], cornerZ + rightZ[direction]);

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
                    // Diagonal pinch. Turning the same way every time keeps the walk closed instead
                    // of doubling back over an edge it already used.
                    direction = (direction + 3) & 3;
                }

                if (startDirection >= 0 &&
                    cornerX == startX && cornerZ == startZ && direction == startDirection)
                {
                    break;
                }

                if (startDirection < 0)
                {
                    startDirection = direction;
                }

                corners.Add(new Vector2(cornerX * cellMeters, cornerZ * cellMeters));
                cornerX += stepX[direction];
                cornerZ += stepZ[direction];
            }

            return corners.ToArray();
        }

        /// <summary>
        /// Douglas-Peucker on a closed ring, which recovers the long straight cell edges from the
        /// staircase the raster trace produces.
        /// </summary>
        public static Vector2[] Simplify(Vector2[] ring, float toleranceMeters)
        {
            if (ring == null || ring.Length < 8 || toleranceMeters <= 0f)
            {
                return ring;
            }

            // Split at the vertex farthest from vertex 0 so the two halves are both well-conditioned
            // open polylines; simplifying a closed ring as one polyline can collapse it entirely.
            var far = 0;
            var farSq = -1f;
            for (var i = 1; i < ring.Length; i++)
            {
                var sq = (ring[i] - ring[0]).sqrMagnitude;
                if (sq > farSq)
                {
                    farSq = sq;
                    far = i;
                }
            }

            var firstHalf = new List<Vector2>();
            for (var i = 0; i <= far; i++)
            {
                firstHalf.Add(ring[i]);
            }

            var secondHalf = new List<Vector2>();
            for (var i = far; i < ring.Length; i++)
            {
                secondHalf.Add(ring[i]);
            }

            secondHalf.Add(ring[0]);

            var a = new List<Vector2>();
            SimplifyOpen(firstHalf, 0, firstHalf.Count - 1, toleranceMeters, a);
            var b = new List<Vector2>();
            SimplifyOpen(secondHalf, 0, secondHalf.Count - 1, toleranceMeters, b);

            var result = new List<Vector2>(a.Count + b.Count);
            result.AddRange(a);
            // Drop b's first (== a's last) and last (== ring[0]) so the ring stays implicit.
            for (var i = 1; i < b.Count - 1; i++)
            {
                result.Add(b[i]);
            }

            return result.Count >= 3 ? result.ToArray() : ring;
        }

        private static void SimplifyOpen(
            List<Vector2> pts, int first, int last, float tolerance, List<Vector2> output)
        {
            var maxDist = -1f;
            var maxIndex = -1;
            for (var i = first + 1; i < last; i++)
            {
                var closest = Geo2.ClosestPointOnSegment(pts[i], pts[first], pts[last]);
                var d = (pts[i] - closest).magnitude;
                if (d > maxDist)
                {
                    maxDist = d;
                    maxIndex = i;
                }
            }

            if (maxIndex > 0 && maxDist > tolerance)
            {
                SimplifyOpen(pts, first, maxIndex, tolerance, output);
                output.RemoveAt(output.Count - 1);
                SimplifyOpen(pts, maxIndex, last, tolerance, output);
                return;
            }

            output.Add(pts[first]);
            output.Add(pts[last]);
        }

        /// <summary>
        /// Constrained midpoint displacement: subdivides every segment, displacing each new midpoint
        /// perpendicular to its segment.
        /// </summary>
        /// <remarks>
        /// The displacement is a fixed fraction of the segment being subdivided, held constant
        /// across levels, so the excursion halves as the segments halve and the curve is
        /// self-similar rather than smooth-then-noisy. Points are clamped to the world rect inset by
        /// the ocean margin, which is what keeps the coast off the world border.
        /// </remarks>
        public static Vector2[] Displace(
            Vector2[] ring,
            float roughness,
            float minSegmentMeters,
            Random rnd,
            float insetMin,
            float insetMaxX,
            float insetMaxZ)
        {
            if (ring == null || ring.Length < 3 || roughness <= 0f)
            {
                return ring;
            }

            var current = new List<Vector2>(ring);
            const int maxLevels = 8;

            for (var level = 0; level < maxLevels; level++)
            {
                var any = false;
                var next = new List<Vector2>(current.Count * 2);

                for (var i = 0; i < current.Count; i++)
                {
                    var a = current[i];
                    var b = current[(i + 1) % current.Count];
                    next.Add(a);

                    var delta = b - a;
                    var length = delta.magnitude;
                    if (length <= minSegmentMeters)
                    {
                        continue;
                    }

                    any = true;
                    var normal = new Vector2(-delta.y, delta.x) / length;
                    var offset = roughness * length * (float)(rnd.NextDouble() * 2.0 - 1.0);
                    var mid = (a + b) * 0.5f + normal * offset;

                    mid.x = Mathf.Clamp(mid.x, insetMin, insetMaxX);
                    mid.y = Mathf.Clamp(mid.y, insetMin, insetMaxZ);
                    next.Add(mid);
                }

                current = next;
                if (!any)
                {
                    break;
                }
            }

            return current.ToArray();
        }

        /// <summary>
        /// True when no two non-adjacent segments of the ring cross.
        /// </summary>
        /// <remarks>
        /// Bucketed, because a displaced ring runs to a few thousand vertices and the quadratic form
        /// would dominate generation. Adjacent pairs are skipped: they share an endpoint, and
        /// <c>Geo2.SegmentIntersect</c> counts a shared endpoint as a hit.
        /// </remarks>
        public static bool IsSimple(IReadOnlyList<Vector2> ring)
        {
            var n = ring?.Count ?? 0;
            if (n < 4)
            {
                return n >= 3;
            }

            var buckets = new Dictionary<long, List<int>>(n);

            long Key(int bx, int bz) => ((long)bx << 32) ^ (uint)bz;

            for (var i = 0; i < n; i++)
            {
                var a = ring[i];
                var b = ring[(i + 1) % n];
                var minBx = Mathf.FloorToInt(Mathf.Min(a.x, b.x) / IntersectionBucketMeters);
                var maxBx = Mathf.FloorToInt(Mathf.Max(a.x, b.x) / IntersectionBucketMeters);
                var minBz = Mathf.FloorToInt(Mathf.Min(a.y, b.y) / IntersectionBucketMeters);
                var maxBz = Mathf.FloorToInt(Mathf.Max(a.y, b.y) / IntersectionBucketMeters);

                for (var bz = minBz; bz <= maxBz; bz++)
                {
                    for (var bx = minBx; bx <= maxBx; bx++)
                    {
                        var key = Key(bx, bz);
                        if (!buckets.TryGetValue(key, out var list))
                        {
                            list = new List<int>(4);
                            buckets[key] = list;
                        }

                        list.Add(i);
                    }
                }
            }

            foreach (var kv in buckets)
            {
                var list = kv.Value;
                for (var a = 0; a < list.Count; a++)
                {
                    for (var b = a + 1; b < list.Count; b++)
                    {
                        var i = list[a];
                        var j = list[b];
                        if (i == j)
                        {
                            continue;
                        }

                        var adjacent = (i + 1) % n == j || (j + 1) % n == i;
                        if (adjacent)
                        {
                            continue;
                        }

                        if (Geo2.SegmentIntersect(
                                ring[i], ring[(i + 1) % n], ring[j], ring[(j + 1) % n], out _))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public static float Perimeter(IReadOnlyList<Vector2> ring)
        {
            var n = ring?.Count ?? 0;
            if (n < 2)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < n; i++)
            {
                total += (ring[(i + 1) % n] - ring[i]).magnitude;
            }

            return total;
        }
    }
}
