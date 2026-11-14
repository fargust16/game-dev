"""Veilmouth on the island (ISLAND_CITY_REBUILD_PLAN.md, stage IC2): the layout of the rebuilt city.

A district is a frame: a rectangle with its own turn, laid out in its own local metres exactly as C5 lays out the whole
city along the axes (VEILMOUTH_CITY_PLAN.md §29). u runs along the frame's first side and v across it, as px and py
do in the city plan, so "N" is the frame's low-v side; for three districts that is the waterside. The frames, their
turns and their ground come from the approved concept (tools/slice/island/concept_b.py); what stands in each block is
set here, and the houses, passages and yards are filled by C5's own rules: this file uses veilmouth_c5_plan's Block
as it is and changes nothing in it. Every named place of C5 keeps its id, since the game finds places by id.

Writes
* the layout the Unity builder will read: unity/Assets/Data/Slice/Map/VeilmouthIslandLayout.json;
* the plan drawn over the island: unity/Assets/Plans/VEILMOUTH_ISLAND_PLAN.png.

Plan metres as everywhere: px east and py south from the area's north-west corner; world X = px - 500, Z = 500 - py.
A frame's point (u, v) is at origin + u * (cos a, sin a) + v * (-sin a, cos a) in plan metres; in the scene the frame
is a GameObject at its origin with a yaw of a degrees, its local x = u and its local z = -v.
Every size is an authoring default. The run fails when a block breaks one of C5's rules.

Run: python tools/slice/veilmouth_island_plan.py
"""

import json
import math
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE / "island"))

import veilmouth_c5_plan as c5   # noqa: E402  C5's rules; importing it plans C5 in memory and writes nothing
import islandgen as g            # noqa: E402
from concept_b import CONCEPT    # noqa: E402

ROOT = HERE.parents[1]
LAYOUT = ROOT / "unity/Assets/Data/Slice/Map/VeilmouthIslandLayout.json"
PLAN_PNG = ROOT / "unity/Assets/Plans/VEILMOUTH_ISLAND_PLAN.png"
KIND = {"emb": "embankment"}
ERRORS, NOTES = [], []


def B(bid, wealth, fam, on=(), stand=(), kind=None, limit=None):
    """A block's content: the family of houses on each side (None for a side of single buildings or an open one),
    its single buildings on a side (facing, metres from the block's low corner, kit key, id, name) or standing free
    (kit key, facing, front and low end from the block's corner, id, name), and a side's rank where it is not its
    street's own."""
    return dict(id=bid, wealth=wealth, fam=fam, on=list(on), stand=list(stand), kind=kind or {}, limit=limit or {})


def all_sides(family):
    return dict(N=family, E=family, S=family, W=family)


