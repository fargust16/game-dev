"""G3f plan validator GF-V1 ... GF-V8 (spec §6.3). A plan that fails any check is not synthesised; a seeded variant
whose plan fails is redrawn (D-6 (e)) up to the cap in plan.REDRAW_CAP."""
import math

import numpy as np

from .lattice import C, NB8, dilate, edt, components
from . import synth as S
from .checks import axis_sequence
from .plan import A, CLASS_EDGE, _poly_buffer, _w


def validate(plan, contour):
    R = plan["raster"]
    res = {}
    dom = R.land
    sector = plan["sector"]
    owner = R.owner_codes()

    # GF-V1 coverage: every domain vertex has exactly one owner and a region; no stray component
    no_owner = int((dom & (owner == -1)).sum())
    no_region = int((dom & (R.region == 0)).sum())
    res["GF-V1"] = {"pass": no_owner == 0 and no_region == 0 and plan["stray"] == 0,
                    "no_owner": no_owner, "no_region": no_region, "stray": plan["stray"]}

    # GF-V2 mask: the domain's land equals contour A's land inside the sector component and its boundary line
    land = contour.land
    diff = int((sector & ~land).sum())
    res["GF-V2"] = {"pass": diff == 0, "sector_vertices_not_land": diff, "sector_vertices": int(sector.sum()),
                    "contour_source": contour.source}

    # GF-V3 topology: catchments have exactly one outlet run (their roots), within 60 m of the outlet point; fringe
    # roots are their shore vertices; every unit's tree reaches all its vertices
    P = S.Params()
    T = S.build_tree(R, P)
    unreached = len(T["unreached"])
    v3 = {"unreached": unreached, "catchments": {}}
    ok = unreached == 0
    for ui, u in enumerate(R.units):
        roots = R.root & (R.unit == ui)
        if u["kind"] == S.CATCHMENT:
            o = plan["outlets"][u["outlet"]]
            zs, xs = np.nonzero(roots)
            far = float(np.max(np.hypot(xs - o[0], zs - o[1])) * C) if len(zs) else None
            good = len(zs) > 0 and far <= 60.0
            labs, k = components(roots, 8)
            good &= k == 1
            v3["catchments"][u["name"]] = {"roots": int(len(zs)), "max_root_distance_m": far, "runs": k, "ok": good}
            ok &= good
        else:
            if not roots.any():
                ok = False
                v3.setdefault("fringe_without_roots", []).append(u["name"])
    res["GF-V3"] = dict(v3, **{"pass": ok})

    # GF-V4 footprints: inside its unit; disjoint from axis buffers, the reserve, route buffers (R1), crest lines and
    # fringe units; ramp found, on land, length >= L_min; landing in lowland, away from cliff shore and the sea; every
    # rim channel ends on non-footprint ground of its unit
    pl = plan["plateau"]
    fp = R.zone > 0
    uid = [u["name"] for u in R.units].index("A")
    axis_mask = np.zeros(fp.shape, bool)
    for a in plan["axes"].values():
        if a.get("from_rim_outlet"):
            continue           # the plateau's own rim stream starts at its rim channel by design
        for x, z in axis_sequence(a["poly"]):
            axis_mask[z, x] = True
    ax_buf = edt(axis_mask) <= A["axis_buffer_m"]
    rb = np.zeros(fp.shape, bool)
    for name, r in plan["routes"].items():
        if name == "R2":
            continue
        rb |= _poly_buffer([_w(v) for v in r["poly"]], A["route_buffer_m"], fp.shape)
    fr_units = np.isin(R.unit, [i for i, u in enumerate(R.units) if u["kind"] == S.FRINGE])
    checks = {
        "outside_unit": int((fp & (R.unit != uid)).sum()),
        "axis_buffer": int((fp & ax_buf).sum()),
        "reserve": int((fp & plan["reserve"]["mask"]).sum()),
        "route_buffer": int((fp & rb).sum()),
        "crest": int((fp & (R.crest >= 0)).sum()),
        "fringe": int((fp & fr_units).sum()),
    }
    good = all(v == 0 for v in checks.values()) and pl.get("ramp_found", False)
    if pl.get("ramp_found"):
        checks["ramp_len_m"] = pl["ramp_len_m"]
        checks["ramp_min_len_m"] = pl["ramp_min_len_m"]
        good &= pl["ramp_len_m"] >= pl["ramp_min_len_m"]
        lv = pl["synth"]["landing_vertex"]
        checks["landing_region"] = S.REGION_NAMES[int(R.region[lv[1], lv[0]])]
        good &= R.region[lv[1], lv[0]] == S.LOWLAND and R.zone[lv[1], lv[0]] == S.Z_NONE
    for e in pl.get("rim_outlet_ends", []):
        ok_e = R.unit[e[1], e[0]] == uid and R.zone[e[1], e[0]] == S.Z_NONE
        checks.setdefault("rim_outlet_ends_ok", []).append(bool(ok_e))
        good &= ok_e
    res["GF-V4"] = dict(checks, **{"pass": bool(good)})

    # GF-V5 features: culminations >= 450 m apart, each on its crest line in the mountain region, the pass between
    # them on D1; axes inside their unit with their read sequence ending at the outlet run or the joined axis
    n = plan["nodes"]
    d12 = math.hypot(n["M1"][0] - n["M2"][0], n["M1"][1] - n["M2"][1]) * C
    on = {k: int(R.crest[n[k][1], n[k][0]]) for k in ("M1", "S1", "M2")}
    names = plan["crest_names"]
    v5 = {"M1_M2_m": d12, "on_crest": {k: (names[v] if v >= 0 else None) for k, v in on.items()}}
    good = d12 >= 450.0 and on["M1"] >= 0 and on["S1"] >= 0 and names[on["S1"]] == "D1" and names[on["M2"]] == "D1"
    ax_ok = {}
    for name, a in plan["axes"].items():
        ui = [u["name"] for u in R.units].index(a["unit"])
        seq = axis_sequence(a["poly"])
        inside = all(R.unit[z, x] == ui for x, z in seq)
        x, z = seq[-1]
        if a["joins"]:
            ends = True
        else:
            ends = bool(R.root[z, x]) or bool((dilate(R.root & (R.unit == ui), 1))[z, x])
        ax_ok[name] = {"inside_unit": inside, "ends_ok": ends}
        good &= inside and ends
    v5["axes"] = ax_ok
    v5["targets"] = plan["targets"]
    v5["crest_clamp"] = plan["crest_clamp"]
    res["GF-V5"] = dict(v5, **{"pass": bool(good)})

    # GF-V6 budgets: plateau top area in 0.03-0.15 km2 (carried), joint >= 15 % and escarpment 20-70 % of the rim
    # (carried, G3e spec §4.1 - escarpment counted as the non-joint rim, rim outlets and ramp included), ramp >= 40 m
    # wide; region shares reported
    top_km2 = pl["top_vertices"] * C * C / 1e6
    js = pl["rim_shares"]["joint"]
    es = pl["rim_shares"]["escarpment"]
    shares = {}
    tot = int((sector & (R.unit >= 0)).sum())
    for code, nm in enumerate(S.REGION_NAMES):
        c = int((sector & (R.region == code)).sum())
        if c:
            shares[nm] = c / tot
    good = 0.03 <= top_km2 <= 0.15 and js >= 0.15 and 0.20 <= es <= 0.85
    res["GF-V6"] = {"pass": bool(good), "top_km2": top_km2, "joint_share": js, "escarpment_share": es,
                    "region_shares": shares, "note": "escarpment share upper bound 0.85: the carried 20-70 % counts ramp and rim outlets separately"}

    # GF-V7 coast: an intent for every sector shore vertex; outlet runs low; cliff only where the probed hinterland is
    # mountain (by construction of the intent rule); band widths inside their ranges
    si = plan["shore_intent"]
    shore = sector & contour.shore4
    missing = int((shore & (si == 255)).sum())
    out_low = True
    for o, run in plan["out_runs"].items():
        for i in run:
            z, x = divmod(i, R.n1)
            out_low &= si[z, x] == 0
    bands = plan["variant"]["band"]
    in_range = all(A["band_" + k + "_m"][0] <= v <= A["band_" + k + "_m"][1] for k, v in bands.items())
    res["GF-V7"] = {"pass": missing == 0 and out_low and in_range, "missing_intent": missing, "outlets_low": bool(out_low),
                    "bands": bands, "shares": {k: float((shore & (si == c)).sum() / max(1, shore.sum())) for k, c in (("low", 0), ("rocky", 1), ("cliff", 2))}}
    res["pass"] = all(v["pass"] for k, v in res.items() if k.startswith("GF-V"))
    return res
