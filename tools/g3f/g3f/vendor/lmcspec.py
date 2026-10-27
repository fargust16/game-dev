"""Read-only verification of the LMC technical specification on historical G3c data.

Reads G3c evaluation lattices, region maps and the G3c report; writes nothing outside this folder.
numpy only. Definitions follow GEOGRAPHY_LMC_TECHNICAL_SPEC.md (§§1, 2, 7, 8).
"""
import heapq
import math
import os
import re
from collections import deque

import numpy as np

N = 1025
C = 2.734375
SEA = 24.0
EVAL = os.path.join(os.path.dirname(__file__), "..", "g3c", "eval")


# ----------------------------------------------------------------------------- data

def load(contour, seed, folder=EVAL, cfg="s30-med"):
    tag = f"g3c_{contour}_{cfg}_{seed}"
    h = np.fromfile(os.path.join(folder, tag + ".f32"), dtype="<f4").reshape(N, N).astype(np.float64)
    reg = np.fromfile(os.path.join(folder, tag + "_region.u8"), dtype=np.uint8).reshape(N, N)
    return h, reg


def parse_report(path=os.path.join(EVAL, "g3c_report.txt")):
    out = {}
    rx_tag = re.compile(r"^\[([AB])_s30-med_(\d+)\] (.*)$")
    for line in open(path, encoding="utf-8"):
        m = rx_tag.match(line.rstrip("\n"))
        if not m:
            continue
        key = (m.group(1), int(m.group(2)))
        d = out.setdefault(key, {"sites": [], "plateaus": [], "summits": [], "hyd_fail": None, "read": {}})
        body = m.group(3)
        s = re.match(r"site r=(\d+) m at \((\d+), (\d+)\) coastal=(\w+) region=(-?\d+)", body)
        if s:
            d["sites"].append(dict(r=int(s.group(1)), x=float(s.group(2)), z=float(s.group(3)),
                                   coastal=s.group(4) == "True", region=int(s.group(5))))
            continue
        p = re.match(r"plateau (\d+): class (\w+) height (\d+) m, top ([\d.]+) km2.*escarpment rim (\d+)%.*max steep-run drop (\d+) m", body)
        if p:
            d["plateaus"].append(dict(idx=int(p.group(1)), cls=p.group(2), height=float(p.group(3)),
                                      top_km2=float(p.group(4)), esc_rim=float(p.group(5)), drop=float(p.group(6))))
            continue
        u = re.match(r"highland summit \((\d+), (\d+)\) (\d+) m, prominence (\d+) m", body)
        if u:
            d["summits"].append(dict(x=float(u.group(1)), z=float(u.group(2)), h=float(u.group(3)), prom=float(u.group(4))))
            continue
        if body.startswith("hydrology planner failed"):
            d["hyd_fail"] = body
            continue
        if body.startswith("R1="):
            for kv in body.split():
                if "=" in kv:
                    k, v = kv.split("=", 1)
                    d["read"][k] = v
            c = re.search(r"coast low/rocky/cliff=(\d+)/(\d+)/(\d+)", body)
            if c:
                d["read"]["coast"] = tuple(int(c.group(i)) for i in (1, 2, 3))
    return out


# ----------------------------------------------------------------------------- basic fields

def ocean_mask(h):
    sea = h <= SEA
    ocean = np.zeros_like(sea)
    q = deque()
    for z in range(N):
        for x in (0, N - 1):
            if sea[z, x] and not ocean[z, x]:
                ocean[z, x] = True
                q.append((z, x))
    for x in range(N):
        for z in (0, N - 1):
            if sea[z, x] and not ocean[z, x]:
                ocean[z, x] = True
                q.append((z, x))
    while q:
        z, x = q.popleft()
        for dz, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            zz, xx = z + dz, x + dx
            if 0 <= zz < N and 0 <= xx < N and sea[zz, xx] and not ocean[zz, xx]:
                ocean[zz, xx] = True
                q.append((zz, xx))
    return ocean


