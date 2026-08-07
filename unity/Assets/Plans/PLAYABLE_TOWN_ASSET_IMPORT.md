# P0 Asset Import Checklist (Library-first)

> **Prototype-era checklist.** Library policy still applies for shells/interiors; P0 GridMap town path is superseded by [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md).

**Policy (read first):** [`PROCEDURAL_TOWN_ASSET_LIBRARY.md`](PROCEDURAL_TOWN_ASSET_LIBRARY.md)

Do **not** buy for a pretty demo. Buy for a **reusable procedural library** (materials → doors/windows/roofs → props → decals → vegetation → FX last).

Prices/dates — snapshot 2026-08-06; re-check before buy.

---

## Cart v2 (library, ~$50–90)

| # | Asset | Est. USD | Slot |
|---|-------|----------|------|
| 1 | Brick Materials V02 | 9.99 | `Materials/` → `wallMaterial` |
| 2 | Cobblestone PBR Material | 4.99 | `Materials/` → `roadMaterial` |
| 3 | 30 - 4K PBR Archviz Materials | check | `Materials/` plaster/wood/**roof**/floor variety |
| 4 | Medieval Doors and Windows M Pack | check | `Modules/Doors` + `Modules/Windows` |
| 5 | Industrial Props Pack | 15 | `Props/` pipes/cables |
| 6 | Floor Detail Decals Vol.3 | 7.99 | `Decals/` |

**Deferred:** HAZE / Enviro (FX). Warehouse-only kits. Complete city packs.

**Forbidden:** city/building architecture packs as generation source.

---

## Import steps

1. Import into `Assets/Art/TownDress/Imported/`, then **sort** into Materials / Modules / Props / Decals (see library doc).
2. Assign tileable materials on `TownGenerator` (`road` / `wall` / `floor` / `plaster` / `roof` / `wood`).
3. Menu: `GameDev/Town/Enable URP Decal Feature` (once) before placing decals.
4. Keep door/window meshes as library prefabs — do not drop whole sample houses into the town.
5. FX (HAZE) only after materials + modules are wired.

Until packs arrive: tint fallbacks + S0 scatter remain valid.

---

## Code hooks (no store dependency)

- `TownPlayableBootstrap.ApplyMaterialDress` — material slots + tint fallbacks  
- `TownPlayableBootstrap.ScatterProps` — S0 placeholders until `Props/` filled  
- `TownUrpDressSetup` — Decal feature menu  

Folder notes: `unity/Assets/Art/TownDress/README.md`
