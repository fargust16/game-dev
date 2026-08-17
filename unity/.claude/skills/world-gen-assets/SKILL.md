---
name: world-gen-assets
description: "Reference for which third-party asset package to use for a given Overworld/Locomotion task — Vista Personal for terrain/biomes/surface-weights/vegetation, URP TerrainLit for the terrain material, WaterWorks for the single WorldWater ocean plane, Free HDR Sky, Human Character Dummy + Human Basic Motions, PrimeTween, Polytope Low Poly Environment, MEC. Trigger when terrain, biomes, water, vegetation, locomotion, camera/UI motion, or chunk streaming is involved. Check ASSET_LIBRARY_REFERENCE.md first."
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
---

# World-Gen & Motion Asset Library

Which already-imported third-party package to reach for on Overworld V0 / Locomotion V0, so agents don't duplicate locked defaults or invent APIs for missing packages.

Source of truth: `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`.
**Terrain traps and hard limits: `unity/Assets/Plans/VISTA_TERRAIN_PLAYBOOK.md` — read it before editing any terrain graph, biome template or vegetation template.**
Natural island geography: `unity/Assets/Plans/NATURAL_ISLAND_GENERATOR.md`.

## Decision table

| If the task is about... | Use | Status |
|---|---|---|
| Camera easing, UI transitions, procedural motion | **PrimeTween** (`com.kyrylokuzyk.primetween`) | Imported |
| Player/NPC locomotion clips | **Human Basic Motions** + **Human Character Dummy** (`HumanDummy_M White`) | Imported |
| Terrain vegetation/prop scatter | **Low Poly Environment** (`Assets/Polytope Studio/...`) | Imported |
| Settlement road/plot dressing (bridge, fence only) | **Low Poly Village** — bridge+fence only, **not** a building kit | Imported, limited |
| Terrain height, biomes, surface weights, vegetation placement (seed-driven) | **Vista Personal** — `VistaManager` / `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph` / `VistaTerrainSampler`. Biome canvas capped at **512 px**; design graphs around it, never plan on raising it | Imported, **SoT** |
| Terrain material / PBR layers | **URP TerrainLit** + the canonical `TerrainLayer` assets (`OverworldTerrainLayers`) | **SoT** |
| MicroSplat | **Out of the pipeline** — Core only, no URP adapter, and its shader ignores `TerrainLayer.diffuseRemap*`/`tileSize`/`smoothness` | Imported, unused |
| Heightmap stamps | **Nothing** — terrain height is Vista's alone. StampIT is imported but out of the pipeline; a stamp pass would be a second height writer | — |
| Ocean water (V0 only) | **WaterWorks** (`Water_Plane` / `SSR_Water`) — one `WorldWater` at Sea Level | Imported, **SoT** |
| Legacy stylized water | **One Click Add Water** — demoted; unused | Imported, demoted |
| Sky / clouds | **Free HDR Sky** (`Assets/Skybox/`) | Imported |
| Chunk load/unload coroutine queuing | **More Effective Coroutines (MEC)** | Imported |
| MapMagic 2 | **Retired** — do not use | Delete leftover `Assets/MapMagic` if Unity unlocked the native DLL |

## Vista graph authoring

Vista's own sample biome graphs are the **architectural reference**, not just examples of output.
Richest one: `Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Mountain/Mountain_TerrainGraphTemplate.asset`
(50 nodes). Others: `Samples/Graphs/MountainRange.asset`, `ErodedMountainRange.asset`, `MoundOfEarth.asset`.
Our graphs are authored in C# by `IslandPlateauGraphBuilder` / `MountainMassifGraphBuilder` over `VistaGraphBuild`,
but only **once**: the bake calls `Load()`, and `Rebuild()` is an explicit confirmed menu action that
discards Inspector edits.
Full node-by-node mapping: `unity/Assets/Plans/VISTA_OVERWORLD_REFERENCE.md` § Reference architecture.

1. **Search first.** Before implementing a biome, open the official Vista sample graph that solves the
   nearest problem and reuse its architecture and node strategy. Do not invent a procedural solution
   next to one the asset already ships.
2. **No decorative nodes.** Never add a node to make terrain look "more detailed" or the graph look
   more sophisticated. Before adding any noise / combine / multiply / curve / remap / mask / math node,
   answer: *what specific visual problem does this node solve?* No answer → no node.
3. **Do not override node defaults without a reason.** Vista's node defaults *are* the reference's
   values — `NoiseNode` ships 8 octaves with `flipSign` + `applyRemapPerLayer`, `HydraulicErosionNode2`
   ships the Macro preset. Set only what is genuinely per-biome (feature `scale`, `seed`). Substituting
   hand-tuned numbers at every node is how a graph ends up with the reference's topology and none of
   its look.
4. **No duplicated derivation.** Do not recompute noise, erosion, height or slope when a graph variable
   or an already-computed map carries the same signal. One erosion pass feeds every mask that needs it.
5. **Variables are the stage interface.** Use `Set Variable` / `Get Variable` between logical stages
   (`Height Map`, `ErosionMask`, `GreenAreaMask` — the reference graph's own names, kept verbatim in
   `VistaGraphBuild`). Stages are not wired to each other with long node chains.
