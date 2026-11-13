using System.Collections.Generic;
using System.Linq;
using GameDev.Confront.Grid;
using GameDev.Confront.Rules;
using GameDev.Confront.Turns;
using GameDev.Core.Localization;
using GameDev.Player.Progression;
using GameDev.Slice.EditorTools;
using GameDev.Slice.Fights;
using GameDev.World.Seed;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Slice.Tests
{
    /// <summary>
    /// CR4, abilities in a fight (05.1 *Abilities of the slice*, 07.2 §3.3, §4.5, 10.2 §5f), on a floor of 20 × 20
    /// one-metre cells: Will Power and the Prepared slots by their formulas; the slice's sixteen abilities with their words
    /// and icons; Дрёма, Стой!, Укол разума, Столп, Не сдвинуть, Опора and Сокрушение as 05.1 has them; Infusion paid for
    /// each blow and stopped by plate; the NPCs' Дрёма on the most dangerous and waking a sleeper beside; CR5's four,
    /// Пелена's haze, Гнёт's area, Рывок behind an enemy and Двойник's copy; and CR7's NPCs, preparing by the seed
    /// and using the four and Не сдвинуть by the models' rules. Initiative is set apart by Dexterity and Perception, 100
    /// and 1.
    /// </summary>
    public sealed class AbilityTests
    {
        private const string Data = "Assets/Data/Confront/";

        private FightRules _rules;
        private FightWeapon _pistol;
        private FightWeapon _sword;
        private FightWeapon _fists;
        private FightOffHand _pouch;
        private TurnEconomy _turns;
        private CombatGridReader _grid;
        private readonly List<(Combatant User, Combatant Target, FightAbility Ability, bool Worked)> _used = new List<(Combatant, Combatant, FightAbility, bool)>();
        private readonly List<(Combatant Attacker, Combatant Target, StrikeResult Result)> _struck = new List<(Combatant, Combatant, StrikeResult)>();

        [SetUp]
        public void BuildThePlace()
        {
            _rules = AssetDatabase.LoadAssetAtPath<FightRules>(Data + "FightRules.asset");
            _pistol = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Pistol.asset");
            _sword = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Sword.asset");
            _fists = AssetDatabase.LoadAssetAtPath<FightWeapon>(Data + "Weapons/Fists.asset");
            _pouch = AssetDatabase.LoadAssetAtPath<FightOffHand>(Data + "Weapons/CartridgePouch.asset");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(10f, -0.5f, 10f);
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            _used.Clear();
            _struck.Clear();
        }

        private static FightAbility Ability(string file) => AssetDatabase.LoadAssetAtPath<FightAbility>(Data + "Abilities/" + file + ".asset");

        private static CharacterSheet Sheet(int quick, int mind = 8, int perception = -1)
        {
            var sheet = new CharacterSheet();
            foreach (CharacterAttribute a in System.Enum.GetValues(typeof(CharacterAttribute)))
            {
                sheet.Set(a, 8);
            }

            sheet.Set(CharacterAttribute.Dexterity, quick);
            sheet.Set(CharacterAttribute.Perception, perception < 0 ? quick : perception);
            sheet.Set(CharacterAttribute.Intelligence, mind);
            sheet.Set(CharacterSkill.Arcana, mind);
            sheet.Set(CharacterSkill.LightMelee, 8);
            sheet.Set(CharacterSkill.LightFirearms, 8);
            return sheet;
        }

        private WeaponSet Set(FightWeapon weapon) => weapon.Ranged ? new WeaponSet(weapon, _pouch) : new WeaponSet(weapon);

        private Fighter Fighter(int quick, FightWeapon weapon, int limit = 9, int mind = 8, FightArmour armour = null, int perception = -1) =>
            _rules.Fighter(Sheet(quick, mind, perception), limit, Set(weapon), armour);

        private Fighter Caster(Arcana arcana, int limit, FightWeapon weapon, params FightAbility[] abilities)
        {
            var f = Fighter(100, weapon, limit);
            f.Prepare(arcana, abilities, 10);
            return f;
        }

        private static Combatant At(int id, int side, Fighter fighter, int x, int z, bool intents = false) =>
            new Combatant(id, side, fighter, new Vector3(x + 0.5f, 0f, z + 0.5f), takesIntents: intents);

        private void Begin(Random rolls, params Combatant[] combatants)
        {
            Physics.SyncTransforms();
            var grid = new CombatGridReader(1f);
            Assert.IsTrue(grid.Follow(new Vector3(5.5f, 0.05f, 5.5f)), "the grid forms");
            _grid = grid;
            _turns = new TurnEconomy(_rules, grid);
            _turns.AbilityUsed += (u, t, a, worked, r) => _used.Add((u, t, a, worked));
            _turns.Struck += (a, t, r) => _struck.Add((a, t, r));
            _turns.BeginEncounter(combatants, rolls);
        }

        [Test]
        public void WillPowerAndPreparedSlotsFollowTheirFormulas()
        {
            var uninitiated = _rules.Fighter(Sheet(8), 9, Set(_pistol));
            Assert.AreEqual(0d, uninitiated.MaxWillPower, 1e-9, "only the initiated have Will Power (COMBAT-7)");
            Assert.AreEqual(0d, uninitiated.WillPower, 1e-9);
            Assert.IsFalse(FightRules.CanPay(uninitiated, 1d, true), "the Uninitiated pays nothing: it has entered no Arcana");

            var nine = _rules.Fighter(Sheet(8), 9, Set(_pistol));
            nine.Prepare(Arcana.Priestess, null, 0);
            Assert.AreEqual(20d + (0.8d * 8d), nine.MaxWillPower, 1e-5, "07.2 §3.3: (20 + 0.8 × Will) × S");
            Assert.AreEqual(nine.MaxWillPower, nine.WillPower, 1e-9, "one entering an Arcana comes into all of it");
            var eight = _rules.Fighter(Sheet(8), 8, Set(_pistol));
            eight.Prepare(Arcana.Priestess, null, 0);
            Assert.AreEqual(nine.MaxWillPower * _rules.LimitMultiplier(8), eight.MaxWillPower, 1e-5, "the same S as HP");
            eight.WillPower = 5d;
            eight.Prepare(Arcana.Priestess, null, 0);
            Assert.AreEqual(5d, eight.WillPower, 1e-9, "preparing again gives none back");

            foreach (var (intelligence, will, slots) in new[] { (1, 1, 3), (8, 8, 4), (15, 15, 5), (50, 50, 10), (100, 100, 10) })
            {
                var sheet = Sheet(8, intelligence);
                sheet.Set(CharacterAttribute.Will, will);
                Assert.AreEqual(slots, _rules.PreparedSlots(sheet), $"07.2 §4.5: Intelligence {intelligence} and Will {will}");
            }
        }

        [Test]
        public void PrepareKeepsEveryPassiveAndAsManyActivesAsTheSlotsHold()
        {
            var f = Fighter(8, _pistol);
            f.Prepare(Arcana.Emperor, new[] { Ability("Kinship"), Ability("Unmoved"), Ability("Halt"), Ability("Support"), Ability("Pillar"), Ability("Halt") }, 2);
            Assert.AreEqual(Arcana.Emperor, f.Arcana);
            CollectionAssert.AreEqual(new[] { Ability("Unmoved"), Ability("Halt") }, f.Prepared.ToArray(), "two slots, in order, no twice");
            CollectionAssert.AreEqual(new[] { Ability("Kinship"), Ability("Support") }, f.Passives.ToArray(), "the passives take no slot");
            Assert.AreEqual(Ability("Support"), f.Knows(AbilityEffect.Support));
            Assert.IsNull(f.Knows(AbilityEffect.Pillar), "an ability left out is not known in the fight");
        }

        [Test]
        public void TheSliceHoldsItsSixteenAbilitiesWithTheirWordsAndIcons()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath);
            var all = settings.Abilities;
            Assert.AreEqual(16, all.Length);
            Assert.IsTrue(all.All(a => a != null && a.Icon != null), "each with its icon");
            Assert.AreEqual(16, all.Select(a => a.Key).Distinct().Count(), "and its own key");
            Assert.AreEqual(8, all.Count(a => a.Arcana == Arcana.Priestess));
            Assert.AreEqual(8, all.Count(a => a.Arcana == Arcana.Emperor));
            foreach (var a in all)
            {
                foreach (var key in new[] { $"ability.{a.Key}.name", $"ability.{a.Key}.text" })
                {
                    Assert.IsTrue(Loc.Tables.Has(key, Loc.English) && Loc.Tables.Has(key, Loc.Russian), $"'{key}' has its Russian and English text");
                }
            }

            var drowse = Ability("Drowse");
            Assert.AreEqual((AbilityEffect.Sleep, 0.33f, 2, 6, 0.4f, 1), (drowse.Effect, drowse.Cost, drowse.Ap, drowse.Range, drowse.Contest, drowse.Rounds), "05.1's numbers");
            Assert.AreEqual(6f, Ability("MindSting").BaseDamage, 1e-6f, "the balance after the answers");
            Assert.AreEqual(8, Ability("Halt").Range, "Стой! within 8 cells");
            Assert.AreEqual(2, Ability("Screen").Rounds, "Пелена for 2 rounds");
            Assert.AreEqual(4, Ability("Double").Ap, "Двойник for 4 AP");
            Assert.AreEqual(2f, Ability("Press").BaseDamage, 1e-6f, "Гнёт base 2");
            Assert.AreEqual(3d, _rules.SupportDefence, 1e-9, "Опора +3");
            Assert.AreEqual(1.5f, Ability("Crush").Multiplier, 1e-6f);
            Assert.AreEqual(0.5f, AssetDatabase.LoadAssetAtPath<FightArmour>(Data + "Armour/Chain.asset").Channel, 1e-6f, "chain halves Infusion");
            Assert.AreEqual(0f, AssetDatabase.LoadAssetAtPath<FightArmour>(Data + "Armour/Plate.asset").Channel, 1e-6f, "plate stops it");
            Assert.AreEqual(1f, AssetDatabase.LoadAssetAtPath<FightArmour>(Data + "Armour/Leather.asset").Channel, 1e-6f);
        }

        [Test]
        public void DrowseSleepsAPersonUntilDamageWakesIt()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Drowse")), 5, 5, intents: true);
            var target = At(1, 1, Fighter(1, _fists), 9, 5, intents: true);
            Begin(new ScriptedAlways(0d), caster, target);
            target.Fighter.Hp = 1000d;
            var ap = _turns.ApLeft;
            var wp = caster.Fighter.WillPower;
            Assert.IsTrue(_turns.CanAim(caster, 0, target), "a person within 6 cells, in sight");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, target.Id)));
            Assert.AreEqual(1, target.Fighter.Asleep, "it sleeps");
            Assert.AreEqual(ap - 2, _turns.ApLeft, "for 2 AP");
            Assert.AreEqual(wp - (0.33d * caster.Fighter.MaxWillPower), caster.Fighter.WillPower, 1e-6, "and a third of maximum Will Power");
            Assert.AreEqual(0d, _rules.Evasion(target.Fighter), 1e-9, "a sleeper evades nothing");
            Assert.IsTrue(_used.Exists(u => u.Ability.Effect == AbilityEffect.Sleep && u.Worked));

            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)), "a shot");
            Assert.Greater(_struck.Last().Result.Damage, 0d);
            Assert.AreEqual(0, target.Fighter.Asleep, "damage wakes it");

            BuildThePlace();
            var other = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Drowse")), 5, 5, intents: true);
            var beast = At(1, 1, Fighter(1, _fists), 7, 5, intents: true);
            beast.Beast = true;
            Begin(new ScriptedAlways(0d), other, beast);
            Assert.IsFalse(_turns.CanAim(other, 0, beast), "Дрёма is for people");
        }

        [Test]
        public void AFailedAbilityLeavesItsUserOpen()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Drowse")), 5, 5, intents: true);
            var target = At(1, 1, Fighter(1, _pistol), 9, 5, intents: true);
            Begin(new ScriptedAlways(0.99d), caster, target);
            var closed = _turns.ContextFor(target, target.Cell, caster).HitBonus;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, target.Id)));
            Assert.AreEqual(0, target.Fighter.Asleep, "it resisted");
            Assert.IsTrue(caster.Fighter.Exposed, "the caster is open");
            Assert.IsTrue(_used.Exists(u => u.Ability.Effect == AbilityEffect.Sleep && !u.Worked));
            var open = _turns.ContextFor(target, target.Cell, caster).HitBonus;
            Assert.AreEqual(closed + _rules.ExposedHit, open, 1e-9, "the next attack on it aims 10 points better");
        }

        [Test]
        public void HaltTakesTheTargetsNextTurn()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Halt")), 5, 5, intents: true);
            var target = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
            Begin(new ScriptedAlways(0d), caster, target);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, target.Id)));
            Assert.AreEqual(1, target.Fighter.LostTurns, "it loses its next turn");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(caster, _turns.Acting, "its turn passed it by");
            Assert.AreEqual(0, target.Fighter.LostTurns);
        }

        [Test]
        public void MindStingStrikesThroughMagicalDefenceAndDisorients()
        {
            var caster = At(0, 0, _rules.Fighter(Sheet(100, 100), 7, Set(_pistol)), 5, 5, intents: true);
            caster.Fighter.Prepare(Arcana.Priestess, new[] { Ability("MindSting") }, 10);
            var target = At(1, 1, Fighter(1, _fists), 15, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, target);
            target.Fighter.Hp = 1000d;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, target.Id)), "a target 10 cells off, in sight");
            Assert.AreEqual(1000d - 6d, target.Fighter.Hp, 1e-9, "base 6, its roll in the middle, through no magical defence");
            Assert.AreEqual(1, target.Fighter.Disoriented, "disoriented for a round");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(target, _turns.Acting);
            Assert.IsTrue(target.Fighter.Dazed, "its turn aims worse");

            BuildThePlace();
            var weak = At(0, 0, _rules.Fighter(Sheet(100, 1), 7, Set(_pistol)), 5, 5, intents: true);
            weak.Fighter.Prepare(Arcana.Priestess, new[] { Ability("MindSting") }, 10);
            var other = At(1, 1, Fighter(1, _fists), 15, 5, intents: true);
            Begin(new ScriptedAlways(0.99d), weak, other);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, other.Id)));
            Assert.AreEqual(1, weak.Fighter.Disoriented, "a miss disorients the caster");
        }

        [Test]
        public void InfusionPaysForEachShotAndAddsMagicPlateStopsIt()
        {
            var damage = new double[2];
            for (var on = 0; on < 2; on++)
            {
                BuildThePlace();
                var shooter = At(0, 0, Fighter(100, _pistol, perception: 1), 5, 5, intents: true);
                shooter.Fighter.Prepare(Arcana.Priestess, new FightAbility[0], 0);
                shooter.Infusing = on == 1;
                var target = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
                Begin(new ScriptedAlways(0.5d), shooter, target);
                target.Fighter.Hp = 1000d;
                var wp = shooter.Fighter.WillPower;
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)));
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(target.Id)));
                damage[on] = 1000d - target.Fighter.Hp;
                Assert.AreEqual(wp - (on * 2 * _rules.InfusionCost(shooter.Fighter)), shooter.Fighter.WillPower, 1e-9, "5 × S for each shot, never once a turn");
            }

            Assert.AreEqual(2d * 0.40d * _rules.WeaponBase(Fighter(100, _pistol)), damage[1] - damage[0], 1e-5, "0.40 of the weapon's base on each shot");

            // Loaded after the new scene, which unloads an asset nothing in it holds.
            BuildThePlace();
            var plate = AssetDatabase.LoadAssetAtPath<FightArmour>(Data + "Armour/Plate.asset");
            var knight = At(0, 0, Fighter(100, _pistol, armour: plate, perception: 1), 5, 5, intents: true);
            knight.Fighter.Prepare(Arcana.Priestess, new FightAbility[0], 0);
            knight.Infusing = true;
            var foe = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), knight, foe);
            var kept = knight.Fighter.WillPower;
            Assert.AreEqual(0d, _rules.InfusionShare(knight.Fighter), 1e-9, "which lets no Infusion through");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(foe.Id)));
            Assert.AreEqual(kept, knight.Fighter.WillPower, 1e-9, "plate stops Infusion, so nothing is poured");

            BuildThePlace();
            var plain = At(0, 0, Fighter(100, _pistol, perception: 1), 5, 5, intents: true);
            plain.Infusing = true;
            var mark = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), plain, mark);
            var full = plain.Fighter.WillPower;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(mark.Id)));
            Assert.AreEqual(full, plain.Fighter.WillPower, 1e-9, "the Uninitiated pours nothing");
        }

        [Test]
        public void UnmovedEndsTheTurnAndLowersPhysicalDamageUntilTheNext()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 9, _pistol, Ability("Unmoved")), 5, 5, intents: true);
            var enemy = At(1, 1, Fighter(1, _pistol), 9, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, enemy);
            var open = _turns.ForecastFor(enemy, caster, enemy.Cell, false).Low;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0)));
            Assert.IsTrue(caster.Fighter.Braced);
            Assert.AreEqual(enemy, _turns.Acting, "it ends the turn");
            Assert.AreEqual(open * 0.75d, _turns.ForecastFor(enemy, caster, enemy.Cell, false).Low, 1e-9, "physical damage 25 % lower");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(caster, _turns.Acting);
            Assert.IsFalse(caster.Fighter.Braced, "until its next turn");
        }

        [Test]
        public void SupportLendsDefenceToAlliesWithinTwoCells()
        {
            var low = new double[2];
            for (var near = 0; near < 2; near++)
            {
                BuildThePlace();
                var enemy = At(0, 1, Fighter(100, _pistol), 5, 5, intents: true);
                var target = At(1, 0, Fighter(1, _fists), 9, 5, intents: true);
                var ally = At(2, 0, Caster(Arcana.Emperor, 8, _fists, Ability("Support")), 9, near == 1 ? 7 : 10, intents: true);
                ally.Fighter.Hp = ally.Fighter.MaxHp;
                Begin(new ScriptedAlways(0.5d), enemy, target, ally);
                low[near] = _turns.ForecastFor(enemy, target, enemy.Cell, false).Low;
            }

            Assert.Less(low[1], low[0], "an ally knowing Опора 2 cells off lowers the damage");
        }

        [Test]
        public void CrushStrikesHalfAgainAsHardAndCostsItsWeaponsApAndOne()
        {
            var damage = new double[2];
            for (var crush = 0; crush < 2; crush++)
            {
                BuildThePlace();
                var caster = At(0, 0, Caster(Arcana.Emperor, 7, _sword, Ability("Crush")), 5, 5, intents: true);
                var target = At(1, 1, Fighter(1, _fists), 6, 5, intents: true);
                Begin(new ScriptedAlways(0.5d), caster, target);
                target.Fighter.Hp = 1000d;
                var ap = _turns.ApLeft;
                Assert.IsTrue(_turns.SubmitIntent(crush == 1 ? TurnIntent.UseAbility(0, target.Id) : TurnIntent.Attack(target.Id)));
                Assert.AreEqual(ap - _sword.AttackAp - crush, _turns.ApLeft, crush == 1 ? "the sword's AP and one" : "the sword's AP");
                damage[crush] = 1000d - target.Fighter.Hp;
            }

            Assert.Greater(damage[0], 0d);
            Assert.AreEqual(damage[0] * 1.5d, damage[1], 1e-9, "×1.5");
        }

        [Test]
        public void PillarHoldsEveryEnemyBesideWhoLosesTheContest()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 7, _fists, Ability("Pillar")), 5, 5, intents: true);
            var east = At(1, 1, Fighter(1, _fists), 6, 5);
            var north = At(2, 1, Fighter(1, _fists), 5, 6);
            Begin(new ScriptedAlways(0d), caster, east, north);
            Assert.IsTrue(_turns.CanUse(caster, 0), "with enemies beside");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0)));
            Assert.AreEqual(caster, east.TauntedBy);
            Assert.AreEqual(caster, north.TauntedBy);

            BuildThePlace();
            var alone = At(0, 0, Caster(Arcana.Emperor, 7, _fists, Ability("Pillar")), 5, 5, intents: true);
            var far = At(1, 1, Fighter(1, _fists), 9, 5);
            Begin(new ScriptedAlways(0d), alone, far);
            Assert.IsFalse(_turns.CanUse(alone, 0), "with nobody beside there is nothing to hold");
        }

        [Test]
        public void AnNpcPriestessSleepsTheMostDangerousOfTwoAwakeEnemies()
        {
            var priestess = At(0, 1, Caster(Arcana.Priestess, 8, _pistol, Ability("Drowse")), 5, 5);
            var strong = At(1, 0, Fighter(100, _pistol, perception: 1), 9, 5, intents: true);
            var weak = At(2, 0, Fighter(1, _fists), 9, 7, intents: true);
            Begin(new ScriptedAlways(0d), priestess, strong, weak);
            strong.Fighter.Hp = weak.Fighter.Hp = 1000d;
            Assert.AreEqual(priestess, _turns.Acting);
            _turns.PlayTurn();
            var sleep = _used.Find(u => u.Ability.Effect == AbilityEffect.Sleep);
            Assert.IsNotNull(sleep.Ability, "it opens its turn with Дрёма");
            Assert.AreEqual(strong, sleep.Target, "on the quick shooter, the more dangerous");
        }

        [Test]
        public void AnAllyBesideWakesASleeper()
        {
            var waker = At(0, 0, Fighter(100, _pistol), 5, 5);
            var sleeper = At(1, 0, Fighter(50, _pistol), 6, 5);
            var enemy = At(2, 1, Fighter(1, _fists), 5, 12, intents: true);
            Begin(new ScriptedAlways(0.5d), waker, sleeper, enemy);
            sleeper.Fighter.Asleep = 1;
            Assert.AreEqual(waker, _turns.Acting);
            _turns.PlayTurn();
            Assert.AreEqual(0, sleeper.Fighter.Asleep, "it wakes its neighbour for 6 AP");
        }

        private CombatCell Cell(int x, int z)
        {
            Assert.IsTrue(_grid.TryCellAt(new Vector3(x + 0.5f, 0.05f, z + 0.5f), out var cell), $"a cell at {x}, {z}");
            return cell;
        }

        private void EndRounds(int rounds)
        {
            for (var i = 0; i < rounds * 2; i++)
            {
                Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            }
        }

        [Test]
        public void ScreenObscuresAShotThroughOrIntoItAndFadesAfterItsRounds()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Screen")), 5, 5, intents: true);
            var enemy = At(1, 1, Fighter(1, _fists), 12, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, enemy);
            Assert.IsFalse(_grid.Shot(caster.Cell, enemy.Cell).Obscured, "an open floor");
            Assert.IsTrue(TurnEconomy.NeedsPoint(Ability("Screen")));
            Assert.IsFalse(_turns.CanAimAt(caster, 0, Cell(14, 5)), "9 cells off, past its 8");
            Assert.IsTrue(_turns.CanAimAt(caster, 0, Cell(9, 5)));
            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbilityAt(0, Cell(9, 5))));
            Assert.AreEqual(ap - 3, _turns.ApLeft, "for 3 AP");
            Assert.AreEqual(1, _turns.Clouds.Count);
            Assert.IsTrue(_grid.Shot(caster.Cell, enemy.Cell).Obscured, "a shot through the haze aims worse");
            Assert.IsTrue(_grid.Shot(enemy.Cell, caster.Cell).Obscured, "both ways");
            Assert.IsFalse(_grid.Shot(Cell(5, 9), Cell(12, 9)).Obscured, "a line beside it does not meet it");

            EndRounds(1);
            Assert.AreEqual(1, _turns.Clouds.Count, "one round ended, one left");
            EndRounds(1);
            Assert.AreEqual(0, _turns.Clouds.Count, "gone after its two");
            Assert.IsFalse(_grid.Shot(caster.Cell, enemy.Cell).Obscured, "and the line is open again");

            BuildThePlace();
            var inside = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Screen")), 5, 5, intents: true);
            var foe = At(1, 1, Fighter(1, _fists), 12, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), inside, foe);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbilityAt(0, inside.Cell)), "on its own cell");
            Assert.IsFalse(_grid.Shot(inside.Cell, foe.Cell).Obscured, "a shot from inside the haze keeps its aim, as from a bush (COMBAT-12 (d))");
            Assert.IsTrue(_grid.Shot(foe.Cell, inside.Cell).Obscured, "one into it aims worse");
        }

        [Test]
        public void PressStrikesEveryoneInItsAreaButItsCasterWithNoRoll()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Press")), 5, 5, intents: true);
            var east = At(1, 1, Fighter(1, _fists), 7, 5, intents: true);
            var corner = At(2, 1, Fighter(1, _fists), 7, 6, intents: true);
            var ally = At(3, 0, Fighter(1, _fists), 6, 6, intents: true);
            var far = At(4, 1, Fighter(1, _fists), 9, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, east, corner, ally, far);
            foreach (var c in new[] { caster, east, corner, ally, far })
            {
                c.Fighter.Hp = 1000d;
            }

            var press = Ability("Press");
            Assert.AreEqual((2, 1), _turns.PressCount(caster, press, Cell(6, 5)), "two enemies and one of its own in the 3 × 3");
            var ap = _turns.ApLeft;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbilityAt(0, Cell(6, 5))));
            Assert.AreEqual(ap - 5, _turns.ApLeft, "for 5 AP");
            Assert.AreEqual(998d, east.Fighter.Hp, 1e-9, "base 2, its roll in the middle, through no magical defence");
            Assert.AreEqual(998d, corner.Fighter.Hp, 1e-9);
            Assert.AreEqual(998d, ally.Fighter.Hp, 1e-9, "its own people too");
            Assert.AreEqual(1000d, far.Fighter.Hp, 1e-9, "nobody outside the area");
            Assert.AreEqual(1000d, caster.Fighter.Hp, 1e-9, "and never its caster, who stands in it");
            Assert.AreEqual(3, _used.Count(u => u.Ability == press && u.Target != null && u.Worked), "each struck told");
            Assert.IsTrue(_used.Exists(u => u.Ability == press && u.Target == null && u.Worked), "then the cast");
        }

        [Test]
        public void DashLandsBehindTheEnemyWithNoReactionAttack()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Dash")), 7, 5, intents: true);
            var swordsman = At(1, 1, Fighter(1, _sword), 8, 5, intents: true);
            Begin(new ScriptedAlways(0d), caster, swordsman);
            Assert.IsTrue(_turns.CanAim(caster, 0, swordsman));
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0, swordsman.Id)));
            Assert.AreEqual(Cell(9, 5), caster.Cell, "behind it");
            Assert.AreEqual(caster.Id, _grid.OccupantAt(Cell(9, 5)));
            Assert.AreEqual(-1, _grid.OccupantAt(Cell(7, 5)), "the cell it left is free");
            Assert.AreEqual(new Vector2Int(-1, 0), caster.Facing, "facing the enemy");
            Assert.AreEqual(0, _struck.Count, "no walk, so no Reaction Attack");

            BuildThePlace();
            var lame = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Dash")), 5, 5, intents: true);
            var foe = At(1, 1, Fighter(1, _fists), 8, 5, intents: true);
            Begin(new ScriptedAlways(0d), lame, foe);
            lame.Fighter.LegBroken = true;
            Assert.IsFalse(_turns.CanUse(lame, 0), "a broken leg takes no step");
        }

        [Test]
        public void DoubleDrawsTheEnemyUntilAHitBreaksItAndDamageShowsItsCaster()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Double")), 5, 5, intents: true);
            var enemy = At(1, 1, Fighter(1, _pistol), 9, 5);
            var ended = new List<bool>();
            Begin(new ScriptedAlways(0d), caster, enemy);
            _turns.DoubleEnded += (c, d, broke) => ended.Add(broke);
            enemy.Fighter.Hp = 1000d;
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0)));
            var copy = caster.Double;
            Assert.IsNotNull(copy, "a copy where it stood");
            Assert.AreEqual(caster, copy.DoubleOf);
            Assert.AreEqual(1, _turns.Doubles.Count);
            Assert.Greater(caster.Fighter.Veiled, 0, "and its caster unseen");
            Assert.IsFalse(_turns.CanUse(caster, 0), "one copy at a time");

            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.MoveTo(Cell(5, 8))));
            Assert.AreEqual(copy.Id, _grid.OccupantAt(Cell(5, 5)), "the copy holds the cell its caster left");
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(enemy, _turns.Acting);
            _turns.PlayTurn();
            Assert.IsTrue(_struck.Count > 0 && _struck.All(s => s.Target != caster), "the enemy shoots the copy, never its caster");
            Assert.AreEqual(copy, _struck[0].Target);
            Assert.IsNull(caster.Double, "the hit broke it");
            Assert.AreEqual(0, _turns.Doubles.Count);
            CollectionAssert.AreEqual(new[] { true }, ended);
            Assert.AreEqual(-1, _grid.OccupantAt(Cell(5, 5)), "and freed its cell");
            Assert.Greater(caster.Fighter.Veiled, 0, "its caster is still unseen to the end of its next turn");

            Assert.AreEqual(caster, _turns.Acting);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.Attack(enemy.Id)));
            Assert.Greater(1000d - enemy.Fighter.Hp, 0d);
            Assert.AreEqual(0, caster.Fighter.Veiled, "damage it deals shows it");
        }

        [Test]
        public void ADoublesVeilEndsAtItsCastersNextTurnsEndAndTakesTheCopy()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Double")), 5, 5, intents: true);
            var enemy = At(1, 1, Fighter(1, _fists), 15, 5, intents: true);
            var ended = new List<bool>();
            Begin(new ScriptedAlways(0d), caster, enemy);
            _turns.DoubleEnded += (c, d, broke) => ended.Add(broke);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.UseAbility(0)));
            EndRounds(1);
            Assert.Greater(caster.Fighter.Veiled, 0, "the turn it cast in has ended");
            Assert.IsNotNull(caster.Double);
            Assert.IsTrue(_turns.SubmitIntent(TurnIntent.EndTurn));
            Assert.AreEqual(0, caster.Fighter.Veiled, "seen again as its next turn ends");
            Assert.IsNull(caster.Double, "its copy gone with the veil");
            CollectionAssert.AreEqual(new[] { false }, ended);
        }

        [Test]
        public void AnNpcPreparesByTheSeedFromEveryActiveAbilityOfItsLimits()
        {
            var all = AssetDatabase.LoadAssetAtPath<SliceSettings>(SliceConfrontSetup.SettingsPath).Abilities;
            var seed = new WorldSeed(110201);
            var eight = SliceConfront.Known(all, Arcana.Priestess, 8, "Warden#1", seed);
            Assert.AreEqual(Ability("KeenEar"), eight[0], "its passive first");
            CollectionAssert.AreEquivalent(new[] { Ability("Screen"), Ability("Double"), Ability("Drowse") }, eight.Skip(1).ToArray(),
                "then every active one of Limits 9 and 8 a fight can use, Пелена and Двойник too (COMBAT-8 (c))");
            CollectionAssert.AreEqual(eight, SliceConfront.Known(all, Arcana.Priestess, 8, "Warden#1", seed), "the same person, the same order");
            var emperor = SliceConfront.Known(all, Arcana.Emperor, 8, "Warden#2", seed);
            CollectionAssert.IsSubsetOf(new[] { Ability("Dash"), Ability("Press") }, emperor, "Рывок and Гнёт for Император");

            // Six actives at Limit 7 of Император and four slots: the seed decides which the slots miss.
            var slots = _rules.PreparedSlots(Sheet(8));
            Assert.AreEqual(4, slots);
            int with = 0, without = 0;
            for (var i = 0; i < 40; i++)
            {
                var f = Fighter(8, _pistol, 7);
                f.Prepare(Arcana.Emperor, SliceConfront.Known(all, Arcana.Emperor, 7, $"Cultist#{i}", seed), slots);
                Assert.AreEqual(slots, f.Prepared.Count);
                if (f.Prepared.Contains(Ability("Pillar")))
                {
                    with++;
                }
                else
                {
                    without++;
                }
            }

            Assert.Greater(with, 0, "some prepare Столп");
            Assert.Greater(without, 0, "and some miss it");
        }

        [Test]
        public void AnNpcPressesWhereItStrikesEnoughEnemies()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Press")), 5, 5);
            var a = At(1, 1, Fighter(1, _pistol), 10, 5, intents: true);
            var b = At(2, 1, Fighter(1, _pistol), 10, 6, intents: true);
            var c = At(3, 1, Fighter(1, _pistol), 11, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, a, b, c);
            a.Fighter.Hp = b.Fighter.Hp = c.Fighter.Hp = 1000d;
            Assert.AreEqual(caster, _turns.Acting);
            _turns.PlayTurn();
            Assert.IsTrue(_used.Exists(u => u.Ability.Effect == AbilityEffect.Press && u.Target == null && u.Worked), "it opens its turn with Гнёт");
            Assert.AreEqual(3, _used.Count(u => u.Ability.Effect == AbilityEffect.Press && u.Target != null), "over all three");

            BuildThePlace();
            var other = At(0, 0, Caster(Arcana.Emperor, 8, _pistol, Ability("Press")), 5, 5);
            var near = At(1, 1, Fighter(1, _pistol), 10, 5, intents: true);
            var apart = At(2, 1, Fighter(1, _pistol), 10, 12, intents: true);
            Begin(new ScriptedAlways(0.5d), other, near, apart);
            near.Fighter.Hp = apart.Fighter.Hp = 1000d;
            _turns.PlayTurn();
            Assert.IsFalse(_used.Exists(u => u.Ability.Effect == AbilityEffect.Press), "one enemy in reach of an area is not worth it");
        }

        [Test]
        public void AnNpcRaisesAScreenFromTheApLeftWhenAShooterCanFireAtIt()
        {
            var caster = At(0, 0, Caster(Arcana.Priestess, 9, _pistol, Ability("Screen")), 5, 5);
            var shooter = At(1, 1, Fighter(1, _pistol), 9, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, shooter);
            shooter.Fighter.Hp = 1000d;
            Assert.AreEqual(caster, _turns.Acting);
            _turns.PlayTurn();
            Assert.AreEqual(3, _struck.Count(s => s.Attacker == caster), "three shots of its 15 AP");
            Assert.AreEqual(1, _turns.Clouds.Count, "then Пелена for the 3 AP left");
            Assert.AreEqual(caster.Cell, _turns.Clouds[0].Center, "over itself");
            Assert.AreEqual(shooter, _turns.Acting, "and its turn is over");
        }

        [Test]
        public void AWoundedNpcKeepsItsApForADouble()
        {
            var shots = new int[2];
            for (var wounded = 0; wounded < 2; wounded++)
            {
                BuildThePlace();
                var caster = At(0, 0, Caster(Arcana.Priestess, 8, _pistol, Ability("Double")), 5, 5);
                var shooter = At(1, 1, Fighter(1, _pistol), 9, 5, intents: true);
                Begin(new ScriptedAlways(0.5d), caster, shooter);
                shooter.Fighter.Hp = 1000d;
                caster.Fighter.Hp = wounded == 1 ? 0.4d * caster.Fighter.MaxHp : caster.Fighter.MaxHp;
                _turns.PlayTurn();
                shots[wounded] = _struck.Count(s => s.Attacker == caster);
                if (wounded == 1)
                {
                    Assert.IsNotNull(caster.Double, "wounded, it leaves a copy");
                    Assert.Greater(caster.Fighter.Veiled, 0);
                }
                else
                {
                    Assert.IsNull(caster.Double, "whole, three shots leave 3 AP, short of Двойник's 4");
                }
            }

            CollectionAssert.AreEqual(new[] { 3, 2 }, shots, "the wounded one keeps 4 AP back from its turn's start");
        }

        [Test]
        public void AnNpcBracesWheneverAnEnemyCouldAttackItNext()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 9, _pistol, Ability("Unmoved")), 5, 5);
            var shooter = At(1, 1, Fighter(1, _pistol), 9, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, shooter);
            shooter.Fighter.Hp = 1000d;
            _turns.PlayTurn();
            Assert.IsTrue(caster.Fighter.Braced, "one enemy 4 cells off is enough since the balance after COMBAT-7");
            Assert.IsTrue(_used.Exists(u => u.Ability.Effect == AbilityEffect.Brace));
        }

        [Test]
        public void AnNpcDashesBehindAFarTargetAndStrikes()
        {
            var caster = At(0, 0, Caster(Arcana.Emperor, 9, _sword, Ability("Dash")), 5, 5);
            var target = At(1, 1, Fighter(1, _pistol), 10, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), caster, target);
            target.Fighter.Hp = 1000d;
            _turns.PlayTurn();
            Assert.IsTrue(_used.Exists(u => u.Ability.Effect == AbilityEffect.Dash), "a walk of 4 cells is worth Рывок");
            Assert.AreEqual(Cell(11, 5), caster.Cell, "behind its target");
            Assert.Greater(_struck.Count(s => s.Attacker == caster && s.Target == target), 0, "and it strikes");

            BuildThePlace();
            var other = At(0, 0, Caster(Arcana.Emperor, 9, _sword, Ability("Dash")), 5, 5);
            var close = At(1, 1, Fighter(1, _pistol), 8, 5, intents: true);
            Begin(new ScriptedAlways(0.5d), other, close);
            close.Fighter.Hp = 1000d;
            _turns.PlayTurn();
            Assert.IsFalse(_used.Exists(u => u.Ability.Effect == AbilityEffect.Dash), "a walk of 2 cells is not");
        }

        /// <summary>Every draw the same value.</summary>
        private sealed class ScriptedAlways : Random
        {
            private readonly double _value;

            public ScriptedAlways(double value) => _value = value;

            public override double NextDouble() => _value;

            protected override double Sample() => _value;

            public override int Next(int maxValue) => (int)(_value * maxValue);

            public override int Next(int minValue, int maxValue) => minValue + (int)(_value * (maxValue - minValue));
        }
    }
}
