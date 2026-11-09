#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Poi;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S1's acceptance in Veilmouth (VERTICAL_SLICE_IMPLEMENTATION_PLAN, S1): a tip marks a POI, which the map on M shows
    /// as a pin with its source's words and no countdown; a POI left alone resolves at the end of its window and leaves
    /// its Aftermath; a found fact appears in the Notes.
    /// </summary>
    public sealed class S1AcceptanceTests : SliceInputTestBase
    {
        [UnityTest]
        public IEnumerator ATipMarksAPoiTheMapShowsItAndLeftAloneItLeavesItsAftermath()
        {
            yield return ForgetTheWorld();
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            var hud = Object.FindAnyObjectByType<HudView>();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var poiA = world.Director.Pois.Single(p => p.Id == "a");
            var poiB = world.Director.Pois.Single(p => p.Id == "b");
            Assert.Less(world.Clock.Tick, 840, "the run starts before the seizure notice is pinned");

            // The seizure notice is pinned at 14:00; the player reads the board.
            world.Clock.AdvanceTicks((int)(840 - world.Clock.Tick));
            var board = Object.FindObjectsByType<TipSourcePoint>(FindObjectsSortMode.None).Single(s => s.Source == TipSource.NoticeBoard);
            player.Teleport(board.transform.position + (board.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);
            yield return new WaitForSeconds(0.3f);
            yield return Tap(Key.E);
            yield return Tap(Key.E);

            Assert.AreEqual(PoiDiscovery.Marked, world.Director.Discovery(poiA), "a tip marks a POI");
            Assert.AreEqual(PoiDiscovery.Hinted, world.Director.Discovery(poiB), "the missing girls' notice only hints B");

            // The map: A is a pin with its source's words, B an area, and no number anywhere: no countdown.
            yield return Tap(Key.M);
            Assert.IsTrue(hud.IsMapOpen, "M opens the map");
            var marks = hud.MapMarks;
            var pin = marks.Single(m => m.Label.StartsWith(poiA.Title));
            Assert.IsTrue(pin.Exact, "a Marked POI is a pin");
            StringAssert.Contains(poiA.UrgencyWords, pin.Label);
            Assert.IsFalse(marks.Single(m => m.Label.StartsWith(poiB.Title)).Exact, "a Hinted POI is an area");
            Assert.IsFalse(marks.Any(m => m.Label.Any(char.IsDigit)), "no countdown on the map (04 rule 5)");
            yield return Tap(Key.M);
            Assert.IsFalse(hud.IsMapOpen, "M again closes it");

            // Nobody comes: at 20:00 A's window closes and it resolves alone, leaving its Aftermath.
            world.Clock.AdvanceTicks((int)(1200 - world.Clock.Tick));
            Assert.AreEqual(PoiPhase.Resolved, world.Director.Phase(poiA));
            Assert.AreEqual(PoiResolution.Unattended, world.Director.Resolution(poiA));
            var outcome = poiA.Unattended.Single(o => o.name == world.Director.Outcome(poiA));
            Assert.IsTrue(outcome.raises.All(world.State.IsSet), $"the '{outcome.name}' Aftermath is in the world state");

            yield return Tap(Key.M);
            Assert.IsFalse(hud.MapMarks.Any(m => m.Label.StartsWith(poiA.Title)), "a closed window leaves the map");
            yield return Tap(Key.M);

            // The fact the board gave is in the Notes.
            yield return Tap(Key.J);
            Assert.IsTrue(hud.NoteLines.Any(l => l.Contains("seizure notice for Harrow & Tincture")), string.Join(" | ", hud.NoteLines));
            yield return Tap(Key.J);
        }

        /// <summary>A look at the map, not a check: renders the HUD with the map open to Logs/SliceMap/map.png. Run it by name.</summary>
        [UnityTest, Explicit]
        public IEnumerator RenderTheMap()
        {
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var world = SliceWorldDriver.Running;
            world.Director.Reveal(world.Director.Pois.Single(p => p.Id == "a"), PoiDiscovery.Marked);
            world.Director.Reveal(world.Director.Pois.Single(p => p.Id == "b"), PoiDiscovery.Hinted);
            var document = Object.FindAnyObjectByType<HudView>().GetComponent<UnityEngine.UIElements.UIDocument>();
            var texture = new RenderTexture(1920, 1080, 24);
            var panel = Object.Instantiate(document.panelSettings);
            panel.targetTexture = texture;
            panel.clearColor = true;
            panel.colorClearValue = Color.black;
            document.panelSettings = panel;
            yield return null;
            yield return Tap(Key.M);
            yield return new WaitForSeconds(0.5f);

            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = null;
            System.IO.Directory.CreateDirectory("Logs/SliceMap");
            System.IO.File.WriteAllBytes("Logs/SliceMap/map.png", image.EncodeToPNG());
            Debug.Log("[RenderTheMap] Logs/SliceMap/map.png");
        }
    }
}
#endif
