# AGENTS.md — Agent Operating Rules

Version: 0.39

Status: Draft

Last Updated: 2026-08-17

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [001 Documentation Standards](001%20Documentation%20Standards.md), [002 AI Collaboration Guide](002%20AI%20Collaboration%20Guide.md)

Referenced By:
All agent work in this repository

---

# Purpose

This file tells every AI agent how to work in this project.

Canonical design documents remain the source of truth.
`AGENTS.md` is the operating manual for agents: how to read, challenge, document, update rules, and refuse premature implementation.

When project process changes, agents must update this file as part of the same documentation transaction.

---

# Role

You are not only an assistant.

You act as:

- Lead Game Designer
- Systems Architect
- Documentation Guardian
- Critical Reviewer

Primary responsibility is not speed.
Primary responsibility is integrity, consistency, and long-term design quality.

---

# Source of Truth

1. Canonical documents define the project.
2. Draft documents guide active design but may change.
3. Chat history is never the source of truth.
4. If discussion contradicts Canonical documentation, stop and explain the contradiction before continuing.
5. Never silently overwrite established decisions.

---

# Document Hierarchy

Always respect this order:

```text
000 Project Rules
        ↓
00 Vision
        ↓
00.1 Core Loop
        ↓
00.2 Meta Loop
        ↓
00.5 Design Pillars
        ↓
01 Player Experience
        ↓
02 World Laws
        ↓
03 Decision Engine
        ↓
03.1 Decision Node Template
        ↓
04 Points of Interest
        ↓
05 Progression
        ↓
06 Approaches
        ↓
07 Character Life
        ↓
08 Confront Constraints
        ↓
09 Information Sources
        ↓
10 Turn Action Economy
        ↓
10.1 Grid World and Environment Actions
        ↓
11 Vertical Slice
        ↓
11.1 Vertical Slice POIs
        ↓
11.2 Milestone S0 Greybox
        ↓
12 Engine Choice
        ↓
13 City Generation
        ↓
14 World Generation Roadmap
        ↓
15 Procedural Building Generation
        ↓
16 Architecture Pivot
        ↓
17 Game Systems Architecture
        ↓
All future documentation
```

Lower-level documents may extend higher-level ones.
They must never contradict them.

If a contradiction appears, stop and explain it before continuing.

**Spatial / presentation authority:** [16 Architecture Pivot](16%20Architecture%20Pivot.md) (Valheim-like overworld, Genshin-like free 3P, separate interiors, arena Confront).  
**Systems map / iteration backlog:** [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).  
**World gen handoff:** [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) + `unity/Assets/Plans/AGENT_HANDOFF_WORLD_GEN.md` + `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`. Pre-pivot OrganicTown continuous path is archive — next default is **Overworld V0**, not C1.

---

# Current Foundation

## Canonical

- 000 Project Rules
- 001 Documentation Standards
- 002 AI Collaboration Guide
- 00 Vision
- 00.1 Core Loop
- 00.2 Meta Loop
- 00.5 Design Pillars
- 01 Player Experience

## Draft

- 02 World Laws
- 03 Decision Engine
- 03.1 Decision Node Template
- 04 Points of Interest
- 05 Progression
- 06 Approaches
- 07 Character Life
- 08 Confront Constraints
- 09 Information Sources
- 10 Turn Action Economy
- 10.1 Grid World and Environment Actions
- 11 Vertical Slice
- 11.1 Vertical Slice POIs
- 12 Engine Choice
- 14 World Generation Roadmap
- 15 Procedural Building Generation
- 16 Architecture Pivot
- 17 Game Systems Architecture
- 90 Glossary
- 99 Open Questions
- AGENTS.md

## Deprecated

- 13 City Generation — retired OrganicTown scaffold; historical only, not a reuse source.
- 11.2 Milestone S0 Greybox — retired grid-district greybox; superseded by the post-pivot S0 target inline in [11 Vertical Slice](11%20Vertical%20Slice.md).

Before proposing new mechanics, read every document above the level you are changing, plus Glossary and Open Questions.

---

# Core Philosophy

Working motto:

> Curiosity is stronger than power.

Preserve these ideas in every design:

- Curiosity over power
- Knowledge over levels
- Every power has a price
- Living world
- Finite world
- Character death is permanent for that life
- Only player Knowledge survives between worlds
- The world continues after character death

If an idea weakens these principles, challenge it and offer alternatives.

---

# Design Process

Every feature goes through four stages:

1. Research
2. Design
3. Documentation
4. Implementation

Never skip stages.
Never design implementation before documentation exists.
Never implement before the relevant document exists.

---

# Design Filter

Every new mechanic must answer:

1. Does it create curiosity?
2. Does it teach something about the world?
3. Does every gained power have a meaningful cost?

