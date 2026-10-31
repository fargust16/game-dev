# 18 RPG Systems Coverage Draft

Version: 0.5
Status: Draft
Last Updated: 2026-10-30

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [001 Documentation Standards](001%20Documentation%20Standards.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [05 Progression](05%20Progression.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)

Referenced By:
[README](README.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), future system design documents, specification-map data

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
- **Derived statistics:** defined in [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md): resources (HP, Stamina, Will Power), combat values (Accuracy, Evasion, Physical/Magical Defence, Initiative, AP), movement, carrying load, and contextual Detection/Stealth. Detailed formulas remain open.
- **Skills and checks:** define trained capabilities, check resolution, difficulty, modifiers, and how attributes affect success.
- **Abilities:** define active/passive powers, prerequisites, costs, targeting, effects, duration, scaling, and counterplay.
- **Character state and conditions:** define wounds, temporary effects, illness, impairment, buffs/debuffs, and their stacking and removal rules. The shared Will / Will Power / Sanity model is defined in [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), including the transformation at Sanity 0.
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

### Owner's itemisation direction — items, crafting, enchantment, and inventory

The item system takes selected structural inspiration from Path of Exile 2, but uses the project's own simplified rules. Do not import unrelated PoE mechanics by default.

- **Item bases and stages:** each item is created from a defined base with foundational properties. A base has a stage (9 through 0); the player may craft any lower-stage base they can supply the corresponding materials for, even after reaching higher profession mastery. Higher-stage crafting uses more varied and harder-to-obtain ordinary resources and a rare ingredient of the corresponding stage.
- **Item creation profession:** the former separate blacksmithing, leatherworking, and tailoring professions are consolidated into one general Item Creation profession. It creates all equipment categories: armour, weapons, rings, amulets/necklaces, and other accessories. Item categories/material families include cloth, leather, chain/chainmail, and plate/metal. The initial number of mechanical bases is intentionally kept small; the current conservative starting point is one base per major armour/material family, with additional bases and visual variants deferred until AI-assisted model generation feasibility is assessed.
- **Enchanting profession:** a separate Enchanting profession handles modifier manipulation and related recipes. Item Creation does not reforge or directly manipulate existing modifiers.
- **Modifier count:** an ordinary crafted item receives a random count from 0 to 6. Item Creation skill/mastery affects only the probability distribution of this count, smoothly shifting it toward more modifiers as mastery rises. It does not affect which modifier types or tiers are selected, or their numerical values. Quality follows from total count: Normal = 0; Magic = 1–2; Rare = 3; Epic = 4; Legendary = 5–6.
- **Major and minor modifier groups:** every item can have at most three major and at most three minor modifiers. There is no separate pool, semantic distinction, or weighted preference between the two groups. Once total modifier count is rolled, the assignment of those modifiers to major versus minor is random, subject only to the three-per-group maximum. Six modifiers necessarily means three major and three minor. Four modifiers may be distributed in any valid combination; the same capacity constraint applies to every other count. The assignment is not affected by base, stage, skill, or other factors.
- **Modifier selection and tiers:** after modifier count and major/minor assignment are determined, modifier identities and tiers are randomly selected using the probability table for the specific base and item stage. Each base has its own table at each stage. These tables determine which modifier types are more or less likely and the tier distribution; higher-stage items favour stronger tiers. There is no hard tier ceiling: even a stage-9 item can theoretically roll the strongest tier. Item Creation skill does not change the probability of any particular modifier type or tier.
- **Modifier values:** each selected modifier's numerical value is rolled randomly within the permitted range for its selected tier. The value is independent of profession skill, base stage, and other character factors.
- **Rare crafting ingredient and naming:** creating an item requires ordinary base resources plus one rare ingredient of the item's stage. The ordinary resources and rare ingredient can be gathered or purchased. Any rare ingredient of the required stage is valid; its identity affects only the item's generated name, not its base properties, modifier pool, weights, or generated stats. The rare ingredient is consumed.
- **Reforging and modifier manipulation:** these are Enchanting operations, not Item Creation. Enchanting will have several distinct recipes, including rerolling all eligible modifiers, protecting/locking a chosen modifier, and adding a specific modifier learned by extraction. Exact recipe catalogue and costs remain open. Previously agreed permanent-lock direction remains unless superseded by the new extraction/insertion rules.
- **Modifier extraction and insertion:** a character may bring an item to an Enchanting operation and extract one chosen modifier from it. The source item is destroyed by extraction. The extracted modifier can then be inserted into another item through the corresponding learned recipe. The insertion recipe requires its own specified ingredients/resources. A modifier inserted this way is treated as locked/protected: later modifier changes on that target item do not alter it. The character can only insert modifiers they have learned by extracting them from items. The extraction/insertion recipe, once learned, defines the ingredient cost. Exact ingredient lists and whether extraction and insertion are one combined recipe or linked operations remain to be detailed.
- **Item repair and durability:** durability decreases through use and damage (for example, armour wears from received hits and weapons from use). Durability does not gradually reduce item statistics. At zero durability, the item becomes unusable and grants none of its properties or bonuses. It is not permanently destroyed and can be repaired at any time by spending basic resources of the type used to create it. Exact repair quantities and durability loss rates remain open.
- **Monster acquisition and harvesting:** monsters do not drop finished equipment. They provide ordinary resources and valuable ingredients, and may provide food/cooking ingredients such as meat. Each creature provides a basic set of ingredients by default. A separate learned monster-harvesting/butchering capability is required to obtain a fuller set of materials from a creature. The exact skill/profession classification and yields remain open.
- **Chest acquisition:** chests may contain a broad random selection of contents, including equipment, notes, ingredients, materials, money, and other items. Chests have rarity tiers assigned as part of world placement/generation. Chests farther from the character's initial area and in more dangerous locations tend to have higher rarity and better loot. Exact rarity probabilities and loot tables remain open.
- **NPC equipment and death:** NPC equipment is generated/assigned as part of the NPC's build, role, and individual characteristics. NPCs are more likely to carry items matching their own stage, but may have higher-stage items as exceptions. On NPC death, approximately half of their carried/equipped items remain recoverable; the rest are considered destroyed, taken by a deity, or otherwise unavailable. Exact rounding/selection rules remain open.
- **Inventory stacking:** all materials and stackable consumables stack, including potions, keys, lockpicks, bandages, crafting ingredients, and similar consumables. Ordinary equipment and accessories never stack, even when their base and modifiers are identical. Currency is stored separately from the ordinary grid inventory.
- **Inventory grid and storage:** the inventory uses a grid with variable item footprints, inspired by Path of Exile 2. The owner prefers a relatively generous carrying grid and a separate home stash/chest. Provisional footprint examples: one-handed weapons and one-handed staves occupy 3 cells; two-handed weapons and staves occupy 8; shoulder armour and trousers occupy 6; belts occupy 2; shields occupy 6. These are reference examples, not a complete final footprint table.
- **Equipment hands and slots:** the character has two universal hand slots. One-handed weapons, shields, talismans, books, and other off-hand items can occupy either compatible hand; two-handed weapons occupy both. A bow or crossbow occupies one hand while its arrows/bolts occupy the other hand as ammunition. Staffs and other explicitly two-handed weapons occupy both hands. Armour/accessory slots include head, body, shoulders, gloves, belt, trousers, boots, two rings, and one necklace/amulet slot. Necklace and amulet are one slot, not two. Exact compatibility and the final equipment-slot catalogue remain open.
- **Unique items:** unique items remain a separate, bespoke item category and will be designed independently.

This is a recorded design direction, not yet a complete implementation specification. Exact base catalogues, item-stage tables, modifier-count probability curves, enchantment recipes/costs, durability rates, harvesting yields, chest loot tables, NPC recovery selection, inventory dimensions, and V1 scope remain open.

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

# First detailed design pass: character attributes, skills, and action-based progression

## Owner's current vision — captured, not yet fully specified

The owner describes a progression model driven by actions and experience in the ordinary sense of lived practice, not by a generic XP pool, character level-ups, or manually distributed attribute points.

- **No conventional character levels or freely allocated level-up points.** A character develops by performing actions and living through their consequences. Pathway stages remain a distinct progression track.
- **Two linked development layers.** General attributes (provisional examples: strength, endurance, intelligence, perception, luck, wisdom) describe broad capabilities. Skills (provisional examples: tree-felling, gathering, running, swimming, crafting) describe practiced activities. The final attribute list and skill taxonomy are open.
- **Actions can develop skills and attributes.** Repeated practice improves the relevant skill and may also develop one or more broad attributes where the action provides a credible training stimulus.
- **Skills have practical consequences.** Greater skill may improve speed, efficiency, quality, yield, or the chance to discover rarer results, where appropriate to that skill.
- **Attributes support related actions even without specialized practice.** For example, high strength should make a character more effective at felling trees even if the tree-felling skill is low. Relevant attributes may also make a related skill easier or faster to develop.
- **Action-specific contribution limits.** An activity should have a bounded or diminishing contribution to general attributes. Tree-felling may develop strength up to a point, after which other kinds of effort are needed or its contribution becomes very small. The exact curves and caps remain open.
- **Anti-abuse is a system requirement.** Repeating trivial or deliberately self-inflicted loops (such as taking damage and healing repeatedly) must not provide unlimited health or attribute growth. Meaningful effort, challenge, risk, and diminishing returns may be part of the eventual solution; no exact formula is decided here.
- **Progress should be perceptible without direct coaching.** The interface can show changed attribute values and newly acquired skills. The game need not explicitly tell the player which stat to raise; the player can infer a Pathway's tendencies from the changes caused by its potion and later actions.
- **Pathway stages are separate from ordinary development.** The current vision is that characters begin without a Pathway stage; taking the relevant potion initiates a Pathway at stage 9, with later advancement through its stages. The potion changes attributes and grants some skills or capabilities, subtly signalling what matters for that Pathway. This must be reconciled with the existing [05 Progression](05%20Progression.md) before becoming canonical.
- **Potion effects should be legible.** After taking a Pathway potion, the player should be able to notice which attributes changed and which skills/capabilities became available, without necessarily receiving an explicit tutorial telling them how to build the character.

## Owner decisions — attributes, skills, and feedback

The owner has approved the following working principles for this draft:

- **Both attributes and skills grow through practice.** An action directly develops its relevant skill and can also develop appropriate general attributes. The size, conditions, and limits of each contribution remain to be designed.
- **Action vocabulary stays compact and gameplay-facing.** List player-visible action families (such as running, jumping, sneaking, gathering herbs, or cooking), not every situational variation (such as jumping over a fence versus a gap). Context, object rules, equipment, and conditions determine how an action resolves.
- **Use concrete individual skills as the default model.** Examples include tree-felling, gathering, running, swimming, and specific crafts. Whether any skills should be grouped or have sub-specialisations remains open; the initial design should not default to broad categories such as “Athletics” or “Crafting” when a distinct skill is meaningful.
- **Show progress without revealing the formulas.** The interface should make attribute changes and skill progression visible, while keeping exact growth calculations hidden. Players should be able to infer patterns from action and outcome rather than being told the optimal build.
- These are draft design decisions, not implementation authorisation. Anti-abuse, diminishing returns, action-to-attribute mappings, and the exact feedback presentation remain open.

## Initial questions for the next design steps

1. What is the conceptual difference between a general attribute and a skill in this game?
2. Which candidate attributes are genuinely useful and distinct, rather than overlapping labels?
3. Are all skills discovered by action, or can teachers, books, Pathways, or tools unlock them directly?
4. What constitutes meaningful practice: repetitions, time, effort, challenge, risk, successful outcomes, or a combination?
5. How should a skill's growth affect speed, yield, quality, rare finds, and failure?
6. Which actions can train each general attribute, and when does their contribution diminish?
7. How should health, endurance, injuries, rest, and recovery develop without rewarding abuse?
8. What feedback should the interface provide immediately versus through gradual observation?
9. How do potion-granted attribute changes and skills interact with ordinary action-based development and Pathway requirements?
10. How do NPCs use the same underlying model while generating and updating their development behind the scenes?

## Provisional status

- Owner's broad design direction: captured in this draft.
- Owner-approved principles: both attributes and skills grow through practice; concrete individual skills are the default; changes are visible without exposing growth formulas.
- Exact attributes and skill categories: open.
- Growth and diminishing-return model: open.
- Anti-abuse rules: requirement identified; mechanism open.
- Feedback and UI: direction captured; presentation open.
- Relationship with existing Pathway progression: requires explicit reconciliation.
- Live interactive map hierarchy and its source data: not yet audited in this pass.

---

# Completed design slice — attributes and action checks (2026-10-30)

The first review-order item has progressed from broad direction to a documented foundational draft. The owner-approved principles are recorded in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md). The base success-probability curve is intentionally still open and is the next design step. This update records design progress only; it does not authorise implementation or claim that the full Character system is complete.

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
| Attributes and checks | Shared foundational draft written; attributes, task-equivalent primary preparation, secondary bonuses, Luck model, and three outcomes have owner-approved principles | Define the base success-probability curve, then tune remaining open coefficients and reconcile combat-specific checks; see [07.1](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md) |
| Skills and abilities | Candidate inventory captured; action-based growth direction captured | Define skill taxonomy after attributes/checks |
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


