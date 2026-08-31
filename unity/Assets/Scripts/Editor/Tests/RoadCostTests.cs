using GameDev.World.Roads;
using GameDev.World.Terrain;
using NUnit.Framework;

namespace GameDev.World.Roads.Tests
{
    public sealed class RoadCostTests
    {
        [Test]
        public void Evaluate_Submerged_IsBlocked()
        {
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.Evaluate(BiomeId.Meadow, 0f, isSubmerged: true)));
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.Evaluate(BiomeId.Mountain, 1f, isSubmerged: true)),
                "submerged should block regardless of biome/slope");
        }

        [Test]
        public void Evaluate_FlatMeadow_IsCheapestBaseline()
        {
            var meadow = RoadCost.Evaluate(BiomeId.Meadow, 0f, isSubmerged: false);
            var forest = RoadCost.Evaluate(BiomeId.Forest, 0f, isSubmerged: false);
            var swamp = RoadCost.Evaluate(BiomeId.Swamp, 0f, isSubmerged: false);
            var highland = RoadCost.Evaluate(BiomeId.Highland, 0f, isSubmerged: false);
            var mountain = RoadCost.Evaluate(BiomeId.Mountain, 0f, isSubmerged: false);

            Assert.AreEqual(RoadCost.BaseCost, meadow);
            Assert.AreEqual(RoadCost.BaseCost, forest);
            Assert.Greater(swamp, meadow);
            Assert.Greater(highland, meadow);
            Assert.Greater(mountain, highland, "a mountain biome should cost more than gentle highland at the same (flat) slope");
        }

        [Test]
        public void Evaluate_SteeperSlope_CostsMoreForTheSameBiome()
        {
            var gentle = RoadCost.Evaluate(BiomeId.Highland, 0.1f, isSubmerged: false);
            var steep = RoadCost.Evaluate(BiomeId.Highland, 0.9f, isSubmerged: false);
            Assert.Greater(steep, gentle);
        }

        [Test]
        public void Evaluate_Mountain_CostsMoreThanSteepMeadow_SoMountainsAreSkirted()
        {
            var mountainFlat = RoadCost.Evaluate(BiomeId.Mountain, 0f, isSubmerged: false);
            var steepMeadow = RoadCost.Evaluate(BiomeId.Meadow, 0.95f, isSubmerged: false);
            var steepHighland = RoadCost.Evaluate(BiomeId.Highland, 0.95f, isSubmerged: false);

            // Skirting is a price signal, not a rule: a mountain has to out-cost the worst
            // non-mountain ground so the search takes a detour whenever one exists.
            Assert.Greater(mountainFlat, steepMeadow);
            Assert.Greater(mountainFlat, steepHighland);
            Assert.Greater(steepHighland, RoadCost.BaseCost);

            // And it stays a price. Mountains are traversable terrain; only water, earthwork past
            // the limits and grade at or above MaxWalkableGrade refuse a route outright.
            Assert.IsFalse(
                float.IsPositiveInfinity(mountainFlat),
                "a mountain is expensive to cross, never impassable");
            Assert.IsTrue(
                float.IsPositiveInfinity(RoadCost.GradeMultiplier(RoadCost.MaxWalkableGrade)),
                "grade is what actually blocks a route");
        }

        [Test]
        public void IsHardObstacleCost_FlagsMountainForTheSmoother_WhileItStaysTraversable()
        {
            // Two different questions about the same cell, and they are allowed to disagree.
            // IsHardObstacleCost answers "may smoothing move a road onto this ground", which is no
            // for a mountain. Whether the planner may route there is answered by the cost being
            // finite, which it is.
            var mountainFlat = RoadCost.Evaluate(BiomeId.Mountain, 0f, isSubmerged: false);

            Assert.IsTrue(RoadCost.IsHardObstacleCost(mountainFlat));
            Assert.IsFalse(float.IsPositiveInfinity(mountainFlat));
        }

        [Test]
        public void Evaluate_ClampsOutOfRangeSlope()
        {
            var overOne = RoadCost.Evaluate(BiomeId.Meadow, 5f, isSubmerged: false);
            var atOne = RoadCost.Evaluate(BiomeId.Meadow, 1f, isSubmerged: false);
            Assert.AreEqual(atOne, overOne, 1e-5f);
        }

        [Test]
        public void GradeMultiplier_AtOrBelowComfortableGrade_IsNeutral()
        {
            Assert.AreEqual(1f, RoadCost.GradeMultiplier(0f), 1e-5f);
            Assert.AreEqual(1f, RoadCost.GradeMultiplier(RoadCost.MaxComfortableGrade), 1e-5f);
        }

        [Test]
        public void GradeMultiplier_NegativeGrade_TreatedTheSameAsPositive()
        {
            // A descent should be exactly as costly to build/traverse as the same-magnitude climb -
            // grade is about how steep the ground is, not which way you're going.
            var descending = RoadCost.GradeMultiplier(-0.4f);
            var climbing = RoadCost.GradeMultiplier(0.4f);
            Assert.AreEqual(climbing, descending, 1e-5f);
        }

        [Test]
        public void GradeMultiplier_EscalatesTowardTheHardCap()
        {
            var justOver = RoadCost.GradeMultiplier(RoadCost.MaxComfortableGrade * 1.1f);
            var doubleGrade = RoadCost.GradeMultiplier(RoadCost.MaxComfortableGrade * 2f);
            var nearCap = RoadCost.GradeMultiplier(RoadCost.MaxWalkableGrade * 0.99f);

            Assert.Greater(justOver, 1f);
            Assert.Greater(doubleGrade, justOver);
            Assert.Greater(nearCap, doubleGrade, "cost should keep climbing as grade approaches the hard cap, without ever reaching it");
            Assert.IsFalse(float.IsPositiveInfinity(nearCap), "just short of the hard cap must still be a real, finite (if very large) cost");
        }

        [Test]
        public void GradeMultiplier_AtOrAboveMaxWalkableGrade_IsBlocked()
        {
            // A road can't literally climb a cliff face - past this grade a step must be refused
            // outright (matching RoadCost.BlockedCost's treatment of submerged ground), not just
            // priced very high, or the pathfinder will still draw a technically-cheapest-available
            // but physically unusable "road" when no genuinely walkable alternative exists.
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.GradeMultiplier(RoadCost.MaxWalkableGrade)));
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.GradeMultiplier(3f)));
            Assert.IsTrue(float.IsPositiveInfinity(RoadCost.GradeMultiplier(-RoadCost.MaxWalkableGrade)),
                "a descent at or beyond the cap must block exactly like a climb of the same magnitude");
        }
    }
}
