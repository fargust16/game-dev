#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Vegetation Studio Pro is deprecated for this project.
    /// Grass SoT = Polytope Lowpoly <c>PT_Grass_02</c> via <see cref="GrassBakeMenu"/> / <see cref="GrassField"/>.
    /// Rowlan VSP Extensions stay quarantined under Assets/_Disabled/.
    /// </summary>
    public static class OverworldVspIntegration
    {
        [MenuItem("GameDev/Overworld/Vegetation/Bake Lowpoly Grass (GPU)")]
        public static void BakeLowpolyGrassMenu()
        {
            VegetationScatterMenu.BakeGrassOnly();
        }

        [MenuItem("GameDev/Overworld/Vegetation/VSP Status (Deprecated)")]
        public static void VspDeprecatedStatusMenu()
        {
            Debug.LogWarning(
                "[Overworld] Vegetation Studio Pro is deprecated here.\n" +
                "Grass SoT: Polytope Lowpoly PT_Grass_02 → GrassField (GameDev/Overworld/Grass).\n" +
                "Menus: GameDev/Overworld/Vegetation/Bake Lowpoly Grass (GPU)\n" +
                "       or full scatter via VegetationScatterMenu / Overworld pipeline.\n" +
                "Rowlan VSP Extensions remain under Assets/_Disabled/ — do not restore.");
        }
    }
}
#endif
