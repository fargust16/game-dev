# Stage 3C — Valley Topology Forensics

Forensic and design pass. No code, tests, scenes, settings or packages were touched; this document is
the only file added.

Date: 2026-09-10 · against the current `CarveField` / `RidgeSkeleton` / `SpurCurve` implementation

---

## 1. Executive finding

The closed contours are not a tuning artefact and not merely a visualisation artefact. They are a
**theorem** about the function currently being carved.

`C` is continuous, non-negative, zero on the *entire* boundary of a simply-connected compact region,
and positive inside it. Any such function has closed level sets around an interior maximum. The carve
is a bump function with compact support — one per corridor — so a contour plot of it *must* show
closed islands, whatever the profiles are set to.

The reason its support is closed on all four sides traces to a single decision: `C ∝ uplift`, adopted
in the last pass so that `H3 ≥ H1` could hold with no clamping. Past the massif toe the uplift is
zero, so the carve is zero, so the valley cannot continue. **The bowl appearance is the direct
geometric consequence of the "never below Stage 1" invariant.**

Underneath that, and independent of it, is a structural gap the design already names and the
implementation never built: **there is no downstream coordinate.** The valley's longitudinal position
is currently the massif's transverse flank coordinate `t`, which is capped at 1 and undefined beyond.
A valley needs arc length along its own floor line. The design says so explicitly — §8 lists the
handoff artefact as *"Valley corridors — floor polylines with downstream links"* — and `ValleyCorridor`
carries no polyline and no link.

Verdict is at §18. The short form: the design is right, the implementation built a different object
than the design specifies, and one contract sentence has to be decided before the correct object can
be built.

---

## 2. Current mathematical model

For a world point resolved through Stage 3A into `(s, d)`:

```
W(s)      = HalfWidthAt(s) · (sign == SteepSide ? Asymmetry : 2 − Asymmetry)
t         = |d| / W(s)                             ∈ [0, 1]
Rⱼ(t)     = aⱼ + driftⱼ · smoothstep(t)            ridge line of spur j
floorⱼ(t) = (Rⱼ₋₁(t) + Rⱼ(t)) / 2                  corridor floor line
w         = |s − floorⱼ| / (distance to nearer ridge)   ∈ [0, 1]

αⱼ        = ValleyDepthPerCrest · CrestAt(headⱼ)/CrestHeightMeters
uplift    = CrestAt(s) · CrossSection(t)
C         = αⱼ · uplift · Rise(t) · Floor(1 − w) · gate
```

with `Rise = (0,0)(0.45,1)(1,1)` and `Floor = (0,0)(0.55,0.55)(0.8,1)(1,1)`, both `ProfileCurve`, so
both have zero slope at every control point.

Corridor `j` owns `s ∈ [Rⱼ₋₁(t), Rⱼ(t)]`; the ends of `CoreInterval` act as virtual ridges.

---

## 3. Current topological model

Enumerate the boundary of corridor `j`'s support:

| boundary | why `C = 0` there |
|---|---|
| `t = 0` (crest) | `Rise(0) = 0` |
| `t = 1` (toe) | `CrossSection(1) = 0`, so `uplift = 0` |
| `s = Rⱼ₋₁(t)` | `w = 1`, `Floor(0) = 0` |
| `s = Rⱼ(t)` | `w = 1`, `Floor(0) = 0` |
| `s = CoreInterval.x / .y` | virtual ridge, same as above |

The support is therefore an open topological disc with `C = 0` on the whole of its boundary and
`C > 0` in its interior. **`C` is a bump.** Its level sets are nested closed curves. That is what the
carve render shows, and it is unavoidable for this support regardless of the profiles.

---

## 4. Exact cause of closed contours

The brief lists six candidate hypotheses. Tested against the code:

| | verdict |
|---|---|
| **A — finite support: `C` exists only while `uplift > 0`** | **TRUE.** `uplift = CrestAt(s)·CrossSection(t)` is a factor of `C`, and `CrossSection(1) = 0`. This is the boundary that closes the *downstream* end. |
| **B — support closed by construction (four zero boundaries)** | **TRUE**, and it is what makes the contours closed rather than merely truncated. See §3. |
| **C — there is no downstream coordinate** | **TRUE, and this is the root cause.** `t` is the massif's transverse coordinate. It is used simultaneously as the corridor's longitudinal position (`Rise(t)`) and as the flank depth (`CrossSection(t)`). It is capped at 1 and has no continuation, so the valley has no parameter along which to leave the massif. A and B are consequences of C. |
| **D — the valley is derived from the strip, not from its own trajectory** | **TRUE.** `ValleyCorridor` is a strip descriptor (§5) and the floor line exists only as an expression evaluated inside `CarveField.Depth`. There is no valley object with a geometry of its own. This is the mechanism by which C is true. |
| **E — `Termination` exists but is not consumed by the carve** | **TRUE, and a separate live defect.** `CarveField` reads `spurs[i].ArcAt(t)` and nothing else from `SpurCurve`. `Termination` and `TargetReachMeters` are never read. A spur truncated by the coastline or by the inter-massif divide still contributes a full-length ridge boundary, so the corridor beside it is bounded by a ridge that does not exist on the ground. |
| **F — another cause** | No further independent cause found. |

### Three properties, distinguished

The brief asks for these to be separated. They are:

**Local monotonicity — GUARANTEED, and provably so.** Along the corridor floor (`w = 0`, `gate = 1`):

```
H3(t) = H2 − C = H1 + U(t) − α·U(t)·Rise(t) = H1 + U(t)·(1 − α·Rise(t))

dH3/dt = U′·(1 − α·Rise) − U·α·Rise′
```

`U′ < 0` (`CrossSection` strictly decreasing), `(1 − α·Rise) > 0` (α ≤ 0.30), `Rise′ ≥ 0`, `U > 0`.
Both terms are `≤ 0` and the first is strictly negative, so `dH3/dt < 0` everywhere. **The corridor
floor descends monotonically from head to toe. There is no pit along a corridor.** The design's §4.2
rule holds.

**Topological continuation — NOT guaranteed.** The support ends at `t = 1`. There is no
`t > 1`, no arc length, and no link to anything downstream. The corridor's support is a bounded
component that touches nothing.

**Terrain expression — FAILS.** The quantity that makes a valley visible is the channel's relief
against its own ridges at the same transverse depth:

```
relief(t) = C(floor, t) − C(ridge, t) = αⱼ · U(t) · Rise(t)
```

`relief(1) = 0`. The groove is deepest in the mid-flank and has healed completely by the toe. Below
that the surface is the undissected Stage 2 apron and then the Stage 1 island. A walker or a river
following the groove downhill arrives at the toe and finds no groove. **The valley is a hanging
groove that terminates in mid-slope, not a valley with an exit.**

So the model guarantees the weakest of the three properties and fails the two that matter for
geography.

### On the visualisation

Part of the reported severity is genuinely the diagnostic: the carve render contours `C`, and `C`
*will* be a bump for any subtractive valley confined to a finite massif — even a correct one. The
render should contour channel relief and overlay flow direction, not contour the carve (§11). But the
underlying failure is real and independent: the channel relief itself reaches zero at the toe.

---

## 5. `ValleyCorridor` audit

Every field currently on the type:

| field | what it is | sufficient for a trajectory? |
|---|---|---|
| `Id`, `MassifIndex`, `FlankSign` | identity and ownership | yes, needed |
| `LowerSpurId`, `UpperSpurId` | the two bounding spurs, `-1` at an interval end | **yes** — these give the two ridge lines whose mid-line is the floor |
| `SInterval` | the arc interval, from stations at `t = 0` | partial — it is the head-end footprint only, it does not move with `t` |
| `HeadS` | midpoint of `SInterval` | yes — this is the head, and it agrees with `floorⱼ(0)` |
| `SWidth`, `IsOuter` | derived conveniences | yes |

**What is absent:** a polyline, an arc-length parameterisation, a downstream direction, a downstream
link, a termination, and a depth profile. The type describes *where a corridor starts and what bounds
it*, not *where it goes*. It is a strip descriptor.

