"""G3f surface synthesis: the shared contract and the three candidate laws (GF-C0, GF-UF, GF-RF).

Specification: unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md §§6-7. Everything here works on a generic plan
raster (`Raster`), so the same code runs on the synthetic lattices of the analytic checks and on the sector.

Owners. Every land vertex of the synthesised domain has exactly one owner: a unit (catchment or coastal fringe) or a
crest line (a boundary record). Steps read anything but write only their owner's vertices; the final assembly writes
each vertex once and logs it (GF-O).

Pipeline (spec §7.1), for every candidate:
  S1 tree (built once from the plan)  S2 drainage area  S3 interior law (candidate-specific)
  S4 plateau sub-law                  S5 crest heights   S6 coastal fringe sub-law
  S7 divide contract (ceiling + carve, flank fill + repair)  S8 Lipschitz lowering (lateral constraint)  S9 assembly
"""
import hashlib
import heapq
import math

import numpy as np

from .lattice import BASE, C, DIAG, NB8, edt

# ----------------------------------------------------------------------------- region codes (G3f's own enum)

SEA, LOWLAND, UPLAND, MOUNTAIN, TOP, BAND, RAMP, CORRIDOR, COAST_LOW, COAST_ROCKY, COAST_CLIFF = range(11)
REGION_NAMES = ["sea", "lowland", "upland", "mountain", "plateau-top", "plateau-band", "ramp", "corridor",
                "coast-low", "coast-rocky", "coast-cliff"]

# footprint zones (plateau write footprint)
Z_NONE, Z_TOP, Z_BAND, Z_RAMP, Z_RIMCH, Z_RSIDE = range(6)

# unit kinds and crest kinds
CATCHMENT, FRINGE = "catchment", "fringe"
DIVIDE, SECTOR, FRINGE_CREST = "divide", "sector", "fringe"


# ----------------------------------------------------------------------------- stateless hashing

_M64 = (1 << 64) - 1


