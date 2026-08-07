# 10 Turn Action Economy

Version: 0.3

Status: Draft

Last Updated: 2026-08-07

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [03 Decision Engine](03%20Decision%20Engine.md), [06 Approaches](06%20Approaches.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
[10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), Future combat implementation and encounter tuning.

---

# Purpose

This document defines the minimal **Turn Action Economy** for V1 Turn-Based Mode on **Combat Arenas**.

It answers:

> What can a character do on one turn during Confront on an arena, without building a giant combat RPG on day one?

Reference feel: Baldur's Gate 3 and Fallout 1/2 decision clarity.
Not a feature-parity clone.

Exploration (overworld and interiors) is **free third-person** and does **not** use this economy ([16 Architecture Pivot](16%20Architecture%20Pivot.md)).

# Central Rules

1. One turn must present a readable choice, not a combo exam.
2. This economy runs **only on Combat Arena scenes** during Confront (and any future arena-only puzzle modes, if added later).
3. Actions must support all six Approaches where relevant on the arena, not only Confront strikes.
4. Break Away, Spare, and Yield responses are first-class actions where relevant.
5. Solo V1 prefers a small fixed structure over a deep Action Point spreadsheet.
6. There is **no** voluntary Turn-Based Mode during free exploration.

# V1 Turn Structure

Each participant in arena Turn-Based Mode receives a turn in initiative order.

On your turn you may use:

1. **Move**
2. **Action**
3. **Bonus** (optional, short list only)

Order is flexible: Move before or after Action/Bonus, unless a specific action says otherwise.

You may skip any part.
You may End Turn early.

## Move

- Spend movement up to your current Move Speed across the **arena square grid**.
- Movement follows legal grid paths from [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md).
- Movement can be split around Action/Bonus unless an action locks you in place.
- Difficult terrain, injury, or Bargains may reduce Move Speed.
- Elevation changes require stairs/ladder/climb tags; risky traversal uses Environment Actions.

## Action

The main thing you do this turn.

V1 Action examples:

- Confront strike / shove / grapple attempt;
- Convince attempt (when the arena fiction allows talking);
- Threaten attempt;
- Exchange offer / hand over item as part of a deal;
- Veil attempt (hide, reposition for cover);
- Interact with a significant arena object;
- Environment Action (vault/jump, climb, mantle, breach);
- **Break Away** attempt;
- **Spare** / accept or offer Yield terms when available;
- Aim/prepare a risky setup that consumes the Action;
- Use a slow item or formula tool that is not Bonus-eligible.

## Bonus

A smaller extra act. V1 keeps this list short on purpose.

V1 Bonus examples:

- quick item use (salve, throw small object, drink prepared vial);
- draw/swap a ready item;
- brief interact (open unlocked adjacent prop, pick up adjacent object);
- short dash: convert Bonus into a little extra Move (optional V1 rule).

If an act is decisive or contested, it is an Action, not a Bonus.

## What V1 does not include

Not required for V1:

- full reaction economy every turn;
- opportunity-attack webs;
- multiclass feat stacks;
- large Action Point shopping menus;
- separate resource for every verb;
- Turn-Based Mode on overworld or interior exploration scenes.

A tiny Reaction exception may exist later for Yield response timing. Default V1: resolve Yield/Spare on the relevant turn as Actions.

# Initiative

When arena Turn-Based Mode starts:

1. build an initiative order for relevant actors;
2. the player character and important NPCs/enemies take turns;
3. irrelevant background actors may be frozen or simplified.

V1 initiative may be very simple (for example a small Dexterity/Perception-style ordering plus light randomness). Exact formula is open tuning.

# Entering and Leaving Turn-Based Mode

## Enter Confront → Arena

When Confront locks in (from overworld or an interior):

1. capture the source **Theme Pack** id;
2. transition to a Combat Arena scene;
3. start Turn-Based Mode with Move + Action + Bonus.

## Exploration pacing (locked)

Outside arenas the avatar uses **Free Exploration**:

- third-person locomotion (Genshin-like feel target, not feature parity);
- follow / orbit camera;
- diegetic interacts (talk, open door to interior scene, examine) — not grid Environment Actions.

No manual Turn-Based Mode toggle during exploration.

## Leave Confront

Arena Turn-Based Mode ends when:

- all hostile Confrontants are dead, fled, spared under resolved terms, or otherwise no longer in Confront;
- or the player successfully Breaks Away and pursuit ends;
- or a scripted resolution says the conflict is over.

Then return to the source overworld or interior scene and apply residue ([08 Confront Constraints](08%20Confront%20Constraints.md)).

# Break Away, Spare, Yield

These are part of the economy, not cutscenes outside it.

| Outcome | Economy handling |
|---------|------------------|
| Break Away | Action; success ends Confront and returns to source with cost/risk |
| Offer Spare | Action when the player controls the finish |
| Accept Yield | Action or response when an NPC offers terms |
| Force Yield attempt | usually Threaten/Confront Action that may create a Yield Threshold |

Exact success chances are later tuning. The existence of these actions is binding.

# Approach Mapping

| Approach | Typical Action verbs on arena |
|----------|-------------------------------|
| Confront | strike, shove, grapple, break object |
| Convince | persuade, appeal, present evidence (when allowed) |
| Threaten | intimidate, ultimatum, brandish |
| Exchange | offer deal, pay, hand over item |
| Veil | hide, take cover, reposition unseen |
| Withdraw | Break Away |

Veil / Convince / Exchange during free exploration use continuous-space interacts and Decision Nodes — not this turn economy.

# Injury and Pressure Soft Rules

V1 only needs lightweight states, for example:

- Healthy;
- Hurt (minor Action/Move pressure);
- Critical (severe pressure);
- Downed / Dead.

Do not invent a medical simulator before the economy feels good.

# Solo Scope

## V1

- Move + Action + short Bonus list **on arenas only**.
- Initiative order.
- Confront → thematic arena → return.
- Break Away / Spare / Yield as Actions.
- Simple injury pressure states.
- No deep reaction layer.
- No exploration TB.

## V2

- Small reaction set if needed.
- More Bonus identities by Pathway.
- Cleaner chase/Break Away rules.

## V3

- Richer tactical toys only if the six Approaches remain equally authored.
- Still reject Action Point bloat that makes Confront the only deep system.

# Anti-Patterns

- giving Confront five interesting verbs and every other Approach only one weak button;
- making Bonus stronger than Action;
- forcing Turn-Based Mode for ordinary overworld or interior walking;
- using the economy to recreate XP grind through many tiny fights;
- delaying all Decision Engine choices until after HP reaches zero;
- resolving ordinary Confront in the exploration scene instead of an arena.

# Design Validation

A V1 arena turn is good if a new player can ask:

1. Where can I move?
2. What is my main Action?
3. Do I need my Bonus now or later?
4. What happens to the world if I wait another turn?

If those questions are unclear, simplify further.

# Documentation Review

- **Architecture Pivot:** Compatible. Economy scoped to arenas; exploration is free 3P.
- **Confront Constraints:** Compatible. Arena presentation + residue / Break Away / Yield.
- **Approaches:** Compatible. Arena verbs map to Approaches; exploration Approaches stay diegetic.
- **Glossary / Open Questions:** Updated; exact move units, initiative formula, and Bonus dash rule remain open.

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [06 Approaches](06%20Approaches.md)
- [03 Decision Engine](03%20Decision%20Engine.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
