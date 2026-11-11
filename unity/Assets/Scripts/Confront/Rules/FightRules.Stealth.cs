using UnityEngine;

namespace GameDev.Confront.Rules
{
    /// <summary>
    /// S3b.1: the stealth attack of 10.3 §7. It rolls to hit as usual; on a hit it adds 50 percentage points to the
    /// critical chance after Luck, the excess over the cap going into critical damage, and the weapon's
    /// <see cref="FightWeapon.StealthCritical"/> (a dagger's 1.0) to the critical multiplier; it never stuns.
    /// </summary>
    public sealed partial class FightRules
    {
        [Header("10.3 §7 Stealth attack (S3b.1)")]
        [Tooltip("Percentage points a stealth attack adds to the critical chance, after Luck (owner-approved 2026-11-03; may rise after playtests).")]
        [SerializeField] private float stealthCritical = 50f;

        public double StealthCritical => stealthCritical;

        private double CriticalPoints(StrikeContext context) => context.CriticalBonus + (context.Stealth ? stealthCritical : 0d);

        private static double CriticalExtraOf(FightWeapon weapon, StrikeContext context) =>
            context.CriticalExtra + (context.Stealth ? weapon.StealthCritical : 0d);
    }
}
