#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Vegetation Studio Pro is deprecated for this project.
    /// </summary>
    /// <remarks>
    /// Grass and canopy placement both belong to the Vista biome graphs — Detail Density Output for
    /// grass, Tree Output for canopy — and are rendered by Unity's own terrain detail/tree passes.
    /// There is no separate grass bake and no second placement system.
    /// Rowlan VSP Extensions stay quarantined under Assets/_Disabled/.
    /// </remarks>
    public static class OverworldVspIntegration
    {
        [MenuItem("GameDev/Overworld/Vegetation/VSP Status (Deprecated)")]
        public static void VspDeprecatedStatusMenu()
        {
            Debug.LogWarning(
                "[Overworld] Vegetation Studio Pro is deprecated here.\n" +
                "Grass comes from the biome graphs' DETAIL DENSITY OUTPUT and canopy from TREE OUTPUT;\n" +
                "both are placed during the Vista bake. Tune them in the terrain graph, not in C#.\n" +
                "Shrubs and rocks come from the same graphs; there is no prefab scatter pass.\n" +
                "Rowlan VSP Extensions remain under Assets/_Disabled/ — do not restore.");
        }
    }
}
#endif
