namespace GameDev.Confront.Rules
{
    /// <summary>
    /// What a blow or shot is likely to do, before it is made (S3a.5): the chance and the damage the player is shown from
    /// Intelligence 10, and the parts of the chance shown from 20 (10.4 §3, 07.1's progressive disclosure). The damage is
    /// an ordinary hit's, through the target's armour, with no critical hit and no guard.
    /// </summary>
    public readonly struct StrikeForecast
    {
        public StrikeForecast(double chance, double low, double high, double baseHit, double accuracy, double evasion, double situational, int luck, double fit)
        {
            Chance = chance;
            Low = low;
            High = high;
            BaseHit = baseHit;
            Accuracy = accuracy;
            Evasion = evasion;
            Situational = situational;
            Luck = luck;
            Fit = fit;
        }

        /// <summary>The hit chance, 0 to 1.</summary>
        public double Chance { get; }

        public double Low { get; }

        public double High { get; }

        /// <summary>The peers' chance before anything else, 0 to 1.</summary>
        public double BaseHit { get; }

        /// <summary>The attacker's accuracy, in points.</summary>
        public double Accuracy { get; }

        /// <summary>The target's Evasion, in points.</summary>
        public double Evasion { get; }

        /// <summary>Points the place and the moment add.</summary>
        public double Situational { get; }

        /// <summary>The attacker's Luck, which multiplies the chance by (1 + Luck / 100).</summary>
        public int Luck { get; }

        /// <summary>The share of its weapon the attacker meets, which multiplies the chance last.</summary>
        public double Fit { get; }
    }

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
