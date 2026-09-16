# Native Island Shape — Preflight

**Status:** Preflight. Read-only analysis. **No production code, scene, setting or generated asset was
changed.**
**Date:** 2026-09-14
**Baseline:** HEAD `7c882c1` plus the uncommitted Unity-native terrain migration
([UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md](UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md)).
**Related:** [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md),
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md),
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md).

Claims are tagged as in the other TerrainGen documents: **[FACT]** read from code or measured here,
**[CONCLUSION]** follows from facts, **[PROPOSAL]** a recommendation that needs an owner decision,
**[OPEN]** deliberately unresolved.

**The question this document answers:** how do we get an expressive fantasy island — silhouette,
coast, lowlands, hills, massifs, ridges, valleys and structural basins — from the existing deterministic
TerrainGen → Unity `TerrainData` pipeline, without restoring the removed external terrain pipeline and
without breaking frozen Stage 3C/3D/3E?

**Short answer.** Today's production relief is only two terms: a height that is a pure function of
distance to the coast, plus one lens-shaped massif. Every other structure TerrainGen owns (ridges, spurs,
valleys, basins) is metadata that never reaches the heightmap. The missing layer is a **new, additive
TerrainGen stage — "Stage 4: island relief" — composed after the frozen `StructuralElevation`**,
evaluated as a pure function of world position, zero at and seaward of the shoreline, and published
through the one composition point the bake already reads (`TerrainAuthorityField`). Valleys and basins
become real terrain later, through a separately decided Stage 3F. The silhouette is a Stage 1 question
that needs its own owner decision.

---

## Method

| Evidence | How it was obtained |
|---|---|
| Production flow | Read `TerrainAuthorityField`, `StructuralElevation`, `Stage1LandmassGenerator`, `LandmassBuilder`, `CoastHeightModel`, `TerrainGenSettings`, `Stage2AMacroStructure`, `Stage2BMassifGenerator`, `MountainMassif`, `MassifUpliftModel`, `MassifSettings`, `OverworldTerrainField`, `OverworldTerrainBuilder`, `OverworldTerrainBake`, `OverworldTerrainMenu`, `OverworldPipeline` |
| Stage ownership | Usage map of every TerrainGen stage type across `Assets/Scripts` (runtime, editor, tests) |
| Frozen contracts | Stage 3C handoff, Stage 3F preflight, pinned hashes in the TerrainGen and bake tests |
| Structure images and stats | Existing dump tools, fresh batch processes, seeds 110201 and 424242: `TerrainGenDump.DumpStage1Batch`, `TerrainGenMassifDump.DumpMassifBatch`, `TerrainGenStage3CDump.DumpBatch`, `TerrainGenStructuralBasinDump.DumpBatch` |
| Native result | `OverworldTerrainValidation.RunBatch -terrainRaw true` for both seeds; raw height, alphamap and tree dumps analysed offline (hypsometry, slope bands, reachability, local relief, height against coast distance, hillshade, slope-class and 10 m contour images) |
| Saved scene | `OverworldBatchDiagnostics.BakeAndDump -overworldSkipBake true` (native `OverworldTerrainDiagnostics` on the saved scene, seed 110201) |

The diagnostic bakes overwrite the gitignored tile assets. They were backed up first and restored byte
for byte (MD5 identical). A fresh process then hashed the saved scene from disk and returned the
recorded signature `0bd540ab3980`. The scene file hash and `git status` are unchanged. All outputs live
outside the repository. Old screenshots and baselines from the removed pipeline were **not** used.

---

## 1. Current authoritative pipeline

**[FACT]** The code matches `OVERWORLD_TERRAIN_ARCHITECTURE.md`. The one production path:

```
seed
 │
 ├─ TerrainAuthorityField(seed, includeMassifs: true)                 World/Terrain/TerrainAuthorityField.cs
 │    ├─ Stage1LandmassGenerator.Generate(seed, TerrainGenSettings.Default)
 │    │     CrustalPartition → LandmassBuilder (grow cells to a land budget, islets, trace,
 │    │     simplify, midpoint-displace) → CoastQuery → CoastHeightModel → 1025² field (+ signed distance)
 │    ├─ Stage2AMacroStructure.Run(seed, …)  (partition + landmass rebuilt, MacroPlates, MacroAxisBuilder)
 │    ├─ MountainMassif.Build(…) for every accepted axis                ("terrain.massif", i)
 │    └─ MassifUpliftModel(massifs)          smooth max, coast gate zero at the shore, 360 m ceiling
 │
 │   HeightAt(x,z) = StructuralElevation.At = Stage1.HeightAt(x,z) + Uplift.UpliftAt(x,z, coastSD)
 │
 ├─ OverworldTerrainField.FromAuthority      one 1025×1025 lattice, 2.734375 m; per vertex:
 │                                            height, uplift, signed coast distance (point queries)
 ├─ HydrologyBake.ApplyPendingCarve(field)   only when a hydrology plan is pending (off by default)
 ├─ OverworldTerrainBuilder.Build
 │    heights  → SetHeightsDelayLOD + SyncHeightmap (513² windows of the shared lattice)
 │    derived  → slope (true degrees), convexity (20 m), D8 drainage (stride 2)
 │    surface  → TerrainSurfaceRules → SetAlphamaps (5 canonical layers)
 │    trees    → TerrainVegetationRules → SetTreeInstances (7 m world cells)
 │    details  → TerrainVegetationRules → SetDetailLayer
 ├─ tile assets refilled in place (GUID-stable), SaveAssets, render distances
 │
 └─ OverworldTerrainMenu.GenerateSurfaceRoutine → ocean plane
    OverworldPipeline.GenerateAll → (hydrology off) → markers → settlements → (roads off) → player
    OverworldBatchDiagnostics.GenerateAllAndSave → Assets/Scenes/Overworld_Generator.unity
```

