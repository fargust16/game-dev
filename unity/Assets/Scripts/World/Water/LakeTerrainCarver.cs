using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Digs one smooth bowl under each lake flood mask. Plains often sit near heightmap 0, so
    /// the carver first shifts the tile up to create dig headroom, then cuts the bed under a
    /// locked water plane. <see cref="LastPerLakeMaxDigMeters"/> is the <b>actual</b> applied dig.
    /// </summary>
    public static class LakeTerrainCarver
    {
        /// <summary>Shallow shelf just inside the waterline (not the basin floor).</summary>
        public const float MinShoreDepthMeters = 4f;

        /// <summary>Large lakes dig at least this deep — enough for a sunk-city volume.</summary>
        public const float MinLargeBasinDepthMeters = 36f;

        /// <summary>Hard cap — deeper than this fights the heightmap ceiling and is not needed for V0.</summary>
        public const float MaxLargeBasinDepthMeters = 48f;

        // Plane sits just under the spill rim (low outlet), not the median high-bank —
        // median rim left the low side of slope lakes with a flying water sheet.
        public const float WaterFreeboardMeters = 0.35f;

        /// <summary>Spill percentile of dry-neighbour rim heights (0 = min, 1 = max).</summary>
        public const float RimSpillPercentile = 0.18f;

        /// <summary>Extra-wide soft bank for rounded waterline.</summary>
        public const float BankBlendMeters = 72f;

        public const float BedNoiseAmplitudeMeters = 1.2f;
        public const float BedNoiseFrequency = 0.01f;
        private const int ShoreBlurPasses = 4;

        /// <summary>Target dig depth requested for each lake on the last <see cref="Carve"/>.</summary>
        public static IReadOnlyList<float> LastPerLakeTargetDepthMeters { get; private set; } =
            System.Array.Empty<float>();

        /// <summary>Actual max dig applied (pre−post height), per lake.</summary>
        public static IReadOnlyList<float> LastPerLakeMaxDigMeters { get; private set; } =
            System.Array.Empty<float>();

        /// <summary>Deepest actual dig across all lakes on the last carve.</summary>
        public static float LastMaxDigMeters { get; private set; }

        /// <summary>How far the whole heightmap was lifted to make dig room (metres).</summary>
        public static float LastHeadroomShiftMeters { get; private set; }

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<WaterZone> zones,
            float targetBasinDepthMeters,
            TerrainLayer bankLayer = null)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                LastPerLakeTargetDepthMeters = System.Array.Empty<float>();
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                LastHeadroomShiftMeters = 0f;
                return;
            }

            var pinned = new List<WaterZone>(zones);
            if (pinned.Count == 0)
            {
                LastPerLakeTargetDepthMeters = System.Array.Empty<float>();
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                LastHeadroomShiftMeters = 0f;
                return;
            }

            var worldHeight = Mathf.Max(terrain.terrainData.size.y, 0.01f);
            var maxDepth = Mathf.Clamp(
                targetBasinDepthMeters,
                MinLargeBasinDepthMeters,
                Mathf.Min(MaxLargeBasinDepthMeters, worldHeight * 0.45f));

            CarveHeights(terrain, pinned, maxDepth);

            if (bankLayer != null)
            {
                PaintBanks(terrain, pinned, bankLayer);
            }

            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrain.terrainData;
            }
        }

        private static void CarveHeights(
            UnityEngine.Terrain terrain,
            IReadOnlyList<WaterZone> zones,
            float targetDepthMeters)
        {
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);
            var worldHeight = Mathf.Max(size.y, 0.01f);

            LastHeadroomShiftMeters = EnsureDigHeadroom(
                heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                zones, targetDepthMeters);

            // Water plane from rim AFTER headroom shift (heights not yet flushed — sample the array).
            for (var i = 0; i < zones.Count; i++)
            {
                zones[i].SetWaterLevel(
                    ComputeWaterLevelFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zones[i]));
            }

            var perLake = new float[zones.Count];
            var perTarget = new float[zones.Count];
            var globalMax = 0f;
            for (var i = 0; i < zones.Count; i++)
            {
                var depthHere = DepthForLakeSize(zones[i], targetDepthMeters);
                perTarget[i] = depthHere;
                perLake[i] = CarveFilledBowl(
                    heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    worldHeight, zones[i], depthHere);
                if (perLake[i] > globalMax)
                {
                    globalMax = perLake[i];
                }
            }

            LastPerLakeTargetDepthMeters = perTarget;
            LastPerLakeMaxDigMeters = perLake;
            LastMaxDigMeters = globalMax;

            SoftenShoreHeights(
                heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, zones);

            // Blur + exterior bank dig can pull the shoreline below the locked plane — raise the
            // dry rim back so the water sheet meets terrain instead of floating over a moat.
            SealLakeRims(
                heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight, zones);

            data.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Plains often sit near heightmap 0 — there is nowhere to dig. Lift the whole tile so lake
        /// pads have room for <paramref name="targetDepthMeters"/> (peaks clamp at 1).
        /// </summary>
        private static float EnsureDigHeadroom(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            IReadOnlyList<WaterZone> zones,
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
                    if (h < minH)
                    {
                        minH = h;
                    }

                    if (h > maxH)
                    {
                        maxH = h;
                    }
                }
            }

            // Prefer measuring under lake pads (plains), not mountain peaks.
            var padMin = 1f;
            var anyPad = false;
            for (var i = 0; i < zones.Count; i++)
            {
                var aabb = zones[i].GetFloodAabbXZ();
                var pad = BankBlendMeters + zones[i].CellSize;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX));
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX));
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ));
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ));
                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        anyPad = true;
                        var h = heights[pz, px];
                        if (h < padMin)
                        {
                            padMin = h;
                        }
                    }
                }
            }

            var floorNorm = anyPad ? padMin : minH;
            if (floorNorm >= needNorm)
            {
                return 0f;
            }

            var shift = needNorm - floorNorm;
            // Leave a little headroom under the normalized ceiling so peaks are not totally crushed.
            var maxShift = Mathf.Max(0f, 0.98f - maxH);
            if (shift > maxShift)
            {
                shift = maxShift;
            }

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

        /// <summary>
        /// Small ponds stay shallow; large lakes dig toward the requested sunk-city depth.
        /// </summary>
        public static float DepthForLakeSize(WaterZone zone, float requestedBasinDepth)
        {
            var r = zone.EquivalentRadiusMeters;
            var t = Mathf.Clamp01((r - 10f) / 55f);
            var smooth = t * t * (3f - 2f * t);
            var pondFloor = MinShoreDepthMeters;
            var largeCap = Mathf.Clamp(
                Mathf.Max(requestedBasinDepth, MinLargeBasinDepthMeters),
                MinLargeBasinDepthMeters,
                MaxLargeBasinDepthMeters);
            return Mathf.Lerp(pondFloor, largeCap, smooth);
        }

        private static void SoftenShoreHeights(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            IReadOnlyList<WaterZone> zones)
        {
            var sdfs = new float[zones.Count][,];
            for (var i = 0; i < zones.Count; i++)
            {
                sdfs[i] = LakeShoreField.BuildSignedDistance(zones[i], exteriorCapMeters: BankBlendMeters + 8f);
            }

            for (var pass = 0; pass < ShoreBlurPasses; pass++)
            {
                var copy = (float[,])heights.Clone();
                for (var pz = 1; pz < resolution - 1; pz++)
                {
                    for (var px = 1; px < resolution - 1; px++)
                    {
                        var worldX = terrainPos.x + px * worldPerPixelX;
                        var worldZ = terrainPos.z + pz * worldPerPixelZ;
                        var nearShore = false;
                        for (var i = 0; i < zones.Count; i++)
                        {
                            var signed = LakeShoreField.Sample(zones[i], sdfs[i], worldX, worldZ);
                            if (signed > -BankBlendMeters * 0.4f && signed < 12f)
                            {
                                nearShore = true;
                                break;
                            }
                        }

                        if (!nearShore)
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

        private static float CarveFilledBowl(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone,
            float targetDepthMeters)
        {
            var sdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
            var aabb = zone.GetFloodAabbXZ();
            var shoreBandMeters = Mathf.Max(30f, zone.CellSize * 8f);
            var seed = HashString(zone.Id);
            var maxActualDig = 0f;
            var waterPlaneY = zone.WaterLevelY;
            var floorY = terrainPos.y + 0.25f;

            var pad = BankBlendMeters + zone.CellSize;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX) - 1);
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX) + 1);
            var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ) - 1);
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ) + 1);

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var signed = LakeShoreField.Sample(zone, sdf, worldX, worldZ);
                    var currentWorldY = terrainPos.y + heights[pz, px] * worldHeight;

                    if (signed < 0f)
                    {
                        // Soft approach from ABOVE the plane only — never dig a dry moat under the
                        // waterline (that is exactly the "flying lake" look).
                        if (signed > -BankBlendMeters && currentWorldY > waterPlaneY + WaterFreeboardMeters)
                        {
                            var t = 1f + signed / BankBlendMeters;
                            var smooth = t * t * (3f - 2f * t);
                            var shelfY = waterPlaneY + WaterFreeboardMeters;
                            var target = Mathf.Lerp(currentWorldY, shelfY, smooth * 0.55f);
                            var appliedBank = WriteWorldYIfLower(
                                heights, pz, px, terrainPos.y, worldHeight, target);
                            if (appliedBank > maxActualDig)
                            {
                                maxActualDig = appliedBank;
                            }
                        }

                        continue;
                    }

                    var tIn = Mathf.Clamp01(signed / shoreBandMeters);
                    var smoothIn = tIn * tIn * (3f - 2f * tIn);
                    var dig = Mathf.Lerp(MinShoreDepthMeters, targetDepthMeters, smoothIn);
                    var n = Mathf.PerlinNoise(
                        (worldX + seed) * BedNoiseFrequency,
                        (worldZ - seed) * BedNoiseFrequency);
                    dig += (n - 0.5f) * 2f * BedNoiseAmplitudeMeters * smoothIn;
                    dig = Mathf.Clamp(dig, MinShoreDepthMeters * 0.5f, targetDepthMeters);

                    // Absolute bed under the water plane — fills the whole flood on slopes.
                    var targetInteriorY = Mathf.Max(floorY, waterPlaneY - dig);
                    var applied = WriteWorldYIfLower(
                        heights, pz, px, terrainPos.y, worldHeight, targetInteriorY);
                    if (applied > maxActualDig)
                    {
                        maxActualDig = applied;
                    }
                }
            }

            return maxActualDig;
        }

        /// <returns>Actual metres lowered at this pixel (0 if unchanged).</returns>
        private static float WriteWorldYIfLower(
            float[,] heights, int pz, int px, float terrainPosY, float worldHeight, float worldY)
        {
            var before = terrainPosY + heights[pz, px] * worldHeight;
            var candidate = (worldY - terrainPosY) / worldHeight;
            if (candidate >= heights[pz, px])
            {
                return 0f;
            }

            heights[pz, px] = Mathf.Clamp01(candidate);
            var after = terrainPosY + heights[pz, px] * worldHeight;
            return before - after;
        }

        private static float ComputeWaterLevelFromHeights(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            WaterZone zone)
        {
            var rim = new List<float>(256);
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x - 1, z);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x + 1, z);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x, z - 1);
                    TryAddDryRimFromHeights(
                        heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        zone, rim, x, z + 1);
                }
            }

            return SpillWaterLevel(rim, zone.WaterLevelY);
        }

        private static float SpillWaterLevel(List<float> rim, float fallbackWaterY)
        {
            if (rim == null || rim.Count == 0)
            {
                return fallbackWaterY - WaterFreeboardMeters;
            }

            rim.Sort();
            // True lake level = spill / low outlet. Median high-bank made slope lakes float.
            var idx = Mathf.Clamp(
                Mathf.RoundToInt((rim.Count - 1) * RimSpillPercentile),
                0,
                rim.Count - 1);
            // Ignore a couple of noisy low samples on large rims.
            if (rim.Count > 20)
            {
                idx = Mathf.Max(idx, 1);
            }

            return rim[idx] - WaterFreeboardMeters;
        }

        /// <summary>
        /// After shore blur: ensure dry land next to the flood sits at/above the water plane so the
        /// mesh cannot hover over a lowered bank.
        /// </summary>
        private static void SealLakeRims(
            float[,] heights,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            IReadOnlyList<WaterZone> zones)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                var waterY = zone.WaterLevelY;
                var sealY = waterY + WaterFreeboardMeters * 0.25f;
                var sdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
                var aabb = zone.GetFloodAabbXZ();
                var pad = zone.CellSize * 3f;
                var minPx = Mathf.Max(0, Mathf.FloorToInt((aabb.xMin - pad - terrainPos.x) / worldPerPixelX) - 1);
                var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.xMax + pad - terrainPos.x) / worldPerPixelX) + 1);
                var minPz = Mathf.Max(0, Mathf.FloorToInt((aabb.yMin - pad - terrainPos.z) / worldPerPixelZ) - 1);
                var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((aabb.yMax + pad - terrainPos.z) / worldPerPixelZ) + 1);

                for (var pz = minPz; pz <= maxPz; pz++)
                {
                    for (var px = minPx; px <= maxPx; px++)
                    {
                        var worldX = terrainPos.x + px * worldPerPixelX;
                        var worldZ = terrainPos.z + pz * worldPerPixelZ;
                        var signed = LakeShoreField.Sample(zone, sdf, worldX, worldZ);
                        // Dry ring only — never raise the wet basin floor.
                        if (signed >= -0.01f || signed < -zone.CellSize * 2.5f)
                        {
                            continue;
                        }

                        var current = terrainPos.y + heights[pz, px] * worldHeight;
                        if (current >= sealY)
                        {
                            continue;
                        }

                        var candidate = (sealY - terrainPos.y) / worldHeight;
                        if (candidate > heights[pz, px])
                        {
                            heights[pz, px] = Mathf.Clamp01(candidate);
                        }
                    }
                }
            }
        }

        private static void TryAddDryRimFromHeights(
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

        public static float ComputeAuthoritativeWaterLevel(UnityEngine.Terrain terrain, WaterZone zone)
        {
            var rim = new List<float>(256);
            for (var x = 0; x < zone.Width; x++)
            {
                for (var z = 0; z < zone.Depth; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    TryAddDryRim(terrain, zone, rim, x - 1, z);
                    TryAddDryRim(terrain, zone, rim, x + 1, z);
                    TryAddDryRim(terrain, zone, rim, x, z - 1);
                    TryAddDryRim(terrain, zone, rim, x, z + 1);
                }
            }

            return SpillWaterLevel(rim, zone.WaterLevelY);
        }

        private static void TryAddDryRim(
            UnityEngine.Terrain terrain, WaterZone zone, List<float> rim, int x, int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth || zone.IsFlooded(x, z))
            {
                return;
            }

            var wx = zone.OriginX + (x + 0.5f) * zone.CellSize;
            var wz = zone.OriginZ + (z + 0.5f) * zone.CellSize;
            rim.Add(terrain.SampleHeight(new Vector3(wx, 0f, wz)) + terrain.transform.position.y);
        }

        private static void PaintBanks(
            UnityEngine.Terrain terrain, IReadOnlyList<WaterZone> zones, TerrainLayer bankLayer)
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

            foreach (var zone in zones)
            {
                var sdf = LakeShoreField.BuildSignedDistance(zone, exteriorCapMeters: BankBlendMeters + 4f);
                var aabb = zone.GetFloodAabbXZ();
                var pad = BankBlendMeters + zone.CellSize;
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
                        var signed = LakeShoreField.Sample(zone, sdf, worldX, worldZ);
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
                            alphamaps[z, x, l] = l == layerIndex ? newWeight : alphamaps[z, x, l] * scale;
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

        private static int HashString(string s)
        {
            unchecked
            {
                var h = 23;
                for (var i = 0; i < s.Length; i++)
                {
                    h = h * 31 + s[i];
                }

                return h & 0x7fffffff;
            }
        }
    }
}
