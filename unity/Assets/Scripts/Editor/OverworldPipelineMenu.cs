#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One-button pipeline: runs every Overworld generation step in sequence (terrain+water, POI,
    /// settlement, roads, vegetation), in dependency order (POI needs biome/height; settlement needs
    /// TownApproach; roads route to the real settlement anchor; vegetation runs last so it can soft-
    /// clear around roads/POIs/settlements via <see cref="GameDev.World.Vegetation.VegetationClearance"/>).
    /// WORLD_GENERATION_PIPELINE.md's iteration list is expected to keep growing - each new
    /// Iteration's own menu item should get one more call appended to <see cref="RunRemainingSteps"/>
    /// (and, if it adds a Clear* counterpart, to <see cref="ClearFullOverworld"/> too) so this stays
    /// the single "give me the latest, everything-wired-up demo" action instead of drifting out of
    /// sync with what the individual per-step menus can do.
    /// </summary>
    public static class OverworldPipelineMenu
    {
        [MenuItem("GameDev/Overworld/Build Full Overworld (Random Seed)")]
        public static void BuildFullRandomSeed()
        {
            var total = OverworldGenTiming.Start();
            OverworldGenTiming.Time("terrain+water", OverworldTerrainMenu.GenerateRandomSeed);
            RunRemainingSteps();
            OverworldGenTiming.Log("full-overworld", OverworldGenTiming.StopMs(total), "random seed");
        }

        [MenuItem("GameDev/Overworld/Build Full Overworld (Default Seed)")]
        public static void BuildFullDefaultSeed()
        {
            var total = OverworldGenTiming.Start();
            OverworldGenTiming.Time("terrain+water", OverworldTerrainMenu.GenerateFromSeed);
            RunRemainingSteps();
            OverworldGenTiming.Log("full-overworld", OverworldGenTiming.StopMs(total), "default seed");
        }

        // Terrain generation (OverworldTerrainMenu.GenerateInternal) clears prior derived output and
        // already runs water (lakes + rivers + carve + meshes) before returning. Settlement must run
        // before Roads (anchor displacement). Vegetation runs after both so clearance falloff can
        // read OverworldRoads / OverworldSettlement / OverworldPoi.
        private static void RunRemainingSteps()
        {
            OverworldGenTiming.Time("poi", PoiMarkerMenu.PlaceMarkers);
            OverworldGenTiming.Time("settlement", SettlementOverworldMenu.PlaceSettlementAtTownApproach);
            OverworldGenTiming.Time("roads", RoadNetworkMenu.BuildRoadNetwork);
            OverworldGenTiming.Time("vegetation", VegetationScatterMenu.ScatterVegetation);
        }

        [MenuItem("GameDev/Overworld/Clear Full Overworld")]
        public static void ClearFullOverworld()
        {
            // ClearTerrain already cascades Water/Vegetation/POI/Roads/Settlement (kept in one place
            // in OverworldTerrainMenu so GenerateInternal and this button can't drift apart).
            SettlementOverworldMenu.ClearSettlement();
            OverworldTerrainMenu.ClearTerrain();
        }
    }
}
#endif
