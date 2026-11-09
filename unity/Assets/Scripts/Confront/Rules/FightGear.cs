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
    }

    /// <summary>
    /// A weapon and what its other hand holds (10.2 §5b): a shield only with a weapon that takes one, a sword, an axe or a
    /// mace, and a firearm only with a Cartridge Pouch. An empty set is a second set the fighter does not carry.
    /// </summary>
    public readonly struct WeaponSet
    {
        public WeaponSet(FightWeapon weapon, FightOffHand offHand = null)
        {
            Weapon = weapon;
            OffHand = offHand;
        }

        public FightWeapon Weapon { get; }

        public FightOffHand OffHand { get; }

        public bool IsEmpty => Weapon == null && OffHand == null;

        public float Weight => (Weapon != null ? Weapon.Weight : 0f) + (OffHand != null ? OffHand.Weight : 0f);

        private bool Shielded => OffHand != null && OffHand.Guard != Guard.None;

        /// <summary>The set's answer to a melee hit: a shield's block over the weapon's own guard.</summary>
        public Guard Guard => Shielded ? OffHand.Guard : Weapon.Guard;

        public float GuardChance => Shielded ? OffHand.GuardChance : Weapon.GuardChance;

        public float GuardStops => Shielded ? OffHand.GuardStops : Weapon.GuardStops;

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

            return null;
        }
    }
}
