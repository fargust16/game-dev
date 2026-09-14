using System.Collections.Generic;
using GameDev.World.Hydrology;
using NUnit.Framework;

namespace GameDev.World.Hydrology.Tests
{
    // Inside the namespace on purpose: these tests live under GameDev.World, where the
    // legacy GameDev.World.WorldSeed would otherwise win the simple name over a
    // file-scoped alias, and silently bind to the class without IWorldSeed.
    using WorldSeed = GameDev.World.Seed.WorldSeed;

    /// <summary>
    /// End-to-end tests for the analysis: the hard world rule, the topology guarantees, and
    /// determinism.
    /// </summary>
    public sealed class HydrologyPlannerTests
    {
        private static HydrologySettings SmallWorldSettings()
        {
            var settings = HydrologySettings.Default;

            // The test island is a fraction of the real one, so the area thresholds are scaled to
            // match. Everything stays in square metres, which is the point of expressing them that
            // way rather than in cells.
            settings.ChannelInitiationAreaSqm = 6_000f;
            settings.FullSizeAreaSqm = 120_000f;
            settings.MinRiverSystemAreaSqm = 8_000f;
            settings.MinRiverSystemFractionOfLargest = 0.12f;
            settings.NodeSpacingMeters = 10f;
            return settings;
        }

        private static HydrologyPlanner.Result PlanValleyIsland(int seed = 110201)
        {
            return HydrologyPlanner.Plan(
                HydrologyTestTerrain.ValleyIsland(), SmallWorldSettings(), new WorldSeed(seed));
        }

        [Test]
        public void ValleyIsland_ProducesARiverNetwork()
        {
            var result = PlanValleyIsland();

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.Greater(result.Definition.Reaches.Count, 0);
            Assert.Greater(result.Definition.Stats.OceanOutletCount, 0);
        }

        [Test]
        public void EveryReach_ReachesTheOcean()
        {
            // The hard world rule, walked explicitly rather than inferred from the algorithm.
            var definition = PlanValleyIsland().Definition;
            var byId = new Dictionary<int, RiverReach>();
            foreach (var reach in definition.Reaches)
            {
                byId[reach.Id.Value] = reach;
            }

            foreach (var start in definition.Reaches)
            {
                var current = start;
                var steps = 0;
                while (current.Termination != ReachTermination.Ocean)
                {
                    Assert.IsTrue(
                        current.DownstreamId.IsValid,
                        $"{current.Id} ends as {current.Termination} with no downstream reach");
                    current = byId[current.DownstreamId.Value];

                    Assert.Less(
                        ++steps, definition.Reaches.Count + 1,
                        $"{start.Id} never reached the ocean");
                }
            }
        }

        [Test]
        public void NoReach_FlowsUphill()
        {
            foreach (var reach in PlanValleyIsland().Definition.Reaches)
            {
                for (var i = 1; i < reach.Nodes.Count; i++)
                {
                    Assert.LessOrEqual(
                        reach.Nodes[i].BedElevation,
                        reach.Nodes[i - 1].BedElevation + HydrologyValidator.UphillToleranceMeters,
                        $"{reach.Id} rises at node {i}");
                }
            }
        }

        [Test]
        public void ReachIds_AreDenseAndAscending()
        {
            var reaches = PlanValleyIsland().Definition.Reaches;
            for (var i = 0; i < reaches.Count; i++)
            {
                Assert.AreEqual(i, reaches[i].Id.Value, "reach ids are not dense and ascending");
            }
        }

        [Test]
        public void Network_HasTributaries()
        {
            var definition = PlanValleyIsland().Definition;

            Assert.Greater(
                definition.Stats.TributaryCount, 0,
                "the two-valley island should produce at least one confluence");
            Assert.Greater(
                definition.Stats.ReachCount, definition.Stats.OceanOutletCount,
                "every reach is its own outlet, so nothing joined anything");
        }

        [Test]
        public void StrahlerOrder_RisesAtAConfluence()
        {
            var definition = PlanValleyIsland().Definition;

            var maxOrder = 0;
            foreach (var reach in definition.Reaches)
            {
                if (reach.StrahlerOrder > maxOrder)
                {
                    maxOrder = reach.StrahlerOrder;
                }

                Assert.GreaterOrEqual(reach.StrahlerOrder, 1);
            }

            Assert.Greater(maxOrder, 1, "no reach ever became a higher-order stream");
        }

