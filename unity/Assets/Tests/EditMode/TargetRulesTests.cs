using System;
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
    /// S3a.8 (10.2 §5a Target Rules V0, as `tactical_sim.py` plays them) on a small built place, a floor of 24 × 24
    /// one-metre cells with its top at 0 m: Target Impact and its multipliers, the melee fighter that strikes the enemy
    /// beside it, the shooter's shot and step back, Waiting and Hold Back, the firearm that opens a fight, and the way a
    /// fighter faces. The fighter whose turn is tested acts first by Perception 100. Dexterity 8 gives every fighter
    /// 10 AP and the fraction rolled as it enters, so a first turn has 10 or 11; the rules estimate with 10.
    /// </summary>
    public sealed class TargetRulesTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _sword;
        private FightWeapon _rifle;
        private FightWeapon _dagger;
        private FightOffHand _pouch;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private readonly List<(Combatant Attacker, Combatant Target, FightWeapon Weapon)> _struck = new List<(Combatant, Combatant, FightWeapon)>();

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _rifle = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Rifle.asset");
            _dagger = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Dagger.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _struck.Clear();
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        private Fighter Fighter(int perception = 1, int melee = 8, int ranged = 1, bool shooter = false, bool opener = false, bool dagger = false)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Perception, perception);
            sheet.Set(CharacterSkill.LightMelee, melee);
            sheet.Set(CharacterSkill.LightFirearms, ranged);
            if (shooter || opener)
            {
                return _rules.Fighter(sheet, 9, new WeaponSet(_rifle, _pouch), null, new WeaponSet(_sword));
            }

            return _rules.Fighter(sheet, 9, new WeaponSet(dagger ? _dagger : _sword), null, default);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f));

        private void Begin(params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(10.5f, 0.05f, 10.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.Struck += (a, t, r) => _struck.Add((a, t, a.Fighter.Weapon));
            _turns.BeginEncounter(combatants, new Random(17));
            foreach (var c in combatants)
            {
                Assert.AreEqual(CombatantState.Fighting, c.State, $"combatant {c.Id} stands on the grid");
            }

            Assert.AreEqual(combatants[0], _turns.Acting, "the first named acts first");
        }

        [Test]
        public void EachFullTurnOfWalkingMultipliesTheScoreBySevenTenths()
        {
            var c = At(0, 0, Fighter(100), 2, 10);
            var near = At(1, 1, Fighter(), 6, 10);
            var far = At(2, 1, Fighter(), 20, 10);
            Begin(c, near, far);
            near.Fighter.Hp = far.Fighter.Hp = 1000d; // no kill, the same share
            Assert.AreEqual(_rules.ReachDiscount, _turns.Score(c, far) / _turns.Score(c, near), 1e-9, "17 steps with 10 AP: one turn of walking, ×0.7");
        }

        [Test]
        public void AKillNowCountsTwoAndAHalfTimes()
        {
            var c = At(0, 0, Fighter(100), 5, 5);
            var e = At(1, 1, Fighter(), 6, 5);
            Begin(c, e);
            var forecast = _rules.Forecast(c.Fighter, e.Fighter, _turns.ContextFor(c, c.Cell, e));
            var expected = (_rules.FullAp(c.Fighter) / _sword.AttackAp) * forecast.Chance * (forecast.Low + forecast.High) * 0.5d;
            e.Fighter.Hp = expected * 1.001d;
            var standing = _turns.Score(c, e);
            e.Fighter.Hp = expected * 0.999d;
            var killed = _turns.Score(c, e);
            Assert.AreEqual(_rules.KillBonus * 1.001d, killed / standing, 1e-6, "the whole share, and ×2.5 for the kill");
        }

        [Test]
        public void AnEnemyLosingItsTurnCountsAFifth()
        {
            var c = At(0, 0, Fighter(100), 5, 5);
            var e = At(1, 1, Fighter(), 9, 5);
            Begin(c, e);
            e.Fighter.Hp = 1000d;
            var awake = _turns.Score(c, e);
            e.Fighter.LostTurns = 1;
            Assert.AreEqual(_rules.HelplessFactor, _turns.Score(c, e) / awake, 1e-9);
        }

        [Test]
        public void AnEnemyWithThreeOfTheFightersAlliesBesideItCountsHalf()
        {
            double Crowded(bool third)
            {
                BuildThePlace();
                var c = At(0, 0, Fighter(100), 11, 10);
                var e = At(1, 1, Fighter(), 10, 10);
                var a = At(2, 0, Fighter(), 9, 10);
                var b = At(3, 0, Fighter(), 10, 11);
                var d = third ? At(4, 0, Fighter(), 10, 9) : At(4, 0, Fighter(), 2, 2);
                Begin(c, e, a, b, d);
                e.Fighter.Hp = 1000d;
                return _turns.Score(c, e);
            }

            // Three and four beside are both Surrounded's +20, so only the crowd differs.
            Assert.AreEqual(_rules.CrowdFactor, Crowded(true) / Crowded(false), 1e-9);
        }

        [Test]
        public void AMeleeFighterStrikesTheEnemyBesideItWhenItWillNotReachItsTarget()
        {
            var c = At(0, 0, Fighter(100), 3, 10);
            var beside = At(1, 1, Fighter(), 3, 11);
            var target = At(2, 1, Fighter(), 18, 10);
            Begin(c, beside, target);
            beside.Fighter.Hp = 1000d;
            target.Fighter.Hp = 1000d * 0.7d / 1.2d;
            Assert.AreEqual(1.2d, _turns.Score(c, target) / _turns.Score(c, beside), 1e-6, "the target is best, but not by half again");

            _turns.PlayTurn();
            Assert.AreEqual(Cell(3, 10), c.Cell, "it stays");
            Assert.AreEqual(2, _struck.Count, "two blows of 4 AP from 10");
            foreach (var (attacker, struck, _) in _struck)
            {
                Assert.AreEqual((c, beside), (attacker, struck), "at the enemy beside it");
            }

            Assert.AreEqual(new Vector2Int(0, 1), c.Facing, "and it faces the way it struck");
            Assert.AreNotEqual(c, _turns.Acting, "its turn is over");
        }

        [Test]
        public void AMeleeFighterLeavesTheEnemyBesideItForATargetWorthHalfAgainMore()
        {
            var c = At(0, 0, Fighter(100), 3, 10);
            var beside = At(1, 1, Fighter(), 3, 11);
            var target = At(2, 1, Fighter(), 18, 10);
            Begin(c, beside, target);
            beside.Fighter.Hp = 1000d;
            target.Fighter.Hp = 350d;
            Assert.AreEqual(2d, _turns.Score(c, target) / _turns.Score(c, beside), 1e-6);
            var ap = _turns.ApLeft;

            _turns.PlayTurn();
            Assert.IsFalse(_struck.Exists(s => s.Attacker == c), "it strikes nobody");
            Assert.IsTrue(_struck.Exists(s => s.Attacker == beside && s.Target == c), "leaving the enemy's side draws its Reaction Attack");
            if (c.Standing)
            {
                Assert.AreEqual(3 + ap, c.Cell.X, "it walks every AP toward the target");
                Assert.AreEqual(1, c.Facing.x, "facing the way it last moved");
            }
        }

        [Test]
        public void AShooterShootsItsTargetInRangeAndStepsBackWithTheAPLeft()
        {
            var shooter = At(0, 0, Fighter(100, melee: 1, ranged: 8, shooter: true), 10, 10);
            var blade = At(1, 1, Fighter(), 10, 13); // near enough that the step back stays inside Break Away's ten cells (S3a.9)
            Begin(shooter, blade);
            Assert.IsTrue(_turns.ReachesFrom(shooter, shooter.Cell, blade), "3 cells, in the rifle's range and in sight");
            var ap = _turns.ApLeft;

            _turns.PlayTurn();
            Assert.AreEqual(1, _struck.Count, "one shot");
            Assert.AreEqual((shooter, blade, _rifle), _struck[0]);
            Assert.AreEqual(Cell(10, 10 - (ap - 6)), shooter.Cell, "6 AP for the shot, the rest in steps away from the blade");
            Assert.AreEqual(0, shooter.Fighter.Reserve, "with every AP");
            Assert.AreEqual(new Vector2Int(0, -1), shooter.Facing);
        }

        [Test]
        public void AShooterWithAnEnemyBesideItTakesItsMeleeSet()
        {
            var shooter = At(0, 0, Fighter(100, melee: 1, ranged: 8, shooter: true), 10, 10);
            var blade = At(1, 1, Fighter(), 11, 10);
            Begin(shooter, blade);

            _turns.PlayTurn();
            Assert.AreEqual(_sword, shooter.Fighter.Weapon, "the sword to hand for 2 AP");
            Assert.AreEqual(2, _struck.Count, "and two blows of 4 AP from the 8 left");
            Assert.AreEqual(_sword, _struck[0].Weapon);
        }

        [Test]
        public void AFirearmCarriedToOpenTheFightGivesWayToTheMeleeSetAfterItsShot()
        {
            var opener = At(0, 0, Fighter(100, melee: 8, ranged: 8, opener: true), 10, 10);
            var blade = At(1, 1, Fighter(), 10, 14);
            Begin(opener, blade);
            var ap = _turns.ApLeft;

            _turns.PlayTurn();
            Assert.AreEqual(1, _struck.Count, "one shot");
            Assert.AreEqual(_rifle, _struck[0].Weapon);
            Assert.AreEqual(_sword, opener.Fighter.Weapon, "then the sword, for 2 AP");
            Assert.AreEqual(Cell(10, 10 + Math.Min(3, ap - 8)), opener.Cell, "and the AP left walk toward the blade");
        }

        [Test]
        public void AMeleeFighterWaitsForAnEnemyThatWillWalkIntoItsReachAndThatEnemyHoldsBack()
        {
            // 7 steps apart: neither reaches the other this turn (10 AP less the sword's 4), and each could walk into the
            // other's reach.
            var quick = At(0, 0, Fighter(100), 3, 10);
            var slow = At(1, 1, Fighter(), 11, 10);
            Begin(quick, slow);
            var quickAp = _turns.ApLeft;
            slow.Fighter.ApFraction = 0d; // its turn has 10 AP, so it cannot walk 7 and strike for 4

            _turns.PlayTurn();
            Assert.IsTrue(quick.Waiting, "the quicker waits");
            Assert.AreEqual(slow, _turns.Acting);

            _turns.PlayTurn();
            Assert.IsTrue(slow.Waited, "so does the slower, the quicker being still to act");
            Assert.AreEqual(slow, _turns.Acting, "of those who waited, the lower Initiative acts first");

            _turns.PlayTurn();
            Assert.AreEqual(Cell(11, 10), slow.Cell, "it walks into no enemy's reach");
            Assert.IsTrue(slow.Fighter.Stance, "and holds back in a Defensive Stance");
            Assert.AreEqual(10, slow.Fighter.Reserve, "with every AP held");
            Assert.AreEqual(new Vector2Int(-1, 0), slow.Facing, "still facing the fight as it began, having neither moved nor struck");
            Assert.AreEqual(quick, _turns.Acting);

            _turns.PlayTurn();
            Assert.AreEqual(Cell(10, 10), quick.Cell, "nobody is left to act: the quicker closes in");
            var blows = quickAp >= 11 ? 1 : 0;
            Assert.AreEqual(blows, _struck.FindAll(s => s.Attacker == quick).Count, "and strikes if 4 AP are left");
            Assert.AreEqual(blows, _struck.FindAll(s => s.Attacker == slow).Count, "which the stance answers with a Counterattack");
            Assert.AreEqual(2, _turns.Round);
        }

        [Test]
        public void AMeleeFighterThatCannotStrikeStopsShortOfAnEnemyStillToAct()
        {
            // 17 steps from the dagger's side: no waiting, since 17 − 10 is beyond the sword's reach of 10 − 4. The walk
            // stops at the first cell one step short of the dagger's next reach, 10 − 3, and the AP left hold a stance.
            var quick = At(0, 0, Fighter(100), 1, 10);
            var slow = At(1, 1, Fighter(dagger: true), 19, 10);
            Begin(quick, slow);
            Assert.AreEqual(17, _turns.PathToStrike(quick, slow, null));
            var ap = _turns.ApLeft;

            _turns.PlayTurn();
            Assert.IsFalse(quick.Waited, "it does not wait");
            Assert.AreEqual(Cell(10, 10), quick.Cell, "9 steps, to 8 from the dagger's side");
            Assert.IsTrue(quick.Fighter.Stance, "then a Defensive Stance");
            Assert.AreEqual(ap - 9, quick.Fighter.Reserve, "on the AP left");
            Assert.AreEqual(slow, _turns.Acting);
        }
    }
}
