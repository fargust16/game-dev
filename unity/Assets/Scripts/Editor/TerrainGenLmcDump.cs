#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3e (working name LMC) — batch entry for Step 1 (technical specification <c>GEOGRAPHY_LMC_TECHNICAL_SPEC.md</c> §10;
    /// readiness <c>GEOGRAPHY_LMC_STEP1_READINESS.md</c> C6–C7). Editor-only research, referenced by nothing: it reads
    /// lattices, writes only outside <c>Assets</c>, never saves a scene, a <c>TerrainData</c> or an asset, and refuses any
    /// seed outside the calibration list. The protected manifest is hashed before and after every run.
    /// </summary>
    public static class TerrainGenLmcDump
    {
        public static readonly int[] CalibrationSeeds = { 110201, 1337 };
        public static readonly float[] G3cAngles = { 2.0f, 5.7f, 14.0f, 20.0f, 35.0f, 38.7f, 56.3f, 75.0f };

        public static void DumpBatch()
        {
            var code = 0;
            try
            {
                code = Run(System.Environment.GetCommandLineArgs());
            }
            catch (Exception e)
            {
                Debug.LogError("G3e: exception " + e);
                code = 2;
            }

            EditorApplication.Exit(code);
        }

        private static string Arg(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }

            return null;
        }

        private static int Run(string[] args)
        {
            var step = Arg(args, "-lmcStep") ?? "continuity";
            var outDir = Arg(args, "-lmcOut");
            if (string.IsNullOrEmpty(outDir)) throw new ArgumentException("-lmcOut is required");
            outDir = Path.GetFullPath(outDir);
            var assets = Path.GetFullPath(Application.dataPath);
            if (outDir.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("output inside Assets is refused");
            Directory.CreateDirectory(outDir);
            var manifest = Arg(args, "-lmcProtected");
            var log = new StringBuilder();
            log.AppendLine($"G3e step {step} — {DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture)}");
            if (!Protected(manifest, log, "before"))
            {
                File.WriteAllText(Path.Combine(outDir, "run_" + step + ".log"), log.ToString());
                return 3;
            }

            var seeds = ParseSeeds(Arg(args, "-lmcSeeds"));
            foreach (var s in seeds)
            {
                if (Array.IndexOf(CalibrationSeeds, s) < 0)
                {
                    log.AppendLine($"REFUSED: seed {s} is not a calibration seed (110201, 1337); nothing generated");
                    File.WriteAllText(Path.Combine(outDir, "run_" + step + ".log"), log.ToString());
                    return 4;
                }
            }

            switch (step)
            {
                case "continuity":
                    Continuity(Arg(args, "-lmcRef"), outDir, log);
                    break;
                case "calibrate":
                    Calibrate(seeds, (Arg(args, "-lmcSynth") ?? "lm3,lm1").Split(','), Arg(args, "-lmcViews") == "true", outDir, log, Arg(args, "-lmcDiag") == "true");
                    break;
                case "views":
                    ViewsStep(seeds, Arg(args, "-lmcIn"), Arg(args, "-lmcRef"), outDir, log);
                    break;
                default:
                    throw new ArgumentException($"unknown -lmcStep '{step}'");
            }

            var ok = Protected(manifest, log, "after");
            File.WriteAllText(Path.Combine(outDir, "run_" + step + ".log"), log.ToString());
            return ok ? 0 : 3;
        }

        private static int[] ParseSeeds(string text)
        {
            if (string.IsNullOrEmpty(text)) return (int[])CalibrationSeeds.Clone();
            var parts = text.Split(',');
            var seeds = new int[parts.Length];
            for (var i = 0; i < parts.Length; i++) seeds[i] = int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            return seeds;
        }

        /// <summary>C6: every protected file must keep the MD5 recorded before Step 1.</summary>
        public static bool Protected(string manifest, StringBuilder log, string when)
        {
            if (string.IsNullOrEmpty(manifest) || !File.Exists(manifest))
            {
                log.AppendLine($"protected manifest {when}: MISSING ({manifest}) — run refused");
                return false;
            }

            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var bad = 0;
            var total = 0;
            foreach (var line in File.ReadAllLines(manifest))
            {
                if (line.Length < 34) continue;
                var expected = line.Substring(0, 32);
                var rel = line.Substring(33).TrimStart('*');
                total++;
                var path = Path.Combine(root, rel);
                var actual = File.Exists(path) ? Md5(path) : "missing";
                if (actual != expected)
                {
                    bad++;
                    log.AppendLine($"  PROTECTED CHANGE {rel}: {expected} -> {actual}");
                }
            }

            log.AppendLine($"protected manifest {when}: {total - bad}/{total} unchanged");
            return bad == 0 && total > 0;
        }

        public static string Md5(string path)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(path))
            {
                var bytes = md5.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        public static float[] ReadFloats(string path, int count)
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length != count * 4) throw new InvalidDataException($"{path}: {bytes.Length} bytes, expected {count * 4}");
            var data = new float[count];
            Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);
            return data;
        }

        public static void WriteFloats(string path, float[] data)
        {
            var bytes = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(path, bytes);
        }

        // ------------------------------------------------------------------ calibration (seeds 110201 and 1337 only)

        private static void Calibrate(int[] seeds, string[] synths, bool views, string outDir, StringBuilder log, bool diag = false)
        {
            var c = CultureInfo.InvariantCulture;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var contour = LmcContour.A();
            log.AppendLine($"contour A: Stage 1 hash {contour.FieldHash} (pinned {LmcContour.PinnedStage1Hash110201}) {(contour.FieldHash == LmcContour.PinnedStage1Hash110201 ? "OK" : "MISMATCH")}, land {(contour.LandAreaSqm / 1e6f).ToString("F3", c)} km2");
            if (contour.FieldHash != LmcContour.PinnedStage1Hash110201) throw new InvalidOperationException("contour A does not match its pinned Stage 1 hash");
            var world = IslandReliefWorld.Build(110201, IslandReliefSettings.Default, withStage3: true);
            var h4a = world.H4AField.Hash();
            var pinned = TerrainGenGeographyDump.PinnedH4AHashes[110201];
            log.AppendLine($"production H4A 110201: {h4a} (pinned {pinned}) {(h4a == pinned ? "OK" : "MISMATCH")}");
            if (h4a != pinned) throw new InvalidOperationException("production H4A hash changed");
            log.AppendLine($"fixtures: {watch.Elapsed.TotalSeconds.ToString("F1", c)} s");
            var layoutSettings = LmcLayoutSettings.Default;
            var lm3Settings = LmcFluvialSettings.Default;
            var lm1Settings = LmcSkeletonSettings.Default;
            var settingsLines = new StringBuilder();
            foreach (var line in layoutSettings.Lines()) settingsLines.AppendLine(line);
            foreach (var line in lm3Settings.Lines()) settingsLines.AppendLine(line);
            foreach (var line in lm1Settings.Lines()) settingsLines.AppendLine(line);
            File.WriteAllText(Path.Combine(outDir, "settings.txt"), settingsLines.ToString());
            var json = new StringBuilder();
            var hashes = new StringBuilder();
            foreach (var seed in seeds)
            {
                var dir = Path.Combine(outDir, "seed_" + seed);
                Directory.CreateDirectory(dir);
                var diagDir = diag ? Path.Combine(dir, "diag") : null;
                if (diagDir != null) Directory.CreateDirectory(diagDir);
                watch.Restart();
                var layout = LmcLayoutBuilder.Build(contour, seed, layoutSettings);
                File.WriteAllText(Path.Combine(dir, "layout.txt"), layout.Lines());
                File.WriteAllBytes(Path.Combine(dir, "layout_region.u8"), layout.Region);
                var inst = new byte[layout.Instance.Length];
                for (var i = 0; i < inst.Length; i++) inst[i] = (byte)(layout.Instance[i] < 0 ? 255 : layout.Instance[i]);
                File.WriteAllBytes(Path.Combine(dir, "layout_instance.u8"), inst);
                File.WriteAllBytes(Path.Combine(dir, "layout_shore_intent.u8"), layout.ShoreIntent);
                if (diagDir != null) File.WriteAllText(Path.Combine(diagDir, "layout_polylines.json"), LayoutJson(layout));
                hashes.AppendLine($"{seed} layout {layout.Hash}");
                log.AppendLine($"seed {seed}: layout {layout.Hash} valid {layout.Valid} ({watch.Elapsed.TotalSeconds.ToString("F1", c)} s)");
                foreach (var check in layout.Checks) log.AppendLine("  " + check);
                if (!layout.Valid)
                {
                    log.AppendLine($"seed {seed}: layout fails validation — not synthesised (spec §4.3)");
                    continue;
                }

                foreach (var synth in synths)
                {
                    watch.Restart();
                    LmcSurface surface;
                    switch (synth.Trim())
                    {
                        case "lm3":
                            var fluvialDiag = diagDir != null ? new LmcFluvialDiag() : null;
                            surface = LmcFluvial.Run(layout, contour, lm3Settings, seed, fluvialDiag);
                            if (fluvialDiag != null)
                            {
                                File.WriteAllText(Path.Combine(diagDir, "lm3_iterations.csv"), fluvialDiag.Csv.ToString());
                                foreach (var kv in fluvialDiag.ChangeMaps) File.WriteAllBytes(Path.Combine(diagDir, "lm3_changes_" + kv.Key.Replace(' ', '_') + ".u8"), kv.Value);
                            }

                            break;
                        case "lm1": surface = LmcSkeleton.Build(layout, contour, lm1Settings, seed, diagDir); break;
                        default: throw new ArgumentException($"unknown synthesis '{synth}'");
                    }

                    var buildSeconds = watch.Elapsed.TotalSeconds;
                    watch.Restart();
                    var subject = new LmcSubject
                    {
                        Name = $"{synth}_{seed}", Seed = seed, N = layout.N, Cell = layout.Cell, H = surface.H, Region = layout.Region, Instance = layout.Instance,
                        Layout = layout, Contour = contour, ConfiguredAngles = surface.Angles, Axes = surface.Axes,
                        ChannelReceivers = surface.Receivers, ChannelArea = surface.Area,
                    };
                    var readings = LmcReadingsBuilder.Measure(subject, out _, out _);
                    var measureSeconds = watch.Elapsed.TotalSeconds;
                    var tag = $"{synth}_{seed}";
                    WriteFloats(Path.Combine(dir, tag + ".f32"), surface.H);
                    var md5 = Md5(Path.Combine(dir, tag + ".f32"));
                    hashes.AppendLine($"{seed} {synth} lattice {md5}");
                    var record = new Dictionary<string, object>
                    {
                        ["synth"] = synth, ["seed"] = seed, ["layout_hash"] = layout.Hash, ["lattice_md5"] = md5,
                        ["build_seconds"] = buildSeconds, ["measure_seconds"] = measureSeconds,
                        ["construction"] = surface.Values, ["construction_report"] = surface.Report, ["construction_defects"] = surface.Defects,
                        ["readings"] = readings.Values, ["depression_lines"] = readings.Lines,
                    };
                    json.AppendLine(LmcJson.Write(record));
                    File.WriteAllText(Path.Combine(dir, tag + "_readings.json"), LmcJson.Write(record));
                    var gates = new StringBuilder();
                    foreach (var kv in readings.Gates) gates.Append($"{kv.Key}={(kv.Value ? "Y" : "n")} ");
                    log.AppendLine($"seed {seed} {synth}: build {buildSeconds.ToString("F1", c)} s, measure {measureSeconds.ToString("F1", c)} s, lattice {md5}; {gates}");
                    foreach (var line in surface.Report) log.AppendLine("    " + line);
                    foreach (var line in surface.Defects) log.AppendLine("    DEFECT " + line);
                    foreach (var flag in readings.Flags) log.AppendLine("    FLAG " + flag);
                    if (views) TerrainGenLmcViews.Render(dir, tag, surface.H, layout, contour, readings);
                }
            }

            File.WriteAllText(Path.Combine(outDir, "calibration.jsonl"), json.ToString());
            File.WriteAllText(Path.Combine(outDir, "hashes.txt"), hashes.ToString());
        }

        /// <summary>Diagnostic run 6: the layout's ridges, trunks, pass corridors, nodes, outlets and plateaus as JSON.</summary>
        private static string LayoutJson(LmcLayout L)
        {
            List<object> Pts(List<Vector2> pts)
            {
                var list = new List<object>();
                foreach (var q in pts) list.Add(new List<object> { q.x, q.y });
                return list;
            }

            var ridges = new List<object>();
            foreach (var r in L.Ridges) ridges.Add(new Dictionary<string, object> { ["id"] = r.Id, ["parent"] = r.Parent, ["points"] = Pts(r.Points) });
            var trunks = new List<object>();
            for (var k = 0; k < L.Trunks.Count; k++) trunks.Add(new Dictionary<string, object> { ["id"] = L.Trunks[k].Id, ["basin"] = L.TrunkBasin[k], ["points"] = Pts(L.Trunks[k].Points) });
            var passes = new List<object>();
            foreach (var p in L.PassCorridors) passes.Add(new Dictionary<string, object> { ["id"] = p.Id, ["points"] = Pts(p.Points) });
            var nodes = new List<object>();
            foreach (var nd in L.Nodes) nodes.Add(new Dictionary<string, object> { ["kind"] = nd.Kind, ["ridge"] = nd.Ridge, ["arc"] = nd.Arc, ["xz"] = new List<object> { nd.P.x, nd.P.y }, ["target"] = nd.Target });
            var outlets = new List<object>();
            for (var k = 0; k < L.Outlets.Count; k++) outlets.Add(new Dictionary<string, object> { ["basin"] = L.OutletBasin[k], ["xz"] = new List<object> { L.Outlets[k].x, L.Outlets[k].y } });
            var plateaus = new List<object>();
            foreach (var pl in L.Plateaus)
            {
                plateaus.Add(new Dictionary<string, object>
                {
                    ["id"] = pl.Id, ["centre"] = new List<object> { pl.Centre.x, pl.Centre.y }, ["edge_target"] = pl.EdgeTarget,
                    ["ramp_bearing"] = pl.RampBearing, ["outlet_bearings"] = new List<float>(pl.OutletBearings), ["band_width"] = pl.BandWidth,
                });
            }

            return LmcJson.Write(new Dictionary<string, object>
            {
                ["hash"] = L.Hash, ["pass_basins"] = new List<object> { L.PassBasinA, L.PassBasinB }, ["ridges"] = ridges, ["trunks"] = trunks,
                ["pass_corridors"] = passes, ["nodes"] = nodes, ["outlets"] = outlets, ["plateaus"] = plateaus,
            });
        }

        // ------------------------------------------------------------------ views and blinded sheets (spec §9)

        /// <summary>
        /// Rebuilds each calibration layout (its hash must equal the calibration run's — V-L8 across processes), renders
        /// the layout's camera set on X, Y, production H4A 110201 and G3c's contour-A lattice for the seed, runs G1 on X, Y
        /// and H4A, and composes the blinded sheets. Which synthesis is X comes from a key written only to <c>sealed/</c>.
        /// </summary>
        private static void ViewsStep(int[] seeds, string inDir, string refDir, string outDir, StringBuilder log)
        {
            if (string.IsNullOrEmpty(inDir)) throw new ArgumentException("-lmcIn (the calibration output) is required");
            var contour = LmcContour.A();
            var world = IslandReliefWorld.Build(110201, IslandReliefSettings.Default, withStage3: true);
            var h4a = world.H4A;
            var sealedDir = Path.Combine(outDir, "sealed");
            var sheets = Path.Combine(outDir, "sheets");
            Directory.CreateDirectory(sealedDir);
            Directory.CreateDirectory(sheets);
            var key = new StringBuilder("G3e blind key — which synthesis is X on each seed. Sealed: not printed on any sheet.\n");
            var index = new StringBuilder("G3e calibration contact sheets — panels: top left X, top right Y, bottom left production H4A 110201 (fixed reference), bottom right G3c contour A s30-med of the same seed (historical reference, a different layout). G1 sheets: bottom right empty.\n");
            var priorHashes = File.Exists(Path.Combine(inDir, "hashes.txt")) ? File.ReadAllText(Path.Combine(inDir, "hashes.txt")) : "";
            foreach (var seed in seeds)
            {
                var layout = LmcLayoutBuilder.Build(contour, seed, LmcLayoutSettings.Default);
                var same = priorHashes.Contains($"{seed} layout {layout.Hash}");
                log.AppendLine($"seed {seed}: layout {layout.Hash} — equal to the calibration run's: {same} (V-L8)");
                var rng = new GameDev.World.Seed.WorldSeed(seed).Fork("research.lmc.blind");
                var lm3IsX = rng.NextDouble() < 0.5;
                key.AppendLine($"{seed}: X = {(lm3IsX ? "S-LM3" : "S-LM1")}, Y = {(lm3IsX ? "S-LM1" : "S-LM3")}");
                var dir = Path.Combine(outDir, "seed_" + seed);
                var renders = Path.Combine(dir, "renders");
                Directory.CreateDirectory(renders);
                var xPath = Path.Combine(renders, "X.f32");
                var yPath = Path.Combine(renders, "Y.f32");
                File.Copy(Path.Combine(inDir, "seed_" + seed, (lm3IsX ? "lm3" : "lm1") + "_" + seed + ".f32"), xPath, true);
                File.Copy(Path.Combine(inDir, "seed_" + seed, (lm3IsX ? "lm1" : "lm3") + "_" + seed + ".f32"), yPath, true);
                var x = ReadFloats(xPath, layout.N * layout.N);
                var y = ReadFloats(yPath, layout.N * layout.N);
                var g3cPath = Path.Combine(refDir ?? "", "g3c", "screen", $"g3c_A_s30-med_{seed}.f32");
                var g3c = File.Exists(g3cPath) ? ReadFloats(g3cPath, layout.N * layout.N) : null;
                var lattices = new List<(string, float[])> { ("X", x), ("Y", y), ("H4A", h4a) };
                if (g3c != null) lattices.Add(("G3cA", g3c));
                var cameras = TerrainGenLmcViews.RenderSet(renders, layout, contour, lattices);
                File.WriteAllText(Path.Combine(dir, "cameras.txt"), cameras);
                TerrainGenLmcViews.WritePlan(Path.Combine(renders, "plan_X.png"), x, layout, contour, null);
                TerrainGenLmcViews.WritePlan(Path.Combine(renders, "plan_Y.png"), y, layout, contour, null);
                foreach (var v in TerrainGenLmcViews.Cameras(layout, contour))
                {
                    TerrainGenLmcViews.Sheet(Path.Combine(sheets, $"{seed}_{v.Scale}_{v.Id}.png"), new[]
                    {
                        Path.Combine(renders, $"X_{v.Id}.png"), Path.Combine(renders, $"Y_{v.Id}.png"),
                        Path.Combine(renders, $"H4A_{v.Id}.png"), Path.Combine(renders, $"G3cA_{v.Id}.png"),
                    });
                    index.AppendLine($"{seed}_{v.Scale}_{v.Id}.png — {v.Placement}");
                }

                foreach (var (label, path) in new[] { ("X", xPath), ("Y", yPath), ("H4A", (string)null) })
                {
                    var g1 = Path.Combine(renders, "g1_" + label);
                    Directory.CreateDirectory(g1);
                    log.AppendLine(GeometryFirstReview.Run(new[] { 110201 }, g1, path, label));
                }

                foreach (var (id, scale) in new[] { ("V1", "island"), ("V2", "island"), ("V3", "island"), ("V4", "player"), ("V5", "player"), ("V6", "island") })
                {
                    TerrainGenLmcViews.Sheet(Path.Combine(sheets, $"{seed}_{scale}_G1-{id}.png"), new[]
                    {
                        Path.Combine(renders, "g1_X", $"review_110201_X_{id}.png"), Path.Combine(renders, "g1_Y", $"review_110201_Y_{id}.png"),
                        Path.Combine(renders, "g1_H4A", $"review_110201_H4A_{id}.png"), null,
                    });
                    index.AppendLine($"{seed}_{scale}_G1-{id}.png — G1 {id} (anchors: contour A's Stage 1, seed 110201)");
                }

                index.AppendLine($"{seed}: plans renders/plan_X.png and renders/plan_Y.png (declared structure over the hillshade)");
            }

            File.WriteAllText(Path.Combine(sealedDir, "blind_key.txt"), key.ToString());
            log.AppendLine($"blind key written to sealed/blind_key.txt, MD5 {Md5(Path.Combine(sealedDir, "blind_key.txt"))} (content not logged)");
            File.WriteAllText(Path.Combine(sheets, "index.txt"), index.ToString());
        }

        // ------------------------------------------------------------------ continuity (spec §13.4–13.5, readiness C5)

        private sealed class G3cRecord
        {
            public List<Vector2> Sites = new List<Vector2>();
            public List<(Vector2 p, float h)> Summits = new List<(Vector2, float)>();
            public string RLine;
        }

        private static Dictionary<string, G3cRecord> ParseReport(string path)
        {
            var result = new Dictionary<string, G3cRecord>();
            var tag = new Regex(@"^\[([AB])_s30-med_(\d+)\] (.*)$");
            var site = new Regex(@"^site r=(\d+) m at \((\d+), (\d+)\)");
            var summit = new Regex(@"^highland summit \((\d+), (\d+)\) (\d+) m, prominence (\d+) m");
            foreach (var line in File.ReadAllLines(path))
            {
                var m = tag.Match(line);
                if (!m.Success) continue;
                var key = m.Groups[1].Value + "_" + m.Groups[2].Value;
                if (!result.TryGetValue(key, out var rec)) { rec = new G3cRecord(); result[key] = rec; }
                var body = m.Groups[3].Value;
                var s = site.Match(body);
                if (s.Success)
                {
                    rec.Sites.Add(new Vector2(float.Parse(s.Groups[2].Value, CultureInfo.InvariantCulture), float.Parse(s.Groups[3].Value, CultureInfo.InvariantCulture)));
                    continue;
                }

                var u = summit.Match(body);
                if (u.Success)
                {
                    rec.Summits.Add((new Vector2(float.Parse(u.Groups[1].Value, CultureInfo.InvariantCulture), float.Parse(u.Groups[2].Value, CultureInfo.InvariantCulture)), float.Parse(u.Groups[3].Value, CultureInfo.InvariantCulture)));
                    continue;
                }

                if (body.StartsWith("R1=", StringComparison.Ordinal)) rec.RLine = body;
            }

            return result;
        }

        private static void Continuity(string refDir, string outDir, StringBuilder log)
        {
            if (string.IsNullOrEmpty(refDir)) throw new ArgumentException("-lmcRef is required");
            var screen = Path.Combine(refDir, "g3c", "screen");
            var reportPath = Path.Combine(screen, "g3c_report.txt");
            var c = CultureInfo.InvariantCulture;
            var inputs = new List<string> { reportPath };
            foreach (var ct in new[] { "A", "B" })
            foreach (var seed in CalibrationSeeds)
            {
                inputs.Add(Path.Combine(screen, $"g3c_{ct}_s30-med_{seed}.f32"));
                inputs.Add(Path.Combine(screen, $"g3c_{ct}_s30-med_{seed}_region.u8"));
            }

            var before = new Dictionary<string, string>();
            foreach (var p in inputs) before[p] = Md5(p);
            log.AppendLine($"continuity: reference report MD5 {before[reportPath]} (expected 5188a6dd19fb44d2bad5f2168a0865e5)");
            var record = ParseReport(reportPath);
            LmcContour contourA = null;
            var lines = new StringBuilder();
            foreach (var ct in new[] { "A", "B" })
            foreach (var seed in CalibrationSeeds)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                const int n = 1025;
                const float cell = 2.734375f;
                var hPath = Path.Combine(screen, $"g3c_{ct}_s30-med_{seed}.f32");
                var h = ReadFloats(hPath, n * n);
                var region = File.ReadAllBytes(Path.Combine(screen, $"g3c_{ct}_s30-med_{seed}_region.u8"));
                var rec = record[ct + "_" + seed];
                var main = rec.Summits[0];
                foreach (var s in rec.Summits) if (s.h > main.h) main = s;
                var b = LmcBase.Build(h, n, cell);
                var v = new Dictionary<string, object> { ["contour"] = ct, ["seed"] = seed, ["lattice_md5"] = before[hPath] };
                var mainIdx = LmcMeasure.MainSummitIndex(b, region);
                v["summit_spec"] = b.H[mainIdx];
                v["summit_record"] = (double)main.h;

                var coast = LmcMeasure.Q7(b);
                v["q7"] = new Dictionary<string, object>
                {
                    ["shares"] = new List<object> { coast.Shares[0], coast.Shares[1], coast.Shares[2] }, ["total"] = coast.Total,
                    ["longest_cliff_of_cliff"] = coast.LongestCliffOfCliff, ["longest_cliff_of_shore"] = coast.LongestCliffOfShore,
                    ["runs"] = coast.Runs, ["changes_per_km"] = coast.ChangesPerKm,
                };

                LmcMeasure.Q1(b, region, out var q1, out var q1r);
                v["q1_all"] = q1;
                var q1map = new Dictionary<string, object>();
                foreach (var code in new[] { 1, 2, 3 }) if (q1r.TryGetValue(code, out var val)) q1map[code.ToString(c)] = val;
                v["q1_by_region"] = q1map;

                var hmask = new bool[n * n];
                for (var i = 0; i < hmask.Length; i++) hmask[i] = region[i] == LmcCodes.Highland;
                LmcMeasure.Q3(b, hmask, out var q3w, out var q3k, out _);
                v["q3_width_median"] = q3w;
                v["q3_kappa_median"] = q3k;
                v["q5_main"] = LmcMeasure.Q5(b, main.p);
                v["q2_theta"] = LmcMeasure.Q2(b, out var systems);
                v["q2_systems"] = systems;
                LmcMeasure.Q4m(b, hmask, main.p, out var ratio, out var ppk, out var lkm, out var path);
                v["q4m"] = new Dictionary<string, object> { ["ratio"] = ratio, ["peaks_per_km"] = ppk, ["length_km"] = lkm, ["path_cells"] = path.Count };

                // plateaus as the reference groups them: 8-connected components of codes 4–6, tops = code 4
                var plat = new bool[n * n];
                for (var i = 0; i < plat.Length; i++) plat[i] = region[i] == 4 || region[i] == 5 || region[i] == 6;
                var plates = new List<LmcPlateauReading>();
                foreach (var cells in LmcMeasure.Components8(plat, n))
                {
                    var top = new bool[n * n];
                    var any = false;
                    foreach (var i in cells) if (region[i] == 4) { top[i] = true; any = true; }
                    if (any) plates.Add(LmcMeasure.Plateau(b, top));
                }

                plates.Sort((p, q) => p.TopKm2.CompareTo(q.TopKm2));
                var plist = new List<object>();
                foreach (var p in plates)
                {
                    plist.Add(new Dictionary<string, object>
                    {
                        ["top_km2"] = p.TopKm2, ["edge_height"] = p.EdgeHeight, ["esc_share"] = p.EscShare, ["joined_share"] = p.JoinedShare,
                        ["cv"] = p.Cv, ["reentrants"] = p.Reentrants,
                    });
                }

                v["plateaus"] = plist;

                // recorded sites: T-edge components and the K2 detours
                var comps = new List<object>();
                var reach = new List<object>();
                var compIds = new List<int>();
                var walk = new List<int>();
                foreach (var s in rec.Sites)
                {
                    var x = (int)Math.Round(s.x / cell, MidpointRounding.ToEven);
                    var z = (int)Math.Round(s.y / cell, MidpointRounding.ToEven);
                    var comp = b.VertexComponent(x, z, out var r, out _);
                    comps.Add(comp);
                    reach.Add(r);
                    compIds.Add(comp);
                    walk.Add(LmcMeasure.NearestWalkVertex(b, x, z));
                }

                var one = compIds.Count > 0 && compIds[0] >= 0;
                foreach (var id in compIds) one &= id == compIds[0];
                v["site_components"] = comps;
                v["site_reachable"] = reach;
                v["sites_one_component"] = one;
                var det = new List<object>();
                var dists = new Dictionary<int, double[]>();
                for (var i = 0; i < rec.Sites.Count; i++)
                for (var j = i + 1; j < rec.Sites.Count; j++)
                {
                    object d = null;
                    if (walk[i] >= 0 && walk[j] >= 0 && compIds[i] == compIds[j])
                    {
                        if (!dists.TryGetValue(i, out var dist)) { dist = LmcMeasure.VertexDijkstra(b, walk[i]); dists[i] = dist; }
                        if (!double.IsInfinity(dist[walk[j]])) d = dist[walk[j]] / LmcMeasure.Octile(walk[i], walk[j], n, b.Cell);
                    }

                    det.Add(new Dictionary<string, object> { ["pair"] = new List<object> { i, j }, ["detour"] = d });
                }

                v["r9_detours"] = det;
                v["dcap"] = LmcMeasure.DCap(b, G3cAngles);
                v["dreg"] = LmcMeasure.DReg(b, region);

                // carried G3c readings on contour A only (draft §6.5): R1 counts, S8, R4 site positions
                if (ct == "A")
                {
                    if (contourA == null) contourA = LmcContour.A();
                    v["contour_a_stage1_hash"] = contourA.FieldHash;
                    v["carried"] = Carried(h, region, n, cell, seed, contourA, b);
                }

                v["record_rline"] = rec.RLine;
                v["seconds"] = Math.Round(watch.Elapsed.TotalSeconds, 1);
                lines.AppendLine(LmcJson.Write(v));
                log.AppendLine($"continuity {ct} {seed}: {watch.Elapsed.TotalSeconds.ToString("F1", c)} s");
            }

            File.WriteAllText(Path.Combine(outDir, "continuity.jsonl"), lines.ToString());
            var changed = 0;
            foreach (var p in inputs) if (Md5(p) != before[p]) changed++;
            log.AppendLine($"continuity inputs unchanged: {inputs.Count - changed}/{inputs.Count}");
        }

        /// <summary>G3c's R1 counts, S8 and R4 site positions recomputed by LMC's carried code on a contour-A grid.</summary>
        private static Dictionary<string, object> Carried(float[] h, byte[] region, int n, float cell, int seed, LmcContour contour, LmcBase b)
        {
            var a = GeographyAnalysis.Build(h, n, cell, seed);
            var land = a.Land;
            var count = n * n;
            var outletOf = LmcMeasure.Catchments(a, n, out var catchArea);
            var peaks = GeographyMetrics.MergeTree(h, land, n);
            int culm = 0, branch = 0, passes = 0, passesBefore = 0;
            var summits = new List<object>();
            foreach (var peak in peaks)
            {
                if (peak.Prominence < GeographyMetrics.SummitProminenceMeters) continue;
                if (region[peak.Index] == LmcCodes.Highland)
                {
                    culm++;
                    summits.Add(new List<object> { (peak.Index % n) * cell, (peak.Index / n) * cell, peak.Height, peak.Prominence });
                }

                if (peak.Col >= 0 && region[peak.Col] == LmcCodes.Highland)
                {
                    var distinct = new HashSet<int>();
                    var cx = peak.Col % n; var cz = peak.Col / n;
                    for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = cx + dx; var z = cz + dz;
                        if (x < 0 || z < 0 || x >= n || z >= n) continue;
                        var o = outletOf[z * n + x];
                        if (o >= 0 && catchArea.TryGetValue(o, out var ar) && ar * cell * cell >= 100_000f) distinct.Add(o);
                    }

                    if (distinct.Count >= 2)
                    {
                        passesBefore++;
                        if (b.VertexReachable(cx, cz)) passes++;
                    }
                }
            }

            for (var i = 0; i < count; i++) if (a.BranchMask[i] && region[i] == LmcCodes.Highland) branch++;

            // S8 depressions, as G3c (hydrology depression fill, depth >= 0.25 m over >= 100 m²)
            var depth = new float[count];
            for (var i = 0; i < count; i++) depth[i] = land[i] ? a.Filled[i] - h[i] : 0f;
            var seen = new bool[count];
            var s8 = 0;
            for (var s = 0; s < count; s++)
            {
                if (seen[s] || depth[s] <= 0.05f) continue;
                var stack = new Stack<int>();
                seen[s] = true;
                stack.Push(s);
                var cells = 0; var deepest = 0f;
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    cells++;
                    deepest = Mathf.Max(deepest, depth[i]);
                    var x = i % n; var z = i / n;
                    foreach (var j in new[] { x > 0 ? i - 1 : -1, x < n - 1 ? i + 1 : -1, z > 0 ? i - n : -1, z < n - 1 ? i + n : -1 })
                    {
                        if (j >= 0 && !seen[j] && depth[j] > 0.05f) { seen[j] = true; stack.Push(j); }
                    }
                }

                if (deepest >= IslandReliefMetrics.DepressionMinDepthMeters && cells * cell * cell >= IslandReliefMetrics.DepressionMinAreaSqm) s8++;
            }

            // R4 sites — positions only (G3c's region ids need its basin instances, which the grid does not store)
            var sites = LmcMeasure.Sites(b, h, contour.Sd, _ => 0, out var capital, out var coastal, out _);
            var sl = new List<object>();
            foreach (var s in sites) sl.Add(new List<object> { s.Position.x, s.Position.y, s.RadiusMeters, s.Coastal });
            return new Dictionary<string, object>
            {
                ["culm"] = culm, ["branch"] = branch, ["passes"] = passes, ["passes_before_reach"] = passesBefore, ["summits"] = summits,
                ["s8"] = s8, ["sites"] = sl, ["capital"] = capital, ["coastal_site"] = coastal,
                ["hydrology_ok"] = a.Hydrology.Succeeded,
            };
        }
    }
}
#endif
