# World Generation Pipeline — Target Architecture

**Status:** Draft
**Date:** 2026-08-14
**Applies to:** Track W (World Gen), all waves after Overworld V0
**Related:** [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md) (current iteration), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

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

Do not scale prefabs independently — placers read `WorldScale`. Lake shores use analytic `LakeSilhouette` SDF (trig wobble), not the 3 m occupancy raster. Geometric pipeline: [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). Generation timing logs as `[Gen] step=... ms=...` from `OverworldGenTiming`.

---

## 2. Pipeline stages → System Cards

| # | Stage | Produces | Maps to (17) | V0 status |
|---|-------|----------|---------------|-----------|
| 1 | World Layout | Continents/islands/coastline | New — not in `17` yet, out of scope until multi-tile worlds are real | Not started; V0 is one stub tile, no ocean/continent concept |
| 2 | Macro Terrain | Mountains/plains/valleys/plateaus, at 3 scales (primary/secondary/detail noise, not one octave) | `W.2 Terrain/Biome` | **Ridge structure + percentile-ranked + vertically compressed macro shape** — a `Voronoi200` "cellular" (F2-F1) ridge field, masked to `macroNoise`'s own high range (so ridges only appear where the map was already trending toward highland), is added onto `macroNoise` before ranking - a connected network of ridgelines with foothills tapering into plains, instead of one shapeless noise blob promoted to "the mountain". The combined field still feeds one `PercentileRankGenerator` (custom node, `Assets/Scripts/World/Terrain/PercentileRankGenerator.cs`), which remaps every cell to its own rank in the field's distribution, so `BiomeClassifier`'s height bands stay exact area-fraction guarantees regardless of seed or ridge shape (a fixed threshold on raw Perlin noise couldn't promise that - documented at length in `BuildGraph`'s history). Raw per-pixel ranking also locally steepens the field wherever the source distribution is densest - a `Blur200` pass (~60m, well under the biome bands' spatial scale) tames that pixel-scale jaggedness without erasing the macro banding. Area guarantee alone isn't slope/flatness, though - so a `BiomeClassifier.PlainsElevationCap`-sized vertical compression squeezes the bottom `RankGreenMaxHeight` of ranked area (Swamp+Green) into a thin elevation slice while Highland+Mountain keep the rest, real-hypsometric-curve style. Mountain and Highland terracing each operate on the compressed field the same isolate/terrace/recombine way, so they still only affect their own guaranteed fraction. Tile size stays 1400x1400 (from MapMagic's 1000x1000 default) for breathing room between the range and buildable plains |
| 3 | Hydrology | Watersheds, rivers, lakes; **rivers carve terrain**, not decals on top | New sub-concern of `W.2`/`W.4` | **Lakes = `LakeDefinition` (one SDF).** Analytic basin (`LakeBasinDeformer`) — designed bowl, no noise floor; basin expanded / water inset. Full pipeline defers dig until after road planning ([LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md)). Rivers remain corridor primitive. |
| 4 | Biomes | Meadow/Forest/Swamp/Highland/Mountain from **height + slope + moisture + temperature + distance-to-water**, not height alone | `W.2 Terrain/Biome` (`IBiomeSampler`) | **Height + slope + moisture, with guaranteed area fractions** — `BiomeClassifier.Classify(height01, slope01, moisture01)` against a percentile-ranked height channel (see Macro Terrain row above): bottom 10% Swamp(wet)/Meadow(dry), next 35% Forest(wet)/Meadow(dry), next 35% Forest unconditionally, next 10% Highland (new `BiomeId`) unless steep (then Mountain), top 10% Mountain - Green (Meadow ∪ Forest) is guaranteed >= 70% of the map by construction, comfortably past the "at least half the map is green" requirement. Slope/moisture read from their own `DirectMatricesOutput200` channels (slope from `Slope200` on the smooth erosion field, moisture an independent large-period noise forked from its own `WorldSeed.Fork("biome")` stream). Temperature and distance-to-water still deferred; lake/river coverage is not part of this guarantee (water occupies whichever band its basin/channel lands in, same as real hydrology sitting in low ground - not tuned via the height graph at all anymore, see Hydrology row above) |
| 5 | Terrain Shaping | Cliffs, terraces, local erosion detail | `W.2`/`W.4` | `Erosion200` + `Terrace200` applied to both Mountain and Highland bands independently (see It. 01) - Highland now has its own, gentler terracing distinct from the mountain's more dramatic steps |
| 6 | Terrain Material | Grass/rock/mud/sand/snow from biome+slope+moisture+water-distance | `W.4 Building/Shell` dressing / new card | **Height+slope+moisture painted, 6 layers** — `TexturesOutput200` in `OverworldTerrainMenu.BuildGraph` paints 6 ready-made `Assets/MapMagic/Demo/TerrainLayers/` layers (grass/dirt/yellow-grass/gravel/cliff-rock/snow, gravel added for the new Highland band) from mask thresholds mirroring `BiomeClassifier`'s own (now percentile-guaranteed) bands, so the painted ground roughly matches the sampled biome. Water-distance-driven sand/shoreline still deferred (same gap as Biomes row) |
| 7 | Vegetation | Ecological Forest/Shrub/Grass density fields; trees/shrubs/flowers Prefab scatter; **GPU grass carpet** (`GrassField`) | `W.2`/`W.4` dressing | Done for V0 — large forest masses, trunk colliders, 12–25 m trees, near-full GPU grass cover (see Iteration 07) |
| 8 | World Features | Ruins/caves/camps/shrines as POI markers | `W.4 POI Placement` | Started — `PoiKind` list already includes ruin/camp/shrine/town approach/cave mouth |
| 9 | Roads | Cost-field path + vertical profile + corridor conform (not canyon cut) | `W.5 Settlement` | **LandscapePlanner** — `LandscapeCostField` (slope³ + earthwork), hierarchical MST, `RoadCorridorDeformer`, see [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) |
| 10 | Settlements | Location choice (flat + water + road access) → `W.5` | `W.5 Settlement` | **S6–S8:** TownApproach dry fabric + District Mix + street props (`SETTLEMENT_PROPERTY_V0.md`) |
| 11 | Districts / Plots | Multi-district parcels + GardenSquare open space + street spine + street dress | `W.5 Settlement` | **Lab through S8** — `CreateDistrictMix` + `StreetPropPlacer`; not OrganicTown |
| 12 | Buildings | Building DNA / Piece-Based Assembly | `W.6 Building/Shell`, [15](../../../15%20Procedural%20Building%20Generation.md) | Deferred |
| 13 | Chunking / Streaming | Deterministic, seamless, LOD | `W.3 Chunk Streaming` | `ChunkCoord` math done (`W0.2`); streamer itself is `W0.5` |