def _mix64(x):
    x = (x + 0x9E3779B97F4A7C15) & _M64
    x = ((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9) & _M64
    x = ((x ^ (x >> 27)) * 0x94D049BB133111EB) & _M64
    return x ^ (x >> 31)


def stream_key(name, seed):
    """64-bit key of a named stream for a world seed (SHA-256 of 'name:seed')."""
    return int.from_bytes(hashlib.sha256(f"{name}:{seed}".encode()).digest()[:8], "little")


def vhash(key, i):
    return _mix64(key ^ _mix64(i))


# ----------------------------------------------------------------------------- parameters

class Params:
    """Rule values (spec §7.6). Every value is in world units; [A] = authoring default."""

    def __init__(self, **kw):
        # tree (S1): cost per metre by region [A]; axis attraction [A]
        self.tree_w = {LOWLAND: 0.005, CORRIDOR: 0.01, UPLAND: 0.10, MOUNTAIN: 0.45, TOP: 0.01, BAND: 1.0,
                       RAMP: 0.25, COAST_LOW: 0.05, COAST_ROCKY: 0.05, COAST_CLIFF: 0.05}
        self.axis_factor = 0.5          # cost multiplier within axis_halo of a declared axis
        self.axis_halo = 1.5 * C        # m
        self.axis_alpha = 1.0           # cost grows by (1 + alpha * min(D_axis, axis_span) / axis_span)
        self.axis_span = 300.0          # m
        # plan guide (S1; calibration fix F1): inside a catchment, a tree edge whose flow climbs the guide G -- the
        # 35-degree cone of the planned crest profiles, max_q [c(q) - D * flank], read from the plan only -- adds
        # guide_climb per metre of climb to the search priority, so drainage leaves a planned crest across its flank
        # instead of running along its foot toward a higher crest segment [A]
        self.guide_climb = 2.0
        # fluvial law (S3, GF-UF / GF-RF) [A]: theta and A_c as G3e spec §5.2; cap 35 deg (= the lateral bound)
        self.theta = 0.45
        self.a_c = 2000.0               # m2
        self.cap = math.tan(math.radians(35.0))
        # region factors chosen so the hillslope maximum k f A_c^-theta is ~1.3 % (lowland), ~3 % (corridor floor),
        # ~26 % (upland) and the cap (mountain) at k_uf = 100 [A]
        self.law_f = {LOWLAND: 0.004, CORRIDOR: 0.01, UPLAND: 0.08, MOUNTAIN: 1.0, TOP: 0.0, RAMP: 0.08}
        self.k_uf = 100.0               # starting global factor; bisection scales the mountain factor
        # control law (S3, GF-C0) [A]: height = BASE + kappa * cost-distance along the tree
        self.kappa = 1.0
        # plateau (S4) [A]
        self.top_grade = math.tan(math.radians(0.8))
        self.band_power = 2.0
        self.rside_power = 1.0
        self.ramp_max = math.tan(math.radians(20.0))
        # crest (S5) and divide contract (S7) [A]
        self.crest_delta = 0.5          # m above the highest adjacent interior vertex (emergent crests)
        self.flank = math.tan(math.radians(35.0))
        self.flank_reach = 30.0         # m: the crest cone fills only this close to a crest line
        self.culm_radius = 300.0        # m: reach of a culmination factor (GF-UF)
        self.eps = 1e-3                 # m per m, minimum rise along a tree edge in repairs
        # coastal fringe (S6) [A]
        self.fringe_floor = {COAST_LOW: 1.0, COAST_ROCKY: 6.0, COAST_CLIFF: 20.0}   # crest at >= 24 m + floor
        # lateral constraint (S8) [A]: Lipschitz bound tan 35 deg -> every triangle outside exempt bands < 45 deg
        self.lip = math.tan(math.radians(35.0))
        # bisection (L-4) carried: <= 12 steps, +-2 m
        self.bisect_steps = 12
        self.bisect_tol = 2.0
        for k, v in kw.items():
            if not hasattr(self, k):
                raise KeyError(k)
            setattr(self, k, v)

    def snapshot(self):
        out = {}
        for k, v in sorted(self.__dict__.items()):
            out[k] = {str(a): b for a, b in sorted(v.items())} if isinstance(v, dict) else v
        return out


# ----------------------------------------------------------------------------- the plan raster

class Raster:
    """What synthesis reads. Arrays are (n0, n1); flat index i = z * n1 + x.

    land       bool    land vertices of the synthesised domain (False outside it)
    unit       int32   unit id for unit vertices, -1 otherwise
    crest      int32   crest (boundary record) id for crest-line vertices, -1 otherwise
    region     uint8   region code
    root       bool    roots of unit trees (catchment outlet segments, fringe shore vertices), height BASE
    zone       uint8   plateau footprint zone (Z_*), plateau id in `plateau_of`
    plateau_of int32   plateau id or -1
    axis       bool    vertices on a declared valley axis (rasterised polyline)
    exempt     bool    vertices exempt from the Lipschitz bound (declared escarpment and cliff bands)
    fixed      float   heights given from outside (context, sea); NaN where synthesised
    units      list of dict  kind, name, params
    crests     list of dict  kind, name; 'plan' float array (n0*n1, NaN where unplanned) for planned crests
    plateaus   list of dict  edge target, class, ramp polyline (vertex coords), landing vertex, top-end vertex
    """

    def __init__(self, n0, n1, cell=C):
        self.n0, self.n1, self.cell = n0, n1, cell
        self.land = np.zeros((n0, n1), bool)
        self.unit = np.full((n0, n1), -1, np.int32)
        self.crest = np.full((n0, n1), -1, np.int32)
        self.region = np.zeros((n0, n1), np.uint8)
        self.root = np.zeros((n0, n1), bool)
        self.zone = np.zeros((n0, n1), np.uint8)
        self.plateau_of = np.full((n0, n1), -1, np.int32)
        self.axis = np.zeros((n0, n1), bool)
        self.exempt = np.zeros((n0, n1), bool)
        self.fixed = np.full((n0, n1), np.nan)
        self.units = []
        self.crests = []
        self.plateaus = []
        self.axes = []          # declared valley axes: lists of (x, z) vertex polylines, source first, mouth last

    def axis_down(self):
        """Forced receivers along declared axes: each axis vertex drains to the next vertex of its read sequence
        (checks.axis_sequence); an axis that reaches a vertex of an earlier axis ends there. Returns {flat: flat}."""
        from .checks import axis_sequence
        down = {}
        on = set()
        for poly in self.axes:
            seq = axis_sequence(poly)
            prev = None
            seen = set()
            for x, z in seq:
                i = z * self.n1 + x
                if i in seen:
                    break           # a read sequence that returns to a vertex ends there (no cycle)
                seen.add(i)
                if prev is not None and prev not in down:
                    down[prev] = i
                if i in on:
                    break
                on.add(i)
                prev = i
        return down

    def owner_codes(self):
        """Per vertex: unit id (>= 0), -(2 + crest id) for crest vertices, -1 elsewhere."""
        o = self.unit.astype(np.int64).copy()
        cm = self.crest >= 0
        o[cm] = -(2 + self.crest[cm].astype(np.int64))
        return o


# ----------------------------------------------------------------------------- S1: tree

def _allowed(zc, zp, same_plateau):
    """May water flow from child zone zc into parent zone zp (parent = receiver)?"""
    if zc == Z_NONE:
        return zp == Z_NONE
    if not same_plateau and zp != Z_NONE:
        return False
    if zc == Z_TOP:
        return zp in (Z_TOP, Z_RIMCH)
    if zc == Z_BAND:
        return zp in (Z_NONE, Z_BAND, Z_RSIDE)
    if zc == Z_RIMCH:
        return zp in (Z_NONE, Z_RIMCH)
    if zc == Z_RAMP:
        return zp in (Z_NONE, Z_RAMP)
    if zc == Z_RSIDE:
        return zp in (Z_NONE, Z_RSIDE)
    return False


def build_tree(R, P, seed=0):
    """Receivers built once from the plan (L-1): a Dijkstra from each unit's roots over its own vertices.

    Cost per metre: region weight x axis attraction; ties by (priority, stateless hash, index). Fringe units use a
    uniform cost. Inside catchments an edge whose flow climbs the plan guide G (P.guide_climb) adds guide_climb x
    the climb to the search priority. Returns recv (flat, -1 for roots and non-unit vertices), order (roots first,
    increasing priority), cost (flat weighted length along the tree, without the guide term) and step length per
    vertex."""
    n0, n1, cell = R.n0, R.n1, R.cell
    n = n0 * n1
    unit = R.unit.reshape(-1).tolist()
    zone = R.zone.reshape(-1).tolist()
    pof = R.plateau_of.reshape(-1).tolist()
    region = R.region.reshape(-1).tolist()
    # axis attraction
    axis_mask = R.axis.copy()
    if R.axes:
        from .checks import axis_sequence
        for poly in R.axes:
            for x, z in axis_sequence(poly):
                axis_mask[z, x] = True
    if axis_mask.any():
        dax = edt(axis_mask, cell).reshape(-1)
    else:
        dax = np.full(n, np.inf)
    kinds = [u["kind"] for u in R.units]
    w = [0.0] * n
    dax_l = dax.tolist()
    for i in range(n):
        u = unit[i]
        if u < 0:
            continue
        if kinds[u] == FRINGE:
            w[i] = 1.0
            continue
        base = P.tree_w.get(region[i], 0.1)
        d = dax_l[i]
        if d <= P.axis_halo:
            w[i] = base * P.axis_factor
        else:
            w[i] = base * (1.0 + P.axis_alpha * min(d, P.axis_span) / P.axis_span)
    down = R.axis_down() if R.axes else {}
    key = stream_key("research.g3f.synth.tree", seed)
    guide = [0.0] * n
    if P.guide_climb > 0.0:
        planned = crest_target_field(R, P)
        cone = _crest_cone(R, P, {int(i): float(planned[i]) for i in np.flatnonzero(~np.isnan(planned))},
                           lambda j: unit[j] >= 0 and kinds[unit[j]] == CATCHMENT)
        for j, v in cone.items():
            guide[j] = v
    prio = [math.inf] * n
    cost = [math.inf] * n
    recv = [-1] * n
    steplen = [0.0] * n
    heap = []
    roots = np.flatnonzero(R.root.reshape(-1)).tolist()
    for i in roots:
        if unit[i] >= 0:
            prio[i] = 0.0
            cost[i] = 0.0
            heap.append((0.0, vhash(key, i), i))
    heapq.heapify(heap)
    done = [False] * n
    order = []
    offs = [(dz, dx, cell * (DIAG if dz and dx else 1.0)) for dz, dx in NB8]
    while heap:
        d, _, i = heapq.heappop(heap)
        if done[i]:
            continue
        done[i] = True
        order.append(i)
        z, x = divmod(i, n1)
        ui, zi, pi = unit[i], zone[i], pof[i]
        for dz, dx, L in offs:
            zz, xx = z + dz, x + dx
            if zz < 0 or zz >= n0 or xx < 0 or xx >= n1:
                continue
            j = zz * n1 + xx
            if done[j] or unit[j] != ui:
                continue
            if not _allowed(zone[j], zi, pof[j] == pi):
                continue
            if j in down and down[j] != i:
                continue
            wl = L * 0.5 * (w[i] + w[j])
            nd = d + wl + P.guide_climb * max(0.0, guide[i] - guide[j])
            if nd < prio[j]:
                prio[j] = nd
                cost[j] = cost[i] + wl
                recv[j] = i
                steplen[j] = L
                heapq.heappush(heap, (nd, vhash(key, j), j))
    unreached = [i for i in range(n) if unit[i] >= 0 and not done[i]]
    return {"recv": recv, "order": order, "cost": cost, "step": steplen, "unreached": unreached}


def areas(R, T):
    """Drainage area (m2) along the tree: A_i = c^2 (1 + sum of donors' A / c^2)."""
    a0 = R.cell * R.cell
    A = [0.0] * (R.n0 * R.n1)
    for i in T["order"]:
        A[i] = a0
    recv = T["recv"]
    for i in reversed(T["order"]):
        r = recv[i]
        if r >= 0:
            A[r] += A[i]
    return A


# ----------------------------------------------------------------------------- S3: interior laws

def _shape(A, P):
    """The law's slope shape g(A) (G3e spec §5.2 form; S-LM3's code is not used)."""
    if A >= P.a_c:
        return A ** (-P.theta)
    return P.a_c ** (-P.theta) * (A / P.a_c)


def _culm_field(R, P, culm):
    """1 + sum_c a_c max(0, 1 - D(v, c) / R_c) over the culmination factors (x, z, a, R_c)."""
    if not culm:
        return None
    zz, xx = np.mgrid[0:R.n0, 0:R.n1]
    f = np.ones((R.n0, R.n1))
    for x, z, a, rad in culm:
        f += a * np.maximum(0.0, 1.0 - np.hypot(xx - x, zz - z) * R.cell / rad)
    return f.reshape(-1)


def law_uf(R, T, A, P, mult_mountain=1.0, culm=None, unit_mult=None, unit_culm=None, unit_extra=None):
    """GF-UF interior (and GF-RF's, per unit): h = h_r + d * min(cap, k * f_region * g(A)), integrated once from the
    roots upward. The mountain, upland and corridor factors are scaled by mult (one value, GF-UF; one per unit,
    GF-RF) and by culmination factors (one list, GF-UF; one list per unit, GF-RF). Returns a dict vertex -> height."""
    recv, step, order = T["recv"], T["step"], T["order"]
    unit = R.unit.reshape(-1)
    region = R.region.reshape(-1)
    zone = R.zone.reshape(-1)
    kinds = [u["kind"] for u in R.units]
    cf = _culm_field(R, P, culm)
    ucf = {u: _culm_field(R, P, c) for u, c in (unit_culm or {}).items()}
    h = {}
    for i in order:
        u = unit[i]
        if kinds[u] != CATCHMENT:
            continue
        r = recv[i]
        if r < 0:
            h[i] = BASE
            continue
        if zone[i] in (Z_TOP, Z_BAND, Z_RIMCH):
            continue  # plateau sub-law (S4); ramp and ramp-side vertices get provisional open-ground heights
        reg = int(region[i])
        f = P.law_f.get(reg, 0.4)
        if reg in (MOUNTAIN, UPLAND, CORRIDOR):
            m = unit_mult.get(u, 1.0) if unit_mult is not None else mult_mountain
            c = ucf.get(u) if unit_culm is not None else cf
            f *= m * (c[i] if c is not None else 1.0) * (unit_extra.get(u, 1.0) if unit_extra else 1.0)
        s = min(P.cap, P.k_uf * f * _shape(A[i], P))
        h[i] = h[r] + step[i] * s
    return h


def law_c0(R, T, P, kappa=None):
    """GF-C0 interior: height = BASE + kappa * cost distance along the tree (no law)."""
    kappa = P.kappa if kappa is None else kappa
    unit = R.unit.reshape(-1)
    zone = R.zone.reshape(-1)
    kinds = [u["kind"] for u in R.units]
    h = {}
    for i in T["order"]:
        if kinds[unit[i]] != CATCHMENT or zone[i] in (Z_TOP, Z_BAND, Z_RIMCH):
            continue
        h[i] = BASE + kappa * T["cost"][i]
    return h


def crest_target_field(R, P):
    """Planned crest heights as a flat array (NaN where no planned crest)."""
    out = np.full(R.n0 * R.n1, np.nan)
    for c in R.crests:
        if c.get("plan") is not None:
            m = ~np.isnan(c["plan"])
            out[m] = c["plan"][m]
    return out


# ----------------------------------------------------------------------------- S4: plateau sub-law

def _nearest_label(src_mask, dom_mask, values, n1):
    """Multi-source BFS (8-neighbour, index order) inside dom_mask from src_mask; returns per-vertex the value of the
    nearest source (by step count, ties by queue order) for vertices in dom_mask."""
    from collections import deque
    n = src_mask.size
    src = src_mask.reshape(-1)
    dom = dom_mask.reshape(-1)
    lab = np.full(n, np.nan)
    seen = np.zeros(n, bool)
    dq = deque()
    for i in np.flatnonzero(src).tolist():
        lab[i] = values[i]
        seen[i] = True
        dq.append(i)
    n0 = n // n1
    while dq:
        i = dq.popleft()
        z, x = divmod(i, n1)
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                if dom[j] and not seen[j]:
                    seen[j] = True
                    lab[j] = lab[i]
                    dq.append(j)
    return lab


def plateau_sublaw(R, T, P, h):
    """L-5 inside each plateau's write footprint: rim channels rise by the edge target, the top is graded to its
    rim outlets, the band and the ramp side bands are two-boundary profiles, the ramp is linear to its landing.
    Writes only footprint vertices (into h); returns per-plateau records."""
    n0, n1, cell = R.n0, R.n1, R.cell
    recv, step, order = T["recv"], T["step"], T["order"]
    zone = R.zone.reshape(-1)
    pof = R.plateau_of.reshape(-1)
    land = R.land.reshape(-1)
    recs = []
    hv = np.full(n0 * n1, np.nan)
    for i, v in h.items():
        hv[i] = v
    for pid, pl in enumerate(R.plateaus):
        fp = (pof == pid)
        # rim channels and top by integration along the tree (channel slope = edge / channel length)
        ch_len = {}
        for i in order:
            if not fp[i]:
                continue
            r = recv[i]
            if zone[i] == Z_RIMCH:
                base = ch_len.get(r, 0.0) if (r >= 0 and zone[r] == Z_RIMCH) else 0.0
                ch_len[i] = base + step[i]
        Lmax = {}
        for i, L in ch_len.items():
            Lmax[i] = L
        # E_top = median height of the ground ring outside the escarpment band + the edge target; each rim channel
        # climbs from its foot to E_top over its length (L-5)
        Lc = max(ch_len.values()) if ch_len else 1.0
        bandm = (fp & (zone == Z_BAND)).reshape(n0, n1)
        ringm = _ring(bandm, 1).reshape(-1) & ~fp & land & (R.unit.reshape(-1) >= 0) & ~np.isnan(hv)
        foot_ring = float(np.median(hv[ringm])) if ringm.any() else float("nan")
        e_top = foot_ring + pl["edge"]
        heads = [i for i in ch_len if not any(recv[j] == i for j in ch_len)]
        ch_foot = {}
        for i in order:
            if fp[i] and zone[i] == Z_RIMCH:
                r = recv[i]
                ch_foot[i] = ch_foot[r] if (r >= 0 and zone[r] == Z_RIMCH) else hv[r]
        s_ch = {i: max(P.eps, (e_top - ch_foot[i]) / Lc) for i in ch_len}
        for i in order:
            if not fp[i]:
                continue
            r = recv[i]
            if zone[i] == Z_RIMCH:
                hv[i] = hv[r] + step[i] * s_ch[i]
            elif zone[i] == Z_TOP:
                hv[i] = hv[r] + step[i] * P.top_grade
        top = fp & (zone == Z_TOP)
        outside = land & (zone == Z_NONE) & (R.unit.reshape(-1) >= 0) & ~np.isnan(hv)
        # band: foot from the nearest outside vertex, top edge from the nearest top vertex
        band = fp & (zone == Z_BAND)
        if band.any():
            dom = band | top | outside
            foot = _nearest_label(outside.reshape(n0, n1) & _ring(band.reshape(n0, n1), 3),
                                  (band | outside).reshape(n0, n1), hv, n1)
            topv = _nearest_label(top.reshape(n0, n1), (band | top).reshape(n0, n1), hv, n1)
            d_out = edt(outside.reshape(n0, n1), cell).reshape(-1)
            d_top = edt(top.reshape(n0, n1), cell).reshape(-1)
            for i in np.flatnonzero(band).tolist():
                t = d_out[i] / (d_out[i] + d_top[i]) if (d_out[i] + d_top[i]) > 0 else 0.0
                f, e = foot[i], topv[i]
                if np.isnan(f) or np.isnan(e):
                    continue
                hv[i] = f + (e - f) * (t ** P.band_power) if e > f else f + (e - f) * t
        # ramp: linear from the top end to the landing along the ramp polyline
        ramp = fp & (zone == Z_RAMP)
        rec = {"plateau": pid, "channel_len_m": Lc, "foot_ring_median": foot_ring, "e_top_target": e_top,
               "channel_slope_max": max(s_ch.values()) if s_ch else None}
        if ramp.any():
            # arc length along the ramp: geodesic distance inside the ramp from its vertices touching the top
            from .lattice import dijkstra8
            rampm = ramp.reshape(n0, n1)
            topm = top.reshape(n0, n1)
            src = {i: 0.0 for i in np.flatnonzero(ramp & _ring(topm, 1).reshape(-1)).tolist()}
            arc, _ = dijkstra8(rampm, src, np.ones((n0, n1)), cell)
            arc = arc.reshape(-1)
            ld = pl["landing_vertex"]
            e_top = float(np.nanmax(hv[top])) if top.any() else float("nan")
            e_top = float(np.nanmean([hv[j] for j in _neighbours_in(src.keys(), top, n0, n1)])) if src else e_top
            e_land = hv[ld[1] * n1 + ld[0]]
            L = float(np.nanmax(np.where(np.isfinite(arc) & ramp, arc, np.nan)))
            grade = (e_top - e_land) / L if L > 0 else 0.0
            capped = grade > P.ramp_max
            grade = min(grade, P.ramp_max)
            rec.update({"ramp_len_m": L, "ramp_grade": grade, "ramp_capped": bool(capped), "e_top_end": e_top,
                        "e_landing": float(e_land)})
            for i in np.flatnonzero(ramp).tolist():
                line = e_top - grade * (arc[i] if np.isfinite(arc[i]) else L)
                g = hv[i]          # the law's provisional open-ground height under the ramp (its floor)
                hv[i] = min(e_top, max(line, g)) if np.isfinite(g) else line
        rside = fp & (zone == Z_RSIDE)
        if rside.any():
            rampm = ramp.reshape(n0, n1)
            outside2 = land & (zone == Z_NONE) & ~np.isnan(hv)
            gnd = _nearest_label(outside2.reshape(n0, n1) & _ring(rside.reshape(n0, n1), 3),
                                 (rside | outside2).reshape(n0, n1), hv, n1)
            rv = _nearest_label(rampm, (rside | ramp).reshape(n0, n1), hv, n1)
            d_g = edt(outside2.reshape(n0, n1), cell).reshape(-1)
            d_r = edt(rampm, cell).reshape(-1)
            for i in np.flatnonzero(rside).tolist():
                t = d_g[i] / (d_g[i] + d_r[i]) if (d_g[i] + d_r[i]) > 0 else 0.0
                g_, r_ = gnd[i], rv[i]
                if np.isnan(g_) or np.isnan(r_):
                    continue
                hv[i] = g_ + (r_ - g_) * (t ** P.rside_power) if r_ > g_ else g_ + (r_ - g_) * t
        # monotone repair inside the footprint along the tree
        for i in order:
            if fp[i] and recv[i] >= 0 and not np.isnan(hv[recv[i]]):
                lo = hv[recv[i]] + P.eps * step[i]
                if np.isnan(hv[i]) or hv[i] < lo:
                    hv[i] = lo
        tv = hv[top]
        rec.update({"top_min": float(np.nanmin(tv)) if tv.size else None, "top_max": float(np.nanmax(tv)) if tv.size else None})
        recs.append(rec)
        for i in np.flatnonzero(fp).tolist():
            h[i] = float(hv[i])
    return recs


def _neighbours_in(vertices, mask, n0, n1):
    """Flat indices in mask that are 8-neighbours of any of the given vertices (sorted, unique)."""
    out = set()
    for i in vertices:
        z, x = divmod(i, n1)
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1 and mask[zz * n1 + xx]:
                out.add(zz * n1 + xx)
    return sorted(out)


def dilate_mask(mask, r):
    from .lattice import dilate
    return dilate(mask, r)


def _ring(mask, r):
    """Vertices within r 8-steps of mask (mask included)."""
    out = mask.copy()
    for _ in range(r):
        m = out.copy()
        m[1:, :] |= out[:-1, :]; m[:-1, :] |= out[1:, :]; m[:, 1:] |= out[:, :-1]; m[:, :-1] |= out[:, 1:]
        m[1:, 1:] |= out[:-1, :-1]; m[:-1, :-1] |= out[1:, 1:]; m[1:, :-1] |= out[:-1, 1:]; m[:-1, 1:] |= out[1:, :-1]
        out = m
    return out


def _arc_of(poly, p):
    """Arc position (in vertex units) of the projection of p onto the polyline."""
    best = (math.inf, 0.0)
    acc = 0.0
    for a, b in zip(poly[:-1], poly[1:]):
        dx, dz = b[0] - a[0], b[1] - a[1]
        L2 = dx * dx + dz * dz
        t = 0.0 if L2 == 0 else max(0.0, min(1.0, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dz) / L2))
        qx, qz = a[0] + t * dx, a[1] + t * dz
        d = math.hypot(p[0] - qx, p[1] - qz)
        if d < best[0]:
            best = (d, acc + t * math.sqrt(L2))
        acc += math.sqrt(L2)
    return best[1]


