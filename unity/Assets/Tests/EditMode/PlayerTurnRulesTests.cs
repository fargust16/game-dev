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
    /// S3a.5's rules under the cursor: the blow's forecast against the rolls it stands for, the walk to a blow before the
    /// click, and the walk told cell by cell for the bodies to follow.
    /// </summary>
    public sealed class PlayerTurnRulesTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _sword;

        [SetUp]
        public void Load()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            Assert.IsNotNull(_rules, "FightRules.asset");
            Assert.IsNotNull(_sword, "Sword.asset");
        }

        private static CharacterSheet Peer()
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in System.Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterSkill.LightMelee, 8);
            return sheet;
        }

        [Test]
        public void TheForecastIsTheHitChanceAndHoldsEveryOrdinaryHitsDamage()
        {
            var leather = AssetDatabase.LoadAssetAtPath<FightArmour>(Data + "Armour/Leather.asset");
            var attacker = _rules.Fighter(Peer(), 9, new WeaponSet(_sword));
            var target = _rules.Fighter(Peer(), 9, new WeaponSet(_sword), leather);
            var forecast = _rules.Forecast(attacker, target, default);
            Assert.AreEqual(_rules.HitChance(attacker, target), forecast.Chance, 1e-9, "the chance is the hit chance");
            Assert.Greater(forecast.High, forecast.Low, "a spread of damage");

            var rolls = new Random(5);
            var hits = 0;
            var ordinary = 0;
            for (var i = 0; i < 4000; i++)
            {
                target.Hp = target.MaxHp;
                var result = _rules.Strike(attacker, target, new StrikeContext { Unguarded = true }, rolls);
                hits += result.Hit ? 1 : 0;
                if (result.Hit && !result.Critical)
                {
                    ordinary++;
                    Assert.That(result.Damage, Is.InRange(forecast.Low - 1e-9, forecast.High + 1e-9), "an ordinary hit's damage lies within the forecast");
                }
            }

            Assert.Greater(ordinary, 1000, "enough ordinary hits");
            Assert.AreEqual(forecast.Chance, hits / 4000d, 0.03, "hits come as often as the forecast says");
        }

        [Test]
        public void TheWalkToABlowEndsBesideTheTargetAndIsToldCellByCell()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            Physics.SyncTransforms();
            var grid = new CombatGridReader(1f);
            Assert.IsTrue(grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            var turns = new TurnEconomy(_rules, grid);
            var player = new Combatant(0, 0, _rules.Fighter(Peer(), 9, new WeaponSet(_sword)), new Vector3(5.5f, 0f, 5.5f), takesIntents: true);
            var enemy = new Combatant(1, 1, _rules.Fighter(Peer(), 9, new WeaponSet(_sword)), new Vector3(9.5f, 0f, 5.5f), takesIntents: true);
            var walked = new List<CombatCell>();
            turns.Walked += (c, cells) =>
            {
                Assert.AreEqual(player, c, "the player walked");
                walked.AddRange(cells);
            };
            turns.BeginEncounter(new[] { player, enemy }, new Random(3));
            if (turns.Acting != player)
            {
                Assert.IsTrue(turns.SubmitIntent(TurnIntent.EndTurn), "the enemy waits its turn");
            }

            Assert.AreEqual(player, turns.Acting, "the player's turn");
            var path = new List<CombatCell>();
            var steps = turns.PathToStrike(player, enemy, path);
            Assert.AreEqual(3, steps, "three steps to the cell beside the enemy");
            Assert.AreEqual(new CombatCell(8, 5, 0), path[path.Count - 1], "a melee blow goes across a side");
            Assert.AreEqual(-1, turns.PathToStrike(player, player, path), "no blow at one's own side");

            var ap = turns.ApLeft;
            Assert.IsTrue(turns.SubmitIntent(TurnIntent.MoveTo(new CombatCell(8, 5, 0))), "the walk");
            CollectionAssert.AreEqual(new[] { new CombatCell(6, 5, 0), new CombatCell(7, 5, 0), new CombatCell(8, 5, 0) }, walked, "told cell by cell");
            Assert.AreEqual(ap - 3, turns.ApLeft);
            Assert.AreEqual(0, turns.PathToStrike(player, enemy, path), "beside it, the blow reaches");
            Assert.IsFalse(turns.ContextFor(player, player.Cell, enemy).Beyond, "a blow from beside is not beyond the next cell");
            Assert.IsTrue(turns.ContextFor(player, new CombatCell(5, 5, 0), enemy).Beyond, "four cells off it is");
        }
    }
}
