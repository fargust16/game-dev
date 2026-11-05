# Veilmouth City Plan (C1)

Version: 0.1

Status: Draft — proposed by the agent on the owner's play test; not accepted

Last Updated: 2026-11-05 (first draft, from the owner's fifteen remarks after playing the dressed C0)

Depends On:
[11 Vertical Slice](../../../11%20Vertical%20Slice.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §2, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md)

Referenced By:
[VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) S0.6, [99 Open Questions](../../../99%20Open%20Questions.md)

---

# Purpose

The first dressing of C0 put real models on the accepted map, and the owner's play test showed the map itself was the problem: a few streets in a field, houses as boxes on grass, props scattered without a reason. This document designs Veilmouth as a city before anything is built again: its zones, its streets in section, its plots and what stands on them, its courtyards, its edge and gates, its ground, and its interiors. It keeps everything C0 holds (the seven buildings that open, the Masters, the Ritual Site Candidates, the resources and beasts outside) and moves it into a believable city.

Nothing here is built until the owner accepts C1. Names are placeholders. Every size is an authoring default, labelled **measured** where it comes from a model.

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
- **Size:** the door and storey of the modules are measured when the builder is written; if a ground-floor storey comes out under 3.5 m or a front door under 2.5 m, the modules are scaled up together until they reach it (a **measured** factor, not a guess).

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

Built from BK_AlchemistHouse (owner, 2026-11-05): `IntWall01a/b`, `IntWall01door`, `IntWall02*` with windows, `IntWoodFloor01`, `StoneFloor01`, `IntCeiling01` with beams and pillars, `WoodStairs`, `Door`, the `Stove`, `TableSink`, cupboards, shelves, drawers, tables, chairs, stools, carpets, paintings, books, bottles, flasks, jars, candles and oil lamps, the cauldrons and lab sets. Its materials are built-in Standard and get URP copies as the Tripo ones did. Ceilings 3.3–3.6 m; rooms that read as rooms, not boxes:

| Interior | Rooms (authoring default) |
|---|---|
| Soot & Bell | taproom 14 × 10 m with the bar, fireplace and tables; kitchen 6 × 5 m with the stove and sink; back door to the yard; stairs up to a landing with closed doors; the bed in a let room 5 × 4 m |
| Harrow & Tincture | shop 8 × 6 m with counter and shelves of bottles; laboratory 8 × 6 m with cauldron, alembic, lab sets, books; store room 4 × 6 m |
| Faction's house | hall 10 × 8 m with the rostrum and pews; office 6 × 5 m; archive 6 × 5 m with shelves |
| Wardens' station | front desk room 8 × 6 m; office 5 × 5 m; guard room with the stove; three cells along a corridor, the owner's bars and door |
| Missing person's room | one tenement room 6 × 5 m: bed, wash stand, stove, table, chair, the cache; the stair landing outside |
| Cellar | three vaults of 6 × 6 m joined by arches, barrels, the circle's place |
| Old chapel | nave 8 × 16 m with pews; vestry 4 × 5 m |

---

# 9. The ground

The agent shapes the slice's ground once from C1 with an authoring tool, at the owner's request of 2026-11-05; the owner then changes it by hand. It is not the production terrain pipeline and touches nothing of TerrainGen. Heights (authoring defaults): sea bed 18–20 m; the river bed 21 m in a channel of 40 m with stone embankments to 26 m; quays 26 m; the city's low streets 26 m; the Ridge plateau 34 m behind a retaining wall; the avenue climbs 8 m over 100 m (8 %); the Soot Stair drops 8 m over 50 m in flights; the ore hill 40–70 m across the river; the area's edge steep beyond 45°. Paint: grass in fields and gardens, forest floor in the forest, rock on the hill and the edge, sand and mud on the shore; streets carry their own paving.

---

# 10. Open for the owner

1. **Whose palace?** Veilmouth is not the full game's capital (21's Capital Gate). The agent recommends the **Governor's Palace**, the Crown's residence in the city, so the full game keeps its capital elsewhere. Alternative: a royal summer palace.
2. **Only right angles?** The kit and the Combat Grid assume 90° turns. The agent recommends keeping them, and getting an older, crooked feel from uneven blocks, jogs, dead ends, and the curve of the embankment drawn in steps. Alternative: some streets at 45°, which needs the kit check and the grid tested at that angle first.
3. **C1's size.** The city grows from 200 × 200 m to about 480 × 340 m, and the area from 700 to 1000 m. A run across takes about three real minutes. Smaller is possible by cutting the terraces or the market quarter.

---

# 11. Order of work after acceptance

1. Measure the Victorian modules and BK_AlchemistHouse; URP copies of BK's materials.
2. The ground (§9).
3. The plot builder and the city (§3–§7); the C0 Map zones become C1's; tests follow.
4. The seven interiors (§8).
5. Trees and green (§6.2).
6. Walk it, render it, and hand it to the owner.
