using System;
using System.Collections.Generic;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Player.Progression;
using GameDev.Slice.Kit;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3a.7 on a small built place, a floor of 24 × 24 one-metre cells with its top at 0 m: the Environment Actions of
    /// 10.1 with their bands, AP and failures, a fall's damage and the broken leg from 4 m, Bleeding, Poison and the
    /// ordinary return of HP on ticks, and First Aid's bandage, antidote and splint (10.2). An outcome that a roll decides
    /// is played under twenty seeds, so both of its outcomes are seen and checked.
    /// </summary>
    public sealed class EnvironmentConditionTests
    {
        private const string Data = "Assets/Data/Confront/";
        private const int Seeds = 20;

        private FightRules _rules;
        private FightWeapon _sword;
        private CombatGridReader _grid;
        private TurnEconomy _turns;

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Block("Floor", -2f, -2f, 24f, 24f, -1f, 0f);
        }

        private static GameObject Block(string name, float x0, float z0, float sizeX, float sizeZ, float bottom, float top)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.position = new Vector3(x0 + (sizeX * 0.5f), (bottom + top) * 0.5f, z0 + (sizeZ * 0.5f));
            block.transform.localScale = new Vector3(sizeX, top - bottom, sizeZ);
            return block;
        }

        private static void Tag(GameObject block, EnvironmentActionKind kind, EnvironmentActionBand band) =>
            block.AddComponent<KitEnvironmentAction>().Set(kind, band);

        private static CombatCell Cell(int x, int z) => new CombatCell(x, z, 0);

        /// <summary>A person with every attribute and skill at <paramref name="all"/>, and the values given set apart.</summary>
        private static CharacterSheet Sheet(int all, params (CharacterAttribute Attribute, int Value)[] set)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, all);
            }

            foreach (CharacterSkill s in Enum.GetValues(typeof(CharacterSkill)))
            {
                sheet.Set(s, all);
            }

            foreach (var (attribute, value) in set)
            {
                sheet.Set(attribute, value);
            }

            return sheet;
        }

        private Fighter Fighter(CharacterSheet sheet) => _rules.Fighter(sheet, 9, new WeaponSet(_sword), null, default);

        private static Combatant At(int id, int side, Fighter fighter, int x, int z) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), takesIntents: true);

        private void Begin(int seed, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            _grid = new CombatGridReader(1f);
            Assert.IsTrue(_grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            _turns = new TurnEconomy(_rules, _grid);
            _turns.BeginEncounter(combatants, new Random(seed));
        }

        /// <summary>Ends the turns of everyone else until <paramref name="c"/> acts.</summary>
        private void PlayUntil(Combatant c)
        {
            for (var i = 0; i < 16 && _turns.Acting != c; i++)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            }

            Assert.AreEqual(c, _turns.Acting, $"combatant {c.Id} comes to act");
        }

        /// <summary>The chance 07.1 gives: the curve at Δ, the secondaries' share of D, Luck, and the cap.</summary>
        private static double Expected(double curve, int difficulty, int[] secondaries, int luck)
        {
            var share = 0d;
            foreach (var s in secondaries)
            {
                share += Math.Min(1d, (double)s / difficulty);
            }

            return Math.Min(0.99d, curve * (1d + (0.10d * share / secondaries.Length)) * (1d + (luck / 100d)));
        }

        [Test]
        public void TheBandsFollowTheCurveOf071()
        {
            Assert.AreEqual(new[] { 1, 3, 5 }, new[] { _rules.BandDifficulty(EnvironmentActionBand.Easy), _rules.BandDifficulty(EnvironmentActionBand.Hard), _rules.BandDifficulty(EnvironmentActionBand.VeryHard) }, "10.1: D 1, 3 and 5");
            Assert.AreEqual(new[] { 2, 3, 3, 3, 3 }, new[]
            {
                _rules.EnvironmentAp(EnvironmentActionKind.VaultJump), _rules.EnvironmentAp(EnvironmentActionKind.ClimbUp), _rules.EnvironmentAp(EnvironmentActionKind.ClimbThroughWindow),
                _rules.EnvironmentAp(EnvironmentActionKind.MantleOnto), _rules.EnvironmentAp(EnvironmentActionKind.KickBreach),
            }, "10.1: a vault 2 AP, the rest 3");

            // Vault / Jump: Dexterity, with Strength; Strength and Luck 1. The curve at Δ −4 … 5.
            var curve = new Dictionary<int, double> { { -4, 0.20 }, { -2, 0.50 }, { 0, 0.80 }, { 1, 0.90 }, { 2, 0.95 }, { 3, 0.98 }, { 5, 0.99 } };
            foreach (var dexterity in new[] { 1, 3, 6 })
            {
                var sheet = Sheet(1, (CharacterAttribute.Dexterity, dexterity));
                foreach (EnvironmentActionBand band in Enum.GetValues(typeof(EnvironmentActionBand)))
                {
                    var d = _rules.BandDifficulty(band);
                    Assert.AreEqual(Expected(curve[dexterity - d], d, new[] { 1 }, 1), _rules.EnvironmentChance(sheet, EnvironmentActionKind.VaultJump, band), 1e-5, $"Dexterity {dexterity}, {band}");
                }
            }

            // Climb Up: Strength, with Endurance and Dexterity.
            var climber = Sheet(1, (CharacterAttribute.Strength, 3), (CharacterAttribute.Endurance, 3));
            Assert.AreEqual(Expected(0.80, 3, new[] { 3, 1 }, 1), _rules.EnvironmentChance(climber, EnvironmentActionKind.ClimbUp, EnvironmentActionBand.Hard), 1e-5, "Strength 3 on a Hard climb");
            Assert.AreEqual(Expected(0.80, 1, new[] { 1 }, 1), _rules.EnvironmentChance(Sheet(1), EnvironmentActionKind.KickBreach, EnvironmentActionBand.Easy), 1e-5, "a breach: Strength, with Endurance");
        }

        [Test]
        public void AVaultClearsALowWallForTwoAPAndAFailureStaysPut()
        {
            var seen = new bool[2];
            for (var seed = 1; seed <= Seeds; seed++)
            {
                BuildThePlace();
                Tag(Block("LowWall", 5.9f, 2f, 0.2f, 7f, 0f, 1f), EnvironmentActionKind.VaultJump, EnvironmentActionBand.Hard);
                var vaulter = At(0, 0, Fighter(Sheet(1)), 5, 5);
                var enemy = At(1, 1, Fighter(Sheet(1)), 15, 15);
                Begin(seed, vaulter, enemy);
                PlayUntil(vaulter);

                Assert.IsFalse(_grid.CanStep(Cell(5, 5), Cell(6, 5)), "no step crosses the wall");
                Assert.IsTrue(_turns.TraverseFrom(vaulter, Cell(6, 5), out var option), "the wall offers its action");
                Assert.AreEqual(EnvironmentActionKind.VaultJump, option.Kind);
                Assert.AreEqual(EnvironmentActionBand.Hard, option.Band);
                Assert.AreEqual(Cell(6, 5), option.Landing, "into the cell past the wall");
                Assert.AreEqual(0f, option.Rise, 0.05f, "on level ground");
                Assert.AreEqual(2, option.Ap);
                Assert.AreEqual(_rules.EnvironmentChance(vaulter.Fighter.Values, EnvironmentActionKind.VaultJump, EnvironmentActionBand.Hard), option.Chance, 1e-9);
                Assert.IsFalse(_turns.TraverseFrom(vaulter, Cell(5, 6), out _), "an open side offers nothing");

                var success = false;
                _turns.Traversed += (c, o, ok, fallen) =>
                {
                    success = ok;
                    Assert.AreEqual(0d, fallen, "a vault never falls");
                };
                var ap = _turns.ApLeft;
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Traverse(Cell(6, 5))));
                Assert.AreEqual(ap - 2, _turns.ApLeft, "2 AP, whatever comes");
                Assert.AreEqual(success ? Cell(6, 5) : Cell(5, 5), vaulter.Cell, success ? "over the wall" : "a failed vault stays where it stood");
                Assert.AreEqual(vaulter, _turns.Acting, "and the turn goes on");
                Assert.AreEqual(vaulter.Fighter.MaxHp, vaulter.Fighter.Hp, 1e-5, "unhurt");
                seen[success ? 1 : 0] = true;
            }

            Assert.IsTrue(seen[0] && seen[1], "both outcomes were seen");
        }

        [Test]
        public void AClimbRisesOntoALedgeAndAFailureFallsBackAndEndsTheTurn()
        {
            var seen = new bool[2];
            for (var seed = 1; seed <= Seeds; seed++)
            {
                BuildThePlace();
                Tag(Block("Ledge", 6f, 2f, 5f, 7f, -1f, 2f), EnvironmentActionKind.ClimbUp, EnvironmentActionBand.Hard);
                var climber = At(0, 0, Fighter(Sheet(1)), 5, 5);
                var enemy = At(1, 1, Fighter(Sheet(1)), 15, 15);
                Begin(seed, climber, enemy);
                PlayUntil(climber);

                Assert.IsTrue(_turns.TraverseFrom(climber, Cell(6, 5), out var option), "the ledge's face offers its action");
                Assert.AreEqual(EnvironmentActionKind.ClimbUp, option.Kind);
                Assert.AreEqual(3, option.Ap);
                Assert.AreEqual(2f, option.Rise, 0.05f, "onto its top, 2 m up");

                var success = false;
                var fall = 0d;
                _turns.Traversed += (c, o, ok, fallen) =>
                {
                    success = ok;
                    fall = fallen;
                };
                var ap = _turns.ApLeft;
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Traverse(Cell(6, 5))));
                if (success)
                {
                    Assert.AreEqual(6, climber.Cell.X, "on the ledge");
                    Assert.AreEqual(2f, _grid.Center(climber.Cell).y, 0.05f);
                    Assert.AreEqual(ap - 3, _turns.ApLeft, "3 AP");
                    Assert.AreEqual(0d, fall);
                    Assert.AreEqual(climber, _turns.Acting);
                }
                else
                {
                    Assert.AreEqual(Cell(5, 5), climber.Cell, "back at its foot");
                    Assert.AreEqual(0.1d * (option.Rise / 2d) * climber.Fighter.MaxHp, fall, 1e-6, "10 % of maximum HP for each 2 m");
                    Assert.AreEqual(climber.Fighter.MaxHp - fall, climber.Fighter.Hp, 1e-6, "through no armour");
                    Assert.IsFalse(climber.Fighter.LegBroken, "2 m breaks nothing");
                    Assert.AreNotEqual(climber, _turns.Acting, "a fall ends the turn");
                }

                seen[success ? 1 : 0] = true;
            }

            Assert.IsTrue(seen[0] && seen[1], "both outcomes were seen");
        }

        [Test]
        public void AFallFromFourMetresBreaksALegWhichHoldsTheFighterInPlace()
        {
            var faller = At(0, 0, Fighter(Sheet(8)), 5, 5);
            var enemy = At(1, 1, Fighter(Sheet(8)), 9, 5);
            var f = faller.Fighter;
            Assert.AreEqual(0.1d * 3d / 2d * f.MaxHp, _rules.Fall(f, 3f), 1e-5, "3 m: 15 %");
            Assert.IsFalse(f.LegBroken);
            f.Hp = f.MaxHp;
            Assert.AreEqual(0.2d * f.MaxHp, _rules.Fall(f, 4f), 1e-5, "4 m: 20 %");
            Assert.IsTrue(f.LegBroken, "and a broken leg");

            Begin(3, faller, enemy);
            PlayUntil(faller);
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 6))), "no step on a broken leg");
            Assert.AreEqual(-1, _turns.PathToStrike(faller, enemy, new List<CombatCell>()), "nor a walk to a blow");

            faller.KnowsFirstAid = true;
            faller.Supplies[(int)Treatment.Splint] = 1;
            var ap = _turns.ApLeft;
            var success = false;
            _turns.Treated += (c, p, t, ok) => success = ok;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Splint, faller.Id)), "a splint on its own leg");
            Assert.AreEqual(ap - 6, _turns.ApLeft, "6 AP");
            Assert.AreEqual(!success, f.LegBroken, "a set splint ends it");
            Assert.AreEqual(success ? 0 : 1, faller.Supplies[(int)Treatment.Splint], "and is used; a failure keeps it");
            if (success)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 6))), "and the fighter walks again");
            }
        }

        [Test]
        public void BleedingTakesItsShareEachTickAndEndsByEndurance()
        {
            var f = Fighter(Sheet(20));
            f.Bleeding = true;
            var rolls = new Random(5);
            var ticks = 0;
            while (f.Bleeding && ticks < 500)
            {
                var hp = f.Hp;
                var tick = _rules.Tick(f, rolls, false);
                Assert.AreEqual(0.01d * f.MaxHp, tick.Bled, 1e-5, "1 % of maximum HP a tick");
                Assert.AreEqual(hp - tick.Bled, f.Hp, 1e-5);
                Assert.AreEqual(0d, tick.Recovered, "no return of HP in a fight");
                Assert.IsTrue(tick.Any);
                ticks++;
            }

            Assert.IsFalse(f.Bleeding, "Endurance 20 ends it, 10 % a tick");
            Assert.Greater(ticks, 0);
            var quiet = _rules.Tick(f, rolls, false);
            Assert.IsFalse(quiet.Any, "nothing more to tell");

            // In a fight: one tick at each round's end.
            var bleeder = At(0, 0, Fighter(Sheet(1)), 5, 5);
            var enemy = At(1, 1, Fighter(Sheet(1)), 15, 15);
            Begin(7, bleeder, enemy);
            bleeder.Fighter.Bleeding = true;
            var told = 0;
            _turns.Conditioned += (c, t) => told += c == bleeder ? 1 : 0;
            var round = _turns.Round;
            for (var i = 0; i < 4 && _turns.Round == round; i++)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            }

            Assert.AreEqual(round + 1, _turns.Round, "the round ended");
            Assert.AreEqual(1, told, "its tick was told");
            Assert.AreEqual(0.99d * bleeder.Fighter.MaxHp, bleeder.Fighter.Hp, 1e-5, "1 % lost at the round's end");
        }

        [Test]
        public void PoisonStopsTheReturnOfHPUntilItEnds()
        {
            var f = Fighter(Sheet(20));
            f.Hp = f.MaxHp * 0.5d;
            Assert.AreEqual(0.001d * f.MaxHp, _rules.Tick(f, new Random(1), true).Recovered, 1e-5, "outside a fight 0.1 % returns each tick, 1 % for each 10");
            Assert.AreEqual(0d, _rules.Tick(f, new Random(1), false).Recovered, "in a fight none");

            f.Poisoned = true;
            var rolls = new Random(9);
            var ticks = 0;
            var hp = f.Hp;
            ConditionTick tick;
            do
            {
                tick = _rules.Tick(f, rolls, true);
                if (!tick.PoisonEnded)
                {
                    Assert.AreEqual(0d, tick.Recovered, "Poison stops the return of HP");
                    Assert.AreEqual(hp, f.Hp, 1e-5, "and takes none itself");
                }

                ticks++;
            }
            while (f.Poisoned && ticks < 1000);

            Assert.IsFalse(f.Poisoned, "Endurance 20 ends it, 5 % a tick");
            Assert.AreEqual(0.001d * f.MaxHp, tick.Recovered, 1e-5, "the tick it ends, HP returns again");

            f.Hp = f.MaxHp - (0.0004d * f.MaxHp);
            _rules.Tick(f, rolls, true);
            Assert.AreEqual(f.MaxHp, f.Hp, 1e-5, "never past the maximum");
        }

        [Test]
        public void FirstAidNeedsTheConditionItsSupplyItsLessonAndItsAP()
        {
            Assert.AreEqual(0.80d * 1.01d, _rules.TreatmentChance(Sheet(1), Treatment.Bandage), 1e-5, "a bandage at Dexterity and First Aid 1: Δ 0, Luck 1");
            Assert.AreEqual(Math.Min(0.99d, 0.50d * 1.01d), _rules.TreatmentChance(Sheet(1), Treatment.Splint), 1e-5, "a splint, D 3: Δ −2");
            Assert.AreEqual(new[] { 3, 3, 6 }, new[] { _rules.TreatmentAp(Treatment.Bandage), _rules.TreatmentAp(Treatment.Antidote), _rules.TreatmentAp(Treatment.Splint) });
            Assert.AreEqual(new[] { 1, 1, 3 }, new[] { _rules.TreatmentTicks(Treatment.Bandage), _rules.TreatmentTicks(Treatment.Antidote), _rules.TreatmentTicks(Treatment.Splint) });

            var medic = At(0, 0, Fighter(Sheet(8)), 5, 5);
            var patient = At(1, 0, Fighter(Sheet(8)), 6, 5);
            var far = At(2, 0, Fighter(Sheet(8)), 8, 5);
            var enemy = At(3, 1, Fighter(Sheet(8)), 15, 15);
            Begin(13, medic, patient, far, enemy);
            PlayUntil(medic);
            patient.Fighter.Poisoned = true;
            patient.Fighter.Bleeding = true;
            patient.Fighter.LegBroken = true;
            far.Fighter.Bleeding = true;
            enemy.Fighter.Bleeding = true;
            medic.Supplies[(int)Treatment.Bandage] = 1;
            medic.Supplies[(int)Treatment.Antidote] = 1;
            var treated = new List<bool>();
            _turns.Treated += (c, p, t, ok) => treated.Add(ok);

            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Bandage, far.Id)), "only in the next cell");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Bandage, enemy.Id)), "only an ally");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Bandage, medic.Id)), "only a condition there is");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Splint, patient.Id)), "only with the item");
            Assert.IsFalse(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Antidote, patient.Id)), "an antidote needs the lesson");
            Assert.AreEqual(0, treated.Count);

            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Treat(Treatment.Bandage, patient.Id)), "a bandage needs none");
            Assert.AreEqual(ap - 3, _turns.ApLeft, "3 AP");
            Assert.AreEqual(!treated[0], patient.Fighter.Bleeding, "a success ends Bleeding");
            Assert.AreEqual(treated[0] ? 0 : 1, medic.Supplies[(int)Treatment.Bandage], "and uses the bandage; a failure keeps it");

            medic.KnowsFirstAid = true;
            ap = _turns.ApLeft;
            var given = _turns.SubmitIntent(TurnIntent.Treat(Treatment.Antidote, patient.Id));
            Assert.AreEqual(ap >= 3, given, "with the lesson, while 3 AP are left");
            if (given)
            {
                Assert.AreEqual(ap - 3, _turns.ApLeft);
                Assert.AreEqual(!treated[1], patient.Fighter.Poisoned, "a success ends Poison");
            }
        }
    }
}
