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
    /// Stage 3F, dump-first: the island with its declared valley corridors cut into it, measured
    /// against the Stage 4A surface it was cut from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The question this exists to answer is the one Stage 4A could not: does cutting the frozen valley
    /// structure into the island turn it into terrain with valleys, or does the island still read as a
    /// stack of offset coastlines? The headline number is the coast-aligned gradient share — 95–99 % on
    /// <c>H2</c>, 63–77 % on <c>H4A</c> — and what it does under the carve.
    /// </para>
    /// <para>
    /// Nothing here is on the production path: <c>TerrainAuthorityField</c> evaluates
    /// <see cref="IslandElevation"/>. The carve is read exactly as <see cref="CarveField"/> binds it
    /// today, to the frozen <see cref="ValleyCorridor"/> trajectories; re-binding it to Slice C's
    /// <c>FinalValleyTrajectory</c> needs a scoped unfreeze of Stage 3C and is not done here.
    /// </para>
    /// </remarks>
    public static class TerrainGenStage3FDump
    {
        public static readonly int[] CanonicalSeeds = TerrainGenIslandReliefDump.CanonicalSeeds;

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";
        private const string SweepArg = "-terrainGenSweep";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3F Valley Carve (Canonical Seeds)", false, 221)]
        public static void DumpMenu()
        {
            Debug.Log(Run(CanonicalSeeds, Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3f")));
        }

        /// <summary>
        /// Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenStage3FDump.DumpBatch</c>
        /// <c>[-terrainGenSeeds a,b] [-terrainGenOut dir]</c>.
        /// </summary>
        public static void DumpBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var sweep = ReadArg(args, SweepArg);
            if (!string.IsNullOrEmpty(sweep))
            {
                Debug.Log(RunSweep(sweep, ReadOutputDirectory(args)));
                return;
            }

            Debug.Log(Run(ReadSeeds(args), ReadOutputDirectory(args)));
        }

        /// <summary>
        /// Sweep <c>start:count</c> (default 1000:40): no images, the contract table is the product.
        /// </summary>
        /// <remarks>
        /// What a sweep is for here is the cases the canonical seven cannot show — a projection that
        /// refuses, a carve outside every trajectory's own half-width, an arc past the declared end, a
        /// carve at sea, and how the depression count moves. Not for tuning: no seed is looked at
        /// individually and nothing here changes a setting.
        /// </remarks>
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
            var settings = RidgeSettings.Default;
            settings.Clamp();

            var report = new StringBuilder();
            report.AppendLine($"Stage 3F F1 sweep seeds {start}..{start + count - 1}");
            report.AppendLine("  carve keyed to Slice C FinalValleyTrajectory; production untouched");
            report.AppendLine("seed traj carveMax landTouched pitsH4A pitsH3F badProj outsideSupport seaCarved arcPastEnd climbMax");

            var badSeeds = 0;
            var worstClimb = 0f;
            for (var i = 0; i < count; i++)
            {
                var seed = start + i;
                var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
                var carveModel = world.Trajectories.BuildCarve(
                    world.Stage1.Coast, settings, world.Stage2B.MassifSettings);
                var h3f = CarvedElevation.EvaluateField(
                    world.Stage1, world.H4AField, carveModel, settings, out var carve);

                var sd = world.Stage1.SignedDistance;
                var land = 0;
                var touched = 0;
                var max = 0f;
                var seaCarved = 0;
                for (var k = 0; k < carve.Heights.Length; k++)
                {
                    if (sd[k] <= 0f)
                    {
                        if (carve.Heights[k] != 0f)
                        {
                            seaCarved++;
                        }

                        continue;
                    }

                    land++;
                    max = Mathf.Max(max, carve.Heights[k]);
                    if (carve.Heights[k] > 0.01f)
                    {
                        touched++;
                    }
                }

                var badProjection = 0;
                var arcPastEnd = 0;
                var climbMax = 0f;
                foreach (var traj in world.Trajectories.Trajectories)
                {
                    if (!traj.HasGeometry)
                    {
                        continue;
                    }

                    if (!traj.Project(traj.Points[traj.Points.Length / 2], out var arc, out _))
                    {
                        badProjection++;
                    }
                    else if (arc < -1e-3f || arc > traj.LengthMeters + 1e-3f)
                    {
                        arcPastEnd++;
                    }

                    var previous = float.NaN;
                    foreach (var p in traj.Points)
                    {
                        var h = CarvedElevation.At(
                            world.Stage1, world.Uplift, world.Relief, carveModel, settings, p.x, p.y);
                        if (!float.IsNaN(previous) && h > previous)
                        {
                            climbMax = Mathf.Max(climbMax, h - previous);
                        }

                        previous = h;
                    }
                }

                var outside = CarvedOutsideSupport(world, carve.Heights);
                var before = IslandReliefMetrics.Measure(world.H4A, world);
                var after = IslandReliefMetrics.Measure(h3f.Heights, world);
                worstClimb = Mathf.Max(worstClimb, climbMax);

                var bad = badProjection + outside + seaCarved + arcPastEnd;
                if (bad > 0)
                {
                    badSeeds++;
                }

                report.AppendLine(
                    $"{seed} {world.Trajectories.Trajectories.Count} {max.ToString("F2", c)} "
                    + $"{(100f * touched / Mathf.Max(1, land)).ToString("F1", c)}% {before.Depressions} {after.Depressions} "
                    + $"{badProjection} {outside} {seaCarved} {arcPastEnd} {climbMax.ToString("F2", c)}");
            }

            report.AppendLine(
                $"summary: seeds with a contract violation {badSeeds}/{count}, worst centreline climb "
                + $"{worstClimb.ToString("F2", c)} m (budget {settings.CorridorIncisionMeters.ToString("F2", c)} m)");
            File.WriteAllText(Path.Combine(directory, "stage3f_sweep.txt"), report.ToString());
            return report.ToString();
        }

        /// <summary>Carved samples that no trajectory's own half-width covers. Must be zero.</summary>
        private static int CarvedOutsideSupport(IslandReliefWorld world, float[] carve)
        {
            var field = world.Stage1.Field;
            var outside = 0;
            for (var i = 0; i < carve.Length; i += 29)
            {
                if (carve[i] <= 0.01f)
                {
                    continue;
                }

                var p = new Vector2(field.WorldX(i % field.Width), field.WorldZ(i / field.Width));
                var covered = false;
                foreach (var traj in world.Trajectories.Trajectories)
                {
                    if (traj.HasGeometry && traj.Project(p, out _, out var across)
                        && across < traj.HalfWidthMeters)
                    {
                        covered = true;
                        break;
                    }
                }

                if (!covered)
                {
                    outside++;
                }
            }

            return outside;
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3F valley carve - the declared valleys cut into the Stage 4A island");
            sb.AppendLine("  NOT connected to TerrainAuthorityField: production heights are still IslandElevation (H4A).");
            sb.AppendLine("  F1: carve is keyed to Slice C FinalValleyTrajectory. F0 (legacy ValleyCorridor) is reported beside it.");
            sb.AppendLine($"  output {directory}");
            sb.AppendLine($"  incision bound {RidgeSettings.Default.CorridorIncisionMeters.ToString("F2", c)} m");
            sb.AppendLine();

            var lines = new List<string>();
            foreach (var seed in seeds)
            {
                lines.Add(Append(sb, seed, directory));
            }

            sb.AppendLine();
            sb.AppendLine("one line per seed:");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            File.WriteAllText(Path.Combine(directory, "stage3f_report.txt"), sb.ToString());
            return sb.ToString();
        }

        private static string Append(StringBuilder sb, int seed, string directory)
        {
            var c = CultureInfo.InvariantCulture;
            var sw = Stopwatch.StartNew();
            var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
            var worldMs = sw.ElapsedMilliseconds;

            var settings = RidgeSettings.Default;
            settings.Clamp();

            // F1: Slice C is the geometry authority. F0's corridor-bound carve is evaluated only so the
            // two can be put side by side; nothing downstream reads it.
            var f1Carve = world.Trajectories.BuildCarve(
                world.Stage1.Coast, settings, world.Stage2B.MassifSettings);

            sw.Restart();
            var h3f = CarvedElevation.EvaluateField(
                world.Stage1, world.H4AField, f1Carve, settings, out var carveField);
            var carveMs = sw.ElapsedMilliseconds;

            sw.Restart();
            CarvedElevation.EvaluateField(
                world.Stage1, world.H4AField, world.Carve, settings, out var f0CarveField);
            var f0Ms = sw.ElapsedMilliseconds;

            var h4a = world.H4A;
            var carve = carveField.Heights;
            var f0 = f0CarveField.Heights;
            var n = world.Width;
            var sd = world.Stage1.SignedDistance;

            var carved = 0;
            var carvedLand = 0;
            var land = 0;
            var maxCarve = 0f;
            double sumCarve = 0;
            var seaNonZero = 0;
            var belowFloor = 0;
            var maxIncision = 0f;
            for (var i = 0; i < carve.Length; i++)
            {
                if (sd[i] <= 0f)
                {
                    if (carve[i] != 0f)
                    {
                        seaNonZero++;
                    }

                    continue;
                }

                land++;
                if (carve[i] > 0.01f)
                {
                    carved++;
                    carvedLand++;
                    sumCarve += carve[i];
                }

                maxCarve = Mathf.Max(maxCarve, carve[i]);
                var floor = world.Stage1.Field.Heights[i];
                if (h3f.Heights[i] < floor - 1e-4f)
                {
                    belowFloor++;
                    maxIncision = Mathf.Max(maxIncision, floor - h3f.Heights[i]);
                }
            }

            var f0Max = 0f;
            var f0Touched = 0;
            double f0Sum = 0;
            var diffMax = 0f;
            for (var i = 0; i < f0.Length; i++)
            {
                f0Max = Mathf.Max(f0Max, f0[i]);
                if (f0[i] > 0.01f)
                {
                    f0Touched++;
                    f0Sum += f0[i];
                }

                diffMax = Mathf.Max(diffMax, Mathf.Abs(carve[i] - f0[i]));
            }

            var before = IslandReliefMetrics.Measure(h4a, world);
            var after = IslandReliefMetrics.Measure(h3f.Heights, world);

            sb.AppendLine(
                $"seed {seed}  massifs {world.Stage2B.Massifs.Count}  corridors {world.Ridges.Valleys.Count}  "
                + $"trajectories {world.Trajectories.Trajectories.Count}  basins {world.Basins.Basins.Count}");
            sb.AppendLine($"  hashes     H4A {world.H4AField.Hash()}  carve {carveField.Hash()}  H3F {h3f.Hash()}");
            sb.AppendLine($"  timing     world {worldMs} ms, F1 carve lattice {carveMs} ms, F0 carve lattice {f0Ms} ms");
            sb.AppendLine(
                $"  F0 vs F1   F0 max {f0Max.ToString("F2", c)} m / mean {(f0Touched > 0 ? f0Sum / f0Touched : 0).ToString("F2", c)} m / "
                + $"{(100f * f0Touched / Mathf.Max(1, land)).ToString("F1", c)}% land; "
                + $"F1 max {maxCarve.ToString("F2", c)} m; worst |F1-F0| {diffMax.ToString("F2", c)} m");
            sb.AppendLine(
                $"  carve      max {maxCarve.ToString("F2", c)} m, mean on carved {(carved > 0 ? sumCarve / carved : 0).ToString("F2", c)} m, "
                + $"land touched {(100f * carvedLand / Mathf.Max(1, land)).ToString("F2", c)}%, "
                + $"sea samples carved {seaNonZero}, below-Stage1 samples {belowFloor} (max {maxIncision.ToString("F2", c)} m)");
            sb.AppendLine("  metrics                         H4A           H3F");
            Metric(sb, "coast-aligned gradient %", before.CoastAlignedPercent, after.CoastAlignedPercent);
            Metric(sb, "floor local relief median m", before.FloorReliefMedian, after.FloorReliefMedian);
            Metric(sb, "floor local relief p90 m", before.FloorReliefP90, after.FloorReliefP90);
            Metric(sb, "height spread at fixed d m", before.HeightSpreadAtDistance, after.HeightSpreadAtDistance);
            Metric(sb, "land <5 deg %", before.LandUnder5, after.LandUnder5);
            Metric(sb, "land >45 deg %", before.LandOver45, after.LandOver45);
            Metric(sb, "reachable <=45 %", before.ReachablePercent, after.ReachablePercent);
            Metric(sb, "curvature ratio", before.CurvatureRatio, after.CurvatureRatio);
            sb.AppendLine(
                $"    depressions >=0.25m&100m2      {before.Depressions} ({before.DepressionMaxDepth.ToString("F2", c)} m)"
                + $"   {after.Depressions} ({after.DepressionMaxDepth.ToString("F2", c)} m)");

            AppendTrajectories(sb, world, f1Carve, settings);
            WriteImages(directory, seed, world, h3f, carveField, f0CarveField);

            return $"seed={seed} corridors={world.Ridges.Valleys.Count} carveMax={maxCarve.ToString("F2", c)} "
                   + $"landTouched={(100f * carvedLand / Mathf.Max(1, land)).ToString("F1", c)}% "
                   + $"coastAligned={before.CoastAlignedPercent.ToString("F1", c)}->{after.CoastAlignedPercent.ToString("F1", c)} "
                   + $"LRp90={before.FloorReliefP90.ToString("F1", c)}->{after.FloorReliefP90.ToString("F1", c)} "
                   + $"pits={after.Depressions} H3F={h3f.Hash()}";
        }

        /// <summary>
        /// Per trajectory: what the carve does along the curve it is now keyed to, and the B7 check.
        /// </summary>
        /// <remarks>
        /// B7 asked whether the carvability bound and the carve are on the same curve. They are now, so
        /// the climb along the trajectory and the carve along it can be read off the same samples: the
        /// column is the largest rise in <c>H3F</c> walking downstream, which is what a corridor is not
        /// allowed to have.
        /// </remarks>
        private static void AppendTrajectories(
            StringBuilder sb, IslandReliefWorld world, CarveField carve, RidgeSettings settings)
        {
            var c = CultureInfo.InvariantCulture;
            var trajectories = world.Trajectories.Trajectories;
            if (trajectories.Count == 0)
            {
                sb.AppendLine("  trajectories none - this world certified no massif, so it has no corridors");
                return;
            }

            sb.AppendLine(
                "  id  corr  term         len_m  half_m   toe_m  maxCarve  meanCarve  endCarve  climb_m");
            for (var t = 0; t < trajectories.Count; t++)
            {
                var traj = trajectories[t];
                if (!traj.HasGeometry)
                {
                    sb.AppendLine($"  {traj.Id,-3} {traj.Id,-5} {traj.Termination,-12} (no geometry)");
                    continue;
                }

                var max = 0f;
                double sum = 0;
                var count = 0;
                var climb = 0f;
                var previous = float.NaN;
                for (var i = 0; i < traj.Points.Length; i++)
                {
                    var p = traj.Points[i];
                    var h = CarvedElevation.At(
                        world.Stage1, world.Uplift, world.Relief, carve, settings, p.x, p.y);
                    var sd = world.Stage1.Coast.SignedDistance(p.x, p.y);
                    var raw = carve.Depth(p.x, p.y, carve.GateFor(sd), sd > 0f);
                    var island = CarvedElevation.At(
                        world.Stage1, world.Uplift, world.Relief, null, settings, p.x, p.y);
                    var applied = island - h;

                    max = Mathf.Max(max, applied);
                    sum += applied;
                    count++;
                    if (!float.IsNaN(previous) && h > previous)
                    {
                        climb = Mathf.Max(climb, h - previous);
                    }

                    previous = h;
                    _ = raw;
                }

                var end = traj.Points[traj.Points.Length - 1];
                var endSd = world.Stage1.Coast.SignedDistance(end.x, end.y);
                var endIsland = CarvedElevation.At(
                    world.Stage1, world.Uplift, world.Relief, null, settings, end.x, end.y);
                var endCarve = endIsland - CarvedElevation.At(
                    world.Stage1, world.Uplift, world.Relief, carve, settings, end.x, end.y);
                _ = endSd;

                sb.AppendLine(
                    $"  {traj.Id,-3} {traj.Id,-5} {traj.Termination,-12} "
                    + $"{traj.LengthMeters.ToString("F0", c),6} {traj.HalfWidthMeters.ToString("F0", c),7} "
                    + $"{traj.ToeArcMeters.ToString("F0", c),7} {max.ToString("F2", c),9} "
                    + $"{(count > 0 ? sum / count : 0).ToString("F2", c),10} {endCarve.ToString("F2", c),9} "
                    + $"{climb.ToString("F2", c),8}");
            }
        }

        private static void Metric(StringBuilder sb, string name, float before, float after)
        {
            var c = CultureInfo.InvariantCulture;
            sb.AppendLine($"    {name,-30} {before.ToString("F2", c),-13} {after.ToString("F2", c)}");
        }

        private static void WriteImages(
            string directory, int seed, IslandReliefWorld world, WorldHeightField h3f,
            WorldHeightField carve, WorldHeightField f0Carve)
        {
            var n = world.Width;
            var sd = world.Stage1.SignedDistance;
            WritePng(Path.Combine(directory, $"seed_{seed}_01_h4a_shaded.png"), Shade(world.H4A, n, world.Cell), n);
            WritePng(Path.Combine(directory, $"seed_{seed}_02_h3f_shaded.png"), Shade(h3f.Heights, n, world.Cell), n);
            WritePng(Path.Combine(directory, $"seed_{seed}_03_carve_f1.png"), Heat(carve.Heights, n, sd), n);
            WritePng(Path.Combine(directory, $"seed_{seed}_04_carve_f0.png"), Heat(f0Carve.Heights, n, sd), n);

            var diff = new float[carve.Heights.Length];
            for (var i = 0; i < diff.Length; i++)
            {
                diff[i] = Mathf.Abs(carve.Heights[i] - f0Carve.Heights[i]);
            }

            WritePng(Path.Combine(directory, $"seed_{seed}_05_carve_diff.png"), Heat(diff, n, sd), n);
            WritePng(
                Path.Combine(directory, $"seed_{seed}_06_trajectories.png"),
                Trajectories(world, n), n);
        }

        /// <summary>The Slice C trajectories drawn over the carve, so the two can be seen to agree.</summary>
        private static Color32[] Trajectories(IslandReliefWorld world, int n)
        {
            var px = new Color32[n * n];
            var sd = world.Stage1.SignedDistance;
            for (var i = 0; i < px.Length; i++)
            {
                px[i] = sd[i] <= 0f ? new Color32(10, 18, 34, 255) : new Color32(38, 48, 40, 255);
            }

            var cell = world.Cell;
            foreach (var traj in world.Trajectories.Trajectories)
            {
                if (!traj.HasGeometry)
                {
                    continue;
                }

                for (var i = 0; i + 1 < traj.Points.Length; i++)
                {
                    var a = traj.Points[i];
                    var b = traj.Points[i + 1];
                    var steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / cell));
                    for (var s = 0; s <= steps; s++)
                    {
                        var p = Vector2.Lerp(a, b, s / (float)steps);
                        var x = Mathf.RoundToInt(p.x / cell);
                        var z = Mathf.RoundToInt(p.y / cell);
                        if (x < 0 || z < 0 || x >= n || z >= n)
                        {
                            continue;
                        }

                        // Head bright, endpoint dark, so the direction of travel is readable.
                        var t = traj.LengthMeters > 1e-3f
                            ? Mathf.Clamp01(traj.Cumulative[Mathf.Min(i, traj.Cumulative.Length - 1)] / traj.LengthMeters)
                            : 0f;
                        px[z * n + x] = new Color32(
                            (byte)(255 - 120 * t), (byte)(220 - 150 * t), (byte)(60 + 120 * t), 255);
                    }
                }
            }

            return px;
        }

        /// <summary>Hillshade with a 5 m contour overlay, the same read as the Stage 4A dump's.</summary>
        private static Color32[] Shade(float[] h, int n, float cell)
        {
            var px = new Color32[n * n];
            var sea = WorldScale.SeaLevelMeters;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    var value = h[i];
                    if (value <= sea)
                    {
                        var t = Mathf.Clamp01((sea - value) / 30f);
                        px[i] = new Color32((byte)(30 + 20 * (1 - t)), (byte)(70 + 40 * (1 - t)), (byte)(130 + 60 * (1 - t)), 255);
                        continue;
                    }

                    IslandReliefMetrics.Gradient(h, n, cell, x, z, out var gx, out var gz);
                    var shade = Mathf.Clamp01(0.55f + 0.45f * Vector3.Dot(
                        new Vector3(-gx, 1f, -gz).normalized, new Vector3(-0.5f, 0.75f, -0.43f).normalized));
                    var band = Mathf.Clamp01((value - sea) / 340f);
                    var r = (byte)(shade * (90 + 150 * band));
                    var g = (byte)(shade * (140 + 90 * band));
                    var b = (byte)(shade * (80 + 120 * band));
                    if (Mathf.Repeat(value, 5f) < 0.35f)
                    {
                        r = (byte)(r * 0.45f);
                        g = (byte)(g * 0.45f);
                        b = (byte)(b * 0.45f);
                    }

                    px[i] = new Color32(r, g, b, 255);
                }
            }

            return px;
        }

        private static Color32[] Heat(float[] v, int n, float[] sd)
        {
            var max = 0f;
            for (var i = 0; i < v.Length; i++)
            {
                max = Mathf.Max(max, v[i]);
            }

            max = Mathf.Max(0.01f, max);
            var px = new Color32[n * n];
            for (var i = 0; i < v.Length; i++)
            {
                if (sd[i] <= 0f)
                {
                    px[i] = new Color32(10, 18, 34, 255);
                    continue;
                }

                var t = Mathf.Clamp01(v[i] / max);
                px[i] = new Color32(
                    (byte)(40 + 215 * t), (byte)(40 + 120 * (1 - Mathf.Abs(0.5f - t) * 2f)), (byte)(160 * (1 - t)), 255);
            }

            return px;
        }

        private static void WritePng(string path, Color32[] pixels, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static string ReadOutputDirectory(string[] args)
        {
            var directory = ReadArg(args, OutputArg);
            return string.IsNullOrEmpty(directory)
                ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3f")
                : directory;
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
