# Procedural Terrain — Stage 3 Design: Ridge, Spur, Valley and Basin Structure

**Design only.** No implementation, no production change, no test change. Stage 4 not started.

Date: 2026-09-08 · Unity 6000.5.7f1 · targets the approved Stage 1 / Stage 2 / Stage 2A / Stage 2B
contract

---

## 1. Repository audit

Read before designing. Everything referenced below exists; nothing is inferred.

### 1.1 What Stage 2 hands over

`World/TerrainGen/MacroAxis.cs` — the certified axis.

| Member | Meaning |
|---|---|
| `Points`, `Cumulative`, `LengthMeters` | the sampled arc-chain curve in world XZ, 20 m spacing |
| `CertifiedCore` (`Vector2`, normalised arc) | the stretch whose whole cross-section clears the coastal gate |
| `CoreLengthMeters` | that stretch in metres |
| `UsableHalfWidthMeters` | the certified half-width |
| `CoastClearanceMeters` | measured minimum clearance along the run |
| `ReachMeters`, `MaxCurvature`, `MinSelfDistanceMeters` | curvature and self-approach, `+∞` when unbent |
| `MeanConvergence`, `SourcePlatePairs` | which plates, and how hard they converge |
| `DistanceTo(x, z)`, `Bounds(pad)`, `Offset(d)` | point query, support rectangle, offset wall |
| `Fillet(...)`, `Resample(...)` (static) | the arc-chain constructor with a named radius |

`World/TerrainGen/MountainMassif.cs` — the body raised on it.

| Member | Meaning |
|---|---|
| `Axis`, `Index`, `Support` (`Rect`) | provenance and bounding rectangle |
| `Project(x, z, out s, out offsetMeters)` | **the axis frame**: normalised arc along, signed metres across |
| `CrestAt(s)`, `HalfWidthAt(s)`, `PointAt(s)`, `CurvatureAt(s)` | the model sampled along its own axis |
| `CrossSection(u)` | crown then a single smoothstep to the toe |
| `Longitudinal` (`ProfileCurve`), `CulminationT`, `CoreInterval` | the crest profile and where its high ground stands |
| `CrestHeightMeters`, `HalfWidthMeters`, `Asymmetry`, `SteepSide` | scale and which flank is the steep one |
| `Evaluate(x, z)` | uplift in metres, never negative |

`Project` is the single most important existing API for Stage 3: it is already the frame a spur has to
be built in.

### 1.2 Geometry, curves and fields

- `World/Town/Geometry/Geo2.cs` — `ClosestPointOnSegment`, `SegmentIntersect`, `PointInPolygon`,
  `PolygonArea`, `PolygonCentroid`, `ResamplePolyline`, `SmoothCatmullRom`, `ChaikinSmooth`,
  `InsetPolygon`, `ConvexHull`, `SplitPolygonByLine`, `IntersectLineWithPolygon`, `ObbLongestAxis`.
  Everything a ridge polyline needs already exists.
- `World/TerrainGen/ProfileCurve.cs` — control points with smoothstep between them. Cannot overshoot,
  preserves monotonicity, zero slope at every control point. This is the repository's way of saying
  "distance in, height out", and Stage 3 should use it rather than analytic falloffs.
- `World/TerrainGen/WorldHeightField.cs` — `ForWorld(resolution)`, `OriginX/Z`, `CellSize`, `Width`,
  `Height`, `Heights`, `WorldX(x)`, `WorldZ(z)`, `Slope01(x, z)`, `Hash()`. One 1025 × 1025 lattice
  over the whole 2800 × 2800 m world at 2.734 m.
- `World/TerrainGen/CoastQuery.cs` — `SignedDistance`, `NearestDistance`, `IsInside`, `RowCrossings`,
  `InsideFromSortedCrossings`. Positive inland.
- `World/WorldScale.cs` — `WorldExtentXMeters/ZMeters` 2800, `SeaLevelMeters` 24,
  `CoastShelfWidthMeters` 200, `IslandOceanMarginMeters` 280, `TerrainHeightBudgetMeters` 700.

### 1.3 Determinism

`World/Seed/IWorldSeed.cs` — `int Value`, `Random Fork(string streamId)`,
`Random Fork(string streamId, int a, int b = 0)`. `System.Random` only; `UnityEngine.Random` is
banned by AGENTS.md. Existing stream names follow `terrain.<layer>.<thing>`:
`terrain.crust.sites`, `terrain.macro.sites`, `terrain.macro.stress`, `terrain.macro.drift`,
`terrain.massif`.

Settings live in a family of plain structs, each owning one layer: `TerrainGenSettings` (Stage 1 and
the shared height scale), `MacroStructureSettings` (Stage 2A), `MassifSettings` (Stage 2B). Each has
`Default` and `Clamp()`. **`WorldGenSettings` is the legacy pipeline's struct** (roads, POI, toggles)
and is not part of this family; Stage 3 must add its own struct, not extend that one.

### 1.4 Hydrology, as it actually exists

`World/Hydrology/` is already built and is **height-field driven**:

```
HydrologyGrid (Heights[], CellSize, SeaLevel)
      → DepressionFill  → FlowRouting  → BasinExtraction  → RiverNetworkBuilder
HydrologyPlanner.Plan(HydrologyGrid grid, HydrologySettings settings, IWorldSeed worldSeed)
```

