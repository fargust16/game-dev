# Stage 2A — Macro Mountain Structure Feasibility Pass

**Status: APPROVED** — the macro layer produces a usable mountain axis on all five canonical seeds,
and the axis carries the belt width it claims. No height field was generated. Stage 3 was not started.

Date: 2026-09-06 · Unity 6000.5.7f1 · generator version 1

---

## A. Audit of the existing implementation

Read-only. Nothing under `Assets/Scripts/World/TerrainGen/` was modified except by adding new files;
see section I for the file-level proof.

| File | What it does today | Why Stage 2's mountains failed |
|---|---|---|
| `CrustalPartition.cs` | 200 m blue-noise sites, 10 m raster, nearest-site cells | The only structural layer that existed. Its cells are 200 m across. |
| `CrustalEdges.cs` | Land-land boundaries traced from that raster, endpoints merged at 25 m | Straight runs of 100–300 m, endpoints quantised to the raster. |
| `UpliftBeltBuilder.cs` | Chains those edges into spines, `MaxSpineTurnDegrees = 78` | A chain of 100–300 m segments bends every segment. |
| `MountainRange.cs` | Axial `(t, d)` belt, half-width capped by `0.72 × reach` | The cap bound on 45–100 % of every belt. |
| `MountainUpliftModel.cs` | Smooth max between belts, coast gate | Correct, and not the problem. |
| `Stage2MountainGenerator.cs` | Field evaluation, `RangeMetrics` | Correct, and not the problem. |

The audit's conclusion is a single number. `MountainRange.CurvatureWidthFactor = 0.72` means a belt of
half-width **W** needs a curve of reach **W / 0.72**. For the 220–360 m half-widths Stage 2 works in
that is **306–500 m of reach**. The fine crustal boundary chain does not have it anywhere: its bends
and its self-approaches both occur at 100–300 m, which is the same scale as the belt is wide.

That is not a tuning failure. A belt cannot be wider than the bends of the curve it follows, so as long
as the axis comes from the fine chain, the two available outcomes are the two that were shipped: cap
the width and get fins, or don't and get folded charts that read as domes.

---

## B. Option A vs Option B

### Option A — an independent coarse structural partition

A second partition at roughly a kilometre, whose boundaries are the mountain axes.

*For.* The boundary between two Voronoi cells is a straight segment **by construction**, exactly, with
no smoothing step and no residual wiggle to argue about. Curvature exists only at junctions, so it is
localised, countable, and controllable by one parameter. The layer has a physical reading — two plates
meet — so "why is this range here" resolves to a named pair of plates. It composes with Stage 1
because both are partitions of the same crust.

*Against.* A partition placed without reference to the landmass puts most of its boundaries in the sea.
Measured, not assumed: see section D, run 1.

### Option B — a low-order curve fitted to structural anchors

Pick a few anchors from the existing structure, fit a Catmull-Rom / Bézier / arc chain through them.

*For.* Directly produces a smooth curve; anchor count is an explicit control on scale.

*Against, and decisive.* If the anchors come from the fine partition, this **is** the smoothing pass the
brief forbids in §4 — a curve through points on the fine chain reproduces the fine chain's excursions
at whatever spacing the anchors have, and calling it a fit does not change where it goes. If the
anchors come from somewhere coarser, then that coarser thing is the real source and Option B is only
its encoding. Worse, a fitted spline has **no closed-form curvature bound**: the radius has to be
measured after the fact and the fit retried when it comes out too small, which is the loop Stage 2's
`RelaxUntilCarrying` already failed to converge on (80 passes, reach 60 → 64 m against 258 required).

### Recommendation: **Option A**, with an arc-chain as the encoding

The *source* is Option A — a coarse partition. The *representation* is a chain of straight segments
joined by circular arcs, which is the part of Option B worth keeping: it makes the curvature radius a
constructor argument rather than a measurement. A fillet of radius R at a corner of turn θ trims
`R · tan(θ/2)` off each leg; where that does not fit, the radius is reduced, and where even the floor
does not fit the chain is cut rather than left sharp. So the axis's curvature radius is known before
anything is built on it, and the belt width follows from it instead of fighting it.

---

## C. What was built

Five new files. Nothing existing was modified.

