using GameDev.Player.Progression;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>The action check against 07.1 §4–8, read from the project's own rules asset.</summary>
    public sealed class ActionCheckTests
    {
        private const string RulesPath = "Assets/Data/Progression/ActionCheckRules.asset";
        private const double Exact = 1e-6;

        private ActionCheckRules _rules;

        [SetUp]
        public void LoadRules()
        {
            _rules = AssetDatabase.LoadAssetAtPath<ActionCheckRules>(RulesPath);
            Assert.IsNotNull(_rules, RulesPath);
        }

        [TestCase(-12, 0.01)]
        [TestCase(-8, 0.01)]
        [TestCase(-7, 0.02)]
        [TestCase(-6, 0.05)]
        [TestCase(-5, 0.10)]
        [TestCase(-4, 0.20)]
        [TestCase(-3, 0.35)]
        [TestCase(-2, 0.50)]
        [TestCase(-1, 0.65)]
        [TestCase(0, 0.80)]
        [TestCase(1, 0.90)]
        [TestCase(2, 0.95)]
        [TestCase(3, 0.98)]
        [TestCase(4, 0.99)]
        [TestCase(9, 0.99)]
        public void TheCurveIsTheTableOf07_1(int delta, double chance)
        {
            Assert.AreEqual(chance, ActionCheck.BaseChance(_rules, delta), Exact);
        }

        [TestCase(-5.5, 0.075)]
        [TestCase(0.5, 0.85)]
        [TestCase(-2.25, 0.4625)]
        [TestCase(3.5, 0.985)]
        [TestCase(-8.5, 0.01)]
        [TestCase(4.5, 0.99)]
        public void BetweenWholeStepsTheCurveIsLinearAndFlatBeyondItsEnds(double delta, double chance)
        {
            Assert.AreEqual(chance, ActionCheck.BaseChance(_rules, delta), Exact);
        }

        [TestCase(6, 4, 5d)]
        [TestCase(9, 1, 5d)]
        [TestCase(9, 9, 9d)]
        [TestCase(6, 5, 5.5)]
        public void PreparationIsTheMeanOfAttributeAndSkill(int attribute, int skill, double preparation)
        {
            Assert.AreEqual(preparation, ActionCheck.Preparation(attribute, skill), Exact);
        }

        /// <summary>10.2 §5a: Dexterity 3 and First Aid 1 treat at 90 %, 80 % and 65 %.</summary>
        [TestCase(1, 0.90)]
        [TestCase(2, 0.80)]
        [TestCase(3, 0.65)]
        public void FirstAidOfANewCharacterHasTheChancesOf10_2(int difficulty, double chance)
        {
            var chances = ActionCheck.Chances(_rules, ActionCheck.Preparation(3, 1), difficulty, perception: 1, luck: 0, hasCritical: false);
            Assert.AreEqual(chance, chances.Success, Exact);
        }

        [Test]
        public void SecondaryAttributesAddATenthAtMost()
        {
            Assert.AreEqual(0d, ActionCheck.SecondaryBonus(_rules, 20, new int[0]), Exact);
            Assert.AreEqual(0.05, ActionCheck.SecondaryBonus(_rules, 20, new[] { 10 }), Exact);
            Assert.AreEqual(0.05, ActionCheck.SecondaryBonus(_rules, 20, new[] { 10, 10 }), Exact, "Averaged, not added.");
            Assert.AreEqual(0.10, ActionCheck.SecondaryBonus(_rules, 20, new[] { 20, 60 }), Exact);

            var chances = ActionCheck.Chances(_rules, 18, 20, perception: 1, luck: 0, secondaries: new[] { 10 });
            Assert.AreEqual(0.50 * 1.05, chances.Success, Exact);
        }

        [Test]
        public void LuckMultipliesAChanceUpTo99Percent()
        {
            Assert.AreEqual(0.60, ActionCheck.ApplyLuck(0.50, 20, _rules.ChanceCap), Exact);
            Assert.AreEqual(0.06, ActionCheck.ApplyLuck(0.05, 20, _rules.ChanceCap), Exact);
            Assert.AreEqual(0.99, ActionCheck.ApplyLuck(0.80, 100, _rules.ChanceCap), Exact);

            var lucky = ActionCheck.Chances(_rules, 2, 4, perception: 1, luck: 20);
            Assert.AreEqual(0.60, lucky.Success, Exact);

            var capped = ActionCheck.Chances(_rules, 30, 20, perception: 1, luck: 0, secondaries: new[] { 20 });
            Assert.AreEqual(0.99, capped.Success, Exact, "The cap holds after the secondary bonus too, with no Luck.");
        }

        [TestCase(1, 0.05)]
        [TestCase(25, 0.268182)]
        [TestCase(75, 0.722727)]
        [TestCase(100, 0.95)]
        public void TheCriticalChanceGrowsWithPerception(int perception, double critical)
        {
            var chances = ActionCheck.Chances(_rules, 10, 10, perception, luck: 0);
            Assert.AreEqual(critical, chances.Critical, Exact);
            Assert.AreEqual(0d, chances.Overflow, Exact);
        }

        [Test]
        public void LuckAboveTheCriticalCapBecomesOverflow()
        {
            var chances = ActionCheck.Chances(_rules, 10, 10, perception: 75, luck: 100);
            Assert.AreEqual(0.95, chances.Critical, Exact);
            Assert.AreEqual(0.722727 * 2 - 0.95, chances.Overflow, 1e-5);
        }

        [Test]
        public void TheThreeOutcomesFollow07_1_8AndSumToOne()
        {
            var chances = ActionCheck.Chances(_rules, 10, 11, perception: 50, luck: 10);
            Assert.AreEqual(chances.Success * chances.Critical, chances.CriticalOutcome, Exact);
            Assert.AreEqual(chances.Success * (1 - chances.Critical), chances.NormalOutcome, Exact);
            Assert.AreEqual(1d, chances.CriticalOutcome + chances.NormalOutcome + chances.Failure, Exact);

            var plain = ActionCheck.Chances(_rules, 10, 11, perception: 100, luck: 0, hasCritical: false);
            Assert.AreEqual(0d, plain.CriticalOutcome, Exact);
        }

        [Test]
        public void RollsFromTheSeedFollowTheChances()
        {
            var chances = ActionCheck.Chances(_rules, 10, 11, perception: 50, luck: 0);
            var rolls = new WorldSeed(110201).Fork(SliceSeedStreams.ActionChecks, 1);
            const int count = 40000;
            var tally = new int[3];
            for (var i = 0; i < count; i++)
            {
                tally[(int)ActionCheck.Roll(chances, rolls)]++;
            }

            Assert.AreEqual(chances.Failure, tally[(int)CheckOutcome.Failure] / (double)count, 0.01);
            Assert.AreEqual(chances.NormalOutcome, tally[(int)CheckOutcome.Normal] / (double)count, 0.01);
            Assert.AreEqual(chances.CriticalOutcome, tally[(int)CheckOutcome.Critical] / (double)count, 0.01);
        }

        [Test]
        public void TheSameSeedRepeatsTheRolls()
        {
            var chances = ActionCheck.Chances(_rules, 10, 11, perception: 50, luck: 0);
            var a = new WorldSeed(110201).Fork(SliceSeedStreams.ActionChecks, 7, 3);
            var b = new WorldSeed(110201).Fork(SliceSeedStreams.ActionChecks, 7, 3);
            for (var i = 0; i < 32; i++)
            {
                Assert.AreEqual(ActionCheck.Roll(chances, a), ActionCheck.Roll(chances, b));
            }
        }
    }
}
