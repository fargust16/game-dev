# GameDev

Documentation-first RPG about curiosity, consequences, and finite living worlds.

Working motto: **Curiosity is stronger than power.**

This repository is currently a design bible. Implementation begins only after the relevant systems are documented.

---

## Start here

1. [AGENTS.md](AGENTS.md) — how AI agents must work in this project
2. [000 Project Rules](000%20Project%20Rules.md) — highest process authority
3. [00 Vision](00%20Vision.md) — why the game exists
4. [90 Glossary](90%20Glossary.md) — shared terms
5. [99 Open Questions](99%20Open%20Questions.md) — unresolved blockers

---

## Document map

### Process

| Doc | Status | Role |
|-----|--------|------|
| [000 Project Rules](000%20Project%20Rules.md) | Canonical | Process, hierarchy, validation |
| [001 Documentation Standards](001%20Documentation%20Standards.md) | Canonical | Document structure |
| [002 AI Collaboration Guide](002%20AI%20Collaboration%20Guide.md) | Canonical | AI design duties |
| [AGENTS.md](AGENTS.md) | Draft | Agent operating manual |

### Foundation

| Doc | Status | Role |
|-----|--------|------|
| [00 Vision](00%20Vision.md) | Canonical | Core philosophy |
| [00.1 Core Loop](00.1%20Core%20Loop.md) | Canonical | Loop inside one character life |
| [00.2 Meta Loop](00.2%20Meta%20Loop.md) | Canonical | Legacy, death, finite worlds |
| [00.5 Design Pillars](00.5%20Design%20Pillars.md) | Canonical | Eight non-negotiable pillars |
| [01 Player Experience](01%20Player%20Experience.md) | Canonical | Intended emotional path |

### Active system drafts

| Doc | Status | Role |
|-----|--------|------|
| [02 World Laws](02%20World%20Laws.md) | Draft | World Engine, Balance, Symptoms, Collapse |
| [03 Decision Engine](03%20Decision%20Engine.md) | Draft | What makes player decisions interesting |
| [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md) | Draft | Practical card for designing one important choice |
| [04 Points of Interest](04%20Points%20of%20Interest.md) | Draft | Timed map POIs, lazy activation, missed-event residue |
| [05 Progression](05%20Progression.md) | Draft | Pathways 9→7, Conduct Progress, External Power Bargains |
| [06 Approaches](06%20Approaches.md) | Draft | Confront, Convince, Threaten, Exchange, Veil, Withdraw |
| [07 Character Life](07%20Character%20Life.md) | Draft | Death Gap, Factionless successors, Legacy Cache, Soft Memory |
| [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md) | Draft | Attribute/skill growth, effective preparation, secondary modifiers, Luck, and three-outcome checks |\n| [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md) | Draft | Derived resources, combat values, movement, carrying load, and contextual capabilities |
| [08 Confront Constraints](08%20Confront%20Constraints.md) | Draft | Hard fence around combat so it cannot become the whole game |
| [09 Information Sources](09%20Information%20Sources.md) | Draft | Boards, papers, bartenders, rumours, traces — partial discovery |
| [10 Turn Action Economy](10%20Turn%20Action%20Economy.md) | Draft | Move + Action + Bonus for turn-based V1, arenas only |
| [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md) | Draft | Combat Arena grid + vault/climb/window verbs |
| [11 Vertical Slice](11%20Vertical%20Slice.md) | Draft | First playable proof: 1 district, 3 POIs, arena TB combat |
| [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md) | Draft | Full cards for arrest, collectors, soot-stair trace |
| [12 Engine Choice](12%20Engine%20Choice.md) | Draft | Engine locked: Unity URP, free 3P + custom arena grid TB |
| [16 Architecture Pivot](16%20Architecture%20Pivot.md) | Draft | Spatial authority: Valheim-like overworld, Genshin-like free 3P, separate interiors, arena Confront |
| [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) | Draft | Systems map, dependency DAG, micro-iteration backlog |
| [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md) | Draft | Candidate RPG systems, hierarchical map principles, review order, and coverage checklist |
| [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) | Draft | Post-pivot world-gen order; Piece-Based Assembly |
| [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) | Draft | Buildings/interiors via Piece-Based Assembly (small → large) |

