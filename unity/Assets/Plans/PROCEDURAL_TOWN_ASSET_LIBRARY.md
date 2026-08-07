# Procedural Town Asset Library Policy

> **Still useful post-pivot:** materials/modules for overworld exterior shells and interior scenes. Continuous OrganicTown wiring is archive — see [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md).

**Status:** Draft  
**Date:** 2026-08-06  
**Applies to:** Exterior shells / interior scene dress (was OrganicTown v2)  
**Related:** [`PLAYABLE_TOWN_ASSET_IMPORT.md`](PLAYABLE_TOWN_ASSET_IMPORT.md), [`PLAYABLE_TOWN_P0_PLAN.md`](PLAYABLE_TOWN_P0_PLAN.md)

---

## Goal

Build a **long-term reusable procedural city asset library**.

Do **not** optimize for a beautiful one-off demo scene.

Every purchased or authored asset must maximize reuse across **thousands of procedurally generated buildings**. Prefer reusable building blocks over complete scenes. Assets **increase visual variety of procedural generation**; they must never replace OrganicTown / `BuildingTemplate` authority.

---

## Priority (buy / author in this order)

| Rank | Category | Role |
|------|----------|------|
| ★★★★★ | **Materials** | Tileable PBR for walls, floors, roads, plaster, wood, metal, roofs — highest reuse |
| ★★★★★ | **Architectural modules** | Grid-friendly wall trims, cornices, chimneys, ledges — snap onto procedural shells |
| ★★★★★ | **Doors** | Swappable door meshes for template `Door` cells / openings |
| ★★★★★ | **Windows** | Swappable window frames for wall openings / env climb stubs |
| ★★★★★ | **Roof elements** | Tileable roof materials + modular ridge/hip/chimney pieces for later roof bake |
| ★★★★★ | **Props** | Barrels, crates, pipes, lamps usable in many districts / sockets |
| ★★★★ | **Decals** | Dirt, leaks, cracks — age variety without new meshes |
| ★★★★ | **Vegetation** | Ivy / sparse street trees — seam hiding, not landscaping demos |
| ★★★ | **FX** | Fog, rain, steam — mood only; lowest procedural reuse |

---

## Acceptance filter (every recommendation)

Ask for each asset:

1. Can this dress **≥100** different generated buildings without looking like the same kitbash scene?
2. Is it a **block** (material / module / prop) rather than a **finished building or town**?
3. Does it **plug into** our templates/shells, or does it tempt us to drop in prefab houses?
4. Will it still be useful after Vertical Slice — in other districts / wealth classes / decades?

If any answer is No → do not buy for the core library.

---

## Forbidden / avoid

- Complete Victorian / steampunk **city kits** and demo towns (Harboria-style, Steam City as architecture source, OWA “450 buildings”, etc.)
- Competing **building generators** that replace OrganicTown (`Building Architect`, home designer suites as authority)
- Monolithic environment demos where the value is the sample scene
- Stylized character packs for P0 (capsule → later Mixamo)
- One-off hero props that only work in a single postcard shot
- HDRP-only packs when we are URP

**Allowed exception:** a “modular town” pack may be mined **only** for loose doors/windows/roof pieces/materials — never for dropping whole houses into the gen pipeline. Prefer packs that ship those pieces as first-class folders.

---

## Folder layout (import target)

```text
Assets/Art/TownDress/
  Materials/          ★★★★★ brick, cobble, plaster, wood, slate, metal
  Modules/
    Doors/            ★★★★★
    Windows/          ★★★★★
    Roofs/            ★★★★★ ridges, hips, chimneys, eaves
    Trim/             ★★★★★ cornices, ledges, sills
  Props/              ★★★★★ lamps, barrels, crates, pipes
  Decals/             ★★★★
  Vegetation/         ★★★★
  FX/                 ★★★   (defer)
  Imported/           raw Asset Store drops before sorting
```

