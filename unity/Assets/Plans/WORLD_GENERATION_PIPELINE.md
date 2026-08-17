# World Generation Pipeline — Target Architecture

**Status:** Draft
**Date:** 2026-08-15
**Applies to:** Track W (World Gen), all waves after Overworld V0
**Related:** [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md) (current iteration), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

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
 ├── terrain.*    (one named sub-fork per concern - graph/macro/ridge/hills/terrace/
 │                 highland-terrace/masses/detail - heightmap macro/medium/detail shaping)
 ├── biome        (temperature/humidity/moisture bands)
 ├── lake.place   (lake candidate site selection)
 ├── lake.{i}     (per-lake shape / orientation / depth — Fork(stream, index))
 ├── poi.{kind}.{i} (per-marker tie-break; adding a kind must not move earlier markers)
 ├── vegetation   (tree scatter only)
 ├── vegetation.shrubs (shrubs / flowers / rocks — independent of trees)
 ├── grass        (GPU grass coverage bake — independent of vegetation)
 ├── decoration   (props / scatter reserved; no RNG yet)
 ├── settlement   (W.5 fabric pose / plots)
 ├── player.spawn (which TownApproach receives the player)
 └── roads       (no RNG — LandscapeCostField + A* are deterministic)
```

No `river` fork: rivers are traced deterministically from the already-generated height field
(flow accumulation has no randomness of its own), so there's nothing to seed independently.

`W.1 WorldSeed` (`unity/Assets/Scripts/World/Seed/`) implements fork-by-name and `Fork(streamId, a, b)` for per-object isolation. Terrain concerns each have their own named fork (`terrain.graph`, `terrain.macro`, …). Lakes use `lake.place` + `lake.{index}`; POI uses `poi.{kind}.{index}` so inserting a new kind does not reshuffle existing TownApproach positions. Skipping a pipeline step must not consume or advance another step's fork. Aggregate output lives in `WorldDefinition` (see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md)). Rivers (`FlowNetwork`/`RiverTracer`) need no seed — pure function of height.

### World scale (single table)

All natural objects share one metre relationship via `GameDev.World.WorldScale` (character ≈ 1.8 m as the base unit):

| Layer | Target metres |
|---|---|
| Character | 1.7–1.8 height, ~0.3 radius |
| Trees | mature 12–25 (hero up to ~40), young 8–14 |
| Shrubs | 0.5–3 |
| Grass / flowers | short 0.05–0.5, tall 0.3–1, flowers 0.1–1 |
| Small lakes (V0 tile) | radius 95–155 → diameter ~190–310 |
| Lake basin dig | 36–48 analytic bowl (LandscapePlanner); dig-only; basin SDF expanded vs water inset |
| Road corridor | main 4 m + shoulder 2.5 + blend 8; maxGrade 0.12; maxCut 1.5 / maxFill 1.0 |
| Tile / chunk | 1400 / 64 |
| Terrain height budget | ~220 |
| Sea level (ocean plane) | `WorldScale.SeaLevelMeters` (~32) |

Do not scale prefabs independently — placers read `WorldScale`. Lake shores use analytic `LakeSilhouette` SDF (trig wobble), not the 3 m occupancy raster. Geometric pipeline: [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). Generation timing logs as `[Gen] step=... ms=...` from `OverworldGenTiming`.

---

## 2. Pipeline stages → System Cards

| # | Stage | Produces | Maps to (17) | V0 status |
|---|-------|----------|---------------|-----------|
| 1 | World Layout | Continents/islands/coastline | New — not in `17` yet | **Natural island-in-ocean** — Vista `NaturalIsland` graph + `IslandNaturalGeography` + slim `IslandOccupancy` + WaterWorks `WorldWater` at `SeaLevelMeters`. See [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md). Multi-tile continents still deferred. |
| 2 | Macro Terrain | Mountains/plains/valleys/plateaus | `W.2 Terrain/Biome` | **Vista Personal** `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph` (Vista reference biome architecture; see `VISTA_OVERWORLD_REFERENCE.md`). MapMagic recipe **retired**. Height sample = `Terrain.SampleHeight` / `VistaTerrainSampler`. Tile 1400×1400; height budget ~220 m. |
| 3 | Hydrology | **Deferred** — V0 ocean plane only; inland lake/river gen removed 2026-08-15 | W.2 / W.2b | Ocean = one WaterWorks WorldWater. Future inland hydro = redesign. |
| 4 | Biomes | Meadow/Forest/Swamp/Highland/Mountain from **height + slope + moisture + temperature + distance-to-water**, not height alone | `W.2 Terrain/Biome` (`IBiomeSampler`) | **Height + slope + moisture** via `VistaTerrainSampler` + `BiomeClassifier` (area-fraction bands still apply). Moisture noise forked from `WorldSeed.Fork("biome")`. Temperature / distance-to-water deferred. |
| 5 | Terrain Shaping | Cliffs, terraces, local erosion detail | `W.2`/`W.4` | Handled inside **Vista** biome graphs / stamps (StampIT for detail); MapMagic `Erosion200`/`Terrace200` recipe retired. |
| 6 | Terrain Material | Grass/sand/soil/cliff/rock from the terrain graphs' morphology masks | `W.4` dressing / new card | Weights from the graphs' `Weight Blend` → `TEXTURE OUTPUT`; material = URP **TerrainLit** over the five canonical layers (`OverworldTerrainLayers`). **No C# painting, no MicroSplat.** Custom `GameDev/Overworld/Terrain` **deleted**. Spec: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md). |
| 7 | Vegetation | Canopy + grass from the graphs' `TREE OUTPUT` / `DETAIL DENSITY OUTPUT`; shrubs/flowers/rocks Prefab scatter | `W.2`/`W.4` dressing | Done for V0 — forest masses and grass placed by Vista, trunk colliders, 12–25 m trees (see Iteration 07) |
| 8 | World Features | Ruins/caves/camps/shrines as POI markers | `W.4 POI Placement` | Started — `PoiKind` list already includes ruin/camp/shrine/town approach/cave mouth |
| 9 | Roads | Cost-field path + vertical profile + corridor conform (not canyon cut) | `W.5 Settlement` | **LandscapePlanner** — `LandscapeCostField` (slope³ + earthwork), hierarchical MST, `RoadCorridorDeformer`, see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) |
| 10 | Settlements | Location choice (flat + water + road access) → `W.5` | `W.5 Settlement` | **S6–S8:** TownApproach dry fabric + District Mix + street props (`SETTLEMENT_PROPERTY_V0.md`) |
| 11 | Districts / Plots | Multi-district parcels + GardenSquare open space + street spine + street dress | `W.5 Settlement` | **Lab through S8** — `CreateDistrictMix` + `StreetPropPlacer`; not OrganicTown |
| 12 | Buildings | Building DNA / Piece-Based Assembly | `W.6 Building/Shell`, [15](../../../15%20Procedural%20Building%20Generation.md) | Deferred |
| 13 | Chunking / Streaming | Deterministic, seamless, LOD | `W.3 Chunk Streaming` | `ChunkCoord` math done (`W0.2`); streamer itself is `W0.5` |

Rows 1, 3, 6, 9 don't have a System Card yet in `17` — add one when that wave actually starts, following the existing card template. Don't pre-author empty cards now.

---

## 3. Principles worth keeping verbatim

- **Never one noise field.** Vista + `IslandNaturalGeography` compose macro/medium/detail; slim `IslandOccupancy` only enforces submerged border. Extend with named concerns, don't collapse to a single Perlin call or radial pancake.
- **Terrain masks are data, not throwaway locals.** Height/slope/moisture read through `VistaTerrainSampler` (Unity Terrain + classifier) so biome/material/vegetation/road rules share one surface.
- **Rivers reshape terrain.** Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified terrain is not hydrology.
- **Biome is not just a texture switch.** It should also drive terrain shape tendencies, vegetation density, and (later) POI placement rules — see `W.2`'s relation to `W.4`/`W.6` dressing.
- **Roads are pathfinding, not lines.** Lowest-cost path respecting slope, rivers (bridges), and mountains (passes) — never straight through a mountain.
- **Settlements are shaped by terrain**, not a flat square with houses dropped on it — matches `W.5`'s "layout intents: road hints, plot footprints, district tags" wording already in `17`.
- **Chunk generation must be a pure function of world coordinates** (`Height = f(worldX, worldZ, seed)`), never local-to-chunk coordinates. Already how `ChunkCoord` (`W0.2`) and `VistaTerrainSampler` / `Terrain.SampleHeight` world-space queries are built — keep it that way for every future sampler.
- **Generation is not persistence.** Regenerable structure (heightmap, biome bands, vegetation placement) is never saved; only player-caused deltas (felled tree, built structure) are. Matches `X.3 Save System`'s existing "never dump regenerable chunk meshes" rule.
- **Seed forks still via `WorldSeed`.** Vista `LocalProceduralBiome.seed` is set from named forks (`terrain.masses`, etc.) — changing vegetation must not move mountains.

---

## 4. Development order (12 iterations, world-gen only — Locomotion/Interior/Arena waves from `14` interleave separately)

| It. | Deliverable | Status |
|-----|-------------|--------|
| 01 | Heightmap only (large-scale terrain, no biomes) | **Natural island geology** — `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph` under `OverworldLpbBake`. Height queries: `VistaTerrainSampler` → `Terrain.SampleHeight`. See [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md). |
| 02 | Landmass (ocean/islands/coastline) | **Warped island mask + WaterWorks WorldWater** at `SeaLevelMeters`. Multi-tile continents still deferred. |
| 03 | Terrain layers (large/medium/small feature scales) | **Vista graph + StampIT** for detail stamps. Small-scale paint/detail must not break biome sampling smoothness. |
| 04 | Biomes (Meadow/Forest/Mountain/Swamp/Coast) | **`VistaTerrainSampler` + `BiomeClassifier`** (height+slope+moisture). Temperature / distance-to-water deferred. |
| 05 | Water | **Ocean only (V0).** Inland lakes/rivers deferred. |
| 06 | Terrain materials (grass/sand/soil/cliff/rock) | Five canonical `TerrainLayer` assets under `Assets/Settings/TerrainLayers/Overworld/`, bound by both terrain graphs and rendered by URP TerrainLit. Vista owns the layer array and the alphamaps. No snow layer in V0. Spec: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) |
| 07 | Vegetation (trees/bushes/grass/flowers/rocks) | **Canopy and grass come from the terrain graphs** — `GreenAreaMask` → `Levels`/`GeometryMask` → `Scatter` → `TREE OUTPUT` / `DETAIL DENSITY OUTPUT`, with `Forest_TreeTemplate` / `Meadow_DetailTemplate` content. Shrubs/flowers/rocks stay Prefab scatter driven by `VegetationEcology` macro/meso fields, with settlement fabric on `OverworldSettlementAnchor` for street/pad exclusion (`GrassExclusion`). The old CPU grass carpet (`GrassMask` → `GrassDistribution` → `GrassField`) is **deleted**. Pipeline vegetation **last**. EditMode: `VegetationEcologyTests`, `VegetationClearanceTests`, `GrassMaskTests`, `OverworldVisualDirectionTests`, `TreePlacementTests` |
| 08 | POIs (caves/ruins/resource areas) | **Profile-driven placement** — `PoiPlacementEngine` + `PoiPlacementCatalog` / per-kind `PoiPlacementProfile` (hard/soft/relation/footprint). Demo quota still from `PoiDemoScenario` (5 TownApproach, 3 Shrine, 10 Camp, 5 Landmark). TownApproach scores a circular footprint (slope / flatness / height variance); Shrine / Camp / Landmark use isolation, forest-edge, and scenic cues. `StubPoiPlacer` remains the `IPoiPlacer` facade. `PoiMarkerMenu` still spawns props/buildings. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) and [17](../../../17%20Game%20Systems%20Architecture.md) W.4. |
| 09 | Roads (graph + terrain cost + river crossings + mountain passes) | **LandscapePlanner** — `LandscapeCostField` nonlinear cost (slope³, excavation^2.5) + existing-road paint discount; growing Prim Primary network among TownApproach (snap to accepted corridors) then Secondary attach to whole network; `RoadTier` Primary/Secondary/Local (Local deferred); `EnsureMinimumConnectivity` (degree ≥ 1); `RoadVerticalProfile` + `RoadCorridorDeformer`; bridges rivers only. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). |

**Corridor conform (not canyon cut).** Terrain lerps toward the road profile across road / shoulder / outer blend zones. Roads avoid deep cuts via cost; deform only gently conforms.

**Natural preference.** Detours up to ~20% length (`RoadDetourTolerance`) beat short steep cuts. |
| 10 | Settlements (location → type → districts → road network) | Property lab through S8 (`SETTLEMENT_PROPERTY_V0.md`: District Mix + GardenSquare + street props + overworld dry place); terraces / TownDress deferred. **Linked to the road network** — `SettlementApproachBinder.TryCreateDryFabricNearApproach`'s dry-ground search can move the fabric up to ~192m (`DrySearchRingMax * DrySearchStepMeters`) from its raw TownApproach POI marker, which previously left `RoadNetworkMenu` routing to a coordinate the settlement no longer occupied. `SettlementOverworldMenu` now drops an `OverworldSettlementAnchor` (`Assets/Scripts/World/Settlement/`) recording the fabric's real landing position, and `RoadNetworkMenu` substitutes that position for the matching TownApproach marker when building its node list, so the road actually reaches the town. Only works when Settlement placement runs before Roads - `OverworldPipelineMenu.RunRemainingSteps` reordered accordingly (Settlement now comes before Roads); running `Build Road Network` standalone without an existing settlement still falls back to the raw marker position, unchanged from before |
| 11 | Buildings (Building DNA / Piece-Based Assembly) | Not started, deferred by `15` |
| 12 | Streaming (chunks, async, LOD, unload) | `ChunkCoord` math done; async/LOD not started |

**Where we actually are:** Vista-native island generator (two LPBs from biome templates + WaterWorks `WorldWater`) with URP TerrainLit over the five canonical layers; hydrology / roads / POI / settlement / prefab vegetation still on the LandscapePlanner rails. Multi-tile continents remain open.

**One-button pipeline:** `WorldGeneratorWindow` / `OverworldPipeline.GenerateAll` runs the Editor pipeline steps below (skip mask from `WorldGenSettings`). Part-Gen menu items call the same step methods. **Whenever a future iteration adds a new Overworld menu step, register it as an `IOverworldStep`.**

---

## 5. Editor pipeline steps (executable build order)

Target architecture in §2 is the long-term map. **This section is the Editor build order** — each step is a full Generate/Clear procedure that can be skipped in Generate All via `WorldGenSettings` step flags (flags are not seeds: same seed + Mountains off → stable map without ridges).

| Step | Artifact | Fork(s) | Clear | Cascade on regenerate | Skip flag |
|------|----------|---------|-------|----------------------|-----------|
| Surface | Vista.Generate + `IslandOccupancy` (height on Unity Terrain) | `terrain.*` without masses/ridge/terrace | Rebuild Vista without mountains; clears downstream | Lakes, markers, settlements, roads, vegetation, grass, player | `RunSurface` |
| Zones | Terrain alphamap / biome paint | (deterministic from height+moisture) | Neutral ground layers | None (paint only) | `RunZones` |
| Mountains | Same Vista bake; massif LPBs spawned from `LPB_Mountain.prefab` with Raise blending | `terrain.mountain.*` | Rebuild Surface without mountain LPBs | Same as Surface | `RunMountains` |
| Markers | POI plan + visuals | `poi.{kind}.{i}` | POI root | Settlements, roads, veg clearance | `RunMarkers` |
| Settlements | TownApproach fabric + anchor | `settlement` | Settlement root | Roads, veg | `RunSettlements` |
| Roads | Network + `TerrainDeformer` (road corridors) | none (deterministic) | Road root | Grass lawn | `RunRoads` |
| Trees | Tree prefabs | `vegetation` | Trees child only | — | `RunTrees` |
| Undergrowth | Shrubs / flowers / rocks | `vegetation.shrubs` | Shrubs/Flowers/Rocks children | — | `RunUndergrowth` |
| Player | Character at a TownApproach | `player.spawn` | Player object | — | `RunPlayer` |

**Surface / Zones / Mountains are not additive prefabs.** They share one **Vista** terrain bake with `TerrainBakeFlags` (`IncludeMountains`, `IncludeZoneTextures`). Changing Surface or Mountains **must cascade-clear** downstream placement — stale content on a new heightmap is worse than an empty tile. MapMagic graphs are retired. Re-ensure ocean after terrain clear.

**Full Generate All order:** Surface → Zones → Mountains → Markers → Settlements → Roads → Ensure Ocean → Trees → Undergrowth → Grass → Player.

**Mountain occupancy for lakes:** any silhouette cell classified `BiomeId.Mountain` (or steep highland) hard-rejects the site. Soft-pass / min-count **must not** place a lake on a mountain. Fewer lakes beat a lake through a ridge. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md).

**Menu:** `GameDev/Overworld/Generate All`, `Clear All`, `Part-Gen/<Step>/Generate|Clear`, `World Generator` (step checkboxes + knobs). Debug menus stay under `Debug/`.

---

## Related Documents

- [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md)
- [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)
- [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)
- [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)