`BasinExtraction` produces `LakeCandidate { Cells, SurfaceElevation, MaxDepth, AreaSqm, SpillCell,
SpillPointXZ, Boundary }`; `LakeBasin` adds `OutletReach`, `InletReaches`, `SpillsDirectlyToOcean`.
`HydrologyValidator` and `HydrologyDiagnostics` exist. Tests assert that every reach reaches the
ocean, no reach flows uphill, Strahler order rises at confluences, a landlocked world fails loudly.

**This is decisive for the design.** Stage 4 does not want a drainage graph from Stage 3 — it derives
one from heights. Stage 3 must therefore not own basins-as-lakes or reaches. Its job is to produce a
height field whose structure makes the planner's answer geographic, plus a declaration of what it
intended so the planner's answer can be *checked* rather than merely trusted.

### 1.5 Quality targets already in the repository

`Plans/WORLD_RELIEF_PRINCIPLES.md`:

- Tier table — T1 massif 250–530 m over local ground, footprint 1000–2000 m, **1 (2 max)** per island;
  T2 plateau 18–40 m, 150–450 m, 6–10; T3 outcrop 5–12 m, 40–90 m, 12–20.
- **Adjacent tiers must differ by ≥ 2.5× in height.** Anything landing between tiers is forbidden.
- Two levels of stacking maximum: T3 on T2, T2 on the island floor, T1 alone.
- Slope: **≥ 95 %** of dry land reachable from the shore at ≤ 45°; **6–15 %** steeper than 45 %;
  **≥ 35 %** under 5°. 45° is `CharacterController.slopeLimit`, not an aesthetic choice.
- Named artificiality tells, all banned: **radially symmetric mountains**, **ridges of uniform
  height**, **valleys that start in the middle of a slope with nothing above them**.
- "Every slope must drain somewhere. A closed basin with no outlet is a bug unless it is a lake."

`OverworldTerrainDiagnostics` (*Dump Terrain Geometry*) reports a curvature-ratio isotropy measure
(1.0 isotropic), first used to diagnose directional bias in the old noise pipeline. Stage 3 will be
measured against it; this pass does not change it.

### 1.6 Diagnostics that already exist

`Editor/TerrainGenDump.cs` (height, slope, uplift, structure renders, `DrawLine`),
`Editor/TerrainGenMacroDump.cs` (plates, boundaries, axes, tube walls),
`Editor/TerrainGenMassifDump.cs` (hypsometric + hillshade, uplift with contours, width bands,
contribution-only shade, profile charts), `Editor/TerrainGenLandmassFeasibility.cs` (corridor sweep).
Stage 3 should extend this family, not start a new one.

---

## 2. Stage 2 input contract (not reinterpreted)

Stage 3 consumes, and never modifies:

```
Stage2BResult
  ├ Stage1            frozen landmass, coastline, base field, signed distance
  ├ Massifs[]         MountainMassif, each on an accepted MacroAxis
  │    ├ Axis.CertifiedCore     where high ground is allowed to stand
  │    ├ Axis.UsableHalfWidth   the certified body half-width
  │    ├ Project(x,z)           the (s, d) frame
  │    └ CrestAt / HalfWidthAt  the envelope along the axis
  ├ Field             Stage 1 + massif uplift, 1025²
  └ UpliftField       the massif contribution alone
```

Three properties of that contract are load-bearing for Stage 3 and must survive it:

1. The coastline does not move — the massif contribution is exactly zero at the shoreline.
2. High ground stands only inside `CertifiedCore`.
3. The massif is bounded by `MaxUpliftMeters` and its width is the certified width.

---

## 3. The Stage 3 geographic model

### 3.1 The one structural idea

A mountain range is not a ridge with decoration. It is **a main divide with spurs descending from it,
and the valleys are what lies between the spurs.** Spurs and valleys are duals: you cannot place both
independently without them crossing.

So Stage 3 **partitions each flank of each massif into alternating strips** in the massif's own
`(s, d)` frame:

```
                    crest / main divide  (= MacroAxis, owned by Stage 2)
   s →   ┌───────┬───────┬───────┬───────┬───────┐
         │ spur  │valley │ spur  │valley │ spur  │      d ↓  (down the flank)
         │  strip│ strip │ strip │ strip │ strip │
         └───────┴───────┴───────┴───────┴───────┘
                        toe / apron
```

Because the strips partition the flank, **ridges cannot cross — there is nowhere for them to cross.**
That is a construction guarantee, not a check applied afterwards. It is the same move that made Stage
2B work: express the problem in the structure's own frame instead of measuring distances in world
space.

### 3.2 Stage 3 is subtractive

Stage 3 does not raise ground. It defines a **carving field** `C(x, z) ≥ 0` and the Stage 3 surface is

```
H₃(x, z) = H₂(x, z) − C(x, z) · gate(x, z)
```

where `gate` is the same coastal gate Stage 2B already uses, read from Stage 1's signed distance.

This single decision buys four guarantees for free:

- The Stage 2 massif remains the **envelope**. Ridge tops *are* the Stage 2 surface, which is also
  what a ridge geologically is: the remnant between two valleys.
- The height budget, the T1 tier and `MaxUpliftMeters` cannot be violated, because nothing is added.
- The coastline cannot move, because the gate is zero at the shore.
- `CertifiedCore` still bounds all high ground, because carving can only lower.

