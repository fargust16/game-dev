namespace GameDev.World.Terrain
{
    /// <summary>
    /// Which biomes take part in the shared Vista overworld bake.
    /// </summary>
    /// <remarks>
    /// Surface and Mountains are not additive prefabs: they are one Vista pass, and this flag
    /// selects whether the massif LPBs are spawned into it. Baking without
    /// <see cref="IncludeMountains"/> gives the island alone, which is the reference bake for
    /// terrain diagnostics.
    /// </remarks>
    [System.Flags]
    public enum TerrainBakeFlags
    {
        None = 0,

        /// <summary>Spawn the seeded <c>LPB_Mountain_n</c> massifs on top of the island.</summary>
        IncludeMountains = 1 << 0,

        /// <summary>
        /// Diagnostic: bake Vista's sample MountainRange graph onto the 2×2 grid, with none of our
        /// own biomes. Useful only for comparing our graphs against the vendor reference.
        /// </summary>
        VistaDemoAsIs = 1 << 1,

        Full = IncludeMountains,
    }
}
