using System;
using System.Collections.Generic;
using GameDev.Grid;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// Deterministic organic road growth: market plaza → attractors → majors → ring → secondaries → alleys.
    /// </summary>
    public static class OrganicRoadBuilder
    {
        public static TownRoadGraph Build(Random rng)
        {
            var g = new TownRoadGraph(TownGrowthParams.MapWidth, TownGrowthParams.MapHeight);
            g.Center = TownGrowthParams.MapCenter;
            var radius = TownGrowthParams.TownRadiusMin +
                         rng.Next(0, TownGrowthParams.TownRadiusMax - TownGrowthParams.TownRadiusMin + 1);
            g.ApplyTownDisk(radius);

            PlaceAttractors(g, rng);
            CarvePlaza(g, rng);
            GrowMajor(g, rng, g.Center, g.NorthGate, TownGrowthParams.MajorWidthRidge);
            GrowMajor(g, rng, g.Center, g.EastFactory, TownGrowthParams.MajorWidthVeil);
            GrowMajor(g, rng, g.Center, g.SouthGate, TownGrowthParams.MajorWidthUndercroft);
            GrowMajor(g, rng, g.Center, g.WestFarm, TownGrowthParams.MajorWidthVeil);

            // Secondary attractors with jitter.
            var a1 = PointOnRing(g, rng, radius - 4, Mathf.PI * 0.15f + (float)(rng.NextDouble() * 0.2 - 0.1));
            var a2 = PointOnRing(g, rng, radius - 6, -Mathf.PI * 0.65f + (float)(rng.NextDouble() * 0.2 - 0.1));
            GrowMajor(g, rng, g.Center, a1, TownGrowthParams.SecondaryWidth);
            GrowMajor(g, rng, g.Center, a2, TownGrowthParams.SecondaryWidth);

            CarveBrokenRing(g, rng);
            GrowSecondaries(g, rng);
            GrowAlleysAndCourtyards(g, rng);
            WidenRandomJunctionPlazas(g, rng);
            CollectJunctions(g);

            return g;
        }

        private static void PlaceAttractors(TownRoadGraph g, Random rng)
        {
            var r = g.TownRadius - 2;
            g.NorthGate = PointOnRing(g, rng, r, Mathf.PI * 0.5f + Jitter(rng, 0.12f));
            g.EastFactory = PointOnRing(g, rng, r, 0f + Jitter(rng, 0.1f));
            g.SouthGate = PointOnRing(g, rng, r, -Mathf.PI * 0.5f + Jitter(rng, 0.12f));
            g.WestFarm = PointOnRing(g, rng, r, Mathf.PI + Jitter(rng, 0.1f));

            // Factory mass just inside / on the east rim.
            var fx = Mathf.Clamp(g.EastFactory.x - 2, 4, g.Width - 20);
            var fy = Mathf.Clamp(g.EastFactory.y - 6, 4, g.Height - 20);
            g.FactoryRectMin = new GridCoord(fx, fy);
            g.FactoryRectMax = new GridCoord(fx + 14, fy + 12);
        }

        private static GridCoord PointOnRing(TownRoadGraph g, Random rng, int radius, float angle)
        {
            var x = g.Center.x + Mathf.RoundToInt(Mathf.Cos(angle) * radius);
            var y = g.Center.y + Mathf.RoundToInt(Mathf.Sin(angle) * radius);
            x += rng.Next(-2, 3);
            y += rng.Next(-2, 3);
            x = Mathf.Clamp(x, 2, g.Width - 3);
            y = Mathf.Clamp(y, 2, g.Height - 3);
            return new GridCoord(x, y);
        }

        private static float Jitter(Random rng, float amount) =>
            (float)(rng.NextDouble() * 2.0 - 1.0) * amount;

        private static void CarvePlaza(TownRoadGraph g, Random rng)
        {
            var rx = TownGrowthParams.PlazaRadiusMin +
                     rng.Next(0, TownGrowthParams.PlazaRadiusMax - TownGrowthParams.PlazaRadiusMin + 1);
            var ry = Mathf.Max(4, rx - 1 + rng.Next(0, 3));
            for (var y = g.Center.y - ry - 2; y <= g.Center.y + ry + 2; y++)
            {
                for (var x = g.Center.x - rx - 2; x <= g.Center.x + rx + 2; x++)
                {
                    if (!g.InBounds(x, y))
                    {
                        continue;
                    }

                    var nx = (x - g.Center.x + (rng.NextDouble() * 0.6 - 0.3)) / rx;
                    var ny = (y - g.Center.y + (rng.NextDouble() * 0.6 - 0.3)) / ry;
                    if (nx * nx + ny * ny <= 1.05)
                    {
                        g.Plaza[x, y] = true;
                        g.Road[x, y] = true;
                    }
                }
            }
        }

        private static void GrowMajor(TownRoadGraph g, Random rng, GridCoord from, GridCoord to, int width)
        {
            var path = BuildOrganicPolyline(g, rng, from, to, wander: 0.55f, stepJitter: 1);
            foreach (var p in path)
            {
                g.MarkStreet(p, width, alley: false);
                g.MajorSamples.Add(p);
            }
        }

        private static List<GridCoord> BuildOrganicPolyline(
            TownRoadGraph g,
            Random rng,
            GridCoord from,
            GridCoord to,
            float wander,
            int stepJitter)
        {
            var points = new List<GridCoord>(128);
            // Midpoint displacement control points, then walk.
            var controls = new List<Vector2>
            {
                new Vector2(from.x, from.y),
                new Vector2(to.x, to.y)
            };

            for (var pass = 0; pass < 4; pass++)
            {
                var next = new List<Vector2>(controls.Count * 2);
                var amp = (6f - pass) * wander;
                for (var i = 0; i < controls.Count - 1; i++)
                {
                    var a = controls[i];
                    var b = controls[i + 1];
                    next.Add(a);
                    var mid = (a + b) * 0.5f;
                    var dir = (b - a).normalized;
                    var perp = new Vector2(-dir.y, dir.x);
                    mid += perp * ((float)rng.NextDouble() * 2f - 1f) * amp;
                    next.Add(mid);
                }

                next.Add(controls[controls.Count - 1]);
                controls = next;
            }

            GridCoord prev = from;
            points.Add(from);
            for (var i = 1; i < controls.Count; i++)
            {
                var target = new GridCoord(
                    Mathf.RoundToInt(controls[i].x),
                    Mathf.RoundToInt(controls[i].y));
                WalkBresenhamJitter(g, rng, prev, target, stepJitter, points);
                prev = points[points.Count - 1];
            }

            return points;
        }

        private static void WalkBresenhamJitter(
            TownRoadGraph g,
            Random rng,
            GridCoord from,
            GridCoord to,
            int jitter,
            List<GridCoord> into)
        {
            var x = from.x;
            var y = from.y;
            var dx = Mathf.Abs(to.x - from.x);
            var dy = Mathf.Abs(to.y - from.y);
            var sx = from.x < to.x ? 1 : -1;
            var sy = from.y < to.y ? 1 : -1;
            var err = dx - dy;
            var guard = 0;

            while (guard++ < 4000)
            {
                if (jitter > 0 && rng.Next(0, 5) == 0)
                {
                    x = Mathf.Clamp(x + rng.Next(-jitter, jitter + 1), 1, g.Width - 2);
                    y = Mathf.Clamp(y + rng.Next(-jitter, jitter + 1), 1, g.Height - 2);
                }

                var c = new GridCoord(x, y);
                if (into.Count == 0 || !into[into.Count - 1].Equals(c))
                {
                    into.Add(c);
                }

                if (x == to.x && y == to.y)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private static void CarveBrokenRing(TownRoadGraph g, Random rng)
        {
            var ringR = g.TownRadius * TownGrowthParams.RingRadiusFactorNum /
                        TownGrowthParams.RingRadiusFactorDen;
            var gaps = 2 + rng.Next(0, 3);
            var gapStart = new float[gaps];
            var gapWidth = new float[gaps];
            for (var i = 0; i < gaps; i++)
            {
                gapStart[i] = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                gapWidth[i] = 0.35f + (float)rng.NextDouble() * 0.45f;
            }

            var steps = Mathf.Max(48, ringR * 6);
            GridCoord? prev = null;
            for (var s = 0; s <= steps; s++)
            {
                var ang = s / (float)steps * Mathf.PI * 2f;
                var inGap = false;
                for (var i = 0; i < gaps; i++)
                {
                    var d = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, gapStart[i] * Mathf.Rad2Deg)) *
                            Mathf.Deg2Rad;
                    if (d < gapWidth[i] * 0.5f)
                    {
                        inGap = true;
                        break;
                    }
                }

                if (inGap)
                {
                    prev = null;
                    continue;
                }

                var rr = ringR + rng.Next(-2, 3);
                var x = g.Center.x + Mathf.RoundToInt(Mathf.Cos(ang) * rr);
                var y = g.Center.y + Mathf.RoundToInt(Mathf.Sin(ang) * rr);
                if (!g.InBounds(x, y) || !g.InTown[x, y])
                {
                    prev = null;
                    continue;
                }

                var c = new GridCoord(x, y);
                g.MarkStreet(c, TownGrowthParams.SecondaryWidth, alley: false);
                if (prev.HasValue)
                {
                    WalkBresenhamJitter(g, rng, prev.Value, c, 0, new List<GridCoord>());
                    // ensure connection cells
                    ConnectShort(g, prev.Value, c, TownGrowthParams.SecondaryWidth, alley: false);
                }

                prev = c;
            }
        }

        private static void ConnectShort(TownRoadGraph g, GridCoord a, GridCoord b, int width, bool alley)
        {
            var x0 = a.x;
            var y0 = a.y;
            var x1 = b.x;
            var y1 = b.y;
            var dx = Mathf.Abs(x1 - x0);
            var dy = Mathf.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;
            for (var n = 0; n < 64; n++)
            {
                g.MarkStreet(x0, y0, width, alley);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        private static void GrowSecondaries(TownRoadGraph g, Random rng)
        {
            var count = TownGrowthParams.SecondaryCountMin +
                        rng.Next(0, TownGrowthParams.SecondaryCountMax - TownGrowthParams.SecondaryCountMin + 1);
            for (var i = 0; i < count; i++)
            {
                if (g.MajorSamples.Count < 2)
                {
                    break;
                }

                var start = g.MajorSamples[rng.Next(0, g.MajorSamples.Count)];
                var angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                var len = 12 + rng.Next(0, 18);
                var x = start.x;
                var y = start.y;
                for (var s = 0; s < len; s++)
                {
                    angle += Jitter(rng, 0.35f);
                    x += Mathf.RoundToInt(Mathf.Cos(angle));
                    y += Mathf.RoundToInt(Mathf.Sin(angle));
                    if (!g.InBounds(x, y) || !g.InTown[x, y])
                    {
                        break;
                    }

                    // Prefer attach: if we hit existing street after a few steps, stop (T-junction).
                    if (s > 3 && g.IsStreet(x, y))
                    {
                        g.MarkStreet(x, y, TownGrowthParams.SecondaryWidth, alley: false);
                        break;
                    }

                    g.MarkStreet(x, y, TownGrowthParams.SecondaryWidth, alley: false);
                }
            }
        }

        private static void GrowAlleysAndCourtyards(TownRoadGraph g, Random rng)
        {
            var poorAlleys = TownGrowthParams.AlleyCountPoorMin +
                             rng.Next(0,
                                 TownGrowthParams.AlleyCountPoorMax - TownGrowthParams.AlleyCountPoorMin + 1);
            var richAlleys = TownGrowthParams.AlleyCountRichMin +
                             rng.Next(0,
                                 TownGrowthParams.AlleyCountRichMax - TownGrowthParams.AlleyCountRichMin + 1);

            for (var i = 0; i < poorAlleys; i++)
            {
                GrowOneAlley(g, rng, preferPoor: true, allowDeadEnd: true);
            }

            for (var i = 0; i < richAlleys; i++)
            {
                GrowOneAlley(g, rng, preferPoor: false, allowDeadEnd: false);
            }

            var courtyards = TownGrowthParams.CourtyardCountMin +
                             rng.Next(0,
                                 TownGrowthParams.CourtyardCountMax - TownGrowthParams.CourtyardCountMin + 1);
            for (var i = 0; i < courtyards; i++)
            {
                TryCourtyard(g, rng);
            }
        }

        private static void GrowOneAlley(TownRoadGraph g, Random rng, bool preferPoor, bool allowDeadEnd)
        {
            GridCoord? start = null;
            for (var attempt = 0; attempt < 40; attempt++)
            {
                var x = rng.Next(4, g.Width - 4);
                var y = rng.Next(4, g.Height - 4);
                if (!g.InTown[x, y] || !g.IsStreet(x, y))
                {
                    continue;
                }

                var c = new GridCoord(x, y);
                var poor = g.WealthOf(c) == TownWealth.Poor || g.DistrictOf(c) == CityDistrict.Undercroft;
                if (preferPoor != poor && attempt < 25)
                {
                    continue;
                }

                start = c;
                break;
            }

            if (!start.HasValue)
            {
                return;
            }

            var angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            var len = preferPoor ? 8 + rng.Next(0, 14) : 6 + rng.Next(0, 8);
            var x0 = start.Value.x;
            var y0 = start.Value.y;
            for (var s = 0; s < len; s++)
            {
                angle += Jitter(rng, 0.55f);
                x0 += Mathf.RoundToInt(Mathf.Cos(angle));
                y0 += Mathf.RoundToInt(Mathf.Sin(angle));
                if (!g.InBounds(x0, y0) || !g.InTown[x0, y0])
                {
                    break;
                }

                if (s > 2 && g.IsStreet(x0, y0) && !allowDeadEnd)
                {
                    g.MarkStreet(x0, y0, TownGrowthParams.AlleyWidth, alley: true);
                    break;
                }

                // Dead end: stop without connecting.
                if (allowDeadEnd && s > len - 3 && rng.Next(0, 3) == 0)
                {
                    g.MarkStreet(x0, y0, TownGrowthParams.AlleyWidth, alley: true);
                    break;
                }

                g.MarkStreet(x0, y0, TownGrowthParams.AlleyWidth, alley: true);
            }
        }

        private static void TryCourtyard(TownRoadGraph g, Random rng)
        {
            var x = rng.Next(8, g.Width - 10);
            var y = rng.Next(8, g.Height - 10);
            if (!g.InTown[x, y] || g.IsStreet(x, y))
            {
                return;
            }

            if (g.DistrictOf(new GridCoord(x, y)) == CityDistrict.Ridge && rng.Next(0, 3) != 0)
            {
                return; // fewer courtyards in rich
            }

            var w = 2 + rng.Next(0, 3);
            var h = 2 + rng.Next(0, 3);
            // Entry from nearest street.
            var entry = FindNearestStreet(g, x, y, 12);
            if (!entry.HasValue)
            {
                return;
            }

            ConnectShort(g, entry.Value, new GridCoord(x, y), TownGrowthParams.AlleyWidth, alley: true);
            for (var yy = y; yy < y + h; yy++)
            {
                for (var xx = x; xx < x + w; xx++)
                {
                    if (g.InBounds(xx, yy) && g.InTown[xx, yy])
                    {
                        g.Alley[xx, yy] = true;
                        g.Road[xx, yy] = true; // walkable courtyard
                    }
                }
            }
        }

        private static GridCoord? FindNearestStreet(TownRoadGraph g, int x, int y, int maxR)
        {
            for (var r = 1; r <= maxR; r++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    for (var dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r)
                        {
                            continue;
                        }

                        var cx = x + dx;
                        var cy = y + dy;
                        if (g.IsStreet(cx, cy))
                        {
                            return new GridCoord(cx, cy);
                        }
                    }
                }
            }

            return null;
        }

        private static void WidenRandomJunctionPlazas(TownRoadGraph g, Random rng)
        {
            for (var i = 0; i < 8; i++)
            {
                if (g.MajorSamples.Count == 0)
                {
                    break;
                }

                var c = g.MajorSamples[rng.Next(0, g.MajorSamples.Count)];
                if (CountStreetNeighbors(g, c) < 3)
                {
                    continue;
                }

                for (var dy = -1; dy <= 1; dy++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var x = c.x + dx;
                        var y = c.y + dy;
                        if (g.InBounds(x, y) && g.InTown[x, y])
                        {
                            g.Road[x, y] = true;
                            if (dx == 0 && dy == 0)
                            {
                                g.Plaza[x, y] = true;
                            }
                        }
                    }
                }
            }
        }

        private static int CountStreetNeighbors(TownRoadGraph g, GridCoord c)
        {
            var n = 0;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    if (g.IsStreet(c.x + dx, c.y + dy))
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        private static void CollectJunctions(TownRoadGraph g)
        {
            for (var y = 2; y < g.Height - 2; y++)
            {
                for (var x = 2; x < g.Width - 2; x++)
                {
                    if (!g.IsStreet(x, y))
                    {
                        continue;
                    }

                    if (CountStreetNeighbors(g, new GridCoord(x, y)) >= 3)
                    {
                        g.Junctions.Add(new GridCoord(x, y));
                    }
                }
            }
        }

        /// <summary>
        /// Carve a corrective 1-cell path so A* can reach the market from an anchor.
        /// </summary>
        public static void EnsureConnected(TownRoadGraph g, GridCoord from, GridCoord to)
        {
            ConnectShort(g, from, to, TownGrowthParams.AlleyWidth, alley: true);
        }
    }
}
