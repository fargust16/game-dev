# Geography-First Terrain Research — Expressive Fantasy Geography

Version: 0.19

Status: Draft — research record. Owner decisions that apply: **GEO-6 answered (a), build both**
(2026-10-18), so the read-only tooling G0 and G1 now exists as Editor-only code
(`Editor/TerrainGenGeographyDump.cs`, `Editor/GeometryFirstReview.cs`) and its measurements are in §§3.6,
7.6 and 10; and **the target geography D6, approved 2026-10-20** in
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) §9 from the owner's concept image (§13), which
supersedes the §4 *proposal* as the target wherever they differ. **G3 ran as owner-commissioned research on
2026-10-19** (§11): one candidate mountain system, isolated in an ~~uncommitted~~ Editor-only research file *(committed 2026-10-24)*,
measured against `H4A`; it approves nothing. **G3b followed on 2026-10-19 … 20** (§12): a converging valley
tree on the same skeleton, in the same file; it approves nothing either. **G3c ran on 2026-10-20 … 21** under
the owner's go-ahead (§14; brief §8.3, D8): neither contour passed the pre-registered rule; it approves
nothing. **The post-G3c review (2026-10-21, §15)** recorded G3c as completed research that failed acceptance,
settled GEO-1 and GEO-2, classified GEO-3 … GEO-8, added GEO-9, and proposed a landform-model comparison
~~that is not authorised~~. That comparison ran as **G3e (LMC)**: Step 1 (2026-10-23 … 24) ended not ready and
S-LM3 was closed with a negative result; the post-mortem checks **G3e-PM** then stopped the S-LM1 line under their §8 —
registered D1 INCONCLUSIVE, H FAIL under §7, DEV-1 a one-run deviation (§16). No G3e model is a production candidate;
production stays `H4A`. On 2026-10-25 the owner approved the phase **G3f** — regional geography architecture
([phase plan](GEOGRAPHY_G3F_PHASE_PLAN.md)) — and its architecture as the hypothesis to test; only its step A is
authorised (§9). The stage responsibility map (§5) and the rest of the roadmap (§9) remain
**proposals awaiting owner review**; the classification of the acceptance suite (§7) follows GEO-7's
decision record. The footprint attribution of §§11–12 was corrected on 2026-10-20 (§13.2).

Last Updated: 2026-10-25 (v0.19: the G3f phase plan approved — a status sentence and a §9 row; v0.18, 2026-10-24: the status paragraph brought up to G3e and G3e-PM; v0.17: G3e-PM ran — registered D1 INCONCLUSIVE, H FAIL by §7, DEV-1; the S-LM1 line stopped, §16; v0.16: the post-mortem checks G3e-PM pre-registered, plan approved, not run, §16; v0.15: G3e Step 1 ended not ready, §16; v0.14, 2026-10-23: LMC named G3e, C8 and C9 decided, §§9, 15; v0.13: LMC specification v0.4 approved, §15; v0.12: LMC Step 1 conditions C2–C5 prepared, §15; v0.11: LMC design note approved, Step 1 readiness check, §15; v0.10: LMC technical specification approved, §15; v0.9: LMC technical specification and its check on G3c data, §15; v0.8: LMC design note v0.2 recorded, §15; v0.7: post-G3c review, §15; v0.6: G3c results, §14; v0.5: G2 outcome and corrections, §13)

Depends On:
[AGENTS.md](../../../AGENTS.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[16 Architecture Pivot](../../../16%20Architecture%20Pivot.md),
[17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md),
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md),
[NATIVE_ISLAND_SHAPE_PREFLIGHT.md](NATIVE_ISLAND_SHAPE_PREFLIGHT.md),
[LANDMASS_T1_FEASIBILITY_AUDIT.md](LANDMASS_T1_FEASIBILITY_AUDIT.md),
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md)

Referenced By:
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md),
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md),
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md),
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md),
[GEOGRAPHY_G3F_PHASE_PLAN.md](GEOGRAPHY_G3F_PHASE_PLAN.md),
[99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Geography-first world shape,
[90 Glossary](../../../90%20Glossary.md) § Geographic Hierarchy and § Geometry-First Review,
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[AGENTS.md](../../../AGENTS.md) § Proposed Decisions Awaiting Owner Review,
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) §24,
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) items 16 and 22

---

## 0. Reading this document

**The question.** The baked island reads as a relatively flat island with one convex elevation. The
target is an anime-fantasy open world whose *geography* — mountain systems, ridges, foothills, valleys,
ravines, cliffs, plateaus, lowlands, coherent drainage, varied coasts — is convincing in a neutral,
untextured preview. What does the system do today, why does it produce what it produces, and what is the
smallest responsible next experiment?

**Short answer.**

1. The production height is `H4A = H1 + U2 + R4A`. `H1` is a pure function of distance to the coast (a
   24 → ~73 m dome whose contours are inset copies of the coastline), `U2` is **at most one unbranched
   lens-shaped massif** on the island (none on 3 of the 7 canonical seeds), and `R4A` adds ≤ 18 m plate
   steps, a ≤ 4.7 m coastal floor and an apron ring — and **zero knolls**. Ridges, spurs, valleys and
   basins exist only as structural data (Stage 3A–3E) or in an experimental carve (Stage 3F). That
   composition *is* "a flat island with one convex elevation" (§1.3).
2. The bake, the resolution and the materials are **not** the cause. The bake reproduces the field
   within 1.07 cm; 2.73 m lattice spacing is ample for macro- and meso-scale geography; ~90 % of the bake
   time is TerrainGen evaluation, not rendering, surfaces or vegetation (§2).
3. Most of the missing geography is **upstream of Stage 3F**. Stage 3 is subtractive and keyed to the
   one massif's frame; it can cut valleys into an existing envelope, never raise a ridge network, a
   second high region, or a lowland trunk valley that has no massif above it (§6).
4. The island is small for multi-range geography: 2.6–3.2 km² of land, inscribed radius 410–781 m, and
   54–59 % of the land within 200 m of the coast where every relief term is gated toward zero. Scale and
   the coast gate are owner-level constraints, not tuning (§1.4, 99 Open Questions GEO-1, GEO-4).
5. The smallest useful next steps are read-only: a geography metrics baseline (G0) and an isolated,
   heights-only Geometry-First Review tool (G1). Neither touches production (§9). **Both are built**
   (GEO-6 (a)); the baseline is in §7.6: on all seven canonical seeds the island's drainage has at
   most Strahler order 2, no saddle lies on the main divide, the declared crests and spurs coincide
   with a water-parting line on 0.6–2.0 % of their length, and the single-dome detector flags five of
   seven seeds — the other two by one indicator each.

**Claim tags.**

- **[MEASURED]** — measured in this research on 2026-10-18 (Unity 6000.5.7f1, batch mode; §10).
- **[PRIOR]** — measured earlier; the source document is named. Not re-run here unless stated.
- **[FACT]** — read from code or a document; the file or symbol is named.
- **[OBSERVED]** — directly visible in a reference image (§4.1).
- **[INTERPRETED]** — a reading of observed material, not a measurement.
- **[CONCLUSION]** — follows from the tagged material above it.
- **[HYPOTHESIS]** — believed, not demonstrated; names how it would be checked.
- **[PROPOSAL]** — a recommendation that needs an owner decision. Never an approved requirement.
- **[OPEN]** — an owner decision; recorded in [99 Open Questions](../../../99%20Open%20Questions.md).

**Status words.** *Production* — on the path `TerrainAuthorityField` → bake. *Frozen* — pinned by
hashes or by an explicit freeze; changes need an owner-approved, scoped unfreeze. *Experimental* —
dump-first code or out-of-repository prototypes, never on the production path. *Proposed* — exists only
in documents. *Approved* — accepted by the owner (cited where it applies).

---

## 1. Current-state audit

### 1.1 The production terrain authority, as it runs

**[FACT]** `World/Terrain/TerrainAuthorityField.cs` builds Stage 1, Stage 2A, every accepted massif and
Stage 4A's `IslandRelief`, then evaluates `IslandElevation.At`:

```
H4A(x, z) = H1(d)           Stage 1: CoastHeightModel over signed coast distance d only
          + U2(x, z, d)     Stage 2B: MassifUpliftModel (smooth max over accepted massifs, coast-gated)
          + R4A(x, z, d, H1, U2)   Stage 4A: M(d) + Γ·(P + K) + A
```

`OverworldTerrainField.FromAuthority` samples that point function at every vertex of the one
1025 × 1025 world lattice (2.734375 m), an optional hydrology carve lowers the lattice (off by default),
and `OverworldTerrainBuilder` writes heights, surface weights, trees and ground cover. Nothing flows
back. Contract: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).

**[MEASURED]** Seed 110201 bake: world signature `656405ea477a`, equal to the signature pinned in
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) item 19; height against TerrainGen max 1.07 cm over
1 052 676 samples; four seams 0.0000 m. Stage 4A dump: `H4A` hashes 110201 `e2bad571f4af76c7` and 1337
`f86e8c15d7105be2`, equal to the pinned values. Production is in the state the documents describe.

### 1.2 Stage responsibilities and status

| Stage | Responsibility | Representation | In production height | Status |
|---|---|---|---|---|
| **1** landmass | crustal partition (200 m sites), land growth to 32–40 % of the world, coastline, `H1 = CoastHeightModel(d)` | analytic signed distance + lattice | **yes** | frozen — golden hashes on 5 seeds |
| 2 belts (`Stage2MountainGenerator`, `UpliftBeltBuilder`) | superseded belt model **with branch ranges** (`MaxBranchesPerRange` 2, crest 0.45–0.70 of parent) | — | no | historical; dumps and tests only |
| **2A** macro structure | plates at 1 100 m spacing (3–4 on land), convergent boundaries, certified axes; `MaxSystems` 2, `MinSystemSeparationMeters` 600, usable half-width ≥ 200 m, 200 m coast clearance | curves | indirectly (axis = massif spine) | hardened contract ([STAGE_2A_SELECTION_HARDENING.md](STAGE_2A_SELECTION_HARDENING.md)) |
| **2B** massif | `MountainMassif` per accepted axis: one crest line, ≤ 2 culminations, crown + crest plateau then one smoothstep flank; `U2` gated to zero at the shore | analytic | **yes** | field hashes pinned by Stage 4A S6; every Stage 3 input reads it |
| 3A–3B | ownership, flanks, divides; primary spurs and corridors per massif flank | curves | no | frozen |
| 3C | valley topology and `FinalValleyTrajectory` (from massif spurs only) | curves | no | frozen; API-only unfreeze spent by 3F F1 |
| 3D | secondary spurs (ribs) | curves | no | frozen |
| 3E | cirque candidates, structural basins (enclosure + outlet, declared on `H2`) | polygons | no | frozen; depth bound B4 open |
| **3F** | carve `H3F = H4A − C3F` (`CarveField`, `CarvedElevation`), `Stage3FVerification`; E family prototyped outside the repository | analytic, lattice | no | **experimental**, dump-first |
| **4A** island relief | `M(d)`, plate-level districts (+18 m, +25 m without a massif), knolls (τ-capped), massif apron | analytic | **yes** | **production authority** (R1, `GeneratorVersion` 2) |
| 4B / 4C | T2 plateaus, T3 outcrops; coastal character | — | — | proposed in documents, not started |
| Hydrology | priority flood → D8 → reaches, lakes, waterfall candidates; lattice carve | lattice | only with `RunHydrology` (off) | built, off by default |
| Bake | heights, surface weights, trees, ground cover | `TerrainData` | — | production |

