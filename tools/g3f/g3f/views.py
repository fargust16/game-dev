"""G3f view kit (spec §10; phase plan §9.7): plan-anchored cameras (identical for every candidate of a variant), the
camera validity pre-check, the S-PLAN image, blind contact sheets and the sealed key. Rendering of the oblique and
player views is done by Unity (TerrainGenG3fDump, step views) with G1's presentation; this module writes its camera
list and composes its PNGs."""
import hashlib
import math
import os
import struct
import zlib

import numpy as np

from .lattice import C, SEA, hillshade, north_up, write_png, raster_polyline4
from . import synth as S

OBLIQUE = (1920, 1080)
PLAYER = (1280, 720)
STRIP = (640, 360)


# ----------------------------------------------------------------------------- PNG decoding (for sheets)

def read_png(path):
    """Minimal decoder for 8-bit RGB / RGBA non-interlaced PNGs (filters 0-4). Returns uint8 (H, W, 3)."""
    data = open(path, "rb").read()
    assert data[:8] == b"\x89PNG\r\n\x1a\n"
    pos = 8
    idat = b""
    w = h = ct = 0
    while pos < len(data):
        ln = struct.unpack(">I", data[pos:pos + 4])[0]
        typ = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + ln]
        if typ == b"IHDR":
            w, h, bd, ct, _, _, il = struct.unpack(">IIBBBBB", body)
            assert bd == 8 and il == 0 and ct in (2, 6)
        elif typ == b"IDAT":
            idat += body
        pos += 12 + ln
    bpp = 3 if ct == 2 else 4
    raw = zlib.decompress(idat)
    stride = w * bpp
    out = np.zeros((h, stride), np.uint8)
    prev = np.zeros(stride, np.int32)
    p = 0
    for y in range(h):
        f = raw[p]
        line = np.frombuffer(raw[p + 1:p + 1 + stride], np.uint8).astype(np.int32)
        p += 1 + stride
        if f == 0:
            cur = line
        elif f == 2:
            cur = (line + prev) & 255
        else:
            cur = np.zeros(stride, np.int32)
            for i in range(stride):
                a = cur[i - bpp] if i >= bpp else 0
                b = prev[i]
                c = prev[i - bpp] if i >= bpp else 0
                if f == 1:
                    v = line[i] + a
                elif f == 3:
                    v = line[i] + ((a + b) >> 1)
                else:
                    pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                    pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                    v = line[i] + pr
                cur[i] = v & 255
        out[y] = cur
        prev = cur
    return out.reshape(h, w, bpp)[:, :, :3]


# ----------------------------------------------------------------------------- cameras

def _ground(h, p):
    x, z = p[0] / C, p[1] / C
    x0, z0 = int(math.floor(x)), int(math.floor(z))
    fx, fz = x - x0, z - z0
    n0, n1 = h.shape
    x0 = min(max(x0, 0), n1 - 2)
    z0 = min(max(z0, 0), n0 - 2)
    a, b, c_, d = h[z0, x0], h[z0, x0 + 1], h[z0 + 1, x0], h[z0 + 1, x0 + 1]
    return float((a * (1 - fx) + b * fx) * (1 - fz) + (c_ * (1 - fx) + d * fx) * fz)


def _outward(contour, v):
    """Unit vector pointing to the sea at shore vertex v (minus the gradient of the distance to the sea)."""
    z, x = v[1], v[0]
    g = contour.dsea[z - 3:z + 4, x - 3:x + 4]
    gz, gx = np.gradient(g)
    nx, nz = -float(gx[3, 3]), -float(gz[3, 3])
    L = math.hypot(nx, nz) or 1.0
    return nx / L, nz / L


