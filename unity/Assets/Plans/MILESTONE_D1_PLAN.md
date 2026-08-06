# Milestone D1 — Enterable BuildingTemplates

**Status:** In progress (D1.1 foundation)  
**Date started:** 2026-08-06  
**Authoritative order:** [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md)  
**Handoff:** [`AGENT_HANDOFF_WORLD_GEN.md`](AGENT_HANDOFF_WORLD_GEN.md)

---

## Goal

Replace solid exterior shells with **schematic enterable interiors** on OrganicTown v2 plots: template library → assign by kind/wealth/fit → greybox bake of floor / wall / door cells.

Locked constraints:

- Same continuous world (no separate interior scenes)
- No `GridMap` bridge yet (**C2**)
- No BG3 occlusion yet (**D2**)
- Deterministic: `WorldSeed.Fork("templates")`, list order only

---

## Micro-slices

| Slice | Deliverable | Done when |
|-------|-------------|-----------|
| **D1.1** | Data model + 5 coded templates + assign stage + interior bake | Templated buildings show floors/walls/door gaps; debug reports template coverage; tests pass |
| **D1.2** | Expand catalog toward 10–20 VS types | tavern, apothecary, churches, station, garrison, castle, palaces, noble/worker/peasant, school, university, market hall, dead-house, … |
| **D1.3** | NPC / prop sockets consumed by narrative layer | sockets readable by future C1; still no GridMap |

---

## D1.1 data model

- `BuildingInteriorCellKind`: `Empty`, `Floor`, `Wall`, `Door`, `PropSocket`, `NpcSocket`
- `BuildingTemplateId` + `BuildingTemplate` (W×H cells @ 1 m, door on local south / −Z)
- `BuildingTemplateCatalog` — static coded starters (headless-safe, no assets required)
- `Building`: `TemplateId`, `TemplateOrigin`, `TemplateYawDeg`
- `Stage_AssignTemplates` after `Stage_Buildings`
- `TownGreyboxBaker`: templated → cell paint; unassigned → solid OBB shell (B behavior)

### Starter templates (D1.1)

| Id | Size | Compatible kinds |
|----|------|------------------|
| PeasantHouse | 6×8 | Residential |
| WorkerHouse | 5×10 | Residential |
| Tavern | 8×12 | Commercial |
| Shop | 6×8 | Commercial, Industrial |
| SmallChurch | 10×14 | Religious, Civic |

---

## Out of D1.1

- Full 10–20 catalog (D1.2)
- Occlusion camera (D2)
- Rasterize into `GridMap` (C2)
- Narrative POIs (C1)

---

## Verify

```bash
dotnet run --project tools/TownDebugHost -- 110201
# Findings: roads OK + template coverage line
# Unity: GameDev/Town/Generate From Seed — look for door gaps / interior floors
```