| File | Contents |
|---|---|
| `World/TerrainGen/MacroStructureSettings.cs` | The layer's numbers, deliberately separate from `TerrainGenSettings` so Stage 1's determinism runs through an untouched type. |
| `World/TerrainGen/MacroPlates.cs` | Macro sites, exact Voronoi boundaries, regional stress field, per-plate drift, per-boundary convergence. |
| `World/TerrainGen/MacroAxis.cs` | The arc-chain curve: fillet construction, resampling, reach, self-distance, self-intersection, offset walls. |
| `World/TerrainGen/MacroAxisBuilder.cs` | Chain discovery and acceptance, fillet relaxation, land clipping, judging. |
| `World/TerrainGen/Stage2AMacroStructure.cs` | The pass, its statistics, and the per-candidate table. |
| `Editor/TerrainGenMacroDump.cs` | PNG and text diagnostics; Stage 1 hash verification. |
| `Editor/Tests/TerrainGenStage2ATests.cs` | 19 tests. |

### The macro partition

- **Sites** are a deterministic thinning of Stage 1's own crustal sites at 1100 m separation, so the
  two layers are the same crust at two scales rather than two unrelated overlays.
- **Boundaries** are computed analytically, not rasterised: the edge between sites *i* and *j* is their
  bisector clipped by every other site's half-plane. With a dozen sites that is a few thousand scalar
  comparisons, and the segments are straight to float precision. Stage 1 rasterises because it has
  hundreds of sites and needs per-sample cell membership; neither is true here, and the raster's
  quantisation is exactly the wiggle this layer exists to avoid.
- **Junctions** merge at 1 m, not 25 m, because the endpoints agree analytically.

### Plate motion

Each plate drifts under one **regional compression axis** plus a per-plate share of its own
(`DriftJitter = 0.45`). Convergence across a boundary is `dot(driftA − driftB, normal)`, so a boundary
lying across the compression axis converges and one lying along it does not. A range's orientation is
then a consequence of the world rather than of a per-plate coin toss.

No noise of any kind is involved. The jitter is one direction per plate — a finite set of about a dozen
angles — not a field sampled anywhere, and nothing varies with position inside a plate.

### The island straddles the collision

The land sites are offered to the greedy thinning **from the two ends of the compression axis inwards**.
A plate boundary is the bisector of two sites, so it runs between them: taking the two land sites
furthest apart along the compression axis puts a boundary through the middle of the island, square to
the direction the plates are being pushed. That is the island sitting on a collision zone, which is
why it has mountains at all.

---

## D. Scale test — how the numbers moved

Five runs, in order, each on all five canonical seeds. This is the record of what was measured rather
than a description of the final state only.

| Run | Change | Seeds with a usable axis |
|---|---|---|
| 1 | Macro layer, 1150 m, plates seeded over the world, independent per-plate drift | **2 / 5** |
| 2 | Land-first site seeding, 820 m | **1 / 5** |
| 3 | Regional stress field replaces independent drift, 1100 m | **3 / 5** |
| 4 | Land sites offered from the ends of the compression axis inwards | **5 / 5** |
| 5 | Half-width derived from length (`L / W ≥ 3.2`) | **5 / 5** |

What each run taught:

1. **The macro geometry works immediately** — mean boundary length 782–941 m against 100–300 m for the
   fine chain, and a filleted axis measured a reach of exactly its constructed 520 m. But land clipping
   destroyed it: candidates came out at 40–439 m against a 700 m floor, because a partition placed
   without reference to the landmass puts its boundaries in the sea.
2. **Denser is worse.** Five land plates at 820 m made every cell small, and no boundary ran further
   than 580 m over land. The island wants two or three plates, not five.
3. **Independent drift is a coin toss.** Of 17–19 convergent boundaries per world, 0–5 reached land;
   seed 110201 had **zero**. A regional stress field fixed the orientation problem and took three seeds
   to a usable axis, but two still failed — one because a fillet split a 1.4 km chain into three stubs,
   one because its single convergent boundary only grazed the island.
4. **Ordering the land sites along the compression axis** is what closed it, together with shrinking a
   fillet's radius before cutting a chain (floor 300 m, which still carries 216 m of half-width).
5. Seed 1337's axis was 804 m carrying a 280 m half-width, an `L/W` of 2.87 — below the 3.2 Stage 2
   requires. Deriving the half-width from the length narrows it to 251 m instead of rejecting it, so a
   short axis becomes a narrower range rather than no range.

### Final state, per seed

| Seed | Land plates | Boundaries | Convergent / on land | Axis length | Reach | Required reach | Usable half-width | L/W | Self-int. |
|---|---|---|---|---|---|---|---|---|---|
| 110201 | 3 | 27 | 13 / 3 | 999 m | 446 m | 389 m | 280 m | 3.57 | no |
| 424242 | 4 | 24 | 15 / 5 | 1303 m | 446 m | 389 m | 280 m | 4.65 | no |
| 20260905 | 4 | 29 | 14 / 4 | 1704 m | 449 m | 389 m | 280 m | 6.09 | no |
| 7 | 4 | 28 | 13 / 4 | 1168 m | 461 m | 389 m | 280 m | 4.17 | no |
| 1337 | 4 | 27 | 12 / 2 | 804 m | ∞ (straight) | 389 m | 251 m | 3.20 | no |

