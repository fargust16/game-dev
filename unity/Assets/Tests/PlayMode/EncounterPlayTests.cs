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
            fights.AutoPlayer = true; // S3a.4's fight plays itself; S3a.5's FightTurnPlayTests plays the player's turn by the cursor
            fights.BeatPace = 4f;
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
            var shown = 0;
            fights.Turns.Struck += (_, _, _) => strikes++;
            fights.BlowShown += (attacker, target, result) =>
            {
                shown++;
                var fallen = BodyOf(target);
                // The blow's own result: the turns run ahead of the bodies, so a later blow may already have felled it.
                if (result.Downed && fallen != null && !fallen.GetBool("Down"))
                {
                    unseen.Add($"combatant {target.Id} fell and stands");
                }
            };

            // The guards are read as the fight begins: with no pause between turns, a body may be walking (out of its
            // guard, in its walk) by the time the key's frames have passed.
            var guards = new List<(int Id, bool Fighting, float Weapon)>();
            fights.Lock.ConfrontStarted += request =>
            {
                foreach (var c in request.Combatants)
                {
                    var guard = BodyOf(c);
                    guards.Add((c.Id, guard != null && guard.GetBool("Fighting"), guard != null ? guard.GetFloat("Weapon") : -1f));
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
                Assert.IsNotNull(BodyOf(c), $"combatant {c.Id} has a body");
            }

            Assert.AreEqual(3, guards.Count, "every fighter's body was seen as the fight began");
            foreach (var (id, fighting, weapon) in guards)
            {
                Assert.IsTrue(fighting, $"combatant {id} takes its guard");
                Assert.AreEqual(id == Fights.SliceConfront.PlayerId ? 1f : 3f, weapon, $"combatant {id}: a sword's guard, or a dagger's");
            }

            for (var time = 0f; fights.InFight && time < 90f; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, $"the fight ends and its last blow is seen; round {fights.Turns.Round}");
            Assert.AreEqual(strikes, shown, "every blow struck is seen to land");
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
