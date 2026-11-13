#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.Slice.Creation;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.2 (07.1 §3; owner, 2026-11-13, S5-1): every level a use reaches is a Screen Message, and L opens the Character
    /// Page with the attributes and skills, their levels and the way to the next; L again closes it.
    /// </summary>
    public sealed class GrowthPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator ALevelIsSaidAndThePageShowsTheWayToTheNext()
        {
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsNotNull(world.Page, "the Character Page is the world's");

            world.Character.Train(CharacterSkill.LightFirearms, 9, CheckOutcome.Normal, new[] { CharacterAttribute.Dexterity });
            Assert.AreEqual(Loc.Format("ui.character.grew", PersonTalk.SkillName(CharacterSkill.LightFirearms), 2), hud.MessageText,
                "the level reached is a Screen Message");
            world.Character.Train(CharacterSkill.Mining, 9, CheckOutcome.Failure, new[] { CharacterAttribute.Strength });

            yield return Tap(Key.L);
            yield return null;
            Assert.IsTrue(world.Page.IsOpen, "L opens the page");
            var lines = hud.ShopLines.ToList();
            var dexterity = world.Character.Get(CharacterAttribute.Dexterity);
            Assert.That(lines, Has.Some.EqualTo(CharacterPage.Line(CharacterPage.AttributeName(CharacterAttribute.Dexterity), dexterity,
                world.Character.Xp(CharacterAttribute.Dexterity))));
            Assert.That(lines, Has.Some.EqualTo(Loc.Format("ui.character.line", PersonTalk.SkillName(CharacterSkill.Mining), 1, 50)),
                "a failed swing: 5 XP of 10, half the way");
            Assert.That(lines, Has.Some.EqualTo(Loc.Get("ui.character.uninitiated")));

            yield return Tap(Key.L);
            yield return null;
            Assert.IsFalse(world.Page.IsOpen, "L again closes it");
            Assert.IsFalse(hud.IsShopOpen);
        }
    }
}
#endif
