using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Numbers for Stage 4A, the island-floor relief: plate-block uplift, knolls and the massif apron.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A separate struct for the same reason <see cref="MassifSettings"/> and <see cref="RidgeSettings"/>
    /// are: Stage 1 and Stage 2 are frozen by hash, and the cheapest way to keep them frozen is to not
    /// touch the types their determinism runs through.
    /// </para>
    /// <para>
    /// Every value is the one PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md §15 and §21 settle, with
    /// its reason there. Two values are defaulted from the stage that already owns the number rather than
    /// restated: the convergence a boundary needs to count as a collision
    /// (<see cref="MacroStructureSettings.ConvergenceThreshold"/>) and the tallest a knoll may stand
    /// (<see cref="RidgeSettings.CorridorIncisionMeters"/> - the depth a Stage 3F floor valley may cut,
    /// so every knoll stays breachable).
    /// </para>
    /// </remarks>
    public struct IslandReliefSettings
    {
        // ---------- plate-block uplift ----------

        /// <summary>Height a plate at level 1 stands above one at level 0, metres.</summary>
        public float PlateUpliftMeters;

        /// <summary>Multiplier on the plate uplift on a world that certified no massif.</summary>
        /// <remarks>
        /// The collision still happened; Stage 2A only refused it the room for a narrow range. The crust it
        /// shortened is carried as a broader highland instead. Above ~1.5 the depression at an interior
        /// junction reappears (design §8).
        /// </remarks>
        public float NoMassifUpliftScale;

        /// <summary>Half the width of a block edge, measured perpendicular to the plate boundary, metres.</summary>
        /// <remarks>
        /// The level changes along one smoothstep across twice this distance, so the steepest slope an
        /// edge adds is <c>1.5 * PlateUpliftMeters / (2 * this)</c>: about 7 degrees at the defaults.
        /// </remarks>
        public float PlateEdgeHalfWidthMeters;

        /// <summary>Radius over which the level field relaxes towards the mean at an on-land junction, metres.</summary>
        public float JunctionRelaxRadiusMeters;

        /// <summary>Land a plate needs before it is a district rather than a sliver, square metres.</summary>
        public float MinPlateLandAreaSqm;

        /// <summary>Land a convergent boundary must cross before it scores, metres.</summary>
        public float ConvergentMinLandLengthMeters;

        /// <summary>Coastline the lowest plate needs; without it the lowland would have no outlet, metres.</summary>
        public float MinLowlandFrontageMeters;

        /// <summary>Convergence a boundary needs to count as a collision, in drift units.</summary>
        public float ConvergenceThreshold;

        // ---------- knolls ----------

        /// <summary>Nominal knoll height before resistance and level scale it, metres.</summary>
        public float KnollHeightMeters;

        /// <summary>Hard ceiling on any knoll, metres. Defaulted from the Stage 3F corridor incision.</summary>
        public float KnollHeightCapMeters;

        /// <summary>Share of eligible crust blocks that keep a knoll.</summary>
        public float KnollKeepFraction;

        /// <summary>Lowest plate level a knoll may stand on.</summary>
        public float KnollMinLevel;

        /// <summary>Lowest shore gate a knoll may stand in: not in the outer coastal band.</summary>
        public float KnollMinGate;

        /// <summary>Along-slope kernel sigma as a fraction of the crust block's equivalent diameter.</summary>
        public float KnollMajorSigmaPerDiameter;

        /// <summary>Across-slope kernel sigma as a fraction of the crust block's equivalent diameter.</summary>
        public float KnollMinorSigmaPerDiameter;

        /// <summary>Largest yaw a knoll may turn away from the fall line, degrees.</summary>
        public float KnollYawJitterDegrees;

        /// <summary>Half the central-difference span the fall line is measured over, metres.</summary>
        public float KnollFallLineStepMeters;

        // ---------- massif apron ----------

        /// <summary>Apron height at the toe as a fraction of the massif's crest height.</summary>
        public float ApronHeightPerCrest;

        /// <summary>Highest the apron fill may climb, as a fraction of the crest height.</summary>
        public float ApronCapPerCrest;

        /// <summary>Apron reach beyond the toe on the gentle flank, metres.</summary>
        public float ApronReachGentleMeters;

        /// <summary>Apron reach beyond the toe on the steep flank, metres.</summary>
        public float ApronReachSteepMeters;

        /// <summary>Apron reach beyond the ends of the axis, along strike, metres.</summary>
        public float ApronReachEndMeters;

        /// <summary>Slope of the talus the apron climbs into the flank at, degrees.</summary>
        public float ApronTalusDegrees;

        /// <summary>Smooth-maximum blend between the flank and the apron surface, metres.</summary>
        public float ApronBlendMeters;

        /// <summary>Apron height at the massif ends relative to under a culmination.</summary>
        public float ApronEndHeightFloor;

        // ---------- budget ----------

        /// <summary>Largest relief Stage 4A may produce anywhere, metres. Asserted by tests, never clamped.</summary>
        public float MaxReliefMeters;

        public static IslandReliefSettings Default => new IslandReliefSettings
        {
            PlateUpliftMeters = 18f,
            NoMassifUpliftScale = 1.4f,
            PlateEdgeHalfWidthMeters = 110f,
            JunctionRelaxRadiusMeters = 450f,
            MinPlateLandAreaSqm = 150000f,
            ConvergentMinLandLengthMeters = 150f,
            MinLowlandFrontageMeters = 150f,
            ConvergenceThreshold = MacroStructureSettings.Default.ConvergenceThreshold,

            KnollHeightMeters = 6.5f,
            KnollHeightCapMeters = RidgeSettings.Default.CorridorIncisionMeters,
            KnollKeepFraction = 0.6f,
            KnollMinLevel = 0.34f,
            KnollMinGate = 0.5f,
            KnollMajorSigmaPerDiameter = 0.22f,
            KnollMinorSigmaPerDiameter = 0.14f,
            KnollYawJitterDegrees = 20f,
            KnollFallLineStepMeters = 60f,

            ApronHeightPerCrest = 0.09f,
            ApronCapPerCrest = 0.18f,
            ApronReachGentleMeters = 180f,
            ApronReachSteepMeters = 110f,
            ApronReachEndMeters = 280f,
            ApronTalusDegrees = 22f,
            ApronBlendMeters = 10f,
            ApronEndHeightFloor = 0.55f,

            MaxReliefMeters = 100f,
        };

        public void Clamp()
        {
            PlateUpliftMeters = Mathf.Clamp(PlateUpliftMeters, 0f, 60f);
            NoMassifUpliftScale = Mathf.Clamp(NoMassifUpliftScale, 1f, 3f);
            PlateEdgeHalfWidthMeters = Mathf.Clamp(PlateEdgeHalfWidthMeters, 10f, 600f);
            JunctionRelaxRadiusMeters = Mathf.Clamp(JunctionRelaxRadiusMeters, 0f, 2000f);
            MinPlateLandAreaSqm = Mathf.Clamp(MinPlateLandAreaSqm, 0f, 4e6f);
            ConvergentMinLandLengthMeters = Mathf.Clamp(ConvergentMinLandLengthMeters, 0f, 4000f);
            MinLowlandFrontageMeters = Mathf.Clamp(MinLowlandFrontageMeters, 0f, 10000f);
            ConvergenceThreshold = Mathf.Clamp(ConvergenceThreshold, 0f, 1.9f);

            KnollHeightCapMeters = Mathf.Clamp(KnollHeightCapMeters, 0f, RidgeSettings.Default.CorridorIncisionMeters);
            KnollHeightMeters = Mathf.Clamp(KnollHeightMeters, 0f, 30f);
            KnollKeepFraction = Mathf.Clamp01(KnollKeepFraction);
            KnollMinLevel = Mathf.Clamp01(KnollMinLevel);
            KnollMinGate = Mathf.Clamp01(KnollMinGate);
            KnollMajorSigmaPerDiameter = Mathf.Clamp(KnollMajorSigmaPerDiameter, 0.02f, 1f);
            KnollMinorSigmaPerDiameter = Mathf.Clamp(KnollMinorSigmaPerDiameter, 0.02f, KnollMajorSigmaPerDiameter);
            KnollYawJitterDegrees = Mathf.Clamp(KnollYawJitterDegrees, 0f, 90f);
            KnollFallLineStepMeters = Mathf.Clamp(KnollFallLineStepMeters, 1f, 500f);

            ApronHeightPerCrest = Mathf.Clamp(ApronHeightPerCrest, 0f, 0.5f);
            ApronCapPerCrest = Mathf.Clamp(ApronCapPerCrest, ApronHeightPerCrest, 0.5f);
            ApronReachGentleMeters = Mathf.Clamp(ApronReachGentleMeters, 1f, 2000f);
            ApronReachSteepMeters = Mathf.Clamp(ApronReachSteepMeters, 1f, 2000f);
            ApronReachEndMeters = Mathf.Clamp(ApronReachEndMeters, 1f, 2000f);
            ApronTalusDegrees = Mathf.Clamp(ApronTalusDegrees, 1f, 45f);
            ApronBlendMeters = Mathf.Clamp(ApronBlendMeters, 0f, 100f);
            ApronEndHeightFloor = Mathf.Clamp01(ApronEndHeightFloor);

            MaxReliefMeters = Mathf.Clamp(MaxReliefMeters, 1f, WorldScale.TerrainHeightBudgetMeters);
        }
    }
}
