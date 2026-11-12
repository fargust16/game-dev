#if UNITY_EDITOR
using System.Collections;
using GameDev.Cameras;
using GameDev.Locomotion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// AN5, 10.1 *Swimming*: in water deeper than the wading depth the character swims, its body floating at the
    /// surface; Shift swims fast as it sprints; Space at a bank climbs out onto it; where the bed rises it wades out.
    /// A basin of its own beside the movement test scene: water at 0 m, a bed at −4 m, a quay 1 m over the water to
    /// the north and a shelf 0.8 m under it to the south.
    /// </summary>
    public sealed class SwimPlayTests : SliceInputTestBase
    {
        private const string ScenePath = "Assets/Scenes/Slice/S0_Movement_Test.unity";
        private static readonly Vector3 Basin = new Vector3(300f, 0f, 300f);

        private static IEnumerator Pool()
        {
            yield return LoadScene(ScenePath);
            Block(new Vector3(0f, -4.5f, 0f), new Vector3(30f, 1f, 30f));    // the bed, 4 m down
            Block(new Vector3(0f, -1.5f, 12f), new Vector3(30f, 5f, 4f));    // the quay, its face at z = 10, its top 1 m over the water
            Block(new Vector3(0f, -2.4f, -14f), new Vector3(30f, 3.2f, 8f)); // the shelf, its edge at z = −10, 0.8 m under the water
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Object.Destroy(water.GetComponent<Collider>());
            water.name = "Water";
            water.transform.position = Basin;
            water.transform.localScale = new Vector3(4f, 1f, 4f);
            water.AddComponent<SwimWater>();
            Physics.SyncTransforms();
            var camera = Object.FindAnyObjectByType<ThirdPersonCamera>();
            Assert.IsNotNull(camera, "the scene's camera");
        }

        private static void Block(Vector3 at, Vector3 size)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position = Basin + at;
            block.transform.localScale = size;
        }

        /// <summary>The root's lowest while the character fell in and came up, m.</summary>
        private static float _lowest;

        private static IEnumerator FallIn(PlayerLocomotion player, float z)
        {
            player.Teleport(Basin + new Vector3(0f, -0.5f, z), Quaternion.identity);
            _lowest = float.MaxValue;
            for (var t = 0f; t < 1.5f; t += Time.deltaTime)
            {
                _lowest = Mathf.Min(_lowest, player.transform.position.y);
                yield return null;
            }

            Assert.AreEqual(PlayerLocomotion.Move.Swimming, player.State, Describe(player));
        }

        [UnityTest]
        public IEnumerator ACharacterInDeepWaterSwimsAtTheSurface()
        {
            yield return Pool();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            yield return FallIn(player, 0f);
            Assert.IsTrue(player.InWater);
            Assert.Less(_lowest, -1.35f, "fallen in, the body goes under before it comes up (owner, 2026-11-12)");
            Assert.AreEqual(-0.28f, player.transform.position.y, 0.08f, "treading still, the root floats 0.28 m under the surface: neck-deep (owner, 2026-11-12)");
            var animator = player.GetComponentInChildren<Animator>();
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Swim") || animator.GetNextAnimatorStateInfo(0).IsName("Swim"), "the swim plays");

            var start = player.transform.position;
            Hold(Key.W);
            yield return new WaitForSeconds(1f);
            var swimming = player.PlanarSpeed;
            Assert.IsFalse(player.IsSprinting, "an ordinary swim is free");
            Hold(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(player.IsSprinting, "the fast swim spends Stamina as the sprint");
            Assert.Greater(player.PlanarSpeed, swimming + 0.5f, "and is faster");
            Assert.AreEqual(-0.08f, player.transform.position.y, 0.1f, "in the stroke the root floats 0.08 m under, the body under the water and the face just out");
            Hold();
            yield return null;
            Assert.Greater(player.transform.position.z - start.z, 2f, $"swam north: {Describe(player)}");
            Assert.AreEqual(PlayerLocomotion.Move.Swimming, player.State);
        }

        [UnityTest]
        public IEnumerator SpaceAtAQuayClimbsOutOntoIt()
        {
            yield return Pool();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            yield return FallIn(player, 7f);
            Hold(Key.W);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(PlayerLocomotion.Move.Swimming, player.State, "held at the quay's face: " + Describe(player));
            yield return Tap(Key.Space);
            Hold();
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(PlayerLocomotion.Move.Grounded, player.State, Describe(player));
            Assert.AreEqual(1f, player.transform.position.y, 0.15f, "on the quay's top");
            Assert.Greater(player.transform.position.z - Basin.z, 10f, "past its edge");
        }

        [UnityTest]
        public IEnumerator WhereTheBedRisesTheCharacterWadesOut()
        {
            yield return Pool();
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            yield return FallIn(player, -7f);
            Hold(Key.S);
            yield return new WaitForSeconds(2.5f);
            Hold();
            yield return new WaitForSeconds(0.3f);
            Assert.AreNotEqual(PlayerLocomotion.Move.Swimming, player.State, Describe(player));
            Assert.IsFalse(player.InWater);
            Assert.AreEqual(-0.8f, player.transform.position.y, 0.15f, "standing on the shelf, the water to its chest");
        }
    }
}
#endif
