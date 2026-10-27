"""Synthetic plan rasters for the analytic checks (spec §14): a cone, a straight ridge between two units with
coastal fringes, a plateau block, an asymmetric divide (assembly), a valley with a tributary."""
import math

import numpy as np

from g3f.lattice import C, raster4, raster_polyline4, components, dilate
from g3f import synth as S


def _base(n0, n1, land):
    R = S.Raster(n0, n1, C)
    R.land = land.copy()
    R.fixed[~land] = 10.0       # sea
    return R


def _shore(land):
    """Land vertices 8-adjacent to the sea (roots: no land vertex can drain to the sea past them)."""
    return land & dilate(~land, 1)


def _fringes(R, width_v, outlet_masks, intent_of=None):
    """Coastal fringe: land within width_v vertices of the sea, minus outlet zones; its crest = fringe vertices
    8-adjacent to the inland part. Fringe units: 8-components of fringe minus crest. Returns the inland mask."""
    land = R.land
    from g3f.lattice import edt
    dsea = edt(~land, 1.0)
    band = land & (dsea <= width_v)
    for m in outlet_masks:
        band &= ~m
    inland = land & ~band
    crest = band & dilate(inland, 1) & ~inland
    fr = band & ~crest
    return band, crest, fr, inland


def cone(n=161, radius=70, summit=100.0):
    zz, xx = np.mgrid[0:n, 0:n]
    c = n // 2
    r = np.hypot(xx - c, zz - c)
    land = r <= radius
    R = _base(n, n, land)
    R.unit[land] = 0
    R.units = [{"kind": S.CATCHMENT, "name": "cone"}]
    R.root = _shore(land)
    R.region[land] = S.LOWLAND
    R.region[land & (r <= 0.9 * radius)] = S.UPLAND
    R.region[land & (r <= 0.8 * radius)] = S.MOUNTAIN
    return R, (c, c, summit, 150.0)


def ridge(n0=101, n1=161, crest_h=80.0, fringe_w=10, outlet_half=8):
    """Land rectangle; a straight divide along z = n0 // 2 from coast to coast; unit 0 south, unit 1 north, each
    draining to an outlet segment in the middle of its coast; coastal fringes elsewhere."""
    land = np.zeros((n0, n1), bool)
    land[5:n0 - 5, 5:n1 - 5] = True
    R = _base(n0, n1, land)
    zc = n0 // 2
    div = np.zeros_like(land)
    div[zc, 5:n1 - 5] = True
    xc = n1 // 2
    out_s = np.zeros_like(land); out_s[5:5 + fringe_w + 3, xc - outlet_half:xc + outlet_half + 1] = True
    out_n = np.zeros_like(land); out_n[n0 - 5 - fringe_w - 3:n0 - 5, xc - outlet_half:xc + outlet_half + 1] = True
    band, fcrest, fr, inland = _fringes(R, fringe_w, [out_s | div, out_n | div])
    # remove the divide line from everything else
    fcrest &= ~div
    fr &= ~div
    core = land & ~band & ~div
    core |= (out_s | out_n) & land & ~div
    labs, k = components(core & ~fcrest, 8)
    R.units = []
    R.crests = [{"kind": S.DIVIDE, "name": "ridge", "plan": None}, {"kind": S.FRINGE_CREST, "name": "fringe-crest", "floor": 1.0}]
    south = labs[zc - 10, xc]
    north = labs[zc + 10, xc]
    R.unit[labs == south] = 0
    R.unit[labs == north] = 1
    R.units += [{"kind": S.CATCHMENT, "name": "south"}, {"kind": S.CATCHMENT, "name": "north"}]
    flab, fk = components(fr, 8)
    for f in range(fk):
        R.unit[flab == f] = len(R.units)
        R.units.append({"kind": S.FRINGE, "name": f"fringe{f}"})
    R.crest[div] = 0
    R.crest[fcrest] = 1
    shore = _shore(land)
    R.root = shore & (R.unit >= 0)
    # catchment roots: their own shore vertices (outlet segments); fringe roots: their shore vertices
    R.region[land] = S.UPLAND
    R.region[land & (np.abs(np.arange(n0)[:, None] - zc) <= 15)] = S.MOUNTAIN
    R.region[fr | fcrest] = S.COAST_LOW
    R.region[(fr | fcrest) & (np.arange(n1)[None, :] < 40)] = S.COAST_CLIFF
    R.exempt = (R.region == S.COAST_CLIFF) & (R.unit >= 0)
    # planned crest profile (R-F): rises from both coasts to crest_h at the middle
    plan = np.full(n0 * n1, np.nan)
    for x in range(5, n1 - 5):
        t = 1.0 - abs(x - xc) / (xc - 5)
        plan[zc * n1 + x] = 24.5 + (crest_h - 24.5) * min(1.0, 1.2 * t)
    R.crests[0]["plan"] = plan
    return R, (xc, zc - 3, crest_h, 60.0)


