# Overworld Visual Direction

**Status:** Draft  
**Date:** 2026-08-15  
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md), [NATURAL_ISLAND_GENERATOR.md](NATURAL_ISLAND_GENERATOR.md), `TerrainBiomePalette`, `RoadSurfaceStyle`, `GrassMask`, `OverworldMicroSplatSetup`, `CoastalBeachShelf`

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

## Terrain surfaces (V0 MicroSplat stack)

**SoT code:** `OverworldMicroSplatSetup.PaintUnifiedIslandSplats` → Convert → strip `_HYBRIDHEIGHTBLEND` → re-paint → `SyncAll`.  
**SoT geometry for coast:** `CoastalBeachShelf` (≤12° profile + coast-band height smooth).  
**Diagnostics:** `GameDev/Overworld/Debug/Diagnose Sand Pipeline` (+ PNG overlays under `Assets/Settings/Vista/SandDebug/`).

### Layer order (alphamap / MicroSplat control)

| Index | Layer asset | Role | When it wins |
|-------|-------------|------|----------------|
| 0 | `MS_Moss_Fields` (grass) | Dry plains / gentle slopes | Default inland; soft fringe with sand |
| 1 | `MS_Shore_Sand` (shore) | Beach + wet / underwater shelf | **Dry:** ASL band Sea+0…Sea+30 (full ≤Sea+2.5) + spatial noise. **Wet:** near land only. Soft fade into grass |
| 2 | `MS_Cliff_Rock` (cliff) | Sheer faces | Steep only (≳70°); **suppressed** in coastal band &lt;58° |
| 3 | `MS_Rock_Cliff` (rock) | Steep foothills / mountain slopes | Steep inland; **not** on beach transition terraces |

Do **not** invent parallel TerrainLayer stacks after Convert — channel remaps cause white/missing sand. UV tile ≈ **12 m**.

### Surface rules (operators)

1. **Dry sand = ASL height band** (Sea+0…Sea+30, full ≤Sea+2.5) with **±~7 m spatial noise** so the edge is not a contour ring. Lakes / low shelves get sand too. **Underwater sand** stays near-land only.
2. **Grass↔sand** = wide SmoothStep fade + multi-pass `FeatherShoreGrassBlend` + `SoftenAllLayerBlends` (all layers). No stair-step alphamap edge.
3. **No sawtooth rock ribbons** on the beach rise — those came from integer flood-ring terraces + slope→rock paint. Fix = coast-band **height smooth** + kill cliff/rock in sand ASL band under ~58°.
4. **Mountain rock/snow** may stay steep; do not flatten massifs for material joins ([rule 5](#how-to-read-this--the-reference-sheets)).
5. Never enable MicroSplat **`_HYBRIDHEIGHTBLEND`** on this bake — it crushed shore weights to invisible.
6. Plateau plains ≈ **55 m** ASL (~30–55 with remap); Outer falloff ≈ **420** local m. Do not raise island base to ~90 m “to look taller” — that recreates the mesa wall.

### Bake order (surfaces)

```text
Vista LPB heights (IslandBase Replace + Mountain Raise)
  → CoastalBeachShelf (≤12° + SmoothCoastBand)
  → PaintUnifiedIslandSplats (grass / sand / cliff / rock)
  → MicroSplat Convert (shared MicroSplatData, URP6.3)
  → Disable _HYBRIDHEIGHTBLEND + recompile
  → Re-paint + SyncAll
  → SandPipelineDiagnostics (optional)
  → GPU grass (PT_Grass_02) — mesh instances, not splat
```

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
- Grass tint midpoints match `TerrainBiomePalette` remaps (same ecosystem).
- City/outskirts = settlement Dust splat with soft edges, not a new `BiomeId`.

## Island / ocean framing

- Full **square** Unity Terrain (1400 m); Vista + `IslandNaturalGeography` build geology; slim `IslandOccupancy` sinks the **border** below **Sea Level**.
- **One** water plane = **WaterWorks** `WorldWater` at `SeaLevelMeters` (ocean + inland lakes as closed basins). No per-lake water objects. OCW demoted to fallback.
- **Snow line** = metres above sea (`WorldScale.SnowLineMeters`), not a raw 0–1 heightmap fraction.
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
| Hitch on chunk entry | Sync `GrassField.EnsureChunk` in beginCameraRendering | Time-sliced bake queue + `SampleBakedLawn01` |

## Performance budget

Prefer deriving look from: **Biome / Road / Height / Slope / Water / low-freq Noise**.

Avoid: volumetric fog, heavy fullscreen FX, 4× per-pixel splat noise, stacking more TerrainLayers for every cosmetic, 2k+ alphamaps “to hide stairs”.

Colors via `TerrainBiomePalette` remaps + **MicroSplat** textures (see below).

## Terrain shader (MicroSplat SoT)

- **Docs:** [MicroSplat Core](https://docs.google.com/document/d/1t_ZEKW8bHJqVWulH9Tcu-V5PIMr-sBOV_Ob7k2y1XY8/edit) + URP adapter package.
- **Packages:** `Packages/com.jbooth.microsplat.core`, `Packages/com.jbooth.microsplat.urp6p3`.
- **Bake SoT:** after Vista height — keep Vista TerrainLayers + alphamap weights → unify UV tile sizes → **Convert to MicroSplat** (shared `MicroSplatData`, URP6.3) → `SyncAll()`. Do **not** replace Vista layers with a parallel stack after Generate (remaps splat channels; caused white flats when a fake snow albedo was injected).
- **Grass SoT:** Polytope Lowpoly `PT_Grass_02` mesh via GPU `GrassField` + `GameDev/Overworld/Grass` (`GrassBakeMenu`). Coverage = full world extent (2×2). Not Vista DetailDensity. Not Vegetation Studio Pro (deprecated for this project).
- Custom `GameDev/Overworld/Terrain` fork is **deleted** — do not recreate or assign it.
- Albedo tile sizes stay **8-16 m** on TerrainLayers. Remaps stay on `TerrainLayer.diffuseRemap*`.

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

### Mountains / snow

- Snow starts at **65–75%** of the highland→peak band (`snowBandFraction ≈ 0.7`).
- Transition width feel **30–80 m** (height soft edge + 45–60 m noise).
- No snow on steep faces (slope ≳ **55°**).
- Base: grass + earth → rock → snow.
- Gravel/rock paint: higher floor + `foothillSlope` gate (Vista height unchanged by paint)
- No concentric rings — joins follow height+slope+noise, not geometric altitude circles.
- Absolute snow start ≈ `SeaLevelMeters + SnowLineMeters` (~127 m world Y at defaults).

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

Mask: biome + height + moisture + slope + noise. World tint map by biome. GPU instancing + LOD via `GrassField`; bake is **time-sliced** (`MaxBakesPerFrame=1`) with `SampleBakedLawn01` (no sync hitch on chunk entry).

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
- Terrain render = MicroSplat Convert + URP6.3; not `GameDev/Overworld/Terrain`
- Water = WaterWorks `WorldWater` at Sea Level; island shelf below sea reads as ocean; inland lakes = closed basins

## Verification

EditMode: palette warmth, SoftRim >=2.5 texels, `RoadLawnMinAtCenter==0`, grass bake queue, MicroSplat/TerrainLit layers, highland slope gate, OCW water, dust soft rect, snow above sea+SnowLine.
Visual: Generate All → island-in-ocean, HDRI sky, soft shadows, warm fields, cooler forest, soft joins, muted teal shallows, snow on gentle peaks only.

