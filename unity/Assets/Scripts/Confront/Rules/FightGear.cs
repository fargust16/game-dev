using System;
using GameDev.Player.Progression;

namespace GameDev.Confront.Rules
{
    /// <summary>How an item answers a melee hit (10.2 §5b *Blocks and parries*).</summary>
    public enum Guard
    {
        None,
        Parry,
        Block,
    }

    /// <summary>What a weapon's other hand holds (10.2 §5b *Blocks and parries*, *Firearms*).</summary>
    public enum OffHandKind
    {
        Shield,
        CartridgePouch,
    }

    /// <summary>One Attribute Requirement of an item at stage 9 (10.2 §5b); an item of stage S asks (10 − S) times as much.</summary>
    [Serializable]
    public struct AttributeRequirement
    {
        public CharacterAttribute attribute;
        public int atStageNine;

        public AttributeRequirement(CharacterAttribute attribute, int atStageNine)
        {
            this.attribute = attribute;
            this.atStageNine = atStageNine;
        }
    }

    /// <summary>
    /// What a fighter's item modifiers add up to (19 §2.4), in 10.2 §5b's units: shares for damage, HP and the critical
    /// multiplier; percentage points for accuracy, Evasion and critical chance; flat defence, AP and Initiative. A
    /// glowing beast's extra HP is a share here too (10.2 §5b *Beasts of the slice*).
    /// </summary>
    [Serializable]
    public struct FightModifiers
    {
        public float damage;
        public float accuracy;
        public float critical;
        public float criticalDamage;
        public float hp;
        public float defence;
        public float evasion;
        public float ap;
        public float initiative;

        /// <summary>19 §2.4 Magical Damage: a share of the weapon's base each blow or shot adds as magical damage (10.2 §5f).</summary>
        public float magic;

        /// <summary>19 §2.4 Magical Defence, flat, beside the armour's.</summary>
        public float ward;

        /// <summary>19 §2.4 Maximum Will Power, a share (07.2 §3.3).</summary>
        public float willPower;

        /// <summary>19 §2.4 Will against control, added to the target's Will in a Will Contest (10.2 §5f).</summary>
        public float will;
    }

    /// <summary>
    /// A weapon and what its other hand holds (10.2 §5b): a shield only with a weapon that takes one, a sword, an axe or a
    /// mace, a firearm only with a Cartridge Pouch, and a second one-handed melee weapon only beside another (*Two
    /// weapons*). An empty set is a second set the fighter does not carry.
    /// </summary>
    public readonly struct WeaponSet
    {
        public WeaponSet(FightWeapon weapon, FightOffHand offHand = null, FightWeapon offWeapon = null)
        {
            Weapon = weapon;
            OffHand = offHand;
            OffWeapon = offWeapon;
        }

        /// <summary>The main hand's weapon.</summary>
        public FightWeapon Weapon { get; }

        public FightOffHand OffHand { get; }

        /// <summary>10.2 §5b *Two weapons*: the other hand's one-handed melee weapon, or null.</summary>
        public FightWeapon OffWeapon { get; }

        /// <summary>A one-handed melee weapon in each hand.</summary>
        public bool Paired => OffWeapon != null;

        public bool IsEmpty => Weapon == null && OffHand == null && OffWeapon == null;

        public float Weight => (Weapon != null ? Weapon.Weight : 0f) + (OffHand != null ? OffHand.Weight : 0f) + (OffWeapon != null ? OffWeapon.Weight : 0f);

        private bool Shielded => OffHand != null && OffHand.Guard != Guard.None;

        /// <summary>The weapon that parries: two weapons parry with the better of the two.</summary>
        private FightWeapon Parrying => Paired && OffWeapon.GuardChance > Weapon.GuardChance ? OffWeapon : Weapon;

        /// <summary>The set's answer to a melee hit: a shield's block over the weapon's own guard, or the better weapon's parry.</summary>
        public Guard Guard => Shielded ? OffHand.Guard : Parrying.Guard;

        public float GuardChance => Shielded ? OffHand.GuardChance : Parrying.GuardChance;

        public float GuardStops => Shielded ? OffHand.GuardStops : Parrying.GuardStops;

        /// <summary>A weapon that may go in either hand beside another: a dagger, a sword, an axe or a mace.</summary>
        public static bool OneHandedMelee(FightWeapon weapon) => weapon != null && !weapon.Ranged && !weapon.TwoHanded;

        /// <summary>Why the set cannot be held, or null when it can.</summary>
        public string Fault()
        {
            if (Weapon == null)
            {
                return "a Weapon Set holds a weapon";
            }

            var pouch = OffHand != null && OffHand.Kind == OffHandKind.CartridgePouch;
            if (Weapon.Ranged && !pouch)
            {
                return $"{Weapon.name} is held only with a Cartridge Pouch in the other hand";
            }

            if (pouch && !Weapon.Ranged)
            {
                return $"a Cartridge Pouch goes only with a firearm, not {Weapon.name}";
            }

            if (OffHand != null && OffHand.Kind == OffHandKind.Shield && !Weapon.TakesShield)
            {
                return $"a shield goes only with a sword, an axe or a mace, not {Weapon.name}";
            }

            if (OffWeapon != null && OffHand != null)
            {
                return $"the other hand holds {OffWeapon.name} or {OffHand.name}, not both";
            }

            if (OffWeapon != null && (!OneHandedMelee(Weapon) || !OneHandedMelee(OffWeapon)))
            {
                return $"two weapons are one-handed melee weapons, not {Weapon.name} and {OffWeapon.name}";
            }

            return null;
        }
    }
}