# ----------------------------------------------------------------------------- S5: crest heights

def _adjacent_land_max(R, h, i, crest, unit, fixed):
    n0, n1 = R.n0, R.n1
    z, x = divmod(i, n1)
    m = -math.inf
    for dz, dx in NB8:
        zz, xx = z + dz, x + dx
        if 0 <= zz < n0 and 0 <= xx < n1:
            j = zz * n1 + xx
            if crest[j] >= 0:
                continue
            if unit[j] >= 0 and j in h:
                m = max(m, h[j])
            elif unit[j] < 0 and not math.isnan(fixed[j]) and fixed[j] > 24.0:
                m = max(m, fixed[j])
    return m


def provisional_crests(R, P, h, mode):
    """S5. Planned crest vertices (every Sector crest; in mode 'plan' every crest whose record has a planned height)
    take their planned height; the others take the highest adjacent catchment or context vertex of the interior law
    + delta (floored per fringe intent). Returns (crest heights, set of planned crest vertices)."""
    crest = R.crest.reshape(-1)
    unit = R.unit.reshape(-1)
    fixed = R.fixed.reshape(-1)
    plan = crest_target_field(R, P)
    kinds = [c["kind"] for c in R.crests]
    ukinds = [u["kind"] for u in R.units]
    hc = {j: v for j, v in h.items() if unit[j] >= 0 and ukinds[unit[j]] == CATCHMENT}
    out, planned = {}, set()
    for i in np.flatnonzero(crest >= 0).tolist():
        cid = crest[i]
        if (R.crests[cid].get("always_planned") or mode == "plan") and not np.isnan(plan[i]):
            out[i] = float(plan[i])
            planned.add(i)
            continue
        m = _adjacent_land_max(R, hc, i, crest, unit, fixed)
        floor = 24.0 + R.crests[cid].get("floor", 1.0) if kinds[cid] == FRINGE_CREST else -math.inf
        val = max(m + P.crest_delta, floor)
        out[i] = val if math.isfinite(val) else BASE + P.crest_delta
    return out, planned


