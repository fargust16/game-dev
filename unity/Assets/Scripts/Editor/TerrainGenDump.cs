#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Headless inspection harness for the procedural terrain generator: PNG dumps, a statistics
    /// report and an in-process determinism check, for a fixed set of seeds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built before the geography it inspects, deliberately. Every stage of this generator is judged
    /// by images and numbers, and the repository's own record - the banding investigation, eight full
    /// bakes to kill eight hypotheses - is what building the harness last costs.
    /// </para>
    /// <para>
    /// It writes outside the project by default. These are diagnostics, not assets: dropping a
    /// megabyte of PNG into <c>Assets/</c> would trigger an import, add untracked files and put
    /// generator output into the same place as authored content.
    /// </para>
    /// <para>
    /// Nothing here touches <c>TerrainData</c>, the scene, or any downstream system. The
    /// generator produces a height field and stops.
    /// </para>
    /// </remarks>
    public static class TerrainGenDump
    {
        /// <summary>Fixed seeds, so two runs are comparable and a regression is visible as a diff.</summary>
        public static readonly int[] DefaultSeeds = { 110201, 424242, 20260905, 7, 1337 };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        // ---------- entry points ----------

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 1 (Default Seeds)", false, 200)]
        public static void DumpStage1DefaultSeedsMenu()
        {
            var directory = Path.Combine(Application.temporaryCachePath, "TerrainGenStage1");
            var report = RunStage1(DefaultSeeds, TerrainGenSettings.Default, directory);
            Debug.Log($"[TerrainGen] Stage 1 dump written to {directory}\n{report}");
        }

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 2 (Default Seeds)", false, 201)]
        public static void DumpStage2DefaultSeedsMenu()
        {
            var directory = Path.Combine(Application.temporaryCachePath, "TerrainGenStage2");
            var report = RunStage2(DefaultSeeds, TerrainGenSettings.Default, directory);
            Debug.Log($"[TerrainGen] Stage 2 dump written to {directory}\n{report}");
        }

        /// <summary>Batch entry: <c>-terrainGenOut &lt;dir&gt;</c>, <c>-terrainGenSeeds 1,2,3</c>.</summary>
        public static void DumpStage1Batch()
        {
            var directory = ReadOutputDirectory("TerrainGenStage1");
            var report = RunStage1(ReadSeeds(), TerrainGenSettings.Default, directory);
            Debug.Log($"[TerrainGen] Stage 1 batch dump written to {directory}\n{report}");
        }

        /// <summary>Batch entry for Stage 2, same arguments.</summary>
        public static void DumpStage2Batch()
        {
            var directory = ReadOutputDirectory("TerrainGenStage2");
            var report = RunStage2(ReadSeeds(), TerrainGenSettings.Default, directory);
            Debug.Log($"[TerrainGen] Stage 2 batch dump written to {directory}\n{report}");
        }

        /// <summary>
        /// Runs one seed phase by phase, logging after each. For finding where a run stops.
        /// </summary>
        /// <remarks>
        /// A generator that dies mid-run leaves no report, which makes the report useless exactly
        /// when it is needed. This walks the same phases the generators walk and logs between them,
        /// so the failing phase is named rather than guessed.
        /// </remarks>
        public static void ProbePhases()
        {
            var settings = TerrainGenSettings.Default;
            settings.Clamp();

            foreach (var seed in ReadSeeds())
            {
                ProbeSeed(seed, settings);
            }
        }

        private static void ProbeSeed(int seed, TerrainGenSettings settings)
        {
            var worldSeed = new GameDev.World.Seed.WorldSeed(seed);

            Debug.Log($"[TerrainGenProbe] seed={seed} phase=start");

            var partition = CrustalPartition.Build(worldSeed, settings);
            Debug.Log($"[TerrainGenProbe] phase=partition sites={partition.SiteCount} "
                      + $"raster={partition.RasterWidth}x{partition.RasterHeight}");

            var landmass = LandmassBuilder.Build(worldSeed, settings, partition);
            Debug.Log($"[TerrainGenProbe] phase=landmass rings={landmass.CoastRings.Count} "
                      + $"cells={landmass.LandSites.Count} islets={landmass.IsletCount} "
                      + $"components={landmass.ComponentCount} rasterLand={landmass.RasterLandFraction:F3}");

            var coast = new CoastQuery(landmass.CoastRings, settings.MaxCoastDistanceMeters);
            Debug.Log($"[TerrainGenProbe] phase=coastquery segments={coast.SegmentCount}");

            var edges = CrustalEdges.Build(partition, landmass.LandSites, coast);
            Debug.Log($"[TerrainGenProbe] phase=crustaledges edges={edges.Edges.Length} "
                      + $"vertices={edges.VertexCount}");

            var ranges = UpliftBeltBuilder.Build(worldSeed, settings, edges, coast, out var beltReport);
            Debug.Log($"[TerrainGenProbe] phase=belts ranges={ranges.Count} {beltReport}");
            for (var i = 0; i < ranges.Count; i++)
            {
                var r = ranges[i];
                Debug.Log($"[TerrainGenProbe] range {i} parent={r.ParentIndex} "
                          + $"spine={r.Spine.Length}pts len={r.LengthMeters:F0}m "
                          + $"crest={r.CrestHeightMeters:F0}m halfWidth={r.HalfWidthMeters:F0}m "
                          + $"asym={r.Asymmetry:F2} steepSide={r.SteepSide}");
            }

            Debug.Log($"[TerrainGenProbe] lastRelax {UpliftBeltBuilder.LastRelaxReport}");

            var full = Stage2MountainGenerator.Generate(seed, settings);
            Debug.Log($"[TerrainGenProbe] phase=field {full.Field.Width}x{full.Field.Height} "
                      + $"hash={full.Stats.FieldHash} h=[{full.Stats.MinHeightMeters:F1}.."
                      + $"{full.Stats.MaxHeightMeters:F1}] uplift=[{full.Stats.UpliftMinMeters:F1}.."
                      + $"{full.Stats.UpliftMaxMeters:F1}] oceanChanged={full.Stats.OceanSamplesChanged}");

            ProbeWorstStep(full);
            Debug.Log("[TerrainGenProbe] phase=done");
        }

        /// <summary>
        /// Finds the largest step between adjacent uplift samples and prints both sides in the
        /// range's own coordinates.
        /// </summary>
        /// <remarks>
        /// A discontinuity in the uplift field means the belt's coordinate chart folded somewhere,
        /// and the useful question is which quantity jumped - the position along the axis, the
        /// distance from it, or the width. Reading that off the two samples either side names the
        /// cause instead of inviting another guess.
        /// </remarks>
        private static void ProbeWorstStep(Stage2Result result)
        {
            var field = result.UpliftField;
            var worst = 0f;
            var worstIndex = -1;
            var worstNeighbour = -1;

            for (var z = 0; z < field.Height; z++)
            {
                var row = z * field.Width;
                for (var x = 1; x < field.Width; x++)
                {
                    var step = Mathf.Abs(field.Heights[row + x] - field.Heights[row + x - 1]);
                    if (step > worst)
                    {
                        worst = step;
                        worstIndex = row + x;
                        worstNeighbour = row + x - 1;
                    }
                }
            }

            for (var z = 1; z < field.Height; z++)
            {
                var row = z * field.Width;
                for (var x = 0; x < field.Width; x++)
                {
                    var step = Mathf.Abs(field.Heights[row + x] - field.Heights[row - field.Width + x]);
                    if (step > worst)
                    {
                        worst = step;
                        worstIndex = row + x;
                        worstNeighbour = row - field.Width + x;
                    }
                }
            }

            if (worstIndex < 0)
            {
                Debug.Log("[TerrainGenProbe] worst uplift step: none");
                return;
            }

            Debug.Log($"[TerrainGenProbe] worst uplift step {worst:F2} m at index {worstIndex}");
            DescribeSample(result, worstIndex, "hi");
            DescribeSample(result, worstNeighbour, "lo");
        }

        private static void DescribeSample(Stage2Result result, int index, string label)
        {
            var field = result.UpliftField;
            var x = index % field.Width;
            var z = index / field.Width;
            var worldX = field.WorldX(x);
            var worldZ = field.WorldZ(z);

            var text = $"[TerrainGenProbe]   {label} ({worldX:F1},{worldZ:F1}) "
                       + $"uplift={field.Heights[index]:F2} "
                       + $"signedCoast={result.Stage1.SignedDistance[index]:F1}";

            for (var r = 0; r < result.Structure.Ranges.Count; r++)
            {
                var range = result.Structure.Ranges[r];
                range.Project(worldX, worldZ, out var t, out var offset);
                text += $" | range{r} t={t:F4} offset={offset:F1} "
                        + $"hw={range.HalfWidthAt(t):F1} crest={range.CrestAt(t):F1} "
                        + $"eval={range.Evaluate(worldX, worldZ):F2}";
            }

            Debug.Log(text);
        }

        // ---------- stage runs ----------

        /// <summary>Generates every seed at Stage 1, writes the PNGs and returns the text report.</summary>
        public static string RunStage1(int[] seeds, TerrainGenSettings settings, string directory)
        {
            Directory.CreateDirectory(directory);

            var sb = Header("Stage 1 (landmass and coastline)", settings);
            var lines = new List<string>(seeds.Length);

            foreach (var seed in seeds)
            {
                Debug.Log($"[TerrainGen] stage 1, generating seed {seed}");
                var result = Stage1LandmassGenerator.Generate(seed, settings);
                var second = Stage1LandmassGenerator.Generate(seed, settings);
                var stable = result.Stats.FieldHash == second.Stats.FieldHash;

                WritePng(
                    Path.Combine(directory, $"stage1_seed{seed}_height.png"),
                    RenderHeight(result.Field, settings.MaxLandHeightMeters));
                WritePng(
                    Path.Combine(directory, $"stage1_seed{seed}_slope.png"),
                    RenderSlope(result.Field));
                WritePng(
                    Path.Combine(directory, $"stage1_seed{seed}_structure.png"),
                    RenderStructure(result, null));

                sb.Append(result.Stats.ToReport());
                sb.AppendLine($"  repeat     {(stable ? "identical" : "DIFFERENT - " + second.Stats.FieldHash)}");
                sb.AppendLine();
                lines.Add(result.Stats.ToLine());
            }

            return Finish(sb, lines, directory, "stage1_stats.txt");
        }

        /// <summary>Generates every seed at Stage 2, writes the PNGs and returns the text report.</summary>
        public static string RunStage2(int[] seeds, TerrainGenSettings settings, string directory)
        {
            Directory.CreateDirectory(directory);

            var sb = Header("Stage 2 (structural mountain uplift)", settings);
            sb.AppendLine($"# crest {settings.CrestHeightMinMeters:F0}-{settings.CrestHeightMaxMeters:F0} m, "
                          + $"half-width {settings.RangeHalfWidthMinMeters:F0}-{settings.RangeHalfWidthMaxMeters:F0} m, "
                          + $"coast gate {settings.UpliftCoastGateMeters:F0} m, "
                          + $"ceiling {settings.MaxUpliftMeters:F0} m");
            sb.AppendLine();

            var lines = new List<string>(seeds.Length);

            foreach (var seed in seeds)
            {
                Debug.Log($"[TerrainGen] stage 2, generating seed {seed}");
                var result = Stage2MountainGenerator.Generate(seed, settings);
                var second = Stage2MountainGenerator.Generate(seed, settings);
                var stable = result.Stats.FieldHash == second.Stats.FieldHash;

                var top = Mathf.Max(
                    WorldScale.SeaLevelMeters + 1f, result.Stats.MaxHeightMeters);

                WritePng(
                    Path.Combine(directory, $"stage2_seed{seed}_height.png"),
                    RenderHeight(result.Field, top));
                WritePng(
                    Path.Combine(directory, $"stage2_seed{seed}_uplift.png"),
                    RenderUplift(result.UpliftField));
                WritePng(
                    Path.Combine(directory, $"stage2_seed{seed}_slope.png"),
                    RenderSlope(result.Field));
                WritePng(
                    Path.Combine(directory, $"stage2_seed{seed}_structure.png"),
                    RenderStructure(result.Stage1, result.Structure.Ranges));

                sb.Append(result.Stats.ToReport());
                sb.Append(result.Stats.ToStage2Report());
                for (var i = 0; i < result.Structure.Ranges.Count; i++)
                {
                    var r = result.Structure.Ranges[i];
                    var m = result.Metrics[i];
                    var kind = r.ParentIndex < 0 ? "system" : $"branch of {r.ParentIndex}";
                    var peaks = m.SummitPositions.Length == 0
                        ? "-"
                        : string.Join("/", System.Array.ConvertAll(m.SummitPositions, v => v.ToString("F2")));

                    sb.AppendLine($"  range {i}    {kind}: spine {r.LengthMeters:F0} m "
                                  + $"({r.Spine.Length} pts), half-width {m.CharacteristicHalfWidthMeters:F0} m "
                                  + $"(nominal {r.HalfWidthMeters:F0}), L/W {m.LengthToWidthRatio:F2}, "
                                  + $"elongation {m.ReliefElongation:F2}");
                    sb.AppendLine($"             crest {r.CrestHeightMeters:F0} m, {m.MajorSummits} summit(s) "
                                  + $"at t={peaks}, lowest saddle {m.MinSaddleFraction:F2} of peak, "
                                  + $"asym {r.Asymmetry:F2} steep {(r.SteepSide > 0 ? "+" : "-")}, "
                                  + $"curvature-capped {(m.CurvatureLimitedFraction * 100f):F0} %, "
                                  + $"{r.SourceSitePairs.Length / 2} crustal boundaries");
                }

                sb.AppendLine($"  repeat     {(stable ? "identical" : "DIFFERENT - " + second.Stats.FieldHash)}");
                sb.AppendLine();
                lines.Add(result.Stats.ToLine());
            }

            return Finish(sb, lines, directory, "stage2_stats.txt");
        }

        // ---------- plumbing ----------

        private static StringBuilder Header(string title, TerrainGenSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Terrain generator - {title}");
            sb.AppendLine($"# world {WorldScale.WorldExtentXMeters:F0} x {WorldScale.WorldExtentZMeters:F0} m, "
                          + $"sea level {WorldScale.SeaLevelMeters:F0} m, "
                          + $"field {settings.FieldResolution} x {settings.FieldResolution}");
            sb.AppendLine();
            return sb;
        }

        private static string Finish(
            StringBuilder sb, List<string> lines, string directory, string fileName)
        {
            sb.AppendLine("# one line per seed");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            var text = sb.ToString();
            File.WriteAllText(Path.Combine(directory, fileName), text);
            return text;
        }

        private static string ReadOutputDirectory(string fallbackName)
        {
            var args = System.Environment.GetCommandLineArgs();
            return ReadArg(args, OutputArg)
                   ?? Path.Combine(Application.temporaryCachePath, fallbackName);
        }

        private static int[] ReadSeeds()
        {
            var args = System.Environment.GetCommandLineArgs();
            var seedArg = ReadArg(args, SeedsArg);
            if (string.IsNullOrEmpty(seedArg))
            {
                return DefaultSeeds;
            }

            var parts = seedArg.Split(',');
            var parsed = new List<int>(parts.Length);
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                {
                    parsed.Add(v);
                }
            }

            return parsed.Count > 0 ? parsed.ToArray() : DefaultSeeds;
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

        private static void WritePng(string path, Color32[] pixels)
        {
            var side = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false);
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

        // ---------- renderers ----------

        /// <summary>
        /// Hypsometric height map: bathymetry in blue, dry land from shore green to upland rock.
        /// </summary>
        private static Color32[] RenderHeight(WorldHeightField field, float topMeters)
        {
            var pixels = new Color32[field.Width * field.Height];
            var sea = WorldScale.SeaLevelMeters;
            var floor = sea - WorldScale.OceanShelfDepthMeters;
            var top = Mathf.Max(sea + 1f, topMeters);

            for (var i = 0; i < pixels.Length; i++)
            {
                var h = field.Heights[i];
                Color color;
                if (h <= sea)
                {
                    var t = Mathf.InverseLerp(floor, sea, h);
                    color = Color.Lerp(new Color(0.04f, 0.12f, 0.30f), new Color(0.30f, 0.66f, 0.80f), t);
                }
                else
                {
                    var t = Mathf.InverseLerp(sea, top, h);
                    if (t < 0.35f)
                    {
                        color = Color.Lerp(
                            new Color(0.85f, 0.82f, 0.60f), new Color(0.30f, 0.52f, 0.24f), t / 0.35f);
                    }
                    else if (t < 0.75f)
                    {
                        color = Color.Lerp(
                            new Color(0.30f, 0.52f, 0.24f), new Color(0.62f, 0.56f, 0.40f),
                            (t - 0.35f) / 0.40f);
                    }
                    else
                    {
                        color = Color.Lerp(
                            new Color(0.62f, 0.56f, 0.40f), new Color(0.96f, 0.96f, 0.94f),
                            (t - 0.75f) / 0.25f);
                    }
                }

                pixels[i] = color;
            }

            return pixels;
        }

        /// <summary>
        /// The uplift contribution alone, normalised to its own maximum.
        /// </summary>
        /// <remarks>
        /// This is the image that answers "is the mountain a belt or a blob" without the coastal
        /// model underneath it confusing the picture.
        /// </remarks>
        private static Color32[] RenderUplift(WorldHeightField upliftField)
        {
            var max = 0f;
            for (var i = 0; i < upliftField.Heights.Length; i++)
            {
                max = Mathf.Max(max, upliftField.Heights[i]);
            }

            var scale = max > 1e-4f ? 1f / max : 0f;
            var pixels = new Color32[upliftField.Heights.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var t = upliftField.Heights[i] * scale;
                var color = t <= 0f
                    ? new Color(0.06f, 0.06f, 0.10f)
                    : Color.Lerp(new Color(0.10f, 0.10f, 0.35f), new Color(1f, 0.95f, 0.75f), t);
                pixels[i] = color;
            }

            return pixels;
        }

        /// <summary>
        /// Slope map, normalised to the field's own maximum.
        /// </summary>
        /// <remarks>
        /// Adaptive rather than fixed-scale on purpose: Stage 1 relief is gentle - a 26 m rise over
        /// 200 m is a slope01 of about 0.008 - and a fixed scale calibrated for mountains would
        /// render the whole Stage 1 world black and hide the only structure it has.
        /// </remarks>
        private static Color32[] RenderSlope(WorldHeightField field)
        {
            var slopes = new float[field.Width * field.Height];
            var max = 0f;

            for (var z = 0; z < field.Height; z++)
            {
                for (var x = 0; x < field.Width; x++)
                {
                    var s = field.Slope01(x, z);
                    slopes[z * field.Width + x] = s;
                    max = Mathf.Max(max, s);
                }
            }

            var scale = max > 1e-6f ? 1f / max : 0f;
            var pixels = new Color32[slopes.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var v = (byte)Mathf.Clamp(Mathf.RoundToInt(slopes[i] * scale * 255f), 0, 255);
                pixels[i] = new Color32(v, v, v, 255);
            }

            return pixels;
        }

        /// <summary>
        /// The structural answer to "why is this here": the crustal partition, which cells became
        /// land, the published coastline, and the uplift belts drawn along the boundaries they follow.
        /// </summary>
        private static Color32[] RenderStructure(
            Stage1Result result, IReadOnlyList<MountainRange> ranges)
        {
            var field = result.Field;
            var partition = result.Structure.Partition;
            var pixels = new Color32[field.Width * field.Height];

            // Land comes from the raster the outline was traced from, not from site membership. The
            // enclosed-sea fill turns pockets the growth left behind into land, and colouring by site
            // would show those as sea here while the height map shows them as land.
            var landRaster = result.Landmass.LandRaster;

            for (var z = 0; z < field.Height; z++)
            {
                var pz = Mathf.Clamp(
                    (int)(field.WorldZ(z) / partition.CellMeters), 0, partition.RasterHeight - 1);
                for (var x = 0; x < field.Width; x++)
                {
                    var px = Mathf.Clamp(
                        (int)(field.WorldX(x) / partition.CellMeters), 0, partition.RasterWidth - 1);
                    var cell = pz * partition.RasterWidth + px;
                    var site = partition.Cells[cell];
                    var land = landRaster[cell];

                    var hue = (site * 0.6180339887f) % 1f;
                    pixels[z * field.Width + x] =
                        Color.HSVToRGB(hue, land ? 0.55f : 0.25f, land ? 0.85f : 0.30f);
                }
            }

            for (var z = 0; z < field.Height; z++)
            {
                var pz = Mathf.Clamp(
                    (int)(field.WorldZ(z) / partition.CellMeters), 0, partition.RasterHeight - 1);
                for (var x = 0; x < field.Width; x++)
                {
                    var px = Mathf.Clamp(
                        (int)(field.WorldX(x) / partition.CellMeters), 0, partition.RasterWidth - 1);
                    var here = partition.Cells[pz * partition.RasterWidth + px];
                    var right = px + 1 < partition.RasterWidth
                        ? partition.Cells[pz * partition.RasterWidth + px + 1]
                        : here;
                    var up = pz + 1 < partition.RasterHeight
                        ? partition.Cells[(pz + 1) * partition.RasterWidth + px]
                        : here;

                    if (here != right || here != up)
                    {
                        pixels[z * field.Width + x] = new Color32(30, 30, 34, 255);
                    }
                }
            }

            var white = new Color32(255, 255, 255, 255);
            for (var r = 0; r < result.Structure.CoastRings.Count; r++)
            {
                var ring = result.Structure.CoastRings[r];
                if (ring == null || ring.Length < 2)
                {
                    continue;
                }

                for (var i = 0; i < ring.Length; i++)
                {
                    DrawLine(pixels, field, ring[i], ring[(i + 1) % ring.Length], white, 0);
                }
            }

            if (ranges != null)
            {
                for (var r = 0; r < ranges.Count; r++)
                {
                    var range = ranges[r];
                    var color = range.ParentIndex < 0
                        ? new Color32(220, 30, 30, 255)
                        : new Color32(255, 150, 40, 255);
                    for (var i = 0; i + 1 < range.Spine.Length; i++)
                    {
                        DrawLine(pixels, field, range.Spine[i], range.Spine[i + 1], color, 1);
                    }
                }
            }

            return pixels;
        }

        private static void DrawLine(
            Color32[] pixels, WorldHeightField field, Vector2 a, Vector2 b, Color32 color, int radius)
        {
            var x0 = Mathf.RoundToInt(a.x / field.CellSize);
            var z0 = Mathf.RoundToInt(a.y / field.CellSize);
            var x1 = Mathf.RoundToInt(b.x / field.CellSize);
            var z1 = Mathf.RoundToInt(b.y / field.CellSize);

            var dx = Mathf.Abs(x1 - x0);
            var dz = -Mathf.Abs(z1 - z0);
            var sx = x0 < x1 ? 1 : -1;
            var sz = z0 < z1 ? 1 : -1;
            var err = dx + dz;
            var guard = dx - dz + 4;

            while (guard-- > 0)
            {
                for (var oz = -radius; oz <= radius; oz++)
                {
                    for (var ox = -radius; ox <= radius; ox++)
                    {
                        var px = x0 + ox;
                        var pz = z0 + oz;
                        if (px >= 0 && pz >= 0 && px < field.Width && pz < field.Height)
                        {
                            pixels[pz * field.Width + px] = color;
                        }
                    }
                }

                if (x0 == x1 && z0 == z1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 >= dz)
                {
                    err += dz;
                    x0 += sx;
                }

                if (e2 <= dx)
                {
                    err += dx;
                    z0 += sz;
                }
            }
        }
    }
}
#endif