**[FACT]** Nothing reads `TerrainData` back into TerrainGen. The hydrology carve is applied to the lattice
downstream of TerrainGen. The only TerrainGen types on the production path are Stage 1, Stage 2A, the
massif and its uplift model. All settings are code defaults (`TerrainGenSettings.Default`,
`MacroStructureSettings.Default`, `MassifSettings.Default`). No authoring asset or window exposes them.

**[FACT]** Measured bake cost (seed 110201 / 424242, fresh process): authority 23.2 / 23.8 s; Unity-side
work under 1 s (heights 34 ms, derived fields 158 ms, alphamaps 524–531 ms, trees 46–47 ms, details
227–235 ms); surface step 25.7 / 26.4 s.

## 2. Current island-shape ownership

| Aspect of the island | Owner today | Where |
|---|---|---|
| Silhouette, land fraction, islets | TerrainGen Stage 1 | `LandmassBuilder` over `CrustalPartition` (200 m cells), `CoastOutline` refinement |
| Coastal falloff, shelf, island floor | TerrainGen Stage 1 | `CoastHeightModel`: three profile curves over **signed coast distance only** |
| Mountain placement and body | TerrainGen Stage 2A / 2B | `MacroPlates` → `MacroAxisBuilder` (certified axes) → `MountainMassif` |
| Mountain prominence | TerrainGen Stage 2 settings | crest 200–330 m, half-width from the certified axis, crown fraction, plateau span |
| Ridges, spurs, valleys, divides | TerrainGen Stage 3A–3D | structural metadata only; **not in the height** |
| Structural basins | TerrainGen Stage 3E | declarative enclosures; no floor or depth by owner decision; **not in the height** |
| Lowlands, hills, plateaus, outcrops | **nobody** | T2/T3 are "a future TerrainGen stage" (`WORLD_RELIEF_PRINCIPLES` §7) |
| Coastal character (cliff / beach / estuary) | **nobody** | designed (`PROCEDURAL_TERRAIN_GENERATOR_DESIGN` §Landmass 6), not built; Stage 1 is deliberately uniform along the shore |
| Surface weights, trees, ground cover | native bake | `TerrainSurfaceRules`, `TerrainVegetationRules` — classification only, no relief |
| Rendering | Unity | URP TerrainLit, `TerrainLayer` assets, render distances |

**[CONCLUSION]** The island shape is fully owned by TerrainGen, which is the right owner. But only
Stages 1 and 2 contribute height, and neither was ever meant to produce the island interior.

## 3. Stage-by-stage responsibility

| Stage | Produces | In authoritative height? | Frozen / pinned? | Safe to change? |
|---|---|---|---|---|
| **Stage 1** landmass | crust cells, land set, coast rings, `CoastHeightModel` field, signed distance | **yes** (`stage1.HeightAt`) | pinned by golden field hashes for 5 seeds (`TerrainGenStage2ATests.Stage1HeightField_MatchesItsRecordedHashes`); the 110201 hash is re-asserted in Stage 3A/3B/valley tests | only as a deliberate contract change: `GeneratorVersion` bump, new hashes, Stage 2A–3E re-validation |
| **Stage 2** belts (`Stage2MountainGenerator`, `UpliftBeltBuilder`) | superseded belt model | no | historical; dump and tests only; `UpliftBeltBuilder` helpers feed `MacroAxisBuilder` | leave |
| **Stage 2A** macro structure | macro plates, candidate and accepted axes, certified core | indirectly (axis = massif spine) | hardened contract (`STAGE_2A_*` docs); Stage 3 is built on it | only with its own justification |
| **Stage 2B** massif | `MountainMassif` + `MassifUpliftModel` | **yes** (`UpliftAt`) | Stage 3A–3E read its geometry; Stage 3E reads `StructuralElevation` | only with its own justification — a change moves every Stage 3 input |
| `StructuralElevation` | `H1 + U2` at a point | **yes** — it *is* production height | **frozen** (named in the migration freeze) | **no** |
| **Stage 3A** ownership / flanks / divides | frame | no | frozen (Stage 3C handoff freezes Stage 1–3C as a body of work) | no |
| **Stage 3B** primary spurs, corridors | skeleton | no | frozen | no |
| **Stage 3C** valley rank, geometry, topology, trajectories; `CarveField`, `Stage3Field` | trajectories; `H3 = H2 − C` in a dump only | **no** — `Stage3Field` is built only by `TerrainGenStage3CDump`, through the legacy 4-argument trajectory path | **frozen** | **no** |
| **Stage 3D** secondary spurs | ribs | no | **frozen** | **no** |
| **Stage 3E** cirque candidates, structural basins | enclosures + outlets | no | **frozen**; floor and depth deferred by owner (B4) | **no** |
| Stage 3F (not built) | first carved TerrainGen field | — | blocked: owner decisions 15.2–15.4 of the 3F preflight | future |

**[FACT]** Measured structure, seeds 110201 / 424242:

| | 110201 | 424242 |
|---|---|---|
| crust sites in world / eligible / land cells | 152 / 63 / 48 | 159 / 64 / 45 |
| land fraction (target) | 0.360 (0.359) | 0.359 (0.335; enclosed sea filled) |
| coast rings, length, crenulation | 1, 9.56 km, 1.35 | 2 (1 islet), 11.20 km, 1.46 |
| Stage 1 field height | 2.0 – 70.8 m | 2.0 – 73.1 m |
| massifs, length, crest uplift, culminations | 1, 999 m, 284 m, 1 | 1, 1 303 m, 293 m, 2 (saddle 0.47) |
| massif area / uplift footprint | 0.47 km² | 0.58 km² |
| Stage 3B spurs / valleys | 7 / 9 | 7 / 9 |
| Stage 3E declared basins (outlet elevation) | 5 (62–79 m) | 5 (61–81 m) |

**[FACT]** Three of the nine seeds in the Stage 3E audit (1337, 256, 777) certify **no massif**. That is a
legitimate geological refusal under the Stage 2A contract (`STAGE_2A_SELECTION_HARDENING.md` §5), and
those worlds are the Stage 1 island floor alone.

