# 16 Architecture Pivot

Version: 0.3

Status: Draft

Last Updated: 2026-08-09

Depends On:
[00 Vision](00%20Vision.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)

Referenced By:
[AGENTS.md](AGENTS.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), future overworld / interior / arena micro-plans

---

# Purpose

This document is the **spatial and presentation authority** after the 2026-08-07 architecture pivot.

It answers:

> How do we split the game into AI-shippable products — seeded overworld, free third-person exploration, separate interiors, and arena combat — without abandoning Decision Engine, Approaches, or Curiosity-first design?

Chat is not the source of truth. If older docs contradict this pivot on presentation/space, **this document wins** until they are updated (they should be updated in the same documentation transaction).

Success question for agents:

> Can a new agent open this file and know which three scene types exist, what movement/combat rules apply in each, and what to implement next — without continuing the pre-pivot continuous OrganicTown path?

---

# Why This Pivot

The pre-pivot path tried to make **one continuous hybrid world**:

- grid truth everywhere;
- enterable buildings in the same scene;
- Turn-Based Mode in-location and outside combat;
- OrganicTown + GridMap bake + occlusion in one stack.

That stack is too expensive for solo + AI-driven development. Failures (invisible walk blockers, dual visual/gameplay truth, town + combat + interiors entangled) proved the cost.

**New goal:** cheaper, isolated systems the AI can finish one ticket at a time; the human reviews and steers.

Philosophy (`00`–`07`, Decision Engine, Approaches as choices, Confront *constraints*) stays.  
**Presentation and spatial locks change.**

---

# Locked Decisions

| Layer | Lock |
|-------|------|
| **Overworld** | Seeded Valheim-like zones/biomes/POIs/creatures; smaller than Valheim OK; **no** tree/rock harvest as core loop |
| **Movement / camera** | Free third-person, Genshin-like locomotion feel (not feature parity); no top-down tactical overworld |
| **Interiors** | Separate scenes; free third-person; **no grid** |
| **Confront** | Load a **thematic Combat Arena** scene; Fallout 1/2 / BG3 turn feel; return with residue |
| **Grid** | **Arena-only** spatial truth |
| **Voluntary TB outside combat** | **Removed** |

## Cancelled locks (pre-pivot)

- Continuous grid-authored world as gameplay truth
- Grid A* Free Exploration as default overworld move
- Continuous enterable OrganicTown interiors + in-world HideWalls as primary path
- In-location Turn-Based Mode / no arena teleport
- Manual Turn-Based Mode outside Confront

---

# Scene Graph

```text
Overworld (seeded biomes, free 3P)
    │
    ├── door / portal ──► Interior Scene (free 3P, no grid)
    │                         │
    │                         └── exit ──► Overworld (or nested interior later)
    │
    └── Confront lock ──► Combat Arena (grid TB)
                              │
Interior ── Confront lock ───┘
                              │
                              └── win / flee / spare / die ──► return to source scene + residue
```

Rules:

1. Overworld and interiors never enter Turn-Based Mode.
2. Confront always transitions into an arena scene (load or additive — implementation choice later).
3. The arena must be **thematically similar** to the source location (street, tavern, cellar, woods…), not a perfect geometry clone of the exploration scene.
4. On arena end, restore the player to the source scene with Confront residue applied ([08](08%20Confront%20Constraints.md)).

---

# 1. Overworld Generation (Valheim-shaped, scoped)

## Intent

Procedural, seed-relative world with biome diversity, creature presence, and placeable structures/POIs — optimized like a Valheim-style streamed world, **not** a survival crafting clone.

## V1 scope

| Include | Exclude |
|---------|---------|
| World seed → height / biome bands | Full Valheim-scale map |
| Chunk / zone streaming intent | Tree/rock harvest progression |
| POI markers (ruin, camp, shrine, town approach, cave mouth) | Continuous enterable city fabric in overworld |
| Exterior shells / landmarks | In-overworld grid pathfinding as truth |
| Creature / encounter spawn tables | Deep base-building |

## Pipeline (design)

```text
World Seed
  → height / moisture / biome map
  → zone / chunk layout
  → POI & structure placement
  → creature spawn tables per biome
  → portal / door links to Interior Scene ids
```

Detail roadmap evolves in [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) under the **post-pivot** track. Pre-pivot OrganicTown continuous work is prototype archive only.

---

# 2. Locomotion (Genshin-like feel)

## Lock

- Third-person free movement on overworld and in interiors.
- Follow / orbit camera; mobile, dynamic feel as the target reference.
- Not feature parity with Genshin (no full elemental kit, climb-everything V1, etc.).

## Exploration interacts

