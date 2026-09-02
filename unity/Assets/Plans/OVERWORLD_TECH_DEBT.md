# Overworld tech debt

Known defects with measurements, so the next session starts from numbers rather than a hunch.
Terrain-specific traps live in [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md); this file is
for work that is understood but not done.

---

## P0 — OPEN / EXTERNAL DEPENDENCY. Vista terrain generation is bistable

The same project, seed and code produce one of exactly two worlds; the second is ~+36 m
higher across a third of the map. Reproducible in fresh Unity processes, roughly 30 % of
full bakes. Investigated across phases 2G-2P and escalated to the vendor: the graph,
execution plan, seed and configuration are provably identical in both states, and every
local fix or validator candidate was tested and rejected.

**This blocks nothing, but it invalidates single-bake evidence.** Any full-stack
measurement must be repeated until the tile hashes match the canonical reference, or
reported for both states. Island-only bakes have been stable throughout.

Full record: [VISTA_BISTABLE_TERRAIN.md](VISTA_BISTABLE_TERRAIN.md).
Vendor draft: [VISTA_BISTABLE_BUG_REPORT.md](VISTA_BISTABLE_BUG_REPORT.md).

Not fixed. Not worked around. Do not mark resolved without a vendor response.

---

## 0. Roads are disabled in the default pipeline

`WorldGenSettings.Default.RunRoads = false` as of 2026-08-17. The step is off for two independent
reasons, and both have to be fixed before turning it back on:

- **It does not produce usable routes.** Observed in the scene: several near-parallel roads between
  the same pair of points, crossing each other, instead of one route per pair. A spanning tree over
  32 nodes should give 31 edges with no duplicates; the last run reported `main=4 branch=27
  corridors=31`, so the count is right and the *geometry* is wrong — the paths themselves wander and
  overlap. Item 8 is one confirmed mechanism (softening emits points it never validated); it is
  probably not the only one.
- **It costs ~90 % of Generate All** (item 1), which makes every unrelated world iteration slow.

Everything else still runs: ocean, terrain, vegetation, POI markers, settlement and player. Turn the
step back on from `GameDev/Overworld/World Generator` when working on roads specifically.

Note for whoever picks this up: POI markers now place across all four tiles (32 of 32 instead of 5),
so the planner sees roughly six times the node count it was last tuned against. Re-measure before
optimising.

---

## 1. RESOLVED — road planning took 141 s, and the search was returning the wrong route

`roads.plan` is **5.3 s** on the real scene, down from 261.5 s measured at the start of the
investigation, and the routes it returns are now provably the cheapest ones. Three separate defects
were behind that, found in this order.

**The height sampler was re-scanning the scene.** `RoadPathfinder` asks for elevation twice per
neighbour edge, and the delegate it was handed ran `OverworldTerrainAccess.SampleHeightWorld`, which
calls `FindObjectsByType<Terrain>` before it samples anything. One measured plan made **69 230 938**
such calls resolving to at most 12 100 distinct grid cells. `RoadNetworkMenu` now precomputes the
height at every cell centre once and answers from that table, matching on exact coordinates rather
than cell containment — the cost field samples the same rectangle on a finer 10 m lattice, and
answering those from a 25 m cell would have wrecked the slope and curvature derived from them.
Sampler calls fell to 13 224 and the output was bitwise identical.

**A third of the searches were duplicates.** Junction samples are taken every 20 m along accepted
polylines, which is finer than the 25 m search grid, so many land in a cell an earlier sample already
asked about. `TryFindPath` reads the goal only through `TryWorldToCell`, so two samples in one cell
must produce the same answer; `TryAttachToNetwork` now caches by goal cell for the duration of one
attachment. 36.2 % fewer A* calls, again bitwise identical output.

**The heuristic was not admissible.** This one changed results, not just timing. Straight-line
distance in metres assumes no metre costs less than 1, and the comment above `Heuristic` said so
explicitly. `LandscapeCostField` breaks that: within `ExistingCorridorHalfWidth` of an accepted road
it multiplies the (>= 1) terrain cost by `ExistingCorridorDiscount`, which production sets to 0.15.
The heuristic therefore overpriced any route that would reuse a corridor, and the search settled for
the straight line — measured at up to 46 % above the true optimum on a painted field, 5 of 40
searches wrong on the real terrain. Worse, it defeated the corridor reuse the discount exists to
create.