# Second detailed design pass: action, skill, and attribute relationship inventory

## Purpose and level of detail

This is a preliminary relationship inventory for design discussion, not a complete implementation specification. It records compact, player-facing action families and candidate skills. It deliberately avoids creating a separate skill for every context or object variation.

- An **action** is a recognisable thing the character can do in gameplay.
- A **skill** is a practiced capability that may improve how well the character performs one or more related actions.
- An **attribute** is a broad character quality that may affect multiple actions and skills.
- An action may develop a skill, one or more attributes, both, or (where no dedicated skill exists) attributes alone.
- The listed attributes are candidates for influence, not confirmed formulas or a promise that each action always increases them.
- No numerical values, coefficients, progression thresholds, caps, or exact check formulas are specified here.

The current candidate attribute set is **Strength, Endurance, Dexterity, Perception, Intelligence, Will, and Luck**. These remain subject to review against the completed action inventory.

## 1. Movement and physical actions

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Accelerated run / sprint | Running | Endurance, Dexterity | Accelerated running consumes Stamina; ordinary walking and ordinary running do not. Carrying a high or overloaded load increases exertion cost. |
| Jump | Jumping | Strength, Dexterity | Skill may improve jump capability within reasonable physical limits; do not create separate skills for different obstacles. |
| Climb | Climbing | Strength, Endurance, Dexterity | Include as a candidate pending scope/implementation assessment. One skill covers climbable walls, fences, rocks, and similar surfaces. |
| Sneak | Stealth | Dexterity, Perception | Skill progression should reflect meaningful stealth practice, especially where detection is possible, not simply toggling a stance in a safe empty area. |
| Move while overloaded | Carrying Capacity / Load Carrying (working name) | Strength, Endurance | Inventory weight is relative to character capacity; “heavy item” is not a separate item category required by this rule. |
| Ordinary walking and ordinary running | No dedicated skill decided | Endurance may develop through appropriate exertion rules | Neither ordinary walking nor ordinary running consumes Stamina. Accelerated running does. Do not create a Walking skill without a clear gameplay need. |
| Strike or attempt to break a supported object using a suitable held item | No universal “destruction” skill decided | Strength; possibly Dexterity | Only explicitly supported objects can be broken; this does not imply that everything in the world is destructible. |

