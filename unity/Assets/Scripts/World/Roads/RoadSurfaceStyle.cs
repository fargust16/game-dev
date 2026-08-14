namespace GameDev.World.Roads
{
    /// <summary>
    /// Tunable packed-earth road look (alphamap only — no extra meshes/draw calls).
    /// Warm lighter center, darker moist edges, soft 1.5–3 m grass blend, gravel/ruts.
    /// </summary>
    public static class RoadSurfaceStyle
    {
        /// <summary>Soft splat blend beyond packed bed (memo: ~1.5–2 m each side).</summary>
        public const float TransitionMeters = 1.85f;

        /// <summary>How strongly the packed center reads (0..1).</summary>
        public const float CenterStrength = 0.98f;

        /// <summary>How strongly dark edge / damp dirt reads (0..1).</summary>
        public const float EdgeStrength = 0.92f;

        /// <summary>Warmth/brightness — light dry soil, readable vs biome grass.</summary>
        public const float Brightness = 1.1f;

        /// <summary>Splat replacement on the solid packed bed (keep high so path stays readable).</summary>
        public const float BedCoverage = 0.97f;

        /// <summary>
        /// Splat replacement in the soft shoulder — lower so grass/earth peeks through
        /// (memo: bare soil + sparse blades, not a hard dirt wall).
        /// </summary>
        public const float TransitionCoverage = 0.52f;

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
    }
}
