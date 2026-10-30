# AGENTS.md — Agent Operating Rules

Version: 0.81

Status: Draft

Last Updated: 2026-10-28 (G3f under the mission: calibration (2 runs, fixes F1, F2), GF-RF frozen, held-out part (1) 8 of 10; the owner's blind review pending; nothing declared passed; production unchanged. Earlier: G3f: the owner's autonomous research mission (2026-10-25) recorded; WP0 done; the technical specification and the research package written. Earlier: G3f: the owner's decisions D-3 … D-12 on content recorded — phase plan v0.3; none authorises execution; the D-9 and D-12 documentation packages, WP0 and WP1 not authorised. Earlier: G3f — regional geography architecture: the owner approved the phase (D-1) and its architecture as the hypothesis to test (D-2); only step A of its plan is authorised; D-3 … D-12 open; GEO-10, GEO-11 added. 2026-10-24: G3e-PM ran: registered D1 INCONCLUSIVE, H FAIL by §7 (accepted), a one-run deviation DEV-1; the owner kept S-LM1's contracts, so the S-LM1 line is stopped. Earlier: G3e-PM: the post-mortem checks pre-registered, plan approved by the owner, not run; nothing runs without a separate authorisation. Earlier: G3e: the not-ready report accepted, Step 1 closed; the backlog since 2026-10-19 committed to main step by step. Earlier: Step 1 ends not ready — report written; T1 kept. Earlier: run 11 — T1, the harmonic toe — implemented and verified, review §23. Earlier: T1 pre-registered, review §22. Earlier: decision note on a continuous toe and the R7(c) axis reading, review §21. Earlier: recording run 10 — the toe is the main cause, review §20. Earlier: (b) kept; run 10 pre-registered, review §19. Earlier: run 9 — option (b) implemented, its check passes; new pools where outflow stays raised. Earlier: rule (b) kept as implemented; decision note on plateau placement, review §17. Earlier: run 8 — ramp rule (b) implemented; its check fails for causes outside the ramp. Earlier: M-1 implementation accepted with caveats, 150 m kept; ramp rule (b) next with a fourth criterion. Earlier: run 7 — M-1 implemented in the measurement code and verified; gates unchanged. Earlier: the full M-1 rule checked on the preserved lattices. Earlier: M-1 approved as a specification rule, spec v0.5 §14. Earlier: S-LM1 ramp rule (b) accepted, not implemented. Earlier: S-LM3 closed with a negative result, option (a); S-LM1 continues alone. Earlier: G3e diagnostic run 6 done — recording only, #14 reverted, results in the run-5 review §11. Earlier: G3e Step 1 after five calibration runs: no synthesis accepted; owner decisions on PD-1, Q9 #9/#14, M-1 and #12; no run or code change without a separate authorisation. Earlier: Step 1 authorised within C6–C7. Earlier: LMC is G3e; C8 commit before the freeze; C9 reference data outside the repository; only the Step 1 authorisation remains. Earlier: LMC specification v0.4 — A1, A3 and the whale-back option (b) approved; open C1, C8, C9; Step 1 not authorised. Earlier: C2–C5 prepared. 2026-10-22: LMC design note approved as the research plan; LMC kept as the working name; Step 1 readiness check written; Step 1 not authorised. Earlier: LMC technical specification approved, §12 items 1–4. Earlier: drafted and checked on historical G3c data. 2026-10-21: LMC design note v0.2 — the owner's revision; LMC pair S-LM3 + S-LM1 approved. Earlier: post-G3c review; G3c ran; G2: target geography D6 approved)

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
10.1 Grid World and Environment Actions
        ↓
10.2 Combat System Draft
        ↓
18 World Time and Living Simulation
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
- **Overworld terrain stack:** **TerrainGen is the terrain authority and Unity `TerrainData` is the representation.** `OverworldTerrainBake` (Editor) runs one production path: `OverworldTerrainGrid` creates the **2×2 × 1400 m** tiles (**`TerrainHeightBudgetMeters` = 700**, heightmap 513, alphamap 512, details 512) → `TerrainAuthorityField` evaluates `IslandElevation` (`H4A`) onto `OverworldTerrainField`, one world-spanning lattice → a pending hydrology plan lowers the lattice (`HydrologyBake.ApplyPendingCarve`, hydrology off by default) → `OverworldTerrainBuilder` writes `SetHeights`, `SetAlphamaps`, `SetTreeInstances`, `SetDetailLayer`. Nothing is read back from `TerrainData` into TerrainGen. **Seams are exact by construction:** tile-edge vertices are shared lattice points, alphamap texels sit at `i / (resolution − 1)`, and every rule is a function of world position. **Terrain material = URP TerrainLit** (`Assets/Settings/OverworldTerrain.mat`) reading the `TerrainLayer` assets — **MicroSplat is out of the pipeline** (Core only, no URP adapter; its shader ignores `TerrainLayer.diffuseRemap*`/`tileSize`/`smoothness`, which is what made “snow” render as raw sand). **Surface layers** = canonical **5**-asset stack in `TerrainSurfaceLayer` order (grass / sand / soil / cliff / rock), textures under `Assets/Art/Terrain/SurfaceLayers`. **V0 water = ocean only** — one WaterWorks `WorldWater` at Sea Level 24 m (`OverworldWater.mat`). Vegetation = `OverworldTerrainBuilder` trees and ground cover over `OverworldVegetationAssets` prototypes (Polytope prefabs); there is **no** second grass system. Architecture: `unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`.
- **Terrain rules (hard):** every surface and vegetation threshold is a **world quantity** — metres, degrees, square metres — on `TerrainSurfaceRules` / `TerrainVegetationRules`; no normalised per-biome height and no hidden vertical scale. Randomness comes only from `TerrainHash` stream keys forked from the world seed, evaluated **statelessly per world cell** so tiles, tests and future chunks agree; never `UnityEngine.Random`. Layers are applied by priority cover, so weights always sum to 1. Coastal rule: seabed rock is allowed at 22 m and suppressed at the 24 m waterline — coastal land never receives seabed rock.
- **Unity terrain traps:** a `TerrainData` must become an asset **before** its alphamaps are written — made an asset afterwards, its splat textures are recreated and the painted weights silently revert to the base layer; `SetDetailResolution` and the `heightmapResolution` setter **discard** existing data; a terrain tree prototype needs an **LOD Group or Mesh Renderer on the prefab root**; terrain details are invisible past `detailObjectDistance` (default 80 m, maximum 250 m); heights store at 16 bits over half the normalised range (a 2.14 cm step on a 700 m tile, so a written height is within 1.07 cm of its source) and splat weights at 8 bits, so equality tests need those tolerances.
- **Terrain guardrails (hard):** **one** production terrain pipeline — no fallback, no alternate bake, no compatibility adapter, no optional external generator. TerrainGen's Stage 3C/3D/3E topology is frozen; the terrain bake consumes the field and never changes it. **Stage 4A island relief (`IslandRelief`) is the production terrain authority** (revision R1, connected 2026-09-18, `GeneratorVersion` 2): `TerrainAuthorityField` evaluates `IslandElevation` — **`H4A = H1 + U2 + R4A`**. `R4A` is non-negative and exactly zero at and seaward of the shore, so connecting it moved no coastline and no ocean sample. **Stage 3D/3E still declare basins and outlets on `StructuralElevation` (`H2`)** — that is the surface their topology was frozen against, and Stage 3F will carve `H = H4A − C3F`; never repoint them at `H4A`. `includeMassifs: false` stays Stage 1 only (no Stage 2A means no macro plates for 4A to stand on). Its EditMode suite is green; residuals (zero knolls, apron ring/zone, coast-aligned gradient, curvature ratio, massif-foot sweep pits) are recorded in `OVERWORLD_TECH_DEBT.md` item 19 as closed-with-residuals owned by Stage 3F / Stage 4B — not as expected red tests, and not as things to retune inside 4A. `TerrainLayer` assets carry Inspector tuning and are created only when missing; their appearance is never set from code. Roads and settlements are the one sanctioned post-bake writer: they append **their own** layers after the bake and must never reorder or repaint the canonical indices 0–4.
- **Road passability contract (approved by the project owner 2026-08-31):** **mountain terrain is traversable terrain, not a hard obstacle.** Roads may cross mountain terrain; pathfinding should prefer cheaper terrain such as **passes, contours and switchbacks** over a direct expensive climb, and it expresses that as **price, not refusal**. Mountain terrain must **not** be converted into `+Infinity` merely because it is Mountain — `MountainPenalty` and `MountainCost` stay **soft**; never turn either into a blocker, and do not read «never straight through a mountain» as «mountains are impassable». Hard blocking is limited to explicitly impossible terrain and constraints: **water / `+Infinity`**, **earthwork beyond allowed limits** (`MaxCutDepth` / `MaxFillHeight`), and **grade >= `MaxWalkableGrade`**. **`ExistingCorridorDiscount` affects road construction cost / corridor-reuse preference only and must never change terrain passability** — read passability from `LandscapeCostField.TerrainCost` / `TerrainCostAtTerrain` (undiscounted) and construction cost from `Evaluate` / `EvaluateAtTerrain` (discounted, what A* prices). Source: `unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md` §3; term in [90 Glossary](90%20Glossary.md) § Road Passability; decision in [99 Open Questions](99%20Open%20Questions.md) § Resolved Decisions. **Approved but not Canonical.** The owner review required by `001 Documentation Standards` § Promotion Rules is granted, and the remaining requirements (no contradictions, dependencies, Glossary, Open Questions) are met. What is missing is only a host: no Canonical document covers world generation, and the document carrying the statement is Draft with unrelated content. Until one exists this entry is binding on agents.
- **Target geography D6 (approved by the project owner 2026-10-20)** — [GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) §9. The overworld island is a **regional geography with a landform grammar**: mountains are the dominant structure (a branching system with several peaks, connected ridges, spurs, passes and valleys — neither one smooth dome nor a uniform mountain field); several large plateaus in different regions, integrated into ridges or foothills, with readable tops and cliff or escarpment edges; barriers and routes designed together (impassable faces allowed, important regions reachable by intentional routes or meaningful bypasses); valleys, lowlands, plains and foothills as deliberate regions; rivers from elevated regions through valley corridors to designated coastal outlets, lakes where the structure holds them, coasts caused by their hinterland; room for 3–5 settlements and numerous POIs; success judged at island, regional and player scale. Source: the owner's concept image `References/G2_target_island_concept_2026-10-20.webp` — a **project reference inspired by DragonSword: Awakening's visual direction, not an official map**; it is a composition, so never infer counts, sizes, heights or positions from it as requirements. D6 sets a target, not a technique: production terrain stays `H4A`, and nothing in D6 authorises implementation; its open parts (footprint, tier wording GEO-2/GEO-8, placement GEO-3, coastal contract GEO-4, lakes GEO-5, gates GEO-7, barriers in play 3F-20) stayed with the owner — what was settled of them on 2026-10-21 is the next entry.
- **Geography decisions after G3c (2026-10-21)** — [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2; [99 Open Questions](99%20Open%20Questions.md) GEO-1 … GEO-9 decision records. Each part carries a status; only the *Resolved* parts below bind agents. **Owner direction:** GEO-1 — the world is not enlarged on G3c's evidence; today's footprint (contour A in controlled research) is the baseline for landform-model research; size is revisited only when a promising model's remaining failures are attributed to room by an experiment that separates area from shape, or a gameplay requirement asks for it; a 3 × 3-tile world stays a travel-pacing decision. GEO-2 — one connected branching mountain system counts as **one T1**; the tier row's numbers are unchanged. GEO-4 — varied coasts caused by their hinterland are the target direction; the production coastal contract and its owning stage are not decided, and G3c's R6 is not validation of coastline diversity. GEO-5 (principle) — lakes only where the structure holds them, never placed arbitrarily or required per seed. GEO-7 — hard gates are the requirements with direct gameplay or topological meaning; the owner's review at three scales judges composition; Strahler order, basin area, slope shares and other morphology metrics are diagnostics; pre-registered thresholds are never changed retroactively (G3c's R1–R8 stand as evaluated); flat land is never rewarded for its own sake. GEO-8 (principle) — a candidate is never rejected only because a large plateau exceeds the T2 row or falls between tiers, and **tier labels never decide traversal**; the tier table stays in force for production design until revised. **Evidence-backed policy:** GEO-3 — landform research continues under option (c); GEO-5 — option (b): a lake only in a **Declared Basin** with one outlet to the ocean, every undeclared closed depression a defect, no endorheic basin, V0 water still ocean only; GEO-9 — geography provides regions, candidate areas and routes, placement (W.4, W.5, roads) chooses sites and never reshapes terrain at region scale to meet a count. None of these authorises production terrain work, a Stage 1 change, a new experiment or G3d.
- **Unity C# RNG:** seeded generation uses `System.Random` (via `IWorldSeed.Fork`), never `UnityEngine.Random`. In files with `using UnityEngine;`, alias with `using Random = System.Random;` to avoid CS0104. Detail: `.cursor/rules/unity-csharp.mdc`.
- The first implementation target is [11 Vertical Slice](11%20Vertical%20Slice.md): prove systems with tiny content, do not expand into full-game scope early.
- Vertical Slice mood: Lord of the Mysteries-like 18–19th century industrial-occult transition; Lower Veil District; Wardens/Veinmarket both selectable; Death Gap out of slice; placeholder names mutable.

When the user shares vision instead of a direct Open Question answer, synthesise a clear Draft answer, challenge contradictions, then write it into documents only after confirmation.

# Proposed Decisions Awaiting Owner Review

Not accepted decisions. They are recorded so agents know what is under review; do not implement them and do not cite them as approved.

- **Stage 3F physical-surface contract** ([handoff](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) §22; [99 Open Questions](99%20Open%20Questions.md) 3F-1, 3F-6, 3F-18, 3F-19): `H4A` stays the **Structural Height** and E would be the **Final Terrain Surface**, the only height written to `TerrainData`, with nothing flowing back into TerrainGen; the final surface is read as the **Triangle Reading** of the quantised lattice (SW→NE split); a **Walkable Triangle** is strictly below 45°; **T-edge Connectivity** is the conservative lower bound of guaranteed walking connectivity, **T-vertex Connectivity** an experimental upper bound, the land between them the **Uncertain Connectivity Zone**; slope readings D2, D3, D6 and D7 never decide a character's walkability, an object's footing, spawn or a character's access gating, D4 keeps only its own uses, and road passability stays under the approved Road Passability contract; **Jump Accessibility** is a separate notion. If approved, the walking contract holds only for the configuration in handoff §22.6: a change to the Unity version, the terrain layout, the `CharacterController` values or the movement and jump constants voids it until re-measured. E is not `H3F`; the line "Stage 3F will carve `H = H4A − C3F`" under Accepted Draft Decisions describes the existing Stage 3F code and changes only with 3F-1. Terms: [90 Glossary](90%20Glossary.md).
- **Rules already in force, restated** (no new decision): E is not connected to `OverworldTerrainBake` or `TerrainAuthorityField`; Stage 3F's carve keeps its existing binding — it cuts along the frozen `FinalValleyTrajectory` through `ValleyTrajectoryNetwork.BuildCarve`, the API-only unfreeze already spent (`unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §20) — and no frozen type changes; Stage 1–4A and the Stage 3C trajectories stay unchanged; Open Questions are not answered for the owner, so 3F-10 (filling inherited depressions), 3F-11 (flat-filled pits), 3F-14 (cirques as permitted basins), 3F-20 (jump and barriers) and the arc-class coverage of the 7.2 m bound in 3F-4 get options and consequences, never a recommendation dressed as a decision.
- **Precautionary guard pending 3F-19** (proposed, not accepted): add no new physical use of slope readings D2, D3, D6 or D7. It leaves every existing use and the approved Road Passability contract as they are.
- **Geography-first programme** ([research record](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md); [99 Open Questions](99%20Open%20Questions.md) § Terrain — Geography-first world shape, GEO-1 … GEO-9; the G2 decision brief [GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md); the post-G3c review [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md)): a target **Geographic Hierarchy** (relief regions, a mountain system with ridges and passes, valleys as complements, caused coasts, explicit basin policies), a stage responsibility map that keeps one terrain authority, a geometry-only acceptance suite, and an isolated heights-only **Geometry-First Review** tool. Its conclusion that most missing geography is upstream of Stage 3F is a research finding, not a decision. **One part is approved: GEO-6 (a)** — the read-only Editor tooling G0 (`TerrainGenGeographyDump`) and G1 (`GeometryFirstReview`) is built (2026-10-18). Keep both read-only: they read lattices, write only outside `Assets`, never save a scene or a `TerrainData`, and their thresholds are authoring defaults that gate nothing (GEO-7's classification makes morphology metrics diagnostics). G3 and its follow-up G3b (2026-10-19 … 20) were commissioned by the owner as research testing GEO-2 (a) as a hypothesis; they approve no mountain-count policy, no coast contract and no architecture. The target itself is now D6 (Accepted Draft Decisions above). **G3c** — research only, region-first, comparing today's footprint with about 40 % more land in the same 2.8 km world, a fixed shoreline per footprint and a per-segment coastal profile — has its scope set (brief D8, §8.3) and was **authorised by the owner on 2026-10-20** on the brief's pre-run checklist (tier rules diagnostic inside G3c); it ran on 2026-10-20 … 21 (neither contour passed; research record §14) and was only ever a research plan, never a basis for production work; its method, parameters, metrics and comparison rule are fixed before any run and are never tuned to make a candidate pass, and its results authorise no production change. Do not state that the island footprint limits drainage or settlement room: G3/G3b did not vary it (research record §13.2); it was a hypothesis G3c tested — and G3c compared two specific contours, so even its result does not attribute a difference to area alone (coastline variety and area-versus-shape are a later stage, G3d). In G3c, calibration never touches the evaluation seeds, and cliffs are reported as steepness and walking connectivity, never as impassable (3F-20). G3c did not show that the footprint limits anything either: it is **completed research that failed acceptance**, not proof that region-first geography is invalid (post-G3c review §1). **Proposed, not decided** (post-G3c review): GEO-8's revised tier contract — the T2 row kept for district shelves and a region-level **Large Plateau** form outside the tier table, subordinate to the mountain system; the v2 reading set for the next evaluation (R1–R11, form diagnostics Q1–Q9); GEO-3's production placement at G4; and an isolated **landform-model comparison** — two surface syntheses, structure fields with a fluvial steady state (S-LM3) and a generalised skeleton strip partition (S-LM1), on one shared typed-region layout (LM2), contour A only, calibration 110201 and 1337, held-out 1005–1014, under the working name *LMC*, which is **not an official experiment ID**. **The owner approved that pair on 2026-10-21**, and its step 0, the design note [GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md) — v0.2, **the owner's revision**, which governs — is written; the agent's v0.1 is kept as [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_DRAFT.md), source material only, not approved. **No LMC code and no LMC run is authorised** until every decision of the note's §12 is recorded explicitly — never infer one; report the blocking item and wait. On 2026-10-22 the owner **supported the note (v0.3) as the research direction and withheld authorisation for code and calibration**; the technical specification [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (elevation definitions with a world-height datum, the S-LM3 model contract, the gate and Q1–Q9 calculations with review-flag levels) was drafted and checked on historical G3c data only — read-only Python in the session scratchpad, no Unity run, no generation — and **approved by the owner the same day (its §12 items 1–4)**: world height as the datum (330–430 m world height = 306–406 m above the sea), the K1 plateau edge height, the S-LM3 model contract as an **experimental** representation, the gate calculations with K2, the flag levels of its §8.1, the layout validation and the S-LM1 ownership rule. These are fixed for LMC; **Step 1 is still not authorised**. Later the same day the owner **approved the design note (v0.5) as the research plan**, confirmed that Step 1 is a separate stage needing explicit authorisation — until then **no new generation, no Unity run and no production change** — and kept **LMC as the working name**, the official experiment ID to be chosen by the repository's naming rules **before implementation** (the G-series line suggests G3e; the owner decides). The conditions for that authorisation are listed in [GEOGRAPHY_LMC_STEP1_READINESS.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md) (C1–C9); do not start any of them that needs code or a Unity run before the owner says so. On 2026-10-23 C2–C5 were prepared with read-only checks on existing G3c data: the specification's §13 held proposed amendments A1–A3, the whale-back measure failed its check, and the calibration-grid reference that Step 1's C# must reproduce is in its §13.5. **The owner then approved A1 and A3 and chose option (b)** — the specification is **v0.4**: R8 covers every declared plateau top; D-cap (with K5) and D-reg are report-only diagnostics; the **whale-back is judged in the owner's visual review** and Q3, Q4, Q4m are never flagged. ~~Open before Step 1: C1 (official ID), C8 (commit policy), C9 (where the reference data is kept).~~ Decided the same day: **the official experiment ID is G3e** (LMC stays its working name; use "G3e (LMC)" in new documents); **LMC's code is not committed during Step 1** — the commit question returns to the owner before the freeze *(Step 1 closed not ready; the owner had everything committed to `main` on 2026-10-24)*; **the reference data lives at `D:\Hobby\game-dev\research-data\lmc-reference\`** (outside the repository, MD5 manifest) — do not edit it, and read the continuity-check grids from there. Every pre-authorisation condition is met; ~~only the explicit Step 1 authorisation remains~~ **Step 1 was authorised by the owner on 2026-10-23 within C6–C7** ([readiness](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md) §4): six Editor-only `TerrainGenLmc*` files, calibration seeds 110201 and 1337 only, outputs outside `Assets`, the 73-file protected manifest checked around every run, stop on any protected-hash change. Step 2 (the owner's review), the freeze and the held-out evaluation stay separate authorisations. **Step 1 state (2026-10-23):** five calibration runs; continuity with G3c holds; **no synthesis passes every hard gate and S-LM3's receiver iteration has never converged**; outputs preserved and SHA-256-verified at `D:\Hobby\game-dev\research-data\g3e-step1\`; review and decisions in [GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) §9 — PD-1 accepted only as a working interpretation (no base height assigned to vertices without an allowed path; outlets checked on the actual surface; its 100 m and region weights temporary authoring parameters), Q9 #9 not a solution, **Q9 #14 rejected**, M-1 option (c) chosen for checking but not implemented (a missing col is reported as not found), Q9 #12 not to be reverted before its cause is established, the instrumented diagnostic run recommended — **authorised the same day and run as diagnostic run 6** (no model change, #14 reverted, recording only; review §11: S-LM3's drainage tree reorganises every iteration, 96–97 % of the changes in the hillslope regime; S-LM1's base is free of closed depressions, the 1337 hollow is dammed by plateau 1's ramp). **The owner then closed S-LM3 in its current configuration as a negative result** (option (a); review §12): its non-convergence is the result, a probable hillslope-regime instability whose causation is not proven; **do not fit S-LM3 further** in this calibration — a changed law is a separate research line with its own plan. G3e continues with **S-LM1 only**; its ramp rule, the source of 110201's four depressions and M-1 are settled separately, never in one patch; if S-LM1 also fails the hard gates, G3e ends in a not-ready report, not a freeze. **S-LM1 ramp rule (b) accepted** (a 40 m constant-width strip along the ramp bearing; exact specification and pre-registered check in review §13.3) — implement it only on the owner's separate authorisation, alone in its own patch; it is the **next priority** (owner, 2026-10-23), with a fourth criterion — no new valley damming or disruption of drainage to the designated outlets (exact form C4, review §15.2, confirmed by the owner); a strip that fails is **never widened automatically** — option (c) is considered separately. **Implemented in run 8** (review §16): the sector's dam on 1337 is gone, but the pre-registered check fails (C1 both seeds, C2 110201, C4 1337) for causes outside the ramp — plateau tops and flanks across drainage lines, a band, an unreachable top — and 110201's R10 now fails. **The owner kept rule (b) as implemented**; the decision note on plateau placement across drainage lines and corridors is review §17 (the layout reserves a smaller footprint than S-LM1's plateau stage writes; options (a)–(d)). **Option (b) — S-LM1 never raises declared drainage — was authorised and implemented in run 9** (review §18): plateau tops exempt (pre-registered), its check passes, 110201's R10 passes again, 1337's valley-8 climb back to 4.27 m; but protected floors whose outflow still crosses raised plateau ground became three new pools. Do not widen the protection — that is option (c); option (a) stays unauthorised. **The owner kept (b)**; the recording run 10 (u, v, owning frame and the other mountain-stage terms, for the 18 pre-plateau axis climbs and 110201's four mountain-stage depressions) was pre-registered (review §19) and **run on the owner's authorisation** (review §20): the toe — the base at the nearest mountain-boundary vertex, piecewise constant — carries about 70 % of the pre-plateau axis climbs and makes three of 110201's four mountain-stage pits; the frame blend and ties inside the axis band most of the rest; §13.1's u candidate one item. Any change to the toe, the blend or how R7(c) reads an axis needs its own decision and authorisation. The decision note is review §21: recommended a harmonic toe (T1) and S-LM1 declaring a valley line (R-c), each a separate patch; **never change R7(c)'s calculation to read the lowest vertex (R-b)**; valley axes are not in the running minimum (Q9 #11's premise is refuted by run 10, but with today's banks the draft's rule cuts culminations) — the owner decides. **T1 was implemented in run 11** on the owner's authorisation (review §23: `ToeField` only; toe equal to the Python reference within 0.0000 m; toe steps and toe pits gone; long climbs toward valley mouths, tie climbs and plateau pools remain; no gate changed state); **the owner kept T1**. **G3e Step 1 ended not ready and is closed** (report accepted by the owner 2026-10-24; [GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md)): no candidate for Step 2, so no freeze and no held-out evaluation — do not generate the held-out seeds, do not start Step 2 or G3d, and add no further Step 1 patch; ~~the commit of the uncommitted backlog (C8) and~~ any follow-up stage (a layout amendment, S-LM1 valley placement, R-c, a changed S-LM3 law) ~~are~~ is the owner's decision, each with its own plan *(the backlog was committed on 2026-10-24)*. ~~After it, in order: the recording run for 110201's two unresolved depressions, then the limited S-LM1 calibration (freeze package or not-ready report).~~ *(Superseded: Step 1 closed not ready; the S-LM1 line stopped after G3e-PM.)* **On 2026-10-24 the owner approved the plan of the post-mortem checks G3e-PM** ([pre-registration](unity/Assets/Plans/GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md)): one views-only Unity step on the preserved run-11 lattices for the owner's look at S-LM1, then read-only checks D1–D4 on the preserved data; it changes no code, definition or threshold and touches no held-out seed. **Nothing of it runs before the owner's separate authorisation** (its §11: A0 commit, A1 views, A2 the look, A3 the checks); its decision table (§8) ends in an exact package, contract decisions first, or stopping S-LM1 — never in a patch by itself. **G3e-PM ran on 2026-10-24** (results in its §13; records at `D:\Hobby\game-dev\research-data\g3e-pm\`): the **registered D1 result is INCONCLUSIVE** (validity check V1 failed), **H is FAIL by §7** (accepted by the owner: 110201's elevated systems already reach undesignated shores on S-LM1's base), and a **one-run deviation, DEV-1**, completed the ledger (outside owners: S-LM1's base, its flank coordinate u, its valley cross-profile). Cite the three separately; never merge them. **The owner kept those contracts, so the current S-LM1 line is stopped** — a research-line decision, not a ban on the mechanisms: implement none of B1, B2 or C, and add no S-LM1 patch. Recorded with it: a base whose coastal surface decides heights beyond the coastal band width is a potential deviation from spec §6. Next, only with its own plan: revisit the landform families or a changed S-LM3 law; G3d waits. **M-1 (the pass col) approved as a rule of the specification** ([technical spec](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md) v0.5 §14): flanking culminations must be realised merge-tree peaks (prominence ≥ 30 m); the col is the one joining them, found only within 150 m of the declared pass, otherwise `not found` — **never substitute another vertex**; R8 fails with the reason when the col is not found or a culmination is not realised. The full rule was checked on the preserved lattices (review §10.4) and is **implemented in the measurement code** (run 7, review §14: lattices bit-equal to run 6, pass readings equal to the Python rule with the exact declared positions; gates unchanged, no synthesis accepted) — **accepted by the owner with caveats; the 150 m radius stays** (window-edge sensitivity is a known property, review §15.1). Never compare against `layout.txt`'s rounded positions — use `layout_polylines.json`. Checks on preserved data that change no code may continue; **a code change, a new run or a Unity run needs a separate authorisation**, and no passing candidate is sought with further heuristics. Checks of that kind on existing data are not LMC code; anything that generates terrain or touches the repository's code is. Its gates and passage rule are fixed by the note, exact metric calculations by a metrics specification the owner approves before calibration, and none is tuned to make a synthesis pass; G3c's research files stay byte-identical (LMC copies what it needs), and G3d waits until a model is promising. Until the owner answers the rest: no production terrain implementation, Stage 1–4A and the frozen Stage 3A–3E stay unchanged, the tier table of `WORLD_RELIEF_PRINCIPLES.md` §1 (its T1 row now read as the mountain system, GEO-2) stays in force for production design, no diagnostic flag or optional path enters the bake, and the production scene is not edited for diagnostics.
- **G3f — regional geography architecture** ([phase plan](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md) v0.5; [99 Open Questions](99%20Open%20Questions.md) GEO-3, GEO-10, GEO-11). **On 2026-10-25 the owner approved the phase (D-1; official ID G3f, working name RGA) and its architecture as the hypothesis to test (D-2):** a seeded landform grammar is instantiated into one authoritative **Geography Plan**; every land vertex has exactly one height owner, a declared drainage unit whose boundaries are divides; relief regions only parameterise the surface law; every landform reserves its write footprint; no landform is applied over another's output; one drainage-consistent synthesis reads the plan; regions divide the problem, tiles only the representation. It is a hypothesis, not an architecture decision (that is the plan's gate G-6), and it changes no Accepted Draft Decision. **Authorised: step A of the plan's §14.1 and the recording of D-3 … D-12** — the plan (v0.3) and its pointers. **Not authorised:** the D-9 documentation package (law 11 in 17 and 14, the Glossary, GEO-11's record), the D-12 package, ~~WP0's read-only checks (step B), the G3f technical specification (step C), any G3f code, terrain generation or Unity run, any held-out variant,~~ anything in production. **On 2026-10-25 the owner authorised an autonomous research mission** (verbatim record `research-data/g3f/gates/mission_authorization_2026-10-25.md`): WP0 to the final report — the specification, research code, Unity runs, calibration and the held-out evaluation within the plan's limits (fixes 6, runs 5, redraws 10, fixed before WP1), commits and pushes to `main`; never production, never a change of an approved decision, never seeds 1005–1024, never the closed S-LM1 / S-LM3 configurations. WP0 is done; the specification [GEOGRAPHY_G3F_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md) (Draft, not approved at G-3) and the research package (`tools/g3f/`, Python; `TerrainGenG3fDump.cs`, Unity steps) are written; held-out seeds 5001–5010 only after the freeze record exists. **Results (2026-10-27/28, specification v0.3 §16):** calibration run 1 qualified no candidate; named fixes F1 (the tree prices climbs of the plan guide, the 35° cone of the planned crests) and F2 (the fixture's reserve moved where the plan leaves its core flat; authored correction 1); run 2 — **GF-RF passed every hard and process gate on all four calibration variants and is frozen** (`research-data/g3f/freeze/FREEZE.md`); held-out 5001–5010: **GF-RF 8 of 10 — part (1) of the pass rule holds**; failures 5003 (an axis mouth segment across a bay — a plan-builder defect, every candidate) and 5006 (plan invalid; F2 costs tall plateaus room); GF-UF 4 of 10, GF-C0 0 of 10. **Part (2), the owner's blind review, has not been held, so no candidate is passed; G-6 (the architecture decision) and G4 (production) are not reached.** Do not re-tune after the held-out result (GF-S7), do not generate 5011–5020 without an invalidating post-freeze change, do not cite GF-RF as an approved architecture, and do not claim cross-machine determinism (one machine). **Decided on content on 2026-10-25 — the plan's decision record quotes the owner's words and the exact texts; it governs, and decisions on content authorise no execution:** D-3 — a changed fluvial law of the S-LM3 family may be *described* in WP1 as a new research line: the closed S-LM3 configuration is not resumed and its code is not used, receivers are built once from the plan, every major divide is declared by the plan, the lateral constraint only lowers and makes no pit or flow across a divide, the criteria and caps are those of the other candidates, and Stage 3 §16 does not apply at the experimental level under that condition; D-4 — candidates C0 and U-F, R-F only after U-F's analytic checks with the purpose "crests set by the plan, synthesis per unit", A1 on both, a tie to the simpler; D-5 — contour A, a 513 × 513-vertex window at 2.734375 m, the agent's fixture with the package's conditions; D-6 — B1 split into a flow gate and a target diagnostic, a reachability-share gate at the recorded 95 % outside declared barrier bands, held-out variants 5 per plateau class, invalid seeded plans redrawn within a cap, calibration 110201 and 1337 only, 1005–1024 not used in G3f, unique G3f labels; D-7 — `main`; D-8 — a global lattice solve for research only, production at G4; D-9 — the new wording of law 11 accepted, **not yet written into 17 or 14** (until the D-9 package is authorised and applied, those documents carry the old text), W.2 deferred; D-10 — a blind review; D-11 — budgets as diagnostics; D-12 — a stale-line package without code changes, not yet authorised. Values left to WP1: the fix, run and redraw caps and the review's M / P / F levels. The S-LM1 line stays stopped; seeds 1005–1024 stay untouched; the plan's terms (Geography Plan, drainage unit, boundary contract, write footprint, stress fixture) stay proposed and get Glossary entries only at G-3.

# Next Design Priority

Current active design layer:

1. Keep World Laws through Vertical Slice coherent while Draft.
2. Design important choices as Decision Node + POI cards naming axes and Approaches.
3. Resolve remaining high-priority Open Questions that block Canonical promotion.
4. Architecture Pivot locked in [16](16%20Architecture%20Pivot.md). Systems map in [17](17%20Game%20Systems%20Architecture.md). Next implementation default: **Overworld V0** (`OVERWORLD_V0_PLAN.md`, iterations W0.1–W0.9) — not OrganicTown C1. Buildings: [15](15%20Procedural%20Building%20Generation.md) Exterior Shell Grammar (sandbox lab parallel OK) + Interior Piece Assembly for Portal buildings only.
5. Stage 3F (prototype E): the documentation stage is under owner review. A dump-first stage 1 — E built and verified through dumps and tests, off the production path — may start only after every **[stage 1]** question in [99 Open Questions](99%20Open%20Questions.md) § Terrain — Stage 3F valley carve is answered; enabling E in the bake waits for the **[production]** questions.
6. Geography-first research (2026-10-18) is under owner review. G0 and G1 are built (GEO-6 (a)); their baseline and captures are the evidence for G2 — the owner's review of the target and of GEO-1, GEO-2, GEO-4 and GEO-7. G3 ran on 2026-10-19 as owner-commissioned research (research record §11): an Editor-only research dump, `TerrainGenG3MountainSystemDump`, that writes candidate lattices outside `Assets` and is never a terrain authority. G3b followed on 2026-10-19 … 20 in the same file (research record §12): a converging valley tree gave a lowland corridor across the range but no Strahler order ≥ 3 network and almost no gain in flat land; the measured limits are the coastal transition (the gated coast-distance ramp) and the construction — the footprint was not varied (corrected 2026-10-20, research record §13.2). G2 was held on 2026-10-20 ([GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md)): target D6 approved, G3c scope set (D8). G3c ran on 2026-10-20 … 21 on the owner's go-ahead (research record §14), Editor-only in four `TerrainGenG3c*` files, isolated the same way: neither contour passed the pre-registered rule (A on 2 of 10 held-out seeds, B on none), B is not a material improvement, so the world is not grown on this evidence. The post-G3c review was recorded on 2026-10-21 ([GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md); research record §15): GEO-1 and GEO-2 resolved, the other GEO questions classified, GEO-9 added, landform-model families compared. The owner chose the pair S-LM3 + S-LM1 on a shared LM2 layout the same day, and the comparison's step 0 is written ([GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md)). The owner then revised the note (v0.2, v0.3 with R5 to designated outlets) and on 2026-10-22 supported it as the research direction without authorising code or calibration. The technical specification ([GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md)) is drafted and checked on G3c's historical lattices (10 of 13 pre-registered checks held; corrections K1–K3). The owner approved that specification (its §12 items 1–4) and the design note (v0.5, as the research plan) the same day, and kept LMC as the working name. The Step 1 readiness check ([GEOGRAPHY_LMC_STEP1_READINESS.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md)) reconciles plan, specification and metrics (findings F1–F4) and lists the conditions C1–C9; C2–C5 were done on 2026-10-23 (A1, A3 and the whale-back option (b) approved — specification v0.4; calibration reference recorded). C1 (ID **G3e**), C8 and C9 were decided the same day. The owner authorised Step 1 on 2026-10-23; after five calibration runs no synthesis is accepted ([GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md)). Diagnostic run 6 and run 7 (M-1 in the measurement code) followed; S-LM3 is closed as a negative result. Run 8 implemented S-LM1's ramp rule (b) (review §16: its target met, its check failed for causes outside the ramp). The owner kept rule (b); the plateau-placement note is review §17; run 9 implemented its option (b) (review §18). The owner kept (b); the recording run 10 for the mountain stage is pre-registered (review §19). Run 10 attributed them (review §20); the options are in review §21; run 11 implemented T1 (review §23); **Step 1 ended not ready** ([report](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md)). The owner accepted the report and closed Step 1 (2026-10-24). Next: only if the owner plans one, a follow-up stage with its own plan (a layout amendment, S-LM1 valley placement, R-c, a changed S-LM3 law); G3d waits for a promising model. The owner approved the plan of the post-mortem checks G3e-PM on 2026-10-24 ([pre-registration](unity/Assets/Plans/GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md)); they run only on a separate authorisation and decide which of those follow-ups, if any, is worth a plan. *They ran the same day:* H failed and the owner stopped the S-LM1 line, so the S-LM1 follow-ups above (layout amendment, valley placement, R-c) are closed; what remains, each only with its own plan, is revisiting the landform families or a changed S-LM3 law. Any production terrain design still waits for GEO-3's production placement (G4) and GEO-8's contract. On 2026-10-25 the owner approved the phase **G3f** — regional geography architecture ([phase plan](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md)) — and its architecture as the hypothesis to test; only step A (the plan and its pointers) is authorised and D-3 … D-12 are open. *The same day* the owner decided D-3 … D-12 on content (the plan's decision record, v0.3); recording them was authorised, executing them was not. Next, each only on its own authorisation: the D-9 and D-12 documentation packages, WP0 (read-only checks on preserved data, in the scope D-3, D-5 and D-6 set), then the G3f technical specification (WP1); no G3f code before the specification is approved (gate G-3). *Superseded the same day by the owner's research mission:* WP0 to the held-out evaluation ran 2026-10-25 … 28 (specification §16; GF-RF frozen, held-out part (1) 8 of 10). Next: the owner's blind review of the sealed sheets (G-5a/G-5b), then G-6; production only at G4.

Do not start combat trees, quest lines, faction lore identities, or full economy simulation before the current Draft foundations are stable enough to instantiate.

After each completed documentation step, commit and push so git preserves the design history.

<!-- lean-ctx -->
## lean-ctx

lean-ctx is active — the MCP tools replace native equivalents.
Full rules: LEAN-CTX.md (open on demand — do not auto-load).
<!-- /lean-ctx -->
