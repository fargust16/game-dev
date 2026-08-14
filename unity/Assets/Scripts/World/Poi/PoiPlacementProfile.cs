using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// Per-kind placement rules: hard rejects, soft weights, relation preferences, optional footprint.
    /// Adding a new <see cref="PoiKind"/> means adding a profile — not rewriting the engine.
    /// </summary>
    public sealed class PoiPlacementProfile
    {
        public PoiKind Kind { get; }
        public BiomeId[] PreferredBiomes { get; }

        // Hard constraints (reject if violated). Negative = disabled.
        public float MinHeight01 { get; set; } = 0.12f;
        public float MaxSlope01 { get; set; } = 1f;
        public float MinDistanceToLakeMeters { get; set; } = 8f;
        public float MinDistanceToSameKindMeters { get; set; } = 64f;
        public float MinDistanceToTownMeters { get; set; } = -1f;
        public bool RejectMountainBiome { get; set; }
        public float FootprintRadiusMeters { get; set; }

        // Soft preference weights (higher = stronger pull). Zero = unused.
        public float PreferFlatWeight { get; set; }
        public float PreferOpenWeight { get; set; }
        public float PreferElevatedWeight { get; set; }
        public float PreferIsolationWeight { get; set; }
        public float PreferForestEdgeWeight { get; set; }
        public float PreferScenicWeight { get; set; }
        public float PreferBiomeWeight { get; set; } = 2f;
        public float PreferSpreadWeight { get; set; } = 1f;
        public float PreferLowVarianceWeight { get; set; }

        public PoiPlacementProfile(PoiKind kind, BiomeId[] preferredBiomes)
        {
            Kind = kind;
            PreferredBiomes = preferredBiomes ?? System.Array.Empty<BiomeId>();
        }
    }
}