def cameras(plan, contour):
    """The camera records of a variant (spec §10.2). World metres; eye either absolute ('abs') or above ground."""
    R = plan["raster"]
    sec = plan["sector"]
    w = lambda v: (v[0] * C, v[1] * C)
    zs, xs = np.nonzero(sec)
    cen = (xs.mean() * C, zs.mean() * C)
    req = math.sqrt(sec.sum() * C * C / math.pi)
    nodes = plan["nodes"]
    cams = []

    def add(cid, scale, eye, emode, evalue, target, tabove, fov, size, shadow, contour_w, near=0.5, placement=""):
        cams.append({"id": cid, "scale": scale, "eye": [float(eye[0]), float(eye[1])], "eye_mode": emode, "eye_value": float(evalue),
                     "target": [float(target[0]), float(target[1])], "target_above": float(tabove), "fov": fov, "near": near,
                     "far": 10000.0, "width": size[0], "height": size[1], "shadow": shadow, "contour": contour_w,
                     "placement": placement})

    # overview from offshore along the sector's seaward normal (centroid -> away from the island centroid)
    isl = np.nonzero(contour.land)
    icen = (isl[1].mean() * C, isl[0].mean() * C)
    dx, dz = cen[0] - icen[0], cen[1] - icen[1]
    L = math.hypot(dx, dz) or 1.0
    add("S-OVR", "overview", (cen[0] + dx / L * 2.2 * req, cen[1] + dz / L * 2.2 * req), "abs", 900.0, cen, 0.0, 40.0, OBLIQUE, 6000.0, 3.0,
        placement="2.2 x equal-area radius offshore along the sector's seaward direction, 900 m, looking at the sector centroid")
    # A-MTN: 200 m offshore of the sector coast farthest from M1, 350 m above the sea, at M1
    m1w = w(nodes["M1"])
    shore = contour.shore4 & sec
    sz, sx = np.nonzero(shore)
    k = int(np.argmax(np.hypot(sx * C - m1w[0], sz * C - m1w[1])))
    far = (int(sx[k]), int(sz[k]))
    o = _outward(contour, far)
    fw = w(far)
    add("A-MTN", "regional", (fw[0] + o[0] * 200, fw[1] + o[1] * 200), "abs", SEA + 350.0, m1w, 0.0, 45.0, OBLIQUE, 5000.0, 2.0,
        placement="200 m offshore of the sector coast farthest from the main culmination, 350 m above the sea, at the main culmination")
    # A-SAD: 500 m along route R1 from the pass, 150 m above ground, at the pass
    s1w = w(nodes["S1"])
    r1 = [w(v) for v in plan["routes"]["R1"]["poly"]]
    k = min(range(len(r1)), key=lambda i: math.hypot(r1[i][0] - s1w[0], r1[i][1] - s1w[1]))
    other = r1[k - 1] if k > 0 else r1[k + 1]
    ddx, ddz = other[0] - s1w[0], other[1] - s1w[1]
    L = math.hypot(ddx, ddz) or 1.0
    add("A-SAD", "regional", (s1w[0] + ddx / L * 500, s1w[1] + ddz / L * 500), "above", 150.0, s1w, 0.0, 50.0, OBLIQUE, 5000.0, 2.0,
        placement="500 m from the declared pass along route R1, 150 m above ground, looking at the pass")
    # plateau views
    pl = plan["plateau"]
    pc = pl["centre_world"]
    jb = pl["joint_bearing"]
    eb = (jb + 180.0) % 360.0
    rim = (pc[0] + math.cos(math.radians(eb)) * pl["r_top_m"], pc[1] + math.sin(math.radians(eb)) * pl["r_top_m"])
    add("A-PLT-E", "regional", (rim[0] + math.cos(math.radians(eb)) * 400, rim[1] + math.sin(math.radians(eb)) * 400), "above",
        400.0 * math.tan(math.radians(35.0)), pc, 0.0, 50.0, OBLIQUE, 5000.0, 2.0,
        placement="400 m outside the middle of the escarpment (opposite the joint), 35 deg down, looking at the top centre")
    jr = (pc[0] + math.cos(math.radians(jb)) * (pl["r_top_m"] + 250), pc[1] + math.sin(math.radians(jb)) * (pl["r_top_m"] + 250))
    add("A-PLT-J", "regional", jr, "above", 80.0, pc, 0.0, 50.0, OBLIQUE, 4000.0, 2.0,
        placement="250 m beyond the joint on the ridge side, 80 m above ground, looking at the top centre")
    # A-CONF: 300 m downstream of the trunk's first confluence, 120 m above ground
    trunk = plan["axes"]["A_trunk"]["poly"]
    t1 = plan["axes"]["A_t1"]["poly"][-1]
    cw = w(t1)
    kk = min(range(len(trunk)), key=lambda i: math.hypot(trunk[i][0] - t1[0], trunk[i][1] - t1[1]))
    dn = w(trunk[min(kk + 1, len(trunk) - 1)])
    ddx, ddz = dn[0] - cw[0], dn[1] - cw[1]
    L = math.hypot(ddx, ddz) or 1.0
    add("A-CONF", "regional", (cw[0] + ddx / L * 300, cw[1] + ddz / L * 300), "above", 120.0, cw, 0.0, 50.0, OBLIQUE, 4000.0, 2.0,
        placement="300 m downstream of the trunk's first confluence, 120 m above ground, looking at it")
    # A-OUT, A-CLF
    o1 = plan["outlets"]["O1"]
    ow = w(o1)
    oo = _outward(contour, o1)
    add("A-OUT", "regional", (ow[0] + oo[0] * 350, ow[1] + oo[1] * 350), "abs", SEA + 60.0, (ow[0] - oo[0] * 300, ow[1] - oo[1] * 300), 0.0,
        50.0, OBLIQUE, 4000.0, 2.0, placement="350 m offshore of outlet O1, 60 m above the sea, looking 300 m inland")
    hv = nodes["H"]
    hw = w(hv)
    ho = _outward(contour, hv)
    add("A-CLF", "regional", (hw[0] + ho[0] * 250, hw[1] + ho[1] * 250), "abs", SEA + 40.0, (hw[0] - ho[0] * 40, hw[1] - ho[1] * 40), 0.0,
        50.0, OBLIQUE, 4000.0, 2.0, placement="250 m offshore of the cliff headland, 40 m above the sea, looking at it")
    # player views, eye 1.7 m
    rc = w(plan["reserve"]["centre"])
    add("P-SITE", "player", rc, "above", 1.7, m1w, 1.7, 60.0, PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m at the settlement reserve centre, looking at the main culmination")
    add("P-SAD", "player", s1w, "above", 1.7, w(nodes["M2"]), 1.7, 60.0, PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m at the declared pass, looking at the secondary culmination")
    if pl.get("landing_world"):
        lw = pl["landing_world"]
        add("P-RAMP", "player", lw, "above", 1.7, pc, 1.7, 60.0, PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m at the ramp landing, looking at the plateau top centre")
    edge = (pc[0] + math.cos(math.radians(eb)) * (pl["r_top_m"] - 5), pc[1] + math.sin(math.radians(eb)) * (pl["r_top_m"] - 5))
    add("P-TOP", "player", edge, "above", 1.7, (edge[0] + math.cos(math.radians(eb)) * 300, edge[1] + math.sin(math.radians(eb)) * 300), 0.0, 60.0,
        PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m at the plateau edge opposite the joint, looking out and down")
    tm = w(trunk[len(trunk) // 2])
    add("P-VAL", "player", tm, "above", 1.7, w(trunk[0]), 1.7, 60.0, PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m on the trunk floor halfway, looking upstream")
    o3 = plan["outlets"]["O3"]
    o3w = w(o3)
    o3o = _outward(contour, o3)
    add("P-COAST", "player", (o3w[0] - o3o[0] * 15, o3w[1] - o3o[1] * 15), "above", 1.7, (o3w[0] - o3o[0] * 400, o3w[1] - o3o[1] * 400), 1.7,
        60.0, PLAYER, 1500.0, 0.5, 0.1, "eye 1.7 m on the low shore at outlet O3, looking inland")
    # route strips: 8 eye-level frames every ~60 m along R1 (site -> pass) and R2 (site -> plateau top)
    for rn, tag in (("R1", "R1"), ("R2", "R2")):
        poly = [w(v) for v in plan["routes"][rn]["poly"]]
        if rn == "R1":
            ks = min(range(len(poly)), key=lambda i: math.hypot(poly[i][0] - s1w[0], poly[i][1] - s1w[1]))
            poly = poly[:ks + 1]
        pts = _along(poly, 60.0, 8)
        for j in range(len(pts)):
            tgt = pts[j + 1] if j + 1 < len(pts) else poly[-1]
            add(f"{tag}-{j}", "player", pts[j], "above", 1.7, tgt, 1.7, 60.0, STRIP, 1500.0, 0.5, 0.1, f"route {rn} frame {j}")
    return cams


def _along(poly, step, count):
    out = []
    acc = 0.0
    segs = list(zip(poly[:-1], poly[1:]))
    total = sum(math.hypot(b[0] - a[0], b[1] - a[1]) for a, b in segs)
    step = min(step, total / max(1, count))
    for k in range(count):
        s = k * step
        acc = 0.0
        for a, b in segs:
            L = math.hypot(b[0] - a[0], b[1] - a[1])
            if acc + L >= s and L > 0:
                t = (s - acc) / L
                out.append((a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t))
                break
            acc += L
    return out


def near_fraction(h, cam):
    """Camera validity pre-check: the share of a 48 x 27 grid of view rays that meet the terrain within 3 m."""
    if cam["scale"] != "player":
        return 0.0
    eye = (cam["eye"][0], cam["eye"][1])
    ey = (cam["eye_value"] if cam["eye_mode"] == "abs" else _ground(h, eye) + cam["eye_value"])
    tgt = cam["target"]
    ty = _ground(h, tgt) + cam["target_above"]
    f = np.array([tgt[0] - eye[0], ty - ey, tgt[1] - eye[1]])
    f /= np.linalg.norm(f)
    right = np.cross(f, [0, 1, 0])
    right /= np.linalg.norm(right) or 1.0
    up = np.cross(right, f)
    th = math.tan(math.radians(cam["fov"]) / 2)
    aspect = cam["width"] / cam["height"]
    hits = 0
    total = 0
    for j in range(27):
        for i in range(48):
            u = ((i + 0.5) / 48 * 2 - 1) * th * aspect
            v = ((j + 0.5) / 27 * 2 - 1) * th
            d = f + u * right + v * up
            d /= np.linalg.norm(d)
            total += 1
            for t in np.arange(0.25, 3.0001, 0.25):
                p = (eye[0] + d[0] * t, ey + d[1] * t, eye[1] + d[2] * t)
                if p[1] <= _ground(h, (p[0], p[2])):
                    hits += 1
                    break
    return hits / total


def camera_list(plan, contour, lattices):
    """Cameras plus a '+10 m' variant of every player camera flagged (> 30 % of rays within 3 m) on any lattice."""
    cams = cameras(plan, contour)
    flagged = {}
    for c in cams:
        fr = max(near_fraction(h, c) for h in lattices)
        if fr > 0.30:
            flagged[c["id"]] = fr
    extra = []
    for c in cams:
        if c["id"] in flagged:
            e = dict(c)
            e["id"] = c["id"] + "+10"
            e["eye_value"] = c["eye_value"] + 10.0
            e["placement"] = c["placement"] + " (+10 m: validity flag)"
            extra.append(e)
    return cams + extra, flagged


def write_unity_list(path, lattices, cams):
    """The text list TerrainGenG3fDump's views step reads."""
    lines = [f"L|{lab}|{p}" for lab, p in lattices]
    for c in cams:
        lines.append("C|" + "|".join(str(v) for v in [c["id"], c["fov"], c["eye"][0], c["eye"][1], c["eye_mode"], c["eye_value"],
                                                      c["target"][0], c["target"][1], c["target_above"], c["near"], c["far"],
                                                      c["width"], c["height"], c["shadow"], c["contour"]]))
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")


# ----------------------------------------------------------------------------- S-PLAN and sheets

def plan_image(plan, lat, path, overlay=True):
    """S-PLAN: top-down hillshade of the 513 x 513 window (2x), with the plan overlaid when overlay is True."""
    x0, z0 = plan_window(plan)
    W = 513
    h = np.where(np.isnan(lat), 10.0, lat)[z0:z0 + W, x0:x0 + W]
    hs = hillshade(h)
    land = h > SEA
    img = np.zeros((W, W, 3), np.uint8)
    img[land] = (np.array([200, 196, 186]) * (0.35 + 0.65 * hs[land, None])).astype(np.uint8)
    img[~land] = (40, 70, 120)
    if overlay:
        R = plan["raster"]
        sub = (slice(z0, z0 + W), slice(x0, x0 + W))
        img[(R.crest[sub] >= 0)] = (255, 255, 255)
        from .checks import axis_sequence
        for a in plan["axes"].values():
            for x, z in axis_sequence(a["poly"]):
                if 0 <= z - z0 < W and 0 <= x - x0 < W:
                    img[z - z0, x - x0] = (40, 90, 255)
        zone = R.zone[sub]
        img[zone == S.Z_TOP] = (img[zone == S.Z_TOP] * 0.5 + np.array([240, 220, 80]) * 0.5).astype(np.uint8)
        img[zone == S.Z_RAMP] = (img[zone == S.Z_RAMP] * 0.5 + np.array([250, 150, 30]) * 0.5).astype(np.uint8)
        rm = plan["reserve"]["mask"][sub]
        edge = rm & ~(np.roll(rm, 1, 0) & np.roll(rm, -1, 0) & np.roll(rm, 1, 1) & np.roll(rm, -1, 1))
        img[edge] = (255, 0, 255)
        for r in plan["routes"].values():
            for x, z in raster_polyline4(r["poly"]):
                if 0 <= z - z0 < W and 0 <= x - x0 < W:
                    img[z - z0, x - x0] = (255, 40, 40)
    write_png(path, np.repeat(np.repeat(north_up(img), 2, 0), 2, 1))


def plan_window(plan):
    return 111, 420


def sealed_order(seed, cls):
    """The blind assignment of C0, UF and RF to panels X, Y, Z (stream research.g3f.views): a permutation from SHA-256."""
    perms = [("C0", "UF", "RF"), ("C0", "RF", "UF"), ("UF", "C0", "RF"), ("UF", "RF", "C0"), ("RF", "C0", "UF"), ("RF", "UF", "C0")]
    k = int.from_bytes(hashlib.sha256(f"research.g3f.views:{seed}:{cls}".encode()).digest()[:8], "little") % 6
    return dict(zip(("X", "Y", "Z"), perms[k]))


def sheet(path, panels, labels, gap=8):
    """A 2 x 2 contact sheet: X | Y over Z | reference; panels are PNG paths (None leaves a blank cell)."""
    imgs = [read_png(p) if p and os.path.exists(p) else None for p in panels]
    hh = max(i.shape[0] for i in imgs if i is not None)
    ww = max(i.shape[1] for i in imgs if i is not None)
    out = np.full((2 * hh + gap, 2 * ww + gap, 3), 255, np.uint8)
    for k, im in enumerate(imgs):
        if im is None:
            continue
        oy, ox = (k // 2) * (hh + gap), (k % 2) * (ww + gap)
        out[oy:oy + im.shape[0], ox:ox + im.shape[1]] = im
        _label(out, ox + 6, oy + 6, labels[k])
    write_png(path, out)


_FONT = {
    "X": ["10001", "01010", "00100", "01010", "10001"], "Y": ["10001", "01010", "00100", "00100", "00100"],
    "Z": ["11111", "00010", "00100", "01000", "11111"], "H": ["10001", "10001", "11111", "10001", "10001"],
    "4": ["00010", "00110", "01010", "11111", "00010"], "A": ["01110", "10001", "11111", "10001", "10001"],
}


def _label(img, x, y, text, s=6):
    """Draws a short label in a 5 x 5 block font, white on black, scale s."""
    w = (len(text) * 6 + 1) * s
    img[y:y + 7 * s, x:x + w] = 0
    for k, ch in enumerate(text):
        g = _FONT.get(ch)
        if not g:
            continue
        for r, row in enumerate(g):
            for c_, bit in enumerate(row):
                if bit == "1":
                    yy, xx = y + (r + 1) * s, x + (k * 6 + c_ + 1) * s
                    img[yy:yy + s, xx:xx + s] = 255
