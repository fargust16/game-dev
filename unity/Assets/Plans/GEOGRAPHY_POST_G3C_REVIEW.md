# Post-G3c Review — Geography Decisions and Landform Model Research

Version: 0.5

Status: Draft — decision record and research plan. The owner asked on 2026-10-21 to settle GEO-1, GEO-2,
GEO-3, GEO-4, GEO-5, GEO-7 and GEO-8 where the evidence and the owner's explicit directions allow, and to
plan research into alternative landform models. **§2 records each decision with one of four statuses**
(§0); only the parts marked *Resolved* are decided. **Owner decision, 2026-10-21:** the pair of syntheses
recommended in §4.9 — **S-LM3 + S-LM1 on one shared LM2 layout** — is approved, and step 0 of the
comparison is written as [GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (v0.2, revised by the
owner), which supersedes §5 where they differ. Nothing is implemented or run; steps 1–5 still need the owner. Production terrain stays
`H4A = H1 + U2 + R4A`.

Last Updated: 2026-10-23 (the official ID G3e noted in §5; 2026-10-22: pointer to the LMC technical specification in §7 item 4)

Depends On:
[AGENTS.md](../../../AGENTS.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Geography-first world shape,
[90 Glossary](../../../90%20Glossary.md),
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (D6, D8, §§3–8),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§1, 4–7, 11–14),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md) (§§3, 7, 16),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) (§§9, 11),
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) (§§20–22),
[17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) (W.4, W.5),
[WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md);
reference image [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-1 … GEO-9;
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §15;
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) §9;
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md);
[AGENTS.md](../../../AGENTS.md); [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) §§1, 3, 6;
[90 Glossary](../../../90%20Glossary.md) § Large Plateau, § Declared Basin;
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md)

---

## 0. How to read this

**Decision statuses** (used in §2 and everywhere this review is cited):

| Status | Meaning |
|---|---|
| **Resolved — owner direction** | directly supported by an explicit owner statement, dated and quoted or cited |
| **Resolved — evidence-backed policy** | a low-risk policy that follows from the approved target D6 and the recorded evidence; it decides no material design trade-off |
| **Proposed — owner confirmation required** | materially affects the game and the evidence does not determine one answer; a default is recommended |
| **Research required** | the direction is clear, a number or a technical contract still needs evidence |

A question may carry several statuses, one per part; each part says which.

**Owner statements used here.** The approved target **D6** (2026-10-20, brief §9) and its concept image;
the G2 directions on GEO-1, GEO-4, GEO-7 (2026-10-20); the G3c plan review and go-ahead (2026-10-20);
and the owner's directions of **2026-10-21**, given with the request for this review:

- GEO-1: "a reasonable increase in scale is permissible if justified, but the world must not be enlarged
  merely because G3c failed";
- GEO-4: use the diverse-coastline approach as the target direction;
- GEO-7: requirements must serve a world with the visual and geographical ambition of the reference, not
  constrain the world to make metrics easy to pass;
- GEO-8: several large plateaus in different regions, integrated into ridges and foothills; mountains
  remain dominant; traversal is mixed — some cliffs cannot be crossed directly, but routes, ascents or
  bypasses give meaningful access.

The owner's request also *recommended* resolutions for GEO-1 … GEO-8. Those recommendations are used as
the basis of §2, but where the request recommends rather than directs, the status says so.

