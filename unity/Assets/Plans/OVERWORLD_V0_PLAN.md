# OVERWORLD V0 — Micro-Plan

Status: Shipped — stabilization pass in progress
Created: 2026-08-07
Last Updated: 2026-08-17
Authority: [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)
Terrain contracts: [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md), [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md)

---

## Goal

A seeded, deterministic overworld on the post-pivot rails:

> WorldSeed → Vista terrain + surface weights + vegetation → POI plan → settlement → road network → player spawn

Vista is the **authoritative terrain-generation framework**. Our C# orchestrates and configures it;
it never reproduces or overrides Vista's generation.

---

## Locked defaults

| Item | Value | Owner |
|------|-------|-------|
| **Playable world** | **2800 × 2800 m** | `WorldScale.WorldExtentXMeters / ZMeters` |
| Terrain tiles | **2 × 2**, 1400 m each | `WorldScale.OverworldTileMeters`, `OverworldTileCountX/Z` |
| Terrain height budget | **700 m** | `WorldScale.TerrainHeightBudgetMeters` = `VistaManager.terrainMaxHeight` = tile `TerrainData.size.y` |
| Sea level | **24 m** | `WorldScale.SeaLevelMeters` |
| Chunk size | **64 m** | `WorldScale.ChunkSizeMeters` |
| Tile heightmap | 513 px → 2.73 m/px | `VistaManager.heightMapResolution` |
| Tile alphamap | 512 px → 2.73 m/px | `VistaManager.textureResolution` |
| **Vista biome canvas** | **512 px — Vista Personal hard cap** | `LocalProceduralBiome.baseResolution`, see below |
| Water | **Ocean only** — one WaterWorks `WorldWater` plane at sea level | `WaterZoneMenu` |
| Terrain material | URP TerrainLit over 5 canonical `TerrainLayer`s | `OverworldTerrainLayers` |
| Determinism | Same seed → same terrain, POIs, settlement, roads, spawn | — |
| SO usage | Immutable defs only; no runtime state in SO | — |

**2800 m is the playable V0 world, not a temporary generation canvas.** Any document or comment
still describing a ~512 m stub is stale — 512 refers to the Vista biome canvas in pixels, which is a
different quantity entirely.

### Vista Personal resolution limit (hard constraint)

Vista **Personal** caps `LocalProceduralBiome.baseResolution` at **512 px** and the tile heightmap
at 513 (`Pinwheel.Vista.Constants.RES_MAX` / `HM_RES_MAX`; nothing in the Personal package registers
the override callback). 1K/2K/4K need Indie or Pro.

**We are not buying Vista Indie.** No plan, graph or fix may depend on a higher biome resolution,
and *"raise the resolution"* is not an accepted answer to a terrain artifact. The correct question is
always: **what is the best terrain we can get inside 512 px?**

What that costs today:

| Biome | Bounds (area + 2 × falloff) | m/px at 512 |
|---|---|---|
| `LPB_IslandBase` | 2240 + 2 × 180 = 2600 m | **5.08** |
| `LPB_Mountain_n` | 1050 + 2 × 520 = 2090 m (X) | **4.08** |
| Tile heightmap (for comparison) | 1400 m at 513 px | 2.73 |

The island biome canvas is therefore **~1.9× coarser than the terrain heightmap it writes into**.
5.08 m/px is the real ceiling on island shape, and no graph node can beat it — every node runs at
graph resolution (`ResolutionOverrideOptions.RelativeToMainInput`, multiplier 1). Shrinking the
biome footprint is the only lever that buys m/px without a licence upgrade.

---

## Architecture (shipped)

```text
WorldSeed
  └─ Vista bake (one pass)
       VistaManager  terrainMaxHeight 700, 2×2 tiles of 1400 m
         ├── LPB_SeaFloor    order  0   SnapBiomeToTiles      worldHeight  20   Replace
         ├── LPB_IslandBase  order 10   SetBiomeArea 2240     worldHeight 140   Replace   Y = 24
         └── LPB_Mountain_n  order 20+  SetBiomeArea 1050×450 worldHeight 530   Raise
              → height + 5 layer weights + tree instances + detail density
  └─ POI plan        PoiPlacementEngine + PoiPlacementCatalog
  └─ Settlement      SettlementApproachBinder → OverworldSettlementAnchor
  └─ Roads           LandscapePlanner (cost field + hierarchical network) → RoadCorridorDeformer
  └─ Player          OverworldPlayerMenu
```

