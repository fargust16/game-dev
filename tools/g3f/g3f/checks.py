"""G3f invariant checks on a synthesised lattice (spec §8): pits, D8 flow across divides, outlets, triangle slopes,
axis climbs, the write log. Pure numpy/stdlib; used by the analytic checks and by the sector readings."""
import math

import numpy as np

from .lattice import NB8, DIAG, C


def d8(h, land, cell=C):
    """Steepest-descent receiver among 8 neighbours on the unfilled surface (slope = drop / distance, strictly
    positive); ties go to the lower flat index. -1 where no neighbour is lower. Non-land neighbours count."""
    n0, n1 = h.shape
    best = np.zeros((n0, n1))
    recv = np.full((n0, n1), -1, np.int64)
    idx = np.arange(n0 * n1).reshape(n0, n1)
    # visit neighbours in ascending flat-index offset so '>' keeps the lower index on ties
    order = sorted(NB8, key=lambda o: o[0] * n1 + o[1])
    for dz, dx in order:
        d = cell * (DIAG if dz and dx else 1.0)
        nb = np.full((n0, n1), np.inf)
        nbi = np.full((n0, n1), -1, np.int64)
        zs = slice(max(0, dz), n0 + min(0, dz)); zd = slice(max(0, -dz), n0 + min(0, -dz))
        xs = slice(max(0, dx), n1 + min(0, dx)); xd = slice(max(0, -dx), n1 + min(0, -dx))
        nb[zd, xd] = h[zs, xs]
        nbi[zd, xd] = idx[zs, xs]
        drop = (h - nb) / d
        take = land & (drop > best)
        best = np.where(take, drop, best)
        recv = np.where(take, nbi, recv)
    return recv.reshape(-1)


def pits(h, land, cell=C):
    """Land vertices with no strictly lower 8-neighbour."""
    r = d8(h, land, cell)
    return np.flatnonzero(land.reshape(-1) & (r < 0))


def flow_across(h, R, cell=C):
    """GF-B: unit vertices (not crest, not root) whose D8 receiver is a vertex of another unit or a crest vertex.
    Returns (crossings, onto_crest) as lists of (vertex, receiver)."""
    land = R.land
    r = d8(h, land, cell)
    unit = R.unit.reshape(-1)
    crest = R.crest.reshape(-1)
    root = R.root.reshape(-1)
    cross, onto = [], []
    for i in np.flatnonzero((unit >= 0) & ~root).tolist():
        j = r[i]
        if j < 0:
            continue
        if crest[j] >= 0:
            onto.append((i, int(j)))
        elif unit[j] >= 0 and unit[j] != unit[i]:
            cross.append((i, int(j)))
    return cross, onto


def trace_ends(h, R, cell=C, max_steps=10 ** 6):
    """Follow D8 from every unit vertex; report per unit the set of end kinds: 'own-root', 'other-root', 'sea', 'pit',
    'crest'. A unit vertex adjacent to the sea ends at the sea only if it is not a root."""
    land = R.land
    r = d8(h, land, cell)
    unit = R.unit.reshape(-1)
    root = R.root.reshape(-1)
    crest = R.crest.reshape(-1)
    lm = land.reshape(-1)
    n = r.size
    end = np.full(n, -1, np.int64)
    out = {}
    for i0 in np.flatnonzero(unit >= 0).tolist():
        path = []
        i = i0
        steps = 0
        while end[i] < 0 and not root[i] and r[i] >= 0 and lm[r[i]] and steps < max_steps:
            path.append(i)
            i = r[i]
            steps += 1
        e = end[i] if end[i] >= 0 else i
        for p in path:
            end[p] = e
        end[i0] = e
        u = unit[i0]
        if root[e]:
            kind = "own-root" if unit[e] == u else "other-root"
        elif crest[e] >= 0:
            kind = "crest"
        elif r[e] >= 0 and not lm[r[e]]:
            kind = "sea"
        else:
            kind = "pit"
        out.setdefault(int(u), {}).setdefault(kind, 0)
        out[int(u)][kind] += 1
    return out


def triangle_slopes_deg(h, cell=C):
    """Slopes of T1 = (00, 10, 11) and T2 = (00, 11, 01) per quad, degrees (G3e §1)."""
    h00 = h[:-1, :-1]; h10 = h[:-1, 1:]; h01 = h[1:, :-1]; h11 = h[1:, 1:]
    s1 = np.hypot((h10 - h00) / cell, (h11 - h10) / cell)
    s2 = np.hypot((h11 - h01) / cell, (h01 - h00) / cell)
    return np.degrees(np.arctan(s1)), np.degrees(np.arctan(s2))


def steep_triangles_outside(h, mask_ok, exempt, cell=C, deg=45.0):
    """Triangles with slope >= deg all of whose vertices are in mask_ok and none exempt."""
    s1, s2 = triangle_slopes_deg(h, cell)
    m = mask_ok & ~exempt
    q00 = m[:-1, :-1]; q10 = m[:-1, 1:]; q01 = m[1:, :-1]; q11 = m[1:, 1:]
    t1 = q00 & q10 & q11
    t2 = q00 & q11 & q01
    return int(((s1 >= deg) & t1).sum()), int(((s2 >= deg) & t2).sum()), int(t1.sum() + t2.sum())


def axis_sequence(poly_vertex):
    """The vertices a declared line is read at (phase plan §9.1, R7 (c) as a line): the polyline (vertex
    coordinates, source first, mouth last) resampled every half cell, each sample's nearest vertex (half away from
    zero), consecutive repeats dropped. Returns a list of (x, z)."""
    pts = []
    for a, b in zip(poly_vertex[:-1], poly_vertex[1:]):
        L = math.hypot(b[0] - a[0], b[1] - a[1])
        k = max(1, int(math.ceil(L / 0.5)))
        for t in range(k):
            pts.append((a[0] + (b[0] - a[0]) * t / k, a[1] + (b[1] - a[1]) * t / k))
    pts.append(tuple(poly_vertex[-1]))
    out = []
    for x, z in pts:
        v = (int(math.floor(x + 0.5)), int(math.floor(z + 0.5)))
        if not out or v != out[-1]:
            out.append(v)
    return out


def axis_climb(h, poly_vertex, cell=C):
    """R7 (c) climb on a declared line (G3e spec §7.1): over the vertices of axis_sequence, source -> mouth,
    climb = max_k [h(n_k) - min_{j<k} h(n_j)]."""
    climb = 0.0
    run_min = math.inf
    for x, z in axis_sequence(poly_vertex):
        v = h[z, x]
        if v - run_min > climb:
            climb = v - run_min
        run_min = min(run_min, v)
    return climb


def write_log_ok(R, wlog):
    """GF-O on a raster: every land vertex of the domain with an owner written exactly once by that owner; nothing
    else written."""
    owner = R.owner_codes().reshape(-1)
    count = wlog["count"]
    writer = wlog["writer"]
    owned = owner != -1
    bad_count = int(((count != 1) & owned).sum())
    bad_writer = int(((writer != owner) & owned).sum())
    stray = int(((count > 0) & ~owned).sum())
    return {"owned": int(owned.sum()), "bad_count": bad_count, "bad_writer": bad_writer, "stray": stray,
            "ok": bad_count == 0 and bad_writer == 0 and stray == 0}
