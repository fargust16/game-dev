#if UNITY_EDITOR
using GameDev.World;
using UnityEditor;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Legacy menu aliases that forward into <see cref="OverworldPipeline"/>.
    /// Prefer Generate All / Clear All / Part-Gen.
    /// </summary>
    public static class OverworldPipelineMenu
    {
        [MenuItem("GameDev/Overworld/Build Full Overworld (Random Seed)", false, 20)]
        public static void BuildFullRandomSeed()
        {
            OverworldPipeline.GenerateAll(System.Environment.TickCount, OverworldTerrainMenu.ActiveSettings);
        }

        [MenuItem("GameDev/Overworld/Build Full Overworld (Default Seed)", false, 21)]
        public static void BuildFullDefaultSeed()
        {
            OverworldPipeline.GenerateAll(OverworldTerrainMenu.DefaultSeed, OverworldTerrainMenu.ActiveSettings);
        }

        /// <summary>Entry used by World Generator window and menu shortcuts.</summary>
        public static void BuildFull(int seed, WorldGenSettings settings)
        {
            OverworldPipeline.GenerateAll(seed, settings);
        }

        [MenuItem("GameDev/Overworld/Clear Full Overworld", false, 22)]
        public static void ClearFullOverworld()
        {
            OverworldPipeline.ClearAll();
        }
    }
}
#endif
