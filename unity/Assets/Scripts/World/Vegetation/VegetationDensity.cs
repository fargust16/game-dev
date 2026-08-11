using UnityEngine;

namespace GameDev.World.Vegetation
{
    /// <summary>
    /// Pure per-point vegetation density modifiers (WORLD_GENERATION_PIPELINE.md Iteration 07):
    /// scaling factors <see cref="VegetationScatterMenu"/> multiplies into its per-biome acceptance
    /// probability, not a replacement for it. Two independent effects:
    /// <list type="bullet">
    /// <item><see cref="SamplePatch"/> - large, smooth Perlin blobs so vegetation reads as natural
    /// groves/clearings instead of uniform static, without needing an actual painted density-map
    /// asset (there isn't one yet).</item>
    /// <item><see cref="SlopeMultiplier"/> - steep ground thins out regardless of biome, since a
    /// cliff face doesn't grow a forest just because the biome underneath it is Forest.</item>
    /// </list>
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
