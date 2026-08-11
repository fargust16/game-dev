using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Roads
{
    /// <summary>
    /// Pure per-point traversal cost (WORLD_GENERATION_PIPELINE.md Iteration 09: "Lowest-cost path
    /// respecting slope, rivers (bridges), and mountains (passes) - never straight through a
    /// mountain"). Kept as a static function of (biome, slope, submerged) rather than baked into
    /// the sampler so it's testable without a live MapMagic terrain - <see cref="RoadCostSampler"/>
    /// is the thin adapter that resolves those three inputs from the real world.
    /// </summary>
    public static class RoadCost
    {
        /// <summary>Submerged ground itself is still impassable at the per-point level - a road
        /// can't walk across a lake bed. <see cref="RoadPathfinder"/> handles bridging separately
        /// (as a special edge that skips a short span of submerged cells entirely, priced by
        /// <see cref="BridgeCostPerMeter"/>/<see cref="MaxBridgeSpanMeters"/> below, not this
        /// per-point cost), so this only ever blocks ordinary cell-to-cell steps.</summary>
        public const float BlockedCost = float.PositiveInfinity;

        public const float BaseCost = 1f;
        public const float SwampCost = 2.5f;
        public const float HighlandCost = 1.6f;
        public const float MountainCost = 4f;

        /// <summary>How sharply a steep slope multiplies cost on top of the biome cost - large
        /// enough that the pathfinder detours through a gentler saddle rather than climbing
        /// straight up a mountain face, without being literally impassable (a short steep stretch
        /// is still cheaper than a very long flat detour).</summary>
        public const float SteepSlopePenalty = 6f;

        /// <summary>Per-meter cost of a bridge span - pricier than ordinary ground
        /// (<see cref="BaseCost"/>) so a short crossing beats a long dry detour, while a wide one
        /// still loses to walking around. Real construction cost has no reason to depend on what's
        /// underwater, so a bridge's cost is pure distance, independent of the (blocked) per-point
        /// terrain cost it skips over. Lowered from an initial 3.5x - on the actual generated lakes
        /// (Perlin-blob shaped, often hundreds of metres across) that value made almost every
        /// detour cheaper than almost every bridge, so bridges essentially never got built even
        /// where a plausible crossing existed; 2x keeps bridging a deliberate, costed choice without
        /// making it uncompetitive against any detour longer than a couple hundred metres.</summary>
        public const float BridgeCostPerMeter = 2f;

        /// <summary>Longest single water crossing a bridge can span - beyond this, the pathfinder
        /// won't even consider bridging in that direction, so roads route around a wide lake
        /// instead of pretending to bridge the whole thing. No real river/strait system exists yet
        /// (Iteration 05), so this only ever fires on a narrow lake inlet or neck. Raised from an
        /// initial 120m - the lake-pit generator (OverworldTerrainMenu.BuildGraph) produces fairly
        /// round basins up to ~500m across, and 120m rarely lined up with any actual narrow point on
        /// one, so bridging had almost no real opportunity to fire at all regardless of cost tuning.
        /// 200m is still a deliberate cap, not "bridge anything" - most lake crossings stay wider
        /// than this and correctly still route around.</summary>
        public const float MaxBridgeSpanMeters = 200f;

        /// <summary>Steepest rise-over-run a cart road climbs comfortably - beyond this,
        /// <see cref="GradeMultiplier"/> escalates sharply. <see cref="Evaluate"/>'s own
        /// slope01-based cost is a per-POINT reading (how steep the ground is right here,
        /// independent of travel direction) and can't by itself produce switchback behaviour: two
        /// directions through the same point read equally steep even though walking along a
        /// mountainside's contour is far gentler than walking straight up it. Grade is a per-EDGE
        /// quantity (real elevation change over the step actually taken), which is what actually
        /// distinguishes those two directions - see <see cref="RoadPathfinder"/>'s optional height
        /// delegate.</summary>
        public const float MaxComfortableGrade = 0.15f;

        /// <summary>How sharply cost escalates as grade approaches <see cref="MaxWalkableGrade"/> -
        /// a grade barely over <see cref="MaxComfortableGrade"/> is only mildly discouraged, growing
        /// smoothly toward the hard cap rather than jumping straight from "expensive" to "blocked".</summary>
        public const float GradePenaltyPower = 2f;

        /// <summary>Grade at or above which a step is genuinely impassable (infinite cost, exactly
        /// like <see cref="BlockedCost"/> for water) rather than merely expensive. An earlier version
        /// only ever multiplied cost, never blocked - on real generated mountains (terraced risers
        /// between ledges can be near-vertical) that meant "no genuinely walkable alternative exists
        /// nearby" still produced *a* path, because A* always returns the least-bad option among
        /// whatever's reachable rather than refusing outright. The result read as a road climbing
        /// straight up a cliff face, physically impossible to actually walk - drawing a technically-
        /// connected but unusable line is worse than admitting no route exists. 60% (~31°) is a
        /// generous upper bound for a foot/pack-animal trail, well beyond anything a cart could use,
        /// but still refuses genuine cliffs.</summary>
        public const float MaxWalkableGrade = 0.6f;

        /// <summary>Multiplies the ordinary terrain-cost step price by how far this specific edge's
        /// grade sits between <see cref="MaxComfortableGrade"/> (no penalty) and
        /// <see cref="MaxWalkableGrade"/> (blocked) - grows without bound as grade approaches the
        /// hard cap, so a route that can lengthen itself to stay under the cap always eventually wins
        /// over one that can't, without an abrupt discontinuity right at the boundary.</summary>
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

            var slope = Mathf.Clamp01(slope01);
            var slopeMultiplier = 1f + slope * slope * SteepSlopePenalty;
            return biomeCost * slopeMultiplier;
        }
    }
}
