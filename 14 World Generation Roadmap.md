# 14 World Generation Roadmap

Version: 0.34

Status: Draft

Last Updated: 2026-10-25 (G3f: the owner's decisions D-3 … D-12 on content recorded in the phase plan v0.3; none authorises execution. Earlier: G3f, regional geography architecture: the phase plan approved — D-1, D-2 — and recorded; only step A authorised. 2026-10-24: G3e-PM ran: H FAIL; the S-LM1 line stopped. G3e-PM post-mortem checks pre-registered, plan approved, not run. G3e Step 1 ended not ready; report accepted, Step 1 closed. 2026-10-23: LMC named G3e; C8, C9 decided. LMC specification v0.4 approved; earlier the same day C2–C5 prepared. 2026-10-22: LMC Step 1 readiness check; design note approved as the research plan. Earlier: LMC technical specification approved; Step 1 not authorised. Earlier: checked on G3c data. 2026-10-21: LMC design note v0.2, the owner's revision; post-G3c review: GEO decisions, model research plan; G3c results; G2 review)

Depends On:
[11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)

Referenced By:
[AGENTS.md](AGENTS.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), Unity plans under `unity/Assets/Plans/`

---

# Purpose

Handoff document for **future agents and humans**: where world generation stopped, what is authoritative after the Architecture Pivot, and the **ordered roadmap** toward a seeded Valheim-shaped overworld with separate interiors and Combat Arenas.

Chat is not the source of truth — update this file when a milestone lands.

Success question for agents:

> Can a new agent open this doc and know exactly what to implement next without continuing the pre-pivot continuous OrganicTown path?

**Presentation / scene authority:** [16 Architecture Pivot](16%20Architecture%20Pivot.md).  
**Systems map / iteration backlog:** [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).

---

# Where We Stopped (2026-08-07)

## Architecture Pivot ✅ (docs)

Locked in [16](16%20Architecture%20Pivot.md):

- seeded Valheim-like overworld (scoped; no harvest loop);
- free third-person locomotion (Genshin-like feel);
- interiors as separate free-move scenes;
- Confront → thematic Combat Arena (grid TB only there).

## Game Systems Architecture ✅ (docs)

[17](17%20Game%20Systems%20Architecture.md) v0.1 records System Cards, dependency DAG, and micro-iterations (Track W = world gen).  
Overworld V0 code micro-plan: `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`.  
Full target shape for Track W beyond V0 (world layout → macro terrain → hydrology → biomes → materials → vegetation → roads → settlements → streaming, plus the 12-iteration build order): `unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md`. Not a build order override — `OVERWORLD_V0_PLAN.md` and this file's roadmap table below still govern what ships next.

Code under `unity/` is still **pre-pivot prototype archive** until Overworld V0 iterations land.

## Pre-pivot prototype archive (deprecated — not a reuse source)

| Track | Deliverable | Notes |
|-------|-------------|-------|
| OrganicTown v2 A–D2 + P0 | Vector town, greybox, templates, occlusion, radius GridMap | Continuous enterable city — deprecated, not viable, not reused |
| v1 raster city | `CityGenerator` playable Lower Veil | Deprecated, historical reference only ([13](13%20City%20Generation.md)) |

Detail plans remain under `unity/Assets/Plans/` with **Superseded** banners. Neither track feeds the new Piece-Based Assembly pipeline in [15](15%20Procedural%20Building%20Generation.md).

---

# Locked Architecture (post-pivot — do not silently reverse)

