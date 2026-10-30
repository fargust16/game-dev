# 18 World Time and Living Simulation

Version: 0.2

Status: Draft — owner-approved design direction, incomplete tuning

Last Updated: 2026-10-30

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
[10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), future NPC AI, encounter, settlement, medical, and world-event specifications.

---

# Purpose and authority

This document records the shared game-time model and living-world simulation.

It is a design draft, not an implementation specification or implementation authorisation. Owner-approved decisions are distinguished from provisional examples and unresolved tuning.

The world continues to exist and change independently of the player. Tactical combat is turn-based, but it does not freeze the world: the shared simulation advances at the end of each complete combat round.

# 1. Shared game time

## Owner-approved direction

- The world uses one shared game-time source. Systems must not maintain unrelated clocks for combat, NPCs, conditions, activities, or world events.
- **One world time tick represents one in-game minute.**
- **Initial pacing: one game tick passes every 10 seconds of real time**, making a 24-hour game day last four real hours. This is the owner-preferred starting value and may be tuned after playtesting.
- The same game-time unit measures free exploration, timed activities, character conditions, and the world simulation.
- Entering or leaving combat must not reset or discard elapsed time or partial-tick progress.
- Player deliberation during a paused dialogue or an active tactical turn does not advance game time.

## Tick boundary

At each one-minute boundary, the world performs a coordinated update of relevant systems. This may include:

- reevaluating NPC goals, plans, and schedules;
- updating long-running activities and their progress;
- processing timed character conditions, natural recovery, and injuries;
- updating world events, threats, faction/settlement consequences, and other time-dependent systems;
- resolving scheduled or pending simulation work.

A tick boundary is a shared reevaluation point, not a requirement that every NPC discard its current task and invent a new one. NPCs normally continue their current intention until it is completed, invalidated, or new information gives them a reason to change it.

# 2. Real-time activities and interaction pauses

During ordinary free exploration, the shared game clock advances continuously at the initial rate of one game tick per 10 real seconds.

## Gathering and resource nodes

- Gathering is an interruptible repeated action, not a single locked animation or a mandatory full-duration task.
- A resource node (tree, mineral deposit, plant patch, fishing spot, etc.) contains a finite available resource reserve.
- Each gathering cycle has an animation/action duration and yields a quantity of the relevant resource. The node is depleted when its reserve is exhausted.
- The player may stop gathering at any time; starting a gathering action does not commit the player to deplete the node.
- Strength and relevant profession skill may affect yield per cycle according to their respective system rules. Exact duration, yield, and node reserve are data-driven and remain to be designed.
- Special and rare finds may be included in gathering reward tables. Their availability and probability depend on the particular resource node/location, material or species, and relevant profession skill. Rare locations may contain distinctive resources or items.
- Fishing is a periodic catch action rather than a fishing minigame. At each completed fishing interval, the player receives a catch determined by the fishing location and relevant skill. Possible catches include ordinary fish, miscellaneous items, and fishing-specific rare finds.

## Trading and dialogue

- Direct dialogue with an NPC pauses the world simulation, allowing the player to consider dialogue choices without NPC schedules or world events advancing.
- A shop/trading interface that interacts with a merchant's shop or stock rather than holding a direct conversation with the NPC does not pause the world.
- Shops and merchants operate according to their schedules and availability. If a shop closes or its merchant leaves while the player is using its interface, the interface may close and the transaction ends.
- NPCs are not indefinitely reserved for the player. An NPC may spend only a limited period waiting or engaging with the player before leaving or returning to their own goals. The exact default interaction window and inactivity timeout remain open.

## Crafting

- Crafting recipes have defined durations expressed in game-time units or fractions of a tick.
- Simple items and small batches should complete quickly; quantity and recipe complexity affect duration.
- Ordinary complex crafting may take up to a small number of ticks, but should not create long real-time waits.
- The current target is that even the most complex ordinary craft should take no more than about six game ticks (approximately one real minute at the proposed time scale).
- Exceptional high-stakes alchemical recipes, such as a potion that raises a Pathway stage, may use the full six-tick preparation period and may include a dedicated quality-sensitive minigame. This is a special recipe category, not a baseline crafting requirement.
- The quality and effectiveness relationship for such potions remains to be designed.

# 3. Sleep, rest, and sleep safety

## Sleep duration and world simulation

- A sleep session advances game time by approximately **6–8 in-game hours** (360–480 world ticks).
- The player may choose to sleep during the day or at night, subject to circumstances.
- NPCs also sleep and rest according to their needs and schedules; sleep is part of the living-world simulation, not a player-only time skip.
- The entire external world continues to be simulated during the sleep interval. NPCs move and act, schedules advance, and world events may occur.
- Sleep is interruptible. The player does not receive the full time skip or its full recovery benefits if sleep ends early.

