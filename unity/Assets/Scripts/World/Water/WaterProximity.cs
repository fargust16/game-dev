using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Helpers for placement systems that need "under water or too close to the wet mask" checks
    /// beyond a single-point <see cref="IWaterSampler.TryGetWaterLevel"/> sample.
    /// </summary>
    public static class WaterProximity
    {
        /// <summary>
        /// True when the surface point is underwater, or dry ground within
        /// <paramref name="shoreBufferMeters"/> of a wet cell (keeps trunks off the waterline when
        /// the sampler grid is coarser than a prefab footprint). True shore bank beyond the buffer
        /// stays eligible for vegetation.
        /// </summary>
        public static bool IsUnderOrTooCloseToWater(
            IWaterSampler water, float worldX, float worldZ, float surfaceY, float shoreBufferMeters)
        {
            if (water == null)
            {
                return false;
            }

            if (water.TryGetWaterLevel(worldX, worldZ, out var waterY) && surfaceY <= waterY)
            {
                return true;
            }

            if (shoreBufferMeters <= 0f)
            {
                return false;
            }

            const int ringSamples = 8;
            for (var i = 0; i < ringSamples; i++)
            {
                var angle = (Mathf.PI * 2f * i) / ringSamples;
                var sx = worldX + Mathf.Cos(angle) * shoreBufferMeters;
                var sz = worldZ + Mathf.Sin(angle) * shoreBufferMeters;
                if (water.TryGetWaterLevel(sx, sz, out _))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
