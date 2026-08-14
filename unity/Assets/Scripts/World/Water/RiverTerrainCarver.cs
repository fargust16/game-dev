using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Carves a continuous river trench (capsule along each centerline segment), not a chain of
    /// circular stamps — discrete stamps left crater pits when points were farther apart than width.
    /// </summary>
    public static class RiverTerrainCarver
    {
        private const int MinRadiusPixels = 3;
        /// <summary>Max spacing along the path when densifying before carve / paint.</summary>
        public const float DensifyStepMeters = 1.5f;
        private const int CorridorSoftenPasses = 2;

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<RiverPath> rivers,
            TerrainLayer bankLayer = null,
            float bankPaintWidthScale = 1.4f)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var pinned = new List<RiverPath>(rivers);
            if (pinned.Count == 0)
            {
                return;
            }

            CarveHeights(terrain, pinned);

            if (bankLayer != null)
            {
                PaintBanks(terrain, pinned, bankLayer, bankPaintWidthScale);
            }

            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrain.terrainData;
            }
        }

        private static void CarveHeights(UnityEngine.Terrain terrain, IReadOnlyList<RiverPath> rivers)
        {
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var original = (float[,])heights.Clone();
            var carvedMask = new bool[resolution, resolution];

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);
            var worldHeight = Mathf.Max(size.y, 0.01f);

            foreach (var river in rivers)
            {
                DensifyPath(
                    river, DensifyStepMeters,
                    out var pts, out var halfWidths, out var depths);

                for (var i = 0; i < pts.Count - 1; i++)
                {
                    CarveHeightSegment(
                        heights, original, carvedMask, resolution,
                        terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        pts[i], pts[i + 1],
                        halfWidths[i], halfWidths[i + 1],
                        depths[i], depths[i + 1]);
                }
            }

            SoftenCarvedCorridor(heights, carvedMask, resolution, CorridorSoftenPasses);

            data.SetHeights(0, 0, heights);
        }

        /// <summary>Evenly spaced samples along the polyline (XZ), lerping width/depth.</summary>
        public static void DensifyPath(
            RiverPath river,
            float stepMeters,
            out List<Vector2> pointsXZ,
            out List<float> halfWidths,
            out List<float> carveDepthsMeters)
        {
            var pts = new List<Vector2>();
            var widths = new List<float>();
            var depths = new List<float>();
            var src = river.Centerline;
            if (src.Count == 0)
            {
                pointsXZ = pts;
                halfWidths = widths;
                carveDepthsMeters = depths;
                return;
            }

            pts.Add(new Vector2(src[0].x, src[0].z));
            widths.Add(Mathf.Max(river.Widths[0] * 0.5f, 1f));
            depths.Add(Mathf.Max(river.CarveDepths[0], 0.5f));

            for (var i = 1; i < src.Count; i++)
            {
                var a = src[i - 1];
                var b = src[i];
                var flat = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
                var segments = Mathf.Max(1, Mathf.CeilToInt(flat / Mathf.Max(stepMeters, 0.5f)));
                for (var s = 1; s <= segments; s++)
                {
                    var t = s / (float)segments;
                    var p = Vector3.Lerp(a, b, t);
                    var w = Mathf.Lerp(river.Widths[i - 1], river.Widths[i], t);
                    var d = Mathf.Lerp(river.CarveDepths[i - 1], river.CarveDepths[i], t);
                    pts.Add(new Vector2(p.x, p.z));
                    widths.Add(Mathf.Max(w * 0.5f, 1f));
                    depths.Add(Mathf.Max(d, 0.5f));
                }
            }

            pointsXZ = pts;
            halfWidths = widths;
            carveDepthsMeters = depths;
        }

        private static void CarveHeightSegment(
            float[,] heights,
            float[,] original,
            bool[,] carvedMask,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            float worldHeight,
            Vector2 a,
            Vector2 b,
            float halfWidthA,
            float halfWidthB,
            float depthMetersA,
            float depthMetersB)
        {
            var bankExtra = WorldScale.RiverBankWidthMeters;
            var maxHalf = Mathf.Max(halfWidthA, halfWidthB) + bankExtra;
            var pad = maxHalf + worldPerPixelX * 2f;
            var minX = Mathf.Min(a.x, b.x) - pad;
            var maxX = Mathf.Max(a.x, b.x) + pad;
            var minZ = Mathf.Min(a.y, b.y) - pad;
            var maxZ = Mathf.Max(a.y, b.y) + pad;

            var minPx = Mathf.Max(0, Mathf.FloorToInt((minX - terrainPos.x) / worldPerPixelX) - 1);
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxX - terrainPos.x) / worldPerPixelX) + 1);
            var minPz = Mathf.Max(0, Mathf.FloorToInt((minZ - terrainPos.z) / worldPerPixelZ) - 1);
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((maxZ - terrainPos.z) / worldPerPixelZ) + 1);

            var ab = b - a;
            var abLenSq = ab.sqrMagnitude;
            if (abLenSq < 1e-6f)
            {
                return;
            }

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var worldX = terrainPos.x + px * worldPerPixelX;
                    var worldZ = terrainPos.z + pz * worldPerPixelZ;
                    var p = new Vector2(worldX, worldZ);
                    var ap = p - a;
                    var t = Mathf.Clamp01(Vector2.Dot(ap, ab) / abLenSq);
                    var closest = a + ab * t;
                    var dist = Vector2.Distance(p, closest);
                    var halfW = Mathf.Lerp(halfWidthA, halfWidthB, t);
                    var outer = halfW + bankExtra;
                    if (dist > outer)
                    {
                        continue;
                    }

                    var depthMeters = Mathf.Lerp(depthMetersA, depthMetersB, t);
                    float profile;
                    if (dist <= halfW)
                    {
                        var u = dist / Mathf.Max(halfW, 1e-3f);
                        // Softer U-profile — full depth on a wider centre strip.
                        var radial = u * u * (3f - 2f * u);
                        var centreFlat = 1f - Mathf.SmoothStep(0.35f, 1f, u);
                        profile = Mathf.Max(1f - radial, centreFlat * 0.85f);
                    }
                    else
                    {
                        // Soft bank shoulder: shallow blend beyond the water half-width.
                        // Keep at least WaterInsetDepth so river water is recessed like lakes.
                        var bankU = (dist - halfW) / Mathf.Max(bankExtra, 1e-3f);
                        var smooth = bankU * bankU * (3f - 2f * bankU);
                        var insetFloor = WorldScale.WaterInsetDepthMeters /
                                         Mathf.Max(depthMeters, 0.01f);
                        profile = Mathf.Max((1f - smooth) * 0.35f, (1f - smooth) * insetFloor);
                    }

                    var depthNorm = (depthMeters / worldHeight) * profile;
                    var candidate = original[pz, px] - depthNorm;
                    if (candidate < heights[pz, px])
                    {
                        heights[pz, px] = Mathf.Clamp01(candidate);
                        carvedMask[pz, px] = true;
                    }
                }
            }
        }

        private static void SoftenCarvedCorridor(
            float[,] heights, bool[,] carvedMask, int resolution, int passes)
        {
            var n = Mathf.Clamp(passes, 0, 6);
            if (n == 0)
            {
                return;
            }

            var scratch = new float[resolution, resolution];
            for (var pass = 0; pass < n; pass++)
            {
                for (var pz = 0; pz < resolution; pz++)
                {
                    for (var px = 0; px < resolution; px++)
                    {
                        if (!carvedMask[pz, px])
                        {
                            scratch[pz, px] = heights[pz, px];
                            continue;
                        }

                        var sum = heights[pz, px] * 4f;
                        var count = 4f;
                        for (var oz = -1; oz <= 1; oz++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oz == 0)
                                {
                                    continue;
                                }

                                var nx = px + ox;
                                var nz = pz + oz;
                                if (nx < 0 || nz < 0 || nx >= resolution || nz >= resolution)
                                {
                                    continue;
                                }

                                if (!carvedMask[nz, nx])
                                {
                                    continue;
                                }

                                sum += heights[nz, nx];
                                count += 1f;
                            }
                        }

                        scratch[pz, px] = sum / count;
                    }
                }

                for (var pz = 0; pz < resolution; pz++)
                {
                    for (var px = 0; px < resolution; px++)
                    {
                        if (carvedMask[pz, px])
                        {
                            heights[pz, px] = scratch[pz, px];
                        }
                    }
                }
            }
        }

        private static void PaintBanks(
            UnityEngine.Terrain terrain, IReadOnlyList<RiverPath> rivers, TerrainLayer bankLayer, float widthScale)
        {
            var data = terrain.terrainData;
            var layerIndex = EnsureLayer(data, bankLayer);
            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var influence = new float[resolution, resolution];

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            foreach (var river in rivers)
            {
                DensifyPath(river, DensifyStepMeters, out var pts, out var halfWidths, out _);
                for (var i = 0; i < pts.Count - 1; i++)
                {
                    var bankScale = 1f + WorldScale.RiverBankWidthMeters /
                        Mathf.Max(1f, Mathf.Max(halfWidths[i], halfWidths[i + 1]));
                    AccumulateInfluenceSegment(
                        influence, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        pts[i], pts[i + 1],
                        halfWidths[i] * widthScale * bankScale,
                        halfWidths[i + 1] * widthScale * bankScale);
                }
            }

            for (var z = 0; z < resolution; z++)
            {
                for (var x = 0; x < resolution; x++)
                {
                    var target = influence[z, x];
                    if (target <= 0f)
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

            data.SetAlphamaps(0, 0, alphamaps);
        }

        private static void AccumulateInfluenceSegment(
            float[,] influence,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            Vector2 a,
            Vector2 b,
            float halfWidthA,
            float halfWidthB)
        {
            var maxHalf = Mathf.Max(halfWidthA, halfWidthB);
            var pad = maxHalf + worldPerPixelX * 2f;
            var minPx = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad - terrainPos.x) / worldPerPixelX) - 1);
            var maxPx = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + pad - terrainPos.x) / worldPerPixelX) + 1);
            var minPz = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad - terrainPos.z) / worldPerPixelZ) - 1);
            var maxPz = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + pad - terrainPos.z) / worldPerPixelZ) + 1);

            var ab = b - a;
            var abLenSq = ab.sqrMagnitude;
            if (abLenSq < 1e-6f)
            {
                return;
            }

            for (var pz = minPz; pz <= maxPz; pz++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var p = new Vector2(
                        terrainPos.x + px * worldPerPixelX,
                        terrainPos.z + pz * worldPerPixelZ);
                    var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / abLenSq);
                    var dist = Vector2.Distance(p, a + ab * t);
                    var halfW = Mathf.Lerp(halfWidthA, halfWidthB, t);
                    if (dist > halfW)
                    {
                        continue;
                    }

                    var u = dist / Mathf.Max(halfW, 1e-3f);
                    var smooth = u * u * (3f - 2f * u);
                    var value = 1f - smooth;
                    if (value > influence[pz, px])
                    {
                        influence[pz, px] = value;
                    }
                }
            }
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
