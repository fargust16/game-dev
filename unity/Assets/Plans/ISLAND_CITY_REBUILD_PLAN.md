# Veilmouth on the Island — the City's Rebuild

Version: 0.1

Status: Draft — the rebuild is authorised by the owner (2026-11-14); this plan is the agent's, and its stages are built in order

Last Updated: 2026-11-14 (first version: the way of building, the map of what must change, the stages)

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
- **The builder** gets a current frame. Its primitives (`World`, `Rect`, `Footprint`, `Put`, `Slab`, the ground readers) go through it, so the code above them (yards, parks, lamps, street trees, hedges, railings) runs unchanged in a frame's local metres. A frame is a GameObject with `KitFrame`, as `Slant` makes it.
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
