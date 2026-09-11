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
    /// Diagnostics for the Stage 2B massif: a height map, an uplift map with the axis on it, and a
    /// width diagnostic.
    /// </summary>
    /// <remarks>
    /// Purpose-built and small. The acceptance question for this stage is whether the massif reads as
    /// a broad mountain body rather than a tube or a row of blobs, and that is answered by three
    /// images and a table, not by a visualisation framework.
    /// </remarks>
    public static class TerrainGenMassifDump
    {
        public static readonly int[] DefaultSeeds = TerrainGenDump.DefaultSeeds;

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 2B Massif (Default Seeds)", false, 203)]
        public static void DumpMassifDefaultSeedsMenu()
        {
            var report = RunMassif(
                DefaultSeeds,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage2b"));
            Debug.Log(report);
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenMassifDump.DumpMassifBatch</c></summary>
        public static void DumpMassifBatch()
        {
            var report = RunMassif(
                ReadSeeds(),
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                ReadOutputDirectory("stage2b"));
            Debug.Log(report);
        }

        public static string RunMassif(
            int[] seeds,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            MassifSettings massifSettings,
            string directory)
        {
            Directory.CreateDirectory(directory);

            var sb = new StringBuilder();
            sb.AppendLine("Stage 2B massif - broad mountain body on the accepted macro axis");
            sb.AppendLine($"  output {directory}");
            sb.AppendLine(
                $"  crest {terrainSettings.CrestHeightMinMeters}-{terrainSettings.CrestHeightMaxMeters} m, "
                + $"ceiling {terrainSettings.MaxUpliftMeters} m, "
                + $"coast gate {terrainSettings.UpliftCoastGateMeters} m, "
                + $"crown {massifSettings.CrownFraction}, "
                + $"width taper floor {massifSettings.WidthTaperFloor}");
            sb.AppendLine();

            var lines = new List<string>();
            for (var i = 0; i < seeds.Length; i++)
            {
                var result = Stage2BMassifGenerator.Generate(
                    seeds[i], terrainSettings, macroSettings, massifSettings);

                sb.Append(Stage2BMassifGenerator.Describe(result));
                sb.AppendLine();
                lines.Add(result.Stats.ToLine());

                var top = Mathf.Max(
                    result.Stats.MaxHeightMeters,
                    WorldScale.SeaLevelMeters + 1f);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_height.png"),
                    RenderHeight(result, top),
                    result.Field.Width);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_massif.png"),
                    RenderMassif(result),
                    result.Field.Width);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_width.png"),
                    RenderWidth(result),
                    result.Field.Width);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_shade.png"),
                    RenderContributionShade(result),
                    result.Field.Width);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_profiles.png"),
                    RenderProfiles(result),
                    result.Field.Width);

                sb.AppendLine(CrossSectionTable(result));
            }

            sb.AppendLine("one line per seed:");
            for (var i = 0; i < lines.Count; i++)
            {
                sb.AppendLine("  " + lines[i]);
            }

            File.WriteAllText(Path.Combine(directory, "stage2b_report.txt"), sb.ToString());
            return sb.ToString();
        }

        /// <summary>
        /// A text cross-section through each massif, so the profile can be read without an image.
        /// </summary>
        private static string CrossSectionTable(Stage2BResult result)
        {
            var c = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            for (var m = 0; m < result.Massifs.Count; m++)
            {
                var massif = result.Massifs[m];

                sb.AppendLine($"  massif {massif.Index} longitudinal profile (s -> crest m):");
                sb.Append("   ");
                for (var i = 0; i <= 20; i++)
                {
                    var s = i / 20f;
                    sb.Append($" {massif.CrestAt(s).ToString("F0", c)}");
                }

                sb.AppendLine();

                sb.AppendLine($"  massif {massif.Index} cross-section at the primary culmination (m from axis -> m):");
                var peakS = massif.CulminationT[0];
                for (var i = 1; i < massif.CulminationT.Length; i++)
                {
                    if (massif.CrestAt(massif.CulminationT[i]) > massif.CrestAt(peakS))
                    {
                        peakS = massif.CulminationT[i];
                    }
                }

                var centre = massif.PointAt(peakS);
                var ahead = massif.PointAt(Mathf.Min(1f, peakS + 0.01f));
                var behind = massif.PointAt(Mathf.Max(0f, peakS - 0.01f));
                var axis = (ahead - behind).normalized;
                var normal = new Vector2(-axis.y, axis.x);

                sb.Append("   ");
                for (var d = -400f; d <= 400f; d += 40f)
                {
                    var p = centre + normal * d;
                    var value = Sample(result.UpliftField, p.x, p.y);
                    sb.Append($" {value.ToString("F0", c)}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ---------- rendering ----------

        /// <summary>Hypsometric height map: bathymetry blue, land shore-green through upland rock.</summary>
        private static Color32[] RenderHeight(Stage2BResult result, float topMeters)
        {
            var field = result.Field;
            var pixels = new Color32[field.Width * field.Height];
            var sea = WorldScale.SeaLevelMeters;
            var floor = sea - WorldScale.OceanShelfDepthMeters;
            var top = Mathf.Max(sea + 1f, topMeters);

            for (var i = 0; i < pixels.Length; i++)
            {
                var h = field.Heights[i];
                if (h <= sea)
                {
                    var t = Mathf.InverseLerp(floor, sea, h);
                    pixels[i] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(12f, 96f, t)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(38f, 140f, t)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(84f, 178f, t)),
                        255);
                    continue;
                }

                var u = Mathf.Clamp01(Mathf.InverseLerp(sea, top, h));
                Color colour;
                if (u < 0.28f)
                {
                    colour = Color.Lerp(
                        new Color(0.78f, 0.80f, 0.60f), new Color(0.40f, 0.60f, 0.32f), u / 0.28f);
                }
                else if (u < 0.62f)
                {
                    colour = Color.Lerp(
                        new Color(0.40f, 0.60f, 0.32f), new Color(0.62f, 0.55f, 0.40f),
                        (u - 0.28f) / 0.34f);
                }
                else
                {
                    colour = Color.Lerp(
                        new Color(0.62f, 0.55f, 0.40f), new Color(0.96f, 0.96f, 0.96f),
                        (u - 0.62f) / 0.38f);
                }

                pixels[i] = colour;
            }

            Hillshade(pixels, field);
            DrawCoast(pixels, field, result, new Color32(255, 255, 255, 255));
            return pixels;
        }

        /// <summary>
        /// Shades the hypsometric map by slope, lit from the north-west.
        /// </summary>
        /// <remarks>
        /// Without it the palette decides the shape: a massif whose core all sits in the top colour
        /// band reads as a flat plateau however domed it actually is, and the acceptance question for
        /// this stage is exactly that distinction.
        /// </remarks>
        private static void Hillshade(Color32[] pixels, WorldHeightField field)
        {
            var light = new Vector3(-0.55f, 0.62f, -0.55f).normalized;
            var scale = 2f * field.CellSize;

            for (var z = 1; z + 1 < field.Height; z++)
            {
                var row = z * field.Width;
                for (var x = 1; x + 1 < field.Width; x++)
                {
                    var dx = (field.Heights[row + x + 1] - field.Heights[row + x - 1]) / scale;
                    var dz = (field.Heights[row + field.Width + x] - field.Heights[row - field.Width + x])
                             / scale;

                    var normal = new Vector3(-dx, 1f, -dz).normalized;
                    var lambert = Mathf.Clamp01(Vector3.Dot(normal, light) * 0.5f + 0.62f);

                    var c = pixels[row + x];
                    pixels[row + x] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.r * lambert), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.g * lambert), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(c.b * lambert), 0, 255),
                        255);
                }
            }
        }

        /// <summary>
        /// The massif contribution alone, with the axis over it and contours at fixed heights.
        /// </summary>
        /// <remarks>
        /// The contours are the width diagnostic in disguise: evenly spaced rings around a single
        /// centre would say "radial blob", rings hugging a line would say "tube". A broad massif shows
        /// long, roughly parallel contours down both flanks with rounded ends.
        /// </remarks>
        private static Color32[] RenderMassif(Stage2BResult result)
        {
            var uplift = result.UpliftField;
            var pixels = new Color32[uplift.Width * uplift.Height];
            var ceiling = Mathf.Max(1f, result.Stats.UpliftMaxMeters);

            for (var i = 0; i < pixels.Length; i++)
            {
                var value = uplift.Heights[i];
                if (value < Stage2BMassifGenerator.FootprintThresholdMeters)
                {
                    pixels[i] = new Color32(28, 32, 40, 255);
                    continue;
                }

                var u = Mathf.Clamp01(value / ceiling);
                pixels[i] = Color.Lerp(
                    new Color(0.13f, 0.20f, 0.34f), new Color(1f, 0.94f, 0.72f), u);
            }

            // Contour lines every 40 m, drawn by testing the neighbour band rather than by tracing.
            var contour = new Color32(40, 40, 46, 255);
            for (var z = 0; z + 1 < uplift.Height; z++)
            {
                for (var x = 0; x + 1 < uplift.Width; x++)
                {
                    var here = uplift.Heights[z * uplift.Width + x];
                    var right = uplift.Heights[z * uplift.Width + x + 1];
                    var up = uplift.Heights[(z + 1) * uplift.Width + x];
                    if (here < Stage2BMassifGenerator.FootprintThresholdMeters)
                    {
                        continue;
                    }

                    if (Mathf.FloorToInt(here / 40f) != Mathf.FloorToInt(right / 40f)
                        || Mathf.FloorToInt(here / 40f) != Mathf.FloorToInt(up / 40f))
                    {
                        pixels[z * uplift.Width + x] = contour;
                    }
                }
            }

            DrawCoast(pixels, uplift, result, new Color32(90, 120, 150, 255));
            DrawAxes(pixels, uplift, result, new Color32(220, 40, 40, 255));
            return pixels;
        }

        /// <summary>
        /// Width made visible: the body above half its local crest, its toe, and the nominal envelope.
        /// </summary>
        /// <remarks>
        /// Three bands rather than a colour ramp, because the acceptance question is a comparison of
        /// widths and a ramp makes that a matter of opinion. The nominal envelope is the width the
        /// geology asked for; the half-height band is what the field actually delivers.
        /// </remarks>
        private static Color32[] RenderWidth(Stage2BResult result)
        {
            var uplift = result.UpliftField;
            var pixels = new Color32[uplift.Width * uplift.Height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(24, 26, 32, 255);
            }

            for (var m = 0; m < result.Massifs.Count; m++)
            {
                var massif = result.Massifs[m];
                var support = massif.Support;
                var cell = uplift.CellSize;

                var xMin = Mathf.Max(0, Mathf.FloorToInt(support.xMin / cell));
                var xMax = Mathf.Min(uplift.Width - 1, Mathf.CeilToInt(support.xMax / cell));
                var zMin = Mathf.Max(0, Mathf.FloorToInt(support.yMin / cell));
                var zMax = Mathf.Min(uplift.Height - 1, Mathf.CeilToInt(support.yMax / cell));

                for (var z = zMin; z <= zMax; z++)
                {
                    var worldZ = uplift.WorldZ(z);
                    for (var x = xMin; x <= xMax; x++)
                    {
                        var worldX = uplift.WorldX(x);
                        var index = z * uplift.Width + x;
                        var value = uplift.Heights[index];

                        massif.Project(worldX, worldZ, out var s, out var offset);
                        var nominal = massif.HalfWidthAt(s);
                        var crest = massif.CrestAt(s);

                        if (Mathf.Abs(offset) <= nominal && crest > 1f)
                        {
                            pixels[index] = new Color32(58, 62, 78, 255);
                        }

                        if (value < Stage2BMassifGenerator.FootprintThresholdMeters || crest <= 1f)
                        {
                            continue;
                        }

                        pixels[index] = value >= crest * 0.5f
                            ? new Color32(232, 196, 108, 255)
                            : new Color32(96, 116, 140, 255);
                    }
                }
            }

            DrawCoast(pixels, uplift, result, new Color32(70, 90, 110, 255));
            DrawAxes(pixels, uplift, result, new Color32(220, 40, 40, 255));
            return pixels;
        }

        /// <summary>
        /// The massif contribution alone, hillshaded, with Stage 1 excluded.
        /// </summary>
        /// <remarks>
        /// Shading the combined field cannot say which layer a crease belongs to, and Stage 1 is
        /// frozen. This isolates Stage 2B's own surface so a defect in it can be told apart from one
        /// it merely inherited.
        /// </remarks>
        private static Color32[] RenderContributionShade(Stage2BResult result)
        {
            var uplift = result.UpliftField;
            var pixels = new Color32[uplift.Width * uplift.Height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = uplift.Heights[i] >= Stage2BMassifGenerator.FootprintThresholdMeters
                    ? new Color32(206, 200, 188, 255)
                    : new Color32(38, 42, 52, 255);
            }

            Hillshade(pixels, uplift);
            DrawCoast(pixels, uplift, result, new Color32(70, 100, 130, 255));
            return pixels;
        }

        /// <summary>
        /// Three charts on one sheet: the longitudinal profile with its widths, the axis curvature,
        /// and a fan of cross-sections.
        /// </summary>
        /// <remarks>
        /// The plan views say where the massif is; these say what shape it is. A width that tracks the
        /// model is a flat fidelity line here, a truncation is a notch, and a cross-section that has
        /// gone tubular or grown a shoulder shows up directly rather than by inference.
        /// </remarks>
        private static Color32[] RenderProfiles(Stage2BResult result)
        {
            var side = result.Field.Width;
            var pixels = new Color32[side * side];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(24, 26, 32, 255);
            }

            if (result.Massifs.Count == 0 || result.Stations.Count == 0)
            {
                return pixels;
            }

            var massif = result.Massifs[0];
            var stations = result.Stations[0];

            var panel = side / 3;
            var grid = new Color32(52, 56, 66, 255);

            // ---- panel 1: longitudinal height and widths ----
            PanelFrame(pixels, side, 0, panel, grid);
            var topHeight = Mathf.Max(massif.CrestHeightMeters, 1f);
            var topWidth = Mathf.Max(massif.HalfWidthMeters * 2.4f, 1f);

            PlotCurve(pixels, side, 0, panel, 400, new Color32(240, 210, 120, 255),
                t => massif.CrestAt(t) / topHeight);
            PlotCurve(pixels, side, 0, panel, 400, new Color32(120, 170, 230, 255),
                t => massif.HalfWidthAt(t) * 2f / topWidth);
            PlotStations(pixels, side, 0, panel, stations, new Color32(230, 120, 110, 255),
                r => r.HalfHeightWidthMeters / topWidth);
            PlotStations(pixels, side, 0, panel, stations, new Color32(120, 220, 150, 255),
                r => r.ToeWidthMeters / topWidth);

            for (var i = 0; i < massif.CulminationT.Length; i++)
            {
                var x = Mathf.Clamp(Mathf.RoundToInt(massif.CulminationT[i] * (side - 1)), 0, side - 1);
                for (var y = side - panel; y < side; y++)
                {
                    pixels[y * side + x] = new Color32(255, 255, 255, 255);
                }
            }

            // ---- panel 2: axis curvature ----
            PanelFrame(pixels, side, panel, panel, grid);
            var topCurvature = Mathf.Max(massif.MaxCurvature(), 1e-5f);
            PlotCurve(pixels, side, panel, panel, 400, new Color32(200, 150, 240, 255),
                t => massif.CurvatureAt(t) / topCurvature);

            // The curvature a body of this width can carry before it folds.
            var limit = 1f / Mathf.Max(1f, massif.HalfWidthMeters / MountainRange.CurvatureWidthFactor);
            PlotCurve(pixels, side, panel, panel, 400, new Color32(230, 90, 80, 255),
                _ => limit / topCurvature);

            // ---- panel 3: cross-sections ----
            PanelFrame(pixels, side, panel * 2, side - panel * 2, grid);
            var sections = new[] { 0.15f, 0.3f, 0.45f, 0.6f, 0.75f, 0.9f };
            var span = massif.HalfWidthMeters * 2.2f;
            for (var k = 0; k < sections.Length; k++)
            {
                var s = sections[k];
                var centre = massif.PointAt(s);
                var ahead = massif.PointAt(Mathf.Min(1f, s + 0.01f));
                var behind = massif.PointAt(Mathf.Max(0f, s - 0.01f));
                var axis = ahead - behind;
                var normal = axis.sqrMagnitude < 1e-8f
                    ? Vector2.up
                    : new Vector2(-axis.normalized.y, axis.normalized.x);

                var hue = k / (float)sections.Length;
                var colour = (Color32)Color.HSVToRGB(hue * 0.75f, 0.55f, 1f);

                PlotCurve(pixels, side, panel * 2, side - panel * 2, 400, colour, t =>
                {
                    var d = Mathf.Lerp(-span, span, t);
                    var p = centre + normal * d;
                    return Sample(result.UpliftField, p.x, p.y) / topHeight;
                });
            }

            return pixels;
        }

        private static void PanelFrame(
            Color32[] pixels, int side, int top, int height, Color32 colour)
        {
            // Index zero is the bottom row of the texture, so a panel `top` rows down from the top of
            // the image has its baseline at `side - top - height` and grows upward from there.
            var baseline = side - top - height;
            for (var x = 0; x < side; x++)
            {
                var y = Mathf.Clamp(baseline, 0, side - 1);
                pixels[y * side + x] = colour;
            }

            for (var q = 1; q < 4; q++)
            {
                var y = baseline + height * q / 4;
                if (y < 0 || y >= side)
                {
                    continue;
                }

                for (var x = 0; x < side; x += 4)
                {
                    pixels[y * side + x] = colour;
                }
            }
        }

        private static void PlotCurve(
            Color32[] pixels, int side, int top, int height, int samples,
            Color32 colour, System.Func<float, float> value)
        {
            var previous = -1;
            for (var i = 0; i <= samples; i++)
            {
                var t = i / (float)samples;
                var x = Mathf.Clamp(Mathf.RoundToInt(t * (side - 1)), 0, side - 1);
                var v = Mathf.Clamp01(value(t));
                var y = side - top - height + Mathf.RoundToInt(v * (height - 2));
                if (y < 0 || y >= side)
                {
                    continue;
                }

                pixels[y * side + x] = colour;
                if (previous >= 0 && Mathf.Abs(y - previous) > 1)
                {
                    var lo = Mathf.Min(y, previous);
                    var hi = Mathf.Max(y, previous);
                    for (var fill = lo; fill <= hi; fill++)
                    {
                        if (fill >= 0 && fill < side)
                        {
                            pixels[fill * side + x] = colour;
                        }
                    }
                }

                previous = y;
            }
        }

        private static void PlotStations(
            Color32[] pixels, int side, int top, int height,
            GameDev.World.TerrainGen.MassifStation[] stations,
            Color32 colour, System.Func<GameDev.World.TerrainGen.MassifStation, float> value)
        {
            for (var i = 0; i < stations.Length; i++)
            {
                var x = Mathf.Clamp(Mathf.RoundToInt(stations[i].S * (side - 1)), 0, side - 1);
                var v = Mathf.Clamp01(value(stations[i]));
                var y = side - top - height + Mathf.RoundToInt(v * (height - 2));

                for (var oy = -2; oy <= 2; oy++)
                {
                    for (var ox = -2; ox <= 2; ox++)
                    {
                        var px = x + ox;
                        var py = y + oy;
                        if (px >= 0 && px < side && py >= 0 && py < side)
                        {
                            pixels[py * side + px] = colour;
                        }
                    }
                }
            }
        }

        private static void DrawCoast(
            Color32[] pixels, WorldHeightField frame, Stage2BResult result, Color32 colour)
        {
            var rings = result.Stage1.Structure.CoastRings;
            for (var r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                if (ring == null || ring.Length < 2)
                {
                    continue;
                }

                for (var i = 0; i < ring.Length; i++)
                {
                    DrawLine(pixels, frame, ring[i], ring[(i + 1) % ring.Length], colour, 0);
                }
            }
        }

        private static void DrawAxes(
            Color32[] pixels, WorldHeightField frame, Stage2BResult result, Color32 colour)
        {
            for (var m = 0; m < result.Massifs.Count; m++)
            {
                var points = result.Massifs[m].Axis.Points;
                for (var i = 0; i + 1 < points.Length; i++)
                {
                    DrawLine(pixels, frame, points[i], points[i + 1], colour, 1);
                }
            }
        }

        private static float Sample(WorldHeightField field, float worldX, float worldZ)
        {
            var x = Mathf.RoundToInt(worldX / field.CellSize);
            var z = Mathf.RoundToInt(worldZ / field.CellSize);
            if (x < 0 || z < 0 || x >= field.Width || z >= field.Height)
            {
                return 0f;
            }

            return field.Heights[z * field.Width + x];
        }

        private static void DrawLine(
            Color32[] pixels, WorldHeightField frame, Vector2 a, Vector2 b, Color32 colour, int radius)
        {
            var x0 = Mathf.RoundToInt(a.x / frame.CellSize);
            var z0 = Mathf.RoundToInt(a.y / frame.CellSize);
            var x1 = Mathf.RoundToInt(b.x / frame.CellSize);
            var z1 = Mathf.RoundToInt(b.y / frame.CellSize);

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
                        if (px >= 0 && pz >= 0 && px < frame.Width && pz < frame.Height)
                        {
                            pixels[pz * frame.Width + px] = colour;
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

        // ---------- plumbing ----------

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
                return DefaultSeeds;
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

            return seeds.Count > 0 ? seeds.ToArray() : DefaultSeeds;
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
