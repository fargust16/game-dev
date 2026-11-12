#if UNITY_EDITOR
using System.Collections;
using GameDev.Cameras;
using GameDev.Locomotion;
using GameDev.World.Timekeeping;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>S0.3 acceptance: the player walks the movement test scene in third person.</summary>
    public sealed class MovementSmokeTests : SliceInputTestBase
    {
        private const string ScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";

        private static IEnumerator LoadScene() => LoadScene(ScenePath);

        [UnityTest]
        public IEnumerator ThePlayerStandsOnTheStreetAndTheCameraFollows()
        {
            yield return LoadScene();

            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsNotNull(player);
            Assert.IsTrue(player.IsGrounded, Describe(player));
            Assert.AreEqual(0f, player.transform.position.y, 0.1f);

            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            Assert.AreSame(player.transform, camera.Target);
            Assert.Less(Vector3.Distance(camera.transform.position, player.transform.position), 10f);
            Assert.IsNotNull(GameClockDriver.Running);
        }

        [UnityTest]
        public IEnumerator HoldingForwardWalksThePlayerNorth()
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var start = player.transform.position;

            Hold(Key.W);
            yield return new WaitForSeconds(0.5f);
            var midway = Describe(player);
            yield return new WaitForSeconds(1f);
            Hold();
            yield return null;

            var moved = player.transform.position - start;
            Assert.Greater(moved.z, 1.5f, $"the player should have walked north; midway: {midway}; end: {Describe(player)}");
            Assert.Less(Mathf.Abs(moved.x), 0.5f);
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual(start.y, player.transform.position.y, 0.15f);
        }

        /// <summary>
        /// The owner's decision of 2026-11-12: the body faces where it goes and the camera only looks. Holding right it turns
        /// east and walks there face first, holding back it turns round and walks south face first, never stepping sideways
        /// or backing away; the blend plays straight ahead only.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePlayerTurnsAndWalksFaceFirst([Values(Key.D, Key.S)] Key key)
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var start = player.transform.position;
            var look = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;

            Hold(key);
            yield return new WaitForSeconds(1.2f);
            var face = player.transform.forward;
            var animator = player.GetComponentInChildren<Animator>();
            var blend = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveZ"));
            Hold();
            yield return null;

            var moved = player.transform.position - start;
            var way = key == Key.D ? Vector3.Cross(Vector3.up, look) : -look;
            Assert.Greater(Vector3.Dot(moved, way), 1.2f, $"the player should have gone {(key == Key.D ? "right" : "back")}; moved {moved}; {Describe(player)}");
            Assert.Greater(Vector3.Dot(face, way), 0.95f, $"the player faces where it goes; face {face}, way {way}");
            Assert.Less(Mathf.Abs(blend.x), 0.05f, $"no side step: {blend}");
            Assert.Greater(blend.y, 0.6f, $"the blend walks forward: {blend}");
        }
    }
}
#endif
