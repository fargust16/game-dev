# G3f — Regional Geography Architecture: Phase Proposal

Version: 0.2

Status: Draft — **phase plan. The owner approved the phase (D-1) and its architecture hypothesis (D-2) on 2026-10-25,
and authorised step A of §14.1** (this document in the repository, pointers in 14, 99 and AGENTS.md). Decisions D-3 …
D-12 and steps B and C of §14.1 are **open**: no code, no terrain generation, no Unity run and no read-only check is
authorised. The text of v0.1 is unchanged except where the decision record below and §§12–13 say so.

Last Updated: 2026-10-25 (v0.2: the owner's decisions D-1 and D-2 and the step-A authorisation recorded; §12 adjusted to
them; GEO-11 widened to law 11. v0.1, 2026-10-24: the proposal)

Depends On:
[AGENTS.md](../../../AGENTS.md), [000 Project Rules](../../../000%20Project%20Rules.md),
[00.2 Meta Loop](../../../00.2%20Meta%20Loop.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[16 Architecture Pivot](../../../16%20Architecture%20Pivot.md),
[17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) (laws 4, 5, 8, 11; W.2–W.5),
[99 Open Questions](../../../99%20Open%20Questions.md) (GEO-1 … GEO-9, 3F-18, 3F-20),
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (D6, §§3–4),
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) (§§1.3, 2, 3, 4),
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (§§1–2, 4–9, 13–14),
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) (§§3, 5, 11, 17, 20–23),
[GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md) (incl. addendum §9),
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md) (§§6.1, 8, 13),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§1–7, 9),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md), [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md), [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) (items 2, 16, 20, 22);
reference image [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)

Referenced By:
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[99 Open Questions](../../../99%20Open%20Questions.md) (GEO-3, GEO-10, GEO-11), [AGENTS.md](../../../AGENTS.md),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §9

---

## Decision record

**The owner's words, 2026-10-25 (verbatim):**

> Одобряю фазу G3f (D-1, D-2) и шаг A из §14.1: внеси план в репозиторий с записью моих решений, обнови 14, 99,
> AGENTS, закоммить и запушь. Решения D-3…D-12 сообщу отдельно.

| Item | State |
|---|---|
| D-1 the phase | **approved** — official ID **G3f**, working name RGA; scope §1, gates §10.1 |
| D-2 the architecture hypothesis | **approved as the hypothesis to test** (§4) — not as an architecture decision; that is G-6 |
| D-3 … D-12 | **open** — the owner will answer them separately |
| D-13 the first authorised step | **step A authorised** (§14.1); steps B (WP0) and C (WP1) not yet |
| Gates (§10.1) | G-1 and G-2 passed for the phase and the hypothesis; the candidate set and D-3 (part of G-2) stay open; G-3 … G-7 not reached |

Nothing in this record authorises code, terrain generation, a Unity run, a read-only check of preserved data or any
production change. The terms of §0 stay proposed; they enter the Glossary only at G-3 (§12).

---

## 0. How to read this

**Tags.** **[FACT]** read from code or a document on `main` at `fae8f28`, source named; **[MEASURED]** a recorded
measurement, source named; **[VISUAL]** the owner's recorded review; **[HYPOTHESIS]** believed, with the check that
settles it; **[PROPOSAL]** this document's recommendation; **[AUTHORING]** an authoring default in world units, to
approve; **[CARRIED]** taken unchanged from an approved G3c/G3e definition so results stay comparable.

**Proposed terms** (Glossary entries only after approval, §12). *Geography Plan* — the one authoritative structural
record of a world's geography (§5.1). *Drainage unit* — a declared catchment with exactly one outlet; the only owner of
the heights of its vertices. *Relief region* — the character of the ground at a vertex (mountain, upland, plateau top,
…), the existing word of the Geographic Hierarchy and of LM2's region codes; regions parameterise the surface law and
never write heights. *Boundary contract* — the height, slope and flow rule on a typed boundary (§5.2). *Write
footprint* — every vertex a landform may influence, reserved in the plan. *Stress fixture* — an authored regional plan
built to contain the configurations that failed before. *Sector* — the part of the island a regional prototype
synthesises. A Unity terrain **tile** is a representation unit and never a geographic one.

**What this document does not do.** It changes no file, runs nothing, reads no held-out seed, adopts no research model,
changes no frozen metric, threshold or acceptance definition, resumes no stopped line, and decides no Open Question.

---

## 1. Phase name, objective and scope

### 1.1 Name

Proposed official ID **G3f** — the next free letter in the G3 line of upstream-relief candidates (G3d stays reserved for
coastline variety; the rule recorded in the LMC design note, agent note 11). Working name **RGA — Regional Geography
Architecture**. The owner assigns the ID.

### 1.2 Objective

Decide on evidence **how the island's geography is owned and synthesised**, and finish with either a production
implementation plan (the input to G4, GEO-3's production half) or a documented stop. Three questions:

1. **Ownership.** Can one authoritative record — a Geography Plan in which every land vertex has exactly one height owner
   and every landform reserves everything it writes — remove the class of defects that dominated G3c and G3e (raised
   drainage, plateau dams, ramps into sea cliffs, coastal capture of highland drainage, climbing valley axes)?
2. **Surface.** Does a drainage-consistent surface synthesis driven by that plan produce the D6 geography, readable at
   regional and player scale, walkable where intended, deterministic and affordable?
3. **Assembly.** Can the surface be synthesised **per drainage unit and merged exactly** (regional assembly), or must it
   be one unified solve? Regions are tested as a way to divide the *problem*; tiles remain the way the *representation*
   is divided, and the two are never confused.

### 1.3 Scope

**In:** the Geography Plan data model and its validator; one authored stress fixture on contour A with seeded variants;
a regional measurement harness proven against preserved G3e data; a plan-anchored view kit; a control surface and two
candidate syntheses under identical conditions; calibration, freeze and held-out evaluation of the regional prototype;
an architecture decision memo; a production implementation plan if an architecture is adopted.

**Out:** production code or data of any kind; a full-island candidate (it belongs to the production plan's first
stage, §7 WP11); held-out seeds 1005–1024 (kept untouched for that later island-scale evaluation); G3d, a larger world
(GEO-1), lakes (GEO-5 realisation), Stage 3F, the hydrology carve, roads, POI and settlement placement, materials and
vegetation, the tier table (GEO-8), the jump question (3F-20), technical-debt items not needed by the prototype.

### 1.4 Design filter (AGENTS.md) and incremental versions (000)

- *Curiosity:* a readable landform grammar — visible culminations, passes between them, plateau tops whose access must be
  found — poses travel questions the player answers by reading the land.
- *Teaching:* geography is caused (rivers end where outlets are, coasts follow what stands behind them); reading it is
  Knowledge that transfers between worlds while the answers change ([00.2 Meta Loop](../../../00.2%20Meta%20Loop.md): a new
  procedural world has different geography; methods transfer, answers are new).
- *Cost:* terrain grants no power; barriers and routes make travel choices with real costs.
- **V1** (this phase and its production plan): fixed relief-region types, one surface law, the validator, seeded plan
  instantiation. **V2:** declared basins and lakes (GEO-5 (b)), coastal planform (G3d), a small archetype library
  (research §4.3 rule 10). **V3:** coupling to biomes and world history (e.g. landslides closing routes).

---

## 2. Evidence supporting the proposed approach

### 2.1 The audit reviewed

The audit of 2026-10-24 — the G3e post-mortem's root-cause table and its documentation audit — is recorded in the
repository through its consequences: the not-ready report's addendum §9, the pre-registration and §13 of
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md), and the status corrections of commits `c3c7c3c`
and `fae8f28`. Its critical findings were re-checked on `main` for this proposal, and four new findings were made
(rows 6, 7, 8, 10).

### 2.2 Critical findings verified on `main`

| # | Finding | How verified | Result |
|---|---|---|---|
| 1 | Production terrain is `H4A` through `TerrainAuthorityField`; no research code is on the path | `World/Terrain/TerrainAuthorityField.cs:92–93` (`HeightAt` → `IslandElevation.At`); `Editor/OverworldTerrainBake.cs:78–83` (authority → `FromAuthority` → `ApplyPendingCarve`); `TerrainGenLmc*` and `TerrainGenG3c*` are referenced only by their own files | holds |
| 2 | G3e Step 1 closed not ready; S-LM3 closed with a negative result; the S-LM1 line stopped; held-out seeds never generated | not-ready report v0.3 §§1, 8, 9; G3e-PM §13.6; AGENTS.md v0.77 | holds |
| 3 | S-LM1 is a chain of stages joined by overlays, not the single-owner partition the specification's §6 describes | `Editor/TerrainGenLmcSkeleton.cs` `Build`, lines 127–223 (base → mountain → pass corridors → plateaus → running minimum); plateau writes `z[i] = Math.Max(z[i], …)` at lines 938, 949, 954 | holds |
| 4 | S-LM1's base lets the coastal surface decide heights beyond the coastal band (Q-O1-a answered *No*) | `TerrainGenLmcSkeleton.cs:368–374`: `zC = BaseLevel + max(0, sd)·t`, `B = min(zE, zC)` over all land, no band width | holds |
| 5 | LM2 reserves less than the synthesis writes; its validation tests neither the ramp's land nor a write footprint | specification §4.3 (V-L6); run-by-run review §§17.1–17.2 | holds |
| 6 | **New.** S-LM1 as built is not a point function: it uses distance transforms, weighted geodesics and a conjugate-gradient toe solve over the lattice | `TerrainGenLmcSkeleton.cs:121–122, 156, 349, 506` | the "best production fit as an analytic point function" argument (post-G3c review §4.8; design note §5.3) did not survive implementation |
| 7 | **New.** Production consumes the terrain authority only as a world-spanning lattice | the only callers of `TerrainAuthorityField` are the bake, the validation and the bake tests; `OverworldTerrainField.FromSamples` samples once per lattice vertex | a global lattice solve is compatible with the bake contract; seams stay exact |
| 8 | **New.** Surface and vegetation rules read TerrainGen's massif uplift `U2` | `TerrainSurfaceRules.MassifUplift*`, `TerrainVegetationRules.PineUplift*`, `TerrainSurfaceClassifier.cs:66` | any replacement authority must supply an equivalent channel (production plan item) |
| 9 | Audit leftovers still stale | post-G3c review header "Nothing is implemented or run"; G2 brief D9 "uncommitted"; roadmap heading "Geography-first research … — proposals, nothing approved" and "GEO-1 … GEO-8"; `IslandElevation.cs:10` (tech debt item 22) | still present — optional housekeeping (§12) |
| 10 | **New.** Law 11 of [17](../../../17%20Game%20Systems%20Architecture.md) makes Piece-Based Assembly binding for Track W "World … generation", while the terrain authority is a continuous field; the W.2 card still says "noise / band rules" | `17` law 11 and W.2 | a documentation ambiguity in a locked law — owner clarification (§13 D-9) |

