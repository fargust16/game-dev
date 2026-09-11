using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The broad uplift body of one mountain system: a longitudinal profile along an accepted macro
    /// axis, multiplied by a cross-section across it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two one-dimensional profiles and nothing else. Height at a point is
    /// <c>crest * longitudinal(s) * crossSection(d, s)</c>, where <c>s</c> is how far along the axis
    /// the point projects and <c>d</c> is how far off it. Separating the dimensions is what keeps the
    /// result a massif: every question about how tall the mountain is at a place is answered by where
    /// that place sits in the mountain's own frame, never by its distance to a nearby point.
    /// </para>
    /// <para>
    /// Deliberately <em>not</em> a maximum over radial influences around samples of the axis. That
    /// formulation makes every sample an independent hill and a chain of hills is what Stage 2
    /// shipped twice. It is worth naming because it is the shape the problem keeps suggesting: it is
    /// easy to write, it is smooth, and it is wrong.
    /// </para>
    /// <para>
    /// The cross-section reuses <see cref="MountainRange.FlankShape"/>. That curve was authored for
    /// this job - concave under the crest, convex at the toe, zero slope at both ends - and the
    /// failure of Stage 2 was never its cross-section.
    /// </para>
    /// </remarks>
    public sealed class MountainMassif
    {
        /// <summary>Index within the world, in the order the axes were accepted.</summary>
        public int Index { get; }

        /// <summary>The macro axis this massif is built on.</summary>
        public MacroAxis Axis { get; }

        /// <summary>Height along the axis, normalised: 0 at the ends, 1 at the primary culmination.</summary>
        public ProfileCurve Longitudinal { get; }

        /// <summary>Peak uplift, metres.</summary>
        public float CrestHeightMeters { get; }

        /// <summary>
        /// Nominal half-width of the body, metres.
        /// </summary>
        /// <remarks>
        /// The geological definition of how wide the massif is, taken from the axis's intended width
        /// rather than derived from its curvature. Curvature enters only through
        /// <see cref="ReachLimitedHalfWidthMeters"/>, as a safety bound that is not supposed to bind.
        /// </remarks>
        public float HalfWidthMeters { get; }

        /// <summary>The widest body this axis could carry without folding, metres.</summary>
        public float ReachLimitedHalfWidthMeters { get; }

        /// <summary>Whether the curvature bound actually reduced the width.</summary>
        /// <remarks>
        /// Expected false on every world. Stage 2A accepts an axis only when its reach already
        /// supports the intended width, so this binding means that guarantee has been broken
        /// somewhere upstream - which is precisely how Stage 2 failed, and worth an assertion rather
        /// than a silent narrowing.
        /// </remarks>
        public bool ReachBound => ReachLimitedHalfWidthMeters < HalfWidthMeters - 0.5f;

        /// <summary>Ratio between the gentle and the steep flank's width.</summary>
        public float Asymmetry { get; }

        /// <summary>Which side of the axis is the steep one, +1 or -1.</summary>
        public int SteepSide { get; }

        /// <summary>Normalised arc positions of the culminations, ascending.</summary>
        public float[] CulminationT { get; }

        /// <summary>
        /// The stretch this massif actually put its high ground on, as normalised arc.
        /// </summary>
        /// <remarks>
        /// The axis's certified core, held clear of both ends so the outer ramps have room. Exposed
        /// because it is what the profile was laid out against: judging a culmination's placement
        /// against the raw certified interval measures it against a slightly different span.
        /// </remarks>
        public Vector2 CoreInterval { get; }

        /// <summary>Bounding rectangle of everything this massif can affect.</summary>
        public Rect Support { get; }

        /// <summary>Length of the smoothed projection curve, metres.</summary>
        public float LengthMeters { get; }

        /// <summary>Interpolated points per axis segment on the projection curve.</summary>
        /// <remarks>
        /// <para>
        /// Stage 2A stores its axis as a polyline sampled every 20 m. Projecting onto a polyline puts
        /// a medial ray at every vertex on its concave side, and the arc coordinate jumps across it by
        /// about <c>2 d tan(theta / 2)</c>, where theta is the turn at that vertex and d the offset.
        /// At 20 m on a 460 m fillet that is a 2.5 degree turn, which 150 m off the axis is a six
        /// metre jump in arc - and through the longitudinal profile, a ten metre step in height.
        /// </para>
        /// <para>
        /// Subdividing the chords does not help: interpolating along them leaves the original vertices
        /// exactly as sharp as they were. Measured, not assumed - a five-fold subdivision moved the
        /// worst step from 10.1 m to 9.7 m. What is needed is a smoother curve, not more points on the
        /// same one, so the axis is rebuilt as a Catmull-Rom through its samples: the turn per vertex
        /// falls with the subdivision, and the jump with it.
        /// </para>
        /// </remarks>
        public const int ProjectionSubdivisions = 20;

        /// <summary>Length over nominal half-width. Under about three a massif reads as a hill.</summary>
        public float LengthToWidthRatio =>
            HalfWidthMeters > 1e-4f ? LengthMeters / HalfWidthMeters : 0f;

        private readonly Vector2[] points;
        private readonly float[] cumulative;
        private readonly Vector2[] tangent;
        private readonly float crownFraction;
        private readonly float widthTaperFloor;
        private readonly float endTaperSpan;
        private readonly float effectiveHalfWidth;
        private readonly int coarseStride;

        public MountainMassif(
            int index,
            MacroAxis axis,
            ProfileCurve longitudinal,
            float crestHeightMeters,
            float halfWidthMeters,
            float asymmetry,
            int steepSide,
            float[] culminationT,
            MassifSettings settings)
            : this(index, axis, longitudinal, crestHeightMeters, halfWidthMeters, asymmetry,
                   steepSide, culminationT, settings, new Vector2(0f, 1f))
        {
        }

        public MountainMassif(
            int index,
            MacroAxis axis,
            ProfileCurve longitudinal,
            float crestHeightMeters,
            float halfWidthMeters,
            float asymmetry,
            int steepSide,
            float[] culminationT,
            MassifSettings settings,
            Vector2 coreInterval)
        {
            settings.Clamp();
            CoreInterval = coreInterval;

            Index = index;
            Axis = axis;
            Longitudinal = longitudinal;
            CrestHeightMeters = crestHeightMeters;
            HalfWidthMeters = halfWidthMeters;
            Asymmetry = asymmetry;
            SteepSide = steepSide >= 0 ? 1 : -1;
            CulminationT = culminationT ?? new[] { 0.5f };

            ReachLimitedHalfWidthMeters = MountainRange.CurvatureWidthFactor * axis.ReachMeters;
            effectiveHalfWidth = Mathf.Min(halfWidthMeters, ReachLimitedHalfWidthMeters);

            crownFraction = settings.CrownFraction;
            widthTaperFloor = settings.WidthTaperFloor;
            endTaperSpan = settings.EndTaperSpan;

            var smoothed = Town.Geometry.Geo2.SmoothCatmullRom(
                axis.Points, 0f, axis.Points.Length > 2 ? ProjectionSubdivisions : 1);
            points = smoothed.ToArray();

            cumulative = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                cumulative[i] = cumulative[i - 1] + (points[i] - points[i - 1]).magnitude;
            }

            LengthMeters = cumulative[points.Length - 1];
            coarseStride = Mathf.Max(1, ProjectionSubdivisions);

            tangent = new Vector2[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var direction = points[Mathf.Min(points.Length - 1, i + 1)]
                                - points[Mathf.Max(0, i - 1)];
                tangent[i] = direction.sqrMagnitude < 1e-10f ? Vector2.right : direction.normalized;
            }

            // The widest the body can get anywhere, which is what the support rectangle has to hold.
            var widest = effectiveHalfWidth * (2f - Mathf.Min(asymmetry, 1f));
            Support = axis.Bounds(widest + 1f);
        }

        /// <summary>Uplift contributed at a world point, metres, never negative.</summary>
        public float Evaluate(float worldX, float worldZ)
        {
            if (worldX < Support.xMin || worldX > Support.xMax
                || worldZ < Support.yMin || worldZ > Support.yMax)
            {
                return 0f;
            }

            var p = new Vector2(worldX, worldZ);
            Nearest(p, out var segment, out var along, out var distance, out var closest);

            var arc = Mathf.Lerp(cumulative[segment], cumulative[segment + 1], along);
            var s = LengthMeters > 1e-4f ? Mathf.Clamp01(arc / LengthMeters) : 0f;

            var height = Longitudinal.Evaluate(s);
            if (height <= 0f)
            {
                return 0f;
            }

            var axis = Vector2.Lerp(tangent[segment], tangent[segment + 1], along);
            if (axis.sqrMagnitude < 1e-8f)
            {
                axis = tangent[segment];
            }

            axis = axis.normalized;

            var toPoint = p - closest;
            var offset = toPoint.magnitude;

            // Which flank, as a continuous value rather than a side: +1 off one flank, -1 off the
            // other, 0 along the axis. A sign test here would put a crease down the whole length.
            var sine = offset < 1e-4f
                ? 0f
                : Mathf.Clamp((axis.x * toPoint.y - axis.y * toPoint.x) / offset, -1f, 1f);

            var steepWeight = Mathf.Clamp01(0.5f + 0.5f * sine * SteepSide);
            steepWeight = steepWeight * steepWeight * (3f - 2f * steepWeight);

            var halfWidth = HalfWidthAt(s) * Mathf.Lerp(2f - Asymmetry, Asymmetry, steepWeight);
            if (halfWidth <= 1e-3f || distance >= halfWidth)
            {
                return 0f;
            }

            return CrestHeightMeters * height * CrossSection(distance / halfWidth);
        }

        /// <summary>
        /// The cross-section: a flat crown around the axis, then one smooth fall to the toe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The crown is what makes the body broad instead of a wall. The flank is a single smoothstep,
        /// which has exactly the authored character - zero slope at the crest, concave under it,
        /// convex at the toe, zero slope where it meets the ground - and is the lowest-curvature curve
        /// that has it.
        /// </para>
        /// <para>
        /// Not <see cref="MountainRange.FlankShape"/>, which is the same shape approximated by five
        /// control points. A profile made of smoothstep spans is forced to zero slope at every control
        /// point, so its second derivative goes as <c>6 dv / span^2</c> and rises as the points get
        /// closer: measured on the lattice, the five-point curve bends about three and a half times
        /// harder than the analytic one for an identical silhouette. Stage 2 keeps its own curve; this
        /// is the massif's, and the two stages no longer share a tuning surface.
        /// </para>
        /// </remarks>
        public float CrossSection(float u)
        {
            u = Mathf.Clamp01(u);
            if (u <= crownFraction)
            {
                return 1f;
            }

            var remaining = 1f - crownFraction;
            if (remaining <= 1e-4f)
            {
                return 0f;
            }

            var v = Mathf.Clamp01((u - crownFraction) / remaining);
            return 1f - v * v * (3f - 2f * v);
        }

        /// <summary>Which axis segment a world point projects onto, for diagnosis.</summary>
        public int NearestSegment(float worldX, float worldZ)
        {
            Nearest(new Vector2(worldX, worldZ), out var segment, out _, out _, out _);
            return segment;
        }

        /// <summary>Number of samples the projection polyline carries.</summary>
        public int ProjectionSampleCount => points.Length;

        /// <summary>A world point in the massif's own coordinates: along the axis, and off it.</summary>
        public void Project(float worldX, float worldZ, out float s, out float offsetMeters)
        {
            var p = new Vector2(worldX, worldZ);
            Nearest(p, out var segment, out var along, out var distance, out var closest);

            var arc = Mathf.Lerp(cumulative[segment], cumulative[segment + 1], along);
            s = LengthMeters > 1e-4f ? Mathf.Clamp01(arc / LengthMeters) : 0f;

            var axis = Vector2.Lerp(tangent[segment], tangent[segment + 1], along);
            if (axis.sqrMagnitude < 1e-8f)
            {
                axis = tangent[segment];
            }

            axis = axis.normalized;
            var toPoint = p - closest;
            var sign = axis.x * toPoint.y - axis.y * toPoint.x >= 0f ? 1f : -1f;
            offsetMeters = distance * sign;
        }

        /// <summary>
        /// Largest second difference the massif's own profiles produce on a lattice of this spacing.
        /// </summary>
        /// <remarks>
        /// The floor the measured continuity is judged against. A profile made of smoothstep spans has
        /// real curvature, concentrated where its control points are close together, so the field is
        /// not supposed to be flat between samples - it is supposed to bend by exactly this much. What
        /// would be a defect is bending by much more.
        /// </remarks>
        public float ProfileSecondDifferenceMeters(float cellSizeMeters)
        {
            var worst = 0f;

            var half = Mathf.Max(1f, effectiveHalfWidth);
            var du = cellSizeMeters / half;
            for (var u = du; u + du <= 1f; u += du * 0.25f)
            {
                var d2 = CrossSection(u + du) - 2f * CrossSection(u) + CrossSection(u - du);
                worst = Mathf.Max(worst, Mathf.Abs(d2) * CrestHeightMeters);
            }

            var ds = cellSizeMeters / Mathf.Max(1f, LengthMeters);
            for (var t = ds; t + ds <= 1f; t += ds * 0.25f)
            {
                var d2 = Longitudinal.Evaluate(t + ds) - 2f * Longitudinal.Evaluate(t)
                         + Longitudinal.Evaluate(t - ds);
                worst = Mathf.Max(worst, Mathf.Abs(d2) * CrestHeightMeters);
            }

            return worst;
        }

        /// <summary>Crest height, metres, at a normalised arc position.</summary>
        public float CrestAt(float s) => CrestHeightMeters * Longitudinal.Evaluate(Mathf.Clamp01(s));

        /// <summary>
        /// Half-width, metres, at a normalised arc position, before the flank asymmetry.
        /// </summary>
        /// <remarks>
        /// A function of how close the position is to an end, and of nothing else. Deriving it from
        /// the longitudinal profile instead would pinch the body at every saddle and swell it at every
        /// culmination, and a massif that does that reads as beads on a string rather than as one
        /// range. The floor keeps the ends from closing to a point, because a body that tapers to
        /// nothing in plan is a lens and a lens is a blob.
        /// </remarks>
        public float HalfWidthAt(float s)
        {
            var fromEnd = Mathf.Clamp01(s);
            fromEnd = Mathf.Min(fromEnd, 1f - fromEnd) / Mathf.Max(1e-4f, endTaperSpan);
            fromEnd = Mathf.Clamp01(fromEnd);
            var eased = fromEnd * fromEnd * (3f - 2f * fromEnd);
            return effectiveHalfWidth * Mathf.Lerp(widthTaperFloor, 1f, eased);
        }

        /// <summary>
        /// Curvature of the axis at a normalised arc position, 1/metres.
        /// </summary>
        /// <remarks>
        /// Menger curvature of three samples spanning about a fifth of the nominal half-width either
        /// side, which is the scale a belt of that width actually feels. Measured on the smoothed
        /// projection curve rather than on Stage 2A's stored polyline, because that curve is what the
        /// body is built on.
        /// </remarks>
        public float CurvatureAt(float s)
        {
            var span = Mathf.Max(2, Mathf.RoundToInt(effectiveHalfWidth * 0.2f / SampleSpacing()));
            var centre = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Clamp01(s) * (points.Length - 1)), span, points.Length - 1 - span);
            if (points.Length < 2 * span + 1)
            {
                return 0f;
            }

            var a = points[centre - span];
            var b = points[centre];
            var c = points[centre + span];

            var ab = (b - a).magnitude;
            var bc = (c - b).magnitude;
            var ca = (a - c).magnitude;
            var denominator = ab * bc * ca;
            if (denominator < 1e-6f)
            {
                return 0f;
            }

            var cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            return 2f * Mathf.Abs(cross) / denominator;
        }

        /// <summary>Mean spacing of the projection curve's samples, metres.</summary>
        public float SampleSpacing() =>
            points.Length > 1 ? LengthMeters / (points.Length - 1) : LengthMeters;

        /// <summary>Largest curvature anywhere along the axis, 1/metres.</summary>
        public float MaxCurvature()
        {
            var worst = 0f;
            for (var i = 0; i <= 200; i++)
            {
                worst = Mathf.Max(worst, CurvatureAt(i / 200f));
            }

            return worst;
        }

        /// <summary>
        /// A copy of this massif on a straight axis of the same length, for continuity diagnosis.
        /// </summary>
        /// <remarks>
        /// Everything the body is made of except the bending: the same profiles, the same width, the
        /// same crest, the same asymmetry, laid on a line. Whatever second difference this produces on
        /// a lattice is the model's own; whatever the real massif produces above it is what the axis's
        /// curvature and the projection onto it contribute.
        /// </remarks>
        public MountainMassif OnAStraightAxis(Vector2 origin, MassifSettings settings)
        {
            var count = Mathf.Max(2, Axis.Points.Length);
            var straight = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                straight[i] = origin + Vector2.right * (LengthMeters * i / (count - 1f));
            }

            var axis = new MacroAxis(
                straight, straight, System.Array.Empty<int>(),
                float.PositiveInfinity, Axis.MeanConvergence, HalfWidthMeters);

            return new MountainMassif(
                Index, axis, Longitudinal, CrestHeightMeters, HalfWidthMeters,
                Asymmetry, SteepSide, CulminationT, settings);
        }

        /// <summary>World point at a normalised arc position along the axis.</summary>
        public Vector2 PointAt(float s)
        {
            var target = Mathf.Clamp01(s) * LengthMeters;
            for (var i = 0; i + 1 < points.Length; i++)
            {
                if (target > cumulative[i + 1] && i + 2 < points.Length)
                {
                    continue;
                }

                var span = cumulative[i + 1] - cumulative[i];
                var u = span < 1e-5f ? 0f : Mathf.Clamp01((target - cumulative[i]) / span);
                return Vector2.LerpUnclamped(points[i], points[i + 1], u);
            }

            return points[points.Length - 1];
        }

        /// <summary>Closest point on the axis: which segment, how far along it, distance, and point.</summary>
        /// <remarks>
        /// Coarse pass over every <see cref="ProjectionSubdivisions"/>-th sample, then an exact pass
        /// over the segments around the winner. Safe because the curve's reach exceeds the widest the
        /// body ever gets - Stage 2A accepts an axis only on that condition, and
        /// <see cref="ReachBound"/> asserts it still holds - so inside the body the distance to the
        /// curve has one minimum along the arc and a local search cannot land on the wrong one.
        /// </remarks>
        private void Nearest(
            Vector2 p, out int segment, out float along, out float distance, out Vector2 closest)
        {
            var coarse = 0;
            var coarseSq = float.PositiveInfinity;
            for (var i = 0; i < points.Length; i += coarseStride)
            {
                var sq = (p - points[i]).sqrMagnitude;
                if (sq >= coarseSq)
                {
                    continue;
                }

                coarseSq = sq;
                coarse = i;
            }

            var lo = Mathf.Max(0, coarse - coarseStride);
            var hi = Mathf.Min(points.Length - 1, coarse + coarseStride);

            segment = lo;
            along = 0f;
            closest = points[lo];
            var bestSq = float.PositiveInfinity;

            for (var i = lo; i < hi; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var ab = b - a;
                var lengthSq = ab.sqrMagnitude;
                var t = lengthSq < 1e-8f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var q = a + ab * t;
                var sq = (p - q).sqrMagnitude;
                if (sq >= bestSq)
                {
                    continue;
                }

                bestSq = sq;
                segment = i;
                along = t;
                closest = q;
            }

            distance = Mathf.Sqrt(bestSq);
        }

        // ---------- construction ----------

        /// <summary>
        /// Builds the massif for one accepted axis, drawing its structure from the world seed.
        /// </summary>
        public static MountainMassif Build(
            Seed.IWorldSeed worldSeed,
            int index,
            MacroAxis axis,
            TerrainGenSettings terrainSettings,
            MassifSettings settings) =>
            Build(worldSeed, index, axis, terrainSettings, settings, new Vector2(0f, 1f));

        /// <inheritdoc cref="Build(Seed.IWorldSeed, int, MacroAxis, TerrainGenSettings, MassifSettings)"/>
        /// <param name="culminationWindow">
        /// The stretch of the axis certified to carry high ground, as normalised arc (start, end).
        /// </param>
        public static MountainMassif Build(
            Seed.IWorldSeed worldSeed,
            int index,
            MacroAxis axis,
            TerrainGenSettings terrainSettings,
            MassifSettings settings,
            Vector2 culminationWindow)
        {
            settings.Clamp();

            var rnd = worldSeed.Fork("terrain.massif", index);

            var crest = Mathf.Lerp(
                terrainSettings.CrestHeightMinMeters,
                terrainSettings.CrestHeightMaxMeters,
                (float)rnd.NextDouble());

            var asymmetry = Mathf.Lerp(
                settings.AsymmetryMin, settings.AsymmetryMax, (float)rnd.NextDouble());
            var steepSide = rnd.Next(2) == 0 ? 1 : -1;

            var longitudinal = BuildLongitudinal(
                rnd, axis.LengthMeters, settings, culminationWindow, out var culminations,
                out var core);

            // UsableHalfWidthMeters, not IntendedHalfWidthMeters. Stage 2A's intended width is what the
            // geology asked for; its usable width is what it certified the axis can carry, and the two
            // differ whenever an axis was accepted on a reach below the target. On the five canonical
            // seeds they coincide, which is why this was invisible until a wider spread of seeds was
            // run: seed 1 has a reach of 350 m, certifies 252 m, and intends 280 m. Taking the
            // intended figure there would put the width back in the hands of the curvature clamp,
            // which is the failure this whole line of work exists to prevent.
            return new MountainMassif(
                index, axis, longitudinal, crest, axis.UsableHalfWidthMeters,
                asymmetry, steepSide, culminations, settings, core);
        }

        /// <summary>
        /// The height profile along the axis, from a handful of structural stations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One culmination on a short axis, two on a long one, never more. The two are deliberately
        /// unequal and the saddle between them stays well above half the lower one, so the result is
        /// one massif with a high point and a shoulder rather than two hills sharing a line.
        /// </para>
        /// <para>
        /// No periodic function is involved and there is nothing to repeat: the profile is five to
        /// seven control points, smoothstepped. Smoothstep has zero slope at every control point,
        /// which is what makes the culminations and the saddle broad instead of pointed, and it
        /// cannot overshoot, so no bump appears that nobody placed.
        /// </para>
        /// </remarks>
        private static ProfileCurve BuildLongitudinal(
            System.Random rnd,
            float lengthMeters,
            MassifSettings settings,
            Vector2 core,
            out float[] culminations,
            out Vector2 effectiveCore)
        {
            var shoulderValue = settings.EndShoulderFraction;
            var plateauAt = settings.CrestPlateauSpan;
            var plateauValue = settings.CrestPlateauFraction;
            var margin = settings.MinRampFraction;

            // The certified core, kept clear of both ends so the outer ramps always have room. Every
            // control point above the shoulder lands inside it, which is what makes "the high ground
            // stands where its whole section clears the coast" true by construction rather than by
            // inspection afterwards.
            var c0 = Mathf.Clamp(core.x, margin, 1f - margin);
            var c1 = Mathf.Clamp(core.y, c0 + margin, 1f - margin);
            effectiveCore = new Vector2(c0, c1);

            // Two culminations only where the core is wide enough to hold both and the saddle between
            // them. Squeezed into a narrow core the three control points land a few hundredths apart
            // and the crest climbs thirteen metres between neighbouring samples - a cliff along the
            // crest line, from a profile that is smooth everywhere it has room to be.
            var wantsTwo = lengthMeters >= settings.SecondCulminationLengthMeters
                           && settings.MaxCulminations >= 2
                           && c1 - c0 >= settings.MinCoreSpanForTwoCulminations;

            var t = new List<float>(11);
            var v = new List<float>(11);

            t.Add(0f);
            v.Add(0f);
            t.Add(c0);
            v.Add(shoulderValue);

            if (!wantsTwo)
            {
                var jitter = (float)(rnd.NextDouble() * 2.0 - 1.0) * settings.CulminationJitter;
                var peak = Mathf.Lerp(c0, c1, Mathf.Clamp01(0.5f + jitter));
                peak = Mathf.Clamp(peak, c0 + (c1 - c0) * 0.25f, c1 - (c1 - c0) * 0.25f);
                culminations = new[] { peak };

                t.Add(peak - (peak - c0) * plateauAt);
                v.Add(plateauValue);
                t.Add(peak);
                v.Add(1f);
                t.Add(peak + (c1 - peak) * plateauAt);
                v.Add(plateauValue);
            }
            else
            {
                var jitterA = (float)(rnd.NextDouble() * 2.0 - 1.0) * settings.CulminationJitter * 0.5f;
                var jitterB = (float)(rnd.NextDouble() * 2.0 - 1.0) * settings.CulminationJitter * 0.5f;

                var first = Mathf.Lerp(c0, c1, Mathf.Clamp(0.28f + jitterA, 0.2f, 0.36f));
                var second = Mathf.Lerp(c0, c1, Mathf.Clamp(0.72f + jitterB, 0.64f, 0.8f));

                var secondary = Mathf.Lerp(
                    settings.SecondaryHeightMin, settings.SecondaryHeightMax, (float)rnd.NextDouble());
                var primaryFirst = rnd.Next(2) == 0;

                var firstHeight = primaryFirst ? 1f : secondary;
                var secondHeight = primaryFirst ? secondary : 1f;

                var saddleFraction = Mathf.Lerp(
                    settings.SaddleFractionMin, settings.SaddleFractionMax, (float)rnd.NextDouble());
                var saddle = Mathf.Min(firstHeight, secondHeight) * saddleFraction;

                culminations = new[] { first, second };

                t.Add(first - (first - c0) * plateauAt);
                v.Add(plateauValue * firstHeight);
                t.Add(first);
                v.Add(firstHeight);
                t.Add((first + second) * 0.5f);
                v.Add(saddle);
                t.Add(second);
                v.Add(secondHeight);
                t.Add(second + (c1 - second) * plateauAt);
                v.Add(plateauValue * secondHeight);
            }

            t.Add(c1);
            v.Add(shoulderValue);
            t.Add(1f);
            v.Add(0f);

            return new ProfileCurve(t.ToArray(), v.ToArray());
        }
    }
}
