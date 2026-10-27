"""G3f lattice conventions and dependency-free helpers (numpy + the standard library only).

Conventions are G3e's (GEOGRAPHY_LMC_TECHNICAL_SPEC.md §1): 1 025 x 1 025 vertices, cell 2.734375 m, arrays indexed
[z, x], flat index i = z * N + x, world position (x c, z c), +x east, +z north. Land is height > 24 m.
"""
import hashlib
import heapq
import math
import struct
import zlib
from collections import deque

import numpy as np

N = 1025
C = 2.734375
SEA = 24.0
BASE = 24.5            # land is written at >= 24.5 m (carried, G3e spec §1)
DIAG = math.sqrt(2.0)

# 8-neighbour offsets in a fixed order (dz, dx); every loop that breaks ties uses this order
NB8 = ((-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1))
NB4 = ((-1, 0), (0, -1), (0, 1), (1, 0))


# ----------------------------------------------------------------------------- I/O

def load_f32(path, n=N):
    return np.fromfile(path, dtype="<f4").reshape(n, n).astype(np.float64)


def save_f32(path, h):
    np.asarray(h, dtype="<f4").tofile(path)


def load_u8(path, n=N):
    return np.fromfile(path, dtype=np.uint8).reshape(n, n)


def save_u8(path, a):
    np.asarray(a, dtype=np.uint8).tofile(path)


def save_i32(path, a):
    np.asarray(a, dtype="<i4").tofile(path)


def sha256_bytes(b):
    return hashlib.sha256(b).hexdigest()


def sha256_file(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def lattice_sha256(h):
    """SHA-256 of the float32 little-endian bytes, the form the lattice is stored in."""
    return sha256_bytes(np.asarray(h, dtype="<f4").tobytes())


def write_png(path, rgb):
    """rgb: uint8 (H, W, 3); row 0 is the top of the image."""
    rgb = np.ascontiguousarray(rgb, dtype=np.uint8)
    h, w, _ = rgb.shape
    raw = b"".join(b"\x00" + rgb[r].tobytes() for r in range(h))

    def chunk(t, d):
        c = struct.pack(">I", len(d)) + t + d
        return c + struct.pack(">I", zlib.crc32(t + d) & 0xFFFFFFFF)

    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
    png += chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


def hillshade(h, cell=C, az=315.0, alt=45.0):
    gz, gx = np.gradient(h, cell)
    slope = np.arctan(np.hypot(gx, gz))
    aspect = np.arctan2(-gx, gz)
    a, e = np.radians(az), np.radians(alt)
    s = np.sin(e) * np.cos(slope) + np.cos(e) * np.sin(slope) * np.cos(a - aspect)
    return np.clip(s, 0, 1)


def north_up(img):
    """Row z grows northwards; images grow downwards."""
    return img[::-1]


# ----------------------------------------------------------------------------- geometry

def to_vertex(p, cell=C):
    """World metres (x, z) -> nearest vertex (x, z)."""
    return int(round(p[0] / cell)), int(round(p[1] / cell))


def to_world(v, cell=C):
    return v[0] * cell, v[1] * cell


def raster4(p, q):
    """4-connected raster of the vertex segment p -> q, both (x, z); deterministic."""
    (x0, z0), (x1, z1) = p, q
    n = int(max(abs(x1 - x0), abs(z1 - z0)) * 4) + 1
    out = [(int(round(x0)), int(round(z0)))]
    for k in range(1, n + 1):
        t = k / n
        x, z = int(round(x0 + (x1 - x0) * t)), int(round(z0 + (z1 - z0) * t))
        px, pz = out[-1]
        if (x, z) == (px, pz):
            continue
        if x != px and z != pz:
            out.append((x, pz))
        out.append((x, z))
    return out


def raster_polyline4(points_vertex):
    """4-connected raster of a vertex polyline; consecutive duplicates removed."""
    out = []
    for a, b in zip(points_vertex[:-1], points_vertex[1:]):
        seg = raster4(a, b)
        if out and seg[0] == out[-1]:
            seg = seg[1:]
        out.extend(seg)
    if not out and points_vertex:
        out = [tuple(int(round(v)) for v in points_vertex[0])]
    return out


def resample_polyline(points, step):
    """World polyline resampled every `step` metres (endpoints kept)."""
    pts = [np.asarray(p, dtype=np.float64) for p in points]
    out = [pts[0]]
    carry = 0.0
    for a, b in zip(pts[:-1], pts[1:]):
        seg = float(np.hypot(*(b - a)))
        if seg <= 0:
            continue
        t = step - carry
        while t <= seg + 1e-9:
            out.append(a + (b - a) * (t / seg))
            t += step
        carry = seg - (t - step)
    if np.hypot(*(out[-1] - pts[-1])) > 1e-6:
        out.append(pts[-1])
    return [tuple(map(float, p)) for p in out]


def polyline_length(points):
    return float(sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in zip(points[:-1], points[1:])))


