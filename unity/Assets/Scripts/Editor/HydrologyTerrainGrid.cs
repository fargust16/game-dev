#if UNITY_EDITOR
using GameDev.World;
using GameDev.World.Hydrology;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Reads the baked terrain into the one height field the drainage analysis works on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every tile at once, in one array. Drainage is a global property — a river that rises on one
    /// tile and reaches the sea on another is the ordinary case — so analysing tiles separately
    /// would mean stitching basins back together afterwards, which is exactly the class of bug the
    /// project's single-tile assumptions produced elsewhere.
    /// </para>
    /// <para>
    /// Heights come from <c>terrainData.GetHeights</c>, once per tile. Sampling position by position
    /// through <c>OverworldTerrainAccess</c> would be correct and unusable: the road planner made
    /// 69 million such calls before it was changed to precompute, and this needs a million samples
    /// before it has even started.
    /// </para>
    /// <para>
    /// Neighbouring tiles share their edge row, so an <c>n</c>-resolution tile grid assembles into
    /// <c>tiles * (n - 1) + 1</c> samples with no duplicated column and no seam in the flow field.
    /// </para>
    /// </remarks>
    public static class HydrologyTerrainGrid
    {
        /// <summary>Builds the analysis grid, or null with a reason logged.</summary>
        public static HydrologyGrid Build(UnityEngine.Terrain[] terrains, int stride = 1)
        {
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("[Hydrology] No terrain in the scene — nothing to analyse.");
                return null;
            }

            var tileCountX = WorldScale.OverworldTileCountX;
            var tileCountZ = WorldScale.OverworldTileCountZ;
            var tileMeters = WorldScale.OverworldTileMeters;

            var tiles = new UnityEngine.Terrain[tileCountX * tileCountZ];
            var resolution = 0;

            for (var i = 0; i < terrains.Length; i++)
            {
                var terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                var position = terrain.transform.position;
                var tx = Mathf.RoundToInt(position.x / tileMeters);
                var tz = Mathf.RoundToInt(position.z / tileMeters);
                if (tx < 0 || tz < 0 || tx >= tileCountX || tz >= tileCountZ)
                {
                    Debug.LogWarning(
                        $"[Hydrology] Terrain '{terrain.name}' at {position} is outside the "
                        + $"{tileCountX}x{tileCountZ} world grid and was skipped.");
                    continue;
                }

                var tileResolution = terrain.terrainData.heightmapResolution;
                if (resolution == 0)
                {
                    resolution = tileResolution;
                }
                else if (tileResolution != resolution)
                {
                    Debug.LogError(
                        $"[Hydrology] Terrain '{terrain.name}' has heightmap resolution "
                        + $"{tileResolution} but another tile has {resolution}. The analysis needs "
                        + "one grid, so it cannot run until the tiles agree.");
                    return null;
                }

                tiles[tz * tileCountX + tx] = terrain;
            }

            if (resolution < 2)
            {
                Debug.LogError("[Hydrology] No usable terrain tiles were found.");
                return null;
            }

            for (var i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                {
                    Debug.LogError(
                        $"[Hydrology] Tile {i % tileCountX},{i / tileCountX} is missing. The "
                        + "analysis needs the whole world, not part of it.");
                    return null;
                }
            }

            stride = Mathf.Clamp(stride, 1, Mathf.Max(1, (resolution - 1) / 4));
            var perTile = (resolution - 1) / stride;
            var width = tileCountX * perTile + 1;
            var height = tileCountZ * perTile + 1;
            var cellSize = tileMeters / perTile;

            var heights = new float[width * height];

            for (var tz = 0; tz < tileCountZ; tz++)
            {
                for (var tx = 0; tx < tileCountX; tx++)
                {
                    var terrain = tiles[tz * tileCountX + tx];
                    var data = terrain.terrainData;
                    var sizeY = data.size.y;
                    var baseY = terrain.transform.position.y;
                    var raw = data.GetHeights(0, 0, resolution, resolution);

                    // Includes the shared edge row so the far edge of the world is written; the
                    // overlap simply rewrites an identical value where two tiles meet.
                    for (var z = 0; z <= perTile; z++)
                    {
                        var worldZ = tz * perTile + z;
                        if (worldZ >= height)
                        {
                            continue;
                        }

                        for (var x = 0; x <= perTile; x++)
                        {
                            var worldX = tx * perTile + x;
                            if (worldX >= width)
                            {
                                continue;
                            }

                            heights[worldZ * width + worldX] =
                                raw[z * stride, x * stride] * sizeY + baseY;
                        }
                    }
                }
            }

            return new HydrologyGrid(
                0f, 0f, width, height, cellSize, heights, WorldScale.SeaLevelMeters);
        }

        /// <summary>Builds the analysis grid from whatever terrain the scene currently holds.</summary>
        public static HydrologyGrid BuildFromScene(int stride = 1) =>
            Build(OverworldTerrainAccess.FindTerrains(), stride);
    }
}
#endif
