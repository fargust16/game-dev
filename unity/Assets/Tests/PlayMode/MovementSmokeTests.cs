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

        /// <summary>
        /// The owner's play test of 2026-11-12: a run turned through 90° carries round with the body at once, never sliding
        /// on the way it ran; every step goes where the body faces.
        /// </summary>
        [UnityTest]
        public IEnumerator ARunTurnedAtARightAngleNeverSlides()
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            var look = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
            var left = -Vector3.Cross(Vector3.up, look);

            Hold(Key.W);
            yield return new WaitForSeconds(1f);
            Hold(Key.A);
            var worst = 0f;
            var turnedAt = player.transform.position;
            var previous = player.transform.position;
            for (var t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                yield return null;
                var step = Vector3.ProjectOnPlane(player.transform.position - previous, Vector3.up);
                previous = player.transform.position;
                if (step.magnitude > 0.002f)
                {
                    worst = Mathf.Max(worst, Vector3.Angle(step, Vector3.ProjectOnPlane(player.transform.forward, Vector3.up)));
                }
            }

            Hold();
            yield return null;
            Assert.Less(worst, 8f, "each step goes where the body faces, with no slide");
            Assert.Greater(Vector3.Dot(player.transform.forward, left), 0.95f, "it runs left");
            Assert.Greater(Vector3.Dot(player.transform.position - turnedAt, left), 1.5f, $"{Describe(player)}");
        }

        /// <summary>
        /// The owner's play test of 2026-11-12: a jump rises in the take-off of Basic Motions with the arms down, never the
        /// fall's raised arms; in place with no key it lands in the whole bent-knee landing, and after a running jump the
        /// landing holds half a second, the body braking to a stop in it before it runs on.
        /// </summary>
        [UnityTest]
        public IEnumerator AJumpRisesInItsTakeOffAndLandsInBentKnees()
        {
            yield return LoadScene();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var animator = player.GetComponentInChildren<Animator>();
            yield return new WaitForSeconds(0.3f);

            var rose = false;
            var fell = false;
            yield return Tap(Key.Space);
            for (var t = 0f; t < 2f && player.State != PlayerLocomotion.Move.Landing; t += Time.deltaTime)
            {
                var info = animator.IsInTransition(0) ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
                rose |= player.State == PlayerLocomotion.Move.Airborne && info.IsName("JumpRise");
                fell |= player.State == PlayerLocomotion.Move.Airborne && info.IsName("Fall");
                yield return null;
            }

            Assert.IsTrue(rose, "the jump plays its take-off");
            Assert.IsFalse(fell, "never the fall's raised arms");
            Assert.AreEqual(PlayerLocomotion.Move.Landing, player.State, "a jump lands in the knees");
            var landed = Time.time;
            while (player.State == PlayerLocomotion.Move.Landing && Time.time < landed + 3f)
            {
                yield return null;
            }

            // The Basic Motions landing is 0.60 s long, 0.48 s of it from LandStart (tools.txt), so whole it holds as long
            // as the half second does.
            var still = Time.time - landed;
            Assert.Greater(still, 0.47f, "in place with no key, the landing plays whole");

            // A running jump: the landing holds half a second while the run brakes to a stop, then the run goes on.
            Hold(Key.W);
            yield return new WaitForSeconds(0.8f);
            Hold(Key.W, Key.Space);
            yield return null;
            yield return null;
            Hold(Key.W);
            for (var t = 0f; t < 2f && player.State != PlayerLocomotion.Move.Landing; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(PlayerLocomotion.Move.Landing, player.State, "the running jump lands in the knees");
            landed = Time.time;
            var braked = player.transform.position;
            var slid = 0f;
            while (player.State == PlayerLocomotion.Move.Landing && Time.time < landed + 2f)
            {
                yield return null;
                if (Time.time - landed > 0.2f)
                {
                    slid += Vector3.ProjectOnPlane(player.transform.position - braked, Vector3.up).magnitude;
                }

                braked = player.transform.position;
            }

            var held = Time.time - landed;
            Hold();
            Assert.AreEqual(0.5f, held, 0.1f, "with a key held, half a second of bent knees");
            Assert.Less(slid, 0.1f, "no glide: the body stands braked in the knees");
        }
    }
}
#endif
