using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Continuous lake shoreline as one analytic curve (vector-graphics style), not a grid mask.
    /// Positive signed distance = inside water. Mesh and terrain carve share this same field so the
    /// shore and the excavated bowl stay aligned and smooth (trig harmonics + soft bay, no cell noise).
    /// </summary>
    public sealed class LakeSilhouette
    {
        public float CenterX { get; }
        public float CenterZ { get; }
        public float RadiusMeters { get; }
        public float Aspect { get; }
        public float RotationRadians { get; }
        public float Phase1 { get; }
        public float Phase2 { get; }
        public float Phase3 { get; }
        public float Amp1 { get; }
        public float Amp2 { get; }
        public float Amp3 { get; }
        public int NoiseSeed { get; }
        public bool HasBay { get; }
        public float BayCenterX { get; }
        public float BayCenterZ { get; }
        public float BayRadiusMeters { get; }

        public LakeSilhouette(
            float centerX,
            float centerZ,
            float radiusMeters,
            float aspect,
            float rotationRadians,
            float phase1,
            float phase2,
            float phase3,
            float amp1,
            float amp2,
            float amp3,
            int noiseSeed,
            bool hasBay,
            float bayCenterX,
            float bayCenterZ,
            float bayRadiusMeters)
        {
            CenterX = centerX;
            CenterZ = centerZ;
            RadiusMeters = Mathf.Max(1f, radiusMeters);
            Aspect = Mathf.Clamp(aspect, 0.35f, 1.5f);
            RotationRadians = rotationRadians;
            Phase1 = phase1;
            Phase2 = phase2;
            Phase3 = phase3;
            Amp1 = amp1;
            Amp2 = amp2;
            Amp3 = amp3;
            NoiseSeed = noiseSeed;
            HasBay = hasBay;
            BayCenterX = bayCenterX;
            BayCenterZ = bayCenterZ;
            BayRadiusMeters = Mathf.Max(0f, bayRadiusMeters);
        }

        /// <summary>
        /// Signed distance in metres: positive inside, negative outside.
        /// Zero iso-line is a C1-smooth closed curve (trig radius + optional soft-union bay).
        /// </summary>
        public float SignedDistance(float worldX, float worldZ)
        {
            var main = MainBodySigned(worldX, worldZ);
            if (!HasBay || BayRadiusMeters < 0.01f)
            {
                return main;
            }

            var bdx = worldX - BayCenterX;
            var bdz = worldZ - BayCenterZ;
            var bay = BayRadiusMeters - Mathf.Sqrt(bdx * bdx + bdz * bdz);
            // Smooth union (IQ) so the bay join is a curve, not a hard cusp — stable for metre-scale SDFs.
            // Larger k → softer bay join (less crease in the distance field / basin).
            return SoftMax(main, bay, k: 14f);
        }

        public bool Contains(float worldX, float worldZ) => SignedDistance(worldX, worldZ) >= 0f;

        /// <summary>Radius of the shore curve along a world-space angle (radians, around centre).</summary>
        public float ShoreRadiusAtAngle(float worldAngleRadians)
        {
            var local = worldAngleRadians - RotationRadians;
            // Map angle into the anisotropic frame used by MainBodySigned.
            var cos = Mathf.Cos(RotationRadians);
            var sin = Mathf.Sin(RotationRadians);
            var dirX = Mathf.Cos(worldAngleRadians);
            var dirZ = Mathf.Sin(worldAngleRadians);
            var rx = dirX * cos + dirZ * sin;
            var rz = (-dirX * sin + dirZ * cos) / Aspect;
            var ang = Mathf.Atan2(rz, rx);
            return RadiusMeters * Wobble(ang);
        }

        public Rect GetAabbXZ(float padMeters = 0f)
        {
            var pad = Mathf.Max(0f, padMeters);
            var extent = RadiusMeters * 1.55f / Mathf.Min(Aspect, 1f) + pad;
            var minX = CenterX - extent;
            var maxX = CenterX + extent;
            var minZ = CenterZ - extent;
            var maxZ = CenterZ + extent;
            if (HasBay)
            {
                minX = Mathf.Min(minX, BayCenterX - BayRadiusMeters - pad);
                maxX = Mathf.Max(maxX, BayCenterX + BayRadiusMeters + pad);
                minZ = Mathf.Min(minZ, BayCenterZ - BayRadiusMeters - pad);
                maxZ = Mathf.Max(maxZ, BayCenterZ + BayRadiusMeters + pad);
            }

            return Rect.MinMaxRect(minX, minZ, maxX, maxZ);
        }

        private float MainBodySigned(float worldX, float worldZ)
        {
            var dx = worldX - CenterX;
            var dz = worldZ - CenterZ;
            var cos = Mathf.Cos(RotationRadians);
            var sin = Mathf.Sin(RotationRadians);
            var rx = dx * cos + dz * sin;
            var rz = (-dx * sin + dz * cos) / Aspect;
            var dist = Mathf.Sqrt(rx * rx + rz * rz);
            if (dist < 1e-4f)
            {
                return RadiusMeters;
            }

            var ang = Mathf.Atan2(rz, rx);
            // Tiny continuous Perlin along the angle only — never a spatial grid (grids = stairs).
            var seed = (NoiseSeed & 1023) * 0.017f;
            var fine = (Mathf.PerlinNoise(ang * 0.35f + seed, seed * 1.7f) - 0.5f) * 0.06f;
            var r = RadiusMeters * Mathf.Clamp(Wobble(ang) + fine, 0.6f, 1.4f);
            return r - dist;
        }

        private float Wobble(float ang) =>
            1f
            + Amp1 * Mathf.Sin(2f * ang + Phase1)
            + Amp2 * Mathf.Sin(5f * ang + Phase2)
            + Amp3 * Mathf.Sin(9f * ang + Phase3);

        /// <summary>Inigo Quilez-style smooth max — stable for metre-scale distances.</summary>
        private static float SoftMax(float a, float b, float k)
        {
            var e = Mathf.Max(k - Mathf.Abs(a - b), 0f);
            return Mathf.Max(a, b) + e * e * 0.25f / Mathf.Max(k, 1e-4f);
        }
    }
}
