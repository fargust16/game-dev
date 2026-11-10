"""Veilmouth's city plan C5 (VEILMOUTH_CITY_PLAN.md §29): the one source of its layout.

C5 cuts C4's map into blocks (§29.1): streets with a pavement on both sides frame rectangular blocks, the houses stand
flush on the pavement round small inner yards, and the Penny is a small square with the market round its statue. This
file turns the blocks into houses, passages and yards by the rules of §29.2 and writes

* the layout the Unity builder reads: unity/Assets/Data/Slice/Map/VeilmouthC5Layout.json;
* the planner's plan with its notes: unity/Assets/Plans/VEILMOUTH_CITY_PLAN_C5.svg;
* the player's map without them: unity/Assets/Data/Slice/Map/VeilmouthC5Plan.xml (CityMapElement's subset);
* a preview PNG, when a path is given: --png <path>.

Plan coordinates as everywhere in the city plan: px runs east and py south from the area's north-west corner, 1 m each,
so world X = px - 500 and world Z = 500 - py; the area is 1000 x 1000 m. Every size here is an authoring default unless
its comment says measured. The run fails when a block breaks a rule: houses that overlap, a gap between two buildings on
one line that is neither 0 nor a passage of at least 4 m (R5), an open end with nothing behind it, or a yard with no way in.

Run: python tools/slice/veilmouth_c5_plan.py [--png path] [--names]
"""

import json
import math
import random
import re
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "unity/Assets/Data/Slice/Map/VeilmouthC5Layout.json"
PLAN_SVG = ROOT / "unity/Assets/Plans/VEILMOUTH_CITY_PLAN_C5.svg"
PLAYER_SVG = ROOT / "unity/Assets/Data/Slice/Map/VeilmouthC5Plan.xml"

# ----------------------------------------------------------------------------------------------- street sections

# §29.2: (pavement on the low side, carriageway, pavement on the high side) in metres. Every road has its pavements.
SECTIONS = {
    "main": (6.0, 10.0, 6.0),
    "side": (6.0, 7.0, 4.0),        # the 6 m pavement, on the north, carries the one row of trees
    "street": (3.0, 6.0, 3.0),
    "lane": (2.5, 5.0, 2.5),
    "quay": (3.0, 9.0, 3.0),
    "embankment": (3.0, 7.0, 6.0),  # 3 m before the houses, the promenade of 6 m by the river
    "works": (2.5, 9.0, 2.5),
}
TREES = {"main": "both", "side": "wide", "embankment": "high"}

layout = {
    "version": "C5",
    "streets": [], "squares": [], "rows": [], "plots": [], "buildings": [], "boxes": [], "water": [], "walls": [],
    "towers": [], "bridges": [], "slants": [], "districts": [], "labels": [], "candidates": [], "zones": [],
    "blocks": [], "passages": [],
}


def carriage(c0, kind):
    """A street's carriageway across it, from the street's low edge c0."""
    w0, road, _ = SECTIONS[kind]
    return c0 + w0, c0 + w0 + road


def mid(span):
    return (span[0] + span[1]) / 2


def street(sid, name, axis, a0, a1, c0, kind, surface="setts"):
    """A street along x (axis "x", from a0 to a1, its low edge at py c0) or along y. A street that meets another runs to
    the other's far carriageway edge, so the carriageways join and only pavements are cut (§29.1 item 2)."""
    w0, road, w1 = SECTIONS[kind]
    layout["streets"].append({
        "id": sid, "name": name, "kind": kind, "axis": axis, "a0": a0, "a1": a1, "c0": c0, "c1": c0 + w0 + road + w1,
        "walk0": w0, "walk1": w1, "surface": surface, "trees": TREES.get(kind, "none"),
    })


def square(sid, name, x0, y0, x1, y1, surface="square", label=True):
    layout["squares"].append({"id": sid, "name": name if label else "", "x0": x0, "y0": y0, "x1": x1, "y1": y1, "surface": surface})


def box(bid, name, x0, y0, x1, y1, height, material="works", base=0.0):
    layout["boxes"].append({"id": bid, "name": name, "x0": x0, "y0": y0, "x1": x1, "y1": y1, "height": height,
                            "material": material, "base": base})


def district(name, x0, y0, x1, y1, colour, at=None):
    d = {"name": name, "x0": x0, "y0": y0, "x1": x1, "y1": y1, "colour": colour}
    if at:
        d["lx"], d["ly"] = at
    layout["districts"].append(d)


# ----------------------------------------------------------------------------------------------- the land and the water

# C4's map stays (§29): the river round the west and the south into the sea, one water plane at 24 m, the walls.
C = (540.0, 440.0)   # the Penny's centre, the statue's foot
HALF = 26.0          # the Penny's frame: its fronts 26 m from the centre, 52 m across (§29.2)
ISLAND, RING = 13.0, 21.0   # the island's edge and the ring carriageway's outer edge: a ring of 8 m
WALL_W, WALL_N = 418, 258
X_IN = WALL_W + 2    # the city's inside, west
APRON = 663          # the quay's apron, from Tar Row's east pavement to the quay wall
COAST = 680          # the city's quay wall; the sea is east of it from py 250 to 710
COAST_OPEN = 700     # the natural coast north and south of the city
RIVER_W = (380, 0, 416, 586)
RIVER_S = (380, 550, COAST + 20, 586)
layout["water"] += [
    {"name": "River Veil", "kind": "river", "x0": RIVER_W[0], "y0": RIVER_W[1], "x1": RIVER_W[2], "y1": RIVER_W[3]},
    {"name": "", "kind": "river", "x0": RIVER_S[0], "y0": RIVER_S[1], "x1": RIVER_S[2], "y1": RIVER_S[3]},
    {"name": "The sea", "kind": "sea", "x0": COAST, "y0": 250, "x1": 1000, "y1": 710},
    {"name": "", "kind": "sea", "x0": COAST_OPEN, "y0": 0, "x1": 1000, "y1": 1000},
]

GATE = (431, 449)
layout["walls"] += [
    {"name": "Town wall (west)", "axis": "y", "t0": WALL_W, "t1": WALL_W + 2, "a0": WALL_N, "a1": RIVER_S[1], "height": 6.0,
     "gaps": [{"a": GATE[0], "b": GATE[1]}]},
    {"name": "Town wall (north)", "axis": "x", "t0": WALL_N, "t1": WALL_N + 2, "a0": WALL_W, "a1": COAST, "height": 6.0, "gaps": []},
]
layout["towers"] += [
    {"name": "Creaking Gate - north tower", "facing": "W", "front": WALL_W - 3, "a": GATE[0] - 5},
    {"name": "Creaking Gate - south tower", "facing": "W", "front": WALL_W - 3, "a": GATE[1]},
    {"name": "Wall tower - north-west", "facing": "N", "front": WALL_N - 3, "a": WALL_W - 3},
    {"name": "Wall tower - cemetery", "facing": "N", "front": WALL_N - 3, "a": 486},
    {"name": "Wall tower - monastery", "facing": "N", "front": WALL_N - 3, "a": 600},
    {"name": "Wall tower - north-east", "facing": "N", "front": WALL_N - 3, "a": COAST - 5},
]
layout["bridges"] += [
    {"name": "Last Farthing Bridge", "axis": "x", "x0": RIVER_W[0] - 2, "y0": 433, "x1": WALL_W, "y1": 447},
    {"name": "Grubby Bridge", "axis": "y", "x0": 532, "y0": RIVER_S[1], "x1": 548, "y1": RIVER_S[3] + 2},
]
WORKS = (X_IN, RIVER_S[3], COAST, 702)
layout["walls"] += [
    {"name": "Works wall (west)", "axis": "y", "t0": WORKS[0] - 2, "t1": WORKS[0], "a0": RIVER_S[3], "a1": WORKS[3] + 2, "height": 4.0, "gaps": []},
    {"name": "Works wall (south)", "axis": "x", "t0": WORKS[3], "t1": WORKS[3] + 2, "a0": WORKS[0] - 2, "a1": COAST, "height": 4.0, "gaps": []},
]
layout["roads"] = [{"name": "West Road", "x0": 0, "y0": 434, "x1": RIVER_W[0] - 2, "y1": 446}]

# ----------------------------------------------------------------------------------------------- streets

