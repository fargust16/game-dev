# Milestone D1 — Enterable BuildingTemplates

**Status:** D1.1–D1.2 done; next D1.3  
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
| **D1.1** ✅ | Data model + 5 coded templates + assign stage + interior bake | Seed 110201: ~26% templated before small fillers; roads OK |
| **D1.2** ✅ | Expand catalog toward 10–20 VS types + small fillers | 16 types; seed 110201 **55%** templated (was 26%) |
| **D1.3** | NPC / prop sockets consumed by narrative layer | sockets readable by future C1; still no GridMap |

---

## D1.1 data model

- `BuildingInteriorCellKind`: `Empty`, `Floor`, `Wall`, `Door`, `PropSocket`, `NpcSocket`
- `BuildingTemplateId` + `BuildingTemplate` (W×H cells @ 1 m, door on local south / −Z)
- `BuildingTemplateCatalog` — static coded starters (headless-safe, no assets required)
- `Building`: `TemplateId`, `TemplateOrigin`, `TemplateYawDeg`
- `Stage_AssignTemplates` after `Stage_Buildings`
- `TownGreyboxBaker`: templated → cell paint; unassigned → solid OBB shell (B behavior)

### Catalog (D1.1 + D1.2)

| Id | Size | Compatible kinds |
|----|------|------------------|
| Booth | 4×4 | Commercial |
| Cottage | 4×6 | Residential |
| AlleyShop | 4×6 | Commercial, Industrial |
| PeasantHouse | 6×8 | Residential |
| WorkerHouse | 5×10 | Residential |
| Shop | 6×8 | Commercial, Industrial |
| DeadHouse | 6×8 | Civic, Religious |
| Apothecary | 6×10 | Commercial, Civic |
| Tavern | 8×12 | Commercial |
| Station | 8×10 | Civic, Industrial |
| School | 8×10 | Civic |
| Garrison | 10×12 | Civic, Industrial |
| NobleHouse | 10×12 | Residential (Rich) |
| SmallChurch | 10×14 | Religious, Civic |
| MarketHall | 12×14 | Commercial, Civic |
| University | 12×16 | Civic |

Still missing vs full VS list (later): castle/palace variants, dedicated apothecary art pass — schematics above cover function first.

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
