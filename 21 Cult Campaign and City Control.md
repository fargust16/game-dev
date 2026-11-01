# 21 Cult Campaign and City Control

Version: 0.10

Status: Draft — owner-approved direction, incomplete specification

Last Updated: 2026-11-01 (a ritual lasts one in-game hour; the relation of ritual and capture is open; NPC behaviour in a fight recorded. Earlier: the city as a battlefield and all three cult Arcana occupied recorded. Earlier: Arcana and the corrected Limit 1 rule recorded. Earlier: the cult's three Pathways recorded; the V1 player reaches Stages 9–8. Earlier: V1 settled: the Spirit General comes out on day 1–2. Earlier: owner's fourth answers: the ritual site, the one-day force of a liberated Place of Power, seers, how a General can be defeated, allied kills of Generals, allied cooperation, force numbers and each General's rate, the Cult Pace slider, and the V1 slice. Earlier: owner's third answers: generation-driven Cult Sorties and their odds, each General's cycle, the Spirit General replacing the teleporting reading, how rituals are stopped, the two-day ritual wait, the Last Free City Rule, and the capital's residents among the dead. Earlier: owner's second answers: capture order, Unclaimed cities and faction ownership, life under Cult Control, ritual sabotage, retreat triggers, the rituals' secret revealed only at manifestation, killing all three Generals as the only pre-manifestation ending, the Holding General's moves, and the Divine Plane restored apart from the Spirit World. Earlier: campaign length, 3×3 target world, 5–7 cities, the secret purpose of rituals, the manifestation condition, Cult Retreat, Cult Thought Link, Generals' behaviour outside cities, the Spirit World, and the Stage Occupancy Rule recorded. Earlier: first version: the pre-manifestation campaign is a war for cities led by three Stage 2 Cult Generals, with asymmetric force generation and a Capital Gate.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [AGENTS.md](AGENTS.md), future faction AI, settlement state, balance, and world-event specifications.

---

# Purpose and authority

This document defines how the campaign advances before the sealed Limit 0 being manifests: how the cult takes the playable state city by city, how allied forces and the player push back, and how the world reaches the capital ritual with or without the player.

[02 World Laws](02%20World%20Laws.md) states the campaign's world-level laws. This document holds its operating rules. It is a design draft, not an implementation specification or implementation authorisation.

Three kinds of statement appear below:

- **Owner-approved** statements are decisions recorded from the owner's concept and answers of 2026-11-01.
- **Working readings** are how this document interprets the owner's words where they leave a gap. They hold until the owner confirms or replaces them.
- **Open** items are listed in [99 Open Questions](99%20Open%20Questions.md) § Cult Campaign and City Control under the IDs CC-1 … CC-35. No implementation may invent answers to them.

# 1. Campaign shape

```text
World creation: every city is under allied control; the cult holds none
        ↓
Allied forces start large and shrink; Cult Forces keep growing
        ↓
A General gathers enough followers and launches a Cult Sortie against a city
        ↓
City Capture: the attack and the ritual begin, other factions' followers are cleared,
the ritual completes, the city becomes a Place of Power and a garrison stays
        ↓
Cult Control ←——— City Liberation returns the city ———→ the cult must take it again
        ↓
Every city except the capital is under Cult Control: the Capital Gate is open
        ↓
Capital Awakening Ritual at the fixed centre of the capital,
with smaller rituals in every captured city
        ↓
Residents of the capital and of captured cities die as the sacrifice; the sealed being manifests
```

The campaign runs without the player. The player may defend, liberate, hunt commanders, work for a faction, or do none of these. Every step above can happen off-screen.

The reference for the intended feel is the Dominator war in *Space Rangers 2*: a side that keeps producing new units against a side that was generated once and wears down. This is a comparison of pressure curves only. No names, lore, or exact rules are taken from that game.

# 2. Scale and pace

## Owner-approved

- At the middle difficulty the campaign lasts **15–20 game days, about 40–50 real hours**. Player skill can shorten or lengthen it within one difficulty, and other difficulty settings sit around this target.
- The target world is **3×3 tiles of 1400 m, 4200 × 4200 m**.
- The world has **5–7 cities, ideally 7**; 6 is also acceptable. The count includes the capital, so 7 cities means the capital plus 6 others.
- Villages and small settlements are not cult targets. Only cities are.
- Cities stand far enough apart that a player who learns of a ritual has time to reach it, stop it, and then move on to the next city. Two neighbouring cities must not be clearable in one quick pass.
- Pressure comes first from information, and distance adds to it.
- The extra room gives time for exploration, gathering, monsters, and crafting. Secret places, chests, and formulas are scattered across the world.
- The player works with what comes to hand, already aware of the time and the threat, and does not choose freely among every opportunity in the world. This is the basis of replayability.

## Derived figures

These follow from existing values. They are planning figures, not measurements.

| Figure | Value | Basis |
|---|---|---|
| Real time per game day with sleep | about 2.8 h | 17 waking game hours at 10 real minutes per game hour |
| 40–50 real hours | about 14–18 game days | At 2.8 h per game day; 20 days is about 57 h |
| Crossing the target world | about 93 ticks, 155% of a one-hour ritual | 4200 m at the 4.5 m/s run of the [G2 brief](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) |
| Land at today's land share | about 6.4 km² | Today's 2.83 km² scaled by world area |
| Distance between neighbouring cities | about 1 km, about 23 ticks | 7 cities evenly spread over that land; roads and relief lengthen it |
| Unopposed capture rate | one city per 2.5–3.3 game days | 6 non-capital cities over 15–20 days |
| Six non-capital cities fallen | about day 18 | First sorties on days 3–8, then one sortie every 1–2 days across the Generals, 60% of sorties succeed |

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

- **City Capture** begins with a Cult Sortie. The followers conducting the ritual gather in one place, and the others keep people away from that place. The cult clears the city of every follower of other factions by driving them out or killing them, the one-hour ritual completes, and a garrison holds the city. How the ritual and the capture relate is open (CC-34).
- A character can sneak to the ritual site, or try to kill everyone before the ritual completes.
- The cult does not attack ordinary residents during City Capture. It targets followers.
- **City Liberation** is symmetrical: every cult follower in the city is driven out or destroyed.
- The player can attempt City Liberation alone or alongside a faction. City defence alongside a faction is also available.
- Killing every follower is not required. Cult followers may decide to retreat.
- A General does not have to be in a captured city. A General may also run from a fight, except the Holding General.
- Rituals can run in parallel in two cities when the cult has gathered enough followers.

## The city as a battlefield (owner-approved)

Confront runs in place, so a whole city becomes one battlefield and the ritual runs in one part of it ([16 Architecture Pivot](16%20Architecture%20Pivot.md)). A combat round is one tick, so a ritual's 60-tick window, one in-game hour or ten real minutes, is up to 60 rounds for a character in the fight.

While Turn-Based Mode runs, the rest of the world stands still and lives one tick at the end of each round. Ordinary NPCs such as traders try to hide and get away from the fight. A defender who can fight, with full health and no order to retreat, goes to it by the nearest route.

## Ritual and capture (open)

Two versions are under consideration (CC-34):

- **Parallel.** The ritual runs from the start of the attack, and the city may or may not fall. If the ritual completes in a city that held, faction leaders can undo it, in about an in-game hour.
- **After the fight.** The ritual can begin only when no resistance remains in the city.

## Stopping a ritual (owner-approved)

A ritual is stopped in one of three ways:

- Kill everyone conducting it.
- Kill the senior who started it. The ritual then counts as disrupted.
- Meet the conditions set for that ritual: persuade cultists, pose as one of them or as a local, pretend to help and derail it. These are strong dialogue checks, such as Intelligence, or Intelligence against Will.

The conditions differ from ritual to ritual, so the player cannot learn one answer and repeat it. They take planning, and they give less warlike Arcana a way to act.

The non-combat routes exist only while the cult has not fully taken the city. Once the city is cleared, a running ritual can be stopped only by fighting.

Once a ritual is disrupted, everyone conducting it loses most of their power and cannot fight properly, and the enemy's strength falls. They try to leave the city first. Against a single intruder they may try to drive that person off or fight. Against many people they most likely retreat.

A new ritual can be held in a city two game days after the previous one there, so that rituals do not run constantly. The city itself can be retaken at once, but the ritual must wait. While it waits, the city does not count as captured and does not open the capital.

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
- A city's garrison is its followers plus one or more seniors left in charge.

# 5. The secret purpose of rituals

## Owner-approved

A capture ritual turns a city into a **Place of Power** for the manifestation. Ordinary residents are needed alive as sacrifices, which is why the cult does not attack them. Materialising a Limit 0 being in the real world takes a very great number of sacrifices.

A Place of Power needs no visible mark on the city. It keeps its force for one game day after its city is liberated: the city must be held for that day before its ritual counts as reset.

Nobody in the world knows this purpose, and neither does a first-time player. Everyone sees rituals with no civilian victims, so the allied factions treat the rituals themselves with scepticism, even as they fight for their cities.

The player can only guess, until manifestation reveals what the rituals were for. That knowledge stays in the player's head. It carries to the next world as Player Knowledge and never as a mechanic: there are no dialogues to convince anyone of it.

At the Capital Awakening Ritual, followers conduct smaller rituals in every captured city at the same moment, and the people of those cities and of the capital die as the sacrifice that manifests the being.

## Change from the earlier draft

The earlier draft described rituals as civilian-targeted. The cult's violence in a capture is now aimed at followers. Residents are spared until the final ritual, and their death is what the campaign is racing towards.

The player cannot evacuate residents. Only a few people of the state survive. They are not simulated: the ending reports them. What play looks like after manifestation is open (CC-20).

A player who wins before manifestation never learns the purpose. It stays a secret and a reason to play again.

# 6. Capital Gate and manifestation

## Owner-approved

- The Capital Awakening Ritual can begin only when every other city is under Cult Control. This rule is the **Capital Gate**.
- The cult therefore captures the other cities first.
- The fall of the last other city tells the player that the capital is next.
- The player can liberate cities, which closes the Capital Gate again once the city has been held for a game day.
- Manifestation succeeds only if every city was captured at the moment of the capital ritual and that ritual was not interrupted. A city liberated less than a game day earlier still counts, because its Place of Power keeps its force.

A last-minute liberation therefore does not stop manifestation. A character who slips away during the attack on the capital and frees a city does not pull the capital's attackers back, and the capital ritual can still succeed.

What the cult does after a broken capital ritual is open (CC-17).

## Last Free City Rule (owner-approved)

A new character can appear only while at least one uncaptured city has no battle in it. A character who dies during the final attack, when the other cities have fallen and the last free city is under attack, has nowhere to return: that death is final, even before manifestation.

## Consequences

The Capital Gate replaces the earlier undecided "required number of rituals". The number of rituals the cult needs is the number of non-capital cities plus every recapture forced by City Liberation.

City Control is the campaign's public progress reading. It replaces the hidden return threat described in the earlier draft. There is still no numeric meter or countdown: the player reads the campaign from reported control of cities, under [09 Information Sources](09%20Information%20Sources.md).

# 7. Cult Generals

## Owner-approved

The cult's forces are divided into three **Cult Forces**, each led by a **Cult General**. All three Generals are Limit 2 followers. They share the objective of capturing cities through rituals and differ in how they act. Those differences require different responses from the player.

Each General can be stopped in a different place: one on the approach, one in a city it holds, and one only during a capture.

Killing all three Generals is the only way to end the campaign before manifestation.

The labels below describe roles only. They are not final names.

| Working label | Owner-approved behaviour | Can it be found? | What it asks of the player |
|---|---|---|---|
| **Spirit General** | Lives in the Spirit World. After a capture it leaves all its followers in the city and returns there. It gathers new followers there, the weakest and the fastest to arrive, and comes out in a city together with them. It comes out only in cities. | Only during a capture, while the ritual runs. | Be at the ritual in time and win while it is out. |
| **Holding General** | The warlike one. Stays with its followers in the city it captured until it has gathered enough for the next sortie, then moves on, leaving only a small part behind. Fights to the end. Hardest of the three in battle. | In the city it holds. | Prepare for one decisive fight, or strike the weak garrisons it leaves behind. |
| **Raiding General** | Walks in the open. After a capture it leaves almost all its force in the city and goes off on foot with a few followers, then gathers a new force in neutral territory, where it has a base. | In neutral territory or on the approach. | Find it between sorties and stop it before it reaches a city. |

The owner's reference for the Spirit General is a *Space Rangers* enemy that travels through black holes. This is a comparison of feel only.

## Spirit World and Divine Plane

These are two different places:

- The **Spirit World** is the other side of the real world. Higher followers of Arcana can reach it, and the Spirit General lives there. The player cannot enter it for now.
- The **Divine Plane** is unreachable. Since a past cataclysm, every Limit 0 being has been there, the sealed being included. They cannot materialise in the real world on their own and act through their followers.

The allied gods refused to sacrifice people to return. The sealed being's cult chose to do it.

## Rank and the Limit Occupancy Rule

The cult has three Arcana: the Empress, the Emperor, and the Hierophant ([05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md)). The sealed being and the Spirit General belong to the Emperor Arcana. Other followers may belong to any of the three.

The **Limit Occupancy Rule** of [05 Progression](05%20Progression.md) is why the Generals are Limit 2. While an Arcana's Limit 0 is occupied, that Arcana has no Limit 1. The sealed being occupies Limit 0 of the Emperor Arcana, so that Arcana stops at Limit 2. The same holds for a god's own Arcana on the allied side, while a faction's other Arcana may have a free Limit 0 and reach Limit 1.

The three Generals are therefore the top of the cult's living hierarchy. Above them stands only the sealed being. The Empress and Hierophant Arcana stop at Limit 2 as well: their Limit 0 belongs to sane gods who want to use the True God's descent for their own ends.

## Working readings

- A killed General is not replaced, because killing all three must remain able to end the campaign. Whether the cult's reinforcement slows as Generals fall is open (CC-9).
- A General is a persistent named actor, not a generated follower.
- The Holding General does not Break Away or offer a Yield. This is an exception for one actor, not a change to [08 Confront Constraints](08%20Confront%20Constraints.md).
- The three behaviours are fixed archetypes across worlds. The World Seed varies who the Generals are, their bases, and their target order, so that a past world teaches how to read a General without solving the next one. Whether they vary by Arcana depends on CC-22.

## Defeating a General (owner-approved)

A character far below Limit 2 cannot win a stat race. A General falls in one of these ways:

- by numbers, alongside an allied force;
- after a disrupted ritual, which strips most of the strength from the General and all the cultists, so they are more likely to retreat than to keep fighting;
- by a character who has reached Limit 2, or Limit 3 with good statistics and strategy.

Artifacts may help. They are tightly controlled, go only to trusted members with high standing, and some are dangerous to Sanity. They are unique items for later design.

Allied forces can kill a General without the player. On the middle setting this is rare, for example when a General attacks a city that holds an allied Limit 2 practitioner. At the lowest setting the game should win by itself: the allied side can kill all three Generals, so that a player who only wants to enjoy the world can do so.

## Seers and the Spirit General

Allied factions have otherworldly seers, and the player character may be one through a seer Arcana. Seers can predict where the Spirit General will come out, so forces can gather there first.

## The final attack

Before the capital, all three Generals attack together. They may fight in different parts of the city, or in one place. This battle needs its own design (CC-17).

# 8. Asymmetric force generation

## Owner-approved

**Allied forces** are generated at world creation. During play, new followers of certain Limits appear at a small periodic rate, in the cities their faction holds. Losses outpace replacement, so the allied side can dwindle over a long campaign.

**Cult Forces** generate much faster and are not tied to the general rules of the world's population. The system gives each General new followers automatically and periodically, near that General. The game does not need to explain where they were found.

Killing rank-and-file cult followers never exhausts the cult. New followers keep appearing until its leadership is destroyed.

Campaign pace depends on difficulty: harder settings make the cult advance faster. The eighth Difficulty Setting, **Cult Pace**, owns this ([02 World Laws](02%20World%20Laws.md)).

## Allied cooperation (owner-approved)

Each allied faction acts in its own way. Factions usually act alone, gathering their own forces and liberating cities with them. When that is no longer possible and their forces are weak, they unite and attack together. Sometimes two factions move on a city at once.

## Cult Sorties (owner-approved)

A **Cult Sortie** is a General's attempt on a city. Each General launches one when it has gathered a certain number of followers.

The campaign has no calendar of attacks. First sorties fall on these days only because that is when enough followers have appeared:

| General | Generation rate | First sortie | Why |
|---|---|---|---|
| Spirit General | Fastest, with weaker followers | Day 3–5 | It strikes unseen and often |
| Raiding General | Middle | Day 5–7 | It leaves a city as soon as it falls |
| Holding General | Slowest | Day 7–8 | It always stays on the defensive |

Later sorties come faster: about one every day or two across the three Generals.

A full city or sortie is about 20 followers of different Limits, with or without a General. A city a General has just left holds about 10. Fifty is too many.

A sortie can succeed or fail, depending on how strong the city's defence is:

- At the middle difficulty the attackers have the better chance, but the allied factions can still repel the cult without the player.
- On harder settings a city is almost certain to fall unless someone interferes.

After a capture, each General gathers its next force in its own way (§7), and the garrison stays behind.

## Working readings

- "Not tied to the general rules" applies to how often followers appear and where they come from. It does not change character rules. Generated cult followers use the same attributes, skills, Limits, and derived-statistic formulas as every other actor, as [02 World Laws](02%20World%20Laws.md) requires. They are generated at their stage and do not play through Alchemy or Conduct Progress.

## Open

Exact rates, Limit mix, and cap are CC-12. Allied starting strength and replacement are CC-13.

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

The World Engine remains an operation scheduler, not a population simulation. Proposed direction, subject to CC-15: each city keeps a coarse ledger of followers per faction and Limit, and only instantiates characters when the player enters. This follows [04 Points of Interest](04%20Points%20of%20Interest.md)' Lazy Activation.

An occupied city is persistent world state rather than a timed POI, because it has no Active Window. Discrete events inside it, such as a ritual or a clash, remain POIs.

# 12. Design check

Filter of [AGENTS.md](AGENTS.md):

| Question | Result |
|---|---|
| Creates curiosity? | Yes. Rituals with no visible victims and each General's tells give the player something to guess at. |
| Teaches about the world? | Yes: General behaviour, tells, and control patterns are prime Player Knowledge targets. |
| Power has a price? | Partly. Fighting for a city costs time elsewhere; costs of allied help are open. |
| Multiple options viable? | **Yes, before a city falls.** Persuasion, disguise, and stealth are real routes during a capture. A fallen city is combat only by the owner's decision; [08 Confront Constraints](08%20Confront%20Constraints.md) accepts alternatives that come beforehand. |
| Failure valuable? | **At risk.** Liberating a city that falls again the next day may only waste time. Liberation needs a lasting payoff (CC-7). |
| World changes afterward? | Yes: control, residents, services, the Capital Gate, and the deaths at manifestation. |

Known risks:

- **Constant urgency against curiosity.** A war with a visible front can crowd out exploration, the game's first pillar. The 15–20 day length, the quiet first days, and the 3×3 world are the response.
- **Solo fight against a whole garrison.** The Cult Thought Link draws a city's followers to one fight. A lone character can meet all of them, so the size of a garrison and how they arrive decide whether solo liberation is possible (CC-4, CC-8).
- **Content cost of ritual conditions.** Conditions that differ every time need a set of authored parts that the World Seed combines, which is a real load for a solo developer (CC-8).
- **Slice scope.** The slice's required mechanics pull many undesigned systems into the first build (CC-29, CC-32, CC-33).
- **Emptied cities after manifestation.** If captured cities lose their residents, the world after manifestation has few people, services, or quests left (CC-20).
- **Time to reach a General.** Direct defeat takes Limit 2, or Limit 3 with good statistics and strategy, inside a 15–20 day campaign. A Limit 7 character has a 1.32 Limit multiplier against a General's 2.66 ([07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md)). If the player cannot get deep enough in time, allied numbers and disrupted rituals carry that whole path (CC-10).
- **Terrain evidence.** The target world and city count sit outside the conditions of the terrain research so far (CC-2).

# 13. Scope

## V1 (owner-approved)

V1 is the one-city Vertical Slice of [11 Vertical Slice](11%20Vertical%20Slice.md): the Spirit General comes out on day 1–2, the ritual starts at once, the General leaves after 15–20 ticks, and the player either stops the ritual and wins or loses. The player reaches Limits 9–8, Limit 7 is for NPCs, and allies and enemies may be deeper. A General that does not attack is an approved fallback.

The following V2 and V3 split is proposed, not approved.

## V2

- The full war: 5–7 cities, three Generals, City Control, and the Capital Gate.
- Coarse per-city force ledger; off-screen clashes resolved as bounded operations.
- Public news and map control layer.

## V3

- Richer city consequences: services, displacement, and return.
- Allied counter-offensives and cross-city logistics.
- Deeper interaction with Minor Factions and the royal institution.

# Documentation Review

- **Vision / Pillars / Player Experience:** Compatible. The world acts without the player, and the player is not a chosen saviour. Curiosity under war pressure is a tracked risk (§12).
- **World Laws:** Updated. City capture replaces the required-ritual count and the hidden return threat; the Divine Plane and the Spirit World are separate places; Limit 1 cult and allied followers are withdrawn; the pre-manifestation ending is killing all three Generals.
- **Decision Engine:** Compatible once non-combat routes exist. Stale single-difficulty wording corrected.
- **Points of Interest / Information Sources:** Updated for persistent occupied cities, public City Control, and the rituals' purpose, which no source reports.
- **Progression:** Updated with the Limit Occupancy Rule for every Arcana and with Arcana change from Limit 5. Limit 2 Generals can appear in V1 and do not widen the player's Limits.
- **Vertical Slice:** Updated. The slice is one city with the Spirit General's invasion, under the same ritual rules; CC-29, CC-32, and CC-33 track what is still undecided.
- **Character Life / Vision / Meta Loop:** Updated. Manifestation is the Capital Awakening Ritual's completion, and the Last Free City Rule is recorded in 07 and in Canonical 00 and 00.2.
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
