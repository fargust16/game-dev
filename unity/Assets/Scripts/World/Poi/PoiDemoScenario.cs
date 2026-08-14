using System.Collections.Generic;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// V0 demo POI quota (WORLD_GENERATION_PIPELINE.md Iteration 08): a fixed count-per-kind
    /// request list for <see cref="StubPoiPlacer"/> / <see cref="PoiPlacementEngine"/>. Towns use
    /// corner+center 3×3 preferred sectors; secondary POI are assigned one preferred sector each
    /// so every habitable sector gets coverage.
    /// </summary>
    public static class PoiDemoScenario
    {
        public const int TownApproachCount = 5;
        public const int ShrineCount = 9;
        public const int CampCount = 9;

        private static readonly BiomeId[] AllBiomes =
        {
            BiomeId.Swamp, BiomeId.Meadow, BiomeId.Forest, BiomeId.Highland, BiomeId.Mountain
        };

        public static List<PoiRequest> BuildRequests() =>
            BuildRequests(WorldGenSettings.Default);

        public static List<PoiRequest> BuildRequests(WorldGenSettings settings)
        {
            settings.Clamp();
            var capacity = settings.TownCount + settings.ShrineCount + settings.SmallCampCount +
                           settings.LandmarkCount;
            var requests = new List<PoiRequest>(capacity);

            var townSlots = WorldSectorMap.PreferredTownSlots;
            var townCount = Mathf.Min(settings.TownCount, townSlots.Length);
            for (var i = 0; i < townCount; i++)
            {
                var slot = townSlots[i];
                var isHub = slot.x == 1 && slot.y == 1;
                requests.Add(new PoiRequest(
                    PoiKind.TownApproach,
                    new[] { BiomeId.Meadow },
                    slot.x,
                    slot.y,
                    isMainHub: isHub));
            }

            // Extra towns beyond 5 (if settings raised) — no preferred sector.
            for (var i = townCount; i < settings.TownCount; i++)
            {
                requests.Add(new PoiRequest(PoiKind.TownApproach, new[] { BiomeId.Meadow }));
            }

            AppendSectorDistributed(
                requests, PoiKind.Shrine, settings.ShrineCount,
                new[] { BiomeId.Forest, BiomeId.Mountain });
            AppendSectorDistributed(
                requests, PoiKind.Camp, settings.SmallCampCount,
                new[] { BiomeId.Meadow, BiomeId.Forest });

            for (var i = 0; i < settings.LandmarkCount; i++)
            {
                var biome = AllBiomes[i % AllBiomes.Length];
                var sx = i % WorldSectorMap.GridSize;
                var sz = (i / WorldSectorMap.GridSize) % WorldSectorMap.GridSize;
                requests.Add(new PoiRequest(PoiKind.Landmark, new[] { biome }, sx, sz));
            }

            return requests;
        }

        private static void AppendSectorDistributed(
            List<PoiRequest> requests,
            PoiKind kind,
            int count,
            BiomeId[] biomes)
        {
            for (var i = 0; i < count; i++)
            {
                var sx = i % WorldSectorMap.GridSize;
                var sz = (i / WorldSectorMap.GridSize) % WorldSectorMap.GridSize;
                requests.Add(new PoiRequest(kind, biomes, sx, sz));
            }
        }
    }
}
