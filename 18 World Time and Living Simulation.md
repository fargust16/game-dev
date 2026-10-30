# 18 World Time and Living Simulation

Version: 0.1

Status: Draft

Last Updated: 2026-10-30

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)

Referenced By:
[17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), future NPC AI, encounter, settlement, world-event, and simulation specifications.

---

# Purpose and authority

This document records the design of the shared game-time model and the living-world simulation.

It is a design draft, not an implementation specification or implementation authorisation. Owner-approved decisions are distinguished from provisional examples and unresolved tuning.

The central principle is that the world continues to exist and change independently of the player. Tactical combat changes how the player and combatants are controlled; it does not create a separate frozen world or stop the shared game clock.

# 1. Shared game time

## Approved direction

- The world uses one shared game-time source. Systems must not maintain unrelated clocks for combat, NPCs, conditions, activities, or world events.
- **One world time tick represents one in-game minute.**
- The same game-time unit applies in free exploration and tactical combat.
- The one-minute tick is a game-time unit, not necessarily one minute of real elapsed time.
- The real-time-to-game-time conversion rate is not yet decided. A faster-than-real-time world clock (for example, one game minute passing in roughly 15–20 real seconds) was discussed as an initial possibility, but is not approved.
- The shared clock continues across transitions between free exploration and tactical combat. Entering or leaving combat must not reset or discard elapsed time or the current partial tick.

## Tick boundary

At each one-minute boundary, the world performs a coordinated update of relevant systems. Depending on the system, this may include:

- reevaluating NPC goals, plans, and schedules;
- updating long-running activities and their progress;
- processing timed character conditions and recovery;
- updating world events, threats, faction/settlement consequences, and other time-dependent systems;
- resolving scheduled or pending simulation work.

A tick boundary is a shared reevaluation point, not a requirement that every NPC discard its current task and invent a new one. NPCs normally continue their current intention until it is completed, invalidated, or new information gives them a reason to change it.

# 2. Continuous execution and event-driven reactions

The world has two complementary update mechanisms:

1. **Ongoing execution:** NPCs and other systems continue carrying out current tasks between tick boundaries. Examples include travelling, gathering resources, trading, crafting, resting, sleeping, socialising, or remaining idle.
2. **Event-driven reaction:** important events are processed when they occur; they do not have to wait for the next one-minute boundary.

Examples of significant events include an NPC detecting a nearby monster, an attack, an alarm, a fire, a settlement being threatened, or an NPC receiving credible information about danger.

An event may interrupt or invalidate a current task and trigger an immediate reaction. The NPC's broader goal and next plan can then be reevaluated at the next tick boundary, or earlier if another significant event requires it.

The implementation must avoid both extremes:
- NPCs must not be forced to reconsider every decision from scratch every frame or every minute.
- NPCs must not remain blindly committed to a task after an important event makes it unsafe or irrelevant.

# 3. NPC goals, tasks, and persistent lives

NPCs should act according to goals, current tasks, schedules, capabilities, and information available to them.

Examples of ordinary activities include:
- a woodcutter travelling to a forest and gathering wood;
- an NPC trading, visiting a tavern, socialising, wandering, resting, or sleeping;
- an NPC travelling between locations to fulfil a responsibility;
- an NPC reacting to a threat by fleeing, hiding, warning others, or fighting.

Schedules can guide ordinary routines, but NPC behaviour must not be limited to a rigid sequence of scripted time slots. Current circumstances and newly received information can change what an NPC decides to do.

## Settlement displacement and return

NPCs are not automatically deleted or considered dead when their home settlement is captured or becomes unsafe.

- An NPC who can escape may travel to a suitable nearby settlement.
- After reaching refuge, the NPC may take up appropriate activities or responsibilities there, subject to the settlement's conditions and the NPC's capabilities.
- If the original settlement is liberated or becomes safe again, the NPC may decide to return.
- Whether an NPC survives, escapes, finds refuge, or returns depends on the actual events and circumstances; none of these outcomes is guaranteed.

Detailed settlement capacity, refugee logistics, faction policy, and NPC reassignment rules remain open for their respective system designs.

# 4. Tactical combat uses the shared clock

## Approved direction: round-based time

- A complete tactical combat round, in which all currently participating combatants have had their opportunity to act in initiative order, advances the shared world clock by **one in-game minute**.
- The player's real-world deliberation time does not advance game time under the currently selected model.
- This is the preferred initial model (Variant A). A real-time action deadline or hybrid real-time combat clock is not part of the current design.
- The world continues to process relevant events and external simulation while combat is underway. Tactical combat is not a separate time bubble.
- If combat ends partway through a world-time tick, the player returns to free control immediately. The shared clock is not reset; the remaining portion of the current tick continues to be accounted for.

## Combatants joining an active encounter

Allies, enemies, and other NPCs may join an active encounter when they physically reach the combat area and become involved.

- A joining actor receives an Initiative value and is inserted into the encounter's initiative order.
- Depending on their position relative to the current initiative cursor, they may act during the current round or wait until the next round.
- Joining does not require restarting the encounter or resetting the current round.
- The exact insertion/tie-breaking procedure and the representation of arrivals during a round remain to be specified.

