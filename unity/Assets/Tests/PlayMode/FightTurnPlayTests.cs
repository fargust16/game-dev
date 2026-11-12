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
                    Assert.IsNull(fights.TargetLit, "over a cell nobody is lit");
                    Assert.AreEqual(1, cursor.Cost, "one step, 1 AP");
                    Assert.IsTrue(fights.PathView.Shown, "the path is drawn");
                    StringAssert.StartsWith(Loc.Format("ui.fight.move", 1), fights.Hud.TipText, "the tip says the walk's AP");
                    Assert.AreEqual(cursor.Provokes, fights.Hud.TipText.Contains(Loc.Get("ui.fight.provokes")), "and warns of a Reaction Attack when the step leaves an enemy's side");

                    // Leaving an enemy's side draws its Reaction Attack (S3a.6), which the player may parry from the AP left.
                    var parries = 0;
                    void Parried(Combatant attacker, Combatant target, GameDev.Confront.Rules.StrikeResult result) => parries += target == me && result.GuardTried ? 1 : 0;
                    turns.Struck += Parried;
                    yield return Click();
                    yield return Settle(fights);
                    turns.Struck -= Parried;
                    Assert.AreEqual(cell, me.Cell, "the click walks");
                    var reaction = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDev.Confront.Rules.FightRules>("Assets/Data/Confront/FightRules.asset").ReactionAp;
                    Assert.AreEqual(startAp - 1 - (parries * reaction), turns.ApLeft, "the step cost 1 AP, and each parry on the way its own");
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

            // The owner's play test of 2026-11-12: the enemy under the cursor is lit, its body ringed by the outline.
            yield return null;
            var body = Fights.SliceFighter.All.First(f => f.Combatant == enemy).transform;
            Assert.AreSame(body, fights.TargetLit, "the enemy under the cursor is lit");
            bool Ringed(Transform t) => t.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterials.Any(m => m != null && m.shader.name == "GameDev/Slice/Target Outline"));
            Assert.IsTrue(Ringed(body), "its body carries the outline");
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
            yield return null;
            Assert.IsNull(fights.TargetLit, "while the blow plays nobody is lit");
            Assert.IsFalse(Ringed(body), "and the outline is gone from the body");
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

        /// <summary>
        /// S3a.7 in play: a low wall tagged Vault / Jump stands beside the player in a Veilmouth street. The cursor over the
        /// cell past it offers the vault and its 2 AP, and the click vaults: over the wall, or, failed, where it stood.
        /// </summary>
        [UnityTest]
        public IEnumerator AVaultInAStreetCrossesALowWall()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;

            yield return Tap(Key.F7);
            yield return PlayersTurn(fights);
            var turns = fights.Turns;
            var me = fights.PlayerCombatant;
            var grid = (CombatGridReader)fights.Grid;
            var start = me.Cell;

            // A side cell on the player's own level, and a wall 1 m high and 3 m long on the line between.
            var over = start;
            foreach (var cell in Neighbours(grid, start))
            {
                if (System.Math.Abs(cell.X - start.X) + System.Math.Abs(cell.Z - start.Z) == 1 && Mathf.Abs(grid.Center(cell).y - grid.Center(start).y) < 0.1f)
                {
                    over = cell;
                    break;
                }
            }

            Assert.AreNotEqual(start, over, $"a level side cell beside the player at {start}");
            var across = over.X != start.X;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestLowWall";
            wall.transform.position = ((grid.Center(start) + grid.Center(over)) * 0.5f) + (Vector3.up * 0.5f);
            wall.transform.localScale = across ? new Vector3(0.2f, 1f, 3f) : new Vector3(3f, 1f, 0.2f);
            wall.AddComponent<GameDev.Slice.Kit.KitEnvironmentAction>().Set(EnvironmentActionKind.VaultJump, EnvironmentActionBand.Easy);
            Physics.SyncTransforms();
            grid.Reread();
            world.Character.Set(CharacterAttribute.Dexterity, 20);
            world.Character.Set(CharacterAttribute.Strength, 20);

            Assert.IsFalse(grid.CanStep(start, over), "no step crosses the wall");
            Assert.IsTrue(turns.TraverseFrom(me, over, out var option), "the wall offers a vault");
            Assert.AreEqual(over, option.Landing);

            var cursor = fights.PlayerCursor;
            yield return Point(fights.FightCamera.View.WorldToScreenPoint(grid.Center(over)));
            Assert.AreEqual(over, cursor.Cell, "the cursor points past the wall");
            Assert.IsTrue(cursor.HasOption, "and offers the vault");
            Assert.AreEqual(EnvironmentActionKind.VaultJump, cursor.Option.Kind);
            Assert.AreEqual(2, cursor.Cost, "for 2 AP");
            Assert.IsTrue(cursor.Affordable);
            StringAssert.StartsWith(Loc.Format("ui.fight.env.VaultJump", 2), fights.Hud.TipText, "the tip names it and its AP");

            var ap = turns.ApLeft;
            bool? vaulted = null;
            turns.Traversed += (c, o, ok, fallen) => vaulted = c == me ? ok : vaulted;
            yield return Click();
            yield return Settle(fights);
            Assert.IsTrue(vaulted.HasValue, "the click vaults");
            Assert.AreEqual(vaulted.Value ? over : start, me.Cell, vaulted.Value ? "over the wall" : "a failed vault stays");
            if (fights.InFight && turns.Acting == me)
            {
                Assert.AreEqual(ap - 2, turns.ApLeft, "2 AP");
            }

            StringAssert.Contains(Loc.Get("ui.fight.env.name.VaultJump"), fights.Hud.LogText, "the log tells it");
            Object.Destroy(wall);
        }

        /// <summary>
        /// The fight as the player sees it, drawn into <c>Logs/SliceFight/</c>: the camera's frame with the HUD over it, at
        /// noon and at dusk, pointing at the nearest enemy (the walk, the blow's AP and its chance at Intelligence 10) and at
        /// a far cell (the path red past the AP). The hardware cursor is not in the picture.
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator RenderTheFightView([Values(12, 19)] int hour)
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            if (world.Clock.Tick < hour * 60)
            {
                world.Clock.AdvanceTicks((int)((hour * 60) - world.Clock.Tick));
            }

            // The camera draws into a full-HD texture for the whole test, so the cursor's rays and the tip use its pixels.
            const int width = 1920, height = 1080;
            var frame = new RenderTexture(width, height, 24);
            Camera.main.targetTexture = frame;
            var document = Object.FindAnyObjectByType<GameDev.UI.HudView>().GetComponent<UnityEngine.UIElements.UIDocument>();
            var overlay = new RenderTexture(width, height, 24);
            var panel = Object.Instantiate(document.panelSettings);
            panel.targetTexture = overlay;
            panel.clearColor = true;
            panel.colorClearValue = Color.clear;
            document.panelSettings = panel;
            yield return null;

            yield return Tap(Key.F7);
            yield return PlayersTurn(fights);
            world.Character.Set(CharacterAttribute.Intelligence, 10);
            var me = fights.PlayerCombatant;
            var camera = fights.FightCamera.View;
            var enemy = fights.Turns.Order.Where(c => c.Standing && c.Side != me.Side).OrderBy(c => (c.Position - me.Position).sqrMagnitude).First();
            yield return Point(camera.WorldToScreenPoint(enemy.Position + (Vector3.up * 1.1f)));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return null; // the camera has drawn into its texture
            Shoot(frame, overlay, $"fight_{hour:00}_enemy.png");

            // A far cell: the farthest reachable cell the grid has in the camera's view, so the path runs red past the AP.
            var far = me.Cell;
            var farthest = 0f;
            for (var x = 0; x < 64; x++)
            {
                for (var z = 0; z < 64; z++)
                {
                    var at = fights.Grid.Center(me.Cell) + new Vector3((x - 32) * fights.Grid.CellSize, 0f, (z - 32) * fights.Grid.CellSize);
                    if (!fights.Grid.TryCellAt(at, out var cell) || !fights.Grid.IsReachable(cell) || fights.Grid.OccupantAt(cell) >= 0)
                    {
                        continue;
                    }

                    var view = camera.WorldToViewportPoint(fights.Grid.Center(cell));
                    var distance = (fights.Grid.Center(cell) - me.Position).magnitude;
                    if (view.z > 0f && view.x > 0.1f && view.x < 0.9f && view.y > 0.15f && view.y < 0.85f && distance > farthest && distance < 11f)
                    {
                        farthest = distance;
                        far = cell;
                    }
                }
            }

            yield return Point(camera.WorldToScreenPoint(fights.Grid.Center(far)));
            yield return new WaitForSecondsRealtime(0.5f);
            yield return null; // the camera has drawn into its texture
            Shoot(frame, overlay, $"fight_{hour:00}_far.png");
            Debug.Log($"[RenderTheFightView] {width}x{height}, path {fights.PathView.Steps} steps, {fights.PathView.Affordable} affordable, {fights.FightCamera.Hidden} hidden");
            Camera.main.targetTexture = null;
        }

        /// <summary>The camera's frame with the HUD's panel laid over it, the panel's alpha premultiplied.</summary>
        private static void Shoot(RenderTexture frame, RenderTexture overlay, string name)
        {
            var scene = Read(frame);
            var hud = Read(overlay);
            var a = scene.GetPixels32();
            var b = hud.GetPixels32();
            for (var i = 0; i < a.Length; i++)
            {
                var keep = 255 - b[i].a;
                a[i] = new Color32(
                    (byte)Mathf.Min(255, b[i].r + (a[i].r * keep / 255)),
                    (byte)Mathf.Min(255, b[i].g + (a[i].g * keep / 255)),
                    (byte)Mathf.Min(255, b[i].b + (a[i].b * keep / 255)),
                    255);
            }

            scene.SetPixels32(a);
            System.IO.Directory.CreateDirectory("Logs/SliceFight");
            System.IO.File.WriteAllBytes("Logs/SliceFight/" + name, scene.EncodeToPNG());
            Debug.Log("[RenderTheFightView] Logs/SliceFight/" + name);
        }

        private static Texture2D Read(RenderTexture texture)
        {
            var active = RenderTexture.active;
            RenderTexture.active = texture;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            RenderTexture.active = active;
            return image;
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