The design's §8 handoff table names the artefact `Valley corridors — floor polylines with downstream
links`. The implemented type is neither a polyline nor linked.

---

## 6. `SpurCurve` / termination audit

| member | used by `CarveField`? |
|---|---|
| `ArcAt(t)`, `DriftArc`, `AttachS` | **yes** — the ridge line |
| `StripInterval` | no (correctly; superseded by the moving ridge partition) |
| `Points`, `Cumulative`, `LengthMeters` | no |
| `TargetReachMeters` | **no — should be** |
| `ReachMeters`, `Truncated` | no |
| `Termination` | **no — should be** |

`ArcAt(t)` is defined for all `t ∈ [0,1]` regardless of where the spur actually stopped. On the
two-massif fixture one spur terminates on the inter-massif divide; the corridor beside it is still
bounded, for the whole flank depth, by a ridge that does not exist past that point. Below a truncated
spur, two corridors that ought to merge into one remain separate.

This is a real defect today and it will matter more once corridors have trajectories, because a
corridor whose bounding ridge ends is a corridor that must merge with its neighbour.

---

## 7. Stage 3B → 3C contract audit

**Question: do we already possess enough information to construct a true longitudinal valley
trajectory without changing Stage 3B topology?**

**Inside the massif: yes, completely.** The floor line is

```
floorⱼ(t) = (Rⱼ₋₁(t) + Rⱼ(t)) / 2,  t ∈ [0, 1]
world(t)  = massif.PointAt(floorⱼ(t)) + normal(floorⱼ(t)) · (σ · t · W(floorⱼ(t)))
```

Every term is already public: `SpurCurve.ArcAt`, `MountainMassif.PointAt`, `HalfWidthAt`,
`Asymmetry`, `SteepSide`. No Stage 3B change is required, no new draw, no new stream. The trajectory
inside the massif is a pure re-expression of data Stage 3B already publishes.

**Beyond the massif toe: the geometry is available, the authority is not.** A continuation needs a
direction (the tangent of `floorⱼ` at `t = 1` — available), a path (steepest descent of Stage 1's own
field, `Stage1Result.Field` — available), and a termination (the coastline, via `CoastQuery` —
available). What is missing is not data. It is permission to lower ground where `H2 = H1`. That is
§9.

**Conclusion:** the missing piece is a **contract**, not a geometric input. Stage 3B's topology is
sufficient and must not change.

---

## 8. Stage 3C → Stage 4 contract audit

`HydrologyPlanner.Plan(HydrologyGrid, HydrologySettings, IWorldSeed)` consumes a height field and
derives flow, basins and reaches itself. `World/Hydrology/` also already contains
`HydrologyCarveField.cs` — Stage 4 owns river incision. Stage 3 must not become the hydrology owner
and must not seed the planner.

**Minimum valley geometry Stage 3 must provide** so Stage 4 derives drainage naturally from `H3`:

1. **No undeclared depression.** `DepressionFill` will otherwise flood them into lakes Stage 3 never
   declared, and `HydrologyValidator`'s "every reach reaches the ocean" becomes a fight against
   Stage 3's artefacts.
2. **A monotone descent from every valley head to the coast.** Not merely inside the massif — the
   whole path. This is what makes the planner's answer follow Stage 3's structure instead of
   inventing its own.
3. **Channel relief that survives to the massif toe, and preferably beyond.** Flow routing is driven
   by the local gradient; where the groove has healed, the routing is decided by whatever the Stage 1
   dome happens to do, and the river leaves the valley. This is the property the current model fails.
4. **Published corridor polylines** so §8's validation — "reaches should occupy corridors" — can be
   run at all. Today there is nothing to compare a reach against.

Note that (3) is satisfiable *without* incising below Stage 1: it only requires the channel relief to
still be non-zero where the massif ends. It is (2) across the apron that needs §9's decision.

---

## 9. Requirement §6 vs L — the contract conflict

**§6 (design):** "Below the massif apron the corridor continues as a shallow trough across the island
floor toward the coast, its depth decaying and capped well under the T2 band."

**L (acceptance):** "Stage 3 never lowers terrain below Stage 1."

Outside the massif `H2 = H1`, so any trough there means `H3 < H1`. The two cannot both hold. This is
not a matter of interpretation; it is arithmetic.

### Assessment of the four options

**Option A — continuation is structural only, no physical lowering.** Rejected. It publishes a
corridor polyline the height field does not contain, so Stage 4's validation would compare reaches
against a line with no terrain expression, and flow across the apron would be decided by the Stage 1
dome. It satisfies the letter of §8's handoff and none of its purpose.

**Option C — continuation belongs to Stage 4.** Has real repository support: `HydrologyCarveField`
exists, and river incision is unambiguously Stage 4's. But it does not work as stated, because
Stage 4 routes flow on the field Stage 3 hands it. If the groove has healed at the toe, Stage 4's
flow leaves the valley *before* it has anything to incise, and the resulting river is not a
continuation of the Stage 3 valley at all. Option C is viable only in combination with fixing
terrain expression (3) above — i.e. the channel must still be a channel where Stage 4 picks it up.

**Option B — Stage 3 is allowed a bounded below-Stage-1 incision.** This is what §6 has said all
along, and it is geologically ordinary: a valley crossing a coastal plain is incised into that plain.
It is also the only option that delivers §8's stated check.

### Recommendation

**Option B, with L restated precisely rather than dropped.** L exists to stop Stage 3 digging
arbitrary holes, and that concern is fully served by a bounded form:

```
H3 ≥ H1 − maxIncision(x, z)
maxIncision decays to 0 at the shoreline via the existing coastal gate
maxIncision capped well below the T2 band floor (18 m) so a trough never reads as a plateau edge
H3 > SeaLevelMeters on land            — the coastline still cannot move
H3 ≤ H2                                — Stage 3 still only removes
dH3/ds ≤ 0 along every corridor        — still no pit
```

**Contract change required:** the acceptance invariant `H3 ≥ H1` becomes `H3 ≥ H1 − maxIncision`,
with `maxIncision` bounded as above. Nothing in Stage 1, 2, 2A, 2B or 3A changes — Stage 1's field
and its hashes are untouched; only the relationship Stage 3's *output* must hold to it is restated.

The design also needs one clarifying sentence (§14).

---

## 10. Independent depression diagnostic design

Operates on `H3` alone plus the minimum declarations needed to classify intent. It must not reuse
`CarveField`, `ValleyCorridor` or any Stage 3 classification, because the question is whether the
carve produced something its own model does not know about.

**Neighbourhood.** 8-connected, matching the convention `HydrologyGrid.NeighborDx/Dz` already
establishes — the same connectivity Stage 4 will use, so a depression this finds is one Stage 4 will
find. The convention is reused; the code is not.

**Algorithm: priority flood from the outlets.** Not a local-minimum scan, which cannot distinguish a
one-cell dimple from a basin and cannot handle plateaus.

1. Seed a min-priority queue with every land cell adjacent to ocean (`SignedDistance ≤ 0` neighbour)
   and every land cell on the lattice edge, at its own `H3`.
2. Pop the lowest cell; for each unvisited neighbour set `fill(n) = max(H3(n), fill(current))` and
   push it at `fill(n)`.
3. When the queue empties, `fill − H3` is the depth of standing water. Cells with
   `fill − H3 > tolerance` are inside a depression.

**Plateau handling** falls out: a flat region is filled at the level of whichever boundary cell was
reached first, so a flat valley floor with an outlet gets `fill = H3` and is not a depression, while a
flat basin floor gets the spill elevation.

**Boundary handling.** Lattice-edge land cells are outlets. The world has an ocean margin, so this
should never fire; if it does, that is itself a finding.

**Ocean handling.** Cells with `SignedDistance ≤ 0` are excluded from the land count and act as
outlets. A depression connected to the ocean at or below sea level is not a depression.

**Valley floor versus enclosed basin.** A valley floor is a transverse minimum but a longitudinal
descent, so priority flood raises nothing along it — `fill = H3` everywhere on a correct corridor.
An enclosed basin has `fill > H3` over a connected area. **The test is exactly `fill == H3`, and it
requires no knowledge of where the valleys are.**

**Truly closed bowl.** A connected component of `fill − H3 > tolerance` with area above a minimum
(a handful of cells, to discount lattice noise). Report per component: area, max depth, spill
elevation, spill location.

**Classification.** A component is intentional only if a declared Stage 3E basin contains its spill
point. Stage 3C declares none, so **every component found is unauthorised**. That makes this
diagnostic usable immediately, before 3E exists.

**Tolerance.** Derived, not chosen: the largest second difference the carve's own profiles can
produce on this lattice, the way `MountainMassif.ProfileSecondDifferenceMeters` already derives its
floor.

---

## 11. Visual diagnostic design

One plan render plus one profile sheet. Both read the production evaluators.

**Valley plan** — the picture that distinguishes `HEAD → CONTINUOUS → EXIT` from `HEAD → CLOSED BOWL`
at a glance:

1. **Spur skeleton** — white polylines, as today.
2. **Valley centrelines** — the corridor floor polylines, drawn from head to termination, with
   arrowheads every fixed distance showing the downstream direction.
3. **Valley floor band** — filled, coloured by **channel relief** (`C` at the floor minus `C` at the
   corridor's own ridge at the same transverse depth), *not* by absolute carve. A valley whose relief
   has healed goes to background colour at exactly the point it stops being a valley, which is the
   thing under test.
4. **Termination markers** — a distinct glyph per `Termination`: coast, merge into a neighbouring
   corridor, inter-massif divide, domain edge, faded out. A "faded out" glyph anywhere but the coast
   is a failure, visible without reading a number.
5. **Continuation** — drawn in a separate colour past the massif toe, so the in-massif and apron
   parts of the same valley are distinguishable but obviously one curve.
6. **Flow-direction overlay** — steepest-descent arrows computed from `H3` on a coarse grid
   (every ~40 m), independent of the corridor declarations. Where the arrows follow the centrelines,
   the substrate agrees with the structure; where they cross them, it does not.
7. **Depression overlay** — the components from §10, filled in a warning colour with their spill
   points marked. On a correct field this layer is empty.

**Valley longitudinal profile sheet** — per corridor, one strip chart: arc length along the
trajectory (metres, from head) against channel relief (metres). A correct valley rises from zero,
holds, and returns to zero **at its termination marker**, which sits at the coast. A bowl returns to
zero in mid-slope with the marker far to its right. Also plot `H3` along the same arc to show it
descending monotonically.

The existing contour-of-`C` render should be kept but demoted: it is now understood to show closed
islands for any correct valley too, so it cannot be the acceptance picture.

---

## 12. Minimal structural correction

The brief's hypothesis is:

```
Spur pair → ValleyCorridor → ValleyTrajectory → longitudinal profile → cross-section →
continuous carve → termination
```

**Proven correct, with one refinement:** `ValleyTrajectory` should not be a new type alongside
`ValleyCorridor` — it should be the geometry `ValleyCorridor` was always supposed to carry. The design
calls the handoff artefact a "floor polyline with downstream links"; that is a corridor with a
trajectory, not two objects.

### The correction

**1. Give `ValleyCorridor` its floor polyline.** Built entirely from existing Stage 3B data (§7):
sample `floorⱼ(t)` for `t ∈ [0,1]`, convert to world through the massif frame, store points and
cumulative arc length. No new seed stream, no new topology, no Stage 3B change.

**2. Add a downstream link and a termination.** `DownstreamId` (another corridor, or none) and a
`ValleyTermination` enum: `Coast`, `Merge`, `Divide`, `Domain`. `Merge` is what happens where a
bounding spur ends — which is where `SpurCurve.Termination` and `TargetReachMeters` finally get
consumed, closing defect E.

**3. Re-key the longitudinal profile to arc length, not to `t`.** This is the substantive change and
the one that removes hypothesis C. Depth becomes `depth(arcAlongFloor)`, defined over the whole
trajectory including the continuation, non-increasing after its maximum, reaching zero **at the
termination** — which for a coastal termination is where the coastal gate is already zero, so the two
agree without a special case.

**4. Re-key the cross-section to distance from the trajectory**, measured inside the corridor's
ridge-bounded domain while inside the massif, and against a half-width that decays with the depth
profile outside it. Inside the massif this is identical to today's `Floor(1 − w)`, so the C1 ridge
boundaries and the ridge partition — the parts of the last correction that were right — are kept
exactly.

**5. Continuation.** From the toe, follow steepest descent of **Stage 1's own field** (not `H3`,
which would be self-referential) to the coast, merging with a neighbouring trajectory where they
converge. Requires §9 Option B.

### Why this opens the contours

The support's downstream boundary moves from `t = 1` to the shoreline, where the coastal gate is
independently zero. The support is no longer a disc with `C = 0` all round: it is a strip that runs
to the edge of the land. Its level sets open toward the sea instead of closing, and the channel relief
survives to where Stage 4 picks it up.

---

## 13. Alternatives rejected

Each was evaluated against the repository, not dismissed by category.

| | why rejected |
|---|---|
| noise, ridged noise, continuation noise, random post-hoc curves | The design's §16 rejected these for Stage 3 entirely; they define topology by accident and produce the named artificiality tells. Nothing in this failure suggests topology is missing — the topology is right and the *parameterisation* is wrong. |
| erosion / hydraulic simulation / flow accumulation | Inverts the architecture: puts drainage before structure, and Stage 4 already owns drainage. Also far more expensive than a curve-level fix. |
| smoothing `H3`, Gaussian blur, TerrainData or Unity Terrain erosion | Symptom removal. The carve is already C1 at every internal boundary; the defect is the shape of its support, which no smoothing changes. |
| post-process valley carving | Would be a second, independent valley model competing with the structural one — the exact duplication the design forbids. |
| increasing carve width | Widens the bowls. Support stays closed. |
| increasing carve depth | Deepens the bowls, and it already cost the slope budget once. Support stays closed. |
| stretching the current bowls | The support is closed at `t = 1` by `CrossSection(1) = 0`; stretching `t` cannot change that. |
| global raster valley mask | Forbidden by the design for the same reason tile-dependent geometry is, and it would resolve the valley no better than its own cell size. |
| a new general-purpose graph framework | The structure is a few dozen corridors with at most one downstream link each. A polyline and an id is the whole requirement; a graph framework is more machinery than the problem has. |

One near-miss worth recording: **Option C (defer continuation to Stage 4)** is not unreasonable and
has repository support in `HydrologyCarveField`. It is rejected only because Stage 4 routes on the
field Stage 3 hands it, so a groove that has healed at the toe gives Stage 4 nothing to continue.

---

## 14. Required contract changes

Two, both small, neither touching Stage 1/2/2A/2B/3A.

**C1 — acceptance invariant.** `H3 ≥ H1` becomes:

> `H3 ≥ H1 − maxIncision`, where `maxIncision` decays to zero at the shoreline through the existing
> coastal gate and is capped well below the T2 tier floor. `H3 > SeaLevelMeters` on land and
> `H3 ≤ H2` everywhere remain unconditional.

Without this, §6's continuation is unimplementable and the valleys must end in mid-slope.

**C2 — design clarification, §6 and §14.** Two sentences:

> "decaying toward the toe" means the corridor's depth profile reaches zero continuously **at its
> termination**, which is the coastline — not at the massif toe.

> "Corridor continuation" is part of Stage 3C and is mandatory. Without it a valley terminates in
> mid-slope, its support is closed, and the corridor handoff of §8 has nothing for Stage 4 to
> validate against.

Everything else in the Stage 3 design is confirmed sound by this analysis. The failure is that the
implementation built §4.1 Q11's *strip* and not §6/§8's *floor polyline*.

---

## 15. Implementation slicing proposal for 3C

Four slices, each independently verifiable. Deliberately front-loading the picture, which is the
lesson from Stage 2 and from this pass.

**3C-1 — trajectories, no carve change.** Give `ValleyCorridor` its floor polyline, downstream link
and termination; consume `SpurCurve.Termination`. Ship the valley-plan diagnostic (§11 items 1, 2, 4,
5). Verifiable visually and by topology tests; the height field is untouched, so the whole Stage 3C
regression stays green.

**3C-2 — carve re-keyed to arc length, in-massif only.** Longitudinal profile over trajectory arc,
cross-section unchanged. Still `H3 ≥ H1`, so no contract change needed yet. Ship the depression
diagnostic (§10); it must report zero components. Ship the profile sheet.

**3C-3 — continuation.** Requires C1. Trajectories run to the coast; corridors merge. Depression
diagnostic still zero; coastline and `H3 > SeaLevel` invariants added.

**3C-4 — quality gate.** Isotropy, reachability and slope bands measured across the canonical and
robustness seed sets, with tuning done against those measurements and nothing else.

---

## 16. Quality-gate implications

**Isotropy** (currently 0.75 / 1.00 / 0.68 / 0.67 against Stage 2's 0.99 / 0.96 / 0.86 / 0.83).

Primarily a **valley-direction** problem. The measure sums `|∂²H/∂x²|` against `|∂²H/∂z²|`. Every
valley currently runs along the flank normal, which on a straight axis is a nearly constant direction,
so all the curvature the valleys add lands on one axis. The one seed that scores 1.00 — 20260905 — has
a strongly S-curved axis whose normals rotate through most of a right angle. That is the whole
explanation.

Spacing is a weak secondary contributor (uniform along the axis) and cross-section a weaker one (the
same profile everywhere). It is **not** a depth-profile problem: depth scales the anisotropy but does
not create it.

The correction should improve this directly and without tuning: a trajectory that curves as it
descends and then continues across the apron in a direction set by Stage 1's own gradient points in a
range of directions instead of one.

**Reachability** (currently 92.1–94.5 %, target ≥ 95 %).

Cannot be attributed yet, and the missing measurement is specific: **Stage 2's own reachability has
never been measured.** If the undissected massif already scores below 95 %, the shortfall belongs to
the massif's slopes and Stage 3 cannot be asked to fix it; if Stage 2 scores above 95 %, the loss is
the valleys cutting off pockets between them and is Stage 3's to answer. This must be measured before
any parameter moves. It is a two-line addition to the existing measurement pass.

**Slope > 45°** (currently 5.2–7.3 %, target 6–15 %; two seeds now below the floor).

Diagnosed, and the honest reading is uncomfortable: the previous run's 6.1–8.8 % was **inflated by the
defects**. An 85 m discontinuity at the massif toe is vertical ground, and it was being counted. The
current 5.2–7.3 % is the first measurement of the real steep fraction. Recovering the band should come
from valleys that are deeper relative to their width — a cross-section question — and specifically
must not come from restoring any discontinuity. Deferred to 3C-4, after the continuation exists,
because a corridor that runs to the coast has more length over which to be steep.

---

## 17. Risks

1. **Continuation paths converging.** Two trajectories following steepest descent on the same dome
   will tend to converge. Merging is the correct answer and is in the model, but the merge rule needs
   to be deterministic and must not produce a crossing. Testable with the existing crossing check.
2. **C1 opens a door.** A bounded below-Stage-1 incision is a new freedom, and the bound has to be
   enforced by construction rather than by a clamp — a clamp is what produced the flat plateaus two
   passes ago.
3. **The apron is where Stage 5's T2 plateaus will go.** A trough crossing ground a later stage wants
   to raise is a conflict the design anticipates (§4.1 Q10) but does not resolve in numbers. Keeping
   the incision well under the T2 floor is what keeps it from becoming one.
4. **Steepest descent on Stage 1's field is only defined where that field has a gradient.** A flat
   island floor gives an ill-conditioned direction. Falling back to the trajectory's tangent at the
   toe is the obvious answer and should be stated rather than discovered.
5. **The slope band may not be recoverable** without valleys deeper than the reachability target
   tolerates. If 3C-4 shows those two constraints in genuine tension, that is a product decision and
   should be escalated, not tuned around.

---

## 18. Explicit decision

The existing Stage 3 design describes the right object. §6 specifies a corridor that continues past
the massif to the coast, §8 specifies the handoff as floor polylines with downstream links, and §4.2
specifies the monotonicity that keeps it free of pits. All three are sound and none needs redesigning.

What the implementation built instead is §4.1 Q11's strip: a region between two spurs, with the
massif's transverse coordinate standing in for a downstream coordinate it never had. That object's
support is closed on all four sides, so its carve is a bump and its contours are islands — by
theorem, not by tuning.

The correction is small and stays inside the existing slice architecture: give the corridor the
polyline the design already asked for, key the longitudinal profile to arc length along it, consume
the spur terminations, and let the trajectory run to the coast.

But it cannot be implemented as it stands, because §6's continuation and the acceptance invariant
`H3 ≥ H1` are arithmetically incompatible outside the massif, and choosing between them is a contract
decision rather than an implementation one. The recommendation is §9 Option B with the bounded
restatement at §14 C1, and the design needs the two clarifying sentences at C2.

**CHANGES REQUIRED**
