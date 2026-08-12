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

        [Test]
        public void Build_WithTerrainSampler_StaysOneContinuousFlatBody()
        {
            var zone = MakeFilledBlob(16, 2f, 10f);
            zone.SetWaterLevel(8.5f);
            float Sample(float x, float z)
            {
                // Spiky terrain that would previously punch slits into the water silhouette.
                var cx = 16f;
                var cz = 16f;
                var d = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
                return 8.5f - 2.5f + d * 0.15f + Mathf.Sin(x * 2f) * 3f;
            }

            var mesh = WaterMeshBuilder.Build(zone, new[] { zone }, Sample);
            Assert.Greater(mesh.vertexCount, 20);
            Assert.IsTrue(mesh.vertices.All(v => Mathf.Approximately(v.y, 8.5f)));
            Assert.AreEqual(1, mesh.subMeshCount, "one lake = one continuous mesh body");
        }

        [Test]
        public void Build_ExpandedSilhouette_ExtendsPastFloodAabbSlightly()
        {
            var zone = MakeFilledBlob(14, 2f, 10f);
            zone.SetWaterLevel(9f);
            var flood = zone.GetFloodAabbXZ();
            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });
            Assert.Greater(mesh.vertexCount, 0);

            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            foreach (var v in mesh.vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }

            Assert.Less(minX, flood.xMin + 0.01f, "water should slightly overshoot flood minX");
            Assert.Greater(maxX, flood.xMax - 0.01f, "water should slightly overshoot flood maxX");
            Assert.Less(minZ, flood.yMin + 0.01f, "water should slightly overshoot flood minZ");
            Assert.Greater(maxZ, flood.yMax - 0.01f, "water should slightly overshoot flood maxZ");
        }
    }
}
