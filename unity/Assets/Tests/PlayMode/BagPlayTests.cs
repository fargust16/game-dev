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
            CollectionAssert.Contains(hud.BagLines, "Plain clothes (Normal), 1.5 kg", string.Join(" | ", hud.BagLines));

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
    }
}
#endif