Two fixes were tried. Scaling the heuristic by the discount (`h = 0.15 x euclidean`) is admissible
and consistent, and it made the search exact — but only 1 % of cells actually cost less than 1 per
metre, so every estimate was divided by 6.67 to accommodate them and `roads.plan` went from 9.9 s to
18.0 s. **That is not what shipped.** `RoadSearchLandmarks` replaced it: ALT bounds from four
deterministic farthest-point landmarks, `max over L of |d(L,n) - d(L,goal)|`. Admissible by the
triangle inequality, which applies because the edge cost is symmetric (per-point costs are averaged
and `RoadCost.GradeMultiplier` takes the absolute grade), and monotone, so the closed set stays sound
and no node needs reopening.

Verified: exact against Dijkstra on the real terrain and at discounts 0.05 / 0.15 / 0.50, zero cost
mismatches; 0 overestimates over 4 000 cells and 0 monotonicity violations over 29 488 edges; the
network is bitwise identical to the scalar-bound version, 31 segments, one component, no duplicate
pairs, loops or crossings; three consecutive plans byte-for-byte equal.

Two traps worth remembering. The distance tables must be `double`: `GradeMultiplier` lets distances
reach 1.2e8 while edges can cost 25, and `float` quantisation at that magnitude turned a 332.232 edge
into a 336.000 drop and broke monotonicity. And the tables measure one `LandscapeCostField.Version` —
painting only makes cells cheaper, so a table built before a repaint starts overestimating.
`EnsureFresh` is the guard, and `LowerBound` returns 0 rather than answer from a stale table.

The scalar bound is still there as the floor under the landmark bound. It wins 0.04 % of the time
with fresh tables, but it is the only estimate available when a table has been invalidated or when
neither endpoint can reach a landmark, and it is what the `Func`-only overloads use.

What did not get fixed: `roads.plan` still runs thousands of searches because `TryAttachToNetwork`
starts a full A* per 20 m polyline sample, and each search allocates ~109 KB of scratch arrays.
Reducing that changes road geometry on every seed, so it is a separate decision — see item 0.

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

## 7. OPEN / BLOCKED BY P0 — the overworld scene versions its generated state

`Assets/Scenes/Overworld_Generator.unity` is two things in one file: a small authored scaffolding
that must survive a clone, and the generated world, which every `Generate All` rewrites. Both are
versioned, so the history records bakes rather than decisions and no merge on the scene is possible.

That contradicts an accepted decision in `AGENTS.md`: *generation is not persistence — regenerable
structure is never saved, only player-caused deltas are.*

**Measured, and still valid.** Between two consecutive commits of the scene:

```
fileID anchors surviving a regeneration ..... 23 of 10 515  (0.2 %)
scene lines re-issued with identical content  345 575 of 391 195  (88.3 %)
scene lines that are genuinely new content ..  45 620  (11.7 %)
scene blobs in the measured history ......... 56, ~186 MB raw, of a 310 MB .git
```

The 23 survivors are the authored scaffolding, and they are the real boundary — not the table this
entry used to carry. They are: the four scene-level settings blocks, `SceneRoots`, and five
GameObjects with their components — `OverworldReflectionProbe`, `OverworldWorldState`,
`Review Camera`, `OverworldVolume`, `Sun`. Note that `VistaManager` is **not** among them: it is
present in the scene but recreated like everything else.

**The one confirmed remaining root cause is unstable object identity.** `ClearMarkers()` and
`ClearSettlement()` destroy their roots and everything is created again, so Unity issues fresh
`fileID` anchors; every reference between objects carries those numbers, and the whole file rewrites.
The mechanism is deterministic and owned by our own Editor code — it is not a Vista symptom.

**Item 7a is a mitigation, not a resolution.** It halved the file by removing duplicated materials.
It did not touch identity: the remaining ~200 000 lines still re-issue under new anchors on every
generation. The churn *fraction* after 7a has not been measured, and there is reason to expect it did
not improve — materials were the part most likely to be content-identical, and 7a removed them.

