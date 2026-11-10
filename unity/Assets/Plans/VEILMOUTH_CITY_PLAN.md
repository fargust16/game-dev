# Veilmouth City Plan (C1, C2, C3, C4, C5)

Version: 0.67

Status: Draft — C1 accepted by the owner, 2026-11-05; C2 (§21), which replaces C1's layout, accepted 2026-11-06; C3 (§24), which replaces C2's layout and houses, asked for by the owner 2026-11-08, built, and walked by the owner the same day, whose remarks go to [VEILMOUTH_REFERENCE_PLAN.md](VEILMOUTH_REFERENCE_PLAN.md); C4 (§28), the owner's map, which replaces C3's layout, ground and names, asked for by the owner 2026-11-10, built the same day and walked by the owner; C5 (§29), which cuts C4's map into blocks, asked for by the owner 2026-11-10 after that walk and being built step by step; the owner's walk of C5 and its fixes in §31 (2026-11-10); sizes remain authoring defaults

Last Updated: 2026-11-11 (§25 colour and the look's table: the city green's middle and light keys a little greyer, (0.31, 0.355, 0.24) and (0.56, 0.60, 0.40), so the palette sheet stays under 40 % under the fuller day grade, saturation −15 (reference plan R3 step 7). Earlier: 2026-11-10: §32: a row on one level: each run of houses wall to wall set on its highest floor, so no band breaks at a party wall; 0 of 58 pairs stepped, 30 before; §31.3: item 6 closed as not reproduced. Earlier: §31: the owner's walk of C5: lamps and road trees in one rhythm of 30 m bays on each kerb, a lamp's base clear of the kerb and its foot on the ground, wards as chalk on whole faces, railings drawn out to their walls and posts at every free end, stone under the works' bank, wing walls at the bridges, one parapet between bridges, every carriageway in setts; §31.4 as built; the hole of item 6 not found, its place asked of the owner. Earlier: §30.2: the lamps, their glass and the statue built; the gaslight made unlit, since its emission keyword was never kept; the frame with the Tripo lamps and people at most 11.95 ms in any view, so the lamp needs no far level. Earlier: §30: the owner's Tripo street lamp in the place of every lamp post and one bronze statue on Drowsy Steps, two more exceptions to §25.6. Earlier: §29.11: the far LOD built and measured: every view's median frame within the 16.7 ms budget, the swap unseen; the yard doors left out of it; its glass a variant of the kit glass that writes depth and reads the panes' rooms.)

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
| Grade | A post volume graded in HDR (Neutral tone mapping, saturation −15 and contrast 10 since 2026-11-11, −22 and 6 before, warm highlights and cool shadows, bloom over HDR highlights) and a twilight volume (violet shadows, amber highlights), weighted by dusk. URP's screen-space ambient occlusion runs on the renderer. |
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
- **Colour** (owner: our range, some contrast, a feeling of green): the city's trees and bushes wear project copies of the pack's foliage (`Foliage 001`, `002`, `004`) whose gradient runs from a dark green (0.12, 0.16, 0.10) through (0.31, 0.355, 0.24) at 0.78 to a light (0.56, 0.60, 0.40), in sRGB (since 2026-11-11; before, (0.30, 0.37, 0.20) and (0.56, 0.60, 0.36), which the fuller day grade lifted over 40 % on the palette sheet). That is darker and greyer than the pack's teal and lime, and a little greener than the forest's olive. Only the `City*` wrappers wear the copies; the forest's fellable trees keep the pack's colours as resource markers (§6.2).
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

