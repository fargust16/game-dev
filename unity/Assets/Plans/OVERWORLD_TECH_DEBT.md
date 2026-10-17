# Overworld tech debt

Known defects with measurements, so the next session starts from numbers rather than a hunch.
The terrain architecture and its Unity traps live in
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md); this file is for work that is
understood but not done.

Items P0, 4, 9, 10 and 10a described defects of the removed terrain pipeline and were deleted with it
on 2026-09-13. The remaining numbers are kept so existing references stay valid.

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

To settle it: `OverworldTerrainValidation.RunBatch -terrainRaw` writes the baked alphamaps and detail
layers per tile; look for a hole in the grass weight or the detail layer at that location. Note that the
terrain bake now places trees and ground cover before settlement paint runs, so a hole that appears
only after the Settlements step is not a bake output.

---

## 7. OPEN — the overworld scene versions its generated state

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
`Review Camera`, `OverworldVolume`, `Sun`.

**The one confirmed remaining root cause is unstable object identity.** `ClearMarkers()` and
`ClearSettlement()` destroy their roots and everything is created again, so Unity issues fresh
`fileID` anchors; every reference between objects carries those numbers, and the whole file rewrites.
The mechanism is deterministic and owned by our own Editor code — it is not a terrain-generation symptom.

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

**The former blocker on removing the generated scene from Git is gone.** It was that the removed
terrain pipeline produced one of two different worlds in fresh processes, so the canonical world could
not be regenerated reliably. The Unity-native terrain bake produces bit-identical heights, alphamaps,
tree instances and detail layers across fresh processes (see
[UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md](UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md)). That covers the
terrain only: fresh-process determinism of the placement steps (POIs, settlement, roads, player) was not
re-measured, and the policy decision itself is still unapproved.

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