### Deprecated

| Doc | Status | Role |
|-----|--------|------|
| [13 City Generation](13%20City%20Generation.md) | Deprecated | Retired OrganicTown scaffold — historical only, not a reuse source |
| [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md) | Deprecated | Retired grid-district greybox — see [11](11%20Vertical%20Slice.md) for the current S0 target |

### Reference

| Doc | Status | Role |
|-----|--------|------|
| [90 Glossary](90%20Glossary.md) | Draft | Terminology |
| [99 Open Questions](99%20Open%20Questions.md) | Draft | Unresolved design questions |

---

## Hierarchy

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
12 Engine Choice
    ↓
13 City Generation (Deprecated)
    ↓
14 World Generation Roadmap
    ↓
15 Procedural Building Generation
    ↓
16 Architecture Pivot
    ↓
17 Game Systems Architecture
    ↓
18 RPG Systems Coverage Draft
    ↓
Future systems
```

`11.2 Milestone S0 Greybox` is Deprecated and sits outside this active chain — see the Deprecated table above.

Lower documents may extend higher ones. They must never contradict them.

---

## Design pipeline

1. Research
2. Design
3. Documentation
4. Implementation

Nothing exists until it is documented.
Canonical documents are the source of truth — not chat history.

---

## Current design stage

Draft foundations in active refinement:

- [02 World Laws](02%20World%20Laws.md) — Influence, Seed, Main Goals, Side Events, Symptoms, Starting Faction, Difficulty
- [03 Decision Engine](03%20Decision%20Engine.md) — five axes; V1 teaches Attention + Approach, then Bargain
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md) — practical design card
- [04 Points of Interest](04%20Points%20of%20Interest.md) — map delivery + Active Windows + lazy activation
- [05 Progression](05%20Progression.md) — 3 Pathways, stages 9→7, churches/entities as Bargains
- [06 Approaches](06%20Approaches.md) — six methods of acting; combat is not the default best
- [07 Character Life](07%20Character%20Life.md) — death gap, factionless later lives, legacy cache
- [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md) — action-based attribute/skill growth and shared probability-check foundation
- [08 Confront Constraints](08%20Confront%20Constraints.md) — combat fence: no XP, diegetic loot, real alternatives
- [09 Information Sources](09%20Information%20Sources.md) — how the player learns without omniscient markers
- [10 Turn Action Economy](10%20Turn%20Action%20Economy.md) — Move + Action + Bonus in turn-based mode
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md) — grid-native world + env verbs
- [11 Vertical Slice](11%20Vertical%20Slice.md) — first playable proof plan
- [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md) — detailed Lower Veil encounter cards
- [12 Engine Choice](12%20Engine%20Choice.md) — Unity locked: free 3P + custom arena grid TB
- [16 Architecture Pivot](16%20Architecture%20Pivot.md) — Valheim-like overworld, Genshin-like free 3P, separate interiors, arena Confront (BG3 / Fallout 1–2 feel)
- [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) — systems map, dependency DAG, micro-iteration backlog
- [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md) — working coverage checklist; attribute/check foundations are documented, with the base success curve next
- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) / [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) — world/settlement/building generation via **Piece-Based Assembly**: small hand-authored pieces snapped small → large (Valheim-style), not the retired [13 City Generation](13%20City%20Generation.md) prototype

Not yet designing as content:

- lore identities
- spell lists
- quest lines
- combat as the default solution
- full economy simulation

Next work: **Overworld V0** per [16 Architecture Pivot](16%20Architecture%20Pivot.md) / [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) and `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` (iterations W0.1–W0.9) — not the archived grid greybox.

---

## Agent rules

- Persistent agent instructions: [AGENTS.md](AGENTS.md)
- Cursor always-on rule: `.cursor/rules/gamedev-design-guardian.mdc`
- Agents must update those rules when the foundation or process changes
