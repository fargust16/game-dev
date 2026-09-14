using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>Why a cirque candidate did not become a declared basin.</summary>
    public enum BasinRejection
    {
        /// <summary>It did.</summary>
        None = 0,

        /// <summary>The ring degenerated: too few points, no area, or it crosses itself.</summary>
        Geometry = 1,

        /// <summary>A boundary point left its own massif's structural ground.</summary>
        Domain = 2,

        /// <summary>Not exactly one outlet run, or the one it has carries no points.</summary>
        OutletRun = 3,

        /// <summary>The perimeter is not ridge structure over a strict majority of its length.</summary>
        RidgeMajority = 4,

        /// <summary>The corridor the enclosure frames has no usable trajectory.</summary>
        CorridorMissing = 5,

        /// <summary>That corridor does not actually leave through the outlet gap.</summary>
        OutletNotOnCorridor = 6,

        /// <summary>That corridor does not continue below the outlet.</summary>
        CorridorNotDownhill = 7,

        /// <summary>A corridor the basin does not enclose runs through its interior.</summary>
        /// <remarks>
        /// Entering, not touching. Both flanks share the crest, so every corridor on the far flank has
        /// its head on the same crest run this enclosure is bounded by, and crosses it there on its way
        /// down the other side. That meeting is inevitable and harmless; running through the enclosed
        /// ground is not.
        /// </remarks>
        ForeignValleyInside = 8,
    }

    /// <summary>
    /// A cirque enclosure that passes structural validation. Not a claim about a hollow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this type deliberately does not carry.</b> There is no <c>FloorElevationMeters</c>, no
    /// depth and no tier. The owner decision recorded in the preflight is that the design's
    /// <em>"floor lies below the outlet by a bounded depth"</em> is not a Stage 3E invariant: measured
    /// over all 28 candidates, the Stage 2 structural surface contains no hollow inside any enclosure,
    /// and the apparent ones were an artefact of which run owned a shared boundary endpoint. A floor
    /// field here would have to be filled with a number that means nothing, so there is none. The
    /// authoritative basin floor arrives with the stage that owns a carved field.
    /// </para>
    /// <para>
    /// There is also no inlet list and no kind. A cirque's hollow sits at the head of its own corridor,
    /// so nothing drains into it and the list would always be empty; and with the intermontane path
    /// blocked there is one kind, so an enum of one member would be a decision taken before it was
    /// needed.
    /// </para>
    /// <para>
    /// <b>One corridor, two roles.</b> <see cref="CorridorId"/> is both the corridor the enclosure
    /// frames and the corridor its outlet feeds, because for a cirque they are the same corridor by
    /// construction: the gap runs between the two toes of the corridor's own bounding spurs, which is
    /// that corridor's mouth. They are not split into two fields because there is one relationship.
    /// An intermontane basin would need them apart - it encloses a stretch of medial ground and spills
    /// into a corridor it does not enclose - and that is one more thing blocked behind B2.
    /// </para>
    /// </remarks>
    public sealed class StructuralBasin
    {
        /// <summary>Enumeration order over accepted candidates. Stable for a given skeleton.</summary>
        public int Id { get; }

        /// <summary>The <see cref="CirqueCandidate.Id"/> this was validated from.</summary>
        public int CandidateId { get; }

        /// <summary>
        /// The primary corridor this encloses, which is also the corridor its outlet feeds.
        /// </summary>
        public int CorridorId { get; }

        public int MassifIndex { get; }

        public int FlankSign { get; }

        /// <summary>The primary spurs on the perimeter, ascending. Never a secondary.</summary>
        public int[] EnclosingSpurIds { get; }

        /// <summary>The closed ring, as E1 built it.</summary>
        public Vector2[] Boundary { get; }

        /// <summary>The four runs and their provenance, as E1 built them.</summary>
        public BoundarySegment[] Segments { get; }

        /// <summary>The lowest point of the outlet gap, in world XZ.</summary>
        public Vector2 OutletPointXZ { get; }

        /// <summary>
        /// The structural elevation there, metres.
        /// </summary>
        /// <remarks>
        /// Read over the <see cref="BoundarySource.Outlet"/> run and over nothing else. The crest and
        /// spur runs are the enclosing ridge, so they are not gaps and cannot supply an outlet's
        /// height; taking the minimum over the whole ring would let the enclosure define its own
        /// opening.
        /// </remarks>
        public float OutletElevationMeters { get; }

        /// <summary>Where on the ring the outlet sits, so the choice can be audited.</summary>
        public int OutletBoundaryIndex { get; }

        public float RidgePerimeterFraction { get; }

        public float AreaSqm { get; }

        public float PerimeterMeters { get; }

        public StructuralBasin(
            int id,
            int candidateId,
            int corridorId,
            int massifIndex,
            int flankSign,
            int[] enclosingSpurIds,
            Vector2[] boundary,
            BoundarySegment[] segments,
            Vector2 outletPointXZ,
            float outletElevationMeters,
            int outletBoundaryIndex,
            float ridgePerimeterFraction,
            float areaSqm,
            float perimeterMeters)
        {
            Id = id;
            CandidateId = candidateId;
            CorridorId = corridorId;
            MassifIndex = massifIndex;
            FlankSign = flankSign >= 0 ? 1 : -1;
            EnclosingSpurIds = enclosingSpurIds;
            Boundary = boundary;
            Segments = segments;
            OutletPointXZ = outletPointXZ;
            OutletElevationMeters = outletElevationMeters;
            OutletBoundaryIndex = outletBoundaryIndex;
            RidgePerimeterFraction = ridgePerimeterFraction;
            AreaSqm = areaSqm;
            PerimeterMeters = perimeterMeters;
        }
    }

    /// <summary>One candidate's verdict, kept whether it passed or not.</summary>
    public readonly struct BasinVerdict
    {
        public int CandidateId { get; }

        public int CorridorId { get; }

        public BasinRejection Rejection { get; }

        /// <summary>The basin's id if it passed, else -1.</summary>
        public int BasinId { get; }

        public bool Accepted => Rejection == BasinRejection.None;

        public BasinVerdict(int candidateId, int corridorId, BasinRejection rejection, int basinId)
        {
            CandidateId = candidateId;
            CorridorId = corridorId;
            Rejection = rejection;
            BasinId = basinId;
        }
    }

    /// <summary>
    /// Stage 3E slice E2: the cirque enclosures that pass structural validation.
    /// </summary>
    /// <remarks>
    /// Structural and declarative. It reads E1's enclosures, the frozen primary trajectories and the
    /// Stage 2 structural elevation, and it writes basins and verdicts. It carves nothing, reads no
    /// <c>H3</c>, creates no corridor, draws no random number and mutates nothing it was given.
    /// </remarks>
    public sealed class StructuralBasinSet
    {
        public int Seed { get; }

        public RidgeSettings Settings { get; }

        /// <summary>The enclosures this validated. Read-only.</summary>
        public CirqueCandidateSet Candidates { get; }

        public IReadOnlyList<StructuralBasin> Basins { get; }

        /// <summary>One entry per candidate, in candidate order, passed or not.</summary>
        public IReadOnlyList<BasinVerdict> Verdicts { get; }

        public StructuralBasinStats Stats { get; }

        public StructuralBasinSet(
            int seed,
            RidgeSettings settings,
            CirqueCandidateSet candidates,
            IReadOnlyList<StructuralBasin> basins,
            IReadOnlyList<BasinVerdict> verdicts,
            StructuralBasinStats stats)
        {
            Seed = seed;
            Settings = settings;
            Candidates = candidates;
            Basins = basins;
            Verdicts = verdicts;
            Stats = stats;
        }

        /// <summary>The basin enclosing one corridor, or null.</summary>
        public StructuralBasin ForCorridor(int corridorId)
        {
            for (var i = 0; i < Basins.Count; i++)
            {
                if (Basins[i].CorridorId == corridorId)
                {
                    return Basins[i];
                }
            }

            return null;
        }
    }

    public sealed class StructuralBasinStats
    {
        public int Seed;
        public int CandidateCount;
        public int BasinCount;

        /// <summary>Indexed by <see cref="BasinRejection"/>.</summary>
        public readonly int[] Rejections = new int[9];

        public float MinOutletElevationMeters = float.PositiveInfinity;
        public float MaxOutletElevationMeters;
        public float MinRidgePerimeterFraction = float.PositiveInfinity;
        public float MaxRidgePerimeterFraction;

        public long BuildMs;

        public int RejectedCount => CandidateCount - BasinCount;

        public string ToLine() =>
            $"seed {Seed}: candidates={CandidateCount} basins={BasinCount}"
            + $" rejected={RejectedCount} build={BuildMs}ms";

        public string RejectionLine() =>
            $"rejections: geometry={Rejections[(int)BasinRejection.Geometry]}"
            + $" domain={Rejections[(int)BasinRejection.Domain]}"
            + $" outletRun={Rejections[(int)BasinRejection.OutletRun]}"
            + $" ridgeMajority={Rejections[(int)BasinRejection.RidgeMajority]}"
            + $" corridorMissing={Rejections[(int)BasinRejection.CorridorMissing]}"
            + $" outletNotOnCorridor={Rejections[(int)BasinRejection.OutletNotOnCorridor]}"
            + $" notDownhill={Rejections[(int)BasinRejection.CorridorNotDownhill]}"
            + $" foreignValley={Rejections[(int)BasinRejection.ForeignValleyInside]}";

        public string ShapeLine() =>
            $"shape: outlet=[{(MinOutletElevationMeters == float.PositiveInfinity ? -1f : MinOutletElevationMeters):F1},"
            + $"{MaxOutletElevationMeters:F1}]m"
            + $" ridgePerimeter=[{(MinRidgePerimeterFraction == float.PositiveInfinity ? -1f : MinRidgePerimeterFraction):P1},"
            + $"{MaxRidgePerimeterFraction:P1}]";
    }
}
