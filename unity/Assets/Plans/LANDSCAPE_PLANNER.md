# Landscape Planner — Geometric Pipeline

**Status:** Draft  
**Date:** 2026-08-14  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), `WorldScale`, `LandscapePlanner`, `WorldDefinition`

## Goal

Stop cutting terrain after the fact. Plan lakes / POIs / roads against shared terrain analysis, then conform terrain once. One `WorldSeed` + identical settings must always reproduce identical geography.

## Derived seed streams

```
WorldSeed
 ├── terrain.* / biome / vegetation / settlement  (existing)
 ├── lake.place          (candidate site selection only)
 ├── lake.{index}        (shape / orientation / depth of one lake)
 ├── poi.{kind}.{index}  (tie-break / jitter for one marker)
 └── roads              (no RNG — deterministic cost field + A*)
```

Adding a tree must not reshape lakes; adding a POI kind must not move existing TownApproach markers.

## Order

1. Init `WorldSeed` + `WorldGenSettings` → `WorldDefinition`
2. Base terrain (MapMagic height)
3. Terrain analysis (height, slope, curvature, roughness)
4. Lake candidates via `LakePlacementScore` + per-lake `lake.{i}` shapes → `LakeDefinition` plan (SDF only — no dig). Hard-reject mountain / high-slope / high height-range sites (`MountainMask` via slope+elevation mass).
5. POI placement once via `PoiPlacementEngine` + 3×3 `WorldSectorMap` (5 TownApproach = corners+center with habitable fallback) → write stable `Pois[]` into `WorldDefinition`
6. Settlement (TownApproach from recorded plan)
7. `LandscapeCostField` (analysis + lake SDF penalty + existing-road paint)
8. Growing road network: Primary (Prim among TownApproach) → Secondary (attach to network) — Local stubs deferred
9. Horizontal soften (`maxHeadingChange` / turn radius) + vertical profiles (`maxGrade` 0.12)
10. `TerrainDeformer`: analytic lake basins (`MaxLakeTerrainModificationMeters` dig cap) + road corridors
11. Water meshes from the same `LakeDefinition` (basin expanded, water overlaps shore by `WaterOverlapMeters`)
12. Rivers: highland sources → gradient descent → terminal **Lake or MapEdge only**; width `lerp(source,mouth,t^1.3)`; bank corridor carve
13. POI marker visuals (no re-place)
14. Vegetation last (exclusion masks from lakes / roads / POIs)

## Key types

| Type | Path | Role |
|------|------|------|
| `WorldDefinition` | `World/` | Aggregate: seed, settings, lakes, POIs, roads, stats |
| `WorldGenSettings` | `World/` | Lake/road/POI knobs for UI + generation |
| `OverworldWorldState` | `World/` | Scene holder for the current `WorldDefinition` |
| `LakeDefinition` | `World/Water/` | One SDF → basin, water, shore, road penalty, veg exclude |
| `LakeSilhouette` | `World/Water/` | Ellipse + LF terrainFactor + organic wobble |
| `LandscapeCostField` | `World/Landscape/` | Analysis + nonlinear road cost + existing-road discount |
| `LandscapePlanner` | `World/Landscape/` | Cost + route orchestration |
| `PoiPlacementProfile` / `PoiPlacementEngine` | `World/Poi/` | Data-driven hard/soft/relation/footprint rules |
| `WorldSectorMap` | `World/Poi/` | Equal 3×3 sectors; town corners+center; habitable fallback |
| `LakeBasinDeformer` | `World/Landscape/` | Analytic bowl (no noise floor); dig capped by `LakeMaxTerrainModificationMeters` |
| `RoadCorridorDeformer` | `World/Roads/` | Road/shoulder/blend conform |
| `RoadVerticalProfile` | `World/Roads/` | Grade / grade-change clamps |
| `TerrainDeformer` | `World/Landscape/` | Applies lakes then corridors |

## Road tiers

| Tier | Role | Generated now? |
|------|------|----------------|
| `Primary` | TownApproach ↔ growing town network | Yes |
| `Secondary` | Non-town POI → nearest good point on network | Yes |
| `Local` | Short stubs to buildings / props | Enum + widths only |

Primary and Secondary use the same attach primitive: min connection cost onto an existing polyline, then paint that corridor into the cost field so the next A* reuses the road instead of running parallel.

## Lake shape

