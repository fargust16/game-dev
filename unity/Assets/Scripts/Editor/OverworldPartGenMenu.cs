#if UNITY_EDITOR
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Part-Gen menu: Generate/Clear each pipeline step via the same methods as Generate All.
    /// </summary>
    public static class OverworldPartGenMenu
    {
        private const int BasePriority = 100;

        [MenuItem("GameDev/Overworld/Part-Gen/Surface/Generate", false, BasePriority)]
        public static void SurfaceGenerate()
        {
            var seed = OverworldTerrainMenu.LastSeed != 0
                ? OverworldTerrainMenu.LastSeed
                : OverworldTerrainMenu.DefaultSeed;
            Debug.Log(
                $"[Part-Gen] Surface Generate uses seed={seed} (deterministic). " +
                "Same seed → same island. For a new shape: Part-Gen/Surface/Generate Random Seed.");
            OverworldPipeline.GenerateStep(OverworldPipelineStepId.Surface);
        }

        [MenuItem("GameDev/Overworld/Part-Gen/Surface/Generate Random Seed", false, BasePriority)]
        public static void SurfaceGenerateRandom()
        {
            var seed = System.Environment.TickCount;
            Debug.Log($"[Part-Gen] Surface Generate Random Seed seed={seed}");
            OverworldTerrainMenu.GenerateSurface(seed, TerrainBakeFlags.None);
        }

        [MenuItem("GameDev/Overworld/Part-Gen/Surface/Clear", false, BasePriority + 1)]
        public static void SurfaceClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Surface);

        [MenuItem("GameDev/Overworld/Part-Gen/Zones/Generate", false, BasePriority + 10)]
        public static void ZonesGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Zones);

        [MenuItem("GameDev/Overworld/Part-Gen/Zones/Clear", false, BasePriority + 11)]
        public static void ZonesClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Zones);

        [MenuItem("GameDev/Overworld/Part-Gen/Mountains/Generate", false, BasePriority + 20)]
        public static void MountainsGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Mountains);

        [MenuItem("GameDev/Overworld/Part-Gen/Mountains/Clear", false, BasePriority + 21)]
        public static void MountainsClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Mountains);

        [MenuItem("GameDev/Overworld/Part-Gen/Markers/Generate", false, BasePriority + 40)]
        public static void MarkersGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Markers);

        [MenuItem("GameDev/Overworld/Part-Gen/Markers/Clear", false, BasePriority + 41)]
        public static void MarkersClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Markers);

        [MenuItem("GameDev/Overworld/Part-Gen/Settlements/Generate", false, BasePriority + 50)]
        public static void SettlementsGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Settlements);

        [MenuItem("GameDev/Overworld/Part-Gen/Settlements/Clear", false, BasePriority + 51)]
        public static void SettlementsClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Settlements);

        [MenuItem("GameDev/Overworld/Part-Gen/Roads/Generate", false, BasePriority + 60)]
        public static void RoadsGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Roads);

        [MenuItem("GameDev/Overworld/Part-Gen/Roads/Clear", false, BasePriority + 61)]
        public static void RoadsClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Roads);

        [MenuItem("GameDev/Overworld/Part-Gen/Trees/Generate", false, BasePriority + 80)]
        public static void TreesGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Trees);

        [MenuItem("GameDev/Overworld/Part-Gen/Trees/Clear", false, BasePriority + 81)]
        public static void TreesClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Trees);

        [MenuItem("GameDev/Overworld/Part-Gen/Undergrowth/Generate", false, BasePriority + 90)]
        public static void UndergrowthGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Undergrowth);

        [MenuItem("GameDev/Overworld/Part-Gen/Undergrowth/Clear", false, BasePriority + 91)]
        public static void UndergrowthClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Undergrowth);

        [MenuItem("GameDev/Overworld/Part-Gen/Grass/Generate", false, BasePriority + 100)]
        public static void GrassGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Grass);

        [MenuItem("GameDev/Overworld/Part-Gen/Grass/Clear", false, BasePriority + 101)]
        public static void GrassClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Grass);

        [MenuItem("GameDev/Overworld/Part-Gen/Player/Generate", false, BasePriority + 110)]
        public static void PlayerGenerate() => OverworldPipeline.GenerateStep(OverworldPipelineStepId.Player);

        [MenuItem("GameDev/Overworld/Part-Gen/Player/Clear", false, BasePriority + 111)]
        public static void PlayerClear() => OverworldPipeline.ClearStep(OverworldPipelineStepId.Player);
    }
}
#endif
