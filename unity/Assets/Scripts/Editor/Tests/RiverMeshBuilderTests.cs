using System.Collections.Generic;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class RiverMeshBuilderTests
    {
        [Test]
        public void MiterOffset_SharpBend_DoesNotExplodeBeyondLimit()
        {
            var points = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 10f),
            };
            const float half = 4f;
            var offset = RiverMeshBuilder.MiterOffset(points, 1, half);
            Assert.LessOrEqual(offset.magnitude, half * RiverMeshBuilder.MiterLimit + 0.05f);
            Assert.Greater(offset.magnitude, 0.5f);
        }

        [Test]
        public void Build_HairpinCenterline_ProducesFiniteMesh()
        {
            var centerline = new List<Vector3>();
            var widths = new List<float>();
            var depths = new List<float>();
            var surface = new List<float>();
            for (var i = 0; i < 8; i++)
            {
                centerline.Add(new Vector3(i * 3f, 10f, 0f));
                widths.Add(8f);
                depths.Add(3f);
                surface.Add(8f);
            }

            // Sharp fold back
            centerline.Add(new Vector3(21f, 10f, 0f));
            centerline.Add(new Vector3(21f, 10f, 6f));
            centerline.Add(new Vector3(12f, 10f, 6f));
            widths.Add(8f);
            widths.Add(8f);
            widths.Add(8f);
            depths.Add(3f);
            depths.Add(3f);
            depths.Add(3f);
            surface.Add(8f);
            surface.Add(8f);
            surface.Add(8f);

            var river = new RiverPath(
                "test", RiverTermination.Sink,
                centerline, widths, depths, surface,
                originX: 0f, originZ: -10f, cellSize: 3f, gridWidth: 20, gridDepth: 20);

            var mesh = RiverMeshBuilder.Build(river, uvTileLength: 8f);
            Assert.Greater(mesh.vertexCount, 4);
            Assert.Greater(mesh.triangles.Length, 0);
            foreach (var v in mesh.vertices)
            {
                Assert.IsFalse(float.IsNaN(v.x) || float.IsInfinity(v.x));
                Assert.IsFalse(float.IsNaN(v.z) || float.IsInfinity(v.z));
            }
        }
    }
}