Measured on a controlled real-scene generation of the POI and settlement steps only (no terrain
bake, no terrain regeneration):

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
- `TerrainTreeSpecies.Orchard` uses the apple prefab only; the pear and plum Polytope trees are not
  prototypes. They would need their own species entries in `TerrainTreeSpecies` and
  `OverworldVegetationAssets`.
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
  only the mechanism. Small, and testable (count live Materials across repeated
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

---

## 14. Terrain surface and vegetation values are authoring defaults

The Unity-native terrain bake (2026-09-13) implements the project's own surface and vegetation rules in
world units. Where the project had a rule but no number, the value is an **authoring default**, labelled
as such on `TerrainSurfaceRules` / `TerrainVegetationRules` and in
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §3–§5. They need art review, not
code work:

- soil drainage area 20 000 → 150 000 m²;
- massif rock by height 110 → 230 m and ridge convexity 1.5 → 5 m;
- steep rock 35 → 42° and cliff 50 → 58°. On seed 110201 the 110–230 m massif band reads cliff 0.48 /
  rock 0.49 / grass 0.03, because the massif flanks are steep (slope p10 44°, p50 54°, p90 61°); the
  split between cliff and rock is exactly what these two bands decide;
- grove noise, species split, tree probability and grass density. Seed 110201 places 11 552 trees and
  3 796 453 detail instances; the removed pipeline placed 15 852 trees on the same seed.

Changing any of them changes every bake that reads it; re-record the validation hashes afterwards.

## 15. Soil is drainage-driven, not erosion deposition

Soil follows D8 upstream drainage area on the depression-filled terrain lattice. The project has no
erosion field, so soil as **erosion deposition** is not modelled. If TerrainGen gains an erosion or
sediment stage, soil should read it instead of drainage.

## 16. Terrain authority evaluation dominates the bake

On seed 110201 the terrain bake spends ~23.5 s evaluating TerrainGen onto the 1025 × 1025 lattice and
under 1.1 s on everything Unity-side (heights 37 ms, derived fields 164 ms, alphamaps ~540 ms, trees
~50 ms, details ~240 ms). The evaluation is single-threaded and per-vertex. It is correct and
deterministic; it is also the only part of the bake worth optimising, and any optimisation must keep
the pinned TerrainGen field hash.

## 17. Terrain surface textures need a licence check

The five canonical layers use textures in `Assets/Art/Terrain/SurfaceLayers/`, carried over from the
removed terrain pipeline's sample content with their import settings and GUIDs. Their store metadata
was stripped on copy. Confirm the licence permits shipping them outside that package, or replace them,
before committing the folder.

## 18. Legacy terrain layers reference missing textures

Pre-existing, unrelated to the terrain bake: `Assets/Settings/TerrainLayers/` holds legacy layers —
`Dirt_Road_Center`, `Dirt_Road_Edge`, `Road_Gravel`, `Dirt_Scaled`, `Dirt_Swamp`,
`Dirt_Settlement_Dust`, `Grass_Fields`, `Grass_Forest`, `Gravel_*`, `Cliff_Rock`, `CliffBright_Scaled`,
`GrassGreen_Scaled`, `GrassYellow_Scaled`, `Snow_*` — whose diffuse texture GUIDs resolve to no asset in
the project. The road and settlement layers among them are live (items 11 and 12), so roads paint with
an untextured layer. The GUIDs were not in the removed terrain package either.

## 19. CLOSED — Stage 4A revision R1 is the production terrain authority

Stage 4A island relief (`IslandRelief*`, `IslandElevation`) **is the terrain** as of 2026-09-18:
`TerrainAuthorityField` evaluates `IslandElevation` (`H4A = H1 + U2 + R4A`) at `GeneratorVersion` 2.
Revision R1 (inland slope budget `M`, τ-derived pair widths, certified junctions, knoll amplitude cap,
apron projection domain, physical S8/S10/S11) landed 2026-09-16 dump-first and was connected unchanged.

Unity 6000.5.7f1: `TerrainGenStage4ATests` **16/16**; full EditMode **572/572**; Stage 2B hashes
`1de91e469513054b` (110201) and `9d9d7b3c2560bc36` (424242) unchanged, Stage 1 golden hashes unchanged.
Canonical S8-A / S8-M / S8-C, S10, S11-T, S11-O and S12 pass on all seven seeds.

**The switch moved the world and nothing else.** The production authority reproduces the dump-first
`H4A` lattice hash **exactly on all seven canonical seeds** (110201 `e2bad571f4af76c7`, 424242
`2076ecf78a34483b`, 20260905 `4b7ae0f5a6cfaae8`, 7 `6a7ad6edd08db7d9`, 1337 `f86e8c15d7105be2`,
256 `a218e07fe1b2cd74`, 777 `48012b8c997a54df`), which is what proves Stage 2A's macro plates and
Stage 2B's are one thing; `AuthorityField_IsTheSameH4AFieldStage4AWasAcceptedOn` pins it. The pinned
authority hash moved `7614bd2ed819` → `a09953a3bdb3` and the 110201 world signature
`c08574603378` → `656405ea477a`, both measured, never authored. Bake acceptance: height vs TerrainGen
max 0.0107 m over 1 052 676 samples (the 16-bit bound), 0 clamped samples, all four seams 0.0000.

**Stage 3D/3E keep declaring basins and outlets on `StructuralElevation` (`H2`)** — the surface their
topology was frozen against. Stage 3F carves `H = H4A − C3F` after this layer. Repointing Stage 3 at
`H4A` is not a cleanup; it would move frozen topology.

**Performance.** Authority time is dominated by junction certification, not by per-point evaluation:
the `H4A` lattice is about half the cost of the old `H2` lattice because `IslandElevation` evaluates the
signed coast distance once where `StructuralElevation` evaluated it twice. Measured totals (stages +
relief build + lattice, ms): 110201 24 541 → 23 321, 424242 24 368 → 23 642, 7 23 096 → 18 983,
20260905 25 487 → 30 128, 1337 24 089 → 34 776, 256 23 144 → 33 021, **777 21 932 → 53 342**. Three of
seven seeds got faster; 777 is the outlier because junction J7 needs 5 certification iterations over a
disc that grows to ~1330 m at 5 m sampling. Cutting that is a separate optimisation task, not a 4A
retune — see item 20.

Residuals that are **not** defects to chase in 4A:

- **zero knolls** on all 7 canonical seeds (45–49 candidates omitted Gate/Level/Resistance/Budget/MassifBody).
  The analytic `g_P` bound left `a ≤ 0`. Soft target 8–25 fails. Owner: a later knoll kernel (R1 §16 R3).
- **apron ring / zone share** — 25.7 % and 30.3 % of land on 424242 and 20260905 against a 25 % target;
  the apron still reads as a ring. Owner: Stage 3F valleys / Stage 4B piedmont.
- **report-only metric misses** R1 §15 already named: 777 coast-aligned 77.2 % (no-massif limit 70 %),
  256 p50 51 m, 1337 upland 64.8 %, 110201/424242/7 coast-aligned over the massif ≤65 % band.
- **curvature ratio (isotropy)** outside the 0.85–1.20 gate on four seeds — 110201 0.76, 424242 1.30,
  20260905 0.80, 7 1.45 (1337 0.99, 256 1.02, 777 0.97). D§11 expected 0.96–1.01. The gate is a dump
  metric, not a test. Owner: the same straight-bisector edge anisotropy as R1 §16 R6.
- **massif-foot depressions** — Unity sweep 1000–1039: **S8-A 0/40**, **S8-M 6/40**, max 2.32 m
  (seeds 1006, 1013, 1019, 1026, 1027, 1038; all `Z_M`, apron and/or U2 at the deepest cell). Frozen
  H2 has one itself on seed 1019. Owner: Stage 3F / Stage 4B. Any **S8-A** failure on the sweep is a
  4A defect.

A red Stage 4A test is now a terrain regression, not a pending decision.

See [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md)
and the change record
[PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md).

---

## 20. OPEN — Stage 4A junction certification dominates the bake on some seeds

`IslandRelief.Build` costs 3.6–39.5 s depending on the seed, against 5.5–6.6 s for all of Stage 1 +
Stage 2A + massifs. The cost is `SampleJunctionAdverse`: it rasterises a disc of radius
`R_J + max w` at `JunctionCertificationStepMeters` = 5 m and pays ~7 `CoastQuery.SignedDistance` calls
plus 2 `LevelAt` per sample, and it re-rasterises the whole disc on every widening iteration. No-massif
worlds run `PlateUpliftMeters × 1.4`, which inflates the derived widths and radii; on seed 777 junction
J7 iterates 5 times with the disc growing to ~1330 m, which is the 39.5 s.

Known, unexercised opportunities — **measure before changing anything, and do not touch the certified
geometry**: `CoastQuery.IsInside` allocates a `List<float>` per call (pre-existing, but 4A multiplies
the call count); `GatedPlateInlandDerivative` recomputes the centre sample's signed distance it was
already given; the disc is re-rasterised in full per iteration instead of incrementally; land is tested
per sample after the distance has already been paid for.

This is an editor-time cost, once per bake, and it does not touch runtime. It is a correctness-neutral
optimisation: any change here must leave every canonical `H4A` hash in item 19 bit-identical.

---

## 21. OPEN — Stage 3F: valleys reach the island, basins do not

Stage 3F exists dump-first as `CarvedElevation` (`H3F = H4A − C3F`), `TerrainGenStage3FDump` and
`TerrainGenStage3FTests`. Production is untouched: `TerrainAuthorityField` still evaluates
`IslandElevation`. F0 (corridor-bound) is §19 of
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md); **F1, which
re-bound the carve to Slice C's `FinalValleyTrajectory` under an API-only Stage 3C unfreeze, is §20.**
Slice C is now the only valley geometry that reaches a height field.