def final_crests(R, P, h, crest_h, planned):
    """S9 (crests). Planned crest vertices keep their height; every other crest vertex is the highest adjacent
    non-crest land vertex of the final surface + delta (floored per fringe intent), so no adjacent vertex can drain
    onto it. A crest vertex with no non-crest land neighbour is the highest adjacent crest vertex + delta."""
    crest = R.crest.reshape(-1)
    unit = R.unit.reshape(-1)
    fixed = R.fixed.reshape(-1)
    kinds = [c["kind"] for c in R.crests]
    out = {}
    pending = []
    for i in np.flatnonzero(crest >= 0).tolist():
        if i in planned:
            out[i] = crest_h[i]
            continue
        m = _adjacent_land_max(R, h, i, crest, unit, fixed)
        cid = crest[i]
        floor = 24.0 + R.crests[cid].get("floor", 1.0) if kinds[cid] == FRINGE_CREST else -math.inf
        if math.isfinite(m):
            out[i] = max(m + P.crest_delta, floor)
        else:
            pending.append(i)
    n0, n1 = R.n0, R.n1
    for i in pending:
        z, x = divmod(i, n1)
        m = BASE
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1 and (zz * n1 + xx) in out:
                m = max(m, out[zz * n1 + xx])
        out[i] = m + P.crest_delta
    return out


