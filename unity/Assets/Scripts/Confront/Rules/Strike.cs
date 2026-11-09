namespace GameDev.Confront.Rules
{
    /// <summary>
    /// What the place and the moment add to one blow or shot (10.2 §5b *Position*, *Firearms*). The grid fills it from
    /// S3a.6; a strike with the default context is a plain blow at a target beside the attacker.
    /// </summary>
    public struct StrikeContext
    {
        /// <summary>Percentage points added to the hit chance before Luck: Surrounded, higher ground, cover, a Point-Blank Shot.</summary>
        public double HitBonus;

        /// <summary>Percentage points added to the critical chance after Luck: higher ground.</summary>
        public double CriticalBonus;

        /// <summary>Added to the critical multiplier.</summary>
        public double CriticalExtra;

        /// <summary>The target stands beyond the cell next to the attacker, where armour counts double against a firearm.</summary>
        public bool Beyond;

        /// <summary>The blow cannot be parried or blocked.</summary>
        public bool Unguarded;

        /// <summary>The second strike of a cleave, for the weapon's share of the damage (10.2 §5b *Damage*).</summary>
        public bool Cleaving;
    }

    /// <summary>What one blow or shot did. The conditions it starts are applied by their own system (S3a.7).</summary>
    public struct StrikeResult
    {
        public bool Hit;
        public bool Critical;

        /// <summary>HP taken from the target, after its armour and any guard.</summary>
        public double Damage;

        /// <summary>The target spent its reaction AP on a parry or a block.</summary>
        public bool GuardTried;

        /// <summary>The parry or block succeeded.</summary>
        public bool Guarded;

        public bool Stunned;

        /// <summary>A beast's critical hit that deals damage: the target bleeds (10.2 §5b *Beasts of the slice*).</summary>
        public bool Bleeds;

        /// <summary>A glowing beast's bite: the target is poisoned.</summary>
        public bool Poisoned;

        /// <summary>The target fell to 0 HP or below.</summary>
        public bool Downed;
    }
}
