"""Veilmouth's island and city concepts (owner, 2026-11-13/14: VS-ISLAND, the world redesign).

One generator, one data file for each concept. A concept names its coast (control points with a shore type), its
ridges, rivers, StampIT stamps, city districts (rectangular frames, each with its own turn), roads and zones; this
module turns that into a height field in metres over Sea Level, a plan, statistics and the data for a 3D preview.
Nothing here touches the Unity project: a concept is judged first, and only the approved one is built.

Plan metres as the city plan's: px east, py south, from the area's north-west corner; world X = px - 500,
world Z = 500 - py. Heights are metres over Sea Level (VeilmouthScene.SeaLevel, 24 m in the scene).
Every number is an authoring default. Randomness is seeded by the concept, so a concept builds the same every time.
"""
import base64
import json
import math
import os
import struct
import zlib

import numpy as np

SIZE = 1000.0
# 513 for a quick look; ISLAND_N=1025 for the height map the scene takes, one sample for each of the terrain's.
N = int(os.environ.get("ISLAND_N", "513"))
STEP = SIZE / (N - 1)
SEA_LEVEL = 24.0
BUDGET = 100.0
HEIGHTMAP = 1025

AX = np.linspace(0.0, SIZE, N)
GX, GY = np.meshgrid(AX, AX)

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "..", ".."))
STAMPS = os.path.join(ROOT, "unity", "Assets", "Rowlan", "Terrain", "Stamps", "Examples", "Heightmaps")
CACHE = os.path.join(HERE, "_cache")

# Shore types: W the ramp from the waterline to the inland height, m; L the lift of the ground behind the shore, m;
# k0 and a the sea bed's first drop and its length, m; rag the shoreline's unevenness, m.
COAST = {
    "b": dict(W=95.0, L=0.0, k0=1.0, a=40.0, rag=1.5),    # sand beach
    "g": dict(W=55.0, L=1.5, k0=1.8, a=25.0, rag=3.0),    # gravel beach
    "r": dict(W=26.0, L=6.0, k0=3.5, a=14.0, rag=7.0),    # rocky shore
    "c": dict(W=9.0, L=20.0, k0=6.0, a=10.0, rag=5.0),    # cliff
    "m": dict(W=150.0, L=0.0, k0=0.6, a=60.0, rag=2.0),   # marsh and mudflat
    "q": dict(W=3.0, L=0.0, k0=4.5, a=3.0, rag=0.0),      # quay, built
}

# Street classes of the city plan C5 (§29.2): the whole section and its carriageway, m.
STREETS = {"main": (22.0, 10.0), "side": (17.0, 7.0), "street": (12.0, 6.0), "lane": (10.0, 5.0),
           "quay": (15.0, 9.0), "emb": (16.0, 7.0), "works": (14.0, 9.0)}
# Row families of the kit (tools/slice/veilmouth_c5_plan.py FAMILIES): height to the ridge and depth, m.
FAMILIES = {"shop": (19.0, 10.0), "town": (15.0, 10.0), "tenement": (16.0, 8.0), "low": (12.0, 8.0),
            "warehouse": (16.0, 8.0), "villa": (13.0, 15.0)}

COL = {
    "pave": (203, 199, 190), "carriage": (146, 143, 139), "road": (156, 132, 100), "track": (176, 152, 118),
    "shop": (133, 93, 80), "town": (158, 108, 86), "tenement": (116, 98, 92), "low": (97, 86, 82),
    "warehouse": (122, 116, 104), "villa": (186, 170, 148), "civic": (98, 82, 126), "church": (128, 112, 156),
    "works": (92, 92, 99), "yard_rich": (150, 178, 118), "yard_mid": (190, 184, 170), "yard_poor": (150, 132, 112),
    "park": (118, 162, 98), "square": (214, 206, 188), "market": (222, 196, 150), "cemetery": (140, 164, 126),
    "wall": (70, 66, 62), "bridge": (188, 172, 142), "pier": (140, 116, 88), "farm": (196, 190, 128),
    "cottage": (110, 84, 64), "water": (86, 140, 178), "mark": (200, 40, 40),
}


# ---------------------------------------------------------------------------------------------------- small tools

def smoothstep(a, b, v):
    t = np.clip((v - a) / (b - a), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def smin(a, b, k):
    """A smooth minimum over k metres."""
    return -k * np.logaddexp(-a / k, -b / k)


def catmull(points, closed, per=16):
    P = np.asarray(points, float)
    n = len(P)
    out = []
    t = np.linspace(0.0, 1.0, per, endpoint=False)[:, None]
    for i in (range(n) if closed else range(n - 1)):
        p0 = P[(i - 1) % n] if (closed or i > 0) else P[0]
        p1, p2 = P[i], P[(i + 1) % n]
        p3 = P[(i + 2) % n] if (closed or i + 2 < n) else P[-1]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t ** 2
                          + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    if not closed:
        out.append(P[-1:])
    return np.vstack(out)


def resample(poly, step):
    poly = np.asarray(poly, float)
    seg = np.hypot(np.diff(poly[:, 0]), np.diff(poly[:, 1]))
    cum = np.concatenate([[0.0], np.cumsum(seg)])
    n = max(2, int(cum[-1] / step))
    s = np.linspace(0.0, cum[-1], n + 1)
    return np.stack([np.interp(s, cum, poly[:, k]) for k in range(poly.shape[1])], 1), s


def dist_field(poly, X=None, Y=None, chunk=6144):
    """The distance to a polyline and the arc length of its nearest point, for every grid point."""
    X = GX if X is None else X
    Y = GY if Y is None else Y
    a = np.asarray(poly[:-1, :2], np.float32)
    ab = np.asarray(poly[1:, :2], np.float32) - a
    l2 = np.maximum((ab ** 2).sum(1), 1e-9)
    seglen = np.sqrt(l2)
    cum = np.concatenate([[0.0], np.cumsum(seglen)]).astype(np.float32)
    P = np.stack([X.ravel(), Y.ravel()], 1).astype(np.float32)
    D = np.empty(len(P), np.float32)
    S = np.empty(len(P), np.float32)
    for i in range(0, len(P), chunk):
        p = P[i:i + chunk]
        apx = p[:, None, 0] - a[None, :, 0]
        apy = p[:, None, 1] - a[None, :, 1]
        t = np.clip((apx * ab[None, :, 0] + apy * ab[None, :, 1]) / l2[None], 0.0, 1.0)
        dx = apx - t * ab[None, :, 0]
        dy = apy - t * ab[None, :, 1]
        d2 = dx * dx + dy * dy
        k = d2.argmin(1)
        r = np.arange(len(p))
        D[i:i + chunk] = np.sqrt(d2[r, k])
        S[i:i + chunk] = cum[k] + t[r, k] * seglen[k]
    return D.reshape(X.shape), S.reshape(X.shape)


def poly_mask(poly, xs, ys):
    """Even-odd fill of a polygon on the points xs × ys."""
    poly = np.asarray(poly, float)
    x0, y0 = poly[:, 0], poly[:, 1]
    x1, y1 = np.roll(x0, -1), np.roll(y0, -1)
    mask = np.zeros((len(ys), len(xs)), bool)
    for i, y in enumerate(ys):
        cross = (y0 <= y) != (y1 <= y)
        if not cross.any():
            continue
        xi = x0[cross] + (y - y0[cross]) * (x1[cross] - x0[cross]) / (y1[cross] - y0[cross])
        row = np.zeros(len(xs) + 1, np.int32)
        np.add.at(row, np.searchsorted(xs, xi), 1)
        mask[i] = (np.cumsum(row)[:-1] % 2) == 1
    return mask


_LATTICE = {}


def vnoise(X, Y, cell, seed):
    T = _LATTICE.get(seed)
    if T is None:
        T = _LATTICE[seed] = np.random.default_rng(seed).random((256, 256)).astype(np.float32)
    fx, fy = X / cell, Y / cell
    ix, iy = np.floor(fx).astype(int), np.floor(fy).astype(int)
    tx, ty = fx - ix, fy - iy
    tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    a, b = T[iy % 256, ix % 256], T[iy % 256, (ix + 1) % 256]
    c, d = T[(iy + 1) % 256, ix % 256], T[(iy + 1) % 256, (ix + 1) % 256]
    top = a + (b - a) * tx
    return top + ((c + (d - c) * tx) - top) * ty


def fbm(X, Y, cell, seed, octaves=4, gain=0.5, ridged=False):
    total, amp, norm = 0.0, 1.0, 0.0
    for o in range(octaves):
        n = vnoise(X, Y, cell / (2 ** o), seed + 17 * o) * 2.0 - 1.0
        if ridged:
            n = 1.0 - 2.0 * np.abs(n)
        total = total + amp * n
        norm += amp
        amp *= gain
    return total / norm


def box_blur(a, k):
    k = max(1, int(k) | 1)
    p = k // 2
    ap = np.pad(a.astype(np.float64), p, mode="edge")
    ii = np.zeros((ap.shape[0] + 1, ap.shape[1] + 1))
    ii[1:, 1:] = ap.cumsum(0).cumsum(1)
    h, w = a.shape
    return (ii[k:k + h, k:k + w] - ii[0:h, k:k + w] - ii[k:k + h, 0:w] + ii[0:h, 0:w]) / (k * k)


def blur(a, metres):
    k = max(3, int(metres / STEP) | 1)
    return box_blur(box_blur(box_blur(a, k), k), k)


def bil(F, X, Y):
    fx = np.clip(X / STEP, 0, N - 1.001)
    fy = np.clip(Y / STEP, 0, N - 1.001)
    ix, iy = fx.astype(int), fy.astype(int)
    tx, ty = fx - ix, fy - iy
    return ((F[iy, ix] * (1 - tx) + F[iy, ix + 1] * tx) * (1 - ty)
            + (F[iy + 1, ix] * (1 - tx) + F[iy + 1, ix + 1] * tx) * ty)


def rect_sdf(X, Y, origin, angle, U, V):
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    dx, dy = X - origin[0], Y - origin[1]
    u, v = dx * c + dy * s, -dx * s + dy * c
    qu, qv = np.abs(u - U / 2) - U / 2, np.abs(v - V / 2) - V / 2
    return np.hypot(np.maximum(qu, 0), np.maximum(qv, 0)) + np.minimum(np.maximum(qu, qv), 0), u, v


# ---------------------------------------------------------------------------------------------------- PNG

def _chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def png_rgb(img):
    img = np.clip(img, 0, 255).astype(np.uint8)
    h, w, _ = img.shape
    raw = np.zeros((h, 1 + w * 3), np.uint8)
    raw[:, 1:] = img.reshape(h, w * 3)
    return (b"\x89PNG\r\n\x1a\n" + _chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0))
            + _chunk(b"IDAT", zlib.compress(raw.tobytes(), 6)) + _chunk(b"IEND", b""))


