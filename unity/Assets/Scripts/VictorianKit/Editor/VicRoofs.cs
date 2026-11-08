using System;
using System.Collections.Generic;
using System.Linq;

namespace GameDev.VictorianKit.EditorTools
{
    /// <summary>What a facade puts in each 2 m bay and on each corner, by storey.</summary>
    public sealed class VicFacade
    {
        /// <summary>
        /// The bay's key on a storey: a wall key of <see cref="VicDress.Walls"/>, "oriel_rect" / "oriel_arch" (an oriel
        /// over this bay and the next), "skip" (the oriel's second bay), or "meters" (two plain 1 m walls).
        /// </summary>
        public Func<VicEdge, int, int, string> Key = (e, b, f) => "plain";

        /// <summary>Whether a corner carries quoins (rustication). Rich houses: every street corner.</summary>
        public Func<VicCorner, bool> Quoin = c => c.Convex;

        /// <summary>Whether a ground-floor door in this bay gets stone rails instead of iron.</summary>
        public Func<VicEdge, int, bool> StoneRail = (e, b) => false;

        /// <summary>The rich house's stucco on every wall module's brick slot.</summary>
        public bool Stucco;
    }

    /// <summary>Openings, oriel and dormers the mansard must leave room for or carry.</summary>
    public sealed class VicMansardOptions
    {
        /// <summary>Front-edge segments the oriel cap replaces: edge index → first 2 m segment (it covers three).</summary>
        public readonly List<(int edge, int segment)> OrielCaps = new List<(int, int)>();

        /// <summary>Flat tiles (cell index) that carry a Chimney_4.</summary>
        public readonly List<(int i, int j)> Chimneys = new List<(int, int)>();

        /// <summary>
        /// A lucarne centred <c>along</c> metres from the edge's start: 0.40 m up a mansard slope as the kit's proof house
        /// _pi sets it, or 0.05 m up an oriel cap (<c>onCap</c>) as its _court sets it. It must sit on one straight
        /// slope with the flat behind it, never beside a corner piece or on a 4 m wing.
        /// </summary>
        public readonly List<(int edge, double along, bool arch, bool onCap)> Lucarnes = new List<(int, double, bool, bool)>();

        /// <summary>A weather vane over this flat tile's centre, or none.</summary>
        public (int i, int j)? Vane;
    }

