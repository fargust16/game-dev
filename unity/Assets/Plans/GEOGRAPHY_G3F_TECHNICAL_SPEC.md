# G3f — Technical Specification (WP1)

Version: 0.2

Status: Draft — **written under the owner's G3f mission authorisation (2026-10-25,
`research-data/g3f/gates/mission_authorization_2026-10-25.md`), not approved at gate G-3.** It fixes everything the G3f
runs depend on — the plan model, the stress fixture, the validator, the boundary contracts, the three surface laws, the
gates and diagnostics, the views and review form, the protocol, the pass and decision rules — and records how it was
developed. Criteria marked **[NEW]** are applied as pre-registered research criteria under the mission; they are not
approved criteria until the owner says so (phase plan §9.8). Nothing here touches production.

Last Updated: 2026-10-28 (v0.2: calibration run 1 recorded (§16.1); named fixes F1 and F2 registered before run 2 (§16.2) —
§4.1 GF-F6 and §7.1 step 1 changed accordingly. v0.1, 2026-10-27: written after WP0 and the development of the research
package; before any registered calibration run)

Depends On:
[GEOGRAPHY_G3F_PHASE_PLAN.md](GEOGRAPHY_G3F_PHASE_PLAN.md) v0.3 (its decision record governs),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (§§1, 2, 7, 8, 14 — carried definitions),
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md) (§13),
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) (§6, the 95 % reachability figure),
[AGENTS.md](../../../AGENTS.md)

