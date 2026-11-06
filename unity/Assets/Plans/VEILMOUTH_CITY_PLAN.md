# Veilmouth City Plan (C1)

Version: 0.7

Status: Draft — accepted by the owner, 2026-11-05; sizes remain authoring defaults

Last Updated: 2026-11-06 (§8, §11: the Dwelling scene for every NPC home, by the owner, after the seven interiors. Earlier: §13: the city built once from C1 by `VeilmouthCity`, with its streets, works, houses, doors, lamps, green, and what is still missing. Earlier: §9: the ground shaped once from C1 by `VeilmouthGround`. Earlier: §2, §8: the modules and BK_AlchemistHouse measured; the Victorian modules are built at ×1.25 and BK's rooms at ×1.2, so interior ceilings are 3 m. Earlier: §12: the Combat Grid and the kit check tested at 45°; streets at 45° are allowed from 3 m wide. Earlier: accepted by the owner with every remark: the Governor's Palace, right-angled streets with some at 45° once the Combat Grid is checked at that angle, and the size. Earlier: first draft, from the owner's fifteen remarks after playing the dressed C0)

Depends On:
[11 Vertical Slice](../../../11%20Vertical%20Slice.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §2, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

Referenced By:
[VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) S0.6, [99 Open Questions](../../../99%20Open%20Questions.md)

---

# Purpose

The first dressing of C0 put real models on the accepted map, and the owner's play test showed the map itself was the problem: a few streets in a field, houses as boxes on grass, props scattered without a reason. This document designs Veilmouth as a city before anything is built again: its zones, its streets in section, its plots and what stands on them, its courtyards, its edge and gates, its ground, and its interiors. It keeps everything C0 holds (the seven buildings that open, the Masters, the Ritual Site Candidates, the resources and beasts outside) and moves it into a believable city.

The owner accepted C1 on 2026-11-05 and it replaces C0's layout; C0's contents stay. Names are placeholders. Every size is an authoring default, labelled **measured** where it comes from a model.

The plan's drawing is [VEILMOUTH_CITY_PLAN_C1.svg](VEILMOUTH_CITY_PLAN_C1.svg), at 1 px = 1 m, north up.

---

# 1. The owner's play test, 2026-11-05

| # | Remark (owner) | Answer |
|---|---|---|
| 1 | Lamp posts stand in front of doors; things need a system | The street furniture rules (§4): a lamp stands on the kerb line at a party wall, never in front of a door |
| 2 | The map is primitive; design the city first, by scale: plots for houses and their grounds, the market, the tavern, the police station; props with a purpose | This plan: zones (§3), plots (§5), prop sets by plot (§6) |
| 3 | Interiors are stone boxes without a roof; build them from BK_AlchemistHouse, with shelves, cupboards, books, potions, a stove, a wash stand; larger and more complex | Interior plans (§8), from BK_AlchemistHouse, with ceilings |
| 4 | Wooden walls stand by some doors | They were the portal leaf, a stand-in drawn over the house's own door. Hidden on 2026-11-05: only its trigger stays |
| 5 | Zones (central street, residential, commercial, slums), each with its own features; the central street dense and of one height, wide pavements for two people, a carriageway for two carriages; residential houses behind railings with a front garden and steps; paved streets; houses larger and detailed; arches lead into courtyards and are passable | Zones (§3), street sections (§2), plots (§5), courtyards (§7) |
| 6 | The player's character is not visible | Fixed 2026-11-05: the Human Character Dummy wears built-in `Unlit/Texture`, which URP does not draw. Every Dummy now wears a URP copy of its material |
| 7 | NPCs hang in the air | Fixed 2026-11-05: they wore the player's animator, whose `Grounded` starts false, so they stood in its fall pose. They now have an idle-only controller. Walking and schedules are S2 and S5c |
| 8 | Rooms are small; everything looks toy-like; rework the scale from the models' sizes | Scale (§2): measured from the models; density, depth and detail, not only size, decide how a city reads |
| 9 | Scene changes jerk; add a transition | Done 2026-11-05: a fade to black of 0.3 s, the load in the dark, and a fade in of 0.5 s once the player and camera stand in the new scene |
| 10 | A city map like Assassin's Creed Syndicate | We take what makes that London read as a city (a river with embankments, a grand processional avenue, dense terraces, rookeries, docks, landmarks over the roofs), not its map |
| 11 | The camera jerks near walls | Done 2026-11-05: the camera arm shortens fast and returns slowly after a short delay, and only the ground and buildings move it; props and plants no longer do |
| 12 | Realistic placement of houses, roads, lamps, props and pavements | §2–§6 |
| 13 | The dragons float, never touch the ground or attack, and play their death | Their own controller is the pack's demo, which plays every clip in turn. They now stand in their idle. Behaviour, attack and fights with beasts are S3 |
| 14 | The city stands in a field; it needs natural entrances and exits of the 18th–early 20th century, a grand avenue at a palace, government buildings | The Edge and the Gates (§3.8), Crown Avenue and the Government Quarter (§3.1) |
| 15 | The agent shapes the ground: the sea and the river dug, the Ridge raised, the Soot Stair's slope, the hill, painted | Ground plan (§9), built once from this plan after the owner accepts it |

Answers to the three questions of the first pass: an arch house lets people through, and stands where a lane leads into a small courtyard worth seeing (§7); the orange and blue crowns stay for now, but plain green trees and grass become the common cover and the coloured trees the rare ones that are resources (§6.4); the door leaf is hidden and only its trigger stays.

---

# 2. Scale and street sections

**Measured** from the survey of 2026-11-05: a person is 1.8 m; a Modular Victorian City house is 6.55 m wide, 15–18 m tall, and only 7 m deep; its lamp post is 4 m. The houses are already near true size. The toy look comes from what stands around them: shells 7 m deep on grass, wide empty ground between them, no pavement edge, and bright flat colours. C1 answers it so:

- **Depth:** a house is built from the pack's modules (§5.1) on a plot 10–14 m deep, with a back wall and a roof, not as a shell.
- **Density:** street fronts are continuous; there is no grass between houses on a street.
- **Detail:** kerbs, steps, railings, shop fronts, awnings, chimneys, and lamps by the rules of §4.
- **Size:** a ground-floor storey under 3.5 m or a front doorway under 2.5 m is scaled up with every module together. **Measured** (survey of 2026-11-05, `Logs/SliceSurvey/survey.tsv`): a module is 2 m wide and a storey 3 m high; `Door 1` is 1.01 × 1.91 m in a `Door frame` of 1.2 × 2.34 m; `Steps` are 2 m wide and 3 m high over 4 m; `Arch` is 4 m wide and 6 m high; `Fence` 1.5 m high. The houses are therefore built at **×1.25**: a module 2.5 m, a storey 3.75 m, a doorway 2.93 m with a door of 2.39 m, an arch 5 × 7.5 m, railings 1.9 m. It is the smallest factor that clears 3.5 m and keeps an even count of modules on whole metres (frontages of 5, 10, 15 m); ×1.17 would clear it but leave modules of 2.33 m off the grid. The scale stands on the model inside the kit wrapper, so the block itself keeps scale 1 (kit standard §1).

**Street sections** (authoring defaults; a carriage is about 2.2 m wide, two pass in 6 m; two people walk abreast in 1.5 m):

| Street | Total | Pavement each side | Carriageway | Notes |
|---|---|---|---|---|
| Crown Avenue (grand) | 36 m | 4 m | 2 × 7 m | a 14 m mall of trees and benches between the carriageways |
| High Street (central) | 18 m | 4 m | 10 m | two carriages pass with a third standing |
| Ridge Street, terrace streets | 12–14 m | 3 m | 6–8 m | plus front areas of 2–4 m behind railings |
| Bell Street, Mercers Row, Cheap Street | 10–12 m | 2–3 m | 6 m | shop fronts, stalls at the kerb |
| Loom Lane, mews | 6–8 m | none or 1 m | shared | cobbles |
| Court alley (Lower Veil) | 2–3 m | — | — | under arches, between tenements |
| Stair lane (Soot Stair) | 8 m | — | — | flights of 1.5–2 m with landings |

Surfaces (owner: asphalt or setts): the carriageways of Crown Avenue and the High Street in asphalt, which London laid in its main streets from the 1870s; the other carriageways in setts; pavements in flagstone, kerbs in stone 0.15 m high, courts and lanes in cobbles.

---

# 3. Zones

City about 480 × 340 m inside an area of 1000 × 1000 m (C0 was 200 × 200 m inside 700 × 700 m). Coordinates are world metres, north +Z.

## 3.1 Crown Avenue and the Government Quarter

A straight avenue from Crown Circus on the High Street north up the Ridge to the Governor's Palace forecourt (§10 asks whose palace). Trees in pairs every 20 m on the mall, lamps on both kerbs, benches between trees. On its flanks the Town Hall (west) and the Assize Court with the Wardens' station and lock-up (east). The palace closes the view up the avenue, and its dome or tower is the city's main landmark, seen from the High Street and the docks.

## 3.2 High Street (the central street)

East–west from the West Gate to the Veil Bridge. Four-storey houses of one cornice height, wall to wall, shops on the ground floor with awnings and signs, corner buildings at each junction. Lamps every 25–30 m staggered on the two kerbs, benches and trash cans at shop fronts, bollards at the corners. It ends in a landmark at each end: the West Gate's tower, and the bridge.

## 3.3 Terraces (residential, west)

Middle-class terraces between the High Street and the Ridge: houses of three storeys on a raised ground floor, each behind a front area with railings, a gate, steps up, and a small tree or shrubs. A garden square (Wardley Square) with railings, lawn, trees, and benches. Mews behind the terraces through arches (§7).

## 3.4 The Ridge (upper town)

The rich, 8 m above the High Street behind a retaining wall. Villas and wide townhouses in gardens, the faction's house, Ridge Street, and the palace. Lamps regular and bright: stealth is hard here.

## 3.5 Market Quarter (commercial)

South-west of the High Street. Market Square with the stalls and the trader, the Masters' counters on its sides, the seer in a corner booth, Cheap Street and Mercers Row lined with shops, the Item Creation yard through an arch, warehouses toward the fishing quay.

## 3.6 Lower Veil (slums and docks)

South-east, by the river and the sea, and the pocket Under the Ridge north of the High Street. Tenements of three and four storeys, narrow fronts, courts and alleys of 2–3 m, washing across them, wall lanterns far apart: darkness for stealth (10.3). Bell Street with the Soot & Bell (corner house with its yard) and Harrow & Tincture, the Notice Board outside the tavern, Loom Lane with the pawnbroker and the missing person's tenement, Ritual Site Candidate 1 as a small square, the Customs House, the docks with warehouses, cranes, cargo, and the sea quay.

## 3.7 Soot Stair and the old chapel

The Soot Stair drops from the east end of Ridge Street through Under the Ridge to the High Street, flights and landings between blind walls, the cellar door at its foot. The old chapel stands in the north-west corner inside the wall, with its graveyard, by a postern toward the forest.

## 3.8 The edge and the gates

The old town wall, 6 m, remains on the north and west, with the West Gate (the road to the fields and farms) and the North Gate (the road through the forest); the river and its embankment are the east edge, the Veil Bridge the east exit to the ore hill, and the sea with its quays the south. Outside the wall: the fields and farms to the west, the forest north, toll houses on the roads. No side of the city meets open grass.

---

# 4. Street furniture rules

- A **lamp** stands on the kerb line, 0.6 m in from the kerb, on the line of a party wall between two houses, never within 2 m of a door or in front of a window bay. Spacing 25–30 m, staggered on streets wider than 12 m, one at every corner. Courts and alleys have wall lanterns on brackets, 15–25 m apart; some stay dark.
- **Benches** stand on squares, the avenue's mall, and at cab stands, facing the street or the green, never across a doorway.
- **Trash cans** stand by shop doors and at corners; **bollards** at corners and at pavement edges by gates.
- **Barrels, crates, firewood, carts** belong to a yard, a dock, a shop's back, or a stall: never on a pavement without an owner.
- Nothing stands in the 1.5 m walking line of a pavement.

These rules apply in the builder, and the kit check gets a new test for them (lamps against doors).

---

# 5. Plots

## 5.1 Building from modules

A plot is a rectangle on the 1 m grid with a frontage, a depth, a storey count, and a type. Its house is built from the Modular Victorian City modules (`Modules/GroundWall1`, `GroundDoor1`, `GroundWindow1`, the `C1` corner pieces, `UpWall`, `UpWindow1`, `Roof`, `RoofCorner`, `Roof Window`, `Arch`, the `WoodenWall*` and `Awning` shop fronts, `Fence`, `Steps`, `Sidewalk Straight` and `Sidewalk corner`). It follows [15](../../../15%20Procedural%20Building%20Generation.md)'s Exterior Shell Grammar in spirit and the Shell Non-Overlap Rule. It is assembled once by the build tool; the result is the owner's.

## 5.2 Plot types

| Type | Frontage × depth | Storeys | Ground floor | Grounds |
|---|---|---|---|---|
| High Street shop house | 6–9 × 12 m | 4 | shop front, awning, sign | yard behind |
| Corner building | 9–14 × 14 m | 4 | shop or tavern, door on the corner | — |
| Terrace house | 6–7 × 12 m | 3 + raised ground | door up 4–6 steps | front area 3 m, railings, gate, a tree or shrubs; garden behind |
| Villa (Ridge) | 14–20 × 14 m | 2–3 | porch | garden 10–20 m round it, wall or railings |
| Tenement | 5–6 × 10 m | 3–4 | door straight on the street | court behind |
| Warehouse | 12–20 × 16 m | 2–3 | loading door, hoist | dock or yard |
| Civic (Town Hall, Assize, Customs) | 30–60 × 30 m | 2–3 high storeys | portico, steps | forecourt |
| Arch house | 9–12 × 12 m | 3–4 | passage 3.5 m wide, 4.5 m high | the court behind (§7) |

## 5.3 The seven that open and the counters

The Soot & Bell (corner house on Bell Street with its yard), Harrow & Tincture (shop house on Bell Street), the faction's house (villa on Ridge Street), the Wardens' station and lock-up (civic, by the avenue), the missing person's tenement (Loom Lane), the cellar (a door at the Soot Stair's foot), the old chapel. The Masters, the trader, the pawnbroker and the seer keep serving over street counters, so they have no interior (C0).

---

# 6. Props by place

## 6.1 Every prop has an owner

| Place | Props |
|---|---|
| Market Square | stalls in rows with an aisle of 3 m, crates and baskets behind them, a pump, a cart at the edge |
| Shop house | sign, awning, a bench or a barrel by the door, trash can |
| Tavern yard | barrels, kegs, a cart, a trough, a lamp over the gate |
| Item Creation yard | anvil, leather frame, armour stand, timber and iron stacks |
| Docks | crates, barrels, rope, a crane, a cart, a bollard line on the quay |
| Terrace | railings, gate, steps, a tree, shrubs, a boot scraper |
| Court | pump, washing lines, crates, a broken cart, a wall lantern |
| Square, avenue | trees, benches, a fountain or a statue, lamps |

## 6.2 Trees and green

Plain green trees and grass are the common cover (Polytope Studio `PT_Fruit_Tree_01_green`, `PT_Pine_Tree_03_green` with their `cut`, `stump`, and `logs` variants, `PT_Grass_02`, shrubs, flowers); the Stylized Trees Pack's orange and blue trees become rare and mark resources (owner, 2026-11-05). In the city trees grow only in squares, the avenue, gardens and front areas.

---

# 7. Courtyards

Every arch leads somewhere worth entering. Four in C1:

| Court | Off | Size | Holds |
|---|---|---|---|
| Mews court | a terrace street | 28 × 18 m | stable doors, a carriage, a pump and trough, hay, washing, a wall lamp, mews cottages at the back |
| Tenement court | Bell Street | 15 × 12 m | a pump, washing lines across, crates, a door to an upper stair, an alley out to Loom Lane: a stealth route |
| Item Creation yard | Cheap Street | 16 × 14 m | the Master's yard (§6.1) |
| Soot & Bell yard | Bell Street | 12 × 10 m | the inn yard (§6.1), the tavern's back door |

The drawing's inset shows the mews court.

---

# 8. Interiors

Built from BK_AlchemistHouse (owner, 2026-11-05): `IntWall01a/b`, `IntWall01door`, `IntWall02*` with windows, `IntWoodFloor01`, `StoneFloor01`, `IntCeiling01` with beams and pillars, `WoodStairs`, `Door`, the `Stove`, `TableSink`, cupboards, shelves, drawers, tables, chairs, stools, carpets, paintings, books, bottles, flasks, jars, candles and oil lamps, the cauldrons and lab sets. Its materials are built-in Standard and get URP copies under `Materials/Slice/BK/` (`SliceUrpMaterials`, 2026-11-05: 142 Standard by their own maps and render modes, the three of BK's refraction glass as clear URP glass, flame and smoke as URP particles).

**Measured** (2026-11-05): BK's walls, floors and ceilings are 2.5 m modules, its walls 2.5 m high, its door 1.1 × 1.98 m, its stairs 2.53 m high; its furniture is at true size (stove 1.51 m high, cupboard 1.9 m, table 0.78 m, chair seat about 0.45 m). The architecture is built at **×1.2**, a 3 m module with a door of 2.38 m, and the furniture at ×1.0. Ceilings are therefore **3 m**, not the 3.3–3.6 m first written here: ×1.32–1.44 would reach those, but makes doors 2.6–2.85 m and leaves the module off whole metres. The taproom and the chapel's nave may take a second row of wall later for height. Rooms that read as rooms, not boxes:

| Interior | Rooms (authoring default) |
|---|---|
| Soot & Bell | taproom 14 × 10 m with the bar, fireplace and tables; kitchen 6 × 5 m with the stove and sink; back door to the yard; stairs up to a landing with closed doors; the bed in a let room 5 × 4 m |
| Harrow & Tincture | shop 8 × 6 m with counter and shelves of bottles; laboratory 8 × 6 m with cauldron, alembic, lab sets, books; store room 4 × 6 m |
| Faction's house | hall 10 × 8 m with the rostrum and pews; office 6 × 5 m; archive 6 × 5 m with shelves |
| Wardens' station | front desk room 8 × 6 m; office 5 × 5 m; guard room with the stove; three cells along a corridor, the owner's bars and door |
| Missing person's room | one tenement room 6 × 5 m: bed, wash stand, stove, table, chair, the cache; the stair landing outside |
| Cellar | three vaults of 6 × 6 m joined by arches, barrels, the circle's place |
| Old chapel | nave 8 × 16 m with pews; vestry 4 × 5 m |

**Dwellings** (owner, 2026-11-06; [15](../../../15%20Procedural%20Building%20Generation.md), [10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1). Every other house opens into one shared Dwelling scene, which assembles its rooms on entry from the house's type and id with the same kit: one home of two to four rooms on the floor of its door, a single flat in a tenement, the same each time, its picked door and taken Stash kept for the run. Rooms (authoring defaults):

| House | Rooms | Wealth |
|---|---|---|
| Terrace | hall, parlour 5 × 4 m, kitchen, bedroom | middling; rich on the Ridge |
| Tenement | one flat: a room 5 × 4 m and a kitchen, or one room | poor |
| Shop | shop room, back room, kitchen | middling; poor in Lower Veil |
| Villa | hall, parlour, dining room, kitchen, study | rich |
| Warehouse | store, office | poor |

---

# 9. The ground

The agent shapes the slice's ground once from C1 with an authoring tool, at the owner's request of 2026-11-05; the owner then changes it by hand. It is not the production terrain pipeline and touches nothing of TerrainGen. Heights (authoring defaults): sea bed 18–20 m; the river bed 21 m in a channel of 40 m with stone embankments to 26 m; quays 26 m; the city's low streets 26 m; the Ridge plateau 34 m behind a retaining wall; the avenue climbs 8 m over 100 m (8 %); the Soot Stair drops 8 m over 50 m in flights; the ore hill 40–70 m across the river; the area's edge steep beyond 45°. Paint: grass in fields and gardens, forest floor in the forest, rock on the hill and the edge, sand and mud on the shore; streets carry their own paving.

**Shaped** 2026-11-05 by `VeilmouthGround` (`GameDev/Slice/Shape Veilmouth Ground from C1 (once)`), which refuses to run once the scene holds the C1 map; the ground is the owner's from then on. The terrain is 1000 × 1000 m with a heightmap of 1025 (a sample every 0.98 m) and weights of 512 on the five canonical layers. As built (authoring defaults):

- the low city at 25.9 m and the Ridge at 33.9 m behind a retaining wall on its edge, one sample wide, for the kit's wall to face;
- Crown Avenue climbs from the Circus at 25.9 m to the forecourt at 33.9 m, and the ground 60 m either side of it climbs with it, easing into the low town and into the Ridge over 25 m more: the Town Hall and the Assize stand on that slope and get level platforms when they are built, and Ridge Street dips to meet the avenue rather than meeting it at a step;
- the Soot Stair is a causeway that falls 8 m over 58 m between the ground on either side, for its flights and blind walls;
- the river bed at 21 m in its 40 m channel, with vertical embankments along the city and banks of 25 m outside it; the sea bed from 19.5 m to 18 m, vertical quays along the city and the basin, a beach elsewhere;
- outside the walls, a coastal plain at the low level that rises 8 m north of the Ridge's line into the forest, rolling there by up to 4 m; the ore hill to 70 m;
- the north, west, and east edges rise 45 m within 45 m of the boundary, steeper than 45°, with a pass where each road leaves.

Checked by `VeilmouthSceneTests` (the heights above, the edge's slope, the roads' passes) and by `VeilmouthRenders`. The same day the C1 city replaced C0's dressing on this ground (§13).

---

# 10. The owner's answers, 2026-11-05

1. **The palace** is the **Governor's Palace**, the Crown's residence in the city; the full game's capital stays elsewhere (21's Capital Gate).
2. **Angles:** streets are at right angles, and some run at 45°. A 45° street is built only after the Combat Grid and the kit check are tested at that angle (§12).
3. **Size:** the city is about 480 × 340 m in an area of 1000 × 1000 m.

---

# 12. Streets at 45°: the grid check

`CombatGridAngleCheck` (agent, 2026-11-05; `Logs/GridAngle/report.txt`) builds a straight walled street 28 m long, 2, 3, 4, 6, and 12 m wide, at 0°, 45°, and 30°, once on the kit's grid and once off it, and reads it with the Combat Grid. **Measured:**

| Reading | 0° | 45° |
|---|---|---|
| Street cells a person can stand on | 100 % | 100 % |
| One end reaches the other | always | from 3 m wide; a 2 m street on the grid falls apart into a staircase the grid cannot cross without cutting corners |
| Grid moves per metre of street | 0.95 | 0.68: a diagonal step costs 1 AP like a straight one (10.1), so a fight along a 45° street covers about 40 % more ground per AP, as it already does across any open square |
| Cells beside the wall with cover, before | 100 % on the grid, 0–100 % off it | 0–52 %, by chance of how the wall met the cells |
| Cells beside the wall with cover, after the change below | 100 % on the grid | 100 % from 3 m wide |

**Changed** the same day: cover was one ray 0.75 m from a cell's middle along each side, which misses a wall at 45°; it is now a thin strip beside each side, from 0.3 to 0.9 m out and as wide as the cell, at each cover height (`CombatGridReader`; authoring defaults). A wall on a grid line gives the same cover as before. At 30°, not asked for, 81–92 % of those cells get cover.

**Rules for C1** (the kit standard §1): a street at 45° is a `KitFrame` on a node of the world grid; its blocks keep the 1 m grid and 90° turns in the frame's axes, and the kit check measures a turned block's overlap on its colliders, not on its world bounds (tests: `KitLayoutCheckTests`). A street at 45° is at least 3 m wide; alleys and courts of 2–3 m stay at right angles. The Combat Grid itself stays world-aligned.

---

# 11. Order of work

1. Measure the Victorian modules and BK_AlchemistHouse; URP copies of BK's materials.
2. The ground (§9).
3. ~~The plot builder and the city (§3–§7); the C0 Map zones become C1's; tests follow.~~ Done 2026-11-05 (§13).
4. The seven interiors (§8).
4a. The Dwelling scene and its generator (§8), then a door on every dwelling, and breaking in with witnesses, the Wardens and the Arrest ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1–6.2, [10.4](../../../10.4%20Dialogue%20and%20Social%20Checks.md) §6).
5. ~~Trees and green (§6.2).~~ Done with the city (§13); the courts' and gardens' details remain.
6. Walk it, render it, and hand it to the owner.

---

# 13. The city as built, 2026-11-05

`VeilmouthCity` (*GameDev → Slice → Build Veilmouth City from C1 (once)*) builds the city once from this plan on the shaped ground, under one root, `Veilmouth C1`, and refuses to run again: from then on the scene is the owner's. It removes C0's `Dressing` root and lays the `C1 Map` zones again with two candidates moved to where their places now are: Candidate 2 to the cellar door's yard (213, −136), Candidate 3 to the chapel's forecourt (−189, −48). `BuildReplacing` is for the agent's own batch iterations only. Layout randomness is a fixed `System.Random`, never the world seed. Everything below is an authoring default.

**Streets** are painted into the ground: three layers appended after the canonical five, which keep their order, because URP's terrain draws eight layers at most (Yughues' textures as terrain layers under `Settings/TerrainLayers/Slice/`, created once and tuned in the Inspector after that): *Setts* (`Rough01`) on every carriageway, *Flags* (`SmoothSquare`) on the pavements, and *Cobbles* (`Mediterranean01`) on squares, lanes, and yards. §2's asphalt has no texture in the agreed packs, so the High Street and the avenue are setts for now. The weights are painted again at 1024 (0.98 m a texel) over the canonical paint of §9, and the low town's yards and courts are cobbled. Kerbs are stone strips 0.3 m wide and 0.15 m high on the pavement's side of each carriageway, broken at every street, square, yard, or gate that meets them; they are NoGrid and carry no collider. As built, in plan coordinates (§3's world coordinates are X = px − 500, Z = 500 − py):

