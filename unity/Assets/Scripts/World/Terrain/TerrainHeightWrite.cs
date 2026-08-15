using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// CPU heightmap writes after Vista GPU populate (<c>SyncControl.None</c>).
    /// Sync first so queued GPU→CPU copies cannot wipe later <see cref="TerrainData.SetHeights"/>.
    /// </summary>
    public static class TerrainHeightWrite
    {
        public static void ForceBudgetSize(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var data = terrain.terrainData;
            var size = data.size;
            // Unity keeps heightmapScale when resolution changes, so size can inflate
            // (e.g. 1400@33 → 22400@513). Always pin overworld tile XZ + vertical budget.
            var needXz =
                Mathf.Abs(size.x - WorldScale.OverworldTileMeters) > 0.01f ||
                Mathf.Abs(size.z - WorldScale.OverworldTileMeters) > 0.01f;
            var needY = Mathf.Abs(size.y - WorldScale.TerrainHeightBudgetMeters) > 0.01f;
            if (needXz || needY)
            {
                data.size = new Vector3(
                    WorldScale.OverworldTileMeters,
                    WorldScale.TerrainHeightBudgetMeters,
                    WorldScale.OverworldTileMeters);
            }
        }

        /// <summary>
        /// Set heightmap resolution then pin world metres. Resolution-first avoids Unity
        /// preserving old metres-per-pixel and exploding tile size (1400→22400).
        /// </summary>
        public static void ForceTileResolutionAndSize(UnityEngine.Terrain terrain, int heightmapResolution)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            var data = terrain.terrainData;
            if (data.heightmapResolution != heightmapResolution)
            {
                data.heightmapResolution = heightmapResolution;
            }

            data.SetDetailResolution(512, 16);
            data.size = new Vector3(
                WorldScale.OverworldTileMeters,
                WorldScale.TerrainHeightBudgetMeters,
                WorldScale.OverworldTileMeters);
        }

        /// <summary>
        /// Pull any pending Vista GPU height into CPU before we overwrite with SetHeights.
        /// </summary>
        public static void SyncFromGpu(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            terrain.terrainData.SyncHeightmap();
        }

        public static void Commit(UnityEngine.Terrain terrain, float[,] heights)
        {
            if (terrain == null || terrain.terrainData == null || heights == null)
            {
                return;
            }

            var data = terrain.terrainData;
            data.SetHeights(0, 0, heights);
            data.SyncHeightmap();
            var collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = data;
            }
        }

        public static void LogRange(UnityEngine.Terrain terrain, string step)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                Debug.LogWarning($"[Overworld] height[{step}]: no terrain");
                return;
            }

            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);
            var min01 = 1f;
            var max01 = 0f;
            var aboveSea = 0;
            var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, data.size.y));
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var h = heights[z, x];
                    if (h < min01)
                    {
                        min01 = h;
                    }

                    if (h > max01)
                    {
                        max01 = h;
                    }

                    if (h > sea01)
                    {
                        aboveSea++;
                    }
                }
            }

            var pos = terrain.transform.position;
            var size = data.size;
            var total = res * res;
            var landPct = 100f * aboveSea / Mathf.Max(1, total);
            var minY = min01 * size.y;
            var maxY = max01 * size.y;
            Debug.Log(
                $"[Overworld] height[{step}]: size=({size.x:F0},{size.y:F0},{size.z:F0})m " +
                $"pos=({pos.x:F0},{pos.z:F0}) 01=[{min01:F3}…{max01:F3}] " +
                $"worldY=[{minY:F1}…{maxY:F1}]m landAboveSea={landPct:F1}% (sea={WorldScale.SeaLevelMeters}m)");
        }
    }
}
