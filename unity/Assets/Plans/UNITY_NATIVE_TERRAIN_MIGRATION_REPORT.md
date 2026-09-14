```
UNITY-NATIVE TERRAIN MIGRATION: PASS
TERRAIN AUTHORITY: TERRAIN GEN
TERRAIN REPRESENTATION: UNITY TERRAIN DATA
EXTERNAL TERRAIN PIPELINE: REMOVED
STAGE 3C: FROZEN
```

# Unity-Native Terrain Migration — Report

**Date:** 2026-09-13
**Scope:** the removed legacy terrain pipeline is gone from the project; TerrainGen's relief is baked
straight into Unity `TerrainData` together with surface weights, tree instances and detail layers.
**Authoritative description of the result:** [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).
**Not committed.** Git history untouched.

Every number below was measured in this migration, in fresh Unity 6000.5.7f1 batch processes, unless it
is labelled otherwise.

---

## 1. Final architecture

```
seed
  │
TerrainGen ──────────── Stage 1 landmass + Stage 2 massif uplift = StructuralElevation   (unchanged)
  │  TerrainAuthorityField
  ▼
OverworldTerrainField ─ one 1025 × 1025 world lattice at 2.734 m: height, uplift, coast distance,
  │                     slope, convexity, drainage — world units only
  ├─ hydrology carve ── optional, one-way, lowers the lattice (HydrologyBake.ApplyPendingCarve)
  ▼
OverworldTerrainBuilder
  ├─ heights ────────── TerrainData.SetHeightsDelayLOD / SyncHeightmap
  ├─ surface weights ── TerrainSurfaceRules → TerrainSurfaceClassifier → SetAlphamaps
  ├─ trees ──────────── TerrainVegetationRules → SetTreeInstances
  └─ ground cover ───── TerrainVegetationRules → SetDetailLayer
  ▼
Unity Terrain (2 × 2 tiles of 1400 m, URP TerrainLit, five canonical TerrainLayers)
```

- **One production path.** `OverworldTerrainMenu.GenerateSurfaceRoutine` → `OverworldTerrainBake.Bake`.
  No fallback, no compatibility adapter, no optional package path, no alternate bake, no source switch.
- **No graph execution, no biome objects.** Rules are plain data types in world units; the classifier
  and placers are project-owned C#. No node graph, no rule engine, no external terrain abstraction.
- **TerrainGen is not modified.** The bake consumes `StructuralElevation` and never writes back. The
  canonical seed's field hash (`7614bd2ed819`) is pinned by test.
- **Nothing reads `TerrainData` back into TerrainGen.**

## 2. Files added

Runtime — `Assets/Scripts/World/Terrain/`:

| File | Role |
|---|---|
| `TerrainAuthorityField.cs` | Builds Stage 1, Stage 2A and the massifs as TerrainGen does; `HeightAt` = `StructuralElevation.At`; `Sample` also returns uplift and signed coast distance (bit-equal height) |
| `OverworldTerrainField.cs` | The world lattice; derived slope (true degrees), convexity (20 m box mean), D8 drainage area on the depression-filled surface; `ApplyCarve`; per-tile height windows |
| `OverworldTerrainBuilder.cs` | Writes heights, alphamaps, tree instances and the detail layer; per-stage timings |
| `TerrainSurfaceRules.cs` | Surface layer enum and world-unit rule values, each labelled measured or authoring default |
| `TerrainSurfaceClassifier.cs` | Priority cover classification: sand → soil → rock → cliff over grass; weights sum to one |
| `TerrainVegetationRules.cs` | Tree species enum, tree and ground-cover rule values |
| `TerrainHash.cs` | Stateless SplitMix64 hashing and value noise keyed by `WorldSeed.Fork` stream keys |
| `OverworldTerrainSampler.cs` | Height / slope / biome sampler over Unity Terrain (renamed from a legacy-named file; GUID kept) |

Editor — `Assets/Scripts/Editor/`:

| File | Role |
|---|---|
| `OverworldTerrainBake.cs` | The bake orchestration, tile-data persistence (refilled in place, GUID-stable), orphan cleanup |
| `OverworldVegetationAssets.cs` | Tree prototypes (Polytope pine, fruit, apple trees + HSV-tinted variants) and the grass detail prototype |
| `OverworldTerrainValidation.cs` | Batch validation: per-tile height / alphamap / tree / detail hashes, seams, height error, timings, optional raw dumps, `-terrainFromScene` |
| `OverworldTerrainReady.cs` | Tile readiness helper (renamed from a legacy-named file; GUID kept) |
| `Tests/OverworldTerrainBakeTests.cs` | 13 tests (section 13) |

Assets and documentation:

| Path | Role |
|---|---|
| `Assets/Art/Terrain/SurfaceLayers/{Grass,Sand,Soil,Cliff,Rock}/` | 10 textures (diffuse + normal) behind the canonical layers, moved out of the removed package with their GUIDs |
| `Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md` | The one authoritative terrain architecture document |
| `Assets/Plans/UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md` | This report |

Generated, gitignored, refilled by every bake: `Assets/Settings/Terrain/OverworldTerrainData_{x}_{z}.asset`,
`Assets/Settings/Terrain/Vegetation/` (grass detail prefab and material, tinted tree prefabs and materials).

