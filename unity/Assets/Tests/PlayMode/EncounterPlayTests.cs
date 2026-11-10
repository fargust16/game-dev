#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.4 in play: F7 starts a development fight in a Veilmouth street. Hostility stops the world and holds free
    /// movement, the grid forms within half a second, every round costs one world tick, and the fight ends with the
    /// world running again.
    /// </summary>
    public sealed class EncounterPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator AStreetFightStartsCostsOneTickARoundAndEnds()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "the slice's world runs");
            var fights = world.Fights;
            Assert.IsNotNull(fights, "the world has its fights");
            fights.TurnPause = 0f;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.IsNotNull(player, "the player");

            long atStart = -1, atEnd = -1;
            var rounds = 0;
            var side = -2;
            fights.Lock.ConfrontStarted += _ => atStart = world.Clock.Tick;
            fights.Lock.ConfrontEnded += s =>
            {
                atEnd = world.Clock.Tick;
                side = s;
            };
            fights.Turns.RoundEnded += _ => rounds++;

            // The bodies in their fight clips (ANIMATION_PIPELINE_PLAN.md A2): every blow is seen, and who falls lies in its death.
            var body = player.GetComponentInChildren<Animator>();
            Animator BodyOf(Combatant c) => c.Id == Fights.SliceConfront.PlayerId ? body
                : Fights.SliceFighter.All.FirstOrDefault(f => f.Combatant == c)?.GetComponentInChildren<Animator>();
            var strikes = 0;
            var unseen = new List<string>();
            fights.Turns.Struck += (attacker, target, result) =>
            {
                strikes++;
                var fallen = BodyOf(target);
                if (target.Fighter.Down && fallen != null && !fallen.GetBool("Down"))
                {
                    unseen.Add($"combatant {target.Id} fell and stands");
                }
            };

            var pressed = Time.realtimeSinceStartup;
            yield return Tap(Key.F7);
            Assert.IsTrue(fights.Lock.InConfront, $"F7 starts a fight at {player.transform.position}");
            Assert.Less(Time.realtimeSinceStartup - pressed, 0.5f, "the grid forms within half a second");
            Assert.IsTrue(fights.GridShown, "the grid is drawn");
            Assert.IsTrue(world.Clock.IsPaused, "hostility stops the world");
            Assert.IsFalse(player.enabled, "free movement is held");
            Assert.AreEqual(3, fights.Turns.Order.Count, "the player and two opponents fight");
            foreach (var c in fights.Turns.Order)
            {
                Assert.AreEqual(CombatantState.Fighting, c.State, $"combatant {c.Id} stands on the grid");
                Assert.IsTrue(fights.Grid.InBounds(c.Cell.X, c.Cell.Z), $"combatant {c.Id} is inside the grid");
                var guard = BodyOf(c);
                Assert.IsNotNull(guard, $"combatant {c.Id} has a body");
                Assert.IsTrue(guard.GetBool("Fighting"), $"combatant {c.Id} takes its guard");
                Assert.AreEqual(c.Id == Fights.SliceConfront.PlayerId ? 1f : 3f, guard.GetFloat("Weapon"), $"combatant {c.Id}: a sword's guard, or a dagger's");
            }

            for (var time = 0f; fights.Lock.InConfront && time < 60f; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.Lock.InConfront, $"the fight ends; round {fights.Turns.Round}");
            Assert.GreaterOrEqual(rounds, 1, "the fight lasted rounds");
            Assert.AreEqual(rounds, atEnd - atStart, "every round costs one world tick, and nothing else moves the clock");
            Assert.IsTrue(side == SliceConfrontSides.Player || side == SliceConfrontSides.Hostile, $"one side stands at the end, not {side}");
            Assert.IsFalse(world.Clock.IsPaused, "the world runs again");
            Assert.IsTrue(player.enabled, "free movement returns");
            Assert.IsFalse(fights.GridShown, "the grid is gone");
            Assert.Greater(strikes, 0, "blows were struck");
            CollectionAssert.IsEmpty(unseen, "who falls plays its death");
            Assert.IsFalse(body.GetBool("Fighting"), "the player's body leaves its guard");
            Debug.Log($"[EncounterPlayTests] {rounds} rounds, {atEnd - atStart} ticks, side {side} stands.");
        }

        private static class SliceConfrontSides
        {
            public const int Player = Fights.SliceConfront.PlayerSide;
            public const int Hostile = Fights.SliceConfront.HostileSide;
        }
    }
}
#endif
