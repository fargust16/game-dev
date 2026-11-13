using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameDev.Confront.Rules;
using GameDev.Player.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>S3a.2: the C# fight rules, headless, against 10.2 §5b *Beasts of the slice* and `tools/balance/beast_sim.py`.</summary>
    public sealed class HeadlessFightTests
    {
        private const string Data = "Assets/Data/Confront/";
        private const int Fights = 4000;
        private const double Gate = 3.0;

        private readonly Dictionary<string, UnityEngine.Object> _assets = new Dictionary<string, UnityEngine.Object>();
        private FightRules _rules;
        private FightWeapon _club;

        [SetUp]
        public void Load()
        {
            _assets.Clear();
            _rules = Asset<FightRules>("FightRules.asset");

            // beast_sim.py's new character fights with a club, base 5 since the combat rework (light melee × 1.25, 10.2
            // §5f) and 5 AP. 10.2 has no club among its iron weapons, so it lives only in this test.
            _club = ScriptableObject.CreateInstance<FightWeapon>();
            _club.name = "Club";
            var club = new SerializedObject(_club);
            club.FindProperty("section").stringValue = "10.2 §5b Beasts of the slice, the new character's club in beast_sim.py";
            club.FindProperty("baseDamage").floatValue = 5f;
            club.FindProperty("attackAp").intValue = 5;
            club.FindProperty("weight").floatValue = 2f;
            club.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Unload() => UnityEngine.Object.DestroyImmediate(_club);

        /// <summary>Loaded once a test: the table builds about 300,000 fighters.</summary>
        private T Asset<T>(string path) where T : UnityEngine.Object
        {
            if (!_assets.TryGetValue(path, out var asset))
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(Data + path);
                Assert.IsNotNull(asset, path);
                _assets[path] = asset;
            }

            return (T)asset;
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

        private Fighter Make(CharacterSheet sheet, int? limit, FightWeapon weapon, string armour, FightModifiers modifiers = default) =>
            _rules.Fighter(sheet, limit, new WeaponSet(weapon), armour == null ? null : Asset<FightArmour>($"Armour/{armour}.asset"), modifiers: modifiers);

        private FightWeapon Weapon(string name) => Asset<FightWeapon>($"Weapons/{name}.asset");

        // beast_sim.py's characters.
        private Fighter NewCharacter() => Make(Sheet(5, 4, 3, 2, 1), null, _club, null);

        private Fighter NewArmed() => Make(Sheet(5, 4, 3, 2, 1), null, Weapon("Sword"), "Leather");

        private Fighter HeroNine() => Make(Sheet(9, 6, 4, 3, 6), 9, Weapon("Sword"), "Cloth");

        private Fighter HeroNineSet() =>
            Make(Sheet(9, 6, 4, 3, 6), 9, Weapon("Sword"), "Leather", new FightModifiers { damage = 0.10f, defence = 5f, accuracy = 3f });

        private Fighter HeroEight() => Make(Sheet(14, 10, 6, 5, 10), 8, Weapon("Sword"), "Leather");

        // 10.2 §5b *Beasts of the slice*.
        private Fighter Wolf() => Make(Sheet(4, 3, 6, 5, 4), null, Weapon("Bite"), "Hide");

        private Fighter Boar() => Make(Sheet(8, 8, 2, 2, 4), null, Weapon("Tusks"), "ThickHide");

        private Fighter GlowingWolf() => Make(Sheet(9, 8, 11, 10, 9), null, Weapon("GlowingBite"), "Hide", new FightModifiers { hp = 1f });

        private Fighter GlowingBoar() => Make(Sheet(10, 10, 4, 4, 6), null, Weapon("Tusks"), "ThickHide", new FightModifiers { hp = 0.25f });

        [Test]
        public void TheBeastTableHoldsWithinThreePoints()
        {
            var characters = new (string Name, Func<Fighter> Make)[]
            {
                ("New character, club, no armour", NewCharacter),
                ("New character, sword, leather", NewArmed),
                ("Limit 9 hero, sword, cloth", HeroNine),
                ("Limit 9 hero, city set", HeroNineSet),
                ("Limit 8 hero, leather", HeroEight),
            };
            var beasts = new (string Name, Func<Fighter[]> Make)[]
            {
                ("Two wolves", () => new[] { Wolf(), Wolf() }),
                ("Three wolves", () => new[] { Wolf(), Wolf(), Wolf() }),
                ("Boar", () => new[] { Boar() }),
                ("Glowing wolf", () => new[] { GlowingWolf() }),
                ("Glowing boar", () => new[] { GlowingBoar() }),
            };

            // The table of 10.2 §5b *Beasts of the slice*, beast_sim.py at seed 7 and 40,000 fights a line, on the combat
            // rework's rules (§5f) with Perception as every attack's secondary attribute (S5b.3).
            double[,] table =
            {
                { 15, 1, 11, 1, 1 },
                { 75, 29, 77, 59, 35 },
                { 79, 32, 79, 64, 39 },
                { 94, 58, 93, 83, 65 },
                { 98, 81, 98, 92, 82 },
            };

            var report = new StringBuilder($"{Fights} fights a cell, C# against 10.2:\n");
            var worst = 0d;
            for (var line = 0; line < characters.Length; line++)
            {
                report.Append($"  {characters[line].Name,-32}");
                for (var column = 0; column < beasts.Length; column++)
                {
                    var rolls = new Random(3102 + 10 * line + column);
                    var wins = 0;
                    for (var i = 0; i < Fights; i++)
                    {
                        var character = new[] { characters[line].Make() };
                        var foes = beasts[column].Make();
                        wins += new HeadlessFight(_rules, character, foes, rolls, sideBIsPack: true).Run() == FightSide.A ? 1 : 0;
                    }

                    var percent = 100d * wins / Fights;
                    worst = Math.Max(worst, Math.Abs(percent - table[line, column]));
                    report.Append($" {percent,5:F1} ({percent - table[line, column]:+0.0;-0.0})");
                }

                report.Append('\n');
            }

            report.Append($"  worst {worst:F1} points");
            TestContext.WriteLine(report);
            Debug.Log(report);
            Assert.LessOrEqual(worst, Gate, report.ToString());
        }

        [Test]
        public void APackBreaksAtTheEndOfTheRoundItsFirstWolfFalls()
        {
            var hero = HeroEight();
            var pack = new[] { Wolf(), Wolf() };
            var fight = new HeadlessFight(_rules, new[] { hero }, pack, new Random(5), sideBIsPack: true);
            fight.Begin();
            pack[0].Hp = 0;
            Assert.AreEqual(FightSide.A, fight.PlayRound(), "the second wolf runs");
            Assert.IsFalse(pack[1].Down, "one sword blow in the first round leaves a wolf standing");

            var lone = new[] { Wolf(), Wolf() };
            var stand = new HeadlessFight(_rules, new[] { HeroEight() }, lone, new Random(5));
            stand.Begin();
            lone[0].Hp = 0;
            Assert.IsNull(stand.PlayRound(), "people who are no pack fight on");
        }

        [Test]
        public void TheFirstTurnClosesInAndTheApLeftStaysForReactions()
        {
            // A greataxe's 7 AP and a bite's 6 are more than a first turn leaves, so nobody strikes in round 1.
            var hero = Make(Sheet(14, 10, 6, 5, 10), 8, Weapon("Greataxe"), "Leather");
            var wolf = Wolf();
            var fight = new HeadlessFight(_rules, new[] { hero }, new[] { wolf }, new Random(11));
            fight.Begin();
            Assert.IsNull(fight.PlayRound());
            Assert.AreEqual(wolf.MaxHp, wolf.Hp);
            Assert.AreEqual(hero.MaxHp, hero.Hp);
            Assert.That(hero.Reserve, Is.InRange(4, 5), "10 + Dexterity 6 / 20 with its fraction, less 6 AP to close in");
        }

        [Test]
        public void BleedingTakesOnePercentARoundUntilItStops()
        {
            var hero = Make(Sheet(14, 10, 6, 5, 10), 8, Weapon("Greataxe"), "Leather");
            var wolf = Wolf();
            var fight = new HeadlessFight(_rules, new[] { hero }, new[] { wolf }, new Random(11));
            fight.Begin();
            fight.Bleed(hero);
            fight.PlayRound();
            Assert.AreEqual(hero.MaxHp * 0.99, hero.Hp, 1e-6, "1 % of maximum HP a tick");
            Assert.AreEqual(wolf.MaxHp, wolf.Hp);

            // Endurance 10 stops it at 5 % a tick: in 4,000 ticks about 200 stops.
            var stops = 0;
            var rolls = new Random(17);
            for (var i = 0; i < 4000; i++)
            {
                var bleeder = Make(Sheet(14, 10, 6, 5, 10), 8, Weapon("Greataxe"), "Leather");
                var tick = new HeadlessFight(_rules, new[] { bleeder }, new[] { Wolf() }, rolls);
                tick.Begin();
                tick.Bleed(bleeder);
                tick.PlayRound();
                stops += tick.IsBleeding(bleeder) ? 0 : 1;
            }

            Assert.AreEqual(0.05, stops / 4000d, 0.01, "Endurance / 2 % a tick");
        }

        [Test]
        public void AStunnedFighterLosesItsTurnAndHoldsNoAp()
        {
            var hero = HeroEight();
            var wolf = Wolf();
            var fight = new HeadlessFight(_rules, new[] { hero }, new[] { wolf }, new Random(3));
            fight.Begin();
            hero.LostTurns = 1;
            fight.PlayRound();
            Assert.AreEqual(wolf.MaxHp, wolf.Hp, "no blow");
            Assert.AreEqual((0, 0), (hero.LostTurns, hero.Reserve));
        }

        [Test]
        public void AFighterWhosePotionSettlesLosesARoundForEachTickLeft()
        {
            var hero = HeroEight();
            var wolf = Wolf();
            hero.Defenceless = 2;
            var fight = new HeadlessFight(_rules, new[] { hero }, new[] { wolf }, new Random(3));
            fight.Begin();
            Assert.AreEqual((2, 2), (hero.LostTurns, hero.Defenceless), "20 §3: Stunned for the Assimilation's ticks left");
            fight.PlayRound();
            Assert.AreEqual((1, 1), (hero.LostTurns, hero.Defenceless), "each round spends one");
            fight.PlayRound();
            Assert.AreEqual((0, 0), (hero.LostTurns, hero.Defenceless));
            Assert.AreEqual(wolf.MaxHp, wolf.Hp, "no blow while Stunned");
        }

        [Test]
        public void InitiativeOrdersTheFightAndATieGoesToTheHigherDexterity()
        {
            var quick = Make(Sheet(5, 5, 6, 5, 5), null, Weapon("Sword"), null);  // (6 + 5) / 2 = 5.5
            var keen = Make(Sheet(5, 5, 4, 7, 5), null, Weapon("Sword"), null);   // (4 + 7) / 2 = 5.5
            // Draws: each fighter's first AP fraction, then for each its d20 and its tie roll.
            var fight = new HeadlessFight(_rules, new[] { keen }, new[] { quick }, new ScriptedRolls(new[] { 0d, 0d, 0.5, 0.1, 0.5, 0.9 }));
            fight.Begin();
            CollectionAssert.AreEqual(new[] { quick, keen }, fight.Order, "equal Initiative: the higher Dexterity first, whatever the tie roll");

            var slow = Make(Sheet(5, 5, 6, 5, 5), null, Weapon("Sword"), null);
            var fast = Make(Sheet(5, 5, 4, 7, 5), null, Weapon("Sword"), null);
            var second = new HeadlessFight(_rules, new[] { slow }, new[] { fast }, new ScriptedRolls(new[] { 0d, 0d, 0.1, 0.5, 0.9, 0.5 }));
            second.Begin();
            CollectionAssert.AreEqual(new[] { fast, slow }, second.Order, "the higher Initiative first");
        }
    }
}