def write_png_rgb(path, img):
    with open(path, "wb") as fh:
        fh.write(png_rgb(img))


def write_png_gray16(path, values):
    v = np.clip(values, 0, 65535).astype(">u2")
    h, w = v.shape
    raw = np.zeros((h, 1 + w * 2), np.uint8)
    raw[:, 1:] = v.view(np.uint8).reshape(h, w * 2)
    with open(path, "wb") as fh:
        fh.write(b"\x89PNG\r\n\x1a\n" + _chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 16, 0, 0, 0, 0))
                 + _chunk(b"IDAT", zlib.compress(raw.tobytes(), 6)) + _chunk(b"IEND", b""))


def _unfilter_paeth(F):
    H, W, K = F.shape
    D = H + W - 1
    Gt = np.zeros((D, H, K), np.uint8)
    for r in range(H):
        Gt[r:r + W, r, :] = F[r]
    Xd = np.zeros((D, H, K), np.uint8)
    p1, p2, cur = (np.zeros((H + 1, K), np.int16) for _ in range(3))
    for d in range(D):
        lo, hi = max(0, d - W + 1), min(H - 1, d)
        a, b, c = p1[lo + 1:hi + 2], p1[lo:hi + 1], p2[lo:hi + 1]
        pa, pb, pc = np.abs(b - c), np.abs(a - c), np.abs(a + b - 2 * c)
        pred = np.where((pa <= pb) & (pa <= pc), a, np.where(pb <= pc, b, c))
        x = (Gt[d, lo:hi + 1].astype(np.int16) + pred) & 255
        cur.fill(0)
        cur[lo + 1:hi + 2] = x
        Xd[d, lo:hi + 1] = x
        p2, p1, cur = p1, cur, p2
    X = np.empty((H, W, K), np.uint8)
    for r in range(H):
        X[r] = Xd[r:r + W, r]
    return X


