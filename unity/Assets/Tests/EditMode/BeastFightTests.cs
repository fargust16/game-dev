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
    /// S3b.2, beasts in Turn-Based Mode (10.2 *Beasts of the slice*), on a floor of 24 × 24 one-metre cells: a wolf pack
    /// breaks when its first wolf falls, the rest fleeing and the fight ending; a boar walks no cell outside its territory
    /// and goes back to it when the character leaves it; and a beast never waits or takes a Defensive Stance.
    /// </summary>
    public sealed class BeastFightTests
    {
        private const string Data = "Assets/Data/Confront/";
        private const int MaxTurns = 400;

        private FightRules _rules;
        private CombatGridReader _grid;
        private TurnEconomy _turns;
        private int _won;

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _won = int.MinValue;
        }

        private static CharacterSheet Sheet(int strength, int endurance, int dexterity, int perception, int melee)
        {
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Strength, strength);
            sheet.Set(CharacterAttribute.Endurance, endurance);
            sheet.Set(CharacterAttribute.Dexterity, dexterity);
            sheet.Set(CharacterAttribute.Perception, perception);
            sheet.Set(CharacterSkill.LightMelee, melee);
            return sheet;
        }

        private T Asset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(Data + path);

        private Fighter Hero() => _rules.Fighter(Sheet(14, 10, 6, 5, 10), 8, new WeaponSet(Asset<FightWeapon>("Weapons/Sword.asset")), Asset<FightArmour>("Armour/Leather.asset"));

        private Fighter Wolf() => _rules.Fighter(Sheet(4, 3, 6, 5, 4), null, new WeaponSet(Asset<FightWeapon>("Weapons/Bite.asset")), Asset<FightArmour>("Armour/Hide.asset"));

        private Fighter Boar() => _rules.Fighter(Sheet(8, 8, 2, 2, 4), null, new WeaponSet(Asset<FightWeapon>("Weapons/Tusks.asset")), Asset<FightArmour>("Armour/ThickHide.asset"));

        private static Combatant At(int id, int side, Fighter fighter, int x, int z, bool will = false) => new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), will);

        private static Combatant Beast(int id, Fighter fighter, int x, int z, int pack, Vector3 home, float range) =>
            new Combatant(id, 1, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f)) { Beast = true, Pack = pack, Home = home, Range = range };

        private void Begin(params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(10.5f, 0.05f, 10.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.Ended += side => _won = side;
            _turns.BeginEncounter(combatants, new Random(23));
        }

        [Test]
        public void APackBreaksWhenItsFirstWolfFalls()
        {
            var home = new Vector3(10.5f, 0f, 10.5f);
            var hero = At(0, 0, Hero(), 10, 10);
            var wolves = new[] { Beast(1, Wolf(), 13, 10, 0, home, 15f), Beast(2, Wolf(), 10, 13, 0, home, 15f), Beast(3, Wolf(), 7, 10, 0, home, 15f) };
            var departed = new List<Combatant>();
            Begin(hero, wolves[0], wolves[1], wolves[2]);
            _turns.Departed += departed.Add;
            for (var n = 0; n < MaxTurns && _turns.IsRunning; n++)
            {
                _turns.PlayTurn();
            }

            Assert.IsFalse(_turns.IsRunning, "the fight ends");
            Assert.AreEqual(0, _won, "the character's side stands");
            var fallen = System.Array.FindAll(wolves, w => w.Fighter.Down);
            Assert.AreEqual(1, fallen.Length, "one wolf falls, and the others do not stay to die");
            foreach (var w in wolves)
            {
                if (!w.Fighter.Down)
                {
                    Assert.AreEqual(Departure.Fled, w.Departure, "the rest of the pack flee");
                    CollectionAssert.Contains(departed, w);
                }
            }

            Assert.IsFalse(hero.Fighter.Down);
        }

        [Test]
        public void ABoarKeepsToItsTerritoryAndGoesBackWhenTheCharacterLeavesIt()
        {
            var home = new Vector3(8.5f, 0f, 10.5f);
            var hero = At(0, 0, Hero(), 11, 10, will: true); // 3 m from the wallow, inside 4 m
            var boar = Beast(1, Boar(), 8, 10, -1, home, 4f);
            var walked = new List<CombatCell>();
            Begin(hero, boar);
            _turns.Walked += (c, cells) =>
            {
                if (c == boar)
                {
                    walked.AddRange(cells);
                }
            };

            // The boar's turns go by the rule; on its first turn the character steps out of the territory, 5 m from the
            // wallow, and then ends its turns there.
            var stepped = false;
            for (var n = 0; n < MaxTurns && _turns.IsRunning; n++)
            {
                if (_turns.Acting != hero)
                {
                    _turns.PlayTurn();
                }
                else if (!stepped)
                {
                    stepped = true;
                    Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(new CombatCell(13, 10, 0))), "the character walks out");
                    if (_turns.IsRunning && _turns.Acting == hero)
                    {
                        _turns.SubmitIntent(TurnIntent.EndTurn);
                    }
                }
                else
                {
                    _turns.SubmitIntent(TurnIntent.EndTurn);
                }
            }

            Assert.IsTrue(stepped, "the character's turn came");
            Assert.IsFalse(_turns.IsRunning, "leaving the territory ends the fight");
            Assert.AreEqual(Departure.Withdrew, boar.Departure, "the boar goes back to its territory");
            Assert.AreEqual(Departure.None, hero.Departure, "the character did not break away: the boar left");
            Assert.IsFalse(boar.Fighter.Down);
            foreach (var cell in walked)
            {
                Assert.IsTrue(TurnEconomy.Keeps(boar, _grid.Center(cell)), $"the boar never walks outside its territory: {cell.X},{cell.Z}");
            }

            Assert.IsTrue(TurnEconomy.Keeps(boar, boar.Position));
        }

        [Test]
        public void ABeastNeverWaitsOrTakesADefensiveStance()
        {
            // Far apart, the boar would wait for the character to walk into its reach, as a person with a melee weapon does.
            var hero = At(0, 0, Hero(), 4, 10);
            var boar = Beast(1, Boar(), 16, 10, -1, new Vector3(16.5f, 0f, 10.5f), 30f);
            var stances = 0;
            Begin(hero, boar);
            _turns.GuardChanged += c =>
            {
                if (c == boar && c.Fighter.Stance)
                {
                    stances++;
                }
            };

            var boarTurns = 0;
            for (var n = 0; n < MaxTurns && _turns.IsRunning; n++)
            {
                var acting = _turns.Acting;
                _turns.PlayTurn();
                if (acting == boar)
                {
                    boarTurns++;
                    Assert.IsFalse(boar.Waited, "no Wait");
                }
            }

            Assert.Greater(boarTurns, 0);
            Assert.AreEqual(0, stances, "no Defensive Stance");
        }
    }
}