Findings, none of them a Stage 4A defect:

- ~~**B7**~~ **CLOSED by F1.** The carve is now keyed to Slice C's `FinalValleyTrajectory`, so the
  bound and the carve are one curve. Measured adverse climb along those trajectories in `H3F`:
  1.94 / 1.96 / 2.56 / 2.29 m against the 7.2 m budget.
- **B9 — CONFIRMED OPEN by F3, and the mechanism is named.** Independent verification
  (`Stage3FVerification`, preflight §21) walks the finished ground along each declared valley and
  classifies every local low. Canonical: 110201 **4 CLOSED, worst 6.32 m**; 424242 **1, 0.71 m**;
  20260905 and 7 none. Sweep 1000–1039: **30/40 seeds, 96 CLOSED**, worst 42.06 m. `H3F ≤ H4A` holds
  at 0 violations. Two mechanisms, both measured: **M1**, the rise is inherited from `H4A` and exceeds
  the carve's own cap — 110201 trajectory 5 has an 8.00 m rise against a 7.20 m `CorridorIncisionMeters`
  budget it spends in full, leaving 6.32 m; **M2**, `TroughProfile` peaks at the toe and decays
  downstream, so it can create a barrier where `H4A` had none (trajectory 3: 0.30 → 1.85 m). M1
  dominates — the carve reduces the rise on 6 of 9 trajectories on 110201. Valley **width** decides the
  outcome: where a low drains around its barrier the cross-valley floor is metres below it, where it
  closes the cross-section is flat, and the narrowest corridor on the seed is one of the closed ones.
  Fixing this means changing `CorridorIncisionMeters`, the trough shape, or Stage 4A's relief — none of
  which F3 is entitled to do. Sweep 1000–1039: 0/40 contract
  violations (no bad projection, nothing outside a trajectory's half-width, nothing carved at sea, no
  arc past a declared end), but a mean of 4.8 extra qualifying depressions per seed, worst 11, and on
  2 of 40 seeds the finished ground still rises up to 7.66 m along a trajectory — more than the
  `CorridorIncisionMeters` budget the trough has to spend. That figure is the *ground's* adverse rise,
  **not** S11-T, which bounds `max(R4A − running min R4A)` and is green. 4–18 qualifying depressions (≤ 3.0 m) remain on
  the four massif seeds where `H4A` had none, with centreline climb well inside budget, so their cause
  is off-centreline — corridor/corridor `SmoothMax` or the cross-section meeting Stage 4A relief.
  Classifying them against declared basins is the independent scan Stage 3F F3 specifies.