    /// <summary>Walls and roofs of a whole house from its plan, as the kit's rules in СБОРКА.md set them.</summary>
    public static class VicRoofs
    {
        /// <summary>Every storey's corners and bays round the outline (the kit's _walls and build_shell).</summary>
        public static void Shell(VicHouse h, VicPlan plan, int floors, VicFacade facade)
        {
            for (var floor = 0; floor < floors; floor++)
            {
                var z = VicKit.FloorZ(floor);
                foreach (var c in plan.Corners)
                {
                    var at = new V3(c.X, c.Y, z);
                    if (c.Convex)
                    {
                        var body = (-(c.In.Out.x + c.Out.Out.x), -(c.In.Out.y + c.Out.Out.y));
                        VicDress.DressCorner(h, at, VicPlan.QuarterRot(body.Item1, body.Item2), floor, floors, "out", facade.Quoin(c), facade.Stucco);
                    }
                    else
                    {
                        var (kind, rot) = Reentry(c);
                        VicDress.DressCorner(h, at, rot, floor, floors, kind, facade.Quoin(c), facade.Stucco);
                    }
                }

                foreach (var e in plan.Edges)
                {
                    var bays = (e.Length - 2) / 2;
                    for (var b = 0; b < bays; b++)
                    {
                        var at = new V3(e.X0 + (e.Dx * (1 + (2 * b))), e.Y0 + (e.Dy * (1 + (2 * b))), z);
                        var key = facade.Key(e, b, floor);
                        switch (key)
                        {
                            case "skip":
                                break;
                            case "oriel_rect":
                            case "oriel_arch":
                                VicDress.Oriel(h, at, e.Rot, floor, floors, key == "oriel_arch", facade.Stucco);
                                break;
                            case "meters":
                                VicDress.PlainMeter(h, at, e.Rot, floor, floors, facade.Stucco);
                                VicDress.PlainMeter(h, new V3(at.X + e.Dx, at.Y + e.Dy, z), e.Rot, floor, floors, facade.Stucco);
                                break;
                            default:
                                VicDress.DressWall(h, at, e.Rot, key, floor, floors, facade.StoneRail(e, b), facade.Stucco);
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The court corner's piece and turn. Reentry and its mirror Reentry_Y are one shape a quarter turn apart (their
        /// UVs differ); the empty quarter goes on the piece's local +X/+Y, and the (+X, −Y) quarter takes the mirror at
        /// 0°, as House U does on its left court corner.
        /// </summary>
        public static (string kind, double rot) Reentry(VicCorner c)
        {
            var (ex, ey) = c.Empty;
            if (ex > 0 && ey < 0)
            {
                return ("reentry_y", 0.0);
            }

            return ("reentry", VicPlan.QuarterRot(ex, ey));
        }

        /// <summary>
        /// One mansard over the whole outline from the top storey's plate (СБОРКА.md, Правила): every slope springs at
        /// 42° with a 2 m inset; street corners take MansardCorner, court corners Valley_D4, the ring between them
        /// Mansard_Deck, the inset plan Flat_Deck, and where a 4 m wing leaves no flat the two slopes meet in
        /// RidgeCap_2m. Parapet round the flat, eaves round the outline.
        /// </summary>
        public static void Mansard(VicHouse h, VicPlan plan, int floors, VicMansardOptions o)
        {
            var spring = VicKit.Spring(floors);
            var top = spring + VicKit.MansardRise;
            var gaps = new HashSet<(int, int)>();
            foreach (var (edge, segment) in o.OrielCaps)
            {
                for (var s = segment; s < segment + 3; s++)
                {
                    gaps.Add((edge, s));
                }

                var e = plan.Edges[edge];
                VicDress.OrielCap(h, new V3(e.X0 + (e.Dx * 2 * segment), e.Y0 + (e.Dy * 2 * segment), spring), e.Rot);
            }

            foreach (var c in plan.Corners)
            {
                var at = new V3(c.X, c.Y, spring);
                if (c.Convex)
                {
                    var body = (-(c.In.Out.x + c.Out.Out.x), -(c.In.Out.y + c.Out.Out.y));
                    h.Drop("SM_Vic_Roof_MansardCorner_Deck", at, VicPlan.QuarterRot(body.Item1, body.Item2));
                }
                else
                {
                    var (ex, ey) = c.Empty;
                    h.Drop("SM_Vic_Roof_Valley_D4_Deck", at, VicPlan.QuarterRot(-ex, -ey));
                }
            }

            foreach (var e in plan.Edges)
            {
                var count = e.Length / 2;
                for (var s = 0; s < count; s++)
                {
                    if ((s == 0 && e.StartConvex) || (s == count - 1 && e.EndConvex) || gaps.Contains((e.Index, s)))
                    {
                        continue;
                    }

                    h.Drop("SM_Vic_Roof_Mansard_Deck", new V3(e.X0 + (e.Dx * 2 * s), e.Y0 + (e.Dy * 2 * s), spring), e.Rot);
                }
            }

            var flat = new HashSet<(int, int)>();
            foreach (var (i, j) in plan.Cells.OrderBy(c => c.j).ThenBy(c => c.i))
            {
                var all = true;
                for (var di = -1; di <= 1 && all; di++)
                {
                    for (var dj = -1; dj <= 1 && all; dj++)
                    {
                        all &= plan.Inside(i + di, j + dj);
                    }
                }

                if (all)
                {
                    flat.Add((i, j));
                    h.Drop("SM_Vic_Roof_Flat_Deck", new V3(i * 2, j * 2, spring), 0.0);
                }
            }

            Ridges(h, plan, flat, top);
            Parapet(h, flat, top);
            Eaves(h, plan, spring, VicKit.Tan, gaps);

            foreach (var (i, j) in o.Chimneys)
            {
                if (!flat.Contains((i, j)))
                {
                    throw new InvalidOperationException($"A Chimney_4 stands only on a flat tile: ({i}, {j}) is not one.");
                }

                VicDress.FlatChimney(h, new V3((i * 2) + 1.0, (j * 2) + 1.0, top));
            }

            if (o.Vane is (int vi, int vj))
            {
                if (!flat.Contains((vi, vj)))
                {
                    throw new InvalidOperationException("The weather vane stands on a flat tile.");
                }

                h.Drop("SM_Vic_WeatherVane", new V3((vi * 2) + 1.0, (vj * 2) + 1.0, top), 0.35);
            }

            foreach (var (edge, along, arch, onCap) in o.Lucarnes)
            {
                var e = plan.Edges[edge];
                var dist = onCap ? 0.05 : 0.40;
                var start = new V3(e.X0, e.Y0, spring);
                h.Drop(arch ? "SM_Vic_Lucarne_Arch" : "SM_Vic_Lucarne_Rect", VicKit.WorldPoint(start, e.Rot, along, dist, dist * VicKit.Tan), e.Rot);
            }

            var first = plan.Edges[0];
            VicDress.Downpipe(h, new V3(first.X0 + 0.45, first.Y0 - VicKit.Overhang, VicKit.EaveZ(spring, VicKit.Tan)), 0.0);
        }

        /// <summary>Ridge caps where two slopes meet at the mansard's full height along a whole cell side, with no flat on either side.</summary>
        private static void Ridges(VicHouse h, VicPlan plan, HashSet<(int, int)> flat, double top)
        {
            bool Full(double x, double y) => plan.Reach(x, y) >= VicKit.MansardInset - 1e-6;
            foreach (var (i, j) in plan.Cells.OrderBy(c => c.j).ThenBy(c => c.i))
            {
                if (flat.Contains((i, j)))
                {
                    continue;
                }

                // The cell's west side, to the cell on its west.
                if (plan.Inside(i - 1, j) && !flat.Contains((i - 1, j)))
                {
                    double x = i * 2, y = j * 2;
                    if (Full(x, y) && Full(x, y + 1) && Full(x, y + 2))
                    {
                        h.Drop("SM_Vic_Roof_RidgeCap_2m", new V3(x, y, top), VicDress.HalfPi);
                    }
                }

                // The cell's south side, to the cell on its south.
                if (plan.Inside(i, j - 1) && !flat.Contains((i, j - 1)))
                {
                    double x = i * 2, y = j * 2;
                    if (Full(x, y) && Full(x + 1, y) && Full(x + 2, y))
                    {
                        h.Drop("SM_Vic_Roof_RidgeCap_2m", new V3(x, y, top), 0.0);
                    }
                }
            }
        }

        /// <summary>
        /// The parapet round each flat region, walked with the flat on its left: a run's end at a convex corner gives
        /// 0.16 m to MansardEdge_Corner (Start, End or Both); at a concave turn two full bars meet (СБОРКА.md, Парапет).
        /// </summary>
        private static void Parapet(VicHouse h, HashSet<(int, int)> flat, double top)
        {
            if (flat.Count == 0)
            {
                return;
            }

            var next = new Dictionary<(int, int), (int, int)>();
            foreach (var (i, j) in flat)
            {
                int x = i * 2, y = j * 2;
                if (!flat.Contains((i, j - 1)))
                {
                    next.Add((x, y), (x + 2, y));
                }

                if (!flat.Contains((i + 1, j)))
                {
                    next.Add((x + 2, y), (x + 2, y + 2));
                }

                if (!flat.Contains((i, j + 1)))
                {
                    next.Add((x + 2, y + 2), (x, y + 2));
                }

                if (!flat.Contains((i - 1, j)))
                {
                    next.Add((x, y + 2), (x, y));
                }
            }

            var seen = new HashSet<(int, int)>();
            foreach (var start in next.Keys.OrderBy(p => p.Item2).ThenBy(p => p.Item1))
            {
                if (seen.Contains(start))
                {
                    continue;
                }

                var loop = new List<(int x, int y)>();
                var at = start;
                do
                {
                    loop.Add(at);
                    seen.Add(at);
                    at = next[at];
                }
                while (at != start);

                // Turning points of this loop, each convex (left turn) or concave.
                var turns = new List<(int x, int y, bool convex)>();
                for (var k = 0; k < loop.Count; k++)
                {
                    var a = loop[(k + loop.Count - 1) % loop.Count];
                    var b = loop[k];
                    var c = loop[(k + 1) % loop.Count];
                    int ax = b.x - a.x, ay = b.y - a.y, bx = c.x - b.x, by = c.y - b.y;
                    if (ax != bx || ay != by)
                    {
                        turns.Add((b.x, b.y, (ax * by) - (ay * bx) > 0));
                    }
                }

                for (var k = 0; k < turns.Count; k++)
                {
                    var a = turns[k];
                    var b = turns[(k + 1) % turns.Count];
                    int dx = Math.Sign(b.x - a.x), dy = Math.Sign(b.y - a.y);
                    var count = (Math.Abs(b.x - a.x) + Math.Abs(b.y - a.y)) / 2;
                    var rot = Math.Atan2(dy, dx);
                    for (var s = 0; s < count; s++)
                    {
                        var shortStart = s == 0 && a.convex;
                        var shortEnd = s == count - 1 && b.convex;
                        var kind = shortStart && shortEnd ? "Both" : shortStart ? "Start" : shortEnd ? "End" : "2m";
                        h.Drop("SM_Vic_Roof_MansardEdge_" + kind, new V3(a.x + (dx * 2 * s), a.y + (dy * 2 * s), top), rot);
                    }

                    if (a.convex)
                    {
                        // The flat lies in the quarter between the outgoing run and the reversed incoming one.
                        var p = turns[(k + turns.Count - 1) % turns.Count];
                        int ix = Math.Sign(a.x - p.x), iy = Math.Sign(a.y - p.y);
                        h.Drop("SM_Vic_Roof_MansardEdge_Corner", new V3(a.x, a.y, top), VicPlan.QuarterRot(dx - ix, dy - iy));
                    }
                }
            }
        }

        /// <summary>
        /// Fascia, gutter and soffit round the outline in 2 m runs from each edge's start, set 0.30 m out; corner pieces
        /// where the eave lines cross. A court's re-entrant eave takes no corner soffit (СБОРКА.md, Правила).
        /// </summary>
        public static void Eaves(VicHouse h, VicPlan plan, double spring, double tan, ISet<(int, int)> gaps = null)
        {
            var z = VicKit.EaveZ(spring, tan);
            var oh = VicKit.Overhang;
            var (straight, cornerSoffit) = VicDress.SoffitIds(tan);
            foreach (var e in plan.Edges)
            {
                var count = e.Length / 2;
                for (var s = 0; s < count; s++)
                {
                    if (gaps != null && gaps.Contains((e.Index, s)))
                    {
                        continue;
                    }

                    var at = new V3(e.X0 + (e.Dx * 2 * s) + (e.Out.x * oh), e.Y0 + (e.Dy * 2 * s) + (e.Out.y * oh), z);
                    VicDress.GutterRun(h, at, e.Rot, straight);
                }
            }

            foreach (var c in plan.Corners)
            {
                var at = new V3(c.X + ((c.In.Out.x + c.Out.Out.x) * oh), c.Y + ((c.In.Out.y + c.Out.Out.y) * oh), z);
                var a = (x: c.Out.Dx, y: c.Out.Dy);
                var b = (x: -c.In.Dx, y: -c.In.Dy);
                var axis = (a.x * b.y) - (a.y * b.x) > 0 ? a : b;
                var rot = Math.Atan2(axis.y, axis.x);
                h.Drop("SM_Vic_Roof_Eave_Corner", at, rot);
                h.Drop("SM_Vic_Rain_Gutter_Corner", at, rot);
                if (c.Convex)
                {
                    h.Drop(cornerSoffit, at, rot);
                }
            }
        }

        /// <summary>
        /// A hipped roof over a W × D rectangle at the origin, D 4 or 6 (D4 / D6): HipCorner at the four corners, the
        /// 42° slope between them, the ridge, hip caps, closed eaves, one Chimney_2 mid-front and the downpipe.
        /// </summary>
        public static void Hip(VicHouse h, int width, int depth, int floors, double chimneyX = double.NaN)
        {
            var key = Key(depth);
            var half = VicKit.HalfSpan(depth);
            var spring = VicKit.Spring(floors);
            var rise = half * VicKit.Tan;
            foreach (var (x, y, rot) in RectCorners(width, depth))
            {
                h.Drop($"SM_Vic_Roof_HipCorner_{key}_Deck", new V3(x, y, spring), rot);
                h.Drop($"SM_Vic_Roof_HipCap_{key}", new V3(x, y, spring), rot);
            }

            for (var x = half; x < width - half - 1e-6; x += 2.0)
            {
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(x, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(x + 2.0, depth, spring), Math.PI);
                h.Drop("SM_Vic_Roof_RidgeCap_2m", new V3(x, half, spring + rise), 0.0);
            }

            Eaves(h, new VicPlan().Rect(0, 0, width, depth).Trace(), spring, VicKit.Tan);
            var up = depth == 4 ? 0.55 : 1.0;
            VicDress.SlopeChimney(h, new V3(0.0, 0.0, 0.0), 0.0, double.IsNaN(chimneyX) ? width * 0.5 : chimneyX, up, spring);
            VicDress.Downpipe(h, new V3(0.45, -VicKit.Overhang, VicKit.EaveZ(spring, VicKit.Tan)), 0.0);
        }

        /// <summary>
        /// A gabled roof over a W × D rectangle at the origin (proof_houses.gable_roof): slopes and ridge in full 2 m
        /// pieces, gable walls and barge boards on both ends, eaves front and back with stops, one Chimney_2 0.85 m up
        /// the front slope at <paramref name="chimneyX"/>, and the downpipe.
        /// </summary>
        public static void Gable(VicHouse h, int width, int depth, int floors, double chimneyX)
        {
            var key = Key(depth);
            var half = VicKit.HalfSpan(depth);
            var spring = VicKit.Spring(floors);
            var rise = half * VicKit.Tan;
            for (var i = 0; i < width / 2; i++)
            {
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(2 * i, 0.0, spring), 0.0);
                h.Drop($"SM_Vic_Roof_Gable_{key}_Deck", new V3(2 * (i + 1), depth, spring), Math.PI);
                h.Drop("SM_Vic_Roof_RidgeCap_2m", new V3(2 * i, half, spring + rise), 0.0);
            }

            h.Drop($"SM_Vic_Roof_GableWall_{key}", new V3(0.0, 0.0, spring), 0.0);
            h.Drop($"SM_Vic_Roof_GableWall_{key}", new V3(width, depth, spring), Math.PI);
            h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(0.0, 0.0, spring), 0.0);
            h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(width, 0.0, spring), 0.0);
            h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(0.0, depth, spring), Math.PI);
            h.Drop($"SM_Vic_Roof_Barge_{key}", new V3(width, depth, spring), Math.PI);

            var z = VicKit.EaveZ(spring, VicKit.Tan);
            var oh = VicKit.Overhang;
            var (straight, _) = VicDress.SoffitIds(VicKit.Tan);
            for (var i = 0; i < width / 2; i++)
            {
                VicDress.GutterRun(h, new V3(2 * i, -oh, z), 0.0, straight);
                VicDress.GutterRun(h, new V3(2 * (i + 1), depth + oh, z), Math.PI, straight);
            }

            foreach (var (x, y, rot) in new[] { (0.0, -oh, 0.0), (width, -oh, 0.0), (0.0, depth + oh, Math.PI), (width, depth + oh, Math.PI) })
            {
                h.Drop("SM_Vic_Rain_Gutter_Stop", new V3(x, y, z), rot);
            }

            VicDress.SlopeChimney(h, new V3(0.0, 0.0, 0.0), 0.0, chimneyX, 0.85, spring);
            VicDress.Downpipe(h, new V3(0.45, -oh, z), 0.0);
        }

        private static string Key(int depth) => depth == 4 ? "D4" : depth == 6 ? "D6" : throw new ArgumentException("A pitched roof spans 4 or 6 m.");

        private static IEnumerable<(double x, double y, double rot)> RectCorners(int w, int d)
        {
            yield return (0.0, 0.0, 0.0);
            yield return (w, 0.0, VicDress.HalfPi);
            yield return (w, d, Math.PI);
            yield return (0.0, d, -VicDress.HalfPi);
        }
    }
}
