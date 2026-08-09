using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// V0 height-band classification (17 Game Systems Architecture, W.2: "2-4 biome ids enough").
    /// Pure function of a 0..1 height value — no moisture/temperature axis yet, so Forest vs.
    /// Swamp at similar elevation isn't distinguished; that's a later-wave refinement, not V0.
    /// </summary>
    public static class BiomeClassifier
    {
        public const float SwampMaxHeight = 0.15f;
        public const float MeadowMaxHeight = 0.35f;
        public const float ForestMaxHeight = 0.65f;

        public static BiomeId Classify(float height01)
        {
            var h = Mathf.Clamp01(height01);

            if (h < SwampMaxHeight)
            {
                return BiomeId.Swamp;
            }

            if (h < MeadowMaxHeight)
            {
                return BiomeId.Meadow;
            }

            if (h < ForestMaxHeight)
            {
                return BiomeId.Forest;
            }

            return BiomeId.Mountain;
        }
    }
}
