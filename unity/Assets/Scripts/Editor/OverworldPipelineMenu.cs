#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// One-button pipeline: runs every Overworld generation step in sequence (terrain+water, POI,
    /// vegetation, settlement, roads), in the dependency order each step already assumes elsewhere
    /// (POI needs biome/height sampling off the terrain; settlement needs a POI TownApproach; roads
    /// connect POI markers and must run last so they can route to wherever the settlement actually
    /// landed, not just the raw marker position). WORLD_GENERATION_PIPELINE.md's iteration list is
    /// expected to keep
    /// growing - each new Iteration's own menu item should get one more call appended to
    /// <see cref="RunRemainingSteps"/> (and, if it adds a Clear* counterpart, to
    /// <see cref="ClearFullOverworld"/> too) so this stays the single "give me the latest,
    /// everything-wired-up demo" action instead of drifting out of sync with what the individual
    /// per-step menus can do.
    /// </summary>
    public static class OverworldPipelineMenu
    {
        [MenuItem("GameDev/Overworld/Build Full Overworld (Random Seed)")]
        public static void BuildFullRandomSeed()
        {
            OverworldTerrainMenu.GenerateRandomSeed();
            RunRemainingSteps();
        }

        [MenuItem("GameDev/Overworld/Build Full Overworld (Default Seed)")]
        public static void BuildFullDefaultSeed()
        {
            OverworldTerrainMenu.GenerateFromSeed();
            RunRemainingSteps();
        }

        // Terrain generation (OverworldTerrainMenu.GenerateInternal) clears prior derived output and
        // already runs water (lakes + rivers + carve + meshes) before returning. Steps below layer
        // on that wet terrain so vegetation/POI/roads see CompositeWaterSampler correctly. Settlement
        // must run before Roads: the dry-ground search can move the fabric well away from its raw
        // TownApproach marker, and RoadNetworkMenu reads OverworldSettlementAnchor to route to the
        // real position instead of the original marker.
        private static void RunRemainingSteps()
        {
            PoiMarkerMenu.PlaceMarkers();
            VegetationScatterMenu.ScatterVegetation();
            SettlementOverworldMenu.PlaceSettlementAtTownApproach();
            RoadNetworkMenu.BuildRoadNetwork();
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
