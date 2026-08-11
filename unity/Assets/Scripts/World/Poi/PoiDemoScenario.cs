using System.Collections.Generic;
using GameDev.World.Terrain;

namespace GameDev.World.Poi
{
    /// <summary>
    /// V0 demo POI quota (WORLD_GENERATION_PIPELINE.md Iteration 08): a fixed count-per-kind
    /// request list for <see cref="StubPoiPlacer"/>'s quota constructor. Grows by adding/adjusting
    /// entries here, not by changing StubPoiPlacer or its callers - PoiMarkerMenu, RoadNetworkMenu
    /// and SettlementOverworldMenu all build markers from this same list so a given seed always
    /// shows/connects/settles the identical marker set across every overworld tool.
    /// </summary>
    public static class PoiDemoScenario
    {
        public const int TownApproachCount = 5;
        public const int ShrineCount = 3;
        public const int CampCount = 10;

        private static readonly BiomeId[] AllBiomes =
        {
            BiomeId.Swamp, BiomeId.Meadow, BiomeId.Forest, BiomeId.Highland, BiomeId.Mountain
        };

        public static List<PoiRequest> BuildRequests()
        {
            var requests = new List<PoiRequest>(TownApproachCount + ShrineCount + CampCount + AllBiomes.Length);

            for (var i = 0; i < TownApproachCount; i++)
            {
                requests.Add(new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }));
            }

            for (var i = 0; i < ShrineCount; i++)
            {
                requests.Add(new PoiRequest(PoiKind.Shrine, new[] { BiomeId.Forest, BiomeId.Mountain }));
            }

            for (var i = 0; i < CampCount; i++)
            {
                requests.Add(new PoiRequest(PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest }));
            }

            // One landmark per biome, so every biome band gets a distinctive POI even though only
            // one generic PoiKind.Landmark exists so far (a V0 stand-in for future per-biome
            // landmark flavors).
            foreach (var biome in AllBiomes)
            {
                requests.Add(new PoiRequest(PoiKind.Landmark, new[] { biome }));
            }

            return requests;
        }
    }
}
