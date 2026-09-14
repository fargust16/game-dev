# Overworld Visual Direction

**Status:** Draft  
**Date:** 2026-08-15  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md), `RoadSurfaceStyle`, `OverworldTerrainLayers`, `TerrainSurfaceRules`, `TerrainVegetationRules`, `OverworldVegetationAssets`

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

**SoT weights:** `TerrainSurfaceRules` evaluated by `TerrainSurfaceClassifier` at every alphamap texel and
written by `OverworldTerrainBuilder` with `SetAlphamaps`. Every mask is a world quantity — height in
metres, slope in degrees, drainage area in square metres, convexity in metres, massif uplift in metres.
Full rule table: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §3.
**SoT material:** URP **TerrainLit** (`OverworldTerrain.mat`) over the five canonical `TerrainLayer` assets.
MicroSplat is **out of the pipeline** — no Convert, no keyword surgery, no texture arrays.
**SoT layer array:** the terrain bake assigns the same five layers to every tile before it paints.
**SoT coast geometry:** TerrainGen Stage 1. There is no coast pass in the bake.
**Diagnostics:** `OverworldTerrainValidation.RunBatch` — per-tile alphamap hashes, per-texel weight sums
(must be ≈1), seams, and layer texture paths.

### Priority cover

The classifier applies the layers in priority order — **sand → soil → rock → cliff** over a grass base —
and each one covers what lies under it by its own mask: every weight is scaled by `1 − mask`, then the
layer gains `mask`. Consequences:

- a higher-priority layer wins where masks overlap; the order is the rule, not the asset index;
- the five weights sum to one by construction, so nothing downstream normalizes them;
- overlapping masks are fine and expected — higher layers simply erode lower ones.

### Canonical layer stack

Five shared `TerrainLayer` assets in `Assets/Settings/TerrainLayers/Overworld/`. The single factory is
`OverworldTerrainLayers.EnsureCanonicalLayers()` / `GetCanonicalLayer(TerrainSurfaceLayer)`, and the
enum value **is** the index below. Textures live under `Assets/Art/Terrain/SurfaceLayers/`.

| Index | Layer asset | Mask |
|-------|-------------|------|
| 0 | `Overworld_Grass` | base — whatever the layers above leave |
| 1 | `Overworld_Sand` | ground below the vegetation floor (full below 27.0 m, none above 28.4 m), seabed included |
| 2 | `Overworld_Soil` | upstream drainage area 20 000 → 150 000 m² on dry land, slope ≤ 30° |
| 3 | `Overworld_Cliff` | sheer dry faces, 50° → 58° |
| 4 | `Overworld_Rock` | the strongest of: sloped seabed below 22–24 m; high massif ground 110 → 230 m; massif ridges; dry ground past the angle of repose, 35° → 42° |

**No snow layer in V0.** An earlier snow layer was a sand texture faked white through
`TerrainLayer.diffuseRemapMin/Max` — fields MicroSplat never read, which is why the massif rendered as
warm cream sand. Re-add a snow layer only when a real snow texture is imported.

Layer appearance (albedo, normal, UV tiling, smoothness, diffuse remap) is an **Inspector property of
the layer asset**. Never set it from generation code, and never build a parallel stack after the bake.

### Surface rules (operators)

1. **Material placement must follow terrain morphology.** Drainage → soil, open land → grass, sheer faces
   → cliff, ridges and bare steep ground → rock, shore → sand. If it reads as coloured patches projected
   onto the terrain, the mask is wrong — not the shader, and not the amount of noise.
2. **Thresholds are world quantities**, stored on `TerrainSurfaceRules` and labelled measured or
   authoring default. No normalised per-biome heights.
3. **Slope is true world slope.** A steep-rock band and a cliff band are one ordered pair of the same
   slope quantity, not two independent layers drawing concentric contour rings.
