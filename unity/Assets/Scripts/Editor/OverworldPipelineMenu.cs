#if UNITY_EDITOR
using Den.Tools;
using GameDev.World;
using GameDev.World.Landscape;
using MapMagic.Core;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// LandscapePlanner full demo:
    /// base terrain → lake plan → POI plan → settlement → roads → joint deform → water → POI visuals → vegetation.
    /// </summary>
    public static class OverworldPipelineMenu
    {
        [MenuItem("GameDev/Overworld/Build Full Overworld (Random Seed)")]
        public static void BuildFullRandomSeed()
        {
            BuildFull(System.Environment.TickCount, OverworldTerrainMenu.ActiveSettings);
        }

        [MenuItem("GameDev/Overworld/Build Full Overworld (Default Seed)")]
        public static void BuildFullDefaultSeed()
        {
            BuildFull(OverworldTerrainMenu.DefaultSeed, OverworldTerrainMenu.ActiveSettings);
        }

        /// <summary>Entry used by World Generator window and menu shortcuts.</summary>
        public static void BuildFull(int seed, WorldGenSettings settings)
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
            OverworldGenTiming.Time("terrain+lake-plan", () =>
                OverworldTerrainMenu.GenerateTerrainAndLakePlan(seed));

            RunLandscapeSteps();
            OverworldGenTiming.Log(
                "full-overworld",
                OverworldGenTiming.StopMs(total),
                $"seed={seed}");
        }

        private static void RunLandscapeSteps()
        {
            var mapMagic = Object.FindFirstObjectByType<MapMagicObject>();
            var tileCoord = new Coord(0, 0);

            OverworldGenTiming.Time("poi-plan", () =>
            {
                if (mapMagic != null)
                {
                    OverworldWorldPlan.BuildAndStorePois(mapMagic, tileCoord);
                }
            });

            OverworldGenTiming.Time("settlement", SettlementOverworldMenu.PlaceSettlementAtTownApproach);
            OverworldGenTiming.Time("roads+deform", RoadNetworkMenu.BuildRoadNetworkWithLandscapePlanner);
            OverworldGenTiming.Time("water.meshes", () =>
            {
                if (mapMagic != null)
                {
                    WaterZoneMenu.ApplyDeformAndMeshes(
                        mapMagic, tileCoord, carveLakes: false, carveRivers: true);
                }
            });
            OverworldGenTiming.Time("poi", PoiMarkerMenu.PlaceMarkers);
            OverworldGenTiming.Time("vegetation", VegetationScatterMenu.ScatterVegetation);
        }

        [MenuItem("GameDev/Overworld/Clear Full Overworld")]
        public static void ClearFullOverworld()
        {
            var state = OverworldWorldState.Find();
            if (state != null)
            {
                state.Clear();
            }

            SettlementOverworldMenu.ClearSettlement();
            OverworldTerrainMenu.ClearTerrain();
        }
    }
}
#endif
