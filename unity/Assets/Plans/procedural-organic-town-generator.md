# Project Overview

> **Agent handoff:** stop-point + full world roadmap live in repo-root [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md) and [`AGENT_HANDOFF_WORLD_GEN.md`](AGENT_HANDOFF_WORLD_GEN.md). Milestones A+B complete; **D1.1 foundation in progress** — see [`MILESTONE_D1_PLAN.md`](MILESTONE_D1_PLAN.md).

- **Game Title / System:** Procedural Organic Historic Town Generator ("OrganicTown v2")
- **High-Level Concept:** A deterministic, seed-driven generator that grows a believable late‑18th/19th‑century European town from a central landmark outward — arterials to gates/bridges/farms, then districts → blocks → plots → buildings — producing gently curved, varied‑width, irregular streets that read as *grown over decades*, not planned on a grid. Target look: pre‑Haussmann Paris, Prague, Edinburgh, Bruges, Rothenburg ob der Tauber.
- **Players:** N/A (authoring/runtime world system). Consumed by the existing single‑player grid game.
- **Inspiration / Reference:** Parish & Müller "Procedural Modeling of Cities"; Citygen/Watson subdivision; Sean Barrett / Oskar Stålberg organic layouts; real medieval city morphology.
- **Tone / Art Direction:** Historic European, human‑scale. Reuses existing S0_Greybox prefab set for the (later) visual layer; core deliverable is the abstract geometry + debug gizmos.
- **Target Platform:** StandaloneWindows64 (project default).
- **Render Pipeline:** URP 17.5.0 (existing).
- **Unity Version:** 6000.5.7f1.

## Decisions locked with user
1. **Architecture:** New **geometric/vector** system (world‑space meters; polyline roads with per‑edge width; polygonal blocks/plots/footprints). *Not* the raster grid model.
2. **Relation to existing:** **Generalize & replace** — new system becomes the authoritative town generator; the narrative POI/dressing layer becomes an optional module layered on top of the new block/plot data. The old raster generator is kept behind its current menu until the new system reaches parity, then retired (see Risks).
3. **Fidelity:** **Data model + debug gizmos first.** Visual prefab instantiation is a thin, later layer.
4. **Timing:** **Runtime generation** from a seed — must be deterministic, fast, and allocation‑conscious (with an optional time‑sliced coroutine path to avoid hitches).

---

# Game Mechanics (Generation Model)

## Core Generation Loop
The generator is a deterministic pipeline of ordered stages. Each stage receives the shared `TownModel` and a **per‑stage RNG** forked from the master seed (`WorldSeed.Fork("stageName")`), guaranteeing reproducibility and stable behavior when a single stage changes.

1. **Site & Landmark** — establish town center, orientation, optional terrain features (a river/coast line and/or a hill ridge for Edinburgh‑like prominence), pick the central landmark type (market square / church / castle / town hall), and carve an irregular central plaza polygon around it.
2. **Boundary & Portals** — generate an organic town boundary polygon (soft blob, optionally clipped by river/coast or bounded by a town wall à la Rothenburg). Place typed **portals** on the boundary: `Gate`, `Bridge` (only where an arterial meets a river), `FarmRoad`, `SettlementRoad`.
3. **Primary Roads (arterials + ring)** — grow gently curved arterial polylines from the central plaza to each portal (curvature‑limited, perturbed, snapping into shared segments when they run close — organic bundling). Add one or two **partial/broken ring** roads. Each road carries a `RoadClass` → width.
4. **Districts** — partition the interior (bounded by arterials/ring) into districts. Classify each by **wealth/type** (`Rich`, `Mixed`, `Poor`, plus `Civic`, `Industrial`, `Market`) using seeds biased by: proximity to landmark/center, elevation (rich on high ground), proximity to river/industry/edges (poor). Each district gets a `DistrictProfile` controlling street density, block/plot size, street widths, courtyard/garden/dead‑end probabilities.
5. **Secondary & Tertiary Streets** — within each district polygon, recursively subdivide (OBB splitting; see Key Asset & Context) into blocks. Split lines become streets, jittered in position and slightly curved for rendering; junctions favor **T over X** (organic). Rich → sparse/wider/straighter; Poor → dense/narrow/winding, more dead‑ends, service alleys, and cut‑through shortcuts.
6. **Blocks → Plots** — inset each block by the bounding street half‑widths to get buildable land; recursively subdivide into **plots** targeting per‑district frontage widths. Guarantee street frontage per plot; leftover interior becomes **courtyard** (dense/poor) or **private garden** (rich).
7. **Buildings** — place a building **footprint** per plot with setback + depth from the district profile; row/party‑wall footprints in dense districts, detached with gardens in rich; occasional L‑shaped corner footprints; skip some plots for gardens/gaps.
8. **Open Spaces** — small **plazas** at select multi‑way junctions and around the landmark; **wells** at plazas/junctions; gardens/courtyards from step 6.
9. **Connectivity & Prominence** — flood‑fill the road graph to verify every plot and **every portal + the landmark** is reachable; add minimal corrective links otherwise. Enforce landmark **visual prominence**: arterials aim at the landmark, keep an open view cone / plaza, optionally place it on the highest terrain.
10. **Human‑scale validation** — assert metric bounds (street widths, plaza sizes, plot frontage/depth, footprint sizes); log violations.

