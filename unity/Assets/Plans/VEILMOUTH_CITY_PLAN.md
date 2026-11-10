# Veilmouth City Plan (C1, C2, C3, C4)

Version: 0.50

Status: Draft — C1 accepted by the owner, 2026-11-05; C2 (§21), which replaces C1's layout, accepted 2026-11-06; C3 (§24), which replaces C2's layout and houses, asked for by the owner 2026-11-08, built, and walked by the owner the same day, whose remarks go to [VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md); C4 (§28), the owner's map, which replaces C3's layout, ground and names, asked for by the owner 2026-11-10 and being built; sizes remain authoring defaults

Last Updated: 2026-11-10 (§28.5: C4's plan, written with the builder's layout and the player's map by tools/slice/veilmouth_c4_plan.py: about 260 × 290 m inside the wall and about 102 buildings; the Penny, 80 m with its ring and Stone Grandad; Long Tongue and Salt Gullet, Lenten and Smoked Streets, Widows' Row, Powder Embankment, Squint Lane; Parsons' End, Sleepy Quarter, Salt End, Lower Veil, the Reek; folk names in map.csv)

Depends On:
[11 Vertical Slice](../../../11%20Vertical%20Slice.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §2, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md), [VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md), [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md)

Referenced By:
[VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) S0.6, [99 Open Questions](../../../99%20Open%20Questions.md), [VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md)

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
| High Street (central) | 18 m | 5 m | 8 m | two carriages pass with a third standing |
| Ridge Street, terrace streets | 12–14 m | 4 m | 4–6 m | plus front areas of 2–4 m behind railings |
| Bell Street, Mercers Row, Cheap Street | 10–12 m | 3 m | 4–6 m | shop fronts, stalls at the kerb |
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

Middle-class houses between the High Street and the Ridge. The district keeps its name, but its houses stand alone (§19, owner 2026-11-06): three storeys, each on its own green plot, railed all round with one gate opposite its door, with a garden of trees, shrubs, flowers, and a bench. A garden square (Wardley Square) with railings, lawn, trees, and benches. Mews behind the terraces through arches (§7).

## 3.4 The Ridge (upper town)

The rich, 8 m above the High Street behind a retaining wall. Villas standing alone in gardens (§19), the faction's house in the largest, Ridge Street, and the palace. Lamps regular and bright: stealth is hard here.

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
| Detached house (Terraces) | 10 × 10 m on a plot 22 × 16 m | 3 | door on the ground, its stoop | a plot railed all round (§19): front garden 4 m, 6 m each side, 2 m behind |
| Villa (Ridge) | 15 × 15 m on a plot 25–37 × 23 m | 3 | door on the ground, its stoop | a plot railed all round (§19): front garden 5 m, 5 m or more each side, 3 m behind |
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
| Shop house | sign, awning, a bench by the door, trash can; no barrel or crate (§25.6) |
| Tavern yard | a cart, a trough, a lamp over the gate; its barrels stand inside, at the bar's end (§25.6) |
| Item Creation yard | anvil, leather frame, armour stand, timber and iron stacks, hedged in (§25.6) |
| Docks | crates, barrels, rope, a crane, a cart, a bollard line on the quay |
| Terrace | railings, gate, steps, a tree, shrubs, a boot scraper |
| Court | pump, washing lines, a broken cart, a wall lantern |
| Square, avenue | trees, benches, a fountain or a statue, lamps |

## 6.2 Trees and green

Plain green trees and grass are the common cover (Polytope Studio `PT_Fruit_Tree_01_green`, `PT_Pine_Tree_03_green` with their `cut`, `stump`, and `logs` variants, `PT_Grass_02`, shrubs, flowers); the Stylized Trees Pack's orange and blue trees become rare and mark resources (owner, 2026-11-05). In the city trees grow only in squares, the avenue, gardens and front areas. Since 2026-11-09 the city's own trees and bushes are the Stylized Trees Pack in a muted city green, with trees along the pavements, hedges and grass (§25, owner); Polytope stays the cover outside the walls.

---

# 7. Courtyards

Every arch leads somewhere worth entering. Four in C1:

| Court | Off | Size | Holds |
|---|---|---|---|
| Mews court | a terrace street | 28 × 18 m | stable doors, a carriage, a pump and trough, hay, washing, a wall lamp, mews cottages at the back |
| Tenement court | Bell Street | 15 × 12 m | a pump, washing lines across, crates, a door to an upper stair, an alley out to Loom Lane: a stealth route |
| Item Creation yard | Cheap Street | 16 × 14 m | the Master's yard (§6.1) |
| Soot & Bell yard | Bell Street | 12 × 10 m | the inn yard (§6.1), the tavern's back door, an exit since VI-8 (`soot-and-bell.yard`) |

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
4. ~~The seven interiors (§8).~~ Done 2026-11-06 (§14).
4a. ~~The Dwelling scene and its generator (§8), then a door on every dwelling~~ (done 2026-11-06, §16), and breaking in with witnesses, the Wardens and the Arrest ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1–6.2, [10.4](../../../10.4%20Dialogue%20and%20Social%20Checks.md) §6).
5. ~~Trees and green (§6.2).~~ Done with the city (§13); the courts' and gardens' details remain.
6. Walk it, render it, and hand it to the owner. The Combat Grid's reading of the city, its interiors and dwellings passed on 2026-11-06 ([VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md), *S0's acceptance in the slice's own scenes*); the railings were lowered to 1.04 m for it. The owner's walk remains.

---

# 13. The city as built, 2026-11-05

*C2 (§21) replaced this layout on 2026-11-06; the rules below still hold, and the coordinates here are C1's.*

`VeilmouthCity` (*GameDev → Slice → Build Veilmouth City from C1 (once)*) builds the city once from this plan on the shaped ground, under one root, `Veilmouth C1`, and refuses to run again: from then on the scene is the owner's. It removes C0's `Dressing` root and lays the `C1 Map` zones again with two candidates moved to where their places now are: Candidate 2 to the cellar door's yard (213, −136), Candidate 3 to the chapel's forecourt (−189, −48). `BuildReplacing` is for the agent's own batch iterations only. Layout randomness is a fixed `System.Random`, never the world seed. Everything below is an authoring default.

**Streets** are painted into the ground: three layers appended after the canonical five, which keep their order, because URP's terrain draws eight layers at most (Yughues' textures as terrain layers under `Settings/TerrainLayers/Slice/`, created once and tuned in the Inspector after that): *Setts* (`Rough01`) on every carriageway, *Flags* (`SmoothSquare`) on the pavements, and *Cobbles* (`Mediterranean01`) on squares, lanes, and yards. §2's asphalt has no texture in the agreed packs, so the High Street and the avenue are setts for now. The weights are painted again at 1024 (0.98 m a texel) over the canonical paint of §9, and the low town's yards and courts are cobbled. Kerbs are stone strips 0.3 m wide and 0.15 m high on the pavement's side of each carriageway, broken at every street, square, yard, or gate that meets them; they are NoGrid and carry no collider. As built, in plan coordinates (§3's world coordinates are X = px − 500, Z = 500 − py):

| Street | Where | Section |
|---|---|---|
| High Street | 280–780 × 662–680, broken by Crown Circus | 5 + 8 + 5 m |
| Crown Avenue | 482–518 × 560–646 | 4 + 7 + 14 mall + 7 + 4 m |
| Ridge Street | 280–708 × 580–592 | 4 + 4 + 4 m |
| Terrace streets West and East | 304–316 and 414–426 × 606–662 | 4 + 4 + 4 m |
| Mercers Row and Bell Street | 280–370 and 430–724 × 730–740, on one line broken by the Market Square | 3 + 4 + 3 m |
| Cheap Street, Dock Road | 454–466 and 724–736 × 680–812 | 3 + 6 + 3 m |
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

---

# 14. The interiors as built, 2026-11-06

`VeilmouthInteriors` (*GameDev → Slice → Build Veilmouth Interiors from C1 (once)*) builds the seven interiors of §8 from BK_AlchemistHouse, each under one root, `Interior C1`, and refuses to rebuild a scene that has it: from then on each interior is the owner's. `BuildReplacing` is for the agent's own batch iterations only. Everything below is an authoring default unless it is marked measured.

**The room kit** is two kit families. `BkRoom` holds the architecture, at ×1.6 along and ×1.44 up since §17 (×1.2 before): walls, doorways, the window, the door, posts, wooden and stone floors, and the ceiling. `BkFurniture` holds the furniture, at game scale ×1.2 since §17 (true size before), plus sets: a table, bench, or shelf with BK's things standing on its top, such as plates and a candle, bottles, books, jars, or lab sets. A shelf, a board, or a painting hangs on a wall's face. **Measured** (render probe, 2026-11-06):

- BK draws a wall only on its local +Z side. A wall between two rooms therefore gets a second face turned to the other room, which is seen and never solid.
- `IntWall02a/b` are brick between timbers, and they line the stone rooms: the kitchen, the laboratory and store room, the lock-up's corridor and cells, the cellar's vaults, and the chapel. `IntWall01a`, both doorways, and the window are plaster, and they line every other room.
- The plaster wall is 0.377 m thick, the brick 0.167 m, the window 0.27 m. A wall's body is capped at 0.3 m (kit standard §2).
- The walls stand 3 m, but their frame tops out just below that. The ceiling was therefore at 2.9 m, which closes a slit of sky seen above the outer walls. After the owner's play test (§15) BK's architecture is stretched upright by 1.2 more: walls 3.6 m, the ceiling at **3.48 m**.

BK's jars of babies and brains are not used.

**Rooms** are rounded to the 4 m module (3 m before §17), on the ground floor only. There are no stairs and no upper floors yet, so the Soot & Bell's let room lies behind a passage, not up a stair.

| Interior | Rooms as built |
|---|---|
| Soot & Bell | taproom 20 × 12 m with the bar, the cabinet, the fireplace, and tables; kitchen 8 × 8 m (stone) with the stove and sink; passage 4 × 8 m with the closed back door to the yard; let room 8 × 8 m with the bed |
| Harrow & Tincture | shop 12 × 8 m with counters and bottle shelves; laboratory 12 × 8 m (stone) with the cauldron, alembic, lab bench, and books; store room 4 × 8 m (stone) |
| Faction's house | hall 16 × 12 m with the rostrum and pews; office 8 × 8 m; archive 8 × 8 m with shelves |
| Wardens' station | front desk room 12 × 8 m; office 8 × 8 m; guard room 8 × 8 m with the stove; corridor 12 × 4 m and three cells 4 × 4 m (stone) behind the owner's bars, widened to 4 m with their door |
| Missing person's room | landing 8 × 4 m (its closed neighbour's door, which led nowhere, removed by the owner after VI-8); the room 8 × 8 m with the bed, wash stand, stove, table, chair, and the cache |
| Cellar | three stone vaults, 8 × 8, 12 × 12, and 8 × 8 m, joined by doorways; barrels, crates, and candles; the circle's place in the middle vault, hidden until a ritual starts there |
| Old chapel | nave 12 × 20 m (stone) with pews, candles, and the rostrum; vestry 4 × 8 m (stone) |

Each room has one warm point light under its ceiling, with no shadows. The street door holds the portal leaf, and the player arrives just inside it. The people are the asset map's coloured Dummies at their posts.

**Counted:** 710 kit blocks in the seven, with no kit fault; 749 after §15 and again after §17.

**Still missing:** stairs and upper floors; arches between the vaults; the cache's contents (the Stash rules of [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3 come with step 4a).

---

# 15. The owner's remarks, 2026-11-06, as fixed

The owner played the built city and interiors and asked for their remarks to be fixed by hand before step 4a, with rules for generating houses and props to follow later. Both passes were rerun by batch at the owner's request (`BuildReplacing`), so the scenes are again the owner's from this build. Every number is an authoring default unless it is marked measured.

| # | Remark | Fix |
|---|---|---|
| 1 | The shop's double door floats | The pack draws its shop door 0.96 m up, on a wooden base end that the bake leaves out. The bake moves the `Double door` down by 0.96 × 1.25 m and the `Door portal` by 0.6 × 1.25 m (measured on the modules at ×1). |
| 2 | Gaps where panels meet | A shop's ground floor is now its shop front to both corners, with no stone corner between fronts. No other gap was found in the renders. The owner is asked to point to any that remain. |
| 3 | The house steps are walked through | Each house door's stoop is three collider steps, 0.17 m high each, following the pack's own fronts (measured: 0.66 × 0.4 m at ×1). |
| 4 | The way to the palace forecourt is walled off, and a person stands in the wall | The Ridge's retaining wall sampled the avenue's ramp. It now stops wherever a street crosses the Ridge's edge. |
| 5 | Running into a wall drops the model head first through it | A house's body now reaches over its pilasters and door frames: a strip 0.16 m proud of the front. It stops 0.16 m short of each end, so fronts meeting in a square's inner corner do not cross. The stoops are solid too. |
| 6 | Trees float | Trees, bushes, and grass stand 0.2 m into the ground. |
| 7 | The city gate flickers, and its lintel sticks out | The lintel is the wall's own thickness, flush with both faces, from 4.5 m over the road up to the wall's top. The tower and wall faces no longer lie in one plane. |
| 8 | Interior ceilings are low, and the camera sticks | See §14: the ceiling is at 3.48 m. Each room has a box collider under its ceiling on Unity's `Ignore Raycast` layer, so the camera stops at it and the Combat Grid does not read it. |
| 9 | Stoves stand out from walls and paintings do not touch them; running at a door drops the player into the void | A piece that stands against a wall (stove, sink, cupboards, chest of drawers, kitchen table, desk, lab bench, alembic) puts its back on the wall's face, 0.19 m from the line, and a painting hangs on that face. Doors are solid leaves in their doorways. A fall guard on the player returns them to the last ground they stood on once they fall 5 m below the scene's lowest collider. |
| 10 | Pavements too narrow | They are wider: 5 m on the High Street, 4 m on Ridge Street and the terrace streets, and 3 m on the others (§2). |
| 11 | Terrace fences should run round the house, further out, with green, trees, bushes, and benches | Each terrace row stands in a railed garden on all four sides, with lawn, shrubs along the front, and a tree and a bench in each side garden of 3 m or more. A villa's railings run round its sides and back, 3 m from the house, with corner trees, shrubs, and a bench. |
| 12 | Corner houses, and no empty corners of odd widths | Shop4, Terrace4, and Tenement4 are baked with a second facade on either side for the ends of a row. A row now fills each free span exactly: it searches the widths for the best fit, and a leftover under one house becomes a yard wall 4 m high. |
| 13 | Storeys' colours should match | A shop house's upper storeys take the wall material of its ground floor's colour set. |
| 14 | The cornice should run along the roof's sides | Each gable end carries the pack's roof cornice without its slope. |
| 15 | Interiors are sparse | More of BK's pieces and sets: carpets, paintings, a dresser, side and round tables, a work table with a cauldron and lab glass, an arms table, shelves of books, a globe, dishes, and baskets. What no pack has is listed for the owner in [SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md). |
| 16 | Generation rules for houses and props | Later, by the owner. |

Other changes in this pass: the arch house into Wardley Garden stands at px 320, with its passage 328–340, and a lamp keeps 8 m from a door measured to its centre.

**Counted** on this build: 3197 kit blocks in the city and 749 in the seven interiors, all with no kit fault, and 277 doorways. Tests pass: `KitLayoutCheckTests`, `PortalPairingTests`, `VeilmouthCityTests`, and `VeilmouthSceneTests` (51 in EditMode), and the door and movement tests in PlayMode (20).

---

# 16. The Dwellings as built, 2026-11-06

Step 4a's first half: the shared Dwelling scene, the generator it runs, and a street door on every dwelling. Every number is an authoring default unless it is marked measured. Breaking in is not built yet (end of this section).

