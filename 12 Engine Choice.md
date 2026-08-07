# 12 Engine Choice

Version: 1.2

Status: Draft

Last Updated: 2026-08-07

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
Prototype repository setup and implementation milestones.

---

# Purpose

This document records the implementation engine under the production constraint:

> AI agents do most of the implementation work from Canonical/Draft docs. The human helps, reviews, and unblocks.

# Decision

**Chosen engine: Unity**

| Field | Value |
|-------|-------|
| Engine | Unity |
| Installed editor observed for this machine | `6000.3.9f1` (Unity 6) |
| Render pipeline | URP |
| Language | C# |
| Project folder | `unity/` |
| Architecture | Free third-person overworld + interior scenes; custom square-grid Turn Action Economy **on Combat Arenas only** |

Godot 4 was considered and remains historically documented below as the rejected alternative for this project phase.

# Why Unity Was Chosen

1. Human familiarity: the project creator can open the editor and help when AI gets stuck.
2. Sufficient AI/code ecosystem for C# 3D prototypes.
3. Fits free 3P locomotion plus a small custom arena grid without requiring a marketplace tactics kit as architecture.

# Binding Implementation Constraints

1. **Overworld / interiors:** free third-person controller + follow/orbit camera ([16 Architecture Pivot](16%20Architecture%20Pivot.md)). Genshin-like feel target; not feature parity.
2. **Combat Arenas:** custom grid + Turn Action Economy owned by project code in `unity/` ([10](10%20Turn%20Action%20Economy.md), [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).
3. NavMesh (or equivalent) **may** be used later for overworld/interior NPC locomotion. It is **not** Confront movement truth — arenas use the project grid.
4. Prefer URP and minimal packages.
5. Follow post-pivot AI backlog in [16](16%20Architecture%20Pivot.md): Overworld V0 → Locomotion V0 → Interior V0 → Arena V0 → Theme Packs V0.
6. No replacing Decision Engine / Approaches with a marketplace tactics pack.
7. Keep package bloat low; every package must earn its place.
8. Design docs remain source of truth over random Unity tutorial patterns.
9. **Input:** Unity Input System, **Package Only** (`activeInputHandler: 1`). Gameplay reads a project input service — not legacy `UnityEngine.Input`.
10. **Pre-pivot OrganicTown / CityGenerator code** under `unity/` is prototype archive. Do not extend the continuous enterable-town + world GridMap path by default.

# Comparison Archive

## Unity — strengths for this project

- human can rescue editor issues;
- large AI training corpus;
- mature 3D/input/UI ecosystem.

## Unity — risks to watch

- heavy tooling;
- AI suggesting outdated APIs;
- temptation to import mismatched Asset Store combat kits or whole-city prefab packs.

## Godot 4 — why not chosen now

- better git/text-diff lightness and lighter install;
- but lower human familiarity here, which matters because the human is the safety net.

# Next Implementation Step

Do **not** continue OrganicTown full C1 as default.  
Next code track after this documentation pivot: micro-plan **Overworld V0** per [16 Architecture Pivot](16%20Architecture%20Pivot.md).

# Documentation Review

- Engine lock confirmed by project creator.
- Architecture updated for Architecture Pivot (free 3P + arena grid).
- Does not change game fantasy; selects delivery tool and presentation split.

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)