## 4. Current island geometry diagnosis

Measured on the native bake (raw heights from `TerrainData`, i.e. exactly what ships):

| Metric | 110201 | 424242 |
|---|---|---|
| land area / fraction | 2.82 km² / 0.359 | 2.81 km² / 0.358 |
| highest point | 349.8 m | 359.0 m |
| land height p5 / p25 / p50 / p75 / p90 / p99 | 25 / 36 / 50 / 61 / 87 / 314 m | 25 / 34 / 47 / 60 / 102 / 300 m |
| land in 24–30 / 30–50 / 50–70 m | 17.3 / 34.4 / 36.0 % | 20.1 / 36.4 / 27.8 % |
| land in 70–85 / 85–110 m | **2.0 / 2.6 %** | **3.1 / 3.6 %** |
| land in 110–230 / 230+ m | 4.3 / 3.3 % | 5.5 / 3.4 % |
| massif footprint (height > 81 m) | 10.7 % of land | 13.3 % of land |
| island floor slope 0–5 / 5–15 / 15–30 / 30–45 / >45° | 52.8 / 41.9 / 4.2 / 1.2 / 0.0 % | 45.3 / 49.6 / 4.2 / 0.7 / 0.2 % |
| massif slope >45° (median slope) | 56.2 % (47.6°) | 52.5 % (46.2°) |
| all land <5° / >45° | 47.2 % / 6.1 % | 39.3 % / 7.2 % |
| reachable from the shore at ≤45° | **92.3 %** | **91.5 %** |
| island floor local relief (height std, 112 m window), median / p90 | **3.4 / 5.0 m** | **3.8 / 5.3 m** |
| island floor height by coast distance | 0–50 m: 26.2 m; 200–250 m: 51.6 m; 600–650 m: 69.5 m | 0–50 m: 26.2 m; 200–250 m: 52.5 m; 550–600 m: 75.1 m |
| deepest point from the coast | 637 m | 671 m |
| land surface weights grass / sand / soil / cliff / rock | 0.776 / 0.137 / **0.001** / 0.029 / 0.057 | 0.742 / 0.158 / **0.001** / 0.041 / 0.057 |
| trees per km² of land | 4 096 | 4 353 |
| hydrology on this relief (110201, migration run) | 8 short coastal rivers, 0 tributaries, 0 lakes, 2.05 km | — |

Native `OverworldTerrainDiagnostics` on the saved scene (110201) agrees. It reports curvature ratio
0.94–1.06 on every tile with no grid period (no banding) and 0 % risers on the two floor-only tiles. The
massif tiles carry 9.5 % / 7.7 % risers, and one of them is 91.8 % reachable.

### Why the result looks the way it does

1. **The island interior is an extruded coastline.** **[FACT]** `Stage1Result.HeightAt(x,z) =
   CoastHeightModel.HeightAt(signedDistance(x,z))`. A shelf curve runs offshore; onshore a 26 m rise over
   200 m is followed by a saturating 30 m rise over 600 m. Every height contour is therefore an inset copy
   of the coastline: the 10 m contour images show nested coast-parallel rings. The gradient points at the
   nearest coast segment everywhere, and where the nearest segment switches the surface folds along the
   **medial axis** (the straight "roof creases" visible in the hillshade). The comment in
   `CoastHeightModel` says this plainly: the uniformity is a stage boundary, not the intended island.
2. **There are no lowlands and no hills.** **[CONCLUSION]** A monotone function of coast distance has no
   interior minima or maxima. 88 % (110201) and 84 % (424242) of dry land sits between 24 and 70 m.
   Local relief is 3–4 m, below the T3 tier. The 70–110 m band is nearly empty (4.6 % / 6.7 %), so the
   hypsometry is **bimodal**: floor, a gap, then the massif.
3. **The massif reads as a mesa standing on a plain.** **[FACT]** Stage 2B's cross-section holds full
   crest height across a crown (`CrownFraction` 0.16, `CrestPlateauFraction` 0.88), then falls ~284 m
   within roughly 160–200 m on each side (110201 cross-section: 0 → 284 m between 250 and 400 m off the
   axis). The slope map shows a **continuous ring wall above 45°** around a flat crown. That is exactly the
   tell `WORLD_RELIEF_PRINCIPLES` §2 forbids ("the wall does not run all the way round"). It is the whole
   reason reachability sits at 91–92 % against the 95 % criterion. The massif is also a single isolated
   lens: its uplift footprint is 0.47 / 0.58 km² (17–21 % of the land), and the ground it raises above
   the floor's highest level covers 11–13 %. Nothing connects it to the rest of the island: no foothills, no
   spurs in the height, no valleys. On three of nine audited seeds it does not exist at all.
4. **Ridges, valleys and basins exist only as data.** **[FACT]** Stage 3A–3E build 7 spurs, 9 valleys
   and 5 basins per seed, but nothing on the production path reads them. The only carved field
   (`Stage3Field`, dump-only) is built from the pre-Slice-C trajectory path. Its measured surface — 33 % of
   the land carved, curvature ratio 0.55 / 0.51 against Stage 2's 0.99 / 0.86, parallel straight
   troughs — is **not** a production candidate. The 3F preflight reached the same verdict for different
   reasons.
5. **The coast is the same everywhere.** **[FACT]** One onshore profile on every shore: no cliff coast
   where the massif approaches the sea, no cove, no estuary. Sand covers 14–16 % of the land because the
   27.0–28.4 m sand band always intersects the same gentle ramp.
6. **The silhouette is a random union of 200 m cells.** **[FACT]** About 45–48 crustal cells are grown
   from a random core along random per-edge weights to a 32–40 % land budget. The union is
   Douglas-Peucker simplified (18 m) and midpoint-displaced (roughness 0.15, 10 m segments). The result
   is irregular and never star-shaped (crenulation 1.35–1.46), but it has **no macro-scale intent**: bays
   and peninsulas are accidents of cell growth. The outline keeps a blocky, right-angled rhythm at cell
   scale (e.g. the square southern notch on 110201). Land fraction is almost constant (0.36) across
   seeds.
