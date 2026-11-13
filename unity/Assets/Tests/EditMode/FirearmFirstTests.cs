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
    /// CR3, 10.2 §5f *The Wardens' baton* and *The AI: a firearm first*, on a floor of 20 × 20 one-metre cells: the baton
    /// subdues a person and kills a beast; a Warden takes its baton beside an enemy and its firearm back when free; a
    /// shooter takes its melee set beside an enemy only when that pays; a shooter weighs its own people on the line and
    /// steps aside from them. Initiative is set apart by Dexterity and Perception, 100 and 1.
    /// </summary>
    public sealed class FirearmFirstTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _baton;
        private FightWeapon _pistol;
        private FightWeapon _rifle;
        private FightWeapon _sword;
        private FightWeapon _fists;
        private FightOffHand _pouch;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private readonly List<(Combatant Attacker, Combatant Target, StrikeResult Result)> _struck = new List<(Combatant, Combatant, StrikeResult)>();

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _baton = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/WardensBaton.asset");
            _pistol = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Pistol.asset");
            _rifle = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Rifle.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _fists = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Fists.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _struck.Clear();
        }

        private WeaponSet Set(FightWeapon weapon) => weapon.Ranged ? new WeaponSet(weapon, _pouch) : new WeaponSet(weapon);

        private Fighter Fighter(int quick, FightWeapon weapon, FightWeapon second = null, int firearms = 8)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in System.Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Dexterity, quick);
            sheet.Set(CharacterAttribute.Perception, quick);
            sheet.Set(CharacterSkill.LightMelee, 8);
            sheet.Set(CharacterSkill.LightFirearms, firearms);
            return _rules.Fighter(sheet, 9, Set(weapon), null, second != null ? Set(second) : default);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z, bool intents = false) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), takesIntents: intents);

        private void Begin(Random rolls, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.Struck += (a, t, r) => _struck.Add((a, t, r));
            _turns.BeginEncounter(combatants, rolls);
        }

        [Test]
        public void TheBatonIsTheWardensSubduingLightMelee()
        {
            Assert.IsNotNull(_baton);
            Assert.IsTrue(_baton.Subdues);
            Assert.IsFalse(_baton.Ranged);
            Assert.IsFalse(_baton.Heavy);
            Assert.AreEqual(5f, _baton.BaseDamage, 1e-6f);
            Assert.AreEqual(4, _baton.AttackAp);
            Assert.AreEqual(5f, _baton.ToHit, 1e-6f);
            Assert.AreEqual(Guard.Parry, _baton.Guard);
            Assert.AreEqual(0.3f, _baton.GuardChance, 1e-6f);
            Assert.AreEqual(CharacterSkill.LightMelee, _baton.Skill);
            Assert.AreEqual(CharacterAttribute.Dexterity, _baton.ProfileAttribute);
            Assert.AreEqual(1, _baton.Requirements.Count);
            Assert.AreEqual(3, _baton.Requirements[0].atStageNine);
        }

        [Test]
        public void TheBatonSubduesAPersonAndKillsABeast()
        {
            for (var beast = 0; beast < 2; beast++)
            {
                BuildThePlace();
                var warden = At(0, 0, Fighter(100, _baton), 5, 5, intents: true);
                var target = At(1, 1, Fighter(1, _fists), 6, 5, intents: true);
                target.Beast = beast == 1;
                Begin(new ScriptedAlways(0d), warden, target);
                target.Fighter.Hp = 0.5d;
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)), "the blow is struck");
                Assert.IsTrue(target.Fighter.Down, "and brings the target down");
                Assert.AreEqual(beast == 0, target.Subdued, beast == 0 ? "a person lies unconscious, not dead" : "a beast dies");
            }
        }

        [Test]
        public void NoOtherWeaponSubdues()
        {
            var shooter = At(0, 0, Fighter(100, _pistol), 5, 5, intents: true);
            var target = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
            Begin(new ScriptedAlways(0d), shooter, target);
            target.Fighter.Hp = 0.5d;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)));
            Assert.IsTrue(target.Fighter.Down);
            Assert.IsFalse(target.Subdued, "a shot kills");
        }

        [Test]
        public void AWardenTakesItsBatonBesideAnEnemyAndItsFirearmBackWhenFree()
        {
            var warden = At(0, 0, Fighter(100, _pistol, _baton), 5, 5);
            var enemy = At(1, 1, Fighter(1, _fists), 6, 5, intents: true);
            Begin(new Random(11), warden, enemy);
            enemy.Fighter.Hp = 1000d;
            Assert.AreEqual(warden, _turns.Acting);
            _turns.PlayTurn();
            Assert.AreEqual(_baton, warden.Fighter.Set.Weapon, "beside an enemy the Warden takes its baton");
            Assert.IsTrue(_struck.Exists(s => s.Attacker == warden), "and strikes with it");

            BuildThePlace();
            var free = At(0, 0, Fighter(100, _baton, _pistol), 5, 5);
            var far = At(1, 1, Fighter(1, _fists), 9, 5, intents: true);
            Begin(new Random(11), free, far);
            _turns.PlayTurn();
            Assert.AreEqual(_pistol, free.Fighter.Set.Weapon, "with nobody beside it, its firearm comes back");
        }

        [Test]
        public void AShooterTakesItsMeleeSetBesideAnEnemyOnlyWhenItPays()
        {
            var weak = At(0, 0, Fighter(100, _rifle, _fists, firearms: 20), 5, 5);
            var enemy = At(1, 1, Fighter(1, _fists), 6, 5, intents: true);
            Begin(new Random(11), weak, enemy);
            enemy.Fighter.Hp = 1000d;
            _turns.PlayTurn();
            Assert.AreEqual(_rifle, weak.Fighter.Set.Weapon, "its fists do less than point-blank shots: it keeps the rifle");
            Assert.IsTrue(_struck.Exists(s => s.Attacker == weak), "and shoots");

            // Dexterity 30 gives 11 AP: one rifle shot, aiming worse beside the enemy, or two sword blows after the change.
            BuildThePlace();
            var armed = At(0, 0, Fighter(30, _rifle, _sword, firearms: 9), 5, 5);
            var other = At(1, 1, Fighter(1, _fists), 6, 5, intents: true);
            Begin(new Random(11), armed, other);
            other.Fighter.Hp = 1000d;
            Assert.AreEqual(armed, _turns.Acting);
            _turns.PlayTurn();
            Assert.AreEqual(_sword, armed.Fighter.Set.Weapon, "a sword's two blows do more than one point-blank shot: it changes");
        }

        [Test]
        public void AShooterWeighsItsOwnPeopleOnTheLine()
        {
            // The same three, the friend once on the line and once off it: nothing else in the score changes.
            var scores = new double[2];
            for (var onLine = 0; onLine < 2; onLine++)
            {
                BuildThePlace();
                var shooter = At(0, 0, Fighter(100, _rifle, null, 20), 2, 5);
                var friend = At(1, 0, Fighter(50, _rifle, null, 20), 4, onLine == 1 ? 5 : 9);
                var target = At(2, 1, Fighter(1, _fists), 12, 5, intents: true);
                Begin(new Random(11), shooter, friend, target);
                Assert.AreEqual(shooter, _turns.Acting);
                Assert.AreEqual(onLine, _turns.PeopleOnLine(shooter, shooter.Cell, target));
                scores[onLine] = _turns.Score(shooter, target);
            }

            var clear = scores[0];
            var behind = scores[1];
            Assert.Greater(clear, 0d);
            Assert.AreEqual(1d - _rules.Intercept, behind / clear, 1e-9, "a target behind a friend counts 0.4 as much");
        }

        [Test]
        public void AShooterStepsAsideFromAFriendOnTheLine()
        {
            var shooter = At(0, 0, Fighter(100, _rifle, null, 20), 2, 5);
            var friend = At(1, 0, Fighter(50, _fists), 4, 5, intents: true);
            var target = At(2, 1, Fighter(1, _fists), 12, 5, intents: true);
            Begin(new Random(11), shooter, friend, target);
            target.Fighter.Hp = 1000d;
            var onLineAtShot = -1;
            CombatCell? from = null;
            _turns.Struck += (a, t, r) =>
            {
                if (a == shooter && from == null)
                {
                    from = shooter.Cell;
                    onLineAtShot = _turns.PeopleOnLine(shooter, shooter.Cell, target);
                }
            };

            Assert.AreEqual(shooter, _turns.Acting);
            _turns.PlayTurn();
            Assert.IsTrue(from.HasValue, "it shot");
            Assert.AreNotEqual(new CombatCell(2, 5, 0), from.Value, "from another cell than its own");
            Assert.AreEqual(0, onLineAtShot, "with nobody on the line");
            Assert.AreEqual(target, _struck.Find(s => s.Attacker == shooter).Target, "and the target took it");
        }

        /// <summary>Every draw the same value.</summary>
        private sealed class ScriptedAlways : Random
        {
            private readonly double _value;

            public ScriptedAlways(double value) => _value = value;

            public override double NextDouble() => _value;

            protected override double Sample() => _value;

            public override int Next(int maxValue) => (int)(_value * maxValue);

            public override int Next(int minValue, int maxValue) => minValue + (int)(_value * (maxValue - minValue));
        }
    }
}
