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
    /// One bar serves every use of 07.1 §8a: theft, a lock, a gathering cycle, Item Creation and S5b's brewing.
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

    /// <summary>
    /// The numbers of every Reaction Check (07.1 §8a): the good zone's width and the marker's pace by
    /// <c>Δ = (attribute + skill) / 2 − difficulty</c>, the weaker the narrower and the faster, with a widest zone and a
    /// slowest marker so the best case stays a challenge (the owner's play test of S5a, 2026-11-11).
    /// </summary>
    public static class ReactionCheck
    {
        /// <summary>The marker's slowest crossing, at even odds and above: 1.5 seconds, as in brewing.</summary>
        public const double Seconds = 1.5d;

        /// <summary>Authoring default, 07.1 §8a: the marker's fastest crossing, s.</summary>
        public const double FastestSeconds = 0.6d;

        /// <summary>Authoring default, 07.1 §8a: each point of Δ below even odds speeds the marker by this factor.</summary>
        public const double PaceStep = 1.15d;

        /// <summary>The good zone at even odds: 20 % of the bar.</summary>
        public const double EvenWidth = 0.20d;

        /// <summary>Each point of Δ widens it, or narrows it, by this factor.</summary>
        public const double Step = 1.25d;

        public const double LeastWidth = 0.05d;

        /// <summary>07.1 §8a: 35 % since the owner's play test of S5a (60 % before), an authoring default.</summary>
        public const double MostWidth = 0.35d;

        /// <summary>Authoring default, 07.1 §8a: a perfect zone is this share of the good zone, in its middle.</summary>
        public const double PerfectShare = 0.25d;

        /// <summary>An Alert observer's Perception counts × 1.25 (10.3 §3).</summary>
        public const double AlertPerception = 1.25d;

        /// <summary>
        /// Authoring default, S5a.8: no good zone begins in the bar's first fifth, so the player sees it before the marker
        /// reaches it.
        /// </summary>
        public const double Lead = 0.20d;

        /// <summary>07.1 §8a: `20 % × 1.25^Δ`, held between 5 % and 35 %.</summary>
        public static double Width(double delta) => Math.Max(LeastWidth, Math.Min(MostWidth, EvenWidth * Math.Pow(Step, delta)));

        /// <summary>07.1 §8a: the marker crosses in `1.5 s × 1.15^Δ`, held between 0.6 s and 1.5 s.</summary>
        public static double Pace(double delta) => Math.Max(FastestSeconds, Math.Min(Seconds, Seconds * Math.Pow(PaceStep, delta)));

        /// <summary>07.1 §4's Δ of a Reaction Check: the mean of attribute and skill against the difficulty.</summary>
        public static double Delta(int attribute, int skill, double difficulty) => ((attribute + skill) / 2d) - difficulty;

        /// <summary>10.3 §6: theft from a person, `Δ = (Dexterity + Theft) / 2 − Perception`, Perception × 1.25 when Alert.</summary>
        public static double TheftDelta(int dexterity, int theft, int perception, bool alert) =>
            Delta(dexterity, theft, perception * (alert ? AlertPerception : 1d));

        public static double TheftWidth(int dexterity, int theft, int perception, bool alert) => Width(TheftDelta(dexterity, theft, perception, alert));

        /// <summary>
        /// The bar of a check at <paramref name="delta"/>: its good zone's width and its marker's pace by 07.1 §8a, the zone
        /// placed by <paramref name="rolls"/>, and a perfect zone in its middle when the use has one.
        /// </summary>
        public static ReactionBar For(double delta, Random rolls, bool perfect = false)
        {
            var bar = Place(Width(delta), rolls, Pace(delta));
            if (!perfect)
            {
                return bar;
            }

            var inner = bar.GoodWidth * PerfectShare;
            return new ReactionBar(bar.GoodStart, bar.GoodWidth, bar.GoodStart + ((bar.GoodWidth - inner) / 2d), inner, bar.Seconds);
        }

        /// <summary>A good zone of <paramref name="width"/> placed on the bar by <paramref name="rolls"/>, after the lead.</summary>
        public static ReactionBar Place(double width, Random rolls, double seconds = Seconds)
        {
            var room = Math.Max(0d, 1d - Lead - width);
            return new ReactionBar(Lead + (rolls.NextDouble() * room), width, seconds: seconds);
        }
    }
}