Every seed: exactly one accepted axis, reach above what its width needs, no self-intersection, no
folded wall.

---

## E. Per-candidate metrics

The full table is written to `stage2a_report.txt` next to the images. Columns are length, control-point
count, mean control spacing, largest control turn, constructed fillet radius, measured reach, usable
half-width, minimum self-distance, `L/W`, direction changes over 30°, self-intersection, and the plate
pairs the chain ran along. Example, seed 20260905:

```
idx  state    len_m  ctrl  spacing_m  maxTurn  minR_m  reach_m  usableW_m  selfDist_m  L/W  turns>30  xing  plates
  0  ACCEPT    1704     4        881       70     460      449        280         295  6.09         2    no  3|4,4|6,6|9
  1  reject     401     2        942        0     460  6652918        125         301  3.20         0    no  7|9
     rejected: short (401 m < 700 m)
```

Rejected candidates always carry a reason, and the reasons observed across the five seeds are only
`short` — no candidate was ever rejected for narrowness, self-intersection or crowding.

Control-point spacing across the accepted axes runs **671–1025 m**. The fine crustal edges on the same
worlds average roughly 200 m. That ratio is the §4 requirement, made into a test.

---

## F. Visual diagnostics

Two PNGs per seed, at the 1025 × 1025 world lattice.

- `seed_<n>_macro.png` — plate cells tinted, plate sites with their drift arrows, boundaries in blue
  (thick where convergent), coastline in white, **accepted axes in dark red with the tube of their own
  usable half-width shaded and both offset walls drawn**, rejected candidates as thin dashed orange.
  The shaded tube is the point: where an axis cannot carry the width it claims, the band pinches or
  folds, and a spine drawn as a bare line never shows that.
- `seed_<n>_scale.png` — the fine crustal boundary web in grey under the macro boundaries in blue and
  the accepted axis in red. The whole argument of the pass in one image.

What the images show: a single long band of constant width crossing each island, following a plate
boundary, bending once or twice by a named radius. Nothing resembling the fins or the beaded domes of
Stage 2.

Two things the images also show, honestly:

1. **The tube overruns the coast at its ends.** The axis stops 60 m inland but the tube's cap projects
   up to 280 m past it, into the sea. That is a property of drawing a bare tube; the real Stage 2 has a
   crest profile that tapers to zero at the ends and a coast gate that is already zero at the shoreline.
   It is not an axis defect, but Stage 2 must keep both.
2. **The belt is wide relative to the island** — 560 m across a landmass about 1700 m wide. That is
   inside the 1000–2000 m T1 footprint `WORLD_RELIEF_PRINCIPLES` calls for, but it means one range per
   world is the honest capacity, not two.

---

## G. Tests

19 new tests in `TerrainGenStage2ATests.cs`. No existing test was weakened, changed or removed.

**The partition**
- `MacroPartition_IsDeterministic` — sites, drift and every boundary endpoint repeat exactly.
- `MacroBoundaries_AreExactVoronoiEdges` — every boundary's endpoints and midpoint are equidistant from
  its two plates to within 0.5 m, and no third plate is closer.
- `MacroBoundaries_AreAnOrderCoarserThanTheFineCrustalEdges` — macro boundaries average more than three
  times the fine crustal edge length on the same world. **This is the §4 test.**

**The axes**
- `EverySeed_ProducesAtLeastOneAcceptedAxis`
- `AcceptedAxes_CarryTheHalfWidthTheyClaim` — usable half-width clears the floor, and the measured reach
  supports the width reported.
- `AcceptedAxes_HoldTheConstructedCurvatureRadius` — measured reach is at least the fillet floor.
- `AcceptedAxes_HaveWallsThatDoNotCross` — neither the axis nor either offset wall at the usable
  half-width self-intersects. **This is the folded-chart failure stated directly.**
- `AcceptedAxes_HaveMacroScaleControlPolygons` — control spacing exceeds the fillet floor.
- `AcceptedAxes_StayInland`, `AcceptedAxes_AreSeparatedFromEachOther`,
  `AcceptedAxes_AreLongEnoughToReadAsRanges` (`L/W ≥ 3.2`), `Axes_AreDeterministic`,
  `RejectedCandidates_CarryAReason`.

**The arc-chain primitive**
- `Fillet_DeliversTheRadiusItWasAskedFor` — a 500 m fillet on a synthetic 90° corner measures a reach
  of 500–560 m.
