"""G3f Geography Plan: the stress fixture instantiated for a seed, rasterised onto the world lattice, validated
(GF-V1 ... GF-V8) and hashed. Specification: GEOGRAPHY_G3F_TECHNICAL_SPEC.md §§4-6.

The plan is the only structural authority: synthesis reads the raster it produces and never edits it."""
import hashlib
import json
import math
import os
from collections import deque

import numpy as np

from .lattice import (BASE, C, N, NB8, SEA, components, dilate, dijkstra8, edt, load_f32, load_u8, raster4,
                      raster_polyline4, polyline_length, sha256_bytes, shore_of)
from . import synth as S
from .checks import axis_sequence

HERE = os.path.dirname(os.path.abspath(__file__))
FIXTURE = os.path.join(os.path.dirname(HERE), "fixture", "sector_nw.json")

# carried target ranges (G3e spec §§2, 4.1) and plateau classes
MAIN_RANGE = (330.0, 430.0)
SECONDARY = (0.62, 0.82)
SADDLE = (0.55, 0.72)
PASS_OFFSET = (40.0, 80.0)
CLASS_EDGE = {"short": (18.0, 40.0), "tall": (60.0, 120.0)}

# authoring defaults of the plan builder (spec §5.4) - world units
A = {
    "lowland_level_est": 35.0,       # m, expected lowland median used to place the pass target in R-F's plan
    "top_area_km2": (0.030, 0.045),  # fixture slot range inside the carried 0.03-0.15 km2
    "joint_deg": (60.0, 100.0),      # joint arc width (share 17-28 %, >= 15 % carried)
    "band_extra_m": 10.0,            # band = edge / tan 65 deg + 10 m (phase plan §6.2)
    "ramp_width_m": 40.0,            # carried V-L6 (>= 40 m)
    "ramp_side_m": 25.0,
    "ramp_len_factor": 1.15 / math.tan(math.radians(18.0)),   # phase plan §6.2: 1.15 x edge / tan 18 deg
    "ramp_len_slack_m": 80.0,
    "rimch_half_v": 1,               # rim channel half-width in vertices (3 wide)
    "axis_buffer_m": 30.0,
    "route_buffer_m": 20.0,
    "landing_from_cliff_m": 100.0,
    "landing_from_sea_m": 50.0,
    "outlet_shift_m": 60.0,          # phase plan §6.3
    "band_low_m": (30.0, 50.0), "band_rocky_m": (40.0, 70.0), "band_cliff_m": (50.0, 90.0),
    "mountain_from_main_m": 200.0, "mountain_from_branch_m": 100.0,
    "lowland_from_main_m": 260.0, "lowland_from_branch_m": 140.0,
    "corridor_half_m": 20.0,
    "headland_top_m": (60.0, 90.0),  # planned crest height of a cliff headland's top (D2 end)
    "headland_top_from_shore_m": 80.0,
    "ctx_mountain_m": 300.0, "ctx_lowland_m": 120.0,
    "intent_probe_m": 150.0,
    "target_grade_max": 0.45,        # a culmination's synthesis target <= BASE + 0.45 x its geodesic distance to the nearest outlet
}
REDRAW_CAP = 10                      # research-data/g3f/gates/limits_before_wp1.md


# ----------------------------------------------------------------------------- deterministic draws

class Stream:
    """Counter-based draws for one named stream (spec §5.3): u_k = splitmix64(key ^ k) / 2^64,
    key = first 8 bytes of SHA-256('name:seed:attempt')."""

    def __init__(self, name, seed, attempt=0):
        self.key = int.from_bytes(hashlib.sha256(f"{name}:{seed}:{attempt}".encode()).digest()[:8], "little")
        self.k = 0
        self.log = []

    def u(self, label):
        self.k += 1
        v = S._mix64(self.key ^ S._mix64(self.k)) / 2.0 ** 64
        self.log.append((label, v))
        return v

    def uniform(self, label, lo, hi):
        return lo + (hi - lo) * self.u(label)

    def choice(self, label, n):
        return min(n - 1, int(self.u(label) * n))


# ----------------------------------------------------------------------------- contour A

class Contour:
    def __init__(self, land, sea_heights, source):
        self.land = land
        self.sea_heights = sea_heights
        self.source = source
        self.shore4 = shore_of(land)
        self.dsea = edt(~land)


def load_contour(rd):
    """Contour A's land mask and Stage 1 sea heights. Preferred source: the C# contour export
    (research-data/g3f/contour/); fallback: run 11's preserved lattice (identical land and sea, WP0 C0-3)."""
    exp = os.path.join(rd, "g3f", "contour")
    if os.path.exists(os.path.join(exp, "contourA_land.u8")):
        land = load_u8(os.path.join(exp, "contourA_land.u8")) != 0
        sea = load_f32(os.path.join(exp, "contourA_stage1.f32"))
        return Contour(land, sea, "g3f/contour export")
    reg = load_u8(os.path.join(rd, "g3e-step1/run11/output/seed_110201/layout_region.u8"))
    lat = load_f32(os.path.join(rd, "g3e-step1/run11/output/seed_110201/lm1_110201.f32"))
    return Contour(reg != 0, lat, "run11 seed_110201 (fallback)")


# ----------------------------------------------------------------------------- variant parameters

def instantiate(fx, seed, cls, attempt=0):
    """Variant parameters from the stream research.g3f.plan (spec §5.3)."""
    st = Stream("research.g3f.plan", seed, attempt)
    v = {"seed": seed, "class": cls, "attempt": attempt}
    v["M1"] = st.uniform("M1", *MAIN_RANGE)
    v["M2"] = v["M1"] * st.uniform("M2", *SECONDARY)
    v["S1_frac"] = st.uniform("S1_frac", *SADDLE)
    v["pass_offset"] = st.uniform("pass_offset", *PASS_OFFSET)
    v["edge"] = st.uniform("edge", *CLASS_EDGE[cls])
    v["top_area_km2"] = st.uniform("top_area", *A["top_area_km2"])
    v["slot"] = st.choice("slot", len(fx["plateau"]["slots"]))
    v["joint_deg"] = st.uniform("joint_deg", *A["joint_deg"])
    v["rim_outlets"] = 1 + st.choice("rim_outlets", 2)
    v["tributary4"] = st.u("tributary4") < 0.5
    v["outlet_shift"] = {o: st.uniform("shift_" + o, -A["outlet_shift_m"], A["outlet_shift_m"]) for o in sorted(fx["outlets"])}
    v["band"] = {k: st.uniform("band_" + k, *A["band_" + k + "_m"]) for k in ("low", "rocky", "cliff")}
    v["headland_top"] = st.uniform("headland_top", *A["headland_top_m"])
    v["draws"] = st.log
    return v