**A follow-up audit found no further small defect.** After 7a the generated content is clean: zero
duplicate spawns (1 857 distinct name/parent/transform tuples over 1 857 GameObjects), colliders
exactly as designed (walls keep them, windows and corners do not), no debug roots, and the 48-line
`MeshRenderer` block is entirely Unity defaults — our code sets only `m_Materials` in it. What
remains is per-object engine overhead on 1 843 legitimately distinct generated objects, dominated by
`MeshRenderer` at roughly 47 % of the post-7a file.

Full resolution therefore needs an architectural decision, and **none of these is approved** —
they are the options an approval would choose between: incremental generation that reuses objects
instead of recreating them; prefab-based serialisation, where an instance stores overrides rather
than every component; mesh aggregation; or a different persistence/versioning policy.

**Removing the generated scene from Git is blocked by P0.** Without the scene in version control the
only way back to the canonical world is to regenerate it, and Vista's bistability means roughly 30 %
of full bakes produce the other world. The canonical world would not be safely recoverable.

Until this is decided, treat scene diffs as noise and never resolve a conflict in them by hand:
regenerate from the seed instead.

---

## 7a. MITIGATED — generated placeholder geometry duplicated its materials

Every shell piece and settlement feature called `new Material(...)` of its own. One measured scene
carried **1 799 inline materials with 62 distinct definitions** — one shader, one keyword set, one
texture set, one float set, one set of GI flags, and only `_BaseColor` and `m_CustomRenderQueue`
varying. At 119 lines each that was 214 081 lines, **54.7 %** of the file.

`GeneratedTintMaterials` now caches by `(tint, renderQueue)` — both axes, because the opening pieces
raise the queue to 3001 so their plaques draw after the walls, and keying on colour alone would merge
a wall with a window. Sharing is safe because nothing mutates these materials after they reach a
renderer: every property write happens before the caller assigns `sharedMaterial`, and no renderer
goes through `renderer.material`, so Unity does not clone them either.

Lifetime is one generation. `Reset()` runs where a generation step clears its previous output, and
every lookup re-checks liveness anyway — a Material whose scene was closed compares equal to null and
is replaced rather than handed back.

Measured on a controlled real-scene generation of the POI and settlement steps only (no bake, no
Vista, no terrain regeneration):

```
inline !u!21 Material blocks .... 1 799 -> 60
scene lines .................... 401 161 -> 200 236   (-50.1 %)
scene bytes .................... 11 043 449 -> 5 529 930
mesh renderers in that run ..... 1 876 (more than the baseline's 1 846)
null sharedMaterial ............ 0
```

That generation rolled a different settlement — batchmode reported `seed=0` — so the object counts
are not comparable to the committed scene and the scene was restored afterwards. The material result
holds regardless: 60 blocks were measured against *more* renderers than the baseline had.

EditMode was 210/210 at that phase, including eight new tests covering shared identity, distinct
tints, distinct queues, exact properties, cache reset, replacement of a destroyed material, and two
renderers sharing one material safely.

**This does not resolve item 7.** It removes duplicated content; identity churn is untouched.

---

## 8. RESOLVED — road softening emitted points it never validated

`RoadPathSimplifierTests.SoftenNatural_CannotCrossImpassableBand` passes, and the whole EditMode
suite is green at **206/206**.

**Root cause: `LimitHeadingChange` validated different geometry from the geometry it emitted.**
Splitting a corner was decided on the chord `a -> mid -> c` with `mid = (a + c) / 2`, but the point
that actually went into the polyline was `Lerp(b, mid, 0.55)` — 45 % of the way back from `mid`
towards the original corner, and therefore outside the chord that had just been cleared, on the side
where `b` was hugging the obstacle. Neither segment through the emitted point was ever checked.

The failure needs two passes and a densified input, which is why the stage looks clean in isolation:
`LimitHeadingChange` alone on the raw path produced 0 violations, `ChaikinSafe` alone produced 0, and
only `Chaikin -> Limit` produced 2. The exact corner, from the second pass:

```
a = (32.5, 25)   b = (37.5, 17.5)   c = (42.5, 13.188)   mid = (37.5, 19.094)
  CHECKED   a -> mid    clear
  CHECKED   mid -> c    clear
  EMITTED   Lerp(b, mid, 0.55) = (37.5, 18.377)
  NEVER CHECKED  a -> emitted   BLOCKED
```

