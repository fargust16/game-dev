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

        // --- Hydrology (small natural lakes on one 1400 m tile) ---
        public const float LakeRadiusMinMeters = 95f;
        public const float LakeRadiusMaxMeters = 155f;
        public const float LakeBasinDepthMinMeters = 36f;
        public const float LakeBasinDepthMaxMeters = 48f;
        public const float WaterShoreExpandMeters = 4.5f;

        // --- Macro geography ---
        public const float OverworldTileMeters = 1400f;
        public const float ChunkSizeMeters = 64f;
        public const float TerrainHeightBudgetMeters = 220f;
    }
}