### 2.3 What the record says about the central question

| # | Evidence | Source | Bearing |
|---|---|---|---|
| E1 | Production `H4A` reads as one dome: single-dome flag 5 of 7 canonical seeds, 0 passes on any main divide, Strahler ≤ 2; Stage 2A certifies one massif on 25 of 29 audited worlds and none on 4 **[MEASURED]** | research §§1.3, 7.6 | the existing structural algorithms cannot reach D6 by themselves |
| E2 | A skeleton from the island's own geometry gave mountain structure on all 7 seeds but no drainage or lowland (G3); a valley tree added a corridor and 12 pits (G3b) **[MEASURED]** | research §§11–12 | structure alone is not geography |
| E3 | Region-first layout (G3c): settlement sites with a coastal one on 20 of 20 lattices; failed on surface form — facets, whale-backs, drums, a continuous cliff band — after 18 named fixes **[MEASURED]** | post-G3c review §1 | *the layout layer works; the surface layer failed* |
| E4 | LM2 was valid by V-L1 … V-L8, yet plateaus wrote 2.26–6.06 ha outside their painted footprint, raised a corridor by up to 123 m and led a ramp into a ~150 m sea cliff **[MEASURED]** | review §17.2 | ownership mismatch between layout and surface |
| E5 | S-LM1 needed 11 named construction fixes and 3 layout fixes **[MEASURED]**; by the audit's reading 8 of the 11 repaired interactions between its stages **[interpretation]**; its base captured highland drainage at undesignated shores on 110201 in every run from 2 to 11 **[MEASURED]** | not-ready report §9.2; G3e-PM D4 | layered overlays accrete fixes (lesson L7) |
| E6 | S-LM3's receiver iteration never converged: 91–97 % of land changed receiver every iteration, 96–97 % of changes in the hillslope regime; 8.8–10.4 % of land triangles steeper than 70 °, 45–59 % of land walkable from the shore **[MEASURED]** | review §§3, 11.2 | the failure is located in the iteration and the hillslope law, not in "fluvial" as such (probable, not proven) |
| E7 | Blind look (G3e-PM Gate A): S-LM3's panels **met** D6 points 1 and 2 on both seeds but failed point 4, with walls, steps and striped slopes; S-LM1 **failed** point 1 on both seeds — over-smoothed **[VISUAL]** | G3e-PM §13.2 | new evidence for the fluvial family's *structure*, not for its convergence or traversal; the panels were an unconverged iterate and blinding was nominal |
| E8 | Gates and eyes disagreed: S-LM1 on 1337 failed only R7 yet failed D6 point 1; S-LM3 failed every gate yet read best | not-ready report §3; G3e-PM §13.2 | both evidence classes are required, and a disagreement rule is required |
| E9 | Compute is not the constraint: island-scale syntheses 2.6–19.8 s, measurement 2.9–5.0 s, layout 2.4–2.6 s per seed; production authority 22.9 s **[MEASURED]** | run 11 `calibration.jsonl`; research §2.1 | a regional prototype is chosen for control and attribution, not for speed |
| E10 | Seams are exact by construction from the world lattice; meanwhile four downstream tools broke by assuming one tile | architecture §2; tech debt item 2 | geography must stay out of tiles, and tiles out of geography |
| E11 | Each new world is procedural with different geography ([00.2](../../../00.2%20Meta%20Loop.md), Canonical); the seed picks placement from authored archetypes ("controlled chaos") | 00.2 § Новый мир; principles §5 | a fixed authored map is not the production answer; an authored *grammar* is |
| E12 | Class-switched coasts made continuous cliff bands (G3c L4); G3e's Q7 flagged both seeds (1337: 54.5 % cliff shore) **[MEASURED]** | post-G3c §1.3; not-ready report §9.2 | the coastal band must be a bounded region whose class is caused, then measured |

### 2.4 Conclusions drawn

1. **Layout first is right; the layout must own everything the surface writes.** E3–E5.
2. **Overlay construction is the recurring failure, not any one rule.** Every family that composed landforms by `max`/`min`
   over one another accreted fixes (E3, E5).
3. **The only surface the owner judged structurally right was drainage-driven** (E7) — and it failed on convergence and on
   walls (E6). Whether a converging, wall-free version still reads right is *the* open question; it is H2 below.
4. **Production needs neither tiles nor point functions from geography** (E9, E10, finding 7). Regions are free to be
   geographic units — drainage units — because nothing downstream depends on how the surface was computed.
5. **Gates and the owner's eyes must both pass, and their disagreement must be handled by rule** (E8).

---

## 3. Alternatives considered

| Option | What it would mean here | Verdict | Reason |
|---|---|---|---|
| **A. Structural procedural algorithms** as the primary driver | extend Stage 2A/2B (a branched massif), add a positive stage after `H2`, or run a process law from uplift fields alone | **rejected as the driver; kept as the surface law inside D** | E1: certification gives one massif or none; GEO-3 (b) violates the ridge–valley duality; a law without a plan cannot guarantee designated outlets — S-LM3 switched 19 000–262 000 vertices between outlet basins per iteration on 1337 (review §11.2) |
| **B. Authored macro layout + procedural regional synthesis** | a hand-made map per world; each region synthesised separately | **rejected for production; adopted for the prototype fixture; per-region synthesis kept as candidate R-F** | E11: each world must differ; L8 (seed-invariant layouts) is a known failure; a solo developer cannot author maps. An authored regional fixture is exactly what isolates the synthesis in a prototype |
| **C. Hierarchical assembly of explicit landforms**, literally | landform templates (plateau, ridge, valley primitives) blended into one field | **template assembly rejected; declared regions and boundaries adopted** | L2 (drums), L7 (accretion): G3c's and S-LM1's plateaus were exactly such templates; the word "piece" also collides with Piece-Based Assembly ([15](../../../15%20Procedural%20Building%20Generation.md)) |
| **D. Hybrid** | authored grammar + seeded instantiation → one Geography Plan with single-writer ownership and typed boundaries → one drainage-consistent synthesis → placement reads | **recommended** (§4) | combines what worked (E3 layout, E7 structure) with the missing piece (ownership, E4–E5); tested regionally before any island-scale work |
| continue S-LM1 | patch its base, flank coordinate, valley floors | **excluded** | stopped by the owner (G3e-PM §13.6); this proposal implements none of B1, B2 or C |
| revive S-LM3 as closed | iterate further | **excluded** | non-convergent (E6); any revival is a *changed law* with its own authorisation (§13 D-3) |
| LM4 — drainage graph + primitives | outlets first, landform primitives along the graph | **rejected** | equidistance ridges are the whale-back of G3c (post-G3c §4.6); its outlet-first idea survives inside D |
| noise, erosion simulation as structure, learned synthesis, external tools | — | **not candidates** | AGENTS.md forbidden shortcuts; post-G3c §4.7 |
| Stage 3F first, on `H4A` | carve declared valleys into today's relief | **not this phase** | 3F carves drainage into a coherent relief and cannot create one (research §6.2) |
| a larger world or coastline variety first | GEO-1, G3d | **not this phase** | GEO-1 resolved (no resizing on this evidence); G3d waits for a promising model |
| keep `H4A` and stop geography research | ship Overworld V0 on today's relief | **the documented fallback** | the stop outcome of §10.5; recorded so that stopping is a decision, not a drift |

---

## 4. Proposed architecture and data ownership

### 4.1 Shape

```
authored landform grammar   (fixed relief-region types, boundary types, a few macro archetypes)   [versioned data]
      + world seed           (named streams)
      │
      ▼
GEOGRAPHY PLAN — the one authoritative record of structure (no heights except targets)
   footprint (from Stage 1, frozen) · drainage units + outlets · relief regions · feature graph
   (culminations, saddles; ridges = unit boundaries; valley axes) · boundary contracts ·
   reservations (plateau write footprints, settlement reserves, routes) · coastal band intents
      │  PlanValidator (P1–P8): nothing is synthesised from an invalid plan
      ▼
SURFACE SYNTHESIS — one writer per vertex; drainage-consistent by construction; reads the plan, never edits it
      │  world-spanning lattice, 2.734375 m
      ├──► readings (gates, diagnostics)   ├──► views   ├──► placement (W.4 / W.5 / roads read; never reshape)
      ▼
OverworldTerrainField.FromSamples → tiles (representation only; shared edge vertices; seams exact)
```

### 4.2 Principles