**The fix validates the candidate that actually ships:** the segment from the previously emitted
point (not the original `a` — on the second pass they differ) to the candidate, and from the
candidate to `c`. If either is blocked the original corner is kept, which is what the negative branch
already did. Same two `SegmentClear` calls per split as before.

Nothing else moved. Thresholds, the two-pass structure, endpoint handling, traversal order, return
shape and smoothing strength are all unchanged; per-function hashes confirm `SoftenNatural`,
`CollapseGridStairs`, `ChaikinSafe`, `SegmentClear` and `ClosestPointOnPolyline` are byte-identical,
as are `RoadPathfinder.cs`, `RoadNetworkBuilder.cs` and `RoadCost.cs`.

Verified. Two regression tests were added — a negative one that first asserts the *old* chord was
clear (so the old code had every reason to split) and that the emitted segment is blocked, then
requires the corner to survive; and a positive one on the same corner without the obstacle, proving
softening still happens. On the real terrain, with no bake: smoothing introduced **zero** violations,
topology held at 31 segments / 31 distinct pairs / 0 duplicates / 0 self loops / one component, and
the network came out bitwise identical — 1168 points, 7801.5430 m, all 43 softened polylines
byte-for-byte equal, deviation 0.00 m. Repeated runs reproduce the same fingerprints. Softening cost
the same 8599 cost-function calls before and after, so there is no measurable performance change.

Worth knowing: on the current seed the fix changes nothing at all, because no corner split there ran
into an obstacle. Its value is on seeds where one does, and the synthetic tests are what protect it.

**Two structural risks in the same class were confirmed during the audit and deliberately left
alone. They were not part of this fix.**

- `ChaikinSafe` validates each source segment `a -> b` and the cut points `q` (25 %) and `r` (75 %)
  on it, then emits `... q_i, r_i, q_{i+1} ...`. The span `r_i -> q_{i+1}` crosses the corner at `b`
  and is never checked — the classic corner-cutting hazard. It did not fire on the failing test or
  on the real terrain, but the mechanism is present.
- `CollapseGridStairs` computes `keep[]` against the *original* points and then drops all of them in
  one pass, so two consecutive removals emit a chord that neither validation covered. Proven on a
  minimal case: from `(0,0) (4,1) (8,1) (12,0)` with a spike at `x in [7.5,8.5]`, both interior
  points are individually droppable against clear chords and the emitted `(0,0) -> (12,0)` is
  blocked. On the real terrain the stage is currently a no-op — 0 points dropped of 579, because the
  12.5 m sampling step puts the 43.75 m chord gate below the 50 m span of two grid steps.

One more finding from the audit, unrelated to softening: A* refuses only `+Infinity`, while
`SegmentClear` refuses anything at or above `MountainCost * 0.85 = 11.9`. A diagonal grid step can
therefore pass between two cheap cells whose shared corner — a point A* never evaluates — sits above
that threshold. That is the source of the single remaining violation on the real terrain (a 35.4 m
span whose midpoint costs 12.119). It predates the softening fix and survives it. The half of that
finding that was a defect is resolved below; the rest is the contract working as intended.

---

## 8a. RESOLVED — passability depended on the corridor discount

**Road passability contract (approved 2026-08-31 — see [90 Glossary](../../../90%20Glossary.md) § Road Passability).** Mountains are traversable terrain, not hard
obstacles. Roads may pass through them; the planner prefers cheaper ground — passes, contours,
switchbacks — over a direct expensive climb, and it does that by price, not by refusal. Only three
things block a route outright: water / `+Infinity`, earthwork beyond `MaxCutDepth` / `MaxFillHeight`,
and grade at or above `MaxWalkableGrade`. `MountainPenalty` and `MountainCost` stay soft.

Against that contract one thing was a genuine defect. `LandscapeCostField.Evaluate` multiplies the
whole cost by `ExistingCorridorDiscount` near an accepted road, and `RoadPathSimplifier` was handed
that same value to answer `RoadCost.IsHardObstacleCost`. A flat mountain costs 13 and reads as an
obstacle; at 0.15 it costs 1.95 and stops reading as one. Obstacle status therefore depended on which
roads Prim happened to accept first — a passability predicate cannot be order-dependent, and painting
a road cannot make ground carry a road that could not carry one before.

