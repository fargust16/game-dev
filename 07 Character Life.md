# 07 Character Life

Version: 0.10

Status: Draft

Last Updated: 2026-11-02 (9 free points and three archetypes set. Earlier: every character starts Uninitiated and Factionless; attribute start and Character Archetypes recorded; the Starting Faction retired. Earlier: terms renamed to Arcana and Limit. Earlier: Last Free City Rule recorded. Earlier: manifestation event and campaign closure reconciled with 02; Death Gap covers the war for cities. Earlier: player death permanently ends same-world succession after the Stage 0 being manifests.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md)

Referenced By:
[08 Confront Constraints](08%20Confront%20Constraints.md), [19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md), future character creation, death flow, legacy cache, and dialogue-memory design.

---

# Purpose

This document defines one Character Life: how a character begins, dies, leaves residue, and is followed by another life in the same world or by a new world.

It answers:

> What happens after death, and how does the next character enter without turning death into a soft reload?

# Central Rules

1. Character death ends that Character Life permanently.
2. Character power, Limit, and personal inventory do not continue as a living hero.
3. Before the sealed Limit 0 being manifests in the living world, the world advances during a fixed short Death Gap and may receive a successor, provided at least one uncaptured city has no battle in it (the Last Free City Rule).
4. Once the being manifests in the living world, player-character death is terminal defeat for that world’s playable story; no successor appears, even if the being is later defeated.
5. Player Knowledge survives.
6. Ordinary attributes and skill development belong to the deceased Character Life and do not transfer to a successor.
7. Legacy may be recovered, but ordinary leftovers are never fully guaranteed.
8. A successor is a new person in a changed world, not a respawn.

# Character Start

Every character, in the slice and in the full game, starts **Uninitiated** and **Factionless**. The owner decided this on 2026-11-02, and it retires the Starting Faction, which went to the first character of a world.

## Attributes at the start

All seven attributes start at 1. The player then does one of two things, once in a life:

- spends 9 free points by hand on any attributes;
- or chooses a **Character Archetype**, a preset spread in a fixed order that gives slightly more, 11 points.

Three archetypes are offered at creation:

| Archetype | Added to the starting 1 | Who it is |
|---|---|---|
| Боец | Сила +5, Выносливость +4, Ловкость +2 | Lives by the body |
| Сыщик | Восприятие +5, Интеллект +4, Ловкость +2 | Lives by noticing |
| Оборванец | Воля +5, Удача +4, Ловкость +2 | Survived the street on stubbornness and luck |

The 9 points and the three names are the owner's. The 11 points and the spreads are authoring defaults.

## Character Archetypes in the full game

This is a direction for later, not V1:

- Archetypes are assembled from templates by seed, so they are not a fixed list.
- An archetype is a backstory and attributes, and it may add a faction, an Arcana, or both.
- A new world offers at most Limit 9.
- The longer a world has lived, and depending on its state, the deeper the Limits an archetype may carry.
- Not every archetype has a Limit. Some only give higher attributes.

# Death

When the character dies, the Character Life and its personal Arcana power end. The body, carried items, open actions, and local scene become world residue; no successor automatically inherits the former character’s carried/equipped items or extracted modifier instances.

The next step depends on the state of the primary opposing being:

- **Before its return to the living world:** a fixed Death Gap begins, world advancement resolves without player control, and the player may create a new character for the same world or start a new world.
- **When no uncaptured city is free of battle:** there is nowhere to return, so the death is final, even before manifestation. This is the Last Free City Rule ([21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)).
- **After it has manifested:** the current playable story ends in defeat if the player character dies. There is no successor and no further same-world Character Life, even if the being has since been defeated. The world may continue beyond the player’s point of view.

Death is not an instant reload of the same person. Manifestation is the completion of the Capital Awakening Ritual ([02 World Laws](02%20World%20Laws.md)). The campaign ends with a rating when the being is defeated; whether the surviving current character can continue afterward remains open.

# Death Gap

V1 uses a **fixed short Death Gap** measured in player world-time.

Recommended V1 feel: a small number of days, long enough for the world to move, short enough that the player still feels continuity.

During the Death Gap the systems must recompute world state from the current situation, including at least:

- Active Window expiry and Unattended Resolution for relevant POIs;
- eligible World Engine Operations that fit the elapsed days, including City Capture and City Liberation;
- Faction Influence residue from those resolutions;
- visible Symptoms that the next character can read;
- fate checks on ordinary non-intentional leftovers (body loot, open camps, unprotected items).

Exact day count remains a tuning Open Question. The rule that the gap is fixed and world-resolving is binding.

# New Character After Death

## Factionless

Every character begins **Factionless**.

Factionless means:

- no initial Main Goal briefing from a faction;
- no soft faction mentor door already open;
- joining any faction requires a **Faction Entry** challenge.

This makes Allegiance earned.

## Faction Entry

To join a faction later, the character must pass a Faction Entry:

- V1 form: a short quest / Decision Node chain / POI-backed trial;
- success grants membership and then the normal soft Arcana nudge of that faction;
- failure may close easy entry, worsen opinion, or force another Approach later.

