using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>
    /// Continuous shore-distance / signed-distance field for one <see cref="WaterZone"/> flood mask.
    /// Positive = metres inside the lake toward the interior; negative = metres outside on dry land.
    /// Uses an 8-connected chamfer distance (not 4-connected Manhattan) so water meshes get smooth
    /// iso-contours instead of stair-stepped cell edges.
    /// </summary>
    public static class LakeShoreField
    {
        private static readonly int[] OrthoDx = { -1, 1, 0, 0 };
        private static readonly int[] OrthoDz = { 0, 0, -1, 1 };
        private static readonly int[] DiagDx = { -1, -1, 1, 1 };
        private static readonly int[] DiagDz = { -1, 1, -1, 1 };

        /// <summary>
        /// Builds a float SDF on the zone's own grid. Interior cells get +distance-to-shore;
        /// exterior cells get -distance-to-water (capped by <paramref name="exteriorCapMeters"/>).
        /// </summary>
        public static float[,] BuildSignedDistance(WaterZone zone, float exteriorCapMeters = 24f)
        {
            var w = zone.Width;
            var d = zone.Depth;
            var sdf = new float[w, d];
            var ortho = Mathf.Max(zone.CellSize, 0.01f);
            var diag = ortho * 1.41421356f;

            // Interior: distance to nearest dry/border neighbour (shore = 0).
            var interior = new float[w, d];
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (!zone.IsFlooded(x, z))
                    {
                        interior[x, z] = 0f;
                        continue;
                    }

                    var shore =
                        !zone.IsFlooded(x - 1, z) || !zone.IsFlooded(x + 1, z) ||
                        !zone.IsFlooded(x, z - 1) || !zone.IsFlooded(x, z + 1) ||
                        x == 0 || z == 0 || x == w - 1 || z == d - 1;
                    interior[x, z] = shore ? 0f : float.PositiveInfinity;
                }
            }

            ChamferPass(interior, w, d, ortho, diag, floodedOnly: zone, requireFlooded: true);

            // Exterior: distance to nearest flooded cell.
            var exterior = new float[w, d];
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    exterior[x, z] = zone.IsFlooded(x, z) ? 0f : float.PositiveInfinity;
                }
            }

            ChamferPass(exterior, w, d, ortho, diag, floodedOnly: null, requireFlooded: false);

            var cap = Mathf.Max(exteriorCapMeters, ortho);
            for (var x = 0; x < w; x++)
            {
                for (var z = 0; z < d; z++)
                {
                    if (zone.IsFlooded(x, z))
                    {
                        var inside = float.IsInfinity(interior[x, z]) ? ortho : interior[x, z];
                        sdf[x, z] = inside;
                    }
                    else
                    {
                        var outside = float.IsInfinity(exterior[x, z]) ? cap : Mathf.Min(exterior[x, z], cap);
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

            // Smoothstep UV so iso-contours are less "faceted" between coarse flood cells.
            tx = tx * tx * (3f - 2f * tx);
            tz = tz * tz * (3f - 2f * tz);

            var a = At(sdf, zone, x0, z0);
            var b = At(sdf, zone, x0 + 1, z0);
            var c = At(sdf, zone, x0, z0 + 1);
            var d = At(sdf, zone, x0 + 1, z0 + 1);
            var ab = Mathf.Lerp(a, b, tx);
            var cd = Mathf.Lerp(c, d, tx);
            return Mathf.Lerp(ab, cd, tz);
        }

        private static void ChamferPass(
            float[,] dist,
            int w,
            int d,
            float ortho,
            float diag,
            WaterZone floodedOnly,
            bool requireFlooded)
        {
            // Forward + backward sweeps approximate Euclidean distance on the grid.
            for (var pass = 0; pass < 2; pass++)
            {
                for (var z = 0; z < d; z++)
                {
                    for (var x = 0; x < w; x++)
                    {
                        RelaxNeighbors(dist, w, d, x, z, ortho, diag, floodedOnly, requireFlooded);
                    }
                }

                for (var z = d - 1; z >= 0; z--)
                {
                    for (var x = w - 1; x >= 0; x--)
                    {
                        RelaxNeighbors(dist, w, d, x, z, ortho, diag, floodedOnly, requireFlooded);
                    }
                }
            }
        }

        private static void RelaxNeighbors(
            float[,] dist,
            int w,
            int d,
            int x,
            int z,
            float ortho,
            float diag,
            WaterZone floodedOnly,
            bool requireFlooded)
        {
            if (requireFlooded && floodedOnly != null && !floodedOnly.IsFlooded(x, z))
            {
                return;
            }

            var best = dist[x, z];
            for (var i = 0; i < 4; i++)
            {
                var nx = x + OrthoDx[i];
                var nz = z + OrthoDz[i];
                if (nx < 0 || nz < 0 || nx >= w || nz >= d)
                {
                    continue;
                }

                if (requireFlooded && floodedOnly != null && !floodedOnly.IsFlooded(nx, nz))
                {
                    continue;
                }

                var candidate = dist[nx, nz] + ortho;
                if (candidate < best)
                {
                    best = candidate;
                }
            }

            for (var i = 0; i < 4; i++)
            {
                var nx = x + DiagDx[i];
                var nz = z + DiagDz[i];
                if (nx < 0 || nz < 0 || nx >= w || nz >= d)
                {
                    continue;
                }

                if (requireFlooded && floodedOnly != null && !floodedOnly.IsFlooded(nx, nz))
                {
                    continue;
                }

                var candidate = dist[nx, nz] + diag;
                if (candidate < best)
                {
                    best = candidate;
                }
            }

            dist[x, z] = best;
        }

        private static float At(float[,] sdf, WaterZone zone, int x, int z)
        {
            if (x < 0 || z < 0 || x >= zone.Width || z >= zone.Depth)
            {
                return -zone.CellSize * 4f;
            }

            return sdf[x, z];
        }
    }
}