1. **Overworld** is the authoritative exploration space: world seed → biomes / zones / POIs / creature spawns ([16](16%20Architecture%20Pivot.md)).
2. **Gameplay movement** on overworld and interiors is **free third-person**, not GridMap.
3. **Interiors** are **separate scenes** (free move, no grid).
4. **Confront** always uses a **Combat Arena** scene; grid + Turn Action Economy apply only there ([10](10%20Turn%20Action%20Economy.md), [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).
5. **“Valheim-level”** means deterministic seeded world + biome diversity + streamed/chunked intent — **not** survival harvest, not AAA biomes art in V0.
6. Pre-pivot OrganicTown continuous fabric is **deprecated** — not the next implementation default, and not a reuse source for the new pipeline ([13](13%20City%20Generation.md)).
7. **Piece-Based Assembly, small → large.** World, settlement, and building generation compose from small hand-authored Pieces with Snap Sockets (Valheim-style), never from a whole hand-tuned continuous mesh or an algorithmic subdivision solver. Detail: [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).
8. Implement **one micro-plan at a time** from the AI backlog in [16](16%20Architecture%20Pivot.md).

---

# Roadmap (mandatory order — post-pivot)

```text
Architecture Pivot docs ✅
    → Overworld V0
        → Locomotion V0
            → Interior V0
                → Arena V0
                    → Theme Packs V0
                        → Vertical Slice content on new rails
                            → richer biomes / interior DNA / monsters
```

| Phase | Goal | Outcome | Depends on |
|-------|------|---------|------------|
| **Pivot docs** ✅ | Spatial split locked | [16](16%20Architecture%20Pivot.md) + related doc updates | — |
| **Overworld V0** | Seeded stub world | Biome bands + terrain + 3–5 POI markers + spawn points; iterations **W0.1–W0.9** in [17](17%20Game%20Systems%20Architecture.md) / `OVERWORLD_V0_PLAN.md` | Pivot + systems map |
| **Locomotion V0** | Free 3P feel | Controller + follow/orbit camera | Overworld V0 |
| **Interior V0** | Scene transition | Door → interior → exit; free move | Locomotion V0 |
| **Arena V0** | Confront presentation | Theme arena + TB skeleton + return + residue stub | Interior V0 |
| **Theme Packs V0** | Arena variety | Street / tavern / woods greybox kits | Arena V0 |
| **Slice content** | Lower Veil fantasy | POIs / tips / Decision Nodes on new rails | Theme Packs V0 |

Each phase = its own plan + commit series. Prefer updating this file’s “Where We Stopped” when a phase completes.

---

# Island Shaping Rebuild (superseded)

The directional-banding investigation belonged to the previous external terrain pipeline's island
shaping stage. That stage no longer exists: the overworld relief is TerrainGen's authoritative field,
written straight into Unity `TerrainData`
(`unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`). Whether the TerrainGen relief has banding
of its own has not been measured; judge it on a bake, not by analogy with the removed pipeline.

---

# Island Relief — TerrainGen Stage 4A (the terrain)

**What replaced the shaping stage.** Measuring the baked island showed the flaw the old recipe had hidden
with noise: the relief was a function of coast distance. 95–99 % of the interior had its height gradient
aligned with the coast normal, and the 70–110 m band — the whole middle of the island — held 0.1–7.8 % of
the land. TerrainGen Stage 4A adds the missing floor relief from structures that already exist: a 1-D
coastal floor `M(d)`, macro-plate districts (uplands and lowlands, with a 20 % lowland reservation),
knolls on the raised districts from crustal cell cores (slope-budget capped; none placed on the
canonical seeds), and an apron at the foot of the Stage 2B massif, domain-limited to the axis reach.
Every term is gated so the land mask cannot move.

**Status 2026-09-18.** Revision R1 is **the production terrain authority**: `TerrainAuthorityField`
evaluates `IslandElevation` (`H4A = H1 + U2 + R4A`) at `GeneratorVersion` 2. `TerrainGenStage4ATests`
16/16, full EditMode 572/572, canonical S8-A/S8-M/S8-C/S10/S11/S12 green, sweep 1000–1039 S8-A 0/40 and
S8-M 6/40. The production field reproduces the dump-first `H4A` hash exactly on all seven canonical
seeds, which is what closed the question of whether Stage 2A's plates and Stage 2B's are the same
plates. Residuals (zero knolls, apron ring/zone, coast-aligned gradient, curvature ratio, massif-foot
pits) are in `unity/Assets/Plans/OVERWORLD_TECH_DEBT.md` item 19, owned by Stage 3F / Stage 4B. Living
contract: `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md`.

**Next:** Stage 3F (the structural valley carve, `H = H4A − C3F`), then Stage 4B/4C. Stage 3D/3E keep
declaring their basins and outlets on `StructuralElevation` (`H2`) — the surface they were frozen
against — so Stage 3F is what reconciles the two levels.

---

# Geography-first research (2026-10-18) — proposals, nothing approved

The baked island reads as a flat island with one convex elevation, and that is what the production
composition produces: `H1` is a function of coast distance only (a 24 → ~73 m dome), `U2` is at most one
unbranched massif (none on 4 of 29 audited worlds), and Stage 4A adds bounded floor relief with no
ridge, valley or second high region. Ridges, valleys and basins exist only as Stage 3 structure or in the
experimental Stage 3F carve. Measured on seed 110201: land p50 58 m, p90 108 m, ~10 % of land above
110 m; ~90 % of the bake time is TerrainGen evaluation, so a neutral review material is a legibility
change, not a speed change.

Research record, target geography, stage responsibility map, acceptance suite and experiment roadmap:
`unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md`. Owner questions: [99 Open Questions](99%20Open%20Questions.md)
§ Terrain — Geography-first world shape (GEO-1 … GEO-8).

What this changes in the order above: **nothing yet.** The research concludes that most of the missing
geography (a mountain system, lowland trunk valleys, relief on massif-free worlds, cliff coasts) is
upstream of Stage 3F, which carves drainage into a coherent relief but cannot create one. The next steps
it proposes are read-only and isolated — a geography metrics baseline (G0) and a heights-only
Geometry-First Review tool (G1). The owner approved both (GEO-6 (a), 2026-10-18) and both are built as
Editor-only tooling; neither changes the order above. Their baseline (research record §7.6) is the
evidence for the owner's review of the target (G2). Stage 3F research
continues on the parts that survive any upstream decision (research record §6.3).

**G3, 2026-10-19 — research, commissioned by the owner before GEO-2 was answered** (research record §11).
One candidate tested the hypothesis "a connected branched mountain system, designed with its valleys,
fits the current island": a skeleton built from Stage 1's coast geometry (spine, two branches, spurs)
replaces the Stage 2B massif and its apron, valleys are carved between the spurs to a monotone floor, and
everything is an uncommitted Editor-only research dump measured with G0 and G1 against production `H4A`.
On all seven canonical seeds, the three massif-free ones included: two or three culminations, one or two
saddles, single-dome flag 5/7 → 0/7, land above 110 m 22.6–29.7 %, no depression, land mask and ocean
unchanged, deterministic across processes. Not achieved: a drainage network (Strahler ≤ 2, as before), a
lowland corridor, and flat land (12–16 % below 5°, against 25–54 % today). Production, the bake, the
scene, frozen stages and every pinned hash are unchanged. **Proposed next (isolated, needs no owner
decision): G3b**, a converging valley network on the same skeleton; GEO-1 … GEO-5 and GEO-7 stay open,
and G3 approves no architecture.

**G3b, 2026-10-19 … 20 — research** (research record §12): a converging valley tree on the G3 skeleton gives
every seed a lowland corridor and doubles tributaries, but Strahler stays 2 on all seven, land below 5° is
15–17 % and 12 shallow S8 depressions appear on four seeds; ~~the limit is the footprint and the coast gate
(GEO-1, GEO-4), not valley topology~~ **[corrected 2026-10-20]** the measured limits are the coastal
transition (the gated coast-distance ramp) and the construction; the footprint was not varied and is a
hypothesis (research record §13.2). The next step was the owner's review (G2), not another candidate.
Production and every pinned hash are unchanged.

**G2, 2026-10-20 — owner review** ([GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md)).
The owner approved the **target geography D6** from a concept image
([`References/G2_target_island_concept_2026-10-20.webp`](References/G2_target_island_concept_2026-10-20.webp) — a
project reference inspired by DragonSword: Awakening's visual direction, not an official map): a regional
geography with a dominant branching mountain system, several plateaus integrated into ridges and
foothills, barriers and routes designed together, deliberate valleys, lowlands and plains, rivers to
designated outlets, coasts caused by their hinterland, room for 3–5 settlements and many POIs, judged at
island, regional and player scale. A G2 diagnostic found **no coastal settlement site on any surface
measured, production `H4A` included** (brief §4.2). **Next: G3c** — research only, region-first, on two
footprints (today's, and about 40 % more land in the same 2.8 km world), each with a fixed shoreline and a
coastal profile that varies by segment; method, parameters, metrics and the comparison rule are fixed in
brief §8.3 before any run, and ~~**the run awaits the owner's go-ahead**~~ the owner gave the go-ahead the same day (G3c ran, next paragraph). G3c compares two specific
contours; coastline variety and the separation of area from shape are a later stage (G3d). What this changes in the order
above: still nothing — production stays `H4A`, and the placement decision (G4, GEO-3) comes after G3c.

**G3c, 2026-10-20 … 21 — ran on the owner's go-ahead** (research record
[§14](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md); Editor-only, uncommitted research code). A
region-first construction — regions on the fixed land mask, a drainage tree grown from designated outlets,
a surface rising from rivers and shore, a highland envelope, composed plateaus, a per-segment coastal
profile — was calibrated on seeds 110201 and 1337, frozen, and evaluated on ten held-out seeds over
contour A (today's, 2.83 km²) and contour B (3.82 km², +35 %, also a different shape). **Neither contour
passed** the pre-registered rule: A met every reading R1–R8 on 2 of 10 seeds, B on none; B added 31–43 %
buildable lowland but with worse hygiene, so it is not a material improvement. By the rule, **the world
is not grown on this evidence.** What held on all twenty lattices: a capital plus four further settlement
sites, at least one coastal — against none on any surface in G2. What failed: R3 (merged buildable
patches), R5 (the hydrology planner's own validation), R7 on B (climbing river axes), R1 (a single
summit); visually, whale-back ridges on A, drum-like plateaus and planar facets on both. **Next: the
owner's review of G3c** (research record §14.11) before any further experiment. What this changes in the
order above: nothing — production stays `H4A`, and every pinned hash is unchanged.

**Post-G3c review, 2026-10-21** ([GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md);
documentation only). G3c is recorded as completed research that failed acceptance — not proof that
region-first geography is invalid, not evidence that land area limits anything. Its lessons: distance-field
surfaces give facets and whale-backs, overlay plateaus read as drums, class-switched coasts give a continuous
cliff band. Decisions, with statuses: **GEO-1 resolved** (owner direction) — no resizing on this evidence,
today's footprint is the baseline for further research, a revisit trigger is recorded, a 3 × 3 world stays a
travel-pacing decision; **GEO-2 resolved** — one branching mountain system counts as one T1; GEO-3 research
continues under option (c), production placement still at G4; GEO-4 — varied, hinterland-caused coasts are
the target, the production contract is open; GEO-5 — lakes only in declared basins with one outlet; GEO-7 —
hard gates are gameplay and topological requirements, composition is judged in the owner's review,
morphology metrics are diagnostics; GEO-8 — a candidate is never rejected only for exceeding the T2 row,
tier labels never decide traversal, and a region-level **Large Plateau** form is proposed; **GEO-9** added
(geography provides regions and candidate areas; placement chooses sites and never reshapes terrain to meet
a count). **Next, awaiting the owner:** an isolated comparison of two landform syntheses — structure fields
with a fluvial steady state, and a generalised skeleton strip partition — on one shared typed-region layout,
contour A only (provisional label *LMC*). G3d comes after a promising model. What this changes in the order
above: nothing — production stays `H4A`.

**LMC step 0, 2026-10-21.** The owner approved the pair **S-LM3 + S-LM1 on one shared LM2 region layout**,
and the comparison's design note is written
([GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md)), and the owner revised it
the same day (v0.2; v0.3 extends R5 to designated outlets on the owner's decision; the note governs): contour A only; calibration 110201 and 1337, held-out 1005–1014; one LM2
layout per seed shared by both syntheses; hard gates R4, R5, R7, R8, R9, R10; indirect indicators R1, R2, R6;
Q1–Q9 for the G3c failure modes; a blinded review at three scales; a method passes only with all hard gates and
the owner's composition acceptance each on ≥ 7 of 10 held-out seeds. Seven explicit owner decisions (its §12)
come before any code, including a metrics specification and the S-LM3 model contract. Production stays `H4A`.

**LMC technical specification, 2026-10-22.** The owner supports the design note as the research direction and has not
authorised code or calibration. The specification
([GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md)) fixes the elevation definitions
(world-height datum), the S-LM3 model contract, the S-LM1 ownership rule, the gate and Q1–Q9 calculations and their
review-flag levels, and was checked on G3c's twenty historical lattices and on analytic surfaces with read-only
scripts — no generation. 10 of 13 pre-registered checks held; three failed expectations led to corrections of the
plateau edge height, the route-detour metric and the flag policy (K1–K3). ~~Next: the owner's approval of the
specification, then the Step 1 go-ahead as a separate decision.~~ **Approved by the owner the same day** (§12 items
1–4: world-height datum, K1, the S-LM3 contract as an experimental model, gates with K2, the flag levels, layout
validation, the S-LM1 ownership rule). Next: the Step 1 go-ahead, still not given. Production stays `H4A`.

**LMC Step 1 readiness, 2026-10-22.** The owner approved the design note (v0.5) as the research plan, kept Step 1 as a
separate stage that needs explicit authorisation, and kept LMC as the working name (the official ID is chosen before
implementation). The readiness check
([GEOGRAPHY_LMC_STEP1_READINESS.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md)) found four items to settle
first — R8 covers only verified plateau tops where the plan asks for every designated top; no verified detector for
the whale-back; no diagnostic for slope clustering at a cap; no reference values yet on G3c's calibration grids for
the continuity check — and lists eight conditions (C1–C8) for the authorisation. Production stays `H4A`.
*2026-10-23:* conditions C2–C5 prepared with read-only checks on existing G3c data — amendments A1 (R8) and A3 (slope
diagnostics) proposed for approval, the whale-back measure failed its check (option (b), judging it in the review, is
recommended), the calibration-grid reference recorded; a ninth condition (C9) asks where the reference data is kept.
The owner then approved A1 and A3 and chose option (b) — the specification is v0.4, and the whale-back is judged in the
visual review. ~~Open before Step 1: the official ID (C1), the commit policy (C8) and the reference data (C9).~~ Decided the
same day: the experiment is **G3e** (working name LMC); its code is not committed during Step 1 (the commit question
returns before the freeze); the reference data is kept outside the repository at
`D:\Hobby\game-dev\research-data\lmc-reference\`. ~~Only the explicit Step 1 authorisation remains.~~ Step 1 was authorised on 2026-10-23 and **ended not ready** on 2026-10-24 after eleven calibration runs: no synthesis passes every hard gate on the calibration seeds, so there is no freeze and no held-out evaluation ([GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md)). The owner accepted the report and closed Step 1. Production terrain is unaffected. *2026-10-24:* the owner approved the plan of the post-mortem checks **G3e-PM** — a first look at S-LM1 at three scales and read-only checks of who owns each remaining failure — pre-registered in [GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](unity/Assets/Plans/GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md); it builds nothing and runs only on the owner's separate authorisation. Its outcome decides whether the S-LM1 line continues with an exact package, needs contract decisions first, or stops. *Outcome, the same day* (§13 there): the registered D1 result is INCONCLUSIVE, H is FAIL by §7 (accepted by the owner), and a one-run deviation (DEV-1) completed the list of contract owners; the owner kept S-LM1's contracts, so **the current S-LM1 line stops**. Next, only with its own plan: revisit the landform families or a changed S-LM3 law; G3d waits. Production terrain is unaffected.

**G3f — regional geography architecture, 2026-10-25** ([GEOGRAPHY_G3F_PHASE_PLAN.md](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md)).
The owner approved the phase (D-1; official ID G3f, working name RGA) and its architecture **as the hypothesis to test**
(D-2): a seeded landform grammar is instantiated into one authoritative **Geography Plan** in which every land vertex has
exactly one height owner — a declared drainage unit whose boundaries are divides — and every landform reserves everything
it writes; one drainage-consistent surface synthesis reads the plan; regions divide the problem, tiles only the
representation. It is to be tested first in a bounded regional prototype on contour A at production resolution — a
control surface, a unified synthesis and a per-unit synthesis under identical conditions, with calibration, a freeze and
held-out variants — then an architecture decision and, only if one is adopted, a production implementation plan as the
input to G4. Decisions D-3 … D-12 (among them whether a drainage-tree law of the S-LM3 family may be the candidate law)
are open, and only step A — the plan and these pointers — is authorised: no code, generation, Unity run or read-only
check. What this changes in the order above: nothing; production stays `H4A`, G3d waits. *Later the same day* the owner
decided D-3 … D-12 on content (the plan's decision record, v0.3): a changed fluvial law of the S-LM3 family may be
described in WP1 as a new research line — the closed S-LM3 configuration is not resumed; the candidates are C0 and U-F,
with R-F only after U-F's analytic checks; the domain is contour A with a 513 × 513-vertex window; the criteria for WP1
include a split flow check, targets as diagnostics and a reachability-share gate; commits go to `main`; a global lattice
solve is allowed for research only; the new wording of law 11 is accepted, its documentation package not yet authorised
and the W.2 line deferred; the review is blind; the budgets are diagnostics; the stale-line package is accepted without
code changes. These are decisions on content: WP0, WP1, code, Unity, generation and the two documentation packages each
still need their own authorisation.

---

# Hydrology / River Generation

**Designed and built 2026-09-03; the carve now lands directly in the terrain lattice.** Full design:
`unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md`.

The drainage analysis ships and is measured: rivers with tributaries, terrain-derived lakes,
waterfall candidates, water meshes and an `IWaterSampler` seam for roads. It runs as the `Hydrology`
pipeline step between Mountains and Markers, as a **two-pass** bake — the analysis reads the finished
relief off `TerrainData`, and the second terrain bake subtracts the planned carve from the terrain
lattice before the surface and vegetation are derived. TerrainGen never reads the carve back.

**`RunHydrology` is off by default.** The earlier blocker — the carve not landing — belonged to the
removed external terrain pipeline; the direct lattice carve replaces it and is verified on seed 110201
(delivered cut within 0.02 m of the plan, nothing outside it moved by more than 0.02 m). A carved world
has not yet been reviewed visually, so turning the step on by default is a separate decision.

The constraints below were the brief the design had to satisfy, and all of them held; they are kept
because any future change to hydrology still has to satisfy them.

## The previous attempts were discarded, not archived

Several inland-hydrology implementations were built and removed: lake site selection and basin
digging, river tracing and spline/ribbon meshes, per-lake water objects, `WaterZone_*` scene
hierarchies, and the settings that fed them. They repeatedly produced architectural complexity,
hidden dependencies, duplicated systems and bugs, and they made terrain generation harder to change.

They are **not an architectural foundation**. Do not resurrect `LakeDefinition`, `RiverTracer`, the
water-zone menu architecture, or any of the deleted constants because they look convenient. Starting
from that code is what made every previous attempt fail the same way.

## What the system models

Worked out from first principles, along this chain:

```text
source → flow direction → drainage basin → terrain valley → river channel
       → lake / depression relationship → water surface
       → downstream terrain and ecology consequences
       → roads, bridges, crossings → POI placement
```

The quality bar is the one already stated in `WORLD_GENERATION_PIPELINE.md`: **rivers reshape
terrain**. Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified
terrain is not hydrology.

## Constraints any design must respect

- **TerrainGen owns terrain relief; the terrain bake owns the carve.** A river lowers the terrain
  lattice inside the bake (`HydrologyBake.ApplyPendingCarve`), after TerrainGen and before the surface
  and vegetation are derived — never as a pass over finished `TerrainData`, and never fed back into
  TerrainGen.
- **The heightmap resolves 2.73 m per vertex** (513 over 1400 m). A channel narrower than that cannot
  be represented as terrain height — that constraint has to be part of the design.
- Determinism: same seed → same watercourses, and re-seeding hydrology must not move the mountains.
- Roads already plan around water through `SeaLevelOceanSampler`; any inland water must enter road
  cost and bridging through that same interface rather than a parallel one.

Written before the code: `unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md`.

## What is still out

Riverbed material and clearing vegetation from the channel are V2 — the carve only lowers the
lattice, so surface and vegetation rules treat a channel like any other low ground.
Waterfall **rendering** is out; only the candidates are derived. Bridges, moisture and
distance-to-water remain future consumers.

---

# Superseded pre-pivot order (archive)

```text
A → B → D1 → D2 → P0 → C1 continuous narrative → C2 full GridMap → E wilderness
```

Do **not** resume this order unless the user explicitly overrides [16](16%20Architecture%20Pivot.md).

---

# Agent Instructions (quick)

1. Read [16 Architecture Pivot](16%20Architecture%20Pivot.md), then [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), then this file.
2. **Next implementation default:** `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` (W0.1→W0.9). Do not extend OrganicTown C1 continuous path.
3. Keep style: no inheritance tricks; deterministic `WorldSeed` forks; human-scale metres; chunk **64 m**; playable overworld **2800 × 2800 m**.
4. Confirm design forks with the user before changing locked architecture above.
5. After a documentation or milestone step: commit when the user asks (human steers git).

---

# Explicitly Out (until later phases)

- Riverbed material, vegetation clearing in the channel, and waterfall rendering
  (see § Hydrology / River Generation)
- Survival harvest / base-building loop
- Full Valheim-scale map
- Marketplace tactics kit as architecture
- Continuous enterable city GridMap as world truth
- Voluntary Turn-Based Mode during exploration
- Full World Engine Influence sim
- Multi-city overworld map
- Shipping deep combat AI as part of Overworld V0

---

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md) — spatial / presentation authority
- [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) — systems map + Track W iterations
- [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) — exterior shells + interior scenes
- [13 City Generation](13%20City%20Generation.md) — deprecated, historical only
- [12 Engine Choice](12%20Engine%20Choice.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- Unity plans: `OVERWORLD_V0_PLAN.md`, `WORLD_GENERATION_PIPELINE.md`, archive under `unity/Assets/Plans/`
