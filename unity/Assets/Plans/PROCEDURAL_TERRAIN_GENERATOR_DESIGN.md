# Procedural Terrain Generator — Architecture Design

**Status:** **Approved by the project owner 2026-09-05.** Implementation proceeds stage by stage through
"Recommended Implementation Stages" below. Stage 5 is a go/no-go gate — see [OPEN] 1.

> **Status update 2026-09-13 — terrain representation.** TerrainGen is now the only terrain authority.
> Its structural field (Stage 1 landmass + Stage 2 massif uplift, `StructuralElevation`) is baked
> straight into Unity `TerrainData` — heights, surface weights, trees and ground cover — by the
> Unity-native terrain bake described in [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).
> The external terrain pipeline this design was written against has been **removed**, so there is no
> `TerrainSource` switch and no second path: statements below about "the current pipeline", its
> shaping recipe or its carve describe the state on 2026-09-05. Stage 3C/3D/3E topology remains
> frozen and is not yet part of the baked field.

> **Status update 2026-09-16 — where the implementation actually stands.** The code's stage names are
> finer than the list in "Recommended Implementation Stages" below, which was written in 2026-09-05
> terms. The mapping, so a reader does not look for stages that were never built under those names:
>
> | Implemented (code, frozen by hashes) | This document's stage list |
> |---|---|
> | Stage 1 landmass and coastline (`Stage1LandmassGenerator`) | Stage 1 |
> | Stage 2A macro structure, 2B massifs (`MacroPlates`, `MountainMassif`) | Stage 2 |
> | Stage 3A ownership, 3B spurs, 3C valley topology and trajectories, 3D/3E structural basins and outlets | Stage 3 |
> | Stage 3F valley carve — **not implemented**, see [PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md) | part of Stage 7 |
> | Stage 4A island relief (`M` + plate blocks + knolls + massif apron) — R1 implemented dump-first, tests green, **not** in the authority path, see [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md) | the floor half of Stage 4 |
> | Stage 4B plateaus and walls, 4C raised coast — not started | the rest of Stage 4 |
> | Unity-native terrain bake of Stage 1 + 2 | Stage 8, delivered early |
> | Hydrology analysis and carve on the lattice (`HydrologyBake.ApplyPendingCarve`), step off by default | Stages 6 and 7 for water |
>
> Two design positions below have moved with it:
>
> - **Stage 5's "single band-limited, structure-modulated noise field"** is not what Stage 4A did. The
>   relief that fills the empty 70–110 m band is positive structural relief owned by plates, crustal
>   cells and the massif frame; noise has no role in it. The isotropy gate ([OPEN] 1) survives as a
>   test (curvature ratio 0.85–1.20, measured 0.96–1.01 on Stage 4A), but a refinement *noise* stage is
>   no longer the way the design expects relief to arrive.
> - **"Stage 4 — terraces and structural basins"** split: the structural basins shipped with Stage
>   3D/3E, the T2 plateaus are Stage 4B, and the floor relief Stage 4A added was not in this plan at
>   all — it came from measuring the baked island and finding a 70–110 m band that held 0.1–7.8 % of
>   land.

**Date:** 2026-09-05
**Applies to:** Track W (World Gen), terrain relief ownership
**Related:** [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md), [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md), [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md), [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md), [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md)

Claims are tagged the way [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) tags them,
so a later reader can separate evidence from judgement:

- **[FACT]** — read out of this repository; the file and symbol are named.
- **[CONCLUSION]** — follows from the facts above it.
- **[ASSUMPTION]** — believed, not measured. Each one names how it will be checked.
- **[OPEN]** — deliberately unresolved. Not silently decided.

---

# Executive Summary

The generator is a **geographic structure model**, not a height-field recipe. A seed produces a small
set of explicit world-space features — a coastline polygon, mountain-range spines, ridge spurs,
structural basins, plateaus — and terrain height at a world coordinate is a deterministic function of
distance to those features. Noise never decides where anything is; it perturbs feature *geometry*
within bounds, and adds a small, structure-modulated amplitude at the very end.

Four decisions carry the design:

1. **One world height field, not four tile fields.** The world is evaluated once as a
   1025 × 1025 lattice at 2.734 375 m in world coordinates; the four Unity tiles are windows into it.
   Tile seams and tile-order independence stop being problems to solve and become properties of the
   representation. **[FACT]** `2 × (513 − 1) + 1 = 1025` is exactly the lattice
   `HydrologyTerrainGrid` already assembles from the baked tiles.
2. **Hydrology is not a later stage — it is the arbiter of the terrain.** The existing drainage
   analysis (`HydrologyPlanner`) already takes a plain `float[]` height field and returns geography.
   The generator feeds it the *preliminary* field, and the rivers, lakes and valleys it returns are
   then carved into that same field before any terrain is written. **[CONCLUSION]** The blocker that
   stopped hydrology shipping — an extra biome in the external pipeline moving the terrain by
   itself — disappears entirely, because with the generator there is no external biome and no
   post-bake repair: the carve is simply part of producing the field.
3. **The ridge network and the drainage network are duals.** Spurs are generated as an explicit
   branching skeleton; the space between spurs is a valley by construction; drainage then decides
   which of those valleys actually carries water. That is what makes "why is this river here" answerable.
4. **The generator produces relief only.** Surface weights, vegetation and materials are not the
   generator's. *(Since 2026-09-13 they are produced by the Unity-native terrain bake from world-space
   rules — see the status update above.)*

The anti-goal is concrete and was measured here: the external pipeline's island shaping recipe
produced directional banding with a curvature ratio of 0.57–0.71 where 1.0 is isotropic, and eight
single-cause hypotheses were killed by experiment (the investigation records were removed with that
pipeline on 2026-09-13). A structure-first model has no global noise field to band. That is the
central hypothesis of this design, it is unproven, and Stage 5 is where it gets falsified or confirmed
by the diagnostic that already exists.

---

# Repository Findings

## World scale — verified, not assumed

**[FACT]** `Assets/Scripts/World/WorldScale.cs`:

| Constant | Value |
|---|---|
| `OverworldTileMeters` | 1400 |
| `OverworldTileCountX` / `Z` | 2 / 2 |
| world extent | 2800 × 2800 m |
| `ChunkSizeMeters` | 64 |
| `TerrainHeightBudgetMeters` | 700 |
| `SeaLevelMeters` | 24 |
| `IslandOceanMarginMeters` | 280 |
| `LandscapeCostCellMeters` | 10 |

**[FACT]** `OverworldTerrainGrid.DefaultHeightmapResolution = 513`,
`DefaultAlphamapResolution = 512`, `DefaultDetailResolution = 512`. Tiles are named
`OverworldTerrain_{x}_{z}` at `(x·1400, 0, z·1400)`.

**[CONCLUSION]** Heightmap sampling is **1400 / 512 = 2.734 375 m per pixel**, and adjacent tiles
share their edge row, so the world lattice is **1025 × 1025**. Alphamaps land on the same pitch.

**[FACT]** Vertical quantisation is documented in-repo as **~21 mm at a 700 m budget**
(Unity stores heights in 16 bits over half the normalised range; the terrain bake measures a worst
error of 1.07 cm against the field — half a step). Any relief feature must be well above that or it is rounding noise.

**[CONCLUSION]** The finest representable horizontal feature is ~2.73 m, and a wall spanning 1–2
pixels is the steepest slope the container can hold. A 20 m drop over 2 px ≈ 74°. Cliffs are
possible; vertical walls are not.

## Seed and determinism

**[FACT]** `GameDev.World.Seed.WorldSeed : IWorldSeed` — `Fork(streamId)` and
`Fork(streamId, a, b = 0)` mix the stream id and integer keys into the seed and return a
`System.Random`. **[FACT]** `AGENTS.md` line 362 requires `System.Random` via `IWorldSeed.Fork` and
forbids `UnityEngine.Random`.

**[FACT]** Streams already in use: `terrain.seafloor`, `terrain.islandbase`, `terrain.islandring`,
`terrain.coast.yaw`, `terrain.mountain.place`, `terrain.mountain.{i}`, `terrain.mountain.ring.{i}`,
`poi.{kind}.{i}`, `poi-visual`, `settlement`, `player.spawn`, `hydrology`.

**[FACT]** There is a second, legacy `GameDev.World.WorldSeed` class in
`Assets/Scripts/World/WorldSeed.cs`, distinct from `GameDev.World.Seed.WorldSeed`. It shadows the
real one from inside `GameDev.World.*` namespaces — this cost a compile error during the migration
spike. New code must use the fully qualified `Seed.WorldSeed`.

## Terrain access and the downstream contract