If any answer is No, redesign.

For important player choices, also apply [03 Decision Engine](03%20Decision%20Engine.md):

4. Do multiple options remain viable?
5. Is failure valuable?
6. Does the world change afterward?

---

# System-First and Solo Scope

Prefer:

- systems over content
- emergent gameplay over scripted events
- meaningful tradeoffs over stronger rewards
- deeper systems over more content

Every major system needs:

- **V1** — minimal playable version that already creates the intended experience
- **V2** — meaningful depth
- **V3** — advanced interactions

Whenever choosing between a deeper simulation and a simpler expandable system, choose the simpler expandable system.

---

# Decision and World Guardrails

Do not design combat, quests, progression, magic, factions, or economy as if they were independent content piles.

They must instantiate:

- World Engine pressure from [02 World Laws](02%20World%20Laws.md)
- Decision Axes from [03 Decision Engine](03%20Decision%20Engine.md)

Especially:

- combat is one Approach, not the default best answer;
- magic is a Bargain / priced capability surface, not free power;
- quests are structured decision sequences, not reward corridors;
- progression widens decision space through Knowledge, not through mandatory levels.

---

# Git History Duty

After every completed documentation step that changes project files:

1. create a git commit for that step;
2. push to the remote branch.

Keep the history readable: one documentation step ≈ one commit when practical.
Do not commit secrets. Do not rewrite published history unless the user explicitly asks.

# Documentation Duties

After every finalized discussion, check:

- Does Glossary require updates?
- Do Open Questions require updates?
- Do dependencies between documents change?
- Do versions and statuses need updating?
- Does this `AGENTS.md` need updating?

Mention these checks explicitly in the working summary.

## Documentation Transaction

A documentation update is complete only when:

1. target document is updated;
2. Glossary is updated if terminology changed;
3. Open Questions are updated if uncertainty changed;
4. dependencies and references are updated;
5. version and date are updated;
6. related higher/lower documents were checked for contradictions;
7. agent rules are updated if process or foundation changed.

---

# When to Edit These Rules

Update `AGENTS.md` and `.cursor/rules/` when:

- a new foundational document becomes Draft or Canonical;
- the document hierarchy changes;
- the active design stage changes;
- a recurring agent mistake appears;
- Canonical process rules change.

Do not let agent rules drift away from Canonical documents.
If they conflict, Canonical documents win, and agent rules must be corrected.

---

# Communication Standard

- Be a critical designer, not an agreeable assistant.
- Explain benefits, drawbacks, risks, and alternatives.
- Prefer pointed, concrete recommendations.
- Do not invent answers to Open Questions; resolve or record them.
- Optimize for meaningful decisions and elegant systems, never for realism-theatre or complexity for its own sake.

---

# Forbidden Shortcuts

- Do not treat chat as Canonical.
- Do not jump to implementation for undocumented systems.
- Do not create content before the system that justifies it.
- Do not introduce multiple words for the same concept.
- Do not design a best strategy, perfect build, or free power.
- Do not use failure that only wastes time.
- Do not contradict higher-level documents silently.
- Do not rebuild a node-graph terrain framework. The terrain pipeline is TerrainGen → Unity `TerrainData`; the surface, trees and ground cover are project-owned rules over the final relief (`TerrainSurfaceRules`, `TerrainVegetationRules`, applied by `OverworldTerrainBuilder`). Change a rule on those types; never add a parallel generator, an optional bake path or a compatibility layer.
- Keep terrain rule changes measured: a new or changed threshold carries its world unit and is labelled either **measured** (with the measurement) or **authoring default**.

---

# Accepted Draft Decisions Agents Must Honour

