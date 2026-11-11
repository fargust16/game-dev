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
        /// AN2 (owner, 2026-11-11): the character faces the camera and moves in eight directions: holding right it steps
        /// east with its face to the north, where the camera looks, and holding back it backs away south still facing north.
        /// </summary>
        [UnityTest]
        public IEnumerator ThePlayerStepsSidewaysAndBacksAwayFacingTheCamera([Values(Key.D, Key.S)] Key key)
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var start = player.transform.position;

            Hold(key);
            yield return new WaitForSeconds(1.2f);
            var look = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            var face = player.transform.forward;
            var animator = player.GetComponentInChildren<Animator>();
            var blend = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveZ"));
            Hold();
            yield return null;

            var moved = player.transform.position - start;
            var way = key == Key.D ? Vector3.Cross(Vector3.up, look) : -look;
            Assert.Greater(Vector3.Dot(moved, way), 1.2f, $"the player should have gone {(key == Key.D ? "right" : "back")}; moved {moved}; {Describe(player)}");
            Assert.Greater(Vector3.Dot(face, look), 0.95f, $"the player faces where the camera looks; face {face}, camera {look}");
            if (key == Key.D)
            {
                Assert.Greater(blend.x, 0.6f, $"the blend steps right: {blend}");
            }
            else
            {
                Assert.Less(blend.y, -0.6f, $"the blend steps back: {blend}");
            }
        }
    }
}
#endif
