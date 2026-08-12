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

        /// <summary>
        /// Soft 0..1 wetness for ecological vegetation (shore → wet meadow), not a hard spawn ring.
        /// Underwater samples return 1; dry ground beyond <paramref name="falloffMeters"/> returns 0.
        /// </summary>
        public static float SampleWetness01(
            IWaterSampler water, float worldX, float worldZ, float falloffMeters)
        {
            if (water == null || falloffMeters <= 0f)
            {
                return 0f;
            }

            if (water.TryGetWaterLevel(worldX, worldZ, out _))
            {
                return 1f;
            }

            // Expanding rings: first hit distance approximates shoreline proximity without a full SDF.
            const int rings = 4;
            const int samplesPerRing = 8;
            var best = falloffMeters + 1f;
            for (var r = 1; r <= rings; r++)
            {
                var radius = falloffMeters * (r / (float)rings);
                for (var i = 0; i < samplesPerRing; i++)
                {
                    var angle = (Mathf.PI * 2f * i) / samplesPerRing;
                    var sx = worldX + Mathf.Cos(angle) * radius;
                    var sz = worldZ + Mathf.Sin(angle) * radius;
                    if (water.TryGetWaterLevel(sx, sz, out _))
                    {
                        if (radius < best)
                        {
                            best = radius;
                        }
                    }
                }
            }

            if (best > falloffMeters)
            {
                return 0f;
            }

            var t = 1f - (best / falloffMeters);
            return t * t * (3f - 2f * t);
        }
    }
}