| # | Principle | Evidence it answers |
|---|---|---|
| P1 | **One structural authority.** The Geography Plan is the only source of structure; every height is derived from it | E4 |
| P2 | **One writer per vertex.** A land vertex belongs to exactly one drainage unit, which alone computes its height; relief regions only parameterise the law; no landform is applied over another's output | E3, E5, L7 |
| P3 | **Ridges are boundaries, valleys are interiors.** Drainage-unit boundaries run along divides; a boundary crosses a valley only at a declared flow crossing (a unit's outlet into its parent) | E5 (coastal capture), review §17 (dams) |
| P4 | **Plan before height.** Reservations cover everything a landform writes (its write footprint); the validator runs before any synthesis | E4 |
| P5 | **Regions divide the problem; tiles divide the representation.** Synthesis never sees a tile; the bake never sees a region | E10 |
| P6 | **Geography provides, placement chooses** (GEO-9) | GEO-9 |
| P7 | **Measured, not tuned.** World units, labelled values, definitions frozen before calibration, named fixes counted and capped | GEO-7; L7 |

### 4.3 Ownership

| Item | Authoritative owner and representation | Derived from it | Guard against contradiction |
|---|---|---|---|
| Island footprint, coastline | **Stage 1** (frozen, golden-hashed); contour A in research | shore vertices and segments | land mask equality (P2 check); the plan never moves the shore |
| Coastal profile | **plan**: coastal band region per shore segment with an intent (low, rocky, cliff) and a **bounded width** | realised profile (synthesis, inside the band only); measured classes (R6, Q7) | band width enforced by the region raster — the coastal rule cannot reach beyond it (answers Q-O1-a) |
| Mountain regions, branching divide tree | **plan**: mountain region; divide tree as unit boundaries; culmination and saddle nodes with target heights | realised crest heights (synthesis) | targets checked after synthesis (±2 m culminations, M-1 col rule) |
| Peaks, saddles | **plan** nodes | realised position and height | M-1 (specification §14), unchanged |
| Spurs, flanks | **synthesis** inside a unit; major spurs may be declared as boundaries | — | review only; no separate spur writer |
| Valleys, drainage corridors | **plan**: unit tree, declared valley axes as polylines, corridor regions | floor profiles (synthesis, monotone by contract) | R7 (c) on the declared lines |
| Lowlands, foothills | **plan** regions | relief inside them (synthesis) | region budgets checked by the validator |
| Plateaus, escarpments, ramps | **plan** plateau record: top outline, typed rim segments, rim outlets, ramp path and landing, class, **write footprint** | walls, talus, ramp gradient (synthesis, inside the footprint only) | P4 exclusivity: footprints never intersect valley axes, corridors, settlement reserves or cliff bands; ramp needs its length of land and lands away from a cliff |
| Watersheds, designated outlets | **plan** units and outlets | the river network (production `HydrologyPlanner`, a check: R5, R10) | B1: no flow crosses a divide; every elevated system reaches a designated outlet |
| Regional transitions | **plan** boundary contracts (§5.2) | realised blend | contract checks after synthesis |
| Settlement sites, routes, POI sites | **plan** reserves candidate areas and declares routes; **placement** (W.4, W.5, roads) chooses sites | capacity and reachability readings (R4, R8, R9) | placement never edits terrain at region scale (GEO-9) |
| Heights | **synthesis** — the only writer of the lattice | everything downstream | write log: every land vertex written once; nothing written back into the plan |

**Authoritative:** Stage 1 footprint; the Geography Plan (topology, regions, contracts, reservations, targets); the
synthesised lattice (heights). **Derived:** every reading, the river network, measured coastal classes, settlement and
POI capacity, Unity `TerrainData`. A derived reading that disagrees with the plan is a candidate defect, never a reason
to re-plan silently.

---

## 5. Geographic data structures and generation pipeline

### 5.1 The Geography Plan

Authored and stored as vectors in world metres; rasterised deterministically onto the world lattice. The raster — one
unit id and one region code per vertex — is what synthesis reads, so "one owner per vertex" is exact.

| Field | Content |
|---|---|
| Domain | lattice spec (1 025², 2.734375 m), sector window and mask (prototype), contour id and Stage 1 hash |
| Drainage units | id, parent unit (or sea), outlet node, boundary ids; the unit graph is a forest rooted at sea outlets |
| Relief regions | fixed enum: mountain, upland/foothill, plateau top, plateau rim band, ramp, valley corridor, lowland, coastal band (low / rocky / cliff), context; per-vertex code |
| Feature graph | nodes — culmination, saddle (pass flag), branch root, ridge end, outlet, confluence, rim outlet — with positions and target heights; edges — divide (a unit boundary), valley axis (polyline, source → mouth), ramp path, route |
| Plateau records | outline, typed rim arcs (joint, escarpment, ramp, gradual), rim outlets, class (18–40 m or 60–120 m), edge target, ramp path and landing, write footprint |
| Boundary contracts | type and parameters per boundary (§5.2) |
| Reservations | settlement reserves (capital-size disc candidates), routes with grade intent, write footprints |
| Targets | main summit 330–430 m world height, secondaries ×0.62–0.82, saddles ×0.55–0.72, pass offset 40–80 m **[CARRIED, specification §§2, 4.1]** |
| Seed record | world seed, stream ids, variant parameters drawn |
| Hash | SHA-256 of a canonical serialisation |

### 5.2 Boundary contracts

| Type | Where | Height | Slope | Flow |
|---|---|---|---|---|
| **Divide** | between sibling units along a ridge | C0: the boundary vertices belong to one side by a fixed rule; in R-F their heights follow a crest profile prescribed by the plan, in U-F they emerge and are reported | the crest is a local maximum across the boundary; no slope continuity is required — a crest is a natural kink | no D8 flow crosses (gate B1) |
| **Flow crossing** | a unit's outlet into its parent (confluence, plateau rim outlet) | the node height is shared and fixed before the child unit is synthesised | the floor stays monotone through the node | one way, at the node only |
| **Region blend** | between relief regions inside one unit | one owner; the law's *parameters* blend across a declared band by a partition of unity in normalised coordinates (the rule of specification §6, now actually applied) | bounded by the law's caps | unchanged |
| **Shore** | coastal band → sea | base level 24.5 m **[CARRIED]** | by intent, inside the band width only | to the sea |
| **Sector** (prototype only) | sector ↔ context, placed on the main divide | as Divide | as Divide | none |

### 5.3 Seeds, streams and order

- Streams `research.g3f.plan` (variant parameters), `research.g3f.synth.<candidate>` (per-vertex tie-break keys through
  `TerrainHash.StreamKey`), `research.g3f.views` (blind key and panel order). `System.Random` only through
  `WorldSeed.Fork`; `using Random = System.Random;`; never `UnityEngine.Random`.
- Units processed in ascending id; priority queues tie-broken by vertex index, then by hash; float64 accumulation in a
  fixed order, float32 output; no parallel loops in the prototype.
- Hashes: plan SHA-256; lattice SHA-256 and `WorldHeightField.Hash` (FNV, comparable with every pinned value); readings
  SHA-256.

### 5.4 Pipeline — the prototype

1. Contour A (`LmcContour.A()`, Stage 1 hash `7599879a71250a06`) → land mask.
2. Stress fixture + seed → Geography Plan (vectors) → raster (units, regions) → **validator P1–P8**; on failure nothing
   is synthesised and the failure is logged.
3. Control surface (C0) over the whole island = the context outside the sector; candidates write the sector only.
4. Embedding into the world lattice (sector = candidate, rest = context; sea from Stage 1).
5. Readings restricted to the sector (§9), the production hydrology planner on the whole lattice (R5ʳ, R10).
6. Views anchored on the plan (identical cameras for every candidate of a variant).
7. Records with SHA-256 manifests under `D:\Hobby\game-dev\research-data\g3f\`.

### 5.5 Pipeline — the production target (for the production plan, not built in G3f)

`seed → Stage 1 (frozen) → Geography Plan (grammar instantiation) → validator → surface synthesis (world lattice) →
[hydrology carve, optional, unchanged] → OverworldTerrainField.FromSamples (the plan lattice; plus the uplift
replacement channel) → bake (unchanged)`. One production path; no fallback, no alternate bake (AGENTS.md).

### 5.6 Regions versus tiles; merging into one field

- **Dividing the problem.** The island is divided into drainage units because their boundaries are divides: water never
  crosses them, so a unit can be synthesised from its own outlet and its own boundary record, and a crest is the one
  place where a slope discontinuity is natural. Whether this division is *exact* — independent unit synthesis equals the
  unified result, and an edit to one unit changes nothing outside it and its descendants — is measured (gate A1, R-F).
- **Merging.** In U-F the field is one solve. In R-F each unit writes only its own vertices, the shared boundary values
  come from one record, and the merge is concatenation; there is no blending step that could re-open a seam.
- **Dividing the representation.** The merged world lattice is cut into 2 × 2 tiles by `OverworldTerrainField` exactly
  as today: edge vertices are shared lattice points, so seams are exact by construction and the tile layout carries no
  geographic meaning.

### 5.7 How each recorded defect is prevented

| Defect (where seen) | Mechanism in this architecture | Check |
|---|---|---|
| seams | world lattice shared by tiles; unit boundaries on divides; one boundary record | T-d seam readings; B1 |
| overlapping ownership (S-LM1 overlays) | one unit and one region per vertex; no overlay operation exists in the synthesis contract | P1, write log O1 |
| plateau writing beyond its footprint (review §17.2) | write footprint reserved and exclusive | P4; O1 |
| ramp into a sea cliff (110201 p1) | ramp path must lie on land for its full length and land away from cliff bands | P4 |
| plateau damming a valley mouth (1337 p1) | footprints may not intersect valley-axis buffers; rim outlets declared | P4; R7 |
| coastal capture of highland drainage (S-LM1 base) | the coastal rule owns only its band; drainage belongs to units whose outlets are designated | B1; R5ʳ |
| unwanted pits | the candidate law makes every vertex higher than its receiver (C0, U-F, R-F); verified, not assumed | R7 (a) |
| climbing axes (S-LM1 12.88 m) | declared axes are tree paths integrated monotonically; read as lines | R7 (c) |
| raised drainage corridors | corridors are regions of their own unit; nothing else writes there | O1; R7 (c) |
| walls and striped slopes (S-LM3) | lateral constraint L-3 (§6.4); escarpments only where declared | undeclared-wall and lateral-step diagnostics; review |

---

## 6. Prototype design

### 6.1 Hypotheses (wording to be frozen in WP1)

- **H1 — ownership.** With a valid Geography Plan, no candidate (control included) shows an interaction defect: O1 and B1
  hold, and R7 holds on the control.
- **H2 — surface.** A plan-driven, single-pass drainage-tree synthesis meets every regional hard gate and is accepted by
  the owner's blind review at regional and player scale (pass rule §10.2) on ≥ 7 of 10 held-out variants.
- **H3 — assembly.** Per-unit synthesis with plan boundary records is exact: unit order does not change a single byte, and
  an edit to one unit changes no vertex outside it and its descendants (A1).
- **H4 — agreement.** The ranking of candidates by gates agrees with the owner's ranking. Not a pass condition; a
  disagreement triggers §10.6.

### 6.2 Domain, size and resolution

**Regional, because** compute is not the constraint (E9) but attribution is: a sector built deliberately around the
configurations that failed tests them on every variant instead of waiting for a seed to produce them, and keeps the
owner's review small.

| Required element | Size driver | Source |
|---|---|---|
| two culminations ≥ 450 m apart with a saddle | ≥ 600 m of divide | specification §4.1, V-L7 |
| a tall-class plateau: top 0.03–0.15 km² (∅ 195–437 m), band edge/tan 65° + 10 m, ramp 1.15 × edge / tan 18° = 212–425 m | ≥ 700 m from ridge to ramp landing | specification §4.1; review §17.1 |
| a capital-size site, core radius 150 m, plus 100 m to reach the sea for the coastal test | ≥ 500 m of lowland | specification §7.1 |
| ≥ 3 converging tributaries, valleys 50–300 m wide | ≥ 600 m of flank | research §4.2 |
| a coast with three intents and a mountain foot within 200 m of the shore on part of it | ≥ 1 km of shoreline | Q7; review §5 |

**Size:** a sector of about 1.0–1.3 km across, 0.8–1.2 km² of land (≈ 28–42 % of contour A's 2.82 km²), inside a window
of **513 × 513 vertices (1 400 m)** with ≥ 50 m margin. The window happens to equal a production tile in size; its
position is chosen by geography and need not align with a tile. A smaller window would force dropping the tall plateau
class or the second culmination — exactly the configurations that failed.

**Resolution: the production lattice, 2.734375 m.** Walkability (T-edge, < 45° triangles), wall representability (a 20 m
wall over one or two steps reads 74–82°), the S8 thresholds (≥ 0.25 m, ≥ 100 m²) and behaviour that is not resolution-
invariant (Stage 3F handoff §15) are all tied to it; a coarser lattice may rank, never accept (research §3.5).

**Placement on contour A** (fixed in WP0, recorded before WP1 freezes): the sector's land boundary lies on the main
divide; its seaward side holds cliff-capable and low shore; it covers the north-west part of the island, where 110201's
run-11 plateau 1 and its ramp to the sea cliff lay (around world (850–1 000, 2 150–2 300) m), so that historical panels
at the same cameras show the failures the fixture stresses.

### 6.3 The stress fixture

| Id | Feature | Stresses (where it failed) |
|---|---|---|
| F1 | main-divide segment on the sector boundary with two culminations and a saddle | single summit and whale-back (L3); the M-1 col |
| F2 | a branch ridge from the main culmination to a cliff headland, dividing units A and B | junction frames (S-LM1 frame-blend climbs); caused coasts (L4) |
| F3 | a saddle on the branch ridge with a declared route between lowlands A and B, offset 40–80 m | pass corridor crossing a plateau (110201 corridor 1); M-1 |
| F4 | unit A: ≥ 3 tributaries converging into a trunk to outlet O1; unit B: 1–2 valleys to outlet O2 | converging valleys; climbs toward mouths (12.88 m); R5 |
| F5 | one plateau on the branch flank: joint toward the ridge, escarpment toward lowland A, a ramp landing in lowland A, ≥ 1 rim outlet; placed so that an unconstrained ramp would point at the coast and the footprint sits beside a valley mouth | write footprints; ramp to cliff (110201 p1); valley-mouth dam (1337 p1); drums (L2) |
| F6 | lowland A with a capital-size settlement reserve near O1, and a coastal site | settlement capacity beside a mountain foot |
| F7 | a coastal band with low (O1, O2), rocky and cliff (headland) intents; the mountain foot within 200 m of the shore on part of the sector | coastal capture (Q-O1-a); the cliff band (Q7) |

**Seeded variant parameters** (the topology — units, region types, sector boundary — is fixed; that is the fixture's
limit): culmination and saddle targets within their ranges; plateau class (each with probability 0.5 **[CARRIED]**),
top area, position among three declared slots on the branch flank, rim shares within specification §4.1, 1–2 rim
outlets; 3–4 tributaries and their heads; outlet position along its low segment (±60 m **[AUTHORING]**); coastal band
widths within each intent's range **[AUTHORING]**.

### 6.4 Candidates and references, under identical conditions

| Id | Surface | Represents | Status |
|---|---|---|---|
| **C0 control** | per unit, a constrained cost-distance from the outlet scaled by region; no law | plan + the simplest geometric synthesis (the G3c lineage) | required; also the tree source and the context |
| **U-F** | plan + **unified** drainage-tree synthesis; crests emergent | option D | required; needs D-3 |
| **R-F** | the same tree and law, **per unit** with crest and boundary heights prescribed by the plan; units concatenated | option B's regional assembly | required; needs D-3 |
| U-P | plan + geometric profiles per region with partition-of-unity blending | option C done with single ownership | optional (WP-O1); the default if D-3 is declined |
| ref `H4A` | production 110201, same cameras | option A as it exists | reference, labelled, not rated |
| ref G3e | run-11 S-LM1 and the closed S-LM3 lattices of 110201, same cameras | history | calibration only, labelled, not rated |

U-F and R-F share the tree and the law, so their comparison isolates assembly; C0 against U-F isolates the law.

**Law contract for U-F and R-F** (required properties; equations and parameters in WP1, checked analytically first):

| # | Property | Why |
|---|---|---|
| L-1 | the receiver tree is built **once** from the plan — outlets, unit membership, declared axes as preferred paths, region costs, stateless hash tie-breaks — and never recomputed from heights | removes the coupling that kept S-LM3 from converging (E6) |
| L-2 | heights integrated once from outlets upward, `h = h_receiver + d · s(A, region)`, concave long profiles in channels and convex near divides | the shape S-LM3 was meant to give, without an iteration |
| L-3 | a deterministic **lateral constraint** that only lowers heights, never creates a closed depression and never moves flow across a divide, bounding steps between adjacent flow paths outside declared escarpment and cliff bands | **[HYPOTHESIS]** S-LM3's walls and stripes are lateral steps between neighbouring flow paths, which integration along a tree does not constrain; checked first on the preserved lattices (WP0, C0-1) |
| L-4 | targets reached by monotone scaling per region, ≤ 12 bisection steps, ±2 m culminations, ±5 m pass, ±2 m plateau edge class **[CARRIED, specification §5.4]** | elevation targets kept without tuning by hand |
| L-5 | plateau: top graded to its rim outlets; walls only in declared escarpment arcs; joint rims continuous with the ridge; ramp capped at 20° along its path (run 11 measured 18.91–19.17°) | L2; R8 |
| L-6 | coastal profile only inside the band width, by intent | Q-O1-a; L4 |
| L-7 | single writer, with a write log | P2 |

Guarantees are **verified, not assumed**: no S8 depression, monotone declared axes, every vertex draining to its own
unit's outlet on the final surface.

### 6.5 Fixed conditions

Contour A only; world 2 800 m; lattice 1 025² at 2.734375 m; sea level 24 m; budget 700 m; land written at ≥ 24.5 m;
sea from Stage 1 **[CARRIED]**. The same plan, context, rule values, cameras and presentation for every candidate of a
variant. Production `H4A` 110201 rebuilt and its pinned hash `e2bad571f4af76c7` checked in every process; the
protected manifest (§8.4) checked before and after every run; outputs outside `Assets`; Editor-only code referenced by
nothing.

### 6.6 Seeds

| Role | Seeds | Rule |
|---|---|---|
| calibration | **110201, 1337**, each with both plateau classes (4 variants) | the only variants generated, measured or viewed before the freeze |
| held-out | **5001–5010** **[PROPOSAL]** | verified unused before the freeze (WP0, C0-4); never generated before the freeze record exists |
| reserve | **5011–5020** | only if an evaluation is invalidated by a post-freeze change |
| untouched | **1005–1024** | kept for the island-scale evaluation of the production plan's first stage |

### 6.7 Steps

Plan and validator → measurement harness proven on preserved data → view kit proven on references → control → U-F →
R-F → calibration (named fixes only, capped) → blind calibration review → freeze record → held-out evaluation (separate
authorisation) with a determinism repeat → blind held-out review → report and architecture decision. Work packages: §7.

### 6.8 Non-goals, and what the prototype cannot show

**Non-goals:** a full-island candidate; seeded instantiation beyond the fixture's variants; several plateaus in several
regions (D6 point 2) and 3–5 settlements (point 7) — island-scale; coastline planform (G3d); lakes; Stage 3F; the
hydrology carve; roads, POI or settlement placement; materials, vegetation, fog; the tier table; the jump.

**Cannot show:** island-scale composition; variety of macro layouts between seeds (L8); travel pacing; production
integration cost; whether walls stop a jumping player (3F-20).

---

## 7. Implementation work packages

Order of dependency: `WP0 → WP1 → [G-3] → WP2 → (WP3 ∥ WP4) → [G-4b] → WP5 → WP6 → WP7 → WP8 → [G-5a] → WP9 → WP10 →
[G-6] → WP11 → [G-7]`. Every code package is split into iterations of at most ~500 lines
([17](../../../17%20Game%20Systems%20Architecture.md) law 8); new research code is estimated at 3 000–3 800 lines
(G3e: about 7 800).

### Required

**WP0 — Evidence pack and read-only checks** *(no repository change)*
- *Purpose:* settle the facts WP1 needs. C0-1: on the closed S-LM3 lattices (`diag-run6`, 110201 `dc33f1ea…`, 1337
  `9f2629ac…`) the share of land triangles ≥ 45° and ≥ 70°, split into steps along the steepest-descent direction and
  lateral steps, by region; expectation written before running: ≥ 50 % of the > 70° triangles outside escarpment and
  cliff bands are lateral. C0-2: a stress catalogue — every G3e failure configuration with coordinates and recorded
  cause. C0-3: two or three candidate sector windows on contour A measured (land, shoreline, divide-to-coast depth) and
  one chosen by §6.2's rules. C0-4: the seed range 5001–5020 checked as unused in the repository and in research data.
  C0-5: run-11 readings that fall inside the chosen sector — the continuity targets for WP3.
- *Depends on:* G-1 and an authorisation of read-only checks.
- *Outputs:* `research-data\g3f\wp0\` — `stress_catalogue.md`, `window.md`, `seed_check.txt`, `c0-1_walls.md`,
  `continuity_targets.json`, scripts, SHA-256 manifests.
- *Verification:* scripts hashed before execution; two runs byte-identical; inputs (run 11 105/105, lmc-reference 48/48,
  `diag-run6`) verified before and after.
- *Complete when:* every item has a recorded result and C0-1's expectation is evaluated as written.

**WP1 — G3f technical specification and pre-registration** *(documentation)*
- *Purpose:* one document that fixes everything a run depends on — the plan model and the stress fixture, validator
  P1–P8 with values, boundary contracts, the law's equations, regional and process gates, diagnostics and flags, the view
  set, the review form, the pass and decision rules, seeds, stop rules, the file breakdown and a readiness checklist. It
  replaces G3e's chain of note, draft, specification and readiness check with one document.
- *Depends on:* WP0.
- *Outputs:* `unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md` (Draft); pointers in 14, 99, AGENTS.md.
- *Verification:* the law checked analytically on small synthetic lattices in Python (a cone, a straight ridge, a plateau
  block, two units meeting at a divide): no pit, monotone axis, bisection reaching its target, the lateral bound
  respected — the method of G3e's specification §11; no Unity.
- *Complete when:* the owner approves it (G-3) and records the authorisation of WP2–WP4.

**WP2 — Geography Plan, stress fixture and validator** *(code; no heights)* — the first code slice, §14.2.

**WP3 — Regional measurement harness and continuity** *(code; preserved data only)*
- *Purpose:* readings restricted to the sector, carrying the specification's §§7–8 calculations unchanged where they
  apply, plus the new gates and diagnostics of §9.
- *Depends on:* WP2a (the sector mask).
- *Outputs:* `TerrainGenG3fMeasure.cs`; a continuity report.
- *Verification:* on run 11's S-LM1 lattices restricted to the sector, the harness reproduces C0-5's targets: S8
  depressions within 3 m, 0.01 m depth and one cell of area (G3e-PM's V1 tolerances, with `DepressionFill`'s 1 mm
  epsilon), axis climbs within 0.01 m, R8 reachability identical.
- *Complete when:* every continuity check holds; otherwise stop S3.

**WP4 — View kit** *(code; renders preserved lattices only)*
- *Purpose:* the camera set, presentation, camera validity rule, route strips, contact sheets and sealed key of §9.7.
- *Depends on:* WP2c, WP3.
- *Verification:* two fresh processes byte-identical, as G3e-PM's D5; reference sheets rendered (`H4A`, run 11).
- *Complete when:* the owner confirms the camera set on the references — a look at cameras, not a rating.

**WP5 — Control surface, context and embedding** *(code; the first new surfaces)*
- *Depends on:* WP2–WP4 and authorisation G-4b.
- *Verification:* C0 has no S8 depression; the production planner accepts the context on its own (R10); determinism.
- *Complete when:* C0's readings and sheets exist for the four calibration variants.

**WP6 — U-F synthesis** *(code; needs D-3)*
- *Verification:* EditMode tests on synthetic lattices (no pit, monotone axes, targets, the lateral bound, single
  writer); determinism; complete readings.
- *Complete when:* U-F runs on the calibration variants; every defect is logged.

**WP7 — R-F synthesis and assembly tests** *(code; needs D-3)*
- *Verification:* gate A1 — two unit orders give byte-identical lattices; one unit's parameters changed alters no vertex
  outside it and its descendants.
- *Complete when:* A1 is measured, pass or fail.

**WP8 — Calibration, blind calibration review, freeze**
- *Purpose:* calibrate on the four calibration variants with named fixes only — at most **6 per candidate** and **5
  calibration runs** **[PROPOSAL]**; a blind review; the freeze record (code hashes and commit, plan and law settings,
  every rule value, cameras, seeds, the expected output inventory).
- *Complete when:* the owner decides at G-5a: freeze one or more candidates, or stop.

**WP9 — Held-out evaluation** *(separate authorisation)*
- *Purpose:* the frozen candidates on 5001–5010; a determinism repeat in fresh processes; the blind review.
- *Complete when:* the pass rule of §10.2 is applied as written.

**WP10 — Report and architecture decision memo** — results, shared and candidate-specific failure modes, the rule of
§10.3 applied, everything preserved. *Complete when:* the owner decides at G-6.

**WP11 — Production implementation plan** *(documentation; only if G-6 adopts an architecture)* — the island-scale
stage first (seeded grammar → plan for the whole island; the chosen synthesis; contour A, then Stage 1 coastlines;
held-out 1005–1014 or the owner's set); then production placement under GEO-3 (c): which of Stage 2B, 3A–3E, 4A and
`H1` inside the land are superseded, the scoped unfreeze, `GeneratorVersion` 3, the re-audit (7 canonical seeds and
1000–1039), the `U2` replacement channel for surface and vegetation rules, Stage 3F's place (G5), the hydrology carve,
downstream re-measurement (roads, POI, settlements — tech-debt item 2 first), pinned hashes and rollback.
*Complete when:* the owner approves it (G-7). Nothing is implemented by it.

### Optional (not part of the required path)

| Id | Package | When it is worth doing |
|---|---|---|
| WP-O1 | U-P profile-law candidate | if D-3 is declined, or as the next step when H1 holds and the fluvial law fails |
| WP-O2 | a second fixture in another part of contour A, held-out only | if the owner wants robustness to a second topology before G-6 |
| WP-O3 | materialised views (presentation only, after geometry ratings are fixed) | for art direction, never for the geometry verdict |
| WP-O4 | a Play Mode walk on the frozen candidate (Stage 3F handoff §21 harness) | to read player-scale traversal beyond T-edge |
| WP-O5 | housekeeping of the audit's remaining stale lines (§2.2 row 9) | any time, as its own documentation commit |

---

## 8. Files and classes

### 8.1 New files (research, Editor-only, namespace `GameDev.EditorTools`, `#if UNITY_EDITOR`, referenced by nothing)

| File | WP | Content |
|---|---|---|
| `unity/Assets/Scripts/Editor/TerrainGenG3fPlan.cs` | WP2a | plan types, the stress fixture's vector data, seeded instantiation, rasterisation, canonical hash |
| `unity/Assets/Scripts/Editor/TerrainGenG3fValidator.cs` | WP2b | validator P1–P8 |
| `unity/Assets/Scripts/Editor/TerrainGenG3fDump.cs` | WP2c… | batch entry `DumpBatch`, steps `plan`, `continuity`, `views`, `calibrate`, `evaluate`; protected manifest; records |
| `unity/Assets/Scripts/Editor/TerrainGenG3fMeasure.cs` | WP3 | sector readings, process gates, diagnostics |
| `unity/Assets/Scripts/Editor/TerrainGenG3fViews.cs` | WP4 | cameras, validity rule, route strips, sheets, sealed key |
| `unity/Assets/Scripts/Editor/TerrainGenG3fSurface.cs` | WP5 | control surface, context, embedding |
| `unity/Assets/Scripts/Editor/TerrainGenG3fFluvial.cs` | WP6–7 | U-F and R-F |
| `unity/Assets/Scripts/Editor/Tests/TerrainGenG3fPlanTests.cs` | WP2b | EditMode, pure C#, synthetic plans |
| `unity/Assets/Scripts/Editor/Tests/TerrainGenG3fSurfaceTests.cs` | WP6–7 | EditMode, small synthetic lattices |

### 8.2 Existing code read, never modified

| Class | File | Used for |
|---|---|---|
| `LmcContour.A()` | `Editor/TerrainGenLmcLayout.cs:99` | contour A and its Stage 1 hash |
| `LmcGrid` (`Index`, `Rasterize`, `Edt`), `LmcLayoutBuilder.WeightedGeodesic` | `Editor/TerrainGenLmcLayout.cs` | rasterisation, distances |
| `LmcBase.Build` (triangles, T-edge, flood, D8), `LmcMeasure` (Q1, Q2, Q5, Q7, `Plateau`, `DCap`, `DReg`, `VertexDijkstra`, `TriDijkstra`, `Components8`) | `Editor/TerrainGenLmcMeasure.cs` | the same definitions as G3e |
| `TerrainGenLmcDump.Protected`, `Md5`, `ReadFloats`, `WriteFloats`; `LmcJson` | `Editor/TerrainGenLmcDump.cs`, `TerrainGenLmcMeasure.cs` | manifests, lattice I/O, records |
| `GeometryFirstReview.Run(seeds, dir, latticePath, candidate)` | `Editor/GeometryFirstReview.cs:125` | G1 V1–V6 on an embedded lattice (optional) |
| `IslandReliefWorld` | `Editor/TerrainGenIslandReliefDump.cs:890` | `H4A` reference |
| `HydrologyGrid`, `HydrologyPlanner.Plan`, `HydrologySettings.Default`, `DepressionFill` | `World/Hydrology/` | R5ʳ, R10, S8 |
| `WorldSeed.Fork`, `TerrainHash`, `WorldHeightField.Hash` | `World/Seed/`, `World/Terrain/`, `World/TerrainGen/` | streams, hashes |
| `OverworldTerrainGrid.Create`, `OverworldTerrainBuilder.WriteHeights`, `OverworldTerrainField.FromSamples` | `World/Terrain/` | heights-only tiles in memory for views |

If a reused helper ever needs a change, it is **copied** into a G3f file with a provenance comment; G3e, G3c, G0 and G1
files are never edited.

### 8.3 Production files

**None change in G3f.** For scoping the production plan only: `World/Terrain/TerrainAuthorityField.cs`,
`World/TerrainGen/IslandElevation.cs` (or a new composition), `World/Terrain/OverworldTerrainField.cs`,
`TerrainSurfaceRules.cs`, `TerrainVegetationRules.cs`, `TerrainSurfaceClassifier.cs` (the `U2` channel),
`TerrainGenSettings` (`GeneratorVersion`), `Editor/OverworldTerrainBake.cs`, `Editor/OverworldTerrainValidation.cs`,
`Editor/Tests/OverworldTerrainBakeTests.cs` and the Stage tests with pinned hashes, `HydrologyBake`, and the consumers in
`World/Landscape/`, `World/Poi/`, `World/Settlement/`.

### 8.4 Protection

The G3e manifest (73 files, `research-data\g3e-step1\scratchpad-g3e\protected_md5_before.txt`) plus the six
`TerrainGenLmc*.cs` files and their `.meta` — 85 files — checked before and after every run; `git diff --quiet --
unity/Assets/Scripts/World` before and after; contour A's Stage 1 hash and production `H4A` 110201's hash equal to their
pinned values; held-out seeds absent from every output before the freeze.

---

## 9. Tests, metrics, seeds and visual evidence

### 9.1 Hard gates (sector readings; calculations of the specification §7.1 carried where they apply)

| Gate | Regional reading | Pass |
|---|---|---|
| R4ʳ settlement | the settlement reserve holds a capital-size site (disc core 150 m, no sea, ≥ 90 % buildable at ≤ 6.84°) and the sector holds a coastal site (core + 100 m reaches the sea) **[CARRIED values]** | both exist and are shore-reachable |
| R5ʳ drainage | on the production planner's network over the embedded lattice: every system with a source in the sector's mountain region or on a plateau top ends at a designated outlet (termination within 150 m **[CARRIED]**) | ≥ 1 such system, and all of them |
| R7 hygiene | (a) S8 in the sector (fill ≥ 0.25 m over ≥ 100 m²); (b) lakes found: 0 (no declared basin); (c) climbs along every declared axis — declared as **lines** (polylines resampled at ½ cell, nearest vertex), so the band ambiguity of review §21.2 cannot arise | (a) 0, (b) 0, (c) every climb ≤ 0.25 m |
| R8ʳ reachability | vertex reachable from the shore on T-edge: the site centre, the plateau top's centroid vertex, the ramp landing, every declared saddle col on a route (M-1 rule, 150 m) | all reachable |
| R9ʳ routes | every declared route's endpoints in one T-edge component; detour, steepest triangle and an alternative reported **[CARRIED, K2]** | all connected |
| R10 planner | `HydrologyPlanner.Plan(grid, HydrologySettings.Default, seed)` on the embedded lattice | `Succeeded` |

### 9.2 Process gates

| Gate | Reading | Pass |
|---|---|---|
| O1 single writer | the write log | every sector land vertex written exactly once, by its unit; nothing outside the sector written |
| B1 boundary contract | D8 on the final surface; targets | 0 vertices draining across a divide (declared flow crossings excepted); culminations ±2 m, pass ±5 m, plateau edge class ±2 m |
| A1 assembly (R-F) | two unit orders; one unit perturbed | byte-identical lattices; no change outside the perturbed unit and its descendants |
| determinism | two fresh processes | lattices, readings and plan byte-identical |
| protection | §8.4 | unchanged |

### 9.3 Diagnostics (report-only; flags only where pre-registered)

| Measure | Detects | Notes |
|---|---|---|
| Q1–Q9, D-cap, D-reg (specification §§8, 13.3) | facets, missing concavity, crests, cones, drums, coasts, accretion, slope clustering | Q1, Q5 and Q6 flags at the §8.1 levels; Q7's levels are island shares and do not transfer to a sector — report only; Q8 is not meaningful on one fixture |
| **undeclared walls** **[new]** | walls outside declared escarpment and cliff bands | share of sector land triangles ≥ 45° outside them, by 45–60° / 60–70° / > 70°, with steep-run heights; never called impassable |
| **lateral steps** **[new]** | ribs between neighbouring flow paths | share of same-unit neighbour pairs, neither the other's receiver, whose height difference exceeds `c · tan 45°` outside declared bands (validated on WP0's C0-1) |
| **parallel flow** **[new]** | fans and stripes | share of hillslope windows (≈ 41 m) whose D8 directions lie within ±22.5° and do not converge within 100 m |
| crest and slope diversity | uniform crests and flanks | culmination height spread; crest coefficient of variation along declared divides; saddle depths; slope bands and aspect variance per region |
| valley continuity | climbs, hanging valleys | longitudinal profile per declared axis (longest flat run, concavity θ); floor step at each confluence |
| plateau | drums, false edges | edge height by K1 against its class; joined share against the plan; rim re-entrants against declared rim outlets; top relief; ramp's steepest triangle |
| coast | caused coasts | measured class against intent per segment; cliff run lengths; class at each outlet |
| settlement suitability | site quality | buildable area inside the reserve (R3); share of the site on the valley floor; distance to outlet and water; route to the pass |
| performance and memory | cost | synthesis and end-to-end time per variant; peak managed memory; island extrapolation by land-vertex count |
| fix count (Q9) | accretion | named fixes by kind, against G3c's 18 and S-LM1's 11 + 3 |

