namespace GameDev.World
{
    /// <summary>
    /// Single world-scale table: character → vegetation → lakes/shores → relief → chunks.
    /// All overworld placers/scatter/player sizing should read these constants instead of
    /// inventing independent fudge factors.
    /// </summary>
    public static class WorldScale
    {
        // --- Character (base unit) ---
        public const float CharacterHeightMeters = 1.8f;
        public const float CharacterRadiusMeters = 0.3f;

        // --- Vegetation ---
        public const float TreeMatureMinMeters = 12f;
        public const float TreeMatureMaxMeters = 25f;
        public const float TreeYoungMinMeters = 8f;
        public const float TreeYoungMaxMeters = 14f;
        public const float TreeHeroMaxMeters = 40f;

        public const float ShrubMinMeters = 0.5f;
        public const float ShrubMaxMeters = 3f;

        public const float GrassShortMinMeters = 0.05f;
        public const float GrassShortMaxMeters = 0.5f;
        public const float GrassTallMinMeters = 0.3f;
        public const float GrassTallMaxMeters = 1f;

        public const float FlowerMinMeters = 0.1f;
        public const float FlowerMaxMeters = 1f;

        // --- Hydrology ---
        public const float LakeRadiusMinMeters = 70f;
        public const float LakeRadiusMaxMeters = 220f;
        public const float LakeBasinDepthMinMeters = 12f;
        public const float LakeBasinDepthMaxMeters = 26f;

        /// <summary>
        /// Horizontal recess (InsetWidth): water sheet expands past the visual shore under banks.
        /// Spec 1.0–2.5 m — prefer larger over visible gaps / floating edges.
        /// </summary>
        public const float WaterOverlapMeters = 1.75f;

        /// <summary>Legacy alias — prefer <see cref="WaterOverlapMeters"/> (positive expand).</summary>
        public const float WaterShoreInsetMeters = -WaterOverlapMeters;

        /// <summary>
        /// Vertical recess (InsetDepth): dry shore must sit at least this far above the water plane.
        /// Spec 0.5–1.5 m.
        /// </summary>
        public const float WaterInsetDepthMeters = 1.0f;

        /// <summary>
        /// Extra depth on steep shores: effectiveInset = base + slope * this (slope ≈ rise/run).
        /// </summary>
        public const float WaterInsetSlopeFactor = 2.5f;

        /// <summary>Extra dig margin beyond overlap (spec Buffer 0.2–0.5 m).</summary>
        public const float WaterInsetBufferMeters = 0.35f;

        /// <summary>
        /// Water plane vs average dry shore:
        /// <c>WaterLevel = AvgShore − WaterDepthOffset</c> (≥ InsetDepth so the sheet stays buried).
        /// </summary>
        public const float WaterDepthOffsetMeters = 1.25f;

        /// <summary>
        /// Basin dig expands beyond water shore iso (overlap + buffer) so the sheet always sits
        /// inside the excavated bowl.
        /// </summary>
        public const float LakeBasinExpandMeters = WaterOverlapMeters + WaterInsetBufferMeters;

        /// <summary>Hard reject when average slope in the footprint exceeds this (~9°).</summary>
        public const float LakeRejectAvgSlope = 0.16f;

        /// <summary>Hard reject when any sample slope exceeds this (~20°).</summary>
        public const float LakeRejectMaxSlope = 0.36f;

        /// <summary>Hard reject when high-terrain mass penalty exceeds this (mountain flank).</summary>
        public const float LakeRejectHighPenalty = 0.8f;

        /// <summary>
        /// Max metres any single heightmap sample may be dug for a lake basin (placement / banks).
        /// Interior bowl may use <see cref="LakeInteriorMaxDigMeters"/> so the sheet stays one body.
        /// </summary>
        public const float LakeMaxTerrainModificationMeters = 18f;

