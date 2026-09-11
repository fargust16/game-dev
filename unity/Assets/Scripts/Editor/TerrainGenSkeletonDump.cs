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
    /// Diagnostics for Stage 3A: one top-down picture of the structural partition, per seed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The question this answers is whether the spatial partition makes geographic sense, so it is
    /// drawn flat: ownership as a tint, flanks as two shades of it, the axis and its certified core
    /// as lines, the medial divide as a line. No hillshade, no hypsometry and no height field - Stage
    /// 3A changes no height, and a terrain render would only make it harder to see whether the
    /// partition is right.
    /// </para>
    /// <para>
    /// The image is a raster; the partition is not. Every pixel is an independent call to the same
    /// geometric <see cref="MassifOwnership.At"/> that Stage 3B will use at arbitrary world
    /// coordinates, which is also what makes this picture evidence: if ownership were computed some
    /// other way for the diagnostic, the diagnostic would not be showing the thing under test.
    /// </para>
    /// <para>
    /// Stage 2B's 1025 x 1025 field is never evaluated. The massifs are built from Stage 2A's cheap
    /// path - partition, landmass, coastline, plates, axes - which is the same construction
    /// <see cref="Stage2BMassifGenerator"/> performs before it starts sampling, and costs a fifth of
    /// a second a seed instead of seven.
    /// </para>
    /// </remarks>
    public static class TerrainGenSkeletonDump
    {
        public static readonly int[] DefaultSeeds = TerrainGenDump.DefaultSeeds;

        /// <summary>The worlds Stage 3A is judged on: the canonical five, the restored two, the refusals.</summary>
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337, 256, 777, 31337,
        };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        /// <summary>Image side, pixels. 700 puts a pixel at four metres.</summary>
        private const int Side = 701;

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3A Skeleton (Audit Seeds)", false, 204)]
        public static void DumpSkeletonAuditSeedsMenu()
        {
            var report = RunSkeleton(
                AuditSeeds,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                RidgeSettings.Default,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3a"));
            Debug.Log(report);
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenSkeletonDump.DumpSkeletonBatch</c></summary>
        public static void DumpSkeletonBatch()
        {
            var directory = ReadOutputDirectory("stage3a");
            var report = RunSkeleton(
                ReadSeeds(),
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                RidgeSettings.Default,
                directory);
            Debug.Log(report);
        }

        public static string RunSkeleton(
            int[] seeds,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            MassifSettings massifSettings,
            RidgeSettings ridgeSettings,
            string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Stage 3A skeleton: ownership, flanks, inter-massif divides");
            sb.AppendLine();

            var frame = WorldHeightField.ForWorld(Side);

            for (var i = 0; i < seeds.Length; i++)
            {
                var seed = seeds[i];
                var stage2a = Stage2AMacroStructure.Run(seed, terrainSettings, macroSettings);
                var massifs = BuildMassifs(seed, stage2a, terrainSettings, massifSettings);
                var skeleton = Stage3ASkeleton.Build(seed, massifs, stage2a.Coast, ridgeSettings);

                var domain = new int[5];
                var pixels = Render(skeleton, stage2a, frame, domain);
                WritePng(Path.Combine(directory, $"seed_{seed}_skeleton.png"), pixels, Side);

                sb.AppendLine(skeleton.Stats.ToLine());

                for (var m = 0; m < massifs.Count; m++)
                {
                    var axis = massifs[m].Axis;
                    var attach = massifs[m].CoreInterval;
                    sb.AppendLine(
                        $"    massif {m}: len={axis.LengthMeters.ToString("F0", culture)}m"
                        + $" halfW={axis.UsableHalfWidthMeters.ToString("F0", culture)}m"
                        + $" reach={Unbounded(axis.ReachMeters, culture)}"
                        + $" steepSide={massifs[m].SteepSide}");

                    // The two intervals side by side, with how close each comes to an axis tip. The
                    // second number on the certified core is the one that reaches 20 m and 0 m.
                    sb.AppendLine(
                        $"      certifiedCore=[{axis.CertifiedCore.x.ToString("F3", culture)},"
                        + $"{axis.CertifiedCore.y.ToString("F3", culture)}]"
                        + $" toTips=[{(axis.CertifiedCore.x * axis.LengthMeters).ToString("F0", culture)}m,"
                        + $"{((1f - axis.CertifiedCore.y) * axis.LengthMeters).ToString("F0", culture)}m]");
                    sb.AppendLine(
                        $"      attachment  =[{attach.x.ToString("F3", culture)},"
                        + $"{attach.y.ToString("F3", culture)}]"
                        + $" toTips=[{(attach.x * axis.LengthMeters).ToString("F0", culture)}m,"
                        + $"{((1f - attach.y) * axis.LengthMeters).ToString("F0", culture)}m]"
                        + " (MountainMassif.CoreInterval)");
                }

                var owned = domain[(int)MassifOwnerKind.Massif]
                            + domain[(int)MassifOwnerKind.BeyondReach]
                            + domain[(int)MassifOwnerKind.BeyondAxisEnd];
                if (owned > 0)
                {
                    sb.AppendLine(
                        $"    domain: structural={Percent(domain[(int)MassifOwnerKind.Massif], owned, culture)}"
                        + $" beyondReach={Percent(domain[(int)MassifOwnerKind.BeyondReach], owned, culture)}"
                        + $" beyondAxisEnd={Percent(domain[(int)MassifOwnerKind.BeyondAxisEnd], owned, culture)}"
                        + $" ofOwnedLand");
                }

                for (var d = 0; d < skeleton.Divides.Count; d++)
                {
                    var divide = skeleton.Divides[d];
                    sb.AppendLine(
                        $"    divide {divide.MassifA}-{divide.MassifB}:"
                        + $" len={divide.LengthMeters.ToString("F0", culture)}m"
                        + $" ends={divide.StartTermination}/{divide.EndTermination}"
                        + $" imbalance={divide.MaxImbalanceMeters.ToString("F3", culture)}m"
                        + $" nearestAxis={divide.MinAxisDistanceMeters.ToString("F1", culture)}m");
                }

                sb.AppendLine(
                    "    frameAgreement="
                    + skeleton.Ownership.MeasureFrameAgreement().ToString("F2", culture) + "m");
            }

            var reportPath = Path.Combine(directory, "stage3a_report.txt");
            File.WriteAllText(reportPath, sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>
        /// Renders the two-massif case, which no seed can produce, on hand-built geometry.
        /// </summary>
        /// <remarks>
        /// Stage 2A caps a world at two systems and across the twenty-nine worlds audited produced
        /// one on every world that got one at all, so the medial divide has no seed that would show
        /// it. Rendering it from a fixture is the only way to actually look at the thing rather than
        /// take the numbers on trust - two straight axes 600 m apart on a square island, whose
        /// divide should be the level line exactly between them, running coast to coast.
        /// </remarks>
        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3A Divide (Two-Massif Fixture)", false, 205)]
        public static void DumpDivideFixtureMenu()
        {
            var report = RunDivideFixture(
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3a"));
            Debug.Log(report);
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenSkeletonDump.DumpDivideFixtureBatch</c></summary>
        public static void DumpDivideFixtureBatch()
        {
            Debug.Log(RunDivideFixture(ReadOutputDirectory("stage3a")));
        }

        public static string RunDivideFixture(string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;

            var ring = new[]
            {
                new Vector2(400f, 400f),
                new Vector2(2400f, 400f),
                new Vector2(2400f, 2400f),
                new Vector2(400f, 2400f),
            };

            var coast = new CoastQuery(new List<Vector2[]> { ring }, 1000f);
            var massifs = new List<MountainMassif>
            {
                FixtureMassif(0, new Vector2(800f, 1100f), new Vector2(1700f, 1100f)),
                FixtureMassif(1, new Vector2(800f, 1700f), new Vector2(1700f, 1700f)),
            };

            var skeleton = Stage3ASkeleton.Build(0, massifs, coast, RidgeSettings.Default);
            var frame = WorldHeightField.ForWorld(Side);
            var pixels = Render(skeleton, coast, new List<Vector2[]> { ring }, frame, null);
            WritePng(Path.Combine(directory, "fixture_two_massif_divide.png"), pixels, Side);

            var sb = new StringBuilder();
            sb.AppendLine("Stage 3A two-massif fixture");
            sb.AppendLine(skeleton.Stats.ToLine());
            for (var d = 0; d < skeleton.Divides.Count; d++)
            {
                var divide = skeleton.Divides[d];
                sb.AppendLine(
                    $"    divide {divide.MassifA}-{divide.MassifB}:"
                    + $" len={divide.LengthMeters.ToString("F0", culture)}m"
                    + $" samples={divide.Points.Length}"
                    + $" ends={divide.StartTermination}/{divide.EndTermination}"
                    + $" imbalance={divide.MaxImbalanceMeters.ToString("F4", culture)}m"
                    + $" nearestAxis={divide.MinAxisDistanceMeters.ToString("F1", culture)}m"
                    + $" from={divide.Points[0].ToString("F1", culture)}"
                    + $" to={divide.Points[divide.Points.Length - 1].ToString("F1", culture)}");
            }

            File.WriteAllText(Path.Combine(directory, "stage3a_fixture.txt"), sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        private static MountainMassif FixtureMassif(int index, Vector2 from, Vector2 to)
        {
            var steps = Mathf.Max(2, Mathf.RoundToInt((to - from).magnitude / 20f));
            var points = new Vector2[steps + 1];
            for (var i = 0; i <= steps; i++)
            {
                points[i] = Vector2.Lerp(from, to, i / (float)steps);
            }

            var axis = new MacroAxis(
                points, new[] { from, to }, new[] { 0, 1 }, float.PositiveInfinity, 0.5f, 250f);

            return new MountainMassif(
                index,
                axis,
                new ProfileCurve(new[] { 0f, 0.5f, 1f }, new[] { 0f, 1f, 0f }),
                300f,
                250f,
                0.85f,
                1,
                new[] { 0.5f },
                MassifSettings.Default,
                new Vector2(0.2f, 0.8f));
        }

        /// <summary>
        /// Raises the Stage 2B massifs on Stage 2A's accepted axes, without evaluating any field.
        /// </summary>
        /// <remarks>
        /// The same three lines <see cref="Stage2BMassifGenerator"/> runs per accepted axis. Nothing
        /// is decided here - the axis carries its own certified core and width - so this reproduces
        /// the massifs rather than reinterpreting them.
        /// </remarks>
        private static List<MountainMassif> BuildMassifs(
            int seed,
            Stage2AResult stage2a,
            TerrainGenSettings terrainSettings,
            MassifSettings massifSettings)
        {
            var worldSeed = new GameDev.World.Seed.WorldSeed(seed);
            var massifs = new List<MountainMassif>();
            var accepted = stage2a.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                massifs.Add(MountainMassif.Build(
                    worldSeed,
                    massifs.Count,
                    accepted[i],
                    terrainSettings,
                    massifSettings,
                    accepted[i].CertifiedCore));
            }

            return massifs;
        }

        // ---------- rendering ----------

        private static readonly Color32 Ocean = new Color32(18, 26, 40, 255);
        private static readonly Color32 Unclaimed = new Color32(84, 84, 80, 255);
        private static readonly Color32 Coastline = new Color32(238, 238, 238, 255);
        private static readonly Color32 AxisColor = new Color32(16, 16, 16, 255);
        private static readonly Color32 CertifiedCoreColor = new Color32(255, 226, 64, 255);
        private static readonly Color32 AttachmentColor = new Color32(64, 240, 120, 255);
        private static readonly Color32 DivideColor = new Color32(236, 62, 214, 255);

        /// <summary>
        /// Per massif: gentle flank, steep flank, beyond reach, beyond the axis end.
        /// </summary>
        /// <remarks>
        /// The first two are the structural domain and are the only ones drawn in the massif's own
        /// hue. The last two are owned ground with no usable frame, drawn muted, because that is the
        /// distinction Stage 3B has to see: colour means "you may ask for a coordinate here".
        /// </remarks>
        private static readonly Color32[][] OwnerColors =
        {
            new[]
            {
                new Color32(198, 140, 92, 255), new Color32(146, 92, 56, 255),
                new Color32(74, 66, 60, 255), new Color32(110, 98, 84, 255),
            },
            new[]
            {
                new Color32(104, 156, 200, 255), new Color32(56, 106, 150, 255),
                new Color32(58, 66, 76, 255), new Color32(88, 100, 114, 255),
            },
            new[]
            {
                new Color32(126, 178, 118, 255), new Color32(76, 122, 70, 255),
                new Color32(60, 72, 58, 255), new Color32(92, 108, 88, 255),
            },
        };

        private static Color32[] Render(
            Stage3ASkeleton skeleton, Stage2AResult stage2a, WorldHeightField frame, int[] domain) =>
            Render(skeleton, stage2a.Coast, stage2a.Structure.CoastRings, frame, domain);

        private static Color32[] Render(
            Stage3ASkeleton skeleton,
            CoastQuery coast,
            IReadOnlyList<Vector2[]> rings,
            WorldHeightField frame,
            int[] domain)
        {
            var pixels = new Color32[frame.Width * frame.Height];

            // Row crossings once per row, not once per pixel. CoastQuery.IsInside walks every coast
            // segment to build them, so the land test would otherwise cost more than the partition.
            var crossings = new List<float>(16);

            for (var z = 0; z < frame.Height; z++)
            {
                var worldZ = frame.WorldZ(z);
                coast.RowCrossings(worldZ, crossings);

                for (var x = 0; x < frame.Width; x++)
                {
                    var worldX = frame.WorldX(x);
                    var land = CoastQuery.InsideFromSortedCrossings(crossings, worldX);
                    var owner = skeleton.Ownership.At(worldX, worldZ, land);

                    Color32 color;
                    switch (owner.Kind)
                    {
                        case MassifOwnerKind.Ocean:
                            color = Ocean;
                            break;
                        case MassifOwnerKind.Unclaimed:
                            color = Unclaimed;
                            break;
                        case MassifOwnerKind.BeyondReach:
                            color = OwnerColors[owner.MassifIndex % OwnerColors.Length][2];
                            break;
                        case MassifOwnerKind.BeyondAxisEnd:
                            color = OwnerColors[owner.MassifIndex % OwnerColors.Length][3];
                            break;
                        default:
                            // The same TryGetFrame gate Stage 3B goes through. If the picture could
                            // read a flank without it, the picture would not be showing the contract.
                            owner.TryGetFrame(out var structural);
                            var palette = OwnerColors[structural.MassifIndex % OwnerColors.Length];
                            var steep = structural.FlankSign
                                        == skeleton.Massifs[structural.MassifIndex].SteepSide;
                            color = palette[steep ? 1 : 0];
                            break;
                    }

                    if (domain != null)
                    {
                        domain[(int)owner.Kind]++;
                    }

                    pixels[z * frame.Width + x] = color;
                }
            }

            for (var r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                for (var i = 0; i < ring.Length; i++)
                {
                    DrawLine(pixels, frame, ring[i], ring[(i + 1) % ring.Length], Coastline, 0);
                }
            }

            for (var d = 0; d < skeleton.Divides.Count; d++)
            {
                var points = skeleton.Divides[d].Points;
                for (var i = 0; i + 1 < points.Length; i++)
                {
                    DrawLine(pixels, frame, points[i], points[i + 1], DivideColor, 1);
                }
            }

            for (var m = 0; m < skeleton.Massifs.Count; m++)
            {
                var axis = skeleton.Massifs[m].Axis;
                for (var i = 0; i + 1 < axis.Points.Length; i++)
                {
                    DrawLine(pixels, frame, axis.Points[i], axis.Points[i + 1], AxisColor, 1);
                }

                // Two intervals, drawn one inside the other, because the gap between them is the
                // contract. Yellow is Stage 2A's certified core - coastal clearance - which on seed 7
                // runs to within 20 m of the tip and on 20261231 starts on the tip. Green is Stage
                // 2B's CoreInterval, which is what a flank actually offers for attachment.
                DrawArcSpan(pixels, frame, axis, axis.CertifiedCore, CertifiedCoreColor, 1);
                DrawArcSpan(
                    pixels, frame, axis, skeleton.Massifs[m].CoreInterval, AttachmentColor, 0);
            }

            return pixels;
        }

        private static string Percent(int part, int whole, CultureInfo culture) =>
            (100f * part / Mathf.Max(1, whole)).ToString("F1", culture) + "%";

        private static string Unbounded(float meters, CultureInfo culture) =>
            float.IsPositiveInfinity(meters) ? "inf" : meters.ToString("F0", culture) + "m";

        /// <summary>Draws a normalised arc interval of an axis.</summary>
        private static void DrawArcSpan(
            Color32[] pixels,
            WorldHeightField frame,
            MacroAxis axis,
            Vector2 interval,
            Color32 color,
            int radius)
        {
            var last = axis.Points.Length - 1;
            var from = Mathf.Clamp(Mathf.RoundToInt(interval.x * last), 0, last);
            var to = Mathf.Clamp(Mathf.RoundToInt(interval.y * last), 0, last);
            for (var i = from; i < to; i++)
            {
                DrawLine(pixels, frame, axis.Points[i], axis.Points[i + 1], color, radius);
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
