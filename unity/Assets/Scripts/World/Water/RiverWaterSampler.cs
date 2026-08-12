using System.Collections.Generic;

namespace GameDev.World.Water
{
    /// <summary>Pure <see cref="IWaterSampler"/> over a fixed <see cref="RiverPath"/> list - mirrors
    /// <see cref="WaterZoneSampler"/>, no scene lookups. Each <see cref="RiverPath"/> already
    /// rasterizes itself to an O(1)-lookup grid, so this is just "ask every river in turn".</summary>
    public sealed class RiverWaterSampler : IWaterSampler
    {
        private readonly IReadOnlyList<RiverPath> rivers;

        public RiverWaterSampler(IReadOnlyList<RiverPath> rivers)
        {
            this.rivers = rivers;
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
        {
            for (var i = 0; i < rivers.Count; i++)
            {
                if (rivers[i].TryGetWaterLevel(worldX, worldZ, out waterY))
                {
                    return true;
                }
            }

            waterY = 0f;
            return false;
        }
    }
}