# ----------------------------------------------------------------------------- helpers

def _v(p):
    return int(round(p[0] / C)), int(round(p[1] / C))


def _w(v):
    return v[0] * C, v[1] * C


def _mouth_inner(contour, o, length_m):
    """The point length_m inland of shore vertex o along the coast's inward normal (gradient of the distance to the
    sea over a 7 x 7 window)."""
    gz, gx = np.gradient(contour.dsea[o[1] - 3:o[1] + 4, o[0] - 3:o[0] + 4])
    nx, nz = float(gx[3, 3]), float(gz[3, 3])
    L = math.hypot(nx, nz) or 1.0
    k = length_m / C
    return (int(round(o[0] + nx / L * k)), int(round(o[1] + nz / L * k)))


def _snap_shore(contour, mask, p):
    zs, xs = np.nonzero(contour.shore4 & mask)
    d = (xs * C - p[0]) ** 2 + (zs * C - p[1]) ** 2
    k = int(np.argmin(d))
    return (int(xs[k]), int(zs[k]))


def _shift_along_shore(contour, mask, v0, shift_m):
    """The sector shore vertex whose signed position along the local coast (principal axis of shore vertices within
    80 m) is nearest v0's + shift."""
    zs, xs = np.nonzero(contour.shore4 & mask)
    p0 = np.array([v0[0], v0[1]], float)
    pts = np.stack([xs, zs], 1).astype(float)
    near = pts[np.hypot(*(pts - p0).T) * C <= 80.0]
    if len(near) < 3:
        return v0
    c = near - near.mean(0)
    _, _, vt = np.linalg.svd(c, full_matrices=False)
    t = vt[0]
    s = (near - p0) @ t * C
    k = int(np.argmin(np.abs(s - shift_m)))
    return (int(near[k][0]), int(near[k][1]))


def _nearest_on_poly(poly, p):
    best = (math.inf, None)
    for a, b in zip(poly[:-1], poly[1:]):
        dx, dz = b[0] - a[0], b[1] - a[1]
        L2 = dx * dx + dz * dz
        t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L2))
        q = (a[0] + t * dx, a[1] + t * dz)
        d = math.hypot(p[0] - q[0], p[1] - q[1])
        if d < best[0]:
            best = (d, q)
    return best[1]


def _labels_bfs(src_mask, dom_mask, labels):
    """Nearest-source label by 8-neighbour BFS steps (ties by queue order) inside dom_mask."""
    n0, n1 = src_mask.shape
    lab = np.full(n0 * n1, -1, np.int64)
    dom = dom_mask.reshape(-1)
    dq = deque()
    for i in np.flatnonzero(src_mask.reshape(-1)).tolist():
        lab[i] = labels[i]
        dq.append(i)
    while dq:
        i = dq.popleft()
        z, x = divmod(i, n1)
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                if dom[j] and lab[j] < 0:
                    lab[j] = lab[i]
                    dq.append(j)
    return lab.reshape(n0, n1)


def _line_mask(vlist, shape):
    m = np.zeros(shape, bool)
    for x, z in vlist:
        if 0 <= z < shape[0] and 0 <= x < shape[1]:
            m[z, x] = True
    return m


def _poly_buffer(points_world, radius_m, shape):
    """Vertices within radius of a world polyline (EDT of its 4-connected raster)."""
    vs = raster_polyline4([(p[0] / C, p[1] / C) for p in points_world])
    return edt(_line_mask(vs, shape)) <= radius_m


def _profile(arc_nodes, total):
    """Piecewise-linear profile through (arc, height) nodes, sorted by arc."""
    arcs = [a for a, _ in arc_nodes]
    hs = [h for _, h in arc_nodes]
    return lambda s: float(np.interp(s, arcs, hs))


def _lip1d(vals, step_m, t):
    """1-D lower Lipschitz envelope (only lowers): v_k = min_j (v_j + |k - j| step t)."""
    out = list(vals)
    for k in range(1, len(out)):
        out[k] = min(out[k], out[k - 1] + step_m[k] * t)
    for k in range(len(out) - 2, -1, -1):
        out[k] = min(out[k], out[k + 1] + step_m[k + 1] * t)
    return out


# ----------------------------------------------------------------------------- build