6. **Graph paints, C# does not.** Surface distribution comes from the graph's `Weight Blend` →
   `TEXTURE OUTPUT`. There is no C# splat painter, and none may be reintroduced — no
   `Get/SetAlphamaps` pass, no `terrainData.terrainLayers` assignment, no `TerrainLayer` recolouring.
7. **Weight Blend semantics.** Output i = `in[i] * Π(1 - in[j])` for every j above i: the **last**
   connected layer wins and **layer 0 must stay unconnected** so Vista fills it white — that is the
   only thing making the five weights sum to 1, and nothing downstream normalizes them.
8. **Texture Output semantics.** `order = (int)OverworldTerrainLayers.SurfaceLayer`, and layers merge
   by `TerrainLayer` **asset identity**. Vista rewrites `terrainData.terrainLayers` **per tile** from
   the distinct layers the covering biomes produced, so **every graph emits the full canonical set**
   (unused ones simply unconnected → black). Skipping one makes tiles disagree on channel meaning.
9. **Altitude in normalized height.** Gate bands with `Curve`/`Levels` over the `Height Map`
   variable, not `HeightMaskNode` metres — metres are compared against the biome's own world height,
   so a Set Biome Height On Spawn change would silently move every band.
10. **Slope is for sheer faces.** The reference uses `SlopeMaskNode` once, as a flatness corrective.
   Two adjacent slope bands used as independent layers draw contour rings across rolling terrain.
11. **Graph places vegetation.** Trees and grass density come from `GreenAreaMask` → `Levels` /
   `Geometry Mask` → `Scatter` → `TREE OUTPUT` / `DETAIL DENSITY OUTPUT`, with our own
   `TreeTemplate` / `DetailTemplate` content (`VistaVegetationTemplates`). There is no second grass
   or tree placement system.
12. **Justify every deviation** from the reference in the builder's doc comment (e.g. one hydraulic
   erosion pass instead of two — two instances share temp RT names and produce "fangs"; the Polaris
   `DetailInstanceOutputNode` branch is skipped because Unity's `TerrainTile` never reads it).
13. **Never edit package templates** under `PinwheelStudio/.../BiomeTemplates/`. Clone into
   `Assets/Settings/Vista/`.

## Biome configuration

Biomes spawn from our own **native Vista biome templates** in
`Assets/Settings/Vista/BiomeTemplates/` (`LPB_IslandBase.prefab`, `LPB_Mountain.prefab`), authored
once by `OverworldBiomeTemplates`. Each prefab carries `LocalProceduralBiome` plus
**`SetBiomeHeightOnSpawn`** (world height) and **`SetBiomeAreaOnSpawn`** (world extent).

- Tune height, area, falloff, blend options, resolution and graph assignment **on the prefab**.
- `OverworldLpbBake` sets **only** seed, XZ placement and the anchor silhouette, which is inscribed
  inside the rectangle the area component authored — so changing the area still changes the size.
- C# must never write `worldHeight`, `worldArea`, `order`, `dataMask`, `blendOptions`,
  `baseResolution` or `transform.localScale` on a spawned biome.
- Prefer `SetBiomeHeightOnSpawn` / `SetBiomeAreaOnSpawn` / `SnapBiomeToTilesOnSpawn` over custom
  equivalents. Multiple LPBs are encouraged; each must be a native LPB.

Diagnose material problems with `GameDev/Overworld/Vista/Dump Layer Weights` before touching the
graph: it prints each tile's layer stack and the per-pixel weight sum (must be ≈1).

## Rules

1. **Never guess an unimported package's API.** Flag gaps; use documented fallbacks.
2. **Don't duplicate** Vista / WaterWorks / Dummy / PrimeTween when they already cover the need. For
   terrain this is absolute: Vista owns generation, C# only orchestrates and configures it.
3. **Low Poly Village is not a building kit** — only Bridge and Fence. Building gen is [15].
4. Height sample = Unity `Terrain.SampleHeight` / `VistaTerrainSampler`. **No MapMagic DirectMatrices.**
5. Seed forks: `WorldSeed.Fork(...)` → `LocalProceduralBiome.seed` / geography placers.
6. V0 water = ocean only (`SeaLevelOceanSampler`); no inland lake/river generators. Future hydrology is a
   redesign from first principles, not a restore of the deleted code.
7. **Vista Personal caps the biome canvas at 512 px** (`Constants.RES_MAX`) and the tile heightmap at 513.
   Every graph node runs at that resolution, and a biome's canvas spans `worldArea + 2 × falloffDistance`.
   Design graphs for it; a smaller biome footprint is the only sanctioned way to buy m/px. **Raising the
   resolution, or budgeting for Vista Indie, is not an accepted fix for a terrain artifact.**
7. Update `ASSET_LIBRARY_REFERENCE.md` when import status changes; keep this skill thin.
8. Asset Store packs may be Built-in RP — convert URP carefully (prefer shipped URP variants). Never project-wide convert.
