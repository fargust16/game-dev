#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Locomotion;
using GameDev.Player.Progression;
using GameDev.Slice.Fights;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a's acceptance (VERTICAL_SLICE_IMPLEMENTATION_PLAN S3a.10), the part no other test holds: a fight in a Veilmouth
    /// street uses cover. The development command starts a fight with a cultist standing behind a street railing, the
    /// player four cells off on the railing's other side; the player takes the rifle, and the shot's chance carries
    /// Half Cover's 15 points (10.2 §5b, 10.1). The rest of S3a's acceptance is S3a.2's parity
    /// (<c>HeadlessFightTests</c>) and S3a.9's <c>LeavingFightPlayTests</c>.
    /// </summary>
    public sealed class S3aAcceptanceTests : SliceInputTestBase
    {
        private const string Veilmouth = "Assets/Scenes/Slice/Veilmouth.unity";
        private const int Off = 4;      // cells from the cover to the shooter
        private const int Railings = 60; // railings tried before the test gives up

        private static readonly (GridSide Side, int Dx, int Dz)[] Sides =
        {
            (GridSide.PlusX, 1, 0), (GridSide.MinusX, -1, 0), (GridSide.PlusZ, 0, 1), (GridSide.MinusZ, 0, -1),
        };

        [UnityTest]
        public IEnumerator AFightInAStreetUsesCover()
        {
            yield return ForgetTheWorld();
            yield return LoadScene(Veilmouth);
            var world = SliceWorldDriver.Running;
            var fights = world.Fights;
            fights.TurnPause = 0f;
            fights.BeatPace = 4f;
            var grid = fights.Grid;
            var rules = UnityEditor.AssetDatabase.LoadAssetAtPath<FightRules>("Assets/Data/Confront/FightRules.asset");

            // A cell behind a railing's Half Cover, with a free cell four steps off on the railing's other side, in sight.
            CombatCell target = default, shooter = default;
            var found = false;
            var tried = 0;
            foreach (var railing in Object.FindObjectsByType<KitEnvironmentAction>(FindObjectsSortMode.InstanceID))
            {
                if (found || tried++ >= Railings || !grid.Follow(railing.transform.position) || !grid.TryCellAt(railing.transform.position, out var near))
                {
                    continue;
                }

                found = Find(grid, near, out target, out shooter);
            }

            Assert.IsTrue(found, $"a railing with Half Cover and a shooter's cell {Off} cells off, among {tried}");

            // The player acts first, so the cultist is still behind the railing when the shot is weighed.
            world.Character.Set(CharacterAttribute.Perception, 20);
            world.Character.Set(CharacterAttribute.Dexterity, 20);
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            player.Teleport(grid.Center(shooter) + (Vector3.up * 0.05f), Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(fights.StartDevelopmentFight(SliceConfront.Cult, new[] { grid.Center(target) }), $"the development fight from {shooter} at {target}");
            var turns = fights.Turns;
            var me = fights.PlayerCombatant;
            var enemy = turns.Order.Single(c => c != me);
            Assert.AreEqual(target, enemy.Cell, "the cultist stands behind the railing");
            for (var time = 0f; time < 30f && fights.InFight && (turns.Acting != me || fights.Showing); time += Time.deltaTime)
            {
                yield return null;
            }

            Assert.AreEqual(1, turns.Round, "the player's turn comes first");
            Assert.AreEqual(target, enemy.Cell, "and the cultist has not moved");

            yield return Tap(Key.X);
            Assert.IsTrue(me.Fighter.Weapon.Ranged, "X takes the rifle");
            var context = turns.ContextFor(me, me.Cell, enemy);
            Assert.AreEqual(-rules.CoverPenalty(CoverLevel.Half), context.HitBonus, 1e-9, "the shot carries Half Cover's penalty and nothing else");
            var open = new StrikeContext { Beyond = true };
            Assert.Less(rules.Forecast(me.Fighter, enemy.Fighter, context).Chance, rules.Forecast(me.Fighter, enemy.Fighter, open).Chance, "the railing lowers the chance");

            var shot = false;
            turns.Struck += (attacker, struck, _) => shot |= attacker == me && struck == enemy && me.Fighter.Weapon.Ranged;
            Assert.IsTrue(turns.SubmitIntent(TurnIntent.Attack(enemy.Id)), "the shot");
            Assert.IsTrue(shot, "the rifle fires at the cultist behind the railing");
        }

        /// <summary>
        /// Near a railing's cell: a free cell with Half Cover on one side, and a free cell on the same level four steps
        /// off on that side, both reachable, with a clear line of sight.
        /// </summary>
        private static bool Find(ICombatGrid grid, CombatCell near, out CombatCell target, out CombatCell shooter)
        {
            target = shooter = default;
            for (var dz = -2; dz <= 2; dz++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var at = grid.Center(near) + new Vector3(dx * grid.CellSize, 0f, dz * grid.CellSize);
                    if (!grid.TryCellAt(at, out var cell) || !grid.IsReachable(cell) || grid.OccupantAt(cell) >= 0)
                    {
                        continue;
                    }

                    foreach (var (side, sx, sz) in Sides)
                    {
                        if (grid.CoverOn(cell, side) != CoverLevel.Half)
                        {
                            continue;
                        }

                        var from = grid.Center(cell) + new Vector3(sx * Off * grid.CellSize, 0f, sz * Off * grid.CellSize);
                        if (grid.TryCellAt(from, out var stand) && grid.IsReachable(stand) && grid.OccupantAt(stand) < 0
                            && Mathf.Abs(grid.Center(stand).y - grid.Center(cell).y) < 0.1f && grid.CanSee(stand, cell)
                            && grid.FindPath(stand, cell, new System.Collections.Generic.List<CombatCell>()) > 0)
                        {
                            target = cell;
                            shooter = stand;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
#endif