**[FACT]** The production massif is unbranched: branch ranges exist only in the superseded belt model
(`UpliftBeltBuilder` is reached from `Stage2MountainGenerator`, which only dumps and tests call).
**[FACT]** Stage 3 is subtractive by design (`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §3.2): ridges are the
remnants of the Stage 2 envelope between carved valleys; outside the massif Stage 3 may cut at most
`CorridorIncisionMeters` = 7.2 m.

### 1.3 What produces the central convex elevation

**[MEASURED]** Seed 110201, `H4A` (Stage 4A dump, §10):

| | value |
|---|---|
| land height p5 / p25 / p50 / p75 / p90 / p99 | 25 / 39 / 58 / 78 / 108 / 326 m |
| land in 24–50 / 50–70 / 70–110 / 110–230 / 230+ m | 39.7 / 25.3 / 25.3 / 5.9 / 3.7 % (the last measured by G0; first derived as the remainder, 3.8 %) |
| highest point | 364.8 m (single massif, crest uplift 284 m) |
| knolls placed | 0 of 48 candidates |
| plate districts | 4, levels 0 / 0.33 / 0.67 / 1.0 → 0 / 6 / 12 / 18 m |
| coast-aligned gradient on the interior floor | 73.6 % (`H2`: 97.4 %) |
| floor local relief, 112 m window, median / p90 | 4.19 / 8.78 m |
| land < 5° / > 45°; reachable from the shore at ≤ 45° (dump method) | 37.2 % / 5.7 %; 92.7 % |
| curvature ratio (isotropy) | 0.759 (gate 0.85–1.20) |

Seed 1337 (no massif): highest point 85.4 m, p90 78 m, no land above 110 m, 3 districts, no valleys.

**[MEASURED, image]** The 5 m contour image of `H4A` on 110201 shows nested coast-parallel rings with
straight medial-axis creases across the floor and one oval massif ringed by its apron — the
"extruded coastline plus one mound" signature `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4 diagnosed on `H2`.

**[CONCLUSION] Causes, in order of weight:**

1. **The datum is a dome by construction.** `CoastHeightModel.HeightAt(d)` rises 26 m over the first
   200 m and saturates +30 m over the next 600 m. A monotone function of coast distance has its maximum
   along the island's medial axis and no interior minimum. Every contour is an inset of the coast.
2. **One lens massif.** On the canonical worlds the island holds room for one certified axis: the axis
   must keep a 200 m-half-width body 200 m clear of the coast on both sides (`LANDMASS_T1_FEASIBILITY_AUDIT.md`
   §1), and a second axis must sit 600 m away. Measured outcome **[PRIOR]**: 25 of 29 worlds carry one
   massif, 4 carry none, none carry two. The massif holds full crest across a crown and falls ~284 m in
   one smoothstep (`CrownFraction` 0.16, `CrestPlateauFraction` 0.88), so it reads as a mesa with a
   continuous > 45° ring (91–92 % reachability, `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4).
3. **Stage 4A is deliberately bounded floor relief.** Positive only, shore-gated, 18 m plate steps,
   knolls capped by the inland slope budget (0 placed on every canonical seed), apron ≤ 0.18 × crest
   around the massif foot. It breaks the 70–110 m gap (4.6 → 25.3 %) and the coast alignment
   (97 → 74 %), but it adds no landform tier and, by design, no ridge, valley or second high region
   (`PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md` §§4, 9).
4. **Structure that would read as geography never reaches the height.** 7 primary spurs, 9 valley
   trajectories and 5 declared basins per massif world exist as curves (Stage 3A–3E). Stage 3F, which
   would realise them, is experimental (§6).
5. **Drainage has nothing to converge on.** **[PRIOR]** On the `H2` relief of 110201 the hydrology
   analysis found 8 short coastal rivers, 0 tributaries, 0 lakes, 2.05 km of river, and soil weight
   0.001 on land (`HYDROLOGY_GENERATION_PLAN.md` §17; `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4).
   **[MEASURED] on `H4A` by G0** (§7.6): 110201 has 8 systems, 0 tributaries, 0 lakes and 1.95 km of
   river — Stage 4A changed almost nothing here; across the seven canonical seeds 5–11 systems,
   0–4 tributaries, Strahler order ≤ 2, 0.84–2.51 km of river.

### 1.4 Limitations by kind

| Kind | Limitation | Evidence | Owner |
|---|---|---|---|
| **Generation model** | coast-distance datum; one unbranched massif; floor relief bounded to ≤ 18 m steps; no positive ridge network; subtractive Stage 3 keyed to the massif frame; no T2/T3 tiers (4B not started); uniform coast (4C not started) | §1.2–1.3 | Stage 1, 2A/2B, 3A–3C, 4B/4C |
| **Scale** | 2.61–3.16 km² of land, inscribed radius 410–781 m **[PRIOR]**; the coastal band `d < 200 m` holds 54–59 % of land **[PRIOR, prototype]** and every relief term is gated toward zero inside it; relief uses ~365 m of the 700 m budget | `LANDMASS_T1_FEASIBILITY_AUDIT.md` §3; Stage 4A design §2 | owner (99 Open Questions GEO-1, GEO-4) |
| **Sampling resolution** | not limiting for macro/meso forms: a 100 m-wide ridge is 37 lattice steps; limiting for channels (< ~8 m cannot be terrain) and for vertical walls (a 20 m wall over 1–2 steps is 74–82°) | `PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md` § World scale; `HYDROLOGY_GENERATION_PLAN.md` §19 | — (a property, not a defect) |
| **Topology** | valleys exist only on massif flanks (B8: massif-free worlds have none); no inter-massif corridor concept (B2b); declared trajectories cross rising ground (36 route closures on `H4A`, seeds 1000–1039) | 3F preflight §§9, 18; handoff §9 | Stage 3B/3C (frozen) |
| **Hydrology** | two unreconciled carves: Stage 3F along declared trajectories, `HydrologyBake` along D8 reaches; no tributary structure on the dome; the documented policy is fill-then-spill with every system reaching the ocean | `HYDROLOGY_GENERATION_PLAN.md` §§9, 11; §6 here | Stage 3F × hydrology |
| **Bake** | none for shape (≤ 1.07 cm, exact seams). Cost: authority evaluation dominates | §2.1 | — |
| **Presentation** | the only review camera is a 2 800 m top-down view; dumps are top-down analytic images; production textures, vegetation (11 747 trees, 3.9 M detail instances on 110201) and the ocean plane hide or confound geometry; no standardised oblique or eye-level capture existed before G1 (§3.6) | `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4 C/M9; §2 here | tooling (G1, built) |

### 1.5 Experiments that can run without changing production

All of these have precedent in the repository's own research practice:

- **TerrainGen dumps** (`TerrainGen*Dump` batch entry points) evaluate any stage on the 1025 lattice and
  write images and reports outside `Assets`, without `TerrainData` or a scene. `TerrainGenIslandReliefDump`
  already accepts `-reliefOverrides` for experiments on Stage 4A's levers.
- **Standalone Unity projects** holding copies of `World/TerrainGen` (the Stage 3F phases 2–10 method,
  handoff §12): prototypes run against real structure with hash checks, nothing enters the repository.
- **Raw lattice export** (`OverworldTerrainValidation.RunBatch -terrainRaw true`) for offline analysis.
- **Heights-only writes** through public `OverworldTerrainGrid.Create` + `OverworldTerrainBuilder.WriteHeights`
  into tiles that are never saved — the Stage 3F phase 7 method — so any research lattice can be viewed
  and physically tested in Unity without the bake (§3.3).

---

## 2. Where the time goes — measured

### 2.1 Production bake, seed 110201 **[MEASURED]**

`OverworldTerrainValidation.RunBatch`, fresh batch process:

| Step | ms | share of the surface step |
|---|---|---|
| `terrain.authority` — TerrainGen build + 1 050 625 point queries onto the lattice | **22 876** | 90.4 % |
| `terrain.heights` — `SetHeights` on four tiles | 35 | 0.1 % |
| `terrain.derived` — slope, convexity, D8 drainage on the lattice | 158 | 0.6 % |
| `terrain.alphamaps` — surface classification | 539 | 2.1 % |
| `terrain.trees` | 49 | 0.2 % |
| `terrain.details` — ground cover | 245 | 1.0 % |
| surface step total (incl. grid creation, asset save) | 25 304 | 100 % |

The validation itself re-evaluates the authority for its height comparison (another TerrainGen build and
lattice pass, not separately timed); the whole process took 75 s wall time including editor start-up.

### 2.2 TerrainGen-only dump **[MEASURED]**

`TerrainGenIslandReliefDump.DumpBatch`, seeds 110201 and 1337, one process (62 s wall time):

| | 110201 | 1337 |
|---|---|---|
| Stage 1 + 2A + 2B fields (`stage2B` in the report) | 7 435 ms | 5 999 ms |
| Stage 3 structure | 38 ms | 0 ms (no massif) |
| Stage 4A build (junction certification dominates) | 6 813 ms | **19 357 ms** |
| Stage 4A relief lattice | 718 ms | 107 ms |
| per-part lattices (diagnostic only) | 888 ms | 277 ms |

### 2.3 What follows

- **[CONCLUSION]** Removing textures, vegetation, water and post-processing from a review saves at most
  ~0.8 s of a 25 s surface step. A neutral material is a *legibility* change, not a speed change.
- **[CONCLUSION]** The TerrainGen-only path evaluates the same `H4A` (identical hash) in ~15 s on 110201
  against ~23 s through `FromAuthority`. The difference (≈ 8 s, derived by subtraction, not instrumented)
  is the per-vertex point-query pass, which recomputes the signed coast distance for every vertex instead
  of reusing Stage 1's lattice. The geometry-first loop should run on the dump path.
- **[PRIOR]** Stage 4A junction certification costs 3.6–39.5 s per seed (tech debt item 20); Stage 1's
  field ~6 s. These, not rendering, bound iteration speed.
- **[CONCLUSION]** Editor start-up and compilation (~20 s per batch process, derived from 62 s wall time
  minus 41.6 s of reported stage time, which also excludes image writing) make **several seeds per
  process** cheaper than one process per seed.

No optimisation is proposed for production here. Any later optimisation must keep every pinned hash
(tech debt items 16, 19, 20).

---

## 3. Geometry-first workflow (proposal)

### 3.1 Requirements for the diagnostic presentation

From the brief, restated as checkable properties of a **Geometry-First Review** ([90 Glossary](../../../90%20Glossary.md)):

1. Neutral untextured material, optionally a height or slope colour ramp; never the production layers.
2. No trees, ground cover, props, buildings, roads, POI visuals or player.
3. No terrain shader features, no post-processing volume, no fog, no reflection probes.
4. No rendered water surface. Sea level is drawn as a thin contour; river and lake data are overlays.
5. One fixed directional light (the production sun direction, `Euler(42, −35, 0)`), constant ambient,
   shadows on (shape reading needs them).
6. A fixed camera set per seed (§3.4), fixed FOV, fixed resolution, deterministic file names.
7. Overlays available on demand: elevation bands, slope classes, contours, watershed boundaries, D8
   direction / accumulation, ridge and valley lines, declared Stage 3 structure, outlets, depressions.
8. Input is **any lattice**: production `H4A`, a Stage 3F candidate, or an experimental field. The review
   never decides a height.

### 3.2 Options considered

| Option | What it is | Verdict |
|---|---|---|
| A. TerrainGen dumps only | top-down hillshade, slope, contour and structure PNGs (exists) | **keep as the first gate**; insufficient alone: no oblique, silhouette or eye-level reading |
| B. **Isolated review tool** | an Editor-only tool that builds tiles in memory with `OverworldTerrainGrid.Create(…, layers, neutralMaterial)`, writes heights with `OverworldTerrainBuilder.WriteHeights`, never calls `PaintSurface`, `PlaceTrees`, `PlaceGrass` or the ocean step, renders the camera set to PNG outside `Assets` and discards the scene | **built** after GEO-6 (a) — `GeometryFirstReview` (§3.6) |
| C. Toggle the production scene | `drawTreesAndFoliage = false`, hide `WorldWater`, swap the material, disable the volume in `Overworld_Generator.unity` | **rejected** — edits a versioned scene that already churns (tech debt item 7) and risks shipping a diagnostic state |
| D. A "diagnostic" flag in the bake | skip vegetation inside `OverworldTerrainBake` | **rejected** — a second, optional bake path; AGENTS.md forbids optional bake paths |

**[FACT]** Option B needs no production code change: `OverworldTerrainGrid.Create` already takes the
material and layer array, `WriteHeights` and `OverworldTerrainField.FromSamples` are public, and the
Stage 3F phase 7 research wrote heights onto tiles exactly this way. **As built** it added two Editor
files and no asset: the neutral layer, texture and material exist in memory only, and no diagnostic
shader was needed (§3.6).

### 3.3 Proposed data flow

```
seed, candidate id ──► TerrainGen dump path (lattice, ~15 s on 110201)
                         │  any WorldHeightField: H4A | H3F | E | experimental field
                         ▼
                 OverworldTerrainField.FromSamples (lattice in, no re-evaluation)
                         ▼
     in-memory tiles: OverworldTerrainGrid.Create(neutral layer, neutral material)
                         ▼
     OverworldTerrainBuilder.WriteHeights  (35 ms; heights only, nothing else)
                         ▼
     camera set + overlays ──► PNG + JSON (camera, hash of the input lattice) outside Assets
```

The tool reads the lattice it is given and writes nothing back. A run is identified by `(seed,
GeneratorVersion, candidate id, lattice hash)`, so two images are comparable exactly when their lattice
hashes and camera records match.

### 3.4 Standard camera set (proposal)

Cameras that compare algorithm versions on one seed must not move with the algorithm. They are therefore
anchored on **Stage 1 only** (frozen, golden-hashed): the coast polygon, its centroid `c`, its
equal-area radius `r`. Feature-anchored views are a secondary set.

| View | Placement (all heights above sea level) | Purpose |
|---|---|---|
| V1 plan | orthographic, top-down, whole world | silhouette, regions, overlays |
| V2 distant oblique | from `c + 1.8 r` at bearing 225°, 900 m high, looking at `c`, FOV 40° | island silhouette, mountain system against the sky |
| V3 opposite oblique | as V2 at bearing 45° | recognisability from a second side |
| V4 mid-distance | from the land point at bearing 180° and 0.6 r from `c`, 120 m above ground, looking at `c` | ridge, valley, foothill transition |
| V5 ground | eye height 1.7 m at the Stage 1 land point at bearing 270°, 0.5 r from `c`, looking at `c`, FOV 60° | scale of nearby landforms |
| V6 drainage | V1 with watersheds, D8 accumulation, channels, outlets, depressions, declared structure | drainage review |
| V7–V9 (secondary) | anchored on the highest point, the longest valley and the lowest saddle of the main divide | feature reading; may move between versions |

The bearings, distances and FOVs are **authoring defaults** to be confirmed on the first run; the rule
that comparison views anchor on Stage 1 is the substance of the proposal.

### 3.5 Research loop, fastest first

| Tier | Runs | Cost (110201) | Valid for |
|---|---|---|---|
| T0 structure | curves and polygons only (axes, spurs, trajectories, basins) | tens of ms (Stage 3 structure 38 ms) | topology, placement, counts |
| T1 lattice dump | TerrainGen field on the 1025 lattice + metrics + top-down images | ~15–26 s per seed **[MEASURED]** | every geometry and drainage metric |
| T2 review | T1 + heights-only tiles + camera set | T1 + ~0.1 s write + render (render cost unmeasured) | visual reading, served-surface checks |
| T3 production bake | full bake + validation | ~25 s + ~23 s **[MEASURED]** | integration only |

Rules for the loop:

- **Reduced resolution is preliminary.** A 513-sample world lattice may be used to rank candidates, never
  to accept one: the Stage 3F correction is not resolution-invariant and Stage 3C trajectories move by up
  to 3.7 m when `FieldResolution` changes (handoff §15). Structure builders must keep reading the
  canonical 1025 lattice; only the *evaluation* of a candidate height may be coarser.
- **Small test regions** are exact for point-function fields (`H4A` is a pure point function) and
  invalid for anything global — junction certification at build time, depression filling, flow
  accumulation. Drainage is always judged on the whole island.
- **Caching** is allowed only inside a dump process, keyed by `(seed, GeneratorVersion, settings hash)`,
  and only for frozen products (Stage 1 result, Stage 2A/2B structure). An on-disk cache must store the
  golden hash and be rejected on mismatch. No cache may feed the production bake: that would be a second
  terrain authority.
- **Seed suite.** The seven canonical seeds (110201, 424242, 20260905, 7, 1337, 256, 777) first, then
  the 1000–1039 sweep, as every earlier stage did. The no-massif seeds are mandatory: a geography that only
  works where Stage 2A certifies a massif fails on 4 of 29 worlds.
- **Automated checks before eyes.** T1 metrics (§7) gate T2 review; T2 review gates any document change.

### 3.6 Geometry-First Review as built (G1)

Built 2026-10-18 after GEO-6 (a). Entry `GameDev.EditorTools.GeometryFirstReview.RunBatch`
(`-geoReviewSeeds a,b`, `-geoReviewOut <dir>`, optionally `-geoReviewLattice <file> -geoReviewCandidate
<id>` for a 1025 × 1025 little-endian float32 lattice); menu *GameDev/Overworld/Terrain Gen/Geometry-First
Review G1*, which asks to save the open scene first and reopens it afterwards. The data flow is §3.3's:
`IslandReliefWorld` (dump path) → lattice (`H4A` or the file) → `OverworldTerrainField.FromSamples`
(nearest lattice vertex, nothing re-evaluated) → `OverworldTerrainGrid.Create` with one neutral layer and
material → `OverworldTerrainBuilder.WriteHeights` per tile → cameras → PNG + JSON. It opens an untitled
scene, never calls `PaintSurface`, `PlaceTrees`, `PlaceGrass`, the ocean step, `CreateAsset` or
`SaveScene`, and destroys everything it made.

| §3.1 requirement | As built |
|---|---|
| 1 neutral material | URP `Terrain/Lit`; one in-memory `TerrainLayer` over a 4 × 4 texture RGB 160/160/154 with **alpha 0**: `Terrain/Lit` and its basemap read smoothness from the diffuse alpha, and an opaque texture rendered the first run as specular plastic. Instanced per-pixel normals on. No colour ramp — the dumps already give top-down slope and contour images |
| 2 nothing but ground | `drawTreesAndFoliage` off; the scene holds the tiles, one sun, one camera and the contour ribbon |
| 3 no shader extras | post-processing, anti-aliasing, MSAA and HDR off on the camera; fog off, no skybox, reflection intensity 0, terrain reflection probes off |
| 4 no water surface | the seabed renders as neutral ground; the 24 m contour (marching squares on the lattice) is an unlit ribbon 0.5 m above sea level, 3 m wide in V1–V3, 1.5 m in V4, 0.5 m in V5 |
| 5 fixed light | directional `Euler(42, −35, 0)`, intensity 1.2, soft shadows, flat ambient 0.40/0.41/0.43. `URP_Pipeline.asset` holds `shadowDistance` 350 m, which leaves the distant views unshadowed, so the tool sets it per view **in memory** and restores it in `finally`; the asset is never marked dirty and its MD5 is unchanged (§10) |
| 6 fixed cameras | table below; every pose, near/far plane, resolution, shadow distance and a pixel hash go to `review_<seed>_<candidate>.json` |
| 7 overlays | V6: fill depth > 0.05 m orange, D8 channel cells (≥ 30 000 m²) blue, the G0 flow-divide skeleton red (main divide bright), hydrology reaches navy by Strahler order, ocean outlets yellow, massif crest lines and primary spurs magenta, Stage 3C trajectories cyan, Stage 3E basin outlines green |
| 8 any lattice | yes; the JSON carries the lattice hash, which for `H4A` equals the pinned hash |

| View | As built | Moves with the reviewed lattice |
|---|---|---|
| V1 | orthographic, 2048 × 2048, half-size 1400 m, straight down from (1400, 1500, 1400), north up | no |
| V2 / V3 | 1920 × 1080, FOV 40°, `c + 1.8 r` at bearing 225° / 45°, 900 m, looking at `c` on sea level | no |
| V4 | 1920 × 1080, FOV 50°, the Stage 1 land point at bearing 180°, 0.6 `r` (walked toward `c` until Stage 1 land), 120 m above the served surface, looking at `c` on sea level | camera height |
| V5 | 1920 × 1080, FOV 60°, the Stage 1 land point at bearing 270°, 0.5 `r`, eye 1.7 m above the served surface, looking level toward `c` | camera height |
| V6 | V1 plus the overlays | overlays |
| V7–V9 | not built (secondary, feature-anchored) | — |

The served surface is `Terrain.SampleHeight`. V4's FOV and look target, V5's level look, the ribbon
widths and the overlay colours are **authoring defaults** added at build; §3.4 left them open.

**[MEASURED]** Per seed the scene takes 0.14–0.19 s and the five renders plus V6 0.65–0.89 s, against
12–44 s of world build and ~0.6 s of drainage analysis: G1 adds about one second to a T1 dump. Two fresh
processes over the seven canonical seeds wrote 42 PNG and 7 JSON files that are byte-identical. The JSON
records the graphics device (Direct3D 12); identity is shown on one machine and driver, not across GPUs.

**[INTERPRETED — agent reading of V2/V3/V5, owner review pending]** On every massif seed the island is one
flat-topped lens (a mesa with a continuous steep ring) standing on a low dome, its foot ringed by the
apron step that V5 shows as a sharp lip (on 424242 a flat bench all round); on 424242, 20260905 and 7
the massif carries two culminations on one body, joined by a col far above the floor. The
massif-free seeds are a near-flat dome whose only visible structure is the concentric coastal-rise rings
and straight medial-axis creases. All seven read as §1.3's "flat island with one convex elevation".

---

## 4. Target geography specification (proposal)

### 4.1 Reference analysis — DragonSword: Awakening

**Sources, inspected 2026-10-18.** Official site gallery, 12 images
(<https://dragonsword-awakening.hound13.com/en/>); Steam community screenshots, the ten "most popular
(week)" items (<https://steamcommunity.com/app/4570720/screenshots/>); the Steam store page
(<https://store.steampowered.com/app/4570720/>), read for its text only — its one screenshot was not
inspected. The task brief mentions supplied screenshots; **none were attached to the task and none exist
in the repository**, so the public material is the only reference used. Of the 22 images inspected, 9 show
open landscape; the rest are combat, interiors or character art. No gameplay footage was reviewed. The
game is built in Unreal Engine 5 (store page); parity is not a goal.

**Update 2026-10-20.** The owner has since supplied and approved a **concept image** of the target island,
stored at [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp).
It is a **project reference inspired by DragonSword: Awakening's visual direction, not an official map
of the game**, and no fact about the game's geography is drawn from it. It is the source of the approved
target D6 ([GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) §9). The readings O1–O8 and I1–I7
below remain as recorded from the public material on 2026-10-18.

**[OBSERVED]**

| # | Image | Landform content |
|---|---|---|
| O1 | site 02 | grassy tablelands ending in near-vertical grey rock walls; a ravine crossed by a rope bridge; several tablelands stepping back at different heights; a central massif with steep faces and a landmark top; a far range of peak silhouettes on the horizon |
| O2 | site 11 | elevated view over a broad valley floor holding a city; ridges on both sides; mid-distance tablelands with cliff margins; a multi-peak range behind; a winding linear low feature on the right of the valley |
| O3 | site 10 | shallow turquoise shelf and sandy beach; coastal tablelands ending in cliffs close to the sea; the landmark massif behind |
| O4 | Steam community, harbour | a cove with terraced cliffs stepping down to the water; a rocky headland carrying a lighthouse; a ridge line behind; the minimap shows an island-shaped region |
| O5 | site 09 | a gorge floor between two cliff walls, a suspension bridge spanning it overhead |
| O6 | site 12, site 07 | lowland meadow and town ground; jagged peaks on the horizon; the landmark visible above the settlement |
| O7 | site 04 | a flat open floor ringed by rock pillars and steep ridges |
| O8 | Steam community, pond | a small water body at the foot of a rock face |

**[INTERPRETED]**

- I1 **Three distance planes.** Foreground ground, a mid-distance band of tablelands or ridges, and a far
  silhouette range read separately in O1, O2, O3, O6.
- I2 **Vertical relief concentrates in walls.** The dominant meso grammar is *flat top — near-vertical
  wall — flat floor* (tableland, escarpment, gorge), not rolling hills. Flat ground stays usable; walls
  are barriers and silhouettes.
- I3 **Mountains are not domes.** Crests are jagged, multiple and at different heights; no summit reads as
  a smooth lens.
- I4 **Coasts vary with what stands behind them.** Beaches front lowlands (O3); cliffs, terraces and
  headlands front uplands (O3, O4); coves sit between headlands (O4).
- I5 **Crossings are designed.** Gorges carry bridges (O1, O5): the barrier is intentional and the crossing
  is a place.
- I6 **The landmark orients.** The central massif is visible from the town, the coast and the tablelands
  (O1, O3, O6) — the BotW "visible landmark" rule already in `WORLD_RELIEF_PRINCIPLES.md` §4.
- I7 **Scale is compressed.** Landmark mountains sit close to towns and coasts; the reading depends on
  composition, not on real-world distances. This is what makes a small island viable at all (§4.5).

**[CONCLUSION]** The reference is consistent with grammar the project already wrote down and never
built: T2 plateaus as *flat top + wall + talus + ramp* (`WORLD_RELIEF_PRINCIPLES.md` §2, Stage 4B),
coastal character caused by the hinterland (`PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md` § Landmass 6,
Stage 4C), ridges and valleys as duals (Stage 3). What the reference adds is a **mountain system** rather
than a single massif, and **cliff coasts**, which the current coast gate forbids (§4.4, GEO-4).

### 4.2 The geographic hierarchy

A **Geographic Hierarchy** ([90 Glossary](../../../90%20Glossary.md)) is a set of levels in which each
level is caused by, and placed relative to, the level above it. Proposed levels, top to bottom:

| Level | Content | Spatial scale on this island | Caused by |
|---|---|---|---|
| 0 footprint | silhouette, land area, coast segments, islets | whole world | Stage 1 |
| 1 relief regions | highland core(s), uplands, lowland plains and corridors, coastal zones | 0.4–1.5 km | macro structure (plates, axes) |
| 2 mountain system | main divide with culminations and saddles; major ridges; subsidiary ridges and spurs; foothills at spur toes | ridges 0.3–1.5 km long | the divide tree of the system |
| 3 valley and drainage system | valleys between ridges; trunk valleys in lowland corridors; tributaries; ravines; headwater catchments; drainage divides | valleys 50–300 m wide | the complement of level 2 plus the lowland corridors of level 1 |
| 4 landforms | plateaus and benches, escarpments and cliffs, outcrops, knolls, enclosed basins | 40–450 m | level-1 regions and level-2 and level-3 margins |
| 5 coast | cliff coasts, headlands, coves, beaches and coastal plains, estuaries | 50–500 m of shore | what stands behind each shore segment |
| 6 water | channels incised in valley floors, lakes in declared basins, waterfalls at escarpments | metres to 100 m | levels 3 and 4, under the basin policy |

The scales are **authoring defaults** sized to today's footprint (§4.5); none is measured, and G3 checks
whether they fit. The levels are not the relief tiers T1–T3 and not the surfaces `H1`, `H2`, `H4A`.

### 4.3 Relationship rules (the grammar)

The rules are what separate geography from a list of features. Each is checkable (§7).

1. **One divide tree.** Every ridge belongs to a tree rooted on a main divide; crest heights descend
   away from the divide except at declared culminations. No orphan ridge.
2. **Valleys are complements.** Every valley lies between two ridges of the tree or along a level-1 lowland
   corridor, and heads at a divide, a saddle or a cirque. No headless valley (existing rule,
   `WORLD_RELIEF_PRINCIPLES.md` §3).
3. **Passes are saddles.** Each major divide carries at least one saddle markedly lower than its
   neighbouring culminations, with valley heads on both sides — that is what makes a pass usable.
4. **Foothills are spur toes**, not a ring around the mountain foot.
5. **Lowland corridors reach the coast** and carry the trunk valleys; plains open onto their own coast
   (Stage 4A already requires lowland frontage ≥ 150 m).
6. **Coasts are caused** (level 5): cliffs where level-1 uplands or level-2 ridges meet the sea, coves at valley
   mouths, beaches at lowland fronts.
7. **Landforms sit on regions.** Plateaus on uplands, escarpments facing lowlands, outcrops on tops and
   ridges; waterfalls where channels cross escarpments.
8. **Tier separation holds** — the 2.5× rule of `WORLD_RELIEF_PRINCIPLES.md` §1 between stacked forms.
9. **Drainage follows structure.** Watershed boundaries follow the divide tree; channels occupy valleys;
   every basin has an explicit policy (§4.6).
10. **Controlled variety, not a template.** A seed picks among a small set of archetypes and parameters
    within bounds (existing "controlled chaos", `WORLD_RELIEF_PRINCIPLES.md` §5). Candidate archetypes,
    for experiment only: *single spine*, *branched range*, *horseshoe around a central basin*, *twin
    highlands with an intermontane corridor*, *dissected plateau*. No archetype is prescribed by this
    document, and no single layout is required on every seed.

### 4.4 Composition and traversal

| Requirement | Geometric reading | Interacts with traversal |
|---|---|---|
| Distinct foreground / middle / distant forms (I1) | from V2–V5 the silhouette of the mountain system, the band of uplands and tablelands (levels 1 and 4) and the near ground separate | — |
| Recognisable mountain silhouettes from several sides (I3) | ≥ 2 culminations of different height; asymmetric flanks; skyline not a single lens from V2 and V3 | — |
| Valleys and ridges guide the eye | long axes of ridges and valleys lead toward the landmark or the coast | valleys are the natural routes |
| Plausible passages through mountains (rule 3) | saddles on the main divide | a pass must be walkable under the controller contract (proposed 3F-18) |
| Mix of access and barriers (I2, I5) | walls > 45° bound tablelands and gorges | walking stops at 45° (measured, handoff §21); **jumping climbs every tested slope to 70°** — whether walls are barriers is the owner's call (3F-20) |
| Viewpoints (I6) | plateau tops and saddles with open sight lines | every visible landmark should be reachable (`WORLD_RELIEF_PRINCIPLES.md` §4) |
| Variety of scales (§4.2) | forms of every level present, not uniform detail | — |

**Visual appeal and traversability are separate requirements.** A cliff, a gorge or a narrow ridge is a
visual success and a traversal question. Where they meet: walls (barrier or not, 3F-20), narrow passages
(T-edge vs T-vertex, 3F-18/3F-23), steep slopes (one 45° triangle can decide 17–34 thousand m² of
access, handoff §20) and bridges (crossings need anchors on both rims). A geometry change must report
both readings and never trade one for the other silently.

### 4.5 Scale budget on the current island

| Quantity | Value | Source |
|---|---|---|
| world | 2 800 × 2 800 m | locked V0 |
| land | 2.61–3.16 km² (32–40 % of the world) | **[PRIOR]** 29-seed audit |
| inscribed radius | 410–781 m | **[PRIOR]** same |
| land within 200 m of the coast | 54–59 % | **[PRIOR, prototype]** Stage 4A §2 |
| massif footprint (one) | 0.47–0.58 km² of uplift, 11–13 % of land above the floor | **[PRIOR]** native preflight §3–4 |
| height used | ~365 m of 700 m | **[MEASURED]** |

**[CONCLUSION]** On this footprint, a *connected* mountain system with two to four major ridges reaching
toward the coast, one or two passes and lowland corridors between the ridges is geometrically plausible
under game-scale compression (I7). Two *separate* ranges with an intermontane basin are not, under the
current Stage 2A rules (600 m separation, 200 m half-width, 200 m coast clearance) — and the
intermontane concept itself is missing (B2a/B2b). **[HYPOTHESIS]** checked by G3: whether a branched
system fits within the coast gate on ≥ 5 of the 7 canonical seeds. Whether the target must fit the V0
world at all is the owner's (99 Open Questions GEO-1, linked to the existing world-size question).

### 4.6 Hydrology as a consequence of geography

- **Catchments and divides** derive from the terrain, never from water placed first: the drainage
  analysis stays downstream and reads the finished relief (`HYDROLOGY_GENERATION_PLAN.md` §7, approved MVP).
- **Headwaters** sit in upland catchments because channels initiate at a contributing area in m² — this
  already holds by construction.
- **Tributaries converge** only if the relief converges. On a dome they cannot (§1.3 item 5); this is a
  terrain problem, not a hydrology tuning problem.
- **Rivers follow valley floors** only if valleys exist where flow goes. Today two valley systems exist
  and do not know each other: Stage 3C trajectories (declared, massif-derived) and D8 reaches (derived
  from the height). Reconciliation is a design item (§5.5).
- **Lakes need basin semantics**: enclosure, fill level, outlet. The current documented policy is
  *fill, then spill* with every system reaching the ocean (`HYDROLOGY_GENERATION_PLAN.md` §§9, 11). Closed
  basins, endorheic lakes, flat-filled terraces and cirque basins are explicit policies with
  consequences, not defaults: 3F-10, 3F-11, 3F-14 and GEO-5.
- **Waterfalls** are candidates where channels cross escarpments (existing `WaterfallCandidate`); they
  need level-4 escarpments to exist first.
- **Crossings** need valley geometry with rims (bridge anchors) or fords (wide shallow floors); both
  read from `RiverNode` data that already exists.

A spline across unchanged terrain is not hydrology (`WORLD_GENERATION_PIPELINE.md` §3); this document
does not relax that bar.

---

## 5. Stage responsibility map (proposal)

The seven responsibilities of the brief, mapped onto what exists. "Status" is the status of the
*mapping*, not of the code. No new terrain authority, no parallel generator and no second bake appear
anywhere in this map: every proposed responsibility either stays where it is or is decided by an owner
choice recorded in 99 Open Questions.

### 5.1 Island footprint and broad elevation structure

- **Today:** Stage 1 (frozen). Footprint by crust-cell growth; broad elevation = coast distance only.
- **Gap:** macro intent of the silhouette (owner decision D1, native preflight §8); broad elevation that
  is not a function of coast distance.
- **Inputs / outputs:** seed, `TerrainGenSettings` → coast rings, signed distance, `H1` lattice.
- **Determinism:** `terrain.crust*`, `terrain.coast.*`; golden hashes on 5 seeds.
- **Failure modes:** amoeba outline with blocky cell rhythm; dome datum (measured).
- **Debug / tests:** `TerrainGenDump.DumpStage1Batch`; Stage 1 golden-hash tests.
- **Status:** existing. **Proposal:** keep Stage 1 frozen through G0–G3; let broad elevation come from
  level-1 relief regions (§5.2) rather than from a new datum. Reopen only through D1.

### 5.2 Mountain regions and large-scale relief

- **Today:** Stage 2A axes + Stage 2B massif (one, unbranched) + Stage 4A plate districts.
- **Gap:** a mountain *system* (main divide, branches, saddles); a second high region where room allows;
  highland on massif-free worlds beyond +25 m plates.
- **Representation:** analytic structure evaluated as a point function — the property every stage so
  far has kept, and the one that makes small test regions exact.
- **Options** (GEO-3; **no decision here**):
  - (a) revise Stage 2B into a branched system (C7 in the native preflight): moves every Stage 3 input,
    needs the 29-seed re-audit and a `GeneratorVersion` bump;
  - (b) a new positive stage composed after `H2`, as Stage 4A was: keeps frozen stages bit-identical, but
    the frozen Stage 3 valleys would ignore the new ridges — ridges without their dual valleys;
  - (c) a geography skeleton that generates ridges and valleys together for the whole island, of which
    today's massif + Stage 3A–3C is a special case: coherent by construction, the largest change.
- **[CONCLUSION]** Whatever the option, ridges and valleys must be designed as duals
  (`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §3.1). Option (b) on its own violates that.
