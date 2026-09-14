# Hydrology Generation — Design & Implementation Plan

**Status:** Draft
**Date:** 2026-09-03 (terrain integration rewritten 2026-09-13 for the Unity-native terrain bake)
**Applies to:** Track W (World Gen), first inland-water system after Overworld V0
**Related:** [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md), [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md), [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md), [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md), [AGENTS.md](../../../AGENTS.md)

---

## Reading this document

Every claim is tagged so a later reader can tell evidence from judgement:

- **[FACT]** — read out of the repository (file and symbol named). The code is authoritative.
- **[CONCLUSION]** — follows from the facts above it.
- **[ASSUMPTION]** — believed but not yet measured. Each one names how it will be checked.
- **[OPEN]** — deliberately unresolved. Not silently decided.

---

## 1. Problem statement

The overworld has no inland water. `WorldScale.SeaLevelMeters = 24` and one WaterWorks
`WorldWater` plane is the whole of V0 hydrology **[FACT: `WaterZoneMenu.EnsureOceanOcWPlane`]**.

The world therefore reads as one undifferentiated landmass. `WORLD_GENERATION_PIPELINE.md` states
the target traversal as *forest → hills → valley → river → bridge → village → open plain → old
forest → mountain pass → lake → city*; none of the water half of that exists.

This plan designs the first real procedural hydrology: rivers, tributaries, lakes, drainage basins,
sources, downstream flow, ocean outlets and waterfall candidates — geographic, deterministic and
extensible.

The quality bar is fixed by `14 World Generation Roadmap.md` and repeated here because it is the
actual test:

```
terrain -> drainage -> valley -> river channel -> lake/depression relationship -> water
```

A blue spline over unchanged terrain is not hydrology.

**Hard world rule:** every generated river system must eventually reach the ocean. A river need not
flow into a lake, and a lake need not exist for every river.

---

## 2. Current architecture audit

### 2.1 Generation orchestration

**[FACT]** `OverworldPipeline.GenerateAllRoutine(seed, settings)` (`Assets/Scripts/Editor/OverworldPipeline.cs`)
runs the ordered steps `Surface -> Mountains -> Markers -> Settlements -> Roads -> Player`, then always
`WaterZoneMenu.EnsureOceanOcWPlane()`. Steps are gated by `WorldGenSettings` bool flags and named by
`enum OverworldPipelineStepId` (`Assets/Scripts/World/WorldGenSettings.cs`).

**[FACT]** Surface and Mountains are **one** terrain bake, selected by
`TerrainBakeFlags.IncludeMountains`; `OverworldTerrainMenu.GenerateSurfaceRoutine` ->
`OverworldTerrainBake.Bake` creates the 2x2 terrain grid, evaluates TerrainGen onto one world lattice
and writes heights, surface weights, trees and ground cover
([OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md)).

**[FACT]** `WorldDefinition` is a plain C# class — explicitly *not* a ScriptableObject — held by the
`OverworldWorldState` MonoBehaviour on a non-serialized property, so it does not survive domain
reload. It already aggregates `Pois` and `Roads` through `SetPois` / `SetRoads`.

**[FACT]** `WorldSeed : IWorldSeed` (`Assets/Scripts/World/Seed/`) provides
`Fork(streamId)` / `Fork(streamId, a, b)` over `System.Random`, mixing the stream id into the seed.
Streams already in use include `terrain.coast`, `terrain.massif`, `terrain.surface.*`,
`terrain.vegetation.*`, `poi`, `settlement_pose_{id}`, `player.spawn`.

**[FACT]** `OverworldGenTiming.Log(step, ms, detail)` is the house timing report, format
`[Gen] step={step} ms={ms} | {detail}`.

### 2.2 World scale and terrain access

**[FACT]** `WorldScale`: `OverworldTileMeters = 1400`, `OverworldTileCountX/Z = 2`, world
2800 x 2800 m, `ChunkSizeMeters = 64`, `TerrainHeightBudgetMeters = 700`, `SeaLevelMeters = 24`,
`IslandOceanMarginMeters = 280`.

**[FACT]** `OverworldTerrainGrid.DefaultHeightmapResolution = 513` per 1400 m tile
-> **2.734 m per heightmap pixel**; the four tiles share one 1025 x 1025 world lattice. Tiles are
named `OverworldTerrain_{tx}_{tz}` at world position `(tx*1400, 0, tz*1400)`.

