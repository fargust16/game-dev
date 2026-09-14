using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Why a secondary spur stopped where it did.</summary>
    /// <remarks>
    /// Deliberately not <see cref="SpurTermination"/>. A primary spur ends against the world - the
    /// coast, a neighbouring massif, the edge of the structural frame. A secondary ends against its
    /// own parent's structure, and those are different vocabularies; sharing one enum would invite the
    /// reading that a secondary can terminate for a world reason, which is exactly what its domain
    /// rule forbids.
    /// </remarks>
    public enum SecondaryTermination
    {
        /// <summary>It reached the reach the tier ratio allowed. The ordinary outcome.</summary>
        Toe = 0,

        /// <summary>The primary valley it descends towards stopped it.</summary>
        Valley = 1,

        /// <summary>The edge of the parent's strip stopped it.</summary>
        Strip = 2,
    }

    /// <summary>
    /// A depth-2 ridge: a rib leaving one primary spur and dying into the flank beside it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its whole coordinate system is the parent's. <see cref="AttachT"/> is the <em>parent's</em>
    /// transverse depth, which is the parameter the parent's own curve is defined on, so an attachment
    /// inside <c>(0,1)</c> is inside the parent by construction and the terminal apron is out of reach
    /// without a world-space rule to enforce it.
    /// </para>
    /// <para>
    /// It is not a <see cref="SpurCurve"/> and must never be put in a list of them.
    /// <see cref="SpurCurve"/> is crest-anchored - <c>AttachS</c> is a crest arc, <c>StripInterval</c>
    /// is a stretch of crest, and <c>ArcAt</c> is the very function the frozen valley-floor
    /// construction reads. A secondary has none of those meanings.
    /// </para>
    /// </remarks>
    public sealed class SecondarySpur
    {
        /// <summary>Identity in the secondary collection only. Never comparable to a primary id.</summary>
        public int Id { get; }

        /// <summary>The frozen primary <see cref="SpurCurve.Id"/> this rib leaves.</summary>
        public int ParentSpurId { get; }

        public int MassifIndex { get; }

        /// <summary>The parent's flank, copied. A rib never changes flank.</summary>
        public int FlankSign { get; }

        /// <summary>Which half of the parent's strip it descends into: +1 up-arc, -1 down-arc.</summary>
        public int Side { get; }

        /// <summary>Where it leaves the parent, in the parent's own transverse depth.</summary>
        public float AttachT { get; }

        /// <summary>The parent's curve evaluated at <see cref="AttachT"/>, world XZ.</summary>
        public Vector2 AttachPoint { get; }

        /// <summary>The stretch of parent depth this rib commands, as <c>[t0,t1]</c>.</summary>
        public Vector2 ParentInterval { get; }

        /// <summary>Attachment first, toe last, world XZ.</summary>
        public Vector2[] Points { get; }

        /// <summary>Arc length at each point, metres.</summary>
        public float[] Cumulative { get; }

        public float LengthMeters { get; }

        /// <summary>The reach the tier ratio and the room together allowed, metres.</summary>
        public float TargetReachMeters { get; }

        /// <summary>
        /// The reach the tier ratio alone allowed, before the room was consulted, metres.
        /// </summary>
        /// <remarks>
        /// Kept beside <see cref="TargetReachMeters"/> so a reader can tell a rib the tier stopped from
        /// one the ground stopped. The strict 2.5 applies here; what a rib actually gets is this or the
        /// room, whichever is less.
        /// </remarks>
        public float TierTargetReachMeters { get; }

        /// <summary>The reach it achieved, metres.</summary>
        public float ReachMeters { get; }

        /// <summary>
        /// How far the rib bows sideways over its length, metres, signed either way by the draw.
        /// </summary>
        /// <remarks>
        /// The depth-2 reading of Stage 3B's own drift: a bounded lateral swing drawn from the shape
        /// stream, at <c>SpurDriftPerGap / TierRatio</c> of the rib's own length - the parent's
        /// coefficient one tier down. Without it every rib is a straight stick and a flank reads as a
        /// comb.
        /// </remarks>
        public float LateralDriftMeters { get; }

        /// <summary>Closest approach to a primary ridge that is not its own parent, metres.</summary>
        public float ClearanceMeters { get; }

        /// <summary>How far the rib departs from its own chord, metres.</summary>
        public float ChordDeviationMeters { get; }

        /// <summary>
        /// The structural tier this rib occupies, metres of relief.
        /// </summary>
        /// <remarks>
        /// Metadata describing the tier, not a terrain height. Nothing in Stage 3C reads it and Stage
        /// 3D writes no height field; it exists so the tier-separation rule can be checked against the
        /// parent's budget deterministically instead of against <c>H3</c>, which carries no ridge.
        /// </remarks>
        public float ReliefBudgetMeters { get; }

        /// <summary>The parent's own budget, carried so the ratio can be read off one object.</summary>
        public float ParentReliefBudgetMeters { get; }

        public SecondaryTermination Termination { get; }

        public Vector2 Toe => Points[Points.Length - 1];

        public SecondarySpur(
            int id,
            int parentSpurId,
            int massifIndex,
            int flankSign,
            int side,
            float attachT,
            Vector2 attachPoint,
            Vector2 parentInterval,
            Vector2[] points,
            float targetReachMeters,
            float tierTargetReachMeters,
            float lateralDriftMeters,
            float clearanceMeters,
            float reliefBudgetMeters,
            float parentReliefBudgetMeters,
            SecondaryTermination termination)
        {
            Id = id;
            ParentSpurId = parentSpurId;
            MassifIndex = massifIndex;
            FlankSign = flankSign >= 0 ? 1 : -1;
            Side = side >= 0 ? 1 : -1;
            AttachT = attachT;
            AttachPoint = attachPoint;
            ParentInterval = parentInterval;
            Points = points;
            TargetReachMeters = targetReachMeters;
            TierTargetReachMeters = tierTargetReachMeters;
            LateralDriftMeters = lateralDriftMeters;
            ClearanceMeters = clearanceMeters;
            ReliefBudgetMeters = reliefBudgetMeters;
            ParentReliefBudgetMeters = parentReliefBudgetMeters;
            Termination = termination;

            Cumulative = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                Cumulative[i] = Cumulative[i - 1] + (points[i] - points[i - 1]).magnitude;
            }

            LengthMeters = points.Length > 0 ? Cumulative[points.Length - 1] : 0f;
            ReachMeters = points.Length > 0 ? (Toe - points[0]).magnitude : 0f;

            var deviation = 0f;
            for (var i = 1; i + 1 < points.Length; i++)
            {
                deviation = Mathf.Max(
                    deviation, DistanceToSegment(points[0], points[points.Length - 1], points[i]));
            }

            ChordDeviationMeters = deviation;
        }

        private static float DistanceToSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            var ab = b - a;
            var length = ab.sqrMagnitude;
            if (length < 1e-12f)
            {
                return (p - a).magnitude;
            }

            var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / length);
            return (p - (a + ab * u)).magnitude;
        }

        /// <summary>Whether any two non-adjacent segments of this rib cross.</summary>
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
    }

    /// <summary>
    /// A cell of the partition one level down: a stretch of one side of a parent's strip.
    /// </summary>
    /// <remarks>
    /// <b>This is not a corridor and must never become one.</b> A <see cref="ValleyCorridor"/> is the
    /// unit the primary valley topology is built on, and that set is frozen at depth 1; a depth-2 cell
    /// entering it would change the ranks, the chains, the divide-end ordering and therefore the
    /// merges Slice B2 settled.
    /// </remarks>
    public sealed class SecondaryStrip
    {
        public int ParentSpurId { get; }

        public int Side { get; }

        /// <summary>The parent-depth interval this cell covers, <c>[t0,t1]</c>.</summary>
        public Vector2 ParentInterval { get; }

        /// <summary>The rib bounding it at the shallow end, or -1 at the parent's head.</summary>
        public int LowerSecondaryId { get; }

        /// <summary>The rib bounding it at the deep end, or -1 at the parent's toe.</summary>
        public int UpperSecondaryId { get; }

        public float Span => ParentInterval.y - ParentInterval.x;

        public SecondaryStrip(
            int parentSpurId,
            int side,
            Vector2 parentInterval,
            int lowerSecondaryId,
            int upperSecondaryId)
        {
            ParentSpurId = parentSpurId;
            Side = side >= 0 ? 1 : -1;
            ParentInterval = parentInterval;
            LowerSecondaryId = lowerSecondaryId;
            UpperSecondaryId = upperSecondaryId;
        }
    }

    /// <summary>
    /// Stage 3D: the depth-2 ridge curves and their strips, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skeleton only. It carves nothing, paints nothing and touches no height field. It holds the
    /// frozen primary skeleton by reference so a reader can resolve a parent, and never writes through
    /// it.
    /// </para>
    /// <para>
    /// Nothing in Stage 3C accepts this type. <see cref="ValleyNetwork.Build"/> takes a
    /// <see cref="RidgeSkeleton"/>, <see cref="ValleyTopology.Build"/> takes a
    /// <see cref="ValleyNetwork"/>, and <see cref="ValleyTrajectoryNetwork.Build"/> takes a
    /// <see cref="ValleyTopology"/>. There is no way in, which is the point.
    /// </para>
    /// </remarks>
    public sealed class SecondaryRidgeSkeleton
    {
        public int Seed { get; }

        public RidgeSettings Settings { get; }

        /// <summary>The frozen depth-1 skeleton. Read-only to everything here.</summary>
        public RidgeSkeleton Primary { get; }

        public IReadOnlyList<SecondarySpur> Spurs { get; }

        public IReadOnlyList<SecondaryStrip> Strips { get; }

        public SecondaryRidgeStats Stats { get; }

        private readonly int[][] children;

        public SecondaryRidgeSkeleton(
            int seed,
            RidgeSettings settings,
            RidgeSkeleton primary,
            IReadOnlyList<SecondarySpur> spurs,
            IReadOnlyList<SecondaryStrip> strips,
            int[][] children,
            SecondaryRidgeStats stats)
        {
            Seed = seed;
            Settings = settings;
            Primary = primary;
            Spurs = spurs;
            Strips = strips;
            Stats = stats;
            this.children = children;
        }

        /// <summary>The ribs of one primary spur, in ascending attachment depth.</summary>
        public IReadOnlyList<int> ChildrenOf(int primarySpurId) =>
            primarySpurId >= 0 && primarySpurId < children.Length
                ? children[primarySpurId]
                : System.Array.Empty<int>();

        /// <summary>The strips of one primary spur on one side, shallow end first.</summary>
        public List<SecondaryStrip> StripsOf(int primarySpurId, int side)
        {
            var found = new List<SecondaryStrip>();
            for (var i = 0; i < Strips.Count; i++)
            {
                if (Strips[i].ParentSpurId == primarySpurId && Strips[i].Side == side)
                {
                    found.Add(Strips[i]);
                }
            }

            return found;
        }
    }

    public sealed class SecondaryRidgeStats
    {
        public int Seed;
        public int PrimarySpurCount;
        public int SecondarySpurCount;
        public int SecondaryStripCount;

        /// <summary>Parents that carry no rib at all, usually for want of room.</summary>
        public int ParentsWithoutChildren;

        /// <summary>Candidates dropped because the reach the room allowed was under one sample.</summary>
        public int RejectedForRoom;

        /// <summary>Candidates dropped by a crossing check. Any of these is worth reading.</summary>
        public int RejectedForCrossingPrimary;
        public int RejectedForCrossingSecondary;
        public int RejectedForCrossingValley;
        public int RejectedForLeavingStrip;
        public int RejectedForLeavingDomain;
        public int RejectedForSelfIntersection;

        /// <summary>Candidates dropped for coming within one sample of an unrelated primary ridge.</summary>
        public int RejectedForClearance;

        public float MinReachMeters = float.PositiveInfinity;
        public float MaxReachMeters;
        public float MinReliefBudgetMeters = float.PositiveInfinity;
        public float MaxReliefBudgetMeters;

        public float MinChordDeviationMeters = float.PositiveInfinity;
        public float MaxChordDeviationMeters;

        /// <summary>Closest any rib comes to a primary ridge that is not its parent, metres.</summary>
        public float MinClearanceMeters = float.PositiveInfinity;

        /// <summary>Indexed by <see cref="SecondaryTermination"/>.</summary>
        public readonly int[] Terminations = new int[3];

        public long BuildMs;

        public int RejectedCount =>
            RejectedForRoom + RejectedForCrossingPrimary + RejectedForCrossingSecondary
            + RejectedForCrossingValley + RejectedForLeavingStrip + RejectedForLeavingDomain
            + RejectedForSelfIntersection + RejectedForClearance;

        public string ToLine() =>
            $"seed {Seed}: primary={PrimarySpurCount} secondary={SecondarySpurCount}"
            + $" strips={SecondaryStripCount} barrenParents={ParentsWithoutChildren}"
            + $" rejected={RejectedCount} build={BuildMs}ms";

        public string RejectionLine() =>
            $"rejections: room={RejectedForRoom} primary={RejectedForCrossingPrimary}"
            + $" secondary={RejectedForCrossingSecondary} valley={RejectedForCrossingValley}"
            + $" strip={RejectedForLeavingStrip} domain={RejectedForLeavingDomain}"
            + $" self={RejectedForSelfIntersection} clearance={RejectedForClearance}";

        public string ShapeLine() =>
            $"shape: reach=[{(MinReachMeters == float.PositiveInfinity ? -1f : MinReachMeters):F0},"
            + $"{MaxReachMeters:F0}]m"
            + $" budget=[{(MinReliefBudgetMeters == float.PositiveInfinity ? -1f : MinReliefBudgetMeters):F1},"
            + $"{MaxReliefBudgetMeters:F1}]m"
            + $" bend=[{(MinChordDeviationMeters == float.PositiveInfinity ? -1f : MinChordDeviationMeters):F2},"
            + $"{MaxChordDeviationMeters:F2}]m"
            + $" minClearance={(MinClearanceMeters == float.PositiveInfinity ? -1f : MinClearanceMeters):F1}m"
            + $" terminations: toe={Terminations[0]} valley={Terminations[1]} strip={Terminations[2]}";
    }
}
