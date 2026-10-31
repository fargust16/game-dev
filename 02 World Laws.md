# 02 World Laws

Version: 0.5

Status: Draft

Last Updated: 2026-10-31

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md)

Referenced By:
[03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [07 Character Life](07%20Character%20Life.md), Future world, faction, consequence, progression, and collapse-system design.

---

# Purpose

This document defines the systemic laws governing the world itself, independent of lore. It is the design basis for world-state changes, faction behaviour, readable consequences, and world collapse. Future faction, settlement, economy, and consequence systems must depend on this document.

The document deliberately defines a lightweight, expandable model. It does not prescribe fictional faction identities, settings, gods, or spell lists.

# First Law

> **The world seeks to preserve balance, while intelligent beings constantly disrupt it.**

This is the primary world law. The world responds to accumulated disruption, while intelligent actors pursue goals that change the conditions around them. Balance and disruption create the pressure from which the remaining systems emerge: faction activity, observable symptoms, escalating consequences, and eventually world collapse.

# Balance

Balance is a **dynamic equilibrium**, not a static or ideal state. It constantly shifts as the world and its actors change. A temporary stabilisation is possible; permanent equilibrium is not.

No faction truly preserves Balance. Every faction pursues its own long-term goals and thereby pushes the world toward one of many extreme states. The player can also shift, redirect, or temporarily stabilise conditions, but cannot freeze the world in place.

Balance is a hidden systemic condition. It is inferred through the world rather than displayed as a single player-facing meter.

## What changes Balance

Balance shifts primarily through **Faction Influence**.

Faction Influence rises when a faction completes Main Goals and can also shift through Side Events and player Allegiance choices. Higher Influence makes a faction harder to oppose and more able to reshape everyday life. Accumulated Influence of competing factions is the pressure that eventually breaks Balance into World Collapse.

# World Seed

Every world is created from a **World Seed**.

The seed determines, at minimum:

- starting world conditions;
- starting Faction Influence values;
- which Main Goals are active and their concrete details;
- Side Event generation;
- priorities used by the World Engine.

The seed must change details enough that memorised answers from a previous world do not solve the next one. Player Knowledge still transfers as methods of reading and intervening, not as a solved checklist.

# Factions

Each faction owns:

- one or more Main Goals drawn from a limited pool;
- available Operations;
- current Faction Influence;
- conditions required to execute each Operation;
- access to followers, mentors, artifacts, and economic niches scaled by Influence.

Factions pursue Main Goals independently of the player. The player may participate, interfere, ignore, support, or betray.

## Main Goals

Main Goals come from a **limited pool**.

The seed changes details such as:

- location;
- timing;
- involved actors;
- required conditions;
- local dressing of the same underlying goal type.

When a faction completes a Main Goal:

1. its Faction Influence increases;
2. the world changes in a readable way under that Influence;
3. future Operations become easier, more frequent, or better supported for that faction.

## Side Events

Side Events are secondary, seed-generated situations. They are useful, but never free power.

In the playable world, Side Events are normally delivered as timed [Points of Interest](04%20Points%20of%20Interest.md): dormant until the player enters their area, finite in player world-time, and capable of unattended resolution if missed.

A Side Event is an Allegiance-bearing decision:

- helping one side yields one benefit;
- helping the other yields a different benefit;
- Influence shifts a little toward the supported side;
- the exact Influence shift stays hidden unless the player understands the faction affiliations involved;
- the choice may open some paths and close others.

Valid Side Event outcomes include allies, mentors, consumables, clues, artifacts, skills for followers, or Knowledge. The design rule is not "always a gift," but "always a meaningful fork with world residue."

# World Engine

The World Engine is a lightweight scheduler of consequential faction actions. It does **not** simulate civilizations, populations, or every NPC decision.

At defined world intervals, the World Engine inspects each faction's current Main Goal, requirements, and eligible Operations, then attempts an eligible Operation. This keeps the simulation bounded, legible, and feasible for a solo developer while allowing the world to evolve without the player.

Operations may include, for example:

- perform a ritual;
- infiltrate an organization;
- corrupt an official;
- steal an artifact;
- assassinate a target;
- spread a disease;
- create a cult;
- sabotage a location.

Every operation may:

- succeed;
- fail;
- be stopped by the player;
- be stopped by another faction;
- fail because of an observable world event.

Random world events must never function as opaque outcome rolls. They are observable conditions or events with readable effects. The player cannot always prevent them, but can learn how they alter operation outcomes and use that knowledge to predict consequences.

## Player intervention

The player may intervene in any faction confrontation or Operation if they are in the right place at the right time.

Place and time are Information problems. The player may learn them from faction followers or related NPCs through Approaches such as:

- persuasion / dialogue, affected by character traits and Knowledge;
- intimidation;
- barter of an artifact, favour, or service;
- combat, which may yield information only if the player has a method, skill, or artifact for extraction — otherwise the NPC may simply die with the secret;
- stalking and eavesdropping, if the player has a stealth-capable method, skill, or artifact;
- a **Yield Threshold**: before defeat or death, an NPC who values survival may offer the needed information in exchange for being spared.

These Approaches instantiate the Decision Engine. Combat is never the only valid route to information.

## Incremental scope

**V1:** A small number of factions, a fixed Operation set, Main Goals from a small pool, seed-driven details, simple Side Events with Allegiance forks, and one attempted Operation per interval. Every Operation or Side Event creates at least one Symptom or Readable Consequence.

**V2:** Operations and Side Events modify one another's conditions and Faction Influence more richly. Stalking, barter chains, and Yield Threshold behaviour become common tools.

**V3:** Longer Operation chains, emergent unintended faction cooperation, and denser seed variation while remaining operation-based rather than a full civilization simulation.

# Symptoms

The player never sees faction progress values or hidden Faction Influence. Instead, the player observes **Symptoms** and reads the world.

Influence spreads through everyday systems, for example:

- trade prices, quality, and availability;
- rest and services becoming restricted or faction-coloured;
- mentors appearing or leaving;
- shops closing or changing owners;
- churches, sermons, and public mood shifting;
- disappearances and corrupted locations;
- random encounters growing more frequent or dangerous at night, then later in daytime;
- otherworldly pressure becoming more likely to find the player;
- rumours, patrols, and unsafe districts.

Symptoms must be consistent enough for hypothesis-forming. They create the context-gathering phase of the Core Loop and let Knowledge improve future decisions without exposing a progress bar.

# Starting Faction and Difficulty

Only the **first character in a newly generated world** begins aligned with a **Starting Faction** and receives an initial understanding of that faction's current Main Goal and what pursuing it requires.

Later characters in the same world begin Factionless and must earn membership through Faction Entry. Full rules live in [07 Character Life](07%20Character%20Life.md).

A member may later betray, defect, or realign. Allegiance change is real and costly: Influence, access, Symptoms, and available paths all change.

## Difficulty Settings

The previous single World Difficulty scale (Easy / Normal / Hard / Hell) is superseded by **seven independent difficulty sliders**. The player can tune distinct aspects of the world separately; a single setting must not silently bundle unrelated effects.

The agreed sliders are:

1. **Combat Difficulty** — changes the generated power distribution of opponents and dangerous creatures. Higher settings make higher attributes and higher Pathway stages more common among generated threats. It does not multiply HP, damage, defence, or other canonical statistics, and does not introduce difficulty-specific character formulas.
2. **World Inhabitants** — controls the quantity, density, diversity, prevalence, and activity of creatures and NPCs. This is independent of their power: a dense world can contain weak inhabitants, while a sparse world can contain dangerous ones.
3. **Resource Gathering** — controls resource availability and the conditions, difficulty, and yield of gathering.
4. **Crafting** — controls recipe requirements, including ingredient quantities and the possible inclusion or increased likelihood of rarer components at higher settings. It does not change crafting time. The allowable output-quality range is not changed by this slider.
5. **Economy** — controls market conditions such as assortment, prices, stock, availability, and access to goods and services. Faction Influence remains the primary driver of local economic conditions.
6. **Equipment Quality** — controls the prevalence and availability of equipment by quality and rarity; higher settings can make rare and high-quality items less common.
7. **Wear and Maintenance** — controls equipment wear and the burden or availability of repair and maintenance.

These sliders affect world-generation conditions, distributions, availability, and costs. They do not create alternative versions of the underlying character, action-check, progression, or item-effectiveness formulas. Where a slider changes generated character strength, the same canonical attributes, skills, Pathway rules, and derived-stat formulas apply to player characters and non-player actors alike.

Combat Difficulty and World Inhabitants are orthogonal: one controls the power profile of generated threats, the other controls how many inhabitants exist and how varied or active they are. Economy and Equipment Quality are also distinct: Economy governs market conditions and access; Equipment Quality governs the quality/rarity distribution of equipment entering the world and its markets.

Exact slider ranges, presets, numerical distributions, and interactions remain open for later design and tuning. The old Easy / Normal / Hard / Hell faction-pressure direction is not retained as a bundled global difficulty model; faction pressure and Operation behaviour must be specified through their owning systems and any explicitly assigned settings.

# Economy Hook

Economy is an expression of Faction Influence.

As a faction's Influence grows, it occupies niches: better access, better prices for allies, worse prices or closed services for opponents, stronger artifacts and support. Detailed economy simulation is deferred, but future economy design must treat Influence as the primary driver rather than an isolated shop spreadsheet.

# World Collapse

A world never ends because of a timer. It ends because Balance has been irreversibly broken: dominant Faction Influence and completed Main Goals push the world into a terminal state.

The player may delay collapse, redirect which terminal state occurs, or accelerate collapse. The player can never prevent collapse forever. This preserves the finite-world Meta Loop while giving choices meaningful consequences.

Whether Balance can partially recover without the player, and the exact terminal win condition per faction identity, remain open questions. No implementation may invent answers before they are documented.

# Knowledge

The world becomes increasingly understandable through play. Failure should result primarily from incomplete understanding, poor preparation, or accepting a known risk — not from unknowable randomness.

By observing Symptoms, Operations, and consequences, the player learns to anticipate likely future changes. Knowledge never guarantees control: it improves prediction and opens better interventions, while the world retains uncertainty and independent agency.

Important places, faction tells, information Approaches, and the long-term meaning of bargains are prime Knowledge targets. Character power systems that use formulas, ingredients, or pathways are future Progression design; they must obey Price of Power and must not replace Knowledge with a pure success-rate bonus.

# External Power Principle

Any power granted by otherworldly entities, churches, deities, or similar external sources is a **Bargain**:

- the advantage is real;
- the price is readable eventually;
- the price is often delayed and escalates;
- refusal remains a valid choice.

Detailed curse, blessing, and pathway systems belong to future Progression / Magic documents. They may not contradict this principle.

# Documentation Review

- **Vision:** Compatible. Living finite world, priced power, and Knowledge over raw control remain central.
- **Core Loop:** Compatible. Symptoms, Side Events, and intervention Approaches feed notice → context → action → consequence → understanding.
- **Meta Loop:** Compatible. Seeded worlds stay finite; Knowledge transfers as method, not solved answers.
- **Design Pillars:** Compatible. Side Events are forks, not free loot; Difficulty raises challenge without erasing curiosity.
- **Player Experience:** Compatible. Hidden Influence shifts reward understanding; Yield Threshold and eavesdropping support inventive Approaches.
- **Decision Engine:** Compatible. Intervention and Side Events instantiate Attention, Approach, Allegiance, Commitment, and Bargain.
- **Glossary:** Updated for Seed, Main Goal, Side Event, Starting Faction, Difficulty Pressure, Yield Threshold.
- **Open Questions:** Resolved Influence/Balance driver, Engine selection basis, symptom direction, starting faction, difficulty direction; recovery and terminal win details remain open.
