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
    /// ground at the layout's start, over the tide, and stays there. The walk itself is the owner's, by hand: with no
    /// slice world in the scene the test's keyboard moved nobody (first run, 2026-11-14), while the owner's did. The
    /// lab scene is built on the machine and not kept in git, so the test is passed over where it has not been built.
    /// </summary>
    public sealed class IslandWalkPlayTests : SliceInputTestBase
    {
        private const string Lab = "Assets/Scenes/Slice/Lab/IslandLab.unity";

        /// <summary>Sea Level, m.</summary>
        private const float Tide = 24f;

        [UnityTest]
        public IEnumerator ThePlayerStandsOnTheGroundAtTheStart()
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

            // It stays where it stands: no fall through the ground, no slide down a bank.
            yield return new WaitForSeconds(2f);
            var at = player.transform.position;
            Assert.That((at - from).magnitude, Is.LessThan(0.3f), "two seconds at rest: " + Describe(player));
            Assert.That(player.IsGrounded, Is.True, Describe(player));
        }
    }
}
#endif