# ----------------------------------------------------------------------------- S6: coastal fringe sub-law

def fringe_sublaw(R, T, P, h, crest_h):
    """L-6: each fringe vertex lies on a two-boundary profile between the shore (BASE) and its unit's inner crest,
    shaped by intent; written only inside fringe units; then a monotone repair along the fringe tree."""
    n0, n1, cell = R.n0, R.n1, R.cell
    unit = R.unit.reshape(-1)
    region = R.region.reshape(-1)
    root = R.root.reshape(-1)
    isfr = np.array([u["kind"] == FRINGE for u in R.units] + [False])
    fr = isfr[np.where(unit >= 0, unit, len(R.units))]
    if not fr.any():
        return
    crest = R.crest.reshape(-1)
    cmask = np.zeros(n0 * n1, bool)
    cval = np.full(n0 * n1, np.nan)
    for i, v in crest_h.items():
        if R.crests[crest[i]]["kind"] == FRINGE_CREST:
            cmask[i] = True
            cval[i] = v
    d_c = edt(cmask.reshape(n0, n1), cell).reshape(-1)
    near_c = _nearest_label(cmask.reshape(n0, n1), (fr | cmask).reshape(n0, n1), cval, n1)
    cost = T["cost"]
    for i in np.flatnonzero(fr).tolist():
        if root[i]:
            h[i] = BASE
            continue
        a, b = cost[i], max(0.0, d_c[i] - cell)
        t = a / (a + b) if (a + b) > 0 else 1.0
        top = near_c[i] if not np.isnan(near_c[i]) else BASE
        reg = int(region[i])
        if reg == COAST_CLIFF:
            u = 1.0 - (1.0 - t) ** 3
        elif reg == COAST_ROCKY:
            u = t
        else:
            u = t ** 1.5
        h[i] = BASE + max(0.0, top - BASE) * u
    recv, step = T["recv"], T["step"]
    for i in T["order"]:
        if fr[i] and recv[i] >= 0:
            lo = h[recv[i]] + P.eps * step[i]
            if h[i] < lo:
                h[i] = lo


