using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3A: who owns which ground, which flank it is on, and where two massifs meet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first slice of Stage 3, and deliberately the one that changes no height. Stage 2 raised
    /// the massif; Stage 3 will carve ridges and valleys into it; this slice only establishes the
    /// frame that carving happens in, so that when the carving arrives it has one reading of the
    /// geometry to work against rather than several.
    /// </para>
    /// <para>
    /// Three things come out of it and nothing else: a <see cref="MassifOwnership"/> partition of the
    /// land, two <see cref="MassifFlank"/>s per massif, and an <see cref="InterMassifDivide"/> per
    /// pair of massifs whose territories meet. There are no spurs, no valleys, no carving and no
    /// basins here, and no placeholder for them - a field nothing reads is a decision made before it
    /// was measured.
    /// </para>
    /// <para>
    /// The crest is <em>referenced</em>, never copied. The main divide of a massif is its
    /// <see cref="MacroAxis"/>, which Stage 2 owns; this slice stores the massif and reads the axis
    /// through it, so there is exactly one curve in the world that is the divide.
    /// </para>
    /// <para>
    /// Deterministic without needing to be: Stage 3A draws no seed stream at all, because every
    /// answer is a geometric consequence of the axes and the coastline it was given.
    /// </para>
    /// </remarks>
    public sealed class Stage3ASkeleton
    {
        public int Seed { get; }

        public RidgeSettings Settings { get; }

        /// <summary>The Stage 2B massifs this skeleton was built over, in their own order.</summary>
        public IReadOnlyList<MountainMassif> Massifs { get; }

        /// <summary>The land partition, queryable at any world coordinate.</summary>
        public MassifOwnership Ownership { get; }

        /// <summary>Two per massif. Empty on a world without one.</summary>
        public IReadOnlyList<MassifFlank> Flanks => Ownership.Flanks;

        /// <summary>Medial divides, one per pair of massifs whose territories meet on land.</summary>
        public IReadOnlyList<InterMassifDivide> Divides { get; }

        public Stage3AStats Stats { get; }

        public Stage3ASkeleton(
            int seed,
            RidgeSettings settings,
            IReadOnlyList<MountainMassif> massifs,
            MassifOwnership ownership,
            IReadOnlyList<InterMassifDivide> divides,
            Stage3AStats stats)
        {
            Seed = seed;
            Settings = settings;
            Massifs = massifs;
            Ownership = ownership;
            Divides = divides;
            Stats = stats;
        }

        /// <summary>Builds the skeleton over a finished Stage 2B world.</summary>
        public static Stage3ASkeleton Build(Stage2BResult stage2b) =>
            Build(stage2b, RidgeSettings.Default);

        public static Stage3ASkeleton Build(Stage2BResult stage2b, RidgeSettings settings) =>
            Build(stage2b.Seed, stage2b.Massifs, stage2b.Stage1.Coast, settings);

        /// <summary>
        /// Builds the skeleton over any set of massifs and coastline.
        /// </summary>
        /// <remarks>
        /// The overload that takes the parts rather than a <see cref="Stage2BResult"/> exists so the
        /// multi-massif path can be exercised. Stage 2A caps a world at two systems and, over the
        /// twenty-nine worlds audited, never produced more than one, so the only way to test what
        /// happens between two massifs is to hand the builder two.
        /// </remarks>
        public static Stage3ASkeleton Build(
            int seed,
            IReadOnlyList<MountainMassif> massifs,
            CoastQuery coast,
            RidgeSettings settings)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            massifs ??= System.Array.Empty<MountainMassif>();
            var ownership = new MassifOwnership(massifs, coast, settings);
            var divides = InterMassifDivideBuilder.Build(massifs, coast, settings);

            var stats = new Stage3AStats
            {
                Seed = seed,
                MassifCount = massifs.Count,
                FlankCount = ownership.Flanks.Count,
                DivideCount = divides.Count,
                MinDivideAxisDistanceMeters = float.PositiveInfinity,
            };

            for (var i = 0; i < divides.Count; i++)
            {
                var divide = divides[i];
                stats.DivideLengthMeters += divide.LengthMeters;
                stats.DivideSampleCount += divide.Points.Length;
                stats.MaxDivideImbalanceMeters =
                    Mathf.Max(stats.MaxDivideImbalanceMeters, divide.MaxImbalanceMeters);
                stats.MinDivideAxisDistanceMeters =
                    Mathf.Min(stats.MinDivideAxisDistanceMeters, divide.MinAxisDistanceMeters);

                if (!divide.TerminatesCleanly)
                {
                    stats.UnresolvedDivideEnds +=
                        (divide.StartTermination == DivideTermination.Unresolved ? 1 : 0)
                        + (divide.EndTermination == DivideTermination.Unresolved ? 1 : 0);
                }

                if (divide.SelfIntersects())
                {
                    stats.SelfIntersectingDivides++;
                }
            }

            stats.BuildMs = sw.ElapsedMilliseconds;
            return new Stage3ASkeleton(seed, settings, massifs, ownership, divides, stats);
        }
    }

    /// <summary>What Stage 3A measured about itself.</summary>
    public sealed class Stage3AStats
    {
        public int Seed;
        public int MassifCount;
        public int FlankCount;
        public int DivideCount;
        public int DivideSampleCount;
        public float DivideLengthMeters;

        /// <summary>Worst departure of a divide from true equidistance, metres. Zero is exact.</summary>
        public float MaxDivideImbalanceMeters;

        /// <summary>Closest any divide comes to any axis, metres. Positive means none crossed one.</summary>
        public float MinDivideAxisDistanceMeters = float.PositiveInfinity;

        /// <summary>Divide arms that stopped without reaching a boundary. Expected zero.</summary>
        public int UnresolvedDivideEnds;

        /// <summary>Divides whose polyline crosses itself. Expected zero.</summary>
        public int SelfIntersectingDivides;

        public long BuildMs;

        public string ToLine()
        {
            var culture = CultureInfo.InvariantCulture;
            var closest = float.IsPositiveInfinity(MinDivideAxisDistanceMeters)
                ? "n/a"
                : MinDivideAxisDistanceMeters.ToString("F1", culture);

            return $"seed {Seed}: massifs={MassifCount} flanks={FlankCount}"
                   + $" divides={DivideCount} divideLen={DivideLengthMeters.ToString("F0", culture)}m"
                   + $" imbalance={MaxDivideImbalanceMeters.ToString("F3", culture)}m"
                   + $" nearestAxis={closest}m"
                   + $" unresolved={UnresolvedDivideEnds} selfCross={SelfIntersectingDivides}"
                   + $" build={BuildMs}ms";
        }
    }
}