def stamp(name):
    """A StampIT height map, 0..1 at 1024 px, decoded at 16 bits from the vendor's PNG and cached beside this file."""
    os.makedirs(CACHE, exist_ok=True)
    cached = os.path.join(CACHE, name.replace(" ", "_") + ".npy")
    if os.path.exists(cached):
        return np.load(cached)
    with open(os.path.join(STAMPS, name + ".png"), "rb") as fh:
        data = fh.read()
    pos, idat, ihdr = 8, [], None
    while pos < len(data):
        ln, typ = struct.unpack(">I4s", data[pos:pos + 8])
        if typ == b"IHDR":
            ihdr = struct.unpack(">IIBBBBB", data[pos + 8:pos + 8 + ln])
        elif typ == b"IDAT":
            idat.append(data[pos + 8:pos + 8 + ln])
        pos += 12 + ln
    w, h, depth, colour, _, _, interlace = ihdr
    assert depth == 16 and interlace == 0
    bpp = {0: 1, 2: 3, 4: 2, 6: 4}[colour] * 2
    raw = np.frombuffer(zlib.decompress(b"".join(idat)), np.uint8).reshape(h, 1 + w * bpp)
    F = np.ascontiguousarray(raw[:, 1:].reshape(h, w, bpp)[:, :, 0:2])
    X = F if (raw[:, 0] == 0).all() else _unfilter_paeth(F)
    hm = ((X[:, :, 0].astype(np.uint16) << 8) | X[:, :, 1]).astype(np.float32) / 65535.0
    hm = hm.reshape(h // 4, 4, w // 4, 4).mean(axis=(1, 3))
    np.save(cached, hm)
    return hm


def apply_stamp(H, spec, land):
    """A stamp laid at a place: its footprint, turn and amplitude; a radial mask unless its border is zero."""
    S = stamp(spec["name"])
    size, rot = spec["size"], math.radians(spec.get("rot", 0.0))
    dx, dy = GX - spec["at"][0], GY - spec["at"][1]
    u = (dx * math.cos(rot) + dy * math.sin(rot)) / size + 0.5
    v = (-dx * math.sin(rot) + dy * math.cos(rot)) / size + 0.5
    inside = (u > 0) & (u < 1) & (v > 0) & (v < 1)
    fu, fv = np.clip(u, 0, 0.9999) * (S.shape[1] - 1), np.clip(v, 0, 0.9999) * (S.shape[0] - 1)
    iu, iv = fu.astype(int), fv.astype(int)
    tu, tv = fu - iu, fv - iv
    val = ((S[iv, iu] * (1 - tu) + S[iv, np.minimum(iu + 1, S.shape[1] - 1)] * tu) * (1 - tv)
           + (S[np.minimum(iv + 1, S.shape[0] - 1), iu] * (1 - tu)
              + S[np.minimum(iv + 1, S.shape[0] - 1), np.minimum(iu + 1, S.shape[1] - 1)] * tu) * tv)
    val = val - spec.get("zero", 0.0)
    r = np.hypot(u - 0.5, v - 0.5) * 2.0
    mask = inside * (1.0 - smoothstep(spec.get("fade0", 0.62), spec.get("fade1", 0.96), r)) if spec.get("mask", True) else inside * 1.0
    if not spec.get("sea", False):
        mask = mask * land
    add = val * spec["amp"] * mask
    return np.maximum(H, H * (1 - mask) + add) if spec.get("mode") == "max" else H + add


# ---------------------------------------------------------------------------------------------------- the city

class District:
    """
    A district: a rectangular frame with its own turn, cut into rectangular blocks by straight streets, as C5's blocks
    are (city plan §29). cols and rows are the blocks' sizes along the frame's u and v; sx and sy the street classes
    between them; border the classes on the frame's four sides (u low, u high, v low, v high), None for no street.
    cells names what a block is when it is not built with houses.
    """

    def __init__(self, name, origin, angle, cols, rows, sx=None, sy=None, border=("street",) * 4, level=2.0,
                 tilt=(0.0, 0.0), apron=16.0, family="town", wealth="mid", cells=None, label=None):
        self.name, self.origin, self.angle = name, origin, angle
        self.cols, self.rows = cols, rows
        self.sx = sx or ["street"] * (len(cols) - 1)
        self.sy = sy or ["street"] * (len(rows) - 1)
        self.border, self.level, self.tilt, self.apron = border, level, tilt, apron
        self.family, self.wealth, self.cells, self.label = family, wealth, cells or {}, label or name
        w = lambda c: STREETS[c][0] if c else 0.0
        self.u0, cursor = [], w(border[0])
        for i, c in enumerate(cols):
            self.u0.append(cursor)
            cursor += c + (w(self.sx[i]) if i < len(cols) - 1 else w(border[1]))
        self.U = cursor
        self.v0, cursor = [], w(border[2])
        for j, r in enumerate(rows):
            self.v0.append(cursor)
            cursor += r + (w(self.sy[j]) if j < len(rows) - 1 else w(border[3]))
        self.V = cursor
        self.c, self.s = math.cos(math.radians(angle)), math.sin(math.radians(angle))

    def world(self, u, v):
        return (self.origin[0] + u * self.c - v * self.s, self.origin[1] + u * self.s + v * self.c)

    def quad(self, u0, v0, u1, v1):
        return [self.world(u0, v0), self.world(u1, v0), self.world(u1, v1), self.world(u0, v1)]

    def frame(self):
        return self.quad(0, 0, self.U, self.V)

    def centre(self):
        return self.world(self.U / 2, self.V / 2)

    def streets(self):
        """Every street of the frame as (class, quad of the whole section, quad of the carriageway)."""
        out = []

        def add(cls, along_v, lo, length):
            if not cls:
                return
            width, carriage = STREETS[cls]
            off = (width - carriage) / 2
            if along_v:
                out.append((cls, self.quad(lo, 0, lo + width, length), self.quad(lo + off, 0, lo + width - off, length)))
            else:
                out.append((cls, self.quad(0, lo, length, lo + width), self.quad(0, lo + off, length, lo + width - off)))

        add(self.border[0], True, 0.0, self.V)
        add(self.border[1], True, self.U - (STREETS[self.border[1]][0] if self.border[1] else 0), self.V)
        add(self.border[2], False, 0.0, self.U)
        add(self.border[3], False, self.V - (STREETS[self.border[3]][0] if self.border[3] else 0), self.U)
        for i, cls in enumerate(self.sx):
            add(cls, True, self.u0[i] + self.cols[i], self.V)
        for j, cls in enumerate(self.sy):
            add(cls, False, self.v0[j] + self.rows[j], self.U)
        return out

    def blocks(self):
        """Every block as (i, j, u0, v0, width, depth, spec); a spanning block covers the streets between its cells."""
        covered, out = set(), []
        for j in range(len(self.rows)):
            for i in range(len(self.cols)):
                if (i, j) in covered:
                    continue
                spec = self.cells.get((i, j), {"kind": "built"})
                if isinstance(spec, str):
                    spec = {"kind": spec}
                ni, nj = spec.get("span", (1, 1))
                for a in range(ni):
                    for b in range(nj):
                        covered.add((i + a, j + b))
                u1 = self.u0[i + ni - 1] + self.cols[i + ni - 1]
                v1 = self.v0[j + nj - 1] + self.rows[j + nj - 1]
                out.append((i, j, self.u0[i], self.v0[j], u1 - self.u0[i], v1 - self.v0[j], spec))
        return out


def fronts(length):
    """Fronts of 10 and 15 m that fill a run on the kit's 5 m step."""
    k, seq = int(length // 5), []
    while k >= 2:
        if k == 3 or k % 2 == 1:
            seq.append(15.0)
            k -= 3
        else:
            seq.append(10.0)
            k -= 2
    return seq


def block_plots(width, depth, family):
    """
    The houses round a block (§29.2): a row on each long side owning the corners, rows between them on the short
    sides, a passage of at least 4 m into the inner yard (R5). Rectangles (u0, v0, u1, v1) in the block's own frame.
    """
    d = FAMILIES[family][1]
    long_u = width >= depth
    A, B = (width, depth) if long_u else (depth, width)
    out, pos = [], (A % 5) / 2
    for f in fronts(A):
        out += [(pos, 0.0, pos + f, d), (pos, B - d, pos + f, B)]
        pos += f
    avail = B - 2 * d
    passage = avail % 5
    if passage < 4:
        passage += 5
    if avail - passage >= 10:
        pos = d + passage
        for f in fronts(avail - passage):
            out.append((0.0, pos, d, pos + f))
            pos += f
        pos = d
        for f in fronts(avail - passage):
            out.append((A - d, pos, A, pos + f))
            pos += f
    return out if long_u else [(b0, a0, b1, a1) for a0, b0, a1, b1 in out]


# ---------------------------------------------------------------------------------------------------- the land

def build_land(C, log=print):
    """The concept's height field, metres over Sea Level, and the fields the plan and the checks read."""
    seed = C.get("seed", 1)
    F = {}

    # The coast: a spline through the control points, each with its shore type, then the shore's own unevenness.
    pts = np.array([(p[0], p[1]) for p in C["coast"]], float)
    kinds = [p[2] for p in C["coast"]]
    lifts = [p[3] if len(p) > 3 else COAST[p[2]]["L"] for p in C["coast"]]
    per = 20
    line = catmull(pts, True, per)
    n = len(pts)
    param = np.arange(len(line)) / per

    def along(values):
        v = np.asarray(values, float)
        return np.interp(param, np.arange(n + 1), np.append(v, v[0]))

    attr = {k: along([COAST[t][k] for t in kinds]) for k in ("W", "k0", "a", "rag")}
    attr["L"] = along(lifts)
    for t in "bgrcmq":
        attr["is_" + t] = along([1.0 if k == t else 0.0 for k in kinds])
    closed = np.vstack([line, line[:1]])
    seglen = np.hypot(np.diff(closed[:, 0]), np.diff(closed[:, 1]))
    arc = np.concatenate([[0.0], np.cumsum(seglen)])[:-1]
    tangent = np.roll(line, -1, 0) - np.roll(line, 1, 0)
    normal = np.stack([tangent[:, 1], -tangent[:, 0]], 1)
    normal /= np.maximum(np.hypot(normal[:, 0], normal[:, 1]), 1e-9)[:, None]
    total = arc[-1] + seglen[-1]
    phase = arc / total * 2 * math.pi
    rng = np.random.default_rng(seed)
    wobble = np.zeros(len(line))
    # Two long waves only, each beating against a slower one: coves and points of 60-170 m, never a saw's teeth.
    for wave, weight in ((int(total / 170), 1.0), (int(total / 64), 0.6)):
        wobble += weight * np.sin(wave * phase + rng.random() * 6.283) * np.cos((wave // 3 + 1) * phase + rng.random() * 6.283)
    # Rock and cliff alone break into small coves and points of about 30 m; sand and shingle keep their long arcs.
    hard = np.clip(attr["is_r"] + attr["is_c"], 0, 1)
    small = np.zeros(len(line))
    for _ in range(5):
        small += np.sin(int(total / rng.uniform(26.0, 72.0)) * phase + rng.random() * 6.283)
    line = line + normal * (wobble * attr["rag"] * 1.5 + small * hard * 1.3)[:, None]
    coast = np.vstack([line, line[:1]])
    F["coast"] = line
    land = poly_mask(line, AX, AX)
    for islet in C.get("islets", []):
        land |= poly_mask(catmull(islet, True, 10), AX, AX)
    log("coast %.0f m, land %.0f m2" % (total, land.sum() * STEP * STEP))

    dist, s = dist_field(coast)
    arc_full = np.concatenate([arc, [total]])

    def at_coast(key):
        return np.interp(s, arc_full, np.append(attr[key], attr[key][0]))

    dl = np.where(land, dist, 0.0)
    ds = np.where(land, 0.0, dist)
    # The nearest shore point changes abruptly across a headland's axis, so what is read from it is smoothed.
    W, lift = np.maximum(blur(at_coast("W"), 24.0), 3.0), blur(at_coast("L"), 36.0)
    for t in "bgrcmq":
        F["is_" + t] = blur(at_coast("is_" + t), 30.0)

    # The ground inland: a gentle rise from the shore, the ridges joined by a p-norm so that they meet as one massif,
    # the ground lifted behind cliffs and rocky shores, and spurs from a ridged noise on the high ground only.
    base = C.get("base", 4.0) * (1.0 - np.exp(-dl / C.get("base_reach", 160.0)))
    wx = GX + 22.0 * fbm(GX, GY, 210.0, seed + 3, 3)
    wy = GY + 22.0 * fbm(GX, GY, 210.0, seed + 5, 3)
    p = 2.6
    acc = np.zeros_like(GX)
    for ridge in C.get("ridges", []):
        rp, _ = resample(np.array(ridge, float), 8.0)
        d, rs = dist_field(rp[:, :2], wx, wy)
        seg = np.concatenate([[0.0], np.cumsum(np.hypot(np.diff(rp[:, 0]), np.diff(rp[:, 1])))])
        height, width = np.interp(rs, seg, rp[:, 2]), np.interp(rs, seg, rp[:, 3])
        t = np.clip(d / width, 0.0, 1.0)
        acc += (height * (np.exp(-2.4 * t * t) - math.exp(-2.4)) / (1 - math.exp(-2.4))) ** p
    for hx, hy, hh, hr in C.get("hills", []):
        t = np.clip(np.hypot(wx - hx, wy - hy) / hr, 0.0, 1.0)
        acc += (hh * (np.exp(-2.4 * t * t) - math.exp(-2.4)) / (1 - math.exp(-2.4))) ** p
    acc += (np.maximum(lift, 0.0) * np.exp(-dl / C.get("lift_reach", 110.0))) ** p
    high = acc ** (1.0 / p)
    spurs = fbm(wx, wy, 150.0, seed + 9, 4, ridged=True)
    H = base + high * (1.0 + C.get("spurs", 0.16) * spurs) + 1.1 * fbm(GX, GY, 130.0, seed + 11, 3) * smoothstep(30, 170, dl)

    for st in C.get("stamps", []):
        if st.get("stage", "pre") == "pre":
            H = apply_stamp(H, st, land)

    # Rivers: each a line from its source to its mouth with a bed that only falls. The valley is cut by a smooth
    # minimum against the river's own profile: bed, banks, a floor and the valley's sides.
    rivers = []
    for r in C.get("rivers", []):
        line_r = catmull([q[:2] for q in r["pts"]], False, 14)
        k = np.arange(len(line_r)) / 14.0
        cols = [np.interp(k, np.arange(len(r["pts"])), [q[c] for q in r["pts"]]) for c in range(2, 6)]
        rp, rs_axis = resample(np.column_stack([line_r] + cols), 4.0)
        rp[:, 2] = np.minimum.accumulate(rp[:, 2])
        d, rs = dist_field(rp[:, :2])
        bed, cw, fw, depth = (np.interp(rs, rs_axis, rp[:, c]) for c in range(2, 6))
        side = r.get("side", 0.34)
        water = bed + depth
        beyond = np.maximum(d - cw / 2, 0.0)
        floor = water + 0.5 + 0.015 * beyond
        valley = np.where(d <= cw / 2, bed + depth * 0.25 * (d / np.maximum(cw / 2, 0.5)) ** 2,
                          np.where(beyond < 3.0, bed + (floor - bed) * smoothstep(0, 3.0, beyond),
                                   floor + side * np.maximum(d - fw / 2, 0.0)))
        # The river's own end fades in over its first metres, so a source is a hollow, not a wall.
        reach = smoothstep(0.0, 60.0, rs) if not r.get("tributary") else 1.0
        H = np.where(reach > 0, smin(H, valley + (1 - reach) * 40.0, 3.0), H)
        rivers.append(dict(name=r.get("name", ""), line=rp, d=d, cw=cw, bed=bed, water=water, depth=depth, s=rs, dry=r.get("dry", False)))
    F["rivers"] = rivers

    # The shore: the inland ground brought down to the waterline over the shore type's ramp.
    ramp = smoothstep(0.0, 1.0, dl / W)
    H = H * ramp + 0.35 * smoothstep(0, 6, dl)

    # Gullies: water gathers down the steepest way and cuts where it gathers, so slopes carry drainage lines and
    # not a noise pattern. Two passes; beaches and built ground are left out later by the platforms.
    F["gully"] = np.zeros_like(H)
    for _ in range(C.get("gully_passes", 2)):
        # Routed over the ground with a little unevenness added, so a smooth slope's water winds and does not rule lines.
        cut = gullies(H + 0.9 * fbm(GX, GY, 46.0, seed + 71 + _, 3), land, C.get("gully", 2.2)) * smoothstep(20, 70, dl)
        H = H - cut
        F["gully"] += cut

    for st in C.get("stamps", []):
        if st.get("stage") == "post":
            H = apply_stamp(H, st, land)

    # Built ground: each district's frame is a level platform (or a gently tilted one) with an apron, then pads for
    # the squares and parks between frames, then the roads benched into the slope.
    F["natural"] = H.copy()
    built = np.zeros_like(H)
    for D in C.get("districts", []):
        sd, u, v = rect_sdf(GX, GY, D.origin, D.angle, D.U, D.V)
        plane = D.level + D.tilt[0] * (u - D.U / 2) + D.tilt[1] * (v - D.V / 2)
        m = smoothstep(D.apron, 0.0, sd)
        H = H + (plane - H) * m
        built = np.maximum(built, (sd <= 0) * 1.0)
    for pad in C.get("pads", []):
        poly = np.array(pad["poly"], float)
        inside = poly_mask(poly, AX, AX)
        d, _ = dist_field(np.vstack([poly, poly[:1]]))
        m = np.where(inside, 1.0, smoothstep(pad.get("apron", 12.0), 0.0, d))
        H = H + (pad["level"] - H) * m
        built = np.maximum(built, inside * 1.0)
    F["built"] = built
    soft = blur(H, 30.0)
    roadmask = np.zeros_like(H)
    F["road_soft"] = np.zeros_like(H)
    for road in C.get("roads", []):
        line_r = catmull(road["pts"], False, 12) if road.get("smooth", True) else np.array(road["pts"], float)
        rp, _ = resample(line_r, 4.0)
        d, _ = dist_field(rp)
        half = road_width(road["cls"]) / 2
        m = smoothstep(half + 7.0, half, d) * (1 - built)
        H = H + (soft - H) * m * land
        roadmask = np.maximum(roadmask, d <= half)
        if road["cls"] not in STREETS:
            F["road_soft"] = np.maximum(F["road_soft"], smoothstep(half + 2.0, half * 0.6, d) * (1 - built))
        road["line"] = rp
    F["roadmask"] = roadmask

    # Water: the channels cut into whatever stands beside them, so a quay or an embankment meets the water upright;
    # then harbour basins, and the sea bed beyond the shore.
    wet = np.zeros(H.shape, bool)
    for r in rivers:
        inside = r["d"] <= r["cw"] / 2 + 0.5
        bowl = r["bed"] + r["depth"] * 0.25 * np.clip(r["d"] / np.maximum(r["cw"] / 2, 0.5), 0, 1) ** 2
        H = np.where(inside, np.minimum(H, bowl), H)
        if not r["dry"]:
            wet |= inside
    k0, a = blur(at_coast("k0"), 40.0), blur(at_coast("a"), 40.0)
    seabed = -(k0 * (1 - np.exp(-ds / a)) + 0.022 * ds)
    H = np.where(land | wet, H, np.maximum(seabed, -16.0))
    for basin in C.get("basins", []):
        inside = poly_mask(np.array(basin["poly"], float), AX, AX)
        H = np.where(inside, np.minimum(H, -basin.get("depth", 4.5)), H)
        wet |= inside
    for rx, ry, rr, rh in C.get("rocks", []):
        t = np.clip(np.hypot(GX - rx, GY - ry) / rr, 0, 1)
        H = H + rh * (1 - t * t) ** 2 * (1.0 + 0.5 * fbm(GX, GY, 9.0, seed + 31, 3))
    F["wet"] = wet

    # Fine relief on natural ground only: a little everywhere, more on steep rock.
    natural = (1 - built) * (1 - roadmask) * (H > 0.6)
    gy, gx = np.gradient(H, STEP)
    slope = np.degrees(np.arctan(np.hypot(gx, gy)))
    H = H + natural * (0.22 * fbm(GX, GY, 21.0, seed + 41, 3) + smoothstep(26, 44, slope) * 0.9 * fbm(GX, GY, 11.0, seed + 43, 3, ridged=True))
    H = np.minimum(H, BUDGET - SEA_LEVEL - 1.0)

    gy, gx = np.gradient(H, STEP)
    F.update(H=H, land=land, dl=dl, ds=ds, slope=np.degrees(np.arctan(np.hypot(gx, gy))), gx=gx, gy=gy)
    return F


def road_width(cls):
    return STREETS[cls][0] if cls in STREETS else {"road": 5.0, "track": 2.0}[cls]


def gullies(H, land, strength):
    """How deep running water cuts each cell: the gathered flow (D8, no pit filling) times the slope it runs down."""
    n = H.shape[0]
    pad = np.pad(H, 1, mode="edge")
    best = np.zeros(H.shape)
    recv = np.full(H.shape, -1, np.int64)
    idx = np.arange(n * n).reshape(n, n)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            drop = (H - pad[1 + dy:1 + dy + n, 1 + dx:1 + dx + n]) / math.hypot(dx, dy)
            target = np.clip(idx + dy * n + dx, 0, n * n - 1)
            ok = drop > best
            if dy == -1:
                ok[0, :] = False
            if dy == 1:
                ok[-1, :] = False
            if dx == -1:
                ok[:, 0] = False
            if dx == 1:
                ok[:, -1] = False
            best = np.where(ok, drop, best)
            recv = np.where(ok, target, recv)
    flat_land = land.ravel()
    order = np.argsort(-H.ravel(), kind="stable")
    acc = np.where(flat_land, STEP * STEP, 0.0).tolist()
    recv_list = recv.ravel().tolist()
    for i in order.tolist():
        j = recv_list[i]
        if j >= 0 and flat_land[i]:
            acc[j] += acc[i]
    A = np.array(acc).reshape(n, n)
    slope = best / STEP
    cut = strength * (1 - np.exp(-A / 1000.0)) * np.clip(slope / 0.22, 0.15, 1.0) * land
    k = max(3, int(5.0 / STEP) | 1)
    return box_blur(box_blur(cut, k), k)


# ---------------------------------------------------------------------------------------------------- drawing

class Canvas:
    def __init__(self, scale=1.4, x0=0.0, y0=0.0, width=SIZE, height=SIZE):
        self.k, self.x0, self.y0 = scale, x0, y0
        self.w, self.h = int(width * scale), int(height * scale)
        self.img = np.zeros((self.h, self.w, 3), np.float32)
        self.X, self.Y = np.meshgrid(x0 + (np.arange(self.w) + 0.5) / scale, y0 + (np.arange(self.h) + 0.5) / scale)

    def _box(self, xs, ys, pad=0.0):
        i0 = max(int((min(xs) - pad - self.x0) * self.k), 0)
        i1 = min(int((max(xs) + pad - self.x0) * self.k) + 2, self.w)
        j0 = max(int((min(ys) - pad - self.y0) * self.k), 0)
        j1 = min(int((max(ys) + pad - self.y0) * self.k) + 2, self.h)
        return i0, i1, j0, j1

    def _blend(self, j0, j1, i0, i1, mask, colour, alpha):
        region = self.img[j0:j1, i0:i1]
        region[mask] = region[mask] * (1 - alpha) + np.array(colour, np.float32) * alpha

    def poly(self, pts, colour, alpha=1.0):
        pts = np.asarray(pts, float)
        i0, i1, j0, j1 = self._box(pts[:, 0], pts[:, 1])
        if i1 <= i0 or j1 <= j0:
            return
        mask = poly_mask(pts, self.X[0, i0:i1], self.Y[j0:j1, 0])
        self._blend(j0, j1, i0, i1, mask, colour, alpha)

    def line(self, pts, width, colour, alpha=1.0, widths=None):
        pts = np.asarray(pts, float)
        wmax = float(np.max(widths)) if widths is not None else width
        i0, i1, j0, j1 = self._box(pts[:, 0], pts[:, 1], wmax / 2 + 1)
        if i1 <= i0 or j1 <= j0:
            return
        d, s = dist_field(pts, self.X[j0:j1, i0:i1], self.Y[j0:j1, i0:i1])
        if widths is not None:
            seg = np.concatenate([[0.0], np.cumsum(np.hypot(np.diff(pts[:, 0]), np.diff(pts[:, 1])))])
            half = np.interp(s, seg, widths) / 2
        else:
            half = max(width / 2, 0.5 / self.k)
        self._blend(j0, j1, i0, i1, d <= half, colour, alpha)

    def ring(self, centre, radius, colour, width=1.5):
        i0, i1, j0, j1 = self._box([centre[0] - radius, centre[0] + radius], [centre[1] - radius, centre[1] + radius], width)
        d = np.abs(np.hypot(self.X[j0:j1, i0:i1] - centre[0], self.Y[j0:j1, i0:i1] - centre[1]) - radius)
        self._blend(j0, j1, i0, i1, d <= width / 2, colour, 1.0)


def terrain_image(F, C, canvas, zones=True):
    """The land as a map: tints by height, hill shading, contours every 5 m, shore, rock, forest, marsh and water."""
    H = bil(F["H"], canvas.X, canvas.Y)
    slope = bil(F["slope"], canvas.X, canvas.Y)
    gx, gy = bil(F["gx"], canvas.X, canvas.Y), bil(F["gy"], canvas.X, canvas.Y)
    dl = bil(F["dl"], canvas.X, canvas.Y)
    stops = [(0, (176, 190, 138)), (4, (158, 182, 122)), (12, (150, 170, 110)), (25, (160, 160, 104)),
             (40, (168, 150, 108)), (58, (176, 164, 140)), (76, (206, 200, 190))]
    hs = np.array([s[0] for s in stops], float)
    rgb = np.stack([np.interp(H, hs, [s[1][c] for s in stops]) for c in range(3)], -1)
    if zones:
        forest = bil(F["forest"], canvas.X, canvas.Y)
        grain = 0.86 + 0.28 * vnoise(canvas.X, canvas.Y, 7.0, 977)
        rgb = rgb * (1 - 0.78 * forest[..., None]) + np.array((52, 88, 58)) * 0.78 * (forest * grain)[..., None]
        farm = bil(F["farm"], canvas.X, canvas.Y)
        stripe = 0.85 + 0.15 * (np.floor((canvas.X + canvas.Y) / 6.0) % 2)
        rgb = rgb * (1 - 0.7 * farm[..., None]) + np.array(COL["farm"]) * 0.7 * (farm * stripe)[..., None]
        marsh = bil(F["marsh"], canvas.X, canvas.Y)
        rgb = rgb * (1 - 0.6 * marsh[..., None]) + np.array((120, 142, 110)) * 0.6 * marsh[..., None]
    beach = np.clip(bil(F["is_b"], canvas.X, canvas.Y) + 0.7 * bil(F["is_g"], canvas.X, canvas.Y), 0, 1)
    sand = beach * smoothstep(3.2, 0.8, H) * (dl > 0) * smoothstep(110, 60, dl)
    tone = np.where(bil(F["is_g"], canvas.X, canvas.Y)[..., None] > 0.5, np.array((176, 170, 156)), np.array((226, 210, 168)))
    rgb = rgb * (1 - sand[..., None]) + tone * sand[..., None]
    rock = smoothstep(30, 42, slope)
    rgb = rgb * (1 - rock[..., None]) + np.array((128, 122, 114)) * rock[..., None]
    shade = np.clip((0.55 * gx + 0.55 * gy) * 1.1 + 1.0, 0.62, 1.32)
    rgb = rgb * shade[..., None]
    depth = np.clip(-H, 0, 16)
    sea = np.stack([np.interp(depth, [0, 1.5, 5, 16], [168, 120, 78, 52]),
                    np.interp(depth, [0, 1.5, 5, 16], [206, 172, 132, 96]),
                    np.interp(depth, [0, 1.5, 5, 16], [206, 196, 176, 150])], -1)
    under = H < 0
    rgb[under] = sea[under]
    for r in F["rivers"]:
        if r["dry"]:
            continue
        d = bil(r["d"], canvas.X, canvas.Y)
        cw = bil(r["cw"], canvas.X, canvas.Y)
        rgb[(d <= np.maximum(cw / 2, 1.6)) & ~under] = COL["water"]
    level = np.floor(H / 5.0)
    edge = (level != np.roll(level, 1, 0)) | (level != np.roll(level, 1, 1))
    rgb[edge & (H > 0.2)] *= 0.72
    canvas.img = rgb.astype(np.float32)
    return canvas


def draw_city(C, F, canvas, plots_out=None, detail=True):
    """Roads, districts, blocks and plots, pads, walls, bridges, piers and the hamlets, over the land."""
    for road in C.get("roads", []):
        cls = road["cls"]
        if cls in STREETS:
            canvas.line(road["line"], STREETS[cls][0], COL["pave"])
            canvas.line(road["line"], STREETS[cls][1], COL["carriage"])
        else:
            canvas.line(road["line"], max(road_width(cls), 2.2), COL[cls])
    for pad in C.get("pads", []):
        canvas.poly(pad["poly"], COL[pad.get("kind", "square")])
    for D in C.get("districts", []):
        canvas.poly(D.frame(), COL["pave"])
        for cls, _, carriage in D.streets():
            canvas.poly(carriage, COL["carriage"])
        for i, j, u0, v0, bw, bd, spec in D.blocks():
            kind = spec.get("kind", "built")
            family = spec.get("family", D.family)
            wealth = spec.get("wealth", D.wealth)
            q = lambda a0, b0, a1, b1: D.quad(u0 + a0, v0 + b0, u0 + a1, v0 + b1)
            if kind == "void":
                canvas.poly(q(0, 0, bw, bd), COL["pave"])
            elif kind in ("park", "square", "market", "cemetery"):
                canvas.poly(q(0, 0, bw, bd), COL[kind])
            elif kind == "works":
                canvas.poly(q(0, 0, bw, bd), COL["yard_poor"])
                for box in spec.get("boxes", [(4, 4, bw - 4, bd * 0.55), (4, bd * 0.55 + 6, bw * 0.6, bd - 4)]):
                    canvas.poly(q(*box), COL["works"])
                    if plots_out is not None:
                        plots_out.append(dict(quad=q(*box), h=spec.get("h", 14.0), base=D.level, kind="works", district=D.name))
            else:
                canvas.poly(q(0, 0, bw, bd), COL["yard_" + {"rich": "rich", "mid": "mid", "poor": "poor"}[wealth]])
                if kind == "built":
                    for p in block_plots(bw, bd, family):
                        canvas.poly(q(*p), COL[family])
                        if detail:
                            canvas.line(q(*p) + [q(*p)[0]], 0.0, (60, 46, 40))
                        if plots_out is not None:
                            plots_out.append(dict(quad=q(*p), h=FAMILIES[family][0], base=D.level, kind=family, district=D.name))
            for single in spec.get("singles", []):
                name, a0, b0, w, d, h, kind_s = single
                canvas.poly(q(a0, b0, a0 + w, b0 + d), COL[kind_s])
                if plots_out is not None:
                    plots_out.append(dict(quad=q(a0, b0, a0 + w, b0 + d), h=h, base=D.level, kind=kind_s, district=D.name, name=name))
    for hamlet in C.get("hamlets", []):
        for quad in hamlet_quads(hamlet):
            canvas.poly(quad, COL["cottage"])
            if plots_out is not None:
                plots_out.append(dict(quad=quad, h=7.0, base=None, kind="cottage", district=hamlet["name"]))
    for wall in C.get("walls", []):
        canvas.line(wall, 2.4, COL["wall"])
    for b in C.get("bridges", []):
        canvas.line([b["a"], b["b"]], b.get("w", 14.0), COL["bridge"])
    for p in C.get("piers", []):
        canvas.line([p["a"], p["b"]], p.get("w", 6.0), COL["pier"])
    for pt in C.get("points", []):
        canvas.ring(pt["at"], 4.0, COL["mark"], 2.5)


def hamlet_quads(hm):
    """Cottages of 6 × 7 m (the kit's House_Farm) either side of a lane, in the hamlet's own frame."""
    c, s = math.cos(math.radians(hm["angle"])), math.sin(math.radians(hm["angle"]))
    out = []
    for k in range(hm["n"]):
        side = 1 if k % 2 == 0 else -1
        u = (k // 2) * hm.get("pitch", 24.0) + (5.0 if side < 0 else 0.0)
        v0 = side * 8.0 if side > 0 else -15.0
        corners = [(u, v0), (u + 6.0, v0), (u + 6.0, v0 + 7.0), (u, v0 + 7.0)]
        out.append([(hm["at"][0] + a * c - b * s, hm["at"][1] + a * s + b * c) for a, b in corners])
    return out


# ---------------------------------------------------------------------------------------------------- zones and checks

def zones(C, F):
    """Forest, farmland and marsh as soft fields: authored shapes first, then what the ground allows."""
    seed = C.get("seed", 1)
    H, dl, slope, land = F["H"], F["dl"], F["slope"], F["land"]
    near = np.zeros_like(H)
    for D in C.get("districts", []):
        sd, _, _ = rect_sdf(GX, GY, D.origin, D.angle, D.U, D.V)
        near = np.maximum(near, smoothstep(26.0, 0.0, sd))
    farm = np.zeros_like(H)
    for poly in C.get("farmland", []):
        farm = np.maximum(farm, poly_mask(catmull(poly, True, 10), AX, AX) * 1.0)
    farm *= (slope < 12) * (H > 1.0) * (1 - near)
    marsh = np.zeros_like(H)
    for poly in C.get("marsh", []):
        marsh = np.maximum(marsh, poly_mask(catmull(poly, True, 10), AX, AX) * 1.0)
    marsh = blur(marsh * (H > -0.3) * (H < 2.2), 8.0)
    bias = np.zeros_like(H)
    for bx, by, br, bv in C.get("forest_bias", []):
        bias += bv * smoothstep(br, br * 0.35, np.hypot(GX - bx, GY - by))
    cover = fbm(GX, GY, 170.0, seed + 61, 4) * 0.9 + bias + C.get("forest", 0.15)
    forest = smoothstep(-0.05, 0.22, cover) * land * (H > 1.6) * smoothstep(48, 34, slope) * smoothstep(14, 40, dl)
    forest *= (1 - farm) * (1 - near) * (1 - np.clip(marsh * 2, 0, 1)) * (1 - F["roadmask"])
    away = np.full_like(H, 1e9)
    for D in C.get("districts", []):
        away = np.minimum(away, rect_sdf(GX, GY, D.origin, D.angle, D.U, D.V)[0])
    for pad in C.get("pads", []):
        poly = np.array(pad["poly"], float)
        away = np.minimum(away, np.where(poly_mask(poly, AX, AX), 0.0, dist_field(np.vstack([poly, poly[:1]]))[0]))
    F.update(forest=blur(forest, 5.0), farm=farm, marsh=marsh, near=near, away=away)


def territories(C, F, apart=98.0, radius=25.0):
    """Where the beasts' territories fit (25 m round a den, 98 m apart): wild, standable ground away from people."""
    H, slope = F["H"], F["slope"]
    ok = F["land"] & (H > 1.5) & (slope < 16) & (F["dl"] > 40) & (F["away"] > 70.0) & (F["farm"] < 0.5) & (F["marsh"] < 0.3)
    step = max(1, int(round(10.0 / STEP)))
    cand = np.argwhere(ok[::step, ::step]) * step
    if len(cand) == 0:
        return []
    pts = np.stack([AX[cand[:, 1]], AX[cand[:, 0]]], 1)
    centre = np.mean([D.centre() for D in C["districts"]], axis=0) if C.get("districts") else np.array([500.0, 500.0])
    far = np.hypot(pts[:, 0] - centre[0], pts[:, 1] - centre[1])
    keep = far > 0.0
    for hm in C.get("hamlets", []):
        keep &= np.hypot(pts[:, 0] - hm["at"][0], pts[:, 1] - hm["at"][1]) > 90.0
    pts, far = pts[keep], far[keep]
    chosen = [pts[far.argmax()]]
    nearest = np.hypot(pts[:, 0] - chosen[0][0], pts[:, 1] - chosen[0][1])
    while True:
        k = nearest.argmax()
        if nearest[k] < apart:
            break
        chosen.append(pts[k])
        nearest = np.minimum(nearest, np.hypot(pts[:, 0] - pts[k][0], pts[:, 1] - pts[k][1]))
    return chosen


def quads_overlap(a, b):
    """Two convex quads overlap (separating axes)."""
    for quad in (a, b):
        for i in range(4):
            ex, ey = quad[(i + 1) % 4][0] - quad[i][0], quad[(i + 1) % 4][1] - quad[i][1]
            nx, ny = -ey, ex
            pa = [p[0] * nx + p[1] * ny for p in a]
            pb = [p[0] * nx + p[1] * ny for p in b]
            if max(pa) <= min(pb) + 1e-6 or max(pb) <= min(pa) + 1e-6:
                return False
    return True


def stats(C, F, plots, dens):
    H, land, slope = F["H"], F["land"], F["slope"]
    cell = STEP * STEP
    natural = land & (F["built"] < 0.5)
    lines = []
    add = lines.append
    add("concept %s: %s" % (C["key"], C["name"]))
    add("land %.0f m2 (%.0f %% of the terrain); coast %.0f m" % (land.sum() * cell, 100 * land.mean(),
        np.hypot(np.diff(F["coast"][:, 0]), np.diff(F["coast"][:, 1])).sum()))
    add("highest ground %.1f m over the sea; mean of the land %.1f m" % (H.max(), H[land].mean()))
    for lo, hi in ((0, 8), (8, 16), (16, 30), (30, 45), (45, 90)):
        add("  natural ground at %2d-%2d deg: %4.1f %%" % (lo, hi, 100 * ((slope >= lo) & (slope < hi) & natural).sum() / max(natural.sum(), 1)))
    for r in F["rivers"]:
        line = r["line"]
        add("river %s: %.0f m long, bed from %.1f to %.1f m, %.0f to %.0f m wide" % (
            r["name"], np.hypot(np.diff(line[:, 0]), np.diff(line[:, 1])).sum(), line[0, 2], line[-1, 2], line[:, 3].min(), line[:, 3].max()))
    area = sum(D.U * D.V for D in C["districts"])
    add("city: %d districts, frames %.0f m2, turns %s" % (len(C["districts"]), area, sorted({D.angle for D in C["districts"]})))
    kinds = {}
    for p in plots:
        kinds[p["kind"]] = kinds.get(p["kind"], 0) + 1
    add("buildings: %d (%s)" % (sum(v for k, v in kinds.items() if k != "cottage"), ", ".join("%s %d" % kv for kv in sorted(kinds.items()))))
    green = 0.0
    for D in C["districts"]:
        for i, j, u0, v0, bw, bd, spec in D.blocks():
            if spec.get("kind") in ("park", "square", "market", "cemetery"):
                green += bw * bd
    green += sum(abs(poly_area(p["poly"])) for p in C.get("pads", []))
    add("parks, squares and markets: %.0f m2, %.0f %% of the frames" % (green, 100 * green / max(area, 1)))
    frames = [(D.name, D.frame()) for D in C["districts"]]
    for a in range(len(frames)):
        for b in range(a + 1, len(frames)):
            if quads_overlap(frames[a][1], frames[b][1]):
                add("OVERLAP: %s and %s" % (frames[a][0], frames[b][0]))
    for D in C["districts"]:
        sd, _, _ = rect_sdf(GX, GY, D.origin, D.angle, D.U, D.V)
        inside = sd <= 0
        wet = (inside & F["wet"]).sum() * cell
        if wet > 30:
            add("WATER under %s: %.0f m2" % (D.name, wet))
        was = F["natural"][inside]
        add("  %s stands at %.1f m; the ground under it was %.1f m (%.1f to %.1f)" % (D.name, D.level, was.mean(), was.min(), was.max()))
    add("beast territories that fit (98 m apart): %d of 17" % len(dens))
    wild = natural & (F["farm"] < 0.5)
    add("wild land %.0f m2; forest %.0f m2; farmland %.0f m2; marsh %.0f m2" % (
        wild.sum() * cell, F["forest"].sum() * cell, F["farm"].sum() * cell, (F["marsh"] > 0.3).sum() * cell))
    return lines


def poly_area(poly):
    p = np.asarray(poly, float)
    return 0.5 * float(np.sum(p[:, 0] * np.roll(p[:, 1], -1) - np.roll(p[:, 0], -1) * p[:, 1]))


# ---------------------------------------------------------------------------------------------------- output

def svg_map(C, F, plots, dens, base_png):
    """The labelled plan: the land's picture under the city's lines and every name, 1 unit = 1 m."""
    def pts(q):
        return " ".join("%.1f,%.1f" % (x, y) for x, y in q)

    def rgb(key):
        return "rgb(%d,%d,%d)" % COL[key]

    o = ['<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 1000 1000" font-family="Segoe UI, Arial, sans-serif">',
         '<!-- Veilmouth island concept %s: %s. Written by tools/slice/island/build.py. 1 unit = 1 m; world X = px - 500, world Z = 500 - py; north is up. -->' % (C["key"], C["name"]),
         '<image x="0" y="0" width="1000" height="1000" xlink:href="data:image/png;base64,%s"/>' % base64.b64encode(base_png).decode()]
    for road in C.get("roads", []):
        cls = road["cls"]
        d = "M" + " L".join("%.1f %.1f" % (x, y) for x, y in road["line"][:, :2])
        if cls in STREETS:
            o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%.1f"/>' % (d, rgb("pave"), STREETS[cls][0]))
            o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%.1f"/>' % (d, rgb("carriage"), STREETS[cls][1]))
        else:
            o.append('<path d="%s" fill="none" stroke="%s" stroke-width="%.1f"/>' % (d, rgb(cls), max(road_width(cls), 2.2)))
    for pad in C.get("pads", []):
        o.append('<polygon points="%s" fill="%s" stroke="#6b655c" stroke-width="0.3"/>' % (pts(pad["poly"]), rgb(pad.get("kind", "square"))))
    for D in C.get("districts", []):
        o.append('<polygon points="%s" fill="%s"/>' % (pts(D.frame()), rgb("pave")))
        for cls, _, carriage in D.streets():
            o.append('<polygon points="%s" fill="%s"/>' % (pts(carriage), rgb("carriage")))
        for i, j, u0, v0, bw, bd, spec in D.blocks():
            kind = spec.get("kind", "built")
            wealth = spec.get("wealth", D.wealth)
            fill = rgb(kind) if kind in ("park", "square", "market", "cemetery") else rgb("pave") if kind == "void" else rgb("yard_poor" if kind == "works" else "yard_" + wealth)
            o.append('<polygon points="%s" fill="%s" stroke="#6b655c" stroke-width="0.25"/>' % (pts(D.quad(u0, v0, u0 + bw, v0 + bd)), fill))
    for p in plots:
        o.append('<polygon points="%s" fill="%s" stroke="#3c2e28" stroke-width="0.25"/>' % (pts(p["quad"]), rgb(p["kind"])))
    for wall in C.get("walls", []):
        o.append('<polyline points="%s" fill="none" stroke="%s" stroke-width="2.4"/>' % (pts(wall), rgb("wall")))
    for b in C.get("bridges", []):
        o.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="%s" stroke-width="%.1f"/>' % (*b["a"], *b["b"], rgb("bridge"), b.get("w", 14.0)))
    for p in C.get("piers", []):
        o.append('<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="%s" stroke-width="%.1f"/>' % (*p["a"], *p["b"], rgb("pier"), p.get("w", 6.0)))
    for x, y in dens:
        o.append('<circle cx="%.1f" cy="%.1f" r="25" fill="none" stroke="#7a3b12" stroke-width="0.8" stroke-dasharray="4 3"/>' % (x, y))
    for pt in C.get("points", []):
        o.append('<circle cx="%.1f" cy="%.1f" r="4" fill="none" stroke="#c82828" stroke-width="1.6"/>' % tuple(pt["at"]))
        o.append('<text x="%.1f" y="%.1f" font-size="7" fill="#7a1010">%s</text>' % (pt["at"][0] + 6, pt["at"][1] + 2.5, pt["name"]))
    for D in C.get("districts", []):
        cx, cy = D.centre()
        o.append('<text x="%.1f" y="%.1f" font-size="8" font-weight="600" text-anchor="middle" fill="#1e1a16" stroke="#ffffff" stroke-width="2" paint-order="stroke">%s</text>' % (cx, cy, D.label))
    for text, x, y, *size in C.get("labels", []):
        o.append('<text x="%.1f" y="%.1f" font-size="%s" font-style="italic" text-anchor="middle" fill="#1e2a36" stroke="#ffffff" stroke-width="1.8" paint-order="stroke">%s</text>' % (x, y, size[0] if size else 8, text))
    o.append('<g font-size="9" fill="#10202c"><line x1="40" y1="962" x2="140" y2="962" stroke="#10202c" stroke-width="2"/><text x="40" y="978">0</text><text x="128" y="978">100 m</text>'
             '<text x="40" y="34" font-size="13" font-weight="600">%s. %s</text><text x="950" y="52" font-size="12" text-anchor="middle">N</text>'
             '<path d="M950 110 L950 60 M943 72 L950 58 L957 72" stroke="#10202c" stroke-width="2" fill="none"/></g>' % (C["key"], C["name"]))
    o.append("</svg>")
    return "\n".join(o)


def build(C, out, log=print):
    os.makedirs(out, exist_ok=True)
    F = build_land(C, log)
    zones(C, F)
    dens = territories(C, F)

    plots = []
    full = terrain_image(F, C, Canvas(1.4))
    base = terrain_image(F, C, Canvas(1.0))
    draw_city(C, F, full, plots)
    for x, y in dens:
        full.ring((x, y), 25.0, (122, 59, 18), 1.6)
    write_png_rgb(os.path.join(out, "map.png"), full.img)

    elev = terrain_image(F, C, Canvas(1.0), zones=False)
    write_png_rgb(os.path.join(out, "elevation.png"), elev.img)

    # The city alone at 3 px to the metre, for the blocks and the plots.
    xs = [p[0] for D in C["districts"] for p in D.frame()]
    ys = [p[1] for D in C["districts"] for p in D.frame()]
    x0, y0 = max(min(xs) - 40, 0), max(min(ys) - 40, 0)
    w, h = min(max(xs) + 40, SIZE) - x0, min(max(ys) + 40, SIZE) - y0
    city = terrain_image(F, C, Canvas(min(3.0, 1500.0 / max(w, h)), x0, y0, w, h))
    draw_city(C, F, city)
    write_png_rgb(os.path.join(out, "city.png"), city.img)

    with open(os.path.join(out, "map.svg"), "w", encoding="utf-8") as fh:
        fh.write(svg_map(C, F, plots, dens, png_rgb(base.img)))

    # The preliminary height map: 1025 px, 16 bits, row 0 the north edge, 0 = the terrain's floor, 65535 = 100 m.
    fine = np.linspace(0.0, SIZE, HEIGHTMAP)
    FX, FY = np.meshgrid(fine, fine)
    H16 = (bil(F["H"], FX, FY) + SEA_LEVEL) / BUDGET * 65535.0
    write_png_gray16(os.path.join(out, "heightmap_1025.png"), H16)

    # For the 3D preview: the heights, the plan as a texture, and every building as a box on its ground.
    texture = terrain_image(F, C, Canvas(2.0))
    draw_city(C, F, texture, detail=False)
    write_png_rgb(os.path.join(out, "texture.png"), texture.img)
    boxes = []
    for p in plots:
        base_z = p["base"] if p["base"] is not None else float(bil(F["H"], np.array([[np.mean([q[0] for q in p["quad"]])]]),
                                                                  np.array([[np.mean([q[1] for q in p["quad"]])]]))[0, 0])
        boxes.append([c for q in p["quad"] for c in q] + [base_z, p["h"], {"works": 2, "civic": 1, "church": 1}.get(p["kind"], 0)])
    every = max(1, int(round(7.8 / STEP)))
    trees = np.argwhere((F["forest"][::every, ::every] > 0.55)) * every
    np.savez(os.path.join(out, "preview.npz"), H=F["H"][::(N - 1) // 512, ::(N - 1) // 512].astype(np.float32), boxes=np.array(boxes, np.float32),
             trees=np.stack([AX[trees[:, 1]], AX[trees[:, 0]], F["H"][trees[:, 0], trees[:, 1]]], 1).astype(np.float32),
             views=np.array([v["eye"] + v["look"] + (v.get("lens", 35.0),) for v in C.get("views", [])], np.float32))
    with open(os.path.join(out, "views.json"), "w", encoding="utf-8") as fh:
        json.dump([v["name"] for v in C.get("views", [])], fh)

    lines = stats(C, F, plots, dens)
    with open(os.path.join(out, "stats.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(lines) + "\n")
    for line in lines:
        log(line)
    return F
