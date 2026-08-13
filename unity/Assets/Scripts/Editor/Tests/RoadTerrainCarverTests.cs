using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadTerrainCarverTests
    {
        [Test]
        public void DensifyPolyline_InsertsPointsAlongLongSegment()
        {
            var src = new List<Vector2> { new Vector2(0f, 0f), new Vector2(10f, 0f) };
            var dense = RoadTerrainCarver.DensifyPolyline(src, stepMeters: 2.5f);

            Assert.Greater(dense.Count, 2, "10m span at 2.5m step should insert intermediate samples");
            Assert.AreEqual(src[0], dense[0]);
            Assert.AreEqual(src[src.Count - 1], dense[dense.Count - 1]);

            for (var i = 1; i < dense.Count; i++)
            {
                Assert.LessOrEqual(
                    Vector2.Distance(dense[i - 1], dense[i]),
                    2.5f + 1e-3f,
                    "consecutive densified points must stay within the step");
            }
        }

        [Test]
        public void DensifyPolyline_EmptyOrSingle_IsStable()
        {
            Assert.AreEqual(0, RoadTerrainCarver.DensifyPolyline(new List<Vector2>(), 2f).Count);
            var one = RoadTerrainCarver.DensifyPolyline(new List<Vector2> { new Vector2(1f, 2f) }, 2f);
            Assert.AreEqual(1, one.Count);
            Assert.AreEqual(new Vector2(1f, 2f), one[0]);
        }
    }
}
