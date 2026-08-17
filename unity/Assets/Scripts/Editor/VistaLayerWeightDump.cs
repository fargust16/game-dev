#if UNITY_EDITOR
using System.Text;
using GameDev.World.Terrain;
using UnityEditor;
using UnityEngine;

namespace GameDev.EditorTools
{
    /// <summary>
    /// Read-only inspection of what Vista actually wrote into each tile's alphamap.
    /// </summary>
    /// <remarks>
    /// Answers the two questions that decide whether a terrain-texturing problem is graph-side or
    /// material-side: is the per-tile <c>terrainLayers</c> stack the one we expect, and do the
    /// weights sum to 1? Vista authors that array per tile in
    /// <c>TerrainTile.PopulateLayerWeights</c> from the biomes that touched the tile, so two tiles
    /// can legitimately end up with different stacks — which is invisible in the scene view but
    /// makes every downstream layer index mean something different.
    /// <para>
    /// Nothing here writes to the terrain. The PNGs go to a scratch folder outside Assets so the
    /// dump never shows up as project churn.
    /// </para>
    /// </remarks>
    public static class VistaLayerWeightDump
    {
        /// <summary>Sub-folder of the project root that receives the exported channel PNGs.</summary>
        private const string DumpFolder = "VistaLayerDump";

        /// <summary>Below this total weight a pixel counts as outside every biome, not as painted.</summary>
        private const float CoverageEpsilon = 0.01f;

        /// <summary>Resolution of the per-layer weight histogram.</summary>
        private const int HistogramBuckets = 10;

        [MenuItem("GameDev/Overworld/Vista/Dump Layer Weights")]
        public static void DumpMenu()
        {
            Dump(exportPng: true);
        }

        [MenuItem("GameDev/Overworld/Vista/Dump Layer Weights (log only)")]
        public static void DumpLogOnlyMenu()
        {
            Dump(exportPng: false);
        }

        /// <summary>
        /// Logs the layer stack and weight statistics of every overworld tile.
        /// </summary>
        /// <param name="exportPng">Also write one grayscale PNG per layer per tile.</param>
        public static void Dump(bool exportPng)
        {
            var terrains = OverworldTerrainAccess.FindTerrains();
            if (terrains == null || terrains.Length == 0)
            {
                Debug.LogWarning("[LayerDump] No overworld terrains in the scene.");
                return;
            }

            var outputFolder = exportPng ? PrepareOutputFolder() : null;
            var report = new StringBuilder();
            report.AppendLine($"[LayerDump] {terrains.Length} tile(s)");

            for (var t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];
                if (terrain == null || terrain.terrainData == null)
                {
                    report.AppendLine($"  tile {t}: <null terrain data>");
                    continue;
                }

                DumpTile(terrain, t, outputFolder, report);
            }

            Debug.Log(report.ToString());
            if (outputFolder != null)
            {
                Debug.Log($"[LayerDump] PNGs written to '{outputFolder}'.");
                EditorUtility.RevealInFinder(outputFolder);
            }
        }