def shore_vertices(h, ocean):
    land = h > SEA
    adj = np.zeros_like(land)
    adj[1:, :] |= ocean[:-1, :]
    adj[:-1, :] |= ocean[1:, :]
    adj[:, 1:] |= ocean[:, :-1]
    adj[:, :-1] |= ocean[:, 1:]
    return land & adj


def vertex_slope_deg(h, cell=C):
    gz, gx = np.gradient(h, cell)
    return np.degrees(np.arctan(np.hypot(gx, gz))), gx, gz


def triangle_slopes(h, cell=C):
    h00 = h[:-1, :-1]; h10 = h[:-1, 1:]; h01 = h[1:, :-1]; h11 = h[1:, 1:]
    # T1 (00,10,11): gx=(h10-h00), gz=(h11-h10); T2 (00,11,01): gx=(h11-h01), gz=(h01-h00)
    s1 = np.hypot((h10 - h00) / cell, (h11 - h10) / cell)
    s2 = np.hypot((h11 - h01) / cell, (h01 - h00) / cell)
    c1 = (h00 + h10 + h11) / 3.0
    c2 = (h00 + h11 + h01) / 3.0
    return s1, s2, c1, c2


# ----------------------------------------------------------------------------- T-edge graph

class TEdge:
    """Walkable land triangles (slope < 45 deg, centroid > 24 m) joined across shared edges."""

    def __init__(self, h, cell=C):
        s1, s2, c1, c2 = triangle_slopes(h, cell)
        q = N - 1
        self.q = q
        land = np.empty((q, q, 2), dtype=bool)
        land[..., 0] = c1 > SEA
        land[..., 1] = c2 > SEA
        walk = land.copy()
        walk[..., 0] &= s1 < 1.0
        walk[..., 1] &= s2 < 1.0
        self.land = land.reshape(-1)
        self.walk = walk.reshape(-1)
        self.slope = np.degrees(np.arctan(np.stack([s1, s2], axis=-1).reshape(-1)))
        self.label = None
        self.shore_reach = None

    def neighbours(self, t):
        q = self.q
        quad = t >> 1
        x = quad % q
        z = quad // q
        if (t & 1) == 0:
            return (t + 1,
                    2 * ((z - 1) * q + x) + 1 if z > 0 else -1,
                    2 * (z * q + x + 1) + 1 if x < q - 1 else -1)
        return (t - 1,
                2 * (z * q + x - 1) if x > 0 else -1,
                2 * ((z + 1) * q + x) if z < q - 1 else -1)

    def components(self):
        walk = self.walk
        land = self.land
        label = np.full(walk.size, -1, dtype=np.int32)
        shore = np.zeros(walk.size, dtype=bool)
        lab = label.tolist() if False else None  # keep numpy; loops below use python ints
        label_l = [-1] * walk.size
        walk_l = walk.tolist()
        land_l = land.tolist()
        comp = 0
        comp_shore = []
        nb = self.neighbours
        for t0 in np.flatnonzero(walk).tolist():
            if label_l[t0] != -1:
                continue
            label_l[t0] = comp
            touches = False
            dq = deque([t0])
            while dq:
                t = dq.popleft()
                for u in nb(t):
                    if u < 0 or not land_l[u]:
                        touches = True
                        continue
                    if walk_l[u] and label_l[u] == -1:
                        label_l[u] = comp
                        dq.append(u)
            comp_shore.append(touches)
            comp += 1
        self.label = np.array(label_l, dtype=np.int32)
        self.comp_shore = np.array(comp_shore, dtype=bool)
        lab = self.label
        self.shore_reach = (lab >= 0) & self.comp_shore[np.maximum(lab, 0)]
        return comp

    def vertex_triangles(self, x, z):
        q = self.q
        out = []
        for dz in (-1, 0):
            for dx in (-1, 0):
                qx, qz = x + dx, z + dz
                if 0 <= qx < q and 0 <= qz < q:
                    t = 2 * (qz * q + qx)
                    out.extend((t, t + 1))
        return out

    def vertex_component(self, x, z):
        best = None
        for t in self.vertex_triangles(x, z):
            if self.label[t] >= 0:
                if self.shore_reach[t]:
                    return int(self.label[t]), True
                best = int(self.label[t])
        return best, False

    def centroid(self, t):
        q = self.q
        quad = t >> 1
        x = quad % q
        z = quad // q
        if (t & 1) == 0:
            return ((x + 2 / 3) * C, (z + 1 / 3) * C)
        return ((x + 1 / 3) * C, (z + 2 / 3) * C)

    def dijkstra(self, source, targets, banned=None):
        dist = {source: 0.0}
        prev = {}
        heap = [(0.0, source)]
        want = set(targets)
        found = {}
        walk = self.walk
        while heap and want:
            d, t = heapq.heappop(heap)
            if d > dist.get(t, math.inf):
                continue
            if t in want:
                found[t] = d
                want.discard(t)
            cx, cz = self.centroid(t)
            for u in self.neighbours(t):
                if u < 0 or not walk[u] or (banned is not None and banned[u]):
                    continue
                ux, uz = self.centroid(u)
                nd = d + math.hypot(ux - cx, uz - cz)
                if nd < dist.get(u, math.inf):
                    dist[u] = nd
                    prev[u] = t
                    heapq.heappush(heap, (nd, u))
        return found, prev


