using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Numbers for the Stage 3 structural network: the ownership layer, then the spur skeleton.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A separate struct beside <see cref="MacroStructureSettings"/> and <see cref="MassifSettings"/>,
    /// for the same reason those are separate from <see cref="TerrainGenSettings"/>: Stage 1 and
    /// Stage 2 are frozen by hash, and the cheapest way to keep them frozen is to not touch the types
    /// their determinism runs through.
    /// </para>
    /// <para>
    /// Only the values a built slice actually reads are here. Valley depth, the cross-valley profile
    /// and the basin thresholds belong to later slices and are deliberately absent - a field nothing
    /// reads is a decision made before it was measured, and Stage 2 spent three passes undoing those.
    /// </para>
    /// <para>
    /// Stage 3A draws no seed stream at all - it is a geometric consequence of Stage 2's axes and
    /// Stage 1's coastline. Stage 3B draws two, and only for bounded variation: how many spurs there
    /// are and where they attach come from the spacing rule and the attachment interval.
    /// </para>
    /// </remarks>
    public struct RidgeSettings
    {
        /// <summary>
        /// How close two axis distances must agree before the point counts as equidistant, metres.
        /// </summary>
        /// <remarks>
        /// Ownership is decided by comparing two distances measured in world coordinates that run to
        /// 2800 m, where a float carries about a quarter of a millimetre. The epsilon is far above
        /// that noise and far below anything geographic, so the tie rule fires on genuine ties and the
        /// answer does not depend on the order the arithmetic happened to round.
        /// </remarks>
        public float OwnershipTieEpsilonMeters;

        /// <summary>Spacing of the marched medial divide, metres.</summary>
        /// <remarks>
        /// Matched to <see cref="MacroStructureSettings.SampleSpacingMeters"/>, which is what the axes
        /// the divide is equidistant from are sampled at. A finer divide would resolve detail its own
        /// inputs do not carry.
        /// </remarks>
        public float DivideStepMeters;

        /// <summary>Newton corrections applied per marched step.</summary>
        /// <remarks>
        /// The correction is an exact Newton step on <c>dA - dB</c>, whose gradient is known in closed
        /// form, so it converges quadratically and two iterations are usually enough. Four is cheap
        /// insurance for the steps taken near a junction, where the nearest point jumps between
        /// segments and the first step lands further off.
        /// </remarks>
        public int DivideRefineIterations;

        /// <summary>How far off the medial set a marched point may sit before the arm is abandoned, metres.</summary>
        public float DivideToleranceMeters;

        /// <summary>Longest a single divide arm may run before it is declared unresolved, metres.</summary>
        /// <remarks>
        /// Twice the world, so a divide that behaves cannot hit it: on an island every arm leaves the
        /// land long before. Reaching this limit means the march is circling rather than terminating,
        /// which is a defect to report and not a length to accept.
        /// </remarks>
        public float DivideMaxArmMeters;

        // ---------- Stage 3B: the primary spur skeleton ----------

        /// <summary>Spur spacing as a multiple of the massif's local half-width.</summary>
        /// <remarks>
        /// <para>
        /// The one number the spur layout is scaled by, and it is derived rather than chosen. The
        /// design fixes the form - <c>spacing = k * HalfWidthAt(s)</c>, because real spur spacing
        /// scales with the slope length it drains - and fixes the acceptance criterion: two to five
        /// spurs on a massif of 1000 to 1700 m. Solving that criterion at both ends of the range gives
        /// <c>k</c> in <c>[0.64, 1.07]</c>, and this is its midpoint.
        /// </para>
        /// <para>
        /// Measured on the six worlds that carry a massif it yields 3, 3, 4, 3, 1 and 3 spurs a flank.
        /// The one is seed 101010, whose usable core is 254 m: the design's own risk section calls for
        /// "an accepted floor of one spur per flank" rather than a crowded partition, and that is what
        /// this produces.
        /// </para>
        /// </remarks>
        public float SpurSpacingPerHalfWidth;

        /// <summary>Most primary spurs one flank may carry.</summary>
        public int MaxPrimarySpursPerFlank;

        /// <summary>
        /// How far a station may be nudged off its even position, as a fraction of the gap.
        /// </summary>
        /// <remarks>
        /// Bounded by the design's anti-crowding rule rather than by taste. Two neighbouring stations
        /// drawing opposite extremes close the gap between them to <c>1 - 2j</c> of nominal, so a
        /// quarter is the largest jitter that cannot squeeze any pair below half the spacing the
        /// spacing rule asked for. Below the bound the layout is a comb; above it, the partition
        /// crowds.
        /// </remarks>
        public float SpurStationJitter;

        /// <summary>
        /// How far a spur may drift along the axis over its length, as a fraction of the room to its
        /// neighbour.
        /// </summary>
        /// <remarks>
        /// The design's own bound: "half of the spacing to the neighbouring attachment, which is what
        /// keeps the spur inside its strip". Measured to the actual neighbours rather than to a
        /// nominal gap, so a station that jittered towards one side has less room on that side. Half
        /// on its own would let two neighbours put their toes in the same place, so the draw is also
        /// held clear of where the previous toe landed - see
        /// <c>PrimarySpurBuilder.DrawDrift</c>. That guard, not a smaller fraction, is what keeps the
        /// valley open.
        /// </remarks>
        public float SpurDriftPerGap;

        /// <summary>Per-spur variation in length, as a fraction.</summary>
        /// <remarks>
        /// <para>
        /// Bounded by the guarantee Stage 2A already provides, so no spur can be aimed outside the
        /// structural domain on any world rather than merely on the worlds measured. Stage 2A
        /// certifies <c>UsableHalfWidth &lt;= CurvatureWidthFactor * reach</c>, so
        /// <c>reach &gt;= W / 0.72 = 1.389 W</c>; the widest flank a massif can have is
        /// <c>(2 - AsymmetryMin) W = 1.26 W</c>. The ratio, <c>1.102</c>, is all the headroom that is
        /// guaranteed, and the jitter takes a tenth of it.
        /// </para>
        /// <para>
        /// Variation, never topology. It changes how far a spur reaches and nothing about how many
        /// there are or where they attach.
        /// </para>
        /// </remarks>
        public float SpurReachJitter;

        /// <summary>
        /// Shortest a spur gets where the crest is at its lowest, as a fraction of the flank width.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Not a new number. It is <see cref="MassifSettings.WidthTaperFloor"/>, which is Stage 2B's
        /// own answer to how far a massif's lateral extent shrinks where the massif is weakest.
        /// Reusing it means Stage 3 does not hold a second opinion about the same question.
        /// </para>
        /// <para>
        /// What it bounds is the relief scaling: a spur attached under a culmination reaches the full
        /// flank width, one attached at a shoulder reaches this fraction of it. Above zero because a
        /// spur shorter than a couple of samples is a fragment rather than a ridge.
        /// </para>
        /// </remarks>
        public float SpurReliefFloor;

        /// <summary>Deepest a valley cuts, as a fraction of the local crest height.</summary>
        /// <remarks>
        /// <para>
        /// Valleys scale with the mountain rather than being a fixed size, which is why the fraction
        /// is of <see cref="MountainMassif.CrestAt"/> and not of a number in metres: a valley on a
        /// 250 m massif and one on a 530 m massif are both T1 features and neither is a T2 one.
        /// </para>
        /// <para>
        /// The value is set against the slope budget in WORLD_RELIEF_PRINCIPLES, which is the
        /// authoritative constraint and is measured rather than assumed - a deeper cut over the same
        /// half-width steepens the flank, and the principles cap ground steeper than 45 degrees at
        /// 15 per cent.
        /// </para>
        /// </remarks>
        public float ValleyDepthPerCrest;

        /// <summary>
        /// Deepest a valley corridor may cut below the Stage 1 surface, metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The bounded exception the corridor continuation needs. Outside the massif
        /// <c>H2 = H1</c>, so a trough across the island floor necessarily cuts below Stage 1;
        /// without the exception a valley must end where the massif uplift does, and its support
        /// closes into a bowl. The exception applies <b>only inside a declared valley corridor</b> -
        /// everywhere else Stage 3 keeps <c>H3 &gt;= H1</c>.
        /// </para>
        /// <para>
        /// Derived, not chosen. WORLD_RELIEF_PRINCIPLES puts the T2 plateau band at 18-40 m and
        /// requires adjacent tiers to differ by at least 2.5x, so <c>18 / 2.5 = 7.2</c> is the largest
        /// amplitude guaranteed to read as a tier below T2 - which is exactly what the design's "capped
        /// well under the T2 band so it never competes with a plateau" asks for. It lands inside the T3
        /// band, 5-12 m, the right scale for a shallow trough on a coastal plain.
        /// </para>
        /// </remarks>
        public float CorridorIncisionMeters;

        /// <summary>Sample spacing along a spur, metres.</summary>
        /// <remarks>
        /// The same 20 m the macro axis is sampled at. A spur resolved finer than the curve it hangs
        /// off would be resolving its own interpolation.
        /// </remarks>
        public float SpurSampleSpacingMeters;

        public static RidgeSettings Default => new RidgeSettings
        {
            OwnershipTieEpsilonMeters = 0.05f,
            DivideStepMeters = MacroStructureSettings.Default.SampleSpacingMeters,
            DivideRefineIterations = 4,
            DivideToleranceMeters = 0.05f,
            DivideMaxArmMeters = 2f * WorldScale.WorldExtentXMeters,

            SpurSpacingPerHalfWidth = 0.85f,
            MaxPrimarySpursPerFlank = 5,
            SpurStationJitter = 0.25f,
            SpurDriftPerGap = 0.5f,
            SpurReachJitter = 0.10f,
            SpurReliefFloor = MassifSettings.Default.WidthTaperFloor,
            ValleyDepthPerCrest = 0.30f,
            CorridorIncisionMeters = 18f / 2.5f,
            SpurSampleSpacingMeters = MacroStructureSettings.Default.SampleSpacingMeters,
        };

        public void Clamp()
        {
            OwnershipTieEpsilonMeters = Mathf.Clamp(OwnershipTieEpsilonMeters, 1e-4f, 5f);
            DivideStepMeters = Mathf.Clamp(DivideStepMeters, 2f, 100f);
            DivideRefineIterations = Mathf.Clamp(DivideRefineIterations, 1, 16);
            DivideToleranceMeters = Mathf.Clamp(DivideToleranceMeters, 1e-3f, 5f);
            DivideMaxArmMeters = Mathf.Clamp(
                DivideMaxArmMeters, 100f, 8f * WorldScale.WorldExtentXMeters);

            SpurSpacingPerHalfWidth = Mathf.Clamp(SpurSpacingPerHalfWidth, 0.25f, 4f);
            MaxPrimarySpursPerFlank = Mathf.Clamp(MaxPrimarySpursPerFlank, 1, 12);

            // Half would let two neighbours meet; a quarter is the bound both are derived at, and
            // clamping to it means a hand-edited setting cannot break the partition.
            SpurStationJitter = Mathf.Clamp(SpurStationJitter, 0f, 0.25f);
            SpurDriftPerGap = Mathf.Clamp(SpurDriftPerGap, 0f, 0.5f);
            SpurReachJitter = Mathf.Clamp(SpurReachJitter, 0f, 0.5f);
            SpurReliefFloor = Mathf.Clamp(SpurReliefFloor, 0.1f, 1f);
            ValleyDepthPerCrest = Mathf.Clamp(ValleyDepthPerCrest, 0f, 0.8f);
            CorridorIncisionMeters = Mathf.Clamp(CorridorIncisionMeters, 0f, 18f);
            SpurSampleSpacingMeters = Mathf.Clamp(SpurSampleSpacingMeters, 2f, 100f);
        }
    }
}
