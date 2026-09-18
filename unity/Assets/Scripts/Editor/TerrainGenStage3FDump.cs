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
            Debug.Log(Run(ReadSeeds(args), ReadOutputDirectory(args)));
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3F valley carve - the declared corridors cut into the Stage 4A island");
            sb.AppendLine("  NOT connected to TerrainAuthorityField: production heights are still IslandElevation (H4A).");
            sb.AppendLine("  carve = CarveField as Stage 3C binds it (ValleyCorridor trajectories, not Slice C).");
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

            sw.Restart();
            var h3f = CarvedElevation.EvaluateField(
                world.Stage1, world.H4AField, world.Carve, settings, out var carveField);
            var carveMs = sw.ElapsedMilliseconds;

            var h4a = world.H4A;
            var carve = carveField.Heights;
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

            var before = IslandReliefMetrics.Measure(h4a, world);
            var after = IslandReliefMetrics.Measure(h3f.Heights, world);

            sb.AppendLine($"seed {seed}  massifs {world.Stage2B.Massifs.Count}  corridors {world.Ridges.Valleys.Count}  basins {world.Basins.Basins.Count}");
            sb.AppendLine($"  hashes     H4A {world.H4AField.Hash()}  carve {carveField.Hash()}  H3F {h3f.Hash()}");
            sb.AppendLine($"  timing     world {worldMs} ms, carve lattice {carveMs} ms");
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

            WriteImages(directory, seed, world, h3f, carveField);

            return $"seed={seed} corridors={world.Ridges.Valleys.Count} carveMax={maxCarve.ToString("F2", c)} "
                   + $"landTouched={(100f * carvedLand / Mathf.Max(1, land)).ToString("F1", c)}% "
                   + $"coastAligned={before.CoastAlignedPercent.ToString("F1", c)}->{after.CoastAlignedPercent.ToString("F1", c)} "
                   + $"LRp90={before.FloorReliefP90.ToString("F1", c)}->{after.FloorReliefP90.ToString("F1", c)} "
                   + $"pits={after.Depressions} H3F={h3f.Hash()}";
        }

        private static void Metric(StringBuilder sb, string name, float before, float after)
        {
            var c = CultureInfo.InvariantCulture;
            sb.AppendLine($"    {name,-30} {before.ToString("F2", c),-13} {after.ToString("F2", c)}");
        }

        private static void WriteImages(
            string directory, int seed, IslandReliefWorld world, WorldHeightField h3f, WorldHeightField carve)
        {
            var n = world.Width;
            WritePng(Path.Combine(directory, $"seed_{seed}_01_h4a_shaded.png"), Shade(world.H4A, n, world.Cell), n);
            WritePng(Path.Combine(directory, $"seed_{seed}_02_h3f_shaded.png"), Shade(h3f.Heights, n, world.Cell), n);
            WritePng(Path.Combine(directory, $"seed_{seed}_03_carve.png"), Heat(carve.Heights, n, world.Stage1.SignedDistance), n);
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
