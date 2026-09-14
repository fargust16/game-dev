namespace GameDev.World.Terrain
{
    /// <summary>
    /// What the overworld terrain bake includes.
    /// </summary>
    /// <remarks>
    /// Surface and Mountains are one terrain bake, and this flag selects whether TerrainGen's massif
    /// uplift is part of the relief. Baking without <see cref="IncludeMountains"/> gives the island
    /// alone, which is the reference surface for terrain diagnostics.
    /// </remarks>
    [System.Flags]
    public enum TerrainBakeFlags
    {
        None = 0,

        /// <summary>Include TerrainGen's accepted massifs in the relief.</summary>
        IncludeMountains = 1 << 0,

        Full = IncludeMountains,
    }
}
