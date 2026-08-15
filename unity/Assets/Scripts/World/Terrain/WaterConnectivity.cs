using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Terrain
{
    /// <summary>
    /// Classifies heightmap cells at/below Sea Level into ocean (touches border) vs inland lakes.
    /// Can raise lowest saddles to seal leaking basins.
    /// </summary>
    public static class WaterConnectivity
    {
        public struct InlandRegion
        {
            public int Id;
            public int CellCount;
            public bool TouchesBorder;
            public float MinHeight01;
            public List<Vector2Int> Cells;
        }

        public struct ValidationResult
        {
            public int OceanCells;
            public int InlandLakeCount;
            public List<InlandRegion> InlandRegions;
            public bool BorderSubmerged;
            public float MinBorderHeightMeters;
        }

        public static ValidationResult Analyze(UnityEngine.Terrain terrain)
        {
            var result = new ValidationResult
            {
                InlandRegions = new List<InlandRegion>(),
            };
            if (terrain == null || terrain.terrainData == null)
            {
                return result;
            }

            var data = terrain.terrainData;
            var res = data.heightmapResolution;
            var size = data.size;
            var heights = data.GetHeights(0, 0, res, res);
            var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
            var labels = new int[res, res];
            var nextId = 1;

            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    if (labels[z, x] != 0 || heights[z, x] > sea01)
                    {
                        continue;
                    }

                    Flood(heights, labels, res, sea01, x, z, nextId, terrain, out var region);
                    if (region.TouchesBorder)
                    {
                        result.OceanCells += region.CellCount;
                    }
                    else if (region.CellCount >= WorldScale.LakeMinCells)
                    {
                        result.InlandRegions.Add(region);
                        result.InlandLakeCount++;
                    }

                    nextId++;
                }
            }

            var minBorder = float.MaxValue;
            var anyWorldBorder = false;
            for (var z = 0; z < res; z++)
            {
                for (var x = 0; x < res; x++)
                {
                    if (!IsWorldOceanBorderCell(terrain, x, z, res))
                    {
                        continue;
                    }

                    anyWorldBorder = true;
                    minBorder = Mathf.Min(minBorder, heights[z, x]);
                }
            }

            if (!anyWorldBorder)
            {
                minBorder = 1f;
            }

            result.MinBorderHeightMeters = minBorder * size.y;
            result.BorderSubmerged = !anyWorldBorder ||
                result.MinBorderHeightMeters <=
                WorldScale.SeaLevelMeters - WorldScale.MinimumSubmersionMeters;
            return result;
        }

        /// <summary>
        /// Raise rims around inland regions that somehow connect after carve; returns sealed count.
        /// </summary>
        public static int SealLeakingBasins(UnityEngine.Terrain terrain, int maxPasses = 2)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return 0;
            }

            var sealedCount = 0;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var data = terrain.terrainData;
                var res = data.heightmapResolution;
                var size = data.size;
                var heights = data.GetHeights(0, 0, res, res);
                var sea01 = Mathf.Clamp01(WorldScale.SeaLevelMeters / Mathf.Max(0.01f, size.y));
                var rim01 = Mathf.Clamp01(
                    (WorldScale.SeaLevelMeters + WorldScale.LakeSafetyMarginMeters) /
                    Mathf.Max(0.01f, size.y));

                // Seed ocean only from WORLD perimeter wet cells (not interior 2×2 seams).
                var ocean = new bool[res, res];
                var q = new Queue<Vector2Int>();
                for (var z = 0; z < res; z++)
                {
                    for (var x = 0; x < res; x++)
                    {
                        if (!IsWorldOceanBorderCell(terrain, x, z, res))
                        {
                            continue;
                        }

                        TryEnqueueOcean(heights, ocean, q, res, sea01, x, z);
                    }
                }

                while (q.Count > 0)
                {
                    var c = q.Dequeue();
                    TryEnqueueOcean(heights, ocean, q, res, sea01, c.x + 1, c.y);
                    TryEnqueueOcean(heights, ocean, q, res, sea01, c.x - 1, c.y);
                    TryEnqueueOcean(heights, ocean, q, res, sea01, c.x, c.y + 1);
                    TryEnqueueOcean(heights, ocean, q, res, sea01, c.x, c.y - 1);
                }

                // Raise dry neighbors of ocean that are below rim (saddle seal near interior).
                var changed = false;
                for (var z = 1; z < res - 1; z++)
                {
                    for (var x = 1; x < res - 1; x++)
                    {
                        if (ocean[z, x] || heights[z, x] > sea01)
                        {
                            continue;
                        }

                        // Inland wet cell that has a path? If any 4-neighbor is ocean-adjacent dry below rim, raise.
                        var nearOcean = ocean[z, x + 1] || ocean[z, x - 1] || ocean[z + 1, x] || ocean[z - 1, x];
                        if (!nearOcean)
                        {
                            continue;
                        }

                        // This inland wet touches ocean → raise intervening dry cells around it to rim.
                        for (var oz = -2; oz <= 2; oz++)
                        {
                            for (var ox = -2; ox <= 2; ox++)
                            {
                                var nx = x + ox;
                                var nz = z + oz;
                                if (nx <= 0 || nz <= 0 || nx >= res - 1 || nz >= res - 1)
                                {
                                    continue;
                                }

                                if (heights[nz, nx] < rim01 && !ocean[nz, nx])
                                {
                                    // Only raise cells that are currently above sea (dry bridge) or thin saddles.
                                    if (heights[nz, nx] > sea01 - 0.002f)
                                    {
                                        heights[nz, nx] = Mathf.Max(heights[nz, nx], rim01);
                                        changed = true;
                                        sealedCount++;
                                    }
                                }
                            }
                        }
                    }
                }

                if (changed)
                {
                    TerrainHeightWrite.Commit(terrain, heights);
                }
                else
                {
                    break;
                }
            }

            return sealedCount;
        }

        /// <summary>
        /// Enforce lake rim above SeaLevel + safety margin for planned lake footprints.
        /// </summary>
        private static void Flood(
            float[,] heights,
            int[,] labels,
            int res,
            float sea01,
            int sx,
            int sz,
            int id,
            UnityEngine.Terrain terrain,
            out InlandRegion region)
        {
            region = new InlandRegion
            {
                Id = id,
                Cells = new List<Vector2Int>(),
                MinHeight01 = 1f,
            };
            var q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(sx, sz));
            labels[sz, sx] = id;
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                region.Cells.Add(c);
                region.CellCount++;
                region.MinHeight01 = Mathf.Min(region.MinHeight01, heights[c.y, c.x]);
                if (IsWorldOceanBorderCell(terrain, c.x, c.y, res))
                {
                    region.TouchesBorder = true;
                }

                TryFlood(heights, labels, q, res, sea01, id, c.x + 1, c.y);
                TryFlood(heights, labels, q, res, sea01, id, c.x - 1, c.y);
                TryFlood(heights, labels, q, res, sea01, id, c.x, c.y + 1);
                TryFlood(heights, labels, q, res, sea01, id, c.x, c.y - 1);
            }
        }

        private static bool IsWorldOceanBorderCell(UnityEngine.Terrain terrain, int px, int pz, int res)
        {
            if (terrain == null || terrain.terrainData == null)
            {
                return px == 0 || pz == 0 || px == res - 1 || pz == res - 1;
            }

            var pos = terrain.transform.position;
            var size = terrain.terrainData.size;
            var wx = pos.x + (px / (float)(res - 1)) * size.x;
            var wz = pos.z + (pz / (float)(res - 1)) * size.z;
            var cell = Mathf.Max(size.x / (res - 1), size.z / (res - 1)) * 1.25f;
            var worldX = WorldScale.WorldExtentXMeters;
            var worldZ = WorldScale.WorldExtentZMeters;
            return wx <= cell || wz <= cell || wx >= worldX - cell || wz >= worldZ - cell;
        }

        private static void TryFlood(
            float[,] heights,
            int[,] labels,
            Queue<Vector2Int> q,
            int res,
            float sea01,
            int id,
            int x,
            int z)
        {
            if (x < 0 || z < 0 || x >= res || z >= res)
            {
                return;
            }

            if (labels[z, x] != 0 || heights[z, x] > sea01)
            {
                return;
            }

            labels[z, x] = id;
            q.Enqueue(new Vector2Int(x, z));
        }

        private static void TryEnqueueOcean(
            float[,] heights,
            bool[,] ocean,
            Queue<Vector2Int> q,
            int res,
            float sea01,
            int x,
            int z)
        {
            if (x < 0 || z < 0 || x >= res || z >= res)
            {
                return;
            }

            if (ocean[z, x] || heights[z, x] > sea01)
            {
                return;
            }

            ocean[z, x] = true;
            q.Enqueue(new Vector2Int(x, z));
        }
    }
}
