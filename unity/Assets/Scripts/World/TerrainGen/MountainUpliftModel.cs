using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Combines the uplift belts into one bounded height contribution that never touches the sea.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two rules do all the work here. Belts combine with a <b>smooth maximum</b>, not a sum, so two
    /// crossing belts make one massif rather than a double-height spike; and the whole contribution
    /// is multiplied by a <b>coast gate</b> that is exactly zero at and seaward of the shoreline.
    /// </para>
    /// <para>
    /// The gate is why "uplift does not move the coastline" is structural rather than hoped for. At
    /// the shore the contribution is zero by construction, so every ocean sample keeps its Stage 1
    /// height to the bit, and no sample below sea level can be lifted above it.
    /// </para>
    /// <para>
    /// The smooth-maximum blend is capped by the smaller of the two contributions. The textbook
    /// polynomial form adds a quarter of the blend width wherever the two inputs are equal - which
    /// includes every point where two belts both contribute nothing, and would raise a phantom ridge
    /// along the line equidistant from two distant ranges. Scaling the blend by the smaller value
    /// makes it vanish exactly where there is nothing to blend.
    /// </para>
    /// </remarks>
    public sealed class MountainUpliftModel
    {
        private readonly MountainRange[] ranges;
        private readonly float blendMeters;
        private readonly float gateMeters;
        private readonly float maxUpliftMeters;

        /// <summary>Fade-in from the shore: 0 at the coastline, 1 one gate width inland.</summary>
        private static readonly ProfileCurve GateShape = new ProfileCurve(
            new[] { 0f, 1f },
            new[] { 0f, 1f });

        public IReadOnlyList<MountainRange> Ranges => ranges;

        public int RangeCount => ranges.Length;

        public MountainUpliftModel(IReadOnlyList<MountainRange> ranges, TerrainGenSettings settings)
        {
            this.ranges = ranges != null
                ? new List<MountainRange>(ranges).ToArray()
                : System.Array.Empty<MountainRange>();
            blendMeters = settings.RangeBlendMeters;
            gateMeters = Mathf.Max(1f, settings.UpliftCoastGateMeters);
            maxUpliftMeters = settings.MaxUpliftMeters;
        }

        /// <summary>Highest uplift this configuration can produce, metres.</summary>
        public float CeilingMeters
        {
            get
            {
                var tallest = 0f;
                for (var i = 0; i < ranges.Length; i++)
                {
                    tallest = Mathf.Max(tallest, ranges[i].CrestHeightMeters);
                }

                return Mathf.Min(maxUpliftMeters, tallest + blendMeters * 0.25f);
            }
        }

        /// <summary>
        /// Uplift at a world point, given its signed distance to the coast (positive inland).
        /// </summary>
        public float UpliftAt(float worldX, float worldZ, float signedDistanceToCoast)
        {
            if (ranges.Length == 0 || signedDistanceToCoast <= 0f)
            {
                return 0f;
            }

            var combined = 0f;
            var any = false;

            for (var i = 0; i < ranges.Length; i++)
            {
                var contribution = ranges[i].Evaluate(worldX, worldZ);
                if (contribution <= 0f)
                {
                    continue;
                }

                if (!any)
                {
                    combined = contribution;
                    any = true;
                    continue;
                }

                combined = SmoothMax(combined, contribution, blendMeters);
            }

            if (!any)
            {
                return 0f;
            }

            var gate = GateShape.Evaluate(Mathf.Clamp01(signedDistanceToCoast / gateMeters));
            return Mathf.Min(combined * gate, maxUpliftMeters);
        }

        /// <summary>
        /// Polynomial smooth maximum, with the blend width capped by the smaller input.
        /// </summary>
        public static float SmoothMax(float a, float b, float blend)
        {
            var k = Mathf.Min(blend, Mathf.Min(a, b));
            if (k <= 1e-5f)
            {
                return Mathf.Max(a, b);
            }

            var h = Mathf.Clamp01(0.5f + 0.5f * (a - b) / k);
            return Mathf.Lerp(b, a, h) + k * h * (1f - h);
        }
    }
}
