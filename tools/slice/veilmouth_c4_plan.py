"""Veilmouth's city plan C4 (VEILMOUTH_CITY_PLAN.md §28): the one source of its layout.

The owner's map of 2026-11-10 is a scheme (§28.1): this file turns it into streets, squares, rows, plots and single
buildings in plan metres, and writes

* the layout the Unity builder reads: unity/Assets/Data/Slice/Map/VeilmouthC4Layout.json;
* the planner's plan with its notes: unity/Assets/Plans/VEILMOUTH_CITY_PLAN_C4.svg;
* the player's map without them: unity/Assets/Data/Slice/Map/VeilmouthC4Plan.xml (CityMapElement's subset);
* a preview PNG, when a path is given: --png <path>.

Plan coordinates as everywhere in the city plan: px runs east and py south from the area's north-west corner, 1 m each,
so world X = px - 500 and world Z = 500 - py; the area is 1000 x 1000 m. Every size here is an authoring default unless
its comment says measured.

Run: python tools/slice/veilmouth_c4_plan.py [--png path]
"""

import json
import math
import struct
import sys
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYOUT = ROOT / "unity/Assets/Data/Slice/Map/VeilmouthC4Layout.json"
PLAN_SVG = ROOT / "unity/Assets/Plans/VEILMOUTH_CITY_PLAN_C4.svg"
PLAYER_SVG = ROOT / "unity/Assets/Data/Slice/Map/VeilmouthC4Plan.xml"

# ----------------------------------------------------------------------------------------------- street sections

# §28.1 item 4: (pavement on the low side, carriageway, pavement on the high side) in metres, and the tree rows.
SECTIONS = {
    "main": (6.0, 10.0, 6.0),
    "ring": (6.0, 8.0, 6.0),
    "side": (6.0, 7.0, 4.0),        # the 6 m pavement carries the one row of trees; flipped by "flip"
    "embankment": (3.0, 7.0, 6.0),  # a 3 m pavement before the houses (authoring default), the promenade of 6 m by the river
    "works": (2.5, 9.0, 2.5),
    "lane": (0.0, 5.0, 0.0),
    "alley": (0.0, 6.0, 0.0),
}

layout = {
    "version": "C4",
    "streets": [], "squares": [], "rows": [], "plots": [], "buildings": [], "boxes": [], "water": [], "walls": [],
    "towers": [], "bridges": [], "slants": [], "districts": [], "labels": [], "candidates": [], "zones": [],
}


def street(sid, name, axis, a0, a1, c0, kind, flip=False, surface="setts", trees=None):
    """A street along x (axis "x": from a0 to a1, its low edge at py c0) or along y; its width follows its section."""
    w0, carriage, w1 = SECTIONS[kind]
    if flip:
        w0, w1 = w1, w0
    c1 = c0 + w0 + carriage + w1
    if trees is None:
        trees = {"main": "both", "ring": "both", "side": "wide", "embankment": "high"}.get(kind, "none")
    layout["streets"].append({
        "id": sid, "name": name, "kind": kind, "axis": axis, "a0": a0, "a1": a1, "c0": c0, "c1": c1,
        "walk0": w0, "walk1": w1, "surface": surface, "trees": trees,
    })
    return c1


def square(sid, name, x0, y0, x1, y1, surface="square", label=True):
    layout["squares"].append({"id": sid, "name": name if label else "", "x0": x0, "y0": y0, "x1": x1, "y1": y1, "surface": surface})


def row(facing, front, a0, a1, family, seed, stoop="plain"):
    layout["rows"].append({"facing": facing, "front": front, "a0": a0, "a1": a1, "family": family, "seed": seed, "stoop": stoop})


def plot(facing, front, s0, s1, kind, garden, behind, seed, name="", at=-1.0):
    layout["plots"].append({"facing": facing, "front": front, "s0": s0, "s1": s1, "kind": kind, "garden": garden,
                            "behind": behind, "seed": seed, "name": name, "at": at})


def building(bid, key, facing, front, a, name, stoop="plain"):
    layout["buildings"].append({"id": bid, "key": key, "facing": facing, "front": front, "a": a, "name": name, "stoop": stoop})


