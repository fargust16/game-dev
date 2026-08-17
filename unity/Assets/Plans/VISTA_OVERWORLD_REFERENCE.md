# Vista Overworld Reference (Golden Scene)

**Status:** Draft  
**Date:** 2026-08-15  
**Scene SoT:** `Assets/Scenes/Overworld_Generator.unity`  
**Vista C# API SoT (official):** [Pinwheel.Vista namespace](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.html) — start at [VistaManager](https://docs.pinwheelstud.io/vista/api/Pinwheel.Vista.VistaManager.html); LPB user guide: [Local procedural biome](https://docs.pinwheelstud.io/vista/docs/local-procedural-biome.html)  
**Terrain material:** URP TerrainLit + the canonical `TerrainLayer` assets. MicroSplat is **not** in the pipeline.  
**Related:** [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md)

---

## Purpose

This document freezes **what actually looks good** and the **modular LPB bake contract**: a **2×2 × 1400 m** grid with `LPB_IslandBase` + `LPB_Mountain_*` under one VistaManager (height **700**). Hand-authored single MountainBiome covering the whole grid remains a visual reference for erosion/texturing only.

Automated `OverworldLpbBake` is the SoT for modular geology. Prefer official Vista API docs above when wiring from C#.

When wiring Vista from C#, prefer the **official API docs** above over guessing from package sources. Package code is implementation detail; docs are the contract.

**Scope:** terrain height, the two biome graphs, surface layers, vegetation placement and the Vista Manager. Vegetation *content* is ours (Polytope meshes via `VistaVegetationTemplates`); its *placement* is Vista's.

---

## Scene inventory (golden)

| Object | Role |
|--------|------|
| `VistaManager` | Owns generate; `terrainMaxHeight = 700`, `heightMapResolution = 513`, `textureResolution = 512` |
| `Terrains` root | Unity Terrain grid |
| `OverworldTerrain_0_0` … `OverworldTerrain_1_1` | Positions `(0\|1400, 0, 0\|1400)`; tile size **1400×1400**, height **700** |
| `MountainBiome (Cloned from Template)` | Child of VistaManager — single covering LPB |
| Graph asset | `Assets/Scenes/Overworld_Generator Biomes/Mountain_TerrainGraph 1.asset` (clone of Mountain template, not our stripped NaturalIsland graph) |
| Terrain data | `Assets/Scenes/Overworld_Generator Terrains/Terrain_*_TerrainData.asset` |

Do **not** mutate package templates under `PinwheelStudio/Vista/Personal/BiomeTemplates/`. Work on scene clones / `Assets/Settings/Vista/` / `Assets/Scenes/Overworld_Generator Biomes/`.

---

## VistaManager settings (working)

| Field | Golden value | Notes |
|-------|--------------|-------|
| `terrainMaxHeight` | **700** | Vertical SoT for tiles + graph height normalization. Set from `WorldScale.TerrainHeightBudgetMeters` on every bake. |
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
| `baseResolution` | **512** | Cache resolution across the biome's own bounds. **This is Vista Personal's hard cap** (`Constants.RES_MAX`), not a choice — 1K/2K/4K need Indie or Pro, which is not a project dependency. Every node in the graph runs at this resolution. |
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

### SetBiomeHeightOnSpawn / SetBiomeAreaOnSpawn (Inspector is authoritative)

Both live on the biome **template prefabs** in `Assets/Settings/Vista/BiomeTemplates/`
(`LPB_IslandBase.prefab`, `LPB_Mountain.prefab`), authored once by `OverworldBiomeTemplates`.

| Component | Field | Owns | Mechanism |
|---|---|---|---|
| `SetBiomeHeightOnSpawn` | `worldHeight` | how many world metres the graph's full 0..1 output covers | `localScale.y = worldHeight / VistaManager.terrainMaxHeight`, plus `useTransformForHeightBlend` |
| `SetBiomeAreaOnSpawn` | `worldArea` | the biome's world XZ extent | writes a centred rectangle into `LocalProceduralBiome.anchors` |

