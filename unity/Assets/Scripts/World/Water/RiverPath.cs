using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Water
{
    /// <summary>How a traced river run ends - kept on the path mainly for diagnostics/logging, not
    /// consumed by gameplay code.</summary>
    public enum RiverTermination
    {
        /// <summary>Ran into a <see cref="WaterZone"/> - the normal case, a river reaching its
        /// lake.</summary>
        Lake,

        /// <summary>Ran into another, already-traced river's course - a tributary reaching its
        /// confluence with the main stem.</summary>
        Confluence,

        /// <summary>Flowed downhill into a sink at the sampled grid's own border - read as leaving
        /// the tile toward unmodelled terrain (V0 is a single stub tile, no ocean/downstream
        /// system yet), not a real basin.</summary>
        OffTile,

        /// <summary>Ran into an interior local minimum too shallow/small to have become a
        /// <see cref="WaterZone"/> (see <see cref="LakeBasinFinder"/>'s <c>minCellsPerZone</c>) - a
        /// small pond or a plausible dead end, not an error.</summary>
        Sink,
    }

    /// <summary>
    /// One traced river run: a centerline (real, pre-carve ground height at every point) plus a
    /// per-point width/carve-depth/water-surface profile derived from <see cref="FlowGrid"/>
    /// accumulation - a trickle at the source, widening toward the mouth. Also rasterizes itself
    /// onto the same grid convention <see cref="WaterZone"/> uses (world-space origin + cell size,
    /// O(1) array lookup) so point queries (<see cref="Contains"/>, <see cref="TryGetWaterLevel"/>)
    /// don't have to walk the polyline - callers like road/POI placement can query this exactly like
    /// a <see cref="WaterZone"/>.
    /// </summary>
    public sealed class RiverPath
    {
        public string Id { get; }
        public RiverTermination Termination { get; }

        /// <summary>World-space centerline point, Y = real pre-carve ground height at that point
        /// (what <see cref="RiverTerrainCarver"/> carves down from).</summary>
        public IReadOnlyList<Vector3> Centerline { get; }
        public IReadOnlyList<float> Widths { get; }
        public IReadOnlyList<float> CarveDepths { get; }
        public IReadOnlyList<float> WaterSurfaceY => waterSurfaceY;

        public float OriginX { get; }
        public float OriginZ { get; }
        public float CellSize { get; }
        public int GridWidth { get; }
        public int GridDepth { get; }

        private readonly float[] waterSurfaceY;
        private readonly bool[,] wet;
        private readonly float[,] waterYGrid;

        public RiverPath(
            string id, RiverTermination termination,
            IReadOnlyList<Vector3> centerline, IReadOnlyList<float> widths,
            IReadOnlyList<float> carveDepths, IReadOnlyList<float> waterSurfaceY,
            float originX, float originZ, float cellSize, int gridWidth, int gridDepth)
        {
            Id = id;
            Termination = termination;
            Centerline = centerline;
            Widths = widths;
            CarveDepths = carveDepths;
            this.waterSurfaceY = new float[waterSurfaceY.Count];
            for (var i = 0; i < waterSurfaceY.Count; i++)
            {
                this.waterSurfaceY[i] = waterSurfaceY[i];
            }

            OriginX = originX;
            OriginZ = originZ;
            CellSize = cellSize;
            GridWidth = gridWidth;
            GridDepth = gridDepth;

            wet = new bool[gridWidth, gridDepth];
            waterYGrid = new float[gridWidth, gridDepth];
            Rasterize();
        }

        /// <summary>
        /// After lake + river carves: sit the ribbon in the river trench. If the sample point falls
        /// into a deep lake basin (span ≫ river carve), snap to that lake's water plane instead of
        /// floating halfway up a 100m crater wall.
        /// </summary>
        public void SyncWaterSurfaceAfterCarve(
            System.Func<float, float, float> sampleTerrainHeightY,
            float waterFillFraction,
            IReadOnlyList<WaterZone> lakes = null)
        {
            var fill = Mathf.Clamp01(waterFillFraction);
            for (var i = 0; i < Centerline.Count; i++)
            {
                var point = Centerline[i];
                var bedY = sampleTerrainHeightY(point.x, point.z);
                var bankY = point.y;
                if (bankY < bedY + 0.05f)
                {
                    bankY = bedY + Mathf.Max(CarveDepths[i], 0.5f);
                }

                var lakeY = FindLakeWaterY(lakes, point.x, point.z);
                float surface;
                if (!float.IsNaN(lakeY))
                {
                    surface = lakeY;
                }
                else
                {
                    var span = bankY - bedY;
                    var maxRiverSpan = Mathf.Max(CarveDepths[i] * 2f, 3f);
                    if (span > maxRiverSpan)
                    {
                        // Deep foreign carve under the path (usually lake) but outside flood mask —
                        // keep a shallow ribbon above the local bed, not mid-air.
                        surface = bedY + Mathf.Max(CarveDepths[i] * fill, 0.35f);
                    }
                    else if (span < 0.2f)
                    {
                        surface = bankY - Mathf.Max(CarveDepths[i] * (1f - fill), 0.2f);
                        if (surface < bedY + 0.05f)
                        {
                            surface = bedY + 0.05f;
                        }
                    }
                    else
                    {
                        surface = bedY + span * fill;
                        surface = Mathf.Clamp(surface, bedY + 0.1f, bankY - 0.05f);
                    }
                }

                waterSurfaceY[i] = surface;
            }

            // Guarantee the mouth meets the lake plane when we terminated in a lake.
            if (Termination == RiverTermination.Lake && lakes != null && Centerline.Count > 0)
            {
                var end = Centerline[Centerline.Count - 1];
                var mouthLakeY = FindLakeWaterY(lakes, end.x, end.z);
                if (float.IsNaN(mouthLakeY))
                {
                    mouthLakeY = FindNearestLakeWaterY(lakes, end.x, end.z, maxDistMeters: 48f);
                }

                if (!float.IsNaN(mouthLakeY))
                {
                    var tail = Mathf.Min(6, Centerline.Count);
                    for (var t = 0; t < tail; t++)
                    {
                        var idx = Centerline.Count - 1 - t;
                        var u = tail == 1 ? 1f : 1f - t / (float)(tail - 1);
                        waterSurfaceY[idx] = Mathf.Lerp(waterSurfaceY[idx], mouthLakeY, u);
                    }
                }
            }

            Rasterize();
        }

        private static float FindLakeWaterY(IReadOnlyList<WaterZone> lakes, float x, float z)
        {
            if (lakes == null)
            {
                return float.NaN;
            }

            for (var i = 0; i < lakes.Count; i++)
            {
                if (lakes[i].Contains(x, z))
                {
                    return lakes[i].WaterLevelY;
                }
            }

            return float.NaN;
        }

        private static float FindNearestLakeWaterY(
            IReadOnlyList<WaterZone> lakes, float x, float z, float maxDistMeters)
        {
            if (lakes == null || lakes.Count == 0)
            {
                return float.NaN;
            }

            var bestY = float.NaN;
            var bestDistSq = maxDistMeters * maxDistMeters;
            for (var i = 0; i < lakes.Count; i++)
            {
                var aabb = lakes[i].GetFloodAabbXZ();
                var cx = Mathf.Clamp(x, aabb.xMin, aabb.xMax);
                var cz = Mathf.Clamp(z, aabb.yMin, aabb.yMax);
                var dx = cx - x;
                var dz = cz - z;
                var distSq = dx * dx + dz * dz;
                if (distSq <= bestDistSq)
                {
                    bestDistSq = distSq;
                    bestY = lakes[i].WaterLevelY;
                }
            }

            return bestY;
        }

        private void Rasterize()
        {
            for (var x = 0; x < GridWidth; x++)
            {
                for (var z = 0; z < GridDepth; z++)
                {
                    wet[x, z] = false;
                    waterYGrid[x, z] = 0f;
                }
            }

            // "Closest centerline point wins" per touched cell, not stamp order - two points along
            // a winding channel can both reach the same cell, and only the nearer one's width/Y are
            // actually correct there.
            var bestDistSq = new float[GridWidth, GridDepth];
            for (var x = 0; x < GridWidth; x++)
            {
                for (var z = 0; z < GridDepth; z++)
                {
                    bestDistSq[x, z] = float.PositiveInfinity;
                }
            }

            for (var i = 0; i < Centerline.Count; i++)
            {
                var point = Centerline[i];
                var halfWidth = Widths[i] * 0.5f;
                var surfaceY = waterSurfaceY[i];

                var centerPx = Mathf.FloorToInt((point.x - OriginX) / CellSize);
                var centerPz = Mathf.FloorToInt((point.z - OriginZ) / CellSize);
                var radius = Mathf.CeilToInt(halfWidth / CellSize) + 1;

                for (var dz = -radius; dz <= radius; dz++)
                {
                    var pz = centerPz + dz;
                    if (pz < 0 || pz >= GridDepth)
                    {
                        continue;
                    }

                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var px = centerPx + dx;
                        if (px < 0 || px >= GridWidth)
                        {
                            continue;
                        }

                        var cellWorldX = OriginX + (px + 0.5f) * CellSize;
                        var cellWorldZ = OriginZ + (pz + 0.5f) * CellSize;
                        var ddx = cellWorldX - point.x;
                        var ddz = cellWorldZ - point.z;
                        var distSq = ddx * ddx + ddz * ddz;
                        if (distSq > halfWidth * halfWidth)
                        {
                            continue;
                        }

                        if (distSq >= bestDistSq[px, pz])
                        {
                            continue;
                        }

                        bestDistSq[px, pz] = distSq;
                        wet[px, pz] = true;
                        waterYGrid[px, pz] = surfaceY;
                    }
                }
            }
        }

        public bool Contains(float worldX, float worldZ)
        {
            var x = Mathf.FloorToInt((worldX - OriginX) / CellSize);
            var z = Mathf.FloorToInt((worldZ - OriginZ) / CellSize);
            return x >= 0 && z >= 0 && x < GridWidth && z < GridDepth && wet[x, z];
        }

        public bool TryGetWaterLevel(float worldX, float worldZ, out float y)
        {
            var x = Mathf.FloorToInt((worldX - OriginX) / CellSize);
            var z = Mathf.FloorToInt((worldZ - OriginZ) / CellSize);
            if (x >= 0 && z >= 0 && x < GridWidth && z < GridDepth && wet[x, z])
            {
                y = waterYGrid[x, z];
                return true;
            }

            y = 0f;
            return false;
        }
    }
}
