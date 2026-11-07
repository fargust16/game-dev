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
        }
    }
}
#endif
