#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Shops;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S5a.3 in Veilmouth: E at Ada Merrow's counter opens her shop, a click buys herbs, and E closes it.</summary>
    public sealed class ShopPlayTests : SliceInputTestBase
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
        public IEnumerator AMastersCounterSellsWhileOpen()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Shops, "the world keeps the shops");
            var counters = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Exclude);
            CollectionAssert.AreEquivalent(new[] { "edda", "silas", "ada", "tobias" }, counters.Select(c => c.Keeper));
            if (world.Clock.MinuteOfDay < 9 * 60)
            {
                world.Clock.AdvanceTicks(9 * 60 - world.Clock.MinuteOfDay);
            }

            var ada = counters.Single(c => c.Keeper == "ada");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(ada.transform.position + (ada.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);
            yield return new WaitForSeconds(0.3f);

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual(Loc.Get("ui.prompt.trade"), hud.PromptText, Describe(player));
            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsShopOpen, "E opens the shop");
            Assert.AreEqual("Ada Merrow", hud.ShopTitle);
            var lines = hud.ShopLines;
            Assert.AreEqual("For sale", lines[0]);
            var herbs = lines.ToList().FindIndex(l => l.StartsWith("Herbs × "));
            Assert.Greater(herbs, 0, string.Join(" | ", lines));
            CollectionAssert.Contains(lines, "First Aid — 10 silver");

            Assert.IsTrue(hud.ClickShopLine(herbs));
            yield return null;

            Assert.AreEqual(1, world.Bag.Count("herb"));
            Assert.AreEqual(400, world.Bag.Bronze);
            Assert.AreEqual("Bought: Herbs.", hud.ShopNote);
            CollectionAssert.Contains(hud.ShopLines, "Yours to sell");

            yield return Tap(Key.E);

            Assert.IsFalse(hud.IsShopOpen, "E again closes it");
        }

        /// <summary>S5a.4: at Edda Brann's counter one who learned Item Creation makes a dagger by a click, ten ticks passing.</summary>
        [UnityTest]
        public IEnumerator AWorkshopMakesAPieceOnAClick()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Workshop, "the world keeps the workshop");
            if (world.Clock.MinuteOfDay < 9 * 60)
            {
                world.Clock.AdvanceTicks(9 * 60 - world.Clock.MinuteOfDay);
            }

            world.State.Set(SliceShops.LessonKey(SliceWorkshop.Lesson), 1);
            world.Bag.Add("iron_ore", 2);
            world.Bag.Add("wood");

            var edda = Object.FindObjectsByType<ShopCounter>(FindObjectsInactive.Exclude).Single(c => c.Keeper == "edda");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(edda.transform.position + (edda.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(edda.InReach(player.transform.position), Describe(player));
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsTrue(edda.Open());
            yield return null;

            var lines = hud.ShopLines.ToList();
            CollectionAssert.Contains(lines, "Workshop: 1 silver a piece");
            var dagger = lines.IndexOf("Make Dagger: Iron ore × 2, Wood × 1 · 10 min");
            Assert.Greater(dagger, 0, string.Join(" | ", lines));
            var tick = world.Clock.Tick;
            Assert.IsTrue(hud.ClickShopLine(dagger));
            yield return null;

            Assert.GreaterOrEqual(world.Clock.Tick, tick + 10, "ten ticks pass at the work");
            Assert.IsTrue(world.Bag.Pieces.Any(p => p.item == "dagger"), "the dagger is in the bag");
            Assert.AreEqual(0, world.Bag.Count("iron_ore"));
            Assert.AreEqual(400, world.Bag.Bronze, "the workshop's 100 bronze");
            StringAssert.StartsWith("Made: Dagger (", hud.ShopNote);
            edda.Close();
        }
    }
}
#endif