def plateau_block(n0=161, n1=161, edge=40.0):
    """One catchment (outlet in the middle of the south coast, fringes elsewhere) with one plateau: top disc,
    escarpment band, a rim channel south, a ramp east then south to a landing, ramp side bands."""
    land = np.zeros((n0, n1), bool)
    land[5:n0 - 5, 5:n1 - 5] = True
    R = _base(n0, n1, land)
    xc = n1 // 2
    out_s = np.zeros_like(land); out_s[5:20, xc - 8:xc + 9] = True
    band_f, fcrest, fr, inland = _fringes(R, 8, [out_s])
    core = land & ~band_f
    core |= out_s & land
    labs, k = components(core & ~fcrest, 8)
    R.unit[labs == labs[80, 80]] = 0
    R.units = [{"kind": S.CATCHMENT, "name": "c"}]
    flab, fk = components(fr, 8)
    for f in range(fk):
        R.unit[flab == f] = len(R.units)
        R.units.append({"kind": S.FRINGE, "name": f"fringe{f}"})
    R.crests = [{"kind": S.FRINGE_CREST, "name": "fringe-crest", "floor": 1.0}]
    R.crest[fcrest] = 0
    R.root = _shore(land) & (R.unit >= 0)
    R.region[land] = S.UPLAND
    R.region[fr | fcrest] = S.COAST_LOW
    # plateau
    zz, xx = np.mgrid[0:n0, 0:n1]
    pc = (80, 100)  # (x, z)
    rr = np.hypot(xx - pc[0], zz - pc[1])
    top = (rr <= 16) & (R.unit == 0)
    band = (rr > 16) & (rr <= 16 + 9) & (R.unit == 0)
    # rim channel: 3 wide strip going south from the top through the band
    rimch = band & (np.abs(xx - pc[0]) <= 1) & (zz < pc[1])
    # ramp: from the top edge east to x = pc[0] + 40, then south to z = pc[1] - 40
    poly = [(pc[0] + 16, pc[1]), (pc[0] + 40, pc[1]), (pc[0] + 40, pc[1] - 40)]
    line = raster_polyline4(poly)
    lm = np.zeros_like(land)
    for x, z in line:
        lm[z, x] = True
    ramp = dilate(lm, 2) & (R.unit == 0) & ~top
    ramp_side = dilate(ramp, 4) & (R.unit == 0) & ~ramp & ~top & ~band
    ramp_side &= ~(zz < pc[1] - 36)          # the landing end stays open (a ramp drains to its landing)
    band &= ~ramp & ~rimch
    R.zone[top] = S.Z_TOP
    R.zone[band] = S.Z_BAND
    R.zone[rimch] = S.Z_RIMCH
    R.zone[ramp] = S.Z_RAMP
    R.zone[ramp_side] = S.Z_RSIDE
    fp = top | band | rimch | ramp | ramp_side
    R.plateau_of[fp] = 0
    R.region[top] = S.TOP
    R.region[band | rimch | ramp_side] = S.BAND
    R.region[ramp] = S.RAMP
    R.exempt = (band | ramp_side | rimch)
    R.plateaus = [{"edge": edge, "class": "tall", "ramp_poly": poly, "ramp_top_vertex": poly[0],
                   "landing_vertex": (pc[0] + 40, pc[1] - 43)}]
    return R, top, band, ramp


