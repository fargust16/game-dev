#if UNITY_EDITOR
using System.Text;
using GameDev.World;
using GameDev.World.Terrain;
using Pinwheel.Vista;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Read-only inspection of the terrain geometry Vista produced, aimed at one question:
    /// <b>is the visible banding an artifact, and of what?</b>
    /// </summary>
    /// <remarks>
    /// <para>
    /// "It looks striped" is not a measurement, and every candidate cause produces striping that
    /// looks identical from an aerial screenshot. What separates them is the <em>spatial period</em>
    /// of the banding and whether it is aligned to the world axes:
    /// </para>
    /// <list type="bullet">
    /// <item>a biome-canvas facet repeats at the biome's own m/px and is axis-aligned;</item>
    /// <item>an erosion rill repeats near the erosion sim's m/px and is axis-aligned;</item>
    /// <item>real drainage has no single period and no preferred axis.</item>
    /// </list>
    /// <para>
    /// So this dump reports the dominant autocorrelation lag of the curvature field, in metres,
    /// alongside the m/px of every biome and erosion pass actually in the scene — put the two next
    /// to each other and the cause names itself. It also separates interior cells from the tile
    /// border, because seam blending is not an interior artifact.
    /// </para>
    /// <para>
    /// Nothing here writes to the terrain. PNGs go to a scratch folder outside <c>Assets</c>, the
    /// same convention <see cref="VistaLayerWeightDump"/> uses, so the dump never shows up as
    /// project churn.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainDiagnostics
    {
        private const string DumpFolder = "VistaTerrainDump";

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

        [MenuItem("GameDev/Overworld/Vista/Dump Terrain Geometry")]
        public static void DumpMenu() => Dump(exportPng: true);

        [MenuItem("GameDev/Overworld/Vista/Dump Terrain Geometry (log only)")]
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
        /// Read from the scene rather than hardcoded: the whole point is that these figures move
        /// when a biome's area or falloff changes, and a stale table would send the next reader
        /// chasing the wrong stage.
        /// </remarks>
        private static void AppendResolutionBudget(StringBuilder report)
        {
            report.AppendLine();
            report.AppendLine("  Resolution budget (Vista Personal caps the biome canvas at 512 px):");

            var manager = Object.FindAnyObjectByType<VistaManager>();
            if (manager != null)
            {
                var tileMetres = WorldScale.OverworldTileMeters;
                var hmRes = Mathf.Max(2, manager.heightMapResolution);
                report.AppendLine(
                    $"    tile heightmap   {hmRes} px over {tileMetres:F0} m " +
                    $"-> {tileMetres / (hmRes - 1):F2} m/px");
            }

            var biomes = Object.FindObjectsByType<LocalProceduralBiome>(FindObjectsSortMode.None);
            if (biomes == null || biomes.Length == 0)
            {
                report.AppendLine("    <no Local Procedural Biomes in the scene>");
                return;
            }

            for (var i = 0; i < biomes.Length; i++)
            {
                var biome = biomes[i];
                if (biome == null)
                {
                    continue;
                }

                if (!TryEstimateBiomeWidth(biome, out var widthMetres))
                {
                    report.AppendLine($"    {biome.name,-18} <no usable anchor polygon>");
                    continue;
                }

                var res = Mathf.Max(1, biome.baseResolution);
                report.AppendLine(
                    $"    {biome.name,-18} {res} px over ~{widthMetres:F0} m " +
                    $"-> {widthMetres / res:F2} m/px  (falloff {biome.falloffDistance:F0})");
            }

            report.AppendLine(
                "    erosion sim canvas = clamp(biomeWidth / featureSize, 32, min(2048, baseResolution));");
            report.AppendLine(
                "    a featureSize finer than biomeWidth/baseResolution is clamped and carves grid rills.");
        }

        /// <summary>
        /// Approximate world width of a biome's canvas: anchor extent plus falloff on both sides.
        /// </summary>
        private static bool TryEstimateBiomeWidth(LocalProceduralBiome biome, out float widthMetres)
        {
            widthMetres = 0f;
            var anchors = biome.anchors;
            if (anchors == null || anchors.Length < 3)
            {
                return false;
            }

            var halfWidth = 0f;
            for (var i = 0; i < anchors.Length; i++)
            {
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(anchors[i].x));
            }

            var scaleX = Mathf.Abs(biome.transform.lossyScale.x);
            widthMetres = 2f * (halfWidth * scaleX + biome.falloffDistance);
            return widthMetres > 1f;
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
    }
}
#endif
