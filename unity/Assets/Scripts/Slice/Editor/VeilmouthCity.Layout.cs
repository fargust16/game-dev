using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// The city of plan C4 from its layout (VEILMOUTH_CITY_PLAN.md §28.5, <see cref="VeilmouthLayout"/>): the streets and
    /// squares as lanes, the town wall with its gate and towers, the river's stone banks, the quay wall, the bridges and
    /// the piers, the Penny's ring and island, and the lamps and trees of every street. Every size here is an authoring
    /// default unless marked measured.
    /// </summary>
    public static partial class VeilmouthCity
    {
        private static VeilmouthLayout _layout;

        /// <summary>The decks over the water, the bridges and the piers, with their top, m: what stands on them stands there.</summary>
        private static readonly List<(Vector2[] Quad, float Top)> _decks = new List<(Vector2[], float)>();

        /// <summary>The houses sunk by <see cref="Sink"/>: their doors' own floor is <see cref="Sink"/> over their base.</summary>
        private static readonly HashSet<Transform> _sunk = new HashSet<Transform>();

        /// <summary>
        /// How far a house on a plain stoop is sunk below the highest ground under it, m (owner, 2026-11-10, §28.1 item 5):
        /// of the kit's four risers of 0.1875 m, the top one's last 0.15 m shows, and the plinth line drops with it.
        /// </summary>
        private const float Sink = 0.6f;

        /// <summary>A deck's top over the water: the streets' level, m.</summary>
        private const float DeckTop = Low + 0.1f;

        /// <summary>A deck's thickness and a parapet's, m.</summary>
        private const float DeckDepth = 1.2f, Parapet = 0.6f, ParapetHeight = 1.1f;

        private static Facing FacingOf(string f) => f switch { "N" => Facing.N, "E" => Facing.E, "S" => Facing.S, _ => Facing.W };

        private static int SurfaceLayer(string surface) => surface switch
        {
            "cobbles" => Cobbles, "grass" => Grass, "square" => Square, _ => Setts,
        };

        /// <summary>A disc as a convex polygon of 48 sides.</summary>
        private static Vector2[] Disc(Vector2 c, float radius) =>
            Enumerable.Range(0, 48).Select(k => c + (new Vector2(Mathf.Cos(k * Mathf.PI / 24f), Mathf.Sin(k * Mathf.PI / 24f)) * radius)).ToArray();

        /// <summary>The houses a plot's kind draws from.</summary>
        private static string[] PlotKeys(string kind) => kind switch
        {
            "villa" => VillaKeys,
            "villa_stucco" => new[] { VictorianHouses.Key("Villa", "stucco") },
            _ => TerraceKeys,
        };

        // ------------------------------------------------------------------------------------------- streets

        /// <summary>
        /// The streets, squares and gardens as lanes, in the order they are painted: a later lane covers an earlier one.
        /// The yards and courts inside the walls are cobbled; the gardens and parks are green; the Penny is paved with its
        /// ring of setts round a green island, flush (§28.1 item 3).
        /// </summary>
        private static void StreetsC4()
        {
            foreach (var area in new[] { _layout.City, _layout.Works })
            {
                _lanes.Add(new Lane { Street = "Yards", Quad = Rect(area.x0, area.y0, area.x1, area.y1), Layer = Cobbles, PaintOnly = true });
            }

            foreach (var p in _layout.plots)
            {
                var depth = PlotKeys(p.kind).Max(k => _kit[k].Depth);
                _lanes.Add(new Lane
                {
                    Street = "Gardens", Quad = PlotQuad(FacingOf(p.facing), p.front, (int)p.s0, (int)p.s1, depth, p.garden, p.behind), Layer = Grass,
                    PaintOnly = true,
                });
            }

            foreach (var s in _layout.squares.Where(s => s.x0 < _layout.Sea.x0 - 0.01f)) // the piers are decks over the sea
            {
                Lanes(string.IsNullOrEmpty(s.name) ? s.id : s.name, SurfaceLayer(s.surface), false, Rect(s.x0, s.y0, s.x1, s.y1), Use.Walk);
            }

            var plaza = _layout.plaza;
            var centre = new Vector2(plaza.cx, plaza.cy);
            _lanes.Add(new Lane { Street = "The Penny's ring", Quad = Disc(centre, plaza.ringOuter), Layer = Setts, PaintOnly = true });
            _lanes.Add(new Lane { Street = "The Penny's island", Quad = Disc(centre, plaza.island), Layer = Grass, PaintOnly = true });

            foreach (var s in _layout.streets)
            {
                Vector2[] Band(float c0, float c1) => s.AlongX ? Rect(s.a0, c0, s.a1, c1) : Rect(c0, s.a0, c1, s.a1);
                if (s.walk0 > 0f)
                {
                    Lanes(s.name, Flags, false, Band(s.c0, s.c0 + s.walk0), Use.Walk);
                }

                if (s.walk1 > 0f)
                {
                    Lanes(s.name, Flags, false, Band(s.c1 - s.walk1, s.c1), Use.Walk);
                }

                Lanes(s.name, SurfaceLayer(s.surface), s.walk0 > 0f || s.walk1 > 0f, Band(s.c0 + s.walk0, s.c1 - s.walk1), Use.Road);
            }

            // Squint Lane at 45° (§12): setts, no pavements, run 2 m into the squares at its ends.
            foreach (var sl in _layout.slants)
            {
                Lanes(sl.name, Setts, false, Along(new Vector2(sl.x0, sl.y0), new Vector2(sl.x1, sl.y1), sl.width / 2f, 2f), Use.Road);
            }

            // Each bridge's carriageway is carried through the pavement or the gate at its city end, so the kerb opens
            // onto it: Grubby Bridge through the embankment's promenade, Last Farthing Bridge through Creaking Gate.
            foreach (var b in _layout.bridges)
            {
                var street = Nearest(b);
                if (street == null)
                {
                    continue;
                }

                var half = (street.c1 - street.walk1 - street.c0 - street.walk0) / 2f;
                var mid = b.axis == "x" ? (b.y0 + b.y1) / 2f : (b.x0 + b.x1) / 2f;
                var quad = b.axis == "x"
                    ? Rect(b.x1 - 2f, mid - half, street.a0, mid + half)
                    : Rect(mid - half, street.c1 - street.walk1, mid + half, b.y0);
                if (quad.Max(q => q.x) - quad.Min(q => q.x) > 0.5f && quad.Max(q => q.y) - quad.Min(q => q.y) > 0.5f)
                {
                    Lanes(b.name, Setts, true, quad, Use.Road);
                }
            }

            // What is not ground to build on: the walls' lines and the river.
            foreach (var w in _layout.walls)
            {
                Take(w.axis == "y" ? Rect(w.t0, w.a0, w.t1, w.a1) : Rect(w.a0, w.t0, w.a1, w.t1), Use.Solid);
            }

            foreach (var r in _layout.Rivers)
            {
                Take(Rect(r.x0, r.y0, r.x1, r.y1), Use.Solid);
            }
        }

        /// <summary>The street a bridge leads into at its city end: the one whose carriageway runs on from its deck.</summary>
        private static VeilmouthLayout.Street Nearest(VeilmouthLayout.BridgeSpec b)
        {
            if (b.axis == "x")
            {
                var mid = (b.y0 + b.y1) / 2f;
                return _layout.streets.Where(s => s.AlongX && s.c0 < mid && s.c1 > mid && s.a0 >= b.x1 - 0.01f).OrderBy(s => s.a0 - b.x1).FirstOrDefault();
            }

            var x = (b.x0 + b.x1) / 2f;
            return _layout.streets.Where(s => s.AlongX && s.a0 < x && s.a1 > x && s.c1 <= b.y0 + 0.01f).OrderBy(s => b.y0 - s.c1).FirstOrDefault();
        }

        // ------------------------------------------------------------------------------------------- walls

        /// <summary>
        /// The town wall and the works' wall in segments that follow the ground, each run stopping at its towers' faces
        /// and at the gate, so no two faces lie in one plane (the owner saw C3's gate flicker, 2026-11-06); the gate's
        /// lintel between its towers; the kit's towers, five storeys of dark brick, 3 m proud of the wall (§28.5).
        /// </summary>
        private static void WallsC4()
        {
            var key = VictorianHouses.Key("Tower5", "dark");
            var tower = _kit[key];
            var towers = _layout.towers.Select(t => (Spec: t, Quad: Footprint(FacingOf(t.facing), t.front, t.a, tower.Width, tower.Depth))).ToList();
            foreach (var w in _layout.walls)
            {
                var alongY = w.axis == "y";
                var gaps = w.gaps.Select(g => ((int)g.a, (int)g.b)).ToList();
                foreach (var (_, quad) in towers)
                {
                    var across0 = alongY ? quad.Min(q => q.x) : quad.Min(q => q.y);
                    var across1 = alongY ? quad.Max(q => q.x) : quad.Max(q => q.y);
                    if (across1 > w.t0 && across0 < w.t1)
                    {
                        gaps.Add(alongY ? ((int)quad.Min(q => q.y), (int)quad.Max(q => q.y)) : ((int)quad.Min(q => q.x), (int)quad.Max(q => q.x)));
                    }
                }

                WallRun(alongY, w.t0, w.t1, (int)w.a0, (int)w.a1, w.height, gaps.ToArray());
                foreach (var g in w.gaps)
                {
                    Lintel(alongY, w.t0, w.t1, g.a, g.b, w.height);
                }
            }

            foreach (var (t, _) in towers)
            {
                House(key, FacingOf(t.facing), t.front, t.a, name: t.name, force: true);
            }
        }

        // ------------------------------------------------------------------------------------------- the waterside

        /// <summary>
        /// The river's stone banks along the town wall and the works, the embankment's parapet, the quay wall, the bridges
        /// and the piers: every edge where built ground meets the water stands upright in stone down to the bed, so no
        /// bare cliff of ground shows (§28.6).
        /// </summary>
        private static void Waterside()
        {
            var city = _layout.City;
            var works = _layout.Works;
            var west = _layout.Rivers.OrderBy(r => r.x0).ThenBy(r => r.x1 - r.x0).First();
            var south = _layout.Rivers.First(r => r != west);
            var sea = _layout.Sea.x0;
            var crossX = _layout.bridges.Where(b => b.axis == "x").Select(b => (b.y0, b.y1)).ToArray();
            var crossY = _layout.bridges.Where(b => b.axis == "y").Select(b => (b.x0, b.x1)).ToArray();

            // Along the river's west leg, between the water and the town wall: a low kerb over the bank.
            foreach (var (from, to) in new[] { (city.y0 - 2f, city.y1), (works.y0, works.y1 + 2f) })
            {
                foreach (var (py0, py1) in Spans(from, to, crossX))
                {
                    Slab(west.x1 - 0.2f, west.x1 + 2f, py0, py1, VeilmouthGround.RiverBed - 0.5f, Low + 0.3f);
                }
            }

            // Along the south leg: the promenade's parapet on the city's side, a low kerb on the works' side.
            foreach (var (px0, px1) in Spans(city.x0 - 2f, sea, crossY))
            {
                Slab(px0, px1, south.y0 - 0.1f, south.y0 + 1.2f, VeilmouthGround.RiverBed - 0.5f, Ground((px0 + px1) / 2f, south.y0 - 1f) + ParapetHeight);
                Slab(px0, px1, south.y1 - 1.2f, south.y1 + 0.1f, VeilmouthGround.RiverBed - 0.5f, Low + 0.3f);
            }

            // The quay wall along the sea, the city's and the works'; under a pier's deck where one meets it.
            var piers = _layout.squares.Where(s => s.x0 >= sea - 0.01f).ToList();
            var moored = piers.Select(s => (s.y0, s.y1)).ToArray();
            foreach (var (from, to) in new[] { (city.y0, city.y1), (works.y0, works.y1 + 2f) })
            {
                foreach (var (py0, py1) in Spans(from, to, moored))
                {
                    Slab(sea, sea + 1.2f, py0, py1, VeilmouthGround.SeaBed - 3f, Low + 0.3f);
                }
            }

            foreach (var s in piers)
            {
                Slab(sea, sea + 1.2f, s.y0, s.y1, VeilmouthGround.SeaBed - 3f, DeckTop - DeckDepth);
                Pier(s);
            }

            foreach (var b in _layout.bridges)
            {
                Bridge(b, b.axis == "x" ? west : south);
            }
        }

        /// <summary>Runs of at most 4 m from <paramref name="from"/> to <paramref name="to"/>, less the holes.</summary>
        private static IEnumerable<(float A0, float A1)> Spans(float from, float to, IReadOnlyList<(float, float)> holes)
        {
            for (var a = from; a < to - 0.01f; a += 4f)
            {
                var pieces = new List<(float A0, float A1)> { (a, Mathf.Min(a + 4f, to)) };
                foreach (var (h0, h1) in holes)
                {
                    pieces = pieces.SelectMany(p => p.A1 <= h0 || p.A0 >= h1 ? new[] { p } : new[] { (A0: p.A0, A1: Mathf.Min(p.A1, h0)), (A0: Mathf.Max(p.A0, h1), A1: p.A1) })
                        .Where(p => p.A1 - p.A0 > 0.01f).ToList();
                }

                foreach (var p in pieces)
                {
                    yield return p;
                }
            }
        }

        /// <summary>
        /// A stone bridge at the streets' level over the river on piers, with parapets: its deck runs on to the land at each
        /// end, where an abutment fills the bank under it down to the bed, so no gap shows between the deck and the ground
        /// (the owner's fourth play test, 2026-11-06).
        /// </summary>
        private static void Bridge(VeilmouthLayout.BridgeSpec b, VeilmouthLayout.Water river)
        {
            var alongX = b.axis == "x";
            var (s0, s1) = alongX ? (b.x0, b.x1) : (b.y0, b.y1);
            var (w0, w1) = alongX ? (river.x0, river.x1) : (river.y0, river.y1);
            var (c0, c1) = alongX ? (b.y0, b.y1) : (b.x0, b.x1);
            // An end on built ground stops at the bridge's own end; one on the natural bank runs 4 m on to the land.
            bool Built(float a) => VeilmouthGround.Built(_layout, alongX ? a : (c0 + c1) / 2f, alongX ? (c0 + c1) / 2f : a);
            var start = Built(w0 - 2f) ? s0 : Mathf.Min(s0, w0 - 4f);
            var end = Built(w1 + 2f) ? s1 : Mathf.Max(s1, w1 + 4f);
            void Box(float a0, float a1, float b0, float b1, float y0, float y1, string material = null)
            {
                if (alongX)
                {
                    Slab(a0, a1, b0, b1, y0, y1, material);
                }
                else
                {
                    Slab(b0, b1, a0, a1, y0, y1, material);
                }
            }

            Box(start, end, c0 + Parapet, c1 - Parapet, DeckTop - DeckDepth, DeckTop, DeckStone);
            Box(start, end, c0, c0 + Parapet, DeckTop - DeckDepth, DeckTop + ParapetHeight);
            Box(start, end, c1 - Parapet, c1, DeckTop - DeckDepth, DeckTop + ParapetHeight);
            Box(start, w0 + 2.5f, c0, c1, VeilmouthGround.RiverBed - 0.5f, DeckTop - DeckDepth);
            Box(w1 - 2.5f, end, c0, c1, VeilmouthGround.RiverBed - 0.5f, DeckTop - DeckDepth);
            var spans = Mathf.Max(1, Mathf.RoundToInt((w1 - w0 - 5f) / 12f));
            for (var k = 1; k < spans; k++)
            {
                var at = w0 + 2.5f + ((w1 - w0 - 5f) * k / spans);
                Box(at - 1.5f, at + 1.5f, c0 - 0.5f, c1 + 0.5f, VeilmouthGround.RiverBed - 1f, DeckTop - DeckDepth);
            }

            var deck = alongX ? Rect(start, c0 + Parapet, end, c1 - Parapet) : Rect(c0 + Parapet, start, c1 - Parapet, end);
            _decks.Add((deck, DeckTop));
            Take(alongX ? Rect(start, c0, end, c0 + Parapet) : Rect(c0, start, c0 + Parapet, end), Use.Solid);
            Take(alongX ? Rect(start, c1 - Parapet, end, c1) : Rect(c1 - Parapet, start, c1, end), Use.Solid);
            Take(deck, Use.Walk);
        }

        /// <summary>A pier: a stone deck at the streets' level over the sea on square piles every 6 m along both edges.</summary>
        private static void Pier(VeilmouthLayout.Square s)
        {
            Slab(s.x0, s.x1, s.y0, s.y1, DeckTop - DeckDepth, DeckTop, DeckStone);
            for (var px = s.x0 + 3f; px < s.x1 - 0.5f; px += 6f)
            {
                foreach (var py in new[] { s.y0 + 0.5f, s.y1 - 0.5f })
                {
                    Slab(px - 0.4f, px + 0.4f, py - 0.4f, py + 0.4f, VeilmouthGround.SeaBed - 3f, DeckTop - DeckDepth);
                }
            }

            var deck = Rect(s.x0, s.y0, s.x1, s.y1);
            _decks.Add((deck, DeckTop));
            Take(deck, Use.Walk);
        }

        // ------------------------------------------------------------------------------------------- lamps

        /// <summary>
        /// The furniture rules (§4), with the owner's fifth play test (2026-11-09, §26): a lamp on the carriageway by the
        /// kerb, its post <see cref="LampFromKerb"/> out from it, at a party wall where one is near, never within 2 m of a
        /// door along the street and never where a carriageway or a rounded corner breaks the kerb; 27 m apart, the two
        /// sides staggered on streets wider than 12 m. A quay's lamps stand in the cell along its edge, the Penny's round
        /// its ring, and a square's in its corners.
        /// </summary>
        private static void LampsC4()
        {
            const float spacing = 27f;
            foreach (var s in _layout.streets.Where(s => s.walk0 > 0f || s.walk1 > 0f))
            {
                var stagger = s.c1 - s.c0 > 12f ? spacing / 2f : 0f;
                if (s.walk0 > 0f)
                {
                    Lamps(s.AlongX, s.c0 + s.walk0, 1, (int)s.a0, (int)s.a1, spacing, 4f);
                }

                if (s.walk1 > 0f)
                {
                    Lamps(s.AlongX, s.c1 - s.walk1, -1, (int)s.a0, (int)s.a1, spacing, 4f + stagger);
                }
            }

            var quay = _layout.SquareById("quay");
            Lamps(false, quay.x1 - 2f, 0, (int)quay.y0, (int)quay.y1, 30f, 8f);

            // Round the Penny's ring, 2 m out from it, off the streets' axes.
            var plaza = _layout.plaza;
            for (var k = 0; k < 8; k++)
            {
                var angle = (22.5f + (45f * k)) * Mathf.Deg2Rad;
                var at = new Vector2(plaza.cx, plaza.cy) + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (plaza.ringOuter + 2f));
                Prop("LampPost", Facing.N, Mathf.Round(at.x - 0.5f), Mathf.Round(at.y - 0.5f));
            }

            foreach (var id in new[] { "drowsy", "bawling", "black_puddle", "deaf_boiler" })
            {
                var q = _layout.SquareById(id);
                foreach (var (px, py) in new[] { (q.x0 + 1f, q.y0 + 1f), (q.x1 - 2f, q.y0 + 1f), (q.x0 + 1f, q.y1 - 2f), (q.x1 - 2f, q.y1 - 2f) })
                {
                    Prop("LampPost", Facing.N, px, py);
                }
            }

            // A bridge's lamps at the ends of its parapets, a pier's at its far end.
            foreach (var b in _layout.bridges)
            {
                var alongX = b.axis == "x";
                var (s0, s1) = alongX ? (b.x0, b.x1) : (b.y0, b.y1);
                var (c0, c1) = alongX ? (b.y0, b.y1) : (b.x0, b.x1);
                foreach (var along in new[] { s0 + 1f, s1 - 2f })
                {
                    foreach (var across in new[] { c0 + Parapet, c1 - Parapet - 1f })
                    {
                        if (alongX)
                        {
                            Prop("LampPost", Facing.N, along, across, force: true);
                        }
                        else
                        {
                            Prop("LampPost", Facing.N, across, along, force: true);
                        }
                    }
                }
            }

            foreach (var s in _layout.squares.Where(s => s.x0 >= _layout.Sea.x0 - 0.01f))
            {
                Prop("LampPost", Facing.N, s.x1 - 2f, s.y0 + 0.5f, force: true);
                Prop("LampPost", Facing.N, s.x1 - 2f, s.y1 - 1.5f, force: true);
            }
        }

        // ------------------------------------------------------------------------------------------- street trees

        /// <summary>
        /// The street trees (§25.3, §26, §27.10, §28.1 item 6): the owner's mature tree in square pits on the carriageway,
        /// flush on the kerb, in the lamps' line. A main or ring street has both kerbs planted where its carriageway is
        /// 8 m or more; a side street and the embankment have one row, on the kerb of their wider pavement, under which
        /// the crowns reach; the slums' lanes and the works have none.
        /// </summary>
        private static void StreetTreesC4()
        {
            var planted = 0;
            var oneSided = 0;
            var street = 0;
            foreach (var s in _layout.streets.Where(s => s.trees != "none" && (s.walk0 > 0f || s.walk1 > 0f)))
            {
                var near = s.c0 + s.walk0;
                var far = s.c1 - s.walk1;
                bool nearSide, farSide;
                if (s.trees == "both")
                {
                    var both = far - near >= TwoRowsFrom;
                    nearSide = both || street % 2 == 0;
                    farSide = both || !nearSide;
                    oneSided += both ? 0 : 1;
                }
                else
                {
                    nearSide = s.walk0 >= s.walk1;
                    farSide = !nearSide;
                    oneSided++;
                }

                if (nearSide && s.walk0 > 0f)
                {
                    planted += TreeRow(s.AlongX, near, 1, (int)s.a0, (int)s.a1);
                }

                if (farSide && s.walk1 > 0f)
                {
                    planted += TreeRow(s.AlongX, far, -1, (int)s.a0, (int)s.a1);
                }

                street++;
            }

            Debug.Log($"[VeilmouthCity] street trees (§26, §27.10): {planted} of the mature tree, each in its pit by the kerb; {oneSided} streets planted on one side.");
        }
    }
}
