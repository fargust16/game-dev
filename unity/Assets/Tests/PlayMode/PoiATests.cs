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
    /// S2.5's PlayMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN) in Veilmouth: POI A's scene stands before
    /// Harrow &amp; Tincture only once the player enters its area within the window (04 *Lazy activation*), and the arrest
    /// is settled by Convince and, in a fresh world, by Exchange. The clock is held while the player talks, so a check
    /// rolls at a known tick.
    /// </summary>
    public sealed class PoiATests : SliceInputTestBase
    {
        private const long Afternoon = 15 * 60;
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
        public IEnumerator TheArrestAppearsWhenThePlayerComesAndIsSettledByConvince()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "a");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();

            yield return GoFar(player, stage);
            Assert.IsFalse(stage.IsShown, "nothing stands there before the window");
            world.Clock.AdvanceTicks((int)(Afternoon - world.Clock.Tick));
            _hold = world.Clock.Pause();
            yield return null;
            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(stage.Poi));
            Assert.IsFalse(stage.IsShown, "nor while the player is away (lazy activation)");

            yield return Before(player, stage);
            Assert.IsTrue(stage.IsShown, "the scene stands once the player enters its area");
            Assert.AreEqual(Loc.Get("ui.prompt.talk"), hud.PromptText);

            // Values high enough that the check stands at its ceiling, 35/36, at the tick the clock is held on.
            world.SetCharacterValues(100);
            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsDialogueOpen);
            CollectionAssert.AreEqual(new[] { Loc.Get("dialogue.poia.scene") }, hud.DialogueLines);
            yield return Tap(Key.Digit1);
            CollectionAssert.AreEqual(new[] { "1. " + Loc.Get("dialogue.poia.street.ask"), "2. " + Loc.Get("dialogue.poia.street.hold"), "3. " + Loc.Get("dialogue.poia.street.walk") },
                hud.DialogueOptions, "the service, walking away, and asking the street; a check needs an Argument");
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("dialogue.poia.neighbours"), hud.DialogueLines.Last());
            yield return Tap(Key.Digit1);
            StringAssert.Contains(Loc.Get("dialogue.poia.street.defend"), hud.DialogueOptions[0], "the neighbours' words are an Argument for Convince");

            yield return Tap(Key.Digit1);

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stage.Poi), "A settled by Convince");
            Assert.IsTrue(world.State.IsSet("debt.veinmarket"), "Veinmarket owes a fighter at the invasion (11)");
            Assert.IsTrue(world.State.IsSet("aftermath.harrow.defended"));
            Assert.AreEqual(Loc.Get("dialogue.poia.freed"), hud.DialogueLines.Last(), "the outcome is still read");
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
            Assert.IsFalse(hud.IsPromptVisible && hud.PromptText == Loc.Get("ui.prompt.talk"), "a settled scene has nothing more to say");
            Assert.IsTrue(stage.IsShown, "its people do not vanish before the player's eyes");

            yield return GoFar(player, stage);
            Assert.IsFalse(stage.IsShown, "they leave once the player is away");
        }

        [UnityTest]
        public IEnumerator TheArrestIsSettledByExchange()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "a");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            world.Clock.AdvanceTicks((int)(Afternoon - world.Clock.Tick));
            yield return Before(player, stage);
            _hold = world.Clock.Pause();

            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            Assert.AreEqual("2. " + Loc.Get("dialogue.poia.street.hold"), hud.DialogueOptions[1]);
            yield return Tap(Key.Digit2);

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stage.Poi), "A settled by Exchange: holding the crowd");
            Assert.IsTrue(world.State.IsSet("debt.wardens"), "the Wardens owe a fighter at the invasion (11)");
            Assert.IsTrue(world.State.IsSet("aftermath.harrow.shop_sealed"));
            Assert.IsFalse(world.State.IsSet("debt.veinmarket"));
            Assert.AreEqual(Loc.Get("dialogue.poia.taken"), hud.DialogueLines.Last());
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
        }

        /// <summary>The arrest drawn into <c>Logs/SliceDialogue/</c>: from the player's camera, and from the street.</summary>
        [UnityTest, Explicit]
        public IEnumerator RenderTheArrest()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var stage = Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == "a");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.Clock.AdvanceTicks((int)(Afternoon - world.Clock.Tick));
            yield return Before(player, stage);
            yield return new WaitForSeconds(1f);
            Shoot(Camera.main, "poia_player.png");

            var eye = new GameObject("Eye").AddComponent<Camera>();
            eye.CopyFrom(Camera.main);
            var street = stage.transform.position + (stage.transform.forward * 9f) + (stage.transform.right * 4f) + (Vector3.up * 3f);
            eye.transform.SetPositionAndRotation(street, Quaternion.LookRotation(stage.transform.position + Vector3.up - street));
            Shoot(eye, "poia_street.png");
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
            Debug.Log("[RenderTheArrest] Logs/SliceDialogue/" + name);
        }

        /// <summary>The player before Kit on the Market square, out of the scene's area (the Notice Board, 25 m away on Bell Street, is in it).</summary>
        private static IEnumerator GoFar(PlayerLocomotion player, PoiStage stage)
        {
            var kit = Object.FindObjectsByType<PersonTalk>().Single(t => t.Dialogue != null && t.Dialogue.Id == "kit");
            var place = kit.transform.position + (kit.transform.forward * 1.5f) + (Vector3.up * 0.1f);
            Assert.IsFalse(stage.Within(place), "the Market square stands out of the scene's area");
            player.Teleport(place, player.transform.rotation);
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>The player on the cobbles before the scene, within its talk.</summary>
        private static IEnumerator Before(PlayerLocomotion player, PoiStage stage)
        {
            var talk = stage.Talk;
            var notCharacters = ~LayerMask.GetMask("Character");
            Vector3? found = null;
            foreach (var distance in new[] { 1.4f, 1.8f, 2.2f, 2.6f })
            {
                foreach (var angle in new[] { 0f, -40f, 40f, -80f, 80f })
                {
                    var along = Quaternion.Euler(0f, angle, 0f) * stage.transform.forward;
                    var place = stage.transform.position + (along * distance);
                    if (found == null
                        && Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 2f, notCharacters, QueryTriggerInteraction.Ignore)
                        && !Physics.CheckCapsule(floor.point + (Vector3.up * 0.4f), floor.point + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && talk.InReach(floor.point))
                    {
                        found = floor.point;
                    }
                }
            }

            Assert.IsTrue(found.HasValue, $"a free place before the scene at {stage.transform.position}");
            player.Teleport(found.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(-stage.transform.forward));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(talk.InReach(player.transform.position), Describe(player));
        }
    }
}
#endif
