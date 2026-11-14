"""
Veilmouth on the island: the layout's audit (unity/Assets/Plans/ISLAND_WORLD_AUDIT.md).

Reads the island's layout and C5's and measures what the owner's walk of 2026-11-14 found, from the data alone:
  - slots between houses narrower than a passage, and row ends with no wall against them;
  - rows of one block that meet at a corner on different levels, by the district's own ground plane;
  - each bridge's ends against the streets it should carry;
  - how much of the town's extent is built.
It changes nothing. Run:  python tools/slice/veilmouth_island_audit.py
A later layout must bring every count of faults here to zero; the script's exit code is the number of kinds that fail.
"""
import contextlib
import io
import json
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
with contextlib.redirect_stdout(io.StringIO()):
    import veilmouth_c5_plan as c5   # its sizes, families and footprint rule; importing it writes nothing

MAPS = os.path.join(HERE, "..", "..", "unity", "Assets", "Data", "Slice", "Map")
ISLAND = json.load(open(os.path.join(MAPS, "VeilmouthIslandLayout.json"), encoding="utf-8"))
C5 = json.load(open(os.path.join(MAPS, "VeilmouthC5Layout.json"), encoding="utf-8"))

# A join of two rows reads as one building while their floors differ by no more than this, m (authoring default:
# the kit's plinth course is 0.15 m).
LEVEL = 0.15

SIZE = dict(c5.SIZES)
for _family, _spec in c5.FAMILIES.items():
    for _name, _width, *_ in _spec[2]:
        SIZE.setdefault(_name, (_width, _spec[1]))


def size(key):
    base = key
    for suffix in ("_cornerL", "_cornerR", "_detached"):
        if base.endswith(suffix) and base not in SIZE:
            base = base[: -len(suffix)]
    return SIZE.get(base) or SIZE.get(base.split("_")[0])


def rect(b):
    s = size(b["key"])
    return c5.footprint(b["facing"], b["front"], b["a"], s[0], s[1]) if s else None


def faced(b):
    """Whether the house's low and high ends along its front have a wall of their own."""
    key, low_is_left = b["key"], b["facing"] in "NE"
    if key.endswith("_detached") or key in c5.SIZES:
        return True, True
    if key.endswith("_cornerL"):
        return (True, False) if low_is_left else (False, True)
    if key.endswith("_cornerR"):
        return (False, True) if low_is_left else (True, False)
    return False, False


def houses(buildings):
    return [(b, r) for b, r in ((b, rect(b)) for b in buildings) if r is not None]


def open_ends(buildings):
    """Row ends with no wall of their own and no building against them across their depth."""
    out, rs = [], houses(buildings)
    for b, r in rs:
        along_x = b["facing"] in "NS"
        lo, hi = (r[0], r[2]) if along_x else (r[1], r[3])
        c0, c1 = (r[1], r[3]) if along_x else (r[0], r[2])
        for side, has_wall, at in (("low", faced(b)[0], lo), ("high", faced(b)[1], hi)):
            if has_wall:
                continue
            cover = 0.0
            for o, q in rs:
                if o is b:
                    continue
                qlo, qhi = (q[0], q[2]) if along_x else (q[1], q[3])
                qc0, qc1 = (q[1], q[3]) if along_x else (q[0], q[2])
                if abs((qhi if side == "low" else qlo) - at) < 0.26 or qlo < at - 0.01 < qhi or qlo < at + 0.01 < qhi:
                    cover = max(cover, min(c1, qc1) - max(c0, qc0))
            if cover < (c1 - c0) - 0.6:
                out.append((b.get("block"), b["key"], b["facing"], b["front"], b["a"], side))
    return out


def slots(passages):
    return [(p["block"], p["facing"], p["a0"], p["a1"]) for p in passages if p["a1"] - p["a0"] < c5.PASSAGE - 0.01]


def corner_levels(frame):
    """Rows of one block at right angles whose houses touch, and the difference of the levels they stand on."""
    g = frame.get("ground")
    if not g:
        return []
    plane = lambda u, v: g["middle"] + g["alongU"] * (u - frame["U"] / 2) + g["alongV"] * (v - frame["V"] / 2)
    rows = {}
    for b, r in houses(frame["buildings"]):
        if b.get("block"):
            rows.setdefault((b["block"], b["facing"], b["front"]), []).append(r)
    # A row stands on the highest ground under it (the builder's OwnFloor).
    level = {k: max(plane(x, y) for r in rs for x in (r[0], r[2]) for y in (r[1], r[3])) for k, rs in rows.items()}
    out, keys = [], list(rows)
    for i, k1 in enumerate(keys):
        for k2 in keys[i + 1:]:
            if k1[0] != k2[0] or (k1[1] in "NS") == (k2[1] in "NS"):
                continue
            if any(min(r[2], q[2]) - max(r[0], q[0]) > -0.3 and min(r[3], q[3]) - max(r[1], q[1]) > -0.3 for r in rows[k1] for q in rows[k2]):
                out.append((k1[0], k1[1] + k2[1], abs(level[k1] - level[k2])))
    return out