        private static void DumpTile(
            UnityEngine.Terrain terrain, int tileIndex, string outputFolder, StringBuilder report)
        {
            var data = terrain.terrainData;
            var layers = data.terrainLayers;
            var layerCount = layers != null ? layers.Length : 0;

            report.AppendLine();
            report.AppendLine(
                $"  tile {tileIndex} '{terrain.gameObject.name}' pos={terrain.transform.position} " +
                $"alphamapRes={data.alphamapResolution} alphamapTextures={data.alphamapTextureCount} " +
                $"layers={layerCount} material='{DescribeMaterial(terrain)}'");

            if (layerCount == 0)
            {
                report.AppendLine("    <no terrain layers — Vista wrote nothing>");
                return;
            }

            for (var i = 0; i < layerCount; i++)
            {
                var layer = layers[i];
                var diffuse = layer != null && layer.diffuseTexture != null
                    ? layer.diffuseTexture.name
                    : "NULL";
                var tile = layer != null ? layer.tileSize : Vector2.zero;
                report.AppendLine(
                    $"    [{i}] {(layer != null ? layer.name : "NULL")} " +
                    $"diffuse='{diffuse}' tileSize={tile.x:F1}x{tile.y:F1}");
            }

            var res = data.alphamapResolution;
            if (res < 2)
            {
                report.AppendLine("    <alphamap too small to sample>");
                return;
            }

            var maps = data.GetAlphamaps(0, 0, res, res);
            var channels = maps.GetLength(2);
            var min = new float[channels];
            var max = new float[channels];
            var sum = new double[channels];

            // A histogram is what an average cannot tell you: whether a mask is a soft gradient or a
            // saturated blanket. Two layers can share an average of 0.5 and mean completely different
            // things — one shading smoothly, the other fully on over half the biome and absent from
            // the rest. Saturation is what makes a layer erase everything under it in Weight Blend.
            var histogram = new int[channels, HistogramBuckets];
            for (var c = 0; c < channels; c++)
            {
                min[c] = float.MaxValue;
                max[c] = float.MinValue;
            }

            // Statistics are taken over painted pixels only. Averaging across the whole tile mixes in
            // the ocean, where Vista multiplies every weight by a zero biome mask, and that made the
            // weight sum look broken when it was merely diluted.
            var sumMin = float.MaxValue;
            var sumMax = float.MinValue;
            double sumTotal = 0;
            var pixels = res * res;
            var covered = 0;

            // Dry land is tracked separately, because whole-tile shares answer the wrong question.
            // The seabed graph paints Sand as its unconnected Weight Blend base, so every submerged
            // pixel is sand by construction — a tile that is mostly ocean reports "sand covers 65 %"
            // and it means nothing about how the island looks. Only the land column is comparable
            // between layers.
            var landSum = new double[channels];
            var landSaturated = new int[channels];
            var landPixels = 0;
            var heightRes = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, heightRes, heightRes);
            var sizeY = data.size.y;
            var terrainY = terrain.transform.position.y;

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var pixelSum = 0f;
                    for (var c = 0; c < channels; c++)
                    {
                        pixelSum += maps[z, x, c];
                    }

                    if (pixelSum <= CoverageEpsilon)
                    {
                        continue;
                    }

                    covered++;
                    sumTotal += pixelSum;
                    if (pixelSum < sumMin) sumMin = pixelSum;
                    if (pixelSum > sumMax) sumMax = pixelSum;

                    // Alphamap and heightmap have different resolutions; map by normalized position.
                    var hz = Mathf.Clamp(Mathf.RoundToInt(z * (heightRes - 1f) / (res - 1f)), 0, heightRes - 1);
                    var hx = Mathf.Clamp(Mathf.RoundToInt(x * (heightRes - 1f) / (res - 1f)), 0, heightRes - 1);
                    var isLand = heights[hz, hx] * sizeY + terrainY > GameDev.World.WorldScale.SeaLevelMeters;
                    if (isLand)
                    {
                        landPixels++;
                    }

