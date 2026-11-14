# Veilmouth on the Island — World Audit and Plan of Correction

Version: 0.3

Status: Draft — the audit the owner ordered on 2026-11-14 (stage 1 of the world polish); its plan is the agent's and waits for no further approval except where §6 names the owner

Last Updated: 2026-11-14 (§9: the geometry of the one network, worked out and not yet drawn. Earlier: §8: stage 2 built and checked, one ground for the town. Earlier: first version: the faults the owner's walk found, their causes by measurement, and the plan)

Depends On:
[ISLAND_CITY_REBUILD_PLAN.md](ISLAND_CITY_REBUILD_PLAN.md), [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §29, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [ISLAND_CONCEPTS.md](../../Documentation/Terrain/ISLAND_CONCEPTS.md), [91 Agent Decision Record](../../../91%20Agent%20Decision%20Record.md) *World polish before gameplay*

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) VS-ISLAND, [AGENTS.md](../../../AGENTS.md)

---

# Purpose

The owner walked the island's city on 2026-11-14 and did not accept it: it reads as separate districts carried into a new scene and joined afterwards. The owner ordered a full revision of the world before any gameplay is moved, beginning with an audit of how the world is built. This document is that audit: what is wrong, why, by what measurement, and how each cause is removed. Nothing in the world was changed to write it.

The owner's rule for the whole work: a fault is removed at its cause in the system that builds the world, never covered by a prop, a stretched texture or a box.

---

# 1. How the audit was made

- **By reading** the three systems that build the island's city: the generator of the ground (`tools/slice/island/islandgen.py`, `concept_b.py`), the planner (`tools/slice/veilmouth_island_plan.py`, on `veilmouth_c5_plan.py`'s `Block`), and the builder (`VeilmouthCity.Island.cs`, `VeilmouthCity.IslandTissue.cs`, on C5's own steps), with the Victorian kit's catalogue (`VeilmouthKitHouses.cs`, `VicHousePlans.cs`).
- **By measuring the layout**, with a tool kept for it: `python tools/slice/veilmouth_island_audit.py`. It reads the island's layout and C5's and counts slots between houses, row ends with no wall, rows that meet at a corner on different levels, bridge ends that continue no street, and the share of the town's extent that is built. Its exit code is the number of kinds that fail; today it is 3 (slots, corner joins, bridge ends). A corrected layout must bring it to 0.
- **Not by Unity:** the owner's editor held the project during the audit, so no batch run was made. Each finding below says whether it rests on a measurement, on the code, or on the owner's screenshot alone.

---

# 2. The one cause under most of it

The city was planned as **five rectangles, each made alone**: its own place in the valley, its own turn, its own border streets, its own ground plane fitted to the land under it. The rules that fill a rectangle are C5's, and C5 stood on level ground. Everything between the rectangles (streets, bridges, walls, the town's edge) was added afterwards by a separate step, from a list drawn before the districts were laid.

Each of the owner's nine questions comes back to one of three things: C5's level-ground rules used on sloping ground; no plan of the town as a whole; and stand-ins of the agent's own that should not have been shown as a result.

---

# 3. The owner's nine questions

## 3.1 Why do buildings lack walls, with slots seen through?

Two causes, both measured.

- **Slots of 1 m and 3 m between houses: 10 of them** (the Penny 6, Lower Veil 2, the Sleepy Quarter 2; C5 has none). C5's rule is that what a row leaves over is nothing or a passage of at least 4 m. The island's planner broke it: to stand a single building in the middle of a block's side (the Town Hall, the Assize, the Guild Hall, the Governor's House, the Customs House) it cut the side rows short by a `limit`, and the cut left 1 m or 3 m. The builder then treats the slot as a passage. This is the agent's fault in `veilmouth_island_plan.py`; IC2's note records the choice and not its cost.
- **Rows that meet at a corner on different levels: 27 of 38 corner joins, by up to 1.53 m.** A row house has no wall on an end that joins a neighbour (`VicEnd.Open`), and a row's end at a block's corner stands flush against the back wall of the corner's house (city plan §29.2). The builder stands each row on one level, the highest ground under it. On C5's level ground both rows of a corner stand at one height and the join is closed. On a district's tilted plane they do not: the open end rises over the wall it should meet, or stops under it, and the roofs do not meet. By district: Lower Veil 4 of 8 joins (0.56 m at most), the Penny 8 of 10 (0.67 m), the Sleepy Quarter 8 of 11 (1.04 m), the Reek 2 of 4 (0.89 m), Parsons' End 5 of 5 (1.53 m).

