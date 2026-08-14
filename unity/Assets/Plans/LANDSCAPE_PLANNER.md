# Landscape Planner — Geometric Pipeline

**Status:** Draft  
**Date:** 2026-08-14  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), `WorldScale`, `LandscapePlanner`, `WorldDefinition`

## Goal

Stop cutting terrain after the fact. Plan lakes / POIs / roads against shared terrain analysis, then conform terrain once. One `WorldSeed` + identical settings must always reproduce identical geography.

## Derived seed streams

```
WorldSeed
 ├── terrain.* / biome / vegetation / vegetation.shrubs / grass / settlement / player.spawn
 ├── lake.place          (candidate site selection only)
 ├── lake.{index}        (shape / orientation / depth of one lake)
 ├── poi.{kind}.{index}  (tie-break / jitter for one marker)
 └── roads              (no RNG — deterministic cost field + A*)
```

Adding a tree must not reshape lakes; adding a POI kind must not move existing TownApproach markers. Skipping a Part-Gen step must not advance another step's fork.

## Order

Editor executable order (also §5 in [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md)):

1. Init `WorldSeed` + `WorldGenSettings` → `WorldDefinition`
2. Surface height (MapMagic; optional mountain layers via `TerrainBakeFlags`)
3. Zones (alphamap / biome paint)
4. Mountains (rebuild graph with ridge/masses/terrace flags — cascade-clears hydrology+)
5. Terrain analysis (height, slope, curvature, roughness)
6. Lake candidates via `LakePlacementScore` + **Mountain occupancy** (`BiomeId.Mountain` / steep highland on full silhouette) + per-lake `lake.{i}` shapes → `LakeDefinition` plan (SDF only — no dig in full run)
7. POI placement once via `PoiPlacementEngine` + 3×3 `WorldSectorMap` → stable `Pois[]`
8. Settlement (TownApproach from recorded plan)
9. `LandscapeCostField` + growing Primary/Secondary roads
10. `TerrainDeformer`: analytic lake basins + road corridors
11. Water meshes (safety-clip where terrain is above water **and** mountain-occupied)
12. Rivers: highland sources → Lake or MapEdge; bank carve
13. POI marker visuals (no re-place)
14. Trees → Undergrowth → GPU grass (separate forks / clears)
15. Player at a seeded TownApproach

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
- Placement uses `LakePlacementScore` (basin walls above floor + low slope + low variance + edge distance − excavation − mountain/cliff). Hard-rejects steep / mid-slope / near-edge sites
- **Mountain occupancy (hard):** any silhouette sample classified `BiomeId.Mountain` (or steep highland) rejects the site — score window alone is not enough (lakes are 95–155 m; a 12-cell score window misses ridge flanks). Soft-pass and `EnsureMinDistinct` **must not** violate occupancy. Prefer fewer lakes over a sheet through a ridge. `LakeMinCount` is a soft target, not an absolute force onto mountains
- Size tiers Large → Medium → Small by area fraction when sites allow (5–12% / 2–5% / 0.5–2% of tile)
- Prefer flatter ground: low slope / low variance dominate the score; center sits near local floor
- **Swamp tint:** if the lake's majority / center biome is `Swamp`, water mesh uses a murkier swamp material (not the clear lake tint)
- `WaterLevel = AvgShoreHeight − max(WaterDepthOffset, WaterInsetDepth)` so the sheet sits under banks
- Water mesh expands by `WaterOverlapMeters` (~1.75 m); basin dig expands by `LakeBasinExpandMeters` (= overlap + buffer ~2.1 m)
- Shore **lip fill** in the overlap ring (band ≥ 2 heightmap texels, fill cap `LakeShoreLipFillMaxMeters`): raise downhill banks to `waterY + WaterInsetDepth` so the flat sheet tucks under terrain. Interior never fills (no dams / split ridges)
- Shore strip blur (`ShoreStripBlurMeters`, ~12 m, 5 passes) after carve so the bank is a ramp, not heightmap stairs
- River mouths overlap lakes by more cells; river bank dig keeps ≥ InsetDepth recess
- Hard reject when avg slope / max slope / high-terrain penalty / height range / mountain-cell ratio exceed `WorldScale.LakeReject*`
- Basin dig per sample: banks capped by `LakeMaxTerrainModificationMeters` (~18 m); **interior bowl** may dig up to `LakeInteriorMaxDigMeters` (~36 m), leftover islands are forced down
- Water mesh is a flat SDF sheet at `WaterLevelY`. Terrain normally conforms to the plane; **safety clip** skips cells where `terrainY > waterY + epsilon` **and** the cell is mountain-occupied (stops a leaked SDF from cutting a ridge)
- Roads do not fill/cut inside the water shore or the shore lip
- Lake count default **3** on a 1400 m tile (UI range 3–6); may ship fewer when mountain occupancy leaves no valid basins
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

`GameDev/Overworld/Generate All` / `Clear All` — orchestrator (`OverworldPipeline`) with step skip mask.  
`GameDev/Overworld/Part-Gen/<Step>/Generate|Clear` — same step methods as Generate All.  
`GameDev/Overworld/World Generator` (`WorldGeneratorWindow`) — seed, knobs, **step checkboxes**, GENERATE WORLD.  
Part-Gen Lakes without roads: place+carve+mesh for quick iteration. Full Generate All always defers dig until after roads.
