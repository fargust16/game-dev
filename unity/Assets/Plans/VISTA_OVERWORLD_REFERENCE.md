# Vista Overworld Reference (Golden Scene)

**Status:** Draft  
**Date:** 2026-08-15  
**Scene SoT:** `Assets/Scenes/Overworld_Generator.unity`  
**Vista C# API SoT (official):** [Pinwheel.Vista namespace](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.html) — start at [VistaManager](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.VistaManager.html); LPB user guide: [Local procedural biome](https://docs.pinwheelstud.io/vista/docs/local-procedural-biome.html)  
**MicroSplat SoT:** [Core documentation](https://docs.google.com/document/d/1t_ZEKW8bHJqVWulH9Tcu-V5PIMr-sBOV_Ob7k2y1XY8/edit) + URP adapter `com.jbooth.microsplat.urp6p3`  
**Related:** [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md)

---

## Purpose

This document freezes **what actually looks good** and the **modular LPB bake contract**: a **2×2 × 1400 m** grid with `LPB_IslandBase` + `LPB_Mountain_*` under one VistaManager (height **700**). Hand-authored single MountainBiome covering the whole grid remains a visual reference for erosion/texturing only.

Automated `OverworldLpbBake` is the SoT for modular geology. Prefer official Vista API docs above when wiring from C#.

When wiring Vista from C#, prefer the **official API docs** above over guessing from package sources. Package code is implementation detail; docs are the contract.

**Out of scope here:** grass / detail foliage (we will replace Vista grass ourselves). Focus = **terrain height + Mountain biome graph + Vista Manager**.

---

## Scene inventory (golden)

| Object | Role |
|--------|------|
| `VistaManager` | Owns generate; `terrainMaxHeight = 500`, `heightMapResolution = 513`, `textureResolution = 512` |
| `Terrains` root | Unity Terrain grid |
| `Terrain_0_0` … `Terrain_1_1` | Positions `(0\|1400, 0, 0\|1400)`; tile size **1400×1400**, height **500** |
| `MountainBiome (Cloned from Template)` | Child of VistaManager — single covering LPB |
| Graph asset | `Assets/Scenes/Overworld_Generator Biomes/Mountain_TerrainGraph 1.asset` (clone of Mountain template, not our stripped NaturalIsland graph) |
| Terrain data | `Assets/Scenes/Overworld_Generator Terrains/Terrain_*_TerrainData.asset` |

Do **not** mutate package templates under `PinwheelStudio/Vista/Personal/BiomeTemplates/`. Work on scene clones / `Assets/Settings/Vista/` / `Assets/Scenes/Overworld_Generator Biomes/`.

---

## VistaManager settings (working)

| Field | Golden value | Notes |
|-------|--------------|-------|
| `terrainMaxHeight` | **700** (automated bake) | Vertical SoT for tiles + graph height normalization. Hand golden scene may still show 500 until re-baked. |
| `heightMapResolution` | 513 | Per-tile heightmap |
| `textureResolution` | 512 | Splat |
| `detailDensityMapResolution` | 512 | Ignore for now (grass later) |
| `shouldCullBiomes` | on | Default |

After changing max height, re-check `SetBiomeHeightOnSpawn.worldHeight` (see below) and each TerrainData `size.y`.

---

## Local Procedural Biome — Mountain (working)

Scene object: `MountainBiome (Cloned from Template)`.

### Transform

| Field | Golden value | Why |
|-------|--------------|-----|
| Position | `(500, 0, 500)` | Same as Vista MountainBiome prefab — local-space origin for template anchors |
| Scale XZ | `(1.05, …, 1.05)` | Template convention; expands authored polygon slightly |
| Scale Y | `1` when `worldHeight == terrainMaxHeight` | See SetBiomeHeightOnSpawn |

### LPB component

| Field | Golden value | Meaning |
|-------|--------------|---------|
| `terrainGraph` | scene clone `Mountain_TerrainGraph 1` | Full Mountain template graph (noise + dual `HydraulicErosionNode2` + texture/weight paths). **Not** `NaturalIsland_TerrainGraph`. |
| `space` | **Local (0)** | Graph `WORLD_BOUNDS` in biome-local space; remaps to world via transform |
| `seed` | **159** (editable) | Biome-level seed → graph `Args.SEED` (see Seed section) |
| `baseResolution` | **512** | Cache resolution in biome bounds (prefab default was 1024; 512 is fine for iteration) |
| `biomeMaskResolution` | 512 | Polygon mask RT |
| `dataMask` | `-1` (all) | Height + textures + population channels from graph |
| `order` | 0 | Only biome today |
| `anchors` | custom quad covering world **0…2800** after transform | Prefab default is `0…2000` local; scene was expanded so the biome covers the full 2×2 |
| `falloffDirection` | **Outer (0)** | Soft edge outside solid polygon |
| Falloff Distance | **380** local (~400 world) | Soft coast shelf; Vista docs: raise until coast is not a wall |
| Anchors | **32** + Chaikin smooth ×2 | Fewer hard polygon corners |
| `blendOptions.heightMapBlendMode` | **Replace (0)** | Single biome — Replace is correct |
| `blendOptions.useTransformForHeightBlend` | **true** | Required so Y scale from SetBiomeHeightOnSpawn affects height |

Approximate scene anchors when covering the full 2×2 (rectangle, local):

```text
(-476.19, 0, -476.19)
(-476.19, 0,  2190.48)
( 2190.48, 0,  2190.48)
( 2190.48, 0, -476.19)
```

With `pos=(500,0,500)` and `scale.xz=1.05` → world footprint ≈ **0…2800** on XZ.
For a **round / organic island**, replace this quad with many dragged anchors (see Island framing below).

### SetBiomeHeightOnSpawn

| Field | Golden | Rule |
|-------|--------|------|
| `worldHeight` | **500** | Target world metres of full graph height output |
| Formula | `localScale.y = worldHeight / VistaManager.terrainMaxHeight` | When both are 500 → Y scale = 1 |

`WorldScale.TerrainHeightBudgetMeters` is now **500** — keep Manager, tiles, and this component aligned.

---

## Island framing = Anchors + Falloff (Vista SoT)

**This is the island silhouette.** Not C# `IslandNaturalGeography`.

Vista builds a **biome mask** from the LPB polygon, then blends Mountain height into tiles with `HeightBlendMode.Replace` modulated by that mask:

| Mask | Result on terrain |
|------|-------------------|
| **1** (inside anchors) | Full Mountain graph height |
| **0** (outside falloff) | No biome contribution → height ~0 → ocean under `WorldWater` |
| **0…1** (falloff band) | Soft coast: height fades from full relief to ocean |

### Anchors (shape)

- Ordered closed polygon in **biome-local** space (then × transform → world).
- **Any shape**: square, circle-ish (many points), irregular coast — Scene gizmos; Insert/Remove via Vista biome tools.
- More vertices ≈ smoother circle / organic shore; 4 corners = rectangular landmass.
- Keep the island **inside** world 0…2800 so Outer falloff + ocean rim fit.

Round-island authoring:

1. Select `MountainBiome (Cloned from Template)`.
2. Add ~12–24 anchors and pull them into a ring.
3. Generate All — land only inside the ring.
4. Iterate shape, then tune falloff.

Automation: write an inset rectangle (4 corners) into `biome.anchors` — same Outer-falloff mechanism.

### Falloff Distance

- Metres each anchor offsets along its corner normal → `falloffAnchors` (`AnchorUtilities.GetFalloff`).
- **Larger** = wider soft coast; **0** = hard cookie-cutter edge.
- Default **50** = short beach; round islands often need **80–150+**; current bake uses **~380** for a soft shelf.
- With `scale.xz ≈ 1.05`, world fade ≈ `distance × 1.05`.
- Coarse N-gon corners stay visible even with large falloff — use **more anchors** (24–32) and optional Chaikin smooth.

### Falloff Direction

| Value | Effect |
|-------|--------|
| **Outer** (use for islands) | Solid land inside anchors; fade **outside** into ocean |
| **Inner** | Fade shrinks into the polygon (rarely for ocean) |

### Tuning order

1. Shape anchors until dry footprint is right.
2. Raise `falloffDistance` until coast is a shelf, not a wall.
3. Keep `WorldWater` at Sea Level 32 — outside mask (~height 0) sits underwater.
4. Re-Generate after every change.

### Do not

- Run old C# island framing / lake punch on a good Mountain+anchors bake.
- Cover the full world with falloff 0 and then C#-carve the coast.

---

## Multi-biome stack (mountains / lakes / fields) — official model

**SoT:** [Local procedural biome](https://docs.pinwheelstud.io/vista/docs/local-procedural-biome.html) + API [LocalProceduralBiome](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.LocalProceduralBiome.html), [IBiome](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.IBiome.html), [BiomeBlendOptions](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.BiomeBlendOptions.html), [VistaManager](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.VistaManager.html).

Vista does **not** need one mega-graph for every landform. Put several **Local Procedural Biome** children under one `VistaManager`. Each LPB has its own:

| Knob | Role |
|------|------|
| `terrainGraph` | Separate graph (Mountain template, plains/noise, lake depression, …) |
| `anchors` + `falloffDistance` / `falloffDirection` | Where this biome applies; soft edge blends into others |
| `order` | Higher order processed **later** (on top of earlier biomes) |
| `dataMask` | Which channels this biome emits (e.g. lakes: Height only; fields: Height + LayerWeightMaps) |
| `seed` | Per-biome `Args.SEED` |
| `blendOptions` | How this biome composites onto the running tile result |
| `space` | Prefer **Local** (`Space.Self`) so relocating the LPB keeps the same relative look |

`VistaManager.Generate*` / `GenerateAll`: for each tile, request overlapping biomes → blend by `order` + mask + `BiomeBlendOptions` → populate tile. `shouldCullBiomes` skips non-overlapping pairs.

### Height blend modes (API; docs UI names differ slightly)

| `HeightBlendMode` | Effect (B = this biome, A = prior blend) |
|-------------------|------------------------------------------|
| `Replace` | Masked overwrite (base landmass / mountain footprint) |
| `Raise` | Add height (docs: “Addictive”) — hills on top of base |
| `Lower` | Subtract (docs: “Subtractive”) — **lake bowls / basins** |
| `KeepHigher` / `KeepLower` | Max/Min; pair with texture/population `HeightWin` when useful |

Population: `Replace` (one ecosystem wins per pixel) vs `Coexist` (layer trees/grass). Textures: usually `Replace`; `HeightWin` only with KeepHigher/KeepLower.

### Recommended V1 stack (Geology → Hydrology → Ecology → Civilization)

```text
order 0     LPB_IslandBase   IslandPlateau graph + harmonic asymmetric anchors + SoftFalloff mask
                             Height Replace + LayerWeightMaps; plateau worldHeight ~55 m (ASL ~30–55)
order 10–19 LPB_Mountain_i   MountainMassif (ridge+foothills delta) + elongated ellipse anchors
                             Height Raise + wide Outer falloff (~520) + SoftFalloff mask
order 20–29 Lakes / Rivers   Lower (not spawned yet — scaffold only)
order 30–39 Forest / Meadow / Swamp / Fields   surface; height off or soft Lower for swamp
order 40–49 City / Village ground   textures (+ light flatten later)
```

**Automated bake** (`OverworldLpbBake`): IslandPlateau base + 1–3 Mountain LPBs with **Raise** (not KeepHigher stamp). Secondary menu: `Spawn Secondary LPB Scaffold` (orders 20–40).

**Coast:** after Vista Generate, `CoastalBeachShelf` applies a **directed ≤12°** height profile on low/mid shelves, then **SmoothCoastBand** (removes flood-ring terraces). High cliff-azimuth walls may stay steep. Sand splat uses **strong** shore weights (including underwater) + multi-pass shore↔grass feather; cliff/rock suppressed on coastal slopes &lt;58°. MicroSplat Convert **without** `_HYBRIDHEIGHTBLEND`. Island Outer falloff SoT = **420** local m; plateau ≈ **55 m** ASL.

**Mask shapes:** Island = radius harmonics (not near-circle); Mountain = irregular ellipse + yaw; never one universal circular mask for every biome type.

**Mountain graph contract:** periphery ≈ 0 (foothills), peaks small area, single `HydraulicErosionNode2`, TextureOutputs include foothill grass/soil so Texture Replace under Raise does not wipe greens.

---

## Seed — how generation depends on it

```text
LocalProceduralBiome.seed  (Inspector: "Seed")
        │
        ▼
TerrainGenerationConfigs.seed
        │
        ▼
Args.SEED  (graph execution argument)
        │
        ▼
Each seeded node:  effectiveSeed = Args.SEED XOR node.m_seed
  (NoiseNode, RidgedMountain, GenericMountain, HydraulicErosion*, Scatter, …)
```

- Changing **only** the biome `seed` reshuffles **all** node RNG streams that xor with `Args.SEED` → mountains, plains, erosion channels move together.
- Node-local `m_seed` values stay authored in the graph; they are relative offsets, not the world seed.
- Our old `NaturalIslandGraphBuilder.InjectSeeds` overwriting every node seed is **not** how the golden Mountain template is meant to be driven. Prefer: set `biome.seed` from `WorldSeed.Fork("terrain.islandbase")` and leave graph node seeds alone.

Re-generate after seed change: VistaManager **Generate All** (or editor generate on the manager).

---

## How mountains vs plains form (Mountain graph)

The working graph is the **Mountain biome template** clone (same family as `Mountain_TerrainGraphTemplate`), not the hand-built NaturalIsland strip.

Observed structure (height path):

1. **Noise / layered noise** — broad landform + variation (plains vs highland bias via remap/curves).
2. **HydraulicErosionNode2 ×2** — carve valleys / deposit; dual instances are part of the official template and run successfully at this footprint.
3. **Height / slope masks, Levels, Curve, Combine** — separate rock vs soft ground weight paths.
4. **HeightOutput** — final 0…1 height against `terrainMaxHeight`.
5. Texture / WeightBlend / Tree / DetailDensity outputs — visual layers; **ignore DetailDensity for our grass plan**.

Plains are not a separate C# biome: they are **low regions of the same heightfield** after erosion + remaps. Do not flatten with `IslandNaturalGeography` if the goal is “looks like the Mountain demo.”

---

## Terrain tiles (working)

| Rule | Detail |
|------|--------|
| Grid | 2×2 |
| Tile metres | 1400 × 1400 |
| Height | **700** (must match Manager `terrainMaxHeight`) |
| Positions | `(tx*1400, 0, tz*1400)` |
| Names | Unique per tile (`Terrain_0_0` …) — required by MicroSplat |
| Registration | Each tile has Vista `TerrainTile` with same `managerId` as VistaManager |
| Size pitfall | After setting `heightmapResolution`, **re-pin** `TerrainData.size` to `(1400, 700, 1400)` — Unity preserves m/px and can inflate size to 22400 |
| Shading | **MicroSplat Convert** (shared `MicroSplatData` + URP6.3) — [MicroSplat Core docs](https://docs.google.com/document/d/1t_ZEKW8bHJqVWulH9Tcu-V5PIMr-sBOV_Ob7k2y1XY8/edit) |
| Grass | **Polytope Lowpoly `PT_Grass_02`** via GPU `GrassField` / `GrassBakeMenu` (not Vista DetailDensity; VSP deprecated) |

---

## What we did wrong (vs this scene)

| Our automated path | Golden scene |
|--------------------|--------------|
| Custom `NaturalIsland_TerrainGraph` (stripped Noise+Ridge+single erosion) | Full **Mountain template** graph clone |
| `terrainMaxHeight` / tile Y = **220** | **500** |
| `Space.World` + metre anchors + `localScale=1` + falloff 0 | Template **Local** + prefab pos/scale + Outer falloff 50 + covering anchors |
| Heavy C# framing + lake carve after Vista | **Leave Vista height alone** (no pancake framing) |
| Mutating node seeds via InjectSeeds | Drive **`biome.seed` only** |
| Assumed dual Hydraulic always OOMs/clashes | Dual `HydraulicErosionNode2` works in Mountain template at 2×2 / 2800 m |

Root visual failures:

1. **220 m budget** crushed relief → pancake.
2. **Stripped graph** lacked Mountain template shaping.
3. **C# framing/lakes** punched holes and flattened coasts after a good Vista bake.

---

## Operator checklist (reproduce quality)

1. Open `Overworld_Generator` scene (do not Clear Terrain if inspecting the golden bake).
2. Confirm VistaManager max height **500** and four tiles **1400 / 500**.
3. Select `MountainBiome (Cloned from Template)` — graph points at scene Biomes clone; tweak **Seed** to explore layouts.
4. Generate All on VistaManager.
5. Shape **Anchors** for the island footprint; tune **Falloff Distance** (Outer) for the coast — see Island framing.
6. Grass: leave Vista detail off or clear later — custom grass pipeline TBD.

---

## Automated bake contract

Contract for `GameDev/Overworld/Generate Terrain From Seed` (`OverworldTerrainMenu`). Matches golden quality; Clear always re-spawns (do not rely on hand-placed LPB surviving Clear).

### Spawn

| Step | Rule |
|------|------|
| Template | `Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Mountain/Mountain_BiomeTemplate.asset` |
| API | `BiomeTemplateSpawner.Spawn(template, new BiomeTemplateSpawnContext(manager))` under VistaManager |
| Graph clone | Persist owned `TerrainGraph` clones from spawn context to `Assets/Settings/Vista/` (e.g. `MountainBiome_Mountain_TerrainGraph.asset`). **Never** edit package templates under `BiomeTemplates/`. |
| Transform | `Space.Local`, `localPosition=(500,0,500)`, `localScale=(1.05,1,1.05)` |
| Height | `VistaManager.terrainMaxHeight` = `SetBiomeHeightOnSpawn.worldHeight` = `WorldScale.TerrainHeightBudgetMeters` = **700** |
| Seed | `biome.seed = WorldSeed(seed).Fork("terrain.islandbase").Next()` only — leave graph node seeds |
| Sharp peaks | Working graph clone under `Assets/Settings/Vista/` — prefer ridge bias / less soft erosion; never edit package templates |

### Local ↔ world anchors

LPB transform as above. World point → local:

```text
local = biome.transform.InverseTransformPoint(world)
```

Local → world: `biome.transform.TransformPoint(local)`.

V1 island is an **inset rectangle** in **world XZ**, then converted to local. After assign: `falloffDirection=Outer`, `falloffDistance≈120`, `RecalculateFalloffAnchors()`.

### V1 island constants (inset square)

| Constant | Value |
|----------|-------|
| `anchorCount` | 4 (rectangle corners) |
| Margin (world) | `IslandOceanMarginMeters` (280) from world edge |
| Solid rectangle | `(margin … extent−margin)` on XZ inside 0…2800 |
| `falloffDistance` | 120 (local units; soft drowned rim into `WorldWater`) |
| `falloffDirection` | Outer |
| Shape | Square island — not N-gon ring |

### Post-pass matrix

| After Vista GenerateAll | V1 |
|-------------------------|----|
| SyncFromGpu | On |
| ForceTileResolutionAndSize (1400, 700, 1400) | On |
| LogRange | On |
| Shared TerrainLayers on all tiles | On |
| MicroSplat Convert (4 tiles as one system) + SyncAll | On |
| EnsureOceanSurface | On |
| `IslandNaturalGeography` | **Off** |
| `IslandOccupancy` boundary | **Off** |
| StampIT | **Off** |
| `SealLeakingBasins` | **Off** |
| Alpine/coast splat hints | **Off** |

### Population / vegetation

- Height + layer textures: **on** for base Mountain LPB (population instance outputs stripped on automated clone to avoid Vista transient double-register).
- Vista DetailDensity: **not** grass SoT.
- Grass SoT: **Polytope Lowpoly** `PT_Grass_02` → `GrassBakeMenu` / `GrassField` + shader `GameDev/Overworld/Grass` ([ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md)).
- Vegetation Studio Pro: **deprecated** for this project; Rowlan VSP Extensions stay under `Assets/_Disabled/`.

### Acceptance checklist

- Logs: `size=(1400,700,1400)` on all tiles (not 22400).
- Square landmass with soft drowned rim (Outer falloff); not circular N-gon; not ~100% land on every tile.
- Continuous Mountain-quality relief; sharper peaks than soft default.
- Shared MicroSplatData / URP shader on all four tiles after Convert.
- No C# lake punch-holes.
- Changing only `terrain.islandbase` seed changes mountain layout; silhouette stays square-class.
## Documentation checks

- Glossary **Vista Biome** / island framing points here.
- Height budget 500 locked in `WorldScale`.
- Island framing Open Question resolved: Anchors + Falloff.