Wire materials into `TownGenerator` slots; modules later bind to template openings (D1 cells / sockets), not to scene-authored streets.

---

## Library cart v2 (under ~$100, verify prices)

Optimized for **reuse**, not fog postcard. Snapshot 2026-08-06.

| # | Asset | Est. USD | Rank | Why it earns a slot |
|---|-------|----------|------|---------------------|
| 1 | Brick Materials V02 ([310944](https://assetstore.unity.com/packages/2d/textures-materials/brick/brick-materials-v02-310944)) | 9.99 | ★★★★★ Materials | Tile brick across all shell walls |
| 2 | Cobblestone PBR Material ([132837](https://assetstore.unity.com/packages/2d/textures-materials/floors/cobblestone-pbr-material-132837)) | 4.99 | ★★★★★ Materials | Street ribbons forever |
| 3 | 30 - 4K PBR Archviz Materials ([173982](https://assetstore.unity.com/packages/2d/textures-materials/30-4k-pbr-archviz-materials-173982)) | check | ★★★★★ Materials | Brick/plaster/wood/**roof** variety in one tileable set |
| 4 | Medieval Doors and Windows M Pack ([310547](https://assetstore.unity.com/packages/3d/props/medieval-doors-and-windows-m-pack-310547)) | check | ★★★★★ Doors/Windows | 11 doors + large window matrix — place on openings; re-skin materials for industrial-occult |
| 5 | Industrial Props Pack ([306565](https://assetstore.unity.com/packages/3d/props/industrial/industrial-props-pack-hdrp-urp-srp-306565)) | 15 | ★★★★★ Props | Pipes/cables/racks across industrial + alley buildings |
| 6 | Floor Detail Decals Vol.3 ([396524](https://marketplace.unity.com/packages/2d/textures-materials/floor-detail-decals-pack-vol-3-30-pbr-decals-for-unity-hdrp-urp-396524)) | 7.99 | ★★★★ Decals | Dirt/cracks without new meshes |
| | **Target total** | **≈ $50–90** | | Leave buffer for a roof-trim or plaster pack if Archviz price is low |

### Swaps if on sale / budget moves

- Cobble $4.99 → Realistic Materials Cobblestone 1 ($15) if you need more road variety.
- Add **Modular Industrial Pipes** ([295003](https://marketplace.unity.com/packages/3d/props/modular-industrial-pipes-pack-295003)) before any fog pack — pipes compose; fog does not.
- Magnesia Stone / plaster pack (~$10) if Archviz lacks enough wall variety.

### Explicitly deferred (demo-leaning)

| Asset | Why deferred |
|-------|----------------|
| HAZE Volumetric Fog ($35) | ★★★ FX — strong mood, **weak library reuse**; buy after materials/modules |
| Enviro 3 | Full weather suite — Phase 2+ |
| Warehouse & Factory Essential | Useful barrels, but warehouse-scene bias; prefer props that read in many districts |
| Victorian street-light mega packs ($60) | Buy only a **small lamp subset** later; not a P0 library blocker |
| Any complete city / 450-building kit | Replaces procedural authority |

### Free / no-cost fillers

- ambientCG (CC0) wood, slate, iron/brass sheets — verify license, import into `Materials/`
- Existing S0 greybox prefabs as module placeholders until doors/windows pack lands

---

## Phase 2 library (next budget)

1. Roof geometry modules (ridge/hip/chimney) if materials-only roofs are not enough  
2. Trim / cornice / sill set (1 m grid)  
3. Ivy / dead-vegetation for seam hiding  
4. Then FX (HAZE)  
5. Mixamo humanoid wardrobe — not stylized Victorian citizen packs  

---

## Wiring note

P0 code already applies `roadMaterial` / `wallMaterial` / `floorMaterial` (+ optional plaster/roof/wood slots) and S0 scatter. Module placement on template openings is a **later bake step** — do not stall Playable P0 on buying FX.
