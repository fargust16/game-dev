#if UNITY_EDITOR
using Den.Tools;
using GameDev.World;
using GameDev.World.Terrain;
using MapMagic.Core;
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
        public static readonly OverworldPipelineStepId[] OrderedSteps =
        {
            OverworldPipelineStepId.Surface,
            OverworldPipelineStepId.Zones,
            OverworldPipelineStepId.Mountains,
            OverworldPipelineStepId.Lakes,
            OverworldPipelineStepId.Markers,
            OverworldPipelineStepId.Settlements,
            OverworldPipelineStepId.Roads,
            OverworldPipelineStepId.WaterMeshes,
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
            OverworldTerrainMenu.ClearTerrainOnly();
        }

        /// <summary>Entry used by World Generator window and menu shortcuts.</summary>
        public static void GenerateAll(int seed, WorldGenSettings settings)
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

            // Surface+Zones+Mountains share one MapMagic bake when any of them run.
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

                // Surface height is always required when mountains/zones bake.
                OverworldGenTiming.Time("surface+zones+mountains", () =>
                    OverworldTerrainMenu.GenerateSurface(seed, flags));
            }

            if (settings.RunLakes)
            {
                OverworldGenTiming.Time("lakes.plan", GenerateLakesPlan);
            }

            if (settings.RunMarkers)
            {
                OverworldGenTiming.Time("markers.plan", () =>
                {
                    var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
                    if (mapMagic != null)
                    {
                        OverworldWorldPlan.BuildAndStorePois(mapMagic, new Coord(0, 0));
                    }
                });
            }

            if (settings.RunSettlements)
            {
                OverworldGenTiming.Time("settlement", SettlementOverworldMenu.PlaceSettlementAtTownApproach);
            }

            if (settings.RunRoads)
            {
                OverworldGenTiming.Time("roads+deform", RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner);
            }

            if (settings.RunWaterMeshes)
            {
                OverworldGenTiming.Time("water.meshes", () =>
                {
                    var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
                    if (mapMagic != null)
                    {
                        WaterZoneMenu.ApplyDeformAndMeshes(
                            mapMagic, new Coord(0, 0), carveLakes: false, carveRivers: true);
                    }
                });
            }

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
                case OverworldPipelineStepId.Lakes:
                    // Part-Gen lakes: place + carve + mesh for quick iteration.
                    GenerateLakesStandalone();
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
                case OverworldPipelineStepId.WaterMeshes:
                {
                    var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
                    if (mapMagic != null)
                    {
                        WaterZoneMenu.ApplyDeformAndMeshes(
                            mapMagic, new Coord(0, 0), carveLakes: false, carveRivers: true);
                    }

                    break;
                }
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
                case OverworldPipelineStepId.Lakes:
                    WaterZoneMenu.ClearWaterZones();
                    WaterZoneMenu.ClearRiverZones();
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
                case OverworldPipelineStepId.WaterMeshes:
                    WaterZoneMenu.ClearWaterMeshesOnly();
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
        }

        private static void GenerateLakesPlan()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            WaterZoneMenu.GenerateForTile(mapMagic, new Coord(0, 0), deferTerrainDeformAndMeshes: true);
        }

        private static void GenerateLakesStandalone()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            if (mapMagic == null || mapMagic.graph == null)
            {
                return;
            }

            WaterZoneMenu.GenerateForTile(mapMagic, new Coord(0, 0), deferTerrainDeformAndMeshes: false);
        }

        private static void ClearDownstreamOfTerrain()
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
