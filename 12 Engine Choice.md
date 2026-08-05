# 12 Engine Choice

Version: 0.1

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)

Referenced By:
Prototype repository setup and implementation milestones.

---

# Purpose

This document compares engines for implementing the Vertical Slice under the real production constraint of this project:

> AI agents do most of the implementation work from Canonical/Draft docs. The human helps, reviews, and unblocks.

Candidate engines: **Unity** and **Godot 4**.

# Project Constraints That Matter

- solo / AI-assisted workflow;
- documentation-first;
- grid turn-based world from S0;
- in-location TB combat + non-combat TB;
- 3D industrial-era district slice;
- long-term maintainability over flashy demos.

# Comparison

## Unity

### Pros

- Human familiarity: you already know pieces of the editor, so you can help when AI gets stuck.
- Huge training corpus: AI tools often produce Unity C# examples quickly.
- Mature ecosystem for 3D, input, UI toolkits, packaging, and third-party grid/TB assets.
- Easy to find human tutorials if a blocker is visual/editor-specific.
- Fine for a grid TB architecture if we own our grid (do not depend on random Asset Store combat kits that fight the design docs).

### Cons

- Heavier install/tooling (Hub, editor versions, packages).
- Scene/prefab workflows can be less transparent in git than Godot text scenes.
- API churn across versions; AI sometimes suggests outdated Unity patterns.
- License/account friction is usually fine for hobby, but still more corporate surface than Godot.
- AI-generated Unity projects can accumulate package bloat if not constrained.

### Best when

The human will regularly open the editor, click scene objects, and rescue AI from Unity-specific confusion.

## Godot 4

### Pros

- Lightweight, open source, simple project model.
- Many core files are text (`.tscn`, `.gd`), which is often easier for AI agents and git diffs.
- Fast iteration loop; good fit for greybox → systems.
- No Hub/account required to start.
- Godot 4 3D is good enough for a Vertical Slice district.
- Clear ownership of a custom grid TB system (less temptation to import a mismatched kit).

### Cons

- You are less familiar, so human rescue may be slower at first.
- Smaller Asset Store equivalent; more custom work for polish.
- AI sometimes still defaults to Unity mental models and must be corrected.
- Fewer “BG3-like” ready-made examples than Unity’s sea of tutorials.
- C# in Godot is viable but ecosystem gravity is still GDScript-first.

### Best when

AI is the primary coder and the repo should stay small, text-diff friendly, and dependency-light.

# Recommendation for This Project

Because AI will do most implementation from our docs:

- **Godot 4 is slightly better for AI-primary, git-friendly, lightweight iteration.**
- **Unity is the better practical choice if your familiarity is the main human safety net.**

Given you already prefer Unity and can intervene yourself, the recommended lock is:

> **Unity (current LTS / stable supported version), URP, custom grid TB — no random combat Asset Store kit as architecture.**

Godot remains a valid alternative if you later decide human editor familiarity matters less than repo simplicity.

# Pending Confirmation

Engine lock awaits explicit user confirmation:

- **A)** Unity (recommended with your familiarity)
- **B)** Godot 4 (recommended if optimizing purely for AI/git lightness)

# Implementation Constraints if Unity Is Chosen

1. Custom grid + Turn Action Economy owned by project code.
2. Environment Actions from [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md) before fancy character controllers.
3. Prefer URP and minimal packages.
4. AI must follow docs in `11.x` milestones in order: S0 grid greybox before POI/combat logic.
5. No replacing Decision Engine with a marketplace tactics pack.

# Implementation Constraints if Godot Is Chosen

1. Same custom grid + TB ownership.
2. Prefer GDScript or C# consistently (pick one at repo init and keep it).
3. Same milestone order and doc obedience.
4. Keep scenes/scripts text-friendly and small.

# Documentation Review

- Does not change design fantasy; only selects delivery tool.
- S0 remains first implementation milestone, now grid-native.
- Final engine not Canonical until user confirms A or B.

# Related Documents

- [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
