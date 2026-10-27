"""G3f analytic checks on synthetic lattices (spec §14; phase plan §7 WP1 verification).

Usage: python -B tests/analytic_checks.py <out_dir>
Writes analytic_checks.json (every reading) and analytic_checks.md (the verdict table). No Unity, no sector data.
"""
import copy
import json
import math
import os
import sys
import time

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))

import synthetic as SY  # noqa: E402
from g3f import synth as S, checks as K  # noqa: E402
from g3f.lattice import NB8, DIAG, C, lattice_sha256  # noqa: E402


def lateral_excess(lat, R, P, tol=0.01):
    """GF-AC-5: largest |dh| - d * lip over 8-adjacent pairs of the same unit, both non-exempt."""
    n0, n1 = lat.shape
    worst = -math.inf
    count = 0
    for dz, dx in ((0, 1), (1, 0), (1, 1), (1, -1)):
        d = C * (DIAG if dz and dx else 1.0)
        a = (slice(0, n0 - dz), slice(max(0, -dx), n1 - max(0, dx)))
        b = (slice(dz, n0), slice(max(0, dx), n1 - max(0, -dx)))
        ua, ub = R.unit[a], R.unit[b]
        ok = (ua >= 0) & (ua == ub) & ~R.exempt[a] & ~R.exempt[b]
        ex = np.abs(lat[a] - lat[b]) - d * P.lip
        ex = np.where(ok, ex, -np.inf)
        worst = max(worst, float(ex.max()))
        count += int((ex > tol).sum())
    return worst, count


def steep_touching(lat, R):
    s1, s2 = K.triangle_slopes_deg(lat)
    land = R.land
    l1 = land[:-1, :-1] & land[:-1, 1:] & land[1:, 1:]
    l2 = land[:-1, :-1] & land[1:, 1:] & land[1:, :-1]

    def any3(m, t):
        q00, q10, q01, q11 = m[:-1, :-1], m[:-1, 1:], m[1:, :-1], m[1:, 1:]
        return (q00 | q10 | q11) if t == 1 else (q00 | q11 | q01)

    ex, cm = R.exempt, R.crest >= 0
    st1 = (s1 >= 45) & l1 & ~any3(ex, 1)
    st2 = (s2 >= 45) & l2 & ~any3(ex, 2)
    total = int(st1.sum() + st2.sum())
    crest = int((st1 & any3(cm, 1)).sum() + (st2 & any3(cm, 2)).sum())
    return {"steep45_outside_exempt": total, "touching_crest": crest, "land_triangles": int(l1.sum() + l2.sum())}


def basic(R, lat, log, P):
    cr, on = K.flow_across(lat, R)
    ends = K.trace_ends(lat, R)
    bad_ends = {u: {k: v for k, v in e.items() if k != "own-root"} for u, e in ends.items()}
    bad_ends = {u: e for u, e in bad_ends.items() if e}
    worst, nexc = lateral_excess(lat, R, P)
    w = K.write_log_ok(R, log["write"])
    return {
        "AC1_pits": int(len(K.pits(lat, R.land))),
        "AC2_bad_ends": bad_ends,
        "AC3_cross": len(cr), "AC3_onto_crest": len(on),
        "AC4_write_ok": w["ok"], "AC4_write": {k: v for k, v in w.items() if k != "ok"},
        "AC5_lateral_worst_excess_m": worst, "AC5_pairs_over_tol": nexc,
        "diag": steep_touching(lat, R),
        "lattice_sha256": lattice_sha256(lat),
        "log": {k: v for k, v in log.items() if k not in ("write", "plateaus")},
        "plateaus": log.get("plateaus"),
    }


