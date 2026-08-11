using System.Collections.Generic;

namespace GameDev.World.Water
{
    /// <summary>Pure <see cref="IWaterSampler"/> over a fixed zone list - no scene lookups.</summary>
    public sealed class WaterZoneSampler : IWaterSampler
    {
        private readonly IReadOnlyList<WaterZone> zones;

        public WaterZoneSampler(IReadOnlyList<WaterZone> zones)
        {
            this.zones = zones;
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.Contains(worldX, worldZ))
                {
                    waterY = zone.WaterLevelY;
                    return true;
                }
            }

            waterY = 0f;
            return false;
        }
    }
}
