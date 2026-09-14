#if UNITY_EDITOR
using System.Text;
using GameDev.World;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Read-only inspection of the baked terrain geometry, aimed at one question:
    /// <b>is the visible banding an artifact, and of what?</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// "It looks striped" is not a measurement, and every candidate cause produces striping that
    /// looks identical from an aerial screenshot. What separates them is the <em>spatial period</em>
    /// of the banding and whether it is aligned to the world axes:
    /// </para>
    /// <list type="bullet">
    /// <item>a resampling facet repeats at a map's own m/px and is axis-aligned;</item>
    /// <item>real drainage has no single period and no preferred axis.</item>
    /// </list>
    /// <para>
    /// So this dump reports the dominant autocorrelation lag of the curvature field, in metres,
    /// alongside the m/px of every terrain map actually in the scene — put the two next to each
    /// other and the cause names itself. It also separates interior cells from the tile
    /// border, because seam blending is not an interior artifact.
    /// </para>
    /// <para>
    /// Nothing here writes to the terrain. PNGs go to a scratch folder outside <c>Assets</c>, so the
    /// dump never shows up as project churn.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainDiagnostics
    {
        private const string DumpFolder = "TerrainDump";

        /// <summary>Cells at each tile edge treated as seam rather than interior.</summary>
        private const int SeamCells = 2;

        /// <summary>Widest banding period considered, in cells. 128 x 2.73 m covers ~350 m.</summary>
        private const int MaxLagCells = 128;

        /// <summary>
        /// Lag at which the second-difference stencil correlates with itself regardless of terrain.
        /// </summary>
        private const int StencilFloorLag = 2;

        /// <summary>First lag whose correlation is about the ground rather than the operator.</summary>
        private const int FirstRealLag = 3;

        /// <summary>Orientation histogram resolution: 12 bins of 15 degrees over 0..180.</summary>
        private const int OrientationBins = 12;

        [MenuItem("GameDev/Overworld/Terrain/Dump Terrain Geometry")]
        public static void DumpMenu() => Dump(exportPng: true);

        [MenuItem("GameDev/Overworld/Terrain/Dump Terrain Geometry (log only)")]
        public static void DumpLogOnlyMenu() => Dump(exportPng: false);

        public static void Dump(bool exportPng)
        {
            // Open the overworld scene when it is not the active one, so the dump reports on the
            // world rather than on whatever happens to be loaded. Never generates: a diagnostic
            // that quietly re-bakes the terrain is measuring its own output.
            if (SceneManager.GetActiveScene().path != OverworldTerrainMenu.ScenePath)
            {
                if (!System.IO.File.Exists(OverworldTerrainMenu.ScenePath))
                {
                    Debug.LogWarning(
                        $"[TerrainDump] No scene at '{OverworldTerrainMenu.ScenePath}'.");
                    return;
                }

                EditorSceneManager.OpenScene(OverworldTerrainMenu.ScenePath, OpenSceneMode.Single);
            }

            var terrains = OverworldTerrainAccess.FindTerrains();
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning(
                    "[TerrainDump] No overworld terrains in the scene — run Generate All first.");
                return;
            }

            var report = new StringBuilder();
            report.AppendLine($"[TerrainDump] {terrains.Length} tile(s)");
            AppendResolutionBudget(report);

            var outputFolder = exportPng ? PrepareOutputFolder() : null;
            for (var t = 0; t < terrains.Length; t++)
            {
                DumpTile(terrains[t], t, outputFolder, report);
            }

            report.AppendLine();
            report.AppendLine(
                "  Read it like this: a dominant period close to one of the m/px figures above is " +
                "that stage's grid showing through. A period far above all of them, with no axis " +
                "preference, is landform - not an artifact.");

            Debug.Log(report.ToString());
            if (outputFolder != null)
            {
                Debug.Log($"[TerrainDump] PNGs written to '{outputFolder}'.");
                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(outputFolder);
                }
            }
        }

        /// <summary>
        /// The resolution every later number has to be compared against.
        /// </summary>
        /// <remarks>
        /// Read from the tiles rather than hardcoded, so a changed resolution cannot leave a stale
        /// table behind.
        /// </remarks>
        private static void AppendResolutionBudget(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("  Resolution budget:");

            var terrain = OverworldTerrainAccess.FindTerrain();
            if (terrain == null || terrain.terrainData == null)
            {
                report.AppendLine("    <no terrain in the scene>");
                return;
            }

            var data = terrain.terrainData;
            var tileMetres = data.size.x;
            var hmRes = Mathf.Max(2, data.heightmapResolution);
            report.AppendLine(
                $"    tile heightmap   {hmRes} px over {tileMetres:F0} m -> {tileMetres / (hmRes - 1):F2} m/px");
            report.AppendLine(
                $"    tile alphamap    {data.alphamapResolution} px -> {tileMetres / Mathf.Max(1, data.alphamapResolution - 1):F2} m/px");
            report.AppendLine(
                $"    tile details     {data.detailResolution} px -> {tileMetres / Mathf.Max(1, data.detailResolution):F2} m/px");
        }

        private static void DumpTile(
            UnityEngine.Terrain terrain, int tileIndex, string outputFolder, StringBuilder report)
        {
            report.AppendLine();
            if (terrain == null || terrain.terrainData == null)
            {
                report.AppendLine($"  tile {tileIndex}: <null terrain data>");
                return;
            }

            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            if (res < 8)
            {
                report.AppendLine($"  tile {tileIndex}: heightmap too small to analyse ({res})");
                return;
            }

            var size = data.size;
            var metresPerCell = size.x / (res - 1);
            var heights = data.GetHeights(0, 0, res, res);

            ReportQuantisation(report, heights, res, size.y);
            ReportDistribution(report, heights, res);

            report.AppendLine(
                $"  tile {tileIndex} '{terrain.name}' pos={terrain.transform.position} " +
                $"size=({size.x:F0},{size.y:F0},{size.z:F0}) heightmap={res} " +
                $"-> {metresPerCell:F2} m/px");

            // Curvature (second difference) in metres. Banding lives here, not in raw height: a
            // gentle slope hides a facet edge that curvature shows as a ridge line.
            var curveX = new float[res, res];
            var curveZ = new float[res, res];
            var minY = float.MaxValue;
            var maxY = float.MinValue;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var h = heights[z, x] * size.y;
                    minY = Mathf.Min(minY, h);
                    maxY = Mathf.Max(maxY, h);
                    if (x > 0 && x < res - 1)
                    {
                        curveX[z, x] =
                            (heights[z, x - 1] - 2f * heights[z, x] + heights[z, x + 1]) * size.y;
                    }

                    if (z > 0 && z < res - 1)
                    {
                        curveZ[z, x] =
                            (heights[z - 1, x] - 2f * heights[z, x] + heights[z + 1, x]) * size.y;
                    }
                }
            }

            var sea = WorldScale.SeaLevelMeters;
            var aboveSea = 0;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    if (heights[z, x] * size.y > sea)
                    {
                        aboveSea++;
                    }
                }
            }

            report.AppendLine(
                $"    height {minY:F1}..{maxY:F1} m, {100f * aboveSea / (res * res):F1}% above sea " +
                $"({sea:F0} m)");

            ReportShelfProfile(report, heights, res, size.y, metresPerCell);
            ReportReachability(report, heights, res, size.y, metresPerCell);
            AppendResidualAnisotropy(report, heights, res, size.y, metresPerCell);
            AppendCurvatureEnergy(report, curveX, curveZ, res, "interior", interior: true);
            AppendCurvatureEnergy(report, curveX, curveZ, res, "seam    ", interior: false);
            AppendDominantPeriod(report, curveX, res, metresPerCell, alongX: true);
            AppendDominantPeriod(report, curveZ, res, metresPerCell, alongX: false);
            AppendOrientation(report, heights, res, size.y, metresPerCell);

            if (outputFolder != null)
            {
                ExportMaps(heights, curveX, curveZ, res, size, tileIndex, terrain.name, outputFolder);
            }
        }

        /// <summary>Slope bands the dry-land histogram is bucketed into, in degrees.</summary>
        private static readonly float[] SlopeBands = { 5f, 15f, 30f, 45f, 60f, 90f };

        /// <summary>
        /// Is the plateau stepped, and how hard are the steps?
        /// </summary>
        /// <remarks>
        /// <para>
        /// The instrument for the terrace stage. A smooth island puts nearly all of its dry land
        /// under 15 deg and almost nothing over 45; a terraced one is bimodal - flat treads plus a
        /// distinct population of risers past 45 deg, which is also the slope a character stops
        /// being able to walk up. The share in the last two bands is therefore both "are the
        /// shelves there" and "how much of the island did they cost".
        /// </para>
        /// <para>
        /// The existing quantisation and flat-run lines cannot answer this. They look for
        /// bit-identical heights, and any erosion pass downstream of a terrace perturbs every cell,
        /// so a perfectly good staircase reads to them as a continuous surface.
        /// </para>
        /// </remarks>
        private static void ReportShelfProfile(
            StringBuilder report, float[,] heights, int res, float heightMetres, float metresPerCell)
        {
            var sea = WorldScale.SeaLevelMeters;
            var counts = new int[SlopeBands.Length];
            var dryCount = 0;

            for (var z = 1; z < res - 1; z++)
            {
                for (var x = 1; x < res - 1; x++)
                {
                    if (heights[z, x] * heightMetres <= sea)
                    {
                        continue;
                    }

                    dryCount++;
                    var gx = (heights[z, x + 1] - heights[z, x - 1]) * heightMetres
                             / (2f * metresPerCell);
                    var gz = (heights[z + 1, x] - heights[z - 1, x]) * heightMetres
                             / (2f * metresPerCell);
                    var degrees = Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg;

                    for (var b = 0; b < SlopeBands.Length; b++)
                    {
                        if (degrees <= SlopeBands[b] || b == SlopeBands.Length - 1)
                        {
                            counts[b]++;
                            break;
                        }
                    }
                }
            }

            if (dryCount == 0)
            {
                report.AppendLine("    slope bands: <no dry land on this tile>");
                return;
            }

            var line = new StringBuilder("    slope bands on dry land:");
            var low = 0f;
            for (var b = 0; b < SlopeBands.Length; b++)
            {
                line.Append(
                    $" {low:F0}-{SlopeBands[b]:F0}={100f * counts[b] / dryCount:F1}%");
                low = SlopeBands[b];
            }

            var risers = counts[SlopeBands.Length - 1] + counts[SlopeBands.Length - 2];
            line.Append($" | risers(>45deg)={100f * risers / dryCount:F1}%");
            report.AppendLine(line.ToString());
        }

        /// <summary>
        /// Steepest slope a character can walk up, in degrees.
        /// </summary>
        /// <remarks>
        /// Unity's <c>CharacterController.slopeLimit</c> default, which
        /// <see cref="GameDev.Player.OverworldPlayerController"/> leaves alone, and the same figure
        /// a default NavMesh agent bakes with. Not the same threshold as
        /// <c>RoadCost.MaxWalkableGrade</c> (0.45, i.e. 24 deg), which is about comfortable roads,
        /// not about whether a person can get up there at all.
        /// </remarks>
        private const float WalkableSlopeDegrees = 45f;

        /// <summary>How far above the waterline the flood fill is seeded from.</summary>
        private const float ShoreBandMetres = 4f;

        /// <summary>
        /// Can a character actually reach every piece of dry land, walking from the shore?
        /// </summary>
        /// <remarks>
        /// <para>
        /// Terracing the plateau buys the multi-level island the reference has, and it can just as
        /// easily buy a shelf ringed by cliffs with no way up - which no screenshot shows and no
        /// player would forgive. Height range and slope histograms cannot tell those apart: both
        /// worlds have the same distribution of steep cells. Only connectivity can.
        /// </para>
        /// <para>
        /// A 4-connected flood fill from the shore, crossing an edge only where the rise over one
        /// cell is inside <see cref="WalkableSlopeDegrees"/>. Diagonals are left out on purpose:
        /// they are longer, so allowing them would pass slopes a character cannot climb.
        /// </para>
        /// <para>
        /// Per tile, seeded from the shore only, so a shelf whose ramp lies on the neighbouring
        /// tile reads as unreachable here. That is why the border count is printed next to it - a
        /// stranded region that touches the tile edge is probably seam truncation, and one that
        /// does not is a real trap.
        /// </para>
        /// </remarks>
        private static void ReportReachability(
            StringBuilder report, float[,] heights, int res, float heightMetres, float metresPerCell)
        {
            var sea = WorldScale.SeaLevelMeters;
            var maxRise = metresPerCell * Mathf.Tan(WalkableSlopeDegrees * Mathf.Deg2Rad);

            var dry = new bool[res * res];
            var reached = new bool[res * res];
            var queue = new System.Collections.Generic.Queue<int>();
            var dryCount = 0;

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var i = z * res + x;
                    var h = heights[z, x] * heightMetres;
                    if (h <= sea)
                    {
                        continue;
                    }

                    dry[i] = true;
                    dryCount++;
                    if (h <= sea + ShoreBandMetres)
                    {
                        reached[i] = true;
                        queue.Enqueue(i);
                    }
                }
            }

            if (dryCount == 0)
            {
                report.AppendLine("    reachable: <no dry land on this tile>");
                return;
            }

            if (queue.Count == 0)
            {
                report.AppendLine(
                    $"    reachable: <no shore on this tile> ({dryCount} dry cells, all above "
                    + $"{sea + ShoreBandMetres:F0} m)");
                return;
            }

            var reachedCount = queue.Count;
            while (queue.Count > 0)
            {
                var i = queue.Dequeue();
                var x = i % res;
                var z = i / res;
                var h = heights[z, x] * heightMetres;

                for (var dir = 0; dir < 4; dir++)
                {
                    var nx = x + (dir == 0 ? 1 : dir == 1 ? -1 : 0);
                    var nz = z + (dir == 2 ? 1 : dir == 3 ? -1 : 0);
                    if (nx < 0 || nx >= res || nz < 0 || nz >= res)
                    {
                        continue;
                    }

                    var n = nz * res + nx;
                    if (!dry[n] || reached[n])
                    {
                        continue;
                    }

                    if (Mathf.Abs(heights[nz, nx] * heightMetres - h) > maxRise)
                    {
                        continue;
                    }

                    reached[n] = true;
                    reachedCount++;
                    queue.Enqueue(n);
                }
            }

            var stranded = dryCount - reachedCount;
            var strandedOnBorder = 0;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var i = z * res + x;
                    if (!dry[i] || reached[i])
                    {
                        continue;
                    }

                    if (x == 0 || z == 0 || x == res - 1 || z == res - 1)
                    {
                        strandedOnBorder++;
                    }
                }
            }

            report.AppendLine(
                $"    reachable: {100f * reachedCount / dryCount:F1}% of dry land from the shore "
                + $"at <={WalkableSlopeDegrees:F0} deg ({stranded} cell(s) stranded, "
                + $"{strandedOnBorder} of them on the tile border)");
        }

        /// <summary>
        /// Anisotropy of the **high-passed** surface: ridge direction with the landform removed.
        /// </summary>
        /// <remarks>
        /// The plain curvature ratio below sums landform and texture together, and an island that is
        /// simply wider than it is tall produces a lopsided ratio by shape alone — no artifact
        /// required. Nine experiments were judged against that number before anyone noticed.
        /// <para>
        /// This subtracts a box-blurred copy of the height field and measures the residual, so it
        /// answers the question that was actually being asked: do the *ridges* run one way, once the
        /// island's own dome is taken out? Read the two numbers together — if they disagree, the
        /// plain one was measuring the outline.
        /// </para>
        /// </remarks>
        private static void AppendResidualAnisotropy(
            StringBuilder report, float[,] heights, int res, float heightMetres, float metresPerCell)
        {
            var radius = Mathf.Max(2, Mathf.RoundToInt(HighPassRadiusMeters / metresPerCell));
            var lo = SeamCells + 1 + radius;
            var hi = res - SeamCells - 1 - radius;
            if (hi - lo < 8)
            {
                report.AppendLine("    residual: <tile too small for the high-pass window>");
                return;
            }

            // Separable box blur; the residual is what the blur could not explain.
            var blurred = new float[res, res];
            var rowSum = new float[res, res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    float acc = 0;
                    var n = 0;
                    for (var k = -radius; k <= radius; k++)
                    {
                        var xi = x + k;
                        if (xi < 0 || xi >= res)
                        {
                            continue;
                        }

                        acc += heights[z, xi];
                        n++;
                    }

                    rowSum[z, x] = acc / n;
                }
            }

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    float acc = 0;
                    var n = 0;
                    for (var k = -radius; k <= radius; k++)
                    {
                        var zi = z + k;
                        if (zi < 0 || zi >= res)
                        {
                            continue;
                        }

                        acc += rowSum[zi, x];
                        n++;
                    }

                    blurred[z, x] = acc / n;
                }
            }

            double sumX = 0, sumZ = 0;
            var count = 0;
            for (var z = lo; z < hi; z++)
            {
                for (var x = lo; x < hi; x++)
                {
                    float R(int zz, int xx) => (heights[zz, xx] - blurred[zz, xx]) * heightMetres;
                    sumX += Mathf.Abs(R(z, x - 1) - 2f * R(z, x) + R(z, x + 1));
                    sumZ += Mathf.Abs(R(z - 1, x) - 2f * R(z, x) + R(z + 1, x));
                    count++;
                }
            }

            if (count == 0)
            {
                report.AppendLine("    residual: <no cells>");
                return;
            }

            var mx = sumX / count;
            var mz = sumZ / count;
            var ratio = mz > 1e-9 ? mx / mz : 0.0;
            report.AppendLine(
                $"    RESIDUAL (high-pass {HighPassRadiusMeters:F0} m) |d2x|={mx:F4} |d2z|={mz:F4} " +
                $"ratio={ratio:F2} ({DescribeAnisotropy(ratio)})");
        }

        /// <summary>Blur radius that separates landform from ridge texture.</summary>
        private const float HighPassRadiusMeters = 40f;

        /// <summary>
        /// Mean |curvature| per axis. A strong asymmetry is the signature of grid-aligned banding.
        /// </summary>
        private static void AppendCurvatureEnergy(
            StringBuilder report, float[,] curveX, float[,] curveZ, int res, string label, bool interior)
        {
            var lo = interior ? SeamCells + 1 : 1;
            var hi = interior ? res - SeamCells - 1 : res - 1;

            double sumX = 0, sumZ = 0;
            var n = 0;
            for (var z = 1; z < res - 1; z++)
            {
                for (var x = 1; x < res - 1; x++)
                {
                    var isInterior = x >= lo && x < hi && z >= lo && z < hi;
                    if (interior != isInterior)
                    {
                        continue;
                    }

                    sumX += Mathf.Abs(curveX[z, x]);
                    sumZ += Mathf.Abs(curveZ[z, x]);
                    n++;
                }
            }

            if (n == 0)
            {
                report.AppendLine($"    {label} curvature: <no cells>");
                return;
            }

            var mx = sumX / n;
            var mz = sumZ / n;
            var ratio = mz > 1e-9 ? mx / mz : 0.0;
            report.AppendLine(
                $"    {label} curvature |d2x|={mx:F4} m |d2z|={mz:F4} m ratio={ratio:F2} " +
                $"({DescribeAnisotropy(ratio)})");
        }

        private static string DescribeAnisotropy(double ratio)
        {
            if (ratio <= 0.0)
            {
                return "degenerate";
            }

            if (ratio > 1.35)
            {
                return "banded along Z - ridges run north-south";
            }

            if (ratio < 0.74)
            {
                return "banded along X - ridges run east-west";
            }

            return "isotropic - no axis preference";
        }

        /// <summary>
        /// Dominant repeat distance of the curvature field along one axis, reported in metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Autocorrelation of the mean-removed curvature, averaged over every line. The top three
        /// peaks are printed because a real landform produces a broad, shallow spread while a grid
        /// artifact produces one sharp spike.
        /// </para>
        /// <para>
        /// <b>Lags below 3 are excluded and reported separately as the stencil floor.</b> The
        /// three-point second difference has a support of 3 cells, so it correlates with itself at
        /// lags 1 and 2 no matter what the terrain does — the first run of this dump reported a
        /// "dominant 5.5 m period" on all four tiles, which is exactly lag 2 at 2.73 m/px and says
        /// nothing about the ground. Compare the floor against the real peaks: a floor far above
        /// them means the signal is dominated by the measurement, not by the terrain.
        /// </para>
        /// </remarks>
        private static void AppendDominantPeriod(
            StringBuilder report, float[,] curve, int res, float metresPerCell, bool alongX)
        {
            var maxLag = Mathf.Min(MaxLagCells, (res - 2 * SeamCells) / 3);
            if (maxLag < 3)
            {
                report.AppendLine("    period: <tile too small>");
                return;
            }

            var corr = new double[maxLag + 1];
            var lines = 0;
            var lo = SeamCells + 1;
            var hi = res - SeamCells - 1;

            for (var line = lo; line < hi; line++)
            {
                // Mean-remove this line so a slope does not masquerade as correlation.
                double mean = 0;
                var count = hi - lo;
                for (var i = lo; i < hi; i++)
                {
                    mean += alongX ? curve[line, i] : curve[i, line];
                }

                mean /= count;

                double variance = 0;
                for (var i = lo; i < hi; i++)
                {
                    var v = (alongX ? curve[line, i] : curve[i, line]) - mean;
                    variance += v * v;
                }

                if (variance < 1e-12)
                {
                    continue;
                }

                for (var lag = StencilFloorLag; lag <= maxLag; lag++)
                {
                    double acc = 0;
                    for (var i = lo; i + lag < hi; i++)
                    {
                        var a = (alongX ? curve[line, i] : curve[i, line]) - mean;
                        var b = (alongX ? curve[line, i + lag] : curve[i + lag, line]) - mean;
                        acc += a * b;
                    }

                    corr[lag] += acc / variance;
                }

                lines++;
            }

            if (lines == 0)
            {
                report.AppendLine($"    period along {(alongX ? "X" : "Z")}: <flat>");
                return;
            }

            var best = new int[3];
            var bestVal = new double[3];
            for (var lag = FirstRealLag; lag <= maxLag; lag++)
            {
                var v = corr[lag] / lines;
                for (var s = 0; s < 3; s++)
                {
                    if (v > bestVal[s])
                    {
                        for (var k = 2; k > s; k--)
                        {
                            bestVal[k] = bestVal[k - 1];
                            best[k] = best[k - 1];
                        }

                        bestVal[s] = v;
                        best[s] = lag;
                        break;
                    }
                }
            }

            var text = new StringBuilder();
            for (var s = 0; s < 3; s++)
            {
                if (best[s] == 0)
                {
                    continue;
                }

                if (s > 0)
                {
                    text.Append(", ");
                }

                text.Append($"{best[s] * metresPerCell:F1} m (r={bestVal[s]:F3})");
            }

            report.AppendLine(
                $"    period along {(alongX ? "X" : "Z")}: {text} " +
                $"[scanned {FirstRealLag * metresPerCell:F1}..{maxLag * metresPerCell:F0} m; " +
                $"stencil floor r={corr[StencilFloorLag] / lines:F3}]");
        }

        /// <summary>
        /// Where the slope actually faces, bucketed into 15-degree bins over 0..180.
        /// </summary>
        /// <remarks>
        /// The decisive test for "grid-aligned". Drainage carved by a real flow simulation points
        /// downhill in every direction, so the histogram is flat; rills locked to the sim grid pile
        /// up in the bins around 0 and 90 degrees. Reported as the share of gradient magnitude
        /// falling in the two axis-aligned bins - above roughly a third is a grid, not a landform.
        /// </remarks>
        private static void AppendOrientation(
            StringBuilder report, float[,] heights, int res, float heightMetres, float metresPerCell)
        {
            var bins = new double[OrientationBins];
            double total = 0;
            var lo = SeamCells + 1;
            var hi = res - SeamCells - 1;

            for (var z = lo; z < hi; z++)
            {
                for (var x = lo; x < hi; x++)
                {
                    var gx = (heights[z, x + 1] - heights[z, x - 1]) * heightMetres / (2f * metresPerCell);
                    var gz = (heights[z + 1, x] - heights[z - 1, x]) * heightMetres / (2f * metresPerCell);
                    var mag = Mathf.Sqrt(gx * gx + gz * gz);
                    if (mag < 1e-5f)
                    {
                        continue;
                    }

                    var angle = Mathf.Atan2(gz, gx) * Mathf.Rad2Deg;
                    if (angle < 0f)
                    {
                        angle += 180f;
                    }

                    if (angle >= 180f)
                    {
                        angle -= 180f;
                    }

                    var bin = Mathf.Clamp((int)(angle / (180f / OrientationBins)), 0, OrientationBins - 1);
                    bins[bin] += mag;
                    total += mag;
                }
            }

            if (total < 1e-9)
            {
                report.AppendLine("    orientation: <flat>");
                return;
            }

            var text = new StringBuilder();
            for (var b = 0; b < OrientationBins; b++)
            {
                if (b > 0)
                {
                    text.Append(' ');
                }

                text.Append($"{100.0 * bins[b] / total:F0}");
            }

            // Bin 0 straddles 0 degrees and bin 6 straddles 90; an even spread puts 1/6 of the
            // energy in that pair.
            var axisShare = (bins[0] + bins[OrientationBins / 2]) / total;
            report.AppendLine(
                $"    orientation %/15deg: {text} | axis-aligned share={100.0 * axisShare:F0}% " +
                $"(even spread = {100.0 * 2 / OrientationBins:F0}%)");
        }

        private static void ExportMaps(
            float[,] heights,
            float[,] curveX,
            float[,] curveZ,
            int res,
            Vector3 size,
            int tileIndex,
            string tileName,
            string outputFolder)
        {
            var minH = float.MaxValue;
            var maxH = float.MinValue;
            var maxCurve = 1e-6f;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    minH = Mathf.Min(minH, heights[z, x]);
                    maxH = Mathf.Max(maxH, heights[z, x]);
                    maxCurve = Mathf.Max(
                        maxCurve,
                        Mathf.Max(Mathf.Abs(curveX[z, x]), Mathf.Abs(curveZ[z, x])));
                }
            }

            var span = Mathf.Max(1e-6f, maxH - minH);
            var metresPerCell = size.x / (res - 1);

            WritePng(
                res, outputFolder, $"tile{tileIndex}_{tileName}_height.png",
                (x, z) => (heights[z, x] - minH) / span);

            // Absolute, not min-max normalised: two bakes are only comparable cell by cell if the
            // scale does not move under them.
            WritePng16(
                res, outputFolder, $"tile{tileIndex}_{tileName}_height16.png",
                (x, z) => heights[z, x]);

            // Curvature is signed: mid grey is flat, so a facet edge reads as a line either way.
            WritePng(
                res, outputFolder, $"tile{tileIndex}_{tileName}_curvature.png",
                (x, z) => 0.5f + 0.5f * (curveX[z, x] + curveZ[z, x]) / maxCurve);

            WritePng(
                res, outputFolder, $"tile{tileIndex}_{tileName}_slope.png",
                (x, z) =>
                {
                    var xi = Mathf.Clamp(x, 1, res - 2);
                    var zi = Mathf.Clamp(z, 1, res - 2);
                    var gx = (heights[zi, xi + 1] - heights[zi, xi - 1]) * size.y / (2f * metresPerCell);
                    var gz = (heights[zi + 1, xi] - heights[zi - 1, xi]) * size.y / (2f * metresPerCell);
                    return Mathf.Atan(Mathf.Sqrt(gx * gx + gz * gz)) * Mathf.Rad2Deg / 90f;
                });
        }

        /// <summary>Height at 16 bits, packed R = high byte, G = low byte.</summary>
        /// <remarks>
        /// Additive: the 8-bit dumps beside it keep their encoding, so earlier images stay
        /// comparable. It exists because the 8-bit height
        /// dump quantises to about 0.48 m per level on this island, which is coarser than several
        /// of the things worth measuring - the overrange knee compresses 2.2 m into roughly 0.5 m,
        /// and at 8 bits that whole feature is one level wide and unreadable.
        /// </remarks>
        private static void WritePng16(
            int res, string outputFolder, string fileName, System.Func<int, int, float> sample)
        {
            var texture = new Texture2D(res, res, TextureFormat.RGB24, mipChain: false);
            var pixels = new Color32[res * res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var v = Mathf.Clamp(Mathf.RoundToInt(sample(x, z) * 65535f), 0, 65535);
                    pixels[z * res + x] = new Color32((byte)(v >> 8), (byte)(v & 0xFF), 0, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);
            System.IO.File.WriteAllBytes(
                System.IO.Path.Combine(outputFolder, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static void WritePng(
            int res, string outputFolder, string fileName, System.Func<int, int, float> sample)
        {
            var texture = new Texture2D(res, res, TextureFormat.RGB24, mipChain: false);
            var pixels = new Color32[res * res];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var v = (byte)Mathf.Clamp(Mathf.RoundToInt(sample(x, z) * 255f), 0, 255);
                    pixels[z * res + x] = new Color32(v, v, v, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false);
            System.IO.File.WriteAllBytes(
                System.IO.Path.Combine(outputFolder, fileName), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static string PrepareOutputFolder()
        {
            var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName;
            var folder = System.IO.Path.Combine(projectRoot, DumpFolder);
            if (System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.Delete(folder, recursive: true);
            }

            System.IO.Directory.CreateDirectory(folder);
            return folder;
        }
        /// <summary>Counts how many distinct height values a tile actually contains.</summary>
        /// <remarks>
        /// A continuous surface stored in floats has nearly one distinct value per sample. A
        /// surface carried through a low-precision buffer somewhere upstream snaps to a lattice,
        /// and the terraces that follow read as contour lines -- which is what the "banding" looks
        /// like once it is rendered instead of reduced to a curvature ratio. The step is reported
        /// in metres so it can be compared against the spacing visible in the scene.
        /// </remarks>
        private static void ReportQuantisation(
            System.Text.StringBuilder report, float[,] heights, int res, float heightMetres)
        {
            var seen = new System.Collections.Generic.HashSet<float>();
            var samples = 0;
            for (var z = SeamCells; z < res - SeamCells; z++)
            {
                for (var x = SeamCells; x < res - SeamCells; x++)
                {
                    seen.Add(heights[z, x]);
                    samples++;
                }
            }

            var sorted = new System.Collections.Generic.List<float>(seen);
            sorted.Sort();

            // The median gap, not the smallest: a handful of near-duplicate floats would drag the
            // minimum to zero and hide a lattice that every other pair sits on.
            var gaps = new System.Collections.Generic.List<float>(sorted.Count);
            for (var i = 1; i < sorted.Count; i++)
            {
                gaps.Add(sorted[i] - sorted[i - 1]);
            }

            gaps.Sort();
            var medianGap = gaps.Count > 0 ? gaps[gaps.Count / 2] : 0f;
            var levels = medianGap > 0f ? Mathf.RoundToInt(1f / medianGap) : 0;

            report.AppendLine(
                $"    QUANTISATION: {sorted.Count} distinct heights over {samples} samples "
                + $"({100f * sorted.Count / samples:F2}%), median step={medianGap:F8} 01-units "
                + $"= {medianGap * heightMetres:F3} m => ~{levels} levels "
                + "(a continuous float surface reads near 100% distinct)");

            // A lattice on its own is harmless -- Unity always stores 16-bit. What makes it
            // *visible* is flat runs: consecutive samples landing on the same level, which read as
            // contour terraces. Real relief steps every cell or two, so the run length is the
            // discriminator, and it is measured along both axes because the artifact is directional.
            ReportFlatRuns(report, heights, res, "X", alongX: true);
            ReportFlatRuns(report, heights, res, "Z", alongX: false);
        }

        /// <summary>Mean length, in cells, of a run of samples sharing one quantised height.</summary>
        private static void ReportFlatRuns(
            System.Text.StringBuilder report, float[,] heights, int res, string axis, bool alongX)
        {
            long runs = 0;
            long cells = 0;
            var longest = 0;
            for (var a = SeamCells; a < res - SeamCells; a++)
            {
                var run = 1;
                for (var b = SeamCells + 1; b < res - SeamCells; b++)
                {
                    var prev = alongX ? heights[a, b - 1] : heights[b - 1, a];
                    var cur = alongX ? heights[a, b] : heights[b, a];
                    if (cur == prev)
                    {
                        run++;
                        continue;
                    }

                    runs++;
                    cells += run;
                    longest = Mathf.Max(longest, run);
                    run = 1;
                }

                runs++;
                cells += run;
                longest = Mathf.Max(longest, run);
            }

            report.AppendLine(
                $"    FLAT RUNS along {axis}: mean={(double)cells / runs:F2} cells, "
                + $"longest={longest} (1.0 = every cell a new level, i.e. no terracing)");
        }

        /// <summary>Percentiles of the raw 0..1 height, for matching one signal against another.</summary>
        /// <remarks>
        /// Two noise stacks can share a min and a max and still distribute their mass completely
        /// differently, which is exactly what dropping the per-octave remap does. Percentiles are
        /// what a post-stack normalisation has to match, so they are what gets printed.
        /// </remarks>
        private static void ReportDistribution(
            System.Text.StringBuilder report, float[,] heights, int res)
        {
            var v = new System.Collections.Generic.List<float>((res - 2 * SeamCells) * (res - 2 * SeamCells));
            for (var z = SeamCells; z < res - SeamCells; z++)
            {
                for (var x = SeamCells; x < res - SeamCells; x++)
                {
                    v.Add(heights[z, x]);
                }
            }

            v.Sort();
            float At(float q) => v[Mathf.Clamp(Mathf.RoundToInt(q * (v.Count - 1)), 0, v.Count - 1)];
            var mean = 0.0;
            for (var i = 0; i < v.Count; i++)
            {
                mean += v[i];
            }

            mean /= v.Count;
            report.AppendLine(
                $"    DISTRIBUTION (01 units): min={At(0f):F4} p05={At(0.05f):F4} p25={At(0.25f):F4} "
                + $"p50={At(0.5f):F4} p75={At(0.75f):F4} p95={At(0.95f):F4} max={At(1f):F4} "
                + $"mean={mean:F4}");
        }

    }
}
#endif
