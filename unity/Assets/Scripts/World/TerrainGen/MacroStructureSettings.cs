using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Numbers for the macro structural layer that mountain axes are drawn from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately a separate struct rather than more fields on <see cref="TerrainGenSettings"/>.
    /// Stage 1 is frozen by hash, and the cheapest way to keep it frozen is to not touch the type its
    /// determinism runs through. When the macro layer is promoted from a feasibility pass into the
    /// real Stage 2 the two can be merged; until then this file is the only place the experiment's
    /// tuning lives.
    /// </para>
    /// <para>
    /// The governing number is <see cref="SiteSpacingMeters"/>. Every other value follows from the
    /// half-width a belt is supposed to carry: a tube of half-width W only embeds around a curve
    /// whose reach exceeds W / <see cref="MountainRange.CurvatureWidthFactor"/>, so the axis needs
    /// bends measured in hundreds of metres, and bends that size only exist in a partition whose
    /// cells are a kilometre across.
    /// </para>
    /// </remarks>
    public struct MacroStructureSettings
    {
        /// <summary>Minimum separation between macro plate sites, metres.</summary>
        /// <remarks>
        /// The one number the whole layer is scaled by. Voronoi edges run roughly 0.5-0.8 of the site
        /// spacing, and a fillet of radius R at a 60 degree junction eats R * tan(30 deg) = 0.58 R off
        /// each end of a segment, so the spacing has to be several times the fillet radius or the
        /// bends have no straight left to sit between.
        /// </remarks>
        public float SiteSpacingMeters;

        /// <summary>How far outside the world macro sites may sit, metres.</summary>
        /// <remarks>
        /// Without an outer ring of sites the cells touching the world edge are unbounded and their
        /// boundaries are artefacts of the clipping rectangle rather than of the partition.
        /// </remarks>
        public float MarginMeters;

        /// <summary>Clip rectangle inflation beyond the world, metres.</summary>
        public float ClipMarginMeters;

        /// <summary>Distance over which the regional compression reverses sign, metres.</summary>
        /// <remarks>
        /// Half the world, so one side drifts one way and the other side the other. Larger values
        /// flatten the field towards a single direction and stop producing convergence at all.
        /// </remarks>
        public float StressScaleMeters;

        /// <summary>How much of a plate's motion is its own rather than the regional field.</summary>
        /// <remarks>
        /// Zero makes every belt perpendicular to the compression axis; one drowns the field in
        /// per-plate directions and brings back the coin toss it replaced.
        /// </remarks>
        public float DriftJitter;

        /// <summary>Convergence a boundary needs before it is a mountain candidate, in drift units.</summary>
        /// <remarks>
        /// Convergence is <c>dot(driftA - driftB, normal)</c> with unit drift vectors, so it lives in
        /// [-2, 2]. Zero would make half of all boundaries candidates.
        /// </remarks>
        public float ConvergenceThreshold;

        /// <summary>Largest direction change a chain may take at a junction, degrees.</summary>
        /// <remarks>
        /// Three cells meet at a Voronoi vertex and the three boundaries leaving it are roughly 120
        /// degrees apart, so a chain that continues through a junction turns about 60 degrees whichever
        /// branch it takes. There is no straighter option to prefer; the cap exists to reject the
        /// degenerate junctions, not to keep chains straight.
        /// </remarks>
        public float MaxTurnDegrees;

        /// <summary>Radius the junction fillets aim for, metres.</summary>
        public float FilletRadiusMeters;

        /// <summary>Radius below which a junction is cut instead of rounded, metres.</summary>
        /// <remarks>
        /// This is the guarantee. Every arc on an axis has radius at least this, so the curvature
        /// radius of the axis is at least this everywhere, and a belt of half-width
        /// <c>0.72 * this</c> embeds unless the axis approaches itself.
        /// </remarks>
        public float MinFilletRadiusMeters;

        /// <summary>Spacing of the sampled axis polyline, metres.</summary>
        public float SampleSpacingMeters;

        /// <summary>How far inside the coast a boundary must reach to be a candidate at all, metres.</summary>
        /// <remarks>
        /// A cheap first filter, not the contract. It says only "this boundary touches land somewhere";
        /// what an axis actually needs is room for the body Stage 2B will raise on it, and that is
        /// <see cref="MassifCoastClearanceMeters"/> plus the certified half-width, measured along the
        /// whole axis rather than at one point. This number was 60 m because that was convenient for
        /// getting length out of an axis, never because 60 m meant anything geologically.
        /// </remarks>
        public float CoastSetbackMeters;

        /// <summary>
        /// Coastal room a massif needs beyond its own half-width, metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The downstream half of the contract, expressed as geometry rather than as a Stage 2B
        /// implementation detail. Stage 2B fades its contribution in from the shoreline over this
        /// distance so the coastline cannot move; until it has faded in, a cross-section that reaches
        /// the water is cut on its seaward flank and not on its inland one, which shears the body
        /// instead of lowering it.
        /// </para>
        /// <para>
        /// Defaulted from <c>TerrainGenSettings.UpliftCoastGateMeters</c>, which is where the project
        /// already records that distance. Both stages read the same number from the same place rather
        /// than one stage reaching into the other.
        /// </para>
        /// </remarks>
        public float MassifCoastClearanceMeters;

        /// <summary>
        /// How long a massif's high ground must be, per metre of its half-width.
        /// </summary>
        /// <remarks>
        /// A body whose high ground is wider than it is long is a mesa however good its axis is, so
        /// the certified core has to be at least this many half-widths long. Two means the crest
        /// region is at least as long as the body is wide.
        /// </remarks>
        public float MinCoreLengthPerHalfWidth;

        /// <summary>Step of the width ladder certification walks down, metres.</summary>
        /// <remarks>
        /// Certification asks "what is the widest body this axis has room for", and answers it by
        /// trying widths from the target downwards. The step only sets how finely the answer is
        /// quantised; it does not decide anything.
        /// </remarks>
        public float CertificationWidthStepMeters;

        /// <summary>Shortest on-land axis worth keeping, metres.</summary>
        public float MinAxisLengthMeters;

        /// <summary>Half-width a belt on this axis is meant to carry, metres.</summary>
        public float TargetHalfWidthMeters;

        /// <summary>Usable half-width below which an axis is not worth building a belt on, metres.</summary>
        public float MinUsableHalfWidthMeters;

        /// <summary>Length an axis must have per metre of half-width before it reads as a range.</summary>
        /// <remarks>
        /// Applied by narrowing the belt rather than by rejecting the axis: a short axis is still a
        /// range, just a narrower one, and dropping it leaves a world with no mountains at all. Matches
        /// <c>TerrainGenSettings.MinLengthToHalfWidthRatio</c>, which Stage 2 checks after the fact.
        /// </remarks>
        public float MinLengthToWidthRatio;

        /// <summary>Closest two accepted axes may come to each other, metres.</summary>
        public float MinSystemSeparationMeters;

        /// <summary>Most mountain systems accepted per world.</summary>
        public int MaxSystems;

        /// <summary>Most boundaries one chain may consume.</summary>
        public int MaxChainEdges;

        public static MacroStructureSettings Default => new MacroStructureSettings
        {
            // Scaled to the island, not to the world. The landmass runs about 1.7 km across, and a
            // partition wants two or three plates on it: their shared boundaries then run right across
            // it. At 820 m five plates fitted, every cell was small, and no boundary ran further than
            // 580 m over land.
            SiteSpacingMeters = 1100f,
            MarginMeters = 1400f,
            ClipMarginMeters = 900f,

            StressScaleMeters = WorldScale.WorldExtentXMeters * 0.5f,
            DriftJitter = 0.45f,
            ConvergenceThreshold = 0.25f,
            MaxTurnDegrees = 78f,

            // A 60 degree junction eats R * tan(30 deg) off each side, so two fillets need 1.15 R of
            // straight between them; 460 m is what boundaries around 550-700 m can actually carry.
            FilletRadiusMeters = 460f,

            // The floor, not the aim: 0.72 * 300 = 216 m of half-width, still above the 200 m an axis
            // has to carry to be worth accepting. Below this a bend is cut instead of rounded.
            MinFilletRadiusMeters = 300f,

            SampleSpacingMeters = 20f,
            CoastSetbackMeters = 60f,
            MassifCoastClearanceMeters = TerrainGenSettings.Default.UpliftCoastGateMeters,
            MinCoreLengthPerHalfWidth = 2f,
            CertificationWidthStepMeters = 10f,
            MinAxisLengthMeters = 700f,

            TargetHalfWidthMeters = 280f,
            MinUsableHalfWidthMeters = 200f,
            MinLengthToWidthRatio = 3.2f,
            MinSystemSeparationMeters = 600f,
            MaxSystems = 2,
            MaxChainEdges = 8,
        };

        public void Clamp()
        {
            SiteSpacingMeters = Mathf.Clamp(SiteSpacingMeters, 400f, 3000f);
            MarginMeters = Mathf.Clamp(MarginMeters, 0f, 4000f);
            ClipMarginMeters = Mathf.Clamp(ClipMarginMeters, 0f, 4000f);

            StressScaleMeters = Mathf.Clamp(StressScaleMeters, 100f, 20000f);
            DriftJitter = Mathf.Clamp(DriftJitter, 0f, 2f);
            ConvergenceThreshold = Mathf.Clamp(ConvergenceThreshold, 0f, 1.9f);
            MaxTurnDegrees = Mathf.Clamp(MaxTurnDegrees, 10f, 89f);

            MinFilletRadiusMeters = Mathf.Clamp(MinFilletRadiusMeters, 50f, 4000f);
            FilletRadiusMeters = Mathf.Clamp(FilletRadiusMeters, MinFilletRadiusMeters, 4000f);

            SampleSpacingMeters = Mathf.Clamp(SampleSpacingMeters, 2f, 100f);
            CoastSetbackMeters = Mathf.Clamp(CoastSetbackMeters, 0f, 1000f);
            MassifCoastClearanceMeters = Mathf.Clamp(MassifCoastClearanceMeters, 0f, 2000f);
            MinCoreLengthPerHalfWidth = Mathf.Clamp(MinCoreLengthPerHalfWidth, 0.5f, 8f);
            CertificationWidthStepMeters = Mathf.Clamp(CertificationWidthStepMeters, 1f, 100f);
            MinAxisLengthMeters = Mathf.Clamp(MinAxisLengthMeters, 100f, 5000f);

            TargetHalfWidthMeters = Mathf.Clamp(TargetHalfWidthMeters, 40f, 2000f);
            MinUsableHalfWidthMeters = Mathf.Clamp(MinUsableHalfWidthMeters, 20f, TargetHalfWidthMeters);
            MinLengthToWidthRatio = Mathf.Clamp(MinLengthToWidthRatio, 1f, 12f);
            MinSystemSeparationMeters = Mathf.Clamp(MinSystemSeparationMeters, 0f, 5000f);
            MaxSystems = Mathf.Clamp(MaxSystems, 0, 6);
            MaxChainEdges = Mathf.Clamp(MaxChainEdges, 1, 32);
        }

        /// <summary>Reach an axis needs before it can carry <see cref="TargetHalfWidthMeters"/>.</summary>
        public float RequiredReachMeters => TargetHalfWidthMeters / MountainRange.CurvatureWidthFactor;
    }
}