4. **Never add noise to hide a mask problem.** Diagnose with the validation batch first: wrong weights are
   a rule problem, wrong layer order is an integration problem, and they need opposite fixes.
5. **Mountain rock may stay steep**; do not flatten massifs for material joins
   ([rule 5](#how-to-read-this--the-reference-sheets)).
6. **Coastal land never gets seabed rock** — the seabed rock mask is zero at the 24 m waterline.

### Bake order (surfaces)

```text
TerrainGen field → one world lattice (height, uplift, coast distance, slope, convexity, drainage)
  → [optional hydrology carve]
  → heights (SetHeights) → layer weights (SetAlphamaps) → trees (SetTreeInstances)
    → ground cover (SetDetailLayer)
  → EnsureOceanSurface
```

That is the whole surface pipeline. Roads and settlements append their own layers afterwards; nothing
else paints, reshapes or recolours. Tile seams are exact by construction — there is no stitching pass.

### Root causes (coast surfaces — do not reintroduce)

| Symptom | Cause | Fix |
|---------|--------|-----|
| Mesa / cake rim | Plateau ~90 m + short Outer falloff + slope-gated coast | Plateau ~55 m; falloff ~420; distance-based ≤12° carve |
| No sand (even underwater) | Hard `beachW` gate / weak shore weights / HybridHeightBlend | Height-band dry sand; strong wet sand near land; strip HybridHeightBlend |
| Green→brown→green cake | Rock/cliff on moderate coastal slope | Suppress cliff/rock in sand ASL band &lt;58°; sandDominant |
| Sawtooth / “ladder” rock at sand edge | Integer flood distance terraces + rock on step faces | `SmoothCoastBand` after carve; stronger shore↔grass feather |
| Sand ring / donut only | Flood-distance or hard ASL contour paint | Dry sand by ASL + Perlin threshold noise; multi-pass blur |
| Contour “staircase” sand↔grass | Hard height threshold + weak feather | Wider fade (≈30 m) + noise ±7 m + SoftenAllLayerBlends |
| Black/white diagonal tile crack | Per-tile beach/splat blur + no `SetNeighbors` | One world lattice + rules that depend on world position only; `SetNeighbors` on the grid |
| Sand in alphamap but Scene looks green | shoreMax ≲0.4 or control not synced | Force shore ≥0.8 near shoreline; Convert → re-paint → SyncAll |

Full terrain contract: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).

---

## Goals

- Bright, cohesive, warm-but-natural palette (saturated, not neon).
- Soft biome transitions (~tens of metres) via displace-before-threshold + blur + 30–60 m noise.
- Grass on green dry land with min density > 0; type/density change, not hard cutouts.
- Grass reads as the same ecosystem as the ground under it (both come from the terrain bake's grass weight).
- City/outskirts = settlement Dust splat with soft edges, not a new `BiomeId`.

## Island / ocean framing

- **2 × 2** Unity Terrain tiles of 1400 m = a **2800 m** world. Geology is TerrainGen's: the Stage 1 island floor and coastline with the Stage 2 massif uplift on top, baked unchanged into Unity `TerrainData`. `IslandNaturalGeography` / `IslandOccupancy` are **deleted** — no framing pass sinks the border.
- **One** water plane = **WaterWorks** `WorldWater` at `SeaLevelMeters` (24 m). **Ocean only** — V0 has no inland lakes or rivers. No per-lake water objects. OCW demoted to fallback.
- Spec: [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md).

## Resolution audit (1400 m tile, heightmap 513)

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
| Contour “ladder” on mountains | Threshold on raw height only | Displace height with noise **before** paint Threshold; blur paint masks — relief unchanged |
| Hard grass / tint seams | Ecology/tint from hard `BiomeId` | `EvaluateSoft` / `TintSoft` blend neighbor Classify bands |
| Sub-pixel road shoulders | Lawn mask ~2 m/px | Lawn bake ~1536 on tile (~1 m/px) |
| Distant gray/white wash | `RenderSettings.fog` + heavy ColorAdjustments / SMH | Fog **off**; mild grade only |
| Hard rectangular painted region | `SettlementDustPaint` axis pads/streets with hard coverage | Soft rect falloff (~2.25 m) + mild noise |
| Full-tile rectangle overlay | `OverworldVegetationDebug` Quad | Cleared on Generate Surface |
| Road staircase | Soft rim 1.2 m << alphamap px (~2.7 m) | `SoftRimMeters` >= **2.5 alphamap texels**; lower TransitionCoverage |
| Huge gravel on flats | `highlandBandMask` height-only from GreenMax | Slope gate + higher gravel floor (paint only) |
| Hitch on chunk entry | A second CPU grass system baking chunks during rendering | Grass is a Unity terrain detail layer written once by the terrain bake |

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
- **Bake SoT:** the terrain bake assigns both the layer array and the alphamaps; only roads and
  settlements append after it.
- **Grass SoT:** the terrain bake's detail layer over the Polytope Lowpoly `PT_Grass_02` mesh
  (`OverworldVegetationAssets`), rendered by Unity's terrain detail pass. Not a second GPU grass system. Not
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
- Massif height and footprint are TerrainGen's (`MassifSettings`); the surface bake never reshapes them.

## Roads

Height corridor widths stay in `WorldScale` (Primary 4 m + shoulder 2.5 m, etc.).

| Zone | Width | Look |
|------|-------|------|
| Packed bed | `halfRoad` | Light dry center + darker edge + gravel speckles |
| Soft edge | `max(1.2 m, 2.5 alphamap texels)` (~6.8 m on _513) | TransitionCoverage ~0.4; BedCoverage ~0.9 |

Grass: **0 on packed bed**; lawn only in soft shoulder (`RoadLawnMinAtCenter = 0`). Pads/water stay clear.

Avoid stepped height LOD — keep terrain pixel error / smoothing as Unity Terrain already does.

## Grass

| Param | Range |
|-------|-------|
| Height | **0.3–1.5 m** (short/tall `PT_Grass_02`) |
| Density on green land | **0.3–1.0** (Fields high; Forest understory ~0.3–0.6) |
| Transition floor | **0.25–0.35** |
| Scatter / jitter | **0.3–0.6** |
| Types | short, tall, poppies (Meadow), rocks (highland) |

Placement comes from `TerrainVegetationRules`: instances per detail cell = peak count × vegetation gate ×
grass weight × patchiness, none on slopes past 35°, rendered by Unity's terrain detail pass. Size range
lives on the detail prototype built by `OverworldVegetationAssets`. The table above is the visual target;
tune it on the rule values, and record whether a value is measured or an authoring default.

## Water

| Depth | Color |
|-------|-------|
| Shallow / shore | Muted blue-green / teal, clearer |
| Deep | Dark blue-green |

**Stack:** WaterWorks `WorldWater` plane only — one sheet at `SeaLevelMeters`, ocean only. No inland water bodies and no per-lake meshes in V0. OCW demoted. Avoid plastic cyan.

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
- Terrain render = URP TerrainLit over the terrain bake's layer array; not MicroSplat, not `GameDev/Overworld/Terrain`
- Material transitions follow terrain morphology; no contour rings, no isolated colour patches
- Water = one WaterWorks `WorldWater` plane at Sea Level; the island shelf below sea reads as ocean; there are no inland water bodies in V0

## Verification

EditMode: SoftRim >=2.5 texels, `RoadLawnMinAtCenter==0`, island silhouette inscribed in the authored
biome area, highland slope gate, OCW water, dust soft rect.
Diagnostic: `OverworldTerrainValidation.RunBatch` — identical five-layer stack on every tile, per-texel weight sum ≈1.
Visual: Generate All → island-in-ocean, HDRI sky, soft shadows, warm fields, cooler forest, soft joins,
muted teal shallows, materials that follow the terrain rather than sit on it.

