# LMC — Technical Draft (agent's v0.1 of the design note, not approved)

Version: 0.1

Status: **Draft — technical source material, not approved and not governing.** This is the agent's first
version of the LMC design note (2026-10-21). The owner replaced it the same day with a revised design note,
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (v0.2, now v0.3), which **governs**. This file is kept,
unchanged below this banner, as **input** for the documents that note requires before any code: the metric
calculation and threshold specification (note §12 item 6, §7.1), the S-LM3 model contract and its
mathematical assumptions (note §4.1, §12 item 7), the elevation definitions of note §6, and the file breakdown
of note §10. Nothing in it is approved by being kept here.

**Where this draft differs from the design note, the note governs.** Known differences: plateau edge classes
are the note's **18–40 m and 60–120 m** (not the ratio classes {0.10, 0.20, 0.30} of §3.4 below); the hard-gate
table, the passage rule, the review protocol, the step list and the owner decisions are the note's (§§7–9, 12
there); Q1–Q9 keep the definitions of the post-G3c review §3.3, and §6.4 below is only a candidate operational
form of them for the metrics specification; the owner-decision list of §12 below is superseded. Section
references inside this draft (§3, §6, §12 …) point to this draft.

Last Updated: 2026-10-22 (pointer to the technical specification; 2026-10-21: banner added when the owner's
revision replaced it as the design note)

**Since 2026-10-22** the material below has been carried into
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md), which states the definitions, the S-LM3 model
contract and the gate and Q calculations for approval and checks them on historical G3c data. Where the two differ,
the specification governs (for example the datum: world height, not height above sea level). This file stays as
history.

Original status line (v0.1, superseded): *Draft — step 0 of the comparison, awaiting the owner's approval …
nothing here is implemented, and steps 1–5 are not authorised.* Production terrain stays
`H4A = H1 + U2 + R4A`.

Depends On:
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) (§§1, 3–5),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§3.6, 7, 11–14),
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (D6, §§4, 7–8),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md) (§§3.1–3.3, 16),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) (§§9–11, 16),
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) (§§20–22),
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-1 … GEO-9, 3F-20,
[AGENTS.md](../../../AGENTS.md)

Referenced By:
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (agent review notes);
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md);
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §15

**Name.** "LMC" is the working name the owner used when approving the pair. It is not a G-series ID (G3d stays
reserved for coastline variety); whether it gets one is the owner's (§12).

---

## 0. How to read this

- **Every number is an authoring default in world units** unless it is named as an existing project value
  (with its source). None is measured or approved. Calibration (step 1) may change authoring defaults of the
  **construction** to fix named defects; it may not change a reading, a definition, a threshold or the
  decision rule after the owner approves this note (§8).
- **Tags:** **[FACT]** read from code or a document; **[PROPOSAL]** this note's choice; **[HYPOTHESIS]**
  believed, with the check that settles it; **[CARRIED]** taken unchanged from G3c's frozen record so the two
  experiments stay comparable.
- **Terms:** *layout* — the shared LM2 description of regions, boundaries and structure sites; *synthesis* —
  the code that turns a layout into a height lattice (S-LM3 or S-LM1); *candidate* — one synthesis's lattice
  for one seed. *Settlement site* and *POI site* are the working terms of brief §0 (Glossary entries await D9).

---

## 1. Question and hypothesis

**Question.** On the baseline footprint (contour A), does a shared typed-region layout synthesised by
**S-LM3** (structure fields with a fluvial steady state) or by **S-LM1** (a generalised skeleton strip
partition) produce the D6 geography — readable at island, regional and player scale — while meeting the hard
gates, where G3c's distance-field construction did not?

**Hypothesis [HYPOTHESIS].** G3c's failures of form — planar facets, whale-back crests, a radial cone, drum
plateaus, an over-continuous cliff band — come from how its surface was made, not from its layout. Holding a
layout fixed per seed and changing only the synthesis removes them for at least one synthesis. *Settled by:*
the form diagnostics Q1–Q7 and the owner's review, compared between the two syntheses on the same layouts and
against G3c's contour-A reference lattices.

**What LMC tests:** the surface model, with the layout held equal; whether either synthesis meets the hard
gates and the review on held-out seeds.

