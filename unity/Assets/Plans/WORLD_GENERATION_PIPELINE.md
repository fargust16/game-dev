# World Generation Pipeline — Target Architecture

**Status:** Draft
**Date:** 2026-08-09
**Applies to:** Track W (World Gen), all waves after Overworld V0
**Related:** [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md) (current iteration), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

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
 ├── terrain      (heightmap, macro/medium/detail shaping)
 ├── biome        (temperature/humidity/moisture bands)
 ├── river        (hydrology graph)
 ├── vegetation   (scatter density + placement)
 ├── poi          (already the W.4 fork name)
 └── settlement   (already the W.5 fork name)
```

`W.1 WorldSeed` (`unity/Assets/Scripts/World/Seed/`) already implements fork-by-name. **Known gap:** `OverworldTerrainMenu.BuildGraph` currently derives multiple values from one `"terrain"` fork via sequential `.Next()` calls (graph seed, plateau seed, mountain seed) — inserting a new noise layer in the middle would silently shift every later derived seed. Each independent concern should get its own named fork (`Fork("terrain.plateau")`, `Fork("terrain.mountain")`, …) instead of relying on call order. Fix when terrain generation moves from the Editor lab tool into real `W0.3`/`W0.4` code, not urgent for the throwaway graph-builder.

---

## 2. Pipeline stages → System Cards

| # | Stage | Produces | Maps to (17) | V0 status |
|---|-------|----------|---------------|-----------|
| 1 | World Layout | Continents/islands/coastline | New — not in `17` yet, out of scope until multi-tile worlds are real | Not started; V0 is one stub tile, no ocean/continent concept |
| 2 | Macro Terrain | Mountains/plains/valleys/plateaus, at 3 scales (primary/secondary/detail noise, not one octave) | `W.2 Terrain/Biome` | Started — plateau+mountain blend in `OverworldTerrainMenu`, single-scale so far |
| 3 | Hydrology | Watersheds, rivers, lakes; **rivers carve terrain**, not decals on top | New sub-concern of `W.2`/`W.4` | Not started |
| 4 | Biomes | Meadow/Forest/Swamp/Mountain from **height + slope + moisture + temperature + distance-to-water**, not height alone | `W.2 Terrain/Biome` (`IBiomeSampler`) | Started, height-only (`BiomeClassifier`) — moisture/slope axes are the next refinement |
| 5 | Terrain Shaping | Cliffs, terraces, local erosion detail | `W.2`/`W.4` | Partial — `Erosion200` in the graph, no cliffs/terraces yet |
| 6 | Terrain Material | Grass/rock/mud/sand/snow from biome+slope+moisture+water-distance | `W.4 Building/Shell` dressing / new card | Not started |
| 7 | Vegetation | Density maps per biome, then scatter (Low Poly Environment prefabs) | `W.2`/`W.4` dressing | Not started (asset ready — see `ASSET_LIBRARY_REFERENCE.md`) |
| 8 | World Features | Ruins/caves/camps/shrines as POI markers | `W.4 POI Placement` | Started — `PoiKind` list already includes ruin/camp/shrine/town approach/cave mouth |
| 9 | Roads | Lowest-cost path through terrain (slope/river/mountain aware), not straight lines | `W.5 Settlement` | Not started |
| 10 | Settlements | Location choice (flat + water + road access) → `W.5` | `W.5 Settlement` | Deferred past Overworld V0 per `16`/`17` |
| 11 | Districts / Plots | District tags on `PlotFootprint` | `W.5 Settlement` | Deferred, already documented as `W.5`'s output shape |
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
| 01 | Heightmap only (large-scale terrain, no biomes) | **In progress** — plateau + rolling hills + one mountain mass, no cliffs/terraces yet |
| 02 | Landmass (ocean/islands/coastline) | Not started — out of scope until multi-tile/ocean is prioritized |
| 03 | Terrain layers (large/medium/small feature scales) | Not started |
| 04 | Biomes (Meadow/Forest/Mountain/Swamp/Coast) | **Started early, height-only** — `BiomeClassifier` exists ahead of hydrology/terrain-layers being done; that's fine for a V0 stub but the multi-axis version belongs after 02-03 land |
| 05 | Water (lakes/rivers/streams/valleys) | **First slice started** — flat water-level plane in `OverworldTerrainMenu` (terrain below `waterLevel01=0.1` reads as lake). Ground baseline is a thin margin above water (`plateauLevels`, 0.13-0.19) with a gentle rolling-hills layer on top. Six deliberate lake sites (`lakeSpecs`: varied size 120-520m + cutoff threshold, random world-space offset per site, combined via max) are subtracted (opacity 0.7) so multiple lakes of varying size reliably reach below water — earlier passes either flooded almost everything (thin margin, one noise-threshold lake layer), floated entirely clear of the water plane (wide margin), or left shallower sites as a dry crater instead of a filled lake (opacity too low for their peak mask value). No flow simulation / carved river valleys yet, that's the next slice |
| 06 | Terrain materials (grass/dirt/rock/sand/mud/snow) | Not started |
| 07 | Vegetation (trees/bushes/grass/flowers/rocks) | **First slice started** — `VegetationScatterMenu` scatters Low Poly Environment prefabs per biome (WorldSeed-forked random points + per-biome acceptance probability + weighted prefab pick), raycast-placed on the real terrain surface, skipped underwater. No density-map asset or slope awareness yet |
| 08 | POIs (caves/ruins/resource areas) | **First slice started (W0.6)** — `StubPoiPlacer` (`Assets/Scripts/World/Poi/`) places 3-5 markers (Ruin/Camp/Shrine/TownApproach/CaveMouth) via `WorldSeed.Fork("poi")` + per-kind biome preference, one marker per chunk max, EditMode-tested with a fake sampler. `PoiMarkerMenu` visualizes them as colored cubes in the lab scene. No POI Director lifecycle (`Q.3`) yet — these are geometric markers only |
| 09 | Roads (graph + terrain cost + river crossings + mountain passes) | Not started |
| 10 | Settlements (location → type → districts → road network) | Not started, deferred by `16`/`17` |
| 11 | Buildings (Building DNA / Piece-Based Assembly) | Not started, deferred by `15` |
| 12 | Streaming (chunks, async, LOD, unload) | `ChunkCoord` math done; async/LOD not started |

**Where we actually are:** partway through Iteration 01, with a piece of Iteration 04 (height-only biome bands) done early because the Editor lab tool made it cheap to try. That's an intentional shortcut for hands-on iteration, not a claim that Iteration 04 is "done" — moisture/slope/temperature axes and real hydrology (Iteration 05) still need to land before biome classification is trustworthy.

---

## Related Documents

- [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md)
- [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)
- [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)
- [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)
