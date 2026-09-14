#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using GameDev.World;
using GameDev.World.Terrain;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Bakes one seed through the production terrain path and records what determinism and acceptance
    /// are judged on: height, alphamap, tree and detail hashes per tile, seams, the distance between
    /// the shipped height and TerrainGen's field, and the stage timings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Run in a fresh process per bake; comparing processes is what proves determinism, one bake proves
    /// nothing. Output goes outside <c>Assets</c>, so a validation run never shows up as project churn.
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -projectPath &lt;project&gt; \
    ///   -executeMethod GameDev.EditorTools.OverworldTerrainValidation.RunBatch \
    ///   -terrainSeed 110201 -terrainRun 1 -terrainOut &lt;folder&gt; [-terrainRaw true] [-terrainFromScene true]
    /// </code>
    /// <para>
    /// <c>-terrainFromScene true</c> skips the bake and hashes the tiles the saved scene loads from disk,
    /// which is how a saved bake is checked against the in-process one.
    /// </para>
    /// </remarks>
    public static class OverworldTerrainValidation
    {
        public static void RunBatch()
        {
            var seed = OverworldTerrainMenu.DefaultSeed;
            var run = 0;
            var outDir = Path.Combine(Directory.GetCurrentDirectory(), "TerrainValidation");
            var raw = false;
            var fromScene = false;
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "-terrainSeed": int.TryParse(args[i + 1], out seed); break;
                    case "-terrainRun": int.TryParse(args[i + 1], out run); break;
                    case "-terrainOut": outDir = args[i + 1]; break;
                    case "-terrainRaw": raw = args[i + 1] == "true"; break;
                    case "-terrainFromScene": fromScene = args[i + 1] == "true"; break;
                }
            }

            EditorCoroutineUtility.StartCoroutineOwnerless(Run(seed, run, outDir, raw, fromScene));
        }

        private static IEnumerator Run(int seed, int run, string outDir, bool raw, bool fromScene)
        {
            Directory.CreateDirectory(outDir);
            var culture = CultureInfo.InvariantCulture;

            OverworldTerrainMenu.OpenOrCreateScene();
            var memoryBefore = GC.GetTotalMemory(true);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (!fromScene)
            {
                yield return OverworldTerrainMenu.GenerateSurfaceRoutine(seed, TerrainBakeFlags.Full);
            }

            var totalMs = watch.ElapsedMilliseconds;

            var report = fromScene ? null : OverworldTerrainBake.LastReport;
            var authority = new TerrainAuthorityField(seed);
            var terrains = OverworldTerrainAccess.FindTerrains();
            var sb = new StringBuilder();
            sb.AppendLine($"# Terrain validation — seed {seed}, run {run}");
            sb.AppendLine($"unity {Application.unityVersion}  massifs {authority.MassifCount}  tiles {terrains.Length}");
            sb.AppendLine();
            sb.AppendLine("## timings (ms)");
            sb.AppendLine(fromScene
                ? "not baked: hashes read from the saved scene and its tile assets"
                : $"authority {OverworldTerrainBake.LastAuthorityMs}  heights {report.HeightMs}  derived {report.DerivedMs}  " +
                  $"alphamaps {report.AlphamapMs}  trees {report.TreeMs}  details {report.DetailMs}  surface-step total {totalMs}");
            sb.AppendLine();
            sb.AppendLine("## layers");
            foreach (var layer in OverworldTerrainLayers.EnsureCanonicalLayers())
            {
                sb.AppendLine(
                    $"{layer.name}: diffuse {(layer.diffuseTexture != null ? AssetDatabase.GetAssetPath(layer.diffuseTexture) : "MISSING")}, " +
                    $"normal {(layer.normalMapTexture != null ? AssetDatabase.GetAssetPath(layer.normalMapTexture) : "MISSING")}");
            }

            sb.AppendLine();
            sb.AppendLine("## tiles");
            sb.AppendLine("tile | height | alphamap | trees | details | weight sum min..max | tree prototypes | detail prototypes | layers");

            var heights = new Dictionary<string, float[]>();
            double absSum = 0;
            var absMax = 0f;
            long samples = 0;
            long memory = 0;
            var treesTotal = 0;
            long detailTotal = 0;

            foreach (var terrain in terrains)
            {
                var marker = terrain.GetComponent<OverworldTerrainTile>();
                var tx = marker != null ? marker.TileX : 0;
                var tz = marker != null ? marker.TileZ : 0;
                var data = terrain.terrainData;
                var res = data.heightmapResolution;
                var step = data.size.x / (res - 1);
                var h01 = data.GetHeights(0, 0, res, res);
                var flat = new float[res * res];
                for (var j = 0; j < res; j++)
                {
                    for (var i = 0; i < res; i++)
                    {
                        var metres = h01[j, i] * data.size.y;
                        flat[j * res + i] = metres;
                        var expected = Mathf.Clamp(authority.HeightAt(tx * data.size.x + i * step, tz * data.size.z + j * step), 0f, data.size.y);
                        var d = Mathf.Abs(metres - expected);
                        absSum += d;
                        absMax = Mathf.Max(absMax, d);
                        samples++;
                    }
                }

                heights[$"{tx},{tz}"] = flat;
                var aw = data.alphamapWidth;
                var ah = data.alphamapHeight;
                var alpha = data.GetAlphamaps(0, 0, aw, ah);
                var layers = alpha.GetLength(2);
                var alphaBytes = new float[aw * ah * layers];
                var sumMin = float.MaxValue;
                var sumMax = float.MinValue;
                for (var z = 0; z < ah; z++)
                {
                    for (var x = 0; x < aw; x++)
                    {
                        var sum = 0f;
                        for (var k = 0; k < layers; k++)
                        {
                            alphaBytes[(z * aw + x) * layers + k] = alpha[z, x, k];
                            sum += alpha[z, x, k];
                        }

                        sumMin = Mathf.Min(sumMin, sum);
                        sumMax = Mathf.Max(sumMax, sum);
                    }
                }

                var trees = data.treeInstances;
                var treeValues = new float[trees.Length * 6];
                for (var t = 0; t < trees.Length; t++)
                {
                    treeValues[t * 6] = trees[t].position.x;
                    treeValues[t * 6 + 1] = trees[t].position.y;
                    treeValues[t * 6 + 2] = trees[t].position.z;
                    treeValues[t * 6 + 3] = trees[t].prototypeIndex;
                    treeValues[t * 6 + 4] = trees[t].widthScale;
                    treeValues[t * 6 + 5] = trees[t].rotation;
                }

                var detailLayers = data.detailPrototypes.Length;
                var dres = data.detailResolution;
                var detailValues = new List<float>(dres * dres * Math.Max(1, detailLayers));
                long detail = 0;
                for (var k = 0; k < detailLayers; k++)
                {
                    var map = data.GetDetailLayer(0, 0, dres, dres, k);
                    for (var z = 0; z < dres; z++)
                    {
                        for (var x = 0; x < dres; x++)
                        {
                            detailValues.Add(map[z, x]);
                            detail += map[z, x];
                        }
                    }
                }

                treesTotal += trees.Length;
                detailTotal += detail;
                memory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(data);

                var layerNames = new List<string>();
                foreach (var l in data.terrainLayers)
                {
                    layerNames.Add(l != null ? l.name : "null");
                }

                sb.AppendLine(
                    $"{tx},{tz} | {Hash(flat)} | {Hash(alphaBytes)} | {Hash(treeValues)} ({trees.Length}) | " +
                    $"{Hash(detailValues.ToArray())} ({detail}) | {sumMin.ToString("F4", culture)}..{sumMax.ToString("F4", culture)} | " +
                    $"{data.treePrototypes.Length} | {detailLayers} | {string.Join(",", layerNames)}");

                if (raw)
                {
                    WriteFloats(Path.Combine(outDir, $"h_{seed}_r{run}_{tx}_{tz}.bin"), flat);
                    WriteFloats(Path.Combine(outDir, $"a_{seed}_r{run}_{tx}_{tz}.bin"), alphaBytes);
                    WriteFloats(Path.Combine(outDir, $"t_{seed}_r{run}_{tx}_{tz}.bin"), treeValues);
                    WriteFloats(Path.Combine(outDir, $"d_{seed}_r{run}_{tx}_{tz}.bin"), detailValues.ToArray());
                }
            }

            sb.AppendLine();
            sb.AppendLine("## acceptance");
            sb.AppendLine(
                $"height vs TerrainGen: mean_abs {(absSum / Math.Max(1, samples)).ToString("F5", culture)} m, " +
                $"max_abs {absMax.ToString("F5", culture)} m over {samples} samples");
            sb.AppendLine($"trees {treesTotal}  detail instances {detailTotal}  clamped lattice samples {(report != null ? report.ClampedHeightSamples.ToString(culture) : "n/a")}");
            sb.AppendLine($"TerrainData runtime memory {memory / (1024 * 1024)} MB  managed heap delta {(GC.GetTotalMemory(false) - memoryBefore) / (1024 * 1024)} MB");
            sb.AppendLine(Seams(heights, terrains.Length > 0 ? terrains[0].terrainData.heightmapResolution : 0, culture));

            var names = new List<string>(heights.Keys);
            names.Sort(StringComparer.Ordinal);
            var world = new StringBuilder();
            foreach (var line in sb.ToString().Split('\n'))
            {
                if (line.Length > 3 && char.IsDigit(line[0]) && line[1] == ',')
                {
                    world.Append(line.Trim()).Append(';');
                }
            }

            sb.AppendLine($"WORLD SIGNATURE {Digest(Encoding.UTF8.GetBytes(world.ToString()))}");
            File.WriteAllText(Path.Combine(outDir, $"validation_{seed}_r{run}.md"), sb.ToString());
            Debug.Log($"[TerrainValidation] seed={seed} run={run} -> {outDir}");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static string Seams(Dictionary<string, float[]> heights, int res, CultureInfo culture)
        {
            var sb = new StringBuilder("seams:");
            foreach (var (a, b, alongX) in new[] { ("0,0", "1,0", true), ("0,1", "1,1", true), ("0,0", "0,1", false), ("1,0", "1,1", false) })
            {
                if (!heights.TryGetValue(a, out var ha) || !heights.TryGetValue(b, out var hb))
                {
                    continue;
                }

                var worst = 0f;
                for (var k = 0; k < res; k++)
                {
                    worst = Mathf.Max(worst, alongX
                        ? Mathf.Abs(ha[k * res + res - 1] - hb[k * res])
                        : Mathf.Abs(ha[(res - 1) * res + k] - hb[k]));
                }

                sb.Append($" {a}|{b}={worst.ToString("F4", culture)}");
            }

            return sb.ToString();
        }

        private static void WriteFloats(string path, float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            File.WriteAllBytes(path, bytes);
        }

        private static string Hash(float[] values)
        {
            var bytes = new byte[values.Length * 4];
            Buffer.BlockCopy(values, 0, bytes, 0, bytes.Length);
            return Digest(bytes);
        }

        private static string Digest(byte[] bytes)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var d = md5.ComputeHash(bytes);
                var sb = new StringBuilder(12);
                for (var i = 0; i < 6; i++)
                {
                    sb.Append(d[i].ToString("x2", CultureInfo.InvariantCulture));
                }

                return sb.ToString();
            }
        }
    }
}
#endif