## Leaving and re-entering an encounter

Combatants may attempt to flee rather than continue fighting. The player and NPCs should use the same underlying spatial escape principle.

- The current proposal is that an actor can leave the tactical encounter after moving beyond a defined separation distance from the nearest hostile combatant.
- A distance of **more than 10 arena cells** was suggested as an initial reference, not a final approved value.
- Leaving the encounter removes that actor from the active tactical participation; it does not necessarily end combat for the remaining participants.
- If a fleeing actor is later found, pursued, or caught by a hostile actor, a new tactical encounter may begin.
- NPCs may choose to flee when the threat is too great, using their judgement, goals, and available information.

The exact escape measurement, line-of-sight rules, pursuit behaviour, and re-engagement conditions remain open. This rule must be reconciled with the arena transition and source-scene rules in [16 Architecture Pivot](16%20Architecture%20Pivot.md).

# 5. Information and awareness

NPCs are not omniscient. An NPC may react only to information it has actually received or detected through a defined channel.

Potential information channels include:

- **Deity / prayer communication:** a character reports an event to their deity; the deity may then communicate relevant information to other characters or agents.
- **Radio and broadcasts:** information is transmitted to listeners within the applicable network and reception conditions.
- **Familiars and observers:** a familiar or assigned watcher observes an area and reports what it detects.
- **Direct perception:** an NPC hears, sees, or otherwise senses an event within the limits of its senses and circumstances.
- **Specific abilities:** supernatural or other explicitly defined abilities can detect, monitor, or report particular events or areas.

Information transmission is not automatic universal knowledge. Each channel needs a sender/source, recipient or eligible audience, content, timing, and any relevant limits. The detailed rules belong to the Information Sources and ability systems.

# 6. Timed conditions and character recovery

Character conditions use the same shared game-time model. They are not measured by an independent combat-only clock.

The current combat-condition duration proposal is:

| Condition | Base duration / natural resolution |
|---|---|
| Poison | No fixed expiry; persists until actively removed or independently overcome |
| Bleeding | No fixed expiry; persists until actively stopped or independently overcome |
| Stun | 2 world ticks |
| Fear | 2 world ticks |
| Immobilisation | 3 world ticks |
| Disarm | 3 world ticks |
| Blindness | 3 world ticks |
| Disorientation | 3 world ticks |

These are the currently selected starting values for design and balance discussion, not final balance values. Poison and Bleeding retain their distinct effects as defined in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md).

## Endurance and condition resistance

- Endurance influences both the chance to resist a condition when it is applied and the chance to overcome an already active condition early.
- For Poison and Bleeding, the character has a low chance of natural removal at each tick, increasing with Endurance. At zero Endurance, there is no natural-removal chance under the current proposal.
- For timed conditions, Endurance increases the chance of early removal at each tick. The influence is condition-specific: severe and dangerous conditions such as Stun and Fear should be more likely to be overcome early than less immediately dangerous restrictions such as Immobilisation or Disarm.
- Equipment, abilities, and temporary effects may add to resistance or early-removal chances.
- Even a character with high Endurance may fail an early-removal check and remain affected for the full base duration.
- Exact formulas, probability curves, caps, stacking rules, and the timing/order of checks remain open for balance design.

# 7. Design constraints

- One shared game clock is authoritative for all time-dependent systems.
- Tactical combat advances game time by complete rounds, not by the player's real-time deliberation.
- Important events can trigger immediate reactions between tick boundaries.
- NPCs pursue persistent goals and tasks; they do not need to reconsider everything every frame or every tick.
- NPC awareness must be based on explicit information channels and perception, never global omniscience.
- The world must continue to evolve during combat, including the possibility of reinforcements arriving.
- NPCs displaced by settlement capture should be able to survive, relocate, and potentially return; they must not be automatically culled merely because their home is lost.
- Prefer a simple, expandable simulation over a costly full simulation of every detail. System-specific update frequency and abstraction can vary, but must remain consistent with the shared world clock.

# 8. Remaining design questions

1. What real-time duration corresponds to one in-game minute, and does it vary by mode or setting?
2. How is partial-tick progress represented and preserved across scene transitions and combat?
3. Which systems execute continuously, which are event-driven, and which update only at tick boundaries?
4. How are long-running player and NPC actions represented, interrupted, paused, and resumed?
5. What is the final encounter escape distance and how is it measured in the arena?
6. How are actors inserted into initiative when they arrive during an active round?
7. How does the arena handle arrivals, departures, pursuit, and re-engagement without breaking the source-scene model?
8. What information can each communication channel transmit, to whom, how quickly, and with what limits?
9. How are NPC goals, task selection, danger assessment, refuge selection, and return decisions governed?
10. What are the final per-condition durations, Endurance curves, resistance rules, and early-removal probabilities?
11. How much of the world is simulated in detail at once, and how are distant or unloaded NPCs abstracted without losing important consequences?

---

# Change discipline

This document is a design authority only after its status and decisions are deliberately promoted. Until then, implementation agents must treat unresolved values and mechanics as open and must not invent final formulas or behaviours.
