using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>One worn-path stroke: densified segment carve + optional dirt alphamap paint.</summary>
    public readonly struct RoadCarveStroke
    {
        public IReadOnlyList<Vector2> Centerline { get; }
        public float CarveWidthMeters { get; }
        public float CarveDepthMeters { get; }
        public float PaintWidthMeters { get; }

        public RoadCarveStroke(
            IReadOnlyList<Vector2> centerline,
            float carveWidthMeters,
            float carveDepthMeters,
            float paintWidthMeters = 0f)
        {
            Centerline = centerline;
            CarveWidthMeters = carveWidthMeters;
            CarveDepthMeters = carveDepthMeters;
            PaintWidthMeters = paintWidthMeters > 0f ? paintWidthMeters : carveWidthMeters;
        }
    }

    /// <summary>
    /// Cuts a continuous worn corridor into terrain along road centerlines: linear grade between
    /// stroke ends (flatten hills), plus a shallow groove, dig-only (never raise). Dirt alphamap paint optional.
    /// </summary>
    public static class RoadTerrainCarver
    {
        private const int MinRadiusPixels = 4;
        public const float DensifyStepMeters = 2.5f;
        private const int CorridorSoftenPasses = 2;

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<IReadOnlyList<Vector2>> centerlines,
            float carveWidthMeters,
            float carveDepthMeters,
            TerrainLayer grooveLayer = null,
            float paintWidthMeters = 0f)
        {
            var strokes = new List<RoadCarveStroke>();
            foreach (var line in centerlines)
            {
                strokes.Add(new RoadCarveStroke(line, carveWidthMeters, carveDepthMeters, paintWidthMeters));
            }

            Carve(terrain, strokes, grooveLayer);
        }

        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<RoadCarveStroke> strokes,
            TerrainLayer grooveLayer = null)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var pinned = new List<RoadCarveStroke>(strokes);
            if (pinned.Count == 0)
            {
                return;
            }

            CarveHeights(terrain, pinned);

            if (grooveLayer != null)
            {
                PaintGroove(terrain, pinned, grooveLayer);
            }

            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrain.terrainData;
            }
        }

        private static void CarveHeights(UnityEngine.Terrain terrain, IReadOnlyList<RoadCarveStroke> strokes)
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

            foreach (var stroke in strokes)
            {
                var dense = DensifyPolyline(stroke.Centerline, DensifyStepMeters);
                if (dense.Count < 2)
                {
                    continue;
                }

                var halfW = Mathf.Max(stroke.CarveWidthMeters * 0.5f, 1f);
                var depth = Mathf.Max(stroke.CarveDepthMeters, 0.05f);

                // Linear design grade between stroke ends — cut hills down to that plane.
                SampleHeightNorm(original, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    dense[0], out var y0Norm);
                SampleHeightNorm(original, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                    dense[dense.Count - 1], out var y1Norm);

                var totalLen = 0f;
                var cumLen = new float[dense.Count];
                cumLen[0] = 0f;
                for (var i = 1; i < dense.Count; i++)
                {
                    totalLen += Vector2.Distance(dense[i - 1], dense[i]);
                    cumLen[i] = totalLen;
                }

                if (totalLen < 0.01f)
                {
                    continue;
                }

                for (var i = 0; i < dense.Count - 1; i++)
                {
                    var t0 = cumLen[i] / totalLen;
                    var t1 = cumLen[i + 1] / totalLen;
                    var grade0 = Mathf.Lerp(y0Norm, y1Norm, t0);
                    var grade1 = Mathf.Lerp(y0Norm, y1Norm, t1);
                    CarveHeightSegment(
                        heights, original, carvedMask, resolution,
                        terrainPos, worldPerPixelX, worldPerPixelZ, worldHeight,
                        dense[i], dense[i + 1], halfW, depth, grade0, grade1);
                }
            }

            SoftenCarvedCorridor(heights, carvedMask, resolution, CorridorSoftenPasses);
            // Dig-only after soften — blur must not raise above original.
            for (var pz = 0; pz < resolution; pz++)
            {
                for (var px = 0; px < resolution; px++)
                {
                    if (carvedMask[pz, px] && heights[pz, px] > original[pz, px])
                    {
                        heights[pz, px] = original[pz, px];
                    }
                }
            }

            data.SetHeights(0, 0, heights);
        }

        public static List<Vector2> DensifyPolyline(IReadOnlyList<Vector2> source, float stepMeters)
        {
            var result = new List<Vector2>();
            if (source == null || source.Count == 0)
            {
                return result;
            }

            result.Add(source[0]);
            var step = Mathf.Max(stepMeters, 0.5f);
            for (var i = 1; i < source.Count; i++)
            {
                var a = source[i - 1];
                var b = source[i];
                var flat = Vector2.Distance(a, b);
                var segments = Mathf.Max(1, Mathf.CeilToInt(flat / step));
                for (var s = 1; s <= segments; s++)
                {
                    result.Add(Vector2.Lerp(a, b, s / (float)segments));
                }
            }

            return result;
        }

        private static void SampleHeightNorm(
            float[,] original,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            Vector2 worldXZ,
            out float heightNorm)
        {
            var px = Mathf.Clamp(
                Mathf.RoundToInt((worldXZ.x - terrainPos.x) / worldPerPixelX), 0, resolution - 1);
            var pz = Mathf.Clamp(
                Mathf.RoundToInt((worldXZ.y - terrainPos.z) / worldPerPixelZ), 0, resolution - 1);
            heightNorm = original[pz, px];
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
            float halfWidth,
            float depthMeters,
            float gradeNormA,
            float gradeNormB)
        {
            var pad = halfWidth + worldPerPixelX * 2f;
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

            var depthNorm = depthMeters / worldHeight;

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
                    if (dist > halfWidth)
                    {
                        continue;
                    }

                    var u = dist / Mathf.Max(halfWidth, 1e-3f);
                    var radial = u * u * (3f - 2f * u);
                    // Flatter worn tread: more depth across the centre strip than a sharp V.
                    var centreFlat = 1f - Mathf.SmoothStep(0.25f, 1f, u);
                    var profile = Mathf.Max(1f - radial, centreFlat * 0.9f);

                    var gradeNorm = Mathf.Lerp(gradeNormA, gradeNormB, t);
                    // Cut-to-grade: flatten hills to the design plane, then press the groove.
                    var targetNorm = gradeNorm - depthNorm * profile;
                    var candidate = Mathf.Min(original[pz, px], targetNorm);
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

                        var sum = 0f;
                        var count = 0;
                        for (var dz = -1; dz <= 1; dz++)
                        {
                            for (var dx = -1; dx <= 1; dx++)
                            {
                                var nx = px + dx;
                                var nz = pz + dz;
                                if (nx < 0 || nz < 0 || nx >= resolution || nz >= resolution)
                                {
                                    continue;
                                }

                                sum += heights[nz, nx];
                                count++;
                            }
                        }

                        scratch[pz, px] = count > 0 ? sum / count : heights[pz, px];
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

        private static void PaintGroove(
            UnityEngine.Terrain terrain,
            IReadOnlyList<RoadCarveStroke> strokes,
            TerrainLayer grooveLayer)
        {
            var data = terrain.terrainData;
            var layerIndex = EnsureLayer(data, grooveLayer);

            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);
            var influence = new float[resolution, resolution];

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;

            foreach (var stroke in strokes)
            {
                var dense = DensifyPolyline(stroke.Centerline, DensifyStepMeters);
                var halfWidth = Mathf.Max(stroke.PaintWidthMeters * 0.5f, 1f);
                var radiusPixelsX = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelX) + 1);
                var radiusPixelsZ = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelZ) + 1);

                foreach (var point in dense)
                {
                    AccumulateInfluence(
                        influence, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        point, halfWidth, radiusPixelsX, radiusPixelsZ);
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

        private static void AccumulateInfluence(
            float[,] influence,
            int resolution,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            Vector2 worldPoint,
            float halfWidth,
            int radiusPixelsX,
            int radiusPixelsZ)
        {
            var localX = (worldPoint.x - terrainPos.x) / worldPerPixelX;
            var localZ = (worldPoint.y - terrainPos.z) / worldPerPixelZ;
            var centerPx = Mathf.RoundToInt(localX);
            var centerPz = Mathf.RoundToInt(localZ);

            for (var dz = -radiusPixelsZ; dz <= radiusPixelsZ; dz++)
            {
                var pz = centerPz + dz;
                if (pz < 0 || pz >= resolution)
                {
                    continue;
                }

                for (var dx = -radiusPixelsX; dx <= radiusPixelsX; dx++)
                {
                    var px = centerPx + dx;
                    if (px < 0 || px >= resolution)
                    {
                        continue;
                    }

                    var worldDx = (px - localX) * worldPerPixelX;
                    var worldDz = (pz - localZ) * worldPerPixelZ;
                    var dist = Mathf.Sqrt(worldDx * worldDx + worldDz * worldDz);
                    if (dist > halfWidth)
                    {
                        continue;
                    }

                    var t = dist / halfWidth;
                    var smooth = t * t * (3f - 2f * t);
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
