# 14 World Generation Roadmap

Version: 0.3

Status: Draft

Last Updated: 2026-08-06

Depends On:
[10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [13 City Generation](13%20City%20Generation.md)

Referenced By:
[AGENTS.md](AGENTS.md), Unity implementation under `unity/Assets/Scripts/World/Town/`, plan `unity/Assets/Plans/procedural-organic-town-generator.md`, work report `unity/Assets/Plans/MILESTONE_B_WORK_REPORT.md`, D1 plan `unity/Assets/Plans/MILESTONE_D1_PLAN.md`

---

# Purpose

Handoff document for **future agents and humans**: where world/town generation stopped, what is authoritative, and the **ordered roadmap** toward a Valheim-scale seeded world with an enterable historic town (BG3-like interiors + camera).

Chat is not the source of truth — update this file when a milestone lands.

Success question for agents:

> Can a new agent open this doc and know exactly what to implement next without re-deriving the architecture from chat?

---

# Where We Stopped (2026-08-06)

## Done

| Track | Deliverable | How to open |
|-------|-------------|-------------|
| **OrganicTown v2 Milestone A** | Vector town: `TownModel` filled by stage pipeline + gizmos | `GameDev/Town/Open Or Create Town Scene` → Scene view Gizmos |
| **OrganicTown v2 Milestone B** | Greybox baker: street ribbons + exterior shells + landmark | `GameDev/Town/Generate From Seed` (auto-bake) or `Bake Greybox` |
| **OrganicTown v2 D1.1 (partial)** | Template data + 5 starters + assign stage + interior bake | See [`MILESTONE_D1_PLAN.md`](unity/Assets/Plans/MILESTONE_D1_PLAN.md); Unity generate to verify |
| **v1 raster playable city** | Walkable greybox Lower Veil (shells, POIs, tips, clock) | `GameDev/City/Generate From Seed` → `City_OrganicTown.unity` + Play |

### v2 Milestone A — key files

- Plan (detail): [`unity/Assets/Plans/procedural-organic-town-generator.md`](unity/Assets/Plans/procedural-organic-town-generator.md)
- Orchestrator: `unity/Assets/Scripts/World/Town/TownGenerator.cs`
- Stages: `Stage_SiteAndLandmark` … `Stage_ConnectivityAndScale`
- Geometry: `unity/Assets/Scripts/World/Town/Geometry/Geo2.cs`, `Polygon2.cs`
- Config: `unity/Assets/Settings/DefaultTownProfile.asset`
- Menu: `GameDev/Town/…` (`TownGenerateMenu.cs`)
- Tests: `unity/Assets/Scripts/Editor/Tests/Geo2Tests.cs`, `TownDeterminismTests.cs`

### v2 Milestone B — key files

- Baker: `unity/Assets/Scripts/World/Town/TownGreyboxBaker.cs`
- Wired from `TownGenerator` (`bakeGreybox`, S0 prefab refs) + menu `GameDev/Town/Bake Greybox`
- Prefabs: `Assets/Prefabs/S0_Greybox/` (`Floor_Tile_1x1`, `Building_Shell_1x1`, `Landmark_Block`, `Wall_Segment_1x3`)
- Still **no** `GridMap` bridge / narrative POIs

### v2 D1.1 — key files (in progress)

- Plan: [`unity/Assets/Plans/MILESTONE_D1_PLAN.md`](unity/Assets/Plans/MILESTONE_D1_PLAN.md)
- `BuildingTemplate.cs`, `BuildingTemplateCatalog.cs` (5 coded starters)
- `Stage_AssignTemplates.cs` (fork `templates`)
- Baker paints floor/wall/door cells when `Building.HasTemplate`
- Tests: `BuildingTemplateTests.cs`
- **Next inside D1:** D1.2 expand catalog to ~10–20 types

### v1 raster — key files (still live for gameplay)

- `CityGenerator.cs`, `OrganicRoadBuilder.cs`, `TownRoadGraph.cs`, `TownBlockParcelizer.cs`
- Menu: `GameDev/City/…`
- Scene: `unity/Assets/Scenes/City_OrganicTown.unity`

## Not done yet

- D1 not complete — D1.1 foundation landed; catalog still short of 10–20 types (**D1.2**)
- No BG3-style wall/roof fade (**D2**)
- No narrative POI port onto v2 + `GridMap` bridge; raster gen not retired (**C**)
- No wilderness / caves outside walls (**E**)

### Geometry note (post-B fix)

`Stage_Streets` corridor topology was corrected after Milestone B: streets follow OBB split chords (not centroid links), leaf blocks are inset by half street width, road nodes snap/merge. Agent-readable check: `dotnet run --project tools/TownDebugHost -- <seed>` → `unity/Assets/Plans/debug/`.

---

# Locked Architecture (do not silently reverse)

1. **OrganicTown v2** is the authoritative *geometry* generator (world metres, polylines/polygons). Milestone A = data + gizmos only.
2. **Gameplay pathfinding** remains **`GridMap` + A\*** until Milestone **C2** adds a rasterization bridge from v2 (+ interiors).
3. **Enterable buildings** = **same continuous world grid** (doors = walkable openings). Not separate interior scenes for typical shops/taverns.
4. **BuildingTemplate library** (10–20 greybox types) later: footprint + interior cell paint + NPC sockets; placement by district / `BuildingKind`.
5. **BG3 camera**: fade/x-ray exterior walls (and optionally roof) while the player is inside a building volume (`BuildingOcclusionController` + follow cam).
6. **“Valheim-level”** means deterministic seeded world + dense reusable templates + exploration loop — **not** AAA biomes/art in these phases. Greybox until asset packs.
7. **Do not retire** v1 raster (`GameDev/City/…`) until **C2** parity.
8. Implement roadmap **one micro-plan at a time** (B, then D1, …). Do not collapse Valheim + interiors into one uncontrolled sprint.

---

# Roadmap (mandatory order)

```text
A data+gizmos ✅
    → B greybox shells from footprints ✅
        → D1 enterable BuildingTemplates (10–20 types)
            → D2 BG3 occlusion camera
                → C1 narrative POIs on v2
                    → C2 GridMap bridge + retire raster
                        → E wilderness ring + cave templates
```

| Phase | Goal | Outcome | Depends on |
|-------|------|---------|------------|
| **A** | Vector town data + gizmos | `TownModel` + Scene gizmos | — |
| **B** ✅ | Greybox baker | Street ribbons + exterior shells from footprints (S0 prefabs) | A |
| **D1** | Enterable templates | 10–20 schematic interiors (tavern, apothecary, churches, station, garrison, castle, palaces, noble/worker/peasant houses, school, university, market hall, dead-house, …) with interior grid + doors; place on plots | B |
| **D2** | BG3 occlusion | Walls/roof fade when inside; character stays readable | D1 |
| **C1** | Narrative layer | Port slice anchors/TipGivers/NPCs onto v2 plazas/plots | B (visuals), A (data) |
| **C2** | Grid bridge + retire v1 | Rasterize roads+interiors → `GridMap`; repoint menus; keep Input System / clock / decisions | C1, D1 |
| **E** | Overworld ring | Outside wall: fields/forest/meadows; 3–5 cave/dungeon templates; portal exits | C2 |

Each phase = its own plan + commit series. Prefer updating this file’s “Where We Stopped” when a phase completes.

---

# Agent Instructions (quick)

1. Read this file + [`MILESTONE_B_WORK_REPORT.md`](unity/Assets/Plans/MILESTONE_B_WORK_REPORT.md) + [`13 City Generation.md`](13%20City%20Generation.md) + the Unity plan under `Assets/Plans/`.
2. **Next implementation default:** finish **D1** — D1.1 foundation exists; expand catalog (**D1.2**) unless the user explicitly requests another phase.
3. Keep style: no inheritance tricks, `WorldSeed.Fork` per stage, list order (no HashSet iteration for geometry), human-scale metres on XZ.
4. Confirm design forks with the user before changing locked architecture above.
5. After a documentation or milestone step: commit and push to `main` per project habit.

---

# Explicitly Out (until later phases)

- Full traffic / carriage AI
- NavMesh as movement truth
- Beautiful art packs (use greybox + later marketplace assets)
- Full World Engine Influence sim
- Multi-city overworld map
- Shipping combat AI / S3 TB combat as part of gen milestones

---

# Related Documents

- [13 City Generation](13%20City%20Generation.md) — v1 raster rules + status pointer
- [12 Engine Choice](12%20Engine%20Choice.md) — Unity / Input System Package Only
- [10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- Unity detail plan: `unity/Assets/Plans/procedural-organic-town-generator.md`