Biomes spawn from native Vista biome templates in `Assets/Settings/Vista/BiomeTemplates/`.
`OverworldLpbBake` sets **only** the seed, XZ placement and the anchor silhouette — never height,
area, order, `dataMask`, `blendOptions` or `localScale`.

### Seed forks

`terrain.seafloor`, `terrain.islandbase`, `terrain.islandring`, `terrain.coast.yaw`,
`terrain.mountain.place`, `terrain.mountain.{i}`, `terrain.mountain.ring.{i}`,
`poi.{kind}.{i}`, `settlement`, `player.spawn`, `poi-visual`.
Roads use no RNG — the cost field and A* are deterministic.

Changing one fork must not move another: re-seeding vegetation must not move the mountains.

---

## Non-goals

- Inland lakes and rivers — **removed from V0**; future hydrology is a redesign, not a restore
  (see [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md) §
  Future Hydrology / River Generation)
- MicroSplat — out of the pipeline; do not reintroduce
- A second terrain painter, height pass, tree placer or grass system
- Continuous enterable OrganicTown / TownGridBaker path
- Harvest / base-building
- Async chunk streaming, LOD budgets, infinite travel
- Monolithic `OverworldManager` owning all domains

---

## Acceptance criteria

1. Editor path: set seed → Generate All → Clear All → regenerate produces identical results.
2. Same seed twice → identical terrain heights, POI positions, settlement pose, road network,
   spawn point (EditMode tests + `Validate Mountain Bake Acceptance`).
3. Different seed → observably different island silhouette, massif placement and POI layout.
4. All four tiles report `size=(1400,700,1400)` and the **same** five-layer stack in canonical
   order, with per-pixel weight sum ≈ 1.000.
5. No axis-aligned banding, grid rills or contour rings in terrain or surface weights, judged at
   close, medium and distant views plus the four tile boundaries.
6. Vegetation (trees, bushes, grass, rocks) present and consistent on all four tiles.
7. POI markers land on every tile they were planned on; roads reach the settlement anchor and carry
   a road surface on every tile they cross.
8. Water is one ocean plane. No inland water generator exists.
9. No GridMap gameplay truth on the overworld.

---

## Editor entry points

| Menu | Purpose |
|------|---------|
| `GameDev/Overworld/Vista/First-Time Setup (graphs + templates)` | Fresh checkout: create graphs, biome and vegetation templates |
| `GameDev/Overworld/Generate All` / `Generate All (Default Seed)` | Full pipeline |
| `GameDev/Overworld/Clear All` | Tear down |
| `GameDev/Overworld/Part-Gen/<Step>/Generate|Clear` | One pipeline step |
| `GameDev/Overworld/World Generator` | Step checkboxes + knobs (`WorldGenSettings`) |
| `GameDev/Overworld/Vista/Validate Mountain Bake Acceptance` | Size / height / anchors / land% |
| `GameDev/Overworld/Vista/Dump Terrain Geometry` | Height, slope, curvature, banding report |
| `GameDev/Overworld/Vista/Dump Layer Weights` | Per-tile layer stack + weight histograms + PNGs |
| `GameDev/Overworld/Vista/Dump Vegetation State` | Per-tile vegetation chain, link by link |

---

## Tests

`unity/Assets/Scripts/Editor/Tests/`

| Test | Asserts |
|------|---------|
| `WorldSeedForkDeterminismTests` | Identical sequences per named fork |
| `ChunkCoordTests` / `OverworldTileChunksTests` | World points map stably at 64 m |
| `BiomeClassifierTests` | Same height/slope/moisture → same biome |
| `MountainBiomeBakeContractTests` | 700 m budget; island silhouette irregular, inscribed, seed-stable, scales with authored extent |
| `PoiPlacementEngineTests` / `StubPoiPlacerTests` | Marker counts and stability |
| `Road*Tests` | Cost field, pathfinder, simplifier, densifier, mesh, carver, surface paint |
| `OverworldVisualDirectionTests` | URP TerrainLit shader name, road soft-rim, settlement dust falloff |

---

## Related documents

- [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) — biome/LPB bake contract
- [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md) — the traps that are invisible in the API
- [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) — target pipeline shape
- [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) — look targets
- [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) — roads and terrain deformation
- [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) — known defects with measurements
