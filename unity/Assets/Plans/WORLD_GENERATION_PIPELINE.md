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
 ├── terrain.seafloor           (LPB_SeaFloor biome seed)
 ├── terrain.islandbase         (LPB_IslandBase biome seed)
 ├── terrain.islandring         (coastline harmonics + radius noise)
 ├── terrain.coast.yaw          (which side is cliff coast, which is beach)
 ├── terrain.mountain.place     (how many massifs, and where)
 ├── terrain.mountain.{i}       (per-massif biome seed)
 ├── terrain.mountain.ring.{i}  (per-massif ridge silhouette + yaw)
 ├── poi.{kind}.{i}             (per-marker tie-break; adding a kind must not move earlier markers)
 ├── poi-visual                 (which prop / building prefab a marker receives)
 ├── settlement                 (W.5 fabric pose / plots)
 ├── player.spawn               (which TownApproach receives the player)
 └── roads                      (no RNG — LandscapeCostField + A* are deterministic)
```

No `vegetation` / `grass` fork: canopy, understory, ground cover and rocks are placed **inside the
Vista terrain graphs**, whose RNG derives from the biome seed (`Args.SEED XOR node.m_seed`). There
is no C# scatter pass left to seed independently.

No `lake` / `river` fork: inland hydrology is not generated in V0 (removed 2026-08-15). See
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md) §
Future Hydrology / River Generation.

`W.1 WorldSeed` (`unity/Assets/Scripts/World/Seed/`) implements fork-by-name and `Fork(streamId, a, b)` for per-object isolation. POI uses `poi.{kind}.{index}` so inserting a new kind does not reshuffle existing TownApproach positions. Skipping a pipeline step must not consume or advance another step's fork. Aggregate output lives in `WorldDefinition` (see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md)). `OverworldLpbBake` and `OverworldWorldPlan` are the source of truth for the fork names above — update this list from the code, not the other way round.

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
| Vista biome canvas | **512 px — Vista Personal hard cap** (island 5.08 m/px, massif 4.08 m/px) |

Do not scale prefabs independently — placers read `WorldScale`. Geometric pipeline: [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). Generation timing logs as `[Gen] step=... ms=...` from `OverworldGenTiming`.

**The 512 px cap is a design constraint, not a bug to route around.** Vista Personal caps
`LocalProceduralBiome.baseResolution` at 512 (`Pinwheel.Vista.Constants.RES_MAX`), so the island
biome canvas is coarser than the tile heightmap it writes into. Terrain graphs must be designed for
that resolution; raising it — or budgeting for Vista Indie — is not an accepted solution to a terrain
artifact. Detail: [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md) §2.4.

---

## 2. Pipeline stages → System Cards

| # | Stage | Produces | Maps to (17) | V0 status |
|---|-------|----------|---------------|-----------|
| 1 | World Layout | Continents/islands/coastline | New — not in `17` yet | **Island-in-ocean, entirely Vista.** `LPB_SeaFloor` + `LPB_IslandBase` anchors/Outer falloff decide the silhouette; WaterWorks `WorldWater` at `SeaLevelMeters`. `IslandNaturalGeography` / `IslandOccupancy` / `NaturalIsland_TerrainGraph` are **deleted** — no C# framing pass. Multi-tile continents still deferred. |
| 2 | Macro Terrain | Mountains/plains/valleys/plateaus | `W.2 Terrain/Biome` | **Vista Personal** `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph` (Vista reference biome architecture; see `VISTA_OVERWORLD_REFERENCE.md`). MapMagic recipe **retired**. Height sample = `Terrain.SampleHeight` / `VistaTerrainSampler`. Tile 1400×1400; height budget **700 m**; biome canvas capped at 512 px. |
| 3 | Hydrology | **Deferred** — V0 ocean plane only; inland lake/river gen removed 2026-08-15 | W.2 / W.2b | Ocean = one WaterWorks WorldWater. Future inland hydro = redesign. |
| 4 | Biomes | Meadow/Forest/Swamp/Highland/Mountain from **height + slope + moisture + temperature + distance-to-water**, not height alone | `W.2 Terrain/Biome` (`IBiomeSampler`) | **Height + slope + moisture** via `VistaTerrainSampler` + `BiomeClassifier`. Classification is **semantic only** — the visible surface comes from the graphs' Weight Blend, not from this. Moisture is currently an unseeded Perlin field (`VistaTerrainSampler.SampleMoisture01` discards its `moistureSeed`), so it is identical across seeds; temperature / distance-to-water deferred. |
| 5 | Terrain Shaping | Cliffs, terraces, local erosion detail | `W.2`/`W.4` | Entirely inside the **Vista** biome graphs (two `HydraulicErosionNode2` passes per graph). MapMagic `Erosion200`/`Terrace200` recipe retired; StampIT is not in the project. |
| 6 | Terrain Material | Grass/sand/soil/cliff/rock from the terrain graphs' morphology masks | `W.4` dressing / new card | Weights from the graphs' `Weight Blend` → `TEXTURE OUTPUT`; material = URP **TerrainLit** over the five canonical layers (`OverworldTerrainLayers`). **No C# painting, no MicroSplat.** Custom `GameDev/Overworld/Terrain` **deleted**. Spec: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md). |
| 7 | Vegetation | Canopy, understory, ground cover and rock dressing, all from the graphs' `TREE OUTPUT` / `DETAIL DENSITY OUTPUT` | `W.2`/`W.4` dressing | **Done and accepted for V0** on all four tiles. There is no GameObject scatter pass — `VegetationScatterMenu` and its CPU mask cluster are deleted. Do not add a second placer. |
| 8 | World Features | Ruins/caves/camps/shrines as POI markers | `W.4 POI Placement` | Started — `PoiKind` list already includes ruin/camp/shrine/town approach/cave mouth |
| 9 | Roads | Cost-field path + vertical profile + corridor conform (not canyon cut) | `W.5 Settlement` | **LandscapePlanner** — `LandscapeCostField` (slope³ + earthwork), hierarchical MST, `RoadCorridorDeformer`, see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) |
| 10 | Settlements | Location choice (flat + water + road access) → `W.5` | `W.5 Settlement` | **S6–S8:** TownApproach dry fabric + District Mix + street props (`SETTLEMENT_PROPERTY_V0.md`) |
| 11 | Districts / Plots | Multi-district parcels + GardenSquare open space + street spine + street dress | `W.5 Settlement` | **Lab through S8** — `CreateDistrictMix` + `StreetPropPlacer`; not OrganicTown |
| 12 | Buildings | Building DNA / Piece-Based Assembly | `W.6 Building/Shell`, [15](../../../15%20Procedural%20Building%20Generation.md) | Deferred |
| 13 | Chunking / Streaming | Deterministic, seamless, LOD | `W.3 Chunk Streaming` | `ChunkCoord` / `OverworldTileChunks` math done at 64 m; the streamer itself is not started |

Rows 1, 3, 6, 9 don't have a System Card yet in `17` — add one when that wave actually starts, following the existing card template. Don't pre-author empty cards now.

---

## 3. Principles worth keeping verbatim

- **Never one noise field.** Each Vista graph composes a broad mass noise with a detail noise and then erodes; the island silhouette is a seeded harmonic anchor ring, not a radius. Extend with named concerns, don't collapse to a single Perlin call or a radial pancake.
- **Vista generates, C# composes.** C# may create and configure Vista components, assign graphs and templates, decide which biome exists where, and trigger generation. It may not write heights, paint alphamaps, assign `terrainData.terrainLayers`, place trees or grass, or fix up Vista's output afterwards. If the result is wrong, the graph or the template is wrong.
- **Design for 512 px.** Vista Personal caps the biome canvas; a graph feature smaller than the canvas cell cannot exist, and asking for one produces grid artifacts rather than detail. Raising the resolution is not an available answer.
- **Terrain masks are data, not throwaway locals.** Height/slope/moisture read through `VistaTerrainSampler` (Unity Terrain + classifier) so biome/material/vegetation/road rules share one surface.
- **Rivers reshape terrain.** Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified terrain is not hydrology. Not implemented in V0 — this is the bar any future attempt has to clear.
- **Biome is not just a texture switch.** It should also drive terrain shape tendencies, vegetation density, and (later) POI placement rules — see `W.2`'s relation to `W.4`/`W.6` dressing.
- **Roads are pathfinding, not lines.** Lowest-cost path respecting slope, rivers (bridges), and mountains (passes) — never straight through a mountain.
- **Settlements are shaped by terrain**, not a flat square with houses dropped on it — matches `W.5`'s "layout intents: road hints, plot footprints, district tags" wording already in `17`.
- **Chunk generation must be a pure function of world coordinates** (`Height = f(worldX, worldZ, seed)`), never local-to-chunk coordinates. Already how `ChunkCoord` and `VistaTerrainSampler` / `Terrain.SampleHeight` world-space queries are built — keep it that way for every future sampler. The same rule applies to **tiles**: a world-dressing pass must resolve the tile that contains a point, never assume tile (0,0).
- **Generation is not persistence.** Regenerable structure (heightmap, biome bands, vegetation placement) is never saved; only player-caused deltas (felled tree, built structure) are. Matches `X.3 Save System`'s existing "never dump regenerable chunk meshes" rule.
- **Seed forks still via `WorldSeed`.** Vista `LocalProceduralBiome.seed` is set from named forks (`terrain.islandbase`, `terrain.mountain.{i}`, …) — changing one concern must not move another. Never mutate individual graph node seeds; drive `biome.seed` only.

---

## 4. Development order (12 iterations, world-gen only — Locomotion/Interior/Arena waves from `14` interleave separately)

| It. | Deliverable | Status |
|-----|-------------|--------|
| 01 | Heightmap only (large-scale terrain, no biomes) | **Natural island geology** — `SeaFloor_TerrainGraph` + `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph` under `OverworldLpbBake`. Height queries: `VistaTerrainSampler` → `Terrain.SampleHeight`. See [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md). |
| 02 | Landmass (ocean/islands/coastline) | **Seeded harmonic anchor ring + Outer falloff + WaterWorks WorldWater** at `SeaLevelMeters`. Multi-tile continents still deferred. |
| 03 | Terrain layers (large/medium/small feature scales) | **Vista graph only**, and bounded by the 512 px biome canvas — the smallest representable island feature is ~5 m. Small-scale detail must not break biome sampling smoothness. |
| 04 | Biomes (Meadow/Forest/Mountain/Swamp/Coast) | **`VistaTerrainSampler` + `BiomeClassifier`** (height+slope+moisture). Temperature / distance-to-water deferred. |
| 05 | Water | **Ocean only (V0).** Inland lakes/rivers deferred. |
| 06 | Terrain materials (grass/sand/soil/cliff/rock) | Five canonical `TerrainLayer` assets under `Assets/Settings/TerrainLayers/Overworld/`, bound by both terrain graphs and rendered by URP TerrainLit. Vista owns the layer array and the alphamaps. No snow layer in V0. Spec: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) |
| 07 | Vegetation (trees/bushes/grass/rocks) | **All four layers come from the terrain graphs** — `GreenAreaMask` → per-species cluster noise → `Scatter` → `TREE OUTPUT` (canopy, understory, rocks) and `DETAIL DENSITY OUTPUT` (ground cover), with `VistaVegetationTemplates` content. The CPU scatter cluster (`VegetationScatterMenu`, `GrassMask`, `GrassDistribution`, `GrassField`, `VegetationEcology`, `TreePlacement`) is **deleted** — do not reintroduce a second placer. **Accepted for V0 across all four tiles; changes are regression fixes only.** Diagnostics: `Dump Vegetation State`. |
| 08 | POIs (caves/ruins/resource areas) | **Profile-driven placement** — `PoiPlacementEngine` + `PoiPlacementCatalog` / per-kind `PoiPlacementProfile` (hard/soft/relation/footprint). Demo quota still from `PoiDemoScenario`, which reads `WorldGenSettings` (**5** TownApproach, **9** Shrine, **9** Camp, **9** Landmark). TownApproach scores a circular footprint (slope / flatness / height variance); Shrine / Camp / Landmark use isolation, forest-edge, and scenic cues. `StubPoiPlacer` remains the `IPoiPlacer` facade. `PoiMarkerMenu` still spawns props/buildings. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) and [17](../../../17%20Game%20Systems%20Architecture.md) W.4. |
| 09 | Roads (graph + terrain cost + mountain passes) | **LandscapePlanner** — `LandscapeCostField` nonlinear cost (slope³, excavation^2.5) + existing-road paint discount; growing Prim Primary network among TownApproach (snap to accepted corridors) then Secondary attach to whole network; `RoadTier` Primary/Secondary/Local (Local deferred); `EnsureMinimumConnectivity` (degree ≥ 1); `RoadVerticalProfile` + `RoadCorridorDeformer`. Bridges span ocean inlets only — there are no inland rivers to cross. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). |
| 10 | Settlements (location → type → districts → road network) | Property lab through S8 (`SETTLEMENT_PROPERTY_V0.md`: District Mix + GardenSquare + street props + overworld dry place); terraces / TownDress deferred. **Linked to the road network** through `OverworldSettlementAnchor`: `SettlementApproachBinder.TryCreateDryFabricNearApproach` can move the fabric up to ~192 m from its raw TownApproach marker, so `SettlementOverworldMenu` records where the fabric actually landed and `RoadNetworkMenu` routes to that position instead. Requires Settlement to run **before** Roads; running `Build Road Network` standalone falls back to the raw marker. |
| 11 | Buildings (Building DNA / Piece-Based Assembly) | Not started, deferred by `15` |
| 12 | Streaming (chunks, async, LOD, unload) | `ChunkCoord` / `OverworldTileChunks` math done; async/LOD not started |

**Corridor conform (not canyon cut).** Terrain lerps toward the road profile across road / shoulder / outer blend zones. Roads avoid deep cuts via cost; deform only gently conforms. Detours up to ~20 % length (`RoadDetourTolerance`) beat short steep cuts.

**Road and settlement layers are appended after Vista's.** They may add their own `TerrainLayer`s downstream of the bake, but must never reorder or repaint canonical biome indices 0–4, and the tile must stay within URP TerrainLit's ceiling of 8 layers.

**Where we actually are:** a Vista-native island (three LPBs from biome templates + one WaterWorks `WorldWater` plane) with URP TerrainLit over the five canonical layers and all vegetation placed by the graphs; POI / settlement / roads on the LandscapePlanner rails. Inland hydrology is out. Multi-tile continents remain open.

**One-button pipeline:** `WorldGeneratorWindow` / `OverworldPipeline.GenerateAll` runs the Editor pipeline steps below (skip mask from `WorldGenSettings`). Part-Gen menu items call the same step methods. **Whenever a future iteration adds a new Overworld menu step, add it to `OverworldPipelineStepId` + `OverworldPipeline.OrderedSteps` and give it a `WorldGenSettings` skip flag** — a step that is not in that enum cannot be skipped, cleared or cascaded.

---

## 5. Editor pipeline steps (executable build order)

Target architecture in §2 is the long-term map. **This section is the Editor build order** — each step is a full Generate/Clear procedure that can be skipped in Generate All via `WorldGenSettings` step flags (flags are not seeds: same seed + Mountains off → stable map without ridges).

| Step | Artifact | Fork(s) | Clear | Cascade on regenerate | Skip flag |
|------|----------|---------|-------|----------------------|-----------|
| Surface | One Vista bake: height, 5 layer weights, trees and detail density on all four tiles | `terrain.seafloor`, `terrain.islandbase`, `terrain.islandring`, `terrain.coast.yaw` | Destroy tiles + LPBs; re-ensure ocean | Markers, settlements, roads, player | `RunSurface` |
| Mountains | Same Vista bake with the massif LPBs spawned from `LPB_Mountain.prefab` (Raise) | `terrain.mountain.place`, `terrain.mountain.{i}`, `terrain.mountain.ring.{i}` | Re-bake Surface without mountain LPBs | Same as Surface | `RunMountains` |
| Markers | POI plan + marker visuals | `poi.{kind}.{i}`, `poi-visual` | POI root | Settlements, roads | `RunMarkers` |
| Settlements | TownApproach fabric + `OverworldSettlementAnchor` | `settlement` | Settlement root | Roads | `RunSettlements` |
| Roads | Network + `RoadCorridorDeformer` (corridor conform + surface paint) | none (deterministic) | Road root | — | `RunRoads` |
| Player | Character at a TownApproach | `player.spawn` | Player object | — | `RunPlayer` |

**Surface and Mountains are not additive prefabs.** They share one **Vista** terrain bake, selected by `TerrainBakeFlags.IncludeMountains`. Changing either **must cascade-clear** downstream placement — stale content on a new heightmap is worse than an empty tile. MapMagic graphs are retired. Re-ensure the ocean plane after every terrain clear.

**Vegetation is not a pipeline step.** Canopy, understory, ground cover and rocks are written by the terrain graphs during Surface. There is no Trees or Undergrowth step and no `vegetation` seed fork.

**Full Generate All order:** Surface (+Mountains) → Markers → Settlements → Roads → Ensure Ocean → Marker visuals → Player. Settlement must precede Roads so the road network can route to `OverworldSettlementAnchor` rather than to the raw POI marker.

**Menu:** `GameDev/Overworld/Generate All`, `Clear All`, `Part-Gen/<Step>/Generate|Clear`, `World Generator` (step checkboxes + knobs). Debug menus stay under `Debug/`.

---

## Related Documents

- [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md)
- [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)
- [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)
- [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)
