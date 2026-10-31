# 000 Project Rules

Version: 1.17

Status: Canonical

Last Updated: 2026-10-31

Depends On:
—

Referenced By:
All Project Documentation

---

# Purpose

This document defines the rules for developing the project.

It is the highest-level document in the repository.

If any document contradicts these rules, this document takes precedence.

---

# Documentation First

The project is documentation-driven.

Nothing exists until it is documented.

Every finalized decision must become part of the project documentation.

Chat history is never considered the source of truth.

Canonical documents are.

---

# Document Hierarchy

Every new document must respect the following hierarchy.

000 Project Rules

↓

00 Vision

↓

00.1 Core Loop

↓

00.2 Meta Loop

↓

00.5 Design Pillars

↓

01 Player Experience

↓

02 World Laws

↓

03 Decision Engine

↓

03.1 Decision Node Template

↓

04 Points of Interest

↓

05 Progression

↓

06 Approaches

↓

07 Character Life

↓

07.1 Character Attributes, Skills, and Action Checks

↓

07.2 Derived Character Statistics

↓

08 Confront Constraints

↓

09 Information Sources

↓

10.1 Grid World and Environment Actions

↓

10.2 Combat System Draft

↓

11 Vertical Slice

↓

11.1 Vertical Slice POIs

↓

11.2 Milestone S0 Greybox

↓

12 Engine Choice

↓

13 City Generation

↓

14 World Generation Roadmap

↓

15 Procedural Building Generation

↓

16 Architecture Pivot

↓

17 Game Systems Architecture

↓

18 World Time and Living Simulation

↓

18 RPG Systems Coverage Draft

↓

19 Items, Crafting, Inventory, Equipment, and Enchanting

↓

All remaining documentation

A lower-level document may extend a higher-level document but must never contradict it.

---

# Document Status

Every document must contain:

Version

Status

Last Updated

Depends On

Referenced By

Allowed statuses:

Canonical

Draft

Deprecated

Only Canonical documents define the project.

---

# Development Pipeline

Every system follows four stages.

1. Research

2. Design

3. Documentation

4. Implementation

Implementation may never begin before documentation exists.

---

# Design Validation

Every mechanic must answer three questions.

1.
Does it create curiosity?

2.
Does it teach something about the world?

3.
Does every gained power have a meaningful cost?

If any answer is "No",
the mechanic must be redesigned.

---

# Canonical Change Policy

Canonical documents cannot be changed casually.

Every modification must explain:

Why the current solution is insufficient.

Which documents are affected.

Whether Glossary must change.

Whether Open Questions must change.

Whether dependencies change.

---

# Documentation Transaction

A documentation update is complete only after confirming:

Glossary updated if needed.

Open Questions updated if needed.

Dependencies updated.

Version updated.

Related documents reviewed.

---

# System-first Design

Systems are designed before content.

Never create:

Cities before settlement systems.

Bosses before boss systems.

Artifacts before artifact systems.

Content exists to demonstrate systems.

---

# Incremental Systems

Every major system must be designed in three versions.

## V1

Minimal playable implementation.

It must already produce the intended player experience and remain realistically implementable by one developer.

## V2

Adds meaningful depth.

## V3

Adds advanced interactions.

Whenever choosing between a deeper simulation and a simpler expandable system, choose the simpler expandable system.

The project must remain achievable for a solo developer.

---

# Philosophy Preservation

The project always prioritizes:

Curiosity over power.

Knowledge over levels.

Meaningful consequences.

Living worlds.

Finite worlds.

Every power has a price.

If a future idea weakens these principles,
the idea must be challenged.

---

# Roles

Project Creator

Defines the vision, direction and player fantasy.

AI Design Guardian

Protects consistency.

Finds contradictions.

Challenges weak ideas.

Maintains documentation quality.

Suggests alternatives.

Never agrees automatically.