### 9.4 Determinism and reproducibility

Every step run twice in fresh Unity 6000.5.7f1 batch processes with no editor open on the project; byte-identical
plans, lattices, readings and images (timing logs excepted). Scripts and preregistration hashed before use; inputs
hashed before and after; every output folder carries a SHA-256 manifest.

### 9.5 Performance and memory **[AUTHORING — budgets reported, not gating, unless the owner says otherwise]**

End-to-end per variant ≤ 60 s; synthesis extrapolated to the whole island ≤ 30 s single-threaded (today's authority:
22.9 s; S-LM3 with 30 iterations: 19.5–19.8 s; S-LM1: 2.6–2.7 s); peak managed memory ≤ 512 MB (one float64 field on
the world lattice is 8.4 MB).

### 9.6 Settlement suitability

R4ʳ as a gate; the R3 buildable area inside the reserve, flood exposure, distance to water and the route to the pass as
diagnostics. Settlement size classes, slope and spacing stay provisional (D2).

### 9.7 Visual review

- **Presentation** — G1's conventions exactly: URP `Terrain/Lit`, one in-memory neutral layer RGB 160/160/154 with alpha
  0, sun `Euler(42, −35, 0)` at 1.2 with soft shadows, flat ambient 0.40/0.41/0.43, no fog, post-processing, anti-
  aliasing, HDR or skybox, reflection 0, no vegetation, a 24 m sea-level ribbon, shadow distance set per view in memory
  (research §3.6). A second sun azimuth for regional views only if pre-registered in WP1.
