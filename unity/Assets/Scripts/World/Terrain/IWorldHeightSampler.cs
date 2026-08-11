namespace GameDev.World.Terrain
{
    /// <summary>
    /// World-space ground Y at XZ. Distinct from <see cref="IBiomeSampler.SampleHeight"/>,
    /// which returns a normalized 0–1 channel — not meters.
    /// </summary>
    public interface IWorldHeightSampler
    {
        bool TrySampleWorldY(float worldX, float worldZ, out float worldY);
    }
}