**[FACT]** `OverworldTerrainAccess` resolves the grid: `FindTerrains()` (sorted by name — a
deliberate determinism fix), `FindTileAt(x, z)`, `TrySampleHeightWorld(x, z, out y)`.

**[FACT]** `RoadNetworkMenu` learned the hard way that per-sample terrain lookups are unusable —
one measured plan made 69 230 938 calls to `SampleHeightWorld`, which runs `FindObjectsByType<Terrain>`
each time. It now precomputes a height table. Any bulk hydrology sampling must read
`terrainData.GetHeights` once per tile.

### 2.3 Water today

**[FACT]** `IWaterSampler.TryGetWaterLevel(worldX, worldZ, out waterY)` is the single water contract.
`SeaLevelOceanSampler` implements it for the ocean.

**[FACT]** `CompositeWaterSampler` (first sampler that reports water wins) and `WaterProximity`
(`IsUnderOrTooCloseToWater`, `SampleWetness01`) compile but have **zero production callers** — they
were built for the removed inland system.

**[FACT — pre-existing defect]** `SeaLevelOceanSampler.FromTerrain(terrain)` closes over a single
`Terrain`, and every production caller passes `OverworldTerrainAccess.FindTerrain()`, i.e. the
south-west tile. Water queries over the other three tiles read that tile's clamped edge height.
This is the last live tile-(0,0) assumption in the project.

### 2.4 Where the carve enters the terrain (the decisive part)

**[FACT]** The terrain bake evaluates TerrainGen onto `OverworldTerrainField`, a world-spanning vertex
lattice, and derives slope, convexity and drainage from it before anything is written to Unity.
`OverworldTerrainField.ApplyCarve(Func<int, int, float>)` lowers requested vertices by a depth in metres
and invalidates the derived quantities, so the surface weights, trees and ground cover are all derived
from the carved relief.

**[FACT]** `HydrologyBake.ApplyPendingCarve(field)` rasterises the pending `HydrologyDefinition` with
`HydrologyCarveField.Rasterize` over the lattice rectangle expanded by half a step, so every pixel
centre is a lattice vertex, and passes it to `ApplyCarve`. It is called by `OverworldTerrainBake.Bake`
after TerrainGen has been evaluated and before heights are written.

**[CONCLUSION]** The carve is one-way: TerrainGen -> lattice -> carve -> `TerrainData`. Nothing reads
`TerrainData` back into TerrainGen, so no hydrology setting can move a mountain.

### 2.5 Hard constraints

**[FACT]** The URP TerrainLit budget is **8** terrain layers and it is full: 5 canonical
(`OverworldTerrainLayers.SurfaceLayer`) plus 3 road layers. `TerrainLayerBudgetTests` asserts
`MaxRenderedTerrainLayers = 8`.

---

## 3. Existing systems reused

| System | Reused as |
|---|---|
| `WorldSeed.Fork` | Dedicated `"hydrology"` stream |
| `WorldScale` | All metres; no new scale constants beyond hydrology's own thresholds |
| `OverworldTerrainAccess` | Grid-aware tile resolution; `FindTerrains()` ordering |
| `WorldDefinition` / `OverworldWorldState` | Aggregate snapshot, via a new `SetHydrology` |
| `IWaterSampler` + `CompositeWaterSampler` | The one water contract; inland water composes into it |
| `OverworldGenTiming` | Timing report |
| `OverworldTerrainField.ApplyCarve` | The carve lowers the terrain lattice inside the terrain bake |
| `OverworldWater.mat`, `OverworldWater` root | Inland water renders with the ocean's material |
| `Assets/Scripts/Editor/Tests/` NUnit style | EditMode tests, no asmdef, synthetic terrain fakes |

---

## 4. Systems intentionally NOT reused

| System | Why not |
|---|---|
| `LakeDefinition`, `RiverTracer`, `WaterZone_*` | Deleted in `a957403`; `14 World Generation Roadmap.md` forbids resurrection. Not an architectural foundation. |
| `TerrainDeformer` / `RoadCorridorDeformer` / `RoadTerrainCarver` | These *do* write the heightmap post-bake, but only as the documented road exception. Hydrology carves the lattice inside the terrain bake instead, before the surface is derived. |
| `RoadPathSimplifier` | Road-specific turn-radius and grade rules; rivers have different geometry. Hydrology gets its own light smoothing. |
| `OverworldTerrainSampler.SampleMoisture01` | Unseeded Perlin. Hydrology will eventually be a better moisture source, but that is out of scope (section 18). |
| A new terrain layer for the riverbed | Layer budget is full (2.5). |

