using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// World-border ocean submersion only. Does NOT sink interior 2×2 tile seams.
    /// Island silhouette comes from <see cref="IslandNaturalGeography"/> soft framing.
    /// </summary>
    public static class IslandOccupancy
    {
        public static void ApplyIslandShape(UnityEngine.Terrain terrain, int seed)
        {
            ApplyBoundarySubmersion(terrain, seed);
        }

        public static void ApplyBoundarySubmersion(UnityEngine.Terrain terrain, int seed)
        {
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
            var maxBorder = sea01 - minSub01;

            var rng = new Random(seed);
            var ox = (float)rng.NextDouble() * 64f;
            var oz = (float)rng.NextDouble() * 64f;

            var worldX = WorldScale.WorldExtentXMeters;
            var worldZ = WorldScale.WorldExtentZMeters;
            var bandMeters = WorldScale.IslandOceanMarginMeters * 0.4f;

            for (var z = 0; z < res; z++)
            {
                var wz = pos.z + (z / (float)(res - 1)) * size.z;
                for (var x = 0; x < res; x++)
                {
                    var wx = pos.x + (x / (float)(res - 1)) * size.x;
                    var edgeDist = Mathf.Min(wx, wz, worldX - wx, worldZ - wz);
                    if (edgeDist > bandMeters)
                    {
                        continue;
                    }

                    var t = 1f - Mathf.Clamp01(edgeDist / bandMeters);
                    t = t * t * (3f - 2f * t);
                    var floorNoise =
                        (Mathf.PerlinNoise(wx * 0.04f + ox, wz * 0.04f + oz) - 0.5f) * 0.012f;
                    var target = Mathf.Clamp(shelfFloor01 + floorNoise, 0f, maxBorder);
                    heights[z, x] = Mathf.Lerp(heights[z, x], target, t);
                    if (edgeDist <= size.x / (res - 1) * 1.5f)
                    {
                        heights[z, x] = Mathf.Min(heights[z, x], maxBorder);
                    }
                }
            }

            TerrainHeightWrite.Commit(terrain, heights);
        }

        public static bool IsDryLand(
            UnityEngine.Terrain terrain,
            float worldX,
            float worldZ,
            float epsilon = 0.35f)
        {
            _ = terrain;
            var y = OverworldTerrainAccess.SampleHeightWorld(worldX, worldZ);
            return y > WorldScale.SeaLevelMeters + epsilon;
        }
    }
}
