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
            CollectionAssert.Contains(accusation.Choices.ToList(), WardenAccusation.Choice.Persuade, "the Promise is Persuasion's Argument, once a life (VS-ASSAULT (a))");

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
        public IEnumerator ACharacterWhoRunsIsFollowedAndAccusedAgainWhenItStops()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var accusation = world.Accusation;
            world.Bag.Spend(world.Bag.Bronze);
            world.State.Raise(SliceConfront.DebtKey(SliceConfront.Wardens));

            yield return Stopped(world, "a Warden stops a character at −12");
            var offered = accusation.Choices.ToList();
            CollectionAssert.DoesNotContain(offered, WardenAccusation.Choice.Pay, "no fine without the money");
            CollectionAssert.DoesNotContain(offered, WardenAccusation.Choice.Bribe, "nor a bribe");
            CollectionAssert.Contains(offered, WardenAccusation.Choice.LockUp);
            CollectionAssert.Contains(offered, WardenAccusation.Choice.Persuade, "the Wardens' debt is Persuasion's Argument");
            var warden = accusation.Warden;

            // VS-ASSAULT (h): the Warden follows the one who ran, and warns it.
            accusation.Choose(WardenAccusation.Choice.Run);
            Assert.IsFalse(accusation.IsOpen);
            Assert.IsTrue(world.Standing.Accused, "running settles nothing");
            Assert.IsNotNull(accusation.Pursuit, "the Warden follows");
            Assert.AreEqual(1, accusation.Pursuit.Warned, "and warns at once");
            Assert.IsTrue(accusation.Busy, "no Suspend Save while it follows");

            // The character stands still where the Warden sees it: caught up with, the talk opens again.
            yield return Stopped(world, "a character who stops is caught up with", 6f); // the Warden runs up from as far as 8 m
            Assert.AreSame(warden, accusation.Warden, "by the Warden who followed");
            Assert.IsNull(accusation.Pursuit);

            // Run again: the warnings given stand, and the second comes at once. Out of its sight, it gives up.
            accusation.Choose(WardenAccusation.Choice.Run);
            Assert.AreEqual(2, accusation.Pursuit.Warned, "the second warning, «or I shoot»");
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var behind = Vector3.ProjectOnPlane(player.transform.position - warden.position, Vector3.up).normalized;
            player.Teleport(Ground(warden.position + (behind * 200f)), player.transform.rotation);
            for (var t = 0f; t < 15f && accusation.Pursuit != null; t += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsNull(accusation.Pursuit, "where it last saw the character, it gives up");
            Assert.IsFalse(world.Fights.InFight, "and no fight starts");
            Assert.Greater(accusation.QuietUntil, world.Clock.Tick, "the quiet ticks of a run follow");
            Assert.IsNotNull(warden.GetComponent<WardenErrand>(), "the Warden walks back to its post");
        }

        [UnityTest]
        public IEnumerator AWardenWhoseTwoWarningsAreIgnoredFights()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var accusation = world.Accusation;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            yield return Stopped(world, "a Warden stops a character at −12");
            var warden = accusation.Warden;
            accusation.Choose(WardenAccusation.Choice.Run);
            Assert.AreEqual(1, accusation.Pursuit.Warned);

            // The character keeps moving in the Warden's sight: each warning's tick runs out, then the Warden fights.
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            var start = player.transform.position;
            var side = Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(start - warden.position, Vector3.up).normalized);
            var reputation = world.Standing.ReputationPoints;
            var frame = 0;
            for (var t = 0f; t < 10f && !fights.InFight; t += Time.deltaTime)
            {
                player.Teleport(start + (side * ((frame++ % 2) * 0.1f)), player.transform.rotation);
                if (accusation.Pursuit != null && frame % 10 == 0)
                {
                    world.Clock.AdvanceTicks(1);
                }

                yield return null;
            }

            Assert.IsTrue(fights.InFight, "ignored a third time, the Warden fights");
            Assert.IsTrue(fights.Assaulting, "the Accusation's Fight, an Assault on the Warden");
            Assert.AreEqual(reputation + world.Standing.AssaultFactor * -3, world.Standing.ReputationPoints, "with its −3");
            Assert.IsNull(accusation.Pursuit);
        }

        [UnityTest]
        public IEnumerator ThePromiseIsGivenOnceALife()
        {
            yield return Arrive();
            var world = SliceWorldDriver.Running;
            var accusation = world.Accusation;
            world.Bag.Spend(world.Bag.Bronze);
            yield return Stopped(world, "a Warden stops a character at −12");
            Assert.IsTrue(world.Standing.PromiseOpen);
            accusation.Choose(WardenAccusation.Choice.Persuade);
            Assert.IsFalse(world.Standing.PromiseOpen, "the Promise is spent, believed or not");
            if (accusation.IsOpen)
            {
                CollectionAssert.DoesNotContain(accusation.Choices.ToList(), WardenAccusation.Choice.Persuade, "not believed, it is not offered again");
                Assert.AreEqual(1, world.Standing.AssaultFactor);
                accusation.Choose(WardenAccusation.Choice.LockUp);
            }
            else
            {
                Assert.AreEqual(2, world.Standing.AssaultFactor, "believed, a new Assault costs double");
                Assert.IsFalse(world.Standing.Accused, "and the Wardens let the character off");
            }
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

        private static IEnumerator Stopped(SliceWorldDriver world, string message, float seconds = 2f)
        {
            for (var t = 0f; t < seconds && !world.Accusation.IsOpen; t += Time.deltaTime)
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
