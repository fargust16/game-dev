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
    /// Asks of a landmass, before any axis is chosen: could a T1 massif stand on it at all?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Stage 2A answers a narrower question - can a massif stand on <em>this boundary</em> - and when
    /// it says no, that answer alone cannot tell whether the island had nowhere to put one or whether
    /// the geology simply pointed somewhere else. This measures the island itself, independently of
    /// where the plates happen to meet, and then measures every plate boundary against the same test.
    /// The difference between the two is the classification.
    /// </para>
    /// <para>
    /// Read-only. It runs Stage 1 as far as the coastline and Stage 2A's plate layer, and writes a
    /// report. Nothing here is part of generation and nothing it computes feeds back into it.
    /// </para>
    /// </remarks>
    public static class TerrainGenLandmassFeasibility
    {
        /// <summary>The canonical five, then the robustness spread.</summary>
        public static readonly int[] AuditSeeds =
        {
            110201, 424242, 20260905, 7, 1337,
            1, 2, 3, 17, 42, 99, 256, 777, 1024, 4096, 9091, 12345,
            31337, 65535, 101010, 202020, 314159, 500001, 654321, 888888,
            1000003, 1234567, 7654321, 20261231,
        };

        private const string OutputArg = "-terrainGenOut";

        /// <summary>Distance-field cell, metres. Fine enough to resolve a 400 m corridor.</summary>
        private const float CellMeters = 10f;

        /// <summary>Directions the straight-corridor search sweeps, over half a turn.</summary>
        private const int Directions = 24;

        /// <summary>Spacing between the parallel lines each direction scans, metres.</summary>
        private const float LineSpacingMeters = 20f;

        /// <summary>Step along each scan line, metres.</summary>
        private const float StepMeters = 10f;

        [MenuItem("GameDev/Overworld/Terrain Gen/Audit Landmass T1 Feasibility", false, 204)]
        public static void AuditMenu()
        {
            var report = Run(
                AuditSeeds,
                TerrainGenSettings.Default,
                MacroStructureSettings.Default,
                Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "feasibility"));
            Debug.Log(report);
        }

        /// <summary>Batch entry. <c>-executeMethod GameDev.EditorTools.TerrainGenLandmassFeasibility.AuditBatch</c></summary>
        public static void AuditBatch()
        {
            var directory = ReadArg(System.Environment.GetCommandLineArgs(), OutputArg);
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.Combine(Directory.GetCurrentDirectory(), "TerrainGenDump", "feasibility");
            }

            var report = Run(
                AuditSeeds, TerrainGenSettings.Default, MacroStructureSettings.Default, directory);
            Debug.Log(report);
        }

        /// <summary>What one world's landmass can and cannot carry.</summary>
        private struct Verdict
        {
            public int Seed;

            public float IslandAreaKm2;
            public float InnerAreaKm2;
            public float MaxInscribedRadius;

            /// <summary>Longest stretch anywhere on the island whose whole section clears the coast.</summary>
            public float BestCoreMeters;

            /// <summary>Longest on-land run containing that core.</summary>
            public float BestAxisMeters;

            public Vector2 BestFrom;
            public Vector2 BestTo;

            /// <summary>Best core any plate boundary offers, and whether that boundary converges.</summary>
            public float BestBoundaryCoreMeters;

            public float BestConvergentBoundaryCoreMeters;
            public int BoundariesThatCouldCertify;
            public int ConvergentBoundariesThatCouldCertify;

            public int AcceptedAxes;

            /// <summary>Chains Stage 2A discovered, and how many survived its exclusive ranking.</summary>
            public int ChainsDiscovered;

            public int ChainsAccepted;

            /// <summary>
            /// Boundaries with room that no candidate ever carried, so certification never saw them.
            /// </summary>
            public int RoomyBoundariesNeverChained;

            public int RoomyConvergentBoundariesNeverChained;

            public string Class;
            public string Why;
        }

        public static string Run(
            int[] seeds,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            string directory)
        {
            terrainSettings.Clamp();
            macroSettings.Clamp();
            Directory.CreateDirectory(directory);

            var c = CultureInfo.InvariantCulture;
            var halfWidth = macroSettings.MinUsableHalfWidthMeters;
            var clearance = macroSettings.MassifCoastClearanceMeters;
            var setback = macroSettings.CoastSetbackMeters;
            var axisRequired = Mathf.Max(
                macroSettings.MinAxisLengthMeters,
                halfWidth * macroSettings.MinLengthToWidthRatio);
            var coreRequired = halfWidth * macroSettings.MinCoreLengthPerHalfWidth;

            var sb = new StringBuilder();
            sb.AppendLine("Landmass T1 feasibility audit - can an island carry a massif at all?");
            sb.AppendLine();
            sb.AppendLine("  the contract, derived from the Stage 2A and 2B settings:");
            sb.AppendLine(
                $"    an island admits a T1 massif iff it contains a curve at least "
                + $"{axisRequired.ToString("F0", c)} m long staying {setback.ToString("F0", c)} m inland,");
            sb.AppendLine(
                $"    holding a sub-stretch at least {coreRequired.ToString("F0", c)} m long over which "
                + $"both points {halfWidth.ToString("F0", c)} m either side of it");
            sb.AppendLine(
                $"    stand at least {clearance.ToString("F0", c)} m from the coast.");
            sb.AppendLine(
                $"    (half-width {halfWidth.ToString("F0", c)} m, gate {clearance.ToString("F0", c)} m, "
                + $"L/W {macroSettings.MinLengthToWidthRatio.ToString("F1", c)}, "
                + $"core {macroSettings.MinCoreLengthPerHalfWidth.ToString("F1", c)} half-widths, "
                + $"floor {macroSettings.MinAxisLengthMeters.ToString("F0", c)} m)");
            sb.AppendLine();

            var verdicts = new List<Verdict>(seeds.Length);
            for (var i = 0; i < seeds.Length; i++)
            {
                verdicts.Add(Audit(seeds[i], terrainSettings, macroSettings, axisRequired, coreRequired));
            }

            sb.AppendLine(
                "  seed        area  inner  inscribed   bestCore  bestAxis   bndCore  cnvCore  axes  chains  unchained  class");
            for (var i = 0; i < verdicts.Count; i++)
            {
                var v = verdicts[i];
                sb.AppendLine(
                    $"  {v.Seed,9}  {v.IslandAreaKm2.ToString("F2", c),5}  "
                    + $"{v.InnerAreaKm2.ToString("F2", c),5}  {v.MaxInscribedRadius.ToString("F0", c),9}  "
                    + $"{v.BestCoreMeters.ToString("F0", c),9}  {v.BestAxisMeters.ToString("F0", c),8}  "
                    + $"{v.BestBoundaryCoreMeters.ToString("F0", c),8}  "
                    + $"{v.BestConvergentBoundaryCoreMeters.ToString("F0", c),7}  "
                    + $"{v.AcceptedAxes,4}  {v.ChainsAccepted}/{v.ChainsDiscovered,-2}  "
                    + $"{v.RoomyConvergentBoundariesNeverChained}/{v.RoomyBoundariesNeverChained,-2}  "
                    + $"{v.Class}");
            }

            sb.AppendLine();
            sb.AppendLine("  worlds with no massif:");
            var classes = new Dictionary<string, int>();
            var withMassif = 0;
            for (var i = 0; i < verdicts.Count; i++)
            {
                if (verdicts[i].AcceptedAxes > 0)
                {
                    withMassif++;
                    continue;
                }

                sb.AppendLine($"    seed {verdicts[i].Seed,9}  class {verdicts[i].Class}  {verdicts[i].Why}");
                classes.TryGetValue(verdicts[i].Class, out var count);
                classes[verdicts[i].Class] = count + 1;
            }

            sb.AppendLine();
            sb.AppendLine(
                $"  {withMassif} of {verdicts.Count} worlds carry a massif "
                + $"({(100f * withMassif / verdicts.Count).ToString("F1", c)} %)");
            foreach (var pair in classes)
            {
                sb.AppendLine($"    class {pair.Key}: {pair.Value}");
            }

            File.WriteAllText(Path.Combine(directory, "feasibility_report.txt"), sb.ToString());
            return sb.ToString();
        }

        private static Verdict Audit(
            int seed,
            TerrainGenSettings terrainSettings,
            MacroStructureSettings macroSettings,
            float axisRequired,
            float coreRequired)
        {
            var worldSeed = new GameDev.World.Seed.WorldSeed(seed);
            var partition = CrustalPartition.Build(worldSeed, terrainSettings);
            var landmass = LandmassBuilder.Build(worldSeed, terrainSettings, partition);
            var coast = new CoastQuery(landmass.CoastRings, terrainSettings.MaxCoastDistanceMeters);

            var distance = Rasterise(coast, out var width, out var height);

            var v = new Verdict { Seed = seed };
            var cell = CellMeters * CellMeters;
            for (var i = 0; i < distance.Length; i++)
            {
                if (distance[i] > 0f)
                {
                    v.IslandAreaKm2 += cell;
                }

                if (distance[i] >= macroSettings.MassifCoastClearanceMeters)
                {
                    v.InnerAreaKm2 += cell;
                }

                v.MaxInscribedRadius = Mathf.Max(v.MaxInscribedRadius, distance[i]);
            }

            v.IslandAreaKm2 /= 1e6f;
            v.InnerAreaKm2 /= 1e6f;

            SweepCorridors(distance, width, height, macroSettings, coreRequired, ref v);

            var plates = MacroPlates.Build(worldSeed, macroSettings, partition, landmass.LandSites);
            MeasureBoundaries(plates, distance, width, height, macroSettings, coreRequired, ref v);

            var stage2a = Stage2AMacroStructure.Run(worldSeed, terrainSettings, macroSettings);
            v.AcceptedAxes = stage2a.AcceptedAxes().Count;
            v.ChainsDiscovered = stage2a.BuildReport.ChainsDiscovered;
            v.ChainsAccepted = stage2a.BuildReport.ChainsAccepted;

            CountUnchained(
                plates, stage2a, distance, width, height, macroSettings, coreRequired, ref v);

            Classify(ref v, macroSettings, axisRequired, coreRequired);
            return v;
        }

        /// <summary>Signed distance to the coast on a coarse lattice, positive inland.</summary>
        private static float[] Rasterise(CoastQuery coast, out int width, out int height)
        {
            width = Mathf.CeilToInt(WorldScale.WorldExtentXMeters / CellMeters) + 1;
            height = Mathf.CeilToInt(WorldScale.WorldExtentZMeters / CellMeters) + 1;

            var field = new float[width * height];
            var crossings = new List<float>(16);

            for (var z = 0; z < height; z++)
            {
                var worldZ = z * CellMeters;
                coast.RowCrossings(worldZ, crossings);
                var row = z * width;

                for (var x = 0; x < width; x++)
                {
                    var worldX = x * CellMeters;
                    var inside = CoastQuery.InsideFromSortedCrossings(crossings, worldX);
                    var d = coast.NearestDistance(worldX, worldZ);
                    field[row + x] = inside ? d : -d;
                }
            }

            return field;
        }

        private static float Sample(float[] field, int width, int height, Vector2 p)
        {
            var x = Mathf.RoundToInt(p.x / CellMeters);
            var z = Mathf.RoundToInt(p.y / CellMeters);
            if (x < 0 || z < 0 || x >= width || z >= height)
            {
                return float.NegativeInfinity;
            }

            return field[z * width + x];
        }

        /// <summary>
        /// The best straight corridor anywhere on the island, in any direction.
        /// </summary>
        /// <remarks>
        /// Straight, deliberately. A curved corridor can only be longer than the straight one it
        /// contains, so this is a lower bound on what the island offers - and a lower bound is what a
        /// feasibility claim needs: if even the best straight strip cannot hold a massif, no axis can.
        /// </remarks>
        private static void SweepCorridors(
            float[] field,
            int width,
            int height,
            MacroStructureSettings settings,
            float coreRequired,
            ref Verdict v)
        {
            var halfWidth = settings.MinUsableHalfWidthMeters;
            var clearance = settings.MassifCoastClearanceMeters;
            var setback = settings.CoastSetbackMeters;

            var centre = new Vector2(
                WorldScale.WorldExtentXMeters * 0.5f, WorldScale.WorldExtentZMeters * 0.5f);
            var reach = Mathf.Sqrt(
                WorldScale.WorldExtentXMeters * WorldScale.WorldExtentXMeters
                + WorldScale.WorldExtentZMeters * WorldScale.WorldExtentZMeters) * 0.5f;

            var steps = Mathf.CeilToInt(2f * reach / StepMeters);
            var lines = Mathf.CeilToInt(2f * reach / LineSpacingMeters);

            for (var d = 0; d < Directions; d++)
            {
                var angle = Mathf.PI * d / Directions;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var normal = new Vector2(-direction.y, direction.x);

                for (var l = 0; l <= lines; l++)
                {
                    var offset = -reach + l * LineSpacingMeters;
                    var origin = centre + normal * offset - direction * reach;

                    var coreRun = 0f;
                    var axisRun = 0f;
                    var coreInAxis = 0f;
                    var axisStart = Vector2.zero;

                    for (var t = 0; t <= steps; t++)
                    {
                        var p = origin + direction * (t * StepMeters);
                        var onLand = Sample(field, width, height, p) >= setback;

                        var core = onLand
                                   && Sample(field, width, height, p + normal * halfWidth) >= clearance
                                   && Sample(field, width, height, p - normal * halfWidth) >= clearance;

                        if (core)
                        {
                            coreRun += StepMeters;
                            coreInAxis = Mathf.Max(coreInAxis, coreRun);
                            if (coreRun > v.BestCoreMeters)
                            {
                                v.BestCoreMeters = coreRun;
                                v.BestFrom = p - direction * coreRun;
                                v.BestTo = p;
                            }
                        }
                        else
                        {
                            coreRun = 0f;
                        }

                        if (onLand)
                        {
                            if (axisRun <= 0f)
                            {
                                axisStart = p;
                            }

                            axisRun += StepMeters;
                        }
                        else
                        {
                            if (coreInAxis >= coreRequired)
                            {
                                v.BestAxisMeters = Mathf.Max(v.BestAxisMeters, axisRun);
                            }

                            axisRun = 0f;
                            coreInAxis = 0f;
                            coreRun = 0f;
                        }
                    }

                    if (coreInAxis >= coreRequired)
                    {
                        v.BestAxisMeters = Mathf.Max(v.BestAxisMeters, axisRun);
                    }

                    _ = axisStart;
                }
            }
        }

        /// <summary>The best core each plate boundary could offer, by the same test.</summary>
        private static void MeasureBoundaries(
            MacroPlates plates,
            float[] field,
            int width,
            int height,
            MacroStructureSettings settings,
            float coreRequired,
            ref Verdict v)
        {
            var halfWidth = settings.MinUsableHalfWidthMeters;
            var clearance = settings.MassifCoastClearanceMeters;

            for (var e = 0; e < plates.Boundaries.Length; e++)
            {
                var boundary = plates.Boundaries[e];
                var direction = boundary.Direction;
                var normal = new Vector2(-direction.y, direction.x);
                var steps = Mathf.Max(2, Mathf.CeilToInt(boundary.Length / StepMeters));

                var run = 0f;
                var best = 0f;
                for (var i = 0; i <= steps; i++)
                {
                    var p = Vector2.Lerp(boundary.A, boundary.B, i / (float)steps);
                    var core = Sample(field, width, height, p + normal * halfWidth) >= clearance
                               && Sample(field, width, height, p - normal * halfWidth) >= clearance;

                    if (core)
                    {
                        run += boundary.Length / steps;
                        best = Mathf.Max(best, run);
                    }
                    else
                    {
                        run = 0f;
                    }
                }

                v.BestBoundaryCoreMeters = Mathf.Max(v.BestBoundaryCoreMeters, best);
                if (best >= coreRequired)
                {
                    v.BoundariesThatCouldCertify++;
                }

                if (boundary.Convergence < settings.ConvergenceThreshold)
                {
                    continue;
                }

                v.BestConvergentBoundaryCoreMeters =
                    Mathf.Max(v.BestConvergentBoundaryCoreMeters, best);
                if (best >= coreRequired)
                {
                    v.ConvergentBoundariesThatCouldCertify++;
                }
            }
        }

        /// <summary>
        /// Boundaries that had room but never reached certification, because no candidate carried them.
        /// </summary>
        /// <remarks>
        /// Chain acceptance sorts by raw length and is exclusive: a long chain marks its boundaries used
        /// and every shorter chain sharing one is discarded, all of it decided before certification runs.
        /// This counts the boundaries that had the room a massif needs and were dropped that way, which
        /// is the difference between "the island had nowhere to put one" and "the search never looked".
        /// </remarks>
        private static void CountUnchained(
            MacroPlates plates,
            Stage2AResult stage2a,
            float[] field,
            int width,
            int height,
            MacroStructureSettings settings,
            float coreRequired,
            ref Verdict v)
        {
            var carried = new HashSet<long>();
            for (var i = 0; i < stage2a.Candidates.Count; i++)
            {
                var pairs = stage2a.Candidates[i].SourcePlatePairs;
                for (var k = 0; k + 1 < pairs.Length; k += 2)
                {
                    carried.Add(((long)pairs[k] << 32) | (uint)pairs[k + 1]);
                }
            }

            var halfWidth = settings.MinUsableHalfWidthMeters;
            var clearance = settings.MassifCoastClearanceMeters;

            for (var e = 0; e < plates.Boundaries.Length; e++)
            {
                var boundary = plates.Boundaries[e];
                var direction = boundary.Direction;
                var normal = new Vector2(-direction.y, direction.x);
                var steps = Mathf.Max(2, Mathf.CeilToInt(boundary.Length / StepMeters));

                var run = 0f;
                var best = 0f;
                for (var i = 0; i <= steps; i++)
                {
                    var p = Vector2.Lerp(boundary.A, boundary.B, i / (float)steps);
                    var core = Sample(field, width, height, p + normal * halfWidth) >= clearance
                               && Sample(field, width, height, p - normal * halfWidth) >= clearance;
                    run = core ? run + boundary.Length / steps : 0f;
                    best = Mathf.Max(best, run);
                }

                if (best < coreRequired)
                {
                    continue;
                }

                var key = ((long)boundary.PlateA << 32) | (uint)boundary.PlateB;
                if (carried.Contains(key))
                {
                    continue;
                }

                v.RoomyBoundariesNeverChained++;
                if (boundary.Convergence >= settings.ConvergenceThreshold)
                {
                    v.RoomyConvergentBoundariesNeverChained++;
                }
            }
        }

        private static void Classify(
            ref Verdict v,
            MacroStructureSettings settings,
            float axisRequired,
            float coreRequired)
        {
            if (v.AcceptedAxes > 0)
            {
                v.Class = "-";
                v.Why = "carries a massif";
                return;
            }

            var islandCanCarry = v.BestCoreMeters >= coreRequired
                                 && v.BestAxisMeters >= axisRequired;

            if (!islandCanCarry)
            {
                // The island itself has no room. Small, or merely the wrong shape?
                var needed = settings.MassifCoastClearanceMeters + settings.MinUsableHalfWidthMeters;
                if (v.InnerAreaKm2 <= 0.001f)
                {
                    v.Class = "E";
                    v.Why =
                        $"nothing on this island is {settings.MassifCoastClearanceMeters:F0} m from the "
                        + "coast, so no cross-section can stand anywhere";
                    return;
                }

                if (v.MaxInscribedRadius < needed)
                {
                    v.Class = "A";
                    v.Why =
                        $"island too small: its deepest point is {v.MaxInscribedRadius:F0} m from the "
                        + $"coast against the {needed:F0} m a section needs";
                    return;
                }

                v.Class = "B";
                v.Why =
                    $"island large enough ({v.IslandAreaKm2:F2} km2, inscribed {v.MaxInscribedRadius:F0} m) "
                    + $"but its shape offers only {v.BestCoreMeters:F0} m of core against "
                    + $"{coreRequired:F0} m, and {v.BestAxisMeters:F0} m of axis against "
                    + $"{axisRequired:F0} m";
                return;
            }

            // The island has room. Did the geology reach it?
            if (v.ConvergentBoundariesThatCouldCertify > 0)
            {
                v.Class = "D";
                v.Why =
                    $"{v.ConvergentBoundariesThatCouldCertify} convergent boundary/boundaries have room "
                    + $"(best core {v.BestConvergentBoundaryCoreMeters:F0} m) and none was accepted; "
                    + $"{v.RoomyConvergentBoundariesNeverChained} of them were never carried by any "
                    + $"candidate at all, out of {v.ChainsDiscovered - v.ChainsAccepted} chain(s) the "
                    + "exclusive length ranking discarded";
                return;
            }

            if (v.BoundariesThatCouldCertify > 0)
            {
                v.Class = "C";
                v.Why =
                    $"{v.BoundariesThatCouldCertify} boundary/boundaries have room (best core "
                    + $"{v.BestBoundaryCoreMeters:F0} m) but none converges above "
                    + $"{settings.ConvergenceThreshold:F2}, so the geology points elsewhere";
                return;
            }

            v.Class = "B";
            v.Why =
                $"the island offers {v.BestCoreMeters:F0} m of core somewhere, but no plate boundary "
                + $"runs through it (best boundary core {v.BestBoundaryCoreMeters:F0} m)";
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
