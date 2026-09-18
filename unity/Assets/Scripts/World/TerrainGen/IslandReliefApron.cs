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
            public readonly Vector2 StartTangent;
            public readonly Vector2 EndTangent;
            public readonly Rect Bounds;
            public readonly float ReachMeters;
            public readonly float DeltaMeters;
            public readonly float DomainMeters;

            public ApronFrame(
                MountainMassif massif,
                Vector2 start,
                Vector2 end,
                Vector2 startTangent,
                Vector2 endTangent,
                Rect bounds,
                float reachMeters,
                float deltaMeters)
            {
                Massif = massif;
                Start = start;
                End = end;
                StartTangent = startTangent;
                EndTangent = endTangent;
                Bounds = bounds;
                ReachMeters = reachMeters;
                DeltaMeters = deltaMeters;
                DomainMeters = float.IsInfinity(reachMeters) || reachMeters <= 0f
                    ? float.PositiveInfinity
                    : Mathf.Max(0f, reachMeters - deltaMeters);
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
                var ds = Mathf.Clamp(2f / Mathf.Max(1f, massif.LengthMeters), 1e-4f, 0.05f);
                var start = massif.PointAt(0f);
                var end = massif.PointAt(1f);
                var startTangent = Direction(massif.PointAt(ds) - start);
                var endTangent = Direction(end - massif.PointAt(1f - ds));
                var support = massif.Support;
                var bounds = Rect.MinMaxRect(
                    support.xMin - margin, support.yMin - margin, support.xMax + margin, support.yMax + margin);
                var axisReach = massif.Axis != null ? massif.Axis.ReachMeters : float.PositiveInfinity;
                var sampleStep = AxisSampleStep(massif);
                var delta = float.IsInfinity(axisReach) || axisReach <= 1e-4f
                    ? 0f
                    : (sampleStep * sampleStep) / (8f * axisReach);
                apronFrames[m] = new ApronFrame(massif, start, end, startTangent, endTangent, bounds, axisReach, delta);
            }
        }

        private static float AxisSampleStep(MountainMassif massif)
        {
            var axis = massif.Axis;
            if (axis == null || axis.Points == null || axis.Points.Length < 2 || axis.Cumulative == null)
            {
                return MacroStructureSettings.Default.SampleSpacingMeters;
            }

            var last = axis.Points.Length - 1;
            return Mathf.Max(1e-3f, (axis.Cumulative[last] - axis.Cumulative[0]) / last);
        }

        private static Vector2 Direction(Vector2 v) => v.sqrMagnitude > 1e-10f ? v.normalized : Vector2.right;

        /// <summary>Ungated apron surface P_A at a world point, metres.</summary>
        public float ApronSurfaceAt(float worldX, float worldZ)
        {
            var surface = 0f;
            for (var m = 0; m < apronFrames.Length; m++)
            {
                var value = ApronSurface(apronFrames[m], worldX, worldZ);
                if (value > surface)
                {
                    surface = value;
                }
            }

            return surface;
        }

        /// <summary>Ungated apron surface of one massif, metres.</summary>
        public float ApronSurfaceAt(int massifIndex, float worldX, float worldZ)
        {
            for (var m = 0; m < apronFrames.Length; m++)
            {
                if (apronFrames[m].Massif.Index == massifIndex)
                {
                    return ApronSurface(apronFrames[m], worldX, worldZ);
                }
            }

            return 0f;
        }

        /// <summary>Effective piedmont reach W_eff at arc s on one side (−1 / +1), metres.</summary>
        public float EffectiveReach(int massifIndex, float s, float side)
        {
            for (var m = 0; m < apronFrames.Length; m++)
            {
                if (apronFrames[m].Massif.Index != massifIndex)
                {
                    continue;
                }

                ProfileAt(apronFrames[m], s, side >= 0f ? 1f : -1f, 0f, out _, out var wEff, out _, out _);
                return wEff;
            }

            return 0f;
        }

        /// <summary>Axis reach and sagitta margin used as the projection domain, metres.</summary>
        public void ApronDomain(int massifIndex, out float reachMeters, out float deltaMeters, out float domainMeters)
        {
            reachMeters = 0f;
            deltaMeters = 0f;
            domainMeters = 0f;
            for (var m = 0; m < apronFrames.Length; m++)
            {
                if (apronFrames[m].Massif.Index != massifIndex)
                {
                    continue;
                }

                reachMeters = apronFrames[m].ReachMeters;
                deltaMeters = apronFrames[m].DeltaMeters;
                domainMeters = apronFrames[m].DomainMeters;
                return;
            }
        }

        /// <summary>
        /// The apron fill: the ground raised to the gated apron surface wherever that surface stands above
        /// the massif's own uplift. Never negative.
        /// </summary>
        private float ApronFill(float worldX, float worldZ, float upliftMeters, float gate)
        {
            if (apronFrames.Length == 0 || gate <= 0f)
            {
                return 0f;
            }

            var surface = ApronSurfaceAt(worldX, worldZ);
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
        /// Ungated apron surface in the massif's own (s, offset) coordinates, metres. This is the
        /// physical profile S10 walks: constant arc, increasing |offset|, so Project is not involved.
        /// </summary>
        public float ApronProfileAt(int massifIndex, float s, float signedOffset, float endness)
        {
            for (var m = 0; m < apronFrames.Length; m++)
            {
                if (apronFrames[m].Massif.Index != massifIndex)
                {
                    continue;
                }

                return ProfileSurface(apronFrames[m], s, signedOffset, signedOffset < 0f ? -1f : 1f, endness);
            }

            return 0f;
        }

        /// <summary>
        /// The ungated apron surface <c>P_A</c> of one massif at a point, metres. Exactly zero beyond
        /// the axis reach minus the sampling sagitta.
        /// </summary>
        private float ApronSurface(in ApronFrame frame, float worldX, float worldZ)
        {
            var bounds = frame.Bounds;
            if (worldX < bounds.xMin || worldX > bounds.xMax || worldZ < bounds.yMin || worldZ > bounds.yMax)
            {
                return 0f;
            }

            var massif = frame.Massif;
            massif.Project(worldX, worldZ, out var s, out var offset);
            var p = new Vector2(worldX, worldZ);
            var axisDistance = AxisDistance(frame, s, offset, p);
            if (axisDistance >= frame.DomainMeters)
            {
                return 0f;
            }

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

            return ProfileSurface(frame, s, offset, sine, endness);
        }

        private float ProfileSurface(in ApronFrame frame, float s, float signedOffset, float sine, float endness)
        {
            if (Mathf.Abs(signedOffset) >= frame.DomainMeters)
            {
                return 0f;
            }

            ProfileAt(frame, s, sine, endness, out var toe, out var reach, out var height, out var hEff);
            var rho = Mathf.Abs(signedOffset) - toe;
            if (rho >= 0f)
            {
                if (hEff <= 0f || reach <= 0f || rho >= reach)
                {
                    return 0f;
                }

                var t = 1f - rho / reach;
                return hEff * t * t;
            }

            if (height <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(height + Mathf.Tan(Settings.ApronTalusDegrees * Mathf.Deg2Rad) * -rho, CapAt(frame.Massif, s));
        }

        private void ProfileAt(
            in ApronFrame frame, float s, float sine, float endness,
            out float toe, out float wEff, out float height, out float hEff)
        {
            var massif = frame.Massif;
            var steepWeight = Smooth01(0.5f + 0.5f * sine * massif.SteepSide);
            toe = massif.HalfWidthAt(s) * Mathf.Lerp(2f - massif.Asymmetry, massif.Asymmetry, steepWeight);
            var w = Mathf.Lerp(
                Mathf.Lerp(Settings.ApronReachGentleMeters, Settings.ApronReachSteepMeters, steepWeight),
                Settings.ApronReachEndMeters,
                endness);

            var domain = frame.DomainMeters;
            wEff = float.IsInfinity(domain)
                ? w
                : Mathf.Clamp(domain - toe, 0f, w);

            var crest = massif.CrestHeightMeters;
            if (crest <= 0f)
            {
                height = 0f;
                hEff = 0f;
                return;
            }

            var along = Settings.ApronEndHeightFloor + (1f - Settings.ApronEndHeightFloor) * massif.CrestAt(s) / crest;
            height = Settings.ApronHeightPerCrest * crest * along;
            hEff = w <= 1e-6f ? 0f : height * (wEff / w);
        }

        private float CapAt(MountainMassif massif, float s)
        {
            var crest = massif.CrestHeightMeters;
            if (crest <= 0f)
            {
                return 0f;
            }

            var along = Settings.ApronEndHeightFloor + (1f - Settings.ApronEndHeightFloor) * massif.CrestAt(s) / crest;
            return Settings.ApronCapPerCrest * crest * along;
        }

        private static float AxisDistance(in ApronFrame frame, float s, float offset, Vector2 p)
        {
            if (s > 0f && s < 1f)
            {
                return Mathf.Abs(offset);
            }

            var end = s <= 0f ? frame.Start : frame.End;
            return (p - end).magnitude;
        }

        /// <summary>Side sine and end weight of a point beyond one end of the axis.</summary>
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