No row end stands with nothing against it in plan (0 found): the walls are missing in height, not in plan.

## 3.2 Why do U- and L-shaped buildings no longer assemble under one roof?

By the code: **the city has never used them.** The Victorian kit has a U-shaped house with one roof (`VicHousePlans.HouseU`) and the court, oriel and pavilion houses of its rich street (`RichStreet`), built and proved in the kit's own lab (`VicStreets`, `VicKitProbe`). The city's catalogue (`VeilmouthKitHouses.Types`) holds rows with their corners, detached houses and single buildings only: "each house is one rectangle under one roof" (`VICTORIAN_STREETS_PLAN.md`). In C5 an L or a U is made of rows joined flush round a yard, and on level ground it reads as one building. On the island the same joins stand apart in level (§3.1), so the L and the U fell apart. Nothing was removed; a join made for level ground was used on a slope, and the kit's true U was never wired in.

## 3.3 Why do districts stand on platforms of different heights?

By the code and the generator's own figures. `islandgen.platforms` fits **a plane to each district alone**, by least squares to the land under it (`fit_surface`), and writes it over the land. Nothing relates one district's plane to the next. The figures (`Documentation/Terrain/Concepts/B/stats.txt`): Lower Veil at 2.9 m over the tide at its middle, where the land lay at 0.3 m and as low as −3.6 m; the Reek at 3.7 m over land at 0.9 m (−2.6 m at its lowest); the Sleepy Quarter 4.0 m; the Penny 3.5 m; Parsons' End 8.6 m, from 1.6 to 15.5 m at its corners. A district over water or low ground is therefore a filled platform with a wall round it, and the wall the builder set (`IslandEdges`) reads the ground only 3 m outside the district, so it stops above the foot of the fill and bare ground shows under it (the owner's view of the Reek's corner).

A second writer adds to it: the squares' pads are written after the districts and over them (`for pad in C["pads"]`), so a pad's apron lifts a district's street into a hump.

## 3.4 Why have the roads between districts no geometry?

By the code: `IslandStreets` only **paints setts on the terrain** along two lines (Salt Gullet, Lenten Street). A street of a district is built by C5's steps: a carriageway, kerbs, pavement meshes, lamps, trees. Those steps run inside a district and nowhere else, so the land between districts has paint and no street. The country roads are the generator's earth.

## 3.5 Why are bridges made apart from the roads?

Measured: **none of the eight ends of the town's four bridges continues a street.** The bridges are a list in `concept_b.py`, drawn with the first sketch, before the planner laid the districts; the planner never reads it. Their ends lie 4 to 52 m outside the districts, and their lines are turned 1.5° to 39.5° off the streets of the district they meet. Two ends happen to land on an embankment, across it and not along a street. So a bridge runs into an embankment's lamps and trees and past a house's corner. The bridges' form (a flat slab on boxes) is the agent's stand-in of IC5.

## 3.6 Why are there empty places inside districts?

Measured, three causes.

- **Nearly half the town is in no district.** Of the extent of the town (the hull round the five districts, 171 000 m²) 53 % lies in a district; the rest is turf between them. This was the concept: five districts with 60 to 100 m between.
- **Streets take more than half of each district:** 45 to 59 % of a district's rectangle is carriageway and pavement, because each small district has a full ring of border streets of its own, 12 to 16 m wide. Buildings cover 19 % of the districts and **9.8 % of the town's extent**; C5 covers 17 % of the rectangle round its blocks.
- **A cap on the count of buildings.** The planner fails outside 135 to 150 buildings (`veilmouth_island_plan.py:402`). The number was the owner's, given with concept B; the agent held it as a hard gate. The first layout had 179 buildings, and to come under 150 the agent opened two blocks' yards to the street and gave one block of the Sleepy Quarter to two villas in gardens. The bare yards open to the street in the owner's views are that.