def asym_divide(n0=121, n1=161):
    """Two catchments separated by a diagonal divide; unit 0 (west) larger; both outlets on the south coast;
    no fringes (roots = each unit's own shore vertices). A planned crest profile for R-F."""
    land = np.zeros((n0, n1), bool)
    land[5:n0 - 5, 5:n1 - 5] = True
    R = _base(n0, n1, land)
    line = raster4((95, 5), (125, n0 - 6))
    div = np.zeros_like(land)
    for x, z in line:
        div[z, x] = True
    labs, k = components(land & ~div, 8)
    R.unit[labs == labs[60, 40]] = 0
    R.unit[labs == labs[60, 150]] = 1
    R.units = [{"kind": S.CATCHMENT, "name": "west"}, {"kind": S.CATCHMENT, "name": "east"}]
    R.crest[div] = 0
    R.crests = [{"kind": S.DIVIDE, "name": "d", "plan": None}]
    shore = _shore(land)
    # outlets: south coast segments only; the rest of the shore is not a root (no fringes here) -> roots only at
    # outlet segments; vertices next to the sea elsewhere still drain by the tree to the outlet
    R.root = shore & (R.unit >= 0)
    R.region[land] = S.UPLAND
    plan = np.full(n0 * n1, np.nan)
    for x, z in line:
        t = (z - 5) / (n0 - 11)
        plan[z * n1 + x] = 25.0 + 60.0 * math.sin(math.pi * t)
    R.crests[0]["plan"] = plan
    return R


def valley(n0=141, n1=141):
    """One catchment draining to an outlet segment in the middle of the south coast, coastal fringes elsewhere; a
    trunk axis from the north to the outlet and a tributary from the north-east joining it; mountain flanks, a
    corridor along the axes."""
    land = np.zeros((n0, n1), bool)
    land[5:n0 - 5, 5:n1 - 5] = True
    R = _base(n0, n1, land)
    xc = n1 // 2
    out_s = np.zeros_like(land); out_s[5:20, xc - 8:xc + 9] = True
    band_f, fcrest, fr, inland = _fringes(R, 8, [out_s])
    core = (land & ~band_f) | (out_s & land)
    labs, k = components(core & ~fcrest, 8)
    R.unit[labs == labs[70, 70]] = 0
    R.units = [{"kind": S.CATCHMENT, "name": "v"}]
    flab, fk = components(fr, 8)
    for f in range(fk):
        R.unit[flab == f] = len(R.units)
        R.units.append({"kind": S.FRINGE, "name": f"fringe{f}"})
    R.crests = [{"kind": S.FRINGE_CREST, "name": "fringe-crest", "floor": 1.0}]
    R.crest[fcrest] = 0
    R.root = _shore(land) & (R.unit >= 0)
    trunk = [(xc, n0 - 20), (xc, 60), (xc - 5, 30), (xc, 6)]
    trib = [(n1 - 20, n0 - 25), (xc + 25, 80), (xc, 60)]
    R.axes = [trunk, trib]
    from g3f.checks import axis_sequence
    am = np.zeros_like(land)
    for poly in R.axes:
        for x, z in axis_sequence(poly):
            am[z, x] = True
    R.region[land] = S.MOUNTAIN
    R.region[land & dilate(am, 4)] = S.CORRIDOR
    R.region[fr | fcrest] = S.COAST_LOW
    return R, [trunk, trib]
