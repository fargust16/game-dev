using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Scene lookup for the overworld Unity Terrain tiles (1..N in a grid).
    /// </summary>
    /// <remarks>
    /// The canonical terrain access point for every downstream system: roads, POI placement,
    /// settlements, biome classification, water and hydrology analysis all reach the ground through
    /// here. It answers in terms of <c>UnityEngine.Terrain</c> and <c>TerrainData</c> only, so it
    /// does not care which tool filled the heightmap - see <see cref="OverworldTerrainTile"/>.
    /// </remarks>
    public static class OverworldTerrainAccess
    {
        public const string TerrainsRootName = "Terrains";

        public static UnityEngine.Terrain FindTerrain()
        {
            var terrains = FindTerrains();
            if (terrains == null || terrains.Length == 0)
            {
                return null;
            }

            // Prefer the tile closest to world origin (SW of the 2×2 grid).
            UnityEngine.Terrain best = null;
            var bestScore = float.MaxValue;
            for (var i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if (t == null)
                {
                    continue;
                }

                var p = t.transform.position;
                var score = p.x * p.x + p.z * p.z;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            return best;
        }

        public static UnityEngine.Terrain[] FindTerrains()
        {
            var all = Object.FindObjectsByType<UnityEngine.Terrain>();
            if (all == null || all.Length == 0)
            {
                return System.Array.Empty<UnityEngine.Terrain>();
            }

            var list = new System.Collections.Generic.List<UnityEngine.Terrain>(all.Length);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                {
                    continue;
                }

                // Membership of the overworld grid, not authorship of its heights. The grid stamps
                // OverworldTerrainTile on every tile, and the parent-name rule also recognises tiles
                // created before the marker existed.
                if (t.GetComponent<OverworldTerrainTile>() != null ||
                    (t.transform.parent != null && t.transform.parent.name == TerrainsRootName))
                {
                    list.Add(t);
                }
            }

            if (list.Count == 0)
            {
                return all;
            }

            // FindObjectsByType returns scene order, which is not guaranteed to be stable between
            // runs. The terrain itself never depended on it - five identical bakes produced
            // byte-identical heightmaps - but the diagnostic tile index did, so the tileN_ prefix
            // on a dump named a different tile from one bake to the next. Comparing two bakes by
            // that prefix therefore compared two different tiles, which is what produced the
            // "same seed, 93.3 % then 99.8 %" reading: those were tiles 0_1 and 1_0, not one tile
            // changing. Sorting by name makes every caller's ordering reproducible.
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list.ToArray();
        }

        public static float HeightBudgetMeters(UnityEngine.Terrain terrain)
        {
            if (terrain != null && terrain.terrainData != null)
            {
                return terrain.terrainData.size.y;
            }

            return WorldScale.TerrainHeightBudgetMeters;
        }

        public static float TileSizeMeters(UnityEngine.Terrain terrain)
        {
            if (terrain != null && terrain.terrainData != null)
            {
                return terrain.terrainData.size.x;
            }

            return WorldScale.OverworldTileMeters;
        }

        public static float WorldExtentXMeters() => WorldScale.WorldExtentXMeters;

        public static float WorldExtentZMeters() => WorldScale.WorldExtentZMeters;

        /// <summary>
        /// Finds the tile containing a world XZ point, or null when the point is off the grid.
        /// </summary>
        public static UnityEngine.Terrain FindTileAt(float worldX, float worldZ)
        {
            var terrains = FindTerrains();
            for (var i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if (t == null || t.terrainData == null)
                {
                    continue;
                }

                var pos = t.transform.position;
                var size = t.terrainData.size;
                if (worldX < pos.x || worldZ < pos.z ||
                    worldX > pos.x + size.x || worldZ > pos.z + size.z)
                {
                    continue;
                }

                return t;
            }

            return null;
        }

        /// <summary>
        /// Height at a world XZ point, reporting whether the point is on the terrain grid at all.
        /// </summary>
        /// <remarks>
        /// The world is 2x2 tiles, so anything that bounds-checks against a single tile silently
        /// discards three quarters of it — that is what dropped 27 of 32 POI markers as "no
        /// surface". Prefer this over <see cref="SampleHeightWorld"/> wherever "off the grid" and
        /// "at height zero" mean different things.
        /// </remarks>
        public static bool TrySampleHeightWorld(float worldX, float worldZ, out float worldY)
        {
            var terrain = FindTileAt(worldX, worldZ);
            if (terrain == null)
            {
                worldY = 0f;
                return false;
            }

            worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;
            return true;
        }

        /// <summary>Sample height in world XZ across the tile grid.</summary>
        public static float SampleHeightWorld(float worldX, float worldZ)
        {
            var terrains = FindTerrains();
            for (var i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if (t == null || t.terrainData == null)
                {
                    continue;
                }

                var pos = t.transform.position;
                var size = t.terrainData.size;
                if (worldX < pos.x || worldZ < pos.z ||
                    worldX > pos.x + size.x || worldZ > pos.z + size.z)
                {
                    continue;
                }

                return t.SampleHeight(new Vector3(worldX, 0f, worldZ)) + pos.y;
            }

            var fallback = FindTerrain();
            if (fallback == null)
            {
                return 0f;
            }

            return fallback.SampleHeight(new Vector3(worldX, 0f, worldZ)) +
                   fallback.transform.position.y;
        }

        /// <summary>
        /// Sampler over the whole tile grid, or null when no terrain exists yet.
        /// </summary>
        /// <remarks>
        /// The terrain passed in is only a fallback for points off the grid;
        /// <see cref="OverworldTerrainSampler"/> resolves the containing tile for every query.
        /// </remarks>
        public static OverworldTerrainSampler CreateSampler(int moistureSeed = 0)
        {
            var terrain = FindTerrain();
            return terrain != null ? new OverworldTerrainSampler(terrain, moistureSeed) : null;
        }
    }
}
