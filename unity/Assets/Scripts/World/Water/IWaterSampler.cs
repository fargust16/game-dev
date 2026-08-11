namespace GameDev.World.Water
{
    /// <summary>
    /// Read-only per-position water query - mirrors <c>IBiomeSampler</c>'s shape (17 Game Systems
    /// Architecture, W.2) so vegetation/POI placement can ask "is there water exactly here, and at
    /// what height" instead of assuming a single global water plane Y.
    /// </summary>
    public interface IWaterSampler
    {
        bool TryGetWaterLevel(float worldX, float worldZ, out float waterY);
    }
}
