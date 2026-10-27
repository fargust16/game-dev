"""WP3 continuity (phase plan §7 WP3; spec §9.9): the G3f harness on run 11's preserved S-LM1 lattices, restricted to
the sector, must reproduce WP0's C0-5 targets — S8 depressions within 3 m, 0.01 m depth and one cell of area; axis
climbs within 0.01 m; reachability identical.

Usage: python -B tests/continuity.py <research-data root> <c05 json> <out json>"""
import json
import math
import os
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))

from g3f.lattice import C, N, SEA, load_f32  # noqa: E402
from g3f import measure as M  # noqa: E402
from g3f.vendor import lmcspec as LS  # noqa: E402


def node_climb(h, pts):
    """G3e R7 (c): nodes read at the nearest vertex, climb = max_k [h(n_k) - min_{j<k} h(n_j)]."""
    climb, run_min = 0.0, math.inf
    for x, z in pts:
        v = float(h[int(round(z / C)), int(round(x / C))])
        if v - run_min > climb:
            climb = v - run_min
        run_min = min(run_min, v)
    return climb


def main(rd, c05_path, out_path):
    c05 = json.load(open(c05_path, encoding="utf-8"))
    from g3f.lattice import raster4, components, load_u8
    land_a = load_u8(os.path.join(rd, "g3e-step1/run11/output/seed_110201/layout_region.u8")) != 0
    cut = land_a.copy()
    for x, z in raster4((516, 441), (600, 887)):
        cut[z, x] = False
    lab, _ = components(cut, 8)
    sec = lab == lab[700, 300]
    global in_sector
    in_sector = lambda x, z: bool(sec[z, x])
    res = {"seeds": {}, "pass": True}
    for seed in ("110201", "1337"):
        base = os.path.join(rd, f"g3e-step1/run11/output/seed_{seed}")
        h32 = load_f32(os.path.join(base, f"lm1_{seed}.f32")).astype(np.float32)
        land = h32 > np.float32(SEA)
        tgt = c05["seeds"][seed]["targets"]
        ocean = M.ocean_mask8(h32)
        filled = M.depression_fill(h32, ocean)
        s8 = M.s8_depressions(h32, filled, land)
        rows = []
        ok = True
        for t in tgt["s8_in_sector"]:
            best = min(s8, key=lambda d: math.hypot(d["xz"][0] - t["xz"][0], d["xz"][1] - t["xz"][1])) if s8 else None
            if best is None:
                rows.append({"target": t, "found": None, "ok": False})
                ok = False
                continue
            dpos = math.hypot(best["xz"][0] - t["xz"][0], best["xz"][1] - t["xz"][1])
            # the C# record prints depth with 2 decimals and area with 0 decimals
            ddep = abs(round(best["depth_m"], 2) - t["depth"])
            darea = abs(round(best["area_m2"]) - t["area_m2"])
            good = dpos <= 3.0 and ddep <= 0.01 and darea <= C * C
            ok &= good
            rows.append({"target": t, "found": best, "dpos_m": dpos, "ddepth_m": ddep, "darea_m2": darea, "ok": good})
        axes = json.load(open(os.path.join(base, "diag", "lm1_axes_and_frames.json"), encoding="utf-8"))["axes_measured"]
        by = {a["name"]: a for a in axes}
        arows = []
        for t in tgt["axes_fully_in_sector"]:
            c = node_climb(h32, by[t["name"]]["points"])
            good = abs(c - t["climb"]) <= 0.01
            ok &= good
            arows.append({"name": t["name"], "target": t["climb"], "harness": c, "ok": good})
        te = LS.TEdge(h32.astype(np.float64))
        te.components()
        rrows = []
        for s in tgt["sites_in_sector"]:
            x, z = int(round(s["xz"][0] / C)), int(round(s["xz"][1] / C))
            _, r = te.vertex_component(x, z)
            good = bool(r) == bool(s["reachable"])
            ok &= good
            rrows.append({"site": s["xz"], "target": s["reachable"], "harness": bool(r), "ok": good})
        # plateau tops whose declared centre lies in the sector: the top vertex nearest the centroid (A1)
        lay = open(os.path.join(base, "layout.txt"), encoding="utf-8").read()
        import re
        reg = np.fromfile(os.path.join(base, "layout_region.u8"), np.uint8).reshape(N, N)
        ins = np.fromfile(os.path.join(base, "layout_instance.u8"), np.uint8).reshape(N, N)
        sector = c05.get("_sector_mask")
        unreach = c05["seeds"][seed]["r8"]["unreachable"]
        for k, cx, cz in re.findall(r"plateau (\d+): centre \((\S+), (\S+)\)", lay):
            k = int(k)
            top = (reg == 4) & (ins == k)
            zs, xs = np.nonzero(top)
            if not len(zs):
                continue
            mx, mz = xs.mean(), zs.mean()
            j = int(np.argmin((xs - mx) ** 2 + (zs - mz) ** 2))
            vx, vz = int(xs[j]), int(zs[j])
            if not in_sector(vx, vz):
                continue
            _, r = te.vertex_component(vx, vz)
            want = f"plateau {k} top" not in unreach
            good = bool(r) == want
            ok &= good
            rrows.append({"plateau_top": k, "vertex": [vx, vz], "target": want, "harness": bool(r), "ok": good})
        r8 = c05["seeds"][seed]["r8"]
        if r8.get("pass_col_in_sector"):
            x, z = int(round(r8["pass_col_xz"][0] / C)), int(round(r8["pass_col_xz"][1] / C))
            _, r = te.vertex_component(x, z)
            good = bool(r) == bool(r8["pass_col_reachable"])
            ok &= good
            rrows.append({"pass_col": r8["pass_col_xz"], "target": r8["pass_col_reachable"], "harness": bool(r), "ok": good})
        res["seeds"][seed] = {"s8": rows, "s8_world_count": len(s8), "axes": arows, "reach": rrows, "ok": ok}
        res["pass"] &= ok
        print(seed, "ok" if ok else "FAIL", "s8", [(r.get("dpos_m"), r.get("ddepth_m"), r.get("darea_m2")) for r in rows],
              "axes", [round(a["harness"] - a["target"], 4) for a in arows], "reach", [r["ok"] for r in rrows], flush=True)
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(res, f, indent=1)
    print("continuity", "PASS" if res["pass"] else "FAIL")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
