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
    /// S4.3's PlayMode done-when (VERTICAL_SLICE_IMPLEMENTATION_PLAN) in the cellar under the Soot Stair, POI C opened by
    /// A's Aftermath: in Stealth Mode, before the stranger's meter fills, Interact at the nook opens the shutters on the
    /// note and the drop; standing in his sight, he steps in and makes the offer himself, and a refusal sends him up
    /// the stair for good. The clock is held while the player talks.
    /// </summary>
    public sealed class PoiCTests : SliceInputTestBase
    {
        private const string Cellar = "Assets/Scenes/Slice/Interiors/SootStairCellar.unity";
        private const long Afternoon = 14 * 60;
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
        public IEnumerator TheNookOpensUnseenAndTheStrangerStepsInWhenHeSees()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Cellar);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "the cellar carries the slice's world for testing");
            var nook = Object.FindAnyObjectByType<NookSpot>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsFalse(nook.Stranger.Cast.activeSelf, "no stranger before C");
            Assert.IsNull(nook.PromptFor(nook.transform.position), "the shutters stay shut before C");

            OpenC(world);
            var place = Before(nook);
            Assert.IsTrue(place.HasValue, $"a free place before the nook at {nook.transform.position}");
            Assert.IsTrue(world.Hiding.SetActive(true), "Stealth Mode, taken at the stair's foot");
            player.Teleport(place.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(Flat(nook.transform.position - place.Value)));
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(nook.Stranger.Cast.activeSelf, "the stranger waits in the circle vault");
            Assert.IsTrue(nook.InReach(player.transform.position), Describe(player));
            Assert.AreEqual(Loc.Get("ui.prompt.open_shutters"), hud.PromptText);
            _hold = world.Clock.Pause();

            yield return Tap(Key.E);

            Assert.AreEqual(NookSpot.NookNode, nook.LastOpened, "his meter has not filled");
            Assert.IsTrue(hud.IsDialogueOpen);
            CollectionAssert.AreEqual(new[] { Loc.Get("dialogue.poic.nook") }, hud.DialogueLines);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("tip.drop.note.line"), hud.DialogueLines.Last(), "the price, before the choice");
            Assert.AreEqual("1. " + Loc.Get("dialogue.poic.note.drink"), hud.DialogueOptions[0]);
            Assert.AreEqual("4. " + Loc.Get("dialogue.poic.note.walk"), hud.DialogueOptions[3]);
            yield return Tap(Key.Digit4);
            Assert.IsFalse(hud.IsDialogueOpen, "closing the shutters to think it over");

            Assert.IsTrue(world.Hiding.SetActive(false));
            yield return null;
            Assert.IsTrue(nook.StrangerSees(player.transform.position), "standing, the player is in his cone and sight: " + Why(world, nook, player));
            yield return Tap(Key.E);

            Assert.AreEqual(NookSpot.StrangerNode, nook.LastOpened);
            CollectionAssert.AreEqual(new[] { Loc.Get("dialogue.poic.stranger") }, hud.DialogueLines);
            CollectionAssert.AreEqual(new[] { "1. " + Loc.Get("dialogue.poic.stranger.hear"), "2. " + Loc.Get("dialogue.poic.stranger.not_now") },
                hud.DialogueOptions, "no Argument, no threat");
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("tip.drop.note.line"), hud.DialogueLines.Last(), "he makes the offer in the note's words");
            var refuse = hud.DialogueOptions.ToList().FindIndex(o => o.EndsWith(Loc.Get("dialogue.poic.offer.s_refuse")));
            Assert.That(refuse, Is.InRange(0, 8), string.Join(" | ", hud.DialogueOptions));
            yield return Tap(Key.Digit1 + refuse);
            Assert.AreEqual(Loc.Get("dialogue.poic.refused"), hud.DialogueLines.Last());
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
            yield return null;

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(nook.Poi), "the refusal settles C");
            Assert.IsTrue(world.State.IsSet("poi.c.refused") && world.State.IsSet(NookSpot.StrangerGone));
            Assert.IsFalse(nook.Stranger.Cast.activeSelf, "he went up the stair");
            Assert.IsNull(nook.PromptFor(player.transform.position), "the shutters are done with");
        }

        /// <summary>14:00 of day 1, POI A settled by the player: its Aftermath opens C (11.1 Slice Linkage Rules 1).</summary>
        private static void OpenC(SliceWorldDriver world)
        {
            Assert.Less(world.Clock.Tick, Afternoon);
            world.Clock.AdvanceTicks((int)(Afternoon - world.Clock.Tick));
            world.Director.Evaluate(world.Clock.Tick);
            var a = world.Director.Pois.Single(p => p.Id == "a");
            var c = world.Director.Pois.Single(p => p.Id == "c");
            world.Director.ResolveParticipated(a);
            world.Director.Evaluate(world.Clock.Tick);
            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(c));
        }

        /// <summary>The first place on the floor out from the nook's wall, clear for the player and within reach.</summary>
        private static Vector3? Before(NookSpot nook)
        {
            var notCharacters = ~LayerMask.GetMask("Character");
            foreach (var distance in new[] { 0.7f, 0.9f, 1.05f })
            {
                foreach (var angle in new[] { 0f, -45f, 45f, -90f, 90f })
                {
                    var place = nook.transform.position - (Quaternion.Euler(0f, angle, 0f) * Flat(nook.transform.forward) * distance);
                    if (!Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 1.5f, notCharacters, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    var feet = floor.point;
                    if (!Physics.CheckCapsule(feet + (Vector3.up * 0.4f), feet + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && nook.InReach(feet))
                    {
                        return feet;
                    }
                }
            }

            return null;
        }

        private static string Why(SliceWorldDriver world, NookSpot nook, PlayerLocomotion player)
        {
            var eyes = nook.Watcher.transform;
            var feet = player.transform.position;
            var from = eyes.position + (Vector3.up * world.Hiding.Rules.EyeHeight);
            var to = feet + (Vector3.up * 1.6f);
            var hits = Physics.RaycastAll(from, (to - from).normalized, (to - from).magnitude, ~0, QueryTriggerInteraction.Ignore)
                .Select(h => $"{h.transform.name}[{LayerMask.LayerToName(h.transform.gameObject.layer)}]@{h.distance:F2}");
            return $"player {feet}, eyes {eyes.position} fwd {eyes.forward}, cone {world.Hiding.Rules.InCone(eyes.position, eyes.forward, feet, 1f)}, "
                + $"shown {nook.Stranger.IsShown}, active {nook.Watcher.isActiveAndEnabled}, hits: {string.Join(", ", hits)}";
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
#endif