## 2. Gathering and resource extraction

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Fell trees / gather wood | Woodcutting | Strength, Endurance | One skill for the activity, not separate skills for branches, firewood, and tree types. |
| Mine minerals with a pickaxe | Mining | Strength, Endurance | Tools, location, and resource rules may also affect outcomes. |
| Gather herbs | Herbalism | Perception, Dexterity; Intelligence is a candidate | Higher skill may improve gathering results and access to rarer finds; rare-herb finding is not a separate skill. |
| Collect materials from defeated creatures | Creature Harvesting / Butchery (working name) | Dexterity, Perception | Exact scope and naming remain open. |
| Catch fish | Fishing | Dexterity, Perception, Intelligence (candidates) | One profession/skill for fishing activities. |
| Find hidden stashes, chests, and concealed objects | Hidden Cache Search (working name) | Perception, Luck | Skill may improve the chance or strength of an in-world audio/visual cue when a cache is nearby. This is a candidate mechanic, not a confirmed UI design. |

## 3. Crafting professions

These are profession-like skills. Their exact recipes, tools, quality rules, and V1 scope will be designed separately.

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Make clothing and work with fabric | Tailoring | Dexterity, Intelligence | Confirmed as a profession candidate. |
| Process hides and make leather goods | Leatherworking | Dexterity, Intelligence | Confirmed as a profession candidate. |
| Forge weapons, tools, and metal goods | Blacksmithing | Strength, Dexterity, Endurance | Confirmed as a profession candidate. |
| Make jewellery | Jewellery Making | Dexterity, Intelligence | Confirmed as a profession candidate. |
| Cook food and meals | Cooking | Dexterity, Intelligence | Explicitly included by the owner. |
| Brew potions and alchemical mixtures | Alchemy | Intelligence, Dexterity | Explicitly included by the owner. |
| Enchant items | Enchanting | Intelligence; other influences TBD | Explicitly included by the owner. Exact relationship with Pathways and supernatural power remains open. |
| Create talismans | Talisman Crafting (candidate) | Intelligence, Dexterity (candidates) | May overlap with Enchanting or Alchemy; do not create as a separate skill until its distinct gameplay role is established. |
| Identify items, herbs, minerals, and artefacts | Identification / Appraisal (working name) | Intelligence, Perception | A profession-like skill. Greater skill should reduce the chance that an item remains unidentified and may reveal quality, rarity, properties, and description. Exact appraisal versus identification split is open. |