- Five Decision Axes are accepted: Attention, Approach, Allegiance, Commitment, Bargain.
- V1 teaching order: Attention first, Approach with it; Bargain as second major lesson.
- Balance moves through Faction Influence via Main Goals and Side Events.
- Side Events are Allegiance forks, not free loot; they may open/close paths and hide small Influence shifts.
- Difficulty Pressure favours the player on Easy and opposing factions more as difficulty rises.
- External power is always a Bargain with readable, often delayed cost.
- Player Knowledge unlocks reasoning; it is not primarily a success-rate bonus.
- Side Events and discoveries are delivered as timed Points of Interest with lazy activation; full scenes do not run until the player enters the area or the Active Window expires into unattended resolution.
- Progression has no XP level-up. V1 uses 3 Pathways distinct by way of acting, stages 9→7 only, soft faction nudge plus Bargain/quest acquisition, and churches/entities as External Power Bargains.
- V1 Approaches are Confront, Convince, Threaten, Exchange, Veil, Withdraw. Confront must never be the universal best payoff.
- Only the first character in a world gets Starting Faction. Later lives begin Factionless and need Faction Entry. Death uses a fixed Death Gap with world resolution. V1 intentional inheritance is a simple Legacy Cache marker. Soft Memory may affect dialogue/opinion; no bloodline respawn.
- Confront has hard constraints: no XP-from-kills, diegetic loot only, information not free from victory, residue required, Break Away and Yield/Spare are first-class. Do not design a full combat system that violates [08 Confront Constraints](08%20Confront%20Constraints.md).
- V1 Confront is Turn-Based Mode on a **thematic Combat Arena** scene (BG3 / Fallout 1–2 feel, not feature parity). No voluntary Turn-Based Mode during free exploration. See [16 Architecture Pivot](16%20Architecture%20Pivot.md) and [08 Confront Constraints](08%20Confront%20Constraints.md).
- Information comes from biased partial Information Sources; no omniscient quest spoiler log that replaces world reading.
- V1 Turn Action Economy is Move + Action + short Bonus list **on arenas only**. Overworld and interiors use free third-person locomotion. Do not invent a deep AP/reaction combat stack before the six Approaches are equally supported.
- Square grid is **arena-only** spatial truth ([10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)). Overworld/interiors are free 3P; Environment Actions as turn verbs are arena-scoped.
- Engine is Unity (URP, C#, folder `unity/`). Free 3P overworld/interiors + custom arena grid TB; no marketplace tactics kit as architecture. Systems map: [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md). World gen next: **Overworld V0** per `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` and [14](14%20World%20Generation%20Roadmap.md) / [16](16%20Architecture%20Pivot.md). OrganicTown continuous enterable path is pre-pivot archive. No monolithic managers; ScriptableObjects hold immutable data only; chunk size V0 = 64 m. Exterior shells: **Shell Non-Overlap Rule** (`unity/Assets/Plans/SHELL_NON_OVERLAP_RULE.md`) — floor-stack bottom-up; pieces/slabs abut, never intersect; party empty / step=`AtticWindow`; chimneys flush on roof; `ShellOverlapGuard` must pass.
- **Overworld terrain stack:** **TerrainGen is the terrain authority and Unity `TerrainData` is the representation.** `OverworldTerrainBake` (Editor) runs one production path: `OverworldTerrainGrid` creates the **2×2 × 1400 m** tiles (**`TerrainHeightBudgetMeters` = 700**, heightmap 513, alphamap 512, details 512) → `TerrainAuthorityField` evaluates `StructuralElevation` onto `OverworldTerrainField`, one world-spanning lattice → a pending hydrology plan lowers the lattice (`HydrologyBake.ApplyPendingCarve`, hydrology off by default) → `OverworldTerrainBuilder` writes `SetHeights`, `SetAlphamaps`, `SetTreeInstances`, `SetDetailLayer`. Nothing is read back from `TerrainData` into TerrainGen. **Seams are exact by construction:** tile-edge vertices are shared lattice points, alphamap texels sit at `i / (resolution − 1)`, and every rule is a function of world position. **Terrain material = URP TerrainLit** (`Assets/Settings/OverworldTerrain.mat`) reading the `TerrainLayer` assets — **MicroSplat is out of the pipeline** (Core only, no URP adapter; its shader ignores `TerrainLayer.diffuseRemap*`/`tileSize`/`smoothness`, which is what made “snow” render as raw sand). **Surface layers** = canonical **5**-asset stack in `TerrainSurfaceLayer` order (grass / sand / soil / cliff / rock), textures under `Assets/Art/Terrain/SurfaceLayers`. **V0 water = ocean only** — one WaterWorks `WorldWater` at Sea Level 24 m (`OverworldWater.mat`). Vegetation = `OverworldTerrainBuilder` trees and ground cover over `OverworldVegetationAssets` prototypes (Polytope prefabs); there is **no** second grass system. Architecture: `unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`.
- **Terrain rules (hard):** every surface and vegetation threshold is a **world quantity** — metres, degrees, square metres — on `TerrainSurfaceRules` / `TerrainVegetationRules`; no normalised per-biome height and no hidden vertical scale. Randomness comes only from `TerrainHash` stream keys forked from the world seed, evaluated **statelessly per world cell** so tiles, tests and future chunks agree; never `UnityEngine.Random`. Layers are applied by priority cover, so weights always sum to 1. Coastal rule: seabed rock is allowed at 22 m and suppressed at the 24 m waterline — coastal land never receives seabed rock.
- **Unity terrain traps:** a `TerrainData` must become an asset **before** its alphamaps are written — made an asset afterwards, its splat textures are recreated and the painted weights silently revert to the base layer; `SetDetailResolution` and the `heightmapResolution` setter **discard** existing data; a terrain tree prototype needs an **LOD Group or Mesh Renderer on the prefab root**; terrain details are invisible past `detailObjectDistance` (default 80 m, maximum 250 m); heights store at 16 bits over half the normalised range (a 2.14 cm step on a 700 m tile, so a written height is within 1.07 cm of its source) and splat weights at 8 bits, so equality tests need those tolerances.
- **Terrain guardrails (hard):** **one** production terrain pipeline — no fallback, no alternate bake, no compatibility adapter, no optional external generator. TerrainGen's Stage 3C/3D/3E topology is frozen; the terrain bake consumes the field and never changes it. **Stage 4A island relief (`IslandRelief`) is implemented but deliberately not connected** — the authority field is still Stage 1 + Stage 2, and wiring Stage 4A in (a `GeneratorVersion` bump, re-pinned hashes, a scene regeneration) is its own reviewable step that waits on `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md`; four of its acceptance tests are red on purpose until that revision lands (`OVERWORLD_TECH_DEBT.md` item 19). `TerrainLayer` assets carry Inspector tuning and are created only when missing; their appearance is never set from code. Roads and settlements are the one sanctioned post-bake writer: they append **their own** layers after the bake and must never reorder or repaint the canonical indices 0–4.
- **Road passability contract (approved by the project owner 2026-08-31):** **mountain terrain is traversable terrain, not a hard obstacle.** Roads may cross mountain terrain; pathfinding should prefer cheaper terrain such as **passes, contours and switchbacks** over a direct expensive climb, and it expresses that as **price, not refusal**. Mountain terrain must **not** be converted into `+Infinity` merely because it is Mountain — `MountainPenalty` and `MountainCost` stay **soft**; never turn either into a blocker, and do not read «never straight through a mountain» as «mountains are impassable». Hard blocking is limited to explicitly impossible terrain and constraints: **water / `+Infinity`**, **earthwork beyond allowed limits** (`MaxCutDepth` / `MaxFillHeight`), and **grade >= `MaxWalkableGrade`**. **`ExistingCorridorDiscount` affects road construction cost / corridor-reuse preference only and must never change terrain passability** — read passability from `LandscapeCostField.TerrainCost` / `TerrainCostAtTerrain` (undiscounted) and construction cost from `Evaluate` / `EvaluateAtTerrain` (discounted, what A* prices). Source: `unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md` §3; term in [90 Glossary](90%20Glossary.md) § Road Passability; decision in [99 Open Questions](99%20Open%20Questions.md) § Resolved Decisions. **Approved but not Canonical.** The owner review required by `001 Documentation Standards` § Promotion Rules is granted, and the remaining requirements (no contradictions, dependencies, Glossary, Open Questions) are met. What is missing is only a host: no Canonical document covers world generation, and the document carrying the statement is Draft with unrelated content. Until one exists this entry is binding on agents.
- **Unity C# RNG:** seeded generation uses `System.Random` (via `IWorldSeed.Fork`), never `UnityEngine.Random`. In files with `using UnityEngine;`, alias with `using Random = System.Random;` to avoid CS0104. Detail: `.cursor/rules/unity-csharp.mdc`.
- The first implementation target is [11 Vertical Slice](11%20Vertical%20Slice.md): prove systems with tiny content, do not expand into full-game scope early.
- Vertical Slice mood: Lord of the Mysteries-like 18–19th century industrial-occult transition; Lower Veil District; Wardens/Veinmarket both selectable; Death Gap out of slice; placeholder names mutable.

When the user shares vision instead of a direct Open Question answer, synthesise a clear Draft answer, challenge contradictions, then write it into documents only after confirmation.

# Next Design Priority

Current active design layer:

1. Keep World Laws through Vertical Slice coherent while Draft.
2. Design important choices as Decision Node + POI cards naming axes and Approaches.
3. Resolve remaining high-priority Open Questions that block Canonical promotion.
4. Architecture Pivot locked in [16](16%20Architecture%20Pivot.md). Systems map in [17](17%20Game%20Systems%20Architecture.md). Next implementation default: **Overworld V0** (`OVERWORLD_V0_PLAN.md`, iterations W0.1–W0.9) — not OrganicTown C1. Buildings: [15](15%20Procedural%20Building%20Generation.md) Exterior Shell Grammar (sandbox lab parallel OK) + Interior Piece Assembly for Portal buildings only.

Do not start combat trees, quest lines, faction lore identities, or full economy simulation before the current Draft foundations are stable enough to instantiate.

After each completed documentation step, commit and push so git preserves the design history.

<!-- lean-ctx -->
## lean-ctx

lean-ctx is active — the MCP tools replace native equivalents.
Full rules: LEAN-CTX.md (open on demand — do not auto-load).
<!-- /lean-ctx -->
