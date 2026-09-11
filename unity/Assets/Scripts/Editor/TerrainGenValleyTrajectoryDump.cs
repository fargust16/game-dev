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
    /// Text and plan-view diagnostic for Stage 3C slice C: the final valley trajectories.
    /// </summary>
    /// <remarks>
    /// Two files. The report carries the per-seed statistics and the continuity metrics; the polyline
    /// file carries the spur divides, the candidate beds, the final trajectories and the confluences
    /// as world-coordinate polylines, which is what a fan, a starfish, a duplicated downstream stretch
    /// or a valley jumping a ridge would all show up in.
    /// </remarks>
    public static class TerrainGenValleyTrajectoryDump
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3C Valley Trajectories (C)", false, 210)]
        public static void DumpMenu()
        {
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3c_trajectories")));
        }

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenValleyTrajectoryDump.DumpBatch</c></summary>
        public static void DumpBatch() =>
            Debug.Log(Run(ReadSeeds(), ReadOutputDirectory("stage3c_trajectories")));

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            var lines = new StringBuilder();

            sb.AppendLine("Stage 3C slice C: final valley trajectories");
            sb.AppendLine("Candidate geometry and Slice B2 topology only. No carving, no heights.");
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

                sb.AppendLine(final.Stats.ToLine());
                sb.AppendLine("    " + final.Stats.ContinuityLine());
                sb.AppendLine("    " + final.Stats.TerminationLine());
                sb.AppendLine(
                    $"    corridors={ridges.Valleys.Count} b2Merges={topology.Stats.MergeCount}"
                    + $" b2Trunks={topology.Stats.TrunkCount} repeat={Repeats(topology, ridgeSettings, stage1, final)}");

                sb.AppendLine("    " + Crossings(final, culture));

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var trajectory = final.Trajectories[i];
                    var inflow = new StringBuilder();
                    for (var k = 0; k < trajectory.TributaryIds.Length; k++)
                    {
                        if (k > 0)
                        {
                            inflow.Append(' ');
                        }

                        inflow.Append('V');
                        inflow.Append(trajectory.TributaryIds[k]);
                        inflow.Append('@');
                        inflow.Append(trajectory.Cumulative[trajectory.ConfluenceIndices[k]]
                            .ToString("F0", culture));
                        inflow.Append('m');
                    }

                    sb.AppendLine(
                        $"    V{trajectory.Id}: massif={trajectory.MassifIndex}"
                        + $" flank={(trajectory.FlankSign > 0 ? "+1" : "-1")}"
                        + $" samples={trajectory.Points.Length} toe={trajectory.ToeIndex}"
                        + $" length={trajectory.LengthMeters.ToString("F0", culture)}m"
                        + $" toeArc={trajectory.ToeArcMeters.ToString("F0", culture)}m"
                        + $" halfWidth={trajectory.HalfWidthMeters.ToString("F0", culture)}m"
                        + $" -> {(trajectory.IsTributary ? "V" + trajectory.DownstreamId : "trunk")}"
                        + $" ({trajectory.Termination}, root V{final.RootOf(i)}, depth {final.DepthOf(i)})"
                        + (inflow.Length > 0 ? "  inflow=[" + inflow + "]" : string.Empty));
                }

                // ---- plan view ----
                lines.AppendLine("seed " + seed.ToString(culture));
                for (var i = 0; i < ridges.Spurs.Count; i++)
                {
                    Polyline(lines, culture, "spur " + ridges.Spurs[i].Id.ToString(culture),
                        ridges.Spurs[i].Points);
                }

                for (var i = 0; i < network.CandidateBeds.Count; i++)
                {
                    Polyline(lines, culture, "bed " + ridges.Valleys[i].Id.ToString(culture),
                        network.CandidateBeds[i]);
                }

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var trajectory = final.Trajectories[i];
                    Polyline(
                        lines, culture,
                        "trajectory " + trajectory.Id.ToString(culture)
                        + " toe " + trajectory.ToeIndex.ToString(culture)
                        + " to " + (trajectory.IsTributary
                            ? trajectory.DownstreamId.ToString(culture)
                            : "-1"),
                        trajectory.Points);

                    for (var k = 0; k < trajectory.ConfluenceIndices.Length; k++)
                    {
                        var at = trajectory.Points[trajectory.ConfluenceIndices[k]];
                        lines.AppendLine(
                            "confluence " + trajectory.TributaryIds[k].ToString(culture)
                            + " " + trajectory.Id.ToString(culture) + " : "
                            + at.x.ToString("F1", culture) + "," + at.y.ToString("F1", culture));
                    }
                }

                lines.AppendLine();
                sb.AppendLine();
            }

            var path = Path.Combine(directory, "stage3c_trajectory_report.txt");
            File.WriteAllText(path, sb.ToString());
            File.WriteAllText(Path.Combine(directory, "stage3c_trajectory_lines.txt"), lines.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>
        /// Where two trajectories that did not merge nevertheless run into each other.
        /// </summary>
        /// <remarks>
        /// Slice B2 decides on straight candidate rays; the continuation descends inside its corridor's
        /// cone, which is a different curve. So "these two do not meet on valid ground" can hold for
        /// the rays and fail for the walks, and where it does, two channels cross on the apron.
        /// Reported with the distance past each toe, because nothing in this slice may act on it:
        /// merging them would overrule Slice B2, and moving them would be a second merge test.
        /// </remarks>
        private static string Crossings(ValleyTrajectoryNetwork final, CultureInfo culture)
        {
            var pairs = new StringBuilder();
            var count = 0;

            for (var a = 0; a < final.Trajectories.Count; a++)
            {
                for (var b = a + 1; b < final.Trajectories.Count; b++)
                {
                    var first = final.Trajectories[a];
                    var second = final.Trajectories[b];

                    for (var i = first.ToeIndex; i + 1 < first.Points.Length; i++)
                    {
                        for (var j = second.ToeIndex; j + 1 < second.Points.Length; j++)
                        {
                            if (!Cross(
                                    first.Points[i], first.Points[i + 1],
                                    second.Points[j], second.Points[j + 1]))
                            {
                                continue;
                            }

                            count++;
                            pairs.Append(pairs.Length > 0 ? " " : string.Empty);
                            pairs.Append("V" + first.Id.ToString(culture));
                            pairs.Append("xV" + second.Id.ToString(culture) + "@");
                            pairs.Append(
                                (first.Cumulative[i] - first.ToeArcMeters).ToString("F0", culture));
                            pairs.Append('/');
                            pairs.Append(
                                (second.Cumulative[j] - second.ToeArcMeters).ToString("F0", culture));
                            pairs.Append('m');
                        }
                    }
                }
            }

            return "crossings=" + count.ToString(culture)
                   + (count > 0 ? "  [" + pairs + "]" : string.Empty);
        }

        private static bool Cross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            var d = (a1.x - a0.x) * (b1.y - b0.y) - (a1.y - a0.y) * (b1.x - b0.x);
            if (Mathf.Abs(d) < 1e-9f)
            {
                return false;
            }

            var u = ((b0.x - a0.x) * (b1.y - b0.y) - (b0.y - a0.y) * (b1.x - b0.x)) / d;
            var v = ((b0.x - a0.x) * (a1.y - a0.y) - (b0.y - a0.y) * (a1.x - a0.x)) / d;
            return u > 1e-6f && u < 1f - 1e-6f && v > 1e-6f && v < 1f - 1e-6f;
        }

        /// <summary>Rebuilds from the same inputs and reports whether every sample came back identical.</summary>
        private static string Repeats(
            ValleyTopology topology,
            RidgeSettings settings,
            Stage1Result stage1,
            ValleyTrajectoryNetwork first)
        {
            var again = ValleyTrajectoryNetwork.Build(topology, settings, stage1);
            if (again.Trajectories.Count != first.Trajectories.Count)
            {
                return "DIFFERS";
            }

            for (var i = 0; i < first.Trajectories.Count; i++)
            {
                var a = first.Trajectories[i];
                var b = again.Trajectories[i];
                if (a.Points.Length != b.Points.Length
                    || a.DownstreamId != b.DownstreamId
                    || a.Termination != b.Termination)
                {
                    return "DIFFERS";
                }

                for (var k = 0; k < a.Points.Length; k++)
                {
                    if (a.Points[k] != b.Points[k])
                    {
                        return "DIFFERS";
                    }
                }
            }

            return "identical";
        }

        private static void Polyline(
            StringBuilder sb, CultureInfo culture, string header, IReadOnlyList<Vector2> points)
        {
            sb.Append(header);
            sb.Append(" : ");
            for (var k = 0; k < points.Count; k++)
            {
                if (k > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(points[k].x.ToString("F1", culture));
                sb.Append(',');
                sb.Append(points[k].y.ToString("F1", culture));
            }

            sb.AppendLine();
        }

        private static string ReadOutputDirectory(string fallbackName)
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            return string.IsNullOrEmpty(directory)
                ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", fallbackName)
                : directory;
        }

        private static int[] ReadSeeds()
        {
            var raw = ReadArg(System.Environment.GetCommandLineArgs(), SeedsArg);
            if (string.IsNullOrEmpty(raw))
            {
                return AuditSeeds;
            }

            var parts = raw.Split(',');
            var seeds = new List<int>(parts.Length);
            for (var i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i].Trim(), out var value))
                {
                    seeds.Add(value);
                }
            }

            return seeds.Count > 0 ? seeds.ToArray() : AuditSeeds;
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
