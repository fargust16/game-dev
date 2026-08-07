# Playable OrganicTown — P0 Handoff Plan

> **Superseded / prototype (2026-08-07):** Continuous playable GridMap town is pre-pivot archive. Authority: [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md). Do not extend as default next work.

**Status:** Implemented (code + dress hooks; store assets optional) — **archive**  
**Date:** 2026-08-06  
**Authoritative order:** [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md) · [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md)  
**Handoff:** [`AGENT_HANDOFF_WORLD_GEN.md`](AGENT_HANDOFF_WORLD_GEN.md)

---

## Goal

Make OrganicTown v2 **walkable in Play Mode** inside a limited radius around the landmark/plaza: GridMap truth, player + tactical camera, socket NPCs, V1 environment actions, and visual dress hooks for complementary (non-architecture) assets.

## Locked decisions

- Architecture stays **procedural** — no city/building packs.
- Playable zone = **radius around landmark/plaza** (default 90 m).
- Movement = **GridMap + A\*** ([10.1](../../../10.1%20Grid%20World%20and%20Environment%20Actions.md)).
- Reuse `GridPlayerController`, `SimpleFollowCamera`, `SimpleInteractable`, `EnvironmentActionMarker`.
- Roadmap meaning: early thin **C2** grid bridge + **C1-lite** interact — not full C1 lore, not retire v1 city, not E.

## Slices

| Slice | Deliverable | Status |
|-------|-------------|--------|
| A0 | This plan + handoff/roadmap/Glossary | ✅ |
| A1 | `TownGridBaker` — roads + interiors → GridMap in radius | ✅ |
| A2 | Spawn player + camera; occlusion track | ✅ |
| A3 | NPC/prop interactables from sockets | ✅ |
| A4 | Env markers: window, vault, climb, door | ✅ |
| A5 | Material/prop/fog dress hooks + S0 scatter (store assets optional) | ✅ hooks |

Menus: `GameDev/Town/Generate From Seed`, `Bake Playable Grid`, `Enable URP Decal Feature`.  
Asset policy: [`PROCEDURAL_TOWN_ASSET_LIBRARY.md`](PROCEDURAL_TOWN_ASSET_LIBRARY.md) (library-first, not demo).  
Import cart: [`PLAYABLE_TOWN_ASSET_IMPORT.md`](PLAYABLE_TOWN_ASSET_IMPORT.md).

## Shopping list (library-first, verify prices)

Do **not** optimize for a fog postcard. Prefer materials + doors/windows/roofs + props.  
Cart v2: Brick V02, Cobble, Archviz materials, Doors/Windows pack, Industrial props, Floor decals. **Defer HAZE** until the library is wired.

## Verify

```text
Unity: GameDev/Town/Generate From Seed → Play
- Walk streets in ~90 m radius
- Enter templated building (D2 walls hide)
- E on Keeper / env marker
```

```bash
dotnet run --project tools/TownDebugHost -- 110201
```
