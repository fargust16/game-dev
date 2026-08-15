using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Soft island framing over Vista height SoT.
    /// Does NOT rebuild mountains — preserves Vista relief, sinks ocean rim below SeaLevel,
    /// and carves closed inland lake basins.
    /// Coastline = Terrain height ∩ SeaLevel.
    /// </summary>
    public static class IslandNaturalGeography
    {
        public struct MaskSample
        {
            public float LandMask;
            public float NormalizedRadius;
        }

        /// <summary>
        /// Soft land-weight framing + lake basins. <paramref name="includeMountains"/> unused
        /// (mountains come from Vista); kept for call-site compatibility.
        /// </summary>
        public static void Apply(UnityEngine.Terrain terrain, int worldSeed, bool includeMountains = true)
        {
            _ = includeMountains;
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            TerrainHeightWrite.ForceBudgetSize(terrain);
            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var size = data.size;
            var pos = terrain.transform.position;
            var heights = data.GetHeights(0, 0, res, res);

            var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
            var shelfFloor01 = Mathf.Clamp01(
                (WorldScale.SeaLevelMeters - WorldScale.OceanShelfDepthMeters) /
                Mathf.Max(0.01f, size.y));
            var minSub01 = WorldScale.MinimumSubmersionMeters / Mathf.Max(0.01f, size.y);
            var landBudget01 = Mathf.Max(0.01f, 1f - sea01);

            var seed = new Seed.WorldSeed(worldSeed);
            var islandSeed = seed.Fork("terrain.island").Next();
            var coastSeed = seed.Fork("terrain.coast").Next();
            var lakeSeed = seed.Fork("lake.place").Next();

            var islandRng = new Random(islandSeed);
            var worldSize = new Vector3(
                WorldScale.WorldExtentXMeters,
                size.y,
                WorldScale.WorldExtentZMeters);
            var maskParams = BuildMaskParams(worldSize, islandRng, islandSeed, coastSeed);

            // Pass 1: preserve Vista relative relief on land; sink ocean via soft land weight.
            for (var z = 0; z < res; z++)
            {
                var localZ = (z / (float)(res - 1)) * size.z;
                var wz = pos.z + localZ;
                for (var x = 0; x < res; x++)
                {
                    var localX = (x / (float)(res - 1)) * size.x;
                    var wx = pos.x + localX;
                    var mask = SampleMask(wx, wz, maskParams);
                    var vista01 = Mathf.Clamp01(heights[z, x]);

                    var seabed =
                        shelfFloor01 +
                        (Mathf.PerlinNoise(wx * 0.0035f + 3.1f, wz * 0.0035f + 7.7f) - 0.5f) * 0.02f;
                    seabed = Mathf.Clamp(seabed, 0f, sea01 - minSub01);

                    // Keep Vista shape: remap full 0–1 field into [sea, budget] on land.
                    var landHeight = sea01 + vista01 * landBudget01;
                    var w = SoftLandWeight(mask.LandMask);
                    // Mild coastal ramp so shore is not a cliff: near mask edge prefer lower land.
                    if (w > 0.05f && w < 0.85f)
                    {
                        var coastLift = Mathf.Lerp(0.02f, 1f, Mathf.SmoothStep(0.05f, 0.85f, w));
                        landHeight = sea01 + vista01 * landBudget01 * coastLift;
                    }

                    var composed = Mathf.Lerp(seabed, landHeight, w);
                    if (w > 0.9f)
                    {
                        composed = Mathf.Max(composed, sea01 + 0.004f);
                    }

                    heights[z, x] = Mathf.Clamp01(composed);
                }
            }

            // Pass 2: closed lake basins (prefer low Vista pockets inland).
            CarveBasins(heights, res, size, pos, sea01, maskParams, lakeSeed);

            TerrainHeightWrite.Commit(terrain, heights);
        }

        public static MaskSample SampleMaskPublic(float wx, float wz, Vector3 size, int worldSeed)
        {
            _ = size;
            var seed = new Seed.WorldSeed(worldSeed);
            var islandSeed = seed.Fork("terrain.island").Next();
            var coastSeed = seed.Fork("terrain.coast").Next();
            var rng = new Random(islandSeed);
            var worldSize = new Vector3(
                WorldScale.WorldExtentXMeters,
                WorldScale.TerrainHeightBudgetMeters,
                WorldScale.WorldExtentZMeters);
            var p = BuildMaskParams(worldSize, rng, islandSeed, coastSeed);
            return SampleMask(wx, wz, p);
        }

        /// <summary>Legacy API — ridge field no longer generated in C# (Vista SoT).</summary>
        public static RidgeField SampleRidgeFieldPublic(
            float wx, float wz, int worldSeed, bool includeMountains)
        {
            _ = wx;
            _ = wz;
            _ = worldSeed;
            _ = includeMountains;
            return default;
        }

        public struct RidgeField
        {
            public float Region;
            public float Ridge;
            public float Valley;
        }

        private static float SoftLandWeight(float landMask)
        {
            var t = Mathf.Clamp01(landMask);
            return t * t * (3f - 2f * t);
        }

        private struct MaskParams
        {
            public float Cx;
            public float Cz;
            public float HalfW;
            public float HalfL;
            public float Cos;
            public float Sin;
            public int Seed;
            public int CoastSeed;
            public float ShelfNorm;
        }

        private static MaskParams BuildMaskParams(Vector3 worldSize, Random rng, int seed, int coastSeed)
        {
            var margin = WorldScale.IslandOceanMarginMeters;
            var halfW = worldSize.x * 0.5f - margin;
            var halfL = worldSize.z * 0.5f - margin;
            var aspect = 0.55f + (float)rng.NextDouble() * 0.7f;
            halfW *= aspect;
            halfL *= 1.15f / Mathf.Max(0.65f, aspect);
            var rot = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            var shelf = (WorldScale.CoastShelfWidthMeters * 1.8f) / Mathf.Max(halfW, halfL);
            return new MaskParams
            {
                Cx = worldSize.x * 0.5f + ((float)rng.NextDouble() - 0.5f) * 180f,
                Cz = worldSize.z * 0.5f + ((float)rng.NextDouble() - 0.5f) * 180f,
                HalfW = halfW,
                HalfL = halfL,
                Cos = Mathf.Cos(rot),
                Sin = Mathf.Sin(rot),
                Seed = seed,
                CoastSeed = coastSeed,
                ShelfNorm = Mathf.Clamp(shelf, 0.12f, 0.38f),
            };
        }

        private static MaskSample SampleMask(float wx, float wz, MaskParams p)
        {
            var dx = wx - p.Cx;
            var dz = wz - p.Cz;
            var rx = dx * p.Cos + dz * p.Sin;
            var rz = -dx * p.Sin + dz * p.Cos;
            var ang = Mathf.Atan2(rz, rx);

            var large =
                0.2f * Mathf.Sin(ang * 2f + p.Seed * 0.01f) +
                0.14f * Mathf.Sin(ang * 3f + p.CoastSeed * 0.017f + 0.8f);
            var medium =
                0.07f * Mathf.Sin(ang * 5f + p.Seed * 0.013f) +
                0.05f * (Mathf.PerlinNoise(wx * 0.0007f + 2f, wz * 0.0007f + 5f) - 0.5f) * 2f;
            var fine =
                0.025f * (Mathf.PerlinNoise(wx * 0.0018f + 9f, wz * 0.0018f + 4f) - 0.5f) * 2f;
            var wobble = 1f + large + medium + fine;

            var nx = rx / Mathf.Max(1f, p.HalfW * wobble);
            var nz = rz / Mathf.Max(1f, p.HalfL * wobble);
            var r = Mathf.Sqrt(nx * nx + nz * nz);

            var coastStart = 1f - p.ShelfNorm;
            float landMask;
            if (r <= coastStart * 0.7f)
            {
                landMask = 1f;
            }
            else if (r >= 1.1f)
            {
                landMask = 0f;
            }
            else
            {
                var t = Mathf.InverseLerp(1.1f, coastStart * 0.7f, r);
                landMask = t * t * t * (t * (t * 6f - 15f) + 10f);
            }

            return new MaskSample { LandMask = landMask, NormalizedRadius = r };
        }

        private struct BasinSite
        {
            public float Cx;
            public float Cz;
            public float Radius;
            public float Depth01;
            public float Aspect;
            public float Cos;
            public float Sin;
            public int NoiseSeed;
        }

        private static void CarveBasins(
            float[,] heights,
            int res,
            Vector3 size,
            Vector3 pos,
            float sea01,
            MaskParams maskParams,
            int lakeSeed)
        {
            var rng = new Random(lakeSeed);
            var target = WorldScale.LakeMinCount +
                         rng.Next(0, Mathf.Max(1, WorldScale.LakePreferredMaxCount - WorldScale.LakeMinCount + 1));
            var sites = new List<BasinSite>(target);
            var extentX = WorldScale.WorldExtentXMeters;
            var extentZ = WorldScale.WorldExtentZMeters;
            var attempts = 0;
            while (sites.Count < target && attempts++ < 100)
            {
                var cx = extentX * (0.28f + (float)rng.NextDouble() * 0.44f);
                var cz = extentZ * (0.28f + (float)rng.NextDouble() * 0.44f);
                var mask = SampleMask(cx, cz, maskParams);
                if (mask.LandMask < 0.62f)
                {
                    continue;
                }

                var tooClose = false;
                for (var i = 0; i < sites.Count; i++)
                {
                    var dx = cx - sites[i].Cx;
                    var dz = cz - sites[i].Cz;
                    if (dx * dx + dz * dz < 180f * 180f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                {
                    continue;
                }

                var rot = (float)(rng.NextDouble() * Mathf.PI);
                sites.Add(new BasinSite
                {
                    Cx = cx,
                    Cz = cz,
                    Radius = 70f + (float)rng.NextDouble() * 140f,
                    Depth01 = 0.03f + (float)rng.NextDouble() * 0.05f,
                    Aspect = 0.5f + (float)rng.NextDouble() * 0.8f,
                    Cos = Mathf.Cos(rot),
                    Sin = Mathf.Sin(rot),
                    NoiseSeed = rng.Next(),
                });
            }

            if (sites.Count == 0)
            {
                return;
            }

            var rim01 = Mathf.Clamp01(
                (WorldScale.SeaLevelMeters + WorldScale.LakeSafetyMarginMeters) /
                Mathf.Max(0.01f, size.y));
            var floor01 = sea01 - 0.03f;
            var wppX = size.x / (res - 1);
            var wppZ = size.z / (res - 1);

            for (var b = 0; b < sites.Count; b++)
            {
                var basin = sites[b];
                // Skip basins whose center is far outside this tile (cheap cull).
                if (basin.Cx < pos.x - basin.Radius * 1.5f ||
                    basin.Cx > pos.x + size.x + basin.Radius * 1.5f ||
                    basin.Cz < pos.z - basin.Radius * 1.5f ||
                    basin.Cz > pos.z + size.z + basin.Radius * 1.5f)
                {
                    continue;
                }

                var rad = basin.Radius * 1.4f;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((basin.Cx - rad - pos.x) / wppX));
                var maxPx = Mathf.Min(res - 1, Mathf.CeilToInt((basin.Cx + rad - pos.x) / wppX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((basin.Cz - rad - pos.z) / wppZ));
                var maxPz = Mathf.Min(res - 1, Mathf.CeilToInt((basin.Cz + rad - pos.z) / wppZ));

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    var wz = pos.z + pz * wppZ;
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var wx = pos.x + px * wppX;
                        var dx = wx - basin.Cx;
                        var dz = wz - basin.Cz;
                        var rx = (dx * basin.Cos + dz * basin.Sin) / basin.Radius;
                        var rz = (-dx * basin.Sin + dz * basin.Cos) / (basin.Radius * basin.Aspect);
                        // Domain-warped radius → irregular shoreline (not a clean ellipse).
                        var n1 = (Mathf.PerlinNoise(wx * 0.008f + basin.NoiseSeed, wz * 0.008f) - 0.5f) * 0.55f;
                        var n2 = (Mathf.PerlinNoise(wx * 0.018f + 11f, wz * 0.018f + basin.NoiseSeed) - 0.5f) * 0.25f;
                        var r = Mathf.Sqrt(rx * rx + rz * rz) + n1 + n2;
                        if (r >= 1.2f)
                        {
                            continue;
                        }

                        var mask = SampleMask(wx, wz, maskParams);
                        if (mask.LandMask < 0.45f)
                        {
                            continue;
                        }

                        if (r < 0.68f)
                        {
                            var bowl = 1f - Mathf.Clamp01(r / 0.68f);
                            bowl = bowl * bowl;
                            var bowlHeight01 = Mathf.Lerp(sea01 - 0.008f, floor01 - basin.Depth01 * 0.45f, bowl);
                            heights[pz, px] = Mathf.Min(heights[pz, px], Mathf.Clamp01(bowlHeight01));
                        }
                        else
                        {
                            var ring = 1f - Mathf.Clamp01((r - 0.68f) / 0.52f);
                            ring = ring * ring;
                            var raised = Mathf.Max(heights[pz, px], rim01);
                            heights[pz, px] = Mathf.Lerp(heights[pz, px], raised, ring * 0.75f);
                        }
                    }
                }
            }
        }
    }
}
