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
    /// Text diagnostic for Stage 3C slices A and B: rank, physical chains, candidate geometry.
    /// </summary>
    /// <remarks>
    /// Deliberately text only. There is no network to draw yet - no merges, no downstream links, no
    /// trajectories - so a picture would show the same fingers the last pass already produced and would
    /// invite reading topology into geometry that has none. The network render belongs to the slice
    /// that builds a network.
    /// </remarks>
    public static class TerrainGenValleyNetworkDump
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3C Valley Network (A+B)", false, 209)]
        public static void DumpMenu()
        {
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3c_network")));
        }

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenValleyNetworkDump.DumpBatch</c></summary>
        public static void DumpBatch() =>
            Debug.Log(Run(ReadSeeds(), ReadOutputDirectory("stage3c_network")));

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3C slices A + B + B2: rank, chains, candidate geometry, resolved topology");
            sb.AppendLine("Slice B2 resolves convergence. No trajectories, no carving, no heights.");
            sb.AppendLine();

            // Plan-view polylines, for looking at the candidate geometry rather than reading it.
            var beds = new StringBuilder();

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
                var network = ValleyNetwork.Build(ridges, ridgeSettings);
                var topology = ValleyTopology.Build(network, ridgeSettings);

                beds.AppendLine("seed " + seed.ToString(culture));
                for (var i = 0; i < network.CandidateBeds.Count; i++)
                {
                    var line = new StringBuilder();
                    var polyline = network.CandidateBeds[i];
                    for (var k = 0; k < polyline.Length; k++)
                    {
                        if (k > 0)
                        {
                            line.Append(' ');
                        }

                        line.Append(polyline[k].x.ToString("F1", culture));
                        line.Append(',');
                        line.Append(polyline[k].y.ToString("F1", culture));
                    }

                    beds.AppendLine(
                        "bed " + ridges.Valleys[i].Id.ToString(culture)
                        + " massif " + ridges.Valleys[i].MassifIndex.ToString(culture)
                        + " flank " + ridges.Valleys[i].FlankSign.ToString(culture)
                        + " : " + line);
                }

                for (var i = 0; i < ridges.Spurs.Count; i++)
                {
                    var line = new StringBuilder();
                    var polyline = ridges.Spurs[i].Points;
                    for (var k = 0; k < polyline.Length; k++)
                    {
                        if (k > 0)
                        {
                            line.Append(' ');
                        }

                        line.Append(polyline[k].x.ToString("F1", culture));
                        line.Append(',');
                        line.Append(polyline[k].y.ToString("F1", culture));
                    }

                    beds.AppendLine("spur " + ridges.Spurs[i].Id.ToString(culture) + " : " + line);
                }

                for (var i = 0; i < network.CandidateRays.Count; i++)
                {
                    var ray = network.CandidateRays[i];
                    beds.AppendLine(
                        "ray " + ridges.Valleys[i].Id.ToString(culture) + " : "
                        + ray.Origin.x.ToString("F1", culture) + "," + ray.Origin.y.ToString("F1", culture)
                        + " " + ray.Direction.x.ToString("F4", culture) + ","
                        + ray.Direction.y.ToString("F4", culture));
                }

                for (var i = 0; i < topology.DownstreamOf.Count; i++)
                {
                    if (topology.DownstreamOf[i] >= 0)
                    {
                        beds.AppendLine(
                            "merge " + i.ToString(culture) + " " + topology.DownstreamOf[i].ToString(culture)
                            + " : " + topology.ConfluenceOf[i].x.ToString("F1", culture) + ","
                            + topology.ConfluenceOf[i].y.ToString("F1", culture));
                    }
                }

                beds.AppendLine();

                sb.AppendLine(network.Stats.ToLine());
                sb.AppendLine("    " + topology.Stats.ToLine());
                sb.AppendLine("    " + topology.Stats.OutcomeLine());

                // ---- rank order ----
                if (network.RankOrder.Count > 0)
                {
                    var order = new StringBuilder();
                    for (var i = 0; i < network.RankOrder.Count; i++)
                    {
                        if (i > 0)
                        {
                            order.Append(" > ");
                        }

                        order.Append(network.RankOrder[i]);
                    }

                    sb.AppendLine("    rank order: " + order);
                }

                // ---- per corridor ----
                for (var i = 0; i < network.Ranks.Count; i++)
                {
                    var corridor = ridges.Valleys[i];
                    var rank = network.Ranks[i];
                    var bed = network.CandidateBeds[i];
                    var ray = network.CandidateRays[i];

                    sb.AppendLine(
                        $"    corridor {corridor.Id}: massif={corridor.MassifIndex}"
                        + $" flank={(corridor.FlankSign > 0 ? "+1" : "-1")}"
                        + $" lowerSpur={corridor.LowerSpurId} upperSpur={corridor.UpperSpurId}"
                        + $" neighbours=[{network.LowerNeighbourOf(i)},{network.UpperNeighbourOf(i)}]"
                        + $" chain={network.ChainOf(i)}");
                    sb.AppendLine(
                        $"      footprint={rank.FlankFootprintSqm.ToString("F0", culture)}m2"
                        + $" headCrest={rank.HeadCrestMeters.ToString("F1", culture)}m"
                        + $" reliefVolume={rank.ReliefVolumeCum.ToString("F0", culture)}m3");

                    if (bed.Length >= 2)
                    {
                        sb.AppendLine(
                            $"      bed: {bed.Length} samples"
                            + $" head={bed[0].ToString("F1", culture)}"
                            + $" toe={bed[bed.Length - 1].ToString("F1", culture)}");
                        sb.AppendLine(
                            $"      ray: origin={ray.Origin.ToString("F1", culture)}"
                            + $" dir={ray.Direction.ToString("F3", culture)}"
                            + $" valid={ray.IsValid}");

                        // How far the candidate geometry has come off the flank normal, and how hard
                        // it had to bend to do it. A ray still on the normal is the flat terminal
                        // tangent the straight-tangent model produced.
                        var massif = ridges.Ownership.Massifs[corridor.MassifIndex];
                        var endArc = ValleyTrajectoryBuilder.FloorArcAt(
                            massif, corridor, ridges.Spurs, 1f);
                        var normal = ValleyTrajectoryBuilder.NormalAt(
                            massif, endArc, ridgeSettings.SpurSampleSpacingMeters)
                            * corridor.FlankSign;
                        var offNormal = Vector2.Angle(ray.Direction, normal);

                        var radius = float.MaxValue;
                        var radiusAt = 0f;
                        for (var k = 2; k < bed.Length; k++)
                        {
                            var a = bed[k - 1] - bed[k - 2];
                            var b = bed[k] - bed[k - 1];
                            if (a.sqrMagnitude < 1e-10f || b.sqrMagnitude < 1e-10f)
                            {
                                continue;
                            }

                            var turn = Mathf.Abs(Vector2.SignedAngle(a, b)) * Mathf.Deg2Rad;
                            if (turn < 1e-5f)
                            {
                                continue;
                            }

                            var here = b.magnitude / turn;
                            if (here < radius)
                            {
                                radius = here;
                                radiusAt = (k - 1) / (float)(bed.Length - 1);
                            }
                        }

                        var halfWidth = ValleyTrajectoryBuilder.CorridorHalfWidthMeters(
                            massif, corridor, ridges.Spurs, endArc, 1f);

                        sb.AppendLine(
                            $"      geometry: offNormal={offNormal.ToString("F2", culture)}deg"
                            + $" minRadius={(radius == float.MaxValue ? -1f : radius).ToString("F0", culture)}m"
                            + $" at t={radiusAt.ToString("F2", culture)}"
                            + $" halfWidth={halfWidth.ToString("F0", culture)}m"
                            + $" toes=[{ToeOf(massif, ridges, corridor.LowerSpurId).ToString("F3", culture)},"
                            + $"{ToeOf(massif, ridges, corridor.UpperSpurId).ToString("F3", culture)}]");
                    }
                }

                // ---- chains and occupancy ----
                for (var c = 0; c < network.Chains.Count; c++)
                {
                    var chain = network.Chains[c];
                    var line = new StringBuilder();
                    for (var i = 0; i < chain.Length; i++)
                    {
                        if (i > 0)
                        {
                            line.Append(" -- S");
                            line.Append(chain.SeparatingSpurs[i - 1]);
                            line.Append(" -- ");
                        }

                        line.Append('V');
                        line.Append(chain.Corridors[i]);
                    }

                    sb.AppendLine(
                        $"    chain {c}: massif={chain.MassifIndex}"
                        + $" flank={(chain.FlankSign > 0 ? "+1" : "-1")}  {line}");

                    var occupancy = new StringBuilder();
                    for (var i = 0; i < chain.Length; i++)
                    {
                        if (i > 0)
                        {
                            occupancy.Append(' ');
                        }

                        occupancy.Append(i);
                        occupancy.Append("->V");
                        occupancy.Append(network.OccupantAt(c, i));
                    }

                    sb.AppendLine("      occupancy: " + occupancy);
                }

                // ---- divide-end events, in processing order, with what B2 decided ----
                for (var i = 0; i < network.ToeEvents.Count; i++)
                {
                    var e = network.ToeEvents[i];
                    var r = topology.Events[i];

                    sb.AppendLine(
                        $"    event {i}: spur={e.SpurId}"
                        + $" positions=[{e.Position},{e.Position + 1}] chain={e.ChainIndex}"
                        + $" corridors=[V{e.LowerCorridorId},V{e.UpperCorridorId}]"
                        + $" active=[V{r.LowerActiveId},V{r.UpperActiveId}]"
                        + $" toeFraction={e.ToeFraction.ToString("F3", culture)}"
                        + $" toe={e.ToePoint.ToString("F0", culture)}");

                    sb.AppendLine(
                        r.Merged
                            ? $"      -> MERGE V{r.LoserId} into V{r.WinnerId}"
                              + $" at={r.ConfluencePoint.ToString("F0", culture)}"
                              + $" axisDist={r.ConfluenceAxisDistanceMeters.ToString("F0", culture)}m"
                            : r.ConfluenceAxisDistanceMeters > 0f
                                ? $"      -> {r.Outcome}"
                                  + $" (rays met at {r.ConfluencePoint.ToString("F0", culture)},"
                                  + $" axisDist={r.ConfluenceAxisDistanceMeters.ToString("F0", culture)}m)"
                                : $"      -> {r.Outcome}");
                }

                // ---- resulting topology ----
                if (topology.Stats.MergeCount > 0)
                {
                    for (var i = 0; i < topology.DownstreamOf.Count; i++)
                    {
                        if (topology.DownstreamOf[i] < 0)
                        {
                            continue;
                        }

                        sb.AppendLine(
                            $"    V{i} -> V{topology.DownstreamOf[i]}"
                            + $" (root V{topology.RootOf(i)}, depth {topology.DepthOf(i)})"
                            + $" at={topology.ConfluenceOf[i].ToString("F0", culture)}");
                    }
                }

                var trunkList = new StringBuilder();
                for (var i = 0; i < topology.Trunks.Count; i++)
                {
                    if (i > 0)
                    {
                        trunkList.Append(' ');
                    }

                    trunkList.Append('V');
                    trunkList.Append(topology.Trunks[i]);
                }

                sb.AppendLine("    trunks: " + trunkList);

                for (var c = 0; c < topology.Occupancy.Count; c++)
                {
                    var slots = topology.Occupancy[c];
                    var line = new StringBuilder();
                    for (var i = 0; i < slots.Length; i++)
                    {
                        if (i > 0)
                        {
                            line.Append(' ');
                        }

                        line.Append(i);
                        line.Append("->V");
                        line.Append(slots[i]);
                    }

                    sb.AppendLine($"    final occupancy chain {c}: " + line);
                }

                sb.AppendLine();
            }

            var path = Path.Combine(directory, "stage3c_network_report.txt");
            File.WriteAllText(path, sb.ToString());
            File.WriteAllText(Path.Combine(directory, "stage3c_beds.txt"), beds.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>Where a bounding divide ends, as a fraction of the flank. One means the foot.</summary>
        private static float ToeOf(MountainMassif massif, RidgeSkeleton ridges, int spurId)
        {
            if (spurId < 0 || spurId >= ridges.Spurs.Count)
            {
                return 1f;
            }

            var spur = ridges.Spurs[spurId];
            var width = ValleyTrajectoryBuilder.FlankHalfWidth(massif, spur.AttachS, spur.FlankSign);
            return width > 1e-3f ? Mathf.Clamp01(spur.ReachMeters / width) : 1f;
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
