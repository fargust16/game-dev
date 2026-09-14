# Overworld Terrain Architecture

**Status:** Authoritative — the one description of how the overworld terrain is produced.
**Applies to:** Track W (World Gen), terrain.
**Related:** [PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md) (the generator),
[WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) (the pipeline steps around it),
[OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) (look and surface intent),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) (inland water).

---

## 1. The pipeline

```
seed
  │
  ▼
TerrainGen ─────────────── authoritative relief (Stage 1 landmass + Stage 2 massif uplift)
  │                        TerrainAuthorityField → StructuralElevation
  ▼
OverworldTerrainField ──── one world-spanning vertex lattice: height, uplift, coast distance,
  │                        then slope, convexity, drainage — all world units
  │
  ├── hydrology carve ──── optional, lowers the lattice (HydrologyBake.ApplyPendingCarve)
  ▼
Unity TerrainData ──────── OverworldTerrainBuilder
  ├── heightmap            SetHeights
  ├── surface weights      SetAlphamaps      ← TerrainSurfaceRules
  ├── tree instances       SetTreeInstances  ← TerrainVegetationRules
  └── detail layers        SetDetailLayer    ← TerrainVegetationRules
  │
  ▼
Unity Terrain ──────────── URP TerrainLit, five canonical TerrainLayers
```

There is **one** production path. No fallback, no alternate bake, no external terrain generator and
no compatibility layer. Nothing reads `TerrainData` back into TerrainGen.

**Terrain authority.** TerrainGen owns the relief. `TerrainAuthorityField` builds Stage 1, Stage 2A
and the accepted massifs exactly as the generator does and evaluates
`StructuralElevation.At = stage1.HeightAt + uplift.UpliftAt(signed coast distance)`. The bake changes
the *sink* of that field, never the field: the canonical seed's field hash is pinned by test.
TerrainGen's Stage 3C/3D/3E topology is frozen and is not read by the bake.

**Unity owns the representation.** Heights, weights, trees and details live in `TerrainData` and are
rendered by Unity's own terrain systems. Every downstream consumer — roads, POIs, settlements, water
samplers, hydrology analysis, player spawn — reads the terrain through `OverworldTerrainAccess` /
`OverworldTerrainSampler`, never through the bake.

## 2. Tile model

| | value | source |
|---|---|---|
| tiles | 2 × 2 | `WorldScale.OverworldTileCountX/Z` |
| tile size | 1400 m | `WorldScale.OverworldTileMeters` |
| world | 2800 × 2800 m | |
| height budget | 700 m (`TerrainData.size.y`) | `WorldScale.TerrainHeightBudgetMeters` |
| sea level | 24 m | `WorldScale.SeaLevelMeters` |
| heightmap | 513 per tile — 2.734 m per vertex | `OverworldTerrainGrid.DefaultHeightmapResolution` |
| alphamap | 512 per tile | `OverworldTerrainGrid.DefaultAlphamapResolution` |
| details | 512 per tile, 16 per patch, instance-count mode | `OverworldTerrainGrid` |
| pivot | lower-left corner at `(x·1400, 0, z·1400)` | |

`OverworldTerrainGrid` creates the tiles, colliders and neighbour links and stamps
`OverworldTerrainTile` on each; it never writes a height. It sets `heightmapResolution` **before**
`size`: Unity keeps metres per sample when the resolution changes, so a 1400 m tile made at the default
33 samples and raised to 513 afterwards comes back 22 400 m wide. Generated tile data lives in
`Assets/Settings/Terrain/OverworldTerrainData_{x}_{z}.asset`. Every bake refills an existing asset in place
(`OverworldTerrainGrid.Configure`) instead of replacing it, so the asset keeps its GUID and a saved scene's
references stay valid across re-bakes.

**Seams are exact by construction.** The lattice is world-spanning, so a tile-edge vertex is one lattice
point shared by both tiles. Alphamap texels are placed at `i / (resolution − 1)` — Unity's own splat
sampling — so a tile's last texel and its neighbour's first are the same world point. Every surface
and vegetation rule is a function of world position alone. There is no stitching pass.

