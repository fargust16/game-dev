using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace GameDev.World.TerrainGen
{
    /// <summary>
    /// The atoms of the landmass: a blue-noise set of crustal sites, the nearest-site partition of
    /// the world rect, and the adjacency between cells.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the structure the coastline is the boundary of, and it is why the coast can have a
    /// bay that curls back or a peninsula with a neck. Any <c>radius(theta)</c> island is star-shaped
    /// about its centre and can express neither.
    /// </para>
    /// <para>
    /// The partition is held as a raster rather than as Voronoi polygons. A polygon partition would
    /// need a Delaunay triangulation with its cocircular and hull cases, and would then have to be
    /// rasterised anyway for the region growth and the boundary trace. The raster is the cheaper and
    /// more robust half of that trade; the published coastline is a polygon regardless, extracted
    /// and refined from the raster boundary.
    /// </para>
    /// <para>
    /// Sites extend past the world rect by <see cref="TerrainGenSettings.CrustMarginMeters"/>.
    /// Without that, every cell along the border is a wedge shaped by the border rather than by the
    /// site distribution, and the coastline inherits the artefact.
    /// </para>
    /// </remarks>
    public sealed class CrustalPartition
    {
        /// <summary>Site positions in world XZ. Some lie outside the world rect.</summary>
        public Vector2[] Sites { get; }

        /// <summary>Raster cell to site index, row-major <c>[z * RasterWidth + x]</c>.</summary>
        public int[] Cells { get; }

        public int RasterWidth { get; }
        public int RasterHeight { get; }
        public float CellMeters { get; }

        /// <summary>Raster cells owned by each site, inside the world rect.</summary>
        public int[] SiteCellCount { get; }

        /// <summary>Centroid of each site's raster cells, in world XZ. Undefined when the count is 0.</summary>
        public Vector2[] SiteCentroid { get; }

        /// <summary>World-space bounds of each site's raster cells (cell corners, not centres).</summary>
        public Vector4[] SiteBounds { get; }

        /// <summary>Site indices adjacent to each site, ascending. Empty for unrepresented sites.</summary>
        public int[][] SiteNeighbours { get; }

        public int SiteCount => Sites.Length;

        /// <summary>Area one raster cell stands for, square metres.</summary>
        public float CellAreaSqm => CellMeters * CellMeters;

        private CrustalPartition(
            Vector2[] sites,
            int[] cells,
            int rasterWidth,
            int rasterHeight,
            float cellMeters,
            int[] siteCellCount,
            Vector2[] siteCentroid,
            Vector4[] siteBounds,
            int[][] siteNeighbours)
        {
            Sites = sites;
            Cells = cells;
            RasterWidth = rasterWidth;
            RasterHeight = rasterHeight;
            CellMeters = cellMeters;
            SiteCellCount = siteCellCount;
            SiteCentroid = siteCentroid;
            SiteBounds = siteBounds;
            SiteNeighbours = siteNeighbours;
        }

        public int RasterIndex(int x, int z) => z * RasterWidth + x;

        /// <summary>World X of a raster cell centre.</summary>
        public float CellCenterX(int x) => (x + 0.5f) * CellMeters;

        /// <summary>World Z of a raster cell centre.</summary>
        public float CellCenterZ(int z) => (z + 0.5f) * CellMeters;

        /// <summary>Site owning a world position, by nearest site. Pure and order-independent.</summary>
        public int SiteAt(float worldX, float worldZ)
        {
            var best = -1;
            var bestSq = float.PositiveInfinity;
            for (var i = 0; i < Sites.Length; i++)
            {
                var dx = Sites[i].x - worldX;
                var dz = Sites[i].y - worldZ;
                var sq = dx * dx + dz * dz;
                // Strict less-than plus ascending iteration makes the tie-break the lower index.
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// True when every raster cell of the site lies inside the world rect inset by
        /// <paramref name="marginMeters"/> - i.e. the site may become land without putting coast
        /// against the world border.
        /// </summary>
        public bool IsInsideMargin(int site, float marginMeters)
        {
            if (site < 0 || site >= Sites.Length || SiteCellCount[site] == 0)
            {
                return false;
            }

            var b = SiteBounds[site];
            return b.x >= marginMeters
                   && b.y >= marginMeters
                   && b.z <= WorldScale.WorldExtentXMeters - marginMeters
                   && b.w <= WorldScale.WorldExtentZMeters - marginMeters;
        }

        public static CrustalPartition Build(Seed.IWorldSeed worldSeed, TerrainGenSettings settings)
        {
            var sites = SampleSites(worldSeed, settings);

            var cellMeters = settings.PartitionCellMeters;
            var rasterWidth = Mathf.Max(2, Mathf.RoundToInt(WorldScale.WorldExtentXMeters / cellMeters));
            var rasterHeight = Mathf.Max(2, Mathf.RoundToInt(WorldScale.WorldExtentZMeters / cellMeters));

            var cells = new int[rasterWidth * rasterHeight];
            var count = new int[sites.Length];
            var sumX = new double[sites.Length];
            var sumZ = new double[sites.Length];
            var bounds = new Vector4[sites.Length];
            for (var i = 0; i < bounds.Length; i++)
            {
                bounds[i] = new Vector4(
                    float.PositiveInfinity, float.PositiveInfinity,
                    float.NegativeInfinity, float.NegativeInfinity);
            }

            for (var z = 0; z < rasterHeight; z++)
            {
                var wz = (z + 0.5f) * cellMeters;
                for (var x = 0; x < rasterWidth; x++)
                {
                    var wx = (x + 0.5f) * cellMeters;

                    var best = 0;
                    var bestSq = float.PositiveInfinity;
                    for (var i = 0; i < sites.Length; i++)
                    {
                        var dx = sites[i].x - wx;
                        var dz = sites[i].y - wz;
                        var sq = dx * dx + dz * dz;
                        if (sq < bestSq)
                        {
                            bestSq = sq;
                            best = i;
                        }
                    }

                    cells[z * rasterWidth + x] = best;
                    count[best]++;
                    sumX[best] += wx;
                    sumZ[best] += wz;

                    var b = bounds[best];
                    b.x = Mathf.Min(b.x, x * cellMeters);
                    b.y = Mathf.Min(b.y, z * cellMeters);
                    b.z = Mathf.Max(b.z, (x + 1) * cellMeters);
                    b.w = Mathf.Max(b.w, (z + 1) * cellMeters);
                    bounds[best] = b;
                }
            }

            var centroid = new Vector2[sites.Length];
            for (var i = 0; i < sites.Length; i++)
            {
                centroid[i] = count[i] > 0
                    ? new Vector2((float)(sumX[i] / count[i]), (float)(sumZ[i] / count[i]))
                    : sites[i];
            }

            var neighbours = BuildAdjacency(cells, rasterWidth, rasterHeight, sites.Length);

            return new CrustalPartition(
                sites, cells, rasterWidth, rasterHeight, cellMeters,
                count, centroid, bounds, neighbours);
        }

        /// <summary>
        /// Blue-noise site placement by dart throwing with a minimum separation.
        /// </summary>
        /// <remarks>
        /// A uniform random dart-throw clumps and leaves holes, which is the failure
        /// WORLD_RELIEF_PRINCIPLES calls out for placement generally. The attempt budget is fixed
        /// rather than "until saturated", so the site count is a deterministic function of the seed
        /// instead of a function of how long the loop happened to run.
        /// </remarks>
        private static Vector2[] SampleSites(Seed.IWorldSeed worldSeed, TerrainGenSettings settings)
        {
            var rnd = worldSeed.Fork("terrain.crust.sites");

            var margin = settings.CrustMarginMeters;
            var minX = -margin;
            var minZ = -margin;
            var spanX = WorldScale.WorldExtentXMeters + 2f * margin;
            var spanZ = WorldScale.WorldExtentZMeters + 2f * margin;

            var spacing = settings.CrustSiteSpacingMeters;
            var spacingSq = spacing * spacing;

            // Bucket the accepted points so rejection is a 3x3 lookup rather than a scan.
            var bucketW = Mathf.Max(1, Mathf.CeilToInt(spanX / spacing));
            var bucketH = Mathf.Max(1, Mathf.CeilToInt(spanZ / spacing));
            var buckets = new List<int>[bucketW * bucketH];

            var accepted = new List<Vector2>(256);
            const int attempts = 40000;

            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var p = new Vector2(
                    minX + (float)rnd.NextDouble() * spanX,
                    minZ + (float)rnd.NextDouble() * spanZ);

                var bx = Mathf.Clamp((int)((p.x - minX) / spacing), 0, bucketW - 1);
                var bz = Mathf.Clamp((int)((p.y - minZ) / spacing), 0, bucketH - 1);

                var ok = true;
                for (var dz = -1; dz <= 1 && ok; dz++)
                {
                    for (var dx = -1; dx <= 1 && ok; dx++)
                    {
                        var nx = bx + dx;
                        var nz = bz + dz;
                        if (nx < 0 || nz < 0 || nx >= bucketW || nz >= bucketH)
                        {
                            continue;
                        }

                        var list = buckets[nz * bucketW + nx];
                        if (list == null)
                        {
                            continue;
                        }

                        for (var k = 0; k < list.Count; k++)
                        {
                            if ((accepted[list[k]] - p).sqrMagnitude < spacingSq)
                            {
                                ok = false;
                                break;
                            }
                        }
                    }
                }

                if (!ok)
                {
                    continue;
                }

                var slot = bz * bucketW + bx;
                buckets[slot] ??= new List<int>(4);
                buckets[slot].Add(accepted.Count);
                accepted.Add(p);
            }

            return accepted.ToArray();
        }

        private static int[][] BuildAdjacency(int[] cells, int width, int height, int siteCount)
        {
            var sets = new HashSet<int>[siteCount];

            void Link(int a, int b)
            {
                if (a == b)
                {
                    return;
                }

                (sets[a] ??= new HashSet<int>()).Add(b);
                (sets[b] ??= new HashSet<int>()).Add(a);
            }

            for (var z = 0; z < height; z++)
            {
                for (var x = 0; x < width; x++)
                {
                    var here = cells[z * width + x];
                    if (x + 1 < width)
                    {
                        Link(here, cells[z * width + x + 1]);
                    }

                    if (z + 1 < height)
                    {
                        Link(here, cells[(z + 1) * width + x]);
                    }
                }
            }

            var result = new int[siteCount][];
            for (var i = 0; i < siteCount; i++)
            {
                if (sets[i] == null || sets[i].Count == 0)
                {
                    result[i] = System.Array.Empty<int>();
                    continue;
                }

                var list = new List<int>(sets[i]);
                // Sorted, never HashSet iteration order: adjacency drives the growth order, and
                // hash iteration order is not a determinism guarantee.
                list.Sort();
                result[i] = list.ToArray();
            }

            return result;
        }
    }
}
