using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Merges flood zones that actually touch (flood cells within a small gap), fills holes, and
    /// renumbers. AABB-only merge is forbidden — elongated plains floods would glue into one
    /// tile-sized mega-lake and kill mountain→lake rivers.
    /// </summary>
    public static class LakeZoneMerger
    {
        /// <param name="gapMeters">
        /// Flood cells within this gap count as one lake. Keep small (one shoreline, not half a tile).
        /// </param>
        /// <param name="maxCellsPerLake">
        /// Refuse a merge that would create a lake larger than this (stops mega-lake glue).
        /// </param>
        public static WaterZone[] MergeNearby(WaterZone[] zones, float gapMeters, int maxCellsPerLake = 1600)
        {
            if (zones == null || zones.Length == 0)
            {
                return System.Array.Empty<WaterZone>();
            }

            if (zones.Length == 1)
            {
                return new[] { FinalizeZone(zones[0], "lake_0") };
            }

            var gapCells = Mathf.Max(1, Mathf.CeilToInt(gapMeters / Mathf.Max(zones[0].CellSize, 0.01f)));
            var parent = new int[zones.Length];
            for (var i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            int Find(int i)
            {
                while (parent[i] != i)
                {
                    parent[i] = parent[parent[i]];
                    i = parent[i];
                }

                return i;
            }

            void Union(int a, int b)
            {
                var ra = Find(a);
                var rb = Find(b);
                if (ra != rb)
                {
                    parent[rb] = ra;
                }
            }

            for (var i = 0; i < zones.Length; i++)
            {
                for (var j = i + 1; j < zones.Length; j++)
                {
                    if (!FloodsTouch(zones[i], zones[j], gapCells))
                    {
                        continue;
                    }

                    if (MergedFloodCount(zones[i], zones[j]) > maxCellsPerLake)
                    {
                        continue;
                    }

                    Union(i, j);
                }
            }

            var groups = new Dictionary<int, List<int>>();
            for (var i = 0; i < zones.Length; i++)
            {
                var root = Find(i);
                if (!groups.TryGetValue(root, out var list))
                {
                    list = new List<int>();
                    groups[root] = list;
                }

                list.Add(i);
            }

            var merged = new List<WaterZone>(groups.Count);
            var index = 0;
            foreach (var members in groups.Values)
            {
                merged.Add(MergeGroup(zones, members, $"lake_{index}"));
                index++;
            }

            return merged.ToArray();
        }

        /// <summary>
        /// Keeps one lake per visual crater (greedy, non-transitive). Union-find clustering was
        /// wrong here: A≈B and B≈C chained across the plains into one mega-group, then the cell
        /// cap discarded everything but the largest lobe — seeds with "1 lake" / "3 lakes".
        /// Pairwise expand into the kept mask when under the cell cap so MapMagic pits still fill.
        /// </summary>
        public static WaterZone[] CullOverlapping(
            WaterZone[] zones,
            float maxOverlapFraction = 0.12f,
            int maxCellsPerLake = 2400)
        {
            if (zones == null || zones.Length == 0)
            {
                return System.Array.Empty<WaterZone>();
            }

            if (zones.Length == 1)
            {
                return new[] { CloneWithId(zones[0], "lake_0") };
            }

            var ordered = new List<WaterZone>(zones);
            ordered.Sort((a, b) => b.FloodedCellCount.CompareTo(a.FloodedCellCount));

            var kept = new List<WaterZone>(ordered.Count);
            foreach (var candidate in ordered)
            {
                var matchIdx = -1;
                for (var i = 0; i < kept.Count; i++)
                {
                    if (SameVisualBasin(candidate, kept[i], maxOverlapFraction))
                    {
                        matchIdx = i;
                        break;
                    }
                }

                if (matchIdx < 0)
                {
                    kept.Add(candidate);
                    continue;
                }

                if (MergedFloodCount(kept[matchIdx], candidate) <= maxCellsPerLake)
                {
                    var pair = new[] { kept[matchIdx], candidate };
                    kept[matchIdx] = MergeGroup(pair, new List<int> { 0, 1 }, kept[matchIdx].Id);
                }
                // else drop candidate — one visual basin keeps the larger lobe only
            }

            return Renumber(kept);
        }

        /// <summary>Keep at most <paramref name="maxCount"/> largest lakes (after spatial cull).</summary>
        public static WaterZone[] CapCount(WaterZone[] zones, int maxCount)
        {
            if (zones == null || zones.Length == 0)
            {
                return System.Array.Empty<WaterZone>();
            }

            if (maxCount < 1 || zones.Length <= maxCount)
            {
                return Renumber(new List<WaterZone>(zones));
            }

            var ordered = new List<WaterZone>(zones);
            ordered.Sort((a, b) => b.FloodedCellCount.CompareTo(a.FloodedCellCount));
            ordered.RemoveRange(maxCount, ordered.Count - maxCount);
            return Renumber(ordered);
        }

        /// <summary>
        /// Pads <paramref name="culled"/> up to <paramref name="minCount"/> using spatially
        /// separated candidates from <paramref name="pool"/> (pre-cull merges). Stabilises seeds
        /// that would otherwise ship 1–2 lakes.
        /// </summary>
        public static WaterZone[] EnsureMinDistinct(
            WaterZone[] culled,
            WaterZone[] pool,
            int minCount,
            float minCenterSeparationMeters)
        {
            var result = new List<WaterZone>();
            if (culled != null)
            {
                result.AddRange(culled);
            }

            if (result.Count >= minCount || pool == null || pool.Length == 0)
            {
                return Renumber(result);
            }

            var ordered = new List<WaterZone>(pool);
            ordered.Sort((a, b) => b.FloodedCellCount.CompareTo(a.FloodedCellCount));

            foreach (var candidate in ordered)
            {
                if (result.Count >= minCount)
                {
                    break;
                }

                var ok = true;
                foreach (var kept in result)
                {
                    if (OverlapFractionOfSmaller(candidate, kept) >= 0.2f)
                    {
                        ok = false;
                        break;
                    }

                    var ca = candidate.GetFloodCentroidXZ();
                    var cb = kept.GetFloodCentroidXZ();
                    var dx = ca.x - cb.x;
                    var dz = ca.z - cb.z;
                    if (dx * dx + dz * dz < minCenterSeparationMeters * minCenterSeparationMeters)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    result.Add(candidate);
                }
            }

            return Renumber(result);
        }

        private static WaterZone[] Renumber(List<WaterZone> zones)
        {
            var result = new WaterZone[zones.Count];
            for (var i = 0; i < zones.Count; i++)
            {
                result[i] = CloneWithId(zones[i], $"lake_{i}");
            }

            return result;
        }

        /// <summary>
        /// True when two floods read as the same crater / shoreline (player would see one lake).
        /// Includes near-touching AABBs — soft bank carve (~72m) makes close lobes look merged.
        /// </summary>
        public static bool SameVisualBasin(WaterZone a, WaterZone b, float maxOverlapFraction = 0.12f)
        {
            if (a == null || b == null)
            {
                return false;
            }

            if (OverlapFractionOfSmaller(a, b) >= maxOverlapFraction)
            {
                return true;
            }

            var aabbA = a.GetFloodAabbXZ();
            var aabbB = b.GetFloodAabbXZ();
            if (AabbIoU(aabbA, aabbB) >= 0.18f)
            {
                return true;
            }

            // Soft-bank visual merge: shores within ~90m look like one water body after carve.
            if (AabbSeparation(aabbA, aabbB) < 90f)
            {
                return true;
            }

            var ca = a.GetFloodCentroidXZ();
            var cb = b.GetFloodCentroidXZ();
            var dx = ca.x - cb.x;
            var dz = ca.z - cb.z;
            var dist = Mathf.Sqrt(dx * dx + dz * dz);
            var link = a.EquivalentRadiusMeters + b.EquivalentRadiusMeters + 40f;
            return dist <= link;
        }

        /// <summary>0 if AABBs overlap; otherwise Euclidean gap between rect edges.</summary>
        public static float AabbSeparation(Rect a, Rect b)
        {
            var dx = 0f;
            if (a.xMax < b.xMin)
            {
                dx = b.xMin - a.xMax;
            }
            else if (b.xMax < a.xMin)
            {
                dx = a.xMin - b.xMax;
            }

            var dy = 0f;
            if (a.yMax < b.yMin)
            {
                dy = b.yMin - a.yMax;
            }
            else if (b.yMax < a.yMin)
            {
                dy = a.yMin - b.yMax;
            }

            if (dx <= 0f && dy <= 0f)
            {
                return 0f;
            }

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public static float AabbIoU(Rect a, Rect b)
        {
            var x0 = Mathf.Max(a.xMin, b.xMin);
            var y0 = Mathf.Max(a.yMin, b.yMin);
            var x1 = Mathf.Min(a.xMax, b.xMax);
            var y1 = Mathf.Min(a.yMax, b.yMax);
            var w = x1 - x0;
            var h = y1 - y0;
            if (w <= 0f || h <= 0f)
            {
                return 0f;
            }

            var inter = w * h;
            var uni = a.width * a.height + b.width * b.height - inter;
            return uni > 0f ? inter / uni : 0f;
        }

        /// <summary>
        /// Fraction of the smaller lake's flooded cells that also sit in the larger lake.
        /// </summary>
        public static float OverlapFractionOfSmaller(WaterZone a, WaterZone b)
        {
            if (a == null || b == null || a.FloodedCellCount == 0 || b.FloodedCellCount == 0)
            {
                return 0f;
            }

            var smaller = a.FloodedCellCount <= b.FloodedCellCount ? a : b;
            var larger = a.FloodedCellCount <= b.FloodedCellCount ? b : a;
            if (smaller.Width != larger.Width || smaller.Depth != larger.Depth ||
                !Mathf.Approximately(smaller.OriginX, larger.OriginX) ||
                !Mathf.Approximately(smaller.OriginZ, larger.OriginZ))
            {
                return smaller.GetFloodAabbXZ().Overlaps(larger.GetFloodAabbXZ()) ? 1f : 0f;
            }

            var overlap = 0;
            for (var x = 0; x < smaller.Width; x++)
            {
                for (var z = 0; z < smaller.Depth; z++)
                {
                    if (smaller.IsFlooded(x, z) && larger.IsFlooded(x, z))
                    {
                        overlap++;
                    }
                }
            }

            return (float)overlap / smaller.FloodedCellCount;
        }

        private static WaterZone CloneWithId(WaterZone zone, string id)
        {
            var flooded = new bool[zone.Width, zone.Depth];
            var heights = new float[zone.Width, zone.Depth];
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    flooded[x, z] = zone.IsFlooded(x, z);
                    heights[x, z] = zone.HeightAt(x, z);
                }
            }

            return new WaterZone(id, zone.WaterLevelY, zone.OriginX, zone.OriginZ, zone.CellSize, flooded, heights);
        }

        /// <summary>True if any flooded cell of A is within gapCells of a flooded cell of B.</summary>
        public static bool FloodsTouch(WaterZone a, WaterZone b, int gapCells)
        {
            if (a.Width != b.Width || a.Depth != b.Depth ||
                !Mathf.Approximately(a.OriginX, b.OriginX) ||
                !Mathf.Approximately(a.OriginZ, b.OriginZ) ||
                !Mathf.Approximately(a.CellSize, b.CellSize))
            {
                // Different grids — fall back to inflated AABB (should not happen in V0).
                var aa = Inflate(a.GetFloodAabbXZ(), gapCells * a.CellSize);
                return aa.Overlaps(b.GetFloodAabbXZ());
            }

            var aabbA = Inflate(a.GetFloodAabbXZ(), gapCells * a.CellSize);
            if (!aabbA.Overlaps(b.GetFloodAabbXZ()))
            {
                return false;
            }

            for (var x = 0; x < a.Width; x++)
            {
                for (var z = 0; z < a.Depth; z++)
                {
                    if (!a.IsFlooded(x, z))
                    {
                        continue;
                    }

                    for (var dx = -gapCells; dx <= gapCells; dx++)
                    {
                        for (var dz = -gapCells; dz <= gapCells; dz++)
                        {
                            if (dx * dx + dz * dz > gapCells * gapCells)
                            {
                                continue;
                            }

                            if (b.IsFlooded(x + dx, z + dz))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private static int MergedFloodCount(WaterZone a, WaterZone b)
        {
            if (a.Width != b.Width || a.Depth != b.Depth)
            {
                return a.FloodedCellCount + b.FloodedCellCount;
            }

            var count = 0;
            for (var x = 0; x < a.Width; x++)
            {
                for (var z = 0; z < a.Depth; z++)
                {
                    if (a.IsFlooded(x, z) || b.IsFlooded(x, z))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static WaterZone MergeGroup(WaterZone[] zones, List<int> members, string id)
        {
            var first = zones[members[0]];
            var width = first.Width;
            var depth = first.Depth;
            var flooded = new bool[width, depth];
            var heights = new float[width, depth];
            var level = float.NegativeInfinity;

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    heights[x, z] = first.HeightAt(x, z);
                }
            }

            foreach (var mi in members)
            {
                var zone = zones[mi];
                if (zone.WaterLevelY > level)
                {
                    level = zone.WaterLevelY;
                }

                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        if (zone.IsFlooded(x, z))
                        {
                            flooded[x, z] = true;
                        }
                    }
                }
            }

            FillEnclosedHoles(flooded, width, depth);
            Dilate(flooded, width, depth, cells: 1);
            FillEnclosedHoles(flooded, width, depth);
            SmoothFloodMask(flooded, width, depth, blurPasses: 4, threshold: 0.40f);
            return new WaterZone(id, level, first.OriginX, first.OriginZ, first.CellSize, flooded, heights);
        }

        private static WaterZone FinalizeZone(WaterZone zone, string id)
        {
            var flooded = new bool[zone.Width, zone.Depth];
            var heights = new float[zone.Width, zone.Depth];
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    flooded[x, z] = zone.IsFlooded(x, z);
                    heights[x, z] = zone.HeightAt(x, z);
                }
            }

            FillEnclosedHoles(flooded, zone.Width, zone.Depth);
            Dilate(flooded, zone.Width, zone.Depth, cells: 1);
            FillEnclosedHoles(flooded, zone.Width, zone.Depth);
            SmoothFloodMask(flooded, zone.Width, zone.Depth, blurPasses: 4, threshold: 0.40f);
            return new WaterZone(id, zone.WaterLevelY, zone.OriginX, zone.OriginZ, zone.CellSize, flooded, heights);
        }

        /// <summary>
        /// Box-blur the occupancy field and re-threshold — rounds stair-step flood corners.
        /// </summary>
        public static void SmoothFloodMask(bool[,] flooded, int width, int depth, int blurPasses, float threshold)
        {
            var field = new float[width, depth];
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    field[x, z] = flooded[x, z] ? 1f : 0f;
                }
            }

            for (var pass = 0; pass < blurPasses; pass++)
            {
                var next = new float[width, depth];
                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        float sum = 0f;
                        var n = 0;
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            for (var dz = -1; dz <= 1; dz++)
                            {
                                var nx = x + dx;
                                var nz = z + dz;
                                if (nx < 0 || nz < 0 || nx >= width || nz >= depth)
                                {
                                    continue;
                                }

                                sum += field[nx, nz];
                                n++;
                            }
                        }

                        next[x, z] = n > 0 ? sum / n : field[x, z];
                    }
                }

                field = next;
            }

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    flooded[x, z] = field[x, z] >= threshold;
                }
            }
        }

        public static void FillEnclosedHoles(bool[,] flooded, int width, int depth)
        {
            var reachableDry = new bool[width, depth];
            var queue = new Queue<(int X, int Z)>();

            void TryEnqueue(int x, int z)
            {
                if (x < 0 || z < 0 || x >= width || z >= depth)
                {
                    return;
                }

                if (flooded[x, z] || reachableDry[x, z])
                {
                    return;
                }

                reachableDry[x, z] = true;
                queue.Enqueue((x, z));
            }

            for (var x = 0; x < width; x++)
            {
                TryEnqueue(x, 0);
                TryEnqueue(x, depth - 1);
            }

            for (var z = 0; z < depth; z++)
            {
                TryEnqueue(0, z);
                TryEnqueue(width - 1, z);
            }

            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                TryEnqueue(x - 1, z);
                TryEnqueue(x + 1, z);
                TryEnqueue(x, z - 1);
                TryEnqueue(x, z + 1);
            }

            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    if (!flooded[x, z] && !reachableDry[x, z])
                    {
                        flooded[x, z] = true;
                    }
                }
            }
        }

        private static void Dilate(bool[,] flooded, int width, int depth, int cells)
        {
            for (var step = 0; step < cells; step++)
            {
                var next = (bool[,])flooded.Clone();
                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        if (flooded[x, z])
                        {
                            continue;
                        }

                        if ((x > 0 && flooded[x - 1, z]) ||
                            (x + 1 < width && flooded[x + 1, z]) ||
                            (z > 0 && flooded[x, z - 1]) ||
                            (z + 1 < depth && flooded[x, z + 1]))
                        {
                            next[x, z] = true;
                        }
                    }
                }

                for (var x = 0; x < width; x++)
                {
                    for (var z = 0; z < depth; z++)
                    {
                        flooded[x, z] = next[x, z];
                    }
                }
            }
        }

        private static Rect Inflate(Rect r, float pad)
        {
            return Rect.MinMaxRect(r.xMin - pad, r.yMin - pad, r.xMax + pad, r.yMax + pad);
        }
    }
}