World interacts (doors, examine, talk) are **diegetic** continuous-space actions — not grid cell Environment Actions.  
Grid Environment Actions live only on arenas ([10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).

---

# 3. Interior Scenes

## Lock

- Entering a building / dungeon mouth **loads a separate scene** (or equivalent isolated space).
- Movement stays free third-person; **no grid**.
- Interior content is keyed by POI / building id + seed (templates or later DNA).
- Thematic match to the exterior / district mood.

## Relation to doc 15

[15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) reframes toward:

- overworld **exterior shells / landmarks**;
- **interior scene** generation recipes;

not continuous Room Graph → in-world GridMap bake as the primary product.

---

# 4. Combat Arenas

## Lock

When Confront locks in:

1. Capture source theme (e.g. `StreetFog`, `Tavern`, `Cellar`, `Woods`).
2. Load / generate a **Combat Arena** from that Theme Pack.
3. Run the AP-based combat system defined in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md) on a square grid.
4. Resolve win / Break Away / Yield-Spare / death.
5. Return to source scene; apply residue (no XP-from-kills — [08](08%20Confront%20Constraints.md) still binds).

## Theme Packs (V0 list)

Street · Tavern · Cellar · Woods · Ruin · Factory floor

Arenas are greybox-first. Fidelity = readable mood + cover props, not 1:1 copy of the exploration mesh.

---

# 5. Pre-Pivot Prototype Map (deprecated — not a reuse source)

| Prototype | Status |
|-----------|--------|
| OrganicTown v2 continuous town (A–D2, P0 GridMap) | Deprecated / archive under `unity/` — see [13 City Generation](13%20City%20Generation.md) |
| In-world Building Occlusion / enterable templates | Deprecated, superseded |
| v1 raster `CityGenerator` playable city | Deprecated, historical reference only |
| Grid Free Exploration + voluntary TB | Cancelled |

OrganicTown is not viable (not modular, not optimized, did not read as a real city — [13 City Generation](13%20City%20Generation.md)). Agents must **not** treat “full C1 continuous narrative on OrganicTown sockets” as the default next implementation, and must not reuse its algorithms in the new [15](15%20Procedural%20Building%20Generation.md) Piece-Based Assembly pipeline.

---

# 6. Relation to Other Docs

| Doc | Role after pivot |
|-----|------------------|
| [08](08%20Confront%20Constraints.md) | Constraints stay; presentation = arena |
| [10.2](10.2%20Combat%20System%20Draft.md) | Initiative + AP combat economy on arenas only |
| [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md) | Arena grid + arena Environment Actions |
| [18](18%20World%20Time%20and%20Living%20Simulation.md) | Shared world clock and living-world simulation |
| [11](11%20Vertical%20Slice.md) | Prove overworld stub → interior → arena → return |
| [12](12%20Engine%20Choice.md) | Free 3P + arena grid architecture |
| [13](13%20City%20Generation.md) | Legacy / mood reference |
| [14](14%20World%20Generation%20Roadmap.md) | Post-pivot overworld roadmap |
| [15](15%20Procedural%20Building%20Generation.md) | Exterior shells + interior scenes |
| [17](17%20Game%20Systems%20Architecture.md) | Systems map, interfaces, micro-iteration backlog |

---

# 7. AI Task Decomposition (implementation backlog)

Record only — **do not implement in the documentation pivot pass.**

| # | Ticket | Outcome |
|---|--------|---------|
| 1 | **Overworld V0** | Seed → biome bands + walkable terrain + 3–5 POI markers + simple creature spawn points (no harvest). Detail: `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` + Track W in [17](17%20Game%20Systems%20Architecture.md) |
| 2 | **Locomotion V0** | Third-person controller + follow camera (Genshin-like feel target) |
| 3 | **Interior V0** | One door → load interior scene → exit back; free move |
| 4 | **Arena V0** | Confront trigger → theme-tagged arena → TB skeleton → return + residue stub |
| 5 | **Theme Packs V0** | Street / tavern / woods greybox arena kits |
| 6 | Later | Richer biomes, interior DNA, monster tables, Vertical Slice content on new rails |

Each ticket should get its own micro-plan before code.

---

# Documentation Review

- **Vision / Pillars:** Compatible — Curiosity, priced power, living world remain; only presentation splits.
- **Confront constraints:** Compatible — arena is presentation; residue / no XP / alternatives remain.
- **Solo / AI shipability:** Improved by isolating overworld, interiors, and arenas.
- **Code:** Untouched; `unity/` OrganicTown remains pre-pivot prototype archive, deprecated, not a reuse source for new world/settlement/building systems ([13](13%20City%20Generation.md), [15](15%20Procedural%20Building%20Generation.md)).

---

# Related Documents

- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md)
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md)
- [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)
- [AGENTS.md](AGENTS.md)