## 3. Files deleted

Named by role: several deleted files carry the removed technology's name.

| Group | Tracked | Previously untracked | Content |
|---|---|---|---|
| Graph and biome authoring | 8 | 2 | C# terrain-graph builders for sea floor, island plateau, massif, shelf, soft falloff mask, hydrology and height injection; the graph-build helper; biome template factory; vegetation template factory |
| Bake orchestration | 1 | 2 | biome spawning bake; height-injection baker; hydrology carve baker |
| Package workarounds, probes, dumps | 6 | 0 | detail-density relay and probe, execution probe, layer-weight dump, vegetation dump; the legacy-named tile sync (renamed to `OverworldTerrainReady.cs`) |
| Spikes and gate tooling | 0 | 12 | height-input, coexistence, dataflow, feedback, threshold, erosion and authoring spikes; three migration gate/validation tools; a legacy terrain menu |
| Integration menu | 1 | 0 | third-party vegetation integration menu |
| Tests | 1 | 2 | biome bake contract tests; height-injection tests; the earlier migration spike tests |
| Height writer helper | 1 | 0 | `TerrainHeightWrite.cs` — no callers left |
| Legacy-named sampler | 1 | 0 | replaced by `OverworldTerrainSampler.cs` (same GUID) |
| Settings folder of the removed pipeline | 67 | 14 | terrain graphs, biome templates, biome prefabs, vegetation templates and tints, height-injection textures |
| Spike settings folder | 0 | 41 | spike graphs, inputs and patched graphs |
| Scene-local graph folder | ignored | 9 | per-scene graph clones |
| Exported screenshots folder | ignored | 7 | package screenshots |
| Documentation | 20 | 12 | the legacy reference, playbook, bistability record and bug report, reference baseline and its 12 images, island generator plan, island shaping and banding investigations, D4/F1 reports, the earlier migration spike, the terrain-authority preflight, the temporary migration baseline |
| Generated dumps | 3 ignored folders | 269 | spike and F1 output under `TerrainGenDump/`; three ignored legacy dump folders |
| Stale generated tile data | ignored | 42 | `Assets/Scenes/Overworld_Generator Terrains/` — old `TerrainData` assets that referenced removed-package assets; nothing referenced them |
| **The third-party package folder** | ignored | 1 007 assets | moved out of `Assets/` (section 11) |

Counts exclude `.meta` files; every `.meta` beside a deleted file was deleted with it. Seven of the tracked
legacy scripts and several tracked legacy settings assets carried uncommitted user modifications before the
migration; they were deleted because they belonged to the removed architecture, and those modifications are
preserved in the backup (`modified_tracked_files.tar`, `uncommitted_tracked.patch`). Before deletion each group was checked by GUID
and by name: only the overworld scene referenced the legacy assets, and it was regenerated (section 10).
Everything deleted is preserved outside the repository in `D:/Hobby/game-dev/_terrain_migration_backup_2026-09-13/`.

## 4. Files migrated

Code — behaviour changed:

| File | Change |
|---|---|
| `Editor/OverworldTerrainMenu.cs` | Rebuilt around the native bake; scene, material, ocean and seed helpers kept |
| `Editor/OverworldPipeline.cs` | Package cancel/lock code removed; Surface + Mountains = one native bake |
| `Editor/OverworldBatchDiagnostics.cs` | Rewritten for the native bake; adds `GenerateAllAndSave` (scene regeneration through the pipeline) |
| `Editor/HydrologyBake.cs` | Carve applied to the lattice (`ApplyPendingCarve`); `VerifyCarve` samples on the same raster |
| `Editor/HydrologyMenu.cs` | Legacy carve baker call removed |
| `Editor/OverworldTerrainLayers.cs` | Canonical layers from `TerrainSurfaceLayer`, textures from `Assets/Art/Terrain/SurfaceLayers` |
| `Editor/OverworldTerrainDiagnostics.cs` | Native resolution budget; dump folder `TerrainDump`; menus under `Terrain/` |
| `World/Terrain/OverworldTerrainGrid.cs` | `Configure` resets new or existing `TerrainData` to an empty tile |
| `World/Terrain/TerrainBakeFlags.cs`, `Editor/Tests/TerrainBakeFlagsTests.cs` | Demo-bake flag and its test removed |
| `World/Hydrology/HydrologyCarveField.cs`, `Editor/Tests/HydrologyCarveFieldTests.cs` | Height-scale normalisation for the removed carve biome and its test removed |
| `World/Terrain/OverworldTerrainAccess.cs`, `World/WorldScale.cs` | Legacy manager name and legacy seam-overlap constant removed |

Comment and wording only: `PoiMarkerMenu`, `RoadNetworkMenu`, `WorldGeneratorWindow`, `OverworldPlayerMenu`,
`TerrainGenDump`, `OverworldTerrainTile`, `BiomeClassifier`, `TerrainDeformer`, `WorldGenSettings`,
`HydrologyPlanner`, `HydrologySettings`, `RiverNetworkBuilder`, `RiverNode`, `HydrologyTestTerrain`, and in
TerrainGen (non-frozen files) `TerrainGenSettings`, `WorldHeightField`, `WorldStructure`.

