# 10 Turn Action Economy

Version: 0.2

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [03 Decision Engine](03%20Decision%20Engine.md), [06 Approaches](06%20Approaches.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md)

Referenced By:
[10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), Future combat implementation, turn-based exploration control, and encounter tuning.

---

# Purpose

This document defines the minimal **Turn Action Economy** for V1 Turn-Based Mode.

It answers:

> What can a character do on one turn, in and out of Confront, without building a giant combat RPG on day one?

Reference feel: Baldur's Gate 3 and Fallout 1/2 decision clarity.
Not a feature-parity clone.

# Central Rules

1. One turn must present a readable choice, not a combo exam.
2. The same economy is used in Confront and in non-combat Turn-Based Mode.
3. Actions must support all six Approaches, not only Confront.
4. Break Away, Spare, and Yield responses are first-class actions where relevant.
5. Solo V1 prefers a small fixed structure over a deep Action Point spreadsheet.

# V1 Turn Structure

Each participant in Turn-Based Mode receives a turn in initiative order.

On your turn you may use:

1. **Move**
2. **Action**
3. **Bonus** (optional, short list only)

Order is flexible: Move before or after Action/Bonus, unless a specific action says otherwise.

You may skip any part.
You may End Turn early.

## Move

- Spend movement up to your current Move Speed across the **square grid**.
- Movement follows legal grid paths from [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md).
- Movement can be split around Action/Bonus unless an action locks you in place.
- Difficult terrain, injury, or Bargains may reduce Move Speed.
- Elevation changes require stairs/ladder/climb tags; risky traversal uses Environment Actions.

## Action

The main thing you do this turn.

V1 Action examples:

- Confront strike / shove / grapple attempt;
- Convince attempt;
- Threaten attempt;
- Exchange offer / hand over item as part of a deal;
- Veil attempt (hide, silent approach, pick pocket if later allowed);
- Interact with a significant object (door, body, lever, board);
- Environment Action (vault/jump, climb, window entry, mantle, breach);
- **Break Away** attempt;
- **Spare** / accept or offer Yield terms when available;
- Aim/prepare a risky setup that consumes the Action;
- Use a slow item or formula tool that is not Bonus-eligible.

## Bonus

A smaller extra act. V1 keeps this list short on purpose.

V1 Bonus examples:

- quick item use (salve, throw small object, drink prepared vial);
- draw/swap a ready item;
- brief interact (open unlocked door beside you, pick up adjacent object);
- short dash: convert Bonus into a little extra Move (optional V1 rule).

If an act is decisive or contested, it is an Action, not a Bonus.

## What V1 does not include

Not required for V1:

- full reaction economy every turn;
- opportunity-attack webs;
- multiclass feat stacks;
- large Action Point shopping menus;
- separate resource for every verb.

A tiny Reaction exception may exist later for Yield response timing. Default V1: resolve Yield/Spare on the relevant turn as Actions.

# Initiative

When Turn-Based Mode starts:

1. build an initiative order for relevant actors;
2. the player character and important NPCs/enemies take turns;
3. irrelevant background actors may be frozen or simplified.

V1 initiative may be very simple (for example a small Dexterity/Perception-style ordering plus light randomness). Exact formula is open tuning.

# Entering and Leaving Turn-Based Mode

## Enter Confront

When Confront locks in, the current location enters Turn-Based Mode automatically.

## Enter outside combat

The player may manually enter Turn-Based Mode to control tense moments:

- careful Veil approach;
- positioning before a conversation;
- timing an Exchange in a crowded room;
- approaching a Physical Trace under threat.

## Leave outside combat

The player may leave Turn-Based Mode when not forced by Confront, returning to normal exploration pacing.

## Exploration pacing (locked for V1 feel)

Outside Turn-Based Mode the avatar moves in **Free Exploration** with two control schemes:

- **Mouse:** click sets a grid A* path (around blockers); Environment Actions on that path are used automatically; camera stays free (pan/zoom/orbit).
- **WASD:** direct walk; camera follows the avatar; Environment Actions require manual **E** (with fail chance).

The world remains **grid-authored** — pathfinding and traversal truth are cells, not physics parkour.

Entering Turn-Based Mode keeps the same path rules for click-move; keyboard steps are discrete cells. Full Move + Action + Bonus economy comes with Confront. S0 toggles TB with **T**.

## Leave Confront

Turn-Based Mode ends when:

- all hostile Confrontants are dead, fled, spared under resolved terms, or otherwise no longer in Confront;
- or the player successfully Breaks Away and pursuit ends;
- or a scripted scene resolution says the conflict is over.

# Break Away, Spare, Yield

These are part of the economy, not cutscenes outside it.

| Outcome | Economy handling |
|---------|------------------|
| Break Away | Action; success ends or relocates Confront with cost/risk |
| Offer Spare | Action when the player controls the finish |
| Accept Yield | Action or response when an NPC offers terms |
| Force Yield attempt | usually Threaten/Confront Action that may create a Yield Threshold |

Exact success chances are later tuning. The existence of these actions is binding.

# Non-Combat Turns

In non-combat Turn-Based Mode:

- the same Move / Action / Bonus structure applies;
- NPCs do not all become combatants;
- some NPCs may still take turns if they are actively watching, escorting, or chasing;
- time in the world still matters; using many turns in a Brief Active Window is a Commitment cost.

This preserves deliberate control without turning every shop visit into a battle.

# Approach Mapping

| Approach | Typical Action verbs |
|----------|----------------------|
| Confront | strike, shove, grapple, break object |
| Convince | persuade, appeal, present evidence |
| Threaten | intimidate, ultimatum, brandish |
| Exchange | offer deal, pay, hand over item |
| Veil | hide, sneak closer, eavesdrop positioning |
| Withdraw | Break Away in Confront; leave/disengage outside Confront |

# Injury and Pressure Soft Rules

V1 only needs lightweight states, for example:

- Healthy;
- Hurt (minor Action/Move pressure);
- Critical (severe pressure);
- Downed / Dead.

Do not invent a medical simulator before the economy feels good.

Bargains, Pathway stage, and equipment may modify Move, Action options, or Bonus availability later without changing this base structure.

# Solo Scope

## V1

- Move + Action + short Bonus list.
- Initiative order.
- In-location Turn-Based Mode for Confront and optional non-combat control.
- Break Away / Spare / Yield as Actions.
- Simple injury pressure states.
- No deep reaction layer.

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
- forcing Turn-Based Mode for ordinary walking across empty streets;
- using the economy to recreate XP grind through many tiny fights;
- delaying all Decision Engine choices until after HP reaches zero.

# Design Validation

A V1 turn is good if a new player can ask:

1. Where can I move?
2. What is my main Action?
3. Do I need my Bonus now or later?
4. What happens to the world if I wait another turn?

If those questions are unclear, simplify further.

# Documentation Review

- **Confront Constraints:** Compatible. Turn-Based Mode now has a minimal action skeleton.
- **Approaches:** Compatible. Economy verbs map to all six methods.
- **Decision Engine:** Compatible. Turns host decisions; they do not replace axes.
- **POI / Information:** Compatible. Non-combat turns support careful discovery under time pressure.
- **Glossary / Open Questions:** Updated; exact move units, initiative formula, and Bonus dash rule remain open.

# Related Documents

- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [06 Approaches](06%20Approaches.md)
- [03 Decision Engine](03%20Decision%20Engine.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [90 Glossary](90%20Glossary.md)
- [99 Open Questions](99%20Open%20Questions.md)