Rows 1, 3, 6, 9 don't have a System Card yet in `17` — add one when that wave actually starts, following the existing card template. Don't pre-author empty cards now.

---

## 3. Principles worth keeping verbatim

- **Never one noise field.** `Landmass mask + mountain mask + large noise + medium noise + biome modifiers + erosion → final height`, each layer with a stated job. This is already how `OverworldTerrainMenu`'s plateau+mountain-noise+Levels blend works; extend the same way (add layers with a reason), don't collapse back to a single Perlin call.
- **Terrain masks are data, not throwaway locals.** Height, temperature, humidity, moisture, slope, distance-to-ocean, mountain-influence, forest-influence, river-influence — worth keeping as named channels (the `DirectMatricesOutput200` mechanism already proves this is wireable) so biome/material/vegetation/road rules can all read the same maps instead of each re-deriving their own.
- **Rivers reshape terrain.** Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified terrain is not hydrology.
- **Biome is not just a texture switch.** It should also drive terrain shape tendencies, vegetation density, and (later) POI placement rules — see `W.2`'s relation to `W.4`/`W.6` dressing.
- **Roads are pathfinding, not lines.** Lowest-cost path respecting slope, rivers (bridges), and mountains (passes) — never straight through a mountain.
- **Settlements are shaped by terrain**, not a flat square with houses dropped on it — matches `W.5`'s "layout intents: road hints, plot footprints, district tags" wording already in `17`.
- **Chunk generation must be a pure function of world coordinates** (`Height = f(worldX, worldZ, seed)`), never local-to-chunk coordinates, or neighboring chunks won't agree at the border. Already how `ChunkCoord` (`W0.2`) and the MapMagic `DirectMatricesHolder` world-space queries (`W0.3`) are built — keep it that way for every future sampler.
- **Generation is not persistence.** Regenerable structure (heightmap, biome bands, vegetation placement) is never saved; only player-caused deltas (felled tree, built structure) are. Matches `X.3 Save System`'s existing "never dump regenerable chunk meshes" rule.

---

## 4. Development order (12 iterations, world-gen only — Locomotion/Interior/Arena waves from `14` interleave separately)

