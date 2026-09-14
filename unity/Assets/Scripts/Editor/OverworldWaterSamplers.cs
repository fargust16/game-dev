#if UNITY_EDITOR
using GameDev.World;
using GameDev.World.Water;

namespace GameDev.EditorTools
{
    /// <summary>
    /// The one place the generation steps build their water query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Roads, settlements and POI placement all ask the same question — "is there water here, and at
    /// what height" — through <see cref="IWaterSampler"/>. Composing the ocean with the generated
    /// inland water here means none of them has to learn that rivers exist, and there is no second
    /// water-query system for inland water to grow into.
    /// </para>
    /// <para>
    /// Inland water comes from whatever <c>WorldDefinition</c> currently holds, so a world generated
    /// without hydrology composes to exactly the ocean sampler it used before.
    /// </para>
    /// </remarks>
    public static class OverworldWaterSamplers
    {
        /// <summary>
        /// Ocean plus this world's rivers and lakes.
        /// </summary>
        /// <param name="terrain">
        /// The terrain the ocean sampler reads heights from. Note that
        /// <c>SeaLevelOceanSampler.FromTerrain</c> closes over this single tile, which is a
        /// pre-existing limitation recorded in <c>HYDROLOGY_GENERATION_PLAN.md</c> §19 — inland
        /// water is indexed in world space and is not affected by it.
        /// </param>
        public static IWaterSampler ForCurrentWorld(UnityEngine.Terrain terrain)
        {
            var ocean = SeaLevelOceanSampler.FromTerrain(terrain);
            var hydrology = OverworldWorldState.Find()?.Definition?.Hydrology;
            if (hydrology == null || hydrology.IsEmpty)
            {
                return ocean;
            }

            return new CompositeWaterSampler(ocean, new InlandWaterSampler(hydrology));
        }
    }
}
#endif