| Street | Where | Section |
|---|---|---|
| High Street | 280–780 × 662–680, broken by Crown Circus | 4 + 10 + 4 m |
| Crown Avenue | 482–518 × 560–646 | 4 + 7 + 14 mall + 7 + 4 m |
| Ridge Street | 280–708 × 580–592 | 3 + 6 + 3 m |
| Terrace streets West and East | 304–316 and 414–426 × 606–662 | 3 + 6 + 3 m |
| Mercers Row and Bell Street | 280–370 and 430–724 × 730–740, on one line broken by the Market Square | 2 + 6 + 2 m |
| Cheap Street, Dock Road | 454–466 and 724–736 × 680–812 | 2 + 8 + 2 m |
| Loom Lane, Ridge Lane, Back Lane | 560–724 × 778–784; 545–698 × 620–626; 280–414 × 600–606 | cobbles |
| Tanners Slant | from the High Street (325, 680) to the Market Square (370, 725), at 45° | 8 m setts |
| Alleys | Court Alley, Loom Alley, Ridge Alley, Chapel Lane, the Garden Passage | 3–14 m, cobbles |
| North Road | 618–628 × 490–580 | setts |
| Quays | the Sea Quay 280–740 × 812–830; the Basin Quay 736–780 × 780–790 | setts |

