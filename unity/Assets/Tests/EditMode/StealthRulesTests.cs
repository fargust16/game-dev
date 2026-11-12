using System;
using GameDev.Confront.Rules;
using GameDev.Confront.Stealth;
using GameDev.Player.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// S3b.1, 10.3's numbers: the Notice Meter's times in 10.3 §3's table at 5 cells against a Limit 9 guard with
    /// Perception 4, its filling, fading, search and Alert; the Vision Cone; what the character reads by Perception and
    /// Intelligence; the check of 10.3 §8 on 07.1's curve; and the stealth attack of 10.3 §7.
    /// </summary>
    public sealed class StealthRulesTests
    {
        private const string Data = "Assets/Data/Confront/";
        private StealthRules _rules;

        [SetUp]
        public void Load()
        {
            _rules = AssetDatabase.LoadAssetAtPath<StealthRules>(Data + "StealthRules.asset");
            Assert.IsNotNull(_rules, "the asset");
            Assert.IsNotNull(_rules.Checks, "07.1's curve");
        }

        [TestCase(3, 1, 0, 4, false, -2d, 1.3d, TestName = "A new character on a lit street: Δ −2, 1.3 s")]
        [TestCase(3, 1, 4, 4, false, 0d, 3d, TestName = "The same on a street at night with no lamp: Δ 0, 3 s")]
        [TestCase(5, 5, 2, 4, false, 2d, 6.8d, TestName = "Жрица after two days in shadow: Δ +2, 6.8 s")]
        [TestCase(5, 5, 2, 4, true, 1d, 4.5d, TestName = "The same against an Alert guard, Perception 5: Δ +1, 4.5 s")]
        public void TheMeterTimesOfTheTable(int dexterity, int stealth, int darkness, int perception, bool alert, double delta, double seconds)
        {
            var d = _rules.Delta(dexterity, stealth, darkness, perception, alert);
            Assert.AreEqual(delta, d, 1e-9, "Δ");
            Assert.AreEqual(seconds, _rules.TimeToFill(d, 5), 0.05, "time to fill at 5 cells");
        }

        [Test]
        public void DeltaIsHeldAndNobodyIsSafeInTheNextCell()
        {
            Assert.AreEqual(4d, _rules.Delta(100, 100, 6, 4, false), "held at +4");
            Assert.AreEqual(-4d, _rules.Delta(1, 1, 0, 100, false), "held at −4");
            Assert.AreEqual(9d, _rules.TimeToFill(4, 1), 0.15, "10.3 §3: even Δ +4 fills the meter in 9 s in the next cell");
            Assert.AreEqual(1.5d, _rules.TimeToFill(0, 10) / _rules.TimeToFill(0, 5), 1e-6, "half as slow again at the cone's edge");
        }

        [Test]
        public void AMeterFillsNoticesOnceAndFadesInEightSeconds()
        {
            var meter = new NoticeMeter(_rules);
            var noticed = 0;
            for (var i = 0; i < 29; i++)
            {
                noticed += meter.See(3d, 0.1f, Vector3.one) ? 1 : 0;
            }

            Assert.AreEqual(0, noticed, "not before 3 s");
            Assert.Greater(meter.Fill, 0.9f);
            meter.Unseen(4f);
            Assert.AreEqual(meter.Fill, 0.47f, 0.05f, "half of a full meter fades in 4 s");
            Assert.IsFalse(meter.Alert, "an unfilled meter brings no Alert");
            meter.Unseen(8f);
            Assert.AreEqual(0f, meter.Fill);
            Assert.IsFalse(meter.Searching, "a meter that never filled sends the observer nowhere");

            for (var i = 0; i < 31; i++)
            {
                noticed += meter.See(3d, 0.1f, new Vector3(2f, 0f, 3f)) ? 1 : 0;
            }

            Assert.AreEqual(1, noticed, "the notice is told once");
            Assert.IsTrue(meter.Noticed && meter.Alert);
            Assert.AreEqual(new Vector3(2f, 0f, 3f), meter.LastSeen);
        }

        [Test]
        public void ANoticedCharacterLostIsSoughtForSixteenSecondsAndTheAlertLastsTenTicks()
        {
            var meter = new NoticeMeter(_rules);
            meter.See(0.01d, 1f, Vector3.zero);
            Assert.IsTrue(meter.Noticed);
            meter.Unseen(8f);
            Assert.IsTrue(meter.Searching, "it goes to look where it saw the character");
            Assert.AreEqual(0.5f, meter.Fill, 0.01f, "16 s to empty");
            for (var tick = 0; tick < 20; tick++)
            {
                meter.Tick();
            }

            Assert.IsTrue(meter.Alert, "Alert while the meter holds anything");
            meter.Unseen(8f);
            Assert.IsFalse(meter.Noticed || meter.Searching, "empty: it gives up");
            for (var tick = 0; tick < _rules.AlertTicks - 1; tick++)
            {
                meter.Tick();
            }

            Assert.IsTrue(meter.Alert, "still Alert after nine ticks");
            meter.See(10d, 0.1f, Vector3.zero);
            meter.Tick();
            meter.Unseen(10f);
            for (var tick = 0; tick < _rules.AlertTicks - 1; tick++)
            {
                meter.Tick();
            }

            Assert.IsTrue(meter.Alert, "seeing the character again starts the time anew");
            meter.Tick();
            Assert.IsFalse(meter.Alert, "ten ticks after the meter emptied");
        }

        [Test]
        public void AStandingCharacterHidesNothingButFillsNoMeter()
        {
            var meter = new NoticeMeter(_rules);
            meter.Watch(Vector3.zero);
            Assert.AreEqual(0f, meter.Fill, "a standing character is no secret");
            meter.See(0.01d, 1f, Vector3.zero);
            meter.Watch(Vector3.right);
            Assert.IsTrue(meter.Seeing && meter.Noticed);
            Assert.AreEqual(Vector3.right, meter.LastSeen, "a noticed character standing is followed");
        }

        [Test]
        public void TheConeReachesFifteenCellsAtItsAngle()
        {
            Assert.AreEqual(15, _rules.ConeCells, "the owner's direction of 2026-11-12");
            var half = _rules.ConeDegrees / 2f;
            Assert.IsTrue(_rules.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 14.9f), 1f), "ahead, in reach");
            Assert.IsFalse(_rules.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 15.1f), 1f), "beyond 15 cells");

            Assert.IsFalse(_rules.InCone(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -2f), 1f), "behind its back");
            var inside = Quaternion.Euler(0f, half - 1f, 0f) * Vector3.forward * 5f;
            var outside = Quaternion.Euler(0f, half + 1f, 0f) * Vector3.forward * 5f;
            Assert.IsTrue(_rules.InCone(Vector3.zero, Vector3.forward, inside, 1f), "just inside the angle");
            Assert.IsFalse(_rules.InCone(Vector3.zero, Vector3.forward, outside, 1f), "just outside it");
        }

        [Test]
        public void WhatTheCharacterReads()
        {
            Assert.IsFalse(_rules.ShowsMeter(3));
            Assert.IsTrue(_rules.ShowsMeter(4), "the meter from Perception 4");
            Assert.AreEqual(0f, _rules.ConeOpacity(2), "no cones below Intelligence 3");
            Assert.That(_rules.ConeOpacity(3), Is.GreaterThan(0f).And.LessThan(0.5f), "faint at 3");
            Assert.AreEqual(1f, _rules.ConeOpacity(6), "clear at 6");
        }

        [Test]
        public void ATurnBasedCheckFollowsTheCurve()
        {
            Assert.AreEqual(0.5d, _rules.TurnCheckChance(-2), 1e-6, "10.3 §8: Δ −2 is 50 %");
            Assert.AreEqual(0.95d, _rules.TurnCheckChance(2), 1e-6, "Δ +2 is 95 %");
            Assert.AreEqual(3, _rules.EnterAp);
            Assert.AreEqual(new[] { 2, 3, 5, 6 }, new[] { _rules.CrouchAp(1), _rules.CrouchAp(2), _rules.CrouchAp(3), _rules.CrouchAp(4) }, "3 AP for two cells");
            Assert.AreEqual(0.75f, _rules.CrouchSpeed);
        }

        [Test]
        public void AStealthAttackAddsFiftyPointsOfCriticalAndADaggerItsOwn()
        {
            var fight = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            var dagger = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Dagger.asset");
            Assert.AreEqual(1f, dagger.StealthCritical, "10.2 §5b: a dagger's 1.0");
            Assert.AreEqual(50d, fight.StealthCritical);
            var attacker = fight.Fighter(Sheet(1), 9, new WeaponSet(dagger), null, default);
            var target = fight.Fighter(Sheet(1), 9, new WeaponSet(dagger), null, default);
            var raw = fight.RawCritical(attacker, 50d);
            Assert.AreEqual(fight.RawCritical(attacker) + 0.5d, raw, 1e-9, "after Luck");
            var multiplier = fight.CriticalMultiplier(attacker, raw, dagger.StealthCritical);
            var through = 1d - fight.Reduction(fight.DefenceAgainst(attacker, target, false, true));

            var rolls = new Random(5);
            int hits = 0, criticals = 0;
            for (var i = 0; i < 4000; i++)
            {
                target.Hp = target.MaxHp * 100d;
                var result = fight.Strike(attacker, target, new StrikeContext { Stealth = true, Unguarded = true }, rolls);
                if (!result.Hit)
                {
                    continue;
                }

                hits++;
                if (result.Critical)
                {
                    criticals++;
                    var ratio = result.Damage / (fight.BaseDamage(attacker) * through);
                    Assert.That(ratio, Is.InRange(multiplier * 0.85d - 1e-6, multiplier * 1.15d + 1e-6), "a dagger's stealth critical: ×(2 + 1.0)");
                }
            }

            Assert.AreEqual(fight.CriticalChance(raw), criticals / (double)hits, 0.03d, "the critical chance with 50 points");
        }

        [Test]
        public void AStealthAttackNeverStuns()
        {
            var fight = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            var mace = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Mace.asset");
            Assert.Greater(mace.Stun, 0f, "a mace stuns at times");
            var attacker = fight.Fighter(Sheet(8), 9, new WeaponSet(mace), null, default);
            var target = fight.Fighter(Sheet(8), 9, new WeaponSet(mace), null, default);
            var rolls = new Random(9);
            int stunsOpen = 0, stunsHidden = 0;
            for (var i = 0; i < 3000; i++)
            {
                target.Hp = target.MaxHp * 100d;
                target.LostTurns = 0;
                stunsHidden += fight.Strike(attacker, target, new StrikeContext { Stealth = true, Unguarded = true }, rolls).Stunned ? 1 : 0;
                target.LostTurns = 0;
                stunsOpen += fight.Strike(attacker, target, new StrikeContext { Unguarded = true }, rolls).Stunned ? 1 : 0;
            }

            Assert.Greater(stunsOpen, 0, "an open blow stuns at times");
            Assert.AreEqual(0, stunsHidden, "10.3 §7: never from hiding");
        }

        private static CharacterSheet Sheet(int value)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, value);
            }

            sheet.Set(CharacterAttribute.Luck, 1);
            return sheet;
        }
    }
}
