using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Water
{
    /// <summary>
    /// Plants a few large, irregular lake basins on a jittered strata grid. Shapes are
    /// anisotropic + angular noise (not perfect circles). MapMagic basin pits are intentionally
    /// not used for the final lakes — <see cref="LakeTerrainCarver"/> digs under these masks.
    /// </summary>
    public static class DistributedLakePlacer
    {
        public const int DefaultMinLakes = 2;
        public const int DefaultTargetLakes = 3;
        public const float DefaultMinSeparationMeters = 380f;

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

            minLakes = Mathf.Clamp(minLakes, 1, 6);
            targetLakes = Mathf.Clamp(targetLakes, minLakes, 6);

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

            // Keep large lakes off the tile rim so soft banks / water meshes don't spill past terrain.
            var edgeMarginCells = Mathf.Max(20, Mathf.RoundToInt(140f / cellSize));

            // 2×2 strata — enough pockets for 2–3 well-spaced large lakes.
            const int strata = 2;
            var candidates = new List<(int Cx, int Cz, float Score)>(strata * strata);
            for (var gx = 0; gx < strata; gx++)
            {
                for (var gz = 0; gz < strata; gz++)
                {
                    var x0 = Mathf.FloorToInt(width * (gx / (float)strata));
                    var x1 = Mathf.FloorToInt(width * ((gx + 1) / (float)strata)) - 1;
                    var z0 = Mathf.FloorToInt(depth * (gz / (float)strata));
                    var z1 = Mathf.FloorToInt(depth * ((gz + 1) / (float)strata)) - 1;
                    x0 = Mathf.Clamp(x0, 4, width - 5);
                    x1 = Mathf.Clamp(x1, x0 + 1, width - 5);
                    z0 = Mathf.Clamp(z0, 4, depth - 5);
                    z1 = Mathf.Clamp(z1, z0 + 1, depth - 5);

                    var jx = x0 + rng.Next(0, Mathf.Max(1, x1 - x0 + 1));
                    var jz = z0 + rng.Next(0, Mathf.Max(1, z1 - z0 + 1));
                    FindLocalLow(heights, width, depth, jx, jz, searchRadius: 12, out var cx, out var cz);
                    if (cx < edgeMarginCells || cz < edgeMarginCells ||
                        cx >= width - edgeMarginCells || cz >= depth - edgeMarginCells)
                    {
                        cx = Mathf.Clamp(cx, edgeMarginCells, width - edgeMarginCells - 1);
                        cz = Mathf.Clamp(cz, edgeMarginCells, depth - edgeMarginCells - 1);
                    }

                    var score = -heights[cx, cz] + (float)rng.NextDouble() * 2f;
                    candidates.Add((cx, cz, score));
                }
            }

            // Extra candidates from remaining low spots so we can still hit minLakes on awkward tiles.
            for (var n = 0; n < 4; n++)
            {
                var jx = edgeMarginCells + rng.Next(0, Mathf.Max(1, width - 2 * edgeMarginCells));
                var jz = edgeMarginCells + rng.Next(0, Mathf.Max(1, depth - 2 * edgeMarginCells));
                FindLocalLow(heights, width, depth, jx, jz, searchRadius: 10, out var cx, out var cz);
                cx = Mathf.Clamp(cx, edgeMarginCells, width - edgeMarginCells - 1);
                cz = Mathf.Clamp(cz, edgeMarginCells, depth - edgeMarginCells - 1);
                candidates.Add((cx, cz, -heights[cx, cz] + (float)rng.NextDouble()));
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            var count = Mathf.Clamp(minLakes + rng.Next(0, targetLakes - minLakes + 1), minLakes, targetLakes);
            count = Mathf.Min(count, candidates.Count);

            var zones = new List<WaterZone>(count);
            var minSepCells = Mathf.Max(40, Mathf.RoundToInt(DefaultMinSeparationMeters / cellSize));
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
                    var distMeters = Mathf.Sqrt(dx * dx + dz * dz) * cellSize;
                    var edgeNeed = existing.EquivalentRadiusMeters + 140f + 120f;
                    if (dx * dx + dz * dz < minSepSq || distMeters < edgeNeed)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                // All lakes are large; slight size variety only.
                var radiusMeters = Mathf.Lerp(130f, 105f, zones.Count / Mathf.Max(1f, count - 1f));
                radiusMeters += (float)(rng.NextDouble() * 20f - 8f);
                radiusMeters = Mathf.Clamp(radiusMeters, 95f, 155f);

                zones.Add(MakeOrganicZone(
                    $"lake_{zones.Count}",
                    heights,
                    minX,
                    minZ,
                    cellSize,
                    width,
                    depth,
                    candidate.Cx,
                    candidate.Cz,
                    radiusMeters,
                    rng));
            }

            if (zones.Count < minLakes)
            {
                var relaxSepSq = Mathf.RoundToInt(280f / cellSize);
                relaxSepSq *= relaxSepSq;
                foreach (var candidate in candidates)
                {
                    if (zones.Count >= minLakes)
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
                        if (dx * dx + dz * dz < relaxSepSq)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (tooClose)
                    {
                        continue;
                    }

                    zones.Add(MakeOrganicZone(
                        $"lake_{zones.Count}",
                        heights,
                        minX,
                        minZ,
                        cellSize,
                        width,
                        depth,
                        candidate.Cx,
                        candidate.Cz,
                        110f,
                        rng));
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

        /// <summary>
        /// Irregular shoreline: rotated ellipse + multi-harmonic radius wobble + optional bay lobe.
        /// </summary>
        private static WaterZone MakeOrganicZone(
            string id,
            float[,] heights,
            float originX,
            float originZ,
            float cellSize,
            int width,
            int depth,
            int centerX,
            int centerZ,
            float radiusMeters,
            Random rng)
        {
            var radiusCells = Mathf.Max(8, Mathf.RoundToInt(radiusMeters / cellSize));
            var flooded = new bool[width, depth];
            var rimSum = 0f;
            var rimCount = 0;

            var aspect = 0.62f + (float)rng.NextDouble() * 0.32f; // elongated
            var rot = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var cos = Mathf.Cos(rot);
            var sin = Mathf.Sin(rot);
            var phase1 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var phase2 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var phase3 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var amp1 = 0.18f + (float)rng.NextDouble() * 0.12f;
            var amp2 = 0.10f + (float)rng.NextDouble() * 0.10f;
            var amp3 = 0.06f + (float)rng.NextDouble() * 0.06f;
            var noiseSeed = rng.Next();

            // Optional bay / second lobe offset for a less blob-like silhouette.
            var bay = rng.NextDouble() < 0.7;
            var bayAngle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var bayDist = radiusCells * (0.35f + (float)rng.NextDouble() * 0.25f);
            var bayCx = centerX + Mathf.Cos(bayAngle) * bayDist;
            var bayCz = centerZ + Mathf.Sin(bayAngle) * bayDist;
            var bayR = radiusCells * (0.35f + (float)rng.NextDouble() * 0.2f);

            var pad = Mathf.CeilToInt(radiusCells * 1.55f);
            var xMin = Mathf.Max(0, centerX - pad);
            var xMax = Mathf.Min(width - 1, centerX + pad);
            var zMin = Mathf.Max(0, centerZ - pad);
            var zMax = Mathf.Min(depth - 1, centerZ + pad);

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var dx = x - centerX;
                    var dz = z - centerZ;
                    var rx = dx * cos + dz * sin;
                    var rz = (-dx * sin + dz * cos) / aspect;
                    var dist = Mathf.Sqrt(rx * rx + rz * rz);
                    var ang = Mathf.Atan2(rz, rx);
                    var wobble =
                        1f
                        + amp1 * Mathf.Sin(2f * ang + phase1)
                        + amp2 * Mathf.Sin(5f * ang + phase2)
                        + amp3 * Mathf.Sin(9f * ang + phase3)
                        + 0.08f * HashNoise(x, z, noiseSeed);
                    var r = radiusCells * Mathf.Clamp(wobble, 0.55f, 1.45f);

                    var inside = dist <= r;
                    if (!inside && bay)
                    {
                        var bdx = x - bayCx;
                        var bdz = z - bayCz;
                        inside = bdx * bdx + bdz * bdz <= bayR * bayR;
                    }

                    if (!inside)
                    {
                        continue;
                    }

                    flooded[x, z] = true;
                    if (dist > r * 0.72f)
                    {
                        rimSum += heights[x, z];
                        rimCount++;
                    }
                }
            }

            LakeZoneMerger.FillEnclosedHoles(flooded, width, depth);
            LakeZoneMerger.SmoothFloodMask(flooded, width, depth, blurPasses: 3, threshold: 0.42f);

            var waterY = rimCount > 0
                ? rimSum / rimCount
                : heights[centerX, centerZ] + 1.5f;

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

        private static float HashNoise(int x, int z, int seed)
        {
            unchecked
            {
                var h = seed;
                h = (h * 374761393) + x * 668265263;
                h = (h * 374761393) + z * 1274126177;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return ((h & 0xFFFF) / 65535f) * 2f - 1f;
            }
        }
    }
}