## Controls and Input Methods (authoring)
- **Editor menu** `GameDev/Town/…`: *Generate From Seed*, *Generate Random Seed*, *Clear*, *Open/Create Town Scene* (mirrors existing `CityGenerateMenu`).
- **Inspector** on `TownGenerator` MonoBehaviour: seed field, `TownProfile` reference, per‑layer gizmo toggles, "Generate on Play" flag, "Time‑sliced" flag.
- **Runtime:** `TownGenerator.Generate(int seed)` (synchronous) or `GenerateAsync(int seed)` (coroutine, budgeted per frame).

---

# UI

Debug‑first visualization (no game UI). `TownGizmoDrawer` (`OnDrawGizmos` / `OnDrawGizmosSelected`) with toggle flags:

- **Boundary & terrain:** town polygon (white), river/coast (blue), ridge contour (brown).
- **Portals:** colored markers by type (Gate/Bridge/Farm/Settlement) + labels.
- **Roads:** polylines colored by class (Arterial = thick amber, Secondary = orange, Tertiary = yellow, Alley = grey), rendered at true width as thin quads.
- **Districts:** filled/translucent polygons colored by wealth (Rich = green, Mixed = teal, Poor = red, Civic = blue, Industrial = grey, Market = gold).
- **Blocks / Plots:** block outlines (dark), plot outlines (thin), building footprints (filled).
- **Open spaces:** plazas (light), wells (dots), gardens/courtyards (green hatch).
- **Landmark:** highlighted footprint + view‑cone lines.

Text overlay via `TextMesh`/Handles (editor only) showing seed, counts (roads/blocks/plots/buildings), and validation warnings.

Wireframe (top‑down, +Z up, +X right; world‑space meters):
```
              [Gate N]
                 |  arterial (curved)
   [Farm W]---+  |  +---[Settlement E]
             \ \ | / /
     districts  (PLAZA + LANDMARK)  districts
             / / | \ \
   [Bridge SW]  |  broken ring
                 |
              [Gate S]
   Poor districts: dense small blocks, alleys, courtyards
   Rich districts: large blocks, wide streets, gardens
```

---

# Key Asset & Context

## World / coordinate conventions
- World‑space **XZ plane**, Y up. Positions in **meters** (human scale). Geometry uses `Vector2` (x→X, y→Z) internally; a helper maps to `Vector3`.
- Determinism: all randomness via `WorldSeed.Fork(stage)` (existing, reused). **Never** rely on `HashSet`/`Dictionary` iteration order for anything affecting geometry — iterate `List`s in insertion order and sort explicitly when needed.
- Allocation‑conscious: pre‑sized `List<>` buffers, reusable scratch buffers on the generator, structs for geometry. Correctness first; then pool the hot buffers.

## Geometry utilities (new) — the technical core
`Geo2` static helper (2D, `Vector2`):
- `Cross`, `Dot`, `PolygonArea` (signed), `PolygonCentroid`, `PointInPolygon`, `SegmentIntersect`, `ClosestPointOnSegment`.
- `InsetPolygon(poly, distance)` — edge‑offset inward (miter, with self‑intersection guard).
- `ObbLongestAxis(poly)` — oriented bounding box via rotating calipers → longest axis + split direction.
- `SplitPolygonByLine(poly, point, dir)` → two sub‑polygons (Sutherland–Hodgman style half‑plane clip).
- `ResamplePolyline(pts, spacing)`, `SmoothCatmullRom(pts, tension, samples)` — for gently curved streets.
- `ChaikinSmooth(poly, iters)` — soften block/plot corners.

