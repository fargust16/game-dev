# 03 Decision Engine

Version: 0.2

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md)

Referenced By:
[03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), Future quest, exploration, combat, progression, faction, magic, and economy design.

---

# Purpose

This document defines the player's **Decision Engine**: the systemic rules that make every important choice interesting.

It is not lore, magic, factions, combat, or quest design. Those systems must later instantiate this engine.

The single question this document answers:

> What makes every important player decision interesting?

# Design Goal

An important decision is interesting when the player:

1. sees more than one viable path;
2. lacks complete certainty, but has enough readable context to reason;
3. accepts a meaningful cost, risk, or opportunity cost;
4. expects the world to change because of the choice;
5. expects to learn something even if the outcome hurts.

If a choice fails these tests, it is not an important decision — it is either noise or a fake choice.

# Decision Axes

The game uses five minimal decision categories. Content systems may combine them, but should not invent parallel category trees for the same concept.

## 1. Attention

**Engage the unknown, or defer/ignore it.**

Examples of instantiation, not locked content:

- investigate a closed door or leave;
- follow a rumour or continue the current path;
- interrupt a ritual or observe from afar.

Attention creates curiosity pressure and opportunity cost. Ignoring something is a real decision because the world continues and the opportunity may change or vanish.

## 2. Approach

**Choose how to act when multiple methods remain viable.**

Examples of instantiation:

- fight, negotiate, infiltrate, barter, withdraw;
- stalk and eavesdrop when stealth methods, skills, or artifacts allow it;
- spare an NPC at a Yield Threshold in exchange for information;
- solve a problem through force, knowledge, alliance, or avoidance.

Approach forbids a single default answer. Combat is one Approach among several and must not be the universally best one.

## 3. Allegiance

**Choose whose interests take priority when interests conflict.**

Examples of instantiation:

- help one faction, another, both partially, or neither;
- protect an NPC at the expense of a settlement goal;
- remain neutral and accept the cost of neutrality.

Allegiance includes non-commitment. Neutrality is never free: the world still moves, and other actors fill the vacuum.

## 4. Commitment

**Spend or preserve scarce stocks.**

Scarce stocks include resources, trust, safe routes, time-of-world, legacy caches, ritual components, and one-shot opportunities.

Commitment asks whether the player burns something valuable now for a clearer or stronger effect, or conserves it while accepting weaker immediate control.

## 5. Bargain

**Accept a Price of Power, or refuse the advantage.**

Examples of instantiation:

- use a dangerous artifact or leave it sealed;
- learn a costly rite or walk away;
- take a wound, debt, curse, obligation, or exposure in exchange for capability.

Bargain is the decision surface for "every power has a price." Refusing power must remain a valid and sometimes wiser path.

## Composition Rule

An important decision node activates at least two axes.

Typical compositions:

- Attention + Approach
- Approach + Allegiance
- Commitment + Bargain
- Allegiance + Commitment
- Attention + Bargain

A node that activates only one axis is usually too thin to carry long-term interest.

# Teaching Order

The five axes are all accepted and permanent for this Draft.

V1 teaches them in this order:

1. **Attention** — primary fantasy lesson: notice the unknown and decide whether to risk engaging it.
2. **Approach** — immediate companion lesson: if you engage, choose how.
3. **Bargain** — second major lesson: external or dangerous power always carries a readable, often delayed price.

Allegiance and Commitment appear early through Side Events and scarce stocks, but the vertical slice should make Attention + Approach unmistakable first.

This order supports experimentation even when a Character Life ends. A new character in the same world, or a new world, can reuse Knowledge gained from failed Attention experiments.

# Side Events as Decision Nodes

Side Events from [02 World Laws](02%20World%20Laws.md) are standard Allegiance nodes:

- each major fork offers useful but different outcomes;
- Faction Influence shifts a little toward the supported side;
- the Influence shift stays hidden unless the player understands relevant affiliations;
- the choice may open some paths and close others;
- pure free bonuses without world residue are invalid.

# Starting Faction Context

A character begins with a Starting Faction and may later defect or realign. That starting bond does not remove Allegiance decisions; it frames them. Betrayal, neutrality, and aid to rivals remain meaningful because access, Symptoms, and open paths change with Allegiance.

World Difficulty changes opposing Faction Influence pressure at creation. It raises challenge; it does not invent a single correct strategy.

# Information Before Choice

## What the player has

Before an important decision, the player should have:

- local facts they can observe;
- Symptoms that hint at wider world pressure;
- incomplete history: rumours, traces, records, NPC motives;
- remembered Knowledge from earlier lives or earlier moments in this life;
- a readable sense of immediate stakes.

## What remains intentionally hidden

- exact Faction Influence values;
- exact Balance state;
- full outcome tables;
- some delayed consequence chains;
- other actors' private plans until revealed by Symptoms or events.

Hidden information exists to preserve reasoning, not to punish the player with unknowable randomness.

## Partial Uncertainty

The Decision Engine uses **Partial Uncertainty**:

- investigation and Knowledge can narrow the range of likely outcomes;
- they must not collapse the decision into a solved equation;
- residual uncertainty must come from living actors, incomplete information, and accepted costs — not from opaque dice the player cannot learn.

Knowledge reduces uncertainty without eliminating tension.

# Tension

Tension exists when every viable option harms or forgoes something the player values.

Sources of tension:

1. **Competing goods** — safety vs discovery, loyalty vs freedom, power vs future options.
2. **Opportunity cost** — choosing one path closes or worsens another.
3. **Moving world** — hesitation is itself a Commitment of world-time; the World Engine does not pause.
4. **Price of Power** — stronger options carry sharper costs.
5. **Incomplete prediction** — the player can reason well and still be surprised by a readable consequence.

## Why there is never an obviously correct choice

An obviously correct choice means one option dominates on every valued axis. The Decision Engine forbids dominance by design:

- every strong option has a price;
- every safe option sacrifices discovery, influence, or timing;
- every faction-aligned option creates enemies, debts, or closed paths;
- every delay lets the world rewrite the board.

## Avoiding a best strategy

There is no universal best strategy because:

- payoffs are context-dependent and change as Symptoms and Operations evolve;
- Knowledge opens new Approaches rather than a single optimal build;
- power always closes other options through its price;
- incomplete playthroughs make different values rational in different lives;
- the world can invalidate yesterday's winning pattern without invalidating the player's learning.

If playtesting finds a dominant loop, the fix is to restore cost, context dependence, or alternative Approaches — not to add more content volume.

# Curiosity

Curiosity is the desire to test a hypothesis about the world.

The Decision Engine consistently produces:

> "What happens if I try this?"

through four production rules:

1. **Readable gaps** — the player sees enough causality to form a hypothesis and enough absence to want a test.
2. **Multiple affordances** — objects, actors, and places invite more than one interaction.
3. **Hypothesis feedback** — experiments return information even when they fail.
4. **Transferable rules** — a discovered rule can be reused in a new context, rewarding understanding over memorised scripts.

Curiosity dies when outcomes are either fully opaque or fully solved. The engine stays between those extremes.

# Valuable Failure

Failure is valuable when it produces at least one of:

1. **Knowledge** — the player understands a rule, cost, motive, or weakness better;
2. **World Delta** — the world changes in a readable way;
3. **New Opportunity** — a path, ally, threat, or mystery appears that did not exist before.

## Forbidden failure

Failures that only waste time without Knowledge, World Delta, or New Opportunity are invalid design.

Death may end a Character Life, but Meta Loop and Legacy ensure the attempt still feeds Knowledge and world history.

# Interaction with the World Engine

```text
Player Decision
      ↓
Changes local state, stocks, allegiances, or available interventions
      ↓
Alters faction Operation eligibility, interruption chances, or resulting Symptoms
      ↓
World Engine attempts Operations on its schedule
      ↓
New Symptoms, pressures, and decision nodes appear
      ↓
Player faces a changed decision space
      ↺
```

## Player → World

Player choices can:

- enable, delay, or interrupt Operations;
- redistribute scarce stocks and access;
- shift Allegiance pressures between factions;
- leave Legacy that later actors exploit;
- accelerate, redirect, or temporarily slow paths toward World Collapse.

## World → Player

World Engine activity can:

- create new Attention hooks through Symptoms;
- remove Approaches by changing access or safety;
- force Allegiance conflicts into the open;
- raise or lower Commitment pressure;
- offer or threaten Bargains.

The feedback loop is the living-world expression of the Core Loop: action changes the world; the changed world changes the next meaningful choice.

# Consequences

Consequences have three independent dimensions.

| Dimension | Poles | Role |
|-----------|-------|------|
| Timing | Immediate / Delayed | Immediate teaches causality; Delayed creates dread and planning |
| Visibility | Visible / Hidden | Visible builds trust in the systems; Hidden rewards later discovery |
| Duration | Temporary / Permanent | Temporary allows recovery paths; Permanent gives weight |

## Mixing rules

1. Every important decision produces at least one **Readable Consequence** the player can eventually interpret.
2. Immediate + Visible consequences should be common enough that the world feels responsive.
3. Delayed consequences should be foreshadowed by Symptoms, debts, rumours, or incomplete traces — never by silent arbitrary twist.
4. Hidden consequences may exist, but their reveal must feel earned and causal.
5. Permanent consequences should be scarce and tied to Allegiance, Bargain, Legacy, or World Collapse pressure.
6. Temporary consequences create mid-term strategy without erasing the meaning of the choice.

## Anti-patterns

- reward screens that replace world change;
- invisible RNG with no learnable pattern;
- permanent punishment that teaches nothing;
- delayed twists that contradict established Symptoms.

# Knowledge and Future Decisions

Knowledge must not act primarily as a success-rate bonus.

## Player Knowledge

Player Knowledge is what survives between Character Lives and worlds. It changes future decisions by unlocking new reasoning:

- the player notices options that were always present but previously invisible;
- the player predicts likely World Engine pressure from Symptoms;
- the player estimates prices before accepting a Bargain;
- the player chooses Approach based on understood weaknesses and motives;
- the player judges when Attention is worth world-time;
- the player remembers important places, faction tells, and methods for extracting information;
- the player understands that some powers distort the character over time.

## Character Progression Direction

Character growth inside one life is not XP level-up. Future Progression design may use pathways, formulas, ingredients, staged advancement, churches, and otherworldly bargains. Those systems must:

- gate power behind discovery and preparation;
- require meaningful between-stage conduct or commitments where relevant;
- vary concrete formulas and ingredients by World Seed;
- treat every external power as a Bargain with readable, often delayed cost;
- widen decision space rather than crowning one best build.

Detailed pathway rules belong in a future Progression document. This Decision Engine only constrains them.

A weaker character guided by stronger Player Knowledge should regularly outperform a stronger character guided by ignorance. That is a success condition for this document.

# Solo-Developer Scope

## Decision Engine overall

### V1

- Five axes defined and used as a design checklist for every important node.
- Vertical slice emphasises Attention + Approach; Bargain appears as the second lesson.
- Each important encounter, quest, or combat fork declares which axes it activates.
- Side Events use Allegiance forks with hidden Influence residue and path open/close effects.
- Information Approaches include dialogue, intimidation, barter, combat extraction, stalking/eavesdropping, and Yield Threshold mercy.
- Every important outcome includes at least one Readable Consequence.
- Failure returns Knowledge or World Delta.
- Simple player → world hooks: interrupt one Operation, change one Allegiance pressure, spend one scarce stock.

### V2

- Decision nodes compose axes dynamically from world state.
- Delayed and Temporary consequences become common and foreshadowed.
- Knowledge unlocks alternate Approaches without numerical dominance.
- World Engine Symptoms regularly create fresh Attention and Allegiance nodes.

### V3

- Emergent multi-axis conflicts from overlapping Operations.
- Hidden consequences with discoverable reveal paths across Character Lives.
- Decision patterns transfer across worlds as player Knowledge while concrete answers stay new.

## Minimal content rule

Prefer one elegant decision node that cleanly activates two axes over many shallow binary choices.

# Design Validation

Every Decision Engine use must still pass the project filter:

1. Does it create curiosity?
2. Does it teach something about the world?
3. Does every gained power have a meaningful cost?

And the Decision Engine's own filter:

4. Does more than one option remain viable?
5. Does failure remain valuable?
6. Does the world become different afterward?

If any answer is No, redesign the node.

# Why This Document Must Exist First

| Later system | Why it depends on Decision Engine |
|--------------|-----------------------------------|
| Combat | Combat is an Approach, not the default solution. Without this document, combat design drifts toward optimal DPS loops. |
| Quests | Quests are structured sequences of Attention, Allegiance, Commitment, and Bargain. Without axes, quests become scripted reward corridors. |
| Progression | Progression must widen decision space through Knowledge and costly power, not flatten it through levels. |
| Magic | Magic is a family of Bargains and Approaches. Its rules must express Price of Power before spell lists exist. |
| Factions | Factions supply Allegiance conflicts and World Engine pressure. Their politics are meaningless without decision tension rules. |
| Economy | Economy is a Commitment surface: scarce stocks, prices as Symptoms, and trade as Approach. Numbers without decision pressure are grind. |

Systems designed before the Decision Engine tend to optimise content volume, combat fairness, or reward pacing — and quietly violate Curiosity over power.

# Documentation Review

- **Vision:** Compatible. Decisions centre curiosity, priced power, living consequences, and learning after loss.
- **Core Loop:** Compatible. The axes operationalise notice → context → action → consequence → understanding → new questions.
- **Meta Loop:** Compatible. Valuable failure and Knowledge preserve meaning across death and worlds; Legacy remains a Commitment/Allegiance residue.
- **Design Pillars:** Compatible. Especially curiosity, priced power, knowledge over levels, incomplete playthroughs, meaningful experiments, no perfect builds, finite worlds.
- **Player Experience:** Compatible. Supports curiosity, tense choice, agency, understandable surprise, loss with legacy, and return with understanding.
- **World Laws:** Compatible. Side Events, intervention Approaches, Starting Faction, Difficulty Pressure, and Influence-driven Balance now align with the Decision Axes.
- **Glossary:** Updated for teaching order implications and related world terms.
- **Open Questions:** Teaching axis and Knowledge role resolved; Hidden-consequence timing and Opportunity Cost vocabulary remain open.

# Related Documents

- [00.1 Core Loop](00.1%20Core%20Loop.md) — repeated decision cycle inside one life.
- [01 Player Experience](01%20Player%20Experience.md) — emotions the Decision Engine must produce.
- [02 World Laws](02%20World%20Laws.md) — world-side pressure that creates and answers decisions.
- [90 Glossary](90%20Glossary.md) — shared terminology.
- [99 Open Questions](99%20Open%20Questions.md) — unresolved decisions that block later systems.