The split is now explicit. `LandscapeCostField.TerrainCost` / `TerrainCostAtTerrain` answer what the
ground itself costs, before any discount; `Evaluate` / `EvaluateAtTerrain` keep answering what it
costs to build there, discount included, and A* still reads those. The arithmetic in `Evaluate` is
unchanged — the terms were extracted into `TerrainCostAt` and the discount reapplied in the same
order — so the search sees exactly the values it saw before. `RoadNetworkBuilder` carries a
`passabilityAtWorld` oracle to `FinalizePath`; every stage of `RoadPathSimplifier` reads its delegate
only through `SegmentClear` and `IsHardObstacleCost`, so what it always wanted was passability, and
its parameter is now named for that.

Verified. EditMode **210/210**. On the real terrain the network is bitwise identical — fingerprint
`6ED317F9BCEF93EE`, 31 segments, 31 distinct pairs, 0 duplicates, 0 self loops, one component, 0
crossings, 1168 points, 7801.536 m, 0 segments changed, deviation 0.00 m — because that map holds
only 18 mountain cells and no road runs near them. Where the defect was visible it changes what it
should: on seed 110201, 2 segments differ and spans crossing obstacle ground drop from 133 to 131.
Across discounts 0.05 / 0.15 / 0.50 / 1.00 the terrain cost stays 13.000 and the obstacle verdict
stays true, while construction cost moves 8.798 → 13.000. Three repeated plans give the same
fingerprint. A*, the ALT landmarks, B2 and `RoadCost` are untouched.

Still open, both recorded during the audit and deliberately not fixed here:
`LandscapeCostField.Evaluate`'s `BlockedCost` branch is unreachable through `EvaluateAtTerrain`
(it passes `SampleHeight(x, z)` as the desired road height, so excavation and fill are identically
zero — measured 0 infinite cells in 12 100 across nine configurations), which leaves A*'s
`IsPositiveInfinity` check dead in production; and `AsPathCostFunc()` is dead code whose body reads
`return float.IsPositiveInfinity(c) ? c : c`.

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

## 10. RESOLVED — "sand covers 70 % of the island" was a measurement-scope error

**There is no sand defect. Do not narrow the beach band.**

`Dump Layer Weights` reported sand at 60–70 % per tile, saturated on 41–57 %, and that was recorded
here as a defect. It was wrong: the dump averaged over **every painted pixel including the seabed**,
and `SeaFloorGraphBuilder` paints Sand as its *unconnected Weight Blend base* — so every submerged
pixel is sand by construction. Dry land is only 27–52 % of a tile, so the ocean dominated the number.

Measured again restricted to pixels above sea level, seed 110201:

| Tile | land % | grass | **sand** | soil | cliff | rock |
|---|---|---|---|---|---|---|
| 0_0 | 35.9 | 0.435 | **0.082** | 0.039 | 0.132 | 0.267 |
| 0_1 | 26.5 | 0.535 | **0.133** | 0.040 | 0.195 | 0.062 |
| 1_1 | 36.8 | 0.526 | **0.079** | 0.049 | 0.096 | 0.223 |
| 1_0 | 52.4 | 0.387 | **0.315** | 0.024 | 0.034 | 0.216 |

Sand on land averages 0.08–0.13 with saturation under 1.5 % on three tiles — a beach, which is what
it is for. Grass leads everywhere at 0.39–0.54. The outlier is tile 1_0 at 0.315, and that tile is
52 % land with the lowest relief, so its coast band is genuinely wide; still not a blanket.

`VistaLayerWeightDump` now prints an `ON LAND` row per layer and the dry-land fraction. **Compare
layers on those rows.** Whole-tile shares are not comparable between a layer that only exists on
land and one that is the seabed's base.

If the island still reads as too pale, look at the layer *assets* — `Overworld_Grass` albedo and its
11 m tiling — or at the lighting, not at the masks.

---

## 10a. Re-tune vegetation density AFTER the banding is fixed

Grass coverage is fragmented, and the exported density map (`Dump Vegetation State` →
`unity/VistaDetailDump/`) shows the holes following **the terrain banding**, not the tree stands and
not any distance from the camera. The mechanism is direct: ground cover is gated on the grass
*weight*, which is `Π(1 − other layers)`, so wherever the banded terrain raises rock, cliff or soil,
grass drops under its floor and disappears in stripes.

