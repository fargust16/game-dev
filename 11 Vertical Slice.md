# 11 Vertical Slice

Version: 0.4

Status: Draft

Last Updated: 2026-08-06

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [13 City Generation](13%20City%20Generation.md)

Referenced By:
[11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [13 City Generation](13%20City%20Generation.md), Future prototype milestones and first playable implementation planning.

---

# Purpose

This document defines the **Vertical Slice**: the smallest playable piece that proves the game fantasy.

It is not the full game.
It is a proof that the systems feel like this project, not like a generic RPG with stickers.

Success question:

> After 30–60 minutes, does the player say “I want to try another approach / another lead,” rather than “I need more XP”?

# Slice Fantasy

Working title for the slice setting:

**Lower Veil District** — one dense urban district at the edge of legality, medicine, factories, and night disappearances.

The player is the **first character in a new world**, so they receive a Starting Faction soft foothold.

No final lore names are locked here. Placeholder faction labels are scaffolding only.

# Era and Mood

Confirmed tone reference: the industrial-occult city feeling associated with *Lord of the Mysteries* — not a copy of its plot, Pathways, or lore names.

The Vertical Slice should feel like a late **18th–19th century transition world**:

- nobility still matters: counts, barons, dukes, a king / crown presence in the background;
- factories and harmful cheap labour already shape the poor districts;
- clear separation between wealth and poverty in streets, clothes, lighting, and access;
- horse-drawn carriages remain common;
- early modern inventions appear as novelty or uneven progress: early bicycles, the first hints of radio/telegraph-like communication, early steam machines;
- gaslight / soot / fog / workshop smell energy over clean high-fantasy sparkle.

Lower Veil sits on the poor/industrial side of that divide, with richer order pressing down through Wardens and night commerce pressing up through Veinmarket.

Implementation rule: art, props, NPC dress, and Information Source wording must reinforce this era before any mystical reveal.

# Design Goals Proven by the Slice

The slice must prove all of the following:

1. Curiosity / Attention — notice a lead and decide whether to chase it.
2. Multiple Approaches — Confront is not the only profitable answer.
3. Timed POIs — missing a window changes the world.
4. Information Sources — board / bartender / rumour / trace beat omniscient markers.
5. Turn-Based Mode — works in Confront and can be used outside combat.
6. World residue — tomorrow looks different because of today.
7. Priced power tease — at least one Bargain-shaped offer or refusal.
8. Solo scope — one developer can prototype it without a content army.

# Scope In

## World

- 1 district map with a few connected locations:
  - street / alley hub;
  - tavern;
  - apothecary shop front;
  - notice board node;
  - one side street for the night abduction POI;
  - one small aftermath / Trace space.
- 1 World Seed for the slice (fixed seed for prototype reliability).
- Lightweight Symptoms only (patrol mood, shop open/closed, rumour tone).

## Factions

- 2 factions only in the slice:
  - **Wardens** (order / patrol / seizure framing);
  - **Veinmarket** (soft underworld / mixture / night-route framing).
- Player Starting Faction: **both available as a creation choice** for the slice.
- Soft Pathway nudge:
  - Wardens → Open Path lean;
  - Veinmarket → Veiled Path lean.
- Bound Path is only teased by one Bargain offer, not fully implemented as a progression track in the slice.

## Character / Progression

- First-character rules only.
- Character begins Pathless or at Pathway stage **9** if they accept the faction soft initiation in-slice.
- No stage 8/7 completion required for slice success.
- One formula lead or church/entity whisper may appear as a Bargain tease.

## Information Sources

Must include:

- Notice Board;
- Bartender;
- Street Rumour;
- at least one Physical Trace;
- at least one Witness or involved NPC.

Newspaper and Soft Memory are optional stretch goals.

## Points of Interest

Exactly three authored POIs. Full cards live in [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md).

### POI A — Brass-Number Arrest at Harrow & Tincture

- Type: Street Conflict
- Window: Short (until second-shift whistle)
- Axes: Allegiance + Approach (+ Attention)
- Teaches: public fork, Influence residue, non-Confront options
- Wardens vs Apothecary Harrow / Veinmarket pressure; "crowned buyer" hint

### POI B — Eleventh-Bell Collectors

- Type: Street Conflict
- Window: Brief (eleventh bell to ~02:00)
- Axes: Allegiance + Approach (+ Attention/Commitment)
- Teaches: rescue-vs-trail tension, urgency, Turn-Based Confront likely
- Loom Lane mill-girl abductions; links to black tincture / crowned demand

### POI C — Soot Stair Trace

- Type: Trace / Clue
- Window: Standard
- Axes: Attention + Approach (+ Bargain tease)
- Appears after A or B resolves (participated or missed)
- Teaches: world continues; A+B linkage; Physical Trace reading; priced black-drop Bargain

## Approaches and Confront

All six Approaches must appear at least once across the three POIs:

- Confront
- Convince
- Threaten
- Exchange
- Veil
- Withdraw

Turn-Based Mode requirements:

- automatic on Confront;
- manual toggle outside combat in at least the tavern street / alley approach;
- Move + Action + Bonus economy;
- Break Away and Spare/Yield available in the abduction and/or arrest Confront.

## Decision / Teaching Order

1. Early: Attention + Approach via board/bartender → choose a lead.
2. Mid: Allegiance fork in POI A or B.
3. Late tease: one Bargain offer (refuse remains valid).

# Scope Out

Not in the Vertical Slice:

- full 3 Pathway content to stage 7;
- Death Gap and successor characters (explicitly out of this slice);
- Factionless later-life flow;
- Legacy Cache;
- World Collapse;
- economy simulation beyond a couple of prices/access locks;
- many districts;
- newspaper network;
- full church pantheon;
- deep crafting;
- more than 2 factions;
- procedural POI generation (hand-authored only);
- perfect BG3 feature parity.

# Player Journey

Recommended critical path for proving the fantasy:

```text
Arrive in Lower Veil District
        ↓
Read board + talk to bartender
        ↓
Choose POI A or B first (Attention / Commitment)
        ↓
Resolve with any Approach set (not only Confront)
        ↓
See residue next day / later that night
        ↓
Follow POI C Trace
        ↓
Meet one Bargain tease and accept or refuse
        ↓
Stop: slice complete
```

Side optional: do both A and B if windows allow; missing one still completes the slice if residue + Trace teach the lesson.

# Success Criteria

The slice passes if playtesters can honestly say most of:

1. “I saw a lead and chose whether to chase it.”
2. “I could solve something without fighting.”
3. “When I waited, the world changed.”
4. “I wanted to know what the trace meant.”
5. “Fighting felt deliberate in turns, not like a grind.”
6. “The power offer felt risky to take.”

Hard fail conditions:

- Confront is clearly the best reward path;
- markers spoil every mystery from minute one;
- missing a POI feels like empty nothing rather than Aftermath;
- the player asks for XP/level-up as the main missing piece.

# Implementation Milestones

## Milestone S0 — Greybox

Detailed plan: [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)

- district layout blocked out;
- tavern, board, shop front, alley;
- walk / look / talk stubs.

**Implementation note (2026-08-06):** compact courtyard / straight-avenue greybox superseded by organic town growth — see [13 City Generation](13%20City%20Generation.md). Slice POIs remain mandatory anchors in Lower Veil.

## Milestone S1 — Information + POI shells

- board/bartender tips create Hinted/Marked states;
- POI A/B Active Windows tick in world-time;
- unattended resolution stub produces Aftermath flags.

**Implementation note (2026-08-06):** runtime stubs live under `unity/Assets/Scripts/World/` (`WorldClock`, `PoiDirector`, `TipGiver`). Full Decision Nodes remain S2.

## Milestone S2 — Decision Nodes

- POI A and B fully fill Decision Node cards in data;
- Convince / Threaten / Exchange / Veil / Withdraw stubs;
- residue flags change shop/patrol/rumour.

**Implementation note (2026-08-06):** `DecisionNodeCatalog` + `DecisionNodeSession` (keys 1–5 / Esc). Six Approaches covered across A/B options. Confront remains a stub until S3 TB combat. Residue via `DistrictResidue` (labels + Merrin follow-up lines).

## Milestone S3 — Turn-Based Mode

- manual TB toggle outside combat;
- Confront enters TB automatically;
- Move + Action + Bonus;
- one simple enemy type;
- Break Away + Spare/Yield stubs.

## Milestone S4 — Trace + Bargain tease

- POI C from Aftermath;
- one Bargain offer node;
- slice end screen / checkpoint.

## Milestone S5 — Polish pass

- readability of urgency;
- fail/valuable failure checks;
- strip anything that pushes toward grind.

# Content Budget

Keep intentionally tiny:

| Piece | V1 slice count |
|-------|----------------|
| Districts | 1 |
| Factions | 2 |
| Main authored POIs | 3 |
| Named talk NPCs | 6–10 |
| Enemy archetypes | 1–2 |
| Pathway stages required | 0–1 (stage 9 optional) |
| Bargain teases | 1 |

If the budget grows, cut content before cutting systems proof.

# Confirmed Slice Decisions

1. **District:** Lower Veil District kept as the slice setting/placeholder.
2. **Mood/era:** Lord of the Mysteries-like industrial-occult transition (18–19th century social structure + early machines), not a plot/lore copy.
3. **Starting Faction:** both Wardens and Veinmarket are selectable.
4. **Death Gap:** fully out of this Vertical Slice.
5. **Names:** all placeholder names may be renamed later.

# Documentation Review

- **Vision / Player Experience:** Slice targets curiosity, tense choice, agency, residue, priced power tease.
- **POI / Information / Approaches / Turn Economy:** Directly implemented in miniature.
- **Progression / Character Life:** Only first-life foothold and Bargain tease; later-life systems deferred. Death Gap excluded from slice scope.
- **Era mood:** Compatible with priced mystery and social inequality without requiring high-fantasy gloss.
- **Solo scope:** Content budget and milestones are intentionally small.
- **Glossary / Open Questions:** Confirmations recorded as resolved.

# Related Documents

- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [10 Turn Action Economy](10%20Turn%20Action%20Economy.md)
- [09 Information Sources](09%20Information%20Sources.md)
- [99 Open Questions](99%20Open%20Questions.md)