## Recovery during sleep

- A completed, uninterrupted sleep session fully restores the character's ordinary HP **only if no active condition or other effect prevents health recovery**.
- Sleep also restores stamina. The rate and additional benefits depend on sleep comfort; exact values remain open.
- If Poison remains active, health and stamina recovery remain blocked for as long as the poison condition persists.
- If Bleeding remains active, it continues to remove health during sleep and can kill the character before waking.
- If a recovery-blocking condition is removed partway through sleep, the corresponding recovery may begin from that point onward. A condition is never treated as removed merely because the character slept.
- A curse or another explicitly defined effect may prevent or alter recovery. Such exceptions must be stated by that effect's own rules.
- Sleep does not automatically heal fractures, serious wounds, or other lasting injuries. These may have their own very low natural-recovery chance, but normally call for First Aid or a specialist.

## Comfort and safety are separate

- **Sleep comfort** describes the quality of the resting conditions. It may affect stamina recovery and grant temporary rest benefits, potentially including attribute bonuses. The exact comfort score, sources, levels, and benefits remain open.
- **Sleep safety** describes the risk of detection, disturbance, or attack. It is independent of comfort.
- A comfortable bed in an inn may offer high comfort and high safety; a tent in the wilderness may offer variable comfort and greater exposure; sleeping outdoors in a dangerous area may be unsafe even if the character has suitable bedding.
- No location is assumed absolutely safe unless its actual protections and world circumstances justify that conclusion.

## Interruptions, alarms, and ambushes

Sleep may be interrupted by events such as:

- a monster, animal, or hostile NPC discovering the sleeping character or camp;
- a raid, attack, or alarm in a settlement;
- fire, destruction of shelter, or another environmental emergency;
- an NPC deliberately waking the character.

An interruption resolves the elapsed portion of the sleep interval and all world events up to that moment. Recovery and condition checks apply only to the time that actually passed.

An interruption does not always begin combat. For example, an alarm may wake the player before enemies arrive, allowing the player to respond or move to another location.

If a hostile actor reaches and attacks the sleeping character before the character can wake and react:

- the attacker may resolve an opening attack before ordinary combat begins;
- the sleeping character's Initiative does not prevent this opening attack;
- the character may take damage before the tactical encounter starts;
- after the opening ambush resolution, normal tactical combat begins and the ordinary Initiative order applies.

A group ambush may allow more than one attacker to act before the ordinary turn order begins. The number of opening attacks, detection rules, wake-up response, and exact ambush resolution remain to be specified. Do not assume that every sleep interruption grants a free attack.

# 4. Natural recovery, conditions, and injuries

All time-dependent conditions and natural recovery use the shared world clock. They do not use an independent combat-only timer.

## Natural health recovery

- Natural HP recovery during ordinary activity is deliberately very slow.
- A starting balance candidate is approximately **1% of maximum HP per 10 world ticks** (10 in-game minutes). This is a proposal for playtesting, not a final approved rate.
- Natural recovery must not make repeated combat sustainable through a short wait. Health is a limited resource and players are expected to use exploration, preparation, First Aid, rest, and medical services rather than treating combat as the only activity.
- Sleep is a distinct recovery mode: it can fully restore ordinary HP when recovery is not blocked, rather than using the slow ordinary recovery rate.

## Poison and Bleeding

- Poison and Bleeding have no guaranteed automatic expiry.
- At each world tick, an active Poison or Bleeding condition may receive a probability check for natural removal. The check may succeed or fail; sleeping does not guarantee success.
- Endurance influences the natural-removal chance. Relevant medical items, treatment, and other explicitly defined modifiers may also influence it.
- If the check fails, the condition remains active and continues applying its effects.
- **Poison** blocks HP and stamina recovery while active. If it remains active throughout sleep, the character does not wake with fully restored HP.
- **Bleeding** continues to cause health loss while active and may reduce HP to zero, including during sleep.
- First Aid items and treatment can stop or neutralise these conditions according to their specific rules.
- Exact per-tick probabilities, Endurance curves, item modifiers, and treatment rules remain open.

## Other timed conditions

The current starting duration proposal is:

| Condition | Starting duration / natural resolution |
|---|---|
| Stun | 2 world ticks |
| Fear | 2 world ticks |
| Immobilisation | 3 world ticks |
| Disarm | 3 world ticks |
| Blindness | 3 world ticks |
| Disorientation | 3 world ticks |

