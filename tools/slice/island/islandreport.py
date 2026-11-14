"""How natural the island's relief is, by measurement: python tools/slice/island/islandreport.py <a|b|c> [tag]

Writes unity/Documentation/Terrain/Polish/Geography/<tag>/: report.txt and the maps it speaks of (hillshade with the
drainage lines, slope, what the stamps alone add, closed hollows). Nothing here changes the concept; it only reads
the height field build_land makes. Measures, each with its unit:
  hypsometry   the share of dry land in each 5 m band, and the hypsometric integral;
  slope        the share of dry land in each band of degrees;
  stamps       what the StampIT stamps add to the relief: built with and without them;
  drainage     priority-flood over the dry land, D8 on the filled ground: catchments by outlet, closed hollows,
               stream length, and how deep the ground is cut where the water gathers;
  roughness    the structure function's exponent between lags, the scales at which the ground has form;
  crests       the profile along each authored ridge: its summits and saddles;
  coast        length, shoreline development, box-count dimension, shore kinds.
"""
import copy
import heapq
import importlib
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import islandgen as g  # noqa: E402


def flood(H, dry):
    """Priority-flood from every cell that is not dry land: the filled ground, with a hair of fall so that it drains."""
    n = H.shape[0]
    filled = np.where(dry, np.inf, H).ravel().tolist()
    h = H.ravel().tolist()
    d = dry.ravel().tolist()
    heap, seen = [], bytearray(n * n)
    for i in range(n * n):
        if not d[i]:
            seen[i] = 1
            y, x = divmod(i, n)
            for j in ((i - n) if y else -1, (i + n) if y < n - 1 else -1, (i - 1) if x else -1, (i + 1) if x < n - 1 else -1):
                if j >= 0 and d[j] and not seen[j]:
                    seen[j] = 1
                    filled[j] = max(h[j], 0.0)
                    heap.append((filled[j], j))
    heapq.heapify(heap)
    eps = 1e-4
    while heap:
        z, i = heapq.heappop(heap)
        y, x = divmod(i, n)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                yy, xx = y + dy, x + dx
                if (dx or dy) and 0 <= yy < n and 0 <= xx < n:
                    j = yy * n + xx
                    if not seen[j]:
                        seen[j] = 1
                        filled[j] = max(h[j], z + eps)
                        heapq.heappush(heap, (filled[j], j))
    out = np.array(filled).reshape(n, n)
    return np.where(dry, out, H)


def receivers(Z, dry):
    n = Z.shape[0]
    pad = np.pad(Z, 1, mode="edge")
    best = np.zeros(Z.shape)
    recv = np.full(Z.shape, -1, np.int64)
    idx = np.arange(n * n).reshape(n, n)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == 0 and dy == 0:
                continue
            drop = (Z - pad[1 + dy:1 + dy + n, 1 + dx:1 + dx + n]) / math.hypot(dx, dy)
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
            recv = np.where(ok, idx + dy * n + dx, recv)
    return np.where(dry, recv, -1)


def drainage(H, dry):
    """Filled ground, gathered area (m2) and each dry cell's outlet (the first wet cell its water reaches)."""
    Z = flood(H, dry)
    recv = receivers(Z, dry)
    order = np.argsort(-Z.ravel(), kind="stable").tolist()
    d = dry.ravel().tolist()
    r = recv.ravel().tolist()
    acc = np.where(dry, g.STEP * g.STEP, 0.0).ravel().tolist()
    for i in order:
        if d[i] and r[i] >= 0:
            acc[r[i]] += acc[i]
    outlet = [-1] * len(r)
    for i in reversed(order):
        if d[i]:
            j = r[i]
            outlet[i] = -1 if j < 0 else (j if not d[j] else outlet[j])
    n = H.shape[0]
    return Z, np.array(acc).reshape(n, n), np.array(outlet).reshape(n, n), recv


def hillshade(H, az=315.0, alt=42.0, exaggerate=2.2):
    gy, gx = np.gradient(H * exaggerate, g.STEP)
    slope = np.arctan(np.hypot(gx, gy))
    aspect = np.arctan2(gy, -gx)
    a, e = math.radians(az), math.radians(alt)
    return np.clip(np.sin(e) * np.cos(slope) + np.cos(e) * np.sin(slope) * np.cos(a - math.pi / 2 - aspect), 0, 1)


