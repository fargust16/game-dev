using GameDev.World.Roads;
using GameDev.World.Terrain;
using GameDev.World.Water;
using NUnit.Framework;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadCostSamplerTests
    {
        private sealed class FixedSampler : IBiomeSampler, ITerrainSlopeSampler
        {
            public BiomeId Biome;
            public float Slope;

            public float SampleHeight(float worldX, float worldZ) => 0f;
            public float SampleSlope01(float worldX, float worldZ) => Slope;
            public BiomeId SampleBiome(float worldX, float worldZ) => Biome;
        }

        private sealed class FixedWaterSampler : IWaterSampler
        {
            public bool HasWater;
            public float WaterY;
            public bool TryGetWaterLevel(float worldX, float worldZ, out float waterY)
            {
                waterY = WaterY;
                return HasWater;
            }
        }

        private sealed class FixedHeightSampler : IWorldHeightSampler
        {
            public bool HasGround = true;
            public float GroundY;
            public bool TrySampleWorldY(float worldX, float worldZ, out float worldY)
            {
                worldY = GroundY;
                return HasGround;
            }
        }

        [Test]
        public void SampleCost_NoWaterAtPoint_UsesBiomeAndSlopeOnly()
        {
            var biome = new FixedSampler { Biome = BiomeId.Meadow, Slope = 0f };
            var water = new FixedWaterSampler { HasWater = false };
            var height = new FixedHeightSampler { GroundY = 10f };
            var sampler = new RoadCostSampler(biome, biome, water, height);

            Assert.AreEqual(RoadCost.BaseCost, sampler.SampleCost(0f, 0f));
        }

        [Test]
        public void SampleCost_GroundAboveWater_IsNotBlocked()
        {
            var biome = new FixedSampler { Biome = BiomeId.Meadow, Slope = 0f };
            var water = new FixedWaterSampler { HasWater = true, WaterY = 5f };
            var height = new FixedHeightSampler { GroundY = 10f };
            var sampler = new RoadCostSampler(biome, biome, water, height);

            Assert.AreEqual(RoadCost.BaseCost, sampler.SampleCost(0f, 0f));
        }

        [Test]
        public void SampleCost_GroundAtOrBelowWater_IsBlocked()
        {
            var biome = new FixedSampler { Biome = BiomeId.Meadow, Slope = 0f };
            var water = new FixedWaterSampler { HasWater = true, WaterY = 10f };
            var height = new FixedHeightSampler { GroundY = 10f };
            var sampler = new RoadCostSampler(biome, biome, water, height);

            Assert.IsTrue(float.IsPositiveInfinity(sampler.SampleCost(0f, 0f)));
        }

        [Test]
        public void SampleCost_WaterPresentButHeightUnknown_IsConservativelyBlocked()
        {
            var biome = new FixedSampler { Biome = BiomeId.Meadow, Slope = 0f };
            var water = new FixedWaterSampler { HasWater = true, WaterY = 10f };
            var height = new FixedHeightSampler { HasGround = false };
            var sampler = new RoadCostSampler(biome, biome, water, height);

            Assert.IsTrue(float.IsPositiveInfinity(sampler.SampleCost(0f, 0f)));
        }
    }
}