## 4. Social and dialogue actions

The existing [06 Approaches](06%20Approaches.md) remain the larger decision framework. The following are candidate social skills, not replacements for those Approaches.

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Persuade through arguments or reasons | Persuasion | Intelligence, Will | Distinct from negotiation if the game supports both as separate outcomes. |
| Intimidate | Intimidation | Strength, Will | Can rely on credible threat, reputation, context, or force; does not always require a physical check. |
| Lie | Deception | Intelligence, Will | Used when a lie is materially contested. |
| Find a compromise or agreement | Diplomacy | Intelligence, Will | Remains a candidate distinct from Persuasion. |
| Negotiate prices | Trading / Haggling | Intelligence, Will | Could connect to merchant access and economy rules. |
| Notice deception or read a person | Insight (working name) | Perception, Intelligence | Exact boundaries versus world knowledge and dialogue context remain open. |
| Resist psychological pressure | Composure / Self-Control (working name) | Will | Could be handled directly by Will rather than a separate skill. |
| Elicit information | Information Elicitation (working name) | Intelligence, Perception | May overlap with Persuasion, Deception, Diplomacy, or dialogue-specific choices; needs later consolidation. |

## 5. Combat and supernatural abilities

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Fight with melee weapons | Melee Weapon Proficiency (working name) | Strength, Dexterity | Whether weapon types have separate skills is undecided. |
| Fight with ranged weapons | Ranged Weapon Proficiency (working name) | Dexterity, Perception | Whether bows, thrown weapons, and other ranged weapons are separate is undecided. |
| Evade an attack | Evasion | Dexterity, Perception | Candidate skill; may instead be resolved from attributes and combat rules. |
| Block an attack | Blocking | Strength, Dexterity | Candidate skill; may instead be a combat action governed by equipment and attributes. |
| Use magical or Pathway abilities | Ability-specific system, not a generic “combat abilities” skill | Depends on the ability | A separate design pass must cover supernatural powers, costs, requirements, combat use, and non-combat use. Do not collapse all magic into one skill by default. |
| Maintain composure under combat pressure | No separate skill decided | Will | May be handled directly through Will and condition rules. |

