using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Amplifies 3–7 major peaks as local maxima on existing ridge crests.
    /// Does not stamp standalone cones onto a flat plateau.
    /// </summary>
    public static class IslandPeakPlacer
    {
        public struct Peak
        {
            public float WorldX;
            public float WorldZ;
            public float Amplitude01;
            public float RadiusMeters;
            public float AxisX;
            public float AxisZ;
        }

        public static List<Peak> Apply(UnityEngine.Terrain terrain, int worldSeed)
        {
            var result = new List<Peak>();
            if (terrain == null || terrain.terrainData == null)
            {
                return result;
            }

            TerrainHeightWrite.ForceBudgetSize(terrain);
            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var size = data.size;
            var heights = data.GetHeights(0, 0, res, res);

            var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
            var landBudget01 = Mathf.Max(0.01f, 1f - sea01);

            var seed = new Seed.WorldSeed(worldSeed);
            var peakSeed = seed.Fork("terrain.peak").Next();
            var rng = new Random(peakSeed);

            var count = WorldScale.PeakPreferredMinCount +
                        rng.Next(0, WorldScale.PeakPreferredMaxCount - WorldScale.PeakPreferredMinCount + 1);
            count = Mathf.Max(WorldScale.PeakMinCount, count);

            var step = Mathf.Max(4, res / 64);
            var candidates = new List<CrestSample>(128);
            var wppX = size.x / (res - 1);
            var wppZ = size.z / (res - 1);

            for (var pz = step; pz < res - step; pz += step)
            {
                var wz = pz * wppZ;
                for (var px = step; px < res - step; px += step)
                {
                    var wx = px * wppX;
                    var mask = IslandNaturalGeography.SampleMaskPublic(wx, wz, size, worldSeed);
                    if (mask.LandMask < 0.55f || mask.NormalizedRadius > 0.88f)
                    {
                        continue;
                    }

                    var rf = IslandNaturalGeography.SampleRidgeFieldPublic(wx, wz, worldSeed, true);
                    if (rf.Ridge < 0.28f || rf.Region < 0.12f)
                    {
                        continue;
                    }

                    var h = heights[pz, px];
                    if (h <= sea01 + 0.02f)
                    {
                        continue;
                    }

                    if (!IsLocalHeightMax(heights, res, px, pz, step))
                    {
                        continue;
                    }

                    candidates.Add(new CrestSample
                    {
                        WorldX = wx,
                        WorldZ = wz,
                        Score = rf.Ridge * 1.2f + rf.Region * 0.6f + (h - sea01) * 0.8f,
                        Height01 = h,
                    });
                }
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));

            var minSep = WorldScale.PeakMinSeparationMeters;
            var minSepSq = minSep * minSep;
            for (var i = 0; i < candidates.Count && result.Count < count; i++)
            {
                var c = candidates[i];
                var ok = true;
                for (var j = 0; j < result.Count; j++)
                {
                    var ddx = c.WorldX - result[j].WorldX;
                    var ddz = c.WorldZ - result[j].WorldZ;
                    if (ddx * ddx + ddz * ddz < minSepSq)
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    continue;
                }

                var axis = EstimateRidgeAxis(c.WorldX, c.WorldZ, worldSeed);
                result.Add(new Peak
                {
                    WorldX = c.WorldX,
                    WorldZ = c.WorldZ,
                    Amplitude01 = 0.22f + (float)rng.NextDouble() * 0.22f,
                    RadiusMeters = 70f + (float)rng.NextDouble() * 90f,
                    AxisX = axis.x,
                    AxisZ = axis.y,
                });
            }

            // If placement under-delivered, add high ridge-score sites (still on ridges, not cones).
            if (result.Count < WorldScale.PeakMinCount)
            {
                CollectRidgeScoreCandidates(candidates, heights, res, size, sea01, worldSeed, step);
                candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            }

            var guard = 0;
            while (result.Count < WorldScale.PeakMinCount && candidates.Count > 0 && guard < candidates.Count)
            {
                var c = candidates[guard++];
                var tooClose = false;
                for (var j = 0; j < result.Count; j++)
                {
                    var ddx = c.WorldX - result[j].WorldX;
                    var ddz = c.WorldZ - result[j].WorldZ;
                    if (ddx * ddx + ddz * ddz < (minSep * 0.5f) * (minSep * 0.5f))
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                var axis = EstimateRidgeAxis(c.WorldX, c.WorldZ, worldSeed);
                result.Add(new Peak
                {
                    WorldX = c.WorldX,
                    WorldZ = c.WorldZ,
                    Amplitude01 = 0.18f + (float)rng.NextDouble() * 0.18f,
                    RadiusMeters = 80f + (float)rng.NextDouble() * 70f,
                    AxisX = axis.x,
                    AxisZ = axis.y,
                });
            }

            for (var i = 0; i < result.Count; i++)
            {
                AmplifyRidgePeak(heights, res, size, sea01, landBudget01, result[i], rng.Next());
            }

            TerrainHeightWrite.Commit(terrain, heights);
            return result;
        }

        private struct CrestSample
        {
            public float WorldX;
            public float WorldZ;
            public float Score;
            public float Height01;
        }

        private static void CollectRidgeScoreCandidates(
            List<CrestSample> candidates,
            float[,] heights,
            int res,
            Vector3 size,
            float sea01,
            int worldSeed,
            int step)
        {
            var wppX = size.x / (res - 1);
            var wppZ = size.z / (res - 1);
            var existing = new HashSet<long>();
            for (var i = 0; i < candidates.Count; i++)
            {
                var key = ((long)(candidates[i].WorldX / wppX) << 32) ^ (long)(candidates[i].WorldZ / wppZ);
                existing.Add(key);
            }

            for (var pz = step; pz < res - step; pz += step)
            {
                var wz = pz * wppZ;
                for (var px = step; px < res - step; px += step)
                {
                    var wx = px * wppX;
                    var key = ((long)px << 32) ^ pz;
                    if (existing.Contains(key))
                    {
                        continue;
                    }

                    var mask = IslandNaturalGeography.SampleMaskPublic(wx, wz, size, worldSeed);
                    if (mask.LandMask < 0.55f)
                    {
                        continue;
                    }

                    var rf = IslandNaturalGeography.SampleRidgeFieldPublic(wx, wz, worldSeed, true);
                    if (rf.Ridge < 0.4f)
                    {
                        continue;
                    }

                    var h = heights[pz, px];
                    if (h <= sea01 + 0.03f)
                    {
                        continue;
                    }

                    candidates.Add(new CrestSample
                    {
                        WorldX = wx,
                        WorldZ = wz,
                        Score = rf.Ridge * 1.4f + rf.Region * 0.5f + (h - sea01) * 0.5f,
                        Height01 = h,
                    });
                }
            }
        }

        private static bool IsLocalHeightMax(float[,] heights, int res, int px, int pz, int radius)
        {
            var h = heights[pz, px];
            var r = Mathf.Max(2, radius / 2);
            for (var dz = -r; dz <= r; dz++)
            {
                var z = pz + dz;
                if (z < 0 || z >= res)
                {
                    continue;
                }

                for (var dx = -r; dx <= r; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    var x = px + dx;
                    if (x < 0 || x >= res)
                    {
                        continue;
                    }

                    if (heights[z, x] > h + 1e-5f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static Vector2 EstimateRidgeAxis(float wx, float wz, int worldSeed)
        {
            const float e = 12f;
            var r0 = IslandNaturalGeography.SampleRidgeFieldPublic(wx, wz, worldSeed, true).Ridge;
            var rx = IslandNaturalGeography.SampleRidgeFieldPublic(wx + e, wz, worldSeed, true).Ridge - r0;
            var rz = IslandNaturalGeography.SampleRidgeFieldPublic(wx, wz + e, worldSeed, true).Ridge - r0;
            var g = new Vector2(rx, rz);
            if (g.sqrMagnitude < 1e-8f)
            {
                return new Vector2(1f, 0f);
            }

            g.Normalize();
            return new Vector2(-g.y, g.x);
        }

        /// <summary>
        /// Raise an existing ridge crest with elongated foothill falloff along the ridge axis.
        /// </summary>
        private static void AmplifyRidgePeak(
            float[,] heights,
            int res,
            Vector3 size,
            float sea01,
            float landBudget01,
            Peak peak,
            int localSeed)
        {
            var rng = new Random(localSeed);
            var alongScale = 1.15f + (float)rng.NextDouble() * 0.45f;
            var acrossScale = 0.55f + (float)rng.NextDouble() * 0.35f;
            var axis = new Vector2(peak.AxisX, peak.AxisZ);
            if (axis.sqrMagnitude < 1e-6f)
            {
                axis = new Vector2(1f, 0f);
            }

            axis.Normalize();
            var across = new Vector2(-axis.y, axis.x);

            var worldPerPx = size.x / (res - 1);
            var worldPerPz = size.z / (res - 1);
            var rad = peak.RadiusMeters;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((peak.WorldX - rad * 1.6f) / worldPerPx));
            var maxPx = Mathf.Min(res - 1, Mathf.CeilToInt((peak.WorldX + rad * 1.6f) / worldPerPx));
            var minPz = Mathf.Max(0, Mathf.FloorToInt((peak.WorldZ - rad * 1.6f) / worldPerPz));
            var maxPz = Mathf.Min(res - 1, Mathf.CeilToInt((peak.WorldZ + rad * 1.6f) / worldPerPz));

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                var wz = pz * worldPerPz;
                for (var px = minPx; px <= maxPx; px++)
                {
                    var wx = px * worldPerPx;
                    var d = new Vector2(wx - peak.WorldX, wz - peak.WorldZ);
                    var along = Vector2.Dot(d, axis) / (rad * alongScale);
                    var side = Vector2.Dot(d, across) / (rad * acrossScale);
                    var r = Mathf.Sqrt(along * along + side * side);
                    if (r >= 1.2f)
                    {
                        continue;
                    }

                    var baseH = heights[pz, px];
                    if (baseH < sea01 + 0.04f)
                    {
                        continue;
                    }

                    var fall = 1f - Mathf.Clamp01(r);
                    fall = fall * fall;
                    var n = Mathf.PerlinNoise(wx * 0.018f + localSeed, wz * 0.018f);
                    var amp = peak.Amplitude01 * fall * (0.88f + 0.2f * n);
                    var boost = amp * landBudget01;
                    var target = Mathf.Clamp01(baseH + boost);
                    var maxPeak = sea01 + 0.92f * landBudget01;
                    heights[pz, px] = Mathf.Min(target, maxPeak);
                }
            }
        }
    }
}
