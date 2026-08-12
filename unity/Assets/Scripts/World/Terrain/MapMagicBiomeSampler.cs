using MapMagic.Terrains;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// <see cref="IBiomeSampler"/> backed by a MapMagic 2 <see cref="DirectMatricesHolder"/>
    /// (see ASSET_LIBRARY_REFERENCE.md) reading the "height", "slope", and "moisture" channels
    /// wired up in OverworldTerrainMenu.BuildGraph. Takes an already-resolved holder rather than
    /// searching the scene per query (DirectMatricesHolder.FindHolder/FindValueAtPosition scan
    /// every instance and are explicitly flagged "not quick" in MapMagic's own source) — the owning
    /// chunk/tile lookup (W0.5 IChunkStreamer) is responsible for resolving the right holder.
    /// </summary>
    public sealed class MapMagicBiomeSampler : IBiomeSampler, ITerrainSlopeSampler
    {
        private const string HeightChannel = "height";
        private const string SlopeChannel = "slope";
        private const string MoistureChannel = "moisture";

        private readonly DirectMatricesHolder holder;

        public MapMagicBiomeSampler(DirectMatricesHolder holder)
        {
            this.holder = holder;
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            return SampleChannel(HeightChannel, worldX, worldZ);
        }

        public float SampleSlope01(float worldX, float worldZ)
        {
            return SampleChannel(SlopeChannel, worldX, worldZ);
        }

        public BiomeId SampleBiome(float worldX, float worldZ)
        {
            var height = SampleHeight(worldX, worldZ);
            var slope = SampleSlope01(worldX, worldZ);
            var moisture = SampleMoisture01(worldX, worldZ);
            return BiomeClassifier.Classify(height, slope, moisture);
        }

        public float SampleMoisture01(float worldX, float worldZ)
        {
            return SampleChannel(MoistureChannel, worldX, worldZ);
        }

        private float SampleChannel(string channel, float worldX, float worldZ)
        {
            if (holder == null || !holder.ContainsPosition(worldX, worldZ))
            {
                return 0f;
            }

            return holder.ValueAtPositionInterpolated(channel, worldX, worldZ);
        }
    }
}
