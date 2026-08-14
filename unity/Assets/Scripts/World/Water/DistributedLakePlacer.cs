using System;
using System.Collections.Generic;
using GameDev.World.Seed;
using GameDev.World.Terrain;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Water
{
    /// <summary>
    /// Plants elongated, terrain-aware lakes on a jittered strata grid. Shapes are
    /// BaseEllipse × TerrainInfluence × SeededOrganicVariation. Placement uses
    /// <see cref="LakePlacementScore"/> plus full-silhouette Mountain occupancy —
    /// not naive height ranking. Min count is a soft target: fewer lakes beat a sheet through a ridge.
    /// </summary>
    public static class DistributedLakePlacer
    {
        public const int DefaultMinLakes = WorldScale.LakeMinCount;
        public const int DefaultTargetLakes = WorldScale.LakeMinCount;
        public const float DefaultMinSeparationMeters = 380f;
        public const float AspectRatioMin = 1.4f;
        public const float AspectRatioMax = 2.2f;
        public const float MountainOccupancySampleStepMeters = 8f;

        private enum LakeSizeTier
        {
            Large = 0,
            Medium = 1,
            Small = 2,
        }

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
            return Place(
                worldHeightSampler, minX, minZ, sizeX, sizeZ, cellSize,
                placeRng: rng,
                worldSeed: null,
                settings: WorldGenSettings.Default,
                minLakes, targetLakes,
                biomeAtWorld: null);
        }

        public static WaterZone[] Place(
            Func<float, float, float> worldHeightSampler,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            float cellSize,
            IWorldSeed worldSeed,
            WorldGenSettings settings)
        {
            return Place(
                worldHeightSampler, minX, minZ, sizeX, sizeZ, cellSize,
                worldSeed, settings, biomeAtWorld: null);
        }

        public static WaterZone[] Place(
            Func<float, float, float> worldHeightSampler,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            float cellSize,
            IWorldSeed worldSeed,
            WorldGenSettings settings,
            Func<float, float, BiomeId> biomeAtWorld)
        {
            settings.Clamp();
            var placeRng = worldSeed.Fork("lake.place");
            // Soft target: try for LakeCount, never force onto mountains.
            var target = Mathf.Max(WorldScale.LakeMinCount, settings.LakeCount);
            var minLakes = target;
            return Place(
                worldHeightSampler, minX, minZ, sizeX, sizeZ, cellSize,
                placeRng, worldSeed, settings, minLakes, target, biomeAtWorld);
        }

        private static WaterZone[] Place(
            Func<float, float, float> worldHeightSampler,
            float minX,
            float minZ,
            float sizeX,
            float sizeZ,
            float cellSize,
            Random placeRng,
            IWorldSeed worldSeed,
            WorldGenSettings settings,
            int minLakes,
            int targetLakes,
            Func<float, float, BiomeId> biomeAtWorld = null)
        {
            if (worldHeightSampler == null || placeRng == null ||
                sizeX < cellSize * 4f || sizeZ < cellSize * 4f)
            {
                return Array.Empty<WaterZone>();
            }

            settings.Clamp();
            minLakes = Mathf.Clamp(minLakes, 1, 6);
            targetLakes = Mathf.Clamp(targetLakes, minLakes, 6);

            var width = Mathf.Max(2, Mathf.RoundToInt(sizeX / cellSize));
            var depth = Mathf.Max(2, Mathf.RoundToInt(sizeZ / cellSize));
            var heights = new float[width, depth];
            var heightMin = float.PositiveInfinity;
            var heightMax = float.NegativeInfinity;
            for (var x = 0; x < width; x++)
            {
                for (var z = 0; z < depth; z++)
                {
                    var h = worldHeightSampler(
                        minX + (x + 0.5f) * cellSize,
                        minZ + (z + 0.5f) * cellSize);
                    heights[x, z] = h;
                    if (h < heightMin)
                    {
                        heightMin = h;
                    }

                    if (h > heightMax)
                    {
                        heightMax = h;
                    }
                }
            }

            var edgeMarginCells = Mathf.Max(
                20, Mathf.RoundToInt(WorldScale.LakeEdgeMinDistanceMeters / cellSize));
            const int strata = 3;
            var candidates = new List<(int Cx, int Cz, float Score)>(strata * strata + 16);

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

                    var jx = x0 + placeRng.Next(0, Mathf.Max(1, x1 - x0 + 1));
                    var jz = z0 + placeRng.Next(0, Mathf.Max(1, z1 - z0 + 1));
                    FindLocalLow(heights, width, depth, jx, jz, searchRadius: 12, out var cx, out var cz);
                    cx = Mathf.Clamp(cx, edgeMarginCells, width - edgeMarginCells - 1);
                    cz = Mathf.Clamp(cz, edgeMarginCells, depth - edgeMarginCells - 1);

                    var score = LakePlacementScore(heights, width, depth, cx, cz, cellSize);
                    if (float.IsNegativeInfinity(score))
                    {
                        continue;
                    }

                    score += (float)placeRng.NextDouble() * 0.05f;
                    candidates.Add((cx, cz, score));
                }
            }

            for (var n = 0; n < 12; n++)
            {
                var jx = edgeMarginCells + placeRng.Next(0, Mathf.Max(1, width - 2 * edgeMarginCells));
                var jz = edgeMarginCells + placeRng.Next(0, Mathf.Max(1, depth - 2 * edgeMarginCells));
                FindLocalLow(heights, width, depth, jx, jz, searchRadius: 10, out var cx, out var cz);
                cx = Mathf.Clamp(cx, edgeMarginCells, width - edgeMarginCells - 1);
                cz = Mathf.Clamp(cz, edgeMarginCells, depth - edgeMarginCells - 1);
                var score = LakePlacementScore(heights, width, depth, cx, cz, cellSize);
                if (float.IsNegativeInfinity(score))
                {
                    continue;
                }

                score += (float)placeRng.NextDouble() * 0.05f;
                candidates.Add((cx, cz, score));
            }

            candidates.Sort((a, b) =>
            {
                var c = b.Score.CompareTo(a.Score);
                if (c != 0)
                {
                    return c;
                }

                c = a.Cx.CompareTo(b.Cx);
                return c != 0 ? c : a.Cz.CompareTo(b.Cz);
            });

            var count = Mathf.Min(targetLakes, Mathf.Max(1, candidates.Count));
            var zones = new List<WaterZone>(count);
            var minSepCells = Mathf.Max(40, Mathf.RoundToInt(DefaultMinSeparationMeters / cellSize));
            var minSepSq = minSepCells * minSepCells;
            var mapArea = Mathf.Max(sizeX * sizeZ, 1f);

            TryPlaceLakes(
                zones, candidates, heights, minX, minZ, cellSize, width, depth,
                worldSeed, placeRng, settings, mapArea, minSepSq, targetLakes,
                heightMin, heightMax, biomeAtWorld);

            if (zones.Count < minLakes)
            {
                // Soft candidates: loosen slope/edge but Mountain occupancy still hard-rejects.
                var soft = GatherSoftCandidates(
                    heights, width, depth, cellSize, edgeMarginCells, placeRng);
                soft.Sort((a, b) =>
                {
                    var c = b.Score.CompareTo(a.Score);
                    if (c != 0)
                    {
                        return c;
                    }

                    c = a.Cx.CompareTo(b.Cx);
                    return c != 0 ? c : a.Cz.CompareTo(b.Cz);
                });

                var relaxSepSq = Mathf.RoundToInt(200f / cellSize);
                relaxSepSq *= relaxSepSq;
                TryPlaceLakes(
                    zones, soft, heights, minX, minZ, cellSize, width, depth,
                    worldSeed, placeRng, settings, mapArea, relaxSepSq, minLakes,
                    heightMin, heightMax, biomeAtWorld);
            }

            return zones.ToArray();
        }

        private static List<(int Cx, int Cz, float Score)> GatherSoftCandidates(
            float[,] heights,
            int width,
            int depth,
            float cellSize,
            int edgeMarginCells,
            Random placeRng)
        {
            var list = new List<(int Cx, int Cz, float Score)>(24);
            var softEdge = Mathf.Max(8, edgeMarginCells / 2);
            for (var n = 0; n < 24; n++)
            {
                var jx = softEdge + placeRng.Next(0, Mathf.Max(1, width - 2 * softEdge));
                var jz = softEdge + placeRng.Next(0, Mathf.Max(1, depth - 2 * softEdge));
                FindLocalLow(heights, width, depth, jx, jz, searchRadius: 14, out var cx, out var cz);
                cx = Mathf.Clamp(cx, softEdge, width - softEdge - 1);
                cz = Mathf.Clamp(cz, softEdge, depth - softEdge - 1);
                var score = LakePlacementScore(heights, width, depth, cx, cz, cellSize, radiusCells: 10);
                if (float.IsNegativeInfinity(score))
                {
                    // Soft: still take local lows with a low placeholder score if not a sheer cliff.
                    var h = SampleBlurred(heights, width, depth, cx, cz);
                    var hx = SampleBlurred(heights, width, depth, cx + 1, cz) -
                             SampleBlurred(heights, width, depth, cx - 1, cz);
                    var hz = SampleBlurred(heights, width, depth, cx, cz + 1) -
                             SampleBlurred(heights, width, depth, cx, cz - 1);
                    var slope = Mathf.Sqrt(hx * hx + hz * hz) / (2f * Mathf.Max(cellSize, 0.01f));
                    if (slope > WorldScale.LakeRejectMaxSlope * 1.35f)
                    {
                        continue;
                    }

                    score = -50f - slope * 10f - h * 0.001f;
                }

                score += (float)placeRng.NextDouble() * 0.05f;
                list.Add((cx, cz, score));
            }

            return list;
        }

        private static void TryPlaceLakes(
            List<WaterZone> zones,
            List<(int Cx, int Cz, float Score)> candidates,
            float[,] heights,
            float minX,
            float minZ,
            float cellSize,
            int width,
            int depth,
            IWorldSeed worldSeed,
            Random placeRng,
            WorldGenSettings settings,
            float mapArea,
            int minSepSq,
            int desiredCount,
            float heightMin,
            float heightMax,
            Func<float, float, BiomeId> biomeAtWorld)
        {
            foreach (var candidate in candidates)
            {
                if (zones.Count >= desiredCount)
                {
                    return;
                }

                if (IsTooClose(
                        zones, candidate.Cx, candidate.Cz, minX, minZ, cellSize, minSepSq,
                        desiredCount <= WorldScale.LakeMinCount ? 90f : 140f))
                {
                    continue;
                }

                var lakeIndex = zones.Count;
                var lakeRng = worldSeed != null
                    ? worldSeed.Fork("lake", lakeIndex)
                    : new Random(unchecked(placeRng.Next() * 397 ^ lakeIndex));

                var tier = SizeTierForIndex(lakeIndex, desiredCount);
                var radiusMeters = RadiusForSizeTier(mapArea, tier, lakeRng, settings.LakeSize);

                var zone = MakeOrganicZone(
                    $"lake_{lakeIndex}",
                    heights,
                    minX,
                    minZ,
                    cellSize,
                    width,
                    depth,
                    candidate.Cx,
                    candidate.Cz,
                    radiusMeters,
                    lakeRng,
                    settings.ShapeIrregularity,
                    settings.TerrainConformity,
                    settings.WaterDepthOffsetMeters);

                if (FloodTouchesMountain(zone, heights, width, depth, heightMin, heightMax, biomeAtWorld))
                {
                    continue;
                }

                zones.Add(zone);
            }
        }

        /// <summary>
        /// Hard Mountain occupancy: any flooded / silhouette sample on BiomeId.Mountain
        /// (or steep highland) rejects the lake. Prefer fewer lakes over a sheet through a ridge.
        /// </summary>
        public static bool FloodTouchesMountain(
            WaterZone zone,
            float[,] heights,
            int width,
            int depth,
            float heightMin,
            float heightMax,
            Func<float, float, BiomeId> biomeAtWorld = null)
        {
            if (zone == null)
            {
                return false;
            }

            var range = Mathf.Max(heightMax - heightMin, 0.01f);
            var step = Mathf.Max(1, Mathf.RoundToInt(MountainOccupancySampleStepMeters / Mathf.Max(zone.CellSize, 0.01f)));

            if (zone.Silhouette != null)
            {
                var aabb = zone.Silhouette.GetAabbXZ(padMeters: zone.CellSize);
                for (var wz = aabb.yMin; wz <= aabb.yMax; wz += MountainOccupancySampleStepMeters)
                {
                    for (var wx = aabb.xMin; wx <= aabb.xMax; wx += MountainOccupancySampleStepMeters)
                    {
                        if (zone.Silhouette.SignedDistance(wx, wz) < 0f)
                        {
                            continue;
                        }

                        if (IsMountainAt(wx, wz, zone, heights, width, depth, heightMin, range, biomeAtWorld))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            for (var z = 0; z < depth; z += step)
            {
                for (var x = 0; x < width; x += step)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
                    var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
                    if (IsMountainAt(wx, wz, zone, heights, width, depth, heightMin, range, biomeAtWorld))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsMountainAt(
            float wx,
            float wz,
            WaterZone zone,
            float[,] heights,
            int width,
            int depth,
            float heightMin,
            float range,
            Func<float, float, BiomeId> biomeAtWorld)
        {
            if (biomeAtWorld != null)
            {
                var biome = biomeAtWorld(wx, wz);
                return biome == BiomeId.Mountain;
            }

            var cx = Mathf.Clamp(
                Mathf.FloorToInt((wx - zone.OriginX) / Mathf.Max(zone.CellSize, 0.01f)),
                0, width - 1);
            var cz = Mathf.Clamp(
                Mathf.FloorToInt((wz - zone.OriginZ) / Mathf.Max(zone.CellSize, 0.01f)),
                0, depth - 1);
            var h01 = (heights[cx, cz] - heightMin) / range;
            var hx = SampleBlurred(heights, width, depth, Mathf.Min(cx + 1, width - 1), cz) -
                     SampleBlurred(heights, width, depth, Mathf.Max(cx - 1, 0), cz);
            var hz = SampleBlurred(heights, width, depth, cx, Mathf.Min(cz + 1, depth - 1)) -
                     SampleBlurred(heights, width, depth, cx, Mathf.Max(cz - 1, 0));
            var slope = Mathf.Sqrt(hx * hx + hz * hz) / (2f * Mathf.Max(zone.CellSize, 0.01f));
            var slope01 = Mathf.Clamp01(slope / 0.6f);
            return BiomeClassifier.Classify(h01, slope01, 0.5f) == BiomeId.Mountain;
        }

        /// <summary>Majority biome under the flood for swamp tint etc.</summary>
        public static BiomeId MajorityBiome(
            WaterZone zone,
            Func<float, float, BiomeId> biomeAtWorld)
        {
            if (zone == null || biomeAtWorld == null)
            {
                return BiomeId.Meadow;
            }

            var counts = new Dictionary<BiomeId, int>();
            var step = Mathf.Max(zone.CellSize * 2f, MountainOccupancySampleStepMeters);
            var aabb = zone.GetFloodAabbXZ();
            for (var wz = aabb.yMin; wz <= aabb.yMax; wz += step)
            {
                for (var wx = aabb.xMin; wx <= aabb.xMax; wx += step)
                {
                    if (!zone.Contains(wx, wz))
                    {
                        continue;
                    }

                    var b = biomeAtWorld(wx, wz);
                    counts.TryGetValue(b, out var n);
                    counts[b] = n + 1;
                }
            }

            var best = BiomeId.Meadow;
            var bestN = -1;
            foreach (var kv in counts)
            {
                if (kv.Value > bestN)
                {
                    bestN = kv.Value;
                    best = kv.Key;
                }
            }

            return best;
        }

        private static LakeSizeTier SizeTierForIndex(int lakeIndex, int total)
        {
            if (total >= 3 && lakeIndex < 3)
            {
                return (LakeSizeTier)lakeIndex;
            }

            return (LakeSizeTier)(lakeIndex % 3);
        }

        private static float RadiusForSizeTier(
            float mapArea,
            LakeSizeTier tier,
            Random lakeRng,
            float sizeBias01)
        {
            float fracMin;
            float fracMax;
            switch (tier)
            {
                case LakeSizeTier.Large:
                    fracMin = WorldScale.LakeLargeAreaFracMin;
                    fracMax = WorldScale.LakeLargeAreaFracMax;
                    break;
                case LakeSizeTier.Medium:
                    fracMin = WorldScale.LakeMediumAreaFracMin;
                    fracMax = WorldScale.LakeMediumAreaFracMax;
                    break;
                default:
                    fracMin = WorldScale.LakeSmallAreaFracMin;
                    fracMax = WorldScale.LakeSmallAreaFracMax;
                    break;
            }

            var t = Mathf.Clamp01(0.35f + sizeBias01 * 0.5f + (float)(lakeRng.NextDouble() * 0.3 - 0.15));
            var frac = Mathf.Lerp(fracMin, fracMax, t);
            var area = mapArea * frac;
            var radius = Mathf.Sqrt(area / Mathf.PI);
            return Mathf.Clamp(radius, WorldScale.LakeRadiusMinMeters, WorldScale.LakeRadiusMaxMeters);
        }

        /// <summary>
        /// Prefer natural basins / gentle slopes; penalize peaks, ridges, and expensive digs.
        /// Returns <see cref="float.NegativeInfinity"/> when the site would destroy mountain /
        /// high-slope terrain (reject candidate — do not excavate the mountain).
        /// </summary>
        public static float LakePlacementScore(
            float[,] heights,
            int width,
            int depth,
            int cx,
            int cz,
            float cellSize,
            int radiusCells = 12)
        {
            // Edge reject — lakes near tile border look clipped and expose water sheets.
            // Scale down on tiny test grids so unit fixtures still exercise basin vs peak.
            var mapSpan = Mathf.Min(width, depth) * Mathf.Max(cellSize, 0.01f);
            var edgeNeed = Mathf.Min(WorldScale.LakeEdgeMinDistanceMeters, mapSpan * 0.18f);
            var edgeCells = Mathf.Max(1, Mathf.RoundToInt(edgeNeed / Mathf.Max(cellSize, 0.01f)));
            if (cx < edgeCells || cz < edgeCells ||
                cx >= width - edgeCells || cz >= depth - edgeCells)
            {
                return float.NegativeInfinity;
            }

            var centerH = SampleBlurred(heights, width, depth, cx, cz);
            var slopeSum = 0f;
            var slopeN = 0;
            var maxSlope = 0f;
            var basinSum = 0f;
            var basinN = 0;
            var varSum = 0f;
            var varN = 0;
            var highPenalty = 0f;
            var cliffPenalty = 0f;
            var minH = float.PositiveInfinity;
            var maxH = float.NegativeInfinity;
            var mountainCells = 0;
            var sampleCells = 0;

            for (var dz = -radiusCells; dz <= radiusCells; dz++)
            {
                for (var dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    var dist = Mathf.Sqrt(dx * dx + dz * dz);
                    if (dist > radiusCells)
                    {
                        continue;
                    }

                    var x = cx + dx;
                    var z = cz + dz;
                    if (x < 1 || z < 1 || x >= width - 1 || z >= depth - 1)
                    {
                        continue;
                    }

                    var h = heights[x, z];
                    if (h < minH)
                    {
                        minH = h;
                    }

                    if (h > maxH)
                    {
                        maxH = h;
                    }

                    sampleCells++;
                    var hx = SampleBlurred(heights, width, depth, x + 1, z) -
                             SampleBlurred(heights, width, depth, x - 1, z);
                    var hz = SampleBlurred(heights, width, depth, x, z + 1) -
                             SampleBlurred(heights, width, depth, x, z - 1);
                    var slope = Mathf.Sqrt(hx * hx + hz * hz) / (2f * Mathf.Max(cellSize, 0.01f));
                    slopeSum += slope;
                    slopeN++;
                    if (slope > maxSlope)
                    {
                        maxSlope = slope;
                    }

                    if (h > centerH + 0.15f)
                    {
                        // Walls above the floor — true closed depression.
                        basinSum += h - centerH;
                        basinN++;
                    }

                    varSum += (h - centerH) * (h - centerH);
                    varN++;

                    if (h > centerH + 25f)
                    {
                        highPenalty += (h - centerH - 25f) * 0.02f;
                    }

                    // Steep + elevated relative to basin floor ≈ mountain / cliff flank.
                    if (slope > 0.35f && h > centerH + 12f)
                    {
                        mountainCells++;
                    }

                    // Near-vertical drop inside footprint.
                    if (slope > WorldScale.LakeRejectMaxSlope)
                    {
                        cliffPenalty += (slope - WorldScale.LakeRejectMaxSlope) * 4f;
                    }
                }
            }

            var avgSlope = slopeN > 0 ? slopeSum / slopeN : 1f;
            var heightRange = sampleCells > 0 ? maxH - minH : 0f;
            var mountainRatio = sampleCells > 0 ? mountainCells / (float)sampleCells : 1f;
            var variance = varN > 0 ? varSum / varN : 0f;

            if (avgSlope > WorldScale.LakeRejectAvgSlope ||
                maxSlope > WorldScale.LakeRejectMaxSlope ||
                highPenalty > WorldScale.LakeRejectHighPenalty ||
                mountainRatio > 0.15f ||
                cliffPenalty > 2.5f ||
                (heightRange > WorldScale.LakeRejectHeightRangeMeters && mountainRatio > 0.06f))
            {
                return float.NegativeInfinity;
            }

            // Center must sit near the local floor, not mid-slope.
            if (sampleCells > 0 && centerH > minH + Mathf.Max(1.5f, heightRange * 0.3f))
            {
                return float.NegativeInfinity;
            }

            var basinSuitability = basinN > 0 ? basinSum / basinN : 0f;
            // Need a real depression (average enclosing walls), not a flat terrace.
            if (basinSuitability < 0.45f)
            {
                return float.NegativeInfinity;
            }

            var excavationCost = Mathf.Sqrt(Mathf.Max(variance, 0f)) * 0.08f;
            var lowSlope = 1f - Mathf.Clamp01(avgSlope / 0.35f);
            var lowVariance = 1f - Mathf.Clamp01(variance / 80f);
            var conformity = Mathf.Clamp01(basinSuitability / 8f);
            var edgeDistCells = Mathf.Min(
                Mathf.Min(cx, cz),
                Mathf.Min(width - 1 - cx, depth - 1 - cz));
            var edgeBonus = Mathf.Clamp01(
                (edgeDistCells * cellSize - WorldScale.LakeEdgeMinDistanceMeters) / 200f);

            return basinSuitability * 1.5f
                   + lowSlope * 4f
                   + lowVariance * 2.5f
                   + conformity * 2f
                   + edgeBonus * 2f
                   - excavationCost
                   - highPenalty
                   - cliffPenalty
                   - avgSlope * 3f
                   - mountainRatio * 6f;
        }

        private static bool IsTooClose(
            List<WaterZone> zones,
            int cx,
            int cz,
            float minX,
            float minZ,
            float cellSize,
            int minSepSq,
            float edgePadMeters)
        {
            foreach (var existing in zones)
            {
                var ec = existing.GetFloodCentroidXZ();
                var ex = Mathf.FloorToInt((ec.x - minX) / cellSize);
                var ez = Mathf.FloorToInt((ec.z - minZ) / cellSize);
                var dx = cx - ex;
                var dz = cz - ez;
                var distMeters = Mathf.Sqrt(dx * dx + dz * dz) * cellSize;
                var edgeNeed = existing.EquivalentRadiusMeters + edgePadMeters + 80f;
                if (dx * dx + dz * dz < minSepSq || distMeters < edgeNeed)
                {
                    return true;
                }
            }

            return false;
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
        /// Elongated ellipse + LF terrain shore modulation + ShapeIrregularity organic wobble.
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
            Random lakeRng,
            float shapeIrregularity,
            float terrainConformity,
            float waterDepthOffsetMeters = -1f)
        {
            shapeIrregularity = Mathf.Clamp01(shapeIrregularity);
            terrainConformity = Mathf.Clamp01(terrainConformity);
            var depthOffset = Mathf.Max(
                waterDepthOffsetMeters > 0f
                    ? waterDepthOffsetMeters
                    : WorldScale.WaterDepthOffsetMeters,
                WorldScale.WaterInsetDepthMeters);

            var flooded = new bool[width, depth];
            var rimSum = 0f;
            var rimCount = 0;

            var centerWorldX = originX + (centerX + 0.5f) * cellSize;
            var centerWorldZ = originZ + (centerZ + 0.5f) * cellSize;

            EstimateBasinOrientation(
                heights, width, depth, centerX, centerZ, cellSize, radiusMeters,
                out var basinRot, out var basinAspectRatio);

            // Spec aspect ratio 1.4–2.2 (major/minor). Silhouette stores minor/major (= 1/ratio).
            var aspectRatio = Mathf.Lerp(AspectRatioMin, AspectRatioMax, (float)lakeRng.NextDouble());
            aspectRatio = Mathf.Lerp(aspectRatio, basinAspectRatio, terrainConformity * 0.55f);
            aspectRatio = Mathf.Clamp(aspectRatio, AspectRatioMin, AspectRatioMax);
            var silhouetteAspect = 1f / aspectRatio;

            var seedRot = (float)(lakeRng.NextDouble() * Mathf.PI);
            var rot = Mathf.LerpAngle(seedRot * Mathf.Rad2Deg, basinRot * Mathf.Rad2Deg,
                terrainConformity * 0.7f) * Mathf.Deg2Rad;

            var phase1 = (float)(lakeRng.NextDouble() * Mathf.PI * 2f);
            var phase2 = (float)(lakeRng.NextDouble() * Mathf.PI * 2f);
            var phase3 = (float)(lakeRng.NextDouble() * Mathf.PI * 2f);
            // ShapeIrregularity 0.35 → noticeable; 0 → ellipse; capped so shores stay smooth.
            var amp1 = (0.08f + (float)lakeRng.NextDouble() * 0.06f) * shapeIrregularity;
            var amp2 = (0.04f + (float)lakeRng.NextDouble() * 0.04f) * shapeIrregularity;
            var amp3 = (0.015f + (float)lakeRng.NextDouble() * 0.02f) * shapeIrregularity;
            var noiseSeed = lakeRng.Next();

            var terrainFactors = BuildTerrainFactors(
                heights, width, depth, originX, originZ, cellSize,
                centerWorldX, centerWorldZ, radiusMeters, rot, silhouetteAspect,
                terrainConformity);

            var silhouette = new LakeSilhouette(
                centerWorldX,
                centerWorldZ,
                radiusMeters,
                silhouetteAspect,
                rot,
                phase1,
                phase2,
                phase3,
                amp1,
                amp2,
                amp3,
                noiseSeed,
                hasBay: false,
                bayCenterX: 0f,
                bayCenterZ: 0f,
                bayRadiusMeters: 0f,
                terrainFactors);

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
                    if (signed < radiusMeters * 0.28f)
                    {
                        rimSum += heights[x, z];
                        rimCount++;
                    }
                }
            }

            LakeZoneMerger.FillEnclosedHoles(flooded, width, depth);

            // WaterLevel = AvgShoreHeight − WaterDepthOffset (hides sheet under banks).
            var waterY = rimCount > 0
                ? rimSum / rimCount - depthOffset
                : heights[centerX, centerZ] - depthOffset;

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

        private static void EstimateBasinOrientation(
            float[,] heights,
            int width,
            int depth,
            int centerX,
            int centerZ,
            float cellSize,
            float radiusMeters,
            out float rotationRadians,
            out float aspectRatio)
        {
            var steps = Mathf.Max(2, Mathf.RoundToInt(radiusMeters / cellSize));
            var centerH = SampleBlurred(heights, width, depth, centerX, centerZ);
            var sumXx = 0f;
            var sumZz = 0f;
            var sumXz = 0f;
            var sumW = 0f;

            for (var dz = -steps; dz <= steps; dz++)
            {
                for (var dx = -steps; dx <= steps; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    var dist = Mathf.Sqrt(dx * dx + dz * dz) * cellSize;
                    if (dist > radiusMeters)
                    {
                        continue;
                    }

                    var h = SampleBlurred(heights, width, depth, centerX + dx, centerZ + dz);
                    var w = Mathf.Max(0f, centerH - h + 0.5f);
                    var ox = dx * cellSize;
                    var oz = dz * cellSize;
                    sumXx += ox * ox * w;
                    sumZz += oz * oz * w;
                    sumXz += ox * oz * w;
                    sumW += w;
                }
            }

            if (sumW < 1e-3f)
            {
                rotationRadians = 0f;
                aspectRatio = 1.6f;
                return;
            }

            var trace = sumXx + sumZz;
            var det = sumXx * sumZz - sumXz * sumXz;
            var disc = Mathf.Max(0f, trace * trace * 0.25f - det);
            var lambda1 = trace * 0.5f + Mathf.Sqrt(disc);
            var lambda2 = trace * 0.5f - Mathf.Sqrt(disc);
            float vx;
            float vz;
            if (Mathf.Abs(sumXz) > 1e-4f)
            {
                vx = lambda2 - sumZz;
                vz = sumXz;
            }
            else if (sumXx <= sumZz)
            {
                vx = 1f;
                vz = 0f;
            }
            else
            {
                vx = 0f;
                vz = 1f;
            }

            var len = Mathf.Sqrt(vx * vx + vz * vz);
            rotationRadians = len > 1e-5f ? Mathf.Atan2(vz / len, vx / len) : 0f;
            var ratio = lambda1 > 1e-4f ? lambda1 / Mathf.Max(lambda2, 1e-4f) : 1.6f;
            aspectRatio = Mathf.Clamp(ratio, AspectRatioMin, AspectRatioMax);
        }

        private static float[] BuildTerrainFactors(
            float[,] heights,
            int width,
            int depth,
            float originX,
            float originZ,
            float cellSize,
            float centerWorldX,
            float centerWorldZ,
            float radiusMeters,
            float rotationRadians,
            float aspect,
            float terrainConformity)
        {
            var n = LakeSilhouette.TerrainFactorSampleCount;
            var raw = new float[n];
            var mean = 0f;
            for (var i = 0; i < n; i++)
            {
                var ang = i * Mathf.PI * 2f / n + rotationRadians;
                var r = radiusMeters * 1.05f;
                var wx = centerWorldX + Mathf.Cos(ang) * r;
                var wz = centerWorldZ + Mathf.Sin(ang) * r;
                var cx = Mathf.Clamp(Mathf.FloorToInt((wx - originX) / cellSize), 0, width - 1);
                var cz = Mathf.Clamp(Mathf.FloorToInt((wz - originZ) / cellSize), 0, depth - 1);
                raw[i] = SampleBlurred(heights, width, depth, cx, cz);
                mean += raw[i];
            }

            mean /= n;
            var factors = new float[n];
            var span = 0f;
            for (var i = 0; i < n; i++)
            {
                span = Mathf.Max(span, Mathf.Abs(raw[i] - mean));
            }

            span = Mathf.Max(span, 0.5f);
            // At conformity 0.75 → ~±22–30% shore pull; still clamped so shape stays elliptical.
            var pull = 0.10f + 0.28f * terrainConformity;
            for (var i = 0; i < n; i++)
            {
                var delta = (mean - raw[i]) / span;
                factors[i] = Mathf.Clamp(1f + delta * pull, 1f - pull, 1f + pull);
            }

            _ = aspect;
            _ = originZ;
            return factors;
        }

        private static float SampleBlurred(float[,] heights, int width, int depth, int x, int z)
        {
            var sum = 0f;
            var n = 0;
            for (var dz = -2; dz <= 2; dz++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var xx = Mathf.Clamp(x + dx, 0, width - 1);
                    var zz = Mathf.Clamp(z + dz, 0, depth - 1);
                    sum += heights[xx, zz];
                    n++;
                }
            }

            return sum / n;
        }
    }
}