- **Equal conditions** — the same embedded lattice bounds, the same context, the same camera records for every
  candidate of a variant.
- **Cameras, anchored on the plan** (so identical for all candidates):

  | Scale | Id | Pose **[AUTHORING; LMC conventions where they exist]** |
  |---|---|---|
  | overview | S-OVR | from offshore along the sector's seaward normal, 2.2 × its equal-area radius, 900 m, FOV 40° |
  | overview | S-PLAN | orthographic top-down of the window (2 048²), with a separate overlay of the plan |
  | regional | A-MTN | 200 m offshore, 350 m above the sea, at the main culmination |
  | regional | A-SAD | 500 m along the route from the branch saddle, 150 m above ground |
  | regional | A-PLT-E, A-PLT-J | 400 m outside the widest escarpment arc at 35° down; along the joint from the ridge side |
  | regional | A-CONF | 300 m downstream of the trunk confluence, 120 m above ground |
  | regional | A-OUT, A-CLF | 350 m offshore at 60 m for O1; 250 m offshore at 40 m for the cliff headland |
  | player | P-SITE, P-SAD, P-RAMP, P-TOP, P-VAL, P-COAST | eye 1.7 m above the served surface at the site centre, the saddle, the ramp foot, the plateau edge, the trunk floor and the low shore, FOV 60° |
  | player | R1, R2 | route strips: 8 eye-level frames every ~60 m from the site to the plateau top and from the site to the saddle |

