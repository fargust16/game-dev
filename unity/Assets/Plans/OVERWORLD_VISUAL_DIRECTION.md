# Overworld Visual Direction

**Status:** Draft  
**Date:** 2026-08-15  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md), [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md), `RoadSurfaceStyle`, `GrassMask`, `OverworldTerrainLayers`, `OverworldBiomeTemplates`

Art-direction behavior for overworld color, splat, roads, grass, water, and grading. Numbers below are **guides**, not mandatory literals — the visual result wins.

## How to read this / the reference sheets

Desired feel: one warm living fantasy world with soft joins — not four hard-colored biomes or stepped masks.

1. **No stair edges.** Roads, biome paints, mountain materials, and grass must read as wide soft zones. If a mask is low-res or stamped from disks, fix the SDF / resolution / rasterization — do not only raise blur.
2. **Grass stays on natural land.** Density / height / hue / patchiness change across biomes; coverage does not die at biome borders.
3. **Biome = character stack**, not only Terrain color (grass, exposure, small veg, details).
4. **Roads are worn packed earth** (dry center, darker edges, gravel, soft grass fade) — not a brown stripe.
5. **Mountains:** material gradient grass→rock→snow by height+slope+noise. **Do not reshape mountain geometry** for material joins.
6. **No distance fog / white wash** — distant panorama stays readable.
7. **Water** natural blue-green depth; warm day grading without bleaching.
8. **Performance first** — few reusable masks + cheap blends; do not stack expensive fullscreen/shader layers.

## Terrain surfaces (V0)

**SoT weights:** the Vista biome graphs' `Weight Blend` → `TEXTURE OUTPUT` (`IslandPlateauGraphBuilder`,
`MountainMassifGraphBuilder`). Every mask is derived from the graph variables `Height Map` / `ErosionMask`.  
**SoT material:** URP **TerrainLit** (`OverworldTerrain.mat`) over the `TerrainLayer` assets Vista binds.
MicroSplat is **out of the pipeline** — no Convert, no keyword surgery, no texture arrays.  
**SoT layer array:** Vista, per tile (`TerrainTile.PopulateLayerWeights`). **C# never assigns
`terrainData.terrainLayers` and never writes alphamaps.**  
**SoT coast geometry:** `IslandPlateau_TerrainGraph` + the LPB Outer falloff. There is no C# coast pass.  
**Diagnostics:** `GameDev/Overworld/Vista/Dump Layer Weights` — per-tile layer stack, per-layer
min/avg/max, per-pixel weight sum (must be ≈1) and one PNG per layer.

### Weight Blend contract

`WeightsBlend.Blend` gives `output[i] = in[i] * Π(1 - in[j])` for every j above i. Consequences:

- the **last** connected layer wins — blend index is priority, not identity;
- **layer 0 must stay unconnected**, so Vista fills it white; that is the only thing making the weights
  sum to 1, and nothing downstream normalizes them;
- overlapping masks are fine and expected — higher layers simply erode lower ones.

### Canonical layer stack

Five shared `TerrainLayer` assets in `Assets/Settings/TerrainLayers/Overworld/`. The single factory is
`OverworldTerrainLayers.EnsureCanonicalLayers()` / `GetCanonicalLayer(SurfaceLayer)`; both builders bind
`TextureOutputNode.terrainLayer` from there, and `TextureOutputNode.order` **is** the index below.
Vista merges biome layers by asset identity, so both graphs must agree — and **both emit all five**,
so every tile ends up with the same array.

| Index | Layer asset | Island mask (blend priority) | Mountain mask (blend priority) |
|-------|-------------|------------------------------|--------------------------------|
| 0 | `Overworld_Grass` | base fill — blend layer 0, unconnected (0) | `Concave` → `Smooth` × foothill curve (2) |
| 1 | `Overworld_Sand` | low-altitude curve × breakup noise (3) | unconnected → black (3) |
| 2 | `Overworld_Soil` | `Levels(ErosionMask)` — drainage (2) | `Levels(ErosionMask)` — drainage (1) |
| 3 | `Overworld_Cliff` | `SlopeMask` 38–62° (4) | `OneMinus(Max(Curve(Erosion), SlopeMask 0–3°))` — ridges (4) |
| 4 | `Overworld_Rock` | `OneMinus(Max(Curve(Erosion), SlopeMask 0–4°))` — ridges (1) | base fill — blend layer 0, unconnected (0) |

**No snow layer in V0.** The old `MS_Snow_Peak` was the Vista `Sand2` texture faked white through
`TerrainLayer.diffuseRemapMin/Max` — fields MicroSplat never read, which is why the massif rendered as
warm cream sand. Re-add a snow layer only when a real snow texture is imported.

