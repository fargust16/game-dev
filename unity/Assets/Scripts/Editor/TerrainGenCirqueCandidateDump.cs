#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Text diagnostic for Stage 3E slice E1: the cirque enclosures the structure already contains.
    /// </summary>
    /// <remarks>
    /// Candidates only. There is no floor, no outlet elevation and no verdict to print, because none
    /// is computed: validation is blocked on a threshold the design never states and on what the
    /// outlet's elevation is measured over. What is printed is the enclosure and its provenance, which
    /// is what E2 will have to validate.
    /// </remarks>
    public static class TerrainGenCirqueCandidateDump
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private const string OutputArg = "-terrainGenOut";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3E Cirque Candidates (E1)", false, 213)]
        public static void DumpMenu() =>
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_cirque")));

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenCirqueCandidateDump.DumpBatch</c></summary>
        public static void DumpBatch()
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            Debug.Log(Run(
                AuditSeeds,
                string.IsNullOrEmpty(directory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_cirque")
                    : directory));
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            var lines = new StringBuilder();

            sb.AppendLine("Stage 3E slice E1: cirque candidates");
            sb.AppendLine("Enumeration only. No floor, no outlet elevation, no basin verdict.");
            sb.AppendLine();

            var terrain = TerrainGenSettings.Default;
            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var ridgeSettings = RidgeSettings.Default;

            foreach (var seed in seeds)
            {
                var stage2a = Stage2AMacroStructure.Run(seed, terrain, macro);
                var worldSeed = new GameDev.World.Seed.WorldSeed(seed);

                var massifs = new List<MountainMassif>();
                var accepted = stage2a.AcceptedAxes();
                for (var i = 0; i < accepted.Count; i++)
                {
                    massifs.Add(MountainMassif.Build(
                        worldSeed, massifs.Count, accepted[i], terrain, massifSettings,
                        accepted[i].CertifiedCore));
                }

                var partition = Stage3ASkeleton.Build(seed, massifs, stage2a.Coast, ridgeSettings);
                var ridges = RidgeSkeleton.Build(partition, ridgeSettings, worldSeed);
                var set = CirqueCandidateBuilder.Build(ridges, ridgeSettings);

                sb.AppendLine(set.Stats.ToLine());
                sb.AppendLine("    " + set.Stats.RejectionLine());
                sb.AppendLine("    " + set.Stats.ShapeLine());

                for (var i = 0; i < set.Candidates.Count; i++)
                {
                    var candidate = set.Candidates[i];
                    sb.AppendLine(
                        $"    C{candidate.Id}: corridor=V{candidate.CorridorId}"
                        + $" massif={candidate.MassifIndex}"
                        + $" flank={(candidate.FlankSign > 0 ? "+1" : "-1")}"
                        + $" spurs=[S{candidate.LowerSpurId},S{candidate.UpperSpurId}]"
                        + $" crest=[{candidate.CrestInterval.x.ToString("F3", culture)},"
                        + $"{candidate.CrestInterval.y.ToString("F3", culture)}]"
                        + $" samples={candidate.Boundary.Length}"
                        + $" area={candidate.AreaSqm.ToString("F0", culture)}m2"
                        + $" perimeter={candidate.PerimeterMeters.ToString("F0", culture)}m"
                        + $" gap={candidate.OutletGapMeters.ToString("F0", culture)}m"
                        + $" crestSpan={candidate.CrestSpanMeters.ToString("F0", culture)}m"
                        + $" convergence={candidate.ConvergenceRatio.ToString("F2", culture)}"
                        + $" ridgePerimeter={candidate.RidgePerimeterFraction.ToString("P1", culture)}");

                    var runs = new StringBuilder();
                    for (var k = 0; k < candidate.Segments.Length; k++)
                    {
                        var run = candidate.Segments[k];
                        if (k > 0)
                        {
                            runs.Append("  ");
                        }

                        runs.Append(run.Source);
                        runs.Append('(');
                        runs.Append(run.Source == BoundarySource.Spur
                            ? "S" + run.SourceId.ToString(culture)
                            : run.Source == BoundarySource.Crest
                                ? "massif " + run.SourceId.ToString(culture)
                                : "gap");
                        runs.Append(", ");
                        runs.Append(run.PointCount.ToString(culture));
                        runs.Append(" pts, ");
                        runs.Append(run.LengthMeters.ToString("F0", culture));
                        runs.Append("m)");
                    }

                    sb.AppendLine("      boundary: " + runs);

                    var ring = new StringBuilder();
                    for (var k = 0; k < candidate.Boundary.Length; k++)
                    {
                        if (k > 0)
                        {
                            ring.Append(' ');
                        }

                        ring.Append(candidate.Boundary[k].x.ToString("F1", culture));
                        ring.Append(',');
                        ring.Append(candidate.Boundary[k].y.ToString("F1", culture));
                    }

                    lines.AppendLine(
                        "candidate " + seed.ToString(culture) + " " + candidate.Id.ToString(culture)
                        + " : " + ring);
                }

                sb.AppendLine();
            }

            File.WriteAllText(
                Path.Combine(directory, "stage3e_cirque_report.txt"), sb.ToString());
            File.WriteAllText(
                Path.Combine(directory, "stage3e_cirque_lines.txt"), lines.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        private static string ReadArg(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif
