using System;
using GameDev.Player.Progression;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Gathering
{
    /// <summary>
    /// The character's Stamina (07.2 §3.2; S5a.5; the owner's play test of S5a): its maximum is (20 + 1.6 × Endurance) × S,
    /// with S = 1 until S5b's Limits. The sprint, gathering and Item Creation spend it; without exertion it comes back at
    /// 20 % of the maximum a world tick (2 % a real second in the slice), spread over the tick's real time, and none while
    /// the world is paused. Run out, the character rests 2.5 real seconds before it begins to come back, and stays tired,
    /// unable to sprint or begin work,
    /// until a tenth has come back; work stopped before the pool ran out lets it come back at once. The spent share is
    /// kept in the world state in millionths, so it lives with the run.
    /// </summary>
    public sealed class SliceStamina
    {
        /// <summary>World state: the share of maximum Stamina spent, in millionths.</summary>
        public const string SpentKey = "player.stamina.used";

        /// <summary>07.2 §2: the Limit multiplier S; 1 for a character without an Arcana, which S5b changes.</summary>
        public const double LimitMultiplier = 1d;

        /// <summary>Authoring default, 07.2 §3.2: the sprint spends this share of the maximum each real second.</summary>
        public const double SprintShare = 0.02d;

        /// <summary>Authoring default, 07.2 §3.2: real seconds of rest after the pool runs out, before it comes back.</summary>
        public const double RestSeconds = 2.5d;

        /// <summary>Authoring default, 07.2 §3.2: the share that must come back before a tired character works or sprints again.</summary>
        public const double RestedShare = 0.10d;

        private const double Whole = 1_000_000d;

        private readonly GatherRules _rules;
        private readonly IWorldState _state;
        private readonly IWorldClock _clock;
        private readonly ICharacterValues _character;
        private readonly double _secondsPerTick;
        private double _last;
        private double _restUntil;
        private bool _exerting;
        private bool _sprinting;
        private bool _spentOut;

        /// <param name="secondsPerTick">Real seconds a world tick lasts, which sets the rest in ticks; 10 in the slice.</param>
        public SliceStamina(GatherRules rules, IWorldState state, IWorldClock clock, ICharacterValues character, double secondsPerTick = 10d)
        {
            _rules = rules;
            _state = state;
            _clock = clock;
            _character = character;
            _secondsPerTick = secondsPerTick > 0d ? secondsPerTick : 10d;
            _last = Now;
            clock.TickBoundary += _ => CatchUp();
        }

        /// <summary>The run's own reckoning beside the kept share, for a Suspend Save (17 X.3): the last tick counted, the rest's end, and whether it ran out.</summary>
        public string Capture() => FormattableString.Invariant($"{_last:R};{_restUntil:R};{(_spentOut ? 1 : 0)}");

        public void Restore(string payload)
        {
            var parts = (payload ?? string.Empty).Split(';');
            if (parts.Length == 3
                && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var last)
                && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var rest))
            {
                _last = last;
                _restUntil = rest;
                _spentOut = parts[2] == "1";
                _exerting = false;
                _sprinting = false;
            }
        }

        /// <summary>While true, Stamina does not come back (07.2 §3.2: recovery pauses during exertion).</summary>
        public bool Exerting
        {
            get => _exerting;
            set
            {
                CatchUp();
                _exerting = value;
            }
        }

        /// <summary>While true the character sprints, and Stamina does not come back.</summary>
        public bool Sprinting
        {
            get => _sprinting;
            set
            {
                CatchUp();
                _sprinting = value;
            }
        }

        public double Max => (_rules.StaminaBase + (_rules.StaminaPerEndurance * _character.Get(CharacterAttribute.Endurance))) * LimitMultiplier;

        /// <summary>What is left, as a share of the maximum.</summary>
        public double Share => 1d - (Spent / Whole);

        public double Current => Max * Share;

        public bool Empty => Spent >= Whole;

        /// <summary>From the moment the pool ran out until a tenth has come back: no sprint and no new work.</summary>
        public bool Tired
        {
            get
            {
                if (_spentOut && Share >= RestedShare - 1e-9)
                {
                    _spentOut = false;
                }

                return _spentOut;
            }
        }

        /// <summary>World time now, in ticks with their fraction, which stands still while the world is paused.</summary>
        private double Now => _clock.Tick + _clock.TickProgress;

        private double Spent => Math.Max(0d, Math.Min(Whole, _state.Get(SpentKey)));

        /// <summary>Spends a share of the maximum, never below nothing; running out begins the rest.</summary>
        public void Spend(double share)
        {
            CatchUp();
            var spent = Math.Min(Whole, Spent + Math.Round(share * Whole));
            _state.Set(SpentKey, (int)spent);
            if (spent >= Whole && !_spentOut)
            {
                _spentOut = true;
                _restUntil = Now + (RestSeconds / _secondsPerTick);
            }
        }

        /// <summary>Gives back what the world time since the last reckoning returns; the HUD calls it each frame.</summary>
        public void CatchUp()
        {
            var now = Now;
            var from = Math.Max(_last, _restUntil);
            if (!_exerting && !_sprinting && now > from)
            {
                var spent = Spent;
                if (spent > 0d)
                {
                    _state.Set(SpentKey, (int)Math.Max(0d, Math.Round(spent - ((now - from) * _rules.StaminaRecovery * Whole))));
                }
            }

            // A clock put back, as a loaded save does, reckons from its new time.
            _last = now;
            if (_restUntil > now + (RestSeconds / _secondsPerTick))
            {
                _restUntil = now;
            }
        }
    }
}