# ----------------------------------------------------------------------------- drainage

def flood_d8(h, ocean, cell=C, eps=1e-3):
    """Priority-flood (epsilon 1 mm, 8-connected) to the ocean, then D8 steepest descent on the filled
    surface; land next to the ocean drains into it (receiver -1). Area accumulated from the top down.
    Returns filled heights, receivers (flat index or -1), drainage area (m^2) and the order (low to high)."""
    n = N
    hh = h.reshape(-1).tolist()
    oc = ocean.reshape(-1).tolist()
    done = list(oc)
    heap = []
    for i in range(n * n):
        if oc[i]:
            continue
        z, x = divmod(i, n)
        seed = x == 0 or z == 0 or x == n - 1 or z == n - 1
        if not seed:
            for j in (i - 1, i + 1, i - n, i + n, i - n - 1, i - n + 1, i + n - 1, i + n + 1):
                if oc[j]:
                    seed = True
                    break
        if seed:
            heap.append((hh[i], i))
            done[i] = True
    heapq.heapify(heap)
    while heap:
        hz, i = heapq.heappop(heap)
        z, x = divmod(i, n)
        for dz in (-1, 0, 1):
            zz = z + dz
            if zz < 0 or zz >= n:
                continue
            for dx in (-1, 0, 1):
                if dz == 0 and dx == 0:
                    continue
                xx = x + dx
                if xx < 0 or xx >= n:
                    continue
                j = zz * n + xx
                if done[j]:
                    continue
                done[j] = True
                v = hh[j]
                if v < hz + eps:
                    v = hz + eps
                    hh[j] = v
                heapq.heappush(heap, (v, j))
    filled = np.array(hh).reshape(n, n)
    land = ~ocean
    best = np.zeros((n, n))
    recv2 = np.full((n, n), -1, dtype=np.int64)
    idx = np.arange(n * n).reshape(n, n)
    for dz in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dz == 0 and dx == 0:
                continue
            d = cell * math.hypot(dz, dx)
            nb = np.full((n, n), np.inf)
            nbi = np.full((n, n), -1, dtype=np.int64)
            zs = slice(max(0, dz), n + min(0, dz)); zd = slice(max(0, -dz), n + min(0, -dz))
            xs = slice(max(0, dx), n + min(0, dx)); xd = slice(max(0, -dx), n + min(0, -dx))
            nb[zd, xd] = filled[zs, xs]
            nbi[zd, xd] = idx[zs, xs]
            nb_ocean = np.zeros((n, n), dtype=bool)
            nb_ocean[zd, xd] = ocean[zs, xs]
            drop = (filled - nb) / d
            drop = np.where(nb_ocean, np.inf, drop)  # a sea neighbour is the outlet
            take = land & (drop > best)
            best = np.where(take, drop, best)
            recv2 = np.where(take, np.where(nb_ocean, -1, nbi), recv2)
    recv = recv2.reshape(-1)
    order = np.argsort(filled.reshape(-1), kind="stable")
    order = order[land.reshape(-1)[order]]
    area = np.full(n * n, cell * cell)
    area[ocean.reshape(-1)] = 0.0
    rl = recv.tolist(); al = area.tolist()
    for i in reversed(order.tolist()):
        r = rl[i]
        if r >= 0:
            al[r] += al[i]
    return filled, recv, np.array(al).reshape(n, n), order.tolist()


