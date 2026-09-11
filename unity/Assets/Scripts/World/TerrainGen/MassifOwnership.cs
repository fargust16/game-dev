using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// What a world point is, structurally: who owns it and whether its frame means anything.
    /// </summary>
    /// <remarks>
    /// The first two are unowned. The last three are all owned by a massif - the partition stays
    /// total over land - but only <see cref="Massif"/> carries a usable structural coordinate.
    /// </remarks>
    public enum MassifOwnerKind
    {
        /// <summary>Outside the coastline. Stage 3 owns no ocean and builds nothing here.</summary>
        Ocean = 0,

        /// <summary>Land in a world that carries no massif. A legitimate answer, not a failure.</summary>
        Unclaimed = 1,

        /// <summary>
        /// Land inside the owning massif's structural domain: the frame is single-valued here.
        /// </summary>
        Massif = 2,

        /// <summary>
        /// Owned, but further from the axis than its reach, where the nearest point is not unique.
        /// </summary>
        /// <remarks>
        /// Beyond Federer's reach a curve has a medial set, and across it the nearest point jumps
        /// from one branch to another. Measured on the six worlds that carry a massif, the arc
        /// coordinate jumps by up to 1592 m between samples ten metres apart out here, and the flank
        /// sign flips with nothing between it and the axis. No coordinate is offered.
        /// </remarks>
        BeyondReach = 3,

        /// <summary>
        /// Owned and within reach, but projecting onto an end of the axis rather than its interior.
        /// </summary>
        /// <remarks>
        /// An open curve has no flank past its ends. Every point out there projects onto the same
        /// endpoint, so the arc coordinate saturates and the sign says only which side of the
        /// terminal tangent the point is on - a half-plane, not a flank. This is not a corner case:
        /// it is 7.9 % to 47.4 % of the owned land on the worlds measured, reaching 1269 m past the
        /// tip. No coordinate is offered.
        /// </remarks>
        BeyondAxisEnd = 4,
    }

    /// <summary>
    /// One massif's flank: the ground on one side of its axis, and where structure may attach to it.
    /// </summary>
    /// <remarks>
    /// The axis is the divide between the two, so a massif has exactly two flanks and every point of
    /// its structural domain is on one of them. The sign is the sign of
    /// <see cref="MountainMassif.Project"/>'s offset, which is Stage 2B's convention and is not
    /// redefined here.
    /// </remarks>
    public readonly struct MassifFlank
    {
        private readonly MountainMassif massif;

        public int MassifIndex { get; }

        /// <summary>+1 or -1, matching the sign of the projected offset.</summary>
        public int Sign { get; }

        /// <summary>Whether this is the flank Stage 2B built steep.</summary>
        public bool IsSteep { get; }

        /// <summary>
        /// The stretch of axis structure may attach to, as normalised arc.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is Stage 2B's <see cref="MountainMassif.CoreInterval"/>, forwarded.</b> Not a copy
        /// and not a second calculation - the property reads through to the massif, so there is one
        /// such interval in the world and it is the one Stage 2B laid its own profile out against.
        /// </para>
        /// <para>
        /// It is deliberately <em>not</em> <see cref="MacroAxis.CertifiedCore"/>, and the two answer
        /// different questions:
        /// </para>
        /// <list type="bullet">
        /// <item><b>CertifiedCore</b> - where the massif has enough coastal clearance for its whole
        /// cross-section. Stage 2A's guarantee about the sea.</item>
        /// <item><b>CoreInterval</b> - where Stage 2B considers the massif usable along its axis,
        /// which is that certified core held clear of both ends so the end ramps have room.</item>
        /// </list>
        /// <para>
        /// The difference is not academic. Measured on real seeds, <c>CertifiedCore</c> reaches
        /// <b>20 m</b> from the axis tip on seed 7 and <b>0 m</b> on seed 20261231 - it starts exactly
        /// at the endpoint. Attaching there would put structure in the region
        /// <see cref="MassifOwnerKind.BeyondAxisEnd"/> exists to fence off. <c>CoreInterval</c> is the
        /// interval that does not do that, so it is the one this flank offers.
        /// </para>
        /// </remarks>
        public Vector2 AttachmentInterval => massif.CoreInterval;

        public MassifFlank(MountainMassif massif, int massifIndex, int sign, bool isSteep)
        {
            this.massif = massif;
            MassifIndex = massifIndex;
            Sign = sign >= 0 ? 1 : -1;
            IsSteep = isSteep;
        }

        /// <summary>Whether structure may attach at a normalised arc position on this flank.</summary>
        /// <remarks>
        /// The single predicate an attachment has to pass. It is inclusive of both ends of
        /// <see cref="AttachmentInterval"/>, which are themselves already held clear of the axis ends.
        /// </remarks>
        public bool CanAttachAt(float s)
        {
            var interval = AttachmentInterval;
            return s >= interval.x && s <= interval.y;
        }
    }

    /// <summary>
    /// A structural coordinate: which flank, how far along, how far across.
    /// </summary>
    /// <remarks>
    /// Only ever produced by <see cref="MassifOwner.TryGetFrame"/>, and only where that coordinate is
    /// single-valued. Everywhere else there is no frame at all rather than a plausible-looking one,
    /// which is why the type exists separately from <see cref="MassifOwner"/>.
    /// </remarks>
    public readonly struct MassifFrame
    {
        public int MassifIndex { get; }

        /// <summary>+1 or -1. See <see cref="MassifFlank.Sign"/>.</summary>
        public int FlankSign { get; }

        /// <summary>Normalised arc along the owning massif's projection curve, strictly interior.</summary>
        public float S { get; }

        /// <summary>Signed transverse offset in the owning massif's frame, metres.</summary>
        public float OffsetMeters { get; }

        public MassifFrame(int massifIndex, int flankSign, float s, float offsetMeters)
        {
            MassifIndex = massifIndex;
            FlankSign = flankSign;
            S = s;
            OffsetMeters = offsetMeters;
        }
    }

    /// <summary>
    /// A world point resolved against the partition: who owns it, and whether it has a frame.
    /// </summary>
    /// <remarks>
    /// Ownership and structural validity are different facts and this type keeps them apart. A point
    /// can be owned - it has a nearest massif, it is on one side of a medial divide - while having no
    /// meaningful flank coordinate at all. Reading the coordinate goes through
    /// <see cref="TryGetFrame"/>, so code that wants one has to acknowledge it might not exist.
    /// </remarks>
    public readonly struct MassifOwner
    {
        private readonly float s;
        private readonly float offsetMeters;
        private readonly int flankSign;

        public MassifOwnerKind Kind { get; }

        /// <summary>Index into the massif list, or -1 when nothing owns the point.</summary>
        public int MassifIndex { get; }

        /// <summary>Distance to the owning axis, metres. This is the quantity ownership compares.</summary>
        public float AxisDistanceMeters { get; }

        /// <summary>
        /// How much nearer the owner is than the next massif, metres. Infinite when it is the only one.
        /// </summary>
        /// <remarks>
        /// Zero on the medial divide, and the reason the divide needs no separate membership test:
        /// it is the level set of this number.
        /// </remarks>
        public float MarginMeters { get; }

        /// <summary>Whether a massif owns this point, whatever the state of its frame.</summary>
        public bool IsOwned => MassifIndex >= 0;

        /// <summary>Whether the point lies in the owning massif's structural domain.</summary>
        public bool IsStructural => Kind == MassifOwnerKind.Massif;

        private MassifOwner(
            MassifOwnerKind kind,
            int massifIndex,
            int flankSign,
            float s,
            float offsetMeters,
            float axisDistanceMeters,
            float marginMeters)
        {
            Kind = kind;
            MassifIndex = massifIndex;
            this.flankSign = flankSign;
            this.s = s;
            this.offsetMeters = offsetMeters;
            AxisDistanceMeters = axisDistanceMeters;
            MarginMeters = marginMeters;
        }

        /// <summary>The structural coordinate, when there is one.</summary>
        /// <remarks>
        /// False outside <see cref="MassifOwnerKind.Massif"/>, and the out parameter is left at its
        /// default rather than filled with whatever the projection happened to return. There is no
        /// overload that skips the test.
        /// </remarks>
        public bool TryGetFrame(out MassifFrame frame)
        {
            if (Kind != MassifOwnerKind.Massif)
            {
                frame = default;
                return false;
            }

            frame = new MassifFrame(MassifIndex, flankSign, s, offsetMeters);
            return true;
        }

        public static MassifOwner Unowned(MassifOwnerKind kind) =>
            new MassifOwner(kind, -1, 0, 0f, 0f, float.PositiveInfinity, float.PositiveInfinity);

        /// <summary>Owned, but with no usable frame. Carries no coordinate to be misread.</summary>
        public static MassifOwner OutsideDomain(
            MassifOwnerKind kind, int massifIndex, float axisDistanceMeters, float marginMeters) =>
            new MassifOwner(kind, massifIndex, 0, 0f, 0f, axisDistanceMeters, marginMeters);

        public static MassifOwner Structural(
            int massifIndex,
            int flankSign,
            float s,
            float offsetMeters,
            float axisDistanceMeters,
            float marginMeters) =>
            new MassifOwner(
                MassifOwnerKind.Massif, massifIndex, flankSign, s, offsetMeters,
                axisDistanceMeters, marginMeters);
    }

    /// <summary>
    /// Stage 3A's partition of the land between massifs, as a geometric function of world position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A point on land belongs to the massif whose <see cref="MacroAxis"/> is nearest. That is the
    /// whole ownership rule. With at most a handful of axes it is exact, cheap and needs no raster -
    /// which matters, because a raster ownership map would put a lattice between Stage 3 and its own
    /// geometry and the design forbids exactly that.
    /// </para>
    /// <para>
    /// <b>Ownership is total over land; the structural frame is not.</b> These are separate claims,
    /// and conflating them is the defect this contract exists to prevent. Every land point has an
    /// owner, because the medial divide between two massifs has to be the boundary of something. Only
    /// the part of that territory inside the owning axis's <see cref="MacroAxis.ReachMeters"/>, and
    /// alongside the axis rather than past its ends, carries a coordinate that means anything.
    /// </para>
    /// <para>
    /// The reach is not a threshold chosen here. It is Federer's reach, which
    /// <see cref="MacroAxis"/> already computes and Stage 2A already certifies every axis against,
    /// and it is exactly the radius within which the nearest point on a curve is unique. Inside it
    /// the arc coordinate and the flank sign are single-valued; outside it they are not, and
    /// <see cref="MountainMassif.Project"/>'s coarse-then-exact search is outside the precondition it
    /// documents for itself. The reach also contains the body Stage 2B built - the widest flank
    /// reaches <c>(2 - Asymmetry)</c> times the effective half-width, which stays under it on every
    /// world measured - so nothing Stage 2B raised falls outside the domain.
    /// </para>
    /// <para>
    /// The five cases, all decided rather than left implicit:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Ocean.</b> Outside the coastline, nothing is owned. This is what makes a divide's
    /// termination at the coast a property of the partition rather than a clip applied to it.</item>
    /// <item><b>Unclaimed.</b> Land in a world with no massif. Four of the ten worlds inspected are
    /// like this and they are not failures.</item>
    /// <item><b>Massif.</b> Owned, within reach, projecting onto the interior of the axis. The only
    /// state that yields a frame.</item>
    /// <item><b>BeyondReach.</b> Owned, but the projection is not unique. Tested first, because a
    /// non-unique projection makes the endpoint question meaningless as well.</item>
    /// <item><b>BeyondAxisEnd.</b> Owned and within reach, but past an end of the axis.</item>
    /// </list>
    /// <para>
    /// Equidistant points go to the lower massif index, with agreement judged against
    /// <see cref="RidgeSettings.OwnershipTieEpsilonMeters"/> so the answer does not depend on float
    /// rounding. A point inside a massif's <see cref="MountainMassif.Support"/> rectangle but outside
    /// its body is owned like any other: the support rectangle is Stage 2B's evaluation bound and has
    /// no structural meaning.
    /// </para>
    /// </remarks>
    public sealed class MassifOwnership
    {
        private readonly IReadOnlyList<MountainMassif> massifs;
        private readonly CoastQuery coast;
        private readonly float tieEpsilon;
        private readonly MassifFlank[] flanks;

        /// <summary>Both flanks of every massif, massif-major, negative sign first.</summary>
        public IReadOnlyList<MassifFlank> Flanks => flanks;

        public int MassifCount => massifs.Count;

        public MassifOwnership(
            IReadOnlyList<MountainMassif> massifs, CoastQuery coast, RidgeSettings settings)
        {
            settings.Clamp();
            this.massifs = massifs ?? System.Array.Empty<MountainMassif>();
            this.coast = coast;
            tieEpsilon = settings.OwnershipTieEpsilonMeters;

            flanks = new MassifFlank[this.massifs.Count * 2];
            for (var i = 0; i < this.massifs.Count; i++)
            {
                var massif = this.massifs[i];
                var steep = massif.SteepSide;
                flanks[i * 2 + 0] = new MassifFlank(massif, i, -1, steep < 0);
                flanks[i * 2 + 1] = new MassifFlank(massif, i, +1, steep >= 0);
            }
        }

        /// <summary>The flank a signed offset falls on.</summary>
        /// <remarks>
        /// Exactly zero is the axis itself, which belongs to neither flank; it resolves to +1 so the
        /// function is total and deterministic. Nothing downstream may read that as the axis being on
        /// a flank - the axis is the divide, and <see cref="MassifFrame.OffsetMeters"/> says so.
        /// </remarks>
        public static int FlankSignOf(float offsetMeters) => offsetMeters < 0f ? -1 : 1;

        /// <summary>
        /// Radius within which a massif's structural frame is single-valued, metres.
        /// </summary>
        /// <remarks>
        /// The owning axis's reach, forwarded. Exposed so a consumer can size its own structure
        /// against the domain rather than discover the edge by being refused a frame at it. Positive
        /// infinity on an axis with no measurable bend.
        /// </remarks>
        public float DomainRadiusMeters(int massifIndex) => massifs[massifIndex].Axis.ReachMeters;

        /// <summary>Resolves a world point against the partition.</summary>
        public MassifOwner At(float worldX, float worldZ) =>
            At(worldX, worldZ, coast == null || coast.IsInside(worldX, worldZ));

        /// <summary>
        /// Resolves a point whose land test the caller has already made.
        /// </summary>
        /// <remarks>
        /// The bulk path. <see cref="CoastQuery.IsInside"/> walks every coast segment to build one
        /// row of crossings, so asking it per sample costs more than the partition itself; a caller
        /// sweeping a whole row can compute the crossings once and pass the answer in. Same rule,
        /// same result - <see cref="At(float, float)"/> is this method with the test done for you.
        /// </remarks>
        public MassifOwner At(float worldX, float worldZ, bool isLand)
        {
            if (!isLand)
            {
                return MassifOwner.Unowned(MassifOwnerKind.Ocean);
            }

            if (massifs.Count == 0)
            {
                return MassifOwner.Unowned(MassifOwnerKind.Unclaimed);
            }

            var best = -1;
            var bestDistance = float.PositiveInfinity;
            var runnerUp = float.PositiveInfinity;

            for (var i = 0; i < massifs.Count; i++)
            {
                var distance = massifs[i].Axis.DistanceTo(worldX, worldZ);

                // Strictly nearer by more than the epsilon, so an equidistant point keeps the owner it
                // already has - which, scanning ascending, is the lowest index that ties.
                if (distance < bestDistance - tieEpsilon)
                {
                    runnerUp = bestDistance;
                    bestDistance = distance;
                    best = i;
                }
                else if (distance < runnerUp)
                {
                    runnerUp = distance;
                }
            }

            if (best < 0)
            {
                // Every axis reported an unmeasurable distance, which takes a curve with fewer than
                // two points. Stage 2A's minimum axis length rules it out; saying so beats indexing
                // with -1 if that ever stops being true.
                return MassifOwner.Unowned(MassifOwnerKind.Unclaimed);
            }

            var margin = runnerUp - bestDistance;

            // Reach first. Past it the nearest point is not unique, which makes the endpoint question
            // meaningless too - there is no single arc position to ask about.
            if (bestDistance >= massifs[best].Axis.ReachMeters)
            {
                return MassifOwner.OutsideDomain(
                    MassifOwnerKind.BeyondReach, best, bestDistance, margin);
            }

            massifs[best].Project(worldX, worldZ, out var s, out var offset);

            // Project clamps to the ends, so a saturated arc coordinate is how a point past the axis
            // announces itself. It is not a coordinate at 0 or 1; it is the absence of one.
            if (s <= 0f || s >= 1f)
            {
                return MassifOwner.OutsideDomain(
                    MassifOwnerKind.BeyondAxisEnd, best, bestDistance, margin);
            }

            return MassifOwner.Structural(
                best, FlankSignOf(offset), s, offset, bestDistance, margin);
        }

        /// <summary>The owning massif index, or -1 where nothing is owned.</summary>
        public int OwnerIndexAt(float worldX, float worldZ) => At(worldX, worldZ).MassifIndex;

        /// <summary>Distance from a point to a given massif's axis, metres.</summary>
        /// <remarks>
        /// The same measurement <see cref="At"/> compares, exposed so the divide builder and the
        /// partition cannot drift apart: the divide is where two of these are equal, and if it were
        /// built on a different distance it would not be the boundary of this partition.
        /// </remarks>
        public float AxisDistance(int massifIndex, Vector2 p) =>
            massifs[massifIndex].Axis.DistanceTo(p.x, p.y);

        /// <summary>
        /// Largest disagreement between the ownership metric and the massif frame's own offset, metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Ownership compares distance to <see cref="MacroAxis.Points"/>, the 20 m polyline. The frame
        /// comes from <see cref="MountainMassif.Project"/>, which measures against the Catmull-Rom
        /// curve Stage 2B smoothed that polyline into. They are two curves, so the two distances are
        /// not identical, and pretending otherwise would be the "different interpretation of the
        /// massif frame" the slice exists to prevent.
        /// </para>
        /// <para>
        /// Sampled on rings around the axis midpoint, which is a spot check rather than a sweep of the
        /// domain - <c>TerrainGenStage3ATests</c> walks the whole land grid and is the measurement
        /// that bounds this. Both put the disagreement at a decimetre.
        /// </para>
        /// </remarks>
        public float MeasureFrameAgreement(int ringsPerMassif = 8, int samplesPerRing = 180)
        {
            var worst = 0f;
            for (var i = 0; i < massifs.Count; i++)
            {
                var massif = massifs[i];
                var reach = Mathf.Max(50f, massif.HalfWidthMeters * 2f);

                for (var r = 1; r <= ringsPerMassif; r++)
                {
                    var radius = reach * (r / (float)ringsPerMassif);
                    for (var k = 0; k < samplesPerRing; k++)
                    {
                        var angle = Mathf.PI * 2f * (k / (float)samplesPerRing);
                        var centre = massif.PointAt(0.5f);
                        var p = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                        massif.Project(p.x, p.y, out _, out var offset);
                        var polyline = massif.Axis.DistanceTo(p.x, p.y);
                        worst = Mathf.Max(worst, Mathf.Abs(Mathf.Abs(offset) - polyline));
                    }
                }
            }

            return worst;
        }
    }
}
