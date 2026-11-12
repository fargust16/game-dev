#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Core.Scenes;
using GameDev.Locomotion;
using GameDev.Player.Progression;
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
    /// S3a.9 in play, in a Veilmouth street: since the owner's direction of 2026-11-12 the player breaks away by Сбежать,
    /// Z, with both opponents eighteen cells off, beyond their cones, and the fight ends; Z under their eyes four cells
    /// off is refused and says why; the player yields to two Wardens with Y, hears their terms, goes with
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
            world.Character.Set(CharacterAttribute.Perception, 100); // the player acts first, before the cultists close in

            var places = Far(player.transform.position, 18);
            Assert.AreEqual(2, places.Count, "two cells eighteen cells off, reachable from the player");
            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Cult, places), "a fight with two cultists far off");
            yield return PlayersTurn(fights);
            var me = fights.PlayerCombatant;
            var turns = fights.Turns;
            Assert.GreaterOrEqual(turns.NearestEnemy(me), 15, "fifteen cells or more from the nearest");
            Assert.IsFalse(turns.Seen(me), "beyond their cones' fifteen cells");
            Assert.IsTrue(turns.CanBreakAway(me));
            yield return Tap(Key.Z);
            Assert.AreEqual(Departure.BrokeAway, me.Departure, "Z: Сбежать");
            yield return Ended(fights);

            Assert.IsTrue(fights.BrokeAway, "the fight ended by the player breaking away");
            Assert.IsFalse(fights.Dead, "nobody died");
            Assert.IsTrue(player.enabled, "free movement returns");
            Assert.IsFalse(world.Clock.IsPaused, "the world runs again");
            Assert.AreEqual(1, world.State.Get(SliceConfront.FoughtKey(SliceConfront.Cult)), "the fight with the cult is counted");
        }

        [UnityTest]
        public IEnumerator BreakingAwayUnderAnEnemysEyeIsRefused()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var fights = SliceWorldDriver.Running.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            SliceWorldDriver.Running.Character.Set(CharacterAttribute.Perception, 100);

            Assert.IsTrue(fights.StartDevelopmentFight(), "two cultists four cells off");
            yield return PlayersTurn(fights);
            var me = fights.PlayerCombatant;
            var ap = fights.Turns.ApLeft;
            Assert.IsTrue(fights.Turns.Seen(me), "they face the fight");
            yield return Tap(Key.Z);
            Assert.AreEqual(Departure.None, me.Departure, "refused");
            Assert.IsTrue(fights.InFight);
            Assert.AreEqual(ap, fights.Turns.ApLeft, "nothing spent");
            Assert.AreEqual(Loc.Get("ui.fight.noflee.seen"), fights.Hud.LogText, "the reason, said");
            fights.Turns.Stop();
            yield return Ended(fights);
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

        /// <summary>Two cells reachable from the player, <paramref name="cells"/> cells off as moves count them and a few apart.</summary>
        private static List<Vector3> Far(Vector3 from, int cells)
        {
            var grid = new CombatGridReader(1f);
            var places = new List<Vector3>();
            if (!grid.Follow(from))
            {
                return places;
            }

            var origin = grid.Origin;
            for (var dz = -cells; dz <= cells && places.Count < 2; dz++)
            {
                for (var dx = -cells; dx <= cells && places.Count < 2; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dz)) != cells)
                    {
                        continue;
                    }

                    var at = grid.Center(origin) + new Vector3(dx, 0f, dz);
                    if (grid.TryCellAt(at, out var cell) && grid.IsReachable(cell) && places.All(p => Vector3.Distance(p, grid.Center(cell)) > 3f))
                    {
                        places.Add(grid.Center(cell));
                    }
                }
            }

            return places;
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
