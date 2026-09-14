---
name: world-gen-assets
description: "Reference for which asset or project system to use for a given Overworld/Locomotion task — TerrainGen + the Unity-native terrain bake for terrain height, surface weights, trees and ground cover, URP TerrainLit for the terrain material, WaterWorks for the single WorldWater ocean plane, Free HDR Sky, Human Character Dummy + Human Basic Motions, PrimeTween, Polytope Low Poly Environment, MEC. Trigger when terrain, biomes, water, vegetation, locomotion, camera/UI motion, or chunk streaming is involved. Check ASSET_LIBRARY_REFERENCE.md first."
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
---

# World-Gen & Motion Asset Library

Which already-imported package or project system to reach for on Overworld V0 / Locomotion V0, so agents don't duplicate locked defaults or invent APIs for missing packages.

Source of truth: `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`.
**Terrain architecture and rules: `unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md` — read it before touching the terrain bake, surface rules, vegetation rules or terrain layers.**

## Decision table

| If the task is about... | Use | Status |
|---|---|---|
| Camera easing, UI transitions, procedural motion | **PrimeTween** (`com.kyrylokuzyk.primetween`) | Imported |
| Player/NPC locomotion clips | **Human Basic Motions** + **Human Character Dummy** (`HumanDummy_M White`) | Imported |
| Terrain tree / ground-cover content (prefabs) | **Low Poly Environment** (`Assets/Polytope Studio/...`) via `OverworldVegetationAssets` | Imported |
| Settlement road/plot dressing (bridge, fence only) | **Low Poly Village** — bridge+fence only, **not** a building kit | Imported, limited |
| Terrain relief (seed-driven) | **TerrainGen** — `TerrainAuthorityField` over `StructuralElevation` | **SoT** |
| Terrain height, surface weights, trees, ground cover in the scene | **Terrain bake** — `OverworldTerrainBake` → `OverworldTerrainBuilder` into Unity `TerrainData` | **SoT** |
| Where surfaces and vegetation go | **`TerrainSurfaceRules` / `TerrainVegetationRules`** — world-space data | **SoT** |
| Terrain material / PBR layers | **URP TerrainLit** + the canonical `TerrainLayer` assets (`OverworldTerrainLayers`) | **SoT** |
| MicroSplat | **Out of the pipeline** — Core only, no URP adapter, and its shader ignores `TerrainLayer.diffuseRemap*`/`tileSize`/`smoothness` | Imported, unused |
| Heightmap stamps | **Nothing** — terrain relief is TerrainGen's alone. StampIT is imported but out of the pipeline; a stamp pass would be a second height writer | — |
| Ocean water (V0 only) | **WaterWorks** (`Water_Plane` / `SSR_Water`) — one `WorldWater` at Sea Level | Imported, **SoT** |
| Legacy stylized water | **One Click Add Water** — demoted; unused | Imported, demoted |
| Sky / clouds | **Free HDR Sky** (`Assets/Skybox/`) | Imported |
| Chunk load/unload coroutine queuing | **More Effective Coroutines (MEC)** | Imported |
| MapMagic 2 | **Retired** — do not use | Delete leftover `Assets/MapMagic` if Unity unlocked the native DLL |

## Terrain rules

1. **One pipeline.** TerrainGen → Unity `TerrainData` → Unity Terrain. No fallback, no alternate bake,
   no compatibility layer, no external terrain generator. Stage 3C/3D/3E topology is frozen; the bake
   consumes TerrainGen's field and never changes it.
2. **World units only.** Every surface and vegetation threshold is metres, degrees or square metres on
   `TerrainSurfaceRules` / `TerrainVegetationRules`, labelled **measured** or **authoring default**. No
   normalised per-biome heights.
3. **Rules, not graphs.** Change a rule on those types; do not build a node graph, a generic rule engine
   or a parallel placer next to them.
4. **Deterministic by world cell.** Randomness comes from `TerrainHash` stream keys forked from the world
   seed and is evaluated statelessly per world cell, so tiles, tests and future chunks agree. Never
   `UnityEngine.Random`.
5. **Seams are exact by construction** — one world lattice, alphamap texels at `i / (resolution − 1)`,
   rules that depend on world position only. Do not add a stitching pass.
6. **Layers.** Five canonical assets in `TerrainSurfaceLayer` order (grass, sand, soil, cliff, rock),
   textures under `Assets/Art/Terrain/SurfaceLayers`. Appearance is an Inspector property of the layer
   asset; never set it from code. Roads and settlements append their own layers after the bake and must
   never reorder or repaint indices 0–4.
7. **Unity traps.** Make `TerrainData` an asset before writing alphamaps (afterwards the painted weights
   revert to the base layer); `SetDetailResolution` and the `heightmapResolution` setter discard data; a
   tree prototype needs an LOD Group or Mesh Renderer on the prefab root; details are invisible past
   `detailObjectDistance` (default 80 m, max 250 m); heights are 16-bit and splat weights 8-bit.

Diagnose terrain with `GameDev/Overworld/Terrain/Dump Terrain Geometry` and
`OverworldTerrainValidation.RunBatch` (hashes, seams, height error, timings) before changing a rule.

## Rules

1. **Never guess an unimported package's API.** Flag gaps; use documented fallbacks.
2. **Don't duplicate** WaterWorks / Dummy / PrimeTween when they already cover the need; for terrain,
   the bake and its rule types are the only writers.
3. **Low Poly Village is not a building kit** — only Bridge and Fence. Building gen is [15].
4. Height sample = Unity `Terrain.SampleHeight` / `OverworldTerrainSampler`. **No MapMagic DirectMatrices.**
5. Seed forks: `WorldSeed.Fork(...)` / `TerrainHash.StreamKey(...)`.
6. V0 water = ocean only (`SeaLevelOceanSampler`); inland hydrology is the separate `Hydrology` step,
   off by default.
7. Update `ASSET_LIBRARY_REFERENCE.md` when import status changes; keep this skill thin.
8. Asset Store packs may be Built-in RP — convert URP carefully (prefer shipped URP variants). Never project-wide convert.
