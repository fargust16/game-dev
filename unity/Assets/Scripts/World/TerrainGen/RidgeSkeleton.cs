using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 3B: the primary spur skeleton, and the valleys it leaves between the spurs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first slice of Stage 3 that produces geography rather than a frame to put geography in. It
    /// still changes no height: what comes out is an explicit structural network - ridges descending
    /// each flank, and the corridors between them - which a later slice will carve into the surface.
    /// </para>
    /// <para>
    /// It is built on Stage 3A and asks for nothing else. Every attachment comes from
    /// <see cref="MassifFlank.AttachmentInterval"/> and every sample of every spur is resolved through
    /// <see cref="MassifOwnership.At"/>, so the geographic rules are not a checklist the builder
    /// follows - they are the only answers it can get.
    /// </para>
    /// <para>
    /// Depth 1 only. The main crest is Stage 2's <see cref="MacroAxis"/> and is referred to, never
    /// copied; secondary spurs are a later slice and nothing here anticipates them.
    /// </para>
    /// </remarks>
    public sealed class RidgeSkeleton
    {
        public int Seed { get; }

        public RidgeSettings Settings { get; }

        /// <summary>The Stage 3A partition this was built on. Unchanged by this stage.</summary>
        public Stage3ASkeleton Ownership { get; }

        /// <summary>Primary spurs, massif-major then flank-major then ascending arc.</summary>
        public IReadOnlyList<SpurCurve> Spurs { get; }

        /// <summary>Valley corridors, one more than there are spurs on each flank.</summary>
        public IReadOnlyList<ValleyCorridor> Valleys { get; }

        public RidgeSkeletonStats Stats { get; }

        public RidgeSkeleton(
            int seed,
            RidgeSettings settings,
            Stage3ASkeleton ownership,
            IReadOnlyList<SpurCurve> spurs,
            IReadOnlyList<ValleyCorridor> valleys,
            RidgeSkeletonStats stats)
        {
            Seed = seed;
            Settings = settings;
            Ownership = ownership;
            Spurs = spurs;
            Valleys = valleys;
            Stats = stats;
        }

        /// <summary>Builds the spur skeleton over a finished Stage 3A partition.</summary>
        public static RidgeSkeleton Build(Stage3ASkeleton skeleton) =>
            Build(skeleton, RidgeSettings.Default);

        public static RidgeSkeleton Build(Stage3ASkeleton skeleton, RidgeSettings settings) =>
            Build(skeleton, settings, new Seed.WorldSeed(skeleton.Seed));

        /// <summary>
        /// Builds the spur skeleton with an explicit seed.
        /// </summary>
        /// <remarks>
        /// The overload the fixtures use. Stage 3A carries the world's seed as a record rather than as
        /// a generator, so the stream this stage draws from has to be handed in for a hand-built
        /// world, which has no seed of its own.
        /// </remarks>
        public static RidgeSkeleton Build(
            Stage3ASkeleton skeleton,
            RidgeSettings settings,
            Seed.IWorldSeed worldSeed,
            Stage1Result stage1 = null)
        {
            settings.Clamp();
            var sw = Stopwatch.StartNew();

            var spurs = new List<SpurCurve>();
            var valleys = new List<ValleyCorridor>();
            var stats = new RidgeSkeletonStats
            {
                Seed = skeleton.Seed,
                MassifCount = skeleton.Massifs.Count,
                FlankCount = skeleton.Flanks.Count,
            };

            PrimarySpurBuilder.Build(skeleton, settings, worldSeed, spurs, valleys, stats);

            // The corridors become trajectories here rather than in the carve, so the geometry exists
            // once and every consumer - the field, the tests, the diagnostics - reads the same curve.
            // Without a Stage 1 result there is no island surface to continue down, so a caller that
            // only wants the spur topology gets the massif half and no continuation.
            if (stage1 != null)
            {
                ValleyTrajectoryBuilder.Build(skeleton, spurs, valleys, stage1, settings);
            }

            stats.SpurCount = spurs.Count;
            stats.ValleyCount = valleys.Count;
            stats.MinSpurLengthMeters = float.PositiveInfinity;
            stats.MinStripArcMeters = float.PositiveInfinity;
            stats.MinValleyArcMeters = float.PositiveInfinity;

            for (var i = 0; i < spurs.Count; i++)
            {
                var spur = spurs[i];
                var massif = skeleton.Massifs[spur.MassifIndex];

                stats.SpurPointCount += spur.Points.Length;
                stats.TotalSpurLengthMeters += spur.LengthMeters;
                stats.MinSpurLengthMeters = Mathf.Min(stats.MinSpurLengthMeters, spur.LengthMeters);
                stats.MaxSpurLengthMeters = Mathf.Max(stats.MaxSpurLengthMeters, spur.LengthMeters);

                var strip = (spur.StripInterval.y - spur.StripInterval.x) * massif.LengthMeters;
                stats.MinStripArcMeters = Mathf.Min(stats.MinStripArcMeters, strip);
                stats.MaxStripArcMeters = Mathf.Max(stats.MaxStripArcMeters, strip);

                if (spur.SelfIntersects())
                {
                    stats.SelfIntersectingSpurs++;
                }

                if (spur.Truncated)
                {
                    stats.TruncatedSpurs++;
                }
            }

            for (var i = 0; i < valleys.Count; i++)
            {
                var valley = valleys[i];
                var massif = skeleton.Massifs[valley.MassifIndex];
                stats.MinValleyArcMeters = Mathf.Min(
                    stats.MinValleyArcMeters, valley.SWidth * massif.LengthMeters);
            }

            // Crossings across the whole world, not only within a flank: two spurs that belong to
            // different flanks or different massifs are supposed to be separated by the crest and by
            // the medial divide, and this is where that stops being an argument.
            for (var i = 0; i < spurs.Count; i++)
            {
                for (var j = i + 1; j < spurs.Count; j++)
                {
                    if (spurs[i].Crosses(spurs[j]))
                    {
                        stats.CrossingSpurPairs++;
                    }
                }
            }

            stats.BuildMs = sw.ElapsedMilliseconds;
            return new RidgeSkeleton(skeleton.Seed, settings, skeleton, spurs, valleys, stats);
        }

        /// <summary>The spurs of one massif and flank, ascending by attachment.</summary>
        public List<SpurCurve> SpursOn(int massifIndex, int flankSign)
        {
            var result = new List<SpurCurve>();
            for (var i = 0; i < Spurs.Count; i++)
            {
                if (Spurs[i].MassifIndex == massifIndex && Spurs[i].FlankSign == flankSign)
                {
                    result.Add(Spurs[i]);
                }
            }

            return result;
        }

        /// <summary>The valley corridors of one massif and flank, ascending by arc.</summary>
        public List<ValleyCorridor> ValleysOn(int massifIndex, int flankSign)
        {
            var result = new List<ValleyCorridor>();
            for (var i = 0; i < Valleys.Count; i++)
            {
                if (Valleys[i].MassifIndex == massifIndex && Valleys[i].FlankSign == flankSign)
                {
                    result.Add(Valleys[i]);
                }
            }

            return result;
        }
    }

    /// <summary>What Stage 3B measured about itself.</summary>
    public sealed class RidgeSkeletonStats
    {
        public int Seed;
        public int MassifCount;
        public int FlankCount;

        /// <summary>Stations the spacing rule placed, before any were found unbuildable.</summary>
        public int StationsPlaced;

        /// <summary>Stations whose spur could not reach two samples. Expected zero.</summary>
        public int StationsWithoutASpur;

        public int SpurCount;
        public int ValleyCount;
        public int SpurPointCount;

        public float TotalSpurLengthMeters;
        public float MinSpurLengthMeters = float.PositiveInfinity;
        public float MaxSpurLengthMeters;

        /// <summary>Arc length of the shortest and longest strip a spur commands, metres.</summary>
        public float MinStripArcMeters = float.PositiveInfinity;

        public float MaxStripArcMeters;

        /// <summary>Arc width of the narrowest valley, metres.</summary>
        public float MinValleyArcMeters = float.PositiveInfinity;

        /// <summary>Spurs a boundary stopped short of their intended reach.</summary>
        public int TruncatedSpurs;

        /// <summary>Spurs whose own polyline crosses itself. Expected zero.</summary>
        public int SelfIntersectingSpurs;

        /// <summary>Pairs of spurs that cross. Expected zero.</summary>
        public int CrossingSpurPairs;

        /// <summary>How many spurs stopped for each reason, indexed by <see cref="SpurTermination"/>.</summary>
        public readonly int[] Terminations = new int[6];

        public long BuildMs;

        public float MeanSpurLengthMeters =>
            SpurCount > 0 ? TotalSpurLengthMeters / SpurCount : 0f;

        public float MeanPointsPerSpur => SpurCount > 0 ? SpurPointCount / (float)SpurCount : 0f;

        public string ToLine()
        {
            var culture = CultureInfo.InvariantCulture;
            var shortest = float.IsPositiveInfinity(MinSpurLengthMeters)
                ? "n/a"
                : MinSpurLengthMeters.ToString("F0", culture);
            var narrowest = float.IsPositiveInfinity(MinValleyArcMeters)
                ? "n/a"
                : MinValleyArcMeters.ToString("F0", culture);

            return $"seed {Seed}: massifs={MassifCount} spurs={SpurCount} valleys={ValleyCount}"
                   + $" len=[{shortest},{MaxSpurLengthMeters.ToString("F0", culture)}]m"
                   + $" mean={MeanSpurLengthMeters.ToString("F0", culture)}m"
                   + $" pts/spur={MeanPointsPerSpur.ToString("F1", culture)}"
                   + $" minValleyArc={narrowest}m"
                   + $" truncated={TruncatedSpurs} noSpur={StationsWithoutASpur}"
                   + $" selfCross={SelfIntersectingSpurs} cross={CrossingSpurPairs}"
                   + $" build={BuildMs}ms";
        }

        public string TerminationLine()
        {
            return $"terminations: toe={Terminations[(int)SpurTermination.Toe]}"
                   + $" coast={Terminations[(int)SpurTermination.Coast]}"
                   + $" domain={Terminations[(int)SpurTermination.Domain]}"
                   + $" axisEnd={Terminations[(int)SpurTermination.AxisEnd]}"
                   + $" divide={Terminations[(int)SpurTermination.Divide]}"
                   + $" unclaimed={Terminations[(int)SpurTermination.Unclaimed]}";
        }
    }
}
