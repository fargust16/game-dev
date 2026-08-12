using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// V0 multi-axis classification (WORLD_GENERATION_PIPELINE.md Iteration 04: "height + slope +
    /// moisture ... not height alone"). The height thresholds below guarantee an exact area
    /// fraction, but not directly against the raw 0..1 "height" channel value - they're derived
    /// from a percentile-ranked source (see <see cref="PercentileRankGenerator"/>) that
    /// <see cref="OverworldTerrainMenu"/> then compresses vertically: Green needs to be walkable,
    /// buildable ground (city sites need actually flat terrain, not just "technically not
    /// Mountain"), so the bottom <see cref="RankGreenMaxHeight"/> of ranked area is squeezed into
    /// only <see cref="PlainsElevationCap"/> of the vertical range, while Highland/Mountain keep
    /// the remaining, much larger, share for dramatic relief. That compression is exactly
    /// monotonic (never reorders which cell is higher than which), so "the lowest X% of area" is
    /// still exactly true after compression - only the elevation VALUE at each rank boundary
    /// changes, hence the constants below being derived expressions rather than the plain
    /// percentages (<see cref="RankSwampMaxHeight"/> etc.) they're built from. Two low-elevation
    /// points that only differ by wetness read differently (Swamp vs. Meadow), and steep ground in
    /// the highland band reads as Mountain instead of gentle Highland - a steep valley wall partway
    /// up a hill is rocky, not a gentle meadow just because it's not that tall, but a carved lake
    /// pit's steep rim in flat lowlands still reads as shoreline, not Mountain. Temperature and
    /// distance-to-water are still deferred (full 5-axis system per the pipeline doc is a later
    /// refinement, not V0).
    /// </summary>
    public static class BiomeClassifier
    {
        // Design-intent area fractions (of the percentile-ranked source, before compression) -
        // these are the actual guarantees ("bottom 10% of the map's area is Swamp-eligible" etc.);
        // everything below is this same promise re-expressed in the compressed elevation scale
        // OverworldTerrainMenu.BuildGraph actually samples.
        public const float RankSwampMaxHeight = 0.10f;
        public const float RankLowerGreenMaxHeight = 0.45f;
        public const float RankGreenMaxHeight = 0.80f;
        public const float RankHighlandMaxHeight = 0.90f;

        /// <summary>How much of the 0..1 elevation range the bottom <see cref="RankGreenMaxHeight"/>
        /// of ranked area (Swamp+Green) is compressed into - small on purpose, so that band reads
        /// as walkable, buildable plains instead of rolling hills with nowhere flat for a
        /// settlement. Highland+Mountain get the remaining (1 - this) share of the range.</summary>
        public const float PlainsElevationCap = 0.17f;

        /// <summary>Bottom 10% of area (by rank): Swamp (wet) / Meadow (dry).</summary>
        public const float SwampMaxHeight = RankSwampMaxHeight * (PlainsElevationCap / RankGreenMaxHeight);

        /// <summary>Next 35% of area (10-45% by rank): Forest (wet) / Meadow (dry) - still
        /// moisture-sensitive.</summary>
        public const float LowerGreenMaxHeight = RankLowerGreenMaxHeight * (PlainsElevationCap / RankGreenMaxHeight);

        /// <summary>Next 35% of area (45-80% by rank): Forest unconditionally. Green (Meadow+Forest)
        /// totals >= 70% of the map's AREA by construction (0-80% by rank, minus whatever the
        /// wet/dry splits above hand to Swamp) - comfortably clearing a 50% floor - even though its
        /// elevation footprint is deliberately thin (see <see cref="PlainsElevationCap"/>).</summary>
        public const float GreenMaxHeight = PlainsElevationCap;

        /// <summary>Next 10% of area (80-90% by rank): Highland, unless steep enough to read as
        /// Mountain instead.</summary>
        public const float HighlandMaxHeight =
            PlainsElevationCap + (RankHighlandMaxHeight - RankGreenMaxHeight) / (1f - RankGreenMaxHeight) * (1f - PlainsElevationCap);

        /// <summary>Above HighlandMaxHeight (top 10% of area, by rank): Mountain.</summary>
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
