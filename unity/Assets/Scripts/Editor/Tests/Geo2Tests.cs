using System.Collections.Generic;
using GameDev.World.Town.Geometry;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Town.Tests
{
    public sealed class Geo2Tests
    {
        [Test]
        public void PolygonArea_UnitSquare_IsOne_CCW()
        {
            var sq = new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };
            Assert.AreEqual(1f, Geo2.PolygonArea(sq), 1e-4f);
        }

        [Test]
        public void PointInPolygon_CenterInside()
        {
            var sq = new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(2, 0), new Vector2(2, 2), new Vector2(0, 2)
            };
            Assert.IsTrue(Geo2.PointInPolygon(new Vector2(1, 1), sq));
            Assert.IsFalse(Geo2.PointInPolygon(new Vector2(3, 3), sq));
        }

        [Test]
        public void InsetPolygon_ShrinksArea()
        {
            var sq = new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)
            };
            var inset = Geo2.InsetPolygon(sq, 1f);
            Assert.Greater(Mathf.Abs(Geo2.PolygonArea(sq)), Mathf.Abs(Geo2.PolygonArea(inset)));
            Assert.IsTrue(Geo2.PointInPolygon(Geo2.PolygonCentroid(inset), sq));
        }

        [Test]
        public void SplitPolygonByLine_AreasSum()
        {
            var sq = new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(0, 10)
            };
            var (left, right) = Geo2.SplitPolygonByLine(sq, new Vector2(5, 5), Vector2.up);
            var sum = Mathf.Abs(Geo2.PolygonArea(left)) + Mathf.Abs(Geo2.PolygonArea(right));
            Assert.AreEqual(Mathf.Abs(Geo2.PolygonArea(sq)), sum, 0.5f);
        }

        [Test]
        public void SmoothCatmullRom_KeepsEndpoints()
        {
            var pts = new List<Vector2> { Vector2.zero, new Vector2(5, 2), new Vector2(10, 0) };
            var smooth = Geo2.SmoothCatmullRom(pts, 0.2f, 4);
            Assert.Less((smooth[0] - pts[0]).magnitude, 1e-3f);
            Assert.Less((smooth[smooth.Count - 1] - pts[pts.Count - 1]).magnitude, 1e-3f);
        }
    }
}
