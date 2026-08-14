using GameDev.World.Terrain;
using UnityEngine;
using WorldScale = GameDev.World.WorldScale;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Per-point traversal cost. Nonlinear slope³ + earthwork-friendly grades.
    /// Prefer contour detours over cutting hills (see <see cref="Landscape.LandscapeCostField"/>).
    /// </summary>
    public static class RoadCost
    {
        public const float BlockedCost = float.PositiveInfinity;

        public const float BaseCost = 1f;
        public const float SwampCost = 1.8f;
        public const float HighlandCost = 2.4f;
        public const float MountainCost = 14f;

        /// <summary>Weight on pow(slope01, 3) — 5° cheap, 25° near-blocked.</summary>
        public const float SteepSlopePenalty = 6f;

        public const float BridgeCostPerMeter = 2.5f;
        public const float MaxBridgeSpanMeters = 55f;

        public const float MaxComfortableGrade = GameDev.World.WorldScale.RoadMaxGrade;
        public const float GradePenaltyPower = 2.5f;
        /// <summary>Hard block grade — above comfort, cost rises; at this value path is refused.</summary>
        public const float MaxWalkableGrade = 0.45f;

        public const float HardObstacleCostThreshold = MountainCost * 0.85f;

        public static float GradeMultiplier(float grade)
        {
            var g = Mathf.Abs(grade);
            if (g <= MaxComfortableGrade)
            {
                return 1f;
            }

            if (g >= MaxWalkableGrade)
            {
                return float.PositiveInfinity;
            }

            var over = (g - MaxComfortableGrade) / (MaxWalkableGrade - MaxComfortableGrade);
            var ratio = over / Mathf.Max(1f - over, 0.0001f);
            return 1f + Mathf.Pow(ratio, GradePenaltyPower);
        }

        /// <summary>Nonlinear slope cost: pow(slope01, 3) * weight.</summary>
        public static float SlopeCost(float slope01, float weight = SteepSlopePenalty)
        {
            var s = Mathf.Clamp01(slope01);
            return Mathf.Pow(s, 3f) * weight;
        }

        /// <summary>Earthwork cost: pow(depth/allowed, 2.5) for cut, pow(…, 2) for fill.</summary>
        public static float EarthworkCost(float excavationMeters, float fillMeters)
        {
            var allowed = Mathf.Max(WorldScale.RoadAllowedEarthworkDepthMeters, 0.2f);
            if (excavationMeters > WorldScale.RoadMaxCutDepthMeters ||
                fillMeters > WorldScale.RoadMaxFillHeightMeters)
            {
                return BlockedCost;
            }

            var cut = Mathf.Pow(Mathf.Max(0f, excavationMeters) / allowed, 2.5f) * 8f;
            var fill = Mathf.Pow(Mathf.Max(0f, fillMeters) / allowed, 2f) * 5f;
            return cut + fill;
        }

        public static float Evaluate(BiomeId biome, float slope01, bool isSubmerged)
        {
            if (isSubmerged)
            {
                return BlockedCost;
            }

            var biomeCost = biome switch
            {
                BiomeId.Swamp => SwampCost,
                BiomeId.Highland => HighlandCost,
                BiomeId.Mountain => MountainCost,
                _ => BaseCost,
            };

            var slopeWeight = biome == BiomeId.Mountain
                ? SteepSlopePenalty * 2.5f
                : SteepSlopePenalty;
            return biomeCost + SlopeCost(slope01, slopeWeight);
        }

        public static bool IsHardObstacleCost(float cost) =>
            float.IsPositiveInfinity(cost) || cost >= HardObstacleCostThreshold;
    }
}
