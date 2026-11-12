#if UNITY_EDITOR
using System.Collections;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.1 in Veilmouth: the character starts with plain clothes and 500 bronze, and I shows the bag page.</summary>
    public sealed class BagPlayTests : SliceInputTestBase
    {
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator IShowsTheBagAndItFollowsThePurse()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Bag, "the world carries the character's bag");
            Assert.AreEqual(SliceWorldDriver.StartingBronze, world.Bag.Bronze);

            var hud = Object.FindAnyObjectByType<HudView>();
            yield return Tap(Key.I);

            Assert.IsTrue(hud.IsBagOpen, "I opens the bag");
            Assert.AreEqual("Money: 5 silver", hud.BagLines[0]);
            CollectionAssert.Contains(hud.BagLines, "Plain clothes (Normal), 1.5 kg · worn", string.Join(" | ", hud.BagLines));
            Assert.AreEqual(Loc.Get("ui.hud.bag.hint"), hud.BagNote);

            // S5a.2: a click takes the clothes off and puts them on again; with no weapon the character fights with fists.
            Assert.IsTrue(hud.ClickBagLine(0));
            yield return null;

            CollectionAssert.Contains(hud.BagLines, "Plain clothes (Normal), 1.5 kg", string.Join(" | ", hud.BagLines));
            Assert.IsFalse(world.Outfit.IsWorn(world.Bag.Pieces[0]));
            Assert.IsTrue(hud.ClickBagLine(0));
            yield return null;

            Assert.IsTrue(world.Outfit.IsWorn(world.Bag.Pieces[0]));
            Assert.AreEqual("Fists", world.Fights.PlayerFighter().Weapon.name);

            world.Bag.Earn(75);
            yield return null;

            Assert.AreEqual("Money: 5 silver 75 bronze", hud.BagLines[0], "an open page follows the purse");

            yield return Tap(Key.J);

            Assert.IsTrue(hud.IsNotesOpen);
            Assert.IsFalse(hud.IsBagOpen, "the notes close the bag");

            yield return Tap(Key.I);
            yield return Tap(Key.I);

            Assert.IsFalse(hud.IsBagOpen, "I again closes it");
        }

        /// <summary>
        /// Esc (owner, 2026-11-12): any open window closes; with none, the menu opens and the world stops under it; Esc
        /// again, or Resume, closes it and the world goes on.
        /// </summary>
        [UnityTest]
        public IEnumerator EscClosesAnyWindowAndOpensTheMenu()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsFalse(hud.AnyWindowOpen);

            yield return Tap(Key.I);
            Assert.IsTrue(hud.IsBagOpen);
            yield return Tap(Key.Escape);
            Assert.IsFalse(hud.IsBagOpen, "Esc closes the bag");
            Assert.IsFalse(hud.IsMenuOpen, "and opens nothing behind it");

            yield return Tap(Key.J);
            Assert.IsTrue(hud.IsNotesOpen);
            yield return Tap(Key.Escape);
            Assert.IsFalse(hud.IsNotesOpen, "Esc closes the notes");
            Assert.IsFalse(hud.IsMenuOpen);

            yield return Tap(Key.Escape);
            Assert.IsTrue(hud.IsMenuOpen, "with no window, Esc opens the menu");
            CollectionAssert.AreEqual(new[] { "Resume", "Change the language", "Quit the game" }, hud.MenuLines);
            Assert.AreEqual(0f, Time.timeScale, "the world stops under the menu");
            Assert.IsTrue(world.Clock.IsPaused, "its clock too");

            yield return Tap(Key.Escape);
            Assert.IsFalse(hud.IsMenuOpen, "Esc again closes it");
            Assert.AreEqual(1f, Time.timeScale, "and the world goes on");
            Assert.IsFalse(world.Clock.IsPaused);

            yield return Tap(Key.Escape);
            Assert.IsTrue(hud.IsMenuOpen);
            Assert.IsTrue(hud.ClickMenuLine(0), "Resume");
            yield return null;
            Assert.IsFalse(hud.IsMenuOpen, "Resume closes it");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(world.Clock.IsPaused);
        }
    }
}
#endif
