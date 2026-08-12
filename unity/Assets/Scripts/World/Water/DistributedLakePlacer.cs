using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Water
{
    /// <summary>
    /// Guarantees a scattered set of lakes by planting basins in a jittered grid (not "whatever
    /// noise minima happened to appear"). Each site gets its own circular flood mask so later
    /// merge/cull cannot glue the whole plains into one mega-lake.
    /// </summary>
    public static class DistributedLakePlacer
    {
        public const int DefaultMinLakes = 4;
        public const int DefaultTargetLakes = 7;

        public static WaterZone[] Place(
            Func<float, float, float> worldHeightSampler,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            float cellSize,
            Random rng,
            int minLakes = DefaultMinLakes,
            int targetLakes = DefaultTargetLakes)
        {
            if (worldHeightSampler == null || rng == null || sizeX < cellSize * 4f || sizeZ < cellSize * 4f)
            {
                return Array.Empty<WaterZone>();
            }

            minLakes = Mathf.Clamp(minLakes, 1, 12);
            targetLakes = Mathf.Clamp(targetLakes, minLakes, 12);

            var width = Mathf.Max(2, Mathf.RoundToInt(sizeX / cellSize));
            var depth = Mathf.Max(2, Mathf.RoundToInt(sizeZ / cellSize));
            var heights = new float[width, depth];
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    heights[x, z] = worldHeightSampler(
                        minX + (x + 0.5f) * cellSize,
                        minZ + (z + 0.5f) * cellSize);
                }
            }

            // 3×3 strata → up to 9 candidate pockets; we keep targetLakes of them.
            const int strata = 3;
            var candidates = new List<(int Cx, int Cz, float Score)>(strata * strata);
            for (var gx = 0; gx < strata; gx++)
            {
                for (var gz = 0; gz < strata; gz++)
                {
                    var x0 = Mathf.FloorToInt(width * (gx / (float)strata));
                    var x1 = Mathf.FloorToInt(width * ((gx + 1) / (float)strata)) - 1;
                    var z0 = Mathf.FloorToInt(depth * (gz / (float)strata));
                    var z1 = Mathf.FloorToInt(depth * ((gz + 1) / (float)strata)) - 1;
                    x0 = Mathf.Clamp(x0, 2, width - 3);
                    x1 = Mathf.Clamp(x1, x0 + 1, width - 3);
                    z0 = Mathf.Clamp(z0, 2, depth - 3);
                    z1 = Mathf.Clamp(z1, z0 + 1, depth - 3);

                    // Jittered pick inside the cell, then snap to the lowest nearby sample so the
                    // bowl sits in a natural dip when one exists.
                    var jx = x0 + rng.Next(0, Mathf.Max(1, x1 - x0 + 1));
                    var jz = z0 + rng.Next(0, Mathf.Max(1, z1 - z0 + 1));
                    FindLocalLow(heights, width, depth, jx, jz, searchRadius: 8, out var cx, out var cz);

                    // Prefer lower ground (plains pockets) but keep highland sites usable.
                    var score = -heights[cx, cz] + (float)rng.NextDouble() * 2f;
                    candidates.Add((cx, cz, score));
                }
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            var count = Mathf.Clamp(minLakes + rng.Next(0, targetLakes - minLakes + 1), minLakes, targetLakes);
            count = Mathf.Min(count, candidates.Count);

            var zones = new List<WaterZone>(count);
            var minSepCells = Mathf.Max(18, Mathf.RoundToInt(160f / cellSize));
            var minSepSq = minSepCells * minSepCells;

            foreach (var candidate in candidates)
            {
                if (zones.Count >= count)
                {
                    break;
                }

                var tooClose = false;
                foreach (var existing in zones)
                {
                    var ec = existing.GetFloodCentroidXZ();
                    var ex = Mathf.FloorToInt((ec.x - minX) / cellSize);
                    var ez = Mathf.FloorToInt((ec.z - minZ) / cellSize);
                    var dx = candidate.Cx - ex;
                    var dz = candidate.Cz - ez;
                    if (dx * dx + dz * dz < minSepSq)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                // First lakes larger; later ones become ponds.
                var t = zones.Count / Mathf.Max(1f, count - 1f);
                var radiusMeters = Mathf.Lerp(92f, 32f, t) + (float)(rng.NextDouble() * 12f - 6f);
                radiusMeters = Mathf.Clamp(radiusMeters, 28f, 100f);

                zones.Add(MakeCircularZone(
                    $"lake_{zones.Count}",
                    heights,
                    minX,
                    minZ,
                    cellSize,
                    width,
                    depth,
                    candidate.Cx,
                    candidate.Cz,
                    radiusMeters));
            }

            // Hard floor: if separation ate too many, relax and fill from remaining candidates.
            if (zones.Count < minLakes)
            {
                foreach (var candidate in candidates)
                {
                    if (zones.Count >= minLakes)
                    {
                        break;
                    }

                    var already = false;
                    foreach (var existing in zones)
                    {
                        var ec = existing.GetFloodCentroidXZ();
                        var ex = Mathf.FloorToInt((ec.x - minX) / cellSize);
                        var ez = Mathf.FloorToInt((ec.z - minZ) / cellSize);
                        if (ex == candidate.Cx && ez == candidate.Cz)
                        {
                            already = true;
                            break;
                        }
                    }

                    if (already)
                    {
                        continue;
                    }

                    zones.Add(MakeCircularZone(
                        $"lake_{zones.Count}",
                        heights,
                        minX,
                        minZ,
                        cellSize,
                        width,
                        depth,
                        candidate.Cx,
                        candidate.Cz,
                        48f));
                }
            }

            return zones.ToArray();
        }

        private static void FindLocalLow(
            float[,] heights,
            int width,
            int depth,
            int seedX,
            int seedZ,
            int searchRadius,
            out int bestX,
            out int bestZ)
        {
            bestX = seedX;
            bestZ = seedZ;
            var bestH = heights[seedX, seedZ];
            for (var dz = -searchRadius; dz <= searchRadius; dz++)
            {
                for (var dx = -searchRadius; dx <= searchRadius; dx++)
                {
                    var x = seedX + dx;
                    var z = seedZ + dz;
                    if (x < 1 || z < 1 || x >= width - 1 || z >= depth - 1)
                    {
                        continue;
                    }

                    var h = heights[x, z];
                    if (h < bestH)
                    {
                        bestH = h;
                        bestX = x;
                        bestZ = z;
                    }
                }
            }
        }

        private static WaterZone MakeCircularZone(
            string id,
            float[,] heights,
            float originX,
            float originZ,
            float cellSize,
            int width,
            int depth,
            int centerX,
            int centerZ,
            float radiusMeters)
        {
            var radiusCells = Mathf.Max(4, Mathf.RoundToInt(radiusMeters / cellSize));
            var flooded = new bool[width, depth];
            var rimSum = 0f;
            var rimCount = 0;
            var rInner = radiusCells * 0.72f;
            var rOuter = radiusCells;

            for (var z = 0; z < depth; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = x - centerX;
                    var dz = z - centerZ;
                    var dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist <= rInner)
                    {
                        flooded[x, z] = true;
                    }
                    else if (dist <= rOuter)
                    {
                        // Soft shore band — still flooded so carver digs a bowl, rim samples water Y.
                        flooded[x, z] = true;
                        rimSum += heights[x, z];
                        rimCount++;
                    }
                }
            }

            var waterY = rimCount > 0
                ? rimSum / rimCount
                : heights[centerX, centerZ] + 1.5f;

            // Copy heights so zones don't share a mutable buffer.
            var heightCopy = new float[width, depth];
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    heightCopy[x, z] = heights[x, z];
                }
            }

            return new WaterZone(id, waterY, originX, originZ, cellSize, flooded, heightCopy);
        }
    }
}
