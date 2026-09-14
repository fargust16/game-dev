using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>Everything the surface rules read at one world point. All world units.</summary>
    public struct TerrainSurfaceSample
    {
        public float HeightMeters;
        public float SlopeDegrees;
        public float ConvexityMeters;
        public float DrainageAreaSqm;
        public float MassifUpliftMeters;

        /// <summary>Shore breakup noise in [0, 1].</summary>
        public float ShoreNoise;

        /// <summary>Rock line breakup noise in [0, 1].</summary>
        public float RockNoise;
    }

    /// <summary>
    /// Evaluates <see cref="TerrainSurfaceRules"/> into layer weights. Pure and deterministic.
    /// </summary>
    public static class TerrainSurfaceClassifier
    {
        public const int LayerCount = 5;

        /// <summary>Stream id for the shore breakup noise.</summary>
        public const string ShoreNoiseStream = "terrain.surface.shore";

        /// <summary>Stream id for the rock line breakup noise.</summary>
        public const string RockNoiseStream = "terrain.surface.rock";

        /// <summary>
        /// Writes the five layer weights for <paramref name="sample"/> into <paramref name="weights"/>,
        /// indexed by <see cref="TerrainSurfaceLayer"/>. They sum to one.
        /// </summary>
        public static void Classify(in TerrainSurfaceSample sample, TerrainSurfaceRules rules, float[] weights)
        {
            weights[0] = 1f;
            weights[1] = 0f;
            weights[2] = 0f;
            weights[3] = 0f;
            weights[4] = 0f;

            var h = sample.HeightMeters;
            var sea = WorldScale.SeaLevelMeters;
            var dry = h > sea ? 1f : 0f;

            // shore -> sand: everything below the vegetation floor, seabed included.
            var shoreOffset = (sample.ShoreNoise - 0.5f) * 2f * rules.ShoreNoiseAmplitudeMeters;
            var sand = 1f - SmoothStep(rules.SandFullBelowMeters, rules.SandNoneAboveMeters, h - shoreOffset);
            Cover(weights, TerrainSurfaceLayer.Sand, sand);

            // drainage -> soil, on dry land above the shore and off steep ground.
            var drainage = SmoothStep(
                Mathf.Log10(rules.SoilDrainageStartSqm), Mathf.Log10(rules.SoilDrainageFullSqm),
                Mathf.Log10(Mathf.Max(1f, sample.DrainageAreaSqm)));
            var soil = dry * (1f - sand) * drainage * (sample.SlopeDegrees <= rules.SoilMaxSlopeDegrees ? 1f : 0f);
            Cover(weights, TerrainSurfaceLayer.Soil, soil);

            // seabed rock on slopes, only below the waterline; ridges, high ground and bare steep ground
            // on land.
            var seabedRock = SmoothStep(rules.SeabedRockSlopeStartDegrees, rules.SeabedRockSlopeFullDegrees, sample.SlopeDegrees)
                             * (1f - SmoothStep(rules.SeabedRockFullBelowMeters, rules.SeabedRockNoneAboveMeters, h));
            var massif = SmoothStep(rules.MassifUpliftStartMeters, rules.MassifUpliftFullMeters, sample.MassifUpliftMeters);
            var rockOffset = (sample.RockNoise - 0.5f) * 2f * rules.RockHeightNoiseMeters;
            var highRock = massif * SmoothStep(rules.RockHeightStartMeters, rules.RockHeightFullMeters, h + rockOffset);
            var ridgeRock = massif * SmoothStep(rules.RidgeConvexityStartMeters, rules.RidgeConvexityFullMeters, sample.ConvexityMeters);
            var steepRock = SmoothStep(rules.SteepRockSlopeStartDegrees, rules.SteepRockSlopeFullDegrees, sample.SlopeDegrees);
            var rock = Mathf.Max(seabedRock, dry * Mathf.Max(Mathf.Max(highRock, ridgeRock), steepRock));
            Cover(weights, TerrainSurfaceLayer.Rock, rock);

            // sheer faces -> cliff, dry land only.
            var cliff = dry * SmoothStep(rules.CliffSlopeStartDegrees, rules.CliffSlopeFullDegrees, sample.SlopeDegrees);
            Cover(weights, TerrainSurfaceLayer.Cliff, cliff);
        }

        /// <summary>Hermite step, 0 at or below <paramref name="edge0"/>, 1 at or above <paramref name="edge1"/>.</summary>
        public static float SmoothStep(float edge0, float edge1, float x)
        {
            if (edge1 <= edge0)
            {
                return x >= edge1 ? 1f : 0f;
            }

            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Layer <paramref name="layer"/> covers every other layer by <paramref name="mask"/>.</summary>
        private static void Cover(float[] weights, TerrainSurfaceLayer layer, float mask)
        {
            mask = Mathf.Clamp01(mask);
            if (mask <= 0f)
            {
                return;
            }

            var keep = 1f - mask;
            for (var i = 0; i < LayerCount; i++)
            {
                weights[i] *= keep;
            }

            weights[(int)layer] += mask;
        }
    }
}