No free faction swap menu.

## Creation choices in V1

When creating a character, the player sets the starting attributes as above and may choose light identity options later defined by content.

System-critical rules for V1:

- starts Factionless;
- starts Uninitiated unless a special documented exception appears later;
- inherits Player Knowledge only;
- can see intentional Legacy Cache markers;
- enters a world already changed by the Death Gap.

# Legacy

## Ordinary residue

As in Meta Loop, ordinary leftovers may include:

- body and carried items;
- camps, open routes, damaged places;
- faction/quest pressure already in motion;
- NPC fates and local rumours.

These remain subject to world fate and are not transferred automatically into the next character's inventory. NPCs may loot, move, sell, or destroy them during the Death Gap or later. The defined way to recover the former character's current equipment is to encounter that former character as a world actor and defeat it; then the per-item NPC equipment-recovery rule in [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) applies.

## Intentional Legacy Cache

V1 supports one simple intentional preparation tool:

**Legacy Cache**

- the living character may create a stash;
- the stash receives a map marker for the next Character Life in this world;
- this exists specifically to keep solo implementation light while still rewarding preparation.

V1 rule:

- the next character can see the Legacy Cache marker;
- the cache is the reliable intentional inheritance channel, separate from anything merely carried or equipped at death;
- it should remain small/limited so it does not become a full gear pipeline that erases death;
- the exact eligible contents, including extracted modifier instances, remain open and are tracked in [99 Open Questions](99%20Open%20Questions.md).

Ordinary unprotected loot remains uncertain. The intentional cache is the player's deliberate Continuity tool.

V2 may add tampering risk, false markers, or rival discovery. V1 keeps the marked cache simple and mostly dependable.

# Soft Memory Between Lives

Characters are not blood-heirs and do not inherit mystical identity.

They are connected by:

1. Player Knowledge;
2. Legacy residue and Legacy Cache;
3. **Soft Memory** in the world.

Soft Memory means NPCs and factions may remember the previous character through:

- rumours;
- reputation fragments;
- dialogue options that reference "the one who..." ;
- opinion shifts if the new character claims kinship, denies it, exploits the legend, or investigates the death.

V1 implementation target: lightweight flags and a few dialogue branches, not a full biography simulator.

Examples of simple V1 hooks:

- an NPC likes the previous rescuer and is warmer to someone investigating their fate;
- a faction hostile to the previous character is suspicious of a newcomer asking the same questions;
- a dialogue choice to invoke or hide the previous name changes local opinion.

This must remain optional content density. The system only requires that Soft Memory can matter, not that every NPC has a paragraph about every past life.

# Same World vs New World

After death the player may:

- continue in the same world after the Death Gap;
- or begin a new world if they choose / if the current world has collapsed.

Same world emphasises Legacy, Soft Memory, and unfinished pressures.
New world emphasises Player Knowledge without the same local residue.

# Relationship to Other Systems

| System | Interaction after death |
|--------|-------------------------|
| World Engine | Advances during Death Gap |
| Points of Interest | Many expire or resolve unattended during Death Gap |
| Progression | Limit and ordinary character-specific attribute/skill development end with the character; methods remain as Player Knowledge |
| Approaches | Soft Memory and Faction Entry are often Convince / Exchange / Threaten / Veil problems |
| Decision Engine | Investigating a previous life is Attention + Allegiance + Commitment |

# Solo Scope

## V1

- Every character: Uninitiated and Factionless until Faction Entry quest/node.
- Fixed short Death Gap with world resolution pass.
- One intentional Legacy Cache with next-life map marker.
- Lightweight Soft Memory flags for rumours / a few dialogue opinion hooks.
- No bloodline power inheritance.

## V2

- Richer Soft Memory and faction legend states.
- Possible tampering or discovery risk around Legacy Caches.
- More varied Faction Entry trials by faction.

## V3

- Multi-life district legends.
- Cross-life investigation chains with delayed Hidden consequence reveals.
- Still no “same hero respawn.”

# Design Validation

After death, the player should feel:

1. loss of the person, not erasure of the story;
2. curiosity about what the world did in the gap;
3. desire to recover, reinterpret, or abandon Legacy;
4. that a foothold must be earned;
5. no urge that reload-as-same-person would have been the real design.

# Documentation Review

- **Meta Loop:** Compatible. Fixed gap, continued world, imperfect ordinary legacy, Knowledge survival.
- **World Laws:** Compatible if Death Gap runs Operations / POI resolutions. The Starting Faction is retired.
- **Progression:** Compatible. Power dies with life; faction membership and Arcana nudge are earned in every life.
- **Decision Engine / Approaches:** Compatible. Faction Entry and Soft Memory create new decision surfaces.
- **Glossary / Open Questions:** Updated for Death Gap, Factionless, Faction Entry, Legacy Cache, Soft Memory.

# Related Documents

- [00.2 Meta Loop](00.2%20Meta%20Loop.md)
- [02 World Laws](02%20World%20Laws.md)
- [05 Progression](05%20Progression.md)
- [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