The garden plots with their stepped walls were the agent's answer to the open edge (IC5). They are boxes filling space, which the owner forbids.

## 3.7 Are there hard limits on buildings, modules, trees or other things?

- **Buildings:** the cap of 135 to 150 above. It is the only limit that leaves ground empty.
- **Trees:** the edge gardens' fruit trees were held to two or three a plot by the agent "by the frame budget", from the nature lab's count that a kit tree is about sixteen game objects. Street trees follow C5's spacing rules, not a count.
- **The builder:** at most four stairs a district's side (`Take(4)` in `IslandEdges`), a stand-in's limit. C5's own counts (12 iron veins, the glades' trees) are gameplay's and are not in the island's build.
- **Unity:** no limit of the engine or the project was met. The kit's houses have a merged far LOD beyond 50 m; nothing else in the island's build is instanced or batched beyond static batching of the stone works.

## 3.8 Why do walls break off and change height?

By the code. `IslandWalls` builds the wall in 4 m pieces, each from the lowest ground under it to **the highest ground under it plus 6 m**: so the top repeats every rise of the ground as a step, each piece on its own. That is C5's `WallRun`, which on level ground gave a level top. A wall's line ends where its sketch ended, at the river's bank, with a tower and nothing after it. The lines themselves were drawn round the sketch and moved once (IC5) to clear the districts. No wall is closed by design: there is no rule that the perimeter is one line.

## 3.9 Why do terrain, roads and pavements join badly at a district's edge?

By the code. Inside a district the terrain is the district's plane and the pavements are meshes laid on it. At the district's line the pavement mesh simply ends; outside, the terrain blends from the plane to the natural land over a bank of 0.45 to 1.55 times the district's apron (8 to 16 m), unevenly. Nothing joins the two: no kerb, no verge, no street running on. Where a quay stands, the stair down it is set outside the wall on ground that was never cut for it, so the terrain shows through its steps (the owner's view of Lower Veil's quay).

---

# 4. What else the owner's views show

| The owner's view | Cause | Whose |
|---|---|---|
| The Reek's sheds with open gables and bare roofs | They are C5's own placeholder masses (`WorksGroup`, "The works (placeholder)"): boxes with roof planes, never finished in C5 either | C5's stand-in, carried over |
| The Penny as a closed ring of road | C5's market ring stands where four streets cross. The island's planner set the plaza inside a cell between streets, so the ring is cut off by pavement on every side | the planner |
| A street bent over a hump | A pad's apron written over a district's plane (§3.3) | the generator |
| A pond beside the wall's end at the river | A hollow the generator's fill left at the bank, outside any designed ground | the generator |
| Pale trees and turf | Not measured yet. The sun is the slice's own (intensity 2). Candidates: the lab's ambient light, which the agent set by hand; the turf's tint in the lab's material; the fruit tree's own leaf colour. Stage 6 measures them against the production scene under one sky | open |
| Stone walls with no detail, stepped garden walls, a flat bridge, a stair with ground in it | The agent's stand-ins of IC5, built as plain boxes and shown as a stage's result | the agent |

---

# 5. What the agent got wrong in method

Recorded so that it is not repeated.

1. **A number held as a gate over a principle.** The cap of 150 buildings was kept by opening holes in the town. The owner's principle, a town with no accidental voids, outranks a count.
2. **Rules made for level ground carried to slopes without a check.** The join of rows at a corner was known to need one level; no check measured it on a tilted district. The kit check passed, because it reads colliders and not seams, which `AGENTS.md` already warns of.
3. **The parts between districts made last, from an older list.** Bridges, walls and roads should come from the plan of the streets, not be laid over it.
4. **Stand-ins shown as a result, and the layout put to the owner as acceptable.** The agent recommended accepting a layout whose faults it had itself listed.
5. **No close views.** IC5's views were from the air and along a street's middle. None looked at a corner join, a district's edge or a bridge's end from a walker's height.

---

# 6. The plan of correction

