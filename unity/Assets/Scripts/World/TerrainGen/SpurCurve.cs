using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Why a spur stopped where it did.</summary>
    /// <remarks>
    /// Every one of these is a boundary the Stage 3A contract drew, not a length the builder chose.
    /// <see cref="Toe"/> is the spur reaching its intended length; the rest are the structural domain,
    /// the coastline or a neighbouring massif getting there first.
    /// </remarks>
    public enum SpurTermination
    {
        /// <summary>It reached its intended reach. The ordinary outcome.</summary>
        Toe = 0,

        /// <summary>It met the coastline.</summary>
        Coast = 1,

        /// <summary>It reached the edge of the structural domain, where the frame stops being single-valued.</summary>
        Domain = 2,

        /// <summary>It ran past an end of the axis, where there is no flank to descend.</summary>
        AxisEnd = 3,

        /// <summary>A neighbouring massif's territory began. The inter-massif divide.</summary>
        Divide = 4,

        /// <summary>Land no massif owns.</summary>
        Unclaimed = 5,
    }

    /// <summary>
    /// A primary spur: one ridge descending a massif's flank from the crest.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Explicit geometry, built in the massif's own <c>(s, d)</c> frame and then written out as world
    /// points. It is not sampled from a field, not traced along a gradient and not carved from noise -
    /// the polyline is the structure, and the height field a later slice cuts will be derived from it
    /// rather than the other way round.
    /// </para>
    /// <para>
    /// Depth 1 in the design's ridge forest. Depth 0 is the massif's own axis, which Stage 2 owns and
    /// this stage only refers to through <see cref="MassifIndex"/>; depth 2 belongs to a later slice
    /// and there is deliberately no field here anticipating it.
    /// </para>
    /// </remarks>
    public sealed class SpurCurve
    {
        /// <summary>Index within the world's spur list.</summary>
        public int Id { get; }

        /// <summary>Which massif's crest this spur leaves.</summary>
        public int MassifIndex { get; }

        /// <summary>Which flank it descends, +1 or -1 in the <see cref="MountainMassif.Project"/> sense.</summary>
        public int FlankSign { get; }

        /// <summary>Where on the crest it attaches, as normalised arc.</summary>
        /// <remarks>
        /// Always inside <see cref="MassifFlank.AttachmentInterval"/>, which is Stage 2B's
        /// <see cref="MountainMassif.CoreInterval"/>. Never merely inside
        /// <see cref="MacroAxis.CertifiedCore"/>: on two of the measured worlds that interval reaches
        /// to within 20 m and 0 m of an axis tip, where there is no flank to attach to.
        /// </remarks>
        public float AttachS { get; }

        /// <summary>
        /// The stretch of crest this spur commands, as normalised arc.
        /// </summary>
        /// <remarks>
        /// Bounded by the midpoints to its neighbours, so the strips of one flank partition the
        /// attachment interval exactly. This is what makes crossing impossible rather than unlikely:
        /// two spurs own disjoint arc intervals and neither leaves its own.
        /// </remarks>
        public Vector2 StripInterval { get; }

        /// <summary>
        /// Total arc the spur drifts from its attachment to its toe, normalised.
        /// </summary>
        /// <remarks>
        /// Stored because the carving field needs the ridge line as a function of transverse
        /// position - <c>ArcAt(t)</c> - and recovering it by projecting the polyline would be a
        /// second reading of the same curve.
        /// </remarks>
        public float DriftArc { get; }

        /// <summary>Where the ridge line sits on the arc, a transverse fraction down the flank.</summary>
        public float ArcAt(float t)
        {
            t = Mathf.Clamp01(t);
            return AttachS + DriftArc * (t * t * (3f - 2f * t));
        }

        /// <summary>Sampled polyline in world XZ, crest first, toe last.</summary>
        public Vector2[] Points { get; }

        /// <summary>Arc length at each point, metres.</summary>
        public float[] Cumulative { get; }

        public float LengthMeters { get; }

        /// <summary>How far the spur was aimed, metres.</summary>
        public float TargetReachMeters { get; }

        /// <summary>How far it actually got before something stopped it, metres.</summary>
        public float ReachMeters { get; }

        public SpurTermination Termination { get; }

        /// <summary>Whether a boundary stopped it short of its intended reach.</summary>
        public bool Truncated => Termination != SpurTermination.Toe;

        /// <summary>Where it leaves the crest.</summary>
        public Vector2 Origin => Points[0];

        /// <summary>Where it dies into the flank.</summary>
        public Vector2 Toe => Points[Points.Length - 1];

        public SpurCurve(
            int id,
            int massifIndex,
            int flankSign,
            float attachS,
            float driftArc,
            Vector2 stripInterval,
            Vector2[] points,
            float targetReachMeters,
            SpurTermination termination)
        {
            Id = id;
            MassifIndex = massifIndex;
            FlankSign = flankSign >= 0 ? 1 : -1;
            AttachS = attachS;
            DriftArc = driftArc;
            StripInterval = stripInterval;
            Points = points;
            TargetReachMeters = targetReachMeters;
            Termination = termination;

            Cumulative = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (points[i] - points[i - 1]).magnitude;
            }

            LengthMeters = points.Length > 0 ? Cumulative[points.Length - 1] : 0f;
            ReachMeters = (Toe - Origin).magnitude;
        }

        /// <summary>Whether any two non-adjacent segments of this spur cross.</summary>
        public bool SelfIntersects()
        {
            for (var i = 0; i + 1 < Points.Length; i++)
            {
                for (var j = i + 2; j + 1 < Points.Length; j++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(
                            Points[i], Points[i + 1], Points[j], Points[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Whether this spur crosses another.</summary>
        public bool Crosses(SpurCurve other)
        {
            for (var i = 0; i + 1 < Points.Length; i++)
            {
                for (var j = 0; j + 1 < other.Points.Length; j++)
                {
                    if (Town.Geometry.Geo2.SegmentIntersect(
                            Points[i], Points[i + 1], other.Points[j], other.Points[j + 1], out _))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <summary>
    /// The valley between two neighbouring spurs, as a structural interval rather than as geometry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A valley is the complement of the spurs, so it is not generated: it is what is left of a flank
    /// once the spurs have taken their strips. There are exactly <c>spurs + 1</c> of them per flank by
    /// construction, and every one has its head on the crest between two attachments - which is the
    /// relief principles' "valleys need headwaters" rule holding by arithmetic rather than by check.
    /// </para>
    /// <para>
    /// No floor polyline and no depth. Both need the cross-valley profile, which is a later slice's
    /// object; what a corridor is here is the interval it occupies and the two ridges that bound it.
    /// </para>
    /// </remarks>
    public sealed class ValleyCorridor
    {
        public int Id { get; }

        public int MassifIndex { get; }

        public int FlankSign { get; }

        /// <summary>The spur bounding it at lower arc, or -1 where the attachment interval does.</summary>
        public int LowerSpurId { get; }

        /// <summary>The spur bounding it at higher arc, or -1 where the attachment interval does.</summary>
        public int UpperSpurId { get; }

        /// <summary>The arc interval it occupies.</summary>
        public Vector2 SInterval { get; }

        /// <summary>Where its head sits on the crest, as normalised arc.</summary>
        public float HeadS => (SInterval.x + SInterval.y) * 0.5f;

        /// <summary>Arc width of the corridor, normalised.</summary>
        public float SWidth => SInterval.y - SInterval.x;

        /// <summary>Whether an attachment-interval end bounds it rather than a spur.</summary>
        public bool IsOuter => LowerSpurId < 0 || UpperSpurId < 0;

        // ---------- the trajectory ----------

        /// <summary>
        /// The valley floor in world XZ, from its head on the crest to its termination.
        /// </summary>
        /// <remarks>
        /// What makes this a valley rather than a strip. The first half is the mid-line between the
        /// corridor's two bounding ridges, evaluated at each transverse depth; the second is the
        /// trough continuing past the massif down Stage 1's own slope. The join at
        /// <see cref="ToeIndex"/> is an interior point of one curve, not a boundary between two.
        /// </remarks>
        public Vector2[] Floor { get; private set; } = System.Array.Empty<Vector2>();

        /// <summary>Arc length at each floor point, metres.</summary>
        public float[] Cumulative { get; private set; } = System.Array.Empty<float>();

        /// <summary>Total length of the trajectory, metres.</summary>
        public float LengthMeters { get; private set; }

        /// <summary>Index of the floor point where the massif part ends and the trough begins.</summary>
        public int ToeIndex { get; private set; }

        /// <summary>Arc length at the massif toe, metres.</summary>
        public float ToeArcMeters => Cumulative.Length > ToeIndex ? Cumulative[ToeIndex] : 0f;

        /// <summary>Half the corridor's width at the toe, metres. Carried into the continuation.</summary>
        public float HalfWidthMeters { get; private set; } = 1f;

        public ValleyTermination Termination { get; private set; } = ValleyTermination.Unresolved;

        /// <summary>The corridor this one joins, or -1 where it reaches an endpoint of its own.</summary>
        public int DownstreamId { get; private set; } = -1;

        /// <summary>Whether the trajectory reached a real endpoint.</summary>
        public bool TerminatesCleanly => Termination != ValleyTermination.Unresolved;

        public bool HasTrajectory => Floor.Length >= 2;

        /// <summary>The head on the crest.</summary>
        public Vector2 HeadPoint => Floor[0];

        /// <summary>The endpoint.</summary>
        public Vector2 EndPoint => Floor[Floor.Length - 1];

        internal void SetTrajectory(
            Vector2[] floor,
            int toeIndex,
            float halfWidthMeters,
            ValleyTermination termination,
            int downstreamId)
        {
            Floor = floor ?? System.Array.Empty<Vector2>();
            ToeIndex = Mathf.Clamp(toeIndex, 0, Mathf.Max(0, Floor.Length - 1));
            HalfWidthMeters = Mathf.Max(1f, halfWidthMeters);
            Termination = termination;
            DownstreamId = downstreamId;

            Cumulative = new float[Floor.Length];
            for (var i = 1; i < Floor.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (Floor[i] - Floor[i - 1]).magnitude;
            }

            LengthMeters = Floor.Length > 0 ? Cumulative[Floor.Length - 1] : 0f;
        }

        /// <summary>Distance from a world point to the floor polyline, metres.</summary>
        public float DistanceToFloor(Vector2 p)
        {
            if (Floor.Length == 0)
            {
                return float.PositiveInfinity;
            }

            if (Floor.Length == 1)
            {
                return (Floor[0] - p).magnitude;
            }

            var best = float.PositiveInfinity;
            for (var i = 0; i + 1 < Floor.Length; i++)
            {
                var q = Town.Geometry.Geo2.ClosestPointOnSegment(p, Floor[i], Floor[i + 1]);
                best = Mathf.Min(best, (q - p).magnitude);
            }

            return best;
        }

        /// <summary>
        /// A world point in the corridor's own frame: arc along the floor, distance across it.
        /// </summary>
        /// <remarks>
        /// The corridor's local coordinate, used only inside its own support. It is not a global
        /// distance-to-nearest field: every point resolved through it has already been placed in this
        /// corridor, and the two ends where a nearest-point projection would go radial are exactly the
        /// two ends where the carve is zero.
        /// </remarks>
        public bool Project(Vector2 p, out float arcMeters, out float acrossMeters)
        {
            arcMeters = 0f;
            acrossMeters = float.PositiveInfinity;

            if (Floor.Length < 2)
            {
                return false;
            }

            var bestSq = float.PositiveInfinity;
            var bestSegment = 0;
            var bestT = 0f;

            for (var i = 0; i + 1 < Floor.Length; i++)
            {
                var a = Floor[i];
                var b = Floor[i + 1];
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
                bestSegment = i;
                bestT = t;
            }

            arcMeters = Mathf.Lerp(Cumulative[bestSegment], Cumulative[bestSegment + 1], bestT);
            acrossMeters = Mathf.Sqrt(bestSq);
            return true;
        }

        /// <summary>Axis-aligned bounds of the trajectory, inflated by a pad.</summary>
        public Rect Bounds(float padMeters)
        {
            if (Floor.Length == 0)
            {
                return Rect.zero;
            }

            var minX = float.PositiveInfinity;
            var minZ = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxZ = float.NegativeInfinity;

            for (var i = 0; i < Floor.Length; i++)
            {
                minX = Mathf.Min(minX, Floor[i].x);
                minZ = Mathf.Min(minZ, Floor[i].y);
                maxX = Mathf.Max(maxX, Floor[i].x);
                maxZ = Mathf.Max(maxZ, Floor[i].y);
            }

            return Rect.MinMaxRect(
                minX - padMeters, minZ - padMeters, maxX + padMeters, maxZ + padMeters);
        }

        public ValleyCorridor(
            int id,
            int massifIndex,
            int flankSign,
            int lowerSpurId,
            int upperSpurId,
            Vector2 sInterval)
        {
            Id = id;
            MassifIndex = massifIndex;
            FlankSign = flankSign >= 0 ? 1 : -1;
            LowerSpurId = lowerSpurId;
            UpperSpurId = upperSpurId;
            SInterval = sInterval;
        }
    }
}
