using GameDev.World;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Terrain.Tests
{
    public sealed class CoastalBeachShelfTests
    {
        [Test]
        public void SmootherStep_IsZeroAtEndsAndHalfAtMid()
        {
            Assert.AreEqual(0f, CoastalBeachShelf.SmootherStep(0f), 1e-5f);
            Assert.AreEqual(1f, CoastalBeachShelf.SmootherStep(1f), 1e-5f);
            Assert.AreEqual(0.5f, CoastalBeachShelf.SmootherStep(0.5f), 1e-4f);
        }

        [Test]
        public void SmootherStep_DerivativeNearEnds_IsNearZero()
        {
            const float eps = 1e-3f;
            var d0 = (CoastalBeachShelf.SmootherStep(eps) - CoastalBeachShelf.SmootherStep(0f)) / eps;
            var d1 = (CoastalBeachShelf.SmootherStep(1f) - CoastalBeachShelf.SmootherStep(1f - eps)) / eps;
            Assert.Less(Mathf.Abs(d0), 0.05f, "start gradient should be near 0");
            Assert.Less(Mathf.Abs(d1), 0.05f, "end gradient should be near 0");
        }

        [Test]
        public void BeachArcWeight_ZeroAtOppositeAzimuth()
        {
            Assert.AreEqual(0f, CoastalBeachShelf.BeachArcWeight(40f + 180f, 40f), 0.001f);
            Assert.AreEqual(1f, CoastalBeachShelf.BeachArcWeight(40f, 40f), 0.02f);
        }

        [Test]
        public void CoastSmoothConstants_AreSane()
        {
            Assert.Greater(CoastalBeachShelf.CoastTransitionMaxMeters, CoastalBeachShelf.MinBeachWidthMeters);
            Assert.Less(CoastalBeachShelf.TargetBeachSlopeDegrees, 15f);
        }

        [Test]
        public void CliffAndBeachYaw_AreOppositeAndDeterministic()
        {
            const int seed = 110201;
            var a = CoastalBeachShelf.SampleCliffCoastYawRadians(seed);
            var b = CoastalBeachShelf.SampleCliffCoastYawRadians(seed);
            Assert.AreEqual(a, b, 1e-6f);
            var beach = CoastalBeachShelf.SampleBeachYawDegrees(seed);
            var fromCliff = CoastalBeachShelf.BeachYawDegreesFromCliffRadians(a);
            Assert.AreEqual(fromCliff, beach, 1e-3f);
            var delta = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, beach));
            Assert.Greater(delta, 170f);
            Assert.Less(delta, 190f);
        }

        [Test]
        public void DirectedCoast_DoesNotCreateConstantHeightTerrace()
        {
            // Synthetic: gentle ramp from sea toward plateau on +X.
            var data = new TerrainData
            {
                heightmapResolution = 65,
                size = new Vector3(640f, WorldScale.TerrainHeightBudgetMeters, 640f)
            };
            var res = data.heightmapResolution;
            var sea01 = WorldScale.SeaLevelMeters / data.size.y;
            var plateau01 = (WorldScale.SeaLevelMeters + 40f) / data.size.y;
            var heights = new float[res, res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var u = x / (float)(res - 1);
                    if (u < 0.25f)
                        heights[z, x] = sea01 * 0.5f;
                    else if (u < 0.55f)
                    {
                        var t = (u - 0.25f) / 0.3f;
                        heights[z, x] = Mathf.Lerp(sea01 * 0.5f, plateau01, t);
                    }
                    else
                        heights[z, x] = plateau01 + 0.01f * Mathf.Sin(z * 0.4f);
                }
            }

            data.SetHeights(0, 0, heights);
            var terrainGo = UnityEngine.Terrain.CreateTerrainGameObject(data);
            try
            {
                var terrain = terrainGo.GetComponent<UnityEngine.Terrain>();
                terrain.transform.position = Vector3.zero;

                CoastalBeachShelf.ApplyAll(new[] { terrain }, 110201);

                var after = data.GetHeights(0, 0, res, res);
                var midZ = res / 2;
                var run = 1;
                var maxRun = 1;
                for (var x = 1; x < res; x++)
                {
                    if (Mathf.Abs(after[midZ, x] - after[midZ, x - 1]) < 1e-5f)
                        run++;
                    else
                        run = 1;
                    if (run > maxRun)
                        maxRun = run;
                }

                Assert.Less(maxRun, 14, "directed coast must not leave a flat terrace run");

                var shoreX = Mathf.RoundToInt(0.28f * (res - 1));
                var inlandX = Mathf.RoundToInt(0.5f * (res - 1));
                Assert.Less(after[midZ, shoreX], after[midZ, inlandX]);
            }
            finally
            {
                Object.DestroyImmediate(terrainGo);
            }
        }

        [Test]
        public void BeachArcWeight_IsClamped01_NeverNegative()
        {
            Assert.AreEqual(1f, CoastalBeachShelf.BeachArcWeight(-54f, -54f), 0.02f);
            Assert.AreEqual(1f, CoastalBeachShelf.BeachArcWeight(0f, 22f), 0.02f);
            Assert.GreaterOrEqual(CoastalBeachShelf.BeachArcWeight(0f, -54f), 0f);
            Assert.LessOrEqual(CoastalBeachShelf.BeachArcWeight(0f, -54f), 1f);
            Assert.AreEqual(0f, CoastalBeachShelf.BeachArcWeight(202f, 22f), 0.001f);
        }

        [Test]
        public void TargetBeachSlope_IsUnderFifteenDegrees()
        {
            Assert.Less(CoastalBeachShelf.TargetBeachSlopeDegrees, 15f);
            Assert.Greater(CoastalBeachShelf.CoastTransitionMaxMeters, 300f);
        }

        [Test]
        public void NoBeachFlatMeters_ConstantRemovedFromApi()
        {
            // Regression: old BeachFlatMeters terrace API must stay gone.
            var type = typeof(CoastalBeachShelf);
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            Assert.IsNull(type.GetField("BeachFlatMeters", flags));
            Assert.IsNull(type.GetField("BeachRiseMeters", flags));
            Assert.IsNull(type.GetField("BeachSurfaceAboveSeaMeters", flags));
        }
    }
}
