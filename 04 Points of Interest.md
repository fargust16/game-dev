# 04 Points of Interest

Version: 0.1

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)

Referenced By:
Future map, exploration, side-event, quest, and world-presentation design.

---

# Purpose

This document defines how Side Events, discoveries, and Decision Nodes appear in the playable world without simulating every scene all the time.

It is an implementation-facing design system for solo development:

- light on CPU;
- clear on the map;
- compatible with a living, finite world;
- built around curiosity and timed commitment.

Inspiration for presentation: map-marked Points of Interest similar in role to those in The Witcher 3 and Cyberpunk 2077. This document does not copy their quest logic; it defines our own timed, seed-driven, lazily activated model.

# Core Idea

The game does **not** constantly simulate every Side Event as a living staged scene.

Instead, the World Seed and World Engine create **Points of Interest (POIs)**:

- visible or discoverable markers on the world map;
- each POI packages one interaction opportunity;
- the heavy scene / Decision Node starts only when the player enters that POI's interaction area;
- every POI has an **Active Window** measured in player world-time;
- if the window ends without player participation, the POI resolves unattended and may still change the world.

This is how we get living-world residue without running a city simulation every second.

# Reconciliation with Living World

Important distinction:

| Layer | What it does while the player is elsewhere |
|-------|--------------------------------------------|
| World Engine | May create, modify, expire, or prioritise POIs and abstract Operations |
| POI package | Remains dormant: no full scene, no crowd AI, no dialogue tree running |
| Active Window timer | Still advances with player world-time |
| Unattended resolution | When the window ends, the outcome is decided without the player and applied as Influence / Symptoms / path changes |

So:

- the world is not frozen;
- the hardware is not asked to stage every drama at once;
- missed opportunities still matter.

Unattended resolution must not be opaque coin-flip randomness. It is weighted by Faction Influence, local conditions, and the POI's designed outcome table, and it must leave Readable Consequences / Symptoms.

# Point of Interest

A **Point of Interest** is a seed-driven map opportunity that can host a Decision Node or a lighter discovery beat.

Minimum contents:

- id / seed parameters;
- map position and interaction radius;
- POI type;
- discovery state;
- Active Window;
- linked Decision Node, or a simple discovery result;
- unattended resolution table;
- world residue on participate / miss.

# Lazy Activation

## Dormant

While the POI is active on the map but the player has not entered its interaction area:

- no full encounter logic runs;
- no need to simulate the apothecary arrest animation across the city;
- only lightweight data exists: marker, timer, discovery flags, abstract weights.

## Activated

When the player enters the interaction area during the Active Window:

- the Decision Node / encounter package loads;
- the player may use Attention, Approach, Allegiance, Commitment, Bargain as designed;
- participation overrides unattended resolution for this POI.

## Expired

When the Active Window ends without activation:

- run unattended resolution;
- apply world residue;
- remove or transform the map marker;
- optionally spawn follow-up POIs or Symptoms elsewhere.

# Discovery

POIs may be found by:

- stumbling into the interaction area;
- newspapers;
- notice boards;
- bartenders and other informants;
- rumours, letters, faction followers;
- another POI that reveals a clue to this one.

## Discovery states (V1)

- **Unknown** — exists in data; no map marker yet
- **Hinted** — player heard enough to know a region/type, not exact point
- **Marked** — visible on the world map
- **Expired / Resolved** — no longer interactable; residue may remain as Symptom or Legacy trace

A discovery clue may open another POI without forcing the player to travel immediately. That is a valid Attention / Commitment hook.

# Active Window

Every POI has a lifetime in **player world-time** only.

Examples of duration classes:

- **Brief** — hours in-world; urgent street events
- **Short** — about a day
- **Standard** — several days
- **Long** — a week or more
- **Persistent** — rare; still finite, but long enough to feel stable

Rules:

1. The player must be able to understand that time matters.
2. Exact second-timers are not required, but the urgency class must be readable.
3. Sources of readability may include rumour wording, notice-board deadlines, NPC phrases, map marker urgency, or diary notes.
4. If an event disappears because the player was late, the world should show what happened, not silently delete the opportunity.

Active Windows create Commitment pressure: world-time is a scarce stock.

# Unattended Resolution

If the player never participates:

1. choose an outcome from the POI's designed unattended table;
2. weight the table by Faction Influence and local state;
3. apply Influence shifts, Symptoms, and path open/close effects;
4. expose enough aftermath that a curious player can reconstruct the story later.

This preserves:

- living world without the player;
- Failure Value / learning after absence;
- solo-friendly simulation budget.

# POI Types (V1)

Keep the first set small.

| Type | Role | Typical axes |
|------|------|--------------|
| Street Conflict | public Allegiance fork | Allegiance + Approach |
| Trace / Clue | discovery that opens another POI | Attention + Approach |
| Cache / Hidden Place | exploration reward with risk | Attention + Commitment |
| Bargain Site | dangerous power / church / entity offer | Bargain + Commitment |
| Operation Echo | residue or interrupt chance for a faction Operation | Attention + Allegiance |

Combat may appear inside a type. Combat is not a POI type by itself.

# Relationship to Other Systems

```text
World Seed + World Engine
        ↓
create / schedule POIs
        ↓
player discovers or stumbles into POI
        ↓
Decision Node activates on area enter
        ↓
participation or unattended resolution
        ↓
Influence / Symptoms / new POIs
```

- [02 World Laws](02%20World%20Laws.md) defines why the world changes.
- [03 Decision Engine](03%20Decision%20Engine.md) defines what makes the choice interesting.
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md) is the choice card.
- This document defines how that card is placed, timed, discovered, and cheaply simulated.

# POI Fields to Add to a Decision Node Card

When a Decision Node is delivered as a POI, also fill:

```text
### POI packaging
POI type:
Map presentation: Unknown / Hinted / Marked
Discovery sources:
Interaction radius note:
Active Window class: Brief / Short / Standard / Long / Persistent
How the player learns urgency:
Unattended resolution summary:
Aftermath Symptom if missed:
Follow-up POI chance:
```

# Incremental Scope

## V1

- Handful of POI types.
- Seed places a limited number of active POIs.
- Map markers for Marked state.
- Active Window classes with readable urgency.
- Unattended resolution tables with Influence weighting.
- Decision Node loads only on area enter.
- Simple clue → unlock another POI chains.

## V2

- More discovery channels and Hinted regional info.
- POIs that transform instead of only expiring.
- Stronger links between Operations and Operation Echo POIs.

## V3

- Denser emergent chains across districts.
- Cross-life Legacy traces that point to expired POI aftermaths.
- Still no full always-on scene simulation.

# Design Validation

A POI is valid only if:

1. it can stay dormant without breaking the fantasy;
2. its Active Window is somehow readable;
3. missing it still teaches or changes the world;
4. participating uses a real Decision Node when the moment is important;
5. it remains feasible for one developer.

# Worked Example — Street Arrest of the Apothecary

This is a complete V1-style card: Decision Node + POI packaging.

