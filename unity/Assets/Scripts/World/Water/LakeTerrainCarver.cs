using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Digs one smooth absolute bowl under each lake flood mask (wipes MapMagic terraces / erosion
    /// furrows inside the mask), then sets one authoritative <see cref="WaterZone.WaterLevelY"/>
    /// from the dry rim. Target depth defaults to half the terrain's peak local height.
    /// </summary>
    public static class LakeTerrainCarver
    {
        public const float MinShoreDepthMeters = 8f;
        public const float WaterFreeboardMeters = 0.55f;
        /// <summary>Extra-wide soft bank for rounded waterline.</summary>
        public const float BankBlendMeters = 72f;
        public const float BedNoiseAmplitudeMeters = 0.85f;
        public const float BedNoiseFrequency = 0.01f;
        private const int ShoreBlurPasses = 8;

        /// <summary>Max dig depth (metres) applied on the last <see cref="Carve"/> call, per lake order.</summary>
        public static IReadOnlyList<float> LastPerLakeMaxDigMeters { get; private set; } = System.Array.Empty<float>();

        /// <summary>Deepest dig across all lakes on the last carve.</summary>
        public static float LastMaxDigMeters { get; private set; }

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<WaterZone> zones,
            float targetBasinDepthMeters,
            TerrainLayer bankLayer = null)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                return;
            }

            var pinned = new List<WaterZone>(zones);
            if (pinned.Count == 0)
            {
                LastPerLakeMaxDigMeters = System.Array.Empty<float>();
                LastMaxDigMeters = 0f;
                return;
            }

            var worldHeight = Mathf.Max(terrain.terrainData.size.y, 0.01f);
            var maxDepth = Mathf.Clamp(targetBasinDepthMeters, MinShoreDepthMeters, worldHeight * 0.9f);
            CarveHeights(terrain, pinned, maxDepth);

            if (bankLayer != null)
            {
                PaintBanks(terrain, pinned, bankLayer);
            }

            foreach (var zone in pinned)
            {
                zone.SetWaterLevel(ComputeAuthoritativeWaterLevel(terrain, zone));
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

            var perLake = new float[zones.Count];
            var globalMax = 0f;
            for (var i = 0; i < zones.Count; i++)
            {
                var depthHere = DepthForLakeSize(zones[i], targetDepthMeters);
                perLake[i] = CarveFilledBowl(
                    heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    worldHeight, zones[i], depthHere);
                if (perLake[i] > globalMax)
                {
                    globalMax = perLake[i];
                }
            }

            LastPerLakeMaxDigMeters = perLake;
            LastMaxDigMeters = globalMax;

            SoftenShoreHeights(
                heights, resolution, terrainPos, worldPerPixelX, worldPerPixelZ, zones);

            data.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Ponds stay shallow; only large lakes dig toward half-mountain depth.
        /// </summary>
        public static float DepthForLakeSize(WaterZone zone, float halfMountainDepth)
        {
            var r = zone.EquivalentRadiusMeters;
            var t = Mathf.Clamp01((r - 10f) / 55f);
            var smooth = t * t * (3f - 2f * t);
            var pondFloor = MinShoreDepthMeters;
            var largeCap = Mathf.Max(halfMountainDepth, pondFloor);
            return Mathf.Lerp(pondFloor, largeCap, smooth);
        }

        /// <summary>
        /// Extra neighbour blur on pixels near any lake shore — rounds remaining heightmap stairs.
        /// </summary>
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
                            if (signed > -BankBlendMeters && signed < BankBlendMeters * 0.5f)
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

        /// <summary>
        /// Absolute smooth bowl: interior height is written to a target profile (not original−dig),
        /// so terraces and erosion grooves inside the flood disappear.
        /// </summary>
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
            // Shore band — full depth only well inside; long soft ramp kills stair cliffs.
            var shoreBandMeters = Mathf.Max(BankBlendMeters * 1.05f, zone.CellSize * 14f);
            var seed = HashString(zone.Id);
            var maxDigApplied = 0f;

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
                        if (signed > -BankBlendMeters)
                        {
                            var t = 1f + signed / BankBlendMeters;
                            var smooth = t * t * (3f - 2f * t);
                            var digBank = MinShoreDepthMeters * 0.35f * smooth;
                            WriteWorldYIfLower(
                                heights, pz, px, terrainPos.y, worldHeight, currentWorldY - digBank);
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

                    if (dig > maxDigApplied)
                    {
                        maxDigApplied = dig;
                    }

                    // Dig down from the real ground under this pixel — guarantees visual depth ≈ dig,
                    // even when flood spill Y sits far above the plains floor.
                    var targetInteriorY = currentWorldY - dig;
                    WriteWorldYIfLower(heights, pz, px, terrainPos.y, worldHeight, targetInteriorY);
                }
            }

            return maxDigApplied;
        }

        private static void WriteWorldYIfLower(
            float[,] heights, int pz, int px, float terrainPosY, float worldHeight, float worldY)
        {
            var candidate = (worldY - terrainPosY) / worldHeight;
            if (candidate < heights[pz, px])
            {
                heights[pz, px] = Mathf.Clamp01(candidate);
            }
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

            if (rim.Count == 0)
            {
                return zone.WaterLevelY - WaterFreeboardMeters;
            }

            rim.Sort();
            var idx = Mathf.Clamp(rim.Count / 4, 0, rim.Count - 1);
            return rim[idx] - WaterFreeboardMeters;
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
