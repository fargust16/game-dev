namespace GameDev.World.Poi
{
    /// <summary>V0 marker kinds (OVERWORLD_V0_PLAN.md W0.6, 17 Game Systems Architecture W.4).</summary>
    public enum PoiKind
    {
        Ruin,
        Camp,
        Shrine,
        TownApproach,
        CaveMouth,

        /// <summary>V0 stand-in for a distinctive per-biome point of interest (see
        /// <see cref="PoiDemoScenario"/>) - one requested per biome until dedicated per-biome
        /// landmark flavors exist.</summary>
        Landmark
    }
}