Layer appearance (albedo, normal, UV tiling, smoothness, diffuse remap) is an **Inspector property of
the layer asset**. Never set it from generation code, and never build a parallel stack after the bake.

### Surface rules (operators)

1. **Material placement must follow terrain morphology.** Drainage → soil, hollows → grass, sheer faces
   → cliff, ridges → rock, shore → sand. If it reads as coloured patches projected onto the terrain,
   the mask is wrong — not the shader, and not the amount of noise.
2. **Altitude bands are normalized**, expressed as `Curve`/`Levels` over the `Height Map` variable.
   `HeightMaskNode` compares against the biome's own world height, so metre thresholds drift the moment
   `Set Biome Height On Spawn` changes.
3. **Slope once per graph, sheer faces only.** Two adjacent slope bands as independent layers draw
   concentric contour rings — that was the old cliff 32–70° + rock 14–32° pair.
4. **Never add noise to hide a mask problem.** Diagnose with Dump Layer Weights first: wrong weights are
   a graph problem, wrong layer order is an integration problem, and they need opposite fixes.
5. **Mountain rock may stay steep**; do not flatten massifs for material joins
   ([rule 5](#how-to-read-this--the-reference-sheets)).
6. Plateau height and island extent are Inspector fields on `LPB_IslandBase.prefab`
   (`Set Biome Height On Spawn`, `Set Biome Area On Spawn`), not constants in C#.

### Bake order (surfaces)

```text
Vista LPB bake — heights (IslandBase Replace + Mountain Raise)
             + layer weights (graph Weight Blend → 5 TEXTURE OUTPUTs per biome)
             + trees (graph TREE OUTPUT → Unity TreeInstances)
             + grass density (graph DETAIL DENSITY OUTPUT → Unity terrain details)
  → SyncFromGpu + pin tile resolution/size
  → TerrainTile.MatchSeams (Vista native)
  → EnsureOceanSurface
```

That is the whole surface pipeline. Nothing paints, reshapes or recolours after Vista.

### Root causes (coast surfaces — do not reintroduce)

| Symptom | Cause | Fix |
|---------|--------|-----|
| Mesa / cake rim | Plateau ~90 m + short Outer falloff + slope-gated coast | Plateau ~55 m; falloff ~420; distance-based ≤12° carve |
| No sand (even underwater) | Hard `beachW` gate / weak shore weights / HybridHeightBlend | Height-band dry sand; strong wet sand near land; strip HybridHeightBlend |
| Green→brown→green cake | Rock/cliff on moderate coastal slope | Suppress cliff/rock in sand ASL band &lt;58°; sandDominant |
| Sawtooth / “ladder” rock at sand edge | Integer flood distance terraces + rock on step faces | `SmoothCoastBand` after carve; stronger shore↔grass feather |
| Sand ring / donut only | Flood-distance or hard ASL contour paint | Dry sand by ASL + Perlin threshold noise; multi-pass blur |
| Contour “staircase” sand↔grass | Hard height threshold + weak feather | Wider fade (≈30 m) + noise ±7 m + SoftenAllLayerBlends |
| Black/white diagonal tile crack | Per-tile beach/splat blur + no `SetNeighbors` | `TerrainTileSeams` stitch + WireNeighbors; WARNING logs maxΔh / maxΔsplat |
| Sand in alphamap but Scene looks green | shoreMax ≲0.4 or control not synced | Force shore ≥0.8 near shoreline; Convert → re-paint → SyncAll |

Full island geology contract: [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md), [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md).

---

## Goals

- Bright, cohesive, warm-but-natural palette (saturated, not neon).
- Soft biome transitions (~tens of metres) via displace-before-threshold + blur + 30–60 m noise.
- Grass on green dry land with min density > 0; type/density change, not hard cutouts.
- Grass reads as the same ecosystem as the ground under it (both come from the graph's green mask).
- City/outskirts = settlement Dust splat with soft edges, not a new `BiomeId`.

## Island / ocean framing

- Full **square** Unity Terrain (1400 m); Vista + `IslandNaturalGeography` build geology; slim `IslandOccupancy` sinks the **border** below **Sea Level**.
- **One** water plane = **WaterWorks** `WorldWater` at `SeaLevelMeters` (ocean + inland lakes as closed basins). No per-lake water objects. OCW demoted to fallback.
- Spec: [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md).

## Resolution audit (1400 m tile, Vista heightmap ~513)

| Map | Resolution | ≈ m / px | Notes |
|-----|------------|----------|-------|
| Height | ~513 | ~2.7 m | Geometry; do not raise casually |
| Alphamap / splat | ~512 | ~2.7 m | Enough for 15–35 m biome joins if masks are continuous |
| Road lawn bake | 1536 | ~0.9 m | Needed for ~1 m road shoulders |
| Grass density sample | 1.5 m cell | — | CPU bake grid |
| Grass tint bake | 384 | ~3.6 m | Soft-blended; low cost |

Do **not** chase soft edges by jumping to 2k–4k alphamaps. Fix SDF / threshold / hard rect stamps first. Guide splat ~4 m/px is a ceiling for cost, not a target to upscale past.

## Root causes we already hit (do not reintroduce)

| Symptom | Cause | Fix |
|---------|--------|-----|
| Scalloped / stepped road edges | Clearance samples stamped as disks (6 m spacing) | Segment SDF + denser samples (~1.25 m) |
| Contour “ladder” on mountains | Threshold on raw height only | Displace height with noise **before** paint Threshold; blur paint masks — height graph unchanged |
| Hard grass / tint seams | Ecology/tint from hard `BiomeId` | `EvaluateSoft` / `TintSoft` blend neighbor Classify bands |
| Sub-pixel road shoulders | Lawn mask ~2 m/px | Lawn bake ~1536 on tile (~1 m/px) |
| Distant gray/white wash | `RenderSettings.fog` + heavy ColorAdjustments / SMH | Fog **off**; mild grade only |
| Hard rectangular painted region | `SettlementDustPaint` axis pads/streets with hard coverage | Soft rect falloff (~2.25 m) + mild noise |
| Full-tile rectangle overlay | `OverworldVegetationDebug` Quad | Cleared on Generate Surface |
| Road staircase | Soft rim 1.2 m << alphamap px (~2.7 m) | `SoftRimMeters` >= **2.5 alphamap texels**; lower TransitionCoverage |
| Huge gravel on flats | `highlandBandMask` height-only from GreenMax | Slope gate + higher gravel floor (paint only) |
| Hitch on chunk entry | A second CPU grass system baking chunks during rendering | Grass is Unity terrain detail from the graph's density map |

## Performance budget

Prefer deriving look from: **Biome / Road / Height / Slope / Water / low-freq Noise**.

Avoid: volumetric fog, heavy fullscreen FX, 4× per-pixel splat noise, stacking more TerrainLayers for every cosmetic, 2k+ alphamaps “to hide stairs”.

Colors come from the `TerrainLayer` assets themselves (see § Terrain surfaces).

## Terrain shader

- **Shader:** URP `Universal Render Pipeline/Terrain/Lit`, material `Assets/Settings/OverworldTerrain.mat`.
- **Why not MicroSplat:** only Core is imported (no URP adapter), and its texture arrays read a layer's
  `diffuseTexture`/`normalMapTexture` only — `diffuseRemapMin/Max`, `tileSize` and `smoothness` are
  dropped. Every albedo tweak we made through those fields was invisible in the shader, which is how a
  sand texture ended up rendering as "snow" on the massif.
- **Bake SoT:** Vista assigns both the layer array and the alphamaps; nothing runs after it.
- **Grass SoT:** the graphs' `DETAIL DENSITY OUTPUT` with `Meadow_DetailTemplate` (Polytope Lowpoly
  `PT_Grass_02`), rendered by Unity's terrain detail pass. Not a second GPU grass system. Not
  Vegetation Studio Pro (deprecated for this project).
- Custom `GameDev/Overworld/Terrain` fork is **deleted** — do not recreate or assign it.
- Albedo tile sizes stay **8-16 m**; remaps stay on `TerrainLayer.diffuseRemap*`. Both are Inspector
  fields on the layer asset — never set from generation code.

## Biome looks (paint layers)

Classification stays `Meadow / Forest / Swamp / Highland / Mountain`. Paint stack (bottom → top):

1. `Grass_Fields` — leftover background (Meadow/Fields)
2. `Dirt_Swamp` — wet × low
3. `Grass_Forest` — mid green band
4. `Gravel_Highland`
5. `Cliff_Rock` — high or steep
6. `Snow_Muted` — peaks (height + noise; not on steep faces)

| Look | Character |
|------|-----------|
| Fields | Warm sunny lime / olive / ochre; medium grass + flowers |
| Forest | Cool saturated deep greens; sparse understory, heavy shade |
| Swamp | Humid dark murky green; sparse grass, stones, water |
| Dust | Warm light tan / dust (settlement suburbs only) |
| Highland | Muted stone-beige |
| Rock | Cool–warm grey stone |
| Snow | Bright low-sat cool white |

Mask blend: green/dirt soft (`bandBlend` ~ **0.10-0.12**); highland/rock use `highlandBlend` ~ **0.05** + **slope gate** (gravel only on real rises). Soften size smaller for highland (~28 m). Transition density floor **0.25-0.35**.

### Mountains

- Base: bare rock. Soil follows the drainage, grass fills the foothill hollows, cliff marks the ridges.
- No snow layer in V0 — see § Terrain surfaces. Re-add one only with a real snow texture.
- No concentric rings — joins follow morphology (erosion, concavity, normalized height), never
  geometric altitude circles.
- Massif height and footprint are Inspector fields on `LPB_Mountain.prefab`, not constants in C#.

## Roads

Height corridor widths stay in `WorldScale` (Primary 4 m + shoulder 2.5 m, etc.).

| Zone | Width | Look |
|------|-------|------|
| Packed bed | `halfRoad` | Light dry center + darker edge + gravel speckles |
| Soft edge | `max(1.2 m, 2.5 alphamap texels)` (~6.8 m on _513) | TransitionCoverage ~0.4; BedCoverage ~0.9 |

Grass: **0 on packed bed**; lawn only in soft shoulder (`RoadLawnMinAtCenter = 0`). Pads/water stay clear.

Avoid stepped height LOD — keep terrain pixel error / smoothing as Vista / Unity Terrain already does.

## Grass

| Param | Range |
|-------|-------|
| Height | **0.3–1.5 m** (short/tall `PT_Grass_02`) |
| Density on green land | **0.3–1.0** (Fields high; Forest understory ~0.3–0.6) |
| Transition floor | **0.25–0.35** |
| Scatter / jitter | **0.3–0.6** |
| Types | short, tall, poppies (Meadow), rocks (highland) |

Placement comes from the terrain graphs' `GreenAreaMask` → `Levels` → `DETAIL DENSITY OUTPUT`, rendered
by Unity's terrain detail pass. Sizes live on `Meadow_DetailTemplate` (Inspector). The table above is
the visual target; tune it on the template and in the graph, not in C#.

## Water

| Depth | Color |
|-------|-------|
| Shallow / shore | Muted blue-green / teal, clearer |
| Deep | Dark blue-green |
| Swamp lakes | Separate olive murk |

**Stack:** WaterWorks `WorldWater` plane only — at `SeaLevelMeters`; lakes are terrain depressions intersecting the same plane. No per-lake meshes. OCW demoted. Avoid plastic cyan.

## Sky / lighting / grading

- **Sky:** Free HDR Sky cubemap (`Assets/Skybox/`) + Camera **Skybox** clear flags.
- **Sun:** one Directional Light as **Sun Source**, yaw/pitch matched to HDRI sun disk (`OverworldTerrainMenu`).
- **Shadows:** Soft Shadows, **4 cascades**, shadow distance ~**350 m**.
- Scene Volume on overworld lab (not global DefaultVolumeProfile): day sun CCT feel **5500–6500 K**; mild ColorAdjustments only (no bleach); **Fog off**.
- Soft AO stays a renderer feature if enabled — not forced on the volume stack.

## Definition of Done (this visual pass)

- No obvious staircase biome / road boundaries
- Grass across natural biomes; density/height/hue change gradually
- Grass tint matches terrain palette midpoints
- Roads readable, soft shoulders, not painted polygons
- Mountains without concentric material rings (geometry unchanged)
- Settlement dust without hard rectangles; debug Quad cleared on bake
- Distant world not gray/white fog
- Water natural blue-green in palette
- World warmer / clearer without global saturation bomb
- No new expensive shader stacks to hide artifacts
- No grass on packed road bed
- Gravel/rock only on foothill/steep rises (not flat plains)
- Entering a new 64 m grass chunk does not hitch (~<=1 bake/frame)
- Terrain render = URP TerrainLit over Vista's layer array; not MicroSplat, not `GameDev/Overworld/Terrain`
- Material transitions follow terrain morphology; no contour rings, no isolated colour patches
- Water = WaterWorks `WorldWater` at Sea Level; island shelf below sea reads as ocean; inland lakes = closed basins

## Verification

EditMode: SoftRim >=2.5 texels, `RoadLawnMinAtCenter==0`, island silhouette inscribed in the authored
biome area, highland slope gate, OCW water, dust soft rect.
Diagnostic: `Dump Layer Weights` — identical five-layer stack on every tile, per-pixel weight sum ≈1.
Visual: Generate All → island-in-ocean, HDRI sky, soft shadows, warm fields, cooler forest, soft joins,
muted teal shallows, materials that follow the terrain rather than sit on it.

