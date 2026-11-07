#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S1.3 in Veilmouth: reading the Notice Board after 14:00 shows the seizure notice and marks POI A.</summary>
    public sealed class TipTests : SliceInputTestBase
    {
        [UnityTest]
        public IEnumerator ReadingTheBoardMarksPoiA()
        {
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "Veilmouth carries the slice's world");
            var board = Object.FindObjectsByType<TipSourcePoint>(FindObjectsSortMode.None).Single(s => s.Source == TipSource.NoticeBoard);
            var poiA = world.Director.Pois.Single(p => p.Id == "a");
            if (world.Clock.Tick < 840)
            {
                world.Clock.AdvanceTicks((int)(840 - world.Clock.Tick));
            }

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(board.transform.position + (board.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);
            yield return new WaitForSeconds(0.3f);

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsTrue(board.InReach(player.transform.position), Describe(player));
            Assert.AreEqual("E  Read", hud.PromptText);

            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsReadingOpen, "the plain panel opens");
            StringAssert.Contains("SEIZURE", hud.ReadingText);
            Assert.AreEqual(PoiDiscovery.Marked, world.Director.Discovery(poiA), "the notice marks POI A");
            Assert.IsFalse(world.Clock.IsPaused, "reading a board does not pause the world");

            yield return Tap(Key.E);

            Assert.IsFalse(hud.IsReadingOpen, "Interact again closes it");

            // S1.4: the notice's fact is in the Character Notes, in the character's words.
            yield return Tap(Key.J);

            Assert.IsTrue(hud.IsNotesOpen, "J opens the Notes");
            Assert.IsTrue(hud.NoteLines.Any(l => l.Contains("seizure notice for Harrow & Tincture")), string.Join(" | ", hud.NoteLines));

            yield return Tap(Key.J);

            Assert.IsFalse(hud.IsNotesOpen, "J again closes them");
        }

        [UnityTest]
        public IEnumerator TheNewsboyIsNamedNearbyAndAskingHimPausesTheWorld()
        {
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var kit = Object.FindObjectsByType<People.NamedPerson>(FindObjectsSortMode.None).Single(p => p.Id == "kit");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(kit.transform.position + (kit.transform.forward * 1.5f) + (Vector3.up * 0.1f), Quaternion.LookRotation(-kit.transform.forward));
            yield return new WaitForSeconds(0.5f);

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.AreEqual("Kit Mallow", hud.NameTagText, "the player sees the newsboy's name nearby");
            Assert.AreEqual("E  Ask", hud.PromptText);

            yield return Tap(Key.E);

            Assert.IsTrue(hud.IsReadingOpen);
            Assert.AreEqual("Kit Mallow", hud.ReadingTitle);
            Assert.IsTrue(world.Clock.IsPaused, "talk pauses the world (10.4)");
            var tick = world.Clock.Tick;

            yield return Tap(Key.E);

            Assert.IsFalse(hud.IsReadingOpen);
            Assert.IsFalse(world.Clock.IsPaused);
            Assert.GreaterOrEqual(world.Clock.Tick, tick + 1, "the talk costs one tick when it ends");
        }
    }
}
#endif
