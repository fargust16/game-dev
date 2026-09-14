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
    /// Text diagnostic for Stage 3E slice E2: which enclosures became declared basins, and why not.
    /// </summary>
    /// <remarks>
    /// Every candidate appears, passed or rejected, with its reason. A validator whose diagnostic
    /// prints only its successes cannot be audited.
    /// </remarks>
    public static class TerrainGenStructuralBasinDump
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private const string OutputArg = "-terrainGenOut";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3E Structural Basins (E2)", false, 215)]
        public static void DumpMenu() =>
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_basins")));

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenStructuralBasinDump.DumpBatch</c></summary>
        public static void DumpBatch()
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            Debug.Log(Run(
                AuditSeeds,
                string.IsNullOrEmpty(directory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_basins")
                    : directory));
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("Stage 3E slice E2: structural basin validation");
            sb.AppendLine("Declarative. No floor, no depth, no tier, no H3, no carving, no RNG.");
            sb.AppendLine();

            var terrain = TerrainGenSettings.Default;
            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var ridgeSettings = RidgeSettings.Default;

            foreach (var seed in seeds)
            {
                var stage1 = Stage1LandmassGenerator.Generate(seed, terrain);
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
                var network = ValleyNetwork.Build(ridges, ridgeSettings);
                var topology = ValleyTopology.Build(network, ridgeSettings);
                var final = ValleyTrajectoryNetwork.Build(topology, ridgeSettings, stage1);
                var candidates = CirqueCandidateBuilder.Build(ridges, ridgeSettings);
                var basins = StructuralBasinBuilder.Build(
                    candidates, final, stage1, massifSettings, ridgeSettings);

                sb.AppendLine(basins.Stats.ToLine());
                sb.AppendLine("    " + basins.Stats.RejectionLine());
                sb.AppendLine("    " + basins.Stats.ShapeLine());

                foreach (var verdict in basins.Verdicts)
                {
                    var candidate = candidates.Candidates[verdict.CandidateId];
                    if (!verdict.Accepted)
                    {
                        sb.AppendLine(
                            $"    C{verdict.CandidateId} (V{verdict.CorridorId}): REJECTED"
                            + $" {verdict.Rejection}"
                            + $"  ridgePerimeter={candidate.RidgePerimeterFraction.ToString("P1", culture)}"
                            + $" area={candidate.AreaSqm.ToString("F0", culture)}m2"
                            + Detail(verdict, candidate, final, culture));
                        continue;
                    }

                    var basin = basins.Basins[verdict.BasinId];
                    sb.AppendLine(
                        $"    B{basin.Id} (C{basin.CandidateId}, V{basin.CorridorId}):"
                        + $" massif={basin.MassifIndex}"
                        + $" flank={(basin.FlankSign > 0 ? "+1" : "-1")}"
                        + $" spurs=[S{basin.EnclosingSpurIds[0]},S{basin.EnclosingSpurIds[1]}]"
                        + $" outlet=({basin.OutletPointXZ.x.ToString("F0", culture)},"
                        + $"{basin.OutletPointXZ.y.ToString("F0", culture)})"
                        + $" outletElev={basin.OutletElevationMeters.ToString("F2", culture)}m"
                        + $" outletIndex={basin.OutletBoundaryIndex}/{basin.Boundary.Length}"
                        + $" ridgePerimeter={basin.RidgePerimeterFraction.ToString("P1", culture)}"
                        + $" area={basin.AreaSqm.ToString("F0", culture)}m2"
                        + $" perimeter={basin.PerimeterMeters.ToString("F0", culture)}m");
                }

                sb.AppendLine();
            }

            sb.AppendLine("validated fields: outlet point, outlet elevation, outlet boundary index,");
            sb.AppendLine("  corridor link, enclosing spurs, boundary, provenance, ridge fraction,");
            sb.AppendLine("  area, perimeter.");
            sb.AppendLine("deferred by owner decision: basin floor elevation, hollow existence,");
            sb.AppendLine("  bounded depth, basin tier. Blocked: intermontane enclosure (B2).");

            File.WriteAllText(
                Path.Combine(directory, "stage3e_basin_report.txt"), sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>For a foreign-valley rejection, which corridor is inside and how deep in.</summary>
        private static string Detail(
            BasinVerdict verdict,
            CirqueCandidate candidate,
            ValleyTrajectoryNetwork final,
            CultureInfo culture)
        {
            if (verdict.Rejection != BasinRejection.ForeignValleyInside)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            for (var t = 0; t < final.Trajectories.Count; t++)
            {
                if (t == candidate.CorridorId || !final.Trajectories[t].HasGeometry)
                {
                    continue;
                }

                var points = final.Trajectories[t].Points;
                var inside = 0;
                var firstAt = Vector2.zero;

                for (var i = 0; i < points.Length; i++)
                {
                    if (!StructuralBasinBuilder.Inside(candidate.Boundary, points[i])
                        || StructuralBasinBuilder.DistanceToRing(candidate.Boundary, points[i])
                        <= RidgeSettings.Default.SpurSampleSpacingMeters)
                    {
                        continue;
                    }

                    if (inside == 0)
                    {
                        firstAt = points[i];
                    }

                    inside++;
                }

                if (inside == 0)
                {
                    continue;
                }

                sb.Append($"  [V{t} inside: {inside} samples, first at (");
                sb.Append(firstAt.x.ToString("F0", culture));
                sb.Append(',');
                sb.Append(firstAt.y.ToString("F0", culture));
                sb.Append(")]");
            }

            return sb.ToString();
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
