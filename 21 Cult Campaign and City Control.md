# 21 Cult Campaign and City Control

Version: 0.24

Status: Draft — owner-approved direction, incomplete specification

Last Updated: 2026-11-02 (the owner's odds with no player and allied conduct recorded; Campaign Numbers V0 rebuilt on a whole war to manifestation or the death of the Generals. Earlier: Campaign Numbers V0 recorded, checked by a campaign simulation. Earlier: the source of traces recorded; the slice's scheme lives in 11. Earlier: a conductor who fights loses 3 AP, owner-approved. Earlier: working reading: a conductor who fights loses 3 AP. Earlier: the Cult Thought Link carries orders and reports only; Посланник restored. Earlier: Ritual Weak Points added. Earlier: ritual dialogue checks are Final Checks. Earlier: dialogue checks follow 10.4. Earlier: stealth in a captured city follows 10.3. Earlier: the Force Estimate governs every side; a senior's death has no effect of its own. Earlier: a ritual lapses a game day after liberation; retreat follows a force estimate; ritual group numbers set. Earlier: allied directive, retreat ending a ritual, undoing from Limit 5, ritual group sizes, and the 20-cell grid recorded; the one-day rule withdrawn. Earlier: ritual order in a city, allied ignorance of rituals, undoing a ritual, and the grid around the player recorded. Earlier: a ritual lasts one in-game hour; the relation of ritual and capture is open; NPC behaviour in a fight recorded. Earlier: the city as a battlefield and all three cult Arcana occupied recorded. Earlier: Arcana and the corrected Limit 1 rule recorded. Earlier: the cult's three Pathways recorded; the V1 player reaches Stages 9–8. Earlier: V1 settled: the Spirit General comes out on day 1–2. Earlier: owner's fourth answers: the ritual site, the one-day force of a liberated Place of Power, seers, how a General can be defeated, allied kills of Generals, allied cooperation, force numbers and each General's rate, the Cult Pace slider, and the V1 slice. Earlier: owner's third answers: generation-driven Cult Sorties and their odds, each General's cycle, the Spirit General replacing the teleporting reading, how rituals are stopped, the two-day ritual wait, the Last Free City Rule, and the capital's residents among the dead. Earlier: owner's second answers: capture order, Unclaimed cities and faction ownership, life under Cult Control, ritual sabotage, retreat triggers, the rituals' secret revealed only at manifestation, killing all three Generals as the only pre-manifestation ending, the Holding General's moves, and the Divine Plane restored apart from the Spirit World. Earlier: campaign length, 3×3 target world, 5–7 cities, the secret purpose of rituals, the manifestation condition, Cult Retreat, Cult Thought Link, Generals' behaviour outside cities, the Spirit World, and the Stage Occupancy Rule recorded. Earlier: first version: the pre-manifestation campaign is a war for cities led by three Stage 2 Cult Generals, with asymmetric force generation and a Capital Gate.)

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
- **Open** items are listed in [99 Open Questions](99%20Open%20Questions.md) § Cult Campaign and City Control under the IDs CC-1 … CC-44. No implementation may invent answers to them.

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
| Six non-capital cities fallen | about day 15 | Simulated with no player, in 64 % of wars, day 11–19 in most of them: Campaign Numbers V0, §8 |
| The war decided | about day 19 | Simulated with no player, day 10–24 in most wars: Campaign Numbers V0, §8 |

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

- **City Capture** begins with a Cult Sortie. The ritual runs alongside the capture. The cult clears the city of every follower of other factions by driving them out or killing them, the one-hour ritual completes, and a garrison holds the city.
- A character can sneak to the ritual site, or try to kill everyone before the ritual completes.
- The cult does not attack ordinary residents during City Capture. It targets followers.
- **City Liberation** is symmetrical: every cult follower in the city is driven out or destroyed.
- The player can attempt City Liberation alone or alongside a faction. City defence alongside a faction is also available.
- Killing every follower is not required. Cult followers may decide to retreat.
- A General does not have to be in a captured city. A General may also run from a fight, except the Holding General.
- Rituals can run in parallel in two cities when the cult has gathered enough followers.

## The city as a battlefield (owner-approved)

The city is one battlefield, and the ritual runs in one part of it ([16 Architecture Pivot](16%20Architecture%20Pivot.md)). The Combat Grid forms only around the player and reaches 20 cells in every direction. Fights the player cannot see have no grid and are simulated, tick by tick.

A combat round is one tick, so a ritual's 60-tick window, one in-game hour or ten real minutes, is up to 60 rounds for a character in the fight.

While Turn-Based Mode runs, the rest of the world stands still and lives one tick at the end of each round. Ordinary NPCs such as traders try to hide and get away from the fight. A defender who can fight, with full health and no order to retreat, goes to it by the nearest route.

## The ritual in a city (owner-approved)

1. A group of cultists goes to the ritual site, while the rest of the force clears it quickly. The group is about 5–7 followers in the campaign, more for a harder ritual, so about 13–15 of a full sortie go on. These numbers are not fixed, and each city has its own strength of defence.
2. Guards protect the site, then move forward to cut off the way to it.
3. The main force moves on into the city towards its centre.

Allied defenders know that a ritual is running and roughly where. Their directive is to beat the main force first and deal with the ritual afterwards, because a ritual has no victims. Once the main force is beaten, they go to the ritual. If they manage this inside the 60 ticks, they can stop it before it completes. A ritual group that still believes it can win stays and finishes, so a lone surviving ally does not end a ritual.

Only the player can choose to go for the ritual first. The simulation's capital battle stretches this rule, and the owner has not confirmed that (§8, CC-17).

The player can find the site by exploring the city. Once the invasion has begun, an allied NPC asked in dialogue can also mark its approximate place on the map.

## Ritual and capture (owner-approved)

The ritual runs alongside the capture, as two separate tracks:

| | The city holds | The city falls |
|---|---|---|
| **Ritual stopped** | A full defence | The cult is in the city with no Place of Power; it waits two days, and the capital stays closed |
| **Ritual completed** | It is undone, or it lapses a game day later | City Capture |

## Undoing a ritual (owner-approved)

A ritual has to be maintained once every 24 game hours. In a liberated city nobody maintains it, so it lapses one game day after the liberation.

There is a faster way. A defender of Limit 5 or deeper who knows the method undoes a ritual within 60 ticks of reaching its site.

- The allied factions have their own method, so a player who joins a faction gets it and can use it from Limit 5.
- A player outside the faction has to earn it. Once the player has its trust, the faction may reveal the method as a reward for a quest.

Undoing is therefore a way to be quicker, not a requirement. A player who frees a city with no such ally free to come can simply wait the day.

## Stopping a ritual (owner-approved)

A ritual is stopped in one of four ways:

- Kill everyone conducting it.
- Kill the senior who started it. The ritual then counts as disrupted.
- A Cult Retreat. Those conducting the ritual stop and leave without fighting.
- Exploit one of its Ritual Weak Points: persuade a cultist, pose as one of them or as a local, pretend to help and derail it, or spoil what it rests on.

The Weak Points differ from ritual to ritual, so the player cannot learn one answer and repeat it. They take planning, and they give less warlike Arcana a way to act.

The non-combat routes exist only while the cult has not fully taken the city. Once the city is cleared, a running ritual can be stopped only by fighting.

A conductor who fights goes on conducting and has 3 AP less each turn ([10.2](10.2%20Combat%20System%20Draft.md) §5b). The ritual does not stop because someone was attacked.

Once a ritual is disrupted, the attributes of the enemy's followers and General fall. They then redo the Force Estimate with that penalty, and it decides whether they fight on or leave. Against a single intruder they may try to drive that person off or fight.

A new ritual can be held in a city two game days after the previous one there, so that rituals do not run constantly. The city itself can be retaken at once, but the ritual must wait. While it waits, the city does not count as captured and does not open the capital.

## Ritual Weak Points (owner-approved)

Every ritual has **Ritual Weak Points**. In the slice it has two, chosen by the seed from the four below. The seed also sets the people, items, and places involved. Exploiting any one of them stops the ritual, which then counts as disrupted.

| Weak Point | What it is | How it is exploited | What it needs |
|---|---|---|---|
| Сомневающийся | A conductor with a personal reason to waver: family in the city, a debt, fear | Persuasion, a Large request | An Argument about that person |
| Посланник | The group waits for a person who brings the last word to go on | Deception, a Large request: pose as that person and bring a false word | A sign or word of the cult |
| Грязная тайна | The senior or a conductor hides something from the others | Intimidation, a Large request | Evidence of it |
| Основа ритуала | The ritual rests on an item or a mixture | Reaching it in Stealth Mode and swapping or spoiling it, with no roll | An item or potion made in advance |

The dialogue routes are Final Checks ([10.4 Dialogue and Social Checks](10.4%20Dialogue%20and%20Social%20Checks.md)).

**Finding them.** No list and no map mark shows a Weak Point. The player recognises one on finding its Argument and seeing the option appear in dialogue.

Arguments come from two places:

- **Before the invasion:** odd traces in the ordinary life of the city, whose meaning the player does not know yet. They were left by people who joined the General earlier, not by a hidden agent, so they lie in the past. On a later run the player knows such traces are worth looking for. The slice's scheme is in [11 Vertical Slice](11%20Vertical%20Slice.md).
- **During the invasion:** eavesdropping at the site, searching a dead follower, or reading a sleeping one with Дрёма and Чужой сон.

**At the site.** Guards challenge a stranger and let it speak. They recognise a member of an allied faction at once, so Deception is harder for it.

**After a failure.** A failed Final Check starts a fight. The followers report it through the Cult Thought Link, and part of the main force may come to the ritual. No dialogue route remains. Only the hidden route stays, if it is one of this ritual's Weak Points.

How many Weak Points a campaign ritual has, and which further kinds exist, is open (CC-8).

## Force Estimate (owner-approved)

Every group, cult or allied, decides whether to fight from a **Force Estimate**: a calculation over the strength of each side, as in strategy games such as Heroes of Might and Magic, and as the rangers of Space Rangers 2 do against the Dominators. The working calculation is in §8.

- Defenders who see that the odds are too great may fight for a moment and then flee, flee at once, or never enter the fight.
- Cult followers fight while they have a chance to win, and flee only when the balance is very strongly against them. Unlike the Dominators, they are people and can run. This rule may be dropped later.
- A change in the fight means a new estimate. A senior's death has no effect of its own: it counts through the strength it removes, so a senior who dies first will often tip the balance. A disrupted ritual counts through the lowered attributes.

## Cult Retreat (owner-approved)

Cult Retreat is collective: once followers start to flee, they flee from every point of the city at once.

They retreat when the Force Estimate is very strongly against them. The order reaches every follower at the same moment through the Cult Thought Link.

If they judge that retreat has no chance either, because they would be caught and killed, they stay and take as many allied followers with them as they can. They know the cult recovers quickly and the allies slowly.

Retreating followers head for the nearest city under Cult Control or the nearest cult force.

The player can chase and kill them while staying close enough that they do not leave the fight. The separation rule of [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md) governs that distance. Past it, they get away.

## Life under Cult Control (owner-approved)

A captured city is a closed combat zone, but residents and shops keep working, and cult followers patrol the streets.

- Stealth is possible, under [10.3 Stealth and Detection](10.3%20Stealth%20and%20Detection.md). A character who stays unnoticed can reach a stash or an NPC they need.
- Once a character is noticed as an enemy, a fight begins and every follower in the city knows where they are. The choice is to fight or to run.
- A stranger with no faction can walk through at some risk, because nobody knows them.
- Followers sense a member of a known faction, approach, and demand a check through dialogue. Refusing starts a fight. A character who agrees and belongs to no faction may only be threatened.

The dialogues themselves are later content.

## Cult Thought Link

Cult followers do not hide. They behave much like the Dominators and converge on any fight.

The **Cult Thought Link** is a chain of command, not a shared mind. It carries orders from above and reports from below, and only in exceptional cases that need everyone to act together: retreat, move to a point, a fight that matters.

- A follower in such a fight reports it, and the order can send the city's followers to it. No alarm is needed.
- Followers in the nearest city can also be sent as reinforcements.

Followers do not use the Link to ask each other things during a ritual or a battle, or to check what a stranger tells them. A lie told to one of them therefore stays unchecked until it fails.

No hidden cult follower can therefore keep a city from being liberated. The same question for allied followers during a capture is open (CC-3).

## Followers and ordinary residents

A **Follower** is a character who belongs to a Major Faction's forces. Ordinary residents are not followers, even when they live under a faction's rule.

Who else counts for capture and liberation is open (CC-3), including Minor Faction members, unaffiliated practitioners, and the player character.

## Working readings

- A character who belongs to a known faction and agrees to the check is exposed and attacked.
- A city's garrison is its followers plus one or more seniors left in charge.
- The capital ritual cannot be undone, because its completion manifests the being.

# 5. The secret purpose of rituals

## Owner-approved

A capture ritual turns a city into a **Place of Power** for the manifestation. Ordinary residents are needed alive as sacrifices, which is why the cult does not attack them. Materialising a Limit 0 being in the real world takes a very great number of sacrifices.

A Place of Power needs no visible mark on the city. It keeps its force after its city is liberated, until its ritual is undone or lapses a game day later (§4).

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
- The player can liberate cities, which closes the Capital Gate again once the city's ritual has been undone or has lapsed.
- Manifestation succeeds only if every city was captured at the moment of the capital ritual and that ritual was not interrupted. A liberated city whose ritual has neither been undone nor lapsed still counts, because its Place of Power keeps its force.

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
- During a sortie the Spirit General stands at the ritual, while the Holding and Raiding Generals go with the main force (CC-43).
- The Holding General does not Break Away or offer a Yield. This is an exception for one actor, not a change to [08 Confront Constraints](08%20Confront%20Constraints.md).
- The three behaviours are fixed archetypes across worlds. The World Seed varies who the Generals are, their bases, and their target order, so that a past world teaches how to read a General without solving the next one. Whether they vary by Arcana depends on CC-22.

## Defeating a General (owner-approved)

A character far below Limit 2 cannot win a stat race. A General falls in one of these ways:

- by numbers, alongside an allied force;
- after a disrupted ritual, which lowers the attributes of the General and all the cultists, so they are more likely to retreat than to keep fighting;
- by a character who has reached Limit 2, or Limit 3 with good statistics and strategy.

Artifacts may help. They are tightly controlled, go only to trusted members with high standing, and some are dangerous to Sanity. They are unique items for later design.

Allied forces can kill a General without the player, and on the middle setting they kill all three in about two wars of five (§8). The owner's odds of 2026-11-02 replace the earlier reading that this is rare at the middle setting. At the lowest setting the game should win by itself: the allied side can kill all three Generals, so that a player who only wants to enjoy the world can do so.

## Seers and the Spirit General

Allied factions have otherworldly seers, and the player character may be one through a seer Arcana. Seers can predict where the Spirit General will come out, so forces can gather there first.

## The final attack

Before the capital, all three Generals attack together. They may fight in different parts of the city, or in one place. This battle needs its own design (CC-17). The simulation's working model is in §8.

# 8. Asymmetric force generation

## Owner-approved

**Allied forces** are generated at world creation. During play, new followers of certain Limits appear at a small periodic rate, in the cities their faction holds. Losses outpace replacement, so the allied side can dwindle over a long campaign.

**Cult Forces** generate much faster and are not tied to the general rules of the world's population. The system gives each General new followers automatically and periodically, near that General. The game does not need to explain where they were found.

Killing rank-and-file cult followers never exhausts the cult. New followers keep appearing until its leadership is destroyed.

Campaign pace depends on difficulty: harder settings make the cult advance faster. The eighth Difficulty Setting, **Cult Pace**, owns this ([02 World Laws](02%20World%20Laws.md)).

## Allied cooperation (owner-approved)

Each allied faction acts in its own way. Factions usually act alone, gathering their own forces and liberating cities with them. When that is no longer possible and their forces are weak, they unite and attack together. Sometimes two factions move on a city at once.

## Allied conduct (owner-approved)

The world lives without the player, and both sides pursue their goals with the same seriousness. The cult captures cities, holds rituals, and ends with the final ritual in the capital. The allied side sees this and works against it.

- Allied factions act on goals, as enemy NPCs do. They decide which city to free and with what forces, gather, and go. When a city is under attack they send extra forces to hold it. They make real attempts to retake captured cities while their forces allow, and at the end they try to hold the capital.
- They cannot throw in everything, because they must defend the capital. Only a pool of their forces is free for this.
- They post assignments for other factions, Factionless characters, and otherworldly people: freeing a city, for a stated reward. This is a **Liberation Contract**.
- They want the Cult Generals dead as much as anyone, and they have Limit 2 generals of their own, the **Allied Generals**.
- Their goddess on the Divine Plane can intervene now and then, with abilities up to Limit 0: a **Divine Blessing** or strengthening of one or several of her direct followers, perhaps a global strengthening. It is never constant attention.

These events are how the war reaches the player. They give the character defences and liberations to join, so that the player takes part in the war and does not only explore.

## Odds with no player (owner-approved)

Simulations always run with no player, and the world they show lives on its own. Only the player's actions can turn these odds: gear, progression, the quality of those actions, how much the character helps in defences and liberations, and how far the character grows before the final round, possibly to Limit 2, on a par with a General.

| Setting | Allied side wins | Cult wins |
|---|---:|---:|
| Low | Most wars | Few wars |
| Middle | 40 % | 60 % |
| Higher | 20 % | 80 %, and worse further up |

At the middle setting the allied 40 % includes killing all three Generals. The slice aims at about the same 40/60 with no player, with no easier or harder variant for now ([11 Vertical Slice](11%20Vertical%20Slice.md)).

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

- At the middle difficulty the allied factions have a good chance of holding a city without the player, especially against the first attempts, and of stopping the ritual in time.
- On harder settings a city is almost certain to fall unless someone interferes, and the ritual will nearly always complete.

After a capture, each General gathers its next force in its own way (§7), and the garrison stays behind.

## Campaign Numbers V0 (authoring defaults)

The owner left balance calculations to the agent on 2026-11-02. These values are checked by `tools/balance/campaign_sim.py`: a whole war with no player, which ends in manifestation or with all three Generals dead. Manifestation counts as the cult's win here, because the fight against the being is not simulated. The Gate counts only cities under Cult Control. Every value here can change in playtest.

### Results with no player

Each row is 3000 wars. A figure from 500 wars moves by about two points.

| Setting | Cult Pace | Cult followers | Cult General attributes | Allies win | Cult wins | Undecided on day 60 | War ends, median day |
|---|---:|---|---:|---:|---:|---:|---:|
| Lowest | ×0.5 | One Limit shallower | 55 | 98 % | 0 % | 2 % | 22.5 |
| Low | ×0.75 | Half a Limit shallower | 70 | 70 % | 29 % | 1 % | 21 |
| Middle | ×1 | As listed below | 75 | 42 % | 57 % | 1 % | 19 |
| High | ×1.25 | Half a Limit deeper | 75 | 19 % | 78 % | 3 % | 16 |
| Highest | ×1.5 | One and a quarter Limits deeper | 75 | 8 % | 90 % | 2 % | 12 |

Half a Limit deeper means that half of the followers stand one Limit deeper, down to Limit 5.

Working reading: these five settings move Cult Pace, Combat Difficulty, and at the low end the Generals' attributes together, so they are a ladder of presets, not the effect of one slider (CC-43).

### Cult Forces

| General | Followers per day | Limit mix |
|---|---:|---|
| Spirit | 6.0 | 75 % Limit 9, 25 % Limit 8 |
| Raiding | 4.0 | 50 % Limit 9, 35 % Limit 8, 15 % Limit 7 |
| Holding | 2.8 | 35 % Limit 9, 40 % Limit 8, 20 % Limit 7, 5 % Limit 6 |

- These are the rates with no city held. Each held city adds the base rate once more, so one city doubles it and three multiply it by four. Early runs without it lasted 28–40 days (working reading, CC-43).
- A General launches a sortie at 18–22 followers, set by the seed. Six go to the ritual.
- A force waiting for a target stops growing at 30, and a city holds at most 30.
- While a held city has fewer than 10 followers, half of a General's new followers go to it.
- After a failed sortie a General waits one day.
- The Holding General leaves 5 behind, and the Raiding General walks away with 3.

### Allied forces

- An ordinary city starts with 10 followers and the capital with 24, in the mix 55 % Limit 9, 30 % Limit 8, 12 % Limit 7, 3 % Limit 6.
- Two Limit 5 and one Limit 4 stand in random cities, and one Limit 3 stands in the capital. The world starts with about 88 allied followers.
- Each held city gains 0.35 followers a day, and the capital 0.7. Of these, 70 % are Limit 9 and the rest Limit 8.
- There are three Allied Generals, Limit 2 with attributes of 75 (working reading, CC-44).
- About 20 outsiders can take a Liberation Contract, in the mix 60 % Limit 9, 30 % Limit 8, 10 % Limit 7. The pool regains 0.25 a day.

An ordinary city holds about half of the "full city" of 20 named above.

### Allied conduct in numbers

The simulation runs one allied command for all three factions, a simplification of "Allied cooperation" above (CC-16).

- **The free pool.** An ordinary city keeps 8 followers at home. The capital keeps 40 % of all allied followers, and at least 12. Everything above that is free.
- **Defence.** When a city is attacked, the command draws free followers until its Force Estimate against the main force reaches about 0.9 to 1.1. If it cannot reach 0.9 it sends nobody, and the city fights alone.
- **Liberation.** Once a day the command considers every captured city, a city with a General in it first, then the weakest garrison. It goes at an estimate of 1.75, or at 1.25 when at most one ordinary city is still free or the final attack is coming.
- **Liberation Contract.** When the free pool falls short, the command hires 4 outsiders for that attempt. If it still falls short the contract is cancelled. Survivors return to the pool of outsiders.
- **Allied Generals.** Two stay at home, so at most one is away, and it answers a call 30 % of the time. It joins a defence only against a General in the main force, and it goes first to a hunt or to a city a General holds. It rests half a day after a task. A General seeks out the rival General in a fight.
- **Last General.** When one Cult General is left, every Allied General may leave, and seers and hunts work four times as often.
- **Seers.** A warning comes on 15 % of the Spirit General's sorties, and an Allied General may then wait at the place. The Raiding General is found on 8 % of days and is hunted at an estimate of 1.2.
- **Divine Blessing.** It multiplies a side's blows by 1.3 for one fight that has an Allied General, at most once in 2 days. A global strengthening is not simulated.

### The final attack

This is a working model, because the design of the final battle is open (CC-17).

- One day after the Gate opens, the cult attacks the capital with every General still alive, every free force, and 60 % of each garrison. Twelve of them conduct the ritual.
- The capital fights with everyone in it, every Allied General, and every outsider. A quarter of the defenders and one Allied General hold the capital's centre, where the ritual stands, and meet it at once. The rest meet the attackers and go to the ritual if they win in time.
- If the ritual is stopped, the cult tries again two days later, as long as the Gate stays open.

The second point stretches the rule of §4 that only the player can choose to go for the ritual first. Here the ritual stands at the fixed centre of a city the defenders already hold, so the model does not treat this as a choice. The owner has not confirmed that reading.

### Force Estimate and fights nobody sees

A fight off-screen runs tick by tick over a ledger of followers per Limit. It uses a kill table built from Combat Numbers V0 ([10.2](10.2%20Combat%20System%20Draft.md) §5b):

```text
kills per tick of one i on one j = 0.62 × attacks × hit chance / hits to kill
hits to kill = HP of j / (blow of i × (1 − reduction of j)), at least 1
Force Estimate of A against B = (kills per tick of A × size of A) / (kills per tick of B × size of B)
```

A table replaces a single strength value because hits are capped at 5 %, so a scalar cannot express a Limit 9 against a Limit 2. Above 1, side A should win. Each side rolls one luck factor for the fight, log-normal with a spread of 0.2. Wounds carry over between ticks.

- Defenders stay out of a fight at an estimate below 0.35, and allied forces break off below 0.25.
- Cult Retreat comes when the cult's own estimate falls below 0.15. Those who retreat suffer 8 more ticks of attacks, and the Holding General never retreats.

The ledger reproduces the two extremes of the tactical simulation: 5 Limit 5 against a General 87 % (tactical 87 %), and a Limit 8 against a Limit 9 98 % (93 %). It is harsher on defenders in small even fights. The slice's 5 allies against 5, 6, and 7 give 29, 8, and 1 %, against 78, 46, and 12 % in the tactical simulation, which has allied abilities the ledger does not. The campaign's odds were set on the ledger, so they carry this bias (CC-15).

### Results at the middle setting

These are 500 wars from the default run.

| Target | Result |
|---|---|
| First sorties on days 3–5, 5–7, 7–8 | Mean days 3.4, 5.5, 7.0 |
| One sortie every 1–2 days | 12.6 sorties by day 18 |
| Allies hold the first attempts | The first sortie takes its city in 14 % of worlds |
| The war lasts 15–20 days | Median day 19, 10th to 90th percentile days 10–24. Allied wins come on days 9–20, and cult wins on days 17–26 |
| Real attempts to retake cities | 5.2 liberations per war, and 4.8 of them succeed |
| Allies hunt the Generals | 1.7 Cult Generals die per war, and 1.3 Allied Generals |
| Help from outside | 2.4 Liberation Contracts and 3.5 Divine Blessings per war |
| The final battle is a real fight | The Gate opens in 64 % of wars, on day 14.6 on average. There are 1.8 final attacks per war, and 1.2 are stopped |
| Losses outpace replacement | 88 allies on day 0, 87 on day 5, 44 on day 10, 24 on day 15 |
| Fifty followers is too many | The largest garrison is the cap of 30 |

A city falls in 67 % of sorties, and the ritual completes in 77 %. At the Highest setting a city falls in 77 % of sorties, below the "almost certain" asked for above, because the allied response now counts (CC-43).

### One slider at a time

Each row changes one value from the middle setting. These are 500 wars each.

| Changed | Allies win | Cult wins | A city falls | War ends, median day |
|---|---:|---:|---:|---:|
| Cult Pace ×0.5 | 66 % | 28 % | 56 % | 30 |
| Cult Pace ×0.75 | 49 % | 50 % | 63 % | 23 |
| Cult Pace ×1.25 | 37 % | 62 % | 66 % | 17 |
| Cult Pace ×1.5 | 35 % | 61 % | 70 % | 15 |
| Followers one Limit shallower | 56 % | 43 % | 60 % | 20 |
| Followers half a Limit shallower | 45 % | 55 % | 65 % | 20 |
| Followers half a Limit deeper | 22 % | 76 % | 71 % | 18 |
| Followers one and a quarter Limits deeper | 10 % | 87 % | 78 % | 15 |

Cult Pace mostly moves the length of the war, and Combat Difficulty mostly moves who wins it.

### The lowest setting

The lowest setting needs more than a slow cult, because allied followers of Limit 6 or shallower hardly hurt a General. Working reading for it: the Generals' attributes fall to 55 (CC-43). All three Generals then die in 98 % of wars, on day 24.5 on average, and a city falls in 35 % of sorties.

## Working readings

- "Not tied to the general rules" applies to how often followers appear and where they come from. It does not change character rules. Generated cult followers use the same attributes, skills, Limits, and derived-statistic formulas as every other actor, as [02 World Laws](02%20World%20Laws.md) requires. They are generated at their stage and do not play through Alchemy or Conduct Progress.

## Open

Exact rates, Limit mix, and cap are CC-12. Allied starting strength and replacement are CC-13. Allied Generals, Liberation Contracts, and the Divine Blessing are CC-44.

# 9. The player in the campaign

The player may:

- join a city's defence alongside a faction;
- liberate a city alone or with allies;
- pursue a General;
- disrupt a ritual through the Approaches available at the site;
- work elsewhere and let the war continue.

None of these is mandatory, and the world does not wait. The pressure to take part comes from the odds of §8: with no player the cult wins most wars from the middle setting up. A player who does nothing reaches the outcome the World Engine produces for that difficulty, as [02 World Laws](02%20World%20Laws.md) describes for the capital confrontation.

As allied forces wear down, the player's share of the defence grows. This is a pressure that emerges from the war, not a chosen-one role: [01 Player Experience](01%20Player%20Experience.md) still applies.

# 10. Information

City Capture, City Liberation, and control changes are significant public events. They reach the player through daily public news, rumours, ambient NPC speech, briefings, and the map's qualitative control layer, under the rules of [09 Information Sources](09%20Information%20Sources.md).

Knowing a city's control is not the same as knowing a General's position. How a General's location becomes known is open (CC-10).

The Cult Thought Link is a defined channel under [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), not global NPC knowledge. It reaches cult followers only.

No source in the world reports the purpose of the rituals.

Captured and liberated cities must look different in the world. Their Symptoms are part of CC-6.

# 11. World Engine and simulation budget

The campaign adds state that [02 World Laws](02%20World%20Laws.md) did not previously track: City Control, the forces present in each city, and three General actors.

The World Engine remains an operation scheduler, not a population simulation. Proposed direction, subject to CC-15: each city keeps a coarse ledger of followers per faction and Limit, and only instantiates characters when the player enters. Its working calculation is in §8. This follows [04 Points of Interest](04%20Points%20of%20Interest.md)' Lazy Activation.

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
- **Solo fight against a whole garrison.** The Cult Thought Link draws a city's followers to one fight. A lone character can meet all of them, so the size of a garrison and how they arrive decide whether solo liberation is possible (CC-8).
- **Content cost of ritual conditions.** Conditions that differ every time need a set of authored parts that the World Seed combines, which is a real load for a solo developer (CC-8).
- **Slice scope.** The slice's required mechanics pull many undesigned systems into the first build (CC-29, CC-32, CC-33).
- **Emptied cities after manifestation.** If captured cities lose their residents, the world after manifestation has few people, services, or quests left (CC-20).
- **Time to reach a General.** Direct defeat takes Limit 2, or Limit 3 with good statistics and strategy, inside a 15–20 day campaign. A Limit 7 character has a 1.32 Limit multiplier against a General's 2.66 ([07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md)). If the player cannot get deep enough in time, allied numbers and disrupted rituals carry that whole path (CC-10).
- **Terrain evidence.** The target world and city count sit outside the conditions of the terrain research so far (CC-2).
- **A war won without the player.** At the middle setting a player who does nothing still wins about two wars of five. That is the owner's chosen odds, and it means a passive run can end in a rating. The rating must show the difference (CC-43).
- **The player as saviour.** "Only the player can turn the odds" must stay a matter of one more strong actor in a real war, not a chosen-one role. Allied Generals who kill Generals on their own are what keeps this true.

# 13. Scope

## V1 (owner-approved)

V1 is the one-city Vertical Slice of [11 Vertical Slice](11%20Vertical%20Slice.md): the Spirit General comes out on day 1–2, the ritual starts at once, the General leaves after 15–20 ticks, and the player either stops the ritual and wins or loses. The player reaches Limits 9–8, Limit 7 is for NPCs, and allies and enemies may be deeper. A General that does not attack is an approved fallback.

The following V2 and V3 split is proposed, not approved.

## V2

- Persuading a cult follower in dialogue that a ritual was pointless, so that the follower undoes it. This is an option for later and is not designed.
- The full war: 5–7 cities, three Generals, City Control, and the Capital Gate.
- Allied conduct: defence, liberation, the capital's reserve, Allied Generals, Liberation Contracts, and the Divine Blessing.
- Coarse per-city force ledger; off-screen clashes resolved as bounded operations.
- Public news and map control layer.

## V3

- Richer city consequences: services, displacement, and return.
- Cross-city logistics.
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