def box(bid, name, x0, y0, x1, y1, height, material="works", base=0.0):
    layout["boxes"].append({"id": bid, "name": name, "x0": x0, "y0": y0, "x1": x1, "y1": y1, "height": height,
                            "material": material, "base": base})


def district(name, x0, y0, x1, y1, colour, label=True):
    layout["districts"].append({"name": name if label else "", "x0": x0, "y0": y0, "x1": x1, "y1": y1, "colour": colour})


def label(text, x, y, size=4.0, colour="#3c3c3c", rotate=0.0, player=True):
    layout["labels"].append({"text": text, "x": x, "y": y, "size": size, "colour": colour, "rotate": rotate, "player": player})


# ----------------------------------------------------------------------------------------------- the land and the water

# The river round the west and the south, into the sea on the east (§28.1 item 1); one water plane at 24 m (item 13).
C = (540.0, 440.0)   # the Penny's centre, the statue's foot
HALF = 40.0          # the Penny's facades stand 40 m from its centre, so a 30 m civic front fits each half of a side
WALL_W, WALL_N = 418, 258
X_IN = WALL_W + 2    # the city's inside, west
QUAY = 660           # the quay's inner edge; the quay runs to the coast
COAST = 680          # the city's quay wall; the sea is east of it from py 250 to 710
COAST_OPEN = 700     # the natural coast north and south of the city, with rocks along it
EMBANK = 534         # the embankment's north edge: py 534-550, the river's bank at 550
RIVER_W = (380, 0, 416, 586)
RIVER_S = (380, 550, COAST + 20, 586)
layout["water"] += [
    {"name": "River Veil", "kind": "river", "x0": RIVER_W[0], "y0": RIVER_W[1], "x1": RIVER_W[2], "y1": RIVER_W[3]},
    {"name": "", "kind": "river", "x0": RIVER_S[0], "y0": RIVER_S[1], "x1": RIVER_S[2], "y1": RIVER_S[3]},
    {"name": "The sea", "kind": "sea", "x0": COAST, "y0": 250, "x1": 1000, "y1": 710},
    {"name": "", "kind": "sea", "x0": COAST_OPEN, "y0": 0, "x1": 1000, "y1": 1000},
]

# ----------------------------------------------------------------------------------------------- the town wall

# A stone wall of 2 m and 6 m over the ground on the north and along the river's west bank (§28.1 item 14), the gate in
# it on its bridge: the one way in from the land (item 1). Its towers are the kit's Tower5 (5 x 5 m), 3 m proud of it.
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

# ----------------------------------------------------------------------------------------------- streets

MAIN = 429           # the main street's north edge: py 429-451
street("main_w", "Long Tongue", "x", X_IN, C[0] - HALF, MAIN, "main")
street("main_e", "Salt Gullet", "x", C[0] + HALF, QUAY, MAIN, "main")
WIDOWS = 352
WIDOWS_S = street("widows", "Widows' Row", "x", X_IN, QUAY, WIDOWS, "side")           # 352-369, trees on the north
street("lenten", "Lenten Street", "y", WIDOWS_S, C[1] - HALF, 530, "ring")
street("smoked", "Smoked Street", "y", C[1] + HALF, EMBANK, 530, "ring")
street("embankment", "Powder Embankment", "x", X_IN, QUAY, EMBANK, "embankment")      # 534-550, the promenade by the river
street("last_turning", "Last Turning", "y", WALL_N + 2, WIDOWS, 434, "lane", surface="cobbles")
DRAUGHT = 474
street("draught", "Draught Lane", "x", 612, QUAY, DRAUGHT, "lane", surface="cobbles")

# The 45° street from the Penny's south-east corner to the Black Puddle (§28.1 item 2, §28.2): 8 m of setts, no pavements.
layout["slants"].append({"name": "Squint Lane", "x0": 576.0, "y0": 476.0, "x1": 607.0, "y1": 507.0, "width": 8.0, "family": "low"})

