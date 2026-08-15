using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// 2×2 (or NxM) overworld tile seams: wire neighbors, stitch shared height/splat edges,
    /// and warn when residual discontinuities remain.
    /// Per-tile CoastalBeachShelf / alphamap blur cannot see across tile borders — that is the
    /// usual cause of the visible black/white join line.
    /// </summary>
    public static class TerrainTileSeams
    {
        public const float HeightWarnMeters = 0.08f;
        public const float SplatWarnDelta = 0.12f;

        public static void WireNeighbors(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null || terrains.Length == 0)
                return;

            var grid = BuildGrid(terrains);
            if (grid.Count == 0)
                return;

            foreach (var kv in grid)
            {
                var key = kv.Key;
                var t = kv.Value;
                if (t == null)
                    continue;

                grid.TryGetValue(new Vector2Int(key.x - 1, key.y), out var left);
                grid.TryGetValue(new Vector2Int(key.x + 1, key.y), out var right);
                grid.TryGetValue(new Vector2Int(key.x, key.y + 1), out var top);
                grid.TryGetValue(new Vector2Int(key.x, key.y - 1), out var bottom);
                t.SetNeighbors(left, top, right, bottom);
                t.allowAutoConnect = true;
            }
        }

        /// <summary>Average shared heightmap edges so beach/C# edits do not crack the join.</summary>
        public static void StitchHeights(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null || terrains.Length < 2)
                return;

            var grid = BuildGrid(terrains);
            foreach (var kv in grid)
            {
                var a = kv.Value;
                if (a == null || a.terrainData == null)
                    continue;

                // Right neighbor (shared vertical edge at +X).
                if (grid.TryGetValue(new Vector2Int(kv.Key.x + 1, kv.Key.y), out var right) &&
                    right != null && right.terrainData != null)
                    StitchHeightEdgeVertical(a, right);

                // Top neighbor (shared horizontal edge at +Z).
                if (grid.TryGetValue(new Vector2Int(kv.Key.x, kv.Key.y + 1), out var top) &&
                    top != null && top.terrainData != null)
                    StitchHeightEdgeHorizontal(a, top);
            }

            for (var i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null)
                    terrains[i].Flush();
            }
        }

        /// <summary>Average shared alphamap edges after per-tile splat / blur.</summary>
        public static void StitchAlphamaps(UnityEngine.Terrain[] terrains)
        {
            if (terrains == null || terrains.Length < 2)
                return;

            var grid = BuildGrid(terrains);
            foreach (var kv in grid)
            {
                var a = kv.Value;
                if (a == null || a.terrainData == null)
                    continue;

                if (grid.TryGetValue(new Vector2Int(kv.Key.x + 1, kv.Key.y), out var right) &&
                    right != null && right.terrainData != null)
                    StitchAlphamapEdgeVertical(a, right);

                if (grid.TryGetValue(new Vector2Int(kv.Key.x, kv.Key.y + 1), out var top) &&
                    top != null && top.terrainData != null)
                    StitchAlphamapEdgeHorizontal(a, top);
            }
        }

        /// <summary>
        /// Samples shared edges and logs a Warning when height or splat jumps remain.
        /// Call after stitch so a Warning means residual / unfixable discontinuity.
        /// </summary>
        public static void DiagnoseAndWarn(UnityEngine.Terrain[] terrains, string stage)
        {
            if (terrains == null || terrains.Length < 2)
                return;

            var grid = BuildGrid(terrains);
            var maxHeightDelta = 0f;
            var maxSplatDelta = 0f;
            var worstHeightPair = "";
            var worstSplatPair = "";
            var edgeSamples = 0;

            foreach (var kv in grid)
            {
                var a = kv.Value;
                if (a == null || a.terrainData == null)
                    continue;

                if (grid.TryGetValue(new Vector2Int(kv.Key.x + 1, kv.Key.y), out var right) &&
                    right != null && right.terrainData != null)
                {
                    MeasureVerticalEdge(
                        a, right, ref maxHeightDelta, ref maxSplatDelta,
                        ref worstHeightPair, ref worstSplatPair, ref edgeSamples);
                }

                if (grid.TryGetValue(new Vector2Int(kv.Key.x, kv.Key.y + 1), out var top) &&
                    top != null && top.terrainData != null)
                {
                    MeasureHorizontalEdge(
                        a, top, ref maxHeightDelta, ref maxSplatDelta,
                        ref worstHeightPair, ref worstSplatPair, ref edgeSamples);
                }
            }

            if (edgeSamples == 0)
            {
                Debug.LogWarning(
                    $"[TerrainSeams] {stage}: no adjacent tile pairs found (grid parse failed?). " +
                    "Visible diagonal crack may be missing SetNeighbors / tile layout.");
                return;
            }

            var heightBad = maxHeightDelta > HeightWarnMeters;
            var splatBad = maxSplatDelta > SplatWarnDelta;
            if (!heightBad && !splatBad)
            {
                Debug.Log(
                    $"[TerrainSeams] {stage}: OK edges={edgeSamples} " +
                    $"maxΔh={maxHeightDelta:F3}m maxΔsplat={maxSplatDelta:F3}");
                return;
            }

            Debug.LogWarning(
                $"[TerrainSeams] {stage}: VISIBLE TILE JOIN likely — " +
                $"maxΔh={maxHeightDelta:F3}m (warn>{HeightWarnMeters:F2}) at [{worstHeightPair}], " +
                $"maxΔsplat={maxSplatDelta:F3} (warn>{SplatWarnDelta:F2}) at [{worstSplatPair}]. " +
                "Cause: per-tile CoastalBeachShelf / alphamap blur cannot see neighbor pixels; " +
                "LOD crack also appears without Terrain.SetNeighbors. " +
                "Fix path: StitchHeights + StitchAlphamaps + WireNeighbors after beach/splat.");
        }

        private static Dictionary<Vector2Int, UnityEngine.Terrain> BuildGrid(
            UnityEngine.Terrain[] terrains)
        {
            var grid = new Dictionary<Vector2Int, UnityEngine.Terrain>();
            var tileMeters = WorldScale.OverworldTileMeters;
            for (var i = 0; i < terrains.Length; i++)
            {
                var t = terrains[i];
                if (t == null || t.terrainData == null)
                    continue;

                var pos = t.GetPosition();
                var tx = Mathf.RoundToInt(pos.x / tileMeters);
                var tz = Mathf.RoundToInt(pos.z / tileMeters);
                grid[new Vector2Int(tx, tz)] = t;
            }

            return grid;
        }

        private static void StitchHeightEdgeVertical(UnityEngine.Terrain left, UnityEngine.Terrain right)
        {
            var a = left.terrainData;
            var b = right.terrainData;
            var res = a.heightmapResolution;
            if (b.heightmapResolution != res)
                return;

            var edgeA = a.GetHeights(res - 1, 0, 1, res);
            var edgeB = b.GetHeights(0, 0, 1, res);
            var avg = new float[res, 1];
            for (var z = 0; z < res; z++)
                avg[z, 0] = 0.5f * (edgeA[z, 0] + edgeB[z, 0]);

            a.SetHeights(res - 1, 0, avg);
            b.SetHeights(0, 0, avg);
        }

        private static void StitchHeightEdgeHorizontal(UnityEngine.Terrain bottom, UnityEngine.Terrain top)
        {
            var a = bottom.terrainData;
            var b = top.terrainData;
            var res = a.heightmapResolution;
            if (b.heightmapResolution != res)
                return;

            var edgeA = a.GetHeights(0, res - 1, res, 1);
            var edgeB = b.GetHeights(0, 0, res, 1);
            var avg = new float[1, res];
            for (var x = 0; x < res; x++)
                avg[0, x] = 0.5f * (edgeA[0, x] + edgeB[0, x]);

            a.SetHeights(0, res - 1, avg);
            b.SetHeights(0, 0, avg);
        }

        private static void StitchAlphamapEdgeVertical(UnityEngine.Terrain left, UnityEngine.Terrain right)
        {
            var a = left.terrainData;
            var b = right.terrainData;
            var res = a.alphamapResolution;
            var layers = a.alphamapLayers;
            if (b.alphamapResolution != res || b.alphamapLayers != layers || layers == 0)
                return;

            var edgeA = a.GetAlphamaps(res - 1, 0, 1, res);
            var edgeB = b.GetAlphamaps(0, 0, 1, res);
            var avg = new float[res, 1, layers];
            for (var z = 0; z < res; z++)
            {
                var sum = 0f;
                for (var l = 0; l < layers; l++)
                {
                    avg[z, 0, l] = 0.5f * (edgeA[z, 0, l] + edgeB[z, 0, l]);
                    sum += avg[z, 0, l];
                }

                if (sum > 1e-5f)
                {
                    for (var l = 0; l < layers; l++)
                        avg[z, 0, l] /= sum;
                }
            }

            a.SetAlphamaps(res - 1, 0, avg);
            b.SetAlphamaps(0, 0, avg);
        }

        private static void StitchAlphamapEdgeHorizontal(UnityEngine.Terrain bottom, UnityEngine.Terrain top)
        {
            var a = bottom.terrainData;
            var b = top.terrainData;
            var res = a.alphamapResolution;
            var layers = a.alphamapLayers;
            if (b.alphamapResolution != res || b.alphamapLayers != layers || layers == 0)
                return;

            var edgeA = a.GetAlphamaps(0, res - 1, res, 1);
            var edgeB = b.GetAlphamaps(0, 0, res, 1);
            var avg = new float[1, res, layers];
            for (var x = 0; x < res; x++)
            {
                var sum = 0f;
                for (var l = 0; l < layers; l++)
                {
                    avg[0, x, l] = 0.5f * (edgeA[0, x, l] + edgeB[0, x, l]);
                    sum += avg[0, x, l];
                }

                if (sum > 1e-5f)
                {
                    for (var l = 0; l < layers; l++)
                        avg[0, x, l] /= sum;
                }
            }

            a.SetAlphamaps(0, res - 1, avg);
            b.SetAlphamaps(0, 0, avg);
        }

        private static void MeasureVerticalEdge(
            UnityEngine.Terrain left,
            UnityEngine.Terrain right,
            ref float maxHeightDelta,
            ref float maxSplatDelta,
            ref string worstHeightPair,
            ref string worstSplatPair,
            ref int edgeSamples)
        {
            var a = left.terrainData;
            var b = right.terrainData;
            var hRes = a.heightmapResolution;
            if (b.heightmapResolution != hRes)
                return;

            var sizeY = Mathf.Max(0.01f, a.size.y);
            var edgeA = a.GetHeights(hRes - 1, 0, 1, hRes);
            var edgeB = b.GetHeights(0, 0, 1, hRes);
            var pair = $"{left.name}|{right.name}";
            for (var z = 0; z < hRes; z++)
            {
                var d = Mathf.Abs(edgeA[z, 0] - edgeB[z, 0]) * sizeY;
                edgeSamples++;
                if (d > maxHeightDelta)
                {
                    maxHeightDelta = d;
                    worstHeightPair = pair;
                }
            }

            MeasureSplatVertical(left, right, ref maxSplatDelta, ref worstSplatPair);
        }

        private static void MeasureHorizontalEdge(
            UnityEngine.Terrain bottom,
            UnityEngine.Terrain top,
            ref float maxHeightDelta,
            ref float maxSplatDelta,
            ref string worstHeightPair,
            ref string worstSplatPair,
            ref int edgeSamples)
        {
            var a = bottom.terrainData;
            var b = top.terrainData;
            var hRes = a.heightmapResolution;
            if (b.heightmapResolution != hRes)
                return;

            var sizeY = Mathf.Max(0.01f, a.size.y);
            var edgeA = a.GetHeights(0, hRes - 1, hRes, 1);
            var edgeB = b.GetHeights(0, 0, hRes, 1);
            var pair = $"{bottom.name}|{top.name}";
            for (var x = 0; x < hRes; x++)
            {
                var d = Mathf.Abs(edgeA[0, x] - edgeB[0, x]) * sizeY;
                edgeSamples++;
                if (d > maxHeightDelta)
                {
                    maxHeightDelta = d;
                    worstHeightPair = pair;
                }
            }

            MeasureSplatHorizontal(bottom, top, ref maxSplatDelta, ref worstSplatPair);
        }

        private static void MeasureSplatVertical(
            UnityEngine.Terrain left,
            UnityEngine.Terrain right,
            ref float maxSplatDelta,
            ref string worstSplatPair)
        {
            var a = left.terrainData;
            var b = right.terrainData;
            var res = a.alphamapResolution;
            var layers = a.alphamapLayers;
            if (b.alphamapResolution != res || b.alphamapLayers != layers || layers == 0)
                return;

            var edgeA = a.GetAlphamaps(res - 1, 0, 1, res);
            var edgeB = b.GetAlphamaps(0, 0, 1, res);
            var pair = $"{left.name}|{right.name}";
            for (var z = 0; z < res; z++)
            {
                for (var l = 0; l < layers; l++)
                {
                    var d = Mathf.Abs(edgeA[z, 0, l] - edgeB[z, 0, l]);
                    if (d > maxSplatDelta)
                    {
                        maxSplatDelta = d;
                        worstSplatPair = pair;
                    }
                }
            }
        }

        private static void MeasureSplatHorizontal(
            UnityEngine.Terrain bottom,
            UnityEngine.Terrain top,
            ref float maxSplatDelta,
            ref string worstSplatPair)
        {
            var a = bottom.terrainData;
            var b = top.terrainData;
            var res = a.alphamapResolution;
            var layers = a.alphamapLayers;
            if (b.alphamapResolution != res || b.alphamapLayers != layers || layers == 0)
                return;

            var edgeA = a.GetAlphamaps(0, res - 1, res, 1);
            var edgeB = b.GetAlphamaps(0, 0, res, 1);
            var pair = $"{bottom.name}|{top.name}";
            for (var x = 0; x < res; x++)
            {
                for (var l = 0; l < layers; l++)
                {
                    var d = Mathf.Abs(edgeA[0, x, l] - edgeB[0, x, l]);
                    if (d > maxSplatDelta)
                    {
                        maxSplatDelta = d;
                        worstSplatPair = pair;
                    }
                }
            }
        }
    }
}
