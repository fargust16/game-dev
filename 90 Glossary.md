# 90 Glossary

Version: 0.12

Status: Draft

Last Updated: 2026-08-05

Depends On:
All Canonical Documents

Referenced By:
All Documentation, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [AGENTS.md](AGENTS.md)

---

## Curiosity

Primary player motivation.

---

## Knowledge

Player understanding that survives Character Lives and worlds. It unlocks better reasoning and option recognition. It must not act primarily as a success-rate bonus.

---

## Player Knowledge

Synonym emphasis for Knowledge held by the real player across lives and worlds, as distinct from temporary character power.

---

## Legacy

Everything remaining after a character dies.

---

## Character

Playable individual living inside one world.

---

## Player

The real person.

The only entity preserving knowledge between worlds.

---

## World

A finite simulation containing civilizations, factions and history.

---

## World Seed

The generation input that determines a world's starting conditions, faction states, Main Goal details, Side Events, and related variation.

---

## World Cycle

The complete lifespan of one generated world.

---

## Character Life

The playable lifespan of one character.

---

## Meta Loop

Progression connecting multiple characters and worlds.

---

## Core Loop

Repeated gameplay cycle during one character's life.

---

## Pillars

Non-negotiable design principles.

---

## Price of Power

Every gained strength requires sacrifice.

---

## Experiment

Any intentional attempt to discover unknown behaviour.

---

## Consequence

A change caused by player action, failure, or World Engine activity. May be immediate or delayed, visible or hidden, temporary or permanent. Important decisions require at least one Readable Consequence.

---

## Living World

A world evolving independently of player presence.

---

## Finite World

A world that eventually reaches its end.

---

## Balance

Dynamic equilibrium of competing world conditions. It constantly shifts and cannot be permanently preserved by any faction. It changes primarily through Faction Influence.

---

## World Engine

A lightweight system that periodically attempts eligible faction operations and applies their readable consequences. It does not simulate entire civilizations.

---

## Operation

A single faction action with conditions and possible outcomes: success, failure, interruption by the player or another faction, or failure caused by an observable world event.

---

## Main Goal

A faction's primary objective drawn from a limited pool. Seed changes details such as place, time, and actors. Completion raises Faction Influence and reshapes the world.

---

## Side Event

A seed-generated secondary situation. It offers useful but forked outcomes, shifts Influence, may open or close paths, and never exists as pure free power.

---

## Symptoms

Observable changes through which the player reads hidden faction activity and influence instead of seeing progress values. Includes trade, services, encounters, rumours, and other everyday pressure.

---

## Faction Influence

The hidden current capacity of a faction to pursue its long-term goals and make operations available. Higher Influence strengthens followers, attempt frequency, artifacts, and economic niche control. It is communicated through Symptoms, not a player-facing value.

---

## Starting Faction

The faction granted only to the first character in a newly generated world. That character receives an initial understanding of the faction's current Main Goal and may later defect or realign at a real cost. Later lives begin Factionless.

---

## Factionless

Character state with no faction membership. Default for every life after the first in the same world. Joining requires Faction Entry.

---

## Faction Entry

A required trial, quest, or Decision Node chain to join a faction after starting Factionless.

---

## Death Gap

Fixed short span of world-time after character death during which the world advances and resolves outstanding pressure before the next character is created.

---

## Legacy Cache

An intentional stash prepared for a future Character Life in the same world. In V1 it creates a map marker for the next character and is the simple reliable inheritance channel.

---

## Soft Memory

Lightweight world remembrance of a previous character through rumours, dialogue hooks, and opinion shifts. Not bloodline power inheritance.

---

## Difficulty Pressure

World creation rule that adjusts non-player faction advantage by difficulty: Easy favours the player's Starting Faction; Normal slightly favours opponents; Hard and Hell strongly favour opponents.

---

## Yield Threshold

An NPC's internal point at which survival matters enough to offer information or surrender terms before death or total defeat.

---

## World Collapse

The terminal transition of a world after Balance is irreversibly broken and a dominant faction achieves its objective. It is caused by world state, never by a timer.

---

## Decision Engine

The systemic rules that make every important player choice interesting. Foundation for quests, exploration, combat, progression, factions, and world interaction.

---

## Decision Node

One important player choice moment designed with the Decision Node Template. It activates Decision Axes, preserves Partial Uncertainty, produces Readable Consequences, and leaves world residue.

