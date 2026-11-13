using System.Collections.Generic;
using GameDev.Player.Progression;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.2 (07.1 §3; owner, 2026-11-13, S5-1): every use grows its skill and the attributes of its formula by
    /// Progression Numbers V0, a failure half and a critical success twice, the zero floor the only brake; the XP carried is
    /// saved with the sheet.
    /// </summary>
    public sealed class GrowthTests
    {
        private const double Tight = 1e-9;

        [Test]
        public void TheAwardsAreProgressionNumbersV0()
        {
            Assert.AreEqual(9, Growth.Stage(1));
            Assert.AreEqual(9, Growth.Stage(9));
            Assert.AreEqual(8, Growth.Stage(10));
            Assert.AreEqual(0, Growth.Stage(99));
            Assert.AreEqual(10d, Growth.BaseXp(9), Tight);
            Assert.AreEqual(5120d, Growth.BaseXp(0), Tight);

            Assert.AreEqual(10d, Growth.Award(1, 9, CheckOutcome.Normal), Tight, "a matched task: its BaseXP");
            Assert.AreEqual(5d, Growth.Award(1, 9, CheckOutcome.Failure), Tight, "a failure half (owner, S5-1)");
            Assert.AreEqual(20d, Growth.Award(1, 9, CheckOutcome.Critical), Tight, "a critical success twice (owner, S5-1)");
            Assert.AreEqual(4d, Growth.Award(15, 9, CheckOutcome.Normal), Tight, "a task a stage below: K 0.4");
            Assert.AreEqual(1d, Growth.Award(25, 9, CheckOutcome.Normal), Tight, "two below: K 0.1");
            Assert.AreEqual(0d, Growth.Award(35, 9, CheckOutcome.Critical), Tight, "three below teaches nothing: the zero floor");
            Assert.AreEqual(8d, Growth.Award(1, 8, CheckOutcome.Normal), Tight, "a harder task: the matched award × 0.8, never more");
            Assert.AreEqual(0.1d, Growth.Award(1, 9, CheckOutcome.Normal, Growth.AttributeShare), Tight, "a primary attribute: a hundredth");
            Assert.AreEqual(0.05d, Growth.Award(1, 9, CheckOutcome.Normal, Growth.SecondaryShare), Tight, "a secondary one: half that");

            Assert.AreEqual(10d, Growth.ToNext(1), Tight);
            Assert.AreEqual(10d * System.Math.Pow(1.1, 9), Growth.ToNext(10), Tight);

            Assert.AreEqual(9, Growth.StageOfDifficulty(9));
            Assert.AreEqual(8, Growth.StageOfDifficulty(10));
            Assert.AreEqual(9, Growth.StageOfTarget(null), "an Uninitiated person or an ordinary beast");
            Assert.AreEqual(8, Growth.StageOfTarget(null, glowing: true), "a glowing beast");
            Assert.AreEqual(8, Growth.StageOfTarget(8), "a Limit 8 target");
            Assert.AreEqual(CheckOutcome.Critical, Growth.Of(ReactionResult.Perfect));
            Assert.AreEqual(CheckOutcome.Failure, Growth.Of(ReactionResult.Bad));
        }

        [Test]
        public void AUseGrowsItsSkillAndTheAttributesOfItsFormula()
        {
            var sheet = new CharacterSheet();
            var grew = new List<string>();
            sheet.SkillGrew += (s, level) => grew.Add($"{s} {level}");
            sheet.AttributeGrew += (a, level) => grew.Add($"{a} {level}");

            sheet.Train(CharacterSkill.LightFirearms, 9, CheckOutcome.Normal, new[] { CharacterAttribute.Dexterity },
                new[] { CharacterAttribute.Perception });
            Assert.AreEqual(2, sheet.Get(CharacterSkill.LightFirearms), "10 XP: level 1 to 2");
            Assert.AreEqual(0d, sheet.Xp(CharacterSkill.LightFirearms), Tight);
            Assert.AreEqual(0.1d, sheet.Xp(CharacterAttribute.Dexterity), Tight, "the Profile Attribute a hundredth");
            Assert.AreEqual(0.05d, sheet.Xp(CharacterAttribute.Perception), Tight, "Perception, secondary, half that (S5b.3)");
            Assert.AreEqual(0d, sheet.Xp(CharacterAttribute.Luck), Tight, "Luck grows by discovery, not by ordinary use");
            CollectionAssert.AreEqual(new[] { "LightFirearms 2" }, grew);

            sheet.Train(CharacterSkill.LightFirearms, 9, CheckOutcome.Critical, new[] { CharacterAttribute.Dexterity });
            Assert.AreEqual(3, sheet.Get(CharacterSkill.LightFirearms), "20 XP: 11 to level 3, 9 carried");
            Assert.AreEqual(9d, sheet.Xp(CharacterSkill.LightFirearms), Tight);

            sheet.Train(null, 9, CheckOutcome.Normal, new[] { CharacterAttribute.Will });
            Assert.AreEqual(0.1d, sheet.Xp(CharacterAttribute.Will), Tight, "a use with no skill trains its attribute alone");
        }

        [Test]
        public void GrowthCarriesOverAndStopsAtNinetyNine()
        {
            var sheet = new CharacterSheet();
            Assert.AreEqual(2, sheet.Gain(CharacterSkill.Mining, 21d), "10 + 11 XP: two levels at once");
            Assert.AreEqual(3, sheet.Get(CharacterSkill.Mining));
            Assert.AreEqual(0, sheet.Gain(CharacterSkill.Mining, -5d), "no XP is taken away");

            sheet.Set(CharacterSkill.Mining, 98);
            sheet.Gain(CharacterSkill.Mining, 1e9);
            Assert.AreEqual(Growth.Top, sheet.Get(CharacterSkill.Mining), "ordinary growth ends at 99; 100 stands apart");
            Assert.AreEqual(0d, sheet.Xp(CharacterSkill.Mining), Tight, "and gathers no XP there");
            Assert.AreEqual(0, sheet.Gain(CharacterSkill.Mining, 100d));
        }

        [Test]
        public void TheCarriedXpIsSavedAndAnOlderSaveReadsNone()
        {
            var sheet = new CharacterSheet();
            sheet.Gain(CharacterSkill.Theft, 15d);
            sheet.Gain(CharacterAttribute.Dexterity, 0.4d);
            var restored = new CharacterSheet();
            restored.Restore(sheet.Capture());
            Assert.AreEqual(2, restored.Get(CharacterSkill.Theft));
            Assert.AreEqual(5d, restored.Xp(CharacterSkill.Theft), Tight);
            Assert.AreEqual(0.4d, restored.Xp(CharacterAttribute.Dexterity), Tight);

            var old = UnityEngine.JsonUtility.FromJson<CharacterSheetState>("{\"attributes\":[{\"name\":\"Dexterity\",\"value\":4}],\"skills\":[]}");
            restored.Restore(old);
            Assert.AreEqual(4, restored.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual(0d, restored.Xp(CharacterAttribute.Dexterity), Tight, "a save from before S5b.2 carries no XP");
        }

        [Test]
        public void ASocialCheckGrowsItsSkillAndBothItsAttributes()
        {
            var sheet = new CharacterSheet();
            var listener = new CharacterSheet();
            listener.Set(CharacterAttribute.Intelligence, 12);
            listener.Set(CharacterAttribute.Will, 10);
            SocialCheck.Train(sheet, CharacterSkill.Persuasion, listener, true);
            Assert.AreEqual(1, sheet.Get(CharacterSkill.Persuasion), "a listener of (12 + 10) / 2 = 11 is a stage 8 task");
            Assert.AreEqual(8d, sheet.Xp(CharacterSkill.Persuasion), Tight, "a stage above: the matched 10 × 0.8");
            Assert.AreEqual(0.08d, sheet.Xp(CharacterAttribute.Intelligence), Tight, "both attributes, of equal weight, the full hundredth");
            Assert.AreEqual(0.08d, sheet.Xp(CharacterAttribute.Will), Tight);

            SocialCheck.Train(new NotASheet(), CharacterSkill.Persuasion, listener, true); // a character that is no sheet is not trained
        }

        private sealed class NotASheet : ICharacterValues
        {
            public int Get(CharacterAttribute attribute) => 1;

            public int Get(CharacterSkill skill) => 1;
        }
    }
}