**[FACT]** After the 2026-09-05 migration spike, the runtime tree `Assets/Scripts/World/` contained
**zero** references to the external terrain package. `OverworldTerrainAccess` discovers
tiles by the project-owned `OverworldTerrainTile` marker (or the `Terrains` parent name), and
`OverworldTerrainGrid` creates the tile grid with no third-party code.

**[FACT]** Everything downstream reads `UnityEngine.Terrain` / `TerrainData` through
`OverworldTerrainAccess` and `OverworldTerrainSampler`.
Verified consumers: `LandscapeCostField`, `RoadPathfinder`, `RoadCorridorDeformer`,
`PoiPlacementEngine` / `StubPoiPlacer`, `SettlementSiteValidator` / `SettlementTerrainConform` /
`SettlementDustPaint`, `SeaLevelOceanSampler`, `InlandWaterSampler`, `HydrologyTerrainGrid`,
`OverworldPlayerMenu`, `OverworldTileChunks`.

**[FACT]** `LandscapeCostField` is constructed from two delegates —
`Func<float,float,float> heightAtWorld` and `Func<float,float,BiomeId> biomeAtWorld` — and
precomputes height, slope, curvature, roughness and biome on its own lattice. It never asks who made
the terrain.

**[FACT]** Bulk per-point sampling through `OverworldTerrainAccess.SampleHeightWorld` costs
~3.1 µs/query because it runs `FindObjectsByType<Terrain>` every call (measured in the migration
spike; 52× a resolved-tile sample). `RoadNetworkMenu` and `HydrologyTerrainGrid` both work around it
by precomputing. Any new bulk consumer must do the same.

## The drainage analysis is reusable; the carve architecture is not

**[FACT]** `Assets/Scripts/World/Hydrology/` is 3 768 lines of pure C# with no `UnityEngine`
dependency beyond `Vector2`/`Vector3`. `HydrologyPlanner.Plan(HydrologyGrid, HydrologySettings,
IWorldSeed) → Result` runs priority-flood depression fill → D8 → flow accumulation → basins → lakes →
reach network → Strahler order → waterfall candidates → validation.

**[FACT]** `HydrologyGrid` is constructed from `(originX, originZ, width, height, cellSize, float[]
heights, seaLevel)`. It needs no terrain and no scene.

**[FACT]** It shipped and was measured on the real island, seed 110201: 6 river systems, 26 reaches,
17 tributaries, 16 sources, 2 terrain-derived lakes, 38 waterfall candidates, 6.03 km of river,
identical across separate Unity processes, every reach validated to reach the ocean.

**[FACT]** The **carve** was what was blocked. Adding a fifth biome to the external pipeline moved the terrain
by itself: with the carve texture zeroed, and again with the height input physically disconnected,
and again with the blend flipped from `Lower` to `Raise`, the world drops identically from 393 m to
357 m, up to 38.41 m of movement at the same coordinate. Two bakes without hydrology were
bit-identical, so this was not the carve field. *(Resolved 2026-09-13: the carve is now a depth field
the Unity-native terrain bake applies to its lattice — `HYDROLOGY_GENERATION_PLAN.md` §8.)*

**[CONCLUSION]** The algorithms are sound and independent of terrain provenance. The *integration
architecture* — carving through an external biome because "the external pipeline owns terrain height" — is what failed,
and it is exactly the constraint that stops existing when the project owns the height field.

## Design intent that survives the external pipeline

**[FACT]** `WORLD_RELIEF_PRINCIPLES.md` is a sourced, numeric statement of what makes relief read as
a place. It is engine-independent and should be treated as the quality specification for the new
generator:

- **Three elevation tiers, minimum 2.5× separation**: T1 massif 250–530 m / 1000–2000 m footprint /
  ×1–2; T2 plateau 18–40 m / 150–450 m / ×6–10; T3 outcrop 5–12 m / 40–90 m / ×12–20. Heights come
  from a fixed table, never a continuous random range. Stacking only onto a strictly higher tier, two
  levels maximum.
- **A plateau is flat top + near-vertical wall + talus apron**, the wall does not run all the way
  round, and the uneven flank is the ramp. A wall meeting flat ground at a hard corner is the
  clearest "this was generated" tell.
- **Every slope must drain somewhere**; a closed basin with no outlet is a bug unless it is a lake.
- **Banned artificiality tells**: radially symmetric mountains, ridges of uniform height, valleys
  with no headwaters.
- **Negative space is a budget**: T2 + T3 footprints cover 25–35 % of dry land, hard ceiling 45 %.
- **Acceptance criteria with numbers** (measured by `OverworldTerrainDiagnostics` on every bake):
  dry land reachable from the shore at ≤ 45° **≥ 95 %**; dry land steeper than 45° **6–15 %**; dry
  land under 5° **≥ 35 %**; distinct `TerrainLayer` per tile ≤ 7. 45° is not aesthetic — it is
  `CharacterController.slopeLimit` and the default NavMesh agent slope.

**[FACT]** The island shaping investigation (2026-08) recorded the isotropy target and the tooling that measures it:
curvature ratio within roughly 0.85–1.20 on every tile with no consistent direction, no Z- or
X-period peak above the stencil floor, axis-aligned orientation share near the 17 % even-spread
baseline, curvature PNG showing branching drainage rather than scan-lines — accepted on three seeds.
The external package's own sample terrain scored 0.84 / 0.87 / 0.97 / 1.50; the project's recipe scored 0.57–0.71.

**[CONCLUSION]** The project already owns both the quality bar and the measurement harness for it.
The new generator does not need a new definition of "good"; it needs to pass the one that exists.

---

# Current Architecture

```
OverworldPipeline.GenerateAllRoutine          (Editor coroutine, WorldGenSettings step mask)
  │
  ├─ Surface + Mountains  ── OverworldTerrainBake ──▶ heights, alphamaps, trees, detail
  │     TerrainGen StructuralElevation (Stage 1 + 2) → OverworldTerrainField →
  │     OverworldTerrainBuilder → TerrainData. Stage 3C-3E topology and Stage 4A relief
  │     exist but are not read by the bake.
  │
  ├─ Hydrology   (OFF by default — analysis and the lattice carve both work:
  │               HydrologyBake.ApplyPendingCarve)
  ├─ Markers     OverworldWorldPlan.BuildAndStorePois → StubPoiPlacer + LandscapeCostField
  ├─ Settlements SettlementOverworldMenu → OverworldSettlementAnchor
  ├─ Roads       (OFF by default — see OVERWORLD_TECH_DEBT items 0 and 8)
  └─ Player

                       everything above reads terrain through
                                       │
                          OverworldTerrainAccess / OverworldTerrainSampler
                                       │
                          UnityEngine.Terrain / TerrainData
```

Working and to be preserved: the tile grid (`OverworldTerrainGrid`, `OverworldTerrainTile`), the
access layer, `WorldSeed` forks, `WorldDefinition` / `OverworldWorldState`, `WorldScale`,
`LandscapeCostField`, POI placement, settlement siting, the water sampler seam, the drainage
analysis, `OverworldGenTiming`, `OverworldTerrainDiagnostics`, `ChunkCoord` / `OverworldTileChunks`.

---

# Legacy / Non-Working Systems

**Do not use any of these as an architectural foundation.**

| System | State | Why it must not be revived |
|---|---|---|
| `RiverTracer`, `LakeDefinition`, `WaterZone_*` hierarchies, per-lake water objects | Deleted | **[FACT]** `14 World Generation Roadmap.md`: they produced architectural complexity, hidden dependencies, duplicated systems and made terrain harder to change. Explicitly forbidden to resurrect. |
| The external-pipeline hydrology carve (four `Lower`-blend biomes + carve textures) | Removed 2026-09-13 | The fifth biome moves the terrain with its height output disconnected. This is the *integration*, not the analysis. Superseded by owning the height field. |
| The external pipeline's island shaping recipe (C#-built terrain graphs, mass/detail noise, shelf curve) | Removed 2026-09-13; was banded | Curvature ratio 0.57–0.71, systematic across 10 seeds (σ 0.189). **This is the noise-soup failure mode the new design exists to avoid.** Do not port the recipe. |
| `IslandNaturalGeography`, `IslandOccupancy`, `CoastalBeachShelf`, `GrassField`, `VegetationScatterMenu`, MapMagic recipe, MicroSplat, OrganicTown, VSP | Deleted / retired | Recorded as retired in `WORLD_GENERATION_PIPELINE.md`. |
| `OverworldTerrainSampler.SampleMoisture01` | Working but wrong | Unseeded Perlin; discards its `moistureSeed`, so moisture is identical across seeds. A known defect, not a contract to preserve. |

**Useful material inside otherwise-legacy documents** — keep the algorithm, drop the architecture:

- `HYDROLOGY_GENERATION_PLAN.md` §6 (priority-flood → D8 → accumulation, and the six rejected
  alternatives with reasons), §9 (fill-then-spill lake policy), §10 (contributing-area channel
  initiation, per-system pruning), §11 (ocean-outlet invariant), §16 (validation rules). All of this
  is terrain-independent and all of it is already implemented and tested.
- `WORLD_RELIEF_PRINCIPLES.md` in full.
- The eight banding hypotheses ruled out on the external pipeline (resolution, erosion, quantisation and
  noise explanations) — do not re-test them against TerrainGen without new evidence.

---

# Design Goals

1. **Geographic causality.** Every feature has a structural answer to "why is it here". Mountains
   exist because a range was placed; rivers because a basin drains; lakes because a basin has no
   lower outlet until that elevation.
2. **Determinism.** Same seed + settings + generator version ⇒ byte-identical height field,
   regardless of tile order, process, or execution context.
3. **World coordinates only.** No feature is ever defined per tile.
4. **Terrain and hydrology designed together**, with one explicit dependency order and one published
   truth about where the water is.
5. **Targets `TerrainData` directly** through the existing `OverworldTerrainGrid` tiles.
6. **Downstream systems unchanged.** They must not be able to tell which generator produced the
   terrain.
7. **Measurably passes the existing quality bar** — the slope bands, reachability and isotropy
   criteria that already exist in the repository, not new ones invented to be passable.
8. **Implementable in small reversible stages**, each independently testable, none requiring the
   whole generator.

---

# Non-Goals

- **Not a graph editor, node system, or external-terrain-pipeline replacement framework.**
- **Not a generic provider / adapter / DI hierarchy.** There is one terrain source and no switch —
  see "Terrain Representation".
- **Not a noise library evaluation.** Noise is a bounded perturbation, never the model.
- **Not a material, alphamap, vegetation, biome, road, POI or settlement system.** The generator
  produces relief and hydrology geography. Nothing else.
- **Not a restore of any deleted hydrology code.**
- **Not the surface and vegetation bake.** That is the terrain bake's job — see "Terrain Representation".
- **Not real-time or streaming generation.** The world is 2800 × 2800 m and is generated in one pass.
- **Not multi-tile continents.** Deferred, but the design records what would break.
- **Not an implementation.** No code, no prototype, no heightmap, no noise, in this task.

---

# Geographic Generation Model

The model has one shape throughout: **a small set of explicit world-space features, and a height
field derived from distance to them.** Everything below is a way of deciding where features go and
what profile they carry.

Two vocabulary items used everywhere:

- **Profile** — a 1-D curve sampled by a normalised parameter, either along a feature (crest
  elevation along a spine) or across it (flank shape by distance). Profiles are authored data
  (a handful of control points), jittered per feature within bounds. They are the reason a shape
  reads as a landform rather than as a bump.
- **Support** — every feature has a bounded footprint. A point outside it receives zero contribution.
  This is what makes evaluation tractable and what makes "which features explain this pixel"
  answerable.

## Landmass and Coastline

**Rejected: radial falloff and harmonic anchor rings.** **[FACT]** The current island is a 36-anchor
harmonic ring plus an outer falloff, and it produces visible straight chords (~60 m of shore per
anchor) that Chaikin smoothing cannot hide. More fundamentally, any
`radius(θ)` function is star-shaped about its centre: it cannot express a bay that curls back, a
peninsula with a neck, or a lagoon behind a spit. Those are the shapes that make a coast read as
geography.

**Proposed: a crustal partition.**

1. **Crustal cells.** Deterministically place `N ≈ 40–70` seed points over the world rect plus a
   margin, with a minimum-separation (blue-noise) rule, then take their Voronoi partition. Cells are
   the atoms of the landmass. The margin matters: cells must extend past the world rect so the
   partition near the border is not an artefact of the boundary.
2. **Continental core and growth.** Pick one interior cell as the core, then region-grow to
   neighbours with a seeded per-edge weight until the accumulated area hits a **land-fraction
   budget** drawn from a narrow band. The result is a connected set of cells — the landmass — whose
   outline is irregular by construction and never star-shaped.
3. **Islets.** Optionally admit 0–2 small disconnected cell groups, budgeted separately, so the world
   is not always exactly one blob. Each must be inside `IslandOceanMarginMeters` of open water.
4. **Coast refinement.** The union boundary is a polygon of long straight cell edges. Refine it by
   **constrained midpoint displacement**: recursively subdivide each edge, displacing the midpoint
   perpendicular by a seeded amount that halves with each level, with three constraints — the polygon
   stays simple (no self-intersection), stays inside the world rect minus the ocean margin, and
   preserves enclosed area within a tolerance. Terminate when segment length reaches ~2–3 × the
   heightmap pitch, i.e. ~8 m; below that the coastline cannot be represented anyway.
5. **Coastal height profile.** Height near the coast is a function of **signed distance to the coast
   polygon**: offshore it descends along a shelf profile to `SeaLevelMeters − OceanShelfDepthMeters`
   at the world border; onshore it rises along a coastal profile. Both are authored curves, not noise.
6. **Coastal character is caused, not chosen.** The onshore profile is selected per coastal segment by
   what stands behind it: a segment whose inland neighbourhood is inside a mountain range's support
   gets the **cliff** profile; a segment at the mouth of a major drainage basin (known after the
   drainage stage — see "Generation Order") gets the **estuary/bay** profile; everything else gets
   the **beach** profile, which is what `BeachWidthMeters = 80` describes.

**Why this is not noise.** The irregularity comes from the partition and from a curve subdivision with
geometric constraints. A player asking "why does the coast bend here" has the answer "because this is
the boundary between two crustal cells, and this cell is not land".

**[OPEN]** Whether Voronoi cells at 40–70 over 2800 m (≈ 350–450 m mean cell size) give enough coastal
variety, or whether a two-level partition (coarse cells subdivided near the boundary) is needed.
*Checked by:* Stage 1 dump — coastline length / convex-hull perimeter ratio, and visual inspection of
three seeds.

## Mountain Systems

**Rejected: independent mountain blobs.** The external pipeline placed 1–2 mountain biomes at seeded
positions with elliptical anchor rings. They are independent objects; nothing connects them to the
landmass or to each other, which is why the world reads as "an island with mountains on it" rather
than as an island *shaped by* its mountains.

**Proposed: uplift belts along crustal boundaries.**

1. **Belt selection.** Every interior edge of the crustal partition that lies inside the landmass gets
   a seeded **convergence weight**. Chain high-weight adjacent edges into polylines; keep the 1–2
   longest chains whose length exceeds a minimum. These are the **range spines**. Because the spines
   follow cell boundaries, a range's direction, length and relationship to the coast are all
   consequences of the same partition that made the coast — the two are not independent.
2. **Spine smoothing.** A chain of Voronoi edges is angular. Smooth it into a polyline with bounded
   curvature (turn angle capped), which is also what keeps ridges from meeting at implausible corners.
3. **Along-spine profiles.** Each range carries:
   - `CrestProfile(t)` — elevation of the crest along normalised length. Drawn from the **T1 table**
     (250–530 m above local ground) with at most ±15 % jitter, and shaped so the maximum is *not* at
     the midpoint — a symmetric range is one of the banned artificiality tells.
   - `HalfWidthProfile(t)` — 500–1000 m, so the footprint lands in the T1 1000–2000 m band.
   - `AsymmetryProfile(t)` — the two flanks are not mirror images; one is steep (the scarp side), one
     is long (the dip side). This single parameter removes most of the "radially symmetric mountain"
     signature.
4. **Branching.** At 1–3 seeded positions along the spine, attach a **secondary belt**: a shorter
   polyline leaving at a bounded angle (drawn from a range, never a fixed value), with crest heights
   scaled to 0.45–0.7 of the parent. Recursion depth ≤ 2. Branches are ranges, not spurs — spurs are
   the next section.
5. **Interaction with the coast.** Where a range's support crosses the coast polygon, the coastal
   profile switches to cliff (above). Where a range terminates inland, its crest profile tapers to
   zero over the last 15 % of its length, which is what produces foothills rather than a cut end.
6. **Height contribution.** For a point `p`: find the nearest point on the spine → `(t, d, side)`.
   Contribution = `CrestProfile(t) · FlankProfile(d / HalfWidth(t), side)`. `FlankProfile` is a single
   authored curve — concave near the crest, convex at the toe — which is what gives a talus apron
   instead of a cone. Multiple ranges combine with a **smooth maximum** (see "Ridges and Valleys" for
   why not plain `max`).

**Elevation hierarchy.** T1 is the range crest. T2 plateaus and T3 outcrops are placed in the next
sections, on the flanks and the lowland, and they obey the 2.5× separation rule against whatever is
locally beneath them — not against sea level.

## Ridges and Valleys

This is where the design either works or degenerates into noise, so the mechanism is stated precisely.

