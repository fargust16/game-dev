using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Presses a shallow, smooth-edged groove into a Unity <see cref="UnityEngine.Terrain"/>'s
    /// heightmap along a set of road centerlines, and optionally paints a matching "worn dirt"
    /// texture layer into the alphamap over the same footprint - the "worn cart track" look
    /// (WORLD_GENERATION_PIPELINE.md Iteration 09): centuries of wheels and hooves settle a road
    /// into the ground and wear the grass away, rather than leaving a ribbon resting on top of
    /// undisturbed ground. Both passes read the whole map once, apply every centerline's
    /// contribution to an in-memory copy, then write back once - point-by-point
    /// <c>TerrainData.SetHeights</c>/<c>SetAlphamaps</c> calls would be both slow (each can trigger a
    /// sync) and wrong within a single call (each successive point would measure against the
    /// already-modified ground instead of the original).
    /// <para>
    /// Known limitation: neither pass has any memory of what the terrain looked like before the
    /// first carve, so calling <see cref="Carve"/> again on an already-carved terrain (e.g. running
    /// <c>Build Road Network</c> twice without regenerating terrain in between) carves/paints a
    /// second time on top of the first rather than replacing it. Regenerating terrain
    /// (<c>OverworldTerrainMenu</c>) always produces a fresh heightmap/alphamap from the graph, so
    /// that's the reliable way to get a clean result.
    /// </para>
    /// </summary>
    public static class RoadTerrainCarver
    {
        // Never sample fewer than this many pixels of radius, regardless of how wide the caller
        // asked for relative to the map's actual resolution - a coarse heightmap/alphamap (a few
        // metres per texel) could otherwise resolve a "wide" groove down to a single pixel, which
        // reads as a barely-there dimple instead of a real trench no matter how deep it's set.
        private const int MinRadiusPixels = 4;

        /// <param name="grooveLayer">Terrain layer painted into the alphamap along the same
        /// footprint as the height carve (e.g. a dirt layer, so the groove reads as trampled ground,
        /// not just a dip in the grass). Pass null to skip texture painting and only carve height.</param>
        /// <param name="paintWidthMeters">Width of the alphamap paint - independent of
        /// <paramref name="carveWidthMeters"/> so the visible "bare dirt" strip can be narrower or
        /// wider than the height groove's own banks.</param>
        public static void Carve(
            UnityEngine.Terrain terrain,
            IEnumerable<IReadOnlyList<Vector2>> centerlines,
            float carveWidthMeters,
            float carveDepthMeters,
            TerrainLayer grooveLayer = null,
            float paintWidthMeters = 0f)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            // Centerlines is a lazily-enumerable sequence in general - both passes need to walk it
            // fully and independently, so pin it to a list once up front instead of re-enumerating
            // (or worse, exhausting) whatever the caller passed in.
            var pinned = new List<IReadOnlyList<Vector2>>(centerlines);

            CarveHeights(terrain, pinned, carveWidthMeters, carveDepthMeters);

            if (grooveLayer != null)
            {
                PaintGroove(terrain, pinned, paintWidthMeters > 0f ? paintWidthMeters : carveWidthMeters, grooveLayer);
            }

            // TerrainCollider can lag a direct SetHeights/SetAlphamaps call - reassigning its
            // terrainData forces an immediate resync, so a raycast against this terrain right after
            // Carve() (as RoadNetworkMenu's mesh-building pass does) hits the newly-carved surface,
            // not the stale pre-carve one.
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrain.terrainData;
            }
        }

        private static void CarveHeights(
            UnityEngine.Terrain terrain,
            IReadOnlyList<IReadOnlyList<Vector2>> centerlines,
            float carveWidthMeters,
            float carveDepthMeters)
        {
            var data = terrain.terrainData;
            var resolution = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, resolution, resolution);
            var original = (float[,])heights.Clone();

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var halfWidth = carveWidthMeters * 0.5f;
            var depthNormalized = carveDepthMeters / Mathf.Max(size.y, 0.01f);

            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);
            var radiusPixelsX = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelX) + 1);
            var radiusPixelsZ = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelZ) + 1);

            foreach (var centerline in centerlines)
            {
                foreach (var point in centerline)
                {
                    CarveHeightPoint(
                        heights, original, resolution, resolution,
                        terrainPos, worldPerPixelX, worldPerPixelZ,
                        point, halfWidth, radiusPixelsX, radiusPixelsZ, depthNormalized);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static void CarveHeightPoint(
            float[,] heights,
            float[,] original,
            int resolutionX,
            int resolutionZ,
            Vector3 terrainPos,
            float worldPerPixelX,
            float worldPerPixelZ,
            Vector2 worldPoint,
            float halfWidth,
            int radiusPixelsX,
            int radiusPixelsZ,
            float depthNormalized)
        {
            var localX = (worldPoint.x - terrainPos.x) / worldPerPixelX;
            var localZ = (worldPoint.y - terrainPos.z) / worldPerPixelZ;
            var centerPx = Mathf.RoundToInt(localX);
            var centerPz = Mathf.RoundToInt(localZ);

            for (var dz = -radiusPixelsZ; dz <= radiusPixelsZ; dz++)
            {
                var pz = centerPz + dz;
                if (pz < 0 || pz >= resolutionZ)
                {
                    continue;
                }

                for (var dx = -radiusPixelsX; dx <= radiusPixelsX; dx++)
                {
                    var px = centerPx + dx;
                    if (px < 0 || px >= resolutionX)
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

                    // Smoothstep falloff: full depth at the centerline, tapering to zero extra depth
                    // at halfWidth, so the groove blends into the surrounding ground instead of
                    // leaving a hard-edged trench.
                    var t = dist / halfWidth;
                    var smooth = t * t * (3f - 2f * t);
                    var depthHere = depthNormalized * (1f - smooth);

                    // heights/original are indexed [z, x] (Unity's TerrainData.GetHeights convention).
                    var candidate = original[pz, px] - depthHere;
                    if (candidate < heights[pz, px])
                    {
                        heights[pz, px] = Mathf.Clamp01(candidate);
                    }
                }
            }
        }

        private static void PaintGroove(
            UnityEngine.Terrain terrain,
            IReadOnlyList<IReadOnlyList<Vector2>> centerlines,
            float paintWidthMeters,
            TerrainLayer grooveLayer)
        {
            var data = terrain.terrainData;

            // Must happen before GetAlphamaps - adding a layer resizes the alphamap's layer
            // dimension, and fetching first would hand back a stale, one-layer-short array.
            var layerIndex = EnsureLayer(data, grooveLayer);

            var resolution = data.alphamapResolution;
            var alphamaps = data.GetAlphamaps(0, 0, resolution, resolution);
            var layerCount = alphamaps.GetLength(2);

            // Desired groove-layer weight per pixel, the max across every centerline point that
            // reaches it - computed once before touching alphamaps so multiple nearby points (or a
            // road curving back near itself) don't compound into over-strong painting.
            var influence = new float[resolution, resolution];

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var halfWidth = paintWidthMeters * 0.5f;
            var worldPerPixelX = size.x / resolution;
            var worldPerPixelZ = size.z / resolution;
            var radiusPixelsX = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelX) + 1);
            var radiusPixelsZ = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelZ) + 1);

            foreach (var centerline in centerlines)
            {
                foreach (var point in centerline)
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

                    // Scale every OTHER layer down proportionally so the total stays 1 - the groove
                    // layer eats into whatever was there (grass, forest floor, etc.) rather than
                    // just adding on top and pushing the total over 1.
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
