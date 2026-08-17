# Third-Party Asset Library Reference

**Status:** Draft
**Date:** 2026-08-15
**Applies to:** Overworld V0 and later world-gen / locomotion waves ([17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md))
**Related:** [OVERWORLD_V0_PLAN.md](OVERWORLD_V0_PLAN.md), [PROCEDURAL_TOWN_ASSET_LIBRARY.md](PROCEDURAL_TOWN_ASSET_LIBRARY.md) (pre-pivot dressing policy, still useful for shell/interior materials)

---

## Purpose

Registry of third-party packages added to the project and **which System Card / Track in [17](../../../17%20Game%20Systems%20Architecture.md) each one serves**. An agent picking up a terrain, vegetation, animation, or UI-motion task should check this table before reaching for a different tool or writing custom code — these packages are the locked defaults for their domain.

Every row is verified against what is actually present on disk at the date above, not the asset's marketing description. Status `Not imported` means: do not write code against it yet, and do not assume its class names — import first, then update this row with the real API surface.

---

## Registry

| Asset | Status | Location in project | Serves (Track / System Card) | Role |
|-------|--------|---------------------|-------------------------------|------|
| **PrimeTween** | Imported | `Packages/com.kyrylokuzyk.primetween.tgz` (package id `com.kyrylokuzyk.primetween`) | `L.3 Camera`, `L.4 Animation`, `X.6 UI` | General-purpose tween/sequence library (allocation-light, no coroutines needed). Default tool for camera easing, UI transitions, procedural motion (e.g. portal fade, POI marker bob) — not a world-gen tool. |
| **Human Basic Motions** (Kevin Iglesias) | Imported | `Assets/Kevin Iglesias/Human Animations/` — `Animations/Male/`, `Animations/Female/`, `Animations/Masked Poses/` (118 FBX clips total), `Models/HumanF_Model.fbx` / `HumanM_Model.fbx`, `Models/Avatar Masks/` (Full/Upper/Head masks), `Scripts/SpineProxy.cs` | `L.4 Animation (locomotion)`, later `A.1/A.2 NPC` | Default **locomotion clip** source for overworld player + later NPCs (walk/run/idle/jump). Avatar masks for upper-body overlay. **Mesh:** overworld player uses **Human Character Dummy** (below), not `HumanM`/`HumanF` and not Char_UEZ. |
| **Human Character Dummy** (Kevin Iglesias) | Imported, **in use** | `Assets/Kevin Iglesias/Human Character Dummy/` — prefab `Prefabs/HumanDummy_M White.prefab`, FBX `Models/HumanCharacterDummy_M.fbx` | `L.4 Animation (locomotion)` | Default **overworld player mesh**. Wired by `OverworldPlayerMenu` + Human Basic Motions. Char_UEZ is **not** used for the overworld player. |
| **Low Poly Environment** (Polytope Studio) | Imported, **in use** | `Assets/Polytope Studio/Lowpoly_Environments/Prefabs/` — `Trees/`, `Rocks/`, `Plants/`, `Shrubs/`, `Flowers/`, `Mushrooms/` | `W.2 Terrain/Biome`, `W.4 Building/Shell` (dressing) | Vegetation prefab source for **trees/shrubs/rocks/grass**, all placed by the terrain graphs. Carpet grass reuses `Plants/PT_Grass_02` **mesh** through `Meadow_DetailTemplate` and the graphs' `DETAIL DENSITY OUTPUT` (Unity terrain details) — not PrefabUtility tufts and not a custom GPU grass system. Species wiring lives in `VistaVegetationTemplates`; `VegetationScatterMenu` and its per-biome catalogs are **deleted**. **Materials ship as Built-in RP by default** (`Sources/Materials/*.mat`) and render magenta in this URP project. The generic "Convert Selected Built-in Materials to URP" converter is **not enough** — some materials (leaf/foliage) use a custom shader with `GrabPass`, which is Built-in-RP-only and throws `ShaderLab::GrabPasses::ApplyGrabPassMainThread` errors under URP's job-threaded rendering; the converter can't rewrite custom shader source. **Fix:** the pack ships a matching URP variant — double-click `Assets/Polytope Studio/Lowpoly_Environments/URP/PT_Nature_Free_URP_17.unitypackage` (version suffix must match the installed URP package version, `17.5.0` here per `Packages/manifest.json`; `12`/`14`/`16` variants also ship for older URP) and Import. Confirmed working: replaces materials in place, no prefab path changes needed in `VegetationScatterMenu.cs`. |
| **Low Poly Village** (Polytope Studio) | Imported, sparse | `Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/` — only `Bridge/` and `Fence/` present in this project | `W.5 Settlement` (road/plot dressing, later) | Not a building kit — only bridge and fence modular pieces are actually imported here. Do **not** treat this as a settlement/building source; flagged as a gap for later `W.6`/[15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md) piece-kit sourcing, not solved by this pack. |
| **Vista Personal 3.2.1** (Pinwheel) | Imported, **in use** | `Assets/PinwheelStudio/Vista/` | `W.2 Terrain/Biome` | **Height/terrain/biome backend.** `OverworldTerrainMenu` + `OverworldLpbBake` over `IslandPlateau_TerrainGraph` (`IslandPlateauGraphBuilder`) and `MountainMassif_TerrainGraph` (`MountainMassifGraphBuilder`), both built on Vista's reference biome architecture. The graphs own height, surface weights (5-layer `Weight Blend`) and vegetation placement. `NaturalIsland_TerrainGraph` is a historical experiment. Height sample = Unity `Terrain.SampleHeight` via `VistaTerrainSampler`. See `VISTA_OVERWORLD_REFERENCE.md`. |
| **WaterWorks** | Imported, **in use** (gitignored vendor) | `Assets/WaterWorks/` — `Water_Plane.prefab`, `SSR_Water` materials/shader | `W.2` ocean visual | **SoT ocean plane.** One `WorldWater` at `SeaLevelMeters`. Inland lake/river mesh builders **removed**. `Water_Volume` stays Unity-6 stubbed. |
| **One Click Add Water** (Houidisoft) | Imported, **fallback only** | `Assets/Houidisoft technology/One Click Add Water -Stylized Water Shader/` | `W.2` (legacy) | Demoted. Do **not** spawn per-lake OCW planes. Prefer WaterWorks `WorldWater`. |
| **MicroSplat Core** (Jason Booth) | Imported, **unused** | `Packages/com.jbooth.microsplat.core/` | — | **Out of the terrain pipeline.** No URP adapter is installed, and its texture arrays only read a `TerrainLayer`'s `diffuseTexture`/`normalMapTexture` — `diffuseRemapMin/Max`, `tileSize` and `smoothness` are silently dropped, which broke our layer look. Terrain material = URP **TerrainLit** over the canonical layers from `OverworldTerrainLayers`. Custom `GameDev/Overworld/Terrain` fork **deleted**. |
| **StampIT** (Rowlan) | Imported, **unused** | `Assets/Rowlan/Terrain/Stamps/` | — | **Out of the terrain pipeline.** Heightmap stamps were a stand-in until editable Vista graphs existed; those graphs are authored now (`Assets/Settings/Vista/`), and a stamp pass would be a second height writer over Vista's. No code references it. |
| **Free HDR Sky** | Imported, **in use** | `Assets/Skybox/` | Overworld lighting | Cubemap skybox + Camera Skybox clear flags. Directional Light = Sun Source matching HDRI disk (`OverworldTerrainMenu.ApplySkyboxAndEnvironment`). |
| **MapMagic 2** | **Retired / removed** | leftover only if Unity still locks native DLL under `Assets/MapMagic/` — delete after closing Editor; retired binary may sit in `tools/_retired_mapmagic/` | — | **Do not use / do not re-import.** Former height backend retired. Height = Vista + `VistaTerrainSampler`. |
| **More Effective Coroutines (MEC)** | Imported | `Assets/Plugins/Digital Dust/MEC/Timing.cs` | `W.3 Chunk Streaming` | Coroutine layer for chunk load/unload queuing in `IChunkStreamer` (`Timing.RunCoroutine` / `Timing.KillCoroutines`, replacing bare Unity `StartCoroutine`). Not wired into any code yet — first real use is W0.5. |