**What LMC does not test:** the footprint (GEO-1: contour A is the baseline; no contour B); coastline
variety and area against shape (G3d, later); production placement (GEO-3 at G4); whether a wall stops a
jumping player (3F-20 — walls are reported as steepness and walking connectivity, never as impassable);
lakes (GEO-5's realisation — LMC declares no basins); the large-plateau contract itself (GEO-8 — LMC gathers
evidence for its numbers, §3.4).

---

## 2. Fixed conditions

- **Contour A only**: the Stage 1 coastline of seed 110201 with production `TerrainGenSettings`; its Stage 1
  field hash must equal the pinned `7599879a71250a06` in every process **[FACT, G3c]**. Every seed of LMC uses
  this contour; a seed drives only the research streams (§2.1).
- World 2 800 m; lattice 1 025² at 2.734375 m; sea level 24 m; height budget 700 m **[FACT]**. Land is written
  at or above the shore level 24.5 m and sea cells are copied from Stage 1, so the shoreline is the contour's
  by construction **[CARRIED]**; LMC additionally **asserts** that the land mask of every candidate equals
  contour A's (0 cells changed).
- **One layout per seed, identical for both syntheses**: built once, written to disk, hashed, and read by both.
  A layout change during calibration is re-run through both syntheses.
- **No noise** in either synthesis: variation comes from seeded layout and synthesis parameters only; per-cell
  hashing is used for deterministic tie-breaking, never as height (Stage 3 design §16; AGENTS terrain rules).
- **Production untouched** (§9): Editor-only research code referenced by nothing; outputs outside `Assets`;
  production `H4A` rebuilt and its pinned hash checked in every process; MD5 manifest of production files before
  and after; G0, G1, the hydrology package and the G3/G3c research files unchanged.
- **Not committed** without the owner.

### 2.1 Determinism and streams

`System.Random` through `WorldSeed.Fork` on research streams only — `research.lmc.layout.*`,
`research.lmc.lm3.*`, `research.lmc.lm1.*` — which no production stage reads; per-cell tie-breaks by
stateless `TerrainHash` keyed on the same streams; every solver visits cells in a fixed order; floating-point
reductions in a fixed order. The determinism repeat of step 5 must be byte-identical (§8).

### 2.2 Seeds

| Role | Seeds | Rule |
|---|---|---|
| calibration | **110201, 1337** | the only seeds generated, measured or viewed before the freeze **[CARRIED]** |
| held-out evaluation | **1005–1014** | not generated, measured or viewed by LMC code before the freeze record exists; a construction change after the freeze voids the evaluation, which moves to **1015–1024** |

The G0 sweep measured production `H4A` on 1005–1014 on 2026-10-18; with a fixed contour that is a different
island, and no G3-family code has used these seeds. G3c's evaluation seeds are not reused (their views have
been seen).

---

## 3. The shared layout (LM2)

The layout is a typed planar partition of contour A's land plus **structure sites** — the places and target
heights both syntheses must honour. It contains no heights except targets.

### 3.1 Inputs

Contour A's land mask, mainland (largest 8-connected component), Euclidean shore distance `sd`, and the
mainland's weighted-diameter spine on a 10.9 m grid **[CARRIED from G3/G3c]**.

### 3.2 Regions