Squares: Crown Circus (475–525 × 646–696), the palace forecourt (460–540 × 530–560), the Market Square (370–430 × 720–765), the Ritual Square (642–667 × 740–778), the chapel's forecourt, Wardley Garden (340–390 × 606–644, lawn), the Soot & Bell's yard and gate, the cellar yard, and the pawnbroker's corner.

**Stone works** are one mesh for each material with a box collider for each solid box (Structure): the town wall, 2 m thick and 6 m over the ground, along the west and north, with the West Gate (a 14 m gap, a tower either side, a lintel from 7 to 9 m), the North Gate, and the chapel's postern; the Ridge's retaining wall with a 1 m parapet wherever the two levels differ by 0.3 m or more; the Soot Stair's blind walls, at least 3.2 m over the low ground, and its cellar door; the river's embankment with a 1 m parapet; the basin's walls and the sea quay's edge; and the Veil Bridge, a stone deck 16 m wide at street level on four piers, from the embankment to the east bank at px 852. Julien Tonsuso's bridge is no longer used.

**Houses** are the 27 variants baked from the Victorian modules (§5.1–§5.2) in rows along every street. A house takes its plot only if it is free; where a row meets a street, a square, or an earlier building, it moves on a metre and tries again. It stands on the highest ground under it, rounded up to 0.1 m, on a stone plinth where the ground falls more than 0.25 m, and every door above the ground outside gets steps (rises of at most 0.18 m, treads of 0.32 m, 2.4 m wide). Each baked house carries a `Door` marker in each doorway. Terrace houses are raised 0.9 m behind front areas of 2 m with railings and a gate at each door; the Ridge's villas stand 10 m apart in gardens behind railings. Tanners Slant is the one street at 45°: a `KitFrame` on the world node (−175, −180), its tenements and shops in the frame's grid, a dark lane with no lamps. The palace is the marble Civic16 with a Wing12 either side; the Town Hall (Civic16) faces the avenue from its podium on the slope; the Assize (Civic12) faces it from the east; the Customs House (Civic12) stands at the basin's head. Arch houses lead from Terrace Street West into Wardley Garden and from Bell Street into the tenement court.

