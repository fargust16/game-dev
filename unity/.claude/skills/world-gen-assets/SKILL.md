---
name: world-gen-assets
description: "Reference for which third-party asset package to use for a given Overworld/Locomotion task — Vista Personal for terrain/biomes, MicroSplat+StampIT for terrain materials/height stamps, WaterWorks for the single WorldWater plane, Free HDR Sky, Human Character Dummy + Human Basic Motions, PrimeTween, Polytope Low Poly Environment, MEC. Trigger when terrain, biomes, water, vegetation, locomotion, camera/UI motion, or chunk streaming is involved. Check ASSET_LIBRARY_REFERENCE.md first."
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
---

# World-Gen & Motion Asset Library

Which already-imported third-party package to reach for on Overworld V0 / Locomotion V0, so agents don't duplicate locked defaults or invent APIs for missing packages.

Source of truth: `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`.
Natural island geography: `unity/Assets/Plans/NATURAL_ISLAND_GENERATOR.md`.

## Decision table

| If the task is about... | Use | Status |
|---|---|---|
| Camera easing, UI transitions, procedural motion | **PrimeTween** (`com.kyrylokuzyk.primetween`) | Imported |
| Player/NPC locomotion clips | **Human Basic Motions** + **Human Character Dummy** (`HumanDummy_M White`) | Imported |
| Terrain vegetation/prop scatter | **Low Poly Environment** (`Assets/Polytope Studio/...`) | Imported |
| Settlement road/plot dressing (bridge, fence only) | **Low Poly Village** — bridge+fence only, **not** a building kit | Imported, limited |
| Terrain height + biomes (seed-driven) | **Vista Personal** + `IslandNaturalGeography` — `VistaManager` / `NaturalIsland_TerrainGraph` / `VistaTerrainSampler` | Imported, **SoT** |
| Terrain materials / PBR layers | **MicroSplat Core** (+ free **MicroSplat URP** adapter when available; else URP TerrainLit + MS example maps) | Core imported; URP adapter gated |
| Heightmap stamps / coastal & alpine detail | **StampIT** (`Assets/Rowlan/Terrain/Stamps/`) via `StampItTerrainDetail` | Imported |
| Ocean water (V0 only) | **WaterWorks** (`Water_Plane` / `SSR_Water`) — one `WorldWater` at Sea Level | Imported, **SoT** |
| Legacy stylized water | **One Click Add Water** — demoted; unused | Imported, demoted |
| Sky / clouds | **Free HDR Sky** (`Assets/Skybox/`) | Imported |
| Chunk load/unload coroutine queuing | **More Effective Coroutines (MEC)** | Imported |
| MapMagic 2 | **Retired** — do not use | Delete leftover `Assets/MapMagic` if Unity unlocked the native DLL |

## Rules

1. **Never guess an unimported package's API.** Flag gaps; use documented fallbacks.
2. **Don't duplicate** Vista / MicroSplat / WaterWorks / Dummy / PrimeTween when they already cover the need.
3. **Low Poly Village is not a building kit** — only Bridge and Fence. Building gen is [15].
4. Height sample = Unity `Terrain.SampleHeight` / `VistaTerrainSampler`. **No MapMagic DirectMatrices.**
5. Seed forks: `WorldSeed.Fork(...)` → `LocalProceduralBiome.seed` / geography placers.
6. V0 water = ocean only (`SeaLevelOceanSampler`); no inland lake/river generators.
7. Update `ASSET_LIBRARY_REFERENCE.md` when import status changes; keep this skill thin.
8. Asset Store packs may be Built-in RP — convert URP carefully (prefer shipped URP variants). Never project-wide convert.
