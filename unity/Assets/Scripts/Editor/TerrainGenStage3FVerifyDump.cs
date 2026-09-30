#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Stage 3F F3: an independent report of what the finished ground does along the declared valleys.
    /// </summary>
    /// <remarks>
    /// Verification only. It changes nothing, it fixes nothing, and it reads the finished surface rather
    /// than the carve that made it — see <see cref="Stage3FVerification"/>. Production is untouched;
    /// <c>TerrainAuthorityField</c> evaluates <see cref="IslandElevation"/>.
    /// </remarks>
    public static class TerrainGenStage3FVerifyDump
    {
        public static readonly int[] CanonicalSeeds = TerrainGenIslandReliefDump.CanonicalSeeds;

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";
        private const string SweepArg = "-terrainGenSweep";

        [MenuItem("GameDev/Overworld/Terrain Gen/Verify Stage 3F Drainage (Canonical Seeds)", false, 222)]
        public static void VerifyMenu()
        {
            Debug.Log(Run(CanonicalSeeds, Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3f-verify")));
        }

        /// <summary>
        /// Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenStage3FVerifyDump.VerifyBatch</c>
        /// <c>[-terrainGenSeeds a,b | -terrainGenSweep start:count] [-terrainGenOut dir]</c>.
        /// </summary>
        public static void VerifyBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var sweep = ReadArg(args, SweepArg);
            var directory = ReadArg(args, OutputArg)
                            ?? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3f-verify");
            if (!string.IsNullOrEmpty(sweep))
            {
                Debug.Log(RunSweep(sweep, directory));
                return;
            }

            Debug.Log(Run(ReadSeeds(args), directory));
        }

        /// <summary>One world, built the way Stage 3F builds it, plus the verification surfaces.</summary>
        private sealed class Subject
        {
            public IslandReliefWorld World;
            public RidgeSettings Settings;
            public CarveField Carve;
            public Stage3FVerification.Surface Finished;
            public long BuildMs;
            public long VerifyMs;
        }

        private static Subject Build(int seed)
        {
            var sw = Stopwatch.StartNew();
            var settings = RidgeSettings.Default;
            settings.Clamp();
            var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
            var carve = world.Trajectories.BuildCarve(
                world.Stage1.Coast, settings, world.Stage2B.MassifSettings);
            var build = sw.ElapsedMilliseconds;

            return new Subject
            {
                World = world,
                Settings = settings,
                Carve = carve,
                BuildMs = build,

                // The surface under test. F3 knows only that this is "the finished ground"; it does not
                // look inside the carve that produced it.
                Finished = (x, z) => CarvedElevation.At(
                    world.Stage1, world.Uplift, world.Relief, carve, settings, x, z),
            };
        }

        private static List<TrajectoryProfile> Verify(Subject s)
        {
            var sw = Stopwatch.StartNew();
            var profiles = new List<TrajectoryProfile>();
            foreach (var traj in s.World.Trajectories.Trajectories)
            {
                profiles.Add(Stage3FVerification.Profile(
                    traj, s.World.Stage1, s.World.Uplift, s.World.Relief, s.Finished,
                    s.World.Basins.Basins, s.Settings));
            }

            s.VerifyMs = sw.ElapsedMilliseconds;
            return profiles;
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3F F3 - independent drainage verification of the finished ground");
            sb.AppendLine("  reads H3F and H4A as surfaces; the carve is derived as H4A - H3F, never read from CarveField");
            sb.AppendLine($"  arc step {Stage3FVerification.StepMeters(RidgeSettings.Default).ToString("F1", c)} m, "
                          + $"qualifying depth {Stage3FVerification.QualifyingDepthMeters.ToString("F2", c)} m "
                          + "(Stage 4A S8's depth; S8's 100 m2 area is not a profile quantity)");
            sb.AppendLine("  production untouched: TerrainAuthorityField still evaluates IslandElevation");
            sb.AppendLine();

            var lines = new List<string>();
            foreach (var seed in seeds)
            {
                lines.Add(AppendSeed(sb, seed, directory, writeProfile: true));
            }

            sb.AppendLine();
            sb.AppendLine("one line per seed:");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            File.WriteAllText(Path.Combine(directory, "stage3f_verify.txt"), sb.ToString());
            return sb.ToString();
        }

        private static string AppendSeed(StringBuilder sb, int seed, string directory, bool writeProfile)
        {
            var c = CultureInfo.InvariantCulture;
            var s = Build(seed);
            var profiles = Verify(s);

            var worstRise = 0f;
            var worstRiseId = -1;
            var counts = new Dictionary<DrainageState, int>();
            var worstClosed = 0f;
            var worstClosedId = -1;
            var compositionViolations = 0;
            var minima = 0;
            var maxima = 0;

            foreach (var p in profiles)
            {
                compositionViolations += p.CompositionViolations;
                minima += p.LocalMinimaCount;
                maxima += p.LocalMaximaCount;
                if (p.AdverseRiseFinished > worstRise)
                {
                    worstRise = p.AdverseRiseFinished;
                    worstRiseId = p.TrajectoryId;
                }

                foreach (var f in p.Findings)
                {
                    counts.TryGetValue(f.State, out var n);
                    counts[f.State] = n + 1;
                    if (f.State == DrainageState.Closed && f.DepthMeters > worstClosed)
                    {
                        worstClosed = f.DepthMeters;
                        worstClosedId = f.TrajectoryId;
                    }
                }
            }

            sb.AppendLine(
                $"seed {seed}  massifs {s.World.Stage2B.Massifs.Count}  trajectories {profiles.Count}  "
                + $"basins {s.World.Basins.Basins.Count}");
            sb.AppendLine($"  timing     world+carve {s.BuildMs} ms, verification {s.VerifyMs} ms");

            if (profiles.Count == 0)
            {
                sb.AppendLine("  no corridors on this world, so Stage 3F leaves the island exactly as Stage 4A left it");
                return $"seed={seed} traj=0 rise=0.00 closed=0 drained=0 basin=0 terminal=0 ambiguous=0";
            }

            sb.AppendLine(
                $"  composition H3F <= H4A violated at {compositionViolations} samples; "
                + $"local minima {minima}, maxima {maxima}");
            sb.AppendLine(
                $"  worst finished adverse rise {worstRise.ToString("F2", c)} m on trajectory {worstRiseId}; "
                + $"worst CLOSED depth {worstClosed.ToString("F2", c)} m on trajectory {worstClosedId}");
            sb.AppendLine(
                $"  states     drained {Count(counts, DrainageState.Drained)}, "
                + $"drainable {Count(counts, DrainageState.DrainableWithinTrajectory)}, "
                + $"CLOSED {Count(counts, DrainageState.Closed)}, "
                + $"terminal {Count(counts, DrainageState.Terminal)}, "
                + $"basin-owned {Count(counts, DrainageState.BasinOwned)}, "
                + $"ambiguous {Count(counts, DrainageState.Ambiguous)}");

            sb.AppendLine(
                "  id  term         len_m  toe_m  half_m  S11T(toe)  dGate  sdExc_m  h1Rise_m  R4Afull  riseH4A  riseH3F  riseH3F(toe)  atS_m  maxCarve  min/max  findings");
            foreach (var p in profiles)
            {
                sb.AppendLine(
                    $"  {p.TrajectoryId,-3} {p.Termination,-12} {p.LengthMeters.ToString("F0", c),6} "
                    + $"{p.ToeArcMeters.ToString("F0", c),6} {p.HalfWidthMeters.ToString("F0", c),7} "
                    + $"{p.S11TClimb.ToString("F2", c),10} {p.GateTraverseFromToe.ToString("F2", c),6} "
                    + $"{p.CoastDistanceExcursionMeters.ToString("F0", c),8} {p.Stage1RiseFromToe.ToString("F2", c),9} "
                    + $"{p.ReliefRiseFullArc.ToString("F2", c),8} "
                    + $"{p.AdverseRiseIsland.ToString("F2", c),8} {p.AdverseRiseFinished.ToString("F2", c),8} "
                    + $"{p.AdverseRiseFinishedFromToe.ToString("F2", c),13} "
                    + $"{p.AdverseRiseArcMeters.ToString("F0", c),6} {p.MaxCarveMeters.ToString("F2", c),9} "
                    + $"{p.LocalMinimaCount}/{p.LocalMaximaCount,-6} {Describe(p.Findings)}");
            }

            foreach (var p in profiles)
            {
                foreach (var f in p.Findings)
                {
                    if (f.State != DrainageState.Closed && f.State != DrainageState.DrainableWithinTrajectory)
                    {
                        continue;
                    }

                    sb.AppendLine(
                        $"    {f.State} traj {f.TrajectoryId} at S={f.ArcMeters.ToString("F0", c)} m "
                        + $"({f.Position.x.ToString("F0", c)},{f.Position.y.ToString("F0", c)}) "
                        + $"floor {f.FloorMeters.ToString("F2", c)} m, barrier {f.BarrierMeters.ToString("F2", c)} m "
                        + $"at S={f.BarrierArcMeters.ToString("F0", c)} m, depth {f.DepthMeters.ToString("F2", c)} m, "
                        + $"across-valley floor at barrier {f.BarrierFloorAcrossMeters.ToString("F2", c)} m, "
                        + $"basin {f.BasinId}, {(f.BelowToe ? "below toe (island floor)" : "above toe (massif flank)")}");
                }
            }

            if (writeProfile && worstRiseId >= 0)
            {
                WriteLongitudinal(directory, seed, profiles, worstRiseId);
            }

            return $"seed={seed} traj={profiles.Count} rise={worstRise.ToString("F2", c)} "
                   + $"closed={Count(counts, DrainageState.Closed)} drained={Count(counts, DrainageState.Drained)} "
                   + $"drainable={Count(counts, DrainageState.DrainableWithinTrajectory)} "
                   + $"basin={Count(counts, DrainageState.BasinOwned)} terminal={Count(counts, DrainageState.Terminal)} "
                   + $"ambiguous={Count(counts, DrainageState.Ambiguous)} worstClosed={worstClosed.ToString("F2", c)}";
        }

        private static int Count(Dictionary<DrainageState, int> counts, DrainageState state) =>
            counts.TryGetValue(state, out var n) ? n : 0;

        private static string Describe(List<DrainageFinding> findings)
        {
            if (findings.Count == 0)
            {
                return "-";
            }

            var sb = new StringBuilder();
            foreach (var f in findings)
            {
                if (f.State == DrainageState.Ambiguous)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(f.State.ToString()[0]);
            }

            return sb.Length == 0 ? "-" : sb.ToString();
        }

        /// <summary>
        /// The worst trajectory as a longitudinal section: arc across, elevation up.
        /// </summary>
        /// <remarks>
        /// Three curves on one frame — the island it was cut from, the finished ground, and the carve
        /// between them — with the local lows and the worst rise marked, so the question "is this rise
        /// a drainage problem" can be answered from the picture rather than the source.
        /// </remarks>
        private static void WriteLongitudinal(
            string directory, int seed, List<TrajectoryProfile> profiles, int trajectoryId)
        {
            TrajectoryProfile p = null;
            foreach (var candidate in profiles)
            {
                if (candidate.TrajectoryId == trajectoryId)
                {
                    p = candidate;
                    break;
                }
            }

            if (p == null || p.Arc.Length < 2)
            {
                return;
            }

            const int w = 1200;
            const int h = 520;
            var px = new Color32[w * h];
            for (var i = 0; i < px.Length; i++)
            {
                px[i] = new Color32(16, 18, 24, 255);
            }

            var minY = float.PositiveInfinity;
            var maxY = float.NegativeInfinity;
            for (var i = 0; i < p.Arc.Length; i++)
            {
                minY = Mathf.Min(minY, Mathf.Min(p.H3F[i], p.H4A[i]));
                maxY = Mathf.Max(maxY, Mathf.Max(p.H3F[i], p.H4A[i]));
            }

            var pad = Mathf.Max(1f, (maxY - minY) * 0.08f);
            minY -= pad;
            maxY += pad;

            int X(float arc) => Mathf.Clamp(
                Mathf.RoundToInt(40 + (w - 60) * arc / Mathf.Max(1e-3f, p.LengthMeters)), 0, w - 1);

            // SetPixels32 is bottom-left origin, so a higher elevation is a higher row index. Writing
            // this the screen way round renders the section upside down, which is exactly wrong for the
            // one picture whose whole point is which way the valley falls.
            int Y(float v) => Mathf.Clamp(
                30 + Mathf.RoundToInt((h - 60) * (v - minY) / Mathf.Max(1e-3f, maxY - minY)), 0, h - 1);

            // The carve, as a filled band between the two surfaces.
            for (var i = 0; i < p.Arc.Length; i++)
            {
                var x = X(p.Arc[i]);
                var a = Y(p.H4A[i]);
                var b = Y(p.H3F[i]);
                for (var y = Mathf.Min(a, b); y <= Mathf.Max(a, b); y++)
                {
                    px[y * w + x] = new Color32(70, 55, 110, 255);
                }
            }

            Plot(px, w, h, p.Arc, p.H4A, X, Y, new Color32(120, 200, 255, 255));
            Plot(px, w, h, p.Arc, p.H3F, X, Y, new Color32(255, 210, 90, 255));

            foreach (var f in p.Findings)
            {
                var colour = f.State switch
                {
                    DrainageState.Closed => new Color32(255, 70, 70, 255),
                    DrainageState.DrainableWithinTrajectory => new Color32(255, 160, 60, 255),
                    DrainageState.BasinOwned => new Color32(90, 220, 140, 255),
                    DrainageState.Terminal => new Color32(160, 160, 200, 255),
                    _ => new Color32(110, 110, 120, 255),
                };

                Marker(px, w, h, X(f.ArcMeters), Y(f.FloorMeters), colour);
                Marker(px, w, h, X(f.BarrierArcMeters), Y(f.BarrierMeters), colour);
            }

            // The worst rise, as a vertical rule.
            var rx = X(p.AdverseRiseArcMeters);
            for (var y = 20; y < h - 20; y += 6)
            {
                px[y * w + rx] = new Color32(255, 255, 255, 255);
            }

            var path = Path.Combine(directory, $"seed_{seed}_profile_traj{trajectoryId}.png");
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(px);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void Plot(
            Color32[] px, int w, int h, float[] arc, float[] v,
            System.Func<float, int> X, System.Func<float, int> Y, Color32 colour)
        {
            for (var i = 0; i + 1 < arc.Length; i++)
            {
                var x0 = X(arc[i]);
                var y0 = Y(v[i]);
                var x1 = X(arc[i + 1]);
                var y1 = Y(v[i + 1]);
                var steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)) + 1;
                for (var k = 0; k <= steps; k++)
                {
                    var x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, k / (float)steps));
                    var y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, k / (float)steps));
                    if (x >= 0 && x < w && y >= 0 && y < h)
                    {
                        px[y * w + x] = colour;
                    }
                }
            }
        }

        private static void Marker(Color32[] px, int w, int h, int x, int y, Color32 colour)
        {
            for (var dy = -3; dy <= 3; dy++)
            {
                for (var dx = -3; dx <= 3; dx++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > 3)
                    {
                        continue;
                    }

                    var px2 = x + dx;
                    var py = y + dy;
                    if (px2 >= 0 && px2 < w && py >= 0 && py < h)
                    {
                        px[py * w + px2] = colour;
                    }
                }
            }
        }

        public static string RunSweep(string spec, string directory)
        {
            Directory.CreateDirectory(directory);
            var start = 1000;
            var count = 40;
            if (!string.IsNullOrEmpty(spec))
            {
                var parts = spec.Split(':');
                if (parts.Length >= 1)
                {
                    int.TryParse(parts[0], out start);
                }

                if (parts.Length >= 2)
                {
                    int.TryParse(parts[1], out count);
                }
            }

            count = Mathf.Max(1, count);
            var c = CultureInfo.InvariantCulture;
            var report = new StringBuilder();
            report.AppendLine($"Stage 3F F3 drainage sweep seeds {start}..{start + count - 1}");
            report.AppendLine("seed traj rise closed drained drainable basin terminal ambiguous worstClosed compViol");

            var seedsWithClosed = 0;
            var totalClosed = 0;
            var closedAtMergeEnd = 0;
            var totalFindings = 0;
            var worstRise = 0f;
            var worstRiseSeed = -1;
            var worstClosed = 0f;
            var worstClosedSeed = -1;
            var worstClosedTraj = -1;
            var totalCompositionViolations = 0;

            for (var i = 0; i < count; i++)
            {
                var seed = start + i;
                var s = Build(seed);
                var profiles = Verify(s);

                var rise = 0f;
                var counts = new Dictionary<DrainageState, int>();
                var seedWorstClosed = 0f;
                var seedWorstClosedTraj = -1;
                var comp = 0;
                foreach (var p in profiles)
                {
                    rise = Mathf.Max(rise, p.AdverseRiseFinished);
                    comp += p.CompositionViolations;
                    foreach (var f in p.Findings)
                    {
                        counts.TryGetValue(f.State, out var n);
                        counts[f.State] = n + 1;
                        totalFindings++;
                        if (f.State == DrainageState.Closed)
                        {
                            // The verifier walks one trajectory and stops at its end. A tributary's
                            // barrier sitting on its own last sample is therefore a limit of this
                            // measurement, not necessarily a closed hollow: the trunk carries the water
                            // on from there and nothing here follows it.
                            if (p.Termination == ValleyTermination.Merge
                                && f.BarrierArcMeters >= p.LengthMeters - Stage3FVerification.StepMeters(s.Settings))
                            {
                                closedAtMergeEnd++;
                            }

                            if (f.DepthMeters > seedWorstClosed)
                            {
                                seedWorstClosed = f.DepthMeters;
                                seedWorstClosedTraj = f.TrajectoryId;
                            }
                        }
                    }
                }

                var closed = Count(counts, DrainageState.Closed);
                totalClosed += closed;
                totalCompositionViolations += comp;
                if (closed > 0)
                {
                    seedsWithClosed++;
                }

                if (rise > worstRise)
                {
                    worstRise = rise;
                    worstRiseSeed = seed;
                }

                if (seedWorstClosed > worstClosed)
                {
                    worstClosed = seedWorstClosed;
                    worstClosedSeed = seed;
                    worstClosedTraj = seedWorstClosedTraj;
                }

                report.AppendLine(
                    $"{seed} {profiles.Count} {rise.ToString("F2", c)} {closed} "
                    + $"{Count(counts, DrainageState.Drained)} {Count(counts, DrainageState.DrainableWithinTrajectory)} "
                    + $"{Count(counts, DrainageState.BasinOwned)} {Count(counts, DrainageState.Terminal)} "
                    + $"{Count(counts, DrainageState.Ambiguous)} {seedWorstClosed.ToString("F2", c)} {comp}");
            }

            report.AppendLine(
                $"summary: seeds with a CLOSED low {seedsWithClosed}/{count}, total CLOSED {totalClosed}, "
                + $"total findings {totalFindings}, composition violations {totalCompositionViolations}");
            report.AppendLine(
                $"  of those CLOSED, {closedAtMergeEnd} have their barrier on the last sample of a Merge "
                + "trajectory, where this verifier stops and the trunk it hands over to is not followed");
            report.AppendLine(
                $"  worst finished adverse rise {worstRise.ToString("F2", c)} m on seed {worstRiseSeed}; "
                + $"worst CLOSED depth {worstClosed.ToString("F2", c)} m on seed {worstClosedSeed} trajectory {worstClosedTraj}");
            File.WriteAllText(Path.Combine(directory, "stage3f_verify_sweep.txt"), report.ToString());
            return report.ToString();
        }

        private static int[] ReadSeeds(string[] args)
        {
            var raw = ReadArg(args, SeedsArg);
            if (string.IsNullOrEmpty(raw))
            {
                return CanonicalSeeds;
            }

            var seeds = new List<int>();
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), out var value))
                {
                    seeds.Add(value);
                }
            }

            return seeds.Count > 0 ? seeds.ToArray() : CanonicalSeeds;
        }

        private static string ReadArg(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif
