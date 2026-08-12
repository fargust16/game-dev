using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Continuous shore-distance / signed-distance field for one <see cref="WaterZone"/> flood mask.
    /// Positive = metres inside the lake toward the interior; negative = metres outside on dry land.
    /// Used by <see cref="LakeTerrainCarver"/> (depth profile) and <see cref="WaterMeshBuilder"/>
    /// (smooth shoreline via fine marching squares) so shape → basin → mesh share one geometry.
    /// </summary>
    public static class LakeShoreField
    {
        /// <summary>
        /// Builds a float SDF on the zone's own grid. Interior cells get +distance-to-shore;
        /// exterior cells get -distance-to-water (capped by <paramref name="exteriorCapMeters"/>).
        /// </summary>
        public static float[,] BuildSignedDistance(WaterZone zone, float exteriorCapMeters = 24f)
        {
            var w = zone.Width;
            var d = zone.Depth;
            var sdf = new float[w, d];
            var dist = new float[w, d];
            var queue = new Queue<(int X, int Z)>();

            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    dist[x, z] = float.PositiveInfinity;
                }
            }

            // Seed shore: flooded cells adjacent to dry (or border).
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        continue;
                    }

                    var shore =
                        !zone.IsFlooded(x - 1, z) || !zone.IsFlooded(x + 1, z) ||
                        !zone.IsFlooded(x, z - 1) || !zone.IsFlooded(x, z + 1);
                    if (shore)
                    {
                        dist[x, z] = 0f;
                        queue.Enqueue((x, z));
                    }
                }
            }

            // Interior BFS (metres ≈ cell steps * CellSize).
            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                var next = dist[x, z] + zone.CellSize;
                Relax(zone, dist, queue, x - 1, z, next, interiorOnly: true);
                Relax(zone, dist, queue, x + 1, z, next, interiorOnly: true);
                Relax(zone, dist, queue, x, z - 1, next, interiorOnly: true);
                Relax(zone, dist, queue, x, z + 1, next, interiorOnly: true);
            }

            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (zone.IsFlooded(x, z))
                    {
                        sdf[x, z] = float.IsInfinity(dist[x, z]) ? zone.CellSize : dist[x, z];
                    }
                    else
                    {
                        sdf[x, z] = float.NegativeInfinity;
                    }
                }
            }

            // Exterior distance from water.
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    dist[x, z] = float.PositiveInfinity;
                }
            }

            queue.Clear();
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (zone.IsFlooded(x, z))
                    {
                        dist[x, z] = 0f;
                        queue.Enqueue((x, z));
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (x, z) = queue.Dequeue();
                var next = dist[x, z] + zone.CellSize;
                if (next > exteriorCapMeters)
                {
                    continue;
                }

                RelaxExterior(dist, queue, w, d, x - 1, z, next);
                RelaxExterior(dist, queue, w, d, x + 1, z, next);
                RelaxExterior(dist, queue, w, d, x, z - 1, next);
                RelaxExterior(dist, queue, w, d, x, z + 1, next);
            }

            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        var outside = float.IsInfinity(dist[x, z]) ? exteriorCapMeters : dist[x, z];
                        sdf[x, z] = -outside;
                    }
                }
            }

            return sdf;
        }

        /// <summary>Bilinear sample of an SDF in world XZ (metres). Outside the grid → large negative.</summary>
        public static float Sample(WaterZone zone, float[,] sdf, float worldX, float worldZ)
        {
            var u = (worldX - zone.OriginX) / zone.CellSize - 0.5f;
            var v = (worldZ - zone.OriginZ) / zone.CellSize - 0.5f;
            var x0 = Mathf.FloorToInt(u);
            var z0 = Mathf.FloorToInt(v);
            var tx = u - x0;
            var tz = v - z0;

            var a = At(sdf, zone, x0, z0);
            var b = At(sdf, zone, x0 + 1, z0);
            var c = At(sdf, zone, x0, z0 + 1);
            var d = At(sdf, zone, x0 + 1, z0 + 1);
            var ab = Mathf.Lerp(a, b, tx);
            var cd = Mathf.Lerp(c, d, tx);
            return Mathf.Lerp(ab, cd, tz);
        }

        private static float At(float[,] sdf, WaterZone zone, int x, int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth)
            {
                return -zone.CellSize * 4f;
            }

            return sdf[x, z];
        }

        private static void Relax(
            WaterZone zone, float[,] dist, Queue<(int X, int Z)> queue,
            int x, int z, float next, bool interiorOnly)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth)
            {
                return;
            }

            if (interiorOnly && !zone.IsFlooded(x, z))
            {
                return;
            }

            if (next >= dist[x, z])
            {
                return;
            }

            dist[x, z] = next;
            queue.Enqueue((x, z));
        }

        private static void RelaxExterior(
            float[,] dist, Queue<(int X, int Z)> queue, int w, int d, int x, int z, float next)
        {
            if (x < 0 || z < 0 || x >= w || z >= d)
            {
                return;
            }

            if (next >= dist[x, z])
            {
                return;
            }

            dist[x, z] = next;
            queue.Enqueue((x, z));
        }
    }
}