def k1_edge(lat, top, land, cell=C):
    """K1 plateau edge height (G3e spec §2.4): median edge drop over escarpment samples."""
    n0, n1 = lat.shape
    s1, s2 = K.triangle_slopes_deg(lat)
    steep = np.zeros((n0, n1), bool)
    for t, s in ((1, s1), (2, s2)):
        m = s >= 45
        steep[:-1, :-1] |= m
        steep[:-1, 1:] |= m if t == 1 else False
        steep[1:, 1:] |= m
        if t == 2:
            steep[1:, :-1] |= m
    rim = []
    for z, x in zip(*np.nonzero(top)):
        if any(0 <= z + dz < n0 and 0 <= x + dx < n1 and not top[z + dz, x + dx] for dz, dx in NB8):
            rim.append((z, x))
    drops = []
    r15 = int(math.ceil(15 / cell))
    for z, x in rim:
        cz, cx = z, x
        path = 0.0
        low = 0
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
        near = steep[max(0, z - r15):z + r15 + 1, max(0, x - r15):x + r15 + 1] & ~top[max(0, z - r15):z + r15 + 1, max(0, x - r15):x + r15 + 1]
        if drop >= 10.0 and near.any():
            drops.append(drop)
    return (float(np.median(drops)) if drops else None), len(drops), len(rim)


