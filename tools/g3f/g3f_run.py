"""G3f research runner (spec §13). Steps:

  variant  --seed S --class short|tall --out DIR [--candidates C0,UF,RF] [--no-assembly]
           plan (D-6 (e) redraws) -> context (GF-C0) -> candidates -> readings -> GF-A; records in DIR/<seed>_<class>/
  hydro-list --runs DIR... --out FILE     the lattice list for TerrainGenG3fDump -g3fStep hydro
  merge-hydro --run DIR --hydro HDIR      folds GF-R5, GF-R10 and R7 (b) into the readings
  views-list --run DIR --out FILE         cameras (+ validity variants) and lattices for -g3fStep views
  sheets --run DIR --renders RDIR --out SDIR   blind sheets (sealed key) and S-PLAN images
  summary --runs DIR...                   the gate table
  manifest --dir DIR                      SHA-256 of every file (SHA256SUMS.txt)

Seeds: calibration 110201 and 1337; held-out 5001-5010 (reserve 5011-5020) only when research-data/g3f/freeze/FREEZE.md
exists; 1005-1024 always refused (D-6 (f))."""
import argparse
import glob
import hashlib
import json
import os
import sys
import time

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

from g3f import plan as PL, synth as S, run as RU, measure as M, validate as V, assembly as AS, views as VW  # noqa: E402
from g3f.lattice import save_f32, load_f32, sha256_file, lattice_sha256  # noqa: E402

RD = os.environ.get("G3F_RD", "D:/Hobby/game-dev/research-data")
CALIBRATION = (110201, 1337)
HELDOUT = tuple(range(5001, 5021))
FREEZE = os.path.join(RD, "g3f", "freeze", "FREEZE.md")


def _json(o):
    if hasattr(o, "item"):
        return o.item()
    if isinstance(o, (set, tuple)):
        return list(o)
    if isinstance(o, np.ndarray):
        return o.tolist()
    return str(o)


def dump(path, obj):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, indent=1, sort_keys=True, default=_json)


def check_seed(seed):
    if 1005 <= seed <= 1024:
        raise SystemExit(f"seed {seed} is reserved for the island-scale evaluation (D-6 (f)); refused")
    if seed in CALIBRATION:
        return "calibration"
    if seed in HELDOUT:
        if not os.path.exists(FREEZE):
            raise SystemExit(f"held-out seed {seed} without the freeze record {FREEZE}; refused")
        return "held-out"
    raise SystemExit(f"seed {seed} is neither a calibration nor a G3f held-out seed; refused")


def variant(a):
    role = check_seed(a.seed)
    out = os.path.join(a.out, f"{a.seed}_{a.cls}")
    os.makedirs(out, exist_ok=True)
    times = {}
    t0 = time.time()
    fx = json.load(open(PL.FIXTURE, encoding="utf-8"))
    con = PL.load_contour(RD)
    pl, val, attempts = PL.instantiate_valid(fx, a.seed, a.cls, con, V.validate)
    times["plan_s"] = time.time() - t0
    dump(os.path.join(out, "attempts.json"), {"role": role, "attempts": attempts, "contour": con.source})
    if pl is None:
        dump(os.path.join(out, "validation.json"), val)
        print(f"{a.seed} {a.cls}: plan invalid after {len(attempts)} attempts")
        return
    rec, ph = PL.plan_record(pl)
    dump(os.path.join(out, "plan.json"), rec)
    open(os.path.join(out, "plan_hash.txt"), "w").write(ph + "\n")
    dump(os.path.join(out, "validation.json"), val)
    P = S.Params()
    t1 = time.time()
    clat, clog = RU.context_lattice(pl, P, a.seed)
    times["context_s"] = time.time() - t1
    save_f32(os.path.join(out, "context.f32"), np.where(np.isnan(clat), 10.0, clat))
    dump(os.path.join(out, "context_log.json"), {k: v for k, v in clog.items() if k != "write"})
    RU.embed(pl, clat)
    dump(os.path.join(out, "params.json"), {"synth": P.snapshot(), "plan": PL.A, "redraw_cap": PL.REDRAW_CAP})
    VW.plan_image(pl, np.where(np.isnan(clat), 10.0, clat), os.path.join(out, "plan.png"), overlay=True)
    for cand in a.candidates.split(","):
        t2 = time.time()
        lat, log, T = RU.run_candidate(pl, P, cand, a.seed)
        times[f"{cand}_synth_s"] = time.time() - t2
        lat = np.where(np.isnan(lat), 10.0, lat)
        save_f32(os.path.join(out, f"{cand}.f32"), lat)
        lat32 = load_f32(os.path.join(out, f"{cand}.f32"))
        dump(os.path.join(out, f"{cand}_synth.json"), {k: v for k, v in log.items() if k != "write"})
        t3 = time.time()
        rd = M.readings(pl, lat32, log, cand)
        times[f"{cand}_read_s"] = time.time() - t3
        dump(os.path.join(out, f"{cand}_readings.json"), rd)
        if cand in ("UF", "RF") and not a.no_assembly:
            t4 = time.time()
            asm = AS.assembly(pl, P, cand, a.seed, lat32)
            times[f"{cand}_assembly_s"] = time.time() - t4
            dump(os.path.join(out, f"{cand}_assembly.json"), asm)
        print(f"{a.seed} {a.cls} {cand}: {rd['gates']}", flush=True)
    times["total_s"] = time.time() - t0
    dump(os.path.join(out, "times.json"), times)


