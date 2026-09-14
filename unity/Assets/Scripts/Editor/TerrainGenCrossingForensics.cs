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
    /// Read-only forensic measurement of the Stage 3C trajectory crossings, for the Stage 3D preflight.
    /// </summary>
    /// <remarks>
    /// Measures only. It builds the pipeline exactly as the Slice C diagnostic does, reads the result
    /// and writes a report; it holds no settings of its own, changes no generation behaviour and is not
    /// referenced by anything that does. Kept separate from
    /// <see cref="TerrainGenValleyTrajectoryDump"/> so that the frozen Stage 3C files stay untouched.
    /// </remarks>
    public static class TerrainGenCrossingForensics
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777,
        };

        private const string OutputArg = "-terrainGenOut";

        [MenuItem("GameDev/Overworld/Terrain Gen/Audit Stage 3C Trajectory Crossings", false, 211)]
        public static void AuditMenu() =>
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3c_crossings")));

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenCrossingForensics.AuditBatch</c></summary>
        public static void AuditBatch()
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            Debug.Log(Run(
                AuditSeeds,
                string.IsNullOrEmpty(directory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3c_crossings")
                    : directory));
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("Stage 3C trajectory crossings - forensic preflight for Stage 3D");
            sb.AppendLine("Read-only. No generation behaviour is affected by this file.");
            sb.AppendLine();

            var terrain = TerrainGenSettings.Default;
            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var ridgeSettings = RidgeSettings.Default;
            var total = 0;

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

                if (final.Trajectories.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($"seed {seed}");

                // ---- per trajectory: where it leaves its own structural domain, and what it ends as ----
                var exitArc = new float[final.Trajectories.Count];
                var exitKind = new MassifOwnerKind[final.Trajectories.Count];
                for (var i = 0; i < final.Trajectories.Count; i++)
                {
                    var trajectory = final.Trajectories[i];
                    exitArc[i] = float.PositiveInfinity;
                    exitKind[i] = MassifOwnerKind.Massif;

                    // The bed is inside the massif by construction; measure from the foot.
                    for (var k = trajectory.ToeIndex; k < trajectory.Points.Length; k++)
                    {
                        var p = trajectory.Points[k];
                        var owner = ridges.Ownership.Ownership.At(p.x, p.y);
                        if (owner.IsStructural && owner.MassifIndex == trajectory.MassifIndex)
                        {
                            continue;
                        }

                        exitArc[i] = trajectory.Cumulative[k] - trajectory.ToeArcMeters;
                        exitKind[i] = owner.Kind;
                        break;
                    }

                    var corridor = ridges.Valleys[i];
                    sb.AppendLine(
                        $"  V{i}: spurs=[S{corridor.LowerSpurId},S{corridor.UpperSpurId}]"
                        + $" massif={corridor.MassifIndex}"
                        + $" flank={(corridor.FlankSign > 0 ? "+1" : "-1")}"
                        + $" termination={final.Trajectories[i].Termination}"
                        + $" downstream={(final.Trajectories[i].IsTributary ? "V" + final.Trajectories[i].DownstreamId : "none")}"
                        + $" apronLength={(final.Trajectories[i].LengthMeters - final.Trajectories[i].ToeArcMeters).ToString("F0", culture)}m"
                        + $" leavesStructuralAt={(float.IsPositiveInfinity(exitArc[i]) ? "never" : exitArc[i].ToString("F0", culture) + "m")}"
                        + $" as={exitKind[i]}");
                }

                // ---- the crossings themselves ----
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
                                if (!Cross(
                                        first.Points[i], first.Points[i + 1],
                                        second.Points[j], second.Points[j + 1], out var at))
                                {
                                    continue;
                                }

                                total++;
                                Describe(
                                    sb, culture, ridges, network, topology, final, stage1,
                                    first, second, i, j, at, exitArc);
                            }
                        }
                    }
                }

                sb.AppendLine();
            }

            sb.AppendLine($"total crossings: {total}");

            var path = Path.Combine(directory, "stage3c_crossing_forensics.txt");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        private static void Describe(
            StringBuilder sb,
            CultureInfo culture,
            RidgeSkeleton ridges,
            ValleyNetwork network,
            ValleyTopology topology,
            ValleyTrajectoryNetwork final,
            Stage1Result stage1,
            FinalValleyTrajectory first,
            FinalValleyTrajectory second,
            int i,
            int j,
            Vector2 at,
            float[] exitArc)
        {
            var lower = ridges.Valleys[first.Id];
            var upper = ridges.Valleys[second.Id];
            var massif = ridges.Ownership.Massifs[first.MassifIndex];

            var pastToeA = first.Cumulative[i] - first.ToeArcMeters;
            var pastToeB = second.Cumulative[j] - second.ToeArcMeters;
            var owner = ridges.Ownership.Ownership.At(at.x, at.y);

            sb.AppendLine(
                $"  CROSSING V{first.Id} x V{second.Id} at ({at.x.ToString("F0", culture)},"
                + $"{at.y.ToString("F0", culture)})");
            sb.AppendLine(
                $"    spurs: V{first.Id}=[S{lower.LowerSpurId},S{lower.UpperSpurId}]"
                + $" V{second.Id}=[S{upper.LowerSpurId},S{upper.UpperSpurId}]"
                + $" shared={SharedSpur(lower, upper)}"
                + $" adjacent={network.AreAdjacent(first.Id, second.Id)}"
                + $" sameChain={network.ChainOf(first.Id) == network.ChainOf(second.Id)}");
            sb.AppendLine(
                $"    terminations: V{first.Id}={first.Termination} V{second.Id}={second.Termination}");
            sb.AppendLine(
                $"    pastToe: V{first.Id}={pastToeA.ToString("F0", culture)}m"
                + $" V{second.Id}={pastToeB.ToString("F0", culture)}m"
                + $"  axisDistance={massif.Axis.DistanceTo(at.x, at.y).ToString("F0", culture)}m"
                + $"  straightFromToeA={(at - first.Points[first.ToeIndex]).magnitude.ToString("F0", culture)}m"
                + $"  straightFromToeB={(at - second.Points[second.ToeIndex]).magnitude.ToString("F0", culture)}m");
            sb.AppendLine(
                $"    ownership at crossing: {owner.Kind}"
                + $" structural={owner.IsStructural}"
                + $" massif={(owner.IsOwned ? owner.MassifIndex.ToString(culture) : "-")}"
                + $"  coastSignedDistance={stage1.Coast.SignedDistance(at.x, at.y).ToString("F0", culture)}m");
            sb.AppendLine(
                $"    already outside own structural domain:"
                + $" V{first.Id}={(pastToeA >= exitArc[first.Id] ? "yes" : "no")}"
                + $" (exit {Format(exitArc[first.Id], culture)})"
                + $" V{second.Id}={(pastToeB >= exitArc[second.Id] ? "yes" : "no")}"
                + $" (exit {Format(exitArc[second.Id], culture)})");

            sb.AppendLine(
                $"    structural features crossed before this point:"
                + $" V{first.Id}: {FeaturesBefore(ridges, first, i)}"
                + $" | V{second.Id}: {FeaturesBefore(ridges, second, j)}");
            sb.AppendLine(
                $"    axis crossing detail: V{first.Id}: {AxisDetail(ridges, first, culture)}"
                + $" | V{second.Id}: {AxisDetail(ridges, second, culture)}");

            sb.AppendLine("    B2 verdict on this pair: " + Verdict(topology, first.Id, second.Id, culture));
        }

        /// <summary>The Slice B2 event that judged this pair, if one did.</summary>
        private static string Verdict(
            ValleyTopology topology, int a, int b, CultureInfo culture)
        {
            for (var e = 0; e < topology.Events.Count; e++)
            {
                var result = topology.Events[e];
                var pair = (result.LowerActiveId == a && result.UpperActiveId == b)
                           || (result.LowerActiveId == b && result.UpperActiveId == a);
                if (!pair)
                {
                    continue;
                }

                return $"event {e} spur S{result.SpurId} -> {result.Outcome}"
                       + (result.ConfluenceAxisDistanceMeters > 0f
                           ? $" (rays met at ({result.ConfluencePoint.x.ToString("F0", culture)},"
                             + $"{result.ConfluencePoint.y.ToString("F0", culture)}),"
                             + $" axisDist={result.ConfluenceAxisDistanceMeters.ToString("F0", culture)}m)"
                           : string.Empty);
            }

            return "never evaluated as an active pair";
        }

        /// <summary>Which primary structural lines a trajectory has crossed before a given segment.</summary>
        private static string FeaturesBefore(RidgeSkeleton ridges, FinalValleyTrajectory t, int before)
        {
            var spurs = new List<int>();
            for (var s = 0; s < ridges.Spurs.Count; s++)
            {
                var points = ridges.Spurs[s].Points;
                for (var i = 0; i < before; i++)
                {
                    var hit = false;
                    for (var k = 0; k + 1 < points.Length; k++)
                    {
                        if (Cross(t.Points[i], t.Points[i + 1], points[k], points[k + 1], out _))
                        {
                            hit = true;
                            break;
                        }
                    }

                    if (hit)
                    {
                        spurs.Add(ridges.Spurs[s].Id);
                        break;
                    }
                }
            }

            var axis = ridges.Ownership.Massifs[t.MassifIndex].Axis.Points;
            var crossedAxis = false;
            for (var i = 0; i < before && !crossedAxis; i++)
            {
                for (var k = 0; k + 1 < axis.Length; k++)
                {
                    if (Cross(t.Points[i], t.Points[i + 1], axis[k], axis[k + 1], out _))
                    {
                        crossedAxis = true;
                        break;
                    }
                }
            }

            if (spurs.Count == 0 && !crossedAxis)
            {
                return "none";
            }

            var sb = new StringBuilder();
            for (var k = 0; k < spurs.Count; k++)
            {
                sb.Append(sb.Length > 0 ? " S" : "S");
                sb.Append(spurs[k]);
            }

            if (crossedAxis)
            {
                sb.Append(sb.Length > 0 ? " axis" : "axis");
            }

            return sb.ToString();
        }

        /// <summary>Where a trajectory meets its own massif's axis polyline, and on what ground.</summary>
        /// <remarks>
        /// Crossing the crest line inside the massif would be impossible ground; crossing the axis
        /// polyline out past the massif's end is crossing a line with no ridge under it. The two are
        /// told apart by the ownership at the meeting point, so the report does not have to guess.
        /// </remarks>
        private static string AxisDetail(
            RidgeSkeleton ridges, FinalValleyTrajectory t, CultureInfo culture)
        {
            var axis = ridges.Ownership.Massifs[t.MassifIndex].Axis.Points;
            var count = 0;
            var deepest = float.NegativeInfinity;
            var report = new StringBuilder();

            for (var i = 0; i + 1 < t.Points.Length; i++)
            {
                for (var k = 0; k + 1 < axis.Length; k++)
                {
                    if (!Cross(t.Points[i], t.Points[i + 1], axis[k], axis[k + 1], out var at))
                    {
                        continue;
                    }

                    count++;
                    var owner = ridges.Ownership.Ownership.At(at.x, at.y);
                    var pastToe = t.Cumulative[i] - t.ToeArcMeters;
                    deepest = Mathf.Max(deepest, pastToe);
                    report.Append(report.Length > 0 ? "; " : string.Empty);
                    report.Append($"({at.x.ToString("F0", culture)},{at.y.ToString("F0", culture)})");
                    report.Append($" pastToe={pastToe.ToString("F0", culture)}m");
                    report.Append($" {owner.Kind} structural={owner.IsStructural}");
                    report.Append($" axisFraction={(k / (float)(axis.Length - 1)).ToString("F2", culture)}");
                }
            }

            return count == 0
                ? "never"
                : $"count={count} deepestPastToe={deepest.ToString("F0", culture)}m  [{report}]";
        }

        private static string SharedSpur(ValleyCorridor a, ValleyCorridor b)
        {
            if (a.UpperSpurId >= 0 && a.UpperSpurId == b.LowerSpurId)
            {
                return "S" + a.UpperSpurId;
            }

            if (b.UpperSpurId >= 0 && b.UpperSpurId == a.LowerSpurId)
            {
                return "S" + b.UpperSpurId;
            }

            return "none";
        }

        private static string Format(float metres, CultureInfo culture) =>
            float.IsPositiveInfinity(metres) ? "never" : metres.ToString("F0", culture) + "m";

        private static bool Cross(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1, out Vector2 at)
        {
            at = Vector2.zero;
            var d = (a1.x - a0.x) * (b1.y - b0.y) - (a1.y - a0.y) * (b1.x - b0.x);
            if (Mathf.Abs(d) < 1e-9f)
            {
                return false;
            }

            var u = ((b0.x - a0.x) * (b1.y - b0.y) - (b0.y - a0.y) * (b1.x - b0.x)) / d;
            var v = ((b0.x - a0.x) * (a1.y - a0.y) - (b0.y - a0.y) * (a1.x - a0.x)) / d;
            if (u <= 1e-6f || u >= 1f - 1e-6f || v <= 1e-6f || v >= 1f - 1e-6f)
            {
                return false;
            }

            at = a0 + (a1 - a0) * u;
            return true;
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
