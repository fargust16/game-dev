# World Generation Pipeline — Target Architecture

**Status:** Draft
**Date:** 2026-08-31
**Applies to:** Track W (World Gen), all waves after Overworld V0
**Related:** [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) (terrain bake), [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md) (current iteration), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

---

## Purpose

The user's full target shape for world generation, distilled into project terms and cross-referenced to the System Cards in `17`. **Not a build order** — `OVERWORLD_V0_PLAN.md` and the iteration list in §3 below govern what actually gets implemented and when. This doc exists so every later wave has a consistent picture of where it's heading, instead of each terrain feature being invented ad hoc.

Quality bar (the user's framing, kept verbatim because it's the right test):

> Не спрашивать "насколько красиво выглядит отдельный кусок terrain?" А проверять: "создаёт ли генератор естественную географию, которая сама создаёт интересные места для исследования?"

A traversal should read as *forest → hills → valley → river → bridge → village → open plain → old forest → mountain pass → lake → city*, not as undifferentiated noise.

---

## 1. Seed hierarchy

One `WorldSeed` forks independent named streams — changing one must not change another (`WorldSeed.Fork("vegetation")` changing must not move the mountains):

```
WorldSeed
 ├── terrain.crust / .crust.*   (TerrainGen Stage 1: crustal partition, islets, edges)
 ├── terrain.coast.{label}.{n}  (TerrainGen Stage 1: coastline attempts)
 ├── terrain.macro.*            (TerrainGen Stage 2A: plates, drift, stress)
 ├── terrain.range.*            (TerrainGen Stage 2A: uplift belts and branches)
 ├── terrain.massif.{i}         (TerrainGen Stage 2B: per-massif shape)
 ├── terrain.surface.shore      (terrain bake: shore-line noise)
 ├── terrain.surface.rock       (terrain bake: massif rock-line noise)
 ├── terrain.vegetation.groves  (terrain bake: grove mask)
 ├── terrain.vegetation.species (terrain bake: lowland pine mask)
 ├── terrain.vegetation.trees   (terrain bake: per-cell tree jitter, acceptance, variant, scale)
 ├── terrain.vegetation.grass   (terrain bake: per-cell ground-cover patchiness)
 ├── hydrology                  (river network; hydrology.sample / hydrology.carve for the carve)
 ├── poi.{kind}.{i}             (per-marker tie-break; adding a kind must not move earlier markers)
 ├── poi-visual                 (which prop / building prefab a marker receives)
 ├── settlement                 (W.5 fabric pose / plots)
 ├── player.spawn               (which TownApproach receives the player)
 └── roads                      (no RNG — LandscapeCostField + A* are deterministic)
```

Trees and ground cover are placed **by the terrain bake**, not by a separate pipeline step. Their
streams are `TerrainHash` keys forked from the world seed and evaluated statelessly per world cell, so
a cell's tree does not depend on which tile or in which order it is evaluated — see
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §7.

Inland hydrology is generated as of 2026-09-03 through the `hydrology` seed stream — see
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md), and
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md) §
Hydrology / River Generation for why the first attempt was removed rather than restored.

`W.1 WorldSeed` (`unity/Assets/Scripts/World/Seed/`) implements fork-by-name and `Fork(streamId, a, b)` for per-object isolation. POI uses `poi.{kind}.{index}` so inserting a new kind does not reshuffle existing TownApproach positions. Skipping a pipeline step must not consume or advance another step's fork. Aggregate output lives in `WorldDefinition` (see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md)). The TerrainGen stages, `OverworldTerrainBuilder`, `TerrainSurfaceClassifier` and `OverworldWorldPlan` are the source of truth for the fork names above — update this list from the code, not the other way round.

### World scale (single table)

All natural objects share one metre relationship via `GameDev.World.WorldScale` (character ≈ 1.8 m as the base unit):

| Layer | Target metres |
|---|---|
| Character | 1.7–1.8 height, ~0.3 radius |
| Trees | mature 12–25 (hero up to ~40), young 8–14 |
| Shrubs | 0.5–3 |
| Grass / flowers | short 0.05–0.5, tall 0.3–1, flowers 0.1–1 |
| Road corridor | main 4 m + shoulder 2.5 + blend 8; maxGrade 0.12; maxCut 1.5 / maxFill 1.0 |
| World extent | **2800 × 2800** (2×2 tiles) |
| Tile / chunk | 1400 / 64 |
| Terrain height budget | **700** |
| Sea level (ocean plane) | `WorldScale.SeaLevelMeters` = **24** |
| Terrain heightmap / alphamap | **513 / 512 per tile** — 2.73 m per height vertex, 16-bit heights, 8-bit splat weights |

