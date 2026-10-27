"""G3f readings on an embedded lattice (spec §9): hard gates GF-R4, GF-R7, GF-R8, GF-R9, GF-RS (GF-R5 and GF-R10 from
the C# hydrology step), process gates GF-B and GF-O, the target reading GF-T and the diagnostics.

Carried calculations (G3e spec §§1, 2, 7, 14) are reproduced here; where a C# routine defines a reading (S8 through
DepressionFill, the site rule, the vertex slope) the replica follows it operation by operation, float32 included."""
import heapq
import math

import numpy as np

from .lattice import C, N, NB8, DIAG, SEA, dilate, edt, components
from . import synth as S
from .checks import d8, axis_sequence, triangle_slopes_deg
from .vendor import lmcspec as LS

BUILDABLE_DEG = 6.84          # carried (road maxGrade 0.12)
SITE_FRACTION = 0.90          # carried
SITE_SPACING = 450.0          # carried
COASTAL_REACH = 100.0         # carried
S8_DEPTH, S8_AREA = 0.25, 100.0   # carried (G0 D-d)
CLIMB_MAX = 0.25              # carried R7 (c)
REACH_SHARE = 0.95            # D-6 (c): the 95 % of WORLD_RELIEF_PRINCIPLES.md §6
M1_RADIUS, M1_PROM = 150.0, 30.0  # carried M-1

# DepressionFill / HydrologyGrid neighbour order (World/Hydrology/HydrologyGrid.cs)
HYD_DX = (1, 0, -1, 0, 1, -1, -1, 1)
HYD_DZ = (0, 1, 0, -1, 1, 1, -1, -1)


# ----------------------------------------------------------------------------- production replicas

def ocean_mask8(h32):
    """HydrologyGrid.BuildOceanMask: <= 24 m and connected to the border through <= 24 m, 8-connected."""
    n0, n1 = h32.shape
    sub = h32 <= np.float32(SEA)
    ocean = np.zeros_like(sub)
    stack = []
    for z in range(n0):
        for x in range(n1):
            if (x == 0 or z == 0 or x == n1 - 1 or z == n0 - 1) and sub[z, x] and not ocean[z, x]:
                ocean[z, x] = True
                stack.append(z * n1 + x)
    sl = sub.reshape(-1).tolist()
    ol = ocean.reshape(-1).tolist()
    while stack:
        i = stack.pop()
        z, x = divmod(i, n1)
        for k in range(8):
            xx, zz = x + HYD_DX[k], z + HYD_DZ[k]
            if 0 <= xx < n1 and 0 <= zz < n0:
                j = zz * n1 + xx
                if not ol[j] and sl[j]:
                    ol[j] = True
                    stack.append(j)
    return np.array(ol, bool).reshape(n0, n1)


def depression_fill(h32, ocean):
    """DepressionFill.Fill: priority flood from the ocean and the border, epsilon 0.001 in float32, heap ties by
    insertion sequence, neighbours in HydrologyGrid order."""
    n0, n1 = h32.shape
    hl = h32.reshape(-1).astype(np.float32)
    filled = hl.copy()
    closed = np.zeros(n0 * n1, bool)
    heap = []
    seq = 0
    oc = ocean.reshape(-1)
    for z in range(n0):
        for x in range(n1):
            i = z * n1 + x
            if oc[i] or x == 0 or z == 0 or x == n1 - 1 or z == n0 - 1:
                closed[i] = True
                heap.append((float(filled[i]), seq, i))
                seq += 1
    heapq.heapify(heap)
    eps = np.float32(0.001)
    fl = filled.tolist()
    cl = closed.tolist()
    hh = hl.tolist()
    while heap:
        cv, _, i = heapq.heappop(heap)
        z, x = divmod(i, n1)
        cv32 = np.float32(fl[i])
        raised = float(cv32 + eps)
        for k in range(8):
            xx, zz = x + HYD_DX[k], z + HYD_DZ[k]
            if 0 <= xx < n1 and 0 <= zz < n0:
                j = zz * n1 + xx
                if cl[j]:
                    continue
                v = hh[j] if hh[j] > raised else raised
                fl[j] = v
                cl[j] = True
                heapq.heappush(heap, (v, seq, j))
                seq += 1
    return np.array(fl, np.float32).reshape(n0, n1)


