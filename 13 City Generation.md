# 13 City Generation

Version: 0.8

Status: Deprecated

Last Updated: 2026-08-09

Depends On:
[02 World Laws](02%20World%20Laws.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md)

Referenced By:
[14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), Prototype city scaffold in `unity/` (archive).

---

> **Deprecated (2026-08-09).** This document is a historical record only. The seed-driven organic town scaffold it describes (`WorldSeed` + `CityGenerator` + `OrganicTown` vector pipeline) is **not viable** as a foundation for the project: it produced a single hand-tuned, non-modular continuous town that was expensive to iterate on, did not scale, and did not read as a real city. It is **not** a reuse source — not for algorithms, not for street/district ideas, not as a "bridge" for later systems. World and settlement generation authority now lives entirely in [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) and [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), built on **Piece-Based Assembly** (small hand-authored modules snapped small → large, Valheim-style), not on this document's continuous-polygon growth pipeline.

# What This Document Was

Historical definition of a **seed-driven organic town scaffold** for the Lower Veil mood: a late-18th/19th-century industrial-occult town grown from a market plaza outward via organic road polylines, radial wedge districts, and frontage-parcel buildings.

Two prototypes were built and shipped as playable greybox under this design:

| Layer | What it was |
|-------|-------------|
| v1 raster | `CityGenerator` + raster helpers — a single playable Lower Veil scaffold |
| v2 vector (OrganicTown) | A–D2 + P0 vector pipeline — districts, parcels, roads, buildings, templates, sockets, occlusion, radius `GridMap` bake |

# Why It Was Retired

- **Not modular.** The generator produced one continuous, hand-tuned polygon town per seed — there was no small reusable "piece" to iterate on, tune, or reuse across settlements.
- **Not optimized.** Continuous per-cell raster/vector geometry and GridMap baking did not scale past a single small district.
- **Did not read as a real city.** Organic road midpoint-displacement and radial wedge districts produced a plausible silhouette but not a structurally believable town.
- **Tangled with combat and interiors.** The [16 Architecture Pivot](16%20Architecture%20Pivot.md) already identifies this entanglement (grid-truth-everywhere, enterable buildings in the same scene, in-location combat) as the root failure that made the whole stack too expensive for solo + AI-driven development.

# What Replaces It

- **Overworld shape:** seeded, chunked, Valheim-style — [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).
- **Settlement / building / interior generation:** Piece-Based Assembly — small hand-authored pieces with Snap Sockets, composed deterministically small → large — [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

Code under `unity/` implementing this document (`CityGenerator`, `OrganicTown`, `TownGridBaker`, related menus/scenes) remains in the repository as archive per [001 Documentation Standards](001%20Documentation%20Standards.md) ("never delete design history") but must not be extended.

# Documentation Review

- Deprecated in the same transaction that adds Piece-Based Assembly to [14](14%20World%20Generation%20Roadmap.md)/[15](15%20Procedural%20Building%20Generation.md).
- No longer authoritative for World Seed districts, street generation, or spatial gameplay truth.

# Related Documents

- [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md)
- [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md)
- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [90 Glossary](90%20Glossary.md)
