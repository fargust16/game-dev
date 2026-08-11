namespace GameDev.World.Terrain
{
    /// <summary>
    /// Read-only slope query at a world XZ point, 0 (flat) to 1 (cliff) - mirrors
    /// <see cref="IBiomeSampler"/>'s shape. Split out rather than folded into IBiomeSampler because
    /// not every consumer needs raw slope (e.g. vegetation scatter only needs the biome result),
    /// while road costing needs slope directly, independent of how BiomeClassifier folds it into a
    /// biome id.
    /// </summary>
    public interface ITerrainSlopeSampler
    {
        float SampleSlope01(float worldX, float worldZ);
    }
}
