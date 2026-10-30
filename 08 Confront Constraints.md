# 08 Confront Constraints

Version: 0.3

Status: Draft

Last Updated: 2026-08-07

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
[16 Architecture Pivot](16%20Architecture%20Pivot.md), Future combat system, weapon content, and encounter tuning.

---

# Purpose

This document defines the hard constraints for **Confront** before any full combat system is designed.

It answers:

> What must always be true about fighting, so combat cannot quietly become the whole game?

This is not a combat design bible.
It is a fence around future combat work.

# Central Rule

Confront is one Approach among six.

If combat becomes the safest, richest, or most informative default, the design has failed and must be revised before more combat content is added.

# What Confront Is

Confront means forcing an issue openly through violence or immediate physical coercion:

- a fight;
- breaking through;
- dragging, restraining, or striking to end a situation;
- open defiance that invites violence.

Threaten may escalate into Confront.
Veil may collapse into Confront when spotted.
Convince and Exchange may fail into Confront.
Withdraw may become unavailable once Confront has fully locked in.

# Hard Constraints

## 1. No XP from violence

Kills, wounds, and wins do not grant experience levels.

They may:

- contribute to Open Path Conduct Progress when appropriate;
- create Legacy residue;
- change Influence, Symptoms, and access;
- drop only what the target actually carried or guarded.

They must not become a grind loop.

## 2. Death is real

Losing Confront can end the Character Life.

V1 may also allow costly escapes, yields, or mercies, but design must not rely on free reload-as-same-person after every bad fight.

## 3. Loot is diegetic

Combat rewards are not loot tables from nowhere.

A defeated target yields:

- what they carried;
- what they were guarding;
- what the scene logically contains.

No “combat completion chest” that exists only because a fight ended.

## 4. Information is not guaranteed by victory

Winning a fight does not automatically explain motives, routes, or sponsors.

Information from Confront requires a method:

- a Yield Threshold confession;
- documents on the body;
- a witness;
- a follow-up Trace POI;
- a tool/skill/artifact that extracts meaning.

Murder can destroy the best informant.

## 5. Confront always leaves residue

An important Confront should change at least one of:

- local safety / patrol Symptoms;
- faction opinion or Influence;
- access to mentors, shops, or districts;
- available follow-up POIs;
- the character's Conduct Progress or Bargain state.

A fight that only spends HP and returns the street to normal is too weak for an important node.

## 6. Alternatives must remain real

Wherever Confront is offered in an important Decision Node, at least one non-Confront Approach should usually remain meaningful beforehand or nearby:

- Convince;
- Threaten without full battle;
- Exchange;
- Veil;
- Withdraw before lock-in.

If the only profitable option is Confront, redesign the node.

## 7. Flee and mercy are first-class

V1 Confront must support:

- **Break Away** — a pressured form of Withdraw with cost/risk;
- **Spare / Accept Yield** — especially at Yield Threshold, often trading safety for information, debt, or later enmity.

These preserve Decision Engine values inside combat, instead of reducing every fight to “delete HP to zero.”

## 8. Pathway bias, not Pathway prison

Open Path may favour Confront and Threaten for Conduct Progress.

Veiled and Bound Paths may treat frequent open massacres as dissonant.

No Pathway may make Confront impossible if the fiction demands it.
No Pathway may make Confront free of price.

# Costs of Confront

Typical Confront costs, alone or combined:

- injury, maiming, or death;
- noise that attracts patrols or rivals;
- legal heat / district hostility;
- lost informants;
- burned Allegiance;
- spent scarce stocks;
- time inside an Active Window;
- escalation that closes Veil and Convince options.

Future combat numbers must express these costs, not erase them.

# V1 Combat Depth

V1 Confront should stay solo-achievable.

Required in V1:

- clear entry into Confront from a Decision Node / POI / escalation;
- player can fight, attempt Break Away, and respond to Yield Threshold;
- outcomes include win, lose/death, costly escape, spare/yield;
- residue is applied to world state;
- no XP pipeline;
- no mandatory deep combo system.

Not required in V1:

- huge weapon trees;
- complex stance graphs;
- perfect physics melee;
- multiplayer-grade AI.

## Presentation mode

V1 Confront uses **Turn-Based Mode on a Combat Arena scene**.

Reference feel, not feature parity:

- Baldur's Gate 3 style initiative / turn order once conflict locks in;
- Fallout 1/2 style deliberate tactical choice on a dedicated arena.

Rules:

1. When Confront locks in, the game transitions to a **Combat Arena** scene (load or additive — implementation later). Ordinary Confront is **not** resolved in-location on the overworld or interior exploration scene.
2. The arena is chosen from a **Theme Pack** matching the source location mood (street, tavern, cellar, woods, …). It must feel thematically related; it need not clone exact exploration geometry.
3. Turn-Based Mode exists **only on arenas**. There is no voluntary Turn-Based Mode during free exploration.
4. Overworld and interior scenes stay free third-person ([16 Architecture Pivot](16%20Architecture%20Pivot.md)).
5. Break Away, Yield/Spare, and other Decision Engine outcomes remain valid inside arena Turn-Based Mode.
6. On Confront end (win, Break Away, Yield/Spare resolution, or death), return to the source scene and apply residue.
7. V1 stays simple: readable turns, clear actions, solo-shippable depth. Do not chase full BG3 feature parity.

This locks presentation. The tactical action economy is defined by Initiative, Action Points (AP), variable AP costs, and defensive reactions in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md). Arena spatial truth is a square grid with Environment Actions; see [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md).

# Escalation Rules

Useful V1 escalation pattern:

```text
Threaten / Veil / Convince / Exchange
        ↓ (failure, refusal, or spotted)
Confront available or forced
        ↓
Break Away / Spare / Kill / Die
        ↓
World residue + possible Trace POI
```

Not every scene must escalate.
Some scenes may begin already in Confront.

# Relation to Other Systems

| System | Confront interaction |
|--------|----------------------|
| Decision Engine | Confront is an Approach option with price |
| Approaches | Must not dominate Convince / Veil / Exchange / Withdraw |
| Progression | May feed Open Path Conduct; never XP levels |
| POI / World Laws | Fights can interrupt Operations, create Symptoms, open Traces |
| Character Life | Death in Confront triggers Death Gap and Legacy rules |
| Bargain / External Power | Curses or blessings may help or sabotage Confront later |

# Anti-Patterns

Forbidden design directions:

- combat as the main progression engine;
- best loot locked behind mandatory fights;
- dialogue that only exists to enter combat;
- enemies as walking XP/bags;
- perfect information after every kill;
- free save-scum assumed as balance;
- stealth and speech options that only delay the “real” fight reward.

# Solo Scope

## V1

- Constraints above are binding for all Confront content.
- Simple fight resolution with Break Away and Yield/Spare.
- Diegetic loot only.
- Residue on world/factions.
- Turn-Based Mode on a thematic Combat Arena for Confront only; free exploration outside arenas.

## V2

- More weapon/method identity under Confront without breaking constraints.
- Richer escalation from other Approaches.
- Stronger Conduct and Soft Memory hooks after public violence.

## V3

- Deep combat craft only if Approaches outside Confront remain equally authored.
- Advanced control-loss Bargains affecting Confront.
- Still no XP-from-kills loop.

# Design Validation

Before adding a fight, ask:

1. Could another Approach solve or reshape this situation?
2. Does victory teach the world, or only pay numbers?
3. What residue remains tomorrow?
4. Can spare / yield / escape create an interesting story?
5. Would a curious Player Knowledge-heavy character ever choose not to fight here?

If the honest answers collapse to “just fight,” redesign.

# Documentation Review

- **Vision / Pillars:** Compatible. Violence is costly, curious alternatives remain, knowledge beats grind.
- **Approaches:** Compatible. Confront is fenced so it cannot erase the other five.
- **Progression / Character Life:** Compatible. No XP; death is real; Conduct may care about open violence.
- **World Laws / POI:** Compatible. Fights are world events with Symptoms and possible Traces.
- **Glossary / Open Questions:** Presentation locked to Combat Arena Turn-Based Mode ([16 Architecture Pivot](16%20Architecture%20Pivot.md)). Exact action economy numbers remain open.

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [06 Approaches](06%20Approaches.md)
- [03 Decision Engine](03%20Decision%20Engine.md)
- [05 Progression](05%20Progression.md)
- [07 Character Life](07%20Character%20Life.md)
- [02 World Laws](02%20World%20Laws.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