Configuration: `ProjectSettings.asset` (scripting define of the removed package removed), `.gitignore`
(legacy ignore rules replaced by the native generated paths).

Documentation rewritten: `AGENTS.md`, `14 World Generation Roadmap.md`, `17 Game Systems Architecture.md`,
`90 Glossary.md`, `99 Open Questions.md`, `.cursor/rules/gamedev-design-guardian.mdc`,
`.cursor/rules/unity-csharp.mdc`, `unity/.claude/skills/world-gen-assets/SKILL.md`, and in `Assets/Plans/`:
`WORLD_GENERATION_PIPELINE`, `OVERWORLD_V0_PLAN`, `OVERWORLD_VISUAL_DIRECTION`, `OVERWORLD_TECH_DEBT`,
`HYDROLOGY_GENERATION_PLAN`, `PROCEDURAL_TERRAIN_GENERATOR_DESIGN`, `ASSET_LIBRARY_REFERENCE`,
`AGENT_HANDOFF_WORLD_GEN`, `LANDSCAPE_PLANNER`, `WORLD_RELIEF_PRINCIPLES`, `LANDMASS_T1_FEASIBILITY_AUDIT`,
`STAGE_2A_SELECTION_HARDENING`, `STAGE_2A_2B_CONTRACT_HARDENING`, `PROCEDURAL_TERRAIN_STAGE_3_DESIGN`,
`PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF`, `PROCEDURAL_TERRAIN_STAGE_3E_PREFLIGHT`, `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT`.

Generated: `Assets/Scenes/Overworld_Generator.unity` (section 10).

## 5. Height pipeline

1. `TerrainAuthorityField(seed, includeMassifs)` builds `Stage1LandmassGenerator`, `Stage2AMacroStructure`,
   `MountainMassif.Build` and `MassifUpliftModel` and evaluates
   `StructuralElevation.At = stage1.HeightAt + uplift.UpliftAt(x, z, signed coast distance)`.
2. `OverworldTerrainField.FromAuthority` samples it once per world lattice vertex (1025 × 1025 at
   1400 / 512 = 2.734 375 m). A tile-edge vertex is one lattice point shared by both tiles.
3. An optional hydrology carve lowers requested vertices (section 9).
4. `OverworldTerrainBuilder.WriteHeights` writes each 513 × 513 tile window as `height / 700 m` with
   `SetHeightsDelayLOD`, then `SyncHeightmap`. Samples outside 0–700 m are counted, not hidden
   (`ClampedHeightSamples`, measured 0 on both canonical seeds).

Precision: Unity stores heights in 16 bits over half the normalised range — a 2.14 cm step on a 700 m
tile. Measured distance between the shipped heightmap and TerrainGen over all 1 052 676 tile samples:
**110201 mean 0.599 cm, max 1.069 cm; 424242 mean 0.583 cm, max 1.070 cm** — rounding to the nearest
step. Tile seams: **0.0000 m** on all four internal edges, both seeds.

## 6. TerrainLayer pipeline

- `OverworldTerrainLayers.EnsureCanonicalLayers()` provides `Overworld_{Grass,Sand,Soil,Cliff,Rock}` in
  `TerrainSurfaceLayer` order; the bake assigns them to every tile before painting. Textures resolve to
  `Assets/Art/Terrain/SurfaceLayers/` (recorded by every validation run). Layer appearance stays an
  Inspector property of the asset.
- The `TerrainData` is an asset **before** any weight is written (otherwise Unity recreates the splat
  textures and the painted weights revert to the base layer — found and fixed during the migration).
- `TerrainSurfaceClassifier.Classify` runs per alphamap texel at world position `i / (512 − 1)` across the
  tile, so a tile's last texel and its neighbour's first are the same world point.

| Layer | Rule (world units) | Basis |
|---|---|---|
| Sand | below the vegetation floor: full below 27.0 m, none above 28.4 m; ±1 m shore noise at 60 m | measured |
| Soil | D8 drainage area 20 000 → 150 000 m² on dry land, slope ≤ 30° | authoring default |
| Rock — seabed | slope 2° → 4°, full below 22 m, none at 24 m | measured |
| Rock — massif | uplift 2 → 25 m and (height 110 → 230 m ±20 m, or convexity 1.5 → 5 m) | authoring default |
| Rock — steep | 35° → 42° on dry land | authoring default, angle of repose |
| Cliff | 50° → 58° on dry land | authoring default |

**Coastal rock.** The seabed rock mask is a function of world height and true slope only: rock is
allowed at world 22 m and suppressed at world 24 m, and coastal land never receives it. There is no
normalised height and no per-biome vertical scale, so the slope amplification of the removed pipeline
(a normalised 34 m / 700 m height, ≈ 20.6× on the seabed) cannot occur. Tested directly (section 13).

**Weights.** Sum per texel 0.9961 … 1.0039 on every tile of both seeds (8-bit storage, ±1/255).

## 7. Tree pipeline

- `OverworldTerrainBuilder.PlaceTrees`: at most one tree per 7 m world cell; jitter, acceptance, species,
  variant, scale and rotation are hashes of the world cell (`terrain.vegetation.*` streams), so the result
  does not depend on tile or evaluation order; each cell is owned by exactly one tile.
