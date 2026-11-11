#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Locomotion;
using GameDev.Slice.Stealth;
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
    /// S4's acceptance (VERTICAL_SLICE_IMPLEMENTATION_PLAN S4.5) through the real scenes and the clock: POI C appears only
    /// through A's or B's Aftermath, its chalk lying on Deaf Boiler Yard only once A has resolved unattended at 20:00;
    /// and the Bargain shows its price before the choice, in the cellar under the Soot Stair, then leaves its residue:
    /// the fog's sight and the Darkness it takes, or the stain of a smashed drop, and the stranger gone on the next visit.
    /// That C also opens after B is held in EditMode (<c>PoiDirectorTests</c>), since A's window always closes before
    /// B's opens.
    /// </summary>
    public sealed class S4AcceptanceTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const string Cellar = "Assets/Scenes/Slice/Interiors/SootStairCellar.unity";
        private const long AOpen = 14 * 60;
        private const long ACloses = 20 * 60;
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
        public IEnumerator PoiCAppearsOnlyThroughAnAftermath()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var a = world.Director.Pois.Single(p => p.Id == "a");
            var c = world.Director.Pois.Single(p => p.Id == "c");
            var chalk = Object.FindObjectsByType<TraceSpot>(FindObjectsSortMode.None).Single(t => t.Tip == "trace.chalk");

            foreach (var minute in new[] { AOpen, ACloses - 1 })
            {
                world.Clock.AdvanceTicks((int)(minute - world.Clock.Tick));
                yield return null;
                yield return null;
                Assert.AreNotEqual(PoiPhase.Open, world.Director.Phase(c), $"no Aftermath at minute {minute}, no C");
                Assert.IsFalse(chalk.Lying, "nothing of C on the yard");
            }

            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(a), "A is open and not yet settled");
            world.Clock.AdvanceTicks(2);
            yield return null;
            yield return null;

            Assert.AreEqual(PoiResolution.Unattended, world.Director.Resolution(a), "nobody came to A");
            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(c), "A's Aftermath opens C");
            Assert.IsTrue(chalk.Lying, "the chalk lies by the cellar door");
            Assert.AreEqual(PoiDiscovery.Unknown, world.Director.Discovery(c));

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var place = Before(chalk.transform, chalk.InReach);
            Assert.IsTrue(place.HasValue, $"a free place by the chalk at {chalk.transform.position}");
            player.Teleport(place.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(Flat(chalk.transform.position - place.Value)));
            yield return new WaitForSeconds(0.3f);
            Assert.IsNotNull(chalk.PromptFor(player.transform.position), Describe(player));
            yield return Tap(Key.E);
            Assert.AreEqual(PoiDiscovery.Marked, world.Director.Discovery(c), "the first Physical Trace marks C");
            yield return Tap(Key.E);
            Assert.IsFalse(hud.IsReadingOpen);
        }

        [UnityTest]
        public IEnumerator TheBargainShowsItsPriceBeforeTheChoiceAndTheFogStays()
        {
            yield return InTheCellarAfterA();
            var world = SliceWorldDriver.Running;
            var nook = Object.FindAnyObjectByType<NookSpot>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var drop = nook.transform.Find("Look/Drop").gameObject;
            Assert.AreEqual(1, world.Hiding.SightPerception, "values 1 before the drop");
            Assert.IsTrue(drop.activeInHierarchy);

            yield return Tap(Key.E);
            CollectionAssert.AreEqual(new[] { "1. " + Loc.Get("dialogue.poic.nook.unfold"), "2. " + Loc.Get("dialogue.poic.nook.close") }, hud.DialogueOptions,
                "no choice before the note");
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("tip.drop.note.line"), hud.DialogueLines.Last(), "the price, said before the choice");
            StringAssert.Contains("no shadow will hide you", hud.DialogueLines.Last());
            Assert.AreEqual("1. " + Loc.Get("dialogue.poic.note.drink"), hud.DialogueOptions[0]);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("dialogue.poic.drank"), hud.DialogueLines.Last(), "the breath rasps once");
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
            yield return null;

            var rules = world.Hiding.Rules;
            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(nook.Poi), "the drink settles C");
            Assert.AreEqual(rules.MeterFromPerception, world.Hiding.SightPerception, "every Notice Meter shown");
            Assert.AreEqual(rules.ConesClearFromIntelligence, world.Hiding.SightIntelligence, "the cones clear");
            Assert.AreEqual(0, world.Hiding.HidingDarkness(player.transform.position), "no shadow hides the character");
            Assert.IsFalse(drop.activeInHierarchy, "the bead is empty");

            _hold?.Dispose();
            _hold = null;
            yield return LoadScene(Cellar);
            Assert.AreSame(world, SliceWorldDriver.Running, "the same world on the next visit");
            nook = Object.FindAnyObjectByType<NookSpot>();
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(nook.Stranger.Cast.activeSelf, "the stranger is gone once C is settled");
            Assert.IsFalse(nook.transform.Find("Look/Drop").gameObject.activeInHierarchy);
            Assert.IsTrue(nook.transform.Find("Look/Note").gameObject.activeInHierarchy, "the note stays on the shelf");
            Assert.IsNull(nook.PromptFor(nook.transform.position), "the shutters are done with");
        }

        [UnityTest]
        public IEnumerator ASmashedDropStainsTheStone()
        {
            yield return InTheCellarAfterA();
            var world = SliceWorldDriver.Running;
            var nook = Object.FindAnyObjectByType<NookSpot>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var stain = nook.transform.Find("Stain - where the drop was smashed").gameObject;
            Assert.IsFalse(stain.activeSelf);

            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            var smash = hud.DialogueOptions.ToList().FindIndex(o => o.EndsWith(Loc.Get("dialogue.poic.note.smash")));
            Assert.That(smash, Is.InRange(0, 8), string.Join(" | ", hud.DialogueOptions));
            yield return Tap(Key.Digit1 + smash);
            Assert.AreEqual(Loc.Get("dialogue.poic.smashed"), hud.DialogueLines.Last());
            yield return Tap(Key.Digit1);
            yield return null;

            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(nook.Poi), "smashing it settles C");
            Assert.IsTrue(stain.activeSelf, "a sweet black stain on the stone");
            Assert.IsFalse(nook.transform.Find("Look/Drop").gameObject.activeInHierarchy);
            Assert.AreEqual(1, world.Hiding.SightPerception, "no fog's sight");
            Assert.AreEqual(Loc.Get("tip.drop.note.note"), world.Tips.HeardInOrder().Select(t => Loc.Get($"tip.{t.id}.note")).Last(),
                "the note's words kept in the Notes");
        }

        /// <summary>A fresh world in the cellar at 20:01 of day 1, A resolved unattended, the player in Stealth Mode before the nook.</summary>
        private IEnumerator InTheCellarAfterA()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Cellar);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "the cellar carries the slice's world for testing");
            var nook = Object.FindAnyObjectByType<NookSpot>();
            Assert.IsNull(nook.PromptFor(nook.transform.position), "the shutters stay shut before C");
            world.Clock.AdvanceTicks((int)(ACloses + 1 - world.Clock.Tick));
            yield return null;
            Assert.AreEqual(PoiPhase.Open, world.Director.Phase(nook.Poi), "A's Aftermath opens C");

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var place = Before(nook.transform, nook.InReach, -1f);
            Assert.IsTrue(place.HasValue, $"a free place before the nook at {nook.transform.position}");
            Assert.IsTrue(world.Hiding.SetActive(true), "Stealth Mode, taken at the stair's foot");
            player.Teleport(place.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(Flat(nook.transform.position - place.Value)));
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(Loc.Get("ui.prompt.open_shutters"), Object.FindAnyObjectByType<HudView>().PromptText);
            _hold = world.Clock.Pause();
        }

        /// <summary>
        /// The first place on the floor round <paramref name="spot"/>, clear for the player and within reach, starting
        /// along its forward times <paramref name="side"/> (the nook faces its wall, so -1 starts out from it).
        /// </summary>
        private static Vector3? Before(Transform spot, Func<Vector3, bool> inReach, float side = 1f)
        {
            var notCharacters = ~LayerMask.GetMask("Character");
            foreach (var distance in new[] { 0.7f, 0.9f, 1.05f })
            {
                foreach (var angle in new[] { 0f, -45f, 45f, -90f, 90f, 135f, -135f, 180f })
                {
                    var place = spot.position + (Quaternion.Euler(0f, angle, 0f) * Flat(spot.forward) * (side * distance));
                    if (!Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 1.5f, notCharacters, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    var feet = floor.point;
                    if (!Physics.CheckCapsule(feet + (Vector3.up * 0.4f), feet + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && inReach(feet))
                    {
                        return feet;
                    }
                }
            }

            return null;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
    }
}
#endif