def point_at_arc(points, s):
    acc = 0.0
    for a, b in zip(points[:-1], points[1:]):
        seg = math.hypot(b[0] - a[0], b[1] - a[1])
        if acc + seg >= s and seg > 0:
            t = (s - acc) / seg
            return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t)
        acc += seg
    return tuple(points[-1])


def fill_polygon(poly_world, n=N, cell=C):
    """Vertices whose world position lies inside the closed polygon (even-odd rule)."""
    pv = np.asarray(poly_world, dtype=np.float64) / cell
    zmin, zmax = int(math.floor(pv[:, 1].min())), int(math.ceil(pv[:, 1].max()))
    mask = np.zeros((n, n), dtype=bool)
    m = len(pv)
    for z in range(max(0, zmin), min(n - 1, zmax) + 1):
        xs = []
        for k in range(m):
            (x1, z1), (x2, z2) = pv[k], pv[(k + 1) % m]
            if (z1 <= z < z2) or (z2 <= z < z1):
                xs.append(x1 + (z - z1) * (x2 - x1) / (z2 - z1))
        xs.sort()
        for a, b in zip(xs[0::2], xs[1::2]):
            lo, hi = int(math.ceil(a)), int(math.floor(b))
            if hi >= lo:
                mask[z, max(0, lo):min(n - 1, hi) + 1] = True
    return mask


# ----------------------------------------------------------------------------- distance transforms

def _edt1d(f, INF=1e20):
    """Felzenszwalb-Huttenlocher squared distance transform of one sampled function (list of floats);
    samples >= INF are absent. Returns a list; INF where no sample exists."""
    n = len(f)
    pts = [q for q in range(n) if f[q] < INF]
    if not pts:
        return [INF] * n
    v = [pts[0]]
    zb = [-math.inf, math.inf]
    for q in pts[1:]:
        fq = f[q] + q * q
        while True:
            p = v[-1]
            s = (fq - (f[p] + p * p)) / (2.0 * (q - p))
            if s <= zb[-2]:
                v.pop()
                zb.pop()
                zb[-1] = math.inf
                if not v:
                    break
            else:
                break
        if not v:
            v.append(q)
            zb = [-math.inf, math.inf]
        else:
            zb[-1] = s
            v.append(q)
            zb.append(math.inf)
    d = [0.0] * n
    k = 0
    for q in range(n):
        while zb[k + 1] < q:
            k += 1
        p = v[k]
        d[q] = (q - p) * (q - p) + f[p]
    return d


def edt(sources, cell=C):
    """Exact Euclidean distance (metres) from every vertex to the nearest True vertex of `sources`."""
    INF = 1e20
    a = np.where(sources, 0.0, INF)
    n0, n1 = a.shape
    tmp = np.empty_like(a)
    for x in range(n1):
        tmp[:, x] = _edt1d(a[:, x].tolist())
    out = np.empty_like(a)
    for z in range(n0):
        out[z, :] = _edt1d(tmp[z, :].tolist())
    out = np.sqrt(np.minimum(out, INF)) * cell
    out[out > 1e9] = np.inf
    return out