- Acceptance = ground (grass weight + ½ soil weight) × vegetation gate (27.0 → 28.4 m) × slope fade
  (20° → 28°) × grove mask (140 m noise, 0.42 → 0.62, drainage lift 0.25) × cap 0.65.
- Species: massif uplift 8 → 30 m becomes pine, lowland pine where the 220 m species noise exceeds 0.62,
  otherwise fruit; variants pale pine 30 %, deep fruit 30 %, orchard 15 %; scale 0.85–1.15.
- Prototypes: 5 (`PT_Pine_Tree_03_green`, tinted pale; `PT_Fruit_Tree_01_green`, tinted deep;
  `PT_Fruit_Tree_01_apples`). A source prefab without a root LOD Group or Mesh Renderer, which Unity requires, is
  refused with an error; all five resolve (5 prototypes per tile in every run).
- Instances are written with `SetTreeInstances(…, snapToHeightmap: true)` after the final heights, so trees
  stand on the shipped terrain (tested).

## 8. Detail pipeline

- One detail layer per tile, 512 × 512, 16 per patch, `DetailScatterMode.InstanceCountMode`.
- Instances per cell = `15 × vegetation gate × grass weight × patchiness` (patchiness 0.75 → 1.0 over
  45 m noise), none past 35°. Peak 15 is measured from the pre-migration island plateau.
- Prototype: the Polytope `PT_Grass_02` LOD0 mesh as a single-mesh prefab, `DetailRenderMode.VertexLit`,
  GPU instancing, size 0.7–1.3.