These are initial design/balance values, not final tuned durations. Endurance may improve the chance of early removal; condition-specific probabilities remain open.

## Injuries and lasting consequences

- Injuries such as fractures and serious wounds are distinct from the ordinary HP pool.
- Sleep restores ordinary HP but does not automatically remove these injuries.
- An injury may have a very low chance of natural healing at each world tick. The chance may increase as more time passes with the injury, but must remain low enough that relying on spontaneous healing is unattractive.
- First Aid may stabilise, dress, immobilise, or otherwise treat injuries within the profession's scope. Some injuries require a doctor or another specialist.
- A character may therefore have full HP while still suffering a fracture, movement penalty, or other lasting injury.
- Injury categories, severity, natural-healing curves, penalties, and exact treatment scope remain open.

## First Aid and professional treatment

First Aid is a profession focused on preparing and applying practical medical supplies, not a universal source of HP restoration.

Potential First Aid products and actions include:

- bandages and dressings;
- antidotes;
- materials and procedures to stop bleeding;
- splints or braces to stabilise fractures and reduce their penalties;
- other basic treatments for concrete physical consequences of accidents or combat.

First Aid is intended to let a prepared character manage common conditions and continue travelling or exploring. It does not replace a doctor and does not automatically cure every wound, disease, curse, or supernatural effect.

- No HP-restoring potions are part of the current design.
- Ordinary HP is restored through sleep (when recovery is not blocked) or appropriate medical services. Any First Aid action that affects HP must be a concrete treatment of a wound, not an instant generic healing consumable; whether and how much such treatment restores HP remains open.
- Doctors and medical facilities can provide more complete treatment for money or another appropriate cost.
- Serious, complex, or supernatural conditions may require specialists, churches, deities, or a dedicated investigation into the condition's nature.
- Curses are not naturally removed by ordinary sleep or routine First Aid. Their removal belongs to a future curse/supernatural treatment system.

# 5. Continuous execution and event-driven reactions

The world has two complementary update mechanisms:

1. **Ongoing execution:** NPCs and other systems continue carrying out current tasks between tick boundaries. Examples include travelling, gathering resources, trading, crafting, resting, sleeping, socialising, or remaining idle.
2. **Event-driven reaction:** important events are processed when they occur; they do not have to wait for the next one-minute boundary.

Examples of significant events include an NPC detecting a nearby monster, an attack, an alarm, a fire, a settlement being threatened, or an NPC receiving credible information about danger.

An event may interrupt or invalidate a current task and trigger an immediate reaction. The NPC's broader goal and next plan can then be reevaluated at the next tick boundary, or earlier if another significant event requires it.

The implementation must avoid both extremes:
- NPCs must not be forced to reconsider every decision from scratch every frame or every minute.
- NPCs must not remain blindly committed to a task after an important event makes it unsafe or irrelevant.

# 6. NPC goals, tasks, and persistent lives

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

# 7. Tactical combat advances the shared world at round boundaries

## Owner-approved round/time relationship

- Tactical combat takes place on the square-grid Combat Arena and uses the Initiative and AP turn order defined in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md).
- **One complete combat round advances the shared world clock by exactly one world tick (one in-game minute).**
- A round is complete only after every currently eligible participant has had their opportunity to act or has otherwise been resolved for that round.
- **The world does not advance during an individual character's turn.** The player may consider and submit actions without real-time pressure.
- Once the round ends, one tick passes and the wider simulation updates: NPCs move and act, schedules and timed conditions advance, events resolve, and nearby actors may approach the encounter.
- Reinforcements may physically reach the combat area between rounds and join the active encounter. They receive Initiative and are inserted into the encounter order according to the insertion rule still to be specified.
- The number of rounds is the duration of the encounter in world time: a typical even fight is expected to take roughly 10–15 rounds; a difficult fight may take 20–30; a very simple fight may take 3–5. These are pacing expectations, not hard encounter limits.
- A one-round fight is possible but should be exceptional.
- The player's real-world deliberation time during a turn does not advance game time.

## Combatants joining, leaving, and re-entering

Allies, enemies, and other NPCs may join an active encounter when they physically reach the combat area and become involved.

- A joining actor receives an Initiative value and is inserted into the encounter's initiative order.
- Depending on their position relative to the current initiative cursor, they may act during the current round or wait until the next round.
- Joining does not require restarting the encounter or resetting the current round.
- The exact insertion/tie-breaking procedure and representation of arrivals during a round remain to be specified.

Combatants may attempt to flee rather than continue fighting. The player and NPCs should use the same underlying spatial escape principle.

