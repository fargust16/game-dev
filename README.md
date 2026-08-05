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
| [08 Confront Constraints](08%20Confront%20Constraints.md) | Draft | Hard fence around combat so it cannot become the whole game |
| [09 Information Sources](09%20Information%20Sources.md) | Draft | Boards, papers, bartenders, rumours, traces — partial discovery |
| [10 Turn Action Economy](10%20Turn%20Action%20Economy.md) | Draft | Move + Action + Bonus for turn-based V1 |
| [11 Vertical Slice](11%20Vertical%20Slice.md) | Draft | First playable proof: 1 district, 3 POIs, TB combat |
| [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md) | Draft | Full cards for arrest, collectors, soot-stair trace |

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
11 Vertical Slice
    ↓
11.1 Vertical Slice POIs
    ↓
Future systems
```

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
- [08 Confront Constraints](08%20Confront%20Constraints.md) — combat fence: no XP, diegetic loot, real alternatives
- [09 Information Sources](09%20Information%20Sources.md) — how the player learns without omniscient markers
- [10 Turn Action Economy](10%20Turn%20Action%20Economy.md) — Move + Action + Bonus in turn-based mode
- [11 Vertical Slice](11%20Vertical%20Slice.md) — first playable proof plan
- [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md) — detailed Lower Veil encounter cards

Not yet designing as content:

- lore identities
- spell lists
- quest lines
- combat as the default solution
- full economy simulation

Next work: Milestone S0 greybox planning, or polish/review the three slice POI cards.

---

## Agent rules

- Persistent agent instructions: [AGENTS.md](AGENTS.md)
- Cursor always-on rule: `.cursor/rules/gamedev-design-guardian.mdc`
- Agents must update those rules when the foundation or process changes