**Proposed: an explicit spur skeleton, with valleys as its complement.**

1. **Spur spawning.** Along each range spine, at seeded intervals, spawn **spurs** — polylines leaving
   the spine and descending its flank. Three rules do the anti-artifact work:
   - **Alternating-side bias with jitter**, so spurs do not comb one way.
   - **Interval drawn per spur** from a range whose mean scales with the local half-width, so spacing
     is not periodic. Periodic spacing is exactly the ~101 m repeat the banding investigation measured.
   - **Departure angle drawn per spur** from a bounded range around the local spine normal, and
     **rotated by the local asymmetry**, so the two flanks do not mirror.
2. **Spur geometry.** A spur is a polyline of 3–6 segments whose crest elevation **descends
   monotonically** from its attachment point to its toe, following a `SpurCrestProfile`. It ends where
   its crest reaches the local base surface. Sub-spurs branch from spurs with the same rules, depth ≤ 3,
   each generation shorter and lower — this is the self-similar structure real dissected slopes have,
   and it comes from recursion over geometry, not from summing octaves of a field.
3. **Non-crossing constraint.** A spur is rejected and re-drawn (bounded retries, then abandoned) if it
   crosses an existing spur of the same range or comes within a minimum separation. Spurs that cross
   are the "spidery starfish" failure mode.
4. **Height contribution.** Same shape as a range: nearest point on the spur → `(t, d)`, contribution
   = `SpurCrest(t) · SpurFlank(d / spurHalfWidth(t))`.
5. **Combination — smooth maximum, not `max`.** Plain `max` over overlapping features creates a crease
   at every intersection, and a field of creases reads as faceted. Use a soft maximum with a blend
   radius proportional to the smaller feature's half-width. The blend radius has an upper bound,
   because too much blending produces the "muddy shapes" `WORLD_RELIEF_PRINCIPLES` §1 forbids.
   **[OPEN]** the right bound. *Checked by:* Stage 3 slope-band measurement — over-blending shows up as
   the cliff share collapsing below 6 %.
6. **Valleys are the complement.** The space between two adjacent spurs is a valley by construction:
   its floor is where both spurs' contributions have decayed, and its axis is the locus equidistant
   from them. Nothing generates valleys directly. **[CONCLUSION]** This is what makes the drainage
   stage meaningful rather than decorative — the valleys already exist as geometry before any water is
   routed, and drainage decides which of them carry a channel.
7. **Headwaters.** Because every valley is bounded upslope by the spine or by a spur junction, no
   valley starts in the middle of a slope with nothing above it — the third banned artificiality tell
   is structurally impossible.

**How each named artifact is avoided:**

| Artifact | Structural reason it cannot appear |
|---|---|
| Parallel artificial ridges | Spur intervals and angles are drawn per spur, and spurs hang off a curved, non-straight spine |
| Concentric rings | Nothing is a function of radius from a point; there is no centre |
| Broad sinusoidal waves | No global periodic field exists at any stage |
| Obvious noise patterns | Noise never contributes more than the refinement budget (see below) and is modulated to zero on flats |
| Radially symmetric mountains | `AsymmetryProfile` plus per-spur angles; crest maximum off-centre |
| Ridges of uniform height | Crest profiles are per-feature, drawn from tier tables with jitter |
| Valleys without headwaters | Valleys are complements of the spur skeleton and are always bounded upslope |

## Basins and Lakes

**Rejected: "pick a lake position and dig a circular hole".** Also rejected: relying on the depression
policy alone. **[FACT]** The shipped policy is *fill, then spill* — priority-flood raises every closed
depression until it spills, and lakes are the connected components where `filled − original >
tolerance`, filtered by `MinLakeAreaSqm` / `MinLakeDepthMeters`. **[CONCLUSION]** That means the
generator does not choose lakes — but it *does* choose whether any closed depression exists at all. A
terrain built purely from descending spur profiles has almost none, and would produce a world with no
lakes. Conversely a rough terrain produces hundreds of tiny ones and the filters throw them away
after paying for them.

**Proposed: structural basins as first-class features, few and caused.**

Three geographic causes, each producing a bounded feature that lowers the surface inside a rim:

1. **Valley-head basin (cirque).** Where two spurs converge near the spine, the enclosed hollow is
   deepened. Cause: glacial/erosional over-deepening at a valley head. Naturally rimmed on three sides.
2. **Intermontane basin.** Between two ranges, or between a range and its branch, where the two
   supports leave a gap wider than a threshold. Cause: a downwarp between uplift belts. These are the
   large ones.
3. **Coastal lagoon.** Behind a spit or a barrier segment of the coast polygon. Cause: longshore
   deposition. Optional; 0–1 per world.

Each basin is `{ Id, Cause, Footprint polygon, RimElevation, FloorDepth }`, with a **budget of 2–5
per world**, and the depth drawn so that the basin is deeper than the drainage tolerance but shallower
than the local relief — a basin that swallows a whole valley is worse than none.

The basin only makes a lake **possible**. Priority-flood decides whether it holds water, where the
surface sits (the spill elevation), and what shape the shoreline has. If a basin turns out to have a
low outlet, it is a dry hollow — which is a perfectly good landform and must not be "fixed".

**Answering the question.** "Why is this lake here?" → "Because an intermontane basin sits between
these two ranges and has no lower outlet until 96 m."

**Lake speckle control.** If measurement shows small spurious depressions, the fix is at the source:
raise the refinement's low-pass floor and tighten the smooth-max blend, **not** post-filtering more
aggressively. A depression the terrain genuinely has is either a lake or a bug in the terrain.

## Drainage

**Reuse the existing analysis unchanged.** `HydrologyPlanner.Plan(HydrologyGrid, HydrologySettings,
IWorldSeed)` already implements exactly the chain the brief asks for, on a plain `float[]`:

```
elevation → priority-flood depression fill (ε, seeded from ocean cells)
          → D8 flow direction on the filled surface
          → flow accumulation (descending filled elevation)
          → drainage basins + lakes (filled − original > tolerance)
          → channel raster (contributing area ≥ ChannelInitiationAreaSqm)
          → reach network, split at confluences / lake inlets / outlets / ocean
          → Strahler order, width, bed elevation
          → waterfall candidates (profile of the ORIGINAL relief)
          → validation
```

**[CONCLUSION]** Evaluating it against the brief's requirements: it is terrain-agnostic, deterministic
by construction (priority-flood ties by insertion order, D8 by fixed neighbour order, accumulation by
descending elevation with an index tie-break), uses the seed in exactly one place (meander phase), and
already guarantees ocean reachability structurally. There is no reason to redesign it, and every
reason not to — it is the one part of the hydrology work that was measured and validated.

**Two changes of context, not of algorithm:**

1. **Its input is now the generator's preliminary field**, not `terrainData.GetHeights`. `HydrologyGrid`
   already takes a `float[]`; the adapter is one constructor call. `HydrologyTerrainGrid` (the
   Editor-side tile reader) stops being on the path for the generator.
2. **Its output is now consumed by the generator**, not by an external biome. This is the whole difference.

**One correctness point worth restating** because it will matter with a new terrain: *below sea level*
is not *the ocean*. `HydrologyGrid.BuildOceanMask` floods from the world border through submerged
cells, so an inland pit under 24 m is a depression, not a false outlet. A generator that produces
deep inland basins will exercise this path far more than the current island does.

## Rivers

Rivers are outputs of the drainage stage, not features the generator places. What the generator adds
is the **carve** — turning the published network into terrain.

**Carve model, two nested profiles per reach:**

1. **Valley** — a wide, shallow, smoothly-shouldered lowering centred on the reach, width and depth
   scaled by contributing area. This is what makes a river read as being *in* a landscape.
2. **Channel** — a narrow, deeper cut inside the valley, floor set to `RiverNode.BedElevation`.
   **[FACT]** the minimum representable channel is ~8.2 m (three heightmap pixels); narrower streams
   can exist as data and as visuals but not as terrain.

**Invariants the carve must satisfy** (each becomes a test):

- **Lowering only.** The carve never raises terrain. A carve field is a non-negative depth subtracted
  from the field.
- **Monotone along the reach.** `RiverNode.BedElevation` already descends downstream, and the carve is
  defined *from* the bed elevation rather than as a re-derived depth — so the carved channel floor is
  monotone by construction. **[FACT]** this is exactly how `HydrologyCarveField` computes it today,
  and the reason is stated in its own remarks: one definition, not two.
- **The lake outlet sill is not carved.** **[FACT]** the shipped design tapers the carve in over
  ~220 m below the spill, because cutting a full valley through the sill would drain the basin the
  analysis just measured. Real lake outlets are sills. This invariant survives unchanged.
- **No new closed depressions.** Lowering along an already-descending path cannot create one, but
  cross-channel interpolation and the valley shoulders can. Verified, not assumed — see H7 in the
  invariants section.