- The current proposal is that an actor can leave the tactical encounter after moving beyond a defined separation distance from the nearest hostile combatant.
- A distance of **more than 10 arena cells** was suggested as an initial reference, not a final approved value.
- Leaving the encounter removes that actor from active tactical participation; it does not necessarily end combat for the remaining participants.
- If a fleeing actor is later found, pursued, or caught by a hostile actor, a new tactical encounter may begin.
- NPCs may choose to flee when the threat is too great, using their judgement, goals, and available information.

The exact escape measurement, line-of-sight rules, pursuit behaviour, and re-engagement conditions remain open. This rule must be reconciled with the arena transition and source-scene rules in [16 Architecture Pivot](16%20Architecture%20Pivot.md).

# 8. Information and awareness

NPCs are not omniscient. An NPC may react only to information it has actually received or detected through a defined channel.

Potential information channels include:
- **Deity / prayer communication:** a character reports an event to their deity; the deity may then communicate relevant information to other characters or agents.
- **Radio and broadcasts:** information is transmitted to listeners within the applicable network and reception conditions.
- **Familiars and observers:** a familiar or assigned watcher observes an area and reports what it detects.
- **Direct perception:** an NPC hears, sees, or otherwise senses an event within the limits of its senses and circumstances.
- **Specific abilities:** supernatural or other explicitly defined abilities can detect, monitor, or report particular events or areas.

Information transmission is not automatic universal knowledge. Each channel needs a sender/source, recipient or eligible audience, content, timing, and any relevant limits. The detailed rules belong to the Information Sources and ability systems.

# 9. Design constraints

- One shared game clock is authoritative for all time-dependent systems.
- One in-game minute is one world tick; the initial real-time pacing is 10 seconds per tick.
- Direct dialogue pauses the world; shop-stock interfaces do not.
- Sleep advances 6–8 in-game hours and simulates the wider world throughout.
- Sleep can be interrupted by world events, including alarms and hostile attacks.
- Sleep restores ordinary HP only when active conditions do not block recovery; it does not automatically cure injuries or curses.
- Poison and Bleeding have probabilistic natural removal, never guaranteed removal by sleep; their active effects continue until removed.
- First Aid treats specific physical conditions and injuries; it is not universal healing. No HP-restoring potions.
- Tactical combat advances the shared world exactly once per completed round, never during an individual turn.
- Nearby NPCs and reinforcements can move and react at round boundaries.
- NPC awareness must be based on explicit information channels and perception, never global omniscience.
- NPCs pursue persistent goals and tasks; they do not need to reconsider everything every frame or every tick.
- Prefer a simple, expandable simulation over a costly full simulation of every detail. System-specific update frequency and abstraction can vary, but must remain consistent with the shared world clock.

# 10. Remaining design questions

1. How is partial-tick progress represented and preserved across scene transitions, paused dialogue, and combat?
2. Which systems execute continuously, which are event-driven, and which update only at tick boundaries?
3. How are long-running player and NPC actions represented, interrupted, paused, and resumed?
4. What are the final gathering-cycle durations, yields, node reserves, and rare-find probability curves?
5. What are the final shop interaction windows and NPC waiting/inactivity timeouts?
6. What are the final sleep duration rules within the 6–8 hour range, and what can the player choose?
7. What is the comfort score, how is it calculated, and what temporary rest bonuses can it grant?
8. How are sleep safety, detection, waking, alarm response, and ambush opening attacks resolved?
9. What is the ordinary natural HP recovery rate and how does it interact with rest outside sleep?
10. What are the Poison and Bleeding natural-removal probabilities, Endurance curves, item modifiers, and treatment rules?
11. What injury categories exist, what penalties do they impose, and what are their natural-healing probabilities and time curves?
12. What exactly can First Aid treat or stabilise, and which injuries require a doctor or specialist?
13. What are the exact HP/stamina recovery rules during sleep, including the timing of recovery when a blocking condition is removed partway through sleep?
14. How are timed condition durations and early-removal probabilities finalised?
15. What is the final encounter escape distance and how is it measured in the arena?
16. How are actors inserted into Initiative when they arrive during an active round?
17. How does the arena handle arrivals, departures, pursuit, and re-engagement without breaking the source-scene model?
18. What information can each communication channel transmit, to whom, how quickly, and with what limits?
19. How are NPC goals, task selection, danger assessment, refuge selection, and return decisions governed?
20. How much of the world is simulated in detail at once, and how are distant or unloaded NPCs abstracted without losing important consequences?

---

# Change discipline

This document is a design authority only after its status and decisions are deliberately promoted. Until then, implementation agents must treat unresolved values and mechanics as open and must not invent final formulas or behaviours.