# What stands in each district. Streets: (id, name) for the four border streets and the inner ones. A cell with no
# block is a square, a garden or a works yard, named under "squares" or built of "boxes".
SPECS = {
    "The Penny": dict(
        id="penny", district="THE PENNY",
        streets=dict(v0=("embankment", "Powder Embankment"), v1=("widows", "Widows' Row"), u0=("thimble", "Thimble Street"),
                     u1=("nettle", "Nettle Street"), sx=[("lenten", "Lenten Street"), ("smoked", "Smoked Street")],
                     sy=[("main_e", "Salt Gullet")]),
        blocks={
            (0, 0): B("PA", "rich", dict(N="town", E="town", S="shop", W="town")),
            # The Town Hall faces the Penny across Salt Gullet; the clock tower rises behind it, 1.5 m off its back
            # wall, its door on the yard (as C5 has them); 2 m clear of the yard's benches, which its cornice overhangs.
            (1, 0): B("PB", "rich", dict(N="shop_rich", E="town", S=None, W="town"), kind=dict(S="penny"),
                      limit=dict(E=(0, 26), W=(0, 26)),   # the side rows end a passage short of the Town Hall's back
                      on=[("S", 11, "Civic_rich", "town_hall", "The Town Hall")],
                      stand=[("Tower6_white", "N", 23.5, 21.5, "clock_tower", "The clock tower")]),
            # Its yard is open to Nettle Street, the town's last street on that side, behind a yard wall.
            (2, 0): B("PC", "middling", dict(N="town", E=None, S="shop", W="town")),
            (0, 1): B("PD", "rich", dict(N="shop", E=None, S="town", W="town"), kind=dict(E="penny"),
                      limit=dict(N=(0, 21), S=(0, 21)),
                      on=[("E", 11, "Civic_stucco", "assize", "The Assize and the Wardens' station")]),
            (2, 1): B("PF", "middling", dict(N="shop", E="town", S="town", W=None), kind=dict(W="penny"),
                      limit=dict(N=(19, 40), S=(19, 40)),
                      on=[("W", 11, "Civic_white", "guild_hall", "The Guild Hall")]),
        },
        plaza=(1, 1),
        squares=[],
    ),
    "Sleepy Quarter": dict(
        id="sleepy", district="SLEEPY QUARTER",
        streets=dict(v0=("sleepers", "Sleepers' Embankment"), v1=("cold_shoulder", "Cold Shoulder"), u0=("spade_lane", "Spade Lane"),
                     u1=("button", "Button Lane"), sx=[("last_turning", "Last Turning"), ("crows_walk", "Crows' Walk")],
                     sy=[("main_w", "Long Tongue")]),
        blocks={
            # A rich family's villa on the embankment at the quarter's corner, a dwelling like any house.
            (0, 0): B("SA", "rich", all_sides("town"), on=[("N", 0, "Villa_stucco", "villa", "The embankment villa")]),
            (1, 0): B("SB", "middling", all_sides("town")),
            # Two villas in their gardens at the quarter's quiet end: fewer houses, more green (owner, 2026-11-14:
            # never more buildings at the picture's cost).
            (2, 0): B("SC", "rich", all_sides(None), on=[("N", 0, "Villa_stucco", "villa_east", ""), ("N", 30, "Villa_stucco", "villa_end", "")]),
            (0, 1): B("SD", "rich", dict(N=None, E="town", S="town", W="town"), limit=dict(E=(19, 40), W=(19, 40)),
                      on=[("N", 5, "Civic_white", "governor", "The Governor's House")]),
            # Its yard is open to Cold Shoulder, under the town wall, behind a yard wall.
            (1, 1): B("SE", "middling", dict(N="town", E="town", S=None, W="town")),
        },
        squares=[dict(cell=(2, 1), id="quiet", name="Quiet Whistle Garden", surface="grass")],
    ),
    "Lower Veil": dict(
        id="lower", district="LOWER VEIL",
        streets=dict(v0=("tar_row", "Tar Row"), v1=("eel_row", "Eel Row"), u0=("hook", "Hook Street"), u1=("gull", "Gull Lane"),
                     sx=[("wet_hem", "Wet Hem")], sy=[("draught", "Draught Lane")]),
        blocks={
            (0, 0): B("LA", "poor", dict(N="warehouse", E="tenement", S="tenement", W="tenement")),
            (1, 0): B("LB", "middling", dict(N=None, E="tenement", S="tenement", W="tenement"), limit=dict(E=(19, 40), W=(19, 40)),
                      on=[("N", 5, "Civic_white", "customs", "The Customs House")]),
            (0, 1): B("LC", "poor", all_sides("tenement"),
                      on=[("S", 17, "Tenement8_poor_detached", "missing", "The missing person's tenement")]),
        },
        # The Black Puddle: the square's north part is open ground; the tavern and the apothecary stand on its south
        # side and face it.
        split={(1, 1): dict(square=dict(id="black_puddle", name="Black Puddle", surface="square", depth=25),
                            block=B("LD", "poor", all_sides(None), kind=dict(N="square"),
                                    on=[("N", 0, "Tavern_brick", "soot_bell", "The Soot & Bell"),
                                        ("N", 19, "ShopTown8_dark_detached", "harrow", "Harrow & Tincture")]))},
        squares=[dict(rect=(0, 0, None, 15), id="quay", name="Fishlip Quay", surface="setts", label=True),
                 dict(rect=(50, -22, 57, 0), id="tipsy", name="Tipsy Pier", surface="setts")],
        candidates=[dict(name="Ritual Site Candidate 1 - Black Puddle", square="black_puddle", size=26)],
        spawn=("black_puddle", 0.5, -0.35),
    ),
    "The Reek": dict(
        id="reek", district="THE REEK",
        streets=dict(v0=("ash_bank", "Ash Bank"), v1=("coughing", "Coughing Lane"), u0=("works_s", "Furnace Road"),
                     u1=("brine", "Brine Lane"), sx=[("works_x", "Clinker Road")], sy=[("rats_choir", "Rats' Choir")]),
        sx_from={0: 1},   # Clinker Road runs between the second row's blocks only: the first row is one works yard
        blocks={
            (1, 1): B("RD", "poor", all_sides("low")),
        },
        # The first row is one works block: sheds round the Deaf Boiler Yard, open to the bank.
        boxes=[
            ("ES2_nw", "Works ES2 - boiler house", 14, 14, 43, 34, 14.0), ("ES2_ne", "Works ES2 - coal store", 84, 14, 113, 34, 10.0),
            ("ES2_w", "Works ES2 - west shed", 14, 38, 43, 54, 12.0), ("ES2_e", "Works ES2 - east shed", 84, 38, 113, 54, 12.0),
            ("ES2_s", "Works ES2 - south shed", 43, 47, 84, 54, 9.0),
            ("ES1_n", "Works ES1 - north shed", 16, 66, 57, 78, 13.0), ("ES1_s", "Works ES1 - south shed", 16, 86, 57, 97, 9.0),
        ],
        squares=[dict(rect=(43, 21, 84, 47), id="deaf_boiler", name="Deaf Boiler Yard", surface="cobbles", label=True)],
        candidates=[dict(name="Ritual Site Candidate 2 - Deaf Boiler Yard", square="deaf_boiler", size=26)],
    ),
    "Parsons' End": dict(
        id="parsons", district="PARSONS' END",
        streets=dict(v0=("bishops_walk", "Bishop's Walk"), v1=("bellman", "Bellman's Row"), u0=("candle", "Candle Row"),
                     u1=("mourners", "Mourners' Lane"), sx=[("tallow", "Tallow Lane")], sy=[("widows_end", "Parsons' Row")]),
        blocks={
            (1, 0): B("CEM", "cemetery", all_sides(None), on=[("S", 14, "Chapel", "chapel", "The old chapel")]),
            # The faction's house and the Brothers' House: two ranges flush at a corner round a garth, the bell
            # tower in it.
            (0, 1): B("CLOSE", "close", all_sides(None),
                      on=[("N", 3, "Villa_stucco", "faction_house", "The faction's house"),
                          ("N", 25, "Civic_stucco", "monastery_n", "The Brothers' House - north range"),
                          ("E", 0, "Civic_stucco", "monastery_w", "The Brothers' House - east range")],
                      stand=[("Tower5_dark", "S", 25, 46, "bell_tower", "The Brothers' bell tower")]),
            (1, 1): B("NW1", "poor", all_sides("low")),
        },
        # The cathedral, stone masses until its model comes, its length along the close and its tower to the west.
        boxes=[
            ("west_front", "The cathedral - tower", 12, 28, 28, 48, 44.0, "cathedral"), ("nave", "The cathedral - nave", 28, 26, 64, 50, 22.0, "cathedral"),
            ("transept", "The cathedral - transept", 40, 12, 56, 64, 20.0, "cathedral"), ("apse", "The cathedral - apse", 64, 30, 70, 46, 16.0, "cathedral"),
        ],
        squares=[dict(cell=(0, 0), id="close", name="", surface="grass"),
                 dict(rect=(10, 10, 38, 26), id="drowsy", name="Drowsy Steps", surface="square", label=True),
                 dict(rect=(58, 50, 80, 66), id="bishops", name="Bishop's Garden", surface="grass", label=True),
                 dict(rect=(90, 10, 130, 50), id="cemetery", name="Last Turning Cemetery", surface="grass", label=True),
                 dict(rect=(90, 50, 130, 66), id="chapel_court", name="", surface="cobbles"),
                 dict(rect=(35, 91, 65, 106), id="garth", name="", surface="grass")],
        candidates=[dict(name="Ritual Site Candidate 3 - Old Chapel", square="chapel_court", size=14)],
    ),
}