def s8_depressions(h32, filled, land, cell=C):
    """TerrainGenLmcMeasure.Depressions: 4-connected components of depth > 0.05 m (depth = filled - h in float32);
    S8 when the deepest >= 0.25 m and cells * c^2 >= 100 m^2. Returns a list of dicts (deepest vertex, depth, area)."""
    n0, n1 = h32.shape
    depth = np.where(land, (filled - h32).astype(np.float32), np.float32(0)).reshape(-1)
    dl = depth.tolist()
    seen = bytearray(n0 * n1)
    out = []
    thr = float(np.float32(0.05))
    for s in np.flatnonzero(depth > np.float32(0.05)).tolist():
        if seen[s]:
            continue
        seen[s] = 1
        stack = [s]
        cells = 0
        deepest = 0.0
        at = s
        while stack:
            i = stack.pop()
            cells += 1
            if dl[i] > deepest:
                deepest, at = dl[i], i
            x, z = i % n1, i // n1
            for j, ok in ((i - 1, x > 0), (i + 1, x < n1 - 1), (i - n1, z > 0), (i + n1, z < n0 - 1)):
                if ok and not seen[j] and dl[j] > thr:
                    seen[j] = 1
                    stack.append(j)
        if deepest >= S8_DEPTH and cells * cell * cell >= S8_AREA:
            out.append({"vertex": [at % n1, at // n1], "xz": [(at % n1) * cell, (at // n1) * cell],
                        "depth_m": float(np.float32(deepest)), "area_m2": cells * cell * cell})
    return out


def vertex_slope_deg(h32, cell=C):
    """IslandReliefMetrics.SlopeDegrees: central differences over +-1 vertex (one-sided at the border), float32."""
    h = h32.astype(np.float32)
    gx = np.zeros_like(h)
    gz = np.zeros_like(h)
    gx[:, 1:-1] = (h[:, 2:] - h[:, :-2]) / np.float32(2 * cell)
    gx[:, 0] = (h[:, 1] - h[:, 0]) / np.float32(cell)
    gx[:, -1] = (h[:, -1] - h[:, -2]) / np.float32(cell)
    gz[1:-1, :] = (h[2:, :] - h[:-2, :]) / np.float32(2 * cell)
    gz[0, :] = (h[1, :] - h[0, :]) / np.float32(cell)
    gz[-1, :] = (h[-1, :] - h[-2, :]) / np.float32(cell)
    return np.degrees(np.arctan(np.sqrt(gx.astype(np.float64) ** 2 + gz.astype(np.float64) ** 2)))


def site_candidates(h32, land, sd, radius, cell=C):
    """The carried site rule's Valid(radius) (TerrainGenLmcMeasure): centres on a stride-2 grid, centre sd >= r, no
    sea in the disc, unbuildable <= 10 % of the disc, sorted by mean slope then index."""
    n0, n1 = h32.shape
    slope = np.where(land, vertex_slope_deg(h32, cell), 90.0)
    sea = (~land).astype(np.int64)
    unb = (~land | (slope > BUILDABLE_DEG)).astype(np.int64)
    w = n1 + 1
    ps_sea = np.zeros((n0, w), np.int64); ps_sea[:, 1:] = np.cumsum(sea, 1)
    ps_unb = np.zeros((n0, w), np.int64); ps_unb[:, 1:] = np.cumsum(unb, 1)
    ps_sl = np.zeros((n0, w)); ps_sl[:, 1:] = np.cumsum(np.where(land, slope, 90.0), 1)
    rc = int(math.floor(radius / cell))
    half = [int(math.floor(math.sqrt(rc * rc - dz * dz))) for dz in range(-rc, rc + 1)]
    area = sum(2 * hw + 1 for hw in half)
    out = []
    for z in range(rc, n0 - rc, 2):
        for x in range(rc, n1 - rc, 2):
            if not land[z, x] or sd[z, x] < radius:
                continue
            s_ = 0
            u_ = 0
            sl = 0.0
            for k, dz in enumerate(range(-rc, rc + 1)):
                hw = half[k]
                lo, hi = x - hw, x + hw + 1
                s_ += ps_sea[z + dz, hi] - ps_sea[z + dz, lo]
                if s_:
                    break
                u_ += ps_unb[z + dz, hi] - ps_unb[z + dz, lo]
                sl += ps_sl[z + dz, hi] - ps_sl[z + dz, lo]
            if s_ == 0 and u_ <= (1.0 - SITE_FRACTION) * area:
                out.append((sl / area, z * n1 + x))
    out.sort()
    return [(i, m) for m, i in out]


# ----------------------------------------------------------------------------- merge tree and M-1

def merge_tree_m1(h32, va, vb):
    """G0 merge tree over land (8-connectivity, descending height, ascending index); returns the prominence of the
    peaks va, vb and c1, the vertex where their components first join (G3e M-1, after m1_full_check.py)."""
    n0, n1 = h32.shape
    hl = h32.reshape(-1).astype(np.float64)
    land = hl > SEA
    li = np.flatnonzero(land)
    order = li[np.lexsort((li, -hl[li]))].tolist()
    par = list(range(n0 * n1))
    peak = list(range(n0 * n1))
    done = bytearray(n0 * n1)
    prom = {va: None, vb: None}
    c1 = None
    hll = hl.tolist()

    def find(i):
        r = i
        while par[r] != r:
            r = par[r]
        while par[i] != r:
            par[i], i = r, par[i]
        return r

    for i in order:
        done[i] = 1
        x, z = i % n1, i // n1
        for dz in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dz == 0 and dx == 0:
                    continue
                nx, nz = x + dx, z + dz
                if nx < 0 or nz < 0 or nx >= n1 or nz >= n0:
                    continue
                j = nz * n1 + nx
                if not done[j]:
                    continue
                ri, rj = find(i), find(j)
                if ri == rj:
                    continue
                pi_, pj_ = peak[ri], peak[rj]
                ihigh = hll[pi_] > hll[pj_] or (hll[pi_] == hll[pj_] and pi_ < pj_)
                lo, hi = (rj, ri) if ihigh else (ri, rj)
                if peak[lo] in prom and prom[peak[lo]] is None:
                    prom[peak[lo]] = hll[peak[lo]] - hll[i]
                par[lo] = hi
        if c1 is None and done[va] and done[vb] and find(va) == find(vb):
            c1 = i
    for v in (va, vb):
        if prom[v] is None:
            prom[v] = hll[v] - SEA
    return prom, c1


def realised_culmination(h32, region, p_world, cell=C):
    """Highest mountain-region vertex within 150 m of the declared point (G3e spec §2.2 / M-1 item 2)."""
    n0, n1 = h32.shape
    x0, z0 = p_world[0] / cell, p_world[1] / cell
    r = int(math.ceil(M1_RADIUS / cell))
    best, bi = -math.inf, None
    for z in range(max(0, int(z0) - r), min(n0, int(z0) + r + 2)):
        for x in range(max(0, int(x0) - r), min(n1, int(x0) + r + 2)):
            if region[z, x] == S.MOUNTAIN and math.hypot(x * cell - p_world[0], z * cell - p_world[1]) <= M1_RADIUS:
                v = float(h32[z, x])
                if v > best or (v == best and z * n1 + x < bi):
                    best, bi = v, z * n1 + x
    return bi


# ----------------------------------------------------------------------------- the readings

def readings(plan, lat, synth_log, cand, hydro=None):
    """Every reading of spec §9 for one candidate lattice embedded in the world lattice."""
    R = plan["raster"]
    dom = R.land                                   # the sector and its boundary crest
    sector = plan["sector"]
    h32 = np.where(np.isnan(lat), np.float32(10.0), lat).astype(np.float32)
    land = h32 > np.float32(SEA)
    out = {"candidate": cand, "gates": {}, "flags": [], "notes": []}
    n0, n1 = h32.shape

    # GF-O single writer
    from .checks import write_log_ok
    w = write_log_ok(R, synth_log["write"])
    out["o"] = w
    out["gates"]["GF-O"] = bool(w["ok"])

    # GF-B flow across Divide-type boundaries (unit -> other unit, onto a crest, or out of the domain into land)
    r = d8(h32.astype(np.float64), land)
    unit = R.unit.reshape(-1)
    crest = R.crest.reshape(-1)
    root = R.root.reshape(-1)
    dm = dom.reshape(-1)
    lm = land.reshape(-1)
    kinds = {"other_unit": 0, "onto_crest": 0, "out_of_domain": 0, "no_receiver": 0}
    examples = []
    for i in np.flatnonzero((unit >= 0) & ~root).tolist():
        j = r[i]
        if j < 0:
            kinds["no_receiver"] += 1
            continue
        k = None
        if crest[j] >= 0:
            k = "onto_crest"
        elif unit[j] >= 0 and unit[j] != unit[i]:
            k = "other_unit"
        elif not dm[j] and lm[j]:
            k = "out_of_domain"
        if k:
            kinds[k] += 1
            if len(examples) < 10:
                examples.append([i % n1, i // n1, k])
    out["b"] = {"counts": kinds, "examples": examples}
    out["gates"]["GF-B"] = kinds["other_unit"] + kinds["onto_crest"] + kinds["out_of_domain"] == 0

    # GF-R7 (a) S8, (c) axis climbs
    ocean = ocean_mask8(h32)
    filled = depression_fill(h32, ocean)
    s8_all = s8_depressions(h32, filled, land)
    s8 = [d for d in s8_all if dom[d["vertex"][1], d["vertex"][0]]]
    climbs = {}
    for name, a in plan["axes"].items():
        climbs[name] = float(_climb(h32, a["poly"]))
    out["r7"] = {"s8_sector": s8, "s8_world": len(s8_all), "climbs_m": climbs}
    r7a = len(s8) == 0
    r7c = all(v <= CLIMB_MAX for v in climbs.values())
    out["r7"]["a"], out["r7"]["c"] = r7a, r7c

    # T-edge
    te = LS.TEdge(h32.astype(np.float64))
    te.components()

    def reach(v):
        comp, ok = te.vertex_component(int(v[0]), int(v[1]))
        return comp, bool(ok)

    # GF-R4 settlement
    sd = edt(~land)
    reserve = plan["reserve"]
    rc = reserve["centre"]
    cap = None
    for i, m in site_candidates(h32, land, sd, reserve["core_m"]):
        x, z = i % n1, i // n1
        if math.hypot(x - rc[0], z - rc[1]) * C <= reserve["radius_m"]:
            cap = (x, z, m)
            break
    coastal = None
    for i, m in site_candidates(h32, land, sd, 60.0):
        x, z = i % n1, i // n1
        if dom[z, x] and sd[z, x] <= 60.0 + COASTAL_REACH and (cap is None or math.hypot(x - cap[0], z - cap[1]) * C >= SITE_SPACING):
            coastal = (x, z, m)
            break
    r4 = {"capital": None, "coastal": None}
    if cap:
        r4["capital"] = {"vertex": [cap[0], cap[1]], "mean_slope_deg": cap[2], "reachable": reach(cap)[1]}
    if coastal:
        r4["coastal"] = {"vertex": [coastal[0], coastal[1]], "mean_slope_deg": coastal[2], "reachable": reach(coastal)[1]}
    out["r4"] = r4
    out["gates"]["GF-R4"] = bool(cap and coastal and r4["capital"]["reachable"] and r4["coastal"]["reachable"])

    # GF-R8 reachability incl. M-1 at the declared pass S1 (flanks M1, M2)
    pl = plan["plateau"]
    r8 = {"points": {}, "m1": None}
    if cap:
        r8["points"]["capital"] = reach(cap)[1]
    top = R.zone == S.Z_TOP
    if top.any():
        zs, xs = np.nonzero(top)
        cx, cz = xs.mean(), zs.mean()
        k = int(np.argmin((xs - cx) ** 2 + (zs - cz) ** 2))
        r8["points"]["plateau_top"] = reach((xs[k], zs[k]))[1]
    if pl.get("synth"):
        lv = pl["synth"]["landing_vertex"]
        r8["points"]["ramp_landing"] = reach(lv)[1]
    else:
        r8["points"]["ramp_landing"] = False
        out["notes"].append("no ramp in the plan")
    m1r = _m1(plan, h32, R.region, te)
    r8["m1"] = m1r
    r8["points"]["pass_col"] = m1r.get("status") == "found" and m1r.get("col_reachable", False)
    out["r8"] = r8
    out["gates"]["GF-R8"] = all(r8["points"].values())

    # GF-R9 routes: endpoints in one T-edge component
    r9 = {}
    for name, rt in plan["routes"].items():
        a, b = rt["poly"][0], rt["poly"][-1]
        ca, oka = reach(a)
        cb, okb = reach(b)
        r9[name] = {"from": list(a), "to": list(b), "connected": ca is not None and ca == cb}
    out["r9"] = r9
    out["gates"]["GF-R9"] = all(v["connected"] for v in r9.values())

    # GF-RS reachable share outside declared bands
    lab = te.label
    qx = n1 - 1
    vr = np.zeros((n0, n1), bool)
    tri_ok = te.shore_reach.reshape(qx, qx, 2)
    for t_, sl in ((0, None), (1, None)):
        m = tri_ok[..., t_]
        vr[:-1, :-1] |= m
        vr[:-1, 1:] |= m if t_ == 0 else False
        vr[1:, 1:] |= m
        if t_ == 1:
            vr[1:, :-1] |= m
    declared = R.exempt & dom
    base = dom & land & ~declared
    share = float((vr & base).sum() / max(1, base.sum()))
    out["rs"] = {"share": share, "vertices": int(base.sum()), "unreachable": int((base & ~vr).sum())}
    out["gates"]["GF-RS"] = share >= REACH_SHARE

    # GF-R5 / GF-R10 from the C# hydrology step; R7 (b) lakes in the sector
    if hydro is not None:
        r5, r10, lakes = _r5(plan, hydro)
        out["r5"], out["r10"] = r5, r10
        out["gates"]["GF-R5"] = r5["pass"]
        out["gates"]["GF-R10"] = r10["succeeded"]
        out["r7"]["lakes_sector"] = lakes
        out["r7"]["b"] = lakes == 0
    else:
        out["gates"]["GF-R5"] = None
        out["gates"]["GF-R10"] = None
        out["r7"]["b"] = None
    out["gates"]["GF-R7"] = (r7a and r7c and out["r7"]["b"]) if out["r7"]["b"] is not None else None
    if out["gates"]["GF-R7"] is None and not (r7a and r7c):
        out["gates"]["GF-R7"] = False

    # GF-T targets (diagnostic, flags per D-6 (b))
    out["t"] = _targets(plan, h32, R.region, m1r)
    for f in out["t"]["flags"]:
        out["flags"].append(f)

    # diagnostics
    out["diag"] = diagnostics(plan, h32, land, dom, r, te, s8_all)
    out["lattice_sha256"] = __import__("hashlib").sha256(h32.astype("<f4").tobytes()).hexdigest()
    return out


def _climb(h, poly):
    climb, run_min = 0.0, math.inf
    for x, z in axis_sequence(poly):
        v = float(h[z, x])
        if v - run_min > climb:
            climb = v - run_min
        run_min = min(run_min, v)
    return climb


def _m1(plan, h32, region, te):
    nodes = plan["nodes"]
    va = realised_culmination(h32, region, (nodes["M1"][0] * C, nodes["M1"][1] * C))
    vb = realised_culmination(h32, region, (nodes["M2"][0] * C, nodes["M2"][1] * C))
    out = {}
    if va is None or vb is None:
        out["status"] = "culmination not realised"
        out["why"] = "no mountain vertex within 150 m"
        return out
    prom, c1 = merge_tree_m1(h32, va, vb)
    n1 = h32.shape[1]
    out["culminations"] = [{"vertex": [v % n1, v // n1], "height": float(h32.reshape(-1)[v]), "prominence": float(prom[v]),
                            "realised": prom[v] >= M1_PROM} for v in (va, vb)]
    if not all(prom[v] >= M1_PROM for v in (va, vb)):
        out["status"] = "culmination not realised"
        return out
    cx, cz = c1 % n1, c1 // n1
    px, pz = nodes["S1"]
    dd = math.hypot(cx - px, cz - pz) * C
    out.update({"col": [cx, cz], "col_height": float(h32[cz, cx]), "col_distance_m": dd})
    if dd > M1_RADIUS:
        out["status"] = "not found"
        return out
    comp, ok = te.vertex_component(cx, cz)
    out["status"] = "found"
    out["col_reachable"] = bool(ok)
    return out


def _targets(plan, h32, region, m1r):
    t = plan["targets"]
    R = plan["raster"]
    out = {"flags": []}
    for name in ("M1", "M2"):
        v = realised_culmination(h32, region, (plan["nodes"][name][0] * C, plan["nodes"][name][1] * C))
        hv = float(h32.reshape(-1)[v]) if v is not None else None
        out[name] = {"target": t[name], "realised": hv, "deviation": (hv - t[name]) if hv is not None else None}
    if out["M1"]["realised"] is None or not (330.0 <= out["M1"]["realised"] <= 430.0):
        out["flags"].append("GF-T: main culmination outside 330-430 m")
    lows = []
    for uname in ("A", "C"):
        ui = [u["name"] for u in R.units].index(uname)
        m = (R.unit == ui) & (R.region == S.LOWLAND)
        if m.any():
            lows.append(float(np.median(h32[m])))
    level = max(lows) if lows else None
    out["lowland_level"] = level
    if m1r.get("status") == "found" and level is not None:
        off = m1r["col_height"] - level
        out["pass_offset"] = off
        if not (40.0 <= off <= 80.0):
            out["flags"].append("GF-T: pass offset outside 40-80 m")
    else:
        out["pass_offset"] = None
        out["flags"].append("GF-T: pass col not found (" + str(m1r.get("status")) + ")")
    return out


def _r5(plan, hydro):
    """GF-R5 on the production planner's network (G3e spec §7.1 R5, read regionally): systems = reaches draining to one
    ocean termination; elevated when a source node lies in the sector's mountain region or on the plateau top;
    designated when the termination lies within 150 m of a designated outlet (O1, O2, O3)."""
    R = plan["raster"]
    n1 = R.n1
    r10 = {"succeeded": bool(hydro.get("succeeded")), "failure": hydro.get("failure"), "message": hydro.get("message")}
    if not hydro.get("succeeded"):
        return {"pass": False, "reason": "planner failed"}, r10, None
    reaches = {r["id"]: r for r in hydro["reaches"]}
    outlets = [(v[0] * C, v[1] * C) for v in plan["outlets"].values()]
    systems = {}
    for r in hydro["reaches"]:
        cur = r
        end = None
        term = None
        for _ in range(100000):
            if cur["termination"] == "Ocean":
                term, end = cur["id"], cur["last_xz"]
                break
            if cur.get("downstream", -1) in reaches:
                cur = reaches[cur["downstream"]]
                continue
            if cur.get("lake_outlet", -1) in reaches:
                cur = reaches[cur["lake_outlet"]]
                continue
            term, end = -2000 - cur["id"], cur["last_xz"]
            break
        sysd = systems.setdefault(term, {"mouth": end, "elevated": False, "sources": []})
        if r["is_source"]:
            x, z = int(round(r["first_xz"][0] / C)), int(round(r["first_xz"][1] / C))
            reg = R.region[z, x] if R.land[z, x] else 0
            if reg in (S.MOUNTAIN, S.TOP) or (reg == S.CORRIDOR and plan["regions_main_dist"][z, x] < 200.0):
                sysd["elevated"] = True
                sysd["sources"].append([x, z])
    elev = [s for s in systems.values() if s["elevated"]]
    und = []
    for s in elev:
        m = s["mouth"]
        ok = any(math.hypot(m[0] - o[0], m[1] - o[1]) <= 150.0 for o in outlets)
        s["designated"] = ok
        if not ok:
            und.append(s["mouth"])
    lakes = sum(1 for lk in hydro.get("lakes", []) if R.land[int(round(lk["spill_xz"][1] / C)), int(round(lk["spill_xz"][0] / C))])
    return ({"pass": len(elev) >= 1 and not und, "elevated": len(elev), "undesignated_mouths": und,
             "systems": len(systems)}, r10, lakes)


# ----------------------------------------------------------------------------- diagnostics

def diagnostics(plan, h32, land, dom, recv, te, s8_all):
    R = plan["raster"]
    out = {}
    s1, s2 = triangle_slopes_deg(h32.astype(np.float64))
    declared = R.exempt
    # undeclared walls: land triangles of the domain with no vertex in a declared band
    def tri(m, t):
        q00, q10, q01, q11 = m[:-1, :-1], m[:-1, 1:], m[1:, :-1], m[1:, 1:]
        return (q00 & q10 & q11) if t == 1 else (q00 & q11 & q01)

    def anyv(m, t):
        q00, q10, q01, q11 = m[:-1, :-1], m[:-1, 1:], m[1:, :-1], m[1:, 1:]
        return (q00 | q10 | q11) if t == 1 else (q00 | q11 | q01)

    d1 = tri(dom & land, 1) & ~anyv(declared, 1)
    d2 = tri(dom & land, 2) & ~anyv(declared, 2)
    tot = int(d1.sum() + d2.sum())
    walls = {}
    for lo, hi in ((45, 60), (60, 70), (70, 91)):
        walls[f"{lo}-{hi}"] = int((d1 & (s1 >= lo) & (s1 < hi)).sum() + (d2 & (s2 >= lo) & (s2 < hi)).sum())
    out["undeclared_walls"] = {"triangles": tot, "bands": walls, "share_ge45": sum(walls.values()) / max(1, tot)}
    # lateral steps: same-unit 4-neighbour pairs, neither the other's D8 receiver, |dh| > c tan 45 outside bands
    n0, n1 = h32.shape
    cnt, pairs = 0, 0
    rr = recv.reshape(n0, n1)
    idx = np.arange(n0 * n1).reshape(n0, n1)
    for dz, dx in ((0, 1), (1, 0)):
        a = (slice(0, n0 - dz), slice(0, n1 - dx))
        b = (slice(dz, n0), slice(dx, n1))
        ua, ub = R.unit[a], R.unit[b]
        ok = (ua >= 0) & (ua == ub) & ~declared[a] & ~declared[b]
        notrecv = (rr[a] != idx[b]) & (rr[b] != idx[a])
        m = ok & notrecv
        pairs += int(m.sum())
        cnt += int((m & (np.abs(h32[a].astype(np.float64) - h32[b]) > C * 1.0)).sum())
    out["lateral_steps"] = {"pairs": pairs, "over_45deg": cnt, "share": cnt / max(1, pairs)}
    # slope bands per region (D-reg) over domain land triangles (region of the south-west vertex)
    reg = R.region[:-1, :-1]
    dreg = {}
    for code, name in enumerate(S.REGION_NAMES):
        for t_, s_ in ((1, s1), (2, s2)):
            m = tri(dom & land, t_) & (reg == code)
            if not m.any():
                continue
            vals = s_[m]
            d = dreg.setdefault(name, {"n": 0, "lt5": 0, "5-15": 0, "15-30": 0, "30-45": 0, "ge45": 0, "_all": []})
            d["n"] += int(vals.size)
            d["lt5"] += int((vals < 5).sum()); d["5-15"] += int(((vals >= 5) & (vals < 15)).sum())
            d["15-30"] += int(((vals >= 15) & (vals < 30)).sum()); d["30-45"] += int(((vals >= 30) & (vals < 45)).sum())
            d["ge45"] += int((vals >= 45).sum())
            d["_all"].append(vals)
    for name, d in dreg.items():
        allv = np.concatenate(d.pop("_all"))
        d["median_deg"] = float(np.median(allv))
    out["d_reg"] = dreg
    # D-cap at the configured angles
    allv = np.concatenate([s1[tri(dom & land, 1)], s2[tri(dom & land, 2)]])
    dcap = {}
    for a in (0.8, 20.0, 35.0, 38.0):
        share = float(((allv >= a - 1) & (allv <= a + 1)).mean())
        fl = [((allv >= lo) & (allv <= hi)).mean() / (hi - lo) for lo, hi in ((a - 4, a - 2), (a + 2, a + 4)) if lo >= 0]
        dcap[str(a)] = {"share": share, "peak_ratio": (share / 2.0) / float(np.mean(fl)) if fl and np.mean(fl) > 0 else None}
    out["d_cap"] = dcap
    # walkable share and steepest
    out["slopes"] = {"land_triangles": int(allv.size), "ge45_share": float((allv >= 45).mean()), "max_deg": float(allv.max()),
                     "median_deg": float(np.median(allv))}
    # coast: realised class per sector shore vertex (highest land within 60 m above sea: <4 low, 4-15 rocky, >=15 cliff)
    shore = dom & land & dilate(~land, 1)
    zs, xs = np.nonzero(shore)
    r60 = int(math.ceil(60 / C))
    classes = {"low": 0, "rocky": 0, "cliff": 0}
    agree = 0
    code = {0: "low", 1: "rocky", 2: "cliff"}
    for z, x in zip(zs, xs):
        win = h32[max(0, z - r60):z + r60 + 1, max(0, x - r60):x + r60 + 1]
        hh = float(win.max()) - SEA
        k = "low" if hh < 4 else ("rocky" if hh < 15 else "cliff")
        classes[k] += 1
        it = plan["shore_intent"][z, x]
        if it in code and code[it] == k:
            agree += 1
    tot = max(1, len(zs))
    out["coast"] = {"shares": {k: v / tot for k, v in classes.items()}, "intent_agreement": agree / tot}
    # plateau
    pl = plan["plateau"]
    top = R.zone == S.Z_TOP
    if top.any():
        tv = h32[top].astype(np.float64)
        out["plateau"] = {"top_relief_m": float(tv.max() - tv.min()), "top_median_m": float(np.median(tv))}
        ramp = R.zone == S.Z_RAMP
        if ramp.any():
            m1_ = tri(ramp, 1); m2_ = tri(ramp, 2)
            vals = np.concatenate([s1[m1_], s2[m2_]])
            out["plateau"]["ramp_triangle_max_deg"] = float(vals.max()) if vals.size else None
            out["plateau"]["ramp_triangle_p95_deg"] = float(np.percentile(vals, 95)) if vals.size else None
        k1 = k1_edge(h32.astype(np.float64), top, land)
        out["plateau"]["k1_edge_m"], out["plateau"]["k1_samples"] = k1[0], [k1[1], k1[2]]
        cls = plan["variant"]["class"]
        lo, hi = {"short": (18.0, 40.0), "tall": (60.0, 120.0)}[cls]
        e = k1[0]
        out["plateau"]["edge_class_flag"] = e is None or e < lo - 0.25 * (hi - lo) or e > hi + 0.25 * (hi - lo)
    # crest variation along declared divides (20 m samples)
    crest_cv = {}
    for name, line in plan["div_lines"].items():
        vals = [float(h32[z, x]) for k, (x, z) in enumerate(line) if k % 7 == 0]
        if len(vals) > 2:
            crest_cv[name] = float(np.std(vals) / max(1e-6, np.mean(vals)))
    out["crest_cv"] = crest_cv
    # settlement: buildable area inside the reserve
    slope = vertex_slope_deg(h32)
    rm = plan["reserve"]["mask"]
    out["reserve_buildable_ha"] = float(((slope <= BUILDABLE_DEG) & rm & land).sum() * C * C / 1e4)
    out["s8_world"] = len(s8_all)
    return out


def k1_edge(lat, top, land, cell=C):
    """K1 plateau edge height (G3e spec §2.4): median edge drop over escarpment samples."""
    n0, n1 = lat.shape
    s1, s2 = triangle_slopes_deg(lat)
    steep = np.zeros((n0, n1), bool)
    m1, m2 = s1 >= 45, s2 >= 45
    steep[:-1, :-1] |= m1 | m2
    steep[:-1, 1:] |= m1
    steep[1:, 1:] |= m1 | m2
    steep[1:, :-1] |= m2
    rim = top & dilate(~top, 1)
    drops = []
    r15 = int(math.ceil(15 / cell))
    for z, x in zip(*np.nonzero(rim)):
        cz, cx = z, x
        path, low = 0.0, 0
        foot = (z, x)
        while path < 150.0:
            best, bz, bx, bd = 0.0, None, None, 0.0
            for dz, dx in NB8:
                zz, xx = cz + dz, cx + dx
                if 0 <= zz < n0 and 0 <= xx < n1 and land[zz, xx] and not top[zz, xx]:
                    d = cell * (DIAG if dz and dx else 1.0)
                    s = (lat[cz, cx] - lat[zz, xx]) / d
                    if s > best:
                        best, bz, bx, bd = s, zz, xx, d
            if bz is None:
                break
            path += bd
            cz, cx = bz, bx
            if math.degrees(math.atan(best)) >= 15.0:
                foot = (cz, cx)
                low = 0
            else:
                low += 1
                if low >= 2:
                    break
        drop = lat[z, x] - lat[foot]
        win = steep[max(0, z - r15):z + r15 + 1, max(0, x - r15):x + r15 + 1] & ~top[max(0, z - r15):z + r15 + 1, max(0, x - r15):x + r15 + 1]
        if drop >= 10.0 and win.any():
            drops.append(drop)
    return (float(np.median(drops)) if drops else None), len(drops), int(rim.sum())
