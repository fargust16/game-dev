#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Locomotion;
using GameDev.UI;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.5 acceptance: the HUD shows the time, and a prompt at a door.</summary>
    public sealed class HudTests : SliceInputTestBase
    {
        private const string StreetScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";

        [UnityTest]
        public IEnumerator TheHudShowsTheClocksTime()
        {
            yield return LoadScene(StreetScenePath);
            var hud = Object.FindAnyObjectByType<HudView>();
            var clock = GameClockDriver.Running;
            Assert.IsNotNull(hud);

            // Paused, so no tick boundary falls between the HUD's update and the comparison.
            var hold = clock.Pause();
            try
            {
                yield return null;
                Assert.AreEqual(HudText.DayAndTime(clock.Day, clock.MinuteOfDay), hud.TimeText);

                clock.AdvanceTicks(60);
                yield return null;
                Assert.AreEqual(HudText.DayAndTime(clock.Day, clock.MinuteOfDay), hud.TimeText);
            }
            finally
            {
                hold.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ThePromptShowsOnlyAtADoor()
        {
            yield return LoadScene(StreetScenePath);
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsFalse(hud.IsPromptVisible, "the player starts away from the door");

            var door = Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Single();
            var pose = door.ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(pose.position, pose.rotation);
            yield return new WaitForSeconds(0.1f);
            Assert.IsTrue(hud.IsPromptVisible);
        }
    }
}
#endif
