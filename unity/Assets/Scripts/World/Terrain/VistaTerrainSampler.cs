using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// <see cref="IBiomeSampler"/> backed by the Vista Unity Terrain grid (1..N tiles).
    /// Height for classification is sea-relative 0..1 (ocean floor does not steal Swamp band).
    /// </summary>
    public sealed class VistaTerrainSampler : IBiomeSampler, ITerrainSlopeSampler
    {
        private readonly UnityEngine.Terrain primaryTerrain;
        private readonly float landBudget;

        public VistaTerrainSampler(UnityEngine.Terrain terrain, int moistureSeed = 0)
        {
            primaryTerrain = terrain;
            landBudget = Mathf.Max(1f, WorldScale.TerrainHeightBudgetMeters - WorldScale.SeaLevelMeters);
            _ = moistureSeed;
        }

        public float SampleWorldHeight(float worldX, float worldZ)
        {
            return OverworldTerrainAccess.SampleHeightWorld(worldX, worldZ);
        }

        /// <summary>Sea-relative height 0..1 for <see cref="BiomeClassifier"/>.</summary>
        public float SampleHeight(float worldX, float worldZ)
        {
            var worldY = SampleWorldHeight(worldX, worldZ);
            return Mathf.Clamp01((worldY - WorldScale.SeaLevelMeters) / landBudget);
        }

        public float SampleSlope01(float worldX, float worldZ)
        {
            var terrain = FindTile(worldX, worldZ) ?? primaryTerrain;
            if (terrain == null || terrain.terrainData == null)
            {
                return 0f;
            }

            var data = terrain.terrainData;
            var local = new Vector3(worldX, 0f, worldZ) - terrain.transform.position;
            var u = Mathf.Clamp01(local.x / data.size.x);
            var v = Mathf.Clamp01(local.z / data.size.z);
            var normal = data.GetInterpolatedNormal(u, v);
            var slope = 1f - Mathf.Clamp01(normal.y);
            return slope;
        }

        public BiomeId SampleBiome(float worldX, float worldZ)
        {
            var worldY = SampleWorldHeight(worldX, worldZ);
            if (worldY <= WorldScale.SeaLevelMeters + 0.35f)
            {
                return BiomeId.Swamp;
            }

            var height01 = SampleHeight(worldX, worldZ);
            var slope01 = SampleSlope01(worldX, worldZ);
            var moisture01 = SampleMoisture01(worldX, worldZ);
            return BiomeClassifier.Classify(height01, slope01, moisture01);
        }

        public float SampleMoisture01(float worldX, float worldZ)
        {
            var nx = worldX * 0.0017f;
            var nz = worldZ * 0.0017f;
            var n = Mathf.PerlinNoise(nx + 17.1f, nz + 9.3f);
            return Mathf.Clamp01(n);
        }

        private static UnityEngine.Terrain FindTile(float worldX, float worldZ)
        {
            var terrains = OverworldTerrainAccess.FindTerrains();
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
    }
}
