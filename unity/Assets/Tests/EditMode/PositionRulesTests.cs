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
    /// S3a.6 (10.2 §5b *Position*, *Wait and Defensive Stance*) on a small built place, a floor of 20 × 20 one-metre cells
    /// with its top at 0 m: the Reaction Attack, Surrounded, a shot's line of sight and cover, higher ground, the
    /// Point-Blank Shot, reactions from the AP left, Wait, the Defensive Stance and its Counterattack, and two Weapon Sets.
    /// Every combatant takes intents, so each turn is played by the test; Initiative is set apart by Dexterity and
    /// Perception, 100, 50 and 1, whose ranges a d20 cannot cross.
    /// </summary>
    public sealed class PositionRulesTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _sword;
        private FightWeapon _dagger;
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
            _dagger = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Dagger.asset");
            _rifle = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Rifle.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Block("Floor", -2f, -2f, 24f, 24f, -1f, 0f);
            _struck.Clear();
        }

        private static void Block(string name, float x0, float z0, float sizeX, float sizeZ, float bottom, float top)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = new Vector3(x0 + (sizeX * 0.5f), (bottom + top) * 0.5f, z0 + (sizeZ * 0.5f));
            block.transform.localScale = new Vector3(sizeX, top - bottom, sizeZ);
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        private Fighter Fighter(int quick, FightWeapon weapon, WeaponSet second = default)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in System.Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Dexterity, quick);
            sheet.Set(CharacterAttribute.Perception, quick);
            sheet.Set(CharacterSkill.MeleeSkill, 8);
            sheet.Set(CharacterSkill.RangedSkill, 8);
            var set = weapon.Ranged ? new WeaponSet(weapon, _pouch) : new WeaponSet(weapon);
            return _rules.Fighter(sheet, 9, set, null, second);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), takesIntents: true);

        private void Begin(int originX, int originZ, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(originX + 0.5f, 0.05f, originZ + 0.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.Struck += (a, t, r) => _struck.Add((a, t, r));
            _turns.BeginEncounter(combatants, new Random(11));
            foreach (var c in combatants)
            {
                Assert.AreEqual(CombatantState.Fighting, c.State, $"combatant {c.Id} stands on the grid");
            }
        }

        [Test]
        public void SurroundedCountsTheEnemiesAtTheSidesThatCanAct()
        {
            var target = At(0, 1, Fighter(50, _sword), 5, 5);
            var a = At(1, 0, Fighter(8, _sword), 4, 5);
            var b = At(2, 0, Fighter(8, _sword), 6, 5);
            var c = At(3, 0, Fighter(8, _sword), 5, 6);
            var corner = At(4, 0, Fighter(8, _sword), 6, 6);
            Begin(5, 5, target, a, b, c, corner);

            Assert.AreEqual(20d, _turns.ContextFor(a, a.Cell, target).HitBonus, 1e-9, "three at its sides: +20; the one at its corner does not count");
            c.Fighter.LostTurns = 1;
            Assert.AreEqual(10d, _turns.ContextFor(a, a.Cell, target).HitBonus, 1e-9, "a stunned enemy does not count: two, +10");
            b.Fighter.LostTurns = 1;
            Assert.AreEqual(0d, _turns.ContextFor(a, a.Cell, target).HitBonus, 1e-9, "one alone is no surrounding");
            b.Fighter.LostTurns = 0;
            Assert.AreEqual(10d, _turns.ContextFor(corner, corner.Cell, target).HitBonus, 1e-9, "an attack from anywhere gains it, the attacker counted only at a side");
        }

        [Test]
        public void LeavingAMeleeEnemysSideDrawsItsReactionAttackOnceARound()
        {
            var walker = At(0, 0, Fighter(100, _sword), 5, 5);
            var blade = At(1, 1, Fighter(1, _dagger), 6, 5);
            var gun = At(2, 1, Fighter(1, _rifle), 4, 5);
            Begin(5, 5, walker, blade, gun);
            Assert.AreEqual(walker, _turns.Acting, "the quickest acts first");
            var walked = new List<CombatCell>();
            _turns.Walked += (c, cells) => walked.AddRange(cells);

            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 4))), "a step away");
            Assert.AreEqual(1, _struck.Count, "one blow: the dagger's, never the rifle's");
            Assert.AreEqual(blade, _struck[0].Attacker);
            Assert.AreEqual(walker, _struck[0].Target);
            var parried = _struck[0].Result.GuardTried ? _rules.ReactionAp : 0;
            Assert.AreEqual(ap - 1 - parried, _turns.ApLeft, "the step's AP, and a parry from the AP left if it was tried");
            CollectionAssert.AreEqual(new[] { Cell(5, 4) }, walked);

            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 5))), "back to its side");
            Assert.AreEqual(1, _struck.Count, "stepping in beside an enemy draws nothing");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 6))), "and away again");
            Assert.AreEqual(1, _struck.Count, "once a round from each enemy");
        }

        [Test]
        public void AShotNeedsALineOfSight()
        {
            Block("Wall", 5f, 3f, 1f, 4f, 0f, 3f);
            var shooter = At(0, 0, Fighter(100, _rifle), 2, 5);
            var target = At(1, 1, Fighter(1, _sword), 8, 5);
            Begin(2, 5, shooter, target);

            Assert.IsFalse(_turns.ReachesFrom(shooter, shooter.Cell, target), "the wall stands between them");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Attack(target.Id)), "no shot through a wall");
            Assert.IsTrue(_turns.ReachesFrom(shooter, Cell(2, 9), target), "past the wall's end the line is clear");

            var path = new List<CombatCell>();
            var steps = _turns.PathToStrike(shooter, target, path);
            Assert.Greater(steps, 0, "a way to a cell with the target in sight");
            Assert.IsTrue(_turns.ReachesFrom(shooter, path[path.Count - 1], target), "the walk ends where the shot can be taken");
            for (var i = 0; i < path.Count - 1; i++)
            {
                Assert.IsFalse(_turns.ReachesFrom(shooter, path[i], target), "and at the first such cell");
            }
        }

        [Test]
        public void CoverFacingTheShooterCountsAgainstAShotBeyondTheNextCell()
        {
            // A fence 1 m high on the line x = 8, at the target's −X side; a wall 2 m high on the line x = 8 at the far
            // target's −X side.
            Block("Fence", 7.95f, 4f, 0.1f, 3f, 0f, 1f);
            Block("Wall", 7.95f, 10f, 0.1f, 3f, 0f, 2f);
            var shooter = At(0, 0, Fighter(100, _rifle), 2, 5);
            var target = At(1, 1, Fighter(1, _sword), 8, 5);
            var far = At(2, 1, Fighter(1, _sword), 8, 11);
            Begin(2, 5, shooter, target, far);

            Assert.AreEqual(-15d, _turns.ContextFor(shooter, shooter.Cell, target).HitBonus, 1e-9, "behind Half Cover, −15");
            Assert.AreEqual(-30d, _turns.ContextFor(shooter, Cell(2, 11), far).HitBonus, 1e-9, "behind Full Cover, −30");
            Assert.AreEqual(0d, _turns.ContextFor(shooter, Cell(14, 5), target).HitBonus, 1e-9, "from the other side the fence covers nothing");
            Assert.AreEqual(0d, _turns.ContextFor(shooter, Cell(8, 17), far).HitBonus, 1e-9, "nor from along the wall's own line");
            var blade = At(3, 0, Fighter(8, _sword), 0, 0);
            Assert.AreEqual(0d, _turns.ContextFor(blade, Cell(7, 4), target).HitBonus, 1e-9, "cover never counts against a blow");
        }

        [Test]
        public void HigherGroundAddsToTheHitAndTheCriticalChance()
        {
            Block("Platform", 1f, 4f, 3f, 3f, -1f, 2f);
            Block("Step", 1f, 9f, 3f, 3f, -1f, 1f);
            var shooter = At(0, 0, Fighter(100, _rifle), 6, 5);
            var target = At(1, 1, Fighter(1, _sword), 12, 5);
            Begin(6, 5, shooter, target);

            Assert.AreEqual(2f, _grid.Center(Cell(2, 5)).y, 0.05f, "the platform's top");
            var high = _turns.ContextFor(shooter, Cell(2, 5), target);
            Assert.AreEqual(10d, high.HitBonus, 1e-9, "2 m above the target: +10 to hit");
            Assert.AreEqual(10d, high.CriticalBonus, 1e-9, "and +10 to the critical chance");
            var step = _turns.ContextFor(shooter, Cell(2, 10), target);
            Assert.AreEqual(0d, step.HitBonus, 1e-9, $"1 m is below the band's {_rules.HigherGroundRise} m");
            Assert.AreEqual(0d, _turns.ContextFor(shooter, shooter.Cell, target).CriticalBonus, 1e-9, "level ground adds nothing");
        }

        [Test]
        public void APointBlankShotAimsWorseWhileAnEnemyStandsBeside()
        {
            var shooter = At(0, 0, Fighter(100, _rifle), 5, 5);
            var blade = At(1, 1, Fighter(1, _dagger), 6, 5);
            var target = At(2, 1, Fighter(1, _sword), 12, 5);
            Begin(5, 5, shooter, blade, target);

            Assert.AreEqual(-10d, _turns.ContextFor(shooter, shooter.Cell, target).HitBonus, 1e-9, "a shot past an enemy at its side");
            var close = _turns.ContextFor(shooter, shooter.Cell, blade);
            Assert.AreEqual(-10d, close.HitBonus, 1e-9, "a shot at the enemy beside it");
            Assert.IsFalse(close.Beyond, "where armour counts once");
            Assert.AreEqual(0d, _turns.ContextFor(shooter, Cell(5, 8), target).HitBonus, 1e-9, "with no enemy beside, a plain shot");
        }

        [Test]
        public void AParryOrABlockSpendsTheAPLeftAtTheEndOfATurn()
        {
            var guard = At(0, 0, Fighter(100, _sword), 5, 5);
            var blade = At(1, 1, Fighter(1, _dagger), 6, 5);
            Begin(5, 5, guard, blade);
            var held = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(held, guard.Fighter.Reserve, "the AP left wait for reactions");

            var reserve = held;
            while (_turns.Acting == blade && guard.Standing && _turns.SubmitIntent(TurnIntent.Attack(guard.Id)))
            {
                var last = _struck[_struck.Count - 1].Result;
                Assert.AreEqual(last.Hit && reserve >= _rules.ReactionAp, last.GuardTried, "a hit is parried while 2 AP are held");
                reserve -= last.GuardTried ? _rules.ReactionAp : 0;
                Assert.AreEqual(reserve, guard.Fighter.Reserve, "each parry spends 2 AP");
            }

            Assert.Greater(_struck.Count, 1, "the dagger struck more than once");
        }

        [Test]
        public void AWaiterActsAfterEveryoneTheLowestInitiativeFirst()
        {
            var a = At(0, 0, Fighter(100, _sword), 2, 2);
            var b = At(1, 1, Fighter(50, _sword), 8, 2);
            var c = At(2, 2, Fighter(1, _sword), 14, 2);
            Begin(8, 2, a, b, c);
            Assert.AreEqual(a, _turns.Acting);
            var aAp = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(2, 3))), "a step first");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Wait), "then a wait");
            Assert.IsTrue(a.Waiting);
            Assert.AreEqual(aAp - 1, a.Fighter.Reserve, "its AP wait in the pool");
            Assert.AreEqual(b, _turns.Acting);
            var bAp = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Wait));
            Assert.AreEqual(c, _turns.Acting);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));

            Assert.AreEqual(b, _turns.Acting, "of those who waited, the lower Initiative first");
            Assert.AreEqual(bAp, _turns.ApLeft, "with the AP it kept");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Wait), "once a round");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(a, _turns.Acting);
            Assert.AreEqual(aAp - 1, _turns.ApLeft);
            Assert.IsFalse(a.Waiting);
            Assert.AreEqual(1, _turns.Round, "still the first round");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(2, _turns.Round, "the round ends after those who waited");
            Assert.AreEqual(a, _turns.Acting, "and the next runs in the usual order");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Wait), "a new round, a new wait");
        }

        [Test]
        public void ADefensiveStanceRaisesEvasionAndGuardAndAnswersAMeleeAttack()
        {
            var guard = At(0, 0, Fighter(100, _sword), 5, 5);
            var blade = At(1, 1, Fighter(50, _dagger), 6, 5);
            var gun = At(2, 1, Fighter(1, _rifle), 5, 9);
            Begin(5, 5, guard, blade, gun);
            var plain = _rules.Evasion(guard.Fighter);
            var held = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Stance), "a stance with no attack this turn");
            Assert.IsTrue(guard.Fighter.Stance);
            Assert.AreEqual(held, guard.Fighter.Reserve);
            Assert.AreEqual(plain + (1.5d * held), _rules.Evasion(guard.Fighter), 1e-9, "+1.5 Evasion for each AP held");
            Assert.AreEqual(0.4d * (1d + (0.03d * held)), _rules.GuardChance(guard.Fighter), 1e-6, "a sword's 40 % parry, ×(1 + 3 % for each AP)");

            Assert.AreEqual(blade, _turns.Acting);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(guard.Id)));
            Assert.AreEqual(2, _struck.Count, "the blow and its Counterattack");
            Assert.AreEqual(guard, _struck[1].Attacker, "the stance answers");
            Assert.AreEqual(blade, _struck[1].Target);
            var spent = (_struck[0].Result.GuardTried ? _rules.ReactionAp : 0) + _sword.AttackAp + _rules.CounterattackAp;
            Assert.AreEqual(held - spent, guard.Fighter.Reserve, "a parry if tried, and the sword's AP plus 2");

            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(gun, _turns.Acting);
            var before = _struck.Count;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(guard.Id)), "a shot at the stance");
            Assert.AreEqual(before + 1, _struck.Count, "a shot passes a stance by: no Counterattack");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));

            Assert.AreEqual(guard, _turns.Acting, "the next round");
            Assert.IsFalse(guard.Fighter.Stance, "a stance holds until its own next turn");
            if (blade.Standing)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(blade.Id)));
                Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Stance), "no stance after an attack in the same turn");
            }
        }

        [Test]
        public void TheSecondWeaponSetComesToHandForTwoAP()
        {
            var both = At(0, 0, Fighter(100, _sword, new WeaponSet(_rifle, _pouch)), 2, 2);
            var one = At(1, 1, Fighter(1, _dagger), 12, 2);
            Begin(2, 2, both, one);
            var changed = 0;
            _turns.GuardChanged += c => changed += c == both ? 1 : 0;
            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.ChangeSet));
            Assert.AreEqual(_rifle, both.Fighter.Weapon, "the rifle to hand");
            Assert.AreEqual(_sword, both.Fighter.Second.Weapon, "the sword to the back");
            Assert.AreEqual(ap - _rules.WeaponSetAp, _turns.ApLeft, "for 2 AP");
            Assert.AreEqual(1, changed, "the scene is told");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.ChangeSet));
            Assert.AreEqual(_sword, both.Fighter.Weapon, "and back");
            while (_turns.ApLeft >= _rules.WeaponSetAp)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.ChangeSet));
            }

            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.ChangeSet), "not with less than 2 AP");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(one, _turns.Acting);
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.ChangeSet), "nor with no second set");
        }
    }
}