def sections(kind, low_water=False):
    """A street's (low pavement, carriageway, high pavement). C5's embankment has its promenade on the high side; one
    along a frame's low side has the water there, so it is turned over."""
    w0, road, w1 = c5.SECTIONS[kind]
    return (w1, road, w0) if (kind == "embankment" and low_water) else (w0, road, w1)


class Frame:
    def __init__(self, D, spec):
        self.D, self.spec, self.id = D, spec, spec["id"]
        self.U, self.V = D.U, D.V
        self.out = dict(id=self.id, name=D.name, district=spec["district"], origin=dict(x=D.origin[0], y=D.origin[1]),
                        angle=round(D.angle, 4), U=D.U, V=D.V,
                        streets=[], squares=[], blocks=[], buildings=[], passages=[], boxes=[], plaza=None, candidates=[])
        self.shapes = []   # (quad in plan metres, colour key, height) for the drawing

    # ---- the streets

    def street_kinds(self):
        D = self.D
        k = lambda c: KIND.get(c, c) if c else None
        return dict(u0=k(D.border[0]), u1=k(D.border[1]), v0=k(D.border[2]), v1=k(D.border[3]),
                    sx=[k(c) for c in D.sx], sy=[k(c) for c in D.sy])

    def streets(self):
        D, names, kinds = self.D, self.spec["streets"], self.street_kinds()
        sec = lambda kind, low=False: sections(kind, low) if kind else (0.0, 0.0, 0.0)
        s_u0, s_u1 = sec(kinds["u0"]), sec(kinds["u1"])
        s_v0, s_v1 = sec(kinds["v0"], True), sec(kinds["v1"])
        ax0, ax1 = s_u0[0], D.U - s_u1[2]          # along u: from the low border's carriageway to the high border's
        ay0, ay1 = s_v0[0], D.V - s_v1[2]
        rows_from = self.spec.get("sx_from", {})

        def add(key, kind, axis, c0, a0, a1, low_water=False):
            if not kind:
                return
            w0, road, w1 = sections(kind, low_water)
            sid, name = key
            trees = "wide" if (kind == "embankment" and low_water) else c5.TREES.get(kind, "none")
            self.out["streets"].append(dict(id=sid, name=name, kind=kind, axis=axis, a0=a0, a1=a1, c0=c0, c1=c0 + w0 + road + w1,
                                            walk0=w0, walk1=w1, surface="cobbles" if kind == "lane" else "setts", trees=trees))

        add(names["v0"], kinds["v0"], "x", 0.0, ax0, ax1, True)
        for j, kind in enumerate(kinds["sy"]):
            add(names["sy"][j], kind, "x", D.v0[j] + D.rows[j], ax0, ax1)
        if kinds["v1"]:
            add(names["v1"], kinds["v1"], "x", D.V - sum(s_v1), ax0, ax1)
        add(names["u0"], kinds["u0"], "y", 0.0, ay0, ay1)
        for i, kind in enumerate(kinds["sx"]):
            a0 = ay0
            if i in rows_from:
                j = rows_from[i]
                w0, road, _ = sections(kinds["sy"][j - 1])
                a0 = D.v0[j - 1] + D.rows[j - 1] + w0 + road / 2   # from the middle of the row's lane, as C5's Bellows Lane
            add(names["sx"][i], kind, "y", D.u0[i] + D.cols[i], a0, ay1)
        if kinds["u1"]:
            add(names["u1"], kinds["u1"], "y", D.U - sum(s_u1), ay0, ay1)

    # ---- the blocks

    def cell(self, i, j):
        D = self.D
        return (D.u0[i], D.v0[j], D.u0[i] + D.cols[i], D.v0[j] + D.rows[j])

    def side_kinds(self, i, j):
        D, kinds = self.D, self.street_kinds()
        return dict(N=kinds["v0"] if j == 0 else kinds["sy"][j - 1], S=kinds["sy"][j] if j < len(D.rows) - 1 else kinds["v1"],
                    W=kinds["u0"] if i == 0 else kinds["sx"][i - 1], E=kinds["sx"][i] if i < len(D.cols) - 1 else kinds["u1"])

    def block(self, rect, sides, b):
        x0, y0, x1, y1 = rect
        edges = {}
        for f in "NESW":
            kind = b["kind"].get(f, sides[f]) or "wall"
            base = x0 if f in "NS" else y0   # a limit is given from the block's low corner along that side
            edges[f] = (kind, b["fam"][f]) + (((base + b["limit"][f][0], base + b["limit"][f][1]),) if f in b["limit"] else ())
        blk = c5.Block(self.id.upper()[:2] + "_" + b["id"] if False else b["id"], rect, b["wealth"], self.spec["district"], edges)
        for facing, off, key, sid, name in b["on"]:
            blk.on(facing, (x0 if facing in "NS" else y0) + off, key, sid, name)
        for key, facing, front_off, a_off, sid, name in b["stand"]:
            front = (y0 if facing in "NS" else x0) + front_off
            a = (x0 if facing in "NS" else y0) + a_off
            blk.stand(key, facing, front, a, sid, name)
        return blk

    def plan(self):
        D, spec, out = self.D, self.spec, self.out
        c5.BLOCKS.clear()
        c5.ERRORS.clear()
        c5.NOTES.clear()
        c5.layout = {"boxes": out["boxes"]}
        self.streets()
        for bx in spec.get("boxes", []):
            bid, name, x0, y0, x1, y1, height = bx[:7]
            out["boxes"].append(dict(id=bid, name=name, x0=x0, y0=y0, x1=x1, y1=y1, height=height,
                                     material=bx[7] if len(bx) > 7 else "works", base=0.0))
        for (i, j), b in spec.get("blocks", {}).items():
            self.block(self.cell(i, j), self.side_kinds(i, j), b)
        for (i, j), s in spec.get("split", {}).items():
            x0, y0, x1, y1 = self.cell(i, j)
            sq = s["square"]
            out["squares"].append(dict(id=sq["id"], name=sq["name"], x0=x0, y0=y0, x1=x1, y1=y0 + sq["depth"], surface=sq["surface"]))
            self.block((x0, y0 + sq["depth"], x1, y1), self.side_kinds(i, j), s["block"])
        for sq in spec.get("squares", []):
            if "cell" in sq:
                x0, y0, x1, y1 = self.cell(*sq["cell"])
            else:
                x0, y0, x1, y1 = sq["rect"]
                x1 = D.U if x1 is None else x1
            out["squares"].append(dict(id=sq["id"], name=sq["name"] if sq.get("label", True) else "", x0=x0, y0=y0, x1=x1, y1=y1, surface=sq["surface"]))
        if spec.get("plaza"):
            x0, y0, x1, y1 = self.cell(*spec["plaza"])
            half = min(x1 - x0, y1 - y0) / 2
            out["squares"].append(dict(id="penny", name="The Penny", x0=x0, y0=y0, x1=x1, y1=y1, surface="square"))
            out["plaza"] = dict(cx=(x0 + x1) / 2, cy=(y0 + y1) / 2, half=half, ringOuter=21.0, ringInner=13.0, island=13.0, plinth=4.0,
                                statue=12.0, name="Stone Grandad",
                                stalls=dict(radius=9.0, count=12, width=3.0, depth=2.0, name="Bawling Row"))

        for blk in c5.BLOCKS:
            blk.run()
        c5.check()
        for e in c5.ERRORS:
            ERRORS.append("%s: %s" % (D.name, e))
        for n in c5.NOTES:
            NOTES.append("%s: %s" % (D.name, n))

        # What C5's planner writes for each block (veilmouth_c5_plan.py, after its checks), in the frame's metres.
        for blk in c5.BLOCKS:
            for h in blk.houses:
                out["buildings"].append(dict(id="", key=h["key"], facing=h["facing"], front=h["front"], a=h["a"], name="", stoop="plain", block=blk.id))
                self.shapes.append((h["fp"], family_of(h["key"]), c5.FAMILIES[h["family"]][0]))
            for s in blk.placed + [dict(s, fp=c5.footprint(s["facing"], s["front"], s["a"], *c5.SIZES[s["key"]])) for s in blk.free]:
                out["buildings"].append(dict(id=s["id"], key=s["key"], facing=s["facing"], front=s["front"], a=s["a"], name=s["name"], stoop=s["stoop"], block=blk.id))
                self.shapes.append((s["fp"], family_of(s["key"]), 15.0))
            for (e, a0, a1, depth) in blk.gaps:
                out["passages"].append(dict(block=blk.id, facing=e.facing, front=e.front, a0=a0, a1=a1, depth=depth))
            x0, y0, x1, y1 = blk.rect
            inset = dict(N=0, E=0, S=0, W=0)
            for e in blk.edge_list:
                if e.family:
                    inset[e.facing] = max(inset[e.facing], e.depth())
            out["blocks"].append(dict(id=blk.id, wealth=blk.wealth, district=blk.district, x0=x0, y0=y0, x1=x1, y1=y1, notch=None,
                                      yard=dict(x0=x0 + inset["W"], y0=y0 + inset["N"], x1=x1 - inset["E"], y1=y1 - inset["S"])))
        for bx in out["boxes"]:
            self.shapes.append(((bx["x0"], bx["y0"], bx["x1"], bx["y1"]), "church" if bx["material"] == "cathedral" else "works", bx["height"]))

        squares = {s["id"]: s for s in out["squares"]}
        for c in spec.get("candidates", []):
            s = squares[c["square"]]
            out["candidates"].append(dict(name=c["name"], x=(s["x0"] + s["x1"]) / 2, y=(s["y0"] + s["y1"]) / 2, size=c["size"]))
        if "spawn" in spec:
            sid, fu, fv = spec["spawn"]
            s = squares[sid]
            out["spawn"] = dict(x=s["x0"] + (s["x1"] - s["x0"]) * fu, y=s["y0"] + (s["y1"] - s["y0"]) * fv)
        if D.surface:
            a, b, c = D.surface
            out["ground"] = dict(middle=round(a, 3), alongU=round(b, 5), alongV=round(c, 5))
            out["edges"] = [dict(side=e["side"], kind=e["kind"], ax=round(e["a"][0], 1), ay=round(e["a"][1], 1), bx=round(e["b"][0], 1),
                                 by=round(e["b"][1], 1), height=round(e["height"], 2), top=round(e["top"], 2)) for e in D.edges]
        return out

    def quad(self, rect):
        x0, y0, x1, y1 = rect
        return [self.D.world(x0, y0), self.D.world(x1, y0), self.D.world(x1, y1), self.D.world(x0, y1)]


