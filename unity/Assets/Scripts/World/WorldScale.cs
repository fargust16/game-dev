namespace GameDev.World
{
    /// <summary>
    /// Single world-scale table: character -> vegetation -> roads -> relief -> chunks.
    /// All overworld placers/scatter/player sizing should read these constants instead of
    /// inventing independent fudge factors.
    /// </summary>
    /// <remarks>
    /// V0 water is the ocean plane only. Inland lake and river generation was removed, and with it
    /// every Lake*/River*/Water-inset constant this table used to carry. Future hydrology is a
    /// redesign from first principles - see "14 World Generation Roadmap" section
    /// "Future Hydrology / River Generation" - so do not reintroduce those constants here in
    /// anticipation of it.
    /// </remarks>
    public static class WorldScale
    {
        // --- Character (base unit) ---
        public const float CharacterHeightMeters = 1.8f;
        public const float CharacterRadiusMeters = 0.3f;

        // --- Vegetation ---
        // Target sizes in world metres, for judging whether a result looks right against the
        // 1.8 m character. They are NOT Unity prototype values: TreeInstance.heightScale and
        // DetailPrototype.minHeight are multipliers on the source prefab, so a prefab's own metre
        // size has to be divided out first. Passing one of these straight into a prototype field
        // asks for 5 % of a mesh instead of 5 cm of grass, which renders as nothing at all.
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
        /// caches soft-blend. Keeps LPB footprint near demo scale (~tile + 2x overlap), not the
        /// full world extent (world-extent biomes OOM simulation nodes).
        /// </summary>
        public const float VistaBiomeSeamOverlapMeters = 80f;

        /// <summary>Full overworld width (tileCountX x tile size).</summary>
        public static float WorldExtentXMeters => OverworldTileMeters * OverworldTileCountX;

        /// <summary>Full overworld depth (tileCountZ x tile size).</summary>
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
        /// Typical horizontal width of the coast transition, for gameplay and layout queries.
        /// Coast <em>geometry</em> is authored in IslandPlateau_TerrainGraph plus the biome falloff.
        /// </summary>
        public const float CoastShelfWidthMeters = 200f;

        /// <summary>
        /// Typical dry-sand band width above sea level (material paint guide).
        /// </summary>
        public const float BeachWidthMeters = 80f;

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

        /// <summary>Preferred peak count upper bound (1-3 natural mountains).</summary>
        public const int PeakPreferredMaxCount = 2;

        /// <summary>Minimum world-space distance between major peaks / massifs.</summary>
        public const float PeakMinSeparationMeters = 480f;
    }
}