def dijkstra8(domain, sources, weight, cell=C, limit=math.inf):
    """Weighted geodesic distance on the 8-neighbour graph restricted to `domain`.

    sources: dict flat index -> start value; weight: per-vertex cost per metre (array); edge cost =
    length * (w_u + w_v) / 2. Ties are broken by flat index (heap order). Returns (dist, parent) as flat arrays;
    parent is -1 for sources and unreached vertices."""
    n0, n1 = domain.shape
    dom = domain.reshape(-1).tolist()
    w = np.asarray(weight, dtype=np.float64).reshape(-1).tolist()
    dist = [math.inf] * (n0 * n1)
    parent = [-1] * (n0 * n1)
    heap = []
    for i, v in sorted(sources.items()):
        if dom[i] and v < dist[i]:
            dist[i] = v
            heap.append((v, i))
    heapq.heapify(heap)
    offs = [(dz, dx, cell * (DIAG if dz and dx else 1.0)) for dz, dx in NB8]
    while heap:
        d, i = heapq.heappop(heap)
        if d > dist[i] or d > limit:
            continue
        z, x = divmod(i, n1)
        wi = w[i]
        for dz, dx, L in offs:
            zz, xx = z + dz, x + dx
            if zz < 0 or zz >= n0 or xx < 0 or xx >= n1:
                continue
            j = zz * n1 + xx
            if not dom[j]:
                continue
            nd = d + L * 0.5 * (wi + w[j])
            if nd < dist[j]:
                dist[j] = nd
                parent[j] = i
                heapq.heappush(heap, (nd, j))
    return np.array(dist).reshape(n0, n1), np.array(parent, dtype=np.int64)


# ----------------------------------------------------------------------------- connectivity

def components(mask, conn=8):
    """Labels (int32, -1 outside the mask) and the component count; labels in raster order of first vertex."""
    n0, n1 = mask.shape
    m = mask.reshape(-1).tolist()
    lab = [-1] * (n0 * n1)
    offs = NB8 if conn == 8 else NB4
    k = 0
    for i0 in np.flatnonzero(mask).tolist():
        if lab[i0] != -1:
            continue
        lab[i0] = k
        dq = deque([i0])
        while dq:
            i = dq.popleft()
            z, x = divmod(i, n1)
            for dz, dx in offs:
                zz, xx = z + dz, x + dx
                if 0 <= zz < n0 and 0 <= xx < n1:
                    j = zz * n1 + xx
                    if m[j] and lab[j] == -1:
                        lab[j] = k
                        dq.append(j)
        k += 1
    return np.array(lab, dtype=np.int32).reshape(n0, n1), k


def dilate(mask, r_vertices, conn=8):
    out = mask.copy()
    for _ in range(r_vertices):
        m = out.copy()
        m[1:, :] |= out[:-1, :]; m[:-1, :] |= out[1:, :]; m[:, 1:] |= out[:, :-1]; m[:, :-1] |= out[:, 1:]
        if conn == 8:
            m[1:, 1:] |= out[:-1, :-1]; m[:-1, :-1] |= out[1:, 1:]; m[1:, :-1] |= out[:-1, 1:]; m[:-1, 1:] |= out[1:, :-1]
        out = m
    return out


def shore_of(land):
    """Land vertices 4-adjacent to non-land."""
    sea = ~land
    adj = np.zeros_like(land)
    adj[1:, :] |= sea[:-1, :]; adj[:-1, :] |= sea[1:, :]; adj[:, 1:] |= sea[:, :-1]; adj[:, :-1] |= sea[:, 1:]
    return land & adj
