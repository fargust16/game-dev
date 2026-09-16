using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    public sealed partial class IslandRelief
    {
        /// <summary>The massifs the apron is built around, in massif index order.</summary>
        public IReadOnlyList<MountainMassif> ApronMassifs => apronMassifs;

        private MountainMassif[] apronMassifs = Array.Empty<MountainMassif>();
        private ApronFrame[] apronFrames = Array.Empty<ApronFrame>();

        /// <summary>What the apron needs from one massif, measured once.</summary>
        private readonly struct ApronFrame
        {
            public readonly MountainMassif Massif;
            public readonly Vector2 Start;
            public readonly Vector2 End;

            /// <summary>Unit tangent at the start, pointing along increasing arc.</summary>
            public readonly Vector2 StartTangent;

            /// <summary>Unit tangent at the end, pointing along increasing arc (outwards).</summary>
            public readonly Vector2 EndTangent;

            public readonly Rect Bounds;

            public ApronFrame(MountainMassif massif, Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent, Rect bounds)
            {
                Massif = massif;
                Start = start;
                End = end;
                StartTangent = startTangent;
                EndTangent = endTangent;
                Bounds = bounds;
            }
        }

        private void BuildApron(IReadOnlyList<MountainMassif> massifs)
        {
            if (massifs == null || massifs.Count == 0)
            {
                return;
            }

            var ordered = new List<MountainMassif>(massifs);
            ordered.Sort((a, b) => a.Index.CompareTo(b.Index));
            apronMassifs = ordered.ToArray();
            apronFrames = new ApronFrame[apronMassifs.Length];

            var margin = Mathf.Max(
                Settings.ApronReachEndMeters,
                Mathf.Max(Settings.ApronReachGentleMeters, Settings.ApronReachSteepMeters)) + 1f;

            for (var m = 0; m < apronMassifs.Length; m++)
            {
                var massif = apronMassifs[m];
                var delta = Mathf.Clamp(2f / Mathf.Max(1f, massif.LengthMeters), 1e-4f, 0.05f);
                var start = massif.PointAt(0f);
                var end = massif.PointAt(1f);
                var startTangent = Direction(massif.PointAt(delta) - start);
                var endTangent = Direction(end - massif.PointAt(1f - delta));
                var support = massif.Support;
                var bounds = Rect.MinMaxRect(
                    support.xMin - margin, support.yMin - margin, support.xMax + margin, support.yMax + margin);
                apronFrames[m] = new ApronFrame(massif, start, end, startTangent, endTangent, bounds);
            }
        }

        private static Vector2 Direction(Vector2 v) => v.sqrMagnitude > 1e-10f ? v.normalized : Vector2.right;

        /// <summary>
        /// The apron fill: the ground raised to the gated apron surface wherever that surface stands above
        /// the massif's own uplift. Never negative.
        /// </summary>
        /// <remarks>
        /// <c>A = SmoothMax(U2, Γ · P_A, blend) − U2</c>, with the project's own
        /// <see cref="MountainUpliftModel.SmoothMax"/>. Where the wall stands clear above the apron surface
        /// (by at least the blend) the fill is exactly zero and the massif is untouched; where the uplift is
        /// zero the fill is exactly the surface. The final guard only absorbs a rounding ULP of the smooth
        /// maximum - the fill is non-negative by definition.
        /// </remarks>
        private float ApronFill(float worldX, float worldZ, float upliftMeters, float gate)
        {
            if (apronFrames.Length == 0 || gate <= 0f)
            {
                return 0f;
            }

            var surface = 0f;
            for (var m = 0; m < apronFrames.Length; m++)
            {
                var value = ApronSurface(apronFrames[m], worldX, worldZ);
                if (value > surface)
                {
                    surface = value;
                }
            }

            var gated = gate * surface;
            if (gated <= 0f)
            {
                return 0f;
            }

            var blend = Mathf.Min(Settings.ApronBlendMeters, Mathf.Min(upliftMeters, gated));
            if (upliftMeters - gated >= blend)
            {
                return 0f;
            }

            var fill = MountainUpliftModel.SmoothMax(upliftMeters, gated, Settings.ApronBlendMeters) - upliftMeters;
            return fill > 0f ? fill : 0f;
        }

        /// <summary>
        /// The ungated apron surface <c>P_A</c> of one massif at a point, metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Everything is read in the massif's own frame through its public projection, so the apron is
        /// shaped by the same axis, half-width taper, asymmetry and steep side as the body it fills against.
        /// <c>ρ</c> is how far beyond the toe the point lies (negative inside it):
        /// </para>
        /// <code>
        /// ρ ≥ 0 :  h(s) · (1 − ρ/W)²                     piedmont, zero slope where it ends
        /// ρ &lt; 0 :  min(h(s) + tan(talus) · (−ρ), c(s))   talus climbing into the flank, capped
        /// </code>
        /// <para>
        /// <b>Reach is constant along every outward ray.</b> Beside the axis a ray is a normal: arc, side and
        /// therefore steep weight and reach do not change along it. Beyond an end a ray leaves the end point:
        /// the side is the sine and the end weight the cosine of its angle to the end tangent, both constant
        /// along it. With a reach that cannot grow outwards the surface cannot rise outwards, which is the
        /// rule that removed the prototype's apron-made depression (design §7.4). The side sine goes to ±1
        /// continuously where a ray from the end meets the normal at the end, so there is no seam between
        /// the two regions.
        /// </para>
        /// </remarks>
        private float ApronSurface(in ApronFrame frame, float worldX, float worldZ)
        {
            var bounds = frame.Bounds;
            if (worldX < bounds.xMin || worldX > bounds.xMax || worldZ < bounds.yMin || worldZ > bounds.yMax)
            {
                return 0f;
            }

            var massif = frame.Massif;
            massif.Project(worldX, worldZ, out var s, out var offset);
            var distance = Mathf.Abs(offset);
            var p = new Vector2(worldX, worldZ);

            float sine;
            float endness;
            if (s <= 0f)
            {
                EndFrame(p - frame.Start, frame.StartTangent, -1f, out sine, out endness);
            }
            else if (s >= 1f)
            {
                EndFrame(p - frame.End, frame.EndTangent, 1f, out sine, out endness);
            }
            else
            {
                sine = offset >= 0f ? 1f : -1f;
                endness = 0f;
            }

            var steepWeight = Smooth01(0.5f + 0.5f * sine * massif.SteepSide);
            var toe = massif.HalfWidthAt(s) * Mathf.Lerp(2f - massif.Asymmetry, massif.Asymmetry, steepWeight);
            var reach = Mathf.Lerp(
                Mathf.Lerp(Settings.ApronReachGentleMeters, Settings.ApronReachSteepMeters, steepWeight),
                Settings.ApronReachEndMeters,
                endness);

            var crest = massif.CrestHeightMeters;
            if (crest <= 0f)
            {
                return 0f;
            }

            var along = Settings.ApronEndHeightFloor + (1f - Settings.ApronEndHeightFloor) * massif.CrestAt(s) / crest;
            var height = Settings.ApronHeightPerCrest * crest * along;
            var cap = Settings.ApronCapPerCrest * crest * along;

            var rho = distance - toe;
            if (rho >= 0f)
            {
                if (rho >= reach)
                {
                    return 0f;
                }

                var t = 1f - rho / reach;
                return height * t * t;
            }

            return Mathf.Min(height + Mathf.Tan(Settings.ApronTalusDegrees * Mathf.Deg2Rad) * -rho, cap);
        }

        /// <summary>Side sine and end weight of a point beyond one end of the axis.</summary>
        /// <param name="toPoint">Point minus the end point.</param>
        /// <param name="tangent">Unit tangent at that end, along increasing arc.</param>
        /// <param name="outward">-1 at the start (outward is against the tangent), +1 at the end.</param>
        private static void EndFrame(Vector2 toPoint, Vector2 tangent, float outward, out float sine, out float endness)
        {
            var length = toPoint.magnitude;
            if (length < 1e-4f)
            {
                sine = 0f;
                endness = 1f;
                return;
            }

            sine = Mathf.Clamp((tangent.x * toPoint.y - tangent.y * toPoint.x) / length, -1f, 1f);
            endness = Mathf.Clamp01(outward * Vector2.Dot(toPoint, tangent) / length);
        }
    }
}