`Polygon2` — thin class wrapping `List<Vector2>` + cached area/centroid.

## Data model (new POCOs) — the primary deliverable
- `TownModel` — root: `seed`, `bounds` (Polygon2), `center`, `terrain` (river/coast polylines, ridge), `landmark`, `List<Portal>`, `List<RoadEdge>` + `List<RoadNode>` (planar graph), `List<District>`, `List<Block>`, `List<Plot>`, `List<Building>`, `List<OpenSpace>`.
- `RoadClass { Arterial, Secondary, Tertiary, Alley, Service }`; `RoadNode { pos, edges }`; `RoadEdge { nodeA, nodeB, List<Vector2> polyline, RoadClass class, float width, bool deadEnd, bool shortcut }`.
- `Portal { pos, PortalType (Gate|Bridge|FarmRoad|SettlementRoad) }`.
- `WealthClass { Rich, Mixed, Poor }`; `DistrictType { Residential, Civic, Market, Industrial }`; `District { Polygon2 area, WealthClass, DistrictType, DistrictProfile profile }`.
- `Block { Polygon2 outline, districtIndex }`; `Plot { Polygon2 outline, blockIndex, hasFrontage, Vector2 frontageDir }`.
- `Building { Polygon2 footprint, float floors, plotIndex, BuildingKind }`; `OpenSpace { Polygon2 area, OpenSpaceKind (Plaza|Well|Garden|Courtyard) }`.
- `Landmark { LandmarkKind (MarketSquare|Church|Castle|TownHall), Polygon2 footprint, Polygon2 plaza }`.

## Config (new ScriptableObject) — replaces `TownGrowthParams` constants
`TownProfile` (metric, tunable, generalizable):
- Town radius range; boundary irregularity; wall on/off; river/coast probability; ridge on/off.
- Portal count range + type weights.
- Per‑`RoadClass` width range (Arterial ~8–12 m, Secondary ~4–6 m, Tertiary ~3–4 m, Alley ~1.5–3 m, Service ~1.5–2 m); curvature/jitter amplitudes.
- Per‑`WealthClass` `DistrictProfile`: target block size, plot frontage/depth range, street density, setback, floors range, courtyard/garden/dead‑end/shortcut probabilities.
- Landmark type weights; plaza size range; well density.

## Existing assets to reuse / generalize / retire
- **Reuse:** `WorldSeed` (`Assets/Scripts/World/WorldSeed.cs`) — seed forking; existing S0_Greybox prefabs & materials (later visual layer only); editor‑menu pattern from `CityGenerateMenu.cs`.
- **Generalize/retire (kept until parity):** `OrganicRoadBuilder`, `TownRoadGraph`, `TownBlockParcelizer`, `TownGrowthParams` (raster). The narrative content in `CityGenerator.PlaceMandatoryAnchors/PlaceDressing` becomes an optional `TownNarrativeLayer` consuming the new plot/block data.
- **Namespaces:** `GameDev.World.Town` (model, pipeline, config) and `GameDev.World.Town.Geometry` (Geo2, Polygon2). PascalCase public, `_camelCase` private (matches project). No asmdefs (project uses `Assembly-CSharp`).

## High‑risk algorithm note
Block generation uses **recursive OBB subdivision** of district polygons (robust, deterministic, cheap) rather than half‑edge planar‑face extraction. Arterials/ring carve the town into district super‑polygons first (half‑plane clipping against curved road corridors, approximated by their sampled polylines); minor streets emerge from subdivision. **Fallback** if district clipping against curved arterials proves unstable: approximate each arterial corridor as a sequence of straight segments for clipping, keep the smooth polyline only for rendering/width.

---

# Implementation Steps

> Roles: **explorer** (read‑only investigation) · **developer** (writes code). Milestone A delivers the data model + gizmos (the agreed first deliverable). Milestones B/C are the thin visual + narrative layers, planned but gated behind A's approval.

### Milestone A — Deterministic data model + debug gizmos

> **Progress (2026-08-06): A1–A10 DONE. B1 DONE. D1.1–D1.3 DONE (16 templates, sockets+roles+query).** Next: D2 occlusion.