The cost is that Stage 3 cannot invent relief in a featureless lowland. That is correct: T2 plateaus
and T3 outcrops are a later refinement stage's business, and inventing them here would break the
tier-separation rule.

### 3.3 What the carve depth is measured against

Inside a strip, define the transverse coordinate `v ∈ [0, 1]` as **normalised position between the
strip's bounding ridge line and its bounding valley line**, not as distance to the nearest ridge.
Distance-to-nearest produces radial artefacts wherever a ridge ends; a partition-of-unity across the
strip does not. `v = 0` on the ridge, `v = 1` on the valley floor.

Depth then comes from a `ProfileCurve` — the repository's existing "distance in, height out" object:

```
C = valleyDepthAt(corridorPosition) · CrossValleyProfile(v)
```

`CrossValleyProfile` is authored: zero slope at the ridge, steepening through the flank, flattening
into a valley floor of finite width. One curve, shared, exactly as `MountainRange.FlankShape` was.

---

## 4. Ridge topology

### 4.1 Answers to the design questions

**1 — minimal data structure.** A forest of polylines with parent links. Nothing more:

```
RidgeCurve {
    int Id, ParentId (−1 for a crest reference), Depth (0 crest, 1 primary, 2 secondary)
    int MassifIndex, int FlankSign (+1 / −1)
    Vector2[] Points, float[] Cumulative, float LengthMeters
    float AttachS                 // where on the parent it leaves
    ProfileCurve HeightBudget     // fraction of local crest this ridge keeps, along its length
    float HalfWidthMeters         // the strip half-width it commands
}
```

There is no separate node/edge graph. A ridge tree of a few dozen curves is better represented as
curves with parent ids than as a general graph; nodes only exist where a child attaches, and
`AttachS` records that.

**2 — the main crest.** It is **not generated**. Depth 0 is a *reference* to `MacroAxis.Points`,
owned by Stage 2. Stage 3 stores `MassifIndex` and reads the axis through `MountainMassif`. This is
the ownership rule: duplicating the crest would create two sources of truth for the divide.

**3 — major spurs.** Attachment stations are placed along the **certified core**, spaced by a target
derived from the flank length rather than chosen: real spur spacing scales with the slope length it
drains, so `spacing ≈ k · HalfWidthAt(s)` with `k` in the range that yields 2–5 spurs on a 1000–1700 m
massif. Each spur leaves the crest at its station and descends the flank. Its direction starts
perpendicular to the axis and swings by a bounded angle drawn from `terrain.ridge.shape`, so spurs are
neither parallel nor radial.

**4 — secondary spurs.** Each primary spur's own strip is partitioned by the same rule one level down,
using the primary spur as the parent divide. A secondary spur is shorter, lower and narrower by the
same ratio the tier table uses between levels.

**5 — what limits recursion.** Depth 2, hard. This is not an arbitrary cap: the relief principles
allow **two levels of stacking maximum**, and each level must differ from its parent by ≥ 2.5× in
height. Crest → primary → secondary already spends both levels.

**6 — how spurs terminate.** A spur's height budget is a `ProfileCurve` that reaches zero at its far
end, so the spur *dies into the surface* rather than stopping. Its length is bounded by whichever
comes first: the budget reaching zero, the massif apron ending, the coastal gate closing, or the
divide with a neighbouring massif. No spur ends at a cliff.

**7 — how crossings are prevented.** By the strip partition of §3.1. Two spurs on the same flank own
disjoint `s`-intervals and each stays inside its own strip; a spur and its own children are nested.
Two spurs on *different* flanks are separated by the crest. Two spurs from *different massifs* are
separated by the inter-massif divide (§7). There is no construction in which two ridge curves can
meet except at a declared parent attachment.

**8 — spacing control.** One number, `SpurSpacingPerHalfWidth`, times the local half-width, with a
minimum absolute spacing so a short massif gets fewer spurs rather than crowded ones. Spacing is
perturbed within a bounded fraction from `terrain.ridge.spurs` so the result is not a comb.

**9 — ridge width.** A ridge has no width of its own. It is the crest of its strip, and the strip
half-width is what the partition assigned it. Width therefore falls out of spacing, which is the
correct dependency: you cannot have wide ridges and narrow spacing.

**10 — behaviour at boundaries.**

| Near | Behaviour |
|---|---|
| **coast** | The carve is multiplied by the Stage 2B coastal gate, so it vanishes at the shoreline. Corridors reach the shore with zero depth. The coastline cannot move. |
| **plateau** | Not Stage 3's object. A future T2 plateau layer sits on the island floor; Stage 3's corridors pass over it and are subtracted from whatever is there. |
| **another massif** | The two spur systems stop at the medial divide between the massifs (§7); the strip between them becomes an inter-massif valley corridor. |
| **another ridge** | Impossible by partition, except at a declared parent attachment. |
| **basin** | A corridor entering a declared basin terminates at the basin's shore; the basin's outlet corridor continues downstream. |

**11 — valleys.** A valley is **the complement of the spurs**: the strip between two adjacent spurs.
Valleys are never placed independently and never generated from noise. Their number is exactly
`spurs + 1` per flank, their spacing is the spur spacing, and their heads are on the crest — which is
precisely the "valleys need headwaters" rule.

