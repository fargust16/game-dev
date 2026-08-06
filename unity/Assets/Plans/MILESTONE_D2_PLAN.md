# Milestone D2 — BG3 Building Occlusion

**Status:** In progress (D2.1–D2.2)  
**Date started:** 2026-08-06  
**Depends on:** D1 ✅ (enterable templates + sockets)  
**Authoritative order:** [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md)

---

## Goal

While the tracked subject (player avatar, or camera focus when exploring the town scene) is **inside a building volume**, fade/hide that building’s exterior walls (and solid shells) so the interior stays readable — BG3 / Fallout tactical feel, not feature parity.

Locked constraints:

- Same continuous world (no interior scenes)
- No GridMap bridge (**C2**)
- No narrative spawn (**C1**)
- Character/focus stays readable; floors and socket markers stay visible

---

## Micro-slices

| Slice | Deliverable | Done when |
|-------|-------------|-----------|
| **D2.1** | `BuildingVolume` containment + baker `BuildingVisualRoot` tags (Wall / Floor / Shell) | EditMode tests for Contains; bake roots tagged |
| **D2.2** | `BuildingOcclusionController` HideWalls mode + wire after bake / camera focus | Enter footprint → walls hide; leave → restore |
| **D2.3** | Optional material fade path / roof mesh when roofs exist | Deferred until art has roofs; API reserved via `OcclusionMode` |

---

## Architecture (growth-friendly)

```text
TownModel.Buildings[i] ──► BuildingVolume.FromBuilding (footprint + height)
                              │
TownGreyboxBaker ──► BuildingVisualRoot (renderers by category)
                              │
BuildingOcclusionController ──► track pos → FindContaining → Apply/Restore
         ▲
SimpleFollowCamera focusTarget / focusPoint (fallback)
```

| Type | Role |
|------|------|
| `BuildingVolume` | Pure geometry: polygon + minY/maxY; `Contains(worldPos)` |
| `BuildingVisualCategory` | Wall, Floor, Shell, Other (append-only) |
| `BuildingVisualRoot` | MonoBehaviour on bake root; building index + renderer lists |
| `BuildingOcclusionMode` | `HideWalls` (V1), `FadeMaterials` (reserved) |
| `BuildingOcclusionController` | Runtime driver; refreshes after bake |

### Tracking priority

1. Explicit `trackTarget` override  
2. `SimpleFollowCamera` focus target (avatar)  
3. Camera focus point (town exploration without player)

### Explicitly out of D2.1–D2.2

- Transparent URP material authoring pack
- Occluding *neighbor* buildings between camera and subject (camera ray cutaway)
- C1 NPC spawn, C2 GridMap

---

## Verify

1. Unity: `GameDev/Town/Generate From Seed`  
2. Place/focus inside a templated building → wall cells disappear; floors remain  
3. Move outside → walls return  
4. EditMode: `BuildingVolumeTests`