- **Camera validity** — a player frame whose pixels are more than 30 % nearer than 3 m is flagged and also rendered from
  +10 m with the same horizontal pose; both are kept (G3e-PM saw two near-dark frames).
- **Sheets** — per variant and camera: panels X, Y, Z (C0, U-F, R-F in a seeded order under a sealed key) and a labelled
  `H4A` panel; historical G3e panels once at calibration, labelled, never rated.
- **Form** — per variant, per scale, per applicable D6 point — 1ʳ (a branching system with several culminations, spurs and
  a saddle as far as the sector shows it), 3, 4, 5, 6ʳ (water and coast inside the sector), 8 — *meets / partly / fails /
  —* with a reason; a best-and-worst preference per sheet; artefacts and counterexamples. Points 2 and 7 are island-scale
  and are not rated. Ratings are hashed and timestamped before the key opens, as in G3e-PM.
- **Separation** — geometry is judged on neutral renders only; materialised views (WP-O3) come after the geometry ratings
  are fixed and change none of them. The concept image guides composition only; no count, size or height is read from
  it.
- **Blinding** is nominal when a candidate has a recognisable signature; that is recorded as a limitation.

### 9.8 What is already approved and what needs the owner

| Criterion | Status |
|---|---|
| R5, R7, R8, R9, R10 calculations; S8 thresholds; T-edge; M-1; K1, K2; Q1–Q9 definitions and §8.1 flag levels; D-cap, D-reg; buildable 6.84°, site radii | **approved for G3e** — carried unchanged; their use in G3f needs the owner's confirmation at G-3 |
| regional variants R4ʳ, R5ʳ, R8ʳ, R9ʳ; R7 (c) on declared lines | **new** — owner approval before binding |
| process gates O1, B1, A1 | **new** — owner approval |
| undeclared walls, lateral steps, parallel flow | **new diagnostics** — report-only unless the owner sets a flag |
| visual form, the pass rule (§10.2), the decision rule (§10.3) | **new** — owner approval |
| fix cap 6, run cap 5; seeds 5001–5020; performance budgets | **new** — owner approval |
| D2 settlement assumptions | **provisional** (unchanged) |

