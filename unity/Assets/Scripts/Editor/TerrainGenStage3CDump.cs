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
    /// Diagnostics for Stage 3C: the carve field and the surface it leaves.
    /// </summary>
    /// <remarks>
    /// The design calls carving the first visually judgeable result of Stage 3, so these are the
    /// pictures that decide whether the structure was worth building: the carve alone, the surface
    /// under a hillshade, and the slope bands the relief principles set targets for. All three read
    /// the production <see cref="Stage3Field"/>, never a separate approximation.
    /// </remarks>
    public static class TerrainGenStage3CDump
    {
        public static readonly int[] AuditSeeds = { 110201, 424242, 20260905, 7, 101010, 20261231 };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3C Carve + Surface", false, 208)]
        public static void DumpMenu()
        {
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3c")));
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenStage3CDump.DumpBatch</c></summary>
        public static void DumpBatch() => Debug.Log(Run(ReadSeeds(), ReadOutputDirectory("stage3c")));

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3C: carve field and the Stage 3 surface");
            sb.AppendLine();

            var terrain = TerrainGenSettings.Default;
            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var ridgeSettings = RidgeSettings.Default;

            foreach (var seed in seeds)
            {
                var stage2b = Stage2BMassifGenerator.Generate(seed, terrain, macro, massifSettings);
                var partition = Stage3ASkeleton.Build(stage2b, ridgeSettings);
                var ridges = RidgeSkeleton.Build(
                    partition, ridgeSettings, new GameDev.World.Seed.WorldSeed(seed), stage2b.Stage1);
                var stage3 = Stage3Field.Build(stage2b, ridges, ridgeSettings);

                var side = stage3.Field.Width;
                WritePng(
                    Path.Combine(directory, $"seed_{seed}_carve.png"),
                    RenderCarve(stage3), side);
                WritePng(
                    Path.Combine(directory, $"seed_{seed}_surface.png"),
                    RenderSurface(stage3, stage2b), side);
                WritePng(
                    Path.Combine(directory, $"seed_{seed}_slope.png"),
                    RenderSlope(stage3, stage2b), side);

                sb.AppendLine(ridges.Stats.ToLine());
                sb.AppendLine("    " + stage3.Stats.ToLine());
                sb.AppendLine(
                    "    stage2Hash=" + stage2b.Stats.FieldHash
                    + " stage3Hash=" + stage3.Stats.FieldHash
                    + " carveHash=" + stage3.Stats.CarveHash
                    + " clamped=" + stage3.Stats.DepthClampedSamples.ToString(culture));
            }

            File.WriteAllText(Path.Combine(directory, "stage3c_report.txt"), sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        // ---------- renders ----------

        private static Color32[] RenderCarve(Stage3Field stage3)
        {
            var carve = stage3.Carve;
            var pixels = new Color32[carve.Width * carve.Height];
            var peak = Mathf.Max(1f, stage3.Stats.MaxCarveMeters);

            for (var i = 0; i < pixels.Length; i++)
            {
                var depth = carve.Heights[i];
                if (depth <= 0.01f)
                {
                    pixels[i] = new Color32(20, 24, 32, 255);
                    continue;
                }

                var u = Mathf.Clamp01(depth / peak);

                // Contours every five metres, so the shape of the cut is readable rather than a blur.
                var band = Mathf.RoundToInt(depth / 5f);
                var onContour = Mathf.Abs(depth - band * 5f) < 0.35f;

                var r = (byte)Mathf.RoundToInt(Mathf.Lerp(40f, 250f, u));
                var g = (byte)Mathf.RoundToInt(Mathf.Lerp(70f, 210f, u * u));
                var b = (byte)Mathf.RoundToInt(Mathf.Lerp(120f, 60f, u));
                pixels[i] = onContour ? new Color32(255, 255, 255, 255) : new Color32(r, g, b, 255);
            }

            return pixels;
        }

        private static Color32[] RenderSurface(Stage3Field stage3, Stage2BResult stage2b)
        {
            var field = stage3.Field;
            var pixels = new Color32[field.Width * field.Height];
            var sea = WorldScale.SeaLevelMeters;

            var highest = 0f;
            for (var i = 0; i < field.Heights.Length; i++)
            {
                highest = Mathf.Max(highest, field.Heights[i]);
            }

            for (var z = 0; z < field.Height; z++)
            {
                for (var x = 0; x < field.Width; x++)
                {
                    var i = z * field.Width + x;
                    var h = field.Heights[i];

                    if (h <= sea)
                    {
                        pixels[i] = new Color32(18, 30, 52, 255);
                        continue;
                    }

                    var u = Mathf.Clamp01((h - sea) / Mathf.Max(1f, highest - sea));
                    var r = Mathf.Lerp(96f, 236f, u);
                    var g = Mathf.Lerp(132f, 224f, Mathf.Sqrt(u));
                    var b = Mathf.Lerp(88f, 208f, u * u);

                    // Hillshade from the north-west, the convention the other dumps use.
                    var xp = Mathf.Min(field.Width - 1, x + 1);
                    var zp = Mathf.Min(field.Height - 1, z + 1);
                    var dx = (field.Heights[z * field.Width + xp] - h) / field.CellSize;
                    var dz = (field.Heights[zp * field.Width + x] - h) / field.CellSize;
                    var shade = Mathf.Clamp01(0.62f + 0.38f * (dx + dz) / Mathf.Sqrt(
                        1f + dx * dx + dz * dz) * 1.6f);

                    pixels[i] = new Color32(
                        (byte)Mathf.Clamp(r * shade, 0f, 255f),
                        (byte)Mathf.Clamp(g * shade, 0f, 255f),
                        (byte)Mathf.Clamp(b * shade, 0f, 255f),
                        255);
                }
            }

            return pixels;
        }

        private static Color32[] RenderSlope(Stage3Field stage3, Stage2BResult stage2b)
        {
            var field = stage3.Field;
            var pixels = new Color32[field.Width * field.Height];
            var coast = stage2b.Stage1.Coast;
            var crossings = new List<float>(16);

            for (var z = 0; z < field.Height; z++)
            {
                coast.RowCrossings(field.WorldZ(z), crossings);
                for (var x = 0; x < field.Width; x++)
                {
                    var i = z * field.Width + x;
                    if (!CoastQuery.InsideFromSortedCrossings(crossings, field.WorldX(x)))
                    {
                        pixels[i] = new Color32(18, 26, 40, 255);
                        continue;
                    }

                    var xm = Mathf.Max(0, x - 1);
                    var xp = Mathf.Min(field.Width - 1, x + 1);
                    var zm = Mathf.Max(0, z - 1);
                    var zp = Mathf.Min(field.Height - 1, z + 1);

                    var dx = (field.Heights[z * field.Width + xp] - field.Heights[z * field.Width + xm])
                             / (2f * field.CellSize);
                    var dz = (field.Heights[zp * field.Width + x] - field.Heights[zm * field.Width + x])
                             / (2f * field.CellSize);
                    var slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;

                    pixels[i] = slope < 5f
                        ? new Color32(120, 190, 130, 255)
                        : slope <= 45f
                            ? new Color32(210, 190, 120, 255)
                            : new Color32(220, 70, 60, 255);
                }
            }

            return pixels;
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
            var raw = ReadArg(System.Environment.GetCommandLineArgs(), SeedsArg);
            if (string.IsNullOrEmpty(raw))
            {
                return AuditSeeds;
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

            return seeds.Count > 0 ? seeds.ToArray() : AuditSeeds;
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
