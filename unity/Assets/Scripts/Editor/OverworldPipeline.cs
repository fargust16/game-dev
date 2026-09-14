#if UNITY_EDITOR
using System.Collections;
using GameDev.World;
using GameDev.World.Terrain;
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

        /// <summary>True while the Generate All coroutine is in flight.</summary>
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
            Debug.Log("[Overworld] Generate All lock cleared.");
        }

        public static readonly OverworldPipelineStepId[] OrderedSteps =
        {
            OverworldPipelineStepId.Surface,
            OverworldPipelineStepId.Mountains,
            OverworldPipelineStepId.Hydrology,
            OverworldPipelineStepId.Markers,
            OverworldPipelineStepId.Settlements,
            OverworldPipelineStepId.Roads,
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
            SettlementOverworldMenu.ClearSettlement();
            RoadNetworkMenu.ClearRoadNetwork();
            PoiMarkerMenu.ClearMarkers();
            HydrologyMenu.Clear();
            WaterZoneMenu.ClearWaterZones();
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
                    "[Overworld] Generate All already running — wait for it to finish, or use " +
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

                // Surface and Mountains are one terrain bake: the Mountains toggle only decides whether
                // TerrainGen's massif uplift is part of the relief.
                var bakeTerrain = settings.RunSurface || settings.RunMountains;
                if (bakeTerrain)
                {
                    var flags = TerrainBakeFlags.None;
                    if (settings.RunMountains)
                    {
                        flags |= TerrainBakeFlags.IncludeMountains;
                    }

                    var surfaceSw = OverworldGenTiming.Start();
                    yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, flags);
                    OverworldGenTiming.Log("surface+mountains", OverworldGenTiming.StopMs(surfaceSw));
                }

                // Hydrology has to follow the terrain and precede everything placed on it: markers,
                // settlements and roads all read water through IWaterSampler, and the carve moves
                // the ground they are placed against.
                if (settings.RunHydrology && bakeTerrain)
                {
                    var flags = TerrainBakeFlags.None;
                    if (settings.RunMountains)
                    {
                        flags |= TerrainBakeFlags.IncludeMountains;
                    }

                    yield return HydrologyMenu.GenerateRoutine(
                        seed, HydrologyMenu.ActiveSettings, flags);
                }
                else if (settings.RunHydrology)
                {
                    Debug.LogWarning(
                        "[Hydrology] Skipped: the step reads drainage off a finished bake, so it "
                        + "cannot run when the terrain step is off.");
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

                // Canopy and ground cover are produced by the Surface step, which writes Unity tree
                // instances and detail layers during the terrain bake. There is no separate tree pass.
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
                    // Honour the Mountains checkbox rather than silently dropping the massifs:
                    // re-baking the surface on its own should not change what the world contains.
                    OverworldTerrainMenu.GenerateSurface(
                        OverworldTerrainMenu.LastSeed != 0
                            ? OverworldTerrainMenu.LastSeed
                            : OverworldTerrainMenu.DefaultSeed,
                        settings.RunMountains
                            ? TerrainBakeFlags.IncludeMountains
                            : TerrainBakeFlags.None);
                    break;
                case OverworldPipelineStepId.Mountains:
                    OverworldTerrainMenu.GenerateSurface(
                        OverworldTerrainMenu.LastSeed != 0
                            ? OverworldTerrainMenu.LastSeed
                            : OverworldTerrainMenu.DefaultSeed,
                        CurrentBakeFlags() | TerrainBakeFlags.IncludeMountains);
                    break;
                case OverworldPipelineStepId.Hydrology:
                    HydrologyMenu.GenerateMenu();
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
                case OverworldPipelineStepId.Mountains:
                    // Cascade: a height change invalidates everything derived from it.
                    ClearDownstreamOfTerrain();
                    if (step == OverworldPipelineStepId.Mountains)
                    {
                        // Clearing only the massifs means re-baking the island without them,
                        // not tearing the terrain down.
                        OverworldTerrainMenu.GenerateSurface(
                            OverworldTerrainMenu.LastSeed != 0
                                ? OverworldTerrainMenu.LastSeed
                                : OverworldTerrainMenu.DefaultSeed,
                            CurrentBakeFlags() & ~TerrainBakeFlags.IncludeMountains);
                    }
                    else
                    {
                        OverworldTerrainMenu.ClearTerrainOnly();
                    }

                    break;
                case OverworldPipelineStepId.Hydrology:
                    HydrologyMenu.Clear();
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
                case OverworldPipelineStepId.Player:
                    OverworldPlayerMenu.ClearPlayer();
                    break;
            }
        }

        private static TerrainBakeFlags CurrentBakeFlags()
        {
            return OverworldTerrainMenu.LastBakeFlags;
        }

        private static void ClearDownstreamOfTerrain()
        {
            OverworldPlayerMenu.ClearPlayer();
            SettlementOverworldMenu.ClearSettlement();
            RoadNetworkMenu.ClearRoadNetwork();
            PoiMarkerMenu.ClearMarkers();
            HydrologyMenu.Clear();
            WaterZoneMenu.ClearWaterZones();
        }
    }
}
#endif