- View settings: detail distance 250 m (Unity's maximum), tree distance 5000 m, pixel error 1.

## 9. Hydrology boundary

```
bake A (no carve) → HydrologyTerrainGrid.BuildFromScene → HydrologyPlanner → HydrologyDefinition
  → bake B: TerrainGen field → lattice → HydrologyBake.ApplyPendingCarve → heights, weights, trees, details
  → HydrologyBake.VerifyCarve compares A and B against the carve field
```

- The carve is a depth field in metres applied to the lattice **downstream of TerrainGen and upstream of
  `TerrainData`**. Pass A does not know hydrology exists; nothing is read back into TerrainGen; no cycle.
- Surface weights, trees and ground cover are derived after the carve.
- `RunHydrology` stays off by default.

Measured, seed 110201, fresh process, `-overworldHydrology true`:

| | |
|---|---|
| network | 8 river systems, 8 reaches, 8 ocean outlets, 0 lakes, 2.05 km; planning 332 ms |
| carve | 21 322 lattice vertices lowered; deepest requested 9.4 m, delivered 9.4 m |
| `VerifyCarve` | worst disagreement inside the carve **0.02 m**; worst movement outside it **0.02 m** (quantisation) |
| carved bake | 25.2 s |

During the migration `VerifyCarve` was found to rasterise the expected carve half a cell off the lattice
(0.88 m apparent disagreement); it now uses the bake's raster.

## 10. Scene integration

`Assets/Scenes/Overworld_Generator.unity` was **regenerated, not edited**: a fresh batch process ran
`OverworldBatchDiagnostics.GenerateAllAndSave` → `OverworldPipeline.GenerateAll(110201, WorldGenSettings.Default)`
→ `EditorSceneManager.SaveScene`.

Log: `[Terrain] Bake done: seed=110201 flags=Full tiles=4 massifs=1 trees=11552 detailInstances=3796453`,
`[POI] placed=32 … skipped: noSurface=0 water=0 noPrefab=0`, player spawned at `TownApproach_1`, `saved=True`.
On the first regeneration the bake's orphan sweep removed the one root object the removed pipeline had
left in the scene with only missing scripts; the scene was then regenerated once more as the very last bake,
so no later bake could overwrite the tile assets it references.

Inspection of the saved scene file:

| Check | Result |
|---|---|
| root objects | `OverworldPoi`, `OverworldReflectionProbe`, `OverworldSettlement`, `OverworldVolume`, `OverworldWater`, `OverworldWorldState`, `Review Camera`, `Sun`, `Terrains` — the legacy manager root is gone |
| `Terrain` / `TerrainCollider` components | 4 / 4 |
| `TerrainData` references | the four `Assets/Settings/Terrain/OverworldTerrainData_{x}_{z}.asset`; GUIDs unchanged across all later bakes |
| missing scripts | 0 |
| GUIDs referenced | 26, all resolve to project assets; 0 belong to the removed package or deleted legacy assets |
| text references to the removed pipeline | 0 |
| terrain material, layers, tree and detail prototypes | assigned (validated from disk, section 14) |

The scene file shrank from 401 161 to 202 330 lines, mostly because the committed scene predated the
generated-material sharing of `OVERWORLD_TECH_DEBT.md` item 7a; object identity still churns on every
generation (item 7, not in scope).

## 11. Package removal proof

- The third-party package lived in an `Assets/` folder (114 MB, 1 007 GUID-bearing assets), never in
  `Packages/manifest.json` or `packages-lock.json`. The folder and its `.meta` were moved out of the project
  into the backup folder; its `.gitignore` entries were removed.
- `Packages/manifest.json`, `Packages/packages-lock.json`: 0 references. No `.asmdef` / `.asmref` references.
- `ProjectSettings.asset`: its scripting define removed (`Standalone: MAPMAGIC2;MM_NATIVE;__MICROSPLAT__`).
  The compiler response files in `Library/Bee` carry no such define; the stale editor-only copy in
  `Library/EditorOnlyScriptingSettings.json` was cleaned as well.
- `Library/ScriptAssemblies`: no assembly of the package.
- The project compiles with the package absent: 0 compiler errors in every batch process of this report.
- GUID proof: the 1 007 package GUIDs plus the 169 GUIDs of the deleted legacy assets (textures the canonical
  layers use were moved and excluded) were searched in every text asset under `Assets/`, `ProjectSettings/`
  and `Packages/`, and — encoded as Unity binary GUIDs — in every binary-serialised asset. Result after the
  migration: **0 references** (the regenerated scene included; the 42 stale tile assets that did reference
  them were deleted).
- The bake test asserts that every component on the generated tiles comes from `UnityEngine*` or
  `Assembly-CSharp*` assemblies.

## 12. Repository reference purge proof

Case-insensitive search of the whole repository (excluding `.git/`, `Library/`, `Temp/`, `Logs/`, `obj/`)
for the removed technology's name, its vendor name, its namespaces, its biome-prefab prefix, its biome,
graph, template and manager type names, its graph and output node names, the legacy builder, bake, relay,
probe, spike and gate class names, the legacy asset folders and generated asset names, the legacy menu
paths and the removed scripting define.

Final run over 7 332 text files (source, scenes, prefabs, assets, materials, settings, packages, tests,
documentation, rules, scripts, `.gitignore`, `.meta`), plus file names:

| Area | Active references |
|---|---|
| source code (runtime, editor, tests) | **0** |
| scenes, prefabs, materials, terrain layers, settings assets | **0** (text search and GUID search, section 11) |
| `ProjectSettings/`, `Packages/manifest.json`, `packages-lock.json`, asmdef / asmref | **0** |
| editor menus | **0** — legacy menu paths gone; terrain menus are `GameDev/Overworld/Terrain/…` and `…/Vegetation/…` |
| documentation, agent rules, skills | **0** |
| CI | none exists in the repository |
| generated assets and dumps | **0** — stale tile data, spike dumps and the legacy dump folders deleted |
| file and folder names | **0** |

Matches that remain are not references to the removed pipeline:

- a PrimeTween demo script (`Plugins/PrimeTween/Demo/Scripts/Wheels.cs`) whose unrelated variable name contains
  the vendor's name as a substring;
- MicroSplat shader sources (`Packages/com.jbooth.microsplat.core`) with the generic phrase "weight blend";
- `PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md` recording that a proposed `TerrainSource` switch was never built, and
  the Unity term "detail density";
- this report naming the deleted project files `TerrainHeightWrite.cs` and `Overworld_Generator Terrains/`.

Outside the repository content: the per-user, gitignored editor layouts under `UserSettings/Layouts/` (see
section 18) and pre-migration `Library/` caches.

## 13. Tests

New — `OverworldTerrainBakeTests` (13):

| Area | Test |
|---|---|
| Height | TerrainGen field reproduces the canonical hash; `Sample` equals `HeightAt` exactly; deterministic and island-only has no uplift; tile heights match the field within quantisation and seams are exact; grid world bounds |
| Layers | weights sum to one and stay in range; seabed rock allowed at 22 m, suppressed at 24 m and on coastal land; slope is true world slope (a 10° plane reads 10°, no amplification); alphamaps deterministic, valid and seamless |
| Trees | deterministic, valid prototype indices, standing on the final terrain above the vegetation floor |
| Details | deterministic, one valid prototype, only above the vegetation floor |
| Carve | lowers only requested vertices and refreshes derived quantities |
| Pipeline | full production bake of seed 110201: four tiles, only Unity and project components, five layers, material, trees, ground cover, heights matching TerrainGen |

Removed with the architecture they tested: biome bake contract tests, height-injection tests, the earlier
migration spike tests, the demo-bake flag test and the carve normalisation test. No test was weakened.

**Full EditMode suite, fresh process, final code: 555 / 555 passed, 0 failed, 0 inconclusive, 0 skipped**
(19.6 min). Within it: `OverworldTerrainBakeTests` 13/13 including the production bake, `HydrologyCarveFieldTests`
6/6, TerrainGen tests 253/253 of which Stage 3 tests 90/90. Before the final cleanup the suite stood at 556/556;
the one test removed since is the carve normalisation test named above.

PlayMode tests: none exist in the project.

## 14. Determinism

Production bake through `OverworldTerrainValidation.RunBatch`, **one fresh Unity process per run**:

| Seed | Runs | World signature | Height, alphamap, tree, detail hashes |
|---|---|---|---|
| 110201 | 6 (+1 earlier, +1 over existing tile assets) | `c08574603378` in all 8 | identical on all four tiles |
| 424242 | 6 (+1 over tile assets holding seed 110201) | `a8d7baad5360` in all 7 | identical on all four tiles |

The world signature is a digest of every tile row (height, alphamap, tree and detail hashes and counts).
The two "over existing tile assets" runs exercise the in-place refill: a tile asset holding another
world's data is reset and comes out bit-identical.

Sources of determinism: no `UnityEngine.Random`, no time, no hash-set iteration; every random choice is a
stateless hash of a world cell under a `WorldSeed.Fork` stream key. No known P0 issue appeared.

**Saved data equals the bake.** A further fresh process opened the saved scene without baking
(`-terrainFromScene true`) and hashed the tiles as loaded from disk: heights, tree and detail hashes are
identical on all four tiles, height error and seams identical, 11 552 trees and 3 796 453 detail instances.
Alphamaps are identical on tiles 0,0, 0,1 and 1,1; tile 1,0 carries one appended `Dirt_Road_Edge` layer painted
by the Settlements step after the bake, as designed — so its alphamap hash and the world signature differ
(`0bd540ab3980`) for exactly that reason.

## 15. Performance

Median of six fresh processes (ms):

| Stage | 110201 | 424242 |
|---|---|---|
| TerrainGen evaluation onto the lattice | 23 559 | 23 967 |
| Height write | 35 | 35 |
| Derived fields (slope, convexity, drainage) | 160 | 163 |
| Alphamaps | 545 | 542 |
| Trees | 47 | 47 |
| Details | 238 | 232 |
| **Surface step total** | **27 205** | **27 848** |

Full Generate All (default settings) for seed 110201: 28.2 s, of which surface 27.3 s.

Memory, estimated from the formats: heights 4 × 513² × 2 B ≈ 2 MB; alphamaps 4 × 2 splat textures ×
512² × 4 B ≈ 8 MB; one detail layer per tile, 512² cells, a few MB; tree instances ≈ 0.4 MB — on the order
of 15 MB of terrain data. Unity's runtime
size report counts 3 MB (it excludes the splat textures); the managed heap grows by 148–178 MB during
the bake (lattice and derived float arrays, released afterwards).

Compared with the removed pipeline: its surface step measured about 46 s. The native path is faster,
and 87 % of it is TerrainGen evaluation — single-threaded and per vertex. Everything Unity-side is
about one second. Not optimised in this migration (`OVERWORLD_TECH_DEBT.md` item 16).

## 16. Canonical seed results

| | 110201 | 424242 |
|---|---|---|
| massifs | 1 | 1 |
| trees | 11 552 | 12 254 |
| detail instances | 3 796 453 | 3 620 418 |
| clamped height samples | 0 | 0 |
| height vs TerrainGen (mean / max) | 0.599 / 1.069 cm | 0.583 / 1.070 cm |
| seams | 0.0000 m | 0.0000 m |
| weight sum | 0.9961 … 1.0039 | 0.9961 … 1.0039 |
| tree / detail prototypes per tile | 5 / 1 | 5 / 1 |
| layers per tile | 5 canonical | 5 canonical |

Per tile, seed 110201:

| Tile | Height | Alphamap | Trees | Details |
|---|---|---|---|---|
| 0,0 | `d24f7a46e4d4` | `13ae55f79a76` | `7025e9d310be` (130) | `03ca0257b0f2` (27 777) |
| 0,1 | `be18b5b8a6e2` | `c603b5ed1d05` | `bcce7183088e` (3 618) | `5b7b47e0aa26` (1 207 717) |
| 1,0 | `ef95a774a202` | `7fe41a66ee88` | `833f766c921f` (4 024) | `a9ce04927825` (1 233 839) |
| 1,1 | `8ef6c4ee5018` | `092791f365a3` | `7c07e52f0391` (3 780) | `b98166046855` (1 327 120) |

Per tile, seed 424242:

| Tile | Height | Alphamap | Trees | Details |
|---|---|---|---|---|
| 0,0 | `758a612effc2` | `8ab794ee1361` | `f60133d375ac` (4 286) | `b23a9226abd7` (1 313 403) |
| 0,1 | `32b8ffdc287c` | `f065c4b25c93` | `2e4dacbde075` (3 601) | `98c03741b8ba` (1 031 321) |
| 1,0 | `66614d52d5dd` | `2815a24326df` | `feccb691ae4d` (1 720) | `6ce13e2e3bc6` (521 963) |
| 1,1 | `1c508feb6093` | `1a838ed2eeff` | `091c2a2e9dda` (2 647) | `8d09e7f35f37` (753 731) |

Surface by height band, seed 110201 (mean weights): underwater sand 0.62 / rock 0.38; 24–27 m sand 0.99,
rock 0; 27–30 m grass/sand transition; 30–110 m grass 0.94–0.998; 110–230 m grass 0.03 / cliff 0.48 /
rock 0.49; above 230 m cliff 0.21 / rock 0.79.

## 17. Remaining authoring decisions

**NEEDS AUTHORING DECISION** — the rule exists and is implemented in world units; the number is a default:

1. **Mountain soil.** Implemented from the understood rule *drainage → soil* (D8 area 20 000 → 150 000 m²).
   Soil as **erosion deposition** is not modelled: the project has no erosion field.
2. **Mountain rock vs cliff.** Massif rock 110 → 230 m, ridge convexity 1.5 → 5 m, steep rock 35° → 42°,
   cliff 50° → 58°. TerrainGen's massif flanks are steep (slope p10 44°, p50 54°, p90 61° on 110201), so
   these two slope bands decide the visible cliff/rock split (currently ≈ 0.48 / 0.49).
3. **Vegetation density.** Grove noise and mask, probability cap, species split and variant shares, grass
   patchiness. 11 552 trees on 110201 against 15 852 in the removed pipeline.
4. **Texture licence.** The canonical layer textures came from the removed package's sample content;
   confirm the licence before committing `Assets/Art/Terrain/SurfaceLayers/`.

Measured, not decisions: the sand band 27.0 → 28.4 m, the seabed rock 22 → 24 m and 2° → 4°, the grass peak
of 15 per cell.

## 18. Remaining technical debt

Recorded in [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md). The removed pipeline's own debts (its
bistability, detail-density writer, island banding, sand measurement scope and banding-driven vegetation
re-tune) were deleted with it; the numbering is preserved.

