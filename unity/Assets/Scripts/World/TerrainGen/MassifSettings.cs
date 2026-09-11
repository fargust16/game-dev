using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Numbers for the broad mountain massif built on an accepted macro axis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shape of the massif lives here; its height scale does not. Crest height is read straight
    /// from <see cref="TerrainGenSettings"/>, and the three numbers this struct does carry for
    /// <see cref="MassifUpliftModel"/> - blend, coast gate and ceiling - are defaulted from it rather
    /// than restated, so the project keeps one record of the mountain height scale.
    /// </para>
    /// <para>
    /// Kept out of <see cref="TerrainGenSettings"/> for the same reason
    /// <see cref="MacroStructureSettings"/> is: Stage 1 is frozen by hash and its determinism runs
    /// through that struct.
    /// </para>
    /// </remarks>
    public struct MassifSettings
    {
        /// <summary>
        /// Fraction of the half-width that stays at full crest height, either side of the axis.
        /// </summary>
        /// <remarks>
        /// The high central region. Without it the cross-section peaks at a single line and the massif
        /// reads as a wall with a knife edge; with it the crown is tens of metres across before the
        /// flank starts, which is what a massif looks like from above.
        /// </remarks>
        public float CrownFraction;

        /// <summary>Shortest axis that earns a second culmination, metres.</summary>
        public float SecondCulminationLengthMeters;

        /// <summary>Narrowest certified core that can hold two culminations, in normalised arc.</summary>
        /// <remarks>
        /// Length alone is not enough: the culminations and the saddle between them are placed inside
        /// the core, so a long axis with a short core has to put all three within a few hundredths of
        /// each other, and a smoothstep across a span that short is a cliff along the crest.
        /// </remarks>
        public float MinCoreSpanForTwoCulminations;

        /// <summary>Hard cap on culminations per massif.</summary>
        /// <remarks>
        /// Two. <c>WORLD_RELIEF_PRINCIPLES</c> allows one T1 massif per island and two at most, and a
        /// row of equal humps is the failure this number exists to make impossible.
        /// </remarks>
        public int MaxCulminations;

        /// <summary>How far a culmination may slide along the axis, in normalised arc.</summary>
        public float CulminationJitter;

        /// <summary>Height of the secondary culmination relative to the primary.</summary>
        public float SecondaryHeightMin;

        public float SecondaryHeightMax;

        /// <summary>Saddle height between two culminations, relative to the lower of them.</summary>
        /// <remarks>
        /// Well above zero. A saddle that drops to the ground turns one massif into two hills, which
        /// is the shape Stage 2 kept producing and the reason its longitudinal test exists.
        /// </remarks>
        public float SaddleFractionMin;

        public float SaddleFractionMax;

        /// <summary>Height at the shoulder control point just inside each end, relative to the peak.</summary>
        /// <remarks>
        /// Non-zero so the ends ramp rather than meeting the ground at a corner, small enough that the
        /// ends are clearly lower than the body.
        /// </remarks>
        public float EndShoulderFraction;

        /// <summary>Where the low shoulder sits, as a fraction of its ramp from the end.</summary>
        public float EndShoulderPosition;

        /// <summary>Shortest ramp either side of a culmination, as a fraction of the length.</summary>
        /// <remarks>
        /// Keeps a culmination away from the ends. Every other control point is placed as a fraction of
        /// the ramp it sits on, so this one number is what stops any span becoming degenerate.
        /// </remarks>
        public float MinRampFraction;

        /// <summary>How far the crest holds near its full height either side of a culmination.</summary>
        /// <remarks>
        /// The difference between a massif and a dome. A profile whose maximum is a single point falls
        /// off along the axis at about the rate it falls off across the axis, and the result is an
        /// ellipse with concentric contours - which is what the first measured pass produced on the
        /// shortest seed. Holding the crest over a fifth of the length either side makes the fall-off
        /// across the axis the dominant one, which is what makes the body read as a range.
        /// </remarks>
        public float CrestPlateauSpan;

        /// <summary>Height at the plateau shoulder, relative to its culmination.</summary>
        public float CrestPlateauFraction;

        /// <summary>Half-width at the very ends as a fraction of the nominal.</summary>
        /// <remarks>
        /// Not zero. A massif whose width goes to nothing at the ends is a lens, and a lens seen from
        /// above is the blob silhouette this stage exists to avoid.
        /// </remarks>
        public float WidthTaperFloor;

        /// <summary>How much of each end the width taper acts over, in normalised arc.</summary>
        /// <remarks>
        /// The width is a function of distance from the ends, deliberately not of the longitudinal
        /// profile. Tying it to the profile narrows the body at every saddle and swells it at every
        /// culmination, which turns one massif into a string of beads joined by a neck.
        /// </remarks>
        public float EndTaperSpan;

        /// <summary>Blend width where two massifs meet, metres.</summary>
        /// <remarks>
        /// Read by <see cref="MassifUpliftModel"/>. Defaulted from
        /// <c>TerrainGenSettings.RangeBlendMeters</c> rather than restated, so the mountain height
        /// scale still has one author even though the model reads it from here.
        /// </remarks>
        public float MassifBlendMeters;

        /// <summary>Distance over which the coast gate opens from zero to one, metres.</summary>
        /// <remarks>
        /// The same distance Stage 2A certifies against as
        /// <c>MacroStructureSettings.MassifCoastClearanceMeters</c>. Both default from
        /// <c>TerrainGenSettings.UpliftCoastGateMeters</c>, so the two stages read one number from one
        /// place rather than either reaching into the other; a test asserts they still agree.
        /// </remarks>
        public float CoastGateMeters;

        /// <summary>Hard ceiling on the massif contribution, metres.</summary>
        public float MaxUpliftMeters;

        /// <summary>
        /// Gate the high ground must stand in for its cross-section to be whole, 0 to 1.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Only a verification now. Stage 2A certifies the stretch of an axis whose whole section
        /// clears the coast, and this stage builds its high ground inside that stretch, so a sheared
        /// section cannot arise. This number is what the check is made against: if it ever finds one,
        /// certification and the massif have stopped agreeing about the same geometry.
        /// </para>
        /// <para>
        /// The machinery this stage used to carry for the same purpose - trimming an axis to a
        /// buildable run, placing culminations inside a window it worked out itself, re-deriving a
        /// width from a shortened corridor - is gone. Each was a way of paying, downstream, for a
        /// contract that was wrong upstream, and each traded one wrong shape for another.
        /// </para>
        /// </remarks>
        public float MinCulminationGate;

        /// <summary>How much of the high ground may stand in a part-open gate. Zero under the contract.</summary>
        public float ShearTolerance;

        /// <summary>Cross-section width multiplier between the gentle and the steep flank.</summary>
        /// <remarks>
        /// Milder than the belts of Stage 2 used. This layer is the broad body; a pronounced scarp is
        /// a ridge-scale feature and belongs to whatever builds ridges.
        /// </remarks>
        public float AsymmetryMin;

        public float AsymmetryMax;

        public static MassifSettings Default => new MassifSettings
        {
            CrownFraction = 0.16f,

            SecondCulminationLengthMeters = 1100f,
            MinCoreSpanForTwoCulminations = 0.40f,
            MaxCulminations = 2,
            CulminationJitter = 0.07f,
            SecondaryHeightMin = 0.62f,
            SecondaryHeightMax = 0.82f,
            SaddleFractionMin = 0.55f,
            SaddleFractionMax = 0.72f,

            EndShoulderFraction = 0.18f,
            EndShoulderPosition = 0.30f,

            MinRampFraction = 0.18f,
            CrestPlateauSpan = 0.30f,
            CrestPlateauFraction = 0.88f,

            WidthTaperFloor = 0.58f,
            EndTaperSpan = 0.16f,

            AsymmetryMin = 0.74f,
            AsymmetryMax = 0.92f,

            MassifBlendMeters = TerrainGenSettings.Default.RangeBlendMeters,
            CoastGateMeters = TerrainGenSettings.Default.UpliftCoastGateMeters,
            MaxUpliftMeters = TerrainGenSettings.Default.MaxUpliftMeters,

            MinCulminationGate = 0.9f,
            ShearTolerance = 0f,
        };

        public void Clamp()
        {
            CrownFraction = Mathf.Clamp(CrownFraction, 0f, 0.6f);

            SecondCulminationLengthMeters =
                Mathf.Clamp(SecondCulminationLengthMeters, 100f, 10000f);
            MinCoreSpanForTwoCulminations = Mathf.Clamp(MinCoreSpanForTwoCulminations, 0.1f, 0.9f);
            MaxCulminations = Mathf.Clamp(MaxCulminations, 1, 2);
            CulminationJitter = Mathf.Clamp(CulminationJitter, 0f, 0.15f);
            SecondaryHeightMin = Mathf.Clamp(SecondaryHeightMin, 0.3f, 0.95f);
            SecondaryHeightMax = Mathf.Clamp(SecondaryHeightMax, SecondaryHeightMin, 0.95f);
            SaddleFractionMin = Mathf.Clamp(SaddleFractionMin, 0.3f, 0.9f);
            SaddleFractionMax = Mathf.Clamp(SaddleFractionMax, SaddleFractionMin, 0.9f);

            EndShoulderFraction = Mathf.Clamp(EndShoulderFraction, 0f, 0.5f);
            EndShoulderPosition = Mathf.Clamp(EndShoulderPosition, 0.1f, 0.45f);

            MinRampFraction = Mathf.Clamp(MinRampFraction, 0.1f, 0.4f);
            CrestPlateauSpan = Mathf.Clamp(CrestPlateauSpan, 0.1f, 0.45f);
            CrestPlateauFraction = Mathf.Clamp(CrestPlateauFraction, 0.4f, 0.99f);

            WidthTaperFloor = Mathf.Clamp(WidthTaperFloor, 0.3f, 1f);
            EndTaperSpan = Mathf.Clamp(EndTaperSpan, 0.02f, 0.45f);

            AsymmetryMin = Mathf.Clamp(AsymmetryMin, 0.5f, 1f);
            AsymmetryMax = Mathf.Clamp(AsymmetryMax, AsymmetryMin, 1f);

            MassifBlendMeters = Mathf.Clamp(MassifBlendMeters, 0f, 200f);
            CoastGateMeters = Mathf.Clamp(CoastGateMeters, 1f, 2000f);
            MaxUpliftMeters = Mathf.Clamp(MaxUpliftMeters, 0f, 650f);

            MinCulminationGate = Mathf.Clamp(MinCulminationGate, 0f, 1f);
            ShearTolerance = Mathf.Clamp(ShearTolerance, 0f, 1f);
        }
    }
}
