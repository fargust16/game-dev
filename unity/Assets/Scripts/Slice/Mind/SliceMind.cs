using System;
using GameDev.Confront.Rules;
using GameDev.World.State;
using GameDev.World.Timekeeping;

namespace GameDev.Slice.Mind
{
    /// <summary>07.2 §3.4's bands of Sanity.</summary>
    public enum SanityBand
    {
        /// <summary>100.</summary>
        Stable,

        /// <summary>70–99.</summary>
        Mild,

        /// <summary>40–69.</summary>
        Significant,

        /// <summary>10–39.</summary>
        Severe,

        /// <summary>1–9.</summary>
        Critical,

        /// <summary>0: the run ends.</summary>
        Lost,
    }

    /// <summary>07.2 §3.3's Fatigue.</summary>
    public enum FatigueState
    {
        Rested,

        /// <summary>From 18 hours awake.</summary>
        Tired,

        /// <summary>From 30 hours awake.</summary>
        VeryTired,
    }

    /// <summary>
    /// The character's mind in the slice (S5b.6; 07.2 §3.3 *Fatigue*, *Full exhaustion*, §3.4 *Sanity in the slice*), with no
    /// scene code: Sanity's band and Fatigue, and what they take away, each band keeping the penalties of those above it
    /// where it names none of its own; full exhaustion's Sanity damage and its Will XP, once an episode that spent 30 % of
    /// the maximum; and the end of the run at Sanity 0. The hours awake and the episode's spending are kept in the world
    /// state, so a save keeps them. Every number is an authoring default of 07.2.
    /// </summary>
    public sealed class SliceMind
    {
        /// <summary>World state: the minutes awake since the last full sleep; a run starts rested.</summary>
        public const string AwakeKey = "player.awake";

        /// <summary>World state: the thousandths of maximum Will Power spent since the last full exhaustion.</summary>
        public const string EpisodeKey = "player.wp.episode";

        public const int TiredHours = 18;
        public const int VeryTiredHours = 30;

        /// <summary>07.2 §3.3: a sleep of 6 hours or more clears Fatigue; a shorter one takes off two hours of it an hour.</summary>
        public const int FullSleepMinutes = 6 * 60;

        /// <summary>07.2 §3.4: Sanity a full sleep gives back.</summary>
        public const int SleepSanity = 3;

        /// <summary>07.2 §3.4: D₀, the base Sanity damage of full exhaustion.</summary>
        public const double ExhaustionDamage = 5d;

        /// <summary>07.2 §3.3: the share of maximum Will Power an episode must spend to earn Will's XP, in thousandths.</summary>
        public const int EpisodeShare = 300;

        private readonly IWorldState _state;
        private readonly SliceSanity _sanity;

        public SliceMind(IWorldState state, IWorldClock clock, SliceSanity sanity)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _sanity = sanity ?? throw new ArgumentNullException(nameof(sanity));
            if (clock != null)
            {
                clock.TickBoundary += _ => Waking();
            }

            _sanity.Changed += value =>
            {
                if (value <= 0)
                {
                    Lost?.Invoke();
                }
            };
        }

        /// <summary>Sanity reached 0: the character is no longer the player's, and the run ends (07.2 §3.4).</summary>
        public event Action Lost;

        public SliceSanity Sanity => _sanity;

        /// <summary>While true the world's ticks are sleep, not waking time (S5c.2).</summary>
        public bool Asleep { get; set; }

        public int MinutesAwake => Math.Max(0, _state.Get(AwakeKey));

        public FatigueState Fatigue => FatigueOf(MinutesAwake);

        public SanityBand Band => BandOf(_sanity.Value);

        public static SanityBand BandOf(int sanity) =>
            sanity >= 100 ? SanityBand.Stable
            : sanity >= 70 ? SanityBand.Mild
            : sanity >= 40 ? SanityBand.Significant
            : sanity >= 10 ? SanityBand.Severe
            : sanity >= 1 ? SanityBand.Critical
            : SanityBand.Lost;

        public static FatigueState FatigueOf(int minutesAwake) =>
            minutesAwake >= VeryTiredHours * 60 ? FatigueState.VeryTired
            : minutesAwake >= TiredHours * 60 ? FatigueState.Tired
            : FatigueState.Rested;

        // The bands' numbers, Stable to Lost (07.2 §3.4); Lost keeps Critical's while the run ends.
        private static readonly int[] BandHit = { 0, 0, -5, -10, -15, -15 };
        private static readonly int[] BandCheck = { 0, 0, -1, -2, -3, -3 };
        private static readonly float[] BandWillPower = { 0f, 0f, 0f, -0.25f, -0.5f, -0.5f };

