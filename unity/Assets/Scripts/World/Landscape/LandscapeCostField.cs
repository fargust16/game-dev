using System;
using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Precomputed landscape characteristics + nonlinear road traversal cost.
    /// Roads minimize distance + slope³ + earthwork + lake/mountain penalties — not shortest path.
    /// Accepted road corridors can be painted so later A* reuses them instead of running parallel.
    /// </summary>
    public sealed class LandscapeCostField
    {
        public const float BlockedCost = float.PositiveInfinity;

        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public int Width { get; }
        public int Depth { get; }

        public float SlopeWeight { get; set; } = 6f;
        public float ExcavationWeight { get; set; } = 8f;
        public float FillWeight { get; set; } = 5f;
        public float CurvatureWeight { get; set; } = 2f;
        public float MountainPenalty { get; set; } = 12f;
        public float ForestPenalty { get; set; } = 1.2f;
        public float LakeShorePenalty { get; set; } = 4f;
        public float AllowedEarthworkDepth { get; set; } = WorldScale.RoadAllowedEarthworkDepthMeters;
        public float MaxCutDepth { get; set; } = WorldScale.RoadMaxCutDepthMeters;
        public float MaxFillHeight { get; set; } = WorldScale.RoadMaxFillHeightMeters;
        public float MaxGrade { get; set; } = WorldScale.RoadMaxGrade;
        public float MaxGradeChange { get; set; } = WorldScale.RoadMaxGradeChange;

        public float MaxGradeChangeOrDefault() =>
            MaxGradeChange > 0f ? MaxGradeChange : WorldScale.RoadMaxGradeChange;
        public float DetourTolerance { get; set; } = WorldScale.RoadDetourTolerance;
        public float ExistingCorridorHalfWidth { get; set; } = WorldScale.RoadExistingCorridorHalfWidthMeters;
        public float ExistingCorridorDiscount { get; set; } = WorldScale.RoadExistingCorridorDiscount;

        private readonly float[] height;
        private readonly float[] slope01;
        private readonly float[] curvature;
        private readonly float[] roughness;
        private readonly float[] distToLake;
        private readonly float[] existingRoadDist;
        private readonly float[] heightBlur;
        private readonly byte[] biome;

        public LandscapeCostField(
            float originX,
            float originZ,
            float sizeX,
            float sizeZ,
            float cellSize,
            Func<float, float, float> heightAtWorld,
            Func<float, float, BiomeId> biomeAtWorld)
        {
            CellSize = Mathf.Max(cellSize, 4f);
            OriginX = originX;
            OriginZ = originZ;
            Width = Mathf.Max(2, Mathf.CeilToInt(sizeX / CellSize));
            Depth = Mathf.Max(2, Mathf.CeilToInt(sizeZ / CellSize));
            var n = Width * Depth;
            height = new float[n];
            slope01 = new float[n];
            curvature = new float[n];
            roughness = new float[n];
            distToLake = new float[n];
            existingRoadDist = new float[n];
            heightBlur = new float[n];
            biome = new byte[n];

            for (var i = 0; i < n; i++)
            {
                existingRoadDist[i] = float.PositiveInfinity;
            }

            for (var z = 0; z < Depth; z++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var wx = OriginX + (x + 0.5f) * CellSize;
                    var wz = OriginZ + (z + 0.5f) * CellSize;
                    var i = Index(x, z);
                    height[i] = heightAtWorld(wx, wz);
                    biome[i] = (byte)biomeAtWorld(wx, wz);
                    distToLake[i] = -9999f;
                }
            }

            ComputeSlopeCurvatureRoughness();
            BlurHeightField();
        }

        public float SampleHeight(float worldX, float worldZ) =>
            height[ClampIndex(worldX, worldZ)];

        public float SampleHeightBlur(float worldX, float worldZ) =>
            heightBlur[ClampIndex(worldX, worldZ)];

        public float SampleSlope01(float worldX, float worldZ) =>
            slope01[ClampIndex(worldX, worldZ)];

        public float SampleCurvature(float worldX, float worldZ) =>
            curvature[ClampIndex(worldX, worldZ)];

        public float SampleRoughness(float worldX, float worldZ) =>
            roughness[ClampIndex(worldX, worldZ)];

        public BiomeId SampleBiome(float worldX, float worldZ) =>
            (BiomeId)biome[ClampIndex(worldX, worldZ)];

        /// <summary>Positive inside water shore; negative outside (metres).</summary>
        public float SampleDistToLake(float worldX, float worldZ) =>
            distToLake[ClampIndex(worldX, worldZ)];

        /// <summary>
        /// Circular footprint stats for POI / lake orientation (world metres radius).
        /// </summary>
        public FootprintStats SampleFootprint(float worldX, float worldZ, float radiusMeters)
        {
            var r = Mathf.Max(radiusMeters, CellSize);
            var steps = Mathf.Max(2, Mathf.CeilToInt(r / CellSize));
            var sumSlope = 0f;
            var maxSlope = 0f;
            var sumH = 0f;
            var sumH2 = 0f;
            var flatCount = 0;
            var count = 0;
            var forest = 0;
            var meadow = 0;
            // ~8° rise/run ≈ 0.14; slope01 maps 0.47 ≈ 25° so 8° ≈ 0.14/0.47 ≈ 0.30
            const float flatSlope01 = 0.30f;

            for (var dz = -steps; dz <= steps; dz++)
            {
                for (var dx = -steps; dx <= steps; dx++)
                {
                    var wx = worldX + dx * CellSize;
                    var wz = worldZ + dz * CellSize;
                    var dist = Mathf.Sqrt(dx * dx + dz * dz) * CellSize;
                    if (dist > r)
                    {
                        continue;
                    }

                    var i = ClampIndex(wx, wz);
                    var s = slope01[i];
                    sumSlope += s;
                    if (s > maxSlope)
                    {
                        maxSlope = s;
                    }

                    var h = height[i];
                    sumH += h;
                    sumH2 += h * h;
                    if (s <= flatSlope01)
                    {
                        flatCount++;
                    }

                    if (biome[i] == (byte)BiomeId.Forest)
                    {
                        forest++;
                    }
                    else if (biome[i] == (byte)BiomeId.Meadow)
                    {
                        meadow++;
                    }

                    count++;
                }
            }

            if (count == 0)
            {
                return default;
            }

            var meanH = sumH / count;
            var variance = Mathf.Max(0f, sumH2 / count - meanH * meanH);
            return new FootprintStats(
                sumSlope / count,
                maxSlope,
                variance,
                flatCount / (float)count,
                forest / (float)count,
                meadow / (float)count,
                count);
        }

        /// <summary>
        /// Local basin direction from blurred height: returns (rotationRadians, aspect).
        /// Aspect &lt; 1 stretches along the valley (perpendicular to steepest ascent).
        /// </summary>
        public void SampleBasinOrientation(float worldX, float worldZ, float radiusMeters, out float rotationRadians, out float aspect)
        {
            var r = Mathf.Max(radiusMeters, CellSize * 2f);
            var steps = Mathf.Max(2, Mathf.CeilToInt(r / CellSize));
            var sumDx = 0f;
            var sumDz = 0f;
            var sumXx = 0f;
            var sumZz = 0f;
            var sumXz = 0f;
            var count = 0;
            var centerH = SampleHeightBlur(worldX, worldZ);

            for (var dz = -steps; dz <= steps; dz++)
            {
                for (var dx = -steps; dx <= steps; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    var dist = Mathf.Sqrt(dx * dx + dz * dz) * CellSize;
                    if (dist > r)
                    {
                        continue;
                    }

                    var wx = worldX + dx * CellSize;
                    var wz = worldZ + dz * CellSize;
                    var h = SampleHeightBlur(wx, wz);
                    var w = Mathf.Max(0f, centerH - h + 0.5f);
                    sumDx += dx * CellSize * w;
                    sumDz += dz * CellSize * w;
                    sumXx += (dx * CellSize) * (dx * CellSize) * w;
                    sumZz += (dz * CellSize) * (dz * CellSize) * w;
                    sumXz += (dx * CellSize) * (dz * CellSize) * w;
                    count++;
                }
            }

            if (count < 4)
            {
                rotationRadians = 0f;
                aspect = 0.85f;
                return;
            }

            // Valley axis ≈ smaller principal component of weighted offsets toward lower ground.
            var trace = sumXx + sumZz;
            var det = sumXx * sumZz - sumXz * sumXz;
            var disc = Mathf.Max(0f, trace * trace * 0.25f - det);
            var lambda1 = trace * 0.5f + Mathf.Sqrt(disc);
            var lambda2 = trace * 0.5f - Mathf.Sqrt(disc);
            // Eigenvector for smaller eigenvalue (valley long axis).
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
            if (len < 1e-5f)
            {
                rotationRadians = Mathf.Atan2(sumDz, sumDx);
                aspect = 0.85f;
                return;
            }

            rotationRadians = Mathf.Atan2(vz / len, vx / len);
            var ratio = lambda1 > 1e-4f ? lambda2 / lambda1 : 1f;
            aspect = Mathf.Clamp(0.55f + ratio * 0.45f, 0.55f, 1.1f);
        }

        /// <summary>
        /// Mark an accepted road corridor so later routing prefers walking on it.
        /// </summary>
        public void PaintExistingRoad(IReadOnlyList<Vector2> polylineXZ, float halfWidthMeters = -1f)
        {
            if (polylineXZ == null || polylineXZ.Count < 2)
            {
                return;
            }

            var half = halfWidthMeters > 0f ? halfWidthMeters : ExistingCorridorHalfWidth;
            var pad = half + CellSize;
            for (var s = 0; s < polylineXZ.Count - 1; s++)
            {
                var a = polylineXZ[s];
                var b = polylineXZ[s + 1];
                var minX = Mathf.Min(a.x, b.x) - pad;
                var maxX = Mathf.Max(a.x, b.x) + pad;
                var minZ = Mathf.Min(a.y, b.y) - pad;
                var maxZ = Mathf.Max(a.y, b.y) + pad;
                var x0 = Mathf.Clamp(Mathf.FloorToInt((minX - OriginX) / CellSize), 0, Width - 1);
                var x1 = Mathf.Clamp(Mathf.FloorToInt((maxX - OriginX) / CellSize), 0, Width - 1);
                var z0 = Mathf.Clamp(Mathf.FloorToInt((minZ - OriginZ) / CellSize), 0, Depth - 1);
                var z1 = Mathf.Clamp(Mathf.FloorToInt((maxZ - OriginZ) / CellSize), 0, Depth - 1);
                for (var z = z0; z <= z1; z++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var wx = OriginX + (x + 0.5f) * CellSize;
                        var wz = OriginZ + (z + 0.5f) * CellSize;
                        var d = DistancePointToSegment(new Vector2(wx, wz), a, b);
                        var i = Index(x, z);
                        if (d < existingRoadDist[i])
                        {
                            existingRoadDist[i] = d;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Nonlinear road cost at a world point for a desired road height (grade-aware earthwork).
        /// </summary>
        public float Evaluate(float worldX, float worldZ, float desiredRoadHeight)
        {
            var i = ClampIndex(worldX, worldZ);
            var lakeDist = distToLake[i];
            if (lakeDist >= 0f)
            {
                return BlockedCost;
            }

            var terrainH = height[i];
            var excavation = Mathf.Max(0f, terrainH - desiredRoadHeight);
            var fill = Mathf.Max(0f, desiredRoadHeight - terrainH);
            if (excavation > MaxCutDepth || fill > MaxFillHeight)
            {
                return BlockedCost;
            }

            var slope = slope01[i];
            var slopeCost = Mathf.Pow(slope, 3f) * SlopeWeight;
            var allowed = Mathf.Max(AllowedEarthworkDepth, 0.2f);
            var cutCost = Mathf.Pow(excavation / allowed, 2.5f) * ExcavationWeight;
            var fillCost = Mathf.Pow(fill / allowed, 2f) * FillWeight;
            var curveCost = curvature[i] * curvature[i] * CurvatureWeight;
            var mountain = biome[i] == (byte)BiomeId.Mountain ? MountainPenalty : 0f;
            var forest = biome[i] == (byte)BiomeId.Forest || biome[i] == (byte)BiomeId.Swamp
                ? ForestPenalty * (0.5f + roughness[i])
                : 0f;

            var shore = 0f;
            if (lakeDist > -40f && lakeDist < 0f)
            {
                var near = 1f + lakeDist / 40f;
                shore = LakeShorePenalty * near * near * (0.35f + slope);
            }

            var cost = 1f + slopeCost + cutCost + fillCost + curveCost + mountain + forest + shore;
            var roadDist = existingRoadDist[i];
            if (roadDist < ExistingCorridorHalfWidth)
            {
                var t = 1f - roadDist / Mathf.Max(ExistingCorridorHalfWidth, 0.01f);
                var discount = Mathf.Lerp(1f, ExistingCorridorDiscount, t * t);
                cost *= discount;
            }

            return cost;
        }

        public float EvaluateAtTerrain(float worldX, float worldZ) =>
            Evaluate(worldX, worldZ, SampleHeight(worldX, worldZ));

        public float EvaluateEdge(float x0, float z0, float x1, float z1)
        {
            var h0 = SampleHeight(x0, z0);
            var h1 = SampleHeight(x1, z1);
            var dist = Vector2.Distance(new Vector2(x0, z0), new Vector2(x1, z1));
            if (dist < 1e-3f)
            {
                return EvaluateAtTerrain(x0, z0);
            }

            var rawGrade = (h1 - h0) / dist;
            var grade = Mathf.Clamp(rawGrade, -MaxGrade, MaxGrade);
            var midX = (x0 + x1) * 0.5f;
            var midZ = (z0 + z1) * 0.5f;
            var desiredMid = (h0 + h1) * 0.5f;
            if (Mathf.Abs(rawGrade) > MaxGrade)
            {
                desiredMid = h0 + grade * (dist * 0.5f);
            }

            var c0 = Evaluate(x0, z0, h0);
            var c1 = Evaluate(x1, z1, h1);
            var cMid = Evaluate(midX, midZ, desiredMid);
            if (float.IsPositiveInfinity(c0) || float.IsPositiveInfinity(c1) || float.IsPositiveInfinity(cMid))
            {
                return BlockedCost;
            }

            return dist * (c0 + cMid + c1) / 3f;
        }

        public Func<float, float, float> AsPathCostFunc() =>
            (x, z) =>
            {
                var c = EvaluateAtTerrain(x, z);
                return float.IsPositiveInfinity(c) ? c : c;
            };

        private void ComputeSlopeCurvatureRoughness()
        {
            for (var z = 0; z < Depth; z++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var i = Index(x, z);
                    var hL = height[Index(Mathf.Max(0, x - 1), z)];
                    var hR = height[Index(Mathf.Min(Width - 1, x + 1), z)];
                    var hD = height[Index(x, Mathf.Max(0, z - 1))];
                    var hU = height[Index(x, Mathf.Min(Depth - 1, z + 1))];
                    var dx = (hR - hL) / (2f * CellSize);
                    var dz = (hU - hD) / (2f * CellSize);
                    var grade = Mathf.Sqrt(dx * dx + dz * dz);
                    slope01[i] = Mathf.Clamp01(grade / 0.47f);

                    var lap = (hL + hR + hD + hU) * 0.25f - height[i];
                    curvature[i] = Mathf.Clamp01(Mathf.Abs(lap) / Mathf.Max(CellSize * 0.05f, 0.2f));
                    roughness[i] = Mathf.Clamp01(Mathf.Abs(lap) / Mathf.Max(CellSize * 0.08f, 0.3f));
                }
            }
        }

        private void BlurHeightField()
        {
            // Two box-blur passes ≈ low-frequency macro terrain for lake orientation / shore factors.
            var tmp = new float[height.Length];
            Array.Copy(height, heightBlur, height.Length);
            for (var pass = 0; pass < 2; pass++)
            {
                for (var z = 0; z < Depth; z++)
                {
                    for (var x = 0; x < Width; x++)
                    {
                        var sum = 0f;
                        var n = 0;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var xx = Mathf.Clamp(x + dx, 0, Width - 1);
                                var zz = Mathf.Clamp(z + dz, 0, Depth - 1);
                                sum += heightBlur[Index(xx, zz)];
                                n++;
                            }
                        }

                        tmp[Index(x, z)] = sum / n;
                    }
                }

                Array.Copy(tmp, heightBlur, tmp.Length);
            }
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lenSq = ab.sqrMagnitude;
            if (lenSq < 1e-8f)
            {
                return Vector2.Distance(p, a);
            }

            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }


        private int Index(int x, int z) => z * Width + x;

        private int ClampIndex(float worldX, float worldZ)
        {
            var x = Mathf.Clamp(Mathf.FloorToInt((worldX - OriginX) / CellSize), 0, Width - 1);
            var z = Mathf.Clamp(Mathf.FloorToInt((worldZ - OriginZ) / CellSize), 0, Depth - 1);
            return Index(x, z);
        }
    }

    /// <summary>Circular footprint aggregates for POI scoring and lake orientation.</summary>
    public readonly struct FootprintStats
    {
        public float AverageSlope01 { get; }
        public float MaxSlope01 { get; }
        public float HeightVariance { get; }
        public float FlatAreaRatio { get; }
        public float ForestRatio { get; }
        public float MeadowRatio { get; }
        public int SampleCount { get; }

        public FootprintStats(
            float averageSlope01,
            float maxSlope01,
            float heightVariance,
            float flatAreaRatio,
            float forestRatio,
            float meadowRatio,
            int sampleCount)
        {
            AverageSlope01 = averageSlope01;
            MaxSlope01 = maxSlope01;
            HeightVariance = heightVariance;
            FlatAreaRatio = flatAreaRatio;
            ForestRatio = forestRatio;
            MeadowRatio = meadowRatio;
            SampleCount = sampleCount;
        }
    }
}