def family_of(key):
    for prefix, fam in (("TenementLow", "low"), ("Tenement", "tenement"), ("Warehouse", "warehouse"), ("ShopTown", "town"),
                        ("Shop", "shop"), ("Terrace", "town"), ("Villa", "villa"), ("Civic", "civic"), ("Tavern", "town"),
                        ("Tower", "church"), ("Chapel", "church"), ("House_Farm", "cottage")):
        if key.startswith(prefix):
            return fam
    return "town"


def hamlet(h):
    """Crooked Hamlet: C5's eight cottages on their lane, in a frame of its own."""
    angle = 225.0   # the concept's 227 deg, taken to the measured turn beside it
    cottages = [dict(key="House_Farm", facing="S", front=0, a=44), dict(key="House_Farm", facing="S", front=0, a=70),
                dict(key="House_Farm", facing="N", front=24, a=36), dict(key="House_Farm", facing="N", front=24, a=66),
                dict(key="House_Farm", facing="S", front=0, a=6), dict(key="House_Farm", facing="N", front=24, a=0),
                dict(key="TenementLow8_poor_detached", facing="N", front=24, a=88),
                dict(key="TenementLow8_poor_detached", facing="S", front=0, a=94)]
    # The frame's middle on the hamlet's place, along Vale Road: its origin on a whole-metre node.
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    origin = (round(h["at"][0] - 52 * c + 12 * s), round(h["at"][1] - 52 * s - 12 * c))
    D = g.District(h["name"], origin, angle, cols=[104], rows=[24], border=(None,) * 4)
    return dict(id="hamlet", name=h["name"], district="", origin=dict(x=D.origin[0], y=D.origin[1]), angle=angle, U=104, V=24,
                streets=[], squares=[], blocks=[], buildings=[], passages=[], boxes=[], plaza=None, candidates=[], cottages=cottages), D