                    for (var c = 0; c < channels; c++)
                    {
                        var w = maps[z, x, c];
                        sum[c] += w;
                        if (w < min[c]) min[c] = w;
                        if (w > max[c]) max[c] = w;
                        histogram[c, BucketOf(w)]++;

                        if (!isLand)
                        {
                            continue;
                        }

                        landSum[c] += w;
                        if (w >= 0.9f)
                        {
                            landSaturated[c]++;
                        }
                    }
                }
            }

            if (covered == 0)
            {
                report.AppendLine("    <no painted pixels on this tile>");
                return;
            }

            report.AppendLine(
                $"    painted pixels: {100f * covered / pixels:F1}% of the tile " +
                "(the rest is outside every biome mask)");
            report.AppendLine(
                $"    weight sum on painted pixels: min={sumMin:F3} avg={sumTotal / covered:F3} " +
                $"max={sumMax:F3} (expected ≈1.000 — Weight Blend only guarantees this when layer 0 " +
                "is the white base)");

            report.AppendLine(
                "    histogram buckets are 0.0–0.1 … 0.9–1.0, as % of painted pixels. A layer that is " +
                "mostly in the last bucket is saturated and will erase every layer below it.");

            for (var c = 0; c < channels; c++)
            {
                var name = c < layerCount && layers[c] != null ? layers[c].name : $"channel {c}";
                var avg = sum[c] / covered;
                var share = sumTotal > 0 ? 100f * sum[c] / sumTotal : 0f;
                var saturated = 100f * histogram[c, HistogramBuckets - 1] / covered;
                var nearZero = 100f * histogram[c, 0] / covered;
                report.AppendLine(
                    $"    [{c}] {name}: avg={avg:F3} max={max[c]:F3} share={share:F1}% " +
                    $"| zero={nearZero:F1}% saturated={saturated:F1}%");
                report.AppendLine($"        {FormatHistogram(histogram, c, covered)}");

                if (landPixels > 0)
                {
                    report.AppendLine(
                        $"        ON LAND: avg={landSum[c] / landPixels:F3} " +
                        $"saturated={100.0 * landSaturated[c] / landPixels:F1}%");
                }
            }

            report.AppendLine(
                $"    dry land = {100f * landPixels / Mathf.Max(1, covered):F1}% of painted pixels " +
                "— compare layers on the ON LAND rows, not the whole-tile ones: the seabed graph " +
                "paints Sand as its Weight Blend base, so every submerged pixel is sand by design.");

            AppendHeightHistogram(data, report);

            if (channels != layerCount)
            {
                report.AppendLine(
                    $"    !! alphamap has {channels} channels but {layerCount} layers — " +
                    "the layer array was replaced without repacking the alphamap");
            }

            if (outputFolder != null)
            {
                ExportChannels(maps, layers, res, channels, tileIndex, terrain.gameObject.name, outputFolder);
            }
        }

        /// <summary>
        /// Distribution of land height above sea, in tenths of the tile's own land range.
        /// </summary>
        /// <remarks>
        /// Every altitude-keyed mask reads this distribution. If the land clusters in one or two
        /// buckets, a band that looks narrow in the graph editor covers most of the island in
        /// practice — which is the difference between a beach and a desert.
        /// </remarks>
        private static void AppendHeightHistogram(TerrainData data, StringBuilder report)
        {
            var res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);
            var sizeY = Mathf.Max(0.01f, data.size.y);
            var sea = GameDev.World.WorldScale.SeaLevelMeters;

            var maxLand = 0f;
            var landPixels = 0;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var y = heights[z, x] * sizeY;
                    if (y <= sea)
                    {
                        continue;
                    }

                    landPixels++;
                    if (y > maxLand) maxLand = y;
                }
            }

            if (landPixels == 0)
            {
                report.AppendLine("    land above sea: none on this tile");
                return;
            }

            var span = Mathf.Max(0.01f, maxLand - sea);
            var buckets = new int[HistogramBuckets];
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    var y = heights[z, x] * sizeY;
                    if (y <= sea)
                    {
                        continue;
                    }

                    buckets[Mathf.Clamp((int)((y - sea) / span * HistogramBuckets), 0, HistogramBuckets - 1)]++;
                }
            }

            var builder = new StringBuilder(HistogramBuckets * 6);
            for (var b = 0; b < HistogramBuckets; b++)
            {
                if (b > 0) builder.Append(' ');
                builder.Append($"{100f * buckets[b] / landPixels:F0}");
            }

            report.AppendLine(
                $"    land above sea={sea:F0}m up to {maxLand:F0}m; distribution in tenths of that " +
                $"span: {builder}");
        }

        private static int BucketOf(float weight)
        {
            return Mathf.Clamp((int)(weight * HistogramBuckets), 0, HistogramBuckets - 1);
        }

        private static string FormatHistogram(int[,] histogram, int channel, int covered)
        {
            var builder = new StringBuilder(HistogramBuckets * 6);
            for (var b = 0; b < HistogramBuckets; b++)
            {
                if (b > 0)
                {
                    builder.Append(' ');
                }

                builder.Append($"{100f * histogram[channel, b] / covered:F0}");
            }

            return builder.ToString();
        }

        private static void ExportChannels(
            float[,,] maps,
            TerrainLayer[] layers,
            int res,
            int channels,
            int tileIndex,
            string tileName,
            string outputFolder)
        {
            for (var c = 0; c < channels; c++)
            {
                var texture = new Texture2D(res, res, TextureFormat.RGB24, mipChain: false);
                var pixels = new Color32[res * res];
                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        var v = (byte)Mathf.Clamp(Mathf.RoundToInt(maps[z, x, c] * 255f), 0, 255);
                        // Texture rows run bottom-up; alphamaps are indexed [z, x] from the same corner.
                        pixels[z * res + x] = new Color32(v, v, v, 255);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply(updateMipmaps: false);

                var layerName = c < layers.Length && layers[c] != null ? layers[c].name : "unknown";
                var path = System.IO.Path.Combine(
                    outputFolder, $"tile{tileIndex}_{tileName}_L{c}_{layerName}.png");
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
        }

        private static string DescribeMaterial(UnityEngine.Terrain terrain)
        {
            var mat = terrain.materialTemplate;
            if (mat == null)
            {
                return "<none>";
            }

            return mat.shader != null ? $"{mat.name} ({mat.shader.name})" : mat.name;
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
