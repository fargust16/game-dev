#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Items;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Alchemy;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5b.5 (20 §3 *Drinking a Limit potion*; 05.1): in Veilmouth a Limit potion is drunk from the bag, asked first; its 60
    /// ticks of Assimilation pass with the character crouched and the world going on, a save waits, a fight would find it
    /// Stunned, and the character reaches Limit 9 and then 8.
    /// </summary>
    public sealed class AdvancementPlayTests : SliceInputTestBase
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
        public IEnumerator ACharacterReachesLimitNineAndThenEight()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world?.Advancement, "the world keeps the Limits");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var sanity = world.Sanity.Value;
            foreach (var attribute in new[] { CharacterAttribute.Perception, CharacterAttribute.Intelligence, CharacterAttribute.Will })
            {
                world.Character.Set(attribute, 1); // as a new character starts
            }

            var nine = new ItemPiece("potion_priestess_9") { perfect = 6 };
            world.Bag.AddPiece(nine);
            yield return Tap(Key.I);
            Assert.IsTrue(hud.IsBagOpen);
            var line = world.Bag.Stacks.Count + world.Bag.Pieces.ToList().IndexOf(nine);
            Assert.IsTrue(hud.ClickBagLine(line));
            yield return null;
            Assert.AreEqual(Loc.Get("ui.drink.ask"), hud.BagNote, "the first click asks, warning of the hour defenceless");
            Assert.IsFalse(world.Advancement.Assimilating);
            Assert.IsTrue(hud.ClickBagLine(line));
            yield return null;

            Assert.IsFalse(hud.IsBagOpen, "the bag closes");
            Assert.AreEqual(Loc.Get("ui.drink.drunk"), hud.MessageText);
            Assert.IsFalse(world.Bag.Pieces.Contains(nine), "the potion is drunk");
            Assert.IsTrue(world.Advancement.Assimilating);
            Assert.AreEqual("ui.save.refused.settling", world.Save.RefusalKey(), "a save waits while a potion settles");
            Assert.AreEqual(world.Advancement.AssimilationLeft, world.Fights.PlayerFighter().Defenceless,
                "a fight now would find it Stunned for the ticks left");

            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(world.Assimilation.Running, "the ticks pass");
            Assert.IsFalse(player.enabled, "the character cannot move");
            Assert.IsTrue(player.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Yield"), "it crouches where it drank");
            var tick = world.Clock.Tick;
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(world.Clock.Tick, tick + 2, "quickly, the world going on");

            yield return Settled(world);
            Assert.AreEqual((Arcana.Priestess, (int?)9), (world.Advancement.CurrentArcana, world.Advancement.CurrentLimit));
            Assert.AreEqual(1 + 3 + 2, world.Character.Get(CharacterAttribute.Perception));
            Assert.AreEqual(sanity - 13, world.Sanity.Value, "10 × 0.4 and the shortfall's 9");
            Assert.AreEqual("The potion has settled. The High Priestess, Limit 9.", hud.MessageText);
            Assert.IsTrue(player.enabled, "the character stands again");
            Assert.IsNull(world.Save.RefusalKey());

            var eight = new ItemPiece("potion_priestess_8", 8);
            world.Bag.AddPiece(eight);
            Assert.AreEqual("ui.drink.fault.ready", world.Advancement.Fault(eight, world.Bag.Catalogue.Find(eight.item)));
            world.State.Set(SliceAdvancement.ConductKey, SliceAdvancement.ConductForLimit8);
            Assert.IsTrue(world.Advancement.Drink(eight, world.Bag.Catalogue.Find(eight.item)));
            yield return Settled(world);
            Assert.AreEqual(8, world.Advancement.CurrentLimit);
            Assert.AreEqual(6 + 4, world.Character.Get(CharacterAttribute.Perception));
        }

        /// <summary>Waits out a potion's 60 ticks, six seconds at the tuning page's default.</summary>
        private static IEnumerator Settled(SliceWorldDriver world)
        {
            var until = Time.realtimeSinceStartup + 20f;
            while (world.Advancement.Assimilating && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(world.Advancement.Assimilating, "the potion settles");
            Assert.IsFalse(world.Assimilation.Running);
        }
    }
}
#endif
