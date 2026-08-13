using System.Collections.Generic;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class RiverTerrainCarverTests
    {
        [Test]
        public void DensifyPath_FillsGapsBetweenSparseCenterlinePoints()
        {
            var centerline = new List<Vector3>
            {
                new Vector3(0f, 10f, 0f),
                new Vector3(30f, 10f, 0f),
            };
            var widths = new List<float> { 8f, 8f };
            var depths = new List<float> { 3f, 3f };
            var surface = new List<float> { 9f, 9f };
            var river = new RiverPath(
                "r", RiverTermination.Sink, centerline, widths, depths, surface,
                0f, -5f, 3f, 20, 10);

            RiverTerrainCarver.DensifyPath(river, 1.5f, out var pts, out var halfW, out var d);
            Assert.Greater(pts.Count, 10, "30m span at 1.5m step must densify");
            Assert.AreEqual(pts.Count, halfW.Count);
            Assert.AreEqual(pts.Count, d.Count);
            Assert.AreEqual(0f, pts[0].x, 0.01f);
            Assert.AreEqual(30f, pts[pts.Count - 1].x, 0.01f);
        }
    }
}
