using System;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Ocean-only water query: wet when terrain height is at or below Sea Level.
    /// </summary>
    public sealed class SeaLevelOceanSampler : IWaterSampler
    {
        private readonly Func<float, float, float> heightAtWorld;

        public SeaLevelOceanSampler(Func<float, float, float> heightAtWorld)
        {
            this.heightAtWorld = heightAtWorld;
        }

        public static SeaLevelOceanSampler FromTerrain(UnityEngine.Terrain terrain)
        {
            if (terrain == null || terrain.terrainData == null)
                return null;

            return new SeaLevelOceanSampler((x, z) =>
                terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y);
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
        {
            waterY = WorldScale.SeaLevelMeters;
            if (heightAtWorld == null)
                return false;
            return heightAtWorld(worldX, worldZ) <= waterY + 0.05f;
        }
    }
}
