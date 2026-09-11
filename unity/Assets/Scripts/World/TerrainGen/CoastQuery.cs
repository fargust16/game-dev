using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The two questions the coastal height model asks of the coastline: how far is this point from
    /// the shore, and is it on the land side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distance is answered from a bucketed segment index with a bounded search radius. The bound is
    /// not an approximation: past <see cref="TerrainGenSettings.MaxCoastDistanceMeters"/> every
    /// profile is saturated, so an exact distance cannot change the height.
    /// </para>
    /// <para>
    /// Each bucket also carries the Chebyshev distance, in buckets, to the nearest bucket that holds
    /// a segment. That single array is what makes the field affordable: the search starts at that
    /// ring instead of at zero, so a point in the middle of the ocean stops after one test rather
    /// than sweeping seventeen empty rings, and a point in the interior skips straight to the shore.
    /// Without it the 1025 x 1025 evaluation spends most of its time looking at empty buckets.
    /// </para>
    /// <para>
    /// Inside/outside is even-odd ray crossing, and it is deliberately available in two forms that
    /// share one predicate: per point, and per lattice row. The bulk field evaluation uses the row
    /// form and the point query uses the point form, so the D2 test - "the point query agrees with
    /// the bulk field" - compares two genuinely different code paths instead of calling the same one
    /// twice.
    /// </para>
    /// </remarks>
    public sealed class CoastQuery
    {
        private const float BucketMeters = 64f;

        private readonly Vector2[] segA;
        private readonly Vector2[] segB;
        private readonly int[][] buckets;
        private readonly int[] bucketRing;
        private readonly int gridWidth;
        private readonly int gridHeight;
        private readonly float maxDistance;
        private readonly int maxRing;

        /// <summary>The rings this query answers about, in published order.</summary>
        public IReadOnlyList<Vector2[]> Rings { get; }

        public int SegmentCount => segA.Length;

        public CoastQuery(IReadOnlyList<Vector2[]> rings, float maxDistanceMeters)
        {
            Rings = rings ?? System.Array.Empty<Vector2[]>();
            maxDistance = Mathf.Max(1f, maxDistanceMeters);
            maxRing = Mathf.CeilToInt(maxDistance / BucketMeters) + 2;

            var total = 0;
            for (var r = 0; r < Rings.Count; r++)
            {
                var ring = Rings[r];
                if (ring != null && ring.Length >= 3)
                {
                    total += ring.Length;
                }
            }

            segA = new Vector2[total];
            segB = new Vector2[total];

            var s = 0;
            for (var r = 0; r < Rings.Count; r++)
            {
                var ring = Rings[r];
                if (ring == null || ring.Length < 3)
                {
                    continue;
                }

                for (var i = 0; i < ring.Length; i++)
                {
                    segA[s] = ring[i];
                    segB[s] = ring[(i + 1) % ring.Length];
                    s++;
                }
            }

            gridWidth = Mathf.CeilToInt(WorldScale.WorldExtentXMeters / BucketMeters) + 1;
            gridHeight = Mathf.CeilToInt(WorldScale.WorldExtentZMeters / BucketMeters) + 1;

            var lists = new List<int>[gridWidth * gridHeight];
            for (var i = 0; i < segA.Length; i++)
            {
                var a = segA[i];
                var b = segB[i];
                var minBx = BucketX(Mathf.Min(a.x, b.x));
                var maxBx = BucketX(Mathf.Max(a.x, b.x));
                var minBz = BucketZ(Mathf.Min(a.y, b.y));
                var maxBz = BucketZ(Mathf.Max(a.y, b.y));

                for (var bz = minBz; bz <= maxBz; bz++)
                {
                    for (var bx = minBx; bx <= maxBx; bx++)
                    {
                        var slot = bz * gridWidth + bx;
                        (lists[slot] ??= new List<int>(4)).Add(i);
                    }
                }
            }

            buckets = new int[lists.Length][];
            for (var i = 0; i < lists.Length; i++)
            {
                buckets[i] = lists[i]?.ToArray();
            }

            bucketRing = BuildRingDistances(buckets, gridWidth, gridHeight);
        }

        private int BucketX(float worldX) =>
            Mathf.Clamp(Mathf.FloorToInt(worldX / BucketMeters), 0, gridWidth - 1);

        private int BucketZ(float worldZ) =>
            Mathf.Clamp(Mathf.FloorToInt(worldZ / BucketMeters), 0, gridHeight - 1);

        /// <summary>
        /// Chebyshev distance in buckets from every bucket to the nearest bucket holding a segment.
        /// </summary>
        /// <remarks>
        /// A multi-source BFS over the 8-connected bucket grid, which is exactly Chebyshev distance.
        /// Buckets with no reachable segment keep <see cref="int.MaxValue"/>, which the query reads
        /// as "further than the search radius" without doing any work.
        /// </remarks>
        private static int[] BuildRingDistances(int[][] buckets, int width, int height)
        {
            var dist = new int[buckets.Length];
            for (var i = 0; i < dist.Length; i++)
            {
                dist[i] = int.MaxValue;
            }

            var queue = new Queue<int>(buckets.Length);
            for (var i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] != null)
                {
                    dist[i] = 0;
                    queue.Enqueue(i);
                }
            }

            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                var x = i % width;
                var z = i / width;
                var next = dist[i] + 1;

                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                        {
                            continue;
                        }

                        var nx = x + dx;
                        var nz = z + dz;
                        if (nx < 0 || nz < 0 || nx >= width || nz >= height)
                        {
                            continue;
                        }

                        var n = nz * width + nx;
                        if (dist[n] <= next)
                        {
                            continue;
                        }

                        dist[n] = next;
                        queue.Enqueue(n);
                    }
                }
            }

            return dist;
        }

        /// <summary>Distance to the nearest coastline segment, clamped to the configured maximum.</summary>
        public float NearestDistance(float worldX, float worldZ)
        {
            if (segA.Length == 0)
            {
                return maxDistance;
            }

            var bx = BucketX(worldX);
            var bz = BucketZ(worldZ);
            var start = bucketRing[bz * gridWidth + bx];
            if (start == int.MaxValue)
            {
                return maxDistance;
            }

            // Nothing closer than (start - 1) buckets exists, so both the early-out and the loop
            // start there.
            if ((start - 1) * BucketMeters >= maxDistance)
            {
                return maxDistance;
            }

            var p = new Vector2(worldX, worldZ);
            var best = maxDistance;

            for (var r = start; r <= maxRing; r++)
            {
                var floorDistance = (r - 1) * BucketMeters;
                if (floorDistance >= best)
                {
                    break;
                }

                if (r == 0)
                {
                    ScanBucket(bx, bz, p, ref best);
                    continue;
                }

                for (var dx = -r; dx <= r; dx++)
                {
                    ScanBucket(bx + dx, bz - r, p, ref best);
                    ScanBucket(bx + dx, bz + r, p, ref best);
                }

                for (var dz = -r + 1; dz <= r - 1; dz++)
                {
                    ScanBucket(bx - r, bz + dz, p, ref best);
                    ScanBucket(bx + r, bz + dz, p, ref best);
                }
            }

            return best;
        }

        private void ScanBucket(int bx, int bz, Vector2 p, ref float best)
        {
            if (bx < 0 || bz < 0 || bx >= gridWidth || bz >= gridHeight)
            {
                return;
            }

            var list = buckets[bz * gridWidth + bx];
            if (list == null)
            {
                return;
            }

            for (var k = 0; k < list.Length; k++)
            {
                var i = list[k];
                var closest = Geo2.ClosestPointOnSegment(p, segA[i], segB[i]);
                var d = (p - closest).magnitude;
                if (d < best)
                {
                    best = d;
                }
            }
        }

        /// <summary>Even-odd crossings of the horizontal line at <paramref name="worldZ"/>, sorted.</summary>
        public void RowCrossings(float worldZ, List<float> buffer)
        {
            buffer.Clear();
            for (var i = 0; i < segA.Length; i++)
            {
                var a = segA[i];
                var b = segB[i];
                var aBelow = a.y <= worldZ;
                var bBelow = b.y <= worldZ;
                if (aBelow == bBelow)
                {
                    continue;
                }

                var t = (worldZ - a.y) / (b.y - a.y);
                buffer.Add(a.x + t * (b.x - a.x));
            }

            buffer.Sort();
        }

        /// <summary>
        /// The one inside/outside predicate. Both the point query and the bulk row fill go through
        /// this, so the two cannot drift apart.
        /// </summary>
        public static bool InsideFromSortedCrossings(List<float> sortedCrossings, float worldX)
        {
            var above = 0;
            for (var i = sortedCrossings.Count - 1; i >= 0; i--)
            {
                if (sortedCrossings[i] <= worldX)
                {
                    break;
                }

                above++;
            }

            return (above & 1) == 1;
        }

        /// <summary>Land test for a single point, independent of any lattice.</summary>
        public bool IsInside(float worldX, float worldZ)
        {
            var buffer = new List<float>(8);
            RowCrossings(worldZ, buffer);
            return InsideFromSortedCrossings(buffer, worldX);
        }

        /// <summary>Signed distance to the coastline: positive inland, negative offshore.</summary>
        public float SignedDistance(float worldX, float worldZ)
        {
            var d = NearestDistance(worldX, worldZ);
            return IsInside(worldX, worldZ) ? d : -d;
        }
    }
}