7. **Soil and river networks have nothing to follow.** **[CONCLUSION]** On a coast-distance roof every
   flow line runs straight down the fall line and never converges. The D8 drainage rule therefore almost
   never reaches its 20 000 m² threshold (soil weight 0.001), and hydrology finds only short parallel
   coastal rivers with no tributaries.

### Separation requested by the brief

**A) Correct structural logic (keep):**
- Crust-cell landmass: non-radial, deterministic, coast kept off the world border by construction.
- Signed-distance coastal profile: exact coastline at sea level, monotone, no pits; a correct *base*
  (T0 datum), just not a whole island.
- Certified macro axes and the massif's longitudinal profile: a range, not a dome or a bead chain;
  bounded width; coast gate zero at the shore.
- Smooth-max combination and a hard uplift ceiling.
- Stage 3 topology: fixed rank, non-crossing spurs, physical adjacency, one-way trajectories,
  declarative basins.
- The native bake: one lattice, exact seams, world-unit rules, deterministic per-cell hashing.

**B) Authoring parameters (tunable, but each move is a contract change where hashed):**
- Stage 1: `LandFractionTargetMin/Max`, `CrustSiteSpacingMeters`, `MaxIslets`, `SimplifyToleranceMeters`,
  `CoastRoughness`, `CoastalTransitionMeters`, `CoastalRiseMeters`, `InteriorScaleMeters`,
  `InteriorRiseMeters`, `ShelfWidthMeters`.
- Stage 2: `CrestHeightMin/MaxMeters`, `RangeHalfWidthMin/MaxMeters`, `MaxUpliftMeters`;
  `MassifSettings` crown, plateau, taper, asymmetry, culminations.
- Bake: `TerrainSurfaceRules`, `TerrainVegetationRules` (classification only).

**C) Missing from the architecture:**
- An island-scale relief layer: lowlands, uplands, hills, a massif foothill apron (T0 variation).
- T2 plateaus and T3 outcrops (0 of the 6–10 and 12–20 the principles ask for).
- Coastal character per shore segment.
- A realised valley / spur / basin surface (Stage 3F), blocked on owner decisions.
- Macro silhouette intent (island archetype: bays, peninsulas, headlands at 0.5–1 km scale).
- An authoring surface for TerrainGen settings (all code defaults today).
- A rendered, eye-level visual review capture. Today's only review camera is a top-down 2 800 m view,
  and batch diagnostics are analytic images.

## 5. What is already good

- **Architecture:** one authority, one composition point (`TerrainAuthorityField`), one lattice, no
  feedback, no second writer. A new relief term can land without any bake redesign.
- **Determinism:** 12 fresh processes identical in the migration, re-confirmed here (`c08574603378`,
  `a8d7baad5360`); Stage 1 golden hashes intact.
- **Seams and precision:** 0.0000 m seams; ≤1.07 cm from TerrainGen (16-bit rounding).
- **No banding:** isotropic curvature (0.94–1.06) and no grid period on any tile.
- **Coastline integrity:** ocean samples never moved by uplift (0 by assertion in the Stage 2B dump).
- **Downstream readiness:** surface rules already work in world units (true slope, drainage area, uplift,
  coast distance), so new relief is classified without new rules. A steep plateau wall becomes rock or
  cliff; a real valley floor becomes soil.
- **Stage 3 structure is rich and ready to be consumed read-only:** trajectories, spurs, ribs, basins with
  outlet positions.
- **Resolution budget is adequate** for the tiers in `WORLD_RELIEF_PRINCIPLES`: T3 outcrops span 15–33
  vertices, T2 plateaus 55–165, and a 20 m plateau wall over one or two vertices is 74–82°.

## 6. What is currently missing

| # | Missing | Consequence today | Owner stage |
|---|---|---|---|
| M1 | Island-floor relief (lowland basins, upland swells, hills) | extruded-roof interior, bimodal hypsometry, 3–4 m local relief, no drainage convergence | new Stage 4 |
| M2 | Massif foothills / apron / uneven wall | continuous >45° ring, 91–92 % reachable, mesa on a plain | Stage 4 (apron); breaking the wall itself is Stage 3F (spurs and valleys) or a Stage 2B contract change |
| M3 | T2 plateaus, T3 outcrops | no nodes, no occlusion, no landmark hierarchy below T1 | Stage 4 |
| M4 | Coastal character | uniform beach ramp all round | Stage 4 (additive onshore term, zero at the shore) |
| M5 | Realised valleys, spurs, basins | ridges and valleys invisible; basins undeclared as terrain | Stage 3F (owner decisions) |
| M6 | Macro silhouette intent | cell-growth amoeba, constant 36 % land | Stage 1 reopening (owner decision) |
| M7 | Islands without a massif | 3 of 9 audited seeds are a flat 70 m plateau | Stage 4 must still make a good island without T1; Stage 2A policy is separate |
| M8 | Authoring surface | every number is a C# default; no profile asset | settings layer for the new stage |
| M9 | Visual review capture | no eye-level rendering in validation | tooling |

## 7. Candidate approaches

