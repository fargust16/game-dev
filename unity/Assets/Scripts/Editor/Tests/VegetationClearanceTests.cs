using System.Collections.Generic;
using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class VegetationClearanceTests
    {
        [Test]
        public void FalloffMultiplier_InsideClearRadius_IsZeroByDefault()
        {
            Assert.AreEqual(0f, VegetationClearance.FalloffMultiplier(2f, 5f, 10f), 1e-5f);
        }

        [Test]
        public void FalloffMultiplier_InsideClearRadius_HonorsMinAtCore()
        {
            Assert.AreEqual(
                0.12f,
                VegetationClearance.FalloffMultiplier(2f, 5f, 10f, minAtCore: 0.12f),
                1e-5f);
        }

        [Test]
        public void FalloffMultiplier_BeyondFalloff_IsOne()
        {
            Assert.AreEqual(1f, VegetationClearance.FalloffMultiplier(20f, 5f, 10f), 1e-5f);
            Assert.AreEqual(
                1f,
                VegetationClearance.FalloffMultiplier(20f, 5f, 10f, minAtCore: 0.12f),
                1e-5f);
        }

        [Test]
        public void FalloffMultiplier_MidBand_IsBetween()
        {
            var mid = VegetationClearance.FalloffMultiplier(10f, 5f, 10f);
            Assert.Greater(mid, 0f);
            Assert.Less(mid, 1f);

            var midResidual = VegetationClearance.FalloffMultiplier(10f, 5f, 10f, minAtCore: 0.12f);
            Assert.Greater(midResidual, 0.12f);
            Assert.Less(midResidual, 1f);
        }

        [Test]
        public void Multiplier_DiskClearsCenter()
        {
            var clearance = new VegetationClearance(
                new List<VegetationClearance.Disk>
                {
                    new VegetationClearance.Disk(new Vector2(0f, 0f), 8f, 12f),
                },
                System.Array.Empty<Vector2>(),
                0f,
                1f);
            Assert.AreEqual(0f, clearance.Multiplier(0f, 0f), 1e-5f);
            Assert.AreEqual(1f, clearance.Multiplier(40f, 0f), 1e-5f);
        }

        [Test]
        public void Multiplier_RoadSamplesBareAtCenter()
        {
            var roads = new List<Vector2>();
            for (var x = 0f; x <= 100f; x += 5f)
            {
                roads.Add(new Vector2(x, 0f));
            }

            var clearance = new VegetationClearance(
                System.Array.Empty<VegetationClearance.Disk>(),
                roads,
                4f,
                6f,
                roadMinAtCore: GrassMask.RoadLawnMinAtCenter);
            Assert.AreEqual(
                GrassMask.RoadLawnMinAtCenter,
                clearance.Multiplier(50f, 0f),
                1e-5f);
            Assert.AreEqual(1f, clearance.Multiplier(50f, 30f), 1e-5f);
        }
    }
}
