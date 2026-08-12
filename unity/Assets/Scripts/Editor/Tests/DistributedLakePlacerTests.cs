using System;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.Water.Tests
{
    public sealed class DistributedLakePlacerTests
    {
        [Test]
        public void Place_RespectsMinCountAndSeparation()
        {
            // Gentle bowl field — every cell is a valid site.
            float Sampler(float x, float z) => 100f + 0.01f * (x + z);

            var zones = DistributedLakePlacer.Place(
                Sampler,
                minX: 0f,
                minZ: 0f,
                sizeX: 900f,
                sizeZ: 900f,
                cellSize: 3f,
                rng: new Random(42),
                minLakes: 2,
                targetLakes: 3);

            Assert.GreaterOrEqual(zones.Length, 2);
            Assert.LessOrEqual(zones.Length, 3);

            for (var i = 0; i < zones.Length; i++)
            {
                for (var j = i + 1; j < zones.Length; j++)
                {
                    var a = zones[i].GetFloodCentroidXZ();
                    var b = zones[j].GetFloodCentroidXZ();
                    var dx = a.x - b.x;
                    var dz = a.z - b.z;
                    Assert.Greater(Mathf.Sqrt(dx * dx + dz * dz), 250f);
                }
            }
        }

        [Test]
        public void Place_IsDeterministicForSameSeed()
        {
            float Sampler(float x, float z) => 50f + Mathf.Sin(x * 0.01f) * 3f;

            var a = DistributedLakePlacer.Place(Sampler, 0, 0, 600, 600, 3f, new Random(7), 2, 3);
            var b = DistributedLakePlacer.Place(Sampler, 0, 0, 600, 600, 3f, new Random(7), 2, 3);
            Assert.AreEqual(a.Length, b.Length);
            for (var i = 0; i < a.Length; i++)
            {
                var ca = a[i].GetFloodCentroidXZ();
                var cb = b[i].GetFloodCentroidXZ();
                Assert.AreEqual(ca.x, cb.x, 0.01f);
                Assert.AreEqual(ca.z, cb.z, 0.01f);
            }
        }
    }
}
