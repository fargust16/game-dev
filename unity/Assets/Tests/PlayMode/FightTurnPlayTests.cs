#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameDev.Cameras;
using GameDev.Confront.Grid;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.5 in play: the player's turn by the cursor in a Veilmouth street. The fight camera takes over and the HUD shows
    /// the order; over a cell the path and its AP show before the click, and the click walks; over an enemy the blow's
    /// AP show, and from Intelligence 10 its chance, from 20 the parts of it; the click strikes; Space ends the turn.
    /// </summary>
    public sealed class FightTurnPlayTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";

        private Mouse _mouse;

        [SetUp]
        public void AddMouse()
        {
            _mouse = InputSystem.AddDevice<Mouse>("SliceTestMouse");
            _mouse.MakeCurrent();
        }

        [TearDown]
        public void RemoveMouse()
        {
            InputSystem.RemoveDevice(_mouse);
        }

        [UnityTest]
        public IEnumerator APlayerTurnByTheCursorWalksStrikesAndEnds()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            Assert.IsNotNull(world, "the slice's world runs");
            var fights = world.Fights;
            Assert.IsNotNull(fights, "the world has its fights");
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            var free = Camera.main.GetComponent<ThirdPersonCamera>();

            yield return Tap(Key.F7);
            Assert.IsTrue(fights.InFight, "F7 starts a fight");
            Assert.IsTrue(fights.FightCamera.Active, "the fight camera takes over");
            Assert.IsTrue(free == null || !free.enabled, "the free camera rests");
            Assert.IsTrue(fights.Hud.Shown, "the HUD shows the fight");
            var turns = fights.Turns;
            var me = fights.PlayerCombatant;
            Assert.IsNotNull(me, "the player fights");
            Assert.IsTrue(me.TakesIntents, "the player's turns wait for the player");

            yield return PlayersTurn(fights);
            Assert.AreEqual(3, fights.Hud.OrderShown, "the order shows the player and two opponents");
            Assert.AreEqual(Loc.Get("ui.fight.turn.yours"), fights.Hud.WhoText, "the HUD says whose turn it is");
            var startAp = turns.ApLeft;
            Assert.Greater(startAp, 4, "a turn's AP");

            // A path: one step, its AP shown before the click.
            var camera = fights.FightCamera.View;
            var cursor = fights.PlayerCursor;
            var moved = false;
            foreach (var cell in Neighbours(fights.Grid, me.Cell))
            {
                yield return Point(camera.WorldToScreenPoint(fights.Grid.Center(cell)));
                if (cursor.Cell == cell && cursor.Target == null)
                {
                    Assert.AreEqual(FightCursor.Shape.Move, cursor.Current, "the cursor shows a walk");
                    Assert.AreEqual(1, cursor.Cost, "one step, 1 AP");
                    Assert.IsTrue(fights.PathView.Shown, "the path is drawn");
                    Assert.AreEqual(Loc.Format("ui.fight.move", 1), fights.Hud.TipText, "the tip says the walk's AP");
                    yield return Click();
                    yield return Settle(fights);
                    Assert.AreEqual(cell, me.Cell, "the click walks");
                    Assert.AreEqual(startAp - 1, turns.ApLeft, "the step cost 1 AP");
                    moved = true;
                    break;
                }
            }

            Assert.IsTrue(moved, $"a free cell beside the player at {me.Cell} could be pointed at");

            // An attack: the nearest enemy pointed at, its blow's AP shown; the chance from Intelligence 10, its parts from 20.
            var enemy = turns.Order.Where(c => c.Standing && c.Side != me.Side).OrderBy(c => (c.Position - me.Position).sqrMagnitude).First();
            var intelligence = world.Character.Get(CharacterAttribute.Intelligence);
            yield return Point(camera.WorldToScreenPoint(enemy.Position + (Vector3.up * 1.1f)));
            Assert.AreEqual(enemy, cursor.Target, "the cursor points at the enemy");
            Assert.IsTrue(cursor.Affordable, $"the walk and the blow, {cursor.Cost} AP, fit in {turns.ApLeft}");
            Assert.AreEqual(FightCursor.Shape.Attack, cursor.Current, "the cursor shows a blow");
            Assert.AreEqual(cursor.Steps + me.Fighter.Weapon.AttackAp, cursor.Cost, "the walk's steps and the weapon's AP");
            StringAssert.Contains(Loc.Format("ui.fight.attack", cursor.Cost), fights.Hud.TipText, "the tip says the blow's AP");
            Assert.Less(intelligence, 10, "the character's Intelligence starts below 10");
            StringAssert.DoesNotContain("%", fights.Hud.TipText, "below Intelligence 10 the chance stays hidden");
            world.Character.Set(CharacterAttribute.Intelligence, 10);
            yield return null;
            yield return null;
            StringAssert.Contains($"{(int)System.Math.Round(cursor.Forecast.Chance * 100d)} %", fights.Hud.TipText, "from Intelligence 10 the chance shows");
            StringAssert.DoesNotContain("×", fights.Hud.TipText, "but not its parts");
            world.Character.Set(CharacterAttribute.Intelligence, 20);
            yield return null;
            yield return null;
            StringAssert.Contains("×", fights.Hud.TipText, "from Intelligence 20 the parts show");
            world.Character.Set(CharacterAttribute.Intelligence, intelligence);

            var struck = false;
            turns.Struck += (attacker, target, _) => struck |= attacker == me && target == enemy;
            yield return Click();
            Assert.IsTrue(struck, "the click strikes the enemy");
            yield return Settle(fights);

            // The end of the turn.
            if (fights.InFight && turns.Acting == me)
            {
                yield return Tap(Key.Space);
                Assert.IsTrue(!fights.InFight || turns.Acting != me, "Space ends the player's turn");
            }

            // The rest of the fight: the player ends each turn until one side stands.
            for (var time = 0f; fights.InFight && time < 120f; time += Time.deltaTime)
            {
                if (turns.Acting == me && !fights.Showing)
                {
                    yield return Tap(Key.Space);
                }

                yield return null;
            }

            Assert.IsFalse(fights.InFight, "the fight ends");
            Assert.IsFalse(fights.FightCamera.Active, "the fight camera lets go");
            Assert.IsTrue(free == null || free.enabled, "the free camera returns");
            Assert.IsFalse(fights.Hud.Shown, "the fight leaves the HUD");
            Assert.IsFalse(fights.PathView.Shown, "no path is left drawn");
        }

        /// <summary>Waits for the player's turn, with every earlier beat shown.</summary>
        private static IEnumerator PlayersTurn(SliceConfront fights)
        {
            for (var time = 0f; time < 60f && fights.InFight && (fights.Turns.Acting != fights.PlayerCombatant || fights.Showing); time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.IsTrue(fights.InFight && fights.Turns.Acting == fights.PlayerCombatant, "the player's turn comes");
            yield return new WaitForSecondsRealtime(0.4f); // the camera settles over the player
        }

        private static IEnumerator Settle(SliceConfront fights)
        {
            yield return null;
            for (var time = 0f; time < 30f && fights.Showing; time += Time.deltaTime)
            {
                yield return null;
            }

            yield return new WaitForSecondsRealtime(0.4f);
        }

        /// <summary>The free cells a step from the player, sides first.</summary>
        private static IEnumerable<CombatCell> Neighbours(ICombatGrid grid, CombatCell from)
        {
            var offsets = new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1) };
            foreach (var (dx, dz) in offsets)
            {
                var size = grid.CellSize;
                var at = grid.Center(from) + new Vector3(dx * size, 0f, dz * size);
                if (grid.TryCellAt(at, out var cell) && grid.IsReachable(cell) && grid.OccupantAt(cell) < 0 && grid.CanStep(from, cell))
                {
                    yield return cell;
                }
            }
        }

        private IEnumerator Point(Vector3 screen)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(screen.x, screen.y) });
            yield return null;
            yield return null;
            yield return null;
        }

        private IEnumerator Click()
        {
            var at = _mouse.position.ReadValue();
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = at }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = at });
            yield return null;
        }
    }
}
#endif