---

## Import notes

Third-party packs listed above live under `unity/Assets/<Vendor>/` (and MicroSplat under `unity/Packages/com.jbooth.microsplat.*`) and are **gitignored** (see repo root `.gitignore`). Import them locally from the Asset Store / vendor installers; paths in this registry must match. PrimeTween is the `Packages/*.tgz` dependency — do not re-commit `Assets/Plugins/PrimeTween`.

First-party stays tracked: `Scripts/`, `Plans/`, authored `Settings/` (including `Settings/Vista/*_TerrainGraph.asset` / biome masks), `Scenes/*.unity`, `Materials/`, `Art/`, `Characters/`, `Input/`, `Prefabs/`, `Tests/`, project `Shaders/`, URP assets.

**Also gitignored (generated, not authored):** `Scenes/Overworld_Generator Biomes|Terrains/`, `Settings/Vista/OverworldTerrainData_*`, `Settings/Vista/TerrainData_*`, `SandDebug/`.

Terrain / water / player stack above is expected on disk after import. For any **new** Asset Store package: import → resolve `.meta` → update this registry + `.gitignore` (do not guess). **Do not re-import MapMagic** for Overworld V0.

---

## Forbidden / avoid

Same acceptance filter as [PROCEDURAL_TOWN_ASSET_LIBRARY.md](PROCEDURAL_TOWN_ASSET_LIBRARY.md) applies to any future addition to this registry: prefer blocks (materials/modules/props/clip libraries) over finished scenes or competing generators. Do not add a package to this file — or wire code against it — until it is actually present on disk.