# The low edges of the streets (§29.2). A lane runs inside the west and north walls so no block stands against a wall.
CROWS, COLD = WALL_N + 2, X_IN          # Crows' Walk under the north wall, Cold Shoulder under the west wall
SPADE, WIDOWS, MAIN, BUTTON, EMBANK = 316, 367, 429, 491, 534
THIMBLE, LENTEN, NETTLE, TAR = 472, 529, 596, 648
crows, cold, spade = carriage(CROWS, "lane"), carriage(COLD, "street"), carriage(SPADE, "lane")
widows, main, button = carriage(WIDOWS, "side"), carriage(MAIN, "main"), carriage(BUTTON, "lane")
emb, thimble, lenten = carriage(EMBANK, "embankment"), carriage(THIMBLE, "street"), carriage(LENTEN, "main")
nettle, tar = carriage(NETTLE, "street"), carriage(TAR, "quay")

street("crows_walk", "Crows' Walk", "x", cold[0], tar[1], CROWS, "lane", "cobbles")
street("cold_shoulder", "Cold Shoulder", "y", crows[0], emb[1], COLD, "street", "cobbles")   # 12 m, so the blocks are 40
street("spade_lane", "Spade Lane", "x", cold[0], thimble[1], SPADE, "lane", "cobbles")
street("last_turning", "Last Turning", "y", crows[0], mid(widows), THIMBLE, "street")
street("thimble", "Thimble Street", "y", mid(widows), emb[1], THIMBLE, "street")
street("widows", "Widows' Row", "x", cold[0], tar[1], WIDOWS, "side")
street("main_w", "Long Tongue", "x", WALL_W, C[0] - RING + 2, MAIN, "main")    # through the gate to the ring
street("main_e", "Salt Gullet", "x", C[0] + RING - 2, tar[1], MAIN, "main")
street("lenten", "Lenten Street", "y", widows[0], C[1] - RING + 2, LENTEN, "main")
street("smoked", "Smoked Street", "y", C[1] + RING - 2, emb[1], LENTEN, "main")
street("nettle", "Nettle Street", "y", crows[0], button[1], NETTLE, "street")
street("button", "Button Lane", "x", cold[0], C[0], BUTTON, "lane", "cobbles")
street("draught", "Draught Lane", "x", C[0], tar[1], BUTTON, "lane", "cobbles")
street("tar_row", "Tar Row", "y", crows[0], emb[1], TAR, "quay")
street("embankment", "Powder Embankment", "x", cold[0], tar[1], EMBANK, "embankment")

# The works over the river: blocks on the works' roads, lanes inside its walls, by the river and by the sea.
ASH, CINDER, CLINKER, COUGH, BRINE, FURNACE, BELLOWS, KETTLE = 588, X_IN, 638, 692, 670, 533, 476, 603
ash, cinder, clinker = carriage(ASH, "lane"), carriage(CINDER, "lane"), carriage(CLINKER, "works")
cough, brine = carriage(COUGH, "lane"), carriage(BRINE, "lane")
street("ash_bank", "Ash Bank", "x", cinder[0], brine[1], ASH, "lane", "cobbles")
street("cinder", "Cinder Lane", "y", ash[0], cough[1], CINDER, "lane", "cobbles")
street("works_s", "Furnace Road", "y", RIVER_S[3] + 2, cough[1], FURNACE, "works")
street("works_x", "Clinker Road", "x", cinder[0], brine[1], CLINKER, "works")
street("coughing", "Coughing Lane", "x", cinder[0], brine[1], COUGH, "lane", "cobbles")
street("brine", "Brine Lane", "y", ash[0], cough[1], BRINE, "lane", "cobbles")
street("bellows", "Bellows Lane", "y", ash[0], mid(clinker), BELLOWS, "lane", "cobbles")
street("rats_choir", "Rats' Choir", "y", mid(clinker), cough[1], BELLOWS, "lane", "cobbles")
street("kettle", "Kettle Lane", "y", ash[0], cough[1], KETTLE, "lane", "cobbles")

# ----------------------------------------------------------------------------------------------- squares

square("penny", "The Penny", C[0] - HALF, C[1] - HALF, C[0] + HALF, C[1] + HALF)
layout["plaza"] = {"cx": C[0], "cy": C[1], "half": HALF, "ringOuter": RING, "ringInner": ISLAND, "island": ISLAND,
                   "plinth": 4.0, "statue": 12.0, "name": "Stone Grandad",
                   # The market round the statue, the stalls facing the road (§29.1 item 5): Bawling Row.
                   "stalls": {"radius": 9.0, "count": 12, "width": 3.0, "depth": 2.0, "name": "Bawling Row"}}
square("black_puddle", "Black Puddle", NETTLE, BUTTON + 10, TAR, EMBANK)
square("quay", "Fishlip Quay", APRON, CROWS, COAST, RIVER_S[1], surface="setts")
square("tipsy", "Tipsy Pier", COAST, 433, 742, 447, surface="setts")
square("one_eye_pier", "", COAST, 520, 752, 530, surface="setts", label=False)
square("cemetery", "Last Turning Cemetery", COLD + 12, SPADE + 10, THIMBLE, 351, surface="grass")
square("chapel_court", "", COLD + 12, 351, THIMBLE, WIDOWS, surface="cobbles", label=False)
square("close", "", 484, CROWS + 10, NETTLE, WIDOWS, surface="grass", label=False)
square("bishops", "Bishop's Garden", 484, CROWS + 10, 514, WIDOWS, surface="grass")
square("quiet", "Quiet Whistle Garden", 566, CROWS + 10, NETTLE, 352, surface="grass")
square("drowsy", "Drowsy Steps", 505, 330, 575, WIDOWS)
square("garth", "", 623, 285, TAR, 322, surface="grass", label=False)

# The cathedral, stone masses until its model comes (SLICE_MODELS_WANTED.md § C4), 8 m south of C4's for Crows' Walk.
box("nave", "The cathedral - nave", 528, 278, 552, 314, 22.0, "cathedral")
box("transept", "The cathedral - transept", 514, 290, 566, 306, 20.0, "cathedral")
box("apse", "The cathedral - apse", 532, 272, 548, 278, 16.0, "cathedral")
box("west_front", "The cathedral - tower", 530, 314, 550, 330, 44.0, "cathedral")

# ----------------------------------------------------------------------------------------------- the kit

# Row families (VeilmouthKitHouses.RowTypes at ×1.25, measured from the baked prefabs): houses of one family share
# height, depth and roof, so their open ends meet. (height to the ridge, depth, [(type, width, palettes)]), m. C5 adds
# the 15 m terrace and low tenement (the kit's Town and Tenement at 12 kit metres, baked with the C5 houses), so every
# family fills any run of 5 m steps.
FAMILIES = {
    "shop": (19, 10, [("Shop8", 10, ("stucco", "brick")), ("Shop12", 15, ("rich",))]),
    "shop_rich": (19, 10, [("Shop12", 15, ("rich",)), ("Shop8", 10, ("stucco",))]),
    "town": (15, 10, [("Terrace8", 10, ("brick", "stock")), ("ShopTown8", 10, ("dark",)), ("Terrace12", 15, ("brick", "stock"))]),
    "tenement": (16, 8, [("Tenement8", 10, ("poor", "brick")), ("Tenement12", 15, ("dark",))]),
    "low": (12, 8, [("TenementLow8", 10, ("poor",)), ("TenementLow12", 15, ("poor",))]),
    "warehouse": (16, 8, [("Warehouse12", 15, ("stock",)), ("Tenement8", 10, ("poor", "brick"))]),
}
# Single buildings: (width along the front, depth), m, measured; none joins a row, so a passage parts it from a house.
SIZES = {
    "Civic_rich": (30, 15), "Civic_stucco": (30, 15), "Civic_white": (30, 15), "Tower6_white": (5, 5), "Tower5_dark": (5, 5),
    "Chapel": (13, 10), "Tavern_brick": (15, 15), "ShopTown8_dark_detached": (10, 10), "Tenement8_poor_detached": (10, 8),
    "Villa_stucco": (15, 15), "House_Farm": (6, 7), "TenementLow8_poor_detached": (10, 8),
}
# A corner belongs to the higher street (§29.2): a lane under a wall is the lowest.
RANK = {"penny": 7, "main": 6, "square": 6, "side": 5, "embankment": 5, "quay": 4, "street": 3, "works": 3, "lane": 2, "wall": 1}
PASSAGE = 4          # R5: the least gap between two buildings on one line, m
HOLDS = ("owned", "flush", "inner")   # ends a house must reach
SPARE = ("passage", "free")           # ends where what no house fills stays open

