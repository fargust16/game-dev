using System.Linq;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class WaterMeshBuilderTests
    {
        private static WaterZone MakeFilledBlob(int size, float cellSize, float waterY)
        {
            var heights = new float[size, size];
            var flooded = new bool[size, size];
            for (var x = 2; x < size - 2; x++)
            {
                for (var z = 2; z < size - 2; z++)
                {
                    flooded[x, z] = true;
                    heights[x, z] = waterY - 3f;
                }
            }

            return new WaterZone("blob", waterY, 0f, 0f, cellSize, flooded, heights);
        }

        [Test]
        public void Build_OneLake_ProducesFlatSurfaceAtWaterLevel()
        {
            var zone = MakeFilledBlob(12, 3f, 11f);
            zone.SetWaterLevel(9.5f);
            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });
            Assert.Greater(mesh.vertexCount, 0, "filled lake must produce water geometry");
            Assert.Greater(mesh.triangles.Length, 0);
            Assert.IsTrue(mesh.vertices.All(v => Mathf.Approximately(v.y, 9.5f)), "one lake = one flat Y");
        }

        [Test]
        public void Build_EmptyFlood_ProducesEmptyMesh()
        {
            var heights = new float[4, 4];
            var flooded = new bool[4, 4];
            var zone = new WaterZone("empty", 1f, 0f, 0f, 2f, flooded, heights);
            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });
            Assert.AreEqual(0, mesh.vertexCount);
        }
    }
}
