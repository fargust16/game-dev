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
        public IEnumerator ThePlayerGoesInAndComesBackWhereTheyEntered() =>
            GoInAndBack(StreetScenePath, "tavern.street", "tavern.inside", "tavern");

        /// <summary>S0.6: the same in Veilmouth, through the Soot &amp; Bell.</summary>
        [UnityTest]
        public IEnumerator InVeilmouthThePlayerGoesIntoTheSootAndBellAndBack() =>
            GoInAndBack("Assets/Scenes/Slice/Veilmouth.unity", "soot-and-bell.street", "soot-and-bell.inside", "soot-and-bell");

        /// <summary>The dressing pass: every other building that opens, through its door leaf on the model's face.</summary>
        [UnityTest]
        public IEnumerator InVeilmouthThePlayerGoesIntoEveryOtherBuildingAndBack(
            [Values("harrow-apothecary", "faction-house", "wardens-lockup", "missing-room", "soot-stair-cellar", "old-chapel")]
            string building) =>
            GoInAndBack("Assets/Scenes/Slice/Veilmouth.unity", building + ".street", building + ".inside", building);

        private IEnumerator GoInAndBack(string streetScenePath, string streetId, string insideId, string buildingId)
        {
            var streetScene = System.IO.Path.GetFileNameWithoutExtension(streetScenePath);
            yield return LoadScene(streetScenePath);
            var clock = GameClockDriver.Running;
            Assert.IsNotNull(clock);

            var street = FindPortal(streetId);
            var interiorScene = street.Target.SceneName;
            var entrance = street.ArrivalPose;
            Object.FindAnyObjectByType<PlayerLocomotion>().Teleport(entrance.position, entrance.rotation);
            yield return new WaitForSeconds(0.2f);
            var before = Now(clock);

            yield return UseDoorAndWaitFor(interiorScene);
            Assert.IsNotNull(InteriorRuntime.Current);
            Assert.AreEqual(buildingId, InteriorRuntime.Current.BuildingId);
            var inside = Object.FindAnyObjectByType<PlayerLocomotion>();
            var insidePose = FindPortal(insideId).ArrivalPose;
            Assert.Less(Vector3.Distance(inside.transform.position, insidePose.position), 0.3f, Describe(inside));
            Assert.IsTrue(inside.IsGrounded, Describe(inside));
            Assert.AreSame(clock, GameClockDriver.Running, "one clock runs through the door");
            var middle = Now(clock);
            Assert.Greater(middle, before);

            yield return UseDoorAndWaitFor(streetScene);
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
