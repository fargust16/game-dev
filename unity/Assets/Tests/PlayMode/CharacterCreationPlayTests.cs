#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Input;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Creation;
using GameDev.Slice.Save;
using GameDev.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.1 (07 *Creation choices in V1*, owner 2026-11-13, S5-4): a new run starts from the creation page, the world
    /// standing still and every game key off under it; the name, the sex, a body, an archetype or 9 free points, and
    /// "Begin" sets the sheet, wears the body and lets the world go; the body comes back with a Suspend Save.
    /// </summary>
    public sealed class CharacterCreationPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [SetUp]
        public void OfferCreation()
        {
            CharacterCreation.Forced = true;
            SliceSaveSlot.Path = Path.Combine(Application.temporaryCachePath, "suspend-creation-test.json");
            SliceSaveSlot.Delete();
        }

        [TearDown]
        public void Forget()
        {
            CharacterCreation.Forced = false;
            SliceSaveSlot.Delete();
            SliceSaveSlot.Path = null;
            Time.timeScale = 1f;
            if (GameInputService.Instance != null)
            {
                GameInputService.Instance.Typing = false;
            }
        }

        [UnityTest]
        public IEnumerator ANewRunStartsFromTheCreationPage()
        {
            yield return BeginRun();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsTrue(world.Creation.IsOpen, "the creation page is up");
            Assert.IsTrue(hud.IsShopOpen);
            Assert.AreEqual(0f, Time.timeScale, "the world stands still under it");
            Assert.IsTrue(GameInputService.Instance.Typing, "every game key is off, so a letter opens no page");
            Assert.IsFalse(player.enabled, "the character stands");
            Assert.IsNotNull(hud.ShopFieldText, "the name's field is shown");

            Assert.IsTrue(Click(hud, "ui.create.begin"));
            Assert.IsTrue(world.Creation.IsOpen, "no name, no beginning");
            Assert.AreEqual(Loc.Get("ui.create.note.name"), hud.ShopNote);

            Assert.IsTrue(hud.TypeShopField("  Мира   Вейл "));
            Assert.IsTrue(Click(hud, "ui.create.sex.woman"));
            var looks = world.Creation.Looks;
            Assert.AreEqual(3, looks.Count, "three bodies of a woman");
            Assert.IsTrue(ClickText(hud, Loc.Format("ui.create.look.line", 2, 3)));
            Assert.IsTrue(Click(hud, "ui.create.archetype.sleuth"));
            yield return null;
            Assert.AreSame(looks[1].Body.GetComponent<Animator>().avatar, player.GetComponentInChildren<Animator>().avatar, "the body chosen is worn at once");

            Assert.IsTrue(Click(hud, "ui.create.begin"));
            Assert.IsFalse(world.Creation.IsOpen, "the life begins");
            Assert.IsFalse(hud.IsShopOpen);
            Assert.AreEqual(1f, Time.timeScale, "the world goes on");
            Assert.IsFalse(GameInputService.Instance.Typing);
            Assert.IsTrue(player.enabled);
            Assert.AreEqual(6, world.Character.Get(CharacterAttribute.Perception));
            Assert.AreEqual(5, world.Character.Get(CharacterAttribute.Intelligence));
            Assert.AreEqual(3, world.Character.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual(1, world.Character.Get(CharacterAttribute.Strength));
            Assert.AreEqual("Мира Вейл", world.Identity.name);
            Assert.IsTrue(world.Identity.female);
            Assert.AreEqual(looks[1].id, world.Identity.look);

            yield return null;
            var bodies = player.GetComponentsInChildren<Animator>(true);
            Assert.AreEqual(1, bodies.Length, "the old body is gone");
            Assert.AreSame(looks[1].Body.GetComponent<Animator>().avatar, bodies[0].avatar);
            Assert.IsNotNull(PlayerWork.Tool(player.transform, PlayerWork.AxeTool), "the new body holds the tools");
        }

        [UnityTest]
        public IEnumerator EveryFreePointMustBeSpent()
        {
            yield return BeginRun();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            hud.TypeShopField("Том");
            var strength = Loc.Format("ui.create.attribute.line", Loc.Get("attribute.strength"), 1);
            Assert.IsTrue(ClickText(hud, strength));
            for (var i = 2; i <= 8; i++)
            {
                Assert.IsTrue(ClickText(hud, Loc.Format("ui.create.attribute.line", Loc.Get("attribute.strength"), i)));
            }

            Assert.AreEqual(1, world.Creation.PointsLeft);
            Assert.IsTrue(Click(hud, "ui.create.begin"));
            Assert.IsTrue(world.Creation.IsOpen, "a point is left");
            Assert.AreEqual(Loc.Format("ui.create.note.points", 1), hud.ShopNote);

            Assert.IsTrue(ClickText(hud, Loc.Format("ui.create.attribute.line", Loc.Get("attribute.dexterity"), 1)));
            Assert.IsTrue(ClickText(hud, Loc.Format("ui.create.attribute.line", Loc.Get("attribute.dexterity"), 2)));
            Assert.AreEqual(Loc.Get("ui.create.note.spent"), hud.ShopNote, "no tenth point");
            Assert.IsTrue(Click(hud, "ui.create.begin"));
            Assert.IsFalse(world.Creation.IsOpen);
            Assert.AreEqual(9, world.Character.Get(CharacterAttribute.Strength));
            Assert.AreEqual(2, world.Character.Get(CharacterAttribute.Dexterity));
            Assert.AreEqual("Том", world.Identity.name);
            Assert.IsFalse(world.Identity.female);
        }

        [UnityTest]
        public IEnumerator TheBodyComesBackWithTheSave()
        {
            yield return BeginRun();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            hud.TypeShopField("Нелл");
            Assert.IsTrue(Click(hud, "ui.create.sex.woman"));
            Assert.IsTrue(ClickText(hud, Loc.Format("ui.create.look.line", 3, 3)));
            Assert.IsTrue(Click(hud, "ui.create.archetype.ragamuffin"));
            Assert.IsTrue(Click(hud, "ui.create.begin"));
            var chosen = world.Identity.look;
            var avatar = world.Creation.Find(chosen).Body.GetComponent<Animator>().avatar;
            yield return null;
            yield return null;

            Assert.IsNull(world.Save.Write());
            world.Identity = new CharacterIdentity { name = "someone else", look = "man" };
            yield return world.Save.Load(SliceSaveSlot.Read());

            Assert.AreEqual("Нелл", world.Identity.name);
            Assert.AreEqual(chosen, world.Identity.look);
            Assert.AreEqual(6, world.Character.Get(CharacterAttribute.Will));
            yield return null;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var bodies = player.GetComponentsInChildren<Animator>(true);
            Assert.AreEqual(1, bodies.Length);
            Assert.AreSame(avatar, bodies[0].avatar, "the loaded city's player wears the body chosen");
        }

        /// <summary>A run from its start: no world before it, the city loaded, the creation page opened at its first frames.</summary>
        private static IEnumerator BeginRun()
        {
            yield return ForgetTheWorld();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Veilmouth, new LoadSceneParameters(LoadSceneMode.Single));

            // Real time: the page stops the frame's time.
            yield return new WaitForSecondsRealtime(0.5f);
            DebugSight.On = false;
        }

        private static bool Click(HudView hud, string key) => ClickText(hud, Loc.Get(key));

        /// <summary>Clicks the page's first line that holds <paramref name="text"/>, as the mouse does.</summary>
        private static bool ClickText(HudView hud, string text)
        {
            var lines = hud.ShopLines.ToList();
            var index = lines.FindIndex(l => l.Contains(text));
            return index >= 0 && hud.ClickShopLine(index);
        }
    }
}
#endif
