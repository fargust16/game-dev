using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// Stage 2A: the macro structural layer and the mountain axes drawn from it, with no height.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A feasibility pass, not a stage. Stage 2 produced belts whose axes came from the fine crustal
    /// boundaries, which bend on the same scale as the belts are wide; capping the width for
    /// correctness turned them into fins, and not capping it folded them into domes. Neither is a
    /// tuning problem, so this pass replaces the source of the axes and measures the result before
    /// anything is built on it.
    /// </para>
    /// <para>
    /// Runs the Stage 1 pipeline only as far as the coastline. The 1025 x 1025 height field is five to
    /// seven seconds a seed and answers nothing about geometry, so it is not evaluated here; the
    /// coast rings and the point-in-polygon query are the whole of what an axis needs from Stage 1.
    /// </para>
    /// </remarks>
    public sealed class Stage2AResult
    {
        public int Seed { get; }
        public TerrainGenSettings TerrainSettings { get; }
        public MacroStructureSettings MacroSettings { get; }

        /// <summary>Stage 1's fine crustal partition, rebuilt exactly as Stage 1 builds it.</summary>
        public CrustalPartition Partition { get; }

        public LandmassBuilder.Result Landmass { get; }
        public WorldStructure Structure { get; }
        public CoastQuery Coast { get; }

        public MacroPlates Plates { get; }

        /// <summary>Every axis the pass produced, longest first, accepted and rejected alike.</summary>
        public IReadOnlyList<MacroAxis> Candidates { get; }

        public MacroAxisBuilder.Report BuildReport { get; }
        public Stage2AStats Stats { get; }

        public Stage2AResult(
            int seed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            CrustalPartition partition,
            LandmassBuilder.Result landmass,
            WorldStructure structure,
            CoastQuery coast,
            MacroPlates plates,
            IReadOnlyList<MacroAxis> candidates,
            MacroAxisBuilder.Report buildReport,
            Stage2AStats stats)
        {
            Seed = seed;
            TerrainSettings = terrainSettings;
            MacroSettings = macroSettings;
            Partition = partition;
            Landmass = landmass;
            Structure = structure;
            Coast = coast;
            Plates = plates;
            Candidates = candidates;
            BuildReport = buildReport;
            Stats = stats;
        }

        public List<MacroAxis> AcceptedAxes()
        {
            var accepted = new List<MacroAxis>();
            for (var i = 0; i < Candidates.Count; i++)
            {
                if (Candidates[i].Accepted)
                {
                    accepted.Add(Candidates[i]);
                }
            }

            return accepted;
        }
    }

    /// <summary>Everything a Stage 2A run can be judged by without opening a scene.</summary>
    public sealed class Stage2AStats
    {
        public int Seed;

        public int FineSiteCount;
        public int MacroPlateCount;
        public int MacroPlatesInWorld;
        public int MacroPlatesOnLand;
        public int MacroBoundaryCount;
        public int MacroJunctionCount;
        public float MeanBoundaryLengthMeters;
        public float MinBoundaryLengthMeters;
        public float MaxBoundaryLengthMeters;

        public int ConvergentBoundaries;
        public int OnLandBoundaries;
        public int ChainsDiscovered;
        public int ChainsAccepted;
        public int FilletRelaxations;
        public int FilletSplits;

        public int CandidateCount;
        public int AcceptedCount;

        public float LongestAxisMeters;
        public float TotalAcceptedAxisMeters;
        public float WorstReachMeters;
        public float WorstUsableHalfWidthMeters;
        public float WorstLengthToWidthRatio;
        public float MinSystemSeparationMeters;
        public int SelfIntersectingCandidates;

        public float RequiredReachMeters;

        public long PartitionMs;
        public long LandmassMs;
        public long PlatesMs;
        public long AxesMs;
        public long TotalMs;

        public string ToLine()
        {
            var c = CultureInfo.InvariantCulture;
            return string.Join(" ", new[]
            {
                $"seed={Seed}",
                $"plates={MacroPlatesOnLand}land/{MacroPlatesInWorld}world/{MacroPlateCount}",
                $"boundaries={MacroBoundaryCount}",
                $"convergent={ConvergentBoundaries}",
                $"onLand={OnLandBoundaries}",
                $"meanBoundary={MeanBoundaryLengthMeters.ToString("F0", c)}m",
                $"chains={ChainsAccepted}/{ChainsDiscovered}",
                $"candidates={CandidateCount}",
                $"accepted={AcceptedCount}",
                $"longest={LongestAxisMeters.ToString("F0", c)}m",
                $"reach={WorstReachMeters.ToString("F0", c)}m",
                $"required={RequiredReachMeters.ToString("F0", c)}m",
                $"usableW={WorstUsableHalfWidthMeters.ToString("F0", c)}m",
                $"L/W={WorstLengthToWidthRatio.ToString("F2", c)}",
                $"gap={MinSystemSeparationMeters.ToString("F0", c)}m",
                $"ms={TotalMs}",
            });
        }
    }

    public static class Stage2AMacroStructure
    {
        public static Stage2AResult Run(
            int seed, TerrainGenSettings terrainSettings, MacroStructureSettings macroSettings) =>
            Run(new Seed.WorldSeed(seed), terrainSettings, macroSettings);

        public static Stage2AResult Run(
            Seed.IWorldSeed worldSeed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings)
        {
            terrainSettings.Clamp();
            macroSettings.Clamp();

            var total = Stopwatch.StartNew();
            var stats = new Stage2AStats
            {
                Seed = worldSeed.Value,
                RequiredReachMeters = macroSettings.RequiredReachMeters,
            };

            var sw = Stopwatch.StartNew();
            var partition = CrustalPartition.Build(worldSeed, terrainSettings);
            stats.PartitionMs = sw.ElapsedMilliseconds;
            stats.FineSiteCount = partition.SiteCount;

            sw.Restart();
            var landmass = LandmassBuilder.Build(worldSeed, terrainSettings, partition);
            stats.LandmassMs = sw.ElapsedMilliseconds;

            var structure = new WorldStructure(
                worldSeed.Value,
                terrainSettings.GeneratorVersion,
                landmass.CoastRings,
                landmass.LandSites,
                landmass.IsletCount,
                partition);

            var coast = new CoastQuery(structure.CoastRings, terrainSettings.MaxCoastDistanceMeters);

            sw.Restart();
            var plates = MacroPlates.Build(worldSeed, macroSettings, partition, landmass.LandSites);
            stats.PlatesMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var candidates = MacroAxisBuilder.Build(plates, macroSettings, coast, out var report);
            stats.AxesMs = sw.ElapsedMilliseconds;

            FillStats(stats, macroSettings, plates, candidates, report);
            stats.TotalMs = total.ElapsedMilliseconds;

            return new Stage2AResult(
                worldSeed.Value, terrainSettings, macroSettings,
                partition, landmass, structure, coast, plates, candidates, report, stats);
        }

        private static void FillStats(
            Stage2AStats stats,
            MacroStructureSettings settings,
            MacroPlates plates,
            IReadOnlyList<MacroAxis> candidates,
            MacroAxisBuilder.Report report)
        {
            stats.MacroPlateCount = plates.PlateCount;
            stats.MacroPlatesInWorld = plates.PlatesInWorld;
            stats.MacroPlatesOnLand = plates.PlatesOnLand;
            stats.MacroBoundaryCount = plates.Boundaries.Length;
            stats.MacroJunctionCount = plates.Junctions.Length;
            stats.MeanBoundaryLengthMeters = plates.MeanInteriorBoundaryLength();

            stats.MinBoundaryLengthMeters = float.PositiveInfinity;
            for (var e = 0; e < plates.Boundaries.Length; e++)
            {
                var length = plates.Boundaries[e].Length;
                stats.MinBoundaryLengthMeters = Mathf.Min(stats.MinBoundaryLengthMeters, length);
                stats.MaxBoundaryLengthMeters = Mathf.Max(stats.MaxBoundaryLengthMeters, length);
            }

            if (plates.Boundaries.Length == 0)
            {
                stats.MinBoundaryLengthMeters = 0f;
            }

            stats.ConvergentBoundaries = report.ConvergentBoundaries;
            stats.OnLandBoundaries = report.OnLandBoundaries;
            stats.ChainsDiscovered = report.ChainsDiscovered;
            stats.ChainsAccepted = report.ChainsAccepted;
            stats.FilletRelaxations = report.FilletRelaxations;
            stats.FilletSplits = report.FilletSplits;

            stats.CandidateCount = candidates.Count;
            stats.WorstReachMeters = float.PositiveInfinity;
            stats.WorstUsableHalfWidthMeters = float.PositiveInfinity;
            stats.WorstLengthToWidthRatio = float.PositiveInfinity;
            stats.MinSystemSeparationMeters = float.PositiveInfinity;

            var accepted = new List<MacroAxis>();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].SelfIntersects)
                {
                    stats.SelfIntersectingCandidates++;
                }

                if (!candidates[i].Accepted)
                {
                    continue;
                }

                accepted.Add(candidates[i]);
            }

            stats.AcceptedCount = accepted.Count;
            for (var i = 0; i < accepted.Count; i++)
            {
                var axis = accepted[i];
                stats.LongestAxisMeters = Mathf.Max(stats.LongestAxisMeters, axis.LengthMeters);
                stats.TotalAcceptedAxisMeters += axis.LengthMeters;
                stats.WorstReachMeters = Mathf.Min(stats.WorstReachMeters, axis.ReachMeters);
                stats.WorstUsableHalfWidthMeters =
                    Mathf.Min(stats.WorstUsableHalfWidthMeters, axis.UsableHalfWidthMeters);
                stats.WorstLengthToWidthRatio =
                    Mathf.Min(stats.WorstLengthToWidthRatio, axis.LengthToWidthRatio);

                for (var k = i + 1; k < accepted.Count; k++)
                {
                    stats.MinSystemSeparationMeters = Mathf.Min(
                        stats.MinSystemSeparationMeters,
                        MacroAxisBuilder.Separation(axis, accepted[k]));
                }
            }

            if (accepted.Count == 0)
            {
                stats.WorstReachMeters = 0f;
                stats.WorstUsableHalfWidthMeters = 0f;
                stats.WorstLengthToWidthRatio = 0f;
            }

            if (accepted.Count < 2)
            {
                stats.MinSystemSeparationMeters = 0f;
            }
        }

        /// <summary>The per-candidate table the feasibility report is judged from.</summary>
        public static string Describe(Stage2AResult result)
        {
            var c = CultureInfo.InvariantCulture;
            var s = result.Stats;
            var sb = new StringBuilder();

            sb.AppendLine($"seed {result.Seed}");
            sb.AppendLine(
                $"  macro      {s.MacroPlatesOnLand} plate(s) seeded on land, {s.MacroPlatesInWorld} touching "
                + $"the world, {s.MacroPlateCount} total, "
                + $"{s.MacroBoundaryCount} boundaries, {s.MacroJunctionCount} junctions");
            sb.AppendLine(
                $"  boundary   length [{s.MinBoundaryLengthMeters.ToString("F0", c)} .. "
                + $"{s.MaxBoundaryLengthMeters.ToString("F0", c)}] m, "
                + $"mean inside world {s.MeanBoundaryLengthMeters.ToString("F0", c)} m");
            sb.AppendLine(
                $"  convergence {s.ConvergentBoundaries} of {s.MacroBoundaryCount} boundaries above "
                + $"{result.MacroSettings.ConvergenceThreshold.ToString("F2", c)}, "
                + $"{s.OnLandBoundaries} of those reach land");
            sb.AppendLine(
                $"  chains     {s.ChainsAccepted} accepted of {s.ChainsDiscovered} discovered, "
                + $"fillet relaxed {s.FilletRelaxations}, split {s.FilletSplits}, "
                + $"pieces {result.BuildReport.SegmentsBeforeLandClip} -> "
                + $"{result.BuildReport.SegmentsAfterLandClip} after land clip");
            sb.AppendLine(
                $"  axes       {s.AcceptedCount} accepted of {s.CandidateCount} candidates, "
                + $"longest {s.LongestAxisMeters.ToString("F0", c)} m, "
                + $"total {s.TotalAcceptedAxisMeters.ToString("F0", c)} m");
            sb.AppendLine(
                $"  width      required reach {s.RequiredReachMeters.ToString("F0", c)} m, "
                + $"worst reach {s.WorstReachMeters.ToString("F0", c)} m, "
                + $"worst usable half-width {s.WorstUsableHalfWidthMeters.ToString("F0", c)} m "
                + $"(intended {result.MacroSettings.TargetHalfWidthMeters.ToString("F0", c)} m)");
            sb.AppendLine(
                $"  spacing    worst L/W {s.WorstLengthToWidthRatio.ToString("F2", c)}, "
                + $"closest accepted pair {s.MinSystemSeparationMeters.ToString("F0", c)} m, "
                + $"self-intersecting candidates {s.SelfIntersectingCandidates}");
            sb.AppendLine(
                $"  timing     partition {s.PartitionMs} ms, landmass {s.LandmassMs} ms, "
                + $"plates {s.PlatesMs} ms, axes {s.AxesMs} ms, total {s.TotalMs} ms");

            sb.Append("  corridor  ");
            for (var w = 0; w < MacroAxisBuilder.ProbeWidths.Length; w++)
            {
                sb.Append(
                    $" W={MacroAxisBuilder.ProbeWidths[w].ToString("F0", c)}:"
                    + $"{result.BuildReport.CorridorAtWidth[w].ToString("F0", c)}m");
            }

            sb.AppendLine();
            sb.AppendLine(
                $"  certify   {result.BuildReport.BaseRunsConsidered} run(s) considered, "
                + $"{result.BuildReport.RunsWithoutRoom} with no room for any permitted width");

            sb.AppendLine("  candidates:");
            sb.AppendLine(
                "    idx  state     len_m  ctrl  spacing_m  maxTurn  minR_m  reach_m  usableW_m  "
                + "selfDist_m  L/W  turns>30  xing  plates");

            for (var i = 0; i < result.Candidates.Count; i++)
            {
                var a = result.Candidates[i];
                var state = a.Accepted ? "ACCEPT" : "reject";
                var selfDistance = float.IsPositiveInfinity(a.MinSelfDistanceMeters)
                    ? "  n/a"
                    : a.MinSelfDistanceMeters.ToString("F0", c);
                var radius = float.IsPositiveInfinity(a.ConstructedRadiusMeters)
                    ? "inf"
                    : a.ConstructedRadiusMeters.ToString("F0", c);
                var reach = float.IsPositiveInfinity(a.ReachMeters)
                    ? "inf"
                    : a.ReachMeters.ToString("F0", c);

                sb.AppendLine(
                    $"    {a.Index,3}  {state}  {a.LengthMeters.ToString("F0", c),7}  "
                    + $"{a.ControlPoints.Length,4}  {a.ControlSpacingMeters.ToString("F0", c),9}  "
                    + $"{a.MaxTurnDegrees.ToString("F0", c),7}  {radius,6}  {reach,7}  "
                    + $"{a.UsableHalfWidthMeters.ToString("F0", c),9}  {selfDistance,10}  "
                    + $"{a.LengthToWidthRatio.ToString("F2", c),4}  {a.MajorDirectionChanges,8}  "
                    + $"{(a.SelfIntersects ? "yes" : " no"),4}  {DescribePlates(a)}");

                if (!a.Accepted)
                {
                    sb.AppendLine($"         rejected: {a.RejectReason}");
                }
            }

            return sb.ToString();
        }

        private static string DescribePlates(MacroAxis axis)
        {
            var sb = new StringBuilder();
            for (var i = 0; i + 1 < axis.SourcePlatePairs.Length; i += 2)
            {
                if (sb.Length > 0)
                {
                    sb.Append(',');
                }

                sb.Append(axis.SourcePlatePairs[i]).Append('|').Append(axis.SourcePlatePairs[i + 1]);
            }

            return sb.ToString();
        }
    }
}
