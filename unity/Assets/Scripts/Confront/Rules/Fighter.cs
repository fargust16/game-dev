using GameDev.Player.Progression;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// One fighter in a fight: its character sheet, Limit, Weapon Sets, armour and modifiers, what they give once
    /// (maximum HP and Load in a Fight), and what changes as the fight goes: HP, the AP it holds for reactions, the
    /// fraction of AP it carries to its next turn, and the turns it loses. <see cref="FightRules.Fighter"/> builds it and
    /// checks its gear.
    /// </summary>
    public sealed class Fighter
    {
        internal Fighter(ICharacterValues values, int? limit, WeaponSet set, WeaponSet second, FightArmour armour, FightModifiers modifiers)
        {
            Values = values;
            Limit = limit;
            Set = set;
            Second = second;
            Armour = armour;
            Modifiers = modifiers;
        }

        public ICharacterValues Values { get; }

        /// <summary>The fighter's Limit, 0 to 9; null for an Uninitiated character or a beast.</summary>
        public int? Limit { get; }

        /// <summary>The Weapon Set in hand.</summary>
        public WeaponSet Set { get; }

        /// <summary>The second Weapon Set, empty if it carries none; its weight counts in Load in a Fight.</summary>
        public WeaponSet Second { get; }

        /// <summary>Null for no armour.</summary>
        public FightArmour Armour { get; }

        public FightModifiers Modifiers { get; }

        public FightWeapon Weapon => Set.Weapon;

        public double MaxHp { get; internal set; }

        public double Hp { get; set; }

        /// <summary>10.2 §5b Load in a Fight: what the load takes from Evasion, in points.</summary>
        public double LoadEvasion { get; internal set; }

        /// <summary>10.2 §5b Load in a Fight: what the load takes from each turn's AP.</summary>
        public int LoadAp { get; internal set; }

        /// <summary>10.2 §5b Blocks and parries: the AP left at the end of its turn, which reactions spend.</summary>
        public int Reserve { get; set; }

        /// <summary>10.2 §5b Action Points: the fraction of AP carried into the next turn.</summary>
        public double ApFraction { get; set; }

        /// <summary>The turns it will lose, as a stun takes them.</summary>
        public int LostTurns { get; set; }

        /// <summary>False for a target that cannot evade, with an Evasion of zero (10.2 §5b Attack); it cannot guard either.</summary>
        public bool CanEvade { get; set; } = true;

        public bool Down => Hp <= 0d;

        public int Get(CharacterAttribute attribute) => Values.Get(attribute);

        public int Get(CharacterSkill skill) => Values.Get(skill);
    }
}