## 6. Exploration and world interaction

| Action | Candidate skill | Candidate attribute influences | Notes |
|---|---|---|---|
| Search for hidden caches and chests | Hidden Cache Search (working name) | Perception, Luck | Same skill as the gathering/exploration entry above; avoid duplicate definitions. |
| Read books, notes, and documents | No dedicated skill proposed | Intelligence may affect interpretation; reading can grant information directly | Reading should not automatically become a skill merely because it is an action. |
| Identify an item and learn its properties | Identification / Appraisal (working name) | Intelligence, Perception | Treat as a profession-like skill, not a generic “notice details” action. |
| Pick locks and open suitable locked mechanisms | Lockpicking (candidate) | Dexterity, Intelligence | Keep as a candidate skill. Context and lock type may affect difficulty without creating a separate skill for each lock. |
| Steal an item from an NPC or the world | Theft / Sleight of Hand (working name) | Dexterity; Stealth and Luck may also matter contextually | Determine later whether Theft is a distinct skill or part of a broader Sleight of Hand skill. Stealth may affect avoiding detection, but the concepts should not be conflated. |
| Discover or disarm a trap | No dedicated trap skill for now | Perception, Dexterity, Intelligence may influence the action | Owner requested removal of separate Trap Detection and Trap Disarming skills. Such actions may still train relevant attributes; this does not guarantee trap-heavy gameplay. |

## 7. Actions that do not currently need their own skill