| It. | Deliverable | Status |
|-----|-------------|--------|
| 01 | Heightmap only (large-scale terrain, no biomes) | **Done for V0 scope, reworked from a single mass to a ridge network** — a `Voronoi200` cellular ridge field, masked to the macro noise's own high range, replaces what used to be one shapeless "mountain feature" with a connected range (ridgelines + foothills tapering into plains). Mountain and Highland are still both terraced into stepped ledges via the same isolate/terrace/recombine technique (`OverworldTerrainMenu.BuildGraph`: `mountainLocal`/`terracedLocal` for Mountain, `highlandLocal`/`highlandTerracedLocal` for Highland, chained so each only reshapes its own band and leaves the other's already-terraced cells untouched - verified via `Matrix.Terrace`'s source, which leaves values above 0.9999 untouched and maps 0 straight through, so the subtract/add pair cancels to a no-op outside each band), now operating on the ridge-bearing field instead of plain noise. Highland uses gentler params (4 terraces, uniformity 0.4, steepness 0.5) than the mountain's (6, 0.6, 0.8) so it reads as rocky outcrops/mesas, not a second mountain. `Erosion200` left at MapMagic's class defaults (3 iterations, durability 0.9, sediment 0.75, fluidity 3) - now runs on top of deliberately-carved basins (see Hydrology row) to soften their rims, the same "refines, doesn't invent" role it plays on ridge terracing. **Incident, four rounds:** round one pushed erosion much further (6 iterations, durability 0.85, sediment 0.6, fluidity 4) and broke lake generation badly - enough hydraulic erosion on the deliberately-flattened plains band (`PlainsElevationCap`) manufactures its own tiny local minima where the pure-noise field had none at all, and `LakeBasinFinder`'s depth-based flood found no real rim on that uniformly flat band, running out to its safety cap almost everywhere ("the whole map is one ocean"). Round two tightened `CellCountSafetyCap`/`MaxLakeDepthMeters` twice (see Hydrology row) and only reduced the symptom, not the cause. Round three found a real contributing cause: the ridge region mask (`ridgeRegionMask`) started at 0.45 - well below Perlin's own ~0.5 mean - so roughly half the map's raw macroNoise values picked up some ridge contribution, not just the top ~20% that ranking turns into Highland/Mountain; raised to 0.62-0.85. This reduced candidate lake-seed count in the plains to a healthy 17 (confirmed via `LogDiagnostics`), but zones STILL hit the safety cap - because round one's premise ("ridges alone will produce enough closed basins") was itself wrong: ridges only shape the highland/mountain band, and the compressed plains band was never given ANY mechanism to form a real depression once the original six hand-placed lake pits were removed. Round four restored basin carving (`basinSpecs`, above) - five deliberate sites, this time masked to the plains band only (so a site can never bite into highland/mountain) and carved deep enough (opacity = full `PlainsElevationCap` band width) to guarantee `LakeBasinFinder` a real rim, instead of the original version's unmasked, arbitrary-depth carving. **Round five, a genuine math bug** (found by hand-deriving the blend math, not by further constant tuning) in round four's own plains mask: it ramped linearly across the ENTIRE plains band (0 to `PlainsElevationCap`), so a basin site centred on merely-average plains ground - not the literal lowest point in the whole band - already had its carve strength cut roughly in half or more before the noise cutoff even applied, since site positions are random and uncorrelated with where the terrain happens to already be lowest. Most of the 5 sites ended up too shallow to register as a lake at all. The mask's actual job is narrower - stop a site from biting into highland, nothing more - so it should read ~1 across nearly the whole plains band and only fall off right at the highland boundary; narrowed to a ramp over just the top 15% of the band. **Round six**: that mask fix worked as intended for depth, but exposed a second, previously-hidden variable - the basin noise cutoff thresholds (0.55-0.66) were carried over unchanged from the old lake-pit system, where the (now-removed) over-wide mask happened to also be damping effective AREA as a side effect. With the mask no longer doing that, those thresholds let 5 overlapping large-period fields cover ~29% of the tile, several sites merging into single basins large enough to hit `CellCountSafetyCap` for real (genuinely overlapping carve area, not a flat-rim failure this time). Area and depth are two separate knobs now - thresholds raised to 0.68-0.77 and site count dropped to four so each carves a distinct, deliberate pit instead of blanketing a third of the plains. **Round seven**: that didn't work either - LogDiagnostics showed total coverage actually rose to 37%, with 5 of 8 zones hitting the cap and the uncapped ones sitting close to it too. Working theory: a narrower/steeper carve edge invites MORE hydraulic erosion right at that edge (steeper local gradient = stronger simulated torrents there), widening the basin floor past what the raw carve footprint suggests - so basinSpecs' thresholds don't cleanly control final flooded size once erosion acts on the result. Site count dropped to three (reduces cross-site overlap) and, more importantly, the actual size bound moved to where it can be enforced directly: `WaterZoneMenu.MaxLakeDepthMeters` (12 -> 9m) and `CellCountSafetyCap` (3000 -> 1500 cells, ~260m diameter) - tightening the outcome rather than continuing to chase upstream noise parameters that don't map predictably to it once erosion is in the loop. **Round eight, confirmed on a verified-fresh compile** (`CellCountSafetyCap`'s new value was checked to actually be in effect, ruling out a stale-DLL false signal from a mid-recompile test): 7 of 8 zones still landed exactly on the new 1500-cell cap even with fewer, higher-threshold basin sites. Root realization - threshold was never the lever that controls final footprint size; a Perlin field's spatial features are sized by its PERIOD regardless of where you cut it, so a 320m-period site cut at 0.55 or 0.77 both isolate a peak region hundreds of metres across, just with a steeper or gentler edge. `basinSpecs`' sizes dropped from the old lake-pit system's 150-480m range to a genuinely pond/small-lake scale (80-200m), with thresholds relaxed back down since small periods no longer need an aggressive cutoff to stay modest. **Round nine**: shrinking the period fixed HALF the zones (4 of 8 landed at genuinely pond/small-lake sizes, 0.5-1.9% of tile each) and produced the first real river - but candidate-minima count jumped 17-27 -> 120 (a shorter period naturally has more peaks per unit area), and enough of those peaks now sit close enough together that their floods bridge into one merged basin before either finds a real rim. Density, not size, is the remaining lever: thresholds raised again (0.70-0.76, this time to cut how much total area qualifies at all rather than to shrink one basin's own footprint, which round seven already showed doesn't work) and `WaterZoneMenu.MaxLakeDepthMeters` lowered further (9 -> 7m) so two nearby basins need to sit genuinely close in height, not just close in space, before merging. Landmass/ocean (Stage 1, Iteration 02) remains explicitly out of scope until multi-tile worlds are prioritized |
| 02 | Landmass (ocean/islands/coastline) | Not started — out of scope until multi-tile/ocean is prioritized |
| 03 | Terrain layers (large/medium/small feature scales) | **Explicit 3-scale layering** — mountain (large, ~2000m) + plateau/hills (medium, ~600m/220m) + `smallDetailNoise` (small, ~30m, `OverworldTerrainMenu.BuildGraph`). The small layer only feeds the rendered `HeightOutput`, never `directOutput`'s "height" channel, so biome sampling and `LakeBasinFinder`'s local-minima search stay on a smooth field instead of tripping over fine-noise micro-bumps |
| 04 | Biomes (Meadow/Forest/Mountain/Swamp/Coast) | **Started early, height-only** — `BiomeClassifier` exists ahead of hydrology/terrain-layers being done; that's fine for a V0 stub but the multi-axis version belongs after 02-03 land |
| 05 | Water (lakes/rivers/streams/valleys) | **Lakes + rivers live** (`unity/Assets/Scripts/World/Water/`). Production lakes use `DistributedLakePlacer` + `LakeSilhouette`: base ellipse × low-frequency terrain shore factor (±10%) × light organic wobble (±3%); orientation/aspect from local basin direction — not `LakeBasinFinder` flood footprints (Finder remains legacy/tests). Basin dig expands by `LakeBasinExpandMeters`; water mesh expands by `WaterOverlapMeters` (~0.25 m) so the sheet sinks under banks. Rivers: `FlowNetwork` / `RiverTracer` / `RiverTerrainCarver` / `RiverMeshBuilder` as before. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). |
| 06 | Terrain materials (grass/dirt/rock/sand/mud/snow) | **First slice done** — `TexturesOutput200` in `OverworldTerrainMenu.BuildGraph` paints 6 layers (grass background, wet-low dirt, forest band, highland gravel, mountain rock, snow cap) from the same height/slope/moisture masks `BiomeClassifier` uses, bottom-to-top priority per the `Matrix.BlendLayers` semantics. `MakeTextureLayer`/`LoadScaledTerrainLayer` duplicate MapMagic's Demo `TerrainLayer` assets into `Assets/Settings/TerrainLayers/*_Scaled.terrainlayer` with `tileSize` rescaled (45m grass/dirt/forest, 60m gravel/rock/snow) instead of mutating the shared originals — the source assets were authored for MapMagic's small demo scene (Gravel at 4m, CliffBright at 20m) and aliased into a visible "TV static" pattern on mountain slopes at our ~1400m tile size |
| 07 | Vegetation (trees/bushes/grass/flowers/rocks) | **Ecological density + GPU grass carpet** — `VegetationEcology` macro/meso Forest/Shrub/Grass fields. Trees/shrubs/flowers/rocks still Prefab scatter; **grass is GPU coverage** via `GrassMask` → `GrassDistribution` (0.5 m cells, ecology on 2 m grid) → `GrassField` + `Graphics.RenderMeshInstanced` (PT_Grass_02 LOD mesh + `GameDev/Overworld/Grass` AlphaClip shader with wind + player bend). No GameObject per blade. Settlement fabric persisted on `OverworldSettlementAnchor` for street/pad exclusion (`GrassExclusion`). Seed fork `"grass"`. Pipeline vegetation **last**. EditMode: `VegetationEcologyTests`, `VegetationClearanceTests`, `GrassMaskTests`, `GrassDistributionTests`, `TreePlacementTests` |
| 08 | POIs (caves/ruins/resource areas) | **Profile-driven placement** — `PoiPlacementEngine` + `PoiPlacementCatalog` / per-kind `PoiPlacementProfile` (hard/soft/relation/footprint). Demo quota still from `PoiDemoScenario` (5 TownApproach, 3 Shrine, 10 Camp, 5 Landmark). TownApproach scores a circular footprint (slope / flatness / height variance); Shrine / Camp / Landmark use isolation, forest-edge, and scenic cues. `StubPoiPlacer` remains the `IPoiPlacer` facade. `PoiMarkerMenu` still spawns props/buildings. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) and [17](../../../17%20Game%20Systems%20Architecture.md) W.4. |
| 09 | Roads (graph + terrain cost + river crossings + mountain passes) | **LandscapePlanner** — `LandscapeCostField` nonlinear cost (slope³, excavation^2.5) + existing-road paint discount; growing Prim Primary network among TownApproach (snap to accepted corridors) then Secondary attach to whole network; `RoadTier` Primary/Secondary/Local (Local deferred); `EnsureMinimumConnectivity` (degree ≥ 1); `RoadVerticalProfile` + `RoadCorridorDeformer`; bridges rivers only. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md). |

