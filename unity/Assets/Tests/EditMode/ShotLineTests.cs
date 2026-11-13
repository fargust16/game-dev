using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Player.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// CR2, 10.1 *Shot Line* (10.2 §5f) on a small built place, a floor of 20 × 20 one-metre cells with its top at 0 m:
    /// solid obstacles stop the line, which flies to the chest, the head or the hips, the first free; a Partial Obstacle
    /// makes a shot Obscured and never stops it; a person on the line takes the shot before the target is rolled, and a
    /// line with nobody on it draws no roll. Initiative is set apart by Dexterity and Perception, 100, 50 and 1.
    /// </summary>
    public sealed class ShotLineTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _sword;
        private FightWeapon _rifle;
        private FightOffHand _pouch;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private readonly List<(Combatant Attacker, Combatant Target, StrikeResult Result)> _struck = new List<(Combatant, Combatant, StrikeResult)>();

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _rifle = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Rifle.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Block("Floor", -2f, -2f, 24f, 24f, -1f, 0f);
            _struck.Clear();
        }

        private static GameObject Block(string name, float x0, float z0, float sizeX, float sizeZ, float bottom, float top)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = new Vector3(x0 + (sizeX * 0.5f), (bottom + top) * 0.5f, z0 + (sizeZ * 0.5f));
            block.transform.localScale = new Vector3(sizeX, top - bottom, sizeZ);
            return block;
        }

        /// <summary>A bush as the kit builds one (Body.Partial): a trigger on the NoGrid layer, marked a Partial Obstacle.</summary>
        private static void Bush(float x0, float z0, float sizeX, float sizeZ, float top)
        {
            var bush = Block("Bush", x0, z0, sizeX, sizeZ, 0f, top);
            bush.layer = ShotTrace.NoGridLayer;
            bush.GetComponent<Collider>().isTrigger = true;
            bush.AddComponent<PartialObstacle>();
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        private static Vector3 Feet(int x, int z) => new Vector3(x + 0.5f, 0f, z + 0.5f);

        private Fighter Fighter(int quick, FightWeapon weapon)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in System.Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Dexterity, quick);
            sheet.Set(CharacterAttribute.Perception, quick);
            sheet.Set(CharacterSkill.LightMelee, 8);
            sheet.Set(CharacterSkill.LightFirearms, 8);
            var set = weapon.Ranged ? new WeaponSet(weapon, _pouch) : new WeaponSet(weapon);
            return _rules.Fighter(sheet, 9, set, null);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z) =>
            new Combatant(id, side, fighter, Feet(x, z), takesIntents: true);

        private void Begin(Random rolls, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.Struck += (a, t, r) => _struck.Add((a, t, r));
            _turns.BeginEncounter(combatants, rolls);
            foreach (var c in combatants)
            {
                Assert.AreEqual(CombatantState.Fighting, c.State, $"combatant {c.Id} stands on the grid");
            }
        }

        [Test]
        public void TheLineFliesToTheChestTheHeadOrTheHipsTheFirstFree()
        {
            Block("Fence", 5f, 4f, 0.1f, 3f, 0f, 1f);
            Block("Parapet", 5f, 8f, 0.1f, 3f, 0f, 1.5f);
            Block("Wall", 5f, 12f, 0.1f, 3f, 0f, 3f);
            Physics.SyncTransforms();

            var low = ShotTrace.Trace(Feet(2, 5), Feet(8, 5));
            Assert.IsTrue(low.Clear, "a low fence hides the hips, not the chest");
            Assert.AreEqual(ShotTrace.Chest, low.To.y, 1e-4f, "the shot flies to the chest");
            Assert.AreEqual(ShotTrace.Muzzle, low.From.y, 1e-4f, "from the muzzle");

            var parapet = ShotTrace.Trace(Feet(2, 9), Feet(8, 9));
            Assert.IsTrue(parapet.Clear, "over a 1.5 m parapet the head is still in sight");
            Assert.AreEqual(ShotTrace.Head, parapet.To.y, 1e-4f, "the shot flies to the head");

            var wall = ShotTrace.Trace(Feet(2, 13), Feet(8, 13));
            Assert.IsFalse(wall.Clear, "a wall stops every line");
            Assert.IsFalse(wall.Obscured);
        }

        [Test]
        public void APartialObstacleMakesAShotObscuredAndNeverStopsIt()
        {
            Bush(5f, 4f, 1f, 3f, 1.6f);
            var shooter = At(0, 0, Fighter(100, _rifle), 2, 5);
            var target = At(1, 1, Fighter(1, _sword), 8, 5);
            var open = At(2, 1, Fighter(1, _sword), 8, 9);
            Begin(new Random(11), shooter, target, open);

            var line = _grid.Shot(shooter.Cell, target.Cell);
            Assert.IsTrue(line.Clear, "a bush never stops a line");
            Assert.IsTrue(line.Obscured, "but the shot across it is Obscured");
            Assert.IsTrue(_turns.ReachesFrom(shooter, shooter.Cell, target), "so the target can be chosen");
            Assert.AreEqual(-_rules.Obscured, _turns.ContextFor(shooter, shooter.Cell, target).HitBonus, 1e-9, "−20 to hit");
            Assert.AreEqual(0d, _turns.ContextFor(shooter, shooter.Cell, open).HitBonus, 1e-9, "a clear line costs nothing");
            var blade = At(3, 0, Fighter(8, _sword), 0, 0);
            Assert.AreEqual(0d, _turns.ContextFor(blade, Cell(7, 5), target).HitBonus, 1e-9, "Obscured never counts against a blow");
        }

        [Test]
        public void OneStandingInABushIsObscuredFromEverySide()
        {
            Bush(12.6f, 4.6f, 1.8f, 1.8f, 1.5f);
            Physics.SyncTransforms();
            Assert.IsTrue(ShotTrace.StandsIn(Feet(13, 5)), "it stands in the bush");
            Assert.IsFalse(ShotTrace.StandsIn(Feet(13, 8)), "three cells away it does not");
            Assert.IsTrue(ShotTrace.Trace(Feet(13, 12), Feet(13, 5)).Obscured, "a shot at it is Obscured");
            Assert.IsFalse(ShotTrace.Trace(Feet(13, 5), Feet(13, 12)).Obscured, "its own shot out of the bush is not, the bush being round the shooter");
        }

        [Test]
        public void APersonOnTheLineIsCountedAndACornerTouchedIsNot()
        {
            var shooter = At(0, 0, Fighter(100, _rifle), 2, 2);
            var target = At(1, 1, Fighter(1, _sword), 8, 8);
            var middle = At(2, 0, Fighter(50, _sword), 5, 5);
            var corner = At(3, 1, Fighter(1, _sword), 5, 4);
            var beyond = At(4, 1, Fighter(1, _sword), 10, 10);
            Begin(new Random(11), shooter, target, middle, corner, beyond);

            Assert.AreEqual(1, _turns.PeopleOnLine(shooter, shooter.Cell, target), "the friend in the middle, never the one whose corner the line touches, nor one beyond the target");
            middle.Fighter.Hp = 0d;
            Assert.AreEqual(0, _turns.PeopleOnLine(shooter, shooter.Cell, target), "the fallen take no shot");
        }

        [Test]
        public void APersonOnTheLineTakesTheShotBeforeTheTarget()
        {
            // Every roll 0: the friend on the line takes the shot, and it hits.
            var shooter = At(0, 0, Fighter(100, _rifle), 2, 5);
            var friend = At(1, 0, Fighter(50, _sword), 5, 5);
            var target = At(2, 1, Fighter(1, _sword), 8, 5);
            Begin(new Constant(0d), shooter, friend, target);
            Assert.AreEqual(shooter, _turns.Acting);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)), "the shot is taken");
            Assert.AreEqual(1, _struck.Count);
            Assert.AreEqual(friend, _struck[0].Target, "the friend on the line took it");
            Assert.IsTrue(_struck[0].Result.Hit);
            Assert.AreEqual(target.Fighter.MaxHp, target.Fighter.Hp, 1e-9, "the target is untouched");
        }

        [Test]
        public void ALineWithNobodyOnItDrawsNoRoll()
        {
            // Every roll 0.99: nobody on the line takes the shot, and the target is rolled.
            var draws = new int[2];
            for (var onLine = 0; onLine < 2; onLine++)
            {
                BuildThePlace();
                var shooter = At(0, 0, Fighter(100, _rifle), 2, 5);
                var other = At(1, 0, Fighter(50, _sword), 5, onLine == 1 ? 5 : 9);
                var target = At(2, 1, Fighter(1, _sword), 8, 5);
                var rolls = new Constant(0.99d);
                Begin(rolls, shooter, other, target);
                var before = rolls.Draws;
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)));
                Assert.AreEqual(target, _struck[0].Target, "nobody took the shot");
                draws[onLine] = rolls.Draws - before;
            }

            Assert.AreEqual(draws[0] + 1, draws[1], "one roll for the one on the line, none for a line with nobody on it");
        }

        /// <summary>Every draw the same value, counted.</summary>
        private sealed class Constant : Random
        {
            private readonly double _value;

            public Constant(double value) => _value = value;

            public int Draws { get; private set; }

            public override double NextDouble()
            {
                Draws++;
                return _value;
            }

            protected override double Sample() => NextDouble();

            public override int Next(int maxValue) => (int)(NextDouble() * maxValue);

            public override int Next(int minValue, int maxValue) => minValue + (int)(NextDouble() * (maxValue - minValue));
        }
    }
}