A valley is **a longitudinal system, not merely that strip.** The strip says which two spurs bound it;
the valley itself is a trajectory with a head, a downstream direction, a floor curve, a cross-section,
a continuation past the massif and a termination. Its longitudinal profile is parameterised by **arc
length along that trajectory**, never by the massif's transverse flank coordinate. Building it as a
strip with the flank coordinate standing in for a downstream one gives every corridor a support that
is zero on all of its own boundaries, and a non-negative function that vanishes on the whole boundary
of a simply-connected region has closed level sets — the valley becomes an isolated bowl by theorem
rather than by accident. That is the failure the first Stage 3C implementation produced, and the
trajectory is what prevents it.

**12 — watershed divides.** The divide skeleton is the set of ridge curves at depth 0 and 1 plus the
inter-massif medial divides. Stage 3 does not compute watersheds — Stage 4 does, from the height
field. Stage 3 publishes the skeleton so Stage 4's answer can be checked against the structure that
was built.

**13 — structural basins.** A basin is created only where structure encloses ground:
- **intermontane** — between two massifs whose spur systems face each other across a corridor that
  has no downhill exit except one gap;
- **cirque-like** — on the concave side of a strongly bent axis, where two spurs and the crest enclose
  a hollow.

Both are found by walking the ridge structure, not by looking for low places in a field.

**14 — what makes a basin geographic.** Three conditions, all structural:
1. its perimeter is ridge structure for at least a stated majority of its length;
2. it has **exactly one** declared outlet — a gap in the enclosing ridge, at a stated elevation;
3. its floor lies below the outlet by a bounded depth, and the outlet connects to a corridor that
   continues downhill.

An arbitrary depression fails all three. This is also what makes it consumable by Stage 4: the
declared outlet is what `BasinExtraction` should independently find as `SpillCell`.

**15 — what Stage 3 preserves for hydrology.** §8.

### 4.2 The one rule that keeps drainage sane

**Corridor depth is non-increasing downstream.** The underlying Stage 1 + Stage 2 surface already
descends from crest to coast; if the carve never deepens as the corridor descends, the carved surface
descends at least as fast as the surface it was cut into. **No pit can form along a corridor.**

Pits then exist only where Stage 3 declared a basin, with an outlet. That is how "no arbitrary
depressions" becomes a property of the construction rather than a hope.

---

## 5. Spur topology in detail

Working in the massif frame `(s, d)` from `MountainMassif.Project`:

```
attachment i at s = aᵢ on the certified core, flank sign σ
spur curve:   s(t) = aᵢ + driftᵢ(t),   d(t) = σ · t · reachᵢ,   t ∈ [0, 1]
```

- `driftᵢ` is a bounded, monotone lateral drift so the spur is not a straight rib. Its bound is the
  half of the spacing to the neighbouring attachment, which is what keeps the spur inside its strip.
- `reachᵢ` is the spur length: the local half-width plus a bounded apron extension, tapered by the
  massif's own `HalfWidthAt(s)` so spurs near the ends are shorter.
- The curve is smoothed and resampled with the existing `Geo2.SmoothCatmullRom` / `MacroAxis.Resample`
  so it carries a bounded curvature, exactly as the macro axis does.
- **Height budget** along the spur is a `ProfileCurve` from 1.0 at the crest to 0.0 at the toe,
  concave so the spur holds its height near the crest and dies gently — the shape a real spur has.

Secondary spurs repeat this with the primary spur as parent and its strip as the domain.

**Bounded variation only.** `terrain.ridge.shape` draws the drift and the reach within stated
fractions. It never decides topology: how many spurs, where they attach and which strips exist come
from spacing and geometry, not from the draw.

---

## 6. Valley derivation

