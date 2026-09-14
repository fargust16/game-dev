# OVERWORLD V0 — Micro-Plan

Status: Shipped — stabilization pass in progress
Created: 2026-08-07
Last Updated: 2026-09-13
Authority: [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
Terrain contract: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md)

---

## Goal

A seeded, deterministic overworld on the post-pivot rails:

> WorldSeed → TerrainGen relief → Unity terrain (heights + surface weights + vegetation) → POI plan → settlement → road network → player spawn

**TerrainGen is the terrain authority** and Unity `TerrainData` is the terrain representation. The
terrain bake writes TerrainGen's field into Unity's own heightmap, alphamaps, tree instances and detail
layers; it adds no relief and nothing reads the terrain back into TerrainGen.

---

## Locked defaults

| Item | Value | Owner |
|------|-------|-------|
| **Playable world** | **2800 × 2800 m** | `WorldScale.WorldExtentXMeters / ZMeters` |
| Terrain tiles | **2 × 2**, 1400 m each | `WorldScale.OverworldTileMeters`, `OverworldTileCountX/Z` |
| Terrain height budget | **700 m** | `WorldScale.TerrainHeightBudgetMeters` = tile `TerrainData.size.y` |
| Sea level | **24 m** | `WorldScale.SeaLevelMeters` |
| Chunk size | **64 m** | `WorldScale.ChunkSizeMeters` |
| Tile heightmap | 513 px → 2.73 m/px, 16-bit | `OverworldTerrainGrid.DefaultHeightmapResolution` |
| Tile alphamap | 512 px → 2.73 m/px, 8-bit weights | `OverworldTerrainGrid.DefaultAlphamapResolution` |
| Tile detail map | 512 px, instance-count mode | `OverworldTerrainGrid` |
| Water | **Ocean only** — one WaterWorks `WorldWater` plane at sea level | `WaterZoneMenu` |
| Terrain material | URP TerrainLit over 5 canonical `TerrainLayer`s | `OverworldTerrainLayers` |
| Determinism | Same seed → same terrain, POIs, settlement, roads, spawn | — |
| SO usage | Immutable defs only; no runtime state in SO | — |

**2800 m is the playable V0 world, not a temporary generation canvas.** Any document or comment
still describing a ~512 m stub is stale — 512 is the tile alphamap and detail resolution in texels, a
different quantity entirely.

The heightmap lattice is world-spanning: a tile-edge vertex is one lattice point shared by both tiles, so
seams are exact by construction. The smallest terrain feature is bounded by the 2.73 m vertex spacing;
there is no coarser intermediate canvas.

---

## Architecture (shipped)

```text
WorldSeed
  └─ Terrain bake (one pass)                          OverworldTerrainBake
       TerrainAuthorityField   TerrainGen Stage 1 island + Stage 2 massif uplift (StructuralElevation)
       OverworldTerrainField   one world lattice: height, uplift, coast distance, slope, convexity, drainage
       OverworldTerrainBuilder → 2×2 Unity TerrainData of 1400 m, size.y 700
              → heights + 5 layer weights + tree instances + detail layer
  └─ POI plan        PoiPlacementEngine + PoiPlacementCatalog
  └─ Settlement      SettlementApproachBinder → OverworldSettlementAnchor
  └─ Roads           LandscapePlanner (cost field + hierarchical network) → RoadCorridorDeformer
  └─ Player          OverworldPlayerMenu
```

Surface and vegetation rules are data on `TerrainSurfaceRules` and `TerrainVegetationRules`, in world
units. Layer assets come from `OverworldTerrainLayers`, tree and grass prototypes from
`OverworldVegetationAssets`. Full contract: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).

### Seed forks

TerrainGen: `terrain.crust*`, `terrain.coast.*`, `terrain.macro.*`, `terrain.range.*`, `terrain.massif.{i}`.
Terrain bake: `terrain.surface.shore`, `terrain.surface.rock`, `terrain.vegetation.{groves,species,trees,grass}`.
Placement: `poi.{kind}.{i}`, `settlement`, `player.spawn`, `poi-visual`.
Roads use no RNG — the cost field and A* are deterministic.

