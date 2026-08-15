using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Scene lookup for overworld Unity Terrains owned by Vista (1..N tiles in a grid).
    /// </summary>
    public static class OverworldTerrainAccess
    {
        public const string TerrainsRootName = "Terrains";
        public const string VistaManagerName = "VistaManager";

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

                if (t.GetComponent<Pinwheel.Vista.UnityTerrain.TerrainTile>() != null ||
                    (t.transform.parent != null && t.transform.parent.name == TerrainsRootName))
                {
                    list.Add(t);
                }
            }

            if (list.Count == 0)
            {
                return all;
            }

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

        public static VistaTerrainSampler CreateSampler(int moistureSeed = 0)
        {
            var terrain = FindTerrain();
            return terrain != null ? new VistaTerrainSampler(terrain, moistureSeed) : null;
        }
    }
}