For a strip bounded by spur `L` and spur `R` (or by a spur and the massif's end):

1. The **valley floor line** runs from the crest point midway between the two attachments, down the
   flank, staying at the strip's mid-line in the `(s, d)` frame.
2. Its **depth profile** is a `ProfileCurve` along its length: zero at the head (it starts *at* the
   divide, so there is nothing above it to be cut off from), growing to a maximum in the middle
   flank, then decaying toward the toe. Non-increasing after the maximum, per §4.2.
3. Its **cross-section** is `CrossValleyProfile(v)`, with `v` the normalised position between the
   bounding ridge and the floor line.

Maximum depth is a fraction of the local crest height, so valleys scale with the mountain rather than
being a fixed size. The fraction is chosen so the resulting corridor sits in the tier gap correctly:
valleys on a T1 massif are a T1-scale feature, not a T2 one, and the flank between ridge and floor
must not exceed the slope band the principles allow.

**Corridor continuation.** Below the massif apron the corridor continues as a shallow trough across
the island floor toward its termination, its depth decaying. **This is part of Stage 3C and is
mandatory**, not an optional refinement: without it the valley ends where the massif uplift does, its
support closes, and there is nothing for Stage 4 to follow from headwater to sea.

"Decaying toward the toe" means the depth profile reaches zero continuously **at the trajectory's
termination** — the coastline, a declared basin, or a merge into another corridor — and *not* at the
massif toe. The massif toe is an interior point of the trajectory and carries no discontinuity of any
kind.

The continuation must terminate at one of: the ocean, a valid basin or lake, another valid drainage
termination, or another explicitly declared structural termination. It is C1 at the join with the
massif part and along its whole length, it remains connected downstream, and it must not enclose a
bowl of its own.

**The coastal incision exception.** Outside the massif `H2 = H1`, so a trough there necessarily cuts
below the Stage 1 surface. Stage 3 is therefore allowed a bounded incision below Stage 1 **only inside
a declared valley corridor**; everywhere else Stage 3 keeps `H3 >= H1`. The bound is derived, not
chosen:

```
CorridorIncisionMeters = T2 floor / tier separation = 18 / 2.5 = 7.2 m
```

`WORLD_RELIEF_PRINCIPLES` puts the T2 plateau band at 18–40 m and requires adjacent tiers to differ by
at least 2.5x, so 7.2 m is the largest amplitude that is *guaranteed* to read as a tier below T2 —
which is exactly what "capped well under the T2 band so it never competes with a plateau" asks for. It
lands inside the T3 band (5–12 m), which is the right scale for a shallow trough on a coastal plain.

Two invariants stay unconditional: `H3 <= H2` everywhere, and `H3 > SeaLevelMeters` on land, so the
coastline cannot move. The coastal gate already takes the carve to zero at the shoreline, which is
what keeps the second one true without a special case.

This remains terrain structure. Stage 4 still owns drainage: it routes on the field, derives its own
basins and reaches, and carves rivers through `HydrologyCarveField`. What Stage 3 provides is a
substrate whose valleys already run somewhere.

---

## 7. Multiple massifs

Today `MacroStructureSettings.MaxSystems` is 2 and the 29-seed audit found one massif on every world,
but the design must not assume it.

**Massif ownership of ground.** For a point, the owning massif is the one whose axis is nearest,
measured with `MacroAxis.DistanceTo`. With at most a handful of massifs this is exact and cheap; no
raster is needed.

**The inter-massif divide** is the medial line where two massifs' ownership changes. Spurs from each
massif stop there. The strip between them is an **inter-massif valley corridor**:

```
   massif A  ───╲                          ╱─── massif B
        spurs    ╲──── valley corridor ───╱    spurs
```

Its floor runs along the medial line and its depth is the smaller of what the two massifs would each
give it, so neither massif's scale dominates the shared valley.

**Basins between massifs.** Where the medial corridor is enclosed at both ends — by the two massifs
converging, or by a spur from each reaching across — the enclosed ground becomes an intermontane
basin candidate:

```
      ridge
         ╲
          ╲____ basin ____ outlet ──→ corridor
                ╱
           ridge
```

The outlet is the lowest gap on its perimeter, chosen deterministically as the single lowest point of
the enclosing structure. Exactly one outlet, by construction.

**Minimum separation.** If two massifs are closer than twice the spur reach, their spur systems would
have no room. The rule is then to shorten both systems' reach to the medial distance rather than to
let them interpenetrate — the same "shrink rather than fold" principle Stage 2B settled on.

The result is one regional relief system: divides that connect massif to massif, corridors that run
between them, and basins where the corridors are enclosed — not several independent mountain blobs.

---

## 8. Hydrology handoff (Stage 4 contract)

Stage 4 is `HydrologyPlanner.Plan(HydrologyGrid, HydrologySettings, IWorldSeed)`. It builds its own
`HydrologyGrid` from a height field and derives everything else. **Stage 3 does not hand it a drainage
graph and must not.**

What Stage 3 hands over:

| Artefact | Consumed how |
|---|---|
| **The Stage 3 height field** | The only mandatory output. `HydrologyGrid` is built from it exactly as today. |
| **Divide skeleton** — ridge curves at depth 0 and 1, plus inter-massif divides | A *check*: Stage 4's watershed boundaries should follow them. A river crossing a depth-0 divide is a defect in one stage or the other. |
| **Valley corridors** — floor polylines with downstream links | A *check*: reaches should occupy corridors. Also a natural seeding hint for `RiverNetworkBuilder` if it ever wants one; it does not need one today. |
| **Basin candidates** — polygon, floor elevation, declared outlet point and elevation | A *check* against `BasinExtraction.LakeCandidate.SpillPointXZ` / `SurfaceElevation`. A declared basin the planner does not find, or a lake the planner finds that Stage 3 did not declare, is a defect. |
| **Monotone-corridor guarantee** (§4.2) | Predicts that `DepressionFill` finds no pits outside declared basins. |

The relationship is deliberately one of **declaration and validation**, not of substitution:
`HydrologyPlanner` stays the authority on drainage, and Stage 3's structures are how we can tell
whether its answer is geographic. That also means Stage 3 can be implemented and shipped before any
hydrology change at all.

Ownership, stated once:

```
Stage 1   coastline, landmass, base field
Stage 2   macro axes, certified core, massif envelope
Stage 3   ridge / spur / valley / basin topology and the carving field
Stage 4   drainage: flow, reaches, lakes, waterfalls
Stage 5   refinement: T2 plateaus, T3 outcrops, surface detail
```

No structure is owned twice. Stage 3 references the crest rather than copying it; Stage 4 derives
lakes rather than importing Stage 3's basins.

---

## 9. Deterministic world-coordinate evaluation

Stage 3's height contribution must be a pure function of world XZ.

**Build phase** (once per world, no lattice involved):
1. Read the massifs. For each, for each flank, place attachments along the certified core.
2. Build spur curves, then secondary spurs.
3. Derive valley corridors as the strip complements; link them downstream.
4. Find basins by walking the enclosing structure; declare outlets.
5. Build a **uniform bucket index** over the world (bucket ≈ 64 m) holding, per bucket, the ids of the
   strips whose support overlaps it.

**Evaluate phase** (any `(x, z)`):
1. Bucket lookup → a handful of candidate strips.
2. For each, project into its parent's frame, get `(s, v)`, reject if outside.
3. Take the deepest applicable carve — strips do not overlap, so at most one applies inside a massif;
   corridors on the apron may overlap only at confluences, resolved by taking the maximum depth, which
   is continuous.
4. Multiply by the coastal gate, subtract from `H₂`.

**Seed streams**, all through `IWorldSeed.Fork`:

| Stream | Decides |
|---|---|
| `terrain.ridge.spurs` | attachment count and station jitter, per massif and flank |
| `terrain.ridge.shape` | per-spur lateral drift and reach, `Fork(id, massifIndex, spurIndex)` |
| `terrain.ridge.secondary` | secondary attachments and shape |
| `terrain.ridge.valley` | per-corridor depth-profile variation, bounded |
| `terrain.basin` | which enclosed candidates become basins, and outlet selection among ties |

No tile index, no chunk coordinate and no lattice index is ever an input. The four `TerrainData`
tiles are windows onto one field; a seam cannot exist because nothing in the construction knows where
a seam would be.

---

## 10. Data model

```
RidgeNetwork                     // one per world
    RidgeCurve[]   Ridges        // depth 1 and 2 only; depth 0 is a reference to a MacroAxis
    Corridor[]     Corridors     // valley floors, including inter-massif ones
    Basin[]        Basins
    StripIndex     Index         // uniform buckets → strip ids

RidgeCurve   { Id, ParentId, Depth, MassifIndex, FlankSign,
               Points[], Cumulative[], LengthMeters, AttachS,
               HeightBudget: ProfileCurve, HalfWidthMeters }

Corridor     { Id, LowerSpurId, UpperSpurId, MassifIndex, FlankSign,
               SInterval, HeadS,                       // where it starts on the crest
               Floor[], Cumulative[], LengthMeters,    // the trajectory, head to termination
               ToeIndex,                               // where the massif part ends
               HalfWidthMeters, MaxDepthMeters,
               Termination (Coast | Basin | Merge | Divide),
               DownstreamId (corridor | basin | coast) }

Basin        { Id, Boundary[], EnclosingRidgeIds[], FloorElevationMeters,
               OutletPointXZ, OutletElevationMeters, OutletCorridorId }
```

Plain data, no `UnityEngine` dependency beyond `Vector2`, rebuildable from seed and settings, not
persisted — the same rules `WorldStructure` already follows.

New settings struct `RidgeSettings` beside `MassifSettings`, with `Default` and `Clamp()`. It owns
spacing per half-width, spur count bounds, reach fractions, drift bounds, depth fractions, the
cross-valley profile's shape, apron decay, basin thresholds and the recursion cap. It reads the
coastal gate from the same place Stage 2A and 2B do, so all three stages agree on one number.

---

## 11. Measurable invariants

| Invariant | Statement | Failure condition |
|---|---|---|
| **Ridge continuity** | Every ridge curve has ≥ 2 samples, its arc length is within bounds, and its height budget reaches 0 at its far end | a ridge ending at non-zero budget |
| **Attachment** | Every depth-1 ridge attaches to a massif crest inside `CertifiedCore`; every depth-2 ridge attaches to a depth-1 ridge | an orphan curve |
| **No self-intersection** | No ridge polyline crosses itself (`Geo2.SegmentIntersect` over non-adjacent segments) | any crossing |
| **No ridge crossing** | No two ridge curves intersect except at a declared attachment | any crossing |
| **Strip partition** | Strips of one flank have disjoint `s`-intervals covering the flank; measured, not assumed | overlap or gap |
| **Bounded recursion** | `Depth ≤ 2` for every curve | depth 3 |
| **Bounded spacing** | Adjacent attachment spacing within `[min, max]` of the target | comb-like or crowded |
| **Corridor monotonicity** | Carve depth non-increasing downstream along every corridor | a pit outside a declared basin |
| **Valley heads** | Every corridor's head is on a divide at depth 0 or 1 | a headless valley |
| **Basin validity** | Exactly one outlet; floor below outlet; perimeter is ridge structure over the stated majority | zero or several outlets |
| **Coast termination** | Carve is 0 at the shoreline; ocean samples byte-identical to Stage 2 | any ocean sample changed |
| **Subtractive** | `H₃ ≤ H₂` everywhere, and `H₃ ≥ H₁` on land | ground raised, or cut below the base island |
| **Massif continuity** | Every massif has ≥ 1 spur per flank where its flank is long enough to hold one | a massif with no ridge structure |
| **Slope bands** | ≥ 95 % reachable at ≤ 45°, 6–15 % > 45°, ≥ 35 % < 5° — the existing targets, unchanged | outside the bands |
| **Isotropy** | Curvature ratio from `OverworldTerrainDiagnostics`, no worse than Stage 2's | a directional bias appearing at Stage 3 |
| **Determinism** | Same seed → identical field hash and identical structure | any difference |
| **Stage 1 / Stage 2 immutability** | Stage 1 hashes unchanged; `CertifiedCore`, certified width and massif envelope unchanged | any change |

---

## 12. Diagnostics

Top-down diagrams, in the existing `Editor/TerrainGen*Dump.cs` idiom — flat colour, drawn lines, no
lighting. We are debugging geography.

1. **Skeleton** — coast, massif axes with certified cores, ridge curves coloured by depth
   (0 crest, 1 primary, 2 secondary), attachment points marked.
2. **Strip partition** — every strip filled with its own colour. Overlaps and gaps are visible
   immediately; this is the picture that proves ridges cannot cross.
3. **Corridors** — valley floor lines with downstream links drawn as arrows, heads marked.
4. **Basins** — polygons with their outlet point and elevation labelled.
5. **Carve field** — `C(x, z)` alone, with contours, so the ridge/valley alternation is legible
   without the massif underneath.
6. **Height + hillshade** — the Stage 3 field, using the existing hillshade so shape rather than
   palette is what is read.
7. **Slope bands** — the three bands from the principles as three flat colours, with the percentages
   printed, measured against the existing targets.
8. **Profile sheet** — per massif, a cross-flank section through several `s` stations showing the
   ridge/valley alternation as a 1-D chart, and the corridor depth profiles.

Plus a text report per seed in the existing style: ridge and corridor counts, depth histogram,
spacing min/mean/max, corridor monotonicity violations, basin count with outlet elevations, slope
band percentages, isotropy ratio, field hash, timings.

---

## 13. Performance

| Quantity | Expected |
|---|---|
| Massifs | 1–2 |
| Flanks | 2 per massif |
| Primary spurs | 2–5 per flank → 8–20 per world |
| Secondary spurs | ≤ 2 per primary → ≤ 40, typically ~15 |
| Ridge curves total | ~25 typical, ~60 worst |
| Corridors | spurs + 1 per flank → ~30 |
| Basins | 0–3 |
| Graph size | ~100 curves, ~100 links |
| Samples per curve | ~40 at 20 m spacing |

Build cost is dominated by curve construction and is comparable to Stage 2A's axis phase — tens of
milliseconds.

Evaluation: 1025² = 1.05 M samples, one bucket lookup plus 4–8 strip tests each, ~8 M projections.
That is the same class as Stage 2B's field pass (0.2–0.9 s measured) and well under Stage 1's 5–6 s.
The bucket index is 44 × 44 at 64 m.

**Explicitly avoided:** evaluating all features per sample; any per-pixel search over the whole graph;
any raster intermediate at Stage 1's resolution during the build phase.

---

## 14. Implementation stages

The task's suggested split is close, with one change: **valleys are the dual of spurs and cannot be a
separate later slice**, and integration must come early so the geography can be *seen* before more
structure is layered on. That was the lesson of Stage 2 — three passes were spent on shapes that
looked plausible in numbers and wrong in pictures.

### Stage 3A — skeleton and ownership
- **In:** `Stage2BResult`.
- **Out:** massif ownership partition, inter-massif medial divides, flank definitions. No spurs.
- **Invariants:** ownership is a partition; divides terminate on the coast or on a massif.
- **Tests:** determinism; partition covers the land exactly once; divides do not cross axes.
- **Diagnostics:** skeleton diagram (1).
- **Non-goals:** no spurs, no carving, no height change.

### Stage 3B — primary spurs and the flank partition
- **In:** 3A.
- **Out:** depth-1 `RidgeCurve[]`, strips, corridor floor lines.
- **Invariants:** partition, spacing bounds, attachment inside `CertifiedCore`, no self-intersection,
  no crossings, spur budget reaching zero.
- **Tests:** the partition invariant on ≥ 24 seeds; spacing bounds; attachment; determinism.
- **Diagnostics:** strip partition (2), corridors (3).
- **Non-goals:** no secondary spurs, no basins, still no height change.

### Stage 3C — valley trajectories, carving field and integration
- **In:** 3B.
- **Out:** valley trajectories (floor polyline, arc length, downstream link, termination) including
  **the corridor continuation**, `C(x, z)`, the Stage 3 `WorldHeightField`, the bucket index.
- **Invariants:** subtractive; ocean unchanged; `H3 >= H1` outside declared corridors and
  `H3 >= H1 - CorridorIncisionMeters` inside them; `H3 > SeaLevelMeters` on land; corridor
  monotonicity; no closed depression outside a declared basin; slope bands; isotropy; Stage 1 and
  Stage 2 immutability; determinism.
- **Tests:** the invariant table, an independent depression scan of `H3` that shares no code with the
  carve, and a Stage 2 regression comparing envelope and certified core.
- **Diagnostics:** valley skeleton, trajectories, terminations, carve field, `H3` contours,
  independent depression map, surface render, profile sheet.
- **Non-goals:** no secondary spurs, no basins, no rivers, no lakes.
- **This is the first slice that can be judged visually, and it is deliberately early.**

### Stage 3D — secondary spurs
- **In:** 3C.
- **Out:** depth-2 curves and their strips.
- **Invariants:** recursion depth ≤ 2; tier separation ≥ 2.5× against the parent; partition holds
  within the parent strip.
- **Tests:** depth cap; tier separation measured from the field; no new crossings.
- **Diagnostics:** skeleton and strips updated; profile sheet showing the second level.
- **Non-goals:** no third level, ever.

### Stage 3E — structural basins
- **In:** 3D.
- **Out:** `Basin[]` with declared outlets; corridors linked through them.
- **Invariants:** basin validity; exactly one outlet; no pit outside a declared basin.
- **Tests:** every declared basin is found by an independent depression scan of the field, with the
  outlet within tolerance; no undeclared pit exists.
- **Diagnostics:** basins (4).
- **Non-goals:** no lakes, no water surface, no drainage.

### Stage 3F — hydrology handoff surface
- **In:** 3E.
- **Out:** the published divide skeleton, corridor links, basin declarations (§8).
- **Invariants:** every published structure is consistent with the field it was built into.
- **Tests:** the declaration/validation cross-checks of §8, run against the *existing*
  `DepressionFill` / `BasinExtraction` as read-only consumers.
- **Diagnostics:** an overlay of declared corridors against an independently computed flow direction.
- **Non-goals:** no change to `HydrologyPlanner`, no rivers, no lakes.

---

## 15. Risks

1. **Short massifs.** A 900 m massif with a 280 m half-width supports two or three spurs per flank.
   Below that the partition degenerates. Mitigation: a minimum spacing that reduces the *count*, and
   an accepted floor of one spur per flank; below that, the flank is left unstructured rather than
   given a token rib.
2. **Slope budget.** Carving raises the steep fraction, and the principles cap it at 15 % above 45°.
   The control is the corridor depth-to-width ratio; it must be tuned against the measurement, not
   assumed. This is the most likely source of a correction pass.
3. **Tier collision.** Valleys on a T1 massif are T1-scale; the apron continuation must stay clearly
   under the T2 band or it will read as a plateau edge. The 2.5× separation rule is the test.
4. **Close massifs.** Two massifs within twice the spur reach share a thin medial corridor. Shortening
   both is the stated response; the risk is that both end up stubby. Measurable, and the honest
   outcome may be that such worlds get a smaller ridge system.
5. **Isotropy.** Spurs perpendicular to a mostly-straight axis could introduce a directional bias that
   the curvature-ratio measure would catch. The bounded drift exists partly to prevent it; it must be
   measured, not asserted.
6. **The lowland stays flat.** A subtractive Stage 3 cannot fix a featureless island floor. That is
   correct and deliberate, and it must not become an argument for adding noise here.

---

## 16. Rejected alternatives

| Alternative | Why rejected |
|---|---|
| **Ridged fractal noise around the massif** | Explicitly banned. It defines topology by accident, produces ridges of uniform height and headless valleys — three of the named artificiality tells at once. |
| **A global periodic noise field, of any octave count** | Banned by the task and by the banding investigation, which traced the old pipeline's east–west anisotropy into `NoiseNode` itself. |
| **Voronoi or Poisson ridge fields** | Produces uniform parallel ridges and comb repetition; has no notion of a parent divide, so spurs would not descend from anything. |
| **Erosion simulation to carve valleys** | Belongs to refinement, not structure. It would also fight Stage 2's certified envelope and is far more expensive than a curve-level solution. |
| **Deriving ridges from flow accumulation on a perturbed base** | Inverts the architecture: it puts drainage before structure, and Stage 4 already owns drainage. It would also make Stage 3 depend on the stage that depends on it. |
| **Radial spurs from culmination points** | The Stage 2 failure mode, twice. A star of ribs around a summit is the "radially symmetric mountain" tell. |
| **Distance-to-nearest-ridge as the transverse coordinate** | Produces radial artefacts wherever a ridge ends — the same medial-axis family of defect that folded Stage 2's belts. The strip partition-of-unity avoids it by construction. |
| **An additive ridge layer on top of the massif** | Would break the height budget, the T1 tier and the certified envelope, and would let Stage 3 silently overwrite Stage 2's approved contract. |
| **A general node/edge graph for the skeleton** | More structure than the problem has. A few dozen curves with parent ids expresses a tree exactly; a graph would invite cycles that the partition forbids. |

---

## 17. Scope boundaries

```
Stage 1        untouched — frozen, hash-pinned
Stage 2 / 2A / 2B  untouched — no specification number changed, no reinterpretation
Stage 4        not started — HydrologyPlanner unchanged, consumed read-only for validation
Stage 5        not started — no T2 plateaus, T3 outcrops or surface detail
TerrainData    untouched      Terrain bake untouched Scene untouched
Materials, vegetation, roads, POI, settlements  untouched
Packages       untouched      Tests unchanged in this pass
```

This pass produced exactly one file: this document. No `.cs`, no `.unity`, no `.asset`, no production
change, and no implementation of any slice.

---

## 18. Verdict

# APPROVED

The design is coherent and implementable against the repository as it actually is:

- **It preserves the Stage 2 contract** by being subtractive. The certified core, the certified width,
  the massif envelope, the height budget and the coastline are all safe by construction rather than by
  checking, because nothing is ever added.
- **Its topology is not noise.** Ridges are a tree rooted on the Stage 2 axis; valleys are the
  complement of the spurs; the strip partition makes crossings impossible rather than improbable.
  Bounded variation exists but decides no topology.
- **It gives hydrology a real handoff** without duplicating ownership: the height field is the
  mandatory output, and the divide/corridor/basin declarations are checks the existing
  `DepressionFill` and `BasinExtraction` can be run against without modification.
- **It is deterministic in world coordinates**, with named seed streams and no lattice, tile or chunk
  input anywhere in the construction.
- **It can be built incrementally** in six slices, with the first visually judgeable result at 3C, and
  no slice requires redesigning Stage 1 or Stage 2.
- **It is measured against the repository's existing targets** — the tier table, the 2.5× separation
  rule, the three slope bands, the isotropy ratio and the named artificiality tells — none of which
  this pass changed.

Stage 3 implementation must not begin from this document without its own task. Stage 4 must not begin
at all.