| Code | Region | Rule [PROPOSAL] |
|---|---|---|
| H | highland — the mountain system | **0.30 of land** **[CARRIED, G3c's frozen s30]**; grown along the spine from a **core** placed at a seeded fraction **t ∈ [0.30, 0.70]** of the spine's length (G3c always used the medial core — lesson L8), preferring wide land; no cell closer than G3c's minimum shore distance |
| L | lowland basins | **2–3 basins, 0.30 of land together** **[CARRIED]**, seeded at the widest points outside H; each touches its own coast with ≥ 150 m of frontage (Stage 4A's lowland frontage **[FACT]**) and owns one **designated outlet** there |
| V | valley corridors | from the designated pass (§3.3) down into a lowland, and from H into each lowland: the trunk axes, each ending at its basin's outlet |
| P | plateaus | §3.4 |
| U | uplands and foothills | the rest of the land; the part within 200 m of H is labelled foothill F (reported, not a separate rule) |

Region outlines are smoothed to a 30 m minimum feature width. The region graph — which region drains into which
— is fixed by the layout: P → U or H → V → L → outlet, and coastal U and H fringes drain directly to the sea.

### 3.3 The mountain system's structure sites

- **Main divide**: the part of the spine inside H. **Branch ridges: 2 or 3**, seeded, by G3's greatest-protrusion
  rule (protrusion ≥ 200 m = `UpliftCoastGateMeters` **[FACT]**), each kept inside H; which side branches first
  is seeded.
- **Culminations: 2–4**, ≥ 450 m apart, on the widest H points, at branch roots and at branch ends. Target
  heights: the **main culmination 330–430 m above sea level** [PROPOSAL — production's massif reaches 364.8 m
  and G3c's systems 253–308 m, whose relief sat near the T1 floor (review §2.2); the range stays inside the T1
  row and the 700 m budget]; the others **× 0.62–0.82** of the main one (`SecondaryHeightMin/Max` **[FACT]**).
- **Saddles** between consecutive culminations at **× 0.55–0.72** of the lower one (`SaddleFractionMin/Max`
  **[FACT]**).
- **One designated pass** on the main divide, where it separates two lowland regions: target height = the
  higher adjacent lowland foot + **40–80 m** [PROPOSAL — a readable col, unlike G3's high saddles; G3b's
  corridor stood 18–20 m above the floor and erased the col]. Its two sides lead into V corridors.

### 3.4 Plateaus

- **2–4 plateaus**, seeded, centres ≥ 500 m apart, in different regions, each on U or F ground adjoining H or a
  branch ridge **[CARRIED counts and spacing]**; **top area 0.03–0.15 km²** **[CARRIED]**.
- **Outline**: irregular, with **1–3 re-entrants** — embayments where a rim outlet valley cuts back into the
  top (the concept image's dissected edges; lesson L2).
- **Rim segments, typed**: *joint* — the top meets the adjoining ridge or foothill without a wall, **≥ 15 % of
  the rim**; *escarpment* — **20–70 % of the rim** (R2 needs ≥ 20 %); *ramp* — at least one, ≥ 40 m wide, the
  walking access; the rest *gradual*.
- **Escarpment height class** drawn per plateau from **{0.10, 0.20, 0.30} × the main culmination's relief above
  the plateau's foot** [PROPOSAL — the GEO-8 evidence route: several height ratios, the lowest usually inside
  the T2 row, the others above it]. A plateau's top always stays below the main culmination. The tier rules are
  reported, never applied (GEO-8's resolved principle).
- **Rim outlets**: one per re-entrant; each drains the top through the escarpment into U, H or V.

### 3.5 Boundaries and shore intents

- **Boundary types**: H–U, H–L, L–U and corridor walls *gradual*; P rim segments as typed in §3.4.
- **Shore intents** per shore segment, from the hinterland within 300 m: within 100 m of an outlet → *low*;
  lowland or corridor front → *low*; H or P within 300 m and **no valley mouth or outlet within 150 m** →
  *cliff*; upland → *rocky*; segments shorter than 80 m merge into their neighbours. An intent is a **boundary
  condition for the synthesis, never a class**: the realised class is measured on the candidate (R6, Q7), so
  the coast reading is not circular as G3c's was (review §1.2).

### 3.6 Basins and outputs

No declared basins: every closed depression on a candidate is a defect (GEO-5; R7). Lake-site candidates are
reported only.

Layout file per seed: region codes (u8 lattice), boundary-type map, divide tree (polylines, node kinds and
target heights), plateau records (outline, rim segments, class, outlets), outlets, shore intents, and its
hash.

---

## 4. Synthesis S-LM3 — structure fields with a fluvial steady state

### 4.1 The law

Every land cell `i` drains to one receiver `r(i)`; heights are integrated upstream from the shore:

```
h(i) = h(r(i)) + d(i, r(i)) · s(i)
s(i) = min( S_cap(i),  k_s(i) · A(i)^(−θ) )                         channel regime, A ≥ A_c
s(i) = min( S_cap(i),  k_s(i) · A_c^(−θ) ) · A(i) / A_c             hillslope regime, A < A_c
```

`A` is the upstream drainage area, `θ = 0.45` (the stream-power concavity; real landscapes commonly report
roughly 0.3–0.7 — reference, not target), `A_c = 2 000 m²` the hillslope–channel transition [PROPOSAL]. The
hillslope regime makes hilltops convex and the channel regime makes long profiles concave, which is what a
distance field could not do (lesson L1). Base level: land cells adjacent to the sea at 24.5 m. Because every
cell stands above its receiver, **no closed depression can form** by construction.

### 4.2 Receivers

Receivers are the steepest descent on the current heights **among neighbours the region graph allows** (a cell
drains within its region or into the region downstream of it, §3.2), with ties broken by the per-cell hash.
Receivers and heights are iterated until the receiver set is unchanged or 30 iterations pass. Two checks are
reported every run: the share of cells whose constrained receiver differs from the unconstrained steepest
descent on the final surface, and the production planner's acceptance (R10).

### 4.3 Fields from the layout

| Field | Where | Value [PROPOSAL] |
|---|---|---|
| `k_s` steepness | lowland L, corridor floors V | low (k_lo) |
| | uplands U, foothills | medium (k_up) |
| | highland H | high (k_hi) × a culmination factor rising toward each culmination along the divide tree |
| | plateau top | low (k_top) |
| | plateau escarpment band (a 20–40 m band outside escarpment rim segments) and coastal *cliff* intent band (60 m) | hard (k_rim) |
| `S_cap` slope cap | general hillslopes | **38°** (below the 45° walking limit, so ordinary slopes stay walkable) |
| | escarpment and cliff bands | **70°** (walls; above the tested jump range only where steeper, reported) |
| floor rule | L and V cells within a floor half-width of a trunk draining ≥ 150 000 m² | drain toward the trunk at a 1° cross-fall, so valleys widen into plains |

`k_lo`, `k_up`, `k_hi`, `k_top` and `k_rim` are not chosen by hand: they are set by **height targeting** (§4.4).

### 4.4 Height targeting

Heights grow monotonically with a `k_s` scale, so each target is met by bisection, in a fixed order: the main
culmination's highest point within 150 m to its target ± 2 m (scales `k_hi`); each secondary culmination by its
local factor; each plateau's escarpment to its class height ± 2 m (scales its `k_rim`) and its top relief to
≤ 5 m (scales `k_top`); the pass to its target ± 5 m (scales the corridor's `k_lo` across the divide). At most 12
bisection steps per target; achieved against target is reported. `k_up` and `k_lo` start from the ratio of the
region's intended relief to its drainage length and are calibration items. The maximum height is asserted below
the 700 m budget.

### 4.5 Plateaus, coast and settlements in S-LM3

- **Plateaus** are raised blocks: low `k_s` on the top drains to the rim outlets; the hard escarpment band makes
  the wall; a channel leaving through a rim outlet makes a steep reach — a fall site — at a re-entrant; ramps and
  joints have no hard band, so the top grades down or into the ridge under the same law.
- **Coast**: high hard ground at a *cliff* intent makes a steep seaward face; low `k_s` at *low* intents and
  outlets makes beaches and mouths; *rocky* uses upland values. The realised class is measured.
- **Settlement ground** is not declared: floors and plains come from low `k_s`, the floor rule and the plateau
  tops, and are measured by R3, R4, R11.

### 4.6 Risks named in advance

A uniform "eroded everywhere" texture (D6's forbidden mountain field) if `k_s` varies too little between
regions; threshold slopes at 38° over large areas (facets of a new kind — Q1 detects them); D8-parallel channels
on smooth slopes; the junction of an escarpment face with the plateau top; receiver iteration that does not
settle; targeting that cannot reach a target. Each, if it happens, is a named calibration defect (§8).

**Stage 3 §16.** Stage 3 rejected "erosion simulation to carve valleys" and "ridges derived from flow on a
perturbed base" inside its own architecture. S-LM3 is a steady-state law over designed fields with no envelope
to fight; for research that is what the owner approved, and whether the rejection's rationale carries over to
production is a question for G4 (§12).

---

## 5. Synthesis S-LM1 — generalised skeleton strip partition

### 5.1 Construction

- **Crest profile** along the divide tree through its nodes (culminations, saddles, the pass) by a monotone
  piecewise cubic, so no node overshoots and no control point draws a bench (G3's `ProfileCurve` lesson,
  research §11.6).
- **Flank frames**: each divide edge has two flanks, parameterised by `s` along the edge and `d` down the flank to
  the H boundary. **Spur stations** every **0.85 half-widths** with jitter **0.25**
  (`RidgeSettings.SpurSpacingPerHalfWidth`, `SpurStationJitter` **[FACT]**); strips alternate spur and valley
  (Stage 3 design §3.1). At a branch root, the wedge between the parent's and the branch's flanks is **one valley
  strip** bounded by the bisector — the trunk valley of that wedge.
- **Transverse coordinate**: `v ∈ [0, 1]` normalised between the bounding spur line and valley line (Stage 3
  design §3.3) — **never distance to the nearest ridge**, the construction that gave G3 its fans and facets.
- **Long flank profile**: `z = toe + (crest − toe) · (1 − u)^γ`, `u = d / D`, `γ ∈ [1.4, 2.0]` seeded — steep under
  the crest, flattening toward the toe (concave), not a constant slope.
- **Cross-valley profile**: an authored partition of unity — zero slope on the spur crest, steepening through the
  flank, flattening into a floor of finite width (Stage 3 design §3.3); valley depth **0.30 × crest**
  (`RidgeSettings.ValleyDepthPerCrest` **[FACT]**), tapering to the toe; every valley floor a **running minimum**
  downstream (the G3 lesson).
- **Lowlands and corridors**: a trunk from each flank's toe to the basin's outlet with a concave long profile;
  across the basin, a normalised coordinate between the trunk and the region boundary, rising gently away from the
  trunk; the pass corridor joins the trunks at the col.
- **Gradual boundaries** blend neighbouring regions by a partition of unity across a 60–150 m band, measured in
  normalised coordinates between the two boundary lines.
- **Plateaus** are skeleton elements: the rim curve shares nodes with the adjoining spur, the top is chosen at or
  below that spur's crest so a *joint* is a smooth saddle; *escarpment* segments get a 65–75° wall over the class
  height with a 20–40 m talus apron at 30–35°; *ramps* grade at ≤ 20° to the foot; the top falls ≤ 1.5° toward its
  rim outlets, which cut the re-entrants.
- **Coast**: the last boundary condition — *cliff* a 60–75° face to the sea level, *rocky* a ≤ 30° band, *low* a
  ≤ 3° band — blended along the shore by a partition of unity between segment break points.

S-LM1 evaluates every lattice vertex as a point function of the layout — the property production stages keep
(research §5.2).

### 5.2 Risks named in advance

Junction artefacts where frames meet at branch roots (G3b's amphitheatres); planar facets inside strips if the
profiles are too simple (Q1); pits where blended boundaries cross valley floors (R7); plateaus that still read
as stamped if joints are weak (Q6); authoring effort exceeding the prototype's scope. Each, if it happens, is a
named calibration defect.

---

## 6. Readings — pre-registered

Definitions reuse research record §14.1 wherever the wording is the same: *land* is height > 24 m; the ocean
is the border-connected sea of the hydrology package; *served slope* is the slope of the Triangle Reading
(SW→NE split); *T-edge reachable* means connected to a walkable shore triangle through walkable triangles
(< 45°) sharing edges, the jump not modelled; *buildable* is a lattice central-difference slope ≤ 6.84°
(road `maxGrade` 0.12 **[FACT]**). Regions are the layout's. Climbing axes are sampled at the lattice vertex
nearest each axis node **[CARRIED, §14.1 implementation note]**.

### 6.1 Hard gates

| Id | Definition | Provisional values |
|---|---|---|
| R4 | settlement sites by brief §4.1 — discs without sea and ≥ 90 % buildable, core radii 60 / 90 / 150 m, centres ≥ 450 m apart, capital first, then the coastal site, then by lowest mean slope; passes with a capital plus ≥ 2 further sites, ≥ 1 coastal, in ≥ 3 regions, **and all selected sites in one T-edge component** | D2 assumptions, provisional |
| R5 | on the hydrology package's network (`HydrologySettings.Default`): **≥ 1** river system with a source node in H or on a plateau, and **every** such system ends within 150 m of a designated outlet | — |
| R7 | 0 S8 depressions (fill ≥ 0.25 m over ≥ 100 m²); 0 lakes found by the analysis (no basin is declared); 0 declared axes climbing > 0.25 m downstream — declared axes are the layout's trunks in both syntheses, plus S-LM1's valley axes and S-LM3's channels draining ≥ 30 000 m² | — |
| R8 | every R4 site centre, verified plateau top (R2) and the designated pass T-edge reachable from the shore | — |
| R9 | every pair of regions holding a selected R4 site is joined by a T-edge route; reported per pair: route length over straight distance, highest served slope on the route, whether a second route exists that stays ≥ 100 m from the first over ≥ 50 % of its length | connection is the gate; detour and alternatives provisional, reported |
| R10 | `HydrologyPlanner.Plan` succeeds on the candidate lattice with `HydrologySettings.Default`; on failure the reason is recorded with the reach, node, bed elevations and the lattice heights there (nearest and bilinear), so tool and surface causes can be told apart | a failure counts as a candidate failure until shown to be a tool defect |
| process | land mask equal to contour A's; production `H4A` hash pinned; determinism repeat byte-identical | — |

### 6.2 Proxies, read with the review

| Id | Definition |
|---|---|
| R1 | (a) ≥ 2 summits of G0's merge tree with prominence ≥ 30 m inside H; (b) the flow-divide skeleton restricted to H has ≥ 1 branch point; (c) ≥ 1 pass — a key col inside H whose neighbours drain to ≥ 2 ocean outlets each draining ≥ 0.1 km², T-edge reachable; (d) G0's single-dome rule not flagged, read with the candidate's own floor **[CARRIED, §14.1]** |
| R2 | ≥ 2 verified plateaus ≥ 500 m apart: ≥ 60 % of top cells ≤ 5°; ≥ 20 % of rim samples (every 10 m) start a continuous served-slope run ≥ 45° dropping ≥ 10 m; rim within 150 m of H or U; top T-edge reachable **[CARRIED]** |
| R6 | measured shore classes — *low* (land within 60 m rises < 4 m), *rocky* (4–15 m), *cliff* (≥ 15 m): ≥ 3 classes each ≥ 5 % of the shoreline; ≥ 80 % of low cells with an L or V hinterland and ≥ 80 % of cliff cells with U, H or P hinterland (majority label within 300 m); every designated outlet on a low cell **[CARRIED]** |

### 6.3 Diagnostics

| Id | Definition |
|---|---|
| R3 | buildable area **inside each region** (an L basin, or the U ground draining to one ocean outlet): number of regions holding ≥ 10 ha |
| R11 | POI-site capacity per setting class and region — classes measured: culmination or viewpoint, pass, plateau top, plateau rim, escarpment foot, fall site (a channel ≥ 30 000 m² dropping ≥ 10 m within 30 m), valley floor or river bank, plain or meadow, headland or cliff coast, cove or beach, river mouth — thinned at 150 m within a class; share T-edge reachable reported |
| G0 | G0's M1–M6, D-a, D-b (Strahler, basin areas), D-c, D-e and T-a where they apply; slope shares; lake-site candidates |
| tier | per plateau: escarpment height, top height against the main culmination, which T-row it fits or falls between (`WORLD_RELIEF_PRINCIPLES.md` §1) |
| walls | per escarpment and cliff: served-slope classes (45–60°, 60–70°, > 70°) and steep-run heights; walls > 70° marked "outside the tested jump range", never "impassable" |
| bands | land use by distance to the sea: 0–60 m, 60–200 m, interior |

### 6.4 Form diagnostics Q1–Q9 (report only)

Values are compared between the syntheses and against two references measured the same way: production `H4A`
on 110201, and G3c's contour-A calibration lattices.

| Id | Detects | Definition [PROPOSAL] |
|---|---|---|
| Q1 | planar facets | share of land steeper than 5° whose surface fits a plane with RMS residual < 0.05 m over a 15 × 15-vertex window (≈ 41 m) |
| Q2 | missing concavity | per river system with ≥ 10 channel nodes draining ≥ 30 000 m²: θ from a least-squares fit of log slope against log area; the median over systems |
| Q3 | whale-back crests | on flow-divide skeleton cells inside H: median crest-normal curvature at a 30 m scale, and the median width of the band within 5 m of the local crest height |
| Q4 | single or uniform crests | separate summits (G0 M3 (c)); coefficient of variation of crest height along the main divide sampled every 20 m; depth of each saddle below its lower culmination |
| Q5 | radial cones | for each culmination with prominence ≥ 30 m: slope-weighted mean of the cosine between downslope direction and the outward radial, over the ring 100–400 m; 1 is a perfect cone |
| Q6 | overlay plateaus | per plateau: share of rim (10 m samples) whose outward drop within 60 m is < 10 m (joined); coefficient of variation of escarpment height along the rim; number of re-entrants (rim samples where a channel ≥ 30 000 m² leaves) |
| Q7 | over-continuous coasts | along the traced shoreline: the longest run of each measured class as a share of the shoreline; class changes per kilometre; for each outlet and valley mouth, the class of the run that holds it |
| Q8 | seed-invariant layouts | over the ten held-out seeds of one synthesis: median pairwise Pearson correlation of z-scored land heights; histogram of branch counts and core positions |
| Q9 | construction by accretion | named construction fixes in calibration, by kind (layout, S-LM3, S-LM1, measurement alignment) |

### 6.5 Measurement continuity check

Before step 1 iterates, LMC's measurement code runs on G3c's two **calibration** lattices of contour A (`s30-med`,
110201 and 1337, from G3c's screen outputs) and must reproduce G3c's R1, R2, R4 (without the new component
clause), R6, R7 and R8 values. Any difference is explained and logged before calibration starts. No G3c
evaluation lattice is read.

---

## 7. Views and the owner's review

### 7.1 Cameras

- **G1, unchanged:** `GeometryFirstReview.Run` with the candidate lattice and seed **110201**, so its anchors are
  contour A's Stage 1 **[FACT: G1 anchors on the seed's Stage 1 and reads the lattice from `-geoReviewLattice`]**
  — views V1–V6.
- **Structure-anchored views**, with G1's presentation (neutral material, fixed sun, field of view and
  resolution), drawn by LMC's own view code: an island overview; an oblique of the mountain system from the coast
  farthest from the main culmination; a regional oblique of each plateau rim from 400 m outside it at 35°; the
  designated pass; two outlets; one shore segment of each measured class; ground views at the capital site and at
  the pass. Anchors come from the **layout**, which is identical for both syntheses, so a seed's views of S-LM3
  and S-LM1 share every camera record.

### 7.2 Contact sheets and blinding

Per seed: one sheet per view set with the two candidates labelled **X** and **Y**, plus production `H4A` (110201)
as a fixed reference. Which synthesis is X on a seed comes from a key written into the freeze record and not
printed on the sheets; the sheet order is fixed by seed. Calibration sheets also show G3c's contour-A lattice for
the same seed as a historical reference (a different layout).

### 7.3 Review form

For each seed and candidate: each D6 point 1–8 at island, regional and player scale — *meets*, *partly* or
*fails*, with a note — and one overall verdict, **composition accepted: yes / no**. The agent's readings are
reported separately from the measurements and from the owner's review; the key is opened after the review.

---

## 8. Process, steps and the decision rule

### 8.1 Steps

| Step | What | Gate |
|---|---|---|
| 0 | this note | **owner approves it** — and with it the readings of §6 and the rule of §8.3 |
| 1 | build the layout and both syntheses; continuity check (§6.5); calibration on 110201 and 1337, fixing named defects only, each logged | **owner's go-ahead** to write code and run calibration |
| 2 | calibration review: both syntheses on both seeds, every view and reading | **owner chooses** S-LM3, S-LM1, both, or stops |
| 3 | freeze record: MD5 of every LMC file and of the G0, G1, hydrology and G3c sources; layout and synthesis settings; every rule value; contour hash; the blinding key; this note's hash | written before step 4 |
| 4 | held-out evaluation on 1005–1014 for each frozen synthesis; determinism repeat in a fresh process | **owner authorises** the run |
| 5 | the owner's review of the held-out sheets; report | — |

### 8.2 Process rules

- Calibration changes **construction** authoring defaults only, each as a named defect with its evidence; never a
  reading, a definition, a threshold or the decision rule. An implementation detail that does not change a
  definition (as G3c's nearest-vertex sampling) may be settled before the freeze and is recorded.
- A **layout** fix is re-run through both syntheses, so neither gets a layout tuned to it.
- The defect log is the Q9 evidence; there is no fixed budget, but many fixes for layer interactions are reported
  as a finding against that synthesis (lesson L7).
- After the freeze, any construction change voids the evaluation; it is rerun on 1015–1024.
- Failures are recorded by kind — construction, definition, tool, layout — as for G3c (research §14.7).

### 8.3 Decision rule (pre-registered with this note)

- A seed **meets the gates** for a synthesis when every hard gate of §6.1 holds on it.
- A synthesis **passes** when it meets the gates on **≥ 7 of 10** held-out seeds (G3c's bar) **and** the owner
  accepts its composition on **≥ 7 of 10** seeds.
- **Both pass:** the owner chooses; gate counts and Q1–Q8 inform the choice; production fit (point function,
  frozen stages) is weighed at G4, not here.
- **One passes:** it is the candidate taken to G4's design question; the other's failure is recorded with causes.
- **Neither passes:** causes recorded by kind; no footprint, coast or production conclusion follows; the model
  families of the review §4 are revisited before anything else is built.
- Every conclusion is stated about these two syntheses on this layout and contour, not about region-first
  geography or the footprint in general.

---

## 9. Code, isolation and verification

**Files** (Editor-only, namespace `GameDev.EditorTools`, referenced by nothing, uncommitted until the owner
decides D9):

| File | Content |
|---|---|
| `Editor/TerrainGenLmcDump.cs` | batch entry `DumpBatch` (`-lmcStep continuity|calibrate|evaluate`, `-lmcSeeds`, `-lmcSynth lm3,lm1`, `-lmcViews`, `-lmcOut`), contour A fixture, reports, freeze record |
| `Editor/TerrainGenLmcLayout.cs` | the shared layout (§3) |
| `Editor/TerrainGenLmcFluvial.cs` | S-LM3 (§4) |
| `Editor/TerrainGenLmcSkeleton.cs` | S-LM1 (§5) |
| `Editor/TerrainGenLmcMeasure.cs` | readings, diagnostics, Q1–Q9 (§6) |
| `Editor/TerrainGenLmcViews.cs` | structure-anchored views and contact sheets (§7) |

- **G3c stays byte-identical.** LMC copies the helpers it needs (contour fixture, grid utilities, T-edge reading,
  site placement, view presentation) instead of calling G3c's files, so G3c remains reproducible from its freeze
  record and either experiment can be removed without breaking the other. G0, G1 and the hydrology package are
  called through their public entry points, unchanged.
- **Outputs** outside `Assets`, in the session scratchpad: layouts, lattices (`.f32`), region maps, structure
  images, reports, view PNGs and JSON, contact sheets, the defect log, the freeze record.
- **Verification in every run:** contour A's Stage 1 hash; production `H4A` rebuilt and hash-checked; the land mask
  assertion; an MD5 manifest of production files before and after (the tile assets and `Settings/Terrain/`, the
  production scene and its folder, URP assets, `ProjectSettings/*.asset`, `packages-lock.json`, and the G0, G1,
  G3 and G3c sources); compilation free of errors and warnings; `git status` showing only LMC's files and
  documentation. The EditMode suite is not required (no tested code changes) and is run if any shared code is
  touched.

**Cost [HYPOTHESIS].** S-LM3: one height integration is O(n) on about 10⁶ cells; with up to 30 receiver
iterations and the bisections of §4.4, a few hundred integrations — seconds to tens of seconds per candidate.
S-LM1: one point evaluation per vertex — seconds. G1 rebuilds world 110201 per call (12–47 s measured in G3).
Calibration: four candidates plus iterations; evaluation: 20 candidates plus a 20-candidate repeat — an estimated
30–90 minutes with views. Measured in step 1.

---

## 10. Risks

| Risk | Mitigation |
|---|---|
| S-LM3 tuned toward the calibration views | only construction defaults move, each logged; held-out seeds unseen; the review is blinded |
| the layout favours one synthesis | one layout per seed for both; layout fixes re-run through both |
| S-LM1's junction work outgrows the prototype | report it as a finding (Q9) rather than simplify S-LM1 into a distance field |
| S-LM3 produces a uniform eroded field | Q1, Q4, Q8 and the review detect it; region contrast in `k_s` is a calibration item |
| walls read as barriers in the views | reported as steepness and connectivity only; 3F-20 decides play |
| the planner rejects S-LM3's network despite drainage by construction | R10 records the node; the surface or tool cause is established before any conclusion |
| contour A limits what either synthesis can show | stated as a limit (§1); the footprint returns only on GEO-1's trigger |

---

## 11. What step 0 changes

Nothing is built and nothing runs. This note records the owner's approval of the pair and the experiment's
design. It pre-registers, for the owner's approval, the readings (R1–R11, Q1–Q9) and the decision rule — the
"v2 reading set" of the post-G3c review §3, now with operational definitions.

---

## 12. Owner decisions for this note

1. **Approve this note** — layout rules, both syntheses in principle, readings, review protocol, decision rule —
   or name the changes.
2. **Step 1 go-ahead**: write the LMC code and run the continuity check and calibration on 110201 and 1337.
3. **Name**: keep "LMC" as the working name, or assign a G-series ID.
4. **Height targets** (main culmination 330–430 m above sea level; pass 40–80 m above its lowland foot) and
   **plateau classes** ({0.10, 0.20, 0.30} of the culmination's relief) — accept as authoring defaults or set
   others.
5. **Later, at G4 only:** whether Stage 3 §16's rejection of erosion-as-structure applies to a steady-state law
   over designed fields in production.