---

## Point of Interest

A seed-driven map opportunity that can host a Decision Node or discovery beat. It stays computationally dormant until the player enters its interaction area or its Active Window expires.

---

## Active Window

The finite lifetime of a Point of Interest measured in player world-time. Missing the window causes unattended resolution.

---

## Lazy Activation

Performance and design rule: full encounter logic for a POI starts only when the player enters its interaction area. Timers and abstract world residue still advance without staging the scene.

---

## Unattended Resolution

Outcome applied when a POI's Active Window ends without player participation. Weighted by Faction Influence and local state, never by opaque meaningless randomness. Leaves Symptoms or other Readable Consequences.

---

## Decision Axis

One of five minimal decision categories: Attention, Approach, Allegiance, Commitment, Bargain. Important nodes activate at least two axes.

---

## Attention

Decision axis: engage the unknown, or defer/ignore it. Primary V1 teaching axis.

---

## Approach

Decision axis: choose how to act when multiple methods remain viable. Immediate companion to Attention in V1 teaching.

Also used for one concrete method from the V1 set in [06 Approaches](06%20Approaches.md): Confront, Convince, Threaten, Exchange, Veil, Withdraw.

---

## Confront

Approach: force the issue openly, including fight or open defiance. Hard constraints live in [08 Confront Constraints](08%20Confront%20Constraints.md).

---

## Break Away

A pressured escape from Confront. A costly form of Withdraw after violence has begun.

---

## Diegetic Loot

Rewards that exist because a target carried or guarded them, not because a fight completion spawned abstract treasure.

---

## Turn-Based Mode

Deliberate paced control mode used for Confront and optionally outside combat. Begins inside the current location; reference feel from Baldur's Gate 3 / Fallout 1–2 without requiring feature parity.

---

## Information Source

A reusable channel through which the player learns partial world or POI information, such as notice boards, newspapers, bartenders, rumours, faction contacts, witnesses, physical traces, or Soft Memory.

---

## Hinted

Discovery state where the player knows a region, type, or urgency of a POI without an exact map pin.

---

## Marked

Discovery state where a POI is visible on the world map.

---

## Convince

Approach: change minds through persuasion, appeal, or shared interest, without fear as the main lever.

---

## Threaten

Approach: compel through fear, pressure, or ultimatum.

---

## Exchange

Approach: trade value for value — items, favours, services, secrets, or debts.

---

## Veil

Approach: act with limited visibility — stalk, eavesdrop, sneak, or misdirect.

---

## Withdraw

Approach: leave, delay, or refuse engagement. Meaningful because the world continues.

---

## Allegiance

Decision axis: choose whose interests take priority when interests conflict. Neutrality counts and is never free.

---

## Commitment

Decision axis: spend or preserve scarce stocks such as resources, trust, safe routes, world-time, or one-shot opportunities.

---

## Bargain

Decision axis: accept a Price of Power, or refuse the advantage. Second major V1 teaching lesson after Attention + Approach. Any external power is a Bargain.

---

## Partial Uncertainty

Information state before important choices: investigation and Knowledge can narrow likely outcomes without collapsing the decision into a solved equation.

---

## Readable Consequence

A consequence the player can eventually interpret as causal. Every important decision must produce at least one.

---

## Failure Value

The requirement that failure produce Knowledge, a World Delta, or a New Opportunity. Failures that only waste time are invalid.

---

## World Delta

A readable change in world state caused by player action, failure, or World Engine activity.

---

## External Power

Capability granted by otherworldly entities, churches, deities, or similar sources. Always treated as a Bargain with a readable, often delayed and escalating price.

---

## Pathway

A staged character-growth track distinct by way of acting. V1 has three Pathways and only stages 9 → 7. Lore names come later.

---

## Stage

A numbered depth on a Pathway. Lower numbers are deeper advancements with sharper prices. V1 uses 9, 8, and 7 only.

---

## Conduct Progress

Between-stage advancement evidence earned by living a Pathway's way of acting. It is not kill-XP and must not become a permanently solved checklist.

---

## Formula

Discoverable advancement knowledge required for a Pathway stage. Concrete formulas vary by World Seed.

---

## Pathless

A character state with no Pathway yet. Valid and sometimes freer, but without staged Pathway advancements.

---

## Canonical

Official project truth.

---

## Draft

Work in progress.

---

## Deprecated

Historical documentation kept for reference.