# The works over the river (§28.1 item 11): a road from Grubby Bridge, one across, an alley through the workers' slum,
# and a wall of 4 m round them on the land's sides.
WORKS = (X_IN, RIVER_S[3], COAST, 702)
street("works_s", "Furnace Road", "y", RIVER_S[3] + 2, WORKS[3], 533, "works")
street("works_x", "Clinker Road", "x", X_IN + 2, COAST, 640, "works")
street("rats_choir", "Rats' Choir", "x", X_IN + 4, 533, 672, "alley", surface="cobbles")
layout["walls"] += [
    {"name": "Works wall (west)", "axis": "y", "t0": WORKS[0] - 2, "t1": WORKS[0], "a0": RIVER_S[3], "a1": WORKS[3] + 2, "height": 4.0, "gaps": []},
    {"name": "Works wall (south)", "axis": "x", "t0": WORKS[3], "t1": WORKS[3] + 2, "a0": WORKS[0] - 2, "a1": COAST, "height": 4.0, "gaps": []},
]

# The road west from the gate through the village into the forest (§28.1 item 15).
layout["roads"] = [{"name": "West Road", "x0": 0, "y0": 434, "x1": RIVER_W[0] - 2, "y1": 446}]

# ----------------------------------------------------------------------------------------------- squares

square("penny", "The Penny", C[0] - HALF, C[1] - HALF, C[0] + HALF, C[1] + HALF)
layout["plaza"] = {"cx": C[0], "cy": C[1], "half": HALF, "ringOuter": 34.0, "ringInner": 26.0, "island": 26.0,
                   "plinth": 4.0, "statue": 12.0, "name": "Stone Grandad"}
square("drowsy", "Drowsy Steps", 505, 322, 575, WIDOWS)
square("bawling", "Bawling Square", 432, MAIN + 22, 470, 500)
square("black_puddle", "Black Puddle", 606, 496, 640, EMBANK)
square("quay", "Fishlip Quay", QUAY, WIDOWS_S, COAST, RIVER_S[1], surface="setts")
square("tipsy", "Tipsy Pier", COAST, 433, 742, 447, surface="setts")
square("one_eye_pier", "", COAST, 520, 752, 530, surface="setts", label=False)
square("cemetery", "Last Turning Cemetery", 440, WALL_N + 4, 478, 334, surface="grass")
square("chapel_court", "", 446, 336, 478, WIDOWS, surface="cobbles", label=False)
square("bishops", "Bishop's Garden", 480, WALL_N + 4, 503, WIDOWS, surface="grass")
square("quiet", "Quiet Whistle Garden", 577, WALL_N + 4, 618, WIDOWS, surface="grass")
square("garth", "", 635, 292, 665, 322, surface="grass", label=False)
square("deaf_boiler", "Deaf Boiler Yard", 600, 658, 636, 694, surface="cobbles")
square("works_gate", "", 532, RIVER_S[3] + 2, 548, 600, surface="setts", label=False)

# ----------------------------------------------------------------------------------------------- buildings

# The centre (§28.1 item 8): the Town Hall and its clock tower, the Assize with the Wardens, the Governor's House.
building("town_hall", "Civic_rich", "S", C[1] - HALF, C[0] - HALF, "The Town Hall")
building("clock_tower", "Tower6_white", "S", C[1] - HALF, C[0] - HALF - 5, "The clock tower")
building("assize", "Civic_stucco", "S", C[1] - HALF, 550, "The Assize and the Wardens' station")
building("governor", "Civic_white", "N", C[1] + HALF, C[0] - HALF, "The Governor's House")
# The religious quarter (item 9): the old chapel before its cemetery, the Brothers' House round its garth.
building("chapel", "Chapel", "S", 336, 455, "The old chapel")
building("monastery_n", "Civic_stucco", "S", 292, 635, "The Brothers' House - north range")
building("monastery_w", "Civic_stucco", "E", 635, 292, "The Brothers' House - west range")
building("monastery_e", "Civic_stucco", "W", 665, 292, "The Brothers' House - east range")
building("bell_tower", "Tower5_dark", "S", 327, 625, "The Brothers' bell tower")
# The port (item 12): the Customs House on the quay, the One-Eye on its pier.
building("customs", "Civic_white", "E", QUAY, 375, "The Customs House")
building("one_eye", "Tower6_white", "W", 745, 520, "The One-Eye")
# Lower Veil: the Soot & Bell and Harrow & Tincture on the Black Puddle; the missing person's tenement on its north side.
building("soot_bell", "Tavern_brick", "E", 606, 504, "The Soot & Bell")
building("harrow", "ShopTown8_dark_detached", "E", 606, 523, "Harrow & Tincture")  # a 4 m passage from the Soot & Bell (R5)
building("missing", "Tenement8_poor_detached", "S", 496, 608, "The missing person's tenement")

