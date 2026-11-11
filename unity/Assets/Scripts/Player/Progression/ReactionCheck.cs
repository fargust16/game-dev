using System;

namespace GameDev.Player.Progression
{
    /// <summary>Where a Reaction Check's marker stopped (20 §3, 10.3 §6).</summary>
    public enum ReactionResult
    {
        /// <summary>Outside the good zone, or not stopped at all.</summary>
        Bad,
        Good,

        /// <summary>Inside the perfect zone, which only brewing's bars have.</summary>
        Perfect,
    }

    /// <summary>
    /// A Reaction Check's bar (20 §3, 10.3 §6, S5a.8): a marker runs once along it, from 0 to 1, in <see cref="Seconds"/>,
    /// and the player stops it. The bar has a good zone, and inside it a perfect zone that theft and a lock do not have.
    /// One bar serves theft, breaking in and S5b's brewing.
    /// </summary>
    public readonly struct ReactionBar
    {
        public ReactionBar(double goodStart, double goodWidth, double perfectStart = 0d, double perfectWidth = 0d, double seconds = ReactionCheck.Seconds)
        {
            GoodStart = goodStart;
            GoodWidth = goodWidth;
            PerfectStart = perfectStart;
            PerfectWidth = perfectWidth;
            Seconds = seconds;
        }

        public double GoodStart { get; }

        public double GoodWidth { get; }

        public double PerfectStart { get; }

        public double PerfectWidth { get; }

        /// <summary>How long the marker takes to cross the bar.</summary>
        public double Seconds { get; }

        /// <summary>The marker's place after <paramref name="elapsed"/> seconds, from 0; past 1 it has left the bar.</summary>
        public double MarkerAt(double elapsed) => Seconds > 0d ? elapsed / Seconds : 1d;

        /// <summary>The result of stopping the marker at <paramref name="marker"/>.</summary>
        public ReactionResult At(double marker)
        {
            if (marker < 0d || marker > 1d)
            {
                return ReactionResult.Bad;
            }

            if (PerfectWidth > 0d && marker >= PerfectStart && marker <= PerfectStart + PerfectWidth)
            {
                return ReactionResult.Perfect;
            }

            return marker >= GoodStart && marker <= GoodStart + GoodWidth ? ReactionResult.Good : ReactionResult.Bad;
        }
    }

    /// <summary>The numbers of a Reaction Check of theft and a lock (10.3 §6, §6.1, owner-approved 2026-11-03 and 2026-11-06).</summary>
    public static class ReactionCheck
    {
        /// <summary>The marker crosses the bar in 1.5 seconds, as in brewing.</summary>
        public const double Seconds = 1.5d;

        /// <summary>The good zone at even odds: 20 % of the bar.</summary>
        public const double EvenWidth = 0.20d;

        /// <summary>Each point of Δ widens it, or narrows it, by this factor.</summary>
        public const double Step = 1.25d;

        public const double LeastWidth = 0.05d;
        public const double MostWidth = 0.60d;

        /// <summary>An Alert observer's Perception counts × 1.25 (10.3 §3).</summary>
        public const double AlertPerception = 1.25d;

        /// <summary>
        /// Authoring default, S5a.8: no good zone begins in the bar's first fifth, so the player sees it before the marker
        /// reaches it.
        /// </summary>
        public const double Lead = 0.20d;

        /// <summary>10.3 §6: `20 % × 1.25^Δ`, held between 5 % and 60 %.</summary>
        public static double Width(double delta) => Math.Max(LeastWidth, Math.Min(MostWidth, EvenWidth * Math.Pow(Step, delta)));

        /// <summary>10.3 §6: theft from a person, `Δ = (Dexterity + Theft) / 2 − Perception`, Perception × 1.25 when Alert.</summary>
        public static double TheftWidth(int dexterity, int theft, int perception, bool alert) =>
            Width(((dexterity + theft) / 2d) - (perception * (alert ? AlertPerception : 1d)));

        /// <summary>A good zone of <paramref name="width"/> placed on the bar by <paramref name="rolls"/>, after the lead.</summary>
        public static ReactionBar Place(double width, Random rolls)
        {
            var room = Math.Max(0d, 1d - Lead - width);
            return new ReactionBar(Lead + (rolls.NextDouble() * room), width);
        }
    }
}
