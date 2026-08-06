# 12 Engine Choice

Version: 1.1

Status: Draft

Last Updated: 2026-08-06

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [13 City Generation](13%20City%20Generation.md)

Referenced By:
Prototype repository setup and implementation milestones.

---

# Purpose

This document records the implementation engine for the Vertical Slice under the production constraint:

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
| Architecture | Custom square grid + Turn Action Economy + Environment Actions |

Godot 4 was considered and remains historically documented below as the rejected alternative for this project phase.

# Why Unity Was Chosen

1. Human familiarity: the project creator can open the editor and help when AI gets stuck.
2. Sufficient AI/code ecosystem for C# 3D prototypes.
3. Acceptable for a custom grid TB game if marketplace combat kits are forbidden as architecture.

# Binding Implementation Constraints

1. Custom grid + Turn Action Economy owned by project code in `unity/`.
2. Environment Actions from [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md) before fancy character controllers.
3. Prefer URP and minimal packages.
4. Follow milestones in order: S0 grid greybox before POI/combat logic.
5. No replacing Decision Engine / Approaches with a marketplace tactics pack.
6. Keep package bloat low; every package must earn its place.
7. Design docs remain source of truth over random Unity tutorial patterns.
8. **Input:** Unity Input System, **Package Only** (`activeInputHandler: 1`). Gameplay reads `GameInputService` — not legacy `UnityEngine.Input`.
9. **City scaffold:** seed-driven districts / avenue / anchors per [13 City Generation](13%20City%20Generation.md); grid remains movement truth.

# Comparison Archive

## Unity — strengths for this project

- human can rescue editor issues;
- large AI training corpus;
- mature 3D/input/UI ecosystem.

## Unity — risks to watch

- heavy tooling;
- AI suggesting outdated APIs;
- temptation to import mismatched Asset Store combat kits.

## Godot 4 — why not chosen now

- better git/text-diff lightness and lighter install;
- but lower human familiarity here, which matters because the human is the safety net.

# Next Implementation Step

Create/maintain the Unity project under `unity/` and execute [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md) on a square grid.

# Documentation Review

- Engine lock confirmed by project creator.
- Does not change game fantasy; selects delivery tool only.
- S0 remains first build milestone.

# Related Documents

- [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- [13 City Generation](13%20City%20Generation.md)