Do not scale prefabs independently — placers read `WorldScale`. Geometric pipeline: [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). Generation timing logs as `[Gen] step=... ms=...` from `OverworldGenTiming`.

The terrain is one world-spanning lattice sampled from TerrainGen, so tile seams are exact by
construction and a terrain feature is limited by the 2.73 m vertex spacing, not by a separate canvas.
Detail: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §2.

---

## 2. Pipeline stages → System Cards

| # | Stage | Produces | Maps to (17) | V0 status |
|---|-------|----------|---------------|-----------|
| 1 | World Layout | Continents/islands/coastline | New — not in `17` yet | **Island-in-ocean from TerrainGen Stage 1** — crustal partition, coastline and island floor (`Stage1LandmassGenerator`); WaterWorks `WorldWater` at `SeaLevelMeters`. `IslandNaturalGeography` / `IslandOccupancy` are **deleted**. Multi-tile continents still deferred. |
| 2 | Macro Terrain | Mountains/plains/valleys/plateaus | `W.2 Terrain/Biome` | **TerrainGen Stage 2** — macro structure and massif uplift, evaluated as `StructuralElevation` by `TerrainAuthorityField` and baked into Unity `TerrainData` ([PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md), [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md)). MapMagic recipe **retired**. Height sample = `Terrain.SampleHeight` / `OverworldTerrainSampler`. Tile 1400×1400; height budget **700 m**. |
| 3 | Hydrology | **Analysis ships 2026-09-03; carve applied by the terrain bake, step still off by default.** Priority-flood depression fill -> D8 -> flow accumulation over the baked relief; rivers, tributaries, lakes and waterfall candidates | `Hydrology` step (**off by default**) | Two-pass: the analysis reads the finished relief of bake A, then bake B lowers the terrain lattice by `HydrologyCarveField` (`HydrologyBake.ApplyPendingCarve`) before surface and vegetation are derived. The carve is one-way and never read back into TerrainGen. Ocean stays one WaterWorks WorldWater. See [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) and [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §6. |
| 4 | Biomes | Meadow/Forest/Swamp/Highland/Mountain from **height + slope + moisture + temperature + distance-to-water**, not height alone | `W.2 Terrain/Biome` (`IBiomeSampler`) | **Height + slope + moisture** via `OverworldTerrainSampler` + `BiomeClassifier`. Classification is **semantic only** — the visible surface comes from the terrain bake's surface rules, not from this. Moisture is currently an unseeded Perlin field (`OverworldTerrainSampler.SampleMoisture01` discards its `moistureSeed`), so it is identical across seeds; temperature / distance-to-water deferred. |
| 5 | Terrain Shaping | Cliffs, terraces, local erosion detail | `W.2`/`W.4` | **TerrainGen only.** Stage 3 (ridges, spurs, valleys, basins) is designed and frozen but not yet part of the baked field; there is no erosion pass. The terrain bake adds no relief. MapMagic `Erosion200`/`Terrace200` recipe retired; StampIT is out of the pipeline. |
| 6 | Terrain Material | Grass/sand/soil/cliff/rock from world-space morphology rules | `W.4` dressing / new card | Weights from `TerrainSurfaceRules` (world metres, degrees, square metres) written by `OverworldTerrainBuilder` with `SetAlphamaps`; material = URP **TerrainLit** over the five canonical layers (`OverworldTerrainLayers`). **No MicroSplat.** Spec: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §3, [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md). |
| 7 | Vegetation | Trees and ground cover from `TerrainVegetationRules`, written as Unity tree instances and detail layers | `W.2`/`W.4` dressing | **Built in the terrain bake** on all four tiles; densities and species split are authoring defaults awaiting art review. There is no GameObject scatter pass — `VegetationScatterMenu` and its CPU mask cluster are deleted. Do not add a second placer. |
| 8 | World Features | Ruins/caves/camps/shrines as POI markers | `W.4 POI Placement` | Started — `PoiKind` list already includes ruin/camp/shrine/town approach/cave mouth |
| 9 | Roads | Cost-field path + vertical profile + corridor conform (not canyon cut) | `W.5 Settlement` | **LandscapePlanner** — `LandscapeCostField` (slope³ + earthwork), hierarchical MST, `RoadCorridorDeformer`, see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) |
| 10 | Settlements | Location choice (flat + water + road access) → `W.5` | `W.5 Settlement` | **S6–S8:** TownApproach dry fabric + District Mix + street props (`SETTLEMENT_PROPERTY_V0.md`) |
| 11 | Districts / Plots | Multi-district parcels + GardenSquare open space + street spine + street dress | `W.5 Settlement` | **Lab through S8** — `CreateDistrictMix` + `StreetPropPlacer`; not OrganicTown |
| 12 | Buildings | Building DNA / Piece-Based Assembly | `W.6 Building/Shell`, [15](../../../15%20Procedural%20Building%20Generation.md) | Deferred |
| 13 | Chunking / Streaming | Deterministic, seamless, LOD | `W.3 Chunk Streaming` | `ChunkCoord` / `OverworldTileChunks` math done at 64 m; the streamer itself is not started |