| # | Approach | Verdict | Why |
|---|---|---|---|
| **C1** | Retune existing Stage 1/2 settings only | **Insufficient** | A monotone coast-distance profile plus one lens cannot produce lowlands, hills, plateaus or valleys. Every Stage 1 change moves 5 golden hashes and all Stage 3 inputs for no gain in kind. |
| **C2** | Put `Stage3Field` (Stage 3C carve) into production | **Rejected now** | `CarveField` is bound to the legacy trajectory path; re-binding edits frozen Stage 3C files (3F preflight §15.2). Its measured surface is anisotropic (0.55 / 0.51) and carves a third of the island into parallel troughs. |
| **C3** | Shape inside the bake (`OverworldTerrainField` noise, erosion, terracing, stamps) | **Rejected** | Creates a second height author downstream of TerrainGen. It is the removed architecture's post-bake repair in new clothes. |
| **C4** | Sculpt or stamp `TerrainData` / Unity terrain tools | **Rejected** | Not procedural, not deterministic, not seed-driven; violates "TerrainData is the representation". |
| **C5** | Global noise / FBM added to the field | **Rejected** | `PROCEDURAL_TERRAIN_GENERATOR_DESIGN` §Terrain Refinement allows only a bounded, structure-modulated refinement, *after* structure exists. Noise first is the failure mode this generator exists to avoid. |
| **C6** | Reopen Stage 1 for silhouette and coastal profiles | **Valid, later, owner decision** | The only way to change the outline. It is a deliberate contract change (version bump, re-pinned hashes, 29-seed Stage 2A–3E re-audit). It does not fix the interior by itself. |
| **C7** | Reopen Stage 2B (wider, lower, apron-shaped massifs) | **Valid, later, owner decision** | Fixes the mesa wall at its source, but moves every Stage 3 input; needs the same re-audit as C6. |
| **C8** | **New additive TerrainGen Stage 4 "island relief" composed after `StructuralElevation`** | **Recommended** | Touches no frozen or hashed file. Stage 1, 2B and 3 hashes and tests stay bit-identical; one composition point; point-evaluable; gated to zero at the shore, so the coastline, ocean and every Stage 3 input are unchanged. |
| **C9** | Stage 3F realisation (valleys, spurs, basins as terrain) | **Required eventually, not first** | The owner decisions in 3F preflight §15 (scoped unfreeze, basin depth rule) come first. Stage 4 must be designed so 3F composes with it. |

## 8. Recommended approach

**[PROPOSAL]** A staged programme, in this order:

1. **Stage 4A — island-floor relief (M1, M2 apron, M7).** A new TerrainGen stage producing a bounded
   additive relief term `R(x, z)`:
   - broad **lowland** basins and **upland** swells at 400–900 m wavelength, caused by structure rather
     than noise — for example tilt and warp of the existing crustal blocks or macro plates, whose
     geometry Stage 1 and 2A already publish;
   - rolling hills at 150–400 m as a secondary, structure-modulated term;
   - a **foothill apron** hugging the massif support that softens the toe and lifts the ground around it,
     so the massif stands *in* the island rather than *on* it.

   Hard properties: zero at and seaward of the shoreline; never lowers land to within a margin of sea
   level; no undeclared closed depression; never raises anything above the 700 m budget.
2. **Stage 4B — T2 plateaus and T3 outcrops (M3),** exactly per `WORLD_RELIEF_PRINCIPLES` §§1, 2, 5, 7:
   blue-noise placement, fixed height tables, three T2 and two T3 archetypes, its own ramp yaw per
   instance, keep-out from the massif footprint and, read-only, from Stage 3 corridors.
3. **Stage 4C — coastal character (M4):** a per-segment onshore modulation, zero at the shoreline. Cliff
   where uplands or the massif apron meet the sea, coves and beaches elsewhere, estuary slots reserved
   for Stage 3F / hydrology outlets.
4. **Owner decision D1 — silhouette (M6):** whether to reopen Stage 1 (C6) once Stage 4A shows the
   interior working.
5. **Owner decision D2 — Stage 3F (M5):** scoped unfreeze and basin depth rule. Its carve then composes
   on top of Stage 4.

**Why this answers the key question.** Lowlands, hills, plateaus, outcrops, foothills and coastal
character all come from one new deterministic TerrainGen stage. The coastline, the massif placement
and every frozen Stage 3 structure stay exactly as they are and are consumed read-only. Ridges, valleys
and basins become terrain through Stage 3F, the stage that already owns them — never through the bake,
never through a graph, never through an external pipeline.

**Why Stage 4 and not "a new Island Shape Generator".** The generator already exists. What is missing is
one relief stage inside it, which is what `WORLD_RELIEF_PRINCIPLES` §7 and the generator design's Stage
4/5 already describe (T2/T3, bounded refinement). The recommendation adds no parallel generator and no
second authority.

## 9. Exact proposed insertion point in the pipeline

```
Stage1LandmassGenerator        (unchanged, golden-hashed)          H1(x,z), coast, crust
Stage2A / MountainMassif       (unchanged)                         U2(x,z)
StructuralElevation.At         (FROZEN, unchanged)                 H2 = H1 + U2
Stage 3A–3E                    (FROZEN, unchanged; keep reading H2) spurs, corridors, trajectories, basins
─────────────────────────────────────────────────────────────────────────────────────────────────────
NEW  Stage 4 island relief     World/TerrainGen/IslandRelief*.cs  (new files only)
       inputs  : seed (own streams), Stage1Result (coast query, crust partition, land set),
                 massifs + MassifUpliftModel (read-only), optional Stage 3 structure (read-only keep-outs),
                 IslandReliefSettings (new, immutable)
       output  : R(x,z) metres, pure function of world position, plus a per-point component breakdown
                 (floor / apron / plateau / outcrop / coast) for diagnostics and surface rules
NEW  IslandElevation.At        = StructuralElevation.At(stage1, uplift, x, z) + relief.At(x, z)
─────────────────────────────────────────────────────────────────────────────────────────────────────
TerrainAuthorityField.HeightAt / Sample   → call IslandElevation instead of StructuralElevation
                                            (the one production line that changes, when 4A is accepted)
OverworldTerrainField → optional carve → OverworldTerrainBuilder → TerrainData      (unchanged)
```

- **No second source of truth.** `R` is a TerrainGen stage; the production height is still exactly one
  TerrainGen expression evaluated at one place. The bake never learns what `R` is made of.