        /// <summary>
        /// Interior-only dig cap. Must exceed typical hillside offset so a ridge cannot split the lake.
        /// </summary>
        public const float LakeInteriorMaxDigMeters = 36f;

        /// <summary>Minimum water column inside the shore (metres below waterY).</summary>
        public const float LakeMinInteriorWaterMeters = 2.5f;

        /// <summary>
        /// Max fill in the shore-overlap ring only (not the interior). Raises downhill banks so the
        /// unchanged flat water sheet sits under terrain instead of hanging over a valley.
        /// </summary>
        public const float LakeShoreLipFillMaxMeters = 16f;

        /// <summary>
        /// Reject when height range in footprint exceeds this with mountain mass present.
        /// </summary>
        public const float LakeRejectHeightRangeMeters = 28f;

        /// <summary>Minimum distance from map/usable edge to a lake center.</summary>
        public const float LakeEdgeMinDistanceMeters = 160f;

        /// <summary>Guaranteed minimum lake count per overworld island.</summary>
        public const int LakeMinCount = 2;

        /// <summary>
        /// Preferred upper lake count. Actual count is seed-derived in
        /// <c>[LakeMinCount, LakePreferredMaxCount]</c>.
        /// </summary>
        public const int LakePreferredMaxCount = 3;

        /// <summary>
        /// Terrain around a closed lake rim must stay above
        /// <c>SeaLevel + LakeSafetyMargin</c> so the single water plane cannot spill to the ocean.
        /// </summary>
        public const float LakeSafetyMarginMeters = 6f;

        /// <summary>Minimum inland water-region area (heightmap cells) to count as a valid lake.</summary>
        public const int LakeMinCells = 48;

        // Area-fraction size tiers (of tile area) → equivalent radius via √(A/π).
        public const float LakeSmallAreaFracMin = 0.005f;
        public const float LakeSmallAreaFracMax = 0.02f;
        public const float LakeMediumAreaFracMin = 0.02f;
        public const float LakeMediumAreaFracMax = 0.05f;
        public const float LakeLargeAreaFracMin = 0.05f;
        public const float LakeLargeAreaFracMax = 0.12f;

        // --- Rivers ---
        public const float RiverSourceWidthMeters = 4f;
        public const float RiverMouthWidthMeters = 22f;
        public const float RiverBankWidthMeters = 10f;
        public const float RiverWidthGrowthExponent = 1.3f;

        // --- Roads (LandscapePlanner corridor) ---
        public const float RoadPrimaryWidthMeters = 4f;
        public const float RoadPrimaryShoulderMeters = 2.5f;
        public const float RoadPrimaryBlendMeters = 8f;
        public const float RoadSecondaryWidthMeters = 2.5f;
        public const float RoadSecondaryShoulderMeters = 1.5f;
        public const float RoadSecondaryBlendMeters = 5f;
        public const float RoadLocalWidthMeters = 1.5f;
        public const float RoadLocalShoulderMeters = 0.75f;
        public const float RoadLocalBlendMeters = 3f;

        /// <summary>Legacy aliases for Primary / Secondary widths.</summary>
        public const float RoadMainWidthMeters = RoadPrimaryWidthMeters;
        public const float RoadMainShoulderMeters = RoadPrimaryShoulderMeters;
        public const float RoadMainBlendMeters = RoadPrimaryBlendMeters;
        public const float RoadBranchWidthMeters = RoadSecondaryWidthMeters;
        public const float RoadBranchShoulderMeters = RoadSecondaryShoulderMeters;
        public const float RoadBranchBlendMeters = RoadSecondaryBlendMeters;

        public const float RoadDetourTolerance = 1.20f;
        public const float RoadExistingCorridorHalfWidthMeters = 12f;
        public const float RoadExistingCorridorDiscount = 0.15f;
        public const float RoadMaxGrade = 0.12f;
        public const float RoadMaxGradeChange = 0.04f;
        public const float RoadMaxCutDepthMeters = 1.5f;
        public const float RoadMaxFillHeightMeters = 1.0f;
        public const float RoadMaxSideSlope = 0.5f;
        public const float RoadAllowedEarthworkDepthMeters = 1.0f;
        public const float LandscapeCostCellMeters = 10f;