# The cathedral, a placeholder of stone masses until its model comes (item 9, SLICE_MODELS_WANTED.md § C4).
box("nave", "The cathedral - nave", 528, 270, 552, 306, 22.0, "cathedral")
box("transept", "The cathedral - transept", 514, 282, 566, 298, 20.0, "cathedral")
box("apse", "The cathedral - apse", 532, 264, 548, 270, 16.0, "cathedral")
box("west_front", "The cathedral - tower", 530, 306, 550, 322, 44.0, "cathedral")

# The works' plain boxes (item 11): colliders, no props; heights 9-16 m.
for i, (x0, y0, x1, y1, h) in enumerate([
    (424, 590, 474, 636, 14.0), (480, 590, 528, 636, 10.0),
    (552, 590, 612, 636, 16.0), (618, 590, 676, 636, 12.0),
    (552, 658, 596, 698, 12.0), (640, 658, 676, 698, 14.0),
]):
    box(f"works_{i + 1}", f"Works block {i + 1}", x0, y0, x1, y1, h)

# ----------------------------------------------------------------------------------------------- rows and plots

# Wealth falls with distance from the Penny (item 7): rich shops and civic fronts on it, shops on the main streets and
# round Bawling Square, villas and detached houses on Widows' Row and the embankment, tenements toward Lower Veil, low
# tenements in the slums by the port, by the cemetery, in Squint Lane and in the works.

# The Penny's own fronts.
row("E", C[0] - HALF, C[1] - HALF, MAIN, "shop_rich", 101)              # west side, north half
row("E", C[0] - HALF, MAIN + 22, C[1] + HALF, "shop_rich", 102)        # west side, south half
row("W", C[0] + HALF, C[1] - HALF, MAIN, "shop_rich", 103)              # east side, north half
row("W", C[0] + HALF, MAIN + 22, 470, "shop", 104)                      # east side, south half, short of Squint Lane
row("N", C[1] + HALF, 550, 570, "shop", 105)                            # south side, east half, short of Squint Lane

# Long Tongue, the gate to the Penny; Bawling Square opens off its south side by the gate, shops round it.
row("S", MAIN, X_IN + 2, 490, "shop", 111)
row("W", 470, MAIN + 22, 500, "shop", 113)
row("N", 500, 432, 470, "shop", 114)
row("E", 432, MAIN + 22, 500, "shop", 115)                              # against the town wall, to the square's corner
# Salt Gullet, the Penny to the quay.
row("S", MAIN, 590, 645, "shop", 121)
row("N", MAIN + 22, 590, 650, "town", 122)

# Widows' Row's south side: detached houses and villas on their plots (§19), railed on its pavement.
plot("N", WIDOWS_S + 4, X_IN + 2, 438, "detached", 4, 2, 141)
plot("N", WIDOWS_S + 4, 438, 454, "detached", 4, 2, 142)
plot("N", WIDOWS_S + 4, 456, 490, "villa", 4, 6, 143)
plot("N", WIDOWS_S + 4, 582, 613, "villa", 4, 6, 144)
plot("N", WIDOWS_S + 4, 613, 644, "villa", 4, 6, 145)
# The faction's house by the cathedral (item 9), on its plot in Quiet Whistle Garden.
plot("S", WIDOWS - 6, 580, 614, "villa_stucco", 6, 15, 160, name="The faction's house")
# The embankment's north side: detached houses on their plots with the river before them.
for i, (s0, s1) in enumerate([(X_IN + 2, 446), (446, 470), (470, 494), (494, 519)]):
    plot("S", EMBANK - 4, s0, s1, "detached", 4, 6, 170 + i)

