using GameDev.World;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class NaturalIslandGeographyTests
    {
        [Test]
        public void Geography_SameSeed_IsDeterministic()
        {
            var a = BuildHeights(110201, true);
            var b = BuildHeights(110201, true);
            Assert.AreEqual(a.GetLength(0), b.GetLength(0));
            Assert.AreEqual(a[10, 10], b[10, 10], 1e-6f);
            Assert.AreEqual(a[200, 180], b[200, 180], 1e-6f);
            Assert.AreEqual(a[400, 400], b[400, 400], 1e-6f);
        }

        [Test]
        public void Geography_DifferentSeed_ChangesCoastOrPeaks()
        {
            var a = BuildHeights(110201, true);
            var b = BuildHeights(991133, true);
            var changed = false;
            for (var i = 0; i < a.GetLength(0); i += 17)
            {
                for (var j = 0; j < a.GetLength(1); j += 17)
                {
                    if (Mathf.Abs(a[i, j] - b[i, j]) > 0.01f)
                    {
                        changed = true;
                        break;
                    }
                }

                if (changed)
                {
                    break;
                }
            }

            Assert.IsTrue(changed, "Different seeds should change the heightfield.");
        }

        [Test]
        public void Geography_BorderIsSubmerged()
        {
            var terrain = CreateTempWorldTerrain(257);
            try
            {
                // Seed a flat mid-height field then frame + sink world border.
                FillFlat(terrain, 0.4f);
                IslandNaturalGeography.Apply(terrain, 424242, true);
                IslandOccupancy.ApplyBoundarySubmersion(
                    terrain, new WorldSeed(424242).Fork("terrain.island").Next());
                var data = terrain.terrainData;
                var res = data.heightmapResolution;
                var heights = data.GetHeights(0, 0, res, res);
                var maxBorder = (WorldScale.SeaLevelMeters - WorldScale.MinimumSubmersionMeters) /
                                data.size.y;
                for (var i = 0; i < res; i++)
                {
                    Assert.LessOrEqual(heights[0, i], maxBorder + 1e-4f);
                    Assert.LessOrEqual(heights[res - 1, i], maxBorder + 1e-4f);
                    Assert.LessOrEqual(heights[i, 0], maxBorder + 1e-4f);
                    Assert.LessOrEqual(heights[i, res - 1], maxBorder + 1e-4f);
                }
            }
            finally
            {
                DestroyTempTerrain(terrain);
            }
        }

        [Test]
        public void Geography_IslandMask_IsNotCircular()
        {
            var size = new Vector3(
                WorldScale.WorldExtentXMeters,
                WorldScale.TerrainHeightBudgetMeters,
                WorldScale.WorldExtentZMeters);
            const int worldSeed = 777001;
            var samples = 72;
            var radii = new float[samples];
            var cx = size.x * 0.5f;
            var cz = size.z * 0.5f;
            for (var i = 0; i < samples; i++)
            {
                var ang = i / (float)samples * Mathf.PI * 2f;
                float lo = 50f, hi = size.x * 0.5f;
                for (var it = 0; it < 18; it++)
                {
                    var mid = (lo + hi) * 0.5f;
                    var wx = cx + Mathf.Cos(ang) * mid;
                    var wz = cz + Mathf.Sin(ang) * mid;
                    var m = IslandNaturalGeography.SampleMaskPublic(wx, wz, size, worldSeed);
                    if (m.LandMask > 0.5f)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                radii[i] = (lo + hi) * 0.5f;
            }

            var mean = 0f;
            for (var i = 0; i < samples; i++)
            {
                mean += radii[i];
            }

            mean /= samples;
            var varSum = 0f;
            var minR = float.MaxValue;
            var maxR = 0f;
            for (var i = 0; i < samples; i++)
            {
                var d = radii[i] - mean;
                varSum += d * d;
                minR = Mathf.Min(minR, radii[i]);
                maxR = Mathf.Max(maxR, radii[i]);
            }

            var std = Mathf.Sqrt(varSum / samples);
            var aspect = maxR / Mathf.Max(1f, minR);
            Assert.Greater(std / mean, 0.04f, "Coastline radii should vary (not a circle).");
            Assert.Greater(aspect, 1.12f, "Bounding radial extent should be asymmetric.");
        }

        [Test]
        public void Framing_PreservesRelativeVistaRelief()
        {
            var terrain = CreateTempWorldTerrain(129);
            try
            {
                // Artificial Vista-like slope: low SW → high NE.
                var data = terrain.terrainData;
                var res = data.heightmapResolution;
                var heights = new float[res, res];
                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        heights[z, x] = (x + z) / (float)(2 * (res - 1));
                    }
                }

                TerrainHeightWrite.Commit(terrain, heights);
                IslandNaturalGeography.Apply(terrain, 555121, true);
                var after = data.GetHeights(0, 0, res, res);
                var sea01 = WorldScale.SeaLevelMeters / data.size.y;
                // Interior sample near world center should stay above sea and keep NE > SW trend.
                var mid = res / 2;
                Assert.Greater(after[mid, mid], sea01);
                Assert.Greater(after[res - 8, res - 8], after[8, 8]);
            }
            finally
            {
                DestroyTempTerrain(terrain);
            }
        }

        [Test]
        public void WaterConnectivity_OceanTouchesBorder()
        {
            var terrain = CreateTempWorldTerrain(129);
            try
            {
                FillFlat(terrain, 0.45f);
                IslandNaturalGeography.Apply(terrain, 333001, true);
                IslandOccupancy.ApplyBoundarySubmersion(
                    terrain, new WorldSeed(333001).Fork("terrain.island").Next());
                var result = WaterConnectivity.Analyze(terrain);
                Assert.IsTrue(result.BorderSubmerged);
                Assert.Greater(result.OceanCells, 0);
            }
            finally
            {
                DestroyTempTerrain(terrain);
            }
        }

        private static float[,] BuildHeights(int seed, bool mountains)
        {
            var terrain = CreateTempWorldTerrain(129);
            try
            {
                FillFlat(terrain, 0.35f);
                IslandNaturalGeography.Apply(terrain, seed, mountains);
                var data = terrain.terrainData;
                return data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution);
            }
            finally
            {
                DestroyTempTerrain(terrain);
            }
        }

        private static void FillFlat(UnityEngine.Terrain terrain, float h01)
        {
            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var heights = new float[res, res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    heights[z, x] = h01;
                }
            }

            TerrainHeightWrite.Commit(terrain, heights);
        }

        /// <summary>One terrain covering full world extent (simulates framed island without 2×2).</summary>
        private static UnityEngine.Terrain CreateTempWorldTerrain(int resolution)
        {
            var data = new TerrainData
            {
                heightmapResolution = resolution,
                size = new Vector3(
                    WorldScale.WorldExtentXMeters,
                    WorldScale.TerrainHeightBudgetMeters,
                    WorldScale.WorldExtentZMeters),
            };
            var go = UnityEngine.Terrain.CreateTerrainGameObject(data);
            go.name = "TempNaturalIslandTerrain";
            go.transform.position = Vector3.zero;
            return go.GetComponent<UnityEngine.Terrain>();
        }

        private static void DestroyTempTerrain(UnityEngine.Terrain terrain)
        {
            if (terrain == null)
            {
                return;
            }

            var data = terrain.terrainData;
            Object.DestroyImmediate(terrain.gameObject);
            if (data != null)
            {
                Object.DestroyImmediate(data);
            }
        }
    }
}
