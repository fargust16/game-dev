# Overworld tech debt

Known defects with measurements, so the next session starts from numbers rather than a hunch.
Terrain-specific traps live in [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md); this file is
for work that is understood but not done.

---

## 1. Road planning takes 141 s — blocking

`[Gen] step=roads.plan ms=140970 | grid 110x110 cells, 32 nodes`

The whole roads step is 141 s of a 149 s `Generate All`. Every other part of it is noise:

| Sub-step | Time |
|---|---|
| `roads.costfield` | 301 ms |
| **`roads.plan`** | **140 970 ms** |
| `roads.profiles` | 3 ms |
| `roads.heights` | 20 ms |
| `roads.paint` | 26 ms |

**This is not a workload problem.** 110×110 is 12 100 cells; a grid that size with 32 nodes should
plan in milliseconds. The cost is inside `LandscapePlanner.PlanRoads` →
`RoadNetworkBuilder.BuildHierarchicalNetwork` / `EnsureMinimumConnectivity`.

It scaled far worse than area: widening the planning region from one tile to the world (2× per axis,
4× area) took the step from 12.6 s to 141 s — roughly 11×, not 4×. That superlinearity is the thing
to find.

Where to look first:
- how many searches `BuildHierarchicalNetwork` runs — all-pairs over 32 nodes is 496 searches, and
  if each is a full-grid Dijkstra without an early exit that alone is 6M node pops per pass;
- whether `EnsureMinimumConnectivity` re-plans from scratch per node instead of reusing the field;
- whether `CostField.EvaluateAtTerrain` is called per neighbour visit rather than cached per cell —
  it closes over `sampler.SampleWorldHeight`, so a cache miss is a terrain sample.

Sub-step timers are already in place (`RoadNetworkMenu`, `RoadCorridorDeformer`); add one more
inside `BuildHierarchicalNetwork` per search and the answer should fall out in one run.

---

## 2. Four systems assume a single terrain tile

`OverworldTerrainAccess.FindTerrain()` returns **one** terrain. These call it and then treat its
bounds as the world:

- `RoadNetworkMenu.cs:48` — road carving and painting land on one tile only, so 3 of 4 tiles get no
  road surface at all;
- `PoiMarkerMenu.cs:100` — `[POI] placed=5 (markers=32) skipped: noSurface=27`, because 27 markers
  fall outside that tile;
- `SettlementOverworldMenu.cs:22`;
- `OverworldWorldPlan.cs:56`.

This was latent until the planning region was widened to the full world (see 3). The fix is to
sample against all four tiles — `OverworldTerrainAccess.FindTerrains()` already exists — or to add a
world-space sampler that picks the tile containing a point.

---

## 3. Planning region was one tile out of four — fixed, keep in mind

`OverworldWorldPlan.BuildAndStorePois` and `RoadNetworkMenu` both derived their region from
`WorldScale.OverworldTileMeters` (1400 m) while the world is 2×2 tiles (2800 m), so POIs were only
ever placed in the bottom-left quadrant. The island sits at the world centre, so on a seed whose dry
land missed that quadrant the run produced zero markers, no roads and no player.

Both now use `WorldScale.WorldExtentXMeters/ZMeters`. The change is correct and necessary, and it is
also what exposed items 1 and 2 — 4× the area for the planner, and markers outside the single tile.

---

## 4. Vista's detail-density writer does not land in the editor

`VistaDetailDensityRelay` writes the density map Vista produces, because Vista's own progressive
writer loses it. Fully documented in the playbook (§2.8) with the measurements that justify it.

It is a vendor workaround, not a design: placement is entirely the terrain graph's. Delete the file
if a Vista update fixes `TerrainTile.PopulateDetailDensityProgressive`, and re-check by toggling
`GameDev/Overworld/Vista/Detail Density Relay (workaround)` off and looking at `detailInstances`.

Diagnostics that pin it, both off by default: `Probe Detail Density Maps`, `Dump Vegetation State`.

---

## 5. `NullReferenceException` at `UnityEditor.Graphs.Edge.WakeUp` on entering play mode

Still reproducing. Two facts stand against each other and neither has been made to fit:

- **Against a broken asset.** All 77 `.controller` files were scanned for transitions whose
  `m_DstState` and `m_DstStateMachine` are both `{fileID: 0}` — none. `OverworldPlayerAnimator`
  itself has every state present and every transition resolved.
- **For it being the editor window.** `UnityEditor.Graphs` is the Animator window's graph view, and
  `UserSettings/Layouts/default-6000.dwlt` contains `AnimatorControllerTool`. That window
  deserializes its graph on every domain reload, which is exactly when this fires.
- **Against that being the whole story.** It was not happening before this session's changes.

Next step, in order: close the Animator window tab and reproduce; if it persists, bisect by
reverting `OverworldPlayerMenu`'s controller changes; if it still persists, the cause is outside
those files. Do not fix this by guessing again — three separate explanations have already failed.

---

## 6. Rectangular patch of ground with no vegetation

Reported and unexplained. What is ruled out: nothing in our code clears details or trees after the
bake — every post-bake terrain write is `SetHeights` or `SetAlphamaps`, and no code sets terrain
holes.

Leading hypothesis, untested: the shape matches `SettlementDustPaint`, which paints rectangular
street/pad/garden zones, and settlement sites are chosen on flat ground. The vegetation mask is the
grass texture weight, which is weakest exactly where soil and sand dominate — flat low ground. If so
the two systems simply prefer the same terrain from opposite directions and it is not an artifact.

To settle it: `Dump Layer Weights` exports one PNG per layer; look for a hole in the grass weight at
that location.

---

## 7. Smaller items

- `PoiMarkerMenu` doc comments still reference the deleted `VegetationScatterMenu`.
- `Species.Orchard` uses the apple prefab only; pears and plums are unused because Vista Personal
  ignores `prefabVariants` (playbook §5). They would need their own species entries.
- Vegetation Pass phases 4–6 from the original spec are not started: flowers and small plants,
  further environmental dressing, LOD/culling budgets.
- URP shadow distance is at its default, so the 20 m canopies only shade nearby ground.
