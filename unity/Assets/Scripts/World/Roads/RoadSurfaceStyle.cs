namespace GameDev.World.Roads
{
    /// <summary>
    /// Tunable packed-earth road look (alphamap only — no extra meshes/draw calls).
    /// Soft rim is at least <see cref="MinSoftRimAlphamapTexels"/> splat texels so Smoothstep
    /// survives ~2.7 m/px alphamaps. Spec: OVERWORLD_VISUAL_DIRECTION.md.
    /// </summary>
    public static class RoadSurfaceStyle
    {
        /// <summary>Aesthetic soft splat blend beyond packed bed (clamped guide).</summary>
        public const float TransitionMeters = 1.2f;

        public const float TransitionMinMeters = 0.5f;

        /// <summary>Allows SoftRimMeters to expand to ≥2.5 alphamap texels on coarse splats.</summary>
        public const float TransitionMaxMeters = 10f;

        /// <summary>
        /// Minimum soft shoulder width in alphamap texels (fixes staircase when 1.2 m ≪ 1 px).
        /// </summary>
        public const float MinSoftRimAlphamapTexels = 2.5f;

        /// <summary>How strongly the packed center reads (0..1).</summary>
        public const float CenterStrength = 0.98f;

        /// <summary>How strongly dark edge / damp dirt reads (0..1).</summary>
        public const float EdgeStrength = 0.92f;

        /// <summary>Warmth/brightness — light dry soil, readable vs biome grass.</summary>
        public const float Brightness = 1.1f;

        /// <summary>Splat replacement on the solid packed bed (readable path, not binary).</summary>
        public const float BedCoverage = 0.9f;

        /// <summary>
        /// Splat replacement in the soft shoulder — low so biome grass/earth blends in.
        /// </summary>
        public const float TransitionCoverage = 0.4f;

        /// <summary>0..1 gravel / stone speck density (biased to edges).</summary>
        public const float GravelAmount = 0.62f;

        /// <summary>0..1 roughness / color noise along the bed.</summary>
        public const float RoughnessVariation = 0.4f;

        /// <summary>0..1 wheel-rut strength.</summary>
        public const float RutStrength = 0.78f;

        /// <summary>Rut half-width as fraction of half road width.</summary>
        public const float RutOffsetFraction = 0.34f;

        /// <summary>Rut groove half-width in metres.</summary>
        public const float RutWidthMeters = 0.42f;

        /// <summary>0..1 sparse wet / dark patches in the bed.</summary>
        public const float WetPatchAmount = 0.3f;

        /// <summary>0..1 thin crack / irregularity streaks near the shoulder.</summary>
        public const float CrackAmount = 0.4f;

        public static float ClampedTransitionMeters =>
            UnityEngine.Mathf.Clamp(TransitionMeters, TransitionMinMeters, TransitionMaxMeters);

        /// <summary>
        /// Soft rim width: aesthetic clamp, then at least <see cref="MinSoftRimAlphamapTexels"/> metres.
        /// </summary>
        public static float SoftRimMeters(float metersPerAlphamapPixel)
        {
            var mPerPx = UnityEngine.Mathf.Max(0.01f, metersPerAlphamapPixel);
            var fromTexels = mPerPx * MinSoftRimAlphamapTexels;
            return UnityEngine.Mathf.Clamp(
                UnityEngine.Mathf.Max(ClampedTransitionMeters, fromTexels),
                TransitionMinMeters,
                TransitionMaxMeters);
        }
    }
}
