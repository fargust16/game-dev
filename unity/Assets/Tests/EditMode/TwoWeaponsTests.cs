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
    /// 10.2 §5b *Two weapons* (owner, 2026-11-11; AN4): a one-handed melee weapon in each hand, each blow struck with
    /// either hand at its own weapon's AP, the pair parrying as its better weapon, both weights carried.
    /// </summary>
    public sealed class TwoWeaponsTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;

        private FightWeapon Weapon(string name)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/" + name + ".asset");
            Assert.IsNotNull(weapon, name);
            return weapon;
        }

        [SetUp]
        public void Load()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            Assert.IsNotNull(_rules, "FightRules.asset");
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
        public void OnlyTwoOneHandedMeleeWeaponsMakeAPair()
        {
            var sword = Weapon("Sword");
            var dagger = Weapon("Dagger");
            var shield = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/Shield.asset");
            Assert.IsNull(new WeaponSet(sword, null, dagger).Fault(), "a sword and a dagger");
            Assert.IsNull(new WeaponSet(Weapon("Axe"), null, Weapon("Mace")).Fault(), "an axe and a mace");
            Assert.IsNotNull(new WeaponSet(sword, null, Weapon("Greatsword")).Fault(), "no two-handed weapon in a pair");
            Assert.IsNotNull(new WeaponSet(Weapon("Greatsword"), null, dagger).Fault(), "nor beside one");
            Assert.IsNotNull(new WeaponSet(Weapon("Pistol"), null, dagger).Fault(), "no firearm in a pair");
            Assert.IsNotNull(new WeaponSet(sword, shield, dagger).Fault(), "the other hand holds a weapon or a shield, not both");
            Assert.IsTrue(WeaponSet.OneHandedMelee(dagger));
            Assert.IsFalse(WeaponSet.OneHandedMelee(Weapon("Rifle")));
        }

        [Test]
        public void ThePairParriesAsItsBetterWeaponAndWeighsBoth()
        {
            var sword = Weapon("Sword");
            var dagger = Weapon("Dagger");
            foreach (var set in new[] { new WeaponSet(sword, null, dagger), new WeaponSet(dagger, null, sword) })
            {
                Assert.IsTrue(set.Paired);
                Assert.AreEqual(sword.GuardChance, set.GuardChance, 1e-6, "the sword's parry, in either hand");
                Assert.AreEqual(sword.GuardStops, set.GuardStops, 1e-6);
                Assert.AreEqual(sword.Weight + dagger.Weight, set.Weight, 1e-6, "both weights count in Load");
            }

            var fighter = _rules.Fighter(Peer(), 9, new WeaponSet(sword, null, dagger));
            Assert.AreEqual(dagger.AttackAp, fighter.BlowAp, "the cheapest blow a planner can buy is the dagger's");
            Assert.AreSame(sword, fighter.Weapon, "the main hand's weapon between blows");
        }

        private sealed class Duel
        {
            public TurnEconomy Turns;
            public Combatant Player;
            public Combatant Enemy;
            public readonly List<StrikeResult> Blows = new List<StrikeResult>();
        }

        /// <summary>A duel beside an enemy with a sword, the player holding <paramref name="main"/> and <paramref name="off"/>, loaded after the new scene unloads what nothing holds.</summary>
        private Duel Begin(string main, string off)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var set = new WeaponSet(Weapon(main), null, Weapon(off));
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            Physics.SyncTransforms();
            var grid = new CombatGridReader(1f);
            Assert.IsTrue(grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            var duel = new Duel { Turns = new TurnEconomy(_rules, grid) };
            duel.Player = new Combatant(0, 0, _rules.Fighter(Peer(), 9, set), new Vector3(5.5f, 0f, 5.5f), takesIntents: true);
            duel.Enemy = new Combatant(1, 1, _rules.Fighter(Peer(), 9, new WeaponSet(Weapon("Sword"))), new Vector3(6.5f, 0f, 5.5f), takesIntents: true);
            duel.Turns.Struck += (attacker, target, result) =>
            {
                if (attacker == duel.Player)
                {
                    duel.Blows.Add(result);
                }
            };
            duel.Turns.BeginEncounter(new[] { duel.Player, duel.Enemy }, new Random(3));
            if (duel.Turns.Acting != duel.Player)
            {
                Assert.IsTrue(duel.Turns.SubmitIntent(TurnIntent.EndTurn), "the enemy waits its turn");
            }

            Assert.AreEqual(duel.Player, duel.Turns.Acting, "the player's turn");
            duel.Enemy.Fighter.Hp = 1e6;
            return duel;
        }

        [Test]
        public void ASwordAndADaggerStrikeThreeTimesInTenAp()
        {
            var duel = Begin("Sword", "Dagger");
            Assert.AreEqual(10, duel.Turns.ApLeft, "a peer's ten AP");
            Assert.IsFalse(duel.Turns.OffHandFor(duel.Player, duel.Enemy, duel.Player.Cell, 10), "the cursor shows the sword's blow first");
            Assert.IsTrue(duel.Turns.OffHandFor(duel.Player, duel.Enemy, duel.Player.Cell, 3), "and with 3 AP left the dagger's");
            Assert.Less(duel.Turns.ForecastFor(duel.Player, duel.Enemy, duel.Player.Cell, true).High,
                duel.Turns.ForecastFor(duel.Player, duel.Enemy, duel.Player.Cell, false).High, "the dagger's forecast is its own");
            while (duel.Turns.SubmitIntent(TurnIntent.Attack(duel.Enemy.Id)))
            {
            }

            CollectionAssert.AreEqual(new[] { false, true, true }, duel.Blows.ConvertAll(b => b.OffHand), "the sword, then the dagger twice: 4 + 3 + 3 AP");
            Assert.AreEqual(0, duel.Turns.ApLeft);
        }

        [Test]
        public void TwoSwordsTakeTurnsAndAHandCanBeChosen()
        {
            var duel = Begin("Sword", "Sword");
            Assert.IsTrue(duel.Turns.SubmitIntent(TurnIntent.Attack(duel.Enemy.Id)));
            Assert.IsTrue(duel.Turns.SubmitIntent(TurnIntent.Attack(duel.Enemy.Id)));
            CollectionAssert.AreEqual(new[] { false, true }, duel.Blows.ConvertAll(b => b.OffHand), "even either way, the hands take turns, the main hand first");

            duel = Begin("Sword", "Dagger");
            Assert.IsTrue(duel.Turns.SubmitIntent(TurnIntent.Attack(duel.Enemy.Id, offHand: true)));
            Assert.IsTrue(duel.Blows[0].OffHand, "the hand asked for");
            Assert.AreEqual(10 - Weapon("Dagger").AttackAp, duel.Turns.ApLeft, "at the dagger's AP");
        }
    }
}