        // --- Macro geography ---
        /// <summary>Edge length of one Unity Terrain tile (Vista grid cell).</summary>
        public const float OverworldTileMeters = 1400f;

        /// <summary>Tiles along world +X.</summary>
        public const int OverworldTileCountX = 2;

        /// <summary>Tiles along world +Z.</summary>
        public const int OverworldTileCountZ = 2;

        /// <summary>
        /// Outer falloff each per-tile Vista biome extends past the tile edge so neighboring
        /// caches soft-blend. Keeps LPB footprint near demo scale (~tile + 2×overlap), not the
        /// full world extent (world-extent biomes OOM simulation nodes).
        /// </summary>
        public const float VistaBiomeSeamOverlapMeters = 80f;

        /// <summary>Full overworld width (tileCountX × tile size).</summary>
        public static float WorldExtentXMeters => OverworldTileMeters * OverworldTileCountX;

        /// <summary>Full overworld depth (tileCountZ × tile size).</summary>
        public static float WorldExtentZMeters => OverworldTileMeters * OverworldTileCountZ;

        public const float ChunkSizeMeters = 64f;
        public const float TerrainHeightBudgetMeters = 700f;

        /// <summary>
        /// World Y of the ocean plane. Full terrain continues under this level as shelf/bathymetry;
        /// dry island land sits above it. With <see cref="TerrainHeightBudgetMeters"/> = 700,
        /// peaks have ~676 m of relief above water at default sea.
        /// Lower than the old 32 m so Outer-falloff sand shelves sit above the waterline.
        /// </summary>
        public const float SeaLevelMeters = 24f;

        /// <summary>
        /// Soft margin from world edge to typical island coastline (ocean shelf toward the border).
        /// Island silhouette stays inscribed inside the world extent square.
        /// </summary>
        public const float IslandOceanMarginMeters = 280f;

        /// <summary>
        /// Typical horizontal width of the directed coast transition (sea → green terrain).
        /// Geometry SoT: <c>CoastalBeachShelf</c> (≤12° profile + coast-band smooth).
        /// </summary>
        public const float CoastShelfWidthMeters = 200f;

        /// <summary>
        /// Typical dry-sand band width above sea level (material paint guide).
        /// </summary>
        public const float BeachWidthMeters = 80f;

        /// <summary>
        /// Snow line as metres above <see cref="SeaLevelMeters"/> (not a raw 0–1 heightmap fraction).
        /// </summary>
        public const float SnowLineMeters = 95f;

        /// <summary>How far below sea the outer shelf floor settles (metres of water column at tile edge).</summary>
        public const float OceanShelfDepthMeters = 22f;

        /// <summary>
        /// Tile-border cells intended as ocean must sit at least this far below Sea Level
        /// (prevents visible terrain/water gaps).
        /// </summary>
        public const float MinimumSubmersionMeters = 1.5f;

        /// <summary>Minimum major peaks per island (seeded Mountain LPBs).</summary>
        public const int PeakMinCount = 1;

        /// <summary>Preferred peak count lower bound.</summary>
        public const int PeakPreferredMinCount = 1;

        /// <summary>Preferred peak count upper bound (1–3 natural mountains).</summary>
        public const int PeakPreferredMaxCount = 2;

        /// <summary>Minimum world-space distance between major peaks / massifs.</summary>
        public const float PeakMinSeparationMeters = 480f;

        /// <summary>
        /// Mouth band: river carve may go at/below Sea Level only within this many metres of the ocean
        /// (cells with landMask near 0 / near coastline). Inland beds stay above Sea Level.
        /// </summary>
        public const float RiverMouthSeaBandMeters = 90f;
    }
}