- **Bounded.** A maximum carve depth in `HydrologySettings`, so a mis-scaled contributing area cannot
  cut a canyon.

**Rivers begin in elevated catchments** because the channel-initiation threshold is contributing area,
and area accumulates downhill from ridge crests. **Rivers reach the ocean or a lake** because
priority-flood makes the surface monotonically descending to the sea. Neither is enforced by a rule;
both are properties of the method.

**Elevation/distance information is preserved.** `RiverNode` already carries `PositionXZ`,
`BedElevation`, `WaterElevation`, `CumulativeDistance` and `ContributingAreaSqm`, and
`WaterfallCandidate` carries `DropMeters` / `RunMeters` / `Slope`. Waterfalls, fords, bridges,
river-mouth POIs and moisture all read from that; none needs a new structure.

**Road crossings** are already served: `InlandWaterSampler` composes into `CompositeWaterSampler`
behind `IWaterSampler`, which is the interface `RoadCostSampler` and `CanBridgeSpan` already use.

## Terrain Refinement

**This is the only place noise appears, and it is bounded on four axes.**

| Axis | Rule | Why |
|---|---|---|
| **What it modifies** | The already-structured field, never feature placement | Structure must remain explainable |
| **Amplitude** | ≤ 25 % of the *local* tier's height, and never more than ~3 m absolute | T3 is 5–12 m; refinement must stay a tier below the smallest form, or it becomes a fourth tier and muddies the hierarchy (`WORLD_RELIEF_PRINCIPLES` §1) |
| **Scale** | 20–120 m | Above the 2.73 m pixel (below it is unrepresentable dither) and below the T2 150–450 m footprint (above it competes with structure) |
| **Where** | Modulated by structure: full amplitude on mid-slopes, → 0 on plateau tops, valley floors, river corridors, and within a margin of the coast | A flat top with noise on it is not a flat top; a noisy channel breaks drainage |

**It is applied to the preliminary field *before* the drainage analysis**, not after. If refinement
came last it would perturb the surface the network was computed on, creating micro-depressions and
channels that no longer sit in their valleys. Running it first means drainage sees the surface it will
actually get, and the carve — which comes after — is the last word.

**It must not be a global FBM sum.** A single band-limited field, one octave family, modulated to zero
over most of the map. If the isotropy measurement at Stage 5 moves in the wrong direction, this is the
first thing to reduce, because it is the only global field in the design.

---

# Generation Order

```
 seed + WorldGenSettings + generatorVersion
   │
 ① STRUCTURE                                    pure, cheap, no field
   │   crustal partition → landmass → coast polygon
   │   uplift belts → range spines + branches
   │   spur skeleton (recursive, non-crossing)
   │   structural basins, T2 plateaus, T3 outcrops
   │   ⇒ WorldStructure   (plain data, ~kilobytes)
   │
 ② PRELIMINARY FIELD                            1025 × 1025 @ 2.734375 m, world coords
   │   evaluate structure → base height
   │   + bounded refinement (structure-modulated)
   │   ⇒ WorldHeightField
   │
 ③ DRAINAGE                                     existing HydrologyPlanner, unchanged
   │   HydrologyGrid(field) → fill → D8 → accumulation → basins → lakes → reaches
   │   ⇒ HydrologyDefinition        ← THE published truth about water
   │
 ④ CARVE                                        valley + channel + lake basins
   │   subtract carve field from WorldHeightField
   │   ⇒ final WorldHeightField
   │
 ⑤ VALIDATE                                     re-run flow routing on the CARVED field
   │   assert the published channels still descend and still reach the sea
   │   assert no new closed depression outside published lakes
   │   failure ⇒ report loudly; never silently repair, never re-plan
   │
 ⑥ WRITE                                        4 × TerrainData.SetHeights, windows into the field
   │
 ⑦ PUBLISH                                      WorldDefinition.SetHydrology + SetStructure
   │
 downstream: biomes, materials, vegetation, POI, settlements, roads, player
```

## Is this ordering right?

The brief asks for the ordering to be evaluated rather than assumed. Three questions matter.

**Does hydrology need one pass, iterative passes, or a data model that allows modification before the
bake?** — **The third.** One pass is enough *because* the model is a field the generator owns and can
modify before writing. That is precisely what the external-pipeline architecture could not do, and why it needed
two bakes and a blend-based carve. **[CONCLUSION]** The two-pass structure in
`HYDROLOGY_GENERATION_PLAN.md` §7 was a consequence of *where the information lived*, not of the
problem. Owning the field collapses it.

**Should step ⑤ re-plan rather than validate?** — **No, and this is a deliberate architectural
commitment.** Carving changes slopes, so a second analysis of the carved field would find slightly
different channels, and a third would differ again. There is no fixed point worth chasing at 2.7 m
resolution. The published `HydrologyDefinition` is the one computed at step ③; step ⑤ asks only
whether the carved terrain is still consistent with it. If it is not, that is a bug in the carve
profile, and the right response is a loud failure, not another iteration.

**Should drainage run before the structure is final?** — **No.** Drainage is a global property of the
finished macro relief: where channels converge depends on the coast and every range together. That
part of the original reasoning survives intact.

**One ordering refinement the brief's sketch does not have:** coastal *character* (cliff / beach /
estuary) cannot be fully decided at step ① because estuaries depend on where the major basins drain.
Split it: the coast **polygon** is decided at ①; the cliff/beach split (which depends only on ranges)
is applied at ②; the estuary profile is applied at ④, together with the carve, where the river mouths
are known. This keeps the coastline geometry — which POI, settlement and road placement all depend on
— fixed before any water is computed.

---

# Determinism and Seed Strategy

## Streams

Reuse `IWorldSeed.Fork` exactly as it is. **No new RNG architecture.** Proposed streams, following the
existing dotted convention:

| Stream | Decides |
|---|---|
| `terrain.crust` | crustal cell points, continental growth, islet budget |
| `terrain.coast` | midpoint-displacement offsets along the coast polygon |
| `terrain.range.place` | which edge chains become ranges, and how many |
| `terrain.range.{i}` | one range's crest / half-width / asymmetry profiles and branch positions |
| `terrain.spur.{rangeId}.{ordinal}` | one spur's interval, angle, length and crest profile |
| `terrain.basin.{i}` | one structural basin's footprint and depth |
| `terrain.shelf.{i}` / `terrain.outcrop.{i}` | T2 / T3 placement and archetype choice |
| `terrain.detail` | the single refinement field |
| `hydrology` | already exists; unchanged |

**[CONCLUSION]** Adding a new *kind* of feature adds a new stream and cannot disturb any existing one,
because `Fork` mixes the stream id into the seed. This is the same property `poi.{kind}.{i}` already
relies on.

## Stability under change

The honest guarantee, stated precisely because a vaguer one would be false:

- **Appending is stable.** A feature draws from `Fork(stream, parentId, ordinal)` where `ordinal` is
  its index among siblings of the same parent. Raising a budget from 4 spurs to 5 leaves spurs 0–3
  byte-identical.
- **Inserting or reordering is not stable**, and no scheme makes it so without content-addressing
  every feature. Do not pretend otherwise; instead, never insert — append.
- **Sibling counts are drawn before any sibling is realised**, so the count itself does not depend on
  the siblings.
- **A deliberate change of geography is visible**: `WorldGenSettings` carries a `generatorVersion`
  integer, bumped whenever a change is intended to move the world. A seed plus a version identifies a
  world; a seed alone does not.

**Adding a POI or a river cannot reshuffle terrain** structurally: POI and hydrology streams are
forked after the structure exists and never feed back into it.

## Determinism risks, and what is actually guaranteed

- **[OPEN] `System.Random` across runtimes.** Seeded `System.Random` is stable within a runtime
  version but its algorithm has changed between .NET implementations. Within one Unity version this
  is fine; across a Unity upgrade it is not guaranteed. *Mitigation if it ever matters:* replace the
  generator *behind* `IWorldSeed` with a fixed integer hash, without touching a single call site.
  *Checked by:* a world-hash test that would fail loudly on a Unity upgrade.
- **[OPEN] Floating-point across machines.** Trig and `Mathf` may differ by an ULP on different
  architectures, which could in principle flip a priority-flood tie. The analysis already breaks ties
  by index rather than by value, which contains most of it. *Checked by:* comparing the world height
  hash produced on two machines — a test that must be run, not assumed.
- **Parallelism.** If field evaluation is ever parallelised, it must remain per-cell pure. Any
  accumulation into shared state, or any feature list built in completion order, breaks determinism.
  Stated here because it is the easiest way to lose it later.
- **No `UnityEngine.Random`, anywhere.** **[FACT]** `AGENTS.md` §Unity C# RNG.

---

