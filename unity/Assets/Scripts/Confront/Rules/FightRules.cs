using System;
using System.Collections.Generic;
using GameDev.Player.Progression;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// The fight's numbers, Combat Numbers V0 of 10.2 §5b with the combat rework of §5f, as one service: a fighter's
    /// values from its character sheet and gear, the hit, the critical hit, damage and armour, blocks and parries, stun
    /// and cleave, Load in a Fight and Attribute Requirements. The formulas follow `tools/balance/combat_sim.py` and `tactical_sim.py`; every number
    /// is an authoring default of 10.2 and lives in this asset. The asset holds no fight's state: that is the
    /// <see cref="Fighter"/>'s. A fight's rolls come from the slice seed's `slice.combat` stream forked with the
    /// fight's number.
    /// </summary>
    [CreateAssetMenu(menuName = "GameDev/Confront/Fight Rules", fileName = "FightRules")]
    public sealed partial class FightRules : ScriptableObject
    {
        [Tooltip("07.1 §7: the critical chance by Perception and its cap, shared with every action check.")]
        [SerializeField] private ActionCheckRules checks;

        [Header("07.2 Maximum Health")]
        [Tooltip("07.2: the universal Limit multiplier S, Limits 0 to 9; an Uninitiated character or a beast uses Limit 9's.")]
        [SerializeField] private float[] limitMultiplier = { 3.52f, 3.06f, 2.66f, 2.31f, 2.01f, 1.75f, 1.52f, 1.32f, 1.15f, 1.00f };

        [Tooltip("07.2: HP = (hpBase + hpPerEndurance × Endurance) × S.")]
        [SerializeField] private float hpBase = 20f;

        [SerializeField] private float hpPerEndurance = 0.8f;

        [Header("10.2 §5b Attack")]
        [Tooltip("The hit chance of peers, before Luck.")]
        [SerializeField, Range(0f, 1f)] private float baseHit = 0.70f;

        [SerializeField, Range(0f, 1f)] private float hitFloor = 0.05f;

        [SerializeField, Range(0f, 1f)] private float hitCeiling = 0.95f;

        [Tooltip("07.1 §10, S5b.3: Perception as every attack's secondary attribute by 07.1 §5, the target's Evasion the difficulty: the hit chance × (1 + share × min(1, Perception / Evasion)) before Luck; 0 turns it off.")]
        [SerializeField, Range(0f, 0.5f)] private float perceptionSecondary = 0.10f;

        [Header("10.2 §5b Damage")]
        [Tooltip("Stage Factor = 1 + stageStep × (9 − item stage).")]
        [SerializeField] private float stageStep = 0.35f;

        [Tooltip("Damage is rolled within ± this share.")]
        [SerializeField, Range(0f, 1f)] private float spread = 0.15f;

        [Tooltip("Reduction = defence / (defence + defenceHalf): at this defence half the damage is stopped.")]
        [SerializeField] private float defenceHalf = 50f;

        [SerializeField, Range(0f, 1f)] private float reductionCap = 0.75f;

        [Tooltip("Physical defence × this against a firearm beyond the next cell.")]
        [SerializeField] private float rangedArmour = 2f;

        [Tooltip("The critical multiplier, before the excess over the critical cap.")]
        [SerializeField] private float criticalMultiplier = 2f;

        [Tooltip("The share of physical defence a critical melee blow passes by.")]
        [SerializeField, Range(0f, 1f)] private float criticalPierce = 0.5f;

        [Header("10.2 §5b Load in a Fight (07.2 §5.2, 19 §5)")]
        [Tooltip("Comfortable capacity = capacityBase + Strength, kg.")]
        [SerializeField] private float capacityBase = 20f;

        [Tooltip("kg a fighter carries besides its armour and weapons.")]
        [SerializeField] private float carried = 3f;

        [Tooltip("The upper share of capacity of the Normal, High and Overloaded bands; above the last is Critical Overload.")]
        [SerializeField] private float[] loadBands = { 1.00f, 1.25f, 1.50f };

        [Tooltip("Evasion lost in the Normal, High, Overloaded and Critical Overload bands, points.")]
        [SerializeField] private float[] loadEvasion = { 0f, 8f, 16f, 24f };

        [Tooltip("AP lost in each band.")]
        [SerializeField] private int[] loadAp = { 0, 0, 1, 2 };

        [Header("10.2 §5b Action Points and Initiative")]
        [SerializeField] private float apBase = 10f;

        [Tooltip("AP in a turn = apBase + Dexterity / apDexterityDivisor, the fraction carried.")]
        [SerializeField] private float apDexterityDivisor = 20f;

        [Tooltip("10.2 §5b Blocks and parries: AP of a reaction.")]
        [SerializeField] private int reactionAp = 2;

        [Tooltip("Initiative = (Dexterity + Perception) / 2 + 1d this.")]
        [SerializeField] private int initiativeDie = 20;

        [Header("10.2 §5b Position")]
        [Tooltip("Points to hit against a fighter Surrounded by two enemies at its sides.")]
        [SerializeField] private float surroundedByTwo = 10f;

        [Tooltip("Points to hit against a fighter Surrounded by three or four.")]
        [SerializeField] private float surroundedByMore = 20f;

        [Tooltip("Points to hit and to the critical chance from higher ground.")]
        [SerializeField] private float higherGround = 10f;

        [SerializeField] private float higherGroundCritical = 10f;

        [Tooltip("Metres the attacker's cell stands above the target's for higher ground. Elevation bands are open (10.2 §7); S3a.6 authoring default.")]
        [SerializeField] private float higherGroundRise = 1.5f;

        [Tooltip("Points to hit lost against a target in Half Cover and in Full Cover, ranged and magical attacks only.")]
        [SerializeField] private float halfCover = 15f;

        [SerializeField] private float fullCover = 30f;

        [Tooltip("10.2 §5b Firearms: points to hit a firearm loses while an enemy stands beside the shooter.")]
        [SerializeField] private float pointBlank = 10f;

        [Header("10.1 Shot Line (10.2 §5f)")]
        [Tooltip("Points to hit a shot loses when its line crosses a Partial Obstacle or its target stands in one, once however many (COMBAT-4).")]
        [SerializeField] private float obscured = 20f;

        [Tooltip("The chance that each person on a shot's line, nearest first, takes the shot before the target is rolled (COMBAT-3).")]
        [SerializeField, Range(0f, 1f)] private float intercept = 0.60f;

        [Header("10.2 §5b Wait and Defensive Stance")]
        [Tooltip("Evasion each AP held in a Defensive Stance adds.")]
        [SerializeField] private float stanceEvasion = 1.5f;

        [Tooltip("Each AP held in a Defensive Stance multiplies the chance to block or parry by (1 + this), as tactical_sim.py does.")]
        [SerializeField] private float stanceGuard = 0.03f;

        [Tooltip("A Counterattack costs the weapon's attack AP plus this.")]
        [SerializeField] private int counterattackAp = 2;

        [Tooltip("AP to change the Weapon Set in hand.")]
        [SerializeField] private int weaponSetAp = 2;

        [Header("10.2 First Aid and conditions in the slice: Bleeding")]
        [Tooltip("The share of maximum HP Bleeding takes each tick; a round is one tick.")]
        [SerializeField, Range(0f, 1f)] private float bleedingShare = 0.01f;

        [Tooltip("Bleeding ends by itself at Endurance × this a tick, Endurance / 2 %.")]
        [SerializeField] private float bleedingEndsPerEndurance = 0.005f;

        [Header("A headless fight, with no space (combat_sim.py)")]
        [Tooltip("AP a melee fighter spends closing in on its first turn, combat_sim.py's CLOSE_IN_CELLS.")]
        [SerializeField] private int closeInAp = 6;

        [Tooltip("A headless fight still undecided after this many rounds is a draw.")]
        [SerializeField] private int headlessRounds = 60;

        public int ReactionAp => reactionAp;
        public double BleedingShare => bleedingShare;
        public double BleedingEndsPerEndurance => bleedingEndsPerEndurance;
        public int CloseInAp => closeInAp;
        public int HeadlessRounds => headlessRounds;
        public double HigherGround => higherGround;
        public double HigherGroundCritical => higherGroundCritical;
        public float HigherGroundRise => higherGroundRise;
        public double PointBlank => pointBlank;
        public double Obscured => obscured;
        public double Intercept => intercept;
        public int CounterattackAp => counterattackAp;
        public int WeaponSetAp => weaponSetAp;

        /// <summary>10.2 §5b Position: points to hit a ranged or magical attack loses against a target in this cover.</summary>
        public double CoverPenalty(Grid.CoverLevel level) => level == Grid.CoverLevel.Full ? fullCover : level == Grid.CoverLevel.Half ? halfCover : 0d;

        /// <summary>10.2 §5b Position: points to hit against a fighter with this many enemies at its sides.</summary>
        public double SurroundedBonus(int enemiesBeside) => enemiesBeside >= 3 ? surroundedByMore : enemiesBeside == 2 ? surroundedByTwo : 0d;

        /// <summary>A fighter for a fight, at full HP; its gear is refused if no character could hold it so.</summary>
        public Fighter Fighter(
            ICharacterValues values,
            int? limit,
            WeaponSet set,
            FightArmour armour = null,
            WeaponSet second = default,
            FightModifiers modifiers = default,
            double? carried = null)
        {
            var fault = set.Fault() ?? (second.IsEmpty ? null : second.Fault());
            if (fault != null)
            {
                throw new ArgumentException(fault, nameof(set));
            }

            var fighter = new Fighter(values, limit, set, second, armour, modifiers) { Carried = carried };
            fighter.MaxHp = (hpBase + hpPerEndurance * values.Get(CharacterAttribute.Endurance)) * LimitMultiplier(limit) * (1d + modifiers.hp);
            fighter.Hp = fighter.MaxHp;
            fighter.MaxWillPower = WillPowerMax(values, limit, modifiers);
            fighter.WillPower = fighter.MaxWillPower;
            var band = LoadBand(LoadShare(fighter));
            fighter.LoadEvasion = loadEvasion[band];
            fighter.LoadAp = loadAp[band];
            return fighter;
        }

        /// <summary>A fighter joins a fight: its first AP fraction is rolled, and it holds no AP and loses no turn.</summary>
        public void Enter(Fighter fighter, Random rolls)
        {
            fighter.ApFraction = rolls.NextDouble();
            fighter.Reserve = 0;
            fighter.LostTurns = 0;
            fighter.Asleep = fighter.Disoriented = 0;
            fighter.Dazed = fighter.Exposed = fighter.Braced = false;
        }

        public double LimitMultiplier(int? limit)
        {
            if (limit is int l && (l < 0 || l >= limitMultiplier.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(limit), limit, "Limits run from 0 to 9.");
            }

            return limitMultiplier[limit ?? limitMultiplier.Length - 1];
        }

        public double StageFactor(int stage) => 1d + stageStep * (9 - stage);

        /// <summary>
        /// 10.2 §5b Attribute Requirements: the share of an item that works, the attribute furthest below its
        /// requirement over that requirement, with no floor; stage S asks (10 − S) times the stage 9 values.
        /// </summary>
        public static double Fit(ICharacterValues values, IReadOnlyList<AttributeRequirement> requirements, int stage)
        {
            var share = 1d;
            foreach (var requirement in requirements)
            {
                var asked = requirement.atStageNine * (10 - stage);
                if (asked > 0)
                {
                    share = Math.Min(share, Math.Min(1d, values.Get(requirement.attribute) / (double)asked));
                }
            }

            return share;
        }

        /// <summary>10.2 §5b Load in a Fight: armour, both Weapon Sets and what it carries, over comfortable capacity.</summary>
        public double LoadShare(Fighter fighter)
        {
            var kg = (fighter.Armour != null ? fighter.Armour.Weight : 0f) + fighter.Set.Weight + fighter.Second.Weight + (fighter.Carried ?? carried);
            return kg / (capacityBase + fighter.Get(CharacterAttribute.Strength));
        }

        /// <summary>0 Normal, 1 High, 2 Overloaded, 3 Critical Overload.</summary>
        public int LoadBand(double share)
        {
            for (var band = 0; band < loadBands.Length; band++)
            {
                if (share <= loadBands[band])
                {
                    return band;
                }
            }

            return loadBands.Length;
        }

        /// <summary>10.2 §5b: `10 + Dexterity / 20`, less the load, with the fraction carried into the next turn.</summary>
        public int TurnAp(Fighter fighter)
        {
            var whole = apBase + fighter.Get(CharacterAttribute.Dexterity) / apDexterityDivisor + fighter.Modifiers.ap - fighter.LoadAp + fighter.ApFraction;
            var turn = Math.Floor(whole);
            fighter.ApFraction = whole - turn;
            return (int)turn;
        }

        /// <summary>10.2 §5b: `(Dexterity + Perception) / 2 + 1d20`; a tie goes to the higher Dexterity, then at random (S3a.4).</summary>
        public double Initiative(Fighter fighter, Random rolls) =>
            (fighter.Get(CharacterAttribute.Dexterity) + fighter.Get(CharacterAttribute.Perception)) / 2d + fighter.Modifiers.initiative + rolls.Next(1, initiativeDie + 1);

        /// <summary>
        /// 10.2 §5f Accuracy: `(profile attribute + the skill of the weapon's kind) / 2`, Dexterity for a light weapon and
        /// Strength for a heavy one, with the weapon's to-hit and the modifiers added; Perception aims nothing.
        /// </summary>
        public double Accuracy(Fighter fighter)
        {
            var weapon = fighter.Weapon;
            var own = (fighter.Get(weapon.ProfileAttribute) + fighter.Get(weapon.Skill)) / 2d;
            return own + fighter.Modifiers.accuracy + weapon.ToHit;
        }

        /// <summary>
        /// 10.2 §5b Attack: `(Dexterity + Perception) / 2`, less the load, and in a Defensive Stance 1.5 for each AP held;
        /// zero for a target that cannot evade or sleeps (05.1 Дрёма).
        /// </summary>
        public double Evasion(Fighter fighter) =>
            fighter.CanEvade && fighter.Asleep == 0
                ? (fighter.Get(CharacterAttribute.Dexterity) + fighter.Get(CharacterAttribute.Perception)) / 2d + fighter.Modifiers.evasion - fighter.LoadEvasion
                  + (fighter.Stance ? stanceEvasion * fighter.Reserve : 0d)
                : 0d;

        /// <summary>The chance its parry or block succeeds: the Weapon Set's, raised in a Defensive Stance by each AP held.</summary>
        public double GuardChance(Fighter fighter) =>
            fighter.Set.GuardChance * (fighter.Stance ? 1d + stanceGuard * fighter.Reserve : 1d);

        /// <summary>10.2 §5b Attribute Requirements: below its weapon's requirements a fighter hits that share as often.</summary>
        public static double Aim(Fighter fighter) => Fit(fighter.Values, fighter.Weapon.Requirements, fighter.Weapon.Stage);

        /// <summary>Whether Perception is every attack's secondary attribute (07.1 §10, S5b.3), so that it grows with each.</summary>
        public bool PerceptionAims => perceptionSecondary > 0f;

        /// <summary>
        /// 07.1 §5 with §10's direction (S5b.3): `1 + share × min(1, Perception / Evasion)`, the target's Evasion standing for
        /// the task's difficulty, so at most +10 % of the hit chance, relatively; a target with no Evasion gives the full share.
        /// </summary>
        public double Secondary(Fighter attacker, Fighter target)
        {
            if (perceptionSecondary <= 0f)
            {
                return 1d;
            }

            var difficulty = Evasion(target);
            var reach = difficulty <= 0d ? 1d : Math.Min(1d, attacker.Get(CharacterAttribute.Perception) / difficulty);
            return 1d + (perceptionSecondary * reach);
        }

        /// <summary>
        /// 10.2 §5b Attack: `(70 % + Accuracy − Evasion + situational) × Perception's secondary share × (1 + Luck / 100)`,
        /// from 5 % to 95 %, then times the share of the weapon its user meets.
        /// </summary>
        public double HitChance(Fighter attacker, Fighter target, double bonusPoints = 0d)
        {
            var chance = (baseHit + (Accuracy(attacker) - Evasion(target) + bonusPoints) / 100d) * Secondary(attacker, target);
            var lucky = chance * (1d + attacker.Get(CharacterAttribute.Luck) / 100d);
            return Math.Clamp(lucky, hitFloor, hitCeiling) * Aim(attacker);
        }

        /// <summary>
        /// 07.1 §7 and 10.2 §5b: the critical chance by Perception, with the weapon's and the modifiers' points, times
        /// Luck; situational points come after Luck. Above the cap it is the excess the multiplier gains.
        /// </summary>
        public double RawCritical(Fighter attacker, double bonusPoints = 0d)
        {
            var share = (attacker.Get(CharacterAttribute.Perception) - CharacterSheet.Lowest) / (double)(CharacterSheet.Highest - CharacterSheet.Lowest);
            var byPerception = checks.CriticalAtLowest + (checks.CriticalAtHighest - checks.CriticalAtLowest) * share;
            var points = (attacker.Weapon.Critical + attacker.Modifiers.critical) / 100d;
            return (byPerception + points) * (1d + attacker.Get(CharacterAttribute.Luck) / 100d) + bonusPoints / 100d;
        }

        public double CriticalChance(double raw) => Math.Clamp(raw, 0d, checks.CriticalCap);

        /// <summary>10.2 §5b Damage: ×2 plus the excess over the 95 % cap, plus the modifiers.</summary>
        public double CriticalMultiplier(Fighter attacker, double raw, double extra = 0d) =>
            criticalMultiplier + Math.Max(0d, raw - checks.CriticalCap) + extra + attacker.Modifiers.criticalDamage;

        /// <summary>10.2 §5b Damage: `defence / (defence + 50)`, up to 75 %.</summary>
        public double Reduction(double defence) => defence <= 0d ? 0d : Math.Min(reductionCap, defence / (defence + defenceHalf));

        /// <summary>The armour's physical defence by its Stage Factor and the share its wearer meets, and the modifiers'.</summary>
        public double PhysicalDefence(Fighter target)
        {
            var armour = target.Armour;
            var worn = armour == null ? 0d : armour.Physical * StageFactor(armour.Stage) * Fit(target.Values, armour.Requirements, armour.Stage);
            return worn + target.Modifiers.defence;
        }

        /// <summary>
        /// The physical defence one strike meets: doubled against a firearm beyond the next cell, and halved by a
        /// critical melee blow, which finds the gap in the armour.
        /// </summary>
        public double DefenceAgainst(Fighter attacker, Fighter target, bool beyond, bool critical, double support = 0d)
        {
            var defence = PhysicalDefence(target) + support;
            if (attacker.Weapon.Ranged)
            {
                return beyond ? defence * rangedArmour : defence;
            }

            return critical ? defence * (1d - criticalPierce) : defence;
        }

        /// <summary>10.2 §5f Damage: `weapon base × Stage Factor` and the modifiers, before the roll; no attribute adds to it.</summary>
        public double BaseDamage(Fighter attacker)
        {
            var weapon = attacker.Weapon;
            return weapon.BaseDamage * StageFactor(weapon.Stage) * (1d + attacker.Modifiers.damage);
        }

        /// <summary>
        /// What a blow or shot in this context is likely to do (S3a.5): the chance of <see cref="HitChance"/>, and an
        /// ordinary hit's damage, the roll's whole spread through the armour it meets.
        /// </summary>
        public StrikeForecast Forecast(Fighter attacker, Fighter target, StrikeContext context)
        {
            var through = BaseDamage(attacker) * (1d - Reduction(DefenceAgainst(attacker, target, context.Beyond, false, context.Support))) * Braced(target);
            var magic = Warded(MagicShare(attacker, context) * WeaponBase(attacker), target);
            return new StrikeForecast(
                HitChance(attacker, target, context.HitBonus),
                (through * (1d - spread)) + magic,
                (through * (1d + spread)) + magic,
                baseHit,
                Accuracy(attacker),
                Evasion(target),
                context.HitBonus,
                attacker.Get(CharacterAttribute.Luck),
                Aim(attacker));
        }

        /// <summary>A melee hit can be parried or blocked by a target holding the reaction's AP and able to act.</summary>
        public bool CanGuard(Fighter target) =>
            target.Set.Guard != Guard.None && target.Reserve >= reactionAp && target.LostTurns == 0 && target.Asleep == 0 && target.CanEvade && !target.Down;

        /// <summary>
        /// One blow or shot: the hit, the critical hit, damage through armour, the target's parry or block from the AP
        /// it holds, then a stun if it still stands and the blow was no stealth attack (10.3 §7), and the conditions a
        /// beast's weapon starts. The draws come in that
        /// order from the fight's rolls.
        /// </summary>
        public StrikeResult Strike(Fighter attacker, Fighter target, StrikeContext context, Random rolls)
        {
            var weapon = attacker.Weapon;
            var result = new StrikeResult();
            if (rolls.NextDouble() >= HitChance(attacker, target, context.HitBonus))
            {
                return result;
            }

            result.Hit = true;
            var raw = RawCritical(attacker, CriticalPoints(context));
            result.Critical = rolls.NextDouble() < CriticalChance(raw);
            var multiplier = result.Critical ? CriticalMultiplier(attacker, raw, CriticalExtraOf(weapon, context)) : 1d;
            var share = (context.Cleaving ? weapon.Cleave : 1d) * (context.Multiplier > 0d ? context.Multiplier : 1d);
            var rolled = BaseDamage(attacker) * (1d - spread + 2d * spread * rolls.NextDouble());
            var damage = rolled * multiplier * share * (1d - Reduction(DefenceAgainst(attacker, target, context.Beyond, result.Critical, context.Support))) * Braced(target);
            // 10.2 §5f: Infusion and the items' Magical Damage, a share of the weapon's base, through magical defence.
            damage += Warded(MagicShare(attacker, context) * WeaponBase(attacker) * multiplier * share, target);

            if (!context.Unguarded && !weapon.Ranged && CanGuard(target))
            {
                var chance = GuardChance(target); // read before the reaction spends its AP
                target.Reserve -= reactionAp;
                result.GuardTried = true;
                if (rolls.NextDouble() < chance)
                {
                    result.Guarded = true;
                    damage *= 1d - target.Set.GuardStops;
                }
            }

            target.Hp -= damage;
            result.Damage = damage;
            result.Downed = target.Down;
            if (damage > 0d)
            {
                target.Asleep = 0; // 05.1 Дрёма: damage wakes
            }

            if (!context.Stealth && !target.Down && weapon.Stun > 0f && rolls.NextDouble() < weapon.Stun)
            {
                result.Stunned = true;
                target.LostTurns = Math.Max(target.LostTurns, 1);
            }

            result.Bleeds = result.Critical && weapon.BleedsOnCritical && damage > 0d && !target.Down;
            result.Poisoned = weapon.PoisonOnHit > 0f && !target.Down && rolls.NextDouble() < weapon.PoisonOnHit;
            return result;
        }

        /// <summary>10.2 §5b Damage: a greatsword's second strike, at an enemy beside the target, with its own roll for its share of the damage.</summary>
        public StrikeResult Cleave(Fighter attacker, Fighter second, StrikeContext context, Random rolls)
        {
            if (attacker.Weapon.Cleave <= 0f)
            {
                throw new InvalidOperationException($"{attacker.Weapon.name} does not cleave.");
            }

            context.Cleaving = true;
            return Strike(attacker, second, context, rolls);
        }
    }
}