| Item | State |
|---|---|
| 7 — the scene versions generated state | **Open.** Its former blocker (a non-reproducible world) is gone for terrain; placement steps were not re-measured and the policy is undecided |
| 14 — surface and vegetation values are authoring defaults | new |
| 15 — soil is drainage-driven, not deposition | new |
| 16 — TerrainGen evaluation dominates the bake (~23.5 s) | new |
| 17 — surface texture licence check | new |
| 18 — legacy road/settlement layers reference textures missing from the project | new record of a **pre-existing** defect; the GUIDs were not in the removed package |

Not claimed fixed: roads (0), single-tile assumptions (2), the play-mode `NullReferenceException` (5), the
vegetation hole (6), and the other existing items.

Local, outside version control and not modified: the editor window layouts in `UserSettings/Layouts/`
still describe a docked window of the removed package; Unity drops a window whose type no longer exists
when it loads the layout (or use *Window → Layouts → Reset*). `Library/` caches from before the migration
are disposable and regenerate.

## 19. Git proof

`git log -1`: `7c882c1` — unchanged HEAD, **no commit created**; history not rewritten.

`git status --short`: 373 entries (untracked folders collapsed). `git diff --stat` (tracked files): 267 files changed, 79302 insertions(+), 305629 deletions(-). The large deletion count is the regenerated scene (section 10) and the removed pipeline's tracked settings assets.