Rows 1, 3, 6, 9 don't have a System Card yet in `17` — add one when that wave actually starts, following the existing card template. Don't pre-author empty cards now.

---

## 3. Principles worth keeping verbatim

- **Never one noise field.** TerrainGen builds relief from named structural stages — crust and coastline, macro plates and uplift belts, massifs — not from a single noise call or a radial pancake. Extend with named concerns.
- **TerrainGen decides, the bake writes, Unity renders.** Relief is TerrainGen's; the terrain bake is the only writer of heights, surface weights, trees and ground cover, and it adds no relief of its own. Nothing reads `TerrainData` back into TerrainGen. If a surface is wrong, a rule value on `TerrainSurfaceRules` / `TerrainVegetationRules` is wrong — do not add a second writer or a post-bake fix-up.
- **World units only.** Surface and vegetation thresholds are metres, degrees and square metres, each labelled measured or authoring default. No normalised per-biome heights.
- **Terrain masks are data, not throwaway locals.** Height/slope/moisture read through `OverworldTerrainSampler` (Unity Terrain + classifier) so biome/material/vegetation/road rules share one surface.
- **Rivers reshape terrain.** Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified terrain is not hydrology. The bar still stands: the terrain bake lowers the lattice by the planned bed before the surface is derived, and the water surface is drawn from the same bed. The step ships disabled until it is accepted in review (see [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md)).
- **Biome is not just a texture switch.** It should also drive terrain shape tendencies, vegetation density, and (later) POI placement rules — see `W.2`'s relation to `W.4`/`W.6` dressing.
- **Roads are pathfinding, not lines.** Lowest-cost path respecting slope, rivers (bridges), and mountains (passes) — never straight through a mountain. Decided 2026-08-31: **a mountain is traversable terrain priced high, never a hard obstacle** — «never straight through» is about the direct expensive climb, not about the ground being forbidden. Full wording in [90 Glossary](../../../90%20Glossary.md) § Road Passability.
- **Settlements are shaped by terrain**, not a flat square with houses dropped on it — matches `W.5`'s "layout intents: road hints, plot footprints, district tags" wording already in `17`.
- **Chunk generation must be a pure function of world coordinates** (`Height = f(worldX, worldZ, seed)`), never local-to-chunk coordinates. Already how `ChunkCoord`, the terrain bake's rules and `OverworldTerrainSampler` / `Terrain.SampleHeight` world-space queries are built — keep it that way for every future sampler. The same rule applies to **tiles**: a world-dressing pass must resolve the tile that contains a point, never assume tile (0,0).
- **Generation is not persistence.** Regenerable structure (heightmap, biome bands, vegetation placement) is never saved; only player-caused deltas (felled tree, built structure) are. Matches `X.3 Save System`'s existing "never dump regenerable chunk meshes" rule.
- **Seed forks still via `WorldSeed`.** Every terrain concern draws from its own named fork (`terrain.coast.*`, `terrain.massif.{i}`, `terrain.vegetation.*`, …) — changing one concern must not move another. Never `UnityEngine.Random`.

---

## 4. Development order (12 iterations, world-gen only — Locomotion/Interior/Arena waves from `14` interleave separately)

