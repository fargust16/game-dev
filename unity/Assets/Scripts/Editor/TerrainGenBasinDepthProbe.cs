#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.TerrainGen;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Read-only measurement of what the Stage 2 structural surface does inside each cirque candidate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Evidence for the B4 design question and nothing else. It declares no basin, applies no
    /// threshold, changes no generation behaviour and is referenced by nothing that does. It reads the
    /// same elevation the preflight resolved as authoritative - Stage 1's surface plus the massif
    /// uplift, which is Stage 2 - and never <c>H3</c>, the carve or any trajectory.
    /// </para>
    /// <para>
    /// The one question it answers: on that surface, is the lowest ground inside an enclosure actually
    /// below the lowest ground in its outlet gap? If it is not, no bound on the difference matters,
    /// because there is no hollow to bound.
    /// </para>
    /// </remarks>
    public static class TerrainGenBasinDepthProbe
    {
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 101010, 20261231,
        };

        private const string OutputArg = "-terrainGenOut";

        [MenuItem("GameDev/Overworld/Terrain Gen/Probe Cirque Structural Depth (B4 evidence)", false, 214)]
        public static void ProbeMenu() =>
            Debug.Log(Run(
                AuditSeeds,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_depth")));

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenBasinDepthProbe.ProbeBatch</c></summary>
        public static void ProbeBatch()
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            Debug.Log(Run(
                AuditSeeds,
                string.IsNullOrEmpty(directory)
                    ? Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "stage3e_depth")
                    : directory));
        }

        public static string Run(int[] seeds, string directory)
        {
            Directory.CreateDirectory(directory);
            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder();

            sb.AppendLine("Stage 3E B4 evidence: the Stage 2 structural surface inside each cirque candidate");
            sb.AppendLine("Read-only. No basin declared, no threshold applied, no H3 touched.");
            sb.AppendLine();

            var terrain = TerrainGenSettings.Default;
            var macro = MacroStructureSettings.Default;
            var massifSettings = MassifSettings.Default;
            var ridgeSettings = RidgeSettings.Default;

            var hollow = 0;
            var total = 0;

            foreach (var seed in seeds)
            {
                var stage1 = Stage1LandmassGenerator.Generate(seed, terrain);
                var stage2a = Stage2AMacroStructure.Run(seed, terrain, macro);
                var worldSeed = new GameDev.World.Seed.WorldSeed(seed);

                var massifs = new List<MountainMassif>();
                var accepted = stage2a.AcceptedAxes();
                for (var i = 0; i < accepted.Count; i++)
                {
                    massifs.Add(MountainMassif.Build(
                        worldSeed, massifs.Count, accepted[i], terrain, massifSettings,
                        accepted[i].CertifiedCore));
                }

                var partition = Stage3ASkeleton.Build(seed, massifs, stage2a.Coast, ridgeSettings);
                var ridges = RidgeSkeleton.Build(partition, ridgeSettings, worldSeed);
                var set = CirqueCandidateBuilder.Build(ridges, ridgeSettings);
                if (set.Candidates.Count == 0)
                {
                    continue;
                }

                var uplift = new MassifUpliftModel(massifs, massifSettings);
                sb.AppendLine($"seed {seed}: {set.Candidates.Count} candidates");

                foreach (var candidate in set.Candidates)
                {
                    total++;

                    // Outlet: the gap run only, which is the resolved B3 reading.
                    var gap = candidate.Segments[2];
                    var outlet = float.MaxValue;
                    for (var k = 0; k < gap.PointCount; k++)
                    {
                        var p = candidate.Boundary[(gap.FirstIndex + k) % candidate.Boundary.Length];
                        outlet = Mathf.Min(outlet, Elevation(stage1, uplift, p));
                    }

                    // For comparison only: the minimum over the whole ring, which is reading A, and
                    // which run of the ring it falls on. The two gap endpoints are the spur toes, and
                    // one of them is bookkept into the spur run rather than the gap, so where the ring
                    // minimum sits decides whether the gap's own minimum saw it.
                    var ring = float.MaxValue;
                    var ringAt = 0;
                    for (var k = 0; k < candidate.Boundary.Length; k++)
                    {
                        var h = Elevation(stage1, uplift, candidate.Boundary[k]);
                        if (h < ring)
                        {
                            ring = h;
                            ringAt = k;
                        }
                    }

                    var ringRun = BoundarySource.Crest;
                    for (var k = 0; k < candidate.Segments.Length; k++)
                    {
                        var run = candidate.Segments[k];
                        if (ringAt >= run.FirstIndex && ringAt < run.FirstIndex + run.PointCount)
                        {
                            ringRun = run.Source;
                        }
                    }

                    var upperToe = Elevation(stage1, uplift, candidate.UpperToe);
                    var lowerToe = Elevation(stage1, uplift, candidate.LowerToe);

                    var floor = InteriorMinimum(stage1, uplift, candidate.Boundary, out var cells);
                    var depth = outlet - floor;
                    if (depth > 0f)
                    {
                        hollow++;
                    }

                    sb.AppendLine(
                        $"    C{candidate.Id} (V{candidate.CorridorId}):"
                        + $" outletGapMin={outlet.ToString("F2", culture)}m"
                        + $" wholeRingMin={ring.ToString("F2", culture)}m"
                        + $" ringMinOn={ringRun}"
                        + $" upperToe={upperToe.ToString("F2", culture)}m"
                        + $" lowerToe={lowerToe.ToString("F2", culture)}m"
                        + $" interiorMin={floor.ToString("F2", culture)}m"
                        + $" depth={depth.ToString("F3", culture)}m"
                        + $" cells={cells}"
                        + $" area={candidate.AreaSqm.ToString("F0", culture)}m2"
                        + $" crestSpan={candidate.CrestSpanMeters.ToString("F0", culture)}m"
                        + $" convergence={candidate.ConvergenceRatio.ToString("F2", culture)}");
                }

                sb.AppendLine();
            }

            sb.AppendLine(
                $"candidates measured: {total};"
                + $" with interior strictly below the outlet gap: {hollow}");

            var path = Path.Combine(directory, "stage3e_depth_probe.txt");
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();
            return sb + "\nwritten to " + directory;
        }

        /// <summary>
        /// The Stage 2 structural surface at a point: Stage 1 plus the gated massif uplift.
        /// </summary>
        /// <remarks>
        /// The same composition <see cref="Stage2BMassifGenerator.EvaluateFields"/> writes into its
        /// field, evaluated analytically instead of read off a lattice.
        /// </remarks>
        private static float Elevation(Stage1Result stage1, MassifUpliftModel uplift, Vector2 p) =>
            StructuralElevation.At(stage1, uplift, p);

        /// <summary>The lowest structural elevation strictly inside a ring, on Stage 1's own lattice.</summary>
        private static float InteriorMinimum(
            Stage1Result stage1, MassifUpliftModel uplift, Vector2[] ring, out int cells)
        {
            var field = stage1.Field;
            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            for (var i = 0; i < ring.Length; i++)
            {
                minX = Mathf.Min(minX, ring[i].x);
                maxX = Mathf.Max(maxX, ring[i].x);
                minZ = Mathf.Min(minZ, ring[i].y);
                maxZ = Mathf.Max(maxZ, ring[i].y);
            }

            var x0 = Mathf.Max(0, Mathf.FloorToInt(minX / field.CellSize));
            var x1 = Mathf.Min(field.Width - 1, Mathf.CeilToInt(maxX / field.CellSize));
            var z0 = Mathf.Max(0, Mathf.FloorToInt(minZ / field.CellSize));
            var z1 = Mathf.Min(field.Height - 1, Mathf.CeilToInt(maxZ / field.CellSize));

            var lowest = float.MaxValue;
            cells = 0;

            for (var z = z0; z <= z1; z++)
            {
                var worldZ = field.WorldZ(z);
                for (var x = x0; x <= x1; x++)
                {
                    var p = new Vector2(field.WorldX(x), worldZ);
                    if (!Inside(ring, p))
                    {
                        continue;
                    }

                    cells++;
                    lowest = Mathf.Min(lowest, Elevation(stage1, uplift, p));
                }
            }

            return cells == 0 ? float.NaN : lowest;
        }

        private static bool Inside(Vector2[] ring, Vector2 p)
        {
            var inside = false;
            for (int i = 0, j = ring.Length - 1; i < ring.Length; j = i++)
            {
                if (ring[i].y > p.y != ring[j].y > p.y
                    && p.x < (ring[j].x - ring[i].x) * (p.y - ring[i].y)
                    / (ring[j].y - ring[i].y) + ring[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
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