- **Determinism:** new named streams only; no existing stream moves; `GeneratorVersion` bump when it
  reaches production.
- **Debug / tests:** `TerrainGenMacroDump`, `TerrainGenMassifDump`; new: divide-tree and saddle overlays.
- **Entry:** G0 baseline and GEO-2 answered. **Exit:** G3 candidate passes §7 macro metrics on ≥ 5 of 7
  canonical seeds and owner review of V1–V5.
- **Status:** existing (2A/2B), proposed (the system), experimental (G3).

### 5.3 Ridge networks, spurs, passes and foothills

- **Today:** 3B primary spurs and 3D ribs as curves (frozen); realised only by 3F's subtractive carve;
  foothills only as the 4A apron ring.
- **Gap:** ridges outside the massif; saddles as declared features; spur-toe foothills.
- **Proposal:** owned by whichever stage owns §5.2's system — the skeleton must come with the system,
  not as decoration after it. 3F realises; it does not invent ridges.
- **Failure modes to watch:** starfish spurs, uniform crest heights, ring foothills (named in the
  generator design § Risks).
- **Status:** existing (structure), proposed (ownership move).

### 5.4 Valley systems and drainage basins

- **Today:** 3C trajectories on massif flanks plus a ≤ 7.2 m floor continuation; 3E enclosures; no
  inter-massif corridors (B2b); no valleys on massif-free worlds (B8).
- **Gap:** trunk valleys in level-1 lowland corridors; valleys whose path follows where water actually goes (the natural
  low lies outside the corridor at ~80 % of plain stations, handoff §11).
- **Proposal:** declared valleys come from the same skeleton as the ridges (§5.2); the D8 analysis is
  the **check** (Stage 3 design §8: reaches should occupy corridors; a river crossing a declared divide is
  a defect in one stage or the other). Valleys from flow alone were rejected for structure
  (Stage 3 design §16) and stay rejected here.
- **Status:** existing (3C/3E), proposed (lowland corridors, reconciliation check).

### 5.5 River incision and channel-to-terrain integration

- **Today:** two carves. Stage 3F cuts valleys along declared trajectories (experimental); `HydrologyBake`
  cuts channels along D8 reaches into the bake lattice (built, off).
- **Proposal (split by scale, not by competition):** Stage 3F owns valley-scale incision into structural
  relief; hydrology owns channel-scale incision (≥ ~8 m wide, the lattice limit) only inside valleys that
  exist, and its validation reports reaches that leave declared valleys. Both lower, neither raises, and
  the order is fixed: TerrainGen (including 3F) → bake lattice → hydrology carve. **[OPEN]** where E
  enters the bake (3F-25) decides how the two compose; that the pending hydrology carve "would lower E"
  is already recorded there.
- **Status:** existing (both carves), proposed (the split).

### 5.6 Lakes, basin policies and coastal transitions

- **Today:** 3E declares enclosures without depth (B4 open); 3F F2 (basin realisation) blocked on B4;
  hydrology fills then spills; coastal character (4C) not started; every relief term is exactly zero at
  and seaward of the shore and gated over the 200 m coastal rise.
- **Gap:** declared basins as terrain; cliff coasts and headlands.
- **Proposal:** basin policy is decided before basins are built (3F-10, 3F-11, 3F-14, GEO-5); coastal
  character stays a TerrainGen stage that keeps the land mask bit-identical (4C), and whether relief may be
  steep immediately inland of a fixed shoreline is GEO-4.
- **Status:** existing (3E, hydrology policy), proposed (4C), open (policies).

### 5.7 Validation and diagnostic visualisation

- **Today:** `TerrainGen*Dump`, `IslandReliefMetrics` (S8 scan), `Stage3FVerification`,
  `OverworldTerrainValidation`, `OverworldTerrainDiagnostics`; no oblique or eye-level capture.
- **Proposal:** G0 metrics (§7) as an Editor-only dump over any lattice; G1 Geometry-First Review (§3).
  Both read lattices; neither writes production data.
- **Status:** existing (dumps), proposed (G0, G1).

### 5.8 Composition order (unchanged where it exists)

```
Stage 1 ─► Stage 2A/2B (+ the mountain-system decision, GEO-3) ─► H2   [3A–3E declared on H2]
        ─► Stage 4A (+ 4B plateaus, 4C coast: proposed) ─► H4A        structural height
        ─► Stage 3F realisation (E family, experimental) ─► final surface (proposed, 3F-1)
        ─► bake lattice ─► hydrology carve (optional) ─► TerrainData
```

---

## 6. Stage 3F within the geography system

Baseline: [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md)
v0.7 (phases 1–10). Read as a research record, not as an approved contract. Nothing below changes its
§22 proposals.

### 6.1 Issue assessment

| Issue | Observed failure | Surface / contract | Geographic consequence | Confirmed or hypothesis | Experiment and acceptance | Belongs to |
|---|---|---|---|---|---|---|
| Carve-made depressions | production `H3F` creates 211 significant S8 depressions vs `H4A` on seeds 1000–1039 (209 by IDS) | 1025 lattice; S8 + independent union-find; later the triangle reading | undeclared ponds and pits along valleys | **confirmed** (handoff §9) | E: 0 on lattice and served surface (§§16, 18); accept: 0 new, 0 deepened, by an independent oracle (3F-2, 3F-3) | **3F** |
| Lateral tilt of the trough | carved low 5–12 m off the axis; 81 % of filled pits off the floor | analytic carve on `H4A` | valleys drain sideways into their own walls | confirmed (§§9–10) | floor-referenced plain template (E) | **3F** |
| `SmoothMax` inflation | +25 % per equal pair, ~18 depressions, plain incision > 7.25 m on 4.2 % of length | `CarveField.Depth` | over-deep junctions | confirmed (§3.2, §9) | hard maximum inside E (3F-9) | **3F** |
| Tributary cut-off | tributary carve at full depth across the trunk, then a 7.2 → 0.7 m collapse in 20 m | per-corridor support | barrier at confluences | confirmed (§3.2) | E: 0 depressions within 60 m of 29–31 junctions | **3F** |
| `TroughProfile` decay | peak at arc 0.5, sheds 7.2 m downstream; median toe at 0.279 | per-corridor longitudinal profile | barriers induced by the carve | confirmed (§3.3) | T3/E variants | **3F** |
| `H2`/`H4A` factorisation | 4 closures where `U2` falls 8–22 m and `R4A` backfills; mountain term tracks `U2` | `H4A = H1 + U2 + R4A` vs carve keyed to `U2` | valley floor climbs where the island is level | confirmed (§3.5) | drained by the envelope at 7.2 m; mountain-term redesign is 3F-16 | **3F × 4A** |
| Lost bypasses | closing an `H4A`-drainable low rises from 0–1 % to 51–54 % at carve ≥ 5 m | amplitude vs cross-valley relief | deep carve seals side drainage | medium-high (§3.3) | measured on E with the plan-view scan | **3F** |
| Route closures | all 36 axial closures of `H4A` are declared trajectories crossing rising ground; 41 `Declared` gaps on 34 of 299 trajectories, 36 on coastal turn-backs | declared trajectory vs `H4A` | valleys that climb, restart or end mid-plain | confirmed (§§9, 16) | no ending rule tested is a solution (§15) | **upstream**: 3C paths vs 4A relief (B10) |
| S11-T unsatisfiable | gate recovery and plate-level crossings raise `R4A` by up to 19.64 m along a trajectory vs a 7.2 m bound | trajectory-wise vs coast-normal contracts | the declared path is wrong for the relief | confirmed design/contract gap (preflight §22) | owner choice among restating S11-T, widening the canonical set, capping `U·Λ` | **upstream** (4A × 3C) |
| Thalweg outside the corridor | the natural low lies outside the corridor at 79.5 % of plain stations | frozen trajectory geometry | valleys not where the land is lowest | confirmed (§11) | thalweg shift F rejected; options in 3F-15 | **upstream** (3C) |
| Massif-free worlds | no corridors, `H3F ≡ H4A` | 3C derives valleys from massif spurs | 3 of 7 canonical seeds get no valleys | confirmed (B8) | none in 3F's power | **upstream** (2A/3B) |
| Served surface and slopes | Unity serves the SW→NE triangle reading; bilinear differs by up to 2.59 m; seven slope readings | quantised lattice, triangles | walkability, footing and access read the wrong slope | confirmed (§§18–21) | proposed contracts §22.2–22.5 (3F-6, 3F-18, 3F-19, 3F-21, 3F-22) | **whole terrain pipeline** (final surface), not 3F alone |
| Massif-foot depressions | S8-M 6/40 sweep seeds, max 2.32 m | `H4A` inside `Z_M ∪ L_M` | strike troughs at the foot | confirmed (tech debt 19) | 3F correction fills 3 of 6 (3F-10 open) | **2B × 4A** (landform) / 3F (drainage) |

### 6.2 Should Stage 3F generate the mountain and valley system?

**No.** **[CONCLUSION]** from §§1.2, 6.1:

- 3F is subtractive and bounded (`H ≤ H4A`, ≤ 7.2 m below the floor on the plain); it cannot create a
  ridge, a high region or relief on a massif-free world.
- Its valley paths are frozen Stage 3C geometry derived from one massif; four of the thirteen issues
  above (route closures, S11-T, thalweg, massif-free worlds) are failures of that geometry against the
  relief, and a fifth (massif-foot depressions) is a relief landform — none of the five is the carve's.
- What 3F *can* own is the realisation of a coherent relief's declared valleys and basins with
  guaranteed drainage — the E family is evidence it can do that on the lattice and the served surface.

**3F carves drainage into an already coherent relief; upstream stages must provide that relief first.**

### 6.3 Recommended scope for 3F research while the upstream question is open

- Continue only the parts that survive any upstream decision: the plan-view drainage oracle (3F-2,
  3F-3), the band-local correction (3F-5), the point-query and final-surface contract (3F-1, 3F-6,
  3F-25), the walking contract (3F-18, 3F-19).
- Do not tune trajectory-bound behaviour (valley endings 3F-12, mouths 3F-13, thalweg 3F-15) until GEO-3
  is decided: if valley paths are redesigned upstream, that tuning is thrown away.
- Exit criteria for 3F research as a whole are unchanged from handoff §23; add one: **re-run the E-family
  acceptance on the first accepted G3 relief** before any production design of 3F.

### 6.4 Owner-only decisions, verified 2026-10-18

Re-read in [99 Open Questions](../../../99%20Open%20Questions.md) v0.30 and handoff §23. IDs, wording,
tags and "owner's call / no recommendation" status are as recorded there; nothing below decides them.

| Topic | ID and status | Alternatives (as recorded) | Evidence | Consequences | Experiment that would reduce uncertainty |
|---|---|---|---|---|---|
| Filling inherited depressions | **3F-10 [stage 1] — owner's call, no recommendation** | (a) fill; (b) never fill; (c) fill below a stated world-quantity size | the prototype fills 3 of 6 inherited `H4A` depressions (1013, 1026, 1038) that lie inside carved components | (a) landforms become terraces; (b) the correction must route around them; (c) needs a justified threshold | report, per inherited depression, area, depth, volume and whether a G3 relief removes it at source; a geography whose massif foot drains removes the question for those seeds |
| Flat-filled pits (terraces) | **3F-11 [stage 1] — owner's call, no recommendation** | (a) accept; (b) reject — reshape the flank or redesign the mountain term; (c) accept only as lake sites | 57 370 m³ over 47 seeds, 82 % on the massif flank, up to 4.4 m deep | (a) flat steps on flanks; (b) 20–23 % less mountain carve or a redesign; (c) depends on inland water, absent in V0 | render terraces in the Geometry-First Review (V4/V5) on the seeds with the largest fills, so the owner judges them as landforms, not numbers |
| Cirques as permitted basins | **3F-14 [stage 1] — owner's call, no recommendation**; research mode proposed, not a decision | (a) not permitted — filled like any pit; (b) permitted — needs B4 and F2 first | 50 of E's 100 pre-fill flank depressions lie in declared cirques | (a) no flank basins, more terraces; (b) E waits for B4/F2 | the recorded research mode: compute E both ways and report depressions, terraces and volumes inside and outside cirques |
| Incision classes for the 7.2 m bound | **3F-4 [stage 1] — the arc-class coverage is the owner's call** (the plain/mountain split carries an agent recommendation) | plain class only, or also the 60 m blend (7.208 m measured) and above-toe cells with `U2` ≤ 0.1 m (up to 7.223 m) | handoff §§15–16, 18 (7.2066 m on the served surface) | which cells a test fails on; nothing about drainage | report the three classes separately on every run, with one storage step (1.07 cm) of tolerance on the served surface |
| Jumping, barriers and traversal | **3F-20 [production] — owner's call, no recommendation** | (A) keep the jump; (B) no jump from steep footing; (C) limited climbing; (D) explicit barrier cliffs | the production jump climbs every tested slope up to 70° | (A) cliffs cannot gate routes or hide POIs; (B) descent-only traps need a directed exit check; (C) needs a steep-run height measure; (D) needs barrier rules or content | measure, on a G3 relief, the area each option would gate (walls by height of steep run), then walk the Play Mode harness of handoff §21 on the candidates |

**This research adds no answer to these questions.** The geography programme makes two of them more
consequential: if walls are meant to gate routes (I2, I5), 3F-20 decides whether they can; if the
target includes lakes in basins (§4.6), 3F-11 and 3F-14 decide what a filled basin becomes.

---

## 7. Geometry-first acceptance suite (proposal)

Every metric is computed on a lattice (T1) or on the served triangle reading (T2), never from textures.
Columns: **baseline** — measured value today, with source; **proposed initial target** — an authoring
default with its reasoning, to calibrate; **owner threshold** — a value that would gate acceptance and
needs approval. No proposed target is a gate until GEO-7 is answered. Numbers inside the metric
*definitions* (window sizes, prominences, areas, distances) are **authoring defaults** in world units, to
be confirmed or replaced in G0; none is measured. G0 kept them; §7.6 names the three definitions it changed
and why.

### 7.1 Macro-relief

| Id | Metric (definition) | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| M1 | hypsometry: land share in 24–50 / 50–70 / 70–110 / 110–230 / 230+ m, and p50 / p90 | 110201 `H4A`: 39.7 / 25.3 / 25.3 / 5.9 / 3.7 %, p50 58 m, p90 108 m; 1337: 41.5 / 32.0 / 26.5 / 0 / 0 %. Canonical seven: land above 110 m 9.6–18.7 % on massif worlds, 0 % on the three massif-free ones; p90 108–155 m / 75–78 m **[MEASURED, G0 §7.6]** | land above 110 m well beyond today's ~10 % so that a highland region exists; calibrate on G3 candidates and reference DEMs | needed (GEO-7) |
| M2 | relief regions: connected components of land whose height exceeds a 600 m-window median by ≥ the plate step (18 m), area ≥ 0.1 km² | 1 region (0.29–0.32 km²) on 110201, 424242, 7; 2 on 20260905 (0.21, 0.18 km²); **0** on 1337, 256, 777 **[MEASURED]** | ≥ 2 on massif worlds, ≥ 1 elsewhere | needed |
| M3 | **single-dome detector**: (a) coast-aligned gradient share of the interior floor; (b) R² of floor height against coast distance; (c) summits with topographic prominence ≥ 30 m; (d) share of land above p90 in its largest connected component | (a) 63.1–77.2 %; (b) 0.858–0.913; (c) 1–2 summits ≥ 30 m, of them **1 separate** on every seed; (d) 1.000 on six seeds, 0.893 on 256. The detector as built (§7.6) flags **5 of 7** — 20260905 escapes on (a) alone, 256 on (d) alone **[MEASURED]** | flag the failure mode when (a) > 65 %, (b) high, (c) ≤ 1 and (d) ≈ 100 % together; the 65 % is Stage 4A's existing target, the rest calibrate in G0 | needed |
| M4 | ridge network: ridge lines from divide extraction (D8 basin boundaries on the filled surface); total length, longest connected ridge, branch count, crest-height coefficient of variation | flow-divide skeleton 7.0–11.8 km, longest 0.98–4.76 km, 4–11 branch points; it traces the floor's medial-axis creases, not the massif crest; crest CV 1.12–1.49 (massif) / 0.60–0.73 (massif-free); expressed 13–25 % on every seed **[MEASURED]** | branching present on massif worlds; non-uniform crests (`WORLD_RELIEF_PRINCIPLES.md` §3 bans uniform crests) | needed |
| M5 | saddles and passes: saddle points of the merge tree with prominence ≥ 15 m; passes = saddles on the main divide joining two valley heads | 1 saddle ≥ 15 m on 424242, 20260905 and 7 — the col between two culminations of one massif, 213–222 m; 0 elsewhere; **0 passes** on the main divide on all seven **[MEASURED]** | ≥ 1 pass on each major divide (rule 3) | needed |
| M6 | silhouette and coast: crenulation; share of shoreline whose land rises ≥ 15 m within 60 m (cliff coast); headland and cove counts | crenulation 1.33–1.59 (Stage 1 statistic); cliff share 0.7–3.7 % on massif worlds, 0 % massif-free; headlands 0–2, coves 0–1 at R = 100 m **[MEASURED]** | cliff share > 0 only if GEO-4 allows it | needed |

### 7.2 Drainage and basins