- **Frozen stages untouched.** Stage 3 keeps reading `StructuralElevation` (H2), which is correct: its
  structures are defined on the structural surface, and none of them is in the production height yet.
- **Future Stage 3F** composes as `H = IslandElevation − C3F`. Its incision bound, today measured against
  the Stage 1 floor, must be re-stated against `H1 + R_floor`. That is a design item for 3F (§17 R4), not
  a reason to delay Stage 4.
- **Dump first.** Like every earlier stage, Stage 4A lands first as TerrainGen code plus a dump tool and
  tests, measured on the canonical seeds. Only then is `TerrainAuthorityField` switched, and
  `GeneratorVersion` bumped with the authority hash re-pinned.

## 10. Parameters that should become authoring controls

New, on an immutable `IslandReliefSettings` struct (later optionally loaded from an immutable
ScriptableObject profile, per the project's "ScriptableObjects = immutable data" rule):

| Group | Control | Starting intent (to be designed, not decided here) |
|---|---|---|
| Floor relief | lowland / upland amplitude | ±10–25 m around the T0 datum, bounded by the sea-level margin |
| | wavelength band | 400–900 m broad, 150–400 m hills |
| | lowland share, upland share | e.g. 20–35 % / 15–30 % of dry land |
| | shore fade distance | ≥ the 200 m coastal transition |
| Massif apron | apron width, apron lift, toe softening | relative to massif half-width and crest |
| T2 plateaus | count, height table, footprint, ramp share, wall steepness, apron | 6–10; `{20, 26, 34}` m ±15 %; 150–450 m |
| T3 outcrops | count, height table, footprint | 12–20; `{6, 9, 12}` m ±15 %; 40–90 m |
| Coast character | cliff share, cliff height, beach width, cove frequency | caused by what stands behind the shore |
| Budgets | T2 + T3 cover of dry land | 25–35 %, hard ceiling 45 % |

Existing authoring-grade values, which change only with an explicit contract decision because they are
hashed or feed frozen stages:

- Stage 1 land fraction band, crust spacing, islets, coast roughness, coastal and interior rise (D1).
- Stage 2 crest height band, half-width band, crown and plateau fractions (C7).
- Surface and vegetation rule defaults (bake-side), re-tuned only after the shape changes (§16 note).

## 11. Parameters that must remain structural / frozen

- `StructuralElevation` expression; Stage 3A–3E code, settings (`RidgeSettings` topology), rank, candidate
  geometry, convergence, trajectories, ribs, cirque candidates, basin validation.
- Stage 1 algorithms and defaults without a version bump (golden hashes for 110201, 424242, 20260905, 7,
  1337).
- Stage 2A certification contract (`MinUsableHalfWidthMeters`, coast clearance, reach, core length) and the
  "one certified axis or an explicit refusal" rule.
- The massif coast gate (uplift exactly 0 at and seaward of the shore) — and the same property for every
  Stage 4 term.
- World constants: sea level 24 m, height budget 700 m, tile 1 400 m, 2×2 tiles, heightmap 513, lattice
  1025 × 1025 at 2.734375 m (`TerrainGenSettings.FieldResolution` = the tile lattice).
- Stream names already in use (`terrain.crust*`, `terrain.coast.*`, `terrain.macro.*`, `terrain.range.*`,
  `terrain.massif.{i}`, `terrain.ridge.*`, `terrain.surface.*`, `terrain.vegetation.*`, `hydrology*`).

## 12. Determinism considerations

- **Streams:** Stage 4 draws only from new named forks (e.g. `terrain.relief.floor`,
  `terrain.relief.plateau.{i}`, `terrain.relief.outcrop.{i}`, `terrain.relief.coast.{segment}`) through
  `GameDev.World.Seed.WorldSeed` — fully qualified, because `GameDev.World.WorldSeed` shadows it inside
  `GameDev.World.*`. Adding a stream must not shift any existing stream.
- **No `UnityEngine.Random`**, no time, no hash-set or dictionary iteration order in output; features are
  sorted by stable ids (the generator design's D4 rule).
- **Pure point function:** `R(x, z)` depends on world position and immutable structure only, never on
  lattice order or neighbouring samples. The Stage 1 D2 test pattern (point query = bulk field) applies.
- **Placement decisions are made once, in structure,** from blue-noise sets with a total order. Evaluation
  sums bounded supports; any smooth-max must be symmetric and use the existing capped
  `MountainUpliftModel.SmoothMax`.
- **Versioning:** production switch = `GeneratorVersion` 1 → 2, a new pinned authority hash in
  `OverworldTerrainBakeTests`, and new world signatures for 110201 and 424242 recorded with the reason.
  Stage 1 golden hashes, Stage 2B field hashes (`1de91e469513054b`, `9d9d7b3c2560bc36`) and every Stage 3
  test must stay bit-identical — that is the proof nothing frozen moved.
- **Hydrology** stays downstream: it reads the baked terrain (pass A) and carves the lattice (pass B).
  Stage 4 must not read hydrology. The generator design's older ordering, with drainage and carve inside
  TerrainGen (its "Generation Order" ③–④), is **superseded by the current owner direction** and should be
  annotated when that document is next touched.

## 13. Tile / seam considerations

- Evaluate on the existing shared lattice through `TerrainAuthorityField`. Tile-edge vertices remain one
  sample; seams stay 0.0000 m by construction.
- No tile-local noise, no per-tile placement, no tile-relative coordinates. Plateaus and outcrops may
  straddle tile edges freely.
- Supports are world rectangles; bucketing, if needed for speed, is world-aligned and independent of tile
  layout.
- Alphamap texels keep the `i / (resolution − 1)` convention; nothing in Stage 4 knows the alphamap exists.
- Features narrower than 3 vertices (~8 m) are not representable and must not be authored (the T3 minimum
  of 40 m is 15 vertices).

## 14. Unity Terrain responsibilities

| Layer | Owns | Must not own |
|---|---|---|
| **A. TerrainGen** | silhouette (Stage 1), massifs (2A/2B), structure (3A–3E), **island relief (new Stage 4)**, future carve (3F); all island-shape authoring controls | anything tile-, texture- or Unity-specific |
| **B. TerrainAuthorityField** | the single composition call into TerrainGen, per-point components for surface rules | any reshaping, smoothing, clamping or re-derivation |
| **C. OverworldTerrainField** | lattice sampling, derived analysis (slope, convexity, drainage), the hydrology carve application | procedural geography of any kind |
| **D. TerrainData** | the stored heightmap, alphamaps, tree instances, detail layers | being edited, sculpted or read back into generation |
| **E. Unity rendering / material** | TerrainLit material, `TerrainLayer` appearance (Inspector), pixel error, basemap, tree and detail distances, lighting | height, placement or classification decisions |

Surface and vegetation rules stay bake-side classification of whatever TerrainGen produced. If Stage 4
exposes a component breakdown (e.g. "plateau wall", "coast cliff"), the surface rules may read it as they
read uplift today. They must never become a reason to change relief.

## 15. What must NOT be changed

- No removed legacy terrain pipeline or any external terrain pipeline, no backup restore, no graph-based terrain system, no generic rule
  engine.
- No edits to Stage 3C / 3D / 3E files, `StructuralElevation`, `CarveField`, `Stage3Field`, valley or basin
  code.
- No Stage 1 / Stage 2A / Stage 2B edits without the owner decisions D1 / C7 and a version bump.
- No relief authored in `OverworldTerrainField`, `OverworldTerrainBuilder`, `TerrainData` or the scene;
  no manual sculpting; no post-bake repair.
- No hydrology inside TerrainGen; hydrology remains the optional downstream carve.
- No hand edits to `Overworld_Generator.unity`; regenerate through `GenerateAllAndSave`.
- No surface or vegetation default changes "for looks" before the shape changes.
- No performance refactor bundled into the shape work (see §17 R6).
- No touching the user's unrelated uncommitted changes; no commit.

## 16. Proposed next implementation step

> **Done, 2026-09-16.** Step 1 became
> [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md);
> steps 2 and 3 are committed (`IslandRelief*`, the dump, thirteen tests), four of those tests fail on
> design-level causes and
> [revision R1](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md) derives the fixes. Step 4
> still holds: `TerrainAuthorityField` is untouched. The relief structure chosen was macro-plate blocks
> plus knoll cores plus a massif apron — not crust-block tilt. The measured effect on the questions this
> preflight raised is in R1 §15.

**[PROPOSAL] Stage 4A — island-floor relief, TerrainGen-only, dump-first.**

Scope:
1. Write `PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md` (short) and get owner answers to: the
   structural cause of lowlands and uplands (crust-block tilt vs macro-plate warp), amplitude and
   wavelength bands, apron shape, and the composition rule for future Stage 3F.
2. Implement new files only: `IslandReliefSettings`, `IslandRelief` (structure: lowland and upland regions,
   apron), `IslandElevation.At`, `TerrainGenIslandReliefDump` (height, slope, contour, hypsometry,
   reachability, depression check through the existing `HydrologyGrid` depression fill), and
   `TerrainGenStage4ATests`.
3. Tests assert: zero relief at and seaward of every coast sample (ocean bit-identical to Stage 1); Stage 1
   and Stage 2B hashes unchanged; point query = lattice; determinism across repeat runs; no undeclared
   closed depression; land never within the sea margin; budget ≤ 700 m; stream isolation (changing the
   relief seed stream moves no Stage 1/2/3 output).
4. **Do not switch `TerrainAuthorityField` in this step.** Integration (one line, version bump, re-pinned
   authority hash, fresh-process determinism, scene regeneration) is a separate, reviewable step after the
   dump metrics are accepted.

Notes on the open authoring defaults from the migration report:

| Default | Relevant to island shape? | Recommendation |
|---|---|---|
| Mountain soil (drainage 20 000–150 000 m²) | **indirectly** — soil is 0.1 % because the relief has no convergence | leave; re-measure after Stage 4A / 3F |
| Mountain rock vs cliff (35–42°, 50–58°) | **yes** — slope distribution will change | leave; re-tune after the massif apron lands |
| Vegetation density | no | leave for a later art pass |
| Texture licence | no | resolve before committing `Assets/Art/Terrain/SurfaceLayers/`; independent |
| "Measured" sand band 27.0–28.4 m, seabed rock 22–24 m, grass peak 15 | **yes for 4C** — calibrated against the removed pipeline's island | treat as legacy-calibrated; revisit with coastal character |

**Performance is not a blocker.** **[FACT]** Stage 1 takes ~6 s (its field ~5.9 s). The remaining ~17 s
of authority time is the bake sampling every lattice vertex through point queries, with the coast
distance evaluated twice per vertex, instead of reusing Stage 1's identical lattice arrays. Stage 4A is
developed in dumps (Stage 2B dump: ~7 s per seed), where this cost does not apply. A later, separate
optimisation can reuse the lattice arrays and keep the pinned hash.

## 17. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | Stage 4 relief reads as noise instead of structure | structural causes only (crust / plate geometry, massif apron); bounded bands; isotropy and period checks from `OverworldTerrainDiagnostics`; human review |
| R2 | Undeclared closed depressions (lakes nobody placed, hydrology noise) | a depression-fill test on every canonical seed; lowlands drain to the coast by construction or are declared |
| R3 | Relief pushes land below sea level or moves the coast | shore gate exactly 0; floor never below `SeaLevel + margin`; Stage 1 ocean bit-identity test |
| R4 | Future Stage 3F carve collides with Stage 4 plateaus or apron | Stage 4B keep-outs read Stage 3 corridors read-only; 3F composition rule written into the Stage 4 design now |
| R5 | Massif wall stays a mesa even with an apron | measure reachability and >45° share after 4A; if still failing, escalate to 3F spurs or a Stage 2B contract change (C7), never to a bake-side fix |
| R6 | Performance work mixed into shape work hides a relief change | keep the lattice-reuse optimisation separate and hash-neutral |
| R7 | Worlds without a massif (1337, 256, 777) stay dull | Stage 4 acceptance includes a no-massif seed |
| R8 | Silhouette remains the limiting factor | decide D1 after 4A with images; the Stage 1 reopening cost is known (hashes, 29-seed re-audit) |
| R9 | Downstream tuning drifts (POI `MinHeight01`, settlement flatness, road grades) | run markers and settlements in validation; compare placement counts (32 POIs placed today on 110201) |
| R10 | Visual judgement only from top-down analytic images | add an eye-level review capture before accepting 4B / 4C |
| R11 | Stale history misleads the next step | see "Hidden traces" below; do not use legacy baselines as targets |

### Hidden traces of the removed architecture (reported, not changed)

None re-introduces the removed pipeline. Repository reference search for its name: 0.

- `WORLD_RELIEF_PRINCIPLES.md` §6 "first measured baseline — 13 shelves, 15 s bake" and the per-tile table
  come from the removed pipeline; §2 "0.7 m/px" is not the native resolution (2.73 m). **Stale; not an
  authority.**
- `TerrainGenSettings` crest-height remark calibrates 200–330 m against "the previously shipped world"
  peak of ~330 m (a legacy measurement used as a design anchor).
- `TerrainSurfaceRules` values labelled "measured" (sand band, seabed rock, grass peak) were measured on
  the removed pipeline's island.
- `PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md` "Generation Order" still places drainage and carve inside
  TerrainGen (superseded; see §12).
- `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §3 and §7 tables describe shipping heights as coming from the
  external pipeline (historical, bannered).
- `OverworldTerrainDiagnostics.ReportShelfProfile` keeps a shelf-era name (cosmetic).
- `TerrainGenStage3CDump` builds `Stage3Field` through the legacy pre-Slice-C trajectory path; its images
  are not representative of frozen Slice C.
- User-local, not source of truth, untouched: `UserSettings/Layouts/default-6000.dwlt` and
  `CurrentMaximizeLayout.dwlt` still reference a window of the removed package; `Library/` caches from
  before the migration.
- Outside the repository: `D:/Hobby/game-dev/_terrain_migration_backup_2026-09-13/` holds the removed
  package and legacy assets. It must never be restored into the project; deleting it is the owner's call.

## 18. Validation plan for seeds 110201 and 424242

Run for Stage 4A (dump-first), then again at integration. Add one no-massif control seed (1337).

**Invariants (must hold exactly):**

| Check | Tool | Baseline today |
|---|---|---|
| Stage 1 golden hashes | `TerrainGenStage2ATests` | 110201 `7599879a71250a06`, 424242 `6266075eaf593ee2` |
| Stage 2B field hashes | Stage 2B dump | `1de91e469513054b`, `9d9d7b3c2560bc36` |
| Stage 3 suite | EditMode (TerrainGen Stage 3*, valley tests) | 90/90 |
| ocean samples moved by relief | Stage 4 dump | must be 0 |
| point query = lattice | Stage 4 tests | exact |
| fresh-process determinism | `OverworldTerrainValidation.RunBatch` ×6 per seed (integration) | identical signatures |
| seams / height error | validation | 0.0000 m / ≤ 1.07 cm |
| full EditMode | test runner | 555/555 (+ new Stage 4 tests) |

**Shape metrics (baseline → direction; thresholds set in the Stage 4 design):**

| Metric | 110201 | 424242 | Direction |
|---|---|---|---|
| land in 70–110 m | 4.6 % | 6.7 % | substantially up (break bimodality) |
| island-floor local relief median (112 m) | 3.4 m | 3.8 m | up, to hill scale |
| floor slope 0–5° | 52.8 % | 45.3 % | stays ≥ 35 % over all land (principles §6) |
| all land >45° | 6.1 % | 7.2 % | 6–15 % |
| reachable at ≤45° | 92.3 % | 91.5 % | ≥ 95 % |
| undeclared closed depressions | 0 | 0 | stays 0 |
| curvature ratio per tile | 0.94–1.06 | — | 0.85–1.20, no grid period |
| soil weight on land | 0.001 | 0.001 | up (convergence appears) |
| hydrology tributaries (pass A analysis) | 0 | — | > 0 |
| T2 / T3 count (4B) | 0 / 0 | 0 / 0 | 6–10 / 12–20 |
| POIs placed (markers) | 32 | — | no regression |
| authority / surface step time | 23.2 / 25.7 s | 23.8 / 26.4 s | no more than +15 % |

**Human review:** height, hillshade, slope-class and contour images per seed (as produced for this
preflight), plus eye-level captures once a capture tool exists. Contours must stop being coast-parallel
rings; the massif must read as rising out of foothills rather than standing on a plain.

---

## Result

```
CURRENT STATE:            PASS
ARCHITECTURE:             PASS
FROZEN STAGES:            PASS
NATIVE TERRAIN PATH:      PASS
ISLAND SHAPE READINESS:   NEEDS DESIGN
RECOMMENDED NEXT STEP:    Stage 4A island-floor relief — a new additive TerrainGen stage (new files only,
                          dump + tests, zero at the shore, composed after the frozen StructuralElevation),
                          designed and measured on 110201 / 424242 / 1337 before TerrainAuthorityField is
                          switched.
```

**Verification of this preflight** (fresh processes, 2026-09-14): focused EditMode run
`OverworldTerrainBakeTests; TerrainGenStage1Tests; TerrainGenStage2BTests; TerrainBakeFlagsTests` —
**60 / 60 passed**. Native validation signatures reproduced (`c08574603378`, `a8d7baad5360`). Saved scene
from disk `0bd540ab3980`. Tile assets restored byte-identical. Scene file and `git status` unchanged. No
production file modified.
