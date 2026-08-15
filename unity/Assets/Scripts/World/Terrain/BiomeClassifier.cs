using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// V0 multi-axis classification (WORLD_GENERATION_PIPELINE.md): height + slope + moisture.
    /// Height is sea-relative 0..1 from <see cref="VistaTerrainSampler"/> (ocean shelf does not
    /// steal the Swamp band). Thresholds keep Green walkable/buildable while Highland/Mountain
    /// own dramatic relief above sea. Temperature / distance-to-water remain deferred.
    /// </summary>
    public static class BiomeClassifier
    {
        // Design-intent land fractions above sea level (not whole-tile percentiles including ocean).
        public const float RankSwampMaxHeight = 0.10f;
        public const float RankLowerGreenMaxHeight = 0.45f;
        public const float RankGreenMaxHeight = 0.80f;
        public const float RankHighlandMaxHeight = 0.90f;

        /// <summary>How much of the 0..1 land elevation range the bottom <see cref="RankGreenMaxHeight"/>
        /// band (Swamp+Green) occupies — thin on purpose so plains stay walkable.</summary>
        public const float PlainsElevationCap = 0.17f;

        /// <summary>Bottom 10% of land: Swamp (wet) / Meadow (dry).</summary>
        public const float SwampMaxHeight = RankSwampMaxHeight * (PlainsElevationCap / RankGreenMaxHeight);

        /// <summary>Next 35% of land (10-45%): Forest (wet) / Meadow (dry).</summary>
        public const float LowerGreenMaxHeight = RankLowerGreenMaxHeight * (PlainsElevationCap / RankGreenMaxHeight);

        /// <summary>Next 35% of land (45-80%): Forest. Green totals most of habitable area.</summary>
        public const float GreenMaxHeight = PlainsElevationCap;

        /// <summary>Next 10% of land (80-90%): Highland, unless steep → Mountain.</summary>
        public const float HighlandMaxHeight =
            PlainsElevationCap + (RankHighlandMaxHeight - RankGreenMaxHeight) / (1f - RankGreenMaxHeight) * (1f - PlainsElevationCap);

        /// <summary>Above HighlandMaxHeight: Mountain.</summary>
        public const float SteepSlope = 0.5f;

        public const float WetMoisture = 0.5f;

        public static BiomeId Classify(float height01, float slope01, float moisture01)
        {
            var h = Mathf.Clamp01(height01);
            var s = Mathf.Clamp01(slope01);
            var m = Mathf.Clamp01(moisture01);

            if (h >= HighlandMaxHeight)
            {
                return BiomeId.Mountain;
            }

            if (h >= GreenMaxHeight)
            {
                return s >= SteepSlope ? BiomeId.Mountain : BiomeId.Highland;
            }

            if (h >= LowerGreenMaxHeight)
            {
                return BiomeId.Forest;
            }

            if (h >= SwampMaxHeight)
            {
                return m >= WetMoisture ? BiomeId.Forest : BiomeId.Meadow;
            }

            return m >= WetMoisture ? BiomeId.Swamp : BiomeId.Meadow;
        }
    }
}
