---
name: world-gen-assets
description: "Reference for which third-party asset package to use for a given Overworld/Locomotion task in this Unity project — PrimeTween for tweening/camera/UI motion, Human Basic Motions for locomotion/NPC animation, Low Poly Environment (Polytope Studio) for terrain vegetation/prop scatter, and MapMagic 2 / More Effective Coroutines once imported. Trigger this skill whenever the user or task involves terrain generation, biome/vegetation dressing, tree/rock/plant scatter, character locomotion or NPC animation clips, camera easing, UI transitions, or chunk streaming/coroutine queuing. Also trigger when deciding whether to write custom code for motion, animation, or terrain instead of reaching for an already-imported package — check this skill first."
allowed-tools: Bash, Read, Write, Edit, Glob, Grep
---

# World-Gen & Motion Asset Library

Which already-imported (or pending) third-party package to reach for on Overworld V0 / Locomotion V0 / later world-gen work, so agents don't write custom code that duplicates a locked default or invent an API for a package that isn't actually in the project.

Source of truth for the full registry (status, exact folder paths, System Card mapping): `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`. This skill is the quick-trigger summary — always cross-check that file before writing code, since import status changes over time.

## Decision table

| If the task is about... | Use | Status |
|---|---|---|
| Camera easing, UI transitions, procedural motion (fade, bob, punch) | **PrimeTween** (`com.kyrylokuzyk.primetween`) | Imported |
| Player/NPC locomotion clips (walk/run/idle/jump), upper-body overlay (aim/interact) | **Human Basic Motions** (`Assets/Kevin Iglesias/Human Animations/`) — use `Models/Avatar Masks/` for upper-body layering via `SpineProxy.cs`, not new full-body clips | Imported |
| Terrain vegetation/prop scatter (trees, rocks, plants, shrubs, flowers, mushrooms) for chunk dressing or biome flavor | **Low Poly Environment** (`Assets/Polytope Studio/Lowpoly_Environments/Prefabs/`) | Imported |
| Settlement road/plot dressing (bridge, fence only) | **Low Poly Village** (`Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/`) — sparse, bridge+fence only, **not** a building kit | Imported, limited |
| Terrain height generation (mountains, erosion, terraces), seed-driven | **MapMagic 2** (`Assets/MapMagic/`, Core + Matrix node pack) — author graph in the node editor, read via `DirectMatricesHolder.ValueAtPositionInterpolated` | Imported, Core + Matrix only |
| Biome banding, river/lake shaping, vegetation/object scatter via MapMagic's own nodes | **Not available** — no Objects/Scatter/Biome/River node pack is installed (only icons exist). Do biome classification and vegetation scatter as our own code on top of MapMagic's height output, using Low Poly Environment prefabs for scatter | — |
| Chunk load/unload coroutine queuing | **More Effective Coroutines (MEC)** (`Assets/Plugins/Digital Dust/MEC/Timing.cs`) | Imported |

## Rules

1. **Never guess an unimported package's API.** If a needed capability isn't actually present (see rows above), don't write code referencing it — flag the gap and use the documented fallback (our own code) instead of assuming a MapMagic node pack or other package will materialize.
2. **Don't duplicate what these packages already do.** A hand-rolled tween helper, a new locomotion clip authored from scratch, or bespoke terrain height noise all need a reason why the imported package doesn't already cover it.
3. **Low Poly Village is not a building kit in this project** — only Bridge and Fence prefabs are imported. Do not treat it as a source for procedural building generation ([15 Procedural Building Generation](../../../../15%20Procedural%20Building%20Generation.md)); that gap is tracked, not solved, in `ASSET_LIBRARY_REFERENCE.md`.
4. **MapMagic's own `DirectMatricesHolder.FindHolder`/`FindValueAtPosition` helpers scan every `DirectMatricesHolder` in the scene per call** (MapMagic's own source calls them "not quick, just an example") — never use them in gameplay code. `IChunkStreamer` caches the holder per resident `ChunkCoord` instead.
5. **Each `Noise200`/`Voronoi200` node has its own `seed` field on the graph asset** — MapMagic has no single world-seed hookup. Any code that regenerates terrain from `WorldSeed` must inject forked seed values into those node fields itself; this is not automatic.
6. When a package's import status or capability changes, update `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`'s registry row — this skill's decision table should stay a thin pointer to that file, not drift into its own copy of the details.
7. **Asset Store packs are frequently authored for Built-in RP and render magenta in this URP project until converted.** Confirmed for Low Poly Environment's `Sources/Materials/*.mat`. First try the generic converter, scoped to that pack's folder only (Project window → select folder → right-click → Rendering → Convert Selected Built-in Materials to URP) — but check for a **shipped URP variant first**: Low Poly Environment has one at `Assets/Polytope Studio/Lowpoly_Environments/URP/PT_Nature_Free_URP_<version>.unitypackage` (pick the `.unitypackage` matching the installed `com.unity.render-pipelines.universal` version in `Packages/manifest.json`) — that's the actual fix when materials use custom shaders (e.g. foliage `GrabPass` shaders) the generic converter can't rewrite. Never run the project-wide converter — it would also touch the frozen OrganicTown archive materials.
