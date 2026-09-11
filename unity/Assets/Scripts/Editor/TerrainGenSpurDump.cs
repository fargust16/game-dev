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
    /// Diagnostics for Stage 3B: the primary spur skeleton over the Stage 3A partition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Layered so a defect is visible rather than merely absent from the numbers. The structural
    /// domain is the bright ground and everything else is muted, so a spur leaving the domain would
    /// cross onto grey; the attachment interval is drawn in its own colour along the crest, so an
    /// attachment outside it would sit off the end of that line; spurs are drawn over the strip bands
    /// they command, so a crossing, a fan or a comb reads immediately.
    /// </para>
    /// <para>
    /// Every pixel and every line comes from the production types - <see cref="MassifOwnership.At"/>,
    /// <see cref="MassifFlank.AttachmentInterval"/>, <see cref="RidgeSkeleton.Spurs"/>. There is no
    /// second path for the picture, because a picture drawn some other way would not be showing the
    /// thing under test.
    /// </para>
    /// </remarks>
    public static class TerrainGenSpurDump
    {
        /// <summary>The worlds Stage 3B is judged on.</summary>
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231, 1337,
        };

        private const string OutputArg = "-terrainGenOut";
        private const string SeedsArg = "-terrainGenSeeds";

        /// <summary>Image side, pixels. 700 puts a pixel at four metres.</summary>
        private const int Side = 701;

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3B Spur Skeleton (Audit Seeds)", false, 206)]
        public static void DumpSpursAuditSeedsMenu()
        {
            var report = RunSpurs(
                AuditSeeds,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                RidgeSettings.Default,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3b"));
            Debug.Log(report);
        }

        /// <summary>Batch entry point. <c>-executeMethod GameDev.EditorTools.TerrainGenSpurDump.DumpSpursBatch</c></summary>
        public static void DumpSpursBatch()
        {
            var directory = ReadOutputDirectory("stage3b");
            var report = RunSpurs(
                ReadSeeds(),
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                MassifSettings.Default,
                RidgeSettings.Default,
                directory);
            Debug.Log(report);
        }

        /// <summary>Batch entry point for the two-massif fixture.</summary>
        public static void DumpSpurFixtureBatch()
        {
            Debug.Log(RunFixture(ReadOutputDirectory("stage3b")));
        }

        [MenuItem("GameDev/Overworld/Terrain Gen/Dump Stage 3B Spurs (Two-Massif Fixture)", false, 207)]
        public static void DumpSpurFixtureMenu()
        {
            Debug.Log(RunFixture(
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3b")));
        }

        public static string RunSpurs(
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
            sb.AppendLine("Stage 3B primary spur skeleton");
            sb.AppendLine();

            var frame = WorldHeightField.ForWorld(Side);

            for (var i = 0; i < seeds.Length; i++)
            {
                var seed = seeds[i];
                var stage2a = Stage2AMacroStructure.Run(seed, terrainSettings, macroSettings);
                var worldSeed = new GameDev.World.Seed.WorldSeed(seed);
                var massifs = BuildMassifs(worldSeed, stage2a, terrainSettings, massifSettings);
                var stage3a = Stage3ASkeleton.Build(seed, massifs, stage2a.Coast, ridgeSettings);
                var ridges = RidgeSkeleton.Build(stage3a, ridgeSettings, worldSeed);

                var pixels = Render(ridges, stage2a.Coast, stage2a.Structure.CoastRings, frame);
                WritePng(Path.Combine(directory, $"seed_{seed}_spurs.png"), pixels, Side);

                sb.AppendLine(ridges.Stats.ToLine());
                sb.AppendLine("    " + ridges.Stats.TerminationLine());
                AppendDetail(sb, ridges, culture);
            }

            var reportPath = Path.Combine(directory, "stage3b_report.txt");
            File.WriteAllText(reportPath, sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>
        /// Renders the two-massif case, which no seed produces, on the Stage 3A fixture geometry.
        /// </summary>
        public static string RunFixture(string directory)
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

            var settings = RidgeSettings.Default;
            var stage3a = Stage3ASkeleton.Build(0, massifs, coast, settings);
            var ridges = RidgeSkeleton.Build(stage3a, settings, new GameDev.World.Seed.WorldSeed(0));

            var frame = WorldHeightField.ForWorld(Side);
            var pixels = Render(ridges, coast, new List<Vector2[]> { ring }, frame);
            WritePng(Path.Combine(directory, "fixture_two_massif_spurs.png"), pixels, Side);

            var sb = new StringBuilder();
            sb.AppendLine("Stage 3B two-massif fixture");
            sb.AppendLine(ridges.Stats.ToLine());
            sb.AppendLine("    " + ridges.Stats.TerminationLine());
            AppendDetail(sb, ridges, culture);

            File.WriteAllText(Path.Combine(directory, "stage3b_fixture.txt"), sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        private static void AppendDetail(
            StringBuilder sb, RidgeSkeleton ridges, CultureInfo culture)
        {
            for (var m = 0; m < ridges.Ownership.Massifs.Count; m++)
            {
                var massif = ridges.Ownership.Massifs[m];
                var attach = massif.CoreInterval;
                sb.AppendLine(
                    $"    massif {m}: axisLen={massif.LengthMeters.ToString("F0", culture)}m"
                    + $" attachment=[{attach.x.ToString("F3", culture)},{attach.y.ToString("F3", culture)}]"
                    + $" span={((attach.y - attach.x) * massif.LengthMeters).ToString("F0", culture)}m"
                    + $" certified=[{massif.Axis.CertifiedCore.x.ToString("F3", culture)},"
                    + $"{massif.Axis.CertifiedCore.y.ToString("F3", culture)}]"
                    + $" domain={Unbounded(massif.Axis.ReachMeters, culture)}");

                foreach (var sign in new[] { -1, 1 })
                {
                    var flankSpurs = ridges.SpursOn(m, sign);
                    var flankValleys = ridges.ValleysOn(m, sign);
                    if (flankSpurs.Count == 0 && flankValleys.Count == 0)
                    {
                        continue;
                    }

                    var stationText = new StringBuilder();
                    for (var i = 0; i < flankSpurs.Count; i++)
                    {
                        if (i > 0)
                        {
                            stationText.Append(' ');
                        }

                        stationText.Append(flankSpurs[i].AttachS.ToString("F3", culture));
                        stationText.Append('/');
                        stationText.Append(flankSpurs[i].LengthMeters.ToString("F0", culture));
                        stationText.Append('m');
                    }

                    sb.AppendLine(
                        $"      flank {(sign > 0 ? "+1" : "-1")}: spurs={flankSpurs.Count}"
                        + $" valleys={flankValleys.Count}"
                        + $" steep={(sign == massif.SteepSide ? "yes" : "no")}"
                        + $"  [{stationText}]");
                }
            }
        }

        private static List<MountainMassif> BuildMassifs(
            GameDev.World.Seed.IWorldSeed worldSeed,
            Stage2AResult stage2a,
            TerrainGenSettings terrainSettings,
            MassifSettings massifSettings)
        {
            var massifs = new List<MountainMassif>();
            var accepted = stage2a.AcceptedAxes();
            for (var i = 0; i < accepted.Count; i++)
            {
                massifs.Add(MountainMassif.Build(
                    worldSeed, massifs.Count, accepted[i], terrainSettings, massifSettings,
                    accepted[i].CertifiedCore));
            }

            return massifs;
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

        // ---------- rendering ----------

        private static readonly Color32 Ocean = new Color32(18, 26, 40, 255);
        private static readonly Color32 Unclaimed = new Color32(84, 84, 80, 255);
        private static readonly Color32 Coastline = new Color32(238, 238, 238, 255);
        private static readonly Color32 AxisColor = new Color32(16, 16, 16, 255);
        private static readonly Color32 AttachmentColor = new Color32(64, 240, 120, 255);
        private static readonly Color32 DivideColor = new Color32(236, 62, 214, 255);
        private static readonly Color32 SpurColor = new Color32(250, 250, 252, 255);
        private static readonly Color32 SpurCoreColor = new Color32(24, 20, 30, 255);
        private static readonly Color32 ValleyHeadColor = new Color32(90, 200, 255, 255);

        /// <summary>Per massif: gentle flank, steep flank, beyond reach, beyond the axis end.</summary>
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
            RidgeSkeleton ridges,
            CoastQuery coast,
            IReadOnlyList<Vector2[]> rings,
            WorldHeightField frame)
        {
            var skeleton = ridges.Ownership;
            var pixels = new Color32[frame.Width * frame.Height];
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
                            owner.TryGetFrame(out var structural);
                            var palette = OwnerColors[structural.MassifIndex % OwnerColors.Length];
                            var steep = structural.FlankSign
                                        == skeleton.Massifs[structural.MassifIndex].SteepSide;
                            color = palette[steep ? 1 : 0];
                            break;
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

            // Valley heads before the spurs, so a head that landed on a spur is overdrawn and the
            // alternation reads as spur / head / spur along the crest.
            for (var v = 0; v < ridges.Valleys.Count; v++)
            {
                var valley = ridges.Valleys[v];
                var massif = skeleton.Massifs[valley.MassifIndex];
                var head = massif.PointAt(valley.HeadS);
                StampDisc(pixels, frame, head, ValleyHeadColor, 2);
            }

            for (var m = 0; m < skeleton.Massifs.Count; m++)
            {
                var axis = skeleton.Massifs[m].Axis;
                for (var i = 0; i + 1 < axis.Points.Length; i++)
                {
                    DrawLine(pixels, frame, axis.Points[i], axis.Points[i + 1], AxisColor, 1);
                }

                DrawArcSpan(
                    pixels, frame, axis, skeleton.Massifs[m].CoreInterval, AttachmentColor, 0);
            }

            // Spurs last and doubled: a dark casing under a bright core, so a spur stays legible over
            // both flank tints and over the muted out-of-domain ground it must never reach.
            for (var s = 0; s < ridges.Spurs.Count; s++)
            {
                var points = ridges.Spurs[s].Points;
                for (var i = 0; i + 1 < points.Length; i++)
                {
                    DrawLine(pixels, frame, points[i], points[i + 1], SpurCoreColor, 2);
                }
            }

            for (var s = 0; s < ridges.Spurs.Count; s++)
            {
                var points = ridges.Spurs[s].Points;
                for (var i = 0; i + 1 < points.Length; i++)
                {
                    DrawLine(pixels, frame, points[i], points[i + 1], SpurColor, 1);
                }

                StampDisc(pixels, frame, points[points.Length - 1], SpurColor, 2);
            }

            return pixels;
        }

        private static void StampDisc(
            Color32[] pixels, WorldHeightField frame, Vector2 p, Color32 color, int radius)
        {
            var cx = Mathf.RoundToInt(p.x / frame.CellSize);
            var cz = Mathf.RoundToInt(p.y / frame.CellSize);
            for (var oz = -radius; oz <= radius; oz++)
            {
                for (var ox = -radius; ox <= radius; ox++)
                {
                    if (ox * ox + oz * oz > radius * radius)
                    {
                        continue;
                    }

                    var px = cx + ox;
                    var pz = cz + oz;
                    if (px >= 0 && pz >= 0 && px < frame.Width && pz < frame.Height)
                    {
                        pixels[pz * frame.Width + px] = color;
                    }
                }
            }
        }

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

        private static string Unbounded(float meters, CultureInfo culture) =>
            float.IsPositiveInfinity(meters) ? "inf" : meters.ToString("F0", culture) + "m";

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
