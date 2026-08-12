using System;
using System.Collections.Generic;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class RiverTracerTests
    {
        private static Func<float, float, float> Sampler(float[,] heights)
        {
            return (worldX, worldZ) => heights[Mathf.FloorToInt(worldX), Mathf.FloorToInt(worldZ)];
        }

        // Flows toward x=0 (height = x), flat along z - every z-row is its own independent channel
        // with no lateral convergence, which is exactly what makes it easy to reason about in a test.
        private static float[,] MakeRamp(int size)
        {
            var heights = new float[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    heights[x, z] = x;
                }
            }

            return heights;
        }

        private static WaterZone MakeLakeAtLowXColumn(int size)
        {
            var flooded = new bool[size, size];
            var heights = new float[size, size];
            for (var z = 0; z < size; z++)
            {
                flooded[0, z] = true;
                heights[0, z] = 0f;
            }

            return new WaterZone("test_lake", waterLevelY: 0f, originX: 0f, originZ: 0f, cellSize: 1f, flooded, heights);
        }

        private static RiverTracerSettings LowThresholdSettings(int maxRivers = 8, float minSourceElevationY = 0f) => new RiverTracerSettings(
            channelAccumulationThreshold: 5,
            referenceAccumulation: 20,
            minWidthMeters: 3f,
            maxWidthMeters: 16f,
            minCarveDepthMeters: 0.6f,
            maxCarveDepthMeters: 2.5f,
            waterFillFraction: 0.6f,
            maxRivers: maxRivers,
            minPathCells: 2,
            minSourceElevationY: minSourceElevationY);

        [Test]
        public void Trace_Ramp_EveryRiverEndsInTheLake()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            var rivers = RiverTracer.Trace(grid, lakes, LowThresholdSettings());

            Assert.Greater(rivers.Length, 0, "the ramp has channel-strength accumulation and a reachable lake - expected at least one river");
            foreach (var river in rivers)
            {
                Assert.AreEqual(RiverTermination.Lake, river.Termination,
                    "every channel on this ramp flows straight to the lake with nothing else to intercept it");
            }
        }

        [Test]
        public void Trace_Ramp_CenterlineHeightNeverIncreasesDownstream()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            var rivers = RiverTracer.Trace(grid, lakes, LowThresholdSettings());

            Assert.Greater(rivers.Length, 0);
            foreach (var river in rivers)
            {
                for (var i = 1; i < river.Centerline.Count; i++)
                {
                    Assert.LessOrEqual(river.Centerline[i].y, river.Centerline[i - 1].y,
                        $"river {river.Id} point {i} sits higher than the point upstream of it");
                }
            }
        }

        [Test]
        public void Trace_Ramp_WidthNeverShrinksDownstream()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            var rivers = RiverTracer.Trace(grid, lakes, LowThresholdSettings());

            Assert.Greater(rivers.Length, 0);
            foreach (var river in rivers)
            {
                for (var i = 1; i < river.Widths.Count; i++)
                {
                    Assert.GreaterOrEqual(river.Widths[i], river.Widths[i - 1],
                        $"river {river.Id} narrows moving downstream at point {i} - more catchment should never produce a smaller channel");
                }
            }
        }

        [Test]
        public void Trace_ThresholdAboveEveryCellsAccumulation_ProducesNoRivers()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            var settings = new RiverTracerSettings(
                channelAccumulationThreshold: 1000, referenceAccumulation: 2000,
                minWidthMeters: 3f, maxWidthMeters: 16f,
                minCarveDepthMeters: 0.6f, maxCarveDepthMeters: 2.5f,
                waterFillFraction: 0.6f, maxRivers: 8, minPathCells: 2,
                minSourceElevationY: 0f);

            var rivers = RiverTracer.Trace(grid, lakes, settings);

            Assert.AreEqual(0, rivers.Length, "no cell on a 10x10 grid can accumulate 1000 cells - nothing should qualify as a channel");
        }

        [Test]
        public void Trace_MaxRivers_CapsReturnedCount()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            // Every one of the 10 parallel z-rows independently qualifies as its own channel on this
            // ramp (no lateral convergence) - a generous cap should return exactly that many rivers,
            // and a tight cap should return exactly the cap.
            var uncapped = RiverTracer.Trace(grid, lakes, LowThresholdSettings(maxRivers: 100));
            var capped = RiverTracer.Trace(grid, lakes, LowThresholdSettings(maxRivers: 3));

            Assert.AreEqual(10, uncapped.Length);
            Assert.AreEqual(3, capped.Length);
        }

        [Test]
        public void Trace_SameInputsTwice_AreDeterministic()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };
            var settings = LowThresholdSettings();

            var first = RiverTracer.Trace(grid, lakes, settings);
            var second = RiverTracer.Trace(grid, lakes, settings);

            Assert.AreEqual(first.Length, second.Length);
            for (var i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i].Id, second[i].Id);
                Assert.AreEqual(first[i].Termination, second[i].Termination);
                Assert.AreEqual(first[i].Centerline.Count, second[i].Centerline.Count);
            }
        }

        [Test]
        public void Trace_HeadBelowMinSourceElevation_ProducesNoRivers()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(10) };

            // Threshold=5 puts every row's head at x=5 (height=5, see Trace_MaxRivers_CapsReturnedCount's
            // note that each row is an independent channel) - requiring sources at or above 6 excludes
            // every one of them, even though the channel/lake are otherwise exactly as reachable as in
            // Trace_Ramp_EveryRiverEndsInTheLake.
            var rivers = RiverTracer.Trace(grid, lakes, LowThresholdSettings(minSourceElevationY: 6f));

            Assert.AreEqual(0, rivers.Length,
                "no head sits at or above the elevation floor - a river should never start on ground this low");
        }

        [Test]
        public void Trace_NoLakeToReach_ProducesNoRivers()
        {
            var heights = MakeRamp(10);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, 10, 10, 1);

            // Same channel-strength ramp as Trace_Ramp_EveryRiverEndsInTheLake, but with no lake for
            // it to reach - every trace dead-ends at the tile edge (x=0 is a border cell) instead,
            // and "always mountain to nearest lake, or it doesn't exist" means none of those partial
            // runs should be returned as rivers.
            var rivers = RiverTracer.Trace(grid, Array.Empty<WaterZone>(), LowThresholdSettings());

            Assert.AreEqual(0, rivers.Length,
                "a run that never reaches a lake is a dead end, not a river - it should be discarded, not returned with a non-Lake termination");
        }

        [Test]
        public void SyncWaterSurfaceAfterCarve_PlacesSurfaceBetweenBedAndBank()
        {
            var centerline = new[] { new Vector3(5f, 10f, 5f), new Vector3(4f, 9f, 5f) };
            var widths = new[] { 4f, 4f };
            var carveDepths = new[] { 2f, 2f };
            var surface = new[] { 9.2f, 8.2f };
            var path = new RiverPath(
                "r0", RiverTermination.Lake, centerline, widths, carveDepths, surface,
                0f, 0f, 1f, 10, 10);

            path.SyncWaterSurfaceAfterCarve((x, z) => 7f, waterFillFraction: 0.6f);

            for (var i = 0; i < path.WaterSurfaceY.Count; i++)
            {
                Assert.Greater(path.WaterSurfaceY[i], 7f);
                Assert.Less(path.WaterSurfaceY[i], centerline[i].y);
            }

            // When the heightmap shows almost no carve, surface still drops under the bank.
            path.SyncWaterSurfaceAfterCarve((x, z) => centerline[0].y - 0.05f, waterFillFraction: 0.6f);
            Assert.Less(path.WaterSurfaceY[0], centerline[0].y);
        }
    }
}
