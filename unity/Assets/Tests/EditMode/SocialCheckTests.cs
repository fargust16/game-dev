using System;
using GameDev.Player.Progression;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEditor;

namespace GameDev.Slice.Tests
{
    /// <summary>The Social Check against 10.4 §2 and §7, read from the project's own rules asset.</summary>
    public sealed class SocialCheckTests
    {
        private const string RulesPath = "Assets/Data/Progression/SocialCheckRules.asset";
        private const double Exact = 1e-9;

        private SocialCheckRules _rules;

        [SetUp]
        public void LoadRules()
        {
            _rules = AssetDatabase.LoadAssetAtPath<SocialCheckRules>(RulesPath);
            Assert.IsNotNull(_rules, RulesPath);
        }

        /// <summary>A chance as 10.4 prints it: a whole percentage.</summary>
        private static int Percent(double chance) => (int)Math.Round(chance * 100d, MidpointRounding.AwayFromZero);

        /// <summary>The chance at a lead of Character Value + Modifiers − Listener Value.</summary>
        private double ChanceAtLead(int lead, RequestSize request, bool enemy)
        {
            const int listener = 2;
            var difficulty = SocialCheck.Difficulty(_rules, request, listener, enemy);
            return SocialCheck.Chance(_rules, lead + listener, 0, difficulty, luck: 0);
        }

        [Test]
        public void ValuesFollow10_4()
        {
            var fresh = new CharacterSheet();
            Assert.AreEqual(0, SocialCheck.CharacterValue(_rules, fresh, CharacterSkill.Persuasion), "A new character has a value of 0.");
            Assert.AreEqual(0, SocialCheck.ListenerValue(_rules, fresh, CharacterSkill.Persuasion));

            var veteran = new CharacterSheet();
            veteran.Set(CharacterAttribute.Intelligence, 50);
            veteran.Set(CharacterAttribute.Will, 50);
            veteran.Set(CharacterSkill.Persuasion, 20);
            Assert.AreEqual(3, SocialCheck.CharacterValue(_rules, veteran, CharacterSkill.Persuasion), "10.4 §7: attributes of 50 with skill 20 give 3.");
            Assert.AreEqual(5, SocialCheck.ListenerValue(_rules, veteran, CharacterSkill.Persuasion), "…against a listener's 5.");

            var brute = new CharacterSheet();
            brute.Set(CharacterAttribute.Strength, 39);
            brute.Set(CharacterAttribute.Will, 1);
            Assert.AreEqual(2, SocialCheck.ListenerValue(_rules, brute, CharacterSkill.Intimidation), "Intimidation uses Strength and Will.");
            Assert.AreEqual(0, SocialCheck.ListenerValue(_rules, brute, CharacterSkill.Deception), "Deception uses Intelligence and Will.");
        }

        [Test]
        public void AnEnemyAddsTwoToTheDifficulty()
        {
            Assert.AreEqual(8, SocialCheck.Difficulty(_rules, RequestSize.Ordinary, 1, enemy: false));
            Assert.AreEqual(10, SocialCheck.Difficulty(_rules, RequestSize.Ordinary, 1, enemy: true));
        }

        [TestCase(RequestSize.Small, 83)]
        [TestCase(RequestSize.Ordinary, 58)]
        [TestCase(RequestSize.Large, 28)]
        [TestCase(RequestSize.Huge, 8)]
        public void EachRequestSizeHasItsChanceAtEqualValues(RequestSize request, int percent)
        {
            Assert.AreEqual(percent, Percent(ChanceAtLead(0, request, enemy: false)));
        }

        [TestCase(-2, 28, 8, 3)]
        [TestCase(-1, 42, 17, 3)]
        [TestCase(0, 58, 28, 8)]
        [TestCase(1, 72, 42, 17)]
        [TestCase(2, 83, 58, 28)]
        [TestCase(3, 92, 72, 42)]
        [TestCase(4, 97, 83, 58)]
        [TestCase(5, 97, 92, 72)]
        [TestCase(6, 97, 97, 83)]
        public void TheBalanceTableOf10_4_7(int lead, int friendlyOrdinary, int enemyOrdinary, int enemyLarge)
        {
            Assert.AreEqual(friendlyOrdinary, Percent(ChanceAtLead(lead, RequestSize.Ordinary, enemy: false)), "Friendly Ordinary");
            Assert.AreEqual(enemyOrdinary, Percent(ChanceAtLead(lead, RequestSize.Ordinary, enemy: true)), "Enemy Ordinary");
            Assert.AreEqual(enemyLarge, Percent(ChanceAtLead(lead, RequestSize.Large, enemy: true)), "Enemy Large");
        }

