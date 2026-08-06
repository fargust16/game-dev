using System.Collections.Generic;
using GameDev.Grid;
using UnityEngine;

namespace GameDev.World.Town
{
    // CityDistrict lives in GameDev.World
    /// <summary>
    /// Raster road layers + helpers for district / wealth classification from town center.
    /// </summary>
    public sealed class TownRoadGraph
    {
        public readonly int Width;
        public readonly int Height;
        public readonly bool[,] Road;
        public readonly bool[,] Alley;
        public readonly bool[,] Plaza;
        public readonly bool[,] InTown;

        public GridCoord Center;
        public int TownRadius;
        public GridCoord NorthGate;
        public GridCoord EastFactory;
        public GridCoord SouthGate;
        public GridCoord WestFarm;
        public GridCoord FactoryRectMin;
        public GridCoord FactoryRectMax;

        public readonly List<GridCoord> MajorSamples = new List<GridCoord>(256);
        public readonly List<GridCoord> Junctions = new List<GridCoord>(128);

        public TownRoadGraph(int width, int height)
        {
            Width = width;
            Height = height;
            Road = new bool[width, height];
            Alley = new bool[width, height];
            Plaza = new bool[width, height];
            InTown = new bool[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public bool InBounds(GridCoord c) => InBounds(c.x, c.y);

        public bool IsStreet(int x, int y) => InBounds(x, y) && (Road[x, y] || Alley[x, y] || Plaza[x, y]);

        public bool IsStreet(GridCoord c) => IsStreet(c.x, c.y);

        public void MarkStreet(int x, int y, int width, bool alley)
        {
            var half = Mathf.Max(0, (width - 1) / 2);
            for (var dy = -half; dy <= half; dy++)
            {
                for (var dx = -half; dx <= half; dx++)
                {
                    var cx = x + dx;
                    var cy = y + dy;
                    if (!InBounds(cx, cy))
                    {
                        continue;
                    }

                    if (alley && width <= 1)
                    {
                        Alley[cx, cy] = true;
                    }
                    else
                    {
                        Road[cx, cy] = true;
                    }
                }
            }
        }

        public void MarkStreet(GridCoord c, int width, bool alley) => MarkStreet(c.x, c.y, width, alley);

        public CityDistrict DistrictOf(GridCoord c)
        {
            var dx = c.x - Center.x;
            var dy = c.y - Center.y;
            var angle = Mathf.Atan2(dy, dx); // -PI..PI, 0 = east
            // North (~PI/2) Ridge, South (~-PI/2) Undercroft, else Lower Veil (esp. east factory).
            if (angle > Mathf.PI * 0.28f && angle < Mathf.PI * 0.95f)
            {
                return CityDistrict.Ridge;
            }

            if (angle < -Mathf.PI * 0.22f && angle > -Mathf.PI * 0.95f)
            {
                return CityDistrict.Undercroft;
            }

            return CityDistrict.LowerVeil;
        }

        public TownWealth WealthOf(GridCoord c)
        {
            var dist = Vector2.Distance(new Vector2(c.x, c.y), new Vector2(Center.x, Center.y));
            var district = DistrictOf(c);
            if (district == CityDistrict.Ridge)
            {
                return dist < TownRadius * 0.45f ? TownWealth.Mixed : TownWealth.Rich;
            }

            if (district == CityDistrict.Undercroft)
            {
                return TownWealth.Poor;
            }

            // Lower Veil: denser/poorer toward factory and south edge of veil.
            if (dist > TownRadius * 0.55f && c.x > Center.x)
            {
                return TownWealth.Poor;
            }

            return TownWealth.Mixed;
        }

        public int HeightOf(GridCoord c) => DistrictOf(c) == CityDistrict.Ridge ? 1 : 0;

        public void ApplyTownDisk(int radius)
        {
            TownRadius = radius;
            var r2 = radius * radius;
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var dx = x - Center.x;
                    var dy = y - Center.y;
                    // Soft blob: slight ellipse + noise-free radius stretch by angle.
                    var stretch = 1f + 0.08f * Mathf.Sin(Mathf.Atan2(dy, dx) * 3f);
                    var rr = (dx * dx + dy * dy) / (stretch * stretch);
                    InTown[x, y] = rr <= r2;
                }
            }
        }
    }
}
