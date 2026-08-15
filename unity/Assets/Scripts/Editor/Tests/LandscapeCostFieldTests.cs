using GameDev.World.Landscape;
using GameDev.World.Roads;
using GameDev.World.Terrain;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Landscape.Tests
{
    public sealed class LandscapeCostFieldTests
    {
        [Test]
        public void SlopeCost_IsNonlinear_SteepMuchDearerThanGentle()
        {
            var gentle = RoadCost.SlopeCost(0.1f);
            var mid = RoadCost.SlopeCost(0.3f);
            var steep = RoadCost.SlopeCost(0.6f);
            Assert.Greater(mid, gentle);
            Assert.Greater(steep, mid * 2f);
        }

        [Test]
        public void EarthworkCost_BlocksBeyondMaxCut()
        {
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.EarthworkCost(5f, 0f)));
            Assert.Greater(RoadCost.EarthworkCost(1.2f, 0f), RoadCost.EarthworkCost(0.2f, 0f));
        }

        [Test]
        public void CostField_BlocksInsideLakeAndPenalizesSteep()
        {
            // Quadratic bowl → steeper near edges.
            float Height(float x, float z) => (z * z) * 0.002f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;

            var field = new LandscapeCostField(
                0f, 0f, 100f, 100f, 10f, Height, Biome);

            var gentle = field.EvaluateAtTerrain(10f, 10f);
            var steep = field.EvaluateAtTerrain(10f, 90f);
            Assert.IsFalse(float.IsPositiveInfinity(gentle));
            Assert.Greater(steep, gentle);
        }

        [Test]
        public void VerticalProfile_ClampsGrade()
        {
            var xz = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(10f, 0f),
                new Vector2(20f, 0f),
            };
            float Sample(float x, float z) => x; // 100% grade raw
            var profile = RoadVerticalProfile.Build(xz, Sample, maxGrade: 0.12f, maxGradeChange: 0.05f);
            Assert.AreEqual(3, profile.Count);
            var g = (profile[1] - profile[0]) / 10f;
            Assert.LessOrEqual(Mathf.Abs(g), 0.12f + 1e-3f);
        }

        [Test]
        public void PaintExistingRoad_DiscountsCorridorCells()
        {
            float Height(float x, float z) => 10f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 200f, 200f, 10f, Height, Biome);
            var before = field.EvaluateAtTerrain(100f, 100f);
            field.PaintExistingRoad(new[]
            {
                new Vector2(0f, 100f),
                new Vector2(200f, 100f),
            });
            var after = field.EvaluateAtTerrain(100f, 100f);
            var off = field.EvaluateAtTerrain(100f, 160f);
            Assert.Less(after, before);
            Assert.Less(after, off);
        }

        [Test]
        public void SampleFootprint_ReportsFlatRatioOnFlatTerrain()
        {
            float Height(float x, float z) => 5f;
            BiomeId Biome(float x, float z) => BiomeId.Meadow;
            var field = new LandscapeCostField(0f, 0f, 200f, 200f, 10f, Height, Biome);
            var fp = field.SampleFootprint(100f, 100f, 40f);
            Assert.Greater(fp.SampleCount, 0);
            Assert.Greater(fp.FlatAreaRatio, 0.9f);
            Assert.Less(fp.MaxSlope01, 0.05f);
        }

        [Test]
        public void EnsureMinimumConnectivity_AttachesOrphan()
        {
            var nodes = new[] { new Vector2(0f, 0f), new Vector2(50f, 0f), new Vector2(25f, 40f) };
            var grid = new RoadGridSpec(0f, 0f, 20, 20, 5f);
            float Cost(float x, float z) => 1f;
            var segments = RoadNetworkBuilder.BuildNetwork(nodes, grid, Cost);
            var trimmed = new System.Collections.Generic.List<RoadSegment>();
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].NodeA == 2 || segments[i].NodeB == 2)
                {
                    continue;
                }

                trimmed.Add(segments[i]);
            }

            RoadNetworkBuilder.EnsureMinimumConnectivity(nodes, trimmed, grid, Cost);
            var degree2 = 0;
            for (var i = 0; i < trimmed.Count; i++)
            {
                if (trimmed[i].NodeA == 2 || trimmed[i].NodeB == 2)
                {
                    degree2++;
                }
            }

            Assert.GreaterOrEqual(degree2, 1);
        }
    }
}
