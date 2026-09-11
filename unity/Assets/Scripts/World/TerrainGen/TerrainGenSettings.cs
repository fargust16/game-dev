using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Tunable knobs for the deterministic terrain generator. Plain C# data, mirroring
    /// <see cref="WorldGenSettings"/> and <c>HydrologySettings</c> in shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generator numbers deliberately live here and not in <see cref="WorldScale"/>. That table is
    /// documented as carrying "the ocean, and only the ocean", and the removed first hydrology
    /// attempt is on record as having been made untunable by putting its constants there. Values
    /// that describe the <em>world</em> (tile size, height budget, sea level, ocean margin) are read
    /// from <see cref="WorldScale"/>; values that describe <em>this generator</em> live here.
    /// </para>
    /// <para>
    /// Stage 1 scope: crustal partition, landmass, coastline and the coastal height model. Fields
    /// for later stages are not present - they arrive with the stage that needs them.
    /// </para>
    /// </remarks>
    [System.Serializable]
    public struct TerrainGenSettings
    {
        /// <summary>
        /// Bumped whenever a change is intended to move the world. A seed plus a version identifies
        /// a world; a seed alone does not.
        /// </summary>
        public int GeneratorVersion;

        // --- field lattice ---

        /// <summary>
        /// Samples per world axis. Must be <c>tileCount * (tileResolution - 1) + 1</c> so the field
        /// is exactly the lattice the Unity tiles share; 2 x 512 + 1 = 1025 at the V0 layout.
        /// </summary>
        public int FieldResolution;

        // --- crustal partition ---

        /// <summary>Minimum separation between crustal sites, metres. Sets the mean cell size.</summary>
        public float CrustSiteSpacingMeters;

        /// <summary>
        /// How far past the world rect sites are placed.
        /// </summary>
        /// <remarks>
        /// Without a margin the partition near the world border is an artefact of the border rather
        /// than of the site distribution, and every edge cell becomes a large wedge.
        /// </remarks>
        public float CrustMarginMeters;

        /// <summary>Cell size of the raster the partition and the region growth run on, metres.</summary>
        public float PartitionCellMeters;

        // --- landmass ---

        /// <summary>Lower bound of the per-seed land-fraction target (fraction of the world rect).</summary>
        public float LandFractionTargetMin;

        /// <summary>Upper bound of the per-seed land-fraction target.</summary>
        public float LandFractionTargetMax;

        /// <summary>Maximum number of detached islets. 0 disables them.</summary>
        public int MaxIslets;

        // --- coast outline ---

        /// <summary>
        /// Douglas-Peucker tolerance applied to the traced cell-union boundary, metres.
        /// </summary>
        /// <remarks>
        /// The trace follows the partition raster and is therefore a staircase at
        /// <see cref="PartitionCellMeters"/>. Simplifying above that recovers the long straight cell
        /// edges the coastline is meant to be built from, before any displacement is applied.
        /// </remarks>
        public float SimplifyToleranceMeters;

        /// <summary>
        /// Midpoint displacement as a fraction of the segment being subdivided.
        /// </summary>
        /// <remarks>
        /// Constant across levels, so the excursion halves as the segments halve and the curve is
        /// self-similar. Raising it past ~0.25 starts producing necks that self-intersect.
        /// </remarks>
        public float CoastRoughness;

        /// <summary>Subdivision stops when a segment is this short, metres.</summary>
        public float CoastMinSegmentMeters;

        // --- coastal height model ---

        /// <summary>Width of the onshore coastal transition, metres.</summary>
        public float CoastalTransitionMeters;

        /// <summary>Height gained across the coastal transition, metres.</summary>
        public float CoastalRiseMeters;

        /// <summary>Distance scale of the gentle interior rise beyond the coastal transition.</summary>
        public float InteriorScaleMeters;

        /// <summary>Height gained across the interior rise, metres.</summary>
        public float InteriorRiseMeters;

        /// <summary>Width of the offshore shelf descent, metres.</summary>
        public float ShelfWidthMeters;

        /// <summary>Depth the offshore shelf settles to below sea level, metres.</summary>
        public float ShelfDropMeters;

        /// <summary>
        /// Distance past which the coastal profiles are saturated, metres.
        /// </summary>
        /// <remarks>
        /// Also the search radius of the nearest-segment query, which is what keeps the distance
        /// field affordable: past saturation the exact distance cannot change the answer.
        /// </remarks>
        public float MaxCoastDistanceMeters;

        // --- mountain uplift belts (Stage 2) ---

        /// <summary>Primary belts kept per world. WORLD_RELIEF_PRINCIPLES allows 1, at most 2.</summary>
        public int MaxMountainSystems;

        /// <summary>Shortest chain of crustal boundaries that can become a primary belt, metres.</summary>
        /// <remarks>
        /// Set against the T1 footprint band rather than against the spine alone: a 700 m spine with
        /// a 450 m half-width covers roughly 1600 x 900 m, which sits inside the 1000-2000 m footprint
        /// WORLD_RELIEF_PRINCIPLES gives the landmark tier. Measured chain lengths across the five
        /// fixed seeds run 728-1575 m, so this admits every one of them - but seed 110201 clears it by
        /// only 28 m, and a seed whose crustal boundaries are all short would produce no mountain at
        /// all. That case is visible in the statistics rather than papered over.
        /// </remarks>
        public float MinSpineLengthMeters;

        /// <summary>Shortest chain that can become a branch, metres.</summary>
        public float MinBranchLengthMeters;

        /// <summary>
        /// Largest direction change accepted between consecutive boundaries of a chain, degrees.
        /// </summary>
        /// <remarks>
        /// Three crustal boundaries meet at a vertex about 120 degrees apart, so continuing through
        /// one means turning about 60. A cap just above that leaves no slack for real junction
        /// geometry, which varies either side of the ideal: measured at 62 degrees, chains stopped at
        /// their first or second vertex and the longest belt on five seeds ran 555-1355 m, so three
        /// of the five produced no mountain at all. 78 accepts an ordinary junction while still
        /// rejecting the roughly 120-degree fold-back that would turn a belt into a random walk.
        /// </remarks>
        public float MaxSpineTurnDegrees;

        /// <summary>Crest height above the local ground, metres. The T1 landmark tier.</summary>
        /// <remarks>
        /// WORLD_RELIEF_PRINCIPLES puts T1 at 250-530 m over a 1000-2000 m footprint. Our island's
        /// equivalent diameter is about 1900 m, so the top of that band would blanket it; and the
        /// world Vista currently ships peaks at roughly 330 m. These numbers sit around that
        /// measurement rather than at the table's ceiling.
        /// </remarks>
        public float CrestHeightMinMeters;

        public float CrestHeightMaxMeters;

        /// <summary>Half-width of the belt at its widest point, metres.</summary>
        /// <remarks>
        /// Measured against the spine length rather than chosen from the tier table alone. Chains run
        /// 700-1500 m, so a half-width of 380-620 m made every belt about as wide as it was long and
        /// the uplift read as overlapping domes rather than as ranges - visible in the second Stage 2
        /// dump. 220-360 m puts the footprint at roughly two to four times its width, which is what
        /// makes a belt read as a belt, and still lands inside the T1 1000-2000 m footprint band.
        /// </remarks>
        public float RangeHalfWidthMinMeters;

        public float RangeHalfWidthMaxMeters;

        /// <summary>
        /// Smallest length-to-half-width ratio a belt may have; the width is capped to reach it.
        /// </summary>
        /// <remarks>
        /// A structure only reads as a range when it is clearly longer than it is wide. Rather than
        /// lowering the length a belt must reach - which would just relabel hills as ranges - the
        /// width follows the length that was actually found, so a short structural path becomes a
        /// narrow ridge instead of a dome.
        /// </remarks>
        public float MinLengthToHalfWidthRatio;

        /// <summary>Half-width multiplier on the scarp flank; below 1, so that side is steeper.</summary>
        public float RangeAsymmetryMin;

        public float RangeAsymmetryMax;

        /// <summary>Branch belts attached to one primary belt, at most.</summary>
        public int MaxBranchesPerRange;

        /// <summary>Branch crest height as a fraction of the primary's.</summary>
        public float BranchCrestScaleMin;

        public float BranchCrestScaleMax;

        /// <summary>Smooth-maximum blend between overlapping belts, metres.</summary>
        /// <remarks>
        /// A plain maximum creases wherever two belts cross, and a field of creases reads as faceted.
        /// The blend rounds the junction; too much of it and the two belts merge into one mushy mass.
        /// </remarks>
        public float RangeBlendMeters;

        /// <summary>
        /// Distance inland over which uplift fades in from zero at the coastline, metres.
        /// </summary>
        /// <remarks>
        /// This is what makes "the coastline does not move" structural rather than hoped for: uplift
        /// is exactly zero at and seaward of the shore, so no ocean sample can be raised and no land
        /// sample can be lowered. Cliff coasts - a belt reaching the water - need the coastal profile
        /// to change instead, which the design defers to a later stage.
        /// </remarks>
        public float UpliftCoastGateMeters;

        /// <summary>Hard ceiling on the combined uplift at any point, metres.</summary>
        public float MaxUpliftMeters;

        public static TerrainGenSettings Default => new TerrainGenSettings
        {
            GeneratorVersion = 1,

            FieldResolution = 1025,

            // Measured, not guessed. The design's estimate of 40-70 cells assumed the cell count
            // only had to buy coastal variety; it also has to buy the growth a choice. A cell may
            // become land only if all of it clears the 280 m ocean margin, and at 260 m spacing that
            // left 22-26 eligible cells against a land budget of 19-24 - so the landmass was very
            // nearly "every interior cell", the edge weights decided almost nothing, and no cell was
            // ever far enough from the mainland to be an islet. 200 m roughly doubles the eligible
            // set and gives the growth real headroom. Evidence: the Stage 1 dump for both values.
            CrustSiteSpacingMeters = 200f,
            CrustMarginMeters = 600f,
            PartitionCellMeters = 10f,

            LandFractionTargetMin = 0.32f,
            LandFractionTargetMax = 0.40f,
            MaxIslets = 2,

            SimplifyToleranceMeters = 18f,
            CoastRoughness = 0.15f,
            CoastMinSegmentMeters = 10f,

            // The onshore numbers put the island floor at 24 + 26 + 30 = 80 m, which is the T0
            // ground the later tiers sit on - WORLD_RELIEF_PRINCIPLES puts that floor in the
            // 43-122 m band, and leaves the whole 700 m budget above it for T1.
            CoastalTransitionMeters = WorldScale.CoastShelfWidthMeters,
            CoastalRiseMeters = 26f,
            InteriorScaleMeters = 600f,
            InteriorRiseMeters = 30f,

            ShelfWidthMeters = 350f,
            ShelfDropMeters = WorldScale.OceanShelfDepthMeters,
            MaxCoastDistanceMeters = 1000f,

            MaxMountainSystems = 2,
            MinSpineLengthMeters = 700f,
            MinBranchLengthMeters = 420f,
            MaxSpineTurnDegrees = 78f,
            CrestHeightMinMeters = 200f,
            CrestHeightMaxMeters = 330f,
            RangeHalfWidthMinMeters = 220f,
            RangeHalfWidthMaxMeters = 360f,
            MinLengthToHalfWidthRatio = 3.2f,
            RangeAsymmetryMin = 0.55f,
            RangeAsymmetryMax = 0.85f,
            MaxBranchesPerRange = 2,
            BranchCrestScaleMin = 0.45f,
            BranchCrestScaleMax = 0.70f,
            RangeBlendMeters = 40f,
            UpliftCoastGateMeters = WorldScale.CoastShelfWidthMeters,
            MaxUpliftMeters = 360f,
        };

        public void Clamp()
        {
            GeneratorVersion = Mathf.Max(1, GeneratorVersion);
            FieldResolution = Mathf.Clamp(FieldResolution, 65, 4097);

            CrustSiteSpacingMeters = Mathf.Clamp(CrustSiteSpacingMeters, 80f, 900f);
            CrustMarginMeters = Mathf.Clamp(CrustMarginMeters, 0f, 2000f);
            PartitionCellMeters = Mathf.Clamp(PartitionCellMeters, 4f, 50f);

            LandFractionTargetMin = Mathf.Clamp(LandFractionTargetMin, 0.05f, 0.90f);
            LandFractionTargetMax = Mathf.Clamp(LandFractionTargetMax, LandFractionTargetMin, 0.90f);
            MaxIslets = Mathf.Clamp(MaxIslets, 0, 8);

            SimplifyToleranceMeters = Mathf.Clamp(SimplifyToleranceMeters, 0f, 200f);
            CoastRoughness = Mathf.Clamp(CoastRoughness, 0f, 0.35f);
            CoastMinSegmentMeters = Mathf.Clamp(CoastMinSegmentMeters, 2f, 200f);

            CoastalTransitionMeters = Mathf.Clamp(CoastalTransitionMeters, 10f, 2000f);
            CoastalRiseMeters = Mathf.Clamp(CoastalRiseMeters, 0f, 400f);
            InteriorScaleMeters = Mathf.Clamp(InteriorScaleMeters, 10f, 4000f);
            InteriorRiseMeters = Mathf.Clamp(InteriorRiseMeters, 0f, 400f);

            ShelfWidthMeters = Mathf.Clamp(ShelfWidthMeters, 10f, 4000f);
            ShelfDropMeters = Mathf.Clamp(ShelfDropMeters, 0f, WorldScale.SeaLevelMeters);
            MaxCoastDistanceMeters = Mathf.Clamp(MaxCoastDistanceMeters, 50f, 4000f);

            MaxMountainSystems = Mathf.Clamp(MaxMountainSystems, 0, 4);
            MinSpineLengthMeters = Mathf.Clamp(MinSpineLengthMeters, 100f, 4000f);
            MinBranchLengthMeters = Mathf.Clamp(MinBranchLengthMeters, 50f, MinSpineLengthMeters);
            MaxSpineTurnDegrees = Mathf.Clamp(MaxSpineTurnDegrees, 20f, 89f);
            CrestHeightMinMeters = Mathf.Clamp(CrestHeightMinMeters, 0f, 600f);
            CrestHeightMaxMeters = Mathf.Clamp(CrestHeightMaxMeters, CrestHeightMinMeters, 600f);
            RangeHalfWidthMinMeters = Mathf.Clamp(RangeHalfWidthMinMeters, 40f, 2000f);
            RangeHalfWidthMaxMeters = Mathf.Clamp(RangeHalfWidthMaxMeters, RangeHalfWidthMinMeters, 2000f);
            MinLengthToHalfWidthRatio = Mathf.Clamp(MinLengthToHalfWidthRatio, 1f, 12f);
            RangeAsymmetryMin = Mathf.Clamp(RangeAsymmetryMin, 0.2f, 1f);
            RangeAsymmetryMax = Mathf.Clamp(RangeAsymmetryMax, RangeAsymmetryMin, 1f);
            MaxBranchesPerRange = Mathf.Clamp(MaxBranchesPerRange, 0, 4);
            BranchCrestScaleMin = Mathf.Clamp(BranchCrestScaleMin, 0.1f, 1f);
            BranchCrestScaleMax = Mathf.Clamp(BranchCrestScaleMax, BranchCrestScaleMin, 1f);
            RangeBlendMeters = Mathf.Clamp(RangeBlendMeters, 0f, 200f);
            UpliftCoastGateMeters = Mathf.Clamp(UpliftCoastGateMeters, 1f, 2000f);
            MaxUpliftMeters = Mathf.Clamp(MaxUpliftMeters, 0f, 650f);
        }

        /// <summary>Highest world Y the Stage 1 coastal model alone can produce, metres.</summary>
        public float MaxLandHeightMeters =>
            WorldScale.SeaLevelMeters + CoastalRiseMeters + InteriorRiseMeters;

        /// <summary>Highest world Y once mountain uplift is added, metres.</summary>
        public float MaxStage2HeightMeters => MaxLandHeightMeters + MaxUpliftMeters;

        /// <summary>Lowest world Y this configuration can produce, metres.</summary>
        public float MinSeaFloorMeters => WorldScale.SeaLevelMeters - ShelfDropMeters;
    }
}