**Corridor conform (not canyon cut).** Terrain lerps toward the road profile across road / shoulder / outer blend zones. Roads avoid deep cuts via cost; deform only gently conforms.

**Natural preference.** Detours up to ~20% length (`RoadDetourTolerance`) beat short steep cuts. |
| 10 | Settlements (location → type → districts → road network) | Property lab through S8 (`SETTLEMENT_PROPERTY_V0.md`: District Mix + GardenSquare + street props + overworld dry place); terraces / TownDress deferred. **Linked to the road network** — `SettlementApproachBinder.TryCreateDryFabricNearApproach`'s dry-ground search can move the fabric up to ~192m (`DrySearchRingMax * DrySearchStepMeters`) from its raw TownApproach POI marker, which previously left `RoadNetworkMenu` routing to a coordinate the settlement no longer occupied. `SettlementOverworldMenu` now drops an `OverworldSettlementAnchor` (`Assets/Scripts/World/Settlement/`) recording the fabric's real landing position, and `RoadNetworkMenu` substitutes that position for the matching TownApproach marker when building its node list, so the road actually reaches the town. Only works when Settlement placement runs before Roads - `OverworldPipelineMenu.RunRemainingSteps` reordered accordingly (Settlement now comes before Roads); running `Build Road Network` standalone without an existing settlement still falls back to the raw marker position, unchanged from before |
| 11 | Buildings (Building DNA / Piece-Based Assembly) | Not started, deferred by `15` |
| 12 | Streaming (chunks, async, LOD, unload) | `ChunkCoord` math done; async/LOD not started |

