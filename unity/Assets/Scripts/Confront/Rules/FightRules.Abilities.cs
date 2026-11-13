using System;
using GameDev.Player.Progression;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// The numbers of Will Power and abilities in a fight (07.2 §3.3, §4.5; 10.2 §5f; 05.1 *Abilities of the slice*):
    /// maximum Will Power, the Prepared slots, magical accuracy and the Will Contest, magical defence, Infusion, and what
    /// the conditions of abilities add. As `tools/balance/combat_sim.py` and `tactical_sim.py` have them; every number is
    /// an authoring default.
    /// </summary>
    public sealed partial class FightRules
    {
        [Header("07.2 §3.3 Will Power")]
        [Tooltip("07.2 §3.3: maximum Will Power = (willPowerBase + willPowerPerWill × Will) × S.")]
        [SerializeField] private float willPowerBase = 20f;

        [SerializeField] private float willPowerPerWill = 0.8f;

        [Header("07.2 §4.5 Prepared Abilities")]
        [Tooltip("Slots = slotsLeast + floor((Intelligence + Will − 2) / slotsStep), at most slotsMost.")]
        [SerializeField, Min(0)] private int slotsLeast = 3;

        [SerializeField, Min(1)] private int slotsStep = 14;

        [SerializeField, Min(0)] private int slotsMost = 10;

        [Header("10.2 §5f Infusion")]
        [Tooltip("Will Power a blow or shot costs, × S.")]
        [SerializeField, Min(0f)] private float infusionCost = 5f;

        [Tooltip("The share of the weapon's base × Stage Factor it adds as magical damage, before the wearer's armour lets it through.")]
        [SerializeField, Min(0f)] private float infusionShare = 0.40f;

        [Header("10.2 §5f, 05.1 Abilities in a fight")]
        [Tooltip("10.2 §5f Will Contest: points of accuracy against Will that move the chance by one whole.")]
        [SerializeField, Min(1f)] private float contestScale = 100f;

        [Tooltip("05.1 Укол разума: points to hit a disoriented fighter loses for its turn.")]
        [SerializeField] private float disorientedHit = 10f;

        [Tooltip("05.1: points to hit the next attack on a fighter whose ability failed gains.")]
        [SerializeField] private float exposedHit = 10f;

        [Tooltip("05.1 Не сдвинуть: the share of physical damage it stops.")]
        [SerializeField, Range(0f, 1f)] private float braceReduction = 0.25f;

        [Tooltip("05.1 Опора: physical defence lent to allies within its range.")]
        [SerializeField] private float supportDefence = 3f;

        [Tooltip("05.1 Дрёма: the AP an ally beside spends to wake a sleeper (owner, 2026-11-03).")]
        [SerializeField, Min(0)] private int wakeAp = 6;

        public double DisorientedHit => disorientedHit;
        public double ExposedHit => exposedHit;
        public double SupportDefence => supportDefence;
        public int WakeAp => wakeAp;

        /// <summary>07.2 §3.3: `(20 + 0.8 × Will) × S`, and the items' share.</summary>
        public double WillPowerMax(ICharacterValues values, int? limit, FightModifiers modifiers) =>
            (willPowerBase + willPowerPerWill * values.Get(CharacterAttribute.Will)) * LimitMultiplier(limit) * (1d + modifiers.willPower);

        /// <summary>07.2 §4.5: `3 + floor((Intelligence + Will − 2) / 14)`, at most 10.</summary>
        public int PreparedSlots(ICharacterValues values)
        {
            var sum = values.Get(CharacterAttribute.Intelligence) + values.Get(CharacterAttribute.Will) - 2;
            return Math.Min(slotsMost, slotsLeast + (Math.Max(0, sum) / slotsStep));
        }

        /// <summary>10.2 §5f: a targeted ability aims by `(Intelligence + Arcana Skill) / 2` and the items; Will never aims.</summary>
        public double MagicAccuracy(Fighter caster) =>
            ((caster.Get(CharacterAttribute.Intelligence) + caster.Get(CharacterSkill.Arcana)) / 2d) + caster.Modifiers.accuracy;

        /// <summary>A targeted ability's hit chance: §5b's formula with magical accuracy, from 5 % to 95 %.</summary>
        public double MagicHitChance(Fighter caster, Fighter target, double bonusPoints = 0d)
        {
            var chance = (baseHit + ((MagicAccuracy(caster) - Evasion(target) + bonusPoints) / 100d)) * Secondary(caster, target);
            return Math.Clamp(chance * (1d + caster.Get(CharacterAttribute.Luck) / 100d), hitFloor, hitCeiling);
        }

        /// <summary>
        /// 10.2 §5f Will Contest: `(start + (Intelligence + Arcana Skill) / 2 − the target's Will) × (1 + Luck / 100)`, from
        /// 5 % to 95 %; the target's Will counts its items' Will against control.
        /// </summary>
        public double WillContest(Fighter caster, Fighter target, double start)
        {
            var will = target.Get(CharacterAttribute.Will) + target.Modifiers.will;
            var chance = start + ((MagicAccuracy(caster) - will) / contestScale);
            return Math.Clamp(chance * (1d + caster.Get(CharacterAttribute.Luck) / 100d), hitFloor, hitCeiling);
        }

        /// <summary>The armour's magical defence by its Stage Factor and the share its wearer meets, and the items'.</summary>
        public double MagicalDefence(Fighter target)
        {
            var armour = target.Armour;
            var worn = armour == null ? 0d : armour.Magical * StageFactor(armour.Stage) * Fit(target.Values, armour.Requirements, armour.Stage);
            return worn + target.Modifiers.ward;
        }

        /// <summary>Magical damage through the target's magical defence.</summary>
        public double Warded(double magic, Fighter target) => magic <= 0d ? 0d : magic * (1d - Reduction(MagicalDefence(target)));

        /// <summary>The weapon's base by its Stage Factor, the measure of Infusion and Magical Damage.</summary>
        public double WeaponBase(Fighter attacker) => attacker.Weapon.BaseDamage * StageFactor(attacker.Weapon.Stage);

        /// <summary>10.2 §5f: the Will Power one Infused blow or shot costs, 5 × S.</summary>
        public double InfusionCost(Fighter fighter) => infusionCost * LimitMultiplier(fighter.Limit);

        /// <summary>10.2 §5f: the share of the weapon's base Infusion adds, 0.40 through the wearer's armour: chain halves it, plate stops it.</summary>
        public double InfusionShare(Fighter fighter) => infusionShare * (fighter.Armour != null ? fighter.Armour.Channel : 1d);

        /// <summary>The share of the weapon's base a blow adds as magical damage: its Infusion and the items' Magical Damage.</summary>
        public double MagicShare(Fighter attacker, StrikeContext context) => context.MagicShare + attacker.Modifiers.magic;

        /// <summary>05.1 Не сдвинуть: what a braced target's physical damage is multiplied by.</summary>
        public double Braced(Fighter target) => target.Braced ? 1d - braceReduction : 1d;

        /// <summary>An ability's price in Will Power, a share of its user's maximum.</summary>
        public double AbilityCost(Fighter fighter, FightAbility ability) => fighter.MaxWillPower * ability.Cost;

        /// <summary>
        /// Whether <paramref name="fighter"/> can pay <paramref name="cost"/>: the player down to nothing, which is full
        /// exhaustion (07.2 §3.3); an NPC only while it keeps something back (`combat_sim.py` can_spend).
        /// </summary>
        public static bool CanPay(Fighter fighter, double cost, bool toExhaustion) =>
            fighter.Arcana != Arcana.None && (toExhaustion ? fighter.WillPower >= cost : fighter.WillPower > cost);

        /// <summary>A Will Contest rolled: true when the caster wins.</summary>
        public bool Contest(Fighter caster, Fighter target, double start, Random rolls) => rolls.NextDouble() < WillContest(caster, target, start);

        /// <summary>
        /// 05.1 Укол разума: a ranged magical attack of <paramref name="ability"/>'s base, ±15 %, through magical defence,
        /// with no critical hit; damage wakes a sleeper. The turns disorient whoever it leaves so.
        /// </summary>
        public StrikeResult Sting(Fighter caster, Fighter target, FightAbility ability, double bonusPoints, Random rolls)
        {
            var result = new StrikeResult();
            if (rolls.NextDouble() >= MagicHitChance(caster, target, bonusPoints))
            {
                return result;
            }

            result.Hit = true;
            var raw = ability.BaseDamage * (1d - spread + (2d * spread * rolls.NextDouble()));
            var damage = Warded(raw, target);
            target.Hp -= damage;
            result.Damage = damage;
            result.Downed = target.Down;
            if (damage > 0d)
            {
                target.Asleep = 0;
            }

            return result;
        }

        /// <summary>
        /// 05.1 Гнёт: magical damage of <paramref name="ability"/>'s base, ±15 %, through magical defence, to one in its
        /// area, with no roll to hit and no critical hit; damage wakes a sleeper (CR5).
        /// </summary>
        public StrikeResult Press(Fighter target, FightAbility ability, Random rolls)
        {
            var raw = ability.BaseDamage * (1d - spread + (2d * spread * rolls.NextDouble()));
            var damage = Warded(raw, target);
            target.Hp -= damage;
            if (damage > 0d)
            {
                target.Asleep = 0;
            }

            return new StrikeResult { Hit = true, Damage = damage, Downed = target.Down };
        }
    }
}
