"""The approved island as the Unity project's source data: its heights and where each surface lies.

Written by build.py --export into unity/Assets/Data/Slice/Island/:
  island_heights.bytes   gzip of 1025 x 1025 little-endian uint16, row 0 the terrain's south edge (Unity's z = 0),
                         0 the terrain's floor and 65535 its 100 m; Sea Level is 24 m.
  island_surfaces.bytes  gzip of L x 1024 x 1024 uint8 weights, layer by layer, the same row order; they sum to 255.
  island.json            the layers in order, each with its texture set, and the sizes above.
The weights are painted by rule from the finished relief (slope, height, the shore's kind, wet ground, forest, fields,
roads, gullies), each rule a cover with a soft edge broken by noise, laid by priority so that the weights always sum
to one (AGENTS.md, terrain rules). The city's own surfaces are added when the city is built on this ground.
"""
import gzip
import json
import os

import numpy as np

import islandgen as g

# The surfaces in the order of the terrain's layers; each is one Poly Haven set (tools/assets/fetch_terrain_textures.py).
SURFACES = ["grass_ground", "forest_ground_04", "forest_leaves_03", "brown_mud_03", "dirt_floor", "gravel_ground_01",
            "river_small_rocks", "rock_face_03", "mossy_rock", "sand_03", "rocks_ground_06", "leafy_grass"]
GRASS, FLOOR, LEAVES, MUD, DIRT, GRAVEL, STONES, ROCK, MOSS, SAND, SHORE, LUSH = range(12)
ALPHAMAP = 1024
ZONES = 512
ZONE_NAMES = ["forest", "marsh", "farm", "wet", "bare", "sand", "hard"]


def surfaces(C, F):
    seed = C.get("seed", 1)
    H, slope, dl, ds, land = F["H"], F["slope"], F["dl"], F["ds"], F["land"]
    X, Y = g.GX, g.GY
    macro = g.fbm(X, Y, 95.0, seed + 201, 4)
    mid = g.fbm(X, Y, 34.0, seed + 203, 3)
    fine = g.fbm(X, Y, 9.0, seed + 205, 3)
    W = np.zeros((len(SURFACES),) + H.shape, np.float32)
    W[GRASS] = 1.0

    def cover(layer, amount):
        amount = np.clip(amount, 0.0, 1.0).astype(np.float32)
        W[:] *= (1.0 - amount)
        W[layer] += amount

    sm = g.smoothstep
    # Meadow: earth shows through in patches and where the ground steepens.
    cover(DIRT, 0.26 * sm(0.05, 0.7, macro + 0.3 * fine) * sm(-0.3, 0.5, mid + 0.4 * fine))
    cover(DIRT, 0.55 * sm(17.0, 32.0, slope + 6.0 * fine))
    # A rougher, mossier turf in clumps of 10-40 m over the open ground: a second set of the same kind, so that no
    # stretch of meadow is one photograph repeated (TERRAIN_POLISH_REPORT.md §2, the layers' way).
    clump = sm(0.0, 0.2, 0.65 * g.fbm(X, Y, 31.0, seed + 211, 3) + 0.35 * g.fbm(X, Y, 9.0, seed + 213, 2))
    cover(FLOOR, 0.35 * clump)
    # A lusher, leafier turf in clumps of its own, and on ground the meadow keeps damp: a second turf of the meadow's
    # own tone (leafy_grass; the owner's second leave, 2026-11-14), so a mix reads as one sward and not as two things.
    lush = sm(0.02, 0.24, 0.6 * g.fbm(X, Y, 43.0, seed + 221, 3) + 0.4 * g.fbm(X, Y, 12.0, seed + 223, 2))
    cover(LUSH, 0.6 * lush * sm(26.0, 14.0, slope))
    # Fields: turned earth in strips.
    strips = 0.5 + 0.5 * np.sin((X * 0.8 + Y * 0.6) * 0.9)
    cover(DIRT, F["farm"] * (0.35 + 0.4 * strips))
    # Woodland floor and its litter.
    f = F["forest"]
    # Half grass still under the trees: an English wood's floor is green between the trunks.
    cover(FLOOR, 0.5 * sm(0.12, 0.9, f + 0.3 * mid + 0.1 * fine))
    cover(LEAVES, 0.4 * sm(0.3, 0.9, f + 0.1 * fine) * sm(-0.2, 0.5, macro + 0.3 * mid))
    # Wet ground: the marsh, and low ground beside tidal water.
    near = np.full_like(H, 1e9)
    for r in F["rivers"]:
        if not r["dry"]:
            near = np.minimum(near, r["d"] - r["cw"] / 2)
    damp = np.maximum(np.clip(F["marsh"] * 1.6, 0, 1), sm(1.5, 0.5, H) * sm(45.0, 6.0, near))
    cover(MUD, 0.85 * damp * (0.55 + 0.45 * sm(-0.3, 0.3, mid)))
    # Where the land's water gathers, a valley floor is damp: rough turf and bare mud along it, broken by noise.
    seep = g.blur(sm(700.0, 6000.0, F["flow"]) * land, 5.0) * sm(20.0, 8.0, slope)
    cover(LUSH, 0.7 * sm(0.05, 0.5, seep))
    cover(MUD, 0.55 * sm(0.25, 0.8, seep + 0.25 * fine))
    # Country roads and tracks: beaten earth with gravel in it.
    cover(DIRT, 0.95 * F["road_soft"])
    cover(GRAVEL, 0.45 * F["road_soft"] * sm(-0.2, 0.4, fine))
    # Where water runs: stones in the gullies and along narrow creeks.
    cover(STONES, 0.85 * sm(0.8, 1.7, F["gully"] + 0.3 * fine))
    for r in F["rivers"]:
        edge = sm(r["cw"] / 2 + (5.0 if r["dry"] else 3.0), r["cw"] / 2, r["d"])
        cover(STONES, edge * (0.9 if r["dry"] else 0.55 * sm(20.0, 8.0, r["cw"])))
    # The shore by its kind.
    # A beach is the strip the sea reaches, 30 to 70 m of it and under about 2 m; behind it the turf begins, thin at
    # first (seen in the lab: at 3 m and 110 m the whole south of the island read as bare sand).
    low = sm(2.2, 0.9, H + 0.5 * mid) * sm(70.0, 30.0, dl + 14.0 * mid)
    hard = np.clip(F["is_r"] + F["is_c"], 0, 1)
    cover(SAND, F["is_b"] * low)
    cover(GRAVEL, F["is_g"] * low)
    cover(MUD, F["is_m"] * low * 0.9)
    cover(SHORE, hard * sm(5.0, 1.6, H) * sm(42.0, 14.0, dl))
    # Rock where the ground is steep, mossed in the woods and on the lower faces.
    rock = sm(30.0, 41.0, slope + 4.0 * mid)
    cover(ROCK, rock)
    cover(MOSS, 0.7 * rock * sm(-0.15, 0.35, macro) * np.clip(1.6 * f + 0.45 * (H < 28.0), 0, 1))

    # Under the water: the bed by the shore's kind, mud in the creeks.
    under = (H < 0.12).astype(np.float32)
    W *= (1.0 - under)
    W[SAND] += under

    def bed(layer, amount):
        cover(layer, amount * under)

    bed(GRAVEL, F["is_g"] * sm(70.0, 20.0, ds))
    bed(SHORE, hard * sm(80.0, 25.0, ds) * (1.0 - 0.0 * under))
    bed(MUD, np.maximum(F["is_m"], sm(4.0, -2.0, near)) * 0.7)
    return W


