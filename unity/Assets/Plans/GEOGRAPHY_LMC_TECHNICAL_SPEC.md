# LMC — Technical Specification

Version: 0.5.3

Experiment: **G3e** (official ID, assigned by the owner 2026-10-23); working name LMC. Reference data for the
continuity check: `D:\Hobby\game-dev\research-data\lmc-reference\` (owner's decision C9; MD5 manifest there).

Status: Draft — **approved by the owner (v0.3 on 2026-10-22; amendments A1 and A3 and option (b) for the whale-back on
2026-10-23, written in as v0.4; amendment M-1 on 2026-10-23, v0.5 §14); Step 1 authorised 2026-10-23 and in
progress.** v0.4 adds: R8 over every declared plateau top (A1, §7.1); the
report-only slope diagnostics D-cap (with K5) and D-reg (A3, §7.3); Q4m as a report-only crest measure (with K4), and
the whale-back judged in the owner's visual review with neither Q4 nor Q4m flagged (option (b), §§8, 8.1). The owner approved
the world-height datum and §2 with K1, the S-LM3 model contract as an experimental representation, the gate
calculations with K2, the Q flag levels of §8.1, the layout validation and the S-LM1 ownership rule. These are now
fixed for LMC and never change to make a synthesis pass (§0). Writing LMC code and running calibration still need the
design note's Step 1 go-ahead. *Status as checked (v0.2):* checked on historical data, awaiting the owner's approval.
The owner supports the LMC design
note v0.3 as the research direction and has **not** authorised code or calibration (2026-10-22). This specification
completes what the note requires before Step 1 — the elevation definitions (note §6), the S-LM3 model contract (§4.1,
§12 item 7), the S-LM1 overlap rule (§5.2), the exact gate and Q1–Q9 calculations (§7, §12 item 6), the views (§8),
the freeze record and file breakdown (§§9–10) — and checks it on **historical G3c data only** (§11): 10 of 13
pre-registered checks held, 3 expectations failed, and the failures led to three corrections (K1–K3, §11.5). Where it
and the design note differ, **the note governs**. Nothing here is implemented; no generation ran for it.

Last Updated: 2026-10-23 (v0.5.3: §14 — the implementation accepted by the owner with caveats; the 150 m radius
stays. v0.5.2: §14 — implemented in the Step 1 measurement code and verified in run 7.
v0.5.1: §14 — the full rule checked on the preserved Step 1 lattices; the code not yet
changed. v0.5: amendment M-1 — the pass col rule — approved by the owner as a specification rule,
§14 and §2.3; its implementation not yet verified. v0.4.1: experiment ID G3e and the reference-data location recorded; content unchanged.
v0.4: A1, A3 and option (b) approved by the owner and written into §§7.1, 7.3, 8, 8.1;
v0.3.2: §13 — proposed amendments A1–A3, then not in force, checked on existing G3c data with
corrections K4, K5, and the calibration-grid reference for Step 1; §§1–12 unchanged. v0.3.1: annotation in §12 only — the note's approval and the readiness findings F1, F3;
the approved content is unchanged. v0.3: approved by the owner, §12 items 1–4; v0.2: verification results and corrections
K1–K3, §§11.4–11.5; v0.1: written and hashed before any computation)

Depends On:
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (v0.3),
[GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md) (source material),
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) (§§1, 3),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§7.6, 14),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md) (§§3, 7, 16),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) (§§9–11, 16),
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) (§§19–22),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-5, GEO-7, GEO-8

Referenced By:
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md);
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md);
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §15;
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-7; [AGENTS.md](../../../AGENTS.md);
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)

---

## 0. How to read this

- **Tags.** **[FACT]** read from code or a document, source named; **[CARRIED]** taken unchanged from G3c's frozen
  record (research §14.1) so LMC and G3c stay comparable; **[PROPOSAL]** this specification's choice, to approve;
  **[AUTHORING]** an authoring default in world units, adjustable in calibration only as a logged construction fix;
  **[MEASURED]** a value computed in §11 on historical data.
- **What may change after approval.** Construction authoring defaults (§§4–6) move only as named defects during
  calibration. Definitions and parameters of §§2–3 and §§7–8 are fixed by the owner's approval and never change
  to make a synthesis pass.
- **Verification boundary.** §11 reads existing G3c outputs and analytic test surfaces with throwaway Python
  scripts in the session scratchpad. No Unity process, no terrain generation, no LMC code.

---

## 1. Conventions

| Item | Definition |
|---|---|
| Lattice | 1 025 × 1 025 vertices, cell `c` = 2.734375 m, index `i = z · 1025 + x`, world position `(x c, z c)` **[FACT]** |
| Height | float32 metres of world height — the lattice value, the same quantity `TerrainData` stores over its 700 m budget **[FACT]** |
| Sea level | 24 m world height (`WorldScale.SeaLevelMeters`) **[FACT]** |
| Land | a vertex with height > 24 m; LMC writes land at ≥ 24.5 m **[CARRIED]** |
| Ocean | vertices ≤ 24 m connected to the world border through vertices ≤ 24 m (the hydrology package's ocean mask) **[FACT]** |
| Shore vertex | a land vertex 4-adjacent to an ocean vertex |
| Triangle reading | each quad split south-west to north-east into T1 = (00, 10, 11) and T2 = (00, 11, 01); a triangle is *land* when its centroid height > 24 m and *walkable* when its slope < 45° **[FACT, G3c `G3cTriangles`; handoff §22]** |
| T-edge graph | walkable land triangles joined across shared edges: T1(x, z) to T2(x, z), T2(x, z − 1), T2(x + 1, z) **[FACT]** |
| Shore-reachable | in a T-edge component that holds a walkable land triangle sharing an edge with a non-land triangle **[CARRIED]** |
| Vertex reachable | a vertex any of whose up to eight touching triangles is shore-reachable **[CARRIED]** |
| Vertex slope | central differences over ±1 vertex (5.47 m base); used only where named (buildable, Q1, Q5) **[FACT, D3 reading]** |
| Buildable | vertex slope ≤ 6.84° (road `maxGrade` 0.12) **[CARRIED]** |
| Regions | the layout's region codes per vertex (§4.2); ids per region instance |
| Distances | Euclidean in world metres; "within r" means ≤ r |
| Randomness | `System.Random` through `WorldSeed.Fork` on `research.lmc.*` streams; per-vertex tie-breaks by stateless `TerrainHash` on the same streams **[PROPOSAL]** |

---

## 2. Elevation definitions (design note §6, items 1–6)

1. **Datum [PROPOSAL].** All targets and readings use **world height** — the lattice value — not height above sea
   level. Sea level sits at 24 m world height, so "330–430 m" means 306–406 m above the sea surface. Reason: every
   project figure the targets are compared with is world height — the 700 m budget, sea level 24 m, G0's
   hypsometry bands, production's 364.8 m highest point, G3c's 253–308 m. *This corrects the technical draft,
   which wrote "above sea level" for the same range.*
2. **Summit height.** The main summit is the highest land vertex inside the layout's mountain region H; its
   height is that vertex's lattice value. A culmination counts as a summit when its topographic prominence on the
   G0 merge tree is ≥ 30 m **[CARRIED, R1]**. Target: main summit 330–430 m world height.
3. **Associated lowland of a pass.** The layout declares the pass and the corridor on each side; each corridor ends
   in one lowland region. The pass's associated lowland is, of those two, the one whose **lowland level** — the
   median lattice height of its vertices — is higher. **Pass offset** = pass col height − that lowland level.
   Target 40–80 m. ~~The col is the lattice key col of the declared pass (the merge-tree saddle within 150 m of the
   declared position).~~ *(amended 2026-10-23, M-1 — §14)* The col is chosen by the rule of §14: the merge-tree col
   joining the two realised flanking culminations, found only within 150 m of the declared position, otherwise "not
   found" — never a substituted vertex.
4. **Plateau edge height** *(corrected 2026-10-22, K1 — §11.5)*. Rim samples are the top's boundary vertices (top
   vertices 8-adjacent to a non-top vertex). For a rim sample `s`, ~~edge drop = `h(s)` − the lowest height of non-top
   land within 90 m of `s`~~ **edge drop** = `h(s)` − `h(f)`, where the **foot** `f` is found by walking from `s` by
   steepest descent over non-top land vertices (8-neighbours): the walk stops when a step's slope is below 15° on two
   consecutive steps, or after 150 m of path, and `f` is the last vertex reached by a step of 15° or more (`f = s` if
   there is none). A rim sample is an **escarpment sample** when its edge drop is ≥ 10 m and a triangle with slope
   ≥ 45° lies within 15 m of `s` outside the top. **Plateau edge height** = the median edge drop over its escarpment
   samples. A sample whose drop is < 10 m is **joined** (a ridge connection or a ramp; the layout's rim type tells
   them apart). *Why corrected:* the 90 m minimum also measured the fall of the ground beyond the foot (about 9 m on
   a 0.10 upland slope), so it overstated low escarpments by 30–80 % on G3c (§11.4, E5).
5. **Class crossing.** Each plateau has one layout class (18–40 m or 60–120 m, drawn with equal probability
   **[CARRIED, G3c `TallClassProbability` 0.5]**). Its measured edge height may vary along the rim; the
   along-rim variation is reported (Q6). A plateau whose measured edge height lies more than 25 % outside its class
   range is **flagged** for the review — a diagnostic, not a gate, and never re-classed after the fact.
6. **Mapping to the height representation.** Research lattices are float32 world heights and are measured
   unquantised. Production storage quantises to 2.14 cm steps (written heights within 1.07 cm) **[FACT, AGENTS]**,
   far below every threshold here; no reading depends on it. A target is "met" when the measured value lies
   inside its range; targeting tolerances are in §5.4.

---

## 3. Shared vocabulary for structure

- **Divide tree**: the layout's main divide and branch ridges as polylines with nodes (culmination, saddle, pass,
  branch root, end), each with a target height.
- **Outlets**: designated outlet points on the shore, one per lowland basin plus any declared for corridors.
- **Rim segment types** (per plateau): *joint*, *escarpment*, *ramp*, *gradual*; **rim outlets** at re-entrants.
- **Shore intents** per shore segment: *low*, *rocky*, *cliff* — boundary conditions only; realised classes are
  measured (R6, Q7).

---

## 4. LM2 layout contract

### 4.1 Construction (in principle)

As the technical draft §3, with the design note's values: mountain region H 0.30 of land; lowland basins 0.30 of
land in 2–3 basins, each with ≥ 150 m of coast frontage and one designated outlet; valley corridors from the
declared pass and from H to each basin; 2–4 plateaus (top 0.03–0.15 km², centres ≥ 500 m apart, on upland or
foothill ground adjoining H or a branch ridge, irregular outlines with 1–3 re-entrants, rim ≥ 15 % joint and
20–70 % escarpment, ≥ 1 ramp ≥ 40 m wide); uplands the rest. The mountain core sits at a seeded fraction
t ∈ [0.30, 0.70] of the spine's length (note §3.3). Branch ridges 2–3; culminations 2–4 ≥ 450 m apart; the
main summit target 330–430 m world height, others × 0.62–0.82 (`SecondaryHeightMin/Max` **[FACT]**); saddles
× 0.55–0.72 of the lower culmination (`SaddleFractionMin/Max` **[FACT]**); one declared pass with offset
40–80 m (§2.3).

### 4.2 Data

Region code per vertex — 0 sea, 1 lowland, 2 upland, 3 mountain (H), 4 plateau top, 5 plateau escarpment band,
6 plateau ramp, 7 valley corridor — plus instance ids (basin, plateau); the divide tree; plateau records (outline,
rim segments with types, class, rim outlets); outlets; shore intents; the layout hash.

### 4.3 Validation (every layout, before either synthesis reads it)

| Check | Rule |
|---|---|
| V-L1 coverage | every land vertex has exactly one region code; every sea vertex code 0 |
| V-L2 mask | the land mask equals contour A's (0 vertices differ) |
| V-L3 shares | H and the lowlands each within ± 2 points of 30 % of land |
| V-L4 connectivity | every region instance one 8-connected component; the region adjacency graph connected |
| V-L5 outlets | each basin has one designated outlet on its own shore vertices; outlets ≥ 400 m apart **[CARRIED]** |
| V-L6 plateaus | count 2–4; rim type shares and the ramp width as §4.1; every plateau adjoins H or upland within 150 m |
| V-L7 divide | 2–4 culminations ≥ 450 m apart inside H; the pass between two culminations on the main divide, with corridors leading into two different basins |
| V-L8 determinism | the layout hash equal in two fresh processes |

A layout failing a check is not synthesised; the failure is logged by seed and check.

---

## 5. S-LM3 model contract (note §4.1; owner decision §12 item 7)

### 5.1 What it is and is not

S-LM3 computes the **steady state of a detachment-limited stream-power law** on a drainage tree whose structure is
constrained by the layout. It is "erosion-inspired": a closed-form relation between slope and drainage area, solved
once per configuration, not a simulation of erosion through time, of sediment transport or of rock. It claims
three properties only: long profiles that steepen toward divides and flatten downstream; convex hilltops; and a
surface that drains to its base level everywhere by construction.

### 5.2 Equations

For land vertex `i` with receiver `r(i)`, horizontal distance `d_i` (c or c√2) and drainage area
`A_i = c² · (1 + Σ_{j: r(j) = i} A_j / c²)` in m²:

```
h_i = h_r(i) + d_i · s_i