---

## 5. Hydrology data model

Namespace `GameDev.World.Hydrology`, folder `Assets/Scripts/World/Hydrology/`. Pure C# — no
`UnityEngine` dependency beyond `Vector2` / `Vector3` — so it is testable without a scene.

```
HydrologyDefinition
 |-- IReadOnlyList<RiverReach>         Reaches     (ordered by ReachId)
 |-- IReadOnlyList<LakeBasin>          Lakes       (ordered by LakeId)
 |-- IReadOnlyList<WaterfallCandidate> Waterfalls  (ordered by reach, then node index)
 `-- HydrologyStats                    Stats

RiverReach
 |-- ReachId  Id                      stable, derived from the outlet plus branch index
 |-- ReachId? DownstreamId            null only for an ocean outlet
 |-- IReadOnlyList<RiverNode> Nodes   upstream -> downstream
 |-- int      StrahlerOrder
 |-- float    ContributingAreaSqm     at the downstream end
 |-- float    WidthMeters
 `-- ReachTermination                 Ocean | Lake | Confluence

RiverNode
 |-- Vector2 PositionXZ
 |-- float   BedElevation             planned channel floor after carving
 |-- float   WaterElevation           BedElevation plus flow depth
 |-- float   CumulativeDistance
 `-- float   ContributingAreaSqm

LakeBasin
 |-- LakeId    Id
 |-- Vector2[] Boundary               shoreline polygon at SurfaceElevation
 |-- float     SurfaceElevation       = spill elevation
 |-- float     MaxDepth, AreaSqm
 |-- ReachId?  OutletReach
 `-- ReachId[] InletReaches

WaterfallCandidate
 |-- ReachId Reach, int NodeIndex
 `-- float DropMeters, RunMeters, Slope
```

**[CONCLUSION]** `RiverNode` carries position, bed elevation, cumulative distance and contributing
area, and reaches carry upstream/downstream links. That is exactly the set a waterfall, a bridge
site, a moisture field or a river-mouth POI needs later — the model does not make any of them
impossible, and none of them is implemented now.

`HydrologySettings` holds every threshold: channel contributing area, minimum lake area and depth,
valley/channel width and depth curves, waterfall drop and run thresholds, maximum carve depth.

`WorldDefinition` gains `Hydrology` plus `SetHydrology(...)`, matching `SetPois` / `SetRoads`.

---

## 6. Algorithm choice and rejected alternatives

**Chosen: priority-flood depression filling with epsilon slope, then D8 flow direction, then flow
accumulation.**

Priority-flood (Barnes, Lehman & Mulla 2014) is initialised from the ocean cells and floods inward
with a monotonic priority queue, raising each filled cell to `max(own, neighbour + eps)`.

**[CONCLUSION]** This makes the working surface **monotonically descending to the ocean by
construction**, which turns four of the required guarantees into structural properties rather than
things the code hopes for:

- every river reaches the ocean — there is nowhere else for a descending path to go;
- no cycles — D8 on a strictly descending surface cannot revisit a cell;
- no river terminates inland;
- no river crosses a ridgeline — flow only ever goes downhill on the *original* relief, because eps
  is microscopic against real relief.

**[CONCLUSION]** It also produces the lakes for free: the cells where `filled > original + tolerance`
are exactly the submerged ones, their connected components are the lakes, and the flat fill level is
the spill elevation. A lake is *derived from the terrain*, never a point with a radius.

Evaluated and rejected:

| Alternative | Verdict |
|---|---|
| D-infinity / multiple-flow-direction | Non-tree topology; polyline and tributary extraction get materially harder, and at 2800 m with a 2.7 m grid the visual gain is nil. **Rejected on complexity.** |
| Depression breaching | Cuts an outlet through the basin rim, i.e. through a watershed. Directly violates "no rivers crossing ridgelines". **Rejected on correctness.** |
| Contour / valley tracing | Produces plausible-looking channels unrelated to actual drainage. Fails the "geographic, not decorative" bar. **Rejected.** |
| Graph-based downhill drainage over sparse nodes | Same result as D8 with extra machinery and worse lake handling. **Rejected on complexity.** |
| Hydraulic / fluid simulation | Explicitly out of scope and unnecessary. **Rejected.** |

Channel initiation uses a **contributing-area** threshold in square metres, not a cell count, so the
network does not change shape if the analysis grid resolution is ever changed.

River hierarchy is emergent: Strahler order from the drainage tree, width and carve depth from
contributing area. There are no hand-authored "main river" / "branch river" categories.

---

## 7. Pipeline order

```
PASS A - terrain bake                   (TerrainGen relief, exactly as without hydrology)
      v terrainData.GetHeights per tile, assembled into one world grid (HydrologyTerrainGrid)
