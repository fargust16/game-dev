#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using GameDev.World.Town.Geometry;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace GameDev.EditorTools
{
    /// <summary>
    /// G3 research candidate (<c>GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md</c> §11): one connected, branched
    /// mountain system whose ridges and valleys come from one skeleton, measured against production
    /// <c>H4A</c> with G0 and reviewed with G1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Research only, never a terrain authority.</b> Nothing here is referenced by production code,
    /// the bake or a scene. The candidate lattice is written to a file outside <c>Assets</c> and read by
    /// the unchanged G0 (<see cref="GeographyAnalysis"/>, <see cref="GeographyMetrics"/>) and G1
    /// (<see cref="GeometryFirstReview.Run"/>) entry points. Random draws use <c>research.g3.*</c>
    /// streams, which no production stage reads.
    /// </para>
    /// <para>
    /// <b>Composition.</b> <c>G3 = H1 + M + Γ·(P + K) + S·g(d)</c>: Stage 1 and the Stage 4A coastal floor
    /// and plate districts are kept exactly (<c>H4A − U2 − A</c>); the Stage 2B massif <c>U2</c> and its
    /// Stage 4A apron <c>A</c> are replaced by the mountain-system field <c>S</c>, gated to zero at and
    /// seaward of the shore by <c>g(d) = smoothstep(d / G)</c>. <c>S</c> is a skeleton field: an envelope
    /// over one spine and its branches (from Stage 1's coast distance), dissected by valleys that lie
    /// between consecutive spurs, are carved to a monotone floor, and continue to the coast as troughs at
    /// most <c>CorridorIncisionMeters</c> below the floor surface. Every number in
    /// <see cref="G3Settings"/> is either an existing project value (named) or an authoring default.
    /// The iteration that led here is recorded in the research record §11.
    /// </para>
    /// <para>
    /// <b>G3b</b> (variant <c>net</c>, research record §12) keeps that skeleton and replaces "every valley
    /// to its nearest shore" with a converging valley tree: trunks along the low lines between each branch
    /// and its parent and across the lowest saddle (a lowland corridor), with flat floors in their lower
    /// reaches, and side valleys that are the links of a drainage tree routed on the gated surface with
    /// the trunks carved, so they join the trunks or each other before the coast.
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.TerrainGenG3MountainSystemDump.DumpBatch \
    ///   [-g3Seeds a,b] [-g3Variants base,deep,net] [-g3Review true|false] [-g3Out &lt;folder outside Assets&gt;]
    /// </code>
    /// </remarks>
    public static class TerrainGenG3MountainSystemDump
    {
        private const string SeedsArg = "-g3Seeds";
        private const string OutputArg = "-g3Out";
        private const string VariantsArg = "-g3Variants";
        private const string ReviewArg = "-g3Review";

        public static void DumpBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var directory = TerrainGenGeographyDump.ReadArg(args, OutputArg);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "g3");
            }

            var seeds = ParseInts(TerrainGenGeographyDump.ReadArg(args, SeedsArg), TerrainGenIslandReliefDump.CanonicalSeeds);
            var rawVariants = TerrainGenGeographyDump.ReadArg(args, VariantsArg);
            var variants = string.IsNullOrEmpty(rawVariants) ? new[] { "base", "deep" } : rawVariants.Split(',');
            var review = TerrainGenGeographyDump.ReadArg(args, ReviewArg) != "false";
            Debug.Log(Run(seeds, directory, variants, review));
        }

        /// <summary>
        /// <c>base</c>: every value as in <see cref="G3Settings.Default"/>, coast gate 200 m (the massif's
        /// own <c>UpliftCoastGateMeters</c>). <c>deep</c>: the same skeleton with valleys 0.50 instead of 0.30
        /// of the crest (head notch 0.25) — a sensitivity, not a proposal. <c>net</c> (G3b, research record
        /// §12): the same spine, branches, culminations and saddles with a converging valley tree instead of
        /// valleys to the nearest shore. Sensitivities of G3b: <c>netrel</c> adds the routing relief to the
        /// surface, <c>netg100</c> uses a 100 m coast gate.
        /// </summary>
        public static G3Settings VariantSettings(string variant, out string id, out float gate)
        {
            var s = G3Settings.Default;
            gate = TerrainGenSettings.Default.UpliftCoastGateMeters;
            s.GateMeters = gate;
            switch (variant.Trim())
            {
                case "deep":
                    s.ValleyDepthPerCrest = 0.50f;
                    s.HeadNotchPerCrest = 0.25f;
                    id = $"G3-skel-g{Mathf.RoundToInt(gate)}-v50";
                    break;
                case "net":
                    s.Network = true;
                    id = $"G3-net-g{Mathf.RoundToInt(gate)}";
                    break;
                case "netg100":
                    // Sensitivity: the same tree with a 100 m coast gate (GEO-4 evidence, not a proposal).
                    s.Network = true;
                    gate = 100f;
                    s.GateMeters = gate;
                    id = $"G3-net-g{Mathf.RoundToInt(gate)}";
                    break;
                case "netrel":
                    // Sensitivity: the routing relief is also added to the surface as hollows and spurs.
                    s.Network = true;
                    s.RouteReliefInSurface = true;
                    id = $"G3-net-g{Mathf.RoundToInt(gate)}-rel";
                    break;
                default:
                    id = $"G3-skel-g{Mathf.RoundToInt(gate)}";
                    break;
            }

            return s;
        }

        public static string Run(int[] seeds, string directory, string[] variants, bool review)
        {
            Directory.CreateDirectory(directory);
            var reviewDir = Path.Combine(directory, "review");
            var c = CultureInfo.InvariantCulture;
            var settings = G3Settings.Default;
            var report = new StringBuilder();
            var summary = new StringBuilder();
            var timings = new StringBuilder("seed surface world_ms system_ms compose_ms analysis_ms metrics_ms review_ms\n");

            report.AppendLine("G3 research candidate - one connected branched mountain system, ridges and valleys from one skeleton");
            report.AppendLine("  research only: no production code, bake, scene or asset reads this; lattices are files outside Assets");
            report.AppendLine("  surfaces: H4A (production baseline) and G3 = H4A - U2 - A + S * smoothstep(d / G) for each gate G");
            foreach (var line in settings.Lines(c))
            {
                report.AppendLine("  " + line);
            }

            report.AppendLine();

            foreach (var seed in seeds)
            {
                var watch = Stopwatch.StartNew();
                var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
                var worldMs = watch.ElapsedMilliseconds;
                var n = world.Width;
                var cell = world.Cell;
                var h4aHash = world.H4AField.Hash();
                TerrainGenGeographyDump.PinnedH4AHashes.TryGetValue(seed, out var pinned);
                report.AppendLine($"seed {seed}  massifs {world.Stage2B.Massifs.Count}  H4A hash {h4aHash}  pinned {pinned ?? "none"}  {(pinned == null ? "unpinned" : pinned == h4aHash ? "PASS" : "FAIL")}");

                // Baseline: production H4A through the unchanged G0 path.
                watch.Restart();
                var baseAnalysis = GeographyAnalysis.Build(world.H4A, n, cell, seed);
                var baseAnalysisMs = watch.ElapsedMilliseconds;
                watch.Restart();
                var baseMetrics = GeographyMetrics.Measure(world, baseAnalysis);
                var baseMetricsMs = watch.ElapsedMilliseconds;
                summary.AppendLine("H4A          " + baseMetrics.SummaryLine(c));
                GeographyImages.WriteStructure(Path.Combine(directory, $"g3_{seed}_H4A_structure.png"), world, baseAnalysis, baseMetrics);
                report.AppendLine("  [H4A] " + baseMetrics.SummaryLine(c));
                var baseLines = GeographyMetrics.DeclaredDivides(world, out var crestCount);
                var baseBoundary = G3Catchments.Boundary(baseAnalysis, out var baseControl);
                var baseCrest = G3Catchments.ShareNear(baseAnalysis, baseBoundary, baseLines.GetRange(0, crestCount));
                var baseSpur = G3Catchments.ShareNear(baseAnalysis, baseBoundary, baseLines.GetRange(crestCount, baseLines.Count - crestCount));
                report.AppendLine($"  [H4A] declared-divide fit (catchment boundaries, G3 diagnostic): massif crests {Percent(baseCrest, c)}, Stage 3 spurs and divides {Percent(baseSpur, c)}; control {Percent(baseControl, c)}");
                summary.AppendLine($"H4A          seed={seed} catchment: crestCatch={Percent(baseCrest, c)} spurCatch={Percent(baseSpur, c)} control={Percent(baseControl, c)}");
                var baseReview = 0L;
                if (review)
                {
                    watch.Restart();
                    GeometryFirstReview.Run(new[] { seed }, reviewDir, null, "H4A");
                    baseReview = watch.ElapsedMilliseconds;
                }

                timings.AppendLine($"{seed} H4A {worldMs} 0 0 {baseAnalysisMs} {baseMetricsMs} {baseReview}");

                foreach (var variant in variants)
                {
                    var variantSettings = VariantSettings(variant, out var id, out var gate);
                    watch.Restart();
                    var system = G3MountainSystem.Build(world, variantSettings);
                    var systemMs = watch.ElapsedMilliseconds;
                    report.AppendLine($"  [{id}] variant {variant.Trim()}: valley depth {variantSettings.ValleyDepthPerCrest} x crest, head notch {variantSettings.HeadNotchPerCrest}, coast gate {gate} m");
                    if (variantSettings.Network)
                    {
                        foreach (var line in variantSettings.NetworkLines(c))
                        {
                            report.AppendLine($"  [{id}] " + line);
                        }
                    }

                    system.AppendStructure(report, c);
                    watch.Restart();
                    var heights = system.Compose(world, gate, out var compose);
                    var composeMs = watch.ElapsedMilliseconds;
                    var field = new WorldHeightField(world.H4AField.OriginX, world.H4AField.OriginZ, n, n, cell, heights);
                    var hash = field.Hash();
                    var latticePath = Path.Combine(directory, $"g3_{seed}_{id}.f32");
                    WriteLattice(latticePath, heights);

                    watch.Restart();
                    var analysis = GeographyAnalysis.Build(heights, n, cell, seed);
                    var analysisMs = watch.ElapsedMilliseconds;
                    watch.Restart();
                    var metrics = GeographyMetrics.Measure(world, analysis);
                    var diagnostics = G3Diagnostics.Measure(system, analysis, heights, world);
                    var metricsMs = watch.ElapsedMilliseconds;

                    report.AppendLine($"  [{id}] lattice hash {hash}  file {Path.GetFileName(latticePath)}");
                    report.AppendLine($"  [{id}] " + metrics.SummaryLine(c));
                    compose.Append(report, c, id);
                    diagnostics.Append(report, c, id);
                    summary.AppendLine($"{id,-12} " + metrics.SummaryLine(c));
                    summary.AppendLine($"{id,-12} " + diagnostics.SummaryLine(c, seed, compose));

                    G3Images.WriteStructure(Path.Combine(directory, $"g3_{seed}_{id}_structure.png"), system, analysis, heights);

                    var reviewMs = 0L;
                    if (review)
                    {
                        watch.Restart();
                        GeometryFirstReview.Run(new[] { seed }, reviewDir, latticePath, id);
                        reviewMs = watch.ElapsedMilliseconds;
                    }

                    timings.AppendLine($"{seed} {id} {worldMs} {systemMs} {composeMs} {analysisMs} {metricsMs} {reviewMs}");
                }

                report.AppendLine();
            }

            report.AppendLine("one line per seed and surface:");
            report.Append(summary);
            File.WriteAllText(Path.Combine(directory, "g3_report.txt"), report.ToString());
            File.WriteAllText(Path.Combine(directory, "g3_timings.txt"), timings.ToString());
            return report.ToString();
        }

        private static string Percent(float v, CultureInfo c) => float.IsNaN(v) ? "n/a" : (v * 100f).ToString("F0", c) + "%";

        private static void WriteLattice(string path, float[] heights)
        {
            var bytes = new byte[heights.Length * 4];
            Buffer.BlockCopy(heights, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(path, bytes);
        }

        private static int[] ParseInts(string raw, int[] fallback)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return fallback;
            }

            var values = new List<int>();
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                {
                    values.Add(v);
                }
            }

            return values.Count > 0 ? values.ToArray() : fallback;
        }
    }

    /// <summary>
    /// G3 parameters. Values named after an existing setting reuse that setting; every other value is an
    /// <b>authoring default</b> of this experiment (world units), not measured and not approved.
    /// </summary>
    public sealed class G3Settings
    {
        // Skeleton (authoring defaults).
        public int CoarseStride = 4;                       // 10.9 m Dijkstra grid
        public float CentralityWeight = 6f;                // cost = 1 + w (1 - r / rmax)^2
        public float BranchMinProtrusionMeters;            // = TerrainGenSettings.UpliftCoastGateMeters (200)
        public int MaxBranches;                            // = TerrainGenSettings.MaxBranchesPerRange (2)
        public int SmoothHalfWindow = 6;                   // coarse points either side (try1: 3)
        public float ResampleMeters;                       // = MacroStructureSettings.SampleSpacingMeters (20)

        // Spine crest.
        public float RadiusSmoothMeters = 100f;
        public float CulminationSpacingMeters;             // = MassifSettings.SecondCulminationLengthMeters / 2 (550)
        public int MaxCulminations = 3;
        public float CulminationMinRadiusFraction = 0.5f;
        public float CrestMinMeters, CrestMaxMeters;       // = TerrainGenSettings.CrestHeightMin/Max (200, 330)
        public float SecondaryMin, SecondaryMax;           // = MassifSettings.SecondaryHeightMin/Max (0.62, 0.82)
        public float SaddleMin, SaddleMax;                 // = MassifSettings.SaddleFractionMin/Max (0.55, 0.72)
        public float EndCrestFraction = 0.2f;
        public float CrestPerRadius = 0.7f;                // crest <= 0.7 x distance to coast (35 deg mean flank)
        public float HalfWidthPerRadius = 0.6f;          // try1: 0.75
        public float HalfWidthMinMeters = 100f;
        public float HalfWidthMaxMeters = 420f;

        // Branches.
        public float BranchCrestMin, BranchCrestMax;       // = TerrainGenSettings.BranchCrestScaleMin/Max (0.45, 0.70)

        // Spurs and valleys.
        public float SpurSpacingPerHalfWidth;              // = RidgeSettings.SpurSpacingPerHalfWidth (0.85)
        public float SpurStationJitter;                    // = RidgeSettings.SpurStationJitter (0.25)
        public float SpurOffsetPerHalfWidth = 0.2f;
        public float ValleyCarveHalfWidthPerGap = 0.5f;    // valleys of one side meet on the spur line between them
        public float HeadNotchPerCrest;                    // = RidgeSettings.ValleyDepthPerCrest / 2 (0.15)
        public float ValleyDepthPerCrest;                  // = RidgeSettings.ValleyDepthPerCrest (0.30)
        public float IncisionMeters;                       // = RidgeSettings.CorridorIncisionMeters (7.2), below the floor surface
        public float SpurToeMeters = 60f;                  // stop descending at this coast distance
        public float SpurMinLengthMeters = 120f;
        public float ParentEndMargin = 0.12f;
        public float SaddleGapPerSpacing = 0.5f;
        public float DescentStepMeters = 10f;
        public float ValleyHalfWidthPerGap = 0.25f;
        public float ValleyMaxGapPerSpacing = 1.6f;

        // G3b converging valley tree (research record §12). Off for G3-skel. Authoring defaults unless named.
        public bool Network;
        public float GateMeters;                           // = TerrainGenSettings.UpliftCoastGateMeters (200): G3b routes and floors on the gated surface Compose writes
        public float CorridorColMeters;                    // = IslandReliefSettings.PlateUpliftMeters (18): corridor floor above the floor surface at the lowest saddle
        public float FlatHalfWidthMaxMeters;               // = HydrologySettings.ValleyHalfWidthMaxMeters (85)
        public float FlatHalfWidthPerHalfWidth = 0.4f;     // trunk flat floor half-width <= 0.4 x the ridge half-width where it starts
        public float TrunkWallPerHalfWidth = 0.6f;         // trunk wall reach beyond the flat floor
        public float FlatCrossFallDegrees = 1.5f;          // flat floors fall toward the axis, so water gathers there and the floor stays below 5 deg
        public float TrunkLowFraction = 0.45f;             // trunk / corridor floor reaches the trough (-IncisionMeters) at this fraction of its axis
        public float WedgeStartPerHalfWidth = 0.35f;       // trunk head offset from the branch root along the wedge bisector
        public float TrunkJoinMeters = 25f;                // within this distance of a trunk axis (+ half its flat floor) a path has joined the trunk
        public float JoinMinTravelMeters = 30f;
        public int DescentSmoothHalfWindow = 3;            // moving average over the 10 m descent steps before resampling
        public float RouteChannelAreaSqm;                  // = HydrologySettings.ChannelInitiationAreaSqm / 2 (15 000): carved links start before G0 counts a channel
        public float[] RouteNoiseWavelengthsMeters = { 250f, 600f };
        public float RouteNoiseSlopeFraction = 0.08f;      // routing perturbation amplitude = 0.08 x local slope x wavelength, per octave: its gradient stays below ~0.5 of the slope, so flow turns by <= ~26 deg and never reverses
        public float RouteDepthPerSqrtArea = 0.08f;        // link depth below the routing height: 15 000 m2 -> 9.8 m, 100 000 m2 -> 25 m
        public float RouteHeadTaperMeters = 60f;
        public float RouteWidthPerSqrtArea = 0.5f;         // link carve half-width: 15 000 m2 -> 61 m, 100 000 m2 -> 158 m
        public float RouteWidthMinMeters = 30f;
        public float RouteWidthMaxMeters = 160f;
        public float RouteFlatAreaSqm = 150000f;           // links draining more get a flat floor (lower reaches)
        public float RouteFlatPerHalfWidth = 0.4f;
        public int RouteSmoothHalfWindow = 2;              // moving average over the 10.9 m link cells, end points fixed
        public int RouteSlopeStencil = 3;                  // slope for the relief amplitude read over +-3 coarse cells (+-33 m)
        public bool RouteReliefInSurface;                  // false: the routing relief only shapes the tree; true (variant netrel): it is added to the surface as hollows and spurs
        public float RouteEnvelopeFadeMeters;              // 0: the perturbation acts on all land; > 0 fades it in over this much envelope (tried at 18 m, research record §12)

        // Closed forms, not ProfileCurve: ProfileCurve has zero slope at every control point, so an
        // interior control draws a bench along the whole ridge (seen in try3, research record §11).
        public static float Smooth01(float u)
        {
            u = Mathf.Clamp01(u);
            return u * u * (3f - 2f * u);
        }

        /// <summary>Ridge cross-section: rounded crest, steepest mid-flank, concave toe.</summary>
        public static float Flank(float u) => 1f - Smooth01(u);

        /// <summary>Branch crest along its length, root 1 to tip 0.1.</summary>
        public static float BranchCrest(float t) => 0.1f + 0.9f * (1f - Smooth01(t));

        /// <summary>Valley carve weight across its axis: 1 on the floor line, 0 on the spur line. V-shaped at the floor (slope -2), so D8 water gathers on the axis instead of running in parallel down a flat-bottomed groove (try7, research record §11).</summary>
        public static float CarveAcross(float v)
        {
            v = Mathf.Clamp01(v);
            return (1f - v) * (1f - v);
        }

        public static G3Settings Default
        {
            get
            {
                var t = TerrainGenSettings.Default;
                var m = MassifSettings.Default;
                var r = RidgeSettings.Default;
                return new G3Settings
                {
                    BranchMinProtrusionMeters = t.UpliftCoastGateMeters,
                    MaxBranches = t.MaxBranchesPerRange,
                    ResampleMeters = MacroStructureSettings.Default.SampleSpacingMeters,
                    CulminationSpacingMeters = m.SecondCulminationLengthMeters * 0.5f,
                    CrestMinMeters = t.CrestHeightMinMeters,
                    CrestMaxMeters = t.CrestHeightMaxMeters,
                    SecondaryMin = m.SecondaryHeightMin,
                    SecondaryMax = m.SecondaryHeightMax,
                    SaddleMin = m.SaddleFractionMin,
                    SaddleMax = m.SaddleFractionMax,
                    BranchCrestMin = t.BranchCrestScaleMin,
                    BranchCrestMax = t.BranchCrestScaleMax,
                    SpurSpacingPerHalfWidth = r.SpurSpacingPerHalfWidth,
                    SpurStationJitter = r.SpurStationJitter,
                    HeadNotchPerCrest = r.ValleyDepthPerCrest * 0.5f,
                    ValleyDepthPerCrest = r.ValleyDepthPerCrest,
                    IncisionMeters = r.CorridorIncisionMeters,
                    CorridorColMeters = IslandReliefSettings.Default.PlateUpliftMeters,
                    FlatHalfWidthMaxMeters = GameDev.World.Hydrology.HydrologySettings.Default.ValleyHalfWidthMaxMeters,
                    RouteChannelAreaSqm = GameDev.World.Hydrology.HydrologySettings.Default.ChannelInitiationAreaSqm * 0.5f,
                };
            }
        }

        public IEnumerable<string> NetworkLines(CultureInfo c)
        {
            yield return $"G3b valley tree: trunks traced by steepest descent on the design height (floor surface H4A - U2 - A + envelope E), one per wedge between a branch and its parent (head {WedgeStartPerHalfWidth} x the smaller root half-width along the wedge bisector), and a corridor across the lowest saddle as two halves from the col; a trunk path within {TrunkJoinMeters} m (+ half the flat floor) of an earlier trunk joins it";
            yield return $"trunk floor lerp(E, -{IncisionMeters} m, smoothstep(t / {TrunkLowFraction})), corridor lerp(min(E, {CorridorColMeters} m = PlateUpliftMeters), same); flat floor half-width min({FlatHalfWidthMaxMeters} m, {FlatHalfWidthPerHalfWidth} x root half-width) x smoothstep(t / {TrunkLowFraction}) (corridor: full from the col), cross-fall {FlatCrossFallDegrees} deg to the axis, walls 1 - smoothstep over {TrunkWallPerHalfWidth} x root half-width";
            yield return $"side valleys = a routed drainage tree on the {CoarseStride}-cell grid: routing height = design height with the trunks carved, + perturbation {RouteNoiseSlopeFraction} x |grad| x lambda x n(p) over lambda = {string.Join(" / ", RouteNoiseWavelengthsMeters)} m (stream research.g3b.route; slope = min of the slope over +-{RouteSlopeStencil} cells and over +-1 cell; {(RouteReliefInSurface ? "also added to the surface as hollows and spurs" : "routing only, in no height")}{(RouteEnvelopeFadeMeters > 0f ? $", faded in over {RouteEnvelopeFadeMeters} m of envelope" : "")}); priority flood to the ocean, the coastal fringe (<= {SpurToeMeters} m from the shore) and the trunk floors, D8, accumulation; links where >= {RouteChannelAreaSqm} m2 drains, split at confluences";
            yield return $"links: depth {RouteDepthPerSqrtArea} x sqrt(area) below the routing height (tapered over {RouteHeadTaperMeters} m at heads), V carve half-width {RouteWidthPerSqrtArea} x sqrt(area) in [{RouteWidthMinMeters}, {RouteWidthMaxMeters}] m; above {RouteFlatAreaSqm} m2 a flat floor up to {RouteFlatPerHalfWidth} x half-width; smoothed +-{RouteSmoothHalfWindow} cells";
            yield return $"coast gate {GateMeters} m: trunks are traced, the tree is routed and every floor is made monotone on the gated surface H4A - U2 - A + S x smoothstep(d / gate) that Compose writes";
            yield return $"mouths: a trunk or link that does not join another valley continues straight down the coast distance to the shore, as G3's troughs do";
            yield return $"floors: every axis is a running minimum in absolute height from its head, with each tributary's mouth floor imposed on the node it joins (tributaries first), then bounded to >= -{IncisionMeters} m below the floor surface";
        }

        public IEnumerable<string> Lines(CultureInfo c)
        {
            yield return $"skeleton: Dijkstra on a {CoarseStride}-cell grid, cost 1 + {CentralityWeight} (1 - r/rmax)^2; spine = weighted diameter; branches = max protrusion (path length - r at attach) >= {BranchMinProtrusionMeters} m, at most {MaxBranches}; smooth +-{SmoothHalfWindow}, resample {ResampleMeters} m";
            yield return $"spine crest: culminations greedily at the widest point, then branch roots, then the widest remaining points (r smoothed {RadiusSmoothMeters} m, >= {CulminationMinRadiusFraction} rmax), >= {CulminationSpacingMeters} m apart; saddle = narrowest point strictly between two culminations (quarter-gap margin), at most {MaxCulminations}; main {CrestMinMeters}-{CrestMaxMeters} m, others x{SecondaryMin}-{SecondaryMax}, saddles x{SaddleMin}-{SaddleMax} of the lower neighbour; ends x{EndCrestFraction}; cap crest <= {CrestPerRadius} r; half-width {HalfWidthPerRadius} r in [{HalfWidthMinMeters}, {HalfWidthMaxMeters}] m";
            yield return $"branches: root crest x{BranchCrestMin}-{BranchCrestMax} of the parent; spurs (lines only, no height of their own): spacing {SpurSpacingPerHalfWidth} x half-width, jitter {SpurStationJitter}, offset {SpurOffsetPerHalfWidth} hw, steepest descent on Stage 1 coast distance to {SpurToeMeters} m, min {SpurMinLengthMeters} m; no station within {SaddleGapPerSpacing} spacing of a saddle or a branch root";
            yield return $"valleys: between consecutive spurs of one side (gap <= {ValleyMaxGapPerSpacing} spacing), head on the parent crest, steepest descent to {SpurToeMeters} m, half-width {ValleyHalfWidthPerGap} gap";
            yield return $"profiles (closed forms): flank 1 - smoothstep(u); branch crest 0.1 + 0.9 (1 - smoothstep(t)); envelope E = max(spine, branches); valleys carved into E to a floor-referenced profile: depth(t) = crest(head) [{HeadNotchPerCrest} (1 - t) + ({ValleyDepthPerCrest} - {HeadNotchPerCrest}) 6.75 t (1 - t)^2], floor = running minimum from the head of E(axis) - depth, not below -{IncisionMeters} m (valleys continue to the coast as troughs at most the Stage 3 corridor incision below the floor surface), carve = max(0, E - floor) x (1 - d / ({ValleyCarveHalfWidthPerGap} gap))^2, deepest wins; spurs are the uncarved lines between valleys; S = E - carve; streams research.g3.crest / .branch / .spur";
        }
    }

    public sealed class G3Ridge
    {
        public int Kind;           // 0 spine, 1 branch, 2 spur
        public int Parent;         // feature index or -1
        public int Side;           // spurs: -1 / +1
        public float ParentArc;    // where it leaves its parent, metres
        public Vector2[] Points;
        public float[] Cumulative;
        public float[] Crest;
        public float[] Half;
        public float Length => Cumulative[Cumulative.Length - 1];
    }

    public sealed class G3Valley
    {
        public int Parent;
        public int Side;
        public Vector2[] Points;
        public float[] Cumulative;
        public float HalfWidth;       // floor zone, for D-e
        public float CarveHalfWidth;  // carve reach, to the spur lines
        public float[] Floor;         // floor height of S along the axis
        public float ParentArc;       // where the head sits on the parent crest

        // G3b only.
        public const int SideClass = 0, TrunkClass = 2, CorridorClass = 3, RoutedClass = 4;
        public int Class;             // SideClass for every G3-skel valley
        public int Group = -1;        // corridor halves share one group
        public int JoinsInto = -1;    // receiving valley, or -1 for the coast
        public int JoinNode;          // node of the receiver the mouth joins
        public bool IsHead;           // routed link that starts at a channel head, not at a confluence
        public float ProfileLength;
        public float[] AreaNodes;     // drained area per node (routed links)
        public float[] FlatHalfNodes; // flat-floor half-width per node
        public float[] CarveHalfNodes;
        public float[] FloorAbs;      // absolute floor height along the axis
        public bool IsTrunk => Class == TrunkClass || Class == CorridorClass;
    }

    public struct G3Mark
    {
        public Vector2 Position;
        public float Arc;
        public float Crest;
        public bool IsSaddle;
    }

    /// <summary>The mountain-system skeleton and its uplift field <c>S</c> (ungated).</summary>
    public sealed class G3MountainSystem
    {
        public int Seed;
        public int N;
        public float Cell;
        public float OriginX, OriginZ;
        public float[] Sd;
        public bool[] Mainland;
        public float RadiusMax;
        public readonly List<G3Ridge> Ridges = new List<G3Ridge>();
        public readonly List<G3Valley> Valleys = new List<G3Valley>();
        public readonly List<G3Mark> Marks = new List<G3Mark>();
        public float[] Uplift;
        public float CapBindingShare;
        public int SpurStations, SpursBuilt, SpursSkippedSide, SpursSkippedLength, SpursSkippedGap;
        public float BranchProtrusionBest;
        private G3Settings settings;

        public static G3MountainSystem Build(IslandReliefWorld w, G3Settings s)
        {
            var sys = new G3MountainSystem
            {
                Seed = w.Seed,
                N = w.Width,
                Cell = w.Cell,
                OriginX = w.H4AField.OriginX,
                OriginZ = w.H4AField.OriginZ,
                Sd = w.Stage1.SignedDistance,
                settings = s,
            };
            sys.BuildMainland();
            sys.BuildSkeleton();
            sys.BuildSpineCrest();
            sys.BuildBranchCrests();
            if (s.Network)
            {
                // G3b: the envelope comes first: the trunks are traced on it, and the side network is
                // routed on it with the trunks carved.
                sys.BuildFloorSurface(w);
                sys.Envelope = sys.BuildEnvelope();
                sys.BuildTrunks();
                sys.BuildRouting();
                sys.BuildRoutedNetwork();
                sys.ApplyRelief();
                sys.EvaluateNetwork();
            }
            else
            {
                sys.BuildSpursAndValleys();
                sys.EvaluateUplift();
            }

            return sys;
        }

        // ---------- lattice helpers ----------

        private Vector2 World(int x, int z) => new Vector2(OriginX + x * Cell, OriginZ + z * Cell);

        public float SdAt(Vector2 p) => Bilinear(Sd, p);

        public float Bilinear(float[] a, Vector2 p)
        {
            var fx = Mathf.Clamp((p.x - OriginX) / Cell, 0f, N - 1.001f);
            var fz = Mathf.Clamp((p.y - OriginZ) / Cell, 0f, N - 1.001f);
            var x0 = (int)fx;
            var z0 = (int)fz;
            var u = fx - x0;
            var v = fz - z0;
            var i = z0 * N + x0;
            var a0 = Mathf.Lerp(a[i], a[i + 1], u);
            var a1 = Mathf.Lerp(a[i + N], a[i + N + 1], u);
            return Mathf.Lerp(a0, a1, v);
        }

        private Vector2 SdGradient(Vector2 p)
        {
            var e = Cell;
            var gx = SdAt(p + new Vector2(e, 0f)) - SdAt(p - new Vector2(e, 0f));
            var gz = SdAt(p + new Vector2(0f, e)) - SdAt(p - new Vector2(0f, e));
            return new Vector2(gx, gz) / (2f * e);
        }

        private bool OnMainland(Vector2 p)
        {
            var x = Mathf.RoundToInt((p.x - OriginX) / Cell);
            var z = Mathf.RoundToInt((p.y - OriginZ) / Cell);
            return x >= 0 && z >= 0 && x < N && z < N && Mainland[z * N + x];
        }

        private void BuildMainland()
        {
            var land = new bool[N * N];
            for (var i = 0; i < land.Length; i++)
            {
                land[i] = Sd[i] > 0f;
            }

            var labels = GeographyGrid.Components8(land, N, out var count);
            var sizes = new int[Mathf.Max(1, count)];
            foreach (var l in labels)
            {
                if (l >= 0)
                {
                    sizes[l]++;
                }
            }

            var best = 0;
            for (var k = 1; k < count; k++)
            {
                if (sizes[k] > sizes[best])
                {
                    best = k;
                }
            }

            Mainland = new bool[N * N];
            for (var i = 0; i < labels.Length; i++)
            {
                Mainland[i] = labels[i] == best && count > 0;
                if (Mainland[i])
                {
                    RadiusMax = Mathf.Max(RadiusMax, Sd[i]);
                }
            }
        }

        // ---------- skeleton ----------

        private int nc;
        private bool[] coarseLand;
        private float[] coarseR;

        private void BuildSkeleton()
        {
            var k = settings.CoarseStride;
            nc = (N - 1) / k + 1;
            coarseLand = new bool[nc * nc];
            coarseR = new float[nc * nc];
            var root = -1;
            for (var z = 0; z < nc; z++)
            {
                for (var x = 0; x < nc; x++)
                {
                    var i = z * nc + x;
                    var fi = (z * k) * N + x * k;
                    coarseLand[i] = Mainland[fi];
                    coarseR[i] = Mainland[fi] ? Sd[fi] : 0f;
                    if (coarseLand[i] && (root < 0 || coarseR[i] > coarseR[root]))
                    {
                        root = i;
                    }
                }
            }

            if (root < 0)
            {
                return;
            }

            var owner = new int[nc * nc];
            for (var i = 0; i < owner.Length; i++)
            {
                owner[i] = -1;
            }

            Dijkstra(new List<int> { root }, out var cost, out _, out _, out _);
            var a = ArgMax(cost);
            Dijkstra(new List<int> { a }, out cost, out _, out var prev, out _);
            var b = ArgMax(cost);
            var spineCells = Trace(prev, b);
            spineCells.Reverse(); // a -> b
            AddRidge(0, -1, spineCells, owner);

            for (var branch = 0; branch < settings.MaxBranches; branch++)
            {
                var sources = new List<int>();
                for (var i = 0; i < owner.Length; i++)
                {
                    if (owner[i] >= 0)
                    {
                        sources.Add(i);
                    }
                }

                Dijkstra(sources, out cost, out var length, out prev, out var source);
                var bestCell = -1;
                var bestProtrusion = float.NegativeInfinity;
                for (var i = 0; i < cost.Length; i++)
                {
                    if (!coarseLand[i] || double.IsInfinity(cost[i]) || source[i] < 0)
                    {
                        continue;
                    }

                    var protrusion = length[i] - coarseR[source[i]];
                    if (protrusion > bestProtrusion)
                    {
                        bestProtrusion = protrusion;
                        bestCell = i;
                    }
                }

                if (branch == 0)
                {
                    BranchProtrusionBest = bestProtrusion;
                }

                if (bestCell < 0 || bestProtrusion < settings.BranchMinProtrusionMeters)
                {
                    break;
                }

                var path = Trace(prev, bestCell); // tip ... source
                path.Reverse();                   // source -> tip
                AddRidge(1, owner[path[0]], path, owner);
            }
        }

        private void AddRidge(int kind, int parent, List<int> cells, int[] owner)
        {
            var index = Ridges.Count;
            var k = settings.CoarseStride;
            var raw = new List<Vector2>(cells.Count);
            foreach (var c in cells)
            {
                raw.Add(World((c % nc) * k, (c / nc) * k));
                owner[c] = index;
            }

            var smooth = new List<Vector2>(raw.Count);
            var h = settings.SmoothHalfWindow;
            for (var i = 0; i < raw.Count; i++)
            {
                if (i == 0 || i == raw.Count - 1)
                {
                    smooth.Add(raw[i]);
                    continue;
                }

                var sum = Vector2.zero;
                var count = 0;
                for (var j = Mathf.Max(0, i - h); j <= Mathf.Min(raw.Count - 1, i + h); j++)
                {
                    sum += raw[j];
                    count++;
                }

                smooth.Add(sum / count);
            }

            var ridge = new G3Ridge { Kind = kind, Parent = parent };
            if (parent >= 0)
            {
                // Pin the root onto the smoothed parent so the branch leaves the crest it belongs to.
                var p = Ridges[parent];
                ClosestOn(p.Points, p.Cumulative, smooth[0], out var arc, out var at, out _);
                smooth[0] = at;
                ridge.ParentArc = arc;
            }

            var resampled = Geo2.ResamplePolyline(smooth, settings.ResampleMeters);
            ridge.Points = resampled.ToArray();
            ridge.Cumulative = Cumulative(ridge.Points);
            ridge.Crest = new float[ridge.Points.Length];
            ridge.Half = new float[ridge.Points.Length];
            Ridges.Add(ridge);
        }

        private void Dijkstra(List<int> sources, out double[] cost, out float[] length, out int[] prev, out int[] source)
        {
            var count = nc * nc;
            cost = new double[count];
            length = new float[count];
            prev = new int[count];
            source = new int[count];
            for (var i = 0; i < count; i++)
            {
                cost[i] = double.PositiveInfinity;
                prev[i] = -1;
                source[i] = -1;
            }

            var heap = new G3Heap(count);
            foreach (var s in sources)
            {
                cost[s] = 0;
                source[s] = s;
                heap.Push(0, s);
            }

            var step = settings.CoarseStride * Cell;
            var rmax = Mathf.Max(1f, RadiusMax);
            var done = new bool[count];
            while (heap.Count > 0)
            {
                heap.Pop(out var key, out var u);
                if (done[u] || key > cost[u])
                {
                    continue;
                }

                done[u] = true;
                var ux = u % nc;
                var uz = u / nc;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0)
                        {
                            continue;
                        }

                        var vx = ux + dx;
                        var vz = uz + dz;
                        if (vx < 0 || vz < 0 || vx >= nc || vz >= nc)
                        {
                            continue;
                        }

                        var v = vz * nc + vx;
                        if (!coarseLand[v] || done[v])
                        {
                            continue;
                        }

                        var metres = step * ((dx != 0 && dz != 0) ? 1.41421356f : 1f);
                        var wu = 1f + settings.CentralityWeight * Sq(1f - Mathf.Min(coarseR[u], rmax) / rmax);
                        var wv = 1f + settings.CentralityWeight * Sq(1f - Mathf.Min(coarseR[v], rmax) / rmax);
                        var next = cost[u] + metres * 0.5 * (wu + wv);
                        if (next < cost[v])
                        {
                            cost[v] = next;
                            length[v] = length[u] + metres;
                            prev[v] = u;
                            source[v] = source[u];
                            heap.Push(next, v);
                        }
                    }
                }
            }
        }

        private static float Sq(float v) => v * v;

        private int ArgMax(double[] cost)
        {
            var best = -1;
            for (var i = 0; i < cost.Length; i++)
            {
                if (!double.IsInfinity(cost[i]) && (best < 0 || cost[i] > cost[best]))
                {
                    best = i;
                }
            }

            return best;
        }

        private static List<int> Trace(int[] prev, int end)
        {
            var path = new List<int>();
            for (var c = end; c >= 0; c = prev[c])
            {
                path.Add(c);
            }

            return path;
        }

        // ---------- crests ----------

        private void BuildSpineCrest()
        {
            if (Ridges.Count == 0)
            {
                return;
            }

            var spine = Ridges[0];
            var count = spine.Points.Length;
            var r = SmoothedRadius(spine);
            var length = spine.Length;
            var rPeak = 0f;
            foreach (var v in r)
            {
                rPeak = Mathf.Max(rPeak, v);
            }

            // Culminations, greedily: the widest point first, then branch roots on the spine, then the
            // widest remaining points; each at least CulminationSpacing from the others and on land at
            // least CulminationMinRadiusFraction of the widest.
            var byRadius = new List<int>();
            for (var i = 0; i < count; i++)
            {
                if (r[i] >= settings.CulminationMinRadiusFraction * rPeak)
                {
                    byRadius.Add(i);
                }
            }

            byRadius.Sort((p, q) => r[q] != r[p] ? r[q].CompareTo(r[p]) : p.CompareTo(q));
            var ordered = new List<int>();
            if (byRadius.Count > 0)
            {
                ordered.Add(byRadius[0]);
            }

            for (var b = 1; b < Ridges.Count; b++)
            {
                if (Ridges[b].Kind == 1 && Ridges[b].Parent == 0)
                {
                    ClosestOn(spine.Points, spine.Cumulative, Ridges[b].Points[0], out _, out _, out var segment);
                    ordered.Add(Mathf.Clamp(segment, 0, count - 1));
                }
            }

            ordered.AddRange(byRadius);
            var culminations = new List<int>();
            foreach (var cand in ordered)
            {
                var ok = true;
                foreach (var kept in culminations)
                {
                    ok &= Mathf.Abs(spine.Cumulative[cand] - spine.Cumulative[kept]) >= settings.CulminationSpacingMeters;
                }

                if (ok && culminations.Count < settings.MaxCulminations)
                {
                    culminations.Add(cand);
                }
            }

            var mainIndex = culminations.Count > 0 ? culminations[0] : count / 2;
            culminations.Sort();

            var rng = new GameDev.World.Seed.WorldSeed(Seed).Fork("research.g3.crest");
            var mainCrest = Mathf.Lerp(settings.CrestMinMeters, settings.CrestMaxMeters, (float)rng.NextDouble());
            var heights = new List<float>();
            foreach (var cIndex in culminations)
            {
                var u = (float)rng.NextDouble();
                heights.Add(cIndex == mainIndex ? mainCrest : mainCrest * Mathf.Lerp(settings.SecondaryMin, settings.SecondaryMax, u));
            }

            if (culminations.Count == 0)
            {
                culminations.Add(mainIndex);
                heights.Add(mainCrest);
            }

            var ts = new List<float>();
            var vs = new List<float>();
            void Add(float t, float v)
            {
                if (ts.Count == 0 || t > ts[ts.Count - 1] + 1e-4f)
                {
                    ts.Add(t);
                    vs.Add(v);
                }
            }

            Add(0f, heights[0] * settings.EndCrestFraction);
            for (var k = 0; k < culminations.Count; k++)
            {
                if (k > 0)
                {
                    // The saddle is the narrowest land between the two culminations.
                    // Strictly between the two, keeping a quarter of the gap clear of either summit.
                    var margin = Mathf.Max(1, (culminations[k] - culminations[k - 1]) / 4);
                    var lo = culminations[k - 1] + margin;
                    var hi = culminations[k] - margin;
                    var argmin = lo;
                    for (var i = lo; i <= hi; i++)
                    {
                        if (r[i] < r[argmin])
                        {
                            argmin = i;
                        }
                    }

                    var u = (float)rng.NextDouble();
                    var saddle = Mathf.Min(heights[k - 1], heights[k]) * Mathf.Lerp(settings.SaddleMin, settings.SaddleMax, u);
                    Add(spine.Cumulative[argmin] / length, saddle);
                    Marks.Add(new G3Mark { Position = spine.Points[argmin], Arc = spine.Cumulative[argmin], Crest = saddle, IsSaddle = true });
                }

                var ci = culminations[k];
                Add(spine.Cumulative[ci] / length, heights[k]);
                Marks.Add(new G3Mark { Position = spine.Points[ci], Arc = spine.Cumulative[ci], Crest = heights[k], IsSaddle = false });
            }

            Add(1f, heights[heights.Count - 1] * settings.EndCrestFraction);
            var profile = new ProfileCurve(ts.ToArray(), vs.ToArray());
            var capped = 0;
            for (var i = 0; i < count; i++)
            {
                var want = profile.Evaluate(spine.Cumulative[i] / length);
                var cap = settings.CrestPerRadius * Mathf.Max(0f, SdAt(spine.Points[i]));
                capped += want > cap ? 1 : 0;
                spine.Crest[i] = Mathf.Min(want, cap);
                spine.Half[i] = HalfWidthAt(r[i]);
            }

            CapBindingShare = capped / (float)Mathf.Max(1, count);
        }

        private float[] SmoothedRadius(G3Ridge ridge)
        {
            var count = ridge.Points.Length;
            var raw = new float[count];
            for (var i = 0; i < count; i++)
            {
                raw[i] = Mathf.Max(0f, SdAt(ridge.Points[i]));
            }

            var half = Mathf.Max(1, Mathf.RoundToInt(settings.RadiusSmoothMeters / settings.ResampleMeters));
            var r = new float[count];
            for (var i = 0; i < count; i++)
            {
                var sum = 0f;
                var n = 0;
                for (var j = Mathf.Max(0, i - half); j <= Mathf.Min(count - 1, i + half); j++)
                {
                    sum += raw[j];
                    n++;
                }

                r[i] = sum / n;
            }

            return r;
        }

        private float HalfWidthAt(float radius) =>
            Mathf.Clamp(settings.HalfWidthPerRadius * radius, settings.HalfWidthMinMeters, settings.HalfWidthMaxMeters);

        private void BuildBranchCrests()
        {
            for (var b = 1; b < Ridges.Count; b++)
            {
                var ridge = Ridges[b];
                var parent = Ridges[ridge.Parent];
                var rootCrest = CrestAlong(parent, ridge.ParentArc);
                var rng = new GameDev.World.Seed.WorldSeed(Seed).Fork("research.g3.branch", b);
                var scale = Mathf.Lerp(settings.BranchCrestMin, settings.BranchCrestMax, (float)rng.NextDouble());
                var r = SmoothedRadius(ridge);
                for (var i = 0; i < ridge.Points.Length; i++)
                {
                    var t = ridge.Cumulative[i] / Mathf.Max(1f, ridge.Length);
                    var want = rootCrest * scale * G3Settings.BranchCrest(t);
                    var cap = settings.CrestPerRadius * Mathf.Max(0f, SdAt(ridge.Points[i]));
                    ridge.Crest[i] = Mathf.Min(want, cap);
                    ridge.Half[i] = HalfWidthAt(r[i]);
                }
            }
        }

        private static float CrestAlong(G3Ridge ridge, float arc) => Interpolate(ridge.Cumulative, ridge.Crest, arc);

        private static float HalfAlong(G3Ridge ridge, float arc) => Interpolate(ridge.Cumulative, ridge.Half, arc);

        private static float Interpolate(float[] cum, float[] values, float arc)
        {
            if (arc <= cum[0])
            {
                return values[0];
            }

            for (var i = 1; i < cum.Length; i++)
            {
                if (arc <= cum[i])
                {
                    var t = (arc - cum[i - 1]) / Mathf.Max(1e-6f, cum[i] - cum[i - 1]);
                    return Mathf.Lerp(values[i - 1], values[i], t);
                }
            }

            return values[values.Length - 1];
        }

        private static void PointAndNormal(G3Ridge ridge, float arc, out Vector2 point, out Vector2 normal)
        {
            var pts = ridge.Points;
            var cum = ridge.Cumulative;
            var i = 1;
            while (i < cum.Length - 1 && cum[i] < arc)
            {
                i++;
            }

            var a = pts[i - 1];
            var b = pts[i];
            var t = Mathf.Clamp01((arc - cum[i - 1]) / Mathf.Max(1e-6f, cum[i] - cum[i - 1]));
            point = Vector2.Lerp(a, b, t);
            var tangent = (b - a).normalized;
            normal = new Vector2(-tangent.y, tangent.x); // left of the direction of travel
        }

        // ---------- spurs and valleys ----------

        private void BuildSpursAndValleys()
        {
            var parents = Ridges.Count;
            for (var p = 0; p < parents; p++)
            {
                var parent = Ridges[p];
                if (parent.Length < 2f * settings.SpurMinLengthMeters)
                {
                    continue;
                }

                for (var sideIndex = 0; sideIndex < 2; sideIndex++)
                {
                    var side = sideIndex == 0 ? -1 : 1;
                    var stations = new List<(float arc, int spur)>();
                    var start = settings.ParentEndMargin * parent.Length;
                    var end = (1f - settings.ParentEndMargin) * parent.Length;
                    var arc = start + (side > 0 ? 0.5f * Spacing(parent, start) : 0f);
                    var station = 0;
                    while (arc < end)
                    {
                        var spacing = Spacing(parent, arc);
                        var rng = new GameDev.World.Seed.WorldSeed(Seed).Fork("research.g3.spur", p * 2 + sideIndex, station);
                        var jittered = arc + ((float)rng.NextDouble() - 0.5f) * 2f * settings.SpurStationJitter * spacing;
                        SpurStations++;
                        var spur = TryBuildSpur(p, side, jittered, spacing);
                        if (spur >= 0)
                        {
                            stations.Add((jittered, spur));
                        }

                        arc += spacing;
                        station++;
                    }

                    for (var k = 0; k + 1 < stations.Count; k++)
                    {
                        var gap = stations[k + 1].arc - stations[k].arc;
                        var mid = 0.5f * (stations[k].arc + stations[k + 1].arc);
                        if (gap > settings.ValleyMaxGapPerSpacing * Spacing(parent, mid))
                        {
                            continue;
                        }

                        PointAndNormal(parent, mid, out var head, out var normal);
                        var start0 = head + side * normal * (0.05f * HalfAlong(parent, mid));
                        var path = Descend(start0);
                        if (path.Count >= 2)
                        {
                            var axis = Geo2.ResamplePolyline(path, settings.ResampleMeters).ToArray();
                            Valleys.Add(new G3Valley
                            {
                                Parent = p,
                                Side = side,
                                Points = axis,
                                Cumulative = Cumulative(axis),
                                HalfWidth = settings.ValleyHalfWidthPerGap * gap,
                                CarveHalfWidth = settings.ValleyCarveHalfWidthPerGap * gap,
                                ParentArc = mid,
                            });
                        }
                    }
                }
            }
        }

        private float Spacing(G3Ridge parent, float arc) => settings.SpurSpacingPerHalfWidth * HalfAlong(parent, arc);

        private int TryBuildSpur(int parentIndex, int side, float arc, float spacing)
        {
            var parent = Ridges[parentIndex];
            if (parentIndex == 0)
            {
                foreach (var mark in Marks)
                {
                    if (mark.IsSaddle && Mathf.Abs(mark.Arc - arc) < settings.SaddleGapPerSpacing * spacing)
                    {
                        SpursSkippedGap++;
                        return -1;
                    }
                }
            }

            for (var b = 0; b < Ridges.Count; b++)
            {
                if (Ridges[b].Kind == 1 && Ridges[b].Parent == parentIndex
                    && Mathf.Abs(Ridges[b].ParentArc - arc) < settings.SaddleGapPerSpacing * spacing)
                {
                    SpursSkippedGap++;
                    return -1;
                }
            }

            PointAndNormal(parent, arc, out var point, out var normal);
            var half = HalfAlong(parent, arc);
            var offset = settings.SpurOffsetPerHalfWidth * half;
            var p0 = point + side * normal * offset;
            if (!OnMainland(p0) || SdAt(p0) < settings.SpurToeMeters + settings.SpurMinLengthMeters * 0.5f)
            {
                SpursSkippedLength++;
                return -1;
            }

            var g = SdGradient(p0);
            if (g.sqrMagnitude < 1e-8f || Vector2.Dot(-g.normalized, side * normal) < 0.2f)
            {
                SpursSkippedSide++;
                return -1;
            }

            var path = Descend(p0);
            if (path.Count < 2 || PathLength(path) < settings.SpurMinLengthMeters)
            {
                SpursSkippedLength++;
                return -1;
            }

            var points = Geo2.ResamplePolyline(path, settings.ResampleMeters).ToArray();
            var spur = new G3Ridge
            {
                Kind = 2,
                Parent = parentIndex,
                Side = side,
                ParentArc = arc,
                Points = points,
                Cumulative = Cumulative(points),
                Crest = new float[points.Length],
                Half = new float[points.Length],
            };

            // A spur adds no height: it is the uncarved line the valleys on either side meet at.

            Ridges.Add(spur);
            SpursBuilt++;
            return Ridges.Count - 1;
        }

        /// <summary>Steepest descent on the Stage 1 coast distance: straight toward the nearest shore.</summary>
        private List<Vector2> Descend(Vector2 start)
        {
            var path = new List<Vector2> { start };
            var p = start;
            for (var step = 0; step < 400; step++)
            {
                if (SdAt(p) <= settings.SpurToeMeters)
                {
                    break;
                }

                var g = SdGradient(p);
                if (g.sqrMagnitude < 1e-8f)
                {
                    break;
                }

                p -= g.normalized * settings.DescentStepMeters;
                if (!OnMainland(p))
                {
                    break;
                }

                path.Add(p);
            }

            return path;
        }

        // ---------- G3b: converging valley tree (research record §12) ----------

        public float[] FloorSurface;
        public float[] Routing;
        public List<Vector2[]> DesignedDivides;
        public int Trunks, CorridorHalves, TrunksJoined, TrunksSkipped, WedgesSkipped, StuckSteps;
        public int Links, LinkHeads, LinksIntoTrunks, LinksToCoast, LinksIntoLinks;
        public float LargestLinkArea;
        public int DesignedOrder, DesignedOrderAtChannelArea;
        public float CorridorSaddleCrest = float.NaN;
        public Vector2 CorridorCol;

        private void BuildFloorSurface(IslandReliefWorld w)
        {
            // The surface G3 keeps: H4A − U2 − A, exactly what Compose adds S to.
            var u2 = w.Stage2B.UpliftField.Heights;
            FloorSurface = new float[N * N];
            for (var i = 0; i < FloorSurface.Length; i++)
            {
                FloorSurface[i] = w.H4A[i] - u2[i] - w.Apron[i];
            }

            // The coast gate Compose applies, so the tree is routed and its floors made monotone on the
            // surface that is actually written, not on the ungated one.
            Gate = new float[N * N];
            for (var i = 0; i < Gate.Length; i++)
            {
                Gate[i] = Sd[i] <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Sd[i] / settings.GateMeters));
            }
        }

        public float[] Gate;
        public float[] RoutingS;

        /// <summary>Design height the trunks are traced on: the floor surface plus the ridge envelope.</summary>
        private float DesignAt(Vector2 p) => Bilinear(FloorSurface, p) + Bilinear(Gate, p) * Bilinear(Envelope, p);

        private Vector2 DesignGradient(Vector2 p)
        {
            var e = Cell;
            var gx = DesignAt(p + new Vector2(e, 0f)) - DesignAt(p - new Vector2(e, 0f));
            var gz = DesignAt(p + new Vector2(0f, e)) - DesignAt(p - new Vector2(0f, e));
            return new Vector2(gx, gz) / (2f * e);
        }

        /// <summary>
        /// Steepest descent on the design height, halving the step until it goes down, and falling back
        /// to G3's coast-distance step where the design height has no lower point ahead. Stops at the spur
        /// toe, off the mainland, or on joining a trunk-class valley.
        /// </summary>
        private List<Vector2> DescendNet(Vector2 start, int excludeGroup, bool appendJoin, out int joins, out int joinNode)
        {
            joins = -1;
            joinNode = 0;
            var path = new List<Vector2> { start };
            var p = start;
            var travelled = 0f;
            for (var step = 0; step < 600; step++)
            {
                if (SdAt(p) <= settings.SpurToeMeters)
                {
                    break;
                }

                if (travelled >= settings.JoinMinTravelMeters && TryJoin(p, excludeGroup, out joins, out joinNode, out var at))
                {
                    if (appendJoin)
                    {
                        path.Add(at);
                    }

                    break;
                }

                var here = DesignAt(p);
                var g = DesignGradient(p);
                var next = p;
                var found = false;
                if (g.sqrMagnitude > 1e-12f)
                {
                    var dir = -g.normalized;
                    for (var length = settings.DescentStepMeters; length >= settings.DescentStepMeters * 0.25f && !found; length *= 0.5f)
                    {
                        next = p + dir * length;
                        found = DesignAt(next) < here;
                    }
                }

                if (!found)
                {
                    var gs = SdGradient(p);
                    if (gs.sqrMagnitude < 1e-8f)
                    {
                        break;
                    }

                    next = p - gs.normalized * settings.DescentStepMeters;
                    StuckSteps++;
                }

                if (!OnMainland(next))
                {
                    break;
                }

                travelled += (next - p).magnitude;
                p = next;
                path.Add(p);
            }

            return path;
        }

        /// <summary>
        /// Extends a path that ends on land straight down the Stage 1 coast distance until it leaves the
        /// mainland, so its trough reaches the shore, where the coast gate takes it to zero.
        /// </summary>
        private void ToShore(List<Vector2> path)
        {
            var p = path[path.Count - 1];
            for (var step = 0; step < 100 && OnMainland(p); step++)
            {
                var g = SdGradient(p);
                if (g.sqrMagnitude < 1e-8f)
                {
                    break;
                }

                p -= g.normalized * settings.DescentStepMeters;
                path.Add(p);
            }
        }

        /// <summary>Nearest trunk-class valley whose axis lies within the join distance of <paramref name="p"/>.</summary>
        private bool TryJoin(Vector2 p, int excludeGroup, out int joins, out int node, out Vector2 at)
        {
            joins = -1;
            node = 0;
            at = p;
            var best = float.PositiveInfinity;
            for (var v = 0; v < Valleys.Count; v++)
            {
                var trunk = Valleys[v];
                if (!trunk.IsTrunk || (excludeGroup >= 0 && trunk.Group == excludeGroup))
                {
                    continue;
                }

                var d = ClosestOn(trunk.Points, trunk.Cumulative, p, out var arc, out var q, out var segment);
                var limit = settings.TrunkJoinMeters + 0.5f * Interpolate(trunk.Cumulative, trunk.FlatHalfNodes, arc);
                if (d <= limit && d < best)
                {
                    best = d;
                    joins = v;
                    at = q;
                    node = arc - trunk.Cumulative[segment] <= trunk.Cumulative[segment + 1] - arc ? segment : segment + 1;
                }
            }

            return joins >= 0;
        }

        private Vector2[] SmoothAndResample(List<Vector2> raw)
        {
            return Geo2.ResamplePolyline(Smooth(raw, settings.DescentSmoothHalfWindow), settings.ResampleMeters).ToArray();
        }

        /// <summary>Moving average over the interior points; the end points stay where they are.</summary>
        private static List<Vector2> Smooth(List<Vector2> raw, int h)
        {
            var smooth = new List<Vector2>(raw.Count);
            for (var i = 0; i < raw.Count; i++)
            {
                if (i == 0 || i == raw.Count - 1)
                {
                    smooth.Add(raw[i]);
                    continue;
                }

                var sum = Vector2.zero;
                var count = 0;
                for (var j = Mathf.Max(0, i - h); j <= Mathf.Min(raw.Count - 1, i + h); j++)
                {
                    sum += raw[j];
                    count++;
                }

                smooth.Add(sum / count);
            }

            return smooth;
        }

        /// <summary>
        /// Trunk valleys: one along the low line of every wedge between a branch and its parent ridge,
        /// and a corridor across the lowest declared saddle, as two halves descending from the col.
        /// </summary>
        private void BuildTrunks()
        {
            for (var b = 1; b < Ridges.Count; b++)
            {
                var branch = Ridges[b];
                if (branch.Kind != 1 || branch.Points.Length < 2)
                {
                    continue;
                }

                var parent = Ridges[branch.Parent];
                var root = branch.Points[0];
                var arc = branch.ParentArc;
                var along = (G3Diagnostics.PointAt(branch.Points, branch.Cumulative, Mathf.Min(60f, branch.Length)) - root).normalized;
                var ahead = G3Diagnostics.PointAt(parent.Points, parent.Cumulative, Mathf.Min(parent.Length, arc + 20f));
                var behind = G3Diagnostics.PointAt(parent.Points, parent.Cumulative, Mathf.Max(0f, arc - 20f));
                var tangent = (ahead - behind).normalized;
                var hwRoot = Mathf.Min(HalfAlong(parent, arc), branch.Half[0]);
                for (var dir = 1; dir >= -1; dir -= 2)
                {
                    var remaining = dir > 0 ? parent.Length - arc : arc;
                    var bisector = along + dir * tangent;
                    if (remaining < hwRoot || bisector.sqrMagnitude < 1e-4f)
                    {
                        WedgesSkipped++;
                        continue;
                    }

                    var start = root + bisector.normalized * (settings.WedgeStartPerHalfWidth * hwRoot);
                    if (!OnMainland(start))
                    {
                        WedgesSkipped++;
                        continue;
                    }

                    AddTrunk(G3Valley.TrunkClass, b, -1, start, null, 0f, hwRoot);
                }
            }

            var lowest = -1;
            for (var m = 0; m < Marks.Count; m++)
            {
                if (Marks[m].IsSaddle && (lowest < 0 || Marks[m].Crest < Marks[lowest].Crest))
                {
                    lowest = m;
                }
            }

            if (lowest < 0 || Ridges.Count == 0)
            {
                return;
            }

            var spine = Ridges[0];
            var col = Marks[lowest];
            CorridorSaddleCrest = col.Crest;
            PointAndNormal(spine, col.Arc, out var colPoint, out var normal);
            CorridorCol = colPoint;
            var hw = HalfAlong(spine, col.Arc);
            for (var side = -1; side <= 1; side += 2)
            {
                var start = colPoint + side * normal * (0.2f * hw);
                if (OnMainland(start))
                {
                    AddTrunk(G3Valley.CorridorClass, 0, 0, start, colPoint, col.Arc, hw);
                }
            }
        }

        private void AddTrunk(int cls, int parent, int group, Vector2 start, Vector2? head, float parentArc, float hwRef)
        {
            var path = DescendNet(start, group, true, out var joins, out var joinNode);
            if (head.HasValue)
            {
                path.Insert(0, head.Value);
            }

            if (joins < 0)
            {
                ToShore(path);
            }

            if (path.Count < 2 || PathLength(path) < settings.SpurMinLengthMeters)
            {
                TrunksSkipped++;
                return;
            }

            var axis = SmoothAndResample(path);
            var cum = Cumulative(axis);
            var length = Mathf.Max(1f, cum[cum.Length - 1]);
            var flatMax = Mathf.Min(settings.FlatHalfWidthMaxMeters, settings.FlatHalfWidthPerHalfWidth * hwRef);
            var wall = settings.TrunkWallPerHalfWidth * hwRef;
            var flat = new float[axis.Length];
            var reach = new float[axis.Length];
            for (var k = 0; k < axis.Length; k++)
            {
                flat[k] = cls == G3Valley.CorridorClass ? flatMax : flatMax * G3Settings.Smooth01(cum[k] / (settings.TrunkLowFraction * length));
                reach[k] = flat[k] + wall;
            }

            Valleys.Add(new G3Valley
            {
                Class = cls,
                Parent = parent,
                Group = group,
                Points = axis,
                Cumulative = cum,
                HalfWidth = Mathf.Max(flatMax, settings.TrunkJoinMeters),
                CarveHalfWidth = flatMax + wall,
                ParentArc = parentArc,
                JoinsInto = joins,
                JoinNode = joinNode,
                ProfileLength = length,
                FlatHalfNodes = flat,
                CarveHalfNodes = reach,
            });

            if (cls == G3Valley.CorridorClass)
            {
                CorridorHalves++;
            }
            else
            {
                Trunks++;
            }

            TrunksJoined += joins >= 0 ? 1 : 0;
        }

        /// <summary>
        /// The design height with the trunks carved (floors without tributaries yet): the surface the
        /// side network is routed on, so side valleys drain into a trunk wherever they reach its walls.
        /// </summary>
        private void BuildRouting()
        {
            ComputeFloors();
            CarveValleys(Valleys, out var carve, out var lowest);
            Routing = new float[N * N];
            RoutingS = new float[N * N];
            for (var i = 0; i < Routing.Length; i++)
            {
                RoutingS[i] = Mathf.Max(lowest[i], Envelope[i] - carve[i]);
                Routing[i] = FloorSurface[i] + Gate[i] * RoutingS[i];
            }
        }

        /// <summary>
        /// Side valleys as a drainage tree. On the skeleton's coarse grid, the routing height (trunks
        /// carved) plus a slope-scaled perturbation is priority-flood filled toward the ocean and the trunk
        /// floors, routed D8, and accumulated; cells draining at least <see cref="G3Settings.RouteChannelAreaSqm"/>
        /// are channels, split into links at confluences. The perturbation only decides where flow lines
        /// merge — it is not part of any height. Each link becomes a valley whose depth and width grow with
        /// its drained area.
        /// </summary>
        private void BuildRoutedNetwork()
        {
            var k = settings.CoarseStride;
            var count = nc * nc;
            var step = k * Cell;
            var cellArea = step * step;
            var height = new float[count];
            var sink = new bool[count];
            var trunkOf = new int[count];
            for (var z = 0; z < nc; z++)
            {
                for (var x = 0; x < nc; x++)
                {
                    var c = z * nc + x;
                    trunkOf[c] = -1;
                    height[c] = Routing[(z * k) * N + x * k];
                    if (!coarseLand[c] || Sd[(z * k) * N + x * k] <= settings.SpurToeMeters)
                    {
                        // The ocean, and the coastal fringe where G3's valleys and spurs also end.
                        sink[c] = true;
                    }
                    else if (TryJoin(World(x * k, z * k), -1, out var t, out _, out _))
                    {
                        sink[c] = true;
                        trunkOf[c] = t;
                    }
                }
            }

            // Slope-scaled hollows and spurs: amplitude κ·|∇h|·λ per octave, so they turn flow lines by a
            // bounded angle wherever the ground slopes and never reverse it; the slope is read over
            // ±RouteSlopeStencil cells so the amplitude varies slowly. With RouteReliefInSurface the same
            // relief is added to the surface, so the final drainage is the routed one plus the carve.
            var perturbation = RouteNoise(step);
            var relief = new float[count];
            var routed = new float[count];
            var stencil = settings.RouteSlopeStencil;
            for (var z = 0; z < nc; z++)
            {
                for (var x = 0; x < nc; x++)
                {
                    var c = z * nc + x;
                    routed[c] = height[c];
                    if (!coarseLand[c])
                    {
                        continue;
                    }

                    var x0 = Mathf.Max(0, x - stencil);
                    var x1 = Mathf.Min(nc - 1, x + stencil);
                    var z0 = Mathf.Max(0, z - stencil);
                    var z1 = Mathf.Min(nc - 1, z + stencil);
                    var gx = (height[z * nc + x1] - height[z * nc + x0]) / Mathf.Max(1f, (x1 - x0) * step);
                    var gz = (height[z1 * nc + x] - height[z0 * nc + x]) / Mathf.Max(1f, (z1 - z0) * step);

                    // The smaller of the wide and the local slope: on a crest or a col the local slope is
                    // near zero, and a wide reading alone would dig hollows into it (a 10 m pit, §12).
                    var lx = (height[z * nc + Mathf.Min(nc - 1, x + 1)] - height[z * nc + Mathf.Max(0, x - 1)]) / (2f * step);
                    var lz = (height[Mathf.Min(nc - 1, z + 1) * nc + x] - height[Mathf.Max(0, z - 1) * nc + x]) / (2f * step);
                    var slope = Mathf.Min(Mathf.Sqrt(gx * gx + gz * gz), Mathf.Sqrt(lx * lx + lz * lz));
                    var body = settings.RouteEnvelopeFadeMeters > 0f ? G3Settings.Smooth01(Envelope[(z * k) * N + x * k] / settings.RouteEnvelopeFadeMeters) : 1f;
                    relief[c] = body * settings.RouteNoiseSlopeFraction * slope * perturbation[c];
                    if (!sink[c])
                    {
                        routed[c] += Gate[(z * k) * N + x * k] * relief[c];
                    }
                }
            }

            if (settings.RouteReliefInSurface)
            {
                Relief = new float[N * N];
                for (var z = 0; z < N; z++)
                {
                    for (var x = 0; x < N; x++)
                    {
                        var i = z * N + x;
                        if (!Mainland[i])
                        {
                            continue;
                        }

                        var fx = Mathf.Min(x / (float)k, nc - 1.001f);
                        var fz = Mathf.Min(z / (float)k, nc - 1.001f);
                        var cx = (int)fx;
                        var cz = (int)fz;
                        var u = fx - cx;
                        var v = fz - cz;
                        var a = Mathf.Lerp(relief[cz * nc + cx], relief[cz * nc + cx + 1], u);
                        var b = Mathf.Lerp(relief[(cz + 1) * nc + cx], relief[(cz + 1) * nc + cx + 1], u);
                        Relief[i] = Mathf.Lerp(a, b, v);
                    }
                }
            }

            // Priority flood from every sink: no pit survives, every land cell drains.
            var filled = new float[count];
            var done = new bool[count];
            var heap = new G3Heap(count);
            for (var c = 0; c < count; c++)
            {
                filled[c] = sink[c] ? routed[c] : float.PositiveInfinity;
                if (sink[c])
                {
                    heap.Push(filled[c], c);
                }
            }

            while (heap.Count > 0)
            {
                heap.Pop(out _, out var c);
                if (done[c])
                {
                    continue;
                }

                done[c] = true;
                var cx = c % nc;
                var cz = c / nc;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = cx + dx;
                        var z = cz + dz;
                        if ((dx == 0 && dz == 0) || x < 0 || z < 0 || x >= nc || z >= nc)
                        {
                            continue;
                        }

                        var nb = z * nc + x;
                        if (done[nb] || sink[nb])
                        {
                            continue;
                        }

                        var value = Mathf.Max(routed[nb], filled[c] + 1e-3f);
                        if (value < filled[nb])
                        {
                            filled[nb] = value;
                            heap.Push(value, nb);
                        }
                    }
                }
            }

            // D8 on the filled surface; accumulation from the highest cell down.
            var receiver = new int[count];
            var order = new List<int>();
            for (var c = 0; c < count; c++)
            {
                receiver[c] = -1;
                if (sink[c])
                {
                    continue;
                }

                order.Add(c);
                var cx = c % nc;
                var cz = c / nc;
                var best = 0f;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = cx + dx;
                        var z = cz + dz;
                        if ((dx == 0 && dz == 0) || x < 0 || z < 0 || x >= nc || z >= nc)
                        {
                            continue;
                        }

                        var nb = z * nc + x;
                        var drop = (filled[c] - filled[nb]) / ((dx != 0 && dz != 0) ? 1.41421356f : 1f);
                        if (drop > best)
                        {
                            best = drop;
                            receiver[c] = nb;
                        }
                    }
                }
            }

            order.Sort((p, q) => filled[q] != filled[p] ? filled[q].CompareTo(filled[p]) : p.CompareTo(q));
            var area = new float[count];
            foreach (var c in order)
            {
                area[c] += cellArea;
                if (receiver[c] >= 0 && !sink[receiver[c]])
                {
                    area[receiver[c]] += area[c];
                }
            }

            var channel = new bool[count];
            var upstream = new int[count];
            foreach (var c in order)
            {
                channel[c] = area[c] >= settings.RouteChannelAreaSqm && receiver[c] >= 0;
            }

            foreach (var c in order)
            {
                if (channel[c] && !sink[receiver[c]])
                {
                    upstream[receiver[c]]++;
                }
            }

            // Links: from a head (no channel upstream) or a confluence (two or more) down to the next
            // confluence, a trunk floor or the coast.
            var trunkCount = Valleys.Count;
            var linkOf = new int[count];
            for (var c = 0; c < count; c++)
            {
                linkOf[c] = -1;
            }

            var starts = new List<int>();
            for (var c = 0; c < count; c++)
            {
                if (channel[c] && upstream[c] != 1)
                {
                    starts.Add(c);
                }
            }

            var endCell = new List<int>();
            var cells = new List<List<int>>();
            foreach (var s in starts)
            {
                var list = new List<int> { s };
                linkOf[s] = trunkCount + cells.Count;
                var cur = s;
                while (true)
                {
                    var r = receiver[cur];
                    if (r < 0 || sink[r] || upstream[r] >= 2)
                    {
                        endCell.Add(r);
                        break;
                    }

                    list.Add(r);
                    linkOf[r] = trunkCount + cells.Count;
                    cur = r;
                }

                cells.Add(list);
            }

            for (var l = 0; l < cells.Count; l++)
            {
                var list = cells[l];
                var pts = new List<Vector2>();
                var areas = new List<float>();
                foreach (var c in list)
                {
                    pts.Add(World((c % nc) * k, (c / nc) * k));
                    areas.Add(area[c]);
                }

                var end = endCell[l];
                var joins = -1;
                var joinNode = 0;
                if (end >= 0 && !sink[end])
                {
                    // Confluence: the end cell starts the receiving link.
                    joins = linkOf[end];
                    pts.Add(World((end % nc) * k, (end / nc) * k));
                    areas.Add(area[end]);
                    LinksIntoLinks++;
                }
                else if (end >= 0 && trunkOf[end] >= 0)
                {
                    var trunk = Valleys[trunkOf[end]];
                    ClosestOn(trunk.Points, trunk.Cumulative, World((end % nc) * k, (end / nc) * k), out var arc, out var at, out var segment);
                    joins = trunkOf[end];
                    joinNode = arc - trunk.Cumulative[segment] <= trunk.Cumulative[segment + 1] - arc ? segment : segment + 1;
                    pts.Add(at);
                    areas.Add(areas[areas.Count - 1]);
                    LinksIntoTrunks++;
                }
                else
                {
                    // To the coast: the trough continues straight down the coast distance to the shore
                    // (G3's rule), so it never stops short in the coastal fringe as a closed pit.
                    var before = pts.Count;
                    ToShore(pts);
                    if (pts.Count == before)
                    {
                        pts.Add(pts[pts.Count - 1]);
                    }

                    while (areas.Count < pts.Count)
                    {
                        areas.Add(areas[areas.Count - 1]);
                    }

                    LinksToCoast++;
                }

                var axis = Smooth(pts, settings.RouteSmoothHalfWindow).ToArray();
                var mouthArea = areas[areas.Count - 1];
                LargestLinkArea = Mathf.Max(LargestLinkArea, mouthArea);
                var reach = new float[axis.Length];
                var flat = new float[axis.Length];
                for (var n = 0; n < axis.Length; n++)
                {
                    reach[n] = RouteHalfWidth(areas[n]);
                    flat[n] = RouteFlatHalfWidth(areas[n], reach[n]);
                }

                LinkHeads += upstream[list[0]] == 0 ? 1 : 0;
                Valleys.Add(new G3Valley
                {
                    Class = G3Valley.RoutedClass,
                    Parent = 0,
                    Points = axis,
                    Cumulative = Cumulative(axis),
                    HalfWidth = 0.5f * RouteHalfWidth(mouthArea),
                    CarveHalfWidth = RouteHalfWidth(mouthArea),
                    JoinsInto = joins,
                    JoinNode = joinNode,
                    IsHead = upstream[list[0]] == 0,
                    AreaNodes = areas.ToArray(),
                    FlatHalfNodes = flat,
                    CarveHalfNodes = reach,
                });
                Links++;
            }

            // Designed divides: land cells whose 4-neighbour drains into a different link or trunk.
            var label = new int[count];
            for (var c = 0; c < count; c++)
            {
                label[c] = -2;
            }

            for (var c = 0; c < count; c++)
            {
                LabelOf(c, label, linkOf, receiver, sink, trunkOf);
            }

            DesignedDivides = new List<Vector2[]>();
            for (var z = 1; z < nc - 1; z++)
            {
                for (var x = 1; x < nc - 1; x++)
                {
                    var c = z * nc + x;
                    if (sink[c] || label[c] < 0)
                    {
                        continue;
                    }

                    foreach (var nb in new[] { c + 1, c - 1, c + nc, c - nc })
                    {
                        if (!sink[nb] && label[nb] >= 0 && label[nb] != label[c])
                        {
                            var p = World(x * k, z * k);
                            DesignedDivides.Add(new[] { p, p + new Vector2(0.5f * step, 0f) });
                            break;
                        }
                    }
                }
            }

            DesignedOrder = 0;
            DesignedOrderAtChannelArea = 0;
            var orders = new int[Valleys.Count];
            var measured = new int[Valleys.Count];
            var channelArea = GameDev.World.Hydrology.HydrologySettings.Default.ChannelInitiationAreaSqm;
            for (var v = 0; v < Valleys.Count; v++)
            {
                DesignedOrder = Mathf.Max(DesignedOrder, OrderOf(v, orders, 0f));
                if (Present(Valleys[v], channelArea))
                {
                    DesignedOrderAtChannelArea = Mathf.Max(DesignedOrderAtChannelArea, OrderOf(v, measured, channelArea));
                }
            }
        }

        /// <summary>A trunk always; a link only if its mouth drains at least <paramref name="minArea"/>.</summary>
        private static bool Present(G3Valley v, float minArea) =>
            v.IsTrunk || v.AreaNodes == null || v.AreaNodes[v.AreaNodes.Length - 1] >= minArea;

        public float[] Relief;

        /// <summary>
        /// Makes the routing relief part of the surface: added to the envelope the valleys are carved
        /// into and to the routing height the link floors are read from.
        /// </summary>
        private void ApplyRelief()
        {
            if (Relief == null)
            {
                return;
            }

            for (var i = 0; i < Relief.Length; i++)
            {
                Envelope[i] += Relief[i];
                RoutingS[i] += Relief[i];
                Routing[i] += Gate[i] * Relief[i];
            }
        }

        /// <summary>The link or trunk a coarse cell first drains into, or -1 for the coast.</summary>
        private static int LabelOf(int c, int[] label, int[] linkOf, int[] receiver, bool[] sink, int[] trunkOf)
        {
            var path = new List<int>();
            var cur = c;
            var result = -1;
            while (true)
            {
                if (label[cur] != -2)
                {
                    result = label[cur];
                    break;
                }

                if (sink[cur])
                {
                    result = trunkOf[cur];
                    label[cur] = result;
                    break;
                }

                if (linkOf[cur] >= 0)
                {
                    result = linkOf[cur];
                    label[cur] = result;
                    break;
                }

                path.Add(cur);
                if (receiver[cur] < 0)
                {
                    break;
                }

                cur = receiver[cur];
            }

            foreach (var q in path)
            {
                label[q] = result;
            }

            return result;
        }

        private float RouteHalfWidth(float area) =>
            Mathf.Clamp(settings.RouteWidthPerSqrtArea * Mathf.Sqrt(area), settings.RouteWidthMinMeters, settings.RouteWidthMaxMeters);

        private float RouteFlatHalfWidth(float area, float halfWidth) =>
            area <= settings.RouteFlatAreaSqm
                ? 0f
                : Mathf.Min(settings.FlatHalfWidthMaxMeters, settings.RouteFlatPerHalfWidth * halfWidth)
                  * G3Settings.Smooth01((area - settings.RouteFlatAreaSqm) / settings.RouteFlatAreaSqm);

        /// <summary>Σ over octaves of λ·n(p), n a smooth value noise in [-1, 1] from the research.g3b.route streams.</summary>
        private float[] RouteNoise(float step)
        {
            var total = new float[nc * nc];
            for (var o = 0; o < settings.RouteNoiseWavelengthsMeters.Length; o++)
            {
                var lambda = settings.RouteNoiseWavelengthsMeters[o];
                var g = Mathf.CeilToInt(nc * step / lambda) + 2;
                var rng = new GameDev.World.Seed.WorldSeed(Seed).Fork("research.g3b.route", o);
                var values = new float[g * g];
                for (var i = 0; i < values.Length; i++)
                {
                    values[i] = (float)(rng.NextDouble() * 2.0 - 1.0);
                }

                for (var z = 0; z < nc; z++)
                {
                    for (var x = 0; x < nc; x++)
                    {
                        var fx = x * step / lambda;
                        var fz = z * step / lambda;
                        var x0 = (int)fx;
                        var z0 = (int)fz;
                        var u = G3Settings.Smooth01(fx - x0);
                        var v = G3Settings.Smooth01(fz - z0);
                        var a = Mathf.Lerp(values[z0 * g + x0], values[z0 * g + x0 + 1], u);
                        var b = Mathf.Lerp(values[(z0 + 1) * g + x0], values[(z0 + 1) * g + x0 + 1], u);
                        total[z * nc + x] += lambda * Mathf.Lerp(a, b, v);
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Strahler order of the declared tree, ignoring channel-initiation area: a valley heads as a
        /// first-order channel unless its head is a confluence; tributaries count in downstream order.
        /// </summary>
        private int OrderOf(int v, int[] orders, float minArea)
        {
            if (orders[v] > 0)
            {
                return orders[v];
            }

            orders[v] = 1;
            var joining = new List<int>();
            for (var u = 0; u < Valleys.Count; u++)
            {
                if (Valleys[u].JoinsInto == v && Present(Valleys[u], minArea))
                {
                    joining.Add(u);
                }
            }

            joining.Sort((p, q) => Valleys[p].JoinNode != Valleys[q].JoinNode ? Valleys[p].JoinNode.CompareTo(Valleys[q].JoinNode) : p.CompareTo(q));
            // A link that starts at a confluence takes its order from the links meeting there, unless
            // none of them is present at this area, in which case it heads as a first-order channel.
            var fedAtHead = false;
            foreach (var u in joining)
            {
                fedAtHead |= Valleys[u].JoinNode == 0;
            }

            var current = Valleys[v].Class == G3Valley.RoutedClass && !Valleys[v].IsHead && fedAtHead ? 0 : 1;
            foreach (var u in joining)
            {
                var o = OrderOf(u, orders, minArea);
                current = current == 0 ? o : o == current ? current + 1 : Mathf.Max(current, o);
            }

            orders[v] = Mathf.Max(1, current);
            return orders[v];
        }

        private void EvaluateNetwork()
        {
            ComputeFloors();
            CarveValleys(Valleys, out var carve, out var lowest);
            Carve = carve;
            Uplift = new float[N * N];
            for (var i = 0; i < Uplift.Length; i++)
            {
                Uplift[i] = Mathf.Max(-settings.IncisionMeters, Mathf.Max(lowest[i], Envelope[i] - carve[i]));
            }
        }

        /// <summary>Floors of every valley in <see cref="Valleys"/>, tributaries before their receivers.</summary>
        private void ComputeFloors()
        {
            var count = Valleys.Count;
            var tributaries = new List<int>[count];
            for (var v = 0; v < count; v++)
            {
                tributaries[v] = new List<int>();
            }

            for (var v = 0; v < count; v++)
            {
                if (Valleys[v].JoinsInto >= 0)
                {
                    tributaries[Valleys[v].JoinsInto].Add(v);
                }
            }

            var state = new int[count];
            for (var v = 0; v < count; v++)
            {
                FloorOf(v, tributaries, state);
            }
        }

        private void CarveValleys(List<G3Valley> valleys, out float[] carve, out float[] lowest)
        {
            var envelope = Envelope;
            var carved = new float[N * N];
            var bounds = new float[N * N];
            var crossFall = Mathf.Tan(settings.FlatCrossFallDegrees * Mathf.Deg2Rad);
            foreach (var valley in valleys)
            {
                if (valley.Points.Length < 2)
                {
                    continue;
                }

                var flats = valley.FlatHalfNodes;
                Rasterize(valley.Points, valley.CarveHalfNodes, (i, segment, t, dist, half) =>
                {
                    var floor = Mathf.Lerp(valley.Floor[segment], valley.Floor[segment + 1], t);
                    var flat = Mathf.Lerp(flats[segment], flats[segment + 1], t);
                    float target, weight;
                    if (flat > 0f)
                    {
                        // Flat floor with a gentle fall to the axis, then walls back up to the envelope.
                        target = floor + crossFall * Mathf.Min(dist, flat);
                        weight = dist <= flat ? 1f : G3Settings.Flank((dist - flat) / Mathf.Max(1e-3f, half - flat));
                    }
                    else
                    {
                        target = floor;
                        weight = G3Settings.CarveAcross(dist / half);
                    }

                    var depth = Mathf.Max(0f, envelope[i] - target) * weight;
                    if (depth > carved[i])
                    {
                        carved[i] = depth;
                    }

                    var bound = Mathf.Min(0f, target) * weight;
                    if (bound < bounds[i])
                    {
                        bounds[i] = bound;
                    }
                });
            }

            carve = carved;
            lowest = bounds;
        }

        /// <summary>
        /// Floor of one valley, after every tributary's: the target is the class profile in absolute
        /// height; each tributary's mouth floor is imposed on the node it joins; the running minimum from
        /// the head makes the floor monotone; the trough bound keeps it at most IncisionMeters below the
        /// floor surface.
        /// </summary>
        private void FloorOf(int v, List<int>[] tributaries, int[] state)
        {
            if (state[v] != 0)
            {
                return;
            }

            state[v] = 1;
            foreach (var u in tributaries[v])
            {
                FloorOf(u, tributaries, state);
            }

            var valley = Valleys[v];
            var pts = valley.Points;
            var count = pts.Length;
            var target = new float[count];
            var fs = new float[count];
            var gate = new float[count];
            for (var k = 0; k < count; k++)
            {
                var p = pts[k];
                fs[k] = Bilinear(FloorSurface, p);
                gate[k] = Bilinear(Gate, p);
                float s;
                if (valley.IsTrunk)
                {
                    var e = Bilinear(Envelope, p);
                    var low = G3Settings.Smooth01(valley.Cumulative[k] / (settings.TrunkLowFraction * valley.ProfileLength));
                    var top = valley.Class == G3Valley.CorridorClass ? Mathf.Min(e, settings.CorridorColMeters) : e;
                    s = Mathf.Lerp(top, -settings.IncisionMeters, low);
                }
                else
                {
                    // Routed link: the routing height (trunks carved) less a depth that grows with the
                    // drained area, tapered in over the first metres of a head link.
                    var depth = settings.RouteDepthPerSqrtArea * Mathf.Sqrt(valley.AreaNodes[k]);
                    if (valley.IsHead)
                    {
                        depth *= G3Settings.Smooth01(valley.Cumulative[k] / settings.RouteHeadTaperMeters);
                    }

                    s = Bilinear(RoutingS, p) - depth;
                }

                target[k] = fs[k] + gate[k] * s;
            }

            foreach (var u in tributaries[v])
            {
                var mouth = Valleys[u].FloorAbs;
                var j = Mathf.Clamp(Valleys[u].JoinNode, 0, count - 1);
                target[j] = Mathf.Min(target[j], mouth[mouth.Length - 1]);
            }

            valley.Floor = new float[count];
            valley.FloorAbs = new float[count];
            var running = float.PositiveInfinity;
            for (var k = 0; k < count; k++)
            {
                running = Mathf.Min(running, target[k]);
                var sFloor = Mathf.Max((running - fs[k]) / Mathf.Max(1e-3f, gate[k]), -settings.IncisionMeters);
                valley.Floor[k] = sFloor;
                valley.FloorAbs[k] = fs[k] + gate[k] * sFloor;
            }

            state[v] = 2;
        }

        private static float PathLength(List<Vector2> path)
        {
            var total = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                total += (path[i] - path[i - 1]).magnitude;
            }

            return total;
        }

        public static float[] Cumulative(Vector2[] points)
        {
            var cum = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                cum[i] = cum[i - 1] + (points[i] - points[i - 1]).magnitude;
            }

            return cum;
        }

        public static float ClosestOn(Vector2[] pts, float[] cum, Vector2 p, out float arc, out Vector2 at, out int segment)
        {
            var best = float.PositiveInfinity;
            arc = 0f;
            at = pts[0];
            segment = 0;
            for (var k = 0; k + 1 < pts.Length; k++)
            {
                var q = Geo2.ClosestPointOnSegment(p, pts[k], pts[k + 1]);
                var d = (p - q).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    at = q;
                    segment = k;
                    arc = cum[k] + (q - pts[k]).magnitude;
                }
            }

            return Mathf.Sqrt(best);
        }

        // ---------- the field ----------

        private float[] BuildEnvelope()
        {
            var envelope = new float[N * N];
            foreach (var ridge in Ridges)
            {
                if (ridge.Kind == 2 || ridge.Points.Length < 2)
                {
                    continue;
                }

                Rasterize(ridge.Points, ridge.Half, (i, segment, t, dist, half) =>
                {
                    var crest = Mathf.Lerp(ridge.Crest[segment], ridge.Crest[segment + 1], t);
                    var value = crest * G3Settings.Flank(dist / half);
                    if (value > envelope[i])
                    {
                        envelope[i] = value;
                    }
                });
            }

            return envelope;
        }

        private void EvaluateUplift()
        {
            var envelope = BuildEnvelope();
            var carve = new float[N * N];
            var lowest = new float[N * N];
            foreach (var valley in Valleys)
            {
                var pts = valley.Points;
                if (pts.Length < 2)
                {
                    continue;
                }

                // Floor-referenced and monotone by construction: the target floor is the envelope along the
                // axis minus a depth that peaks a third of the way down, and the floor is its running
                // minimum from the head, so it never rises downstream.
                var count = pts.Length;
                var crestHead = CrestAlong(Ridges[valley.Parent], valley.ParentArc);
                valley.Floor = new float[count];
                var length = Mathf.Max(1f, valley.Cumulative[count - 1]);
                var running = float.PositiveInfinity;
                for (var k = 0; k < count; k++)
                {
                    var t = valley.Cumulative[k] / length;
                    var depth = crestHead * (settings.HeadNotchPerCrest * (1f - t)
                        + (settings.ValleyDepthPerCrest - settings.HeadNotchPerCrest) * 6.75f * t * (1f - t) * (1f - t));
                    running = Mathf.Min(running, Bilinear(envelope, pts[k]) - depth);
                    valley.Floor[k] = Mathf.Max(running, -settings.IncisionMeters);
                }

                var halves = new float[count];
                for (var k = 0; k < count; k++)
                {
                    halves[k] = valley.CarveHalfWidth;
                }

                Rasterize(pts, halves, (i, segment, t, dist, half) =>
                {
                    var floor = Mathf.Lerp(valley.Floor[segment], valley.Floor[segment + 1], t);
                    var across = G3Settings.CarveAcross(dist / half);
                    var depth = Mathf.Max(0f, envelope[i] - floor) * across;
                    if (depth > carve[i])
                    {
                        carve[i] = depth;
                    }

                    // Below the floor surface the trough may reach its floor, never the incision bound.
                    var bound = Mathf.Min(0f, floor) * across;
                    if (bound < lowest[i])
                    {
                        lowest[i] = bound;
                    }
                });
            }

            Envelope = envelope;
            Carve = carve;
            Uplift = new float[N * N];
            for (var i = 0; i < Uplift.Length; i++)
            {
                Uplift[i] = Mathf.Max(lowest[i], envelope[i] - carve[i]);
            }
        }

        private delegate void CellVisitor(int index, int segment, float t, float distance, float half);

        /// <summary>Visits every mainland cell within a polyline's half-width with its closest segment.</summary>
        private void Rasterize(Vector2[] pts, float[] halves, CellVisitor visit)
        {
            var maxHalf = 0f;
            var min = pts[0];
            var max = pts[0];
            foreach (var h in halves)
            {
                maxHalf = Mathf.Max(maxHalf, h);
            }

            foreach (var p in pts)
            {
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            var x0 = Mathf.Max(0, Mathf.FloorToInt((min.x - maxHalf - OriginX) / Cell));
            var x1 = Mathf.Min(N - 1, Mathf.CeilToInt((max.x + maxHalf - OriginX) / Cell));
            var z0 = Mathf.Max(0, Mathf.FloorToInt((min.y - maxHalf - OriginZ) / Cell));
            var z1 = Mathf.Min(N - 1, Mathf.CeilToInt((max.y + maxHalf - OriginZ) / Cell));
            var segMin = new Vector2[pts.Length - 1];
            var segMax = new Vector2[pts.Length - 1];
            for (var k = 0; k + 1 < pts.Length; k++)
            {
                var pad = Mathf.Max(halves[k], halves[k + 1]);
                segMin[k] = Vector2.Min(pts[k], pts[k + 1]) - new Vector2(pad, pad);
                segMax[k] = Vector2.Max(pts[k], pts[k + 1]) + new Vector2(pad, pad);
            }

            for (var z = z0; z <= z1; z++)
            {
                for (var x = x0; x <= x1; x++)
                {
                    var i = z * N + x;
                    if (!Mainland[i])
                    {
                        continue;
                    }

                    var p = World(x, z);
                    var best = float.PositiveInfinity;
                    var bestSeg = -1;
                    var bestT = 0f;
                    for (var k = 0; k + 1 < pts.Length; k++)
                    {
                        if (p.x < segMin[k].x || p.x > segMax[k].x || p.y < segMin[k].y || p.y > segMax[k].y)
                        {
                            continue;
                        }

                        var ab = pts[k + 1] - pts[k];
                        var len2 = Mathf.Max(1e-6f, ab.sqrMagnitude);
                        var t = Mathf.Clamp01(Vector2.Dot(p - pts[k], ab) / len2);
                        var d = (p - (pts[k] + ab * t)).sqrMagnitude;
                        if (d < best)
                        {
                            best = d;
                            bestSeg = k;
                            bestT = t;
                        }
                    }

                    if (bestSeg < 0)
                    {
                        continue;
                    }

                    var dist = Mathf.Sqrt(best);
                    var half = Mathf.Lerp(halves[bestSeg], halves[bestSeg + 1], bestT);
                    if (dist < half)
                    {
                        visit(i, bestSeg, bestT, dist, half);
                    }
                }
            }
        }

        public float[] Carve;
        public float[] Envelope;

        /// <summary>
        /// <c>H4A − U2 − A + S·smoothstep(d / gate)</c>. Ocean and shore samples equal <c>H4A</c> exactly.
        /// </summary>
        public float[] Compose(IslandReliefWorld w, float gateMeters, out G3Composition stats)
        {
            stats = new G3Composition { GateMeters = gateMeters };
            var h = new float[N * N];
            var u2 = w.Stage2B.UpliftField.Heights;
            double sTotal = 0, sKept = 0;
            var landCells = 0;
            var mountainCells = 0;
            for (var i = 0; i < h.Length; i++)
            {
                var floor = w.H4A[i] - u2[i] - w.Apron[i];
                var sd = Sd[i];
                var gate = sd <= 0f ? 0f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(sd / gateMeters));
                var s = Uplift[i] * gate;
                var value = floor + s;
                if (value > WorldScale.TerrainHeightBudgetMeters)
                {
                    stats.Clamped++;
                    value = WorldScale.TerrainHeightBudgetMeters;
                }

                h[i] = value;
                if (sd > 0f)
                {
                    landCells++;
                    sTotal += Mathf.Max(0f, Uplift[i]);
                    sKept += Mathf.Max(0f, s);
                    if (s < 0f)
                    {
                        stats.IncisedCells++;
                        stats.MaxIncision = Mathf.Max(stats.MaxIncision, -s);
                    }
                    mountainCells += s > IslandReliefSettings.Default.PlateUpliftMeters ? 1 : 0;
                }

                if (sd <= 0f && Mathf.Abs(value - w.H4A[i]) > 0f)
                {
                    stats.OceanChanged++;
                }

                if ((value > WorldScale.SeaLevelMeters) != (w.H4A[i] > WorldScale.SeaLevelMeters))
                {
                    stats.MaskChanged++;
                }
            }

            stats.GateLossShare = sTotal > 0 ? (float)(1.0 - sKept / sTotal) : 0f;
            stats.MountainShare = landCells > 0 ? mountainCells / (float)landCells : 0f;
            return h;
        }

        public void AppendStructure(StringBuilder sb, CultureInfo c)
        {
            var spines = 0;
            var branches = 0;
            var spurs = 0;
            var spurKm = 0f;
            foreach (var r in Ridges)
            {
                spines += r.Kind == 0 ? 1 : 0;
                branches += r.Kind == 1 ? 1 : 0;
                spurs += r.Kind == 2 ? 1 : 0;
                spurKm += r.Kind == 2 ? r.Length / 1000f : 0f;
            }

            var spine = Ridges.Count > 0 ? Ridges[0] : null;
            sb.AppendLine($"  [G3 structure] rmax {RadiusMax.ToString("F0", c)} m; spine {(spine != null ? spine.Length.ToString("F0", c) : "-")} m; branches {branches} (best protrusion {BranchProtrusionBest.ToString("F0", c)} m vs {settings.BranchMinProtrusionMeters} m); spurs {spurs} of {SpurStations} stations ({spurKm.ToString("F2", c)} km; skipped: gap {SpursSkippedGap}, side {SpursSkippedSide}, length {SpursSkippedLength}); valleys {Valleys.Count}; crest cap binding on {(CapBindingShare * 100f).ToString("F0", c)} % of the spine");
            var marks = new StringBuilder();
            foreach (var m in Marks)
            {
                marks.Append(m.IsSaddle ? " saddle " : " culmination ").Append(m.Crest.ToString("F0", c)).Append(" m @ ").Append(m.Arc.ToString("F0", c)).Append(" m;");
            }

            sb.AppendLine("  [G3 spine marks]" + marks);
            var carveMax = 0f;
            var carved = 0;
            double carveSum = 0;
            for (var i = 0; Carve != null && i < Carve.Length; i++)
            {
                carveMax = Mathf.Max(carveMax, Carve[i]);
                carved += Carve[i] > 1f ? 1 : 0;
                carveSum += Carve[i];
            }

            var floorDrop = new StringBuilder();
            for (var v = 0; v < Valleys.Count && v < 4; v++)
            {
                var f = Valleys[v].Floor;
                var e0 = Bilinear(Envelope, Valleys[v].Points[0]);
                floorDrop.Append($" v{v}: E(head) {e0.ToString("F0", c)} floor {f[0].ToString("F0", c)} -> {f[f.Length - 1].ToString("F0", c)} m, axis {Valleys[v].Cumulative[Valleys[v].Cumulative.Length - 1].ToString("F0", c)} m, half {Valleys[v].CarveHalfWidth.ToString("F0", c)} m;");
            }

            sb.AppendLine($"  [G3 carve] max {carveMax.ToString("F1", c)} m, cells > 1 m {carved}, volume {(carveSum * Cell * Cell / 1e6).ToString("F2", c)} Mm3;{floorDrop}");
            if (FloorSurface != null)
            {
                var trunkKm = 0f;
                var trunkLines = new StringBuilder();
                for (var v = 0; v < Valleys.Count; v++)
                {
                    var valley = Valleys[v];
                    if (!valley.IsTrunk)
                    {
                        continue;
                    }

                    var length = valley.Cumulative[valley.Cumulative.Length - 1];
                    trunkKm += length / 1000f;
                    var tribs = 0;
                    foreach (var u in Valleys)
                    {
                        tribs += u.JoinsInto == v ? 1 : 0;
                    }

                    trunkLines.Append($" v{v} {(valley.Class == G3Valley.CorridorClass ? "corridor" : "wedge")} of ridge {valley.Parent}: {length.ToString("F0", c)} m, floor {valley.FloorAbs[0].ToString("F0", c)} -> {valley.FloorAbs[valley.FloorAbs.Length - 1].ToString("F0", c)} m (S {valley.Floor[0].ToString("F0", c)} -> {valley.Floor[valley.Floor.Length - 1].ToString("F1", c)}), flat half {valley.FlatHalfNodes[valley.FlatHalfNodes.Length - 1].ToString("F0", c)} m, tributaries {tribs}, {(valley.JoinsInto >= 0 ? "joins v" + valley.JoinsInto : "to the coast")};");
                }

                var linkKm = 0f;
                foreach (var valley in Valleys)
                {
                    linkKm += valley.Class == G3Valley.RoutedClass ? valley.Cumulative[valley.Cumulative.Length - 1] / 1000f : 0f;
                }

                sb.AppendLine($"  [G3b network] trunks {Trunks} (wedges skipped {WedgesSkipped}), corridor halves {CorridorHalves} (lowest saddle crest {CorridorSaddleCrest.ToString("F0", c)} m), trunks skipped {TrunksSkipped}, trunks joining a trunk {TrunksJoined}, trunk km {trunkKm.ToString("F2", c)}; routed links {Links} ({linkKm.ToString("F2", c)} km, heads {LinkHeads}, into links {LinksIntoLinks}, into trunks {LinksIntoTrunks}, to the coast {LinksToCoast}), largest link area {(LargestLinkArea / 1e6f).ToString("F3", c)} km2; designed Strahler order {DesignedOrder} ({DesignedOrderAtChannelArea} counting only links that drain the 30 000 m2 channel area); designed divide cells {(DesignedDivides != null ? DesignedDivides.Count : 0)}; trunk descent fallback steps {StuckSteps}");
                sb.AppendLine("  [G3b trunks]" + trunkLines);
            }
            for (var b = 1; b < Ridges.Count; b++)
            {
                if (Ridges[b].Kind == 1)
                {
                    sb.AppendLine($"  [G3 branch {b}] parent {Ridges[b].Parent} at {Ridges[b].ParentArc.ToString("F0", c)} m, length {Ridges[b].Length.ToString("F0", c)} m, root crest {Ridges[b].Crest[0].ToString("F0", c)} m");
                }
            }
        }
    }

    public sealed class G3Composition
    {
        public float GateMeters;
        public int Clamped;
        public int OceanChanged;
        public int MaskChanged;
        public float GateLossShare;
        public float MountainShare;
        public int IncisedCells;
        public float MaxIncision;

        public void Append(StringBuilder sb, CultureInfo c, string id) =>
            sb.AppendLine($"  [{id}] gate {GateMeters} m: S removed by the coast gate {(GateLossShare * 100f).ToString("F1", c)} % of its land volume; land with gated S > plate step (18 m) {(MountainShare * 100f).ToString("F1", c)} %; ocean samples changed {OceanChanged}; land-mask cells changed {MaskChanged}; clamped {Clamped}; incised below the floor surface on {IncisedCells} cells, max {MaxIncision.ToString("F2", c)} m");
    }

    /// <summary>G3-specific checks on the candidate's own declared structure (G0 measures production's).</summary>
    public sealed class G3Diagnostics
    {
        public float CrestNear, SpurNear, CrestMeters, SpurMeters;
        public float ReachInValley;
        public int Valleys, ValleysClimbing;
        public float WorstValleyClimb;
        public int SpineSummits, SpineCols;
        public float SpineDeepestCol;
        public float CrestCatchment, SpurCatchment, CatchmentControl;

        // Lowest crossing of the spine line between its outermost culminations (G3b corridor reading):
        // on the candidate, on its floor surface, and on production H4A along the same line.
        public float DivideControl = float.NaN;
        public float SpineLowest = float.NaN, SpineLowestAboveFloor = float.NaN, SpineLowestH4A = float.NaN;
        public float CorridorColHeight = float.NaN;
        public readonly List<string> Details = new List<string>();

        public const float QualifyingClimbMeters = 0.25f;
        public const float AlongSummitProminenceMeters = 30f;  // = GeographyMetrics.SummitProminenceMeters
        public const float AlongColDepthMeters = 15f;          // = GeographyMetrics.SaddleProminenceMeters

        public static G3Diagnostics Measure(G3MountainSystem sys, GeographyAnalysis a, float[] h, IslandReliefWorld w)
        {
            var d = new G3Diagnostics();
            var n = a.N;
            var near = GeographyGrid.SquaredDistance(a.DivideMask, n);
            var limit = GeographyMetrics.DeclaredDivideSteps * GeographyMetrics.DeclaredDivideSteps;
            double crestIn = 0, crestAll = 0, spurIn = 0, spurAll = 0;
            foreach (var ridge in sys.Ridges)
            {
                var pts = ridge.Points;
                for (var k = 0; k + 1 < pts.Length; k++)
                {
                    var length = (pts[k + 1] - pts[k]).magnitude;
                    var samples = Mathf.Max(1, Mathf.CeilToInt(length / (a.Cell * 0.5f)));
                    var piece = length / samples;
                    for (var s = 0; s < samples; s++)
                    {
                        var p = Vector2.Lerp(pts[k], pts[k + 1], (s + 0.5f) / samples);
                        var x = Mathf.RoundToInt(p.x / a.Cell);
                        var z = Mathf.RoundToInt(p.y / a.Cell);
                        if (x < 0 || z < 0 || x >= n || z >= n || !a.Land[z * n + x])
                        {
                            continue;
                        }

                        var hit = near[z * n + x] <= limit ? piece : 0f;
                        if (ridge.Kind == 2)
                        {
                            spurAll += piece;
                            spurIn += hit;
                        }
                        else
                        {
                            crestAll += piece;
                            crestIn += hit;
                        }
                    }
                }
            }

            d.CrestMeters = (float)crestAll;
            d.SpurMeters = (float)spurAll;
            d.CrestNear = crestAll > 0 ? (float)(crestIn / crestAll) : float.NaN;
            d.SpurNear = spurAll > 0 ? (float)(spurIn / spurAll) : float.NaN;

            var crestLines = new List<Vector2[]>();
            var spurLines = new List<Vector2[]>();
            foreach (var ridge in sys.Ridges)
            {
                (ridge.Kind == 2 ? spurLines : crestLines).Add(ridge.Points);
            }

            // G3b has no spur lines: its side divides are the designed divides of the routed tree.
            if (sys.DesignedDivides != null)
            {
                spurLines = sys.DesignedDivides;
                d.SpurNear = G3Catchments.ShareNear(a, a.DivideMask, spurLines);
                d.SpurMeters = 0f;
                foreach (var segment in spurLines)
                {
                    d.SpurMeters += 2f * (segment[1] - segment[0]).magnitude; // one coarse cell each
                }
            }

            var boundary = G3Catchments.Boundary(a, out d.CatchmentControl);
            d.CrestCatchment = G3Catchments.ShareNear(a, boundary, crestLines);
            d.SpurCatchment = G3Catchments.ShareNear(a, boundary, spurLines);
            d.DivideControl = G3Catchments.LandShareNear(a, a.DivideMask);

            // River nodes inside a declared G3 valley half-width (G0's D-e rule, on G3's own valleys).
            d.ReachInValley = float.NaN;
            if (a.Hydrology.Succeeded && sys.Valleys.Count > 0)
            {
                var nodes = 0;
                var inside = 0;
                foreach (var reach in a.Hydrology.Definition.Reaches)
                {
                    foreach (var node in reach.Nodes)
                    {
                        nodes++;
                        foreach (var v in sys.Valleys)
                        {
                            var cum = G3MountainSystem.Cumulative(v.Points);
                            if (G3MountainSystem.ClosestOn(v.Points, cum, node.PositionXZ, out _, out _, out _) <= v.HalfWidth)
                            {
                                inside++;
                                break;
                            }
                        }
                    }
                }

                d.ReachInValley = nodes > 0 ? inside / (float)nodes : float.NaN;
            }

            // Declared valley axes on the composed lattice: largest climb above the running minimum.
            d.Valleys = sys.Valleys.Count;
            foreach (var v in sys.Valleys)
            {
                var cum = G3MountainSystem.Cumulative(v.Points);
                var total = cum[cum.Length - 1];
                var running = float.PositiveInfinity;
                var climb = 0f;
                var dipArc = 0f;
                var runningArc = 0f;
                var climbArc = 0f;
                for (var s = 0f; s <= total; s += 5f)
                {
                    var p = PointAt(v.Points, cum, s);
                    var height = sys.Bilinear(h, p);
                    if (height < running)
                    {
                        running = height;
                        runningArc = s;
                    }

                    if (height - running > climb)
                    {
                        climb = height - running;
                        dipArc = runningArc;
                        climbArc = s;
                    }
                }

                d.WorstValleyClimb = Mathf.Max(d.WorstValleyClimb, climb);
                d.ValleysClimbing += climb > QualifyingClimbMeters ? 1 : 0;
                if (climb > QualifyingClimbMeters)
                {
                    // Own floor at the dip and at the top of the climb: a climb with a flat own floor comes
                    // from another valley's deeper carve at the dip; a rising own floor from the trough bound.
                    var ownDip = v.FloorAbs != null ? Interpolated(cum, v.FloorAbs, dipArc) : float.NaN;
                    var ownTop = v.FloorAbs != null ? Interpolated(cum, v.FloorAbs, climbArc) : float.NaN;
                    var heightDip = sys.Bilinear(h, PointAt(v.Points, cum, dipArc));
                    d.Details.Add(FormattableString.Invariant($"climbing axis v{sys.Valleys.IndexOf(v)} class {v.Class}: {climb:F2} m over {total:F0} m, {(v.JoinsInto >= 0 ? "joins v" + v.JoinsInto : "to the coast")}; dip at {dipArc:F0} m (surface {heightDip:F1}, own floor {ownDip:F1}), top at {climbArc:F0} m (own floor {ownTop:F1})"));
                }
            }

            // G3b realisation: designed drained area at each link's mouth against the D8 accumulation the
            // composed lattice actually gathers there (largest within 3 cells of the node above the mouth).
            var designedPresent = 0;
            var realisedPresent = 0;
            var ratios = new List<float>();
            foreach (var v in sys.Valleys)
            {
                if (v.Class != G3Valley.RoutedClass || v.Points.Length < 3)
                {
                    continue;
                }

                var designed = v.AreaNodes[v.AreaNodes.Length - 2];
                if (designed < a.ChannelAreaSqm)
                {
                    continue;
                }

                var q = v.Points[v.Points.Length - 2];
                var qx = Mathf.RoundToInt(q.x / a.Cell);
                var qz = Mathf.RoundToInt(q.y / a.Cell);
                var measuredArea = 0f;
                for (var dz = -3; dz <= 3; dz++)
                {
                    for (var dx = -3; dx <= 3; dx++)
                    {
                        var x = qx + dx;
                        var z = qz + dz;
                        if (x >= 0 && z >= 0 && x < n && z < n && a.Land[z * n + x])
                        {
                            measuredArea = Mathf.Max(measuredArea, a.Accumulation[z * n + x]);
                        }
                    }
                }

                designedPresent++;
                realisedPresent += measuredArea >= a.ChannelAreaSqm ? 1 : 0;
                ratios.Add(measuredArea / designed);
            }

            if (designedPresent > 0)
            {
                ratios.Sort();
                d.Details.Add(FormattableString.Invariant($"realisation: {designedPresent} links drain >= {a.ChannelAreaSqm:F0} m2 by design, {realisedPresent} gather it on the lattice; measured / designed area at the mouth: min {ratios[0]:F2}, median {ratios[ratios.Count / 2]:F2}, max {ratios[ratios.Count - 1]:F2}"));
            }

            // Where every S8 depression sits relative to the declared valleys.
            var found = new List<IslandReliefMetrics.DepressionInfo>();
            IslandReliefMetrics.ScanDepressions(h, w, out _, out _, out _, out _, out _, found);
            foreach (var pit in found)
            {
                var p = new Vector2((pit.DeepestIndex % n) * a.Cell, (pit.DeepestIndex / n) * a.Cell);
                var nearest = -1;
                var nearestDistance = float.PositiveInfinity;
                var nearestArc = 0f;
                for (var v = 0; v < sys.Valleys.Count; v++)
                {
                    var valley = sys.Valleys[v];
                    var dist = G3MountainSystem.ClosestOn(valley.Points, valley.Cumulative, p, out var arc, out _, out _);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        nearest = v;
                        nearestArc = arc;
                    }
                }

                var cls = nearest >= 0 ? sys.Valleys[nearest].Class : -1;
                var len = nearest >= 0 ? sys.Valleys[nearest].Cumulative[sys.Valleys[nearest].Cumulative.Length - 1] : 0f;
                d.Details.Add(FormattableString.Invariant($"S8 depression at ({p.x:F0}, {p.y:F0}) depth {pit.DepthMeters:F2} m area {pit.AreaSqm:F0} m2; nearest valley v{nearest} class {cls} at {nearestDistance:F0} m, arc {nearestArc:F0} of {len:F0} m"));
            }

            // Culminations and cols realised along the declared spine on the composed lattice.
            if (sys.Ridges.Count > 0)
            {
                var spine = sys.Ridges[0];
                var total = spine.Length;
                var profile = new List<float>();
                for (var s = 0f; s <= total; s += 10f)
                {
                    profile.Add(sys.Bilinear(h, PointAt(spine.Points, spine.Cumulative, s)));
                }

                CountAlong(profile, out d.SpineSummits, out d.SpineCols, out d.SpineDeepestCol);

                var first = float.PositiveInfinity;
                var last = float.NegativeInfinity;
                foreach (var m in sys.Marks)
                {
                    if (!m.IsSaddle)
                    {
                        first = Mathf.Min(first, m.Arc);
                        last = Mathf.Max(last, m.Arc);
                    }
                }

                if (last > first)
                {
                    var u2 = w.Stage2B.UpliftField.Heights;
                    var lowestH4A = float.PositiveInfinity;
                    d.SpineLowest = float.PositiveInfinity;
                    for (var s = first; s <= last; s += 5f)
                    {
                        var p = PointAt(spine.Points, spine.Cumulative, s);
                        var height = sys.Bilinear(h, p);
                        lowestH4A = Mathf.Min(lowestH4A, sys.Bilinear(w.H4A, p));
                        if (height < d.SpineLowest)
                        {
                            d.SpineLowest = height;
                            d.SpineLowestAboveFloor = height - (sys.Bilinear(w.H4A, p) - sys.Bilinear(u2, p) - sys.Bilinear(w.Apron, p));
                        }
                    }

                    d.SpineLowestH4A = lowestH4A;
                }
            }

            if (sys.FloorSurface != null && !float.IsNaN(sys.CorridorSaddleCrest))
            {
                d.CorridorColHeight = sys.Bilinear(h, sys.CorridorCol);
            }

            return d;
        }

        /// <summary>
        /// Peaks along a 1-D profile whose drop to the next higher peak (or the profile end) on both
        /// sides is at least the summit prominence, and the cols between them at least the col depth
        /// below the lower neighbour.
        /// </summary>
        private static void CountAlong(List<float> p, out int summits, out int cols, out float deepest)
        {
            summits = 0;
            cols = 0;
            deepest = 0f;
            var peaks = new List<int>();
            for (var i = 0; i < p.Count; i++)
            {
                var left = float.PositiveInfinity;
                var leftHigher = false;
                for (var j = i - 1; j >= 0; j--)
                {
                    left = Mathf.Min(left, p[j]);
                    if (p[j] > p[i])
                    {
                        leftHigher = true;
                        break;
                    }
                }

                var right = float.PositiveInfinity;
                var rightHigher = false;
                for (var j = i + 1; j < p.Count; j++)
                {
                    right = Mathf.Min(right, p[j]);
                    if (p[j] > p[i])
                    {
                        rightHigher = true;
                        break;
                    }
                }

                var leftDrop = p[i] - (i == 0 ? p[i] : left);
                var rightDrop = p[i] - (i == p.Count - 1 ? p[i] : right);
                var prominence = (leftHigher, rightHigher) switch
                {
                    (true, true) => Mathf.Max(leftDrop, rightDrop) > 0 ? Mathf.Min(leftDrop, rightDrop) : 0f,
                    (true, false) => leftDrop,
                    (false, true) => rightDrop,
                    _ => float.PositiveInfinity,
                };

                if (prominence >= AlongSummitProminenceMeters && i > 0 && i < p.Count - 1 && p[i] >= p[i - 1] && p[i] >= p[i + 1])
                {
                    peaks.Add(i);
                }
            }

            summits = peaks.Count;
            for (var k = 0; k + 1 < peaks.Count; k++)
            {
                var low = float.PositiveInfinity;
                for (var j = peaks[k]; j <= peaks[k + 1]; j++)
                {
                    low = Mathf.Min(low, p[j]);
                }

                var depth = Mathf.Min(p[peaks[k]], p[peaks[k + 1]]) - low;
                if (depth >= AlongColDepthMeters)
                {
                    cols++;
                    deepest = Mathf.Max(deepest, depth);
                }
            }
        }

        private static float Interpolated(float[] cum, float[] values, float s)
        {
            for (var i = 1; i < cum.Length; i++)
            {
                if (s <= cum[i])
                {
                    var t = (s - cum[i - 1]) / Mathf.Max(1e-6f, cum[i] - cum[i - 1]);
                    return Mathf.Lerp(values[i - 1], values[i], t);
                }
            }

            return values[values.Length - 1];
        }

        public static Vector2 PointAt(Vector2[] pts, float[] cum, float s)
        {
            for (var i = 1; i < cum.Length; i++)
            {
                if (s <= cum[i])
                {
                    var t = (s - cum[i - 1]) / Mathf.Max(1e-6f, cum[i] - cum[i - 1]);
                    return Vector2.Lerp(pts[i - 1], pts[i], t);
                }
            }

            return pts[pts.Length - 1];
        }

        public void Append(StringBuilder sb, CultureInfo c, string id)
        {
            sb.AppendLine($"  [{id}] declared-divide fit (D-a rule on G3's own lines): crest+branches {Pct(CrestNear, c)} of {(CrestMeters / 1000f).ToString("F2", c)} km, spurs (G3b: designed divides of the routed tree) {Pct(SpurNear, c)} of {(SpurMeters / 1000f).ToString("F2", c)} km; control: land within the same distance of a flow divide {Pct(DivideControl, c)}");
            sb.AppendLine($"  [{id}] declared-divide fit (catchment boundaries, G3 diagnostic): crest+branches {Pct(CrestCatchment, c)}, spurs {Pct(SpurCatchment, c)}; control: land within the same distance of a boundary {Pct(CatchmentControl, c)}");
            sb.AppendLine($"  [{id}] river nodes inside a G3 valley half-width (D-e rule): {Pct(ReachInValley, c)}; valley axes climbing > {QualifyingClimbMeters} m: {ValleysClimbing} of {Valleys}, worst {WorstValleyClimb.ToString("F2", c)} m");
            sb.AppendLine($"  [{id}] along the spine: summits with prominence >= {AlongSummitProminenceMeters} m {SpineSummits}, cols >= {AlongColDepthMeters} m {SpineCols} (deepest {SpineDeepestCol.ToString("F0", c)} m)");
            sb.AppendLine($"  [{id}] lowest crossing of the spine line between its outer culminations: {SpineLowest.ToString("F1", c)} m ({SpineLowestAboveFloor.ToString("F1", c)} m above the floor surface H4A - U2 - A); H4A along the same line {SpineLowestH4A.ToString("F1", c)} m; corridor col {CorridorColHeight.ToString("F1", c)} m");
            foreach (var line in Details)
            {
                sb.AppendLine($"  [{id}]   " + line);
            }
        }

        public string SummaryLine(CultureInfo c, int seed, G3Composition comp) =>
            $"seed={seed} g3: crestFit={Pct(CrestNear, c)} spurFit={Pct(SpurNear, c)} divControl={Pct(DivideControl, c)} crestCatch={Pct(CrestCatchment, c)} spurCatch={Pct(SpurCatchment, c)} control={Pct(CatchmentControl, c)} reachInValley={Pct(ReachInValley, c)} valleysClimbing={ValleysClimbing}/{Valleys} worstClimb={WorstValleyClimb.ToString("F2", c)} spineSummits={SpineSummits} spineCols={SpineCols} spineLow={SpineLowest.ToString("F0", c)} spineLowS={SpineLowestAboveFloor.ToString("F0", c)} spineLowH4A={SpineLowestH4A.ToString("F0", c)} gateLoss={(comp.GateLossShare * 100f).ToString("F0", c)}% mountain={(comp.MountainShare * 100f).ToString("F0", c)}% mask={comp.MaskChanged} ocean={comp.OceanChanged}";

        private static string Pct(float v, CultureInfo c) => float.IsNaN(v) ? "n/a" : (v * 100f).ToString("F0", c) + "%";
    }

    /// <summary>
    /// G3 diagnostic: divides as boundaries between outlet catchments. Every land cell is labelled by
    /// the ocean cell its D8 path (G0's, on the filled surface) ends in; a boundary cell has a 4-neighbour
    /// in a different catchment, both catchments at least the channel-initiation area. Not a G0
    /// definition: G0's flow-divide rule is cell-local and misses spur crests that descend steeply.
    /// </summary>
    public static class G3Catchments
    {
        public static bool[] Boundary(GeographyAnalysis a, out float control)
        {
            var n = a.N;
            var count = n * n;
            var label = new int[count];
            for (var i = 0; i < count; i++)
            {
                label[i] = -2;
            }

            var path = new List<int>();
            for (var start = 0; start < count; start++)
            {
                if (!a.Land[start] || label[start] != -2)
                {
                    continue;
                }

                path.Clear();
                var c = start;
                var result = -1;
                for (var guard = 0; guard < count; guard++)
                {
                    if (!a.Land[c])
                    {
                        result = c;
                        break;
                    }

                    if (label[c] != -2)
                    {
                        result = label[c];
                        break;
                    }

                    path.Add(c);
                    var d = a.Directions[c];
                    if (d == GameDev.World.Hydrology.FlowRouting.NoDirection)
                    {
                        result = -1;
                        break;
                    }

                    var x = c % n + GameDev.World.Hydrology.HydrologyGrid.NeighborDx[d];
                    var z = c / n + GameDev.World.Hydrology.HydrologyGrid.NeighborDz[d];
                    if (x < 0 || z < 0 || x >= n || z >= n)
                    {
                        result = -1;
                        break;
                    }

                    c = z * n + x;
                }

                foreach (var q in path)
                {
                    label[q] = result;
                }
            }

            var area = new Dictionary<int, int>();
            for (var i = 0; i < count; i++)
            {
                if (a.Land[i] && label[i] >= 0)
                {
                    area.TryGetValue(label[i], out var v);
                    area[label[i]] = v + 1;
                }
            }

            var minCells = a.ChannelAreaSqm / (a.Cell * a.Cell);
            bool Big(int l) => l >= 0 && area.TryGetValue(l, out var v) && v >= minCells;
            var boundary = new bool[count];
            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (!a.Land[i] || !Big(label[i]))
                    {
                        continue;
                    }

                    foreach (var j in new[] { i + 1, i - 1, i + n, i - n })
                    {
                        if (a.Land[j] && label[j] != label[i] && Big(label[j]))
                        {
                            boundary[i] = true;
                            break;
                        }
                    }
                }
            }

            var near = GeographyGrid.SquaredDistance(boundary, n);
            var limit = GeographyMetrics.DeclaredDivideSteps * GeographyMetrics.DeclaredDivideSteps;
            var land = 0;
            var close = 0;
            for (var i = 0; i < count; i++)
            {
                if (a.Land[i])
                {
                    land++;
                    close += near[i] <= limit ? 1 : 0;
                }
            }

            control = land > 0 ? close / (float)land : float.NaN;
            return boundary;
        }

        /// <summary>Control for <see cref="ShareNear"/>: the share of all land within the same distance of the mask.</summary>
        public static float LandShareNear(GeographyAnalysis a, bool[] mask)
        {
            var near = GeographyGrid.SquaredDistance(mask, a.N);
            var limit = GeographyMetrics.DeclaredDivideSteps * GeographyMetrics.DeclaredDivideSteps;
            var land = 0;
            var close = 0;
            for (var i = 0; i < near.Length; i++)
            {
                if (a.Land[i])
                {
                    land++;
                    close += near[i] <= limit ? 1 : 0;
                }
            }

            return land > 0 ? close / (float)land : float.NaN;
        }

        public static float ShareNear(GeographyAnalysis a, bool[] boundary, List<Vector2[]> lines)
        {
            var n = a.N;
            var near = GeographyGrid.SquaredDistance(boundary, n);
            var limit = GeographyMetrics.DeclaredDivideSteps * GeographyMetrics.DeclaredDivideSteps;
            double all = 0, hit = 0;
            foreach (var pts in lines)
            {
                for (var k = 0; k + 1 < pts.Length; k++)
                {
                    var length = (pts[k + 1] - pts[k]).magnitude;
                    var samples = Mathf.Max(1, Mathf.CeilToInt(length / (a.Cell * 0.5f)));
                    var piece = length / samples;
                    for (var s = 0; s < samples; s++)
                    {
                        var p = Vector2.Lerp(pts[k], pts[k + 1], (s + 0.5f) / samples);
                        var x = Mathf.RoundToInt(p.x / a.Cell);
                        var z = Mathf.RoundToInt(p.y / a.Cell);
                        if (x < 0 || z < 0 || x >= n || z >= n || !a.Land[z * n + x])
                        {
                            continue;
                        }

                        all += piece;
                        hit += near[z * n + x] <= limit ? piece : 0;
                    }
                }
            }

            return all > 0 ? (float)(hit / all) : float.NaN;
        }
    }

    public static class G3Images
    {
        public static void WriteStructure(string path, G3MountainSystem sys, GeographyAnalysis a, float[] h)
        {
            var n = a.N;
            var pixels = GeographyImages.Hillshaded(h, n, a.Cell, a.Land);
            for (var i = 0; i < pixels.Length; i++)
            {
                if (a.Skeleton[i])
                {
                    pixels[i] = GeographyImages.Blend(pixels[i], new Color32(150, 30, 30, 255), 0.6f);
                }

                if (a.Channel[i])
                {
                    pixels[i] = new Color32(60, 110, 255, 255);
                }

                if (a.Land[i] && a.Filled[i] - h[i] > 0.05f)
                {
                    pixels[i] = new Color32(255, 120, 0, 255);
                }
            }

            if (a.Hydrology.Succeeded)
            {
                foreach (var reach in a.Hydrology.Definition.Reaches)
                {
                    var pts = new Vector2[reach.Nodes.Count];
                    for (var k = 0; k < pts.Length; k++)
                    {
                        pts[k] = reach.Nodes[k].PositionXZ;
                    }

                    GeographyImages.DrawPolyline(pixels, n, a.Cell, pts, new Color32(10, 40, 160, 255));
                }
            }

            foreach (var v in sys.Valleys)
            {
                var colour = v.Class == G3Valley.TrunkClass ? new Color32(0, 255, 120, 255)
                    : v.Class == G3Valley.CorridorClass ? new Color32(200, 255, 0, 255)
                    : v.Class == G3Valley.RoutedClass ? new Color32(0, 140, 255, 255)
                    : new Color32(0, 210, 220, 255);
                GeographyImages.DrawPolyline(pixels, n, a.Cell, v.Points, colour);
            }

            foreach (var r in sys.Ridges)
            {
                var colour = r.Kind == 0 ? new Color32(255, 20, 20, 255) : r.Kind == 1 ? new Color32(255, 140, 0, 255) : new Color32(220, 0, 220, 255);
                GeographyImages.DrawPolyline(pixels, n, a.Cell, r.Points, colour);
            }

            foreach (var m in sys.Marks)
            {
                var x = Mathf.RoundToInt(m.Position.x / a.Cell);
                var z = Mathf.RoundToInt(m.Position.y / a.Cell);
                GeographyImages.Disc(pixels, n, z * n + x, 6, m.IsSaddle ? new Color32(255, 230, 0, 255) : new Color32(255, 255, 255, 255));
            }

            GeographyImages.WritePng(path, pixels, n);

            // Flow accumulation, log scale from one cell to the channel-initiation area and beyond.
            var flow = GeographyImages.Hillshaded(h, n, a.Cell, a.Land);
            var cellArea = a.Cell * a.Cell;
            for (var i = 0; i < flow.Length; i++)
            {
                if (!a.Land[i])
                {
                    continue;
                }

                var t = Mathf.Clamp01(Mathf.Log10(Mathf.Max(1f, a.Accumulation[i] / cellArea)) / Mathf.Log10(4f * a.ChannelAreaSqm / cellArea));
                if (t > 0.35f)
                {
                    flow[i] = GeographyImages.Blend(flow[i], new Color32(0, 60, 255, 255), t);
                }
            }

            GeographyImages.WritePng(path.Replace("_structure.png", "_flow.png"), flow, n);
        }
    }

    /// <summary>Binary min-heap on (key, index) with the index as a deterministic tie-break.</summary>
    public sealed class G3Heap
    {
        private double[] keys;
        private int[] values;

        public G3Heap(int capacity)
        {
            keys = new double[Mathf.Max(16, capacity)];
            values = new int[keys.Length];
        }

        public int Count { get; private set; }

        public void Push(double key, int value)
        {
            if (Count == keys.Length)
            {
                Array.Resize(ref keys, keys.Length * 2);
                Array.Resize(ref values, values.Length * 2);
            }

            var i = Count++;
            keys[i] = key;
            values[i] = value;
            while (i > 0)
            {
                var parent = (i - 1) / 2;
                if (!Less(i, parent))
                {
                    break;
                }

                Swap(i, parent);
                i = parent;
            }
        }

        public void Pop(out double key, out int value)
        {
            key = keys[0];
            value = values[0];
            Count--;
            keys[0] = keys[Count];
            values[0] = values[Count];
            var i = 0;
            while (true)
            {
                var l = 2 * i + 1;
                var r = l + 1;
                var m = i;
                if (l < Count && Less(l, m))
                {
                    m = l;
                }

                if (r < Count && Less(r, m))
                {
                    m = r;
                }

                if (m == i)
                {
                    break;
                }

                Swap(i, m);
                i = m;
            }
        }

        private bool Less(int a, int b) => keys[a] < keys[b] || (keys[a] == keys[b] && values[a] < values[b]);

        private void Swap(int a, int b)
        {
            (keys[a], keys[b]) = (keys[b], keys[a]);
            (values[a], values[b]) = (values[b], values[a]);
        }
    }
}
#endif
