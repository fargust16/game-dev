#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.World.Interiors;
using GameDev.World.Portals;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.4 acceptance: in and out of the tavern, back where the player entered, and the clock keeps counting.</summary>
    public sealed class DoorTests : SliceInputTestBase
    {
        private const string StreetScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";
        private const string StreetScene = "S0_Movement_Test";
        private const string TavernScene = "S0_Tavern_Test";

        private static Portal FindPortal(string id) =>
            Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude).Single(p => p.Id == id);

        private static double Now(IWorldClock clock) => clock.Tick + clock.TickProgress;

        private IEnumerator UseDoorAndWaitFor(string sceneName)
        {
            yield return Tap(Key.E);
            var deadline = Time.realtimeSinceStartup + 10f;
            while ((SceneManager.GetActiveScene().name != sceneName || SceneTransition.IsTraveling) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, "the door should have led to the other scene");
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator ThePlayerGoesInAndComesBackWhereTheyEntered()
        {
            yield return LoadScene(StreetScenePath);
            var clock = GameClockDriver.Running;
            Assert.IsNotNull(clock);

            var entrance = FindPortal("tavern.street").ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(entrance.position, entrance.rotation);
            yield return new WaitForSeconds(0.2f);
            var before = Now(clock);

            yield return UseDoorAndWaitFor(TavernScene);
            Assert.IsNotNull(InteriorRuntime.Current);
            Assert.AreEqual("tavern", InteriorRuntime.Current.BuildingId);
            var inside = Object.FindAnyObjectByType<PlayerLocomotion>();
            var insidePose = FindPortal("tavern.inside").ArrivalPose;
            Assert.Less(Vector3.Distance(inside.transform.position, insidePose.position), 0.3f, Describe(inside));
            Assert.IsTrue(inside.IsGrounded, Describe(inside));
            Assert.AreSame(clock, GameClockDriver.Running, "one clock runs through the door");
            var middle = Now(clock);
            Assert.Greater(middle, before);

            yield return UseDoorAndWaitFor(StreetScene);
            Assert.IsNull(InteriorRuntime.Current);
            var outside = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.Less(Vector3.Distance(outside.transform.position, entrance.position), 0.3f, Describe(outside));
            Assert.Less(Quaternion.Angle(outside.transform.rotation, entrance.rotation), 1f);
            Assert.AreSame(clock, GameClockDriver.Running);
            Assert.Greater(Now(clock), middle);
        }

        [UnityTest]
        public IEnumerator TheDoorIgnoresAPlayerOutOfReach()
        {
            yield return LoadScene(StreetScenePath);
            yield return Tap(Key.E);
            yield return new WaitForSeconds(0.3f);

            Assert.IsFalse(SceneTransition.IsTraveling);
            Assert.AreEqual(StreetScene, SceneManager.GetActiveScene().name);
        }
    }
}
#endif