- **B8 — massif-free worlds have no valleys.** Corridors come from massif spurs only, so 1337, 256 and
  777 carry zero corridors and `H3F` is bit-identical to `H4A` there. Three of seven canonical seeds
  get no Stage 3F relief at all.
- **Carve shape.** Max 66–75 m but 4.5–5.9 m mean over carved ground: the depth is in compact
  excavations at corridor heads on massif flanks, with faint traces downstream. The island floor gains
  much less than the massif does.

**B10 (new) — Stage 4A's own S11-T, S11-O O-3 and plate+apron bounds fail on sweep seed 1032.**
Stage 4A's unmodified harness reports `C(R4A) 19.64 m` against ≤ 7.2 m (trajectory 8; trajectory 7 is
7.59 m), `plate+apron adverse 21.34 m` against ≤ 1.5 m, and `S11-O O-3: FAIL`. All three are asserted by
test on the **seven canonical seeds only**, where they pass, so nothing caught this. Cross-validated
against an independent reproduction on Stage 4A's own stations — see
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md) §21.4. This sits
**upstream of Stage 3F**: the carve cannot remove a rise larger than its own budget, and it is the
measured cause of B9's M1. Whether the canonical set should widen, or the bounds should hold off it, is
an owner decision. Do not fix it by changing the carve.

