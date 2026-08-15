namespace GameDev.World.Terrain
{
    /// <summary>
    /// Optional layers inside the shared Vista overworld bake.
    /// Surface / Zones / Mountains rebuild one Vista pass (not additive prefabs).
    /// </summary>
    [System.Flags]
    public enum TerrainBakeFlags
    {
        None = 0,
        IncludeMountains = 1 << 0,
        IncludeZoneTextures = 1 << 1,
        /// <summary>
        /// Diagnostic: bake Vista sample MountainRange onto 2×2 with no C# framing/lakes/stamps.
        /// </summary>
        VistaDemoAsIs = 1 << 2,
        Full = IncludeMountains | IncludeZoneTextures,
    }
}
