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
        public IEnumerator ThoseWhoSawThePlayerSearchAndTheFightStartsAgainOnSight()
        {
            yield return SetOut();
            var fights = SliceWorldDriver.Running.Fights;
            var hp = fights.Searching.Select(s => s.Fighter).ToList();
            Assert.IsTrue(fights.Searching.All(s => s.Now == FightSearch.Phase.Running), "running where they saw the player");
            for (var time = 0f; time < 30f && !fights.InFight; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(fights.InFight, "a searcher sees the player and the fight starts again");
            Assert.AreEqual(0, fights.Searching.Count, "the search is over");
            var again = fights.Turns.Order.Where(c => c != fights.PlayerCombatant).Select(c => c.Fighter).ToList();
            CollectionAssert.AreEquivalent(hp, again, "the same two, their HP as they left");
            fights.Turns.Stop();
            yield return Ended(fights);
        }

        [UnityTest]
        public IEnumerator ASearcherWhoFindsNobodyLooksRoundAndGoesBack()
        {
            yield return SetOut();
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.enabled = false; // nobody can see a character who is not about
            var bodies = fights.Searching.Select(s => s.gameObject).ToList();
            for (var time = 0f; time < 30f && fights.Searching.Any(s => s.Now == FightSearch.Phase.Running); time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(fights.Searching.All(s => s.Now == FightSearch.Phase.Looking), "both look round where they stopped: " + Searchers(fights) + $", paused {world.Clock.IsPaused}");
            using (world.Clock.Pause())
            {
                // The clock held, each tick is one of ours; a searcher that got there first has looked longer.
                for (var tick = 0; tick < FightSearch.LookTicks; tick++)
                {
                    world.Clock.AdvanceTicks(1);
                    foreach (var s in fights.Searching)
                    {
                        Assert.AreEqual(s.Looked >= FightSearch.LookTicks ? FightSearch.Phase.Returning : FightSearch.Phase.Looking, s.Now, $"{s.name} after {s.Looked} ticks");
                    }
                }
            }

            Assert.IsTrue(fights.Searching.All(s => s.Now == FightSearch.Phase.Returning), "three ticks of looking, then back");
            for (var time = 0f; time < 40f && fights.Searching.Count > 0; time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(0, fights.Searching.Count, "back where they stood");
            yield return null;
            Assert.IsTrue(bodies.All(b => b == null), "development opponents go once their search is over");
            Assert.IsFalse(fights.InFight);
            player.enabled = true;
        }

        private static string Searchers(SliceConfront fights) => string.Join("; ", fights.Searching.Select(s =>
            $"{s.name} {s.Now} at {s.transform.position} to {s.Place}, {Vector3.Distance(s.transform.position, s.Place):F1} m"));

        /// <summary>
        /// Two cultists twelve cells ahead along a street see the player; it walks ten cells back, beyond their cones,
        /// and takes Сбежать; both set out to search.
        /// </summary>
        private IEnumerator SetOut()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            world.Character.Set(CharacterAttribute.Perception, 100);
            world.Character.Set(CharacterAttribute.Dexterity, 100); // 15 AP: ten steps back and Сбежать

            Assert.IsTrue(Line(player.transform.position, out var places, out var back), "a straight way: two cells twelve ahead, ten behind");
            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Cult, places), "two cultists twelve cells off");
            yield return PlayersTurn(fights);
            var me = fights.PlayerCombatant;
            var turns = fights.Turns;
            var cultists = turns.Order.Where(c => c != me).ToList();
            Assert.IsTrue(cultists.All(c => c.Chosen == me && c.SawWho == me), "both mean to strike the player and see it");
            Assert.IsTrue(turns.SubmitIntent(TurnIntent.MoveTo(back)), "ten cells back");
            yield return PlayersTurn(fights);
            Assert.IsFalse(turns.Seen(me), "beyond their cones");
            Assert.IsTrue(cultists.All(c => c.SawAt.HasValue), "each knows where it last saw the player");
            yield return Tap(Key.Z);
            Assert.AreEqual(Departure.BrokeAway, me.Departure, "Z: Сбежать");
            yield return Ended(fights);
            Assert.AreEqual(2, fights.Searching.Count, "both search");
        }

        /// <summary>
        /// A straight way from the player along an axis: two reachable cells twelve cells ahead, three apart, both seeing
        /// the player's cell, and the cell ten back, every cell from the first to it reachable.
        /// </summary>
        private static bool Line(Vector3 from, out List<Vector3> places, out CombatCell back)
        {
            places = new List<Vector3>();
            back = default;
            var grid = new CombatGridReader(1f);
            if (!grid.Follow(from))
            {
                return false;
            }

            var origin = grid.Origin;
            var start = grid.Center(origin);
            foreach (var d in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                var side = new Vector3(d.z, 0f, -d.x);
                if (!grid.TryCellAt(start + (d * 12f), out var a) || !grid.TryCellAt(start + (d * 12f) + (side * 3f), out var b)
                    || !grid.IsReachable(a) || !grid.IsReachable(b) || !grid.CanSee(a, origin) || !grid.CanSee(b, origin))
                {
                    continue;
                }

                var clear = true;
                for (var k = -11; k <= 10 && clear; k++)
                {
                    clear = grid.TryCellAt(start - (d * k), out var c) && grid.IsReachable(c);
                }

                if (clear && grid.TryCellAt(start - (d * 10f), out back))
                {
                    places.Add(grid.Center(a));
                    places.Add(grid.Center(b));
                    return true;
                }
            }

            return false;
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
