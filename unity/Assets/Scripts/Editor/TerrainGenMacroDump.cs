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
    /// Diagnostics for the Stage 2A macro structure pass: images and a text table, no height field.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="TerrainGenDump"/> on purpose. That tool renders height, slope and
    /// uplift, all of which need the 1025 x 1025 lattice; this one answers a geometry question and
    /// runs in about a fifth of a second a seed because it never evaluates one.
    /// </remarks>
    public static class TerrainGenMacroDump
    {
        public static readonly int[] DefaultSeeds = TerrainGenDump.DefaultSeeds;

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 2A Macro Structure (Default Seeds)", false, 202)]
        public static void DumpMacroDefaultSeedsMenu()
        {
            var report = RunMacro(
                DefaultSeeds,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage2a"));
            Debug.Log(report);
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenMacroDump.DumpMacroBatch</c></summary>
        public static void DumpMacroBatch()
        {
            var directory = ReadOutputDirectory("stage2a");
            var report = RunMacro(
                ReadSeeds(), TerrainGenSettings.Default, MacroStructureSettings.Default, directory);
            Debug.Log(report);
        }

        /// <summary>
        /// Re-runs Stage 1 in full and prints its field hashes, so the pass can prove it moved nothing.
        /// </summary>
        public static void VerifyStage1Batch()
        {
            var seeds = ReadSeeds();
            var settings = TerrainGenSettings.Default;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 1 hash verification");
            for (var i = 0; i < seeds.Length; i++)
            {
                var result = Stage1LandmassGenerator.Generate(seeds[i], settings);
                sb.AppendLine(
                    $"seed {seeds[i],9}  hash {result.Stats.FieldHash}  "
                    + $"land {result.Stats.FieldLandFraction.ToString("F4", CultureInfo.InvariantCulture)}  "
                    + $"coastKm {(result.Stats.CoastLengthMeters / 1000f).ToString("F2", CultureInfo.InvariantCulture)}");
            }

            var report = sb.ToString();
            Debug.Log(report);
        }

        public static string RunMacro(
            int[] seeds,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            string directory)
        {
            Directory.CreateDirectory(directory);

            var sb = new StringBuilder();
            sb.AppendLine("Stage 2A macro structure - candidate mountain axes, geometry only");
            sb.AppendLine($"  output {directory}");
            sb.AppendLine(
                $"  macro spacing {macroSettings.SiteSpacingMeters} m, "
                + $"fillet {macroSettings.MinFilletRadiusMeters}-{macroSettings.FilletRadiusMeters} m, "
                + $"convergence >= {macroSettings.ConvergenceThreshold}, "
                + $"target half-width {macroSettings.TargetHalfWidthMeters} m "
                + $"(needs reach {macroSettings.RequiredReachMeters:F0} m)");
            sb.AppendLine();

            var lines = new List<string>();
            for (var i = 0; i < seeds.Length; i++)
            {
                var result = Stage2AMacroStructure.Run(seeds[i], terrainSettings, macroSettings);
                sb.Append(Stage2AMacroStructure.Describe(result));
                sb.AppendLine();
                lines.Add(result.Stats.ToLine());

                var frame = WorldHeightField.ForWorld(terrainSettings.FieldResolution);
                var land = RasteriseLand(result.Coast, frame);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_macro.png"),
                    RenderMacro(result, frame, land),
                    frame.Width);

                WritePng(
                    Path.Combine(directory, $"seed_{seeds[i]}_scale.png"),
                    RenderScaleComparison(result, frame, land),
                    frame.Width);
            }

            sb.AppendLine("one line per seed:");
            for (var i = 0; i < lines.Count; i++)
            {
                sb.AppendLine("  " + lines[i]);
            }

            File.WriteAllText(Path.Combine(directory, "stage2a_report.txt"), sb.ToString());
            return sb.ToString();
        }

        // ---------- rendering ----------

        private static bool[] RasteriseLand(CoastQuery coast, WorldHeightField frame)
        {
            var land = new bool[frame.Width * frame.Height];
            var crossings = new List<float>(16);
            for (var z = 0; z < frame.Height; z++)
            {
                coast.RowCrossings(frame.WorldZ(z), crossings);
                var row = z * frame.Width;
                for (var x = 0; x < frame.Width; x++)
                {
                    land[row + x] =
                        CoastQuery.InsideFromSortedCrossings(crossings, frame.WorldX(x));
                }
            }

            return land;
        }

        /// <summary>
        /// Plates, their boundaries, and every candidate axis with the belt it could carry.
        /// </summary>
        /// <remarks>
        /// The translucent band is the tube of the axis's own usable half-width. Where an axis cannot
        /// carry the width it claims the band pinches or folds, which is the failure Stage 2 shipped
        /// and the one thing a spine drawn as a bare line will never show.
        /// </remarks>
        private static Color32[] RenderMacro(Stage2AResult result, WorldHeightField frame, bool[] land)
        {
            var pixels = new Color32[frame.Width * frame.Height];
            var plates = result.Plates;

            for (var z = 0; z < frame.Height; z++)
            {
                var wz = frame.WorldZ(z);
                var row = z * frame.Width;
                for (var x = 0; x < frame.Width; x++)
                {
                    var plate = plates.PlateAt(frame.WorldX(x), wz);
                    var hue = (plate * 0.6180339887f) % 1f;
                    pixels[row + x] = land[row + x]
                        ? Color.HSVToRGB(hue, 0.32f, 0.86f)
                        : Color.HSVToRGB(hue, 0.16f, 0.30f);
                }
            }

            // Belt footprints first, so the axis line and the coast stay readable on top of them.
            for (var i = 0; i < result.Candidates.Count; i++)
            {
                var axis = result.Candidates[i];
                if (!axis.Accepted)
                {
                    continue;
                }

                StampTube(pixels, frame, axis, axis.UsableHalfWidthMeters, new Color32(210, 60, 40, 255), 0.45f);
            }

            var divergent = new Color32(120, 120, 130, 255);
            var convergentColor = new Color32(40, 40, 150, 255);
            for (var e = 0; e < plates.Boundaries.Length; e++)
            {
                var boundary = plates.Boundaries[e];
                var strong = boundary.Convergence >= result.MacroSettings.ConvergenceThreshold;
                DrawLine(
                    pixels, frame, boundary.A, boundary.B,
                    strong ? convergentColor : divergent, strong ? 1 : 0);
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
                    DrawLine(pixels, frame, ring[i], ring[(i + 1) % ring.Length], white, 0);
                }
            }

            var rejected = new Color32(255, 170, 40, 255);
            var accepted = new Color32(190, 20, 20, 255);
            for (var i = 0; i < result.Candidates.Count; i++)
            {
                var axis = result.Candidates[i];
                if (axis.Accepted)
                {
                    for (var k = 0; k + 1 < axis.Points.Length; k++)
                    {
                        DrawLine(pixels, frame, axis.Points[k], axis.Points[k + 1], accepted, 2);
                    }

                    var left = axis.Offset(axis.UsableHalfWidthMeters);
                    var right = axis.Offset(-axis.UsableHalfWidthMeters);
                    var wall = new Color32(90, 10, 10, 255);
                    for (var k = 0; k + 1 < left.Length; k++)
                    {
                        DrawLine(pixels, frame, left[k], left[k + 1], wall, 0);
                        DrawLine(pixels, frame, right[k], right[k + 1], wall, 0);
                    }
                }
                else
                {
                    // Dashed, so a rejected candidate can never be mistaken for an accepted one.
                    for (var k = 0; k + 1 < axis.Points.Length; k++)
                    {
                        if ((k / 3) % 2 == 1)
                        {
                            continue;
                        }

                        DrawLine(pixels, frame, axis.Points[k], axis.Points[k + 1], rejected, 0);
                    }
                }
            }

            for (var i = 0; i < plates.Sites.Length; i++)
            {
                StampDisc(pixels, frame, plates.Sites[i], 12f, new Color32(255, 255, 255, 255));
                StampDisc(pixels, frame, plates.Sites[i], 7f, new Color32(20, 20, 25, 255));
                var tip = plates.Sites[i] + plates.Drift[i] * 90f;
                DrawLine(pixels, frame, plates.Sites[i], tip, new Color32(20, 20, 25, 255), 0);
            }

            return pixels;
        }

        /// <summary>
        /// The fine crustal boundary chain and the macro boundaries on the same image.
        /// </summary>
        /// <remarks>
        /// The whole argument of this pass in one picture: the thin dark web is what Stage 2 built its
        /// spines from, bending every hundred metres or so; the thick line is what a belt three hundred
        /// metres wide can actually follow.
        /// </remarks>
        private static Color32[] RenderScaleComparison(
            Stage2AResult result, WorldHeightField frame, bool[] land)
        {
            var pixels = new Color32[frame.Width * frame.Height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = land[i] ? new Color32(232, 230, 222, 255) : new Color32(52, 60, 74, 255);
            }

            var fine = CrustalEdges.Build(result.Partition, result.Structure.LandSites, result.Coast);
            var fineColor = new Color32(150, 150, 158, 255);
            for (var e = 0; e < fine.Edges.Length; e++)
            {
                DrawLine(pixels, frame, fine.Edges[e].A, fine.Edges[e].B, fineColor, 0);
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
                    DrawLine(pixels, frame, ring[i], ring[(i + 1) % ring.Length], white, 0);
                }
            }

            var macroColor = new Color32(40, 40, 150, 255);
            for (var e = 0; e < result.Plates.Boundaries.Length; e++)
            {
                var boundary = result.Plates.Boundaries[e];
                DrawLine(pixels, frame, boundary.A, boundary.B, macroColor, 1);
            }

            var accepted = new Color32(190, 20, 20, 255);
            for (var i = 0; i < result.Candidates.Count; i++)
            {
                var axis = result.Candidates[i];
                if (!axis.Accepted)
                {
                    continue;
                }

                for (var k = 0; k + 1 < axis.Points.Length; k++)
                {
                    DrawLine(pixels, frame, axis.Points[k], axis.Points[k + 1], accepted, 2);
                }
            }

            return pixels;
        }

        private static void StampTube(
            Color32[] pixels,
            WorldHeightField frame,
            MacroAxis axis,
            float halfWidthMeters,
            Color32 color,
            float strength)
        {
            for (var i = 0; i < axis.Points.Length; i++)
            {
                StampDiscBlend(pixels, frame, axis.Points[i], halfWidthMeters, color, strength);
            }
        }

        private static void StampDisc(
            Color32[] pixels, WorldHeightField frame, Vector2 centre, float radiusMeters, Color32 color)
        {
            StampDiscBlend(pixels, frame, centre, radiusMeters, color, 1f);
        }

        private static void StampDiscBlend(
            Color32[] pixels,
            WorldHeightField frame,
            Vector2 centre,
            float radiusMeters,
            Color32 color,
            float strength)
        {
            var radius = Mathf.Max(0, Mathf.RoundToInt(radiusMeters / frame.CellSize));
            var cx = Mathf.RoundToInt(centre.x / frame.CellSize);
            var cz = Mathf.RoundToInt(centre.y / frame.CellSize);
            var radiusSq = radius * radius;

            for (var dz = -radius; dz <= radius; dz++)
            {
                var pz = cz + dz;
                if (pz < 0 || pz >= frame.Height)
                {
                    continue;
                }

                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dz * dz > radiusSq)
                    {
                        continue;
                    }

                    var px = cx + dx;
                    if (px < 0 || px >= frame.Width)
                    {
                        continue;
                    }

                    var index = pz * frame.Width + px;
                    if (strength >= 1f)
                    {
                        pixels[index] = color;
                        continue;
                    }

                    var existing = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.r, color.r, strength)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.g, color.g, strength)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(existing.b, color.b, strength)),
                        255);
                }
            }
        }

        private static void DrawLine(
            Color32[] pixels, WorldHeightField frame, Vector2 a, Vector2 b, Color32 color, int radius)
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
                            pixels[pz * frame.Width + px] = color;
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
            var args = System.Environment.GetCommandLineArgs();
            var directory = ReadArg(args, OutputArg);
            return string.IsNullOrEmpty(directory)
                ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", fallbackName)
                : directory;
        }

        private static int[] ReadSeeds()
        {
            var args = System.Environment.GetCommandLineArgs();
            var raw = ReadArg(args, SeedsArg);
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