Referenced By:
[GEOGRAPHY_G3F_PHASE_PLAN.md](GEOGRAPHY_G3F_PHASE_PLAN.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[99 Open Questions](../../../99%20Open%20Questions.md) (GEO-10), [AGENTS.md](../../../AGENTS.md)

---

## 0. How to read this

**Tags** as in the phase plan §0: **[FACT]**, **[MEASURED]**, **[CARRIED]** (unchanged from an approved G3c/G3e
definition), **[AUTHORING]** (an authoring default in world units, chosen by the agent, labelled so), **[NEW]** (a G3f
criterion applied under the mission, awaiting the owner), **[PROPOSAL]**.

**Labels (D-6 (g)).** Every G3f label carries the prefix **GF-**, so none collides with S8, B1/B2/C, O1, A1, K1–K5,
P1–P8, D1–D9 or C0 of earlier work:

| Phase plan name | G3f label | What |
|---|---|---|
| C0, U-F, R-F | **GF-C0, GF-UF, GF-RF** | the control and the two candidate laws |
| P1–P8 | **GF-V1 … GF-V8** | plan validator |
| F1–F7 | **GF-F1 … GF-F7** | fixture elements |
| R4ʳ, R5ʳ, R7, R8ʳ, R9ʳ, R10, reach share | **GF-R4, GF-R5, GF-R7, GF-R8, GF-R9, GF-R10, GF-RS** | hard gates |
| O1, B1 (flow), A1, determinism, protection | **GF-O, GF-B, GF-A, GF-D, GF-P** | process gates |
| B1 (targets) | **GF-T** | target reading (diagnostic, D-6 (b)) |
| S1–S8 (stops) | **GF-S1 … GF-S8** | stop conditions |
| WP0 C0-1 … C0-5 | WP0 checks (historical names kept in WP0's records) | read-only checks |
| analytic checks | **GF-AC1 … GF-AC9** | §14 |

**Where things are.** Code: `tools/g3f/` (Python 3.13 + numpy 2.5, the research package) and
`unity/Assets/Scripts/Editor/TerrainGenG3fDump.cs` (the Unity steps). Data: `D:\Hobby\game-dev\research-data\g3f\`
(never in Git). This document: the definitions.

---

## 1. Conventions

Carried from G3e spec §1 unchanged **[CARRIED]**: lattice 1 025 × 1 025 vertices, cell `c` = 2.734375 m, index
`i = z · 1025 + x`, world position `(x c, z c)`, +x east, +z north; heights float32 world height; sea level 24 m; land
> 24 m; land written at ≥ 24.5 m (`BASE`); triangles T1 = (00, 10, 11), T2 = (00, 11, 01), walkable < 45°; T-edge graph,
shore-reachable and vertex-reachable as G3e §1; vertex slope by central differences (`IslandReliefMetrics.SlopeDegrees`);
buildable ≤ 6.84°.

G3f additions:

| Item | Definition |
|---|---|
| Contour A | `LmcContour.A()` (Stage 1 hash `7599879a71250a06` **[FACT]**); exported by the Unity `contour` step to `research-data/g3f/contour/` (land mask, Stage 1 heights) and checked equal, vertex by vertex, to run 11's land mask and sea heights (§14.3) |
| Ocean | the hydrology package's mask: ≤ 24 m and 8-connected to the world border through ≤ 24 m (`HydrologyGrid.BuildOceanMask`) **[FACT]** |
| Sector | WP0 C0-3: the 8-connected land component containing vertex (300, 700) after removing the 4-connected raster of the segment `P_s` (516, 441) → `P_n` (600, 887); 1.010 km², shoreline 3 289 m, north-west orientation **[MEASURED]** |
| Window | 513 × 513 vertices from (111, 420) (WP0 C0-3) — the frame of S-PLAN and of the plan images |
| Domain | the sector plus its boundary line (the Sector crest); everything a candidate writes |
| Context | contour A's land outside the domain; synthesised once per variant by GF-C0 on coarse units (§4.4) and embedded identically for every candidate |
| Distances | Euclidean unless named geodesic (8-neighbour Dijkstra, steps c and c√2) |
| Randomness | counter-based streams: `u_k = splitmix64(key ⊕ splitmix64(k)) / 2^64`, `key` = first 8 bytes of SHA-256 of `name:seed:attempt`; streams `research.g3f.plan` (variant draws), `research.g3f.synth.tree` (tree tie-breaks), `research.g3f.views` (blind order). The research package is Python, so `System.Random` / `WorldSeed.Fork` are not used; production code would use them (G4) |

---

## 2. Inputs, outputs and owners

| Item | Owner and representation | Written by | Read by |
|---|---|---|---|
| Contour A land, Stage 1 sea heights | Stage 1 (frozen) | Unity `contour` step (export only) | plan builder |
| Geography Plan (vectors, variant draws, targets, raster hashes) | the plan builder; canonical JSON `plan.json`, SHA-256 `plan_hash.txt` | `g3f_run.py variant` | everything below |
| Plan raster: unit id, crest record id, region, root, zone, plateau id, axes, exempt | the plan | plan builder | synthesis, readings |
| Unit vertex heights | **its unit** (one writer) | synthesis | readings |
| Crest-line vertex heights | **its boundary record** (Divide, Sector or Fringe crest) | synthesis | readings |
| Context heights, sea heights | the context run / Stage 1 | context synthesis | embedded as fixed input; never written by a candidate |
| Lattice | the assembly of the above | synthesis | readings, Unity steps |
| River network | derived (production `HydrologyPlanner`) | Unity `hydro` step | GF-R5, GF-R10, GF-R7 (b) |
| Readings, views, sheets, key | derived | readings, Unity `views` step, sheet composer | the report, the owner |

A derived reading that disagrees with the plan is a candidate defect, never a reason to re-plan (phase plan §4.3).

---

## 3. The Geography Plan

A plan is the fixture (§4) instantiated for one variant (seed, plateau class, attempt): vector data in world metres,
the variant's draws, the targets, and a raster of one unit or crest record, one region and one zone per vertex.

**Units.** Every domain land vertex belongs to exactly one owner:

- a **catchment** — a declared drainage unit with exactly one designated outlet run (shore vertices within 30 m of the
  outlet point); its roots are the run's vertices; nothing else of it touches the sea;
- a **coastal fringe** — the coastal band of one stretch of coast between catchment outlets and divide ends; its roots
  are its shore vertices; its parent is the catchment it borders most (A1, §9.3);
- a **crest record** — the 4-connected raster of a declared divide (D1, D2), of the sector boundary (S), or the inner
  edge of the coastal band (the fringe crest F). A 4-connected line separates 8-connected regions, so no D8 step can pass
  from one unit to another without stopping on a crest vertex.

**Regions** (G3f's own enum): sea, lowland, upland, mountain, plateau top, plateau band, ramp, valley corridor, coast
low, coast rocky, coast cliff. Regions only parameterise the laws; they never write heights.

**Zones** (plateau write footprint): top, escarpment band, rim channel, ramp, ramp side; nothing else may be written by
the plateau sub-law, and nothing outside the footprint drains into it.

**Canonical record and hash (GF-V8).** `plan.json` = the vectors, the variant and its draws, the targets, the crest
clamps, axes, routes, reserve, plateau record, units and crest names, and the SHA-256 of every raster array; the plan
hash is the SHA-256 of that JSON (sorted keys, no whitespace).

---

## 4. The stress fixture (GF-F1 … GF-F7)

Authored by the agent from the G3e record (D-5), data in `tools/g3f/fixture/sector_nw.json`, instantiated by
`g3f/plan.py`. The topology is fixed; the variant draws are in §4.3.

### 4.1 Elements

| Id | Element in this fixture | Stresses (WP0 C0-2) |
|---|---|---|
| GF-F1 | main divide **D1** from the main culmination **M1** (on the sector boundary at z ≈ 1 870 m) west through the declared pass **S1** (1 170, 1 845) and the secondary culmination **M2** (950, 1 815) to the west coast (E1); M1–M2 590 m | single dome, whale-back; the M-1 col (X12) |
| GF-F2 | branch ridge **D2** from M1 north to the cliff headland **H** on the north coast; it divides units **A** (west) and **B** (east) | junction frames (X7), caused coasts |
| GF-F3 | the declared pass is **S1 on D1**, with route **R1** from the settlement reserve in lowland A over S1 to lowland C (1 060, 1 560) | pass corridor over a plateau (X3), M-1 |
| GF-F4 | unit **A**: trunk to **O1** (north bay) with tributaries A_t1, A_t2, the rim stream A_t3 and (seeded) A_t4; unit **B**: valley B_v to **O2**; unit **C**: trunk to **O3** (south bay head) with C_t1, C_t2 | converging valleys, climbs toward mouths (X6), R5 (X5) |
| GF-F5 | one plateau in unit A on D1's north flank near M2 (three slots), joint toward D1, escarpment elsewhere, rim outlet 1 toward A_t3, a seeded second rim outlet, and a **ramp chosen by the plan builder** (§4.2) landing in lowland A; its west side faces the west coast | write footprints (X2), ramp to cliff (X1), drums |
| GF-F6 | lowland A with the **settlement reserve** (centre (1 135, 2 242) m — moved by fix F2, §16.2, from (1 210, 2 150) m — radius 170 m, capital core 150 m) near O1; lowland C at O3; a lowland disc at O2 | settlement capacity beside a mountain foot |
| GF-F7 | coastal band with intents low (outlets ± 70 m, lowland hinterland), rocky (upland hinterland, E1), cliff (mountain hinterland, the headland H); band widths seeded per intent; the mountain foot within 200 m of the shore near P_s and H | coastal capture (X5), cliff bands |

**Deviations from the phase plan §6.3, decided while authoring (before any validator run is registered):**
(1) the main divide does not run *along* the sector boundary: WP0 C0-3 showed the boundary's depth profile cannot hold
two culminations 450 m apart; D1 runs west from M1 across the sector's deep band, as G3e's 110201 main divide did;
(2) the declared pass is on the main divide (S1), not on the branch: a 40–80 m offset pass on a branch that starts at a
~300 m summit needs ≥ 400 m of branch at the walkability bound, which the sector does not have; (3) the plateau is on the
main divide's flank, not the branch's: the space between the branch, the trunk and the reserve cannot hold a
tall-class footprint and its ramp. Each keeps the situation the element stresses.

### 4.2 Plan builder rules [AUTHORING unless marked]

- **Nodes.** M1 = the boundary-line vertex nearest z = 1 870 m; E1, H, O1–O3 snapped to the nearest sector shore
  vertex; outlets shifted along the coast by the drawn shift (± 60 m, phase plan §6.3).
- **Provisional regions** (sector, before the fringe): mountain within 200 m of D1 or S or 100 m of D2; lowland at
  ≥ 260 m from D1 and S and ≥ 140 m from D2, plus the reserve disc and the O2 lowland disc (110 m); upland elsewhere.
- **Coastal intent** per shore vertex: the hinterland region probed 140–160 m inland (mountain → cliff, upland →
  rocky, lowland → low); then E1 ± 60 m rocky, H ± 60 m cliff, and every outlet ± 70 m low (outlets win).
- **Coastal band**: land within the band width of its nearest shore vertex's intent; the **mouth** of each outlet (a
  strip as wide as the run across the band along the coast's inward normal) is removed from it; interior pockets with no
  unit seed join the band; band pieces cut off from the sea by crest lines join the fringe crest record.
- **Fringe crest** = band vertices 8-adjacent to interior land (the band's inner edge).
- **Corridor**: mountain or upland vertices within 20 m of a declared axis.
- **Axes**: polylines; a tributary ends at the nearest point of the axis it joins; an axis ending at an outlet enters
  the mouth along the inward normal (points nearer the outlet than the mouth's inner end are dropped).
- **Plateau** (L-5): top disc of the drawn area at the drawn slot; joint arc of the drawn width centred on the bearing
  to D1's nearest vertex; escarpment band of width `edge / tan 65° + 10 m` (phase plan §6.2) outside the joint; rim
  channel 1 (3 vertices wide) toward A_t3's target; **ramp**: for each start bearing in 15° steps outside rim outlet 1
  (± 40°) and the joint (± half + 10°), an 8-neighbour shortest path over passable ground (unit A, not crest, not fringe,
  not the reserve, ≥ 30 m + 45 m from axes, ≥ 20 m + 45 m from route R1, not within 2 vertices of rim channel 1) from
  that rim vertex to a lowland landing ≥ 50 m from the sea and ≥ 100 m from cliff-intent shore at path length in
  `[L_min, L_min + 80 m]`, `L_min = 1.15 · edge / tan 18°` (phase plan §6.2); the landing nearest the reserve centre
  wins (ties: bearing, index); the ramp is the 40 m strip **[CARRIED width]** along the path, with 25 m ramp-side bands
  (open at the landing end); rim channel 2 (if drawn) at the escarpment bearing farthest from rim channel 1 and the ramp
  start; pockets of unit A enclosed by the footprint join it.
- **Targets** (phase plan §5.1 **[CARRIED]** ranges): M1 drawn in 330–430 m; M2 = M1 × [0.62, 0.82]; the saddle
  fraction × [0.55, 0.72]; the pass offset 40–80 m. **Feasibility clamp [AUTHORING]**: a culmination's synthesis target
  is min(drawn, `BASE + 0.45 · D_out`), `D_out` = its geodesic distance through the domain to the nearest outlet run;
  the S1 target = min(35 m + offset, saddle fraction × min(M1, M2)), 35 m being the expected lowland level. Drawn and
  clamped values are both recorded; GF-T flags realised values outside the approved ranges (D-6 (b)). *Why:* at the
  walkability bound (§6.5) the sector cannot hold a 330–430 m summit together with lowlands, valleys and low coasts —
  development showed GF-UF saturating at ~195 m and GF-C0 inflating its far interior to ~600 m when asked for the drawn
  value (§15).
- **Planned crest profiles** (GF-RF, and the context's Sector crest): piecewise linear in arc length through the
  node targets — S: shore end `BASE + 1` → M1 → shore end; D1: M1 → S1 → M2 → E1's shore `BASE + 1`; D2: M1 → the
  headland top (drawn 60–90 m) 80 m before the shore → `BASE + 1` — then the 1-D lower Lipschitz envelope at the flank
  slope (only lowers), so a planned crest is walkable along its own line.

### 4.3 Seeded variant parameters (stream `research.g3f.plan`)

M1, M2 factor, saddle fraction, pass offset; edge height in the class range (short 18–40 m, tall 60–120 m
**[CARRIED]**); top area 0.030–0.045 km² (inside the carried 0.03–0.15); slot (one of three); joint width 60–100°;
rim outlets 1 or 2; tributary A_t4 yes/no; outlet shifts ± 60 m; band widths low 30–50, rocky 40–70, cliff 50–90 m;
headland top 60–90 m. The plateau class is given, not drawn (calibration: both classes; held-out: by position, §11.3).

### 4.4 Context (D-5)

Coarse whole-island units for GF-C0's context: divides K0 (M1 east and south along the island's spine to the south bay
head) and K1 (from K0 north-east to the north coast), the Sector crest (planned) — three catchments KN, KE, KW whose
every shore vertex is a root. Regions: mountain within 300 m of a context crest, lowland within 120 m of the sea,
upland elsewhere. The context is GF-C0 with its law bisected to M1's target and capped by the cone
`M1 − 2 m − 0.02 · distance to M1` **[AUTHORING]**, so no context ground rises above the main culmination (otherwise M1
is not a merge-tree peak and M-1 fails for reasons outside the sector).

---

## 5. (reserved)

## 6. The validator and the boundary contracts

### 6.1 Validator GF-V1 … GF-V8

A plan failing any check is not synthesised; a seeded variant whose plan fails is redrawn (attempt k + 1 of the same
stream) up to **10 attempts** (`research-data/g3f/gates/limits_before_wp1.md`, fixed before WP1); past the cap the
variant is *plan invalid* and fails for every candidate, denominator unchanged (D-6 (e)).

| Check | Rule |
|---|---|
| GF-V1 coverage | every domain vertex has exactly one owner (unit or crest record) and a region; no unassigned component |
| GF-V2 mask | every sector vertex is contour A land |
| GF-V3 topology | every catchment's roots form one run, all within 60 m of its outlet point; every fringe has roots; the tree (§7.1) reaches every unit vertex |
| GF-V4 footprints | the plateau footprint lies in unit A and meets no axis buffer (30 m; the plateau's own rim stream excepted), the reserve, route R1's buffer (20 m), a crest line or a fringe; the ramp exists, its length ≥ `L_min`, its landing in lowland open ground; every rim channel ends on open ground of unit A |
| GF-V5 features | M1–M2 ≥ 450 m **[CARRIED, V-L7]**; M1, S1, M2 on crest lines (S1 and M2 on D1); every axis inside its unit, ending at its outlet run or the axis it joins |
| GF-V6 budgets | plateau top 0.03–0.15 km² **[CARRIED]**; joint ≥ 15 % of the rim **[CARRIED]**; non-joint rim 20–85 % (the carried 20–70 % escarpment counts ramp and rim outlets separately) |
| GF-V7 coast | an intent for every sector shore vertex; outlet runs low; band widths in their ranges |
| GF-V8 determinism | the plan hash identical in two fresh processes (checked with GF-D) |

### 6.2 Boundary contracts

| Contract | Height | Flow |
|---|---|---|
| **Divide** (D1, D2) | GF-C0, GF-UF: **emergent** — the highest adjacent non-crest land vertex of the final surface + δ (δ = 0.5 m); GF-RF: **planned** (§4.2) | no unit vertex drains onto it or across it (GF-B) |
| **Sector** (S) | planned in GF-RF and in the context; emergent in GF-C0 and GF-UF (the context side counts among its neighbours) | as Divide; nothing drains out of the domain into context land |
| **Fringe crest** (F) | emergent in every candidate, floored at 24 m + 1 / 6 / 20 m (low / rocky / cliff) | as Divide |
| **Shore** | roots at `BASE` | to the sea |

A crest vertex that has no non-crest land neighbour takes the highest adjacent crest value + δ (no pit).

---

## 7. The surface synthesis (L-1 … L-7)

### 7.1 Shared steps (every candidate)

1. **Tree (L-1)** — built once from the plan, never from heights: a Dijkstra from each unit's roots over its own
   vertices, 8 neighbours, edge cost `length · (w_u + w_v) / 2`, ties by (priority, stateless hash, index); inside a
   catchment an edge whose flow climbs the **plan guide** `G` — the 35° cone of the planned crest profiles,
   `max_q [c(q) − D · tan 35°]`, read from the plan only — adds `2 × climb` (m) to the search priority (fix F1,
   §16.2; the recorded cost, which GF-C0 reads, stays the weighted length along the tree). Weight per
   metre **[AUTHORING]**: lowland 0.005, corridor 0.01, upland 0.10, mountain 0.45, plateau top 0.01, band 1.0, ramp
   0.25; × 0.5 within 4.1 m of a declared axis, otherwise × (1 + min(D_axis, 300 m) / 300 m); fringe units uniform.
   Declared axes are **forced tree paths**: an axis vertex can only receive from its downstream axis neighbour. Zone
   rules: open ground drains only to open ground; the top only to the top or a rim channel; band, ramp, ramp side and
   rim channel drain outward.
2. **Area** `A_i = c² (1 + Σ_{donors} A_j / c²)`.
3. **Interior law** (§7.2, candidate-specific) on catchment vertices (top, band and rim channel excepted).
4. **Plateau sub-law (L-5)**: `E_top` = median height of the ground ring outside the band + the edge target; each rim
   channel climbs from its foot to `E_top` over its length; the top is graded from the channel heads at 0.8°; the band
   is `foot + (top edge − foot) · t²`, `t = D_out / (D_out + D_top)`; the ramp descends linearly from the top edge at
   grade `min(tan 20°, drop / length)` but never below the law's provisional ground under it (and never above
   `E_top`); ramp sides are linear between ground and ramp; then every footprint tree edge rises ≥ 1 mm/m.
5. **Crest heights, catchments**: provisional crest values (planned or emergent from the law).
6. **Flank fill (S7a)**: catchment open ground is raised to the crest cone `max_q [c(q) − D · tan 35°]` — within
   30 m of an emergent crest, without limit for a planned crest — capped next to a planned crest at `c − δ`; then every
   catchment tree edge rises ≥ 1 mm/m (donors raised).
7. **Planned ceiling (S7b)**: unit vertices next to a planned crest lie ≥ δ below it (never below `BASE + 5.5 mm`); the
   tree path below each lowered vertex is carved to keep falling.
8. **Lateral constraint (L-3) and axis carve**: `h(v) ← min_w [h(w) + d · tan 35°]` over same-unit neighbours outside
   exempt vertices (escarpment band, rim channel, ramp sides, cliff fringe), by one Dijkstra on the lower envelope,
   alternated with a carve that makes every declared axis fall along its read sequence, until neither changes anything
   (≤ 20 rounds, both only lower; convergence logged).
9. **Coast (L-6)**: fringe crests from the final catchments; each fringe vertex `BASE + (c − BASE) · u(t)`,
   `t = a / (a + max(0, D_crest − c))` (`a` its tree distance to the shore), `u` = t^1.5 (low), t (rocky),
   1 − (1 − t)³ (cliff); repair; the planned ceiling for every planned crest; the lateral constraint on fringes.
10. **Final crests** (emergent ones recomputed on the final surface) and **assembly (L-7)**: each owner's vertices are
    written once; the write log records count and writer per vertex.

### 7.2 The three laws

- **GF-C0 (control, no law)**: `h = BASE + κ · cost` (the tree's cost distance); κ by bisection (§7.3) to the main
  culmination.
- **GF-UF (unified fluvial)**: `h = h_r + d · min(cap, k · f_region · g(A))`, `g(A) = A^(−θ)` for `A ≥ A_c`,
  `A_c^(−θ) (A / A_c)` below (the S-LM3 family's form, G3e spec §5.2, new code), θ = 0.45, `A_c` = 2 000 m²,
  k = 100, cap tan 35° **[AUTHORING]**; region factors lowland 0.004, corridor 0.01, upland 0.08, mountain 1.0, ramp
  0.08 **[AUTHORING]** (hillslope maxima ≈ 1.3 %, 3 %, 26 % and the cap). Mountain, upland and corridor factors are
  scaled by **one global multiplier** bisected to M1, then by **culmination factors**
  `1 + Σ a_c · max(0, 1 − D / R_c)` bisected in turn to M2 (R 300 m, reading radius 150 m) and to the pass S1
  (R 100 m, reading radius 30 m; the factor may fall to 0.05). Crests emergent; the whole sector is one solve.
- **GF-RF (crests set by the plan, synthesis per unit)**: the same law and parameters, applied **per catchment**: each
  catchment's multiplier is bisected to the highest plan node it adjoins (within 4 vertices), then culmination factors
  to its other adjoining nodes, each reading **only that unit's vertices**; Divide and Sector crests are planned;
  nothing in one unit's synthesis reads another unit (A1 by construction, measured in GF-A).

### 7.3 Targets (L-4)

Bisection on the law's own heights: `≤ 12` steps, doubling until bracketed then halving, tolerance ± 2 m
**[CARRIED]**; the best step is kept (ties: the smaller scale); unmet targets are logged, never forced. The realised
values are readings (GF-T), flagged outside the approved ranges (D-6 (b)).

### 7.4 What is guaranteed by construction and what is only checked

Guaranteed (proved in the code's docstrings, checked by GF-AC): every unit vertex keeps a strictly lower neighbour in
its unit after every step, so no pit and no closed depression arises inside a unit; no unit vertex can drain onto a
crest vertex or into another unit; declared axes fall; each owner writes once; the lateral bound holds between
same-unit non-exempt neighbours. **Not guaranteed** (measured): steepness across crest lines and coastal bands,
walkability elsewhere, reachability, the planner's network, culmination realisation (M-1), form.

### 7.5 Rule values

All values above are in `Params` (`g3f/synth.py`) and `A` (`g3f/plan.py`), written with every run to `params.json`.
A change to any of them after the freeze is a new configuration.

---

## 8. (reserved)

## 9. Readings

### 9.1 Hard gates (sector readings) [NEW as regional variants; calculations CARRIED where named]

| Gate | Calculation | Pass |
|---|---|---|
| **GF-R4** settlement | the carried site rule (G3e `Valid`: stride-2 centres, disc without sea, ≥ 90 % buildable at ≤ 6.84°, centre distance to non-land ≥ r, lowest mean slope first) — a capital (core 150 m) whose centre lies in the reserve disc, and a coastal village (60 m, centre ≤ 160 m from non-land) in the domain, ≥ 450 m from the capital | both exist and both centres are vertex-reachable |
| **GF-R5** drainage | on the production planner's network over the embedded lattice (Unity `hydro` step): systems = reaches draining to one ocean termination; *elevated* when a source node lies in the domain's mountain region, on the plateau top or in a corridor within 200 m of D1/S; designated when the termination is within 150 m **[CARRIED]** of O1, O2 or O3 | ≥ 1 elevated system, and all designated |
| **GF-R7** hygiene | (a) S8 depressions whose deepest vertex is in the domain — `DepressionFill` replica (float32, ε 1 mm, insertion-order ties), 4-connected components of depth > 0.05 m, S8 when deepest ≥ 0.25 m and area ≥ 100 m² **[CARRIED]**; (b) planner lakes whose spill point lies in the domain; (c) every declared axis read **as a line** (polyline resampled every ½ cell, nearest vertex, repeats dropped), climb = `max_k [h(n_k) − min_{j<k} h(n_j)]` | (a) 0, (b) 0, (c) every climb ≤ 0.25 m |
| **GF-R8** reachability | vertex-reachable: the capital centre, the plateau top vertex nearest its centroid, the ramp landing, and the pass col by **M-1 [CARRIED]** (flanks M1 and M2; realised = highest mountain vertex within 150 m, a merge-tree peak with prominence ≥ 30 m; the col joining them found within 150 m of S1, otherwise *not found*) | all reachable; a missing col or an unrealised culmination fails with its reason |
| **GF-R9** routes | R1 (reserve centre → over S1 → lowland C) and R2 (reserve centre → ramp landing → plateau top): endpoints in one T-edge component | both connected |
| **GF-R10** planner | `HydrologyPlanner.Plan(grid, HydrologySettings.Default, seed)` on the embedded lattice | `Succeeded` |
| **GF-RS** reach share (D-6 (c)) | share of domain land vertices outside declared bands (exempt) that are vertex-reachable | ≥ 0.95 |

### 9.2 Process gates

| Gate | Reading | Pass |
|---|---|---|
| **GF-O** single writer | the write log | every domain owner vertex written exactly once by its owner; nothing else written |
| **GF-B** flow (D-6 (a)) | D8 steepest descent on the final, unfilled surface (ties: lower index) | 0 unit vertices draining onto a crest, into another unit, or out of the domain onto context land |
| **GF-A** assembly | (a) GF-RF's units in reverse order; GF-UF's unit ids reversed; (b) unit A's law factor × 1.10 | (a) byte-identical lattice; (b) no change outside unit A, its fringe children and the fringe-crest vertices within 2 vertices of A. Pass-relevant for GF-RF; measured on GF-UF (D-4) |
| **GF-D** determinism | each step twice in fresh processes | plans, lattices and readings byte-identical (timing logs excepted) |
| **GF-P** protection | the 85-file protected manifest (`research-data/g3f/protected_md5_before.txt`: G3e's 73 + the 12 `TerrainGenLmc*` files) before and after every Unity process; contour A's and `H4A` 110201's pinned hashes in every Unity process | unchanged |

### 9.3 GF-T — target reading (diagnostic, D-6 (b))

M1 and M2 realised heights against their targets (drawn and clamped), the pass offset at the M-1 col against the higher
of the median lowland heights of units A and C; flags: M1 outside 330–430 m, offset outside 40–80 m, plateau K1 edge
more than 25 % of the class range outside it (G3e spec §2.5).

### 9.4 Diagnostics (report-only)

Undeclared walls (domain land triangles ≥ 45° with no vertex in a declared band, by 45–60 / 60–70 / > 70°); lateral
steps (same-unit 4-neighbour pairs, neither the other's D8 receiver, |Δh| > c · tan 45°, outside bands); D-reg (slope
bands and median per region); D-cap at 0.8°, 20°, 35°, 38° (K5 flank rule simplified: both flanks used where they
exist); slopes (share ≥ 45°, max, median); coast (realised class per shore vertex by the R6 rule — highest land within
60 m: < 4, 4–15, ≥ 15 m above the sea — and agreement with the intent); plateau (top relief, top median, ramp triangle
maximum and 95th percentile, K1 edge **[CARRIED]** and its class flag); crest CV along D1 and D2; buildable area in
the reserve; S8 count over the whole world lattice; timings.

### 9.5 Continuity of the harness (phase plan WP3)

On run 11's preserved S-LM1 lattices restricted to the sector, the harness reproduces WP0 C0-5's targets: S8 within
3 m, 0.01 m depth and one cell of area; axis climbs within 0.01 m (G3e's node reading); reachability of the sites, the
plateau tops and the pass col identical (§14.3). Failure is stop GF-S3.

---

## 10. Views and the blind review (D-10)

- **Presentation**: G1's exactly (TerrainGenG3fDump's session is a copy of G3e's `TerrainGenLmcViews.Session`): URP
  TerrainLit, neutral layer 160/160/154, sun Euler (42, −35, 0) at 1.2, flat ambient, no fog, post-processing,
  anti-aliasing, HDR or skybox, a 24 m sea-level ribbon; one light preset.
- **Cameras** (`g3f/views.py`, identical for every candidate of a variant; placement text in each record): S-OVR,
  A-MTN, A-SAD, A-PLT-E, A-PLT-J, A-CONF, A-OUT, A-CLF (1920 × 1080), P-SITE, P-SAD, P-RAMP, P-TOP, P-VAL, P-COAST
  (1280 × 720, eye 1.7 m), route strips R1-0…7 and R2-0…7 (640 × 360); S-PLAN is a top-down hillshade of the window
  with the plan overlaid (Python).
- **Camera validity**: a player camera whose rays (48 × 27 grid) meet the terrain within 3 m on more than 30 % on any
  candidate's lattice is also rendered from +10 m; both are kept.
- **Sheets**: per variant and camera, 2 × 2: X | Y over Z | `H4A` 110201 (labelled reference, not rated). X, Y, Z are
  GF-C0, GF-UF, GF-RF in the order `sealed_order(seed, class)` (SHA-256 of `research.g3f.views:seed:class` mod 6);
  the key is written only to `sealed/blind_key.txt`.
- **Form** (phase plan §9.7, D-10): per variant, per scale, per applicable D6 point — 1ʳ, 3, 4, 5, 6ʳ, 8 — meets / partly
  / fails / — with a reason, absolute ratings of each panel first, then a best-and-worst preference; ratings hashed and
  time-stamped before the key opens. **The agent gives no assessment of the sheets.** The agent looked at one sheet per
  run only to confirm the four panels rendered.

---

## 11. Protocol

### 11.1 Calibration

Variants: 110201 and 1337, each short and tall (four variants, D-6 (f)). One **calibration run** = every candidate on
all four variants with readings, the `hydro` step, views and sheets, recorded under `research-data/g3f/calib/run<k>/`
with the code commit. Between runs only **named fixes** (a numbered change with its hypothesis and the readings it
addresses), at most **6 per candidate** and **5 runs** (`limits_before_wp1.md`; the plan's proposal, adopted, not
approved). A 7th fix or a 6th run stops that candidate (GF-S4).

### 11.2 Freeze rule [NEW]

A candidate is frozen for the held-out evaluation if it passes **every hard gate and every process gate** (GF-A for
GF-RF only) on **all four** calibration variants. If none qualifies: no freeze and a not-ready report (GF-S5). The
owner's blind calibration review (G-5a) cannot happen inside the mission; its sheets are prepared and its absence is
recorded. The freeze record (`research-data/g3f/freeze/FREEZE.md`) lists the commit SHA, the code and parameter hashes,
the fixture hash, this specification's SHA-256, the frozen candidates and the held-out list — and must exist before any
held-out seed is generated (the runner and the Unity step refuse otherwise).

### 11.3 Held-out evaluation

Seeds **5001 … 5010** (WP0 C0-4 verified unused); class by position — **odd positions short, even positions tall**
(`limits_before_wp1.md`); reserve 5011–5020 only after an invalidating post-freeze change (GF-S7). Every frozen
candidate (and GF-C0 as the control) on all ten; readings, `hydro`, views, sheets; a **determinism repeat** of the whole
evaluation in fresh processes.

### 11.4 Stop conditions

GF-S1 a protected file or pinned hash changes — stop at once. GF-S2 the fixture fails its first registered validator run
after 2 authored corrections. GF-S3 continuity fails. GF-S4 a 7th fix or 6th run. GF-S5 no candidate passes every hard
gate on all four calibration variants — no freeze. GF-S6 a candidate passes only with a changed definition — stop.
GF-S7 the held-out rule fails — no re-tuning. GF-S8 an out-of-scope defect (the planner, Unity) — that reading is
INCONCLUSIVE for the variant.

### 11.5 Failure classes (mission §5 F)

Every failed reading is classed: implementation error, invalid input, invariant violation, model insufficiency, data
insufficiency, inconclusive evidence.

---

## 12. Pass rule and decision rule (phase plan §§10.2–10.3, D-6)

A candidate **passes** when (1) every hard gate (GF-R4, R5, R7, R8, R9, R10, RS) and every process gate (GF-O, GF-B,
GF-D, GF-P; GF-A for GF-RF) holds on **≥ 7 of 10** held-out variants (a plan-invalid variant counts as failed; results
also per class), **and** (2) the owner's blind review rates no applicable D6 point *fails* and points 1ʳ, 3, 4 *meets*
on ≥ 7 of 10. Part (2) needs the owner; until the ratings exist, no candidate can be declared passed. Simplicity rule:
if GF-C0 also passes, the simpler surface is preferred unless the owner's review prefers the candidate. The decision
table of phase plan §10.3 applies (a tie between GF-UF and GF-RF goes to the simpler, D-4).

---

## 13. Artefacts, logs and commands

Per variant (`<run>/<seed>_<class>/`): `attempts.json`, `plan.json`, `plan_hash.txt`, `validation.json`, `params.json`,
`plan.png`, `context.f32`, `context_log.json`, `<cand>.f32`, `<cand>_synth.json`, `<cand>_readings.json`,
`<cand>_assembly.json`, `times.json`. Per run: `hydro/` (planner JSON per lattice), `views/` (camera list, renders,
sheets, `sealed/`), `SHA256SUMS.txt`.

```
python -B tools/g3f/g3f_run.py variant --seed 110201 --class short --out <RD>/g3f/calib/run1
python -B tools/g3f/g3f_run.py hydro-list --runs <run> --out <run>/hydro_list.txt
Unity.exe -batchmode -quit -projectPath <repo>/unity -executeMethod GameDev.EditorTools.TerrainGenG3fDump.DumpBatch
  -g3fStep hydro -g3fIn <run>/hydro_list.txt -g3fProtected <RD>/g3f/protected_md5_before.txt -g3fOut <run>/hydro
python -B tools/g3f/g3f_run.py merge-hydro --run <run> --hydro <run>/hydro
python -B tools/g3f/g3f_run.py views-list --run <run> --out-dir <run>/views
Unity.exe ... -g3fStep views -g3fIn <run>/views/views_<seed>_<class>.txt -g3fOut <run>/views/renders
python -B tools/g3f/g3f_run.py sheets --renders <run>/views/renders --out <run>/views/sheets
python -B tools/g3f/g3f_run.py manifest --dir <run>
```

`<RD>` = `D:/Hobby/game-dev/research-data`. Unity 6000.5.7f1, no editor open on the project.

**File breakdown (deviation from phase plan §8.1).** The plan proposed nine C# Editor files. G3f implements the plan,
the syntheses and the readings as a Python research package (`tools/g3f/g3f/`: `lattice`, `plan`, `validate`, `synth`,
`checks`, `measure`, `assembly`, `run`, `views`; `vendor/lmcspec.py` a byte copy of the approved G3e reference
replicas, SHA-256 `de666680…ed79`) and keeps one C# Editor file for what needs Unity (`contour`, `hydro`, `views`,
`h4a`). *Why:* the approved G3e measurement definitions already exist as executable Python reference replicas, the
continuity proof is direct, and iteration is fast; nothing is referenced by production either way, and a production
implementation is C# by the pipeline rule (G4). *Cost:* the surfaces are not C# TerrainGen code — a production port is
a G4 item, and cross-language determinism is unproven.

---

## 14. Analytic checks and continuity

### 14.1 Analytic checks (GF-AC1 … AC9; phase plan WP1 verification)

`tools/g3f/tests/analytic_checks.py` on five synthetic lattices (`tests/synthetic.py`): a cone (one catchment, all
shore roots), a straight ridge between two catchments with coastal fringes and a cliff stretch, a plateau block (top,
band, rim channel, ramp with side bands, fringes), a valley (trunk and tributary as declared axes, fringes), an
asymmetric divide (two catchments, a planned crest). Readings per case and candidate: AC1 pits; AC2 every unit vertex's
D8 path ends at a root of its own unit; AC3 D8 steps across or onto crests; AC4 the write log; AC5 the lateral bound
between same-unit non-exempt neighbours; AC6 bisection meets its target (cone); AC7 axis climbs (valley); AC8 plateau
top relief, ramp grade, K1 edge (plateau); AC9 GF-RF order invariance, per-unit = joint, locality under a perturbation
of one unit, and GF-UF relabel invariance and locality.

**Expectations** (properties the construction is designed to guarantee): AC1 = 0, AC2 all own-root, AC3 = 0, AC4 ok,
AC5 ≤ 0.01 m excess, AC7 = 0, AC9 GF-RF order-invariant, per-unit = joint and local; for every case and candidate. AC6
met on the cone. AC8 and GF-UF locality: reported (no expectation). *Honesty note:* the checks were developed together
with the code — they found and drove the fixes listed in §15.1 — and are then run unchanged as the registered check
(§14.2).

### 14.2 Results of the registered run [MEASURED]

Run on the code commit `7344699` (file hashes `research-data/g3f/wp1/SHA256SUMS.code.txt`), twice in fresh
processes (`ac_run_a`, `ac_run_b`): every reading byte-identical, only the recorded run time differs. **Every expectation
holds:** no pit, every unit vertex ends at its own root, no crossing or onto-crest step, the write log exact, the lateral
bound exact (worst excess ≤ 0), the cone's bisection met, both axes climb 0.0000 m, GF-RF order-invariant, per-unit =
joint and local (0 vertices outside the perturbed unit change); GF-UF relabel-invariant but **not local** (2 006
vertices outside the perturbed unit change, through its emergent crest and its global multiplier — as designed). Plateau
block: top relief 1.26 m, ramp 18–19°, K1 edge 38.0–38.2 m against a 40 m target. Steep triangles (≥ 45°) outside
exempt bands occur on every synthetic case and all touch a crest line: walls where two sides of a crest differ — a
diagnostic, not an invariant.

| case | AC1 pits | AC2 bad ends | AC3 cross / onto | AC4 write | AC5 worst excess m (pairs) | other | steep >= 45 outside exempt (touching crest) |
|---|---|---|---|---|---|---|---|
| cone/C0 | 0 | - | 0 / 0 | True | -0.6073 (0) | AC6 law 99.52 vs 99.50 met=True (final 99.52) | 0 (0) |
| cone/UF | 0 | - | 0 / 0 | True | 0.0000 (0) | AC6 law 100.21 vs 99.50 met=True (final 83.77) | 0 (0) |
| ridge/C0 | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 652 (652) |
| ridge/UF | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 948 (948) |
| ridge/RF | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 380 (380) |
| plateau/C0 | 0 | - | 0 / 0 | True | 0.0000 (0) | AC8 relief 1.26 m, ramp 17.97 deg capped=False, K1 38.20797424951423 vs 40.0 | 1010 (1010) |
| plateau/UF | 0 | - | 0 / 0 | True | 0.0000 (0) | AC8 relief 1.26 m, ramp 19.00 deg capped=False, K1 38.041096595770384 vs 40.0 | 1065 (1065) |
| plateau/RF | 0 | - | 0 / 0 | True | 0.0000 (0) | AC8 relief 1.26 m, ramp 19.00 deg capped=False, K1 38.041096595770384 vs 40.0 | 1065 (1065) |
| valley/C0 | 0 | - | 0 / 0 | True | 0.0000 (0) | AC7 climbs 0.0000, 0.0000 | 1324 (1324) |
| valley/UF | 0 | - | 0 / 0 | True | 0.0000 (0) | AC7 climbs 0.0000, 0.0000 | 1250 (1250) |
| valley/RF | 0 | - | 0 / 0 | True | 0.0000 (0) | AC7 climbs 0.0000, 0.0000 | 795 (795) |
| asym/C0 | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 346 (346) |
| asym/UF | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 333 (333) |
| asym/RF | 0 | - | 0 / 0 | True | 0.0000 (0) | - | 84 (84) |

Assembly: RF {'AC9_order_invariant': True, 'AC9_per_unit_equals_joint': True, 'AC9_perturb_changed_outside': 0, 'AC9_perturb_changed_inside': 9022}; UF {'AC9_relabel_invariant': True, 'AC9_perturb_changed_outside': 2006, 'AC9_perturb_changed_inside': 3720}

### 14.3 Continuity [MEASURED]

Run 11's S-LM1 lattices, twice in fresh processes, identical output: **continuity PASS** — every C0-5 target reproduced
within G3e-PM's tolerances.

| Seed | S8 depressions (position / depth / area differences) | Axis climbs (G3e node reading) | Reachability (sites, plateau tops, pass col) |
|---|---|---|---|
| 110201 | 0.49 m / 0.00 / 0, 0.31 m / 0.00 / 0 | 10 axes, max abs. difference 0.0000 m | unreachable = target, unreachable = target, reachable = target |
| 1337 | 0.11 m / 0.00 / 0, 0.59 m / 0.00 / 0, 0.30 m / 0.00 / 0 | 7 axes, max abs. difference 0.0000 m | reachable = target, reachable = target, reachable = target, reachable = target |

The Unity `contour` step's export equals run 11's land mask and sea heights vertex by vertex; contour A's Stage 1 hash
`7599879a71250a06` and `H4A` 110201's pinned hash `e2bad571f4af76c7` were confirmed in every Unity process
(protected manifest 85 / 85 before and after).

---

## 15. Development record (before any registered calibration run)

### 15.1 What changed while the code was developed

The research package was developed against the synthetic cases of §14 and the four calibration variants (110201 and
1337, short and tall). No held-out seed was generated, read or viewed. Every change is classed (mission §5 F):

| # | Change | Found by | Class |
|---|---|---|---|
| 1 | crest vertices adjacent to lower crest vertices let interiors drain onto crests → emergent crests recomputed on the final surface; planned crests bound fringes too | AC3 on the ridge | implementation error |
| 2 | flank fill raised roots; roots fixed at `BASE` | AC1 on the asymmetric divide | implementation error |
| 3 | the tree did not follow declared axes; axes made forced tree paths, an axis carve after the lateral constraint | AC7 on the valley | design gap |
| 4 | a global tree carve after the lateral constraint re-opened lateral excesses; replaced by a carve only below lowered vertices | AC5 | implementation error |
| 5 | outlet zones too wide (roots up to 120 m from outlets); replaced by the mouth strip | GF-V3 on the calibration plans | implementation error |
| 6 | an axis entering its outlet reversed on itself (a cycle in the forced receivers); trailing points dropped, cycles cut | GF-V3 (479 unreached vertices) | implementation error |
| 7 | the ramp search from the whole rim found ramps ≤ ~240 m; one start per bearing (ramps may wind) — tall plateaus feasible up to ~85 m edges | GF-V4 on 110201 tall | design gap |
| 8 | the Sector crest planned for every candidate made the bisection insensitive and GF-C0 inflate to ~600 m; Sector emergent in GF-C0/GF-UF, planned in GF-RF and the context | development readings | design decision |
| 9 | the drawn 330–430 m summit infeasible in the sector at the walkability bound; synthesis targets clamped (§4.2), drawn values kept and flagged | development readings (GF-UF saturated at ~195 m) | model/fixture limitation |
| 10 | context higher than M1 (M1 not a peak) → context cone below M1 | GF-R8 (M-1) | design gap |
| 11 | lowlands too steep for a capital, coasts all read as cliff → region factors and tree weights set from slope targets (§7.1–7.2) | GF-R4, coast diagnostic | design decision |
| 12 | an unlimited flank fill turned high emergent crests into 35° cones over whole lowlands; emergent crests now 30 m, planned crests unlimited | GF-C0 lowland 232 m | design gap |
| 13 | GF-UF could not realise M2 or a pass; culmination factors (the S-LM3 family's mechanism, G3e §5.4) for M2 and S1 | GF-R8 | design decision |
| 14 | GF-RF's path-normalised law (to each subtree's highest, then lowest planned crest leaf) produced crest walls and 35° cones; replaced by the per-unit law of §7.2 | GF-R8 (col unreachable), development renders | design decision |
| 15 | bisection on the final surface was non-monotone in practice; moved to the law's own heights | development | implementation error |

**Disclosure.** Changes 8–14 were decided after seeing development readings on the calibration variants, and changes
12 and 14 after the agent looked at development hillshades of the sector (an informal look, not a rating). They are
design decisions taken before the freeze of this specification; they count as pre-registration development, not as
named calibration fixes, because no calibration run had been registered. The calibration run cap and fix cap start
from zero at the first registered run. The held-out evaluation is untouched by any of this.

### 15.2 What WP0 established (summary; records in `research-data/g3f/wp0/`)

C0-1: of S-LM3's > 70° land triangles outside its cap bands, 100 % contain a definitely lateral edge (90 % and 76 % of
all > 70° triangles lie outside the bands) — the premise of L-3 holds. C0-3: the sector and window above. C0-4:
5001–5020 unused. C0-5: the continuity targets of §9.5.

---

## 16. Results

*(appended by run; definitions above are not edited after the freeze)*

### 16.1 Calibration run 1 [MEASURED]

Code commit `a5bcdd6` (`research-data/g3f/calib/run1/commit.txt`, file hashes `SHA256SUMS.code.txt`); four
calibration variants × three candidates, the whole run twice in fresh processes (copies `a` and `b`, started
2026-10-27 22:37 and 22:43 UTC), the `hydro` step on both copies, views, renders and sealed blind sheets on copy `a`
(122 sheets, 488 renders; the agent checked only that the files exist and are non-empty). Every plan validated at
attempt 0. About 170 s of Python per variant and copy (plan 11 s, context 5 s, synthesis 7–21 s per candidate, readings
15–18 s, assembly 26–40 s); `hydro` ≈ 0.3 s per lattice. Records: `research-data/g3f/calib/run1/` (`gate_table.md`,
`compare.txt`, `MANIFEST`, the diagnosis scripts in `diag/`).

**GF-D** 92 of 92 files identical between the copies; **GF-P** every Unity process 85 / 85 before and after. A
comparison-tool correction was made before any gate was read from it (T1 below).

| Variant | GF-C0 | GF-UF | GF-RF |
|---|---|---|---|
| 110201 short | every gate | every gate | every gate |
| 110201 tall | **R8** | **R8** | **R4, R5, R7, R10** |
| 1337 short | **R8** | every gate | every gate |
| 1337 tall | **R8** | **R8** | every gate |

Freeze rule (§11.2): GF-C0 1 of 4, GF-UF 2 of 4, GF-RF 3 of 4 — **no candidate qualifies after run 1.** GF-O, GF-B,
R7 (a) and (c), R9 and RS (≥ 99.5 %) hold everywhere; GF-A for GF-RF local and order-invariant on all four (GF-UF
relabel-invariant, not local, as designed). GF-T flags the main culmination outside 330–430 m on every lattice (the
feasibility clamp of §4.2, ≈ 294–299 m).

**Failures and their classes** (mission §5 F):

1. *GF-C0 and GF-UF, R8 — M2 not realised* (model insufficiency). The highest mountain vertex within 150 m of M2 sits on
   the reading disc's edge, 148 m west (293, 660), with prominence 0 (GF-UF 1337 tall: 17.8 m < 30 m). With D1
   emergent, the crest is lowest near M2 and rises west to the coast end (x ≈ 262: GF-UF tall 269 m, GF-C0 tall 241 m)
   before a ≈ 200 m drop to the fringe: the far western ends of catchments A and C — the farthest points of their trees
   from O1 and O3 — become their highest ground and invert the plan's D1 profile. In the short variants GF-UF realises
   M2 93 m east of the declared point with prominence 38 m (an 8 m margin). This is the situation D-4 named as R-F's
   purpose ("crests set by the plan").
2. *GF-RF, 110201 tall, R10 (and R5, R7 (b), which read the failed planner)* (model insufficiency). The planner's
   validation stopped at "reach_12 flows uphill at node 1: 63.76 m to 88.93 m". The report-only diagnosis (T2) shows
   node 1 sampling the D1 crest vertex (317, 662) at 177.1 m between trench vertices at 67.7 m. Traced step by step
   (`diag/trace.py`): S7b capped (300, 662) from its law height 254.2 m to 67.7 m — the cap of a vertex next to D1's
   low coast end, carried down a tree path that runs east **along D1's foot toward M2** — and the lateral constraint then
   lowered the flanks from that trench (C side the same way). It is systematic: in all four GF-RF lattices 79–107 D1
   crest vertices stand more than 30 m above both flanks (maximum 107–118 m), and S7b carves ≈ 10 000 vertices per
   variant; R10 failed where the planner's smoothed nodes happened to touch the wall.
3. *GF-RF, 110201 tall, R4 — no capital* (invalid input). The best 150 m disc within 170 m of the reserve centre has
   10.24 % unbuildable ground (≤ 10 % required; 1337 tall passes at 9.71 %). Computed from the plan alone
   (`diag/reserve_fit.py`), **36.6–38.1 % of the fixture's reserve core disc lies under the 35° cones of the planned
   crests** (M1 ≈ 294 m at 428 m, D2 ≈ 170 m at 246 m), so the capital has to shift ≈ 120 m south-west — where the tall
   ramp lands (the builder takes the landing nearest the reserve, outside its 170 m disc).
4. Diagnostic, not a gate: in GF-C0 and GF-UF the Sector crest stands > 30 m above the sector side at 226–325
   vertices (emergent, it takes the context's height near M1). Not addressed.

**Tool corrections (no reading changed):** T1 — `calib/compare_runs.py` hashed JSON files whole, so the timing field
`seconds` of the synthesis, context and hydro logs counted as differences (33 "different"); GF-D's definition excepts
timing, so the tool now drops that field and skips logs and step input lists (they name each copy's own paths); the
first output is kept as `compare_raw.txt`. T2 — the Unity `hydro` step reports, for an uphill validation failure, the
failing reach's nodes (position, bed, lattice height nearest and bilinear, filled height); report-only, after
G3e's `DiagnoseR10`.

### 16.2 Named fixes before calibration run 2

Registered before run 2, each with its hypothesis and the readings it addresses (§11.1). Both change shared steps, so
each counts for every candidate: **fixes used 2 of 6 per candidate; runs used 1 of 5.** No gate, reading, threshold or
seed changes.

**F1 — plan-guided tree (S1; `Params.guide_climb` = 2.0 per metre of climb [AUTHORING]).** A catchment tree edge
whose flow climbs the guide `G` (the 35° cone of the planned crest profiles of S, D1 and D2; plan only, the same for
every candidate) costs 2 × the climb in the Dijkstra priority. *Hypothesis:* GF-RF's D1 walls and the trench that broke
R10 on 110201 tall come from tree paths that run along the foot of a planned crest toward a higher crest segment and
carry the S7b cap of the crest's low end downstream; priced climbs make those paths leave the crest across its flank,
so no trench runs along a crest foot. *Addresses:* GF-RF R10 (with R5, R7 (b)) on 110201 tall; the crest-wall
diagnostic. *Expected:* GF-RF's D1 crest vertices > 30 m above both flanks near zero; no gate regresses. The value
2.0 was set once and not varied.

**F2 — the settlement reserve where the plan leaves its core flat (fixture; authored correction 1 — counted under the
S2 limit as well, conservatively).** Reserve centre (1 210, 2 150) → (1 135, 2 242) m: the stride-2 vertex within
170 m of the old centre that minimises the share of the 150 m core disc lying under the planned crests' 35° cones, on a
crest line, in a plateau footprint or at sea, computed on the four calibration plans from plan quantities only
(`calib/run1/diag/reserve_fit.py`; best (415–417, 820–822) on all four, 0.4–4.1 % against 36.6–38.1 % at the old
centre). *Hypothesis:* R4 failed on GF-RF 110201 tall because the fixture put the reserve core under its own crests'
cones, pushing the capital toward the tall ramp; with the reserve where the plan leaves its core flat, the ramp is kept
out of it (the builder forbids the reserve disc) and the capital disc fits for every candidate. *Addresses:* GF-RF R4
on 110201 tall. Routes R1 and R2 and the P-SITE view start at the new centre.

**Not fixed:** GF-UF's and GF-C0's R8 (failure 1 of §16.1) — the emergent divide's inversion is the reason D-4 gave
for GF-RF, and GF-C0 is the control; no candidate-specific change was made for them.

**Development trials (disclosed; calibration seeds only, not registered runs).** `dev_f1` (F1 alone; 110201 tall,
1337 short; hydro on three lattices): GF-RF's D1 walls > 30 m fell to 4–6 vertices (maximum 33 m), the planner
succeeded on GF-RF 110201 tall. `dev_f2` (F1 + F2; all four variants, no assembly; hydro on all): GF-RF passed every
hard gate on all four; GF-C0 failed R8 on all four; GF-UF failed R8 on 110201 tall and 1337 short and passed on 1337
tall (its M2 sits near the 30 m prominence threshold either way). The analytic checks of §14.1 still hold with F1
(`dev_ac`: no pit, no crossing, write log exact, lateral excess ≤ 0, climbs 0, GF-RF order-invariant and local).
The measurement code is unchanged, so the continuity check (§14.3) was not repeated.
