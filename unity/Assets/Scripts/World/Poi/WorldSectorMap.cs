using GameDev.World.Landscape;
using GameDev.World.Terrain;
using UnityEngine;

namespace GameDev.World.Poi
{
    /// <summary>
    /// Equal-size 3×3 world sectors for TownApproach / secondary POI distribution.
    /// Sector indices: X 0..2 west→east, Z 0..2 south→north.
    /// </summary>
    public readonly struct WorldSectorMap
    {
        public const int GridSize = 3;
        public const float DefaultHabitableThreshold = 0.28f;

        /// <summary>Preferred TownApproach slots: four corners + center.</summary>
        public static readonly Vector2Int[] PreferredTownSlots =
        {
            new Vector2Int(0, 2), // NW
            new Vector2Int(2, 2), // NE
            new Vector2Int(1, 1), // Center (main hub)
            new Vector2Int(0, 0), // SW
            new Vector2Int(2, 0), // SE
        };

        public float OriginX { get; }
        public float OriginZ { get; }
        public float SizeX { get; }
        public float SizeZ { get; }
        public float SectorSizeX => SizeX / GridSize;
        public float SectorSizeZ => SizeZ / GridSize;

        public WorldSectorMap(float originX, float originZ, float sizeX, float sizeZ)
        {
            OriginX = originX;
            OriginZ = originZ;
            SizeX = Mathf.Max(sizeX, 1f);
            SizeZ = Mathf.Max(sizeZ, 1f);
        }

        public static WorldSectorMap FromCostField(LandscapeCostField field)
        {
            if (field == null)
            {
                return new WorldSectorMap(0f, 0f, WorldScale.OverworldTileMeters, WorldScale.OverworldTileMeters);
            }

            return new WorldSectorMap(
                field.OriginX,
                field.OriginZ,
                field.Width * field.CellSize,
                field.Depth * field.CellSize);
        }

        public void ClampSector(ref int sx, ref int sz)
        {
            sx = Mathf.Clamp(sx, 0, GridSize - 1);
            sz = Mathf.Clamp(sz, 0, GridSize - 1);
        }

        public void WorldToSector(float worldX, float worldZ, out int sx, out int sz)
        {
            sx = Mathf.Clamp(Mathf.FloorToInt((worldX - OriginX) / SectorSizeX), 0, GridSize - 1);
            sz = Mathf.Clamp(Mathf.FloorToInt((worldZ - OriginZ) / SectorSizeZ), 0, GridSize - 1);
        }

        public Vector2 SectorCenter(int sx, int sz)
        {
            ClampSector(ref sx, ref sz);
            return new Vector2(
                OriginX + (sx + 0.5f) * SectorSizeX,
                OriginZ + (sz + 0.5f) * SectorSizeZ);
        }

        public bool Contains(int sx, int sz, float worldX, float worldZ)
        {
            ClampSector(ref sx, ref sz);
            var minX = OriginX + sx * SectorSizeX;
            var minZ = OriginZ + sz * SectorSizeZ;
            return worldX >= minX && worldX < minX + SectorSizeX &&
                   worldZ >= minZ && worldZ < minZ + SectorSizeZ;
        }

        /// <summary>
        /// Fraction of sampled cells that are buildable (gentle slope, not lake, not mountain).
        /// </summary>
        public float HabitableRatio(int sx, int sz, LandscapeCostField analysis, float maxSlope01 = 0.45f)
        {
            if (analysis == null)
            {
                return 1f;
            }

            ClampSector(ref sx, ref sz);
            var minX = OriginX + sx * SectorSizeX;
            var minZ = OriginZ + sz * SectorSizeZ;
            var step = Mathf.Max(analysis.CellSize, 24f);
            var ok = 0;
            var total = 0;
            for (var z = minZ + step * 0.5f; z < minZ + SectorSizeZ; z += step)
            {
                for (var x = minX + step * 0.5f; x < minX + SectorSizeX; x += step)
                {
                    total++;
                    if (analysis.SampleDistToLake(x, z) > -12f)
                    {
                        continue;
                    }

                    if (analysis.SampleSlope01(x, z) > maxSlope01)
                    {
                        continue;
                    }

                    if (analysis.SampleBiome(x, z) == BiomeId.Mountain)
                    {
                        continue;
                    }

                    ok++;
                }
            }

            return total > 0 ? ok / (float)total : 0f;
        }

        public bool IsHabitable(int sx, int sz, LandscapeCostField analysis, float threshold = DefaultHabitableThreshold) =>
            HabitableRatio(sx, sz, analysis) >= threshold;

        /// <summary>
        /// Resolve preferred sector → nearest habitable neighbor → best unoccupied globally.
        /// </summary>
        public Vector2Int ResolveHabitableSector(
            int preferredSx,
            int preferredSz,
            LandscapeCostField analysis,
            bool[,] occupied,
            float threshold = DefaultHabitableThreshold)
        {
            ClampSector(ref preferredSx, ref preferredSz);
            if (IsFreeAndHabitable(preferredSx, preferredSz, occupied, analysis, threshold))
            {
                return new Vector2Int(preferredSx, preferredSz);
            }

            // Expanding Chebyshev rings around the preferred sector.
            for (var ring = 1; ring <= GridSize; ring++)
            {
                Vector2Int best = default;
                var bestDist = float.PositiveInfinity;
                var found = false;
                for (var sz = 0; sz < GridSize; sz++)
                {
                    for (var sx = 0; sx < GridSize; sx++)
                    {
                        var cheb = Mathf.Max(Mathf.Abs(sx - preferredSx), Mathf.Abs(sz - preferredSz));
                        if (cheb != ring)
                        {
                            continue;
                        }

                        if (!IsFreeAndHabitable(sx, sz, occupied, analysis, threshold))
                        {
                            continue;
                        }

                        var d = (sx - preferredSx) * (sx - preferredSx) + (sz - preferredSz) * (sz - preferredSz);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = new Vector2Int(sx, sz);
                            found = true;
                        }
                    }
                }

                if (found)
                {
                    return best;
                }
            }

            // Last resort: any free sector, even below threshold.
            for (var sz = 0; sz < GridSize; sz++)
            {
                for (var sx = 0; sx < GridSize; sx++)
                {
                    if (occupied != null && occupied[sx, sz])
                    {
                        continue;
                    }

                    return new Vector2Int(sx, sz);
                }
            }

            return new Vector2Int(preferredSx, preferredSz);
        }

        private bool IsFreeAndHabitable(
            int sx,
            int sz,
            bool[,] occupied,
            LandscapeCostField analysis,
            float threshold)
        {
            if (occupied != null && occupied[sx, sz])
            {
                return false;
            }

            return analysis == null || HabitableRatio(sx, sz, analysis) >= threshold;
        }
    }
}