# ----------------------------------------------------------------------------- S7: divide contract

def flank_fill(R, T, P, h, crest_h, planned):
    """S7a. No catchment vertex outside a footprint lies below the crest cone max_q [c(q) - D * flank] (a raise),
    capped next to planned crests at (adjacent planned crest - delta); then every catchment tree edge is made to
    rise by at least eps per metre (a repair that raises donors)."""
    n0, n1 = R.n0, R.n1
    unit = R.unit.reshape(-1)
    zone = R.zone.reshape(-1)
    ukinds = [u["kind"] for u in R.units]
    recv, step, order = T["recv"], T["step"], T["order"]

    def mine(j):
        u = unit[j]
        return u >= 0 and ukinds[u] == CATCHMENT

    ceil = {}
    for i in planned:
        z, x = divmod(i, n1)
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                if mine(j):
                    ceil[j] = min(ceil.get(j, math.inf), crest_h[i] - P.crest_delta)
    cone = _crest_cone(R, P, crest_h, mine)
    root = R.root.reshape(-1)
    # an emergent crest needs only a short transition (flank_reach); a planned crest is carried by its flanks wherever
    # the cone stands above the law (no reach limit), so a planned crest is never a free-standing wall
    cmask = np.zeros(R.n0 * R.n1, bool)
    cmask[[i for i in crest_h if i not in planned]] = True
    near = (edt(cmask.reshape(R.n0, R.n1), R.cell) <= P.flank_reach).reshape(-1) if cmask.any() else np.zeros(R.n0 * R.n1, bool)
    if planned:
        pcone = _crest_cone(R, P, {i: crest_h[i] for i in planned}, mine)
        near_p = np.zeros(R.n0 * R.n1, bool)
        near_p[list(pcone.keys())] = True
        near |= near_p
    raised = 0
    for i, v in cone.items():
        if not near[i]:
            continue
        v = min(v, ceil.get(i, math.inf))
        if i in h and zone[i] == Z_NONE and not root[i] and h[i] < v:
            h[i] = v
            raised += 1
    repaired = 0
    for i in order:
        r = recv[i]
        if r >= 0 and mine(i) and i in h and r in h:
            lo = h[r] + P.eps * step[i]
            if h[i] < lo:
                h[i] = lo
                repaired += 1
    return {"flank_raised": raised, "flank_repaired": repaired}


def planned_ceiling(R, T, P, h, crest_h, planned):
    """S7b. Every unit vertex next to a planned crest vertex lies at least delta below it (a lowering; never below
    BASE + 2 eps c), then the tree path below each lowered vertex is carved so that every edge on it falls by at
    least eps per metre toward the root (receivers lowered, roots never). Only lowers; leaves no pit."""
    n0, n1 = R.n0, R.n1
    unit = R.unit.reshape(-1)
    root = R.root.reshape(-1)
    recv, step, cost = T["recv"], T["step"], T["cost"]
    lowered = []
    for i in planned:
        c = crest_h[i]
        z, x = divmod(i, n1)
        for dz, dx in NB8:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                lim = max(c - P.crest_delta, BASE + P.eps * R.cell * 2.0)
                if unit[j] >= 0 and not root[j] and j in h and h[j] > lim:
                    h[j] = lim
                    lowered.append(j)
    carved = 0
    for v in sorted(set(lowered), key=lambda i: (-cost[i], i)):
        cur = v
        r = recv[cur]
        while r >= 0 and recv[r] >= 0 and r in h:
            lim = h[cur] - P.eps * step[cur]
            if h[r] <= lim:
                break
            h[r] = lim
            carved += 1
            cur, r = r, recv[r]
    return {"ceiling_lowered": len(set(lowered)), "ceiling_carved": carved}


def lateral_and_axes(R, P, h, kinds, max_rounds=20):
    """S8: the lateral constraint (L-3) and the declared-axis carve, alternated until neither lowers anything.
    Both only lower and both constraint sets are closed under pointwise maximum, so the alternation converges to
    the highest surface below the input that satisfies both; non-convergence within max_rounds is logged."""
    total_lip = 0
    total_axis = 0
    rounds = 0
    for rounds in range(1, max_rounds + 1):
        a = axis_carve(R, P, h)
        b = lipschitz_lower(R, P, h, kinds=kinds)["lip_lowered"]
        total_axis += a
        total_lip += b
        if a == 0 and (b == 0 or not R.axes):
            break
    return {"lip_lowered": total_lip, "axis_carved": total_axis, "s8_rounds": rounds,
            "s8_converged": rounds < max_rounds or (a == 0 and b == 0)}