| Id | Metric | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| D-a | watershed consistency: share of declared divide length within 2 lattice steps of a D8 basin boundary (as built: of a flow-divide cell, §7.6) | **0.6–2.0 %** on the four massif seeds; n/a without a massif **[MEASURED]** | high; the exact share calibrates in G0 | needed |
| D-b | tributary structure: Strahler orders, bifurcation ratio, Hack exponent of trunk length against basin area | `H2` 110201: 8 systems, 0 tributaries **[PRIOR]**; `H4A`: 110201 8 systems, 0 tributaries, 1.95 km; canonical 5–11 systems, 0–4 tributaries, Strahler ≤ 2; bifurcation and Hack exponent not interpretable at 7–15 reaches **[MEASURED]** | order ≥ 3 on the largest system; literature references for calibration only — bifurcation ratio 3–5 in many real networks ([Strahler number](https://en.wikipedia.org/wiki/Strahler_number)), Hack exponent slightly below 0.6 ([Hack's law](https://en.wikipedia.org/wiki/Hack%27s_law)) | needed |
| D-c | downstream continuity under the stated surface convention (priority flood ε 1 mm, 8-connected, on the quantised lattice; checked again on the triangle reading) | lattice: flow tree complete and validation PASS on all seven **[MEASURED]**; triangle reading not run | 0 uphill reaches, 0 cycles (existing) | existing |
| D-d | closed depressions: S8 (≥ 0.25 m and ≥ 100 m²) + independent IDS, outside permitted basins | `H4A` 0 on all seven canonical; sweep 6/40 worlds with one depression each, deepest 2.32 m — the six seeds tech debt item 19 lists **[MEASURED]** | 0 new outside permitted basins (requirement D1 as researched) | 3F-2 and the basin policies |
| D-e | river/valley alignment: share of reach length inside declared valleys; channel incision depth relative to the valley floor | 20–82 % of river node length inside a Stage 3C half-width on the four massif seeds; n/a without a massif; incision n/a on `H4A` **[MEASURED]** | high alignment; reported until GEO-3 | needed |

### 7.3 Geometry and traversal

| Id | Metric | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| T-a | slope bands of all land: < 5°, 5–15°, 15–30°, 30–45°, > 45° | 110201 `H4A`: < 5° 37.2 %, > 45° 5.7 %; canonical < 5° 25.4–54.1 %, > 45° 0–7.4 % (0 % on the three massif-free seeds) **[MEASURED]** | existing finished-island targets: ≥ 35 % below 5°, 6–15 % above 45° (`WORLD_RELIEF_PRINCIPLES.md` §6) | existing |
| T-b | walking connectivity: T-edge reachable share and the uncertain zone (T-vertex − T-edge) | `H4A` 94.53 %, E 95.04 % over 8 seeds **[PRIOR]** | ≥ 95 % (existing principle, read on T-edge) | 3F-17, 3F-18 |
| T-c | passages and bottlenecks: minimax steepest triangle along the best path from the shore to each region; area decided by passages within ±1° of 45° | 17–34 thousand m² on 5 of 8 seeds **[PRIOR]** | reported, never hidden | 3F-20, 3F-23 |
| T-d | seams and tile artefacts | 0.0000 m **[MEASURED]** | 0 (existing) | existing |
| T-e | resolution sensitivity: every metric at 513 / 1025 / 2049 evaluation | partial (handoff §15) | stated per metric; a metric that moves across resolutions is preliminary below 1025 | — |
| T-f | sampled field against served surface: max \|triangle − lattice\| and slope disagreement at thresholds | ≤ 1.07 cm height; D3 and T on opposite sides of 28 / 35 / 45 / 60° on 0.08–0.34 % of land **[PRIOR]** | reported (existing contracts) | 3F-6, 3F-21 |

### 7.4 Visual comparison

Required captures per seed and candidate, from the §3.4 camera set with the §3.1 presentation:

1. the whole island from a distant elevated viewpoint (V2, V3);
2. an oblique view of the mountain system and its transition into lower ground (V2 cropped, or V7);
3. a mid-distance view of a ridge, a valley and foothills (V4, V8);
4. a ground-level view (V5);
5. a drainage view with watersheds, channels and outlets (V6).

Comparisons are valid only between captures with equal camera records. A human review states, per seed,
whether rules 1–10 of §4.3 read in the images — the generator design's "what must still be looked at by
a human" list, extended to the hierarchy.

### 7.5 Calibration

- **Against today:** G0 measured every "not measured" baseline on the canonical seeds and the sweep (§7.6).
- **Against reference geography:** real digital elevation models of small mountainous islands, scaled to
  the island footprint, give hypsometry, ridge, saddle and drainage figures to compare with. Which DEMs to
  use is part of G2; none were used here.
- **Against the visual reference:** §4.1 readings, checked on V2–V5 captures; no metric is fitted to a
  screenshot.

### 7.6 G0 baseline on production `H4A` — measured 2026-10-18

`GameDev.EditorTools.TerrainGenGeographyDump.DumpBatch` (`-geoSeeds`, `-geoSweep start:count`,
`-geoOut`) on `IslandReliefWorld` with `IslandReliefSettings.Default`, the hydrology package read-only on a
copy of the lattice. The report prints every definition with its authoring defaults; the numbers here are
**[MEASURED]**. Every `H4A` hash equals the value pinned in tech debt item 19, so this is production.

| seed | massifs | > 110 m % | M2 | M3 (a) % | (b) η² | (c) all / separate | (d) | single dome | M4 skeleton km / longest km / branches | M5 saddles / passes | cliff % | D-a % | D-b systems / tributaries / order / km | D-e % | T-a < 5° / > 45° % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 110201 | 1 | 9.6 | 1 | 73.6 | 0.913 | 1 / 1 | 1.000 | **flag** | 9.05 / 2.49 / 6 | 0 / 0 | 1.7 | 0.6 | 8 / 0 / 1 / 1.95 | 31 | 37.2 / 5.7 |
| 424242 | 1 | 12.8 | 1 | 68.1 | 0.910 | 2 / 1 | 1.000 | **flag** | 9.36 / 1.60 / 9 | 1 / 0 | 0.7 | 1.2 | 5 / 2 / 2 / 1.14 | 20 | 27.9 / 6.6 |
| 20260905 | 1 | 18.7 | 2 | 63.1 | 0.858 | 2 / 1 | 1.000 | no — (a) | 7.01 / 0.98 / 4 | 1 / 0 | 1.7 | 2.0 | 11 / 4 / 2 / 2.51 | 67 | 25.4 / 7.4 |
| 7 | 1 | 10.6 | 1 | 73.0 | 0.877 | 2 / 1 | 1.000 | **flag** | 9.90 / 1.87 / 4 | 1 / 0 | 3.7 | 1.8 | 7 / 0 / 1 / 1.09 | 82 | 30.4 / 5.4 |
| 1337 | 0 | 0 | 0 | 66.8 | 0.888 | 1 / 1 | 1.000 | **flag** | 9.79 / 4.76 / 11 | 0 / 0 | 0 | n/a | 8 / 2 / 2 / 1.54 | n/a | 49.9 / 0.0 |
| 256 | 0 | 0 | 0 | 67.8 | 0.877 | 1 / 1 | 0.893 | no — (d) | 11.76 / 3.84 / 10 | 0 / 0 | 0 | n/a | 10 / 0 / 1 / 0.84 | n/a | 44.3 / 0.0 |
| 777 | 0 | 0 | 0 | 77.2 | 0.913 | 1 / 1 | 1.000 | **flag** | 10.89 / 2.67 / 11 | 0 / 0 | 0 | n/a | 10 / 2 / 2 / 1.38 | n/a | 54.1 / 0.0 |

D-c (flow tree complete, planner validation) passes and D-d counts 0 S8 depressions on all seven. M6
headlands 0–2 and coves 0–1 per seed at R = 100 m. Per seed G0 adds ~0.6 s of drainage analysis and
1.1–1.5 s of metrics to a 12–45 s world build.

**Sweep 1000–1039** (`-geoSweep 1000:40`, one process, 875 s, worst seed 82 s; no pinned hashes exist for
these seeds). 32 worlds carry one massif, 8 none, none two.

| metric | massif worlds (32) | massif-free worlds (8) |
|---|---|---|
| land above 110 m | 2.6–20.0 % | 0 % |
| M2 relief regions | 1 on every world | 0 on every world |
| M3 separate summits / all summits ≥ 30 m | 1 on every world / 2 on 11 worlds (two culminations) | 1 / 1 |
| M3 (a) / (b) / (d) | 56.4–78.4 % / 0.839–0.948 / 1.000 | 55.9–97.3 % / 0.857–0.935 / 0.508–1.000 |
| single dome flagged | 17 (15 escape on (a) alone) | 6 (one on (a), one on (d)) |
| M5 saddles ≥ 15 m / passes on the main divide | 0–2 / 1 on 4 worlds | 0 / 0 |
| D-a | 0.7–6.7 % | n/a |
| D-b | Strahler ≤ 2 on every world; over all 40 worlds 1–16 systems, 0–8 tributaries, 0.08–3.64 km of river | Strahler ≤ 2 |
| D-d S8 | 6 worlds with one depression each — 1006, 1013, 1019, 1026, 1027, 1038, deepest 2.32 m | 0 |
| cliff share / land > 45° | 0–6.7 % / 1.9–8.5 % | 0 / 0 |

D-c passes on all 40. **D-d reproduces tech debt item 19 exactly** — the same six seeds and the same
2.32 m — through G0's own pipeline, which cross-checks the S8 measurement. The detector flags 23 of 40:
16 misses fail (a) alone and one fails (d) alone; a "three of four" rule would flag all 40. That the
escape is (a) on 16 worlds says Stage 4A's 65 % target is not a dome criterion; it is recorded for GEO-7,
not acted on.

**Definitions as built, and what changed after the first run.** Three definitions moved from §§7.1–7.2's
wording during G0; the first two moved **after** the first canonical run, so they are calibrated on these
seven seeds and are not independent evidence:

1. **M3 (c)** first counted every summit with prominence ≥ 30 m. That counted the two culminations of one
   massif on 424242, 20260905 and 7 as two summits (prominence 80–98 m, joined by a col at 213–222 m) and
   left those seeds unflagged, although G1 shows one body (§3.6). As built, (c) counts **separate**
   summits — key col below the land p90 — and the report keeps the literal count beside it.
2. **M4** was first built as stream-link catchment boundaries (channels from 30 000 m²). On `H4A` D8 flow
   barely converges — 1.5–2.8 km of channel cells per island — so most land drains to the sea without
   meeting a channel and the boundaries missed most parting lines, the massif crest included. As built, a
   cell is a **flow divide** when, along one lattice axis, both neighbours drain away from it (D8 within
   45°), and the mask is thinned (Zhang–Suen) to a skeleton that carries length, components, branch
   points (crossing number ≥ 3) and the main divide. The first-run figures (3.9–9.1 km, 3–18 branches) are
   superseded.
3. **D-a** measures declared divides against the flow-divide mask (within 2 lattice steps), not against
   basin boundaries.

**Readings [CONCLUSION]:**

- **The detector flags 5 of the 7 seeds the G1 captures show as single-dome** (§3.6). 20260905 escapes on
  (a) alone (63.1 % against Stage 4A's 65 %, which was set as a success target for floor relief, not as a
  dome criterion) and 256 on (d) alone (0.893 against 0.95: its top decile splits in two). A "three of
  four" rule would flag all seven; it is not adopted, because it would be chosen after seeing the data.
  Which rule and which thresholds gate anything is GEO-7.
- **Water parts on the floor, not on the mountains.** D-a is 0.6–2.0 %: the massif crown is flat across,
  so D8 runs along the crest instead of away from it, and the Stage 3B spurs have no relief in `H4A`
  (Stage 3 is not in the height, §1.2). The flow-divide skeleton traces the straight medial-axis creases
  of the coast-distance datum — the "extruded coastline" of §1.3 read from drainage.
- **No mountain system.** No seed has a second separate summit, and no significant saddle lies on the main
  divide (M5 passes 0); the only saddles ≥ 15 m are cols between the culminations of one massif. Massif-free
  seeds have no relief region (M2 0) and no land above 110 m.
- **Drainage stays coastal.** Strahler order ≤ 2, ≤ 4 tributaries, 0.84–2.51 km of river: §1.3 item 5
  holds on `H4A`. Bifurcation ratios (6–11) and Hack exponents (0.70–2.79) are computed over 7–15
  reaches and are not interpretable at that count; they become meaningful only on a relief with a
  network.
- **Coast.** Shore land rising ≥ 15 m within 60 m exists on massif worlds (0.7–3.7 % of the shoreline) and
  nowhere else; where it lies was not inspected. The coast gate still forbids sea cliffs (GEO-4).
- **Slopes.** Massif-free seeds carry 0 % of land above 45°, below the 6–15 % finished-island target of
  `WORLD_RELIEF_PRINCIPLES.md` §6; massif seeds 5.4–7.4 %, which the massif-free zeros place on the massif and its apron.

---

## 8. Documentation contradictions found

| # | Place | Says | Status |
|---|---|---|---|
| 1 | `OVERWORLD_TERRAIN_ARCHITECTURE.md` header | Stage 4A "not in this path yet" | contradicted by its own §1; **corrected** in this transaction |
| 2 | `WORLD_GENERATION_PIPELINE.md` §2 row 2 | macro terrain "evaluated as `StructuralElevation`" | production is `IslandElevation` (`H4A`); **corrected** |
| 3 | `IslandElevation.cs` remarks | "`TerrainAuthorityField` does not call it… production height is still `StructuralElevation.At`" | stale code comment; **recorded** (tech debt item 22) — code is not changed from documentation work |
| 4 | `TerrainGenIslandReliefDump` report header | "NOT connected to TerrainAuthorityField: production heights are still StructuralElevation (H2)" | stale output string; **recorded** (tech debt item 22) |
| 5 | `WORLD_RELIEF_PRINCIPLES.md` §1 table | T1 massif count "1 (2 max)" per island | not a contradiction today; **in tension with the target** (§4.1). Flagged as under review (GEO-2); the rule stays in force |
| 6 | `HYDROLOGY_GENERATION_PLAN.md` §17 | hydrology measured on "TerrainGen's relief" 2026-09-13 | that relief was `H2`, before Stage 4A; noted here, re-measured in G0 |

---

## 9. Prioritised experiment roadmap (proposal)

Every step is isolated: nothing in G0–G3 touches `TerrainAuthorityField`, the bake, the production scene
or a frozen file. Each step needs the owner's go-ahead before it starts; none is implemented by this
document.

| Step | What | Depends on | Risk | Completion criteria |
|---|---|---|---|---|
| **G0** | Geography metrics baseline: an Editor-only dump computing §7 M1–M6, D-a–D-e, T-a on `H4A` (and on `H3F`, where it exists) for the canonical seeds and the 1000–1039 sweep, reusing the dump lattice path and the hydrology package read-only | none | metric definitions are themselves proposals; a weak definition can mislead | numbers recorded here for every "not measured" baseline; the single-dome detector flags production `H4A` on the seeds a human would flag; process repeatable (identical report in two processes); `git status` clean apart from the new Editor file. **Built 2026-10-18** (`TerrainGenGeographyDump`): baselines in §7.6, report byte-identical in two processes, `git status` adds only the Editor files. `H3F` was not measured (G0 ran on `H4A` only). **Partly met:** the detector flags 5 of the 7 seeds G1 shows as single-dome (23 of 40 on the sweep); its rule is GEO-7's to settle |
| **G1** | Geometry-First Review tool (§3): heights-only in-memory tiles, neutral material, camera set, overlays, PNG + JSON outside `Assets` | none (parallel with G0) | render cost unmeasured; URP Terrain Lit with a single neutral layer may still need a diagnostic shader for slope colours | identical images and camera records for the same lattice in two processes; production scene hash and tile assets unchanged; V1–V6 produced for `H4A` and E (when E is available). **Built 2026-10-18** (`GeometryFirstReview`, §3.6): images and records byte-identical in two processes, scene and tile assets unchanged, V1–V6 for `H4A` on the seven canonical seeds; no diagnostic shader needed; render ≈ 1 s per seed. **E not reviewed:** it lives outside the repository; the tool reads it through `-geoReviewLattice` once exported |
| **G2** | Target adoption: owner review of §4 against G0 numbers and G1 images; choice of reference DEMs; answers to GEO-1, GEO-2, GEO-4, GEO-7 | G0, G1 | the target is reopened after evidence; that is the point | 99 Open Questions entries answered or explicitly deferred; §4 and §7 revised to v0.2 with approved and still-proposed parts separated. **Held 2026-10-20** in [GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md): target **D6 approved** from the owner's concept image; owner directions on GEO-1 (decide by G3c, footprint A against B), GEO-4 (varied coast; fixed shoreline per footprint in G3c) and GEO-7 (metrics diagnose, they do not restrict); GEO-1 … GEO-5, GEO-7 and GEO-8 otherwise open; reference DEMs not chosen (§13) |
| **G3** | Upstream relief candidates, dump-first and standalone (the 3F phases method): compare §5.2 options (a)–(c) as fields with their ridge **and** valley skeletons; no wiring | G0, G1, GEO-2 | design scope grows; the frozen Stage 3 topology may prove incompatible with any good candidate | one candidate passes the M-series and D-d on ≥ 5 of 7 canonical seeds including a no-massif seed, keeps the land mask, adds no undeclared depression, and passes owner review of V2–V5. **Run 2026-10-19 by owner instruction before GEO-2, as a test of GEO-2 (a)** (§11): one candidate (shared skeleton, option (c)); on all 7 seeds single dome 0/7, multiple culminations and saddles, 0 S8, mask unchanged — but no drainage network (Strahler ≤ 2), no lowland corridor and 12–16 % flat land. The M-series has no approved thresholds (GEO-7); owner review of V2–V5 pending. **G3b ran 2026-10-19 … 20** (§12): a converging valley tree on the same skeleton gives every seed a lowland corridor 18–20 m above the floor surface and 2–6 tributaries, but Strahler stays 2 on all seven, land below 5° 15–17 %, and 12 S8 depressions appear on four seeds; ~~the limit is the footprint and the coast gate, not valley topology~~ **[corrected 2026-10-20, §13.2]** the measured limits are the coastal transition (gated coast-distance ramp) and the construction; the footprint was not varied and is a hypothesis. G2 followed (row above). **G3c ran 2026-10-20 … 21** (§14): region-first geography on contours A and B; neither passed the rule (A met every reading on 2 of 10 seeds, B on none); settlement sites, coastal sites and plateaus held on both; the world is not grown on this evidence. ~~**Next: the owner's review of G3c** (§14.11)~~ **Reviewed 2026-10-21 (§15):** the owner chose the pair S-LM3 + S-LM1 on a shared LM2 layout; the comparison's step 0 is written and revised by the owner ([GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) v0.2) and awaits the seven decisions of its §12 — no code or run authorised. **G3e (LMC), 2026-10-23:** the comparison has the official ID G3e; plan, specification (v0.4) and every pre-authorisation condition are approved; Step 1 (code and calibration) awaits explicit authorisation. **G3e Step 1 ran 2026-10-23 … 24 and ended not ready** (§16) |
| **G3f** | Regional geography architecture ([phase plan](GEOGRAPHY_G3F_PHASE_PLAN.md)): one authoritative Geography Plan with a single height owner per vertex (declared drainage units) and reserved write footprints, read by one drainage-consistent synthesis; a bounded regional prototype on contour A comparing a control, a unified and a per-unit synthesis; then an architecture decision and, if one is adopted, a production implementation plan as the input to G4 | G3e and G3e-PM (§16) | the candidate law repeats S-LM3's walls and stripes; regional results not transferring to the island (plan §11) | phase and hypothesis **approved 2026-10-25** (D-1, D-2); only step A authorised; D-3 … D-12 open; the plan's gates G-3 … G-7 not reached |
| **G4** | Placement decision: a design document stating where the chosen mechanism lives, what unfreezes, the version bump and the re-audit cost | G3 | an unfreeze of Stage 2B or 3A–3C | owner approval (GEO-3); nothing implemented before it |
| **G5** | Stage 3F on the new relief: E-family acceptance (3F-2) re-run on the G3 relief | G3, 3F stage-1 answers | E may need changes where the new valleys differ | 0 new depressions by the independent oracle; arc-class report per 3F-4; review captures |
| **G6** | Basins, lakes and coast: basin policy, F2 against B4, Stage 4C coastal character, hydrology on the final surface | G4, G5, 3F-10/11/14, GEO-4, GEO-5 | inland water is outside V0 | each policy decided before its geometry is built |

**Smallest useful sequence:** G0 and G1 in parallel (read-only, no production change), then G2. G3 is the
first step that designs terrain; it was planned to follow GEO-2, and the owner instead commissioned it as
research on 2026-10-19 (§11); G3b followed (§12) and found the remaining limits in the coastal
transition and the construction — ~~in the footprint and the coast gate~~, corrected 2026-10-20 (§13.2);
G2, the owner's review, approved the target D6 (§13); G3c ran on two footprints (brief §8.3, §14) and
failed acceptance; the post-G3c review (§15) settled or classified the GEO questions and proposes a
landform-model comparison on today's footprint, which awaits the owner; G4 still waits for GEO-3's
production half.

---

## 10. Verification of this research

Run on 2026-10-18, Unity 6000.5.7f1 (Windows, batch mode), no editor open on the project, HEAD `3b7f890`.

| Check | Command | Result |
|---|---|---|
| Stage 4A dump, seeds 110201 and 1337 | `-executeMethod GameDev.EditorTools.TerrainGenIslandReliefDump.DumpBatch -terrainGenSeeds 110201,1337 -terrainGenOut <scratch>` | exit 0; `H4A` hashes `e2bad571f4af76c7`, `f86e8c15d7105be2` equal the pinned values; all Stage 4A structural checks PASS on both seeds; §11 report-only misses as already recorded (110201: coast-aligned 73.6 %, knolls 0, curvature 0.76; 1337: knolls 0) |
| Production bake + validation, seed 110201 | `-executeMethod GameDev.EditorTools.OverworldTerrainValidation.RunBatch -terrainSeed 110201 -terrainRun 1 -terrainOut <scratch>` | exit 0; world signature `656405ea477a` equals the pinned value; height error ≤ 1.07 cm; seams 0.0000 m; timings in §2.1 |
| Working tree | the four gitignored tile assets were backed up before the bake and restored afterwards; MD5 of all four and of `Overworld_Generator.unity` equal before and after; `git status --porcelain` empty after both runs | clean |

**Not run:** the EditMode test suite — no code changed. All outputs (images, reports, logs) live in the
session scratchpad, outside the repository.

**G0 and G1 (second pass, same day, same HEAD; GEO-6 (a)).** The two Editor files were authored, compiled
with no error or warning, and run in the main checkout with no editor open on the project.

| Check | Command | Result |
|---|---|---|
| G0, canonical seeds, two fresh processes | `-executeMethod GameDev.EditorTools.TerrainGenGeographyDump.DumpBatch -geoOut <scratch>` | exit 0 twice; `geography_report.txt` (MD5 `859708b2…`), `geography_metrics.json` (`587dce02…`) and the seven structure PNGs byte-identical; all seven `H4A` hashes PASS against item 19 |
| G0 sweep | `… -geoSweep 1000:40 -geoOut <scratch>` | exit 0; 40 seeds in 875 s; S8 result equal to item 19 (§7.6) |
| G1, canonical seeds, two fresh processes | `-executeMethod GameDev.EditorTools.GeometryFirstReview.RunBatch -geoReviewSeeds 110201,424242,20260905,7,1337,256,777 -geoReviewOut <scratch>` | exit 0 twice; 42 PNG and 7 JSON byte-identical; every lattice hash equals the pinned `H4A` hash. A first G1 run was discarded before these two: the opaque neutral texture made the ground specular and the contour ribbon half-hidden (both fixed, §3.6) |
| Working tree | before the first run: copies and MD5 of `Overworld_Generator.unity` and its `.meta`, the four gitignored tile assets, the gitignored `Scenes/Overworld_Generator/` folder (lighting data, probes), `URP_Pipeline.asset`, `URP_Renderer.asset`, every `ProjectSettings/*.asset` and `packages-lock.json` | all 39 MD5s identical after four G0 and three G1 processes; `git status` adds only `TerrainGenGeographyDump.cs`, `GeometryFirstReview.cs` and their `.meta` beside this research's uncommitted documents |

**Not run:** the EditMode suite — the pass adds two Editor files and changes no tested code. **Not
reviewed:** E, which is not in the repository (§9 G1).

---

## 11. G3 — first coherent mountain-system prototype (research, 2026-10-19)

**Mandate.** The owner commissioned G3 as a bounded research experiment on 2026-10-19, before GEO-2
and GEO-3 were answered: a connected branched mountain system is the **test hypothesis**, not an
approved mountain-count policy or architecture. GEO-1 … GEO-5 and GEO-7 stay open; nothing here is a
decision. Production code, the bake, the scene, tile assets, frozen Stage 1–4A and Stage 3A–3E files and
every golden hash are unchanged (§11.9).

### 11.1 Hypothesis and the candidate chosen

> A single connected but branched mountain system, designed together with its major valleys, can produce
> a recognisable multi-ridge landscape on the current island footprint without a new terrain authority
> or a change to the production pipeline.

**Candidate `G3-skel` — a shared skeleton (§5.2 option (c), in its lightest form).** One skeleton is
built from Stage 1's frozen coast geometry; ridges and valleys are both derived from it; the field
replaces the Stage 2B massif `U2` and its Stage 4A apron `A`, and keeps everything else of `H4A`:

```
G3 = H1 + M + Γ·(P + K)  +  S · smoothstep(d / 200 m)        (= H4A − U2 − A + gated S)
S  = E − carve,  bounded below by −7.2 m inside valley troughs
E  = max over spine and branches of crest(s) · (1 − smoothstep(dist / halfwidth(s)))
```

**Alternatives considered** (§5.2):

| Option | Why not the primary candidate |
|---|---|
| (a) revise Stage 2B into a branched massif | Stage 2A certifies one unbranched axis or none (4 of 29 audited worlds, 3 of 7 canonical): a branched 2B would still leave the massif-free worlds empty, and the axis sits where a 200 m half-width body clears the coast, not where the island's own spine is |
| (b) new positive stage after `H2` | adds ridges without their dual valleys — frozen Stage 3C derives valleys from the massif frame only (§5.2); it would answer a narrower question than the hypothesis |
| (c) shared skeleton | **chosen**: ridges and valleys come from one structure on every world, massif or not; the cost is that it bypasses Stage 2B, the apron and Stage 3A–3E, which is exactly what GEO-3 (c) would have to accept |

A second *approach* was not built. One cheap sensitivity was: `G3-skel-g200-v50`, the same skeleton with
valleys 0.50 instead of 0.30 of the crest. A coast-gate variant was dropped after measurement: the
200 m gate removes only 2–6 % of `S`'s volume (§11.4), so a narrower gate could not change the reading.

### 11.2 The model as built

| Part | Rule | Values (source) |
|---|---|---|
| mainland | largest 8-connected component of Stage 1 land | — |
| spine | weighted diameter of the mainland on a 10.9 m grid: Dijkstra with cost `1 + 6 (1 − r/rmax)²`, `r` = Stage 1 coast distance; smoothed ±6 grid points, resampled at 20 m | 20 m = `MacroStructureSettings.SampleSpacingMeters`; the rest authoring defaults |
| branches | greatest protrusion (path length − `r` at the attachment) from the current tree, while ≥ 200 m | 200 m = `UpliftCoastGateMeters`; at most 2 = `MaxBranchesPerRange` |
| culminations | greedily: widest point, then branch roots, then the widest remaining points (`r` ≥ 0.5 rmax), ≥ 550 m apart, at most 3; main crest 200–330 m, others ×0.62–0.82 | 550 m = `SecondCulminationLengthMeters` / 2; 200–330 m = `CrestHeightMin/Max`; 0.62–0.82 = `SecondaryHeightMin/Max` |
| saddles | narrowest land strictly between two culminations, at ×0.55–0.72 of the lower | `SaddleFractionMin/Max` |
| crest cap | crest ≤ 0.7 × coast distance (35° mean flank) | authoring default |
| ridge width | half-width 0.6 `r`, clamped 100–420 m; cross-section `1 − smoothstep` | authoring defaults |
| branch crest | root ×0.45–0.70 of the parent, descending to ×0.1 at the tip | `BranchCrestScaleMin/Max` |
| spurs | stations every 0.85 half-widths per side (jitter 0.25), none within half a spacing of a saddle or branch root; each a steepest-descent line on Stage 1 coast distance to 60 m from the shore. Spurs add no height: they are the uncarved lines between valleys | 0.85, 0.25 = `RidgeSettings.SpurSpacingPerHalfWidth`, `SpurStationJitter` |
| valleys | one between each pair of consecutive spurs; head on the parent crest; axis = steepest descent on coast distance; depth `crest·[0.15(1−t) + 0.15·6.75 t(1−t)²]`; **floor = running minimum of E(axis) − depth**, so it never rises downstream; carve `max(0, E − floor)·(1 − d/w)²`, w = half the spur gap; deepest wins | 0.30 = `RidgeSettings.ValleyDepthPerCrest`, 0.15 its half |
| troughs to the coast | the floor may go below the floor surface by at most 7.2 m, then is gated to zero at the shore | 7.2 m = `RidgeSettings.CorridorIncisionMeters` (no 3F-4 question is answered by reusing it here) |
| randomness | `System.Random` through `WorldSeed.Fork("research.g3.crest" / ".branch" / ".spur", …)` — streams no production stage reads | — |

Every value is an existing project number (named) or an **authoring default**; none is measured,
none approved.

### 11.3 Code, data flow and isolation

```
IslandReliefWorld (dump path, H4A hash = pinned)      ── unchanged
  └─ G3MountainSystem.Build (skeleton, crests, spurs, valleys, S)   new, Editor-only
       └─ Compose: H4A − U2 − A + S·g  → lattice file *.f32 outside Assets
            ├─ GeographyAnalysis.Build + GeographyMetrics.Measure  (G0, unchanged)
            ├─ G3Diagnostics (G3's own declared lines, catchment boundaries, valley axes)
            └─ GeometryFirstReview.Run(seed, dir, lattice, id)      (G1, unchanged)
```

- **One new file**, `unity/Assets/Scripts/Editor/TerrainGenG3MountainSystemDump.cs` (+ the Unity
  `.meta`), Editor-only, referenced by nothing; **uncommitted**. G0 and G1 are called through their public
  entry points and were not modified.
- The candidate is identified as `G3-skel-g200` / `G3-skel-g200-v50` with its own lattice hash; the
  production `H4A` is rebuilt and hash-checked in the same process as the baseline.
- `Compose` asserts the land mask and every ocean sample against `H4A`: **0 changed** on all seven seeds.

Run (one process, all seeds, both variants, G1 included — 12 min 33 s):

```
Unity.exe -batchmode -quit -projectPath <project> \
  -executeMethod GameDev.EditorTools.TerrainGenG3MountainSystemDump.DumpBatch \
  -g3Seeds 110201,424242,20260905,7,1337,256,777 -g3Variants base,deep -g3Review true -g3Out <dir>
```

Outputs in the session scratchpad
(`…/scratchpad/g3/final/`): `g3_report.txt` (definitions, structure, per-surface G0 lines and G3
diagnostics), `table.md`, `g3_timings.txt`, `g3_<seed>_<id>.f32` lattices, `g3_<seed>_<id>_structure.png`
(hillshade, skeleton, valleys, channels, depressions), `g3_<seed>_<id>_flow.png` (D8 accumulation),
`review/review_<seed>_<id>_V1…V6.png` + JSON (G1), and `g3_contact_sheet_V2_V3.png` (`H4A` against
`G3-skel-g200`, V2 and V3, all seven seeds). The scratchpad is temporary; the command above regenerates
every file byte for byte (§11.9).

### 11.4 Baseline against candidate

`H4A` → `G3-skel-g200`; definitions are G0's (§7.6) unless marked G3. **[MEASURED]**

| seed | massif | land > 110 m % | M2 | M3 (a) % | M3 (b) | M3 (c) sep | M3 (d) | single dome | M5 saddles / passes | culminations / saddles declared | spine summits / cols (G3) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 110201 | 1 | 9.6 → 29.7 | 1 → 1 | 73.6 → 35.7 | 0.913 → 0.853 | 1 → 3 | 1.000 → 0.725 | FLAG → no | 0/0 → 2/0 | 3 / 2 | 3 / 2 |
| 424242 | 1 | 12.8 → 29.2 | 1 → 1 | 68.1 → 49.6 | 0.910 → 0.749 | 1 → 2 | 1.000 → 0.945 | FLAG → no | 1/0 → 2/0 | 3 / 2 | 3 / 2 |
| 20260905 | 1 | 18.7 → 28.3 | 2 → 1 | 63.1 → 42.4 | 0.858 → 0.620 | 1 → 2 | 1.000 → 0.894 | no → no | 1/0 → 1/0 | 2 / 1 | 2 / 1 |
| 7 | 1 | 10.6 → 28.8 | 1 → 1 | 73.0 → 38.6 | 0.877 → 0.804 | 1 → 2 | 1.000 → 0.960 | FLAG → no | 1/0 → 3/0 | 3 / 2 | 4 / 3 |
| 1337 | 0 | 0 → 29.3 | 0 → 1 | 66.8 → 35.7 | 0.888 → 0.764 | 1 → 1 | 1.000 → 1.000 | FLAG → no | 0/0 → 2/0 | 3 / 2 | 3 / 2 |
| 256 | 0 | 0 → 22.6 | 0 → 1 | 67.8 → 46.7 | 0.877 → 0.844 | 1 → 2 | 0.893 → 0.924 | no → no | 0/0 → 2/0 | 3 / 2 | 4 / 3 |
| 777 | 0 | 0 → 26.9 | 0 → 1 | 77.2 → 40.7 | 0.913 → 0.770 | 1 → 2 | 1.000 → 0.771 | FLAG → no | 0/0 → 2/1 | 2 / 1 | 2 / 1 |

| seed | D-b systems / tributaries / Strahler / river km | S8 | land < 5° / > 45° % | crest on catchment boundary (G3) | spurs on catchment boundary (G3) / control | crest on flow divide (D-a rule) | river nodes in G3 valleys | valley axes climbing > 0.25 m | mountain share / gate loss / crest cap binding |
|---|---|---|---|---|---|---|---|---|---|
| 110201 | 8/0/1/1.95 → 8/0/1/1.63 | 0 → 0 | 37.2/5.7 → 13.6/12.3 | 0 → 8 % | 2 % / 1 % | 0.6 → 37 % | 75 % | 0 of 15 (0.14 m) | 38 % / 3 % / 7 % |
| 424242 | 5/2/2/1.14 → 7/3/2/1.65 | 0 → 0 | 27.9/6.6 → 15.5/19.0 | 2 → 16 % | 3 % / 3 % | 1.2 → 49 % | 36 % | 1 of 12 (0.35 m) | 36 % / 3 % / 45 % |
| 20260905 | 11/4/2/2.51 → 14/3/2/3.19 | 0 → 0 | 25.4/7.4 → 15.4/11.5 | 12 → 19 % | 2 % / 4 % | 2.0 → 41 % | 50 % | 0 of 20 (0.12 m) | 36 % / 2 % / 15 % |
| 7 | 7/0/1/1.09 → 9/1/2/0.96 | 0 → 0 | 30.4/5.4 → 12.4/21.1 | 3 → 14 % | 2 % / 2 % | 1.8 → 41 % | 73 % | 0 of 25 (0.06 m) | 37 % / 2 % / 20 % |
| 1337 | 8/2/2/1.54 → 8/1/2/1.35 | 0 → 0 | 49.9/0.0 → 11.9/18.9 | n/a → 28 % | 0 % / 2 % | n/a → 30 % | 21 % | 0 of 16 (0.07 m) | 37 % / 2 % / 38 % |
| 256 | 10/0/1/0.84 → 9/2/2/1.61 | 0 → 0 | 44.3/0.0 → 15.9/9.6 | n/a → 18 % | 0 % / 1 % | n/a → 42 % | 18 % | 0 of 8 (0.00 m) | 30 % / 6 % / 54 % |
| 777 | 10/2/2/1.38 → 10/0/1/1.79 | 0 → 0 | 54.1/0.0 → 13.4/12.1 | n/a → 20 % | 1 % / 2 % | n/a → 38 % | 56 % | 0 of 16 (0.11 m) | 36 % / 3 % / 26 % |

Baseline "crest" for the catchment and flow-divide columns is production's own declared structure
(massif crest lines, Stage 3 spurs); on the candidate it is G3's spine and branches. Every candidate
`H4A` rebuilt in the process equalled its pinned hash. Candidate lattice hashes (`G3-skel-g200`):
110201 `fac7ed693e68f24d`, 424242 `f6db8cd2c07364e5`, 20260905 `3e9e0dff8b0dfe78`, 7 `93dffe8f8b67af40`,
1337 `b77fc95ca43b0878`, 256 `6c1e59b67f7ee1a6`, 777 `8d1a261b5c0d9630`.

**Sensitivity `-v50`** (valleys 0.50 × crest): land > 110 m −0.3 to −1.0 points, carve volume ×1.5
(max carve 79–118 m against 48–71 m), valley axes climbing on 5 seeds (1–2 each, worst 1.64 m on
424242), drainage class unchanged (Strahler ≤ 2), and **no visible difference** at V2 (compared on
20260905).
Valley depth is not the lever that changes the reading.

**Skeleton per seed.** Spine 2.4–3.2 km; two branches on every seed (best protrusion 393–944 m against the
200 m threshold, so the existing branch budget of 2 binds); 18–34 spurs from 41–59 stations (6–32 skipped
because the land was too narrow to hold a 120 m spur); 8–25 valleys. Build 0.41–0.74 s per seed,
composition 0.18–0.21 s, G0 analysis and metrics ~2 s; the 12–47 s per G1 call is G1 rebuilding the world.

### 11.5 Visual reading — agent reading, owner review pending

From `g3_contact_sheet_V2_V3.png` and the per-seed V2–V5:

- **Macro structure: yes.** Every seed, the three massif-free ones included, now shows a connected range
  with two or three culminations at different heights, cols between them and branch ridges reaching toward
  the coast; from V2 and V3 the skyline is a ridge line, not a lens. 1337 goes from an almost flat plate
  island to a mountain system.
- **Dissection: weak.** Flanks read as smooth "whale-back" slopes; valleys show as grooves near the crest
  and fade downslope. The deeper variant does not change this.
- **Construction artefacts.** Fan-shaped striations and planar facets on the inside of spine bends (V2–V4):
  the field is a distance to a polyline whose crest and width are interpolated per segment, so the closest
  segment jumps on concave sides. A production construction would need a smooth parameterisation.
- **Ground level (V5).** The Stage 1-anchored eye point (bearing 270°, 0.5 r) now stands on a mountain
  flank on several seeds and looks into the slope; the comparison is fixed as designed but no longer
  informative there.
- **What is still missing against the reference (§4.1):** tablelands and walls (level 4), cliff coasts,
  lowland corridors, rivers in valleys.

### 11.6 What worked, what failed, and why

| Finding | Evidence | Cause |
|---|---|---|
| **Worked:** a connected branched system with multiple culminations and saddles on all 7 seeds, including the 3 without a certified massif | 2–3 culminations, 1–2 declared saddles, 1–3 G0 saddles ≥ 15 m on every seed; single dome 5/7 → 0/7; land > 110 m 22.6–29.7 % | the skeleton comes from the island's own spine, not from Stage 2A certification |
| **Worked:** valleys lie between ridges and descend to the coast | 0–1 of 8–25 axes climb (worst 0.35 m); 0 S8 depressions; troughs ≤ 7.2 m; mask and ocean unchanged | floor-referenced valleys with a running-minimum floor (the 3F lesson, §10 variant A) |
| **Worked:** declared crests coincide with drainage much better than production's | crest on flow divides 30–49 % (production massif 0.6–2.0 %); on catchment boundaries 8–28 % against a 1–4 % control | ridges and valleys from one skeleton |
| **Failed: no drainage network** | Strahler ≤ 2, 0–3 tributaries, 1.0–3.2 km of river — the same class as `H4A`; spur crests on catchment boundaries 0–3 % (≈ control) | **model**: every valley runs straight to its nearest shore, so valleys are parallel first-order catchments of roughly 30 000–90 000 m² (axis length × spur gap), near the 30 000 m² channel threshold, and none collects another; **diagnostic** (reading of `*_flow.png`): D8 on a smooth analytic surface runs in parallel lattice lines and converges only where the cross-slope is strong |
| **Failed: no lowland corridor** | declared saddles carry 114–172 m of uplift above the floor surface; no col comes down to it | **model**: saddle heights reuse the massif's ×0.55–0.72 range, which keeps cols high; a corridor needs a deliberately low col or a trunk valley |
| **Conflict: mountains against flat land** | land < 5° 25–54 % → 11.9–15.9 %; > 45° 0–7.4 % → 9.6–21.1 % (above the 6–15 % finished-island target on 3 seeds) | ~~**footprint**~~ **G3's sizing and the coastal ramp** [corrected 2026-10-20, §13.2 — no run varied the footprint]: the system takes 30–38 % of the land, the coastal ramp is 54–59 % of the land and is not flat, so the interior flats `H4A` had are where the mountains now stand; valley floors are V-shaped and provide no flats |
| **Footprint limits, measured** | crest cap (0.7 × coast distance) binds on 7–54 % of the spine (worst on the narrow islands 256, 424242, 1337); 6–32 spur stations lost to narrow land | the island's inscribed radius (410–781 m) bounds crest height and spur length **under G3's own authoring caps** (crest ≤ 0.7 × coast distance, half-width 0.6 r, 120 m minimum spur) [qualified 2026-10-20, §13.2]; **the coast gate is not the binding constraint** for the mountains (it removes 2–6 % of `S`) — for drainage it is (§12.6) |
| **Frozen structures** | none were needed; the candidate replaces `U2` and `A` and ignores Stage 3A–3E | adopting this kind of skeleton means superseding the massif-derived Stage 2B/3A–3E geometry — GEO-3 (c)'s stated cost |
| **Diagnostic limits** | G0's D-a and D-e read production's declared structure; M3 (a)/(b) classify "floor" with production's `U2` mask; M5 "pass" needs a saddle on G0's flow-divide skeleton, which follows the spine only 30–49 % of its length (passes 0 on 6 seeds although the spine carries 1–3 cols ≥ 15 m); V5's anchor lands on slopes | definitions built for `H4A`; G3 adds its own versions beside them, nothing in G0/G1 was changed |

**Calibration caveat.** Eight construction iterations were run on 110201 (and 1337 from the second on);
each fixed a named defect, none changed a metric definition or threshold:

| Try | Change | Reason |
|---|---|---|
| 1 | medial skeleton + spurs as ridges, `ProfileCurve` profiles | first build: 1 culmination, < 5° 7 % |
| 2 | smoother spine, half-width 0.6 r, branch roots as summit sites | spine bends, no lowland; one culmination |
| 3 | greedy culminations, saddles strictly between | a saddle had been placed on its own culmination |
| 4 | closed-form profiles; spurs as additive ribs | `ProfileCurve` has zero slope at every control, so interior controls drew benches and spur knobs ("needles") |
| 5 | valleys carved into the envelope to a concave floor | ribs too faint to read |
| 6 | floor = running minimum of envelope − depth | the concave floor overtook the envelope mid-slope; carve only in the upper half |
| 7 | troughs continue to the coast ≤ 7.2 m | valleys ended mid-slope and water spread on the convex coastal ramp |
| 8 | V-shaped cross-section `(1 − v)²` | flat-bottomed valleys let D8 run in parallel (no change in drainage class) |

The other five seeds were never looked at during iteration, but every parameter except the authoring
defaults of §11.2 is an existing project value, so the numbers above are evidence about the structure,
not about a tuned fit.

### 11.7 Can the current island hold the structure?

**Yes, for a single connected branched system — at a price the footprint sets.** On all seven seeds the
skeleton fits two branches, two or three culminations and one or two saddles, and its valleys reach the
coast without a depression or a moved shoreline. The coast gate does not limit it. What the footprint
does limit: crest height on narrow islands, spur length, and — decisively — **the share of flat land**: a
system that reads as mountains from V2/V3 occupies 30–38 % of the land and leaves 12–16 % below 5°.
Whether that trade is acceptable is GEO-7 (the ≥ 35 % flat-land and 6–15 % steep targets are existing
Draft numbers); whether a larger footprint should relieve it is GEO-1. Neither blocks the next
experiment.

**[Qualified 2026-10-20, §13.2]** The flat-land share above was measured with G3's own sizing (30–38 % of
the land) on today's coast-distance profile. Neither was varied against the footprint, so "the footprint
limits the share of flat land" is a hypothesis, not a measurement; G3c tests it (brief §8.3).

### 11.8 Recommendation and decisions

**Next task (one): G3b — a converging valley network on the same skeleton.** Keep the G3 spine,
branches and crests; replace "every valley to its nearest shore" with a valley **tree**: trunk valleys
along the low lines between the spine and each branch and across the lowest saddle, side valleys joining
them, lower reaches with flat floors (the lowland corridor); measure Strahler order, spur and crest fit to
catchment boundaries, flat share, lowland-corridor height, S8 and climbing axes with the same harness
(≈ 13 min per full run). It is the highest-value step because both properties G3 failed — a drainage
network and lowland/flat ground — come from valley topology, while the macro structure it was meant to
prove is already there.

| Alternative | Trade-off |
|---|---|
| smooth the field construction first (no fans, facets) | improves the look, answers no geographic question; worth doing once the topology is settled |
| placement decision now (G4, GEO-3) | premature: whether frozen Stage 3A–3C can be the "special case" depends on the valley topology G3b chooses |
| deeper valleys | measured (`-v50`): no visible change, more climbing axes |
| owner answers GEO-1 / GEO-7 first | possible at any time; G3b's flat-share result is the evidence those answers need |

**Evidence for the open questions (not answers):** GEO-2 — option (a), one connected branched system
counted as one T1, fits on all seven seeds; GEO-3 — the evidence favours a shared skeleton (option (c))
over (a), which cannot reach the massif-free worlds, and (b), whose ridges would lack valleys; GEO-4 —
the gate costs 2–6 % of the system's volume, so it constrains coasts, not the mountains; GEO-7 — the
flat-land and steep-land targets conflict with a footprint-filling system. **No owner decision blocks G3b.**
The owner may also decide whether the uncommitted research file stays in the repository (as earlier
dump-first research code did) or is deleted.

### 11.9 Verification

| Check | Result |
|---|---|
| production baseline | `H4A` rebuilt in every G3 process; hashes equal the pinned values on all 7 seeds (`e2bad571…`, `2076ecf7…`, `4b7ae0f5…`, `6a7ad6ed…`, `f86e8c15…`, `a218e07f…`, `48012b8c…`) |
| determinism | a second fresh process (110201, 1337, `base`, G1 on) gave byte-identical lattices, identical G0 summary lines and 12/12 identical G1 images; a third process (all 7 seeds, both variants, G1 off, after a comment-only edit) gave all 14 lattices byte-identical |
| land mask, ocean | 0 cells changed on every seed and variant |
| production files | MD5 of the four gitignored tile assets and `Overworld_Generator.unity` identical before and after; `git status` shows only the new Editor file and its `.meta` beside this documentation |
| compilation | the Unity log of the run that recompiled the file reports no compiler error or warning |
| **not run** | the EditMode suite: no tested code changed; the 1000–1039 sweep: G3 is a seven-seed experiment |

---

## 12. G3b — a converging valley tree on the G3 skeleton (research, 2026-10-19 … 20)

**Mandate.** The isolated next step §11.8 proposed, run as research under the owner's G3 commission.
Nothing here is a decision: GEO-1 … GEO-5 and GEO-7 stay open. Production code, the bake, the scene, the
tile assets, frozen Stage 1–4A and Stage 3A–3E files and every pinned hash are unchanged (§12.9).

### 12.1 Hypothesis and the candidate

> G3 failed on a drainage network, a lowland corridor and flat land because every valley ran straight to
> its nearest shore. A converging valley tree on the same skeleton — trunks along the low lines between
> the spine and each branch and across the lowest saddle, side valleys joining them, flat floors in the
> lower reaches — fixes all three.

What would count (none of it an approved threshold, GEO-7): Strahler order ≥ 3 on the largest system
(§7.2 D-b, proposed target); a crossing of the range near the floor surface; land below 5° clearly above
G3's 12–16 %; and G3's hygiene kept — 0 S8, no climbing valley axis, mask and ocean unchanged.

**Candidate `G3-net-g200`.** G3's spine, branches, culminations, saddles, crests and envelope `E` are
kept exactly; G3's spurs and its valleys to the nearest shore are replaced by the tree of §12.2. It is
measured beside `G3-skel-g200` in the same process with the unchanged G0 and G1. Two sensitivities ran
separately: `G3-net-g200-rel` (the routing relief added to the surface) and `G3-net-g100` (the same tree
with a 100 m coast gate — GEO-4 evidence, not a proposal).

### 12.2 The model as built

| Part | Rule | Values (source) |
|---|---|---|
| design height | `D = (H4A − U2 − A) + g·E`, `g = smoothstep(d / 200 m)` — the gated surface `Compose` writes; trunks are traced, the tree routed and every floor made monotone on it | 200 m = `UpliftCoastGateMeters` |
| wedge trunks | one per wedge between a branch and its parent: head 0.35 × the smaller root half-width along the wedge bisector, steepest descent on `D` with step halving; a trunk reaching an earlier trunk (within 25 m + half its flat floor) joins it | authoring defaults |
| corridor | across the **lowest** declared saddle: two halves from the col, each starting 0.2 half-widths off the crest and descending on `D` | — |
| trunk and corridor floors | trunk `lerp(E, −7.2 m, smoothstep(t / 0.45))`; corridor `lerp(min(E, 18 m), −7.2 m, …)`, so the col sits 18 m above the floor surface | 18 m = `IslandReliefSettings.PlateUpliftMeters`; 7.2 m = `RidgeSettings.CorridorIncisionMeters`; 0.45 authoring |
| flat floors (lower reaches) | half-width `min(85 m, 0.4 × root half-width) × smoothstep(t / 0.45)` (corridor: full from the col), cross-fall 1.5° toward the axis, walls `1 − smoothstep` over 0.6 × root half-width | 85 m = `HydrologySettings.ValleyHalfWidthMaxMeters`; the rest authoring |
| side valleys | links of a drainage tree on the skeleton's 10.9 m grid: routing height = `D` with the trunks carved, plus a perturbation `0.08 · ‖∇‖ · λ · n(p)` summed over λ = 250 and 600 m (`‖∇‖` the smaller of the slope over ±1 and ±3 cells; stream `research.g3b.route`). It only decides where flow lines merge and is **not** in the height. Priority flood toward the ocean, the coastal fringe (≤ 60 m from the shore) and the trunk floors; D8; accumulation; links where ≥ 15 000 m² drains, split at confluences | 15 000 m² = ½ `HydrologySettings.ChannelInitiationAreaSqm`; 60 m = G3's spur toe; κ 0.08 and the two λ authoring, chosen during iteration (below) |
| link carve | depth `0.08 · √area` below the routing height (tapered over 60 m at a head), V half-width `0.5 · √area` in [30, 160] m; above 150 000 m² a flat floor | authoring defaults |
| floors | every axis a running minimum of the gated absolute height from its head, each tributary's mouth floor imposed on the node it joins (tributaries first), then bounded to at most 7.2 m below the floor surface | 7.2 m = `CorridorIncisionMeters` |
| mouths | a trunk or link that joins nothing continues straight down the coast distance to the shore (G3's troughs) | — |

Every number is an existing project value (named) or an **authoring default**; none is measured or approved.

### 12.3 Code, data flow and isolation

The same uncommitted Editor-only research file, `Editor/TerrainGenG3MountainSystemDump.cs`, now
≈ 3 400 lines; nothing references it. G3's code path is untouched apart from one refactor (the envelope
moved into its own method), and `G3-skel-g200` reproduces all seven §11.4 lattice hashes byte for byte.
G0 and G1 were not modified. New: variants `net`, `netrel`, `netg100`; diagnostics for the designed tree
(its Strahler order with and without the 30 000 m² channel area, the measured-against-designed area at
each link mouth), the lowest crossing of the spine line, the designed divides of the routed tree, the
location of every S8 depression and the cause of every climbing axis.

```
Unity.exe -batchmode -quit -projectPath <project> \
  -executeMethod GameDev.EditorTools.TerrainGenG3MountainSystemDump.DumpBatch \
  -g3Seeds 110201,424242,20260905,7,1337,256,777 -g3Variants base,net -g3Review true -g3Out <dir>
# sensitivities: -g3Variants netrel,netg100 -g3Review false
```

Main run 12 min 41 s (G1 included), sensitivities 4 min 5 s. Building the tree takes 1.16–1.57 s per seed
(G3's skeleton 0.42–0.75 s). Outputs, all outside the repository, in the session scratchpad
(`…/scratchpad/g3b/final/`, `…/sens/`): `g3_report.txt`, `tables.txt`, lattices, structure and flow PNGs,
G1 `review/` V1–V6 + JSON, and `g3b_contact_sheet_V2.png` (`H4A`, `G3-skel-g200`, `G3-net-g200`, seven
seeds). The commands above regenerate the report, the lattices and every image byte for byte (§12.9);
`tables.txt` and the contact sheet were assembled from them outside Unity.

### 12.4 Baseline, G3 and G3b

**[MEASURED]**, G0 definitions (§7.6) unless marked G3. Lowest crossing = the lowest point of the
composed surface on the spine line between its outermost culminations, with its height above the floor
surface `H4A − U2 − A` in brackets; for `H4A` the same line is read on production.

| seed | surface | D-b systems / tributaries / Strahler / river km | S8 | < 5° / > 45° % | lowest crossing m | land > 110 % | crest / side-divide on catchment boundary % (control) | climbing axes (worst m) |
|---|---|---|---|---|---|---|---|---|
| 110201 | `H4A` | 8 / 0 / 1 / 1.95 | 0 | 37.2 / 5.7 | 66 | 9.6 | 0 / 1 (3) | — |
| | G3-skel | 8 / 0 / 1 / 1.63 | 0 | 13.6 / 12.3 | 179 (99) | 29.7 | 8 / 2 (1) | 0 of 15 |
| | **G3-net** | 8 / 4 / 2 / 2.21 | 0 | 16.5 / 13.1 | 90 (20) | 23.4 | 25 / 24 (3) | 4 of 30 (1.66) |
| 424242 | `H4A` | 5 / 2 / 2 / 1.14 | 0 | 27.9 / 6.6 | 67 | 12.8 | 2 / 6 (2) | — |
| | G3-skel | 7 / 3 / 2 / 1.65 | 0 | 15.5 / 19.0 | 205 (138) | 29.2 | 16 / 3 (3) | 1 of 12 (0.35) |
| | **G3-net** | 12 / 4 / 2 / 2.97 | 1 | 16.8 / 17.7 | 82 (20) | 23.4 | 35 / 21 (3) | 4 of 35 (1.58) |
| 20260905 | `H4A` | 11 / 4 / 2 / 2.51 | 0 | 25.4 / 7.4 | 64 | 18.7 | 12 / 12 (3) | — |
| | G3-skel | 14 / 3 / 2 / 3.19 | 0 | 15.4 / 11.5 | 184 (112) | 28.3 | 19 / 2 (4) | 0 of 20 |
| | **G3-net** | 12 / 6 / 2 / 3.77 | 0 | 17.1 / 10.5 | 87 (20) | 22.5 | 33 / 19 (4) | 1 of 37 (0.26) |
| 7 | `H4A` | 7 / 0 / 1 / 1.09 | 0 | 30.4 / 5.4 | 73 | 10.6 | 3 / 8 (2) | — |
| | G3-skel | 9 / 1 / 2 / 0.96 | 0 | 12.4 / 21.1 | 213 (140) | 28.8 | 14 / 2 (2) | 0 of 25 |
| | **G3-net** | 9 / 2 / 2 / 2.21 | 3 | 16.4 / 19.7 | 91 (18) | 22.7 | 25 / 13 (2) | 10 of 46 (8.68) |
| 1337 | `H4A` | 8 / 2 / 2 / 1.54 | 0 | 49.9 / 0.0 | 71 | 0 | n/a (2) | — |
| | G3-skel | 8 / 1 / 2 / 1.35 | 0 | 11.9 / 18.9 | 247 (169) | 29.3 | 28 / 0 (2) | 0 of 16 |
| | **G3-net** | 10 / 2 / 2 / 3.53 | 4 | 15.7 / 17.1 | 94 (20) | 22.8 | 32 / 15 (4) | 6 of 42 (4.87) |
| 256 | `H4A` | 10 / 0 / 1 / 0.84 | 0 | 44.3 / 0.0 | 58 | 0 | n/a (1) | — |
| | G3-skel | 9 / 2 / 2 / 1.61 | 0 | 15.9 / 9.6 | 186 (120) | 22.6 | 18 / 0 (1) | 0 of 8 |
| | **G3-net** | 9 / 4 / 2 / 2.47 | 0 | 16.6 / 10.9 | 81 (19) | 16.7 | 18 / 16 (2) | 5 of 34 (1.59) |
| 777 | `H4A` | 10 / 2 / 2 / 1.38 | 0 | 54.1 / 0.0 | 63 | 0 | n/a (2) | — |
| | G3-skel | 10 / 0 / 1 / 1.79 | 0 | 13.4 / 12.1 | 192 (121) | 26.9 | 20 / 1 (2) | 0 of 16 |
| | **G3-net** | 11 / 2 / 2 / 2.82 | 4 | 15.2 / 12.6 | 89 (18) | 22.2 | 19 / 16 (2) | 6 of 33 (1.25) |

"Side divide" is G3's spur lines for G3-skel and the designed divides of the routed tree for G3-net;
`H4A` uses production's declared massif crests and Stage 3 spurs. On every seed and surface: `H4A` hash
equal to the pinned value, land mask and ocean unchanged (0 cells), single dome not flagged on G3-skel
and G3-net.

**The tree itself (G3-net).**

| seed | trunks + corridor halves | links (km) | links into trunks / into links / to the coast | largest link km² | designed Strahler: all links / links ≥ 30 000 m² | links designed ≥ 30 000 m²: gathering it on the lattice, median measured / designed area |
|---|---|---|---|---|---|---|
| 110201 | 4 + 2 | 24 (4.18) | 6 / 6 / 12 | 0.145 | 2 / 2 | 8 of 10, 0.99 |
| 424242 | 4 + 2 | 29 (4.92) | 6 / 8 / 15 | 0.096 | 3 / 3 | 9 of 15, 0.76 |
| 20260905 | 3 + 2 (one wedge too short) | 32 (5.30) | 5 / 8 / 19 | 0.095 | 2 / 2 | 13 of 15, 0.94 |
| 7 | 4 + 2 | 40 (6.32) | 6 / 16 / 18 | 0.160 | 3 / 3 | 11 of 22, 0.66 |
| 1337 | 4 + 2 | 36 (5.58) | 2 / 22 / 12 | 0.214 | 3 / 3 | 15 of 21, 0.81 |
| 256 | 4 + 2 | 28 (4.45) | 3 / 13 / 12 | 0.109 | 2 / 2 | 9 of 12, 0.81 |
| 777 | 4 + 2 | 27 (5.19) | 4 / 6 / 17 | 0.140 | 2 / 2 | 5 of 8, 1.02 |

Further G3-net readings: rivers inside declared valleys (D-e rule on G3's own valleys) 95–100 % (G3-skel
18–75 %); declared crests on flow divides 38–55 % (G3-skel 30–49 %), designed divides 10–18 % against a
6–7 % control; land with gated `S` above the plate step 24–31 % (G3-skel 30–38 %); the coast gate removes
2.6–6.4 % of `S`; separate summits 2–3 on every seed (1337: 1 → 3, the corridor splits its range).

**S8 depressions (G3-net).** 12 on four seeds — 424242 1, 7 3, 1337 4, 777 4 — 0.26–1.51 m deep and
112–3 387 m². Ten sit on a routed link axis, one 19 m off a link (7), and one, 1.51 m and 3 387 m²,
beside a wedge trunk on 1337 where the trunk does not follow the crease between spine and branch; that one
was present in every iteration on 1337.

**Climbing axes (G3-net), 36 of 257.** 26 are the steep V axis sampled between lattice points: the axis's
own floor is monotone there and the surface stands ≤ 2.2 m above it. 8 are genuine: where a routed link
runs up the floor surface, the 7.2 m trough bound lifts its floor (worst 7.9 m, seed 7). 2 are trunk
heads crossing a deeper carve (≤ 4.9 m).

**Sensitivities (seven seeds).**

| variant | D-b tributaries / Strahler | river km | S8 (seeds, deepest) | < 5° % | reading |
|---|---|---|---|---|---|
| `G3-net-g200-rel` (relief in the surface) | 2–6 / 2 on all seven | 2.30–3.76 | 28 on six seeds, deepest 9.86 m | 15.0–17.5 | no gain; the hollows deepen the bend-facet artefact at a branch root into a 10 m pit (1337) — the reason the candidate keeps the relief out of the height |
| `G3-net-g100` (100 m coast gate) | 6–13 / 3 on 424242, 2 elsewhere | 2.60–4.24 | 116 on all seven, 0.25–1.51 m | 14.9–17.1 | designed order 3 on four seeds; tributaries roughly double, but the troughs and floors, built for a 200 m gate, leave 12–22 shallow pits per seed and 12–27 climbing axes |

### 12.5 Visual reading — agent reading, owner review pending

From `g3b_contact_sheet_V2.png` and the per-seed V2–V5:

- **Corridor: yes.** On every seed the range is cut by a broad low pass at its lowest saddle, so it reads
  as two (1337: three) bodies along one line rather than one wall; the wedge trunks show as wide smooth
  floors between a branch and the spine.
- **Dissection: weaker than G3.** G3's parallel spur grooves are gone; routed links begin where
  15 000 m² drains, mostly on the lower flanks and the coastal ramp, so the upper flanks read smoother than
  in G3 and the side valleys hardly register at V2/V3.
- **New artefacts.** Terraced, amphitheatre-like steps at branch roots where the corridor's and the wedge
  trunks' walls meet (424242, 256, 20260905, 1337), and a vertical sliver beside the culmination at a
  branch root (110201). They belong to the class §11.5 named — the closest segment of a polyline jumps —
  now made larger by wide trunk walls.

### 12.6 What worked, what failed, and why

| Finding | Evidence | Cause |
|---|---|---|
| **Worked: a lowland corridor** | lowest crossing of the range 81–94 m, 18–20 m above the floor surface, on all seven (G3-skel 179–247 m, 99–169 m above) | built in: the corridor floor is 18 m above the floor surface at the lowest saddle |
| **Worked: drainage follows the declared structure** | rivers inside declared valleys 95–100 %; side divides on catchment boundaries 13–24 % against a 2–4 % control (G3's spurs 0–3 %); tributaries 2–6 (G3-skel 0–3), river km 2.21–3.77 (0.96–3.19) | valleys routed on the surface that is written, and carved to monotone floors |
| **Failed: no Strahler ≥ 3** | measured order 2 on all seven; the tree's own design order is 2 on four seeds and 3 on three; about two thirds of the links designed to drain 30 000 m² gather it on the lattice | ~~**footprint and coast gate**~~ **coastal transition and flank construction** [corrected 2026-10-20, §13.2 — the footprint was not varied]: 54–59 % of the land is the coastal ramp, where the gate takes `S` to zero and the floor surface — a coast-distance dome — sends water straight to the shore; the mountain body's flanks are 100–420 m deep. So every basin stays at ≤ 0.21 km², while order 3 at a 30 000 m² channel area needs two nested pairs of first-order channels, in practice a basin of ~0.2–0.4 km² with room to converge. A 100 m gate lets the tree converge further (tributaries ≈ ×2, order 3 once) |
| **Failed: flat land** | land < 5° 15.2–17.1 % (G3-skel 11.9–15.9, `H4A` 25.4–54.1); land > 45° 10.5–19.7 % | the trunk flats are small against the island; the mountain body still covers 24–31 % of the land and the ramp is not flat |
| **Failed: hygiene** | S8 0 → 12 on four seeds; climbing axes 0–1 → 1–10 per seed (8 genuine) | links that run up the floor surface meet the 7.2 m trough bound; one trunk misses its crease |
| **Visual: dissection weaker than G3** | §12.5 | links start low; G3's spur grooves removed |
| **Diagnostic lesson** | before routing moved onto the gated surface, the designed tree reached order 3–4, but only about half of its 30 000 m² links were realised (median measured / designed 0.31–0.87); afterwards 0.66–1.02 and an honest design order of 2–3 | a valley network designed on a surface other than the one written is not the network the surface drains |

**Calibration caveat.** Thirteen iterations ran on 110201 and 1337 (the other five seeds were not looked
at); several authoring defaults — κ, the two λ, the 15 000 m² link area — were chosen during them:

| Try | Change | Reason / what it showed |
|---|---|---|
| 1 | wedge trunks + corridor; G3's spur-gap valleys descending on `D`; consecutive valleys paired at 0.75 of their length | order 2, 2 S8, 6 climbing axes; spurs ending on a trunk fell below the 120 m spur minimum, so the wedges kept almost no side valleys |
| 2 | spurs ending on a trunk accepted at half that minimum | side valleys reached the trunks, but arms climbed up to 15 m: the confluence at the pair's midpoint ran one arm up a trunk wall |
| 3 | side valleys routed on `D` with the trunks carved; confluence on the lower arm, before the trunk walls | 6 of 10 side valleys into trunks, stems still to the coast, order 2, long lateral connectors — geometric pairing dropped |
| 4 | side valleys = links of a routed D8 tree, κ 0.06, λ 160 / 400 m | designed order 3 (57–60 links), links only on the coastal ramp; measured order 1–2 |
| 5 | κ 0.2 | links wandered along the shore and climbed up to 13 m |
| 6 | coastal fringe as a sink; perturbation on the mountain body only | clean (1 S8 per seed) but 20–22 links, designed order 2 |
| 7 | κ 0.3 | the perturbation (amplitude up to ~70 m) outweighed the slope and the flood filled its hollows to flats: order 2 |
| 8 | κ 0.08, λ 250 / 600 m, on all land | designed order 3 at 30 000 m², measured 2 |
| 9 | the perturbation added to the surface | 10 tributaries, order 2, a 10 m pit on 1337 |
| 10 | realisation diagnostic | only about half of the links designed to drain 30 000 m² gathered it (median 0.31–0.87) |
| 11 | routing, trunk tracing and floors on the gated surface | realisation median 0.82–1.06; the design order fell to 2 on 110201 |
| 12 | relief amplitude from the smaller slope reading; 100 m gate tried | 100 m gate: tributaries ×2, order 3 once, 13–21 pits |
| 13 | mouths continued to the shore; relief kept out of the height | 0 / 4 S8 on the two seeds |

The calibration moved the design toward the surface that is written, never toward a metric target; no
G0 definition or threshold changed.

### 12.7 Verdict

**The hypothesis holds for the corridor and fails for drainage and flat land.** A converging tree on the
G3 skeleton gives every seed a lowland pass across the range 18–20 m above the floor surface, puts the
rivers inside declared valleys and on declared divides, more than doubles tributaries (10 → 24 over the
seven seeds) and lengthens the rivers by about two thirds — but on none of the seven seeds does the island
drain through a network of Strahler order ≥ 3 at the production 30 000 m² channel area, and land below 5°
stays at 15–17 %. The limit is not the valley topology: once
the tree is routed on the surface that is actually written, its own design order is 2–3, because the
gated coastal ramp (54–59 % of the land) and the 100–420 m flanks keep every basin at ≤ 0.21 km². G3b
also costs hygiene G3 had: 12 shallow S8 depressions on four seeds and 8 genuinely climbing axes, and new
construction artefacts at branch roots. It is not a production candidate.

**[Corrected 2026-10-20, §13.2]** "The limit is not the valley topology" holds only for the one family
tested — a tree routed on the coast-distance datum. The measured causes are the coastal transition (the
gated ramp, confirmed by the 100 m sensitivity) and the flank construction; the footprint was never varied
and remains a hypothesis.

### 12.8 Recommendation and decisions

**Next task (one): owner review G2 with the G3 and G3b evidence, before any further candidate.** Another
iteration on today's footprint and gate would tune parameters against ~~a geometric limit~~ the limits the
coastal transition and the construction set [corrected 2026-10-20, §13.2: no footprint limit was
measured]. The questions
that decide what a G3c could achieve are owner questions:

| Question | G3b evidence (not an answer) |
|---|---|
| GEO-1 scale | even with lowland trunks and a corridor, flat land is 15–17 % and basins ≤ 0.21 km²; ~~a converging network of order ≥ 3 and ≥ 35 % flat land both need room the footprint does not have~~ [corrected 2026-10-20, §13.2: whether the footprint lacks that room is untested — the measured limits are the coastal transition and the construction] |
| GEO-3 placement | unchanged reading: ridges, trunks, corridor and routed side valleys all come from one skeleton (option (c)); new: the network must be routed on the final gated surface, so wherever it lives it must see that surface |
| GEO-4 coast contract | the 200 m gate is the binding constraint on drainage convergence, not on the mountains (§11.7): a 100 m gate roughly doubles tributaries and reaches order 3 once, at the price of pits a shorter gate would need its own trough design for |
| GEO-5 basins | none tried; the 12 pits are defects, not basins |
| GEO-7 targets | no variant reaches order ≥ 3 at 30 000 m² with the 200 m gate; whether that target (or that channel area) fits a ~3 km² island, and whether ≥ 35 % flat land can coexist with a mountain system on it, is the owner's call |

| Alternative | Trade-off |
|---|---|
| G3c with a 100 m gate and troughs designed for it | tests GEO-4 (b) further; presupposes a coast-contract change the owner has not made |
| fix the hygiene and artefacts first (trough bound, crease tracking, smooth parameterisation) | makes G3b cleaner, changes none of the three answers above |
| a smaller channel area for D-b | changes the measurement, not the island; GEO-7's to decide |
| a larger island | GEO-1; moves Stage 1 hashes and every downstream tuning |

The owner may also decide whether the uncommitted research file stays in the repository; it now holds G3
and G3b.

### 12.9 Verification

| Check | Result |
|---|---|
| production baseline | `H4A` rebuilt in every process; all seven hashes equal the pinned values |
| G3 unchanged | `G3-skel-g200` lattice hashes equal §11.4 on all seven seeds |
| candidate hashes (`G3-net-g200`) | 110201 `72b133fba3073798`, 424242 `5e6ab811e17a956a`, 20260905 `df810144bd99842a`, 7 `f0dd4f164e16ca54`, 1337 `319ed757f92a33a1`, 256 `b14e8e9315c5aba5`, 777 `bcd8b5ddf88d1d4f` |
| determinism | a second fresh process (all seven seeds, `base,net`, G1 off): 14 of 14 lattices, the whole report and every structure and flow PNG byte-identical; a third (110201 and 1337, `net`, G1 on): 28 of 28 G1 images and JSON byte-identical to the main run |
| land mask, ocean | 0 cells changed on every seed and variant, sensitivities included |
| production files | before the first run, copies and MD5 of 63 files — the four tile assets and the rest of `Settings/Terrain/`, `Overworld_Generator.unity` and its `.meta`, `Scenes/Overworld_Generator/`, `URP_Pipeline.asset`, `URP_Renderer.asset`, every `ProjectSettings/*.asset`, `packages-lock.json`, and the G0 and G1 sources: all identical after 18 Unity processes; `git status` shows only this documentation and the research file with its `.meta` |
| compilation | no compiler error or warning in the logs of the runs that recompiled the file |
| **not run** | the EditMode suite (no tested code changed); the 1000–1039 sweep (G3b is a seven-seed experiment); owner review of V2–V5 |

---

## 13. G2 — target adoption and corrections (2026-10-20)

**Mandate.** The owner's G2 review, recorded in
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (v0.2). This section records what it changes
here; the brief owns the decisions, the requirements framework and the G3c plan.

### 13.1 What the review decided and what it left open

- **Approved — target geography D6** (brief §9), from the owner's concept image
  [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp).
  The image is a project reference inspired by DragonSword: Awakening's visual direction, **not an
  official map**; it is a composition, and no count, size, height or position is read from it as a
  requirement. Where D6 and the §4 proposal differ, D6 is the target; §4's hierarchy and grammar remain the
  proposed vocabulary for checking it.
- **Owner directions (not final answers):** GEO-1 — decide today's footprint (A) against about 40 % more
  land in the same 2.8 km world (B) by experiment; a 3 × 3-tile world is a separate travel decision.
  GEO-4 — spatially varied coastal transitions; in G3c the shoreline is fixed within each footprint and the
  coastal profile varies by segment; the production coastal contract stays open. GEO-7 — metrics diagnose,
  they do not restrict; Strahler order, basin area and the low-slope share are diagnostics in G3c.
- **Scope set, run not authorised — G3c (D8)**, brief §8.3: method, parameters, fixed conditions, metrics,
  readings and the A/B comparison rule are written before any run. The owner's review of the plan
  (2026-10-20) fixed one contour per footprint, an explicit seed list (calibration 110201 and 1337;
  evaluation 424242, 20260905, 7, 256, 777, 1000–1004), a calibration-only configuration screen,
  cliffs judged by steepness and walking connectivity without an impassability verdict, and conclusions
  stated about the two contours, not about area alone. The owner gave the go-ahead the same day and G3c ran
  (§14).
- **Still open:** GEO-1 … GEO-5 and GEO-7 as decisions; the new **GEO-8** (plateau tiers against
  `WORLD_RELIEF_PRINCIPLES.md` §1); 3F-20; the reference DEMs of §7.5 (not chosen).

**Status reminders.** Production terrain is `H4A = H1 + U2 + R4A` with Stage 4A R1 as the authority. G3
and G3b are research prototypes, not production geography. The Stage 3F E-family prototype is not in the
repository and not approved. Stage 3F's carve is experimental and off the production path; the hydrology
carve exists in the bake but is off by default. G3c is research only.

### 13.2 Corrections to the attribution of G3 and G3b

The original wording is kept, struck through, where it stood; every measurement is unchanged.

| Where | Said | Corrected to | Evidence |
|---|---|---|---|
| §11.6 "Conflict: mountains against flat land" | cause: footprint | G3's sizing (30–38 % of the land) and the non-flat coastal ramp | no run varied the footprint; the mountain share is G3's authoring choice (§11.2) |
| §11.6 "Footprint limits, measured" | the inscribed radius bounds crest height and spur length | the same, **under G3's own caps** (crest ≤ 0.7 × coast distance, half-width 0.6 r, 120 m spur minimum) | those caps are G3 authoring defaults (§11.2; the 120 m spur minimum, §11.4), not geography |
| §11.7 | the footprint "decisively" limits the flat-land share | a hypothesis: measured with G3's sizing on today's coast profile | as above |
| §12.6 "Failed: no Strahler ≥ 3" | cause: footprint and coast gate | coastal transition and flank construction | the 100 m-gate sensitivity changed tributaries ×2 and reached order 3 once (§12.4); no footprint was tested |
| §12.7 verdict | "the limit is not the valley topology" | true only for the family tested, a tree routed on the coast-distance datum | one family of valley topology was tried |
| §12.8 | "a geometric limit"; GEO-1 row "room the footprint does not have" | limits set by the coastal transition and the construction; footprint untested | as above |
| §9 G3 row and "Smallest useful sequence" | "the limit is the footprint and the coast gate" | coastal transition and construction; footprint a hypothesis | as above |
| [14](../../../14%20World%20Generation%20Roadmap.md), [99](../../../99%20Open%20Questions.md) GEO-1, [AGENTS.md](../../../AGENTS.md) item 6 | the same attribution, copied from §12.7 | corrected the same way in each | as above |

**What the evidence does support, unchanged:** the uniform coastal ramp — 54–59 % of the land within
200 m of the shore **[PRIOR]**, 50–59 % on the five lattices measured for G2, with the 60–200 m band at a
median 11.5–15.8° and 19–24 % buildable — drains the land straight to the sea and leaves **no coastal
settlement site on any surface measured, production `H4A` included** (brief §4.2); and the G3/G3b
construction produces fan, facet and terrace artefacts of the distance-to-polyline family that
`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §16 rejects.

### 13.3 Gaps the approved target exposes

Recorded as research questions in brief §8.4, nothing implemented: plateau tiers against the T2 row and the
2.5× rule (**GEO-8**); impassable cliffs against a jump that climbs every tested slope up to 70° (3F-20);
ownership of a hinterland-driven coastal profile, since `H1` is golden-hashed Stage 1 height (GEO-4); G1
cannot anchor on a non-production coastline (footprint B); G0 definitions tied to production structure;
robustness across coastline shapes and the separation of area from shape, which G3c's one contour per
footprint does not test (owner direction: a separate later stage, G3d); an offshore archipelago beyond
Stage 1's two-islet budget; the image's 0–5 km scale bar, which is illustrative only.

---

## 14. G3c — region-first geography on two contours (research, authorised 2026-10-20)

**Mandate.** The owner authorised G3c on 2026-10-20 on the pre-run checklist of
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) §9: contours A and B by the recorded rule, the
seed list (calibration 110201, 1337; evaluation 424242, 20260905, 7, 256, 777, 1000–1004), the matrix
16 + 20 + 20, tier rules diagnostic inside G3c, readings R1–R8 and the comparison rule of brief §8.3.
Research only; production, the bake, the scene, tile assets, frozen stages and pinned hashes stay
unchanged.

### 14.1 Operational definitions of R1–R8 (written before step 1)

Provisional values, authoring defaults, never production gates. "Land" is height > 24 m; the ocean is the
border-connected sea of the hydrology package. Region labels (highland, upland, lowland basin, plateau top,
plateau wall or ramp) are the candidate's declared regions. **Served slope** is the slope of the Triangle
Reading (each lattice quad split south-west to north-east); **T-edge reachable** means connected to a
walkable land triangle on the shore (a walkable triangle sharing an edge with a sea triangle) through
walkable triangles (slope < 45°) sharing edges; the jump is not modelled. **Buildable** is the lattice
central-difference slope ≤ 6.84° used in brief §4.2.

| Reading | Operational definition |
|---|---|
| R1 | (a) ≥ 2 summits of the G0 merge tree with prominence ≥ 30 m inside the highland; (b) the G0 flow-divide skeleton, restricted to the highland, has ≥ 1 branch point (crossing number ≥ 3); (c) ≥ 1 pass: the key col of a summit with prominence ≥ 30 m, inside the highland, whose 8-neighbours drain (D8 on the priority-flood surface) to ≥ 2 different ocean outlets each draining ≥ 0.1 km², and which is T-edge reachable; (d) the G0 single-dome rule not flagged, read with the candidate's own floor (land outside highland and plateaus) and the contour's coast distance |
| R2 | ≥ 2 verified plateaus whose centres are ≥ 500 m apart; a declared plateau is verified when ≥ 60 % of its top cells are ≤ 5°, ≥ 20 % of its rim samples (every 10 m) start a continuous run of served slope ≥ 45° that drops ≥ 10 m, its rim lies within 150 m of highland or upland, and its top is T-edge reachable |
| R3 | ≥ 2 contiguous buildable patches of ≥ 10 ha in different regions (a region is one lowland basin, or the upland draining to one ocean outlet) |
| R4 | settlement sites by brief §4.1 (core radii 60 / 90 / 150 m, disc without sea and ≥ 90 % buildable, centres ≥ 450 m apart), placed deterministically: the capital site with the lowest mean slope, then the coastal site (core radius + 100 m reaches the sea) with the lowest mean slope, then further sites by lowest mean slope; passes with a capital plus ≥ 2 further sites, ≥ 1 of them coastal, in ≥ 3 different regions (R3 regions, plateaus and the highland count as regions) |
| R5 | on the hydrology package's network (`HydrologySettings.Default`): every river system — the reaches draining to one ocean termination — that has a source node in the highland or on a plateau ends within 150 m of a declared outlet |
| R6 | each shoreline cell (land, 4-adjacent to the ocean) is **low** (land within 60 m rises < 4 m above sea level), **rocky** (4–15 m) or **cliff** (≥ 15 m, the G0 M6 rule); passes with ≥ 3 classes each ≥ 5 % of the shoreline, ≥ 80 % of low cells with a lowland or valley hinterland and ≥ 80 % of cliff cells with an upland, highland or plateau hinterland (majority label within 300 m), and every declared outlet on a low cell |
| R7 | 0 S8 depressions (G0 D-d: fill depth ≥ 0.25 m over ≥ 100 m²) and 0 declared river axes that climb more than 0.25 m downstream |
| R8 | every R4 site centre, verified plateau top and R1 pass is T-edge reachable |

Reported beside the readings, never deciding them: Strahler order, basin areas, the low-slope share, slope
classes of escarpments (45–60°, 60–70°, > 70°), steep-run heights, walls steeper than 70° ("outside the
tested jump range", 3F-20), tier-rule conformance of each plateau (`WORLD_RELIEF_PRINCIPLES.md` §1), and
land use by distance to the sea (0–60 m, 60–200 m, interior).

Two implementation details were settled during calibration, before the freeze, without changing any
definition or value: R7's climbing check samples the lattice vertex nearest each river node — the cells the
construction writes the river into — because bilinear sampling between cells reads a V-valley wall as a
climb (G3b recorded the same effect, §12.4); and R5 is coded exactly as written, so it is vacuously true
when no system rises in the highland or on a plateau — the count of such systems is reported beside it.

### 14.2 The construction as built

Editor-only research code, uncommitted: `Editor/TerrainGenG3cRegionFirstDump.cs` (entry, contours, report),
`TerrainGenG3cRegionFirstGeography.cs` (construction), `TerrainGenG3cMeasure.cs` (R1–R8 and diagnostics),
`TerrainGenG3cViews.cs` (views with G1's presentation; G1 itself unchanged). Nothing references it.

1. **Regions** from the fixed land mask: a highland of the configured share around a weighted-diameter spine
   (medial, or confined to one end of the island), two or three lowland basins (30 % of the land) grown from
   seeded wide points, each with a designated outlet on its own coast and a corridor to it; secondary
   outlets where the highland nears the coast; uplands are the rest; 2–4 plateaus beside the highland.
2. **Drainage tree** grown upstream from the designated outlets and filled in with tributaries: spacing by
   region (lowland 200 m, upland 150 m, highland 220 m); floors rise at 0.6 %, 2.5 % and 14 % plus 65 % of
   the massif envelope's rise; a floor may not stand above the shore-derived surface or above what a
   neighbouring floor's valley side reaches.
3. **Surface** = the eikonal distance from the river floors and the shore, climbing at a region slope (lowland
   0.035, upland 0.10, highland 0.8 × culmination factor, shore band 1.5 behind highland or plateaus and
   0.25 before uplands, low mouth plains at every outlet). Ridges are the equidistant lines between valleys —
   duals by construction — and no closed depression can form.
4. **Highland envelope**: inside the highland the surface is capped by a harmonic foot surface plus an
   envelope of 160 m × culmination factor rising to the core; floors are clamped below it.
5. **Plateaus** composed over the surface: a top tilted to its ramp, an uneven wall and talus, a 20° ramp
   toward the lowest ground; the plateau shows only where it stands above the ground, so its uphill side is
   flush. Escarpment heights drawn 18–40 m or 60–120 m with equal probability (§14.1, GEO-8 evidence).
6. **Finishing**: two 3 × 3 smoothing passes away from rivers, shore and plateaus; troughs trapped at plateau
   feet filled to their spill level, with the volume reported.

Every value is an authoring default in world units (the full list is printed in the run reports and the
freeze record); none is measured or approved.

### 14.3 Calibration log (steps 1–2; seeds 110201 and 1337 only)

Eleven construction iterations, each fixing a named defect; no definition, threshold or reading changed.

| Defect | Observed on the calibration seeds | Change |
|---|---|---|
| D1 | the layout hardly varied with the seed | seeded zone axis, zone extent, spine weighting and trimmed ends; seeded lowland seeds |
| D2 | river floors stood above the ground around them; axes "climbed" up to 69 m | sources fixed in the solver; each floor bounded by the shore-derived surface and by neighbouring floors |
| D3, D10 | a sparse network (4–7 km): tributaries died at their seventh step, inside the spacing radius of their own parent | infill passes; the parent's nodes near the junction exempt until the branch can clear them |
| D4 | low-plateau walls one or two lattice steps wide were smoothed below 45° | walls at least three steps wide |
| D5 | secondary outlets ended on cliffs | a low mouth plain at every outlet |
| D6 | eikonal plateau walls formed a raised rim up to 75 m above a sunken top | plateaus composed over the surface instead of as solver costs |
| D7 | 77–84 % of the shoreline was cliff, rocky shores were missing | cliffs only before highland or plateaus, rocky shores before uplands |
| D8, D8b | the massif was a cost cone with one summit; valleys rode on top of the envelope | an envelope the valleys cut into; floors climb 65 % of it |
| D9 | a ramp could face uphill and vanish | the ramp leaves toward the lowest ground |
| D11, D11c, D18 | one summit only: culminations too broad, valleys too dense to let ridges reach the envelope, culminations placed off the highland | narrower culminations at the widest highland points; highland valley spacing 220 m and slope 0.8 |
| D12 | troughs up to 4.7 m deep at plateau feet | local fill to spill level, volume reported |
| D13, D14, D15 | drum-like plateaus, a uniform cliff band, planar eikonal facets | irregular outlines and uneven walls; band width varies along the coast; light smoothing |
| D16, D17 | the envelope cap cut river floors; an eikonal foot cut a trench along the highland's straight edge | floors clamped to the cap; a harmonic foot |

### 14.4 Contours, configuration screen and the freeze

| Contour | Land | Inscribed radius | Bounding box | Crenulation | Within 60 m / 60–200 m of the sea | Stage 1 field hash |
|---|---|---|---|---|---|---|
| A — seed 110201, production | 2.825 km² | 607 m | 2146 × 2188 m | 1.35 | 19.0 / 35.0 % | `7599879a71250a06` (pinned, PASS) |
| B — seed 110201, land fraction [0.49, 0.52] | 3.820 km² (+35 %) | 902 m | 2177 × 2188 m | 1.24 | 14.3 / 27.9 % | `51f34c31710bf526` |

B was accepted on the first pre-registered attempt (3.8–4.1 km², one landmass, inside the margin). It
differs in shape as well as area: the large bay of A is filled and B is close to square.

Screen (R1–R8 met over the two calibration seeds, per contour): s20-med A 13 / B 11; s20-off 13 / 12;
**s30-med 15 / 16**; s30-off 11 / 8. The rule chose **s30-med** (highest lower count). Freeze record
`g3c_freeze_record.txt` (MD5 `f219b434…`) holds the code hashes, contours, choice and every setting; it was
written before any evaluation seed was generated, and the construction did not change afterwards.

### 14.5 Evaluation (step 4): s30-med, ten held-out seeds **[MEASURED]**

| seed | contour | R1 | R2 | R3 | R4 | R5 | R6 | R7 | R8 | met | summits ≥ 30 m / passes | plateaus verified | elevated systems | S8 / climbing axes | Strahler | largest basin km² | < 5° / > 45° % | highest m | coast low / rocky / cliff % |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 424242 | A | Y | Y | Y | Y | n | Y | Y | Y | 7 | 2 / 2 | 2/2 | 0 (planner failed) | 0 / 0 | – | 0.227 | 35.8 / 12.5 | 291 | 43/20/36 |
| 20260905 | A | Y | Y | n | Y | n | Y | Y | Y | 6 | 3 / 1 | 2/2 | 0 (planner failed) | 0 / 0 | – | 0.332 | 37.3 / 13.6 | 281 | 49/7/44 |
| 7 | A | Y | Y | n | Y | Y | Y | Y | Y | 7 | 2 / 1 | 2/2 | 2 | 0 / 0 | 3 | 0.415 | 39.3 / 12.3 | 297 | 41/16/43 |
| 256 | A | n | Y | n | Y | Y | Y | Y | Y | 6 | 1 / 0 | 2/2 | 0 | 0 / 0 | 2 | 0.230 | 37.4 / 11.4 | 270 | 36/23/41 |
| 777 | A | Y | Y | Y | Y | Y | Y | Y | Y | **8** | 5 / 1 | 2/2 | 1 | 0 / 0 | 2 | 0.287 | 38.6 / 14.1 | 276 | 35/19/46 |
| 1000 | A | Y | Y | n | Y | n | Y | Y | Y | 6 | 3 / 1 | 2/2 | 1 | 0 / 0 | 2 | 0.381 | 39.5 / 13.5 | 254 | 32/14/54 |
| 1001 | A | Y | Y | Y | Y | Y | Y | Y | Y | **8** | 4 / 1 | 3/3 | 0 | 0 / 0 | 2 | 0.327 | 39.0 / 11.8 | 253 | 44/10/45 |
| 1002 | A | n | Y | Y | Y | Y | Y | Y | Y | 7 | 2 / 0 | 3/3 | 0 | 0 / 0 | 2 | 0.320 | 40.2 / 13.5 | 284 | 38/15/47 |
| 1003 | A | Y | Y | Y | Y | n | Y | Y | Y | 7 | 2 / 1 | 2/2 | 0 (planner failed) | 0 / 0 | – | 0.232 | 38.6 / 12.8 | 262 | 42/7/51 |
| 1004 | A | Y | Y | n | Y | Y | n | Y | Y | 6 | 2 / 1 | 2/2 | 0 | 0 / 0 | 2 | 0.246 | 38.5 / 11.5 | 281 | 42/20/38 |
| 424242 | B | Y | Y | n | Y | Y | Y | Y | Y | 7 | 2 / 1 | 2/2 | 4 | 0 / 0 | 2 | 0.676 | 40.0 / 4.9 | 307 | 32/27/41 |
| 20260905 | B | Y | Y | n | Y | n | Y | n | Y | 5 | 2 / 1 | 2/2 | 0 (planner failed) | 0 / 1 | – | 0.470 | 38.3 / 5.2 | 297 | 35/25/41 |
| 7 | B | n | Y | n | Y | Y | Y | Y | Y | 6 | 1 / 0 | 2/2 | 2 | 0 / 0 | 3 | 0.938 | 39.2 / 4.0 | 300 | 28/36/36 |
| 256 | B | Y | Y | Y | Y | Y | Y | n | Y | 7 | 2 / 1 | 2/2 | 4 | 0 / 1 | 3 | 0.565 | 39.3 / 6.5 | 308 | 21/32/46 |
| 777 | B | n | n | n | Y | Y | Y | n | n | 3 | 1 / 0 | 1/2 | 4 | 1 / 1 | 2 | 0.525 | 39.4 / 4.8 | 304 | 22/38/40 |
| 1000 | B | Y | Y | Y | Y | Y | Y | n | Y | 7 | 3 / 2 | 2/2 | 4 | 0 / 2 | 3 | 0.885 | 40.0 / 6.8 | 296 | 17/35/47 |
| 1001 | B | Y | Y | n | Y | Y | Y | Y | Y | 7 | 2 / 1 | 2/2 | 3 | 0 / 0 | 3 | 0.782 | 39.3 / 6.1 | 297 | 14/37/49 |
| 1002 | B | n | Y | n | Y | n | Y | Y | Y | 5 | 1 / 0 | 3/3 | 0 (planner failed) | 0 / 0 | – | 0.508 | 37.9 / 4.6 | 301 | 30/32/38 |
| 1003 | B | n | Y | Y | Y | Y | Y | n | Y | 6 | 1 / 0 | 2/2 | 3 | 0 / 1 | 3 | 0.667 | 39.4 / 4.3 | 306 | 25/30/45 |
| 1004 | B | Y | Y | n | Y | n | Y | n | Y | 5 | 2 / 1 | 3/3 | 4 | 0 / 1 | 3 | 0.691 | 40.7 / 5.8 | 304 | 16/34/49 |

Readings held, A / B, over ten seeds: R1 8 / 6, R2 10 / 9, R3 5 / 3, R4 10 / 10, R5 6 / 7 (A vacuously on 4,
B on none), R6 9 / 10, R7 10 / 4, R8 10 / 9. Buildable lowland, B against A: +31 to +43 % on all ten seeds.

### 14.6 The comparison rule

**Neither contour passes**: A meets every reading on 2 of 10 seeds (777, 1001), B on none; the rule needs 7.
**B does not materially improve on A**: it adds ≥ 25 % buildable lowland on 10 of 10 seeds — a nearly
mechanical gain, since lowlands are a fixed 30 % share of 35 % more land — but with worse hygiene (R7 fails on
6 of 10 B seeds, on none of A's). The pre-registered decision for "both fail" applies: **the concept or the
coastal transition is wrong as built, and the world is not grown on this evidence.** The result is stated
about contour A and contour B, not about land area as such (brief §8.3).

### 14.7 What failed, and why

| Reading | Failures A / B | Cause | Kind |
|---|---|---|---|
| R3 | 5 / 7 | buildable land is abundant (57–64 % of the land) and continuous, so patches merge across regions into one component that R3 counts once; R4 holds on every lattice | definition meeting gentle uplands (0.10 → 5.7°) |
| R5 | 4 / 3 | five failures are the hydrology planner's own validation ("reach flows uphill") on composed relief; two are elevated systems ending at an undeclared point; A passes vacuously on four seeds because no channel starts inside its narrow highland | tool failure; construction |
| R7 | 0 / 6 | one or two river axes climbing 0.3–25.7 m (floors clamped to the envelope, overlapping valley floors) and one 2.06 m depression, all on B's larger highland | construction |
| R1 | 2 / 4 | only one summit ≥ 30 m in the highland on those layouts | construction (culmination envelope) |
| R2, R6, R8 | 0–1 each | one B seed (777) with an unverified plateau; one A seed (1004) with too few rocky cells | layout |

**What held on every lattice:** a capital plus four further settlement sites, at least one coastal, in at least
three regions (R4 20 / 20 — the rule stops at five sites); and **coastal settlement sites on all twenty**,
against none on any surface measured in G2 (brief §4.2). Low, rocky and cliff shores consistent with their
hinterland on 19 of 20. Diagnostics: largest basin 0.23–0.42 km² on A, 0.47–0.94 km² on B (G3b ≤ 0.21);
Strahler 2–3 on both; land below 5° 36–41 %; land above 45° 11–14 % on A, 4–7 % on B; highest point
253–297 m on A, 296–308 m on B. Plateau-foot fill 2–69 717 m³ per lattice.

### 14.8 Visual reading — agent reading, owner review pending

From `g3c_contact_sheet_V2_AB.png` and the per-seed views:

- **A:** on every seed the same crescent-shaped, whale-backed ridge follows the island's medial axis — the
  fixed contour and medial placement leave little variety between seeds; its faces are planar.
- **B:** a central peak with radiating ridges, closer to the "dominant mountain" of the target; faces still
  planar.
- **Both:** plateaus read as drums with straight ramps despite D13; lowlands show the eikonal construction's
  planar facets; a cliff band runs along much of the coast; no lakes. Heights only — but even as shape,
  neither approaches the composition of the concept image.

### 14.9 What this means — evidence, not decisions

- **Footprint (GEO-1):** on this construction contour A carries the settlement requirement, plateaus and a
  varied coast; B gives a stronger mountain silhouette, larger basins and more rivers rising in the
  mountains, but worse hygiene. The footprint was not shown to be the binding limit; the rule forbids
  growing the world on this evidence, and shape differs as well as area.
- **Placement (GEO-3):** a region-first skeleton with valleys and ridges as duals is pit-free and makes the
  coast and the lowlands work; the distance-from-drainage surface it uses gives planar facets and whale-back
  ridges, so a production mechanism would need a more expressive landform model — a research finding.
- **Coast (GEO-4):** a per-segment profile behind a fixed shoreline removed the coastal-ramp limit of G2 on
  all twenty lattices.
- **Criteria (GEO-7):** R3 counts merged patches once and R5 can pass vacuously or fail on the planner's own
  validation — weaknesses for the owner's threshold decision; nothing was redefined.
- **Plateaus (GEO-8):** both escarpment classes were built and verified; the 60–120 m class falls between T2
  and T1 by the tier rule and reads as the stronger landform. Evidence only.

### 14.10 Verification

| Check | Result |
|---|---|
| determinism | the evaluation repeated in a fresh process: 62 of 62 files (20 lattices, 20 region maps, 20 structure images, report, summary) byte-identical; screen and calibration hashes also repeated across processes |
| contours | contour A's Stage 1 field hash equals the pinned `7599879a71250a06` in every run; contour B's hashes as frozen |
| production | MD5 of 64 files — the tile assets and `Settings/Terrain/`, the production scene and its folder, URP assets, every `ProjectSettings/*.asset`, `packages-lock.json`, the G0, G1 and G3 sources — identical after 21 Unity processes |
| `H4A` | not rebuilt: G3c does not read it; the pinned Stage 1 hash is checked instead |
| not run | the EditMode suite (no tested code changed); the owner's visual review |

Outputs (session scratchpad `…/scratchpad/g3c/`): `contours/`, `screen/`, `freeze/g3c_freeze_record.txt`,
`eval/` (report, summary, `table.md`, lattices, region maps, structure images, review views,
`g3c_contact_sheet_V2_AB.png`, `lowland_ab.txt`), `eval_repeat/`, and the calibration runs `try1`–`try11`.

### 14.11 Recommended next step

**The owner's review of G3c** — the views and §§14.5–14.8 — before any further experiment. The decisions it
informs: whether the distance-from-drainage construction is worth another iteration or a more expressive
landform model is needed (a construction change voids this evaluation; a rerun uses seeds 1005–1014); whether
R3 and R5 serve GEO-7; and whether coastline robustness (G3d) is worth running before the landform question
is answered.

---

## 15. Post-G3c review (2026-10-21)

**Mandate.** The owner asked to treat G3c as a completed failed experiment, to settle GEO-1, GEO-2, GEO-3,
GEO-4, GEO-5, GEO-7 and GEO-8 where the evidence and the owner's explicit directions allow, and to plan
research into alternative landform models — documentation only. The record is
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md); this section states what it changes here.

- **§14 is unchanged.** G3c's readings, rule, tables, views and conclusions stand as evaluated; proposed
  corrections to R1–R8 live in the review (§3), never here. G3c is **completed research that failed
  acceptance and produced useful evidence** — not a proof that region-first geography is invalid, and not
  evidence that land area limits anything (review §1.2).
- **Lessons** (review §1.3): distance-field surfaces give planar facets and whale-back crests; overlay
  plateaus read as drums; envelope culminations give one summit or a cone; class-switched coasts give an
  over-continuous cliff band; composed surfaces can fail the production hydrology planner; readings must be
  non-vacuous; a construction that needs many fixes for layer interactions has the wrong representation.
- **Decisions** (review §2; 99 Open Questions decision records): GEO-1 **resolved** — no resizing on this
  evidence, today's footprint is the research baseline; GEO-2 **resolved** — one branching system is one T1;
  GEO-3 research direction (c), production placement at G4; GEO-4 target direction resolved, contract open;
  GEO-5 policy (b) with declared basins; GEO-7 classification resolved, the next reading set proposed;
  GEO-8 principle resolved, the tier contract proposed; **GEO-9** added for settlement and POI placement.
- **Next research** (review §§4–5, a proposal): four landform-model families compared; recommended, two
  surface syntheses — structure fields with a fluvial steady state, and a generalised skeleton strip
  partition — on one shared typed-region layout, contour A only, calibration 110201 and 1337, held-out
  1005–1014, under the provisional label *LMC*. It needs the owner's approval at every gate; G3d waits until
  a model is promising.

**Documents changed with the review:** the review itself (new); this record (status, §9, §15);
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (status, D7, D8 heading, §9 notes);
[99 Open Questions](../../../99%20Open%20Questions.md) (decision records, GEO-9, § Resolved Decisions);
[90 Glossary](../../../90%20Glossary.md) (Large Plateau, Declared Basin, Geographic Hierarchy);
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) (§1 T1 and plateau notes, §3 rule 1, §6 note);
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md);
[AGENTS.md](../../../AGENTS.md). No code, scene, asset, `TerrainData` or research output changed; no Unity
process ran.

**Owner decision and step 0 (2026-10-21).** The owner approved the recommended pair — **S-LM3 + S-LM1 on one
shared LM2 region layout** — and asked for the comparison's design note without runs. It is
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md). The owner revised the agent's draft the same
day; **the owner's text governs** (v0.2; v0.3 extends R5, on the owner's decision, to every river system
rising in the mountains or on a plateau ending at a designated outlet): contour A only; calibration 110201 and 1337, held-out 1005–1014,
reserve 1015–1024; one LM2 layout per seed for both syntheses; hard gates R4, R5, R7, R8, R9, R10, indirect
indicators R1, R2, R6, diagnostics R3, R11; Q1–Q9 as defined in the post-G3c review §3.3; a check that the
metrics harness reproduces G3c on its calibration grids; a blinded X/Y review at three scales; the passage rule
(all hard gates and the owner's composition acceptance, each on ≥ 7 of 10 held-out seeds); seven explicit owner
decisions (its §12) before any code. The agent's v0.1, with its construction formulas and candidate metric
definitions, is kept as [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md) — source material
for the metrics specification and the S-LM3 model contract, not approved.

**Technical specification, checked on historical data (2026-10-22).** The owner supports the note as the research
direction and has not authorised code or calibration. [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md)
states the elevation definitions (world-height datum), the S-LM3 model contract, the S-LM1 ownership rule, the gate and
Q1–Q9 calculations and their review-flag levels. It was hashed before any computation and checked with read-only
Python on G3c's twenty evaluation lattices (§14) and analytic surfaces — no generation: 10 of 13 pre-registered checks
held (among them: B's main summit more radial than A's on 10 of 10 seeds; G3c's plateaus are drums on 44 of 44; the
coast classes reproduce G3c's record on 20 of 20); three expectations failed and led to corrections — the plateau
edge height overstated low escarpments (K1), the route detour over triangle centroids depends on direction (K2), and
one uniform flag level would misfire (K3). ~~It awaits the owner's approval;~~ **The owner approved it the same day**
(its §12 items 1–4); Step 1 is a separate decision and is not authorised.

**Design note approved; Step 1 readiness (2026-10-22).** The owner approved the design note (v0.5) as the research
plan, kept Step 1 unauthorised — a separate stage with its own explicit authorisation, and until then no generation,
Unity run or production change — and kept LMC as the working name, the official ID to be chosen by the repository's
rules before implementation. [GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) reconciles plan,
specification and metrics (F1 R8 scope; F2 no verified whale-back detector; F3 no slope-cap diagnostic; F4 no
reference values on G3c's calibration grids), lists what remains unverified, and sets the conditions C1–C8 for the
authorisation — among them the official ID (the G3 line's next free letter is G3e; G3d is reserved).
On 2026-10-23, at the owner's request, C2–C5 were prepared with read-only checks on existing G3c data
([GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) §13): proposed amendments A1 (R8 over every declared
plateau top), A2 (a whale-back measure on the measured crest — it failed its pre-registered check, 6 of 10 against 7)
and A3 (slope clustering at configured angles and slope bands per region — after a flank-rule correction it finds
G3c's upland slopes piled at 5.7° on 20 of 20 lattices); the calibration-grid reference for Step 1 is recorded with
4 of 4 continuity checks held. ~~None of A1–A3 is in force before the owner approves it.~~ The owner approved A1 and A3
the same day and chose option (b) for the whale-back — judged in the visual review, Q3, Q4 and Q4m unflagged; the
specification is v0.4. Step 1 is still not authorised ~~(open: C1, C8, C9)~~. Later the same day the owner decided
C1 — **the experiment's official ID is G3e** (the next free letter in the G3 line; working name LMC) — C8 (no commit of
its code during Step 1; the commit returns to the owner before the freeze) and C9 (the reference data copied,
MD5-verified, to `D:\Hobby\game-dev\research-data\lmc-reference\`). Only the Step 1 authorisation remains.

---

## 16. G3e (LMC) Step 1 — outcome (2026-10-23 … 24)

Step 1 was authorised by the owner on 2026-10-23 and ran eleven times on the calibration seeds 110201 and 1337; the
record is [GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) and the outcome
[GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md).

- **Not ready**: neither synthesis passes every hard gate on either calibration seed; no Step 2, no freeze, no held-out
  evaluation, and the held-out seeds were never generated. This is not a held-out failure and says nothing about the
  island's size (contour A fixed) or against GEO-3's option (c).
- **S-LM3** (structure fields with a fluvial steady state) was closed with a negative result: its receiver iteration never
  converged.
- **S-LM1** (skeleton strip partition) improved under four authorised fixes — a constant-width ramp strip, protection of
  declared drainage from plateaus, a harmonic toe, and the measurement rule M-1 — and on 1337 fails only R7. Its remaining
  failures come from where the shared layout puts plateaus (across drainage lines; one ramp toward a sea cliff) and from
  where S-LM1 puts its valleys relative to the base.

**Post-mortem checks G3e-PM (2026-10-24) — plan approved by the owner, not run.** A single pass on the preserved run-11
data and one views-only Unity step, pre-registered in
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md): the owner's first look at S-LM1 at three scales
(no S-LM1 image existed), then a ledger of the 30 hard-gate failure items of run 11 with their owners, the feasibility of
the LM2 plateau-siting amendment on contour A, plateau 1's access on 110201 and the routing of 110201's elevated systems.
Its hypothesis is that every item has an owner within a bounded set (LM2 plateau siting, S-LM1's plateau stage, valley
stations, axis declaration); its pre-registered decision table ends in continue with an exact package, contract decisions
first, or stop S-LM1. It changes no code and no definition; running it needs the owner's separate authorisation.
*(Authorised and run the same day — next paragraph.)*

**G3e-PM outcome (2026-10-24) — the S-LM1 line stopped.** Results in §13 of
[GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md); records outside the repository at
`D:\Hobby\game-dev\research-data\g3e-pm\`. In the owner's blind look S-LM1 met no D6 point and failed point 1 (a dominant
branching mountain system) on both seeds; the other panel was the closed S-LM3 configuration, whose ratings belong to a
separate line. Three results are kept apart:
- **registered D1: INCONCLUSIVE** — a validity check of the S8 replica failed (the replica lacked `DepressionFill`'s 1 mm
  fill epsilon), so D1 stopped;
- **H: FAIL by §7**, accepted by the owner — on 110201 all four elevated systems already reach undesignated shores on
  S-LM1's base surface (D4), and FAIL takes precedence;
- **DEV-1, a one-run deviation** with the epsilon — it completed the ledger: 15 of 30 items covered; outside owners S-LM1's
  base, its flank coordinate u and its valley cross-profile.

The owner kept those three contracts, so by the pre-registered decision table **the current S-LM1 line stops** — a
research-line decision, not a claim that the mechanisms can never be revised. Also recorded: a base on which the coastal
surface decides heights beyond the coastal band width is treated as a potential deviation from the specification's §6.
Nothing here concerns island size or GEO-3's option (c). Next, only with its own plan: revisit the landform families
(post-G3c review §4) or a changed S-LM3 law; G3d stays deferred.

## Related documents

- [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) — the production terrain contract
- [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) — the quality principles this target extends
- [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) — Stage 3F research and proposed contracts
- [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md) — the production relief layer
- [NATIVE_ISLAND_SHAPE_PREFLIGHT.md](NATIVE_ISLAND_SHAPE_PREFLIGHT.md) — the first diagnosis of the island's shape
- [GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) — the G2 decision brief: approved target D6, G3c scope D8
- [GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) — the post-G3c decision record (GEO-1 … GEO-9) and the landform-model research plan
- [GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) — step 0 of the landform-model comparison (S-LM3 + S-LM1 on a shared LM2 layout), v0.2 the owner's revision
- [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md) — the agent's v0.1 of that note, kept as unapproved technical source material
- [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) — the LMC technical specification, checked on historical G3c data
- [GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) — the readiness check and conditions for authorising LMC's Step 1
- [GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) — the run-by-run record and decisions of G3e Step 1
- [GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md) — G3e Step 1's not-ready report
- [GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md) — G3e-PM, the post-mortem checks: pre-registration (plan approved, not run)
- [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Geography-first world shape (GEO-1 … GEO-8)
