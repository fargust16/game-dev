using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// A candidate mountain axis: an arc-chain curve with a proven curvature radius, plus everything
    /// needed to judge whether a belt of a given half-width can be built on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geometry only. This type carries no height, no crest profile and no flank shape, because the
    /// question this pass exists to answer is whether a usable axis can be produced at all. A belt is
    /// a tube around a curve, and a tube of half-width W only embeds -- only fails to fold over itself
    /// -- when W stays under the curve's reach. Stage 2 failed on exactly that, so the axis has to
    /// carry its own proof before anything is built on it.
    /// </para>
    /// <para>
    /// The construction is a chain of straight segments joined by circular arcs, so the curvature
    /// radius is not measured after the fact and hoped to be adequate: it is a constructor argument.
    /// <see cref="ConstructedRadiusMeters"/> is what the fillets were built to; <see cref="ReachMeters"/>
    /// is what the finished polyline actually measures, and it is the smaller of the two whenever the
    /// curve doubles back near itself.
    /// </para>
    /// </remarks>
    public sealed class MacroAxis
    {
        /// <summary>Sampled polyline, evenly spaced along the curve.</summary>
        public Vector2[] Points { get; }

        /// <summary>Arc length at each point, metres.</summary>
        public float[] Cumulative { get; }

        /// <summary>Total arc length, metres.</summary>
        public float LengthMeters { get; }

        /// <summary>The chain corners the arc-chain was filleted from, before rounding.</summary>
        public Vector2[] ControlPoints { get; }

        /// <summary>Plate pairs this axis was chained from, as <c>[i * 2 + 0/1]</c>.</summary>
        public int[] SourcePlatePairs { get; }

        /// <summary>Smallest fillet radius used, metres. Infinite when the axis is a single segment.</summary>
        public float ConstructedRadiusMeters { get; }

        /// <summary>Mean convergence of the boundaries the chain ran along.</summary>
        public float MeanConvergence { get; }

        /// <summary>Smallest reach over the sampled curve, metres.</summary>
        /// <remarks>
        /// Federer's reach, evaluated discretely: for a point p with unit tangent t, every other point
        /// q gives <c>|q - p|^2 / (2 |t x (q - p)|)</c>, which is the radius of the circle through p
        /// tangent to t and passing through q. The smallest such radius bounds both how tightly the
        /// curve bends and how closely it approaches itself, which is why one number is enough.
        /// </remarks>
        public float ReachMeters { get; }

        /// <summary>
        /// Largest reach this world can distinguish from a straight line, metres.
        /// </summary>
        /// <remarks>
        /// The world's diagonal. Beyond it a curve is straight as far as anything in the world is
        /// concerned, and <see cref="ReachMeters"/> is reported as positive infinity rather than as
        /// whatever finite number the arithmetic happened to produce.
        /// </remarks>
        public static float MaxMeasurableReachMeters =>
            Mathf.Sqrt(
                WorldScale.WorldExtentXMeters * WorldScale.WorldExtentXMeters
                + WorldScale.WorldExtentZMeters * WorldScale.WorldExtentZMeters);

        /// <summary>
        /// Smallest distance from this axis to the coastline over its whole length, metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The certified half of the geometry contract. Stage 2B raises a body that reaches
        /// <see cref="UsableHalfWidthMeters"/> either side of the axis and fades it in over a coastal
        /// gate, so an axis is only usable where the coast is at least
        /// <c>gate + half-width</c> away. Certifying the width without certifying the room for it is
        /// what let seed 1337 through: a legal axis, a legal width, and nowhere to put them.
        /// </para>
        /// <para>
        /// Positive infinity on an axis built for diagnosis rather than by the builder, which has no
        /// coastline to be near.
        /// </para>
        /// </remarks>
        public float CoastClearanceMeters { get; internal set; } = float.PositiveInfinity;

        /// <summary>
        /// The stretch of this axis certified to carry high ground, as normalised arc (start, end).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The other half of the contract, and the one that keeps it from destroying geography. A
        /// massif's ends taper to nothing, so they may run close to the coast without harm; only the
        /// high ground has to stand where its whole cross-section clears the coastal gate. Certifying
        /// the <em>whole</em> axis at that clearance rejected four of the five canonical worlds, all
        /// of which were in fact sound - the corridor a 1.7 km island offers is about 600 m, and their
        /// axes were 800 to 1600 m of which only the middle needed the room.
        /// </para>
        /// <para>
        /// Stage 2B may put its culminations only inside this interval. Outside it the crest is on its
        /// way to zero, which is what a range running down to the sea looks like.
        /// </para>
        /// <para>
        /// The whole axis on one built for diagnosis rather than by the builder.
        /// </para>
        /// </remarks>
        public Vector2 CertifiedCore { get; internal set; } = new Vector2(0f, 1f);

        /// <summary>Length of the certified core, metres.</summary>
        public float CoreLengthMeters => Mathf.Max(0f, CertifiedCore.y - CertifiedCore.x) * LengthMeters;

        /// <summary>Closest approach between parts of the curve more than a bend apart, metres.</summary>
        public float MinSelfDistanceMeters { get; }

        /// <summary>Half-width a belt on this axis could carry, metres.</summary>
        public float UsableHalfWidthMeters { get; }

        /// <summary>Half-width the settings asked for, metres.</summary>
        public float IntendedHalfWidthMeters { get; }

        /// <summary>Corners of the control polygon turning by more than 30 degrees.</summary>
        public int MajorDirectionChanges { get; }

        /// <summary>Largest turn in the control polygon, degrees.</summary>
        public float MaxTurnDegrees { get; }

        /// <summary>Whether any two non-adjacent sampled segments cross.</summary>
        public bool SelfIntersects { get; }

        /// <summary>Whether the curve closes back on itself within a sample spacing.</summary>
        public bool Loops { get; }

        /// <summary>Length over intended half-width. Under about 3 a belt reads as a hill.</summary>
        public float LengthToWidthRatio =>
            IntendedHalfWidthMeters > 1e-4f ? LengthMeters / IntendedHalfWidthMeters : 0f;

        /// <summary>Mean spacing of the control polygon, metres.</summary>
        public float ControlSpacingMeters
        {
            get
            {
                if (ControlPoints.Length < 2)
                {
                    return 0f;
                }

                var total = 0f;
                for (var i = 0; i + 1 < ControlPoints.Length; i++)
                {
                    total += (ControlPoints[i + 1] - ControlPoints[i]).magnitude;
                }

                return total / (ControlPoints.Length - 1);
            }
        }

        /// <summary>Largest curvature along the curve, 1/metres.</summary>
        public float MaxCurvature => ReachMeters > 1e-4f ? 1f / ReachMeters : float.PositiveInfinity;

        /// <summary>Index within its pass. Assigned by the builder, negative until then.</summary>
        public int Index { get; internal set; } = -1;

        /// <summary>Whether the pass kept this axis.</summary>
        public bool Accepted { get; internal set; }

        /// <summary>Why the pass dropped it, or an empty string when it did not.</summary>
        public string RejectReason { get; internal set; } = string.Empty;

        /// <summary>Along-curve separation beyond which two points count as a self-approach, metres.</summary>
        private const float SelfApproachArcMeters = 300f;

        public MacroAxis(
            Vector2[] points,
            Vector2[] controlPoints,
            int[] sourcePlatePairs,
            float constructedRadiusMeters,
            float meanConvergence,
            float intendedHalfWidthMeters)
        {
            Points = points;
            ControlPoints = controlPoints ?? System.Array.Empty<Vector2>();
            SourcePlatePairs = sourcePlatePairs ?? System.Array.Empty<int>();
            ConstructedRadiusMeters = constructedRadiusMeters;
            MeanConvergence = meanConvergence;
            IntendedHalfWidthMeters = intendedHalfWidthMeters;

            Cumulative = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (points[i] - points[i - 1]).magnitude;
            }

            LengthMeters = points.Length > 0 ? Cumulative[points.Length - 1] : 0f;

            ReachMeters = ComputeReach(points);
            UsableHalfWidthMeters = Mathf.Min(
                intendedHalfWidthMeters, MountainRange.CurvatureWidthFactor * ReachMeters);

            MinSelfDistanceMeters = ComputeMinSelfDistance(points, Cumulative);
            SelfIntersects = ComputeSelfIntersects(points);
            Loops = points.Length > 3
                    && (points[points.Length - 1] - points[0]).magnitude < SelfApproachArcMeters * 0.25f;

            MeasureTurns(ControlPoints, out var major, out var maxTurn);
            MajorDirectionChanges = major;
            MaxTurnDegrees = maxTurn;
        }

        /// <summary>Closest distance from a world point to the curve, metres.</summary>
        public float DistanceTo(float worldX, float worldZ)
        {
            var p = new Vector2(worldX, worldZ);
            var best = float.PositiveInfinity;
            for (var i = 0; i + 1 < Points.Length; i++)
            {
                var closest = Town.Geometry.Geo2.ClosestPointOnSegment(p, Points[i], Points[i + 1]);
                best = Mathf.Min(best, (closest - p).magnitude);
            }

            return best;
        }

        /// <summary>Axis-aligned bounds of the curve, inflated by <paramref name="padMeters"/>.</summary>
        public Rect Bounds(float padMeters)
        {
            var minX = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;
            for (var i = 0; i < Points.Length; i++)
            {
                minX = Mathf.Min(minX, Points[i].x);
                minZ = Mathf.Min(minZ, Points[i].y);
                maxX = Mathf.Max(maxX, Points[i].x);
                maxZ = Mathf.Max(maxZ, Points[i].y);
            }

            return Rect.MinMaxRect(
                minX - padMeters, minZ - padMeters, maxX + padMeters, maxZ + padMeters);
        }

        /// <summary>The curve offset sideways by <paramref name="offsetMeters"/>, for drawing.</summary>
        /// <remarks>
        /// The tube's own wall. When the axis carries the half-width it claims, the two walls stay
        /// apart; when it does not, they cross, and that crossing is the folded chart Stage 2 produced.
        /// </remarks>
        public Vector2[] Offset(float offsetMeters)
        {
            var result = new Vector2[Points.Length];
            for (var i = 0; i < Points.Length; i++)
            {
                var ahead = Points[Mathf.Min(Points.Length - 1, i + 1)];
                var behind = Points[Mathf.Max(0, i - 1)];
                var tangent = ahead - behind;
                if (tangent.sqrMagnitude < 1e-10f)
                {
                    tangent = Vector2.right;
                }

                tangent.Normalize();
                result[i] = Points[i] + new Vector2(-tangent.y, tangent.x) * offsetMeters;
            }

            return result;
        }

        private static float ComputeReach(Vector2[] points)
        {
            var count = points.Length;
            if (count < 3)
            {
                return float.PositiveInfinity;
            }

            var tangent = new Vector2[count];
            for (var i = 0; i < count; i++)
            {
                var direction = points[Mathf.Min(count - 1, i + 1)] - points[Mathf.Max(0, i - 1)];
                tangent[i] = direction.sqrMagnitude < 1e-10f ? Vector2.right : direction.normalized;
            }

            var best = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var p = points[i];
                var t = tangent[i];
                for (var j = 0; j < count; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    var v = points[j] - p;
                    var lengthSq = v.sqrMagnitude;
                    if (lengthSq < 1e-6f)
                    {
                        continue;
                    }

                    var offAxis = Mathf.Abs(t.x * v.y - t.y * v.x);
                    if (offAxis <= 0f)
                    {
                        continue;
                    }

                    var radius = lengthSq / (2f * offAxis);

                    // A radius larger than the world cannot be told from a straight line by anything
                    // inside the world, and the float noise in the cross product of two points a
                    // kilometre apart is easily enough to manufacture one. Discarding those is what
                    // makes an unbent axis report an unbounded reach instead of a arbitrary six
                    // million metres - a number that is not a measurement of anything.
                    if (radius > MaxMeasurableReachMeters)
                    {
                        continue;
                    }

                    best = Mathf.Min(best, radius);
                }
            }

            return best;
        }

        private static float ComputeMinSelfDistance(Vector2[] points, float[] cumulative)
        {
            var best = float.PositiveInfinity;
            for (var i = 0; i < points.Length; i++)
            {
                for (var j = i + 1; j < points.Length; j++)
                {
                    if (cumulative[j] - cumulative[i] < SelfApproachArcMeters)
                    {
                        continue;
                    }

                    best = Mathf.Min(best, (points[j] - points[i]).magnitude);
                }
            }

            return best;
        }

        private static bool ComputeSelfIntersects(Vector2[] points)
        {
            for (var i = 0; i + 1 < points.Length; i++)
            {
                for (var j = i + 2; j + 1 < points.Length; j++)
                {
                    if (j == i + 1)
                    {
                        continue;
                    }

                    if (Town.Geometry.Geo2.SegmentIntersect(
                            points[i], points[i + 1], points[j], points[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void MeasureTurns(Vector2[] control, out int major, out float maxTurn)
        {
            major = 0;
            maxTurn = 0f;
            for (var i = 1; i + 1 < control.Length; i++)
            {
                var incoming = control[i] - control[i - 1];
                var outgoing = control[i + 1] - control[i];
                if (incoming.sqrMagnitude < 1e-8f || outgoing.sqrMagnitude < 1e-8f)
                {
                    continue;
                }

                var turn = Vector2.Angle(incoming, outgoing);
                maxTurn = Mathf.Max(maxTurn, turn);
                if (turn > 30f)
                {
                    major++;
                }
            }
        }

        /// <summary>
        /// Rounds every corner of a polyline with a circular arc, so the result has a curvature radius
        /// no smaller than <paramref name="radiusMeters"/> wherever a fillet fitted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The trim a fillet needs at a corner is <c>R * tan(theta / 2)</c> on each side, so a segment
        /// has to be long enough for the fillets at both its ends. Where it is not, the corner is
        /// reported through <paramref name="fitFailures"/> and left sharp; the caller splits the chain
        /// there rather than quietly accepting a curve it cannot build a belt on.
        /// </para>
        /// <para>
        /// Deliberately not smoothing. A Chaikin or Catmull-Rom pass would round the corner too, but
        /// with no radius anyone can name and no way to say afterwards what half-width the result
        /// carries. Here the radius is the input.
        /// </para>
        /// </remarks>
        public static Vector2[] Fillet(
            IReadOnlyList<Vector2> polyline,
            float radiusMeters,
            float sampleSpacingMeters,
            out List<int> fitFailures)
        {
            fitFailures = new List<int>();
            if (polyline == null || polyline.Count < 2)
            {
                return System.Array.Empty<Vector2>();
            }

            if (polyline.Count == 2)
            {
                return Resample(polyline, sampleSpacingMeters);
            }

            var trim = new float[polyline.Count];
            for (var i = 1; i + 1 < polyline.Count; i++)
            {
                var incoming = polyline[i] - polyline[i - 1];
                var outgoing = polyline[i + 1] - polyline[i];
                if (incoming.sqrMagnitude < 1e-8f || outgoing.sqrMagnitude < 1e-8f)
                {
                    continue;
                }

                var turn = Vector2.Angle(incoming, outgoing) * Mathf.Deg2Rad;
                if (turn < 1e-3f)
                {
                    continue;
                }

                trim[i] = radiusMeters * Mathf.Tan(Mathf.Min(turn, Mathf.PI * 0.98f) * 0.5f);
            }

            // A segment carries the fillets at both its ends. Where the two do not fit, the corner
            // that asked for more is the one that cannot be built.
            for (var s = 0; s + 1 < polyline.Count; s++)
            {
                var length = (polyline[s + 1] - polyline[s]).magnitude;
                var needed = trim[s] + trim[s + 1];
                if (needed <= length * 0.98f)
                {
                    continue;
                }

                if (trim[s] >= trim[s + 1] && s > 0)
                {
                    fitFailures.Add(s);
                }
                else if (s + 1 < polyline.Count - 1)
                {
                    fitFailures.Add(s + 1);
                }
                else if (s > 0)
                {
                    fitFailures.Add(s);
                }
            }

            if (fitFailures.Count > 0)
            {
                fitFailures.Sort();
                for (var i = fitFailures.Count - 1; i > 0; i--)
                {
                    if (fitFailures[i] == fitFailures[i - 1])
                    {
                        fitFailures.RemoveAt(i);
                    }
                }

                return System.Array.Empty<Vector2>();
            }

            var curve = new List<Vector2> { polyline[0] };
            for (var i = 1; i + 1 < polyline.Count; i++)
            {
                var incoming = (polyline[i] - polyline[i - 1]).normalized;
                var outgoing = (polyline[i + 1] - polyline[i]).normalized;
                if (trim[i] <= 1e-4f)
                {
                    curve.Add(polyline[i]);
                    continue;
                }

                var start = polyline[i] - incoming * trim[i];
                var end = polyline[i] + outgoing * trim[i];

                var turn = Vector2.SignedAngle(incoming, outgoing) * Mathf.Deg2Rad;
                var side = turn >= 0f ? 1f : -1f;
                var centre = start + new Vector2(-incoming.y, incoming.x) * (side * radiusMeters);

                var fromStart = start - centre;
                var fromEnd = end - centre;
                var sweep = Mathf.Atan2(
                    fromStart.x * fromEnd.y - fromStart.y * fromEnd.x,
                    fromStart.x * fromEnd.x + fromStart.y * fromEnd.y);

                var arcLength = Mathf.Abs(sweep) * radiusMeters;
                var steps = Mathf.Max(2, Mathf.CeilToInt(arcLength / sampleSpacingMeters));
                for (var k = 0; k <= steps; k++)
                {
                    var angle = sweep * (k / (float)steps);
                    var cos = Mathf.Cos(angle);
                    var sin = Mathf.Sin(angle);
                    curve.Add(centre + new Vector2(
                        fromStart.x * cos - fromStart.y * sin,
                        fromStart.x * sin + fromStart.y * cos));
                }
            }

            curve.Add(polyline[polyline.Count - 1]);
            return Resample(curve, sampleSpacingMeters);
        }

        /// <summary>Even resampling that keeps both endpoints.</summary>
        public static Vector2[] Resample(IReadOnlyList<Vector2> polyline, float spacingMeters)
        {
            if (polyline == null || polyline.Count == 0)
            {
                return System.Array.Empty<Vector2>();
            }

            if (polyline.Count == 1)
            {
                return new[] { polyline[0] };
            }

            var total = 0f;
            for (var i = 0; i + 1 < polyline.Count; i++)
            {
                total += (polyline[i + 1] - polyline[i]).magnitude;
            }

            if (total < 1e-4f)
            {
                return new[] { polyline[0], polyline[polyline.Count - 1] };
            }

            var steps = Mathf.Max(1, Mathf.RoundToInt(total / Mathf.Max(1e-3f, spacingMeters)));
            var result = new Vector2[steps + 1];
            result[0] = polyline[0];
            result[steps] = polyline[polyline.Count - 1];

            var segment = 0;
            var walked = 0f;
            var segmentLength = (polyline[1] - polyline[0]).magnitude;

            for (var k = 1; k < steps; k++)
            {
                var target = total * (k / (float)steps);
                while (segment + 2 < polyline.Count && walked + segmentLength < target)
                {
                    walked += segmentLength;
                    segment++;
                    segmentLength = (polyline[segment + 1] - polyline[segment]).magnitude;
                }

                var t = segmentLength > 1e-6f ? Mathf.Clamp01((target - walked) / segmentLength) : 0f;
                result[k] = Vector2.Lerp(polyline[segment], polyline[segment + 1], t);
            }

            return result;
        }
    }
}
