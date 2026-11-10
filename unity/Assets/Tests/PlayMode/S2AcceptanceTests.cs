#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Slice.People;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.Portals;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S2's acceptance (VERTICAL_SLICE_IMPLEMENTATION_PLAN S2): POI A and B can each be settled by at least two Approaches
    /// without a fight, a failed Social Check cannot be repeated on the spot, and residue shows the next day (S2.7). Two
    /// runs in Veilmouth settle A by Exchange and by Convince and B by Convince and by Exchange, and a third shows the
    /// residue of a lost Final Check. A check is made to fail by holding the clock on a tick whose roll, in this world's
    /// seed, lies above its chance.
    /// </summary>
    public sealed class S2AcceptanceTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const long Afternoon = 15 * 60;
        private const long Night = (23 * 60) + 30;
        private const long NextMorning = 1440 + (8 * 60) + 30;

        // The residue SliceResidue placed, by name.
        private const string Sealed = "Residue - Harrow & Tincture sealed";
        private const string Pairs = "Residue - Warden pairs on the Black Puddle";
        private const string Fear = "Residue - Fear on Draught Lane";
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
            Release();
            Loc.Language = _language;
        }

        [UnityTest]
        public IEnumerator HelpingTheWardensAndSwarmingTheLaneShowTheNextDay()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var stageA = Stage("a");
            var stageB = Stage("b");
            var door = Door("harrow-apothecary.street");

            // A by Exchange: holding the crowd for the Wardens.
            AdvanceTo(world, Afternoon);
            yield return Before(player, stageA);
            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            Assert.AreEqual("2. " + Loc.Get("dialogue.poia.street.hold"), hud.DialogueOptions[1]);
            yield return Tap(Key.Digit2);
            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stageA.Poi), "A settled by Exchange");
            yield return Tap(Key.Digit1);
            Release();
            yield return null;
            Assert.IsTrue(Residue(Sealed).IsShowing, "the Wardens seal the shop with wax and brass at once");
            Assert.IsTrue(door.Sealed);

            // B: a failed check that cannot be tried again, then Convince.
            AdvanceTo(world, FailingTick(Night, "poib.urge", 0.6, "poib.swarm"));
            world.Tips.Read(TipSource.NoticeBoard, world.Clock.Tick);
            yield return Before(player, stageB);
            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            Assert.AreEqual("3. " + Loc.Get("dialogue.poib.lane.run"), hud.DialogueOptions[2]);
            yield return Tap(Key.Digit3);
            StringAssert.Contains(Loc.Get("dialogue.poib.warden.urge"), hud.DialogueOptions[0]);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(Loc.Get("dialogue.poib.shrugged"), hud.DialogueLines.Last(), "the Warden shrugs: the check failed");
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.DialogueOptions.Any(o => o.Contains(Loc.Get("dialogue.poib.warden.urge"))), "a failed check is not tried again on the spot (10.4)");
            yield return Tap(Key.Digit2);
            world.SetCharacterValues(100);
            StringAssert.Contains(Loc.Get("dialogue.poib.lane.swarm"), hud.DialogueOptions[0]);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stageB.Poi), "B settled by Convince");
            Assert.IsFalse(world.State.IsSet("poi.b.fled"), "no fight");
            yield return Tap(Key.Digit1);
            Assert.IsFalse(hud.IsDialogueOpen);
            Release();

            // The next day.
            AdvanceTo(world, NextMorning);
            Release();
            yield return null;
            Assert.IsTrue(Residue(Pairs).IsShowing, "Warden pairs on the Black Puddle the next morning");
            Assert.IsTrue(Residue(Pairs).Shows.All(o => o.activeSelf));
            Assert.IsFalse(Residue(Fear).IsShowing, "the girls went free: no fear on Draught Lane");

            // Kit on Bawling Square first, far enough for the night's scenes to have gone.
            yield return TalkTo(player, "kit");
            CollectionAssert.Contains(hud.DialogueLines, Loc.Get("tip.kit.seal.line"), "Kit's day-2 rumour follows the seal");
            yield return Tap(Key.Escape);

            yield return AtDoor(player, door);
            Assert.AreEqual(Loc.Get("ui.prompt.sealed"), hud.PromptText);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(SceneTransition.IsTraveling, "a sealed door leads nowhere");
            Assert.AreEqual("Veilmouth", SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator DefendingHarrowAndFetchingAWardenShowTheNextDay()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var stageA = Stage("a");
            var stageB = Stage("b");
            var door = Door("harrow-apothecary.street");

            // A by Convince: the neighbours' words for Harrow, at the tick S2.5's test holds, with values at the ceiling.
            AdvanceTo(world, Afternoon);
            yield return Before(player, stageA);
            world.SetCharacterValues(100);
            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            yield return Tap(Key.Digit1);
            yield return Tap(Key.Digit1);
            StringAssert.Contains(Loc.Get("dialogue.poia.street.defend"), hud.DialogueOptions[0]);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stageA.Poi), "A settled by Convince");
            Assert.IsTrue(world.State.IsSet("aftermath.harrow.defended"));
            yield return Tap(Key.Digit1);
            Release();

            // B by Exchange: a tip promised to a Warden.
            AdvanceTo(world, Night);
            yield return Before(player, stageB);
            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            yield return Tap(Key.Digit2);
            Assert.AreEqual("1. " + Loc.Get("dialogue.poib.warden.promise_tip"), hud.DialogueOptions[0]);
            yield return Tap(Key.Digit1);
            Assert.AreEqual(PoiResolution.Participated, world.Director.Resolution(stageB.Poi), "B settled by Exchange");
            Assert.IsFalse(world.State.IsSet("poi.b.fled"), "no fight");
            yield return Tap(Key.Digit1);
            Release();

            // The next day: the shop open, no pairs, and Harrow behind his counter.
            AdvanceTo(world, NextMorning);
            Release();
            yield return null;
            Assert.IsFalse(Residue(Sealed).IsShowing);
            Assert.IsFalse(door.Sealed);
            Assert.IsFalse(Residue(Pairs).IsShowing, "the Wardens lost Harrow: no pairs");
            yield return TalkTo(player, "kit");
            CollectionAssert.DoesNotContain(hud.DialogueLines, Loc.Get("tip.kit.seal.line"), "no seal, no rumour of one");
            yield return Tap(Key.Escape);

            yield return AtDoor(player, door);
            Assert.AreEqual(Loc.Get("ui.prompt.enter"), hud.PromptText);
            yield return Through("HarrowApothecary");
            Assert.IsTrue(Harrow().activeInHierarchy, "Harrow is back in his shop");
        }

        /// <summary>Residue that is not the acceptance's: Harrow away while held, and the lane's fear after a lost Final Check.</summary>
        [UnityTest]
        public IEnumerator HarrowIsAwayWhileHeldAndTheLaneFearsWhenTheCollectorsWin()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var stageB = Stage("b");

            AdvanceTo(world, Afternoon);
            Release();
            yield return AtDoor(player, Door("harrow-apothecary.street"));
            yield return Through("HarrowApothecary");
            Assert.IsFalse(Harrow().activeInHierarchy, "Harrow is held in the street, not behind his counter");
            yield return AtDoor(player, Door("harrow-apothecary.inside"));
            yield return Through("Veilmouth");

            world.Tips.Read(TipSource.StreetRumour, world.Clock.Tick);
            AdvanceTo(world, FailingTick(Night, "poib.threaten", 0.5));
            yield return Before(player, stageB);
            yield return Tap(Key.E);
            yield return Tap(Key.Digit1);
            StringAssert.Contains(Loc.Get("dialogue.poib.lane.threaten"), hud.DialogueOptions[1]);
            yield return Tap(Key.Digit2);
            Assert.IsFalse(hud.IsDialogueOpen, "a lost Final Check ends the conversation in a fight, which S3 builds");
            Assert.IsTrue(world.State.IsSet("aftermath.loom_lane.girls_taken"));
            Release();

            AdvanceTo(world, NextMorning);
            Release();
            yield return null;
            var fear = Residue(Fear);
            Assert.IsTrue(fear.IsShowing, "the next morning the lane's women stay at home");
            Assert.IsTrue(fear.Hides.Length > 0 && fear.Hides.All(o => !o.activeSelf));
        }

        /// <summary>Runs the clock tick by tick to <paramref name="tick"/>, then holds it, so a check rolls at a known tick.</summary>
        private void AdvanceTo(SliceWorldDriver world, long tick)
        {
            Release();
            world.Clock.AdvanceTicks((int)(tick - world.Clock.Tick));
            _hold = world.Clock.Pause();
        }

        private void Release()
        {
            _hold?.Dispose();
            _hold = null;
        }

        /// <summary>
        /// The first tick from <paramref name="from"/> whose roll for <paramref name="failing"/> lies above
        /// <paramref name="above"/>, its chance at values 1, and whose rolls for <paramref name="passing"/> lie under the
        /// ceiling, 35/36, in this world's seed (DialogueSession: the option's fork of the social checks' stream by tick).
        /// </summary>
        private static long FailingTick(long from, string failing, double above, params string[] passing)
        {
            var seed = AssetDatabase.LoadAssetAtPath<SliceSettings>("Assets/Data/Slice/SliceSettings.asset").CreateSeed();
            for (var tick = from; tick < from + 120; tick++)
            {
                var at = (int)tick;
                double Roll(string key) => seed.Fork(SliceSeedStreams.SocialChecks, StableHash.Of(key), at).NextDouble();
                if (Roll(failing) > above && passing.All(k => Roll(k) < 0.95))
                {
                    return tick;
                }
            }

            Assert.Fail($"no tick within two hours of {from} fails {failing}");
            return from;
        }

        private static PoiStage Stage(string id) => Object.FindObjectsByType<PoiStage>().Single(s => s.Poi.Id == id);

        private static Portal Door(string id) => Object.FindObjectsByType<Portal>().Single(p => p.Id == id);

        private static WorldResidue Residue(string name) =>
            Object.FindObjectsByType<WorldResidue>(FindObjectsInactive.Include).Single(r => r.name == name);

        private static GameObject Harrow() =>
            Object.FindObjectsByType<NamedPerson>(FindObjectsInactive.Include).Single(p => p.Id == "harrow").gameObject;

        private static IEnumerator AtDoor(PlayerLocomotion player, Portal door)
        {
            var pose = door.ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
            yield return new WaitForSeconds(0.3f);
        }

        private IEnumerator Through(string scene)
        {
            yield return Tap(Key.E);
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((SceneManager.GetActiveScene().name != scene || SceneTransition.IsTraveling) && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "the door leads through");
            yield return new WaitForSeconds(0.3f);
        }

        /// <summary>Opens the conversation of the named person <paramref name="id"/> from a free place before them.</summary>
        private IEnumerator TalkTo(PlayerLocomotion player, string id)
        {
            var person = Object.FindObjectsByType<PersonTalk>().Single(t => t.Dialogue != null && t.Dialogue.Id == id);
            var notCharacters = ~LayerMask.GetMask("Character");
            Vector3? found = null;
            foreach (var distance in new[] { 1.2f, 1.5f, 1.8f })
            {
                for (var angle = 0f; angle < 360f && found == null; angle += 30f)
                {
                    var place = person.transform.position + (Quaternion.Euler(0f, angle, 0f) * person.transform.forward * distance);
                    if (Physics.Raycast(place + Vector3.up, Vector3.down, out var floor, 2f, notCharacters, QueryTriggerInteraction.Ignore)
                        && !Physics.CheckCapsule(floor.point + (Vector3.up * 0.4f), floor.point + (Vector3.up * 1.5f), 0.3f, notCharacters, QueryTriggerInteraction.Ignore)
                        && person.InReach(floor.point))
                    {
                        found = floor.point;
                    }
                }
            }

            Assert.IsTrue(found.HasValue, $"a free place before {id}");
            player.Teleport(found.Value + (Vector3.up * 0.1f), Quaternion.LookRotation(Flat(person.transform.position - found.Value)));
            yield return new WaitForSeconds(0.5f);
            yield return Tap(Key.E);
            Assert.IsTrue(Object.FindAnyObjectByType<HudView>().IsDialogueOpen, $"{id}'s conversation opens");
        }

        /// <summary>The player on the cobbles within the scene's talk, wherever its people leave room.</summary>
        private static IEnumerator Before(PlayerLocomotion player, PoiStage stage)
        {
            var talk = stage.Talk;
            var notCharacters = ~LayerMask.GetMask("Character");
            Vector3? found = null;
            foreach (var distance in new[] { 1.4f, 1.8f, 2.2f, 2.6f })
            {
                for (var angle = 0f; angle < 360f && found == null; angle += 30f)
                {
                    var place = stage.transform.position + (Quaternion.Euler(0f, angle, 0f) * stage.transform.forward * distance);
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
            Assert.IsTrue(stage.IsShown, "the scene stands once the player enters its area");
        }

        private static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
        }
    }
}
#endif
