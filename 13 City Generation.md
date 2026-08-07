# 13 City Generation

Version: 0.7

Status: Draft (legacy / mood reference)

Last Updated: 2026-08-07

Depends On:
[02 World Laws](02%20World%20Laws.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
[14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), Prototype city scaffold in `unity/` (archive).

---

> **Status after Architecture Pivot (2026-08-07):** This document is **legacy / district-mood reference**. It is **not** the authoritative spatial model. Authority: [16 Architecture Pivot](16%20Architecture%20Pivot.md) (seeded overworld, separate interiors, arena combat). OrganicTown / `CityGenerator` code remains prototype archive.

# Purpose

Historical definition of the **seed-driven organic town scaffold** for Lower Veil mood: a late-18th/19th-century industrial-occult town that appears to have grown over decades.

Useful for: street names, district wedges, POI placement fiction, art direction.  
Not useful as: continuous grid gameplay truth or next implementation default.

Success question (historical):

> Does regenerating with seed A vs B change crooked streets and blocks while always placing slice POIs?

# Status (2026-08-07)

| Layer | State |
|-------|--------|
| **v1 raster playable** | Prototype archive — `CityGenerator` … `City_OrganicTown.unity`, menu `GameDev/City/…` |
| **v2 vector (OrganicTown)** | Prototype archive — A–D2 + P0 done under pre-pivot locks. Menu `GameDev/Town/…` |

**Living roadmap:** [16 Architecture Pivot](16%20Architecture%20Pivot.md) + post-pivot [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md).  
**Next phase:** Overworld V0 — not OrganicTown C1.

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

- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md)
- [12 Engine Choice](12%20Engine%20Choice.md)
- [90 Glossary](90%20Glossary.md)
