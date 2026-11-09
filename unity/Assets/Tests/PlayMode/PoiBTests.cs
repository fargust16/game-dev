#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S2.6's PlayMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN) in Veilmouth: POI B's scene stands on Loom Lane only
    /// once the player enters its area within the night window (04 *Lazy activation*), and the snatch is settled by two
    /// Approaches without a fight: by Convince, the lane's people swarming, and in a fresh world by Exchange, a tip
    /// promised to a Warden. The clock is held while the player talks, so a check rolls at a known tick.
    /// </summary>
    public sealed class PoiBTests : SliceInputTestBase
    {
        private const long Night = (23 * 60) + 30;
        private string _language;
        private IDisposable _hold;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
            Loc.Language = Loc.English;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            _hold?.Dispose();
            _hold = null;
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator TheSnatchAppearsAtNightAndIsSettledByConvince()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "b");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();

            yield return GoFar(player, stage);
            Assert.IsFalse(stage.IsShown, "nothing stands there before the eleventh bell");
            world.Clock.AdvanceTicks((int)(Night - world.Clock.Tick));
            _hold = world.Clock.Pause();
            yield return null;
            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(stage.Poi));
            Assert.IsFalse(stage.IsShown, "nor while the player is away (lazy activation)");

            // The board's notice of girls gone missing, read in the day, is the Argument for the lane's people.
            world.Tips.Read(TipSource.NoticeBoard, world.Clock.Tick);
            Assert.IsTrue(world.State.IsSet("tip.board.girls.heard"));

            yield return Before(player, stage);
            Assert.IsTrue(stage.IsShown, "the scene stands once the player enters its area");
            Assert.AreEqual(Loc.Get("ui.prompt.talk"), hud.PromptText);

            // Values high enough that the check stands at its ceiling, 35/36, at the tick the clock is held on.
            world.SetCharacterValues(100);
            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsDialogueOpen);
            CollectionAssert.AreEqual(new[] { Loc.Get("dialogue.poib.scene") }, hud.DialogueLines);
            yield return Tap(Key.Digit1);
            StringAssert.Contains(Loc.Get("dialogue.poib.lane.swarm"), hud.DialogueOptions[0], "the board's notice is an Argument for Convince");
            Assert.IsFalse(hud.DialogueOptions.Any(o => o.Contains(Loc.Get("dialogue.poib.lane.threaten"))), "the coach-coats are not heard yet");

            yield return Tap(Key.Digit1);

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stage.Poi), "B settled by Convince");
            Assert.IsTrue(world.State.IsSet("aftermath.loom_lane.girls_freed"));
            Assert.IsTrue(world.State.IsSet("against.veinmarket"), "Veinmarket will remember who stood in its way");
            Assert.IsFalse(world.State.IsSet("poi.b.fled"), "no fight");
            Assert.AreEqual(Loc.Get("dialogue.poib.freed"), hud.DialogueLines.Last(), "the outcome is still read");
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
            Assert.IsFalse(hud.IsPromptVisible && hud.PromptText == Loc.Get("ui.prompt.talk"), "a settled scene has nothing more to say");
            Assert.IsTrue(stage.IsShown, "its people do not vanish before the player's eyes");

            yield return GoFar(player, stage);
            Assert.IsFalse(stage.IsShown, "they leave once the player is away");
        }

        [UnityTest]
        public IEnumerator TheSnatchIsSettledByExchange()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "b");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            world.Clock.AdvanceTicks((int)(Night - world.Clock.Tick));
            yield return Before(player, stage);
            _hold = world.Clock.Pause();

            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            Assert.AreEqual("2. " + Loc.Get("dialogue.poib.lane.run"), hud.DialogueOptions[1]);
            yield return Tap(Key.Digit2);
            Assert.AreEqual("1. " + Loc.Get("dialogue.poib.warden.promise_tip"), hud.DialogueOptions[0], "nothing heard: no Argument to urge the Warden with");
            yield return Tap(Key.Digit1);

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stage.Poi), "B settled by Exchange: a tip promised");
            Assert.IsTrue(world.State.IsSet("debt.wardens"), "the Wardens owe a fighter at the invasion (11)");
            Assert.IsTrue(world.State.IsSet("promise.wardens.tip"));
            Assert.IsTrue(world.State.IsSet("aftermath.loom_lane.snatch_spoiled"));
            Assert.IsFalse(world.State.IsSet("against.veinmarket"));
            Assert.AreEqual(Loc.Get("dialogue.poib.whistle"), hud.DialogueLines.Last());
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
        }

        /// <summary>The snatch drawn into <c>Logs/SliceDialogue/</c>: from the player's camera, and from along the lane.</summary>
        [UnityTest, Explicit]
        public IEnumerator RenderTheSnatch()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "b");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.Clock.AdvanceTicks((int)(Night - world.Clock.Tick));
            yield return Before(player, stage);
            yield return new WaitForSeconds(1f);
            Shoot(Camera.main, "poib_player.png");

            var eye = new GameObject("Eye").AddComponent<Camera>();
            eye.CopyFrom(Camera.main);
            var lane = stage.transform.position + (stage.transform.forward * 7f) + (Vector3.up * 3f);
            eye.transform.SetPositionAndRotation(lane, Quaternion.LookRotation(stage.transform.position + Vector3.up - lane));
            Shoot(eye, "poib_lane.png");
            var above = stage.transform.position + (Vector3.up * 14f) + (stage.transform.forward * 0.1f);
            eye.transform.SetPositionAndRotation(above, Quaternion.LookRotation(stage.transform.position - above, -stage.transform.forward));
            Shoot(eye, "poib_above.png");
            Object.Destroy(eye.gameObject);
        }

        private static void Shoot(Camera camera, string name)
        {
            var texture = new RenderTexture(1600, 900, 24);
            var target = camera.targetTexture;
            camera.targetTexture = texture;
            camera.Render();
            camera.targetTexture = target;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory("Logs/SliceDialogue");
            System.IO.File.WriteAllBytes("Logs/SliceDialogue/" + name, image.EncodeToPNG());
            Debug.Log("[RenderTheSnatch] Logs/SliceDialogue/" + name);
        }

        /// <summary>The player before Kit on the Market square, out of the scene's area.</summary>
        private static IEnumerator GoFar(PlayerLocomotion player, PoiStage stage)
        {
            var kit = Object.FindObjectsByType<PersonTalk>().Single(t => t.Dialogue != null && t.Dialogue.Id == "kit");
            var place = kit.transform.position + (kit.transform.forward * 1.5f) + (Vector3.up * 0.1f);
            Assert.IsFalse(stage.Within(place), "the Market square stands out of the scene's area");
            player.Teleport(place, player.transform.rotation);
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>The player on the cobbles of the lane, within the scene's talk, wherever the people leave room.</summary>
        private static IEnumerator Before(PlayerLocomotion player, PoiStage stage)
        {
            var talk = stage.Talk;
            var notCharacters = ~LayerMask.GetMask("Character");
            Vector3? found = null;
            foreach (var distance in new[] { 1.4f, 1.8f, 2.2f, 2.6f })
            {
                for (var angle = 0f; angle < 360f && found == null; angle += 30f)
                {
                    var along = Quaternion.Euler(0f, angle, 0f) * stage.transform.forward;
                    var place = stage.transform.position + (along * distance);
                    if (Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 2f, notCharacters, QueryTriggerInteraction.Ignore)
                        && !Physics.CheckCapsule(floor.point + (Vector3.up * 0.4f), floor.point + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && talk.InReach(floor.point))
                    {
                        found = floor.point;
                    }
                }
            }

            Assert.IsTrue(found.HasValue, $"a free place before the scene at {stage.transform.position}");
            player.Teleport(found.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(Flat(stage.transform.position - found.Value)));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(talk.InReach(player.transform.position), Describe(player));
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }
    }
}
#endif
