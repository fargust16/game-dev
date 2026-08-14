using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Landscape.Tests
{
    public sealed class LakeBasinDeformerTests
    {
        [Test]
        public void Apply_FillsDownhillLip_AndClearsInteriorRidge()
        {
            const int res = 65;
            const float size = 200f;
            const float worldHeight = 80f;
            const float baseY = 40f;
            const float cx = 100f;
            const float cz = 100f;
            const float radius = 35f;

            var data = new TerrainData
            {
                heightmapResolution = res,
                size = new Vector3(size, worldHeight, size)
            };
            var heights = new float[res, res];
            var pixel = size / (res - 1);
            for (var pz = 0; pz < res; pz++)
            {
                for (var px = 0; px < res; px++)
                {
                    var wx = px * pixel;
                    var wz = pz * pixel;
                    var y = baseY;
                    var dx = wx - cx;
                    var dz = wz - cz;
                    if (dx * dx + dz * dz < 8f * 8f)
                    {
                        y = 55f;
                    }
                    else if (wx > cx + radius + 1.5f && wx < cx + radius + 8f &&
                             wz > cz - 12f && wz < cz + 12f)
                    {
                        y = 28f;
                    }

                    heights[pz, px] = y / worldHeight;
                }
            }

            data.SetHeights(0, 0, heights);

            var go = UnityEngine.Terrain.CreateTerrainGameObject(data);
            try
            {
                var terrain = go.GetComponent<UnityEngine.Terrain>();
                var sil = new LakeSilhouette(
                    cx, cz, radius, 1f, 0f,
                    0f, 0f, 0f, 0f, 0f, 0f, 1,
                    false, 0f, 0f, 0f);
                const int cells = 50;
                const float cell = 4f;
                var flooded = new bool[cells, cells];
                var zoneHeights = new float[cells, cells];
                for (var x = 0; x < cells; x++)
                {
                    for (var z = 0; z < cells; z++)
                    {
                        var wx = (x + 0.5f) * cell;
                        var wz = (z + 0.5f) * cell;
                        var dx = wx - cx;
                        var dz = wz - cz;
                        flooded[x, z] = dx * dx + dz * dz <= 25f * 25f;
                        zoneHeights[x, z] = baseY;
                    }
                }

                var zone = new WaterZone("lip", baseY, 0f, 0f, cell, flooded, zoneHeights, sil);
                var lake = new LakeDefinition(zone, 16f);
                LakeBasinDeformer.Apply(terrain, new[] { lake });

                var waterY = lake.WaterLevelY;
                var centerY = SampleY(terrain, cx, cz);
                Assert.Less(centerY, waterY - 0.9f, "interior ridge must be a wet bowl, not a land split");

                var lipX = cx + radius + 3f;
                var lipY = SampleY(terrain, lipX, cz);
                Assert.GreaterOrEqual(
                    lipY,
                    waterY - 0.05f,
                    "downhill overlap ring must rise to meet the flat water sheet");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(data);
            }
        }

        private static float SampleY(UnityEngine.Terrain terrain, float x, float z)
        {
            return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrain.transform.position.y;
        }
    }
}
