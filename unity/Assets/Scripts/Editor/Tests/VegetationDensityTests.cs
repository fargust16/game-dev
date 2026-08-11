using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class VegetationDensityTests
    {
        [Test]
        public void SamplePatch_StaysWithinZeroToOne()
        {
            for (var x = -500f; x <= 500f; x += 37f)
            {
                for (var z = -500f; z <= 500f; z += 41f)
                {
                    var v = VegetationDensity.SamplePatch(x, z, 12.3f, -45.6f);
                    Assert.GreaterOrEqual(v, 0f);
                    Assert.LessOrEqual(v, 1f);
                }
            }
        }

        [Test]
        public void SamplePatch_IsSmoothBetweenNeighboringPoints()
        {
            // Neighbouring samples (a few metres apart, well inside one ~50m patch) should be
            // close in value - proves this is a smooth field, not per-point noise/static.
            var a = VegetationDensity.SamplePatch(100f, 100f, 0f, 0f);
            var b = VegetationDensity.SamplePatch(102f, 100f, 0f, 0f);
            Assert.Less(System.Math.Abs(a - b), 0.1f);
        }

        [Test]
        public void SamplePatch_DifferentOffsets_ProduceDifferentFields()
        {
            // Different seeds must actually shift the pattern, or every generation would place
            // vegetation clumps in identical spots regardless of seed. Compare across many points
            // rather than a single one - Mathf.PerlinNoise can coincidentally return an equal value
            // at an isolated point (observed: (100,100) with these exact two offsets happened to
            // land on the same value) even though the two fields are genuinely different overall.
            var anyDifferent = false;
            for (var x = 0f; x < 500f && !anyDifferent; x += 23f)
            {
                for (var z = 0f; z < 500f && !anyDifferent; z += 29f)
                {
                    var a = VegetationDensity.SamplePatch(x, z, 0f, 0f);
                    var b = VegetationDensity.SamplePatch(x, z, 5000f, -3000f);
                    if (!Mathf.Approximately(a, b))
                    {
                        anyDifferent = true;
                    }
                }
            }

            Assert.IsTrue(anyDifferent, "Different seeds produced an identical density field across every sampled point - suspiciously non-random.");
        }

        [Test]
        public void SlopeMultiplier_FlatGround_IsFullStrength()
        {
            Assert.AreEqual(1f, VegetationDensity.SlopeMultiplier(0f), 1e-5f);
        }

        [Test]
        public void SlopeMultiplier_AtOrAboveMaxSlope_IsZero()
        {
            Assert.AreEqual(0f, VegetationDensity.SlopeMultiplier(VegetationDensity.MaxSlope01), 1e-5f);
            Assert.AreEqual(0f, VegetationDensity.SlopeMultiplier(1f), 1e-5f);
        }

        [Test]
        public void SlopeMultiplier_DecreasesMonotonicallyWithSlope()
        {
            var gentle = VegetationDensity.SlopeMultiplier(0.1f);
            var steep = VegetationDensity.SlopeMultiplier(0.4f);
            Assert.Greater(gentle, steep);
        }

        [Test]
        public void SlopeMultiplier_ClampsOutOfRangeInput()
        {
            Assert.AreEqual(1f, VegetationDensity.SlopeMultiplier(-1f), 1e-5f);
            Assert.AreEqual(0f, VegetationDensity.SlopeMultiplier(5f), 1e-5f);
        }
    }
}
