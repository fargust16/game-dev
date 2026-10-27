"""G3f variant runner: plan -> context (GF-C0 on the coarse whole-island units) -> one candidate on the sector,
embedded in the world lattice. Specification §§7, 11, 13."""
import math
import time

import numpy as np

from . import synth as S
from .lattice import C, BASE


def context_lattice(plan, P, seed):
    """The context surface: GF-C0 on the coarse units with the Sector crest planned; the main culmination's interior
    neighbourhood scaled to its planned height. Identical for every candidate of a variant."""
    ctx = plan["context"]
    R = ctx["raster"]
    m1 = plan["nodes"]["M1"]
    c_m1 = plan["raster"].crests[0]["plan"][m1[1] * R.n1 + m1[0]]
    t0 = time.time()
    # the context never rises above the main culmination: a ceiling cone M1 - 2 m - 0.02 x distance [A]
    zz, xx = np.mgrid[0:R.n0, 0:R.n1]
    ceil = c_m1 - 2.0 - 0.02 * np.hypot(xx - m1[0], zz - m1[1]) * C
    lat, log, T = S.synthesize(R, P, "C0", seed=seed, target=(m1[0], m1[1], c_m1, 150.0), ceiling=ceil)
    log["seconds"] = time.time() - t0
    return lat, log


def embed(plan, context_lat):
    """The sector raster's fixed heights: the context outside the sector, Stage 1 sea heights at sea."""
    R = plan["raster"]
    land_ctx = ~np.isnan(context_lat) & ~plan["sector"] & (R.crest < 0)
    fixed = R.fixed.copy()
    fixed[land_ctx] = context_lat[land_ctx]
    R.fixed = fixed
    return R


def sector_target(plan):
    """Bisection target for GF-C0 and GF-UF (L-4): the main culmination's planned height, read on interior vertices
    within 150 m of it."""
    R = plan["raster"]
    m1 = plan["nodes"]["M1"]
    return (m1[0], m1[1], float(R.crests[0]["plan"][m1[1] * R.n1 + m1[0]]), 150.0)


def plan_nodes(plan):
    """The plan's height nodes for the laws (x, z, target, reading radius, factor radius): M1, M2 (150 m readings,
    300 m factors) and the declared pass S1 (its interior within 30 m, a 100 m factor)."""
    n, t = plan["nodes"], plan["targets"]
    return [(n["M1"][0], n["M1"][1], t["M1"], 150.0, 300.0), (n["M2"][0], n["M2"][1], t["M2"], 150.0, 300.0),
            (n["S1"][0], n["S1"][1], t["S1"], 30.0, 100.0)]


def run_candidate(plan, P, candidate, seed, T=None):
    R = plan["raster"]
    t0 = time.time()
    nodes = plan_nodes(plan)
    if candidate == "RF":
        lat, log, T = S.synthesize(R, P, "RF", seed=seed, T=T, secondary=nodes)
    elif candidate == "UF":
        lat, log, T = S.synthesize(R, P, "UF", seed=seed, target=sector_target(plan), T=T, secondary=nodes[1:])
    else:
        lat, log, T = S.synthesize(R, P, "C0", seed=seed, target=sector_target(plan), T=T)
    log["seconds"] = time.time() - t0
    return lat, log, T
