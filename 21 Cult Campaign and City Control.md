# 21 Cult Campaign and City Control

Version: 0.3

Status: Draft — owner-approved direction, incomplete specification

Last Updated: 2026-11-01 (owner's second answers: capture order, Unclaimed cities and faction ownership, life under Cult Control, ritual sabotage, retreat triggers, the rituals' secret revealed only at manifestation, killing all three Generals as the only pre-manifestation ending, the Holding General's moves, and the Divine Plane restored apart from the Spirit World. Earlier: campaign length, 3×3 target world, 5–7 cities, the secret purpose of rituals, the manifestation condition, Cult Retreat, Cult Thought Link, Generals' behaviour outside cities, the Spirit World, and the Stage Occupancy Rule recorded. Earlier: first version: the pre-manifestation campaign is a war for cities led by three Stage 2 Cult Generals, with asymmetric force generation and a Capital Gate.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [AGENTS.md](AGENTS.md), future faction AI, settlement state, balance, and world-event specifications.

---

# Purpose and authority

This document defines how the campaign advances before the sealed Stage 0 being manifests: how the cult takes the playable state city by city, how allied forces and the player push back, and how the world reaches the capital ritual with or without the player.

[02 World Laws](02%20World%20Laws.md) states the campaign's world-level laws. This document holds its operating rules. It is a design draft, not an implementation specification or implementation authorisation.

Three kinds of statement appear below:

- **Owner-approved** statements are decisions recorded from the owner's concept and answers of 2026-11-01.
- **Working readings** are how this document interprets the owner's words where they leave a gap. They hold until the owner confirms or replaces them.
- **Open** items are listed in [99 Open Questions](99%20Open%20Questions.md) § Cult Campaign and City Control under the IDs CC-1 … CC-23. No implementation may invent answers to them.

# 1. Campaign shape

```text
World creation: every city is under allied control; the cult holds none
        ↓
Allied forces start large and shrink; Cult Forces keep growing
        ↓
A Cult Force gathers enough followers and moves on a city
        ↓
City Capture: attack, other factions' followers cleared, six-hour ritual,
the city becomes a Place of Power and a garrison stays
        ↓
Cult Control ←——— City Liberation returns the city ———→ the cult must take it again
        ↓
Every city except the capital is under Cult Control: the Capital Gate is open
        ↓
Capital Awakening Ritual at the fixed centre of the capital,
with smaller rituals in every captured city
        ↓
Residents of captured cities die as the sacrifice; the sealed being manifests
```

The campaign runs without the player. The player may defend, liberate, hunt commanders, work for a faction, or do none of these. Every step above can happen off-screen.

The reference for the intended feel is the Dominator war in *Space Rangers 2*: a side that keeps producing new units against a side that was generated once and wears down. This is a comparison of pressure curves only. No names, lore, or exact rules are taken from that game.

# 2. Scale and pace

## Owner-approved

- At the middle difficulty the campaign lasts **15–20 game days, about 40–50 real hours**. Player skill can shorten or lengthen it within one difficulty, and other difficulty settings sit around this target.
- The target world is **3×3 tiles of 1400 m, 4200 × 4200 m**.
- The world has **5–7 cities, ideally 7**; 6 is also acceptable.
- Villages and small settlements are not cult targets. Only cities are.
- Cities stand far enough apart that a player who learns of a ritual has time to reach it, stop it, and then move on to the next city. Two neighbouring cities must not be clearable in one quick pass.
- Pressure comes first from information, and distance adds to it.
- The extra room gives time for exploration, gathering, monsters, and crafting. Secret places, chests, and formulas are scattered across the world.
- The player works with what comes to hand, already aware of the time and the threat, and does not choose freely among every opportunity in the world. This is the basis of replayability.

## Working reading

The count of 5–7 includes the capital, so 7 cities means the capital plus 6 others.

## Derived figures

These follow from existing values. They are planning figures, not measurements.

| Figure | Value | Basis |
|---|---|---|
| Real time per game day with sleep | about 2.8 h | 17 waking game hours at 10 real minutes per game hour |
| 40–50 real hours | about 14–18 game days | At 2.8 h per game day; 20 days is about 57 h |
| Crossing the target world | about 93 ticks, 26% of a six-hour window | 4200 m at the 4.5 m/s run of the [G2 brief](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) |
| Land at today's land share | about 6.4 km² | Today's 2.83 km² scaled by world area |
| Distance between neighbouring cities | about 1 km, about 23 ticks | 7 cities evenly spread over that land; roads and relief lengthen it |
| Unopposed capture rate | one city per 2.5–3.3 game days | 6 non-capital cities over 15–20 days |

## Effect on geography

The 3×3 target is the gameplay requirement that [99 Open Questions](99%20Open%20Questions.md) GEO-1 reserved for changing the world size, and it answers the travel-pacing decision D3. The city count replaces the "3–5 settlements" of target geography D6.

This is a game-design target. It authorises no production terrain work. The V0 engineering world stays 2800 × 2800 m, and G3f's evidence, which was gathered in that world, does not extend to the larger one without its own plan (CC-2).

# 3. Cities and City Control

## Owner-approved

- At world creation the whole playable state belongs to the three allied Major Factions. The cult holds no city.
- The cult's objective before manifestation is to capture cities by conducting rituals in them.
- The capital is the last target. The cult works on every other city first.
- Neutral territory exists outside the cities. The Raiding General's base stands there.

## City Control

**City Control** is the state saying who holds a city. It has three values:

- **Allied Control** — one allied Major Faction holds the city, and its new followers appear there as defenders.
- **Cult Control** — the city was taken through City Capture and has not been liberated.
- **Unclaimed** — the city was liberated and no faction holds it yet.

Control changes only when City Capture or City Liberation completes, or when a faction claims an Unclaimed city. During an attack, the city keeps its current control.

## Who takes a liberated city (owner-approved)

- The allied faction with the most followers in the city takes it.
- A city liberated by the player alone is Unclaimed. The first allied faction whose follower arrives claims it, and that faction's followers then appear there as defenders.
- Replacement of allied followers becomes possible in the city again.

## Working readings

- "The whole world belongs to the allied factions" is read as "every city". Land between cities carries no City Control and stays neutral.
- An Unclaimed city is not under Cult Control, so it keeps the Capital Gate closed. How the cult takes an Unclaimed city is open (CC-7).

# 4. City Capture and City Liberation

## Owner-approved

- **City Capture** runs in a fixed order: the cult attacks, clears the city of every follower of other factions by driving them out or killing them, conducts a six-hour ritual that can still be stopped, and then holds the city with a garrison.
- The cult does not attack ordinary residents during City Capture. It targets followers.
- **City Liberation** is symmetrical: every cult follower in the city is driven out or destroyed.
- The player can attempt City Liberation alone or alongside a faction. City defence alongside a faction is also available.
- Killing every follower is not required. Cult followers may decide to retreat.
- A General does not have to be in a captured city. A General may also run from a fight, except the Holding General.
- Rituals can run in parallel in two cities when the cult has gathered enough followers.

## Ritual sabotage (owner-approved)

A ritual is a concrete mechanism in the city. It can be sabotaged out of combat, without a fight.

Once a ritual is disrupted, everyone conducting it loses most of their power and cannot fight properly. They try to leave the city first. Against a single intruder they may try to drive that person off or fight. Against many people they most likely retreat.

The cult cannot repeat the ritual straight away. How long it must wait is open (CC-7). What the mechanism is and what sabotage takes is open (CC-8).

## Cult Retreat (owner-approved)

Cult Retreat is collective: once followers start to flee, they flee from every point of the city at once.

They retreat when a senior left in charge of the city dies, or when losses leave them in a dangerously small minority. They head for the nearest city under Cult Control or scatter outside the walls.

The player can chase and kill them while staying close enough that they do not leave the fight. The separation rule of [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md) governs that distance. Past it, they get away.

## Life under Cult Control (owner-approved)

A captured city is a closed combat zone, but residents and shops keep working, and cult followers patrol the streets.

- Stealth is possible. A character who stays unnoticed can reach a stash or an NPC they need.
- Once a character is noticed as an enemy, a fight begins and every follower in the city knows where they are. The choice is to fight or to run.
- A stranger with no faction can walk through at some risk, because nobody knows them.
- Followers sense a member of a known faction, approach, and demand a check through dialogue. Refusing starts a fight. A character who agrees and belongs to no faction may only be threatened.

The dialogues themselves are later content.

## Cult Thought Link

Cult followers do not hide. They behave much like the Dominators and converge on any fight.

The **Cult Thought Link** is a mental ability to exchange thoughts at a distance:

- Every follower in a city knows where a fight is and runs to it. No alarm is needed.
- Followers in the nearest city can also learn of it and arrive as reinforcements.

No hidden cult follower can therefore keep a city from being liberated. The same question for allied followers during a capture is open (CC-3).

## Followers and ordinary residents

A **Follower** is a character who belongs to a Major Faction's forces. Ordinary residents are not followers, even when they live under a faction's rule.

Who else counts for capture and liberation is open (CC-3), including Minor Faction members, unaffiliated practitioners, and the player character.

## Working readings

- A character who belongs to a known faction and agrees to the check is exposed and attacked.
- A city's garrison is its followers plus one or more seniors left in charge. Its numbers are open (CC-4).

# 5. The secret purpose of rituals

## Owner-approved

A capture ritual turns a city into a **Place of Power** for the manifestation. Ordinary residents are needed alive as sacrifices, which is why the cult does not attack them. Materialising a Stage 0 being in the real world takes a very great number of sacrifices.

A Place of Power needs no visible mark on the city.

Nobody in the world knows this purpose, and neither does a first-time player. Everyone sees rituals with no civilian victims, so the allied factions treat the rituals themselves with scepticism, even as they fight for their cities.

The player can only guess, until manifestation reveals what the rituals were for. That knowledge stays in the player's head. It carries to the next world as Player Knowledge and never as a mechanic: there are no dialogues to convince anyone of it.

At the Capital Awakening Ritual, followers conduct smaller rituals in every captured city at the same moment, and the people there die as the sacrifice that manifests the being.

## Change from the earlier draft

The earlier draft described rituals as civilian-targeted. The cult's violence in a capture is now aimed at followers. Residents are spared until the final ritual, and their death is what the campaign is racing towards.

The player cannot evacuate residents. Whether the capital's residents die too, and what the world looks like afterwards, is open (CC-20). Whether a player who wins before manifestation ever learns the secret is open (CC-21).

# 6. Capital Gate and manifestation

## Owner-approved

- The Capital Awakening Ritual can begin only when every other city is under Cult Control. This rule is the **Capital Gate**.
- The cult therefore captures the other cities first.
- The fall of the last other city tells the player that the capital is next.
- The player can liberate cities, which closes the Capital Gate again.
- Manifestation succeeds only if every city was captured at the moment of the capital ritual and that ritual was not interrupted.

## Working reading

Liberating a city while the capital ritual is running breaks the condition, so the ritual fails. What the cult does next is open (CC-17).

## Consequences

The Capital Gate replaces the earlier undecided "required number of rituals". The number of rituals the cult needs is the number of non-capital cities plus every recapture forced by City Liberation.

City Control is the campaign's public progress reading. It replaces the hidden return threat described in the earlier draft. There is still no numeric meter or countdown: the player reads the campaign from reported control of cities, under [09 Information Sources](09%20Information%20Sources.md).

# 7. Cult Generals

## Owner-approved

The cult's forces are divided into three **Cult Forces**, each led by a **Cult General**. All three Generals are Stage 2 followers. They share the objective of capturing cities through rituals and differ in how they act. Those differences require different responses from the player.

Generals prepare their next attack in an area outside the city, so that they cannot be killed early. The exception is the Holding General, who stays with its followers in the cities it captures.

Killing all three Generals is the only way to end the campaign before manifestation.

The labels below describe roles only. They are not final names.

| Working label | Owner-approved behaviour | Can it be found? | What it asks of the player |
|---|---|---|---|
| **Teleporting General** | Lives in the Spirit World and teleports in to attack. Can be defeated only straight after teleporting, because for some time it cannot return. | Not in the real world. | Be at the arrival in time and win inside that window. |
| **Holding General** | The warlike one. Stays in person with its followers in each city it captures. When it moves to the next city, it leaves only a small part behind and takes its main force along. Fights to the end. Hardest of the three in battle. | Yes. | Prepare for one decisive fight, or strike the weak garrisons it leaves behind. |
| **Raiding General** | Captures a city, then leaves on foot for a base in neutral territory and raids from there. | Yes. | Find the base and break the raiding cycle. |

The owner's reference for the Teleporting General is a *Space Rangers* enemy that travels through black holes. This is a comparison of feel only.

## Spirit World and Divine Plane

These are two different places:

- The **Spirit World** is the other side of the real world. Higher followers of Pathways can reach it, and the Teleporting General lives there. The player cannot enter it for now.
- The **Divine Plane** is unreachable. Since a past cataclysm, every Stage 0 being has been there, the sealed being included. They cannot materialise in the real world on their own and act through their followers.

The allied gods refused to sacrifice people to return. The sealed being's cult chose to do it.

## Rank and the Stage Occupancy Rule

The **Stage Occupancy Rule** of [05 Progression](05%20Progression.md) is why the Generals are Stage 2. While a Pathway's Stage 0 is occupied, that Pathway has no Stage 1. The sealed being occupies Stage 0, so its followers stop at Stage 2. The same holds for the allied factions, whose gods occupy Stage 0: they too stop at Stage 2.

The three Generals are therefore the top of the cult's living hierarchy. Above them stands only the sealed being.

## Working readings

- A killed General is not replaced, because killing all three must remain able to end the campaign. Whether the cult's reinforcement slows as Generals fall is open (CC-9).
- A General is a persistent named actor, not a generated follower.
- The Holding General does not Break Away or offer a Yield. This is an exception for one actor, not a change to [08 Confront Constraints](08%20Confront%20Constraints.md).
- The three behaviours are fixed archetypes across worlds. The World Seed varies who the Generals are, their bases, and their target order, so that a past world teaches how to read a General without solving the next one. Whether they vary by Pathway depends on CC-22.
- Stage 2 Generals are a full-game world premise, in the same sense as the Stage 2–3 head of the covert alchemical network in [02 World Laws](02%20World%20Laws.md). They do not widen V1's Stages 9–7 player scope in [05 Progression](05%20Progression.md).

# 8. Asymmetric force generation

## Owner-approved

**Allied forces** are generated at world creation. During play, new followers of certain stages appear at a small periodic rate, in the cities their faction holds. Losses outpace replacement, so the allied side can dwindle over a long campaign.

**Cult Forces** generate much faster and are not tied to the general rules of the world's population. They simply gain new followers periodically.

Killing rank-and-file cult followers never exhausts the cult. New followers keep appearing until its leadership is destroyed.

When a Cult Force has gathered a certain number of followers, it attempts to capture a city and conduct a ritual there.

Campaign pace depends on difficulty: harder settings make the cult advance faster.

## Working readings

- "Not tied to the general rules" applies to how often followers appear and where they come from. It does not change character rules. Generated cult followers use the same attributes, skills, Pathway stages, and derived-statistic formulas as every other actor, as [02 World Laws](02%20World%20Laws.md) requires. They are generated at their stage and do not play through Alchemy or Conduct Progress.
- The strength threshold belongs to each Cult Force, not to the cult as a whole.

## Open

Spawn location, stage mix, cap, and the in-world source of new followers are CC-12. Allied starting strength and replacement are CC-13. Which difficulty setting owns campaign pace is CC-14: [02 World Laws](02%20World%20Laws.md) forbids a single slider from silently bundling unrelated effects, so this cannot be attached to a slider by default.

# 9. The player in the campaign

The player may:

- join a city's defence alongside a faction;
- liberate a city alone or with allies;
- pursue a General;
- disrupt a ritual through the Approaches available at the site;
- work elsewhere and let the war continue.

None of these is mandatory, and the world does not wait. A player who does nothing reaches the outcome the World Engine produces for that difficulty, as [02 World Laws](02%20World%20Laws.md) describes for the capital confrontation.

As allied forces wear down, the player's share of the defence grows. This is a pressure that emerges from the war, not a chosen-one role: [01 Player Experience](01%20Player%20Experience.md) still applies.

# 10. Information

City Capture, City Liberation, and control changes are significant public events. They reach the player through daily public news, rumours, ambient NPC speech, briefings, and the map's qualitative control layer, under the rules of [09 Information Sources](09%20Information%20Sources.md).

Knowing a city's control is not the same as knowing a General's position. How a General's location becomes known is open (CC-10).

The Cult Thought Link is a defined channel under [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), not global NPC knowledge. It reaches cult followers only.

No source in the world reports the purpose of the rituals.

Captured and liberated cities must look different in the world. Their Symptoms are part of CC-6.

# 11. World Engine and simulation budget

The campaign adds state that [02 World Laws](02%20World%20Laws.md) did not previously track: City Control, the forces present in each city, and three General actors.

The World Engine remains an operation scheduler, not a population simulation. Proposed direction, subject to CC-15: each city keeps a coarse ledger of followers per faction and stage, and only instantiates characters when the player enters. This follows [04 Points of Interest](04%20Points%20of%20Interest.md)' Lazy Activation.

An occupied city is persistent world state rather than a timed POI, because it has no Active Window. Discrete events inside it, such as a ritual or a clash, remain POIs.

# 12. Design check

Filter of [AGENTS.md](AGENTS.md):

| Question | Result |
|---|---|
| Creates curiosity? | Yes. Rituals with no visible victims and each General's tells give the player something to guess at. |
| Teaches about the world? | Yes: General behaviour, tells, and control patterns are prime Player Knowledge targets. |
| Power has a price? | Partly. Fighting for a city costs time elsewhere; costs of allied help are open. |
| Multiple options viable? | **Partly.** Ritual sabotage and stealth give real non-combat routes during a capture. Once a city has fallen, breaking its garrison is still combat only. [08 Confront Constraints](08%20Confront%20Constraints.md) requires real alternatives (CC-8). |
| Failure valuable? | **At risk.** Liberating a city that falls again the next day may only waste time. Liberation needs a lasting payoff (CC-7). |
| World changes afterward? | Yes: control, residents, services, the Capital Gate, and the deaths at manifestation. |

Known risks:

- **Constant urgency against curiosity.** A war with a visible front can crowd out exploration, the game's first pillar. The 15–20 day length and the 3×3 world are the response; the tempo curve is still open (CC-1).
- **Solo fight against a whole garrison.** The Cult Thought Link draws a city's followers to one fight. A lone character can meet all of them, so the size of a garrison and how they arrive decide whether solo liberation is possible (CC-4, CC-8).
- **Sabotage as a best strategy.** If one stealthy character can always break a ritual and send its conductors running, sabotage beats every other answer. The mechanism needs its own price and risk (CC-8).
- **Emptied cities after manifestation.** If captured cities lose their residents, the world after manifestation has few people, services, or quests left (CC-20).
- **Power gap.** A Stage 7 character facing a Stage 2 General has a 1.32 stage multiplier against 2.66, before abilities ([07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md)). Defeating a General needs a route other than a stat race (CC-10).
- **Terrain evidence.** The target world and city count sit outside the conditions of the terrain research so far (CC-2).

# 13. Scope

The following V1/V2/V3 split is proposed, not approved. It depends on CC-11, including whether the campaign is part of V1 at all.

## V1

- Small city set with City Control and the Capital Gate.
- Coarse per-city force ledger; off-screen clashes resolved as bounded operations.
- Three Generals with their fixed behaviours.
- Asymmetric periodic generation.
- Public news and map control layer.

## V2

- Richer city consequences: services, displacement, and return.
- Non-combat liberation routes beyond the first set.
- General tells that vary more by seed.

## V3

- Allied counter-offensives and cross-city logistics.
- Deeper interaction with Minor Factions and the royal institution.

# Documentation Review

- **Vision / Pillars / Player Experience:** Compatible. The world acts without the player, and the player is not a chosen saviour. Curiosity under war pressure is a tracked risk (§12).
- **World Laws:** Updated. City capture replaces the required-ritual count and the hidden return threat; the Divine Plane and the Spirit World are separate places; Stage 1 cult and allied followers are withdrawn; the pre-manifestation ending is killing all three Generals.
- **Decision Engine:** Compatible once non-combat routes exist. Stale single-difficulty wording corrected.
- **Points of Interest / Information Sources:** Updated for persistent occupied cities, public City Control, and the rituals' purpose, which no source reports.
- **Progression:** Updated with the Stage Occupancy Rule for every Pathway and with Pathway change from Stage 5. Stage 2 Generals are a full-game premise and do not widen V1's player stages.
- **Character Life:** Updated. Manifestation is the Capital Awakening Ritual's completion; a manifestation during a Death Gap is open (CC-18).
- **Confront:** Compatible in principle; §12 records the requirement for real alternatives.
- **World Time:** Updated. The Cult Thought Link is a defined information channel.
- **World generation:** The 3×3 target and 5–7 cities are recorded in 14, 17, and GEO-1 without any production change.
- **Glossary / Open Questions / AGENTS.md:** Updated.

# Related Documents

- [02 World Laws](02%20World%20Laws.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [05 Progression](05%20Progression.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [09 Information Sources](09%20Information%20Sources.md)
- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)
- [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