# World Coordinates and Tile Seams

**The tile-seam problem is dissolved rather than solved.**

**[CONCLUSION]** Because adjacent Unity tiles share their edge row, four 513² tiles are exactly a
1025 × 1025 world lattice at 2.734 375 m. The generator evaluates that one lattice in world
coordinates and each tile is a **window** into it — tile `(tx, tz)` takes rows/columns
`[tx·512 … tx·512+512]`. The shared edge is literally the same array element read twice.

Consequences:

- **Seam continuity** is not achieved by matching, blending or a seam-stitching pass. There is nothing to match.
- **Tile-order independence** is not a property to test for in the generator — no code is per tile.
  It is still tested (S1, D2 below), because the test is what stops a future change reintroducing
  per-tile generation.
- **A feature crossing a tile boundary is one feature.** A range spine is a world polyline; a river
  reach is a world polyline; a coast polygon is a world polygon; a lake is a world basin. None of them
  knows tiles exist. Tiles appear for the first time at step ⑥.

**The point-query obligation.** Structure evaluation (step ②) must additionally be expressible as a
pure function `HeightAt(worldX, worldZ)` with no dependence on evaluation order or on neighbouring
samples. The bulk evaluator is what actually runs; the point query exists (a) as the determinism test
D2, and (b) because streaming and higher-resolution tiles would need it later.

**[OPEN] — the honest limitation.** Steps ③–⑤ are **global**: priority-flood, accumulation and basin
extraction cannot be evaluated at a point. So the *carved* field is not a pure point function; it is
`structure(x,z) + refinement(x,z) − carve(x,z)` where `carve` is a precomputed global raster. For a
2800 × 2800 m world this is irrelevant. For multi-tile continents it is the thing that would have to
be redesigned — most likely by computing drainage on a coarse global grid and refining locally. Record
it now; do not design for it now.

---

# Proposed Data Model

Plain deterministic data. Every type below is justified by a named consumer; anything without one is
not in the list.

## `WorldStructure` — the geographic skeleton

| Field | Why it exists | Consumer |
|---|---|---|
| `CoastRings : Vector2[][]` | The landmass outline. The one shape everything coastal is a function of. | field evaluation; coastal profile selection; future beach material |
| `Ranges : MountainRange[]` | Where the uplift is | field evaluation; "why is this mountain here"; future mountain-pass POI |
| `Spurs : RidgeSpur[]` | The dissected slope structure | field evaluation; valley identity |
| `Basins : StructuralBasin[]` | Where a lake becomes possible | field evaluation; explains every lake |
| `Shelves : TerraceForm[]` (T2 + T3) | Flat nodes and reference detail | field evaluation; future settlement/POI siting |

```
MountainRange   { RangeId Id; Vector2[] Spine; float[] CrestProfile; float[] HalfWidthProfile;
                  float[] AsymmetryProfile; RangeId? ParentId; int BranchOrdinal; }
RidgeSpur       { SpurId Id; RangeId RangeId; SpurId? ParentSpurId; Vector2[] Polyline;
                  float[] CrestProfile; float HalfWidthMeters; int Depth; }
StructuralBasin { BasinId Id; BasinCause Cause; Vector2[] Footprint;
                  float RimElevation; float FloorDepthMeters; }
TerraceForm     { TerraceId Id; TerrainTier Tier; Vector2 Center; float FootprintMeters;
                  float TopHeightMeters; float RampAzimuthRadians; int Archetype; }
```

**Properties.** Derived (rebuildable from seed + settings + version), deterministic, **not persisted**
— `WORLD_GENERATION_PIPELINE.md` §3 already states "generation is not persistence". Owned by the
generator, published on `WorldDefinition` beside `Hydrology` for the same reason hydrology is: so
downstream systems read one truth rather than re-deriving it. Pure C#, no `UnityEngine` dependency
beyond `Vector2` — the same rule that makes the hydrology package testable without a scene.

**No downstream system may *require* it in V1.** A world baked from the structural field alone has an
empty `WorldStructure`, and everything must still work.

## `WorldHeightField` — the one intermediate representation

```
WorldHeightField { float OriginX, OriginZ; float CellSize; int Width, Height; float[] Heights; }
```

**Why an intermediate exists at all**, given the brief's preference for `generator → TerrainData`:

1. The drainage analysis needs the whole world as one array regardless — **[FACT]** `HydrologyGrid`
   takes exactly `(originX, originZ, width, height, cellSize, float[], seaLevel)`. Producing tiles
   first and reassembling them would be strictly more work and would reintroduce the seam problem.
2. The carve modifies the field between analysis and write. With no intermediate, that modification
   would have to happen on `TerrainData`, i.e. a post-write repair pass — the exact pattern the
   project has ruled out.
3. It is the smallest possible type: a header and a `float[]`. It is not a framework, it has no
   behaviour beyond indexing, and `new HydrologyGrid(field…)` is a one-line adapter.

**Properties.** Derived, deterministic, not persisted, ~4.2 MB. Lives only during generation.

## Reused unchanged

`HydrologyDefinition` / `RiverReach` / `RiverNode` / `LakeBasin` / `WaterfallCandidate` /
`HydrologySettings` / `HydrologyGrid` — **[FACT]** already exist, already tested, already carry
everything waterfalls, bridges, moisture and river-mouth POIs need. `WorldDefinition`,
`OverworldWorldState`, `WorldSeed`, `WorldScale`, `WorldGenSettings`, `OverworldTerrainGrid`,
`OverworldTerrainTile`, `OverworldTerrainAccess`, `ChunkCoord`, `OverworldTileChunks`.

## Not proposed, deliberately

No `IHeightProvider` / `ITerrainGenerator` interface hierarchy, no generator registry, no feature base
class with virtual `Evaluate`, no ScriptableObject configuration tree, no per-feature MonoBehaviour, no
serialized asset for the structure. **[CONCLUSION]** There is exactly one generator and exactly one
consumer; an abstraction over a set of size one is cost with no benefit, and the repository's own
history (`WaterZone_*`, per-lake objects) records what that costs.

## `WorldGenSettings` additions

One field, no more:

- `int GeneratorVersion` — so a deliberate change of geography is identifiable.

*(A `TerrainSource` switch was proposed here for coexistence with the external pipeline. It was never
built, and with that pipeline removed there is nothing to switch between.)*

Generator tuning (feature budgets, tier tables, profile control points, refinement amplitude) belongs
in a `TerrainGenSettings` struct alongside `HydrologySettings`, for the same reason
`HYDROLOGY_GENERATION_PLAN.md` gave: **[FACT]** the old `WorldScale` lake/river constants are what made
the removed first attempt impossible to tune, and `WorldScale` is explicitly documented as carrying
"the ocean, and only the ocean".

---

# Unity Terrain Integration

```
WorldStructure → WorldHeightField (1025²) → [drainage, carve] → final field
                                                                     │
                          per tile: window [tx·512 … tx·512+512] ────┤
                                                                     ▼
                                    OverworldTerrainGrid tiles → TerrainData.SetHeights
```

**Writing.** For tile `(tx, tz)`: `h01 = Mathf.Clamp01(worldY / data.size.y)` with `size.y = 700`,
into `float[513, 513]`, one `SetHeights(0, 0, …)` per tile. Note `GetHeights`/`SetHeights` are indexed
`[z, x]` — **[FACT]** the convention `OverworldTerrainBuilder` uses.

