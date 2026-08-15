using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// LEGACY radial pancake relief — replaced by <see cref="IslandNaturalGeography"/>.
    /// Kept so old references compile; do not call from the bake pipeline.
    /// </summary>
    [System.Obsolete("Use IslandNaturalGeography.Apply instead.")]
    public static class IslandBaseRelief
    {
        public static void Apply(UnityEngine.Terrain terrain, int seed, bool includeMountains)
        {
            // Forward to the natural geography path so accidental calls are not pancakes.
            IslandNaturalGeography.Apply(terrain, seed, includeMountains);
        }
    }
}
