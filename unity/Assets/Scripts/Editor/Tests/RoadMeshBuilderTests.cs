using System.Collections.Generic;
using GameDev.World.Roads;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadMeshBuilderTests
    {
        [Test]
        public void Build_StraightFlatPath_ProducesExpectedVertexAndTriangleCounts()
        {
            var centerline = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 10f),
                new Vector3(0f, 0f, 20f),
            };

            var mesh = RoadMeshBuilder.Build(centerline, width: 4f, uvTileLength: 4f);

            Assert.AreEqual(centerline.Count * 2, mesh.vertexCount);
            Assert.AreEqual((centerline.Count - 1) * 2, mesh.triangles.Length / 3);
        }

        [Test]
        public void Build_StraightFlatPath_HasExpectedWidthAtEachSample()
        {
            var centerline = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 10f),
            };

            var mesh = RoadMeshBuilder.Build(centerline, width: 4f, uvTileLength: 4f);
            var vertices = mesh.vertices;

            // Path runs along +Z, so width must be a purely X offset.
            Assert.AreEqual(4f, Vector3.Distance(vertices[0], vertices[1]), 1e-4f);
            Assert.AreEqual(-2f, vertices[0].x, 1e-4f);
            Assert.AreEqual(2f, vertices[1].x, 1e-4f);
        }

        [Test]
        public void Build_FlatPath_TrianglesFaceUpward()
        {
            var centerline = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(3f, 0f, 8f),
                new Vector3(-2f, 0f, 16f),
            };

            var mesh = RoadMeshBuilder.Build(centerline, width: 3f, uvTileLength: 3f);
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (var t = 0; t < triangles.Length; t += 3)
            {
                var a = vertices[triangles[t]];
                var b = vertices[triangles[t + 1]];
                var c = vertices[triangles[t + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                Assert.Greater(Vector3.Dot(normal, Vector3.up), 0f, "every triangle should face upward on a flat path");
            }
        }

        [Test]
        public void Build_UvsTileAlongTravelledDistance_NotPerSegmentIndex()
        {
            // Two samples 20m apart should reach v=1 with tileLength=10, even though it's a single
            // segment - proves tiling uses real distance, not "one tile per point-to-point step".
            var centerline = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 20f),
            };

            var mesh = RoadMeshBuilder.Build(centerline, width: 4f, uvTileLength: 10f);
            var uvs = mesh.uv;

            Assert.AreEqual(0f, uvs[0].y, 1e-4f);
            Assert.AreEqual(0f, uvs[1].y, 1e-4f);
            Assert.AreEqual(2f, uvs[2].y, 1e-4f);
            Assert.AreEqual(2f, uvs[3].y, 1e-4f);
        }

        [Test]
        public void Build_TwoPointPath_StillProducesAValidQuad()
        {
            var centerline = new List<Vector3> { new Vector3(0f, 0f, 0f), new Vector3(5f, 0f, 0f) };
            var mesh = RoadMeshBuilder.Build(centerline, width: 2f, uvTileLength: 2f);

            Assert.AreEqual(4, mesh.vertexCount);
            Assert.AreEqual(2, mesh.triangles.Length / 3);
        }
    }
}
