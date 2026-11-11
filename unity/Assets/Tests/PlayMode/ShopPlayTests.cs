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
    }
}
#endif
