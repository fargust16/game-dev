using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Continuous lake shoreline as one analytic curve (vector-graphics style), not a grid mask.
    /// Shape = base ellipse × low-frequency terrainFactor(θ) × light organic wobble.
    /// Positive signed distance = inside water. Mesh and terrain carve share this same field.
    /// </summary>
    public sealed class LakeSilhouette
    {
        public const int TerrainFactorSampleCount = 32;

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
        public float[] TerrainFactors { get; }

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
            float bayRadiusMeters,
            float[] terrainFactors = null)
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
            TerrainFactors = NormalizeTerrainFactors(terrainFactors);
        }

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
            return SoftMax(main, bay, k: 14f);
        }

        public bool Contains(float worldX, float worldZ) => SignedDistance(worldX, worldZ) >= 0f;

        public float ShoreRadiusAtAngle(float worldAngleRadians)
        {
            var cos = Mathf.Cos(RotationRadians);
            var sin = Mathf.Sin(RotationRadians);
            var dirX = Mathf.Cos(worldAngleRadians);
            var dirZ = Mathf.Sin(worldAngleRadians);
            var rx = dirX * cos + dirZ * sin;
            var rz = (-dirX * sin + dirZ * cos) / Aspect;
            var ang = Mathf.Atan2(rz, rx);
            return RadiusMeters * RadiusScale(ang);
        }

        public float TerrainFactorAtAngle(float localAngleRadians) =>
            SampleTerrainFactor(localAngleRadians);

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
            var r = RadiusMeters * RadiusScale(ang);
            return r - dist;
        }

        private float RadiusScale(float localAng)
        {
            var organic = Mathf.Clamp(Wobble(localAng), 0.85f, 1.15f);
            var terrain = SampleTerrainFactor(localAng);
            return Mathf.Clamp(organic * terrain, 0.75f, 1.30f);
        }

        /// <summary>
        /// Low-frequency shore bends only (harmonics 2 and 3). No high-frequency tooth noise.
        /// Amplitudes are expected already scaled by ShapeIrregularity at construction.
        /// </summary>
        private float Wobble(float ang) =>
            1f
            + Amp1 * Mathf.Sin(2f * ang + Phase1)
            + Amp2 * Mathf.Sin(3f * ang + Phase2)
            + Amp3 * Mathf.Sin(4f * ang + Phase3);

        private float SampleTerrainFactor(float localAng)
        {
            if (TerrainFactors == null || TerrainFactors.Length == 0)
            {
                return 1f;
            }

            var n = TerrainFactors.Length;
            var u = (localAng / (Mathf.PI * 2f) % 1f + 1f) % 1f;
            var f = u * n;
            var i0 = Mathf.FloorToInt(f) % n;
            var i1 = (i0 + 1) % n;
            var t = f - Mathf.Floor(f);
            return Mathf.Lerp(TerrainFactors[i0], TerrainFactors[i1], t);
        }

        private static float[] NormalizeTerrainFactors(float[] source)
        {
            var factors = new float[TerrainFactorSampleCount];
            if (source == null || source.Length == 0)
            {
                for (var i = 0; i < factors.Length; i++)
                {
                    factors[i] = 1f;
                }

                return factors;
            }

            for (var i = 0; i < factors.Length; i++)
            {
                var u = i / (float)factors.Length;
                var f = u * source.Length;
                var i0 = Mathf.FloorToInt(f) % source.Length;
                var i1 = (i0 + 1) % source.Length;
                var t = f - Mathf.Floor(f);
                factors[i] = Mathf.Clamp(Mathf.Lerp(source[i0], source[i1], t), 0.7f, 1.3f);
            }

            return factors;
        }

        private static float SoftMax(float a, float b, float k)
        {
            var e = Mathf.Max(k - Mathf.Abs(a - b), 0f);
            return Mathf.Max(a, b) + e * e * 0.25f / Mathf.Max(k, 1e-4f);
        }
    }
}