**Step A1 — Geometry foundation** ✅
- Create `Assets/Scripts/World/Town/Geometry/Geo2.cs` and `Polygon2.cs` with the utilities above.
- **Role:** developer · **Dependencies:** None · **Parallelizable:** Yes (independent of A2).

**Step A2 — Data model + config** ✅
- Create `TownModel.cs`, `RoadEdge.cs` (+`RoadNode`,`RoadClass`), `Portal.cs`, `District.cs` (+enums,`DistrictProfile`), `Block.cs`, `Plot.cs`, `Building.cs`, `OpenSpace.cs`, `Landmark.cs`, and `TownProfile.cs` (ScriptableObject) with a `CreateAssetMenu`. Author one default `TownProfile` asset under `Assets/Settings/`.
- **Role:** developer · **Dependencies:** A1 (uses `Polygon2`) · **Parallelizable:** Partially.

**Step A3 — Orchestrator + seed streams** ✅
- Create `TownGenerator.cs` (MonoBehaviour): fields `seed`, `TownProfile profile`, gizmo toggles, "generate on play", "time‑sliced". `Generate(int seed)` runs stages in order; `GenerateAsync` coroutine variant. Reuse `WorldSeed.Fork`. Reusable scratch buffers.
- **Role:** developer · **Dependencies:** A2 · **Parallelizable:** No.

**Step A4 — Stage: Site, terrain & landmark** ✅
- `Stage_SiteAndLandmark.cs`: center, orientation, optional river/coast + ridge, boundary polygon, landmark type + footprint + central plaza.
- **Role:** developer · **Dependencies:** A3 · **Parallelizable:** No.

**Step A5 — Stage: Portals & primary roads** ✅
- `Stage_PrimaryRoads.cs`: typed portals on boundary; curved arterials center→portals (Catmull‑Rom, curvature‑limited, bundling); 1–2 broken rings; populate planar graph (`RoadNode`/`RoadEdge`) with widths.
- **Role:** developer · **Dependencies:** A4 · **Parallelizable:** No.

**Step A6 — Stage: Districts** ✅
- `Stage_Districts.cs`: partition interior into district polygons (clip against arterial/ring corridors; fallback per Risk note); classify wealth/type; assign `DistrictProfile`.
- **Role:** developer · **Dependencies:** A5 · **Parallelizable:** No.

**Step A7 — Stage: Secondary streets + blocks** ✅
- `Stage_Streets.cs`: recursive OBB subdivision of each district → blocks; split lines → Secondary/Tertiary/Alley streets (jitter + slight curve, T‑junction bias); dense districts add dead‑ends, service alleys, and cut‑through shortcuts.
- **Role:** developer · **Dependencies:** A6 + A1 · **Parallelizable:** No.

**Step A8 — Stage: Plots + buildings + open spaces** ✅
- `Stage_Plots.cs` (inset blocks, recursive plot subdivision, frontage guarantee, courtyard/garden leftovers), `Stage_Buildings.cs` (footprints, row/detached/L‑corner, floors), `Stage_OpenSpaces.cs` (plazas at junctions, wells, gardens/courtyards).
- **Role:** developer · **Dependencies:** A7 · **Parallelizable:** Plots→Buildings sequential; OpenSpaces partially parallel after plots.

**Step A9 — Stage: Connectivity & prominence + human‑scale validation** ✅
- `Stage_ConnectivityAndScale.cs`: road‑graph flood‑fill; ensure all portals + landmark + every plot reachable (corrective links); enforce landmark view cone/plaza/elevation; assert metric bounds and log violations.
- **Role:** developer · **Dependencies:** A8 · **Parallelizable:** No.

**Step A10 — Debug gizmos + editor menu** ✅
- `TownGizmoDrawer.cs` (layered toggles per UI section) and `Assets/Scripts/Editor/TownGenerateMenu.cs` (`GameDev/Town/…` generate/clear/open‑scene). Create `Assets/Scenes/Town_Generator.unity` (or reuse a scratch scene) hosting `TownGenerator`.
- **Role:** developer · **Dependencies:** A2..A9 · **Parallelizable:** Gizmos vs menu parallel.

### Milestone B — Thin visual (greybox) layer *(gated behind A)* ✅
**Step B1** ✅ — `TownGreyboxBaker.cs`: instantiate street floor ribbons (curved) + building shells scaled to footprints + landmark block, reusing S0_Greybox prefabs. Wired from `TownGenerator` (`bakeGreybox`) and `GameDev/Town/Bake Greybox`. **Role:** developer · **Dependencies:** Milestone A · **Parallelizable:** No.

