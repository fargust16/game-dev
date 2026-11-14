"""Erosion-like shaping of the island's relief: the land is cut by the water it gathers.

The ridges and stamps of a concept give the island its mass; this gives it its drainage. Each pass routes the water
over the ground with its closed hollows filled (priority-flood) and a little unevenness that changes from pass to
pass, so that no line is ruled along the grid; the water is shared among every lower neighbour (multiple flow
directions), which gathers it into broad swales first and channels only where they deepen. Every cell is then cut
toward the neighbour it drains to by the stream-power law, dz/dt = -K * (A - head)^m * S, solved implicitly from the
mouths upstream so that it cannot overshoot; above a channel's head (the area a channel needs to begin) a slope is
not cut at all. Then the slopes creep (diffusion, stronger on valley floors, where a stream lays down what it
carried). Rock resists unevenly: K varies over the island, so harder ground stands as steps and knolls. Sea and tidal
water are the base level, at the water's surface, and never change.

All quantities are world quantities: A in m2, heights and distances in m. Every number is an authoring default.
"""
import heapq
import math

import numpy as np

NEIGHBOURS = [(dy, dx) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dx or dy]


def flood(H, dry, eps=2e-4):
    """Priority-flood from every cell that is not dry land: the ground with its hollows filled and a hair of fall."""
    n = H.shape[0]
    h = H.ravel().tolist()
    d = dry.ravel().tolist()
    filled = list(h)
    seen = bytearray(0 if v else 1 for v in d)
    wet_edge = (~dry) & (np.roll(dry, 1, 0) | np.roll(dry, -1, 0) | np.roll(dry, 1, 1) | np.roll(dry, -1, 1))
    heap = [(h[i], i) for i in np.flatnonzero(wet_edge.ravel()).tolist()]
    heapq.heapify(heap)
    push, pop = heapq.heappush, heapq.heappop
    while heap:
        z, i = pop(heap)
        y, x = divmod(i, n)
        z += eps
        for yy in (y - 1, y, y + 1):
            if 0 <= yy < n:
                row = yy * n
                for xx in (x - 1, x, x + 1):
                    if 0 <= xx < n:
                        j = row + xx
                        if not seen[j]:
                            seen[j] = 1
                            if h[j] < z:
                                filled[j] = z
                            push(heap, (filled[j], j))
    return np.array(filled).reshape(n, n)


def drops(Z):
    """The fall to each of the eight neighbours, per cell of distance; zero where the neighbour is not lower."""
    n = Z.shape[0]
    pad = np.pad(Z, 1, mode="edge")
    out = []
    for dy, dx in NEIGHBOURS:
        drop = (Z - pad[1 + dy:1 + dy + n, 1 + dx:1 + dx + n]) / math.hypot(dx, dy)
        drop = np.maximum(drop, 0.0)
        if dy == -1:
            drop[0, :] = 0
        if dy == 1:
            drop[-1, :] = 0
        if dx == -1:
            drop[:, 0] = 0
        if dx == 1:
            drop[:, -1] = 0
        out.append(drop)
    return out


def route(Z, dry, cell_area, spread=1.6):
    """
    Over the filled ground Z: the area each cell gathers (m2), shared among the lower neighbours by fall^spread, the
    steepest neighbour of each dry cell (flat index, -1 for none) with the distance to it in cells, and the dry cells
    from the highest down.
    """
    n = Z.shape[0]
    D = drops(Z)
    P = [d ** spread for d in D]
    total = np.maximum(sum(P), 1e-30)
    offs = [dy * n + dx for dy, dx in NEIGHBOURS]
    W = [(p / total).ravel().tolist() for p in P]
    order = np.argsort(-Z.ravel(), kind="stable")
    order = order[dry.ravel()[order]].tolist()
    acc = np.where(dry, cell_area, 0.0).ravel().tolist()
    w0, w1, w2, w3, w4, w5, w6, w7 = W
    o0, o1, o2, o3, o4, o5, o6, o7 = offs
    for i in order:
        a = acc[i]
        if w0[i]:
            acc[i + o0] += a * w0[i]
        if w1[i]:
            acc[i + o1] += a * w1[i]
        if w2[i]:
            acc[i + o2] += a * w2[i]
        if w3[i]:
            acc[i + o3] += a * w3[i]
        if w4[i]:
            acc[i + o4] += a * w4[i]
        if w5[i]:
            acc[i + o5] += a * w5[i]
        if w6[i]:
            acc[i + o6] += a * w6[i]
        if w7[i]:
            acc[i + o7] += a * w7[i]
    stack = np.stack(D)
    k = stack.argmax(0)
    idx = np.arange(n * n).reshape(n, n)
    recv = np.where(dry & (stack.max(0) > 0), idx + np.array(offs)[k], -1)
    run = np.array([math.hypot(dx, dy) for dy, dx in NEIGHBOURS])[k]
    return np.array(acc).reshape(n, n), recv, run, order


