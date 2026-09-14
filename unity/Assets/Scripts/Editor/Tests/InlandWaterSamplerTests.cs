using System.Collections.Generic;
using GameDev.World.Hydrology;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    /// <summary>
    /// The one seam between hydrology and roads/settlements/POIs. It has to answer the existing
    /// <see cref="IWaterSampler"/> question and nothing more — no new gameplay rule, no second water
    /// query system.
    /// </summary>
    public sealed class InlandWaterSamplerTests
    {
        private static HydrologyDefinition OneRiverAndOneLake()
        {
            var nodes = new List<RiverNode>();
            for (var i = 0; i < 20; i++)
            {
                nodes.Add(new RiverNode(
                    new Vector2(100f + i * 10f, 500f),
                    60f - i,
                    61f - i,
                    i * 10f,
                    120_000f));
            }

            var reach = new RiverReach(
                new ReachId(0),
                ReachId.None,
                LakeId.None,
                nodes,
                2,
                120_000f,
                14f,
                ReachTermination.Ocean,
                true);

            var lake = new LakeBasin(
                new LakeId(0),
                new[]
                {
                    new Vector2(800f, 800f),
                    new Vector2(900f, 800f),
                    new Vector2(900f, 900f),
                    new Vector2(800f, 900f),
                },
                42f,
                6f,
                10_000f,
                new ReachId(0),
                new ReachId[0],
                new Vector2(900f, 850f));

            return new HydrologyDefinition(
                new[] { reach },
                new[] { lake },
                new WaterfallCandidate[0],
                default,
                HydrologySettings.Default);
        }

        [Test]
        public void PointOnTheChannel_IsWater()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsTrue(sampler.TryGetWaterLevel(200f, 500f, out var y));
            Assert.AreEqual(51f, y, 0.5f);
        }

        [Test]
        public void PointBesideTheChannel_IsNotWater()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsFalse(sampler.TryGetWaterLevel(200f, 600f, out _));
        }

        [Test]
        public void WaterLevel_FollowsTheReachDownstream()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsTrue(sampler.TryGetWaterLevel(110f, 500f, out var upstream));
            Assert.IsTrue(sampler.TryGetWaterLevel(280f, 500f, out var downstream));
            Assert.Greater(
                upstream, downstream, "the reported surface must fall along the river, not be flat");
        }

        [Test]
        public void PointInsideALake_ReportsTheLakeSurface()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsTrue(sampler.TryGetWaterLevel(850f, 850f, out var y));
            Assert.AreEqual(42f, y, 1e-3f);
        }

        [Test]
        public void PointOutsideALake_IsNotWater()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsFalse(sampler.TryGetWaterLevel(950f, 850f, out _));
        }

        [Test]
        public void EmptyHydrology_ReportsNoWaterAnywhere()
        {
            var sampler = new InlandWaterSampler(HydrologyDefinition.Empty);

            Assert.IsTrue(sampler.IsEmpty);
            Assert.IsFalse(sampler.TryGetWaterLevel(0f, 0f, out _));
            Assert.IsFalse(sampler.TryGetWaterLevel(1400f, 1400f, out _));
        }

        [Test]
        public void NullDefinition_IsTreatedAsNoWater()
        {
            var sampler = new InlandWaterSampler(null);

            Assert.IsFalse(sampler.TryGetWaterLevel(500f, 500f, out _));
        }

        [Test]
        public void ComposedWithTheOcean_TheOceanStillAnswersOffshore()
        {
            // The whole point of routing inland water through CompositeWaterSampler: the callers
            // keep one water query and neither source has to know about the other.
            var ocean = new SeaLevelOceanSampler((_, _) => 5f);
            var inland = new InlandWaterSampler(OneRiverAndOneLake());
            var composite = new CompositeWaterSampler(ocean, inland);

            Assert.IsTrue(composite.TryGetWaterLevel(2000f, 2000f, out var offshore));
            Assert.AreEqual(WorldScale.SeaLevelMeters, offshore, 1e-3f);

            Assert.IsTrue(composite.TryGetWaterLevel(850f, 850f, out _));
        }

        [Test]
        public void RiverHalfWidth_IsReportedOnTheChannelAndZeroAwayFromIt()
        {
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.Greater(sampler.RiverHalfWidthAt(200f, 500f), 6f);
            Assert.AreEqual(0f, sampler.RiverHalfWidthAt(200f, 700f), 1e-4f);
        }

        [Test]
        public void QueriesAreAnsweredAcrossTheWholeWorld_NotJustTheFirstTile()
        {
            // The tile-(0,0) trap the ocean sampler still has. Inland water is indexed in world
            // space, so a query on any tile is answered from the same structure.
            var sampler = new InlandWaterSampler(OneRiverAndOneLake());

            Assert.IsTrue(sampler.TryGetWaterLevel(850f, 850f, out _), "tile (0,0)");
            Assert.IsFalse(sampler.TryGetWaterLevel(2100f, 2100f, out _), "tile (1,1) has no water");
        }
    }
}