**Root cause, diagnosed 2026-09-30 — DESIGN/CONTRACT GAP plus TEST-COVERAGE GAP, not an
implementation bug.** Stage 4A's gate is a function of Stage 1's height, so along any curve
`ΔR4A ≈ ΔΓ·U·Λ + ΔΛ·U·Γ + ΔM`, and both leading terms scale with `U = 18 m`. **S11-T's bound is
7.2 m while `R4A`'s own range on a level-1.0 district is `U + M ≈ 19.7 m`**, so a trajectory traversing
either the gate band or the level range exceeds the bound by construction. On 1032 V8 the trajectory
grazes the shore (`sd = 3 m`, where `Γ = 0` is Stage 4A's own S1 guarantee) and turns 262 m back inland
(`Γ = 1`, full 18 m plate uplift): 19.715 − 0.077 = 19.64 m, apron zero, no knolls, level unchanged.

Every local invariant R1 carries is a derivative **along the coast-distance gradient** (S8-C, S12);
S11-T is a derivative **along a trajectory**. Going inland `Γ·P` must increase, and S11-T counts that as
adverse — the two contracts are in direct tension and R1 reconciles nothing. All three junctions on 1032
certified at `R 450 → 450` in one iteration, and the apron is ordinary; the seed is geometrically
unlucky, not malformed. The written formulas were checked against the code on this seed and match.

Coverage: the canonical seven contain no trajectory that traverses the gate band — their largest Stage 1
rise after closest approach is 8.57 m against 1032's 28.39 m. `Stage3FVerification` now reports
`GateTraverseFromToe`, `CoastDistanceExcursionMeters` and `Stage1RiseFromToe`, which separate the two
mechanisms: `dGate > 0` is gate recovery (1032 V8 1.00 → 19.64 m, V7 0.36 → 7.59 m), `dGate = 0` is a
plate-level crossing (1032 V6 ΔΛ 0.317 → 5.38 m). Full analysis:
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md) §22.

No remedy is chosen. The candidates differ in kind — widen the canonical set, restate S11-T as a bound
on something the construction can guarantee, or cap `U·Λ` — and the last would move every world.

Do not treat B8 or B9 as licence to retune Stage 4A. Both are Stage 3C/3F questions, and B8 may be a
Stage 4B one. **F2 (basin realisation) stays blocked on B4**, a basin's depth bound, which is still an
owner decision.

**Note 2026-10-16 — the axial `CLOSED` count is not the drainage measure.** Research phases 1–4 in
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md)
(§§9–13, seeds 1000–1039) change how B9 reads. Nothing above is withdrawn as a measurement, and no
production code changed.

- **All 36 axial `CLOSED` lows of `H4A` are route closures.** None lies inside a plan-view (S8)
  depression; water leaves the trajectory sideways. M1 describes declared trajectories crossing
  rising ground, not basins. Treat `Stage3FVerification`'s `CLOSED` as a diagnostic, not as the defect
  count.
- **The plan-view measure:** production `H3F` creates **211** new significant S8 depressions relative
  to `H4A` on the 32 massif seeds (209 by an independent union-find scan). The main mechanisms are the
  trough floor following `H4A`'s lateral slope (the cross-section low sits 5–12 m off the axis), the
  mountain term (59 on its own) and `SmoothMax` (about 18).
- **"Fixing this means changing `CorridorIncisionMeters`, the trough shape, or Stage 4A's relief" does
  not hold for plan-view drainage.** A 12 m bound creates 217 against 222 at 7.2 m. An E-family hybrid
  (a floor-referenced plain carve plus a band-local correction on the massif flank) reaches zero new
  depressions at the 7.2 m plain bound, confirmed by the independent scan — a research result, not an
  approved remedy.
- **B10's rise does not make basins by itself** — `H4A` has only six significant S8 depressions on the
  sweep. What it produces is route-barrier valleys: the gaps and terminations on trajectories with
  `dGate` > 0 (handoff §§9–10). B10 and B4 are otherwise unchanged by this research.

**Note 2026-10-17 — stale XML documentation in `CarvedElevation`.** The `<remarks>` of
`World/TerrainGen/CarvedElevation.cs` still say the carve is read "as `CarveField` binds it today — to
the frozen `ValleyCorridor` trajectories, not to Slice C's `FinalValleyTrajectory`", and that re-binding
"is deliberately not done here". F1 (preflight §20) did re-bind it, API-only: `CarveField` is typed to
`IValleyCarveCurve`, and `ValleyTrajectoryNetwork.BuildCarve` builds the carve along
`FinalValleyTrajectory`, as the top of this item says. The comment misled the Stage 3F documentation
stage once (99 Open Questions 3F-8, corrected in v0.30). Fix it the next time `CarvedElevation.cs` is
touched — it is a production-code change and is not made from documentation work.

Open decisions: [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Stage 3F valley carve.
