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
    /// Geography metrics baseline (G0 of <c>GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md</c> §9): the §7 metrics
    /// M1–M6, D-a–D-e and T-a measured on the production height <c>H4A</c>, per seed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only. The world comes from <see cref="IslandReliefWorld"/> — the dump lattice path whose
    /// <c>H4A</c> hash equals the production authority's — and the drainage from the hydrology package
    /// (<see cref="DepressionFill"/>, <see cref="FlowRouting"/>, <see cref="HydrologyPlanner"/>), which
    /// is run on a copy of the lattice and never carves anything. Nothing here writes a
    /// <c>TerrainData</c>, opens a scene or saves an asset; output goes outside <c>Assets</c>.
    /// </para>
    /// <para>
    /// Every window, threshold and area in the metric definitions is an <b>authoring default</b> of
    /// the research record, not a measured or approved value; the report prints them. No metric is a
    /// gate: GEO-7 decides which become one.
    /// </para>
    /// <para>
    /// <c>geography_report.txt</c> and <c>geography_metrics.json</c> are deterministic and comparable
    /// byte for byte between processes; timings go to <c>geography_timings.txt</c> only.
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.TerrainGenGeographyDump.DumpBatch \
    ///   [-geoSeeds a,b] [-geoSweep 1000:40] [-geoOut &lt;folder outside Assets&gt;]
    /// </code>
    /// </remarks>
    public static class TerrainGenGeographyDump
    {
        private const string OutputArg = "-geoOut";
        private const string SeedsArg = "-geoSeeds";
        private const string SweepArg = "-geoSweep";

        /// <summary>
        /// <c>H4A</c> lattice hashes pinned in <c>OVERWORLD_TECH_DEBT.md</c> item 19 (Stage 4A R1,
        /// <c>GeneratorVersion</c> 2). A mismatch means the dump is not measuring production.
        /// </summary>
        public static readonly Dictionary<int, string> PinnedH4AHashes = new Dictionary<int, string>
        {
            { 110201, "e2bad571f4af76c7" },
            { 424242, "2076ecf78a34483b" },
            { 20260905, "4b7ae0f5a6cfaae8" },
            { 7, "6a7ad6edd08db7d9" },
            { 1337, "f86e8c15d7105be2" },
            { 256, "a218e07fe1b2cd74" },
            { 777, "48012b8c997a54df" },
        };

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Geography Metrics G0 (Canonical Seeds)", false, 230)]
        public static void DumpMenu()
        {
            Debug.Log(Run(
                TerrainGenIslandReliefDump.CanonicalSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "geography"),
                true));
        }

        /// <summary>Batch entry; see the type remarks for arguments.</summary>
        public static void DumpBatch()
        {
            var args = System.Environment.GetCommandLineArgs();
            var directory = ReadArg(args, OutputArg);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "geography");
            }

            var sweep = ReadArg(args, SweepArg);
            if (!string.IsNullOrEmpty(sweep))
            {
                Debug.Log(Run(ParseSweep(sweep), directory, false));
                return;
            }

            Debug.Log(Run(ParseSeeds(ReadArg(args, SeedsArg)), directory, true));
        }

        public static string Run(int[] seeds, string directory, bool images)
        {
            Directory.CreateDirectory(directory);
            var c = CultureInfo.InvariantCulture;
            var report = new StringBuilder();
            var json = new StringBuilder();
            var timings = new StringBuilder();
            var summary = new StringBuilder();

            report.AppendLine("Geography metrics baseline G0 - GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md section 7, measured on production H4A");
            report.AppendLine("  surface: H4A = H1 + U2 + R4A on the 1025 x 1025 dump lattice (IslandReliefWorld, IslandReliefSettings.Default)");
            report.AppendLine("  read-only: hydrology package on a copy of the lattice; no TerrainData, scene or asset is written");
            report.AppendLine("  every window, threshold and area below is an AUTHORING DEFAULT of the research record; no metric is a gate (GEO-7)");
            foreach (var line in GeographyMetrics.DefinitionLines())
            {
                report.AppendLine("  " + line);
            }

            report.AppendLine();
            json.Append("{\"seeds\":[");
            timings.AppendLine("seed total_ms world_ms analysis_ms metrics_ms");

            for (var s = 0; s < seeds.Length; s++)
            {
                var seed = seeds[s];
                var watch = Stopwatch.StartNew();
                var world = IslandReliefWorld.Build(seed, IslandReliefSettings.Default, withStage3: true);
                var worldMs = watch.ElapsedMilliseconds;

                watch.Restart();
                var analysis = GeographyAnalysis.Build(world.H4A, world.Width, world.Cell, seed);
                var analysisMs = watch.ElapsedMilliseconds;

                watch.Restart();
                var m = GeographyMetrics.Measure(world, analysis);
                var metricsMs = watch.ElapsedMilliseconds;

                m.AppendReport(report, c);
                summary.AppendLine(m.SummaryLine(c));
                json.Append(s == 0 ? string.Empty : ",").Append(m.ToJson(c));
                timings.AppendLine($"{seed} {worldMs + analysisMs + metricsMs} {worldMs} {analysisMs} {metricsMs}");

                if (images)
                {
                    GeographyImages.WriteStructure(Path.Combine(directory, $"geo_{seed}_structure.png"), world, analysis, m);
                }
            }

            json.Append("]}");
            report.AppendLine("one line per seed:");
            report.Append(summary);

            File.WriteAllText(Path.Combine(directory, "geography_report.txt"), report.ToString());
            File.WriteAllText(Path.Combine(directory, "geography_metrics.json"), json.ToString());
            File.WriteAllText(Path.Combine(directory, "geography_timings.txt"), timings.ToString());
            return report.ToString();
        }

        private static int[] ParseSeeds(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return TerrainGenIslandReliefDump.CanonicalSeeds;
            }

            var seeds = new List<int>();
            foreach (var part in raw.Split(','))
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    seeds.Add(value);
                }
            }

            return seeds.Count > 0 ? seeds.ToArray() : TerrainGenIslandReliefDump.CanonicalSeeds;
        }

        /// <summary><c>start:count</c>, default 1000:40 — the sweep every earlier stage used.</summary>
        private static int[] ParseSweep(string spec)
        {
            var start = 1000;
            var count = 40;
            var parts = spec.Split(':');
            if (parts.Length >= 1)
            {
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out start);
            }

            if (parts.Length >= 2)
            {
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out count);
            }

            var seeds = new int[Mathf.Max(1, count)];
            for (var i = 0; i < seeds.Length; i++)
            {
                seeds[i] = start + i;
            }

            return seeds;
        }

        internal static string ReadArg(string[] args, string name)
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

    /// <summary>
    /// Stage 1 anchors for comparison views: the mainland coast polygon's centroid and equal-area
    /// radius. Frozen and golden-hashed, so cameras anchored here do not move with the relief.
    /// </summary>
    public readonly struct GeographyAnchors
    {
        public readonly Vector2 Centroid;
        public readonly float EqualAreaRadius;
        public readonly float MainlandAreaSqm;

        private GeographyAnchors(Vector2 centroid, float radius, float area)
        {
            Centroid = centroid;
            EqualAreaRadius = radius;
            MainlandAreaSqm = area;
        }

        public static GeographyAnchors From(Stage1Result stage1)
        {
            var rings = stage1.Structure.CoastRings;
            if (rings.Count == 0 || rings[0] == null || rings[0].Length < 3)
            {
                var half = WorldScale.WorldExtentXMeters * 0.5f;
                return new GeographyAnchors(new Vector2(half, half), 0f, 0f);
            }

            // Shoelace centroid in double precision: the ring has thousands of vertices.
            var ring = rings[0];
            double a = 0, cx = 0, cz = 0;
            for (var i = 0; i < ring.Length; i++)
            {
                var p = ring[i];
                var q = ring[(i + 1) % ring.Length];
                var cross = (double)p.x * q.y - (double)q.x * p.y;
                a += cross;
                cx += (p.x + q.x) * cross;
                cz += (p.y + q.y) * cross;
            }

            a *= 0.5;
            var centroid = new Vector2((float)(cx / (6.0 * a)), (float)(cz / (6.0 * a)));
            var area = (float)Math.Abs(a);
            return new GeographyAnchors(centroid, Mathf.Sqrt(area / Mathf.PI), area);
        }

        /// <summary>
        /// The point at <paramref name="bearingDegrees"/> (clockwise from +Z, north) and
        /// <paramref name="distanceMeters"/> from the centroid, walked back toward the centroid until it
        /// lies on Stage 1 land when <paramref name="requireLand"/>.
        /// </summary>
        public Vector2 PointAt(float bearingDegrees, float distanceMeters, Stage1Result stage1, bool requireLand)
        {
            var b = bearingDegrees * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Sin(b), Mathf.Cos(b));
            if (!requireLand)
            {
                return Centroid + dir * distanceMeters;
            }

            for (var d = distanceMeters; d >= 0f; d -= 1f)
            {
                var p = Centroid + dir * d;
                if (Stage1SignedDistance(stage1, p) > 0f)
                {
                    return p;
                }
            }

            return Centroid;
        }

        /// <summary>Nearest-sample Stage 1 signed coast distance (positive on land).</summary>
        public static float Stage1SignedDistance(Stage1Result stage1, Vector2 p)
        {
            var field = stage1.Field;
            var x = Mathf.Clamp(Mathf.RoundToInt((p.x - field.OriginX) / field.CellSize), 0, field.Width - 1);
            var z = Mathf.Clamp(Mathf.RoundToInt((p.y - field.OriginZ) / field.CellSize), 0, field.Height - 1);
            return stage1.SignedDistance[z * field.Width + x];
        }
    }

    /// <summary>
    /// Drainage and divide products of one lattice, shared by the G0 metrics and the G1 drainage view.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The surface convention is the hydrology package's: priority flood (ε 1 mm) from the
    /// border-connected sea, 8-connected D8 on the filled surface, contributing area in m². Channels
    /// are cells at or above <see cref="HydrologySettings.ChannelInitiationAreaSqm"/> of the shipping
    /// settings.
    /// </para>
    /// <para>
    /// <b>Divides</b> are read off the same D8 field: a land cell is a <i>flow divide</i> when, along
    /// one of the four lattice axes, both neighbours drain away from it (their D8 direction within 45°
    /// of pointing away). That is where water parts, whatever the size of the basins on either side —
    /// on smooth relief D8 flow barely converges, so basin-boundary readings that need channels miss
    /// most of the parting lines. The divide mask is thinned (Zhang–Suen) to a one-cell skeleton,
    /// which carries the length, connectivity and branching. It is defined by the D8 field alone and
    /// knows nothing about declared structure.
    /// </para>
    /// </remarks>
    public sealed class GeographyAnalysis
    {
        public int N { get; private set; }
        public float Cell { get; private set; }
        public float[] Heights { get; private set; }
        public bool[] Land { get; private set; }
        public bool[] Ocean { get; private set; }
        public float[] Filled { get; private set; }
        public byte[] Directions { get; private set; }
        public float[] Accumulation { get; private set; }
        public bool[] Channel { get; private set; }
        public int ChannelCells { get; private set; }
        public float ChannelAreaSqm { get; private set; }
        public int ProcessedCells { get; private set; }

        /// <summary>Flow-divide cells (see remarks).</summary>
        public bool[] DivideMask { get; private set; }

        /// <summary>One-cell skeleton of <see cref="DivideMask"/>.</summary>
        public bool[] Skeleton { get; private set; }

        /// <summary>8-connected skeleton component per cell, or -1.</summary>
        public int[] SkeletonComponent { get; private set; }

        /// <summary>Length of each skeleton component in metres.</summary>
        public List<float> ComponentLengthMeters { get; private set; }

        public float SkeletonLengthMeters { get; private set; }

        /// <summary>The longest skeleton component — the main divide — or -1.</summary>
        public int MainComponent { get; private set; }

        /// <summary>Skeleton cells with three or more branches leaving them (crossing number ≥ 3).</summary>
        public bool[] BranchMask { get; private set; }

        /// <summary>Height minus the box mean over 20 m — the production convexity reading.</summary>
        public float[] Convexity { get; private set; }

        public HydrologyPlanner.Result Hydrology { get; private set; }

        public static GeographyAnalysis Build(float[] heights, int n, float cell, int seed)
        {
            var a = new GeographyAnalysis { N = n, Cell = cell, Heights = heights };
            var sea = WorldScale.SeaLevelMeters;
            var count = n * n;
            var settings = HydrologySettings.Default;
            a.ChannelAreaSqm = settings.ChannelInitiationAreaSqm;

            var grid = new HydrologyGrid(0f, 0f, n, n, cell, (float[])heights.Clone(), sea);
            a.Ocean = grid.BuildOceanMask();
            a.Filled = DepressionFill.Fill(grid, a.Ocean);
            a.Directions = FlowRouting.ComputeD8(grid, a.Filled, a.Ocean);
            a.Accumulation = FlowRouting.Accumulate(grid, a.Directions, a.Ocean, out var processed);
            a.ProcessedCells = processed;

            a.Land = new bool[count];
            a.Channel = new bool[count];
            for (var i = 0; i < count; i++)
            {
                a.Land[i] = heights[i] > sea;
                a.Channel[i] = a.Land[i] && a.Accumulation[i] >= a.ChannelAreaSqm;
                if (a.Channel[i])
                {
                    a.ChannelCells++;
                }
            }

            a.DivideMask = a.FlowDivides();
            a.Skeleton = GeographyGrid.Thin(a.DivideMask, n);
            a.MeasureSkeleton();
            a.Convexity = BoxConvexity(heights, n, Mathf.Max(1, Mathf.RoundToInt(20f / cell)));

            var planGrid = new HydrologyGrid(0f, 0f, n, n, cell, (float[])heights.Clone(), sea);
            a.Hydrology = HydrologyPlanner.Plan(planGrid, settings, new GameDev.World.Seed.WorldSeed(seed));
            return a;
        }

        private static readonly int[] AxisDx = { 1, 0, 1, 1 };
        private static readonly int[] AxisDz = { 0, 1, 1, -1 };

        private bool[] FlowDivides()
        {
            var mask = new bool[N * N];
            for (var z = 1; z < N - 1; z++)
            {
                for (var x = 1; x < N - 1; x++)
                {
                    var i = z * N + x;
                    if (!Land[i])
                    {
                        continue;
                    }

                    for (var axis = 0; axis < 4 && !mask[i]; axis++)
                    {
                        var dx = AxisDx[axis];
                        var dz = AxisDz[axis];
                        var b = (z + dz) * N + x + dx;
                        var a = (z - dz) * N + x - dx;
                        mask[i] = Land[a] && Land[b] && DrainsAlong(a, -dx, -dz) && DrainsAlong(b, dx, dz);
                    }
                }
            }

            return mask;
        }

        /// <summary>True when the D8 direction at <paramref name="i"/> is within 45° of (dx, dz).</summary>
        private bool DrainsAlong(int i, int dx, int dz)
        {
            var d = Directions[i];
            if (d == FlowRouting.NoDirection)
            {
                return false;
            }

            var fx = HydrologyGrid.NeighborDx[d];
            var fz = HydrologyGrid.NeighborDz[d];
            var dot = fx * dx + fz * dz;
            var norm = Mathf.Sqrt((fx * fx + fz * fz) * (dx * dx + dz * dz));
            return dot / norm > 0.5f;
        }

        private void MeasureSkeleton()
        {
            var n = N;
            SkeletonComponent = GeographyGrid.Components8(Skeleton, n, out var components);
            ComponentLengthMeters = new List<float>(components);
            var length = new double[components];
            BranchMask = new bool[n * n];

            for (var z = 1; z < n - 1; z++)
            {
                for (var x = 1; x < n - 1; x++)
                {
                    var i = z * n + x;
                    if (!Skeleton[i])
                    {
                        continue;
                    }

                    // Forward links only (E, N, NE, NW), so every link counts once; a diagonal is
                    // skipped where an orthogonal path through a shared neighbour already joins the pair.
                    var e = Skeleton[i + 1];
                    var north = Skeleton[i + n];
                    var west = Skeleton[i - 1];
                    var sum = 0.0;
                    if (e)
                    {
                        sum += 1.0;
                    }

                    if (north)
                    {
                        sum += 1.0;
                    }

                    if (Skeleton[i + n + 1] && !e && !north)
                    {
                        sum += 1.41421356;
                    }

                    if (Skeleton[i + n - 1] && !west && !north)
                    {
                        sum += 1.41421356;
                    }

                    length[SkeletonComponent[i]] += sum * Cell;
                    BranchMask[i] = GeographyGrid.CrossingNumber(Skeleton, n, x, z) >= 3;
                }
            }

            MainComponent = -1;
            var best = 0.0;
            SkeletonLengthMeters = 0f;
            for (var k = 0; k < components; k++)
            {
                ComponentLengthMeters.Add((float)length[k]);
                SkeletonLengthMeters += (float)length[k];
                if (length[k] > best)
                {
                    best = length[k];
                    MainComponent = k;
                }
            }
        }

        /// <summary>Height minus the box mean of radius <paramref name="r"/> samples (summed-area table).</summary>
        public static float[] BoxConvexity(float[] h, int n, int r)
        {
            var w = n + 1;
            var sat = new double[w * w];
            for (var z = 0; z < n; z++)
            {
                double row = 0;
                for (var x = 0; x < n; x++)
                {
                    row += h[z * n + x];
                    sat[(z + 1) * w + x + 1] = sat[z * w + x + 1] + row;
                }
            }

            var result = new float[n * n];
            for (var z = 0; z < n; z++)
            {
                var z0 = Mathf.Max(0, z - r);
                var z1 = Mathf.Min(n - 1, z + r);
                for (var x = 0; x < n; x++)
                {
                    var x0 = Mathf.Max(0, x - r);
                    var x1 = Mathf.Min(n - 1, x + r);
                    var sum = sat[(z1 + 1) * w + x1 + 1] - sat[z0 * w + x1 + 1] - sat[(z1 + 1) * w + x0] + sat[z0 * w + x0];
                    var mean = sum / ((x1 - x0 + 1) * (z1 - z0 + 1));
                    result[z * n + x] = (float)(h[z * n + x] - mean);
                }
            }

            return result;
        }
    }

    /// <summary>Lattice helpers: components, exact distance transform.</summary>
    public static class GeographyGrid
    {
        /// <summary>8-connected components of a mask; -1 outside it. Labels in scan order.</summary>
        public static int[] Components8(bool[] mask, int n, out int count)
        {
            var label = new int[mask.Length];
            for (var i = 0; i < label.Length; i++)
            {
                label[i] = -1;
            }

            count = 0;
            var stack = new Stack<int>();
            for (var start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || label[start] >= 0)
                {
                    continue;
                }

                label[start] = count;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    var i = stack.Pop();
                    var x = i % n;
                    var z = i / n;
                    for (var dz = -1; dz <= 1; dz++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            var nx = x + dx;
                            var nz = z + dz;
                            if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= n || nz >= n)
                            {
                                continue;
                            }

                            var j = nz * n + nx;
                            if (mask[j] && label[j] < 0)
                            {
                                label[j] = count;
                                stack.Push(j);
                            }
                        }
                    }
                }

                count++;
            }

            return label;
        }

        /// <summary>
        /// Zhang–Suen thinning of a mask to a one-cell, 8-connected skeleton. Deterministic: each
        /// sub-iteration marks in scan order and deletes after the scan.
        /// </summary>
        public static bool[] Thin(bool[] mask, int n)
        {
            var img = (bool[])mask.Clone();
            for (var z = 0; z < n; z++)
            {
                img[z * n] = false;
                img[z * n + n - 1] = false;
            }

            for (var x = 0; x < n; x++)
            {
                img[x] = false;
                img[(n - 1) * n + x] = false;
            }

            var active = new List<int>();
            for (var i = 0; i < img.Length; i++)
            {
                if (img[i])
                {
                    active.Add(i);
                }
            }

            var marked = new List<int>();
            var p = new bool[8];
            var changed = true;
            while (changed)
            {
                changed = false;
                for (var pass = 0; pass < 2; pass++)
                {
                    marked.Clear();
                    foreach (var i in active)
                    {
                        if (!img[i])
                        {
                            continue;
                        }

                        Ring(img, n, i, p);
                        var b = 0;
                        for (var k = 0; k < 8; k++)
                        {
                            b += p[k] ? 1 : 0;
                        }

                        if (b < 2 || b > 6 || Transitions(p) != 1)
                        {
                            continue;
                        }

                        // p[0] N, p[2] E, p[4] S, p[6] W.
                        var keep = pass == 0
                            ? (p[0] && p[2] && p[4]) || (p[2] && p[4] && p[6])
                            : (p[0] && p[2] && p[6]) || (p[0] && p[4] && p[6]);
                        if (!keep)
                        {
                            marked.Add(i);
                        }
                    }

                    foreach (var i in marked)
                    {
                        img[i] = false;
                    }

                    changed |= marked.Count > 0;
                }

                active.RemoveAll(i => !img[i]);
            }

            return img;
        }

        /// <summary>Number of empty→filled transitions around a cell's eight neighbours.</summary>
        public static int CrossingNumber(bool[] img, int n, int x, int z)
        {
            var p = new bool[8];
            Ring(img, n, z * n + x, p);
            return Transitions(p);
        }

        /// <summary>Neighbours clockwise from north: N, NE, E, SE, S, SW, W, NW (+Z is north).</summary>
        private static void Ring(bool[] img, int n, int i, bool[] p)
        {
            p[0] = img[i + n];
            p[1] = img[i + n + 1];
            p[2] = img[i + 1];
            p[3] = img[i - n + 1];
            p[4] = img[i - n];
            p[5] = img[i - n - 1];
            p[6] = img[i - 1];
            p[7] = img[i + n - 1];
        }

        private static int Transitions(bool[] p)
        {
            var t = 0;
            for (var k = 0; k < 8; k++)
            {
                if (!p[k] && p[(k + 1) % 8])
                {
                    t++;
                }
            }

            return t;
        }

        /// <summary>Areas, in cells, of the components <see cref="Components8"/> labelled.</summary>
        public static int[] ComponentSizes(int[] label, int count)
        {
            var sizes = new int[count];
            foreach (var l in label)
            {
                if (l >= 0)
                {
                    sizes[l]++;
                }
            }

            return sizes;
        }

        /// <summary>
        /// Exact squared Euclidean distance, in cells², from every cell to the nearest feature cell
        /// (Felzenszwalb–Huttenlocher). Cells with no feature anywhere get a large finite value.
        /// </summary>
        public static float[] SquaredDistance(bool[] feature, int n)
        {
            const float inf = 1e20f;
            var d = new float[n * n];
            for (var i = 0; i < d.Length; i++)
            {
                d[i] = feature[i] ? 0f : inf;
            }

            var f = new float[n];
            var outRow = new float[n];
            var v = new int[n];
            var zBreaks = new float[n + 1];

            for (var x = 0; x < n; x++)
            {
                for (var z = 0; z < n; z++)
                {
                    f[z] = d[z * n + x];
                }

                Transform1D(f, n, outRow, v, zBreaks);
                for (var z = 0; z < n; z++)
                {
                    d[z * n + x] = outRow[z];
                }
            }

            for (var z = 0; z < n; z++)
            {
                Array.Copy(d, z * n, f, 0, n);
                Transform1D(f, n, outRow, v, zBreaks);
                Array.Copy(outRow, 0, d, z * n, n);
            }

            return d;
        }

        private static void Transform1D(float[] f, int n, float[] d, int[] v, float[] z)
        {
            var k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;
            for (var q = 1; q < n; q++)
            {
                var s = ((f[q] + (float)q * q) - (f[v[k]] + (float)v[k] * v[k])) / (2f * q - 2f * v[k]);
                while (s <= z[k])
                {
                    k--;
                    s = ((f[q] + (float)q * q) - (f[v[k]] + (float)v[k] * v[k])) / (2f * q - 2f * v[k]);
                }

                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (var q = 0; q < n; q++)
            {
                while (z[k + 1] < q)
                {
                    k++;
                }

                var dq = q - v[k];
                d[q] = dq * dq + f[v[k]];
            }
        }
    }

    /// <summary>One summit of the merge tree: its key col and topographic prominence.</summary>
    public struct GeographyPeak
    {
        public int Index;
        public float Height;
        public float Prominence;

        /// <summary>Key col cell, or -1 when the col is the sea (the island's highest point).</summary>
        public int Col;
        public float ColHeight;
    }

    /// <summary>The §7 measurements on one lattice.</summary>
    public sealed class GeographyMetrics
    {
        // ---- authoring defaults (research record §7; none measured, none approved) ----
        public const float RegionWindowMeters = 600f;
        public const float RegionExcessMeters = 18f;
        public const float RegionMinAreaSqm = 100_000f;
        public const float SummitProminenceMeters = 30f;
        public const float SaddleProminenceMeters = 15f;
        public const float PassDistanceSteps = 3f;
        public const float CoastBinMeters = 10f;
        public const float DomeCoastAlignedPercent = 65f;
        public const float DomeEtaSquared = 0.80f;
        public const int DomeMaxSummits = 1;
        public const float DomeP90Share = 0.95f;
        public const float CliffRiseMeters = 15f;
        public const float CliffRunMeters = 60f;
        public const float CoastFeatureRadiusMeters = 100f;
        public const float CoastFeatureMinAreaSqm = 10_000f;
        public const float ExpressedConvexityMeters = 1.5f;
        public const float DeclaredDivideSteps = 2f;
        public const float MinDivideComponentMeters = 50f;
        public const float FloorUpliftMeters = IslandReliefMetrics.FloorUpliftMeters;

        public int Seed;
        public int GeneratorVersion;
        public int Massifs;
        public string H4AHash;
        public string PinnedHash;
        public GeographyAnchors Anchors;
        public float LandAreaSqm;

        // M1
        public float[] Bands = new float[5];
        public float P50, P90, Highest;

        // M2
        public List<float> RegionAreasSqm = new List<float>();

        /// <summary>Cells of the M2 regions, for images.</summary>
        public bool[] RegionMask;

        // M3
        public float CoastAlignedPercent;
        public float EtaSquared;
        public int Summits;
        public int SeparateSummits;
        public List<GeographyPeak> Peaks = new List<GeographyPeak>();
        public float P90LargestShare;
        public bool SingleDome;

        // M4
        public float ChannelKm;
        public float DivideLengthMeters;
        public int DivideComponents;
        public float LongestDivideMeters;
        public int BranchPoints;
        public float CrestMean, CrestCv;
        public float ExpressedShare;

        // M5
        public int Saddles;
        public int Passes;
        public List<GeographyPeak> SaddlePeaks = new List<GeographyPeak>();
        public List<bool> SaddleIsPass = new List<bool>();

        // M6
        public float Crenulation;
        public float ShorelineMeters;
        public float CliffShare;
        public int Headlands, Coves;
        public float HeadlandAreaSqm, CoveAreaSqm;
        public bool[] HeadlandMask, CoveMask;

        // D-a
        public float DeclaredDivideMeters;
        public float DeclaredNearShare;
        public float CrestNearShare, SpurNearShare;
        public float CrestMeters, SpurMeters;

        // D-b, D-c
        public string HydrologyStatus;
        public int Systems, Reaches, Tributaries, Sources, Lakes;
        public float RiverKm;
        public int MaxOrder, LargestSystemOrder;
        public float LargestSystemAreaSqm;
        public List<int> StreamsPerOrder = new List<int>();
        public float BifurcationRatio;
        public float HackExponent;
        public int HackPoints;
        public bool FlowTreeComplete;

        // D-d
        public int Depressions;
        public float DepressionAreaSqm, DepressionMaxDepth;
        public int MicroDepressions;

        // D-e
        public float ReachInValleyShare;
        public int Trajectories;

        // T-a
        public float[] SlopeBands = new float[5];

        public static IEnumerable<string> DefinitionLines()
        {
            var c = CultureInfo.InvariantCulture;
            yield return "definitions (land = height > 24 m sea level; lattice 1025 x 1025, 2.734 m):";
            yield return "  M1 hypsometry: land share in 24-50/50-70/70-110/110-230/230+ m; p50, p90, highest point";
            yield return $"  M2 relief regions: 8-connected land whose height exceeds the median of land heights in a {RegionWindowMeters} m square window by >= {RegionExcessMeters} m, area >= {(RegionMinAreaSqm / 1e6f).ToString("F2", c)} km2 (median on a 16-sample grid, window sampled every 4, bilinear)";
            yield return $"  M3 single-dome detector: (a) coast-aligned gradient share of the interior floor (IslandReliefMetrics: uplift <= {FloorUpliftMeters} m, sd > 250 m, within 18 deg);";
            yield return $"     (b) eta^2 of floor height on signed coast distance in {CoastBinMeters} m bins (share of variance a function of coast distance explains);";
            yield return $"     (c) summits with topographic prominence >= {SummitProminenceMeters} m (8-connected merge tree, sea level is the key col of each island's top), all and SEPARATE (key col below p90);";
            yield return "     (d) share of land >= p90 in its largest 8-connected component";
            yield return $"     FLAG single dome when (a) > {DomeCoastAlignedPercent}% and (b) >= {DomeEtaSquared.ToString("F2", c)} and separate (c) <= {DomeMaxSummits} and (d) >= {DomeP90Share.ToString("F2", c)}";
            yield return "  M4 divides: flow-divide cells of the filled-surface D8 (along one lattice axis both neighbours drain away, within 45 deg), thinned to a one-cell skeleton (Zhang-Suen);";
            yield return $"     length over skeleton links (1 or sqrt 2 cells); components >= {MinDivideComponentMeters} m; longest = main divide; branch points = clusters of skeleton cells with crossing number >= 3;";
            yield return $"     crest mean / CV over skeleton cells of (height - 24 m); expressed = share of skeleton cells with convexity (height - 20 m box mean) >= {ExpressedConvexityMeters.ToString(c)} m; channels = cells >= {HydrologySettings.Default.ChannelInitiationAreaSqm.ToString("F0", c)} m2";
            yield return $"  M5 saddles: key cols of summits with prominence >= {SaddleProminenceMeters} m; pass = such a saddle within {PassDistanceSteps} lattice steps of the main divide skeleton";
            yield return $"  M6 coast: crenulation = Stage 1 mainland length / convex-hull perimeter; cliff = shore land cell whose land rises >= {CliffRiseMeters} m within {CliffRunMeters} m;";
            yield return $"     headlands / coves = 8-connected land removed by opening / sea added by closing with a {CoastFeatureRadiusMeters} m disc, area >= {(CoastFeatureMinAreaSqm / 1e6f).ToString("F2", c)} km2";
            yield return $"  D-a declared divides (massif crest lines, Stage 3B primary spurs, inter-massif divides) on land: share of length within {DeclaredDivideSteps} lattice steps of an M4 flow-divide cell";
            yield return "  D-b hydrology package on the lattice (HydrologyPlanner, shipping settings): systems, reaches, tributaries, Strahler, streams per order, bifurcation ratio (mean N_k / N_k+1), Hack exponent (least squares log L on log A over reach ends, L = longest upstream channel length)";
            yield return "  D-c flow tree complete (every cell visited) and the planner's own validation (0 uphill reaches, 0 cycles)";
            yield return "  D-d S8 depressions (>= 0.25 m and >= 100 m2) by priority-flood fill depth (IslandReliefMetrics); no basin is permitted today, so all count";
            yield return "  D-e share of river node length inside a Stage 3C FinalValleyTrajectory half-width; incision is n/a on H4A (no carve)";
            yield return "  T-a slope bands of all land (central differences): <5, 5-15, 15-30, 30-45, >45 deg";
        }

        public static GeographyMetrics Measure(IslandReliefWorld w, GeographyAnalysis a)
        {
            var m = new GeographyMetrics
            {
                Seed = w.Seed,
                GeneratorVersion = w.Stage1.Settings.GeneratorVersion,
                Massifs = w.Stage2B.Massifs.Count,
                H4AHash = w.H4AField.Hash(),
                Anchors = GeographyAnchors.From(w.Stage1),
            };
            m.PinnedHash = TerrainGenGeographyDump.PinnedH4AHashes.TryGetValue(w.Seed, out var pinned) ? pinned : null;

            var h = a.Heights;
            var n = a.N;
            var cell = a.Cell;
            var cellArea = cell * cell;

            // M1 percentiles, M3a and D-d use the Stage 4A dump's own measurement code.
            var relief = IslandReliefMetrics.Measure(h, w);
            m.CoastAlignedPercent = relief.CoastAlignedPercent;
            m.P50 = relief.Percentiles[2];
            m.P90 = relief.Percentiles[4];
            m.Highest = relief.HighestPoint;
            m.Depressions = relief.Depressions;
            m.DepressionAreaSqm = relief.DepressionAreaSqm;
            m.DepressionMaxDepth = relief.DepressionMaxDepth;
            m.MicroDepressions = relief.MicroDepressions;

            var landCount = 0;
            var bands = new int[5];
            var slopes = new int[5];
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!a.Land[i])
                    {
                        continue;
                    }

                    landCount++;
                    var v = h[i];
                    bands[v < 50f ? 0 : v < 70f ? 1 : v < 110f ? 2 : v < 230f ? 3 : 4]++;
                    var s = IslandReliefMetrics.SlopeDegrees(h, n, cell, x, z);
                    slopes[s < 5f ? 0 : s < 15f ? 1 : s < 30f ? 2 : s <= 45f ? 3 : 4]++;
                }
            }

            m.LandAreaSqm = landCount * cellArea;
            for (var b = 0; b < 5; b++)
            {
                m.Bands[b] = 100f * bands[b] / Mathf.Max(1, landCount);
                m.SlopeBands[b] = 100f * slopes[b] / Mathf.Max(1, landCount);
            }

            MeasureRegions(m, a, cellArea);
            MeasureDome(m, w, a);
            MeasureDivides(m, a);
            MeasureCoast(m, w, a, cellArea);
            MeasureDeclaredDivides(m, w, a);
            MeasureHydrology(m, w, a);
            return m;
        }

        // ---------- M2 ----------

        private static void MeasureRegions(GeographyMetrics m, GeographyAnalysis a, float cellArea)
        {
            var n = a.N;
            var h = a.Heights;
            const int centreStride = 16;
            const int sampleStride = 4;
            var half = Mathf.RoundToInt(RegionWindowMeters * 0.5f / a.Cell);
            var gn = (n - 1) / centreStride + 1;
            var median = new float[gn * gn];
            var buffer = new List<float>(4096);

            for (var gz = 0; gz < gn; gz++)
            {
                for (var gx = 0; gx < gn; gx++)
                {
                    var cx = gx * centreStride;
                    var cz = gz * centreStride;
                    buffer.Clear();
                    for (var z = Mathf.Max(0, cz - half); z <= Mathf.Min(n - 1, cz + half); z += sampleStride)
                    {
                        for (var x = Mathf.Max(0, cx - half); x <= Mathf.Min(n - 1, cx + half); x += sampleStride)
                        {
                            var i = z * n + x;
                            if (a.Land[i])
                            {
                                buffer.Add(h[i]);
                            }
                        }
                    }

                    if (buffer.Count == 0)
                    {
                        median[gz * gn + gx] = WorldScale.SeaLevelMeters;
                        continue;
                    }

                    buffer.Sort();
                    median[gz * gn + gx] = buffer[buffer.Count / 2];
                }
            }

            var mask = new bool[n * n];
            for (var z = 0; z < n; z++)
            {
                var fz = z / (float)centreStride;
                var z0 = Mathf.Min(gn - 1, Mathf.FloorToInt(fz));
                var z1 = Mathf.Min(gn - 1, z0 + 1);
                var tz = fz - z0;
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!a.Land[i])
                    {
                        continue;
                    }

                    var fx = x / (float)centreStride;
                    var x0 = Mathf.Min(gn - 1, Mathf.FloorToInt(fx));
                    var x1 = Mathf.Min(gn - 1, x0 + 1);
                    var tx = fx - x0;
                    var med = Mathf.Lerp(
                        Mathf.Lerp(median[z0 * gn + x0], median[z0 * gn + x1], tx),
                        Mathf.Lerp(median[z1 * gn + x0], median[z1 * gn + x1], tx),
                        tz);
                    mask[i] = h[i] - med >= RegionExcessMeters;
                }
            }

            var label = GeographyGrid.Components8(mask, n, out var count);
            var sizes = GeographyGrid.ComponentSizes(label, count);
            foreach (var size in sizes)
            {
                var area = size * cellArea;
                if (area >= RegionMinAreaSqm)
                {
                    m.RegionAreasSqm.Add(area);
                }
            }

            m.RegionAreasSqm.Sort((p, q) => q.CompareTo(p));
            m.RegionMask = new bool[n * n];
            for (var i = 0; i < label.Length; i++)
            {
                m.RegionMask[i] = label[i] >= 0 && sizes[label[i]] * cellArea >= RegionMinAreaSqm;
            }
        }

        // ---------- M3 ----------

        private static void MeasureDome(GeographyMetrics m, IslandReliefWorld w, GeographyAnalysis a)
        {
            var n = a.N;
            var h = a.Heights;
            var sd = w.Stage1.SignedDistance;
            var uplift = w.Stage2B.UpliftField.Heights;

            // (b) eta^2 of floor height on coast distance, bins visited in key order.
            var bins = new Dictionary<int, double[]>();
            var keys = new List<int>();
            double sum = 0, sumSq = 0;
            long count = 0;
            for (var i = 0; i < h.Length; i++)
            {
                if (!a.Land[i] || uplift[i] > FloorUpliftMeters)
                {
                    continue;
                }

                var key = Mathf.FloorToInt(sd[i] / CoastBinMeters);
                if (!bins.TryGetValue(key, out var acc))
                {
                    acc = new double[3];
                    bins[key] = acc;
                    keys.Add(key);
                }

                acc[0]++;
                acc[1] += h[i];
                acc[2] += (double)h[i] * h[i];
                sum += h[i];
                sumSq += (double)h[i] * h[i];
                count++;
            }

            keys.Sort();
            double within = 0;
            foreach (var key in keys)
            {
                var acc = bins[key];
                within += acc[2] - acc[1] * acc[1] / acc[0];
            }

            var total = count > 0 ? sumSq - sum * sum / count : 0.0;
            m.EtaSquared = total > 1e-9 ? (float)(1.0 - within / total) : 1f;

            // (c) merge tree.
            m.Peaks = MergeTree(h, a.Land, n);
            foreach (var p in m.Peaks)
            {
                if (p.Prominence < SummitProminenceMeters)
                {
                    continue;
                }

                m.Summits++;

                // A summit stands on separate high ground when its key col drops below the top decile
                // of land; two culminations of one massif share a col far above it.
                if (p.Col < 0 || p.ColHeight < m.P90)
                {
                    m.SeparateSummits++;
                }
            }

            // (d) share of land >= p90 in its largest component.
            var above = new bool[h.Length];
            var aboveCount = 0;
            for (var i = 0; i < h.Length; i++)
            {
                above[i] = a.Land[i] && h[i] >= m.P90;
                if (above[i])
                {
                    aboveCount++;
                }
            }

            var label = GeographyGrid.Components8(above, n, out var components);
            var sizes = GeographyGrid.ComponentSizes(label, components);
            var largest = 0;
            foreach (var size in sizes)
            {
                largest = Mathf.Max(largest, size);
            }

            m.P90LargestShare = aboveCount > 0 ? largest / (float)aboveCount : 0f;
            m.SingleDome = m.CoastAlignedPercent > DomeCoastAlignedPercent
                           && m.EtaSquared >= DomeEtaSquared
                           && m.SeparateSummits <= DomeMaxSummits
                           && m.P90LargestShare >= DomeP90Share;
        }

        /// <summary>
        /// Topographic prominence of every summit on land by a descending union-find sweep (ties broken
        /// by index). Records summits with prominence >= 1 m.
        /// </summary>
        public static List<GeographyPeak> MergeTree(float[] h, bool[] land, int n)
        {
            var sea = WorldScale.SeaLevelMeters;
            var keys = new List<ulong>(h.Length / 2);
            for (var i = 0; i < h.Length; i++)
            {
                if (land[i])
                {
                    // Descending height, ascending index; heights on land are positive floats, whose
                    // bit patterns order like the values.
                    var bits = (uint)BitConverter.SingleToInt32Bits(h[i]);
                    keys.Add(((ulong)(uint.MaxValue - bits) << 32) | (uint)i);
                }
            }

            var order = keys.ToArray();
            Array.Sort(order);

            var parent = new int[h.Length];
            var peak = new int[h.Length];
            var done = new bool[h.Length];
            var peaks = new List<GeographyPeak>();

            foreach (var key in order)
            {
                var i = (int)(key & 0xffffffffUL);
                done[i] = true;
                parent[i] = i;
                peak[i] = i;
                var x = i % n;
                var z = i / n;
                for (var dz = -1; dz <= 1; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var nx = x + dx;
                        var nz = z + dz;
                        if ((dx == 0 && dz == 0) || nx < 0 || nz < 0 || nx >= n || nz >= n)
                        {
                            continue;
                        }

                        var j = nz * n + nx;
                        if (!done[j])
                        {
                            continue;
                        }

                        var ri = Find(parent, i);
                        var rj = Find(parent, j);
                        if (ri == rj)
                        {
                            continue;
                        }

                        var pi = peak[ri];
                        var pj = peak[rj];
                        var iHigher = h[pi] > h[pj] || (h[pi] == h[pj] && pi < pj);
                        var low = iHigher ? rj : ri;
                        var high = iHigher ? ri : rj;
                        var prominence = h[peak[low]] - h[i];
                        if (prominence >= 1f)
                        {
                            peaks.Add(new GeographyPeak
                            {
                                Index = peak[low], Height = h[peak[low]], Prominence = prominence, Col = i, ColHeight = h[i],
                            });
                        }

                        parent[low] = high;
                    }
                }
            }

            for (var i = 0; i < h.Length; i++)
            {
                if (done[i] && Find(parent, i) == i)
                {
                    peaks.Add(new GeographyPeak
                    {
                        Index = peak[i], Height = h[peak[i]], Prominence = h[peak[i]] - sea, Col = -1, ColHeight = sea,
                    });
                }
            }

            peaks.Sort((p, q) =>
            {
                var byProminence = q.Prominence.CompareTo(p.Prominence);
                return byProminence != 0 ? byProminence : p.Index.CompareTo(q.Index);
            });
            return peaks;
        }

        private static int Find(int[] parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }

            return i;
        }

        // ---------- M4, M5 ----------

        private static void MeasureDivides(GeographyMetrics m, GeographyAnalysis a)
        {
            var n = a.N;
            var h = a.Heights;
            var sea = WorldScale.SeaLevelMeters;
            m.ChannelKm = a.ChannelCells * a.Cell / 1000f;
            m.DivideLengthMeters = a.SkeletonLengthMeters;
            m.LongestDivideMeters = a.MainComponent >= 0 ? a.ComponentLengthMeters[a.MainComponent] : 0f;
            foreach (var length in a.ComponentLengthMeters)
            {
                if (length >= MinDivideComponentMeters)
                {
                    m.DivideComponents++;
                }
            }

            double sum = 0, sumSq = 0;
            var cells = 0;
            var expressed = 0;
            for (var i = 0; i < a.Skeleton.Length; i++)
            {
                if (!a.Skeleton[i])
                {
                    continue;
                }

                var crest = h[i] - sea;
                sum += crest;
                sumSq += (double)crest * crest;
                cells++;
                if (a.Convexity[i] >= ExpressedConvexityMeters)
                {
                    expressed++;
                }
            }

            if (cells > 0)
            {
                var mean = sum / cells;
                var std = Math.Sqrt(Math.Max(0.0, sumSq / cells - mean * mean));
                m.CrestMean = (float)mean;
                m.CrestCv = mean > 1e-6 ? (float)(std / mean) : 0f;
                m.ExpressedShare = expressed / (float)cells;
            }

            GeographyGrid.Components8(a.BranchMask, n, out var clusters);
            m.BranchPoints = clusters;

            // M5: saddles of the merge tree, passes on the main divide.
            var main = new bool[n * n];
            for (var i = 0; i < main.Length; i++)
            {
                main[i] = a.MainComponent >= 0 && a.SkeletonComponent[i] == a.MainComponent;
            }

            var nearMain = GeographyGrid.SquaredDistance(main, n);
            foreach (var p in m.Peaks)
            {
                if (p.Col < 0 || p.Prominence < SaddleProminenceMeters)
                {
                    continue;
                }

                m.Saddles++;
                var isPass = nearMain[p.Col] <= PassDistanceSteps * PassDistanceSteps;
                if (isPass)
                {
                    m.Passes++;
                }

                m.SaddlePeaks.Add(p);
                m.SaddleIsPass.Add(isPass);
            }
        }

        // ---------- M6 ----------

        private static void MeasureCoast(GeographyMetrics m, IslandReliefWorld w, GeographyAnalysis a, float cellArea)
        {
            var n = a.N;
            var h = a.Heights;
            var land = a.Land;
            m.Crenulation = w.Stage1.Stats.CoastCrenulation;

            var reach = Mathf.CeilToInt(CliffRunMeters / a.Cell);
            var reachSq = (CliffRunMeters / a.Cell) * (CliffRunMeters / a.Cell);
            var shore = 0;
            var cliff = 0;
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!land[i])
                    {
                        continue;
                    }

                    var isShore = (x > 0 && !land[i - 1]) || (x < n - 1 && !land[i + 1])
                                  || (z > 0 && !land[i - n]) || (z < n - 1 && !land[i + n]);
                    if (!isShore)
                    {
                        continue;
                    }

                    shore++;
                    var top = h[i];
                    for (var dz = -reach; dz <= reach; dz++)
                    {
                        for (var dx = -reach; dx <= reach; dx++)
                        {
                            var nx = x + dx;
                            var nz = z + dz;
                            if (nx < 0 || nz < 0 || nx >= n || nz >= n || dx * dx + dz * dz > reachSq)
                            {
                                continue;
                            }

                            var j = nz * n + nx;
                            if (land[j] && h[j] > top)
                            {
                                top = h[j];
                            }
                        }
                    }

                    if (top - WorldScale.SeaLevelMeters >= CliffRiseMeters)
                    {
                        cliff++;
                    }
                }
            }

            // Shore cells per metre of a randomly oriented coast average 4 / pi per cell.
            m.ShorelineMeters = shore * a.Cell * Mathf.PI / 4f;
            m.CliffShare = shore > 0 ? cliff / (float)shore : 0f;

            var r2 = (CoastFeatureRadiusMeters / a.Cell) * (CoastFeatureRadiusMeters / a.Cell);
            var sea = new bool[n * n];
            for (var i = 0; i < sea.Length; i++)
            {
                sea[i] = !land[i];
            }

            // Opening: erode land by R, dilate back; what does not come back is a headland.
            var toSea = GeographyGrid.SquaredDistance(sea, n);
            var eroded = new bool[n * n];
            for (var i = 0; i < eroded.Length; i++)
            {
                eroded[i] = land[i] && toSea[i] > r2;
            }

            var toEroded = GeographyGrid.SquaredDistance(eroded, n);
            var headland = new bool[n * n];
            for (var i = 0; i < headland.Length; i++)
            {
                headland[i] = land[i] && toEroded[i] > r2;
            }

            // Closing: dilate land by R, erode back; sea that stays covered is a cove.
            var toLand = GeographyGrid.SquaredDistance(land, n);
            var outside = new bool[n * n];
            for (var i = 0; i < outside.Length; i++)
            {
                outside[i] = toLand[i] > r2;
            }

            var toOutside = GeographyGrid.SquaredDistance(outside, n);
            var cove = new bool[n * n];
            for (var i = 0; i < cove.Length; i++)
            {
                cove[i] = !land[i] && toOutside[i] > r2;
            }

            CountFeatures(headland, n, cellArea, out m.Headlands, out m.HeadlandAreaSqm, out m.HeadlandMask);
            CountFeatures(cove, n, cellArea, out m.Coves, out m.CoveAreaSqm, out m.CoveMask);
        }

        private static void CountFeatures(bool[] mask, int n, float cellArea, out int count, out float area, out bool[] kept)
        {
            var label = GeographyGrid.Components8(mask, n, out var components);
            var sizes = GeographyGrid.ComponentSizes(label, components);
            count = 0;
            area = 0f;
            foreach (var size in sizes)
            {
                if (size * cellArea >= CoastFeatureMinAreaSqm)
                {
                    count++;
                    area += size * cellArea;
                }
            }

            kept = new bool[mask.Length];
            for (var i = 0; i < mask.Length; i++)
            {
                kept[i] = label[i] >= 0 && sizes[label[i]] * cellArea >= CoastFeatureMinAreaSqm;
            }
        }

        // ---------- D-a ----------

        /// <summary>Declared divides as world polylines: massif crests first, then primary spurs and inter-massif divides.</summary>
        public static List<Vector2[]> DeclaredDivides(IslandReliefWorld w, out int crestCount)
        {
            var lines = new List<Vector2[]>();
            foreach (var massif in w.Stage2B.Massifs)
            {
                var steps = Mathf.Max(8, Mathf.CeilToInt(massif.LengthMeters / 5f));
                var line = new Vector2[steps + 1];
                for (var i = 0; i <= steps; i++)
                {
                    line[i] = massif.PointAt(i / (float)steps);
                }

                lines.Add(line);
            }

            crestCount = lines.Count;
            if (w.Ridges != null)
            {
                foreach (var spur in w.Ridges.Spurs)
                {
                    lines.Add(spur.Points);
                }

                foreach (var divide in w.Ridges.Ownership.Divides)
                {
                    lines.Add(divide.Points);
                }
            }

            return lines;
        }

        private static void MeasureDeclaredDivides(GeographyMetrics m, IslandReliefWorld w, GeographyAnalysis a)
        {
            var n = a.N;
            var isDivide = new bool[n * n];
            for (var i = 0; i < isDivide.Length; i++)
            {
                isDivide[i] = a.DivideMask[i];
            }

            var near = GeographyGrid.SquaredDistance(isDivide, n);
            var limit = DeclaredDivideSteps * DeclaredDivideSteps;
            var lines = DeclaredDivides(w, out var crestCount);
            var step = a.Cell * 0.5f;
            double total = 0, inside = 0, crestTotal = 0, crestInside = 0, spurTotal = 0, spurInside = 0;

            for (var l = 0; l < lines.Count; l++)
            {
                var line = lines[l];
                if (line == null)
                {
                    continue;
                }

                for (var k = 0; k + 1 < line.Length; k++)
                {
                    var p = line[k];
                    var q = line[k + 1];
                    var length = (q - p).magnitude;
                    var samples = Mathf.Max(1, Mathf.CeilToInt(length / step));
                    var piece = length / samples;
                    for (var s = 0; s < samples; s++)
                    {
                        var pt = Vector2.Lerp(p, q, (s + 0.5f) / samples);
                        var x = Mathf.RoundToInt(pt.x / a.Cell);
                        var z = Mathf.RoundToInt(pt.y / a.Cell);
                        if (x < 0 || z < 0 || x >= n || z >= n || !a.Land[z * n + x])
                        {
                            continue;
                        }

                        var hit = near[z * n + x] <= limit;
                        total += piece;
                        inside += hit ? piece : 0;
                        if (l < crestCount)
                        {
                            crestTotal += piece;
                            crestInside += hit ? piece : 0;
                        }
                        else
                        {
                            spurTotal += piece;
                            spurInside += hit ? piece : 0;
                        }
                    }
                }
            }

            m.DeclaredDivideMeters = (float)total;
            m.DeclaredNearShare = total > 0 ? (float)(inside / total) : float.NaN;
            m.CrestMeters = (float)crestTotal;
            m.CrestNearShare = crestTotal > 0 ? (float)(crestInside / crestTotal) : float.NaN;
            m.SpurMeters = (float)spurTotal;
            m.SpurNearShare = spurTotal > 0 ? (float)(spurInside / spurTotal) : float.NaN;
        }

        // ---------- D-b, D-c, D-e ----------

        private static void MeasureHydrology(GeographyMetrics m, IslandReliefWorld w, GeographyAnalysis a)
        {
            m.FlowTreeComplete = a.ProcessedCells == a.N * a.N;
            var plan = a.Hydrology;
            m.HydrologyStatus = plan.Succeeded ? "ok" : plan.Failure + ": " + plan.Message;
            m.Trajectories = w.Trajectories != null ? w.Trajectories.Trajectories.Count : 0;
            m.ReachInValleyShare = float.NaN;
            m.BifurcationRatio = float.NaN;
            m.HackExponent = float.NaN;
            if (!plan.Succeeded)
            {
                return;
            }

            var def = plan.Definition;
            var reaches = def.Reaches;
            m.Reaches = reaches.Count;
            m.Lakes = def.Lakes.Count;
            m.RiverKm = def.TotalRiverLengthKilometers;
            var index = new Dictionary<ReachId, int>();
            for (var r = 0; r < reaches.Count; r++)
            {
                index[reaches[r].Id] = r;
            }

            var upstream = new List<int>[reaches.Count];
            for (var r = 0; r < reaches.Count; r++)
            {
                upstream[r] = new List<int>();
            }

            for (var r = 0; r < reaches.Count; r++)
            {
                var reach = reaches[r];
                if (reach.IsSource)
                {
                    m.Sources++;
                }

                if (reach.DownstreamId.IsValid && index.TryGetValue(reach.DownstreamId, out var d))
                {
                    m.Tributaries++;
                    upstream[d].Add(r);
                }

                m.MaxOrder = Mathf.Max(m.MaxOrder, reach.StrahlerOrder);
                if (reach.Termination == ReachTermination.Ocean)
                {
                    m.Systems++;
                    if (reach.ContributingAreaSqm > m.LargestSystemAreaSqm)
                    {
                        m.LargestSystemAreaSqm = reach.ContributingAreaSqm;
                        m.LargestSystemOrder = reach.StrahlerOrder;
                    }
                }
            }

            // Streams per order: a reach of order k none of whose upstream reaches has order k.
            var perOrder = new int[m.MaxOrder + 1];
            for (var r = 0; r < reaches.Count; r++)
            {
                var k = reaches[r].StrahlerOrder;
                var continues = false;
                foreach (var u in upstream[r])
                {
                    continues |= reaches[u].StrahlerOrder == k;
                }

                if (!continues && k >= 1)
                {
                    perOrder[k]++;
                }
            }

            double ratioSum = 0;
            var ratios = 0;
            for (var k = 1; k <= m.MaxOrder; k++)
            {
                m.StreamsPerOrder.Add(perOrder[k]);
                if (k < m.MaxOrder && perOrder[k + 1] > 0)
                {
                    ratioSum += perOrder[k] / (double)perOrder[k + 1];
                    ratios++;
                }
            }

            m.BifurcationRatio = ratios > 0 ? (float)(ratioSum / ratios) : float.NaN;

            // Hack: longest upstream channel length against contributing area, over reach ends.
            var longest = new float[reaches.Count];
            var resolved = new bool[reaches.Count];
            for (var r = 0; r < reaches.Count; r++)
            {
                LongestUpstream(r, reaches, upstream, longest, resolved);
            }

            double sx = 0, sy = 0, sxx = 0, sxy = 0;
            for (var r = 0; r < reaches.Count; r++)
            {
                var area = reaches[r].ContributingAreaSqm;
                if (area <= 0f || longest[r] <= 0f)
                {
                    continue;
                }

                var lx = Math.Log10(area);
                var ly = Math.Log10(longest[r]);
                sx += lx;
                sy += ly;
                sxx += lx * lx;
                sxy += lx * ly;
                m.HackPoints++;
            }

            if (m.HackPoints >= 3)
            {
                var denom = m.HackPoints * sxx - sx * sx;
                m.HackExponent = Math.Abs(denom) > 1e-12 ? (float)((m.HackPoints * sxy - sx * sy) / denom) : float.NaN;
            }

            // D-e: river nodes inside a declared valley half-width.
            if (w.Trajectories == null || m.Trajectories == 0)
            {
                return;
            }

            var nodes = 0;
            var insideNodes = 0;
            foreach (var reach in reaches)
            {
                foreach (var node in reach.Nodes)
                {
                    nodes++;
                    foreach (var t in w.Trajectories.Trajectories)
                    {
                        if (!t.HasGeometry || !t.Project(node.PositionXZ, out _, out var across))
                        {
                            continue;
                        }

                        if (across <= t.HalfWidthMeters)
                        {
                            insideNodes++;
                            break;
                        }
                    }
                }
            }

            m.ReachInValleyShare = nodes > 0 ? insideNodes / (float)nodes : float.NaN;
        }

        private static void LongestUpstream(
            int r, IReadOnlyList<RiverReach> reaches, List<int>[] upstream, float[] longest, bool[] resolved)
        {
            // Iterative post-order over the reach tree.
            var stack = new Stack<(int reach, bool expanded)>();
            stack.Push((r, false));
            while (stack.Count > 0)
            {
                var (current, expanded) = stack.Pop();
                if (resolved[current])
                {
                    continue;
                }

                if (!expanded)
                {
                    stack.Push((current, true));
                    foreach (var u in upstream[current])
                    {
                        if (!resolved[u])
                        {
                            stack.Push((u, false));
                        }
                    }

                    continue;
                }

                var best = 0f;
                foreach (var u in upstream[current])
                {
                    best = Mathf.Max(best, longest[u]);
                }

                longest[current] = best + reaches[current].LengthMeters;
                resolved[current] = true;
            }
        }

        // ---------- output ----------

        private static string F(float v, string format, CultureInfo c) =>
            float.IsNaN(v) ? "n/a" : v.ToString(format, c);

        private static string J(float v, CultureInfo c) =>
            float.IsNaN(v) || float.IsInfinity(v) ? "null" : v.ToString("R", c);

        public string HashVerdict => PinnedHash == null ? "unpinned" : PinnedHash == H4AHash ? "PASS" : "FAIL";

        private string At(int index, CultureInfo c, float cell, int n) =>
            $"({((index % n) * cell).ToString("F0", c)},{((index / n) * cell).ToString("F0", c)})";

        public void AppendReport(StringBuilder sb, CultureInfo c)
        {
            const int n = 1025;
            var cell = WorldScale.WorldExtentXMeters / (n - 1);
            sb.AppendLine($"seed {Seed}  GeneratorVersion {GeneratorVersion}  massifs {Massifs}  land {(LandAreaSqm / 1e6f).ToString("F3", c)} km2");
            sb.AppendLine($"  H4A hash {H4AHash}  pinned {PinnedHash ?? "-"}  {HashVerdict}");
            sb.AppendLine(
                $"  Stage 1 anchors: mainland centroid ({Anchors.Centroid.x.ToString("F1", c)}, {Anchors.Centroid.y.ToString("F1", c)}) m, "
                + $"equal-area radius {Anchors.EqualAreaRadius.ToString("F1", c)} m, mainland {(Anchors.MainlandAreaSqm / 1e6f).ToString("F3", c)} km2");

            sb.AppendLine(
                $"  M1 hypsometry %: 24-50 {F(Bands[0], "F1", c)} / 50-70 {F(Bands[1], "F1", c)} / 70-110 {F(Bands[2], "F1", c)} / 110-230 {F(Bands[3], "F1", c)} / 230+ {F(Bands[4], "F1", c)}; "
                + $"p50 {F(P50, "F1", c)} m, p90 {F(P90, "F1", c)} m, highest {F(Highest, "F1", c)} m");

            var regions = new StringBuilder();
            foreach (var area in RegionAreasSqm)
            {
                regions.Append(regions.Length == 0 ? string.Empty : ", ").Append((area / 1e6f).ToString("F3", c));
            }

            sb.AppendLine($"  M2 relief regions: {RegionAreasSqm.Count} ({(regions.Length > 0 ? regions + " km2" : "none")})");
            sb.AppendLine(
                $"  M3 single dome: (a) coast-aligned {F(CoastAlignedPercent, "F1", c)}%  (b) eta2 {F(EtaSquared, "F3", c)}  "
                + $"(c) summits >= {SummitProminenceMeters} m prominence {Summits}, separate {SeparateSummits}  (d) p90 largest-component share {F(P90LargestShare, "F3", c)}  -> {(SingleDome ? "FLAGGED single dome" : "not flagged")}");

            var top = new StringBuilder();
            for (var p = 0; p < Mathf.Min(6, Peaks.Count); p++)
            {
                var peak = Peaks[p];
                top.Append(p == 0 ? string.Empty : "; ")
                    .Append($"{F(peak.Height, "F1", c)} m prom {F(peak.Prominence, "F1", c)} at {At(peak.Index, c, cell, n)}");
            }

            sb.AppendLine($"     summits by prominence: {top}");
            sb.AppendLine(
                $"  M4 divides: skeleton {(DivideLengthMeters / 1000f).ToString("F2", c)} km in {DivideComponents} component(s) >= {MinDivideComponentMeters} m, "
                + $"longest {(LongestDivideMeters / 1000f).ToString("F2", c)} km, branch points {BranchPoints}, "
                + $"crest mean {F(CrestMean, "F1", c)} m above sea, CV {F(CrestCv, "F3", c)}, expressed (convexity >= {ExpressedConvexityMeters.ToString(c)} m) {F(ExpressedShare * 100f, "F1", c)}%; channels {ChannelKm.ToString("F2", c)} km of cells");

            var saddles = new StringBuilder();
            for (var s = 0; s < SaddlePeaks.Count; s++)
            {
                var p = SaddlePeaks[s];
                saddles.Append(s == 0 ? string.Empty : "; ")
                    .Append($"col {F(p.ColHeight, "F1", c)} m at {At(p.Col, c, cell, n)} (peak {F(p.Height, "F1", c)}, prom {F(p.Prominence, "F1", c)}){(SaddleIsPass[s] ? " on main divide" : string.Empty)}");
            }

            sb.AppendLine($"  M5 saddles >= {SaddleProminenceMeters} m: {Saddles}, passes on the main divide: {Passes}{(saddles.Length > 0 ? " - " + saddles : string.Empty)}");
            sb.AppendLine(
                $"  M6 coast: crenulation {F(Crenulation, "F3", c)}, shoreline {(ShorelineMeters / 1000f).ToString("F2", c)} km, "
                + $"cliff share {F(CliffShare * 100f, "F2", c)}%, headlands {Headlands} ({(HeadlandAreaSqm / 1e6f).ToString("F3", c)} km2), coves {Coves} ({(CoveAreaSqm / 1e6f).ToString("F3", c)} km2)");
            sb.AppendLine(
                $"  D-a declared divides near a D8 divide: {F(DeclaredNearShare * 100f, "F1", c)}% of {(DeclaredDivideMeters / 1000f).ToString("F2", c)} km "
                + $"(crests {F(CrestNearShare * 100f, "F1", c)}% of {(CrestMeters / 1000f).ToString("F2", c)} km, spurs/divides {F(SpurNearShare * 100f, "F1", c)}% of {(SpurMeters / 1000f).ToString("F2", c)} km)");

            var orders = new StringBuilder();
            for (var k = 0; k < StreamsPerOrder.Count; k++)
            {
                orders.Append(k == 0 ? string.Empty : "/").Append(StreamsPerOrder[k]);
            }

            sb.AppendLine(
                $"  D-b hydrology: {HydrologyStatus}; systems {Systems}, reaches {Reaches}, sources {Sources}, tributaries {Tributaries}, lakes {Lakes}, "
                + $"river {RiverKm.ToString("F2", c)} km; max Strahler {MaxOrder}, largest system order {LargestSystemOrder} "
                + $"({(LargestSystemAreaSqm / 1e6f).ToString("F3", c)} km2); streams per order {(orders.Length > 0 ? orders.ToString() : "-")}; "
                + $"bifurcation {F(BifurcationRatio, "F2", c)}; Hack exponent {F(HackExponent, "F3", c)} over {HackPoints} reach ends");
            sb.AppendLine($"  D-c flow tree complete {(FlowTreeComplete ? "yes" : "NO")}; planner validation {(HydrologyStatus == "ok" ? "PASS" : HydrologyStatus)}");
            sb.AppendLine(
                $"  D-d S8 depressions {Depressions} ({DepressionAreaSqm.ToString("F0", c)} m2, deepest {DepressionMaxDepth.ToString("F2", c)} m), micro {MicroDepressions}");
            sb.AppendLine($"  D-e river nodes inside a declared valley: {F(ReachInValleyShare * 100f, "F1", c)}% ({Trajectories} trajectories); incision n/a on H4A");
            sb.AppendLine(
                $"  T-a slope %: <5 {F(SlopeBands[0], "F1", c)} / 5-15 {F(SlopeBands[1], "F1", c)} / 15-30 {F(SlopeBands[2], "F1", c)} / 30-45 {F(SlopeBands[3], "F1", c)} / >45 {F(SlopeBands[4], "F1", c)}");
            sb.AppendLine();
        }

        public string SummaryLine(CultureInfo c) => string.Join(" ", new[]
        {
            $"seed={Seed}",
            $"massifs={Massifs}",
            $"hash={HashVerdict}",
            $"p50/p90={F(P50, "F0", c)}/{F(P90, "F0", c)}",
            $"gt110={F(Bands[3] + Bands[4], "F1", c)}",
            $"regions={RegionAreasSqm.Count}",
            $"aligned={F(CoastAlignedPercent, "F1", c)}",
            $"eta2={F(EtaSquared, "F3", c)}",
            $"summits30={Summits}",
            $"separate={SeparateSummits}",
            $"p90share={F(P90LargestShare, "F3", c)}",
            $"dome={(SingleDome ? "FLAG" : "no")}",
            $"divideKm={(DivideLengthMeters / 1000f).ToString("F1", c)}",
            $"branches={BranchPoints}",
            $"crestCV={F(CrestCv, "F2", c)}",
            $"expressed={F(ExpressedShare * 100f, "F0", c)}",
            $"saddles={Saddles}",
            $"passes={Passes}",
            $"cliff={F(CliffShare * 100f, "F1", c)}",
            $"headlands={Headlands}",
            $"coves={Coves}",
            $"Da={F(DeclaredNearShare * 100f, "F0", c)}",
            $"systems={Systems}",
            $"tribs={Tributaries}",
            $"order={MaxOrder}",
            $"Rb={F(BifurcationRatio, "F2", c)}",
            $"hack={F(HackExponent, "F2", c)}",
            $"riverKm={RiverKm.ToString("F2", c)}",
            $"pits={Depressions}",
            $"De={F(ReachInValleyShare * 100f, "F0", c)}",
            $"lt5={F(SlopeBands[0], "F1", c)}",
            $"gt45={F(SlopeBands[4], "F1", c)}",
        });

        public string ToJson(CultureInfo c)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"seed\":{Seed},\"generatorVersion\":{GeneratorVersion},\"massifs\":{Massifs},");
            sb.Append($"\"h4aHash\":\"{H4AHash}\",\"hashVerdict\":\"{HashVerdict}\",\"landAreaSqm\":{J(LandAreaSqm, c)},");
            sb.Append($"\"anchors\":{{\"cx\":{J(Anchors.Centroid.x, c)},\"cz\":{J(Anchors.Centroid.y, c)},\"r\":{J(Anchors.EqualAreaRadius, c)}}},");
            sb.Append($"\"M1\":{{\"bands\":[{J(Bands[0], c)},{J(Bands[1], c)},{J(Bands[2], c)},{J(Bands[3], c)},{J(Bands[4], c)}],\"p50\":{J(P50, c)},\"p90\":{J(P90, c)},\"highest\":{J(Highest, c)}}},");
            sb.Append("\"M2\":{\"regionsSqm\":[");
            for (var i = 0; i < RegionAreasSqm.Count; i++)
            {
                sb.Append(i == 0 ? string.Empty : ",").Append(J(RegionAreasSqm[i], c));
            }

            sb.Append("]},");
            sb.Append($"\"M3\":{{\"coastAligned\":{J(CoastAlignedPercent, c)},\"eta2\":{J(EtaSquared, c)},\"summits\":{Summits},\"separateSummits\":{SeparateSummits},\"p90Share\":{J(P90LargestShare, c)},\"singleDome\":{(SingleDome ? "true" : "false")}}},");
            sb.Append($"\"M4\":{{\"channelKm\":{J(ChannelKm, c)},\"lengthM\":{J(DivideLengthMeters, c)},\"components\":{DivideComponents},\"longestM\":{J(LongestDivideMeters, c)},\"branches\":{BranchPoints},\"crestMean\":{J(CrestMean, c)},\"crestCv\":{J(CrestCv, c)},\"expressed\":{J(ExpressedShare, c)}}},");
            sb.Append($"\"M5\":{{\"saddles\":{Saddles},\"passes\":{Passes}}},");
            sb.Append($"\"M6\":{{\"crenulation\":{J(Crenulation, c)},\"shorelineM\":{J(ShorelineMeters, c)},\"cliffShare\":{J(CliffShare, c)},\"headlands\":{Headlands},\"coves\":{Coves}}},");
            sb.Append($"\"Da\":{{\"declaredM\":{J(DeclaredDivideMeters, c)},\"nearShare\":{J(DeclaredNearShare, c)},\"crestShare\":{J(CrestNearShare, c)},\"spurShare\":{J(SpurNearShare, c)}}},");
            sb.Append($"\"Db\":{{\"status\":\"{HydrologyStatus.Replace("\"", "'")}\",\"systems\":{Systems},\"reaches\":{Reaches},\"sources\":{Sources},\"tributaries\":{Tributaries},\"lakes\":{Lakes},\"riverKm\":{J(RiverKm, c)},\"maxOrder\":{MaxOrder},\"largestOrder\":{LargestSystemOrder},\"bifurcation\":{J(BifurcationRatio, c)},\"hack\":{J(HackExponent, c)},\"hackPoints\":{HackPoints}}},");
            sb.Append($"\"Dc\":{{\"flowTreeComplete\":{(FlowTreeComplete ? "true" : "false")}}},");
            sb.Append($"\"Dd\":{{\"depressions\":{Depressions},\"areaSqm\":{J(DepressionAreaSqm, c)},\"deepest\":{J(DepressionMaxDepth, c)},\"micro\":{MicroDepressions}}},");
            sb.Append($"\"De\":{{\"inValleyShare\":{J(ReachInValleyShare, c)},\"trajectories\":{Trajectories}}},");
            sb.Append($"\"Ta\":[{J(SlopeBands[0], c)},{J(SlopeBands[1], c)},{J(SlopeBands[2], c)},{J(SlopeBands[3], c)},{J(SlopeBands[4], c)}]");
            sb.Append('}');
            return sb.ToString();
        }
    }

    /// <summary>Top-down diagnostic image of the G0 structure readings, for checking the definitions by eye.</summary>
    public static class GeographyImages
    {
        public static void WriteStructure(string path, IslandReliefWorld w, GeographyAnalysis a, GeographyMetrics m)
        {
            var n = a.N;
            var pixels = Hillshaded(a.Heights, n, a.Cell, a.Land);

            for (var i = 0; i < pixels.Length; i++)
            {
                if (m.RegionMask != null && m.RegionMask[i])
                {
                    pixels[i] = Blend(pixels[i], new Color32(240, 200, 60, 255), 0.35f);
                }

                if (m.HeadlandMask[i])
                {
                    pixels[i] = Blend(pixels[i], new Color32(200, 90, 200, 255), 0.5f);
                }

                if (m.CoveMask[i])
                {
                    pixels[i] = Blend(pixels[i], new Color32(90, 200, 220, 255), 0.6f);
                }

                if (a.Channel[i] && a.Land[i])
                {
                    pixels[i] = new Color32(40, 90, 230, 255);
                }
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                if (a.Skeleton[i])
                {
                    pixels[i] = a.SkeletonComponent[i] == a.MainComponent ? new Color32(230, 30, 30, 255) : new Color32(160, 50, 50, 255);
                }
                else if (a.DivideMask[i])
                {
                    pixels[i] = Blend(pixels[i], new Color32(230, 90, 90, 255), 0.45f);
                }
            }

            if (w.Trajectories != null)
            {
                foreach (var t in w.Trajectories.Trajectories)
                {
                    DrawPolyline(pixels, n, a.Cell, t.Points, new Color32(0, 230, 230, 255));
                }
            }

            foreach (var line in GeographyMetrics.DeclaredDivides(w, out _))
            {
                DrawPolyline(pixels, n, a.Cell, line, new Color32(230, 0, 230, 255));
            }

            foreach (var p in m.Peaks)
            {
                if (p.Prominence >= GeographyMetrics.SaddleProminenceMeters)
                {
                    Disc(pixels, n, p.Index, p.Prominence >= GeographyMetrics.SummitProminenceMeters ? 5 : 3, new Color32(0, 0, 0, 255));
                }
            }

            for (var s = 0; s < m.SaddlePeaks.Count; s++)
            {
                Disc(pixels, n, m.SaddlePeaks[s].Col, 4, m.SaddleIsPass[s] ? new Color32(30, 230, 30, 255) : new Color32(255, 255, 255, 255));
            }

            WritePng(path, pixels, n);
        }

        public static Color32[] Hillshaded(float[] h, int n, float cell, bool[] land)
        {
            var pixels = new Color32[h.Length];
            var az = 315f * Mathf.Deg2Rad;
            var alt = 40f * Mathf.Deg2Rad;
            var lx = Mathf.Cos(alt) * Mathf.Cos(az);
            var lz = Mathf.Cos(alt) * Mathf.Sin(az);
            var ly = Mathf.Sin(alt);
            for (var z = 0; z < n; z++)
            {
                for (var x = 0; x < n; x++)
                {
                    var i = z * n + x;
                    if (!land[i])
                    {
                        pixels[i] = new Color32(28, 40, 62, 255);
                        continue;
                    }

                    IslandReliefMetrics.Gradient(h, n, cell, x, z, out var gx, out var gz);
                    var len = Mathf.Sqrt(gx * gx + gz * gz + 1f);
                    var shade = Mathf.Clamp01((-gx * lx - gz * lz + ly) / len);
                    var v = (byte)Mathf.Clamp(60 + 180 * shade, 0, 255);
                    pixels[i] = new Color32(v, v, v, 255);
                }
            }

            return pixels;
        }

        public static Color32 Blend(Color32 a, Color32 b, float t) => new Color32(
            (byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), 255);

        public static void DrawPolyline(Color32[] pixels, int n, float cell, Vector2[] points, Color32 colour)
        {
            if (points == null)
            {
                return;
            }

            for (var k = 0; k + 1 < points.Length; k++)
            {
                var length = (points[k + 1] - points[k]).magnitude;
                var steps = Mathf.Max(1, Mathf.CeilToInt(length / (cell * 0.5f)));
                for (var s = 0; s <= steps; s++)
                {
                    var p = Vector2.Lerp(points[k], points[k + 1], s / (float)steps);
                    var x = Mathf.RoundToInt(p.x / cell);
                    var z = Mathf.RoundToInt(p.y / cell);
                    if (x >= 0 && z >= 0 && x < n && z < n)
                    {
                        pixels[z * n + x] = colour;
                    }
                }
            }
        }

        public static void Disc(Color32[] pixels, int n, int index, int radius, Color32 colour)
        {
            if (index < 0)
            {
                return;
            }

            var cx = index % n;
            var cz = index / n;
            for (var dz = -radius; dz <= radius; dz++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    var x = cx + dx;
                    var z = cz + dz;
                    if (dx * dx + dz * dz <= radius * radius && x >= 0 && z >= 0 && x < n && z < n)
                    {
                        pixels[z * n + x] = colour;
                    }
                }
            }
        }

        public static void WritePng(string path, Color32[] pixels, int width)
        {
            var texture = new Texture2D(width, pixels.Length / width, TextureFormat.RGBA32, false);
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
    }
}
#endif