# ----------------------------------------------------------------------------- Q metrics

def q1_planar(h, mask_slope, half=7):
    """Share of masked vertices whose (2half+1)^2 window fits a plane with RMS < 0.05 m."""
    k = 2 * half + 1
    zz, xx = np.mgrid[0:N, 0:N].astype(np.float64)

    def box(a):
        s = np.zeros((N + 1, N + 1))
        s[1:, 1:] = a.cumsum(0).cumsum(1)
        out = np.full((N, N), np.nan)
        out[half:N - half, half:N - half] = (s[k:, k:] - s[:-k, k:] - s[k:, :-k] + s[:-k, :-k])
        return out

    hm = h - np.nanmean(h)
    S = box(hm); S2 = box(hm * hm); SX = box(xx * hm); SZ = box(zz * hm)
    n = k * k
    su2 = k * sum(u * u for u in range(-half, half + 1))
    a = S / n
    b = (SX - xx * S) / su2
    c = (SZ - zz * S) / su2
    ss = S2 - n * a * a - b * b * su2 - c * c * su2
    rms = np.sqrt(np.maximum(ss, 0) / n)
    valid = mask_slope & np.isfinite(rms)
    return valid, rms < 0.05


def q3_crest(h, region_mask, cell=C):
    dirs = [((0, 1), 1.0), ((1, 0), 1.0), ((1, 1), math.sqrt(2)), ((1, -1), math.sqrt(2))]
    best_w = np.full((N, N), np.nan)
    best_k = np.full((N, N), np.nan)
    crest_any = np.zeros((N, N), dtype=bool)

    def shift(a, dz, dx, k):
        out = np.full_like(a, np.nan)
        sz, sx = dz * k, dx * k
        zs = slice(max(0, sz), N + min(0, sz)); zd = slice(max(0, -sz), N + min(0, -sz))
        xs = slice(max(0, sx), N + min(0, sx)); xd = slice(max(0, -sx), N + min(0, -sx))
        out[zd, xd] = a[zs, xs]
        return out

    for (dz, dx), f in dirs:
        step = cell * f
        k30 = int(round(30.0 / step))
        p1 = shift(h, dz, dx, 1); m1 = shift(h, -dz, -dx, 1)
        pk = shift(h, dz, dx, k30); mk = shift(h, -dz, -dx, k30)
        crest = (h >= p1) & (h >= m1) & (h > np.fmax(pk, mk)) & region_mask
        crest &= np.isfinite(pk) & np.isfinite(mk)
        kappa = (pk + mk - 2 * h) / (k30 * step) ** 2
        width = np.ones((N, N))
        for sgn in (1, -1):
            alive = np.ones((N, N), dtype=bool)
            for s in range(1, 111):
                hs = shift(h, sgn * dz, sgn * dx, s)
                alive &= np.nan_to_num(hs, nan=-1e9) >= h - 5.0
                if not alive.any():
                    break
                width += alive
        width *= step
        upd = crest & (~crest_any | (width < np.nan_to_num(best_w, nan=np.inf)))
        best_w[upd] = width[upd]
        best_k[upd] = kappa[upd]
        crest_any |= crest
    return crest_any, best_w, best_k


def q5_radial(h, cx, cz, cell=C, r0=100.0, r1=400.0):
    zz, xx = np.mgrid[0:N, 0:N]
    px = xx * cell - cx; pz = zz * cell - cz
    r = np.hypot(px, pz)
    gz, gx = np.gradient(h, cell)
    g = np.hypot(gx, gz)
    ring = (r >= r0) & (r <= r1) & (h > SEA) & (g > 1e-6)
    cosv = (-(gx * px + gz * pz)) / (np.maximum(g, 1e-12) * np.maximum(r, 1e-9))
    w = g[ring]
    return float((w * cosv[ring]).sum() / w.sum()) if w.size else float("nan")