ERRORS, NOTES = [], []


def footprint(facing, front, a, width, depth):
    if facing == "S":
        return a, front - depth, a + width, front
    if facing == "N":
        return a, front, a + width, front + depth
    if facing == "E":
        return front - depth, a, front, a + width
    return front, a, front + depth, a + width


def faced_suffix(facing, lo_faced, hi_faced):
    """The kit's end variant (VictorianHouses.CornerKey): a house's local -X is the row's low end for N and E fronts."""
    if lo_faced and hi_faced:
        return "_detached"
    if not lo_faced and not hi_faced:
        return ""
    low_is_left = facing in "NE"
    return "_cornerL" if lo_faced == low_is_left else "_cornerR"


class Edge:
    """One side of a block: its front line, its street's rank and its houses' family."""

    def __init__(self, block, p, q, spec):
        dx, dy = (q[0] > p[0]) - (q[0] < p[0]), (q[1] > p[1]) - (q[1] < p[1])
        self.block, self.d = block, (dx, dy)
        self.facing = {(0, -1): "N", (1, 0): "E", (0, 1): "S", (-1, 0): "W"}[(dy, -dx)]
        self.along_x = self.facing in "NS"
        self.front = p[1] if self.along_x else p[0]
        ap, aq = (p[0], q[0]) if self.along_x else (p[1], q[1])
        self.lo, self.hi, self.plus = min(ap, aq), max(ap, aq), aq > ap
        self.kind, self.family = spec[0], spec[1]
        self.limit = spec[2] if len(spec) > 2 else None
        self.ends, self.trim, self.singles = {"lo": "free", "hi": "free"}, {"lo": 0, "hi": 0}, []

    def depth(self):
        return FAMILIES[self.family][1]

    def pinned(self, side):
        for s in self.singles:
            w = SIZES[s["key"]][0]
            if (side == "lo" and abs(s["a"] - self.lo) < 0.01) or (side == "hi" and abs(s["a"] + w - self.hi) < 0.01):
                return s
        return None

    def builds_at(self, side):
        if self.pinned(side):
            return True
        if self.family is None:
            return False
        if self.limit:
            return self.limit[0] <= self.lo if side == "lo" else self.limit[1] >= self.hi
        return True


