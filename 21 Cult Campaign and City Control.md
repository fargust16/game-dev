# 21 Cult Campaign and City Control

Version: 0.1

Status: Draft — owner-approved direction, incomplete specification

Last Updated: 2026-11-01 (first version: the pre-manifestation campaign is a war for cities led by three Stage 2 Cult Generals, with asymmetric force generation and a Capital Gate.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [AGENTS.md](AGENTS.md), future faction AI, settlement state, balance, and world-event specifications.

---

# Purpose and authority

This document defines how the campaign advances before the sealed Stage 0 being manifests: how the cult takes the playable state city by city, how allied forces and the player push back, and how the world reaches the capital ritual with or without the player.

[02 World Laws](02%20World%20Laws.md) states the campaign's world-level laws. This document holds its operating rules. It is a design draft, not an implementation specification or implementation authorisation.

Three kinds of statement appear below:

- **Owner-approved** statements are decisions recorded from the owner's concept of 2026-11-01.
- **Working readings** are how this document interprets the concept where the owner's words leave a gap. They hold until the owner confirms or replaces them.
- **Open** items are listed in [99 Open Questions](99%20Open%20Questions.md) § Cult Campaign and City Control under the IDs CC-1 … CC-19. No implementation may invent answers to them.

# 1. Campaign shape

```text
World creation: every city is under allied control; the cult holds none
        ↓
Allied forces start large and shrink; Cult Forces keep growing
        ↓
A Cult Force gathers enough followers and moves on a city
        ↓
City Capture: other factions' followers are removed, the city is held, the ritual completes
        ↓
Cult Control ←——— City Liberation returns the city ———→ the cult must take it again
        ↓
Every city except the capital is under Cult Control: the Capital Gate is open
        ↓
Capital Awakening Ritual at the fixed centre of the capital
        ↓
Manifestation of the Sealed Being
```

The campaign runs without the player. The player may defend, liberate, hunt commanders, work for a faction, or do none of these. Every step above can happen off-screen.

The reference for the intended feel is the Dominator war in *Space Rangers 2*: a side that keeps producing new units against a side that was generated once and wears down. This is a comparison of pressure curves only. No names, lore, or exact rules are taken from that game.

# 2. Cities and City Control

## Owner-approved

- At world creation the whole playable state belongs to the three allied Major Factions. The cult holds no city.
- The cult's objective before manifestation is to capture cities by conducting rituals in them.
- The capital is the last target. The cult works on every other city first.
- Neutral territory exists outside the cities. The Raiding General's base stands there.

## City Control

**City Control** is the state saying which side holds a city. It has two values:

- **Allied Control** — the city is held by the allied side.
- **Cult Control** — the city was taken through City Capture and has not been liberated.

Control changes only when City Capture or City Liberation completes. During an attack, the city keeps its current control.

## Working readings

- "The whole world belongs to the allied factions" is read as "every city". Land between cities carries no City Control and stays neutral.
- City Control is a side-level state. Which of the three allied Major Factions holds a given city, and whether a liberated city returns to the same one, is open (CC-7).

# 3. City Capture and City Liberation

## Owner-approved

- **City Capture** requires the cult to remove every follower of other factions from the city, by driving them out or killing them, and to hold the city.
- The cult does not attack ordinary residents during City Capture. It targets followers.
- **City Liberation** is symmetrical: every cult follower in the city is driven out or destroyed.
- The player can attempt City Liberation alone or alongside a faction. City defence alongside a faction is also available.

## Followers and ordinary residents

A **Follower** is a character who belongs to a Major Faction's forces. Ordinary residents are not followers, even when they live under a faction's rule.

Who else counts for capture and liberation is open (CC-3), including Minor Faction members, unaffiliated practitioners, and the player character.

## Working readings

- The cult must keep a presence in a captured city. A city with no cult followers meets the liberation condition, so a force that leaves entirely loses the city. The Raiding General's departure therefore leaves a garrison behind; its size is open (CC-4).
- The six-hour ritual window of [02 World Laws](02%20World%20Laws.md) stays the current target. It is read as the period the cult must hold the ritual while allied forces and the player can still interrupt it. Its order relative to clearing the city is open (CC-4).

## Change from the earlier draft

The earlier draft described rituals as civilian-targeted. This concept replaces that targeting: the cult's violence in a capture is aimed at followers, not residents. What the ritual itself does to a city and its residents is open (CC-5).

# 4. Capital Gate

## Owner-approved

- The Capital Awakening Ritual can begin only when every other city is under Cult Control. This rule is the **Capital Gate**.
- The cult therefore captures the other cities first.
- The fall of the last other city tells the player that the capital is next.
- The player can liberate cities, which closes the Capital Gate again.

## Consequences

The Capital Gate replaces the earlier undecided "required number of rituals". The number of rituals the cult needs is the number of non-capital cities plus every recapture forced by City Liberation.

City Control is the campaign's public progress reading. It replaces the hidden return threat described in the earlier draft. There is still no numeric meter or countdown: the player reads the campaign from reported control of cities, under [09 Information Sources](09%20Information%20Sources.md).

What happens when a city is liberated while the Capital Awakening Ritual is running is open (CC-17).

# 5. Cult Generals

## Owner-approved

The cult's forces are divided into three **Cult Forces**, each led by a **Cult General**. All three Generals are Stage 2 followers. They share the objective of capturing cities through rituals and differ in how they act. Those differences require different responses from the player.

The labels below describe roles only. They are not final names.

| Working label | Owner-approved behaviour | What it asks of the player |
|---|---|---|
| **Teleporting General** | Can teleport and moves around the world constantly. | Predict and intercept instead of chasing. |
| **Holding General** | Arrives at one zone and stays there in person, trying to take one specific city. Fights to the end. Hardest of the three in battle. | Prepare for one decisive fight, or work around it. |
| **Raiding General** | Arrives, captures a city, then withdraws to a base in neutral territory and raids from there. | Find the base and break the raiding cycle. |

## Working readings

- A General is a persistent named actor, not a generated follower. Whether a killed General is replaced is open (CC-9).
- "Fights to the end" means the Holding General does not Break Away or offer a Yield. This is an exception for one actor, not a change to [08 Confront Constraints](08%20Confront%20Constraints.md).
- The three behaviours are fixed archetypes across worlds. The World Seed varies who the Generals are, their Pathways, their bases, and their target order, so that a past world teaches how to read a General without solving the next one.
- Stage 2 Generals are a full-game world premise, in the same sense as the Stage 2–3 head of the covert alchemical network in [02 World Laws](02%20World%20Laws.md). They do not widen V1's Stages 9–7 player scope in [05 Progression](05%20Progression.md).

## Conflict to resolve

[02 World Laws](02%20World%20Laws.md) lists Stage 1 followers and allows Stage 1 ritual leaders. Stage 1 is deeper than Stage 2, so a Stage 1 cult follower outranks a General in power. Whether anyone stands above the three Generals is open (CC-9). The answer also settles who the "leader" of the reinforcement rule in §6 is.

# 6. Asymmetric force generation

## Owner-approved

**Allied forces** are generated at world creation. During play, new followers of certain stages appear at a small periodic rate. Losses outpace replacement, so the allied side can dwindle over a long campaign.

**Cult Forces** generate much faster and are not tied to the general rules of the world's population. They simply gain new followers periodically.

Killing rank-and-file cult followers never exhausts the cult. New followers keep appearing until its leadership is destroyed.

When a Cult Force has gathered a certain number of followers, it attempts to capture a city and conduct a ritual there.

Campaign pace depends on difficulty: harder settings make the cult advance faster.

## Working readings

- "Not tied to the general rules" applies to how often followers appear and where they come from. It does not change character rules. Generated cult followers use the same attributes, skills, Pathway stages, and derived-statistic formulas as every other actor, as [02 World Laws](02%20World%20Laws.md) requires. They are generated at their stage and do not play through Alchemy or Conduct Progress.
- The strength threshold belongs to each Cult Force, not to the cult as a whole.

## Open

Spawn location, stage mix, cap, and the in-world source of new followers are CC-12. Allied starting strength and replacement are CC-13. Which difficulty setting owns campaign pace is CC-14: [02 World Laws](02%20World%20Laws.md) forbids a single slider from silently bundling unrelated effects, so this cannot be attached to a slider by default.

# 7. The player in the campaign

The player may:

- join a city's defence alongside a faction;
- liberate a city alone or with allies;
- pursue a General;
- disrupt a ritual through the Approaches available at the site;
- work elsewhere and let the war continue.

None of these is mandatory, and the world does not wait. A player who does nothing reaches the outcome the World Engine produces for that difficulty, as [02 World Laws](02%20World%20Laws.md) describes for the capital confrontation.

As allied forces wear down, the player's share of the defence grows. This is a pressure that emerges from the war, not a chosen-one role: [01 Player Experience](01%20Player%20Experience.md) still applies.

# 8. Information

City Capture, City Liberation, and control changes are significant public events. They reach the player through daily public news, rumours, ambient NPC speech, briefings, and the map's qualitative control layer, under the rules of [09 Information Sources](09%20Information%20Sources.md).

Knowing a city's control is not the same as knowing a General's position. How a General's location becomes known is open (CC-10).

Captured and liberated cities must look different in the world. Their Symptoms are part of CC-6.

# 9. World Engine and simulation budget

The campaign adds state that [02 World Laws](02%20World%20Laws.md) did not previously track: City Control, the forces present in each city, and three General actors.

The World Engine remains an operation scheduler, not a population simulation. Proposed direction, subject to CC-15: each city keeps a coarse ledger of followers per faction and stage, and only instantiates characters when the player enters. This follows [04 Points of Interest](04%20Points%20of%20Interest.md)' Lazy Activation.

An occupied city is persistent world state rather than a timed POI, because it has no Active Window. Discrete events inside it, such as a ritual or a clash, remain POIs.

# 10. Design check

Filter of [AGENTS.md](AGENTS.md):

| Question | Result |
|---|---|
| Creates curiosity? | Yes, if each General has readable tells and the player can ask "where next, and why there?" |
| Teaches about the world? | Yes: General behaviour, tells, and control patterns are prime Player Knowledge targets. |
| Power has a price? | Partly. Fighting for a city costs time elsewhere; costs of allied help are open. |
| Multiple options viable? | **Not yet proven.** "Drive out or destroy" reads as combat only. Non-combat routes are required by [08 Confront Constraints](08%20Confront%20Constraints.md) (CC-8). |
| Failure valuable? | **At risk.** Liberating a city that falls again the next day may only waste time. Liberation needs a lasting payoff (CC-7). |
| World changes afterward? | Yes: control, residents, services, and the Capital Gate. |

Known risks:

- **Constant urgency against curiosity.** A war with a visible front can crowd out exploration, the game's first pillar. Campaign length decides this (CC-1).
- **Geography scale.** The V0 world is 2.8 km across, and [the G2 brief](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) gives a 4.5 m/s run. Crossing it takes about 10 real minutes, or about 62 ticks. That is 17% of a six-hour ritual window, so travel creates little Attention pressure and teleportation adds little. The same brief targets 3–5 settlements, which leaves 2–4 non-capital cities for three Generals. Both need a decision (CC-2).
- **Single-actor deadlock.** If one hidden follower blocks a capture or a liberation, one character in a cellar can freeze a city. The "every follower" rule needs a definition that closes this (CC-3).
- **Power gap.** A Stage 7 character facing a Stage 2 General has a 1.32 stage multiplier against 2.66, before abilities ([07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md)). Defeating a General needs a route other than a stat race (CC-10).

# 11. Scope

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

- **Vision / Pillars / Player Experience:** Compatible. The world acts without the player, and the player is not a chosen saviour. Curiosity under war pressure is a tracked risk (§10).
- **World Laws:** Updated. City capture replaces the required-ritual count and the hidden return threat; civilian targeting is withdrawn.
- **Decision Engine:** Compatible once non-combat routes exist. Stale single-difficulty wording corrected.
- **Points of Interest / Information Sources:** Updated for persistent occupied cities and public City Control.
- **Progression:** Updated. Stage 2 Generals are a full-game premise and do not widen V1's player stages.
- **Character Life:** Updated. Manifestation is the Capital Awakening Ritual's completion; a manifestation during a Death Gap is open (CC-18).
- **Confront:** Compatible in principle; §10 records the requirement for real alternatives.
- **World Time:** Compatible. Generation and ritual windows use the shared clock.
- **Glossary / Open Questions / AGENTS.md:** Updated.

# Related Documents

- [02 World Laws](02%20World%20Laws.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [09 Information Sources](09%20Information%20Sources.md)
- [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