**Grid creation is not the generator's job.** `OverworldTerrainGrid.Create(parent, spec, layers,
material)` already builds the tiles, stamps `OverworldTerrainTile`, sets resolutions, collider and
neighbours. The generator receives the tiles and fills them.

**Ordering against the existing post-bake writers.** `TerrainDeformer` → `RoadCorridorDeformer` /
`RoadSurfacePaint` / `SettlementDustPaint` write heights and alphamaps *after* the terrain exists —
**[FACT]** the documented road exception. They stay after the generator, and hydrology validation runs
**before** them, since a road corridor is allowed to violate the drainage the generator produced.

**Clamping and the height budget.** The field must be asserted within `[0, 700]` before writing, not
clamped silently. A structure that overshoots is a budgeting bug and must be reported — **[FACT]** the
terrain bake reports `ClampedHeightSamples` for exactly this reason, and deliberately reports rather
than corrects.

**What the generator does *not* write:** alphamaps, `terrainLayers`, trees, detail density. Those
belong to the terrain bake, which derives them from world-space rules over the same lattice —
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §3–§5.

---

# Downstream Contracts

| System | Receives | Needs modification? |
|---|---|---|
| `OverworldTerrainAccess` | `Terrain` / `TerrainData`, tiles stamped by `OverworldTerrainGrid` | **No** |
| `OverworldTerrainSampler` (height / slope / biome) | `SampleHeight`, `GetInterpolatedNormal` | **No** |
| `BiomeClassifier` | sea-relative height + slope + moisture | **No**, but see below |
| `SeaLevelOceanSampler` | a height delegate | **Yes — pre-existing defect.** `FromTerrain` closes over a *single* tile (**[FACT]** `OverworldWaterSamplers` remarks; `HYDROLOGY_GENERATION_PLAN.md` §2.3 [OPEN]). Today the island is centred and the bug is partly masked; a generator producing real coast on all four tiles will expose it |
| `InlandWaterSampler` / `CompositeWaterSampler` | `HydrologyDefinition` | **No.** They finally stop being empty by default |
| `LandscapeCostField` | two delegates | **No** |
| Roads (`RoadPathfinder`, `RoadVerticalProfile`, `RoadCorridorDeformer`) | height, slope, water | **No** contract change. But roads are **[FACT]** off by default with two open defects; a new terrain does not fix them and must not be blamed for them |
| POI (`PoiPlacementEngine`, `StubPoiPlacer`) | biome, slope, footprint stats | **No** contract change. **Tuning is at risk**: `MinHeight01 = 0.12` of the land band (~105 m) is a hard reject, measured in the migration spike. A generator whose lowland sits below that places nothing |
| Settlements (`SettlementSiteValidator`, `SettlementTerrainConform`, `OverworldSettlementAnchor`) | ground Y, water level, flatness | **No** contract change; needs T2 plateau tops to exist as flat nodes |
| Player spawn (`OverworldPlayerMenu`) | a TownApproach marker + ground | **No** |
| Chunking (`ChunkCoord`, `OverworldTileChunks`) | world coordinates | **No** |
| Terrain material / vegetation | alphamaps, `terrainLayers`, trees, detail | **Closed 2026-09-13** — written by the terrain bake (`TerrainSurfaceRules`, `TerrainVegetationRules`) |

**The one design goal that is fully met:** no downstream system can tell which generator produced the
terrain, because none of them asks. **[FACT]** verified in the 2026-09-05 migration spike — no external
terrain package references in `Assets/Scripts/World/`, and contract tests passed against a plain Unity terrain.

**Two contracts that would need work, stated plainly rather than deferred silently:**

1. `SeaLevelOceanSampler.FromTerrain` single-tile closure — a small, independent fix.
2. Surface weights and vegetation — implemented by the terrain bake on 2026-09-13.

**One opportunity, not a requirement:** `BiomeClassifier`'s moisture is an unseeded Perlin field that
discards its seed. `HydrologyDefinition` is the natural replacement (distance-to-water), and
`WorldStructure` could supply orographic context. Neither is in this design.

---

# Geographic Invariants and Tests

Structural, measurable, and mostly runnable without a scene — the drainage package already
demonstrates that a pure-C# geography layer is testable on synthetic fields
(**[FACT]** `HydrologyTestTerrain`, `LandscapeCostFieldTests`).

## Determinism

| Id | Invariant |
|---|---|
| D1 | Same seed + settings + `GeneratorVersion` ⇒ identical world height field hash |
| D2 | `HeightAt(x, z)` equals the bulk field at every lattice point (structure + refinement stage only) |
| D3 | No NaN, no infinity, anywhere in the field; every sample within `[0, TerrainHeightBudgetMeters]` |
| D4 | Structure feature ids and collection ordering are stable and sorted, never hash-iteration order |
| D5 | Raising a sibling budget by one leaves siblings `0…n−1` byte-identical |
| D6 | Re-seeding `hydrology` does not change the field before step ④ |

## Tiles and seams

| Id | Invariant |
|---|---|
| S1 | Shared edge samples between adjacent tiles are bit-identical |
| S2 | Writing tiles in any order produces identical `TerrainData` (guards against a regression to per-tile generation) |
| S3 | Every tile's `size`, `heightmapResolution` and position match `OverworldTerrainGrid.Spec.Default` after the write |

## Geography

| Id | Invariant |
|---|---|
| G1 | Coast rings are simple closed polygons inside the world rect minus `IslandOceanMarginMeters`; land is one connected component plus at most the islet budget |
| G2 | Land fraction of the world rect within its budgeted band |
| G3 | Tier separation ≥ 2.5× between adjacent tiers; no form's height lands in a gap between tier tables |
| G4 | Every spur's crest descends monotonically from its attachment; no two spurs of one range intersect; branch depth ≤ 3 |
| G5 | T2 + T3 footprint coverage of dry land within 25–45 % |
| G6 | No range's crest maximum at its own midpoint (anti-symmetry) |
| G7 | **Isotropy** — curvature ratio 0.85–1.20 on every tile with no consistent direction across tiles; no X- or Z-period peak above the stencil floor; axis-aligned orientation share near 17 %. **[FACT]** already implemented in `OverworldTerrainDiagnostics` / `OverworldBatchDiagnostics` |
| G8 | Slope bands: dry land > 45° in 6–15 %; dry land < 5° ≥ 35 %; shore-reachable at ≤ 45° ≥ 95 % |

## Hydrology

| Id | Invariant |
|---|---|
| H1 | Every reach reaches the ocean by following `DownstreamId`; no cycles |
| H2 | No node higher than its upstream neighbour beyond epsilon |
| H3 | Lake surface ≥ basin floor and ≤ spill elevation; boundary is a closed polygon |
| H4 | Carve is non-negative everywhere (lowering only) |
| H5 | After carving, re-routed flow still follows the published channels within tolerance |
| H6 | After carving, no closed depression exists outside the published lakes |
| H7 | The lake outlet sill is not cut below the published spill elevation |
| H8 | Water surface is never above terrain outside a published water body, beyond the freeboard `SettlementSiteValidator.DefaultFreeboardMeters` |

## Downstream

| Id | Invariant |
|---|---|
| P1 | Every lattice point of `LandscapeCostField` yields a finite, non-NaN cost |
| P2 | POI placement yields at least the requested TownApproach count; habitable fraction not below the current world's |
| P3 | A road path exists between every pair of TownApproach anchors |
| P4 | Player spawn resolves to dry ground above sea level |

## What must still be looked at by a human

Numbers cannot answer these, and the project's acceptance rule for terrain shape — show a bake,
not a diff, on three seeds — applies:

- Does the T1 massif read as *the* landmark from the shore?
- Do spurs read as spurs, or as a starfish?
- Does the coastline read as a coast, or as a wobbled circle?
- Do valleys read as places to walk through?
- Does a river read as belonging to its valley?
- Is the world legible in Lynch's terms — paths, edges, districts, nodes, landmarks?

**[FACT]** `WORLD_RELIEF_PRINCIPLES` §5 also warns that this changes how every island on every seed
looks, which makes it an art decision requiring the user's sign-off.

---

# Performance Considerations

No benchmarks are given, because there is no implementation. What follows is where cost will be and
where measurement must be taken.

**Likely expensive, in order of risk:**

1. **Feature-distance evaluation.** 1 050 625 cells × nearest-point queries over potentially hundreds
   of spur segments is the dominant cost and the one most likely to be naive. *Direction:* every
   feature has a bounded support, so rasterise per feature into its own footprint rather than
   iterating all features per cell; index segments in a uniform bucket grid — **[FACT]** the pattern
   `InlandWaterSampler` already uses at 64 m buckets for exactly this reason.
2. **Drainage.** **[FACT]** already budgeted at ~2.5 s total for 1 050 625 cells (fill < 1.5 s, flow
   < 0.3 s, network < 0.2 s, raster < 0.3 s) with ~18 MB working memory, and that budget was written
   against this same grid.
3. **Carve rasterisation.** Bounded by total river length × valley width.
4. **Terrain writes.** **[FACT]** measured in the migration spike: 4 × 513² `SetHeights` ≈ 33 ms.
   Not a concern.
5. **Anything that samples through `OverworldTerrainAccess` in bulk.** 3.1 µs/query. The generator
   must never do this — it owns the field.

**Where to measure:** `OverworldGenTiming.Log` sub-steps, matching the existing `[Gen] step=… ms=…`
convention — `terrain.structure`, `terrain.field`, `terrain.refine`, `hydrology.*` (already exist),
`terrain.carve`, `terrain.write`.

**Memory:** one `float[1025²]` ≈ 4.2 MB; three or four live at once ≈ 17 MB, plus the drainage
working set.

**Not to be optimised now:** nothing. Build the straightforward version, measure at Stage 4, then
decide. **[FACT]** the road planner's 261 s → 5.3 s recovery came from finding the actual defect
(69 M scene scans), not from optimising ahead of measurement.

---

# Terrain Representation

**Implemented 2026-09-13.** The external terrain pipeline is removed and there is one path:

```
TerrainGen (StructuralElevation) ──▶ OverworldTerrainField ──▶ OverworldTerrainBuilder ──▶ TerrainData ──▶ downstream
                                      (+ hydrology carve)       heights, weights, trees, details
