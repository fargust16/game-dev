# Work Report — OrganicTown Milestone B + Corridor Fix

**Date:** 2026-08-06  
**Audience:** future agents / humans continuing world gen  
**Authoritative stop-point:** [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md) · short handoff: [`AGENT_HANDOFF_WORLD_GEN.md`](AGENT_HANDOFF_WORLD_GEN.md)

---

## Summary

Completed **Milestone B** (greybox baker from v2 footprints) and immediately diagnosed + fixed a **street/block corridor topology** bug that made the bake look like random roads-through-buildings. Locked architecture preserved: v2 vector geometry remains authority; no GridMap bridge; no interiors; v1 raster city kept.

**Next default:** Milestone **D1** — enterable BuildingTemplates.

---

## What landed

### Milestone B — greybox visual layer

| Piece | Path / menu |
|-------|-------------|
| Baker | `unity/Assets/Scripts/World/Town/TownGreyboxBaker.cs` |
| Wire-up | `TownGenerator` (`bakeGreybox`, S0 prefab refs); auto-bake after `Generate` |
| Menu | `GameDev/Town/Bake Greybox`, `Generate From Seed` (bakes), `Clear` |
| Prefabs | `Floor_Tile_1x1`, `Building_Shell_1x1`, `Landmark_Block` under `Assets/Prefabs/S0_Greybox/` |

Baker responsibilities (only):

1. Street floor ribbons along road polylines at `RoadEdge.Width`
2. Exterior shells scaled to building footprint OBBs
3. Landmark block (skips duplicate `BuildingKind.Landmark` shells)
4. Simple directional sun under bake root

**Does not:** GridMap, player, POIs, interiors, occlusion.

### Agent-readable debug (critical)

Unity Scene view is not enough for agents. Added a dump pipeline:

| Piece | Purpose |
|-------|---------|
| `TownDebugReport.cs` | Markdown metrics + SVG top-down map + findings |
| `tools/TownDebugHost/` | Headless .NET host (Unity stubs) — run without locking Editor |
| Menu | `GameDev/Town/Export Debug Report` |
| Output dir | `unity/Assets/Plans/debug/` |
| How-to | `unity/Assets/Plans/debug/README.md` |

```bash
dotnet run --project tools/TownDebugHost -- 110201
```

Produces:

- `town_seed_<seed>_report.md`
- `town_seed_<seed>_map.svg`

Healthy targets after corridor fix: **roads-in-buildings ≲ 10%**, **roads-in-blocks ≲ 15%**.

### Corridor topology fix (post-B)

**Symptom:** greybox looked chaotic — roads through buildings, stubs to nowhere, fabric filling all space. Gizmos had looked “schematic” because outlines read as a plan; bake exposed the data bug.

**Root causes:**

1. `Stage_Streets.AddSplitStreet` linked **block centroids**, not the split edge
2. Leaf blocks were **not inset** by half street width
3. Bug in `Geo2.IntersectLineWithPolygon` (`u` sign wrong) → `TrySplitChord` often failed → almost no subdivision
4. Every street created fresh nodes → disconnected graph / “roads to nowhere”

**Fixes:**

| Change | Where |
|--------|--------|
| Split chord streets + mild `BuildCorridorPolyline` | `Stage_Streets`, `TownGraphUtil` |
| Leaf block inset by half width; district seed carve | `Stage_Streets` |
| Short dead-end stubs from corridor (not through-block) | `Stage_Streets` |
| `FindOrAddNode` + `MergeNearbyNodes` | `TownGraphUtil` |
| `TrySplitChord` / `IntersectLineWithPolygon` | `Geo2` |
| Milder arterial curvature | `Stage_PrimaryRoads` |
| Geo2 chord unit tests | `Geo2Tests` |

### Seed 110201 verification (TownDebugHost)

| Probe | Before fix | After fix |
|-------|------------|-----------|
| Road samples in buildings | 34.9% | **4.5%** OK |
| Road samples in blocks | 99.6% | **5.7%** OK |
| Degree-1 nodes | 983 | 168 OK |
| Blocks / buildings (order of magnitude) | ~3 / dense mess or centroid mess | 408 / 822 coherent |

Latest dump checked into `Assets/Plans/debug/` for baseline comparison.

---

## Docs updated in this step

- `14 World Generation Roadmap.md` — B ✅, geometry note, next = D1
- `13 City Generation.md` — status pointer
- `AGENTS.md` + `.cursor/rules/gamedev-design-guardian.mdc` — next world-gen = D1
- `procedural-organic-town-generator.md` — B1 ✅
- `AGENT_HANDOFF_WORLD_GEN.md` — stop point
- This report

Glossary / Open Questions: no terminology or OQ changes required.

---

## Locked architecture (do not reverse)

1. OrganicTown **v2** = geometry authority (metres, polylines/polygons)
2. Gameplay pathfinding stays **GridMap + A\*** until **C2**
3. Enterable buildings later = same continuous world grid (**D1**), not separate scenes for typical shops
4. Do **not** retire `GameDev/City/…` raster until C2
5. Implement roadmap one phase at a time: **D1 → D2 → C1 → C2 → E**

---

## How a new agent should start

1. Read `14 World Generation Roadmap.md` + this report + `AGENT_HANDOFF_WORLD_GEN.md`
2. Run `dotnet run --project tools/TownDebugHost -- 110201` — confirm Findings stay OK
3. In Unity: `GameDev/Town/Generate From Seed` — visual greybox + gizmos
4. Implement **D1** only unless user overrides: BuildingTemplate library (10–20 types) with interior cell paint + doors on plots
5. Do **not** start wilderness (E), full GridMap bridge (C2), or BG3 occlusion (D2) before D1

---

## Known leftovers / non-goals of this step

- Interior templates (**D1**) not started
- Narrative POIs / TipGivers still on v1 raster (**C1**)
- Some residual ~4–6% road/building overlap is acceptable noise (arterial bends, stub tips); do not “perfect” before D1 unless metrics regress
- `Building_Shell_2x2` unused by baker (1×1 scaled to metres — intentional)
- Empty `unity/Assets/Tests/` folder may exist as Unity stub; EditMode tests live under `unity/Assets/Scripts/Editor/Tests/`
