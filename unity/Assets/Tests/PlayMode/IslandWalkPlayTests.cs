#if UNITY_EDITOR
using System.Collections;
using System.IO;
using GameDev.Locomotion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// The island's walkable duplicate (ISLAND_CITY_REBUILD_PLAN.md, IC6.1): in the lab scene the player stands on the
    /// ground at the layout's start, over the tide, and walks the street. The lab scene is built on the machine and not
    /// kept in git, so the test is passed over where it has not been built.
    /// </summary>
    public sealed class IslandWalkPlayTests : SliceInputTestBase
    {
        private const string Lab = "Assets/Scenes/Slice/Lab/IslandLab.unity";

        /// <summary>Sea Level, m.</summary>
        private const float Tide = 24f;

        [UnityTest]
        public IEnumerator ThePlayerStandsAtTheStartAndWalksTheStreet()
        {
            if (!File.Exists(Lab))
            {
                Assert.Ignore("the island's lab scene is not built on this machine");
            }

            yield return ForgetTheWorld();
            yield return LoadScene(Lab);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.That(player, Is.Not.Null, "the lab scene has no player: build the island's city first");
            yield return new WaitForSeconds(1f);
            Assert.That(player.IsGrounded, Is.True, Describe(player));
            var from = player.transform.position;
            Assert.That(from.y, Is.GreaterThan(Tide + 0.5f), "the start lies over the tide: " + Describe(player));

            Hold(Key.W);
            yield return new WaitForSeconds(3f);
            Hold();
            yield return new WaitForSeconds(0.5f);
            var at = player.transform.position;
            Assert.That(Vector3.ProjectOnPlane(at - from, Vector3.up).magnitude, Is.GreaterThan(3f), "three seconds of walking: " + Describe(player));
            Assert.That(at.y, Is.GreaterThan(Tide + 0.5f), "still on the town's ground: " + Describe(player));
            Assert.That(player.IsGrounded, Is.True, Describe(player));
        }
    }
}
#endif
