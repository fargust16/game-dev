using GameDev.World.Water;
using NUnit.Framework;
using UnityEngine;

namespace GameDev.World.Water.Tests
{
    public sealed class LakeSilhouetteTests
    {
        private static LakeSilhouette MakeCircle(float cx, float cz, float radius)
        {
            return new LakeSilhouette(
                cx, cz, radius,
                aspect: 1f,
                rotationRadians: 0f,
                phase1: 0f, phase2: 0f, phase3: 0f,
                amp1: 0f, amp2: 0f, amp3: 0f,
                noiseSeed: 1,
                hasBay: false,
                bayCenterX: 0f, bayCenterZ: 0f, bayRadiusMeters: 0f);
        }

        [Test]
        public void SignedDistance_CenterIsDeeplyInside()
        {
            var s = MakeCircle(0f, 0f, 50f);
            Assert.Greater(s.SignedDistance(0f, 0f), 40f);
        }

        [Test]
        public void SignedDistance_FarOutsideIsNegative()
        {
            var s = MakeCircle(0f, 0f, 50f);
            Assert.Less(s.SignedDistance(200f, 0f), -100f);
        }

        [Test]
        public void IsoLine_IsSmoothNotAxisAlignedStairs()
        {
            // Angular wobble shore — sample the zero crossing along a ring and ensure
            // consecutive radius samples do not jump by a full 3 m cell (pixel stair).
            var s = new LakeSilhouette(
                0f, 0f, 80f,
                aspect: 0.75f,
                rotationRadians: 0.4f,
                phase1: 0.2f, phase2: 1.1f, phase3: 2.3f,
                amp1: 0.22f, amp2: 0.12f, amp3: 0.08f,
                noiseSeed: 42,
                hasBay: false,
                bayCenterX: 0f, bayCenterZ: 0f, bayRadiusMeters: 0f);

            var prevR = -1f;
            var maxJump = 0f;
            const int samples = 72;
            for (var i = 0; i < samples; i++)
            {
                var ang = i * Mathf.PI * 2f / samples;
                // Binary-search radius of zero crossing along this ray.
                var lo = 20f;
                var hi = 160f;
                for (var iter = 0; iter < 24; iter++)
                {
                    var mid = (lo + hi) * 0.5f;
                    var wx = Mathf.Cos(ang) * mid;
                    var wz = Mathf.Sin(ang) * mid;
                    if (s.SignedDistance(wx, wz) >= 0f)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                var r = (lo + hi) * 0.5f;
                if (prevR > 0f)
                {
                    maxJump = Mathf.Max(maxJump, Mathf.Abs(r - prevR));
                }

                prevR = r;
            }

            Assert.Less(maxJump, 1.5f, "shore radius must be a continuous curve (no 3m cell stairs)");
        }

        [Test]
        public void ShoreRadiusAtAngle_ChangesContinuously()
        {
            var s = new LakeSilhouette(
                0f, 0f, 100f,
                aspect: 0.8f,
                rotationRadians: 0.3f,
                phase1: 0.5f, phase2: 1.2f, phase3: 2.0f,
                amp1: 0.2f, amp2: 0.1f, amp3: 0.07f,
                noiseSeed: 7,
                hasBay: true,
                bayCenterX: 40f, bayCenterZ: 10f, bayRadiusMeters: 35f);

            var prev = s.ShoreRadiusAtAngle(0f);
            var maxJump = 0f;
            const int n = 120;
            for (var i = 1; i <= n; i++)
            {
                var ang = i * Mathf.PI * 2f / n;
                var r = s.ShoreRadiusAtAngle(ang);
                maxJump = Mathf.Max(maxJump, Mathf.Abs(r - prev));
                prev = r;
            }

            Assert.Less(maxJump, 2f, "angular shore radius samples must not jump like a grid stair");
        }

        [Test]
        public void WaterMesh_WithSilhouette_CoversExpandedBasin()
        {
            var silhouette = MakeCircle(30f, 30f, 18f);
            var size = 40;
            var cell = 2f;
            var flooded = new bool[size, size];
            var heights = new float[size, size];
            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    var wx = (x + 0.5f) * cell;
                    var wz = (z + 0.5f) * cell;
                    flooded[x, z] = silhouette.Contains(wx, wz);
                    heights[x, z] = 5f;
                }
            }

            var zone = new WaterZone("sil", 10f, 0f, 0f, cell, flooded, heights, silhouette);
            var mesh = WaterMeshBuilder.Build(zone, new[] { zone });
            Assert.Greater(mesh.vertexCount, 0);

            // Every flooded cell centre must lie inside the mesh AABB inflated sheet.
            var minX = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var minZ = float.PositiveInfinity;
            var maxZ = float.NegativeInfinity;
            foreach (var v in mesh.vertices)
            {
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.z < minZ) minZ = v.z;
                if (v.z > maxZ) maxZ = v.z;
            }

            for (var x = 0; x < size; x++)
            {
                for (var z = 0; z < size; z++)
                {
                    if (!flooded[x, z])
                    {
                        continue;
                    }

                    var wx = (x + 0.5f) * cell;
                    var wz = (z + 0.5f) * cell;
                    Assert.GreaterOrEqual(wx, minX - 0.01f);
                    Assert.LessOrEqual(wx, maxX + 0.01f);
                    Assert.GreaterOrEqual(wz, minZ - 0.01f);
                    Assert.LessOrEqual(wz, maxZ + 0.01f);
                }
            }
        }
    }
}