| It. | Deliverable | Status |
|-----|-------------|--------|
| 01 | Heightmap only (large-scale terrain, no biomes) | **TerrainGen relief baked into Unity `TerrainData`** — `TerrainAuthorityField` → `OverworldTerrainField` → `OverworldTerrainBuilder`. Height queries: `OverworldTerrainSampler` → `Terrain.SampleHeight`. See [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md). |
| 02 | Landmass (ocean/islands/coastline) | **TerrainGen Stage 1 landmass + WaterWorks WorldWater** at `SeaLevelMeters`. Multi-tile continents still deferred. |
| 03 | Terrain layers (large/medium/small feature scales) | **TerrainGen stages only**, bounded by the 2.73 m heightmap vertex spacing. Stage 3 detail is designed but not yet in the baked field. Small-scale detail must not break biome sampling smoothness. |
| 04 | Biomes (Meadow/Forest/Mountain/Swamp/Coast) | **`OverworldTerrainSampler` + `BiomeClassifier`** (height+slope+moisture). Temperature / distance-to-water deferred. |
| 05 | Water | **Ocean shipping; inland network built but disabled.** Rivers, tributaries and terrain-derived lakes; every system reaches the sea by construction and by explicit validation. Held back for review of the carved result. |
| 06 | Terrain materials (grass/sand/soil/cliff/rock) | Five canonical `TerrainLayer` assets under `Assets/Settings/TerrainLayers/Overworld/`, assigned and painted by the terrain bake and rendered by URP TerrainLit. The bake owns the layer array and the alphamaps. No snow layer in V0. Spec: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) |
| 07 | Vegetation (trees/bushes/grass/rocks) | **Trees and ground cover come from the terrain bake** — `TerrainVegetationRules` → `OverworldTerrainBuilder.PlaceTrees` (Unity tree instances: pine and fruit species with tinted variants) and `PlaceGrass` (one detail layer), content from `OverworldVegetationAssets`. Shrubs and rock props are not placed. The CPU scatter cluster (`VegetationScatterMenu`, `GrassMask`, `GrassDistribution`, `GrassField`, `VegetationEcology`, `TreePlacement`) is **deleted** — do not reintroduce a second placer. Densities are authoring defaults. Diagnostics: `OverworldTerrainValidation.RunBatch`. |
| 08 | POIs (caves/ruins/resource areas) | **Profile-driven placement** — `PoiPlacementEngine` + `PoiPlacementCatalog` / per-kind `PoiPlacementProfile` (hard/soft/relation/footprint). Demo quota still from `PoiDemoScenario`, which reads `WorldGenSettings` (**5** TownApproach, **9** Shrine, **9** Camp, **9** Landmark). TownApproach scores a circular footprint (slope / flatness / height variance); Shrine / Camp / Landmark use isolation, forest-edge, and scenic cues. `StubPoiPlacer` remains the `IPoiPlacer` facade. `PoiMarkerMenu` still spawns props/buildings. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) and [17](../../../17%20Game%20Systems%20Architecture.md) W.4. |
| 09 | Roads (graph + terrain cost + mountain passes) | **LandscapePlanner** — `LandscapeCostField` nonlinear cost (slope³, excavation^2.5) + existing-road paint discount; growing Prim Primary network among TownApproach (snap to accepted corridors) then Secondary attach to whole network; `RoadTier` Primary/Secondary/Local (Local deferred); `EnsureMinimumConnectivity` (degree ≥ 1); `RoadVerticalProfile` + `RoadCorridorDeformer`. Water now reaches road costing through `CompositeWaterSampler(ocean, InlandWaterSampler)`, so rivers are real crossings; `CanBridgeSpan` is still `=> false`, so bridging itself is unchanged. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). |
| 10 | Settlements (location → type → districts → road network) | Property lab through S8 (`SETTLEMENT_PROPERTY_V0.md`: District Mix + GardenSquare + street props + overworld dry place); terraces / TownDress deferred. **Linked to the road network** through `OverworldSettlementAnchor`: `SettlementApproachBinder.TryCreateDryFabricNearApproach` can move the fabric up to ~192 m from its raw TownApproach marker, so `SettlementOverworldMenu` records where the fabric actually landed and `RoadNetworkMenu` routes to that position instead. Requires Settlement to run **before** Roads; running `Build Road Network` standalone falls back to the raw marker. |
| 11 | Buildings (Building DNA / Piece-Based Assembly) | Not started, deferred by `15` |
| 12 | Streaming (chunks, async, LOD, unload) | `ChunkCoord` / `OverworldTileChunks` math done; async/LOD not started |

