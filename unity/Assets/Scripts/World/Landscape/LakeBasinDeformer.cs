using System.Collections.Generic;
using GameDev.World.Water;
using UnityEngine;

namespace GameDev.World.Landscape
{
    /// <summary>
    /// Applies analytic lake basins from <see cref="LakeDefinition"/> — designed bowl geometry,
    /// not originalNoise − dig. Interior is one wet bowl. Overlap ring may fill a tight shore lip
    /// so the unchanged flat water sheet tucks under banks (mesh is never clipped to terrain).
    /// </summary>
    public static class LakeBasinDeformer
    {
        public const float WaterFreeboardMeters = LakeTerrainCarver.WaterFreeboardMeters;
        public const float BankBlendMeters = 6f;
        public const float RimSpillPercentile = LakeTerrainCarver.RimSpillPercentile;
        public const float ShoreStripBlurMeters = LakeTerrainCarver.ShoreStripBlurMeters;
        private const int ShoreBlurPasses = 5;

        public static IReadOnlyList<float> LastPerLakeMaxDigMeters { get; private set; } =
            System.Array.Empty<float>();

        public static float LastMaxDigMeters { get; private set; }
        public static float LastHeadroomShiftMeters { get; private set; }

        public static void Apply(
            UnityEngine.Terrain terrain,
            IReadOnlyList<LakeDefinition> lakes,
            TerrainLayer bankLayer = null)
        {
            if (terrain == null || terrain.terrainData == null || lakes == null || lakes.Count == 0)
            {
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                LastHeadroomShiftMeters = 0f;
                return;
            }

            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);
            var worldHeight = Mathf.Max(size.y, 0.01f);

            var maxTarget = 0f;
            for (var i = 0; i < lakes.Count; i++)
            {
                if (lakes[i].TargetDepthMeters > maxTarget)
                {
                    maxTarget = lakes[i].TargetDepthMeters;
                }
            }

            LastHeadroomShiftMeters = EnsureDigHeadroom(
                heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                lakes, maxTarget);

            var original = (float[,])heights.Clone();

            for (var i = 0; i < lakes.Count; i++)
            {
                lakes[i].SetWaterLevel(ComputeSpillWaterLevel(
                    heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                    lakes[i]));
            }

            var perLake = new float[lakes.Count];
            var globalMax = 0f;
            for (var i = 0; i < lakes.Count; i++)
            {
                perLake[i] = CarveAnalyticBasin(
                    heights, original, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    worldHeight, lakes[i]);
                if (perLake[i] > globalMax)
                {
                    globalMax = perLake[i];
                }
            }

            LastPerLakeMaxDigMeters = perLake;
            LastMaxDigMeters = globalMax;

            for (var i = 0; i < lakes.Count; i++)
            {
                Debug.Log(
                    $"[LakeRecess] {lakes[i].Id} digDone: maxDig={perLake[i]:F2}m " +
                    $"waterY={lakes[i].WaterLevelY:F2} targetDepth={lakes[i].TargetDepthMeters:F1}m " +
                    $"overlap={lakes[i].WaterOverlapMeters:F2} basinExpand={lakes[i].BasinExpandMeters:F2} " +
                    $"lipFillMax={WorldScale.LakeShoreLipFillMaxMeters:F0}");
            }

            // Undo interior fills (no islands / split ridges). Keep overlap-ring lip fills.
            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    if (heights[pz, px] <= original[pz, px])
                    {
                        continue;
                    }

