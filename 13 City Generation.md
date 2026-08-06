# 13 City Generation

Version: 0.1

Status: Draft

Last Updated: 2026-08-06

Depends On:
[02 World Laws](02%20World%20Laws.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [12 Engine Choice](12%20Engine%20Choice.md)

Referenced By:
Prototype city scaffold in `unity/`, Vertical Slice playtests.

---

# Purpose

Define the **seed-driven city scaffold** that replaces the compact hand-painted S0 courtyard loop.

The mesh stays greybox. The layout becomes a readable multi-district street graph with mandatory Anchors.

Success question:

> Does regenerating with seed A vs B change streets/alleys while always placing slice POIs and garrison/undercroft stubs?

# World Seed Streams

Prototype default seed: **110201** (Inspector-overridable).

| Stream | Owns |
|--------|------|
| `roads` | Avenue Y, cross-streets, alleys, plaza |
| `parcels` | Building footprints along the avenue |
| `anchors` | Side/offset of mandatory sites (not presence) |
| `dressing` | Gas-lamp spacing, sparse street stubs, waymarkers |

Implementation: `WorldSeed` + `CityGenerator` in `unity/Assets/Scripts/World/`.

# Districts

Grid target: **~160×120 cells**, ~1 m/cell.

| District | Band (approx.) | Role |
|----------|----------------|------|
| **Ridge** | North | Higher class / height band; player spawn (Ridge Approach) |
| **Lower Veil** | Middle | Industrial main street — Vertical Slice playable core |
| **Undercroft** | South / dense alleys | Darker stubs — Veinmarket door, unlicensed doctor |

# Road Graph

- Primary **Avenue**: carriage-width **3 cells**, east–west through Lower Veil.
- Secondary **cross-streets** north–south, including a Ridge approach corridor.
- **Foot alleys**: 1-cell secondary graph intersecting the avenue; denser in Undercroft.
- At least one readable **crossroads plaza**.

Gameplay truth remains `GridMap` walkability + A* — not NavMesh.

# Performance (Greybox Scale)

Do **not** spawn one floor cube per cell.

- Merged **avenue/alley floor strips**.
- Buildings: instance existing S0_Greybox prefabs on parcels.
- Factory: one east mass volume in Lower Veil.

# Mandatory Anchors

Seed may change parcel side / offset. Presence is fixed.

| Anchor | District | Contents |
|--------|----------|----------|
| Soot & Bell + Merrin + Notice Board | Lower Veil | hub + tips |
| Harrow & Tincture + POI A | Lower Veil | apothecary shell |
| Loom Lane / Alley + Nia + POI B | Lower Veil | collectors shell |
| Coal Stairs + POI C | Lower Veil east | Trace after Aftermath |
| Ridge Approach spawn | Ridge | player start |
| Warden Post stub | Ridge / Lower edge | garrison + guard NPC stub |
| Veinmarket Alley stub | Undercroft | dark door + fixer NPC stub |
| Unlicensed doctor stub | Undercroft | optional tip source |

Slice POIs **live in Lower Veil** — city scale does not discard [11.1](11.1%20Vertical%20Slice%20POIs.md).

# Editor

- `GameDev/City/Generate From Seed` — rebuild scene children; seed on `CityGenerator` Inspector.
- `GameDev/City/Generate From Seed (New Random)` — tick-based seed, then generate.
- Legacy `GameDev/S0/...` menus forward to the same generator.

# Systems Wired After Generate

`GameInputService`, `WorldClock`, `PoiDirector`, `DistrictResidue`, `DecisionNodeSession`, lighting bind, pathfinding on generated walkability.

# Explicitly Out

- Full traffic / carriage AI.
- NavMesh as movement truth.
- Beautiful art packs.
- Full World Engine Influence sim.
- Multi-city / overworld map.

# Documentation Review

- Extends World Seed (02) and grid rules (10.1); does not replace Decision Engine.
- Greybox milestone scale superseded; slice content still required.

# Related Documents

- [11 Vertical Slice](11%20Vertical%20Slice.md)
- [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)
- [12 Engine Choice](12%20Engine%20Choice.md)
- [90 Glossary](90%20Glossary.md)
