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

        /// <summary>
        /// Step 4a: a dwelling of each kind, through its street door into the shared Dwelling scene, which builds its
        /// rooms and its door back; the dwelling's id is its street door's.
        /// </summary>
        [UnityTest]
        public IEnumerator InVeilmouthThePlayerGoesIntoADwellingAndBack(
            [Values("Terrace", "Tenement", "Shop", "Villa", "Warehouse")] string archetype) =>
            GoInAndBack("Assets/Scenes/Slice/Veilmouth.unity", () => Object.FindObjectsByType<Portal>(FindObjectsInactive.Exclude)
                .Where(p => p.Target.SceneName == "Dwelling" && p.Target.Context.Contains("|" + archetype + "|"))
                .OrderBy(p => p.Id, System.StringComparer.Ordinal).First(), null, null);

        private IEnumerator GoInAndBack(string streetScenePath, string streetId, string insideId, string buildingId) =>
            GoInAndBack(streetScenePath, () => FindPortal(streetId), insideId, buildingId);

        /// <summary>A null <paramref name="insideId"/> or <paramref name="buildingId"/> is the street door's own id's.</summary>
        private IEnumerator GoInAndBack(string streetScenePath, System.Func<Portal> pickStreet, string insideId, string buildingId)
        {
            var streetScene = System.IO.Path.GetFileNameWithoutExtension(streetScenePath);
            yield return LoadScene(streetScenePath);
            var clock = GameClockDriver.Running;
            Assert.IsNotNull(clock);

            var street = pickStreet();
            insideId ??= street.Id + ".inside";
            buildingId ??= street.Id;
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

            // The reference plan's R1: the street stays loaded behind the interior, switched off.
            Assert.AreEqual(streetScene, SceneTransition.KeptScene.name, "the street is kept behind the interior");
            Assert.AreEqual(1, Object.FindObjectsByType<PlayerLocomotion>(FindObjectsInactive.Exclude).Length,
                "only the interior's player is awake");

            yield return UseDoorAndWaitFor(streetScene);
            Assert.IsNull(InteriorRuntime.Current);
            Assert.IsFalse(SceneManager.GetSceneByName(interiorScene).isLoaded, "the interior is unloaded behind the traveller");
            Assert.IsFalse(SceneTransition.KeptScene.IsValid(), "nothing is kept in the street");
            var outside = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.Less(Vector3.Distance(outside.transform.position, entrance.position), 0.3f, Describe(outside));
            Assert.Less(Quaternion.Angle(outside.transform.rotation, entrance.rotation), 1f);
            Assert.AreSame(clock, GameClockDriver.Running);
            Assert.Greater(Now(clock), middle);

            // The street's own camera, switched on again, stands behind the player as a newly loaded one would.
            var rig = Object.FindAnyObjectByType<GameDev.Cameras.ThirdPersonCamera>();
            if (rig != null)
            {
                var look = Vector3.ProjectOnPlane(rig.transform.forward, Vector3.up).normalized;
                Assert.Greater(Vector3.Dot(look, outside.transform.forward), 0.7f, "the camera looks the way the player faces");
                Assert.Less(Vector3.Distance(rig.transform.position, outside.transform.position), 10f, "the camera is with the player");
            }
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
