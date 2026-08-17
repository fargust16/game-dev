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

## 7. The overworld scene is half generated and versions like it

`Assets/Scenes/Overworld_Generator.unity` produced **416 386 changed lines** in one commit. Every
`Generate All` rewrites it, so the history records bakes rather than decisions and any merge on the
scene is hopeless.

It cannot simply be ignored, because it is two things in one file:

| Authored — must survive a clone | Generated — rewritten every bake |
|---|---|
| `VistaManager` and its settings | terrain tiles and their `TerrainData` |
| directional light, sky, camera rig | spawned LPB instances |
| `WorldGenSettings` / seed state | POI markers, settlement greybox, roads, bridges |
| ocean plane | the player object |

The fix is to separate them: move the authored scaffolding into a prefab (or a small additive scene)
that is versioned, and let `Generate All` build everything else into a scene that is not. The
generated side is already reproducible — the seed is logged with every run and the terrain data,
graph clones and baked lighting are ignored — so the only thing standing in the way is that the
scaffolding has never been extracted.

Until that happens, treat scene diffs as noise and never resolve a conflict in them by hand:
regenerate from the seed instead.

---

## 8. Road softening emits points it never validated — failing test, mechanism known

`RoadPathSimplifierTests.SoftenNatural_CannotCrossImpassableBand` fails: a softened road segment
crosses a cell whose cost is `+Infinity`. This predates the stabilization pass — it is not caused by
the hydrology removal, and the test only crosses cost-infinite cells, so it does not depend on
inland water existing.

Two places emit a point other than the one they checked:

1. **`LimitHeadingChange`** (`RoadPathSimplifier.cs`, corner-split branch) validates the chord
   `a → mid → c` where `mid = (a + c) / 2`, and then inserts **`Vector2.Lerp(b, mid, 0.55f)`** — a
   different point. Nothing establishes that the segments through the inserted point are clear.
2. **`ChaikinSafe`** validates each source segment `a → b` and the two cut points `q` (25 %) and `r`
   (75 %) on it, then emits `… q_i, r_i, q_{i+1} …`. The pair **`r_i → q_{i+1}` spans the corner at
   `b`** and is never checked. That is the classic Chaikin corner-cutting hazard: cutting a corner
   that hugs an obstacle moves the path into it.

Both are the same bug class — *validate the geometry you actually emit* — and the fix in each case
is local: check the segment through the candidate point and fall back to keeping the original
corner. It is deliberately **not** applied here because it changes generated road geometry on every
seed, and this milestone's road work was scoped to the multi-tile writers.

Do not "fix" this by loosening the test. A road crossing impassable ground is the defect.

---

## 9. Island shaping produces directional banding — diagnosed, fix is an art decision

The island's terrain bands east-west with a repeating ~101 m / ~186 m spacing. Measured: curvature
ratio 0.57–0.71 on all four tiles where Vista's own sample graph, on the same pipeline, reports
0.84–1.50 with no periodicity at all.

Eight hypotheses were tested and killed, including every resolution, erosion and quantisation
explanation the documentation used to assert. The cause is the island's shaping recipe as a whole,
not a parameter — so the fix is a rebuild of that stage against the reference graph, and it changes
how every island looks.

Full record, including what not to re-test and how to verify a rebuild:
**[ISLAND_SHAPING_REBUILD.md](ISLAND_SHAPING_REBUILD.md)**. Evidence with numbers:
[VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md) §2.4a–2.4d.

Cheap unrelated fix found alongside it: `OverworldLpbBake.IslandAnchorCount` is 36 for a 2.2 km
island, so the coastline reads as straight chords.

---

## 10. Sand covers 60–70 % of every tile

`Dump Layer Weights`, seed 110201, all four tiles:

| Tile | sand share | saturated (weight ≥ 0.9) |
|---|---|---|
| 1_1 | 60.8 % | 50.2 % |
| 0_0 | 64.8 % | 56.9 % |
| 0_1 | 70.5 % | 55.8 % |
| 1_0 | 62.3 % | 41.3 % |

Sand sits at Weight Blend priority 3 in `IslandPlateauGraphBuilder.BuildSurfaceStage`, above soil and
rock, so wherever it saturates it erases them — grass is down to 16–22 % and soil to 1–2 %. This is
the same failure the project already fixed once for soil ("soil covering 45–75 % of the island"), now
reappearing on a different layer, and it is the likeliest explanation for the washed-out cream cast
over the whole island in the reference screenshot.

The band is `LowAltitudeCurve(0.03, 0.10)` over the **normalized** graph height, and the island's
land is bottom-heavy: the height histogram shows 27–38 % of dry land inside the lowest tenth of the
24 → 331 m span. A band meant as a beach is therefore covering the plateau.

Related to item 9 but independent of it: this is a surface-weight problem, not a shaping one, and it
can be fixed without rebuilding the geometry. It is still a look decision — narrowing the band
changes the island's colour everywhere — so agree the target before changing it.

---

## 11. One tile carries 9 terrain layers; URP TerrainLit renders 8

Measured on tile `OverworldTerrain_1_0`, which is the tile the settlement landed on:

```
[0..4] the five canonical biome layers
[5]    Dirt_Settlement_Dust
[6]    Dirt_Road_Center
[7]    Dirt_Road_Edge
[8]    Road_Gravel      <- past the ceiling, silently not rendered
```

The other three tiles have 8 layers because they get roads but no settlement dust. The playbook
predicted this (§9); it is now reproduced with evidence.

Two consequences: `Road_Gravel` does not render on whichever tile holds the settlement, and the tile
stacks disagree, so any code that assumes a layer index is the same across tiles is wrong there.

Fix direction: roads and settlement between them append four layers onto a five-layer base, which
cannot fit. Either merge the road layers (centre/edge/gravel is three assets for one road surface),
or paint dust into an existing layer instead of adding one.

---

## 12. Weight sum drops to 0.25 on two tiles

`weight sum on painted pixels: min=0.251` on `OverworldTerrain_0_0` and `min=0.255` on
`OverworldTerrain_0_1`, against an expected ≈1.000 (average is fine at 0.998). Weight Blend only
guarantees the sum when layer 0 is the unconnected white base, which it is, so the shortfall comes
from something downstream — most likely the road/settlement paint writing into a tile whose weights
it does not renormalise, at the same pixels where the stack overflows in item 11.

Not visible in the average; only the histogram's `min` catches it. Worth resolving together with 11.

---

## 13. Smaller items

- `PoiMarkerMenu` doc comments still reference the deleted `VegetationScatterMenu`.
- `Species.Orchard` uses the apple prefab only; pears and plums are unused because Vista Personal
  ignores `prefabVariants` (playbook §5). They would need their own species entries.
- Vegetation Pass phases 4–6 from the original spec are not started: flowers and small plants,
  further environmental dressing, LOD/culling budgets.
- URP shadow distance is at its default, so the 20 m canopies only shade nearby ground.