**Spacing superseded** by §31.1 (owner's walk, 2026-11-10): a street's lamps and trees keep one rhythm of bays on each kerb, a lamp in each bay's middle and two trees between lamps.

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
- **The owner's Tripo models furnish interiors only.** Each holds up to about 10 000 polygons (the owner; the table lamp 14 410, measured), too many for the city's open plan. Their places are in the furnishing rules §10. The wooden hanging sign among them is the one exception: by the owner's word one hangs beside the street door of Harrow & Tincture and of the Soot & Bell, its bracket on the facade and its board's foot 2.6 m above the sill (`VeilmouthSigns`), blank until the trades' emblems come ([99 Open Questions](../../../99%20Open%20Questions.md) VS-SIGN (b)). By the owner's word of 2026-11-10 two more stand outside: the Tripo street lamp in the place of every lamp post, and one bronze statue (§30).

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
| 6 | (owner) "A tree needs a special bed, as if it stood in the earth, fenced by a square bed; lamps and trees stand on the road, flush on the pavement, not on it" | The pavements of §25.2 go back: the High Street's are 4 m, the Parade's and the byelaw streets' 3 m. A street tree stands in a tree pit on the carriageway, flush on the kerb: a 1.6 m square (1.2 m since §27) of stone kerb 0.1 m wide at the kerb's height, round soil 0.08 m high in the soil layer's colour, so a ring of earth shows round the trunk, 1.1 m across at its foot. A lamp stands on the carriageway 0.3 m from the kerb (0.45 m since §31.1, so its base clears the kerb). Each keeps its block's grid cell: the post or the trunk moves inside the cell with its body, so the Combat Grid reads it where it stands. Trees and lamps keep §25.3's spacing, and stay clear of a door's reach and a lane's mouth |
| 7 | The lines along the kerbs come and go with the camera | The wet band of R4 lay 3 cm over the terrain, which the terrain's own level of detail hid and showed. It is gone |
| 8 | (owner) "The posts at a fence's ends must always form a passage, and an end without a post must meet the wall" | The railing module has one post, at one end. Each run of railing now gets a post at an end that stands free, at a gate or a gap, and loses its post at an end that meets a house, so the railing runs into the wall. Since §31.2 an end is read against the walls' colliders, and one that stops short is drawn out to meet its wall |
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

Built, 2026-11-10, step by step without stops (§28.1, item 20): the plan (§28.5), the ground (§28.6), the streets (§28.7), the houses (§28.8), the districts and the outskirts (§28.9), and the slice's places, tests and cost (§28.10). It waits for the owner's walk (99 VS-C4); its frame budget is open (99 VS-FRAME).

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
| Widows' Row | side, 17 m (§28.6) | the wall → the quay, along the religious quarter's south edge |
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


## 28.6 The ground, 2026-11-10

`VeilmouthGround` shapes the ground once from the layout, as it did from C3. Its old hills, the Ridge and the ore hill, are gone, and so are the owner's hand changes to C3's ground (§28.1 item 19). `VeilmouthLayout` reads the layout for the ground, the city and the map's zones. All heights are authoring defaults.

- **The city and the works** stand level at 25.9 m. A rise of 1.5 m lies under the Penny and runs on to the cathedral's west front. It is full within 30 m of that line and gone at 120 m.
- **The river** is dug to a bed at 21 m under the one water plane at 24 m. Its bank is natural outside the walls and stone along the city and the works; the stone banks are built with the city (step 5).
- **The sea** has a bed at 19.5 m by the quay, deepening 2.5 m over 200 m. The open coast falls over rocks within 12 m of the water.
- **The land outside** rolls by up to 5 m and rises 6 m inland. It never sinks below 26.2 m, so no pond forms: water is only the river and the sea (§28.1 item 13). The north, west and south edges rise 45 m, steeper than a walker can climb, and the West Road leaves through a pass.
- **Paint**: built ground is soil until the city paints its streets; then rock on cliffs and on the open coast, sand below 25.2 m, soil on the West Road, and grass with forest floor in Gloomwood.

Widows' Row's south pavement is 4 m, not 3.5 m, so every front behind it lies on a whole metre: the street is 17 m, py 352–369.

Views: `Logs/SliceSurvey/c4_ground_*`.

## 28.7 The streets, 2026-11-10

`VeilmouthCity` now builds C4 from the layout file. C3's tables, its streets, its squares and its plots are gone from the builder, and the root is `Veilmouth C4`. Its new part, `VeilmouthCity.Layout.cs`, places what the layout names. All sizes are authoring defaults.

- **Paint.** The order is:
  1. cobbles over the city and the works;
  2. grass on the plots' gardens;
  3. the squares, as walking ground;
  4. the Penny's setts ring (r 34 m) round its grass island (r 26 m);
  5. each street's two pavements and its carriageway;
  6. Squint Lane at 45°;
  7. the bridges' city ends.

  The canonical five layers keep their order.
- **Pavements.** Pavements are raised 0.15 m (R4): 15 outlines, 9140 m², 4366 m of kerb, 3 carriageway ends carried on and 8 rounded corners. The Penny is flush paving with no kerb.
- **The wall.**
  - The town wall stands on the layout's lines, with the gate between two towers and a lintel over it.
  - The north wall has four towers.
  - The works keep their own west and south walls.
- **The water's edge.**
  - The river has a stone bank along the city and the works.
  - Along the south leg, the city side has a parapet 1.1 m high and the works side a low kerb.
  - The quay wall runs from the seabed to 0.3 m over the city.
  - Every run is in 4 m stones and opens where a bridge or a pier meets it.
- **Bridges and piers.**
  - Last Farthing Bridge and Grubby Bridge have a deck, parapets, abutments and piers every 12 m or so.
  - A bridge end on the natural bank runs 4 m onto the land.
  - Tipsy Pier and the One-Eye's pier stand on piles every 6 m.
  - A deck is what stands on it stands on.
- **Lamps.**
  - Along each street's kerbs every 27 m, the far side staggered where the street is wider than 12 m.
  - Along the quay's edge.
  - Eight round the Penny.
  - One on a corner of each of the four small squares.
  - Two at each bridge end and at each pier's end.
- **Trees.**
  - 78 of the owner's mature tree stand in kerb pits.
  - A street marked for both sides gets both kerbs when its carriageway is 8 m or more, else one row on alternate sides; two streets are planted on one side.
  - None stand in the slums or the works (§28.1).

Houses come next (step 6), so the passage and back-door counts are 0 for now. The people, the dogs, the mystic layer and the POI stages that C3 placed stay on its coordinates until step 8.

Views: `Logs/SliceSurvey/c4_*` (top: `c4_city`, `c4_penny`, the districts; at eye height: `c4_eye_*`).

## 28.8 The houses, 2026-11-10

The builder puts up the layout's houses from the owner's Victorian Kit at ×1.25. It places:

- the single buildings: the Penny's civic fronts, the chapel, the Brothers' House and its bell tower, the Customs House, the One-Eye, and Lower Veil's openable houses with their portals;
- the rows, each of one family, wall to wall with passages (R5);
- the plots, with a detached house or a villa in a railed garden;
- the low tenements on Squint Lane at 45°.

**Counts.** There are 105 footprints, the wall's six towers among them, with 137 doors. 0 kit faults remain, 21 passages run between houses, and no gap under 4 m is left in a row. Of the back doors, 39 open and 53 are walled up. The least room between a plot's steps and its railings is 1.57 m.

**Stoops (§28.1 item 5, §28.2).**

- A house on a plain stoop is set 0.6 m down through its plinth line. Only its top step, 0.15 m, shows over the pavement, and its stoop rails by the door are hidden. 75 houses stand so.
- A rich row and every plot keep the kit's full stoop: four steps of 0.19 m reaching 1.4 m. That is under a quarter of a 6 m pavement. A plot's steps stay inside its railings.
- The civic fronts, the towers, the chapel and the villas keep their own stairs and bases.
- A buried step is not counted as a stoop, so grass and trees may stand over it.

**Planner fixes.**

- Bawling Square's west row now ends at the square's corner (py 500). It used to run 10 m past it into the south row.
- The short row by the Penny on Long Tongue's south side is gone, since no house fitted there.
- Harrow & Tincture stands 2 m further south, so a passage of 4 m lies between it and the Soot & Bell.
- The layout now names about 102 buildings inside the walls and the works.

Views: `Logs/SliceSurvey/c4_*`.

## 28.9 The districts and the outskirts, 2026-11-10

`VeilmouthCity.Districts` places what the layout names besides the streets and the houses. All sizes and counts are authoring defaults. The randomness is a fixed System.Random, never the world seed.

- **Stone masses.** These are placeholders until their models come (SLICE_MODELS_WANTED.md § C4), each group its own merged mesh with box colliders.
  - The cathedral is a cross of light stone: nave 22 m, transept 20 m, apse 16 m and a tower of 44 m over its west front.
  - The six works blocks are sooty brick, 10–16 m high.
  - Stone Grandad on the Penny's island is 12 m in all: a stepped plinth of 8 × 8 m, a pedestal and the figure. Four benches face him across the grass.
- **The cellar door** is in works block 6's west wall, opening on Deaf Boiler Yard, with its portal into the Soot Stair cellar.
- **The ritual circles** wait hidden at the three candidates: the Black Puddle, Deaf Boiler Yard and the old chapel's court.
- **Last Turning Cemetery** has 98 graves in rows. Its walls stand on the lane's side and the garden's side, with a gate from Last Turning.
- **The parks** hold 31 garden trees (MTree):
  - Bishop's Garden has two lines along its middle walk, with benches on the walk.
  - Quiet Whistle Garden has a grove north of the faction's plot.
  - The Brothers' garth has a lawn, one tree and a bench on each side.
- **The market on Bawling Square.** A row of stalls holds the trader, the Masters of Enchanting and First Aid, and the seer's counter. A row of food stalls faces it, with benches in the aisle, and the Item Creation corner has its anvil, leather frame and armour stand. 6 m stays clear before every front.
- **Cargo** stands only on Fishlip Quay before the warehouses and on Tipsy Pier.
- **Crooked Hamlet.**
  - Eight cottages stand by the West Road.
  - Each farm has a plank yard of 5 m before its front, since the road runs 6 m from the fronts, and a fenced bed beside it.
  - Fruit trees grow behind the farms, with posts along the road and grass and shrubs in the open land.
- **Gloomwood.**
  - Each of the eight glades has five fellable trees and up to four herbs, 40 fellable trees and 30 herbs in all.
  - 4013 cover trees stand, three in four of them pines. Each 6 m cell gets one chance of a tree:
    - 85 % in a 25 m belt along the edges that face open land, so the wood reads as a wall from the hamlet, the river and the walls;
    - 45 % in the stands that a value noise on a 45 m lattice marks;
    - 6 % between the stands, which leaves clearings.
  - Six wolf and four boar territories hold their stand-ins.
  - Twelve iron veins lie at the foot of the area's rising edges, out of the West Road's pass.

Kit check: 7043 blocks, 140 doors, 0 kit faults. The 4013 forest trees are the larger share of the blocks, and step 8 measures their cost. Unity's terrain trees would cost less at this count, but they need the vendor's prefab on the terrain, not a kit wrapper, so they wait for the owner.

Views: `Logs/SliceSurvey/c4_world`, `c4_city`, `c4_hamlet`, `c4_eye_penny`, `c4_eye_cathedral`, `c4_eye_market`, `c4_eye_cemetery`, `c4_eye_works`, `c4_eye_hamlet`, `c4_eye_gloomwood`.

## 28.10 The slice's places, tests and cost, 2026-11-10

Step 8 moves what C3 placed on its own coordinates onto C4, checks the city with the slice's tests, and measures what it costs. All places and counts are authoring defaults; the POI places are working readings, not owner decisions.

**One rebuild.** `VeilmouthCity.RebuildStreets` now runs the whole chain once, and `RebuildGreenery` is the same chain:

1. the city (`Run`);
2. the named people (`SlicePeople.PlaceAll`);
3. the slice's world and the Notice Board's tips (`SliceWorldSetup.AddToVeilmouth`);
4. the dogs (`SliceDogs.PlaceReplacing`);
5. the ornaments (`VeilmouthOrnaments.Place`);
6. the mystic layer (`VeilmouthMystic.Place`);
7. the reflections (`VeilmouthReflections.Bake`);
8. the POI stages and their residue (`Restage`);
9. the C4 views (`VeilmouthRenders.RenderC4`).

**People** (`PeopleC4`, before `SlicePeople` puts each named person in place of the Dummy of that name):

- **Wardens**, four:
  - two at the Assize's door;
  - one on Salt Gullet's north pavement;
  - one at the Black Puddle.
- **The faction**: two members in the Brothers' front garden.
- **Veinmarket**: two by the east end of Draught Lane.
- **The market.** On Bawling Square, before their stalls as C3 stood them:
  - the trader;
  - the Masters of Enchanting, First Aid and Item Creation;
  - the seer.
  The pawnbroker stands at her counter and chest in the Black Puddle's north-east corner, clear of the hidden ritual circle.
- **Townsfolk**: twenty walkers, ten women and ten men. They walk:
  - on the main streets' pavements;
  - at the Penny's corners;
  - on the embankment's river walk;
  - on the quay, in the squares, and in the works;
  - one, the mill woman, on Draught Lane, whom POI B's fear keeps at home;
  - one farmer on the West Road.
  A pavement point is taken from its street (`Pave`), inside the street's c0..c1 along its edge. The first placing had read the pavements as lying outside c0..c1, which put twelve walkers and a Warden in houses or the river.
- **Named people.** `SlicePeople` placed Edda, Silas, Ada, Tobias, Ruth, Ilse and Kit where their Dummies stand; the kit's new stand-ins are on Bawling Square beside Tobias.

**Dogs**: five:

- by the player's start on Powder Embankment;
- on Bawling Square;
- on Salt Gullet's carriageway;
- on Drowsy Steps by Widows' Row;
- on Fishlip Quay.

The first placing put one dog in the river and one on a roof, so both moved onto the carriageway.

**Ornaments**: thirteen.

- Three angels: Bishop's Garden, the cemetery, and the west villa's garden.
- Eight birdbaths: Bishop's Garden, the faction's garden, two in Widows' Row's detached gardens, and four in the embankment houses' gardens.
- Two lions before the Governor's House, measured from its own bounds.

**The mystic layer** (WORLD_STYLE_GUIDE §6, with C4's names):

- **112 gas lamps.**
- **31 fog banks**: over the river's two legs from the north wall to the quay, the sea by Fishlip Quay, Draught Lane, Rats' Choir, the Black Puddle, Deaf Boiler Yard and the cemetery.
- **Wards over 4 of the 44 doors** in Lower Veil and the Reek, about one in eight by a hash of the door's place.
- **Runes, 14 sets**:
  - either side of Creaking Gate;
  - three along the north wall;
  - on the works' west and south walls;
  - on the brick blocks either side of Furnace Road;
  - on the two nearest walls round each Ritual Site Candidate.
- **Strange windows**: Harrow & Tincture and three tenements of Lower Veil.

**Reflections**: the sky and 90 street probes in 30 m cells over world X −80…180 and Z −56…240, all baked.

**A dwelling's wealth** (`VeilmouthDwellings.WealthAt`), which furnishes the generated Dwelling scene:

| Where | Wealth |
| --- | --- |
| tenements and warehouses, anywhere | poor |
| villas, anywhere | rich |
| Lower Veil, the Reek and the quay | poor |
| round the Penny | rich |
| Parsons' End west of Last Turning | poor |
| Parsons' End east of Last Turning | rich |
| everywhere else | middling |

**The POIs** (11.1 § *Places in C4*):

- **POI A** stands before Harrow & Tincture's street door on the Black Puddle.
- **POI B** stands in the middle of Draught Lane, C3's Loom Lane.
- **POI C** begins at the cellar door on Deaf Boiler Yard.
- **The residue.**
  - The seal goes on Harrow & Tincture's door.
  - The Warden pairs stand on the Black Puddle (`Residue - Warden pairs on the Black Puddle`).
  - The fear on Draught Lane keeps at home the one townswoman whose walk comes within 30 m of the lane (`Residue - Fear on Draught Lane`; C3 had two).
  - The C3 residue roots are removed by their old names.
- **The Notice Board** stands on the Black Puddle, 3 m out from the Soot & Bell's front. At 1 m out, one stoop refused it and the tavern's stoop touched it by 0.14 m: the kit check caught the touch, which the free-place test does not see.

**C3's names in the story follow the map:**

| C3 | C4 | Russian |
| --- | --- | --- |
| Loom Lane | Draught Lane | переулок Сквозняк |
| Bell Street | the Black Puddle | Чёрная Лужа |
| the Market square | Bawling Square | Горластая площадь |

They changed in the dialogue and POI string tables, in the POI cards (`PoiANode`, `PoiBNode`, the option *Leave Draught Lane*), and in the tests' messages. Asset ids and string keys keep C3's names, for example `aftermath.loom_lane.*`.

**Fixed on the way:**

- A lamp round the Penny stood 6.5 m before a corner house's door. Each of the eight is now turned along the ring, by up to 12°, until it stands in no door's way.
- `TownsfolkTests`, in EditMode and in PlayMode, count C4's twenty walkers.
- `VeilmouthCityTests` ask for at least 3 warded doors of C4's 44, where C3's larger poor districts gave more than 5.

**Tests**:

- The EditMode set (`GameDev.Slice.Tests.EditMode`) passed 303 of 303 after the last rebuild.
- In the PlayMode set (slice-play, without the Diagnostic category), 52 of 53 passed, and the five render tests, which run only on request, were skipped.
- The one failure was the PlayMode `TownsfolkTests`, which still counted C3's fifteen walkers. Counting twenty, it passed on its own run, with every walker walking on the ground and keeping to its route.

**Cost**, measured 2026-11-10 (`VeilmouthFrameProbe`, the editor's batch render, Direct3D 12, an RTX 5070 Ti laptop GPU):

- **The scene.** 115 236 game objects and 90 476 mesh renderers, against C3's 99 898 and 82 345 (scene-load diagnostic, 2026-11-10 00:21). Without the forest's 23 299 objects the scene holds 91 937, fewer than C3.
- **Gloomwood's cover.** 3 981 trees by the probe's test: a pivot inside the forest's rectangles, which leaves out a few at their edges of the 4 013 built. They hold 23 299 game objects (20 % of the scene) and 11 943 mesh renderers (13 %). Each tree has an LOD group, and all their meshes' levels sum to 8.7 million triangles.
- **Frames.** Five views at 1920 × 1080 at a walker's height. For each view, four rounds with the trees shown and four with them hidden, taken in turn, of 9 frames after 3 warm-ups each. Each frame is waited for by reading one pixel back. Median ms, with the trees / without them:

| View | With | Without | Difference |
| --- | --- | --- | --- |
| in Gloomwood | 9.70 | 6.58 | +3.13 |
| Crooked Hamlet toward the wood | 7.65 | 5.99 | +1.66 |
| the West Road toward the gate | 50.79 | 49.92 | +0.87 |
| down Salt Gullet | 53.23 | 48.00 | +5.22 |
| across the Penny | 31.25 | 31.22 | +0.03 |

What the figures say:

- The forest's kit trees add 2–3 ms to views of the wood and 0–5 ms to the city's long views. So §28.9's question about Unity's terrain trees is answered by measurement: they are not needed for cost.
- The city itself costs 31–53 ms an editor frame in its long views. An editor frame carries the editor's own overhead, and no frame has been measured in a player build. The frame budget and that measure are open for the owner (99 VS-FRAME). The owner already allows the GPU Resident Drawer and GPU occlusion culling (§28.1 item 17), but they need DOTS instancing in the facade and window shaders.
- A first run that measured the trees shown before hidden gave the wrong sign in the wood (−5 ms). Taking the rounds in turn removed it.

**Load** (`SceneLoadTimingTests`, 2026-11-10 03:46, in the editor with graphics):

- Veilmouth's cold load reads in 3.38 s, wakes in 7.89 s and draws its first frame at 8.32 s. C3 took 3.62, 8.47 and 8.93 s (00:21), though C4 holds 115 237 game objects against C3's 99 898.
- A door's journey takes 0.44–0.80 s, and the first entry into the Dwelling is the longest.

**Not redone.** The older survey renders (`RenderLook`, `RenderPavements`, `RenderGreenery`, `RenderPorches`, `RenderTownsfolk`, `RenderOrnaments`, `RenderMystic`, `RenderDressing`) still hold C3's camera places. They are left until a pass needs them; `RenderC4` covers C4.

**Counts since earlier steps.** There are 77 street trees, not §28.7's 78: one pit went to a house in step 6. The kit has 7 045 blocks, two more than §28.9's, for the pawnbroker's counter and chest.

Views: `Logs/SliceSurvey/c4_*`, with five new views:

- `c4_eye_masters`: the Masters before their stalls;
- `c4_eye_assize`: the Wardens at the Assize;
- `c4_eye_puddle`: the Black Puddle with the pawnbroker and a Warden;
- `c4_eye_draught`: Draught Lane, where POI B happens;
- `c4_eye_lions`: the lions before the Governor's House.

# 29. C5, the owner's map cut into blocks, 2026-11-10

The owner walked C4 the day it was built and asked for its streets to be laid again. C5 keeps C4's map: the river, the wall and its gate, the port, the works over the river, the cathedral, the village and the forest, the ground's shape and the folk names. It replaces C4's streets, houses and squares inside the walls and in the works. Sections §28.5–§28.10 stay as the record of C4.

## 29.1 The owner's decisions

The owner gave five screenshots of C4 and one of a reference. The reference shows the principle: streets frame blocks, fronts stand on the pavement, and yards lie behind. It is not a map to copy.

1. **A pavement along every road.** Lanes and alleys have pavements too, which replaces §28.1 item 4's lane and alley rows. Every way into a building is from a pavement. In C4, a lane's houses stood straight on its carriageway, and a carriageway ran into a house.
2. **The carriageway runs unbroken** along its whole length. No pavement, band of setts, square or building crosses it or ends it. In C4:
   - a pavement ran across a lane's mouth;
   - a street painted later laid its pavement over an earlier one's carriageway.
3. **Blocks.** The streets cut the city into blocks, rectangular or square for now. Some streets are wide and some are narrow lanes.
   - A pavement rings every block.
   - The houses stand flush on the pavement, back to back round a small inner yard.
   - A yard has one way in, which is also its way out, or a way through the block.
4. **Yards and open spaces.**
   - A rich district's yard is a small park.
   - A poor district's yard holds mud, rubbish, drunkards and the other marks of poverty.
   - Where the houses cannot fill a block and an open space is left, a stall, a notice board or other props that fit the block stand there.
5. **The market moves to the Penny.** The stalls stand round the statue facing the road, because most people are there. The island with the statue shrinks; C4's was so large that it left much empty space.
6. **The port's joint.** Where the quay meets the shore, a gap opens that a walker falls into. It is a defect to fix.
7. **Size.** The walls stay where they are, with about 155 buildings inside them and in the works. This replaces §28.1 item 3's 100–120 (the owner's answer, 2026-11-10). GPU occlusion culling, which §28.1 item 17 allowed, becomes part of the work, since dense blocks hide the most from it.

## 29.2 The agent's readings (authoring defaults)

- **Squint Lane at 45° is dropped** while blocks are rectangular (item 3, "for now"). It can come back when blocks may take other shapes.
- **Street sections.** Pavement, carriageway and pavement, in metres. Trees stand only on pavements of about 6 m (§28.1 item 4), never in the slums or the works.

  | Class | Section | Trees | Where |
  |---|---|---|---|
  | main | 6 + 10 + 6 | both sides | Long Tongue and Salt Gullet; Lenten Street and Smoked Street, to the Penny |
  | side | 6 + 7 + 4 | the 6 m side | Widows' Row |
  | street | 3 + 6 + 3 | none | the streets between blocks |
  | lane | 2.5 + 5 + 2.5 | none | lanes, the lanes inside the walls, the slums |
  | quay | 3 + 9 + 3 | none | the road along Fishlip Quay; the quay's apron lies east of it |
  | embankment | 3 + 7 + 6 | on the promenade | Powder Embankment |
  | works | 2.5 + 9 + 2.5 | none | the works' roads |

- **Lanes inside the walls.** A lane runs inside the west and north walls, so no block stands against a wall and every block has a pavement on all sides.
- **How a block is built.** Each side of a block holds one row of houses, its fronts on the pavement's inner edge.
  - **Corners.** At an outer corner one row owns the corner, chosen by its street's rank (main, then side, street and lane) and then by its length. The owning row's end house is a corner house with its second facade on the other street. The other row starts behind it, its end house open and flush against the corner house's back wall.
  - **Flush joins.** The kit's terrace houses are open at their party walls. A flush join is allowed only where the wall behind is at least as high and as wide as the open end (heights in the kit: shops 19 m, tenements and warehouses 16 m, terraces and civic fronts 15 m, low tenements 12 m). So one family, or a lower one against a higher one, meets at a corner.
  - **Inner corners.** At an inner corner (only round the Penny) both rows run to the corner, each ending in a faced end.
  - **Passages.** A gap between houses is a passage of at least 4 m (R5). Its houses are faced on it, and it is the yard's way in. A yard has one passage (a dead end) or two on opposite sides (a way through). A side whose length the houses' 10 m and 15 m fronts cannot fill takes its yard's passage there.
- **Families by wealth.**
  - Rich shops round the Penny.
  - Shops on the main streets.
  - Terraces in the middling blocks.
  - Tenements toward Salt End and Lower Veil.
  - Low tenements in the slums: Lower Veil, the cemetery's Parsons' End, the works.
  - Warehouses on the quay.
  - The civic buildings (the Town Hall with its clock tower, the Assize, the Governor's House and a guild hall) face the main streets at the Penny's four corners.
- **The Penny.**
  - A square frame of houses, 52 m across, round:
    - a ring carriageway of 8 m;
    - a raised island of 13 m radius, with the statue on its plinth and a ring of stalls facing the road.
  - Customers stand on the island's outer walk.
  - The frame's corners belong to four L-shaped blocks, the only blocks with an inner corner. They are paved to the facades, with lamps, benches and the Notice Board where open space is left.
- **Squares and parks are blocks too**, ringed by pavement:
  - the Black Puddle;
  - the cathedral's close with Bishop's Garden and Quiet Whistle Garden, and the faction's house in it;
  - Last Turning Cemetery with the old chapel and its court;
  - the Brothers' House round its garth.
- **The works** are blocks of plain boxes on the works' roads. Deaf Boiler Yard, with POI C's cellar door and Ritual Site Candidate 2, is the inner yard of a works block with one way in.
- **Yards' dressing.**
  - Rich: grass, Veilmouth's garden trees, benches and birdbaths.
  - Middling: cobbles, with a bench or a tree.
  - Poor: bare earth with rubbish and drunkards. Only what the asset map holds is used; what it lacks, rubbish and the drunkards' poses, goes on the wanted list.
  - Barrels, crates and boxes stay as §25.6 and the furnishing rules say: never as filler.
- **Names.** C4's names stay on the streets that keep their place. A new street takes a new folk name in Russian and English (§28.1 item 16). Bawling Square is no longer a square; its name goes with the market to the Penny's stalls, *Bawling Row*.

## 29.3 Steps

As C4 was (§28.1 item 20), C5 is built step by step without stops, each step ending with a commit, a push and views:

1. this section, with the Glossary, the open questions and the agents' rules;
2. the plan from one source: blocks, streets, squares and the Penny, with the builder's layout, the plan and the player's map;
3. the ground's surfaces with continuous carriageways, the pavements round every block, and the port's joint;
4. the houses round the blocks, with their corners, passages and yards;
5. the yards' dressing, the Penny's market, trees, lamps and the props in open spaces;
6. the slice's places on C5: people, dogs, POIs, the mystic layer, reflections, and the tests;
7. the GPU Resident Drawer and GPU occlusion culling, measured before and after;
8. the cold load, the views and the documents.

## 29.4 The plan, 2026-11-10

C5 has one source, `tools/slice/veilmouth_c5_plan.py`. It writes three files from one table: the builder's layout `Data/Slice/Map/VeilmouthC5Layout.json`, the plan `VEILMOUTH_CITY_PLAN_C5.svg`, and the player's map `Data/Slice/Map/VeilmouthC5Plan.xml`. With `--png` it also draws a preview. It stops with an error when:

- two buildings overlap;
- a gap between two buildings on one line is between 0 and 4 m (R5);
- a building stands outside its block or in its notch;
- a side cannot be filled.

The builder still reads C4 until step 3 switches it (done, §29.5).

**As planned.** There are 154 buildings inside the walls and in the works, against the owner's about 155:

- 124 row houses, 73 of them with a faced end;
- 15 single buildings;
- 15 works sheds.

They stand in 23 blocks, with 26 passages into the yards, 4–13 m wide. The village keeps its 8 cottages outside the walls.

| Kind of block | Blocks |
|---|---|
| rich | 5 |
| middling | 5 |
| poor | 5 |
| works | 6 |
| cemetery | 1 |
| close | 1 |

Readings that changed from §29.2 while the plan was solved (authoring defaults):

- **Corners are searched, not ranked.** Every combination of a block's corner owners is tried, and the best is kept. The order of preference:
  1. no broken rule;
  2. one or two ways into the yard;
  3. a yard with a way in;
  4. the fewest corners given to the lower street;
  5. the least ground left as passages.

  A single building never takes a flush join. An end that no house can reach becomes a passage beside the corner house's back wall.
- **Two 15 m row houses join the kit**, because 10 m fronts alone left runs of 21, 25 and 42 m that nothing could fill:
  - *Terrace12*, brick or stock;
  - *TenementLow12*, poor.

  Both are baked in step 4.
- **Cold Shoulder, inside the west wall, is a 12 m street (3 + 6 + 3)**, not a lane, so the west blocks are 40 m deep. Crows' Walk is the lane inside the north wall.
- **The cathedral moves 8 m south** to clear Crows' Walk. Its close stays one block with Bishop's Garden, Quiet Whistle Garden, Drowsy Steps and the faction's house, a stucco villa facing south.
- **The Penny** is 52 m square (514–566 × 414–466).
  - Its ring carriageway runs from 13 m to 21 m from the centre.
  - The island has a 13 m radius. Stone Grandad, 12 m high, stands on a plinth of 4 m radius.
  - *Bawling Row* is 12 stalls, 3 × 2 m, on a 9 m radius, facing the road.
  - The four civic buildings fill the L-blocks' main-street sides up to the notch: the Town Hall and the Governor's House face each other across Long Tongue, and the Assize and the guild hall across Salt Gullet.
  - The clock tower stands free behind the Town Hall, in its block's yard.
- **The Customs House** stands at E2A's north-east corner, facing the quay.
- **The Brothers' House** is two ranges, north and west, round the garth, with a free bell tower. Town houses close the block's east side.
- **E1C has three ways into its yard.** The Soot & Bell and Harrow are single buildings, so passages open beside them. The rule asks for one or two; this exception is accepted.
- **The works.** Each works block holds two sheds 14 m deep. ES2 is a ring of five boxes round Deaf Boiler Yard, whose one way in is 6 m wide off Clinker Road.

**New folk names** (`map.csv`), in Russian and English:

| English | Russian |
|---|---|
| Crows' Walk | Воронья Тропа |
| Cold Shoulder | Холодное Плечо |
| Spade Lane | Лопатный переулок |
| Thimble Street | Напёрсточная улица |
| Nettle Street | Крапивная улица |
| Button Lane | Пуговичный переулок |
| Tar Row | Смоляной Ряд |
| Ash Bank | Пепельный Берег |
| Cinder Lane | Переулок Окалины |
| Coughing Lane | Кашляющий переулок |
| Brine Lane | Рассольный переулок |
| Bellows Lane | Переулок Мехов |
| Kettle Lane | Котелковый переулок |
| Bawling Row | Горластый Ряд |
| Guild Hall | Гильдейский дом |

C4's Bawling Square and Squint Lane stay in the table until step 6 moves their references. Step 6 moved them (§29.6); their two keys stay in `map.csv` only while C4's record map, `VeilmouthC4Plan.xml`, uses them.

## 29.5 The ground and the houses, 2026-11-10

Steps 3 and 4. The builder reads C5's layout.

**The ground.**

- **Carriageways.** Every carriageway is a road, painted after every pavement, so no pavement, band of setts or square crosses it. Where a carriageway ended at another street's pavement, it was carried on to the next carriageway; there were 5 such ends.
- **Pavements.** Every street's pavements are raised, with kerbs and rounded corners, and ring every block:
  - 50 outlines, 26,177 m², 10,538 m of kerb;
  - 102 rounded corners;
  - 82 tree pits, one for each street tree.

  The mesh is cut into one piece for each 64 m chunk, 40 in all. As one piece it was 101 MB, over GitHub's limit of 100 MB for one file.
- **The Penny.** Its frame and island are raised. Its ring carriageway is 48 road segments.
- **Squares.** The Black Puddle and Drowsy Steps are raised squares.
- **Yards** are painted by wealth: grass in a rich yard, cobbles in a middling one, mud in a poor one. The passages are cobbled.
- **The port's joint.** A coping 1.5 m wide runs inland of the quay wall and closes the gap. The decks are laid one for each unbroken run, and the piers start on the coping.

**The houses.** The rows stand round 23 blocks, their fronts flush on the pavement, with 27 passages into the yards.

- R5 holds: no span is left without a house, and no gap in a row is under 4 m.
- The 15 m terrace and the low tenement of §29.4 are baked into the kit.
- Back doors: 54 open, and 77 are walled up where nothing lies behind them.
- The clock tower's door and stoop open north onto its yard. Facing south, its stoop ran 0.4 m into the Town Hall.
- **Counted:** 5,793 kit blocks with no kit fault, 194 doors and 186 lanes.

## 29.6 The yards, the market and the slice's places, 2026-11-10

Steps 5 and 6.

**The yards' dressing** (§29.1 item 4). Each yard is dressed round its middle, so the 15 m houses that reach past the plan's yard edge never take a piece's place.

| Yard | Dressing |
|---|---|
| rich | grass, a garden tree in each corner, two benches facing each other |
| middling | cobbles, one garden tree in a corner, one bench |
| poor | mud, dustbins in three corners and firewood in the fourth, a drunkard, and a second one in a yard over 500 m² |

The Penny's four L-shaped corners each have a bench against the side wall.

- **Counted:** 19 garden trees, 14 yard benches and 4 corner benches, 18 dustbins and firewood, and 10 drunkards.
- **Wanted list** (§29.2): loose rubbish, and poses for the drunkards (sitting, lying, holding a bottle). Until they come, a drunkard is a standing purple Dummy.

**Bawling Row.** The kit's stalls are 6–9 m long, not the plan's 3 × 2 m, so the Row is six stalls and five pieces round Stone Grandad. There are two to a side, facing the road. All 11 are placed.

- The six stalls: the trader's, Enchanting, food, First Aid, fish, and Item Creation.
- The five pieces: the seer's counter with its candles, the armour stand, the anvil and the leather frame.
- The Masters and the sellers stand on the island's outer walk: Edda Brann, Silas Quill, Ada Merrow, Tobias Penn and Madame Ilse. Kit Mallow, the newsboy, stands between the trader and the Master of Enchanting.

**The slice's places on C5** (working readings, not owner decisions):

- **POI A** stands before Harrow & Tincture's street door on the Black Puddle.
- **POI B** stands in Draught Lane's middle, between the Lower Veil blocks E1B and E1C, with house walls on both sides. Its earlier spot lay beside the open Black Puddle.
- **POI C** begins at the cellar door on Deaf Boiler Yard.
- **The residue** of POI A puts the seal and two Warden pairs on the Black Puddle. POI B's fear keeps a mill woman at home: she walks Draught Lane's north pavement from 5 m east of its stage, within the residue's 30 m. Her walk moved 38 m west along the lane, since at its old spot no townswoman was left within 30 m of the new stage.
- **Dogs:** five dogs. The one that walked Bawling Square now walks Thimble Street.
- **Townsfolk:** the same 20 walkers, on routes taken from their streets' pavements.
- **Dwellings** take the wealth of the block they stand on, found within 2 m of the door:
  - tenements and warehouses are poor anywhere, and villas rich anywhere;
  - the works are poor;
  - outside every block, a door on the quay or in the Reek is poor, and any other is middling.
- **Ornaments:**
  - the three angels, in Bishop's Garden, the cemetery and the Sleepy Quarter's yard;
  - six birdbaths, in Bishop's Garden, Quiet Whistle Garden, the embankment yard, and the yards of the Governor's House, the Assize and the Town Hall;
  - the great lions lie unturned along the Governor's House's front, either side of its door, both faces on Long Tongue. Turned to face the street, a lion 4 m deep left no way past on the 6 m pavement with its stoop and tree pits.
- **The mystic layer:**
  - 7 wards over 45 doors in Lower Veil and the Reek;
  - 251 gas lamps;
  - 35 fog banks.
- **Reflections:** 90 street probes.

**The plan, changed while placing** (authoring defaults):

- **The quay's row is warehouses.** In the warehouse family the 15 m warehouse is preferred, and a tenement fills only what a warehouse cannot. The run had chosen 10 m tenements throughout, which left the port with no warehouse.
- **A villa on the embankment**, at W1C's south-west corner in the Sleepy Quarter, is a rich dwelling. The faction's house had been C5's only villa, and it is no dwelling.
- Since these two changes, every kind of house has dwellings. The plan stands at 153 buildings: 122 row houses, 16 single buildings and 15 works sheds, with 27 passages.

**Tests.**

- The railing test now leaves out a railing piece whose sampled cell meets a wall. A passage's railing ends at the houses (the owner's rule, §17), and in that cell the grid's cover probe, a cell wide, meets the wall's corner as well. That corner reading is Full Cover, and it is the wall's, not the railing's. Of 76 railings, 26 are left out; the other 100 sides are all Half Cover, and all 76 crossings are closed.
- EditMode slice tests: 303 of 303 pass.
- PlayMode slice tests: 53 of 53 pass, with the five diagnostics left out.

## 29.7 The GPU Resident Drawer and GPU occlusion culling, 2026-11-10

Step 7 (§29.1 item 8; [99 Open Questions](../../../99%20Open%20Questions.md) VS-FRAME). Unity 6's GPU Resident Drawer draws the city's renderers through the BatchRendererGroup, and with it comes GPU occlusion culling. It is measured against the SRP Batcher alone, which C5 draws with now.

**What the drawer needed** (reference plan R2 step 8):

- **The kit's shaders.** The facade and window shaders take DOTS instancing as URP's Lit does: `DOTS.hlsl` in every pass that draws the city, and one DOTS property block in each input file. The block holds only `_BaseColor`, which no renderer overrides, so every piece still reads its material's own values. A window still finds its room through its renderer's user value, which the drawer carries.
- **What it takes.** With those shaders the drawer takes 103 138 of C5's 106 143 mesh renderers (97.2 %), against 60 356 (56.9 %) before. The views across the Penny and down Draught Lane look the same with the drawer on and off.
- **What it leaves:**
  - 2 712 renderers in LOD groups with animated cross-fade, which the drawer refuses. They are the houses' small details, whose animated fade the owner asked for (reference plan R2 step 2, 2026-11-08), and the city's trees.
  - 293 renderers with a vendor shader that has no DOTS variant (`Growth/Foliage`).
- **Project settings:**
  - The Graphics settings' "BatchRendererGroup Variants" must be Keep All, or the editor turns the drawer off, and a build strips the shaders' DOTS variants.
  - A build keeps the drawer's compute shaders only while the drawer is on in the pipeline asset.
  - With Keep All, the DOTS variant of the vendor water shader `GapperGames/Volumetric_Water` does not compile.

**Measured.** Seven walker's views at 1920 × 1080:

| Probe | Run |
|---|---|
| Editor play mode, `GpuResidentDrawerTimingTests` | 3 rounds in turn; 60 frames after 30 warm-ups each |
| Development build, `FrameProbe` | 3 rounds in turn; 120 frames after 60 warm-ups each; High quality, no frame cap, no vertical sync |

The build is made by `SliceLoadProbeBuild.BuildForFrames` and started with `-frameTiming`. It sets Keep All and the drawer for the build only.

Medians, in ms:

| | SRP Batcher alone | Drawer with GPU occlusion culling | Change |
|---|---|---|---|
| Editor, frame | 100.4 | 338.1 | +237 % |
| Build, frame | 21.6 | 20.0 | −8 % |
| Build, main thread | 21.5 | 17.3 | −20 % |
| Build, GPU | 4.0 | 4.3 | +8 % |

- **In the build, view by view,** the pooled median falls by 8 %, but the frame moves by no more than 1 ms in six of the seven views. Only down the West Road does it fall, from 27.3 to 25.8 ms; down Long Tongue it rises from 24.2 to 24.5 ms. The main thread falls by 9–23 % in the six city views and rises by 5 % in Gloomwood.
- **The editor's frame is not C4's.** This frame is the whole play-mode loop. VS-FRAME's 31–53 ms were the editor's `Camera.Render` alone, so the two cannot be compared.

**Readings** (the agent's):

- **The city's frame is bound by the CPU.** The GPU works about 4 ms of a 22 ms frame, so GPU occlusion culling saves work the GPU does not need saved, and its own culling costs the GPU a little.
- **The main thread is no longer what holds the frame.** The drawer takes a fifth off the main thread, but the frame hardly moves. With the drawer, the frame runs 2–5 ms longer than the main thread in the city's views. Something else, likely the render thread, then holds it. This is not measured. *Refuted by §29.9:* with the drawer, the render thread works 5.4 ms and waits 8.8 ms for the main thread, and the gap is mostly the main thread's wait for Present.
- **The editor is slower.** In the editor the drawer makes the frame two to three times slower. The likely cause is the editor's safety checks on the drawer's jobs. This is not measured.

**Decision** (the agent's, an authoring default): **the drawer stays off in the pipeline asset.**

- It saves 8 % of the build's pooled frame, and view by view only down the West Road; the long views down Long Tongue and Salt Gullet do not gain.
- It would make the owner's walks in the editor two to three times slower.
- It needs Keep All in the project's Graphics settings. Those are the owner's local settings, which the agent does not commit, and a build with the drawer on and without Keep All loses its DOTS variants.
- The vendor water's DOTS variant does not compile.

The shaders stay DOTS-ready. While the setting is not Keep All, a build strips their DOTS variants, so they cost nothing. Both probes stay, so the drawer can be measured again once the frame's real holder is found.

**Next** (open, VS-FRAME). The frame's cost is the CPU's, and C5 has 106 143 mesh renderers, 44 950 of them facade pieces. The options, measured before they are built:

- (a) measure the render thread first, with the FrameTimingManager's render-thread time in `FrameProbe`;
- (b) merge each house's pieces into one mesh per material, keeping their look;
- (c) the merged far LOD for each house type that §28.1 item 17 allows;
- (d) fewer shadow casters at a distance.

The agent leans to (a), then (b). *(a) is done in §29.9, which puts (c) first.*

## 29.8 The cold load and the views, 2026-11-10

Step 8.

**Load** (`SceneLoadTimingTests`, 2026-11-10 14:08, in the editor with graphics):

- **The cold load.** Veilmouth reads in 4.25 s, wakes in 9.77 s and draws its first frame at 10.39 s. C4 took 3.38, 7.89 and 8.32 s (§28.10).
- **The scene.** C5 holds 130 101 game objects, 46 044 of them in rooms behind windows, with 107 061 mesh renderers and 4 879 LOD groups. C4 held 115 237 game objects, so for 13 % more objects the load takes about a quarter longer.
- **A door's journey** takes 0.46–0.86 s. The first entries into the tavern and the Dwelling are the longest.
- **Not measured:** C5's load in a build. The frame probe's build has the drawer on, so its load would not be the game's.

**Views.** These are in `Logs/SliceSurvey`:

- the walker's views of steps 4–6 (`c5_eye_*`);
- the district views (`c5_city`, `c5_penny`, `c5_port` and the rest);
- the drawer's pairs (`c5_grd_2_*` across the Penny, `c5_grd_4_*` down Draught Lane).

**Open after C5** (99 VS-C5, VS-FRAME):

- Squint Lane;
- the yards' missing rubbish and poses;
- the frame budget and its next lever (§29.9); *met by the far LOD, §29.11*;
- the owner's walk of C5.

## 29.9 The frame's holder, measured, 2026-11-10

Asked for by the owner after §29.7, as its option (a) (VS-FRAME).

**What `FrameProbe` records now.** Besides the frame, the main thread and the GPU, it records:

- from the FrameTimingManager, the main thread's wait for Present and the render thread's time, from the start of its work to Present;
- the renderer's counters: draw calls, set-pass calls, triangles and shadow casters;
- every `Gfx.Wait…` marker the player has, summed over a frame on all threads. Among them, `Gfx.WaitForGfxCommandsFromMainThread` is the render thread waiting for the main thread.

A third mode turns the camera's shadows off. The views, the build and the rounds are §29.7's: 3 rounds in turn, 120 frames after 60 warm-ups each. The report is `Logs/SliceSurvey/c5_frames_rt.txt`.

**Times.** Medians of all seven views, in ms:

| | Frame | Main thread | Its wait for Present | Render thread | Render thread waiting for the main thread | GPU |
|---|---|---|---|---|---|---|
| SRP Batcher alone, as the game ships | 22.15 | 22.09 | 0.02 | 3.21 | 13.74 | 3.97 |
| Drawer with GPU occlusion culling | 19.90 | 17.88 | 1.41 | 5.41 | 8.76 | 4.31 |
| SRP Batcher alone, no shadows | 16.85 | 16.80 | 0.02 | 2.41 | 11.10 | 3.05 |

**Counters.** Medians of all seven views:

| | Draw calls | Set-pass calls | Triangles | Shadow casters |
|---|---|---|---|---|
| SRP Batcher alone | 55 187 | 378 | 13.8 M | 33 460 |
| Drawer | 116 086 | 354 | 6.6 M | 32 601 |
| No shadows | 42 878 | 294 | 9.3 M | 0 |

**Readings** (the agent's):

- **The main thread holds the frame.** As the game ships, the main thread works all but 0.06 ms of the frame and does not wait for Present. The render thread works about 3 ms and then waits about 14 ms for the main thread's commands. The GPU works about 4 ms.
- **§29.7's reading is refuted.** It guessed that the render thread holds the drawer's frame. With the drawer, the render thread works 5.4 ms and waits 8.8 ms. The 2 ms by which the drawer's frame outruns its main thread are mostly the main thread's wait for Present (1.4 ms). What that Present waits on is not measured.
- **The main thread's cost follows the draws.** View by view, its time is 2.1 ms plus 0.37 ms for every thousand draw calls (R² 0.97). It runs from 6.8 ms for 13 113 draw calls in Gloomwood to 28.6 ms for 66 823 down the West Road. With the drawer, the slope falls to 0.11 ms per thousand, but the fixed cost rises to 5.0 ms.
- **Shadows take 22–29 % of a city view's frame,** and 17 % in Gloomwood. That is 3.4 ms across the Penny and 8.2 ms down the West Road. In the long views they add only about 10 600 draw calls. So their cost lies mostly in culling about 45 000 shadow casters through four cascades, to 150 m, at a resolution of 4096.
- **The GPU is not the limit.** It works 2–6 ms in every view and mode.
- **Not measured:**
  - The share of the main thread spent on the city's scripts. Gloomwood's 6.8 ms, with few draws, is its upper bound.
  - The share of SSAO's depth-normals prepass in the draws. SSAO takes depth and normals in Forward+, so the prepass draws every opaque piece a second time.

**Next** (open, VS-FRAME; the agent's reading):

- **The budget.** The frame budget is still open. The agent proposes 16.7 ms (60 frames a second) as the measure: in this development build, at 1920 × 1080, on High quality, on the owner's machine. As the game ships, the long views take 20–29 ms and the Penny 14 ms.
- **The levers,** by what they would take off the main thread:
  - (c) the merged far LOD for each house type. Far houses make most of a long view's draws and casters. §28.1 item 17 allows it only if the budget is not met.
  - (d) fewer shadow casters. If small details and far LODs cast no shadows, the look stays close. Fewer cascades or a shorter shadow distance change the look, and that is the owner's call.
  - (b) each house's pieces merged at every distance. The *Kit houses* rule of the record forbids it, so only the owner can open it.
  - (e) SSAO without its prepass: after the opaques, from depth. It changes the grade, so it is measured first.
- **The agent leans to (c) with the part of (d) that keeps the look,** each measured with `FrameProbe` before and after, once the owner sets the budget.

## 29.10 The frame budget and the merged far LOD, 2026-11-10

**The owner's decision** (2026-11-10, after §29.9):

- **The frame budget is 16.7 ms**, which is 60 frames a second. It is measured by `FrameProbe` in the development build, at 1920 × 1080, on High quality, on the owner's machine.
- **The merged far LOD for each house type is built.** §28.1 item 17 allowed it once the budget is not met, and the budget is not met: as the game ships, the long views take 20–29 ms.
- **Its shadows are part of it,** as far as they keep the look (§29.9 (d)).

**Reading of the budget** (the agent's, an authoring default): every one of `FrameProbe`'s seven views keeps its median frame within 16.7 ms. A pooled median would hide the long views.

**The far LOD** (the agent's design, authoring defaults; `VeilmouthFarLod`):

1. **One far LOD per house type,** built in place in the 67 baked prefabs of `VeilmouthKitHouses`, as `RedressAll` builds the rooms. A bake would give every piece a new file ID, and the city's overrides find the pieces by their IDs. The far LOD is a child of the house's Model, named `FarLod`.
2. **What it is.** One mesh for the type, with one submesh for each material. It is drawn by one renderer for each shadow setting: the pieces that cast shadows, and the glass, which casts none. Its geometry is the kit's own: every piece's mesh, baked into the Model's space, with the same materials. Nothing is simplified, atlased or made new.
3. **What it leaves out:**
   - the Details, which keep their own LOD group and fade (R2 step 2);
   - the rooms, which keep their own distance switch.
4. **The switch.** A second LOD group stands on the house's root:
   - LOD0 holds the pieces the far LOD holds;
   - LOD1 is the far LOD, and it is never culled.

   The switch comes at 50 m (authoring default), set for each type from its own size with the camera's 60° view. 50 m lies beyond the built rooms' 46 m, so a far house is a shell, as its rooms are already off. The switch has no cross-fade. Both levels draw the same geometry with the same materials, so the swap cannot be seen, and the window glass has no cross-fade. With no animated cross-fade, the group also stays open to the GPU Resident Drawer (§29.7).
5. **Windows.** A pane finds its built room through its renderer's user value (`VicWindowRooms`), and one renderer for a whole house has one value. So the far mesh carries each pane's room in its third UV channel, as two 16-bit halves. The window shader reads that value where it is not zero, and the renderer's own value elsewhere, so every pane keeps its far look.
6. **The scene.** After placing the houses, the city changed some of them:
   - strange glass on four houses (`VeilmouthMystic`);
   - walled-up back doors (`VeilmouthCity.BackDoors`);
   - hidden stoop rails (`SinkStoops`).

   Rails are Details, so they are not in the far LOD. For every other house that differs from its type, a sync over Veilmouth handles the far LOD:
   - where only materials differ, the house keeps the type's mesh, with its own materials mapped;
   - where pieces differ, the house gets a far mesh of its own, built from its own active pieces;
   - either way, its LOD0 is set from its own pieces.

   The sync runs once, at the owner's request, and the scene is saved.
7. **Shadows** (the look-keeping part of §29.9 (d)): a far house casts its shadow through its far LOD's one renderer instead of through its pieces. The Details keep their own shadows.
8. **Memory.** The far LOD is not static for batching, so each type's mesh stays shared by all its houses. Its memory is reported when it is built.
9. **Checks.** Tests check that the far LOD holds every triangle of the pieces it replaces, with the same materials, and that panes keep their rooms. Views at the switch distance, before and after, check the look. `FrameProbe` measures the frame before and after.

## 29.11 The far LOD built and measured, 2026-11-10

Built by `VeilmouthFarLod` as §29.10 designed, with the changes the building found. Batch: `-executeMethod GameDev.Slice.EditorTools.VeilmouthFarLod.Run` (the types, then the scene); report in `Logs/SliceSurvey/c5_farlod.txt`.

**What was built:**

- **The 67 house types** have their far LOD: 22 512 renderers and 24 797 draws near against 412 draws far. Its 67 meshes hold 1 565 628 vertices: 65.4 MB of vertex and index data, 131.2 MB of asset files in git.
- **One renderer a type, not two** (revises §29.10 item 2). Every piece the far LOD holds casts shadows in the kit's settings, the glass included, so the glass needs no renderer of its own: the window shader has no shadow pass, and the glass casts none either way.
- **The vertex** is 40 bytes: the position as three floats, the normal and tangent as four SNorm16 each, the UV as two floats, and the pane's room as two UNorm16 halves in the third UV channel. SNorm16 is one part in 32 767, far finer than a house 50 m away shows. The far mesh matches its pieces' glass vertices to five decimals.
- **The yard doors stay out** (revises §29.10 item 6). The city walls up some back doors (`VeilmouthCity.BackDoors`), and with them in, 77 houses needed a far mesh of their own, 64 MB more. A yard door's pieces are now left out of the far LOD and drawn at every distance, as the Details are: the door, its wall, trim, steps and rail within 1.7 m of the door's marker (`VeilmouthKitHouses.AtYardDoor`). Every house then keeps its type's mesh. Of Veilmouth's 145 kit houses, 144 are as their type, and one, Harrow & Tincture, has its own materials mapped onto the type's mesh; none needs a mesh of its own. The scene's houses draw 46 573 times near against 882 far.
- **The far glass** is a material variant of each kit glass, kept in `FarLod/Glass/`, which overrides only the window shader's `_FarLod`. A later change to a kit glass reaches its variant. `_FarLod` does two things:
  1. **It writes depth.** One renderer draws its panes in their index order, and beyond 46 m a house's rooms are off. So through a front window the back window of the empty house shows, and with no depth written it drew over the front pane wherever it came later in the mesh. Beyond 46 m a pane is opaque, so writing depth changes nothing else.
  2. **It reads each pane's room from the mesh.** A kit mesh has no third UV channel, and what a shader reads from a missing channel is undefined. In the first build the kit's own glass read noise there and took it for a room, a fault in the near look between 32 and 50 m. Now only the far glass reads the channel; the kit glass reads its renderer's value as before.
- **The window hash** takes its half-metre cell with an odd sixty-fourth (+0.28125) instead of +0.25. The kit puts anchors on a sixteenth of a metre, and with +0.25 some lay on a cell's edge, where the last bit of a float chose the cell, so the far LOD could light another pane, or half of one. Which windows are lit at night changes once with it.
- **Builds keep the far LOD.** `VeilmouthKitHouses.Bake`, `RedressAll` and `StripPorches` build it again, reusing its objects and meshes, so their file IDs, GUIDs and the scene's overrides hold.

**The look** (`VeilmouthRenders.RenderFarLod`; `Logs/SliceSurvey/farlod_*`, `c5_farlod_views.txt`). The seven views of `FrameProbe` were rendered at 16:00 and 23:00, with the kit houses alone so that the trees' wind is no difference. Each view compares the houses drawn with their pieces against the houses as played, switching at 50 m, and a crop shows the box that differs most:

- **As played,** the houses differ from their pieces in at most 0.002 % of a view's pixels by more than 8 of 255 in any channel, with a mean difference of 0.000, by day and by night. The swap cannot be seen.
- **The first build** differed in up to 1.07 % at night, across the Penny: lit windows that differed, from the two faults the far glass now fixes.
- **With the far LOD forced at every distance,** at most 0.036 % differs (Thimble Street at night). This happens only in houses nearer than 50 m, which never draw their far LOD in play.

**The frame** (`FrameProbe` in the development build, 1920 × 1080, High, the owner's machine; `Logs/SliceSurvey/c5_frames_farlod*.txt`; medians of 3 rounds of 120 frames, ms):

| View | As shipped (§29.9, 14:27) | Far LOD, first build (15:14) | Far LOD with its glass, three runs (15:51–16:10) |
|---|---|---|---|
| down Long Tongue | 24.55 | 9.35 | 11.00–12.32 |
| down Salt Gullet | 24.90 | 12.04 | 13.13–15.17 |
| across the Penny | 14.22 | 6.33 | 7.29–10.36 |
| down Thimble Street | 19.70 | 9.72 | 12.89–16.04 |
| Draught Lane | 22.68 | 9.18 | 12.80–14.46 |
| the West Road | 28.60 | 7.57 | 10.70–12.80 |
| in Gloomwood | 7.36 | 5.23 | 6.96–8.54 |
| all seven | 22.15 | 8.77 | 11.33–12.86 |

- **The budget is met.** Every view's median frame is within 16.7 ms in every run.
- **What the far LOD takes away.** In the first build the pooled frame falls from 22.15 to 8.77 ms, and the main thread from 22.09 to 8.32 ms. The median view's draw calls fall from 55 187 to 13 490, and its shadow casters from 33 460 to 7 384. The GPU stays at about 4 ms and the render thread at about 2 ms. With the drawer the pooled frame is 9.01 ms, and with no shadows 7.64 ms.
- **The later runs were taken on a busier machine.** Gloomwood is the control: its draw calls (7 272 against 7 304) and set-pass calls (204) are the same in every run, yet its frame rose by 33–63 % between them. The owner's browser was working at the time. The far glass adds no draw call and 22–58 set-pass calls in the city's views, where its depth state alternates with the kit glass's; its cost cannot be told from that noise.
- **The margin.** On the busier machine, Thimble Street keeps 0.7 ms of the budget. If a later change eats it, the next lever is §29.9's (d) beyond what this LOD took: fewer cascades or a shorter shadow distance. That changes the look, so it is the owner's call; no shadows saves about 1.1 ms of the pooled frame.

**Tests:** `KitHouseFarLodTests`:

- every house switches at 50 m with no cross-fade, and its LOD0 is the pieces merged, with no detail or room;
- every far LOD holds every triangle of its pieces by shadow setting and material, with kit glass drawn by its far variant;
- every pane keeps its room;
- the far glass is the kit glass with only `_FarLod` overridden, and no kit glass sets it.

`VeilmouthSceneTests.EveryKitHouseFarLodHoldsItsOwnPieces` checks the city's houses against their own pieces. EditMode 308/308. PlayMode 53/53: `LoosePropsTests.ThePlayerDoesNotShoveATable`, in the tavern, which this work does not touch, failed once in the full run (the table moved 1.06 m) and passed alone with its fixture, 11/11. Its push is moved by the frame's length, so it can fail on a slow frame.

# 30. The owner's street lamp and statue, 2026-11-10

The owner, 2026-11-10, with ten interior pieces and six people (asset map §1, §4; furnishing rules §10):

- **Every street lamp is the owner's Tripo lamp** (`vintage_street_lamp`), a thin post with a box lantern, in the place of Modular Victorian City's lamp post.
- **One bronze statue** (`bronze_statue`), a gentleman in a top hat on his pedestal, stands somewhere in the city: one in the whole city.

Both are exceptions to §25.6's rule that the Tripo models furnish interiors only, by the owner's word, as the hanging sign is.

## 30.1 The agent's readings (authoring defaults)

- **The lamp keeps the kit key `LampPost`**, so the 251 lamps of C5 change in place: each keeps its cell, its kerb spot (§26.1) and its light. It stands 4 m high, the lamp post's measured height (§2), and its lantern is where the old one's was within 0.3 m, so `VeilmouthMystic.Lamps` puts the gas light in it as before.
- **Its glass.** The Tripo lamp is one mesh with one material, so the whole lamp cannot glow. A small box of our own, the lantern's glass, stands inside the lantern; its material's name holds "glass", which is how `VeilmouthMystic.Lamps` finds the panes it lights at night.
- **Its weight.** The lamp holds 4 935 triangles, 1.24 million for 251 lamps, against the old post's lighter mesh. It is measured with FrameProbe against the 16.7 ms budget (§29.10); if a view breaks the budget, the lamp gets a far level beyond 60 m (authoring default) that casts no shadow.
- **The statue stands on Drowsy Steps**, the raised square before the cathedral, in the middle of its open paving and facing south down the steps, 4.8 m high with its pedestal (authoring default), so the figure reads larger than life over the passers-by. It is not Stone Grandad: Stone Grandad keeps its 12 m placeholder on the Penny's island until its own model comes (SLICE_MODELS_WANTED.md § C4); the owner may put this statue there instead.

## 30.2 Built and measured, 2026-11-10

**The lamps** (`SliceKitTripo.LampGlass`, `VeilmouthMystic`; renders `Logs/SliceSurvey/lamp_{noon,night}_{0,1,2}.png` and `lamp_*_close.png`):

- **In place.** All 251 lamps of C5 wear the Tripo model under the kit key `LampPost`, 4 m high.
- **The glass** is not the box §30.1 planned but eight quads of our own. Two sit on each face of the lantern, either side of its middle bar, just outside the measured panes (0.745–0.840 of the lamp's height). By day their material, `Kit_LampGlass`, is a dark pane.
- **Built upright first.** The glass is built in the wrapper's upright frame and then hung under the model. Built in the model's own frame, which Tripo turns −90° about X, it lay flat, and its collider stood 3.5 m out.
- **By night** `VeilmouthSky` swaps the glass for `Veilmouth_Gaslight`, and each lamp's light burns at the glass's centre.
  - The gaslight is now **URP Unlit**, at 1.15 × the gaslight colour.
  - Before, it was a Lit material whose `_EMISSION` keyword the asset never kept: its `m_ValidKeywords` had stayed empty since C3. The new panes therefore rendered black at night until this change.
- **Checks.** The mystic layer's city check finds no kit fault in 5 794 blocks. One walker's route met a new lamp; its route was planned again, 62 m long, and no route meets a lamp or the statue.

**The statue** (`VeilmouthOrnaments`; render `orn_09.png`) stands on Drowsy Steps at plan (539, 350), 27.6 m up, on the cathedral's axis and clear of its door, facing south down the steps.

**The frame** (`FrameProbe`, development build, 1920 × 1080, High, the owner's machine; `Logs/SliceSurvey/c5_frames_tripo.txt`; medians of 3 rounds of 120 frames, ms). The city now has the Tripo lamps, the statue, and the Tripo people in place of the Dummies.

| View | As shipped | No shadows |
|---|---|---|
| down Long Tongue | 10.02 | 8.88 |
| down Salt Gullet | 11.95 | 9.39 |
| across the Penny | 6.82 | 5.87 |
| down Thimble Street | 10.33 | 8.54 |
| Draught Lane | 10.13 | 9.35 |
| the West Road | 8.46 | 7.75 |
| in Gloomwood | 4.58 | 4.20 |

- **Within budget.** Every view keeps its median within the 16.7 ms budget (§29.10), 9.62 ms pooled, so the lamp needs no far level; §30.1's fallback is not built.
- **What the lamps and people add.** Pooled over the views, against §29.11's last run:
  - triangles rose from 14.3 to 16.2 million;
  - draw calls fell from 13 490 to 13 361;
  - shadow casters fell from 7 384 to 7 237.
- **Not a speed-up.** The frames are lower than §29.11's last runs, but the machine's load differed between those runs (§29.11). They show only that the budget holds, not that the frame got faster.

# 31. The owner's walk of C5, 2026-11-10

The owner walked C5 with the Tripo lamps and people and sent eleven remarks with screenshots. The first ten are the city's; the eleventh, the sizes of interior pieces, is in [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §11.

1. Trees and lamps stood along the roads at random, so a tree and a lamp could stand together, two lamps could stand side by side, and a lamp could stand on a pavement by a house. They should follow each street's own order, by the rules that streets are lit and planted by.
2. Some lamps stood in the pavement, their bases inside the kerb.
3. The wards stood out too much, and some hung in the air past a wall's end. They should read as graffiti, only a little lighter than the wall.
4. Some railings stopped short of the house they belong to.
5. Some railings had no end post on the side of the way through.
6. One building had a hole: a wall not built, showing the inner walls and the setts under it.
7. A slit of water stood between the bank and the river's stone edge.
8. The joints of the river's parapet showed. They should not.
9. A slit stood between the bank and a bridge. The bridges should meet the ground more tightly.
10. Some roads were painted in another texture. Every road of the city should have the same one.

**Measured before the work** (`VeilmouthWalkAudit`, `Logs/SliceSurvey/walk_audit.txt`):

- 251 lamps and 82 road trees.
  - 85 pairs of lamps stood under 12 m apart.
  - 40 lamps stood within a road tree's crown, 6.5 m of its trunk.
  - 6 lamps stood within 2 m of a wall.
  - 115 lamps' bases reached into the kerb. The lamp's base reaches 0.366 m from its axis in its lowest 0.3 m, against §26's 0.3 m from the kerb.
  - The lamps the owner saw sunk in the pavement were another fault, found later: a block on the carriageway stands on a 0.1 m step at or below the ground, so a lamp's foot sank up to 0.1 m into the setts (§31.4).
- 2 of 19 wards were off their wall.
- 76 railing pieces: 12 ends stopped short of a wall, and 53 free ends had no post. Most of those were posts hidden at a house's plot whose wall stood further off.
- A continuous slit, 0.7 m wide, ran the length of the works' bank of the south river, between its kerb and the bank.
- The promenade's parapet changed height three times, and its texture began again every 4 m.
- The 145 houses were cast into at every storey; glass counts as a wall. No ray passed through a wall at any of the 77 walled-up back doors or anywhere else, so item 6 was not found (§31.3).

## 31.1 Street lamps and road trees: one rhythm on each kerb

**Sources** (the agent's reading, 2026-11-10):

- SP 42.13330 (Russia) asks 4 m between a tree's trunk and a lighting post, 2 m from a trunk to the carriageway's edge, and 5 m from a trunk to a building's wall.
- British practice lights a street on one side where it is narrow, staggered where it is middling, and on both sides opposite each other where it is wide. Where trees line a street, it sets the columns midway between the trees.
- Victorian gas lamps stood about 25–35 m apart.

**The rule.** A street's kerb is cut into runs where a junction, a carriageway or a rounded corner breaks it. Each run is divided into equal bays of about 30 m, with at least one bay. The lamps and the trees of a run keep the same bays:

- **A lamp stands in the middle of each bay**, 0.45 m out from the kerb on the carriageway.
- **A road tree stands at a quarter and at three quarters of each bay**, midway between two lamps, in its pit flush on the kerb. A 30 m bay's trees stand 15 m apart and 7.5 m from the lamps.
- **Which kerb is lit:**
  - Both kerbs planted, where the carriageway is 8 m or more (§27): a lamp on each kerb, opposite each other.
  - One kerb planted: the lamps on the other kerb. Where the other kerb has no pavement, they go on the planted one, between its trees.
  - No trees, on a street wider than 12 m: the two kerbs take the bays in turn, which staggers the lamps.
  - No trees, narrower: one kerb, the wider pavement's.
- **A station may move.** A lamp moves at most 3 m: to a party wall first, the nearest, then to the nearest metre that stands in no door's way (§26). A tree moves at most 2 m. A station with no such place stays empty.
- **Clearances.** No two lamps stand within 12 m of each other, except a wide street's opposite pair. No lamp stands within 6.5 m of a road tree's trunk. That distance is the mature tree's crown, 6.15 m along the row (measured, §27), and 0.35 m more. Road trees stand at least 8 m apart.
- **Order.** Lamps are placed first: a bridge's, the piers', the Penny's ring, the quay's, then each street's. Then the trees. A square's corner lamps come last, each only where no lamp stands within 12 m and no house within 2 m. Otherwise the lamp moves up to 2 m further in along the square's diagonal, or none stands there.
- **The base clears the kerb.** The post stands 0.45 m out from the kerb: the base's measured 0.366 m from its axis, with 0.084 m to spare.

**Authoring defaults:** the 30 m bay; the 3 m and 2 m moves; 12 m between lamps; 8 m between trees; 2 m from a house; a lamp only on a run of 8 m or more and a tree only on one of 6 m or more; and 0.084 m of spare. Two numbers are measured: the 6.15 m crown and the 0.366 m base.

## 31.2 Wards, railings, the river's edges, bridges and carriageways

- **Wards (item 3).** A ward is old chalk and paint, lit as its wall is and never glowing. Its colour is a pale grey (0.78, 0.80, 0.84) with alpha 0.42 for a door's sign and 0.34 for runes (authoring defaults). It is put only where one flat face of the wall lies under the whole of it, read by 5 × 3 rays. It may slide up to 1.5 m along the wall, then narrow to two thirds, to find such a place, or it is not put.
- **Railings (items 4, 5).** An end is read against the colliders, not the plots, which reach past a house's face.
  - An end with a wall within 1.6 m along its line is drawn out to meet it. The end piece is stretched along the run and its posts keep their width. The bare end then abuts the wall with no post.
  - Every other end is free and has a post.
  - Authoring default: the 1.6 m reach, at most 1.8 times the bars' spacing on a 2 m piece.
- **The river's edges (items 7, 8).**
  - The promenade's parapet is one length between two bridges, at the bridges' own parapet height, the deck's top + 1.1 m.
  - The works' bank has stone under the bank's edge, 1.5 m beyond its kerb and 5 cm under the ground. No water shows between them.
  - Every stone work lays its texture in the world's frame, so boxes side by side read as one length of stone.
- **Bridges (item 9).**
  - On a natural bank, wing walls of the same stone stand either side of the abutment, 1.5 m out and 5 cm under the ground.
  - A bridge's carriageway through a pavement is no wider than the bridge, so the pavement meets its parapet.
- **Carriageways (item 10).** Every street's carriageway is the setts, whatever its layout surface; grass is the one exception. The ground under the whole city is setts, so the corners the pavements round off at a junction read as carriageway, not as the old cobbles. Squares, yards, passages and gardens keep their own ground.

## 31.3 The hole in a wall (item 6)

The owner's sixth screenshot shows the inner walls of a house and the setts under it through an unbuilt wall. The agent looked for it in three ways:

- **Rays through the walls.** Each of the 145 houses was cast into from every side at every storey, at a ray each 0.5 m. Its colliders were cooked for the cast, and glass, whose mesh cannot be read, took a box. A ray that went more than 0.8 m deeper than the side's median depth was flagged. The first pass also read the far LOD's merged mesh and flagged only two houses. Read without it, 12 are flagged:
  - nine civic houses and villas, the Town Hall among them, whose stepped facades have a built deeper part, as a render of the Town Hall shows;
  - three terraces and tenements with 1, 4 and 8 rays at an upper storey, through a window.
  No hidden piece stands near any of them.
- **The walled-up back doors.** Each of the 77 has the kit's plain wall in the house's paint. A yard door's pieces stay out of the far LOD and draw at every distance (§29.11).
- **A house standing over its ground.** Rays cast inward 5 cm over the ground, 0.4 m out from the walls, flag a gap of more than 8 cm. They flag all 145 houses, so the check cannot tell a gap from a house's base and reads nothing.

**Not found.** Renders from 2.5 m outside each flagged spot, and from further off, showed built walls; the narrow yards' renders landed inside the neighbouring rooms. The owner could not find the place again either (2026-11-10), and will name the building when a hole is seen next. The item is closed as not reproduced.

## 31.4 As built

Measured after the work by `VeilmouthWalkAudit` (`Logs/SliceSurvey/walk_audit.txt`):

| Check | Before | After |
|---|---|---|
| Street lamps | 251 | 149 |
| Road trees | 82 | 51 |
| Pairs of lamps under 12 m | 85 | 9, every one a wide street's opposite pair across 9.1–11 m |
| Lamps within 6.5 m of a road tree | 40 | 0 |
| Lamps within 2 m of a wall | 6 | 0 |
| Lamps whose base reaches into the kerb | 115 | 0 |
| A lamp's foot over the ground | down to −0.1 m | 0.000 m |
| Road trees under 8 m of another | – | 0 |
| Wards off their wall | 2 of 19 | 0 of 19 |
| Railing ends short of a wall | 12 of 152 | 0 |
| Free railing ends with no post | 53 | 0 |
| Walled-up back doors with no plain wall | 0 of 77 | 0 of 77 |
| Water edges with a slit | the works' bank, 79 readings | 0 of 12 profiles |
| Steps in the promenade's parapet | 3 | 1 |
| Kit faults in the seven interiors and the Dwellings | 0 | 0 |

- **A lamp's foot.** A block on the carriageway keeps its 0.1 m step, and its model is raised to the ground, so no foot sinks.
- **A building's door.** A kerb lamp keeps clear of every portal's door as well as of the recorded doors. A building that opens has its door where its portal is: a lamp stood 2.0 m beside such a door at plan (580.5, 429.0), which the door's marker did not show (`VeilmouthCityTests.NoLampStandsInFrontOfADoor`). It now stands 3 m from it.
- **Corners and boards.** A square's corner lamp also stands 1.5 m clear of every solid piece, such as the Notice Board, and the Penny's ring lamps keep 2 m from a house, as a street lamp does (authoring defaults).
- **Railings.** After the posts at a wall are hidden, the bars' end is measured again. An end that still stops 0.01–0.55 m short of the wall is drawn out once more (authoring default).
- **Not yet seen by the owner.** These fixes wait for the owner's walk.

# 32. A row on one level, 2026-11-10

**The owner's remark** (screenshots 13 and 14): on sloping ground the houses of a row stand stepped, so their plinths and string courses jump at every party wall, and a plinth's cut end shows. The owner offered three ways: tilt each house to the ground, make the city's ground level, or stand a row on a level base that rises over the pavement on its low side, with steps up to its doors. The owner left the choice to the agent.

**The cause** (measured): the city stands almost level, at 25.9 m with a 1.5 m rise under the Penny (§28.6), so a street falls 1.7 % at most. But each house took its own floor: the highest ground under it, or the pavement's top before it, rounded up to 0.1 m, and lowered 0.6 m if it stood on a plain stoop (§28.1 item 5). Of the 58 pairs of houses wall to wall, 30 stood on different levels, 0.1–0.4 m apart, and every band of their fronts broke at the wall between them.

**The choice: a row on a level base.**

- **Tilting** is rejected. A tilted house leans off the plumb, its windows and doors skew, and no real street builds so.
- **A level city** is rejected. It takes away the rise under the Penny and the fall to the river and the sea, which the city's ground was shaped for (§28.6). The same stepped joints would come back on any later hill.
- **A level base** is how Victorian terraces stand on a fall. A row keeps one ground floor, and its low end stands on a higher plinth, with more steps up to its doors. It needs no new piece: the stone plinth under a house where the ground falls and the steps from a raised door to the ground already exist (§13).

**The rule** (`VeilmouthCity.Terrace`):

- **One level for each run.** A run of houses wall to wall, between two passages or a row's ends, takes the highest own floor among its houses. Every other house in the run is raised to it, so its plinth, string courses and eaves meet its neighbours' flush.
- **The low end.** A raised house's stone base shows under it, laid in the world's frame as the other stone works are, so it reads as one plinth along the run. Its door gets the steps it needs, as on any raised door.
- **A run too steep for one level.** Where a run's own floors spread more than 1.2 m (authoring default: eight steps of 0.15 m), it is cut at a party wall into the fewest levels, from its start.
- **A run is read by its floors as they would stand**, sunk or not: a house on a plain stoop beside one on its own stair is raised to it.
- **What is not changed.** Detached houses, villas, civic fronts and the chapel that stand alone keep their own floors.

**Checked by** `VeilmouthWalkAudit`: every pair of houses wall to wall, by their floors' difference, and the street doors highest over the ground before them, with renders.

**As built** (`VeilmouthCity.Lifts` for the planner's buildings, `VeilmouthCity.Level` for each run; `Logs/SliceSurvey/walk_audit.txt`):

- C5's block houses are the planner's buildings, one by one, so they are grouped into runs by their front and their touching ends. A first build levelled only the rows of the older path, which C5 leaves empty, and changed nothing.
- **Party walls:** 0 of 58 pairs on different levels (30 before, up to 0.40 m apart).
- **Lifts:** the most a house is raised is 0.70 m; no run spreads over 1.2 m, so none is cut.
- **Doors:** of 138 street doors, 29 stand more than 0.5 m over the ground 1.5 m before them, the highest 0.83 m. Each has its stoop and the steps down from it (`walk_raised_*.png`).
- **Not yet seen by the owner.**
