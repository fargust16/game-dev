# Veilmouth on the Island — the City's Rebuild

Version: 0.3

Status: Draft — the rebuild is authorised by the owner (2026-11-14); this plan is the agent's, and its stages are built in order

Last Updated: 2026-11-14 (§8: IC5's first part built: the town's walls and gates, quays with stairs, retaining walls, bridges, the pier, garden plots at the town's edge, the hamlet, the lighthouse and the dress passes; what the owner is asked to judge before IC6. Earlier: §3 and §8: IC3 and IC4 built, the five districts stand in the lab scene; the builder's frame is a slip, not a current frame under the primitives. Earlier: first version: the way of building, the map of what must change, the stages)

Depends On:
[ISLAND_CONCEPTS.md](../../Documentation/Terrain/ISLAND_CONCEPTS.md), [TERRAIN_POLISH_REPORT.md](../../Documentation/Terrain/TERRAIN_POLISH_REPORT.md), [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §28–§29, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md) §1, [VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md) R4–R5, [91 Agent Decision Record](../../../91%20Agent%20Decision%20Record.md) *Island redesign*

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) VS-ISLAND

---

# Purpose

Concept B, the Valley, is accepted, and its ground is polished. This plan says how the city is built again on it: what the builder is today, what must change, in what order, and what protects the production scene meanwhile. It is written before the work, by `AGENTS.md`'s process; each stage adds what it found.

Every number is an authoring default unless it says measured.

---

# 1. What the owner asked

The rebuild's five principles (owner, 2026-11-14):

1. The city sits in the relief. It never reads as a set of rectangular platforms.
2. The passages between city, nature and shore are natural.
3. Retaining walls, quays, stairs, banks and changes of level are worked out.
4. Setts, stone flags and earth roads each have their own character and texture.
5. Performance is kept: no complex geometry where a material or a decal is enough.

