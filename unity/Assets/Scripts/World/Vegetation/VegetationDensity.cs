using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Shared slope falloff plus a legacy meso patch helper. New flora placement uses
    /// <see cref="VegetationEcology"/> (macro forest / shrub / grass fields); <see cref="SamplePatch"/>
    /// remains for tests and any callers that only need a single ~50m blob field.
    /// </summary>
    public static class VegetationDensity
    {
        // Noise-space frequency for patch clustering - low enough that patches read at roughly
        // 50m scale (distinct groves/clearings), not per-sample noise.
        public const float PatchFrequency = 0.02f;

        // Slope01 at or above this reads as vegetation-free (bare rock/scree) regardless of biome.
        public const float MaxSlope01 = 0.6f;

        public static float SamplePatch(float worldX, float worldZ, float noiseOffsetX, float noiseOffsetZ)
        {
            var n = Mathf.PerlinNoise(
                (worldX + noiseOffsetX) * PatchFrequency,
                (worldZ + noiseOffsetZ) * PatchFrequency);

            // Smoothstep sharpens contrast between clumps and gaps rather than leaving most of the
            // map at a uniform mid-grey density.
            return n * n * (3f - 2f * n);
        }

        public static float SlopeMultiplier(float slope01)
        {
            var s = Mathf.Clamp01(slope01);
            return 1f - Mathf.Clamp01(s / MaxSlope01);
        }
    }
}
