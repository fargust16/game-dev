# 16 Architecture Pivot

Version: 0.16

Status: Draft

Last Updated: 2026-11-12 (§2: the Aimed Walk, the one exception to facing where it goes, by the owner's answer to VS-ALARM (h). Earlier: the grid reaches 30 cells in the slice, by the owner's direction after the play test of 2026-11-12. Earlier: §2: the owner revokes the camera-facing body; the character faces where it goes and never walks backward or sideways. Earlier: §2: the eight-way movement as built (AN2): a sprint backward is a run, and the sneak and the swim turn the body to where it goes, as their clips go forward only. Earlier: 2026-11-11: §2: the character faces the camera and moves in eight directions, by the owner's decision. Earlier: the Vertical Slice is an owner-approved exception to the seeded Overworld: one hand-built scene. Earlier: the Combat Grid test is passed (CC-33). Earlier: the Combat Grid is built during a fight; the slice uses 20 cells, with 50 planned. Earlier: transparent walls detailed from the lab. Earlier: the Combat Grid follows the player. Earlier: Combat Grid reach set to 20 cells in every direction. Earlier: the Combat Grid forms around the player; interiors stay separate scenes; door and camera rules recorded. Earlier: what failed in 2026-08 stated exactly; the world lives between rounds; the door question added to the technical test. Earlier: owner replaced the Combat Arena with combat in place on a Combat Grid.)

Depends On:
[00 Vision](00%20Vision.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)

Referenced By:
[AGENTS.md](AGENTS.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), future overworld / interior / combat micro-plans

---

# Purpose

This document is the **spatial and presentation authority** after the 2026-08-07 architecture pivot.

It answers:

> How do we split the game into AI-shippable products — seeded overworld, free third-person exploration, separate interiors, and turn-based combat in place — without abandoning Decision Engine, Approaches, or Curiosity-first design?

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
| **Overworld** | Seeded Valheim-like zones/biomes/POIs/creatures; smaller than Valheim OK; **no** tree/rock harvest as core loop. Exception, owner's decision of 2026-11-04: the Vertical Slice is one hand-built scene, a small city and its forest, assembled from a modular kit ([11](11%20Vertical%20Slice.md)) |
| **Movement / camera** | Free third-person, Genshin-like locomotion feel (not feature parity); no top-down tactical overworld |
| **Interiors** | Separate scenes, like a change of level in Fallout; free third-person outside Confront; their own Combat Grid during it |
| **Confront** | Runs **in place**, in the scene where it starts; Fallout 1/2 / BG3 turn feel; free movement returns with residue |
| **Grid** | A square **Combat Grid** forms around the player during Confront and is gone afterwards; no grid truth outside Confront or away from the player |
| **Voluntary TB outside combat** | **Removed** |

## Cancelled locks (pre-pivot)

- Continuous grid-authored world as gameplay truth
- Grid A* Free Exploration as default overworld move
- Continuous enterable OrganicTown interiors + in-world HideWalls as primary path
- Manual Turn-Based Mode outside Confront

---

# Combat in place (owner decision, 2026-11-01)

The Combat Arena is cancelled. When Confront starts, the place the player stands in becomes the battlefield: the space around the player turns into grid cells, as in Baldur's Gate 3 or Fallout 2. Characters move over those cells, take cover behind walls and surfaces, hide in vegetation, and can leave the fight. A whole city can be a battlefield, with the rest of it simulated.

This restores one lock the 2026-08-07 pivot cancelled, in-location Turn-Based Mode. The others stay cancelled: there is no grid truth outside Confront, buildings stay separate interior scenes, and exploration stays free third-person.

The record of why the pivot cancelled it: the stack was a permanent grid baked over one hand-tuned town, with enterable buildings in the same scene. Its known failures were "air walls" where hidden walls kept their cells, two truths for visuals and gameplay, and a `GridMap` bake that did not scale past one district ([13 City Generation](13%20City%20Generation.md)). None of this measured a grid over terrain, so it is not evidence against one.

During Turn-Based Mode the rest of the world stands still and lives one tick at the end of each round.

The Combat Grid forms only around the player, for everyone taking part there. In the slice it reaches 30 cells in every direction, a diameter of 60 with the player at the centre. It follows the player every step. A fighter outside it drops into simulation, and one who comes inside appears on it from the next round. Fights the player cannot see have no grid and are not rendered: they are simulated, tick by tick.

The grid is built during a real fight, never ahead of it. That is cheaper than a grid for a whole city ([test](unity/Assets/Plans/COMBAT_GRID_TECH_TEST.md)). The slice used 20 cells and grew to 30 by the owner's direction after the play test of 2026-11-12, so that everyone within 30 cells is in a fight ([10.2](10.2%20Combat%20System%20Draft.md) §9); the plan for later is 50 cells, about 50 m, as a measure of how far a person sees. Everything beyond that is simulated.

Interiors stay separate scenes. A fight carries through a door the way a change of level does in Fallout:

- Enemies follow the player into a building only if, by their target rules, the player is still their priority: no other enemy nearby and the door is the nearest valid way to the target.
- While no enemy is inside with the player, time there runs in ordinary in-game minutes. If an enemy comes in, the fight resumes on the interior's own Combat Grid.
- NPCs do not go into buildings on their own during a fight. Attackers and defenders do not hide in them: they leave beyond the city instead, and a defender who is inside comes out and joins the fight.

Around a fight in the street, the camera can rotate. Walls that block the view become transparent, with only their outlines shown and without revealing the interior behind them. In the lab a whole building turns into its foundation and wall outlines, because cutting a hole in one shows the empty shell inside.

The [technical test](unity/Assets/Plans/COMBAT_GRID_TECH_TEST.md) holds, and the owner accepted it (CC-33).

# Scene Graph

```text
Overworld (seeded biomes, free 3P)
    │
    ├── door / portal ──► Interior Scene (free 3P, own Combat Grid in Confront)
    │                         │
    │                         └── exit ──► Overworld (or nested interior later)
    │
    └── Confront ──► Combat Grid around the player (TB)
                              │
Interior ── Confront ─────────┘
                              │
                              └── win / flee / spare / die ──► free movement + residue
```

Rules:

1. Turn-Based Mode runs only during Confront.
2. A Combat Grid forms around the player from the scene's real geometry for cover and movement. Fights elsewhere are simulated without a grid.
3. A character can leave the fight by moving away.
4. A fight can carry through a door into an interior scene, under the rules of the section above.
5. When Confront ends, free movement returns with residue applied ([08](08%20Confront%20Constraints.md)).

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
- **The character faces where it goes** (owner's decision, 2026-11-12, revoking that of 2026-11-11): the keys choose a way relative to the camera, the body turns to it, at 900°/s (authoring default), and walks, runs or sprints face first; it never walks backward or sideways, and the camera only looks. Standing still, the body keeps the way it last faced. The one exception is the **Aimed Walk** (owner, 2026-11-12; [10.2](10.2%20Combat%20System%20Draft.md) §5e): while a firearm is aimed, the body faces the aim and walks any way, its side and back clips playing. *History:* from 2026-11-11 the body faced the camera and moved in eight directions, each with its own clip, turning in place when the camera turned past it (AN2); the owner found it wrong in play and revoked it. The side and back clips stay in the player's controller, unplayed.
- Not feature parity with Genshin (no full elemental kit, climb-everything V1, etc.).

## Exploration interacts

World interacts (doors, examine, talk) are **diegetic** continuous-space actions — not grid cell Environment Actions.  
Environment Actions apply on the Combat Grid during Confront ([10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).

---

# 3. Interior Scenes

## Lock

- Entering a building / dungeon mouth **loads a separate scene** (or equivalent isolated space).
- Movement stays free third-person, with no grid outside Confront.
- Interior content is keyed by POI / building id + seed (templates or later DNA).
- Thematic match to the exterior / district mood.

## Relation to doc 15

[15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) reframes toward:

- overworld **exterior shells / landmarks**;
- **interior scene** generation recipes;

not continuous Room Graph → in-world GridMap bake as the primary product.

---

# 4. Combat in Place

## Lock

When Confront locks in:

1. A Combat Grid forms around the player from the scene's geometry.
2. Run the AP-based combat system defined in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md) on that grid.
3. Characters can take cover, hide, and leave the fight.
4. Resolve win / Break Away / Yield-Spare / death.
5. Return to free movement; apply residue (no XP-from-kills — [08](08%20Confront%20Constraints.md) still binds).

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
| [08](08%20Confront%20Constraints.md) | Constraints stay; presentation = combat in place |
| [10.2](10.2%20Combat%20System%20Draft.md) | Initiative + AP combat economy on the Combat Grid |
| [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md) | Combat Grid + Environment Actions |
| [18](18%20World%20Time%20and%20Living%20Simulation.md) | Shared world clock and living-world simulation |
| [11](11%20Vertical%20Slice.md) | Prove a hand-built slice scene → interior → combat in place |
| [12](12%20Engine%20Choice.md) | Free 3P + Combat Grid architecture |
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
| 4 | **Combat Grid V0** | Confront trigger → grid over the current scene → TB skeleton → free movement + residue stub |
| 5 | Later | Richer biomes, interior DNA, monster tables, Vertical Slice content on new rails |

Each ticket should get its own micro-plan before code.

---

# Documentation Review

- **Vision / Pillars:** Compatible — Curiosity, priced power, living world remain; only presentation splits.
- **Confront constraints:** Compatible — combat in place is presentation; residue / no XP / alternatives remain.
- **Solo / AI shipability:** Overworld and interiors stay isolated. Combat in place raised the technical risk, and its test holds (CC-33).
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
