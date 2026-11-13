using System;
using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Rules;
using GameDev.Player.Progression;
using NUnit.Framework;
using UnityEditor;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>S3a.1: the fight's numbers against 10.2 §5b.</summary>
    public sealed class FightRulesTests
    {
        private const string Data = "Assets/Data/Confront/";
        private const double Tight = 1e-5;

        private FightRules _rules;

        [SetUp]
        public void Load()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            Assert.IsNotNull(_rules, "FightRules.asset");
        }

        private static FightWeapon Weapon(string name)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<FightWeapon>($"{Data}Weapons/{name}.asset");
            Assert.IsNotNull(weapon, name);
            return weapon;
        }

        private static FightOffHand OffHand(string name)
        {
            var offHand = AssetDatabase.LoadAssetAtPath<FightOffHand>($"{Data}Weapons/{name}.asset");
            Assert.IsNotNull(offHand, name);
            return offHand;
        }

        private static FightArmour Armour(string name)
        {
            var armour = AssetDatabase.LoadAssetAtPath<FightArmour>($"{Data}Armour/{name}.asset");
            Assert.IsNotNull(armour, name);
            return armour;
        }

        private static CharacterSheet Sheet(int strength, int endurance, int dexterity, int perception, int melee, int ranged = 1, int luck = 1)
        {
            var sheet = new CharacterSheet();
            sheet.Set(CharacterAttribute.Strength, strength);
            sheet.Set(CharacterAttribute.Endurance, endurance);
            sheet.Set(CharacterAttribute.Dexterity, dexterity);
            sheet.Set(CharacterAttribute.Perception, perception);
            sheet.Set(CharacterAttribute.Luck, luck);
            sheet.Set(CharacterSkill.LightMelee, melee);
            sheet.Set(CharacterSkill.LightFirearms, ranged);
            return sheet;
        }

        private Fighter Armed(CharacterSheet sheet, string weapon, string armour = null, int? limit = null, string offHand = null) =>
            _rules.Fighter(sheet, limit, new WeaponSet(Weapon(weapon), offHand == null ? null : OffHand(offHand)), armour == null ? null : Armour(armour));

        // 10.2 §5b *Reference fighters of the slice*.
        private Fighter ReferenceNine(string weapon = "Sword") => Armed(Sheet(6, 6, 4, 4, 5), weapon, "Cloth", 9);

        private Fighter ReferenceEight() => Armed(Sheet(12, 10, 8, 6, 12), "Sword", "Leather", 8);

        private Fighter Rifleman(int strength = 5, string armour = null) =>
            Armed(Sheet(strength, 6, 6, 6, 3, ranged: 12), "Rifle", armour, 8, "CartridgePouch");

        [Test]
        public void PeersHitAboutSeventyOnePercent()
        {
            var a = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            var b = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            Assert.AreEqual(_rules.Accuracy(a), _rules.Evasion(b), Tight, "peers: accuracy equals Evasion");
            Assert.AreEqual(0.707, _rules.HitChance(a, b), Tight, "70 % × (1 + Luck 1 / 100)");

            // The slice's Limit 9 fighters, with a weapon that adds nothing to hit.
            Assert.AreEqual(0.71, _rules.HitChance(ReferenceNine("Axe"), ReferenceNine("Axe")), 0.01);

            var rolls = new Random(9105);
            var hits = 0;
            const int strikes = 20000;
            for (var i = 0; i < strikes; i++)
            {
                b.Hp = b.MaxHp;
                hits += _rules.Strike(a, b, default, rolls).Hit ? 1 : 0;
            }

            Assert.AreEqual(0.707, hits / (double)strikes, 0.01, "the rolls land on the chance");
        }

        [Test]
        public void TheHitChanceTakesLuckAndStaysBetweenFiveAndNinetyFivePercent()
        {
            var target = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            Assert.AreEqual(0.70 * 1.21, _rules.HitChance(Armed(Sheet(10, 10, 10, 10, 10, luck: 21), "Axe"), target), Tight);
            Assert.AreEqual(0.95, _rules.HitChance(Armed(Sheet(10, 10, 10, 10, 10, luck: 51), "Axe"), target), Tight);
            Assert.AreEqual(0.95, _rules.HitChance(Armed(Sheet(90, 10, 90, 90, 90), "Axe"), target), Tight);
            Assert.AreEqual(0.05, _rules.HitChance(Armed(Sheet(10, 10, 10, 10, 10), "Axe"), Armed(Sheet(10, 10, 99, 99, 1), "Axe")), Tight);
            Assert.AreEqual(0.95, _rules.HitChance(Armed(Sheet(10, 10, 10, 10, 10), "Axe"), target, bonusPoints: 40), Tight, "situational points");
        }

        [Test]
        public void PlateStopsAThirdOfABlowAndHalfABullet()
        {
            var plate = Armed(Sheet(15, 10, 6, 6, 10), "Sword", "Plate", 8);
            var leather = ReferenceEight();
            var swordsman = ReferenceEight();
            var rifleman = Rifleman();

            Assert.AreEqual(0.32, _rules.Reduction(_rules.DefenceAgainst(swordsman, plate, false, false)), 0.005, "a blow on plate");
            Assert.AreEqual(0.49, _rules.Reduction(_rules.DefenceAgainst(rifleman, plate, true, false)), 0.005, "a bullet on plate");
            Assert.AreEqual(0.14, _rules.Reduction(_rules.DefenceAgainst(swordsman, leather, false, false)), 0.005, "a blow on leather");
            Assert.AreEqual(0.24, _rules.Reduction(_rules.DefenceAgainst(rifleman, leather, true, false)), 0.005, "a bullet on leather");
            Assert.AreEqual(24d / 74d, _rules.Reduction(_rules.DefenceAgainst(rifleman, plate, false, false)), Tight, "point blank: plain defence");
            Assert.AreEqual(0.75, _rules.Reduction(1000), Tight, "reduction stops at 75 %");

            // The same through whole strikes: a hit, no critical, the middle of the ±15 % roll.
            var blow = _rules.Strike(swordsman, plate, default, Scripted(0, 0.99, 0.5));
            Assert.AreEqual(6.25 * (1 - 24d / 74d), blow.Damage, Tight, "10.2 §5f: no attribute adds damage");
            var shot = _rules.Strike(rifleman, plate, new StrikeContext { Beyond = true }, Scripted(0, 0.99, 0.5));
            Assert.AreEqual(9 * (1 - 48d / 98d), shot.Damage, Tight, "the rifle's base 9");
            Assert.AreEqual(plate.MaxHp - blow.Damage - shot.Damage, plate.Hp, Tight);
        }

        [Test]
        public void DamageRollsWithinFifteenPercent()
        {
            var a = ReferenceEight();
            var b = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            var mean = _rules.BaseDamage(a);
            Assert.AreEqual(mean * 0.85, _rules.Strike(a, b, default, Scripted(0, 0.99, 0)).Damage, Tight);
            Assert.AreEqual(mean * 1.15, _rules.Strike(a, b, default, Scripted(0, 0.99, 1)).Damage, Tight);
            Assert.AreEqual(1, _rules.StageFactor(9), Tight, "Stage Factor at stage 9");
            Assert.AreEqual(1.35, _rules.StageFactor(8), Tight);
            Assert.AreEqual(1 + 0.35 * 7, _rules.StageFactor(2), Tight);
        }

        [Test]
        public void ACriticalHitDoublesAndAMeleeOneHalvesPhysicalDefence()
        {
            var plate = Armed(Sheet(15, 10, 6, 6, 10), "Sword", "Plate", 8);
            Assert.AreEqual(12, _rules.DefenceAgainst(ReferenceEight(), plate, false, true), Tight, "a critical blow finds the gap");
            Assert.AreEqual(48, _rules.DefenceAgainst(Rifleman(), plate, true, true), Tight, "a critical shot does not");

            var a = ReferenceEight();
            var raw = _rules.RawCritical(a);
            Assert.AreEqual((0.05 + 0.90 * 5 / 99d) * 1.01, raw, Tight, "Perception 6, Luck 1");
            Assert.AreEqual(2, _rules.CriticalMultiplier(a, raw), Tight);

            var keen = Armed(Sheet(10, 10, 10, 100, 10), "Dagger");
            var keenRaw = _rules.RawCritical(keen);
            Assert.AreEqual((0.95 + 0.25) * 1.01, keenRaw, Tight, "the dagger's points come before Luck");
            Assert.AreEqual(0.95, _rules.CriticalChance(keenRaw), Tight);
            Assert.AreEqual(2 + keenRaw - 0.95, _rules.CriticalMultiplier(keen, keenRaw), Tight, "the excess over 95 % adds to ×2");

            var target = Armed(Sheet(15, 10, 6, 6, 10), "Sword", "Plate", 8);
            var crit = _rules.Strike(a, target, default, Scripted(0, 0, 0.5));
            Assert.IsTrue(crit.Critical);
            Assert.AreEqual(_rules.BaseDamage(a) * 2 * (1 - 12d / 62d), crit.Damage, Tight);
        }

        [Test]
        public void BlocksAndParriesComeFromTheItemAndSpendTheReactionAp()
        {
            var a = ReferenceEight();
            var swordsman = ReferenceEight();
            swordsman.Reserve = 2;
            var parried = _rules.Strike(a, swordsman, default, Scripted(0, 0.99, 0.5, 0.39));
            Assert.IsTrue(parried.GuardTried && parried.Guarded);
            Assert.AreEqual(0, parried.Damage, Tight, "a parry stops all");
            Assert.AreEqual(0, swordsman.Reserve);

            var unanswered = _rules.Strike(a, swordsman, default, Scripted(0, 0.99, 0.5));
            Assert.IsFalse(unanswered.GuardTried, "no AP left to parry");

            var shielded = Armed(Sheet(12, 10, 8, 6, 12), "Sword", "Leather", 8, "Shield");
            shielded.Reserve = 4;
            var blocked = _rules.Strike(a, shielded, default, Scripted(0, 0.99, 0.5, 0.49));
            var plain = _rules.Strike(a, ReferenceEight(), default, Scripted(0, 0.99, 0.5));
            Assert.AreEqual(plain.Damage * 0.2, blocked.Damage, Tight, "a shield blocks 50 % of the time and stops 80 %");
            var failed = _rules.Strike(a, shielded, default, Scripted(0, 0.99, 0.5, 0.5));
            Assert.IsTrue(failed.GuardTried && !failed.Guarded);
            Assert.AreEqual(plain.Damage, failed.Damage, Tight);
            Assert.AreEqual(0, shielded.Reserve);

            var great = Armed(Sheet(12, 10, 8, 6, 12), "Greatsword", "Leather", 8);
            great.Reserve = 2;
            var block = _rules.Strike(a, great, default, Scripted(0, 0.99, 0.5, 0.1));
            Assert.AreEqual(plain.Damage * 0.4, block.Damage, Tight, "a two-handed weapon blocks and stops 60 %");

            var stood = ReferenceEight();
            stood.Reserve = 2;
            Assert.IsFalse(_rules.Strike(Rifleman(), stood, default, Scripted(0, 0.99, 0.5)).GuardTried, "a shot cannot be parried");
            Assert.AreEqual(2, stood.Reserve);
            Assert.IsFalse(_rules.Strike(a, stood, new StrikeContext { Unguarded = true }, Scripted(0, 0.99, 0.5)).GuardTried);
            stood.LostTurns = 1;
            Assert.IsFalse(_rules.CanGuard(stood), "a stunned fighter cannot parry");
        }

        [Test]
        public void AStunTakesTheNextTurnOfATargetThatStands()
        {
            var maul = Armed(Sheet(16, 10, 6, 6, 12), "Maul", "Leather", 8);
            var target = ReferenceEight();
            var stun = _rules.Strike(maul, target, default, Scripted(0, 0.99, 0.5, 0.07));
            Assert.IsTrue(stun.Stunned);
            Assert.AreEqual(1, target.LostTurns);
            _rules.Strike(maul, target, default, Scripted(0, 0.99, 0.5, 0));
            Assert.AreEqual(1, target.LostTurns, "a target already losing a turn loses no more");
            Assert.IsFalse(_rules.Strike(maul, ReferenceEight(), default, Scripted(0, 0.99, 0.5, 0.08)).Stunned, "8 % of hits");

            var weak = ReferenceEight();
            weak.Hp = 1;
            var kill = _rules.Strike(maul, weak, default, Scripted(0, 0.99, 0.5));
            Assert.IsTrue(kill.Downed && !kill.Stunned);
        }

        [Test]
        public void AGreatswordCleavesForHalfAndOnlyAGreatsword()
        {
            var a = Armed(Sheet(12, 10, 8, 6, 12), "Greatsword", "Leather", 8);
            var first = _rules.Strike(a, ReferenceEight(), default, Scripted(0, 0.99, 0.5));
            var second = _rules.Cleave(a, ReferenceEight(), default, Scripted(0, 0.99, 0.5));
            Assert.AreEqual(first.Damage * 0.5, second.Damage, Tight);
            Assert.Throws<InvalidOperationException>(() => _rules.Cleave(ReferenceEight(), ReferenceEight(), default, Scripted(0, 0.99, 0.5)));
        }

        [Test]
        public void ABeastsCriticalHitBleedsAndAGlowingBitePoisons()
        {
            var wolf = Armed(Sheet(4, 3, 6, 5, 4), "Bite", "Hide");
            var bitten = _rules.Strike(wolf, ReferenceEight(), default, Scripted(0, 0, 0.5));
            Assert.IsTrue(bitten.Critical && bitten.Bleeds);
            Assert.IsFalse(_rules.Strike(wolf, ReferenceEight(), default, Scripted(0, 0.99, 0.5)).Bleeds);

            var glowing = Armed(Sheet(9, 8, 11, 10, 9), "GlowingBite", "Hide");
            Assert.IsTrue(_rules.Strike(glowing, ReferenceEight(), default, Scripted(0, 0.99, 0.5, 0.29)).Poisoned, "30 %");
            Assert.IsFalse(_rules.Strike(glowing, ReferenceEight(), default, Scripted(0, 0.99, 0.5, 0.31)).Poisoned);
            Assert.IsFalse(_rules.Strike(ReferenceEight(), ReferenceEight(), default, Scripted(0, 0, 0.5)).Bleeds, "a sword's critical hit draws no Bleeding");
            var parrying = ReferenceEight();
            parrying.Reserve = 2;
            Assert.IsFalse(_rules.Strike(wolf, parrying, default, Scripted(0, 0, 0.5, 0.1)).Bleeds, "a parried critical bite draws none");
        }

        // 10.2 §5f: the combat rework's bases; a beast's natural weapon takes in its old Strength factor and aim.
        [TestCase("Dagger", 3.75, 3, 10, 25, Guard.Parry, 0.30, 1.0, 0, 0, false, false, 1)]
        [TestCase("Sword", 6.25, 4, 5, 0, Guard.Parry, 0.40, 1.0, 0, 0, false, false, 1)]
        [TestCase("Axe", 6.25, 5, 0, 20, Guard.Parry, 0.30, 1.0, 0, 0, false, false, 1)]
        [TestCase("Mace", 8.1, 5, 0, -3, Guard.Parry, 0.30, 1.0, 0.05, 0, false, false, 1)]
        [TestCase("Greatsword", 11.5, 6, 5, 0, Guard.Block, 0.40, 0.6, 0, 0.5, true, false, 1)]
        [TestCase("Greataxe", 11.5, 7, 0, 22, Guard.Block, 0.40, 0.6, 0, 0, true, false, 1)]
        [TestCase("Maul", 14.2, 7, 0, -3, Guard.Block, 0.40, 0.6, 0.08, 0, true, false, 1)]
        [TestCase("Pistol", 6.5, 4, -10, 0, Guard.None, 0, 0, 0, 0, false, true, 6)]
        [TestCase("Rifle", 9, 6, 0, 0, Guard.None, 0, 0, 0, 0, true, true, 16)]
        [TestCase("Bite", 5.4, 6, -0.5, 5, Guard.None, 0, 0, 0, 0, false, false, 1)]
        [TestCase("Tusks", 6.4, 5, 1, 0, Guard.None, 0, 0, 0, 0, false, false, 1)]
        public void TheWeaponTableHoldsItsValues(string name, double damage, int ap, double hit, double critical, Guard guard, double chance,
            double stops, double stun, double cleave, bool twoHanded, bool ranged, int range)
        {
            var w = Weapon(name);
            Assert.AreEqual(damage, w.BaseDamage, Tight, "base damage");
            Assert.AreEqual(ap, w.AttackAp, "AP");
            Assert.AreEqual(hit, w.ToHit, Tight, "to hit");
            Assert.AreEqual(critical, w.Critical, Tight, "critical chance");
            Assert.AreEqual(guard, w.Guard, "guard");
            Assert.AreEqual(chance, w.GuardChance, Tight, "guard chance");
            Assert.AreEqual(stops, w.GuardStops, Tight, "damage stopped");
            Assert.AreEqual(stun, w.Stun, Tight, "stun");
            Assert.AreEqual(cleave, w.Cleave, Tight, "cleave");
            Assert.AreEqual(twoHanded, w.TwoHanded, "hands");
            Assert.AreEqual(ranged, w.Ranged, "firearm");
            Assert.AreEqual(range, w.RangeCells, "range");
            StringAssert.StartsWith("10.2 §5b", w.Section, "names its section of 10.2");
        }

        [Test]
        public void TheWeaponsKeepTheirTraits()
        {
            string[] oneHanded = { "Dagger", "Sword", "Axe", "Mace" };
            string[] twoHanded = { "Greatsword", "Greataxe", "Maul" };
            var melee = oneHanded.Concat(twoHanded).Select(Weapon).ToList();
            var dagger = Weapon("Dagger");
            Assert.IsTrue(melee.All(w => w == dagger || (w.AttackAp > dagger.AttackAp && w.ToHit < dagger.ToHit && w.Critical < dagger.Critical)),
                "a dagger: fewest AP, best aim and critical chance");
            Assert.IsTrue(oneHanded.Skip(2).All(n => Weapon(n).AttackAp > Weapon("Sword").AttackAp), "a sword: fewer AP than other one-handed weapons");
            Assert.AreEqual(Weapon("Sword").BaseDamage, Weapon("Axe").BaseDamage, "an axe: a sword's damage...");
            Assert.Greater(Weapon("Axe").Critical, Weapon("Sword").Critical, "...with critical hits");
            Assert.IsTrue(oneHanded.All(n => Weapon(n).BaseDamage <= Weapon("Mace").BaseDamage), "a mace: most one-handed damage");
            Assert.IsTrue(melee.All(w => w.BaseDamage <= Weapon("Maul").BaseDamage), "a maul: most damage");
            Assert.IsTrue(melee.All(w => (w.Cleave > 0) == (w.name == "Greatsword")), "only a greatsword cleaves");
            Assert.IsTrue(melee.All(w => (w.Stun > 0) == (w.name == "Mace" || w.name == "Maul")), "a mace and a maul stun");
            Assert.IsTrue(twoHanded.All(n => Weapon(n).AttackAp > oneHanded.Max(o => Weapon(o).AttackAp)), "a one-handed weapon is faster");
            Assert.AreEqual(1d, dagger.StealthCritical, Tight, "a dagger adds 1.0 to a stealth attack's multiplier");

            var pistol = Weapon("Pistol");
            var rifle = Weapon("Rifle");
            Assert.AreEqual(2, 10 / pistol.AttackAp, "a pistol fires twice in a turn of 10 AP");
            Assert.AreEqual(4, 10 - rifle.AttackAp, "a rifle fires once and leaves 4 AP");
            Assert.Less(pistol.RangeCells, rifle.RangeCells);
            Assert.Less(pistol.ToHit, rifle.ToHit, "a pistol aims worse");
            Assert.IsTrue(Weapon("Bite").BleedsOnCritical && Weapon("Tusks").BleedsOnCritical);
            Assert.AreEqual(0.3, Weapon("GlowingBite").PoisonOnHit, Tight);
        }

        [Test]
        public void TheArmourTableHoldsItsValues()
        {
            var table = new Dictionary<string, (double Physical, double Magical)>
            {
                ["Cloth"] = (2, 20), ["Leather"] = (8, 8), ["Chain"] = (16, 3), ["Plate"] = (24, 0), ["Hide"] = (4, 0), ["ThickHide"] = (8, 0),
            };
            foreach (var (name, (physical, magical)) in table)
            {
                var armour = Armour(name);
                Assert.AreEqual(physical, armour.Physical, Tight, name);
                Assert.AreEqual(magical, armour.Magical, Tight, name);
                StringAssert.StartsWith("10.2 §5b", armour.Section, name);
            }
        }

        [Test]
        public void GearThatCannotBeHeldIsRefused()
        {
            var sheet = Sheet(12, 10, 8, 6, 12);
            Assert.Throws<ArgumentException>(() => Armed(sheet, "Dagger", offHand: "Shield"), "a shield goes with a sword, an axe or a mace");
            Assert.Throws<ArgumentException>(() => Armed(sheet, "Greatsword", offHand: "Shield"));
            Assert.Throws<ArgumentException>(() => Armed(sheet, "Pistol"), "a firearm is held only with a Cartridge Pouch");
            Assert.Throws<ArgumentException>(() => Armed(sheet, "Sword", offHand: "CartridgePouch"));
            Assert.Throws<ArgumentException>(() => _rules.Fighter(sheet, 8, new WeaponSet(Weapon("Sword")), second: new WeaponSet(Weapon("Rifle"))));
            Assert.DoesNotThrow(() => Armed(sheet, "Rifle", offHand: "CartridgePouch"));
            Assert.DoesNotThrow(() => Armed(sheet, "Mace", offHand: "Shield"));
            Assert.DoesNotThrow(() => _rules.Fighter(sheet, 8, new WeaponSet(Weapon("Rifle"), OffHand("CartridgePouch")), second: new WeaponSet(Weapon("Sword"))));
        }

        [Test]
        public void AttributeRequirementsScaleAnItemByItsShortfall()
        {
            var maul = Weapon("Maul");
            Assert.AreEqual(0.5, FightRules.Fit(Sheet(4, 1, 1, 1, 1), maul.Requirements, 9), Tight, "half the requirement, half the item");
            Assert.AreEqual(1, FightRules.Fit(Sheet(9, 1, 1, 1, 1), maul.Requirements, 9), Tight, "no more than the whole");
            Assert.AreEqual(0.5, FightRules.Fit(Sheet(8, 1, 1, 1, 1), maul.Requirements, 8), Tight, "stage 8 asks double");
            Assert.AreEqual(6d / 7d, FightRules.Fit(Sheet(6, 1, 1, 1, 1), Weapon("Greatsword").Requirements, 9), Tight,
                "Strength 6 of 7: since the combat rework a weapon asks only its profile attribute");
            var dexterous = new CharacterSheet();
            dexterous.Set(CharacterAttribute.Dexterity, 3);
            Assert.AreEqual(1, FightRules.Fit(dexterous, Weapon("Rifle").Requirements, 9), Tight, "the rifle asks Dexterity 3, Perception no more");

            var weak = Armed(Sheet(4, 10, 10, 10, 10), "Maul");
            var mace = Armed(Sheet(10, 10, 4, 10, 10), "Mace");
            var target = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            Assert.AreEqual(0.5, FightRules.Aim(weak), Tight);
            Assert.AreEqual(0.8, FightRules.Aim(mace), Tight, "four fifths of a mace's Dexterity 5");
            var unaided = Math.Min(0.95, (0.70 + (_rules.Accuracy(weak) - _rules.Evasion(target)) / 100) * 1.01);
            Assert.AreEqual(unaided * 0.5, _rules.HitChance(weak, target), Tight, "a weapon above its user aims worse");

            var plate = Armed(Sheet(4, 10, 10, 10, 10), "Sword", "Plate");
            Assert.AreEqual(24 * 4d / 7d, _rules.PhysicalDefence(plate), Tight, "armour gives the share its wearer meets");
        }

        [Test]
        public void LoadInAFightFollowsItsBands()
        {
            var shielded = Armed(Sheet(15, 10, 6, 6, 10), "Sword", "Plate", 8, "Shield");
            Assert.AreEqual(40.8 / 35, _rules.LoadShare(shielded), Tight, "plate, a sword and a shield: 38 kg, about 41 with the rest");
            Assert.AreEqual(1, _rules.LoadBand(_rules.LoadShare(shielded)), "High");
            Assert.AreEqual((8d, 0), (shielded.LoadEvasion, shielded.LoadAp));
            Assert.AreEqual((6 + 6) / 2d - 8, _rules.Evasion(shielded), Tight);

            var shooter = Rifleman(5, "Plate");
            Assert.AreEqual(3, _rules.LoadBand(_rules.LoadShare(shooter)), "a shooter of Strength 5 in plate is at Critical Overload");
            Assert.AreEqual((24d, 2), (shooter.LoadEvasion, shooter.LoadAp));

            Assert.AreEqual(0, _rules.LoadBand(_rules.LoadShare(ReferenceNine())), "nobody in the reference table is above Normal");
            Assert.AreEqual(0, _rules.LoadBand(_rules.LoadShare(ReferenceEight())));
            Assert.AreEqual(2, _rules.LoadBand(1.4));
        }

        [Test]
        public void ApCarriesItsFractionAndLoadTakesItsShare()
        {
            var nimble = Armed(Sheet(10, 10, 10, 10, 10), "Axe");
            nimble.ApFraction = 0;
            var turns = Enumerable.Range(0, 4).Select(_ => _rules.TurnAp(nimble)).ToArray();
            CollectionAssert.AreEqual(new[] { 10, 11, 10, 11 }, turns, "10 + Dexterity 10 / 20");

            var shooter = Armed(Sheet(5, 6, 10, 6, 3, ranged: 12), "Rifle", "Plate", 8, "CartridgePouch");
            shooter.ApFraction = 0;
            CollectionAssert.AreEqual(new[] { 8, 9 }, new[] { _rules.TurnAp(shooter), _rules.TurnAp(shooter) }, "Critical Overload: −2 AP");

            var entering = ReferenceEight();
            entering.Reserve = 3;
            entering.LostTurns = 1;
            _rules.Enter(entering, Scripted(0.25));
            Assert.AreEqual((0.25, 0, 0), (entering.ApFraction, entering.Reserve, entering.LostTurns), "the first fraction is rolled");
        }

        [Test]
        public void InitiativeIsHalfDexterityAndPerceptionAndAD20()
        {
            var a = ReferenceEight();
            Assert.AreEqual(7 + 1, _rules.Initiative(a, Scripted(0)), Tight);
            Assert.AreEqual(7 + 20, _rules.Initiative(a, Scripted(0.999)), Tight);
        }

        [Test]
        public void HpFollowsEnduranceAndTheLimitMultiplier()
        {
            // 10.2 §5b *Reference fighters of the slice* and *Beasts of the slice*; 07.2 Maximum Health.
            Assert.AreEqual(25, Math.Round(ReferenceNine().MaxHp));
            Assert.AreEqual(32, Math.Round(ReferenceEight().MaxHp));
            Assert.AreEqual(43, Math.Round(Armed(Sheet(20, 16, 12, 10, 20), "Greatsword", "Leather", 7).MaxHp));
            Assert.AreEqual(22, Math.Round(Armed(Sheet(4, 3, 6, 5, 4), "Bite", "Hide").MaxHp), "wolf");
            Assert.AreEqual(26, Math.Round(Armed(Sheet(8, 8, 2, 2, 4), "Tusks", "ThickHide").MaxHp), "boar");
            var glowingWolf = _rules.Fighter(Sheet(9, 8, 11, 10, 9), null, new WeaponSet(Weapon("GlowingBite")), Armour("Hide"), modifiers: new FightModifiers { hp = 1f });
            var glowingBoar = _rules.Fighter(Sheet(10, 10, 4, 4, 6), null, new WeaponSet(Weapon("Tusks")), Armour("ThickHide"), modifiers: new FightModifiers { hp = 0.25f });
            Assert.AreEqual(53, Math.Round(glowingWolf.MaxHp), "glowing wolf: twice a wolf's HP");
            Assert.AreEqual(35, Math.Round(glowingBoar.MaxHp), "glowing boar: a quarter more");
            Assert.AreEqual(glowingWolf.MaxHp, glowingWolf.Hp, "a fighter starts whole");
            Assert.AreEqual(3.52, _rules.LimitMultiplier(0), Tight);
            Assert.Throws<ArgumentOutOfRangeException>(() => _rules.LimitMultiplier(10));
        }

        [Test]
        public void ACharacterSheetCarriesBothWeaponSkills()
        {
            var sheet = Sheet(1, 1, 1, 1, melee: 12, ranged: 3);
            Assert.AreEqual((12, 3), (sheet.Get(CharacterSkill.LightMelee), sheet.Get(CharacterSkill.LightFirearms)));
            var swordsman = Armed(Sheet(6, 6, 6, 6, melee: 12, ranged: 3), "Sword");
            var shooter = Armed(Sheet(6, 6, 6, 6, melee: 12, ranged: 3), "Pistol", offHand: "CartridgePouch");
            Assert.AreEqual((6 + 12) / 2d + 5, _rules.Accuracy(swordsman), Tight, "light melee: (Dexterity + Light Melee) / 2");
            Assert.AreEqual((6 + 3) / 2d - 10, _rules.Accuracy(shooter), Tight, "light firearms: (Dexterity + Light Firearms) / 2");
        }

        [Test]
        public void AccuracyIsTheProfileAttributeAndTheSkillAndNoAttributeAddsDamage()
        {
            // 10.2 §5f: a light weapon aims by Dexterity, a heavy one by Strength, each with its own skill; Perception aims nothing.
            var sheet = Sheet(14, 6, 4, 60, melee: 2, ranged: 3);
            sheet.Set(CharacterSkill.HeavyMelee, 10);
            Assert.AreEqual((14 + 10) / 2d + 5, _rules.Accuracy(Armed(sheet, "Greatsword")), Tight, "heavy melee: (Strength + Heavy Melee) / 2");
            Assert.AreEqual((4 + 2) / 2d + 5, _rules.Accuracy(Armed(sheet, "Sword")), Tight, "light melee: (Dexterity + Light Melee) / 2");
            Assert.AreEqual((4 + 3) / 2d, _rules.Accuracy(Armed(sheet, "Rifle", offHand: "CartridgePouch")), Tight, "Perception 60 aims no shot");
            Assert.IsTrue(Weapon("Greatsword").Heavy && Weapon("Greataxe").Heavy && Weapon("Maul").Heavy, "the heavy melee weapons");
            Assert.IsFalse(Weapon("Rifle").Heavy || Weapon("Pistol").Heavy || Weapon("Sword").Heavy, "the firearms are light");
            Assert.AreEqual(CharacterSkill.HeavyMelee, Weapon("Maul").Skill);
            Assert.AreEqual(CharacterSkill.LightFirearms, Weapon("Rifle").Skill);
            Assert.AreEqual(CharacterAttribute.Dexterity, Weapon("Rifle").ProfileAttribute);

            var strong = Armed(Sheet(90, 6, 6, 6, 6), "Sword");
            var weak = Armed(Sheet(1, 6, 6, 6, 6), "Sword");
            Assert.AreEqual(_rules.BaseDamage(weak), _rules.BaseDamage(strong), Tight, "Strength adds no damage");
            Assert.AreEqual(6.25, _rules.BaseDamage(strong), Tight);
        }

        [Test]
        public void ASaveOfOldSkillNamesKeepsTheTrainedValues()
        {
            // Melee Skill and Ranged Skill were renamed Light Melee and Light Firearms by the combat rework (07.1 §10).
            var state = new CharacterSheetState();
            state.skills.Add(new CharacterValueEntry { name = "MeleeSkill", value = 14 });
            state.skills.Add(new CharacterValueEntry { name = "RangedSkill", value = 9 });
            var sheet = new CharacterSheet();
            sheet.Restore(state);
            Assert.AreEqual((14, 9), (sheet.Get(CharacterSkill.LightMelee), sheet.Get(CharacterSkill.LightFirearms)));
            Assert.AreEqual(1, sheet.Get(CharacterSkill.HeavyMelee), "a skill the save lacks starts at 1");
        }

        private static Random Scripted(params double[] draws) => new ScriptedRolls(draws);
    }
}
