using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Tile plumbing around Vista's GPU populate: heightmap read-back, resolution/size pinning and
    /// range logging.
    /// </summary>
    /// <remarks>
    /// Nothing here shapes terrain. Vista owns the heights; this only keeps the Unity-side tile
    /// container consistent and reports what Vista produced.
    /// </remarks>
    public static class TerrainHeightWrite
    {
        /// <summary>Detail map resolution and patch size for the overworld tiles.</summary>
        private const int DetailResolution = 512;
        private const int DetailResolutionPerPatch = 16;

        /// <summary>
        /// Set heightmap resolution then pin world metres. Resolution-first avoids Unity
        /// preserving old metres-per-pixel and exploding tile size (1400→22400).
        /// </summary>
        /// <remarks>
        /// Safe to call after generation: every setter here is guarded against a no-op write.
        /// That guard is not cosmetic — <c>TerrainData.SetDetailResolution</c> and the
        /// <c>heightmapResolution</c> setter both <b>discard existing data</b>. Calling them
        /// unconditionally after Vista finished erased the detail layers it had just written, which
        /// is why grass never appeared: the graph produced it and this wiped it a moment later.
        /// </remarks>
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

            if (data.detailResolution != DetailResolution ||
                data.detailResolutionPerPatch != DetailResolutionPerPatch)
            {
                data.SetDetailResolution(DetailResolution, DetailResolutionPerPatch);
            }

            var size = new Vector3(
                WorldScale.OverworldTileMeters,
                WorldScale.TerrainHeightBudgetMeters,
                WorldScale.OverworldTileMeters);
            if (data.size != size)
            {
                data.size = size;
            }
        }

        /// <summary>Pull Vista's GPU heightmap into the CPU copy so reads see the real terrain.</summary>
        public static void SyncFromGpu(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return;
            }

            terrain.terrainData.SyncHeightmap();
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