def embankments(C, F):
    """The town's water edge (ISLAND_WORLD_AUDIT.md, stage 2): along each tidal river, the two lines where the town's
    ground is cut down to the channel, as far as they run inside the town's line or a terrace's. The generator cuts
    the channel half a metre beyond the water's width, upright; an embankment's wall stands on that line."""
    np = g.np
    town = C.get("town")
    if not town:
        return []
    polys = [np.array(p["poly"], float) for p in [town] + town.get("terraces", [])]
    inside = lambda x, y: any(g.poly_mask(P, np.array([x]), np.array([y]))[0, 0] for P in polys)
    out = []
    for r in F["rivers"]:
        if r["dry"]:
            continue
        line = r["line"]
        t = np.gradient(line[:, :2], axis=0)
        t = t / np.maximum(np.hypot(t[:, 0], t[:, 1]), 1e-6)[:, None]
        normal = np.column_stack([-t[:, 1], t[:, 0]])
        for sign, side in ((1.0, "right"), (-1.0, "left")):
            edge = line[:, :2] + sign * normal * (line[:, 3:4] / 2 + 0.5)
            run = []
            for x, y in list(edge) + [(None, None)]:
                if x is not None and inside(x, y):
                    run.append(dict(x=round(float(x), 2), y=round(float(y), 2)))
                    continue
                if len(run) >= 3:
                    out.append(dict(name=r["name"], kind=side, line=run))
                run = []
    return out


