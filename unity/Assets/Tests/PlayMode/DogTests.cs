#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Locomotion;
using GameDev.Slice.People;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>The owner's dog experiment (2026-11-06): the dogs of Veilmouth walk or sit on the ground, and greet the player who comes close.</summary>
    public sealed class DogTests : SliceInputTestBase
    {
        private static readonly string[] Own = { "Walking01", "SittingStart", "SittingCycle" };

        private static readonly string[] All =
        {
            "Walking01", "Walking02", "Breathing", "Running01", "WigglingTail", "SittingStart", "SittingCycle", "EatingStart", "EatingCycle",
            "AngryStart", "AngryCycle", "AngryEnd",
        };

        [UnityTest]
        public IEnumerator InVeilmouthTheDogsWalkOrSitAndGreetThePlayer()
        {
            yield return LoadScene("Assets/Scenes/Slice/Veilmouth.unity");
            var dogs = Object.FindObjectsByType<StreetDog>(FindObjectsSortMode.None).OrderBy(d => d.name).ToArray();
            Assert.AreEqual(5, dogs.Length, "five dogs in the city");

            yield return new WaitForSeconds(2f);

            var ground = LayerMask.GetMask("Ground", "Structure");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            foreach (var dog in dogs)
            {
                var at = dog.transform.position;
                Assert.IsTrue(Physics.Raycast(at + (Vector3.up * 1.5f), Vector3.down, out var hit, 4f, ground), $"{dog.name} has ground under it");
                Assert.AreEqual(hit.point.y, at.y, 0.1f, $"{dog.name} stands on the ground");
                var info = dog.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0);
                var near = Vector3.Distance(dog.transform.position, player.transform.position) < 4.5f;
                var states = near ? Own.Append("WigglingTail") : Own;
                var playing = All.FirstOrDefault(info.IsName) ?? "another state";
                Assert.IsTrue(states.Contains(playing), $"{dog.name} walks or sits, or greets the player beside it; it plays {playing}, {Vector3.Distance(dog.transform.position, player.transform.position):0.0} m from the player");
            }

            // The player steps up to the dog farthest from the start, which turns to greet it.
            var first = dogs.OrderByDescending(d => Vector3.Distance(d.transform.position, player.transform.position)).First();
            player.Teleport(first.transform.position + (first.transform.forward * 1.5f) + (Vector3.up * 0.1f), player.transform.rotation);

            yield return new WaitForSeconds(1f);

            var animator = first.GetComponentInChildren<Animator>();
            Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("WigglingTail") || animator.GetNextAnimatorStateInfo(0).IsName("WigglingTail"), $"{first.name} wags its tail at the player");
            var start = first.transform.position;

            yield return new WaitForSeconds(1f);

            Assert.Less(Vector3.Distance(start, first.transform.position), 0.05f, $"{first.name} stays to greet");
        }
    }
}
#endif