def disc_offsets(radius_cells):
    r = int(math.floor(radius_cells))
    out = []
    for dz in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dz * dz + dx * dx <= radius_cells * radius_cells:
                out.append((dz, dx))
    return out


def q7_coast(h, ocean, cell=C):
    shore = shore_vertices(h, ocean)
    land_h = np.where(h > SEA, h, -np.inf)
    rad = 60.0 / cell
    rr = int(math.floor(rad))
    zz, xx = np.mgrid[-rr:rr + 1, -rr:rr + 1]
    disc = zz * zz + xx * xx <= rad * rad
    cls = {}
    pts = np.argwhere(shore)
    for z, x in pts:
        z0, z1 = max(0, z - rr), min(N, z + rr + 1)
        x0, x1 = max(0, x - rr), min(N, x + rr + 1)
        win = land_h[z0:z1, x0:x1]
        dm = disc[(z0 - z + rr):(z1 - z + rr), (x0 - x + rr):(x1 - x + rr)]
        rise = win[dm].max() - SEA
        cls[(int(z), int(x))] = 0 if rise < 4 else (1 if rise < 15 else 2)
    total = len(cls)
    shares = [sum(1 for v in cls.values() if v == k) / total * 100 for k in range(3)]
    # runs: 8-connected components of same-class shore vertices
    seen = set()
    runs = []
    for p, k in cls.items():
        if p in seen:
            continue
        seen.add(p)
        dq = deque([p]); size = 0
        while dq:
            z, x = dq.popleft(); size += 1
            for dz in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    q = (z + dz, x + dx)
                    if q in cls and q not in seen and cls[q] == k:
                        seen.add(q); dq.append(q)
        runs.append((k, size))
    cliff_total = sum(s for k, s in runs if k == 2)
    longest_cliff = max([s for k, s in runs if k == 2], default=0)
    length_km = total * cell / 1000.0
    return dict(shares=shares, total=total, longest_cliff_of_cliff=longest_cliff / max(1, cliff_total),
                longest_cliff_of_shore=longest_cliff / total, runs=len(runs), changes_per_km=len(runs) / length_km)


def components8(mask):
    lab = np.full(mask.shape, -1, dtype=np.int32)
    comps = []
    for z, x in np.argwhere(mask):
        if lab[z, x] >= 0:
            continue
        cid = len(comps); cells = []
        lab[z, x] = cid
        dq = deque([(z, x)])
        while dq:
            a, b = dq.popleft(); cells.append((a, b))
            for dz in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    zz, xx = a + dz, b + dx
                    if 0 <= zz < N and 0 <= xx < N and mask[zz, xx] and lab[zz, xx] < 0:
                        lab[zz, xx] = cid; dq.append((zz, xx))
        comps.append(cells)
    return lab, comps


