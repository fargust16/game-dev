using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Combines the massifs into one bounded contribution that never touches the sea.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same two rules Stage 2 established, applied to the massif model instead of the belt model:
    /// massifs combine with a smooth maximum rather than a sum, and the whole contribution is
    /// multiplied by a coast gate that is exactly zero at and seaward of the shoreline. The gate is
    /// why "the mountains do not move the coastline" is structural rather than hoped for.
    /// </para>
    /// <para>
    /// The smooth maximum is <see cref="MountainUpliftModel.SmoothMax"/> itself rather than a copy of
    /// it. Its blend is capped by the smaller input, which is the fix that stops two distant massifs
    /// raising a phantom ridge along the line equidistant from both; re-deriving that here would be a
    /// second place to get it wrong.
    /// </para>
    /// <para>
    /// In practice Stage 2A accepts one axis per world, so the smooth maximum has one input and does
    /// nothing. It is here because <c>MacroStructureSettings.MaxSystems</c> is 2 and a world that
    /// finally produces two must not develop a crease between them.
    /// </para>
    /// </remarks>
    public sealed class MassifUpliftModel
    {
        private readonly MountainMassif[] massifs;
        private readonly float blendMeters;
        private readonly float gateMeters;
        private readonly float maxUpliftMeters;

        /// <summary>Fade-in from the shore: 0 at the coastline, 1 one gate width inland.</summary>
        private static readonly ProfileCurve GateShape = new ProfileCurve(
            new[] { 0f, 1f },
            new[] { 0f, 1f });

        public IReadOnlyList<MountainMassif> Massifs => massifs;

        public int MassifCount => massifs.Length;

        public MassifUpliftModel(IReadOnlyList<MountainMassif> massifs, MassifSettings settings)
        {
            settings.Clamp();

            this.massifs = massifs != null
                ? new List<MountainMassif>(massifs).ToArray()
                : System.Array.Empty<MountainMassif>();
            blendMeters = settings.MassifBlendMeters;
            gateMeters = Mathf.Max(1f, settings.CoastGateMeters);
            maxUpliftMeters = settings.MaxUpliftMeters;
        }

        /// <summary>Highest contribution this configuration can produce, metres.</summary>
        public float CeilingMeters
        {
            get
            {
                var tallest = 0f;
                for (var i = 0; i < massifs.Length; i++)
                {
                    tallest = Mathf.Max(tallest, massifs[i].CrestHeightMeters);
                }

                return Mathf.Min(maxUpliftMeters, tallest + blendMeters * 0.25f);
            }
        }

        /// <summary>
        /// Contribution at a world point, given its signed distance to the coast (positive inland).
        /// </summary>
        public float UpliftAt(float worldX, float worldZ, float signedDistanceToCoast)
        {
            if (massifs.Length == 0 || signedDistanceToCoast <= 0f)
            {
                return 0f;
            }

            var combined = 0f;
            var any = false;

            for (var i = 0; i < massifs.Length; i++)
            {
                var contribution = massifs[i].Evaluate(worldX, worldZ);
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

                combined = MountainUpliftModel.SmoothMax(combined, contribution, blendMeters);
            }

            if (!any)
            {
                return 0f;
            }

            var gate = GateShape.Evaluate(Mathf.Clamp01(signedDistanceToCoast / gateMeters));
            return Mathf.Min(combined * gate, maxUpliftMeters);
        }
    }
}
