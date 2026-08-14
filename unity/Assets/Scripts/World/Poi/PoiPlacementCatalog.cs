using System.Collections.Generic;
using GameDev.World.Terrain;

namespace GameDev.World.Poi
{
    /// <summary>Default <see cref="PoiPlacementProfile"/> table for every <see cref="PoiKind"/>.</summary>
    public static class PoiPlacementCatalog
    {
        private static readonly Dictionary<PoiKind, PoiPlacementProfile> Profiles = Build();

        public static PoiPlacementProfile Get(PoiKind kind) =>
            Profiles.TryGetValue(kind, out var p) ? p : Profiles[PoiKind.Camp];

        public static IReadOnlyDictionary<PoiKind, PoiPlacementProfile> All => Profiles;

        private static Dictionary<PoiKind, PoiPlacementProfile> Build()
        {
            var town = new PoiPlacementProfile(PoiKind.TownApproach, new[] { BiomeId.Meadow })
            {
                MaxSlope01 = 0.30f, // ~8°
                MinDistanceToLakeMeters = 24f,
                MinDistanceToSameKindMeters = 280f,
                RejectMountainBiome = true,
                FootprintRadiusMeters = 48f,
                PreferFlatWeight = 4f,
                PreferLowVarianceWeight = 3f,
                PreferOpenWeight = 1.5f,
                PreferBiomeWeight = 2.5f,
                PreferSpreadWeight = 2f,
            };

            var shrine = new PoiPlacementProfile(PoiKind.Shrine, new[] { BiomeId.Forest, BiomeId.Mountain })
            {
                MaxSlope01 = 0.55f,
                MinDistanceToLakeMeters = 12f,
                MinDistanceToSameKindMeters = 160f,
                MinDistanceToTownMeters = 120f,
                FootprintRadiusMeters = 16f,
                PreferElevatedWeight = 2.5f,
                PreferIsolationWeight = 3f,
                PreferScenicWeight = 2f,
                PreferBiomeWeight = 2f,
                PreferSpreadWeight = 1.5f,
            };

            var camp = new PoiPlacementProfile(PoiKind.Camp, new[] { BiomeId.Meadow, BiomeId.Forest })
            {
                MaxSlope01 = 0.40f,
                MinDistanceToLakeMeters = 16f,
                MinDistanceToSameKindMeters = 90f,
                MinDistanceToTownMeters = 80f,
                FootprintRadiusMeters = 20f,
                PreferForestEdgeWeight = 3.5f,
                PreferFlatWeight = 1.5f,
                PreferBiomeWeight = 2f,
                PreferSpreadWeight = 1.5f,
            };

            var landmark = new PoiPlacementProfile(PoiKind.Landmark, new[]
            {
                BiomeId.Swamp, BiomeId.Meadow, BiomeId.Forest, BiomeId.Highland, BiomeId.Mountain
            })
            {
                MaxSlope01 = 0.85f,
                MinDistanceToLakeMeters = 8f,
                MinDistanceToSameKindMeters = 180f,
                FootprintRadiusMeters = 24f,
                PreferScenicWeight = 4f,
                PreferIsolationWeight = 2f,
                PreferElevatedWeight = 1.5f,
                PreferBiomeWeight = 3f,
                PreferSpreadWeight = 1f,
            };

            var ruin = new PoiPlacementProfile(PoiKind.Ruin, new[] { BiomeId.Forest, BiomeId.Meadow })
            {
                MaxSlope01 = 0.45f,
                MinDistanceToLakeMeters = 12f,
                MinDistanceToSameKindMeters = 140f,
                FootprintRadiusMeters = 20f,
                PreferIsolationWeight = 2f,
                PreferBiomeWeight = 2f,
                PreferSpreadWeight = 1.5f,
            };

            var cave = new PoiPlacementProfile(PoiKind.CaveMouth, new[] { BiomeId.Mountain })
            {
                MaxSlope01 = 0.90f,
                MinDistanceToLakeMeters = 20f,
                MinDistanceToSameKindMeters = 160f,
                FootprintRadiusMeters = 16f,
                PreferElevatedWeight = 2f,
                PreferScenicWeight = 2f,
                PreferBiomeWeight = 3f,
                PreferSpreadWeight = 1.5f,
            };

            return new Dictionary<PoiKind, PoiPlacementProfile>
            {
                { PoiKind.TownApproach, town },
                { PoiKind.Shrine, shrine },
                { PoiKind.Camp, camp },
                { PoiKind.Landmark, landmark },
                { PoiKind.Ruin, ruin },
                { PoiKind.CaveMouth, cave },
            };
        }
    }
}
