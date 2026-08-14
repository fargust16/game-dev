using GameDev.World;
using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakePlacementAndShapeTests
    {
        [Test]
        public void LakePlacementScore_BasinBeatsPeak()
        {
            const int w = 64;
            const int d = 64;
            var heights = new float[w, d];
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    // Gentle plains — avoid a global mountain bowl that false-rejects basins.
                    heights[x, z] = 80f + 0.002f * ((x - 32) * (x - 32) + (z - 32) * (z - 32));
                }
            }

            // Carve a soft basin at (20,20).
            for (var x = 12; x <= 28; x++)
            {
                for (var z = 12; z <= 28; z++)
                {
                    var dx = x - 20;
                    var dz = z - 20;
                    heights[x, z] = 70f + 0.05f * (dx * dx + dz * dz);
                }
            }

            var basin = DistributedLakePlacer.LakePlacementScore(heights, w, d, 20, 20, 3f);
            var peak = DistributedLakePlacer.LakePlacementScore(heights, w, d, 50, 50, 3f);
            Assert.False(float.IsNegativeInfinity(basin), "soft basin must remain a valid candidate");
            Assert.Greater(basin, peak);
        }

        [Test]
        public void LakePlacementScore_MountainFlank_IsHardRejected()
        {
            const int w = 64;
            const int d = 64;
            var heights = new float[w, d];
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    // Steep mountain wall: height rises sharply with x.
                    heights[x, z] = 40f + x * 2.5f;
                }
            }

            var score = DistributedLakePlacer.LakePlacementScore(heights, w, d, 32, 32, 3f);
            Assert.IsTrue(float.IsNegativeInfinity(score));
        }

        [Test]
        public void Place_WithWorldSeed_IsDeterministicAndElongated()
        {
            float Sampler(float x, float z) =>
                80f + Mathf.Sin(x * 0.008f) * 6f + Mathf.Cos(z * 0.007f) * 5f;

            var settings = WorldGenSettings.Default;
            settings.LakeCount = 3;
            settings.ShapeIrregularity = 0.35f;
            settings.TerrainConformity = 0.75f;

            // Enclosing GameDev.World.WorldSeed shadows Seed.WorldSeed — qualify explicitly.
            global::GameDev.World.Seed.IWorldSeed seed = new global::GameDev.World.Seed.WorldSeed(1847459267);
            var a = DistributedLakePlacer.Place(Sampler, 0, 0, 900, 900, 3f, seed, settings);
            var b = DistributedLakePlacer.Place(Sampler, 0, 0, 900, 900, 3f, seed, settings);

            Assert.AreEqual(a.Length, b.Length);
            Assert.GreaterOrEqual(a.Length, WorldScale.LakeMinCount);

            for (var i = 0; i < a.Length; i++)
            {
                var ca = a[i].GetFloodCentroidXZ();
                var cb = b[i].GetFloodCentroidXZ();
                Assert.AreEqual(ca.x, cb.x, 0.01f);
                Assert.AreEqual(ca.z, cb.z, 0.01f);

                var sil = a[i].Silhouette;
                Assert.IsNotNull(sil);
                // Silhouette aspect = 1 / aspectRatio; aspectRatio ∈ [1.4, 2.2] → [~0.45, ~0.71]
                Assert.LessOrEqual(sil.Aspect, 0.75f);
                Assert.GreaterOrEqual(sil.Aspect, 0.40f);
            }
        }

        [Test]
        public void ShapeIrregularity_Zero_IsNearEllipse()
        {
            var s = new LakeSilhouette(
                0f, 0f, 80f,
                aspect: 1f / 1.8f,
                rotationRadians: 0.3f,
                phase1: 0.2f, phase2: 1.1f, phase3: 2.3f,
                amp1: 0f, amp2: 0f, amp3: 0f,
                noiseSeed: 42,
                hasBay: false,
                bayCenterX: 0f, bayCenterZ: 0f, bayRadiusMeters: 0f);

            var prevR = -1f;
            var maxJump = 0f;
            const int samples = 72;
            for (var i = 0; i < samples; i++)
            {
                var ang = i * Mathf.PI * 2f / samples;
                var r = s.ShoreRadiusAtAngle(ang);
                if (prevR > 0f)
                {
                    maxJump = Mathf.Max(maxJump, Mathf.Abs(r - prevR));
                }

                prevR = r;
            }

            Assert.Less(maxJump, 4f);
        }

        [Test]
        public void ShapeIrregularity_Mid_HasSmoothBendsNotTeeth()
        {
            var irregularity = 0.45f;
            var s = new LakeSilhouette(
                0f, 0f, 80f,
                aspect: 1f / 1.7f,
                rotationRadians: 0.4f,
                phase1: 0.2f, phase2: 1.1f, phase3: 2.3f,
                amp1: 0.12f * irregularity,
                amp2: 0.06f * irregularity,
                amp3: 0.025f * irregularity,
                noiseSeed: 42,
                hasBay: false,
                bayCenterX: 0f, bayCenterZ: 0f, bayRadiusMeters: 0f);

            var prevR = -1f;
            var maxJump = 0f;
            var minR = float.PositiveInfinity;
            var maxR = 0f;
            const int samples = 72;
            for (var i = 0; i < samples; i++)
            {
                var ang = i * Mathf.PI * 2f / samples;
                var r = s.ShoreRadiusAtAngle(ang);
                minR = Mathf.Min(minR, r);
                maxR = Mathf.Max(maxR, r);
                if (prevR > 0f)
                {
                    maxJump = Mathf.Max(maxJump, Mathf.Abs(r - prevR));
                }

                prevR = r;
            }

            Assert.Greater(maxR - minR, 2f);
            Assert.Less(maxJump, 8f);
        }

        [Test]
        public void PerLakeSeed_ChangingNeighborDoesNotReshapeExisting()
        {
            float Sampler(float x, float z) => 50f + Mathf.Sin(x * 0.01f) * 3f;
            var settings = WorldGenSettings.Default;
            settings.LakeCount = 3;

            global::GameDev.World.Seed.IWorldSeed seed = new global::GameDev.World.Seed.WorldSeed(99);
            var one = DistributedLakePlacer.Place(Sampler, 0, 0, 700, 700, 3f, seed, settings);
            settings.LakeCount = 4;
            var two = DistributedLakePlacer.Place(Sampler, 0, 0, 700, 700, 3f, seed, settings);

            Assert.GreaterOrEqual(one.Length, WorldScale.LakeMinCount);
            Assert.GreaterOrEqual(two.Length, WorldScale.LakeMinCount);
            // lake_0 uses Fork("lake", 0) — shape params should match for the first lake.
            Assert.AreEqual(one[0].Silhouette.Aspect, two[0].Silhouette.Aspect, 0.001f);
            Assert.AreEqual(one[0].Silhouette.RotationRadians, two[0].Silhouette.RotationRadians, 0.001f);
            Assert.AreEqual(one[0].Silhouette.Amp1, two[0].Silhouette.Amp1, 0.0001f);
        }

        [Test]
        public void Place_SizeTiers_VaryRadiiAcrossFirstThreeLakes()
        {
            // Flat bowl so hard placement always finds 3 sites.
            float Sampler(float x, float z) =>
                60f + 0.00002f * ((x - 350f) * (x - 350f) + (z - 350f) * (z - 350f));

            var settings = WorldGenSettings.Default;
            settings.LakeCount = 3;
            global::GameDev.World.Seed.IWorldSeed seed = new global::GameDev.World.Seed.WorldSeed(424242);
            var zones = DistributedLakePlacer.Place(Sampler, 0, 0, 700, 700, 4f, seed, settings);
            Assert.GreaterOrEqual(zones.Length, WorldScale.LakeMinCount);

            var radii = new float[Mathf.Min(3, zones.Length)];
            for (var i = 0; i < radii.Length; i++)
            {
                radii[i] = zones[i].Silhouette.RadiusMeters;
            }

            // Large → Medium → Small ordering for first three indices.
            Assert.Greater(radii[0], radii[2], "large tier should exceed small tier");
            Assert.GreaterOrEqual(radii[0], radii[1] - 1f);
            Assert.GreaterOrEqual(radii[1], radii[2] - 1f);
        }
    }
}