**The seven that open** (§5.3), each with a portal leaf in its door: the Soot & Bell (Tavern6, Bell Street's north side, with its yard and gate east of it), Harrow & Tincture (Shop4, west of the tavern), the faction's house (Villa6 on Ridge Street), the Assize and the Wardens' station (the avenue's east flank), the missing person's tenement (Tenement4 on Loom Lane), the cellar door in the Soot Stair's east wall, and the old chapel (north-west corner). The Notice Board stands at the tavern's gate.

**Lamps** follow §4: in the pavement's cell by the kerb, at a party wall where one is within 5 m, never within 2 m of a door along its front, 25–30 m apart, staggered across streets wider than 12 m, and at the corners of the forecourt and the Circus. `VeilmouthCityTests` checks every lamp against every door marker and portal.

**Squares and green:** the avenue's mall has trees in pairs every 20 m with benches between; the Crown Column stands in the Circus; Wardley Garden has railings with gates, five trees, benches, shrubs and flowers; the forecourt has four trees. Plain green trees and grass are Polytope Studio's (§6.2): about 560 pines and fruit trees and 200 shrubs in the forest, grass and flowers in the glades and fields; the Stylized Trees Pack's coloured crowns are only the 40 fellable trees round the glades.

**Outside the walls:** four farms on the West road and the fenced flax field; the forest north of the wall, with eight glades, 30 herb patches, six wolf and four boar territories with their stand-ins; the ore hill with 12 iron veins, pines, and shrubs. People: the Wardens, the faction, Veinmarket, the Masters, the sellers, the pawnbroker, and townsfolk at their posts in the asset map's colours.

**Counted** on the full build: 1953 kit blocks with no kit fault, 315 houses, 323 doorways, 147 lamps, 233 railing segments, 7 portals. Tests: `VeilmouthCityTests` (no lamp before a door, the seven doors, the slant's frame, C1 replaces C0's dressing), with `KitLayoutCheckTests`, `PortalPairingTests`, and `VeilmouthSceneTests`.

**Still missing or simplified** in this pass: the Item Creation yard stands in the Market Square, not through an arch off Cheap Street; the mews court is Wardley Garden and its passage; courts have no pumps, washing lines, carts, or cranes (no models yet); the stalls are few; roofs keep the pack's blue slate; the Combat Grid has not yet been run on the new streets; the interiors are still C0's (§11, step 4).
