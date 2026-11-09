using System;
using GameDev.Core.Save;
using GameDev.Player.Progression;
using NUnit.Framework;

namespace GameDev.Slice.Tests
{
    public sealed class CharacterSheetTests
    {
        [Test]
        public void ANewSheetStartsEveryValueAtOne()
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute attribute in Enum.GetValues(typeof(CharacterAttribute)))
            {
                Assert.AreEqual(1, sheet.Get(attribute), attribute.ToString());
            }

            foreach (CharacterSkill skill in Enum.GetValues(typeof(CharacterSkill)))
            {
                Assert.AreEqual(1, sheet.Get(skill), skill.ToString());
            }

            Assert.AreEqual(7, Enum.GetValues(typeof(CharacterAttribute)).Length, "07.1 §2 names seven attributes.");
        }

        [Test]
        public void ValuesStayBetweenOneAndAHundred()
        {
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Strength, 0);
            sheet.Set(CharacterAttribute.Luck, 150);
            sheet.Set(CharacterSkill.Deception, -3);
            Assert.AreEqual(1, sheet.Get(CharacterAttribute.Strength));
            Assert.AreEqual(100, sheet.Get(CharacterAttribute.Luck));
            Assert.AreEqual(1, sheet.Get(CharacterSkill.Deception));
        }

        [Test]
        public void TheSheetComesBackFromASave()
        {
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Will, 34);
            sheet.Set(CharacterAttribute.Perception, 12);
            sheet.Set(CharacterSkill.Intimidation, 7);
            var file = SaveAssembler.Capture(new ISaveContributor[] { new CharacterSheetSaveContributor(sheet) });

            var restored = new CharacterSheet();
            restored.Set(CharacterAttribute.Strength, 50);
            var missing = SaveAssembler.Restore(file, new ISaveContributor[] { new CharacterSheetSaveContributor(restored) });

            CollectionAssert.IsEmpty(missing);
            Assert.AreEqual(SaveSchema.PlayerCharacter, file.sections[0].key);
            Assert.AreEqual(34, restored.Get(CharacterAttribute.Will));
            Assert.AreEqual(12, restored.Get(CharacterAttribute.Perception));
            Assert.AreEqual(7, restored.Get(CharacterSkill.Intimidation));
            Assert.AreEqual(1, restored.Get(CharacterAttribute.Strength), "A restore replaces the whole sheet.");
        }

        [Test]
        public void ASavedNameThisBuildLacksIsSkipped()
        {
            var state = new CharacterSheetState();
            state.attributes.Add(new CharacterValueEntry { name = "Charisma", value = 40 });
            state.attributes.Add(new CharacterValueEntry { name = "3", value = 40 });
            state.attributes.Add(new CharacterValueEntry { name = "Dexterity", value = 9 });

            var sheet = new CharacterSheet();
            sheet.Restore(state);

            Assert.AreEqual(9, sheet.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual(1, sheet.Get(CharacterAttribute.Perception), "A number is not a name.");
        }
    }
}