def axis_carve(R, P, h):
    """Every declared axis falls toward its mouth: walking each read sequence (checks.axis_sequence) from source to
    mouth, a vertex not at least eps per metre below its predecessor is lowered to that (roots never change). Only
    lowers, only axis vertices. Returns the number of lowered vertices."""
    if not R.axes:
        return 0
    from .checks import axis_sequence
    root = R.root.reshape(-1)
    seqs = [axis_sequence(poly) for poly in R.axes]
    n = 0
    for _ in range(len(seqs) + 1):
        changed = 0
        for seq in seqs:
            prev = None
            for x, z in seq:
                i = z * R.n1 + x
                if prev is not None and i in h and prev in h and not root[i]:
                    d = R.cell * (DIAG if (prev % R.n1 != x and prev // R.n1 != z) else 1.0)
                    lim = h[prev] - P.eps * d
                    if h[i] > lim:
                        h[i] = lim
                        changed += 1
                prev = i
        n += changed
        if changed == 0:
            break
    return n


def carve(R, T, P, h):
    """Every tree edge falls by at least eps per metre toward the root: receivers are lowered below their donors,
    leaves first; roots are never changed. Only lowers; leaves no pit. Returns the number of lowered vertices."""
    recv, step, order = T["recv"], T["step"], T["order"]
    n = 0
    for i in reversed(order):
        r = recv[i]
        if r >= 0 and recv[r] >= 0 and i in h and r in h:
            lim = h[i] - P.eps * step[i]
            if h[r] > lim:
                h[r] = lim
                n += 1
    return n


def _crest_cone(R, P, crest_h, mine):
    """max over crest vertices q of c(q) - geodesic distance * flank, inside catchments, by a Dijkstra."""
    n0, n1, cell = R.n0, R.n1, R.cell
    best = {}
    heap = []
    for i, c in crest_h.items():
        heap.append((-c, i))
    heapq.heapify(heap)
    val = dict((i, c) for i, c in crest_h.items())
    offs = [(dz, dx, cell * (DIAG if dz and dx else 1.0)) for dz, dx in NB8]
    while heap:
        negv, i = heapq.heappop(heap)
        v = -negv
        if val.get(i, -math.inf) > v:
            continue
        z, x = divmod(i, n1)
        for dz, dx, L in offs:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                if not mine(j):
                    continue
                nv = v - L * P.flank
                if nv <= BASE:
                    continue
                if nv > val.get(j, -math.inf):
                    val[j] = nv
                    best[j] = nv
                    heapq.heappush(heap, (-nv, j))
    return best


# ----------------------------------------------------------------------------- S8: lateral constraint

def lipschitz_lower(R, P, h, units=None, kinds=None):
    """L-3: h(v) <- min over same-unit neighbours w (h(w) + d * lip), to the fixed point, by one Dijkstra on the lower
    envelope. Exempt vertices (declared escarpment and cliff bands) are neither lowered nor used as sources. Only
    lowers; a lowered vertex is left above the neighbour that bounds it, so no pit or closed depression is made, and
    no flow moves across a divide (same-unit pairs only)."""
    n0, n1, cell = R.n0, R.n1, R.cell
    unit = R.unit.reshape(-1)
    ex = R.exempt.reshape(-1)
    root = R.root.reshape(-1)
    sel = set(units) if units is not None else None
    if kinds is not None:
        ks = [u for u, d in enumerate(R.units) if d["kind"] in kinds]
        sel = set(ks) if sel is None else sel & set(ks)
    cur = {}
    heap = []
    for i, v in h.items():
        if unit[i] < 0 or ex[i] or (sel is not None and unit[i] not in sel):
            continue
        cur[i] = v
        heap.append((v, i))
    heapq.heapify(heap)
    offs = [(dz, dx, cell * (DIAG if dz and dx else 1.0)) for dz, dx in NB8]
    lowered = 0
    while heap:
        v, i = heapq.heappop(heap)
        if v > cur[i]:
            continue
        z, x = divmod(i, n1)
        ui = unit[i]
        for dz, dx, L in offs:
            zz, xx = z + dz, x + dx
            if 0 <= zz < n0 and 0 <= xx < n1:
                j = zz * n1 + xx
                if j in cur and unit[j] == ui and not root[j]:
                    nv = v + L * P.lip
                    if nv < cur[j]:
                        cur[j] = nv
                        heapq.heappush(heap, (nv, j))
    for i, v in cur.items():
        if v < h[i]:
            lowered += 1
            h[i] = v
    return {"lip_lowered": lowered}


# ----------------------------------------------------------------------------- orchestration

def synthesize(R, P, candidate, seed=0, target=None, T=None, ceiling=None, secondary=None, unit_extra=None,
               unit_order=None):
    """Run the pipeline for one candidate on raster R.

    candidate: 'C0', 'UF' or 'RF'. target: for C0 and UF, (x, z, height, radius_m) — bisection on the law's own
    heights (the highest open-ground catchment vertex within radius) scales kappa (C0) or the mountain-and-upland
    factor (UF) to height - crest_delta, +- bisect_tol, <= bisect_steps steps (L-4). secondary (UF only): further
    culminations (x, z, height, radius_m), each met in turn by its culmination factor. ceiling: an optional field the
    law's heights are capped by. Returns (lattice, log, tree)."""
    if T is None:
        T = build_tree(R, P, seed)
    A = areas(R, T)
    log = {"candidate": candidate, "unreached": len(T["unreached"])}

    def law(scale, culm=None, unit_mult=None, unit_culm=None):
        if candidate == "C0":
            h = law_c0(R, T, P, kappa=scale)
        elif candidate == "UF":
            h = law_uf(R, T, A, P, mult_mountain=scale, culm=culm, unit_extra=unit_extra)
        else:
            h = law_uf(R, T, A, P, unit_mult=unit_mult or {}, unit_culm=unit_culm or {}, unit_extra=unit_extra)
        if ceiling is not None:
            # min(law, ceiling) keeps every vertex above a lower neighbour (the ceiling falls away from its apex,
            # the law along its tree)
            cf = ceiling.reshape(-1)
            for i in list(h.keys()):
                if h[i] > cf[i]:
                    h[i] = float(cf[i])
        return h

    def probe(h, t, only=None):
        x, z, _, rad = t
        r = int(math.ceil(rad / R.cell))
        unit = R.unit
        best = -math.inf
        for zz in range(max(0, z - r), min(R.n0, z + r + 1)):
            for xx in range(max(0, x - r), min(R.n1, x + r + 1)):
                if math.hypot(xx - x, zz - z) * R.cell <= rad and unit[zz, xx] >= 0 and R.zone[zz, xx] == Z_NONE \
                        and (only is None or unit[zz, xx] == only):
                    j = zz * R.n1 + xx
                    if j in h:
                        best = max(best, h[j])
        return best

    def bisect(fn, want, s0, lo=0.0):
        hi = None
        s = s0
        steps = []
        for k in range(P.bisect_steps):
            got = fn(s)
            steps.append((s, got))
            if abs(got - want) <= P.bisect_tol:
                break
            if got < want:
                lo = s
                s = s * 2.0 if hi is None else 0.5 * (lo + hi)
            else:
                hi = s
                s = 0.5 * (lo + hi)
        best = min(steps, key=lambda t: (abs(t[1] - want), t[0]))
        return best[0], steps, abs(best[1] - want) <= P.bisect_tol

    # L-4: the law is monotone in each scale, so bisection on the law's own heights is valid; the contract steps
    # follow once. RF meets planned crests in closed form.
    scale = 1.0
    culm = []
    if candidate in ("C0", "UF") and target is not None:
        want = target[2] - P.crest_delta
        scale, steps, met = bisect(lambda s_: probe(law(s_), target), want, P.kappa if candidate == "C0" else 1.0)
        log["bisection"], log["bisection_target"], log["bisection_met"] = steps, want, met
        if candidate == "UF" and secondary:
            log["secondary"] = []
            for x, z, H, rad, frad in secondary:
                want2 = H - P.crest_delta
                fn = lambda a_, x=x, z=z, rad=rad, frad=frad: probe(law(scale, culm + [(x, z, max(a_, 0.05) - 1.0, frad)]), (x, z, H, rad))
                a, steps2, met2 = bisect(fn, want2, 1.0)
                a = max(a, 0.05)
                culm.append((x, z, a - 1.0, frad))
                log["secondary"].append({"at": [x, z], "target": want2, "factor": a, "steps": steps2, "met": met2})
    unit_mult, unit_culm = {}, {}
    if candidate == "RF" and secondary:
        # GF-RF: each catchment alone - its scale meets its highest adjacent plan node, culmination factors its other
        # adjacent nodes; nothing reads another unit (A1)
        log["units"] = {}
        for ui in (unit_order if unit_order is not None else range(len(R.units))):
            ud = R.units[ui]
            if ud["kind"] != CATCHMENT:
                continue
            near_u = dilate_mask(R.unit == ui, 4)
            nodes = sorted([nd for nd in secondary if near_u[nd[1], nd[0]]], key=lambda t: -t[2])
            if not nodes:
                unit_mult[ui] = 1.0
                continue
            x, z, H, rad, frad = nodes[0]
            m, st, met = bisect(lambda s_, ui=ui, x=x, z=z, H=H, rad=rad: probe(
                law(1.0, unit_mult={**unit_mult, ui: s_}, unit_culm=unit_culm), (x, z, H, rad), only=ui), H - P.crest_delta, 1.0)
            unit_mult[ui] = m
            unit_culm[ui] = []
            rec = [{"at": [x, z], "target": H - P.crest_delta, "scale": m, "met": met}]
            for x, z, H, rad, frad in nodes[1:]:
                fn = lambda a_, x=x, z=z, H=H, rad=rad, frad=frad, ui=ui: probe(
                    law(1.0, unit_mult=unit_mult, unit_culm={**unit_culm, ui: unit_culm[ui] + [(x, z, max(a_, 0.05) - 1.0, frad)]}),
                    (x, z, H, rad), only=ui)
                a, st2, met2 = bisect(fn, H - P.crest_delta, 1.0)
                a = max(a, 0.05)
                unit_culm[ui].append((x, z, a - 1.0, frad))
                rec.append({"at": [x, z], "target": H - P.crest_delta, "factor": a, "met": met2})
            log["units"][ud["name"]] = rec
    h = law(scale, culm, unit_mult, unit_culm)
    mode = "plan" if candidate == "RF" else "emergent"
    steps = {"plateaus": plateau_sublaw(R, T, P, h)}
    # catchments first: divide and sector crests, flank fill, planned ceiling, lateral constraint
    crest_h, planned = provisional_crests(R, P, h, mode)
    inner = {i: v for i, v in crest_h.items() if R.crests[R.crest.flat[i]]["kind"] != FRINGE_CREST}
    steps.update(flank_fill(R, T, P, h, inner, planned & set(inner)))
    steps.update(planned_ceiling(R, T, P, h, inner, planned & set(inner)))
    steps.update(lateral_and_axes(R, P, h, (CATCHMENT,)))
    # then the coast: fringe crests from the final catchments, the fringe profile, its lateral constraint
    crest_h, planned = provisional_crests(R, P, h, mode)
    fringe_sublaw(R, T, P, h, crest_h)
    f = planned_ceiling(R, T, P, h, crest_h, planned)
    steps["fringe_ceiling_lowered"] = f["ceiling_lowered"]
    steps["fringe_lip_lowered"] = lipschitz_lower(R, P, h, kinds=(FRINGE,))["lip_lowered"]
    crest_f = final_crests(R, P, h, crest_h, planned)
    log["scale"] = scale
    log["culmination_factors"] = culm
    log.update(steps)
    lat, wlog = assemble(R, h, crest_h=crest_f)
    log["write"] = wlog
    return lat, log, T


def assemble(R, h, crest_h):
    """S9: write each owner's vertices once into the lattice; fixed vertices (context, sea) are copied, not
    written. Returns the lattice and the write log (count and writer per vertex)."""
    n = R.n0 * R.n1
    lat = R.fixed.reshape(-1).copy()
    count = np.zeros(n, np.int32)
    writer = np.full(n, -1, np.int64)
    owner = R.owner_codes().reshape(-1)
    for i, v in h.items():
        if owner[i] >= 0:
            lat[i] = v
            count[i] += 1
            writer[i] = owner[i]
    for i, v in crest_h.items():
        lat[i] = v
        count[i] += 1
        writer[i] = owner[i]
    return lat.reshape(R.n0, R.n1), {"count": count, "writer": writer}


# ----------------------------------------------------------------------------- assembly test helpers (GF-A)

def restrict(R, units):
    """A copy of R in which only the given units own vertices; every other unit's vertices become unowned (their
    heights are absent). Crest records, plateaus and axes are shared."""
    import copy
    Q = copy.copy(R)
    keep = np.isin(R.unit, list(units))
    Q.unit = np.where(keep, R.unit, -1).astype(np.int32)
    Q.root = R.root & keep
    return Q


def synthesize_per_unit(R, P, candidate, order, seed=0, T=None):
    """GF-RF assembly: each unit synthesised alone (restrict) in the given order; each writes only its own vertices
    into the lattice; crest vertices take the value of the last run that wrote them (planned crests are identical
    in every run). Returns the lattice and per-unit logs."""
    if T is None:
        T = build_tree(R, P, seed)
    lat = R.fixed.copy()
    logs = {}
    for u in order:
        Q = restrict(R, [u])
        l_u, log_u, _ = synthesize(Q, P, candidate, seed=seed, T=T)
        m = R.unit == u
        lat[m] = l_u[m]
        cm = R.crest >= 0
        lat[cm] = l_u[cm]
        logs[u] = {k: v for k, v in log_u.items() if k != "write"}
    return lat, logs
