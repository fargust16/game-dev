using System.Collections.Generic;
using GameDev.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Vegetation.Tests
{
    public sealed class GrassDistributionTests
    {
        [Test]
        public void CellMeters_TargetsDenseLawnOverlap()
        {
            // ~6 clumps/m² with 1.2 m mesh — lawn in meadows; forests thin via mask.
            var perSq = 1f / (GrassDistribution.CellMeters * GrassDistribution.CellMeters);
            Assert.Greater(perSq, 5f);
            Assert.Less(perSq, 12f);
        }

        [Test]
        public void EmptyChunk_HasZeroCount()
        {
            var empty = GrassChunk.Empty(0, 0, Vector3.zero);
            Assert.AreEqual(0, empty.Count);
            Assert.AreEqual(0, empty.Matrices.Length);
        }

        [Test]
        public void ForkGrass_IsIndependentOfVegetationStream()
        {
            var seed = new GameDev.World.Seed.WorldSeed(110201);
            var grassA = new List<int>();
            var grassB = new List<int>();
            var veg = new List<int>();

            var g1 = seed.Fork("grass");
            var g2 = seed.Fork("grass");
            var v1 = seed.Fork("vegetation");
            for (var i = 0; i < 8; i++)
            {
                grassA.Add(g1.Next());
                grassB.Add(g2.Next());
                veg.Add(v1.Next());
            }

            CollectionAssert.AreEqual(grassA, grassB);
            CollectionAssert.AreNotEqual(grassA, veg);
        }
        [Test]
        public void SampleMaskBilinear_CornerValues_Interpolate()
        {
            var mask = new float[2, 2];
            mask[0, 0] = 0f;
            mask[1, 0] = 1f;
            mask[0, 1] = 0f;
            mask[1, 1] = 1f;
            var origin = Vector3.zero;
            var mid = GrassDistribution.SampleMaskBilinear(
                mask, 2, origin, 64f, GrassDistribution.MaskSampleMeters * 0.5f, 0f);
            Assert.AreEqual(0.5f, mid, 0.05f);
        }
    }
}