**Corridor conform (not canyon cut).** Terrain lerps toward the road profile across road / shoulder / outer blend zones. Roads avoid deep cuts via cost; deform only gently conforms. Detours up to ~20 % length (`RoadDetourTolerance`) beat short steep cuts.

**Road and settlement layers are appended after the terrain bake's.** They may add their own `TerrainLayer`s downstream of the bake, but must never reorder or repaint canonical biome indices 0–4, and the tile must stay within URP TerrainLit's ceiling of 8 layers.

**Where we actually are:** TerrainGen's island relief baked into four Unity Terrain tiles (plus one WaterWorks `WorldWater` plane) with URP TerrainLit over the five canonical layers and all vegetation placed by the terrain bake; POI / settlement / roads on the LandscapePlanner rails. Inland hydrology is generated and validated, and its carve is applied by the terrain bake — the step is off by default. Multi-tile continents remain open.

**One-button pipeline:** `WorldGeneratorWindow` / `OverworldPipeline.GenerateAll` runs the Editor pipeline steps below (skip mask from `WorldGenSettings`). Part-Gen menu items call the same step methods. **Whenever a future iteration adds a new Overworld menu step, add it to `OverworldPipelineStepId` + `OverworldPipeline.OrderedSteps` and give it a `WorldGenSettings` skip flag** — a step that is not in that enum cannot be skipped, cleared or cascaded.

---

## 5. Editor pipeline steps (executable build order)

Target architecture in §2 is the long-term map. **This section is the Editor build order** — each step is a full Generate/Clear procedure that can be skipped in Generate All via `WorldGenSettings` step flags (flags are not seeds: same seed + Mountains off → stable map without ridges).

| Step | Artifact | Fork(s) | Clear | Cascade on regenerate | Skip flag |
|------|----------|---------|-------|----------------------|-----------|
| Surface | One terrain bake: height, 5 layer weights, trees and ground cover on all four tiles | `terrain.crust*`, `terrain.coast.*`, `terrain.macro.*`, `terrain.range.*`, `terrain.surface.*`, `terrain.vegetation.*` | Destroy tiles; re-ensure ocean | Markers, settlements, roads, player | `RunSurface` |
| Mountains | Same terrain bake with TerrainGen's massif uplift included | `terrain.massif.{i}` | Re-bake Surface without massif uplift | Same as Surface | `RunMountains` |
| Markers | POI plan + marker visuals | `poi.{kind}.{i}`, `poi-visual` | POI root | Settlements, roads | `RunMarkers` |
| Settlements | TownApproach fabric + `OverworldSettlementAnchor` | `settlement` | Settlement root | Roads | `RunSettlements` |
| Roads | Network + `RoadCorridorDeformer` (corridor conform + surface paint) | none (deterministic) | Road root | — | `RunRoads` — **off by default**, see [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) item 0 |
| Player | Character at a TownApproach | `player.spawn` | Player object | — | `RunPlayer` |

**Surface and Mountains are not additive prefabs.** They share one terrain bake, selected by `TerrainBakeFlags.IncludeMountains`. Changing either **must cascade-clear** downstream placement — stale content on a new heightmap is worse than an empty tile. MapMagic graphs are retired. Re-ensure the ocean plane after every terrain clear.

**Vegetation is not a pipeline step.** Trees and ground cover are written by the terrain bake during Surface. There is no Trees or Undergrowth step.

**Full Generate All order:** Surface (+Mountains) → Markers → Settlements → Roads → Ensure Ocean → Marker visuals → Player. Settlement must precede Roads so the road network can route to `OverworldSettlementAnchor` rather than to the raw POI marker.

**Roads are currently skipped**, so a default Generate All is ocean + terrain + vegetation + POI + settlement + player. The order above is unchanged; only the flag is off.

**Menu:** `GameDev/Overworld/Generate All`, `Clear All`, `Part-Gen/<Step>/Generate|Clear`, `World Generator` (step checkboxes + knobs). Debug menus stay under `Debug/`.

---

## Related Documents

- [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md)
- [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)
- [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)
- [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)
