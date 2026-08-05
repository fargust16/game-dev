# 09 Information Sources

Version: 0.1

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md)

Referenced By:
Future dialogue, UI rumour, newspaper, notice-board, and discovery content.

---

# Purpose

This document defines how the player learns about the world, POIs, threats, and opportunities without progress bars or omniscient quest markers.

It answers:

> Where does information come from, and how does it create curiosity instead of a checklist?

Information Sources feed Attention, unlock or hint POIs, support Approaches, and make Player Knowledge useful across lives.

# Central Rules

1. Important information is usually partial.
2. Sources can be wrong, biased, late, or incomplete.
3. Better Approaches and Knowledge improve access to information; they do not delete uncertainty.
4. Learning about a POI is not the same as solving it.
5. Time matters: some tips expire with Active Windows.
6. V1 keeps a small set of reusable source types.

# What An Information Source Does

An Information Source may:

- create a **Hinted** POI;
- upgrade a POI to **Marked**;
- reveal urgency class / Active Window language;
- reveal a faction tell, route, motive fragment, or Aftermath Symptom;
- open a follow-up Trace POI;
- grant Formula discovery leads;
- create Soft Memory hooks about previous lives.

It should rarely hand over a perfect solution with no remaining decision.

# V1 Source Types

## 1. Notice Board

Public posted information.

- Good for: district disturbances, missing persons, official warnings, job-like Side Event hooks.
- Bias: often legal / guard / merchant framing.
- May be outdated relative to the true Active Window.
- Low social cost to read; may attract attention if the player interacts openly in hostile districts.

## 2. Newspaper / Broadsheet

Printed public narrative.

- Good for: broader district mood, Influence Symptoms, scandal, delayed Aftermath.
- Bias: owners, churches, or factions may slant the story.
- Strong for teaching the player to read Symptoms over time.
- Often weaker on exact map pins; stronger on Hinted regions and motive fragments.

## 3. Bartender / Publican

Social hub informant.

- Good for: tonight's Brief POIs, local urgency language, who is afraid of whom.
- Access may improve through Convince, Exchange, Threaten, or prior Allegiance.
- Can point to Marked POIs with clearer timing than a board.
- May refuse, lie, or sell half-truths.

## 4. Street Rumour

Ambient or NPC chatter.

- Good for: cheap Attention hooks and Soft Memory.
- Low reliability; high curiosity.
- Often creates Hinted state rather than Marked.
- Excellent for Death Gap aftermath and "collector" style pressure.

## 5. Faction Contact

Mentor, follower, officer, or handler.

- Good for: Main Goal details, Operation timing, faction-framed opportunities.
- Requires membership or a temporary Opening through Exchange / Convince / Threaten / Veil.
- Factionless characters must work harder here; this supports the Character Life penalty.
- Strong bias: information serves the faction's interests.

## 6. Witness / Involved NPC

Someone tied to a scene.

- Good for: local truth after or during a POI.
- Approaches matter: Convince, Threaten, Exchange, Veil eavesdrop, Confront + Yield Threshold.
- May become unavailable if killed, fled, or silenced during Unattended Resolution.

## 7. Physical Trace

Objects, marks, ledgers, blood, sealed doors, dropped tokens.

- Good for: Aftermath and Veil-oriented discovery.
- Often found by stumbling into a location or following a Hinted lead.
- Strong bridge into Trace POIs and Formula leads.
- Does not argue back, but can be misread.

## 8. Legacy Soft Memory

World remembrance of previous characters.

- Good for: dialogue options, opinion shifts, investigation of prior lives.
- Comes from [07 Character Life](07%20Character%20Life.md).
- Should open questions and contacts, not restore the old hero's power.

# Discovery Pipeline

```text
Source tip
   ↓
Unknown → Hinted → Marked
   ↓
Player enters POI area or window expires
   ↓
Decision Node / Unattended Resolution
   ↓
Aftermath Symptoms + possible new Source tips / Trace POIs
```

Rules:

- Not every tip must Mark the map.
- Hinted information can be enough to create meaningful Attention / Commitment pressure.
- A source may warn that time is short without giving the exact pin.
- Player Knowledge helps recognise which sources are usually biased or fertile.

# Reliability and Bias

Every V1 source type has a bias profile.

| Source | Typical bias |
|--------|--------------|
| Notice Board | official / commercial framing |
| Newspaper | owner or faction narrative |
| Bartender | local survival and profit |
| Street Rumour | fear, exaggeration, fragments |
| Faction Contact | faction goals |
| Witness | self-protection |
| Physical Trace | no intention, easy misread |
| Soft Memory | incomplete legend |

Bias is a feature. It teaches world reading.

Deliberate false tips are allowed if the player can eventually detect the pattern through Symptoms or contradiction. Opaque random lying with no learnable logic is invalid.

# Approaches to Extracting Information

From [06 Approaches](06%20Approaches.md):

- **Convince** — ask, appeal, share interest
- **Threaten** — pressure for a tip
- **Exchange** — pay with coin, favour, secret, service
- **Veil** — overhear, stalk to a meeting, read without permission
- **Confront** — only yields info with a method, document, or Yield Threshold
- **Withdraw** — leave and try another source before the window dies

Turn-Based Mode outside combat may help set up Veil or careful Exchange in tense rooms without forcing Confront.

# Urgency Language

Because POIs have Active Windows, sources must often communicate time.

V1 methods:

- explicit phrases ("tonight after the eleventh bell");
- board deadlines;
- newspaper "still unresolved as of this morning";
- bartender tone that marks Brief vs Long;
- map urgency after Marked upgrade.

Exact UI chrome remains open. The design rule is: the player should be able to understand that waiting has a cost.

# Relation to Other Systems

| System | Role of Information Sources |
|--------|-----------------------------|
| Points of Interest | Main discovery and urgency channel |
| World Laws | Translate Influence into readable tips and Symptoms |
| Decision Engine | Tips create Attention and Partial Uncertainty |
| Progression | Formula leads and church/entity whispers |
| Character Life | Factionless players rely more on public sources at first |
| Confront | Violence can destroy Witness sources |

# Solo Scope

## V1

- All eight source types exist as reusable patterns.
- Notice Board, Bartender, Street Rumour, and Physical Trace are the most common.
- Newspaper / Faction Contact / Witness / Soft Memory appear in smaller numbers.
- Tips can Hint or Mark POIs and communicate urgency.
- No omniscient quest journal that replaces world reading.

## V2

- Source rivalries and planted disinformation with clearer tell patterns.
- Subscription-like newspaper arcs tied to Influence stages.
- Richer Faction Contact desks after Faction Entry.

## V3

- Cross-district information markets.
- Soft Memory legends that rewrite public sources over multiple lives.
- Still no perfect truth dashboard.

# Anti-Patterns

- quest log that spoils the best Approach;
- every bartender always tells the whole truth for free;
- map markers for all content from minute one;
- information that only drops from Confront;
- tips that never expire while their POIs do;
- fake rumours with no readable way to ever detect the pattern.

# Design Validation

A new tip is good if:

1. it creates "what if I check this?";
2. it leaves at least one uncertainty;
3. it has a plausible bias;
4. acting late can change its value;
5. a curious player can use it without being told the optimal build path.

# Documentation Review

- **POI system:** Compatible. Sources are the discovery layer for timed lazy POIs.
- **Decision Engine:** Compatible. Information narrows, does not erase, Partial Uncertainty.
- **Approaches / Confront:** Compatible. Multiple extraction methods; violence is not the info king.
- **Character Life / Progression:** Compatible. Factionless hardship and Formula leads both fit.
- **Glossary / Open Questions:** Updated; exact UI and spawn density remain open.

# Related Documents

- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [02 World Laws](02%20World%20Laws.md)
- [06 Approaches](06%20Approaches.md)
- [07 Character Life](07%20Character%20Life.md)
- [03 Decision Engine](03%20Decision%20Engine.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
