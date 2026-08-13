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

                // All lakes are large; slight size variety only (WorldScale small-lake band).
                var radiusMeters = Mathf.Lerp(
                    GameDev.World.WorldScale.LakeRadiusMaxMeters - 25f,
                    GameDev.World.WorldScale.LakeRadiusMinMeters + 10f,
                    zones.Count / Mathf.Max(1f, count - 1f));
                radiusMeters += (float)(rng.NextDouble() * 20f - 8f);
                radiusMeters = Mathf.Clamp(
                    radiusMeters,
                    GameDev.World.WorldScale.LakeRadiusMinMeters,
                    GameDev.World.WorldScale.LakeRadiusMaxMeters);

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
            var flooded = new bool[width, depth];
            var rimSum = 0f;
            var rimCount = 0;

            var aspect = 0.62f + (float)rng.NextDouble() * 0.32f;
            var rot = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var phase1 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var phase2 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var phase3 = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var amp1 = 0.18f + (float)rng.NextDouble() * 0.12f;
            var amp2 = 0.10f + (float)rng.NextDouble() * 0.10f;
            var amp3 = 0.06f + (float)rng.NextDouble() * 0.06f;
            var noiseSeed = rng.Next();

            var centerWorldX = originX + (centerX + 0.5f) * cellSize;
            var centerWorldZ = originZ + (centerZ + 0.5f) * cellSize;

            // Optional bay / second lobe (world metres) — same formula as analytic silhouette.
            var hasBay = rng.NextDouble() < 0.7;
            var bayAngle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var bayDist = radiusMeters * (0.35f + (float)rng.NextDouble() * 0.25f);
            var bayWorldX = centerWorldX + Mathf.Cos(bayAngle) * bayDist;
            var bayWorldZ = centerWorldZ + Mathf.Sin(bayAngle) * bayDist;
            var bayRadius = radiusMeters * (0.35f + (float)rng.NextDouble() * 0.2f);

            var silhouette = new LakeSilhouette(
                centerWorldX,
                centerWorldZ,
                radiusMeters,
                aspect,
                rot,
                phase1,
                phase2,
                phase3,
                amp1,
                amp2,
                amp3,
                noiseSeed,
                hasBay,
                bayWorldX,
                bayWorldZ,
                hasBay ? bayRadius : 0f);

            // Occupancy mask for Contains/biomes — shore mesh/carve use Silhouette, not this raster.
            var aabb = silhouette.GetAabbXZ(padMeters: cellSize * 2f);
            var xMin = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - originX) / cellSize));
            var xMax = Mathf.Min(width - 1, Mathf.CeilToInt((aabb.xMax - originX) / cellSize));
            var zMin = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - originZ) / cellSize));
            var zMax = Mathf.Min(depth - 1, Mathf.CeilToInt((aabb.yMax - originZ) / cellSize));

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var wx = originX + (x + 0.5f) * cellSize;
                    var wz = originZ + (z + 0.5f) * cellSize;
                    var signed = silhouette.SignedDistance(wx, wz);
                    if (signed < 0f)
                    {
                        continue;
                    }

                    flooded[x, z] = true;
                    // Near-shore ring for provisional waterY before carver locks the rim.
                    if (signed < radiusMeters * 0.28f)
                    {
                        rimSum += heights[x, z];
                        rimCount++;
                    }
                }
            }

            LakeZoneMerger.FillEnclosedHoles(flooded, width, depth);

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

            return new WaterZone(id, waterY, originX, originZ, cellSize, flooded, heightCopy, silhouette);
        }
    }
}
