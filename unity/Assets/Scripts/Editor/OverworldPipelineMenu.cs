#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One-button pipeline: terrain+water, then settlement → roads → POI markers → vegetation.
    /// Settlement/roads recompute the same seeded <c>StubPoiPlacer</c> quota (no scene POI needed).
    /// Visual POI markers land immediately before vegetation so flora can hard-clear a pocket around
    /// each marker via <see cref="GameDev.World.Vegetation.VegetationClearance"/>. Vegetation is
    /// always last. WORLD_GENERATION_PIPELINE.md's iteration list is expected to keep growing - each
    /// new Iteration's own menu item should get one more call in <see cref="RunRemainingSteps"/>
    /// (and Clear* in <see cref="ClearFullOverworld"/> if needed) so this stays the single full-demo
    /// action.
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

        // Terrain generation clears prior derived output and builds water before returning.
        // Settlement before roads (anchor displacement). POI visuals penultimate; vegetation last
        // so clearance reads OverworldRoads / OverworldSettlement / OverworldPoi.
        private static void RunRemainingSteps()
        {
            OverworldGenTiming.Time("settlement", SettlementOverworldMenu.PlaceSettlementAtTownApproach);
            OverworldGenTiming.Time("roads", RoadNetworkMenu.BuildRoadNetwork);
            OverworldGenTiming.Time("poi", PoiMarkerMenu.PlaceMarkers);
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
