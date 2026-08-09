# Town Dress — Procedural Asset Library

**Not a demo kit.** Goal: reusable blocks for thousands of OrganicTown buildings.

Full policy: [`../../Plans/PROCEDURAL_TOWN_ASSET_LIBRARY.md`](../../Plans/PROCEDURAL_TOWN_ASSET_LIBRARY.md)

## Priority

1. ★★★★★ Materials  
2. ★★★★★ Architectural modules / Doors / Windows / Roofs / Props  
3. ★★★★ Decals / Vegetation  
4. ★★★ FX (last)

## Folders

| Folder | Contents |
|--------|----------|
| `Materials/` | Tileable brick, cobble, plaster, wood, slate, metal |
| `Modules/Doors/` | Door meshes for template openings |
| `Modules/Windows/` | Window frames for wall openings |
| `Modules/Roofs/` | Ridge / hip / chimney / eave pieces |
| `Modules/Trim/` | Cornices, sills, ledges |
| `Props/` | Lamps, barrels, crates, pipes |
| `Decals/` | Dirt, leaks, cracks |
| `Vegetation/` | Ivy / sparse trees |
| `FX/` | Fog/rain — defer |
| `Imported/` | Untouched Asset Store drops |

## Cart v2 (see import checklist)

Brick V02 · Cobble · Archviz materials · Doors/Windows pack · Industrial props · Floor decals.

**Do not** import Victorian city / building packs as architecture.

## After purchase

1. Sort out of `Imported/` into the folders above.  
2. Assign materials on `TownGenerator`.  
3. `GameDev/Town/Enable URP Decal Feature`.  
4. Wire door/window prefabs into future opening bake (not whole houses).  

P0 without store assets: tint fallbacks + S0 scatter + Unity fog.