def main():
    C = CONCEPT
    F = g.build_land(C, lambda s: None)
    g.zones(C, F)
    frames = []
    for D in C["districts"]:
        frames.append(Frame(D, SPECS[D.name]))
    layout = dict(version="I1", concept=C["key"], frames=[f.plan() for f in frames])

    hm, hD = hamlet(C["hamlets"][0])
    layout["frames"].append(hm)
    layout["lighthouse"] = dict(id="one_eye", key="Tower6_white", name="The One-Eye", x=C["points"][0]["at"][0], y=C["points"][0]["at"][1])
    layout["walls"] = [dict(name="Town wall %d" % (k + 1), height=6.0, line=[dict(x=p[0], y=p[1]) for p in w]) for k, w in enumerate(C.get("walls", []))]
    layout["gates"] = [dict(name=p["name"], x=p["at"][0], y=p["at"][1]) for p in C.get("points", []) if p["kind"] == "gate"]
    layout["bridges"] = [dict(name=b["name"], ax=b["a"][0], ay=b["a"][1], bx=b["b"][0], by=b["b"][1], width=b["w"]) for b in C.get("bridges", [])]
    layout["piers"] = [dict(ax=p["a"][0], ay=p["a"][1], bx=p["b"][0], by=p["b"][1], width=p["w"]) for p in C.get("piers", [])]
    layout["roads"] = [dict(name=r["name"], kind=r["cls"], line=[dict(x=round(float(p[0]), 1), y=round(float(p[1]), 1)) for p in r["pts"]]) for r in C.get("roads", [])]
    layout["embankments"] = embankments(C, F)
    layout["pads"] = [dict(name=p["name"], kind=p["kind"], level=p["level"], line=[dict(x=q[0], y=q[1]) for q in p["poly"]]) for p in C.get("pads", [])]

    # ---- counts and checks
    houses = sum(1 for f in layout["frames"] for b in f["buildings"] if not b["id"])
    named = [b["id"] for f in layout["frames"] for b in f["buildings"] if b["id"]] + ["one_eye"]
    sheds = sum(1 for f in layout["frames"] for bx in f["boxes"] if bx["material"] == "works")
    cottages = len(hm["cottages"])
    total = houses + len(named) + sheds + cottages
    want = {"one_eye", "villa", "town_hall", "clock_tower", "governor", "assize", "guild_hall", "soot_bell", "harrow", "customs",
            "missing", "chapel", "faction_house", "monastery_n", "monastery_w", "bell_tower"}
    if not want <= set(named):
        ERRORS.append("C5's named buildings with no place on the island: %s" % sorted(want - set(named)))
    ids = [s["id"] for f in layout["frames"] for s in f["squares"]]
    for sid in ("penny", "black_puddle", "quay", "tipsy", "cemetery", "chapel_court", "close", "bishops", "quiet", "drowsy", "garth", "deaf_boiler"):
        if sid not in ids:
            ERRORS.append("C5's square '%s' has no place on the island" % sid)
    streets = [s["id"] for f in layout["frames"] for s in f["streets"]]
    if len(streets) != len(set(streets)):
        ERRORS.append("two streets share an id: %s" % sorted(s for s in set(streets) if streets.count(s) > 1))
    for sid in ("main_e", "draught", "rats_choir"):
        if sid not in streets:
            ERRORS.append("the game's street '%s' has no place on the island" % sid)
    if not 135 <= total <= 150:
        ERRORS.append("%d buildings: the owner asked for 135 to 150" % total)
    for f in layout["frames"]:
        o, a = f["origin"], math.radians(f["angle"])
        vec = (math.cos(a), math.sin(a))
        whole = any(abs(vec[0] * k - round(vec[0] * k)) < 1e-3 and abs(vec[1] * k - round(vec[1] * k)) < 1e-3 for k in (1.0, math.sqrt(2), math.sqrt(5)))
        if not whole or o["x"] != int(o["x"]) or o["y"] != int(o["y"]):
            ERRORS.append("%s: its frame is not on a whole-metre node at one of the measured turns" % f["name"])

    LAYOUT.parent.mkdir(parents=True, exist_ok=True)
    LAYOUT.write_text(json.dumps(layout, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    draw(C, F, frames, hm, hD, layout)

    for e in ERRORS:
        print("  BROKEN: " + e)
    for n in NOTES:
        print("  note: " + n)
    for f in layout["frames"]:
        gr = f.get("ground")
        print("  %-16s %3d x %3d m at %5.1f deg: %2d streets, %2d blocks, %3d buildings, %2d passages%s" % (
            f["name"], f["U"], f["V"], f["angle"], len(f["streets"]), len(f["blocks"]), len(f["buildings"]) + len(f.get("cottages", [])),
            len(f["passages"]), "; ground %.1f m, %.1f %% and %.1f %%" % (gr["middle"], abs(gr["alongU"]) * 100, abs(gr["alongV"]) * 100) if gr else ""))
    print("VEILMOUTH ON THE ISLAND: %d row houses, %d single buildings, %d works sheds, %d cottages: %d buildings; %d streets." % (
        houses, len(named), sheds, cottages, total, len(streets)))
    return 1 if ERRORS else 0


def draw(C, F, frames, hm, hD, layout):
    """The plan over the island, 2 px to the metre round the town."""
    x0, y0, w, h = 200.0, 100.0, 760.0, 860.0
    cv = g.Canvas(2.0, x0, y0, w, h)
    g.terrain_image(F, C, cv, zones=True)
    col = g.COL
    for r in C.get("roads", []):
        line = r.get("line")
        if line is not None:
            cv.line(line, g.road_width(r["cls"]), col["road"] if r["cls"] != "track" else col["track"])
    for f in frames:
        D, out = f.D, f.out
        for s in out["streets"]:
            lo, hi = s["c0"], s["c1"]
            rect = (s["a0"], lo, s["a1"], hi) if s["axis"] == "x" else (lo, s["a0"], hi, s["a1"])
            cv.poly(f.quad(rect), col["pave"])
        for s in out["streets"]:
            lo, hi = s["c0"] + s["walk0"], s["c1"] - s["walk1"]
            rect = (s["a0"], lo, s["a1"], hi) if s["axis"] == "x" else (lo, s["a0"], hi, s["a1"])
            cv.poly(f.quad(rect), col["carriage"])
        tone = dict(grass="park", square="square", setts="carriage", cobbles="yard_mid")
        for s in out["squares"]:
            cv.poly(f.quad((s["x0"], s["y0"], s["x1"], s["y1"])), col[tone[s["surface"]]])
        for b in out["blocks"]:
            y = b["yard"]
            if y["x1"] > y["x0"] and y["y1"] > y["y0"]:
                cv.poly(f.quad((y["x0"], y["y0"], y["x1"], y["y1"])), col[{"rich": "yard_rich", "poor": "yard_poor"}.get(b["wealth"], "yard_mid")])
        if out["plaza"]:
            p = out["plaza"]
            cv.ring(D.world(p["cx"], p["cy"]), (p["ringOuter"] + p["ringInner"]) / 2, col["carriage"], p["ringOuter"] - p["ringInner"])
        for rect, fam, _ in f.shapes:
            cv.poly(f.quad(rect), col.get(fam, col["town"]))
        for e in D.edges:
            if e["kind"] != "bank":
                cv.line([e["a"], e["b"]], 2.0, (40, 40, 44) if e["kind"] == "quay" else (120, 60, 40))
    for c in hm["cottages"]:
        w_, d_ = c5.SIZES[c["key"]]
        fp = c5.footprint(c["facing"], c["front"], c["a"], w_, d_)
        cv.poly([hD.world(fp[0], fp[1]), hD.world(fp[2], fp[1]), hD.world(fp[2], fp[3]), hD.world(fp[0], fp[3])], col["cottage"])
    for wl in C.get("walls", []):
        cv.line(wl, 2.4, col["wall"])
    for b in C.get("bridges", []):
        cv.line([b["a"], b["b"]], b["w"], col["bridge"])
    for p in C.get("points", []):
        cv.ring(p["at"], 5.0, col["mark"], 1.6)
    g.write_png_rgb(str(PLAN_PNG), cv.img)


if __name__ == "__main__":
    sys.exit(main())