**The journey.** A dwelling's street door is a `Portal` whose journey carries the house as its context, a `DwellingKey`: the street door's id, the street scene, the door's place in decimetres, the house's kind, and its wealth. Every such door arrives at `dwelling.inside` in `Scenes/Slice/Interiors/Dwelling.unity`, which is in Build Settings. There `DwellingRuntime` reads the key, plans the home, builds it from the kit, sets its door to lead back to the street door, and stands the player just inside. Opened alone in the editor, the scene builds a preview terrace.

**The same house, the same rooms.** The plan comes from the slice seed forked by the stream `slice.dwellings` and the door's place, so a house always gives the same rooms and furniture, and houses of one kind differ (12 terraces gave more than 6 different plans). Nothing in the house is kept yet: a picked door and a taken Stash are kept for the run once breaking in exists.

**Rooms** on the 4 m module (3 m before §17), ground floor only, the street door along the front; each plan is mirrored left to right half the time:

| House | Rooms as built |
|---|---|
| Tenement | one room 8 × 8 m, or that room and a stone kitchen 4 × 8 m |
| Terrace | hall 4 × 12 m, parlour 8 × 8 m, stone kitchen 8 × 4 m, bedroom 12 × 4 m |
| Shop | shop room 12 × 8 m with the street door, back room 8 × 8 m, stone kitchen 4 × 8 m |
| Villa | parlour 8 × 8 m, hall 4 × 12 m with the street door, dining room 8 × 8 m, study 8 × 8 m, stone kitchen 8 × 8 m, bedroom 8 × 8 m behind the study (§18) |
| Warehouse | stone store 12 × 12 m, office 8 × 8 m |

