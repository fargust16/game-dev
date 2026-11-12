#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Locomotion;
using GameDev.Player.Standing;
using GameDev.Slice.Fights;
using GameDev.Slice.People;
using GameDev.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// 10.4 §6 *Accusation* in Veilmouth (WA5): with Reputation at −10 or lower, a Warden on post who sees the character
    /// within 8 m stops it, and the world waits. The fine paid raises Reputation to −5, and no Warden stops it again; a
    /// character who cannot pay is offered neither the fine nor a bribe, can run, and is stopped again after a while;
    /// the Wardens' debt opens Persuasion.
    /// </summary>
    public sealed class AccusationPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator AWardenAccusesACharacterOfLowReputationAndTheFineSettlesIt()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var accusation = world.Accusation;
            Assert.IsNotNull(accusation, "the world keeps the Wardens' Accusation");
            world.Bag.Earn(2 * SliceConfront.WardensFine);
            var bronze = world.Bag.Bronze;

            yield return Stopped(world, "a Warden within 8 m stops a character at −12");
            Assert.IsTrue(PeopleAround.IsWarden(accusation.Warden));
            Assert.IsTrue(world.Clock.IsPaused, "the world waits while the Warden speaks");
            Assert.IsTrue(Object.FindAnyObjectByType<HudView>().IsDialogueOpen, "in the dialogue column");
            Assert.AreEqual(SliceConfront.WardensFine, accusation.Fine);
            CollectionAssert.IsSubsetOf(new[] { WardenAccusation.Choice.Pay, WardenAccusation.Choice.LockUp, WardenAccusation.Choice.Bribe, WardenAccusation.Choice.Fight, WardenAccusation.Choice.Run },
                accusation.Choices.ToList());
            CollectionAssert.DoesNotContain(accusation.Choices.ToList(), WardenAccusation.Choice.Persuade, "Persuasion needs the Wardens' debt (10.4 §1)");

            var tick = world.Clock.Tick;
            accusation.Choose(WardenAccusation.Choice.Pay);
            Assert.IsFalse(accusation.IsOpen);
            Assert.IsFalse(world.Clock.IsPaused);
            Assert.AreEqual(tick + 1, world.Clock.Tick, "the dialogue's end costs a tick");
            Assert.AreEqual(bronze - SliceConfront.WardensFine, world.Bag.Bronze, "the fine is paid");
            Assert.AreEqual(-5, world.Standing.ReputationPoints, "paid, the name rises to −5");
            Assert.IsFalse(world.Standing.Accused);

            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                Assert.IsFalse(accusation.IsOpen, "and no Warden stops it again");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ACharacterWhoCannotPayRunsAndIsStoppedAgainLater()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var accusation = world.Accusation;
            world.Bag.Spend(world.Bag.Bronze);

            yield return Stopped(world, "a Warden stops a character at −12");
            var offered = accusation.Choices.ToList();
            CollectionAssert.DoesNotContain(offered, WardenAccusation.Choice.Pay, "no fine without the money");
            CollectionAssert.DoesNotContain(offered, WardenAccusation.Choice.Bribe, "nor a bribe");
            CollectionAssert.Contains(offered, WardenAccusation.Choice.LockUp);

            accusation.Choose(WardenAccusation.Choice.Run);
            Assert.IsFalse(accusation.IsOpen);
            Assert.IsTrue(world.Standing.Accused, "running settles nothing");
            for (var t = 0f; t < 1f; t += Time.deltaTime)
            {
                Assert.IsFalse(accusation.IsOpen, "the Wardens leave it be a while");
                yield return null;
            }

            world.State.Raise(SliceConfront.DebtKey(SliceConfront.Wardens));
            world.Clock.AdvanceTicks(WardenAccusation.QuietTicks + 1);
            yield return Stopped(world, "after the quiet ticks a Warden stops it again");
            CollectionAssert.Contains(accusation.Choices.ToList(), WardenAccusation.Choice.Persuade, "the Wardens' debt is Persuasion's Argument");
            accusation.Choose(WardenAccusation.Choice.Run);
        }

        // Veilmouth, the character's Reputation at −12, and the character 4 m before a Warden on post, facing it.
        private IEnumerator Arrive()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            for (var t = 0f; t < 2f && !player.IsGrounded; t += Time.deltaTime)
            {
                yield return null;
            }

            var warden = PeopleAround.Wardens(player.transform.position).FirstOrDefault();
            Assert.IsNotNull(warden, "Veilmouth has Wardens on post");
            world.State.Set(SocialStanding.ReputationKey, -12);
            Assert.IsTrue(world.Standing.Accused, "at −12 a Warden accuses");
            var way = Vector3.ProjectOnPlane(warden.forward, Vector3.up).normalized;
            player.Teleport(Ground(warden.position + (way * 4f)), Quaternion.LookRotation(-way));
        }

        private static IEnumerator Stopped(SliceWorldDriver world, string message)
        {
            for (var t = 0f; t < 2f && !world.Accusation.IsOpen; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(world.Accusation.IsOpen, message);
        }

        private static Vector3 Ground(Vector3 at) =>
            Physics.Raycast(at + (Vector3.up * 3f), Vector3.down, out var hit, 8f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : at;
    }
}
#endif