        // Fatigue's, Rested to Very tired (07.2 §3.3).
        private static readonly int[] FatigueHit = { 0, -5, -10 };
        private static readonly double[] FatigueZones = { 1d, 0.8d, 0.6d };

        /// <summary>Percentage points to hit, Sanity's and Fatigue's together (07.2 §3.4: they add up).</summary>
        public int HitPenalty => BandHit[(int)Band] + FatigueHit[(int)Fatigue];

        /// <summary>Points on the preparation of every Action Check of 07.1.</summary>
        public int CheckPenalty => BandCheck[(int)Band];

        /// <summary>Points on every Social Check: Sanity's, and −1 Very tired.</summary>
        public int SocialPenalty => BandCheck[(int)Band] + (Fatigue == FatigueState.VeryTired ? -1 : 0);

        /// <summary>The share a Reaction Check's zones keep: half at Critical, four fifths Tired and three fifths Very tired.</summary>
        public double ReactionShare => (Band >= SanityBand.Critical ? 0.5d : 1d) * FatigueZones[(int)Fatigue];

        /// <summary>Maximum Will Power's share lost, −25 % Severe and −50 % Critical, added to the items' share (07.2 §3.3).</summary>
        public float WillPowerShift => BandWillPower[(int)Band];

        /// <summary>What a fighter made of the character takes from its mind: the points to hit and the share of Will Power.</summary>
        public FightModifiers FightShift => new FightModifiers { accuracy = HitPenalty, willPower = WillPowerShift };

        /// <summary>Stamina's return: half Tired or Very tired.</summary>
        public double StaminaShare => Fatigue == FatigueState.Rested ? 1d : 0.5d;

        /// <summary>
        /// Whether Will Power comes back on <paramref name="tick"/>: never Very tired, three ticks in four below Stable (its
        /// three-quarter speed), every tick otherwise.
        /// </summary>
        public bool WillPowerReturns(long tick) =>
            Fatigue != FatigueState.VeryTired && (Band == SanityBand.Stable || (tick % 4) != 0);

        private void Waking()
        {
            if (!Asleep)
            {
                _state.Set(AwakeKey, MinutesAwake + 1);
            }
        }

        /// <summary>
        /// A sleep of <paramref name="minutes"/> ended (07.2 §3.3–3.4): 6 hours or more clears Fatigue and gives back
        /// 3 Sanity; a shorter one takes off two minutes awake for each minute slept.
        /// </summary>
        public void Slept(int minutes)
        {
            if (minutes >= FullSleepMinutes)
            {
                _state.Set(AwakeKey, 0);
                _sanity.Restore(SleepSanity);
                return;
            }

            _state.Set(AwakeKey, Math.Max(0, MinutesAwake - (2 * Math.Max(0, minutes))));
        }

        /// <summary>Will Power spent by an ability or Infusion, counted toward the episode's share.</summary>
        public void Spent(double amount, double max)
        {
            if (amount > 0d && max > 0d)
            {
                _state.Set(EpisodeKey, Math.Min(1_000_000, _state.Get(EpisodeKey) + (int)Math.Round(1000d * amount / max)));
            }
        }

        /// <summary>
        /// Will Power ran out through its use (07.2 §3.3–3.4): Sanity takes `D₀ × S × 100 / (100 + Will)`, rounded; Will's XP
        /// is earned if the episode spent at least 30 % of the maximum; the next episode starts from nothing.
        /// </summary>
        public (int Damage, bool Reward) Exhaust(int will, double limitMultiplier)
        {
            var damage = ExhaustionDamageOf(will, limitMultiplier);
            var reward = _state.Get(EpisodeKey) >= EpisodeShare;
            _state.Set(EpisodeKey, 0);
            _sanity.Lose(damage);
            return (damage, reward);
        }

        public static int ExhaustionDamageOf(int will, double limitMultiplier) =>
            (int)Math.Round(ExhaustionDamage * limitMultiplier * 100d / (100d + Math.Max(0, will)), MidpointRounding.AwayFromZero);

        /// <summary>The string table key of a band's words, for the character page.</summary>
        public static string BandKey(SanityBand band) => "ui.mind.band." + band.ToString().ToLowerInvariant();

        /// <summary>The string table key of Fatigue's words, or null when rested.</summary>
        public static string FatigueKey(FatigueState fatigue) =>
            fatigue == FatigueState.Rested ? null : "ui.mind.fatigue." + fatigue.ToString().ToLowerInvariant();
    }
}
