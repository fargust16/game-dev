using GameDev.World.Terrain;
using GameDev.World.Water;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Adapts the real biome/slope/water/height samplers into the plain
    /// <c>Func&lt;float,float,float&gt;</c> <see cref="RoadPathfinder.TryFindPath"/> expects,
    /// resolving <see cref="RoadCost.Evaluate"/>'s "is this point submerged" input by comparing
    /// real ground Y against the water surface (same check as
    /// <c>SettlementSiteValidator.IsPointDry</c>) rather than assuming a single global water plane.
    /// </summary>
    public sealed class RoadCostSampler
    {
        private readonly IBiomeSampler biome;
        private readonly ITerrainSlopeSampler slope;
        private readonly IWaterSampler water;
        private readonly IWorldHeightSampler height;

        public RoadCostSampler(IBiomeSampler biome, ITerrainSlopeSampler slope, IWaterSampler water, IWorldHeightSampler height)
        {
            this.biome = biome;
            this.slope = slope;
            this.water = water;
            this.height = height;
        }

        public float SampleCost(float worldX, float worldZ)
        {
            var biomeId = biome.SampleBiome(worldX, worldZ);
            var slope01 = slope.SampleSlope01(worldX, worldZ);
            return RoadCost.Evaluate(biomeId, slope01, IsSubmerged(worldX, worldZ));
        }

        private bool IsSubmerged(float worldX, float worldZ)
        {
            if (water == null || !water.TryGetWaterLevel(worldX, worldZ, out var waterY))
            {
                return false;
            }

            if (height == null || !height.TrySampleWorldY(worldX, worldZ, out var groundY))
            {
                return true;
            }

            return groundY <= waterY;
        }
    }
}