- General observation / “noticing details” as a player-facing activity: no dedicated skill. Do not invent a generic Observation skill without a concrete, visible gameplay loop.
- Reading books, documents, and records: no dedicated reading skill; information comes from the source, while Intelligence may matter for interpretation if a later rule needs it.
- Separate skills for jumping over different obstacle types, climbing different surface types, carrying specific categories of heavy objects, or breaking each kind of object: not proposed.
- Separate skills for finding rare herbs or determining whether a particular herb is rare: not proposed; Herbalism and Identification have different potential roles.
- Dedicated Trap Detection and Trap Disarming skills: removed from the current candidate list. Relevant attributes may still matter if these actions exist.
- Generic universal destruction skill and quick item handling: not proposed.

## 8. Progression principles applying to this inventory

1. The list is organised from actions to skills to attributes: actions are the most numerous layer, skills are fewer or roughly comparable in count, and attributes are a small shared set.
2. A skill may cover several closely related actions; an action may develop more than one skill where the relationship is justified. Avoid artificial one-skill-per-context granularity.
3. Attributes can affect many actions. A high attribute may help a character perform an action even with little practice in the associated skill.
4. Repeated actions can develop relevant skills and appropriate attributes, but an action does not necessarily increase every associated attribute every time.
5. Attribute development from a particular activity must eventually use diminishing returns or another bounded contribution rule. Anti-abuse remains required; exact mechanisms are undecided.
6. Skill growth should have observable practical effects. Examples include movement capability, resource yield, rarity of finds, crafting results, identification success, or social outcomes, depending on the skill.
7. Hidden-cache search may provide an audio/visual proximity cue at higher skill levels; this is a proposed direction, not a locked mechanic.
8. Do not expose exact growth formulas to the player. Attribute and skill changes should be visible while calculations remain hidden.
9. Player characters and NPCs share the underlying Character model. NPC skill values and development may be generated or simulated without exposing player-facing progression UI.

## 9. Current owner decisions and remaining questions

### Confirmed direction in this design pass

- Keep action families broad and recognisable rather than creating a separate action/skill for every situation.
- Keep Running, Jumping, Sneaking/Stealth, and (provisionally) Climbing as compact movement/action concepts.
- Use profession-like skills for Woodcutting, Mining, Herbalism, creature resource harvesting, and Fishing.
- Add Cooking, Alchemy, and Enchanting.
- Keep Identification/Appraisal as a profession-like skill for learning item, artefact, herb, and mineral properties.
- Keep Lockpicking as a candidate skill.
- Add stealing as a gameplay action; whether its skill is Theft or Sleight of Hand remains open.
- Do not add a generic Observation skill or separate Trap Detection/Trap Disarming skills at this stage.
- Reading documents does not require a dedicated skill.
- Hidden-cache discovery may use Perception and Luck, with a possible skill-based proximity cue.
- Combat includes melee, ranged, and supernatural abilities; supernatural abilities need a separate design pass.
- The current seven attributes remain candidates; no final attribute list or formulas are locked.

### Still open

- Final names and boundaries for load carrying, creature harvesting, hidden-cache search, identification/appraisal, theft/sleight of hand, and social skills.
- Whether Climbing is included in V1 and how it will be implemented.
- Which attributes are trained by each action versus merely used to resolve it.
- Whether combat weapon proficiency, Evasion, and Blocking are skills or are resolved through attributes, equipment, and combat actions.
- Whether Talisman Crafting is a distinct profession or part of Enchanting/Alchemy.
- How Identification reveals rarity, quality, description, and artefact/mineral properties.
- How a Hidden Cache Search skill affects audio/visual cues and whether that cue fits the intended exploration experience.
- Which candidate actions and professions are required for V1 versus deferred.
- The interaction between ordinary skills, attributes, Pathways, and supernatural abilities.

## Provisional status update

- Action/skill/attribute relationship inventory: first owner-reviewed pass recorded; still provisional.
- Candidate attribute list: seven working candidates; not final.
- Profession list: Woodcutting, Mining, Herbalism, creature resource harvesting, Fishing, Tailoring, Leatherworking, Blacksmithing, Jewellery Making, Cooking, Alchemy, Enchanting, and Identification/Appraisal captured as candidates.
- Exploration: generic observation removed; dedicated trap skills removed for now; lockpicking and theft retained as candidates.
- Combat: melee, ranged, and supernatural ability tracks identified; full combat/ability design deferred.
- Numeric formulas, exact growth rules, caps, anti-abuse mechanism, and V1 scope remain open.
