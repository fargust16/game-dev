namespace GameDev.World.Terrain
{
    /// <summary>
    /// Read-only height/biome query at a world XZ point. No mesh ownership (17 Game Systems
    /// Architecture, W.2) — implementations must stay cheap enough to call per-probe, not scan a
    /// scene.
    /// </summary>
    public interface IBiomeSampler
    {
        float SampleHeight(float worldX, float worldZ);

        BiomeId SampleBiome(float worldX, float worldZ);
    }
}