def plateau_edges(h, reg, tri_max_slope_v, recv=None, area=None, cell=C):
    """Per plateau: edge height (spec §2.4), joined / escarpment shares, edge-drop CV, re-entrants (Q6)."""
    plat = (reg == 4) | (reg == 5) | (reg == 6)
    lab, comps = components8(plat)
    top = reg == 4
    land = h > SEA
    nontop_land_h = np.where(land & ~top, h, np.inf)
    r90 = 90.0 / cell; r15 = 15.0 / cell
    o90 = int(math.floor(r90)); o15 = int(math.floor(r15))
    g90z, g90x = np.mgrid[-o90:o90 + 1, -o90:o90 + 1]; d90 = g90z ** 2 + g90x ** 2 <= r90 ** 2
    g15z, g15x = np.mgrid[-o15:o15 + 1, -o15:o15 + 1]; d15 = g15z ** 2 + g15x ** 2 <= r15 ** 2
    steep_nontop = np.where(~top, tri_max_slope_v, 0.0)
    out = []
    for cid, cells in enumerate(comps):
        cz = np.array([c[0] for c in cells]); cx = np.array([c[1] for c in cells])
        tmask = np.zeros((N, N), dtype=bool)
        sel = top[cz, cx]
        tmask[cz[sel], cx[sel]] = True
        if tmask.sum() == 0:
            continue
        nb = np.zeros_like(tmask)
        for dz in (-1, 0, 1):
            for dx in (-1, 0, 1):
                nb |= np.roll(np.roll(~tmask, dz, 0), dx, 1)
        rim = tmask & nb
        drops = []; esc = 0; joined = 0
        for z, x in np.argwhere(rim):
            z0, z1 = max(0, z - o90), min(N, z + o90 + 1); x0, x1 = max(0, x - o90), min(N, x + o90 + 1)
            w = nontop_land_h[z0:z1, x0:x1][d90[(z0 - z + o90):(z1 - z + o90), (x0 - x + o90):(x1 - x + o90)]]
            m = w.min() if w.size else np.inf
            drop = h[z, x] - m if np.isfinite(m) else 0.0
            z0, z1 = max(0, z - o15), min(N, z + o15 + 1); x0, x1 = max(0, x - o15), min(N, x + o15 + 1)
            s = steep_nontop[z0:z1, x0:x1][d15[(z0 - z + o15):(z1 - z + o15), (x0 - x + o15):(x1 - x + o15)]]
            steep = s.size and s.max() >= 45.0
            if drop >= 10.0 and steep:
                esc += 1; drops.append(drop)
            elif drop < 10.0:
                joined += 1
        nrim = int(rim.sum())
        reentrants = None
        if recv is not None:
            exits = np.zeros((N, N), dtype=bool)
            for z, x in np.argwhere(tmask):
                i = z * N + x
                r = recv[i]
                if r >= 0 and area[z, x] >= 30000.0:
                    rz, rx = divmod(int(r), N)
                    if not tmask[rz, rx]:
                        exits[z, x] = True
            reentrants = len(components8(exits)[1]) if exits.any() else 0
        out.append(dict(top_km2=tmask.sum() * cell * cell / 1e6, rim=nrim,
                        edge_height=float(np.median(drops)) if drops else float("nan"),
                        esc_share=esc / max(1, nrim), joined_share=joined / max(1, nrim),
                        cv=float(np.std(drops) / np.mean(drops)) if len(drops) > 1 else float("nan"),
                        reentrants=reentrants))
    return out


def vertex_max_triangle_slope(h, cell=C):
    s1, s2, _, _ = triangle_slopes(h, cell)
    t = np.degrees(np.arctan(np.maximum(s1, s2)))
    v = np.zeros((N, N))
    v[:-1, :-1] = np.maximum(v[:-1, :-1], t)
    v[:-1, 1:] = np.maximum(v[:-1, 1:], t)
    v[1:, :-1] = np.maximum(v[1:, :-1], t)
    v[1:, 1:] = np.maximum(v[1:, 1:], t)
    return v


def q2_theta(h, recv, area, order, cell=C, min_nodes=10, a_ch=30000.0):
    hf = h.reshape(-1); af = area.reshape(-1)
    # system = ocean termination reached by following receivers
    sysid = np.full(N * N, -1, dtype=np.int64)
    for i in order:  # order is from outlets upward: receivers come first
        r = recv[i]
        sysid[i] = i if r < 0 else sysid[r]
    thetas = []
    ch = np.flatnonzero((af >= a_ch) & (recv >= 0))
    bysys = {}
    for i in ch.tolist():
        r = int(recv[i])
        zi, xi = divmod(i, N); zr, xr = divmod(r, N)
        d = cell * math.hypot(zi - zr, xi - xr)
        s = (hf[i] - hf[r]) / d
        if s > 0:
            bysys.setdefault(int(sysid[i]), []).append((math.log(af[i]), math.log(s)))
    for k, pts in bysys.items():
        if len(pts) >= min_nodes:
            a = np.array(pts)
            if np.ptp(a[:, 0]) < 1e-6:
                continue
            slope = np.polyfit(a[:, 0], a[:, 1], 1)[0]
            thetas.append(-slope)
    return float(np.median(thetas)) if thetas else float("nan"), len(thetas)
