# Landscape Planner — Geometric Pipeline

**Status:** Draft  
**Date:** 2026-08-15  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md), `WorldScale`, `LandscapePlanner`, `WorldDefinition`

## Goal

Plan POIs / roads against shared terrain analysis, then conform road corridors once. One `WorldSeed` + identical settings must always reproduce identical geography.

**Hydrology (2026-08-15):** inland **lake and river generation is removed** from the codebase. V0 water = **one ocean plane** (`WorldWater` at Sea Level). Future inland hydro is a redesign, not a restore of `LakeDefinition` / `RiverTracer`.

## Derived seed streams

```
WorldSeed
 ├── terrain.* / biome / vegetation / vegetation.shrubs / grass / settlement / player.spawn
 ├── poi.{kind}.{index}  (tie-break / jitter for one marker)
 └── roads              (no RNG — deterministic cost field + A*)
```

Adding a tree must not reshape roads; adding a POI kind must not move existing TownApproach markers. Skipping a Part-Gen step must not advance another step's fork.

## Order

Editor executable order (also §5 in [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md)):

1. Init `WorldSeed` + `WorldGenSettings` → `WorldDefinition`
2. Surface height (terrain bake from TerrainGen; massifs included via `TerrainBakeFlags.IncludeMountains`)
3. Zones (alphamap / biome paint)
4. Mountains (the same terrain bake with the massif flag — cascade-clears downstream)
5. Terrain analysis (height, slope, curvature, roughness) via `LandscapeCostField`
6. POI placement once via `PoiPlacementEngine` + 3×3 `WorldSectorMap` → stable `Pois[]`
7. Settlement (TownApproach from recorded plan)
8. Growing Primary/Secondary roads → `TerrainDeformer` (road corridors only)
9. Ensure ocean `WorldWater` at Sea Level
10. POI marker visuals (no re-place)
11. Trees → Undergrowth → GPU grass (separate forks / clears)
12. Player at a seeded TownApproach (Human Character Dummy + Human Basic Motions)

## Key types

| Type | Path | Role |
|------|------|------|
| `WorldDefinition` | `World/` | Aggregate: seed, settings, POIs, roads, stats |
| `WorldGenSettings` | `World/` | Road/POI knobs + pipeline step mask for UI |
| `OverworldWorldState` | `World/` | Scene holder for the current `WorldDefinition` |
| `LandscapeCostField` | `World/Landscape/` | Analysis + nonlinear road cost + existing-road discount |
| `LandscapePlanner` | `World/Landscape/` | Cost + route orchestration |
| `PoiPlacementProfile` / `PoiPlacementEngine` | `World/Poi/` | Data-driven hard/soft/relation/footprint rules |
| `WorldSectorMap` | `World/Poi/` | Equal 3×3 sectors; town corners+center; habitable fallback |
| `RoadCorridorDeformer` | `World/Roads/` | Road/shoulder/blend conform |
| `RoadVerticalProfile` | `World/Roads/` | Grade / grade-change clamps |
| `TerrainDeformer` | `World/Landscape/` | Applies road corridors |
| `SeaLevelOceanSampler` | `World/Water/` | Ocean wetness via height ≤ Sea Level |
| `IWaterSampler` / `WaterProximity` | `World/Water/` | Placement exclusion against ocean |

## Road tiers

| Tier | Role | Generated now? |
|------|------|----------------|
| `Primary` | TownApproach ↔ growing town network | Yes |
| `Secondary` | Non-town POI → nearest good point on network | Yes |
| `Local` | Short stubs to buildings / props | Enum + widths only |

Primary and Secondary use the same attach primitive: min connection cost onto an existing polyline, then paint that corridor into the cost field so the next A* reuses the road instead of running parallel.

## POI sectors (3×3)

- Preferred TownApproach slots: NW / NE / Center(hub) / SW / SE
- Unsuitable sector (low `SectorHabitableRatio`) → nearest free habitable neighbor
- Secondary POI (camp / shrine / landmark) request preferred sectors round-robin
- Towns place first; Primary roads then connect the recorded town set

## Constraints

- `RoadDetourTolerance = 1.20` (prefer snap to existing road over independent parallel route)
- `maxCutDepth = 1.5 m`, `maxFillHeight = 1.0 m`
- Settlement / roads / POI visuals read `WorldDefinition.Pois` — never re-run the placer
- Ocean exclusion uses `SeaLevelOceanSampler` (no inland lake SDF)

## Editor entry

`GameDev/Overworld/Generate All` / `Clear All` — orchestrator (`OverworldPipeline`) with step skip mask.  
`GameDev/Overworld/Part-Gen/<Step>/Generate|Clear` — same step methods as Generate All.  
`GameDev/Overworld/World Generator` (`WorldGeneratorWindow`) — seed, knobs, **step checkboxes**, GENERATE WORLD.  
`GameDev/Overworld/Ensure Ocean Plane` — WaterWorks `WorldWater` only.

## Visual (dressing)

Bake order above is unchanged for non-hydro steps. Color / splat / road lawn / grass / ocean numbers live in [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md):

- Surface weights come from the terrain bake's world-space surface rules; roads paint packed bed + soft edge **after** the bake, appending their own layers without reordering or repainting canonical indices 0–4.
- Vegetation (placed by the terrain bake) and the WaterWorks SSR_Water **ocean** material read the same world after deform.
- Terrain render = **URP TerrainLit** over the five canonical `TerrainLayer` assets the terrain bake assigns. MicroSplat is out of the pipeline — do not reintroduce it.