ANALYSIS - pure C#
      v ocean mask (h <= SeaLevelMeters)
      v priority-flood depression fill (eps), seeded from ocean cells
      v D8 flow directions on the filled surface
      v flow accumulation (cells processed in descending filled elevation)
      v basins, lakes (filled - original > tolerance), river network, Strahler order
      v waterfall candidates (elevation profile of the ORIGINAL relief along each reach)
      v VALIDATION (section 16) - failure reports, never silently repairs
PASS B - terrain bake with the pending plan
      v TerrainGen field -> lattice -> HydrologyBake.ApplyPendingCarve (metres, 2.734 m/px)
      v heights, surface weights, trees and ground cover from the carved lattice
      v HydrologyBake.VerifyCarve: pass A vs pass B against the carve field
WATER RENDERING - river ribbons + lake meshes on OverworldWater.mat
```

### Why two passes, and not one or three

**[CONCLUSION]** Drainage is a global property of the *finished* macro relief: where channels
converge depends on the island silhouette and every massif together. The analysis therefore runs on
a finished bake, and its result re-enters the terrain bake as a carve. That is two passes — not a
theoretical nicety, a consequence of where the information lives.

A third pass is not added. The carve is deterministic and bounded; whether it disturbed the drainage
enough to matter is answered by validating against the final terrain, not by iterating.

### Why the carve is not fed back into TerrainGen

**[CONCLUSION]** TerrainGen is the terrain authority and is frozen by stage. Reading pass A's
`TerrainData` back into it, or making TerrainGen aware of hydrology, would create a cycle in which a
river setting could move the relief it was derived from. The carve is applied downstream of TerrainGen
and upstream of `TerrainData` only.

---

## 8. Terrain integration strategy

| Property | Value | Reason |
|---|---|---|
| Where | `OverworldTerrainBake.Bake`, after TerrainGen is evaluated, before heights are written | Surface weights and vegetation see the carved relief |
| Grid | the terrain lattice, 1025 x 1025 at 2.734 m | Exact parity with the tile heightmaps; tile seams stay exact |
| Raster | `HydrologyCarveField.Rasterize` over the lattice rect expanded by half a step | Pixel centres are lattice vertices — no resampling |
| Units | metres, clamped to `HydrologySettings.MaxCarveDepthMeters` (45 m) | No normalisation step |
| Ground reference | `HydrologyGrid.SampleHeightWorld` of pass A | The carve profile is measured against the relief the analysis saw |
| Scope | heights only; weights and vegetation follow because they are derived afterwards | No extra layer (budget full), no second vegetation writer |

**No post-bake repair pass is added.** The terrain bake remains the only writer of heights, weights,
trees and ground cover; hydrology only supplies a depth field to it.

### Carve profile

Per pixel, from the river network in world space:

```
d          = distance to the nearest reach centreline
valley(d)  = smoothstep taper from valleyHalfWidth to 0, scaled by valleyDepth(order)
channel(d) = smoothstep taper from channelHalfWidth to 0, scaled by channelDepth(order)
lake(p)    = surfaceElevation - targetLakeFloor, inside the lake polygon
carve      = min(max(valley + channel, lake), MaxCarveDepthMeters)     metres
```

Valley and channel widths and depths come from contributing area, so a first-order headwater gets a
shallow notch and the main stem gets a real valley.

Two details the implementation added that the first draft of this plan did not have, both because
they turned out to be load-bearing:

- **Deepest wins, never the sum.** Overlapping contributions are combined with `max`. Adding them
  would gouge a pit at every confluence, which is exactly where the network is most visible.
- **Lake basins are excluded from the raster.** A lake's surface was measured from the un-carved
  basin, so letting a valley shoulder reach in would move the ground out from under the water plane
  the same analysis produced.

**[ASSUMPTION]** A three-heightmap-pixel channel (~8.2 m) reads as a river at player scale.
*Checked by:* a bake and a visual review; the carve widths are `HydrologySettings` values, tunable
without touching the algorithm.

---

## 9. Lake strategy

Lakes are **not** placed. They are the connected components of `filled - original > tolerance` after
priority-flood, filtered by `MinLakeAreaSqm` and `MinLakeDepthMeters`.

Consequences, all of them terrain-derived:

- surface elevation = the spill elevation the flood computed;
- every lake has exactly one outlet (its spill point) and zero or more inlets;
- lake shape is the basin's shape, not a radius;
- a lake exists only where the terrain actually holds water.

**Terminal lakes are not generated.** Priority-flood raises every basin until it spills, so a basin
that would be endorheic instead becomes a lake with an outlet that continues to the ocean. This is
the deterministic depression policy, chosen over breaching (cuts watersheds), discarding
(loses geography) and outlet routing (needs an arbitrary destination):

> **Depression policy: fill, then spill.** Every closed depression becomes a lake whose surface sits
> at its spill elevation, and its outlet reach continues downstream. There is no random fallback and
> no endorheic basin.

**[CONCLUSION]** This is what makes the ocean-outlet invariant structural: with no terminal basins,
every drainage path ends in the sea.

Two rules the implementation added to keep a lake a lake:

- **A lake's outflow is always drawn**, whatever its contributing area, and the system containing it
  is never pruned. A basin that passed the area and depth filters is a real body of water, and
  dropping its outlet would leave it draining into nothing.
- **The outlet sill is not carved.** The carve tapers in over ~220 m below the spill, comfortably
  more than the widest valley shoulder. Cutting a full valley through the spill would drain the
  basin the analysis just measured — which is also why real lake outlets are sills.

One correctness detail worth stating plainly: **"below sea level" is not the same as "the ocean".**
The ocean mask is the submerged cells reachable from the world border through other submerged cells,
so a closed inland pit whose floor happens to sit under 24 m is a depression like any other. Treating
it as sea would hand the network a false outlet in the middle of the island and silently satisfy the
one invariant this system exists to guarantee.

---

## 10. River network strategy

One shared drainage network, not N independent rivers.

1. Flow accumulation over the whole grid.
2. Cells above `ChannelInitiationAreaSqm` form the channel raster.
3. Channel cells are walked downstream along D8 into reaches, split at confluences, lake inlets,
   lake outlets and ocean outlets.
4. Strahler order from the reach tree; width and carve depth follow contributing area.
5. Centrelines are resampled to a fixed spacing and lightly smoothed, with the constraint that a
   smoothed node may not rise above its upstream neighbour.

Two separate controls, because they answer different questions:

- **`ChannelInitiationAreaSqm`** decides how branched each system is. Raising it removes the smallest
  headwaters first; it cannot produce parallel duplicate networks, because D8 gives each cell exactly
  one downstream neighbour.
- **`MinRiverSystemAreaSqm` / `MinRiverSystemFractionOfLargest`** decide how many systems there are.
  Without them an island grows a watercourse at every coastal gully — the «dozens of parallel rivers»
  failure — and a measured run with the initiation threshold alone produced 35 outlets on a test
  island. The judgement is made per **system** and drops the whole tree above an outlet, never part
  of one, so nothing is left ending inland. The relative half is what keeps the count stable when the
  island's size changes between seeds.

---

## 11. Ocean outlet invariant

**Invariant:** for every reach, following `DownstreamId` terminates at a reach whose termination is
`Ocean`.

Guaranteed by construction (section 6) and **verified explicitly** by `HydrologyValidator`, which
walks every reach downstream with a visited set. A cycle, a null downstream link on a non-ocean
reach, or a walk exceeding the reach count is a hard failure.

On failure the generator reports the reason and emits **zero rivers**. It never invents a river to
meet a quota, and there is no hidden random fallback. Fewer valid rivers beat invalid geography.

---

## 12. Water rendering strategy

Geographic truth (`HydrologyDefinition`) and visual representation are separate. Rendering never
becomes the source of geometry.

- One root `OverworldWater/InlandWater`, cleared and rebuilt as a unit like every other generated root.
- **One mesh per river system**, not per segment: a ribbon swept along the reach polylines, width
  from Strahler order. Expected count: single digits.
- **One mesh per lake**: a polygon at `SurfaceElevation`.
- Material: the existing `Assets/Settings/Water/OverworldWater.mat`. No new material, no new shader.
- Surface elevations come from the **planned** bed, which the carve produced, so water cannot float
  above or sink below the terrain it was cut for.

No GameObject per segment, no serialized water hierarchy in the canonical scene.

---

## 13. Road integration seam

The smallest clean seam, because the interface already exists:

```
HydrologyDefinition -> InlandWaterSampler : IWaterSampler
                    -> CompositeWaterSampler(ocean, inland)   (already written, previously uncalled)
                    -> RoadCostSampler / SettlementSiteValidator / SegmentCrossesWater / CanBridgeSpan
