# 02 World Laws

Version: 0.1

Status: Draft

Last Updated: 2026-08-05

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md)

Referenced By:
Future world, faction, consequence, and collapse-system design.

---

# Purpose

This document defines the systemic laws governing the world itself, independent of lore. It is the design basis for world-state changes, faction behaviour, readable consequences, and world collapse. Future faction, settlement, economy, and consequence systems must depend on this document.

The document deliberately defines a lightweight, expandable model. It does not prescribe fictional factions, settings, gods, or magic.

# First Law

> **The world seeks to preserve balance, while intelligent beings constantly disrupt it.**

This is the primary world law. The world responds to accumulated disruption, while intelligent actors pursue goals that change the conditions around them. Balance and disruption create the pressure from which the remaining systems emerge: faction activity, observable symptoms, escalating consequences, and eventually world collapse.

# Balance

Balance is a **dynamic equilibrium**, not a static or ideal state. It constantly shifts as the world and its actors change. A temporary stabilisation is possible; permanent equilibrium is not.

No faction truly preserves Balance. Every faction pursues its own long-term goals and thereby pushes the world toward one of many extreme states. The player can also shift, redirect, or temporarily stabilise conditions, but cannot freeze the world in place.

Balance is a hidden systemic condition. It is inferred through the world rather than displayed as a single player-facing meter.

# World Engine

The World Engine is a lightweight scheduler of consequential faction actions. It does **not** simulate civilizations, populations, or every NPC decision.

Each faction owns only:

- long-term goals;
- available operations;
- current Faction Influence;
- conditions required to execute each operation.

At defined world intervals, the World Engine attempts one eligible operation. This keeps the simulation bounded, legible, and feasible for a solo developer while allowing the world to evolve without the player.

Operations may include, for example:

- perform a ritual;
- infiltrate an organization;
- corrupt an official;
- steal an artifact;
- assassinate a target;
- spread a disease;
- create a cult;
- sabotage a location.

Every operation may:

- succeed;
- fail;
- be stopped by the player;
- be stopped by another faction;
- fail because of a random world event.

Random world events must never function as opaque outcome rolls. They are observable conditions or events with readable effects. The player cannot always prevent them, but can learn how they alter operation outcomes and use that knowledge to predict consequences.

## Incremental scope

**V1:** A small number of factions, a fixed set of operations, simple eligibility conditions, and one attempted operation per interval. Every operation creates at least one readable symptom or persistent consequence.

**V2:** Operations can modify one another's conditions and Faction Influence, allowing indirect conflict and delayed consequences.

**V3:** Operations may form longer chains and create emergent, unintended cooperation between factions. The World Engine remains operation-based rather than becoming a full civilization simulation.

# Symptoms

The player never sees faction progress values or hidden Faction Influence. Instead, the player observes **Symptoms** and reads the world.

Symptoms may include:

- shops closing;
- merchants disappearing;
- price increases;
- mentors leaving;
- churches changing;
- strange sermons;
- disappearances;
- increased monster activity;
- dangerous districts;
- new patrols;
- rumours;
- corrupted locations.

Symptoms must be sufficiently consistent for players to form and test hypotheses. They create the context-gathering phase of the Core Loop and let knowledge improve future decisions without exposing a progress bar.

# World Collapse

A world never ends because of a timer. It ends because Balance has been irreversibly broken: a dominant faction achieves its objective and the world transitions into a terminal state.

The player may delay collapse, redirect which terminal state occurs, or accelerate collapse. The player can never prevent collapse forever. This preserves the finite-world Meta Loop while giving choices meaningful consequences.

The exact conditions that change Balance, define faction victory, and determine whether recovery is possible remain open design questions. No implementation may assume answers before they are resolved.

# Knowledge

The world becomes increasingly understandable through play. Failure should result primarily from incomplete understanding, poor preparation, or accepting a known risk—not from unknowable randomness.

By observing Symptoms, operations, and their consequences, the player learns to anticipate likely future changes. Knowledge never guarantees control: it improves prediction and opens better interventions, while the world retains uncertainty and independent agency.

# Documentation Review

- **Vision:** Compatible. The World Engine reinforces a living, finite world and makes knowledge more valuable than numerical power.
- **Core Loop:** Compatible. Symptoms provide discoverable context; operations generate meaningful consequences and new questions.
- **Meta Loop:** Compatible. Collapse is state-driven rather than timer-driven, and the world continues to change after character death.
- **Design Pillars:** Compatible. The model favours curiosity, trade-offs, incomplete knowledge, experimentation, and finite worlds.
- **Player Experience:** Compatible. Readable symptoms support tension, agency, surprise with understandable causes, and learning after loss.
- **Metadata and dependencies:** Present and linked. Affected canonical documents now include required metadata and references.
- **Glossary:** Updated with Balance, World Engine, Operation, Symptoms, Faction Influence, and World Collapse.
- **Open Questions:** Updated with the unresolved World Laws decisions that block detailed implementation.