```

- **No switch.** No `TerrainSource`, no provider interface, no fallback bake. The terrain bake reads
  TerrainGen unconditionally.
- **The generator stays pure.** TerrainGen does not reference Unity terrain types or the bake; the
  bake consumes its field and never changes it (the canonical seed's field hash is pinned by test).
- **Surface and vegetation** come from world-space rules in the bake, not from the generator.
- **Stage boundary.** The baked field is Stage 1 + Stage 2 today. Stage 3 onwards joins the field
  through `StructuralElevation` when those stages are unfrozen; the bake needs no change for it.

Full contract, tile model, rules, determinism and tests:
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).

---

# Open Questions

Unresolved on purpose. Each names how it gets answered.

1. **[OPEN] Will a structure-first field pass the isotropy bar?** The central hypothesis. There is no
   global noise field to band, so it should — but "should" is not evidence, and the current recipe
   also looked like it should. *Answered by:* G7 measured at Stage 5 on three seeds with
   `OverworldBatchDiagnostics`. **This is the design's single largest risk and its designated
   checkpoint.**
2. **[OPEN] Crustal cell count.** 40–70 is a guess about coastal variety. *Answered by:* Stage 1 dump,
   coastline-length ratio plus visual inspection.
3. **[OPEN] Smooth-max blend radius.** Too small creases, too large muddies. *Answered by:* Stage 3
   slope bands (G8); over-blending collapses the cliff share below 6 %.
4. **[OPEN] Structural basin budget.** 2–5 is a guess. Too few gives a lakeless world; too many gives
   pond speckle. *Answered by:* Stage 6 lake count and area distribution across five seeds.
5. **[OPEN] Is one carve pass plus validation sufficient on real relief?** Argued structurally in
   "Generation Order"; unmeasured. *Answered by:* H5/H6 at Stage 7. If it fails, the response is to fix
   the carve profile, not to add an iteration.
6. **[OPEN] Refinement amplitude and modulation curve.** *Answered by:* G7 and G8 together at Stage 5.
7. **[OPEN] `System.Random` and float determinism across runtimes/machines.** *Answered by:* D1 world
   hash compared across two machines and after a Unity upgrade.
8. **[OPEN] Will POI/settlement/road tuning survive?** They were tuned against ~115 m island-only
   relief, ~330 m with massifs, 26–52 % land per tile, `MinHeight01 = 0.12`. *Answered by:* P1–P4 at
   Stage 9. Whether to retune the generator or the consumers is a decision for that stage, not now.
9. **[OPEN] Does the estuary coastal profile need the drainage result, or is basin geometry enough?**
   The ordering refinement in "Generation Order" assumes it needs drainage. *Answered by:* Stage 7.
10. **Surface weights and vegetation.** Answered 2026-09-13 by the terrain bake; rule values are open authoring decisions (`OVERWORLD_TECH_DEBT.md` item 14).

---

# Risks

| Risk | Severity | Mitigation / early signal |
|---|---|---|
| **Ridge skeleton reads as a starfish** — the classic failure of explicit ridge networks | High | Non-crossing constraint, recursive depth limit, spur crest monotonicity, per-spur angle jitter. Visible at Stage 3, before any drainage work is built on it |
| **Isotropy target not met anyway** | High | Stage 5 checkpoint with an existing measurement; the design's only global field is the refinement, which can be reduced to zero to isolate the cause |
| **Smooth-max produces muddy shapes** — violates `WORLD_RELIEF_PRINCIPLES` §1 | Medium | Bounded blend radius; G8 cliff-share floor catches it numerically |
| **Lake speckle or no lakes at all** | Medium | Structural basins are explicit and budgeted; Stage 6 measures the distribution |
| **Downstream tuning breaks** (POI floor, road grades, settlement flatness) | Medium | P1–P4 at Stage 9; the generator is off by default until they pass |
| **Determinism drift** across Unity versions or machines | Medium | D1 world hash as a permanent test; `IWorldSeed` is the single replacement point |
| **Scope creep into a framework** | Medium | The non-goals list; one enum instead of a provider hierarchy; `WorldStructure` is data with no behaviour |
| **The design is large and could stall half-built** | Medium | Ten stages, each independently testable; each stage's output is inspectable on its own before the next is built |
| **Someone reintroduces per-tile generation** for streaming | Low, high cost | S1/S2 tests exist precisely to fail loudly |

---

# Recommended Implementation Stages

Derived from the design, not from a template. No stage requires the whole generator. *(The original
plan put every stage behind a default-off `TerrainSource` switch beside the external pipeline; that
pipeline is removed and the terrain bake reads TerrainGen directly.)*

**Stage 0 — primitives and the inspection harness.** `WorldHeightField`; deterministic seeded
primitives (blue-noise point sets, Voronoi partition, polyline distance with bucketing, profile curve
evaluation); world/field hashing; a headless dump tool producing height PNG, slope PNG, curvature PNG
and a stats line, reusing `OverworldBatchDiagnostics` conventions. *Tests:* D1, D3, D4, purity of the
point query. *Why first:* every later stage is judged by images and numbers, and building that last is
how the previous investigations became expensive.

**Stage 1 — landmass and coastline.** Crustal partition, continental growth, coast refinement, shelf
and coastal profiles. Output: a world that is land and sea and nothing else. *Tests:* G1, G2, D1–D3.
*Answers:* [OPEN] 2.

**Stage 2 — uplift belts.** Range spines from cell boundaries, crest/half-width/asymmetry profiles,
branching, coastal cliff selection. *Tests:* G6, plus G8's flat-share as a sanity floor. *Reversible:*
Stage 1 remains a valid output on its own.

**Stage 3 — spur skeleton and valleys.** Recursive spurs, non-crossing constraint, smooth-max
combination. *Tests:* G4, G8. *Answers:* [OPEN] 3. **First point at which the terrain can be judged as
terrain.**

**Stage 4 — terraces and structural basins.** T2 plateaus (flat top + wall + talus + ramp), T3
outcrops, structural basins. *Tests:* G3, G5, G8, plus the first `OverworldGenTiming` measurements.
*Why here:* basins must exist before drainage can find lakes. *(Split in the implementation: structural
basins shipped as Stage 3D/3E; the island's floor relief shipped as Stage 4A in 2026-09-16; T2 plateaus
are Stage 4B and not started.)*

**Stage 5 — refinement, and the isotropy checkpoint.** The single band-limited, structure-modulated
field. *(Superseded as the source of floor relief — see the 2026-09-16 status update. The isotropy gate
stays; the noise term is not scheduled.)* *Tests:* **G7** on three seeds, G8. *Answers:* [OPEN] 1 and 6. **This is a go/no-go gate: if
isotropy fails and cannot be recovered by reducing refinement, the ridge model itself needs revision
before anything downstream is built on it.**

**Stage 6 — drainage on the generated field.** `WorldHeightField → HydrologyGrid → HydrologyPlanner`.
No carve. Rivers and lakes exist as data and gizmos only. *Tests:* H1–H3 (already implemented as
`HydrologyValidator` rules), lake count/area distribution. *Answers:* [OPEN] 4. *Cheap because the
whole analysis already exists and is tested.*

**Stage 7 — carve.** Valley + channel + lake basin subtraction; estuary coastal profile; the
validation re-run. *(Water half delivered 2026-09-13 as the hydrology lattice carve; the structural
valley half is Stage 3F, still a preflight.)* *Tests:* H4–H8. *Answers:* [OPEN] 5 and 9. **The stage that the external-pipeline architecture
could not reach.**

**Stage 8 — TerrainData integration.** *Delivered early, 2026-09-13, for the Stage 1 + 2 field:*
`OverworldTerrainBake` writes one world lattice into the tile windows, with no switch. *Tests:* S1–S3, D1 end to end, D3 against the 700 m budget.

**Stage 9 — downstream validation.** Run cost field, POI, settlement, roads, water samplers and player
spawn against generator terrain; compare against the current world's acceptance ranges. *Tests:*
P1–P4, G8, and the full 309-test EditMode suite as a regression gate. *Answers:* [OPEN] 8. *Output:* a
decision on whether to retune the generator or the consumers.

**Stage 10 — surface and vegetation.** *Delivered 2026-09-13* by the terrain bake: world-space surface
rules, tree instances and ground cover, canonical layer textures moved to `Assets/Art/Terrain/SurfaceLayers/`.
What remains is art review of the authoring defaults.

---

## Related Documents

- [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) — how the field becomes Unity terrain
- [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) — the quality specification, engine-independent
- [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) — the drainage algorithm this design reuses, and the carve architecture it replaces
- [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) — road defects, terrain authoring defaults
- [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) — pipeline steps, seed forks, world scale