def build(fx, var, contour, P=None):
    """Instantiate the fixture for one variant; returns a dict plan with vectors, rasters (synth.Raster for the
    sector and for the context) and the records the validator and the readings need."""
    P = P or S.Params()
    land = contour.land
    shape = land.shape
    sec = fx["sector"]
    bline = raster4(tuple(sec["P_s"]), tuple(sec["P_n"]))
    bmask = _line_mask(bline, shape) & land
    lab, _ = components(land & ~bmask, 8)
    sx, sz = sec["seed_vertex"]
    sector = lab == lab[sz, sx]
    plan = {"fixture": fx["id"], "variant": {k: v for k, v in var.items() if k != "draws"},
            "draws": [[a, round(b, 12)] for a, b in var["draws"]], "notes": []}

    # nodes
    nodes = {}
    z1870 = fx["nodes"]["M1"]["on_sector_boundary_z"] / C
    m1 = min(bline, key=lambda v: (abs(v[1] - z1870), v[0]))
    nodes["M1"] = m1
    for k in ("S1", "M2"):
        nodes[k] = _v(fx["nodes"][k]["at"])
    for k in ("E1", "H"):
        nodes[k] = _snap_shore(contour, sector, fx["nodes"][k]["snap_shore"])
    outlets = {}
    for o, d in sorted(fx["outlets"].items()):
        v0 = _snap_shore(contour, sector, d["snap_shore"])
        outlets[o] = _shift_along_shore(contour, sector, v0, var["outlet_shift"][o])

    def resolve(pt):
        if isinstance(pt, str):
            return nodes[pt] if pt in nodes else outlets[pt]
        return _v(pt)

    # divides (crest lines): sector boundary first, then D1, D2
    crest = np.full(shape, -1, np.int32)
    crests = []

    def add_crest(name, kind, vlist, plan_h=None, extra=None):
        cid = len(crests)
        rec = {"kind": kind, "name": name, "plan": None, "vertices": []}
        if extra:
            rec.update(extra)
        for x, z in vlist:
            if land[z, x] and crest[z, x] < 0:
                crest[z, x] = cid
                rec["vertices"].append((x, z))
        crests.append(rec)
        return cid

    add_crest("S", S.SECTOR, bline)
    div_lines = {}
    for name in ("D1", "D2"):
        pts = [resolve(p) for p in fx["divides"][name]["points"]]
        vl = raster_polyline4(pts)
        div_lines[name] = vl
        add_crest(name, S.DIVIDE, vl)
    plan["nodes"] = {k: list(v) for k, v in nodes.items()}
    plan["outlets"] = {k: list(v) for k, v in outlets.items()}

    # provisional catchments and regions -> coastal intents -> fringe band and fringe crests
    cm = crest >= 0
    lab, k = components(sector & ~cm, 8)
    seeds = {u: _v(d["seed"]) for u, d in sorted(fx["units"].items())}
    prov = np.full(shape, -1, np.int32)
    for ui, (u, sv) in enumerate(seeds.items()):
        prov[lab == lab[sv[1], sv[0]]] = ui
    dist = {}
    for name in ("D1", "D2"):
        dist[name] = edt(_line_mask(div_lines[name], shape))
    dist["S"] = edt(_line_mask(bline, shape))
    dmain = np.minimum(dist["D1"], dist["S"])
    reserve = fx["reserve"]
    rc = _v(reserve["centre"])
    zz, xx = np.mgrid[0:shape[0], 0:shape[1]]
    rdisc = np.hypot(xx - rc[0], zz - rc[1]) * C <= reserve["radius_m"]
    region = np.zeros(shape, np.uint8)
    inn = sector & ~cm
    mountain = (dmain < A["mountain_from_main_m"]) | (dist["D2"] < A["mountain_from_branch_m"])
    lowland = ((dmain >= A["lowland_from_main_m"]) & (dist["D2"] >= A["lowland_from_branch_m"])) | rdisc
    for d in fx.get("lowland_discs", []):
        c0 = _v(d["centre"])
        lowland |= np.hypot(xx - c0[0], zz - c0[1]) * C <= d["radius_m"]
    region[inn] = S.UPLAND
    region[inn & mountain & ~lowland] = S.MOUNTAIN
    region[inn & lowland] = S.LOWLAND
    # coastal intent per sector shore vertex: hinterland region probed inland, then the overrides
    shore = contour.shore4 & sector
    hint = _labels_bfs(shore, sector, np.arange(shape[0] * shape[1]))   # nearest shore vertex per sector vertex
    probe = sector & (contour.dsea >= A["intent_probe_m"] - 10) & (contour.dsea <= A["intent_probe_m"] + 10) & inn
    intent = {}
    votes = {}
    for z, x in zip(*np.nonzero(probe)):
        s_i = hint[z, x]
        votes.setdefault(int(s_i), []).append(int(region[z, x]))
    shore_idx = np.flatnonzero(shore.reshape(-1)).tolist()
    sh_lab = _labels_bfs(np.isin(np.arange(shape[0] * shape[1]), list(votes.keys())).reshape(shape),
                         shore | probe, np.arange(shape[0] * shape[1]))
    for i in shore_idx:
        j = int(sh_lab.reshape(-1)[i])
        rs = votes.get(j, [S.UPLAND])
        top = S.MOUNTAIN if S.MOUNTAIN in rs else (S.UPLAND if S.UPLAND in rs else S.LOWLAND)
        intent[i] = {S.MOUNTAIN: "cliff", S.UPLAND: "rocky", S.LOWLAND: "low"}[top]
    n1 = shape[1]
    for name, kind in (("H", "cliff"), ("E1", "rocky")):
        v = nodes[name]
        for i in shore_idx:
            z, x = divmod(i, n1)
            if math.hypot(x - v[0], z - v[1]) * C <= 60.0:
                intent[i] = kind
    for o, v in outlets.items():          # outlets win over the headland overrides
        for i in shore_idx:
            z, x = divmod(i, n1)
            if math.hypot(x - v[0], z - v[1]) * C <= fx["outlets"][o]["half_length_m"] + 40.0:
                intent[i] = "low"
    # outlet runs: shore vertices within half_length of each outlet -> the catchment's roots; the outlet zone is the
    # band land whose nearest shore vertex is in the run, excluded from the fringe so the catchment reaches the sea
    # exactly along its run
    near_shore = hint.reshape(-1)
    outlet_zone = np.zeros(shape, bool)
    out_runs = {}
    mouths = {}
    for o, v in outlets.items():
        run = [i for i in shore_idx if math.hypot(i % n1 - v[0], i // n1 - v[1]) * C <= fx["outlets"][o]["half_length_m"]]
        out_runs[o] = run
        outlet_zone |= np.isin(near_shore, run).reshape(shape) & sector
        # the mouth: a strip of the run's width across the band along the coast's inward normal (holds the trunk's
        # last segment)
        mi = _mouth_inner(contour, v, max(var["band"].values()) + 15.0)
        seg = _poly_buffer([_w(v), _w(mi)], fx["outlets"][o]["half_length_m"], shape)
        outlet_zone |= seg & sector
        mouths[o] = mi
    # band: sector land within the band width of its nearest shore vertex's intent
    width = np.zeros(shape[0] * shape[1])
    for i in np.flatnonzero(sector.reshape(-1)).tolist():
        s_i = int(near_shore[i])
        width[i] = var["band"][intent.get(s_i, "rocky")]
    band = sector & (contour.dsea <= width.reshape(shape)) & ~outlet_zone & ~cm
    interior = sector & ~band & ~cm
    # interior pockets that hold no unit seed join the fringe band (no land is left without an owner)
    ilab, ik = components(interior, 8)
    keep = {int(ilab[sv[1], sv[0]]) for sv in seeds.values()}
    for c_ in range(ik):
        if c_ not in keep:
            band |= ilab == c_
    interior = sector & ~band & ~cm
    fcrest = band & dilate(interior, 1)
    fid = add_crest("F", S.FRINGE_CREST, [(int(x), int(z)) for z, x in zip(*np.nonzero(fcrest))], extra={"floor": 1.0})
    cm = crest >= 0
    # final units
    unit = np.full(shape, -1, np.int32)
    units = []
    lab, k = components(sector & ~cm & ~band, 8)
    for u, sv in seeds.items():
        ui = len(units)
        unit[lab == lab[sv[1], sv[0]]] = ui
        units.append({"kind": S.CATCHMENT, "name": u, "outlet": fx["units"][u]["outlet"]})
    stray = sector & ~cm & ~band & (unit < 0)
    flab, fk = components(band & ~cm, 8)
    seaadj = land & dilate(~land, 1)
    orphan_fringe = 0
    for f in range(fk):
        m = flab == f
        if not (m & seaadj).any():
            # a band piece cut off from the sea by crest lines has no root: it joins the fringe crest record
            crest[m] = fid
            crests[fid]["vertices"].extend((int(x), int(z)) for z, x in zip(*np.nonzero(m)))
            orphan_fringe += int(m.sum())
            continue
        unit[m] = len(units)
        units.append({"kind": S.FRINGE, "name": f"F{len(units)}"})
    cm = crest >= 0
    plan["orphan_fringe_vertices"] = orphan_fringe
    # a fringe unit's parent is the catchment with most vertices within 3 vertices of it (its coastal frontage, A1)
    for fi, ud in enumerate(units):
        if ud["kind"] != S.FRINGE:
            continue
        ring = dilate(unit == fi, 3) & (unit >= 0) & (unit != fi)
        ids, cnt = np.unique(unit[ring], return_counts=True)
        best_ = [(int(c_), int(i_)) for i_, c_ in zip(ids, cnt) if units[int(i_)]["kind"] == S.CATCHMENT]
        ud["parent"] = units[max(best_)[1]]["name"] if best_ else None
    # regions final: fringe by intent; corridor along axes; plateau later
    shore_intent = np.full(shape, 255, np.uint8)
    code = {"low": 0, "rocky": 1, "cliff": 2}
    for i, it in intent.items():
        shore_intent.reshape(-1)[i] = code[it]
    fr_intent = near_shore.copy()
    for f_i in np.flatnonzero((band & ~cm).reshape(-1)).tolist():
        it = intent.get(int(near_shore[f_i]), "rocky")
        region.reshape(-1)[f_i] = {"low": S.COAST_LOW, "rocky": S.COAST_ROCKY, "cliff": S.COAST_CLIFF}[it]
    region[cm & sector] = 0
    for i in np.flatnonzero((cm & sector).reshape(-1)).tolist():
        region.reshape(-1)[i] = S.MOUNTAIN if crest.reshape(-1)[i] != fid else S.COAST_ROCKY

    # axes
    axes = {}
    for name, d in fx["axes"].items():
        if d.get("optional") and not var["tributary4"]:
            continue
        pts = [resolve(p) for p in d["points"]]
        last = d["points"][-1]
        if isinstance(last, str) and last in outlets:
            # enter the outlet along the mouth strip (the coast's inward normal)
            o, mi = outlets[last], mouths[last]
            dm = math.hypot(mi[0] - o[0], mi[1] - o[1]) + 10.0 / C
            keep = [q for q in pts[:-1] if math.hypot(q[0] - o[0], q[1] - o[1]) > dm]
            pts = keep + [mi, o]
        if "joins" in d:
            tgt = axes[d["joins"]]["poly"]
            q = _nearest_on_poly(tgt, pts[-1])
            pts = pts + [(int(round(q[0])), int(round(q[1])))]
        axes[name] = {"unit": d["unit"], "poly": pts, "joins": d.get("joins"), "from_rim_outlet": d.get("from_rim_outlet", False)}
    axis_mask = np.zeros(shape, bool)
    for a in axes.values():
        for x, z in axis_sequence(a["poly"]):
            axis_mask[z, x] = True
    corr = edt(axis_mask) <= A["corridor_half_m"]
    region[(region == S.MOUNTAIN) & corr & (unit >= 0)] = S.CORRIDOR
    region[(region == S.UPLAND) & corr & (unit >= 0)] = S.CORRIDOR

    # routes (vectors) and buffers
    routes = {}
    for name, d in fx["routes"].items():
        pts = [rc]
        for via in d.get("via", []):
            pts.append(nodes[via])
        if isinstance(d["to"], list):
            pts.append(_v(d["to"]))
        routes[name] = {"poly": pts, "to": d["to"], "role": d["role"]}

    # plateau record and footprint
    plan["_div"] = div_lines
    pl = _plateau(fx, var, contour, sector, unit, units, region, crest, axis_mask, rdisc, routes, intent, shape, plan)
    zone = pl.pop("zone_arr")
    pof = pl.pop("pof_arr")
    # pockets of the plateau's unit enclosed by its footprint (no path over open ground to the unit's outlet) join
    # the footprint: as ramp-side ground where they touch it, otherwise as escarpment band (both drain outward)
    uidp = [u["name"] for u in units].index(fx["plateau"]["unit"])
    open_ground = (unit == uidp) & (zone == S.Z_NONE) & (crest < 0)
    olab, ok_ = components(open_ground, 8)
    good = set(np.unique(olab[open_ground & seaadj]).tolist())
    pockets = 0
    for c_ in range(ok_):
        if c_ in good:
            continue
        m = olab == c_
        zone[m] = S.Z_RSIDE if (dilate(m, 1) & (zone == S.Z_RSIDE)).any() else S.Z_BAND
        pof[m] = 0
        pockets += int(m.sum())
    pl["pockets_absorbed_vertices"] = pockets
    pl["footprint_vertices"] = int((zone > 0).sum())
    region[zone == S.Z_TOP] = S.TOP
    region[(zone == S.Z_BAND) | (zone == S.Z_RIMCH) | (zone == S.Z_RSIDE)] = S.BAND
    region[zone == S.Z_RAMP] = S.RAMP
    for name, a in axes.items():
        if a["from_rim_outlet"] and pl.get("rim_outlet_ends"):
            a["poly"] = [tuple(pl["rim_outlet_ends"][0])] + [p for p in a["poly"] if not isinstance(p, str)][1:]
    if pl.get("ramp_poly_world"):
        routes["R2"]["poly"] = [rc, _v(pl["landing_world"])] + [_v(p) for p in reversed(pl["ramp_poly_world"])] + [_v(pl["centre_world"])]

    # roots: unit vertices 8-adjacent to the sea (outlet runs for catchments, the shore for fringes)
    root = seaadj & (unit >= 0)

    # planned crest heights
    # feasibility of the drawn culmination targets in this sector (spec §5.5): geodesic distance over the sector to
    # the nearest outlet run; the drawn value is kept, the synthesis target is clamped and the reading flags it
    srcs = {}
    for o, run in out_runs.items():
        for i in run:
            srcs[i] = 0.0
    dom_all = sector | _line_mask(bline, shape) & land
    d_out, _ = dijkstra8(dom_all, srcs, np.ones(shape), C)
    feas = {k: BASE + A["target_grade_max"] * float(d_out[nodes[k][1], nodes[k][0]]) for k in ("M1", "M2")}
    var_t = _targets(var, P, feas)
    plan["targets"] = var_t
    _crest_plans(crests, crest, nodes, outlets, div_lines, bline, var, var_t, contour, intent, near_shore, P, shape, plan)

    # sector raster
    R = S.Raster(shape[0], shape[1], C)
    bm = _line_mask(bline, shape) & land
    dom = sector | bm                       # the sector and its boundary crest (owned by the Sector record)
    R.land = dom
    R.unit = unit
    R.crest = np.where(dom, crest, -1).astype(np.int32)
    region[bm] = S.MOUNTAIN
    R.region = np.where(dom, region, 0).astype(np.uint8)
    R.root = root & sector
    R.zone = zone
    R.plateau_of = pof
    R.axes = [a["poly"] for a in axes.values()]
    R.exempt = sector & ((zone == S.Z_BAND) | (zone == S.Z_RIMCH) | (zone == S.Z_RSIDE) | (region == S.COAST_CLIFF))
    R.units = units
    R.crests = [{k: v for k, v in c.items() if k != "vertices"} for c in crests]
    R.plateaus = [pl["synth"]] if pl.get("synth") else []
    R.fixed = np.where(land, np.nan, contour.sea_heights)

    plan.update({"raster": R, "sector": sector, "crest_names": [c["name"] for c in crests], "axes": axes,
                 "routes": routes, "plateau": pl, "reserve": {"centre": list(rc), "radius_m": reserve["radius_m"],
                 "core_m": reserve["core_m"], "mask": rdisc}, "shore_intent": shore_intent, "out_runs": out_runs,
                 "stray": int(stray.sum()), "div_lines": div_lines, "bline": bline,
                 "regions_main_dist": dmain})
    plan["context"] = _context(fx, contour, sector, bline, crests, nodes, plan)
    return plan


def _targets(var, P, feas):
    t = {"M1_drawn": var["M1"], "M2_drawn": var["M2"], "M1_feasible": feas["M1"], "M2_feasible": feas["M2"]}
    t["M1"] = min(var["M1"], feas["M1"])
    t["M2"] = min(var["M2"], feas["M2"], t["M1"])
    t["S1"] = min(A["lowland_level_est"] + var["pass_offset"], var["S1_frac"] * min(t["M1"], t["M2"]))
    t["S1_saddle_fraction_target"] = var["S1_frac"] * min(t["M1"], t["M2"])
    t["S1_pass_target"] = A["lowland_level_est"] + var["pass_offset"]
    t["H_top"] = var["headland_top"]
    t["edge"] = var["edge"]
    return t


def _crest_plans(crests, crest, nodes, outlets, div_lines, bline, var, t, contour, intent, near_shore, P, shape, plan):
    """Planned crest heights (spec §5.5): piecewise linear through node targets along each line, coast ends at
    BASE + 1 m, then the 1-D Lipschitz envelope at the flank slope (only lowers) so a planned crest is walkable
    along its line. The Sector crest is planned for every candidate; D1, D2 and fringe crests only for GF-RF."""
    n1 = shape[1]
    clamp = {}
    for c in crests:
        if c["kind"] == S.FRINGE_CREST:
            continue
        name = c["name"]
        line = bline if name == "S" else div_lines[name]
        arc = [0.0]
        for a, b in zip(line[:-1], line[1:]):
            arc.append(arc[-1] + math.hypot(b[0] - a[0], b[1] - a[1]) * C)
        idx = {v: k for k, v in enumerate(line)}
        if name == "S":
            ks = idx[nodes["M1"]]
            pts = [(0.0, BASE + 1.0), (arc[ks], t["M1"]), (arc[-1], BASE + 1.0)]
        elif name == "D1":
            pts = [(0.0, t["M1"]), (arc[idx[nodes["S1"]]], t["S1"]), (arc[idx[nodes["M2"]]], t["M2"]), (arc[-1], BASE + 1.0)]
        else:
            top_at = max(0.0, arc[-1] - A["headland_top_from_shore_m"])
            pts = [(0.0, t["M1"]), (top_at, t["H_top"]), (arc[-1], BASE + 1.0)]
        f = _profile(sorted(pts), arc[-1])
        raw = [f(s) for s in arc]
        steps = [0.0] + [arc[k] - arc[k - 1] for k in range(1, len(arc))]
        env = _lip1d(raw, steps, P.flank)
        planv = np.full(shape[0] * shape[1], np.nan)
        for (x, z), h in zip(line, env):
            if crest[z, x] == crests.index(c):
                planv[z * n1 + x] = h
        c["plan"] = planv
        clamp[name] = {"raw_max": max(raw), "planned_max": max(env), "lowered_max_m": max(r - e for r, e in zip(raw, env))}
        if name == "D1":
            clamp["D1"]["S1_planned"] = env[idx[nodes["S1"]]]
            clamp["D1"]["M2_planned"] = env[idx[nodes["M2"]]]
    plan["crest_clamp"] = clamp


def _rim_channel(cv, b, r_top, w_band, band, shape):
    """A straight rim channel (3 vertices wide) from inside the top's edge across the band along bearing b; returns its
    mask (band cells only) and its outer end vertex (the first vertex past the band)."""
    n0, n1 = shape
    dx, dz = math.cos(math.radians(b)), math.sin(math.radians(b))
    m = np.zeros(shape, bool)
    for k in range(int((r_top - 2 * C) / C), int((r_top + w_band) / C) + 3):
        px, pz = cv[0] + dx * k, cv[1] + dz * k
        for o in range(-A["rimch_half_v"], A["rimch_half_v"] + 1):
            qx, qz = int(round(px - dz * o)), int(round(pz + dx * o))
            if 0 <= qx < n1 and 0 <= qz < n0 and band[qz, qx]:
                m[qz, qx] = True
    k = int((r_top + w_band) / C) + 2
    end = (int(round(cv[0] + dx * k)), int(round(cv[1] + dz * k)))
    return m, end


def _plateau(fx, var, contour, sector, unit, units, region, crest, axis_mask, rdisc, routes, intent, shape, plan):
    """Plateau record and write footprint (spec §5.4, L-5). Order: the top disc at the drawn slot; the joint arc toward
    the declared ridge; the escarpment band elsewhere; rim outlet 1 toward the rim-outlet stream's target; the ramp
    path chosen by the builder within GF-V4 (shortest path from the escarpment rim, away from rim outlet 1, to a
    lowland landing at least L_min away, the landing nearest the settlement reserve); rim outlet 2 (if drawn) at the
    escarpment bearing farthest from rim outlet 1 and from the ramp start."""
    n0, n1 = shape
    fp = fx["plateau"]
    uid = [u["name"] for u in units].index(fp["unit"])
    cv = _v(fp["slots"][var["slot"]])
    r_top = math.sqrt(var["top_area_km2"] * 1e6 / math.pi)
    edge = var["edge"]
    w_band = edge / math.tan(math.radians(65.0)) + A["band_extra_m"]
    zz, xx = np.mgrid[0:n0, 0:n1]
    d = np.hypot(xx - cv[0], zz - cv[1]) * C
    bear = (np.degrees(np.arctan2(zz - cv[1], xx - cv[0])) + 360.0) % 360.0
    rv, best = None, math.inf
    for x, z in plan["_div"][fp["joint_toward"]]:
        dd = math.hypot(x - cv[0], z - cv[1])
        if dd < best:
            best, rv = dd, (x, z)
    jb = (math.degrees(math.atan2(rv[1] - cv[1], rv[0] - cv[0])) + 360.0) % 360.0
    half = var["joint_deg"] / 2.0

    def angdiff(a, b):
        return abs((a - b + 180.0) % 360.0 - 180.0)

    inunit = unit == uid
    top = (d <= r_top) & inunit
    joint = angdiff(bear, jb) <= half
    band = (d > r_top) & (d <= r_top + w_band) & ~joint & inunit
    tgt = None
    for name, a in fx["axes"].items():
        if a.get("from_rim_outlet"):
            tgt = _v(a["points"][-1])
    b1 = (math.degrees(math.atan2(tgt[1] - cv[1], tgt[0] - cv[0])) + 360.0) % 360.0
    if angdiff(b1, jb) <= half + 10.0:
        b1 = (jb + 180.0) % 360.0
    rim1, end1 = _rim_channel(cv, b1, r_top, w_band, band, shape)
    rec = {"centre_world": [cv[0] * C, cv[1] * C], "r_top_m": r_top, "edge_target_m": edge, "band_m": w_band,
           "joint_bearing": jb, "joint_deg": var["joint_deg"], "slot": var["slot"]}
    L_min = A["ramp_len_factor"] * edge
    rec["ramp_min_len_m"] = L_min
    forbidden = (~sector) | (crest >= 0) | (unit != uid) | rdisc | (region == S.COAST_LOW) | (region == S.COAST_ROCKY) | (region == S.COAST_CLIFF)
    forbidden |= edt(axis_mask) <= A["axis_buffer_m"]
    for name, r in routes.items():
        if name == "R2":
            continue
        forbidden |= _poly_buffer([_w(v) for v in r["poly"]], A["route_buffer_m"], shape)
    forbidden |= dilate(rim1, 2)
    cliff_shore = np.zeros(shape, bool)
    for i, it in intent.items():
        if it == "cliff":
            cliff_shore.reshape(-1)[i] = True
    near_cliff = edt(cliff_shore) <= A["landing_from_cliff_m"]
    pad = A["ramp_width_m"] / 2.0 + A["ramp_side_m"]
    forbid_pad = edt(forbidden) <= pad
    start = top & dilate(~top & ~joint, 1)
    cand_b = [float(b) for b in range(0, 360, 15) if angdiff(b, b1) > 40.0 and angdiff(b, jb) > half + 10.0]
    passable0 = ~forbid_pad & ~top & ~(joint & (d > r_top) & (d <= r_top + 2 * C))
    landing_base = (region == S.LOWLAND) & ~near_cliff & (contour.dsea >= A["landing_from_sea_m"])
    rcx, rcz = plan_reserve_centre(fx)
    best = None
    searched = []
    # one start per candidate bearing (the rim vertex nearest that bearing), so a ramp may wind around the plateau;
    # among all starts, the landing nearest the settlement reserve wins (ties: smaller bearing, smaller index)
    for b in cand_b:
        zs_, xs_ = np.nonzero(start)
        if not len(zs_):
            break
        k_ = int(np.argmin([angdiff(bear[z_, x_], b) for z_, x_ in zip(zs_, xs_)]))
        si = int(zs_[k_] * n1 + xs_[k_])
        passable = passable0.copy()
        passable.reshape(-1)[si] = True
        dist, parent = dijkstra8(passable, {si: 0.0}, np.ones(shape), C, limit=L_min + A["ramp_len_slack_m"] + 10.0)
        ok = landing_base & passable & (dist >= L_min) & (dist <= L_min + A["ramp_len_slack_m"])
        fin = np.isfinite(dist)
        searched.append([b, float(dist[fin].max()) if fin.any() else 0.0, int(ok.sum())])
        if ok.any():
            cand = np.flatnonzero(ok.reshape(-1))
            dres = np.hypot(cand % n1 - rcx, cand // n1 - rcz)
            j = int(np.lexsort((cand, dres))[0])
            key = (float(dres[j]), b)
            if best is None or key < best[0]:
                best = (key, int(cand[j]), dist, parent)
    rec["ramp_found"] = best is not None
    rec["ramp_search"] = {"bearings": searched}
    if best is not None:
        _, li_, dist, parent = best
        landing_ok = np.zeros(shape, bool)
        landing_ok.reshape(-1)[li_] = True
    else:
        landing_ok = np.zeros(shape, bool)
    zone = np.zeros(shape, np.uint8)
    pof = np.full(shape, -1, np.int32)
    ramp = np.zeros(shape, bool)
    rside = np.zeros(shape, bool)
    pv = None
    if landing_ok.any():
        rcx, rcz = plan_reserve_centre(fx)
        cand = np.flatnonzero(landing_ok.reshape(-1))
        dres = np.hypot(cand % n1 - rcx, cand // n1 - rcz)
        li = int(cand[np.lexsort((cand, dres))[0]])
        path = [li]
        while parent[path[-1]] >= 0:
            path.append(int(parent[path[-1]]))
        pv = [(i % n1, i // n1) for i in path[::-1]]   # top edge first
        line = _line_mask(pv, shape)
        dl = edt(line)
        idx_lab = _labels_bfs(line, dl <= pad + C, _path_index(pv, shape))
        ramp = (dl <= A["ramp_width_m"] / 2.0) & inunit & ~top
        end_cap = idx_lab >= len(pv) - 1 - int(math.ceil(A["ramp_width_m"] / 2.0 / C))
        rside = (dl > A["ramp_width_m"] / 2.0) & (dl <= pad) & inunit & ~top & ~band & ~rim1 & ~end_cap
        rec.update({"ramp_len_m": float(dist.reshape(-1)[li]), "ramp_start_bearing": float(bear[pv[0][1], pv[0][0]]),
                    "landing_world": [pv[-1][0] * C, pv[-1][1] * C],
                    "ramp_poly_world": [[x * C, z * C] for x, z in pv[::max(1, len(pv) // 24)]] + [[pv[-1][0] * C, pv[-1][1] * C]],
                    "ramp_vertices": [list(v) for v in pv]})
    rim_b = [b1]
    rim_ends = [end1]
    rimch = rim1.copy()
    if var["rim_outlets"] == 2:
        avoid = [b1] + ([rec["ramp_start_bearing"]] if pv else [])
        cands = [float(b) for b in range(0, 360, 5) if angdiff(b, jb) > half + 15.0]
        cands.sort(key=lambda b: (-min(angdiff(b, a) for a in avoid), b))
        placed = False
        for b2 in cands:
            m2, e2 = _rim_channel(cv, b2, r_top, w_band, band & ~ramp & ~rside, shape)
            e_ok = 0 <= e2[0] < n1 and 0 <= e2[1] < n0 and inunit[e2[1], e2[0]] and not (ramp | rside | band | top)[e2[1], e2[0]]
            if m2.any() and e_ok and not (dilate(m2, 1) & (ramp | rside)).any():
                rimch |= m2
                rim_b.append(b2)
                rim_ends.append(e2)
                placed = True
                break
        rec["rim_outlet_2_placed"] = placed
    band &= ~rimch & ~ramp
    zone[rside] = S.Z_RSIDE
    zone[ramp] = S.Z_RAMP
    zone[band] = S.Z_BAND
    zone[rimch] = S.Z_RIMCH
    zone[top] = S.Z_TOP
    pof[zone > 0] = 0
    rec["rim_outlet_bearings"] = rim_b
    rec["rim_outlet_ends"] = [list(e) for e in rim_ends]
    if pv:
        rec["synth"] = {"edge": edge, "class": var["class"], "ramp_poly": [(x, z) for x, z in pv],
                        "ramp_top_vertex": pv[0], "landing_vertex": _beyond(pv, ramp, shape)}
    rec["footprint_vertices"] = int((zone > 0).sum())
    rec["top_vertices"] = int(top.sum())
    rec["zone_arr"] = zone
    rec["pof_arr"] = pof
    rim = top & dilate(~top, 1)
    rb = bear[rim]
    rec["rim_shares"] = {"joint": float(np.mean([angdiff(b, jb) <= half for b in rb])) if rb.size else 0.0}
    rec["rim_shares"]["escarpment"] = 1.0 - rec["rim_shares"]["joint"]
    return rec


def _path_index(pv, shape):
    """Flat array: index along the path for path vertices, -1 elsewhere (labels for _labels_bfs)."""
    a = np.full(shape[0] * shape[1], -1, np.int64)
    for k, (x, z) in enumerate(pv):
        if a[z * shape[1] + x] < 0:
            a[z * shape[1] + x] = k
    return a


def _beyond(pv, ramp, shape):
    """The first vertex past the ramp's last centre-line vertex that is outside the ramp strip (the landing)."""
    n0, n1 = shape
    (x1, z1), (x0, z0) = pv[-1], pv[max(0, len(pv) - 6)]
    dx, dz = x1 - x0, z1 - z0
    L = math.hypot(dx, dz) or 1.0
    for k in range(1, 40):
        x, z = int(round(x1 + dx / L * k)), int(round(z1 + dz / L * k))
        if 0 <= x < n1 and 0 <= z < n0 and not ramp[z, x]:
            return (x, z)
    return pv[-1]


def plan_div_points(plan, name):
    return plan["_div"][name]


def plan_axes_raw(fx):
    return fx["axes"]


def plan_reserve_centre(fx):
    return _v(fx["reserve"]["centre"])


def _context(fx, contour, sector, bline, crests, nodes, plan):
    """Coarse whole-island context (D-5): context divides K0, K1 rasterised as emergent crest lines; the Sector crest
    shared as planned; units = components of the remaining context land, every land vertex next to the sea a root;
    regions by distance to the context ridges (mountain) and to the sea (lowland). Synthesised by GF-C0 only."""
    land = contour.land
    shape = land.shape
    ctx_land = land & ~sector
    crest = np.full(shape, -1, np.int32)
    recs = []
    sec_rec = crests[0]
    bm = _line_mask(bline, shape) & land
    crest[bm] = 0
    recs.append({"kind": S.SECTOR, "name": "S", "plan": sec_rec["plan"], "always_planned": True})
    lines = {}
    for name, d in sorted(fx["context"]["divides"].items()):
        pts = []
        for p in d["points"]:
            pts.append(nodes[p] if isinstance(p, str) else _v(p))
        pts.append(_snap_shore(contour, ctx_land, d["snap_end_shore"]))
        vl = raster_polyline4(pts)
        lines[name] = vl
        cid = len(recs)
        for x, z in vl:
            if ctx_land[z, x] and crest[z, x] < 0:
                crest[z, x] = cid
        recs.append({"kind": S.DIVIDE, "name": name, "plan": None})
    cm = crest >= 0
    lab, k = components(ctx_land & ~cm, 8)
    unit = np.full(shape, -1, np.int32)
    units = []
    for name, sv in sorted(fx["context"]["units"].items()):
        v = _v(sv)
        unit[lab == lab[v[1], v[0]]] = len(units)
        units.append({"kind": S.CATCHMENT, "name": name})
    stray = ctx_land & ~cm & (unit < 0)
    d_r = edt(cm)
    region = np.zeros(shape, np.uint8)
    inn = unit >= 0
    region[inn] = S.UPLAND
    region[inn & (d_r <= A["ctx_mountain_m"])] = S.MOUNTAIN
    region[inn & (contour.dsea <= A["ctx_lowland_m"])] = S.LOWLAND
    R = S.Raster(shape[0], shape[1], C)
    R.land = (ctx_land | bm)
    R.unit = unit
    R.crest = crest
    R.region = region
    R.root = (land & dilate(~land, 1)) & inn
    R.units = units
    R.crests = recs
    R.fixed = np.where(land, np.nan, contour.sea_heights)
    R.fixed[sector] = np.nan
    return {"raster": R, "stray": int(stray.sum()), "lines": {k: [list(v) for v in vl] for k, vl in lines.items()},
            "units": [u["name"] for u in units]}


def plan_record(plan):
    """The canonical, JSON-serialisable record of a plan: vectors, variant, targets and the SHA-256 of every raster.
    Its SHA-256 (keys sorted, no whitespace) is the plan hash (GF-V8)."""
    R = plan["raster"]

    def h(a):
        return sha256_bytes(np.ascontiguousarray(a).tobytes())

    pl = {k: v for k, v in plan["plateau"].items() if k not in ("synth",)}
    rec = {
        "fixture": plan["fixture"], "variant": plan["variant"], "draws": plan["draws"], "nodes": plan["nodes"],
        "outlets": plan["outlets"], "targets": plan["targets"], "crest_clamp": plan["crest_clamp"],
        "axes": {k: {"unit": a["unit"], "poly": [list(map(int, p)) for p in a["poly"]], "joins": a["joins"]} for k, a in sorted(plan["axes"].items())},
        "routes": {k: {"poly": [list(map(int, p)) for p in r["poly"]], "role": r["role"]} for k, r in sorted(plan["routes"].items())},
        "reserve": {k: v for k, v in plan["reserve"].items() if k != "mask"},
        "plateau": pl,
        "units": R.units, "crests": [{"kind": c["kind"], "name": c["name"]} for c in R.crests],
        "rasters": {"unit": h(R.unit), "crest": h(R.crest), "region": h(R.region), "root": h(R.root), "zone": h(R.zone),
                    "exempt": h(R.exempt), "land": h(R.land), "shore_intent": h(plan["shore_intent"]),
                    "planned_crests": h(np.nan_to_num(np.stack([c["plan"] if c.get("plan") is not None else np.full(R.n0 * R.n1, np.nan) for c in R.crests]), nan=-1.0)),
                    "context_unit": h(plan["context"]["raster"].unit), "context_crest": h(plan["context"]["raster"].crest),
                    "context_region": h(plan["context"]["raster"].region)},
    }
    txt = json.dumps(rec, sort_keys=True, separators=(",", ":"), default=lambda o: o.item() if hasattr(o, "item") else (list(o) if isinstance(o, (tuple, set)) else str(o)))
    return rec, hashlib.sha256(txt.encode()).hexdigest()


def instantiate_valid(fx, seed, cls, contour, validate_fn, P=None, cap=REDRAW_CAP):
    """D-6 (e): draw attempt 0, 1, ... from research.g3f.plan until the plan validates or the cap is reached; every
    attempt is recorded. Returns (plan or None, validation, attempts)."""
    attempts = []
    for k in range(cap):
        var = instantiate(fx, seed, cls, k)
        pl = build(fx, var, contour, P)
        v = validate_fn(pl, contour)
        attempts.append({"attempt": k, "pass": v["pass"], "failed": [n for n, r in v.items() if n.startswith("GF-V") and not r["pass"]]})
        if v["pass"]:
            return pl, v, attempts
    return None, v, attempts