In the owner's order of stages. The three systems are corrected where they are; no second generator, planner or builder is made. Each stage ends with the audit tool, close views in Unity from a walker's height in at least three districts, and a commit.

## Stage 2 — one ground for the town

- **The generator makes one town surface, not a plane a district.** `platforms` takes the town's whole extent as one designed ground: the valley's floor under the town is near level, falling toward the Veil by no more than 0.5 % (for the eye and for water), so that a block of 40 m differs by 0.2 m at most and its rows meet on one level, as C5's do. The valley's rise of 2.2 % that IC1 gave the floor moves to the valley's sides, behind the town.
- **One upper terrace, on purpose:** the cathedral's close on the ridge's foot stands higher, level. (As built, §8: it comes down to the town as the ridge's foot, a slope a street climbs, and not by a wall.)
- **Pads are part of the same surface**, written once with the districts, never after them.
- **No fill over water without its wall to the bed.** Where the town's ground meets the river it is a quay or an embankment whose wall is read down to the lowest ground before it, and the stair's place is cut in the ground.
- **Done when:** the audit tool finds no corner join over 0.15 m apart; a section through the town in any direction shows no step that is not a designed wall.

## Stage 3 — architecture

- **No slot under a passage.** The planner's `limit` goes. A single building in a side stands with the rows flush to it or a whole passage from it, and the planner fails on any gap that is neither nothing nor a passage of 4 m or more.
- **The corner join is checked where it is built:** after a district is built the builder measures each join of two rows (their floors within 0.05 m, the open end's top under the wall it meets), and reports a fault otherwise. A see-through check is added to the lab: rays through each block's sides at two heights must meet a wall.
- **The kit's U-shaped house and the rich street's houses enter the city's catalogue** (`HouseU`, the court, oriel and pavilion houses), for the blocks whose form they fit, after their bake is probed as every other type's was. Rows joined round a yard stay for the rest.
- **The count of buildings follows the ground.** The cap of 135 to 150 goes (the owner's order of 2026-11-14 withdraws it). Every block's side is built or is a named open place: a square, a garden, a yard behind its wall. No yard opens bare to a street. An estimate, to be measured: the town's extent built as C5's blocks are gives 230 to 260 buildings.
- **The Reek's works are built, not stood in for:** from Poly Haven's Modular Factory Facade (CC0; the owner allows its geometry), brought to the game's scale and the game's own materials, with whole walls, windows, doors and roofs, through the kit's catalogue and its bake. Poly Haven's Modular Fort 01 is noted as a later source for a fort inside the town; it is not fetched now.
- **Done when:** the audit tool finds no slot and no join apart; the see-through check passes in every district; close views of three corners in three districts show closed walls and roofs.

## Stage 4 — one network of streets

- **The planner lays the town's streets first, as one network:** the embankments along both banks, the streets across the valley, the bridges as lengths of those streets, the gates as their ends. Districts are names over the blocks the streets leave, not rectangles with rings of their own. Where the river's bend turns the grid, the two turns meet in a square or a green made for it, never in a street at a chance angle.
- **A bridge is a street over water:** its carriageway and pavements run on from the street at each end, on one line and one level with it. The audit tool's count of loose ends goes to 0. Its form is a Victorian town bridge: stone piers with cutwaters, arches, parapets with piers, iron railings and lamps from the kit, a built end at each bank.
- **A street between districts is built as a street** by the same steps as one inside, since it lies inside the one plan.
- **The Penny** stands where streets cross, its market in the middle of a square the streets enter, as C5's does; no ring cut off by pavement.
- **What works stays:** the streets inside the districts, with their pavements, lamps and trees, are C5's steps and are not rewritten.
- **Done when:** every district is reached from every other along carriageways; no bridge's end meets a house, a lamp or a tree; no street is paint alone.

## Stage 5 — the town's edge, the walls, the shores

- **The wall is designed or it is not there.** The agent's recommendation: keep it on the town's land sides, as C5 had it, as one closed line from water to water, its top level along each run between two towers and stepping only at a tower, its foot following the ground; its ends in the water as built heads. Historic examples of the eighteenth and nineteenth centuries are read first, as the owner asks, and the choice is shown to the owner with them.
- **The garden boxes go.** The town's edge is made by the plan: the last street, its houses' backs, and beyond them ground that is named (a garden, a paddock, a green), left empty of props for now as the owner says.
- **The shores and the river:** depth that varies, shallows, stony and sandy and earthen banks, slopes and erosion, in the generator, which already has them for the open coast; the reach through the town and the Beck's mouth are done again with the town's surface. Rocks and boulders from the CC0 sets the project holds.
- **Done when:** the wall has no loose end; the bank meets every quay, bridge and wall with no bare fill; the shore's views read as an island's.

## Stage 6 — colour and light

- The pale vegetation is measured (§4) and brought to a quiet, clear green within the slice's palette; materials are checked by day and by night, as the owner's earlier decision already asks.

## Stage 7 — the whole island

- The audit tool at 0; the kit check; the new join and see-through checks; the frame probe against the budget with the larger town; close views of every district, every bridge and the whole of the wall; then the owner's walk.
- If the frame budget fails with the larger town, the answer is instancing, batching and LOD, never fewer houses.

## What waits for the owner

Nothing stops stage 2. Two things are shown to the owner when they are reached, because they change the town's face: the wall's design (stage 5), and the plan of the one network with its count of buildings (stage 4), before it is built.

---

# 7. What is stopped

- **Gameplay is not moved** until this work is done and checked (the owner's first constraint). IC6 of the rebuild's plan stops at IC6.1, the player's rig in the lab scene, which is kept because the world is judged by walking it.
- **The layout I1 is not accepted.** The acceptance of 2026-11-14, given on views from the air, is withdrawn by the owner's walk.

---

# 8. Stage 2 as built: one ground for the town (2026-11-14)

In `tools/slice/island/islandgen.py` (`town_ground`, `platforms`) and `concept_b.py` (`town`); the embankment in `veilmouth_island_plan.py` (`embankments`) and `VeilmouthCity.IslandTissue.cs` (`IslandEmbankments`).

- **The town has one ground.** Everything inside the town's line, which runs as the walls do from water to water on both banks, stands 2.0 m over the tide, the height C5's streets stood over its sea. A district has no ground of its own: it takes the town's. The land round the town is eased toward it over about 70 m before the water runs, so the valley's sides begin behind the town. The squares' pads are no longer written over it.
- **The close is the one upper terrace,** level at 7.0 m. A wall of 5 m toward the town was built first and failed at a walker's height: it stood as a platform in the town's middle, with the terrain's bank bare under the wall's foot. It now comes down to the town as the ridge's foot does, over 60 m, a slope of 7 to 10 % that a street climbs.
- **Measured** (`veilmouth_island_audit.py`): 100.0 % of the town's dry ground, 13.2 ha, lies within 0.05 m of its level, and all of the close's. **No corner join of two rows stands apart in level: 0 of 38, where 27 did.**
- **The embankment follows the water.** The channel is cut into the town's level upright, so the wall stands on the channel's own line for the whole of the town: **1 244 m** along both banks of the Veil and the Beck, its coping on one level, with 10 stairs to the tide set outside the wall over the water, where no ground stands in them. A district's side is no longer a quay of its own.
- **Bridges** meet the ground at one height at both ends, 2.0 m. They still continue no street (stage 4).
- **The garden plots are removed** from the build and from the code.
- **Seen from a walker's height** (`Documentation/Terrain/City/Stage2/`): a block's corner in each of the five districts, each closed in wall and roof; the Veil between its embankments from Last Farthing Bridge; the close on its rise from the town; the town from the north.
- **The kit check:** 1 377 blocks, no fault. No tracked file under `Assets/Prefabs` or `Assets/Art` changed.

**What the same views show, for the stages after:** the districts still stand apart on turf, and a district's pavement ends in the air at its corner (stage 4); the slots between houses are still there (stage 3); the walls and the bridges are plain stone (stages 4 and 5); the open ground of the town carries the woodland's leaf litter in places (stage 6). The audit tool's exit code is 2: slots, and bridge ends.

**For the next stage.** The slots cannot be closed by the planner's rule alone: a single building 30 m wide in a side of 52 m with side rows 10 m deep leaves 1 m either side whatever the rule says. The blocks' own sizes must fit what stands in them, which is the plan of the one network (stage 4). So stages 3 and 4 are planned together, the plan is drawn and shown to the owner, and then built.

---

# 9. The one network: its geometry, worked out (2026-11-14)

Working numbers of the agent for stages 3 and 4, from the layout I1 and the river's line. **Not drawn and not built yet;** the plan is drawn from them, checked by the audit tool, and shown to the owner before anything is built. Plan metres, x east and y south.

## 9.1 Why the districts can join

The Veil runs through the town in two straight reaches: about 63.4° above the bend and 45° below it. The districts already lie along them: the Sleepy Quarter (63.435°) faces the Penny (243.435°) across the upper reach, and the Reek (45°) faces Lower Veil (225°) across the lower. So **the two districts of a reach are parallel, and a bridge can be one straight length of a street on both banks.** The reaches differ by 18.4°, and that turn is taken once on each bank:

- **The west bank is the bend's outer side.** The lines of the two districts' river sides meet at (474, 574). From there the two grids part as they leave the river: a wedge of open ground, nothing at the embankment and 42 m wide at the districts' backs, 120 m from the river. It is a green made on purpose, where the two embankments meet as one street with a turn.
- **The east bank is the inner side,** where two grids would run into each other away from the river; and the Beck comes in exactly there. So the east bank is two bodies with the Beck between them: the Penny above it, Lower Veil below. The land along the Beck's two banks, a wedge 70 m wide at the Veil and 7 m at the Penny's upper corner between the Beck and the Penny's lower side, and a strip of 9 to 25 m between the Beck and Lower Veil, is the town's public ground by the water (the Bawling Wedge and the Beck's walk), paved or green, never a block cut to a chance shape.

## 9.2 The districts' new sizes

Street sections are 12 m, as in I1. A block's side is a multiple of 5 m, so that houses of 10 and 15 m fill it.

| District | Origin, turn | Columns along the river | Why |
|---|---|---|---|
| Sleepy Quarter | (384, 394), 63.435° | 65, 45, 45 (U 203) | reaches the meeting point (474, 574); its first cross street lies at 83 m |
| The Reek | (478, 578), 45° | 55, 60, 60 (U 223) | begins at the meeting point and fills the bank that I1 left as turf; its second cross street lies at 151 m |
| The Penny | (518, 554), 243.435° | 40, 50, 40 (U 178) | the market's column is 50 m, which a civic building of 30 m between two rows of 10 m fills with no slot; its Smoked Street lies at 120 m |
| Lower Veil | (696, 736), 225° | 45, 40, 40 (U 173) | one block more upstream, its upper side 9 to 25 m from the Beck's line; its second cross street lies at 115 m |

The Reek's origin is (478, 578) and not (476, 576): two nodes down its line the sum of the two districts' street stations across the lower reach comes within 3 cm of a whole metre (266), so a bridge's two ends can both stand on a street's middle.

## 9.3 The bridges, as lengths of streets

- **Last Farthing Bridge:** the Penny's Smoked Street (station 120) and the Sleepy Quarter's first cross street (station 83) lie on one line across the upper reach (their stations sum to 203): 48 m, within 0.04° of square to the streets.
- **Grubby Bridge:** Lower Veil's second cross street (station 115) and the Reek's second (station 151) lie on one line across the lower reach: from about (615, 655) to (585, 685), 42 m, within 0.2° of square.
- **Beck Bridge:** Lower Veil's street along the river runs on over the Beck into the Bawling Wedge, where the Penny's streets begin.
- **No Lenten Bridge:** the ground is level there.

## 9.4 Still to settle in the drawing

- The districts' depths (rows away from the river) and what fills each new block, with no cap on the count.
- The Penny's market at a crossing of streets.
- The way up to the close: a street due east from the Penny's upper side to the close's west side, about 84 m, climbing the ridge's foot at 7 to 10 %, with no row of houses on the slope.
- The wall's line round the larger town (stage 5).