```text
## Decision Node: Street Arrest of the Apothecary

Status: Ready as design example
World Seed sensitivity: Medium
(seed changes street, names, exact rewards, quiet faction sponsorship, and window length within Short class)

### POI packaging
POI type: Street Conflict
Map presentation: starts Unknown or Hinted; becomes Marked after notice board / bartender / neighbour rumour
Discovery sources:
- notice board: "Public disturbance near the apothecary row"
- bartender: "Guards are grabbing the mixture-seller before night market"
- stumbling into the street during the Active Window
Interaction radius note: the street segment + shop front; entering starts the node
Active Window class: Short (about one in-world day from posting / spawn)
How the player learns urgency:
- notice text says "before night market"
- bartender says it will be "done by tomorrow either way"
- if marked, map uses urgent Short-class presentation
Unattended resolution summary:
- weighted by local Guard Influence vs Soft-Market / underworld Influence
- common miss results: apothecary taken, shop sealed; or apothecary escapes and shop is looted
Aftermath Symptom if missed:
- closed shop boards / cheaper or rarer medicine / denser patrols / new rumour about "banned mixtures"
Follow-up POI chance:
- Trace / Clue POI to the seized drawers, escape route, or buyer of the mixtures

### 1. Situation
At dusk, guards try to arrest an apothecary accused of selling banned mixtures.
Neighbours argue. The shop door is half-open. The player arrives while the outcome is still unsettled.

### 2. Active Decision Axes
Primary: Allegiance
Secondary: Approach
Optional third: Attention (walk away / come back later inside the window)

### 3. What the player knows
Observable facts:
- arrest is happening now;
- both sides want help or non-interference;
- the shop has medicines and locked drawers;
- the Active Window will not last past the night market / next day.

Symptoms / rumours / clues:
- medicines recently got expensive;
- banned-rite ingredients are in street talk;
- night patrols grew harsher.

Relevant Player Knowledge that can help:
- reading whether this guard office or this apothecary usually fronts for a known faction;
- knowing eavesdropping may beat a blind public choice.

### 4. What stays hidden
Hidden Influence or affiliations:
- small Influence shift toward the helped side;
- exact sponsors remain hidden unless the player understands the tells.

Hidden delayed consequences:
- later mentor access, recipe clue, or patrol pattern;
- a follow-up POI may appear only after this one resolves.

Information discoverable with effort:
- stalk/eavesdrop the captain and a messenger;
- persuade a neighbour;
- inspect drawers if access is gained.

### 5. Options

Option name: Defend the apothecary
Axes used: Allegiance + Approach
Method / Approach: confront, persuade, or distract guards
Immediate cost or risk: conflict with guards; injury, fine, or pursuit
Likely Readable Consequence: apothecary stays free and later offers aid
What may open: medicines / buffs / ingredient clue / quiet ally network
What may close: easy help from this guard office; some legal mentors
Failure result: shop damaged, arrest half-succeeds, but player learns who actually wanted the drawers

Option name: Help the guards seize him
Axes used: Allegiance + Approach
Method / Approach: assist arrest, block escape, or testify
Immediate cost or risk: local blame from neighbours who trust the apothecary
Likely Readable Consequence: guards control the shop and favour the player
What may open: weapon/skill mentor, patrol tips, temporary legal cover
What may close: apothecary aid path; soft access to banned-mixture knowledge
Failure result: apothecary escapes with a secret; player learns the guards' real target was the stock

Option name: Stalk and eavesdrop first
Axes used: Attention + Approach
Method / Approach: stealth observation before committing
Immediate cost or risk: spends world-time inside a Short window; scene may end without a clean hero moment
Likely Readable Consequence: hears who ordered the arrest or where contraband is going
What may open: better later option, barter leverage, Yield Threshold advantage
What may close: the simple public save / assist timing
Failure result: spotted, trusted less by both sides, still gains one partial Influence tell

Option name: Leave before it ends
Axes used: Attention + Commitment
Method / Approach: non-intervention, maybe return later if window remains
Immediate cost or risk: may miss participation entirely
Likely Readable Consequence: if window expires, unattended resolution posts Aftermath Symptoms next day
What may open: aftermath Trace POI; safer observation of residue
What may close: both immediate mentor/medicine offers
Failure result: not a soft reload fail; the world chooses and teaches the cost of delay

### 6. World residue
Faction Influence shift: small hidden shift to the supported side, or to the unattended winner
Symptom changes: shop open/closed, medicine prices, patrol density, rumour tone
Path open / path close: mentor path and mixture-clue path pressure each other
Legacy potential: freed ally, sealed shop, or ruined storefront can persist after death

### 7. Curiosity check
Hypotheses:
- "If he is guilty, why do neighbours defend him?"
- "If guards are righteous, why do they want the drawers more than the man?"
Remaining question:
- "Who benefits from controlling this shop?"

### 8. Validation
Creates curiosity? Yes
Teaches something about the world? Yes
Every gained power has a price? Yes
Multiple viable options? Yes
Failure valuable? Yes
World different afterward? Yes
Lazy / timed POI valid? Yes
```

# Documentation Review

- **Vision / Pillars:** Compatible. Curiosity, timed commitment, living residue, and incomplete playthroughs are reinforced.
- **World Laws:** Compatible if World Engine mainly schedules POIs/Operations rather than full scenes. Unattended resolution keeps Influence moving without the player.
- **Decision Engine:** Compatible. POIs are delivery vehicles for Decision Nodes, not a replacement for axes.
- **Solo scope:** Compatible. Dormant POIs are the V1 performance strategy.
- **Glossary / Open Questions:** Updated for POI terms; exact UI urgency vocabulary and duration numbers remain open.

# Related Documents

- [02 World Laws](02%20World%20Laws.md)
- [03 Decision Engine](03%20Decision%20Engine.md)
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