---

## 10. Acceptance criteria, decision gates and stop conditions

### 10.1 Gates

| Gate | Decides | Input | By |
|---|---|---|---|
| **G-1 Research and evidence review** | whether the evidence and the phase scope are accepted | this proposal | owner |
| **G-2 Architecture proposal** | the hybrid of §4 as the hypothesis to test; the candidate set; D-3 | this proposal | owner |
| **G-3 Experiment and frozen criteria** | the G3f specification (WP1), every new criterion of §9.8; authorisation of WP2–WP4 | WP0, WP1 | owner |
| **G-4 Authorised implementation** | (a) WP2–WP4 — no new surfaces; (b) WP5–WP7 — surfaces on calibration variants only | WP2–WP4 records | owner |
| **G-5 Validation and visual review** | (a) after calibration: freeze, change or stop; (b) after held-out: the pass rule applied | WP8, WP9 | owner |
| **G-6 Architecture decision** | §10.3 | WP10 | owner |
| **G-7 Production implementation plan** | approve, revise or reject the plan (input to G4) | WP11 | owner |

### 10.2 Pass rule for a candidate (to be frozen in WP1)

On the frozen configuration a candidate passes when:

1. every hard gate (R4ʳ, R5ʳ, R7, R8ʳ, R9ʳ, R10) and every process gate (O1, B1, determinism, protection; A1 for R-F)
   holds on **≥ 7 of 10** held-out variants — the bar of G3c and G3e; **and**
2. the owner's blind review rates **no** applicable D6 point *fails*, and rates points **1ʳ, 3 and 4** *meets*, on
   **≥ 7 of 10** held-out variants.

The owner's preference against the control is recorded. **Simplicity rule:** if C0 also passes, the simpler surface is
preferred unless the owner's review prefers the candidate ([000](../../../000%20Project%20Rules.md): choose the simpler
expandable system).

### 10.3 Architecture decision rule (G-6)

| Outcome | Decision proposed to the owner |
|---|---|
| U-F and R-F pass; the review prefers neither | Geography Plan + **per-unit** synthesis (locality, plan-owned crests, per-unit regeneration) |
| U-F passes, R-F fails | Geography Plan + **unified** synthesis; regions remain planning and ownership units only |
| R-F passes, U-F fails | Geography Plan + per-unit synthesis with designed boundaries |
| only C0 passes | Geography Plan + the simple surface; the law is recorded as adding nothing yet |
| none passes; H1 holds (O1, B1 and C0's hygiene hold; failures are of form) | keep the Geography Plan as the foundation; next step a law comparison under the same plan (WP-O1) — with its own plan |
| none passes; H1 fails | the ownership model is insufficient: stop G3f; the owner chooses between revisiting the families (post-G3c §4) and stopping geography research (production stays `H4A`) |

### 10.4 Stop conditions

| # | Condition | Action |
|---|---|---|
| S1 | a protected file or pinned hash changes | stop at once; report; nothing further runs |
| S2 | the fixture fails the validator after 2 logged plan corrections | stop at G-4a: the plan concept is defective; report |
| S3 | the harness cannot reproduce the continuity targets | stop; a harness fix is measurement code and needs its own authorisation |
| S4 | a candidate needs a 7th named fix or a 6th calibration run | that candidate stops; recorded as not ready |
| S5 | no candidate passes every hard gate on all four calibration variants | no freeze; a not-ready report (design note §11 style) |
| S6 | a candidate passes only with a changed definition or threshold | stop; never retroactive; a new experiment would need a new pre-registration |
| S7 | the held-out rule fails | the candidate fails; no re-tuning; the reserve is used only if a post-freeze change invalidated the evaluation |
| S8 | an out-of-scope defect blocks a reading | §10.7 |

### 10.5 If every candidate fails

The phase still ends with a decision and preserved results: the not-ready report separates shared from candidate-specific
failures (design note §11), says whether H1 held, and §10.3's last two rows apply. "Keep `H4A` and ship Overworld V0
on today's relief" is a legitimate owner decision and is recorded as such.

### 10.6 If gates and the owner's review disagree

Neither overrides the other (design note §8): a candidate needs both. Each disagreement is classified — a **metric blind
spot** (gates pass, the views show a defect: a new diagnostic is proposed for the *next* experiment, never retroactively),
a **view blind spot** (the views look right, a gate fails: the failure is located in images and a camera is proposed for
next time), or a **presentation defect** (dark or obstructed frames). A family the owner prefers visually despite failed
gates may still be chosen as the family of the next stage — a family decision, never a candidate acceptance — and that
decision is recorded in the owner's words (as with S-LM3's structure in G3e-PM).

### 10.7 Defects outside the prototype's scope

Recorded with evidence and not fixed inside G3f: a production defect becomes a tech-debt entry; a tool defect (the
hydrology planner, G1, Unity) makes the affected reading *INCONCLUSIVE* for that variant, and the owner decides whether
a separately authorised fix and a re-run of that reading only follow. Production code is never patched under G3f's
authorisation.

### 10.8 What survives a failure

