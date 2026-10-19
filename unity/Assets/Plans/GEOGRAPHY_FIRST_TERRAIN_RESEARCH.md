# Geography-First Terrain Research — Expressive Fantasy Geography

Version: 0.1

Status: Draft — research record. Nothing in it is an approved decision. The target geography (§4),
the stage responsibility map (§5), the acceptance suite (§7), the Geometry-First Review mode (§3) and
the experiment roadmap (§9) are **proposals awaiting owner review**.

Last Updated: 2026-10-18

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
   heights-only Geometry-First Review tool (G1). Neither touches production (§9).

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
| land in 24–50 / 50–70 / 70–110 / 110–230 / 230+ m | 39.7 / 25.3 / 25.3 / 5.9 / 3.8 % (the last derived as the remainder) |
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
   0.001 on land (`HYDROLOGY_GENERATION_PLAN.md` §17; `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4). **Not
   re-measured on `H4A`**; G0 (§9) measures it.

### 1.4 Limitations by kind

| Kind | Limitation | Evidence | Owner |
|---|---|---|---|
| **Generation model** | coast-distance datum; one unbranched massif; floor relief bounded to ≤ 18 m steps; no positive ridge network; subtractive Stage 3 keyed to the massif frame; no T2/T3 tiers (4B not started); uniform coast (4C not started) | §1.2–1.3 | Stage 1, 2A/2B, 3A–3C, 4B/4C |
| **Scale** | 2.61–3.16 km² of land, inscribed radius 410–781 m **[PRIOR]**; the coastal band `d < 200 m` holds 54–59 % of land **[PRIOR, prototype]** and every relief term is gated toward zero inside it; relief uses ~365 m of the 700 m budget | `LANDMASS_T1_FEASIBILITY_AUDIT.md` §3; Stage 4A design §2 | owner (99 Open Questions GEO-1, GEO-4) |
| **Sampling resolution** | not limiting for macro/meso forms: a 100 m-wide ridge is 37 lattice steps; limiting for channels (< ~8 m cannot be terrain) and for vertical walls (a 20 m wall over 1–2 steps is 74–82°) | `PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md` § World scale; `HYDROLOGY_GENERATION_PLAN.md` §19 | — (a property, not a defect) |
| **Topology** | valleys exist only on massif flanks (B8: massif-free worlds have none); no inter-massif corridor concept (B2b); declared trajectories cross rising ground (36 route closures on `H4A`, seeds 1000–1039) | 3F preflight §§9, 18; handoff §9 | Stage 3B/3C (frozen) |
| **Hydrology** | two unreconciled carves: Stage 3F along declared trajectories, `HydrologyBake` along D8 reaches; no tributary structure on the dome; the documented policy is fill-then-spill with every system reaching the ocean | `HYDROLOGY_GENERATION_PLAN.md` §§9, 11; §6 here | Stage 3F × hydrology |
| **Bake** | none for shape (≤ 1.07 cm, exact seams). Cost: authority evaluation dominates | §2.1 | — |
| **Presentation** | the only review camera is a 2 800 m top-down view; dumps are top-down analytic images; production textures, vegetation (11 747 trees, 3.9 M detail instances on 110201) and the ocean plane hide or confound geometry; no standardised oblique or eye-level capture exists | `NATIVE_ISLAND_SHAPE_PREFLIGHT.md` §4 C/M9; §2 here | tooling (G1) |

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
| B. **Isolated review tool** | an Editor-only tool that builds tiles in memory with `OverworldTerrainGrid.Create(…, layers, neutralMaterial)`, writes heights with `OverworldTerrainBuilder.WriteHeights`, never calls `PaintSurface`, `PlaceTrees`, `PlaceGrass` or the ocean step, renders the camera set to PNG outside `Assets` and discards the scene | **recommended** |
| C. Toggle the production scene | `drawTreesAndFoliage = false`, hide `WorldWater`, swap the material, disable the volume in `Overworld_Generator.unity` | **rejected** — edits a versioned scene that already churns (tech debt item 7) and risks shipping a diagnostic state |
| D. A "diagnostic" flag in the bake | skip vegetation inside `OverworldTerrainBake` | **rejected** — a second, optional bake path; AGENTS.md forbids optional bake paths |

**[FACT]** Option B needs no production code change: `OverworldTerrainGrid.Create` already takes the
material and layer array, `WriteHeights` and `OverworldTerrainField.FromSamples` are public, and the
Stage 3F phase 7 research wrote heights onto tiles exactly this way. It adds new Editor-only files (a
tool, a neutral material and optionally a diagnostic shader) — that is implementation and waits for
approval (G1, §9).

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
be confirmed or replaced in G0; none is measured.

### 7.1 Macro-relief

| Id | Metric (definition) | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| M1 | hypsometry: land share in 24–50 / 50–70 / 70–110 / 110–230 / 230+ m, and p50 / p90 | 110201 `H4A`: 39.7 / 25.3 / 25.3 / 5.9 / 3.8 %, p50 58 m, p90 108 m **[MEASURED]**; 1337: 41.5 / 32.0 / 26.5 / 0 / 0 % | land above 110 m well beyond today's ~10 % so that a highland region exists; calibrate on G3 candidates and reference DEMs | needed (GEO-7) |
| M2 | relief regions: connected components of land whose height exceeds a 600 m-window median by ≥ the plate step (18 m), area ≥ 0.1 km² | not measured — G0 | ≥ 2 on massif worlds, ≥ 1 elsewhere | needed |
| M3 | **single-dome detector**: (a) coast-aligned gradient share of the interior floor; (b) R² of floor height against coast distance; (c) summits with topographic prominence ≥ 30 m; (d) share of land above p90 in its largest connected component | (a) 73.6 % / 66.8 % **[MEASURED]** (110201 / 1337); (b)–(d) not measured — G0 | flag the failure mode when (a) > 65 %, (b) high, (c) ≤ 1 and (d) ≈ 100 % together; the 65 % is Stage 4A's existing target, the rest calibrate in G0 | needed |
| M4 | ridge network: ridge lines from divide extraction (D8 basin boundaries on the filled surface); total length, longest connected ridge, branch count, crest-height coefficient of variation | not measured — G0 | branching present on massif worlds; non-uniform crests (`WORLD_RELIEF_PRINCIPLES.md` §3 bans uniform crests) | needed |
| M5 | saddles and passes: saddle points of the merge tree with prominence ≥ 15 m; passes = saddles on the main divide joining two valley heads | not measured — G0 | ≥ 1 pass on each major divide (rule 3) | needed |
| M6 | silhouette and coast: crenulation; share of shoreline whose land rises ≥ 15 m within 60 m (cliff coast); headland and cove counts | crenulation 1.35–1.46 **[PRIOR]**; cliff share expected ~0 under the coast gate — G0 | cliff share > 0 only if GEO-4 allows it | needed |

### 7.2 Drainage and basins

| Id | Metric | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| D-a | watershed consistency: share of declared divide length within 2 lattice steps of a D8 basin boundary | not measured | high; the exact share calibrates in G0 | needed |
| D-b | tributary structure: Strahler orders, bifurcation ratio, Hack exponent of trunk length against basin area | `H2` 110201: 8 systems, 0 tributaries **[PRIOR]**; `H4A` not measured | order ≥ 3 on the largest system; literature references for calibration only — bifurcation ratio 3–5 in many real networks ([Strahler number](https://en.wikipedia.org/wiki/Strahler_number)), Hack exponent slightly below 0.6 ([Hack's law](https://en.wikipedia.org/wiki/Hack%27s_law)) | needed |
| D-c | downstream continuity under the stated surface convention (priority flood ε 1 mm, 8-connected, on the quantised lattice; checked again on the triangle reading) | existing `HydrologyValidator` rules | 0 uphill reaches, 0 cycles (existing) | existing |
| D-d | closed depressions: S8 (≥ 0.25 m and ≥ 100 m²) + independent IDS, outside permitted basins | `H4A` 0 on canonical, 6/40 sweep inside `Z_M ∪ L_M` **[PRIOR]** | 0 new outside permitted basins (requirement D1 as researched) | 3F-2 and the basin policies |
| D-e | river/valley alignment: share of reach length inside declared valleys; channel incision depth relative to the valley floor | not measured | high alignment; reported until GEO-3 | needed |

### 7.3 Geometry and traversal

| Id | Metric | Baseline | Proposed initial target | Owner threshold |
|---|---|---|---|---|
| T-a | slope bands of all land: < 5°, 5–15°, 15–30°, 30–45°, > 45° | 110201 `H4A`: < 5° 37.2 %, > 45° 5.7 % **[MEASURED]** | existing finished-island targets: ≥ 35 % below 5°, 6–15 % above 45° (`WORLD_RELIEF_PRINCIPLES.md` §6) | existing |
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

- **Against today:** G0 measures every "not measured" baseline on the canonical seeds and the sweep.
- **Against reference geography:** real digital elevation models of small mountainous islands, scaled to
  the island footprint, give hypsometry, ridge, saddle and drainage figures to compare with. Which DEMs to
  use is part of G2; none were used here.
- **Against the visual reference:** §4.1 readings, checked on V2–V5 captures; no metric is fitted to a
  screenshot.

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
| **G0** | Geography metrics baseline: an Editor-only dump computing §7 M1–M6, D-a–D-e, T-a on `H4A` (and on `H3F`, where it exists) for the canonical seeds and the 1000–1039 sweep, reusing the dump lattice path and the hydrology package read-only | none | metric definitions are themselves proposals; a weak definition can mislead | numbers recorded here for every "not measured" baseline; the single-dome detector flags production `H4A` on the seeds a human would flag; process repeatable (identical report in two processes); `git status` clean apart from the new Editor file |
| **G1** | Geometry-First Review tool (§3): heights-only in-memory tiles, neutral material, camera set, overlays, PNG + JSON outside `Assets` | none (parallel with G0) | render cost unmeasured; URP Terrain Lit with a single neutral layer may still need a diagnostic shader for slope colours | identical images and camera records for the same lattice in two processes; production scene hash and tile assets unchanged; V1–V6 produced for `H4A` and E (when E is available) |
| **G2** | Target adoption: owner review of §4 against G0 numbers and G1 images; choice of reference DEMs; answers to GEO-1, GEO-2, GEO-4, GEO-7 | G0, G1 | the target is reopened after evidence; that is the point | 99 Open Questions entries answered or explicitly deferred; §4 and §7 revised to v0.2 with approved and still-proposed parts separated |
| **G3** | Upstream relief candidates, dump-first and standalone (the 3F phases method): compare §5.2 options (a)–(c) as fields with their ridge **and** valley skeletons; no wiring | G0, G1, GEO-2 | design scope grows; the frozen Stage 3 topology may prove incompatible with any good candidate | one candidate passes the M-series and D-d on ≥ 5 of 7 canonical seeds including a no-massif seed, keeps the land mask, adds no undeclared depression, and passes owner review of V2–V5 |
| **G4** | Placement decision: a design document stating where the chosen mechanism lives, what unfreezes, the version bump and the re-audit cost | G3 | an unfreeze of Stage 2B or 3A–3C | owner approval (GEO-3); nothing implemented before it |
| **G5** | Stage 3F on the new relief: E-family acceptance (3F-2) re-run on the G3 relief | G3, 3F stage-1 answers | E may need changes where the new valleys differ | 0 new depressions by the independent oracle; arc-class report per 3F-4; review captures |
| **G6** | Basins, lakes and coast: basin policy, F2 against B4, Stage 4C coastal character, hydrology on the final surface | G4, G5, 3F-10/11/14, GEO-4, GEO-5 | inland water is outside V0 | each policy decided before its geometry is built |

**Smallest useful sequence:** G0 and G1 in parallel (read-only, no production change), then G2. G3 is the
first step that designs terrain; it starts only after GEO-2 is answered.

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

---

## Related documents

- [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) — the production terrain contract
- [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) — the quality principles this target extends
- [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) — Stage 3F research and proposed contracts
- [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md) — the production relief layer
- [NATIVE_ISLAND_SHAPE_PREFLIGHT.md](NATIVE_ISLAND_SHAPE_PREFLIGHT.md) — the first diagnosis of the island's shape
- [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Geography-first world shape (GEO-1 … GEO-7)
