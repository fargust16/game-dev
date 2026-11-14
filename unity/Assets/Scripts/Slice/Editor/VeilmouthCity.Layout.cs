using System.Collections.Generic;
using System.Linq;
using GameDev.Slice.Kit;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

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

        /// <summary>
        /// How far the quay wall's coping and a pier's deck reach inland over the ground's edge, m: past the terrain's
        /// last sample on land (0.98 m apart) and its level of detail; and the coping's top, a kerb over the quay.
        /// </summary>
        private const float Coping = 1.5f, CopingTop = Low + VeilmouthPavements.Kerb;

        /// <summary>Authoring default, m (§31): stone under a bank's edge beside a kerb or a bridge's abutment, out from it.</summary>
        private const float BankFill = 1.5f;

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

        /// <summary>The ground of a block's inner yard by its wealth (§29.2): a rich yard's grass, a poor one's bare earth, else cobbles.</summary>
        private static int YardLayer(string wealth) => wealth switch
        {
            "rich" => Grass, "poor" => YardEarth, _ => Cobbles,
        };

        /// <summary>A poor yard's mud: the canonical soil layer.</summary>
        private const int YardEarth = 2;

        /// <summary>The sides of the Penny's ring of setts, as many as the island's disc has (<see cref="Disc"/>).</summary>
        private const int RingSides = 48;

        /// <summary>
        /// The streets, squares, blocks and gardens as lanes, in the order they are painted, every carriageway last
        /// (<see cref="PaintStreets"/>, §29.1 item 2). The ground inside the walls is cobbled; a block's inner yard is grass,
        /// cobbles or earth by its wealth, its passages cobbled; the gardens and parks are green. The squares that houses
        /// front, the Penny's frame and the island with the statue are raised pavements, so every way into a building is
        /// from a pavement (§29.1 item 1); the Penny's ring is a carriageway of setts in segments round the island, and the
        /// streets' carriageways run on into it unbroken.
        /// </summary>
        private static void Streets()
        {
            foreach (var area in new[] { _layout.City, _layout.Works })
            {
                // Setts under all, so the corners the pavements round off at a junction read as the carriageway (§31).
                _lanes.Add(new Lane { Street = "Yards", Quad = Rect(area.x0, area.y0, area.x1, area.y1), Layer = Setts, PaintOnly = true });
            }

            foreach (var b in _layout.blocks.Where(b => b.yard != null && b.yard.x1 > b.yard.x0))
            {
                _lanes.Add(new Lane
                {
                    Street = $"{b.id}'s yard", Quad = Rect(b.yard.x0, b.yard.y0, b.yard.x1, b.yard.y1), Layer = YardLayer(b.wealth), PaintOnly = true,
                });
            }

            foreach (var p in _layout.passages)
            {
                _lanes.Add(new Lane
                {
                    Street = $"{p.block}'s passage", Quad = Footprint(FacingOf(p.facing), p.front, p.a0, p.a1 - p.a0, p.depth), Layer = Cobbles,
                    PaintOnly = true,
                });
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

            // A paved square is a raised pavement; a court, a yard, the quay's apron and the green ones lie flush.
            foreach (var s in _layout.squares.Where(s => s.x0 < _layout.Sea.x0 - 0.01f)) // the piers are decks over the sea
            {
                var layer = s.surface == "square" ? Flags : SurfaceLayer(s.surface);
                Lanes(string.IsNullOrEmpty(s.name) ? s.id : s.name, layer, false, Rect(s.x0, s.y0, s.x1, s.y1), Use.Walk);
            }

            var plaza = _layout.plaza;
            var centre = new Vector2(plaza.cx, plaza.cy);
            Vector2 Dir(int k) => new Vector2(Mathf.Cos(k * 2f * Mathf.PI / RingSides), Mathf.Sin(k * 2f * Mathf.PI / RingSides));
            // A district with no plaza of its own (the island's frames but the Penny's) has neither ring nor island.
            for (var k = 0; k < (plaza.half > 0f ? RingSides : 0); k++)
            {
                var quad = new[]
                {
                    centre + (Dir(k) * plaza.ringInner), centre + (Dir(k) * plaza.ringOuter), centre + (Dir(k + 1) * plaza.ringOuter),
                    centre + (Dir(k + 1) * plaza.ringInner),
                };
                Lanes("The Penny's ring", Setts, true, quad, Use.Road);
            }

            if (plaza.half > 0f)
            {
                Lanes("The Penny's island", Flags, false, Disc(centre, plaza.island), Use.Walk);
            }

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

                // §31 (owner's walk, 2026-11-10): every carriageway in the one texture, the setts.
                Lanes(s.name, s.surface == "grass" ? Grass : Setts, s.walk0 > 0f || s.walk1 > 0f, Band(s.c0 + s.walk0, s.c1 - s.walk1), Use.Road);
            }

            // Squint Lane at 45° (§12): setts, no pavements, run 2 m into the squares at its ends.
            foreach (var sl in _layout.slants)
            {
                Lanes(sl.name, Setts, false, Along(new Vector2(sl.x0, sl.y0), new Vector2(sl.x1, sl.y1), sl.width / 2f, 2f), Use.Road);
            }

            // Each bridge's carriageway is carried through the pavement or the gate at its city end, so the kerb opens
            // onto it: Grubby Bridge through the embankment's promenade, as wide as Smoked Street's carriageway that runs
            // on over it, and Last Farthing Bridge through Creaking Gate.
            foreach (var b in _layout.bridges)
            {
                var street = Nearest(b);
                if (street == null)
                {
                    continue;
                }

                var mid = b.axis == "x" ? (b.y0 + b.y1) / 2f : (b.x0 + b.x1) / 2f;
                var onward = b.axis == "x"
                    ? street
                    : _layout.streets.Where(s => !s.AlongX && s.c0 < mid && s.c1 > mid && s.a1 <= b.y0 + 0.01f).OrderBy(s => b.y0 - s.a1).FirstOrDefault();
                var half = (street.c1 - street.walk1 - street.c0 - street.walk0) / 2f;
                var (r0, r1) = onward != null ? (onward.c0 + onward.walk0, onward.c1 - onward.walk1) : (mid - half, mid + half);
                // No wider than the bridge, so the pavement meets its parapet with no strip of ground between (§31).
                var (bc0, bc1) = b.axis == "x" ? (b.y0, b.y1) : (b.x0, b.x1);
                (r0, r1) = (Mathf.Max(r0, bc0), Mathf.Min(r1, bc1));
                var quad = b.axis == "x"
                    ? Rect(b.x1 - 2f, r0, street.a0, r1)
                    : Rect(r0, street.c1 - street.walk1, r1, b.y0);
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

            // Along the south leg: the promenade's parapet on the city's side, one length between two bridges at the height
            // of the bridges' own parapets, so no step and no joint shows (§31); a low kerb on the works' side, with stone
            // under the bank's edge beyond it a little under the ground, so no slit of water shows between them (§31).
            foreach (var (px0, px1) in Runs(city.x0 - 2f, sea, crossY))
            {
                Slab(px0, px1, south.y0 - 0.1f, south.y0 + 1.2f, VeilmouthGround.RiverBed - 0.5f, DeckTop + ParapetHeight);
            }

            foreach (var (px0, px1) in Spans(city.x0 - 2f, sea, crossY))
            {
                Slab(px0, px1, south.y1 - 1.2f, south.y1 + 0.1f, VeilmouthGround.RiverBed - 0.5f, Low + 0.3f);
                Slab(px0, px1, south.y1 + 0.1f, south.y1 + BankFill, VeilmouthGround.RiverBed - 0.5f, Low - 0.05f);
            }

            // The quay wall along the sea, the city's and the works', from the north wall's footing; under a pier's deck
            // where one meets it. Its coping reaches Coping m inland over the ground's edge, where the terrain falls to the
            // sea bed between two of its samples and opened a gap a walker fell into (the owner's walk of C4, §29.1 item 6);
            // its top is a deck a kerb's height over the quay.
            var piers = _layout.squares.Where(s => s.x0 >= sea - 0.01f).ToList();
            var moored = piers.Select(s => (s.y0, s.y1)).ToArray();
            foreach (var (from, to) in new[] { (city.y0 - 3f, city.y1), (works.y0, works.y1 + 2f) })
            {
                foreach (var (py0, py1) in Spans(from, to, moored))
                {
                    Slab(sea - Coping, sea + 1.2f, py0, py1, VeilmouthGround.SeaBed - 3f, CopingTop);
                }

                foreach (var (py0, py1) in Runs(from, to, moored))
                {
                    _decks.Add((Rect(sea - Coping, py0, sea + 1.2f, py1), CopingTop));
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

        /// <summary>The unbroken runs from <paramref name="from"/> to <paramref name="to"/> between the holes.</summary>
        private static IEnumerable<(float A0, float A1)> Runs(float from, float to, IReadOnlyList<(float A, float B)> holes)
        {
            var a = from;
            foreach (var (h0, h1) in holes.Where(h => h.B > from && h.A < to).OrderBy(h => h.A))
            {
                if (h0 > a + 0.01f)
                {
                    yield return (a, h0);
                }

                a = Mathf.Max(a, h1);
            }

            if (to > a + 0.01f)
            {
                yield return (a, to);
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

            // §31 (owner's walk, 2026-11-10): on a natural bank, wing walls either side of the abutment, a little under the
            // ground, so the bank meets the bridge with no slit of water beside it.
            foreach (var (a0, a1, natural) in new[] { (start, w0 + 0.5f, !Built(w0 - 2f)), (w1 - 0.5f, end, !Built(w1 + 2f)) })
            {
                if (natural)
                {
                    Box(a0, a1, c0 - BankFill, c0, VeilmouthGround.RiverBed - 0.5f, Low - 0.05f);
                    Box(a0, a1, c1, c1 + BankFill, VeilmouthGround.RiverBed - 0.5f, Low - 0.05f);
                }
            }
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

        /// <summary>
        /// A pier: a stone deck at the streets' level over the sea on square piles every 6 m along both edges; it starts
        /// <see cref="Coping"/> m inland, over the ground's edge, as the quay wall's coping does.
        /// </summary>
        private static void Pier(VeilmouthLayout.Square s)
        {
            Slab(s.x0 - Coping, s.x1, s.y0, s.y1, DeckTop - DeckDepth, DeckTop, DeckStone);
            for (var px = s.x0 + 3f; px < s.x1 - 0.5f; px += 6f)
            {
                foreach (var py in new[] { s.y0 + 0.5f, s.y1 - 0.5f })
                {
                    Slab(px - 0.4f, px + 0.4f, py - 0.4f, py + 0.4f, VeilmouthGround.SeaBed - 3f, DeckTop - DeckDepth);
                }
            }

            var deck = Rect(s.x0 - Coping, s.y0, s.x1, s.y1);
            _decks.Add((deck, DeckTop));
            Take(deck, Use.Walk);
        }

        // ------------------------------------------------------------------------------------------- buildings

        /// <summary>The kit key of a layout's building: its own entry where the catalogue has one, else the baked house's.</summary>
        private static string KitKey(string key) => _kit.ContainsKey(key) ? key : "House_" + key;

        /// <summary>
        /// Whether a building on a plain stoop is sunk (owner, 2026-11-10, §28.1 item 5): every house on a street's
        /// pavement or a square, but the civic fronts, the towers and the chapel, which keep their own stairs and bases.
        /// </summary>
        private static bool Sinks(string key, string stoop) =>
            stoop != "rich" && !key.Contains("Civic") && !key.Contains("Palace") && !key.Contains("Tower") && !key.Contains("Villa") && key != "Chapel";

        /// <summary>The row families (§24.4, §28.5): houses of one family share height, depth and roof, so their open ends meet.</summary>
        private static string[] Family(string family) => family switch
        {
            "shop_rich" => new[] { VictorianHouses.Key("Shop12", "rich"), VictorianHouses.Key("Shop8", "stucco") },
            "shop" => ShopRow,
            "town" => TownRow,
            "tenement" => TenementRow,
            "low" => LowTenementRow,
            "warehouse" => WarehouseRow,
            _ => throw new System.ArgumentException($"no row family '{family}'"),
        };

        /// <summary>While set, <see cref="House"/> sinks what it puts up by <see cref="Sink"/>.</summary>
        private static bool _sinking;

        /// <summary>
        /// A sunk house's stoop rails are hidden: its stoop shows only its top step, 0.15 m over the pavement, which needs
        /// none (owner, 2026-11-10: a house with no railings straight on the pavement has no stair, or a narrow low one).
        /// </summary>
        private static void SinkStoops(GameObject house)
        {
            _sunk.Add(house.transform);
            var model = house.transform.Find("Model");
            if (model == null)
            {
                return;
            }

            var doors = house.transform.Cast<Transform>().Where(t => t.name == VictorianHouses.DoorMarker || t.name == VeilmouthKitHouses.YardDoorMarker)
                .Select(t => t.position).ToList();
            foreach (var piece in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("SM_Vic_Rail_")))
            {
                if (doors.Any(d => Flat(piece.position - d) < 2.2f && Mathf.Abs(piece.position.y - d.y) < 1.2f))
                {
                    piece.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Every building the planner placed (§29.4): the blocks' row houses, each with its faced ends as the planner chose
        /// them, sunk on its plain stoop; the Penny's civic fronts, the chapel, the Brothers' House, the Customs House, the
        /// One-Eye, the faction's house, and Lower Veil's openable houses with their doors into their interiors (§5.3).
        /// </summary>
        private static void Buildings()
        {
            var portals = new Dictionary<string, (string Id, string Scene, string Target)>
            {
                ["faction_house"] = ("faction-house.street", VeilmouthDressing.FactionHouseScenePath, "faction-house.inside"),
                ["assize"] = ("wardens-lockup.street", VeilmouthDressing.LockupScenePath, "wardens-lockup.inside"),
                ["soot_bell"] = (VeilmouthScene.SootAndBellStreetPortalId, VeilmouthScene.SootAndBellScenePath, VeilmouthScene.SootAndBellInsidePortalId),
                ["harrow"] = ("harrow-apothecary.street", VeilmouthDressing.HarrowScenePath, "harrow-apothecary.inside"),
                ["missing"] = ("missing-room.street", VeilmouthDressing.MissingRoomScenePath, "missing-room.inside"),
            };
            var lifts = Lifts();
            foreach (var b in _layout.buildings)
            {
                var key = KitKey(b.key);
                if (key == "Chapel")
                {
                    Chapel(b);
                    continue;
                }

                _sinking = Sinks(key, b.stoop);
                var house = House(key, FacingOf(b.facing), b.front, b.a, lifts.TryGetValue(b, out var lift) ? lift : 0f, b.name, true);
                _sinking = false;
                if (house == null)
                {
                    continue;
                }

                if (portals.TryGetValue(b.id, out var portal))
                {
                    Entrance(house, portal.Id, portal.Scene, portal.Target);
                }

                if (b.id == "soot_bell")
                {
                    VeilmouthYardDoor.Leaf(house.transform, _kit, house.transform.parent);
                }
            }

            // The Notice Board on the Black Puddle by the Soot & Bell, 3 m out from the square's west fronts, beyond the reach of
            // their stoops (1 m out, it was refused by one stoop and touched the tavern's by 0.14 m, which the free-place test
            // does not see), and north of the hidden ritual circle.
            var puddle = _layout.squares.FirstOrDefault(s => s.id == "black_puddle");
            if (puddle == null)
            {
                return; // a district of the island that is not the Black Puddle's
            }

            var board = new[] { (3f, 3f), (3f, 6f), (4f, 2f), (5f, 4f) }
                .Select(o => Prop("NoticeBoard", Facing.E, puddle.x0 + o.Item1, puddle.y0 + o.Item2, "Notice Board"))
                .FirstOrDefault(p => p != null);
            if (board == null)
            {
                Debug.LogError("[VeilmouthCity] no free place for the Notice Board on the Black Puddle.");
            }
        }

        /// <summary>
        /// §32: the planner's buildings wall to wall on one front, a run, set on one level (<see cref="Level"/>) by their
        /// floors as they would stand, sunk or not; so a sunk house beside one on its own stair is raised to it. Returns
        /// each building's lift.
        /// </summary>
        private static Dictionary<VeilmouthLayout.BuildingSpec, float> Lifts()
        {
            var lifts = new Dictionary<VeilmouthLayout.BuildingSpec, float>();
            float Floor(VeilmouthLayout.BuildingSpec b) =>
                OwnFloor(KitKey(b.key), FacingOf(b.facing), b.front, b.a) - (Sinks(KitKey(b.key), b.stoop) ? Sink : 0f);

            foreach (var line in _layout.buildings.Where(b => KitKey(b.key) != "Chapel").GroupBy(b => (b.facing, Mathf.RoundToInt(b.front * 10f))))
            {
                var run = new List<VeilmouthLayout.BuildingSpec>();
                foreach (var b in line.OrderBy(b => b.a).Append(null))
                {
                    var last = run.Count > 0 ? run[run.Count - 1] : null;
                    if (last != null && (b == null || Mathf.Abs(last.a + _kit[KitKey(last.key)].Width - b.a) > 0.01f))
                    {
                        var lift = Level(run.Select(Floor).ToList());
                        for (var i = 0; i < run.Count; i++)
                        {
                            lifts[run[i]] = lift[i];
                        }

                        run.Clear();
                    }

                    if (b != null)
                    {
                        run.Add(b);
                    }
                }
            }

            return lifts;
        }

        /// <summary>The old chapel, its door's portal at the kit's door point, opening on its forecourt (§28.5).</summary>
        private static void Chapel(VeilmouthLayout.BuildingSpec b)
        {
            const string key = "Chapel";
            var f = FacingOf(b.facing);
            var chapel = House(key, f, b.front, b.a, name: b.name, force: true);
            var right = chapel.transform.right;
            var door = chapel.transform.position + (chapel.transform.rotation * new Vector3(_kit[key].DoorX, 0f, _kit[key].Depth));
            var leaf = (GameObject)PrefabUtility.InstantiatePrefab(_kit["PortalLeaf"].Prefab, _parent);
            var start = door - (right * 1.5f);
            leaf.transform.SetPositionAndRotation(new Vector3(Mathf.Round(start.x), Up(door.y), Mathf.Round(start.z)), chapel.transform.rotation);
            Configure(leaf, "old-chapel.street", VeilmouthDressing.ChapelScenePath, "old-chapel.inside");
            _doors.Add((Plan(door), Outward(f)));
        }

        /// <summary>
        /// The rows and the plots (§28.5): a row of one family along its street, wall to wall with passages (R5), sunk
        /// unless it is a rich row; a plot's detached house or villa in its railed garden (§19), on its full stair.
        /// </summary>
        private static void Houses()
        {
            foreach (var p in _layout.plots)
            {
                var keys = PlotKeys(p.kind);
                var rng = new Random(p.seed);
                var named = !string.IsNullOrEmpty(p.name);
                var house = Plot(FacingOf(p.facing), p.front, (int)p.s0, (int)p.s1, keys[rng.Next(keys.Length)], p.garden, p.behind, rng,
                    named ? p.name : null, p.at >= 0f ? p.at : (float?)null, named);
                if (house != null && p.name == "The faction's house")
                {
                    Entrance(house, "faction-house.street", VeilmouthDressing.FactionHouseScenePath, "faction-house.inside");
                }
            }

            foreach (var r in _layout.rows)
            {
                _sinking = r.stoop != "rich";
                Row(FacingOf(r.facing), r.front, (int)r.a0, (int)r.a1, Family(r.family), r.seed);
                _sinking = false;
            }

            foreach (var s in _layout.slants)
            {
                _sinking = true;
                Slant(s);
                _sinking = false;
            }

            // The planner's passages into the yards (§29.4, R5): their ground taken as walk, their backs railed once every house stands.
            foreach (var p in _layout.passages)
            {
                Passage(FacingOf(p.facing), p.front, Mathf.RoundToInt(p.a0), Mathf.RoundToInt(p.a1), Mathf.RoundToInt(p.depth));
            }

            Debug.Log($"[VeilmouthCity] C5 houses: {_houses.Count} footprints, {_sunk.Count} sunk on plain stoops (§28.1 item 5), {_passages} passages; " +
                $"rows on one level (§32): the most a house is raised {_terraceLift:0.00} m, {_terraceCuts} runs cut.");
        }

        /// <summary>
        /// Squint Lane at 45° (§12): a frame on a world node; its houses keep the grid and the turns in its axes. The low
        /// tenements of Lower Veil, a run on each side with its ends faced; a run stays unbroken, or its open ends would show.
        /// </summary>
        private static void Slant(VeilmouthLayout.SlantSpec s)
        {
            var from = new Vector2(s.x0, s.y0);
            var to = new Vector2(s.x1, s.y1);
            var frame = new GameObject($"{s.name} (45°)");
            frame.transform.SetParent(_root, false);
            var d = to - from;
            frame.transform.SetPositionAndRotation(World(from.x, from.y), Quaternion.Euler(0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, 0f));
            frame.AddComponent<KitFrame>();
            var palette = Family(s.family);
            var rng = new Random(45);
            var half = s.width / 2f;
            var length = d.magnitude;
            foreach (var side in new[] { 1, -1 })
            {
                // In the frame's axes a house's local −X lies toward the lane's end (+X) on the side whose fronts turn
                // round (side 1), toward its start on the other.
                var placed = new List<(string Key, int X, Vector2[] Quad, float Z0)>();
                for (var x = 6; x + 10 <= length - 6f; x += 10)
                {
                    var key = palette[rng.Next(palette.Length)];
                    var e = _kit[key];
                    var z0 = side > 0 ? half : -half - e.Depth;
                    var local = new[] { new Vector2(x, z0), new Vector2(x + e.Width, z0), new Vector2(x + e.Width, z0 + e.Depth), new Vector2(x, z0 + e.Depth) };
                    var quad = local.Select(l => Plan(frame.transform.TransformPoint(new Vector3(l.x, 0f, l.y)))).ToArray();
                    if (!Free(quad, true))
                    {
                        break;
                    }

                    placed.Add((key, x, quad, z0));
                }

                var endSide = side > 0 ? VictorianHouses.Side.Left : VictorianHouses.Side.Right;
                var startSide = side > 0 ? VictorianHouses.Side.Right : VictorianHouses.Side.Left;
                for (var i = 0; i < placed.Count; i++)
                {
                    var (key, x, quad, z0) = placed[i];
                    key = placed.Count == 1 ? VictorianHouses.CornerKey(key, VictorianHouses.Side.Both)
                        : i == 0 ? VictorianHouses.CornerKey(key, startSide)
                        : i == placed.Count - 1 ? VictorianHouses.CornerKey(key, endSide) : key;
                    var e = _kit[key];
                    Take(quad, Use.Solid);
                    _houses.Add(quad);
                    var (min, max) = GroundUnder(quad);
                    var floor = Up(max) - (_sinking ? Sink : 0f);
                    var house = Put(key, frame.transform, x, z0, side > 0 ? 2 : 0, floor);
                    foreach (Transform child in house.transform)
                    {
                        if (child.name == VictorianHouses.DoorMarker)
                        {
                            var f = house.transform.forward;
                            Doorway(child.position, new Vector2(f.x, -f.z), floor, e.Stoop, e.StoopWidth);
                        }
                    }

                    Stoops(house);
                    if (_sinking)
                    {
                        SinkStoops(house);
                    }

                    if (floor - min > 0.25f)
                    {
                        var centre = frame.transform.TransformPoint(new Vector3(x + (e.Width / 2f), 0f, z0 + (e.Depth / 2f)));
                        _works.Box(new Vector3(centre.x, (floor + min - 0.6f) / 2f, centre.z), new Vector3(e.Width, floor - min + 0.6f, e.Depth),
                            frame.transform.eulerAngles.y);
                    }
                }
            }
        }

        // ------------------------------------------------------------------------------------------- lamps

        /// <summary>Every lamp placed: its plan point, and for a kerb's lamp its street's id and its kerb (+1 near, −1 far).</summary>
        private static readonly List<(Vector2 C, string Street, int Side)> _lamps = new List<(Vector2, string, int)>();

        /// <summary>Every road tree's trunk, plan.</summary>
        private static readonly List<Vector2> _roadTrees = new List<Vector2>();

        /// <summary>
        /// The owner's walk of 2026-11-10 (§31): a street's lamps and trees keep one rhythm on each unbroken run of kerb
        /// between its junctions. The lamps go first: the bridges', the Penny's ring, the quay's, then each street's kerbs
        /// (<see cref="StreetLamps"/>); the trees follow (<see cref="StreetTreesC4"/>), and a square's corners last
        /// (<see cref="SquareLamps"/>), only where no lamp is near.
        /// </summary>
        private static void LampsC4()
        {
            _lamps.Clear();
            _roadTrees.Clear();
            _planted.Clear();

            // Every door a lamp keeps clear of: the recorded doors, and each portal's door at its middle, 1.5 m along it, as
            // VeilmouthCityTests reads it (a building that opens has its door where its portal is, not at its marker).
            _doorPoints = _doors.Select(d => d.At)
                .Concat(_root.GetComponentsInChildren<GameDev.World.Portals.Portal>(true).Select(p => Plan(p.transform.position + (p.transform.right * 1.5f))))
                .ToList();

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
                        var (px, py) = alongX ? (along, across) : (across, along);
                        if (Prop("LampPost", Facing.N, px, py, force: true) != null)
                        {
                            _lamps.Add((new Vector2(px + 0.5f, py + 0.5f), null, 0));
                        }
                    }
                }
            }

            foreach (var s in _layout.squares.Where(s => s.x0 >= _layout.Sea.x0 - 0.01f))
            {
                foreach (var (px, py) in new[] { (s.x1 - 2f, s.y0 + 0.5f), (s.x1 - 2f, s.y1 - 1.5f) })
                {
                    if (Prop("LampPost", Facing.N, px, py, force: true) != null)
                    {
                        _lamps.Add((new Vector2(px + 0.5f, py + 0.5f), null, 0));
                    }
                }
            }

            // Round the Penny's ring, 2 m out from it, off the streets' axes, each turned along the ring until it stands in no
            // door's way (9 m before a door and 2.5 m to its side, a little wider than VeilmouthCityTests reads).
            var plaza = _layout.plaza;
            var doors = _root.GetComponentsInChildren<Transform>(true).Where(t => t.name == VictorianHouses.DoorMarker).ToList();
            bool InADoorsWay(Vector2 plan)
            {
                var at = new Vector3(plan.x - 500f, 0f, 500f - plan.y);
                return doors.Any(d =>
                {
                    var off = at - d.position;
                    off.y = 0f;
                    var outside = Vector3.Dot(off, d.forward);
                    return outside > 0f && outside < 9f && Mathf.Abs(Vector3.Dot(off, d.right)) < 2.5f;
                });
            }

            for (var k = 0; k < (plaza.half > 0f ? 8 : 0); k++) // none where a district has no plaza of its own (the island's frames)
            {
                foreach (var turn in new[] { 0f, 6f, -6f, 12f, -12f })
                {
                    var angle = (22.5f + (45f * k) + turn) * Mathf.Deg2Rad;
                    var at = new Vector2(plaza.cx, plaza.cy) + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (plaza.ringOuter + 2f));
                    var corner = new Vector2(Mathf.Round(at.x - 0.5f), Mathf.Round(at.y - 0.5f));
                    if (!InADoorsWay(corner + new Vector2(0.5f, 0.5f)) && !InADoorsWay(corner + new Vector2(0.5f, -0.5f)) &&
                        LampRoom(corner + new Vector2(0.5f, 0.5f), null, 0, false, false) &&
                        Prop("LampPost", Facing.N, corner.x, corner.y) != null)
                    {
                        _lamps.Add((corner + new Vector2(0.5f, 0.5f), null, 0));
                        break;
                    }
                }
            }

            // The quay's lamps in the cell inside the quay wall's coping.
            // C5's quay runs along the sea, north to south. A district of the island has none, or one along its own
            // waterside, whose lamps the island's waterside stage sets.
            var quay = _layout.squares.FirstOrDefault(s => s.id == "quay");
            if (quay != null && quay.y1 - quay.y0 > quay.x1 - quay.x0)
            {
                Lamps(false, quay.x1 - Coping - 1.5f, 0, (int)quay.y0, (int)quay.y1, Bay, 8f);
            }

            var alternate = 0;
            foreach (var s in _layout.streets.Where(s => s.walk0 > 0f || s.walk1 > 0f))
            {
                _planted[s] = PlantedKerbs(s, ref alternate);
            }

            foreach (var s in _layout.streets.Where(s => s.walk0 > 0f || s.walk1 > 0f))
            {
                StreetLamps(s);
            }
        }

        /// <summary>Each street's planted kerbs (§27): both where the carriageway is TwoRowsFrom m or more, else one.</summary>
        private static readonly Dictionary<VeilmouthLayout.Street, (bool Near, bool Far)> _planted = new Dictionary<VeilmouthLayout.Street, (bool, bool)>();

        /// <summary>
        /// The street trees' kerbs (§25.3, §26, §27.10): a main or ring street has both kerbs planted where its carriageway
        /// is 8 m or more; a side street and the embankment have one row, on the kerb of their wider pavement, under which
        /// the crowns reach; a narrower main street has one row on alternate sides street by street; the slums' lanes and
        /// the works have none.
        /// </summary>
        private static (bool Near, bool Far) PlantedKerbs(VeilmouthLayout.Street s, ref int alternate)
        {
            if (s.trees == "none")
            {
                return (false, false);
            }

            bool near, far;
            if (s.trees == "both")
            {
                var both = (s.c1 - s.walk1) - (s.c0 + s.walk0) >= TwoRowsFrom;
                near = both || alternate % 2 == 0;
                far = both || !near;
            }
            else
            {
                near = s.walk0 >= s.walk1;
                far = !near;
            }

            alternate++;
            return (near && s.walk0 > 0f, far && s.walk1 > 0f);
        }

        /// <summary>
        /// A street's kerb lamps (§31): one in the middle of each bay of about <see cref="Bay"/> m on a kerb's run. Both
        /// kerbs planted: a lamp on each, opposite. One planted: the lamps on the other kerb, or on the planted one between
        /// its trees where the other has no pavement. None: on a street wider than 12 m the kerbs take the bays in turn,
        /// else the wider pavement's kerb takes them all.
        /// </summary>
        private static void StreetLamps(VeilmouthLayout.Street s)
        {
            var (treesNear, treesFar) = _planted[s];
            bool hasNear = s.walk0 > 0f, hasFar = s.walk1 > 0f;
            if (treesNear && treesFar)
            {
                KerbLamps(s, true, _ => true);
                KerbLamps(s, false, _ => true);
            }
            else if (treesNear || treesFar)
            {
                var near = treesNear ? !hasFar : hasNear;
                KerbLamps(s, near, _ => true);
            }
            else if (hasNear && hasFar && s.c1 - s.c0 > 12f)
            {
                KerbLamps(s, true, i => i % 2 == 0);
                KerbLamps(s, false, i => i % 2 == 1);
            }
            else
            {
                KerbLamps(s, hasNear && (!hasFar || s.walk0 >= s.walk1), _ => true);
            }
        }

        /// <summary>The unbroken runs of a kerb, as metre cells [R0, R1): the pavement on its inside, no carriageway or rounded corner crossing it.</summary>
        private static List<(int R0, int R1)> KerbRunsOf(bool alongX, float line, int side, int a0, int a1)
        {
            var runs = new List<(int, int)>();
            var start = -1;
            for (var at = a0; at <= a1; at++)
            {
                var ok = at < a1 && KerbRuns(alongX, line, side, at + 0.5f, 0.6f);
                if (ok && start < 0)
                {
                    start = at;
                }
                else if (!ok && start >= 0)
                {
                    runs.Add((start, at));
                    start = -1;
                }
            }

            return runs;
        }

        /// <summary>A kerb run's bays: as many of about <see cref="Bay"/> m as fit, at least one, and their length.</summary>
        private static (int Count, float Length) Bays((int R0, int R1) run)
        {
            var length = run.R1 - run.R0;
            var count = Mathf.Max(1, Mathf.RoundToInt(length / Bay));
            return (count, length / (float)count);
        }

        private static void KerbLamps(VeilmouthLayout.Street s, bool near, System.Func<int, bool> takes)
        {
            var line = near ? s.c0 + s.walk0 : s.c1 - s.walk1;
            var side = near ? 1 : -1;
            foreach (var run in KerbRunsOf(s.AlongX, line, side, (int)s.a0, (int)s.a1).Where(r => r.R1 - r.R0 >= MinLampRun))
            {
                var (count, bay) = Bays(run);
                for (var i = 0; i < count; i++)
                {
                    if (takes(i))
                    {
                        KerbLamp(s, line, side, run, run.R0 + (bay * (i + 0.5f)));
                    }
                }
            }
        }

        /// <summary>
        /// One kerb lamp at a bay's middle, or moved by at most <see cref="LampShift"/> m: to a party wall first, the nearest,
        /// then to the nearest metre clear of a door's way (§26) and of other lamps and trees (<see cref="LampRoom"/>);
        /// none where nothing is clear.
        /// </summary>
        private static void KerbLamp(VeilmouthLayout.Street s, float line, int side, (int R0, int R1) run, float station)
        {
            var alongX = s.AlongX;
            var across = line + (side * LampFromKerb);
            var tries = _partyWalls.Where(w => w.AlongX == alongX && Mathf.Abs(w.Front - line) < 6f && Mathf.Abs(w.At - station) <= LampShift)
                .Select(w => w.At).OrderBy(w => Mathf.Abs(w - station)).ToList();
            for (var k = 0; k <= LampShift; k++)
            {
                tries.Add(Mathf.Round(station) + k);
                tries.Add(Mathf.Round(station) - k);
            }

            foreach (var at in tries.Distinct())
            {
                var along = at - 0.5f;
                var c = alongX ? new Vector2(along, across) : new Vector2(across, along);
                if (along < run.R0 + 0.5f || along > run.R1 - 0.5f || !KerbRuns(alongX, line, side, along, 0.6f) ||
                    _doorPoints.Any(d => Mathf.Abs((alongX ? d.y : d.x) - across) < 8.5f && Mathf.Abs((alongX ? d.x : d.y) - along) < 2.2f) ||
                    !LampRoom(c, s.id, side, alongX, true))
                {
                    continue;
                }

                if (OnCarriageway("LampPost", c, 1f) != null)
                {
                    _lamps.Add((c, s.id, side));
                    return;
                }
            }

            _lampsDropped++;
        }

        private static int _lampsDropped;

        /// <summary>The doors' plan points a kerb lamp keeps clear of, gathered by <see cref="LampsC4"/>.</summary>
        private static List<Vector2> _doorPoints = new List<Vector2>();

        /// <summary>
        /// Whether a lamp may stand at a plan point (§31): no lamp within <see cref="LampFromLamp"/> m but a wide street's
        /// lamp opposite on its other kerb; no road tree within <see cref="LampFromTree"/> m; and, off a kerb, no house
        /// within <see cref="LampFromWall"/> m.
        /// </summary>
        private static bool LampRoom(Vector2 c, string street, int side, bool alongX, bool kerb)
        {
            foreach (var l in _lamps)
            {
                var d = l.C - c;
                if (d.sqrMagnitude >= LampFromLamp * LampFromLamp)
                {
                    continue;
                }

                if (street == null || l.Street != street || l.Side != -side || Mathf.Abs(alongX ? d.x : d.y) > LampShift)
                {
                    return false;
                }
            }

            if (_roadTrees.Any(t => (t - c).sqrMagnitude < LampFromTree * LampFromTree))
            {
                return false;
            }

            // Off a kerb, no house and no mass of the works or the cathedral within LampFromWall m (§31: a corner lamp of Deaf
            // Boiler Yard stood 1.5 m from the works' wall).
            var room = Rect(c.x - LampFromWall, c.y - LampFromWall, c.x + LampFromWall, c.y + LampFromWall);
            return kerb || !(_houses.Any(h => Overlap(h, room)) || _layout.boxes.Any(b => Overlap(Rect(b.x0, b.y0, b.x1, b.y1), room)));
        }

        /// <summary>
        /// A square's lamps in its corners (§4), each only where no lamp stands within <see cref="LampFromLamp"/> m and no
        /// house within <see cref="LampFromWall"/> m, tried 1 m further in along the diagonal when a house is near (§31).
        /// </summary>
        private static void SquareLamps()
        {
            foreach (var id in new[] { "drowsy", "black_puddle", "deaf_boiler" })
            {
                var q = _layout.squares.FirstOrDefault(s => s.id == id);
                if (q == null)
                {
                    continue; // the square lies in another district of the island
                }

                foreach (var (px, py, dx, dy) in new[] { (q.x0 + 1f, q.y0 + 1f, 1, 1), (q.x1 - 2f, q.y0 + 1f, -1, 1), (q.x0 + 1f, q.y1 - 2f, 1, -1), (q.x1 - 2f, q.y1 - 2f, -1, -1) })
                {
                    for (var k = 0; k < 3; k++)
                    {
                        var (x, y) = (px + (dx * k), py + (dy * k));
                        var c = new Vector2(x + 0.5f, y + 0.5f);
                        // §31: a corner lamp also keeps 1.5 m from anything solid on the square, such as the Notice Board
                        // (authoring default; one stood behind the board on the Black Puddle).
                        if (!LampRoom(c, null, 0, false, false) ||
                            _taken.Any(t => t.Item2 == Use.Solid && Overlap(t.Item1, Rect(c.x - 1.5f, c.y - 1.5f, c.x + 1.5f, c.y + 1.5f))))
                        {
                            continue;
                        }

                        if (Prop("LampPost", Facing.N, x, y) != null)
                        {
                            _lamps.Add((c, null, 0));
                            break;
                        }
                    }
                }
            }

            Debug.Log($"[VeilmouthCity] lamps (§31): {_lamps.Count}, {_lampsDropped} bay stations left without a lamp; {_roadTrees.Count} road trees.");
        }

        // ------------------------------------------------------------------------------------------- street trees

        /// <summary>
        /// The street trees (§25.3, §26, §27.10, §31): the owner's mature tree in square pits on the carriageway, flush on
        /// the kerb, on the planted kerbs (<see cref="PlantedKerbs"/>), two in each of a run's bays, a quarter of the bay
        /// from its lamp station either way, so a tree stands midway between lamps and none within a crown of one.
        /// </summary>
        private static void StreetTreesC4()
        {
            var planted = 0;
            foreach (var s in _layout.streets.Where(s => s.walk0 > 0f || s.walk1 > 0f))
            {
                var (near, far) = _planted[s];
                if (near)
                {
                    planted += TreeRow(s.AlongX, s.c0 + s.walk0, 1, (int)s.a0, (int)s.a1);
                }

                if (far)
                {
                    planted += TreeRow(s.AlongX, s.c1 - s.walk1, -1, (int)s.a0, (int)s.a1);
                }
            }

            Debug.Log($"[VeilmouthCity] street trees (§26, §27.10, §31): {planted} of the mature tree, each in its pit by the kerb; {_planted.Values.Count(p => p.Near != p.Far)} streets planted on one side.");
        }
    }
}