```
LakeShape  = BaseEllipse(size, aspect 1.4–2.2, rotation)
           × TerrainInfluence(macro height/slope/basin × TerrainConformity)
           × Organic(low-freq harmonics 2–4 × ShapeIrregularity)
BasinShape = LakeShape expanded by LakeBasinExpandMeters
WaterShape = LakeShape expanded by WaterOverlapMeters (~1.75 m InsetWidth)
```

- Defaults: `ShapeIrregularity = 0.35`, `TerrainConformity = 0.75`
- `terrainFactor` scaled by conformity (~±25–30% at 0.75), still clamped so the result stays an ellipse
- `organic` low-frequency only (no harmonic-9 / fine Perlin shore noise)
- Rotation = blend(seed rotation, basin eigenvector)
- Placement uses `LakePlacementScore` (basin walls above floor + low slope + low variance + edge distance − excavation − mountain/cliff). Hard-rejects steep / mountain / mid-slope / near-edge sites — never excavate a mountain to force a lake
- Guaranteed **≥ 3 lakes** (`LakeMinCount`): size tiers Large → Medium → Small by area fraction (5–12% / 2–5% / 0.5–2% of tile); soft candidate pass if hard reject leaves fewer than 3
- `WaterLevel = AvgShoreHeight − max(WaterDepthOffset, WaterInsetDepth)` so the sheet sits under banks
- Water mesh expands by `WaterOverlapMeters` (~1.75 m); basin dig expands by `LakeBasinExpandMeters` (= overlap + buffer ~2.1 m)
- Shore **lip fill** in the overlap ring (band ≥ 2 heightmap texels, fill cap `LakeShoreLipFillMaxMeters`): raise downhill banks to `waterY + WaterInsetDepth` so the flat sheet tucks under terrain. Interior never fills (no dams / split ridges)
- Shore strip blur (`ShoreStripBlurMeters`, ~12 m, 5 passes) after carve so the bank is a ramp, not heightmap stairs
- River mouths overlap lakes by more cells; river bank dig keeps ≥ InsetDepth recess
- Hard reject when avg slope / max slope / high-terrain penalty / height range / mountain-cell ratio exceed `WorldScale.LakeReject*`
- Basin dig per sample: banks capped by `LakeMaxTerrainModificationMeters` (~18 m); **interior bowl** may dig up to `LakeInteriorMaxDigMeters` (~36 m), leftover islands are forced down
- Water mesh is a continuous flat SDF sheet at `WaterLevelY` — **not clipped to terrain**. Terrain conforms to the plane
- Roads do not fill/cut inside the water shore or the shore lip
- Lake count default **3** on a 1400 m tile (UI range 3–6)
- Default max depth ~22 m (tunable); analytic bowl profile 0–15% shore / 15–60% wall / 60–100% flat floor

### Rivers

- Sources require highland elevation + channel accumulation
- Every kept river terminates at **Lake** or **OffTile** (map edge) — Sink mid-terrain is discarded
- Width grows with arc-length `t^RiverWidthGrowthExponent` blended with flow accumulation (`RiverSourceWidthMeters` → `RiverMouthWidthMeters`)
- `RiverTerrainCarver` digs a soft bank shoulder of `RiverBankWidthMeters` beyond the water half-width

### POI sectors (3×3)

- Preferred TownApproach slots: NW / NE / Center(hub) / SW / SE
- Unsuitable sector (low `SectorHabitableRatio`) → nearest free habitable neighbor
- Secondary POI (camp / shrine / landmark) request preferred sectors round-robin so coverage spans the tile
- Towns place first; Primary roads then connect the recorded town set

UI label **Water Inset Width (m)** maps to `WaterOverlapMeters` (sheet expands under banks). Do not invert the sign.

## Constraints

- `RoadDetourTolerance = 1.20` (prefer snap to existing road over independent parallel route)
- `maxCutDepth = 1.5 m`, `maxFillHeight = 1.0 m`
- Basin = shore SDF + `LakeBasinExpandMeters`; water sheet expands by `WaterOverlapMeters` (under banks)
- Bridges: rivers only (never lakes)
- Settlement / roads / POI visuals read `WorldDefinition.Pois` — never re-run the placer

## Editor entry

`GameDev/Overworld/World Generator` (`WorldGeneratorWindow`) — seed field, Copy / Regenerate / GENERATE WORLD, foldouts, World Statistics.  
`GameDev/Overworld/Build Full Overworld` remains a shortcut into the same orchestrator.  
Standalone `Generate Water Zones` still place+carve+mesh for quick iteration; Full / GENERATE WORLD always defer dig until after roads.
