#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Slice.Fights;
using GameDev.UI;
using GameDev.World.Portals;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.9 in play, in a Veilmouth street: the player breaks away from the development fight by walking ten cells
    /// from both opponents, and the fight ends; the player yields to two Wardens with Y, hears their terms, goes with
    /// them, and is let out of their lock-up at 08:00, owing the fine, the weapons held.
    /// </summary>
    public sealed class LeavingFightPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        [UnityTest]
        public IEnumerator BreakingAwayEndsAStreetFight()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();

            Assert.IsTrue(fights.StartDevelopmentFight(), "a fight with two cultists");
            yield return PlayersTurn(fights);
            var me = fights.PlayerCombatant;
            var turns = fights.Turns;
            var away = Away(fights, me, out var steps);
            Assert.IsTrue(away.HasValue, $"a cell ten cells from both opponents within {turns.ApLeft} AP of {me.Cell}");
            Assert.IsTrue(turns.SubmitIntent(TurnIntent.MoveTo(away.Value)), $"the walk to {away.Value}, {steps} steps");
            Assert.AreEqual(Departure.BrokeAway, me.Departure, "the player breaks away");
            yield return Ended(fights);

            Assert.IsTrue(fights.BrokeAway, "the fight ended by the player breaking away");
            Assert.IsFalse(fights.Dead, "nobody died");
            Assert.IsTrue(player.enabled, "free movement returns");
            Assert.IsFalse(world.Clock.IsPaused, "the world runs again");
            Assert.AreEqual(1, world.State.Get(SliceConfront.FoughtKey(SliceConfront.Cult)), "the fight with the cult is counted");
        }

        [UnityTest]
        public IEnumerator AYieldToTheWardensEndsInTheLockUp()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.State.Set(SliceConfront.DebtKey(SliceConfront.Wardens), 1);

            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Wardens), "a fight with two Wardens");
            yield return PlayersTurn(fights);
            var me = fights.PlayerCombatant;
            Assert.IsTrue(fights.Turns.Order.Where(c => c != me).All(c => c.AcceptsYield), "Wardens take a surrender");
            yield return Tap(Key.Y);
            Assert.AreEqual(Departure.Yielded, me.Departure, "Y yields");
            yield return Ended(fights);

            Assert.AreEqual(SliceConfront.Wardens, fights.Terms, "the Wardens' terms are said");
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.IsTrue(hud.IsDialogueOpen, "in the dialogue column");
            CollectionAssert.Contains(hud.DialogueLines.Select(l => l.Contains(Loc.Get("ui.yield.wardens.line"))), true, "their line");
            Assert.IsTrue(world.Clock.IsPaused, "a dialogue pauses the world");
            Assert.AreEqual(0, world.State.Get(SliceConfront.DebtKey(SliceConfront.Wardens)), "a fight with the Wardens cancels their debt");
            var before = (world.Clock.Day * 1440) + world.Clock.MinuteOfDay;

            yield return Tap(Key.Digit1);
            Assert.IsNull(fights.Terms, "the terms are taken");
            Assert.IsFalse(hud.IsDialogueOpen, "the column closes");
            for (var time = 0f; time < 120f && (SceneTransition.IsTraveling || SceneManager.GetActiveScene().name != SliceConfront.LockupScene); time += Time.unscaledDeltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(SliceConfront.LockupScene, SceneManager.GetActiveScene().name, "the character is in the Wardens' lock-up");
            var now = (world.Clock.Day * 1440) + world.Clock.MinuteOfDay;
            Assert.Greater(now, before, "the night has passed");
            Assert.That(world.Clock.MinuteOfDay, Is.InRange(8 * 60, (8 * 60) + 2), "let out at 08:00");
            Assert.AreEqual(SliceConfront.WardensFine, world.State.Get(SliceConfront.OwedKey(SliceConfront.Wardens)), "the fine is owed");
            Assert.AreEqual(1, world.State.Get(SliceConfront.WeaponsHeldKey), "the weapons are held");
            Assert.AreEqual(1, world.State.Get(SliceConfront.LockupsKey));
            Assert.AreEqual(1, world.State.Get(SliceConfront.FoughtKey(SliceConfront.Wardens)), "the fight with the Wardens is counted");
            var door = Object.FindObjectsByType<Portal>(FindObjectsSortMode.None).Single(p => p.Id == SliceConfront.LockupArrival);
            player = Object.FindAnyObjectByType<PlayerLocomotion>();
            Assert.Less(Vector3.Distance(player.transform.position, door.ArrivalPose.position), 1.5f, "by the lock-up's door");
            Assert.IsTrue(player.enabled, "free to walk out");
        }

        /// <summary>The cell within the turn's AP nearest by steps that stands ten cells or more from every opponent.</summary>
        private static CombatCell? Away(SliceConfront fights, Combatant me, out int steps)
        {
            var grid = fights.Grid;
            var turns = fights.Turns;
            var foes = turns.Order.Where(c => c.Standing && c.Side != me.Side).ToList();
            var path = new List<CombatCell>();
            CombatCell? best = null;
            steps = int.MaxValue;
            var reach = turns.ApLeft;
            for (var dz = -reach; dz <= reach; dz++)
            {
                for (var dx = -reach; dx <= reach; dx++)
                {
                    var at = grid.Center(me.Cell) + new Vector3(dx * grid.CellSize, 0f, dz * grid.CellSize);
                    if (!grid.TryCellAt(at, out var cell) || !grid.IsReachable(cell) || grid.OccupantAt(cell) >= 0
                        || foes.Any(f => Mathf.Max(Mathf.Abs(f.Cell.X - cell.X), Mathf.Abs(f.Cell.Z - cell.Z)) < 10))
                    {
                        continue;
                    }

                    var n = grid.FindPath(me.Cell, cell, path);
                    if (n > 0 && n <= reach && n < steps)
                    {
                        best = cell;
                        steps = n;
                    }
                }
            }

            return best;
        }

        private static IEnumerator PlayersTurn(SliceConfront fights)
        {
            for (var time = 0f; time < 60f && fights.InFight && (fights.Turns.Acting != fights.PlayerCombatant || fights.Showing); time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(fights.InFight && fights.Turns.Acting == fights.PlayerCombatant, "the player's turn comes");
        }

        private static IEnumerator Ended(SliceConfront fights)
        {
            for (var time = 0f; time < 60f && fights.InFight; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight is over and its last beat played");
        }
    }
}
#endif