What stays from before: the kit's rectangular buildings at their scale; city plan §29's urban rules (a pavement along every road, unbroken carriageways, houses flush round small yards, R5's passages of at least 4 m); 135 to 150 buildings; walls with gates and guards only where neither water nor relief closes the city; the frame budget of 16.7 ms.

---

# 2. What the builder is today

Read on 2026-11-14 (two read-only surveys; file and line as they then stood).

## 2.1 The chain

`tools/slice/veilmouth_c5_plan.py` writes `Assets/Data/Slice/Map/VeilmouthC5Layout.json`, the plan SVG and the player's map. `VeilmouthLayout.Load()` reads the JSON. `VeilmouthCity.RebuildStreets` (`VeilmouthCity.cs:136`) runs, in order: `Run(true)`, `SlicePeople.PlaceAll`, `SliceWorldSetup.AddToVeilmouth`, `SliceDogs.PlaceReplacing`, `VeilmouthOrnaments.Place`, `VeilmouthMystic.Place`, `VeilmouthReflections.Bake`, `Restage`, `SliceBlacksmith.Run`, `SlicePeopleModels.ScenesOnly`, `VeilmouthFarLod.SyncScene`, `VeilmouthRenders.RenderC5`.

## 2.2 Everything is a rectangle along the axes

- The layout has no angle. A street has `axis` x or y; a building has a compass `facing`, a `front` line and a low end `a`; blocks, squares, water, districts and boxes are `x0..y1`.
- The builder's primitives turn by quarter turns only: `Footprint`, `Rect`, `PutPlan`, `Put` (`Quaternion.Euler(0, 90 * turns, 0)`, positions rounded to whole metres), and every line API takes `alongX` (`Lamps`, `TreeRow`, `HedgeLine`, `RailingRun`, `Fence`, `WallRun`, `Lintel`). `Bridge`, `Pier` and `Waterside` assume a west river leg, a south leg and a sea line.
- What already copes with a turn: `Overlap` (separating axes), `Inside`, `Along`, the alphamap's raster, and `Works.Box`, which takes a yaw.
- **The one turned frame that exists is `Slant`** (`VeilmouthCity.Layout.cs:656-726`): a GameObject with a yaw and a `KitFrame` marker, houses placed in its local metres, plan quads by `TransformPoint`. C5 dropped its only slanted street, so no frame stands in the city today.
- `KitLayoutCheck` (`KitLayoutCheck.cs:134`) accepts a frame only on a whole-metre node at a multiple of 45°. `SLICE_MODULAR_KIT_STANDARD.md` §1 says the same.
- Heights: the city is level at 25.9 m with a rise of 1.5 m. A house stands on the highest ground under it and gets a plinth where the ground falls away (`OwnFloor`, `VeilmouthCity.cs:643`). There is no stair, retaining wall or embankment code in C5, and no kit piece for city walls, gates, quays or outdoor steps: all of those are procedural stone (`Works.Box`).

## 2.3 What is tied to today's coordinates

| Kind | Where | How it is placed |
|---|---|---|
| The player's start | `VeilmouthScene.cs:51` | world point |
| Hidden caches, 40 | `Slice/Caches/CacheSpots.cs:56-110` | world points |
| Paid task places, 9 | `Slice/Tasks/TaskSpot.cs:44-55` | world points |
| Dwellings by id | `SliceAlchemy.cs:85`, `SliceTasks.cs:85` | the door's world position in decimetres |
| Hours posts | `SliceHoursPosts.cs:36-39` | world points and Portals by id |
| Dogs, Kit | `SliceDogs.cs:39`, `SlicePeople.cs:48` | world points |
| POI A, B, C | `Assets/Data/Slice/Poi/Poi*.asset` | world `place`, and Portals by id |
| Ornaments, 11 | `VeilmouthOrnaments.cs:34-48` | plan points |
| Glades, territories, iron veins, hamlet, townsfolk | `VeilmouthCity.Districts.cs:29-53, 552-567, 730, 869` | plan points and bands |
| Fog places, rune rays | `VeilmouthMystic.cs:410-432, 585-600` | layout ids and plan lines |
| The reflection box, the sky probe | `VeilmouthReflections.cs:34, 66` | world box |
| The walls, for sleep | `Slice/Mind/SliceSleep.cs:56-61` | world bounds |
| News and home places | `Resources/Slice/VeilmouthPlaces.json` (`VeilmouthPlacesBake`) | world rectangles |
| The player's map | `VeilmouthC5Plan.xml`, `UI/CityMapElement.cs:111` | a static SVG, world to plan by ±500 |
| Render and probe views | `VeilmouthRenders.cs:88-110`, `Diagnostics/FrameProbe.cs:57-65` | world points |
| Sea Level 24 m | `WorldScale.cs:100`, `CombatGridReader.cs:82`, `VeilmouthSky.cs:89`, `VeilmouthScene.cs:39` | stays 24 m |

Tests that pin the old city (all in `slice` or `slice-play`): heights at fixed points and the zones (`VeilmouthSceneTests`); counts, eight named portals, **no `KitFrame` in the city**, six to eight terrain layers whose first is `Overworld_` (`VeilmouthCityTests`); the 1 m grid and quarter turns (`KitLayoutCheckTests`); the walls' bounds (`SleepTests`); 40 caches by district; 46 walkers; the den's dwelling id; 40 trees, 30 herbs, 12 veins, 10 flax; 32 beasts.

Old saves break: keys are built from positions.

---

# 3. The way of building: a district is a frame

**Decision (the agent's, by the survey).** The builder is not rewritten to oriented rectangles. Each district is built in its own local metres, where everything is along the axes exactly as today, under a frame that carries the district's turn. This is `Slant`'s way, made general.

- **The layout** gains `frames`: each with its name, its origin on a whole-metre node of the plan, its turn, its size, its ground (§4), and its own lists in local metres (`streets`, `squares`, `blocks`, `buildings`, `passages`, `boxes`), in the shapes C5 uses. What lies between frames (water, the town's walls as lines, gates, bridges, quays, stairs, retaining walls, country roads, the hamlet, the woods' zones, the ritual candidates, the start) is in plan metres, as oriented pieces.
- **The planner** is new, `tools/slice/veilmouth_island_plan.py`. It takes the districts from `tools/slice/island/concept_b.py` and fills each block by C5's own rules, which work in any frame's local metres: corner ownership, flush joins, passages, the mix of widths (`veilmouth_c5_plan.py`'s `Block`). C5's planner is not changed.
- **The builder** builds a district on a slip (changed in IC3, §8): the district is built by C5's own steps at C5's own mapping of plan metres to the world, with only the ground reader looking at the island where the district will lie, and the finished district is then set under a `KitFrame` that carries it to its place and turn. Nothing above the ground reader changes (yards, parks, lamps, street trees, hedges, railings), and no primitive learns of frames.
- **The turns** are 0°, 26.565°, 45°, 63.435° and their quarter turns, the ones measured clean on the Combat Grid (`ISLAND_CONCEPTS.md` §2). `KitLayoutCheck` and the kit standard's §1 are widened to them: a frame's direction is a whole-number vector (1, 0), (2, 1), (1, 1) or (1, 2).

Why not oriented rectangles everywhere: most of the builder reads `alongX` and compass facings (§2.2); a frame changes a handful of primitives and leaves the rules of §29 where they are.

---

# 4. The city in the relief

A district's ground is no level platform. It is a **graded surface fitted to the natural ground under it**, and its edges are what a town builds where its ground meets the land:

- **The surface.** A plane through the frame, fitted by least squares to the ground the generator made, its grade held to 5 % along and across. A street on it has a fall a cart can take; a row of houses steps down it, each on its own footing (the builder's plinth).
- **Toward the water:** a quay or an embankment wall, upright, with steps down to the water where a street ends on it.
- **Toward higher ground:** where the fitted surface lies more than 1 m under the land, a retaining wall holds the hill; where less, a bank of turf, its width broken by noise so no edge is a ruled line.
- **Toward lower ground:** where the surface stands more than 1 m over the land, a retaining wall holds the town, with stairs where a lane meets it; where less, a bank.
- **Between two levels inside a district** (a square that must be level, a terrace): a low wall and steps.
- **Outside the frames** the country roads leave through the gates as earth roads, and the first fields, gardens and trees begin at the last houses.

By principle 5, a wall, a quay and a flight of steps are plain stone (`Works.Box`, as C5's quay wall and bridges are); nothing is modelled where the terrain's material carries it.

**Surfaces** (principle 4), all in the island's MicroSplat material, none read stochastically: setts on carriageways, flags on pavements and squares, cobbles in lanes and yards, beaten earth with gravel on country roads.

---

# 5. Protection

- All work is on the branch `island-redesign`.
- The rebuilt city is assembled in a scene of its own, a duplicate site: its own scene, terrain data and layout file. `Veilmouth.unity`, `Veilmouth_Terrain.asset` and `VeilmouthC5Layout.json` are not written until the duplicate has passed its checks and the owner has walked it.
- The slice's tests go on running against the production scene until the swap. The swap is one stage, with its tests changed in the same commit.
- Old saves do not survive the swap. This is said to the owner before it.

---

# 6. Stages

Each stage ends in a commit and leaves the project building.

| Stage | What | Done when |
|---|---|---|
| **IC0** | This plan; the owner's decisions recorded | written |
| **IC1** | The ground under the city: fitted surfaces in place of level platforms, banks and the lines of retaining walls and quays, in the generator; its export for the builder | the lab shows the city's ground with no rectangular platform; each frame's grade and its walls are listed |
| **IC2** | The planner: `VeilmouthIslandLayout.json` with frames, its plan as SVG | every block passes C5's rules; 135 to 150 buildings; the plan is drawn |
| **IC3** | The builder's frames: the current frame under the primitives; `KitLayoutCheck` and the kit standard widened to the measured turns | one district stands in the duplicate scene, and the kit check passes |
| **IC4** | All five districts, their streets painted in the island's material (setts, flags, cobbles), yards, lamps, street trees | the duplicate scene holds the whole city |
| **IC5** | Between the frames: quays and embankments, retaining walls, stairs, bridges, the town's walls and gates, country roads, the hamlet | every change of level has its wall, bank or stair; every gate its road |
| **IC6** | Gameplay moved: the start, the Portals and named places, caches, tasks, posts, dogs, ornaments, glades, territories, veins, the reflection box, the places' bake, the player's map | each kind placed from the new layout, not from a list of old points |
| **IC7** | Checks: the kit check, the frame probe against the audit's baseline, day and night views, the slice's tests against the duplicate | the budget holds; the tests pass on the duplicate |
| **IC8** | The swap: the production scene, terrain and layout replaced; tests and documents changed with it | after the owner's walk of the duplicate |

The woodland is not in these stages: it is a task of its own (`AGENTS.md`, *Island woodland*). IC5 places only the trees the city itself has.

---

# 7. Risks

| Risk | What answers it |
|---|---|
| A frame on a 1:2 line puts no house on the world's 1 m grid | The kit check reads a frame's blocks in the frame's own metres, as it does for 45° today; the Combat Grid was measured clean at these turns |
| Pavement meshes and kerbs were made for level ground | IC3 checks them on a graded frame first; a pavement follows its frame's plane |
| Positions hard-coded in runtime code | IC6 moves each list to the layout or to named places; nothing is patched to new numbers by hand |
| The terrain's layers by index and the `Overworld_` names | The duplicate site names its own layers; the tests change at the swap |
| The frame budget | IC7 probes before the swap; far LODs of the houses are unchanged |
| Dwelling ids from door positions | IC6 gives the two named dwellings a name in the layout |

---

# 8. What each stage found

## IC1, the ground under the city (2026-11-14)

In `tools/slice/island/islandgen.py` (`fit_surface`, `frame_edges`, `platforms`) and `concept_b.py`.

- **Fitting showed what the level platforms had hidden.** The Veil's valley was 60 to 100 m wide at its floor, and the districts are 120 to 150 m deep from the water: they stood on valley sides of 15 %. Fitted to that land they needed retaining walls of 5 to 11 m along hundreds of metres.
- **So the valley is wider where the town stands:** a floor of 220 to 300 m, rising 2.2 m in a hundred from a bank of 1.3 m over the tide. A town is built where a valley gives it room; the ridges begin behind it.
- **The land round each frame is eased toward the frame's ground** across 35 to 70 m, unevenly, before the water runs, and never over water.
- **Result**, each frame's ground a plane: Lower Veil 0.0 % and 1.7 %; the Penny 0.7 % and 1.8 %; Sleepy Quarter 0.2 % and 3.5 %; the Reek 0.3 % and 3.2 %; Parsons' End 6.0 % and 4.8 %, the steepest, on the ridge's foot. Quays: 126 m at Lower Veil and 126 m at the Reek, 4.7 m from the bed to the quay's top. Retaining wall: about 140 m at Parsons' End, 1.5 m high on average and 2 m at most. Every other edge is a bank under a metre. (`Documentation/Terrain/Concepts/B/stats.txt` has the figures of the last build.)
- Seen in the lab: no rectangular platform shows in the valley (`Documentation/Terrain/Polish/Island/02_valley_from_the_estuary.jpg`).

## IC2, the planner (2026-11-14)

`tools/slice/veilmouth_island_plan.py` writes `Assets/Data/Slice/Map/VeilmouthIslandLayout.json` (version `I1`) and draws `VEILMOUTH_ISLAND_PLAN.png`.

- Five district frames and the hamlet's, each with C5's lists in its own metres: 32 streets, 18 blocks, **149 buildings** (116 row houses, 18 single buildings, 7 works sheds, 8 cottages). Every block passes C5's rules; the run fails otherwise.
- **Every named place of C5 keeps its id:** the 16 named buildings, the 12 squares the game looks for, and the streets `main_e`, `draught` and `rats_choir`.
- What changed from the concept's sketch: the Penny's second row is 52 m, so the market is the 52 m square C5's plaza is; Parsons' End's first row is 56 m, for the cathedral's transept; the Reek's first row is one works yard round the Deaf Boiler; the Black Puddle is the open north part of its cell, with the Soot & Bell and Harrow & Tincture on its south side; two villas in gardens take a block of the Sleepy Quarter, and two blocks open their yards to the town's last street, to keep to 150 buildings.
- A single building in the middle of a side needs the two side rows to stop a passage short of its back: the block's `limit`. Without it the corner houses overlap it or stand 1 m off it.
- **Still the concept's, to be drawn again in IC5:** the town's walls and gates (the Penny's far corner now touches the north wall's line), the bridges' ends, the country roads' ends at the frames.
- **New street names** (the island has 32 streets, C5 has 24): Sleepers' Embankment, Eel Row, Hook Street, Gull Lane, Wet Hem, Bishop's Walk, Bellman's Row, Candle Row, Mourners' Lane, Tallow Lane, Parsons' Row. They need their Russian text in the string tables with the player's map (IC6).

## IC3 and IC4, the districts built (2026-11-14)

`VeilmouthCity.Island.cs` (`VeilmouthCity.BuildIsland`), run by `VeilmouthIslandLab.BuildCityAndRender` into `Assets/Scenes/Slice/Lab/IslandLab.unity`; its report is `Logs/IslandCity/report.txt`.

- **A district is built on a slip, then carried.** §3 planned a current frame under the builder's primitives. The survey of the primitives showed a shorter way with the same result: build the district exactly as C5 builds, at C5's mapping of plan metres to the world, and afterwards set its root under a `KitFrame` at the district's origin and turn. The one thing that must know where the district will lie is the ground: `BareGround` reads the island's terrain through `_frameWorld` while a district is on the slip. The streets' paint is carried the same way, quad by quad (`PaintFrame`).
- **What the builder had to learn** is that a place may be absent: on the island a district holds only its own squares, so the steps that look for one (`Parks`, `Cemetery`, `Market`'s pawnbroker, `SquareLamps`, `LampsC4`'s ring and quay, the Notice Board) pass over a place their district has not. C5 has every place, so C5 builds as before.
- **The five districts stand:** 133 footprints, 193 doors, 215 lanes, 31 passages, 1 003 kit blocks. Every row stands on one level; the most a row was raised over its ground is 1.2 m (Parsons' End, the 6 % frame), and no run had to be cut. The streets are painted in the island's own material: setts, cobbles and yard earth from the slice's layers beside the island's twelve, none read stochastically.
- **The kit check** now takes a frame at a measured turn (`IsMeasuredTurn`: 0°, 26.565°, 45°, 63.435° and their quarter turns) and compares two blocks of one frame in the frame's axes (`FrameBounds`): world bounds overstate a block turned 26.6° by up to a third of its width, which read as overlaps between pieces that only touch. One true fault was left, a yard bench under the clock tower's cornice; the planner now stands the tower 2 m further along the Town Hall's back. That move is checked by the next build.
- **A fault of the first run, and its guard.** `Works.Finish` wrote every mesh into `Assets/Prefabs/SliceKit/Works/` under its group's name, so the island's cathedral and works overwrote nine of production's meshes. They were restored from git at once. Meshes now go to `_worksFolder`, which an island frame sets to `Works/Island/{frame}` (ignored by git) and a `finally` sets back. **After every island build `git status` must show no tracked change under `Assets/Prefabs` or `Assets/Art`.**
- **Not built yet, inside the frames:** the cargo, the dwellings' doors and the dress passes (towers, graves, gates, wall dress, grates, covers), which read the built scene by C5's world mapping and need the frame's carry; the hamlet's cottages (its frame has no streets and is built in IC5).

**What the views show** (`Logs/IslandLab/11` to `15`), and what IC5 must answer:

1. **Each district is a paved rectangle on open turf.** Its ground follows the relief, but its outline is a ruled line with a street all round it. This is what the owner's first principle forbids, and it is the first thing IC5 answers: the walls, the quays and the roads tie the five together, and on the land sides the last street is a back lane with gardens beyond it, of uneven depth.
2. **The Veil's banks show steps at the water's edge** along the town's reach, where the 1.3 m bank of IC1 runs across the terrain's 1 m grid at an angle. In the town the quays and embankment walls cover it; outside it the bank is eased in the generator.
3. **The lab's light is not the game's:** shadows are near black and roofs read as one dark mass, because the lab scene has no baked ambient light or reflection probes. Materials are judged by day and by night in IC7, after the look pass. (The near-black shade was the missing ambient probe, given outright since IC5; the steps at the water's edge were the terrain's pixel error of 5, not the bank.)

## IC5, between the districts (2026-11-14)

`VeilmouthCity.IslandTissue.cs`, called by `BuildIsland` after the districts. Built in place, in the world's own metres, from the layout and the terrain as it lies; plain stone throughout (principle 5). Every size is an authoring default.

- **The town's walls were drawn again round the districts as the planner laid them** (`concept_b.py`): the north wall ran through the Penny's far corner and stood 1.4 m from Parsons' End. Each line now keeps 14 m or more from a district, and a gate stands where its road crosses its wall. Built: **1 323 m of wall** in three runs, 6 m high, in 4 m pieces that follow the ground, a coping course of lighter stone, **13 towers** at its turns and ends and **5 gates** (two towers and a lintel each). The wall ends at the water on both banks of the Veil.
- **A district's sides are read from the ground, not from a list:** 1 m inside the side and 3 m outside it, in 4 m pieces. Water outside gives a quay wall down to the bed; a difference over 0.7 m gives a retaining wall (with a parapet where the town stands more than 1.2 m over the land); less is left to the bank. Built: **264 m of quay** with **6 stairs to the water** where a street comes out on it (risers of 0.17 m), **159 m of wall holding the town** and **28 m holding the hill**.
- **Bridges** rise from the ground's own height at each end to a level crown that lets the tide pass under, each length of deck a sloped box, so a walker meets no step: Last Farthing Bridge 46 m, Grubby Bridge 59 m, Beck Bridge 34 m, the Head footbridge 40 m. **The Lenten Bridge is not built:** the ground is level under its line, so Lenten Street runs on it. The Bawling Wedge's pad was widened to carry the Beck Bridge's south end, which stood 0.2 m over the tide; it stands at 0.8 m now, still the lowest ground in the town. The pier is 23 m, its deck at the quay's top.
- **The town's edge** (principles 1 and 2): beyond each district's last street on the land lie **85 walled garden plots** of uneven width and depth, kitchen gardens of bare earth, orchards on lush turf (56 fruit trees in all, few by the frame budget) and rough grass, none between an embankment or a quay and its water, none across a road or within 3.5 m of the town's wall. Their walls are in each district's own stone works: no new mesh.
- **Salt Gullet and Lenten Street** between districts are laid in setts on the ground as it lies.
- **The hamlet's eight cottages and the lighthouse** are built as frames of their own; the lighthouse's lantern and the clock tower's faces, the cemetery's stones, the tree pits' grates and the covers in the paving are dressed on the slip, where a district still lies along the axes their rays expect. A sewer cover's road is the terrain, which on the slip is not the district's own, so its height and tilt are read where the district will lie.
- **The kit check:** 1 433 blocks, no fault. No tracked file under `Assets/Prefabs` or `Assets/Art` changed.
- **The lab's terrain is drawn at a pixel error of 2** (it was 5): at 5 the water's edge stood in steps from the air, which IC3 took for the bank. **The lab's views give the ambient probe outright** after the scene is opened (the three colours' mean all round, the sky's lead from above): a scene opened again in a batch run has none, setting the three colours again did not bring it back, and without it the shade stood near black in IC3's views.
- **The views** are kept in `Documentation/Terrain/City/` (`11` to `20`): the town from the south and from the north-west, the Penny from the air, two streets at eye level, the Sleepy Quarter's embankment, the Creaking Gate, Lower Veil's quay with its stair, Last Farthing Bridge, the Reek with the South Gate.

**Not built yet in IC5:** the gates' and the wall's dress, lamps on the quays and the bridges, cargo on the quays, the hamlet's road posts, and the dwellings' doors (IC6, with the Portals).

**For the owner to judge before IC6.** Gameplay is placed from the layout, so the layout should be accepted first:

1. **The composition.** Veilmouth is five districts of 110 to 180 m with 60 to 100 m of open land between them, inside one wall. Each district's ground follows the relief and its edge is broken by gardens, but from the air a district still reads as a rectangle of streets, and the land between is turf. At eye level the streets, the embankments, the quay and the bridges read as a town. Bringing the districts closer, or building along the roads between them, changes the layout and the count of buildings (149 of 135 to 150).
2. **The walls.** A wall of 6 m round a town of this size is a strong line in the land. It is plain stone with a coping; its dress (plinth, pilasters, the gatehouses) follows C5's.
3. **The low ground at the Beck's mouth** (0.8 m over the tide at the Beck Bridge) reads as a tidal flat in the town's middle.

