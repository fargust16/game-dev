using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Presses a valley into a Unity <see cref="UnityEngine.Terrain"/>'s heightmap along a set of
    /// <see cref="RiverPath"/> centerlines, and optionally paints a bank texture layer over the same
    /// footprint - same technique as <see cref="GameDev.World.Roads.RoadTerrainCarver"/> (read the
    /// whole map once, apply every point's contribution to an in-memory copy measured from one
    /// consistent original, write back once), kept as its own type rather than a shared one because
    /// a river's width and carve depth vary continuously along its length (source trickle to river
    /// mouth) where a road's don't - <see cref="GameDev.World.Roads.RoadTerrainCarver"/>'s call
    /// shape (one constant width/depth per call) has no way to express that.
    /// <para>
    /// Same known limitation as the road carver: neither pass remembers what the terrain looked
    /// like before the first carve, so carving twice without regenerating terrain in between digs
    /// twice. Regenerating terrain (<c>OverworldTerrainMenu</c>) is the reliable way to get a clean
    /// result.
    /// </para>
    /// </summary>
    public static class RiverTerrainCarver
    {
        private const int MinRadiusPixels = 4;

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

            // TerrainCollider can lag a direct SetHeights/SetAlphamaps call - reassigning its
            // terrainData forces an immediate resync, so anything that raycasts this terrain right
            // after Carve() (lake mesh building, river mesh building) hits the carved surface.
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

            var terrainPos = terrain.transform.position;
            var size = data.size;
            var worldPerPixelX = size.x / (resolution - 1);
            var worldPerPixelZ = size.z / (resolution - 1);

            foreach (var river in rivers)
            {
                for (var i = 0; i < river.Centerline.Count; i++)
                {
                    var point = river.Centerline[i];
                    var halfWidth = river.Widths[i] * 0.5f;
                    var depthNormalized = river.CarveDepths[i] / Mathf.Max(size.y, 0.01f);
                    var radiusPixelsX = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelX) + 1);
                    var radiusPixelsZ = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelZ) + 1);

                    CarveHeightPoint(
                        heights, original, resolution, resolution,
                        terrainPos, worldPerPixelX, worldPerPixelZ,
                        new Vector2(point.x, point.z), halfWidth, radiusPixelsX, radiusPixelsZ, depthNormalized);
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
                    // at halfWidth, so the valley blends into the surrounding bank instead of a
                    // hard-edged trench.
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

        private static void PaintBanks(
            UnityEngine.Terrain terrain, IReadOnlyList<RiverPath> rivers, TerrainLayer bankLayer, float widthScale)
        {
            var data = terrain.terrainData;

            // Must happen before GetAlphamaps - adding a layer resizes the alphamap's layer
            // dimension, and fetching first would hand back a stale, one-layer-short array.
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
                for (var i = 0; i < river.Centerline.Count; i++)
                {
                    var point = river.Centerline[i];
                    var halfWidth = river.Widths[i] * widthScale * 0.5f;
                    var radiusPixelsX = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelX) + 1);
                    var radiusPixelsZ = Mathf.Max(MinRadiusPixels, Mathf.CeilToInt(halfWidth / worldPerPixelZ) + 1);

                    AccumulateInfluence(
                        influence, resolution, terrainPos, worldPerPixelX, worldPerPixelZ,
                        new Vector2(point.x, point.z), halfWidth, radiusPixelsX, radiusPixelsZ);
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
