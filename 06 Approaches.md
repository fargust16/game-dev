# 06 Approaches

Version: 0.1

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md)

Referenced By:
[08 Confront Constraints](08%20Confront%20Constraints.md), Future combat, dialogue, stealth, barter, and encounter implementation.

---

# Purpose

This document defines the player's **Approaches**: the distinct methods for acting when a Decision Node offers more than one viable way forward.

It answers:

> How can the player act, without making combat the default best answer?

This is not a combat system, dialogue system, or stealth system.
Those later documents must instantiate these Approaches.

# Relationship to Decision Engine

In [03 Decision Engine](03%20Decision%20Engine.md), **Approach** is one Decision Axis:

> Choose how to act when multiple methods remain viable.

This document names the V1 method set used by that axis.

An important Decision Node should usually offer at least two Approaches, or one Approach plus Withdraw / delay, unless a rare tutorial constraint applies.

# Design Rules

1. Combat is one Approach, never the universal best payoff.
2. Every Approach has a cost, risk, or opportunity cost.
3. Failure with an Approach must still be valuable when the node is important.
4. Player Knowledge unlocks better judgment about which Approach fits, not a flat success-rate cheat.
5. Pathways bias Approaches; they must not delete the others.
6. Keep the V1 set small enough for one developer.

# V1 Approach Set

V1 uses six Approaches.

| Approach | Plain meaning | Typical tools |
|----------|---------------|---------------|
| **Confront** | Force the issue openly | fight, shove, break, open defiance |
| **Convince** | Change minds without fear as the main lever | persuasion, appeal, truth, shared interest |
| **Threaten** | Compel through fear or pressure | intimidation, ultimatum, show of force without full battle |
| **Exchange** | Trade value for value | barter, favour, service, artifact, information deal |
| **Veil** | Act without full open identity | stalk, eavesdrop, sneak, misdirection, unnoticed entry |
| **Withdraw** | Leave, delay, or refuse engagement | walk away, wait out a window, decline a Bargain |

These names are system terms. Soft fiction wording in UI may vary later, but documents should keep one term per Approach.

## Confront

Open force or open physical resolution.

- Strong when time is short and secrecy is already dead.
- Costly in wounds, noise, legal heat, and burned relationships.
- Must not be the only Approach that yields real Information or real rewards.

## Convince

Social resolution through consent, trust, or shared logic.

- Strong when the player has clues, reputation, or a readable shared interest.
- Can fail without combat and still reveal motives.
- Distinct from Threaten: fear is not the main lever.

## Threaten

Social or physical pressure through fear.

- Strong for Open Path conduct and desperate urgency.
- Risks lasting enmity, false compliance, or escalation into Confront.
- Yield Threshold moments often begin here or in Confront.

## Exchange

Resolution through trade.

- Strong for Bound Path conduct and information markets.
- Includes trading items, services, secrets, protection, or future debts.
- A deal may itself be a Bargain if the price is powered or spiritual.

## Veil

Resolution through limited visibility.

- Strong for Veiled Path conduct and Active Window scouting.
- Includes stalking, eavesdropping, unnoticed movement, and hidden observation.
- Failure is often "spotted," which can collapse into Threaten or Confront.

## Withdraw

Refusal or delay.

- Always meaningful because POIs and World Engine continue.
- May preserve stocks and open aftermath Traces.
- May close the best live rewards and live trails.
- Required whenever Attention is truly active: ignoring is a real option.

# Information Gathering

Many nodes are about learning place, time, motive, or route before a larger fork.

Valid Information Approaches in V1:

- Convince an informant;
- Threaten an informant;
- Exchange for a tip;
- Veil to overhear or follow;
- Confront if the player has a method to extract information in or after conflict;
- Withdraw and seek another source.

Rules from World Laws still apply:

- combat does not guarantee information;
- an NPC may hit a Yield Threshold and offer information for survival;
- Player Knowledge improves which method is wise, not which method is magically free.

# Pathway Bias

From [05 Progression](05%20Progression.md):

| Pathway | Natural Approaches | Dissonant Approaches |
|---------|--------------------|----------------------|
| Open Path | Confront, Threaten, open Exchange | prolonged Veil as the main identity |
| Veiled Path | Veil, quiet Exchange, selective Convince | constant public Confront |
| Bound Path | Exchange, Convince/Threaten via patrons, mediated deals | pure Pathless self-reliance with no debts |

Bias means:

- Conduct Progress prefers some Approaches;
- some tools/mentors appear more often;
- dissonant Approaches remain possible with sharper cost or slower Pathway progress.

Bias must never mean "other Approaches are fake options."

# Composition With Other Axes

Approaches rarely appear alone.

Examples:

- Allegiance + Confront: fight for one side
- Allegiance + Veil: follow one side's victims to learn the sponsor
- Commitment + Exchange: spend a rare artifact for a key tip
- Bargain + Withdraw: refuse External Power
- Attention + Veil: scout a POI before committing
- Attention + Withdraw: leave a humming cellar for another day

When filling a Decision Node card, name both the axes and the Approaches used by each option.

# What This Document Does Not Design

Deferred to later system docs:

- hit chances, stances, cooldowns, combat AI;
- full dialogue trees and persuasion minigames;
- stealth detection meters;
- economy price formulas for Exchange;
- rite choreography and spell animation.

Those systems may add depth only if they preserve the six Approaches as real choices.

# Solo Scope

## V1

- Six Approaches named and used in Decision Node / POI design.
- Important nodes offer multiple Approaches or Approach + Withdraw.
- Information can be gained without Confront.
- Pathway bias affects Conduct Progress and opportunity frequency.
- Simple success/fail/complication outcomes are enough; no deep minigames required.

## V2

- Soft support tools per Approach (skills, artifacts, mentors).
- Richer Yield Threshold and spotted-Veil complications.
- Light rite/Invoke support under Exchange or as an extension for Bound Path.

## V3

- Emergent Approach chains across districts.
- Deeper specialised systems under each Approach without deleting the set.
- Cross-life Player Knowledge about when each Approach collapses.

# Design Validation

An Approach option is valid only if:

1. it could reasonably succeed in the situation;
2. it has a distinct cost or risk from the alternatives;
3. failure still teaches or changes something when the node is important;
4. it does not make all other Approaches obsolete;
5. a solo developer can prototype it without a huge subsystem.

If Confront always pays best, redesign rewards and residue before writing more combat content.

# Worked Mapping — Existing Examples

## Street Arrest of the Apothecary

- Defend apothecary → Confront or Convince
- Help guards → Confront, Convince, or Threaten
- Scout first → Veil
- Leave → Withdraw
- Later tip purchase → Exchange

## Night Abductions on the Street

- Protect victims → Confront, Convince, or Threaten
- Follow the captors → Veil
- Fetch patrol → Convince / Exchange with authorities, possibly Threaten urgency
- Leave → Withdraw

# Documentation Review

- **Decision Engine:** Compatible. This document instantiates the Approach axis without replacing other axes.
- **Progression:** Compatible. Open / Veiled / Bound bias map cleanly onto the six methods.
- **World Laws / POI:** Compatible. Information Approaches and Withdraw support timed living-world play.
- **Vision / Pillars:** Compatible. Curiosity and multiple viable methods beat combat supremacy.
- **Glossary / Open Questions:** Updated; deep subsystem rules remain open.

# Related Documents

- [03 Decision Engine](03%20Decision%20Engine.md)
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)
- [05 Progression](05%20Progression.md)
- [02 World Laws](02%20World%20Laws.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
