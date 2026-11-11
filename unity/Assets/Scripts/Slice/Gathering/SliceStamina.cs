using System;
using GameDev.Player.Progression;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Gathering
{
    /// <summary>
    /// The character's Stamina (07.2 §3.2; S5a.5): its maximum is (20 + 1.6 × Endurance) × S, with S = 1 until S5b's
    /// Limits; exertion spends it, and each world tick without exertion gives back 5 % of the maximum. The spent share is
    /// kept in the world state in thousandths, so it lives with the run.
    /// </summary>
    public sealed class SliceStamina
    {
        /// <summary>World state: the share of maximum Stamina spent, in thousandths.</summary>
        public const string SpentKey = "player.stamina.spent";

        /// <summary>07.2 §2: the Limit multiplier S; 1 for a character without an Arcana, which S5b changes.</summary>
        public const double LimitMultiplier = 1d;

        private readonly GatherRules _rules;
        private readonly IWorldState _state;
        private readonly ICharacterValues _character;

        public SliceStamina(GatherRules rules, IWorldState state, IWorldClock clock, ICharacterValues character)
        {
            _rules = rules;
            _state = state;
            _character = character;
            clock.TickBoundary += _ => Recover();
        }

        /// <summary>While true, Stamina does not come back (07.2 §3.2: recovery pauses during exertion).</summary>
        public bool Exerting { get; set; }

        public double Max => (_rules.StaminaBase + (_rules.StaminaPerEndurance * _character.Get(CharacterAttribute.Endurance))) * LimitMultiplier;

        /// <summary>What is left, as a share of the maximum.</summary>
        public double Share => 1d - (Spent / 1000d);

        public double Current => Max * Share;

        public bool Empty => Spent >= 1000;

        private int Spent => Math.Max(0, Math.Min(1000, _state.Get(SpentKey)));

        /// <summary>Spends a share of the maximum, never below nothing.</summary>
        public void Spend(double share) => _state.Set(SpentKey, Math.Min(1000, Spent + (int)Math.Round(share * 1000d)));

        private void Recover()
        {
            var spent = Spent;
            if (!Exerting && spent > 0)
            {
                _state.Set(SpentKey, Math.Max(0, spent - (int)Math.Round(_rules.StaminaRecovery * 1000d)));
            }
        }
    }
}