**Wealth** ([19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3) comes from where the house stands, in plan coordinates: the Ridge (py < 600) is rich; tenements and warehouses elsewhere are poor; a house in Lower Veil (px ≥ 470 and py ≥ 690, or px ≥ 545 and 600 < py < 662) is poor; the rest are middling. It chooses the furniture each room may take, the Lock (2, 4, or 6, [10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.1), and the Stash chance.

**Furniture** by each room's kind and wealth, from the same `BkFurniture` kit as §14:

- Pieces go against walls first, then in the middle, then free, then hung. A doorway keeps the cells along its segment clear, and behind them the two its 1.76 m opening gives onto (§17). A window or door segment takes no wall piece, and each wall segment takes one.
- A solid piece keeps 0.16 m from a wall's line, the clearance a wall's 0.3 m body leaves. Carpets take no cells.
- The bed is `Kit_Furniture_WallBed`, the Tripo bed with its back on the wall's beams (`Anchor.Back`, at game scale since §17): the centred bed can never clear a wall.
- What a room asks for and cannot place is listed for review, never shown; on the samples only a villa parlour's second painting.

**The Stash** is BK's trunk, in the first of the bedroom, study, room, back room, office, store, and kitchen that the house has: in half the poor homes, 60 % of the middling, 70 % of the rich (400 houses each, within 8 points). It is a marker only; what it holds comes with S5a.

**The doors.** `VeilmouthDwellings.AddDoors` puts a street door at each baked house's door marker, under `Dwelling doors` in `Veilmouth City`, unless one of the seven already opens there. It reads the house's kind from its baked prefab: Shop, Terrace, Tenement, Villa, Warehouse, and an arch house as a terrace in stucco and a tenement otherwise; civic buildings, the tavern, and the chapel are not dwellings. The city's build adds them, and the menu *GameDev → Slice → Dwellings → Add Dwelling Doors to Veilmouth (once)* added them to the owner's scene and refuses to run again. **Counted:** 265 doors, each with its own id: 101 middling and 19 poor shops, 61 tenements, 12 terraces, 10 villas, 62 warehouses.

**Checked:** 56 sample homes, seven kinds at their wealth, eight each, with no kit fault, each with its rooms, at least two pieces a room, and a door that leads back. Renders of 14 are in `Logs/SliceSurvey/dwelling_*.png` (*GameDev → Slice → Render Dwellings*). Tests pass: `DwellingTests` with the city's, kit's, and portals' tests (63 in EditMode); a PlayMode test walks into a terrace, a tenement, a shop, a villa, and a warehouse of Veilmouth and back out (26 in PlayMode).

**Not built yet**, each waiting on its milestone; the owner chose to wait for them rather than build stand-ins ahead (2026-11-06):

- The lock: a dwelling's door opens freely. Picking it is a Reaction Check of Theft and Dexterity with Lockpicks (10.3 §6.1), which needs the Reaction Check and inventory of S5a.
- Witnesses who see a break-in from outside, and residents inside: Vision Cones and the Notice Meter of S3.
- A witness fetching a Warden, and the Arrest: walking townsfolk of S5c and dialogue of S2 ([10.4](../../../10.4%20Dialogue%20and%20Social%20Checks.md) §6).
- The Stash's contents, and a picked door and taken Stash kept for the run: S5a and the save of S5c.
- Residents at home by their schedules: S5c.
- Stairs and upper floors, as in §14; a plain bed for poor homes ([SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md)).

---

# 17. The owner's second play test, 2026-11-06, as fixed

The owner played the city and interiors again and gave ten remarks with screenshots. Both passes, the interiors, and the kit were rebuilt by batch at the owner's request, so the scenes are again the owner's from this build. Every number is an authoring default unless it is marked measured.

| # | Remark | Fix |
|---|---|---|
| 1 | The potions fall through the alchemist's shelves; check that every prop has a collider | BK's potions carry their own dynamic rigid bodies. The kit now removes a vendor model's rigid bodies and joints with its colliders, and the wrapper's box is what is solid (kit standard §4). Every kit block has a collider, and no dynamic body is left in the seven interiors. |
| 2 | The bed, benches, cupboards and tables are small beside the character, and the interiors are cramped | Furniture and loose props stand at game scale ×1.2 (kit standard §1): the bed is 2.3 m long, the bench and notice board larger. BK's architecture is ×1.6 along and ×1.44 up, so its wall is a **4 m module**, 3.6 m high, with a doorway 1.76 m wide; every room of §14 and §16 grew by a third, and the ceiling stays at 3.48 m. |
| 3 | The painting still floats; perhaps it is levelled by the timber beams, not the wall's surface | It was. **Measured** from each wall's mesh: the plaster between the beams is 0.127 m behind them at ×1, and BK's plaster walls stood 0.28 m further back than its brick ones at ×1.6, so a painting on the beams' line was 0.43 m off the plaster. Every BK wall, doorway and window is now set by its infill plane, 0.05 m from the line, and what hangs on a wall hangs on that plane; a piece against a wall puts its back on the beams, 0.25 m from the line (kit standard §2). Every hung piece in the seven was measured with its back on the plaster. |
| 4 | The steps to some houses have gaps and mixed styles | A house's steps are one style, the stoop's own `Porch` material, in treads of the stoop's width and the kit's least run, 0.28 m, each rising by an equal step down to the ground with no gap; where the ground is lower than the stoop's foot, a filled plinth of the same material closes it. |
| 5 | On slopes the fence stands upright, not along the slope; props do not follow the ground | A prop's seen model tilts to the ground's plane under its footprint, at most 25°, and a fence or railing pitches along its length only. Its block, pivot and body stay level on the grid, so the layout check and the Combat Grid read it as before. Lamp posts, columns, the notice board, graves and stands stay plumb; plants are sunk as before. |
| 6 | Holes in fences where no way in or out is planned | Railings are 2 m segments with a 1 m segment (`RailingShort`) for an odd metre, and a gate is left only before a door within 5 m that faces the railing. A railing is set over any prop already there, but never through a house, so it now stops at a neighbouring house. |
| 7 | The Wardens' board floats and is off-centre on its wall | Hung pieces are two-cell blocks centred on their 4 m wall segment and on the infill plane (remark 3). The rostrum and the bar cabinet stand with their backs on the wall. |
| 8 | Interiors feel stuffy | See remark 2. |
| 9 | Gaps where two different assets meet; the ground floor mixes two assets, and the whole of it should be the blue one | A shop's ground floor is now `WoodenWallBlue` all round: its fronts and back, its side walls, and both corners of each front and back, mirrored at each end, so no stone or plaster panel meets the shop front on that floor. |
| 10 | The notice board is very small beside the character | It stands at game scale, 3 m wide. |

**What this costs**, for the owner's review:

- An interior doorway is now 1.76 m wide and lets **two** Combat Grid cells through where it stands over a node, so chokepoints are wider; the grid check expects two there and one over a cell's middle.
- Aligning by the infill brings the plaster walls' end posts up to 0.46 m into the room at the nodes.
- A prop tilts only in its model: on a steep slope its level body may stand slightly proud of the ground at one end.
- Railings end at a neighbouring house rather than leaving a gap before it.

**Counted** on this build: 3284 kit blocks in the city, 277 doorways, and 749 in the seven interiors, all with no kit fault; 56 sample Dwellings with no kit fault; the grid check passes (railings 1498 of 1499 low enough, 750 of 750 crossings closed, house fronts closed). The stale 3 m BK prefabs and the old-size furniture, which nothing used, were removed from the kit.

---

# 18. The owner's third play test, 2026-11-06

The owner played again and sent four screenshots with remarks and wishes. What could be fixed in the kit was fixed the same day; the rest is recorded here and in [99 Open Questions](../../../99%20Open%20Questions.md) (VS-PHYS, VS-PLOT, VS-ROOMS, VS-BEASTS, FG-TRANSIT, FG-CAB), and waits for the owner's word on its design.

| # | Remark | Where it stands |
|---|---|---|
| 1 | Bottles float over the shelf | Fixed: a set's things stand on the surface a ray finds under them; the shelf's brackets had raised its bounds above the board. Every thing of every set now sits on its base (measured, gap 0.000 m). |
| 2 | Paintings sink into the wall | Fixed: paintings and boards stand 0.04 m off the plaster, in every interior and Dwelling, since they are kit prefabs. |
| 3 | Railings stand too close to houses, have holes, and steps run past them onto the pavement | Built with 4 (§19): railings stand 2 m or more from the house, without a hole, the only opening a 3 m gate opposite the door; every door's steps end 1.5 m or more inside them (least measured: 1.78 m). |
| 4 | Each railed house should stand alone on its own green ground; only shopping streets and the slums build in rows; rich residential quarters have gardens with a bench, trees, shrubs, and flowers | Built (§19, VS-PLOT): the Terraces and the Ridge stand as detached houses on railed green plots with trees, shrubs, poppies, and a bench; §3.3, §3.4, and §5.2 changed to match. |
| 5 | Every home needs beds, and living rooms must be furnished | Fixed in the Dwelling generator: a villa has a bedroom behind its study, and a shop's back room has the shopkeeper's bed; tenements and terraces had beds. Rooms keep their furniture lists (§16). |
| 6 | All interiors look the same; assemble rooms at random like a construction set | Recorded (VS-ROOMS); later. |
| 7 | Tables, chairs, crates, barrels, and small things should have mass and gravity, fall, roll, and later break | Built the same day, at the owner's choice, by the kit standard's §4 (*Loose props*): they sleep where they stand until touched, a set's things come off it, the player pushes them, and townsfolk shoulder them aside. Breaking is V2 (VS-PHYS). |
| 8 | City transport, carriages as cabs, street animals | Recorded for later (FG-TRANSIT, FG-CAB, VS-BEASTS). |
| 9 | A warning from `VolumetricLight` | Fixed: BK's script, which needs the built-in pipeline, is removed from the kit's lamps and candles. The Tripo Bridge's socket error is the bridge's own: its server was already running, as when its window is opened twice. |

---

# 19. Detached houses (VS-PLOT), 2026-11-06

The owner, after the third play test: every house with railings is a building of its own on its own ground; only shopping streets and the slums build in rows; the rich residential quarters give each house railings all round, one gate opposite its door, and a green garden with a bench, trees, shrubs, and flowers; the railings never have holes and keep a clear distance from the house. The agent reads the rich residential quarters as the Terraces (§3.3) and the Ridge (§3.4). Every number below is an authoring default.

**A plot** is a rectangle on the 1 m grid holding one house:

- **Railings all round, without a hole.** The only opening is the gate, 3 m wide, opposite the house door; nothing else in the line is left out. Two plots side by side share the railing between them.
- **Clear of the house:** the railings stand at least 2 m from the house's back and sides, and the front garden is at least 4 m deep. The house stands on the ground with no raised floor, so its steps end at least 1.5 m inside the railings and never cross them.
- **A garden:** grass over the whole plot; a small tree in each back corner where the side garden is 3 m or more; shrubs along the side railings, one in four a flower (`Poppy`, Polytope Studio, already in the kit); flowers along the front railings either side of the gate path; a bench in the wider side garden. Nothing stands on the 3 m path from the gate to the door.
- **The house** has windows on both sides and its back, since it is seen all round: the Terraces' house type and the villa are baked with both side facades.

**The Terraces** keep their four rows on Terrace Street West and East (§2): two plots of 22 m on each row, 607–651 along the street, a house of 10 × 10 m in the middle of each, front gardens of 4 m to the pavement. The arch house to Wardley Garden (§7) stands on its own on the west row of Terrace Street West, unrailed, since it is a passage and not a home, with a small green beside it.

**The Ridge** has seven villa plots on Ridge Street's north side, the houses set back to give front gardens of 5 m: four of 29 m between 330 and 446, one of 25 m and the faction's house in the largest, 36 m, between 556 and 617, and two of 34 m between 629 and 697.

This replaces the terrace rows railed round as a block and the villas railed on three sides of §17. Shopping streets, the High Street, Lower Veil, and the docks keep building wall to wall.

---

# 20. The owner's fourth play test, 2026-11-06

The owner walked the detached houses and sent two screenshots with remarks. What the kit and the builders could fix was fixed the same day; the smaller city is a new plan and waits for the owner's acceptance.

| # | The owner saw or asked | Answer |
|---|---|---|
| 1 | The plots' railings have holes | The railing was scaled uniformly by the mean of its length and height, so when its height came down to 1.04 m for low cover its length fell short of 2 m and every joint left a gap. It is now 2 m long whatever its height (`SliceKitCatalogue`). |
| 2 | Some things hang in the air and fly apart at a touch | See kit standard §4: every loose prop in Veilmouth and in every interior must rest where it stands, checked by letting the scene fall for 3 s (`LoosePropsPass.RestCheckBatch`, `LoosePropsTests`), and the player's push is an impulse cut to what brings a prop to its speed limit, so a light thing is nudged, never shot away. |
| 3 | Everything flies from the player; benches, tables, and big barrels must not | Real masses (a bench 60 kg, a tavern table 50, a full barrel 140) and a walker's push of 200 N: with friction 0.6, what weighs over 34 kg does not slide; a chair, a stool, a basket, a box still do (kit standard §4). |
| 4 | The bridge's joints have holes | The embankment stopped 1.4 m short of the bridge's parapets at both corners, over the river. It now runs on to them; the deck runs 2 m on to the land at each end, and an abutment fills the bank under each end down to the river bed. |
| 5 | The bar should stand nearer the wall and its keeper, so the taproom has more floor | The bar stands 1.5 m before its back cabinet, the keeper between them (§8). |
| 6 | The market street where the player starts is too long; more full crossroads with pavements and lamps; the slice's city may be 1.5 to 2 times smaller, since the city will later be built on the island | The owner chose ×0.6, the ground shaped again, and every place kept with fewer repeats; C2 (§21) is proposed and waits for the owner's acceptance ([99 Open Questions](../../../99%20Open%20Questions.md) VS-SIZE). |

---

# 21. C2: the smaller city (proposed), 2026-11-06

**Status: accepted by the owner, 2026-11-06, Ridge at 4 m; built the same day (below).** The owner after the fourth play test (§20, row 6) chose: the city ×0.6, the ground shaped again from the new plan (the owner's hand changes to it, if any, are lost), and every place of C1 kept with fewer repeats. The map is [VEILMOUTH_CITY_PLAN_C2.svg](VEILMOUTH_CITY_PLAN_C2.svg). Plan coordinates stay those of C1 (world X = px − 500, Z = 500 − py), so the area, the river, the sea, the fields, the forest, the ore hill, and the beast territories stay where they are. Every number is an authoring default.

**What stays as C1:** the zones and their character (§3), the street sections (§2), the furniture rules (§4), the plots and the seven that open (§5), the detached houses (§19), the interiors (§8), and the river, the bridge, the basin, and the sea quay.

**What changes:**

- **Size.** The city is 300 × 204 m (px 480–780, py 626–830), C1's 500 × 340 at ×0.6. Streets keep their widths, so blocks shrink more than the city: the rows are 10 m deep, as built.
- **Crossroads.** The High Street (300 m) and Mercers Row with Bell Street (220 m) are crossed every 40–90 m: by Terrace Street and Cheap Street (one line, px 516–528), by Crown Avenue at the Circus and Market Street south of it (px 604–616), and by the Soot Stair and Dock Road (one line, px 700–712); the West Gate and the bridge close the High Street's ends. Each crossroads has pavements, kerbs broken for the crossing, and lamps at its corners.
- **The Ridge is 4 m high**, not 8 m: the avenue climbs from the Circus (py 715) to Ridge Street (py 674), 4 m over 41 m (9.8 %, C1's was 9.3 %), and the Soot Stair falls 4 m over 46 m. At 8 m the avenue would climb 20 %. *The owner's word is needed on this.*
- **The Ridge** (py 626–680): Ridge Street (662–674) from the West Gate's side to the North Gate's road; north of it the old chapel with its forecourt (Candidate 3) and its graveyard in the north-west corner, two villas, the Governor's Palace (the centre only, no wings) on its forecourt at the head of the avenue, the faction's house in its 36 m plot, and two villas more: four villas, C1 had seven. The North Gate moves to px 742–756, at the end of Ridge Street.
- **The slope** (py 680–726), between the Ridge's wall and the High Street: the Terraces west of the avenue, two detached houses on Terrace Street and Wardley Garden (24 × 28 m); the Town Hall (Civic12) and the Assize with the Wardens' station (Civic12) flanking the avenue; Under the Ridge east of it, with Ridge Lane, its tenements, the Soot Stair, and the cellar yard (Candidate 2).
- **The High Street** (py 726–744) from the West Gate to the bridge, shops wall to wall on both sides, Crown Circus (40 m) round its column at the avenue's foot.
- **South of it** (py 744–830): the Market Quarter west, with the Market Square (50 × 26 m) between the High Street and Mercers Row, Tanners Slant at 45° from the High Street to Cheap Street (§12), and the Masters' stalls in the square; Lower Veil east, with Bell Street, the Soot & Bell and Harrow & Tincture on its north side, the Ritual Square (Candidate 1) and Loom Lane with the missing person's tenement and the pawnbroker; Dock Road, the Customs House at the basin's head, and the sea quay.
- **Fewer repeats**: one arch house (Lower Veil's, into the tenement court); the Terraces' arch house to Wardley Garden is dropped, the garden opening on Terrace Street by a gate.
- **The ground** is shaped again from C2 by the tool of §9, the low town at 25.9 m and the Ridge at 29.9 m; the roads out leave at the West Gate (py 735), by the bridge (py 735), and at the North Gate (px 747).
- **The player starts** on Bell Street before the Soot & Bell, as in C1.

**As built, 2026-11-06.** `VeilmouthGround` (*Shape Veilmouth Ground from C2 (once)*) shaped the ground again from C2 and replaced the `C1 Map` by the `C2 Map`; `VeilmouthCity` rebuilt the city under `Veilmouth C2`, removing `Veilmouth C1`. The Ridge is 29.9 m and the low town 25.9 m; the avenue's halfway and the Soot Stair's are 27.9 m. Beyond §21's list: Wardley Garden opens north on the *Garden Walk*, a cobbled path of 4 m under the Ridge's wall from Terrace Street, since the Terraces' plots close its other sides; *Stair Lane* carries Ridge Lane's tenements on from the cellar yard to the river, and *Ridge Alley* joins Ridge Lane to the High Street; the Soot & Bell's yard lies beside it on Dock Road; the old chapel's postern is dropped. Every crossroads of §21 has a lamp on each pavement corner by both kerbs, unless a door or another lamp is within its reach. The builder's stone works are now written into their mesh assets in place: copying a new mesh into an asset left the old one on screen for the rest of the session, so the first renders showed C1's walls. Result: 2349 blocks, 0 kit faults, the least room between a door's steps and its railings 3.18 m; every loose prop rests (0 of the scene's and the interiors'); grid check PASS: every street site reaches 635 cells or more, 77 of 77 house fronts closed, railings 830 of 830 sides low cover and 415 of 415 crossings closed. There are 77 dwellings, C1 had 269. One townsman first stood by the Town Hall's podium with no room to walk a round; he walks the avenue's mall instead.

---

# 22. Victorian Interiors for every interior, 2026-11-07

The owner chose Victorian Interiors for every interior, the seven and the Dwelling scene, wholly: its floors, walls, ceilings, doors, window walls, stairs, fireplaces, furniture, bathrooms, books, pictures, clocks, plants, curtains, lamps, and candles. Another pack's prop stays only where a room's purpose needs it and the pack has none. §14 and §16 describe what stands until the rebuild. The rebuild is planned in [VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md):

- a 3 m module with walls 3.5 m, used at ×1.0, **measured**;
- rooms by period and wealth;
- a fireplace wherever a stove stood;
- bathrooms by wealth;
- rich houses on two or three floors, as many as they show outside;
- curtains, lamps and candles lit by the clock, and the sun through the windows.

It runs in five iterations, each on the owner's authorisation. VI-2 rebuilt the seven, VI-3 gave the Soot & Bell its upper floor, and VI-4 rebuilt the Dwellings (2026-11-07): §14's and §16's rooms, module and furniture are replaced by that plan's §7 and §8. The journey, the seed stream, the street doors and the Stash's chances of §16 stand.

---

# 23. Garden ornaments, 2026-11-08

The owner added two models, a stone birdbath and an angel pouring water, to be used in the city now, and the same day a second set: two more angels of other proportions and a great marble lion, with the wish of three different angels in different corners of the city, each in a green place, the lion near the palace or elsewhere as a show of power and greatness, two or three of them, and many birdbaths in rich yards and gardens. Fountains and other ornaments will follow. `VeilmouthOrnaments.Place` sets them in the scene in its own group, `Ornaments`, and run again replaces only that group. Each takes the grid node nearest its wished place whose footprint stays inside its garden or forecourt, on ground even within 0.35 m, with 0.5 m clear all round for walking (authoring defaults); the pass reports its node and renders it from the front:

| Ornament | Where (px, py of its north-west corner) | Why there |
|---|---|---|
| The angel pouring water | the heart of Wardley Garden (557, 696), facing its gate on the north; it takes the place of the garden's middle tree | §6.1 asks a square for a fountain or a statue; the garden is the city's one green square of the middle class |
| The kneeling angel pouring a jar into a basin (2 × 2 m) | the old chapel's graveyard (511, 645), by its south wall, looking north over the graves | the north-west corner; a graveyard angel |
| The standing angel pouring a jar into its bowl (1 × 1 m) | the east villa's front garden on the Ridge (730, 654), facing Ridge Street | the north-east corner, the green of the rich |
| A pair of lions (4 × 3 m each) | the Governor's Palace forecourt, either side of the avenue's axis (600, 653) and (616, 653), heads west, looking down the avenue | the crown's power where the avenue ends; the forecourt is the city's most seen place |
| Birdbaths | the front gardens of all four Ridge villas (528, 652), (581, 652), (673, 652), (708, 652) and of the faction's house (636, 652); the gardens of both detached houses on Terrace Street (513, 700), (530, 697); Wardley Garden, either side of the angel (549, 697), (565, 697) | rich and middle-class gardens; the faction's house keeps the villas' manner |

Each stands on a grid node on the highest ground under it, rounded up to 0.1 m. Sizes are authoring defaults: the angels 2.2, 2.6 and 2.4 m tall with their bases, the lion 3.5 m, the birdbath 0.95 m. The owner's models all face north (+Z) unturned, as the renders show; the first angel had stood with its back to its gate and is now turned to it. "Злачные районы" was read with the owner's next words, a show of power and greatness: the lions stand at the palace, not in Lower Veil. A third lion was tried before the Customs House, but the free ground nearest it lay in Bell Street's row of shops, so it was left out; two lions are inside the owner's two or three. The poorer districts take none; a pump and a horse trough are still wanted there (asset map §9 row 22).

---

# 24. C3: the kit city, 2026-11-08

**Status: asked for by the owner on 2026-11-08 and designed by the agent the same day; the build is recorded in §24.7 when it stands; it waits for the owner's walk ([99 Open Questions](../../../99%20Open%20Questions.md) VS-C3).** After accepting the Victorian show streets ([VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md)), the owner asked for Veilmouth's plan to be reviewed and improved directly on its scene, with these goals:

- the map and the way streets and districts are formed are remade;
- roads, pavements, squares and districts take realistic sizes;
- the city's area stays the same, so the gain comes from placement;
- the city and the land round it read as a Victorian fantasy of the 18th to early 20th century with a little mysticism;
- the game and the world share one style through geometry, textures and the models themselves.

The owner's answers the same day:

| Question | Answer |
|---|---|
| Houses | The owner's Victorian Kit everywhere in the city at ×1.25, civic buildings included. It replaces Modular Victorian City. |
| Mysticism | A crimson moon and fog, a Gothic skyline and occult signs. "Alchemy and soot" was not chosen. |
| Ground | Shaped again from C3 by §9's tool; the owner's hand changes to it, if any, are lost. |
| Windows | Real rooms behind them ([VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md) §4.4). |

The map is [VEILMOUTH_CITY_PLAN_C3.svg](VEILMOUTH_CITY_PLAN_C3.svg). The style that binds the city, its interiors and the land round it is [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md).

Plan coordinates stay those of C1 and C2: world X = px − 500 and Z = 500 − py. Every number is an authoring default.

## 24.1 What C2 got wrong

Real Victorian streets set the measure:

- **Byelaws:** the model byelaws of the 1870s asked about 36 ft (11 m) for a new street, and London about 40 ft (12 m) for a carriage street.
- **Carriages:** a carriage is about 2.2 m wide, and two pass in 6 m.

Against that measure, C2 had these faults:

- **Narrow carriageways.** Ridge Street, Terrace Street, Mercers Row and Bell Street had carriageways of 4 m, where two carriages cannot pass.
- **Pavements wider than the road.** The High Street had pavements of 5 m and the side streets 4 m beside starved carriageways.
- **An oversized avenue.** The avenue was 36 m wide in a city 300 m across, a third of the slope between the Ridge and the High Street.
- **No breaks or yards.** Houses stood wall to wall for 50–100 m with no passage, and rows stood back to back with no yards.
- **A small, misplaced market.** The market square was 50 × 26 m and lay off the axis that the palace and the avenue set.

## 24.2 Street sections

| Street | Total | Section | Surface |
|---|---|---|---|
| High Street | 18 m | 5 m pavement, 8 m carriageway (two carriages pass), 5 m pavement (§25.2; C3 had 4 + 10 + 4) | setts; asphalt waits for a texture |
| Crown Avenue | 24 m | 3 m, 6 m, a 6 m mall of trees and benches, 6 m, 3 m | setts; the mall in cobbles |
| The Parade (C2's Ridge Street) | 12 m | 4 m pavement, 5 m carriageway, and a 3 m promenade along the Ridge's retaining wall, whose parapet is its balustrade (§25.2; C3 had 3 + 6 + 3) | setts |
| Byelaw streets: Terrace Street with Cheap Street, Market Street, Dock Road, Mercers Row, Bell Street | 12 m | 3.5 + 5 + 3.5 m (§25.2; C3 had 3 + 6 + 3) | setts |
| North Road | 10 m | carriageway only, to the North Gate | setts |
| Lanes: Ridge Lane, Stair Lane, Loom Lane | 6 m | shared, no pavement | cobbles |
| Garden Walk | 4 m | a path under the Ridge's wall | cobbles |
| Passages | 4 m | through a row, open to the sky; the kit has no arch to pass under (3 m in C3; 4 m by reference plan R5) | cobbles |
| Soot Stair | 8 m | flights and landings between blind walls | cobbles |
| Tanners Slant | 8 m | at 45° under a `KitFrame` (§12) | setts |

Kerbs and lamps follow §4. A passage is 4 m wide, a real way to the yard behind (reference plan R5; C3 had 3 m). Like every alley, it runs at right angles (§12). Every pavement is a raised island, 0.15 m above the carriageway, with a stone kerb, broken only by carriageways (reference plan R4). On 2026-11-09 the owner widened the pavements from the carriageways, with no house moved (§25.2). The wider streets the reference plan proposes would still need a new layout (C4).

## 24.3 The plan

Coordinates are plan metres, px × py.

**The Ridge** (py 626–680, 29.9 m):

- **The Parade** runs from the chapel's forecourt to North Road (480–742 × 668–680). North Road leads to the North Gate (742–752 × 626–668); the gate's opening is 740–754, with towers at 735–740 and 754–759.
- **North of the Parade**, every plot is railed on the line of its pavement (py 668). From west to east they hold:
  - the old chapel with its forecourt (480–502 × 650–668, Candidate 3) and graveyard (503–530 × 628–650);
  - villas in 530–558 and 558–586;
  - the Governor's Palace (592–632, front at py 650) behind its forecourt (586–638 × 650–668), with the lions;
  - the faction's house in 638–676;
  - villas in 676–707 and 707–738;
  - the gate lodge east of North Road (754–778).
- **A villa** is a house of 15 × 15 m with a front garden of 6 m and a back garden running to py 628.

**The slope** (py 680–726) is low town except the avenue's ramp:

- **Crown Avenue** (600–624 × 680–726) climbs 4 m over 46 m (8.7 %) from the High Street to the Parade. The Crown Column stands at the foot of its mall.
- **The Town Hall** (582–597 × 686–716) faces the avenue from a podium over the ramp. Its **clock tower** (592–597 × 721–726) stands at the corner of the avenue and the High Street: six storeys, about 26 m, the tallest thing in the city.
- **The Assize and the Wardens' station** (627–642 × 686–716) face the Town Hall across the avenue.
- **The Terraces**, west of the avenue:
  - Terrace Street (516–528 × 680–726);
  - four detached houses of 10 × 10 m on plots 16 m along the street (500–516 and 528–544, each × 684–700 and 700–716);
  - Wardley Garden (546–570 × 684–712), which opens north on the Garden Walk (528–572 × 680–684);
  - the Town Hall's yard (570–582 × 686–716).
- **Under the Ridge**, east of the avenue:
  - Ridge Lane (642–700 × 696–702) and the Soot Stair (700–708 × 680–714);
  - the cellar yard (709–721 × 690–726, Candidate 2), with the cellar door in the stair's east wall;
  - Stair Lane (721–778 × 696–702);
  - tenements 7.5 m deep along both lanes, with yards behind them;
  - Ridge Alley (669–673 × 702–726) down to the High Street.

**The High Street** (py 726–744) runs from the West Gate (opening 728–742) to the bridge. Its shops stand four storeys wall to wall, broken by passages, and its south side opens on the Market Place.

**The Market Quarter** (py 744–830, px 480–606):

- **The Market Place** (550–606 × 744–784, 56 × 40 m) lies on the Crown Axis.
  - It opens on the High Street to the north and on Market Street to the east, and Mercers Row (480–550 × 772–784) enters its west side.
  - It holds the Masters' stalls, the trader, the seer, and the Item Creation yard in its south-west corner.
- **Cheap Street** (516–528 × 744–812) runs south to the sea, and Tanners Slant runs from (486, 744) to (516, 774).
- **Market Street** (606–618 × 744–812) carries the axis on to the Sea Quay.
- **Warehouses** face the Sea Quay, with yards behind them.

**Lower Veil** (py 744–830, px 618–778):

- **Bell Street** (618–701 × 772–784) holds:
  - the Soot & Bell (680–695 × 757–772), with its yard (695–701 × 757–772) on Dock Road;
  - Harrow & Tincture (670–680 × 762–772);
  - the Notice Board at the yard's gate;
  - the player's start, before the tavern.
- **The Tenement Court** (646–660 × 754–762) is reached by passages from the High Street and from Bell Street, which makes it a stealth route.
- **Loom Lane** (618–701 × 796–802) holds the missing person's tenement (662–672 × 802–810) and the pawnbroker's corner (690–696 × 802–810).
- **The Ritual Square** (630–656 × 784–810, Candidate 1) lies between Bell Street and the sea, and Loom Lane crosses it.
- **Dock Road** (701–713 × 744–812) is lined with warehouses.
- **The Customs House** (745–775 × 769–784) stands on the Basin Quay (713–780 × 784–790).

**The edge** stays as it was:

- the town wall and the West Gate;
- the river and its embankment, and the Veil Bridge;
- the basin and the Sea Quay.

Only the gate towers change: they become kit towers.

**Axes and breaks.** The Crown Axis runs from the palace through the avenue, the clock tower's corner and Market Street to the sea at px 612. A long row is broken at least every 40 m, by a street, a passage or a square.

## 24.4 Houses from the kit

The houses are the Victorian Kit at ×1.25.

**Scale.** At ×1.25 the module is 2.5 m and the storey 3.75 m. Every street door stands on a plinth of 0.75 m, with the kit's own stone steps and rails. **Measured:** `SM_Vic_Steps_Single` reaches 1.1 m out and 0.6 m up at ×1, so 1.375 m and 0.75 m in the city.

**Modules stay separate.** Each module stays a prefab instance; no mesh is merged and no atlas baked (the kit's rules).

**Baked types.** `VeilmouthKitHouses` bakes each house type once as a project prefab of those instances. Each prefab carries:

- its rooms behind the windows ([VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md) §4.4), whose furniture keeps its own size under the house's scale;
- the kit standard's body and front colliders;
- a `Door` marker at each street door, so the city places it as it placed C2's houses.

**Sizes on the grid.**

- Widths are whole multiples of 5 m (two kit modules), so rows stay on the 1 m grid.
- A pitched roof spans at most 6 m of the kit, so a tenement is 7.5 m deep on a plot 8 m deep.

| Type | Kit plan (m) | In the city (m) | Storeys | Roof | Where |
|---|---|---|---|---|---|
| Shop house | 8 × 8, 12 × 8 | 10 × 10, 15 × 10 | 4 | mansard with lucarnes | High Street, Market Place |
| Town house | 8 × 8, 12 × 8 | 10 × 10, 15 × 10 | 3 | mansard | side streets |
| Tenement | 8 × 6, 12 × 6 | 10 × 7.5, 15 × 7.5 | 2–3 | gable or hip | Under the Ridge, Lower Veil |
| Detached house | 8 × 8 | 10 × 10 | 3 | mansard, porch | Terraces, gate lodge |
| Villa | 12 × 10 with a 4 × 2 pavilion | 15 × 15 | 3 | mansard, porch, balcony, vane | the Ridge, the faction's house |
| Civic | 24 × 10 with an 8 × 2 pavilion | 30 × 15 | 3 | mansard, porch | Town Hall, Assize, Customs House |
| Palace | 32 × 10 with a 12 × 2 pavilion | 40 × 15 | 3 | mansard, porch, balcony, vane | the Governor's Palace |
| Tavern | 12 × 12 | 15 × 15 | 3 | mansard | the Soot & Bell |
| Warehouse | 12 × 6 | 15 × 7.5 | 3 | gable | quays, Dock Road |
| Tower | 4 × 4 | 5 × 5 | 5–6 | hipped pyramid | gate towers, clock tower |

**Facades.**

- A house's party walls are plain. The house at a row's end shows a second facade on the side street, as C2's corner houses did.
- Each district takes its own palette from the kit's: rich districts in stucco and rich brick; middling ones in common brick; poor ones in stock or poor brick with oxblood, green or brown woodwork.
- The kit has no shop front, so a shop shows double doors and double windows on its ground floor until a shop front is made (asset map §9).

## 24.5 The mystic layer

The details are in [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md) §6.

**Sky and light:**

- a crimson moon that rises over the sea in the south-east by night;
- a twilight grade at dusk;
- gas lamps with a cold green-blue flame.

**Fog:** low fog along the river, the basin, the docks, Lower Veil's courts, the graveyard and the Soot Stair.

**Skyline:** the clock tower and the gate towers stand over the roofs. Spires, gargoyles and veiled statues wait for models (asset map §9).

**Old wards:**

- **Where:** faint signs over the keystones of chosen doors, and runes on old walls: the town wall by the gates, the retaining wall, the Soot Stair's walls, and the three Ritual Site Candidates.
- **What they are not:** they carry no rule and are not the traces of a Weak Point ([21](../../../21%20Cult%20Campaign%20and%20City%20Control.md) §4).

**Strange windows:** a dim violet-green glass in Harrow & Tincture and a few shops of Lower Veil.

## 24.6 What stays

- the seven buildings that open, with their portals;
- the Dwelling doors and the Masters;
- the Ritual Site Candidates, each within its own place;
- the furniture rules (§4) and the detached plots (§19);
- the ornaments (§23), which move with their gardens;
- outside the city: the ground, the forest, the fields, the hill and the beasts.

## 24.7 As built

C3 was built once on 2026-11-08 by `VeilmouthCity.RebuildC3`. The run:

- shapes the ground (`VeilmouthGround`);
- builds the city;
- places the dogs (`SliceDogs`), the ornaments (`VeilmouthOrnaments`) and the mystic layer (`VeilmouthMystic`);
- writes the survey renders (`VeilmouthRenders.RenderC3`, `RenderMystic`).

From here on the ground and the scene are the owner's to change by hand. The agent reruns nothing unless the owner asks (AGENTS.md, *Slice assets*).

**The city.**

| What | Count |
|---|---|
| kit blocks | 2 799 |
| street doors | 111 |
| street surfaces | 62 |
| baked house types (`Assets/Prefabs/SliceKit/KitHouses`) | 55 |
| dogs | 5 |
| ornaments (§23) | 14 |
| kit faults | 0 |

- The least room between a door's steps and its railings is 2.6 m; §19 asks for 1.5 m.
- The last fixes before the clean check:
  - a bay's collider box starts at the front's face;
  - the Market Place's west row stands at px 551, with a 3 m yard behind it;
  - the weapons stall, the barrels, a fence and a court lamp moved off the houses.
- The rooms behind the windows turn off their furniture's kit blocks (`VicRooms`), so the kit check (`KitLayoutCheck`) skips a turned-off block.
- The builder loads the kit's wrappers (`SliceKitCatalogue.Load`) and never bakes them again. A rebake under the owner's newer packs and editor had turned the lockup's cell bars into another model with no collider; the wrappers were restored from git.
- The wrappers on disk must keep their file IDs. A rebuild of a composite set gives its parts new IDs, and the rooms behind the windows turn those parts off by ID, so after one their furniture woke again: props fell, colliders and kit blocks came back. The Victorian kit test therefore reads the wrappers (`SliceKitCatalogue.LoadVictorian`) instead of rebuilding them. After any deliberate rebuild of the catalogue, bake the kit houses again (`VeilmouthKitHouses.BakeAllBatch`) and then the city.
- The city stands Dummies at the named people's places, and `SlicePeople.PlaceAll` puts the roster's people in their stead, Edda Brann at the weapons stall among them.
- `SliceWorldSetup.AddToVeilmouth` gives the Notice Board, by the Soot & Bell's yard, its tips (S1.3).
- The player's map (S1.6) is `Assets/Data/Slice/Map/VeilmouthC3Plan.xml`, copied by hand from the C3 plan without its legend, heights, Crown Axis, start or Ritual Site Candidates. POIs A, B and C keep their places: the C3 doors stand within 3 m of them.
- The slice's EditMode tests pass, 100 of 100, and its PlayMode tests 39 of 39 (about 10 minutes); neither run rewrote a wrapper.

**Houses.** `VeilmouthKitHouses` bakes each house type once as a prefab of separate module instances, with no merged mesh and no atlas (§24.4).

- A four-storey shop house holds about 1 870 module instances.
- One LOD group per house holds only its downpipes and their fittings, brackets, chimney pots and small rails, and cross-fades them out below 4 % of the screen, 320–410 m with the player's 60° view. Every other piece, surrounds, cornices, bands, sills and quoins among them, is drawn at every distance (reference plan R2 step 2).
- The rooms behind the windows switch on within 48 m of the camera and off beyond 53 m (`VicRoomsSwitch`, R2 step 3). Between 32 m and 46 m the glass turns to the mapped room, so the switch happens behind opaque glass.
- The rooms are plain renderers (R1.3): one game object for each mesh a block shows, with no wrapper, collider, body, light or script, since nobody enters them. The city has 92 114 game objects where it had 138 158, the same 80 652 renderers, and its first load takes 7.9 s in the editor where it took 11.8 s, and 2.0 s in a development build (3.9 s on its first run), with every door 0.4–0.5 s there (measured, reference plan R1.4).
- Attic rooms stand under the mansard's flat only. Carried to the outline, as at first, their walls stood out through the slopes and the lucarnes.
- Each house's `VicWindowRooms` tells every pane which built room stands behind it, so between 32 m and 46 m the glass turns to a mapped room of that room's own finish, depth, walls, floor and ceiling (R2 step 4). 1 715 of the 1 762 panes have a room; the rest keep a made-up one.
- `VeilmouthKitHouses.RedressAll` gives the baked houses their LOD and builds their rooms again in place, without baking the houses again: a bake gives every piece a new file ID, and the strange windows override pieces by ID.
- A raised door has a doorcase of two 3 m pilasters, its stoop and its lintel trims; villas and civic houses keep the stone balcony above it (R6). The porch's columns and bare beam are gone: the kit's balcony slab, 0.86 m deep, cannot roof columns 1.32 m out, and `VeilmouthKitHouses.StripPorches` took them out of the 8 baked types in place.
- The 55 prefabs take about 132 MB of text on disk.

**Streets (reference plan R4–R5, 2026-11-09).** `VeilmouthCity.RebuildStreets` rebuilds the city, people, ornaments, mystic layer and reflections on the ground as it stands, without shaping it again.

- **Pavements:** `VeilmouthPavements` builds raised islands, 16 closed outlines and 6 525 m² in all, 0.15 m above the carriageway.
  - The top is the kit's paving. Every edge has a light-stone kerb, 3 292 m in all, and the islands carry one mesh collider on the Ground layer.
  - Corners where carriageways meet are rounded at 2 m (27), and 11 carriageway ends are carried across the pavement's line.
  - A wet band 0.5 m wide runs along 2 079 m of gutter, on NoGrid.
  - Houses, doors, stoops, lamps and props stand on the pavement's top.
- **Passages:** a terrace leaves a passage of 4 m where it meets a building, with a railing at its back that has a 2 m opening; the yard walls are gone.
  - Built: 22 passages and 18 spans left open.
  - No gap under 4 m stands between buildings of a row, by the city's own report.
- **Shop goods (R7):** beside 32 of the 37 shop doors a crate, a barrel or a box stood in the second metre from the front, clear of the stoop; a villa's balcony over its door has iron rails. The goods were taken away on 2026-11-09 by the owner's word (§25.6).
- **Counts:** 2 862 kit blocks after R7 (2 830 after R6, 2 858 before the porch columns' colliders went), 93 houses with 101 street doors (C3: 103 with 111), and no kit fault.

**The mystic layer** stands under its own root, `Veilmouth Sky` (WORLD_STYLE_GUIDE §5–§6). It is only seen: it has no collider and no kit block, and all of it is on the NoGrid layer. The Combat Grid, sight and stealth never read it (test `TheMysticLayerIsOnlySeen`).

| Part | As built |
|---|---|
| Sky and light | `VeilmouthSky` follows the game clock. One directional light is the sun by day (05:00–20:00, at most 42° high, intensity 2, golden from 15:00). By night (20:00–05:00) it is the crimson moon: 6–28° high in the south-east, a soft red at intensity 0.15, fading in and out over an hour. The project renders in Linear. Three painted skies (day, dusk, night) of the project's `GameDev/Slice/Sky` shader over Free HDR Sky's two cubemaps are blended, the sunset's painted sun turned to the light; trilight ambient, an exponential haze in the horizon's colour and the environment reflection's strength follow the hour (reference plan R3). |
| Shadows | The sun's shadow map is 4 096 px over 150 m in four cascades (10, 30 and 70 m), with a 20 % border and soft filtering at high quality (R2 step 7). |
| Reflections | The painted sky, baked alone at noon, is the scene's environment reflection. 70 street probes are baked at noon, 3 m over the ground, one in each open 30 m cell. Each probe's box is 34 × 50 × 34 m with a 4 m blend, and the probes stand under their own root, `Veilmouth Reflections`. The clock dims both toward night, since they hold the day (`VeilmouthReflections.Bake`, R2 step 5). They are baked again whenever the city is rebuilt. |
| Moon | A separate disc about 4° across, drawn after the sky. The painted skies have no sun disc of their own by day. |
| Grade | A post volume graded in HDR (Neutral tone mapping, saturation −22, warm highlights and cool shadows, bloom over HDR highlights) and a twilight volume (violet shadows, amber highlights), weighted by dusk. URP's screen-space ambient occlusion runs on the renderer. |
| Gas lamps | 106, each with one teal point light (range 10 m, intensity 10), lit in the windows' lamp hours. The lamp glass turns to an emissive gaslight material. |
| Fog | 27 banks of unlit particles, 1–3 m deep over their ground: the river, the basin, the Sea Quay, Dock Road, Loom Lane, Tenement Court, the Soot & Bell's yard, Pawn Corner, the graveyard and the Soot Stair. Pale and thin by day, violet at dusk, blue-grey and thickest by night. |
| Wards | 27. Door signs over about one door in eight in Lower Veil and Under the Ridge. Runes by the west and north gates, on the retaining wall, on the Soot Stair's parapets, and by the three Ritual Site Candidates. |
| Strange windows | 4: Harrow & Tincture and three shops of Lower Veil, in a dim violet-green glass, a material variant of the kit's poor glass. |

- **Measured:** soft particles faded every fog puff to nothing in the renders, so the fog material has none. The puff's own soft edge hides where it meets the ground.
- The fog, lamp and sky numbers are authoring defaults.

**Still wanted** (asset map §9, style guide §6.2): spires for the chapel and a church, a clock face, gargoyles, statues with covered faces, pointed lucarnes, a shop front, and a porch roof, a door hood and an iron-and-glass canopy for the doors (`SLICE_MODELS_WANTED.md`).

**The owner's walk** (2026-11-08) found six faults: the yard walls between houses, a minute per door, the bare porches, pops with distance, pavements painted at road level, and a palette too bright. Their causes and the plan to reach the owner's reference images are in [VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md). Its first step is done: a journey now loads behind the veil at High priority, and the way back into the city fell from 52 s to 10.5 s (measured). The owner then chose to keep the city loaded behind its interiors (the plan's D3 (a)): every door now takes about 0.5 s each way, and only the first load, 12 s, remains (measured). The editor and the game now draw at the quality level High (the plan's D5), where the editor had played at Very Low, the main cause of the pops the owner saw.

**Open until the owner's walk:** 99 VS-C3.

---

# 25. Greenery, 2026-11-09

Asked for by the owner before S3a.1: the city's trees and bushes from the Stylized Trees Pack, retinted to the city's range so they stand out a little without shouting; bushes also as whole green figures (hedges, beds, a maze); grass on the city's earth off the roads; trees along the pavements, which are widened; and Roadside Tales dressing the land outside the walls, chiefly along the roads. The owner chose to widen the pavements from the carriageways. Every number below is an authoring default unless it is marked as the owner's.

## 25.1 Trees and bushes in the city

- **Source** (owner): the Stylized Trees Pack's `Birch 001–003` and `Warped 001–003`, and its bushes. Better trees come later (owner). Since §26.1 (row 11) Polytope's fruit tree, 9.9 m, takes the birches' place, and since §27 Veilmouth's own trees from Blender take every city tree's place.
- **Colour** (owner: our range, some contrast, a feeling of green): the city's trees and bushes wear project copies of the pack's foliage (`Foliage 001`, `002`, `004`) whose gradient runs from a dark green (0.12, 0.16, 0.10) through (0.30, 0.37, 0.20) at 0.78 to a light (0.56, 0.60, 0.36), in sRGB. That is darker and greyer than the pack's teal and lime, and a little greener than the forest's olive. Only the `City*` wrappers wear the copies; the forest's fellable trees keep the pack's colours as resource markers (§6.2).
- **Size**: about 8 m high, smaller than the forest's 9–13 m, so a crown stays under the eaves of a three-storey row.
- **Where**: they replace the Polytope fruit trees inside the walls, on Crown Avenue's mall, the palace forecourt, Wardley Garden, and the plots' gardens. Bushes `006`, `011` and `015` at 1.4 m replace the Polytope shrub in the city's gardens. Outside the walls Polytope stays the cover.

## 25.2 Wider pavements

**Superseded** by the owner's fifth play test (§26.1, row 6): the trees and lamps moved onto the carriageway, and the pavements went back to their widths before this section.

The owner's choice: the width comes from the carriageways, and no house moves.

| Street | Before | Now |
|---|---|---|
| High Street (18 m) | 4 + 10 + 4 m | 5 + 8 + 5 m |
| The Parade (12 m) | 3 + 6 + 3 m | 4 m pavement, 5 m carriageway, 3 m promenade |
| Byelaw streets (12 m) | 3 + 6 + 3 m | 3.5 + 5 + 3.5 m |

Crown Avenue keeps its section, its mall already planted. A 5 m carriageway still lets two carriages pass at a walk. The lamps stay 0.5 m from the kerb.

## 25.3 Street trees

Since §26.1 (row 6) a street tree stands on the carriageway in a square pit flush on the kerb, not on the pavement. A city tree stands by the kerb in the lamps' line on the High Street, the Parade's pavement and the byelaw streets, about every 11 m (the owner's screenshot), so lamps and trees take turns. A tree stays at least 4 m from a lamp. Like a lamp, it never stands within a door's 8 m reach, nor before a stoop, by shop goods, in a kerb's rounding or in a passage's mouth. Until the tree grate on the wanted list comes, a tree stands in the paving.

## 25.4 Hedges, beds and grass

- **A hedge** is the pack's low round bushes (`019`, `020`) clipped to 0.8 m and set end to end on whole metres. Since §26.1 (row 4) it is walked through and gives no cover, as a bush.
- **The plots' gardens**: a hedge runs inside the front railings, broken at the gate's path, in place of the row of shrubs and flowers.
- **Wardley Garden**: four hedged beds round a cross of paths, north to south at px 557–561 and east to west at py 695–699. The benches, the corner trees and the angel stay.
- **The palace forecourt**: a parterre of hedged beds either side of the Crown axis's way.
- **A maze** is not built: no place in C3 holds one. It stays an option for the owner.
- **Grass**: Polytope's grass clump grows on the gardens' green earth and in the beds, about one to 4 m², never on a path.

## 25.5 Roadside Tales outside the walls

The pieces are chosen by meaning and measured from the pack's meshes (`tools/assets/fbx_sections.py`, the finest level, m):

| Use | Pieces | Measured | Joined |
|---|---|---|---|
| Dry-stone walls along the West road by the farms' fields | `Cobble_01b` | wall 3.31 m long, its fallen stones 6.33 m | every 3 m, the walls overlapping 0.31 m (the pack's demo pen joins them every 3.28 m) |
| The flax field's fence, in place of the medieval props' fence | `ByAFieldField_01a–01c` | 2.21 m | scaled along to 2 m, end to end |
| The farm yards | `Plank_01a–01c` | 1.56 m | scaled along to 2 m, end to end |
| Vegetable beds at the farms | `VegetablePatch_02a–02b` | 2.38 m | scaled along to 2 m, end to end |
| Posts on the verges of the North and West roads | `Wooden_Pin_01j` | 1.38 m high | every 40 m |

Fences and walls are low cover, 0.8–1.4 m high. The pack's gates, ends, corners and its stone gate are not used yet: a way in is a gap, and the stone gate's opening is not measured.

## 25.6 Fences and loose things in the city (owner)

The owner, 2026-11-09, after the greenery's first run:

- **No field fence inside the walls.** A plot, a yard or a bed is closed by a hedge (§25.4), the city's iron railings, or a stone wall. The medieval props' plank fence and Roadside Tales' field fences stand only outside the walls (§25.5). The Item Creation yard on the Market Place is hedged.
- **No barrel, crate or box set down for its own sake.** In a city barrels stand in the port and in warehouses; in a home a barrel is rare. So the goods before the shops (§24.7, R7.1) are taken away, and so are the barrels, crates, boxes and firewood of the Soot & Bell's yard, the tenement court, the cellar yard and the Item Creation yard. The quays' cargo and the warehouses' stock on Dock Road stay. Indoors the rule is [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) G5.
- **The owner's Tripo models furnish interiors only.** Each holds up to about 10 000 polygons (the owner; the table lamp 14 410, measured), too many for the city's open plan. Their places are in the furnishing rules §10. The wooden hanging sign among them is the one exception: by the owner's word one hangs beside the street door of Harrow & Tincture and of the Soot & Bell, its bracket on the facade and its board's foot 2.6 m above the sill (`VeilmouthSigns`), blank until the trades' emblems come ([99 Open Questions](../../../99%20Open%20Questions.md) VS-SIGN (b)).

## 25.7 Build

`VeilmouthCity.RebuildGreenery` (batch) builds the city again on its present ground, with the passes beside its root. It then places POI A's and POI B's stages and the next day's residue again, since they stood among the old walkers and on the old kerb line, and renders the `green_*` views. What it built is recorded under 25.8 after the run.

## 25.8 As built, 2026-11-09

`VeilmouthCity.RebuildGreenery` ran once, after the owner's word of §25.6 (log `Logs/greenery.log`, views `Logs/SliceSurvey/green_*`). Counted in the saved scene:

| Built | Count |
|---|---|
| City trees, birches and warped trees of the Stylized Trees Pack | 135, of them 106 street trees (§25.3) |
| Hedge pieces, `019` and `020` at 0.8 m | 379 |
| City bushes in beds and gardens (`006`, `011`, `015`) and shrubs | 200 and 191 |
| Grass clumps and poppies | 1 568 and 99 |
| Roadside Tales outside the walls: dry-stone wall, field fence, farm-yard planks, vegetable beds, posts | 184, 31, 56, 40, 24, in 4 farm yards |
| Pavements (R4) | 16 outlines, 7 749 m², 3 332 m of kerb, 2 109 m of wet gutter |
| Barrels, crates and boxes in the city | 3, 10 and 11, all on the quays and by the Dock Road warehouses |
| Plank or field fence inside the walls | none |

The city has 4 820 blocks, 101 doors and 62 lanes, with no kit fault. One city tree gave its place to an angel of §23. The fruit trees and pines (191 and 356) are Polytope's, on the land outside the walls, as before.

Not yet seen by the owner: the city waits for the owner's walk with the rest of C3 ([99 Open Questions](../../../99%20Open%20Questions.md) VS-C3, VS-GREEN).

# 26. The owner's fifth play test, 2026-11-09

The owner walked the greenery of §25 before S3a.4 and sent twelve remarks. By the owner's word, what concerns the city is fixed now (§26.1, §26.3); what concerns the land outside the walls goes into the plans (§26.2), and S3a.4 starts only after both. The street section was the owner's choice: the pavements go back, and the trees and lamps stand on the carriageway. Every number below is an authoring default unless it is marked as the owner's.

## 26.1 The city, fixed

| # | The owner's remark | Fix |
|---|---|---|
| 1 | A door by the Wardens' station cannot be entered: the ground blocks it | A back door opens only when its stoop, the steps down from it and a landing of 0.8 m beyond stand clear of houses, plinths, walls and ground over its floor; then it gets its steps and, like every door, a portal (§26.3). Any other back door is walled up with the kit's plain wall in the house's paint, its steps hidden (`VeilmouthCity.BackDoors`) |
| 2 | Not every house can be entered; the Town Hall's two columns stand over its door's arch | A civic house's arched door has a wide doorcase whose pilasters stand clear of the arch (`VicAssembly.Doorcase(wide)`). Every door seen now opens: a dwelling's into its Dwelling; a civic building's, the palace's and the clock tower's into an Office (§26.3) |
| 3 | The piano is very small; fit all the Tripo pieces to the world | Measured against the player (1.8 m) in one lineup. The piano is sized by its keys, which stand at 0.41 of its height, to 0.82 m, so it is 2.0 m over its carved top and two cells deep. The wardrobe is 2.4 m, the cheval glass and the screen 2.0 m, the glazed cabinet 2.1 m, the coat stand 2.05 m, and the armchair 1.4 m, as Victorian Interiors' own. A rebuilt wrapper whose footprint changes takes the old one's place, so no stale wrapper is left ([INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §10) |
| 4 | Hedges must be passable; trees stay solid | A hedge is a `NoGrid` block with no collider: walked through, no cover. The trees keep their trunks |
| 5 | Through doors and windows a wall stands right behind a door's middle | The rooms behind the windows are cut round every opening, the doors' too, so no inner wall stands behind a door (`VicRooms`) |
| 6 | (owner) "A tree needs a special bed, as if it stood in the earth, fenced by a square bed; lamps and trees stand on the road, flush on the pavement, not on it" | The pavements of §25.2 go back: the High Street's are 4 m, the Parade's and the byelaw streets' 3 m. A street tree stands in a tree pit on the carriageway, flush on the kerb: a 1.6 m square (1.2 m since §27) of stone kerb 0.1 m wide at the kerb's height, round soil 0.08 m high in the soil layer's colour, so a ring of earth shows round the trunk, 1.1 m across at its foot. A lamp stands on the carriageway 0.3 m from the kerb. Each keeps its block's grid cell: the post or the trunk moves inside the cell with its body, so the Combat Grid reads it where it stands. Trees and lamps keep §25.3's spacing, and stay clear of a door's reach and a lane's mouth |
| 7 | The lines along the kerbs come and go with the camera | The wet band of R4 lay 3 cm over the terrain, which the terrain's own level of detail hid and showed. It is gone |
| 8 | (owner) "The posts at a fence's ends must always form a passage, and an end without a post must meet the wall" | The railing module has one post, at one end. Each run of railing now gets a post at an end that stands free, at a gate or a gap, and loses its post at an end that meets a house, so the railing runs into the wall |
| 9 | Less wind: a slight sway | The scene's wind is 0.07 in strength at a speed of 2.5, with a main wind of 0.6 and turbulence of 0.3, against the pack's 0.2, 5, 1 and 1 |
| 11 | The tree of the owner's screenshot, greener and 1.5 times taller, in the city in place of the birches | Polytope's `PT_Fruit_Tree_01_green` at 1.5 times its height, 9.9 m, in the muted city green (§25.1), is the street tree and three of the six garden trees. The Warped trees stay in the gardens, and the birches are gone from the city. **Superseded** by §27: Veilmouth's own trees |

## 26.2 The land outside the walls, planned

Not built now (owner). It is planned as one land pass before the beasts of S3b.2 ([VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) S3b.0), on the ground the owner keeps by hand (kit standard §7). The pass places blocks and paints the terrain's layers and grass. It reshapes no relief but the far backdrop.

| # | The owner's remark | Plan |
|---|---|---|
| 10a | What are the posts with no collider on the outer roads? | Roadside Tales' `Wooden_Pin_01j`, a verge post every 40 m along the North and West roads (§25.5), set with no body as small dressing. The land pass gives a post a collider where it stays, and takes it away where the entrance's fences come |
| 10b | Narrower outer roads | From 8 m of soil to 5 m, a cart's way with a verge. The city's North Road keeps its setts to the gate |
| 10c | Grass on every green surface with no gaps, thinning naturally near the roads | The terrain's detail layer, the method of [OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md) §5 (Polytope's `PT_Grass_02` as the detail mesh, instanced). The land pass paints it once on the slice's own terrain; it is not the Overworld's bake. It is full on the grass layer, fades over the last 2 m before a road or path, and is absent on soil, rock or paving. It replaces Polytope's grass clumps in the gardens and glades, so the scene has one grass system |
| 10d | The forest almost right after the city | The forest's edge comes to within about 15 m of the walls wherever no road, farm, field or glade stands |
| 10e | A proper entrance: fences along the road and the highway, and signposts | At the North Gate and where the West road leaves the city, Roadside Tales' field fence runs on both verges for the last 60–80 m. The pack's end posts stand at each break, and its gate where a farm track joins. Signposts stand at the forks. No pack in the project has a signpost: it goes on the wanted list, or the owner's Tripo makes one (VS-LAND) |
| 10f | A small village behind the wall, with fields and cows behind a fence, and the forest right behind it | The four farms of the West road (§24.3) become a hamlet: their houses closer together round a lane, the flax field and a pasture fenced with field fence, and the forest behind them. The cows are Shepherd Valley's `P_Cow`, 4–6 of them grazing and wandering inside the pasture. It is an HDRP pack, so its material is converted to URP Lit first. It is not in the asset map yet, so it needs the owner's word (VS-LAND) |
| 10g | A denser forest of bushes and the like, still fightable | Kit standard §7 and 10.1 hold: a tree is a trunk with a collider, an obstacle and Full Cover. The forest stays fightable because trunks stand at least 3 m apart and its glades are 15–25 m across. Between the trunks the undergrowth grows thick: bushes, ferns and saplings with no collider, walked through like the city's hedges and giving no cover. Whether undergrowth hides a crouching character from a Vision Cone is open (VS-FOREST). Thickets too dense to cross stand only away from paths and glades, as solid blocks |
| 12a | A far backdrop of high snowy mountains | A ring of mountains beyond the playable area's steep edge (C0 §2), out of reach. It uses Rowlan's stamp `Alpine Mountains 23`, already in the project, on a terrain of its own outside the slice's. It gets a snow layer once a real snow texture comes: none is in the project ([OVERWORLD_VISUAL_DIRECTION.md](OVERWORLD_VISUAL_DIRECTION.md)) |
| 12b | Rocks and islets from Rock and Boulders 2 at the water's crossings, in the mountains and in the forest | The pack is not in the project. It waits for the owner's import, then enters the asset map and is wrapped as kit blocks. Rocks stand in the river by the bridge, on the coast, on the ore hill and in the forest as Full Cover, never on a path (VS-LAND) |
| 12c | Many paths to the fighting glades and resource spots, with ore and fellable trees gathered round them | Soil paths, 2 m wide, run from the roads to each glade and to the ore hill's veins. The fellable trees ring the glades as now (§24.3), and the ore veins cluster in two or three spots on the hill, each reached by a path. The counts of C0 §2 stay |
| 12d | Hunting grounds with wolves and boars | The six wolf and four boar territories of C0 §2 become the hunting grounds: a glade or clearing each, reached by a path. The boars' are at the forest's edge and by the fields, the wolves' deeper in. They are content piece C3, placed with the beasts in S3b.2 |

## 26.3 The Office and the back doors

- **The Office** (the owner's choice, "a generator: an office") is a kind of Dwelling that is no home. Every door of a civic building and the palace opens into a rich one: a reception at the door where a clerk receives callers, two offices, an archive of shelves and boxes, and a WC. The clock tower's door opens into its keeper's one room. An Office is generated as every Dwelling is, from its house's key ([15](../../../15%20Procedural%20Building%20Generation.md) *The slice's Dwellings*).
- **Back doors**: every house's yard door is either opened onto the ground, with its steps and a portal into the same Dwelling as its street door, or walled up (§26.1, row 1). A building with one authored entrance, as the seven are, walls up its back door. The exception is the Soot & Bell's yard door, which has its own portal (§13). A walking townsperson's or dog's route never climbs a door's steps, front or back: they are no walkway (`SliceTownsfolk.PlanRoute`). A straight stretch of a route is checked for the person's room both on each cell's floor and at the height of the line it walks, so it never cuts a stoop's corner from a step beside it.

## 26.4 As built, 2026-11-09

Built by `VeilmouthCity.RebuildGreenery` after the houses were baked again, the Tripo wrappers rebuilt, the seven enriched and the Dwellings written again; counted in the saved scene.

| Item | Count |
|---|---|
| Kit blocks, doors with a portal, lanes | 4 757, 148 (101 before), 62; no kit fault |
| Door markers that open, and without a portal | 147, none without one |
| Back doors | 47 opened with their steps, 47 walled up |
| Street trees, each in its 1.6 m pit | 110 |
| Gas lamps | 83, each on the carriageway by the kerb or on the Sea Quay |
| Railing posts | 58 added at free ends, 19 hidden against houses |
| Pavements (R4) | 16 outlines, 6 525 m², 3 292 m of kerb, no wet band |

The old 2 × 1 piano wrapper is gone from the Soot & Bell and the project; the piano now stands as a 2 × 2 block on the stage. The two shop signs hang on the grid, their boards moved inside their blocks to where they hung before (§25.6). The pictures of the run are in `Logs/SliceSurvey/pt6/`.

Not yet seen by the owner: the city waits for the owner's next walk.

# 27. Veilmouth's own trees, 2026-11-09

Asked for by the owner after the fifth play test: several different thin trees with leaves, made in Blender with the Easy Tree add-on, optimised and moving in the wind, in the city in place of the present ones; smaller tree pits; on narrow streets trees on one side only, on wide streets on both. Every number below is an authoring default unless it is marked as measured or as the owner's.

## 27.1 The trees

| Tree | Height with its leaves (measured) | Triangles, LOD0 / LOD1 | Crown | Where |
|---|---|---|---|---|
| Street hornbeam A, B | 9.6, 9.2 m | 4 579 / 1 731, 4 310 / 1 754 | upright oval, 3.4–3.7 m across | street rows |
| Street beech A, B | 8.9, 8.7 m | 4 550 / 1 755, 4 579 / 1 797 | upright oval, 3.8–4.0 m across | street rows, Crown Avenue's mall, the palace's corners, gardens |
| Garden beech A | 10.7 m | 7 060 / 2 768 | broad dome, 6.6–6.9 m across | gardens |
| Garden hornbeam B | 10.0 m | 7 060 / 2 737 | broad dome, 6.3–6.9 m across | gardens |

- **Made** by `tools/blender/veilmouth_trees.py` in Blender 5.2 with MTree 5.6 (GPL-3; its code is not shipped, only the meshes it grows), as §27.5 sets out: a trunk and three orders of branches; the leaves are cluster cards, a spray of photographed leaves on short shoots rendered once into a 1024² card of colour, cut-out and normal, one for the beech and one for the hornbeam, cut to an octagon round its leaves. The cards stand evenly in the crown's outer shell, face out of it and lie along its surface. The crown's normals point out from its middle, so it lights as one mass. LOD1 keeps 45 % of the cards at 1.3 times their size, on bark from 2.5 cm thick decimated to 35 % of LOD0's bark budget.
- **Trunks** (measured): 0.38–0.48 m across at the foot for the street trees, 0.56–0.73 m for the garden trees: thicker than the first trees' 0.30–0.36 m, while the owner asked for thin trees. Open (§27.5).
- **Textures**: ambientCG's `Bark001` and leaf sets `LeafSet024` (beech) and `LeafSet014` (hornbeam), CC0, taken from Easy Tree's assets: `Assets/Art/Trees/Textures/`.
- **Wind** (owner, §26.1 row 9: a slight sway): the shader `GameDev/Slice/Tree` moves each vertex in world space by weights the script writes into its vertex colour: a sway that grows with height to the power 1.8, a flutter on the leaves' cards only, and each card's phase. A tree's top leans up to about 0.22 m in the gusts, which breathe about every 7 s and travel along the wind as waves about 105 m long, so a row never sways as one; the leaves flutter about 3.5 cm. Bark and leaves share one wind, so a card never leaves its branch. It is not driven by the scene's `WindZone`, which moves only the Stylized Trees Pack outside the walls; its numbers are `VeilmouthTrees`'. Moving in world space, it survives static batching.
- **Look**: the leaves are cut out at 0.5, their coverage kept at every mip so a far crown does not thin out; drawn on both sides; tinted toward §25.1's muted green; and they let the sun through (0.55) when seen against it. The night grade dims their sky light as it does every material's. They cast shadows and write depth and normals, so SSAO reads them.
- **Import** (`VeilmouthTrees`, an asset postprocessor): an FBX's `_LOD0` and `_LOD1` objects become a cross-fading LOD Group, LOD0 down to 18 % of the screen's height (about 45 m away for a 9 m tree) and LOD1 down to 2.5 % (about 310 m), past which the haze hides it. Its materials are taken by name from `Assets/Materials/Slice/Trees/`.
- **Kit**: `CityTree_<name>` wrappers (`SliceKitCatalogue`, `Kit_Tree_<name>_1x8` to `_1x10`) with a trunk body, so a tree stays solid and Full Cover in a fight (§26.1 row 4). Each tree is turned about its trunk by a hash of its place, so neighbours differ.

## 27.2 Where they stand

- **A street** is planted with one kind, hornbeam or beech street by street, its two variants in turn, so it reads as one planting. Crown Avenue's mall and the palace's corners have the street beech. A garden takes a garden tree, or now and then a street beech or hornbeam.
- **Tree pits** (owner: smaller): a 1.2 m square, from 1.6 m. With its 0.1 m kerb the soil is 1.0 m across, a ring of about 0.3 m round the trunk.
- **One row or two** (owner): a street has trees on both kerbs only where its carriageway is at least 8 m wide. The High Street's 10 m keeps both rows. The byelaw streets' 6 m keep one row, on alternate kerbs street by street, so 4.8 m of carriageway stays clear beside a pit. The Parade keeps its one row.

## 27.3 Build

1. In Blender 5.2, with MTree 5.6 and Easy Tree (for its textures) installed: `blender -b --factory-startup -noaudio --python tools/blender/veilmouth_trees.py -- --out unity/Assets/Art/Trees --seeds 3` writes the six FBX and their textures; `--sheet <png>` renders them side by side, `--only` builds the named trees, `--cards-only` only the cards.
2. In Unity: `VeilmouthTrees.Setup` makes the three materials and imports the trees again; `VeilmouthCity.RebuildGreenery` plants them.

Easy Tree 1.0.1 failed in Blender 5.2 on adding a tree, on its leaves' inputs: Blender 5.2 sets a Geometry Nodes modifier's inputs through `properties.inputs`, no longer by the socket's name. The fix is in the owner's installed copy of the add-on, outside the repository, so an update of Easy Tree replaces it. Since §27.5 only Easy Tree's textures are used.

## 27.4 As built, 2026-11-09

`VeilmouthCity.RebuildGreenery -lookLabel trees` ran once (log `Logs/trees_city.log`, views `Logs/SliceSurvey/trees_*`). Counted in the saved scene:

| Item | Count |
|---|---|
| City trees | 116: street hornbeam A 31, B 25; street beech A 22, B 22; garden beech A 12; garden hornbeam B 4 |
| Street trees, each in its 1.2 m pit | 87 (110 before); 6 narrow streets planted on one side |
| Polytope's fruit trees and the warped trees inside the walls | none; the 191 fruit trees and 356 pines outside the walls stay |
| Kit blocks, doors with a portal, lanes | 4 734, 148, 62; no kit fault |

One garden hornbeam gave its place to an angel of §23. The city's and the kit layout's tests pass, 25 of 25.

Not yet seen by the owner: the city waits for the owner's next walk.

Again with the MTree trees of §27.5, `VeilmouthCity.RebuildGreenery -lookLabel mtree` (log `Logs/mtree_city.log`, views `Logs/SliceSurvey/mtree_*`): the same places, 87 street trees, 6 narrow streets planted on one side, 4 734 blocks, 148 doors, 62 lanes, no kit fault. The city's and the kit layout's tests pass, 25 of 25.

## 27.5 The trees grown again with MTree, 2026-11-09

The owner asked why some of the first trees read as shapeless bushes (many thin twigs sticking out, scattered clumps of leaves, a noisy outline, uneven density) and for a sequence of changes, then for its steps 1–4. Steps 5–7 (the cards' normals, LOD1, wind) were not asked for and are as before.

**Causes, measured on the first trees.** Their cards covered 13–19 % of their square, 8–13 % of that painted stems, and were fans seen edge-on; a card stood on every twig, so leaves clumped (cards in 0.5 m cells spread with a CV of 0.64–0.81, the crown's outer 1 m filled 33–71 %); the hierarchy was deep and even; nothing bounded the crown (the garden beech's stray limb).

**What was built.**

1. *Cluster card.* A flat spray of photographed leaves on short shoots, its side shoots longest in its middle so the spray is an oval (a pointed spray made the outline spiky), the leaves in front of the stems. Measured: the beech card covers 58 % of its octagon, stems 1.1 % of that; the hornbeam card 52 %, stems 2.5 % (the plan asked under 2 %). The plan named MTree's leaf shapes; photographed leaves were used instead, for realism.
2. *Skeleton.* MTree's trunk and three orders of branches. A street tree keeps a leader to 0.8 of its height with 4.5 limbs a metre at 48°, an upright oval; a garden tree's trunk ends halfway up and its limbs rise to a broad dome. Tried and dropped: the plan's first numbers, a trunk to 0.9 of the height with drooping limbs, read as a conifer; a street tree whose few long limbs made its top leaned to one side.
3. *Crown.* The envelope is chosen for each tree from MTree's shapes by the nearest fit to its old silhouette, with a penalty for a crown leaning off its trunk, and the limbs are lengthened or shortened to the old crown's width. The street crowns are ovals now, not the old flat tops. A branch under 2 cm reaching past the crown's own smoothed outline (8 height bands by 12 sectors) is cut, so no bare twig sticks out.
4. *Leaves.* Cards sit only on branches under 1.5 cm, spaced as blue noise in the crown's outer 1.2 m with 15 % deeper: 330 cards for a street tree and 610 for a garden tree, of 0.8 and 1.0 m (the plan said 0.55–0.7 m; larger cards cover the shell within the budget). Each faces out of the crown, lies along its surface and is turned up to 35° about its length, so the outline is not a fringe of edge-on cards. Bark thinner than 1.2 cm is removed under the cards.

**Measured**, LOD0 rendered from four sides with the leaves through their cut-out at 0.5. *Gaps*: sky within each row's span of leaves; *bark*: wood showing there; *outline*: the outline smoothed over 0.25 m against its 2 m mean, over the crown's half width; *spread*: the CV of cover over the 0.5 m tiles inside the crown. The analysis' voxel measure counted card centres, so it favoured many small cards, and was replaced by this one.

| Tree | Gaps, first → MTree | Bark showing | Outline | Spread |
|---|---|---|---|---|
| Street hornbeam A | 0.151 → 0.046 | 0.098 → 0.015 | 0.135 → 0.106 | 0.085 → 0.038 |
| Street hornbeam B | 0.156 → 0.059 | 0.093 → 0.028 | 0.172 → 0.131 | 0.115 → 0.067 |
| Street beech A | 0.168 → 0.056 | 0.051 → 0.018 | 0.177 → 0.115 | 0.113 → 0.048 |
| Street beech B | 0.158 → 0.056 | 0.040 → 0.027 | 0.190 → 0.110 | 0.096 → 0.059 |
| Garden beech A | 0.278 → 0.083 | 0.040 → 0.034 | 0.150 → 0.111 | 0.357 → 0.114 |
| Garden hornbeam B | 0.170 → 0.096 | 0.082 → 0.045 | 0.117 → 0.113 | 0.209 → 0.152 |

Against the plan: the outline's noise fell 21–42 % on the street trees, 26 % on the garden beech and 3 % on the garden hornbeam, where the plan asked 40 %. Budgets are met: LOD0 4 310–4 579 triangles for a street tree (5–6 thousand asked) and 7 060 for a garden tree (8 thousand).

**Open.** The trunks are thicker than the owner's thin trees (§27.1). On 2026-11-09 the owner sent a reference for the tree to be seen most in the city, an open, layered crown on visible natural limbs, and asked for its odds to be assessed with this geometry kept ([99 Open Questions](../../../99%20Open%20Questions.md) VS-GREEN). Assessed in §27.6.

## 27.6 The reference tree, assessed

The owner's reference (2026-11-09): a mature city tree of 10–12 m, a clear trunk to 2.5–3 m splitting into 3–5 natural limbs, an open crown in layers with gaps, elm or lime leaves; LOD0 8–12 thousand triangles, LOD1 4–6, LOD2 1.5–2.5; bark, branches and cut-out leaves; 2–3 shapes in 2–3 sizes; leaves may fill the regions round the branches instead of sitting on twigs. Assessed by a prototype outside the build; nothing in the city changed.

**How studios do it.** A tree's leaves are clusters: a texture of several leaves with their twigs, on cards cut close to the alpha, because overdraw costs more than triangles; leaves hidden inside the crown are trimmed; a farther LOD has fewer, larger leaves; a crown's leaf normals are turned out from its middle so it shades as one body (SpeedTree's Puffiness); 3–5 models of a species, scaled and turned at random. Wind is a vertex shader: a main bend growing toward the top and a detail flutter from vertex colours (Crysis: edge stiffness, a phase for each leaf, overall stiffness, occlusion in alpha), or a branch hierarchy with a pivot for each branch (Pivot Painter); Horizon Zero Dawn and Ghost of Tsushima drive trees from one wind field or gust noise shared with grass and cloth. Leaves are double-sided with an alpha cut and a translucency term. Far trees become impostors (Fortnite's octahedral impostors). No studio's per-tree budget was found in print; an indie artist cites 800–3 500 triangles a tree with two LODs, so the owner's targets are at the high end.

**The prototype.** MTree with a trunk to half the height that thins above the split, so the trunk reads as dividing: first-order limbs from 0.55 to 0.95 of it, 2.5–3 a metre, at 36–42° with up attraction 0.45–0.5, length 5.5–6 m, radius 0.85 of the trunk's, split chance 0.5–0.6; second order from 0.1 of a limb, 4 a metre. Leaves: the thin branches' points sampled by 0.75 m height bands (each band by the square root of its count, so the lower crown gets leaves), 40 clumps by k-means, each an ellipsoid of 0.55–1.0 m half width flattened to 0.7 high; hornbeam cards of 0.6 m on each clump's surface, 3.4 a square metre, those inside another clump dropped; each card faces 55 % toward the sky, as elms and hornbeams hold their sprays, its length out from its clump; its normals blend its clump's outward direction (0.6) with the crown's (0.4). Bark from 1.2 cm, twigs under 2.5 cm cut outside the clumps; LOD1 and LOD2 keep 50 % and 22 % of the cards, 1.35 and 1.9 times larger, on bark from 2.5 and 4.5 cm. Every number an authoring default.

| Prototype (11 m) | Clear trunk | Limbs | Crown | LOD0 (bark + leaves) | LOD1 | LOD2 | Sky / wood seen |
|---|---|---|---|---|---|---|---|
| A, seed 7 | 2.98 m | 6 | 7.9 × 9.2 m | 10 859 (4 499 + 6 360) | 5 172 | 2 104 | 0.219 / 0.038 |
| B, seed 3 | 3.21 m | 7 | 7.9 × 9.1 m | 11 052 (4 500 + 6 552) | 5 135 | 2 092 | 0.217 / 0.050 |
| C, seed 11 | 3.10 m | 4 | 7.0 × 8.6 m | 10 493 (4 679 + 5 814) | 4 762 | 1 900 | 0.195 / 0.096 |

Measured from four sides as §27.5 measures (the street trees there: sky 0.046–0.059, wood 0.015–0.028). Trunks 0.46–0.49 m across at the foot. The coarse outline noise is 0.126–0.141, lumpy as the reference's clumps are.

**Odds.** The form — trunk, limbs, an open crown with gaps, visible wood — and the budget are within reach: the prototypes have them. The reference's rendered leaves are reached only in part: up close the cards read as separate sprays, and its dense small leaves, translucency and soft shade inside the crown need a cluster texture baked from a three-dimensional spray, the Tree shader's translucency and an occlusion term. Tried and dropped: limbs from the trunk's top 20 % (one limb, a blunt trunk end); cards lying along a round clump (rosettes); twigs kept in the crown's gaps (the decimated bark crumpled into shards). Prototype C's bark still shows shards where the decimation stalls; branches would have to be thinned by order before decimating.

**Where it would stand.** A crown of 8–9 m from a kerb pit reaches the facades on the High Street (4 m pavements) and the byelaw streets (3 m); it fits the squares, Crown Avenue's mall and the gardens, and a smaller tree of about 9 m with a crown under 7 m fits the High Street. Open for the owner in VS-GREEN, with whether to build it. The owner then set the tree in Easy Tree by hand: built in §27.7, and again in §27.8.

## 27.7 The owner's mature tree, 2026-11-09

*Replaced by §27.8 the same day: kept as the record of the first build.*

The owner tuned a tree by hand in Easy Tree's Simple Tree Generator, liked it, and asked for it as it is, with only its leaves changed: the project's cluster cards in place of Easy Tree's twigs and leaves. Built as `MatureHornbeamA` by `tools/blender/veilmouth_trees.py` (kind `mature`). Every size is an authoring default unless it says measured.

- **The owner's values** (Easy Tree 1.0.1, `EASY_TREE`): two branches a level, trunk height 2, treetop height 2, 8 levels, branch length 13, random angle 30°, jitter 0.1, gravity −0.15, thickness 3, seed 104; leaves Hornbeam from min height 70, density 1, scale 1–1.5. The generator's tree is 29.4 units high (measured) and is scaled to 11 m.
- **What is taken from it**: its 768 branches as lines, each point with its tube's radius (an output added to the build's copy of the generator: the profile circle's radius times the scale Curve to Mesh gives the point), and its 4 032 twig points with the way each twig grows. Easy Tree's own tubes, 469 thousand triangles at its 12 sides (measured), its twigs and its leaves are not used.
- **Bark**, built from the lines as tubes: each branch to where it thins to 0.6 cm; its line within 6 cm, or 25 cm where it is thinner than 3 cm; 6 sides from 8 cm of radius, 4 from 3 cm, 3 below; ending in a point; the bark texture repeats every metre. LOD1 and LOD2 stop at 3.0 and 6.0 cm, with a looser line and a side fewer. Decimating Easy Tree's tubes to the budget tore them into shards (tried); one tolerance for thin and thick costs 8.9 thousand triangles, the looser line on thin branches 6.1 thousand (measured). Ending thin branches at the leaves' outline cut none, as the twig points reach as far as the branches (tried).
- **Leaves**: 970 hornbeam cards of 0.66 m, the size they were baked at, so the leaves are life-size; on twig points chosen as §27.5 does (blue noise, three quarters in the outer 2 m shell, a quarter inside); each laid along its twig, a little lifted, with half its outward part taken off, so a twig growing straight out of the crown's side does not show its card edge-on (tried without: the crown's sides were slivers). Normals and wind as §27.5.

| MatureHornbeamA (measured) | Size | LOD0 | LOD1 | LOD2 |
|---|---|---|---|---|
| Triangles (bark + leaves) | 11.25 m high, crown 12.5 × 12.6 m, trunk 0.30 m across at the foot | 11 924 (6 104 + 5 820) | 5 016 (2 130 + 2 886) | 1 748 (512 + 1 236) |
| Cards | | 970 | 481 | 206 |

The LODs are within the owner's reference budgets of §27.6 (8–12, 4–6 and 1.5–2.5 thousand). In Unity the tree is `Assets/Art/Trees/Tree_MatureHornbeamA.fbx` with a LOD Group of three, set by `VeilmouthTrees`: LOD0 to 25 % of the screen's height (about 38 m away), LOD1 to 7 % (about 135 m), LOD2 to 2.5 % (about 380 m); its kit wrapper is `CityTree_MatureHornbeamA`. It is checked in Blender's preview renders and by its LOD Group as imported, not yet in the game's view.

**Open.** Where it stands ([99 Open Questions](../../../99%20Open%20Questions.md) VS-GREEN): it is not placed. Its crown is 12.5 m across, wider than §27.6's 8–9 m, so from a kerb pit it reaches the facades on the High Street and the byelaw streets; the squares, Crown Avenue's mall and the gardens hold it.

## 27.8 The owner's mature tree with Easy Tree's own branches, 2026-11-09

The owner looked at §27.7's tree and saw that its cards carried their spray's brown shoots (the card was a photographed spray, `Tree_Twig_Hornbeam`). The owner asked again: Easy Tree's tree as Easy Tree builds it, its bark and twigs kept, with only the leaf's mesh replaced by a realistic leaf. The owner accepts that a leaf may seem to hang in the air. Built again as `MatureHornbeamA` by `tools/blender/veilmouth_trees.py` (kind `mature`), in place of §27.7's build. Every size is an authoring default unless it says measured.

- **The owner's second setting** (Easy Tree 1.0.1, `EASY_TREE`): two branches a level, trunk height 3, treetop height 2, 7 levels, branch length 11, random angle 30°, jitter 0, gravity 0, thickness 3, seed 105; leaves Hornbeam from min height 55, density 2, scale 1–1.5. The owner also added a Subtract in Make Tree Skeleton, so that fewer branches grow. The build makes the same change in its own copy of the generator: the subtrahend of the node it feeds, `Integer Math.013`, becomes −2, and the build stops if that node is not the one it expects. The bark then has the 67 704 faces the owner's editor shows (132 336 triangles; measured). The generator's tree is 24.2 units high (measured) and is scaled by 0.4546 to 11 m.
- **Kept from Easy Tree**: its bark as it builds it, 12 sides, with its UVs; its 2 048 twigs, each a tapered tube of four sides, 52 triangles, with its UVs; and on each twig its 7 leaf places at the golden angle.
- **The leaf**: on each leaf place, one flat quad of a photographed hornbeam leaf in place of Easy Tree's diamond leaf (6 faces, 0.30 units long). Each quad shows a random cell of the LeafSet014 atlas (3 × 2 leaves), which the build copies with its alpha as `Tree_Leaf_Hornbeam` and its normal map as `Tree_Leaf_Hornbeam_Normal`. A quad is as long as Easy Tree's leaf with its stalk, so a leaf is 14.8–22.2 cm (measured): larger than a life-size hornbeam leaf, as Easy Tree's own leaves are. The tree carries 14 336 leaves and no shoot of the spray card. Normals and wind are as in §27.5.
- **LOD1 and LOD2**: Easy Tree's tubes are not decimated, because they tore into shards in §27.7. The bark is built from the branch lines as in §27.7, to 1.5 and 3.4 cm. The leaves are the same single leaves, 14 % and 6 % of them, kept by a fixed hash of each leaf's foot and grown 2× and 2.9× about it. Tried and dropped: cards in the far LODs made clumps beside LOD0's open crown, and a first thinning of 12 % and 4.5 % was too sparse.

| MatureHornbeamA (measured) | Size | LOD0 | LOD1 | LOD2 |
|---|---|---|---|---|
| Triangles (bark + leaves) | 11.3 m high, crown 12.3 × 10.7 m, trunk 0.36 m across at the foot | 267 504 (238 832 + 28 672) | 5 687 (1 789 + 3 898) | 2 268 (660 + 1 608) |
| Leaves | | 14 336 | 1 949 | 804 |

LOD0's bark is Easy Tree's bark and twigs: 132 336 + 106 496 triangles. As imported into Unity it is 255 216 triangles, 12 288 fewer, six for each twig. The owner's 67.7 thousand counts the bark's faces only, which are quads. With its twigs and leaves, LOD0 is about 21 times §27.6's reference budget of 8–12 thousand. That is the owner's choice of Easy Tree's own branches. LOD1 and LOD2 stay within their budgets.

**In Unity** the tree is the same `Assets/Art/Trees/Tree_MatureHornbeamA.fbx`, with the LOD Group of §27.7 (25 %, 7 %, 2.5 %). Its leaves take a new material, `VeilmouthLeaf_Hornbeam`, made by `VeilmouthTrees` as the cards' material is: the Tree shader, cut at alpha 0.5, two-sided, with translucency and wind. Its kit wrapper keeps its GUID and is now `Kit_Tree_MatureHornbeamA_1x12`, named by its new measure.

**Tree wrappers keep their LOD Groups.** A kit wrapper kept only a vendor model's finest level, a rule from play test VI-5, and so it had stripped every tree's LOD Group. A tree now keeps its group (`SliceKitCatalogue.Wrap`). The seven Veilmouth wrappers were rebuilt in place with their GUIDs; the street and garden trees now switch at 18 % and 2.5 % (measured on the wrappers). The vendor trees' wrappers (birch, warped, fruit, pine) were not rebuilt and still lack their groups.

Checked in Blender's renders and in a Unity probe of the imported model and its wrapper; not yet in the game's view.

**LOD0's weight, measured** (at the owner's request, 2026-11-09; a probe outside the build, nothing in the build changed). Easy Tree's Tree to Mesh has a Subdivision Surface after its tubes, but the node is muted in Easy Tree as it ships. The bark is plain 12-sided tubes, and turning subdivision off changes nothing (measured: the same 267 504 triangles). What can be lightened without changing the tree's lines is the number of sides of the bark's profile circle and of the twig's, and the twig's caps:

| LOD0 variant (measured in Blender) | Bark tubes | Twigs | Leaves | Triangles | Vertices | Bark silhouette against the present tree |
|---|---|---|---|---|---|---|
| Present: bark 12 sides, twigs 4 sides with caps | 132 336 | 106 496 | 28 672 | 267 504 | 182 660 | 1 |
| Bark 8 sides, twigs 3 sides without caps | 88 224 | 73 728 | 28 672 | 190 624 (−29 %) | 145 752 | 0.986 |
| Bark 6 sides, twigs 3 sides without caps | 66 168 | 73 728 | 28 672 | 168 568 (−37 %) | 134 466 | 0.972 |

The silhouette is the bark's front view without leaves at 1.3 cm a pixel, as intersection over union. In close views from 3.5 m the 6-sided branches have slightly angular outlines, and the smooth shading hides their faces. The leaves and their places do not change. Even at 6 sides LOD0 is about 14 times the reference budget. Unity would drop about 6 thousand more degenerate triangles at the twigs' tips (calculated, not imported).

What is left is mostly twigs. A twig is 2–4 mm thick at its base (measured: radius 0.003 units, scale 1–1.5, factor 0.4546). At 1080 lines and a 60° view, 4 mm is one pixel at about 3.7 m (calculated). Beyond a few metres the twigs cost 74–106 thousand triangles and draw less than a pixel. Easy Tree's bark at 6 sides with every leaf and no twigs would be 94 840 triangles (the sum of the measured parts), for a level between LOD0 and LOD1. The leaves would then hang in the air, as the owner accepted.

**Open** ([99 Open Questions](../../../99%20Open%20Questions.md) VS-GREEN):

- **Where it stands.** Resolved by the owner, 2026-11-10: along every road (§27.10).
- **LOD0's weight.** Resolved by the owner, 2026-11-10: lighter tubes with the twigs only near (§27.9).

## 27.9 Lighter tubes and the twigs only near, 2026-11-10

The owner chose both measured ways of §27.8: 6 bark sides with 3-sided twigs, and the twigs drawn only near (99 VS-GREEN (b) with (d)). `veilmouth_trees.py` sets them in its copy of Easy Tree's generator (`EASY_SIDES`): the bark's profile circle 6 sides for 12, the twig's 3 for 4, and no twig caps. The build stops if Easy Tree's nodes are not as it ships them. The tree now has four LODs. Its leaves, its lines and the two far levels are as in §27.8.

| MatureHornbeamA (measured) | LOD0 | LOD1 | LOD2 | LOD3 |
|---|---|---|---|---|
| What | Easy Tree's bark at 6 sides, its twigs at 3, every leaf | the same without twigs | 14 % of the leaves, 2× larger, on bark from the lines | 6 %, 2.9× larger |
| Triangles in Blender (bark + leaves) | 168 568 (139 896 + 28 672) | 94 840 (66 168 + 28 672) | 5 687 | 2 268 |
| Triangles in Unity | 162 424 | 94 840 | 5 687 | 2 268 |
| Drawn to (LOD Group of 12.5 m, 60° view) | 80 % of the screen's height, about 13.5 m | 25 %, about 43 m | 7 % | 2.5 % |

Unity drops 6 144 degenerate triangles, three at each twig's tip (calculated in §27.8, now measured). A twig of 4 mm is under a third of a pixel at 13.5 m, so the switch from LOD0 to LOD1 only takes away lines that have already faded. In Blender's front renders LOD0 and LOD1 cannot be told apart. Within 13.5 m the tree is still about 14 times §27.6's budget for LOD0, and from there to 43 m about 8 times. That is the owner's choice of Easy Tree's own branches. LOD2 and LOD3 stay within budget. `VeilmouthTrees` sets the four levels (0.8, 0.25, 0.07, 0.025); the kit wrapper `Kit_Tree_MatureHornbeamA_1x12` was rebuilt in place with its GUID and carries the same group (measured). Checked in Blender's renders and by a Unity probe of the imported model and its wrapper; not yet in the game's view.

Where it stands: resolved by the owner, 2026-11-10 (§27.10).

## 27.10 The mature tree along the roads, 2026-11-10

*The owner, 2026-11-10:* the mature tree goes along the roads, for which it was made first; the parks and the rich houses' gardens keep Veilmouth's earlier trees (99 VS-GREEN, *Where it stands*). The agent counts Crown Avenue's mall, a row of trees down the avenue's middle (§24.2), as a road. The palace forecourt's two corner trees stand in a square, so they keep the earlier trees.

- **Roads.** Every kerb pit of §26.1 and §27 takes `CityTree_MatureHornbeamA` (`RoadTree` in `VeilmouthCity.Greenery`), and so do the mall's three places. The pits, the rule of one row where a carriageway is under 8 m, and the lamps do not change.
- **The crown's turn.** The crown is 12.3 m one way and 10.7 m the other (§27.8, measured). A road's tree turns its narrow way across its road (`RoadYaw`). It is flipped or not by its plan point and turned up to 15° off square (authoring default), so a row never repeats one crown. Turned so, the crown reaches about 0.8 m less toward the houses (calculated: half of 12.3 − 10.7).
- **Parks, gardens and the forecourt.** The rich houses' gardens and the parks draw from the same garden list as before: GardenBeechA, GardenHornbeamB, StreetBeechB and StreetHornbeamA. The forecourt's corners take StreetBeechA or StreetBeechB. StreetHornbeamB now stands nowhere in the city; its model and wrapper stay in the kit.

**As built** (`VeilmouthCity.RebuildGreenery`): 87 mature trees in kerb pits, with 6 narrow streets planted on one side; 4 734 blocks, 148 doors, 62 lanes, 0 kit faults. Counted in the saved scene by a probe that is now removed:

| City trees (measured) | Count |
|---|---|
| The mature tree in kerb pits | 87 |
| The mature tree on Crown Avenue's mall | 3 |
| GardenBeechA, gardens and parks | 12 |
| StreetBeechB, gardens and parks | 5 |
| GardenHornbeamB, gardens and parks | 4 |
| StreetHornbeamA, gardens and parks | 3 |
| StreetBeechA, the forecourt's corners | 2 |

**The crown toward the houses** (the same probe, on LOD1, which has every leaf but no twigs, above 3 m). 66 road trees have a house within 20 m at 6 m up; 24 have none. Across its road a crown reaches 5.35–6.31 m from the trunk (median 5.46 m); along it, 5.41–6.33 m (median 6.24 m). For the part behind a wall, every fifth vertex was linecast from the trunk's axis at its own height. 14 559 of 1 295 712 samples (1.1 %) lie behind a wall, in 41 of the 66 trees. They reach a median of 0.51 m past the wall, 2.99 m at the 95th percentile and 4.52 m at most. The walls hit are mostly house fronts (9 039 samples), the stucco shop (2 245), the city's own stonework (1 767) and the brick terrace (1 137). On the narrower streets the outer leaves go into the front walls, as 99 VS-GREEN's *Where it stands* foresaw. This is its option (b) without the pruning: no crown is cut back.

**Checks.**

- The tests `VeilmouthCityTests` and `KitLayoutCheckTests`: 25 of 25 passed.
- The load diagnostic (`SceneLoadTimingTests`, in the editor with graphics):
  - The city's cold first frame is 8.93 s, against 7.86 s on 2026-11-09 before the tree passes of §27–§27.9. The trees' own share is not separated from the passes in between.
  - A door's journey takes 0.40–0.84 s.
  - The GPU's frame cost was not measured.
- Views: `Logs/SliceSurvey/green_*`.
- One tree shape stands 90 times, varied only by its flip and turn.
- Not yet seen by the owner in the game.

**The crowns that reach into front walls**: resolved by the owner, 2026-11-10, with C4's wider pavements and lower, narrower stoops (§28.1, items 4 and 6).

# 28. C4, the owner's map, 2026-11-10

The owner drew a map of the city's scheme: districts, roads and the kind of building in each. Its title reads *Уэллпорт*; the city stays Veilmouth (owner). C4 replaces C3's layout, its ground and its names. The interiors and the Dwelling scene stay as they are. Sections §24–§27 stay as the record of C3.

## 28.1 The owner's decisions

1. **The map is a scheme.** C4 keeps its layout and where things stand, not its scale, its curves or its size. The geography stays:
   - the river runs round the west and the south and into the sea on the east;
   - the one way in from the land is the West Gate on its bridge, and the one way out is the port;
   - (1) the centre, (2) the religious quarter to the north, (3) the residential district to the west, (4) the port to the east, (5) the works to the south over the river;
   - across the river stand a few village houses, and then the forest begins.
2. **Straight streets at right angles, with no curves.** The only round place is the centre. From it four straight streets run at right angles: north to the religious quarter, south to the works, west through the residential district and east to the port. One short street runs at 45°. Every other street is straight, so that the kit fits its houses at the corners.
3. **Size.** C4 is not the map at 1:1. The residential district is cut down hard, and so is the port's north-east. The city has 100–120 buildings.
4. **Street sections** (accepted; the pavements under trees are finally sized by the mature tree's measured crown, about 6 m):

   | Street | Carriageway | Pavements | Trees | Total |
   |---|---|---|---|---|
   | Main street (gate → centre → port) | 10 m | ≈ 6 + 6 | two rows | ≈ 22 m |
   | Ring | 8 m | ≈ 6 + 6 | two rows | ≈ 20 m |
   | Side street | 7 m | ≈ 6 + 3.5 | one row | ≈ 16.5 m |
   | Embankment | 7 m | a promenade of ≈ 6 m | one row on the promenade | ≈ 13 m and a parapet |
   | Works road | 9 m | 2.5 + 2.5 | none | 14 m |
   | Lane, slum alley | 5 m, shared | none | none | 5–6 m |

   No tree stands in the slums or the works.
5. **Trees.** The mature tree is built again with the owner's new Easy Tree setting, for more visible leaves (§28.3).
6. **Stoops.**
   - A house that stands straight on the pavement, with no railings, has no steps, or one low and narrow step.
   - A wealthy house may have steps, but they reach at most a quarter of the pavement's width (the owner allows a third or a quarter; C4 takes a quarter). Today's reach half the pavement.
   - A house with its own railed yard keeps its steps inside the yard.
   - The houses stand lower: the plinth follows the steps.
7. **Wealth** falls with distance from the centre. The slums lie in the works, by the port, and a little by the religious quarter.
8. **The centre.** At its middle stands a statue of the city's patron saint, 10–15 m high, as a placeholder until a model comes. Round it stand the Town Hall with its clock tower, the Assize with the Wardens, guild halls and the palace. The market adjoins the main street.
9. **The religious quarter** holds a great cathedral (a placeholder), with parks round it and all that goes with them, the monastery and the cemetery. The faction's house stands by the cathedral.
10. **The three ritual sites** keep their kinds: a square in Lower Veil, a cellar yard in the works, and the old chapel by the cemetery.
11. **The works** are filled with plain building boxes with colliders and no props of their own. Chimneys, machines and factory modules come later.
12. **The port** follows the map: quays and piers, warehouses and the Customs House from the kit, and a lighthouse from a kit tower. No ship stands until a model comes.
13. **Water and relief.**
    - The river and the sea are one water plane at 24 m.
    - The river has stone embankments in the city and natural banks outside it.
    - The relief is gentle: the centre and the cathedral stand on a low rise, and rocks line the sea.
14. **A stone town wall** closes the city where the river and the sea do not, so the city has one gate.
15. **The outskirts.** Six to ten cottages from the kit stand across the river, and then the forest begins. Fields, cows, the mountains behind and the hunting grounds of S3b.0 come later.
16. **Names follow folk toponymy** (*Folkloric Street Naming*). Streets, lanes, places and corners are named as if by the residents, with a folkloric, ironic edge. The names are of five kinds:
    - nicknames, such as *Bald Boar* or *Lame Goose*;
    - ironic ones, such as *Dandy Hairpin* or *Drunken Lamp*;
    - grim ones, such as *Sly Nook* or *Black Stair*;
    - everyday ones, such as *Old Crane* or *Three Barrels*;
    - mysterious ones, such as *Nameless Man's Lane* or *Quiet Whistle*.

    Each name is a string key with its Russian and English text. Veilmouth, Lower Veil, the Soot & Bell and Harrow & Tincture keep their names.
17. **Performance.** The owner allows Unity 6's GPU Resident Drawer and GPU occlusion culling. A merged far LOD for each house type is allowed only if the budget is still not met with everything above.
18. **Placeholders** go on the wanted list ([SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md) § *C4 placeholders*).
19. **The ground** is shaped again; the owner's hand changes to C3's ground are dropped.
20. **How the work runs.** The steps are:
    1. the tree;
    2. the plan;
    3. the ground and the water;
    4. the streets;
    5. the houses;
    6. the districts;
    7. the outskirts;
    8. the slice's places and the checks.

    Each step ends with a commit, a push and views. The agent does not stop for confirmation and takes the best decision for correctness, accuracy and performance (owner).

## 28.2 The agent's readings (authoring defaults)

- **The count.** 100–120 is every building inside the wall: houses, civic buildings, the cathedral, the port's buildings and the works' boxes. The village is counted apart.
- **The round centre.** A paved circle round the statue lies within a square frame of houses whose corners are right angles. The four streets leave from the middle of the frame's sides. The 45° street leaves from its south-east corner for the port and Lower Veil.
- **Stoops in numbers.** A step is 0.15 m high and 0.3 m deep.
  - A house straight on the pavement has none, or one step no wider than its door.
  - A wealthy house has up to the quarter of its pavement: one step on 3.5 m, up to five on 6 m.
  - A house with its own railed yard keeps the kit's steps inside its plot.
  - A house's plinth is as high as its steps, and the house is set down by the rest.
- **The beasts' territories and the gathering places** move into the forest across the river.

## 28.3 The mature tree's third setting, 2026-11-10

The owner found the leaves too faint and gave a third setting in Easy Tree:

- Branch Length 13 for 11;
- Random Angle 40° for 30°;
- Min Height 65 for 55;
- Leaf Min and Max Scale 1.5–2.5 for 1.0–1.5.

The other values are as in §27.8. `veilmouth_trees.py` builds the tree as in §27.9. Easy Tree scales each twig with its leaves by Leaf Min–Max Scale, so our leaves grow with it. The build's report now measures the leaves it makes; it used to print a range worked out for the scale of 1.0–1.5.

| MatureHornbeamA (measured) | Second setting (§27.9) | Third setting |
|---|---|---|
| Size | 12.28 × 10.69 × 11.30 m | 12.74 × 11.61 × 11.40 m |
| Twigs, leaves | 2 048, 14 336 | 2 496, 17 472 |
| Leaf with its stalk (5th–95th percentile) | about 15–22 cm | 20.5–32.4 cm |
| Trunk at the foot | 0.36 m | 0.32 m |
| LOD0 in Unity (bark at 6 sides, twigs at 3, every leaf) | 162 424 | 195 792 |
| LOD1 (no twigs) | 94 840 | 113 424 |
| LOD2, LOD3 | 5 687, 2 268 | 6 305, 2 748 |

The LOD Group keeps §27.9's levels, LOD0 to about 13.5 m. The kit wrapper `Kit_Tree_MatureHornbeamA_1x12` was rebuilt in place with its GUID (`VeilmouthTrees.SetupMature`).

**The crown's reach from the trunk** (LOD1 above 3 m, measured in Blender):

- the wide way: 6.51 m and 6.22 m at most, 5.03 m and 4.86 m at the 95th percentile;
- the narrow way: 6.12 m and 5.49 m at most, 4.35 m and 4.19 m at the 95th percentile.

In C4 a road tree turns its narrow way across its road, its shorter side toward its own pavement. Its trunk stands 0.6 m into the carriageway from the kerb, so a 6 m pavement puts a facade 6.6 m from the trunk. That is beyond the crown's farthest point on either side, so no crown reaches a wall (calculated from the measured reach). The pavement under trees is therefore 6 m, as §28.1 item 4 expected.

Views: Blender's LOD row, and `Logs/SliceSurvey/green_*` of C3, rendered with the new tree before C3 is replaced.

## 28.4 Status

Being built, step by step (§28.1, item 20). Each step adds its section here.

## 28.5 The plan, 2026-11-10

Plan: [VEILMOUTH_CITY_PLAN_C4.svg](VEILMOUTH_CITY_PLAN_C4.svg).

**One source.** `tools/slice/veilmouth_c4_plan.py` holds the layout and writes three files from it:

- the layout the builder reads, `Assets/Data/Slice/Map/VeilmouthC4Layout.json`;
- this plan;
- the player's map without the planner's notes, `Assets/Data/Slice/Map/VeilmouthC4Plan.xml`, which replaces C3's (S1.6).

A change to the layout is made in the script, and the three files are written again.

**Size.**

- Inside its wall the city is about 260 × 290 m, px 420–680 and py 260–550, close to C3's 300 × 204 m.
- The works over the river add 260 × 116 m.
- The script estimates about 102 buildings:
  - 66 in rows and 6 on Squint Lane;
  - 10 on plots;
  - 14 single buildings;
  - 6 works blocks.
- The builder's count replaces this estimate at step 5.
- Eight cottages stand in the village, counted apart (§28.2).

**The land.**

- River Veil is 36 m wide. It comes down the west at px 380–416, turns east along the south at py 550–586, and runs into the sea.
- The city's quay wall stands at px 680, and the open coast at px 700.
- The town wall runs on the north at py 258 and on the river's west bank at px 418. It is 2 m thick and 6 m high, with five towers. Creaking Gate stands in it on the main street, between two of them.
- Last Farthing Bridge carries the West Road over the river to the gate.
- Grubby Bridge carries Smoked Street over the river to the works, which have a wall of 4 m on their land sides.

**The centre, the Penny.**

- It is a square of 80 m. Its facades stand 40 m from the statue, so a civic front of 30 m fits each half of a side between its streets.
- A ring of setts, 8 m wide at radius 26–34 m, runs round an island of 26 m. Stone Grandad, the patron saint, stands on his plinth in its middle.
- Between the ring and the facades there are 6 m at the middle of each side, like the main streets' pavements.
- The Town Hall stands on the north-west, with its clock tower at the corner; the Assize with the Wardens on the north-east; the Governor's House on the south-west.
- Rich shops line the west and east sides.

**The streets.**

| Street | Section | Runs |
|---|---|---|
| Long Tongue | main, 22 m | Creaking Gate → the Penny |
| Salt Gullet | main, 22 m | the Penny → Fishlip Quay |
| Lenten Street | ring, 20 m | the Penny → Drowsy Steps before the cathedral |
| Smoked Street | ring, 20 m | the Penny → Powder Embankment and Grubby Bridge |
| Widows' Row | side, 16.5 m | the wall → the quay, along the religious quarter's south edge |
| Powder Embankment | a 3 m pavement, 7 m of carriageway and the 6 m promenade, 16 m | the wall → the quay, along the river |
| Squint Lane | 8 m of setts at 45° | the Penny's south-east corner → the Black Puddle |
| Last Turning, Draught Lane | lanes, 5 m | by the cemetery; through the port's slum |
| Furnace Road, Clinker Road | works roads, 14 m | through the works |
| Rats' Choir | an alley, 6 m | through the workers' slum |

The embankment has a 3 m pavement before its houses, which the 13 m of §28.1 item 4 did not include (authoring default).

**The districts.**

- **Parsons' End**, the religious quarter, north of Widows' Row:
  - the old chapel before Last Turning Cemetery, and the slum of Parsons' End beside it on Last Turning;
  - Bishop's Garden;
  - the cathedral on Drowsy Steps;
  - Quiet Whistle Garden with the faction's house;
  - the Brothers' House round its garth by the sea.
- **Sleepy Quarter**, the west:
  - shops on Long Tongue;
  - Bawling Square by the gate, with shops round it;
  - villas and detached houses on Widows' Row and on Powder Embankment.
- **Salt End**, the north-east: shops, villas, and the Customs House on Fishlip Quay.
- **Lower Veil**, the south-east:
  - Squint Lane;
  - the Black Puddle, with the Soot & Bell and Harrow & Tincture on it;
  - the slum on Draught Lane;
  - the quay's warehouses;
  - Tipsy Pier, and the One-Eye on its own pier.
- **The Reek**, the works: six blocks, Deaf Boiler Yard and Rats' Choir.
- **Outside the walls**: Crooked Hamlet by the West Road, and then Gloomwood.

**The Ritual Site Candidates**:

1. the Black Puddle;
2. Deaf Boiler Yard, with the cellar's door in a works block;
3. the old chapel's forecourt.

The player starts on the embankment before the Black Puddle.

**Names.** `Resources/Localization/map.csv` gives the 49 names on the map in English and Russian, and C3's names leave it. They follow §28.1 item 16's five kinds:

- nicknames: Long Tongue, Salt Gullet, Squint Lane, Fishlip Quay, Stone Grandad, the One-Eye;
- ironic: Powder Embankment, Drowsy Steps, Tipsy Pier, Last Farthing Bridge, Rats' Choir;
- grim: Widows' Row, Last Turning, the Black Puddle;
- everyday: Lenten Street, Smoked Street, Draught Lane, Furnace Road, Clinker Road;
- mysterious: Quiet Whistle Garden, Deaf Boiler Yard.

The district names are Parsons' End, Sleepy Quarter, Salt End, Lower Veil and the Reek; the village is Crooked Hamlet, and the forest Gloomwood.
