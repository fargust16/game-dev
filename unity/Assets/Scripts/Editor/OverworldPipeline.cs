#if UNITY_EDITOR
using System.Collections;
using GameDev.World;
using GameDev.World.Terrain;
using Pinwheel.Vista;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Executable overworld pipeline: each step Generate/Clear, skippable via
    /// <see cref="WorldGenSettings"/> step flags. Same <see cref="WorldSeed"/> forks per step.
    /// </summary>
    public static class OverworldPipeline
    {
        private static EditorCoroutine s_runningGenerate;

        /// <summary>True while Generate All coroutine is in flight (Vista wait included).</summary>
        public static bool IsGenerateAllRunning => s_runningGenerate != null;

        [InitializeOnLoadMethod]
        private static void ResetGenerateLockOnDomainReload()
        {
            s_runningGenerate = null;
        }

        [MenuItem("GameDev/Overworld/Cancel Generate All Lock", false, 3)]
        public static void CancelGenerateAllLock()
        {
            s_runningGenerate = null;
            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager != null)
            {
                manager.CancelGeneration();
            }

            Debug.Log("[Overworld] Generate All lock cleared.");
        }

        public static readonly OverworldPipelineStepId[] OrderedSteps =
        {
            OverworldPipelineStepId.Surface,
            OverworldPipelineStepId.Zones,
            OverworldPipelineStepId.Mountains,
            OverworldPipelineStepId.Markers,
            OverworldPipelineStepId.Settlements,
            OverworldPipelineStepId.Roads,
            OverworldPipelineStepId.Trees,
            OverworldPipelineStepId.Undergrowth,
            OverworldPipelineStepId.Grass,
            OverworldPipelineStepId.Player,
        };

        [MenuItem("GameDev/Overworld/Generate All", false, 0)]
        public static void GenerateAllMenuRandom()
        {
            GenerateAll(System.Environment.TickCount, OverworldTerrainMenu.ActiveSettings);
        }

        [MenuItem("GameDev/Overworld/Generate All (Default Seed)", false, 1)]
        public static void GenerateAllMenuDefault()
        {
            GenerateAll(OverworldTerrainMenu.DefaultSeed, OverworldTerrainMenu.ActiveSettings);
        }

        [MenuItem("GameDev/Overworld/Clear All", false, 2)]
        public static void ClearAll()
        {
            var state = OverworldWorldState.Find();
            if (state != null)
            {
                state.Clear();
            }

            OverworldPlayerMenu.ClearPlayer();
            VegetationScatterMenu.ClearVegetation();
            GrassBakeMenu.Clear();
            SettlementOverworldMenu.ClearSettlement();
            RoadNetworkMenu.ClearRoadNetwork();
            PoiMarkerMenu.ClearMarkers();
            WaterZoneMenu.ClearWaterZones();
            WaterZoneMenu.ClearRiverZones();
            WaterZoneMenu.EnsureOceanOcWPlane();
            OverworldTerrainMenu.ClearTerrainOnly();
            CancelGenerateAllLock();
        }

        /// <summary>Entry used by World Generator window and menu shortcuts.</summary>
        public static void GenerateAll(int seed, WorldGenSettings settings)
        {
            if (s_runningGenerate != null)
            {
                Debug.LogWarning(
                    "[Overworld] Generate All already running — wait for Vista, or use " +
                    "GameDev/Overworld/Cancel Generate All Lock if it is stuck.");
                return;
            }

            s_runningGenerate = EditorCoroutineUtility.StartCoroutineOwnerless(GenerateAllRoutine(seed, settings));
        }

        private static IEnumerator GenerateAllRoutine(int seed, WorldGenSettings settings)
        {
            try
            {
                settings.Clamp();
                OverworldTerrainMenu.ActiveSettings = settings;
                var state = OverworldWorldState.FindOrCreate();
                state.SetDefinition(new WorldDefinition
                {
                    Seed = seed,
                    Settings = settings,
                });

                var total = OverworldGenTiming.Start();

                // Surface+Zones+Mountains share one Vista terrain bake when any of them run.
                var bakeTerrain = settings.RunSurface || settings.RunZones || settings.RunMountains;
                if (bakeTerrain)
                {
                    var flags = TerrainBakeFlags.None;
                    if (settings.RunMountains)
                    {
                        flags |= TerrainBakeFlags.IncludeMountains;
                    }

                    if (settings.RunZones)
                    {
                        flags |= TerrainBakeFlags.IncludeZoneTextures;
                    }

                    var surfaceSw = OverworldGenTiming.Start();
                    yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);
                    OverworldGenTiming.Log("surface+zones+mountains", OverworldGenTiming.StopMs(surfaceSw));
                }

                if (settings.RunMarkers)
                {
                    OverworldGenTiming.Time("markers.plan", () => OverworldWorldPlan.BuildAndStorePois());
                }

                if (settings.RunSettlements)
                {
                    OverworldGenTiming.Time("settlement", SettlementOverworldMenu.PlaceSettlementAtTownApproach);
                }

                if (settings.RunRoads)
                {
                    OverworldGenTiming.Time("roads+deform", RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner);
                }
                WaterZoneMenu.EnsureOceanOcWPlane();

                if (settings.RunMarkers)
                {
                    OverworldGenTiming.Time("markers.visuals", PoiMarkerMenu.PlaceMarkers);
                }

                if (settings.RunTrees)
                {
                    OverworldGenTiming.Time("trees", VegetationScatterMenu.ScatterTrees);
                }

                if (settings.RunUndergrowth)
                {
                    OverworldGenTiming.Time("undergrowth", VegetationScatterMenu.ScatterUndergrowth);
                }

                if (settings.RunGrass)
                {
                    OverworldGenTiming.Time("grass", VegetationScatterMenu.BakeGrassOnly);
                }

                if (settings.RunPlayer)
                {
                    OverworldGenTiming.Time("player", OverworldPlayerMenu.SpawnPlayerAtTown);
                }

                OverworldGenTiming.Log("full-overworld", OverworldGenTiming.StopMs(total), $"seed={seed}");
            }
            finally
            {
                s_runningGenerate = null;
            }
        }

        public static void GenerateStep(OverworldPipelineStepId step)
        {
            var settings = OverworldTerrainMenu.ActiveSettings;
            settings.Clamp();
            switch (step)
            {
                case OverworldPipelineStepId.Surface:
                    OverworldTerrainMenu.GenerateSurface(
                        OverworldTerrainMenu.LastSeed != 0
                            ? OverworldTerrainMenu.LastSeed
                            : OverworldTerrainMenu.DefaultSeed,
                        TerrainBakeFlags.None);
                    break;
                case OverworldPipelineStepId.Zones:
                    OverworldTerrainMenu.GenerateSurface(
                        OverworldTerrainMenu.LastSeed != 0
                            ? OverworldTerrainMenu.LastSeed
                            : OverworldTerrainMenu.DefaultSeed,
                        CurrentBakeFlags() | TerrainBakeFlags.IncludeZoneTextures);
                    break;
                case OverworldPipelineStepId.Mountains:
                    OverworldTerrainMenu.GenerateSurface(
                        OverworldTerrainMenu.LastSeed != 0
                            ? OverworldTerrainMenu.LastSeed
                            : OverworldTerrainMenu.DefaultSeed,
                        CurrentBakeFlags() | TerrainBakeFlags.IncludeMountains);
                    break;
                case OverworldPipelineStepId.Markers:
                    PoiMarkerMenu.PlaceMarkers();
                    break;
                case OverworldPipelineStepId.Settlements:
                    SettlementOverworldMenu.PlaceSettlementAtTownApproach();
                    break;
                case OverworldPipelineStepId.Roads:
                    RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner();
                    break;
                case OverworldPipelineStepId.Trees:
                    VegetationScatterMenu.ScatterTrees();
                    break;
                case OverworldPipelineStepId.Undergrowth:
                    VegetationScatterMenu.ScatterUndergrowth();
                    break;
                case OverworldPipelineStepId.Grass:
                    VegetationScatterMenu.BakeGrassOnly();
                    break;
                case OverworldPipelineStepId.Player:
                    OverworldPlayerMenu.SpawnPlayerAtTown();
                    break;
            }
        }

        public static void ClearStep(OverworldPipelineStepId step)
        {
            switch (step)
            {
                case OverworldPipelineStepId.Surface:
                case OverworldPipelineStepId.Zones:
                case OverworldPipelineStepId.Mountains:
                    // Cascade: height change invalidates everything derived.
                    ClearDownstreamOfTerrain();
                    if (step == OverworldPipelineStepId.Mountains)
                    {
                        OverworldTerrainMenu.GenerateSurface(
                            OverworldTerrainMenu.LastSeed != 0
                                ? OverworldTerrainMenu.LastSeed
                                : OverworldTerrainMenu.DefaultSeed,
                            CurrentBakeFlags() & ~TerrainBakeFlags.IncludeMountains);
                    }
                    else if (step == OverworldPipelineStepId.Zones)
                    {
                        OverworldTerrainMenu.GenerateSurface(
                            OverworldTerrainMenu.LastSeed != 0
                                ? OverworldTerrainMenu.LastSeed
                                : OverworldTerrainMenu.DefaultSeed,
                            CurrentBakeFlags() & ~TerrainBakeFlags.IncludeZoneTextures);
                    }
                    else
                    {
                        OverworldTerrainMenu.ClearTerrainOnly();
                    }

                    break;
                case OverworldPipelineStepId.Markers:
                    PoiMarkerMenu.ClearMarkers();
                    break;
                case OverworldPipelineStepId.Settlements:
                    SettlementOverworldMenu.ClearSettlement();
                    break;
                case OverworldPipelineStepId.Roads:
                    RoadNetworkMenu.ClearRoadNetwork();
                    break;
                case OverworldPipelineStepId.Trees:
                    VegetationScatterMenu.ClearTrees();
                    break;
                case OverworldPipelineStepId.Undergrowth:
                    VegetationScatterMenu.ClearUndergrowth();
                    break;
                case OverworldPipelineStepId.Grass:
                    GrassBakeMenu.Clear();
                    break;
                case OverworldPipelineStepId.Player:
                    OverworldPlayerMenu.ClearPlayer();
                    break;
            }
        }

        private static TerrainBakeFlags CurrentBakeFlags()
        {
            return OverworldTerrainMenu.LastBakeFlags;
        }        private static void ClearDownstreamOfTerrain()
        {
            OverworldPlayerMenu.ClearPlayer();
            VegetationScatterMenu.ClearVegetation();
            GrassBakeMenu.Clear();
            SettlementOverworldMenu.ClearSettlement();
            RoadNetworkMenu.ClearRoadNetwork();
            PoiMarkerMenu.ClearMarkers();
            WaterZoneMenu.ClearWaterZones();
            WaterZoneMenu.ClearRiverZones();
        }
    }
}
#endif
