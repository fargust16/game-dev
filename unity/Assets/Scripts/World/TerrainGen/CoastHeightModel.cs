using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Turns signed distance-to-coast into world height. The whole of Stage 1's relief.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three authored curves and nothing else: a shelf descending offshore, a coastal rise onshore,
    /// and a long gentle interior rise beyond it. The coastline sits at
    /// <see cref="WorldScale.SeaLevelMeters"/> by construction, because the onshore curve starts at
    /// zero rise and the offshore curve starts at zero drop.
    /// </para>
    /// <para>
    /// <b>There is deliberately no variation along the shore at this stage.</b> The design makes
    /// coastal character a consequence of what stands behind a segment — cliff where a mountain
    /// range reaches the water, estuary at a river mouth — and neither exists yet. Adding a
    /// decorative wobble now would be the noise-first modelling this generator exists to avoid, and
    /// would have to be removed when the real cause arrives. The uniformity is a stage boundary, not
    /// an oversight.
    /// </para>
    /// <para>
    /// The result is monotone in distance on both sides, which is what makes the Stage 1 field free
    /// of closed depressions and therefore a clean starting point for drainage later.
    /// </para>
    /// </remarks>
    public readonly struct CoastHeightModel
    {
        private readonly TerrainGenSettings settings;
        private readonly ProfileCurve onshore;
        private readonly ProfileCurve interior;
        private readonly ProfileCurve offshore;

        public CoastHeightModel(TerrainGenSettings settings)
        {
            this.settings = settings;

            // Concave near the shore, straightening inland: a beach rises slowly out of the water
            // and then more steadily, which is what separates a shore from a ramp.
            onshore = new ProfileCurve(
                new[] { 0f, 0.15f, 0.5f, 1f },
                new[] { 0f, 0.10f, 0.55f, 1f });

            // Saturating: the interior flattens out rather than tilting forever toward the middle.
            interior = new ProfileCurve(
                new[] { 0f, 0.4f, 1f },
                new[] { 0f, 0.45f, 1f });

            // Steep just off the beach, then levelling onto the shelf floor.
            offshore = new ProfileCurve(
                new[] { 0f, 0.3f, 1f },
                new[] { 0f, 0.45f, 1f });
        }

        /// <summary>World Y for a signed distance to the coast, positive inland.</summary>
        public float HeightAt(float signedDistanceMeters)
        {
            if (signedDistanceMeters >= 0f)
            {
                var u = Mathf.Clamp01(signedDistanceMeters / settings.CoastalTransitionMeters);
                var h = WorldScale.SeaLevelMeters + settings.CoastalRiseMeters * onshore.Evaluate(u);

                var beyond = signedDistanceMeters - settings.CoastalTransitionMeters;
                if (beyond > 0f)
                {
                    var v = Mathf.Clamp01(beyond / settings.InteriorScaleMeters);
                    h += settings.InteriorRiseMeters * interior.Evaluate(v);
                }

                return h;
            }

            var a = Mathf.Clamp01(-signedDistanceMeters / settings.ShelfWidthMeters);
            return WorldScale.SeaLevelMeters - settings.ShelfDropMeters * offshore.Evaluate(a);
        }
    }
}
