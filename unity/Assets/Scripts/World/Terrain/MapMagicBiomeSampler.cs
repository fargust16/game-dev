using MapMagic.Terrains;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// <see cref="IBiomeSampler"/> backed by a MapMagic 2 <see cref="DirectMatricesHolder"/>
    /// (see ASSET_LIBRARY_REFERENCE.md) reading the "height" channel wired up in
    /// OverworldTerrainMenu.BuildGraph. Takes an already-resolved holder rather than searching
    /// the scene per query (DirectMatricesHolder.FindHolder/FindValueAtPosition scan every
    /// instance and are explicitly flagged "not quick" in MapMagic's own source) — the owning
    /// chunk/tile lookup (W0.5 IChunkStreamer) is responsible for resolving the right holder.
    /// </summary>
    public sealed class MapMagicBiomeSampler : IBiomeSampler
    {
        private const string HeightChannel = "height";

        private readonly DirectMatricesHolder holder;

        public MapMagicBiomeSampler(DirectMatricesHolder holder)
        {
            this.holder = holder;
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            if (holder == null || !holder.ContainsPosition(worldX, worldZ))
            {
                return 0f;
            }

            return holder.ValueAtPositionInterpolated(HeightChannel, worldX, worldZ);
        }

        public BiomeId SampleBiome(float worldX, float worldZ)
        {
            return BiomeClassifier.Classify(SampleHeight(worldX, worldZ));
        }
    }
}
