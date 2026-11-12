using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Stealth;
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
    /// S3b.1, stealth in Turn-Based Mode (10.3 §7–8), on a floor of 24 × 24 one-metre cells: Stealth Mode costs 3 AP and
    /// is refused under an enemy's cone; a crouching walk costs 3 AP for two cells; an enemy does not go for a hidden
    /// fighter; a cone that comes to touch one checks once on 07.1's curve; and a fight can open with a stealth attack
    /// before Initiative, which costs nothing of the attacker's first turn.
    /// </summary>
    public sealed class StealthFightTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private StealthRules _stealth;
        private FightWeapon _sword;
        private FightWeapon _dagger;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private readonly List<(Combatant Observer, Combatant Hider, double Chance, bool Stays)> _checks = new List<(Combatant, Combatant, double, bool)>();

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _stealth = AssetDatabase.LoadAssetAtPath<StealthRules>(Data + "StealthRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _dagger = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Dagger.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _checks.Clear();
        }

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        private Fighter Fighter(int perception = 1, int dexterity = 8, int stealth = 1, FightWeapon weapon = null)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Perception, perception);
            sheet.Set(CharacterAttribute.Dexterity, dexterity);
            sheet.Set(CharacterSkill.Stealth, stealth);
            sheet.Set(CharacterSkill.MeleeSkill, 8);
            return _rules.Fighter(sheet, 9, new WeaponSet(weapon ? weapon : _sword), null, default);
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z, bool will = false) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), will);

        private void Begin(Combatant opener, Combatant target, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(10.5f, 0.05f, 10.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid) { Stealth = _stealth };
            _turns.ConeChecked += (o, h, chance, stays) => _checks.Add((o, h, chance, stays));
            _turns.BeginEncounter(combatants, new Random(17), opener, target);
        }

        [Test]
        public void StealthModeCostsThreeApAndACrouchingWalkThreeForTwoCells()
        {
            var me = At(0, 0, Fighter(100), 1, 10, will: true);
            var enemy = At(1, 1, Fighter(), 20, 10);
            Begin(null, null, me, enemy);
            Assert.AreEqual(me, _turns.Acting);
            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.CanHide(me), "nineteen cells off, no cone reaches it (15 cells since 2026-11-12)");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Hide));
            Assert.IsTrue(me.Hidden);
            Assert.AreEqual(ap - 3, _turns.ApLeft, "3 AP");
            Assert.AreEqual(3, _turns.WalkAp(me, 2));
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(1, 12))), "two cells crouching");
            Assert.AreEqual(ap - 6, _turns.ApLeft, "3 AP for two cells");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Hide), "already hidden");
        }

        [Test]
        public void StealthModeIsRefusedUnderACone()
        {
            var me = At(0, 0, Fighter(100), 6, 10, will: true);
            var enemy = At(1, 1, Fighter(), 12, 10);
            Begin(null, null, me, enemy);
            Assert.AreEqual(new Vector2Int(-1, 0), enemy.Facing, "an enemy faces the fight from the start");
            var ap = _turns.ApLeft;
            Assert.IsFalse(_turns.CanHide(me));
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Hide), "its cone covers the character");
            Assert.AreEqual(ap, _turns.ApLeft, "nothing is spent");
        }

        [Test]
        public void AnEnemyDoesNotGoForAHiddenFighter()
        {
            var me = At(0, 0, Fighter(100), 1, 10, will: true);
            var enemy = At(1, 1, Fighter(), 20, 10);
            Begin(null, null, me, enemy);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Hide));
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(enemy, _turns.Acting);
            _turns.PlayTurn();
            Assert.AreEqual(Cell(20, 10), enemy.Cell, "it has nobody to go for");
            Assert.AreEqual(me.Fighter.MaxHp, me.Fighter.Hp, 1e-9);
            Assert.IsTrue(me.Hidden);
        }

        [Test]
        public void AConeThatComesToTouchAHiddenFighterChecksOnce()
        {
            var me = At(0, 0, Fighter(100, dexterity: 4, stealth: 2), 0, 10, will: true);
            var ally = At(1, 0, Fighter(50), 11, 10, will: true);
            var enemy = At(2, 1, Fighter(4), 17, 10);
            Begin(null, null, me, ally, enemy);
            Assert.AreEqual(new Vector2Int(-1, 0), enemy.Facing, "it faces the ally, the nearer");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Hide), "seventeen cells off, beyond its cone of 15");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(ally, _turns.Acting);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(enemy, _turns.Acting);
            CollectionAssert.IsEmpty(_checks, "no cone has touched it yet");

            _turns.PlayTurn(); // it walks to the ally, and the hider comes within its cone's fifteen cells

            var mine = _checks.FindAll(c => c.Hider == me);
            Assert.AreEqual(1, mine.Count, "one check when the cone comes, none while it stays");
            Assert.AreEqual(enemy, mine[0].Observer);
            var delta = _stealth.Delta(4, 2, 0, 4, false);
            Assert.AreEqual(-1d, delta, "(4 + 2) / 2 − 4");
            Assert.AreEqual(_stealth.TurnCheckChance(delta), mine[0].Chance, 1e-9, "07.1's curve at Δ −1, no distance");
            Assert.AreEqual(mine[0].Stays, me.Hidden, "a failure reveals it at once");
        }

        [Test]
        public void AFightOpensWithAStealthAttackBeforeInitiative()
        {
            var me = At(0, 0, Fighter(1, weapon: _dagger), 10, 10, will: true);
            var enemy = At(1, 1, Fighter(100), 11, 10);
            me.Hidden = true;
            var struck = new List<(Combatant, Combatant, StrikeResult)>();
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(10.5f, 0.05f, 10.5f)));
            _turns = new TurnEconomy(_rules, _grid) { Stealth = _stealth };
            _turns.Struck += (a, t, r) => struck.Add((a, t, r));
            Combatant firstActing = null;
            _turns.TurnAdvanced += c => firstActing ??= c;
            _turns.BeginEncounter(new[] { me, enemy }, new Random(3), me, enemy);

            Assert.GreaterOrEqual(struck.Count, 1, "the stealth attack");
            Assert.AreEqual(me, struck[0].Item1, "the hider strikes first");
            Assert.AreEqual(enemy, struck[0].Item2);
            Assert.IsFalse(me.Hidden, "the attack reveals it");
            Assert.AreEqual(enemy, firstActing, "Initiative after it: Perception 100 acts first");
            if (!enemy.Fighter.Down && _turns.IsRunning)
            {
                _turns.PlayTurn();
                Assert.AreEqual(me, _turns.Acting, "and the hider's own first turn comes after");
            }
        }
    }
}