def hydro_list(a):
    lines = []
    for run in a.runs:
        for d in sorted(glob.glob(os.path.join(run, "*_*"))):
            if not os.path.isdir(d):
                continue
            seed = os.path.basename(d).split("_")[0]
            for f in sorted(glob.glob(os.path.join(d, "*.f32"))):
                lab = os.path.basename(d) + "_" + os.path.basename(f)[:-4]
                lines.append(f"{lab}|{seed}|{os.path.abspath(f).replace(os.sep, '/')}")
    open(a.out, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    print(len(lines), "lattices")


def merge_hydro(a):
    fx = json.load(open(PL.FIXTURE, encoding="utf-8"))
    con = PL.load_contour(RD)
    for d in sorted(glob.glob(os.path.join(a.run, "*_*"))):
        if not os.path.isdir(d) or not os.path.exists(os.path.join(d, "plan.json")):
            continue
        seed, cls = os.path.basename(d).split("_")
        pl, val, att = PL.instantiate_valid(fx, int(seed), cls, con, V.validate)
        _, ph = PL.plan_record(pl)
        assert ph == open(os.path.join(d, "plan_hash.txt")).read().strip(), "plan hash differs from the recorded one"
        for rf in sorted(glob.glob(os.path.join(d, "*_readings.json"))):
            cand = os.path.basename(rf).split("_")[0]
            hp = os.path.join(a.hydro, f"hydro_{os.path.basename(d)}_{cand}.json")
            if not os.path.exists(hp):
                continue
            hy = json.load(open(hp, encoding="utf-8"))
            assert hy["lattice_md5"] == hashlib.md5(open(os.path.join(d, f"{cand}.f32"), "rb").read()).hexdigest()
            rd = json.load(open(rf, encoding="utf-8"))
            r5, r10, lakes = M._r5(pl, hy)
            rd["r5"], rd["r10"] = r5, r10
            rd["gates"]["GF-R5"] = r5["pass"]
            rd["gates"]["GF-R10"] = r10["succeeded"]
            rd["r7"]["lakes_sector"] = lakes
            rd["r7"]["b"] = lakes == 0 if lakes is not None else False
            rd["gates"]["GF-R7"] = bool(rd["r7"]["a"] and rd["r7"]["c"] and rd["r7"]["b"])
            dump(rf, rd)
            print(os.path.basename(d), cand, rd["gates"])


def views_list(a):
    fx = json.load(open(PL.FIXTURE, encoding="utf-8"))
    con = PL.load_contour(RD)
    lines_l = []
    all_cams = {}
    for d in sorted(glob.glob(os.path.join(a.run, "*_*"))):
        if not os.path.isdir(d) or not os.path.exists(os.path.join(d, "plan.json")):
            continue
        seed, cls = os.path.basename(d).split("_")
        pl, val, att = PL.instantiate_valid(fx, int(seed), cls, con, V.validate)
        lats = []
        for cand in ("C0", "UF", "RF"):
            p = os.path.join(d, f"{cand}.f32")
            if os.path.exists(p):
                lats.append((f"{seed}_{cls}_{cand}", p))
        hs = [load_f32(p) for _, p in lats]
        cams, flagged = VW.camera_list(pl, con, hs)
        all_cams[f"{seed}_{cls}"] = {"cameras": cams, "flagged": flagged}
        lst = os.path.join(a.out_dir, f"views_{seed}_{cls}.txt")
        h4a = os.path.join(RD, "g3f", "contour", "h4a_110201.f32")
        VW.write_unity_list(lst, [(lab, os.path.abspath(p).replace(os.sep, "/")) for lab, p in lats] + [(f"{seed}_{cls}_H4A", h4a)], cams)
        lines_l.append(lst)
    dump(os.path.join(a.out_dir, "cameras.json"), all_cams)
    print("\n".join(lines_l))


def sheets(a):
    os.makedirs(a.out, exist_ok=True)
    sealed = os.path.join(a.out, "sealed")
    os.makedirs(sealed, exist_ok=True)
    cams = json.load(open(os.path.join(a.renders, "..", "cameras.json"), encoding="utf-8")) if os.path.exists(os.path.join(a.renders, "..", "cameras.json")) else None
    key = ["G3f blind key - which candidate is X, Y, Z on each variant. Sealed: not printed on any sheet."]
    index = ["G3f contact sheets: top left X, top right Y, bottom left Z (candidates in a seeded order, key sealed), bottom right production H4A 110201 (labelled reference, not rated)."]
    for var, cd in sorted(cams.items()):
        seed, cls = var.split("_")
        order = VW.sealed_order(int(seed), cls)
        key.append(f"{var}: " + ", ".join(f"{k} = {v}" for k, v in sorted(order.items())))
        for c in cd["cameras"]:
            panels = [os.path.join(a.renders, f"{var}_{order[k]}_{c['id']}.png") for k in ("X", "Y", "Z")]
            panels.append(os.path.join(a.renders, f"{var}_H4A_{c['id']}.png"))
            outp = os.path.join(a.out, f"{var}_{c['scale']}_{c['id']}.png")
            VW.sheet(outp, panels, ["X", "Y", "Z", "H4A"])
            index.append(f"{os.path.basename(outp)}: {c['placement']}")
    open(os.path.join(sealed, "blind_key.txt"), "w", encoding="utf-8").write("\n".join(key) + "\n")
    open(os.path.join(a.out, "index.txt"), "w", encoding="utf-8").write("\n".join(index) + "\n")
    print("sheets written; key sealed at", sealed)


def summary(a):
    rows = []
    for run in a.runs:
        for d in sorted(glob.glob(os.path.join(run, "*_*"))):
            for rf in sorted(glob.glob(os.path.join(d, "*_readings.json"))):
                rd = json.load(open(rf, encoding="utf-8"))
                asm = os.path.join(d, rd["candidate"] + "_assembly.json")
                A1 = json.load(open(asm)) if os.path.exists(asm) else None
                rows.append((os.path.basename(d), rd["candidate"], rd["gates"], A1, rd.get("flags", [])))
    for r in rows:
        print(r[0], r[1], r[2], "A1", (r[3] or {}).get("local"), (r[3] or {}).get("order_invariant", (r[3] or {}).get("relabel_invariant")), "flags", len(r[4]))


def manifest(a):
    lines = []
    for root, _, files in os.walk(a.dir):
        for f in sorted(files):
            if f == "SHA256SUMS.txt":
                continue
            p = os.path.join(root, f)
            lines.append(f"{sha256_file(p)} *{os.path.relpath(p, a.dir).replace(os.sep, '/')}")
    lines.sort(key=lambda s: s.split("*", 1)[1])
    open(os.path.join(a.dir, "SHA256SUMS.txt"), "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
    print(len(lines), "files")


def main():
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    v = sub.add_parser("variant"); v.add_argument("--seed", type=int, required=True); v.add_argument("--class", dest="cls", required=True)
    v.add_argument("--out", required=True); v.add_argument("--candidates", default="C0,UF,RF"); v.add_argument("--no-assembly", action="store_true")
    h = sub.add_parser("hydro-list"); h.add_argument("--runs", nargs="+", required=True); h.add_argument("--out", required=True)
    m = sub.add_parser("merge-hydro"); m.add_argument("--run", required=True); m.add_argument("--hydro", required=True)
    w = sub.add_parser("views-list"); w.add_argument("--run", required=True); w.add_argument("--out-dir", required=True)
    s = sub.add_parser("sheets"); s.add_argument("--renders", required=True); s.add_argument("--out", required=True)
    u = sub.add_parser("summary"); u.add_argument("--runs", nargs="+", required=True)
    f = sub.add_parser("manifest"); f.add_argument("--dir", required=True)
    a = ap.parse_args()
    {"variant": variant, "hydro-list": hydro_list, "merge-hydro": merge_hydro, "views-list": views_list, "sheets": sheets,
     "summary": summary, "manifest": manifest}[a.cmd](a)


if __name__ == "__main__":
    main()