**The target.** D6 and the concept image
[`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)
— a project reference inspired by the visual direction of DragonSword: Awakening, **not an official map of
that game**. The goal is its principles of geographical composition, not a pixel reproduction; no count,
size, height or position is read from it as a requirement.

**Tags.** **[MEASURED]** measured in G3c (research record §14) or earlier, source named; **[FACT]** read
from code or a document; **[VISUAL]** an agent reading of images, owner review pending; **[HYPOTHESIS]**
believed, not demonstrated, with the check that would settle it; **[PROPOSAL]** a recommendation.

**What this document does not do.** It implements no model, runs no generation, calibration or evaluation,
launches no G3d, changes no production code, `TerrainData`, scene, material, shader or asset, changes no G3c
threshold or result, deletes no research artefact, and commits nothing.

---

## 1. G3c — a completed experiment that failed acceptance

### 1.1 What is preserved

Full record: research record [§14](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md). Nothing below changes it; the
pre-registered readings R1–R8 and the comparison rule stay exactly as written and as evaluated.

- **Neither contour passed.** Contour A met R1–R8 on 2 of 10 evaluation seeds (777, 1001), contour B on 0
  of 10; the rule needed 7 of 10 **[MEASURED]**.
- **B is not a material improvement.** B's buildable lowland was 31–43 % larger on all ten seeds, largely
  because lowlands are a fixed 30 % share of 35 % more land; R7 failed on 6 of 10 B seeds and on none of
  A's **[MEASURED]**.
- **What held on all twenty lattices:** a capital plus four further settlement sites, at least one coastal,
  in at least three regions (R4 20 / 20) — against no coastal site on any surface measured in G2
  **[MEASURED]**.
- **What the views show [VISUAL]:** A repeats the same crescent, whale-backed ridge on every seed; B carries
  a central peak with radial slopes; on both, plateaus read as cylinders or drums placed on the surface,
  lowlands show planar facets, and coastal cliffs form an over-continuous band.
- **Hydrology:** the production planner rejected the network on five of twenty lattices ("reach flows
  uphill"); R5 passed vacuously on four A seeds because no river started in the narrow highland
  **[MEASURED]**.
- **Determinism** held (62 of 62 files byte-identical in a fresh process). Deterministic output is not
  evidence of geographical quality.

### 1.2 What G3c does not show

- It compared **two specific contours** with **one construction**. Contour B differs from A in shape as
  well as area (the large bay filled, close to square). G3c did not isolate land area from coastline
  shape and **did not show that island size causes any failure**.
- It does **not** show that region-first geography is invalid. It shows that *this* construction — regions
  on a fixed mask, a surface built as eikonal distance from a designed drainage tree, a highland capped by
  an envelope, plateaus composed on top with `max()`, coastal classes assigned per segment — does not reach
  D6.
- **R6 is not validation of coastline diversity.** It checks classes that the construction assigned by
  hinterland, so consistency was largely built in; it passed on 19 of 20 lattices while the views show an
  over-continuous cliff band.
- **R4 is not evidence of geographical success.** Sites were found on every lattice, including the ones
  that read worst.

### 1.3 Architectural lessons

| # | Observed | Root cause in the representation | What a future model must do |
|---|---|---|---|
| L1 | planar facets in lowlands and on flanks | the surface is a distance field: constant slope per region from its sources, no concave long profiles, no convex hilltops | produce slope that varies with position in the drainage — steep near divides, gentle downstream |
| L2 | drum- or cylinder-like plateaus | plateaus composed over the surface as independent overlays; a wall everywhere the top stands above the ground | make plateau tops, rims and their access part of the same representation as the ridges and valleys around them; rims retreat unevenly and are cut by valleys (the concept image's plateaus are stepped and dissected, with falls at their edges) |
| L3 | one summit or a whale-back; a radial cone on B | the highland is an envelope cap with Gaussian culminations; ridges are equidistance lines between valleys | carry several culminations, saddles and branch ridges as structure, and let dissection break the crest |
| L4 | over-continuous cliff band | coastal class switched on wherever highland or plateau lay within 300 m | derive the coastal profile from local relief and break it at valley mouths and outlets; report run lengths |
| L5 | planner rejected 5 of 20 networks | the surface was composed from parts drained separately | make every candidate surface one the **production** hydrology planner accepts; treat a rejection as a candidate failure until shown to be a tool defect |
| L6 | R5 vacuous on 4 seeds | the reading quantified over an empty set | readings must be non-vacuous: require the thing to exist, then test it |
| L7 | eighteen construction fixes in calibration | each fix patched an interaction between independent layers (envelope × rivers, plateaus × surface, cap × floors) | prefer one representation with fewer independent layers; count calibration fixes as a process diagnostic |
| L8 | the same layout on every seed of contour A | a fixed contour plus medial placement leaves the seed little to decide | let the seed choose structure (divide topology, branch count, plateau placement), and measure variety between seeds |
| L9 | more climbing axes on B's larger highland | interactions grow with the highland's size | test hygiene on the largest configuration, not only the typical one |
| L10 | sites and outlets everywhere, composition still failing | settlement capacity stopped being the bottleneck once the coastal ramp was replaced | judge landform quality directly; capacity is necessary, not sufficient |

**Failure modes every future model must avoid** (each with a diagnostic proposed in §3.3): planar facets;
whale-back crests; radial cones; overlay plateaus (drums); over-continuous coastal classes; networks the
production planner rejects; vacuous readings; seed-invariant layouts; accidental pits and climbing valley
axes; terrain flattened to make sites.

---

## 2. GEO decision records

The exact wording of each question is in [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain —
Geography-first world shape; it is summarised here, not changed.

### 2.1 GEO-1 — Geographic scale

- **Question and scope (99, tag [before G2]):** must the target geography fit the locked V0 world
  (2 800 × 2 800 m; land 32–40 %)? It asks **only whether geography is a reason to answer the world-size
  question now**; the V1 overworld size itself is the separate § Exploration question, and a 3 × 3-tile
  world is the travel-pacing decision D3.
- **Owner statements:** 2026-10-20 — decide A against B by experiment, the 3 × 3 world separate, the
  footprint a hypothesis; 2026-10-21 — a reasonable increase is permissible if justified, never merely
  because G3c failed.
- **Evidence:** G3/G3b never varied the footprint (research §13.2); G3c compared two contours that differ in
  shape and area, neither passed, and B was not a material improvement (§1.1).
- **Resolution — Resolved — owner direction (2026-10-21):** **geography is not a reason to change the world
  size or the land fraction now.** The V0 world and Stage 1's land-fraction band stay. Contour A (seed
  110201's Stage 1 coastline) is the **baseline footprint** for landform-model research. Contour B remains a
  tested research candidate, not evidence that area is the limiting factor. The question is reopened only
  when (1) a landform model is promising on the baseline, and (2) either an experiment that separates area
  from shape (G3d's design) attributes its remaining failures to room, or a gameplay requirement — travel
  pacing D3, the § Exploration size question — calls for more land.
- **Changes:** the footprint is no longer an open variable of the next research step.
- **Does not authorise:** a Stage 1 change, contour B in production, a larger world, G3d.

### 2.2 GEO-2 — Mountain budget (the T1 count)

- **Question and scope:** `WORLD_RELIEF_PRINCIPLES.md` §1 sets the **T1 massif** count at 1 per island
  (2 max). Is the target (a) one connected, branched system counted as one T1, (b) several separate T1
  ranges, or (c) the single massif with relief only below it?
- **What T1 is [FACT]:** a row of the Draft relief-tier table — role "the landmark, visible from
  everywhere", 250–530 m above local ground, footprint 1 000–2 000 m, count 1 (2 max). Its source in
  production is the Stage 2 massif uplift (principles §7). The table states design intent; it is not a
  production gate (the acceptance table of principles §6 does not read it), and production Stage 2A/2B
  enforce their own limits (`MaxSystems` 2, crest 200–330 m).
- **Owner statement:** D6 point 1 (2026-10-20) — "a branching mountain system with multiple peaks,
  connected ridges, spurs, passes and valleys … neither one smooth central dome nor a uniform field of
  mountains"; confirmed 2026-10-21 ("mountains remain dominant").
- **Evidence:** G3 — one connected branched system fits all seven canonical seeds; G3c — culminations ≥ 30 m
  prominence 1–5 per seed, highest points 253–308 m above sea level **[MEASURED]**.
- **Resolution — Resolved — owner direction (D6):** **option (a).** T1 names the island's **mountain
  system**: one connected, branching system with a main divide, several culminations, branch ridges, spurs
  and passes. Its culminations and ridges are parts of one T1 form, not separate T1s; the 2.5× separation
  rule is a rule between tiers and is not applied among them. The count stays **1 (2 max)** as written:
  D6's target is one dominant system, and a second separate system (option (b)) is not part of it.
- **Classification of the row:** a **provisional Draft classification**. Its heights and footprint are
  authoring values, unchanged by this decision and not re-measured against D6. **Research note (not a
  change):** G3c's systems topped out at 253–308 m above sea level, so their relief above local ground sat
  near or below the row's 250 m floor; whether D6's dominant mountains need more of the 700 m budget is a
  measurement for the next model (§5), not a reason to change the row now.
- **Does not authorise:** any change to Stage 2A/2B, to the tier table's numbers, or to production.

### 2.3 GEO-3 — Where the mountain system lives

**The request's GEO-3 is not the GEO-3 in 99.** The request describes GEO-3 as settlement and POI placement;
in 99, GEO-3 is "**Where the mountain system lives** [after G3; decided in G4]" — the stage that would own
the system. GEO-3 keeps its scope here. The settlement and POI relationship the request asked for is
recorded as a new question, **GEO-9** (§2.8), so that neither is decided under the other's name.

- **Question and scope:** if the system grows beyond today's massif, (a) revise Stage 2B into a branched
  system, (b) add a positive stage after `H2`, as Stage 4A was, or (c) a geography skeleton generating
  ridges and valleys together for the whole island, of which today's massif plus Stage 3A–3C is a special
  case. Decided in G4.
- **Evidence:** G3 — (c) reached every seed, (a) cannot reach massif-free worlds while Stage 2A certifies
  the axis, (b) adds ridges without their dual valleys (research §11.8); G3b — the network must be routed
  on the final surface (§12.6); G3c — a region-first variant of (c) made lowlands and coasts work and failed
  on landform quality (§1); G2 — a skeleton that owns the coastal profile also reaches Stage 1's
  golden-hashed `H1` inside the land (brief §6.3).
- **Resolution, research direction — Resolved — evidence-backed policy:** landform-model research continues
  under **(c)**: regions, ridges, valleys, plateaus and coastal transitions come from one structure.
  (b) alone is not pursued: it violates the duality rule (Stage 3 design §3.1). (a) is not pursued as the
  mechanism, because Stage 2A certification leaves massif-free worlds without mountains; a chosen model may
  still reuse Stage 2A/2B concepts.
- **Resolution, production placement — Proposed — owner confirmation required (at G4):** which stages a
  chosen model supersedes (2B, 4A, 3A–3E; and `H1` inside the land if it owns the coastal profile), the
  scoped unfreeze, the `GeneratorVersion` bump and the re-audit. The agent's recommendation is (c), from
  G3–G3c; it is **not** an approval of an unfreeze, and G4 waits until a model passes its evaluation.
- **Does not authorise:** an unfreeze, a new stage, any production work.

### 2.4 GEO-4 — Coast contract

- **Question and scope:** every relief term is zero at and seaward of the shore and gated over the 200 m
  coastal rise. Should relief rise steeply just inland of an unchanged shoreline? Options: (a) keep the
  gate; (b) steep relief inland with the land mask bit-identical; (c) let the shoreline move (a Stage 1
  reopening).
- **Owner statements:** 2026-10-20 (D5) — spatially varied coastal transitions caused by the inland
  geography are the preferred direction; 2026-10-21 — the diverse-coastline approach is the target
  direction.
- **Evidence:** G0 — cliff share of the shoreline 0–3.7 %, headlands 0–2 and coves 0–1 per seed on `H4A`;
  G2 — no coastal settlement site on any surface measured; G3c — per-segment profiles behind a fixed
  shoreline gave coastal sites on 20 of 20 lattices, but the cliff band was over-continuous and R6 checks
  assigned classes (§1.2).
- **Resolution, target — Resolved — owner direction (2026-10-20, 2026-10-21):** **varied coasts caused by
  their hinterland are the target**: beaches and low shores at lowland fronts and outlets, coves at valley
  mouths, rocky shores and headlands, cliffs where high ground meets the sea — never one uniform ramp.
  Option (a), the uniform gated ramp as the coast model, is therefore not the target direction (it allows no
  sea cliffs or headlands).
- **Resolution, production contract — Research required, then Proposed at G4:** (b) against (c), and which
  stage owns the profile given that `H1` is golden-hashed Stage 1 height. **Default for research: (b)** —
  profiles vary behind contour A's fixed shoreline; planform variety (coves and headlands as shapes) on
  Stage 1's coastlines is measured; whether it needs (c) is G3d's question, after a model is promising.
- **Does not authorise:** G3d, a Stage 1 change, a coastal stage, any production change. G3c's R6 result is
  not cited as validation of coastline diversity.

### 2.5 GEO-5 — Basins and inland water

- **Question and scope:** the approved hydrology MVP (2026-09-03) routes every system to the ocean and fills
  every closed depression to its spill level — no endorheic basin; V0 water is ocean only. Does the target
  keep that, include lakes in declared basins, or also endorheic basins? Options (a) keep fill-then-spill
  and ocean outlets with basins as filled flats or dry hollows; (b) lakes in declared basins with outlets;
  (c) also endorheic basins. Sits beside 3F-10, 3F-11, 3F-14 without answering them.
- **Facts:** the hydrology analysis does not place lakes; a lake is any filled component above
  `MinLakeAreaSqm` and `MinLakeDepthMeters`, with one outlet at its spill point and an uncarved sill
  (`HYDROLOGY_GENERATION_PLAN.md` §9). Undeclared closed depressions are defects under the S8 rule
  (research §7.2 D-d); principles §3 says a closed basin is a bug unless it is a lake.
- **Owner statement:** D6 point 6 — "lakes sit where the structure holds them".
- **Resolution, principle — Resolved — owner direction (D6):** lakes are part of the target **where the
  structure holds them**; they are not inserted at arbitrary locations and not required on every seed.
- **Resolution, policy — Resolved — evidence-backed policy: option (b).** Three classes are kept apart:
  1. a **natural closed basin** — an enclosure the geography itself declares (enclosure, spill point,
     outlet), such as an intermontane hollow, a cirque or a plateau-top hollow; it may hold a lake or be
     realised as a filled flat (3F-10, 3F-11, 3F-14 decide realisation);
  2. an **intentional lake** — a declared basin with a retention rule: surface at the spill level, one
     outlet continuing to the ocean, the outlet sill uncarved (hydrology §9);
  3. an **accidental pit** — any closed depression the geography did not declare: a terrain defect. Because
     fill-then-spill would turn it into a "lake", every lake the analysis finds must coincide with a
     declared basin, or it is reported as a defect.

  Every lake keeps exactly one outlet and the **ocean-outlet invariant stays**; endorheic basins (option
  (c)) are not adopted — reopening them changes the validator's invariant and is the owner's call. Term:
  [90 Glossary](../../../90%20Glossary.md) § Declared Basin.
- **Research required:** how the chosen model declares basins (enclosure, depth — B4, realisation — F2);
  inland water rendering, which stays outside V0.
- **Does not authorise:** lake generation, a lake experiment, inland water in V0 (the Resolved Decision
  "V0 inland lakes / rivers" stands), any hydrology change.

### 2.6 GEO-7 — Target and acceptance criteria

- **Question and scope:** is the target of research §4 the accepted direction, and which proposed metrics
  of §7 become gates, with which thresholds?
- **Owner statements:** D6 (2026-10-20) supersedes research §4 where they differ; 2026-10-20 — metrics help
  evaluate the target and must not restrict it, Strahler order, basin area and the low-slope share are
  diagnostics; 2026-10-21 — requirements serve the target's ambition, not easy metric passes.
- **Resolution, target — Resolved — owner direction:** D6 is the target.
- **Resolution, classification — Resolved — owner direction (2026-10-20, 2026-10-21):**

  | Class | Contents | Rule |
  |---|---|---|
  | **Hard gates** | requirements with direct gameplay or topological meaning: capacity for 3–5 settlements including one capital-size site; numerous, varied, reachable POI sites (capacity, never a quota); useful walking routes between settlement regions; intended accessible places reachable (deliberate exceptions declared, 3F-20); no accidental topological defects (S8 outside declared basins, climbing valley axes, D-c continuity, a network the production planner accepts); rivers with elevated sources reaching designated outlets; process gates (determinism, pinned production hashes, the land-mask control in experiments) | a candidate failing one fails, whatever its views |
  | **Visual review** | the owner's review at island, regional-aerial and player scale (D6 point 8) | required for acceptance; composition is judged here, with proxies reported beside it |
  | **Diagnostics** | Strahler order, basin areas, the low-slope and steep shares, bifurcation ratio, Hack exponent, G0's M1–M6, tier conformance, the form diagnostics of §3.3 | reported, never gating, unless a gameplay purpose is recorded for a specific one |
  | **Provisional assumptions** | settlement size classes, slope and spacing (D2); POI pacing; route detour values | working values until the owner approves them |

  Thresholds pre-registered for an evaluation are never changed retroactively. Flat land is never rewarded
  for its own sake: the low-slope share counts toward nothing, and a candidate that gains buildable land by
  flattening the mountain and plateau composition fails the visual review.
- **Resolution, the next reading set — Proposed — owner confirmation required:** R1–R8 reviewed and a v2 set
  proposed in §3, for pre-registration before the next evaluation. G3c's rule is not rewritten.
- **Research required:** numerical values for the new gates (route quality, POI-site capacity per class).
- **Production:** the acceptance table of principles §6 keeps its role as a regression check on today's
  `H4A` bakes; its "≥ 35 % under 5°" and "6–15 % above 45°" rows are not targets for geography candidates.

### 2.7 GEO-8 — Plateaus and escarpments in the tier system

- **Question and scope:** D6's large plateaus may exceed the T2 row (18–40 m above local ground,
  150–450 m across, 6–10 per island) and fall between T2 and T1, which the 2.5× separation rule forbids.
  Options: (a) stay within T2; (b) change the T2 row; (c) large plateaus as a relief-region form outside the
  tier table, the 2.5× rule applying to forms standing on them.
- **Owner statements:** D6 points 2–4; 2026-10-20 — tier rules diagnostic inside G3c only; 2026-10-21 —
  several large plateaus integrated into ridges and foothills, mountains dominant, mixed traversal.
- **Evidence [MEASURED]:** G3c built plateaus of both escarpment classes (18–40 m and 60–120 m) and verified
  them (flat top, escarpment on ≥ 20 % of the rim, a walking route to the top) on 19 of 20 lattices; the
  60–120 m class falls between T2 and T1 and **[VISUAL]** reads as the stronger landform. Both read as drums,
  which is a construction defect (L2) that confounds any reading of height alone.
- **Resolution, principle — Resolved — owner direction (2026-10-21):** a candidate is **not rejected solely
  because a large plateau falls outside the T2 row or between tiers under the generic 2.5× rule**. Option
  (a) is therefore not the target. **Tier labels classify scale and legibility, never traversal**: whether a
  face blocks the player is read from geometric steepness and walking connectivity, and decided under 3F-20.
- **Production:** the tier table and the 2.5× rule stay in force for production design until deliberately
  revised. (No plateau stage exists in production: T2 is "not built", principles §7.)
- **Resolution, revised contract — Proposed — owner confirmation required. Default: (c).** Two plateau
  classes:
  1. **T2 shelf** — the existing row unchanged: a district-scale climbable shelf, 18–40 m.
  2. **Large plateau** — a level-1 relief-region form of the Geographic Hierarchy, outside the T1–T3 height
     table ([90 Glossary](../../../90%20Glossary.md) § Large Plateau, proposed). It is separated from T1 by
     **form** — a readable top behind an escarpment on part of its rim, joined to a ridge or foothill on
     another part — and it stays **subordinate to the mountain system**: its top below the system's main
     culminations, never the landmark. The 2.5× rule keeps applying to forms standing on it (T2 shelves,
     T3 outcrops) and between it and those forms. This follows the precedent of principles §1's 2026-09-16
     amendment, which separated T0 floor relief from T2 by slope rather than height.

  *Why not (b):* widening T2 to, say, 120 m makes one class out of a district shelf and a regional
  tableland, and breaks the 2.5× separation between T2 and T1 (120 m × 2.5 = 300 m against T1's 250 m
  floor) unless the T1 row moves too — so (b) changes two rows to express one form.
- **Research required:** the large plateau's numbers — escarpment height range, top area, count, and the
  subordination margin below the culminations. **Minimum evidence:** from a model whose plateaus are not
  overlays (L2), the distributions of escarpment height and top height against culmination height on
  calibration and held-out seeds; the owner's review of regional obliques at several height ratios; the
  plateau-integration diagnostic Q6 (§3.3). G3c's measurements show both classes can be built and walked to;
  they are not sufficient to fix a number.
- **Does not authorise:** a change to the tier table, a plateau stage, any production work.

### 2.8 GEO-9 (new) — Geography and settlement / POI placement

- **Question and scope:** how terrain generation and settlement and POI placement divide responsibility so
  that the island holds 3–5 settlements (one possibly a capital) and numerous varied, reachable POIs without
  the terrain being distorted to meet a count.
- **Owner statements:** D6 point 7 — "the world supports 3–5 settlements and numerous POIs while keeping
  visually strong, varied terrain; not every area has to be suitable for construction"; the relationship
  recommended in the request of 2026-10-21.
- **Evidence:** G2 — capacity on today's surfaces is set by the coastal ramp and the mountain share (brief
  §4.3); G3c — sites on all twenty lattices, including the ones that read worst (L10). W.4's V0 demo
  requests markers by quota over a 3 × 3 sector map and scores candidates per kind by local slope and
  flatness — not from geographic regions (`PoiDemoScenario`, `WorldSectorMap`, `PoiPlacementEngine`)
  **[FACT]**.
- **Resolution — Resolved — evidence-backed policy:**
  - **Geography generation (TerrainGen) owns** regions and their relief; candidate buildable areas as a
    consequence of designed lowlands, valley floors, plateau tops and coastal plains; routes, passes,
    barriers and bypasses; coastal forms and outlets. It exposes them as read-only structure — region map,
    candidate areas, route graph — and reports capacity as a diagnostic.
  - **Placement (W.4 POI Placement, W.5 Settlement, roads under the Road Passability contract) owns** the
    choice of sites among the candidates, counts, spacing, the capital, pacing and the road network. It reads
    the terrain and edits it only as its own contracts allow (road earthwork within `MaxCutDepth` /
    `MaxFillHeight`; building pads, deferred in W.5). It never flattens or reshapes terrain at region scale
    to meet a count.
  - **When capacity falls short, the fix is in the geography's design** (region budgets, reserved
    lowlands), never a placement-driven terrain edit; and a site count alone never proves geographical
    success.
  - **Capacity is evaluated with route quality**: reachability of each site, road-feasible links between
    settlement regions, alternative routes and detours.
- **Depends on unresolved assumptions:** D2 (settlement size classes, slope, spacing; whether terraced
  hillside towns belong to the style); D3 (travel pacing); POI pacing in seconds between discoveries;
  3F-20 (whether cliffs are barriers); the timed-POI density of [04](../../../04%20Points%20of%20Interest.md).
  The working terms *settlement site* and *POI site* (brief §0) still await D9 before they get Glossary
  entries.
- **Does not authorise:** any change to W.4, W.5, roads or the POI demo scenario.

### 2.9 Summary

| Question | Resolved — owner direction | Resolved — evidence-backed policy | Proposed — owner confirmation | Research required |
|---|---|---|---|---|
| GEO-1 | geography is no reason to resize now; baseline footprint; revisit trigger | — | — | — (reopens on its trigger) |
| GEO-2 | (a): one branching system = one T1; count unchanged | — | — | T1 heights only if a model approaches them |
| GEO-3 | — | research under (c) | production placement (G4) | — |
| GEO-4 | target: varied, hinterland-caused coasts | — | contract (b)/(c) and owner stage, at G4 | planform variety (G3d, later) |
| GEO-5 | lakes where the structure holds them | (b); three classes; ocean-outlet invariant | — | basin declaration and depth in the chosen model; rendering after V0 |
| GEO-7 | D6 is the target; four-class criteria | — | the v2 reading set (§3) | values of new gates |
| GEO-8 | never rejected for exceeding T2; tiers ≠ traversal | — | (c) large plateau as region-level form | its numbers |
| GEO-9 | — | geography provides, placement chooses | — | D2, D3 values |

---

## 3. Acceptance criteria after G3c

### 3.1 R1–R8 reviewed individually

G3c's readings and its rule stay as evaluated (research §14.1, §14.6). The column "v2" is a **proposal**
for the next experiment, to be pre-registered before it runs; implementation notes of §14.1 (climbing
sampled at the nearest vertex) carry over.

| Reading | What G3c showed | Weakness | Proposed v2 | Proposed class |
|---|---|---|---|---|
| R1 mountain structure (≥ 2 summits ≥ 30 m prominence, branching, a pass draining to two outlets, single dome not flagged) | failed on single-summit layouts (2 A, 4 B); passed on whale-backs | counts summits; blind to crest shape (L3) and to radial cones | keep the structural tests; add culmination height spread and the form diagnostics Q3–Q5 beside it | **proxy** for D6 point 1, read with the review |
| R2 plateaus (≥ 2 verified, ≥ 500 m apart, flat top, ≥ 20 % escarpment rim, near highland or upland, reachable top) | 19 / 20 | passes drums: nothing tests integration with ridges or foothills (L2) | keep; add Q6 (plateau integration, rim height variation) | **proxy** for D6 points 2–3 |
| R3 buildable land in ≥ 2 regions (contiguous patches ≥ 10 ha) | 5 A / 7 B failures with abundant land | a patch spanning regions is counted once, so continuous buildable land fails | buildable area **inside each region** (patch ∩ region), ≥ 10 ha in ≥ 2 regions | **diagnostic** — R4 carries the gameplay requirement |
| R4 settlement sites (capital + ≥ 2, ≥ 1 coastal, ≥ 3 regions) | 20 / 20 | a lower bound by greedy placement; says nothing about routes | keep; require a walking route between every pair of selected sites (T-edge) | **hard gate** (values provisional, D2) |
| R5 elevated rivers reach designated outlets | 6 A (4 vacuous) / 7 B | vacuous with no elevated source; planner rejections counted with routing failures | require **≥ 1** system with an elevated source, then every such system to a designated outlet; report planner rejections separately and count them as failures until shown to be tool defects | **hard gate** |
| R6 coast classes (≥ 3 classes ≥ 5 %, consistent with hinterland, outlets low) | 19 / 20 | consistency largely built in; blind to run length (L4) and to planform | keep the classes; add Q7 (longest run per class, class changes per km, breaks at valley mouths) | **proxy** for D6 point 6, read with the review |
| R7 hygiene (0 S8 outside declared basins, 0 climbing axes) | 10 A / 4 B | none | keep; add "every lake the analysis finds lies in a declared basin" (GEO-5) | **hard gate** |
| R8 reachability (sites, plateau tops, passes on T-edge) | 10 A / 9 B | T-edge is a conservative lower bound (jump not modelled) | keep; extend to POI-site candidates of each setting class | **hard gate** (exceptions declared, 3F-20) |

### 3.2 Proposed additions

| Id | Reading | Why | Class |
|---|---|---|---|
| R9 (new) | **routes between regions**: every settlement region joined to every other by a T-edge walking route; report detour ratio against the straight line, maximum served slope on the route and whether an alternative route exists | "useful routes between regions" is a gameplay requirement (GEO-7) that R1–R8 did not test | **hard gate** for connection; detour and alternatives **provisional** values |
| R10 (new) | **production planner validity**: `HydrologyPlanner` validation passes on the candidate lattice | L5; separates a broken surface from a routing choice | **hard gate** |
| R11 (new) | **POI-site capacity**: sites per setting class and region (brief §4.4) | D6 point 7; numerous and varied, not a quota | **diagnostic** until pacing values exist (D2) |

### 3.3 Form diagnostics for the G3c failure modes

Report-only, every value an authoring default in world units, no threshold set. They make the failures of
§1.3 visible in numbers so the review is not the only detector.

| Id | Detects | Measure (outline) |
|---|---|---|
| Q1 | planar facets (L1) | share of land steeper than 5° whose surface fits a plane within a small residual over a 30–50 m window |
| Q2 | missing concavity (L1) | river long-profile concavity: exponent θ of slope against drainage area per system (real landscapes commonly report roughly 0.3–0.7 — a calibration reference, not a target) |
| Q3 | whale-back crests (L3) | crest-normal curvature along declared and measured divides; width of the near-flat crest band |
| Q4 | uniform or single crests (L3) | separate summits, crest height coefficient of variation along the main divide, saddle depth below neighbouring culminations |
| Q5 | radial cones (L3) | circular variance of slope aspect in rings around each culmination, against a control |
| Q6 | overlay plateaus (L2) | per plateau: share of rim joined to ridge or foothill ground without an escarpment; escarpment-height variation along the rim; re-entrants (valley heads cutting the rim) |
| Q7 | over-continuous coasts (L4) | longest continuous run of each shore class as a share of the shoreline; class changes per kilometre; whether every valley mouth breaks a cliff run |
| Q8 | seed-invariant layouts (L8) | pairwise similarity between seeds of the normalised height field and of the divide tree on the same contour |
| Q9 | construction by accretion (L7) | number of named construction fixes needed in calibration, by kind |

---

## 4. Alternative landform models

### 4.1 Constraints from the project

Any family must, to be worth prototyping:

- **One terrain authority.** TerrainGen stays the authority and `TerrainData` the representation; no
  parallel generator, alternate bake, compatibility layer or node-graph terrain framework (AGENTS.md
  § Forbidden Shortcuts). A family is a candidate for *what TerrainGen computes*, reached only through
  GEO-3 (c) and an owner-approved unfreeze.
- **Representation properties.** Every production stage so far is an analytic point function, which makes
  small test regions and seams exact (research §5.2). A global lattice solve is possible on the finite
  1 025² world lattice — hydrology already does one — but it gives that property up; the owner weighs it at
  G4.
- **Determinism.** `System.Random` through `IWorldSeed.Fork` for structure; stateless `TerrainHash` per
  world cell where per-cell variation is needed; fixed iteration order in any solver.
- **Rejected constructions** (Stage 3 design §16): ridged or global periodic noise, Voronoi or Poisson ridge
  fields, radial spurs, distance-to-nearest-ridge transverse coordinates, an additive ridge layer over a
  certified envelope, erosion simulation as structure, ridges derived from flow on a perturbed base. They
  were rejected inside the Stage 2 / Stage 3 architecture; where a family below touches one, it says so.
- **Unaffected contracts:** Road Passability; the Stage 3F proposals; the 45° walking limit and its
  readings (handoff §§20–22); the height budget (700 m) and the lattice (2.734375 m — walls taller than a
  few metres cannot be vertical, research §1.4).

### 4.2 What the G3 family already tried

G3, G3b and G3c are one lineage: **geometric construction from distance fields** — distance to a
polyline skeleton (G3), a valley tree routed on the coast-distance dome (G3b), eikonal distance from a
designed drainage tree inside designed regions, with plateau overlays (G3c). The lineage produced macro
structure (G3), a lowland corridor (G3b) and lowlands, coastal sites and outlets (G3c), and failed each time
on surface form: fans, facets, terraces, whale-backs, drums. A next family must change **how the surface is
made**, not only the layout.

### 4.3 LM1 — Landform skeleton first

- **Representation and order:** an explicit tree of features with attributes — main divide with
  culmination and saddle nodes, branch ridges, spurs, valley axes as their duals, plateau rims as closed
  curves attached to ridge or spur nodes, coast segments. Order: footprint → divide tree → spurs and
  valleys by a **strip partition of each flank** (Stage 3 design §3.1, generalised from one massif to a
  branched tree) → plateau rims → coastal constraints → surface → detail.
- **Peaks and ridges:** culminations and saddles are explicit nodes with their own heights; crest
  cross-sections set sharpness; spurs dissect the flanks. Whale-backs and cones are avoided only if the
  surface between features is shaped by the partition's profiles, not by distance to the skeleton (the
  G3 failure).
- **Valleys, lowlands, plateaus, cliffs:** valleys are strips between spurs; lowlands are floors at the toe
  of the partition; a plateau rim is a skeleton curve whose segments are typed (escarpment, ramp, joint to a
  ridge), so integration is a property of the graph.
- **Plateau edges and access:** explicit — ramp segments and joints are rim attributes.
- **Drainage and outlets:** designed valleys end at designated outlets; D8 is the check. Interpolation between
  features can trap water (biharmonic overshoot) or flatten it (harmonic surfaces have no interior pits but
  give soap-film facets — L1 again).
- **Coast:** coast segments take profiles from the feature that reaches them (ridge end → headland or cliff,
  valley mouth → cove or beach).
- **Settlements and connectivity:** lowland floors and plateau tops are explicit; routes follow valleys and
  passes in the graph.
- **Failure modes:** junction artefacts in strip frames at branch roots (G3b's amphitheatres); facets between
  curves; constraint conflicts; authoring effort.
- **Complexity and fit:** high (frames on a tree, junctions). **Best fit with production**: it extends
  Stage 3's own vocabulary and can stay an analytic point function, so today's massif plus Stage 3A–3C is
  literally its special case.
- **Isolated prototype:** the generalised strip partition on a divide tree, plateau rims as graph curves,
  constrained surface; Editor-only, lattice output outside `Assets`.

### 4.4 LM2 — Region and boundary first

- **Representation and order:** a planar partition of the island into typed regions — highland, foothill,
  plateau, lowland basin, valley corridor, coastal plain — and **typed boundaries** between them
  (escarpment-barrier, escarpment-with-ramp, gradual transition, ridge line, shore type). Order: footprint →
  region graph under budgets and adjacency rules → boundary typing → region levels → boundary profiles →
  interior relief → drainage corridors from the region graph → check.
- **Peaks and ridges:** not by itself. The highland's interior needs its own model — LM1 or LM3 — or it
  reverts to a dome or a field; LM2 is a composition framework.
- **Valleys, lowlands, plateaus, cliffs:** explicit regions and boundaries; a plateau that shares a
  gradual boundary with the highland is "integrated" by construction; a river crossing an escarpment
  boundary is a waterfall site.
- **Plateau edges and access:** boundary types; access routes are boundaries of type ramp.
- **Drainage and outlets:** each lowland region names its outlet; corridors connect them.
- **Coast:** shore boundaries typed from the adjacent region, broken where a corridor meets the sea; a
  long highland or plateau front still makes a long cliff unless run lengths are bounded (G3c's L4).
- **Settlements and connectivity:** the region graph is the settlement and route graph — the strongest
  family for GEO-9's evaluation.
- **Failure modes:** a mosaic of uniform regions with visible boundary lines; T-junction artefacts where
  three regions meet; drum plateaus if the escarpment boundary dominates; template repetition.
- **Complexity and fit:** medium; regions can be polygons evaluated per point.
- **Relation to G3c:** G3c was a weak LM2 (regions, assigned outlets and coast classes) with a G3-lineage
  surface. LM2's layout layer is the part G3c did well.

### 4.5 LM3 — Hybrid landform grammar: structure fields with fluvial synthesis

- **Representation and order:** the structure is authored as **fields and constraints**, and one process
  law turns them into the surface: an **uplift field** (where land rises: the mountain system's divide,
  culminations and branches as elongated uplift, plateaus as raised blocks, low uplift in lowlands); an
  **erodibility field** (hard rims and caprock where escarpments should stand, soft ground in basins); and
  **base levels** — the sea at the shore, designated outlets for the major systems, local base levels at
  declared basin sills and plateau rim knickpoints. The surface is the **steady state of a stream-power
  incision law** on a drainage tree computed on the same lattice (in the family of Cordonnier et al. 2016;
  solver of Braun and Willett 2013; analytic steady state of Tzathas et al. 2024), with a slope cap for
  hillslopes and a low transport slope on large floors so that valleys widen into plains. Order: footprint
  → region layout (LM2's layer) → fields and base levels → steady-state solve, iterated until the drainage
  tree is stable → detail.
- **Peaks and ridges:** ridges are the divides between competing basins and culminations the maxima of
  uplift, so crests are sharp, branch and vary in height by construction; an elongated, branched uplift
  avoids the cone, dissection avoids the whale-back. This is the only family in which long profiles are
  concave and hilltops convex without being authored (L1).
- **Valleys, lowlands, plateaus, cliffs:** valleys are where drainage concentrates; lowlands are low-uplift
  regions near base level; a plateau is a raised block of low internal relief graded to its rim outlets
  behind a hard rim, so its edge is an escarpment that valleys cut back into — stepped and dissected, with
  falls at the knickpoints, as in the concept image (L2). This is the uplift-then-erosion story of
  principles §2 rule 4 and §3, which the project already wrote down.
- **Plateau edges and access:** where the hard rim is interrupted, or where a ridge meets the block, the
  ground grades up — an access route made by the same law; walls are where the rim is hard.
- **Drainage and outlets:** by construction every cell drains along the tree to a base level, so the
  surface has no accidental pit; outlets are where the base levels are put. The production planner still
  checks the result (R10).
- **Coast:** the profile emerges from uplift and erodibility near each shore segment — high hard ground
  gives cliffs, low soft ground beaches and mouths — not from a class switch (L4).
- **Settlements and connectivity:** floors, plains and plateau tops are measured, not declared; capacity
  depends on the floor and transport rules.
- **Failure modes:** a uniform "eroded everywhere" texture — D6's forbidden mountain field — unless the
  fields vary strongly by region; knife-edge ridges and threshold slopes above 45° over large areas
  (walking); D8-parallel channels on smooth slopes (G3 saw them, research §11.6); plateaus are transient in
  nature and need their explicit block and rim constraints; parameter sensitivity; iteration convergence.
- **Rejected-construction check:** Stage 3 §16 rejected "erosion simulation to carve valleys" because it
  fought Stage 2's certified envelope, and "ridges from flow on a perturbed base" because drainage came
  before structure. Here structure comes first (designed fields) and there is no envelope to fight, but the
  owner should confirm that the rationale does not carry over before production design.
- **Complexity and fit:** medium to implement, high to tune; a global lattice solve (not a point function);
  deterministic with a fixed order; runtime **[HYPOTHESIS]** seconds per world (O(n) per iteration on about
  10⁶ cells, tens of iterations), to be measured.
- **Isolated prototype:** fields from a region layout, the steady-state solver, slope cap and floor rule;
  Editor-only.

### 4.6 LM4 — Drainage graph first with landform primitives

- **Representation and order:** a hierarchical river graph grown inland from designated coastal outlets,
  a watershed partition around it, and landform primitives along its edges (valley cross-sections) and at
  its boundaries (ridges, hills), blended by a construction tree (the family of Génevaux et al. 2013).
- **Peaks and ridges:** emerge at watershed junctions; heights from upstream distance and region.
- **Plateaus, coast, settlements:** plateaus need primitives of their own; coast from the outlet layout.
- **Failure modes:** ridges on watershed boundaries are equidistance lines — the whale-back of G3c, whose
  eikonal surface was a crude member of this family; Voronoi-like cell boundaries; blending seams.
- **Complexity and fit:** high; graph plus primitive library.
- **Assessment:** the family closest to what failed. Its strong idea — outlets and the river graph first —
  survives inside LM3's base levels and LM2's outlet assignment. **Not recommended for the next step.**

### 4.7 Not candidates

| Approach | Why not |
|---|---|
| noise-based relief (ridged, fractal, periodic) | banned (Stage 3 design §16; WORLD_GENERATION_PIPELINE §3 "never one noise field") |
| full erosion simulation over time as the structure | expensive, hard to steer, and it replaces design with process; LM3 takes the steady state instead |
| example-based or learned synthesis | no reference elevation data chosen (research §7.5), no training data, and it would hide structure in a model |
| an external terrain tool or node-graph framework | forbidden: one production pipeline, no parallel generator (AGENTS.md) |

### 4.8 Comparison

| | LM1 skeleton first | LM2 region and boundary first | LM3 fields + fluvial synthesis | LM4 drainage graph + primitives |
|---|---|---|---|---|
| Primary representation | feature tree with attributes | typed region partition and boundaries | uplift, erodibility and base-level fields | river graph, watershed cells, primitives |
| Several peaks, branching ridges | explicit nodes | delegated to an inner model | emergent from uplift and drainage competition | emergent at watershed junctions |
| Avoids whale-back / cone (L1, L3) | only with partition profiles; risk of facets | no (delegated) | **yes, by the process law** | weak (equidistance ridges) |
| Integrated plateaus (L2) | rim curves in the graph | gradual boundaries | **raised blocks with hard rims, dissected edges** | needs own primitives |
| Drainage and outlets | designed, checked | outlets per region, checked | **by construction** to designated base levels | by construction |
| Coast without a cliff band (L4) | from features at the shore | typed boundaries, needs run bounds | **from local fields** | from outlets only |
| Settlement and route evaluation | via graph | **region graph is the route graph** | measured on the result | via graph |
| Main risks | junction artefacts, facets, effort | mosaic look, no mountains alone | uniform eroded texture, steep slopes, tuning | repeats G3c's failures |
| Fit with production | **best** (extends Stage 3, analytic) | good (polygons) | global solve, not a point function | moderate |
| Overlap with G3 lineage | partial (skeleton) | partial (G3c's layout) | **none** | **high** |
| Prototype cost | high | medium | medium | high |

### 4.9 Recommendation [PROPOSAL]

Prototype **two surface syntheses on one shared layout**:

- the **layout** is LM2's layer — typed regions, typed boundaries, outlets, culmination and plateau sites —
  built once per seed and identical for both syntheses, so the comparison isolates the variable G3c failed
  on, the way the surface is made;
- **synthesis S-LM3** — structure fields and the fluvial steady state — because it is the only family that
  addresses L1, L2 and L3 by its law rather than by fixes, and it has no overlap with the failed lineage;
- **synthesis S-LM1** — the generalised strip partition with plateau rims in the graph — because it is the
  best production fit and the strongest geometric alternative, so the comparison is between a
  process-constrained and an explicitly constructed surface.

LM4 is not prototyped (closest to the failed lineage); LM2's own per-region surface is not built as a third
synthesis (G3c's lineage, least new information). **No winner is chosen from these descriptions**; the choice
comes from the controlled comparison of §5 and the owner's review.

---

## 5. Proposed controlled prototype

**Provisional name: LMC** (*landform model comparison*). This is a working label, not an official experiment
ID: the G-series assigns G3d to coastline variety, and the owner names experiments. Nothing in this section
is authorised. *2026-10-23: the owner assigned the official ID **G3e**; LMC remains the working name.*

**Since 2026-10-21:** the owner approved the pair (§4.9) and step 0 is written as
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) — v0.2, the owner's revision of the agent's
draft (the draft is kept as [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md), not
approved). The note classes the readings, sets the passage rule (all hard gates on ≥ 7 of 10 held-out seeds
**and** the owner's composition acceptance on ≥ 7 of 10), uses plateau edge classes of 18–40 m and 60–120 m,
and defers exact calculations to a metrics specification the owner approves before calibration. Where it and
this section differ, the note governs once the owner approves it. This section is kept as written.

### 5.1 Question

On the baseline footprint, does a shared typed-region layout with **S-LM3** or with **S-LM1** produce the D6
geography — readable at island, regional and player scale — while meeting the hard gates, where G3c's
construction did not?

### 5.2 Fixed conditions

- **Contour A only** (seed 110201's Stage 1 coastline, the land mask of production `H4A`, hash
  `e2bad571f4af76c7`). The footprint is not a variable (GEO-1), so area and shape cannot be confounded;
  contour B is not used.
- World 2 800 m; lattice 1 025² at 2.734375 m; sea level 24 m; height budget 700 m; land written at or above
  the shore level, sea cells copied from Stage 1 (the G3c convention).
- The same layout per seed for both syntheses; the same rule values in world units; outputs outside
  `Assets`; Editor-only research code referenced by nothing; G0 and G1 unchanged; production `H4A` rebuilt
  and its pinned hash checked in every process; production files hashed before and after, as in G3c.

### 5.3 Seeds

| Role | Seeds | Rule |
|---|---|---|
| calibration | **110201, 1337** | the only seeds generated, measured or viewed before the freeze |
| held-out evaluation | **1005–1014** | the replacement set G2 pre-registered (brief §8.3) and no G3-family code has generated; not generated, measured or viewed before the freeze record exists; a construction change after the freeze voids the evaluation, which then moves to 1015–1024 |

G3c's evaluation seeds (424242, 20260905, 7, 256, 777, 1000–1004) are not reused for evaluation: their G3c
views have been seen.

### 5.4 Steps

| Step | What | Gate |
|---|---|---|
| 0 | design note: layout rules, both syntheses in principle, the v2 readings (§3), form diagnostics, review protocol and decision rule, all written before code | **owner approves the plan** |
| 1 | build the layout and both syntheses on the calibration seeds; fix named defects only, each logged (Q9) | — |
| 2 | calibration review: views and readings of both syntheses on both seeds | **owner chooses** one or both syntheses to freeze, or stops |
| 3 | freeze record: code hashes, layout and synthesis settings, every rule value | written before step 4 |
| 4 | held-out evaluation on 1005–1014; determinism repeat in a fresh process | **owner authorises** the run |
| 5 | owner's visual review of the held-out views; report | — |

### 5.5 Views and review protocol

- **Fixed cameras:** G1's V1–V6 with its contour-A anchors (G1 applies: contour A is production's 110201
  coastline), plus structure-anchored views at the same conventions — an island overview, an oblique of the
  mountain system, a regional oblique of each plateau rim, the main pass and two outlets, one shore segment of
  each class, and ground views at a settlement site and a pass. Identical camera records for both
  syntheses on a seed.
- **Contact sheets** per seed: S-LM3, S-LM1 and, for reference, production `H4A`; G3c's frozen calibration
  lattices of contour A as a historical reference only (different layout).
- **Review form** (owner): for each D6 point 1–8 and each scale, *meets / partly / fails* with a note; images
  labelled by code with the synthesis hidden, order fixed by a published seed. The agent's readings are
  reported separately from the measurements and from the owner's review.

### 5.6 Measurements

Per seed and synthesis, identically: the v2 readings (§3.1–3.2), the form diagnostics Q1–Q9 (§3.3), G0's
metrics where they apply, slope classes and steep-run heights per escarpment, T-edge connectivity, and land
use by distance band (0–60 m, 60–200 m, interior).

### 5.7 Decision rule (proposal, to be confirmed before step 4)

- A synthesis **passes** when every hard gate holds on ≥ 7 of 10 held-out seeds — the bar G3c used — and the
  owner's review accepts the composition at all three scales.
- **Between two passing syntheses:** the owner's review decides; hard-gate counts and form diagnostics inform
  it; production fit (§4.8) is weighed at G4, not here.
- **Neither passes:** record the causes by kind (construction, definition, tool, layout) as for G3c; no
  footprint, coast or production conclusion follows; the families of §4 are revisited before anything else
  is built.

### 5.8 Cost, risk and limits

- **Cost [HYPOTHESIS]:** two syntheses on a shared layout; the steady-state solve and the strip partition
  are each seconds per world; G1 rebuilding worlds dominated G3's runtime.
- **Risks:** tuning S-LM3 toward the views (mitigated by the calibration/held-out split and the defect log);
  S-LM1's junction work exceeding the prototype's scope; the layout biasing one synthesis (mitigated by
  sharing it).
- **Cannot show:** whether the footprint limits anything (GEO-1's trigger), robustness to coastline shape
  (G3d), production placement (G4), or whether walls stop a jumping player (3F-20).

---

## 6. Sequencing

| # | Step | State |
|---|---|---|
| 1 | close or classify the GEO decisions | **this document** (§2) |
| 2 | preserve the G3c results and known construction defects | research §14 unchanged; lessons §1 here |
| 3 | compare alternative model architectures | §4 |
| 4 | choose one or two promising families for isolated prototypes | **done 2026-10-21**: the owner approved S-LM3 + S-LM1 on a shared LM2 layout (§4.9) |
| 5 | validate composition and topological integrity on calibration seeds | LMC step 0 written and revised by the owner ([design note](GEOGRAPHY_LMC_DESIGN_NOTE.md) v0.2), awaiting its §12 decisions; steps 1–2 after the owner's go-ahead |
| 6 | freeze the candidate and the evaluation procedure before held-out seeds | LMC step 3 |
| 7 | independent evaluation only after the owner authorises it | LMC steps 4–5 |
| 8 | G3d — coastline-shape diversity and area against shape — only once a model is promising enough that coastline variation is the meaningful next question | after 7; not planned in detail |

Then G4 (placement, GEO-3 production half), G5 (Stage 3F on the new relief) and G6 (basins, lakes, coast)
keep their places in research §9.

---

## 7. Owner decisions remaining

1. **GEO-8:** adopt the large plateau as a region-level form outside the tier table (option (c)), or widen the
   T2 row and move T1 with it (option (b))?
2. ~~**Model research:** approve the shared-layout comparison of S-LM3 and S-LM1 (§4.9), choose a different
   pair, or a single family?~~ **Decided 2026-10-21:** S-LM3 + S-LM1 on a shared LM2 layout.
3. **LMC step 0:** ~~may the design note for the prototype be written~~ written on the owner's request
   (2026-10-21) and revised by the owner (v0.2) — [GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md);
   still open: the seven decisions of its §12 (approval, step 1, name, elevation targets, plateau classes,
   metrics specification, the S-LM3 model contract).
4. **Acceptance:** the note's gates and passage rule (§§7.1, 7.5 there) and a metrics specification with exact
   calculations, approved before calibration (its §12 item 6). R5 was extended on 2026-10-21 by the owner's
   decision: every river system rising in the mountains or on a plateau must also end at a designated outlet
   (note v0.3, agent review note 1). The specification is drafted and checked on historical G3c data
   ([GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md), 2026-10-22); its §8.1 replaces this
   review's single "G3c reference level" idea for flags (§3.3) with flags only where the check confirmed separation.
5. **LM3 and Stage 3 §16:** if LM3 is pursued, confirm that the rejection of "erosion simulation as
   structure" in Stage 3's architecture does not apply to a steady-state law over designed fields.
6. **GEO-3 at G4, later:** whether a successful model may supersede Stage 2B, 4A and 3A–3E and touch `H1`
   inside the land — only after an evaluation passes.
7. **GEO-4 production contract, later:** profile behind a fixed shoreline (b) or a shoreline that may move
   (c) — after G3d.
8. **D2, D3:** settlement size classes, slope and spacing, terraced towns; travel pacing — still provisional.
9. **D9 housekeeping:** whether the uncommitted G3–G3c research files stay, and whether this documentation is
   committed.

---

## 8. Verification and references

**Verification.** No generation, calibration or evaluation ran for this review; no Unity process was started.
Production code, `TerrainData`, scenes, materials, shaders and assets were not touched; G3c's thresholds,
tables and outputs are unchanged; no research artefact was deleted; nothing was committed or pushed. The
documents changed with this review are listed in research §15.

**Literature** (reading for the families of §4; none is evidence that a family fits this island):

- Génevaux, Galin, Guérin, Peytavie, Beneš — *Terrain Generation Using Procedural Models Based on
  Hydrology*, ACM Transactions on Graphics 32(4), 2013 (LM4; already a source of principles §3).
- Cordonnier, Braun, Cani, Beneš, Galin, Peytavie, Guérin — *Large Scale Terrain Generation from Tectonic
  Uplift and Fluvial Erosion*, Computer Graphics Forum 35(2), 2016 (LM3; already a source of principles §3).
- Braun, Willett — *A very efficient O(n), implicit and parallel method to solve the stream power equation
  governing fluvial incision and landscape evolution*, Geomorphology 180–181, 2013 (LM3 solver).
- Tzathas et al. — *Physically-based analytical erosion for fast terrain generation*, Computer Graphics
  Forum 43(2), 2024 (LM3 steady state).
- Hnaidi, Guérin, Akkouche, Peytavie, Galin — *Feature based terrain generation using diffusion equation*,
  Computer Graphics Forum 29(7), 2010 (LM1 constrained surface).
- Galin et al. — *A Review of Digital Terrain Modeling*, Computer Graphics Forum 38(2), 2019 (survey).