def axes(f):
    a = math.radians(f["angle"])
    return (math.cos(a), math.sin(a)), (-math.sin(a), math.cos(a))


def to_plan(f, u, v):
    (ux, uy), (vx, vy) = axes(f)
    return f["origin"]["x"] + u * ux + v * vx, f["origin"]["y"] + u * uy + v * vy


def to_local(f, x, y):
    (ux, uy), (vx, vy) = axes(f)
    dx, dy = x - f["origin"]["x"], y - f["origin"]["y"]
    return dx * ux + dy * uy, dx * vx + dy * vy


def bridge_ends(layout):
    """Each end of each bridge: how far outside the nearest district it lies, how far its line is turned off that
    district's streets, and the street it lands on."""
    out, town = [], [f for f in layout["frames"] if f.get("streets")]
    for b in layout.get("bridges", []):
        far = lambda x, y: min(max(-u, u - f["U"], -v, v - f["V"], 0.0) for f in town for u, v in [to_local(f, x, y)])
        if far(b["ax"], b["ay"]) > 60 and far(b["bx"], b["by"]) > 60:
            continue   # a bridge out in the country carries a road, not a street of the town
        for end, (x, y) in (("a", (b["ax"], b["ay"])), ("b", (b["bx"], b["by"]))):
            outside, f, u, v = min(((max(-u, u - f["U"], -v, v - f["V"], 0.0), f, u, v) for f in town for u, v in [to_local(f, x, y)]), key=lambda t: t[0])
            u0, v0 = to_local(f, b["ax"], b["ay"])
            u1, v1 = to_local(f, b["bx"], b["by"])
            turn = math.degrees(math.atan2(v1 - v0, u1 - u0)) % 90
            e = 0.05
            on = [s["id"] for s in f["streets"]
                  if (s["axis"] == "x" and s["c0"] - e <= v <= s["c1"] + e and s["a0"] - 1 <= u <= s["a1"] + 1)
                  or (s["axis"] == "y" and s["c0"] - e <= u <= s["c1"] + e and s["a0"] - 1 <= v <= s["a1"] + 1)]
            # Or on a square named for it: there the streets begin, and the bridge needs no street's line.
            square = [p["name"] for p in layout.get("pads", []) if p["kind"] == "square" and in_polygon([(q["x"], q["y"]) for q in p["line"]], x, y)]
            out.append((b["name"], end, f["name"], outside, 0.0 if square and not on else min(turn, 90 - turn), on + ["the square " + n for n in square]))
    return out


def in_polygon(poly, x, y):
    inside = False
    for k in range(len(poly)):
        (x0, y0), (x1, y1) = poly[k], poly[(k + 1) % len(poly)]
        if (y0 > y) != (y1 > y) and x < x0 + (y - y0) * (x1 - x0) / (y1 - y0):
            inside = not inside
    return inside


