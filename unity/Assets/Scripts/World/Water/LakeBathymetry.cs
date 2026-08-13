using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Plate / basin bathymetry from two similar contours (not a cone to the centre).
    /// <para>
    /// Outer contour = shoreline (Depth = 0).<br/>
    /// Inner contour = same shape inset by a wall width (Depth = D<sub>max</sub>, flat floor).<br/>
    /// Between them = one smooth arc (like a bowl / plate rim), then constant depth inside.
    /// </para>
    /// Shore distance d decides which band you are in; we never deepen toward a medial-axis
    /// "centre spike" (that is what looked like rocks / ridges inside the lake).
    /// </summary>
    public static class LakeBathymetry
    {
        public struct Settings
        {
            /// <summary>
            /// Inner flat floor size as a fraction of the outer characteristic radius
            /// (0.5 ≈ half-size contour). Wall band = (1 − scale) · R.
            /// </summary>
            public float InnerContourScale;

            /// <summary>
            /// Wall arc shape. 2 = soft plate (derivative 0 at floor). Higher = steeper near shore.
            /// </summary>
            public float ShapeExponent;

            /// <summary>Unused when 0 — keep for optional later unevenness.</summary>
            public float NoiseAmplitude;

            public float NoiseScaleRelativeToRadius;

            /// <summary>Tiny shore dig to avoid z-fighting with the water plane (metres).</summary>
            public float ShoreEpsilonMeters;

            /// <summary>Target D<sub>mean</sub>/D<sub>max</sub> (optional p nudge). 0 = off.</summary>
            public float MeanToMaxDepthRatio;

            /// <summary>Legacy / unused by inset model (kept so old Profile copies don't break).</summary>
            public float FlatBottomStartU;

            public int DistanceFieldSmoothPasses;
            public int InteriorHeightSmoothPasses;
            public float ShorePinMeters;

            public static Settings NaturalDefault => new Settings
            {
                // Bottom contour ~33% smaller than shore (scale 0.67). Walls loft outer→inner as an arc.
                InnerContourScale = 0.67f,
                ShapeExponent = 2f,
                NoiseAmplitude = 0f,
                NoiseScaleRelativeToRadius = 0.65f,
                ShoreEpsilonMeters = 0.03f,
                MeanToMaxDepthRatio = 0f,
                FlatBottomStartU = 0.67f,
                // Inset model does not need medial-axis blur; light height soften only.
                DistanceFieldSmoothPasses = 0,
                InteriorHeightSmoothPasses = 3,
                ShorePinMeters = 2.5f,
            };
        }

        /// <summary>
        /// Wall width in metres for the outer→inner loft (distance from shore to the flat floor contour).
        /// </summary>
        public static float WallWidthMeters(float characteristicRadiusMeters, Settings settings)
        {
            var scale = Mathf.Clamp(settings.InnerContourScale, 0.2f, 0.85f);
            var r = Mathf.Max(characteristicRadiusMeters, 1f);
            return Mathf.Max(r * (1f - scale), 2f);
        }

        /// <summary>
        /// Plate-wall arc for t∈[0,1] (0 = shore, 1 = inner contour).
        /// Uses 1−(1−t)<sup>p</sup> so the join onto the flat floor has zero slope (no crease).
        /// </summary>
        public static float WallArc01(float t, float shapeExponent)
        {
            var tt = Mathf.Clamp01(t);
            var p = Mathf.Clamp(shapeExponent, 0.75f, 6f);
            return 1f - Mathf.Pow(1f - tt, p);
        }

        /// <summary>
        /// Depth from shore distance and characteristic radius (≈ outer contour size / d at centre).
        /// Inside the inner contour → flat D<sub>max</sub>. In the wall ring → smooth arc.
        /// </summary>
        public static float BaseDepthFromShoreDistance(
            float distanceToShoreMeters,
            float characteristicRadiusMeters,
            float maxDepthMeters,
            Settings settings)
        {
            var dMax = Mathf.Max(maxDepthMeters, 0f);
            if (dMax <= 1e-6f || distanceToShoreMeters <= 1e-5f)
            {
                return 0f;
            }

            var wall = WallWidthMeters(characteristicRadiusMeters, settings);
            if (distanceToShoreMeters >= wall)
            {
                // Inside the smaller contour — flat plate floor (no centre funnel).
                return dMax;
            }

            var t = distanceToShoreMeters / wall;
            return Mathf.Clamp(dMax * WallArc01(t, settings.ShapeExponent), 0f, dMax);
        }

        /// <summary>Legacy u-normalisation (tests / debug). Prefer inset <see cref="BaseDepthFromShoreDistance"/>.</summary>
        public static float NormalizeU(float distanceToShoreMeters, float maxInteriorDistanceMeters)
        {
            var dMax = Mathf.Max(maxInteriorDistanceMeters, 1e-3f);
            return Mathf.Clamp01(distanceToShoreMeters / dMax);
        }

        /// <summary>Legacy wrapper — maps old u∈[0,1] through the wall arc only (no flat inset).</summary>
        public static float BaseDepthMeters(float u, float maxDepthMeters, Settings settings)
        {
            var dMax = Mathf.Max(maxDepthMeters, 0f);
            if (dMax <= 1e-6f || u <= 1e-5f)
            {
                return 0f;
            }

            return Mathf.Clamp(dMax * WallArc01(u, settings.ShapeExponent), 0f, dMax);
        }

        public static float BlendShoreDistanceForBathymetry(
            float rawDistanceMeters,
            float smoothedDistanceMeters,
            float shorePinMeters)
        {
            // Inset model uses raw shore distance (smoothed field optional / usually off).
            if (rawDistanceMeters <= 0f || shorePinMeters <= 0f)
            {
                return rawDistanceMeters;
            }

            var pin = Mathf.Max(shorePinMeters, 0.5f);
            var t = Mathf.SmoothStep(pin * 0.45f, pin * 1.6f, rawDistanceMeters);
            return Mathf.Lerp(rawDistanceMeters, Mathf.Max(smoothedDistanceMeters, 0f), t);
        }

        public static float ApplyBedNoise(
            float baseDepthMeters,
            float maxDepthMeters,
            float worldX,
            float worldZ,
            float equivalentRadiusMeters,
            int lakeSeed,
            Settings settings)
        {
            var a = Mathf.Clamp(settings.NoiseAmplitude, 0f, 0.2f);
            if (a <= 1e-6f || baseDepthMeters <= 1e-4f)
            {
                return Mathf.Clamp(baseDepthMeters, 0f, maxDepthMeters);
            }

            var radius = Mathf.Max(equivalentRadiusMeters, 8f);
            var wavelength = Mathf.Max(radius * Mathf.Clamp(settings.NoiseScaleRelativeToRadius, 0.2f, 2f), 12f);
            var freq = 1f / wavelength;
            var n0 = Mathf.PerlinNoise(
                (worldX + lakeSeed * 0.17f) * freq,
                (worldZ - lakeSeed * 0.13f) * freq);
            var n1 = Mathf.PerlinNoise(
                (worldX - lakeSeed * 0.09f) * freq * 0.5f + 19.7f,
                (worldZ + lakeSeed * 0.11f) * freq * 0.5f + 7.3f);
            var n = ((n0 + n1) * 0.5f - 0.5f) * 2f;
            var depth = baseDepthMeters * (1f + a * n);
            return Mathf.Clamp(depth, 0f, maxDepthMeters * (1f + a * 0.25f));
        }

        /// <summary>
        /// Full depth. <paramref name="maxInteriorDistanceMeters"/> is the characteristic outer radius
        /// (max shore-distance / silhouette radius) — used only to size the inner contour, not as a
        /// "deepest point at centre" target.
        /// </summary>
        public static float DepthMeters(
            float distanceToShoreMeters,
            float maxInteriorDistanceMeters,
            float maxDepthMeters,
            float worldX,
            float worldZ,
            float equivalentRadiusMeters,
            int lakeSeed,
            Settings settings)
        {
            var radius = Mathf.Max(maxInteriorDistanceMeters, equivalentRadiusMeters, 1f);
            var d0 = BaseDepthFromShoreDistance(
                distanceToShoreMeters, radius, maxDepthMeters, settings);
            if (d0 <= 0f)
            {
                return Mathf.Max(0f, settings.ShoreEpsilonMeters);
            }

            var d = ApplyBedNoise(
                d0, maxDepthMeters, worldX, worldZ, equivalentRadiusMeters, lakeSeed, settings);
            var t = Mathf.Clamp01(distanceToShoreMeters / Mathf.Max(WallWidthMeters(radius, settings), 1e-3f));
            return Mathf.Max(d, settings.ShoreEpsilonMeters * Mathf.Clamp01(t * 8f));
        }

        public static float MaxDepthForLake(
            float equivalentRadiusMeters,
            float aspectCompactness01,
            float requestedBasinDepth,
            float minLargeDepth,
            float maxLargeDepth)
        {
            var r = Mathf.Max(equivalentRadiusMeters, 1f);
            var t = Mathf.Clamp01((r - 8f) / 70f);
            t = t * t * (3f - 2f * t);
            var largeCap = Mathf.Clamp(
                Mathf.Max(requestedBasinDepth, minLargeDepth),
                minLargeDepth,
                maxLargeDepth);
            var baseDepth = Mathf.Lerp(1.2f, largeCap, t);
            var elongScale = Mathf.Lerp(0.78f, 1f, Mathf.Clamp01(aspectCompactness01));
            return Mathf.Clamp(baseDepth * elongScale, 1.2f, maxLargeDepth);
        }

        public static float AspectCompactness(WaterZone zone)
        {
            if (zone.Silhouette != null)
            {
                var a = Mathf.Max(zone.Silhouette.Aspect, 1e-3f);
                return Mathf.Clamp01(Mathf.Min(a, 1f / a));
            }

            var aabb = zone.GetFloodAabbXZ();
            var w = Mathf.Max(aabb.width, 1e-3f);
            var h = Mathf.Max(aabb.height, 1e-3f);
            return Mathf.Clamp01(Mathf.Min(w, h) / Mathf.Max(w, h));
        }

        public static float MaxInteriorDistanceMeters(
            WaterZone zone,
            System.Func<float, float, float> shoreSignedDistance,
            float sampleStepMeters = 4f)
        {
            if (zone.Silhouette != null)
            {
                return Mathf.Max(zone.Silhouette.RadiusMeters, zone.CellSize);
            }

            var aabb = zone.GetFloodAabbXZ();
            var step = Mathf.Max(sampleStepMeters, zone.CellSize);
            var maxD = 0f;
            for (var z = aabb.yMin; z <= aabb.yMax; z += step)
            {
                for (var x = aabb.xMin; x <= aabb.xMax; x += step)
                {
                    var d = shoreSignedDistance(x, z);
                    if (d > maxD)
                    {
                        maxD = d;
                    }
                }
            }

            return Mathf.Max(maxD, zone.CellSize);
        }

        public static float ShapeExponentForMeanRatio(float meanToMaxRatio, float fallbackP = 2f)
        {
            var r = Mathf.Clamp(meanToMaxRatio, 0.25f, 0.85f);
            if (r < 0.4f)
            {
                return Mathf.Lerp(3.2f, 2.4f, (r - 0.25f) / 0.15f);
            }

            if (r < 0.55f)
            {
                return Mathf.Lerp(2.4f, 1.9f, (r - 0.4f) / 0.15f);
            }

            return Mathf.Lerp(1.9f, 1.25f, (r - 0.55f) / 0.3f);
        }
    }
}