Changing one fork must not move another: re-seeding vegetation must not move the mountains.

---

## Non-goals

- Inland lakes and rivers in the default pipeline — the `Hydrology` step exists but is off by default
  (see [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md))
- MicroSplat — out of the pipeline; do not reintroduce
- A second terrain painter, height pass, tree placer or grass system
- An external terrain generator, a node-graph framework or a compatibility bake path
- Continuous enterable OrganicTown / TownGridBaker path
- Harvest / base-building
- Async chunk streaming, LOD budgets, infinite travel
- Monolithic `OverworldManager` owning all domains

---

## Acceptance criteria

1. Editor path: set seed → Generate All → Clear All → regenerate produces identical results.
2. Same seed twice → identical terrain heights, POI positions, settlement pose, road network,
   spawn point (EditMode tests + `OverworldTerrainValidation.RunBatch` hashes in fresh processes).
3. Different seed → observably different island silhouette, massif placement and POI layout.
4. All four tiles report `size=(1400,700,1400)` and the **same** five-layer stack in canonical
   order, with per-pixel weight sum ≈ 1.000.
5. No axis-aligned banding, grid rills or contour rings in terrain or surface weights, judged at
   close, medium and distant views plus the four tile boundaries.
6. Vegetation (trees and ground cover) present and consistent on all four tiles.
7. POI markers land on every tile they were planned on; roads reach the settlement anchor and carry
   a road surface on every tile they cross.
8. Water is one ocean plane unless the `Hydrology` step is enabled.
9. No GridMap gameplay truth on the overworld.

---

## Editor entry points

| Menu | Purpose |
|------|---------|
| `GameDev/Overworld/Terrain/Rebuild Canonical Terrain Layers` | Recreate the five canonical `TerrainLayer` assets |
| `GameDev/Overworld/Vegetation/Rebuild Vegetation Assets` | Recreate tree and grass prototypes and tinted variants |
| `GameDev/Overworld/Generate All` / `Generate All (Default Seed)` | Full pipeline |
| `GameDev/Overworld/Clear All` | Tear down |
| `GameDev/Overworld/Part-Gen/<Step>/Generate|Clear` | One pipeline step |
| `GameDev/Overworld/World Generator` | Step checkboxes + knobs (`WorldGenSettings`) |
| `GameDev/Overworld/Terrain/Dump Terrain Geometry` | Height, slope, curvature, banding report |
| `OverworldTerrainValidation.RunBatch` (batch mode) | Per-tile height / alphamap / tree / detail hashes, seams, height error vs TerrainGen, timings |

---

## Tests

`unity/Assets/Scripts/Editor/Tests/`

| Test | Asserts |
|------|---------|
| `WorldSeedForkDeterminismTests` | Identical sequences per named fork |
| `ChunkCoordTests` / `OverworldTileChunksTests` | World points map stably at 64 m |
| `BiomeClassifierTests` | Same height/slope/moisture → same biome |
| `OverworldTerrainBakeTests` | TerrainGen field hash; heights within 16-bit quantisation; exact seams; weight sums; coastal rock rule; true slope; deterministic trees and grass; carve; full production bake |
| `TerrainBakeFlagsTests` / `TerrainLayerBudgetTests` | Bake flags; layer budget |
| `PoiPlacementEngineTests` / `StubPoiPlacerTests` | Marker counts and stability |
| `Road*Tests` | Cost field, pathfinder, simplifier, densifier, mesh, carver, surface paint |
| `OverworldVisualDirectionTests` | URP TerrainLit shader name, road soft-rim, settlement dust falloff |

---

## Related documents

- [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) — terrain bake contract
- [PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md) — TerrainGen
- [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) — target pipeline shape
- [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) — look targets
- [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) — roads and terrain deformation
- [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) — known defects with measurements