```

`RoadNetworkMenu` and `SettlementOverworldMenu` swap their bare `SeaLevelOceanSampler` for the
composite. Nothing else in the road system changes; `CanBridgeSpan` stays as it is; roads stay
disabled by default (`WorldGenSettings.Default.RunRoads = false`).

**Dependency direction is Hydrology -> roads/settlements/POIs.** Roads do not invent water.

---

## 14. Determinism strategy

- Own seed stream: `worldSeed.Fork("hydrology")`.
- **Re-seeding hydrology cannot move the mountains — structurally.** Pass A does not know hydrology
  exists; the hydrology seed is only read after pass A has finished.
- The analysis itself is almost entirely deterministic-by-construction: priority-flood ties are
  broken by insertion order, D8 ties by a fixed neighbour order, accumulation by descending filled
  elevation with an index tie-break. RNG is used only where a genuine arbitrary choice remains.
- All output collections are sorted by stable ids before they leave the planner.
- No `UnityEngine.Random` anywhere (`AGENTS.md` Unity C# RNG rule).

The seed is used in exactly one place, and deliberately so: the phase of a low-frequency meander
offset applied to the resampled centrelines. A D8 path is a staircase on a lattice and smoothing
alone leaves it reading as an axis-aligned zigzag; the meander phase is a genuinely arbitrary choice,
which is what a seed is for. It is capped at a fraction of the channel half width and clamped to the
world, so it can never carry water out of the valley the same numbers carved. Everything else about
the network is a pure function of the terrain.

---

## 15. Performance budget

| Quantity | Value |
|---|---|
| Analysis grid | 1025 x 1025 at 2.734 m (the terrain's own sampling, tiles sharing edges) |
| Cells | 1 050 625 |
| Working memory | ~18 MB (height, filled, accumulation, direction, labels) |
| Carve field | 1025^2 floats = 4 MB |

**[ASSUMPTION]** depression fill < 1.5 s, flow routing < 0.3 s, network build < 0.2 s, rasterisation
< 0.3 s, total hydrology planning < 2.5 s. *Checked by:* `OverworldGenTiming` sub-steps
`hydrology.sample`, `hydrology.fill`, `hydrology.flow`, `hydrology.network`, `hydrology.raster`.

**[FACT]** Pass B costs the same as pass A plus the rasterisation: the terrain bake does not cache,
and TerrainGen evaluation dominates it (`OVERWORLD_TECH_DEBT.md` item 16).

**[OPEN]** If measured planning exceeds the budget, the fallback is a 513^2 analysis grid at
5.47 m/px, with the carve still rasterised at 2.734 m/px. Not decided in advance — measure first.

---

## 16. Validation rules

`HydrologyValidator` runs before the definition is published. Each rule is also an EditMode test.

1. Every reach reaches the ocean by following `DownstreamId`.
2. No node is higher than its upstream neighbour beyond an epsilon tolerance.
3. No cycles; the reach graph is a forest of trees rooted at ocean outlets.
4. No duplicate reaches and no duplicate node positions within a reach.
5. All coordinates inside the world rect; no NaN or infinity.
6. `LakeBasin.SurfaceElevation` is at or above the basin floor and at or below the spill elevation;
   boundary is a closed polygon with at least 3 vertices.
7. Output ordering is deterministic (sorted by id, never by hash iteration order).
8. **Carve check.** `HydrologyBake.VerifyCarve` compares the pass A and pass B heightmaps against the
   carve field: the delivered cut must match the requested cut, and the terrain must not move where
   nothing was requested. Either disagreement above 1 m is reported loudly, not silently absorbed.
9. Failures are reported, never hidden. A seed that cannot produce a valid network yields zero
   rivers plus a diagnostic naming the rule that failed.

---

## 17. MVP scope

Approved by the project owner on 2026-09-03: analysis, data model, terrain carving, water rendering,
the `IWaterSampler` seam, diagnostics, gizmos and EditMode tests, with **no riverbed dressing in V1**
and the 8-layer budget untouched.

The first carve was built inside the removed terrain pipeline and never landed: adding the carve moved
the terrain by tens of metres even with a zero carve. Since 2026-09-13 the carve is a depth field applied
to the terrain lattice by the Unity-native terrain bake (section 8), and it lands as planned.

Measured on seed 110201 with TerrainGen's relief (fresh batch process, 2026-09-13):

- 8 river systems, 8 reaches, 8 sources, 8 ocean outlets, 0 tributaries, 0 lakes, 0 waterfall
  candidates, 2.05 km of river; planning ~0.3 s (depression fill ~0.25 s, flow ~27 ms, network ~12 ms);
- carve: 21 322 lattice vertices lowered, deepest 9.4 m requested and 9.4 m delivered; worst movement
  outside the carve 0.02 m; worst disagreement inside it 0.02 m — 16-bit height quantisation;
- water meshes: 8 river ribbons;
- every reach validated to reach the ocean, no cycles, no uphill flow.

The network is smaller than the one measured on the previous relief (6 systems, 26 reaches, 6.03 km):
the island relief is now TerrainGen's, and the thresholds are unchanged `HydrologySettings` values.

`RunHydrology` stays **off by default** until the carved result has been reviewed in the scene.

---

## 18. Future extensions

| Extension | Route |
|---|---|
| Riverbed dressing (no trees in the channel, sand bed) | A carve-depth input to `TerrainVegetationRules` / `TerrainSurfaceRules` in the terrain bake, which already holds the carve field when it derives the surface. The material half needs a terrain-layer budget decision first. |
| Waterfall rendering | Data already present in `WaterfallCandidate`; only VFX and placement remain. |
| Bridges | `CanBridgeSpan` already exists and currently returns false; hydrology gives it real crossings. |
| Distance-to-water / moisture | `HydrologyDefinition` becomes the source; `OverworldTerrainSampler.SampleMoisture01` currently discards its seed and is the natural replacement point. |
| River-mouth and ford POIs | `RiverNode` already carries everything needed. |
| Streaming | Reaches are chunk-addressable via `ChunkCoord`. |

---

## 19. Risks and unresolved questions

- **[RESOLVED 2026-09-13] `VerifyCarve` sampled the carve half a cell off the lattice.** It rasterised
  the expected carve over the heightmap rectangle while the bake rasterises over the lattice rectangle
  expanded by half a step, which read as up to 0.88 m of "disagreement" on a steep bank. Both now use the
  same raster; the measured disagreement is 0.02 m.
- **[LIMITATION] Vegetation in the channel is not explicitly excluded.** Trees and ground cover are
  derived from the carved lattice, so steeper banks and higher drainage (soil) reduce them, but nothing
  removes them from the bed itself. The V2 route is in section 18.
- **[LIMITATION] No riverbed material.** The 8-layer budget is full; a sand bed would displace an
  existing layer and change material architecture. Out of scope.
- **[LIMITATION] Minimum channel width ~8.2 m** — three pixels of a 513-resolution heightmap over a
  1400 m tile. Streams narrower than that cannot exist as terrain, only as visuals. This is a
  property of the terrain resolution, not of the hydrology system.
- **[OPEN] `SeaLevelOceanSampler` tile-(0,0) bug** (2.3). The composite sampler does not fix it. It
  is a pre-existing defect and fixing it is a separate decision; it is **not** in this scope.
- **[OPEN] Analysis grid resolution** — 1025^2 pending measurement (section 15).
- **[ASSUMPTION] Island relief supports interesting drainage.** The island is ~2240 m across with
  ~120 m of relief above sea level, a ~5 % mean gradient. *Checked by:* the first diagnostic run's
  river count and total length; thresholds are settings, not constants.

---

## 20. Explicit non-goals

Not in this work, at all:

- real-time fluid or hydraulic simulation;
- sediment transport;
- waterfall rendering (only candidate derivation);
- bridges;
- swimming, fishing, water physics, water creatures;
- biome or moisture rewrite;
- new terrain layers, new materials, material overhaul;
- vegetation system changes;
- streaming;
- re-enabling roads globally;
- changing TerrainGen relief, terrain surface rules or terrain material architecture;
- any post-bake terrain repair or a second height writer — the carve is a depth field the terrain bake applies;
- resurrecting `LakeDefinition`, `RiverTracer` or `WaterZone_*`.