def main(out):
    os.makedirs(out, exist_ok=True)
    P = S.Params()
    res = {"params": P.snapshot(), "cases": {}}
    t0 = time.time()

    # cone: bisection (AC6) + basics
    R, tgt = SY.cone()
    for cand in ("C0", "UF"):
        lat, log, T = S.synthesize(R, P, cand, target=tgt)
        r = basic(R, lat, log, P)
        x, z, H, rad = tgt
        got = max(lat[zz, xx] for zz in range(R.n0) for xx in range(R.n1)
                  if R.unit[zz, xx] >= 0 and math.hypot(xx - x, zz - z) * C <= rad)
        r["AC6_target"] = H - P.crest_delta
        r["AC6_final_probe"] = float(got)
        r["AC6_law_probe"] = float(log["bisection"][[s for s, _ in log["bisection"]].index(log["scale"])][1])
        r["AC6_met"] = bool(log["bisection_met"])
        res["cases"][f"cone/{cand}"] = r

    # ridge
    R, tgt = SY.ridge()
    for cand in ("C0", "UF", "RF"):
        lat, log, T = S.synthesize(R, P, cand, target=tgt if cand != "RF" else None)
        res["cases"][f"ridge/{cand}"] = basic(R, lat, log, P)

    # plateau block
    R, top, band, ramp = SY.plateau_block()
    for cand in ("C0", "UF", "RF"):
        lat, log, T = S.synthesize(R, P, cand)
        r = basic(R, lat, log, P)
        tv = lat[top]
        r["AC8_top_relief_m"] = float(tv.max() - tv.min())
        pl = log["plateaus"][0]
        r["AC8_ramp_grade_deg"] = math.degrees(math.atan(pl["ramp_grade"]))
        r["AC8_ramp_capped"] = pl["ramp_capped"]
        e, ns, nr = k1_edge(lat, top, R.land)
        r["AC8_k1_edge_m"] = e
        r["AC8_k1_samples"] = [ns, nr]
        r["AC8_edge_target_m"] = R.plateaus[0]["edge"]
        res["cases"][f"plateau/{cand}"] = r

    # valley: axis climbs (AC7)
    R, polys = SY.valley()
    for cand in ("C0", "UF", "RF"):
        lat, log, T = S.synthesize(R, P, cand, target=(70, 120, 150.0, 60.0) if cand != "RF" else None)
        r = basic(R, lat, log, P)
        r["AC7_climbs_m"] = [float(K.axis_climb(lat, p)) for p in polys]
        res["cases"][f"valley/{cand}"] = r

    # asymmetric divide: assembly (AC9)
    R = SY.asym_divide()
    T = S.build_tree(R, P)
    f32 = lambda a: np.asarray(a, "<f4")
    for cand in ("C0", "UF", "RF"):
        lat, log, _ = S.synthesize(R, P, cand, T=T, target=(40, 60, 120.0, 100.0) if cand != "RF" else None)
        res["cases"][f"asym/{cand}"] = basic(R, lat, log, P)
    joint, _, _ = S.synthesize(R, P, "RF", T=T)
    a, _ = S.synthesize_per_unit(R, P, "RF", [0, 1], T=T)
    b, _ = S.synthesize_per_unit(R, P, "RF", [1, 0], T=T)
    P2 = S.Params(theta=0.55)
    lat2 = R.fixed.copy()
    for u, PP in ((0, P2), (1, P)):
        Q = S.restrict(R, [u])
        l, _, _ = S.synthesize(Q, PP, "RF", T=T)
        lat2[R.unit == u] = l[R.unit == u]
        lat2[R.crest >= 0] = l[R.crest >= 0]
    d = f32(lat2) != f32(a)
    out0 = (R.unit != 0) & R.land
    rf = {"AC9_order_invariant": bool(np.array_equal(f32(a), f32(b))),
          "AC9_per_unit_equals_joint": bool(np.array_equal(f32(a), f32(joint))),
          "AC9_perturb_changed_outside": int((d & out0).sum()), "AC9_perturb_changed_inside": int((d & ~out0).sum())}
    uf, _, _ = S.synthesize(R, P, "UF", T=T, target=(40, 60, 120.0, 100.0))
    R2 = copy.copy(R)
    R2.unit = np.where(R.unit >= 0, 1 - R.unit, -1).astype(np.int32)
    R2.units = list(reversed(R.units))
    uf2, _, _ = S.synthesize(R2, P, "UF", target=(40, 60, 120.0, 100.0))
    uf3, _, _ = S.synthesize(R, P2, "UF", T=T, target=(40, 60, 120.0, 100.0))
    du = f32(uf3) != f32(uf)
    ufr = {"AC9_relabel_invariant": bool(np.array_equal(f32(uf), f32(uf2))),
           "AC9_perturb_changed_outside": int((du & out0).sum()), "AC9_perturb_changed_inside": int((du & ~out0).sum())}
    res["assembly"] = {"RF": rf, "UF": ufr}
    res["seconds"] = round(time.time() - t0, 1)

    with open(os.path.join(out, "analytic_checks.json"), "w", encoding="utf-8") as f:
        json.dump(res, f, indent=1, sort_keys=True, default=lambda o: o.item() if hasattr(o, "item") else str(o))
    lines = ["| case | AC1 pits | AC2 bad ends | AC3 cross / onto | AC4 write | AC5 worst excess m (pairs) | other | steep >= 45 outside exempt (touching crest) |",
             "|---|---|---|---|---|---|---|---|"]
    for k, r in res["cases"].items():
        other = []
        if "AC6_met" in r:
            other.append(f"AC6 law {r['AC6_law_probe']:.2f} vs {r['AC6_target']:.2f} met={r['AC6_met']} (final {r['AC6_final_probe']:.2f})")
        if "AC7_climbs_m" in r:
            other.append("AC7 climbs " + ", ".join(f"{c:.4f}" for c in r["AC7_climbs_m"]))
        if "AC8_top_relief_m" in r:
            other.append(f"AC8 relief {r['AC8_top_relief_m']:.2f} m, ramp {r['AC8_ramp_grade_deg']:.2f} deg capped={r['AC8_ramp_capped']}, K1 {r['AC8_k1_edge_m']} vs {r['AC8_edge_target_m']}")
        lines.append(f"| {k} | {r['AC1_pits']} | {r['AC2_bad_ends'] or '-'} | {r['AC3_cross']} / {r['AC3_onto_crest']} | {r['AC4_write_ok']} | "
                     f"{r['AC5_lateral_worst_excess_m']:.4f} ({r['AC5_pairs_over_tol']}) | {'; '.join(other) or '-'} | "
                     f"{r['diag']['steep45_outside_exempt']} ({r['diag']['touching_crest']}) |")
    lines.append("")
    lines.append(f"Assembly: RF {res['assembly']['RF']}; UF {res['assembly']['UF']}")
    with open(os.path.join(out, "analytic_checks.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))


if __name__ == "__main__":
    main(sys.argv[1])