Classification of every changed path (`git status --untracked-files=all`, `.meta` included), against the pre-migration snapshot of every modified and untracked file:

| Class | Paths | Meaning |
|---|---|---|
| MIGRATION | 34 | tracked file changed by this migration only |
| MIGRATION (added) | 58 | new file added by this migration |
| DELETE | 217 | tracked file deleted by this migration |
| USER-EXISTING | 97 | user's pre-existing uncommitted change, byte-identical to before the migration |
| USER-EXISTING + MIGRATION | 24 | user's pre-existing change, further edited by this migration (user content kept) |
| GENERATED | 1 | regenerated by the pipeline |
| UNEXPECTED | 0 | — |

**GENERATED:** `unity/Assets/Scenes/Overworld_Generator.unity`.

**USER-EXISTING + MIGRATION** (each keeps the user's own edits; the migration changed only terrain wording or legacy terrain code in it):

- `14 World Generation Roadmap.md`
- `unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md`
- `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3E_PREFLIGHT.md`
- `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md`
- `unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md`
- `unity/Assets/Scripts/Editor/HydrologyBake.cs`
- `unity/Assets/Scripts/Editor/HydrologyMenu.cs`
- `unity/Assets/Scripts/Editor/OverworldBatchDiagnostics.cs`
- `unity/Assets/Scripts/Editor/OverworldPipeline.cs`
- `unity/Assets/Scripts/Editor/OverworldTerrainMenu.cs`
- `unity/Assets/Scripts/Editor/PoiMarkerMenu.cs`
- `unity/Assets/Scripts/Editor/RoadNetworkMenu.cs`
- `unity/Assets/Scripts/Editor/Tests/HydrologyCarveFieldTests.cs`
- `unity/Assets/Scripts/Editor/Tests/HydrologyTestTerrain.cs`
- `unity/Assets/Scripts/Editor/Tests/TerrainBakeFlagsTests.cs`
- `unity/Assets/Scripts/Editor/WorldGeneratorWindow.cs`
- `unity/Assets/Scripts/World/Hydrology/HydrologyCarveField.cs`
- `unity/Assets/Scripts/World/Hydrology/HydrologyPlanner.cs`
- `unity/Assets/Scripts/World/Hydrology/HydrologySettings.cs`
- `unity/Assets/Scripts/World/Hydrology/RiverNetworkBuilder.cs`
- `unity/Assets/Scripts/World/Hydrology/RiverNode.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainAccess.cs`
- `unity/Assets/Scripts/World/WorldGenSettings.cs`
- `unity/Assets/Scripts/World/WorldScale.cs`

**MIGRATION** (tracked, changed):

- `.cursor/rules/gamedev-design-guardian.mdc`
- `.cursor/rules/unity-csharp.mdc`
- `.gitignore`
- `17 Game Systems Architecture.md`
- `90 Glossary.md`
- `99 Open Questions.md`
- `AGENTS.md`
- `unity/.claude/skills/world-gen-assets/SKILL.md`
- `unity/Assets/Plans/AGENT_HANDOFF_WORLD_GEN.md`
- `unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md`
- `unity/Assets/Plans/LANDMASS_T1_FEASIBILITY_AUDIT.md`
- `unity/Assets/Plans/LANDSCAPE_PLANNER.md`
- `unity/Assets/Plans/OVERWORLD_TECH_DEBT.md`
- `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`
- `unity/Assets/Plans/OVERWORLD_VISUAL_DIRECTION.md`
- `unity/Assets/Plans/PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md`
- `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md`
- `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md`
- `unity/Assets/Plans/STAGE_2A_2B_CONTRACT_HARDENING.md`
- `unity/Assets/Plans/STAGE_2A_SELECTION_HARDENING.md`
- `unity/Assets/Plans/WORLD_RELIEF_PRINCIPLES.md`
- `unity/Assets/Scripts/Editor/OverworldPlayerMenu.cs`
- `unity/Assets/Scripts/Editor/OverworldTerrainDiagnostics.cs`
- `unity/Assets/Scripts/Editor/OverworldTerrainLayers.cs`
- `unity/Assets/Scripts/Editor/TerrainGenDump.cs`
- `unity/Assets/Scripts/World/Landscape/TerrainDeformer.cs`
- `unity/Assets/Scripts/World/Terrain/BiomeClassifier.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainGrid.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainTile.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainBakeFlags.cs`
- `unity/Assets/Scripts/World/TerrainGen/TerrainGenSettings.cs`
- `unity/Assets/Scripts/World/TerrainGen/WorldHeightField.cs`
- `unity/Assets/Scripts/World/TerrainGen/WorldStructure.cs`
- `unity/ProjectSettings/ProjectSettings.asset`

**MIGRATION (added)** (non-`.meta`):

- `unity/Assets/Art/Terrain/SurfaceLayers/Cliff/rock_face_diff_1k.jpg`
- `unity/Assets/Art/Terrain/SurfaceLayers/Cliff/rock_face_nor_gl_1k.exr`
- `unity/Assets/Art/Terrain/SurfaceLayers/Grass/grass_dark_diff.jpg`
- `unity/Assets/Art/Terrain/SurfaceLayers/Grass/grass_dark_nor_gl.exr`
- `unity/Assets/Art/Terrain/SurfaceLayers/Rock/rock_base_diff.jpg`
- `unity/Assets/Art/Terrain/SurfaceLayers/Rock/rock_base_nor_gl.exr`
- `unity/Assets/Art/Terrain/SurfaceLayers/Sand/sand_diff.png`
- `unity/Assets/Art/Terrain/SurfaceLayers/Sand/sand_nor_gl.png`
- `unity/Assets/Art/Terrain/SurfaceLayers/Soil/sediment_diff.jpg`
- `unity/Assets/Art/Terrain/SurfaceLayers/Soil/sediment_nor_gl.exr`
- `unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`
- `unity/Assets/Plans/UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md`
- `unity/Assets/Scripts/Editor/OverworldTerrainBake.cs`
- `unity/Assets/Scripts/Editor/OverworldTerrainReady.cs`
- `unity/Assets/Scripts/Editor/OverworldTerrainValidation.cs`
- `unity/Assets/Scripts/Editor/OverworldVegetationAssets.cs`
- `unity/Assets/Scripts/Editor/Tests/OverworldTerrainBakeTests.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainBuilder.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainField.cs`
- `unity/Assets/Scripts/World/Terrain/OverworldTerrainSampler.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainAuthorityField.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainHash.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainSurfaceClassifier.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainSurfaceRules.cs`
- `unity/Assets/Scripts/World/Terrain/TerrainVegetationRules.cs`

**DELETE** (106 tracked non-`.meta` files; names of legacy-named paths are not repeated here):

- `unity/Assets/Plans/`: 3
- `unity/Assets/Plans/` — legacy-named documents: 5
- `unity/Assets/Plans/` — the reference baseline image folder: 12
- `unity/Assets/Scripts/Editor/Tests/`: 1
- `unity/Assets/Scripts/Editor/`: 8
- `unity/Assets/Scripts/World/Terrain/`: 1
- `unity/Assets/Scripts/` — legacy-named scripts: 9
- `unity/Assets/Settings/` — the removed pipeline's settings folder: 67

**USER-EXISTING** (44 non-`.meta` paths) — untouched: TerrainGen Stage 3D/3E/3F sources, hydrology sources and tests, Stage 3 plans and dumps, water samplers and the other files the user had changed before the migration. The frozen Stage 3 sources were compared byte-for-byte with the snapshot (`CirqueCandidate`, `CirqueCandidateBuilder`, `SecondaryRidgeSkeleton`, `SecondarySpurBuilder`, `StructuralBasin`, `StructuralBasinBuilder`, `StructuralElevation`: identical); `RidgeSkeleton`, `ValleyNetwork`, `Stage3ASkeleton`, `Stage3Field`, `PrimarySpurBuilder`, `CarveField`, `InterMassifDivide` are unchanged against HEAD. The three TerrainGen files in MIGRATION (`TerrainGenSettings`, `WorldHeightField`, `WorldStructure`) differ in comments only.

Previously untracked files removed by the migration: 357 (non-`.meta`) — all belonged to the removed architecture, its spikes or its migration evidence (section 3), and are preserved in `untracked_files.tar` in the backup folder.
