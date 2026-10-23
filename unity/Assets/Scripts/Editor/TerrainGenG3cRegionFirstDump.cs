#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3c research candidate (<c>GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md</c> §14, plan in
    /// <c>GEOGRAPHY_TARGET_BRIEF_G2.md</c> §8.3): the approved target geography D6 built region-first on two
    /// fixed shorelines, contour A (production's default world) and contour B (about 40 % more land in the
    /// same world).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Research only, never a terrain authority.</b> Nothing here is referenced by production code, the
    /// bake or a scene. Contour B is a Stage 1 run with a raised land-fraction band, made in memory; no
    /// production setting changes. Lattices, reports and images are written outside <c>Assets</c>. Random
    /// draws use <c>research.g3c.*</c> streams, which no production stage reads.
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.TerrainGenG3cRegionFirstDump.DumpBatch \
    ///   -g3cStep contours|run [-g3cSeeds a,b] [-g3cContours A,B] [-g3cConfigs s20-med,s30-off]
    ///   [-g3cViews true|false] -g3cOut &lt;folder outside Assets&gt;
    /// </code>
    /// </remarks>
    public static class TerrainGenG3cRegionFirstDump
    {
        private const string StepArg = "-g3cStep";
        private const string OutputArg = "-g3cOut";

        public static void DumpBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var directory = TerrainGenGeographyDump.ReadArg(args, OutputArg);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "g3c");
            }

            Directory.CreateDirectory(directory);
            var step = TerrainGenGeographyDump.ReadArg(args, StepArg) ?? "contours";
            switch (step)
            {
                case "contours":
                    Debug.Log(G3cContours.Report(directory));
                    break;
                case "run":
                    var seeds = ParseInts(TerrainGenGeographyDump.ReadArg(args, "-g3cSeeds"));
                    var contours = (TerrainGenGeographyDump.ReadArg(args, "-g3cContours") ?? "A,B").Split(',');
                    var configs = (TerrainGenGeographyDump.ReadArg(args, "-g3cConfigs") ?? string.Join(",", G3cConfig.All)).Split(',');
                    var views = TerrainGenGeographyDump.ReadArg(args, "-g3cViews") == "true";
                    Debug.Log(Run(directory, seeds, contours, configs, views));
                    break;
                default:
                    throw new ArgumentException($"Unknown {StepArg} '{step}'.");
            }
        }

        public static string Run(string directory, int[] seeds, string[] contourIds, string[] configIds, bool views)
        {
            var c = CultureInfo.InvariantCulture;
            var settings = G3cSettings.Default;
            var report = new StringBuilder();
            var summary = new StringBuilder();
            var timings = new StringBuilder("contour config seed build_ms measure_ms views_ms\n");
            report.AppendLine("G3c research run - region-first geography on fixed contours (research only; nothing here is read by production)");
            foreach (var line in settings.Lines())
            {
                report.AppendLine("  setting " + line);
            }

            var a = G3cContours.BuildA();
            report.AppendLine($"  contour A Stage 1 hash {a.FieldHash} (pinned {G3cContours.PinnedStage1Hash110201}: {(a.FieldHash == G3cContours.PinnedStage1Hash110201 ? "PASS" : "FAIL")})");
            var built = new Dictionary<string, G3cContour> { ["A"] = a };
            foreach (var id in contourIds)
            {
                if (id.Trim() != "B" || built.ContainsKey("B"))
                {
                    continue;
                }

                var b = G3cContours.BuildB(report);
                if (b == null)
                {
                    report.AppendLine("  contour B unavailable: no attempt passed the acceptance rule");
                    continue;
                }

                b.PlateauScale = b.LandAreaSqm / a.LandAreaSqm;
                built["B"] = b;
                report.AppendLine($"  contour B Stage 1 hash {b.FieldHash}, coast-distance hash {b.SdHash}, plateau scale {b.PlateauScale.ToString("F3", c)}");
            }

            foreach (var kv in built)
            {
                report.AppendLine("  " + G3cContours.ShapeLine(kv.Value));
            }

            report.AppendLine();
            var watch = new System.Diagnostics.Stopwatch();
            G3cViews viewer = null;
            try
            {
                if (views)
                {
                    viewer = new G3cViews();
                    viewer.Begin();
                }

                foreach (var contourId in contourIds)
                {
                    if (!built.TryGetValue(contourId.Trim(), out var contour))
                    {
                        continue;
                    }

                    foreach (var configId in configIds)
                    {
                        var config = G3cConfig.Parse(configId);
                        foreach (var seed in seeds)
                        {
                            var tag = $"{contour.Id}_{config.Id}_{seed}";
                            watch.Restart();
                            var result = G3cGeography.Build(contour, config, seed, settings);
                            var buildMs = watch.ElapsedMilliseconds;
                            watch.Restart();
                            var readings = G3cMeasure.Measure(result, contour, out var analysis);
                            var measureMs = watch.ElapsedMilliseconds;
                            var field = new WorldHeightField(0f, 0f, result.N, result.N, result.Cell, result.Heights);
                            var hash = field.Hash();
                            G3cContours.WriteFloats(Path.Combine(directory, $"g3c_{tag}.f32"), result.Heights);
                            File.WriteAllBytes(Path.Combine(directory, $"g3c_{tag}_region.u8"), result.Region);
                            G3cImages.WriteStructure(Path.Combine(directory, $"g3c_{tag}_structure.png"), result, readings, analysis);

                            report.AppendLine($"[{tag}] lattice hash {hash}");
                            report.AppendLine($"[{tag}] layout: end {result.End}, spine {result.Spine.Count} pts, culminations {result.Culminations.Count}, waist ({result.Waist.x.ToString("F0", c)}, {result.Waist.y.ToString("F0", c)}), lowlands {result.LowlandSeeds.Count}, outlets {result.Outlets.Count} ({result.PrimaryOutlets} primary), pass pair {result.PassPairA}/{result.PassPairB}, plateaus {result.Plateaus.Count}, rivers {result.Rivers.Count}, nodes {result.NodePos.Count}");
                            report.AppendLine($"[{tag}] shares: highland {(100f * result.HighlandShareActual).ToString("F1", c)} %, lowland {(100f * result.LowlandShareActual).ToString("F1", c)} %, upland {(100f * result.UplandShareActual).ToString("F1", c)} %");
                            var byRegion = new int[4];
                            foreach (var p in result.NodePos)
                            {
                                byRegion[Mathf.Min(3, (int)result.Region[G3cGrid.Index(p, result.N, result.Cell)])]++;
                            }

                            report.AppendLine($"[{tag}] river nodes by region lowland/upland/highland: {byRegion[1]}/{byRegion[2]}/{byRegion[3]}; highland refusals spacing/ceiling/floor {result.AdmitFailsByRegion[3, 3]}/{result.AdmitFailsByRegion[3, 4]}/{result.AdmitFailsByRegion[3, 5]}; upland {result.AdmitFailsByRegion[2, 3]}/{result.AdmitFailsByRegion[2, 4]}/{result.AdmitFailsByRegion[2, 5]}");
                            var culmLine = new StringBuilder();
                            foreach (var q in result.Culminations)
                            {
                                var qi = G3cGrid.Index(q, result.N, result.Cell);
                                culmLine.Append($" ({q.x.ToString("F0", c)}, {q.y.ToString("F0", c)}) h {result.Heights[qi].ToString("F0", c)} U {result.Uplift[qi].ToString("F0", c)};");
                            }

                            var wi = G3cGrid.Index(result.Waist, result.N, result.Cell);
                            report.AppendLine($"[{tag}] culminations:{culmLine} waist h {result.Heights[wi].ToString("F0", c)} U {result.Uplift[wi].ToString("F0", c)}");
                            report.AppendLine($"[{tag}] plateau-foot fill: {result.FootFillCells} cells, {result.FootFillVolume.ToString("F0", c)} m3");
                            report.AppendLine($"[{tag}] river steps refused: outside/sea {result.AdmitFails[0]}, blocked {result.AdmitFails[1]}, shore {result.AdmitFails[2]}, spacing {result.AdmitFails[3]}, ceiling {result.AdmitFails[4]}, floor {result.AdmitFails[5]}");
                            foreach (var note in result.Notes)
                            {
                                report.AppendLine($"[{tag}] note: {note}");
                            }

                            foreach (var line in readings.DefectLines)
                            {
                                report.AppendLine($"[{tag}] {line}");
                            }

                            foreach (var line in readings.PlateauLines)
                            {
                                report.AppendLine($"[{tag}] {line}");
                            }

                            foreach (var site in readings.SiteList)
                            {
                                report.AppendLine($"[{tag}] site r={site.RadiusMeters.ToString("F0", c)} m at ({site.Position.x.ToString("F0", c)}, {site.Position.y.ToString("F0", c)}) coastal={site.Coastal} region={site.Region} meanSlope={site.MeanSlope.ToString("F1", c)}");
                            }

                            report.AppendLine($"[{tag}] {readings.Line(c)}");
                            summary.AppendLine($"{tag} hash={hash} {readings.Line(c)}");

                            var viewsMs = 0L;
                            if (viewer != null)
                            {
                                watch.Restart();
                                viewer.Render(directory, tag, contour, result, readings);
                                viewsMs = watch.ElapsedMilliseconds;
                            }

                            timings.AppendLine($"{contour.Id} {config.Id} {seed} {buildMs} {measureMs} {viewsMs}");
                        }
                    }
                }
            }
            finally
            {
                viewer?.End();
            }

            report.AppendLine();
            report.AppendLine("one line per contour, configuration and seed:");
            report.Append(summary);
            File.WriteAllText(Path.Combine(directory, "g3c_report.txt"), report.ToString());
            File.WriteAllText(Path.Combine(directory, "g3c_summary.txt"), summary.ToString());
            File.WriteAllText(Path.Combine(directory, "g3c_timings.txt"), timings.ToString());
            return summary.ToString();
        }

        private static int[] ParseInts(string raw)
        {
            var values = new List<int>();
            if (string.IsNullOrEmpty(raw))
            {
                return new[] { 110201, 1337 };
            }

            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                {
                    values.Add(v);
                }
            }

            return values.ToArray();
        }
    }

    /// <summary>One fixed shoreline: a Stage 1 world whose land mask every seed of a footprint shares.</summary>
    public sealed class G3cContour
    {
        public string Id;
        public int StageSeed;
        public float LandFractionMin;
        public float LandFractionMax;
        public Stage1Result Stage1;
        public int N;
        public float Cell;
        public bool[] Land;
        public bool[] Mainland;
        public float[] Sd;
        public float LandAreaSqm;
        public int LandComponents;
        public bool InsideMargin;
        public string FieldHash;
        public string SdHash;

        /// <summary>Plateau top areas scale with land on B (brief §8.3): land / contour A's land.</summary>
        public float PlateauScale = 1f;

        public float[] Ocean => Stage1.Field.Heights;
    }

    /// <summary>
    /// Step 0 of the G3c plan: contour A is the Stage 1 coastline of the default world, seed 110201;
    /// contour B is seed 110201's Stage 1 with the land-fraction band raised to about 0.50, accepted by a rule
    /// written before any interior run, with pre-registered fallbacks.
    /// </summary>
    public static class G3cContours
    {
        public const int ContourSeed = 110201;

        /// <summary>Stage 1 field hash of seed 110201 pinned by <c>Stage1HeightField_MatchesItsRecordedHashes</c>.</summary>
        public const string PinnedStage1Hash110201 = "7599879a71250a06";

        public const float BLandMinSqm = 3_800_000f;
        public const float BLandMaxSqm = 4_100_000f;

        /// <summary>The B attempts in their pre-registered order (brief §8.3).</summary>
        public static readonly (int seed, float min, float max)[] BAttempts =
        {
            (110201, 0.49f, 0.52f),
            (110201, 0.48f, 0.52f),
            (424242, 0.49f, 0.52f),
            (20260905, 0.49f, 0.52f),
        };

        public static G3cContour BuildA()
        {
            var settings = TerrainGenSettings.Default;
            return Build("A", ContourSeed, settings);
        }

        /// <summary>The first B attempt that passes the acceptance rule, or null.</summary>
        public static G3cContour BuildB(StringBuilder log)
        {
            foreach (var (seed, min, max) in BAttempts)
            {
                var settings = TerrainGenSettings.Default;
                settings.LandFractionTargetMin = min;
                settings.LandFractionTargetMax = max;
                var contour = Build("B", seed, settings);
                var accepted = Accept(contour, out var why);
                log?.AppendLine($"  B attempt seed {seed} band [{min.ToString("F2", CultureInfo.InvariantCulture)}, {max.ToString("F2", CultureInfo.InvariantCulture)}]: land {(contour.LandAreaSqm / 1e6f).ToString("F3", CultureInfo.InvariantCulture)} km2, components {contour.LandComponents}, inside margin {contour.InsideMargin} -> {(accepted ? "ACCEPTED" : "rejected: " + why)}");
                if (accepted)
                {
                    return contour;
                }
            }

            return null;
        }

        public static bool Accept(G3cContour c, out string why)
        {
            var maxComponents = 1 + TerrainGenSettings.Default.MaxIslets;
            if (c.LandAreaSqm < BLandMinSqm || c.LandAreaSqm > BLandMaxSqm)
            {
                why = "land outside 3.8-4.1 km2";
                return false;
            }

            if (c.LandComponents > maxComponents)
            {
                why = $"more than {maxComponents} land components";
                return false;
            }

            if (!c.InsideMargin)
            {
                why = "land inside the ocean margin";
                return false;
            }

            why = "";
            return true;
        }

        public static G3cContour Build(string id, int seed, TerrainGenSettings settings)
        {
            var stage1 = Stage1LandmassGenerator.Generate(seed, settings);
            var field = stage1.Field;
            var n = field.Width;
            var cell = field.CellSize;
            var count = n * n;
            var c = new G3cContour
            {
                Id = id,
                StageSeed = seed,
                LandFractionMin = settings.LandFractionTargetMin,
                LandFractionMax = settings.LandFractionTargetMax,
                Stage1 = stage1,
                N = n,
                Cell = cell,
                Land = new bool[count],
                Sd = stage1.SignedDistance,
                FieldHash = field.Hash(),
            };

            var sea = WorldScale.SeaLevelMeters;
            var margin = WorldScale.IslandOceanMarginMeters;
            var landCount = 0;
            c.InsideMargin = true;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    c.Land[i] = field.Heights[i] > sea;
                    if (!c.Land[i])
                    {
                        continue;
                    }

                    landCount++;
                    var wx = field.WorldX(x);
                    var wz = field.WorldZ(z);
                    if (wx < margin || wz < margin || wx > WorldScale.WorldExtentXMeters - margin || wz > WorldScale.WorldExtentZMeters - margin)
                    {
                        c.InsideMargin = false;
                    }
                }
            }

            c.LandAreaSqm = landCount * cell * cell;
            var label = GeographyGrid.Components8(c.Land, n, out var components);
            c.LandComponents = components;
            var sizes = GeographyGrid.ComponentSizes(label, components);
            var largest = 0;
            for (var k = 1; k < sizes.Length; k++)
            {
                if (sizes[k] > sizes[largest])
                {
                    largest = k;
                }
            }

            c.Mainland = new bool[count];
            for (var i = 0; i < count; i++)
            {
                c.Mainland[i] = label[i] == largest && components > 0;
            }

            c.SdHash = new WorldHeightField(field.OriginX, field.OriginZ, n, n, cell, c.Sd).Hash();
            return c;
        }

        /// <summary>Shape statistics reported for A and B side by side (brief §8.3).</summary>
        public static string ShapeLine(G3cContour c)
        {
            var inv = CultureInfo.InvariantCulture;
            var n = c.N;
            var cell = c.Cell;
            float maxSd = 0f;
            int minX = n, minZ = n, maxX = 0, maxZ = 0;
            var land = 0;
            var band60 = 0;
            var band200 = 0;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!c.Land[i])
                    {
                        continue;
                    }

                    land++;
                    maxSd = Mathf.Max(maxSd, c.Sd[i]);
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minZ = Mathf.Min(minZ, z);
                    maxZ = Mathf.Max(maxZ, z);
                    if (c.Sd[i] <= 60f)
                    {
                        band60++;
                    }
                    else if (c.Sd[i] <= 200f)
                    {
                        band200++;
                    }
                }
            }

            var stats = c.Stage1.Stats;
            return $"contour {c.Id}: Stage 1 seed {c.StageSeed}, land-fraction band [{c.LandFractionMin.ToString("F2", inv)}, {c.LandFractionMax.ToString("F2", inv)}], " +
                   $"land {(c.LandAreaSqm / 1e6f).ToString("F3", inv)} km2, components {c.LandComponents}, islets {stats.IsletCount}, " +
                   $"inscribed radius {maxSd.ToString("F0", inv)} m, bounding box {((maxX - minX) * cell).ToString("F0", inv)} x {((maxZ - minZ) * cell).ToString("F0", inv)} m, " +
                   $"crenulation {stats.CoastCrenulation.ToString("F2", inv)}, coast {stats.CoastLengthMeters.ToString("F0", inv)} m, " +
                   $"land within 60 m of the sea {(100f * band60 / Mathf.Max(1, land)).ToString("F1", inv)} %, 60-200 m {(100f * band200 / Mathf.Max(1, land)).ToString("F1", inv)} %, " +
                   $"Stage 1 field hash {c.FieldHash}, coast-distance hash {c.SdHash}";
        }

        public static string Report(string directory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("G3c step 0 - contours (research only; Stage 1 in memory, no production setting changed)");
            var a = BuildA();
            var pinned = a.FieldHash == PinnedStage1Hash110201 ? "PASS" : "FAIL";
            sb.AppendLine($"  A: Stage 1 field hash {a.FieldHash} against pinned {PinnedStage1Hash110201}: {pinned}");
            sb.AppendLine("  " + ShapeLine(a));
            var b = BuildB(sb);
            if (b == null)
            {
                sb.AppendLine("  B: no attempt passed the acceptance rule - the scale half of G3c stops here (brief §8.3)");
            }
            else
            {
                sb.AppendLine("  " + ShapeLine(b));
                WriteFloats(Path.Combine(directory, "g3c_contour_B_sd.f32"), b.Sd);
                WritePlan(Path.Combine(directory, "g3c_contour_B.png"), b);
            }

            WriteFloats(Path.Combine(directory, "g3c_contour_A_sd.f32"), a.Sd);
            WritePlan(Path.Combine(directory, "g3c_contour_A.png"), a);
            File.WriteAllText(Path.Combine(directory, "g3c_contours.txt"), sb.ToString());
            return sb.ToString();
        }

        public static void WriteFloats(string path, float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(path, bytes);
        }

        private static void WritePlan(string path, G3cContour c)
        {
            var n = c.N;
            var size = 1024;
            var pixels = new Color32[size * size];
            for (var pz = 0; pz < size; pz++)
            {
                for (var px = 0; px < size; px++)
                {
                    var x = Mathf.Min(n - 1, px * n / size);
                    var z = Mathf.Min(n - 1, pz * n / size);
                    var i = z * n + x;
                    if (!c.Land[i])
                    {
                        pixels[pz * size + px] = new Color32(40, 70, 120, 255);
                        continue;
                    }

                    var t = Mathf.Clamp01(c.Sd[i] / 600f);
                    var g = (byte)(120 + 100 * t);
                    pixels[pz * size + px] = c.Mainland[i] ? new Color32(g, g, (byte)(g - 20), 255) : new Color32(200, 120, 80, 255);
                }
            }

            GeographyImages.WritePng(path, pixels, size);
        }
    }
}
#endif