                    var wx = terrainPos.x + px * worldPerPixelX;
                    var wz = terrainPos.z + pz * worldPerPixelZ;
                    if (!IsShoreLipSample(lakes, wx, wz, worldPerPixelX, worldPerPixelZ))
                    {
                        heights[pz, px] = original[pz, px];
                    }
                }
            }

            BlurShoreStrip(
                heights, lakes, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight);

            data.SetHeights(0, 0, heights);

            if (bankLayer != null)
            {
                PaintBanks(terrain, lakes, bankLayer);
            }

            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrain.terrainData;
            }
        }

        private static float CarveAnalyticBasin(
            float[,] heights,
            float[,] original,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            LakeDefinition lake)
        {
            var aabb = lake.GetBasinAabbXZ();
            var pad = BankBlendMeters + 2f;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
            var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

            var waterPlaneY = lake.WaterLevelY;
            var floorY = terrainPos.y + 0.25f;
            var depth = lake.TargetDepthMeters;
            var dMax = EstimateBasinRadius(lake, minPx, maxPx, minPz, maxPz,
                terrainPos, worldPerPixelX, worldPerPixelZ);
            var shoreRadius = lake.Silhouette != null
                ? Mathf.Max(lake.Silhouette.RadiusMeters, 8f)
                : Mathf.Max(dMax, 8f);
            var overlap = Mathf.Max(lake.WaterOverlapMeters, 0.25f);
            var texel = Mathf.Max(worldPerPixelX, worldPerPixelZ);
            // Overlap (1.75 m) is often < one heightmap texel (~2.7 m). Lip must cover ≥2 texels.
            var lipBand = Mathf.Max(overlap, texel * 2f, 4f);
            var shelfBand = Mathf.Max(BankBlendMeters, lipBand);
            var maxDig = 0f;

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var originalY = terrainPos.y + original[pz, px] * worldHeight;
                    var shoreSd = lake.WaterShoreSignedDistance(worldX, worldZ);
                    var slope = EstimateLocalSlope(
                        heights, resolution, px, pz, worldPerPixelX, worldPerPixelZ, worldHeight);
                    var insetDepth = Mathf.Clamp(
                        WorldScale.WaterInsetDepthMeters + slope * WorldScale.WaterInsetSlopeFactor,
                        WorldScale.WaterInsetDepthMeters,
                        1.5f);
                    var shelfY = waterPlaneY + insetDepth;

                    if (shoreSd >= 0f)
                    {
                        var u = Mathf.Clamp01(shoreSd / shoreRadius);
                        var dig = LakeBathymetry.AnalyticBasinDepth01(u, depth);
                        var minWater = Mathf.Lerp(
                            1f,
                            WorldScale.LakeMinInteriorWaterMeters,
                            Mathf.Clamp01(shoreSd / 12f));
                        dig = Mathf.Max(dig, minWater);
                        var targetY = Mathf.Max(floorY, waterPlaneY - dig);
                        var applied = WriteDigOnly(
                            heights, original, pz, px, terrainPos.y, worldHeight, targetY,
                            WorldScale.LakeInteriorMaxDigMeters);
                        var afterY = terrainPos.y + heights[pz, px] * worldHeight;
                        if (afterY > waterPlaneY - 0.25f)
                        {
                            applied += WriteDigOnly(
                                heights, original, pz, px, terrainPos.y, worldHeight,
                                Mathf.Max(floorY, waterPlaneY - minWater),
                                WorldScale.TerrainHeightBudgetMeters);
                        }

                        if (applied > maxDig)
                        {
                            maxDig = applied;
                        }

                        continue;
                    }

                    if (shoreSd >= -lipBand)
                    {
                        var alreadyDug = heights[pz, px] < original[pz, px] - 1e-7f;
                        if (!alreadyDug && originalY < shelfY)
                        {
                            WriteToward(
                                heights, original, pz, px, terrainPos.y, worldHeight, shelfY,
                                maxDigMeters: 0f,
                                maxFillMeters: WorldScale.LakeShoreLipFillMaxMeters);
                        }
                        else if (originalY > shelfY)
                        {
                            var t = 1f + shoreSd / lipBand;
                            var smooth = t * t * (3f - 2f * t);
                            var target = Mathf.Lerp(originalY, shelfY, smooth * 0.95f);
                            var applied = WriteDigOnly(
                                heights, original, pz, px, terrainPos.y, worldHeight, target,
                                WorldScale.LakeMaxTerrainModificationMeters);
                            if (applied > maxDig)
                            {
                                maxDig = applied;
                            }
                        }

                        continue;
                    }

                    if (shoreSd > -shelfBand && originalY > waterPlaneY && originalY > shelfY)
                    {
                        var t = 1f + shoreSd / shelfBand;
                        var smooth = t * t * (3f - 2f * t);
                        var target = Mathf.Lerp(originalY, shelfY, smooth * 0.9f);
                        var applied = WriteDigOnly(
                            heights, original, pz, px, terrainPos.y, worldHeight, target,
                            WorldScale.LakeMaxTerrainModificationMeters);
                        if (applied > maxDig)
                        {
                            maxDig = applied;
                        }
                    }
                }
            }

            return maxDig;
        }

        private static bool IsShoreLipSample(
            IReadOnlyList<LakeDefinition> lakes,
            float worldX,
            float worldZ,
            float worldPerPixelX,
            float worldPerPixelZ)
        {
            for (var i = 0; i < lakes.Count; i++)
            {
                var sd = lakes[i].WaterShoreSignedDistance(worldX, worldZ);
                var lipBand = LipBandMeters(lakes[i], worldPerPixelX, worldPerPixelZ);
                if (sd < 0f && sd >= -lipBand)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 3×3 blur on both sides of the shore iso so the lip is a ramp, not heightmap stairs.
        /// Deep bowl (outside the strip) is left alone. Wet side stays below water; dry lip stays above.
        /// </summary>
        private static void BlurShoreStrip(
            float[,] heights,
            IReadOnlyList<LakeDefinition> lakes,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight)
        {
            var radius = ShoreStripBlurMeters;
            for (var pass = 0; pass < ShoreBlurPasses; pass++)
            {
                var copy = (float[,])heights.Clone();
                for (var i = 0; i < lakes.Count; i++)
                {
                    var lake = lakes[i];
                    var aabb = lake.GetBasinAabbXZ();
                    var pad = radius + BankBlendMeters;
                    var minPx = Mathf.Max(1, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                    var maxPx = Mathf.Min(resolution - 2, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                    var minPz = Mathf.Max(1, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                    var maxPz = Mathf.Min(resolution - 2, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

                    for (var pz = minPz; pz <= maxPz; pz++)
                    {
                        for (var px = minPx; px <= maxPx; px++)
                        {
                            var worldX = terrainPos.x + px * worldPerPixelX;
                            var worldZ = terrainPos.z + pz * worldPerPixelZ;
                            if (Mathf.Abs(lake.WaterShoreSignedDistance(worldX, worldZ)) > radius)
                            {
                                continue;
                            }

                            var sum =
                                copy[pz - 1, px - 1] + copy[pz - 1, px] + copy[pz - 1, px + 1] +
                                copy[pz, px - 1] + copy[pz, px] * 4f + copy[pz, px + 1] +
                                copy[pz + 1, px - 1] + copy[pz + 1, px] + copy[pz + 1, px + 1];
                            heights[pz, px] = sum / 12f;
                        }
                    }
                }
            }

            for (var i = 0; i < lakes.Count; i++)
            {
                ClampShoreAfterBlur(
                    heights, lakes[i], resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight);
            }
        }

        private static void ClampShoreAfterBlur(
            float[,] heights,
            LakeDefinition lake,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight)
        {
            var aabb = lake.GetBasinAabbXZ();
            var radius = ShoreStripBlurMeters;
            var lipBand = LipBandMeters(lake, worldPerPixelX, worldPerPixelZ);
            var pad = radius + BankBlendMeters;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
            var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));
            var waterY = lake.WaterLevelY;
            var wetMaxNorm = Mathf.Clamp01((waterY - 0.35f - terrainPos.y) / worldHeight);
            var dryMinNorm = Mathf.Clamp01((waterY + 0.2f - terrainPos.y) / worldHeight);

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var sd = lake.WaterShoreSignedDistance(worldX, worldZ);
                    if (sd >= 0f && sd <= radius)
                    {
                        if (heights[pz, px] > wetMaxNorm)
                        {
                            heights[pz, px] = wetMaxNorm;
                        }
                    }
                    else if (sd < 0f && sd >= -lipBand)
                    {
                        if (heights[pz, px] < dryMinNorm)
                        {
                            heights[pz, px] = dryMinNorm;
                        }
                    }
                }
            }
        }

        private static float LipBandMeters(
            LakeDefinition lake, float worldPerPixelX, float worldPerPixelZ)
        {
            var texel = Mathf.Max(worldPerPixelX, worldPerPixelZ);
            return Mathf.Max(lake.WaterOverlapMeters, texel * 2f, 4f);
        }

        private static float EstimateBasinRadius(
            LakeDefinition lake,
            int minPx,
            int maxPx,
            int minPz,
            int maxPz,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ)
        {
            if (lake.Silhouette != null)
            {
                return Mathf.Max(lake.Silhouette.RadiusMeters + lake.BasinExpandMeters, 8f);
            }

            var maxD = 0f;
            var step = Mathf.Max(1, (maxPx - minPx) / 32);
            for (var pz = minPz; pz <= maxPz; pz += step)
            {
                for (var px = minPx; px <= maxPx; px += step)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var d = lake.BasinSignedDistance(worldX, worldZ);
                    if (d > maxD)
                    {
                        maxD = d;
                    }
                }
            }

            return Mathf.Max(maxD, lake.Zone.EquivalentRadiusMeters, 8f);
        }

        private static float WriteDigOnly(
            float[,] heights,
            float[,] original,
            int pz,
            int px,
            float terrainPosY,
            float worldHeight,
            float worldY,
            float maxDigMeters = -1f)
        {
            return WriteToward(
                heights, original, pz, px, terrainPosY, worldHeight, worldY,
                maxDigMeters, maxFillMeters: 0f);
        }

        private static float WriteToward(
            float[,] heights,
            float[,] original,
            int pz,
            int px,
            float terrainPosY,
            float worldHeight,
            float worldY,
            float maxDigMeters,
            float maxFillMeters)
        {
            var before = terrainPosY + heights[pz, px] * worldHeight;
            var originalY = terrainPosY + original[pz, px] * worldHeight;
            var target = worldY;
            if (maxDigMeters >= 0f && target < originalY - maxDigMeters)
            {
                target = originalY - maxDigMeters;
            }

            if (maxFillMeters >= 0f && target > originalY + maxFillMeters)
            {
                target = originalY + maxFillMeters;
            }

            if (maxFillMeters <= 0f && target > before)
            {
                return 0f;
            }

            var candidate = Mathf.Clamp01((target - terrainPosY) / worldHeight);
            if (Mathf.Abs(candidate - heights[pz, px]) < 1e-7f)
            {
                return 0f;
            }

            heights[pz, px] = candidate;
            var after = terrainPosY + heights[pz, px] * worldHeight;
            return Mathf.Max(0f, before - after);
        }

        /// <summary>Local rise/run from heightmap neighbours (0 ≈ flat).</summary>
        private static float EstimateLocalSlope(
            float[,] heights,
            int resolution,
            int px,
            int pz,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight)
        {
            var x0 = Mathf.Max(0, px - 1);
            var x1 = Mathf.Min(resolution - 1, px + 1);
            var z0 = Mathf.Max(0, pz - 1);
            var z1 = Mathf.Min(resolution - 1, pz + 1);
            var dx = (heights[pz, x1] - heights[pz, x0]) * worldHeight /
                     Mathf.Max((x1 - x0) * worldPerPixelX, 0.01f);
            var dz = (heights[z1, px] - heights[z0, px]) * worldHeight /
                     Mathf.Max((z1 - z0) * worldPerPixelZ, 0.01f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static float EnsureDigHeadroom(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            IReadOnlyList<LakeDefinition> lakes,
            float targetDepthMeters)
        {
            var needNorm = (targetDepthMeters + 6f) / worldHeight;
            var minH = 1f;
            var maxH = 0f;
            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    var h = heights[pz, px];
                    if (h < minH) minH = h;
                    if (h > maxH) maxH = h;
                }
            }

            var padMin = 1f;
            var anyPad = false;
            for (var i = 0; i < lakes.Count; i++)
            {
                var aabb = lakes[i].GetBasinAabbXZ();
                var pad = BankBlendMeters;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        anyPad = true;
                        if (heights[pz, px] < padMin)
                        {
                            padMin = heights[pz, px];
                        }
                    }
                }
            }

            var floorNorm = anyPad ? padMin : minH;
            if (floorNorm >= needNorm)
            {
                return 0f;
            }

            var shift = Mathf.Min(needNorm - floorNorm, Mathf.Max(0f, 0.98f - maxH));
            if (shift <= 0.0001f)
            {
                return 0f;
            }

            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    heights[pz, px] = Mathf.Clamp01(heights[pz, px] + shift);
                }
            }

            return shift * worldHeight;
        }

        private static float ComputeSpillWaterLevel(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            LakeDefinition lake)
        {
            var rim = new List<float>(256);
            var zone = lake.Zone;
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    TryAddDryRim(heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        worldHeight, zone, rim, x - 1, z);
                    TryAddDryRim(heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        worldHeight, zone, rim, x + 1, z);
                    TryAddDryRim(heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        worldHeight, zone, rim, x, z - 1);
                    TryAddDryRim(heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        worldHeight, zone, rim, x, z + 1);
                }
            }

            var offset = Mathf.Max(
                WorldScale.WaterDepthOffsetMeters,
                WorldScale.WaterInsetDepthMeters);
            var before = lake.WaterLevelY;
            if (rim.Count == 0)
            {
                var fallback = lake.WaterLevelY - Mathf.Min(offset, WaterFreeboardMeters);
                LakeRecessDiagnostics.LogSpillCompute(
                    lake.Id, avgRimY: fallback + offset, rimCount: 0, before, fallback, offset);
                return fallback;
            }

            // Low-rim spill: avg pulled water ABOVE downhill shores (FLOAT 6–13 m in logs).
            // Use low percentile so the plane sits under weak banks too.
            rim.Sort();
            var lowIdx = Mathf.Clamp(
                Mathf.FloorToInt((rim.Count - 1) * RimSpillPercentile),
                0,
                rim.Count - 1);
            var lowRim = rim[lowIdx];
            var sum = 0f;
            for (var i = 0; i < rim.Count; i++)
            {
                sum += rim[i];
            }

            var avgRim = sum / rim.Count;
            var after = lowRim - offset;
            LakeRecessDiagnostics.LogSpillCompute(
                lake.Id, avgRim, rim.Count, before, after, offset);
            Debug.Log(
                $"[LakeRecess] {lake.Id} spillDetail: lowP{RimSpillPercentile:F2}={lowRim:F2} " +
                $"avg={avgRim:F2} min={rim[0]:F2} max={rim[rim.Count - 1]:F2} → waterY={after:F2}");
            return after;
        }

        private static void TryAddDryRim(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone,
            List<float> rim,
            int x,
            int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth || zone.IsFlooded(x, z))
            {
                return;
            }

            var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
            var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
            var px = Mathf.Clamp(Mathf.RoundToInt((wx - terrainPos.x) / worldPerPixelX), 0, resolution - 1);
            var pz = Mathf.Clamp(Mathf.RoundToInt((wz - terrainPos.z) / worldPerPixelZ), 0, resolution - 1);
            rim.Add(terrainPos.y + heights[pz, px] * worldHeight);
        }

        private static void PaintBanks(
            UnityEngine.Terrain terrain,
            IReadOnlyList<LakeDefinition> lakes,
            TerrainLayer bankLayer)
        {
            var data = terrain.terrainData;
            var layerIndex = EnsureLayer(data, bankLayer);
            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            for (var i = 0; i < lakes.Count; i++)
            {
                var lake = lakes[i];
                var aabb = lake.GetBasinAabbXZ();
                var pad = BankBlendMeters;
                var minAx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxAx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minAz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxAz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));

                for (var z = minAz; z <= maxAz; z++)
                {
                    for (var x = minAx; x <= maxAx; x++)
                    {
                        var worldX = terrainPos.x + (x + 0.5f) * worldPerPixelX;
                        var worldZ = terrainPos.z + (z + 0.5f) * worldPerPixelZ;
                        var signed = lake.BasinSignedDistance(worldX, worldZ);
                        if (signed > 5f || signed < -BankBlendMeters)
                        {
                            continue;
                        }

                        var t = signed >= 0f
                            ? 1f - signed / 5f
                            : 1f + signed / BankBlendMeters;
                        t = Mathf.Clamp01(t);
                        var target = t * t * (3f - 2f * t);
                        if (target <= 0.02f)
                        {
                            continue;
                        }

                        var current = alphamaps[z, x, layerIndex];
                        var newWeight = Mathf.Max(current, target);
                        if (newWeight <= current)
                        {
                            continue;
                        }

                        var otherSum = 1f - current;
                        var newOtherSum = 1f - newWeight;
                        var scale = otherSum > 0.0001f ? newOtherSum / otherSum : 0f;
                        for (var l = 0; l < layerCount; l++)
                        {
                            alphamaps[z, x, l] = l == layerIndex
                                ? newWeight
                                : alphamaps[z, x, l] * scale;
                        }
                    }
                }
            }

            data.SetAlphamaps(0, 0, alphamaps);
        }

        private static int EnsureLayer(TerrainData data, TerrainLayer layer)
        {
            var layers = data.terrainLayers;
            for (var i = 0; i < layers.Length; i++)
            {
                if (layers[i] == layer)
                {
                    return i;
                }
            }

            var expanded = new TerrainLayer[layers.Length + 1];
            layers.CopyTo(expanded, 0);
            expanded[layers.Length] = layer;
            data.terrainLayers = expanded;
            return layers.Length;
        }
    }
}