class Block:
    def __init__(self, bid, rect, wealth, district_name, edges, notch=None):
        self.id, self.rect, self.wealth, self.district, self.spec, self.notch = bid, rect, wealth, district_name, edges, notch
        self.singles, self.free, self.houses, self.gaps = [], [], [], []
        BLOCKS.append(self)

    def on(self, facing, a, key, sid, name="", stoop="plain"):
        """A single building on the block's side facing `facing`, from a along it."""
        self.singles.append({"facing": facing, "a": a, "key": key, "id": sid, "name": name, "stoop": stoop})
        return self

    def stand(self, key, facing, front, a, sid, name="", stoop="plain"):
        """A single building standing free in the block, off its sides."""
        self.free.append({"key": key, "facing": facing, "front": front, "a": a, "id": sid, "name": name, "stoop": stoop})
        return self

    def outline(self):
        x0, y0, x1, y1 = self.rect
        corners = [(x0, y0), (x1, y0), (x1, y1), (x0, y1)]
        if self.notch is None:
            return corners, None
        where, nx, ny = self.notch
        cut = {
            "NW": [(x0, y0 + ny), (x0 + nx, y0 + ny), (x0 + nx, y0)],
            "NE": [(x1 - nx, y0), (x1 - nx, y0 + ny), (x1, y0 + ny)],
            "SE": [(x1, y1 - ny), (x1 - nx, y1 - ny), (x1 - nx, y1)],
            "SW": [(x0 + nx, y1), (x0 + nx, y1 - ny), (x0, y1 - ny)],
        }[where]
        i = ["NW", "NE", "SE", "SW"].index(where)
        return corners[:i] + cut + corners[i + 1:], cut[1]

    def notch_rect(self):
        if self.notch is None:
            return None
        x0, y0, x1, y1 = self.rect
        where, nx, ny = self.notch
        return {"NW": (x0, y0, x0 + nx, y0 + ny), "NE": (x1 - nx, y0, x1, y0 + ny),
                "SE": (x1 - nx, y1 - ny, x1, y1), "SW": (x0, y1 - ny, x0 + nx, y1)}[where]

    def edges(self):
        pts, inner = self.outline()
        out = []
        for k in range(len(pts)):
            p, q = pts[k], pts[(k + 1) % len(pts)]
            facing = Edge(self, p, q, (0, None)).facing
            spec = self.spec["notch"] if inner in (p, q) else self.spec[facing]
            out.append(Edge(self, p, q, spec))
        for s in self.singles:
            hosts = [e for e in out if e.facing == s["facing"] and e.lo - 0.01 <= s["a"] and s["a"] + SIZES[s["key"]][0] <= e.hi + 0.01]
            if not hosts:
                ERRORS.append(f"{self.id}: {s['id']} is on no side facing {s['facing']}.")
                continue
            hosts[0].singles.append(s)
        return out

    def corners(self, edges):
        """Each corner: an inner one, an open one, one that what stands on it decides, or a choice between its sides."""
        out = []
        for k in range(len(edges)):
            e1, e2 = edges[k - 1], edges[k]
            s1, s2 = ("hi" if e1.plus else "lo"), ("lo" if e2.plus else "hi")
            if e1.d[0] * e2.d[1] - e1.d[1] * e2.d[0] < 0:
                out.append(("inner", e1, s1, e2, s2))
                continue
            p1, p2 = e1.pinned(s1), e2.pinned(s2)
            if p1 and p2:
                ERRORS.append(f"{self.id}: {p1['id']} and {p2['id']} both stand on one corner.")
            if not (e1.builds_at(s1) and e2.builds_at(s2)):
                out.append(("open", e1, s1, e2, s2))
            elif p1 or p2:
                out.append(("first" if p1 else "second", e1, s1, e2, s2))
            else:
                out.append(("choice", e1, s1, e2, s2))
        return out

    def own(self, corners, choice):
        """Sets the ends for one choice of owners (§29.2): the owner's corner house stands to the corner; the other side
        starts behind it, flush against its back wall where that wall is at least as high, else after a passage.
        Returns how many corners went to the lower street."""
        bits = iter(choice)
        violations = 0
        for kind, e1, s1, e2, s2 in corners:
            if kind == "inner":
                e1.ends[s1] = e2.ends[s2] = "inner"
                continue
            if kind == "open":
                for e, s in ((e1, s1), (e2, s2)):
                    e.ends[s] = "owned" if e.builds_at(s) else "free"
                continue
            first = kind == "first" or (kind == "choice" and next(bits))
            o, so, n, sn = (e1, s1, e2, s2) if first else (e2, s2, e1, s1)
            if kind == "choice" and RANK[o.kind] < RANK[n.kind]:
                violations += 1
            single = o.pinned(so)
            o.ends[so] = "owned"
            n.trim[sn] = SIZES[single["key"]][1] if single else o.depth()
            flush = single is None and n.family is not None and FAMILIES[o.family][0] >= FAMILIES[n.family][0]
            n.ends[sn] = "flush" if flush else "passage"
        return violations

    def segments(self, e):
        """The runs of an edge between its ends and its single buildings, with their end types."""
        lo, hi = e.lo + e.trim["lo"], e.hi - e.trim["hi"]
        tlo, thi = e.ends["lo"], e.ends["hi"]
        if e.limit:
            if e.limit[0] > e.lo:
                lo, tlo = max(lo, e.limit[0]), "free"
            if e.limit[1] < e.hi:
                hi, thi = min(hi, e.limit[1]), "free"
        segs, cur, tcur = [], lo, tlo
        for s in sorted(e.singles, key=lambda s: s["a"]):
            a0, a1 = s["a"], s["a"] + SIZES[s["key"]][0]
            if a0 > cur + 0.01:
                segs.append((cur, a0, tcur, "passage"))
            elif a0 < cur - 0.01:
                self.errs.append(f"{s['id']} reaches into the corner's house")
            cur, tcur = a1, "passage"
        segs.append((cur, hi, tcur, thi))
        return segs

    def solve(self, choice, force=None):
        """Fills every side for one choice of corner owners."""
        self.houses, self.gaps, self.placed, self.errs = [], [], [], []
        edges = self.edges()
        self.violations = self.own(self.corners(edges), choice)
        for ei, e in enumerate(edges):
            for s in e.singles:
                w, d = SIZES[s["key"]]
                self.placed.append({**s, "front": e.front, "fp": footprint(e.facing, e.front, s["a"], w, d)})
            segs = self.segments(e)
            if e.family is None:
                # A side of single buildings: only the gaps between them are passages; its open stretches are the edge
                # of the square, the garden or the garth behind.
                for (a0, a1, t0, t1) in segs:
                    if t0 == "passage" and t1 == "passage" and a1 - a0 > 0.01:
                        if a1 - a0 < PASSAGE - 0.01:
                            self.errs.append(f"a gap of {a1 - a0:g} m between single buildings on the {e.facing} side")
                        self.gaps.append((e, a0, a1, 10))
                continue
            rng = random.Random(zlib.crc32(f"{self.id}.{ei}".encode()))
            for si, seg in enumerate(segs):
                self.fill(e, seg, rng, force == (ei, si))
        return edges

    @staticmethod
    def compose(family, length, t0, t1, force):
        """The widths that fill a run best: the most house, what is left 0 or a passage, the widths mixed."""
        types = FAMILIES[family][2]
        widths = sorted({w for _, w, _ in types})
        need = PASSAGE * ((t0 == "passage") + (t1 == "passage"))
        spare = t0 in SPARE or t1 in SPARE
        holds = t0 in HOLDS or t1 in HOLDS
        best = None
        for n15 in range(0, int(length // 15) + 1 if 15 in widths else 1):
            for n10 in range(0, int(length // 10) + 1 if 10 in widths else 1):
                s = 10 * n10 + 15 * n15
                left = length - s
                if left < -0.01:
                    continue
                if s == 0:
                    # No house: the run is one passage, or nothing, where no end must hold a house.
                    ok = not holds and not force and (left < 0.01 or left >= PASSAGE - 0.01)
                elif spare:
                    ok = left >= need - 0.01
                else:
                    ok = left >= PASSAGE - 0.01 if force else (abs(left) < 0.01 or left >= PASSAGE - 0.01)
                if ok:
                    n = n10 + n15
                    score = (s, -abs(n15 - n / 3.0))
                    if best is None or score > best[0]:
                        best = (score, n10, n15, left)
        return best

    def fill(self, e, seg, rng, force):
        a0, a1, t0, t1 = seg
        length = a1 - a0
        if length < -0.01:
            self.errs.append(f"the {e.facing} side at {e.front} is {length:.1f} m long between its corners")
            return
        if length < 0.01:
            return
        best = self.compose(e.family, length, t0, t1, force)
        if best is None:
            # An open end no house can reach becomes a passage beside the corner house's back wall.
            relax = lambda t: "passage" if t == "flush" else t
            for alt in ((t0, relax(t1)), (relax(t0), t1), (relax(t0), relax(t1))):
                if alt != (t0, t1):
                    best = self.compose(e.family, length, alt[0], alt[1], force)
                    if best is not None:
                        t0, t1 = alt
                        break
        if best is None:
            self.errs.append(f"no houses fit the {e.facing} side at {e.front} from {a0:g} to {a1:g} ({length:g} m, ends {t0}/{t1})")
            return
        _, depth, types = FAMILIES[e.family]
        _, n10, n15, left = best
        by_width = {w: [t for t in types if t[1] == w] for w in (10, 15)}
        houses = [10] * n10 + [15] * n15
        rng.shuffle(houses)
        if not houses:
            if left > 0.01:
                self.gaps.append((e, a0, a1, depth))
            return
        gap_lo = PASSAGE if t0 == "passage" else 0
        gap_hi = PASSAGE if t1 == "passage" else 0
        middle = 0
        if t0 in SPARE or t1 in SPARE:
            extra = left - gap_lo - gap_hi
            if t1 in SPARE:
                gap_hi += extra
            else:
                gap_lo += extra
        else:
            middle = left
        split = len(houses)
        if middle > 0.01:
            fixed = ("owned", "inner")
            if len(houses) >= 2:
                split = len(houses) // 2
            elif t0 not in fixed:
                split = 0
            elif t1 not in fixed:
                split = 1
            else:
                self.errs.append(f"no room for a passage on the {e.facing} side at {e.front} from {a0:g}")
                return
        a = a0
        if gap_lo > 0.01:
            self.gaps.append((e, a, a + gap_lo, depth))
            a += gap_lo
        runs = [houses[:split], houses[split:]]
        for ri, run in enumerate(runs):
            if ri == 1 and middle > 0.01:
                self.gaps.append((e, a, a + middle, depth))
                a += middle
            for i, w in enumerate(run):
                if i > 0:
                    lo_faced = False
                elif ri == 1 and middle > 0.01:
                    lo_faced = True
                else:
                    lo_faced = not (a - a0 < 0.01 and t0 == "flush")
                if i < len(run) - 1:
                    hi_faced = False
                elif ri == 0 and middle > 0.01:
                    hi_faced = True
                else:
                    hi_faced = not (gap_hi < 0.01 and t1 == "flush")
                kind, _, palettes = rng.choice(by_width[w])
                key = f"{kind}_{rng.choice(palettes)}{faced_suffix(e.facing, lo_faced, hi_faced)}"
                self.houses.append({"key": key, "facing": e.facing, "front": e.front, "a": a, "w": w, "d": depth,
                                    "fp": footprint(e.facing, e.front, a, w, depth), "family": e.family})
                a += w
        if gap_hi > 0.01:
            self.gaps.append((e, a, a + gap_hi, depth))

    def score(self):
        """Lower is better: no broken rule, one or two ways into the yard, the corners with the higher streets, the
        least ground left as passages."""
        ways = len(self.gaps)
        return (len(self.errs), max(0, ways - 2), 0 if ways or not self.houses else 1, self.violations,
                sum(a1 - a0 for (_, a0, a1, _) in self.gaps))

    def run(self):
        """Tries every choice of corner owners and keeps the best; a closed yard gets a passage on its lowest street."""
        choices = sum(1 for c in self.corners(self.edges()) if c[0] == "choice")
        best = None
        for bits in range(2 ** choices):
            choice = [bool(bits >> i & 1) for i in range(choices)]
            sides = self.solve(choice)
            if not self.gaps and self.houses and not self.errs:
                options = sorted((RANK[e.kind], -(s[1] - s[0]), ei, si)
                                 for ei, e in enumerate(sides) if e.family is not None
                                 for si, s in enumerate(self.segments(e)) if s[1] - s[0] >= 10 + PASSAGE)
                for opt in options:
                    self.solve(choice, force=opt[2:])
                    if not self.errs and self.gaps:
                        break
                else:
                    sides = self.solve(choice)
            sc = self.score()
            if best is None or sc < best[0]:
                best = (sc, choice, self.houses, self.gaps, self.placed, self.errs, sides)
        _, self.choice, self.houses, self.gaps, self.placed, self.errs, self.edge_list = best
        for err in self.errs:
            ERRORS.append(f"{self.id}: {err}")
        if len(self.gaps) > 2:
            NOTES.append(f"{self.id}: {len(self.gaps)} ways into its yard.")
        elif not self.gaps and self.houses:
            ERRORS.append(f"{self.id}: its yard has no way in and no side can spare a passage.")


BLOCKS = []

# ----------------------------------------------------------------------------------------------- the blocks

# Wealth falls from the Penny toward Lower Veil, the cemetery and the works (§29.2): rich shops on the Penny, shops on
# the main streets, terraces in the middling blocks, tenements in Lower Veil, low tenements in the slums, warehouses on
# the quay. The civic buildings face the main streets at the Penny's four corners.
N15 = HALF - (MAIN + 22 - MAIN) / 2   # the Penny's corners, 15 x 15, between its frame and the main streets

W1A = Block("W1A", (COLD + 12, WIDOWS + 17, THIMBLE, MAIN), "rich", "SLEEPY QUARTER",
            {"N": ("side", "town"), "E": ("street", "town"), "S": ("main", "shop"), "W": ("wall", "town")})
W1B = Block("W1B", (COLD + 12, MAIN + 22, THIMBLE, BUTTON), "middling", "SLEEPY QUARTER",
            {"N": ("main", "shop"), "E": ("street", "town"), "S": ("lane", "town"), "W": ("wall", "town")})
W1C = Block("W1C", (COLD + 12, BUTTON + 10, THIMBLE, EMBANK), "rich", "SLEEPY QUARTER",
            {"N": ("lane", "town"), "E": ("street", "town"), "S": ("embankment", "town"), "W": ("wall", "town")})
W2A = Block("W2A", (THIMBLE + 12, WIDOWS + 17, LENTEN, MAIN), "rich", "THE PENNY",
            {"N": ("side", "town"), "E": ("main", "shop"), "S": ("main", None), "W": ("street", "town"),
             "notch": ("penny", "shop_rich")}, notch=("SE", N15, N15))
W2A.on("S", THIMBLE + 12, "Civic_rich", "town_hall", "The Town Hall")
# The clock tower rises behind the Town Hall, 0.5 m off its back wall (its eave overhangs 0.375 m).
W2A.stand("Tower6_white", "S", MAIN - 15.5, THIMBLE + 12 + 12.5, "clock_tower", "The clock tower")
W2B = Block("W2B", (THIMBLE + 12, MAIN + 22, LENTEN, BUTTON), "rich", "THE PENNY",
            {"N": ("main", None), "E": ("main", "shop"), "S": ("lane", "town"), "W": ("street", "town"),
             "notch": ("penny", "shop_rich")}, notch=("NE", N15, N15))
W2B.on("N", THIMBLE + 12, "Civic_white", "governor", "The Governor's House")
W2C = Block("W2C", (THIMBLE + 12, BUTTON + 10, LENTEN, EMBANK), "middling", "SLEEPY QUARTER",
            {"N": ("lane", "town"), "E": ("main", "shop"), "S": ("embankment", "town"), "W": ("street", "town")})
E1A = Block("E1A", (LENTEN + 22, WIDOWS + 17, NETTLE, MAIN), "rich", "THE PENNY",
            {"N": ("side", "town"), "E": ("street", "town"), "S": ("main", None), "W": ("main", "shop"),
             "notch": ("penny", "shop_rich")}, notch=("SW", N15, N15))
E1A.on("S", C[0] + HALF, "Civic_stucco", "assize", "The Assize and the Wardens' station")
E1B = Block("E1B", (LENTEN + 22, MAIN + 22, NETTLE, BUTTON), "middling", "LOWER VEIL",
            {"N": ("main", None), "E": ("street", "tenement"), "S": ("lane", "tenement"), "W": ("main", "shop"),
             "notch": ("penny", "shop_rich")}, notch=("NW", N15, N15))
E1B.on("N", C[0] + HALF, "Civic_white", "guild_hall", "The Guild Hall")
E1C = Block("E1C", (LENTEN + 22, BUTTON + 10, NETTLE, EMBANK), "poor", "LOWER VEIL",
            {"N": ("lane", "tenement"), "E": ("square", None), "S": ("embankment", "tenement"), "W": ("main", "tenement")})
# The Soot & Bell and Harrow & Tincture face the Black Puddle, a passage between them to the tavern's yard door (R5).
E1C.on("E", BUTTON + 10, "Tavern_brick", "soot_bell", "The Soot & Bell")
E1C.on("E", EMBANK - 10, "ShopTown8_dark_detached", "harrow", "Harrow & Tincture")
E2A = Block("E2A", (NETTLE + 12, WIDOWS + 17, TAR, MAIN), "middling", "SALT END",
            {"N": ("side", "town"), "E": ("quay", "warehouse"), "S": ("main", "shop"), "W": ("street", "tenement")})
E2A.on("E", WIDOWS + 17, "Civic_white", "customs", "The Customs House")
E2B = Block("E2B", (NETTLE + 12, MAIN + 22, TAR, BUTTON), "poor", "LOWER VEIL",
            {"N": ("main", "shop"), "E": ("quay", "warehouse"), "S": ("lane", "low"), "W": ("street", "tenement")})
E2B.on("S", 621, "Tenement8_poor_detached", "missing", "The missing person's tenement")

# Parsons' End: the slum by the cemetery, the cemetery with the old chapel and its court, the cathedral's close with
# its gardens and the faction's house, the Brothers' House round its garth.
NW1 = Block("NW1", (COLD + 12, CROWS + 10, THIMBLE, SPADE), "poor", "PARSONS' END",
            {"N": ("wall", "low"), "E": ("street", "low"), "S": ("lane", "low"), "W": ("wall", "low")})
CEM = Block("CEM", (COLD + 12, SPADE + 10, THIMBLE, WIDOWS), "cemetery", "PARSONS' END",
            {"N": ("lane", None), "E": ("street", None), "S": ("side", None), "W": ("wall", None)})
CEM.stand("Chapel", "S", WIDOWS, 434, "chapel", "The old chapel")
CLOSE = Block("CLOSE", (THIMBLE + 12, CROWS + 10, NETTLE, WIDOWS), "close", "PARSONS' END",
              {"N": ("wall", None), "E": ("street", None), "S": ("side", None), "W": ("street", None)})
CLOSE.on("S", 579, "Villa_stucco", "faction_house", "The faction's house")
NE = Block("NE", (NETTLE + 12, CROWS + 10, TAR, WIDOWS), "middling", "PARSONS' END",
           {"N": ("wall", None), "E": ("quay", "town", (322, WIDOWS)), "S": ("side", "town"), "W": ("street", "town")})
NE.on("N", NETTLE + 12, "Civic_stucco", "monastery_n", "The Brothers' House - north range")
NE.on("W", 289, "Civic_stucco", "monastery_w", "The Brothers' House - west range")
NE.stand("Tower5_dark", "S", 310, 632, "bell_tower", "The Brothers' bell tower")

# The works (§29.2): blocks of plain boxes on the works' roads, colliders without props, 9-16 m high; the workers' slum
# on Rats' Choir; Deaf Boiler Yard, POI C's yard and Ritual Site Candidate 2, inside a block with one way in.
WS1 = Block("WS1", (CINDER + 10, CLINKER + 14, BELLOWS, COUGH), "poor", "THE REEK",
            {"N": ("works", "low"), "E": ("lane", "low"), "S": ("wall", "low"), "W": ("wall", "low")})
WS2 = Block("WS2", (BELLOWS + 10, CLINKER + 14, FURNACE, COUGH), "poor", "THE REEK",
            {"N": ("works", "low"), "E": ("works", "low"), "S": ("wall", "low"), "W": ("lane", "low")})
WORKS_BLOCKS = [
    ("WN1", (CINDER + 10, ASH + 10, BELLOWS, CLINKER), (14.0, 10.0)),
    ("WN2", (BELLOWS + 10, ASH + 10, FURNACE, CLINKER), (12.0, 15.0)),
    ("EN1", (FURNACE + 14, ASH + 10, KETTLE, CLINKER), (16.0, 11.0)),
    ("EN2", (KETTLE + 10, ASH + 10, BRINE, CLINKER), (12.0, 14.0)),
    ("ES1", (FURNACE + 14, CLINKER + 14, KETTLE, COUGH), (13.0, 9.0)),
]
for wid, (x0, y0, x1, y1), (h0, h1) in WORKS_BLOCKS:
    Block(wid, (x0, y0, x1, y1), "works", "THE REEK", {"N": ("works", None), "E": ("works", None), "S": ("works", None), "W": ("works", None)})
    box(f"{wid}_n", f"Works {wid} - north shed", x0, y0, x1, y0 + 14, h0)
    box(f"{wid}_s", f"Works {wid} - south shed", x0, y1 - 14, x1, y1, h1)
ES2 = (KETTLE + 10, CLINKER + 14, BRINE, COUGH)
Block("ES2", ES2, "works", "THE REEK", {"N": ("works", None), "E": ("lane", None), "S": ("wall", None), "W": ("lane", None)})
DEAF = (ES2[0] + 8, ES2[1] + 8, ES2[2] - 8, ES2[3] - 6)
box("ES2_nw", "Works ES2 - boiler house", ES2[0], ES2[1], 638, DEAF[1], 14.0)
box("ES2_ne", "Works ES2 - coal store", 644, ES2[1], ES2[2], DEAF[1], 10.0)   # the yard's one way in, 6 m, off Clinker Road
box("ES2_w", "Works ES2 - west shed", ES2[0], DEAF[1], DEAF[0], ES2[3], 12.0)
box("ES2_e", "Works ES2 - east shed", DEAF[2], DEAF[1], ES2[2], ES2[3], 12.0)
box("ES2_s", "Works ES2 - south shed", DEAF[0], DEAF[3], DEAF[2], ES2[3], 9.0)
square("deaf_boiler", "Deaf Boiler Yard", *DEAF, surface="cobbles")

# The port: the One-Eye on its pier.
layout["buildings"].append({"id": "one_eye", "key": "Tower6_white", "facing": "W", "front": 745, "a": 520, "name": "The One-Eye",
                            "stoop": "plain", "block": ""})

# ----------------------------------------------------------------------------------------------- solving and checks

for b in BLOCKS:
    b.run()


def overlap(a, b, eps=0.01):
    return a[0] < b[2] - eps and b[0] < a[2] - eps and a[1] < b[3] - eps and b[1] < a[3] - eps


def check():
    shapes = []
    for b in BLOCKS:
        for h in b.houses:
            shapes.append((h["fp"], f"{b.id} {h['key']} at {h['a']:g}"))
        for s in b.placed + [dict(s, fp=footprint(s["facing"], s["front"], s["a"], *SIZES[s["key"]])) for s in b.free]:
            shapes.append((s["fp"], f"{b.id} {s['id']}"))
    for bx in layout["boxes"]:
        if bx["material"] != "cathedral":   # the cathedral's masses cross by design
            shapes.append(((bx["x0"], bx["y0"], bx["x1"], bx["y1"]), bx["id"]))
    for i in range(len(shapes)):
        for j in range(i + 1, len(shapes)):
            (a, na), (c, nc) = shapes[i], shapes[j]
            if overlap(a, c):
                ERRORS.append(f"overlap: {na} and {nc}")
            for axis in (0, 1):
                other = 1 - axis
                gap = max(a[axis] - c[axis + 2], c[axis] - a[axis + 2])
                line = abs(a[other] - c[other]) < 0.3 or abs(a[other + 2] - c[other + 2]) < 0.3
                if line and 0.05 < gap < PASSAGE - 0.05:
                    ERRORS.append(f"R5: {gap:.2f} m between {na} and {nc}")
    # Every building stands inside its block (and off the Penny's corners).
    for b in BLOCKS:
        x0, y0, x1, y1 = b.rect
        n = b.notch_rect()
        for h in b.houses + b.placed:
            f = h["fp"]
            if f[0] < x0 - 0.01 or f[1] < y0 - 0.01 or f[2] > x1 + 0.01 or f[3] > y1 + 0.01 or (n and overlap(f, n)):
                ERRORS.append(f"{b.id}: {h.get('id', h['key'])} stands outside its block")


check()

for b in BLOCKS:
    for h in b.houses:
        layout["buildings"].append({"id": "", "key": h["key"], "facing": h["facing"], "front": h["front"], "a": h["a"],
                                    "name": "", "stoop": "plain", "block": b.id})
    for s in b.placed + [dict(s, fp=None) for s in b.free]:
        layout["buildings"].append({"id": s["id"], "key": s["key"], "facing": s["facing"], "front": s["front"], "a": s["a"],
                                    "name": s["name"], "stoop": s["stoop"], "block": b.id})
    for (e, a0, a1, depth) in b.gaps:
        layout["passages"].append({"block": b.id, "facing": e.facing, "front": e.front, "a0": a0, "a1": a1, "depth": depth})
    x0, y0, x1, y1 = b.rect
    n = b.notch_rect()
    inset = {"N": 0, "E": 0, "S": 0, "W": 0}
    for e in b.edge_list:
        if e.family:
            inset[e.facing] = max(inset[e.facing], e.depth())
    layout["blocks"].append({
        "id": b.id, "wealth": b.wealth, "district": b.district, "x0": x0, "y0": y0, "x1": x1, "y1": y1,
        "notch": {"x0": n[0], "y0": n[1], "x1": n[2], "y1": n[3]} if n else None,
        "yard": {"x0": x0 + inset["W"], "y0": y0 + inset["N"], "x1": x1 - inset["E"], "y1": y1 - inset["S"]},
    })

# ----------------------------------------------------------------------------------------------- the outskirts

layout["cottages"] = [
    {"key": "House_Farm", "facing": "S", "front": 428, "a": 300}, {"key": "House_Farm", "facing": "S", "front": 428, "a": 326},
    {"key": "House_Farm", "facing": "N", "front": 452, "a": 292}, {"key": "House_Farm", "facing": "N", "front": 452, "a": 322},
    {"key": "House_Farm", "facing": "S", "front": 428, "a": 262}, {"key": "House_Farm", "facing": "N", "front": 452, "a": 256},
    {"key": "TenementLow8_poor_detached", "facing": "N", "front": 452, "a": 344},
    {"key": "TenementLow8_poor_detached", "facing": "S", "front": 428, "a": 350},
]
layout["forest"] = {"rects": [
    {"x0": 20, "y0": 20, "x1": COAST_OPEN - 10, "y1": 236},
    {"x0": 20, "y0": 236, "x1": 370, "y1": 404},
    {"x0": 20, "y0": 476, "x1": 370, "y1": 980},
    {"x0": 370, "y0": 600, "x1": WORKS[0] - 8, "y1": 980},
    {"x0": WORKS[0] - 8, "y0": 720, "x1": COAST_OPEN - 10, "y1": 980},
]}

# ----------------------------------------------------------------------------------------------- districts and places

district("", THIMBLE + 12, WIDOWS + 17, NETTLE, BUTTON, "#efe2c8")   # the Penny's blocks; the square names them
district("PARSONS' END", X_IN, CROWS, APRON, WIDOWS, "#e3d6f0", at=(452, 266))
district("SLEEPY QUARTER", X_IN, WIDOWS + 17, THIMBLE + 12, EMBANK, "#f6e3c4", at=(451, 380))
district("SALT END", NETTLE, WIDOWS + 17, APRON, MAIN + 22, "#e8dcc8", at=(626, 380))
district("LOWER VEIL", LENTEN + 22, MAIN + 22, APRON, RIVER_S[1], "#e9cfc7", at=(610, 556))
district("THE REEK", WORKS[0], WORKS[1], WORKS[2], WORKS[3], "#d8d2c8", at=(500, 708))
district("", APRON, CROWS, COAST, RIVER_S[1], "#d6d0c4")

layout["candidates"] += [
    {"name": "Ritual Site Candidate 1 - Black Puddle", "x": (NETTLE + TAR) / 2, "y": (BUTTON + 10 + EMBANK) / 2, "size": 26},
    {"name": "Ritual Site Candidate 2 - Deaf Boiler Yard", "x": (DEAF[0] + DEAF[2]) / 2, "y": (DEAF[1] + DEAF[3]) / 2, "size": 26},
    {"name": "Ritual Site Candidate 3 - Old Chapel", "x": 461, "y": 359, "size": 14},
]
layout["spawn"] = {"x": 620, "y": 541}   # on the embankment before the Black Puddle and the Soot & Bell

# ----------------------------------------------------------------------------------------------- drawing

COLOURS = {
    "land": "#d9e8bf", "water": "#7fb2d9", "walk": "#c8c8c8", "road": "#9a9a9a", "lane": "#b0b0b0", "square": "#d2cdc2",
    "grass": "#a9cf8e", "cobbles": "#c4bcae", "setts": "#a9a9a9", "wall": "#6d6253", "house": "#8a7a68", "corner": "#6e5f4f",
    "civic": "#6f5f8f", "works": "#8d8d8d", "cathedral": "#7d6f9f", "forest": "#9cc48a", "bridge": "#b7a98c",
    "statue": "#4a4036", "stall": "#c9873c", "passage": "#e0d4b8",
    "rich": "#b7d99f", "middling": "#cbc3b1", "poor": "#a8916f", "cemetery": "#a9cf8e", "close": "#a9cf8e", "yard_works": "#b4aea4",
}


class Canvas:
    """A minimal raster for the preview: filled rectangles, polygons and circles, written as a PNG with no library."""

    def __init__(self, w, h, scale):
        self.w, self.h, self.s = w, h, scale
        self.px = bytearray(b"\xff" * (w * h * 3))

    def _set(self, x, y, rgb):
        if 0 <= x < self.w and 0 <= y < self.h:
            i = (y * self.w + x) * 3
            self.px[i:i + 3] = rgb

    def rect(self, x0, y0, x1, y1, colour):
        rgb = bytes.fromhex(colour[1:])
        for y in range(int(min(y0, y1) * self.s), int(math.ceil(max(y0, y1) * self.s))):
            for x in range(int(min(x0, x1) * self.s), int(math.ceil(max(x0, x1) * self.s))):
                self._set(x, y, rgb)

    def poly(self, pts, colour):
        rgb = bytes.fromhex(colour[1:])
        pts = [(x * self.s, y * self.s) for x, y in pts]
        y0, y1 = int(min(p[1] for p in pts)), int(math.ceil(max(p[1] for p in pts)))
        for y in range(y0, y1):
            yc = y + 0.5
            xs = []
            for i in range(len(pts)):
                (ax, ay), (bx, by) = pts[i], pts[(i + 1) % len(pts)]
                if (ay <= yc < by) or (by <= yc < ay):
                    xs.append(ax + (yc - ay) * (bx - ax) / (by - ay))
            xs.sort()
            for a, b in zip(xs[::2], xs[1::2]):
                for x in range(int(a + 0.5), int(b + 0.5)):
                    self._set(x, y, rgb)

    def circle(self, cx, cy, r, colour):
        n = 48
        self.poly([(cx + r * math.cos(2 * math.pi * k / n), cy + r * math.sin(2 * math.pi * k / n)) for k in range(n)], colour)

    def save(self, path):
        raw = b"".join(b"\x00" + bytes(self.px[y * self.w * 3:(y + 1) * self.w * 3]) for y in range(self.h))

        def chunk(tag, data):
            c = struct.pack(">I", len(data)) + tag + data
            return c + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

        png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", self.w, self.h, 8, 2, 0, 0, 0))
        png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
        Path(path).write_bytes(png)


def stalls():
    """The stalls round the statue, facing out to the ring road: centres and their angle."""
    st = layout["plaza"]["stalls"]
    return [(C[0] + st["radius"] * math.cos(t), C[1] + st["radius"] * math.sin(t), t)
            for t in (2 * math.pi * (k + 0.5) / st["count"] for k in range(st["count"]))]


def shapes():
    """Every shape of the plan in drawing order: (kind, geometry, colour, player)."""
    out = [("rect", (0, 0, 1000, 1000), COLOURS["land"], True)]
    for f in layout["forest"]["rects"]:
        out.append(("rect", (f["x0"], f["y0"], f["x1"], f["y1"]), COLOURS["forest"], True))
    for d in layout["districts"]:
        out.append(("rect", (d["x0"], d["y0"], d["x1"], d["y1"]), d["colour"], True))
    for w in layout["water"]:
        out.append(("rect", (w["x0"], w["y0"], w["x1"], w["y1"]), COLOURS["water"], True))
    for r in layout["roads"]:
        out.append(("rect", (r["x0"], r["y0"], r["x1"], r["y1"]), COLOURS["cobbles"], True))
    for b in BLOCKS:
        pts, _ = b.outline()
        out.append(("poly", pts, COLOURS["yard_works" if b.wealth == "works" else b.wealth], True))
    for s in layout["squares"]:
        out.append(("rect", (s["x0"], s["y0"], s["x1"], s["y1"]), COLOURS.get(s["surface"], COLOURS["square"]), True))
    # Every pavement first and every carriageway last, as the ground paints them (§29.1 item 2).
    for s in layout["streets"]:
        whole = (s["a0"], s["c0"], s["a1"], s["c1"]) if s["axis"] == "x" else (s["c0"], s["a0"], s["c1"], s["a1"])
        out.append(("rect", whole, COLOURS["walk"], True))
    p = layout["plaza"]
    out.append(("circle", (p["cx"], p["cy"], p["ringOuter"]), COLOURS["road"], True))
    for s in layout["streets"]:
        if s["axis"] == "x":
            road = (s["a0"], s["c0"] + s["walk0"], s["a1"], s["c1"] - s["walk1"])
        else:
            road = (s["c0"] + s["walk0"], s["a0"], s["c1"] - s["walk1"], s["a1"])
        out.append(("rect", road, COLOURS["lane" if s["kind"] == "lane" else "road"], True))
    out.append(("circle", (p["cx"], p["cy"], p["island"]), COLOURS["walk"], True))
    for x, y, t in stalls():
        st = p["stalls"]
        ux, uy = math.cos(t), math.sin(t)
        vx, vy = -uy, ux
        hw, hd = st["width"] / 2, st["depth"] / 2
        out.append(("poly", [(x + ux * hd + vx * hw, y + uy * hd + vy * hw), (x + ux * hd - vx * hw, y + uy * hd - vy * hw),
                             (x - ux * hd - vx * hw, y - uy * hd - vy * hw), (x - ux * hd + vx * hw, y - uy * hd + vy * hw)],
                    COLOURS["stall"], True))
    out.append(("circle", (p["cx"], p["cy"], p["plinth"]), COLOURS["statue"], True))
    for b in layout["bridges"]:
        out.append(("rect", (b["x0"], b["y0"], b["x1"], b["y1"]), COLOURS["bridge"], True))
    for w in layout["walls"]:
        g = (w["t0"], w["a0"], w["t1"], w["a1"]) if w["axis"] == "y" else (w["a0"], w["t0"], w["a1"], w["t1"])
        out.append(("rect", g, COLOURS["wall"], True))
    for b in BLOCKS:
        for (e, a0, a1, depth) in b.gaps:
            out.append(("rect", footprint(e.facing, e.front, a0, a1 - a0, min(depth, 3)), COLOURS["passage"], False))
        for h in b.houses:
            colour = COLOURS["corner"] if h["key"].endswith(("_cornerL", "_cornerR", "_detached")) else COLOURS["house"]
            f = h["fp"]
            out.append(("rect", f, colour, True))
            # the party walls, a pale line, so each house shows
            if h["facing"] in "NS":
                out.append(("rect", (f[0], f[1], f[0] + 0.5, f[3]), "#b3a593", True))
            else:
                out.append(("rect", (f[0], f[1], f[2], f[1] + 0.5), "#b3a593", True))
        for s in b.placed + [dict(s, fp=footprint(s["facing"], s["front"], s["a"], *SIZES[s["key"]])) for s in b.free]:
            civic = s["key"].startswith(("Civic", "Tower", "Chapel", "Villa"))
            out.append(("rect", s["fp"], COLOURS["civic" if civic else "corner"], True))
    for bx in layout["boxes"]:
        out.append(("rect", (bx["x0"], bx["y0"], bx["x1"], bx["y1"]), COLOURS[bx["material"]], True))
    for t in layout["towers"]:
        out.append(("rect", footprint(t["facing"], t["front"], t["a"], 5, 5), COLOURS["wall"], True))
    for b in layout["buildings"]:
        if b["id"] == "one_eye":
            out.append(("rect", footprint(b["facing"], b["front"], b["a"], 5, 5), COLOURS["civic"], True))
    for c in layout["cottages"]:
        w, d = SIZES[c["key"]]
        out.append(("rect", footprint(c["facing"], c["front"], c["a"], w, d), COLOURS["house"], True))
    for c in layout["candidates"]:
        h = c["size"] / 2
        out.append(("frame", (c["x"] - h, c["y"] - h, c["x"] + h, c["y"] + h), "#d23c3c", False))
    return out


def png(path, view=(0, 0, 1000, 1000), scale=1.0):
    x0, y0, x1, y1 = view
    canvas = Canvas(int((x1 - x0) * scale), int((y1 - y0) * scale), scale)

    def shift(g):
        return [(x - x0, y - y0) for x, y in g]

    for kind, g, colour, _ in shapes():
        if kind == "rect":
            canvas.rect(g[0] - x0, g[1] - y0, g[2] - x0, g[3] - y0, colour)
        elif kind == "poly":
            canvas.poly(shift(g), colour)
        elif kind == "circle":
            canvas.circle(g[0] - x0, g[1] - y0, g[2], colour)
        elif kind == "frame":
            for e in ((g[0], g[1], g[2], g[1] + 1), (g[0], g[3] - 1, g[2], g[3]), (g[0], g[1], g[0] + 1, g[3]), (g[2] - 1, g[1], g[2], g[3])):
                canvas.rect(e[0] - x0, e[1] - y0, e[2] - x0, e[3] - y0, colour)
    canvas.save(path)


# The names the map writes on its buildings, short (each is a key in map.csv: map.<name in lower snake case>).
MAP_NAMES = {
    "town_hall": "Town Hall", "clock_tower": "Clock", "assize": "Assize, Wardens", "governor": "Governor's House",
    "guild_hall": "Guild Hall", "chapel": "Old Chapel", "monastery_n": "Brothers' House", "customs": "Customs House",
    "one_eye": "One-Eye", "soot_bell": "Soot & Bell", "harrow": "Harrow", "faction_house": "Faction House",
}


def svg(player):
    """The plan as SVG, in CityMapElement's subset; the player's map leaves out the planner's notes (S1.6)."""
    view = (200, 236, 580, 500) if player else (190, 226, 600, 520)
    x0, y0, w, h = view
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{x0} {y0} {w} {h}" font-family="Segoe UI, Arial, sans-serif">']
    if player:
        out.append("  <!-- The player's map (S1.6): VEILMOUTH_CITY_PLAN_C5.svg without the planner's notes. Written by "
                   "tools/slice/veilmouth_c5_plan.py. 1 unit = 1 m; world X = px - 500, world Z = 500 - py. North is up. -->")
    else:
        out.append("  <!-- Veilmouth, city plan C5 (VEILMOUTH_CITY_PLAN.md §29), written by tools/slice/veilmouth_c5_plan.py "
                   "from the layout it also writes for the builder. 1 unit = 1 m; world X = px - 500, world Z = 500 - py. -->")

    def f(v):
        return f"{v:g}"

    for kind, g, colour, for_player in shapes():
        if player and not for_player:
            continue
        if kind == "rect":
            out.append(f'  <rect x="{f(min(g[0], g[2]))}" y="{f(min(g[1], g[3]))}" width="{f(abs(g[2] - g[0]))}" '
                       f'height="{f(abs(g[3] - g[1]))}" fill="{colour}"/>')
        elif kind == "poly":
            out.append(f'  <polygon points="{" ".join(f"{x:.1f},{y:.1f}" for x, y in g)}" fill="{colour}"/>')
        elif kind == "circle":
            out.append(f'  <circle cx="{f(g[0])}" cy="{f(g[1])}" r="{f(g[2])}" fill="{colour}"/>')
        elif kind == "frame":
            out.append(f'  <rect x="{f(g[0])}" y="{f(g[1])}" width="{f(g[2] - g[0])}" height="{f(g[3] - g[1])}" fill="none" '
                       f'stroke="{colour}" stroke-width="0.8"/>')

    def text(t, x, y, size, colour="#3c3c3c", rotate=0.0, bold=False):
        weight = ' font-weight="bold"' if bold else ""
        turn = f' transform="rotate({f(rotate)} {x:.1f} {y:.1f})"' if rotate else ""
        t = t.replace("&", "&amp;")
        out.append(f'  <text x="{x:.1f}" y="{y:.1f}" font-size="{f(size)}" fill="{colour}" text-anchor="middle"{weight}{turn}>{t}</text>')

    for d in layout["districts"]:
        if d["name"]:
            text(d["name"], d["lx"], d["ly"], 4.5, "#6a5a48", bold=True)
    for s in layout["streets"]:
        a0, a1 = s["a0"], s["a1"]
        if s["id"] == "main_w":
            a0 = X_IN
        across = (s["c0"] + s["c1"]) / 2
        size = 2.8 if s["kind"] == "lane" else 3.4
        if s["axis"] == "x":
            text(s["name"], (a0 + a1) / 2, across + size * 0.35, size)
        else:
            text(s["name"], across + size * 0.35, (a0 + a1) / 2, size, rotate=90)
    for q in layout["squares"]:
        if q["name"]:
            dy = HALF - 4 if q["id"] == "penny" else 1.2
            text(q["name"], (q["x0"] + q["x1"]) / 2, (q["y0"] + q["y1"]) / 2 + dy, 3.4, "#4a4036")
    for b in layout["buildings"]:
        if b["id"] in MAP_NAMES:
            w, d = SIZES[b["key"]]
            bx0, by0, bx1, by1 = footprint(b["facing"], b["front"], b["a"], w, d)
            text(MAP_NAMES[b["id"]], (bx0 + bx1) / 2, (by0 + by1) / 2 + 1.0, 2.6, "#ffffff" if w >= 15 else "#2e2e2e")
    pl = layout["plaza"]
    text(pl["name"], pl["cx"], pl["cy"] - 5, 2.6, "#4a4036")
    text(pl["stalls"]["name"], pl["cx"], pl["cy"] + 7, 2.4, "#4a4036")
    text("Cathedral", 540, 300, 3.6, "#ffffff")
    text("River Veil", 398, 300, 5, "#2a5a85", rotate=90)
    text("River Veil", 470, 570, 5, "#2a5a85")
    text("The sea", 740, 380, 6, "#2a5a85")
    text("Creaking Gate", 404, 426, 3.4, "#4a4036")
    text("Last Farthing Bridge", 398, 458, 3.0, "#4a4036")
    text("Grubby Bridge", 562, 570, 3.0, "#4a4036")
    text("West Road", 240, 442, 3.4, "#4a4036")
    text("Crooked Hamlet", 300, 418, 4, "#4a4036", bold=True)
    text("Gloomwood", 260, 340, 6, "#3d6a30", bold=True)
    text("Gloomwood", 560, 250, 6, "#3d6a30", bold=True)
    if not player:
        for c in layout["candidates"]:
            text(c["name"].split(" - ")[1], c["x"], c["y"] + c["size"] / 2 + 4, 2.6, "#d23c3c")
        sp = layout["spawn"]
        out.append(f'  <circle cx="{f(sp["x"])}" cy="{f(sp["y"])}" r="1.5" fill="#d23c3c"/>')
        text("start", sp["x"] + 6, sp["y"] + 1, 2.6, "#d23c3c")
        text("Veilmouth, plan C5 (2026-11-10): blocks ringed by pavements, houses flush round their yards; pale strips are "
             "the passages into the yards, red frames the Ritual Site Candidates; neither is on the player's map.", 400, 740, 3.0)
    out.append("</svg>")
    return "\n".join(out) + "\n"


def map_names():
    """Every text the player's map writes, for the string table."""
    return sorted(set(re.findall(r">([^<>]+)</text>", svg(True))))


def main():
    if ERRORS:
        print("VEILMOUTH C5: the plan breaks its rules:")
        for e in ERRORS:
            print("  " + e)
    for n in NOTES:
        print("  note: " + n)
    LAYOUT.write_text(json.dumps(layout, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    PLAN_SVG.write_text(svg(False), encoding="utf-8")
    PLAYER_SVG.write_text(svg(True), encoding="utf-8")
    if "--names" in sys.argv:
        for n in map_names():
            print(n.replace("&amp;", "&"))
    houses = sum(len(b.houses) for b in BLOCKS)
    singles = sum(len(b.placed) + len(b.free) for b in BLOCKS) + 1
    works = sum(1 for bx in layout["boxes"] if bx["material"] == "works")
    passages = len(layout["passages"])
    faced = sum(1 for b in BLOCKS for h in b.houses if h["key"].endswith(("_cornerL", "_cornerR", "_detached")))
    print(f"VEILMOUTH C5: {houses} row houses ({faced} with a faced end), {singles} single buildings, {works} works "
          f"sheds: {houses + singles + works} buildings inside the walls and the works; {len(BLOCKS)} blocks, {passages} "
          f"passages into yards; {len(layout['cottages'])} cottages outside.")
    if "--png" in sys.argv:
        out = sys.argv[sys.argv.index("--png") + 1]
        png(out, (400, 250, 700, 710), 2.4)
        png(out.replace(".png", "_all.png"), (0, 0, 1000, 1000), 0.8)
        print("preview:", out)
    return 1 if ERRORS else 0


if __name__ == "__main__":
    sys.exit(main())