# Smoked Street.
row("E", 530, C[1] + HALF + 15, EMBANK, "town", 181, stoop="rich")
row("W", 550, C[1] + HALF + 10, EMBANK, "tenement", 182)
# Lower Veil: the embankment's tenements, the Black Puddle's sides, the quay's warehouses.
row("S", EMBANK, 560, 590, "tenement", 191)
row("S", 496, 618, 640, "low", 192)
row("W", 640, 496, EMBANK, "low", 193)
row("E", QUAY, 414, MAIN, "warehouse", 201)
row("E", QUAY, MAIN + 22, DRAUGHT, "warehouse", 202)
row("E", QUAY, DRAUGHT + 5, 496, "warehouse", 203)
row("E", QUAY, 497, EMBANK, "warehouse", 204)
# The port's slum on Draught Lane, between Salt Gullet's backs and the Black Puddle.
row("S", DRAUGHT, 618, 652, "low", 205)
row("N", DRAUGHT + 5, 618, 652, "low", 206)

# The slum by the cemetery (item 7): Parsons' End on Last Turning.
row("E", 434, WALL_N + 12, 310, "low", 231)
# The workers' slum in the works, Rats' Choir.
row("S", 672, X_IN + 6, 466, "low", 241)
row("N", 678, X_IN + 6, 466, "low", 242)

# ----------------------------------------------------------------------------------------------- the outskirts

# The village across the river (item 15): cottages by the West Road, then the forest.
layout["cottages"] = [
    {"key": "House_Farm", "facing": "S", "front": 428, "a": 300}, {"key": "House_Farm", "facing": "S", "front": 428, "a": 326},
    {"key": "House_Farm", "facing": "N", "front": 452, "a": 292}, {"key": "House_Farm", "facing": "N", "front": 452, "a": 322},
    {"key": "House_Farm", "facing": "S", "front": 428, "a": 262}, {"key": "House_Farm", "facing": "N", "front": 452, "a": 256},
    {"key": "TenementLow8_poor_detached", "facing": "N", "front": 452, "a": 344},
    {"key": "TenementLow8_poor_detached", "facing": "S", "front": 428, "a": 350},
]
layout["forest"] = {"rects": [
    {"x0": 20, "y0": 20, "x1": COAST_OPEN - 10, "y1": 236},     # north of the wall
    {"x0": 20, "y0": 236, "x1": 370, "y1": 404},                # west, north of the village
    {"x0": 20, "y0": 476, "x1": 370, "y1": 980},                # west, south of the village
    {"x0": 370, "y0": 600, "x1": WORKS[0] - 8, "y1": 980},      # south-west, by the works
    {"x0": WORKS[0] - 8, "y0": 720, "x1": COAST_OPEN - 10, "y1": 980},  # south of the works
]}

# ----------------------------------------------------------------------------------------------- districts, labels, zones

district("", 470, WIDOWS_S, 606, 496, "#efe2c8", label=False)   # the Penny's square names it
district("PARSONS' END", X_IN, WALL_N + 2, COAST, WIDOWS, "#e3d6f0")
district("SLEEPY QUARTER", X_IN, WIDOWS_S, 530, EMBANK, "#f6e3c4")
district("SALT END", 550, WIDOWS_S, QUAY, MAIN + 22, "#e8dcc8")
district("LOWER VEIL", 550, MAIN + 22, QUAY, RIVER_S[1], "#e9cfc7")
district("THE REEK", WORKS[0], WORKS[1], WORKS[2], WORKS[3], "#d8d2c8")
district("", QUAY, WIDOWS_S, COAST, RIVER_S[1], "#d6d0c4", label=False)