def hull_area(points):
    P = sorted(set(points))
    cross = lambda o, a, b: (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in P:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(P):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    H = lower[:-1] + upper[:-1]
    return abs(sum(H[i][0] * H[(i + 1) % len(H)][1] - H[(i + 1) % len(H)][0] * H[i][1] for i in range(len(H)))) / 2


def built(buildings, boxes=()):
    """Ground under houses, and under the stone masses that stand for the works and the cathedral."""
    return sum((r[2] - r[0]) * (r[3] - r[1]) for _, r in houses(buildings)) + sum((b["x1"] - b["x0"]) * (b["y1"] - b["y0"]) for b in boxes if b.get("material") != "cathedral")


def town_ground():
    """The exported height map inside the town's line and inside each terrace: how much of the dry ground stands on
    its level, and how far the rest lies from it. Returns the lines to print and whether the ground passes."""
    import gzip
    import numpy as np
    sys.path.insert(0, os.path.join(HERE, "island"))
    with contextlib.redirect_stdout(io.StringIO()):
        import islandgen as g
        from concept_b import CONCEPT
    town = CONCEPT.get("town")
    if not town:
        return ["  the concept names no town: each district has a ground of its own"], False
    path = os.path.join(MAPS, "..", "Island", "island_heights.bytes")
    raw = np.frombuffer(gzip.open(path, "rb").read(), "<u2")
    n = int(round(math.sqrt(raw.size)))
    H = raw.reshape(n, n)[::-1].astype(np.float64) / 65535.0 * g.BUDGET - g.SEA_LEVEL   # row 0 the plan's north
    axis = np.linspace(0.0, 1000.0, n)
    out, ok = [], True
    taken = np.zeros((n, n), bool)
    for part in town.get("terraces", []) + [town]:
        inside = g.poly_mask(np.array(part["poly"], float), axis, axis) & ~taken
        taken |= inside
        if part is not town:
            # The ground round a terrace is its foot or its wall's, not the town's level.
            for _ in range(int(math.ceil(part.get("foot", 2.5) * 1.2 * (n - 1) / 1000.0))):
                taken[1:, :] |= taken[:-1, :]; taken[:-1, :] |= taken[1:, :]
                taken[:, 1:] |= taken[:, :-1]; taken[:, :-1] |= taken[:, 1:]
        dry = inside & (H > 0.3)
        off = np.abs(H[dry] - part["level"])
        on = float((off <= 0.05).mean())
        # What is off the level and dry: the river's walls are a metre or two wide, so a few per cent is the channels' edge.
        out.append("  %-10s level %.1f m over the tide: %.0f m2 dry, %.1f %% within 0.05 m of its level, %.1f %% over 0.5 m off it (the water's edge); highest %.2f m, lowest dry %.2f m"
                   % (part.get("name", "the town"), part["level"], dry.sum() * (1000.0 / (n - 1)) ** 2, 100 * on, 100 * float((off > 0.5).mean()), H[dry].max(), H[dry].min()))
        ok &= on > 0.93 and H[dry].max() < part["level"] + 0.06
    return out, ok


def main():
    fails = 0
    lines, level = town_ground()
    print("The town's ground, from the exported height map")
    print("\n".join(lines))
    fails += not level
    print()
    print("C5, the city in production: %d buildings; slots %d; open row ends %d; its ground is level, so its rows meet on one level"
          % (len(C5["buildings"]), len(slots(C5["passages"])), len(open_ends(C5["buildings"]))))
    blocks = C5["blocks"]
    extent = (max(b["x1"] for b in blocks) - min(b["x0"] for b in blocks)) * (max(b["y1"] for b in blocks) - min(b["y0"] for b in blocks))
    print("   built %.0f m2: %.0f %% of its blocks, %.0f %% of the rectangle round them" % (
        built(C5["buildings"]), 100 * built(C5["buildings"]) / sum((b["x1"] - b["x0"]) * (b["y1"] - b["y0"]) for b in blocks),
        100 * built(C5["buildings"]) / extent))

    town = [f for f in ISLAND["frames"] if f.get("blocks")]
    print("\nThe island, layout %s: %d districts" % (ISLAND["version"], len(town)))
    all_slots = all_open = all_apart = 0
    worst = 0.0
    for f in town:
        s, o, joins = slots(f["passages"]), open_ends(f["buildings"]), corner_levels(f)
        apart = [j for j in joins if j[2] > LEVEL]
        top = max([j[2] for j in joins] or [0.0])
        g = f["ground"]
        fall = abs(g["alongU"]) * f["U"] + abs(g["alongV"]) * f["V"]
        frame = f["U"] * f["V"]
        streets = sum((st["a1"] - st["a0"]) * (st["c1"] - st["c0"]) for st in f["streets"])
        print("  %-15s %3d buildings; slots under %g m: %d %s; open row ends: %d; ground falls %.1f m corner to corner: %d of %d corner joins over %.2f m apart, %.2f m at most"
              % (f["name"], len(f["buildings"]), c5.PASSAGE, len(s), [(x[0], x[3] - x[2]) for x in s] if s else "", len(o), fall, len(apart), len(joins), LEVEL, top))
        print("  %-15s built %.0f m2 of %.0f m2 (%.0f %%); streets with their pavements %.0f m2 (%.0f %%)"
              % ("", built(f["buildings"], f["boxes"]), frame, 100 * built(f["buildings"], f["boxes"]) / frame, streets, 100 * streets / frame))
        all_slots, all_open, all_apart, worst = all_slots + len(s), all_open + len(o), all_apart + len(apart), max(worst, top)
    print("  in all: %d slots, %d open row ends, %d corner joins apart in level (%.2f m at most)" % (all_slots, all_open, all_apart, worst))
    fails += (all_slots > 0) + (all_open > 0) + (all_apart > 0)

    print("\nBridges against the streets they should carry")
    loose = 0
    for name, end, district, outside, turn, on in bridge_ends(ISLAND):
        ok = on and turn < 0.5
        loose += not ok
        print("  %-22s end %s: %5.1f m outside %-15s turned %4.1f deg off its streets; on %s" % (name, end, outside, district + ";", turn, ", ".join(on) if on else "no street"))
    print("  in all: %d of %d ends do not continue a street" % (loose, len(bridge_ends(ISLAND))))
    fails += loose > 0

    area = hull_area([to_plan(f, u, v) for f in town for u in (0, f["U"]) for v in (0, f["V"])])
    frames = sum(f["U"] * f["V"] for f in town)
    total = sum(built(f["buildings"], f["boxes"]) for f in town)
    print("\nThe town's extent (the hull round its districts): %.0f m2; inside a district %.0f %%; built %.1f %% of the extent, %.0f %% of the districts"
          % (area, 100 * frames / area, 100 * total / area, 100 * total / frames))
    return fails


if __name__ == "__main__":
    sys.exit(main())
