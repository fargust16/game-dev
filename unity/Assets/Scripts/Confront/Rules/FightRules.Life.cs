using System;
using GameDev.Confront.Grid;
using GameDev.Player.Progression;
using UnityEngine;
using Random = System.Random;

namespace GameDev.Confront.Rules
{
    /// <summary>A First Aid treatment of 10.2 *First Aid and conditions in the slice*, and the condition it ends.</summary>
    public enum Treatment
    {
        Bandage,
        Antidote,
        Splint,
    }

    /// <summary>What one world tick did to a fighter's conditions (S3a.7).</summary>
    public struct ConditionTick
    {
        public double Bled;
        public bool BleedingEnded;
        public bool PoisonEnded;
        public double Recovered;

        public bool Any => Bled > 0d || BleedingEnded || PoisonEnded;
    }

    /// <summary>
    /// S3a.7: the conditions of the slice on world ticks, falls, the Environment Actions of 10.1 and the First Aid
    /// treatments of 10.2, as numbers of the same asset.
    /// </summary>
    public sealed partial class FightRules
    {
        // 10.1 V1 numbers: each action's attribute and secondaries, in the order of EnvironmentActionKind. They are the
        // design's table, not tuning, so they stay in code with it.
        private static readonly CharacterAttribute[] EnvironmentAttribute =
        {
            CharacterAttribute.Dexterity, CharacterAttribute.Strength, CharacterAttribute.Dexterity, CharacterAttribute.Strength, CharacterAttribute.Strength,
        };

        private static readonly CharacterAttribute[][] EnvironmentSecondary =
        {
            new[] { CharacterAttribute.Strength },
            new[] { CharacterAttribute.Endurance, CharacterAttribute.Dexterity },
            new[] { CharacterAttribute.Strength },
            new[] { CharacterAttribute.Dexterity },
            new[] { CharacterAttribute.Endurance },
        };

        [Header("10.2 First Aid and conditions in the slice: Poison, recovery and falls")]
        [Tooltip("Poison ends by itself at Endurance × this a tick, Endurance / 4 %.")]
        [SerializeField] private float poisonEndsPerEndurance = 0.0025f;

        [Tooltip("10.2 Health, owner-approved 2026-10-31: the share of maximum HP that returns each tick outside a fight, 1 % for each 10 ticks, while no condition stops it.")]
        [SerializeField] private float recoveryShare = 0.001f;

        [Tooltip("10.1 Failure: a fall takes this share of maximum HP for each 2 m, which armour does not reduce.")]
        [SerializeField] private float fallShare = 0.1f;

        [Tooltip("10.2: a fall of this many metres or more breaks a leg.")]
        [SerializeField] private float fallBreaksLeg = 4f;

        [Header("10.1 V1 numbers: Environment Actions")]
        [Tooltip("AP of Vault / Jump, Climb Up, Climb Through Window, Mantle Onto and Kick / Breach, in that order. They replace the step's 1 AP.")]
        [SerializeField] private int[] environmentAp = { 2, 3, 3, 3, 3 };

        [Tooltip("The difficulty D of the Easy, Hard and Very hard bands.")]
        [SerializeField] private int[] bandDifficulty = { 1, 3, 5 };

        [Tooltip("S3a.7 authoring defaults: the rise, m, from the cell left to the cell landed in, lowest and highest, for each action in the same order. Kick / Breach lands nowhere.")]
        [SerializeField] private Vector2[] landingRise =
        {
            new Vector2(-1.2f, 0.6f), new Vector2(0.6f, 3.2f), new Vector2(-1.2f, 0.6f), new Vector2(0.4f, 2.2f), Vector2.zero,
        };

        [Header("10.2 First Aid in the slice: bandage, antidote, splint")]
        [SerializeField] private int[] treatmentAp = { 3, 3, 6 };

        [Tooltip("Ticks a treatment takes outside a fight.")]
        [SerializeField] private int[] treatmentTicks = { 1, 1, 3 };

        [SerializeField] private int[] treatmentDifficulty = { 1, 2, 3 };

        [Tooltip("Whether a treatment needs the First Aid lesson.")]
        [SerializeField] private bool[] treatmentNeedsLesson = { false, true, true };

        [NonSerialized] private int[] _secondaryLevels;

        public int EnvironmentAp(EnvironmentActionKind kind) => environmentAp[(int)kind];

        public int BandDifficulty(EnvironmentActionBand band) => bandDifficulty[(int)band];

        /// <summary>The rise from the cell left to the cell landed in, m, as (lowest, highest).</summary>
        public Vector2 LandingRise(EnvironmentActionKind kind) => landingRise[(int)kind];

