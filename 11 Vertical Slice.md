# 11 Vertical Slice

Version: 0.9

Status: Draft

Last Updated: 2026-11-01 (one or two Arcana with different styles, the first-day trial, and recipes outside the faction recorded. Earlier: balance and start settled: Stage 8 as the player's limit, 8–10 enemies, the six-hour window, a Factionless start, and three named Pathways. Earlier: rewritten for the owner's one-city slice: the Spirit General's invasion on day 1–2, a ritual the player stops or loses to, one joinable faction with two or three Pathways, and the required mechanics. Earlier: owner redefined the slice as one city at Stages 9–7; retired combat references replaced with 10.2.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md), [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)

Referenced By:
[11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md), Future prototype milestones and first playable implementation planning.

---

# Purpose

This document defines the **Vertical Slice**: the first version of the game and the smallest playable piece that proves its fantasy.

It is not the full game.
It is a proof that the systems feel like this project, not like a generic RPG with stickers.

Success question:

> After one run, does the player say “I want to try another approach / another lead,” rather than “I need more XP”?

A run lasts one to two game days before the invasion plus the fight for the ritual, about 3–6 real hours.

# Slice Story

The owner approved this story on 2026-11-01.

- **Place:** one city, not necessarily the capital, and a small area outside it, enough to gather resources, craft, and make potions.
- **Opening:** the player has one to two game days of ordinary life in the city.
- **Invasion:** after those days the Spirit General comes out in the city with its followers, and the ritual starts at once.
- **The General leaves:** after 15–20 ticks it returns to the Spirit World and leaves the ritual to its followers. It cannot be hunted afterwards.
- **The final fight:** the main battle is against its followers.
- **Ending:** the player either stops the ritual and wins, or the ritual succeeds and the player loses. The character's death also ends the run, and the player starts again.

The slice uses the campaign's rules from [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md): one rule for how a ritual starts and stops, in a world of one city.

## The General in the slice

The preferred version lets a player who feels ready fight the General on a later run and see how strong it is, and even die to it.

If building a Stage 2 opponent is too much for the first version, the General appears, casts a spell without attacking, and leaves after the same 15–20 ticks. This fallback is approved.

## The fight

- 8–10 enemies take part.
- Allies stand with the player, in numbers that make the two sides about equal once the player is counted. The player's presence must change the result.
- The ritual keeps the campaign's six-hour window, one real hour of free exploration. If the player stops it in time, the run is won.

## Stopping the ritual

- Kill everyone conducting it.
- Kill the senior who started it.
- Sneak to the ritual site, or pass dialogue checks through deception, persuasion, or intimidation, while the cult has not fully taken the city.

The conditions differ from run to run.

# Slice Fantasy

The slice takes place in one city. **Lower Veil District** is one of its districts: a dense urban area at the edge of legality, medicine, factories, and night disappearances.

The player is the **first character in a new world**.

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
5. Combat Arena — Confront transitions to a thematic Turn-Based arena and returns with residue ([16 Architecture Pivot](16%20Architecture%20Pivot.md)).
6. Scene split — overworld free 3P + at least one separate interior scene (no grid).
7. World residue — tomorrow looks different because of today.
8. Priced power tease — at least one Bargain-shaped offer or refusal.
9. The war's core — a General, its followers, and a ritual the player can stop in more than one way.
10. Preparation matters — what the player gathered, crafted, brewed, and learned in the first days changes the fight for the ritual.
11. Solo scope — one developer (with AI) can prototype it without a content army.

# Scope In

## Required mechanics

Each of these is implemented in a limited form:

- free exploration of the city and the area outside it;
- gathering;
- Item Creation;
- Alchemy;
- combat;
- dialogue with checks;
- stealth;
- world time;
- sleep.

## World

- Seeded **overworld stub** (Valheim-shaped, small): walkable terrain + biome tint + POI markers ([16](16%20Architecture%20Pivot.md)).
- One city with a small territory outside it. It does not need the 3×3 target world of [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md).
- Lower Veil locations as **exterior POIs / shells** plus **separate interior scenes** where needed:
  - street / alley hub (overworld);
  - tavern (door → interior scene);
  - apothecary shop front (+ optional interior);
  - notice board node;
  - one side street for the night abduction POI;
  - one small aftermath / Trace space.
- 1 World Seed for the slice (fixed seed for prototype reliability).
- Lightweight Symptoms only (patrol mood, shop open/closed, rumour tone).
- Pre-pivot continuous OrganicTown grid district is **not** the slice delivery target.

## Factions

- **One joinable faction:** the internal-order faction of [02 World Laws](02%20World%20Laws.md), the hidden police of practitioners.
- **Wardens** (order / patrol / seizure framing) and **Veinmarket** (soft underworld / mixture / night-route framing) stay as local groups.
- **The cult** appears through the Spirit General and its followers, from the Emperor, Empress, and Hierophant Arcana.
- Members of other factions are optional.

## Character / Progression

- The character starts without a faction. On the first day it can pass a trial to join the faction, or stay out and search alone. This is a slice exception to the Starting Faction rule of [07 Character Life](07%20Character%20Life.md).
- Stage 9 is a certain goal in a run. Stage 8 takes directed effort. Stage 7 is for NPCs only. Allies and enemies may be deeper.
- The slice offers one or two potions, so that a run has two styles of play: one through dialogue and stealth, the other through confrontation ([05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md)).
- A character outside the faction can find potion recipes too, including the cult's, with a somewhat different approach.
- Each Limit grants two abilities. An NPC at Limit 7 has about six, the same abilities the player would have.

## The first-day trial

The slice has no backstory: the character has come of age and decides to become an adventurer. Someone offers it a job, or it overhears talk of an investigation into a disappearance. This is the trial.

The character starts with almost nothing, so fighting is hard at first. It can find a weapon and try force, or use attention and dialogue. It learns about supernatural forces during the trial or later.
- One Bargain offer remains as a priced tease.

## Information Sources

Must include:

- Notice Board;
- Bartender;
- Street Rumour;
- at least one Physical Trace;
- at least one Witness or involved NPC.

Newspaper and Soft Memory are optional stretch goals.

## Points of Interest

The three authored POIs of the first days stay as side events. Full cards live in [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md).

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
- Window: until the invasion
- Axes: Attention + Approach (+ Bargain tease)
- Appears after A or B resolves (participated or missed)
- Teaches: world continues; A+B linkage; Physical Trace reading; priced black-drop Bargain

### The invasion

- Type: Operation Echo
- Window: the ritual's window
- Axes: Attention + Approach + Commitment
- Teaches: the world's threat, preparation, and more than one way to stop a ritual

## Approaches and Confront

All six Approaches must appear at least once across the slice:

- Confront
- Convince
- Threaten
- Exchange
- Veil
- Withdraw

Turn-Based Mode / arena requirements:

- Confront always loads a **thematic Combat Arena** (not in-location TB);
- no voluntary Turn-Based Mode during free exploration;
- the Initiative and AP economy of [10.2](10.2%20Combat%20System%20Draft.md) on the arena only;
- Break Away and Spare/Yield available in the abduction and/or arrest Confront;
- at least one Theme Pack used (e.g. street or alley).

## Decision / Teaching Order

1. Early: Attention + Approach via board/bartender → choose a lead.
2. Mid: Allegiance fork in POI A or B.
3. Late tease: one Bargain offer (refuse remains valid).
4. Final test: the invasion asks the player to use what they prepared.

# Scope Out

Not in the Vertical Slice:

- Enchanting;
- Death Gap and successor characters;
- Legacy Cache;
- Pathway change;
- Factionless later-life flow;
- several cities, City Control between cities, and the Capital Gate;
- manifestation and World Collapse;
- economy simulation beyond a couple of prices/access locks;
- newspaper network;
- full church pantheon;
- procedural POI generation (hand-authored only);
- perfect BG3 feature parity.

# Player Journey

Recommended critical path for proving the fantasy:

```text
Arrive in the city
        ↓
Read board + talk to bartender; gather, craft, brew, join the faction or not
        ↓
Choose POI A or B first (Attention / Commitment)
        ↓
Resolve with any Approach set (not only Confront)
        ↓
See residue next day / later that night
        ↓
Follow POI C Trace; meet one Bargain tease and accept or refuse
        ↓
Day 1–2: the Spirit General comes out in the city and the ritual starts
        ↓
15–20 ticks later the General leaves
        ↓
Fight, sneak, or talk through the followers to the ritual
        ↓
Stop the ritual and win, or lose to it
```

Side optional: do both A and B if windows allow; missing one still completes the run if residue + Trace teach the lesson.

# Success Criteria

The slice passes if playtesters can honestly say most of:

1. “I saw a lead and chose whether to chase it.”
2. “I could solve something without fighting.”
3. “When I waited, the world changed.”
4. “I wanted to know what the trace meant.”
5. “Fighting felt deliberate in turns, not like a grind.”
6. “The power offer felt risky to take.”
7. “What I prepared changed how the ritual fight went.”
8. “I want to try stopping the ritual another way.”

Hard fail conditions:

- Confront is clearly the best reward path;
- markers spoil every mystery from minute one;
- missing a POI feels like empty nothing rather than Aftermath;
- the player asks for XP/level-up as the main missing piece.

# Implementation Milestones

## Milestone S0 — Greybox / post-pivot stub

Detailed plan: [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md) (historical grid-district notes) + [16 Architecture Pivot](16%20Architecture%20Pivot.md) backlog.

Post-pivot S0 target:

- seeded overworld stub + free 3P walk;
- one interior scene (tavern);
- board / talk stubs on overworld or interior;
- POI shell markers.

**Implementation note:** Pre-pivot OrganicTown / CityGenerator greybox remains in `unity/` as prototype archive, not the delivery target.

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

## Milestone S3 — Combat Arena

- Confront loads a thematic Combat Arena;
- the Initiative and AP economy of [10.2](10.2%20Combat%20System%20Draft.md) on the arena grid only;
- one simple enemy type;
- Break Away + Spare/Yield stubs;
- return to source scene with residue stub.
- no voluntary TB outside arenas.

## Milestone S4 — Trace + Bargain tease

- POI C from Aftermath;
- one Bargain offer node.

## Milestone S5 — Preparation loop

- gathering, Item Creation, and Alchemy in a limited form;
- sleep and the shared clock across the first days;
- the joinable faction, its trial, and the slice's Arcana.

## Milestone S6 — Invasion

- the Spirit General comes out on day 1–2 and leaves after 15–20 ticks;
- the ritual site, its guards, and its senior;
- 8–10 enemies and allied groups that keep the sides about equal;
- the ways to stop the ritual, including stealth and dialogue checks;
- win and loss endings.

## Milestone S7 — Polish pass

- readability of urgency;
- fail/valuable failure checks;
- strip anything that pushes toward grind.

# Content Budget

Keep intentionally tiny:

| Piece | V1 slice count |
|-------|----------------|
| Cities | 1 |
| Districts with authored POIs | 1 (Lower Veil) |
| Joinable factions | 1 |
| Local groups | 2 |
| Main authored POIs | 3 + the invasion |
| Named talk NPCs | 6–10 |
| Enemy archetypes | 2–3 |
| Generals | 1 |
| Player Arcana | 1–2 |
| Player stages | 9–8, with Stage 7 for NPCs |
| Bargain teases | 1 |

If the budget grows, cut content before cutting systems proof.

# Confirmed Slice Decisions

1. **Place:** one city, with Lower Veil as one of its districts. Its name is open.
2. **Mood/era:** Lord of the Mysteries-like industrial-occult transition (18–19th century social structure + early machines), not a plot/lore copy.
3. **Faction:** only the internal-order faction is joinable, through a first-day trial; Wardens and Veinmarket are local groups.
4. **Timing:** the Spirit General comes out after one to two game days.
5. **Death Gap, Legacy Cache, Pathway change, and Enchanting:** out of this Vertical Slice.
6. **Names:** all placeholder names may be renamed later.

# Documentation Review

- **Architecture Pivot:** Slice delivery follows overworld → interior → arena rails.
- **Vision / Player Experience:** Slice targets curiosity, tense choice, agency, residue, priced power tease.
- **POI / Information / Approaches / Combat:** Directly implemented in miniature (TB on arenas only).
- **Progression / Character Life:** One Factionless life per run. The player reaches Stages 9–8; Stage 7 is for NPCs, and allies and enemies may be deeper. Later-life systems are excluded.
- **Cult Campaign:** The slice uses the campaign's ritual rules in one city ([21](21%20Cult%20Campaign%20and%20City%20Control.md)).
- **Era mood:** Compatible with priced mystery and social inequality without requiring high-fantasy gloss.
- **Solo scope:** The required mechanics are many, so each stays in a limited form, and content stays small. The open items are CC-29 and CC-32 in [99 Open Questions](99%20Open%20Questions.md).
- **Glossary / Open Questions:** Confirmations recorded as resolved.

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md)
- [09 Information Sources](09%20Information%20Sources.md)
- [99 Open Questions](99%20Open%20Questions.md)