This is item 9 surfacing through the surface weights into vegetation — not a separate defect, and not
fixable by tuning the floor. Any value is a compromise between two symptoms:

| Green floor | Instances | Symptom |
|---|---|---|
| 0.06 (before) | 4 595 634 | grass on beach sand, past the waterline, on vertical mountain faces |
| 0.30 (now) | 1 821 735 | clean edges, but coverage broken into stripes by the banding |

The slope mask and the dry-land gate now handle cliffs and water independently of the floor, so once
the banding is gone the floor can come back down — probably to ~0.20 — without the grass returning
to the beach. **Re-measure then; do not tune it before.** Trees are less sensitive (29 735 placed
after grouping into forest types) because a `ScatterNode` samples its mask against a per-point
random, so a merely-low mask still places some.

---

## 11. RESOLVED — the settlement tile wanted a ninth terrain layer

URP TerrainLit draws **eight** terrain layers: one `_Control` texture feeds Splat0..Splat3 and a
single `TERRAIN_SPLAT_ADDPASS` feeds four more. The tile the settlement landed on was asking for
nine, so one of them was never going to be drawn, and the one that lost was `Road_Gravel`.

**The settlement no longer owns a layer.** `SettlementOverworldMenu.EnsureDustTerrainLayer` returns
`Dirt_Road_Edge` rather than creating `Dirt_Settlement_Dust`, so the stack now reads:

```
[0..4] the five canonical biome layers
[5]    Dirt_Road_Center
[6]    Dirt_Road_Edge      <- the settlement paints its streets, pads and gardens here too
[7]    Road_Gravel
```

The merge is deliberate, not a rename. The two layers already shared their diffuse texture, normal
map, 10x10 tiling and smoothness; only `tileOffset`, specular and `normalScale` differed, and the
road edge's values are the ones kept — it is the more finished of the two and carries roughly nine
times the painted area. One behavioural consequence: the two used to occupy separate channels, so a
road crossing settlement ground wrote into its own. They now share one, and because the settlement
paints before the roads, a road over a pad simply wins. Both wanted the same material there.

Every reachable combination now fits, which follows from `EnsureLayerIndex` merging by `TerrainLayer`
asset identity rather than by name:

| Tile | Layers |
|---|---|
| roads only | 5 canonical + centre + edge + gravel = **8** |
| settlement only | 5 canonical + edge (as dust) = **6** |
| roads and settlement | 5 canonical + centre + edge (shared) + gravel = **8** |

Order does not matter: whichever step runs first appends the edge layer, and the second finds it.

Both live appenders — `RoadSurfacePaint.EnsureLayerIndex` and `SettlementDustPaint.EnsureLayerIndex`
— also refuse to append past eight and log an error rather than adding a layer that would not be
drawn. `Road_Gravel` is no longer displaced.

`TerrainLayerBudgetTests` protects all of this against a well-meaning rename: it drives the real
`RoadSurfacePaint.Paint` and the real `EnsureDustTerrainLayer`, and asserts that dust resolves to the
road edge asset, that either execution order gives the same stack, that eight is never exceeded, that
canonical indices 0-4 do not move, that a ninth append is refused without touching the alphamap, that
repainting does not reorder, and that a missing edge or gravel layer still paints the road.

The budget is now exactly full at 8 of 8. A ninth surface — a path type, a field, snow on the road —
would reopen this, and there is **no priority rule** anywhere in the project to decide which layer
loses; nothing in any canonical or draft document ranks road layers against settlement layers. That
decision would have to be made before another layer is added.

Two loose ends, neither load-bearing. The `Dirt_Settlement_Dust.terrainlayer` asset is still tracked
and no longer loaded by any code. And if `Dirt_Road_Edge.terrainlayer` were ever missing,
`EnsureDustTerrainLayer`'s fallback clones `Dirt_Scaled`, names the clone `Dirt_Settlement_Dust` and
writes it to the road-edge path — an asset whose name disagrees with its file. The branch is
unreachable while the asset exists.

---

## 12. RESOLVED on the live path — weight sum collapse

The recorded symptom was `weight sum on painted pixels: min=0.251` on `OverworldTerrain_0_0` and
`min=0.255` on `OverworldTerrain_0_1` against an expected ~1.000. It was attributed to the paint not
renormalising, and to item 11.