**Precision.** Heights store at 16 bits over half the normalised range: one step of a 700 m tile is
2.14 cm, and a written height is rounded to within 1.07 cm of the field (measured worst case 1.069 cm).
Splat weights store at 8 bits (±1/255).

## 3. Surface classification

`TerrainSurfaceClassifier` evaluates `TerrainSurfaceRules` at every alphamap texel. The rules are the
project's own morphology rules — **shore → sand, open land → grass, drainage → soil, ridges / high
massif ground / bare steep ground → rock, sheer faces → cliff** — applied in that priority order, each
covering the ones before it by its mask, so the five weights always sum to one.

Every quantity is a world quantity. There is no normalised height and no per-biome vertical scale.

| layer | rule | values | basis |
|---|---|---|---|
| Sand | ground below the vegetation floor, seabed included; ±1 m low-frequency shore noise | full below 27.0 m, none above 28.4 m | **measured** — the island's authored bare-shore band |
| Soil | D8 upstream drainage area on the depression-filled surface; dry land above the shore, slope ≤ 30° | 20 000 → 150 000 m² | **authoring default** |
| Rock (seabed) | sloped seabed, only below the waterline | slope 2° → 4°; full below 22 m, none at 24 m | **measured** — the pre-migration underwater rock and the verified coastal-rock reference |
| Rock (massif) | massif ground (uplift 2 → 25 m) that is high (110 → 230 m, ±20 m noise) or a ridge (convexity 1.5 → 5 m over a 20 m radius) | as listed | **authoring default** |
| Rock (steep) | dry ground past the angle of repose | 35° → 42° | **authoring default**, physical basis |
| Cliff | sheer faces on dry land | 50° → 58° | **authoring default** |

**Coastal rule.** Coastal land never receives seabed rock: the seabed rock mask is zero at the 24 m
waterline, and land slopes are classified in true degrees.

Layer assets: `Assets/Settings/TerrainLayers/Overworld/Overworld_{Grass,Sand,Soil,Cliff,Rock}.terrainlayer`
in `TerrainSurfaceLayer` order, textures under `Assets/Art/Terrain/SurfaceLayers/`. Appearance is an
Inspector property of the layer asset and is never set from code. Roads and settlements append their
own layers after the bake and never reorder or repaint indices 0–4. Terrain material:
`Assets/Settings/OverworldTerrain.mat`, URP `Terrain/Lit`.

## 4. Trees

`OverworldTerrainBuilder.PlaceTrees` places at most one tree per 7 m world cell. The candidate's
position jitter, acceptance, species, scale and rotation are hashes of the world cell, so a cell gets
the same tree whichever tile or order evaluates it; each cell belongs to exactly one tile.

| rule | values | basis |
|---|---|---|
| vegetation floor | 27.0 → 28.4 m | **measured** |
| ground | grass weight + ½ soil weight at the point; nothing on sand, rock or cliff | rule |
| slope | fades from 20°, none at 28° | **authoring default** |
| groves | value noise at 140 m, mask 0.42 → 0.62, drainage (8 000 → 60 000 m²) lifts it by 0.25 | **authoring default** |
| probability cap | 0.65 | **authoring default** |
| species | massif uplift 8 → 30 m becomes pine; lowland pine where the 220 m species noise exceeds 0.62; otherwise fruit | **authoring default** |
| variants | pale pine 30 %; deep fruit 30 %, orchard 15 % | **authoring default** |
| scale | 0.85 → 1.15 | **authoring default** |

Prototypes: `OverworldVegetationAssets` — Polytope Lowpoly pine and fruit trees, with HSV-tinted
variants generated under `Assets/Settings/Terrain/Vegetation/Tints/`. Trees use the final height and
are snapped to the heightmap.

## 5. Ground cover

`OverworldTerrainBuilder.PlaceGrass` writes one detail layer: instances per 2.73 m cell =
`15 × vegetation gate × grass weight × patchiness`, patchiness 0.75 → 1.0 over 45 m noise, none on slopes
past 35°. The 15-per-cell peak is **measured** (the island plateau before the migration). Prototype: the
Polytope `PT_Grass_02` mesh as a single-mesh prefab, `DetailRenderMode.VertexLit`, GPU instancing.

