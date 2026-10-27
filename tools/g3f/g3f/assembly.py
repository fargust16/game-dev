"""GF-A on the sector (spec §9.3): (a) order - GF-RF's units synthesised in two orders, GF-UF's units relabelled, must
give byte-identical lattices; (b) locality - one catchment's own law factor changed by x 1.10 may change no vertex
outside that catchment, its coastal fringe units (their parent, spec §4.3) and the fringe-crest vertices between
them."""
import copy

import numpy as np

from . import synth as S
from . import run as RU


def _f32(a):
    return np.asarray(a, "<f4")


def assembly(plan, P, candidate, seed, base_lat, perturb_unit="A", factor=1.10):
    R = plan["raster"]
    names = [u["name"] for u in R.units]
    nodes = RU.plan_nodes(plan)
    out = {"candidate": candidate}
    if candidate == "RF":
        order = list(range(len(R.units)))[::-1]
        lat2, _, _ = S.synthesize(R, P, "RF", seed=seed, secondary=nodes, unit_order=order)
        out["order_invariant"] = bool(np.array_equal(_f32(lat2), _f32(base_lat)))
    else:
        perm = list(range(len(R.units)))[::-1]
        inv = {old: new for new, old in enumerate(perm)}
        R2 = copy.copy(R)
        R2.unit = np.where(R.unit >= 0, np.vectorize(lambda u: inv.get(int(u), -1))(R.unit), -1).astype(np.int32)
        R2.units = [R.units[o] for o in perm]
        R2.root = R.root
        lat2, _, _ = S.synthesize(R2, P, "UF", seed=seed, target=RU.sector_target(plan), secondary=nodes[1:])
        out["relabel_invariant"] = bool(np.array_equal(_f32(lat2), _f32(base_lat)))
    ui = names.index(perturb_unit)
    extra = {ui: factor}
    if candidate == "RF":
        lat3, _, _ = S.synthesize(R, P, "RF", seed=seed, secondary=nodes, unit_extra=extra)
    else:
        lat3, _, _ = S.synthesize(R, P, "UF", seed=seed, target=RU.sector_target(plan), secondary=nodes[1:], unit_extra=extra)
    diff = _f32(lat3) != _f32(base_lat)
    allowed = R.unit == ui
    for fi, ud in enumerate(R.units):
        if ud["kind"] == S.FRINGE and ud.get("parent") == perturb_unit:
            allowed |= R.unit == fi
    fcrest = np.zeros_like(allowed)
    for ci, c in enumerate(R.crests):
        if c["kind"] == S.FRINGE_CREST:
            fcrest |= R.crest == ci
    from .lattice import dilate
    allowed |= fcrest & dilate(R.unit == ui, 2)
    out["perturbed_unit"] = perturb_unit
    out["changed_inside"] = int((diff & allowed).sum())
    out["changed_outside"] = int((diff & ~allowed).sum())
    out["local"] = out["changed_outside"] == 0
    return out