        [Test]
        public void TheSlicesExamplesOf10_4_7()
        {
            Assert.AreEqual(42, Percent(SocialCheck.Chance(_rules, 0, 0, SocialCheck.Difficulty(_rules, RequestSize.Ordinary, 1, false), 0)), "A new character asks a resident of value 1.");
            Assert.AreEqual(72, Percent(SocialCheck.Chance(_rules, 0, 0, SocialCheck.Difficulty(_rules, RequestSize.Small, 1, false), 0)));

            var conductor = SocialCheck.Difficulty(_rules, RequestSize.Large, 0, enemy: true);
            Assert.AreEqual(17, Percent(SocialCheck.Chance(_rules, 1, 0, conductor, 0)), "Value 1 against a Limit 9 conductor's 0.");
            Assert.AreEqual(42, Percent(SocialCheck.Chance(_rules, 1, 2, conductor, 0)));
            Assert.AreEqual(72, Percent(SocialCheck.Chance(_rules, 1, 4, conductor, 0)));

            var senior = SocialCheck.Difficulty(_rules, RequestSize.Large, 1, enemy: true);
            Assert.AreEqual(8, Percent(SocialCheck.Chance(_rules, 1, 0, senior, 0)), "Intimidating a Limit 8 senior, 1 against 1.");
            Assert.AreEqual(28, Percent(SocialCheck.Chance(_rules, 1, 2, senior, 0)));
            Assert.AreEqual(58, Percent(SocialCheck.Chance(_rules, 1, 4, senior, 0)));
        }

        [Test]
        public void ModifiersStayWithinFour()
        {
            Assert.AreEqual(4, SocialCheck.Modifiers(_rules, 7));
            Assert.AreEqual(-4, SocialCheck.Modifiers(_rules, -6));
            Assert.AreEqual(3, SocialCheck.Modifiers(_rules, 3));
            Assert.AreEqual(SocialCheck.Chance(_rules, 0, 4, 11, 0), SocialCheck.Chance(_rules, 0, 7, 11, 0), Exact);
        }

        [Test]
        public void OnePlusOneAlwaysFailsAndSixPlusSixAlwaysSucceeds()
        {
            Assert.AreEqual(35d / 36d, SocialCheck.DiceChance(100, 7), Exact);
            Assert.AreEqual(1d / 36d, SocialCheck.DiceChance(-100, 7), Exact);
        }

        [Test]
        public void LuckMultipliesTheChanceUpTo97Percent()
        {
            Assert.AreEqual(21d / 36d * 1.2, SocialCheck.Chance(_rules, 0, 0, 7, luck: 20), Exact);
            Assert.AreEqual(SocialCheck.Ceiling, SocialCheck.Chance(_rules, 0, 0, 5, luck: 100), Exact);
            Assert.AreEqual(SocialCheck.Ceiling, SocialCheck.Chance(_rules, 9, 0, 7, luck: 100), Exact, "Luck never beats 1 + 1.");
            Assert.AreEqual(97, Percent(SocialCheck.Ceiling));
            Assert.AreEqual(2d / 36d, SocialCheck.Chance(_rules, 0, 0, 20, luck: 100), Exact, "Luck 100 doubles even 6 + 6 alone.");
        }

        [Test]
        public void RollsFromTheSeedFollowTheChance()
        {
            var chance = SocialCheck.Chance(_rules, 1, 0, SocialCheck.Difficulty(_rules, RequestSize.Large, 0, true), 0);
            var rolls = new WorldSeed(110201).Fork(SliceSeedStreams.SocialChecks, 1);
            const int count = 40000;
            var wins = 0;
            for (var i = 0; i < count; i++)
            {
                wins += SocialCheck.Roll(chance, rolls) ? 1 : 0;
            }

            Assert.AreEqual(chance, wins / (double)count, 0.01);
        }

        [Test]
        public void TheSameOptionAndTickRepeatTheRoll()
        {
            var a = new WorldSeed(110201).Fork(SliceSeedStreams.SocialChecks, 12345, 480);
            var b = new WorldSeed(110201).Fork(SliceSeedStreams.SocialChecks, 12345, 480);
            Assert.AreEqual(SocialCheck.Roll(0.5, a), SocialCheck.Roll(0.5, b));
        }
    }
}