        public int TreatmentAp(Treatment treatment) => treatmentAp[(int)treatment];

        public int TreatmentTicks(Treatment treatment) => treatmentTicks[(int)treatment];

        public bool TreatmentNeedsLesson(Treatment treatment) => treatmentNeedsLesson[(int)treatment];

        /// <summary>
        /// 10.1 Success and Failure: an Environment Action's chance on the curve of 07.1, the attribute alone against the
        /// band's D (Climbing and Jumping are candidate skills), with its secondaries and Luck. It has no critical outcome.
        /// </summary>
        public double EnvironmentChance(ICharacterValues values, EnvironmentActionKind kind, EnvironmentActionBand band)
        {
            var secondary = EnvironmentSecondary[(int)kind];
            if (_secondaryLevels == null || _secondaryLevels.Length != secondary.Length)
            {
                _secondaryLevels = new int[secondary.Length];
            }

            for (var i = 0; i < secondary.Length; i++)
            {
                _secondaryLevels[i] = values.Get(secondary[i]);
            }

            return ActionCheck.Chances(checks, values.Get(EnvironmentAttribute[(int)kind]), BandDifficulty(band),
                values.Get(CharacterAttribute.Perception), values.Get(CharacterAttribute.Luck), false, _secondaryLevels).Success;
        }

        /// <summary>10.2 First Aid in the slice: `Δ = (Dexterity + First Aid skill) / 2 − D` on the curve of 07.1, with Luck.</summary>
        public double TreatmentChance(ICharacterValues values, Treatment treatment) =>
            ActionCheck.Chances(checks, ActionCheck.Preparation(values.Get(CharacterAttribute.Dexterity), values.Get(CharacterSkill.FirstAid)),
                treatmentDifficulty[(int)treatment], values.Get(CharacterAttribute.Perception), values.Get(CharacterAttribute.Luck), false).Success;

        /// <summary>Whether the fighter has the condition the treatment ends.</summary>
        public static bool Needs(Fighter fighter, Treatment treatment) =>
            treatment == Treatment.Bandage ? fighter.Bleeding : treatment == Treatment.Antidote ? fighter.Poisoned : fighter.LegBroken;

        public static void Cure(Fighter fighter, Treatment treatment)
        {
            switch (treatment)
            {
                case Treatment.Bandage:
                    fighter.Bleeding = false;
                    break;
                case Treatment.Antidote:
                    fighter.Poisoned = false;
                    break;
                default:
                    fighter.LegBroken = false;
                    break;
            }
        }

        /// <summary>
        /// One world tick of the fighter's conditions (10.2): Bleeding takes its share of maximum HP, then may end by
        /// Endurance; Poison may end by Endurance. Outside a fight (<paramref name="recovering"/>) HP returns at the
        /// ordinary rate while neither stops it. The draws come in that order.
        /// </summary>
        public ConditionTick Tick(Fighter fighter, Random rolls, bool recovering)
        {
            var tick = default(ConditionTick);
            if (fighter.Down)
            {
                return tick;
            }

            if (fighter.Bleeding)
            {
                tick.Bled = bleedingShare * fighter.MaxHp;
                fighter.Hp -= tick.Bled;
                if (rolls.NextDouble() < fighter.Get(CharacterAttribute.Endurance) * bleedingEndsPerEndurance)
                {
                    fighter.Bleeding = false;
                    tick.BleedingEnded = true;
                }
            }

            if (fighter.Poisoned && rolls.NextDouble() < fighter.Get(CharacterAttribute.Endurance) * poisonEndsPerEndurance)
            {
                fighter.Poisoned = false;
                tick.PoisonEnded = true;
            }

            if (recovering && !fighter.Down && !fighter.Bleeding && !fighter.Poisoned && fighter.Hp < fighter.MaxHp)
            {
                var hp = Math.Min(fighter.MaxHp, fighter.Hp + recoveryShare * fighter.MaxHp);
                tick.Recovered = hp - fighter.Hp;
                fighter.Hp = hp;
            }

            return tick;
        }

        /// <summary>10.1 Failure: a fall takes its share of maximum HP for each 2 m, through any armour, and from 4 m breaks a leg (10.2).</summary>
        public double Fall(Fighter fighter, float metres)
        {
            if (metres <= 0f)
            {
                return 0d;
            }

            var damage = fallShare * (metres / 2d) * fighter.MaxHp;
            fighter.Hp -= damage;
            if (metres >= fallBreaksLeg)
            {
                fighter.LegBroken = true;
            }

            return damage;
        }
    }
}