layout["candidates"] += [
    {"name": "Ritual Site Candidate 1 - Black Puddle", "x": 623, "y": 518, "size": 26},
    {"name": "Ritual Site Candidate 2 - Deaf Boiler Yard", "x": 618, "y": 676, "size": 26},
    {"name": "Ritual Site Candidate 3 - Old Chapel", "x": 462, "y": 344, "size": 14},
]
layout["spawn"] = {"x": 620, "y": 541}   # on the embankment before the Black Puddle and the Soot & Bell

# ----------------------------------------------------------------------------------------------- counts


WIDTHS = {"shop": 11.5, "shop_rich": 12.5, "town": 10.0, "tenement": 11.5, "low": 10.0, "warehouse": 15.0}


def estimate():
    rows = 0
    for r in layout["rows"]:
        length = r["a1"] - r["a0"]
        rows += max(0, int((length + 2) // (WIDTHS[r["family"]] + 0.8)))
    slants = sum(2 * int((math.dist((s["x0"], s["y0"]), (s["x1"], s["y1"])) - 12) // 10) for s in layout["slants"])
    plots = len(layout["plots"])
    singles = len(layout["buildings"])
    works = sum(1 for b in layout["boxes"] if b["material"] == "works")
    return rows, slants, plots, singles, works


# ----------------------------------------------------------------------------------------------- drawing

COLOURS = {
    "land": "#d9e8bf", "water": "#7fb2d9", "walk": "#c8c8c8", "road": "#9a9a9a", "lane": "#b9b9b9", "square": "#d2cdc2",
    "grass": "#a9cf8e", "cobbles": "#c4bcae", "setts": "#a9a9a9", "wall": "#6d6253", "house": "#8a7a68", "civic": "#6f5f8f",
    "works": "#8d8d8d", "cathedral": "#7d6f9f", "forest": "#9cc48a", "bridge": "#b7a98c", "plot": "#b9d6a2", "statue": "#4a4036",
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


def footprint(facing, front, a, width, depth):
    if facing == "S":
        return a, front - depth, a + width, front
    if facing == "N":
        return a, front, a + width, front + depth
    if facing == "E":
        return front - depth, a, front, a + width
    return front, a, front + depth, a + width


def plot_quad(facing, front, s0, s1, depth, garden, behind):
    """VeilmouthCity.PlotQuad: from the garden's railing before the front to the plot's back behind the house."""
    s = {"N": -1, "S": 1, "E": 1, "W": -1}[facing]
    c0, c1 = front + s * garden, front - s * (depth + behind)
    lo, hi = min(c0, c1), max(c0, c1)
    return (s0, lo, s1, hi) if facing in ("N", "S") else (lo, s0, hi, s1)


SIZES = {  # width along the front, depth (m), measured from the baked kit houses and the wrappers
    "Civic_rich": (30, 15), "Civic_stucco": (30, 15), "Civic_white": (30, 15), "Tower6_white": (5, 5), "Tower5_dark": (5, 5),
    "Chapel": (13, 10), "Tavern_brick": (15, 15), "ShopTown8_dark_detached": (10, 10), "Tenement8_poor_detached": (10, 8),
    "House_Farm": (6, 7), "TenementLow8_poor_detached": (10, 8), "villa": (15, 15), "villa_stucco": (15, 15), "detached": (10, 10),
}
DEPTHS = {"shop": 10, "shop_rich": 10, "town": 10, "tenement": 8, "low": 8, "warehouse": 8}


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
    for s in layout["squares"]:
        out.append(("rect", (s["x0"], s["y0"], s["x1"], s["y1"]), COLOURS.get(s["surface"], COLOURS["square"]), True))
    for s in layout["streets"]:
        if s["axis"] == "x":
            whole = (s["a0"], s["c0"], s["a1"], s["c1"])
            road = (s["a0"], s["c0"] + s["walk0"], s["a1"], s["c1"] - s["walk1"])
        else:
            whole = (s["c0"], s["a0"], s["c1"], s["a1"])
            road = (s["c0"] + s["walk0"], s["a0"], s["c1"] - s["walk1"], s["a1"])
        out.append(("rect", whole, COLOURS["walk"], True))
        out.append(("rect", road, COLOURS["lane" if s["kind"] in ("lane", "alley") else "road"], True))
    p = layout["plaza"]
    out.append(("circle", (p["cx"], p["cy"], p["ringOuter"]), COLOURS["road"], True))
    out.append(("circle", (p["cx"], p["cy"], p["island"]), COLOURS["grass"], True))
    out.append(("circle", (p["cx"], p["cy"], p["plinth"]), COLOURS["statue"], True))
    for s in layout["slants"]:
        dx, dy = s["x1"] - s["x0"], s["y1"] - s["y0"]
        n = math.hypot(dx, dy)
        ox, oy = -dy / n * s["width"] / 2, dx / n * s["width"] / 2
        out.append(("poly", [(s["x0"] + ox, s["y0"] + oy), (s["x1"] + ox, s["y1"] + oy), (s["x1"] - ox, s["y1"] - oy), (s["x0"] - ox, s["y0"] - oy)],
                    COLOURS["road"], True))
    for b in layout["bridges"]:
        out.append(("rect", (b["x0"], b["y0"], b["x1"], b["y1"]), COLOURS["bridge"], True))
    for w in layout["walls"]:
        g = (w["t0"], w["a0"], w["t1"], w["a1"]) if w["axis"] == "y" else (w["a0"], w["t0"], w["a1"], w["t1"])
        out.append(("rect", g, COLOURS["wall"], True))
    # Houses as their rows' strips, so the plan shows the blocks without placing each house.
    for r in layout["rows"]:
        out.append(("rect", footprint(r["facing"], r["front"], r["a0"], r["a1"] - r["a0"], DEPTHS[r["family"]]), COLOURS["house"], True))
    for pl in layout["plots"]:
        w, d = SIZES[pl["kind"]]
        out.append(("rect", plot_quad(pl["facing"], pl["front"], pl["s0"], pl["s1"], d, pl["garden"], pl["behind"]), COLOURS["plot"], True))
        a = pl["at"] if pl["at"] >= 0 else pl["s0"] + math.floor((pl["s1"] - pl["s0"] - w) / 2)
        out.append(("rect", footprint(pl["facing"], pl["front"], a, w, d), COLOURS["house"], True))
    for b in layout["buildings"]:
        w, d = SIZES[b["key"]]
        civic = b["key"].startswith(("Civic", "Tower", "Chapel"))
        out.append(("rect", footprint(b["facing"], b["front"], b["a"], w, d), COLOURS["civic" if civic else "house"], True))
    for b in layout["boxes"]:
        out.append(("rect", (b["x0"], b["y0"], b["x1"], b["y1"]), COLOURS[b["material"]], True))
    for t in layout["towers"]:
        out.append(("rect", footprint(t["facing"], t["front"], t["a"], 5, 5), COLOURS["wall"], True))
    for s in layout["slants"]:
        dx, dy = (s["x1"] - s["x0"]), (s["y1"] - s["y0"])
        n = math.hypot(dx, dy)
        ux, uy = dx / n, dy / n
        nx, ny = -uy, ux
        for side in (1, -1):
            a0, a1 = 6, n - 6
            near, far = s["width"] / 2, s["width"] / 2 + 8
            pts = [(s["x0"] + ux * a0 + nx * near * side, s["y0"] + uy * a0 + ny * near * side),
                   (s["x0"] + ux * a1 + nx * near * side, s["y0"] + uy * a1 + ny * near * side),
                   (s["x0"] + ux * a1 + nx * far * side, s["y0"] + uy * a1 + ny * far * side),
                   (s["x0"] + ux * a0 + nx * far * side, s["y0"] + uy * a0 + ny * far * side)]
            out.append(("poly", pts, COLOURS["house"], True))
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
    "chapel": "Old Chapel", "monastery_n": "Brothers' House", "customs": "Customs House", "one_eye": "One-Eye",
    "soot_bell": "Soot & Bell", "harrow": "Harrow",
}


def svg(player):
    """The plan as SVG, in CityMapElement's subset; the player's map leaves out the planner's notes (S1.6)."""
    view = (200, 236, 580, 500) if player else (190, 226, 600, 520)
    x0, y0, w, h = view
    out = [f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{x0} {y0} {w} {h}" font-family="Segoe UI, Arial, sans-serif">']
    if player:
        out.append("  <!-- The player's map (S1.6): VEILMOUTH_CITY_PLAN_C4.svg without the planner's notes. Written by "
                   "tools/slice/veilmouth_c4_plan.py. 1 unit = 1 m; world X = px - 500, world Z = 500 - py. North is up. -->")
    else:
        out.append("  <!-- Veilmouth, city plan C4 (VEILMOUTH_CITY_PLAN.md §28), written by tools/slice/veilmouth_c4_plan.py "
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
            text(d["name"], (d["x0"] + d["x1"]) / 2, d["y0"] + 6, 4.5, "#6a5a48", bold=True)
    for s in layout["streets"]:
        mid = (s["a0"] + s["a1"]) / 2
        across = (s["c0"] + s["c1"]) / 2
        size = 3.2 if s["kind"] in ("lane", "alley") else 3.6
        if s["axis"] == "x":
            text(s["name"], mid, across + size * 0.35, size)
        else:
            text(s["name"], across + size * 0.35, mid, size, rotate=90)
    for s in layout["slants"]:
        text(s["name"], (s["x0"] + s["x1"]) / 2 + 2, (s["y0"] + s["y1"]) / 2, 3.2, rotate=45)
    for q in layout["squares"]:
        if q["name"]:
            text(q["name"], (q["x0"] + q["x1"]) / 2, (q["y0"] + q["y1"]) / 2 + (12 if q["id"] == "penny" else 1.2), 3.6, "#4a4036")
    for b in layout["buildings"]:
        if b["id"] in MAP_NAMES:
            w, d = SIZES[b["key"]]
            bx0, by0, bx1, by1 = footprint(b["facing"], b["front"], b["a"], w, d)
            text(MAP_NAMES[b["id"]], (bx0 + bx1) / 2, (by0 + by1) / 2 + 1.2, 3.0, "#ffffff" if w >= 15 else "#2e2e2e")
    pl = layout["plaza"]
    text(pl["name"], pl["cx"], pl["cy"] - 6, 3.2, "#4a4036")
    text("Cathedral", 540, 292, 3.6, "#ffffff")
    text("Faction House", 597, 312, 3.0, "#2e2e2e")
    text("River Veil", 398, 300, 5, "#2a5a85", rotate=90)
    text("River Veil", 470, 570, 5, "#2a5a85")
    text("The sea", 740, 380, 6, "#2a5a85")
    text("Creaking Gate", 404, 426, 3.4, "#4a4036")
    text("Last Farthing Bridge", 398, 458, 3.0, "#4a4036")
    text("Grubby Bridge", 560, 570, 3.0, "#4a4036")
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
        text("Veilmouth, plan C4 (2026-11-10): red frames are the Ritual Site Candidates, never on the player's map.", 400, 740, 3.2)
    out.append("</svg>")
    return "\n".join(out) + "\n"


def map_names():
    """Every text the player's map writes, for the string table."""
    import re
    return sorted(set(re.findall(r">([^<>]+)</text>", svg(True))))


def main():
    LAYOUT.write_text(json.dumps(layout, indent=1, ensure_ascii=False) + "\n", encoding="utf-8")
    PLAN_SVG.write_text(svg(False), encoding="utf-8")
    PLAYER_SVG.write_text(svg(True), encoding="utf-8")
    if "--names" in sys.argv:
        for n in map_names():
            print(n.replace("&amp;", "&"))
    rows, slants, plots, singles, works = estimate()
    print(f"VEILMOUTH C4: about {rows} row houses, {slants} on the slant, {plots} plots, {singles} single buildings, "
          f"{works} works blocks: about {rows + slants + plots + singles + works} inside the walls and the works; "
          f"{len(layout['cottages'])} cottages outside.")
    if "--png" in sys.argv:
        out = sys.argv[sys.argv.index("--png") + 1]
        png(out, (300, 240, 830, 790), 2.0)
        png(out.replace(".png", "_all.png"), (0, 0, 1000, 1000), 0.8)
        print("preview:", out)


if __name__ == "__main__":
    main()
