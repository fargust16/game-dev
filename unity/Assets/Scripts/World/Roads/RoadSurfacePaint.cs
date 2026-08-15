using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Paints expressive packed-earth roads into terrain alphamaps:
    /// light center, dark edges, wheel ruts, gravel speckles, wet patches.
    /// Distance mask from the centerline — no extra geometry or draw calls.
    /// </summary>
    public static class RoadSurfacePaint
    {
        public static void Paint(
            UnityEngine.Terrain terrain,
            IReadOnlyList<RoadDefinition> roads,
            TerrainLayer centerLayer,
            TerrainLayer edgeLayer,
            TerrainLayer gravelLayer)
        {
            if (terrain == null || terrain.terrainData == null ||
                roads == null || roads.Count == 0 || centerLayer == null)
            {
                return;
            }

            var data = terrain.terrainData;
            var centerIndex = EnsureLayerIndex(data, centerLayer);
            var edgeIndex = edgeLayer != null ? EnsureLayerIndex(data, edgeLayer) : -1;
            var gravelIndex = gravelLayer != null ? EnsureLayerIndex(data, gravelLayer) : -1;

            // Layers may have grown; refresh after Ensure calls.
            data = terrain.terrainData;
            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            var transition = RoadSurfaceStyle.SoftRimMeters(
                Mathf.Max(worldPerPixelX, worldPerPixelZ));

            for (var r = 0; r < roads.Count; r++)
            {
                var road = roads[r];
                if (road.CenterlineXZ == null || road.CenterlineXZ.Count < 2)
                {
                    continue;
                }

                var halfRoad = Mathf.Max(0.5f, road.RoadWidthMeters * 0.5f);
                var packedEnd = halfRoad;
                var blendEnd = packedEnd + transition;
                var rutOffset = halfRoad * RoadSurfaceStyle.RutOffsetFraction;

                var dense = RoadTerrainCarver.DensifyPolyline(
                    road.CenterlineXZ, RoadCorridorDeformer.DensifyStepMeters);

                for (var i = 0; i < dense.Count - 1; i++)
                {
                    var a = dense[i];
                    var b = dense[i + 1];
                    var ab = b - a;
                    var len = ab.magnitude;
                    if (len < 1e-4f)
                    {
                        continue;
                    }

                    var dir = ab / len;
                    var pad = blendEnd + 2.5f;
                    var minAx = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad - terrainPos.x) / worldPerPixelX));
                    var maxAx = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + pad - terrainPos.x) / worldPerPixelX));
                    var minAz = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad - terrainPos.z) / worldPerPixelZ));
                    var maxAz = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + pad - terrainPos.z) / worldPerPixelZ));

                    for (var z = minAz; z <= maxAz; z++)
                    {
                        for (var x = minAx; x <= maxAx; x++)
                        {
                            var worldX = terrainPos.x + (x + 0.5f) * worldPerPixelX;
                            var worldZ = terrainPos.z + (z + 0.5f) * worldPerPixelZ;
                            var p = new Vector2(worldX, worldZ);
                            var ap = p - a;
                            var t = Mathf.Clamp01(Vector2.Dot(ap, dir) / len);
                            var closest = a + dir * (t * len);
                            var delta = p - closest;
                            var dist = delta.magnitude;
                            if (dist > blendEnd)
                            {
                                continue;
                            }

                            // Signed lateral distance (left/right of travel).
                            var cross = dir.x * delta.y - dir.y * delta.x;
                            var sd = cross >= 0f ? dist : -dist;
                            var along = (a.x + b.x) * 0.37f + (a.y + b.y) * 0.71f + t * len;

                            // Distance masks: 0 at centerline → falloff to blend end.
                            var outer = 1f - Smooth01(Mathf.Clamp01(dist / blendEnd));
                            if (outer <= 0.01f)
                            {
                                continue;
                            }

                            // Light packed bed (memo: warmer trampled center).
                            var centerMask = 1f - Smooth01(Mathf.Clamp01(dist / Mathf.Max(halfRoad * 0.88f, 0.01f)));
                            centerMask *= outer;

                            // Darker dirt ring (memo: edge / moisture) — wider soft shoulder.
                            var edgeStart = halfRoad * 0.45f;
                            var edgeEnd = packedEnd;
                            var edgeMask = Smooth01(InverseLerpClamped(edgeStart, edgeEnd, dist));
                            edgeMask *= 1f - Smooth01(InverseLerpClamped(packedEnd, blendEnd, dist));
                            edgeMask *= outer;

                            // Asymmetric wheel ruts that weave slightly along the road (avoid twin rails).
                            var weave = Mathf.Sin(along * 0.31f) * 0.12f
                                        + (Hash01(Mathf.RoundToInt(along * 0.5f) * 9182741) - 0.5f) * 0.08f;
                            var rutLeft = rutOffset * (1f + weave);
                            var rutRight = rutOffset * (1f - weave * 0.65f);
                            var rutHalfW = RoadSurfaceStyle.RutWidthMeters *
                                           Mathf.Lerp(0.85f, 1.2f, Hash01(Mathf.RoundToInt(along) * 374761393));
                            var rutL = 1f - Smooth01(Mathf.Clamp01(Mathf.Abs(sd - rutLeft) / rutHalfW));
                            var rutR = 1f - Smooth01(Mathf.Clamp01(Mathf.Abs(sd + rutRight) / rutHalfW));
                            var rutMask = Mathf.Max(rutL * 0.95f, rutR * 0.8f);
                            rutMask *= Mathf.Lerp(0.55f, 1f, centerMask) * RoadSurfaceStyle.RutStrength;

                            // Wet / dark patches (irregular, not tiled).
                            var wetNoise = Hash01(
                                Mathf.RoundToInt(worldX * 1.7f) * 374761393
                                ^ Mathf.RoundToInt(worldZ * 1.7f) * 668265263
                                ^ (r + 31) * 1274126177);
                            var wetMask = 0f;
                            if (wetNoise > 1f - RoadSurfaceStyle.WetPatchAmount * 0.32f && centerMask > 0.2f)
                            {
                                wetMask = (wetNoise - (1f - RoadSurfaceStyle.WetPatchAmount * 0.32f)) /
                                          Mathf.Max(RoadSurfaceStyle.WetPatchAmount * 0.32f, 0.01f);
                                wetMask = Smooth01(wetMask) * centerMask * 0.7f;
                            }

                            // Thin crack / irregularity streaks near the shoulder (directional).
                            var crackBand = 1f - Smooth01(Mathf.Abs(dist - halfRoad * 0.9f) / 0.55f);
                            var crackNoise = Hash01(Mathf.RoundToInt(along * 2.2f) * 1597334677 ^ r * 224682251);
                            var crackMask = 0f;
                            if (crackNoise > 1f - RoadSurfaceStyle.CrackAmount * 0.45f)
                            {
                                crackMask = crackBand * Smooth01(crackNoise) * RoadSurfaceStyle.CrackAmount;
                            }

                            // Color / roughness variation (breaks flat plastic dirt).
                            var rough = Hash01(x * 73856093 ^ z * 19349663 ^ r * 83492791);
                            var alongRough = 0.5f + 0.5f * Mathf.Sin(along * 0.19f + rough * 6.28f);
                            var roughMul = Mathf.Lerp(
                                1f - RoadSurfaceStyle.RoughnessVariation * 0.4f,
                                1f + RoadSurfaceStyle.RoughnessVariation * 0.22f,
                                Mathf.Lerp(rough, alongRough, 0.45f));

                            var centerW = centerMask * RoadSurfaceStyle.CenterStrength *
                                          RoadSurfaceStyle.Brightness * roughMul;
                            centerW *= 1f - rutMask * 0.6f;
                            centerW *= 1f - crackMask * 0.35f;
                            centerW = Mathf.Clamp01(centerW);

                            var edgeW = 0f;
                            if (edgeIndex >= 0)
                            {
                                edgeW = Mathf.Max(
                                    edgeMask * RoadSurfaceStyle.EdgeStrength,
                                    rutMask,
                                    wetMask,
                                    crackMask * 0.85f);
                                edgeW *= Mathf.Lerp(0.88f, 1.12f, Hash01(x * 19349663 ^ z * 83492791));
                                edgeW = Mathf.Clamp01(edgeW * outer);
                            }

                            // Gravel/stones prefer shoulders and ruts (memo: detail overlay).
                            var gravelW = 0f;
                            if (gravelIndex >= 0)
                            {
                                var gravelGate = Mathf.Max(edgeMask * 1.1f, centerMask * 0.55f, rutMask * 0.7f);
                                if (gravelGate > 0.12f)
                                {
                                    var stone = Hash01(
                                        x * 265443576 ^ z * 1013904223 ^ (r + 7) * 1597334677
                                        ^ Mathf.RoundToInt(along) * 224682251);
                                    var threshold = 1f - Mathf.Lerp(0.14f, 0.4f, RoadSurfaceStyle.GravelAmount);
                                    if (stone > threshold)
                                    {
                                        var amt = (stone - threshold) / Mathf.Max(1f - threshold, 0.01f);
                                        gravelW = Mathf.Lerp(0.08f, 0.36f, amt) * gravelGate *
                                                  Mathf.Lerp(0.55f, 1f, RoadSurfaceStyle.GravelAmount);
                                        gravelW = Mathf.Clamp01(gravelW);
                                    }
                                }
                            }

                            if (centerW <= 0.02f && edgeW <= 0.02f && gravelW <= 0.02f)
                            {
                                continue;
                            }

                            // Soft grass reveal in the transition band; solid bed stays readable.
                            var packed = Smooth01(centerMask);
                            var coverage = Mathf.Lerp(
                                RoadSurfaceStyle.TransitionCoverage,
                                RoadSurfaceStyle.BedCoverage,
                                packed);
                            var sum = centerW + edgeW + gravelW;
                            if (sum > 1e-4f && sum < coverage)
                            {
                                var boost = coverage / sum;
                                centerW = Mathf.Clamp01(centerW * boost);
                                edgeW = Mathf.Clamp01(edgeW * boost);
                                gravelW = Mathf.Clamp01(gravelW * boost);
                            }

                            WriteRoadPixel(
                                alphamaps, z, x, layerCount,
                                centerIndex, centerW,
                                edgeIndex, edgeW,
                                gravelIndex, gravelW);
                        }
                    }
                }
            }

            data.SetAlphamaps(0, 0, alphamaps);
        }

        /// <summary>
        /// Repaint roads from densified clearance samples (when full <see cref="RoadDefinition"/>
        /// list is not available). Uses segment distance between ordered samples (not radial stamps).
        /// </summary>
        public static void PaintFromClearanceSamples(
            UnityEngine.Terrain terrain,
            IReadOnlyList<Vector2> samples,
            float roadWidthMeters,
            float shoulderMeters,
            TerrainLayer centerLayer,
            TerrainLayer edgeLayer,
            TerrainLayer gravelLayer)
        {
            _ = shoulderMeters; // height shoulder stays in WorldScale; packed paint uses halfRoad only.
            if (terrain == null || terrain.terrainData == null ||
                samples == null || samples.Count == 0 || centerLayer == null)
            {
                return;
            }

            var data = terrain.terrainData;
            var centerIndex = EnsureLayerIndex(data, centerLayer);
            var edgeIndex = edgeLayer != null ? EnsureLayerIndex(data, edgeLayer) : -1;
            var gravelIndex = gravelLayer != null ? EnsureLayerIndex(data, gravelLayer) : -1;
            data = terrain.terrainData;

            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            var halfRoad = Mathf.Max(0.5f, roadWidthMeters * 0.5f);
            var packedEnd = halfRoad;
            var transition = RoadSurfaceStyle.SoftRimMeters(
                Mathf.Max(worldPerPixelX, worldPerPixelZ));
            var blendEnd = packedEnd + transition;
            var pad = blendEnd + 2.5f;

            for (var i = 0; i < samples.Count - 1; i++)
            {
                var a = samples[i];
                var b = samples[i + 1];
                var ab = b - a;
                var len = ab.magnitude;
                if (len < 1e-4f || len > RoadSampleDistance.MaxSegmentGapMeters)
                {
                    continue;
                }

                var dir = ab / len;
                var minAx = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x) - pad - terrainPos.x) / worldPerPixelX));
                var maxAx = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.x, b.x) + pad - terrainPos.x) / worldPerPixelX));
                var minAz = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y) - pad - terrainPos.z) / worldPerPixelZ));
                var maxAz = Mathf.Min(resolution - 1, Mathf.CeilToInt((Mathf.Max(a.y, b.y) + pad - terrainPos.z) / worldPerPixelZ));

                for (var z = minAz; z <= maxAz; z++)
                {
                    for (var x = minAx; x <= maxAx; x++)
                    {
                        var worldX = terrainPos.x + (x + 0.5f) * worldPerPixelX;
                        var worldZ = terrainPos.z + (z + 0.5f) * worldPerPixelZ;
                        var p = new Vector2(worldX, worldZ);
                        var dist = RoadSampleDistance.PointToSegmentMeters(p, a, b);
                        if (dist > blendEnd)
                        {
                            continue;
                        }

                        var outer = 1f - Smooth01(Mathf.Clamp01(dist / blendEnd));
                        var centerMask = SampleCenterMask(dist, halfRoad, blendEnd);
                        var edgeMask = edgeIndex >= 0
                            ? SampleEdgeMask(dist, halfRoad, packedEnd, blendEnd)
                            : 0f;

                        var along = (a.x + b.x) * 0.37f + (a.y + b.y) * 0.71f;
                        var rough = Hash01(x * 73856093 ^ z * 19349663 ^ i * 83492791);
                        var alongRough = 0.5f + 0.5f * Mathf.Sin(along * 0.19f + rough * 6.28f);
                        var roughMul = Mathf.Lerp(
                            1f - RoadSurfaceStyle.RoughnessVariation * 0.45f,
                            1f + RoadSurfaceStyle.RoughnessVariation * 0.25f,
                            Mathf.Lerp(rough, alongRough, 0.45f));

                        var centerW = Mathf.Clamp01(
                            centerMask * RoadSurfaceStyle.CenterStrength *
                            RoadSurfaceStyle.Brightness * roughMul);
                        var edgeW = Mathf.Clamp01(edgeMask * RoadSurfaceStyle.EdgeStrength * outer);
                        var gravelW = 0f;
                        if (gravelIndex >= 0 && centerMask > 0.15f)
                        {
                            var stone = Hash01(x * 374761393 ^ z * 668265263 ^ i * 1274126177);
                            var threshold = 1f - Mathf.Lerp(0.18f, 0.55f, RoadSurfaceStyle.GravelAmount);
                            if (stone > threshold)
                            {
                                gravelW = Mathf.Lerp(0.1f, 0.4f, stone) * centerMask;
                            }
                        }

                        var packed = Smooth01(centerMask);
                        var coverage = Mathf.Lerp(
                            RoadSurfaceStyle.TransitionCoverage,
                            RoadSurfaceStyle.BedCoverage,
                            packed);
                        var sum = centerW + edgeW + gravelW;
                        if (sum > 1e-4f && sum < coverage)
                        {
                            var boost = coverage / sum;
                            centerW = Mathf.Clamp01(centerW * boost);
                            edgeW = Mathf.Clamp01(edgeW * boost);
                            gravelW = Mathf.Clamp01(gravelW * boost);
                        }

                        if (centerW > 0.02f || edgeW > 0.02f || gravelW > 0.02f)
                        {
                            WriteRoadPixel(
                                alphamaps, z, x, layerCount,
                                centerIndex, centerW,
                                edgeIndex, edgeW,
                                gravelIndex, gravelW);
                        }
                    }
                }
            }

            data.SetAlphamaps(0, 0, alphamaps);
            Debug.Log(
                $"[Roads] Repainted packed-earth surface from {samples.Count} clearance samples " +
                $"(width={roadWidthMeters:F1}m, segment SDF). BedCoverage={RoadSurfaceStyle.BedCoverage:F2}.");
        }

        /// <summary>
        /// Sets road layer weights and renormalizes the remaining splat so coverage stays 1.
        /// </summary>
        public static void WriteRoadPixel(
            float[,,] alphamaps,
            int z,
            int x,
            int layerCount,
            int centerIndex,
            float centerW,
            int edgeIndex,
            float edgeW,
            int gravelIndex,
            float gravelW)
        {
            centerW = Mathf.Max(0f, centerW);
            edgeW = Mathf.Max(0f, edgeW);
            gravelW = Mathf.Max(0f, gravelW);

            // Prefer max with existing road paint so overlapping corridors accumulate.
            centerW = Mathf.Max(alphamaps[z, x, centerIndex], centerW);
            if (edgeIndex >= 0)
            {
                edgeW = Mathf.Max(alphamaps[z, x, edgeIndex], edgeW);
            }

            if (gravelIndex >= 0)
            {
                gravelW = Mathf.Max(alphamaps[z, x, gravelIndex], gravelW);
            }

            var roadSum = centerW + edgeW + gravelW;
            if (roadSum <= 0.001f)
            {
                return;
            }

            if (roadSum > RoadSurfaceStyle.BedCoverage)
            {
                var s = RoadSurfaceStyle.BedCoverage / roadSum;
                centerW *= s;
                edgeW *= s;
                gravelW *= s;
                roadSum = RoadSurfaceStyle.BedCoverage;
            }

            var keep = 1f - roadSum;
            var otherSum = 0f;
            for (var l = 0; l < layerCount; l++)
            {
                if (l == centerIndex || l == edgeIndex || l == gravelIndex)
                {
                    continue;
                }

                otherSum += alphamaps[z, x, l];
            }

            var otherScale = otherSum > 1e-5f ? keep / otherSum : 0f;
            for (var l = 0; l < layerCount; l++)
            {
                if (l == centerIndex)
                {
                    alphamaps[z, x, l] = centerW;
                }
                else if (l == edgeIndex)
                {
                    alphamaps[z, x, l] = edgeW;
                }
                else if (l == gravelIndex)
                {
                    alphamaps[z, x, l] = gravelW;
                }
                else
                {
                    alphamaps[z, x, l] *= otherScale;
                }
            }
        }

        public static float SampleCenterMask(float distMeters, float halfRoadMeters, float blendEndMeters)
        {
            var outer = 1f - Smooth01(Mathf.Clamp01(distMeters / Mathf.Max(blendEndMeters, 0.01f)));
            var center = 1f - Smooth01(Mathf.Clamp01(distMeters / Mathf.Max(halfRoadMeters * 0.88f, 0.01f)));
            return center * outer;
        }

        public static float SampleEdgeMask(
            float distMeters,
            float halfRoadMeters,
            float packedEndMeters,
            float blendEndMeters)
        {
            var edgeStart = halfRoadMeters * 0.45f;
            var edge = Smooth01(InverseLerpClamped(edgeStart, packedEndMeters, distMeters));
            edge *= 1f - Smooth01(InverseLerpClamped(packedEndMeters, blendEndMeters, distMeters));
            var outer = 1f - Smooth01(Mathf.Clamp01(distMeters / Mathf.Max(blendEndMeters, 0.01f)));
            return edge * outer;
        }

        private static int EnsureLayerIndex(TerrainData data, TerrainLayer layer)
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

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static float InverseLerpClamped(float a, float b, float v)
        {
            if (Mathf.Abs(b - a) < 1e-5f)
            {
                return v >= b ? 1f : 0f;
            }

            return Mathf.Clamp01((v - a) / (b - a));
        }

        private static float Hash01(int n)
        {
            unchecked
            {
                var h = (uint)n;
                h ^= h >> 16;
                h *= 0x7feb352du;
                h ^= h >> 15;
                h *= 0x846ca68bu;
                h ^= h >> 16;
                return (h & 0x00FFFFFFu) / 16777215f;
            }
        }
    }
}