### Milestone D1 — Enterable BuildingTemplates *(gated behind B)*
Micro-plan: [`MILESTONE_D1_PLAN.md`](MILESTONE_D1_PLAN.md).

**Step D1.1** ✅ (foundation) — `BuildingTemplate` + coded catalog (PeasantHouse, WorkerHouse, Tavern, Shop, SmallChurch); `Stage_AssignTemplates` (`WorldSeed.Fork("templates")`); baker paints floor/wall/door cells; unfitted plots keep solid shells. **Role:** developer · **Dependencies:** B1.

**Step D1.2** ✅ — Expand catalog to 16 types (Booth/Cottage/AlleyShop + apothecary, station, garrison, school, university, market hall, dead-house, noble house); prefer largest wealth-compatible fit; seed 110201 ≈55% templated. **Dependencies:** D1.1.

**Step D1.3** ✅ — `BuildingTemplateSockets` / `TownSocketQuery`: placed roles + world positions; explicit-socket override hook; gizmos + greybox markers. Still no C1 spawn / GridMap. **Dependencies:** D1.2.

### Milestone C — Narrative layer + old‑system retirement *(gated behind A/B)*
**Step C1** — `TownNarrativeLayer.cs`: port POI/anchor/tip‑giver placement (from `CityGenerator`) to consume new block/plot/plaza data. **Dependencies:** A (+ B for visuals).
**Step C2** — Retire raster `OrganicRoadBuilder`/`TownRoadGraph`/`TownBlockParcelizer`/`TownGrowthParams` and repoint `CityGenerateMenu`; add optional `GridMap` rasterization bridge if gameplay pathfinding must consume the new town (see Risks). **Dependencies:** C1.

---

# Verification & Testing

## Determinism (critical)
- Generate seed X twice → identical `TownModel` (assert equal counts and, for a sample, identical vertex positions within epsilon). Add an EditMode test `TownDeterminismTests`.
- Change one stage's RNG usage → only that stage's output changes (stage isolation via `Fork`).

## Geometry correctness (EditMode unit tests, `Geo2Tests`)
- `PolygonArea` sign/convention; `PointInPolygon` on known cases; `InsetPolygon` shrinks area and stays inside; `SplitPolygonByLine` areas sum to original; `SmoothCatmullRom` passes through/near control points.

## Structural invariants (runtime asserts + gizmo review)
- Every `Plot.hasFrontage == true` (or is explicitly a courtyard/garden).
- Road graph is connected; **every portal and the landmark are reachable** from the plaza (flood‑fill test).
- No plot/building polygon overlaps a street corridor; footprints lie within their plot inset.
- Human scale: all street widths, plot frontages, plaza sizes within `TownProfile` metric bounds (validation log is empty).

## Believability / manual checks (gizmo scene)
- Streets read as curved/irregular with varied width; junctions predominantly T‑shaped.
- Rich districts: larger blocks/plots, wider streets, gardens. Poor districts: dense, narrow alleys, inner courtyards, winding passages, some dead‑ends + shortcuts.
- Landmark visually prominent (open plaza + arterials converging + view cone unobstructed).
- Small plazas, wells, service alleys, irregular block shapes present.
- Sweep 10+ seeds; confirm variety without failures/log warnings.

## Performance (runtime target)
- Medium town generates within a frame‑budget acceptable for a load screen; `GenerateAsync` time‑sliced path shows no long hitch (profile with the Profiler). Confirm no per‑frame GC after generation completes.

## Build
- Project compiles with no errors (`Unity.GetConsoleLogs` clean); EditMode tests green.

---

# Risks & Open Points
1. **District clipping vs curved arterials** — highest technical risk. Mitigation: straight‑segment approximation of arterial corridors for clipping (smooth polyline kept only for rendering). Documented fallback in Key Asset & Context.
2. **"Replace" vs live gameplay** — existing `GridPlayerController`/`GridPathfinder` consume the raster `GridMap`. The new system is pure vector; **gameplay pathfinding integration is deferred** to Milestone C2 (optional `GridMap` rasterization bridge). Until then the old raster generator stays functional behind its current menu. Confirm this staging is acceptable.
3. **Scope of Milestone A** — first deliverable is intentionally geometry + gizmos only; no prefab spawning. B/C are separately approved.
