# Victorian Interiors — Rebuild Plan

Version: 0.14

Status: Draft — the owner's decision of 2026-11-07, formalised by the agent; the owner's answers in §9; VI-1 … VI-7 done, VI-6 having built [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) for the Dwellings and VI-7 the seven by the same rules; VI-8 (play test) held, its eight remarks and both follow-ups fixed (§8)

Created: 2026-11-07

Last Updated: 2026-11-08 (VI-8 second follow-up in §8: the balustrade assembled as the pack's own demo scene assembles it, its ends capped by newels, the stair narrowed and its rails capped, the stair lab scene, the missing room's blind door removed, the Soot & Bell's back door made an exit to its yard, every shut leaf seated in its doorway as the pack's demo seats it. The earlier note is in §10)

Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md)

Related: [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §5.2, §8, §14, §16; [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](VERTICAL_SLICE_IMPLEMENTATION_PLAN.md); [SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md); [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md) §6

---

## 1. The decision (owner, 2026-11-07)

Every interior of the slice is built from **Victorian Interiors** (`Assets/Victorian Interiors/Victorian Interiors/`). This covers the seven interiors of the city plan and the generated Dwelling scene. The pack supplies the floors, walls, ceilings, doors, windows, stairs, furniture, pictures and small things.

- **Everything is replaced.** Everything now standing in the interiors is replaced with the pack's pieces. This includes BK_AlchemistHouse's architecture and furniture and the Tripo bed.
- **Themed exceptions.** A prop from another pack stays only when the room's purpose needs it and this pack has nothing like it (§6).
- **New things the houses gain:**
  - a bathroom with a toilet;
  - plants, clocks, and bookcases full of books;
  - desks, chairs at every table, and carpets;
  - a fireplace wherever a stove stood;
  - curtains on every window;
  - light from lamps, candles and the sun.
- **Rich houses.** A rich house has two or three floors, as many as its building shows outside.
- **Room types.** Rooms follow how houses of the 18th–20th centuries were laid out (§4).

BK_AlchemistHouse's place as the base for interiors (owner, 2026-11-05) ends here. Its lab sets, bottles and jars may stay only as §6's themed props.

## 2. The pack, measured

From the survey of 2026-11-07 (`SliceAssetSurvey.RunVictorianInteriors`; `Logs/SliceSurvey/vinterior.tsv` and the sheets `vinterior_1…5.png`). All sizes are **measured**, in metres, as width × height × depth. The materials are already URP Lit.

| Family | Pieces | Measured |
|---|---|---|
| Walls | `Wall_1` … `Wall_6`, `Wall_8`, each with `Corner_Inner` and `Corner_Outer`; `Wall_7`, a half wall | 3 × 3.5, 0.22–0.34 thick; `Wall_7` 1.5 high |
| Wall finishes | paper over a panelled dado: `Wall_1` plain, `Wall_2` its doorway, `Wall_3` its window; plain dark: `Wall_5`, `Wall_8` its doorway, `Wall_6` its window; `Wall_4`: wood panelling, plain only | measured by the probe, VI-1 |
| Floor, ceiling | `Floor` (one wooden floor), `Ceiling` | 3 × 3; the ceiling 0.23 thick |
| Doors | `Door_1`, `Door_2` | 1.37 × 2.12 |
| Stairs | `Stair`, `Banister`, `Banister_End` | 2.75 wide, 4.69 high, 6.35 long; banister 2.94 long |
| Fireplace | `Mantel` with `Fire.mat` and its particle system | 2.91 × 3.51 × 0.89 |
| Bathroom | `Bathtub`, `Toilet_Seat`, `Sink`, `Bucket`, `Brush`, `Soap`, `Sponge`, the towel `Hanger`s | bath 2.33 × 1.1 × 1.29; toilet 0.74 × 1.35 × 1.07; basin 0.79 × 1.33 × 0.77 |
| Beds | `Bed` | 1.78 × 1.92 × 3.14 |
| Books | `BookShelf_1` (2.84 × 3.18), `BookShelf_2` (2.21 × 2.31), `Shelve_3`, `Shelve_4` (1.79 × 3.29), `Cupboard_2` (1.21 × 3.28); `Book_1…10`, `Books_Group_1…5` | — |
| Tables, seats | `Table_1…6` (0.78–0.96 high; `Table_4` 3.82 × 1.86, a dining table), `Table_Prefab` (a dressed desk); `Chair_1`, `Chair_2`, `Chair_Small`; `Couch_1`, `Couch_2` brown and green | chair 0.8 × 1.26 × 0.85; settee up to 2.86 long |
| Carpets | `Carpet_1`, `Carpet_2` (3.7 × 5.4), `Carpet_3`, `Carpet_4` (about 2.2–2.5) | — |
| Light | `Lamp_1` (a chandelier, hanging 1.16 below its pivot), `Lamp_2` (a wall lamp), `Candle_1…4` (emissive) | — |
| Time, walls, green | `Clock` (a long-case clock, 0.9 × 2.48), `Clock_Small`; `Mirror`, `Mirror_1…3`; `Picture_1…8`; `Plant_1…4`, `Pot_1`, `Pot_2`; `Vase_1…6` | — |
| Windows | `Curtain` | 2.91 × 3.08 × 0.26, the width of one wall module |
| Small things | plates, cups, cutlery, bottles, boxes, papers, a scroll, pen and ink, an ashtray, coat stands, `Holder` (3.29 high), `Counter`, `Phone` | — |

**Measured traps:**

- **Off-centre pivots.** These prefabs have their pivot away from their body: `Books_Group_*`, `BookShelf_3`, `Box_2_2`, `Clock_Small Variant`, `Counter`, `Cupboard_4`, `Hanger_*`, `Lamp_2`, `Plate_2`, `Plate_3`, `Shelf_4`, `Table_Prefab`. Their kit wrappers must correct this (kit standard §1).
- **Windows.** The pack has no separate window. A window is a window wall, `Wall_2` or `Wall_6`.
- **Glass.** The window walls carry no glass material, and their openings are not empty: the panes are part of the wall's opaque mesh and texture, whose albedo has no alpha (measured, VI-1). No light passes through them (§5).
- **Floors.** There is one floor material, wood, and no stone floor or stone wall.
- **The telephone.** `Phone` is not used: the city has no telephone line (authoring default).

## 3. Scale and module

The pack is built at the slice's game scale already, so it is used at **×1.0**:

- A table stands 0.93 m against a true 0.76 m, which is ×1.2, the game scale of every loose prop since city plan §17.
- A wall stands 3.5 m, against the present ceiling of 3.48 m.

**Measured:** the module is **3 m**, the module of its walls, floor and ceiling. A storey's ceiling stands at 3.5 m.

Rooms are rounded to the 3 m module from the present 4 m sizes, keeping their floor area. After the owner's play test of 2026-11-06 the rooms were widened against a cramped feeling, and they must not shrink back:

| Now (4 m module) | Becomes (3 m module) |
|---|---|
| 4 m | 3 m (a passage, a WC) or 6 m |
| 8 m | 9 m |
| 12 m | 12 m |
| 16 m | 15 m |
| 20 m | 21 m |

These sizes are authoring defaults.

**Two kit families wrap the pack,** following the kit standard:

- `ViRoom` holds the architecture: walls by finish, corners, doorways, window walls, doors, floor, ceiling, stairs, banister.
- `ViFurniture` holds everything else.

**Measured in VI-1** (`VictorianInteriorsProbe`, `Logs/SliceSurvey/vinterior_probe.txt`):

- **Both sides drawn.** Every wall has equal faces on both sides, so one wall serves two rooms.
- **Face planes.** A paper wall's dado stands 0.08 m proud of its paper; the dark and panelled walls 0.03 m. The kit stands every wall with its main face 0.08 m from the room's edge line (`ViFace`), where pictures, mirrors and curtains hang. Furniture backs stand 0.16 m from that line (`ViBack`), clear of the deepest dado.
- **Doorway.** The clear opening is 1.35 × 2.18 m.
- **Windows.** Opaque, as §2 says.
- **Stair.** One flight rises about 3.54 m, a storey, in about 19 steps of 0.19 × 0.32 m, a slope of 28.5° along its noses (measured in VI-3). Its footprint is 3 × 7 m; its top end lies on a module line.
- **Lights in the prefabs.** `Candle_1…4` carry a point light each, `Lamp_2` one, `Table_Prefab` two; `Lamp_1`, the chandelier, is emissive only; the fire is a separate particle prefab.
- **Clock.** `Clock_Small` is a mantel clock that stands, not a wall clock.
- **Bed.** Its head is at its back, against the wall.
- **Corners.** A piece three cells wide fills its module almost wholly, so it cannot stand in a corner: the side wall's dado meets it. Such pieces stand on a wall's middle module or in the room.

## 4. Rooms by period and wealth

The houses follow the layout of a British-style town house of the late 18th to the early 20th century. That is the era of the slice ([11](../../../11%20Vertical%20Slice.md), *Era and Mood*). Wealth already comes from where the house stands (city plan §16; [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3). The interior then shows that wealth: a player who breaks in reads a household from its rooms ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.2, *a Stash reads from who lives there*).

### 4.1 Floors

- **Rich house:** as many interior floors as the building has storeys outside, two or three (owner). The storeys come from the baked house's type (city plan §5.2): a villa 3, a detached house 3, a shop house 4, a tenement 3–4, a warehouse 2–3. A warehouse is a store, not a home, and keeps one floor.
- **Middling and poor houses:** one floor, as today; a middling house gains no upper floor (owner, §9).
- **A tenement flat:** one flat on one floor of its building, whatever the storeys outside. That is how tenements were let.

### 4.2 Room types

| Room | Period purpose | Wealth | Key pieces |
|---|---|---|---|
| Entrance hall with the stair | receives callers; the stair goes up from it | rich, middling | stair and banister, long-case clock, coat stand, mirror, a plant, carpet runner |
| Drawing room | the best room, for callers and evenings | rich | fireplace, settee and armchairs, a low table, pictures, mirror over the mantel, chandelier, carpet, plants, vases |
| Parlour | the family's sitting room; the drawing room of the middling house | middling | fireplace, settee, chairs, round table, small bookcase, pictures, carpet |
| Dining room | meals at a long table | rich, middling | long table, chairs all round, glazed cupboard of plates, fireplace, chandelier, pictures |
| Study or library | the master's books and papers | rich | desk, chair, bookcases full of books, a reading chair, globe or clock, wall lamps |
| Kitchen | cooking and the servants' table | all | fireplace as the hearth, table, chairs, cupboard, shelf of plates and cups, basin, bucket |
| Scullery | washing pots and linen, behind the kitchen | rich | basin, buckets, brush, soap |
| Bedroom | sleep; the best on the first floor | all | bed, washstand or chest of drawers, wardrobe or tall cupboard, chair, mirror, fireplace (rich), carpet, candle |
| Dressing room | beside the main bedroom | rich, optional | tall cupboard, mirror, chair, coat stands |
| Bathroom with WC | bath, toilet and basin, behind one door | rich | bath, toilet, basin, towel rail, mirror, soap and sponge |
| WC | a toilet and a basin in a closet | middling | toilet, basin |
| Nursery | the children's room, on the top floor | rich, three floors | small beds, small chair, table, toys from the small things |
| Servant's room | the maid's room in the attic storey | rich, three floors | narrow bed, chair, small table, candle |
| Room | the poor family's one room: they live, sleep and cook here | poor | fireplace as hearth and stove, bed, table, chairs, a washstand with a basin, bucket |
| Shop room, back room | trade at the front, the shopkeeper's room behind | shop | counter, shelves; the back room as a poor or middling Room |
| Office, store | the warehouse's clerk and its goods | warehouse | desk, chair, cupboard; crates and barrels (§6) |

**Washing by wealth.** A poor household has a washroom of 3 × 3 m with a basin, a bucket and a toilet. A middling terrace, villa or shop has a bathroom of 3 × 6 m with a bath, a toilet and a basin; a middling flat a WC of 3 × 3 with a toilet and a basin. A rich house has a full bathroom on its first floor and a second under the roof (VI-6, furnishing rules §5). The owner set the poor washroom on 2026-11-07 after VI-5, replacing §9.3 (furnishing rules §9).

**Layout by floor, rich house:**

- **Ground floor:** hall with the stair, drawing room, dining room, study, kitchen with scullery.
- **First floor:** main bedroom, a second bedroom, bathroom, dressing room.
- **Second floor** (three-storey houses only): nursery, servant's room, a box room.

A rich shop house keeps its shop and back room downstairs. Its living rooms move up the same way.

**Fireplaces:**

- The pack's `Mantel` stands wherever a stove stood (owner). This includes the kitchen and the poor Room, where the fireplace is the cooking hearth.
- In a rich house every main room has one: the drawing room, dining room, study and bedrooms.
- A fireplace stands against an inner wall. Its back faces another room, never a window.

## 5. Windows, curtains and light

**Windows.**

- Every window is a window wall, with the pack's `Curtain` hung inside it.
- The curtains are drawn open by day and closed by night, from the clock. They do not move while the player watches; they change at the tick of dusk or dawn. Authoring defaults. **In VI-2 they stay open:** the pack has one curtain, tied back, and nothing to draw. Closing them waits for a drawn curtain (`SLICE_MODELS_WANTED.md`).
- The pack's window panes are opaque (§2), so no sun can shine through them. Each window instead carries a **lit pane**: a quad of our own unlit material just inside the glass, whose colour follows the sky at the hour. It is our own material, never a change to a vendor file.

**Daylight** (changed in VI-1 from a directional sun, which the opaque windows would block):

- Each window carries one spot light of its own, inside the pane, aimed down into the room. Its colour and strength follow the hour from the game clock, so daylight falls in a patch on the floor under each window and fades at dusk.
- The spot casts no shadow; at most the room's main window may, inside the light budget below.
- Ambient light comes from the sky at the hour.
- Through a window the player sees the sky and the light, not the street (owner, §9).
- The hour comes from the slice's running clock (`GameClockDriver.Running`), as outside. No second clock is made. `InteriorLighting` holds each interior's lights and sets them from the hour.
- Hours (authoring defaults): daylight from 05:00 to 20:00, full from 07:00 to 18:00; lamps burn while less than half the day's light stands; a fireplace burns 06:00–10:00 and 17:00–23:00, a kitchen hearth 06:00–22:00. A room with no window, the cellar, keeps its candles lit.

**Lamps and candles:**

| Wealth | Light pieces |
|---|---|
| Rich, middling | wall lamps (`Lamp_2`) in every room, two or more in a large one; a chandelier (`Lamp_1`) only under a ceiling higher than a storey, the Soot & Bell's taproom alone in the slice (owner, VI-8 remark 7) |
| Poor | candles (`Candle_1…4`) on tables and mantels |

- Each lamp or candle carries one warm point light of its own. It replaces the present hidden light under each ceiling. The pack lit its lamps for baked light at 0.9; real-time, each burns at 2.5 at least and reaches 6–9 m (authoring defaults). The chandelier has no light of its own and carries one.
- Lamps are lit from dusk to dawn by the clock. Once the slice has resident schedules (S5c), lamps are lit only where someone is at home and awake.
- A fireplace burns with Vefects' small fire (asset map: *Fire*) and a flickering light, lit and put out together: a lit hearth shows its flames, a cold one sheds no light at all (owner, VI-8 remark 6). It burns in the morning and the evening, and the kitchen hearth burns all day. Authoring defaults.

**Why this matters.** Darkness adds to Stealth skill ([10.3](../../../10.3%20Stealth%20and%20Detection.md)), so an unlit house at night is a different place to break into from a lit one. Light is information, not decoration.

**Budget.**

- Point lights cast no shadows.
- At most eight real-time lights stand in one room, the sun included. A room that asks for more lights the extra pieces with emission only.
- The renderer is **Forward+** (VI-8). Under Forward a mesh took at most four extra lights, chosen per object by the view, so the hearth's light fell on the floor from one angle and not from another (remark 6). Forward+ lights every pixel from every light in range.
- The figure is an authoring default. VI-2 keeps it: window spots burn by day and lamps by night, never together, so the most lights at once in a room is seven, in the Soot & Bell's taproom at night. The frame time itself was not measured in batch; VI-5 checks it in play.

## 6. Themed props kept from other packs

These pieces stay only because a room's purpose needs them and Victorian Interiors has nothing like them:

| Room | Kept | From |
|---|---|---|
| Soot & Bell taproom | the bar counter and its back cabinet; barrels | the owner's Tripo models; Multistory Dungeons 2 |
| Harrow's laboratory and shop | cauldron, alembic, lab bench, jars and bottles on shelves | BK_AlchemistHouse lab sets, Multistory Dungeons 2 *Books And Alchemy* |
| Wardens' lock-up | the cell bars and cell door; a weapons table | the owner's Tripo models; BK |
| Faction's hall, old chapel | pews, rostrum, candle stands | the present kit |
| Cellar, store rooms, warehouses | barrels, crates, sacks, firewood, baskets | Multistory Dungeons 2, Low Poly Medieval Props |
| Cellar vaults, the cells, the chapel's nave | stone walls and stone floors (owner, §9) | BK's stone, or Low Poly Dungeons Lite |
| The cellar | the magic circle | our own (21 §4) |

Every other piece that stands in the seven interiors or the Dwelling kit today goes.

## 7. The seven interiors

The room programmes stay those of city plan §8, rebuilt on the 3 m module and dressed with the pack. Their people keep their posts, and their portals and door ids do not change, so the city, the named people and every test keep working. Upper floors are added only where §4 or §9 says so.

| Interior | Rebuilt as |
|---|---|
| Soot & Bell | taproom 21 × 12, 7 m high, to the pub plan of the furnishing rules §6; kitchen 6 × 12 with the hearth; scullery 3 × 12; a stair hall 12 × 12 and the stair to the landing, a back landing and two let rooms on the first floor (owner, §9; VI-3, VI-7) |
| Harrow & Tincture | shop 12 × 9 with the counter across it, shop shelves behind, glazed cupboards and bottle shelves; laboratory 12 × 9 (themed); store room 3 × 9 |
| Faction's house | hall 21 × 12, symmetric about the street door, with the rostrum, pews and two fireplaces; office 9 × 9 with a desk, bookcase and armchairs; archive 12 × 9 with bookcases full of books |
| Wardens' station | front desk room 12 × 9; office 9 × 9 with a desk and a clock; guard room 9 × 9 with a hearth, table and chairs; corridor and three cells (themed) |
| Missing person's room | the landing and one poor Room 9 × 9: bed, hearth, table, chair, washstand, the cache |
| Cellar | three vaults (themed, §9) |
| Old chapel | nave 15 × 21 and vestry (themed walls, §9), with candle stands |

Rebuilding these scenes needs the owner's request each time, and the owner has given it now: the scenes are the owner's (AGENTS, *Slice assets*). `VeilmouthInteriors` is rebuilt for the pack, and its run replaces only its own root.

## 8. Iterations

Each iteration ends with tests, renders in `Logs/SliceSurvey/`, a commit, and a word to the owner. Each starts only on the owner's authorisation.

| Step | Work | Done when |
|---|---|---|
| **VI-1 Kit** | Wrap the pack as `ViRoom` and `ViFurniture`: colliders, layers, 1 m placement, back-anchored footprints, pivots corrected. Measure what §3 lists. One sample room of each finish, rendered. | the kit check passes on every wrapper; §3's measurements are recorded here. **Done 2026-11-07:** 74 wrappers, three sample rooms and a gallery of 298 blocks with no kit fault (`VictorianInteriorsSamples`, `VictorianKitTests`) |
| **VI-2 The seven, ground floors and light** | `VeilmouthInteriors` on the 3 m module with `ViRoom`; §7's rooms dressed from `ViFurniture` and §6; fireplaces, curtains, lamps and candles; daylight through each window's lit pane and spot, from the clock (§5). | the seven build with no kit fault; the portal, people and S1 tests pass; renders at noon and at night. **Done 2026-11-07:** the seven with 55–207 blocks each and no kit fault; 24 EditMode and 29 PlayMode tests pass; renders `room_*_noon`, `room_*_night` and `roomeye_*` |
| **VI-3 Stairs and floors** | The assembler learns storeys: a floor at each 3.5 m plus the ceiling, a stair well, the stair and banister, ceilings that are the next floor, the camera kept under each storey. The Combat Grid reads the storey the fight is on. The Soot & Bell's let rooms go upstairs (owner, §9). | a character walks up and down every stair; the Combat Grid builds on an upper floor; tests. **Done 2026-11-07:** the Soot & Bell on two storeys with no kit fault; the player walks the stair up and down (`StairTests`); a fight on the landing reads the landing and reaches the hall down the stair (`CombatGridStoreyTests`); 25 EditMode and 30 PlayMode tests pass |
| **VI-4 Dwellings** | `DwellingPlanner` gains §4's room types and the washing by wealth; rich houses get their storeys from the baked house; furnishing lists move to `ViFurniture`; the Stash keeps its rooms and chances. | the sample homes plan and build with no kit fault, each wealth and kind; the PlayMode walk into each kind and back; renders. **Done 2026-11-07:** 14 sample homes, every kind and wealth, with no kit fault; the player walks into each kind and back (`DoorTests`) and up and down both stairs of a rich terrace, villa and shop house (`StairTests`); renders `dwelling_<kind>_<wealth>_<n>` storey by storey |
| **VI-5 Play test** | The owner walks the seven and a home of each kind. | the owner's remarks, recorded as in city plan §15. **Held 2026-11-07:** eight remarks, below; four fixed at once, four lead to [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) |
| **VI-6 Furnishing rules** | The places, rooms and buildings of [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §7 as data, a planner that fills a room from its places, and a check; the Dwellings refurnished: a washroom in every home, servants in rich houses, burglary numbers checked again. | every sample home has somewhere to sleep, cook and wash, walk-ways clear, no small thing on the floor; kit faults 0; renders. |
| **VI-7 The seven by the rules** | The seven interiors rebuilt by the same rules; the Soot & Bell to the rules' §6 plan, its taproom two storeys high, 7 m. | the counter faces the street door, the way to it clear; kit faults 0; the named people in place; renders. **Done 2026-11-08:** the seven with 55–394 blocks each and no kit fault; the named people in place; 35 EditMode and 34 PlayMode tests pass, the stair, the doors, loose props and a fight on the landing among them; renders `room_*` and `roomeye_*` at noon and at night |
| **VI-8 Play test** | The owner walks the seven and a home of each kind. | the owner's remarks, recorded as in city plan §15. |

**What VI-2 changed from §5 and §7** (authoring defaults unless the owner's):

- The faction's hall is 18 × 12, not 15 × 12, so the aisle runs under the street door between two rows of pews. The chapel's nave is 15 × 21, not 12 × 21, for the same reason.
- The Soot & Bell's let room stands downstairs, behind the passage, until VI-3 takes it up the stair. The scullery comes with VI-3.
- Harrow's laboratory has no hearth: its fire is the cauldron's.
- The cells are three stone rooms of 3 × 6 behind one bar front each, with its door.
- Stone walls are BK's brick at ×1.2, stretched to the 3.5 m storey. A wall never stands between a stone room and a finished one. BK's stone window shows a recessed opening of 0.9 × 1.6 m, found by rays; its lit pane stands on its glass.
- The street door carries the pack's door leaf, shut, with the portal before it, as before.
- A desk whose back rises above its top got a body of two boxes, one under its things and one for the back. One box over all held the things inside it, and they were flung out when woken (`SliceKitCatalogue.RaisedBack`).
- The pack's open shelves (`ViShelves`) stand empty: only bookcases are filled. Seen from far above, a floor shows its lower-detail mesh in patches; at a walker's height it does not.

**What VI-3 changed and settled** (authoring defaults unless the owner's):

- **Storeys.** A storey is 3.5 m. A room has a storey number; its floor tiles, ceiling, walls, openings and lights stand at that storey's height. A ceiling is placed under the next storey's floor, and a box of 0.26 m (3.14–3.40 m) over each module stops the camera, below the slab of the floor above.
- **Bodies under a floor.** The floor slab is 0.1 m thick, so the bodies of walls and of what reaches the ceiling stop at 3.4 m (`SliceKitVictorian.ViBodyTop`, measured) and the slab rests on them.
- **Stair and well.** The stair climbs its footprint's +Z toward a module line, where the upper floor begins. Its well is 6 × 3 m, two floor tiles left out; the stair's lowest metre runs under the floor before the well, with room to stand over it. Balustrades (`ViBanister`, solid) guard the well's sides and its low end. A floor tile or balustrade inside the stair's bounds is nested under the stair as its piece (kit standard §6).
- **The stair's slope.** A ray down onto the pack's steps often struck a nose and read a wall (64°–67°), so the grid cut the stair in two. The steps' collider is replaced by a 0.1 m slab on the noses, 28.5°, worked out from the steps' vertices (`SliceKitCatalogue.AddRamp`); the rails keep theirs. A foot stands at most a riser (0.19 m) over a tread. On the grid the stair is one cell wide: beside the rails the capsule has no room.
- **Soot & Bell.** Ground floor: taproom 21 × 12, kitchen 6 × 9 with its hearth, scullery 3 × 9, stair hall 12 × 9. First floor: the landing over the stair hall, let room 9 × 9 over the kitchen and scullery, let room 2 9 × 6 over the taproom's back.
- **Combat Grid.** It already read many levels per column. `CombatGridReader.TryFindStart` now picks the level nearest the fighter's height, so a fight on the landing starts on the landing, not in the hall under it.
- **Renders.** A storey is rendered with everything above it hidden: `room_<name>` and `room_<name>_floor2`.
- **Tables.** A walker's push slid the 2 m oak table 5.1 cm at 40 kg; the pack's tables weigh 50 and 40 kg.

**What VI-4 changed and settled** (authoring defaults unless the owner's):

- **Floors.** Every rich terrace, villa and shop house has three floors. §4.1 gives them three or four storeys outside, and an interior keeps at most three. Middling and poor homes, tenement flats and warehouses keep one (owner, §9).
- **The stair hall.** A rich house's hall is 12 × 12 with a switchback: the first flight climbs east in its middle row to the first floor, the second climbs west in the row behind to the second. Each storey's landing is a Landing room over the hall. A plan mirrored left to right turns both flights.
- **Rich house, 24 × 21 m.** Ground floor: drawing room 6 × 12, dining room 6 × 12, study 9 × 9 in panelling, kitchen 9 × 9, scullery 6 × 9. First floor: bedroom and second bedroom 6 × 12, dressing room 6 × 9, bathroom 6 × 9, guest room 12 × 9. Second floor: nursery, servant's room, box room. A rich shop house keeps its shop, back room and kitchen downstairs; drawing room, dining room, bathroom and study go to the first floor, bedrooms, servant's room and box room to the second.
- **Middling and poor homes** (as rebuilt in VI-6, furnishing rules §5). A middling terrace or villa has a hall, parlour 9 × 9, kitchen 9 × 6 with its hearth, a bathroom 3 × 6 beside it and a bedroom 12 × 6. A poor one is entered into its kitchen-living room 12 × 9, with a coal store, a bedroom, a washroom 3 × 3 and a back bedroom. A middling shop has a shop room 12 × 9, a kitchen, a bathroom and a back room; a poor shop a shop room, a back room where the household cooks and sleeps, and a washroom. A tenement flat is one room, or a kitchen and a bedroom, with a washroom (a WC if middling).
- **A 6 × 6 kitchen cannot hold a fireplace.** A fireplace stands against an inner wall with a room behind it, and three modules leave no such wall free of the doorways. Kitchens are 9 × 6.
- **Warehouse.** The store and office take the dark finish, not stone: §9 keeps stone for the cellar vaults, the cells and the chapel's nave only.
- **Furnishing.** Every room kind has its own list from `ViFurniture`, each item with the wealth it starts from and, where it stops, the wealth it ends at: candles in poor homes, wall lamps in middling and rich, a chandelier and a fireplace in every rich main room, a picture drawn from the pack's eight by the seed. Chairs stand at a table's sides facing it. A desk wider than a module takes two wall segments. A closet of one module keeps only the first row inside its door clear. Crates and barrels stay in stores and box rooms (§6). Two pieces put free never touch: a crate's body juts 1 cm past its cell, and two side by side broke the kit check.
- **Clearance.** A piece's back stands 0.16 m from the wall line, where the wall's face is. The planner's clearance is 0.155 m, because the kit check allows 0.01 m.
- **The Stash** is still BK's trunk, a themed prop the pack lacks (§6), in the first of the bedroom, study, room, back room, dressing room, office, box room, store and kitchen that the home has, at 50 / 60 / 70 % by wealth, as before.
- **Light.** Every window has a curtain, a lit pane and its spot; every hearth burns; the scene's lights follow the clock as in the seven (§5). The Dwelling scene has no sun of its own.
- **Renders.** A home is rendered storey by storey with everything above hidden: `dwelling_<kind>_<wealth>_<n>`, `_floor2`, `_floor3`.

**What VI-7 changed and settled** (authoring defaults unless the owner's):

- **The Soot & Bell's taproom** follows the pub plan of the furnishing rules §6. The street door is in the middle of the front, and the bar (5 m) stands opposite it on the same axis, x 10.5, with the back-bar cabinet behind it, half a metre off the axis because a 4 m piece cannot centre on the 1 m grid, and Nell between them. The way from the door to the bar is 5 m wide and empty, edged by two low partitions of the pack's banister. West of it is the saloon: a carpet, a sofa and two armchairs round a low table before the hearth, and two snug tables with armchairs under the window. East of it is the public bar: two tables with stools on the bare floor, four chairs facing the stage, and the stage itself, 3 × 6 m and 0.4 m high, four layers of the pack's floor tile, at the east wall on the room's axis. It stands 1 m off the wall, whose body its tiles would meet, and no table stands within 2 m of its front. The pack has no fixed bench and no piano (rules §8). The kitchen and stair-hall doors stand at the back wall's two ends.
- **Its height.** The taproom's upper storey is one well, so no floor or ceiling lies between, and its ceiling is at 7 m (owner, rules §9). A row of five high windows lights it, and three chandeliers hang at 6.8 m, their lights reaching 12.5 m. A storey that is all well gets no room marker of its own.
- **Its back range is 12 m deep**, not 9, so the second let room (9 × 6) stands over the stair hall. Upstairs the landing keeps the well's south and west sides. The well's west balustrade runs to the module line, where a wall's body would meet it, so the landing's west strip opens without a wall into a back landing of 3 × 6 with a chair and a small table (`Opening.None`). The let rooms have a bed with a nightstand, a wash stand, a dresser or glazed cupboard and a small table with a chair.
- **Harrow & Tincture** greets with two counter sets across the shop before the street door, the shop shelves centred behind them, Harrow between, and the laboratory door at the back wall's end beside the counter.
- **The faction's hall is 21 m wide**, not 18, so that it is symmetric about the street door's axis: the 3 m aisle, three rows of pews either side, the rostrum, candle stands, two hearths, two chandeliers and the two doors at the back. The archive behind is 12 × 9, its bookcases never in a corner.
- **The Wardens' front desk** stands across the street door's axis with Coyle behind it; the waiting chairs face it.
- The missing person's wash stand is the basin set. The cellar and the chapel stay as built in VI-2: the chapel's aisle and pews were already symmetric.

**VI-5, the route** (2026-11-07). The owner plays `Veilmouth`, starting on Bell Street before the Soot & Bell, and walks:

- **The seven** through their street doors: the Soot & Bell (both storeys and the stair), Harrow & Tincture, the faction's house, the Wardens' lock-up with its cells, the missing person's room, the cellar under the Soot Stair, the old chapel. Once by day and once by night, for the light from the clock (§5).
- **A home of each kind found in C2.** Veilmouth has 77 dwelling doors, in six kinds; one door of each, as world X, Z in metres, from its key: a rich villa on the Ridge (40.8, −156; three floors), a middling terrace (12, −197.8), a middling shop (36.8, −280), a poor shop (143.8, −244), a poor tenement flat (64.2, −280), a poor warehouse (44.2, −312).
- **The kinds C2 does not have.** No rich terrace, rich shop house, poor terrace or middling villa stands in the city. Each is seen in the `Dwelling` scene played alone, with the `Dwelling` object's *Preview Context* set to, for example, `dwelling.0.0|Veilmouth|0|0|Shop|Rich`; the kind is one of Terrace, Tenement, Shop, Villa, Warehouse, and the wealth one of Poor, Middling, Rich. Its street door then leads nowhere, since no such door stands in the city.

What to look for, besides the owner's own eye: whether rooms feel cramped on the 3 m module, the stair and a fight on it, the light by day and night and the frame time in the rich houses, and whether a home reads its household's wealth.

**VI-5, the owner's remarks** (2026-11-07):

| # | Remark | Done |
|---|---|---|
| 1 | The balustrades do not meet the stair's well, and the stair meets the floor badly | The balustrade stood in the middle of the metre beside the well, half a metre off its edge. It now lies along its 3 m with its side flush with its footprint's front, on the well's edge, lengthened to 3 m (it measured 2.94) so two in a row close. The stair stands 0.12 m further on, so its top step meets the upper floor at the module line and its newel stands on the landing (`SliceKitVictorian`). |
| 2 | The publican could be asked from the floor above | A person answers only on the player's level (1.5 m up or down) and in sight at chest height, through no wall, floor or ceiling; a counter does not block (`TipSourcePoint`). A door is within reach only on its own level (`Portal`). |
| 3 | The walls' look changes with the camera's angle, in almost every interior | The pack's walls and furniture carry LOD groups whose coarser meshes differ in their panels. A wrapper keeps the finest level only (`SliceKitCatalogue.KeepFinestLevel`). |
| 4 | A basin in a bedroom and no toilet anywhere; a bathroom with bath, basin, toilet, wall cabinets | A wash place in every home: [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) §3–§5, the poor's washroom a decision of §9. |
| 5 | A plant on the floor; furnishing must follow rules per room, walls and corners first, passages clear, symmetry; the bar centred opposite the door, a stage, a higher ceiling with chandeliers, from a real pub plan | The rules §2–§4; the pub's plan §6; the taproom's height a decision of §9. |
| 6 | Light shows round the door inside | The sky showed through the gap round a shut door and the seams between wall pieces. Inside an interior the camera now draws black behind the walls (`InteriorLighting`). |
| 7 | A hall of barrels and crates, a chair in the middle of a room; every home must have a bedroom, a kitchen and a bath | The rules §5: a home always has somewhere to sleep, to cook and to wash. |
| 8 | The same faults everywhere: build from purpose to places to rooms to buildings; small props on surfaces; shops with a counter across the width; servants in rich houses who run for the Wardens | The rules §1–§7; servants §5, their numbers a decision of §9. |

**VI-8, the route** (2026-11-08). As VI-5's route, from Bell Street before the Soot & Bell, by day and by night, with these to look at besides the owner's own eye:

- **The Soot & Bell.** Whether the bar reads from the street door, the way to it stays clear, the taproom feels high and the saloon and public bar read as two sides; the stage without its piano; the stair, the landing and the back landing, both let rooms; that Nell answers only from the floor (VI-5 remark 2).
- **Harrow & Tincture, the Wardens' lock-up.** Whether each greets across the room from its counter or desk.
- **The faction's house.** Whether the widened hall reads as symmetric, the aisle clear to the rostrum.
- **A home of each kind** (the six doors of VI-5's route, and the Dwelling scene for the kinds C2 lacks): somewhere to sleep, cook and wash; walk-ways clear; small things on surfaces; servants' rooms in the rich villa.

The remarks are recorded here as in VI-5.

**VI-8, the owner's remarks** (2026-11-08) and what was done:

| # | Remark | Done |
|---|---|---|
| 1 | Between the taproom's storeys a band of wood panelling seems missing; fix it in every like place | The pack's dark walls (`Wall_5`, `Wall_6`, `Wall_8`) carry a frieze of light red damask under the cornice, which reads as a gap where no panel stands. Our own copy of each material (`ViFrieze`) recolours the damask in the panels' wood, its pattern left faintly; every room built of those walls takes it, the Dwellings too. The pack's files are untouched. |
| 2 | What are the rails and chairs by the bar for? | The rails were the pack's banister standing in for the low partition between saloon and public bar (§6's pub plan); the chairs faced the stage. They read as a lost stair rail, so the partition is out and wanted in `SLICE_MODELS_WANTED.md`. |
| 3 | Every wall seems to lose a panel just under the ceiling | The same frieze (remark 1). |
| 4 | A hole in the stair's balustrade | Two balustrades each stand on their own side of the well, so at a corner they touched at one point and left a notch a rail wide. The pack's newel post (`Banister_End`) now stands in every such corner, found from the balustrades' bodies (`DwellingAssembler.NewelSpots`), in the seven and the Dwellings. |
| 5 | The stage is a few floor tiles; a missing piece is never pieced together from others: it goes to the wanted list, and a primitive with a texture may stand in | The stage is one box of our own with the pack's parquet (`ViStage_3x6`). The rule is in the furnishing rules, G10. |
| 6 | The hearth's light falls on the floor from one angle and not another; a fire is either really burning or out | Forward+ (§5). The hearth now burns with Vefects' fire, its light lit and put out with its flames, and the fire's own particle lights are off. |
| 7 | The great chandelier only in high halls; wall lamps in ordinary rooms | Chandeliers only in the taproom; Harrow's shop, the faction's hall and the Dwellings' rooms take wall lamps (§5, rules G9). |
| 8 | A globe in the middle of a passage; no small thing on a floor | The globe stands in the study's corner. A doorway check now runs on every build: two metres before each doorway on both sides its middle metre must be clear; it found and cleared a chair in the chapel's vestry and the scullery's basket and bucket. |

The detail asked for at the end of the remarks — props that would make the rooms lived in — is listed in `SLICE_MODELS_WANTED.md`, *Interior detail*.

**VI-8, the follow-up** (owner, 2026-11-08) and what was done:

| # | Remark | Done |
|---|---|---|
| 1 | The fire rises too high; sink its foot into the logs; wider than tall | Vefects' fire is squashed to ×1.35 across and ×0.55 up of its scale 0.85 (`DwellingAssembler.FireShape`), and its foot sinks 0.06 m into the logs (`FireSink`). Its own distortion and particle lights are off; the hearth's flickering light stays. Authoring defaults, to be judged in play. |
| 2 | The balustrade's lower beam is offset: the rails hang in the air, a void at the well | The balustrade now stands by its own design (below). Its balusters stand on the nosing at the well's edge, and the fascia hangs down the well's side, as the model was made. |
| 3 | NPC names show through walls | A name shows only when nothing solid (ground, walls, floors, furniture, doors) stands between the camera and the head (`HudView.InSight`). |
| 4 | The corner newel stands lower than the rails meet; no gaps | The newel is the pack's `Banister_End` at 0.34 × 1.34 m, its top 0.23 m above the handrails, which now die into its sides. |

**Why the rails failed three times.** The balustrade was placed by its bounding box: its lowest point on the floor and its side flush with the well. But the model's lowest point is the fascia, which hangs 0.32 m below its floor line to cover the well's edge. So the rail stood 0.32 m too high and back to front: the fascia lay on the landing as a "lower beam", the balusters stood beyond the edge, and every newel, measured to the floor, came out below the handrails. The kit check reads colliders, not seams, and whole-room renders were too far away to show the joints, so each fix treated a symptom. Now the rail keeps its own floor line (`KeepY`) and is turned so its nosing meets the well; its sections were measured from the FBX (`tools/assets/fbx_sections.py`); and the Soot & Bell's render pass takes close-ups of each joint (`detail_sb_newel_*`, `detail_sb_rail_*`, `detail_sb_fascia_below`). A rule against placing a vendor model by its bounding box alone is in `AGENTS.md`.

**VI-8, the second follow-up** (owner, 2026-11-08) and what was done. The owner asked to settle the rails for good, in a scene of their own if need be:

| # | Remark | Done |
|---|---|---|
| 1 | The rails' ends are open; close them with a piece of the pack, or say there is none | The pack's piece is `Banister_End`, the newel post. A newel now stands at every free end of a gallery rail that does not die into a wall, and at both ends of each stair rail: the foot post 0.018 m inside the rail's foot, the head post 0.054 m past its head on the upper floor, as in the pack's demo (`SliceKitCatalogue.AddRailPosts`). |
| 2 | Press the upper rails onto the beam at the well | The balustrade's balusters now stand on the well's edge, its nosing 0.219 m over the well and its fascia down the well's side (`ViRailOverhang`, **measured** from the demo). |
| 3 | The corner rails do not meet the corner post; build the stair in a scene of its own | The newel is `Banister_End` at its own size, 0.257 m square and 1.34 m tall, on the floor, centred on the handrails' line 0.0445 m in from the edge (`DwellingAssembler.RailLine`, **measured**), so both handrails die into its sides as in the demo. The stair is narrowed ×0.92 to 2.534 m so its handrail clears the gallery's fascia. `StairLab` builds a rich villa by the game's own assembler in `Assets/Scenes/Slice/Lab/StairLab.unity` and renders every newel, rail seat and stair post close; `StairLab.ProbeVendor` measures and renders the demo. |
| 4 | Extra doors leading nowhere | Only two leaves led nowhere, both in the seven; the Dwellings have their street door alone. The missing room's neighbour's door on the landing is now a wall. The Soot & Bell's back door is now an exit to its yard, at the owner's word: inside it moved to the stair hall's east wall (21, 21), as the yard lies east of the house; outside the house is baked again with the pack's own shop door in its yard side (`VictorianHouses` `YardDoors`), and `VeilmouthYardDoor.Place` sets the paired portal (`soot-and-bell.yard`) in it, touching nothing else in the city scene. |
| 5 | A strip of sky round the shut leaves (seen at the new yard door) | Measured by rays on our doorway (`StairLab.MeasureDoorways`) and in the pack's demo (`StairLab.ProbeVendorDoor`): the opening runs 0.789–2.162 m along the block, 0.0245 m short of the block's middle, and its head is at 2.179 m. Our leaf stood on the floor at the block's middle, leaving 2.5 cm of sky at one jamb and 6 cm under the head. The demo raises the leaf 0.06 m onto the doorway's own threshold, its top meeting the head, and centres it on the opening; every Door_1 and Door_2 now stands so (`ViLeafSeat`, **measured**), within 1 mm of each jamb. |

**Why the rails kept failing.** Every placement was derived from the models' bounds and our own reading of them. The pack's demo scene (`Victorian Interiors/Test Scene.unity`), where the author assembled the stair, the balustrade and the newel, was never opened. It is now the reference for every joint, and `AGENTS.md`'s rule on vendor models asks for the pack's own demo first.

**Risks:**

- **A fight on a stair.** The stair is one grid cell wide. Whether a fight there needs a rule of its own (a narrow place, Surrounded impossible) is left to VI-5 and to the Combat Grid's own documents.
- **Lights and frame time.** The count stays within §5's budget (§5); the frame time is checked in VI-5.
- **Room size.** The 3 m module must not bring back the cramped feeling the owner reported on 2026-11-06. §3's table keeps the floor area, and VI-5 checks it.

## 9. The owner's answers (2026-11-07)

The owner accepted all five recommendations and authorised VI-1. These are now decisions:

1. **Stone rooms.** The cellar's vaults, the cells and the chapel's nave keep stone as themed exceptions. The old stone kitchens and the laboratory take the pack's walls.
2. **Middling houses.** No upper floor in the slice. Only rich houses get their storeys.
3. **Washing by wealth.** A rich house has a bathroom, a middling house a WC, and a poor household a basin and a bucket, with the privy in the court. *Replaced after VI-5 (owner, 2026-11-07): a poor home has a washroom with a basin, a bucket and a toilet (furnishing rules §9).*
4. **Upper floors of the seven.** The Soot & Bell's let rooms go up a stair. The faction's house stays on one floor, and Harrow sleeps in the back of the shop as now.
5. **The view through windows.** A window shows only the sky and the light in the slice.

## 10. Update history

- 2026-11-08 (v0.14): every shut leaf seated as the pack's demo seats it (§8).
- 2026-11-08 (v0.13): the Soot & Bell's back door made an exit to its yard (§8).
- 2026-11-08 (v0.12): VI-8's second follow-up: the rails by the pack's demo scene, newels at their ends, the stair lab, the missing room's blind door (§8).
- 2026-11-08 (v0.11): VI-8's follow-up: the fire, the balustrade's seat, the newel, names behind walls, and why the rails failed (§8).
- 2026-11-08 (v0.10): VI-8, the owner's eight remarks and what was done (§8); chandeliers only under a high ceiling and Forward+ (§5); the real fire.
- 2026-11-08: VI-8 authorised; its route recorded in §8.
- 2026-11-08 (v0.9): VI-7, the seven furnished by the rules; the Soot & Bell to the pub plan with its 7 m taproom, its back range 12 m deep and a back landing; the faction's hall 21 m wide.
- 2026-11-07 (v0.8): VI-6, the Dwellings furnished by `INTERIOR_FURNISHING_RULES.md` §7: places, rooms, the planner and its check; a washroom or bathroom in every home, servants in rich houses, the burglary numbers checked again (10.3 §6).
- 2026-11-07 (v0.7): the owner's four decisions on the furnishing rules: VI-6 … VI-8 added to §8, the poor washroom replaces §9.3.
- 2026-11-07 (v0.6): VI-5, the owner's eight remarks; the balustrades, talking through floors, the walls' LOD and the sky round doors fixed; furnishing by rules drafted in `INTERIOR_FURNISHING_RULES.md`.
- 2026-11-07 (v0.5): VI-4, the Dwellings rebuilt from Victorian Interiors, with §4's rooms, washing by wealth, and rich houses on three floors joined by a switchback stair.
- 2026-11-07 (v0.4): VI-3, storeys, the stair and its well, the Soot & Bell's let rooms upstairs, the Combat Grid on the storey of the fight.
- 2026-11-07 (v0.3): VI-2, the seven rebuilt on the 3 m module, with light from the clock. Earlier: VI-1, the kit families built and measured; the windows are opaque, so daylight comes through lit panes; the owner's answers in §9.