        [Test]
        public void SameSeedAndSettings_ProduceIdenticalHydrology()
        {
            var first = PlanValleyIsland().Definition;
            var second = PlanValleyIsland().Definition;

            Assert.AreEqual(first.Reaches.Count, second.Reaches.Count);
            Assert.AreEqual(first.Lakes.Count, second.Lakes.Count);
            Assert.AreEqual(first.Waterfalls.Count, second.Waterfalls.Count);

            for (var r = 0; r < first.Reaches.Count; r++)
            {
                var a = first.Reaches[r];
                var b = second.Reaches[r];
                Assert.AreEqual(a.Id, b.Id);
                Assert.AreEqual(a.DownstreamId, b.DownstreamId);
                Assert.AreEqual(a.StrahlerOrder, b.StrahlerOrder);
                Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);

                for (var n = 0; n < a.Nodes.Count; n++)
                {
                    // Exact equality, not a tolerance: the analysis is a pure function of the same
                    // inputs, so any drift at all would mean something non-deterministic crept in.
                    Assert.AreEqual(a.Nodes[n].PositionXZ, b.Nodes[n].PositionXZ);
                    Assert.AreEqual(a.Nodes[n].BedElevation, b.Nodes[n].BedElevation);
                }
            }
        }

        [Test]
        public void HydrologySeed_DoesNotAlterTheTerrainItReads()
        {
            // The guarantee that re-seeding hydrology cannot move the mountains. Structurally the
            // terrain is baked before hydrology runs at all; this pins the other half of it, that
            // the analysis never writes back into the height field it was handed.
            var grid = HydrologyTestTerrain.ValleyIsland();
            var before = (float[])grid.Heights.Clone();

            HydrologyPlanner.Plan(grid, SmallWorldSettings(), new WorldSeed(1));
            HydrologyPlanner.Plan(grid, SmallWorldSettings(), new WorldSeed(999));

            CollectionAssert.AreEqual(before, grid.Heights);
        }

        [Test]
        public void LandlockedWorld_FailsLoudlyInsteadOfInventingRivers()
        {
            var result = HydrologyPlanner.Plan(
                HydrologyTestTerrain.LandlockedPlateau(),
                SmallWorldSettings(),
                new WorldSeed(110201));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(HydrologyPlanner.FailureReason.NoOcean, result.Failure);
            Assert.IsTrue(result.Definition.IsEmpty, "a failed plan still produced geography");
            Assert.IsNotEmpty(result.Message);
        }

        [Test]
        public void ThresholdTooHigh_ReportsNoChannelsRatherThanAnEmptySuccess()
        {
            var settings = SmallWorldSettings();
            settings.ChannelInitiationAreaSqm = 50_000_000f;

            var result = HydrologyPlanner.Plan(
                HydrologyTestTerrain.ValleyIsland(), settings, new WorldSeed(110201));

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(HydrologyPlanner.FailureReason.NoChannels, result.Failure);
        }

        [Test]
        public void RaisingTheSystemThreshold_ReducesTheNumberOfSeparateRivers()
        {
            // The control that keeps an island from growing a rill at every gully.
            var loose = SmallWorldSettings();
            loose.MinRiverSystemFractionOfLargest = 0.01f;
            loose.MinRiverSystemAreaSqm = 6_000f;

            var strict = SmallWorldSettings();
            strict.MinRiverSystemFractionOfLargest = 0.75f;

            var terrain = HydrologyTestTerrain.ValleyIsland();
            var many = HydrologyPlanner.Plan(terrain, loose, new WorldSeed(110201));
            var few = HydrologyPlanner.Plan(terrain, strict, new WorldSeed(110201));

            Assert.IsTrue(many.Succeeded, many.Message);
            Assert.IsTrue(few.Succeeded, few.Message);
            Assert.LessOrEqual(
                few.Definition.Stats.OceanOutletCount,
                many.Definition.Stats.OceanOutletCount);
        }

        [Test]
        public void PitOnTheIsland_BecomesALakeWiredIntoTheNetwork()
        {
            var grid = HydrologyTestTerrain.RampWithPit(size: 96, cellSize: 12f, depth: 10f);
            var settings = SmallWorldSettings();
            var result = HydrologyPlanner.Plan(grid, settings, new WorldSeed(110201));

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, result.Definition.Lakes.Count);

            var lake = result.Definition.Lakes[0];
            Assert.IsTrue(
                lake.OutletReach.IsValid,
                "a filled basin must spill, so its outlet reach has to exist");
            Assert.IsNotNull(result.Definition.FindReach(lake.OutletReach));
        }
    }
}