**Both are spawn-time one-shots.** They write `transform.localScale.y` and the anchor polygon once,
from `IBiomeTemplateSpawnCallbackReceiver.OnSpawnedFromBiomeTemplate`, and never read their own
fields again. Vista's live height authority is therefore `transform.localScale.y ×
VistaManager.terrainMaxHeight` with `blendOptions.useTransformForHeightBlend` — the component is an
initializer for that scale, not a live control.

Consequences a future agent must not rediscover the hard way:

- Editing `World Height` on a biome **already in the scene** changes nothing until something
  re-applies it. Use `GameDev/Overworld/Vista/Apply Biome Inspector Settings + Regenerate`.
- `Generate All` **respawns every biome from the template prefab**, so scene-instance edits are
  discarded. The durable place to change a value is
  `Assets/Settings/Vista/BiomeTemplates/*.prefab`.
- Re-applying area rewrites the anchors as a rectangle, so the organic silhouette has to be
  re-inscribed afterwards — the apply menu does exactly that.

**`OverworldLpbBake` must never write either field, nor `transform.localScale`, `order`, `dataMask`,
`blendOptions` or `baseResolution`.** It sets only the seed, the XZ position, and an organic
silhouette **inscribed inside** the rectangle the area component produced — so changing `worldArea`
in the Inspector still changes the biome's size, and changing `worldHeight` still changes its height.

`VistaManager.terrainMaxHeight` and `WorldScale.TerrainHeightBudgetMeters` are both **700** and must
stay aligned with the tile `TerrainData.size.y`.

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
3. Keep `WorldWater` at Sea Level **24** — outside the mask (~height 0) sits underwater.
4. Re-Generate after every change.

**Falloff costs resolution.** The biome canvas covers `area + 2 × falloffDistance`, so widening the
falloff spreads the same 512 px over more ground. The island's 180 m falloff already turns a 2240 m
area into 2600 m of bounds — 5.08 m/px instead of 4.38.

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
order  0    LPB_SeaFloor     SeaFloor graph + SnapBiomeToTiles, no falloff
                             Height Replace + LayerWeightMaps; worldHeight 20 m — the world floor
order 10    LPB_IslandBase   IslandPlateau graph + harmonic asymmetric anchors + SoftFalloff mask
                             Height Replace + LayerWeightMaps; worldHeight 140 m, biome Y = 24 (sea)
order 20+   LPB_Mountain_i   MountainMassif delta + elongated ellipse anchors, one order per massif
                             Height Raise + wide Outer falloff (520) + SoftFalloff mask; worldHeight 530 m
```

Hydrology, ecology and civilization bands are **not** in the stack: V0 water is one ocean plane,
vegetation is emitted by the two land graphs themselves, and settlements/roads append their layers
after the bake. Do not add a Lower-blend water biome — see
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md) §
Future Hydrology / River Generation.

**Automated bake** (`OverworldLpbBake.SpawnIslandAndMountainStack`): SeaFloor + IslandPlateau base +
1–2 Mountain LPBs with **Raise** (not a KeepHigher stamp).

**Coast:** entirely graph + falloff. `IslandPlateau_TerrainGraph` biases its distribution toward high ground with a `Curve` (`ShelfCurve`) so the island reads as a plateau, and the LPB's **Outer falloff** drags the rim under water; the coastline silhouette comes from the seeded anchor polygon inscribed in `SetBiomeAreaOnSpawn.worldArea`, and the biome sits at world `Y = SeaLevelMeters` so its low ground is not drowned. Sand is a normalized low-altitude `Curve` band multiplied by a small noise (to break the contour ring), painted as Weight Blend priority 3. There is **no** post-generation C# coast pass — `CoastalBeachShelf` was deleted. Falloff distance and the plateau height are Inspector fields on `LPB_IslandBase.prefab`.

**Mask shapes:** Island = radius harmonics (not near-circle); Mountain = irregular ellipse + yaw; never one universal circular mask for every biome type.

**Mountain graph contract:** periphery ≈ 0 so `Raise` produces foothills rather than a stamped plateau — held by the `Levels` floor (`outLow 0.05`) plus the LPB's wide Outer falloff mask. Peaks cover a small area; two `HydraulicErosionNode2` passes (macro then micro), with the erosion mask read off the **second**. TextureOutputs include foothill grass/soil so Texture Replace under Raise does not wipe the greens. Height space is **delta** above the island, never ASL, and every altitude band is a fraction of the normalized graph height — never an absolute metre threshold.

---

## Reference architecture (what our graphs copy)

Vista's official sample biome graphs are the **architectural baseline**, not merely examples of output.
Richest one: `Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Mountain/Mountain_TerrainGraphTemplate.asset`
— 50 nodes, 48 edges, 9 groups. Supporting samples: `Samples/Graphs/MountainRange.asset` (34),
`ErodedMountainRange.asset` (51), `MoundOfEarth.asset` (11, the minimal island-like landform).
**There is no official island graph** — both of ours descend from the Mountain template.

Our builders: `IslandPlateauGraphBuilder`, `MountainMassifGraphBuilder`, sharing `VistaGraphBuild`
(edges, `Set`/`Get Variable`, canonical texture outputs, the vegetation stage).

### The stage interface

The reference graph does **not** wire its logical stages together. It publishes three mask variables and
every later stage reads them. We keep the names verbatim:

```text
Terrain generation -> Set Variable "Height Map"
                   -> Set Variable "ErosionMask"

Surface layers     -> Get Variable "Height Map" / "ErosionMask"
                   -> Set Variable "GreenAreaMask"

Vegetation         -> Get Variable "GreenAreaMask" / "Height Map"
```

Reference usage: `Height Map` 1 set / 4 gets, `ErosionMask` 1 set / 3 gets, `GreenAreaMask` 1 set / 2 gets.
All typed `Pinwheel.Vista.Graph.MaskSlot`.

### Node mapping

| Reference node | Purpose | In / Out | Why it exists | Ours |
|---|---|---|---|---|
| `NoiseNode` x2 (750/8 oct, 1000/1 oct, Perlin01) | macro + mezzo form | none / mask | the whole base shape from **two** noise fields | x2 in both graphs, **no domain warp** |
| `CombineNode(Multiply)` | mix the two scales | 2 masks / mask | "generic mountain shape" | x1 |
| `LevelsNode` out 0.05..1 | amplitude normalize | mask / mask | exposed as Min/Max Height | island `Curve`+`Remap` (ASL 30–55); mountain foothill `Curve` (Raise-delta contract) |
| `HydraulicErosionNode2` x2 (200 iter) | macro + micro drainage | height / height + soil | one pass yields **both** relief and the erosion mask | **x2 each** — macro at the node's own defaults, micro with its own feature size (see deviations) |
| `SetVariableNode "Height Map"` | publish height | height / var | the stage interface | yes, both |
| `CurveNode` then `SetVariableNode "ErosionMask"` | publish flow | soil out / var | shaped once, read by every surface mask | yes, both |
| `GetVariableNode` x9 | read without a wire | var / mask | stages stay independent | yes, both |
| `LevelsNode` from `ErosionMask` | soil in drainage | var / weight | "soil appears in the drainage" | soil layer, both |
| `ConcaveNode` + `SmoothNode` + `Combine(Mul)` with `HeightMask` | moss in low hollows | var / weight | "dark moss in hollows at low altitude" | grass layer, mountain |
| `HeightMaskNode` | height ramp in **world metres** | height / mask | the shader compares against `Args.TERRAIN_HEIGHT`, which is the **biome's own world height** (`LocalProceduralBiome.cs:1174`) — so a metre threshold silently drifts when `SetBiomeHeightOnSpawn.worldHeight` changes | **not used** — we gate altitude with `Curve`/`Levels` over the normalized `Height Map` (`VistaGraphBuild.LowAltitudeCurve`) so bands rescale with the biome |
| `Combine(Max)` + `MathNode(one-minus)` over `Curve(ErosionMask)` and `SlopeMask(0..3°)` | ridge highlight | var / weight | inverted erosion marks ridges; the flat mask kills the false positive on low-activity plains | **reproduced in both graphs** (`VistaGraphBuild.AddInvert`): island rock layer, mountain cliff layer |
| `WeightBlendNode` layerCount 5 | stack weights | 5 masks / 5 weights | output i = `in[i] * Π(1 - in[j])` for j > i — **last layer wins**, and **layer 0 unconnected = white base**, the only thing making the weights sum to 1. Nothing normalizes them downstream. | yes, both, layerCount 5 |
| `TextureOutputNode` x5 | bind a `TerrainLayer` | weight / splat | Vista sets `terrainData.terrainLayers` itself, **per tile**, from `srcLayers.Distinct()` (`AlphaMapsCombiner`/`TerrainTile.PopulateLayerWeights`), merging by asset identity and sorting by `order` | yes — **both graphs emit all five canonical outputs** (`VistaGraphBuild.AddCanonicalTextureOutputs`) so every tile gets the same stack |
| `Combine(Max)` of two green outputs, then `SetVariableNode "GreenAreaMask"` | where it reads green | weights / var | vegetation follows the paint | yes, but `SetVariable` directly — we have one green layer, so the `Combine` would do nothing |
| `Get GreenAreaMask` + `Levels` + `Combine(Mul)` with `GeometryMask`, then `Scatter`, then `TREE OUTPUT` | trees | var / instances | green plus a height/slope rule | yes, both |
| `Get GreenAreaMask` + `Levels`, then `DETAIL DENSITY OUTPUT` | grass | var / details | density from the same variable | yes, both |
| `Scatter` then `DETAIL INSTANCE OUTPUT` | grass, Polaris path | var / buffer | Polaris terrain only | **not reproduced** — see deviations |

### Deviations, and why each is justified

> **The lesson that cost the most time.** Copying the reference's *topology* while substituting our own
> numbers at every node produced terrain that looked nothing like it. Three substitutions were decisive,
> and all three were cases of overriding a Vista node's own well-chosen defaults:
>
> | We wrote | Vista's default / the reference | Effect of our version |
> |---|---|---|
> | `NoiseNode` with 3–4 octaves, no `remapCurve` | 8 octaves, `flipSign`, **`applyRemapPerLayer`** + a remap curve | Smooth blobs instead of ridged rock. Per-layer remapping is what creates the reference's character; no downstream curve recovers it. |
> | `Levels(0, 0.5, 0.55)` — a contrast stretch | `Levels(0, 0.5, 1, outLow 0.05)` — a gentle floor lift | Clipped the whole upper half to 1, flattening plateau top and massif crown into mush. |
> | `Curve` boosting the erosion mid-tones | effectively a pass-through | Turned sparse drainage into a dense signal; the soil layer then covered **75 %** of the island in sediment. |
>
> Rule: **do not override a Vista node's defaults without a stated reason.** The defaults are the
> reference's values. Set only what is genuinely per-biome — feature `scale` and `seed`.


| Deviation | Reason |
|---|---|
| Erosion **feature sizes** are ours, not the reference's (macro 8 m, micro 6 m) | The reference is authored on a 1024 px canvas; Vista Personal caps ours at 512, so its feature sizes do not transfer. The sim canvas is `clamp(biomeWidth / featureSize, 32, min(2048, 512))`, and a request above 512 px is silently clamped — which carves grid-aligned rills instead of drainage. At the current biome sizes 8 m → 328 px and 6 m → 440 px, so neither pass is clamped. Recheck these numbers whenever a biome's area or falloff changes. |
| Micro pass runs 100 iterations, half the reference's 200 | Rill instability grows with iteration count on a coarse grid, and our grid is half the reference's. |
| Mountain: plain `Levels` with `outLow 0.05` where the periphery must approach 0 | The massif is a **height delta under `HeightBlendMode.Raise`**; a flat periphery would make Raise stamp a plateau, so the falloff mask does the rest of the work. |
| Island: `Curve` + `Remap 0–1` where the reference has `Levels` | Remap is auto-levels: it stretches whatever range the shaping produced across the full 0..1. `Set Biome Height On Spawn` then decides how many metres that is, and sea level cuts the coastline out of the low ground. A remap **floor** (the old 0.55) left ~25 m of relief on a 55 m biome and forced the falloff to do all the coastal shaping — do not reintroduce one. |
| `SetVariable "GreenAreaMask"` without `Combine(Max)` | The reference has two green layers; we have one. A node with nothing to combine is a decorative node. |
| Mountain rock is the unconnected base layer; no separate "Rock Peak" layer | The reference has two rock textures; our canonical stack has one. Base fill gives the same result with five fewer nodes. |
| Mountain emits a Sand output with its Weight Blend input unconnected | Sand has no place on a massif, but every graph must emit the **full** canonical set or tiles disagree about what alphamap channel *n* means. An unconnected input is black. |
| No `DetailInstanceOutputNode` and no second `Scatter` | `TerrainTile` implements `IDetailDensityPopulator` but no detail-instance populator, so on Unity terrain those nodes would compute a buffer nothing reads. |
| No domain warp on any `NoiseNode` | Warp is the most expensive knob on the node, and the plateau/massif profile comes from the shaping curve. |
| **No snow layer in V0** | The project has no snow texture. The previous `MS_Snow_Peak` was `sand2_diff.png` faked white through `TerrainLayer.diffuseRemapMin/Max` — fields MicroSplat ignored entirely, which is why the massif rendered warm cream. `WorldScale.SnowLineMeters` is deleted with it. Re-add the layer when a real texture is imported. |
| **Proportional** bands (foothill skirt) are normalized `Curve`/`Levels`; **sea-anchored** bands (the beach) use `HeightMaskNode` metres | `Args.TERRAIN_HEIGHT` is the biome's own world height and the biome sits at world Y 0, so `HeightMaskNode` metres are metres above sea. Sea level does not move when the biome gets taller, so the beach *should* stay put — that is the one band where absolute metres are correct. A skirt that should scale with the massif must not use them. |
| Erosion nodes keep their own Macro preset; only `iterationCount` is set (200, the reference's value) | The node's defaults already are the reference macro pass. The previous hand-tuned override (140 iterations, rain 6, erosion 6) under-ran the simulation, and under-iterated erosion scratches straight parallel lines down a slope instead of competing for drainage. |
| `Curve(ErosionMask)` in front of `OneMinus` is a near-**step** (1 by t=0.01), not a ramp | Copied from the reference. The inverted result must be a thin ridge mask; a gradual ramp inverts into a broad mask that, as the top Weight Blend layer, paints the whole landform one colour. |
| Slope is used once per graph, for sheer faces only | Two adjacent slope bands used as independent layers (the old cliff 32–70° + rock 14–32°) draw concentric contour rings across a rolling plateau. The reference uses `SlopeMaskNode` only as a flatness corrective. |

### Graph shape

Authored by `IslandPlateauGraphBuilder` / `MountainMassifGraphBuilder`; the asset is the source of
truth and the numbers below must be read back from it after any rebuild.

```text
IslandPlateau_TerrainGraph
  Noise(1200, 8 oct, remapPerLayer) --+
                                      +-- Combine(Mul) -> Levels(outLow .05) -> Curve(shelf)
  Noise(1900, 1 oct) ----------------+                        |
                                      HydraulicErosion2(macro: fs 8, 200 it, default preset)
                                                              |
                                      HydraulicErosion2(micro: fs 6, 100 it, ramps 10/300)
                                                              |- height -> Set "Height Map" -> HEIGHT OUTPUT
                                                              +- soil   -> Curve(linear) -> Set "ErosionMask"

  Weight Blend(5) priority base->top:
    0 grass  (unconnected = white base)                               -> Texture Output "Grass"
    1 rock   OneMinus( Max( Curve(ErosionMask), SlopeMask(0-4) ) )    -> Texture Output "Rock"
    2 soil   Levels(ErosionMask)                                      -> Texture Output "Soil"
    3 sand   Curve(HeightMap, low band) x Noise(95)                   -> Texture Output "Sand"
    4 cliff  SlopeMask(38-90, rising curve)                           -> Texture Output "Cliff"
  Vegetation: Weight Blend layer 0 x DryLandGate(BiomeMask) -> Set "GreenAreaMask"
              -> 5 canopy species (own cluster noise each) -> Set "CanopyMask"
              -> understory (gated by blurred CanopyMask), rocks (inverse green), ground cover

MountainMassif_TerrainGraph
  Noise(750, 8 oct, remapPerLayer) ---+
                                      +-- Combine(Mul) -> Levels(outLow .05)
  Noise(1000, 1 oct) ----------------+                        |
                                      HydraulicErosion2(macro) -> HydraulicErosion2(micro)
                                                              |- height -> Set "Height Map" -> HEIGHT OUTPUT
                                                              +- soil   -> Curve(linear) -> Set "ErosionMask"

  Weight Blend(5) priority base->top:
    0 rock   (unconnected = white base)                               -> Texture Output "Rock"
    1 soil   Levels(ErosionMask)                                      -> Texture Output "Soil"
    2 grass  Concave -> Smooth  x  Curve(HeightMap, foothill band)    -> Texture Output "Grass"
    3 sand   (unconnected = black, emitted for stack parity)          -> Texture Output "Sand"
    4 cliff  OneMinus( Max( Curve(ErosionMask), SlopeMask(0-3) ) )    -> Texture Output "Cliff"
  Vegetation: Weight Blend layer 2 -> same shape as the island graph, minus ground cover
              (two graphs emitting the same DetailTemplate send Vista down its merge path)

Both graphs emit all five canonical Texture Outputs with order = (int)SurfaceLayer, so every tile's
terrainLayers array is [Grass, Sand, Soil, Cliff, Rock] regardless of which biomes covered it.
```

The rules that follow from all of this live in `unity/.claude/skills/world-gen-assets/SKILL.md`
section "Vista graph authoring", and in `AGENTS.md`.

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
| Registration | Each tile has Vista `TerrainTile` with same `managerId` as VistaManager |
| Size pitfall | After setting `heightmapResolution`, **re-pin** `TerrainData.size` to `(1400, 700, 1400)` — Unity preserves m/px and can inflate size to 22400 |
| Names | Unique per tile (`OverworldTerrain_0` …) |
| Shading | **URP TerrainLit** (`OverworldTerrain.mat`) reading the `TerrainLayer` assets Vista binds. **MicroSplat is out of the pipeline** — its shader only consumes `diffuseTexture`/`normalMapTexture` and ignores `diffuseRemap*`, `tileSize` and `smoothness`. |
| Layer array | **Vista owns `terrainData.terrainLayers`, per tile.** C# never assigns it. Both graphs emit all five canonical outputs so the arrays match across tiles. |
| Seams | Vista's own `TerrainTile.MatchSeams()` after neighbour wiring — no custom stitcher. |
| Grass | **Polytope Lowpoly `PT_Grass_02`** through the graphs' `DETAIL DENSITY OUTPUT` and Unity's terrain detail renderer. There is no second grass system. |

---

## What we did wrong (vs this scene)

| Our automated path | Golden scene |
|--------------------|--------------|
| Custom `NaturalIsland_TerrainGraph` (stripped Noise+Ridge+single erosion) | Full Mountain-template **architecture**, reproduced in `IslandPlateauGraphBuilder` / `MountainMassifGraphBuilder` |
| `terrainMaxHeight` / tile Y = **220** | **700** |
| `Space.World` + metre anchors + `localScale=1` + falloff 0 | Template **Local** + prefab pos/scale + Outer falloff + seeded anchor ring |
| Heavy C# framing + lake carve after Vista | **Leave Vista height alone** (no pancake framing, no inland hydrology) |
| Mutating node seeds via InjectSeeds | Drive **`biome.seed` only** |
| Assumed dual Hydraulic always OOMs/clashes | Two `HydraulicErosionNode2` passes run fine at 2×2 / 2800 m; the reference has two |
| Copying the reference's numbers as well as its topology | Reference numbers are authored for a **1024 px** canvas; on Personal's 512 they have to be re-derived |

Root visual failures:

1. **220 m budget** crushed relief → pancake.
2. **Stripped graph** lacked Mountain template shaping.
3. **C# framing/lakes** punched holes and flattened coasts after a good Vista bake.

---

## Operator checklist (reproduce quality)

1. Open `Overworld_Generator`.
2. Confirm VistaManager max height **700** and four tiles **1400 / 700**.
3. Tune the world in the Inspector, not in C#:
   - biome height / area → `LPB_IslandBase.prefab`, `LPB_Mountain.prefab`
     (`Set Biome Height On Spawn`, `Set Biome Area On Spawn`, falloff distance, blend options);
   - terrain shape and surface distribution → the two Terrain Graph assets in `Assets/Settings/Vista/`;
   - surface look (albedo, normal, UV tiling, smoothness) → the five `TerrainLayer` assets in
     `Assets/Settings/TerrainLayers/Overworld/`;
   - vegetation content → the templates in `Assets/Settings/Vista/Vegetation/`
     (`Pine_`, `PinePale_`, `Fruit_`, `FruitDeep_`, `Orchard_`, `Undergrowth_`, `Rock_TreeTemplate`,
     `Meadow_DetailTemplate`) — one prefab per template, because Vista Personal ignores
     `prefabVariants`.
4. `GameDev/Overworld/Generate All`.
5. When materials look wrong, run `GameDev/Overworld/Vista/Dump Layer Weights` **before** touching the
   graph: it prints each tile's layer stack and the per-pixel weight sum (must be ≈1) and exports one
   PNG per layer, which separates a mask problem from a layer-mapping problem.

---

## Automated bake contract

Contract for `GameDev/Overworld/Generate Terrain From Seed` (`OverworldTerrainMenu`). Matches golden quality; Clear always re-spawns (do not rely on hand-placed LPB surviving Clear).

### Spawn

| Step | Rule |
|------|------|
| Templates | Our own, in `Assets/Settings/Vista/BiomeTemplates/`: `SeaFloor_`, `IslandBase_`, `Mountain_BiomeTemplate.asset` + their `LPB_*.prefab`. Authored once by `OverworldBiomeTemplates`; **never** edit the package templates under `PinwheelStudio/.../BiomeTemplates/`. |
| API | `BiomeTemplateSpawner.Spawn(template, new BiomeTemplateSpawnContext(manager))` under VistaManager |
| Graph clone | Persist owned `TerrainGraph` clones from the spawn context to `Assets/Settings/Vista/` (`IslandBase_IslandPlateau_TerrainGraph.asset`, `Mountain_0_MountainMassif_TerrainGraph.asset`, …) |
| Height | `VistaManager.terrainMaxHeight` = `WorldScale.TerrainHeightBudgetMeters` = **700**; each biome's own height is `SetBiomeHeightOnSpawn.worldHeight` on its prefab (seabed 20, island 140, massif 530) |
| Seed | `biome.seed` from the matching named fork only (`terrain.seafloor` / `terrain.islandbase` / `terrain.mountain.{i}`) — never touch graph node seeds |
| What the bake may write | **Only** seed, XZ position, and the anchor silhouette. Height, area, order, `dataMask`, `blendOptions`, `baseResolution` and `localScale` belong to the prefab. |

### Local ↔ world anchors

World point → local: `local = biome.transform.InverseTransformPoint(world)`;
back again: `biome.transform.TransformPoint(local)`.

`SetBiomeAreaOnSpawn` writes a centred **rectangle** into `biome.anchors`; `OverworldLpbBake` then
treats that rectangle as the authoritative *extent* and inscribes an organic silhouette inside it, so
changing `worldArea` in the Inspector still changes the biome's size.

### Island / massif silhouettes (`OverworldLpbBake`)

| Constant | Value |
|----------|-------|
| Island anchors | **36**, radius harmonics (3 terms) + seeded noise, Chaikin-smoothed ×2 |
| Island asymmetry | Cliff coast reaches to 1.0, the opposite beach coast pulls in to 0.72, yaw from `terrain.coast.yaw` |
| Island area / falloff | `SetBiomeAreaOnSpawn.worldArea` **2240 m**, `falloffDistance` **180** — on the prefab |
| Massif anchors | **28**, irregular ellipse (2 harmonics) rotated to a seeded yaw, smoothed ×2 |
| Massif area / falloff | **1050 × 450 m**, `falloffDistance` **520**; a second massif is scaled to 0.38 |
| Massif count / spacing | 1 (72 % of seeds) or 2, at least `PeakMinSeparationMeters` (480) apart |
| `falloffDirection` | Outer, on both |

Never a square island and never one universal circular mask — and remember that widening a falloff
spends biome-canvas resolution (see the falloff note above).

### Post-pass matrix

| After Vista GenerateAll | V1 |
|-------------------------|----|
| SyncFromGpu | On |
| ForceTileResolutionAndSize (1400, 700, 1400) | On |
| LogRange | On |
| `TerrainTile.MatchSeams` (Vista native) | On |
| EnsureOceanSurface | On |
| Assigning `terrainData.terrainLayers` from C# | **Off** — Vista owns it |
| MicroSplat Convert | **Off** — out of the pipeline |
| C# splat painting (`SetAlphamaps`) | **Off** — deleted |
| C# coast pass (`CoastalBeachShelf`) | **Off** — deleted |
| Custom seam stitcher (`TerrainTileSeams`) | **Off** — deleted |
| `IslandNaturalGeography` / `IslandOccupancy` / StampIT | **Off** — deleted |
| Inland lake / river generation | **Off** — deleted; V0 water is the ocean plane only |

### Population / vegetation

- The two land biomes request the full `BiomeDataMask` (height, hole, layer weights, tree instances,
  detail density) on the template prefab — Vista discards any graph stage the mask does not ask for.
  The seabed deliberately asks for height + layer weights only, so Vista does not try to pull
  vegetation outputs its graph does not have.
- Canopy SoT: the graphs' **TREE OUTPUT**, one template per species (Vista Personal ignores
  `prefabVariants`, so a colour or fruit variant has to be its own species with its own cluster
  noise).
- Understory and rock dressing: also **TREE OUTPUT** — bushes gated by the blurred `CanopyMask`,
  rocks driven by the inverse of `GreenAreaMask`.
- Ground cover: the island graph's **DETAIL DENSITY OUTPUT** with `Meadow_DetailTemplate`
  (Polytope Lowpoly `PT_Grass_02`), rendered by Unity's terrain detail pass. The massif opts out —
  two graphs emitting the same `DetailTemplate` send Vista down its merge path.
- `VegetationScatterMenu`, `GrassField`, `GrassDistribution` and `VistaDetailDensity` are **deleted**.
  There is no GameObject scatter pass — do not reintroduce a second placer.
- Vegetation Studio Pro: **deprecated**; Rowlan VSP Extensions stay under `Assets/_Disabled/`.
- **Vegetation is accepted for V0** on all four tiles. Touch it only to repair a regression a terrain
  change caused.

### Acceptance checklist

- Logs: `size=(1400,700,1400)` on all tiles (not 22400).
- Landmass with a soft drowned rim (Outer falloff); not a circular N-gon; not ~100% land per tile.
- Continuous Mountain-quality relief.
- `Dump Layer Weights`: every tile reports the same five layers in canonical order, and the per-pixel
  weight sum is ≈1.000.
- Material transitions follow terrain morphology (drainage → soil, hollows → grass, sheer → cliff,
  ridges → rock). No contour rings, no isolated patches.
- Changing `Set Biome Height On Spawn` on `LPB_Mountain.prefab` changes the massif height; nothing in
  the console reports overwriting it.
- Changing `Set Biome Area On Spawn` changes the biome footprint.
- An edit to a Terrain Graph asset survives `Generate All`.
- `Dump Terrain Geometry`: no axis-aligned banding above the noise floor, and no dominant
  autocorrelation lag matching a canvas cell size (island 5.08 m, massif 4.08 m, erosion sims 5.9 /
  7.9 m). Judge at close, medium and distant views plus the four tile boundaries — a close-up alone
  decides nothing.
- The console reports no `Hydraulic Erosion 2: Feature Size … needs a … px sim canvas` warning. If it
  does, a biome grew and its feature size now clamps.

## Documentation checks

- Glossary **Vista Biome** / island framing points here.
- Height budget 700 locked in `WorldScale.TerrainHeightBudgetMeters` and `VistaManager.terrainMaxHeight`.
- Island framing Open Question resolved: Anchors + Falloff.
- Vista Personal's 512 px biome canvas is stated here, in `VISTA_TERRAIN_PLAYBOOK.md` §2.4,
  `OVERWORLD_V0_PLAN.md` and `AGENTS.md`. Raising it is not an accepted fix for a terrain artifact.
