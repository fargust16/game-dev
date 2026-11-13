#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.People;
using GameDev.Slice.Trial;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S5c.4 (SLICE_TRIAL §1; 19 §6.2) in the scenes: Thomas Wren's door stays locked until Lottie gives his key, her line
    /// about the boards finds the cache under his bed, and Mr Ashgrave's conversations lead from the offer through the
    /// cache handed in and Father Hale's word to Faction Entry, whose gift page opens when the talk ends.
    /// </summary>
    public sealed class TrialPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator LottiesKeyOpensTheDoorAndHerLineFindsTheBoard()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == SliceTrial.DoorId);
            Assert.IsNotNull(door.GetComponent<TrialDoor>(), "the trial locks Thomas Wren's door");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(door.ArrivalPose.position, door.ArrivalPose.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(door.Sealed);
            Assert.AreEqual(Loc.Get("ui.prompt.locked"), hud.PromptText);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual("Veilmouth", SceneManager.GetActiveScene().name, "without the key the door stays shut");

            // Lottie Wren beside her tenement's door: the key, and the line about the boards.
            var lottie = Object.FindObjectsByType<PersonPost>(FindObjectsInactive.Include).First(p => p.Person == SliceTrial.Lottie && p.Post == SliceHours.Lane);
            lottie.gameObject.SetActive(true);
            var talk = lottie.GetComponent<PersonTalk>();
            Assert.IsNotNull(talk, "Lottie is talked to at her door");
            player.Teleport(lottie.transform.position + (lottie.transform.forward * 1.2f), Quaternion.LookRotation(-lottie.transform.forward));
            yield return new WaitForSeconds(0.2f);
            talk.Open();
            Choose(talk, "look");
            Choose(talk, "key_habits");
            Choose(talk, "boards_go");
            Assert.IsFalse(talk.IsOpen);
            Assert.IsTrue(world.Trial.HasThomasKey, "Lottie gives her brother's key");
            Assert.IsTrue(world.State.IsSet(SliceTrial.BoardsArgument), "and an Argument: he hid things under the boards");
            Assert.IsTrue(world.Tips.HasHeard(world.Tips.Find(SliceTrial.MissingTip)), "the note that Thomas Wren is missing");
            lottie.gameObject.SetActive(false);

            player.Teleport(door.ArrivalPose.position, door.ArrivalPose.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Loc.Get("ui.prompt.trial.unlock"), hud.PromptText);
            yield return Tap(Key.E);
            yield return WaitFor("MissingRoom");
            Assert.IsTrue(world.Trial.DoorOpen, "the door is open for good");

            // Lottie's line finds the board with no roll.
            var board = Object.FindAnyObjectByType<TrialCache>();
            Assert.IsNotNull(board);
            var inside = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == "missing-room.inside");
            var towards = inside.ArrivalPose.position - board.transform.position;
            towards.y = 0f;
            player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(board.transform.position + (towards.normalized * 1.7f), Quaternion.LookRotation(-towards));
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(world.Trial.CacheNoticed);
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual(Loc.Get("ui.prompt.trial.board"), hud.PromptText);
            var bronze = world.Bag.Bronze;
            yield return Tap(Key.E);
            yield return null;
            Assert.IsTrue(world.Trial.CacheFound, "the board is lifted");
            Assert.IsTrue(SliceTrial.CacheFormulas.All(f => world.Bag.Count(f) == 1), "both formulas of Император");
            Assert.AreEqual(1, world.Bag.Count(SliceTrial.LetterItem));
            Assert.AreEqual(bronze + SliceTrial.Savings, world.Bag.Bronze, "and Thomas's savings");
        }

        [UnityTest]
        public IEnumerator AshgraveAdmitsTheCharacterAndTheGiftIsTaken()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var house = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).First(p => p.Id == "faction-house.street");
            Assert.IsTrue(SceneTransition.Travel(house.Target));
            yield return WaitFor(SliceWorldDriver.FactionHouseScene);

            var talk = Object.FindObjectsByType<PersonTalk>(FindObjectsInactive.Exclude).First(t => t.Dialogue != null && t.Dialogue.Id == SliceTrial.Ashgrave);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(talk.transform.position + (talk.transform.forward * 1.2f), Quaternion.LookRotation(-talk.transform.forward));
            yield return new WaitForSeconds(0.2f);

            yield return Talk(talk, "work", "accept");
            Assert.IsTrue(world.State.IsSet(SliceTrial.OfferedKey), "Ashgrave offers the trial");
            Assert.IsTrue(world.Tips.HasHeard(world.Tips.Find(SliceTrial.MissingTip)));
            yield return Talk(talk, "thomas", "again_ok");
            Assert.IsFalse(world.State.IsSet(SliceTrial.HandedKey), "with nothing found, Ashgrave says to look again");

            world.State.Raise(SliceTrial.NoticedKey);
            Assert.IsTrue(world.Trial.TakeCache());
            var bronze = world.Bag.Bronze;
            yield return Talk(talk, "cache", "handed_go");
            Assert.IsTrue(world.State.IsSet(SliceTrial.HandedKey));
            Assert.IsTrue(SliceTrial.CacheFormulas.All(f => world.Bag.Count(f) == 0), "Ashgrave takes the formulas");
            Assert.AreEqual(bronze, world.Bag.Bronze, "the coins stay the character's");
            Assert.IsFalse(world.TrialRun.IsPageOpen, "no gift before Faction Entry");

            world.State.Raise(SliceTrial.VouchedKey); // Father Hale's word, his conversation tested as data
            yield return Talk(talk, "vouched", "entry_thanks");
            Assert.IsTrue(world.Trial.IsMember, "Faction Entry");
            Assert.AreEqual(1, world.Bag.Count(SliceTrial.PriestessNine));
            yield return null;
            yield return null;
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsTrue(world.TrialRun.IsPageOpen, "the gift's page opens when the talk ends");
            Assert.AreEqual(Loc.Get("ui.trial.gift.title"), hud.ShopTitle);
            Assert.AreEqual(1 + SliceTrial.GiftCount, hud.ShopLines.Count, "a heading and three items");
            var pieces = world.Bag.Pieces.Count;
            Assert.IsTrue(hud.ClickShopLine(2));
            Assert.IsTrue(world.Trial.GiftTaken);
            Assert.IsFalse(hud.IsShopOpen, "the page closes once a gift is taken");
            Assert.Greater(world.Bag.Pieces.Count, pieces);

            world.Trial.Advanced(Arcana.Priestess, 9);
            yield return Talk(talk, "step", "step_thanks");
            Assert.AreEqual(1, world.Bag.Count(SliceTrial.PriestessEight), "Ashgrave's next step at Limit 9");
            yield return null;
            yield return null;
            Assert.IsFalse(world.TrialRun.IsPageOpen, "the gift is taken: no page");
        }

        /// <summary>Opens the conversation and answers the options <paramref name="ids"/> in turn.</summary>
        private static IEnumerator Talk(PersonTalk talk, params string[] ids)
        {
            talk.Open();
            yield return null;
            foreach (var id in ids)
            {
                Choose(talk, id);
            }

            Assert.IsFalse(talk.IsOpen, "the conversation has ended");
            yield return null;
        }

        private static void Choose(PersonTalk talk, string id)
        {
            Assert.IsTrue(talk.IsOpen, $"{talk.Dialogue.Id} is open for '{id}'");
            var offered = talk.Conversation.Options.Select(o => o.id).ToList();
            var index = offered.IndexOf(id);
            Assert.GreaterOrEqual(index, 0, $"{talk.Dialogue.Id} offers '{id}' among {string.Join(", ", offered)}");
            talk.Choose(index);
        }

        private static IEnumerator WaitFor(string scene)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((SceneManager.GetActiveScene().name != scene || SceneTransition.IsTraveling) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(scene, SceneManager.GetActiveScene().name);
            yield return new WaitForSeconds(0.5f);
        }

        private static IEnumerator Arrive()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }
        }
    }
}
#endif