Neither holds for the code that actually runs. **Both live writers measure the real sum of the other
channels and rescale to it**, so they not only preserve a sum of 1, they repair a pixel that arrives
below it:

- `RoadSurfacePaint.WriteRoadPixel` — `keep = 1 - roadSum`, then loops the non-road layers to get
  `otherSum` and scales them by `keep / otherSum`.
- `SettlementDustPaint.WritePixel` — the same shape around `coverage`.

Verified numerically on transcriptions of both: a pixel seeded at 0.25 comes back at exactly 1.0000
through either writer.

**The one formula that does not preserve the sum is in dead code.** `RoadTerrainCarver` assumes
`otherSum = 1 - current` instead of measuring it, which holds only if the pixel already sums to 1 —
the same 0.25 pixel comes back at 0.55. But `RoadTerrainCarver.Carve` has **zero callers** outside
its own file; production uses only its `DensifyPolyline` helper. The path that writes alphamaps and
appends layers there never runs.

Item 11 does not cause this either: appending a layer leaves the sum alone, because Unity zeroes the
new channel.

The recorded 0.251 predates the current code and could not be reproduced against it. Do not reopen
this on account of the dead carver — if that code is ever revived, its normalisation and its missing
layer-budget guard both need fixing first, and that is a separate decision.

One residual path in the live writers is worth knowing: if every other channel is already zero,
`otherSum` falls under the `1e-5` guard and the sum ends at the coverage value rather than 1. It is
not reachable in practice, because `BedCoverage` caps road weight at 0.9 and leaves the rest
distributed — but that reasoning is an argument, not a measurement.

---

## 13. Smaller items

- `PoiMarkerMenu` doc comments still reference the deleted `VegetationScatterMenu`.
- `Species.Orchard` uses the apple prefab only; pears and plums are unused because Vista Personal
  ignores `prefabVariants` (playbook §5). They would need their own species entries.
- Vegetation Pass phases 4–6 from the original spec are not started: flowers and small plants,
  further environmental dressing, LOD/culling budgets.
- URP shadow distance is at its default, so the 20 m canopies only shade nearby ground.
- `RoadPathfinder.TryFindPath` now takes 10 parameters, five of which (`costField`, `heightMetersAtWorld`,
  `isBlocked`, `minCostPerMetre`, `landmarks`) always travel together from `RoadNetworkBuilder`. They
  want to be one search-context struct. Left alone deliberately: the road search was in flight and a
  signature change touches every caller including 11 test sites, so it is worth doing on its own.
- **Generated material lifecycle.** `GeneratedTintMaterials.Reset()` clears the cache but does not
  destroy the Materials it was holding. They were created with `new Material(...)`, are not assets,
  and after a reset nothing references them from the scene graph — so they are not serialised, but
  they are also not collected until a domain reload. This is a runtime/session lifetime question, not
  a serialisation one, and it is unrelated to item 7. Not measured: no leak size was established,
  only the mechanism. Independent of P0, small, and testable (count live Materials across repeated
  generations).
- **FIXED — no `.gitattributes`.** The repository had none, and `core.autocrlf = true` came from
  Git-for-Windows' *system* config, so normalisation was decided by whichever Git each contributor
  installed rather than by the project. The policy is now four rules: `* text=auto`, plus explicit
  `binary` for `*.png`, `*.fbx` and `*.tgz` — the only binary types actually tracked. No `eol` rules,
  no merge driver, no generic Unity template: none of those was justified by the audit.
  <br>Every tracked text blob was **already LF** — all ~1 010 of them scanned, zero CR bytes, the
  400 000-line scene included — so the file records the existing state rather than changing it. A
  controlled `git add --renormalize` (run against a throwaway index so the working tree was never
  staged) touched **no** existing file: the scene did not appear despite carrying 401 161 CR bytes in
  the working tree, and no binary asset appeared. Index EOL counts before and after are identical at
  `lf=1010, -text=16, none=2`.
  <br>What it prevents: this working tree currently holds 830 732 CR bytes across 338 files, 788 454
  of them in the two `.unity` scenes. Committed by a Git with `autocrlf=false` — the default off
  Windows — those would have entered the index and rewritten the scene end to end.
  <br>**This does not resolve item 7.** It removes one way of making that churn worse; the identity
  churn item 7 is about is untouched.
