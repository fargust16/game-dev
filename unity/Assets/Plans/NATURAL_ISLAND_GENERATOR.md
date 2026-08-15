# Natural Island Generator

**Status:** Draft (quality SoT superseded — see Vista reference)  
**Date:** 2026-08-15  
**Applies to:** Track W.2 Terrain / Biome, hydrology visuals  
**Related:** [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) (**golden scene**), [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [ASSET_LIBRARY_REFERENCE.md](ASSET_LIBRARY_REFERENCE.md), [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md)

---

## Quality SoT (2026-08-15)

Automated multi-LPB bake is the working path:

→ **[VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md)** § Recommended V1 stack  
`LPB_IslandBase` (Replace) + `LPB_Mountain_*` (Raise) on 2×2×1400 + **height 700**.

Hand-authored single MountainBiome covering the whole grid remains a useful visual reference for erosion/texturing, but **modular LPBs are the architecture** — do not merge mountains back into IslandBase.

---

## Goal

Replace the radial “pancake” island (`IslandBaseRelief` × elliptical `IslandOccupancy` + per-lake OCW planes) with a geological heightfield around a fixed **Sea Level**, and **one** horizontal water plane that is both ocean and inland lakes.

Quality bar: terrain continues under water; lakes are closed depressions intersecting Sea Level; no radial mound; no per-lake water GameObjects. **Vertical relief must match the golden Vista Mountain bake**, not a 220 m flattened remap.

---

## Core model

| Concept | Rule |
|---------|------|
| `SeaLevel` | `WorldScale.SeaLevelMeters` (32). Single water Y. |
| Heightfield | **Vista Mountain biome graph** (scene golden) — not stripped NaturalIsland graph |
| Vertical budget | **500 m** locked — `WorldScale.TerrainHeightBudgetMeters` = VistaManager / tile `size.y` |
| Island mask | **Vista LPB Anchors + Outer Falloff** (biome mask). Not C# `IslandNaturalGeography`. |
| Mountains | Vista Mountain template SoT |
| Grid | `2×2` tiles × 1400 m = **2800 m** world extent |
| Vista biomes | Golden: one Mountain LPB, `Space.Local`, template pos/scale — silhouette via anchors/falloff; see [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) |
| Lakes | Floor `< SeaLevel`, rim `≥ SeaLevel + LakeSafetyMarginMeters`; flood-fill must not reach world border |
| Water visual | One `WorldWater` (WaterWorks plane + `Assets/Settings/Water/OverworldWater.mat` teal clone) |

---

## Pipeline (target alignment)

```text
WorldSeed forks
  → VistaManager (terrainMaxHeight = 700) + Unity Terrain grid 2×2 (1400 m, size.y = 700)
  → LPB_IslandBase: IslandPlateau_TerrainGraph + harmonic asymmetric anchors + SoftFalloff mask,
      plateau worldHeight ~55 m (ASL ~30–55, not a 90 m mesa), Replace + LayerWeightMaps
  → LPB_Mountain_i (1–3): elongated ellipse anchors + wide Outer falloff, Raise delta ~480–580 m
  → (later) LPB_Lake / River / Forest / Meadow / Swamp / Field / City (order bands 20–49)
  → Vista GenerateAll → remap cache onto tiles
  → SyncFromGpu + ForceTileSize (1400, 700, 1400)
  → CoastalBeachShelf directed ≤12° profile + SmoothCoastBand (no terrace / no flood-ring ladder)
  → C# coast splat (wet/dry/underwater sand, soft fringe) → MicroSplat Convert **without** HybridHeightBlend
  → WorldWater at SeaLevel (OverworldWater.mat)
  → GrassField (Polytope PT_Grass_02, full 2800×2800)
```

### Continuity rules

1. **Island base = plateau graph** — never empty Blank graph; Outer falloff + SoftFalloff mask; rolling remap (tens of metres), not a constant tabletop.
2. **Mountains = 1–3 Local Biomes** — ridge/ellipse anchors + wide falloff; **Raise** height delta (not KeepHigher dome stamp).
3. **Island framing = harmonic radius anchors + Outer falloff** — soft drowned rim, not a round pancake.
4. **Coast height = directed ≤12° profile + SmoothCoastBand** — softens low/mid shelves; cliff-azimuth high walls may stay steep. Sand material must remain visible underwater near shore; shore↔grass uses multi-pass feather; no rock ribbons on coastal slopes &lt;58°.
5. Never Blank ±500 × `localScale = world extent` (OOM). Template scale ~1.05 with local anchors is fine.
6. Terrain grid stays 2×2 for streaming/LOD; unique tile names for MicroSplat.
7. Full automated contract: [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) § Automated bake contract.
8. Terrain shading SoT: C# splat (`MS_Moss_Fields` / `MS_Shore_Sand` / `MS_Cliff_Rock` / `MS_Rock_Cliff`) → Convert + URP adapter; disable `_HYBRIDHEIGHTBLEND`. Surface operator guide: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) § Terrain surfaces.
9. Secondary LPBs: menu `GameDev/Overworld/Vista/Spawn Secondary LPB Scaffold` — order bands Hydro 20–29 / Eco 30–39 / Civ 40–49.
10. **Island Outer falloff ≈ 420 local m**; plateau worldHeight ≈ **55 m** (ASL ~30–55). Short falloff or ~90 m plateau recreates the mesa “cake” rim.

### Seed forks

| Fork | Role |
|------|------|
| `terrain.islandbase` | **`LocalProceduralBiome.seed` only** (Args.SEED). Leave Mountain graph node seeds. |
| `terrain.island` / `lake.place` | Future framing / lakes after height SoT is locked |

No `UnityEngine.Random` — only `WorldSeed.Fork` → `System.Random`.

---

## Vista (Personal 3.2.1)

**Working graph:** Mountain biome template clone (`Overworld_Generator Biomes/Mountain_TerrainGraph 1.asset`).

**Deprecated for quality:** `NaturalIsland_TerrainGraph` (Noise×3 + Ridge + Landslide≤0.5 + Hydraulic×1) — kept only as historical experiment.

Menu: `GameDev/Overworld/Vista/Rebuild Natural Island Graph` — do not prefer over Mountain template until redesign.

**Diagnostic:** `Bake Demo MountainRange As-Is` — secondary; golden MountainBiome scene is primary.

**Safe notes:**

- Golden Mountain template runs dual `HydraulicErosionNode2` at 2800 m footprint successfully.
- Still avoid Blank ±500 × scale=extent.
- Always pin tile size after heightmap resolution changes.

## Terrain tile size pitfall

Unity `TerrainData` keeps **metres-per-pixel** when `heightmapResolution` changes.
Setting size to 1400 m at default res 33, then raising res to 513 yields:

`size = (1400/32) × 512 = 22400 m`

Always set resolution first, then pin `size = (OverworldTileMeters, heightBudget, OverworldTileMeters)` via `TerrainHeightWrite.ForceTileResolutionAndSize` (height budget = Manager max height, **700**).

---

## Water visuals

| Asset | Role |
|-------|------|
| **WaterWorks** (`Assets/WaterWorks/Water_Plane.prefab`, `SSR_Water`) | **SoT** — single `WorldWater` at Sea Level |
| **One Click Add Water** | Demoted — unused stylized fallback; do not spawn per-lake OCW planes |
| `Water_Volume` renderer feature | Stay stubbed (Unity 6) — do not enqueue |

Rivers: existing `RiverMeshBuilder` / `RiverTerrainCarver` with `SSR_Water`. Never carve bed ≤ Sea Level except inside the coastal mouth band (`RiverMouthSeaBandMeters`). Skip paths that would drain a closed basin to the ocean.

---

## Validation

After generate (height SoT):

- Relief reads as Mountain demo (not pancake); peaks use full vertical budget
- 2×2 seams continuous under one LPB
- `size=(1400,700,1400)` (or agreed budget), not 22400

Island / lake validation (after framing redesign):

- Border ocean / closed lakes as designed
- Exactly one `WorldWater`

Debug: `IslandGeographyDebug` / menu PNGs when framing returns.

---

## Deferred

- Alpine tarns (extra water planes above Sea Level)
- Waterfall meshes (WaterWorks has no waterfall API)
- Multi-tile continents
- Procedural anchor rings with stronger coast noise (V0 already uses seeded 16-gon)
- Custom grass (replace Vista detail)
- Closed inland lakes that do not fight the Mountain+anchors height SoT