def structure(H, mask, lags):
    out = []
    for lag in lags:
        k = max(1, int(round(lag / g.STEP)))
        dx = (H[:, k:] - H[:, :-k])[mask[:, k:] & mask[:, :-k]]
        dy = (H[k:, :] - H[:-k, :])[mask[k:, :] & mask[:-k, :]]
        out.append(float(np.sqrt((np.concatenate([dx, dy]) ** 2).mean())))
    return out


def upscale(img, k=2):
    return np.repeat(np.repeat(img, k, 0), k, 1)


def main():
    key = sys.argv[1].lower()
    tag = sys.argv[2] if len(sys.argv) > 2 else "current"
    C = importlib.import_module("concept_" + key).CONCEPT
    if tag == "before":
        # The concept as it stood before the polish of 2026-11-14: no erosion, the coast's line by its sines alone.
        C = {k: v for k, v in C.items() if k not in ("erosion", "coast_noise", "coast_waves", "bench", "hill_warp", "gully_passes")}
        C["spurs"] = 0.20
    out =os.path.join(g.ROOT, "unity", "Documentation", "Terrain", "Polish", "Geography", tag)
    os.makedirs(out, exist_ok=True)
    quiet = lambda s: None
    F = g.build_land(copy.deepcopy(C), quiet)
    bare = copy.deepcopy(C)
    bare["stamps"] = []
    F0 = g.build_land(bare, quiet)

    H, land = F["H"], F["land"]
    dry = land & (H > 0.0)
    wild = dry & (F["built"] < 0.5)
    cell = g.STEP * g.STEP
    L = []
    say = L.append
    say("%s  N=%d, cell %.2f m" % (C["name"], g.N, g.STEP))
    say("dry land %.0f m2, of it built platforms %.0f m2; highest %.1f m" % (dry.sum() * cell, (dry & ~wild).sum() * cell, H.max()))

    say("\nHYPSOMETRY (dry land outside platforms)")
    h = H[wild]
    top = math.ceil(h.max() / 5) * 5
    for lo in range(0, top, 5):
        share = ((h >= lo) & (h < lo + 5)).mean()
        say("  %2d-%2d m  %5.1f %%  %s" % (lo, lo + 5, share * 100, "#" * int(round(share * 100))))
    say("  hypsometric integral %.2f (0.5 a straight profile; under 0.3 old, worn land; over 0.6 young plateaus)" % ((h.mean() - h.min()) / (h.max() - h.min())))
    say("  median %.1f m, upper quartile %.1f m, upper decile %.1f m" % tuple(np.percentile(h, [50, 75, 90])))

    say("\nSLOPE (dry land outside platforms), degrees")
    s = F["slope"][wild]
    for lo, hi in ((0, 3), (3, 8), (8, 15), (15, 25), (25, 35), (35, 45), (45, 90)):
        share = ((s >= lo) & (s < hi)).mean()
        say("  %2d-%2d  %5.1f %%  %s" % (lo, hi, share * 100, "#" * int(round(share * 100))))

    say("\nSTAMPS: the relief built with them against the relief without")
    diff = H - F0["H"]
    base_sd = float(F0["H"][wild].std())
    say("  the land's own height spread without stamps %.2f m; what stamps add: RMS %.2f m, largest +%.1f / %.1f m" % (
        base_sd, float(np.sqrt((diff[wild] ** 2).mean())), diff[wild].max(), diff[wild].min()))
    say("  dry land the stamps move by over 1 m: %.0f %%; by over 3 m: %.0f %%" % ((np.abs(diff[wild]) > 1).mean() * 100, (np.abs(diff[wild]) > 3).mean() * 100))
    for st in C.get("stamps", []):
        rot = math.radians(st.get("rot", 0.0))
        dx, dy = g.GX - st["at"][0], g.GY - st["at"][1]
        u = (dx * math.cos(rot) + dy * math.sin(rot)) / st["size"]
        v = (-dx * math.sin(rot) + dy * math.cos(rot)) / st["size"]
        inside = (np.abs(u) < 0.5) & (np.abs(v) < 0.5) & wild
        if inside.any():
            one = copy.deepcopy(bare)
            one["stamps"] = [st]
            d1 = g.build_land(one, quiet)["H"] - F0["H"]
            edge = inside & (np.maximum(np.abs(u), np.abs(v)) > 0.44)
            say("  %-26s %3d m square: adds RMS %.2f m, range %+.1f..%+.1f m; on its last 6 %% by the edge still %.2f m" % (
                st["name"], st["size"], float(np.sqrt((d1[inside] ** 2).mean())), d1[inside].min(), d1[inside].max(),
                float(np.abs(d1[edge]).mean()) if edge.any() else 0.0))

    say("\nDRAINAGE (priority-flood, D8 on the filled ground)")
    Z, acc, outlet, recv = drainage(H, dry)
    depth = np.where(dry, Z - H, 0.0)
    hollow = depth > 0.3
    say("  closed hollows deeper than 0.3 m: %.0f m2 (%.1f %% of dry land), deepest %.1f m" % (hollow.sum() * cell, hollow.sum() / dry.sum() * 100, depth.max()))
    # A mouth inside the coast's own line is on a tidal creek; one outside it is on the open sea.
    wet = land.ravel()
    ids, counts = np.unique(outlet[dry], return_counts=True)
    to_creek = sum(c for i, c in zip(ids, counts) if i >= 0 and wet[i])
    say("  dry land draining into the tidal creeks %.0f %%, straight to the sea %.0f %%" % (to_creek / dry.sum() * 100, 100 - to_creek / dry.sum() * 100))
    # Catchments joined by the stretch of shore they reach: outlets within 40 m count as one mouth.
    mouths = []
    for i, c in sorted(zip(ids, counts), key=lambda t: -t[1]):
        if i < 0:
            continue
        y, x = divmod(int(i), g.N)
        px, py = x * g.STEP, y * g.STEP
        for m in mouths:
            if math.hypot(m[0] - px, m[1] - py) < 40.0:
                m[2] += c
                break
        else:
            mouths.append([px, py, c, bool(wet[i])])
    mouths.sort(key=lambda m: -m[2])
    say("  mouths (40 m of shore each) gathering over 1 ha: %d; the ten largest:" % sum(1 for m in mouths if m[2] * cell > 1e4))
    for m in mouths[:10]:
        say("    (%3.0f, %3.0f) %s  %5.1f ha  %4.1f %%" % (m[0], m[1], "creek" if m[3] else "sea  ", m[2] * cell / 1e4, m[2] / dry.sum() * 100))
    smooth = g.blur(H, 60.0)
    for thr in (2000.0, 10000.0):
        st = dry & (acc >= thr) & (F["built"] < 0.5)
        cut = (smooth - H)[st]
        say("  lines gathering over %.1f ha: %.2f km (%.1f km per km2); the ground there lies %.1f m (median) under its 60 m mean, and under it at all on %.0f %% of their length" % (
            thr / 1e4, st.sum() * g.STEP / 1000, st.sum() * g.STEP / 1000 / (dry.sum() * cell / 1e6),
            float(np.median(cut)) if cut.size else 0.0, (cut > 0.5).mean() * 100 if cut.size else 0.0))

    say("\nROUGHNESS: RMS height difference between points a lag apart (dry land outside platforms)")
    lags = [2, 4, 8, 16, 32, 64, 128, 256]
    sf = structure(H, wild, lags)
    for i, lag in enumerate(lags):
        hurst = math.log(sf[i] / sf[i - 1], 2) if i else float("nan")
        say("  %3d m  %6.2f m   exponent from the lag before %s" % (lag, sf[i], "%.2f" % hurst if i else "  - "))
    say("  (natural ground keeps an exponent near 0.7-0.9 across these lags; 1.0 means a plane at that scale, with no form of its own)")

    say("\nCRESTS: the ground along each authored ridge")
    for k, ridge in enumerate(C.get("ridges", [])):
        rp, sarc = g.resample(np.array(ridge, float)[:, :2], 4.0)
        prof = g.bil(H, rp[:, 0], rp[:, 1])
        sm = np.convolve(prof, np.ones(5) / 5, mode="same")
        # A zigzag of 2 m: a turn counts once the profile has come back by 2 m from its last extreme.
        ext, hi, lo, up = [], 0, 0, None
        for i in range(1, len(sm)):
            if sm[i] > sm[hi]:
                hi = i
            if sm[i] < sm[lo]:
                lo = i
            if up is not False and sm[hi] - sm[i] > 2.0:
                ext.append((sarc[hi], sm[hi], "summit"))
                up, lo = False, i
            elif up is not True and sm[i] - sm[lo] > 2.0:
                if up is False:
                    ext.append((sarc[lo], sm[lo], "saddle"))
                up, hi = True, i
        say("  ridge %d, %.0f m long: %.1f..%.1f m; %d summits, %d saddles of over 2 m: %s" % (
            k + 1, sarc[-1], prof.min(), prof.max(), sum(1 for e in ext if e[2] == "summit"), sum(1 for e in ext if e[2] == "saddle"),
            ", ".join("%s %.0f m at %.0f" % (e[2], e[1], e[0]) for e in ext)))

    say("\nCOAST")
    line = F["coast"]
    closed = np.vstack([line, line[:1]])
    length = float(np.hypot(np.diff(closed[:, 0]), np.diff(closed[:, 1])).sum())
    area = land.sum() * cell
    say("  the outer shore %.0f m round %.0f m2: shoreline development %.2f (a circle 1.0; natural islands of this size 1.3-2.5)" % (length, area, length / (2 * math.sqrt(math.pi * area))))
    edge = dry & ~(np.roll(dry, 1, 0) & np.roll(dry, -1, 0) & np.roll(dry, 1, 1) & np.roll(dry, -1, 1))
    ys, xs = np.nonzero(edge)
    sizes, boxes = [4, 8, 16, 32, 64], []
    for sz in sizes:
        k = max(1, int(round(sz / g.STEP)))
        boxes.append(len(set(zip((ys // k).tolist(), (xs // k).tolist()))))
    dim = -np.polyfit(np.log(sizes), np.log(boxes), 1)[0]
    say("  every waterline, creeks too: box-count dimension %.2f over 4-64 m (a smooth curve 1.0; natural rocky coasts 1.15-1.3)" % dim)
    kinds = {"b": "sand", "g": "gravel", "r": "rock", "c": "cliff", "m": "marsh", "q": "quay"}
    near = edge & (F["dl"] < 12)
    tot = sum(float(F["is_" + t][near].sum()) for t in kinds)
    say("  shore kinds by length: " + ", ".join("%s %.0f %%" % (kinds[t], F["is_" + t][near].sum() / tot * 100) for t in kinds))

    with open(os.path.join(out, "report.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(L) + "\n")
    print("\n".join(L))

    # The maps.
    shade = 0.5 * hillshade(H) + 0.3 * hillshade(H, 45.0) + 0.2 * hillshade(H, 225.0)
    tint = np.stack([np.interp(H, [0, 10, 25, 45, 60], c) for c in ([118, 150, 168, 150, 200], [150, 168, 156, 132, 200], [96, 104, 104, 112, 200])], -1)
    img = tint * (0.35 + 0.9 * shade[..., None])
    img[~dry] = np.array([70, 104, 132]) * (0.8 + 0.2 * np.clip(H[~dry] / -8.0, 0, 1))[..., None] if (~dry).any() else img[~dry]
    plain = img.copy()
    for thr, col in ((2000.0, (60, 120, 210)), (10000.0, (20, 60, 190))):
        img[dry & (acc >= thr)] = col
    img[hollow] = (200, 40, 160)
    g.write_png_rgb(os.path.join(out, "hillshade.png"), upscale(plain))
    g.write_png_rgb(os.path.join(out, "drainage.png"), upscale(img))
    sl = np.clip(F["slope"] / 50.0, 0, 1)
    simg = np.stack([40 + 215 * sl, 200 - 150 * sl, 90 - 60 * sl], -1)
    simg[~dry] = (70, 104, 132)
    g.write_png_rgb(os.path.join(out, "slope.png"), upscale(simg))
    dimg = np.stack([128 + 22 * np.clip(diff, -5, 5), 128 - 12 * np.abs(np.clip(diff, -5, 5)), 128 - 22 * np.clip(diff, -5, 5)], -1) * (0.55 + 0.6 * shade[..., None])
    dimg[~dry] = (70, 104, 132)
    g.write_png_rgb(os.path.join(out, "stamps.png"), upscale(dimg))
    np.save(os.path.join(g.CACHE, "report_%s_%s.npy" % (key, tag)), H)


if __name__ == "__main__":
    main()
