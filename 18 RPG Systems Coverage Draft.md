# 18 RPG Systems Coverage Draft

Version: 0.1
Status: Draft
Last Updated: 2026-10-29

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [001 Documentation Standards](001%20Documentation%20Standards.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [05 Progression](05%20Progression.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)

Referenced By:
[README](README.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), future system design documents, specification-map data

---

# Purpose

This is a **working coverage draft**, not a specification and not authorisation to implement systems.

It records candidate RPG system areas, observations, dependencies to investigate, and the order in which we will design them. We will return to this document after each design session so that unresolved areas are not forgotten. A candidate is not automatically approved, required for V1, or confirmed absent from the existing specification map.

The interactive specification map remains the place to navigate system nodes and their relationships. This document is the durable review checklist that helps us audit the map, discover gaps, and decide what each node should contain.

# Design workflow

For each system, work in small steps:

1. Inspect the current map and canonical/draft documents before proposing a new node.
2. State the system's purpose and a short list of design questions/observations.
3. Discuss the player's vision and constraints with the project owner.
4. Identify the correct parent system and only the direct dependencies/relationships that materially affect the system.
5. Decide whether the work belongs in an existing node, a new child node, or a cross-system relationship.
6. Update the specification map and its underlying source of truth; keep global view at major systems and details nested underneath.
7. Update relevant documentation, dependencies, glossary, and open questions as required.
8. Record status here and choose the next step.

Do not silently invent answers to unresolved game-design questions. Do not duplicate existing nodes merely because their current descriptions are incomplete. Do not begin implementation as a consequence of this draft.

# Map structure principles

- **Global map = major systems only.** The global view should remain readable and show the main systems and meaningful direct relationships between them.
- **Hierarchical drill-down.** Each major system contains its principal subsystems; each subsystem may contain more detailed nodes as needed. Keep the tree navigable rather than exposing every detail at the global level.
- **Direct relationships only.** A node should link directly to systems it actually exchanges state, rules, requirements, or outcomes with—not to loosely related systems just to create a dense graph.
- **A parent owns its children.** Entering a major system should reveal its relevant subnodes and the relationships needed to understand that system.
- **Shared character model.** The player character and NPCs are both Characters using a shared underlying model. The player character receives explicit player-facing progression and controls; NPC state, inventory, attributes, abilities, and equipment can be generated and simulated behind the scenes according to NPC-specific rules.
- **Distinguish system from content.** For example, define the creature system before populating a bestiary; define quest rules before writing quest lines; define item rules before making item catalogues.
- **Keep the design philosophy intact.** Curiosity over power, knowledge over levels, meaningful consequences, finite/living worlds, and a meaningful price for gained power remain constraints.
- **Scope by version.** For each system, distinguish V1's minimum coherent gameplay from V2/V3 extensions. A node may be documented now even if implementation is deferred.
- **No assumed absence.** Each candidate below must be checked against the live map and repository before a node is added.

# Candidate major systems and observations

These are candidate groupings for the global map. Names are provisional and may be merged, split, or renamed after map inspection.

## A. Character

A shared model for playable characters and NPCs, with role-specific rules for player-facing control versus autonomous simulation.

- **Attributes:** define the character's core qualities, ranges, starting values, and the actions or requirements each attribute affects.
- **Derived statistics:** define computed values such as health, movement, initiative, accuracy, evasion, resistances, perception, and carrying capacity.
- **Skills and checks:** define trained capabilities, check resolution, difficulty, modifiers, and how attributes affect success or quality.
- **Abilities:** define active/passive powers, prerequisites, costs, targeting, effects, duration, scaling, and counterplay.
- **Character state and conditions:** define wounds, temporary effects, illness, impairment, buffs/debuffs, and their stacking and removal rules.
- **Character progression:** define all forms of growth without assuming a generic XP/level system.
- **Character life and death:** connect the character's lifetime to death, successor creation, legacy, and world state.
- **NPC profile and autonomy:** define how NPC characters are generated, equipped, assigned capabilities, and simulated behind the scenes.

## B. Pathways and power progression

Existing progression foundations must be audited and extended, not replaced by a generic level system.

- **Pathway identity and acquisition:** define how a character obtains a Pathway and how each Pathway changes the way they act.
- **Stages and advancement requirements:** define eligibility, Conduct Progress, formula knowledge, ingredients/conditions, and Bargain/price.
- **Attribute and ability requirements:** investigate how abilities teach or imply the qualities needed for deeper advancement; requirements should be discoverable rather than explained as a solved checklist.
- **Sources of attribute changes:** define permanent, temporary, conditional, and equipment-based changes, including whether and how they can satisfy advancement requirements.
- **External power:** define blessings, entities, rituals, and other bargains, including benefits, costs, delayed consequences, and conflicts with Pathways.
- **Pathway interactions and dissonance:** define compatibility, conflict, and consequences without enabling a free perfect build.

## C. Items, inventory, and equipment

- **Item model:** define item identity, categories, properties, rarity/quality, requirements, hidden properties, and uniqueness.
- **Inventory and containers:** define capacity, stacking, transfers, dropping, storage, and item ownership.
- **Equipment:** define slots, compatibility, attribute requirements, granted properties, and recalculation of derived statistics.
- **Consumables and reagents:** define use, charges, ingredients, formula components, and effects.
- **Item acquisition and loss:** define loot, quest rewards, trade, theft, confiscation, death, and legacy.
- **Durability and maintenance:** assess whether repair, wear, and degradation belong in V1 or should be deferred.

## D. Combat and confrontation

Combat is a distinct arena-based mode under the current architecture; it must not make combat the default best solution.

- **Encounter start and participants:** define how Confront begins and who is included.
- **Arena spatial rules:** define grid, movement, terrain, cover, height, line of sight, and interaction with arena objects.
- **Turn/action economy:** audit existing Move + Action + Bonus rules and define costs and turn order.
- **Attack and damage resolution:** define hit checks, damage types, mitigation, critical outcomes, resistances, and attribute/ability scaling.
- **Defence and survival:** define health, wounds, status effects, incapacitation, death, and recovery.
- **Ability use in combat:** define costs, ranges, targeting, area effects, durations, and counterplay.
- **Combat AI:** define threat assessment, target selection, positioning, ability use, surrender, and retreat.
- **Combat outcomes and residue:** define victory, escape, yield/spare, death, witnesses, alarms, loot boundaries, and changes to the source scene/world.

## E. World actors and creatures

- **Shared actor/character foundation:** connect NPCs and human opponents to the shared Character model.
- **Creature model:** define creature attributes, behaviours, abilities, needs, habitat, weaknesses, and resistances.
- **Spawn and population rules:** define spawn tables, biome/time/POI conditions, population limits, respawn, migration, and removal.
- **NPC behaviour and schedules:** define goals, daily activity, reactions, relationships, and world simulation when the player is absent.
- **Dialogue and social interaction:** define dialogue state, checks, available approaches, information exchange, and consequences.
- **Knowledge of creatures/NPCs:** define how observations, records, rumours, and experiments reveal capabilities without omniscient information.

## F. Quests, decisions, and information

- **Quest system:** define quest creation/acquisition, objectives, stages, optional goals, failure, refusal, branching, and completion.
- **World events:** define triggers, timers, active windows, unattended resolution, and consequences.
- **Decision system:** preserve the existing Decision Engine and define options, trade-offs, irreversible outcomes, and readable consequences.
- **Information and investigation:** define sources, clues, evidence, inference, uncertainty, misinformation, and knowledge persistence.
- **Requirements and checks:** define the shared way quests, dialogue, POIs, abilities, and progression test attributes, skills, items, knowledge, relationships, and world state.
- **Rewards and resolution:** define rewards beyond combat loot, including knowledge, access, allies, faction changes, progression opportunities, and world changes.

## G. Exploration and interaction

- **Movement and traversal:** define locomotion, collision, jumping/falling, traversable surfaces, and interaction constraints in overworld/interiors.
- **Points of Interest:** audit existing POI rules and define discovery, activation, state, content, revisit, expiry, and consequences.
- **Interiors and dungeons:** define entry/exit, exploration, locks, traps, secrets, encounter transitions, and generated structure.
- **Gathering and resource collection:** define discovery, eligibility, tools/skill checks, time/cost, yield, depletion, regrowth, and world-state changes.
- **World interaction:** define examining, opening, talking, manipulating objects, hidden objects, and context-sensitive actions.
- **Time, weather, and environmental conditions:** define which conditions affect visibility, actors, resources, POIs, and events.

## H. Factions and relationships

- **Faction model:** define goals, resources, members, territory, methods, allies, and rivals.
- **Reputation and attitudes:** distinguish trust, fear, hostility, recognition, and other relationship dimensions instead of assuming one universal score.
- **Membership and ranks:** define entry, obligations, privileges, advancement, expulsion, betrayal, and faction-specific access.
- **Faction conflict and diplomacy:** define alliances, operations, escalation, territorial change, and reactions to player actions.
- **Personal relationships:** define trust, debts, friendship, rivalry, memory, and NPC-specific responses.

## I. Economy and crafting

- **Currency and value:** define currencies, item value, scarcity, and regional variation.
- **Trade and merchants:** define stock, prices, access, negotiation, and merchant attitudes.
- **World economy:** assess the minimum viable model for production, supply, demand, faction resources, and event-driven changes.
- **Crafting and alchemy:** define recipes, tools, requirements, inputs, quality, discovery, and failure.
- **Repair and storage:** define maintenance and persistent storage if included in scope.

## J. Supernatural powers and their price

- **Power sources:** connect Pathways, blessings, entities, rituals, artifacts, and other sources to the Character/Ability model.
- **Bargains and blessings:** define offers, benefits, prices, obligations, refusal, and delayed escalation.
- **Symptoms and side effects:** define observable and hidden consequences, duration, accumulation, detection, and effects on behaviour or checks.
- **Rituals:** define knowledge, location, timing, components, participants, interruption, and failure.
- **Loss of control and countermeasures:** assess how unstable power can affect agency and what meaningful responses exist.

## K. World simulation and generation

- **World generation and biomes:** preserve the existing seeded, deterministic world-generation architecture.
- **Settlement/building/interior generation:** preserve Piece-Based Assembly and current scene boundaries.
- **Persistent world state:** define mutations, ownership, destroyed/opened/looted objects, and state across scene transitions and character lives.
- **World Engine and global threats:** connect existing faction operations, goals, influence, symptoms, and events to persistent simulation.
- **Simulation activity and performance:** define which systems tick at distance, pause, or resolve lazily, without simulating every entity continuously.

## L. Shared rules and supporting systems

- **Unified check/modifier rules:** define common resolution for attributes, skills, abilities, situational factors, and uncertainty.
- **Unified effect system:** define temporary/permanent effects and stacking, source attribution, duration, and removal.
- **Save/load and versioning:** define persistence for character, inventory, world state, quests, factions, NPCs, and consequences.
- **UI and player feedback:** define character sheet, inventory, quest log, dialogue, map, combat UI, and how hidden rules are communicated fairly.
- **Balance and validation:** define formula ownership, test cases, scaling boundaries, edge cases, and dominant-strategy checks.

# Initial review order

This is a proposed design/review order, not an implementation schedule. Reorder only after checking dependencies and discussing trade-offs.

1. **Character attributes and unified checks** — establish the foundational vocabulary and resolution rules before defining dependent mechanics.
2. **Skills and abilities** — determine how attributes influence both combat and non-combat actions.
3. **Pathways and advancement requirements** — connect abilities, attribute requirements, discoveries, formulas, ingredients, and price of power.
4. **Items, inventory, and equipment** — define how item properties and equipment modify character state and requirements.
5. **Combat system** — connect attributes, derived statistics, skills, abilities, equipment, AI, and confrontation consequences.
6. **NPCs and creatures** — apply shared character rules to player-independent actors, with NPC-specific generation and simulation.
7. **Quests, POIs, information, and rewards** — define the loops that provide discovery, progression opportunities, and consequences.
8. **Factions, relationships, and social systems** — connect actors, decisions, reputation, membership, and world operations.
9. **Gathering, crafting, trade, and economy** — define resource acquisition and use in relation to progression and the world.
10. **World simulation, death/legacy, persistence, UI, and balance** — audit cross-system completeness and close remaining dependencies.

# Status tracker

The following are review states, not claims about the current map.

| Area | Draft status | Next action |
|---|---|---|
| Map hierarchy and direct relationships | Not audited | Inspect the live map and identify its data source |
| Shared Character model for player and NPC | Proposed | Compare against existing actor/NPC/character definitions |
| Attributes and checks | Not designed in this pass | First detailed design session |
| Skills and abilities | Candidate inventory captured | Review after attributes/checks |
| Pathways and advancement requirements | Existing foundation; integration open | Audit [05 Progression](05%20Progression.md) and dependent nodes |
| Items, inventory, equipment | Candidate inventory captured | Audit map and existing architecture cards |
| Combat | Existing constraints and turn economy; full rules open | Audit [08](08%20Confront%20Constraints.md), [10](10%20Turn%20Action%20Economy.md), [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md), [16](16%20Architecture%20Pivot.md) |
| NPCs and creatures | Partial architecture references | Audit actor/spawn/AI cards |
| Quests, POIs, information | Existing foundations; integration open | Audit [03](03%20Decision%20Engine.md), [04](04%20Points%20of%20Interest.md), [09](09%20Information%20Sources.md) |
| Factions and relationships | Existing world/faction foundations; depth open | Audit [02 World Laws](02%20World%20Laws.md) and [05 Progression](05%20Progression.md) |
| Economy and crafting | Mentioned architecturally; mechanics open | Determine V1 minimum after core item/resource rules |
| World simulation and generation | Significant existing documentation | Preserve current authority and audit only missing gameplay interfaces |
| Shared rules, save, UI, balance | Partial architectural coverage | Audit system cards and cross-system contracts |

# Constraints and open design questions

- Attribute names, ranges, formulas, and allocation rules are not decided by this draft.
- The exact relationship between attributes, skills, abilities, Pathway requirements, equipment bonuses, and temporary effects must be designed explicitly.
- The game must not quietly acquire a generic kill-XP/level-up loop; any progression proposal must be checked against [05 Progression](05%20Progression.md) and the project pillars.
- The NPC model must share the underlying Character foundation without requiring player-facing progression/UI for every NPC.
- Global map structure and direct edges must be checked against the actual site data before changing it.
- Candidate groupings and priorities can change after the owner reviews them; this document records observations, not final design decisions.

# Completion criteria for this draft

This coverage draft can be considered reviewed when every candidate area has been compared with the actual map and repository, each area is classified as covered/partial/missing/deferred, duplicates are resolved, direct dependencies are represented, and the owner has reviewed the proposed hierarchy and design order. The overall RPG specification is complete only after the relevant systems have been designed, not merely listed here.