def export(C, F, out):
    os.makedirs(out, exist_ok=True)
    if g.N != g.HEIGHTMAP:
        raise SystemExit("export needs ISLAND_N=%d, one sample for each of the terrain's" % g.HEIGHTMAP)
    heights = np.clip((F["H"] + g.SEA_LEVEL) / g.BUDGET * 65535.0, 0, 65535).astype("<u2")[::-1]
    with gzip.open(os.path.join(out, "island_heights.bytes"), "wb", compresslevel=6) as fh:
        fh.write(np.ascontiguousarray(heights).tobytes())

    W = surfaces(C, F)
    centres = (np.arange(ALPHAMAP) + 0.5) * g.SIZE / ALPHAMAP
    TX, TY = np.meshgrid(centres, centres)
    fine = np.stack([g.bil(W[k], TX, TY) for k in range(len(SURFACES))])
    fine /= np.maximum(fine.sum(0, keepdims=True), 1e-6)
    q = np.floor(fine * 255.0).astype(np.int32)
    top = fine.argmax(0)
    rest = 255 - q.sum(0)
    np.add.at(q, (top, np.arange(ALPHAMAP)[:, None], np.arange(ALPHAMAP)[None, :]), rest)
    with gzip.open(os.path.join(out, "island_surfaces.bytes"), "wb", compresslevel=6) as fh:
        fh.write(np.ascontiguousarray(q[:, ::-1, :].astype(np.uint8)).tobytes())

    # What the living cover is placed by (VeilmouthNatureLab): woodland, marsh, fields, where water gathers, and
    # where nothing grows (built ground, roads, water), each 0..255 on a grid of 2 m, row 0 the south edge.
    wetness = g.blur(g.smoothstep(700.0, 6000.0, F["flow"]) * F["land"], 6.0)
    bare = np.clip(F["built"] + F["roadmask"] + (F["H"] < 0.4) + F["road_soft"], 0, 1)
    zones = np.stack([F["forest"], np.clip(F["marsh"], 0, 1), F["farm"], np.clip(wetness * 1.6, 0, 1), bare,
                      np.clip(F["is_b"], 0, 1), np.clip(F["is_r"] + F["is_c"], 0, 1)])
    zc = (np.arange(ZONES) + 0.5) * g.SIZE / ZONES
    ZX, ZY = np.meshgrid(zc, zc)
    zq = np.stack([np.clip(g.bil(z.astype(np.float64), ZX, ZY) * 255.0, 0, 255) for z in zones]).astype(np.uint8)
    with gzip.open(os.path.join(out, "island_zones.bytes"), "wb", compresslevel=6) as fh:
        fh.write(np.ascontiguousarray(zq[:, ::-1, :]).tobytes())

    cell = g.STEP * g.STEP
    share ={SURFACES[k]: round(float((W[k] * F["land"]).sum() / max(F["land"].sum(), 1)), 4) for k in range(len(SURFACES))}
    with open(os.path.join(out, "island.json"), "w", encoding="utf-8") as fh:
        json.dump({"concept": C["key"], "name": C["name"], "size": g.SIZE, "heightmap": g.HEIGHTMAP, "alphamap": ALPHAMAP,
                   "seaLevel": g.SEA_LEVEL, "heightBudget": g.BUDGET, "layers": SURFACES,
                   "zones": ZONE_NAMES, "zonemap": ZONES,
                   "landArea": round(float(F["land"].sum() * cell)), "highest": round(float(F["H"].max()), 2),
                   "landShare": share}, fh, indent=1)
    return share