The Geography Plan model and validator; the stress fixture and its catalogue; the regional harness with its continuity
proof; the view kit with its camera rule; the control baseline; every candidate's records and the defect log; the
disagreement record — all under `research-data\g3f\` with manifests, and the code in the repository history.

---

## 11. Risks, performance constraints and rollback

### 11.1 Risks

| # | Risk | Likelihood | Mitigation |
|---|---|---|---|
| K1 | the single-pass law still gives walls, ribs or stripes | medium–high | L-3, validated first on preserved lattices (C0-1); the new diagnostics; the control comparison |
| K2 | an evenly "eroded" texture everywhere — the uniform mountain field D6 forbids | medium | region-specific parameters; designed units; the review |
| K3 | the fixture is tailored to known failures and over-fits | medium | seeded held-out variants; WP-O2; the island-scale stage before any production work |
| K4 | regional results do not transfer to the island (budgets, seed variety, 3–5 settlements) | high, by design | listed in §6.8; the island-scale stage is mandatory in WP11 |
| K5 | fixes accrete again (L7) | medium | fix and run caps; named fixes only |
| K6 | the harness depends on G3e code | low | protected manifest; copy-on-change; continuity proof |
| K7 | gates and eyes disagree | medium (it happened in G3e) | §10.6 |
| K8 | Stage 3 §16's rejection of flow-derived structure is held to apply | low–medium | D-3; WP-O1 as the fallback |
| K9 | the production jump climbs walls (3F-20 open) | certain | walls reported as steepness; T-edge is a lower bound; no "impassable" claim |
| K10 | the sector boundary or the context confounds views or R10 | medium | the boundary on the main divide; cameras on the seaward side; the context identical for all; R10 checked on the context alone (WP5) |
| K11 | the owner's review load | medium | 4 calibration and 10 held-out sheets; compact route strips; prefilled forms |
| K12 | production integration under GEO-3 (c) is large (unfreeze, `H1` inside the land, `U2` channel, downstream re-measurement) | certain if adopted | planned explicitly in WP11; nothing now |
| K13 | solo time | medium | iterations ≤ 500 lines; optional packages deferred; a stop point at every gate |

### 11.2 Performance constraints

§9.5. Unity batch start-up costs about 20 s per process (research §2.3), so several variants run per process. All
budgets are reported and become gates only if the owner says so.

### 11.3 Rollback

- **G3f artefacts** are Editor-only files referenced by nothing, with outputs outside the repository; rollback is a
  `git revert` of G3f's commits. Production, G3e and G3c files are protected by the manifest, the pinned hashes and the
  `git diff` check, so there is nothing to restore there.
- **Documents** are append-only history: a rejected proposal stays, annotated, never deleted
  ([001](../../../001%20Documentation%20Standards.md)).
- **A later production switch** (outside G3f): one commit changes the authority's composition, `GeneratorVersion` and the
  pinned hashes; reverting it restores `H4A` exactly. No dual path is kept — one production path (AGENTS.md).

---

## 12. Documentation changes required

| Document | Change | When | Proposed version / status |
|---|---|---|---|
| `unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md` (new) | this proposal, with the owner's decisions recorded verbatim | first authorised step — **done 2026-10-25** | v0.2, Draft — phase plan as approved |
| `unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md` (new) | WP1 | before any code | v0.1 Draft → approved at G-3 |
| G3f calibration review, report (new) | WP8–WP10 | later | Draft |
| [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md) | a G3f paragraph (**done 2026-10-25**); the stale heading and "GEO-1 … GEO-8" only with D-12 | first step; D-12 | 0.33 |
| [99 Open Questions](../../../99%20Open%20Questions.md) | GEO-3: a G3f line; new **GEO-10** (ownership model: drainage units, regions, regional assembly) and **GEO-11** (the terrain authority's form: may it be a global lattice solve rather than a point function, and does law 11 of 17 govern the terrain height field — D-8, D-9) (**done 2026-10-25**) | first step | 0.53 |
| [90 Glossary](../../../90%20Glossary.md) | Geography Plan, Drainage Unit, Boundary Contract, Write Footprint, Stress Fixture — only once approved | G-3 | minor version |
| [AGENTS.md](../../../AGENTS.md) | "Proposed Decisions Awaiting Owner Review": the G3f proposal; after each gate what is authorised; Next Design Priority item 6 | first step (**done 2026-10-25**), then per gate | 0.78 … |
| [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) | law 11's scope; the W.2 data-flow line | only on the owner's decision D-9 (a locked law) | 0.9 |
| [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) | §9 roadmap row for G3f (**done 2026-10-25**); a results section at the end | first step; G-6 | 0.19 … |
| [GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md), [GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) | the stale header line; D9 "uncommitted" annotated | optional (D-12) | 0.5.1 each |
| [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md), [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) | none in G3f | — | — |
| `.cursor/rules/` | checked at G-3; changed only if the process changes | G-3 | — |

Every step runs the documentation transaction of AGENTS.md (Glossary, Open Questions, dependencies, versions, higher
documents, agent rules) and is committed and pushed as one step (Git History Duty).

---

## 13. Owner decisions required before execution

| # | Decision | Options | Recommendation |
|---|---|---|---|
| D-1 | the phase | approve G3f (working name RGA), its scope (§1) and gates (§10.1) / revise / reject | approve — **approved 2026-10-25** |
| D-2 | the architecture hypothesis | hybrid of §4 to be tested; regions as ownership units, tiles as representation | approve as a hypothesis, not as a decision — **approved 2026-10-25 as a hypothesis** |
| D-3 | **the fluvial family with a changed law** (L-1 … L-7) | authorise it as G3e-PM §8's "changed S-LM3 law" line, with confirmation that Stage 3 §16's rejection of flow-derived structure does not apply at the experimental level to a tree built from a designed plan / decline | authorise; the evidence is E6–E7. If declined, U-P becomes the candidate law |
| D-4 | candidate set | C0 + U-F + R-F; U-P optional | C0 + U-F + R-F; U-P only as the fallback |
| D-5 | domain and fixture | contour A, one 513² window chosen in WP0, a fixture authored by the agent from the G3e record, seeded variants; WP-O2 optional | approve; WP-O2 only if the owner wants a second topology early |
| D-6 | criteria to freeze in WP1 | §9.8's new rows: regional gates, process gates, diagnostics, form, pass and decision rules, caps, seeds; 1005–1024 untouched | approve the list for WP1 to specify |
| D-7 | commit policy | each documentation step and each code package to `main` (Editor-only, isolated) / a research branch | `main`, as the G3e backlog was; the freeze record cites a commit |
| D-8 | representation | allow a global lattice solve for the research candidates now; production at G4 | allow for research (finding 7) |
| D-9 | law 11 of 17 | clarify that Piece-Based Assembly governs placed content (settlements, buildings, interiors, props) and the terrain height field follows AGENTS.md's terrain stack / state otherwise | clarify; it is a locked law, so only the owner can |
| D-10 | visual review | blind with a sealed key; references labelled; nominal blinding accepted; second light preset yes/no | blind; one light preset |
| D-11 | budgets | §9.5 as reported diagnostics / as gates | reported diagnostics |
| D-12 | housekeeping | apply the remaining stale-line fixes (§2.2 row 9) as their own commit / leave | apply, separately |
| D-13 | **the first authorised step** | §14.1 steps A–C (documentation and read-only checks; no code) | approve A; B and C may follow in order — **A authorised 2026-10-25; B and C open** |

If any decision stays open, nothing that depends on it starts; the agent reports the blocking item and waits.

---

## 14. Practical first slice — executed only after authorisation

### 14.1 First authorised step (no code, no generation)

Each sub-step is one commit and push, and starts only when the owner has authorised it:

- **A. Record the phase.** Check that `main` equals `origin/main` and the tree is clean; read AGENTS.md and this plan.
  Place the approved text at `unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md` with a `.meta` (fresh GUID, so Unity does
  not write one during a batch run) and a section quoting the owner's decisions verbatim; update 14, 99 (GEO-3,
  GEO-10, GEO-11), AGENTS.md and the research record's §9; apply D-12 only if approved, in its own commit; run the
  documentation transaction; commit and push.
- **B. WP0** exactly as §7 describes: Python 3 with numpy in `research-data\g3f\wp0\scripts\`, scripts hashed before
  running, inputs verified before and after, two runs compared; nothing in the repository changes.
- **C. WP1** — the technical specification with the analytic checks; present it for approval (G-3). Nothing further.

### 14.2 First code slice — WP2, plan only (after G-3)

**Preconditions.** The specification approved and its commit pushed; the authorisation of WP2 recorded in
`research-data\g3f\gates\`; `git status` clean; the 85-file manifest snapshot written to
`research-data\g3f\protected_md5_before.txt`.

**Files** (§8.1): `TerrainGenG3fPlan.cs` (2a, ≤ 500 lines), `TerrainGenG3fValidator.cs` and
`Tests/TerrainGenG3fPlanTests.cs` (2b, ≤ 500), `TerrainGenG3fDump.cs` with the `plan` step only (2c, ≤ 300).

**Shape** (illustrative; WP1 fixes the fields):

```csharp
public enum G3fRegion : byte { Sea, Mountain, Upland, PlateauTop, PlateauBand, Ramp, Corridor, Lowland, CoastLow, CoastRocky, CoastCliff, Context }
public enum G3fBoundaryType : byte { Divide, FlowCrossing, RegionBlend, Shore, Sector }
public sealed class G3fPlan
{
    public int Seed; public int N; public float Cell; public RectInt Window;
    public bool[] Land; public byte[] Region; public int[] Unit;          // one region and one unit per vertex
    public List<G3fNode> Nodes; public List<G3fUnit> Units; public List<G3fBoundary> Boundaries;
    public List<G3fPlateau> Plateaus; public List<G3fAxis> ValleyAxes;
    public List<G3fReserve> Reserves; public List<G3fRoute> Routes;
    public string Hash;                                                  // SHA-256 of the canonical serialisation
}
public static class G3fPlanBuilder { public static G3fPlan Instantiate(LmcContour contour, int seed, int plateauClass); }
public static class G3fValidator   { public static G3fValidation Run(G3fPlan plan); }  // P1–P8, values recorded
```

**Validator P1–P8** (values fixed by WP1): P1 coverage — every sector land vertex has one unit and one region; P2 mask —
land equals contour A's; P3 topology — every unit one outlet, the unit graph a forest rooted at sea outlets, outlets on
low shore; P4 footprints — plateau write footprints exclusive, ramp on land for its full length, landing away from cliff
bands; P5 features — culminations in the mountain region ≥ 450 m apart, saddles below their culminations, divides on
unit boundaries, axes inside one unit and directed to its outlet; P6 budgets — region shares, plateau top area, rim
shares, ramp width; P7 coast — an intent for every shore vertex, cliff only behind high regions, outlets and valley
mouths on low segments, band widths bounded; P8 determinism — the same plan hash twice.

**Tests.** One EditMode test per check that fails on a synthetic plan violating it; one valid synthetic plan; the
fixture instantiated twice gives the same hash. The whole EditMode suite stays green.

**Commands** (no editor open on the project; `{n}` = 1, 2):

```
"C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -projectPath D:/Hobby/game-dev/game-dev/unity
  -runTests -testPlatform EditMode -testResults <RD>/g3f/wp2/tests_{n}.xml -logFile <RD>/g3f/wp2/tests_{n}.log

"C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -quit -projectPath D:/Hobby/game-dev/game-dev/unity
  -executeMethod GameDev.EditorTools.TerrainGenG3fDump.DumpBatch -g3fStep plan -g3fSeeds 110201,1337
  -g3fProtected <RD>/g3f/protected_md5_before.txt -g3fOut <RD>/g3f/wp2/plan_{n} -logFile <RD>/g3f/wp2/plan_{n}/unity.log
```

`<RD>` = `D:/Hobby/game-dev/research-data`. The step refuses an output inside `Assets` and any seed outside the
calibration list.

**Outputs** per seed and plateau class: `plan_<seed>_<class>.json` (canonical) and its hash; `validator_<seed>_<class>.txt`
(P1–P8 with values); `units_*.i32` and `regions_*.u8` (window rasters); `plan_*.png` — land shaded by region, unit
boundaries, divides, axes, outlets, plateau footprint, ramp path and landing, the settlement reserve, routes, shore
intents and the sector boundary.

**Complete when:** P1–P8 pass on 110201 and 1337 for both plateau classes with at most 2 logged plan corrections
(otherwise S2); every check has its failing test and the suite is green; two processes give byte-identical outputs; the
manifest, the pinned contour-A hash and `git diff --quiet -- unity/Assets/Scripts/World` hold; `git status` shows only
the new G3f files; the owner has seen the plan images. Commit and push only on the owner's word.

**Not allowed in this slice:** any height synthesis (the control included), any change to G3e, G3c, G0, G1 or production
files, any held-out seed, any scene save, any output inside `Assets`.

---

## 15. Verification of this proposal

Read for this proposal: the documents listed under Depends On; the code cited in §2.2; run 11's calibration records
(timings) and G3e-PM's views (the plan and overview sheet of 110201); the concept image. Nothing was generated, run,
changed, committed or pushed; no held-out seed was read; the repository is as it was at `fae8f28`.

**v0.2 (step A, 2026-10-25).** This document was placed in the repository with the decision record above; 14, 99
(GEO-3, GEO-10, GEO-11), AGENTS.md and the research record's §9 point to it. No code, terrain, Unity run, read-only check
or production file was touched; the audit's remaining stale lines (§2.2 row 9) were left for D-12.
