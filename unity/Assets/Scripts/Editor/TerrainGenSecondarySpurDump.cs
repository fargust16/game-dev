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
    /// Text and plan-view diagnostic for Stage 3D: the depth-2 ridge curves and their strips.
    /// </summary>
    /// <remarks>
    /// Identity is rendered as <c>S{parent}.{secondary}</c> throughout, so a reader can never mistake
    /// a rib for a primary spur. The Stage 3C figures are printed alongside, unchanged, because the
    /// first thing to check about Stage 3D is that it moved none of them.
    /// </remarks>
    public static class TerrainGenSecondarySpurDump
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3D Secondary Spurs", false, 212)]
        public static void DumpMenu() =>
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3d_secondary")));

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenSecondarySpurDump.DumpBatch</c></summary>
        public static void DumpBatch() =>
            Debug.Log(Run(ReadSeeds(), ReadOutputDirectory("stage3d_secondary")));

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            var lines = new StringBuilder();

            sb.AppendLine("Stage 3D: secondary ridge curves and child strips");
            sb.AppendLine("Skeleton only. No corridors, no topology, no heights.");
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
                var secondary = SecondarySpurBuilder.Build(ridges, final, ridgeSettings, worldSeed);

                sb.AppendLine(secondary.Stats.ToLine());
                sb.AppendLine("    " + secondary.Stats.RejectionLine());
                sb.AppendLine("    " + secondary.Stats.ShapeLine());
                sb.AppendLine(
                    $"    stage3c unchanged: corridors={ridges.Valleys.Count}"
                    + $" trajectories={final.Trajectories.Count}"
                    + $" confluences={final.Stats.ConfluenceCount}"
                    + $" crossings={Crossings(final)}");

                if (secondary.Spurs.Count == 0 && ridges.Spurs.Count == 0)
                {
                    sb.AppendLine();
                    continue;
                }

                for (var p = 0; p < ridges.Spurs.Count; p++)
                {
                    var parent = ridges.Spurs[p];
                    var kids = secondary.ChildrenOf(parent.Id);
                    sb.AppendLine(
                        $"    S{parent.Id}: massif={parent.MassifIndex}"
                        + $" flank={(parent.FlankSign > 0 ? "+1" : "-1")}"
                        + $" attachS={parent.AttachS.ToString("F3", culture)}"
                        + $" reach={parent.ReachMeters.ToString("F0", culture)}m"
                        + $"/{parent.TargetReachMeters.ToString("F0", culture)}m"
                        + $" termination={parent.Termination}"
                        + $" children={kids.Count}"
                        + $" strips=[{secondary.StripsOf(parent.Id, -1).Count},"
                        + $"{secondary.StripsOf(parent.Id, 1).Count}]");

                    for (var c = 0; c < kids.Count; c++)
                    {
                        var rib = secondary.Spurs[IndexOf(secondary, kids[c])];
                        sb.AppendLine(
                            $"      S{rib.ParentSpurId}.{rib.Id}:"
                            + $" depth=2"
                            + $" side={(rib.Side > 0 ? "+1" : "-1")}"
                            + $" attachT={rib.AttachT.ToString("F3", culture)}"
                            + $" at=({rib.AttachPoint.x.ToString("F0", culture)},"
                            + $"{rib.AttachPoint.y.ToString("F0", culture)})"
                            + $" samples={rib.Points.Length}"
                            + $" length={rib.LengthMeters.ToString("F0", culture)}m"
                            + $" reach={rib.ReachMeters.ToString("F1", culture)}m"
                            + $"/{rib.TargetReachMeters.ToString("F1", culture)}m"
                            + $" tierTarget={rib.TierTargetReachMeters.ToString("F1", culture)}m"
                            + $" bend={rib.ChordDeviationMeters.ToString("F2", culture)}m"
                            + $" drift={rib.LateralDriftMeters.ToString("F2", culture)}m"
                            + $" clearance={rib.ClearanceMeters.ToString("F1", culture)}m"
                            + $" budget={rib.ReliefBudgetMeters.ToString("F1", culture)}m"
                            + $" parentBudget={rib.ParentReliefBudgetMeters.ToString("F1", culture)}m"
                            + $" ratio={(rib.ParentReliefBudgetMeters / Mathf.Max(1e-3f, rib.ReliefBudgetMeters)).ToString("F2", culture)}"
                            + $" termination={rib.Termination}");
                    }
                }

                // ---- plan view ----
                lines.AppendLine("seed " + seed.ToString(culture));
                for (var i = 0; i < ridges.Spurs.Count; i++)
                {
                    Polyline(lines, culture, "spur " + ridges.Spurs[i].Id.ToString(culture),
                        ridges.Spurs[i].Points);
                }

                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    Polyline(lines, culture, "trajectory " + final.Trajectories[i].Id.ToString(culture),
                        final.Trajectories[i].Points);
                }

                for (var i = 0; i < secondary.Spurs.Count; i++)
                {
                    var rib = secondary.Spurs[i];
                    Polyline(
                        lines, culture,
                        "secondary " + rib.ParentSpurId.ToString(culture) + "."
                        + rib.Id.ToString(culture),
                        rib.Points);
                }

                lines.AppendLine();
                sb.AppendLine();
            }

            var path = Path.Combine(directory, "stage3d_secondary_report.txt");
            File.WriteAllText(path, sb.ToString());
            File.WriteAllText(Path.Combine(directory, "stage3d_secondary_lines.txt"), lines.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        private static int IndexOf(SecondaryRidgeSkeleton secondary, int id)
        {
            for (var i = 0; i < secondary.Spurs.Count; i++)
            {
                if (secondary.Spurs[i].Id == id)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>The Stage 3C baseline crossing count, reprinted so a drift would be obvious.</summary>
        /// <remarks>
        /// Strictly-interior intersections only, which is the predicate Stage 3C's own diagnostic and
        /// test 23 use. An endpoint touch is not a crossing: a tributary ends <em>on</em> its
        /// survivor, and counting that confluence as a crossing would put the baseline at ten.
        /// </remarks>
        private static int Crossings(ValleyTrajectoryNetwork final)
        {
            var count = 0;
            for (var a = 0; a < final.Trajectories.Count; a++)
            {
                for (var b = a + 1; b < final.Trajectories.Count; b++)
                {
                    var first = final.Trajectories[a];
                    var second = final.Trajectories[b];
                    for (var i = 0; i + 1 < first.Points.Length; i++)
                    {
                        for (var j = 0; j + 1 < second.Points.Length; j++)
                        {
                            if (Interior(
                                    first.Points[i], first.Points[i + 1],
                                    second.Points[j], second.Points[j + 1]))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private static bool Interior(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
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