## 6. Hydrology boundary

Hydrology is a separate system and is off by default (`WorldGenSettings.RunHydrology`).

```
bake A (no hydrology) → HydrologyTerrainGrid.BuildFromScene → HydrologyPlanner → HydrologyDefinition
  → bake B: TerrainGen field → HydrologyBake.ApplyPendingCarve lowers the lattice
            → surface, trees and ground cover derived from the carved lattice
  → HydrologyBake.VerifyCarve compares A and B against the carve field
```

The carve is one-way. It is applied to the lattice downstream of TerrainGen and is never read back
into it; pass A does not know hydrology exists, so no hydrology setting can move a mountain.
`HydrologyCarveField.Rasterize` is evaluated over the lattice expanded by half a step, so its pixel
centres are lattice vertices.

## 7. Determinism

- TerrainGen's field is deterministic per seed; the canonical seed's field hash is pinned by test.
- The bake uses no `UnityEngine.Random`, no time, no hash-set iteration. Noise and per-cell randomness
  come from `TerrainHash` stream keys forked from the world seed (`WorldSeed.Fork`) and are stateless.
- The same seed produces bit-identical heights, alphamaps, tree instances and detail layers in every
  process. `OverworldTerrainValidation.RunBatch` records the per-tile hashes; compare fresh processes.

## 8. Bake order

`OverworldTerrainMenu.GenerateSurfaceRoutine` → clear markers, roads, settlement, player and water →
`OverworldTerrainBake.Bake`:

1. clear the terrain grid and any root scene object left with only missing scripts;
2. create the grid with layers and material; make each `TerrainData` an asset **before** writing into
   it — an existing tile asset is reset and reused, a missing one is created (an asset made after painting
   comes back with fresh splat textures and loses the painted weights);
3. bind tree and detail prototypes;
4. evaluate TerrainGen onto the lattice; apply a pending hydrology carve;
5. heights → derived fields → alphamaps → trees → ground cover;
6. save the tile data; apply view distances (details 250 m, trees 5000 m, pixel error 1, basemap 20 km).

Then the ocean plane is restored and the pipeline continues with hydrology, markers, settlements, roads
and the player (`WORLD_GENERATION_PIPELINE.md`).

## 9. Versioning

Generated terrain is not versioned independently: it is a pure function of the seed, TerrainGen's
settings and the two rule sets. Changing a rule value changes every bake that reads it, which is why
each value is labelled measured or authoring default. The scene stores the last bake; the tile data
assets and vegetation assets are gitignored and regenerated, so a fresh clone runs Generate All before the
scene's terrain has data.

## 10. Testing

`OverworldTerrainBakeTests`:

- TerrainGen field reproduces the canonical hash; `Sample` equals `HeightAt`; island-only has no uplift;
- written heights match the field within 16-bit quantisation; seams exact; tile bounds;
- classifier weights sum to one; seabed rock allowed at 22 m and suppressed at 24 m and on coastal land;
  slope is true world slope (a 10° plane reads 10°);
- alphamaps deterministic, valid and seamless;
- trees deterministic, valid prototypes, standing on the final terrain above the vegetation floor;
- ground cover deterministic and only above the vegetation floor;
- the carve lowers only requested vertices and refreshes derived quantities;
- the full production bake of the canonical seed: four tiles, only Unity and project components, layers,
  material, trees, ground cover, heights matching TerrainGen.

Fresh-process determinism and timings: `OverworldTerrainValidation.RunBatch` per seed per process;
`-terrainFromScene true` hashes the tiles a saved scene loads from disk instead of baking.

## 11. Authoring decisions still open

The rules above carry authoring defaults where the project has a rule but no number. They are listed
for art review, not as defects: soil drainage area; massif rock height and ridge convexity; the steep
rock and cliff slope bands, which decide how much of the steep massif flanks reads as cliff rather than
rock; grove size and density; species split; tree and grass density. Soil as **erosion deposition**
is not modelled — the project has no erosion field — so soil follows the documented drainage rule.