def erode(H, base, step, spec, resist=None, uneven=None, log=print):
    """
    H         heights, m over Sea Level.
    base      cells that are the base level and stay as they are (sea, tidal water).
    step      the grid's cell, m.
    spec      passes, k (the cut's rate for a pass), m (the exponent of the gathered area), head (the area a channel
              needs to begin, m2), creep (m2 a pass), floor (extra creep where over floor_area m2 gathers), limit (the
              deepest cut allowed, m).
    resist    how the rock resists where it lies; 1 the island's mean.
    uneven    uneven(p): the unevenness the water of pass p is routed over, m; None for none.
    Returns the new heights, the gathered area of the last pass (m2) and how deep each cell was cut (m, positive).
    """
    passes, k, m = spec.get("passes", 14), spec.get("k", 0.30), spec.get("m", 0.45)
    creep, floor, floor_area = spec.get("creep", 0.35), spec.get("floor", 1.6), spec.get("floor_area", 4000.0)
    limit, head = spec.get("limit", 9.0), spec.get("head", 0.0)
    dry = ~base
    given = H
    # The base level is the water's surface, not its bed: a bank is cut down to the water and no further.
    H = np.where(base, np.maximum(H, 0.0), H)
    start = H.copy()
    n = H.shape[0]
    resist = np.ones_like(H) if resist is None else resist
    for p in range(passes):
        Z = flood(H + (uneven(p) * dry if uneven else 0.0), dry)
        A, recv, run, order = route(Z, dry, step * step)
        f = (k / np.maximum(resist, 0.05) * np.maximum(A - head, 0.0) ** m / (run * step)).ravel().tolist()
        h = H.ravel().tolist()
        r = recv.ravel().tolist()
        for i in reversed(order):
            j = r[i]
            if j >= 0 and h[j] < h[i]:
                h[i] = (h[i] + f[i] * h[j]) / (1.0 + f[i])
        H = np.where(dry, np.array(h).reshape(n, n), H)
        H = np.maximum(H, np.minimum(start, np.maximum(start - limit, 0.3)))
        # Creep: explicit diffusion in sub-steps of at most a fifth of a cell's square.
        rate = (creep + floor * np.clip(A / floor_area, 0.0, 1.0)) / (step * step)
        sub = int(math.ceil(float(rate.max()) / 0.2))
        for _ in range(sub):
            pad = np.pad(H, 1, mode="edge")
            lap = pad[:-2, 1:-1] + pad[2:, 1:-1] + pad[1:-1, :-2] + pad[1:-1, 2:] - 4.0 * H
            H = np.where(dry, H + rate / sub * lap, H)
    H = np.where(dry, flood(H, dry), H)
    A, _, _, _ = route(H, dry, step * step)
    log("erosion: %d passes on a %.1f m grid, cut up to %.1f m, mean %.2f m over the dry land" % (
        passes, step, float((start - H)[dry].max()), float(np.maximum(start - H, 0)[dry].mean())))
    return np.where(base, given, H), A, np.where(dry, np.maximum(start - H, 0.0), 0.0)


def fill(H, base, keep=None):
    """The ground with its closed hollows filled level; keep marks cells that stay as they are (built ground)."""
    Z = flood(H, ~base)
    out = np.where(base, H, Z)
    return out if keep is None else np.where(keep, H, out)


def resize(a, n):
    """Bilinear, from one square grid to another of n samples a side over the same ground."""
    if a.shape[0] == n:
        return a
    x = np.linspace(0, a.shape[0] - 1, n)
    i = np.minimum(x.astype(int), a.shape[0] - 2)
    t = x - i
    rows = a[i] * (1 - t)[:, None] + a[i + 1] * t[:, None]
    return rows[:, i] * (1 - t)[None, :] + rows[:, i + 1] * t[None, :]
