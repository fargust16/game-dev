#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.Hydrology;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Stage 4A diagnostics: per-part relief images for human review, the design's §11 metrics against
    /// the Stage 2 baseline, the §19.2 structural checks, and hashes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything is measured on the 1025 × 1025 world lattice through <see cref="IslandRelief.At"/> and
    /// <see cref="IslandRelief.Sample"/>, the same functions a point query uses. Nothing here writes a
    /// <c>TerrainData</c>, opens a scene or touches the production terrain path.
    /// </para>
    /// <para>
    /// The depression scan uses the hydrology package's <see cref="DepressionFill"/> as an independent
    /// checker: it shares no code with the relief, which is what makes it a check. Stage 4A itself never
    /// references hydrology.
    /// </para>
    /// </remarks>
    public static class TerrainGenIslandReliefDump
    {
        public static readonly int[] CanonicalSeeds = { 110201, 424242, 20260905, 7, 1337, 256, 777 };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";
        private const string SweepArg = "-terrainGenSweep";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 4A Island Relief (Canonical Seeds)", false, 220)]
        public static void DumpMenu()
        {
            Debug.Log(Run(CanonicalSeeds, Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage4a")));
        }

        /// <summary>
        /// Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenIslandReliefDump.DumpBatch</c>
        /// <c>[-terrainGenSeeds a,b] [-terrainGenOut dir] [-reliefOverrides "Field=value;Field=value"]</c>.
        /// </summary>
        /// <remarks>
        /// Overrides exist for calibration experiments on design-permitted levers and are printed at the top
        /// of the report; the defaults in <see cref="IslandReliefSettings.Default"/> are what ships.
        /// </remarks>
        public static void DumpBatch()
        {
            var settings = IslandReliefSettings.Default;
            var overrides = ApplyOverrides(ref settings, ReadArg(System.Environment.GetCommandLineArgs(), "-reliefOverrides"));
            var sweep = ReadArg(System.Environment.GetCommandLineArgs(), SweepArg);
            if (!string.IsNullOrEmpty(sweep))
            {
                Debug.Log(RunSweep(sweep, ReadOutputDirectory("stage4a-sweep"), settings, overrides));
                return;
            }

            Debug.Log(Run(ReadSeeds(), ReadOutputDirectory("stage4a"), settings, overrides));
        }

        public static string Run(int[] seeds, string directory) =>
            Run(seeds, directory, IslandReliefSettings.Default, string.Empty);

        private static string ApplyOverrides(ref IslandReliefSettings settings, string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            object boxed = settings;
            var applied = new List<string>();
            foreach (var pair in raw.Split(';'))
            {
                var parts = pair.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                var field = typeof(IslandReliefSettings).GetField(parts[0].Trim());
                if (field == null || field.FieldType != typeof(float)
                    || !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    throw new ArgumentException($"Bad relief override '{pair}'.");
                }

                field.SetValue(boxed, value);
                applied.Add($"{field.Name}={value.ToString(CultureInfo.InvariantCulture)}");
            }

            settings = (IslandReliefSettings)boxed;
            return string.Join(", ", applied);
        }

        public static string Run(int[] seeds, string directory, IslandReliefSettings settings, string overrides)
        {
            Directory.CreateDirectory(directory);
            var c = CultureInfo.InvariantCulture;
            var report = new StringBuilder();
            var summary = new StringBuilder();

            report.AppendLine("Stage 4A island relief - plate-block uplift, knolls, massif apron");
            report.AppendLine("  NOT connected to TerrainAuthorityField: production heights are still StructuralElevation (H2).");
            report.AppendLine($"  output {directory}");
            report.AppendLine(
                $"  settings: plate {settings.PlateUpliftMeters} m (x{settings.NoMassifUpliftScale} no massif), "
                + $"τ {settings.InlandSlopeBudget.ToString("F3", c)}, lowland reserve {settings.LowlandReserveShare.ToString("F2", c)}, "
                + $"edge min {settings.PlateEdgeHalfWidthMeters} m, junction min {settings.JunctionRelaxRadiusMeters} m, "
                + $"knoll {settings.KnollHeightMeters} m (cap {settings.KnollHeightCapMeters.ToString("F1", c)} m, keep {settings.KnollKeepFraction}), "
                + $"apron {settings.ApronHeightPerCrest}/{settings.ApronCapPerCrest} x crest, reach "
                + $"{settings.ApronReachSteepMeters}/{settings.ApronReachGentleMeters}/{settings.ApronReachEndMeters} m");
            report.AppendLine("  floor = land with massif uplift <= 2 m; metrics on the lattice, baseline = H2");
            report.AppendLine(string.IsNullOrEmpty(overrides)
                ? "  overrides: none (shipping defaults)"
                : "  OVERRIDES (experiment, not the defaults): " + overrides);
            report.AppendLine();

            foreach (var seed in seeds)
            {
                var world = IslandReliefWorld.Build(seed, settings, withStage3: true);
                var baseline = IslandReliefMetrics.Measure(world.H2, world);
                var relief = IslandReliefMetrics.Measure(world.H4A, world);
                var checks = IslandReliefChecks.Run(world);
                var targets = IslandReliefTargets.Evaluate(world, baseline, relief, checks);

                AppendSeed(report, world, baseline, relief, checks, targets, c);
                summary.AppendLine(SummaryLine(world, baseline, relief, checks, targets, c));
                WriteImages(directory, world);
            }

            report.AppendLine("one line per seed:");
            report.Append(summary);
            File.WriteAllText(Path.Combine(directory, "stage4a_report.txt"), report.ToString());
            return report.ToString();
        }

        // ---------- report ----------

        private static void AppendSeed(
            StringBuilder sb,
            IslandReliefWorld w,
            IslandReliefMetrics b,
            IslandReliefMetrics r,
            IslandReliefChecks k,
            List<IslandReliefTargets.Result> targets,
            CultureInfo c)
        {
            var relief = w.Relief;
            sb.AppendLine($"seed {w.Seed}  massifs {w.Stage2B.Massifs.Count}  plate scale x{relief.WorldUpliftScale.ToString("F2", c)} ({relief.PlateUpliftMeters.ToString("F1", c)} m)");
            sb.AppendLine($"  hashes     stage1 {w.Stage1.Stats.FieldHash}  stage2B {w.Stage2B.Stats.FieldHash}  relief {w.ReliefField.Hash()}  H4A {w.H4AField.Hash()}");
            sb.AppendLine($"  signature  {Signature(w)}");
            sb.AppendLine($"  timing     stage2B {w.Stage2BMs} ms, stage3 {w.Stage3Ms} ms, relief build {w.BuildMs} ms, relief lattice {w.LatticeMs} ms, parts lattice {w.PartsMs} ms");

            sb.AppendLine(
                $"  plates     {relief.DistrictCount} district(s) of {relief.Plates.Count}; craton rule {relief.UsedCratonRule}; "
                + $"lowland reservation {relief.ReservedDistrictCount} district(s), {(relief.ReservedLandShare * 100f).ToString("F1", c)}% of land; τ {relief.SlopeBudget.ToString("F3", c)}");
            foreach (var p in relief.Plates)
            {
                if (p.OwnLandAreaSqm <= 0f && p.IsDistrict == false && p.OwnFrontageMeters <= 0f)
                {
                    continue;
                }

                sb.AppendLine(
                    $"    P{p.Index,-2} site=({p.Site.x.ToString("F0", c)},{p.Site.y.ToString("F0", c)}) "
                    + $"ownLand={(p.OwnLandAreaSqm / 1e6f).ToString("F3", c)}km2 ownFront={p.OwnFrontageMeters.ToString("F0", c)}m "
                    + (p.IsDistrict
                        ? $"DISTRICT land={(p.LandAreaSqm / 1e6f).ToString("F3", c)}km2 front={p.FrontageMeters.ToString("F0", c)}m score={p.Score.ToString("F0", c)} rank={p.Rank} level={p.Level.ToString("F2", c)}"
                        : $"merged->P{p.EffectiveIndex} level={p.Level.ToString("F2", c)}"));
            }

            var pairLine = new StringBuilder("    pair widths");
            var pairCount = 0;
            for (var a = 0; a < relief.Plates.Count; a++)
            {
                if (!relief.Plates[a].IsDistrict)
                {
                    continue;
                }

                for (var plateB = a + 1; plateB < relief.Plates.Count; plateB++)
                {
                    if (!relief.Plates[plateB].IsDistrict)
                    {
                        continue;
                    }

                    var halfW = relief.PairHalfWidth(a, plateB);
                    if (halfW <= 0f)
                    {
                        continue;
                    }

                    pairLine.Append($" P{a}|P{plateB}={halfW.ToString("F0", c)}m");
                    pairCount++;
                }
            }

            if (pairCount > 0)
            {
                sb.AppendLine(pairLine.ToString());
            }

            foreach (var e in relief.Boundaries)
            {
                if (e.OnLandLengthMeters <= 0f)
                {
                    continue;
                }

                sb.AppendLine(
                    $"    B{e.Index,-2} P{e.PlateA}|P{e.PlateB} conv={e.Convergence.ToString("F2", c)} len={e.LengthMeters.ToString("F0", c)}m onLand={e.OnLandLengthMeters.ToString("F0", c)}m"
                    + (e.Scored ? $" SCORED upper=P{e.UpperPlate} lower=P{e.LowerPlate}" : string.Empty));
            }

            foreach (var j in relief.Junctions)
            {
                var cert = j.Certificate;
                sb.AppendLine(
                    $"    J{j.Index} at ({j.Position.x.ToString("F0", c)},{j.Position.y.ToString("F0", c)}) plates [{string.Join(",", j.Plates)}] mean level {j.MeanLevel.ToString("F2", c)} "
                    + $"R {cert.InitialRadiusMeters.ToString("F0", c)}->{cert.FinalRadiusMeters.ToString("F0", c)} m "
                    + $"adverse {cert.MaxAdverseDerivative.ToString("F4", c)} (τ {relief.SlopeBudget.ToString("F3", c)}) "
                    + $"iters {cert.Iterations} {(cert.Passed ? "PASS" : "FAIL")}");
                foreach (var pair in cert.Pairs)
                {
                    sb.AppendLine(
                        $"      P{pair.PlateA}|P{pair.PlateB} w {pair.InitialMeters.ToString("F0", c)}->{pair.FinalMeters.ToString("F0", c)} m");
                }
            }

            sb.AppendLine($"  knolls     {relief.Knolls.Count} placed of {relief.KnollCandidates.Count} candidates");
            foreach (var kn in relief.KnollCandidates)
            {
                sb.AppendLine(
                    $"    site {kn.SiteIndex,-3} ({kn.Center.x.ToString("F0", c)},{kn.Center.y.ToString("F0", c)}) D={kn.DiameterMeters.ToString("F0", c)}m r={kn.Resistance.ToString("F2", c)} "
                    + $"level={kn.Level.ToString("F2", c)} gate={kn.Gate.ToString("F2", c)} "
                    + (kn.Placed
                        ? $"KNOLL h={kn.HeightMeters.ToString("F2", c)}m cap={kn.Cap.ToString("F2", c)} gP={kn.PlateDescentBound.ToString("F4", c)} yaw={(kn.YawRadians * Mathf.Rad2Deg).ToString("F0", c)}deg sigma={kn.SigmaMajorMeters.ToString("F0", c)}x{kn.SigmaMinorMeters.ToString("F0", c)}m"
                        : "omitted: " + kn.Rejection));
            }

            foreach (var m in relief.ApronMassifs)
            {
                relief.ApronDomain(m.Index, out var reach, out var delta, out var domain);
                var gentle = relief.EffectiveReach(m.Index, 0.5f, m.SteepSide > 0f ? -1f : 1f);
                var steep = relief.EffectiveReach(m.Index, 0.5f, m.SteepSide > 0f ? 1f : -1f);
                sb.AppendLine(
                    $"  apron      massif {m.Index}: crest {m.CrestHeightMeters.ToString("F0", c)} m, length {m.LengthMeters.ToString("F0", c)} m, "
                    + $"steep side {m.SteepSide}, asymmetry {m.Asymmetry.ToString("F2", c)}, "
                    + $"R_axis {reach.ToString("F1", c)} m δ {delta.ToString("F3", c)} m domain {domain.ToString("F1", c)} m, "
                    + $"W_eff mid gentle/steep {gentle.ToString("F0", c)}/{steep.ToString("F0", c)} m, "
                    + $"toe height {(relief.Settings.ApronHeightPerCrest * m.CrestHeightMeters).ToString("F1", c)} m, cap {(relief.Settings.ApronCapPerCrest * m.CrestHeightMeters).ToString("F1", c)} m");
            }

            sb.AppendLine(
                $"  relief     max {k.ReliefMax.ToString("F2", c)} m, mean on land {k.ReliefMeanLand.ToString("F2", c)} m, "
                + $"M max {k.CoastalFloorMax.ToString("F2", c)} m, knoll max {k.KnollMax.ToString("F2", c)} m, apron max {k.ApronMax.ToString("F2", c)} m");
            sb.AppendLine($"  shares     upland(level>=0.5) {k.UplandPercent.ToString("F1", c)}%  lowland(level<0.25) {k.LowlandPercent.ToString("F1", c)}%  knoll footprint(>1 m) {k.KnollFootprintPercent.ToString("F1", c)}%  apron zone(>1 m) {k.ApronZonePercent.ToString("F1", c)}%");

            sb.AppendLine("  metrics                         baseline H2      Stage 4A");
            AppendMetric(sb, "land p5/p25/p50/p75/p90/p99 m", b.PercentileLine(c), r.PercentileLine(c));
            AppendMetric(sb, "highest point m", b.HighestPoint.ToString("F1", c), r.HighestPoint.ToString("F1", c));
            AppendMetric(sb, "land 24-50/50-70 %", $"{b.Band24To50.ToString("F1", c)}/{b.Band50To70.ToString("F1", c)}", $"{r.Band24To50.ToString("F1", c)}/{r.Band50To70.ToString("F1", c)}");
            AppendMetric(sb, "land 70-110 %", b.Band70To110.ToString("F1", c), r.Band70To110.ToString("F1", c));
            AppendMetric(sb, "land 110-230 %", b.Band110To230.ToString("F1", c), r.Band110To230.ToString("F1", c));
            AppendMetric(sb, "floor LR 112 m median/p90", $"{b.FloorReliefMedian.ToString("F2", c)}/{b.FloorReliefP90.ToString("F2", c)}", $"{r.FloorReliefMedian.ToString("F2", c)}/{r.FloorReliefP90.ToString("F2", c)}");
            AppendMetric(sb, "height spread at fixed d m", b.HeightSpreadAtDistance.ToString("F2", c), r.HeightSpreadAtDistance.ToString("F2", c));
            AppendMetric(sb, "coast-aligned gradient %", b.CoastAlignedPercent.ToString("F1", c), r.CoastAlignedPercent.ToString("F1", c));
            AppendMetric(sb, "floor slope 0-5/5-15/15-30/>30", b.FloorSlopeLine(c), r.FloorSlopeLine(c));
            AppendMetric(sb, "land <5 / >45 %", $"{b.LandUnder5.ToString("F1", c)}/{b.LandOver45.ToString("F1", c)}", $"{r.LandUnder5.ToString("F1", c)}/{r.LandOver45.ToString("F1", c)}");
            AppendMetric(sb, "reachable <=45 %", b.ReachablePercent.ToString("F1", c), r.ReachablePercent.ToString("F1", c));
            AppendMetric(sb, "depressions >=0.25m&100m2", $"{b.Depressions} ({b.DepressionAreaSqm.ToString("F0", c)} m2)", $"{r.Depressions} ({r.DepressionAreaSqm.ToString("F0", c)} m2, deepest {r.DepressionMaxDepth.ToString("F2", c)} m)");
            AppendMetric(sb, "micro depressions", $"{b.MicroDepressions} (<= {b.MicroMaxDepth.ToString("F2", c)} m)", $"{r.MicroDepressions} (<= {r.MicroMaxDepth.ToString("F2", c)} m)");
            AppendMetric(sb, "curvature ratio", b.CurvatureRatio.ToString("F3", c), r.CurvatureRatio.ToString("F3", c));

            sb.AppendLine("  structural checks");
            sb.AppendLine($"    S1  relief == 0 at sd <= 0: {Verdict(k.SeaSamplesWithRelief == 0)} ({k.SeaSamplesWithRelief} of {k.SeaSamples} samples non-zero)");
            sb.AppendLine($"    S2  land mask unchanged: {Verdict(k.LandMaskChanges == 0)} ({k.LandMaskChanges} samples differ)");
            sb.AppendLine($"    S3  bounded & finite: {Verdict(k.BoundsViolations == 0)} (violations {k.BoundsViolations}, H4A max {k.H4AMax.ToString("F2", c)} m)");
            sb.AppendLine(
                $"    S8-A depressions outside Z_M ∪ L_M: {Verdict(k.S8ACount == 0)} ({k.S8ACount}; deepest {k.S8AMaxDepth.ToString("F2", c)} m, {k.S8AAreaSqm.ToString("F0", c)} m2)");
            sb.AppendLine(
                $"    S8-M depressions inside Z_M ∪ L_M: {Verdict(k.S8MCount == 0)} ({k.S8MCount}; deepest {k.S8MMaxDepth.ToString("F2", c)} m, {k.S8MAreaSqm.ToString("F0", c)} m2)");
            foreach (var line in IslandReliefChecks.DepressionAttribution(w))
            {
                sb.AppendLine("        " + line);
            }

            sb.AppendLine(
                $"    S8-C coast-normal derivative outside ownership: {Verdict(k.S8CViolations == 0)} "
                + $"(min {k.S8CMinDerivative.ToString("E2", c)}, threshold -1e-3, samples {k.S8CSamples}, beyond-800 {k.LandBeyond800Percent.ToString("F2", c)}%)");
            sb.AppendLine($"    S9  knoll <= cap, apron <= 0.18 x crest: {Verdict(k.KnollMax <= relief.Settings.KnollHeightCapMeters + 1e-4f && k.ApronMax <= k.ApronCap + 1e-3f)} (knoll {k.KnollMax.ToString("F2", c)}/{relief.Settings.KnollHeightCapMeters.ToString("F2", c)}, apron {k.ApronMax.ToString("F2", c)}/{k.ApronCap.ToString("F2", c)})");
            sb.AppendLine(
                $"    S10 ungated P_A: beyond-domain {k.ApronBeyondDomainMax.ToString("F4", c)} m ({k.ApronBeyondDomainSamples} samples); "
                + $"normals {k.ApronNormalRays} (violations {k.ApronNormalViolations}, worst {k.ApronNormalWorstRise.ToString("F4", c)} m); "
                + $"gated-fill rays (report) {k.ApronRayViolations} worst {k.ApronRayWorstRise.ToString("F4", c)} m: "
                + Verdict(k.ApronBeyondDomainMax <= 1e-3f && k.ApronNormalWorstRise <= 1e-3f));
            foreach (var line in k.ApronRayLines)
            {
                sb.AppendLine("        " + line);
            }

            sb.AppendLine(
                $"    S11-T trajectories: {k.Trajectories} checked; C(R4A) {k.TrajectoryWorstClimb.ToString("F2", c)} m (<= 7.2), "
                + $"plate+apron adverse {k.TrajectoryPlateApronRise.ToString("F2", c)} m (<= 1.5), "
                + $"I1 H2 {k.TrajectoryI1H2.ToString("F3", c)} -> H4A {k.TrajectoryI1H4A.ToString("F3", c)} (Δ {k.TrajectoryI1Increase.ToString("F4", c)} m), "
                + $"R4A adverse sum (report) {k.TrajectoryReliefRise.ToString("F2", c)} m: "
                + Verdict(k.TrajectoryWorstClimb <= 7.2f && k.TrajectoryPlateApronRise <= 1.5f && k.TrajectoryI1Increase <= 0.001f));
            foreach (var line in k.TrajectoryLines)
            {
                sb.AppendLine("        " + line);
            }

            sb.AppendLine(
                $"    S11-O O-1 end<outlet: {Verdict(k.OutletO1Failures == 0)} ({k.OutletO1Failures} fail / {k.Outlets}); "
                + $"O-2 post-carve rim: {(k.CarveMeasurable ? Verdict(k.OutletO2Failures == 0) : "UNMEASURABLE")} "
                + $"({k.OutletO2Failures} fail, {k.OutletO2Checked} with H2 margin>0"
                + (k.CarveMeasurable ? $", carve max {k.CarveMax.ToString("F2", c)} m" : ", " + k.CarveNote)
                + "); "
                + $"O-3 I1/C: {Verdict(k.OutletO3Failures == 0)}; O-4 worst declared-min shift {k.OutletMinShift.ToString("F1", c)} m, declared-above-run {k.OutletDeclaredAboveMin.ToString("F2", c)} m");
            foreach (var line in k.OutletLines)
            {
                sb.AppendLine("        " + line);
            }

            sb.AppendLine($"    S12 junction certificates: {Verdict(k.S12Failures == 0)} ({k.S12Failures} fail / {relief.Junctions.Count})");

            sb.AppendLine("  §11 targets");
            foreach (var t in targets)
            {
                sb.AppendLine($"    {(t.Pass ? "pass" : "FAIL")} {(t.Hard ? "hard" : "soft")}  {t.Name}: {t.Actual} (target {t.Target})");
            }

            sb.AppendLine();
        }

        private static void AppendMetric(StringBuilder sb, string name, string baseline, string relief) =>
            sb.AppendLine($"    {name,-30} {baseline,-16} {relief}");

        private static string Verdict(bool pass) => pass ? "PASS" : "FAIL";

        private static string SummaryLine(
            IslandReliefWorld w, IslandReliefMetrics b, IslandReliefMetrics r, IslandReliefChecks k,
            List<IslandReliefTargets.Result> targets, CultureInfo c)
        {
            var hardFails = 0;
            var softFails = 0;
            foreach (var t in targets)
            {
                if (!t.Pass)
                {
                    if (t.Hard)
                    {
                        hardFails++;
                    }
                    else
                    {
                        softFails++;
                    }
                }
            }

            return string.Join(" ", new[]
            {
                $"seed={w.Seed}",
                $"massifs={w.Stage2B.Massifs.Count}",
                $"districts={w.Relief.DistrictCount}",
                $"knolls={w.Relief.Knolls.Count}",
                $"band70_110={b.Band70To110.ToString("F1", c)}->{r.Band70To110.ToString("F1", c)}",
                $"LR={b.FloorReliefMedian.ToString("F1", c)}/{b.FloorReliefP90.ToString("F1", c)}->{r.FloorReliefMedian.ToString("F1", c)}/{r.FloorReliefP90.ToString("F1", c)}",
                $"spread={b.HeightSpreadAtDistance.ToString("F1", c)}->{r.HeightSpreadAtDistance.ToString("F1", c)}",
                $"coastAligned={b.CoastAlignedPercent.ToString("F0", c)}->{r.CoastAlignedPercent.ToString("F0", c)}",
                $"lt5={b.LandUnder5.ToString("F1", c)}->{r.LandUnder5.ToString("F1", c)}",
                $"gt45={b.LandOver45.ToString("F1", c)}->{r.LandOver45.ToString("F1", c)}",
                $"reach={b.ReachablePercent.ToString("F1", c)}->{r.ReachablePercent.ToString("F1", c)}",
                $"pits={r.Depressions}",
                $"Rmax={k.ReliefMax.ToString("F1", c)}",
                $"Rmean={k.ReliefMeanLand.ToString("F1", c)}",
                $"hmax={b.HighestPoint.ToString("F0", c)}->{r.HighestPoint.ToString("F0", c)}",
                $"trajR={k.TrajectoryReliefRise.ToString("F2", c)}",
                $"trajPA={k.TrajectoryPlateApronRise.ToString("F2", c)}",
                $"hardFails={hardFails}",
                $"softFails={softFails}",
                $"relief={w.ReliefField.Hash()}",
                $"sig={Signature(w)}",
            });
        }

        /// <summary>A short signature over the relief hash and every structure decision.</summary>
        public static string Signature(IslandReliefWorld w)
        {
            var sb = new StringBuilder(w.ReliefField.Hash());
            var c = CultureInfo.InvariantCulture;
            foreach (var p in w.Relief.Plates)
            {
                sb.Append('|').Append(p.EffectiveIndex).Append(':').Append(p.Level.ToString("R", c));
            }

            foreach (var k in w.Relief.Knolls)
            {
                sb.Append('|').Append(k.SiteIndex).Append(':').Append(k.HeightMeters.ToString("R", c));
            }

            unchecked
            {
                var h = 14695981039346656037UL;
                foreach (var ch in sb.ToString())
                {
                    h ^= ch;
                    h *= 1099511628211UL;
                }

                return h.ToString("x16").Substring(0, 12);
            }
        }

        // ---------- images ----------

        private static void WriteImages(string directory, IslandReliefWorld w)
        {
            var prefix = Path.Combine(directory, $"seed_{w.Seed}_");
            var n = w.Width;

            WritePng(prefix + "01_h2_shaded.png", RenderShaded(w.H2, w), n);
            WritePng(prefix + "02_h4a_shaded.png", RenderShaded(w.H4A, w), n);
            WritePng(prefix + "03_relief_total.png", RenderHeat(w.ReliefField.Heights, w, 45f, true), n);
            WritePng(prefix + "04_gate.png", RenderHeat(w.Gate, w, 1f, false), n);
            WritePng(prefix + "05_plate_gated.png", RenderHeat(Multiply(w.Gate, w.Plate), w, 30f, true), n);
            WritePng(prefix + "06_knolls_gated.png", RenderHeat(Multiply(w.Gate, w.Knoll), w, 7.2f, true), n);
            WritePng(prefix + "07_apron.png", RenderHeat(w.Apron, w, 60f, true), n);
            WritePng(prefix + "08_structure.png", RenderStructure(w), n);
            WritePng(prefix + "09_slope_h2.png", RenderSlopeClasses(w.H2, w), n);
            WritePng(prefix + "10_slope_h4a.png", RenderSlopeClasses(w.H4A, w), n);
            WritePng(prefix + "11_depressions_h4a.png", RenderDepressions(w.H4A, w), n);
            WritePng(prefix + "12_h4a_contours5m.png", RenderContoursOnly(w.H4A, w, 5f), n);
            WritePng(prefix + "13_coastal_floor.png", RenderHeat(w.CoastalFloor, w, 8f, true), n);
        }

        private static float[] Multiply(float[] a, float[] b)
        {
            var result = new float[a.Length];
            for (var i = 0; i < a.Length; i++)
            {
                result[i] = a[i] * b[i];
            }

            return result;
        }

        private static float[] Hillshade(float[] h, int n, float cell)
        {
            var shade = new float[h.Length];
            var az = 315f * Mathf.Deg2Rad;
            var alt = 40f * Mathf.Deg2Rad;
            var lx = Mathf.Cos(alt) * Mathf.Cos(az);
            var lz = Mathf.Cos(alt) * Mathf.Sin(az);
            var ly = Mathf.Sin(alt);
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    IslandReliefMetrics.Gradient(h, n, cell, x, z, out var gx, out var gz);
                    var nx = -gx;
                    var nz = -gz;
                    var len = Mathf.Sqrt(nx * nx + nz * nz + 1f);
                    shade[z * n + x] = Mathf.Clamp01((nx * lx + nz * lz + ly) / len);
                }
            }

            return shade;
        }

        private static Color32[] RenderShaded(float[] h, IslandReliefWorld w)
        {
            var n = w.Width;
            var shade = Hillshade(h, n, w.Cell);
            var pixels = new Color32[h.Length];
            for (var i = 0; i < h.Length; i++)
            {
                var land = h[i] > WorldScale.SeaLevelMeters;
                if (!land)
                {
                    var depth = Mathf.Clamp01((WorldScale.SeaLevelMeters - h[i]) / 22f);
                    pixels[i] = new Color32((byte)(30 + 40 * (1 - depth)), (byte)(70 + 60 * (1 - depth)), (byte)(120 + 60 * (1 - depth)), 255);
                    continue;
                }

                var t = Mathf.Clamp01((h[i] - WorldScale.SeaLevelMeters) / 140f);
                var s = 0.30f + 0.85f * shade[i];
                var r = (80 + 150 * t) * s;
                var g = (135 + 80 * t) * s;
                var b = (70 + 110 * t) * s;
                var above = h[i] - WorldScale.SeaLevelMeters;
                if (Mathf.Abs((above % 10f) - 5f) > 4.45f)
                {
                    r *= 0.35f;
                    g *= 0.35f;
                    b *= 0.35f;
                }

                pixels[i] = new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255), (byte)Mathf.Clamp(b, 0, 255), 255);
            }

            return pixels;
        }

        private static Color32[] RenderContoursOnly(float[] h, IslandReliefWorld w, float interval)
        {
            var pixels = new Color32[h.Length];
            for (var i = 0; i < h.Length; i++)
            {
                if (h[i] <= WorldScale.SeaLevelMeters)
                {
                    pixels[i] = new Color32(40, 60, 90, 255);
                    continue;
                }

                var above = h[i] - WorldScale.SeaLevelMeters;
                var major = Mathf.Abs((above % (interval * 4f)) - interval * 2f) > interval * 2f - 0.6f;
                var minor = Mathf.Abs((above % interval) - interval * 0.5f) > interval * 0.5f - 0.35f;
                pixels[i] = major ? new Color32(20, 20, 20, 255) : minor ? new Color32(110, 110, 110, 255) : new Color32(235, 232, 220, 255);
            }

            return pixels;
        }

        private static Color32[] RenderHeat(float[] values, IslandReliefWorld w, float max, bool landOnly)
        {
            var pixels = new Color32[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                var land = w.H2[i] > WorldScale.SeaLevelMeters;
                if (landOnly && !land)
                {
                    pixels[i] = new Color32(15, 25, 45, 255);
                    continue;
                }

                var t = Mathf.Clamp01(values[i] / max);
                var r = 40 + 215 * t;
                var g = 60 + 150 * (1 - Mathf.Abs(t - 0.5f) * 2f);
                var b = 200 - 180 * t;
                if (!land)
                {
                    r *= 0.5f;
                    g *= 0.5f;
                    b *= 0.5f;
                }

                pixels[i] = new Color32((byte)r, (byte)g, (byte)b, 255);
            }

            return pixels;
        }

        private static Color32[] RenderSlopeClasses(float[] h, IslandReliefWorld w)
        {
            var n = w.Width;
            var pixels = new Color32[h.Length];
            var palette = new[]
            {
                new Color32(120, 190, 90, 255), new Color32(200, 200, 90, 255), new Color32(220, 150, 60, 255),
                new Color32(200, 70, 50, 255), new Color32(90, 20, 20, 255),
            };
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (h[i] <= WorldScale.SeaLevelMeters)
                    {
                        pixels[i] = new Color32(40, 70, 120, 255);
                        continue;
                    }

                    var slope = IslandReliefMetrics.SlopeDegrees(h, n, w.Cell, x, z);
                    var band = slope < 5f ? 0 : slope < 15f ? 1 : slope < 30f ? 2 : slope < 45f ? 3 : 4;
                    pixels[i] = palette[band];
                }
            }

            return pixels;
        }

        private static Color32[] RenderDepressions(float[] h, IslandReliefWorld w)
        {
            var fill = IslandReliefMetrics.FillDepth(h, w);
            var pixels = new Color32[h.Length];
            for (var i = 0; i < h.Length; i++)
            {
                if (h[i] <= WorldScale.SeaLevelMeters)
                {
                    pixels[i] = new Color32(20, 30, 50, 255);
                }
                else if (fill[i] > 0.05f)
                {
                    var t = Mathf.Clamp01(fill[i] / 1f);
                    pixels[i] = new Color32(255, (byte)(200 * (1 - t)), (byte)(60 * (1 - t)), 255);
                }
                else
                {
                    pixels[i] = new Color32(90, 90, 90, 255);
                }
            }

            return pixels;
        }

        private static Color32[] RenderStructure(IslandReliefWorld w)
        {
            var n = w.Width;
            var shade = Hillshade(w.H4A, n, w.Cell);
            var pixels = new Color32[w.H2.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                if (w.H2[i] <= WorldScale.SeaLevelMeters)
                {
                    pixels[i] = new Color32(25, 40, 70, 255);
                    continue;
                }

                var t = Mathf.Clamp01(w.Level[i]);
                var s = 0.55f + 0.45f * shade[i];
                var r = Mathf.Lerp(70f, 225f, t) * s;
                var g = Mathf.Lerp(150f, 190f, t) * s;
                var b = Mathf.Lerp(170f, 110f, t) * s;
                if (w.Apron[i] > 1f)
                {
                    r = Mathf.Lerp(r, 240f, 0.35f);
                    g = Mathf.Lerp(g, 120f, 0.35f);
                    b = Mathf.Lerp(b, 60f, 0.35f);
                }

                pixels[i] = new Color32((byte)r, (byte)g, (byte)b, 255);
            }

            var relief = w.Relief;
            var macro = w.Stage2B.Plates;
            for (var e = 0; e < macro.Boundaries.Length; e++)
            {
                var info = relief.Boundaries[e];
                var colour = info.Scored
                    ? new Color32(220, 20, 20, 255)
                    : info.Convergence >= relief.Settings.ConvergenceThreshold
                        ? new Color32(255, 150, 30, 255)
                        : new Color32(240, 240, 240, 255);
                DrawLine(pixels, n, w.Cell, macro.Boundaries[e].A, macro.Boundaries[e].B, colour, info.Scored ? 1 : 0);
            }

            foreach (var j in relief.Junctions)
            {
                DrawCircle(pixels, n, w.Cell, j.Position, relief.Settings.JunctionRelaxRadiusMeters, new Color32(255, 255, 255, 255));
                DrawDisc(pixels, n, w.Cell, j.Position, 14f, new Color32(0, 0, 0, 255));
            }

            foreach (var m in relief.ApronMassifs)
            {
                var steps = Mathf.Max(8, Mathf.CeilToInt(m.LengthMeters / 10f));
                var previous = m.PointAt(0f);
                for (var i = 1; i <= steps; i++)
                {
                    var next = m.PointAt(i / (float)steps);
                    DrawLine(pixels, n, w.Cell, previous, next, new Color32(200, 0, 200, 255), 1);
                    previous = next;
                }
            }

            foreach (var kn in relief.KnollCandidates)
            {
                if (!kn.Placed)
                {
                    DrawDisc(pixels, n, w.Cell, kn.Center, 6f, new Color32(90, 90, 90, 255));
                    continue;
                }

                DrawEllipse(pixels, n, w.Cell, kn.Center, kn.SigmaMajorMeters * 1.36f, kn.SigmaMinorMeters * 1.36f, kn.YawRadians, new Color32(20, 20, 20, 255));
                DrawDisc(pixels, n, w.Cell, kn.Center, 8f, new Color32(255, 230, 0, 255));
            }

            return pixels;
        }

        private static void Plot(Color32[] pixels, int n, int x, int z, Color32 colour)
        {
            if (x >= 0 && z >= 0 && x < n && z < n)
            {
                pixels[z * n + x] = colour;
            }
        }

        private static void DrawLine(Color32[] pixels, int n, float cell, Vector2 a, Vector2 b, Color32 colour, int thickness)
        {
            var length = (b - a).magnitude;
            var steps = Mathf.Max(1, Mathf.CeilToInt(length / (cell * 0.5f)));
            for (var i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(a, b, i / (float)steps);
                var x = Mathf.RoundToInt(p.x / cell);
                var z = Mathf.RoundToInt(p.y / cell);
                for (var dz = -thickness; dz <= thickness; dz++)
                {
                    for (var dx = -thickness; dx <= thickness; dx++)
                    {
                        Plot(pixels, n, x + dx, z + dz, colour);
                    }
                }
            }
        }

        private static void DrawCircle(Color32[] pixels, int n, float cell, Vector2 centre, float radius, Color32 colour)
        {
            var steps = Mathf.CeilToInt(2f * Mathf.PI * radius / (cell * 0.5f));
            for (var i = 0; i < steps; i++)
            {
                var a = i * 2f * Mathf.PI / steps;
                var p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Plot(pixels, n, Mathf.RoundToInt(p.x / cell), Mathf.RoundToInt(p.y / cell), colour);
            }
        }

        private static void DrawEllipse(Color32[] pixels, int n, float cell, Vector2 centre, float a, float b, float yaw, Color32 colour)
        {
            var steps = Mathf.CeilToInt(2f * Mathf.PI * Mathf.Max(a, b) / (cell * 0.5f));
            var cos = Mathf.Cos(yaw);
            var sin = Mathf.Sin(yaw);
            for (var i = 0; i < steps; i++)
            {
                var t = i * 2f * Mathf.PI / steps;
                var u = Mathf.Cos(t) * a;
                var v = Mathf.Sin(t) * b;
                var p = centre + new Vector2(u * cos - v * sin, u * sin + v * cos);
                Plot(pixels, n, Mathf.RoundToInt(p.x / cell), Mathf.RoundToInt(p.y / cell), colour);
            }
        }

        private static void DrawDisc(Color32[] pixels, int n, float cell, Vector2 centre, float radius, Color32 colour)
        {
            var r = Mathf.CeilToInt(radius / cell);
            var cx = Mathf.RoundToInt(centre.x / cell);
            var cz = Mathf.RoundToInt(centre.y / cell);
            for (var dz = -r; dz <= r; dz++)
            {
                for (var dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dz * dz <= r * r)
                    {
                        Plot(pixels, n, cx + dx, cz + dz, colour);
                    }
                }
            }
        }

        private static void WritePng(string path, Color32[] pixels, int side)
        {
            var texture = new Texture2D(side, pixels.Length / side, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        // ---------- plumbing ----------

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

        /// <summary>
        /// Sweep  <c>start:count</c>  (default 1000:40). Images are skipped; the S8 table is the product.
        /// </summary>
        public static string RunSweep(string spec, string directory, IslandReliefSettings settings, string overrides)
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
            report.AppendLine($"Stage 4A S8 sweep seeds {start}..{start + count - 1}");
            report.AppendLine(string.IsNullOrEmpty(overrides) ? "  overrides: none" : "  OVERRIDES: " + overrides);
            report.AppendLine(
                "seed pitsA pitsM maxDepth area class attribution");

            var aSeeds = 0;
            var mSeeds = 0;
            var maxDepth = 0f;
            for (var i = 0; i < count; i++)
            {
                var seed = start + i;
                var world = IslandReliefWorld.Build(seed, settings, withStage3: true);
                var checks = IslandReliefChecks.Run(world);
                if (checks.S8ACount > 0)
                {
                    aSeeds++;
                }

                if (checks.S8MCount > 0)
                {
                    mSeeds++;
                }

                maxDepth = Mathf.Max(maxDepth, Mathf.Max(checks.S8AMaxDepth, checks.S8MMaxDepth));
                var attr = checks.S8ACount + checks.S8MCount == 0
                    ? "-"
                    : string.Join("; ", IslandReliefChecks.DepressionAttribution(world));
                report.AppendLine(
                    $"{seed} A={checks.S8ACount} M={checks.S8MCount} "
                    + $"deep={Mathf.Max(checks.S8AMaxDepth, checks.S8MMaxDepth).ToString("F2", c)} "
                    + $"area={(checks.S8AAreaSqm + checks.S8MAreaSqm).ToString("F0", c)} {attr}");
            }

            report.AppendLine(
                $"summary: S8-A seeds {aSeeds}/{count}, S8-M seeds {mSeeds}/{count}, max depth {maxDepth.ToString("F2", c)} m");
            File.WriteAllText(Path.Combine(directory, "stage4a_sweep.txt"), report.ToString());
            return report.ToString();
        }
    }

    /// <summary>
    /// One seed's Stage 4A world on the lattice: Stage 2B, the relief, every part per sample, and the
    /// frozen Stage 3 structures it is checked against. Shared by the dump and the tests.
    /// </summary>
    public sealed class IslandReliefWorld
    {
        public int Seed { get; private set; }
        public Stage2BResult Stage2B { get; private set; }
        public Stage1Result Stage1 => Stage2B.Stage1;
        public MassifUpliftModel Uplift { get; private set; }
        public IslandRelief Relief { get; private set; }

        public int Width { get; private set; }
        public float Cell { get; private set; }

        /// <summary>Relief through <see cref="IslandRelief.EvaluateField"/> (the At path).</summary>
        public WorldHeightField ReliefField { get; private set; }

        public WorldHeightField H4AField { get; private set; }

        public float[] H2 { get; private set; }
        public float[] H4A { get; private set; }
        public float[] Gate { get; private set; }
        public float[] Level { get; private set; }
        public float[] Plate { get; private set; }
        public float[] Knoll { get; private set; }
        public float[] Apron { get; private set; }
        public float[] CoastalFloor { get; private set; }

        /// <summary>Relief through <see cref="IslandRelief.Sample"/>, for the At/Sample identity.</summary>
        public float[] SampleTotal { get; private set; }

        public ValleyTrajectoryNetwork Trajectories { get; private set; }
        public StructuralBasinSet Basins { get; private set; }
        public RidgeSkeleton Ridges { get; private set; }
        public CarveField Carve { get; private set; }

        public string Stage1HashBefore { get; private set; }
        public string Stage2BHashBefore { get; private set; }
        public string UpliftHashBefore { get; private set; }

        public long Stage2BMs { get; private set; }
        public long Stage3Ms { get; private set; }
        public long BuildMs { get; private set; }
        public long LatticeMs { get; private set; }
        public long PartsMs { get; private set; }

        public static IslandReliefWorld Build(int seed, IslandReliefSettings settings, bool withStage3)
        {
            var world = new IslandReliefWorld { Seed = seed };
            var sw = Stopwatch.StartNew();
            world.Stage2B = Stage2BMassifGenerator.Generate(seed);
            world.Stage2BMs = sw.ElapsedMilliseconds;

            world.Stage1HashBefore = world.Stage2B.Stage1.Field.Hash();
            world.Stage2BHashBefore = world.Stage2B.Field.Hash();
            world.UpliftHashBefore = world.Stage2B.UpliftField.Hash();

            if (withStage3)
            {
                sw.Restart();
                BuildStage3(world);
                world.Stage3Ms = sw.ElapsedMilliseconds;
            }

            world.Uplift = new MassifUpliftModel(world.Stage2B.Massifs, world.Stage2B.MassifSettings);

            sw.Restart();
            world.Relief = IslandRelief.Build(
                new GameDev.World.Seed.WorldSeed(seed), world.Stage1, world.Stage2B.Plates, world.Stage2B.Massifs, settings);
            world.BuildMs = sw.ElapsedMilliseconds;

            world.Evaluate();
            return world;
        }

        public static IslandReliefWorld FromParts(int seed, Stage2BResult stage2b, IslandRelief relief)
        {
            var world = new IslandReliefWorld
            {
                Seed = seed,
                Stage2B = stage2b,
                Stage1HashBefore = stage2b.Stage1.Field.Hash(),
                Stage2BHashBefore = stage2b.Field.Hash(),
                UpliftHashBefore = stage2b.UpliftField.Hash(),
                Uplift = new MassifUpliftModel(stage2b.Massifs, stage2b.MassifSettings),
                Relief = relief,
            };
            world.Evaluate();
            return world;
        }

        public static void BuildStage3(IslandReliefWorld world)
        {
            var ridgeSettings = RidgeSettings.Default;
            var stage2b = world.Stage2B;
            var worldSeed = new GameDev.World.Seed.WorldSeed(world.Seed);
            var skeleton = Stage3ASkeleton.Build(stage2b.Seed, stage2b.Massifs, stage2b.Stage1.Coast, ridgeSettings);
            var ridges = RidgeSkeleton.Build(skeleton, ridgeSettings, worldSeed, stage2b.Stage1);
            world.Ridges = ridges;
            world.Carve = new CarveField(ridges, stage2b.Stage1.Coast, ridgeSettings, stage2b.MassifSettings);
            var network = ValleyNetwork.Build(ridges, ridgeSettings);
            var topology = ValleyTopology.Build(network, ridgeSettings);
            world.Trajectories = ValleyTrajectoryNetwork.Build(topology, ridgeSettings, stage2b.Stage1);
            var candidates = CirqueCandidateBuilder.Build(ridges, ridgeSettings);
            world.Basins = StructuralBasinBuilder.Build(
                candidates, world.Trajectories, stage2b.Stage1, stage2b.MassifSettings, ridgeSettings);
        }

        private void Evaluate()
        {
            var baseField = Stage1.Field;
            Width = baseField.Width;
            Cell = baseField.CellSize;

            var sw = Stopwatch.StartNew();
            ReliefField = Relief.EvaluateField(baseField, Stage2B.UpliftField, Stage1.SignedDistance);
            LatticeMs = sw.ElapsedMilliseconds;

            var count = baseField.SampleCount;
            H2 = (float[])Stage2B.Field.Heights.Clone();
            H4A = new float[count];
            Gate = new float[count];
            Level = new float[count];
            Plate = new float[count];
            Knoll = new float[count];
            Apron = new float[count];
            CoastalFloor = new float[count];
            SampleTotal = new float[count];

            sw.Restart();
            for (var z = 0; z < baseField.Height; z++)
            {
                var worldZ = baseField.WorldZ(z);
                for (var x = 0; x < baseField.Width; x++)
                {
                    var i = z * baseField.Width + x;
                    var sample = Relief.Sample(
                        baseField.WorldX(x), worldZ, Stage1.SignedDistance[i],
                        baseField.Heights[i], Stage2B.UpliftField.Heights[i]);
                    Gate[i] = sample.Gate;
                    Level[i] = sample.PlateLevel;
                    Plate[i] = sample.Plate;
                    Knoll[i] = sample.Knoll;
                    Apron[i] = sample.Apron;
                    CoastalFloor[i] = sample.CoastalFloor;
                    SampleTotal[i] = sample.Total;
                    H4A[i] = H2[i] + ReliefField.Heights[i];
                }
            }

            PartsMs = sw.ElapsedMilliseconds;
            H4AField = new WorldHeightField(baseField.OriginX, baseField.OriginZ, baseField.Width, baseField.Height, Cell, H4A);
        }
    }

    /// <summary>The design §11 measurements on one lattice height array.</summary>
    public sealed class IslandReliefMetrics
    {
        public float[] Percentiles = new float[6];
        public float HighestPoint;
        public float Band24To50, Band50To70, Band70To110, Band110To230;
        public float FloorSlope0To5, FloorSlope5To15, FloorSlope15To30, FloorSlopeOver30;
        public float LandUnder5, LandOver45;
        public float ReachablePercent;
        public float FloorReliefMedian, FloorReliefP90;
        public float CoastAlignedPercent;
        public float HeightSpreadAtDistance;
        public int Depressions;
        public float DepressionAreaSqm;
        public float DepressionMaxDepth;
        public int MicroDepressions;
        public float MicroMaxDepth;
        public float CurvatureRatio;

        public const float FloorUpliftMeters = 2f;
        public const float DepressionMinDepthMeters = 0.25f;
        public const float DepressionMinAreaSqm = 100f;

        public string PercentileLine(CultureInfo c) =>
            string.Join("/", Array.ConvertAll(Percentiles, p => p.ToString("F0", c)));

        public string FloorSlopeLine(CultureInfo c) =>
            $"{FloorSlope0To5.ToString("F1", c)}/{FloorSlope5To15.ToString("F1", c)}/{FloorSlope15To30.ToString("F1", c)}/{FloorSlopeOver30.ToString("F1", c)}";

        public static IslandReliefMetrics Measure(float[] h, IslandReliefWorld w)
        {
            var n = w.Width;
            var cell = w.Cell;
            var sea = WorldScale.SeaLevelMeters;
            var sd = w.Stage1.SignedDistance;
            var uplift = w.Stage2B.UpliftField.Heights;
            var m = new IslandReliefMetrics();

            var land = new bool[h.Length];
            var floor = new bool[h.Length];
            var landHeights = new List<float>(h.Length / 2);
            var slope = new float[h.Length];
            m.HighestPoint = float.NegativeInfinity;

            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    m.HighestPoint = Mathf.Max(m.HighestPoint, h[i]);
                    land[i] = h[i] > sea;
                    if (!land[i])
                    {
                        continue;
                    }

                    floor[i] = uplift[i] <= FloorUpliftMeters;
                    landHeights.Add(h[i]);
                    slope[i] = SlopeDegrees(h, n, cell, x, z);
                }
            }

            landHeights.Sort();
            var ps = new[] { 5f, 25f, 50f, 75f, 90f, 99f };
            for (var p = 0; p < ps.Length; p++)
            {
                m.Percentiles[p] = Percentile(landHeights, ps[p]);
            }

            int b1 = 0, b2 = 0, b3 = 0, b4 = 0;
            foreach (var v in landHeights)
            {
                if (v < 50f) b1++;
                else if (v < 70f) b2++;
                else if (v < 110f) b3++;
                else if (v < 230f) b4++;
            }

            var landCount = Mathf.Max(1, landHeights.Count);
            m.Band24To50 = 100f * b1 / landCount;
            m.Band50To70 = 100f * b2 / landCount;
            m.Band70To110 = 100f * b3 / landCount;
            m.Band110To230 = 100f * b4 / landCount;

            int f0 = 0, f1 = 0, f2 = 0, f3 = 0, floorCount = 0, under5 = 0, over45 = 0;
            for (var i = 0; i < h.Length; i++)
            {
                if (!land[i])
                {
                    continue;
                }

                if (slope[i] < 5f) under5++;
                if (slope[i] > 45f) over45++;
                if (!floor[i])
                {
                    continue;
                }

                floorCount++;
                if (slope[i] < 5f) f0++;
                else if (slope[i] < 15f) f1++;
                else if (slope[i] < 30f) f2++;
                else f3++;
            }

            floorCount = Mathf.Max(1, floorCount);
            m.FloorSlope0To5 = 100f * f0 / floorCount;
            m.FloorSlope5To15 = 100f * f1 / floorCount;
            m.FloorSlope15To30 = 100f * f2 / floorCount;
            m.FloorSlopeOver30 = 100f * f3 / floorCount;
            m.LandUnder5 = 100f * under5 / landCount;
            m.LandOver45 = 100f * over45 / landCount;

            m.ReachablePercent = 100f * Reachable(land, slope, n) / landCount;
            LocalRelief(h, floor, n, out m.FloorReliefMedian, out m.FloorReliefP90);
            m.CoastAlignedPercent = CoastAligned(h, sd, floor, n, cell);
            m.HeightSpreadAtDistance = HeightSpread(h, sd, floor);
            ScanDepressions(h, w, out m.Depressions, out m.DepressionAreaSqm, out m.DepressionMaxDepth, out m.MicroDepressions, out m.MicroMaxDepth);
            m.CurvatureRatio = Curvature(h, floor, n);
            return m;
        }

        /// <summary>Central differences in the interior, one-sided at the border (numpy.gradient).</summary>
        public static void Gradient(float[] h, int n, float cell, int x, int z, out float gx, out float gz)
        {
            var xm = Mathf.Max(0, x - 1);
            var xp = Mathf.Min(n - 1, x + 1);
            var zm = Mathf.Max(0, z - 1);
            var zp = Mathf.Min(n - 1, z + 1);
            gx = (h[z * n + xp] - h[z * n + xm]) / ((xp - xm) * cell);
            gz = (h[zp * n + x] - h[zm * n + x]) / ((zp - zm) * cell);
        }

        public static float SlopeDegrees(float[] h, int n, float cell, int x, int z)
        {
            Gradient(h, n, cell, x, z, out var gx, out var gz);
            return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;
        }

        private static float Percentile(List<float> sorted, float p)
        {
            if (sorted.Count == 0)
            {
                return 0f;
            }

            var position = p / 100f * (sorted.Count - 1);
            var lo = Mathf.FloorToInt(position);
            var hi = Mathf.Min(sorted.Count - 1, lo + 1);
            return Mathf.Lerp(sorted[lo], sorted[hi], position - lo);
        }

        private static int Reachable(bool[] land, float[] slope, int n)
        {
            var seen = new bool[land.Length];
            var queue = new Queue<int>();
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!land[i] || slope[i] > 45f)
                    {
                        continue;
                    }

                    var shore = (x > 0 && !land[i - 1]) || (x < n - 1 && !land[i + 1])
                                || (z > 0 && !land[i - n]) || (z < n - 1 && !land[i + n]);
                    if (shore)
                    {
                        seen[i] = true;
                        queue.Enqueue(i);
                    }
                }
            }

            var count = 0;
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                count++;
                var x = i % n;
                var z = i / n;
                if (x > 0) Visit(i - 1);
                if (x < n - 1) Visit(i + 1);
                if (z > 0) Visit(i - n);
                if (z < n - 1) Visit(i + n);
            }

            return count;

            void Visit(int j)
            {
                if (!seen[j] && land[j] && slope[j] <= 45f)
                {
                    seen[j] = true;
                    queue.Enqueue(j);
                }
            }
        }

        /// <summary>Std of height in a 41 × 41-sample (112 m) window, median and p90 over the floor.</summary>
        private static void LocalRelief(float[] h, bool[] floor, int n, out float median, out float p90)
        {
            const int r = 20;
            var width = n + 1;
            var sum = new double[width * width];
            var sumSq = new double[width * width];
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var v = (double)h[z * n + x];
                    var i = (z + 1) * width + x + 1;
                    sum[i] = v + sum[i - 1] + sum[i - width] - sum[i - width - 1];
                    sumSq[i] = v * v + sumSq[i - 1] + sumSq[i - width] - sumSq[i - width - 1];
                }
            }

            var values = new List<float>();
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    if (!floor[z * n + x])
                    {
                        continue;
                    }

                    var x0 = Mathf.Max(0, x - r);
                    var x1 = Mathf.Min(n - 1, x + r);
                    var z0 = Mathf.Max(0, z - r);
                    var z1 = Mathf.Min(n - 1, z + r);
                    var count = (x1 - x0 + 1) * (z1 - z0 + 1);
                    var s = sum[(z1 + 1) * width + x1 + 1] - sum[z0 * width + x1 + 1] - sum[(z1 + 1) * width + x0] + sum[z0 * width + x0];
                    var s2 = sumSq[(z1 + 1) * width + x1 + 1] - sumSq[z0 * width + x1 + 1] - sumSq[(z1 + 1) * width + x0] + sumSq[z0 * width + x0];
                    var mean = s / count;
                    values.Add((float)Math.Sqrt(Math.Max(0.0, s2 / count - mean * mean)));
                }
            }

            values.Sort();
            median = Percentile(values, 50f);
            p90 = Percentile(values, 90f);
        }

        /// <summary>Share of interior floor whose height gradient lies within 18° of the coast-distance gradient.</summary>
        private static float CoastAligned(float[] h, float[] sd, bool[] floor, int n, float cell)
        {
            var aligned = 0;
            var total = 0;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!floor[i] || sd[i] <= 250f)
                    {
                        continue;
                    }

                    Gradient(h, n, cell, x, z, out var hx, out var hz);
                    Gradient(sd, n, cell, x, z, out var dx, out var dz);
                    var hm = Mathf.Sqrt(hx * hx + hz * hz);
                    var dm = Mathf.Sqrt(dx * dx + dz * dz);
                    if (hm <= 0.01f || dm <= 0.5f)
                    {
                        continue;
                    }

                    total++;
                    if ((hx * dx + hz * dz) / (hm * dm) > 0.95f)
                    {
                        aligned++;
                    }
                }
            }

            return total > 0 ? 100f * aligned / total : 0f;
        }

        /// <summary>Median, over 50 m coast-distance bands beyond 200 m, of the floor height std.</summary>
        private static float HeightSpread(float[] h, float[] sd, bool[] floor)
        {
            const int bands = 16;
            var count = new int[bands];
            var sum = new double[bands];
            var sumSq = new double[bands];
            for (var i = 0; i < h.Length; i++)
            {
                if (!floor[i] || sd[i] <= 200f)
                {
                    continue;
                }

                var band = Mathf.FloorToInt((sd[i] - 200f) / 50f);
                if (band < 0 || band >= bands)
                {
                    continue;
                }

                count[band]++;
                sum[band] += h[i];
                sumSq[band] += (double)h[i] * h[i];
            }

            var spreads = new List<float>();
            for (var b = 0; b < bands; b++)
            {
                if (count[b] <= 300)
                {
                    continue;
                }

                var mean = sum[b] / count[b];
                spreads.Add((float)Math.Sqrt(Math.Max(0.0, sumSq[b] / count[b] - mean * mean)));
            }

            spreads.Sort();
            return Percentile(spreads, 50f);
        }

        /// <summary>Filled minus original on land, from the hydrology package's priority flood.</summary>
        public static float[] FillDepth(float[] h, IslandReliefWorld w)
        {
            var copy = (float[])h.Clone();
            var grid = new HydrologyGrid(0f, 0f, w.Width, w.Width, w.Cell, copy, WorldScale.SeaLevelMeters);
            var ocean = grid.BuildOceanMask();
            var filled = DepressionFill.Fill(grid, ocean);
            var depth = new float[h.Length];
            for (var i = 0; i < h.Length; i++)
            {
                depth[i] = h[i] > WorldScale.SeaLevelMeters ? filled[i] - h[i] : 0f;
            }

            return depth;
        }

        /// <summary>One counted depression: where it is and how deep.</summary>
        public struct DepressionInfo
        {
            public int DeepestIndex;
            public float AreaSqm;
            public float DepthMeters;
        }

        public static void ScanDepressions(
            float[] h, IslandReliefWorld w, out int count, out float area, out float maxDepth, out int micro, out float microMax) =>
            ScanDepressions(h, w, out count, out area, out maxDepth, out micro, out microMax, null);

        public static void ScanDepressions(
            float[] h, IslandReliefWorld w, out int count, out float area, out float maxDepth, out int micro, out float microMax,
            List<DepressionInfo> found)
        {
            var depth = FillDepth(h, w);
            var n = w.Width;
            var cellArea = w.Cell * w.Cell;
            var label = new int[h.Length];
            count = 0;
            area = 0f;
            maxDepth = 0f;
            micro = 0;
            microMax = 0f;
            var stack = new Stack<int>();

            for (var start = 0; start < h.Length; start++)
            {
                if (label[start] != 0 || depth[start] <= 0.05f)
                {
                    continue;
                }

                label[start] = 1;
                stack.Push(start);
                var cells = 0;
                var deepest = 0f;
                var deepestIndex = start;
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    cells++;
                    if (depth[i] > deepest)
                    {
                        deepest = depth[i];
                        deepestIndex = i;
                    }
                    var x = i % n;
                    var z = i / n;
                    if (x > 0) Push(i - 1);
                    if (x < n - 1) Push(i + 1);
                    if (z > 0) Push(i - n);
                    if (z < n - 1) Push(i + n);
                }

                var componentArea = cells * cellArea;
                if (deepest >= DepressionMinDepthMeters && componentArea >= DepressionMinAreaSqm)
                {
                    count++;
                    area += componentArea;
                    maxDepth = Mathf.Max(maxDepth, deepest);
                    found?.Add(new DepressionInfo { DeepestIndex = deepestIndex, AreaSqm = componentArea, DepthMeters = deepest });
                }
                else
                {
                    micro++;
                    microMax = Mathf.Max(microMax, deepest);
                }
            }

            void Push(int j)
            {
                if (label[j] == 0 && depth[j] > 0.05f)
                {
                    label[j] = 1;
                    stack.Push(j);
                }
            }
        }

        private static float Curvature(float[] h, bool[] floor, int n)
        {
            double cx = 0, cz = 0;
            long count = 0;
            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (!floor[i])
                    {
                        continue;
                    }

                    cx += Math.Abs(h[i + 1] - 2f * h[i] + h[i - 1]);
                    cz += Math.Abs(h[i + n] - 2f * h[i] + h[i - n]);
                    count++;
                }
            }

            return count > 0 && cz > 0 ? (float)(cx / cz) : 1f;
        }
    }

    /// <summary>The §19.2 structural checks that need more than a lattice metric.</summary>
    public sealed class IslandReliefChecks
    {
        public int SeaSamples, SeaSamplesWithRelief, LandMaskChanges, BoundsViolations;
        public float H4AMax, ReliefMax, ReliefMeanLand, KnollMax, ApronMax, ApronCap;
        public float UplandPercent, LowlandPercent, KnollFootprintPercent, ApronZonePercent;
        public int ApronRays, ApronRayViolations;
        public float ApronRayWorstRise;

        /// <summary>The same rays judged on the ungated surface P_A = A / Γ, to separate the gate from the reach.</summary>
        public int ApronRayUngatedViolations;
        public float ApronRayUngatedWorstRise;
        public List<string> ApronRayLines = new List<string>();
        public int Trajectories;
        public float TrajectoryReliefRise, TrajectoryPlateApronRise, TrajectoryWorstClimb;
        public List<string> TrajectoryLines = new List<string>();
        public int Outlets;
        public float OutletWorstRise;

        /// <summary>Largest adverse rise R4A adds to the ground itself downstream of an outlet (H4A minus H2).</summary>
        public float OutletWorstGroundAdded;
        public List<string> OutletLines = new List<string>();

        public float CoastalFloorMax;
        public int S8ACount, S8MCount;
        public float S8AMaxDepth, S8MMaxDepth, S8AAreaSqm, S8MAreaSqm;
        public int S8CViolations, S8CSamples;
        public float S8CMinDerivative = float.PositiveInfinity;
        public float LandBeyond800Percent;
        public int ApronNormalRays, ApronNormalViolations, ApronBeyondDomainSamples;
        public float ApronNormalWorstRise, ApronBeyondDomainMax;
        public float TrajectoryI1H2, TrajectoryI1H4A, TrajectoryI1Increase;
        public int OutletO1Failures, OutletO2Failures, OutletO2Checked, OutletO3Failures;
        public float OutletMinShift, OutletDeclaredAboveMin, CarveMax;
        public bool CarveMeasurable;
        public string CarveNote = string.Empty;
        public int S12Failures;
        public bool[] MassifZone;
        public bool[] LeeZone;

        public static IslandReliefChecks Run(IslandReliefWorld w)
        {
            var k = new IslandReliefChecks();
            var sea = WorldScale.SeaLevelMeters;
            var sd = w.Stage1.SignedDistance;
            var r = w.ReliefField.Heights;
            var landCount = 0;
            double reliefSum = 0;

            for (var i = 0; i < r.Length; i++)
            {
                if (sd[i] <= 0f)
                {
                    k.SeaSamples++;
                    if (r[i] != 0f)
                    {
                        k.SeaSamplesWithRelief++;
                    }
                }

                if ((w.H4A[i] > sea) != (w.H2[i] > sea))
                {
                    k.LandMaskChanges++;
                }

                if (float.IsNaN(r[i]) || float.IsInfinity(r[i]) || r[i] < 0f || r[i] > w.Relief.Settings.MaxReliefMeters
                    || float.IsNaN(w.H4A[i]) || w.H4A[i] > WorldScale.TerrainHeightBudgetMeters)
                {
                    k.BoundsViolations++;
                }

                k.H4AMax = Mathf.Max(k.H4AMax, w.H4A[i]);
                k.ReliefMax = Mathf.Max(k.ReliefMax, r[i]);
                k.KnollMax = Mathf.Max(k.KnollMax, w.Knoll[i]);
                k.ApronMax = Mathf.Max(k.ApronMax, w.Apron[i]);
                k.CoastalFloorMax = Mathf.Max(k.CoastalFloorMax, w.CoastalFloor[i]);

                if (w.H2[i] > sea)
                {
                    landCount++;
                    reliefSum += r[i];
                    if (w.Level[i] >= 0.5f) k.UplandPercent++;
                    if (w.Level[i] < 0.25f) k.LowlandPercent++;
                    if (w.Gate[i] * w.Knoll[i] > 1f) k.KnollFootprintPercent++;
                    if (w.Apron[i] > 1f) k.ApronZonePercent++;
                }
            }

            landCount = Mathf.Max(1, landCount);
            k.ReliefMeanLand = (float)(reliefSum / landCount);
            k.UplandPercent = 100f * k.UplandPercent / landCount;
            k.LowlandPercent = 100f * k.LowlandPercent / landCount;
            k.KnollFootprintPercent = 100f * k.KnollFootprintPercent / landCount;
            k.ApronZonePercent = 100f * k.ApronZonePercent / landCount;

            foreach (var m in w.Relief.ApronMassifs)
            {
                k.ApronCap = Mathf.Max(k.ApronCap, w.Relief.Settings.ApronCapPerCrest * m.CrestHeightMeters);
            }

            ApronRaysCheck(w, k);
            ApronPhysicalCheck(w, k);
            TrajectoryCheck(w, k);
            OutletCheck(w, k);
            OwnershipAndS8(w, k);
            JunctionCertificates(w, k);
            return k;
        }

        /// <summary>Point-query sample at a world point: H1, U2 and the relief through the same functions a caller uses.</summary>
        public static IslandReliefSample SampleAt(IslandReliefWorld w, float x, float z, out float h1, out float u2)
        {
            var sdPoint = w.Stage1.Coast.SignedDistance(x, z);
            h1 = w.Stage1.Model.HeightAt(sdPoint);
            u2 = w.Uplift.UpliftAt(x, z, sdPoint);
            return w.Relief.Sample(x, z, sdPoint, h1, u2);
        }

        private static void JunctionCertificates(IslandReliefWorld w, IslandReliefChecks k)
        {
            foreach (var j in w.Relief.Junctions)
            {
                if (j.Certificate == null || !j.Certificate.Passed
                    || j.Certificate.MaxAdverseDerivative > w.Relief.SlopeBudget + 1e-5f)
                {
                    k.S12Failures++;
                }
            }
        }

        private static void OwnershipAndS8(IslandReliefWorld w, IslandReliefChecks k)
        {
            var n = w.Width;
            var cell = w.Cell;
            var sd = w.Stage1.SignedDistance;
            var u2 = w.Stage2B.UpliftField.Heights;
            k.MassifZone = new bool[sd.Length];
            k.LeeZone = new bool[sd.Length];
            var owned = new bool[sd.Length];
            var beyond = 0;
            var land = 0;

            for (var i = 0; i < sd.Length; i++)
            {
                if (sd[i] <= 0f)
                {
                    continue;
                }

                land++;
                if (sd[i] > 800f)
                {
                    beyond++;
                }

                if (u2[i] > 0f || w.Apron[i] > 0f)
                {
                    k.MassifZone[i] = true;
                    owned[i] = true;
                }
            }

            k.LandBeyond800Percent = land > 0 ? 100f * beyond / land : 0f;

            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (sd[i] <= 0f || owned[i])
                    {
                        continue;
                    }

                    IslandReliefMetrics.Gradient(sd, n, cell, x, z, out var gx, out var gz);
                    var glen = Mathf.Sqrt(gx * gx + gz * gz);
                    if (glen < 0.5f)
                    {
                        continue;
                    }

                    var nx = gx / glen;
                    var nz = gz / glen;
                    var px = w.Stage1.Field.WorldX(x);
                    var pz = w.Stage1.Field.WorldZ(z);
                    var steps = Mathf.CeilToInt(sd[i] / cell) + 2;
                    for (var s = 0; s < steps; s++)
                    {
                        var ix = Mathf.Clamp(Mathf.RoundToInt((px - w.Stage1.Field.OriginX) / cell), 0, n - 1);
                        var iz = Mathf.Clamp(Mathf.RoundToInt((pz - w.Stage1.Field.OriginZ) / cell), 0, n - 1);
                        var j = iz * n + ix;
                        if (k.MassifZone[j])
                        {
                            k.LeeZone[i] = true;
                            owned[i] = true;
                            break;
                        }

                        if (sd[j] <= 0f)
                        {
                            break;
                        }

                        px -= nx * cell;
                        pz -= nz * cell;
                    }
                }
            }

            var found = new List<IslandReliefMetrics.DepressionInfo>();
            IslandReliefMetrics.ScanDepressions(w.H4A, w, out _, out _, out _, out _, out _, found);
            var depth = IslandReliefMetrics.FillDepth(w.H4A, w);
            foreach (var d in found)
            {
                var outside = DepressionHasOutsideCell(depth, owned, n, d.DeepestIndex);
                if (outside)
                {
                    k.S8ACount++;
                    k.S8AMaxDepth = Mathf.Max(k.S8AMaxDepth, d.DepthMeters);
                    k.S8AAreaSqm += d.AreaSqm;
                }
                else
                {
                    k.S8MCount++;
                    k.S8MMaxDepth = Mathf.Max(k.S8MMaxDepth, d.DepthMeters);
                    k.S8MAreaSqm += d.AreaSqm;
                }
            }

            var h1 = w.Stage1.Field.Heights;
            k.S8CMinDerivative = float.PositiveInfinity;
            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (sd[i] <= 0f || owned[i])
                    {
                        continue;
                    }

                    IslandReliefMetrics.Gradient(sd, n, cell, x, z, out var gx, out var gz);
                    var glen = Mathf.Sqrt(gx * gx + gz * gz);
                    if (glen < 0.95f)
                    {
                        continue;
                    }

                    k.S8CSamples++;
                    var nx = gx / glen;
                    var nz = gz / glen;
                    var bxp = BudgetHeight(w, h1, z * n + x + 1);
                    var bxm = BudgetHeight(w, h1, z * n + x - 1);
                    var bzp = BudgetHeight(w, h1, (z + 1) * n + x);
                    var bzm = BudgetHeight(w, h1, (z - 1) * n + x);
                    var dbx = (bxp - bxm) / (2f * cell);
                    var dbz = (bzp - bzm) / (2f * cell);
                    var deriv = dbx * nx + dbz * nz;
                    k.S8CMinDerivative = Mathf.Min(k.S8CMinDerivative, deriv);
                    if (deriv < -1e-3f)
                    {
                        k.S8CViolations++;
                    }
                }
            }

            if (k.S8CSamples == 0 || float.IsInfinity(k.S8CMinDerivative))
            {
                k.S8CMinDerivative = 0f;
            }
        }

        private static float BudgetHeight(IslandReliefWorld w, float[] h1, int i) =>
            h1[i] + w.CoastalFloor[i] + w.Gate[i] * (w.Plate[i] + w.Knoll[i]);

        private static bool DepressionHasOutsideCell(float[] depth, bool[] owned, int n, int start)
        {
            var seen = new bool[depth.Length];
            var stack = new Stack<int>();
            seen[start] = true;
            stack.Push(start);
            var outside = false;
            while (stack.Count > 0)
            {
                var i = stack.Pop();
                if (!owned[i])
                {
                    outside = true;
                }

                var x = i % n;
                var z = i / n;
                Try(x > 0, i - 1);
                Try(x < n - 1, i + 1);
                Try(z > 0, i - n);
                Try(z < n - 1, i + n);
            }

            return outside;

            void Try(bool ok, int j)
            {
                if (ok && !seen[j] && depth[j] > 0.05f)
                {
                    seen[j] = true;
                    stack.Push(j);
                }
            }
        }

        private static void ApronPhysicalCheck(IslandReliefWorld w, IslandReliefChecks k)
        {
            var field = w.Stage1.Field;
            var c = CultureInfo.InvariantCulture;
            foreach (var m in w.Relief.ApronMassifs)
            {
                w.Relief.ApronDomain(m.Index, out _, out _, out var domain);
                for (var z = 0; z < field.Height; z += 4)
                {
                    for (var x = 0; x < field.Width; x += 4)
                    {
                        var wx = field.WorldX(x);
                        var wz = field.WorldZ(z);
                        m.Project(wx, wz, out var s, out var offset);
                        var p = new Vector2(wx, wz);
                        var dist = s > 0f && s < 1f
                            ? Mathf.Abs(offset)
                            : (p - (s <= 0f ? m.PointAt(0f) : m.PointAt(1f))).magnitude;
                        if (dist < domain)
                        {
                            continue;
                        }

                        var pa = w.Relief.ApronSurfaceAt(m.Index, wx, wz);
                        if (pa > 1e-3f)
                        {
                            k.ApronBeyondDomainSamples++;
                            k.ApronBeyondDomainMax = Mathf.Max(k.ApronBeyondDomainMax, pa);
                        }
                    }
                }

                for (var i = 0; i < 24; i++)
                {
                    var s = (i + 0.5f) / 24f;
                    ProbeExactNormal(w, k, m, s, 1f, domain);
                    ProbeExactNormal(w, k, m, s, -1f, domain);
                }

                for (var a = 0; a < 8; a++)
                {
                    var angle = (-70f + 20f * a) * Mathf.Deg2Rad;
                    ProbeEndProfile(w, k, m, 0f, angle, domain);
                    ProbeEndProfile(w, k, m, 1f, angle, domain);
                }
            }

            if (k.ApronNormalViolations > 0)
            {
                k.ApronRayLines.Add(
                    $"S10 P_A normals: {k.ApronNormalViolations} rises, worst {k.ApronNormalWorstRise.ToString("F4", c)} m; "
                    + $"beyond-domain max {k.ApronBeyondDomainMax.ToString("F4", c)} m");
            }
        }

        /// <summary>
        /// Offset direction at arc <paramref name="s"/> from a 1 m chord of <see cref="MountainMassif.PointAt"/>,
        /// matching the interpolating tangent <see cref="MountainMassif.Project"/> uses, not a 0.005-s stencil.
        /// </summary>
        private static void ProbeExactNormal(
            IslandReliefWorld w, IslandReliefChecks k, MountainMassif m, float s0, float side, float domain)
        {
            k.ApronNormalRays++;
            var previous = float.NaN;
            var rayRise = 0f;
            var sign = side >= 0f ? 1f : -1f;
            for (var t = 0f; t <= 800f; t += 5f)
            {
                var pa = t >= domain ? 0f : w.Relief.ApronProfileAt(m.Index, s0, sign * Mathf.Max(t, 1e-3f), 0f);
                RecordApronSample(k, pa, t, domain, ref previous, ref rayRise);
            }

            if (rayRise > 0.001f)
            {
                k.ApronNormalViolations++;
                k.ApronNormalWorstRise = Mathf.Max(k.ApronNormalWorstRise, rayRise);
            }
        }

        private static void ProbeEndProfile(
            IslandReliefWorld w, IslandReliefChecks k, MountainMassif m, float sEnd, float angle, float domain)
        {
            k.ApronNormalRays++;
            var previous = float.NaN;
            var rayRise = 0f;
            var endness = Mathf.Clamp01(Mathf.Abs(Mathf.Cos(angle)));
            var sin = Mathf.Sin(angle);
            for (var t = 0f; t <= 800f; t += 5f)
            {
                var offset = Mathf.Max(t, 1e-3f) * (Mathf.Abs(sin) < 1e-4f ? 1e-3f : sin);
                var pa = t >= domain ? 0f : w.Relief.ApronProfileAt(m.Index, sEnd, offset, endness);
                RecordApronSample(k, pa, t, domain, ref previous, ref rayRise);
            }

            if (rayRise > 0.001f)
            {
                k.ApronNormalViolations++;
                k.ApronNormalWorstRise = Mathf.Max(k.ApronNormalWorstRise, rayRise);
            }
        }

        private static void RecordApronSample(
            IslandReliefChecks k, float pa, float dist, float domain, ref float previous, ref float rayRise)
        {
            if (dist >= domain && pa > 1e-3f)
            {
                k.ApronBeyondDomainSamples++;
                k.ApronBeyondDomainMax = Mathf.Max(k.ApronBeyondDomainMax, pa);
            }

            if (!float.IsNaN(previous))
            {
                rayRise = Mathf.Max(rayRise, pa - previous);
            }

            previous = pa;
        }

        /// <summary>
        /// 64 outward rays per massif: 24 stations × 2 sides along the axis normals, and 8 rays fanning out
        /// of each end. Beyond the massif's footprint (uplift exactly zero) the fill is the apron surface
        /// itself and must not rise outwards.
        /// </summary>
        private static void ApronRaysCheck(IslandReliefWorld w, IslandReliefChecks k)
        {
            foreach (var m in w.Relief.ApronMassifs)
            {
                var rays = new List<(Vector2 origin, Vector2 dir)>();
                for (var i = 0; i < 24; i++)
                {
                    var s = (i + 0.5f) / 24f;
                    var origin = m.PointAt(s);
                    var tangent = (m.PointAt(Mathf.Min(1f, s + 0.005f)) - m.PointAt(Mathf.Max(0f, s - 0.005f))).normalized;
                    var normal = new Vector2(-tangent.y, tangent.x);
                    rays.Add((origin, normal));
                    rays.Add((origin, -normal));
                }

                var start = m.PointAt(0f);
                var end = m.PointAt(1f);
                var startOut = (start - m.PointAt(0.005f)).normalized;
                var endOut = (end - m.PointAt(0.995f)).normalized;
                for (var a = 0; a < 8; a++)
                {
                    var angle = (-70f + 20f * a) * Mathf.Deg2Rad;
                    rays.Add((start, Rotate(startOut, angle)));
                    rays.Add((end, Rotate(endOut, angle)));
                }

                var c = CultureInfo.InvariantCulture;
                var rayIndex = 0;
                foreach (var (origin, dir) in rays)
                {
                    k.ApronRays++;
                    var previous = float.NaN;
                    var previousUngated = float.NaN;
                    var previousGate = float.NaN;
                    var violated = false;
                    var ungatedViolated = false;
                    var ungatedWorst = 0f;
                    var ungatedTrace = string.Empty;
                    var worst = 0f;
                    var worstAt = 0f;
                    var worstGateFrom = 0f;
                    var worstGateTo = 0f;
                    for (var t = 0f; t <= 800f; t += 5f)
                    {
                        var p = origin + dir * t;
                        if (p.x < 0f || p.y < 0f || p.x > WorldScale.WorldExtentXMeters || p.y > WorldScale.WorldExtentZMeters)
                        {
                            break;
                        }

                        var sample = SampleAt(w, p.x, p.y, out _, out var u2);
                        if (u2 != 0f)
                        {
                            previous = float.NaN;
                            previousUngated = float.NaN;
                            continue;
                        }

                        var ungated = sample.Gate > 0f ? sample.Apron / sample.Gate : float.NaN;
                        if (!float.IsNaN(previous))
                        {
                            var rise = sample.Apron - previous;
                            if (rise > 1e-4f)
                            {
                                violated = true;
                                k.ApronRayWorstRise = Mathf.Max(k.ApronRayWorstRise, rise);
                                if (rise > worst)
                                {
                                    worst = rise;
                                    worstAt = t;
                                    worstGateFrom = previousGate;
                                    worstGateTo = sample.Gate;
                                }
                            }
                        }

                        if (!float.IsNaN(previousUngated) && !float.IsNaN(ungated))
                        {
                            var rise = ungated - previousUngated;
                            if (rise > 1e-3f)
                            {
                                if (!ungatedViolated || rise > ungatedWorst)
                                {
                                    var before = origin + dir * (t - 5f);
                                    m.Project(before.x, before.y, out var s0, out var o0);
                                    m.Project(p.x, p.y, out var s1, out var o1);
                                    ungatedWorst = rise;
                                    ungatedTrace =
                                        $"; ungated step {rise.ToString("F3", c)} m at {t.ToString("F0", c)} m: projection s {s0.ToString("F3", c)} -> {s1.ToString("F3", c)}, "
                                        + $"offset {o0.ToString("F0", c)} -> {o1.ToString("F0", c)} m (axis reach {m.Axis.ReachMeters.ToString("F0", c)} m)";
                                }

                                ungatedViolated = true;
                                k.ApronRayUngatedWorstRise = Mathf.Max(k.ApronRayUngatedWorstRise, rise);
                            }
                        }

                        previous = sample.Apron;
                        previousUngated = ungated;
                        previousGate = sample.Gate;
                    }

                    if (violated)
                    {
                        k.ApronRayViolations++;
                        k.ApronRayLines.Add(
                            $"massif {m.Index} ray {rayIndex} from ({origin.x.ToString("F0", c)},{origin.y.ToString("F0", c)}) dir ({dir.x.ToString("F2", c)},{dir.y.ToString("F2", c)}): "
                            + $"worst rise {worst.ToString("F3", c)} m at {worstAt.ToString("F0", c)} m, gate {worstGateFrom.ToString("F3", c)} -> {worstGateTo.ToString("F3", c)}"
                            + (ungatedViolated ? ", ungated surface ALSO rises" + ungatedTrace : ", ungated surface monotone"));
                    }

                    if (ungatedViolated)
                    {
                        k.ApronRayUngatedViolations++;
                    }

                    rayIndex++;
                }
            }
        }

        /// <summary>Site index of the placed knoll whose centre is nearest a point, or -1.</summary>
        public static int NearestKnoll(IslandRelief relief, Vector2 p)
        {
            var best = -1;
            var bestSq = float.PositiveInfinity;
            foreach (var knoll in relief.Knolls)
            {
                var sq = (knoll.Center - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = knoll.SiteIndex;
                }
            }

            return best;
        }

        /// <summary>
        /// Where each counted depression of H4A sits, what every relief part contributes there, and how many
        /// depressions each part produces on its own on top of H2.
        /// </summary>
        public static List<string> DepressionAttribution(IslandReliefWorld w)
        {
            var lines = new List<string>();
            var found = new List<IslandReliefMetrics.DepressionInfo>();
            IslandReliefMetrics.ScanDepressions(w.H4A, w, out var count, out _, out _, out _, out _, found);
            if (count == 0)
            {
                return lines;
            }

            var c = CultureInfo.InvariantCulture;
            var n = w.Width;
            foreach (var d in found)
            {
                var i = d.DeepestIndex;
                var p = new Vector2(w.Stage1.Field.WorldX(i % n), w.Stage1.Field.WorldZ(i / n));
                var junction = float.PositiveInfinity;
                foreach (var j in w.Relief.Junctions)
                {
                    junction = Mathf.Min(junction, (j.Position - p).magnitude);
                }

                var knollSite = NearestKnoll(w.Relief, p);
                var knollDistance = float.PositiveInfinity;
                foreach (var kn in w.Relief.Knolls)
                {
                    if (kn.SiteIndex == knollSite)
                    {
                        knollDistance = (kn.Center - p).magnitude;
                    }
                }

                var zm = w.Stage2B.UpliftField.Heights[i] > 0f || w.Apron[i] > 0f;
                lines.Add(
                    $"depression at ({p.x.ToString("F0", c)},{p.y.ToString("F0", c)}) depth {d.DepthMeters.ToString("F2", c)} m area {d.AreaSqm.ToString("F0", c)} m2: "
                    + $"sd {w.Stage1.SignedDistance[i].ToString("F0", c)} m, H1 {w.Stage1.Field.Heights[i].ToString("F1", c)} m, M {w.CoastalFloor[i].ToString("F2", c)} m, "
                    + $"gate {w.Gate[i].ToString("F2", c)}, level {w.Level[i].ToString("F2", c)}, "
                    + $"plate {(w.Gate[i] * w.Plate[i]).ToString("F2", c)} m, knoll {(w.Gate[i] * w.Knoll[i]).ToString("F2", c)} m, apron {w.Apron[i].ToString("F2", c)} m, U2 {w.Stage2B.UpliftField.Heights[i].ToString("F2", c)} m; "
                    + $"Z_M {zm}; nearest junction {junction.ToString("F0", c)} m, nearest knoll site {knollSite} at {knollDistance.ToString("F0", c)} m");
            }

            var parts = new (string name, Func<int, float> part)[]
            {
                ("plate only", i => w.Gate[i] * w.Plate[i]),
                ("knolls only", i => w.Gate[i] * w.Knoll[i]),
                ("apron only", i => w.Apron[i]),
                ("plate+knolls", i => w.Gate[i] * (w.Plate[i] + w.Knoll[i])),
                ("plate+apron", i => w.Gate[i] * w.Plate[i] + w.Apron[i]),
            };

            foreach (var (name, part) in parts)
            {
                var h = new float[w.H2.Length];
                for (var i = 0; i < h.Length; i++)
                {
                    h[i] = w.H2[i] + part(i);
                }

                IslandReliefMetrics.ScanDepressions(h, w, out var partCount, out var area, out var deepest, out _, out _);
                lines.Add($"{name,-13}: {partCount} depression(s), {area.ToString("F0", c)} m2, deepest {deepest.ToString("F2", c)} m");
            }

            return lines;
        }

        private static Vector2 Rotate(Vector2 v, float angle)
        {
            var c = Mathf.Cos(angle);
            var s = Mathf.Sin(angle);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>
        /// Along every Stage 3C final trajectory downstream of its toe: the adverse rise of R4A and of its
        /// plate + apron part (sum of positive increments), and the largest single climb of R4A.
        /// </summary>
        private static void TrajectoryCheck(IslandReliefWorld w, IslandReliefChecks k)
        {
            if (w.Trajectories == null)
            {
                return;
            }

            var c = CultureInfo.InvariantCulture;
            foreach (var trajectory in w.Trajectories.Trajectories)
            {
                if (!trajectory.HasGeometry)
                {
                    continue;
                }

                k.Trajectories++;
                var reliefRise = 0f;
                var plateApronRise = 0f;
                var knollRise = 0f;
                var groundBefore = 0f;
                var groundAfter = 0f;
                var climb = 0f;
                var runningMin = float.PositiveInfinity;
                var minH2 = float.PositiveInfinity;
                var minH4 = float.PositiveInfinity;
                var i1H2 = 0f;
                var i1H4 = 0f;
                var previousR = float.NaN;
                var previousPA = float.NaN;
                var previousK = float.NaN;
                var previousH2 = float.NaN;
                var previousH4 = float.NaN;
                var knollSites = new List<int>();

                for (var i = Mathf.Max(0, trajectory.ToeIndex); i < trajectory.Points.Length; i++)
                {
                    var p = trajectory.Points[i];
                    var sample = SampleAt(w, p.x, p.y, out var h1, out var u2);
                    var total = sample.Total;
                    var plateApron = sample.Gate * sample.Plate + sample.Apron;
                    var knoll = sample.Gate * sample.Knoll;
                    var h2 = h1 + u2;
                    var h4 = h2 + total;
                    if (!float.IsNaN(previousR))
                    {
                        reliefRise += Mathf.Max(0f, total - previousR);
                        plateApronRise += Mathf.Max(0f, plateApron - previousPA);
                        knollRise += Mathf.Max(0f, knoll - previousK);
                        groundBefore += Mathf.Max(0f, h2 - previousH2);
                        groundAfter += Mathf.Max(0f, h4 - previousH4);
                    }

                    if (knoll > 0.5f)
                    {
                        var nearest = NearestKnoll(w.Relief, p);
                        if (nearest >= 0 && !knollSites.Contains(nearest))
                        {
                            knollSites.Add(nearest);
                        }
                    }

                    runningMin = Mathf.Min(runningMin, total);
                    climb = Mathf.Max(climb, total - runningMin);
                    minH2 = Mathf.Min(minH2, h2);
                    minH4 = Mathf.Min(minH4, h4);
                    i1H2 = Mathf.Max(i1H2, Mathf.Max(0f, h1 - minH2));
                    i1H4 = Mathf.Max(i1H4, Mathf.Max(0f, h1 - minH4));
                    previousR = total;
                    previousPA = plateApron;
                    previousK = knoll;
                    previousH2 = h2;
                    previousH4 = h4;
                }

                k.TrajectoryReliefRise = Mathf.Max(k.TrajectoryReliefRise, reliefRise);
                k.TrajectoryPlateApronRise = Mathf.Max(k.TrajectoryPlateApronRise, plateApronRise);
                k.TrajectoryWorstClimb = Mathf.Max(k.TrajectoryWorstClimb, climb);
                k.TrajectoryI1H2 = Mathf.Max(k.TrajectoryI1H2, i1H2);
                k.TrajectoryI1H4A = Mathf.Max(k.TrajectoryI1H4A, i1H4);
                k.TrajectoryI1Increase = Mathf.Max(k.TrajectoryI1Increase, i1H4 - i1H2);
                k.TrajectoryLines.Add(
                    $"V{trajectory.Id}: C(R4A) {climb.ToString("F2", c)} m, R4A adverse {reliefRise.ToString("F2", c)} m (report), plate+apron {plateApronRise.ToString("F2", c)} m, "
                    + $"knolls {knollRise.ToString("F2", c)} m over sites [{string.Join(",", knollSites)}]; "
                    + $"I1 H2 {i1H2.ToString("F3", c)} -> H4A {i1H4.ToString("F3", c)}; "
                    + $"ground adverse H2 {groundBefore.ToString("F2", c)} m -> H4A {groundAfter.ToString("F2", c)} m");
            }
        }

        /// <summary>
        /// S11-O: O-1 corridor end below outlet on H4A; O-2 post-carve rim margin; O-3 I1/C downstream;
        /// O-4 reported shift of the outlet-run minimum.
        /// </summary>
        private static void OutletCheck(IslandReliefWorld w, IslandReliefChecks k)
        {
            if (w.Basins == null)
            {
                return;
            }

            k.CarveMeasurable = w.Carve != null;
            k.CarveNote = k.CarveMeasurable
                ? string.Empty
                : "O-2 UNMEASURABLE WITH CURRENT PUBLIC/DIAGNOSTIC API";
            var c = CultureInfo.InvariantCulture;

            foreach (var basin in w.Basins.Basins)
            {
                k.Outlets++;
                MinOnRun(w, basin, BoundarySource.Outlet, out var outH2, out var outH4, out var outCarved, out var outMinPoint, out var outDeclared);
                MinOnRun(w, basin, BoundarySource.Crest, out var ridgeH2, out var ridgeH4, out var ridgeCarved, out _, out _);
                MinOnRun(w, basin, BoundarySource.Spur, out var spurH2, out var spurH4, out var spurCarved, out _, out _);
                ridgeH2 = Mathf.Min(ridgeH2, spurH2);
                ridgeH4 = Mathf.Min(ridgeH4, spurH4);
                ridgeCarved = Mathf.Min(ridgeCarved, spurCarved);
                k.CarveMax = Mathf.Max(k.CarveMax, outH4 - outCarved, ridgeH4 - ridgeCarved);

                var endH4 = float.PositiveInfinity;
                FinalValleyTrajectory trajectory = null;
                if (w.Trajectories != null)
                {
                    foreach (var t in w.Trajectories.Trajectories)
                    {
                        if (t.Id == basin.CorridorId)
                        {
                            trajectory = t;
                            break;
                        }
                    }
                }

                if (trajectory != null && trajectory.HasGeometry)
                {
                    var end = trajectory.EndPoint;
                    var sample = SampleAt(w, end.x, end.y, out var h1, out var u2);
                    endH4 = h1 + u2 + sample.Total;
                }

                if (!(endH4 < outH4))
                {
                    k.OutletO1Failures++;
                }

                var outletH4 = outH4;
                if (trajectory != null && trajectory.HasGeometry)
                {
                    IncludeCarved(w, trajectory.Points[ClosestIndex(trajectory.Points, basin.OutletPointXZ)],
                        ref outH2, ref outH4, ref outCarved, ref outMinPoint);
                }

                IncludeCarved(w, basin.OutletPointXZ, ref outH2, ref outH4, ref outCarved, ref outMinPoint);

                if (trajectory != null && trajectory.HasGeometry)
                {
                    var radius = Mathf.Max(40f, 2f * trajectory.HalfWidthMeters);
                    var nearbyC = 0f;
                    for (var i = 0; i < trajectory.Points.Length; i++)
                    {
                        var p = trajectory.Points[i];
                        if ((p - basin.OutletPointXZ).magnitude > radius && (p - outMinPoint).magnitude > radius)
                        {
                            continue;
                        }

                        nearbyC = Mathf.Max(nearbyC, CarveAt(w, p));
                    }

                    nearbyC = Mathf.Max(nearbyC, CarveAt(w, basin.OutletPointXZ), CarveAt(w, outMinPoint));
                    outCarved = Mathf.Min(outCarved, outletH4 - nearbyC);
                }

                var marginH2 = ridgeH2 - outH2;
                var outC = outH4 - outCarved;
                var ridgeC = ridgeH4 - ridgeCarved;
                if (marginH2 > 0f)
                {
                    k.OutletO2Checked++;
                    if (k.CarveMeasurable)
                    {
                        if (ridgeCarved - outCarved <= 0f)
                        {
                            k.OutletO2Failures++;
                        }
                    }
                }

                var i1H2 = 0f;
                var i1H4 = 0f;
                var climbR = 0f;
                if (trajectory != null && trajectory.HasGeometry)
                {
                    var start = Mathf.Max(
                        ClosestIndex(trajectory.Points, basin.OutletPointXZ),
                        Mathf.Max(0, trajectory.ToeIndex));
                    var minH2 = float.PositiveInfinity;
                    var minH4 = float.PositiveInfinity;
                    var minR = float.PositiveInfinity;
                    for (var i = start; i < trajectory.Points.Length; i++)
                    {
                        var p = trajectory.Points[i];
                        var sample = SampleAt(w, p.x, p.y, out var h1, out var u2);
                        var h2 = h1 + u2;
                        var h4 = h2 + sample.Total;
                        minH2 = Mathf.Min(minH2, h2);
                        minH4 = Mathf.Min(minH4, h4);
                        minR = Mathf.Min(minR, sample.Total);
                        i1H2 = Mathf.Max(i1H2, Mathf.Max(0f, h1 - minH2));
                        i1H4 = Mathf.Max(i1H4, Mathf.Max(0f, h1 - minH4));
                        climbR = Mathf.Max(climbR, sample.Total - minR);
                    }

                    if (i1H4 > i1H2 + 0.001f || climbR > 7.2f)
                    {
                        k.OutletO3Failures++;
                    }
                }

                k.OutletMinShift = Mathf.Max(k.OutletMinShift, (outMinPoint - basin.OutletPointXZ).magnitude);
                k.OutletDeclaredAboveMin = Mathf.Max(k.OutletDeclaredAboveMin, outDeclared - outH4);
                k.OutletLines.Add(
                    $"basin {basin.Id} (V{basin.CorridorId}) O-1 end {endH4.ToString("F1", c)} < outlet {outletH4.ToString("F1", c)} "
                    + $"{(endH4 < outletH4 ? "PASS" : "FAIL")}; H2 margin {marginH2.ToString("F1", c)} m; "
                    + $"carve out {outC.ToString("F2", c)} / ridge {ridgeC.ToString("F2", c)} m; "
                    + $"O-4 shift {(outMinPoint - basin.OutletPointXZ).magnitude.ToString("F0", c)} m, declared-above {(outDeclared - outH4).ToString("F2", c)} m; "
                    + $"O-3 I1 {i1H2.ToString("F3", c)}->{i1H4.ToString("F3", c)} C(R) {climbR.ToString("F2", c)}");
            }
        }

        private static int ClosestIndex(Vector2[] points, Vector2 p)
        {
            var best = 0;
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i < points.Length; i++)
            {
                var sq = (points[i] - p).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            return best;
        }

        private static void IncludeCarved(
            IslandReliefWorld w, Vector2 p, ref float minH2, ref float minH4, ref float minCarved, ref Vector2 minH4Point)
        {
            var sample = SampleAt(w, p.x, p.y, out var h1, out var u2);
            var h2 = h1 + u2;
            var h4 = h2 + sample.Total;
            var carved = h4 - CarveAt(w, p);
            minH2 = Mathf.Min(minH2, h2);
            if (h4 < minH4)
            {
                minH4 = h4;
                minH4Point = p;
            }

            minCarved = Mathf.Min(minCarved, carved);
        }

        private static float CarveAt(IslandReliefWorld w, Vector2 p)
        {
            if (w.Carve == null || w.Stage1?.Coast == null)
            {
                return 0f;
            }

            var sd = w.Stage1.Coast.SignedDistance(p.x, p.y);
            return w.Carve.Depth(p.x, p.y, w.Carve.GateFor(sd), sd > 0f);
        }

        private static void MinOnRun(
            IslandReliefWorld w,
            StructuralBasin basin,
            BoundarySource source,
            out float minH2,
            out float minH4,
            out float minCarved,
            out Vector2 minH4Point,
            out float declaredH4)
        {
            minH2 = float.PositiveInfinity;
            minH4 = float.PositiveInfinity;
            minCarved = float.PositiveInfinity;
            minH4Point = basin.OutletPointXZ;
            declaredH4 = 0f;
            var ring = basin.Boundary;
            if (ring == null || ring.Length == 0 || basin.Segments == null)
            {
                return;
            }

            var declared = SampleAt(w, basin.OutletPointXZ.x, basin.OutletPointXZ.y, out var dh1, out var du2);
            declaredH4 = dh1 + du2 + declared.Total;

            foreach (var segment in basin.Segments)
            {
                if (source == BoundarySource.Crest || source == BoundarySource.Spur)
                {
                    if (!segment.IsRidge)
                    {
                        continue;
                    }
                }
                else if (segment.Source != source)
                {
                    continue;
                }

                for (var i = 0; i < segment.PointCount; i++)
                {
                    var p = ring[(segment.FirstIndex + i) % ring.Length];
                    IncludeCarved(w, p, ref minH2, ref minH4, ref minCarved, ref minH4Point);
                }
            }
        }
    }

    /// <summary>The design §11 target column, evaluated. Thresholds, not expected values.</summary>
    public static class IslandReliefTargets
    {
        public sealed class Result
        {
            public string Name;
            public string Actual;
            public string Target;
            public bool Pass;
            public bool Hard;
        }

        public static List<Result> Evaluate(
            IslandReliefWorld w, IslandReliefMetrics b, IslandReliefMetrics r, IslandReliefChecks k)
        {
            var c = CultureInfo.InvariantCulture;
            var noMassif = !w.Relief.HasMassif;
            var list = new List<Result>();

            void Add(string name, float actual, string target, bool pass, bool hard) =>
                list.Add(new Result { Name = name, Actual = actual.ToString("F2", c), Target = target, Pass = pass, Hard = hard });

            Add("land 70-110 %", r.Band70To110, noMassif ? "12-32" : "15-32",
                r.Band70To110 >= (noMassif ? 12f : 15f) && r.Band70To110 <= 32f, true);
            Add("land p50 m", r.Percentiles[2], "52-65", r.Percentiles[2] >= 52f && r.Percentiles[2] <= 65f, false);
            Add("land p90 m", r.Percentiles[4], noMassif ? "72-95" : "100-135",
                noMassif ? r.Percentiles[4] >= 72f && r.Percentiles[4] <= 95f : r.Percentiles[4] >= 100f && r.Percentiles[4] <= 135f, false);
            Add("floor local relief median m", r.FloorReliefMedian, noMassif ? ">=3.5" : ">=4.0",
                r.FloorReliefMedian >= (noMassif ? 3.5f : 4f), true);
            Add("floor local relief p90 m", r.FloorReliefP90, ">=7.0", r.FloorReliefP90 >= 7f, true);
            Add("height spread at fixed coast distance m", r.HeightSpreadAtDistance, ">=4.0", r.HeightSpreadAtDistance >= 4f, true);
            Add("coast-aligned gradient %", r.CoastAlignedPercent, noMassif ? "<=70" : "<=65",
                r.CoastAlignedPercent <= (noMassif ? 70f : 65f), true);
            Add("floor slope 0-5 %", r.FloorSlope0To5, noMassif ? ">=40" : ">=28", r.FloorSlope0To5 >= (noMassif ? 40f : 28f), false);
            Add("land <5 %", r.LandUnder5, $">=24 and >={(b.LandUnder5 - 15f).ToString("F1", c)}",
                r.LandUnder5 >= 24f && r.LandUnder5 >= b.LandUnder5 - 15f, true);
            Add("floor slope 15-30 %", r.FloorSlope15To30, "<=20", r.FloorSlope15To30 <= 20f, false);
            Add("floor slope >30 %", r.FloorSlopeOver30, "<=2", r.FloorSlopeOver30 <= 2f, false);
            Add("land >45 %", r.LandOver45, $"<={(b.LandOver45 + 0.5f).ToString("F2", c)}", r.LandOver45 <= b.LandOver45 + 0.5f, true);
            Add("reachable <=45 %", r.ReachablePercent, $">={(b.ReachablePercent - 0.5f).ToString("F2", c)}",
                r.ReachablePercent >= b.ReachablePercent - 0.5f, true);
            Add("lowland share %", k.LowlandPercent, "report (reserve >=20% land)", w.Relief.ReservedLandShare >= 0.20f - 1e-4f, true);
            Add("upland share %", k.UplandPercent, "report", true, false);
            Add("knolls", w.Relief.Knolls.Count, "8-25", w.Relief.Knolls.Count >= 8 && w.Relief.Knolls.Count <= 25, false);
            Add("knoll footprint %", k.KnollFootprintPercent, "<=20", k.KnollFootprintPercent <= 20f, false);
            if (w.Relief.HasMassif)
            {
                Add("apron zone %", k.ApronZonePercent, "<=25", k.ApronZonePercent <= 25f, false);
            }

            Add("R4A max m", k.ReliefMax, "<=100", k.ReliefMax <= w.Relief.Settings.MaxReliefMeters, true);
            Add("R4A mean on land m", k.ReliefMeanLand, "5-14", k.ReliefMeanLand >= 5f && k.ReliefMeanLand <= 14f, true);
            Add("highest point change m", r.HighestPoint - b.HighestPoint, "<=30", r.HighestPoint - b.HighestPoint <= 30f, false);
            Add("depressions S8-A (outside Z_M∪L_M)", k.S8ACount, "0", k.S8ACount == 0, true);
            Add("depressions S8-M (inside, canonical)", k.S8MCount, "0", k.S8MCount == 0, true);
            if (w.Trajectories != null && k.Trajectories > 0)
            {
                Add("trajectory C(R4A) m", k.TrajectoryWorstClimb, "<=7.2", k.TrajectoryWorstClimb <= 7.2f, true);
                Add("trajectory plate+apron adverse rise m", k.TrajectoryPlateApronRise, "<=1.5", k.TrajectoryPlateApronRise <= 1.5f, true);
                Add("trajectory I1 increase m", k.TrajectoryI1Increase, "<=0.001", k.TrajectoryI1Increase <= 0.001f, true);
                Add("trajectory R4A adverse sum m", k.TrajectoryReliefRise, "report", true, false);
            }

            Add("curvature ratio", r.CurvatureRatio, "0.85-1.20", r.CurvatureRatio >= 0.85f && r.CurvatureRatio <= 1.2f, false);
            return list;
        }
    }
}
#endif
