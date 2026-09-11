using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// One uplift belt: a world-space spine plus the profiles that turn position along it and
    /// distance from it into height.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The spine is the primary object. Height is a function of two numbers and nothing else - how
    /// far along the range a point lies, and how far from its axis - so a belt is one continuous
    /// structure with variation along its length rather than a set of hills that happen to be in a
    /// row. There is no field, no octave and no noise here; a summit exists because the crest profile
    /// peaks there, and a flank is steep because the flank curve is steep.
    /// </para>
    /// <para>
    /// <b>The axis parameterisation is only valid while the belt is narrower than it bends.</b> The
    /// set of points at a given distance from a curve stops being well defined once the half-width
    /// exceeds the local radius of curvature: the tube self-intersects, two distant arc positions
    /// become equidistant from the same point, and the longitudinal coordinate jumps across that
    /// locus. That is what produced the straight rays in the first Stage 2 dump - half-widths of
    /// 450-570 m against junction curvature radii near 100-200 m - and it is a property of the
    /// geometry, not of the sampling. The half-width is therefore capped by the local curvature
    /// radius, which narrows a range where it turns sharply. A range that pinches at a bend is
    /// ordinary geography; a range whose coordinate chart folds over is a defect.
    /// </para>
    /// <para>
    /// <b>Asymmetry is what stops a cross-section reading as a mound.</b> The two flanks get
    /// different half-widths, so the same crest height falls away over different distances - a scarp
    /// on one side, a dip slope on the other.
    /// </para>
    /// </remarks>
    public sealed class MountainRange
    {
        /// <summary>Index in <see cref="WorldStructure.Ranges"/>. Also its seed stream key.</summary>
        public int Index { get; }

        /// <summary>Range this one branched from, or -1 when it is a primary belt.</summary>
        public int ParentIndex { get; }

        /// <summary>Smoothed, uniformly resampled world-space axis.</summary>
        public Vector2[] Spine { get; }

        /// <summary>Arc length from the first vertex to each vertex.</summary>
        public float[] Cumulative { get; }

        public float LengthMeters { get; }

        /// <summary>Crest elevation along the spine, normalised 0..1 of <see cref="CrestHeightMeters"/>.</summary>
        public ProfileCurve Crest { get; }

        /// <summary>Half-width along the spine, normalised 0..1 of <see cref="HalfWidthMeters"/>.</summary>
        public ProfileCurve HalfWidth { get; }

        public float CrestHeightMeters { get; }

        /// <summary>Half-width at the range's widest point before the curvature cap, metres.</summary>
        public float HalfWidthMeters { get; }

        /// <summary>Half-width multiplier on the steep flank; below 1, so that side is the scarp.</summary>
        public float Asymmetry { get; }

        /// <summary>Which side of the spine is the scarp: +1 or -1.</summary>
        public int SteepSide { get; }

        /// <summary>Crustal site pairs the spine was chained from, flattened, for provenance.</summary>
        public int[] SourceSitePairs { get; }

        /// <summary>Everything outside this rectangle contributes nothing.</summary>
        public Rect Support { get; }

        /// <summary>
        /// Mean of the effective half-width along the range, metres - what the belt is actually as
        /// wide as, after the curvature cap.
        /// </summary>
        public float CharacteristicHalfWidthMeters { get; private set; }

        /// <summary>Length over characteristic half-width. Below about three, a belt reads as a hill.</summary>
        public float LengthToWidthRatio =>
            CharacteristicHalfWidthMeters > 1e-3f ? LengthMeters / CharacteristicHalfWidthMeters : 0f;

        /// <summary>Fraction of the range where the curvature cap, not the profile, set the width.</summary>
        public float CurvatureLimitedFraction { get; private set; }

        /// <summary>
        /// How far the half-width may reach as a fraction of the local curvature radius.
        /// </summary>
        /// <remarks>
        /// Below one by a clear margin, because the tube has to stay embedded with room to spare: at
        /// exactly the curvature radius the far flank collapses to a point.
        /// </remarks>
        public const float CurvatureWidthFactor = 0.72f;

        /// <summary>
        /// Cross-range shape, shared by every belt: crest, upper mountain, flank, foothill, ground.
        /// </summary>
        /// <remarks>
        /// Monotonic and smooth, concave under the crest and convex at the toe. The convex toe is the
        /// talus apron; a flank that meets flat ground at a hard angle is the clearest "this was
        /// generated" tell there is.
        /// </remarks>
        public static readonly ProfileCurve FlankShape = new ProfileCurve(
            new[] { 0f, 0.22f, 0.50f, 0.78f, 1f },
            new[] { 1f, 0.88f, 0.52f, 0.16f, 0f });

        private float[] sampleT;
        private float[] crestValue;
        private float[] halfWidthEffective;
        private Vector2[] tangent;

        public MountainRange(
            int index,
            int parentIndex,
            Vector2[] spine,
            ProfileCurve crest,
            ProfileCurve halfWidth,
            float crestHeightMeters,
            float halfWidthMeters,
            float asymmetry,
            int steepSide,
            int[] sourceSitePairs)
        {
            Index = index;
            ParentIndex = parentIndex;
            Spine = spine;
            Crest = crest;
            HalfWidth = halfWidth;
            CrestHeightMeters = crestHeightMeters;
            HalfWidthMeters = halfWidthMeters;
            Asymmetry = asymmetry;
            SteepSide = steepSide >= 0 ? 1 : -1;
            SourceSitePairs = sourceSitePairs ?? System.Array.Empty<int>();

            Cumulative = new float[spine.Length];
            for (var i = 1; i < spine.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (spine[i] - spine[i - 1]).magnitude;
            }

            LengthMeters = Cumulative[spine.Length - 1];
            Precompute();

            var widest = 0f;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < spine.Length; i++)
            {
                min = Vector2.Min(min, spine[i]);
                max = Vector2.Max(max, spine[i]);
                widest = Mathf.Max(widest, halfWidthEffective[i]);
            }

            widest *= Mathf.Max(asymmetry, 2f - asymmetry);
            Support = Rect.MinMaxRect(
                min.x - widest, min.y - widest, max.x + widest, max.y + widest);
        }

        /// <summary>Height this belt contributes at a world point, in metres. Never negative.</summary>
        /// <remarks>
        /// One lookup of the longitudinal profile and one of the cross-range profile, both at the
        /// point's position in the range's own coordinates. Deliberately <em>not</em> a maximum over
        /// radial influences around each crest sample: that formulation makes every sample an
        /// independent hill, and a row of independent hills is what a range must not be.
        /// </remarks>
        public float Evaluate(float worldX, float worldZ)
        {
            if (worldX < Support.xMin || worldX > Support.xMax ||
                worldZ < Support.yMin || worldZ > Support.yMax)
            {
                return 0f;
            }

            var p = new Vector2(worldX, worldZ);
            Nearest(p, out var segment, out var along, out var distance, out var closest);

            var crest = Mathf.Lerp(crestValue[segment], crestValue[segment + 1], along);
            if (crest <= 0f)
            {
                return 0f;
            }

            var baseHalfWidth =
                Mathf.Lerp(halfWidthEffective[segment], halfWidthEffective[segment + 1], along);
            if (baseHalfWidth <= 1e-3f)
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

            // Which flank, as a continuous value rather than a side: plus one directly off one side
            // of the crest, minus one off the other, zero along the axis. A sign test here would put
            // a crease down the length of the range.
            var sine = offset < 1e-4f
                ? 0f
                : Mathf.Clamp((axis.x * toPoint.y - axis.y * toPoint.x) / offset, -1f, 1f);

            var steepWeight = Mathf.Clamp01(0.5f + 0.5f * sine * SteepSide);
            steepWeight = steepWeight * steepWeight * (3f - 2f * steepWeight);
            var halfWidth = baseHalfWidth * Mathf.Lerp(2f - Asymmetry, Asymmetry, steepWeight);

            if (distance >= halfWidth)
            {
                return 0f;
            }

            return CrestHeightMeters * crest * FlankShape.Evaluate(distance / halfWidth);
        }

        /// <summary>
        /// A world point in the range's own coordinates: how far along it, how far from its axis.
        /// </summary>
        /// <remarks>
        /// Exposed because measuring a belt in world axes misjudges it. A curved range fills a fat
        /// bounding ellipse and scores as round under any covariance measure, however narrow it
        /// actually is; in its own frame its extent along the axis and its spread across the axis are
        /// what they look like.
        /// </remarks>
        public void Project(float worldX, float worldZ, out float t, out float offsetMeters)
        {
            var p = new Vector2(worldX, worldZ);
            Nearest(p, out var segment, out var along, out var distance, out var closest);

            var arc = Mathf.Lerp(Cumulative[segment], Cumulative[segment + 1], along);
            t = LengthMeters > 1e-4f ? Mathf.Clamp01(arc / LengthMeters) : 0f;

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

        /// <summary>Crest height, in metres, at a normalised arc position.</summary>
        public float CrestAt(float t) => CrestHeightMeters * Crest.Evaluate(t);

        /// <summary>Effective half-width, in metres, at a normalised arc position.</summary>
        public float HalfWidthAt(float t)
        {
            if (LengthMeters <= 1e-4f)
            {
                return halfWidthEffective[0];
            }

            var target = Mathf.Clamp01(t) * LengthMeters;
            for (var i = 0; i + 1 < Spine.Length; i++)
            {
                if (target > Cumulative[i + 1] && i + 2 < Spine.Length)
                {
                    continue;
                }

                var span = Cumulative[i + 1] - Cumulative[i];
                var u = span < 1e-5f ? 0f : Mathf.Clamp01((target - Cumulative[i]) / span);
                return Mathf.Lerp(halfWidthEffective[i], halfWidthEffective[i + 1], u);
            }

            return halfWidthEffective[Spine.Length - 1];
        }

        /// <summary>World point at a normalised arc position along the spine.</summary>
        public Vector2 PointAt(float t)
        {
            var target = Mathf.Clamp01(t) * LengthMeters;
            for (var i = 0; i + 1 < Spine.Length; i++)
            {
                if (target > Cumulative[i + 1] && i + 2 < Spine.Length)
                {
                    continue;
                }

                var span = Cumulative[i + 1] - Cumulative[i];
                var u = span < 1e-5f ? 0f : Mathf.Clamp01((target - Cumulative[i]) / span);
                return Vector2.LerpUnclamped(Spine[i], Spine[i + 1], u);
            }

            return Spine[Spine.Length - 1];
        }

        /// <summary>
        /// Closest point on the spine: which segment, how far along it, the distance, and the point.
        /// </summary>
        /// <remarks>
        /// A linear scan over the spine's own segments. The support rectangle has already rejected
        /// everything far away, and a spine carries under two hundred samples, so an index here would
        /// cost more than it saves.
        /// </remarks>
        private void Nearest(
            Vector2 p, out int segment, out float along, out float distance, out Vector2 closest)
        {
            var bestSq = float.PositiveInfinity;
            segment = 0;
            along = 0f;
            closest = Spine[0];

            for (var i = 0; i + 1 < Spine.Length; i++)
            {
                var a = Spine[i];
                var ab = Spine[i + 1] - a;
                var lengthSq = ab.sqrMagnitude;
                var u = lengthSq < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSq);
                var point = a + ab * u;
                var sq = (p - point).sqrMagnitude;
                if (sq >= bestSq)
                {
                    continue;
                }

                bestSq = sq;
                segment = i;
                along = u;
                closest = point;
            }

            distance = Mathf.Sqrt(bestSq);
        }

        /// <summary>
        /// Per-sample crest height, effective half-width and smoothed tangent.
        /// </summary>
        /// <remarks>
        /// The effective half-width is the profile's width capped by the local curvature radius, and
        /// then run through a rolling minimum so the cap eases in rather than notching the range at a
        /// single sample. Capping is what keeps the axis coordinates single-valued; the rolling
        /// minimum is what keeps the resulting narrowing a taper instead of a nick.
        /// </remarks>
        private void Precompute()
        {
            var count = Spine.Length;
            sampleT = new float[count];
            crestValue = new float[count];
            halfWidthEffective = new float[count];
            tangent = new Vector2[count];

            var window = Mathf.Max(1, Mathf.RoundToInt(count * 0.05f));

            var reach = ComputeReach();
            var rawWidth = new float[count];
            var curvatureLimited = 0;

            for (var i = 0; i < count; i++)
            {
                var t = LengthMeters > 1e-4f ? Mathf.Clamp01(Cumulative[i] / LengthMeters) : 0f;
                sampleT[i] = t;
                crestValue[i] = Crest.Evaluate(t);

                var ahead = Spine[Mathf.Min(count - 1, i + window)];
                var behind = Spine[Mathf.Max(0, i - window)];
                var direction = ahead - behind;
                if (direction.sqrMagnitude < 1e-8f)
                {
                    direction = i + 1 < count ? Spine[i + 1] - Spine[i] : Spine[i] - Spine[i - 1];
                }

                tangent[i] = direction.normalized;

                var profileWidth = HalfWidthMeters * HalfWidth.Evaluate(t);
                var capped = Mathf.Min(profileWidth, CurvatureWidthFactor * reach[i]);
                if (capped < profileWidth - 0.5f)
                {
                    curvatureLimited++;
                }

                rawWidth[i] = capped;
            }

            // Rolling minimum: a cap that applies at one sample has to apply to its neighbours too,
            // or the range steps in and out of the narrowing over a few metres.
            var span = Mathf.Max(1, window);
            var total = 0f;
            for (var i = 0; i < count; i++)
            {
                var lowest = float.PositiveInfinity;
                for (var k = Mathf.Max(0, i - span); k <= Mathf.Min(count - 1, i + span); k++)
                {
                    lowest = Mathf.Min(lowest, rawWidth[k]);
                }

                halfWidthEffective[i] = lowest;
                total += lowest;
            }

            CharacteristicHalfWidthMeters = count > 0 ? total / count : 0f;
            CurvatureLimitedFraction = count > 0 ? curvatureLimited / (float)count : 0f;
        }

        /// <summary>
        /// Reach of the spine at each sample: how wide a tube around it can be before it folds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One exact quantity in place of two heuristics that each measured half the problem. For a
        /// curve, the reach at a point is the smallest of <c>|q - p|^2 / (2 d)</c> over every other
        /// sample q, where d is how far q lies off the tangent line at p. As q approaches p this
        /// tends to the radius of curvature, and for a distant arm of the same curve it gives half
        /// the separation - so a single formula bounds both the tight bend and the near contact, at
        /// every scale at once and with no window to choose.
        /// </para>
        /// <para>
        /// Choosing that window is what the two previous attempts got wrong. A short baseline reads a
        /// resampled polyline as locally straight; a long one straddles a bend and averages it away.
        /// Seed 110201 folded under both, and under a multi-scale compromise, before this.
        /// </para>
        /// </remarks>
        private float[] ComputeReach()
        {
            var count = Spine.Length;
            var reach = new float[count];
            var localTangent = new Vector2[count];

            for (var i = 0; i < count; i++)
            {
                var ahead = Spine[Mathf.Min(count - 1, i + 1)];
                var behind = Spine[Mathf.Max(0, i - 1)];
                var direction = ahead - behind;
                localTangent[i] = direction.sqrMagnitude < 1e-10f ? Vector2.right : direction.normalized;
            }

            for (var i = 0; i < count; i++)
            {
                var best = float.MaxValue;
                var p = Spine[i];
                var t = localTangent[i];

                for (var j = 0; j < count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var v = Spine[j] - p;
                    var lengthSq = v.sqrMagnitude;
                    if (lengthSq < 1e-6f)
                    {
                        continue;
                    }

                    var offAxis = Mathf.Abs(t.x * v.y - t.y * v.x);
                    if (offAxis < 1e-4f)
                    {
                        continue;
                    }

                    best = Mathf.Min(best, lengthSq / (2f * offAxis));
                }

                reach[i] = best;
            }

            return reach;
        }
    }
}