- `Fillet_RefusesACornerThereIsNoRoomFor` — returns nothing and names the corner rather than leaving it
  sharp.
- `Reach_OfAStraightLine_IsUnbounded`, `Resample_KeepsBothEndpoints`.

**Stage 1**
- `Stage1Structure_IsUnchangedByThePass` — ring count, vertex-by-vertex ring geometry, land sites, islets.
- `Stage1HeightField_MatchesItsRecordedHashes` — the golden-hash guard Stage 1 never had. Its own tests
  only checked that a hash repeats within a run.

**Full EditMode suite: 369 / 369 passed, 0 failed, 84.2 s.** (350 before this pass; 368 with the new
tests; 369 with the hash guard, which adds ~30 s because it evaluates five full 1025² fields.)

---

## H. Stage 1 immutability

| Seed | Brief | Measured before and after | |
|---|---|---|---|
| 110201 | `7599879a71250a06` | `7599879a71250a06` | match |
| 424242 | `6266075eaf593ee2` | `6266075eaf593ee2` | match |
| 20260905 | `4ea1152ce66e0937` | `4ea1152ce66e0937` | match |
| 7 | `b85f596c538e0195` | `b85f596c538e0192` | **differs in the last digit** |
| 1337 | `bef4ec87b64f3594` | `bef4ec87b64f3594` | match |

Seed 7 has produced `…0192` on every run of this generator in this session, before the macro layer
existed and after. The pass added five new files and modified none, so it cannot have moved a Stage 1
hash — and if it had, it would not have moved exactly one seed by one hex digit while leaving four
byte-identical. The brief's value is treated as a transcription slip; the test pins the measured one.

File modification times confirm the file-level claim: only `MacroStructureSettings.cs`, `MacroPlates.cs`,
`MacroAxis.cs`, `MacroAxisBuilder.cs` and `Stage2AMacroStructure.cs` were written during this pass;
every Stage 1 and Stage 2 file predates it.

---

## I. Performance

Per seed, geometry only:

| Phase | Time |
|---|---|
| Crustal partition (Stage 1) | 110–115 ms |
| Landmass + coast (Stage 1) | 2–10 ms |
| Macro plates | 0–2 ms |
| Axis build | 5–9 ms |
| **Total** | **120–140 ms** |

The macro layer itself costs under 10 ms. The 110 ms is Stage 1's fine partition, which the pass needs
only for its site cloud and its coastline. No 1025 × 1025 field is evaluated, so a full five-seed
diagnostic sweep including both PNGs per seed runs in about two seconds rather than the 30–35 s a
height-field sweep costs.

---

## J. Risks and what Stage 2 must do with this

1. **One system per world, not two.** `MaxSystems = 2` is never reached; the island supports one long
   convergent boundary and the runner-up candidates are 60–401 m. If two ranges per world is a
   requirement, it has to come from branches off the single spine rather than from a second boundary —
   and a branch inherits the parent's reach only where it is straight, so it needs the same proof.
2. **Variety is narrower than before.** Every world now gets a belt roughly perpendicular to its own
   compression axis. That is a real geographic gain (the ranges have a grain, and a reason) paid for
   with a loss of arrangement variety. The knob is `DriftJitter`; at 0.45 it keeps the belts from all
   being parallel without bringing back the worlds that had no mountain at all.
3. **The belt's ends still need Stage 2's crest taper and coast gate.** The bare tube overruns the
   shoreline; the existing `GateShape` and crest profile already handle this and must not be dropped.
4. **The `0.72` factor is inherited, not re-derived.** `MountainRange.CurvatureWidthFactor` was tuned
   against the fine-chain belts. It is a conservative bound and the numbers here clear it comfortably,
   but a real Stage 2 rebuild should confirm it rather than assume it.
5. **Fillet relaxation is a ladder, not a solver.** It steps 460 → 368 → 300 and then cuts. Two of five
   seeds hit one relaxation; none hit the floor. If macro spacing is retuned downward this is the first
   thing that will start splitting chains.

---

## K. Verdict

**APPROVED.**

The pass answers the question it was asked. The source of mountain axes was the problem, not the belt
model: a macro plate partition at 1100 m produces boundaries whose straight runs are 671–1025 m, and an
arc-chain over them has a curvature radius that is set rather than measured. All five canonical seeds
produce exactly one axis that carries the belt width it claims, with reach 446 m or better against 389 m
required, no self-intersection and no folded wall. Stage 1 is untouched and hash-pinned. 369 / 369 tests
pass.

No height field was generated. Stage 3 was not started. Nothing was committed or pushed.
