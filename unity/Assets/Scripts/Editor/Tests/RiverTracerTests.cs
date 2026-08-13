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

            // Lake-feeder pass discards OffTile/Sink runs. EnsureSpineRiver may still keep one long
            // OffTile channel as the map-spanning stem when no lake exists on the stub tile.
            var rivers = RiverTracer.Trace(grid, Array.Empty<WaterZone>(), LowThresholdSettings());

            Assert.LessOrEqual(rivers.Length, 1,
                "without lakes, only an optional OffTile spine is allowed — not a pile of dead-end feeders");
            if (rivers.Length == 1)
            {
                Assert.AreEqual(RiverTermination.OffTile, rivers[0].Termination);
            }
        }

        [Test]
        public void Trace_LargeRamp_SpineRiverSpansMostOfTheTile()
        {
            // Bigger grid so the 0.45× tile-span length gate for EnsureSpineRiver is meaningful.
            const int size = 40;
            var heights = MakeRamp(size);
            var grid = FlowNetwork.Compute(Sampler(heights), 0, 0, size, size, 1);
            var lakes = new[] { MakeLakeAtLowXColumn(size) };

            var rivers = RiverTracer.Trace(grid, lakes, LowThresholdSettings(maxRivers: 4));
            Assert.Greater(rivers.Length, 0);

            var tileSpan = size;
            var minLen = tileSpan * 0.45f;
            var longest = 0f;
            foreach (var river in rivers)
            {
                var len = 0f;
                for (var i = 1; i < river.Centerline.Count; i++)
                {
                    var a = river.Centerline[i - 1];
                    var b = river.Centerline[i];
                    var dx = b.x - a.x;
                    var dz = b.z - a.z;
                    len += Mathf.Sqrt(dx * dx + dz * dz);
                }

                if (len > longest)
                {
                    longest = len;
                }
            }

            Assert.GreaterOrEqual(longest, minLen * 0.85f,
                "expected at least one mountain→lake/OffTile stem spanning most of the tile");
        }

        [Test]
        public void SyncWaterSurfaceAfterCarve_HugsCarvedBedWithSmallFreeboard()
        {
            var centerline = new[] { new Vector3(5f, 10f, 5f), new Vector3(4f, 9f, 5f) };
            var widths = new[] { 4f, 4f };
            var carveDepths = new[] { 2f, 2f };
            var surface = new[] { 9.2f, 8.2f };
            var path = new RiverPath(
                "r0", RiverTermination.Lake, centerline, widths, carveDepths, surface,
                0f, 0f, 1f, 10, 10);

            const float bed = 7f;
            path.SyncWaterSurfaceAfterCarve((x, z) => bed, waterFillFraction: 0.35f);

            for (var i = 0; i < path.WaterSurfaceY.Count; i++)
            {
                Assert.Greater(path.WaterSurfaceY[i], bed + 0.3f);
                Assert.Less(path.WaterSurfaceY[i], bed + 1.3f, "must not float metres above the trench");
                Assert.Less(path.WaterSurfaceY[i], centerline[i].y);
            }
        }
    }
}
