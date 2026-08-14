namespace GameDev.World.Terrain
{
    /// <summary>
    /// Optional layers inside the shared MapMagic overworld graph.
    /// Surface / Zones / Mountains are not additive prefabs — they rebuild one graph.
    /// </summary>
    [System.Flags]
    public enum TerrainBakeFlags
    {
        None = 0,
        IncludeMountains = 1 << 0,
        IncludeZoneTextures = 1 << 1,
        Full = IncludeMountains | IncludeZoneTextures,
    }
}
