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

Replace the radial “pancake” island with a geological heightfield around a fixed **Sea Level**, and **one** horizontal **ocean** water plane.

Quality bar: terrain continues under water; no radial mound; **no inland lake/river generation in V0** (removed 2026-08-15). **Vertical relief must match the golden Vista Mountain bake**, not a flattened remap — and it must reach that quality inside a 512 px biome canvas.

---

## Core model

| Concept | Rule |
|---------|------|
| `SeaLevel` | `WorldScale.SeaLevelMeters` = **24**. Single water Y. |
| Heightfield | `IslandPlateau_TerrainGraph` + `MountainMassif_TerrainGraph`, both built on the Vista Mountain template's **architecture** — not on its numbers, which are authored for a 1024 px canvas |
| Vertical budget | **700 m** locked — `WorldScale.TerrainHeightBudgetMeters` = VistaManager / tile `size.y` |
| Island mask | **Vista LPB Anchors + Outer Falloff** (biome mask). Not C# `IslandNaturalGeography`, which is deleted. |
| Mountains | 1–2 `LPB_Mountain_n` with `HeightBlendMode.Raise` |
| Grid | `2×2` tiles × 1400 m = **2800 m** world extent — this is the playable V0 world |
| Biome canvas | **512 px, Vista Personal hard cap** — the resolution ceiling on island shape |
| Inland lakes / rivers | **Removed from V0 code** — a future redesign from first principles, not a restore |
| Water visual | One `WorldWater` (WaterWorks plane + `Assets/Settings/Water/OverworldWater.mat` teal clone) |

---

## Pipeline (target alignment)

```text
WorldSeed forks
  → VistaManager (terrainMaxHeight = 700) + Unity Terrain grid 2×2 (1400 m, size.y = 700)
  → LPB_SeaFloor: SeaFloor_TerrainGraph + SnapBiomeToTiles, worldHeight 20 m, Replace — the world floor
  → LPB_IslandBase: IslandPlateau_TerrainGraph + 36 harmonic asymmetric anchors + SoftFalloff mask,
      worldHeight 140 m at biome Y = 24 (sea level), Replace + LayerWeightMaps
  → LPB_Mountain_i (1–2): 28 elongated ellipse anchors + wide Outer falloff (520), Raise delta 530 m
  → Vista GenerateAll → remap cache onto tiles
      (height + 5 layer weights + tree instances + detail density, all from the graphs)
  → SyncFromGpu + ForceTileSize (1400, 700, 1400)
  → TerrainTile.MatchSeams (Vista native)
  → WorldWater at SeaLevel (OverworldWater.mat) — ocean only
```

**No post-Vista terrain pass.** `CoastalBeachShelf`, the C# height-band coast splat, MicroSplat
Convert, `IslandOccupancy` and `GrassField` are all deleted. Roads and settlements are the one
sanctioned downstream writer, and they append their own layers without touching biome indices 0–4.

### Continuity rules

1. **Island base = plateau graph** — never empty Blank graph; Outer falloff + SoftFalloff mask; rolling remap (tens of metres), not a constant tabletop.
2. **Mountains = 1–3 Local Biomes** — ridge/ellipse anchors + wide falloff; **Raise** height delta (not KeepHigher dome stamp).
3. **Island framing = harmonic radius anchors + Outer falloff** — soft drowned rim, not a round pancake.
4. **Coast height = directed ≤12° profile + SmoothCoastBand** — softens low/mid shelves; cliff-azimuth high walls may stay steep. Sand material must remain visible underwater near shore; shore↔grass uses multi-pass feather; no rock ribbons on coastal slopes &lt;58°.
5. Never Blank ±500 × `localScale = world extent` (OOM). Template scale ~1.05 with local anchors is fine.
6. Terrain grid stays 2×2 for streaming/LOD; unique tile names for MicroSplat.
7. Full automated contract: [VISTA_OVERWORLD_REFERENCE.md](VISTA_OVERWORLD_REFERENCE.md) § Automated bake contract.
8. Terrain shading SoT: C# splat (`MS_Moss_Fields` / `MS_Shore_Sand` / `MS_Cliff_Rock` / `MS_Rock_Cliff`) → Convert + URP adapter; disable `_HYBRIDHEIGHTBLEND`. Surface operator guide: [OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md) § Terrain surfaces.
9. Secondary LPBs: menu `GameDev/Overworld/Vista/Spawn Secondary LPB Scaffold` — Eco 30–39 / Civ 40–49 (no hydro placeholders).
10. **Island Outer falloff ≈ 420 local m**; plateau worldHeight ≈ **55 m** (ASL ~30–55). Short falloff or ~90 m plateau recreates the mesa “cake” rim.

### Seed forks

| Fork | Role |
|------|------|
| `terrain.islandbase` | **`LocalProceduralBiome.seed` only** (Args.SEED). Leave Mountain graph node seeds. |
| `terrain.island` | Framing helpers when needed |

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

Inland rivers/lakes: **not generated**. Ocean plane only.

---

## Validation

After generate (height SoT):

- Relief reads as Mountain demo (not pancake); peaks use full vertical budget
- 2×2 seams continuous under one LPB
- `size=(1400,700,1400)` (or agreed budget), not 22400

Validation:

- Border ocean as designed
- Exactly one `WorldWater`

Debug: `IslandGeographyDebug` / menu PNGs when framing returns.

---

## Deferred

- Alpine tarns (extra water planes above Sea Level)
- Waterfall meshes (WaterWorks has no waterfall API)
- Multi-tile continents
- Procedural anchor rings with stronger coast noise (V0 already uses seeded 16-gon)
- Custom grass (replace Vista detail)
- Inland lake/river redesign (not a restore of removed MapMagic-era placers)