**Where we actually are:** Iterations 01 and 03-09 done for V0 scope (ridge-structured macro terrain with Highland/Mountain terracing, multi-scale noise, height+slope+moisture biomes with guaranteed area fractions, terrain-derived Water Zones with basin-fed lakes AND flow-accumulation rivers that carve their own valleys, 6-layer terrain painting, ecological multi-layer vegetation, quota-scaled POI with real props, and a bridged, scalable road network that treats rivers as water the same way it already treats lakes), Iteration 10 (Settlements) through the property lab and linked to the road network. Still open: Iteration 02 (Landmass/ocean, deliberately deferred until multi-tile worlds), temperature/distance-to-water biome axes, river-specific bridge tuning (Iteration 09's `MaxBridgeSpanMeters`/`BridgeCostPerMeter` are still only tuned against lake geometry), POI Director lifecycle (Q.3 - markers are still placed props, not live gameplay objects), and Iteration 11 (Buildings, deferred by `15`).

**One-button pipeline:** `WorldGeneratorWindow` / `OverworldPipeline.GenerateAll` runs the Editor pipeline steps below (skip mask from `WorldGenSettings`). Part-Gen menu items call the same step methods. **Whenever a future iteration adds a new Overworld menu step, register it as an `IOverworldStep`.**

---

## 5. Editor pipeline steps (executable build order)

Target architecture in §2 is the long-term map. **This section is the Editor build order** — each step is a full Generate/Clear procedure that can be skipped in Generate All via `WorldGenSettings` step flags (flags are not seeds: same seed + Mountains off → stable map without ridges).

| Step | Artifact | Fork(s) | Clear | Cascade on regenerate | Skip flag |
|------|----------|---------|-------|----------------------|-----------|
| Surface | MapMagic height + `height/slope/moisture` channels (no mountain layers) | `terrain.*` without masses/ridge/terrace | Graph rebuild without mountains; clears downstream | Lakes, markers, settlements, roads, vegetation, grass, player | `RunSurface` |
| Zones | Terrain alphamap / biome paint | (deterministic from height+moisture) | Neutral ground layers | None (paint only) | `RunZones` |
| Mountains | Same graph with masses + ridge + mountain/highland terrace + rock/snow | `terrain.masses`, `terrain.ridge`, `terrain.terrace`, `terrain.highland-terrace` | Rebuild Surface without mountain layers | Same as Surface | `RunMountains` |
| Lakes | `LakeDefinition` plan + river paths (dig deferred in full run) | `lake.place`, `lake.{i}` | `OverworldWater` / `OverworldRivers` | Markers+ if hydrology invalidates sites | `RunLakes` |
| Markers | POI plan + visuals | `poi.{kind}.{i}` | POI root | Settlements, roads, veg clearance | `RunMarkers` |
| Settlements | TownApproach fabric + anchor | `settlement` | Settlement root | Roads, veg | `RunSettlements` |
| Roads | Network + joint `TerrainDeformer` (lake basins + corridors) | none (deterministic) | Road root; does not un-dig lakes alone | Water meshes, grass lawn | `RunRoads` |
| WaterMeshes | Lake/river meshes after dig | none | Mesh children under water roots | Veg water exclusion | `RunWaterMeshes` |
| Trees | Tree prefabs | `vegetation` | Trees child only | — | `RunTrees` |
| Undergrowth | Shrubs / flowers / rocks | `vegetation.shrubs` | Shrubs/Flowers/Rocks children | — | `RunUndergrowth` |
| Grass | GPU `GrassField` | `grass` | GrassField objects | — | `RunGrass` |
| Player | Character at a TownApproach | `player.spawn` | Player object | — | `RunPlayer` |

**Surface / Zones / Mountains are not additive prefabs.** They share one MapMagic graph with `TerrainBakeFlags` (`IncludeMountains`, `IncludeZoneTextures`). Changing Surface or Mountains **must cascade-clear** lakes through grass — stale water planes on a new heightmap are worse than an empty tile.

**Full Generate All order:** Surface → Zones → Mountains → Lakes (plan) → Markers → Settlements → Roads (dig basins) → WaterMeshes → Trees → Undergrowth → Grass → Player.

**Mountain occupancy for lakes:** any silhouette cell classified `BiomeId.Mountain` (or steep highland) hard-rejects the site. Soft-pass / min-count **must not** place a lake on a mountain. Fewer lakes beat a lake through a ridge. See [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md).

**Menu:** `GameDev/Overworld/Generate All`, `Clear All`, `Part-Gen/<Step>/Generate|Clear`, `World Generator` (step checkboxes + knobs). Debug menus stay under `Debug/`.

---

## Related Documents

- [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md)
- [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)
- [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)
- [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)