s_i = min( tan φ_i ,  k_i · A_i^(−θ) )                        if A_i ≥ A_c     (channel regime)
s_i = min( tan φ_i ,  k_i · A_c^(−θ) ) · (A_i / A_c)          if A_i <  A_c     (hillslope regime)
```

- From `∂h/∂t = U − K A^m S^n = 0` with `n = 1`: `S = (U/K) · A^(−m)`; `k = U/K` (units m^(2θ)), `θ = m = 0.45`
  **[AUTHORING]** — a concavity in the range real landscapes commonly report (roughly 0.3–0.7).
- The hillslope regime makes slope proportional to drainage area — the steady-state analogue of linear diffusion,
  whose slope grows linearly with distance from the divide — so hilltops are convex. It is an analogue, not a
  diffusion solve. `A_c = 2 000 m²` **[AUTHORING]**.
- `φ` is the slope cap: 38° on general ground, 70° in escarpment and cliff bands **[AUTHORING]**.
- **Floor rule.** In lowland and corridor vertices within a floor half-width `w_f = min(85 m, 0.25 √A_t)` of a trunk
  vertex draining `A_t ≥ 150 000 m²`, the receiver points toward the trunk and `s_i = tan 1°`
  (85 m = `HydrologySettings.ValleyHalfWidthMaxMeters` **[FACT]**; the rest **[AUTHORING]**).
- **Base level.** Shore vertices: `h = 24.5 m`, no receiver.

### 5.3 Receivers and iteration

- **Allowed moves** (from the layout): within a region; across a *gradual* or *joint* boundary in either direction;
  out of a plateau top only through its rim outlets; from an escarpment band only away from the top; into the sea only
  from shore vertices. The coastal fringe of H and uplands may drain straight to the sea.
- **Receiver** = the allowed 8-neighbour of steepest descent on the current heights; ties broken by the per-vertex
  hash.
- **Start**: heights from the constrained shortest-path distance to base level × 0.05 **[AUTHORING]**, so the first
  receiver tree is defined.
- **Iterate**: receivers → topological order from base level → `A` → heights. Stop when the receiver set is unchanged
  or after 30 iterations **[AUTHORING]**. Non-convergence is recorded (named defect), the last iterate is used, and the
  oscillating vertex count is reported.
- **Guarantee**: every land vertex is higher than its receiver by `d_i s_i > 0`, so no closed depression exists. Two
  checks are reported: the share of vertices whose constrained receiver differs from the unconstrained steepest
  descent on the final surface, and R10.

### 5.4 Fields and height targeting

`k` is piecewise by region — lowland, upland, mountain (× a culmination factor rising toward each culmination along
the divide tree), plateau top, escarpment and cliff bands — and set by **monotone bisection** (heights grow with any
`k` scale), in this order: main summit to its target ± 2 m; each secondary culmination ± 2 m; each plateau edge to its
class target ± 2 m and top relief ≤ 5 m; the pass to its offset ± 5 m. At most 12 steps per target; achieved values are
reported against targets; the maximum height must stay below 680 m world height **[AUTHORING]** (700 m budget).

### 5.5 Determinism, cost, failure

Fixed visiting orders and reductions; heights in float32, integration in float64. Cost **[HYPOTHESIS]**: one
integration O(n) on ~10⁶ vertices; up to 30 iterations × up to ~60 bisection solves — tens of seconds per candidate.
Failures are logged, never hidden: non-convergence, an unreachable target, a height over budget.

### 5.6 Stage 3 §16 — the argument for the owner (§12 item 7)

Stage 3 rejected "erosion simulation to carve valleys" because it would fight Stage 2's certified envelope and cost
more than a curve solution, and "ridges derived from flow accumulation on a perturbed base" because it put drainage
before structure. S-LM3 has no envelope to fight (the layout and its fields are the structure), solves a steady state
rather than a time simulation, and derives ridges from designed fields, not from a perturbed base. The agent reads the
rejection as specific to Stage 3's architecture; whether it carries over to **production** is the owner's question at
G4. For LMC the owner's decision is only whether S-LM3 is a permitted **experimental** representation.

---

## 6. S-LM1 construction contract

As the technical draft §5, with one rule the note asks for (§5.2):

**Ownership and overlap.** Every land vertex has exactly one owner: the plateau whose outline contains it; otherwise the
coastal band of its shore segment within the coastal band width; otherwise the flank strip of the divide edge whose
`(s, d)` frame contains it, frames being cut at branch roots by the angle bisector between parent and branch (the
wedge valley); otherwise the lowland or upland region by its layout code. Ownership uses region membership and the
frames' extents, never distance to the nearest ridge as a height coordinate (Stage 3 design §3.3). **Influence fields
never add**: where two owners meet, a partition of unity across a declared blend band (60–150 m **[AUTHORING]**),
expressed in normalised coordinates between the two boundary lines, interpolates their heights; outside blend bands one
owner decides. Valley floors keep a running minimum downstream after blending.

S-LM1 evaluates every vertex as a point function of the layout.

---

## 7. Gate and indicator calculations (note §7)

### 7.1 Hard gates

| Gate | Calculation | Pass |
|---|---|---|
| **R4** | Sites by the G3c rule **[CARRIED, research §14.1]**: discs without sea and ≥ 90 % buildable; capital core radius 150 m, then the coastal site (core + 100 m reaches the sea), then further sites by lowest mean slope; centres ≥ 450 m apart; at most five. Then the T-edge component of each site's centre vertex. | a capital plus ≥ 2 further sites, ≥ 1 coastal, in ≥ 3 regions, **and all selected site centres in one T-edge component** |
| **R5** | On the production hydrology package's network (`HydrologySettings.Default`): a *system* is the set of reaches draining to one ocean termination; it is *elevated* when a source node lies in H or on a plateau top; it *ends at a designated outlet* when its ocean termination is within 150 m **[CARRIED]** of one. If the planner fails, R5 fails and R10 records why. | **≥ 1 elevated system, and every elevated system ends at a designated outlet** (owner decision, 2026-10-21) |
| **R7** | (a) S8 depressions: priority-flood fill depth ≥ 0.25 m over a connected area ≥ 100 m² **[CARRIED, G0 D-d]**; (b) lakes the analysis finds; (c) climbing axes — for each declared axis (the layout's trunks in both syntheses; S-LM1's valley axes; S-LM3's channels draining ≥ 30 000 m²) sampled at its nodes at the nearest vertex, `climb = max_k [h(n_k) − min_{j<k} h(n_j)]` | (a) 0, (b) 0 (LMC declares no basin, GEO-5), (c) every climb ≤ 0.25 m |
| **R8** | Vertex reachable (§1) at: each R4 site centre; ~~each verified plateau's top vertex nearest its centroid~~ **the top vertex nearest the centroid of every plateau the layout declares** *(A1, approved 2026-10-23)*; the declared pass col | all reachable |
| **R9** | For every pair of regions holding a selected site: the two site centres in one T-edge component *(the gate)*; reported per pair — the **detour** *(corrected 2026-10-22, K2 — §11.5)*: ~~route length over straight distance (shortest path over triangle centroids)~~ the shortest path on the 8-neighbour vertex graph over **walkable vertices** (every touching land triangle walkable; edge lengths c and c√2) divided by the octile distance between the two vertices — the same metric on open ground, so open flat ground gives exactly 1; the highest triangle slope along the T-edge route; and whether an alternative exists (removing walkable triangles within 100 m of the middle half of the first T-edge route leaves the pair connected) | every pair connected; detour and alternative reported, provisional |
| **R10** | `HydrologyPlanner.Plan(grid, HydrologySettings.Default, seed)` on the candidate lattice; on failure record the reason, reach id, node index, the two bed heights, and the lattice heights at those nodes (nearest vertex and bilinear) | `Succeeded` |
| process | V-L2 mask equality; production `H4A` pinned hash; determinism repeat byte-identical | all hold |

### 7.2 Indirect indicators — R1, R2, R6

Carried unchanged from research §14.1 (summits, branch point, pass with two outlet catchments, single-dome rule; verified
plateaus; measured shore classes and hinterland consistency). Reported beside the review, never gating.

### 7.3 Diagnostics — R3, R11 and the rest

As technical draft §6.3: R3 buildable area inside each region (≥ 10 ha); R11 POI-site capacity per setting class
(thinned at 150 m); G0's metrics where they apply; tier conformance per plateau; walls by served-slope class
(45–60°, 60–70°, > 70°) with steep-run heights, never called impassable; land use by distance band; lake-site
candidates.

*Added by A3 (approved 2026-10-23), both report-only, no flag:* **D-cap** — for each slope angle the construction
configures, the share of land triangles with served slope within ± 1° of it and its peak ratio against the flanking
bands, with the flank rule K5 (definition: §13.3); **D-reg** — slope bands (< 5°, 5–15°, 15–30°, 30–45°, > 45°) and the
median slope per region code (§13.3). They answer the design note's §4.4 risks "slopes clustering around the
configured maximum" and "similar slopes across different regions".

---

## 8. Q1–Q9 — operational definitions and flag policy

The Q1–Q9 of record are the post-G3c review §3.3; below are their operational forms. **Q values never gate**
(GEO-7). ~~Each Q gets a **review flag**: a candidate is flagged on a Q when its value is at least as bad as the
**G3c reference level** — the median of that Q over G3c's twenty evaluation lattices, measured in §11 — so a flag
reads "no better than the failed baseline on this failure mode".~~ *(Corrected 2026-10-22, K3 — §11.5.)* A Q gets a
**review flag** only where §11 showed that it separates the failure it names; the flag level is the median of that Q
over the G3c lattices that showed the failure (contour A for features of LMC's contour, contour B for B's cone), so a
flag reads "no better than the failed baseline on this failure mode". The flag levels are in §8.1. Flags draw the
reviewer's attention; they decide nothing. A Q without a flag is reported only.

| Q | Detects | Calculation | Worse is |
|---|---|---|---|
| Q1 | planar facets | over land vertices with vertex slope > 5°: share whose 15 × 15-vertex window (≈ 41 m) fits a least-squares plane with RMS residual < 0.05 m; reported for all land and per region | higher |
| Q2 | missing concavity | per river system with ≥ 10 channel vertices (A ≥ 30 000 m², priority-flood D8 on the lattice): least-squares slope of log S against log A over its channel vertices with S > 0; the median θ over systems | lower θ |
| Q3 | whale-back crests | crest vertices in H: for one of the four lattice directions `u`, `h(p) ≥ h(p ± u)` and `h(p) > max h(p ± 11u)` (30 m); per crest vertex, curvature `κ = [h(p + 11u) + h(p − 11u) − 2h(p)] / (30 m)²` and the **flat-crest width** = the run along `u` through `p` of vertices within 5 m of `h(p)`; medians over crest vertices | higher width, κ closer to 0 |
| Q4 | single or uniform crests | separate summits (G0 M3 (c)); coefficient of variation of the declared main divide's height sampled every 20 m; each saddle's depth below its lower culmination | fewer summits, lower CV |
| Q4m *(A2, approved 2026-10-23 as report-only)* | uniform crests on the **measured** crest | crest relief ratio over the middle 60 % of the main measured crest path (K4), crest peaks per km, crest length — definition §13.2 | lower ratio, fewer peaks |
| Q5 | radial cones | for each culmination with prominence ≥ 30 m: over land vertices 100–400 m from it, the slope-weighted mean of `cos` between the downslope direction (−∇h, vertex slope) and the outward radial; the value at the main summit | higher |
| Q6 | overlay plateaus | per plateau (§2.4): joined share and escarpment share of rim samples; coefficient of variation of edge drop over escarpment samples; re-entrants = channels (A ≥ 30 000 m²) leaving the top, counted as distinct exit groups | lower joined share, lower CV, fewer re-entrants |
| Q7 | over-continuous coasts | shore vertices classed by the R6 rule (highest land within 60 m: < 4 m, 4–15 m, ≥ 15 m above sea level); runs = 8-connected components of same-class shore vertices; **the cliff share of the shoreline** *(added 2026-10-22, §11.5)*; the longest cliff run as a share of all cliff shore vertices and of the shoreline; class changes per kilometre of shoreline (shore vertices × c as the length); the class of the run at each outlet and valley mouth | higher cliff share and longest-run share, fewer changes |
| Q8 | seed-invariant layouts | over the ten held-out lattices of one synthesis: median pairwise Pearson correlation of land heights after z-scoring each lattice; divide-tree branch counts and core positions | higher |
| Q9 | construction by accretion | named construction fixes in calibration, by kind | more |

### 8.1 Flag levels (approved 2026-10-22; Q4m and the whale-back rule added 2026-10-23)

**Whale-back (option (b), owner, 2026-10-23):** no metric detects it reliably on the evidence (Q3, E2; Q4m, E10), so
the whale-back is **judged in the owner's visual review** at the three scales; Q3, Q4 and Q4m are reported beside it
and never flagged. This is how the design note's §7.4 requirement to detect whale-back ridges is met.

| Q | Flag when | Level and its source |
|---|---|---|
| Q1 | planar share of land steeper than 5° **≥ 10.9 %** | G3c contour A median (uplands 23.5 %, highland ~0 %: separation confirmed, E8) |
| Q2 | — (report only) | G3c is not low on this measure (θ median 0.49, spread 0.09–1.25); correct on analytic profiles only |
| Q3 | — (report only) | E2 failed: cross-crest width does not separate A's whale-back from B (§11.4); the whale-back is judged in the review (option (b)) |
| Q4 | — (report only) | not checkable on G3c data (no recorded divide); reported in Step 1 |
| Q4m | — (report only) | E10 failed: 6 of 10, needed 7 (§13.5); option (b) |
| Q5 | radial index at the main summit **≥ 0.74** | G3c contour B median (B's cone: B above A on 10 of 10 seeds, E1); analytic cone 1.00, straight ridge 0.65 |
| Q6 | joined share **≤ 0.3 %** **and** no re-entrant | G3c, both contours (drums: escarpment above joined on 44 of 44, no re-entrant on 43 of 44, E3; K1 definition) — and LMC's layout asks for ≥ 15 % joint, so a measured share near 0 contradicts its own layout |
| Q7 | cliff share of the shoreline **≥ 44.6 %**, or longest cliff run **≥ 14.8 %** of the shoreline | G3c contour A medians; shares reproduce G3c's record exactly (E7); the single-run reading of "band" failed (E4), so the share carries the flag |
| Q8 | median pairwise correlation **≥ 0.846** | G3c contour A (no contrast in the data; the level marks "as seed-invariant as G3c") |
| Q9 | — (report only) | G3c needed 18 named fixes (research §14.3) — reported beside LMC's count |

---

## 9. Views, review record and freeze record

- **Views**: G1 V1–V6 through `GeometryFirstReview.Run` with seed 110201 and the candidate lattice (contour A's
  anchors) **[FACT]**; structure-anchored views as technical draft §7.1 — island overview, mountain-system oblique,
  each plateau rim from 400 m at 35°, the pass, two outlets, one shore run of each measured class, ground views at the
  capital and the pass; G1's presentation; identical camera records for both syntheses on a seed.
- **Review record** (note §8): per seed, camera and candidate (X or Y) — the D6 points assessed at each scale,
  accepted / rejected, a reason, artefacts and counterexamples; the X/Y key sealed in the freeze record.
- **Freeze record** (note §9 Step 3): the list there plus this specification's MD5, the layout validation results,
  and the Q flag levels.

## 10. File breakdown (note §10)

| File | Content |
|---|---|
| `Editor/TerrainGenLmcDump.cs` | batch entry, contour A fixture, protected-hash manifest, reports, freeze record |
| `Editor/TerrainGenLmcLayout.cs` | LM2 layout and its validation (§4) |
| `Editor/TerrainGenLmcFluvial.cs` | S-LM3 (§5) |
| `Editor/TerrainGenLmcSkeleton.cs` | S-LM1 (§6) |
| `Editor/TerrainGenLmcMeasure.cs` | gates, indicators, diagnostics, Q1–Q9 (§§7–8) |
| `Editor/TerrainGenLmcViews.cs` | structure-anchored views, contact sheets, review record templates (§9) |

Editor-only, namespace `GameDev.EditorTools`, referenced by nothing; G3c's files untouched (helpers copied); G0, G1 and
the hydrology package called through public entry points.

---

## 11. Verification on historical data

### 11.1 What can be checked without generation

| Check | Data | Kind |
|---|---|---|
| C1 gate mapping | G3c's report (research §14) | what G3c would score under §7.1 — from recorded values |
| C2 computability | G3c's twenty evaluation lattices and region maps | each new calculation runs and returns finite values |
| C3 definition fidelity | the same | the plateau edge height of §2.4 recovers G3c's drawn class heights; the summit of §2.2 equals G3c's recorded summit; Q7's classes reproduce G3c's recorded shore shares |
| C4 metric correctness | analytic surfaces | Q1, Q2, Q5 return known answers |
| C5 known-group discrimination | G3c, contour A against contour B | the Q diagnostics separate failure modes the owner's review saw |
| C6 reference levels | G3c's twenty lattices | the Q flag levels of §8 |

**Not checkable on historical data:** S-LM3 and S-LM1 themselves (no code exists); the pass offset (G3c declared no pass
and corridors); Q4's divide CV (G3c recorded no divide); R5's designated outlets beyond G3c's own record; R10 diagnosis
of G3c's failures (G3c did not record the failing nodes' positions); views. **No production `H4A` lattice is available**
(the Stage 3F session holding them was cleaned), and rebuilding one would be a generation run; production references
wait for Step 1.

### 11.2 Data and scripts

G3c evaluation lattices `g3c_{A,B}_s30-med_{seed}.f32` and `_region.u8` (codes 0 sea, 1 lowland, 2 upland, 3 highland,
4 plateau top, 5 wall, 6 ramp **[FACT, `G3cResult`]**), `g3c_report.txt`; read-only, from the session scratchpad
(`…/scratchpad/g3c/eval/`). Scripts and outputs: `…/scratchpad/lmc_spec/`. Python 3 with numpy only.

### 11.3 Expectations, written before any computation

- **M1 (C1, derived from the record).** Under LMC's R5, G3c contour A meets R5 on **2** seeds (7 and 777: genuine
  elevated systems; 256, 1001, 1002 and 1004 passed G3c's R5 vacuously; 424242, 20260905 and 1003 failed the planner),
  contour B on **7** (as in G3c — no B seed passed vacuously). R10: A fails on 3 seeds, B on 2.
- **E1 (Q5).** The main summit's radial index is higher on B than on A on ≥ 7 of 10 paired seeds (B: "a central peak
  with radial slopes").
- **E2 (Q3).** The median flat-crest width in H is larger on A than on B on ≥ 7 of 10 paired seeds (A: "whale-back").
- **E3 (Q6).** On ≥ 80 % of G3c plateaus the escarpment share of rim samples exceeds the joined share, and on ≥ 80 %
  there is no re-entrant ("drums").
- **E4 (Q7).** On ≥ 15 of 20 lattices one cliff run holds ≥ 50 % of all cliff shore vertices ("an over-continuous
  band").
- **E5 (C3).** The plateau edge height of §2.4 lies within ± 25 % of the plateau's recorded G3c height on ≥ 90 % of
  plateaus.
- **E6 (C3).** The §2.2 summit equals G3c's highest recorded highland summit within 0.5 m on 20 of 20 lattices.
- **E7 (C3).** Q7's low / rocky / cliff shares reproduce G3c's recorded shares within ± 3 points on ≥ 18 of 20.
- **E8 (C5).** Q1's planar share on uplands exceeds that on highland on ≥ 7 of 10 contour-A lattices (eikonal uplands
  rise at a constant slope; the highland is capped by a curved envelope).
- **E9 (C2, R4/R9).** All five recorded G3c sites lie in one T-edge component on ≥ 19 of 20 lattices (R8 held 19/20).
- **C4 answers.** Q1: a tilted plane → 100 %, a sphere cap of radius 300 m → 0 %; Q2: a synthetic profile built with
  θ = 0.45 → 0.45 ± 0.02; Q5: a cone → ≥ 0.99, a straight Gaussian ridge → markedly lower than the cone.
- **No expectation:** Q2 on G3c (its river floors rise at region-wise constant slopes, which are concave in steps), Q8
  (no contrast exists in the data), R9 detours (descriptive).

A failed expectation is reported as such. If it shows a **definition** cannot detect what it names, the definition is
corrected before approval and the correction is logged in §11.5 — G3c is the only labelled failure set, and no LMC
candidate exists yet. Thresholds of gates are not touched.

### 11.4 Results **[MEASURED]**

**Pre-registration.** §§1–11.3 were written and hashed before any computation: MD5 `3fd631d15ff79d951c4c2e4c909f06e1`
at 2026-10-22T18:12:12Z (copy: `…/scratchpad/lmc_spec/spec_as_preregistered.md`). §11.3 is unchanged since; the
definitions corrected afterwards are struck through in place (§§2.4, 7.1 R9, 8) and listed in §11.5. The 41 input
files (twenty lattices, twenty region maps, the report) have identical MD5 before and after.

| Item | Expectation | Result | Verdict |
|---|---|---|---|
| M1 | LMC R5: A on 2 (7, 777), B on 7; R10 fails A 3, B 2 | A 2 (7, 777); B 7 (424242, 7, 256, 777, 1000, 1001, 1003); R10 fails A 3, B 2 | as derived |
| E1 Q5 | B > A on ≥ 7 of 10 | **10 of 10**; main summit A 0.38–0.62, B 0.70–0.79 | confirmed |
| E2 Q3 | flat-crest width A > B on ≥ 7 of 10 | **0 of 10**: A 27–41 m, B 58–77 m; curvature A −0.022 … −0.028 m⁻¹, B −0.011 … −0.015 m⁻¹ — A's crests are narrower and sharper across | **failed** |
| E3 Q6 | escarpment > joined on ≥ 80 %; no re-entrant on ≥ 80 % | 44 of 44; 43 of 44; joined share median 0.000 (0.002 with K1), escarpment share 0.89 (G3c's own record: 84 %) | confirmed |
| E4 Q7 | one cliff run ≥ 50 % of cliff shore on ≥ 15 of 20 | **1 of 20**: the longest run holds 22–61 % (median 33 %) of cliff shore; cliff share of the shoreline 36–54 % | **failed** |
| E5 edge height | within ± 25 % of recorded on ≥ 90 % | **15 of 44**, all overstated (T2 class 20 → 32–47 m) | **failed → K1** |
| E6 summit | equal within 0.5 m on 20 of 20 | 20 of 20 (largest difference 0.43 m — the record rounds to metres) | confirmed |
| E7 Q7 classes | shares within 3 points on ≥ 18 of 20 | 20 of 20 (largest difference 0.5 point) | confirmed |
| E8 Q1 | uplands > highland on ≥ 7 of 10 (A) | 10 of 10: uplands 19–33 % planar, highland 0–0.8 % | confirmed |
| E9 R4/R9 | all sites in one T-edge component on ≥ 19 of 20 | 19 of 20; on B 777 one site lies in a component not reachable from the shore, as G3c's R8 failure there | confirmed |
| C4 Q1 | plane 100 %, sphere cap 0 % | 100 %, 0 % | correct |
| C4 Q2 | 0.45 ± 0.02 | 0.4500 on the synthetic valley (priority flood, D8, accumulation, fit) | correct |
| C4 Q5 | cone ≥ 0.99; ridge markedly lower | cone 1.000; straight Gaussian ridge 0.653 | correct |

**Descriptive (no expectation).** Q2 θ per lattice 0.09–1.25, median 0.49 — G3c's step-wise river floors do not read
as missing concavity. Q8 median pairwise correlation: contour A 0.846 (0.70–0.95), contour B 0.915. R9 on four lattices
(A and B × 424242, 777): 36 of 40 site pairs connected (the 4 others all on B 777); with the K2 metric, detours on
contour A median 1.25, maximum 2.87, on B at most 1.57; an alternative route survives on 33 of 36 pairs.

**Reference levels** (medians; contour A | all twenty): Q1 planar share 10.9 % | 11.8 %; Q1 uplands 23.5 % | 25.2 %;
Q2 θ 0.50 | 0.49; Q3 width 30.9 m | 49.5 m; Q3 curvature −0.026 | −0.019 m⁻¹; Q5 0.528 | 0.659 (contour B 0.742);
Q6 joined share 0.3 % | 0.2 %, edge-drop CV 0.22 | 0.24 (K1); Q7 cliff share 44.6 % | 44.4 %, longest cliff run
14.8 % | 15.1 % of the shoreline, class changes 2.8 | 3.0 per km; Q8 0.846 (A).

**Runtime.** About 20 s per lattice for every calculation above in single-threaded Python; the C# implementation is
expected to be faster.

### 11.5 Corrections found by the verification

| Id | What the verification showed | Correction | Check after the correction |
|---|---|---|---|
| **K1** | E5 failed: "the lowest non-top land within 90 m" also measured the fall of the ground beyond the foot (≈ 9 m on a 0.10 upland slope), overstating T2-class escarpments by 30–80 % | §2.4: the foot is found by a steepest-descent walk that stops where the slope flattens below 15° | on the same data (**post-hoc**, not independent): 38 of 44 within ± 25 % (tall class 10 of 10, T2 class 28 of 34); absolute error median 3.8 m, 90th percentile 8.5 m, signed median +2.6 m. Independent confirmation: the layout's declared class heights on LMC's calibration seeds in Step 1 |
| **K2** | the R9 detour over triangle centroids is direction-dependent on open flat ground: 1.22 along the axes, 1.05 along the south-west–north-east diagonal, 1.72 along the other | §7.1 R9: length on the 8-neighbour graph of walkable vertices over the octile distance; connectivity stays on T-edge | flat-ground control 1.000 in all four directions; G3c detours above |
| **K3** | one rule "flag at the G3c median" would flag ordinary ridges on Q5 (contour A's 0.53 is below a straight ridge's 0.65) and natural concavity on Q2 (G3c's θ ≈ 0.49) | §8: flags only where §11 confirmed separation, at the level of the failing group; §8.1 lists them | — (a policy; the levels are the measured medians) |
| Q7 | E4 failed: G3c's cliffs are several long runs, not one band | the cliff share of the shoreline is reported and carries the Q7 flag; the run measures stay | — |

**Failed expectations kept as findings, without a definition change:** E2 — Q3 measures how broad a crest is across;
it does not capture the "whale-back" read on contour A, which concerns how uniform the crest is along its length. Q3
stays report-only; that property belongs to Q4's crest-height variation along the declared divide, to be verified
in Step 1. E4 — see Q7 above.

**Untouched:** every gate threshold; the R4, R5, R7, R8 and R10 calculations; Q1, Q2, Q5, Q8 definitions.

**Files** (session scratchpad `…/scratchpad/lmc_spec/`, temporary): `lmcspec.py` (definitions), `analytic_tests.py`,
`run_eval.py`, `run_q8_r9.py`, `plateau_fix.py`, `r9_fix.py`, `summarize.py`; results `analytic_results.txt`,
`eval_results_{A,B}.jsonl`, `q8_r9_results.json`, `plateau_fix_results.txt`, `r9_fix_results.json`,
`reference_levels.txt`, `summary.txt`; hashes `spec_md5_before_computation.txt`, `inputs_md5_before.txt`,
`inputs_md5_after.txt`. They are reference implementations for checking the C# in Step 1, not project code.

---

## 12. Owner decisions on this specification

**Decided 2026-10-22 (owner):** items 1–4 below are **approved** as worded; item 5 — the Step 1 go-ahead — is **not
given**. How this maps onto the design note's §12: item 6 (metric specification) and item 7 (S-LM3 model contract)
are decided by items 3 and 2 here; items 4 (elevation targets) and 5 (plateau edge classes) are decided with §2, which
states them (summit 330–430 m world height, pass offset 40–80 m, classes 18–40 m and 60–120 m); items 1 (approving the
note), 2 (Step 1) and 3 (the name) remain open. *Later the same day:* the owner approved the note (v0.5) as the research
plan, kept Step 1 unauthorised and kept LMC as the working name (official ID before implementation). The readiness check
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) found two places where this specification needs an
owner-approved amendment before Step 1: R8 covers only *verified* plateau tops where the note asks for every designated
top (F1), and no report-only diagnostic covers slope clustering at a cap or per-region slope bands (F3).

1. Approve §2 — in particular the **world-height datum** (330–430 m world height = 306–406 m above the sea) and the
   corrected plateau edge height (K1).
2. Approve the S-LM3 model contract (§5) as a permitted **experimental** representation (design note §12 item 7).
3. Approve the gate calculations (§7.1, with K2 for R9's detour), the Q definitions (§8), and the **flag levels of §8.1**
   (K3) — the design note's §12 item 6.
4. Approve the layout validation (§4.3) and the S-LM1 ownership rule (§6).
5. Then, separately: the Step 1 go-ahead (design note §12 item 2).

---

## 13. Amendments A1–A3 — ~~not in force until the owner approves them~~ **approved 2026-10-23, written into v0.4**

Prepared on 2026-10-23 at the owner's request (Step 1 readiness conditions C2–C4). ~~The approved text of §§1–12 is
unchanged; if approved, these amendments are written into it as v0.4.~~ **The owner approved A1 and A3 and chose
option (b) for the whale-back** (A2's measure Q4m kept as report-only, unflagged). They are written into §7.1 (R8),
§7.3 (D-cap, D-reg), §8 (Q4m) and §8.1 (the whale-back rule). This section stays as the record of how they were
prepared and checked.

### 13.1 A1 — R8 covers every declared plateau top (F1, condition C2)

| Gate | Approved text | Proposed text |
|---|---|---|
| R8 | Vertex reachable (§1) at: each R4 site centre; **each verified plateau's** top vertex nearest its centroid; the declared pass col | Vertex reachable (§1) at: each R4 site centre; **the top vertex nearest the centroid of every plateau the layout declares**; the declared pass col |

*Why:* the design note's R8 asks for "designated plateau tops"; the approved wording lets a plateau that fails R2's
verification escape the reachability gate. Every declared plateau has at least one ramp (§4.1), so every top is meant
to be reachable.

### 13.2 A2 — Q4m, whale-back on the measured crest (F2 option (a), condition C3)

Q4 (crest variation along the **declared** divide) stays as written; Q4m reads the same property on the **measured**
crest, so it exists for any lattice, G3c's included.

- **Crest vertices**: Q3's detector restricted to the mountain region H (unit-scale maximum along one of the four
  lattice directions, above both points 30 m away along it).
- **Main crest**: crest vertices dilated by one vertex (3 × 3), 8-connected components; the component holding the
  main summit, or else the largest; its **crest path** is the component's geodesic diameter (two breadth-first sweeps,
  8-neighbour steps of c and c√2); heights are read on the lattice along the path and resampled every 20 m.
- **Q4m-a, crest relief ratio** = (90th − 10th percentile of crest-path height) ÷ (median crest-path height − the
  10th percentile height of H), *(K4)* ~~over the whole path~~ **over the middle 60 % of the path** (the first and last
  20 % excluded, so the fall of a ridge toward its ends does not count). Low = a crest that hardly rises or falls along
  its length compared with how high it stands — the whale-back reading. Dimensionless.
- **Q4m-b, crest peaks per km**: maxima of the resampled crest profile whose prominence along the profile is ≥ 15 m,
  per kilometre of crest path. Reported.
- **Q4m-c, crest length** (km). Reported.
- *Flag*: proposed only if the check of §13.4 confirms separation (the same rule as §8, K3).

### 13.3 A3 — two report-only slope diagnostics (F3, condition C4)

- **D-cap, slope clustering at a configured angle.** For each slope angle α a construction configures (S-LM3: its caps
  38° and 70°; S-LM1: its ramp, talus and wall angles; G3c, for reference: atan 0.035 = 2.0°, atan 0.10 = 5.7°,
  atan 0.25 = 14.0°, ramp 20°, talus 35°, atan 0.8 = 38.7°, atan 1.5 = 56.3°, wall 75°): over land triangles, the
  share whose served slope lies in [α − 1°, α + 1°], and its **peak ratio** — that share per degree divided by the mean
  share per degree of the two flanking bands [α − 4°, α − 2°] and [α + 2°, α + 4°]. *(K5)* A flank is **dropped** when,
  clipped to [0°, 90°], it is empty or overlaps another configured angle's band [β − 1°, β + 1°]; the ratio uses the
  remaining flank, and when none remains only the share is reported. A ratio well above 1 means slopes pile up at the
  configured angle.
- **D-reg, slope bands per region.** For each region code, the share of its land triangles (region of the triangle's
  south-west vertex) in < 5°, 5–15°, 15–30°, 30–45° and > 45°, and the median slope.
- Both report-only, no flag (GEO-7).

### 13.4 Checks on existing data, written before any computation

Same boundary as §11: read-only Python on existing G3c outputs and analytic surfaces; no generation, no Unity.

- **E10 (Q4m-a).** Contour A's crest relief ratio is lower than contour B's on ≥ 7 of 10 paired evaluation seeds —
  from the recorded reading that A carries "the same crescent-shaped, whale-backed ridge" and B "a central peak with
  radiating ridges" (research §14.8).
- **E11 (D-cap).** On G3c, the peak ratio at the upland angle 5.7° (atan 0.10, a constant slope of the eikonal
  construction) is ≥ 2 on ≥ 15 of 20 evaluation lattices.
- **C4 answers.** Q4m-a on a long ridge of constant crest height → ≤ 0.05; on a ridge whose crest carries three
  culminations of different heights → ≥ 0.3. D-cap on a cone of constant 30° slope → peak ratio at 30° ≥ 5; on a
  Gaussian hill → ≤ 2 at every angle tested.
- **No expectation:** Q4m-b (G3c records more separate summits on A than on B, so its direction is not predicted),
  D-reg (descriptive).
- **Calibration-grid references (F4, condition C5).** On G3c's four `s30-med` calibration grids (A and B × 110201,
  1337), every calculation of §§2, 7, 8 and 13 that runs on G3c data is computed and recorded with the grids' MD5, as
  the reference Step 1's C# must reproduce. Continuity with G3c's own record is expected as on the evaluation grids:
  summit within 0.5 m and shore-class shares within 1 point on 4 of 4 (E6, E7 held with margins 0.43 m and 0.5 point).

A failed expectation is reported as such; no definition is changed on these data without being logged and returned
to the owner.

### 13.5 Results **[MEASURED]**

**Pre-registration.** §§13.1–13.4 were hashed before any computation: MD5 `421fe9149674e5a558a51058e25c2ab3` at
2026-10-22T23:23:53Z (copy `…/scratchpad/lmc_c2c5/spec_as_preregistered.md`). The definitions changed afterwards (K4,
K5) are struck through in §§13.2–13.3 and listed below. All G3c inputs read — the twenty evaluation lattices with
their report, and the four calibration grids with theirs — have identical MD5 before and after.

| Item | Expectation | Result | Verdict |
|---|---|---|---|
| C4 Q4m-a | constant-crest ridge ≤ 0.05; three culminations ≥ 0.3 | first run **0.438** / 0.838 — the ridge's fall toward its ends counted; after **K4** (middle 60 %), **0.0005** / **0.582** | failed → K4, then correct (before any G3c data was read) |
| C4 D-cap | 30° cone ≥ 5; Gaussian hill ≤ 2 | cone 52 269 (its flanks are empty); hill at most 0.97 | correct |
| **E10** Q4m-a | A below B on ≥ 7 of 10 seeds | **6 of 10**; medians A 0.55, B 0.70; per seed A 0.37–0.88, B 0.44–0.84 | **failed** |
| E11 D-cap | ratio at 5.7° ≥ 2 on ≥ 15 of 20 | **0 of 20** (1.28–1.69): the left flank [1.7°, 3.7°] holds the lowland peak at 2.0° (ratio 3.1–4.5 there); after **K5** (that flank dropped), **20 of 20**, ratio 20.5–49.9, median 29.6 | **failed → K5**; the post-hoc check holds |
| continuity (C5) | summit within 0.5 m and shore shares within 1 point on 4 of 4 calibration grids | 4 of 4 (largest 0.48 m) and 4 of 4 (largest 0.6 point); all recorded sites in one T-edge component on 4 of 4, matching G3c's R8 = Y | as expected |

**Descriptive.** Q4m-b: A 0.68–1.97 crest peaks per km (median 1.45), B 0.68–1.59 (1.05) — A's crest carries *more*
undulations, as G3c's record of more separate summits on A says. Q4m-c: crest paths 0.8–2.9 km. D-reg, medians over the
twenty lattices: lowland median slope 2.0° (99 % below 5°), upland 5.7° (76 % in 5–15°), highland 37.5°, plateau tops
1.1°, walls 39.5°, ramps 21.2° (96 % in 15–30°) — each region's median sits on its configured G3c angle (atan 0.035,
atan 0.10, the 20° ramp), the signature of slopes set per region rather than by position in the drainage.

**What the checks mean for the amendments.**
- **A2 (whale-back, F2 option (a)) did not produce a verified detector.** Neither the cross-crest width (Q3, E2) nor
  the along-crest relief (Q4m-a, E10) separates contour A's "whale-back" from contour B reliably; Q4m-b runs the other
  way. The visual reading may describe A's smooth, faceted flanks — which Q1 does flag — more than its crest
  **[INTERPRETED]**. Recommended: **option (b)** — the whale-back is judged in the owner's review; Q4 and Q4m are
  reported, unflagged; no further tuning of a crest measure on G3c's data.
- **A3 (slope diagnostics) works with K5:** D-cap finds slopes piled at a configured angle (5.7° on all twenty
  lattices after K5; 2.0° visibly in D-reg), D-reg shows region-wise constant slopes. Both stay report-only.
- **A1** needs no data check: it is a wording change.

**Calibration-grid reference (C5)** — what Step 1's C# must reproduce on G3c's `s30-med` calibration grids (definitions
of §§2, 7, 8, 13 with K1, K2, K4, K5):

| Grid | Lattice MD5 | Summit m | Shore low / rocky / cliff % | Sites in one T-edge component | Plateau edge height m (K1) | Q6 joined share | Q6 re-entrants | Q1 | Q2 θ | Q3 width m | Q3 κ m⁻¹ | Q5 | Q4m a / b / c | Q7 longest cliff run / shoreline | R9 detour median / max (K2) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A 110201 | `842ed509101dd55f4a63bfd03af05696` | 271.88 | 34.7 / 16.4 / 48.9 | yes | 31.6, 31.9 | 0.000, 0.163 | 0, 0 | 0.116 | 0.163 | 30.9 | −0.0252 | 0.487 | 0.619 / 1.28 / 2.35 | 0.250 | 1.14 / 1.61 |
| A 1337 | `b69a565695ba2276801f40cf4d425411` | 261.47 | 31.6 / 22.6 / 45.8 | yes | 37.1, 33.0 | 0.008, 0.000 | 0, 0 | 0.102 | 0.500 | 27.1 | −0.0285 | 0.531 | 0.687 / 0.68 / 2.93 | 0.193 | 1.46 / 3.03 |
| B 110201 | `dc5b9d0ecb7035a888f948da390f9225` | 304.40 | 23.3 / 31.1 / 45.6 | yes | 65.8, 40.3 | 0.091, 0.108 | 0, 0 | 0.133 | 0.181 | 61.9 | −0.0156 | 0.643 | 0.569 / 1.64 / 2.43 | 0.169 | 1.28 / 1.73 |
| B 1337 | `f85b6f26ce70d276aba45304fdd95267` | 308.48 | 17.6 / 39.5 / 42.9 | yes | 18.3, 24.7, 25.7 | 0.003, 0.091, 0.007 | 0, 0, 0 | 0.141 | 0.665 | 49.2 | −0.0184 | 0.658 | 0.549 / 0.79 / 2.52 | 0.157 | 1.30 / 2.20 |

G3c's own record for the same grids (its screen report, `…/scratchpad/g3c/screen/g3c_report.txt`, MD5
`5188a6dd19fb44d2bad5f2168a0865e5`): summits 272, 261, 304, 308 m; shore shares 35/16/49,
31/23/46, 23/31/46, 17/40/43 %; R8 = Y on all four; plateau heights as built 24 and 34, 39 and 39, 73 and 34, 22, 23 and
20 m. Plateaus are matched by top area. The reference scripts and raw results are in `…/scratchpad/lmc_c2c5/` and
`…/scratchpad/lmc_spec/`; the scratchpad is temporary (readiness condition C9). *C9 decided 2026-10-23:* scripts,
results and the four calibration grids with their report are copied, MD5-verified, to
`D:\Hobby\game-dev\research-data\lmc-reference\` (`MANIFEST.md5`, `README.md`).

**Corrections:** **K4** (Q4m-a over the middle 60 % of the crest path) — found by an analytic check before G3c data
was read; **K5** (D-cap flank rule) — found by E11 on G3c data, checked post-hoc on the same data. Both belong to the
proposed amendments A2 and A3 ~~and are not in force until the owner approves them~~ — approved with them on
2026-10-23 (A2 as report-only).

---

## 14. Amendment M-1 — the pass col (approved by the owner 2026-10-23 as a specification rule)

**Why.** In Step 1 the literal reading of §2.3 ("the merge-tree saddle within 150 m") picked a noise col on a rough
surface, and the code substituted the vertex at the declared pass when no col qualified. Review
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) §§7.3, 10.1.

**Rule.**

1. **Flanking culminations.** The nearest declared culminations of the main divide on each side of the declared pass,
   by arc length.
2. **Culminations realised.** Each one's vertex — the highest mountain-region vertex within 150 m of its declared point
   (§2.2) — must be a peak of G0's merge tree with prominence ≥ 30 m (G0's summit prominence; a peak that never merges
   into higher ground has prominence = its height − 24 m, as in G0). Otherwise the reading is **`culmination not
   realised`**.
3. **Pass col.** c1 — the col at which the components of the two realised culminations first join in G0's merge tree
   (8-connectivity, descending height, ascending index).
4. **Relation to the declared pass.** The col counts as found only within 150 m of the declared pass position;
   otherwise **`not found`**, with its distance reported. **No other vertex is substituted.**
5. **Pass height.** The pass offset (§2.3) is measured only at a found col; otherwise the pass height is not found.
6. **R8.** When the col is not found or a culmination is not realised, the pass target is unmet and **R8 fails with that
   reason stated**; the pass target is never skipped.

**Status.** Approved as a rule of this specification; **not yet a verified implementation** — the full rule (items 2 and
6 included) is first checked on the preserved Step 1 lattices with read-only Python, covering the three outcomes
(culmination not realised; col not found within 150 m; col found) and confirming that R8 never skips the pass target.
The Step 1 code still carries the earlier reading and its substitution until a separately authorised change.

*Checked 2026-10-23:* the full rule ran, read-only, on the twelve preserved Step 1 lattices and three synthetic cases
(review §10.4): all pre-registered expectations held, all three outcomes occur, and R8 never passes with the pass target
unmet. The code change implementing it still needs a separate authorisation.

*Implemented 2026-10-23 (run 7, review §14):* the Step 1 measurement code follows this rule; the run left every lattice
bit-equal to the previous run, and its pass readings equal the Python computation of the rule at full precision when both
use the exact declared positions. The earlier substitution is gone.

*Accepted by the owner with caveats (2026-10-23, review §15.1):* the implementation is accepted on the grounds of run 7.
**The 150 m radius stays**: the sensitivity at the window's edge (review §14.4) is a known property of the rule, and on
the data in hand it changed no verdict.
