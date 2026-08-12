using GameDev.World.Terrain;
using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class VegetationEcologyTests
    {
        private static VegetationEcology.NoiseOffsets FixedOffsets =>
            new VegetationEcology.NoiseOffsets(10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f);

        [Test]
        public void Evaluate_OutputsStayWithinZeroToOne()
        {
            for (var x = 0f; x < 800f; x += 73f)
            {
                for (var z = 0f; z < 800f; z += 91f)
                {
                    var s = VegetationEcology.Evaluate(
                        x, z, BiomeId.Forest, 0.1f, 0.6f, 0.2f, FixedOffsets);
                    Assert.GreaterOrEqual(s.Forest, 0f);
                    Assert.LessOrEqual(s.Forest, 1f);
                    Assert.GreaterOrEqual(s.Shrub, 0f);
                    Assert.LessOrEqual(s.Shrub, 1f);
                    Assert.GreaterOrEqual(s.Grass, 0f);
                    Assert.LessOrEqual(s.Grass, 1f);
                }
            }
        }

        [Test]
        public void Evaluate_SteepSlope_KillsVegetation()
        {
            var s = VegetationEcology.Evaluate(
                100f, 100f, BiomeId.Forest, VegetationDensity.MaxSlope01, 0.7f, 0f, FixedOffsets);
            Assert.AreEqual(0f, s.Forest, 1e-5f);
            Assert.AreEqual(0f, s.Shrub, 1e-5f);
            Assert.AreEqual(0f, s.Grass, 1e-5f);
        }

        [Test]
        public void Evaluate_ForestBiome_HasHigherPeakForestThanMeadow()
        {
            var forestMax = 0f;
            var meadowMax = 0f;
            for (var x = 0f; x < 1200f; x += 47f)
            {
                for (var z = 0f; z < 1200f; z += 53f)
                {
                    forestMax = Mathf.Max(
                        forestMax,
                        VegetationEcology.Evaluate(x, z, BiomeId.Forest, 0.05f, 0.65f, 0f, FixedOffsets).Forest);
                    meadowMax = Mathf.Max(
                        meadowMax,
                        VegetationEcology.Evaluate(x, z, BiomeId.Meadow, 0.05f, 0.35f, 0f, FixedOffsets).Forest);
                }
            }

            Assert.Greater(forestMax, 0.55f, "Forest biome never formed a dense forest region.");
            Assert.Greater(forestMax, meadowMax + 0.15f, "Meadow trees rival forest masses.");
        }

        [Test]
        public void Evaluate_ShrubIsNotIdenticalToForest()
        {
            var identical = 0;
            var compared = 0;
            for (var x = 0f; x < 600f; x += 41f)
            {
                for (var z = 0f; z < 600f; z += 43f)
                {
                    var s = VegetationEcology.Evaluate(
                        x, z, BiomeId.Forest, 0.08f, 0.55f, 0.1f, FixedOffsets);
                    compared++;
                    if (Mathf.Abs(s.Forest - s.Shrub) < 0.02f)
                    {
                        identical++;
                    }
                }
            }

            Assert.Less(identical / (float)compared, 0.35f, "Shrub density collapsed into tree density.");
        }

        [Test]
        public void Evaluate_DenseForest_SuppressesGrassRelativeToOpen()
        {
            VegetationEcology.Sample dense = default;
            VegetationEcology.Sample open = default;
            var foundDense = false;
            var foundOpen = false;

            for (var x = 0f; x < 1400f && !(foundDense && foundOpen); x += 37f)
            {
                for (var z = 0f; z < 1400f && !(foundDense && foundOpen); z += 41f)
                {
                    var s = VegetationEcology.Evaluate(
                        x, z, BiomeId.Forest, 0.05f, 0.6f, 0f, FixedOffsets);
                    if (!foundDense && s.Forest > 0.7f)
                    {
                        dense = s;
                        foundDense = true;
                    }

                    if (!foundOpen && s.Forest < 0.2f && s.Grass > 0.2f)
                    {
                        open = s;
                        foundOpen = true;
                    }
                }
            }

            Assert.IsTrue(foundDense, "No dense forest sample found.");
            Assert.IsTrue(foundOpen, "No open/grass sample found.");
            Assert.GreaterOrEqual(open.Grass, dense.Grass - 0.05f, "Open grass should not be weaker than canopy grass.");
            Assert.Greater(dense.Grass, 0.15f, "Dense forest still needs understory grass cover.");
        }

        [Test]
        public void RemapForestStates_PreservesOrder()
        {
            var a = VegetationEcology.RemapForestStates(0.1f);
            var b = VegetationEcology.RemapForestStates(0.5f);
            var c = VegetationEcology.RemapForestStates(0.9f);
            Assert.Less(a, b);
            Assert.Less(b, c);
        }

        [Test]
        public void MacroField_IsSmoothOverShortDistances()
        {
            var a = VegetationEcology.Evaluate(200f, 200f, BiomeId.Forest, 0.05f, 0.6f, 0f, FixedOffsets);
            var b = VegetationEcology.Evaluate(205f, 200f, BiomeId.Forest, 0.05f, 0.6f, 0f, FixedOffsets);
            Assert.Less(Mathf.Abs(a.Forest - b.Forest), 0.2f);
        }
    }
}
