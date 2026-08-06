# 13 City Generation

Version: 0.4

Status: Draft

Last Updated: 2026-08-06

Depends On:
[02 World Laws](02%20World%20Laws.md), [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [12 Engine Choice](12%20Engine%20Choice.md)

Referenced By:
Prototype city scaffold in `unity/`, Vertical Slice playtests.

---

# Purpose

Define the **seed-driven organic town scaffold** for Lower Veil: a late-18th/19th-century industrial-occult town that appears to have grown over decades, not a Haussmann grid or a single carved avenue.

Meshes stay greybox. Gameplay truth stays `GridMap` + A*.

Success question:

> Does regenerating with seed A vs B change crooked streets and blocks while always placing slice POIs and garrison/undercroft stubs, with every anchor path-connected to the market?

# Status (2026-08-06)

| Layer | State |
|-------|--------|
| **v1 raster playable** | `CityGenerator` + `OrganicRoadBuilder` / `TownRoadGraph` / `TownBlockParcelizer` — scene `City_OrganicTown.unity`, menu `GameDev/City/…` |
| **v2 vector (OrganicTown)** | Milestone **A complete** (data model + stage pipeline + gizmos). Menu `GameDev/Town/…`, scene created via *Open Or Create Town Scene*. Plan: `unity/Assets/Plans/procedural-organic-town-generator.md`. Greybox bake (B), enterable interiors, BG3 occlusion, wilderness — **gated next** |

# Growth Pipeline (v1 raster — playable)

Implementation: `WorldSeed` + `CityGenerator` + raster helpers under `unity/Assets/Scripts/World/Town/`.

Order:

1. **Market plaza** (irregular) at town center — Soot & Bell hub.
2. **Attractors** on the rim — Ridge (N) gate, factory (E), Undercroft (S) gate, farm/bridge (W).
3. **Major roads** — organic polylines (midpoint displacement), variable width by district.
4. **Broken ring** + **secondary streets** + **alleys / courtyards / dead ends**.
5. **District mask** — radial wedges + distance (not axis-aligned bands).
6. **Frontage parcels** → greybox buildings; factory mass on the east.
7. **Mandatory anchors** → path guarantee to market.
8. Wire clock / POI / decisions / lighting / input.

# World Seed Streams

Prototype default seed: **110201** (Inspector-overridable).

| Stream | Owns |
|--------|------|
| `roads` | Plaza, attractors, majors, ring, secondaries, alleys |
| `blocks` | Reserved for block IDs / future subdivision detail |
| `parcels` | Frontage plot depth/width and building instances |
| `anchors` | Side/offset of mandatory sites (not presence) |
| `dressing` | Waymarkers, wells, sparse street stubs |

# Districts (wedges)

Grid target: **~180×140 cells**, ~1 m/cell. Town disk radius ~55–68.

| District | Geometry | Role |
|----------|----------|------|
| **Ridge** | Northern wedge, higher cells | Richer plots, wider streets, player spawn |
| **Lower Veil** | Central + eastern belt | Market, slice POIs, factory corridor |
| **Undercroft** | Southern dense wedge | Narrow alleys, Veinmarket, doctor |

Street widths (human scale): Ridge majors 3–4 cells, Veil arterial 3, Undercroft approach 2, alleys 1.

# Mandatory Anchors

Seed may change parcel side / offset. Presence is fixed. Each must A*-reach the market (corrective alley if needed).

| Anchor | Placement rule |
|--------|----------------|
| Soot & Bell + Merrin + Notice Board | On / abutting market plaza |
| Harrow & Tincture + POI A | Lower Veil along east major |
| Loom Lane / Alley + Nia + POI B | Lower Veil side-street / alley |
| Coal Stairs + POI C | Near factory mass, east |
| Ridge Approach spawn | North gate road, Ridge |
| Warden Post stub | Ridge / Lower boundary |
| Veinmarket Alley stub | Undercroft deep alley |
| Unlicensed doctor stub | Undercroft, different block |

Slice POIs **live in Lower Veil**.

# Performance (Greybox Scale)

- Merged **street floor strips** only (no per-cell cubes).
- Buildings along **street frontage**; factory as one mass.
- Optional dense grid debug drawer — expect cost on large maps.

# Editor

- Scene: `Assets/Scenes/City_OrganicTown.unity` (separate from legacy `S0_LowerVeil`)
- `GameDev/City/Open Or Create Town Scene`
- `GameDev/City/Generate From Seed`
- `GameDev/City/Generate From Seed (New Random)`
- Legacy `GameDev/S0/...` can still rebuild greybox; prefer City menu for the organic town.

# Explicitly Out

- Full traffic / carriage AI.
- NavMesh as movement truth.
- Beautiful art packs.
- Full World Engine Influence sim.
- Multi-city / overworld map.

# Documentation Review

- Replaces the E–W avenue scaffold model from v0.1.
- Extends World Seed (02) and grid rules (10.1); does not replace Decision Engine.

# Related Documents

- [11 Vertical Slice](11%20Vertical%20Slice.md)
- [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)
- [12 Engine Choice](12%20Engine%20Choice.md)
- [90 Glossary](90%20Glossary.md)
