#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S2.4's done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN), in the Soot &amp; Bell and in each language: asking Nell with an
    /// Approach marks POI B; and a check's tag shows its chance once the development command raises Intelligence to 10.
    /// </summary>
    public sealed class ConversationTests : SliceInputTestBase
    {
        private const long Evening = 16 * 60;
        private string _language;

        [SetUp]
        public void KeepLanguage()
        {
            _language = Loc.Language;
        }

        [TearDown]
        public void RestoreLanguage()
        {
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator AskingNellWithAnApproachMarksPoiB([Values(Loc.English, Loc.Russian)] string language)
        {
            Loc.Language = language;
            yield return EveningAtTheBar();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            var poiB = world.Director.Pois.Single(p => p.Id == "b");
            Assert.AreEqual(PoiDiscovery.Unknown, world.Director.Discovery(poiB));
            Assert.AreEqual(Loc.Get("ui.prompt.talk"), hud.PromptText);

            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsDialogueOpen, "Interact opens the dialogue column");
            CollectionAssert.AreEqual(new[] { Loc.Get("tip.nell.harrow.line") }, hud.DialogueLines, "her evening tip, said on arriving");
            var options = hud.DialogueOptions;
            Assert.AreEqual(3, options.Count, string.Join(" | ", options));
            var tag = Loc.Format("ui.dialogue.check", PersonTalk.SkillName(CharacterSkill.Intimidation));
            Assert.AreEqual($"1. {tag} {Loc.Get("dialogue.nell.evening.threaten")}", options[0], "a threat names its skill, and at Intelligence 1 no chance");
            Assert.AreEqual("2. " + Loc.Get("dialogue.nell.evening.exchange"), options[1]);
            Assert.AreEqual("3. " + Loc.Get("dialogue.nell.evening.leave"), options[2]);
            var tick = world.Clock.Tick;

            yield return Tap(Key.Digit2);

            Assert.AreEqual(PoiDiscovery.Marked, world.Director.Discovery(poiB), "Nell's Loom Lane tip, given for Exchange, marks POI B");
            Assert.AreEqual(Loc.Get("tip.nell.collectors.line"), hud.DialogueLines.Last());
            CollectionAssert.Contains(hud.DialogueLines, Loc.Get("dialogue.nell.evening.exchange"), "the character's answer stays above");
            Assert.GreaterOrEqual(world.Clock.Tick, tick + 60, "an hour's work behind the bar");
            Assert.IsTrue(world.Clock.IsPaused);
            CollectionAssert.AreEqual(new[] { "1. " + Loc.Get("dialogue.nell.told.thanks") }, hud.DialogueOptions);

            yield return Tap(Key.Digit1);

            Assert.IsFalse(hud.IsDialogueOpen);
            Assert.IsFalse(world.Clock.IsPaused);

            yield return Tap(Key.J);

            CollectionAssert.Contains(hud.NoteLines, Loc.Get("tip.nell.collectors.note"), "the fact is in the Notes");

            yield return Tap(Key.J);
        }

        [UnityTest]
        public IEnumerator FromIntelligenceTenACheckShowsItsChance()
        {
            Loc.Language = Loc.English;
            yield return EveningAtTheBar();
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();

            yield return Tap(Key.F8);

            Assert.AreEqual(10, world.Character.Get(CharacterAttribute.Intelligence), "the development command's first step");

            yield return Tap(Key.E);

            StringAssert.StartsWith("1. [Intimidation: ", hud.DialogueOptions[0]);
            StringAssert.Contains(" %] ", hud.DialogueOptions[0]);

            yield return Tap(Key.Escape);
            yield return Tap(Key.F8);

            Assert.AreEqual(20, world.Character.Get(CharacterAttribute.Intelligence));

            yield return Tap(Key.E);

            StringAssert.Contains("(Attitude ", hud.DialogueOptions[0], "from 20 the modifiers behind the chance");

            yield return Tap(Key.Escape);
        }

        /// <summary>The HUD alone, drawn into <c>Logs/SliceDialogue/</c>: Nell's column at Intelligence 20, open and after Exchange.</summary>
        [UnityTest, Explicit]
        public IEnumerator RenderTheDialogue([Values(Loc.English, Loc.Russian)] string language)
        {
            Loc.Language = language;
            yield return EveningAtTheBar();
            var document = Object.FindAnyObjectByType<HudView>().GetComponent<UnityEngine.UIElements.UIDocument>();
            var texture = new RenderTexture(1920, 1080, 24);
            var panel = Object.Instantiate(document.panelSettings);
            panel.targetTexture = texture;
            panel.clearColor = true;
            panel.colorClearValue = new Color(0.25f, 0.3f, 0.35f);
            document.panelSettings = panel;
            yield return null;
            yield return Tap(Key.F8);
            yield return Tap(Key.F8);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.5f);
            Save(texture, $"open_{language}.png");
            yield return Tap(Key.Digit2);
            yield return new WaitForSeconds(0.5f);
            Save(texture, $"told_{language}.png");
            yield return Tap(Key.Escape);
        }

        private static void Save(RenderTexture texture, string name)
        {
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory("Logs/SliceDialogue");
            System.IO.File.WriteAllBytes("Logs/SliceDialogue/" + name, image.EncodeToPNG());
            Debug.Log("[RenderTheDialogue] Logs/SliceDialogue/" + name);
        }

        /// <summary>A fresh world in the Soot &amp; Bell at 16:00, the player before Nell's bar within her reach.</summary>
        private IEnumerator EveningAtTheBar()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Interiors/SootAndBell.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "the Soot & Bell carries the slice's world for testing");
            Assert.Less(world.Clock.Tick, Evening);
            world.Clock.AdvanceTicks((int)(Evening - world.Clock.Tick));

            var nell = Object.FindObjectsByType<PersonTalk>(FindObjectsSortMode.None).Single(t => t.Dialogue.Id == "nell");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var place = Before(nell);
            Assert.IsTrue(place.HasValue, $"a free place before Nell at {nell.transform.position}");
            player.Teleport(place.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(-nell.transform.forward));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(nell.InReach(player.transform.position), Describe(player));
        }

        /// <summary>The first place on the floor in front of the person, clear for the player and within reach.</summary>
        private static Vector3? Before(PersonTalk person)
        {
            var notCharacters = ~LayerMask.GetMask("Character");
            foreach (var distance in new[] { 1.6f, 2f, 2.4f, 2.8f })
            {
                foreach (var angle in new[] { 0f, -30f, 30f })
                {
                    var along = Quaternion.Euler(0f, angle, 0f) * person.transform.forward;
                    var place = person.transform.position + (along * distance);
                    if (!Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 1.5f, notCharacters, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    var feet = floor.point;
                    if (!Physics.CheckCapsule(feet + (Vector3.up * 0.4f), feet + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && person.InReach(feet))
                    {
                        return feet;
                    }
                }
            }

            return null;
        }
    }
}
#endif
