using System.Collections.Generic;
using System.Linq;
using GameDev.Player.Progression;
using GameDev.Slice.Creation;
using GameDev.Slice.EditorTools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.1 (07 *Character Start*, *Creation choices in V1*): 9 free points or one of three archetypes of 11, a name kept
    /// clean, and three bodies of each sex, each a player body with its controller and the tools in its right hand.
    /// </summary>
    public sealed class CharacterCreationTests
    {
        [Test]
        public void ThreeArchetypesOfElevenPoints()
        {
            Assert.AreEqual(3, CharacterStart.Archetypes.Length);
            Assert.IsTrue(CharacterStart.Archetypes.All(a => a.Points == 11), "an archetype gives slightly more, 11 points");
            var fighter = CharacterStart.Archetypes[0];
            Assert.AreEqual(5, fighter.Added[CharacterAttribute.Strength]);
            Assert.AreEqual(4, fighter.Added[CharacterAttribute.Endurance]);
            Assert.AreEqual(2, fighter.Added[CharacterAttribute.Dexterity]);
            Assert.IsTrue(CharacterStart.Archetypes.All(a => a.Added[CharacterAttribute.Dexterity] == 2),
                "every archetype reaches Dexterity 3, a firearm's and leather's requirement");
        }

        [Test]
        public void NinePointsOrAnArchetypeExactly()
        {
            var sheet = new CharacterSheet();
            var eight = new Dictionary<CharacterAttribute, int> { [CharacterAttribute.Dexterity] = 4, [CharacterAttribute.Will] = 4 };
            Assert.IsFalse(CharacterStart.Apply(sheet, eight, null), "8 points are not all 9");
            Assert.AreEqual(1, sheet.Get(CharacterAttribute.Dexterity), "a refused start leaves the sheet as it was");

            var nine = new Dictionary<CharacterAttribute, int> { [CharacterAttribute.Dexterity] = 4, [CharacterAttribute.Will] = 5 };
            Assert.IsTrue(CharacterStart.Apply(sheet, nine, null));
            Assert.AreEqual(5, sheet.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual(6, sheet.Get(CharacterAttribute.Will));
            Assert.AreEqual(1, sheet.Get(CharacterAttribute.Luck));
            Assert.AreEqual(1, sheet.Get(CharacterSkill.LightFirearms), "skills stay at 1");

            var sleuth = CharacterStart.Archetypes[1];
            Assert.IsFalse(CharacterStart.Apply(sheet, nine, sleuth), "an archetype's points are its own spread");
            var spread = sleuth.Added.ToDictionary(p => p.Key, p => p.Value);
            Assert.IsTrue(CharacterStart.Apply(sheet, spread, sleuth));
            Assert.AreEqual(6, sheet.Get(CharacterAttribute.Perception));
            Assert.AreEqual(5, sheet.Get(CharacterAttribute.Intelligence));
            Assert.AreEqual(3, sheet.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual(1, sheet.Get(CharacterAttribute.Will), "a start sets every attribute anew");
        }

        [Test]
        public void ANameIsKeptClean()
        {
            Assert.AreEqual(string.Empty, CharacterStart.CleanName("   "));
            Assert.AreEqual("Мира Вейл", CharacterStart.CleanName("  Мира   Вейл "));
            Assert.AreEqual(CharacterStart.NameLength, CharacterStart.CleanName(new string('а', 40)).Length);
        }

        [Test]
        public void ThreeBodiesOfEachSexHoldTheirTools()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var looks = settings.PlayerLooks;
            Assert.AreEqual(SlicePlayerBodies.Offered.Length, looks.Length, "SlicePlayerBodies sets the list");
            Assert.AreEqual(3, looks.Count(l => !l.female));
            Assert.AreEqual(3, looks.Count(l => l.female));
            Assert.AreEqual(looks.Length, looks.Select(l => l.id).Distinct().Count(), "each id once");
            Assert.AreEqual("man", looks[0].id, "the first man's body is the one the scenes' player wears");
            foreach (var look in looks)
            {
                var body = look.Body;
                Assert.IsNotNull(body, $"{look.id} has a Model");
                var animator = body.GetComponent<Animator>();
                Assert.IsNotNull(animator, $"{look.id} is animated");
                Assert.IsTrue(animator.avatar != null && animator.avatar.isHuman, $"{look.id} is Humanoid");
                var controller = animator.runtimeAnimatorController;
                Assert.IsNotNull(controller, $"{look.id} plays the player's controller");
                Assert.AreEqual(look.female, controller is AnimatorOverrideController, $"{look.id}: a woman plays the women's takes");
                foreach (var tool in new[] { PlayerWork.AxeTool, PlayerWork.PickaxeTool, PlayerWork.HammerTool })
                {
                    var held = body.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == tool);
                    Assert.IsNotNull(held, $"{look.id} holds {tool}");
                    Assert.IsFalse(held.gameObject.activeSelf, $"{look.id}'s {tool} is put away");
                }
            }
        }
    }
}
