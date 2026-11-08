# Veilmouth City Plan (C1, C2, C3)

Version: 0.24

Status: Draft — C1 accepted by the owner, 2026-11-05; C2 (§21), which replaces C1's layout, accepted 2026-11-06; C3 (§24), which replaces C2's layout and houses, asked for by the owner 2026-11-08 and waiting for the owner's walk; sizes remain authoring defaults

Last Updated: 2026-11-08 (§24: C3, the kit city, at the owner's request: realistic street sections on the same area, the Market Place on the Crown Axis, passages and yards, the owner's Victorian Kit at ×1.25 for every house, and a mystic layer by the new world style guide. Earlier: §23: the owner's second set of ornaments: three different angels in green places in three parts of the city, a pair of great lions before the Governor's Palace, birdbaths in every rich garden; the first angel turned to its gate. Earlier: §13, §14: the Soot & Bell's back door is an exit to its yard, the pack's shop door in the house's yard side with its portal, by the owner after VI-8. Earlier: §14: the missing room's landing no longer has a closed neighbour's door, which led nowhere (owner, VI-8). Earlier: §23: the owner's garden ornaments placed: the angel pouring water at the heart of Wardley Garden, birdbaths in two Ridge villas' front gardens and the faction's. Earlier: §22: VI-4 rebuilt the Dwellings, so §14's and §16's rooms are replaced by `VICTORIAN_INTERIORS_PLAN.md` §7–§8. Earlier: §22: Victorian Interiors chosen by the owner for every interior; the rebuild planned in `VICTORIAN_INTERIORS_PLAN.md`. Earlier: §21: C2 accepted by the owner and built: the ground shaped again and the city rebuilt. Earlier: §21: C2, the smaller city, proposed: 300 × 204 m at the owner's ×0.6, crossroads every 40–90 m, the Ridge 4 m high. Earlier: §20: the owner's fourth play test: railings without holes, the bridge closed against the ground, the bar by its keeper; a smaller city with more crossroads waits for C2. Earlier: §19: detached houses, by the owner after the third play test: every railed house stands alone on its own green plot, railed all round with one gate opposite its door; §3.3, §3.4 and §5.2 changed to match. Earlier: §18, remark 7: loose props built, by kit standard §4. Earlier: §18: the owner's third play test: shelves, paintings, and beds fixed; detached houses, loose props, a room constructor, transport, cabs, and animals recorded. Earlier: §17: the owner's second play test fixed and rerun at the owner's request: still dressing, game scale ×1.2, BK's 4 m module, walls set by their infill, one-style steps, props on slopes, railings without holes, blue shop floors; §14 and §16 resized. Earlier: §16: the Dwelling scene, its generator, and a street door on all 265 dwellings, built by step 4a's first half; breaking in waits on S2, S3, S5a, and S5c. Earlier: §15: the owner's remarks after the play test of the built city and interiors fixed in both passes, rerun at the owner's request; §2, §13 and §14 changed to match. Earlier: §14: the seven interiors built once from §8 by `VeilmouthInteriors`, BK's walls measured as single-sided, brick for stone rooms and plaster for the rest, ceilings at 2.9 m. Earlier: §8, §11: the Dwelling scene for every NPC home, by the owner, after the seven interiors. Earlier: §13: the city built once from C1 by `VeilmouthCity`, with its streets, works, houses, doors, lamps, green, and what is still missing. Earlier: §9: the ground shaped once from C1 by `VeilmouthGround`. Earlier: §2, §8: the modules and BK_AlchemistHouse measured; the Victorian modules are built at ×1.25 and BK's rooms at ×1.2, so interior ceilings are 3 m. Earlier: §12: the Combat Grid and the kit check tested at 45°; streets at 45° are allowed from 3 m wide. Earlier: accepted by the owner with every remark: the Governor's Palace, right-angled streets with some at 45° once the Combat Grid is checked at that angle, and the size. Earlier: first draft, from the owner's fifteen remarks after playing the dressed C0)

Depends On:
[11 Vertical Slice](../../../11%20Vertical%20Slice.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §2, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md), [VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md), [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md)

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
| High Street | 18 m | 4 m pavement, 10 m carriageway (two carriages pass with a third standing), 4 m pavement | setts; asphalt waits for a texture |
| Crown Avenue | 24 m | 3 m, 6 m, a 6 m mall of trees and benches, 6 m, 3 m | setts; the mall in cobbles |
| The Parade (C2's Ridge Street) | 12 m | 3 m pavement, 6 m carriageway, and a 3 m promenade along the Ridge's retaining wall, whose parapet is its balustrade | setts |
| Byelaw streets: Terrace Street with Cheap Street, Market Street, Dock Road, Mercers Row, Bell Street | 12 m | 3 + 6 + 3 m | setts |
| North Road | 10 m | carriageway only, to the North Gate | setts |
| Lanes: Ridge Lane, Stair Lane, Loom Lane | 6 m | shared, no pavement | cobbles |
| Garden Walk | 4 m | a path under the Ridge's wall | cobbles |
| Passages | 3 m | through a row, open to the sky; the kit has no arch to pass under | cobbles |
| Soot Stair | 8 m | flights and landings between blind walls | cobbles |
| Tanners Slant | 8 m | at 45° under a `KitFrame` (§12) | setts |

Kerbs and lamps follow §4. A passage is 3 m wide rather than 2.5 m, to keep the 1 m grid. Like every alley, it runs at right angles (§12).

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
  - Ridge Alley (670–673 × 702–726) down to the High Street.

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

