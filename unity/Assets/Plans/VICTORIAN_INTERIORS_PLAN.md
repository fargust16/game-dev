# Victorian Interiors — Rebuild Plan

Version: 0.2

Status: Draft — the owner's decision of 2026-11-07, formalised by the agent; the owner's answers in §9; VI-1 done, VI-2 … VI-5 wait for the owner

Created: 2026-11-07

Last Updated: 2026-11-07 (VI-1: the kit families built and measured; the windows are opaque, so daylight comes through lit panes; the owner's answers in §9)

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
- **Stair.** One flight rises about 3.54 m, a storey, in about 19 steps of 0.19 × 0.35 m, a slope of 28°. It is checked against the slice's character controller in VI-3.
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

**Washing by wealth.** A poor household has no bathroom: the privy stands in the court, outside the interior, and indoors there is only a basin and a bucket. A middling house has a WC. A rich house has a full bathroom. The owner confirmed this (§9).

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
- The curtains are drawn open by day and closed by night, from the clock. They do not move while the player watches; they change at the tick of dusk or dawn. Authoring defaults.
- The pack's window panes are opaque (§2), so no sun can shine through them. Each window instead carries a **lit pane**: a quad of our own unlit material just inside the glass, whose colour follows the sky at the hour. It is our own material, never a change to a vendor file.

**Daylight** (changed in VI-1 from a directional sun, which the opaque windows would block):

- Each window carries one spot light of its own, inside the pane, aimed down into the room. Its colour and strength follow the hour from the game clock, so daylight falls in a patch on the floor under each window and fades at dusk.
- The spot casts no shadow; at most the room's main window may, inside the light budget below.
- Ambient light comes from the sky at the hour.
- Through a window the player sees the sky and the light, not the street (owner, §9).
- The hour comes from `WorldClock`, as for `S0_LightingController` outside. No second clock is made.

**Lamps and candles:**

| Wealth | Light pieces |
|---|---|
| Rich, middling | a chandelier (`Lamp_1`) in the main rooms; wall lamps (`Lamp_2`) in halls, studies and bedrooms |
| Poor | candles (`Candle_1…4`) on tables and mantels |

- Each lamp or candle carries one warm point light of its own. It replaces the present hidden light under each ceiling.
- Lamps are lit from dusk to dawn by the clock. Once the slice has resident schedules (S5c), lamps are lit only where someone is at home and awake.
- A fireplace burns with the pack's fire and a flickering light. It burns in the morning and the evening, and the kitchen hearth burns all day. Authoring defaults.

**Why this matters.** Darkness adds to Stealth skill ([10.3](../../../10.3%20Stealth%20and%20Detection.md)), so an unlit house at night is a different place to break into from a lit one. Light is information, not decoration.

**Budget.**

- Point lights cast no shadows.
- At most eight real-time lights stand in one room, the sun included. A room that asks for more lights the extra pieces with emission only.
- The figure is an authoring default, to be checked in VI-2 against the frame time.

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
| Soot & Bell | taproom 21 × 12 with the bar, a fireplace, tables with chairs, pictures; kitchen 9 × 9 with the hearth; scullery; a stair to the let rooms on the first floor (owner, §9) |
| Harrow & Tincture | shop 12 × 9 with counters, glazed cupboards and bottle shelves; laboratory 12 × 9 (themed); store room 3 × 9 |
| Faction's house | hall 15 × 12 with the rostrum, pews and a fireplace; office 9 × 9 with a desk, bookcase and armchairs; archive 9 × 9 with bookcases full of books |
| Wardens' station | front desk room 12 × 9; office 9 × 9 with a desk and a clock; guard room 9 × 9 with a hearth, table and chairs; corridor and three cells (themed) |
| Missing person's room | the landing and one poor Room 9 × 9: bed, hearth, table, chair, washstand, the cache |
| Cellar | three vaults (themed, §9) |
| Old chapel | nave 12 × 21 and vestry (themed walls, §9), with the pack's candles, books and carpets |

Rebuilding these scenes needs the owner's request each time, and the owner has given it now: the scenes are the owner's (AGENTS, *Slice assets*). `VeilmouthInteriors` is rebuilt for the pack, and its run replaces only its own root.

## 8. Iterations

Each iteration ends with tests, renders in `Logs/SliceSurvey/`, a commit, and a word to the owner. Each starts only on the owner's authorisation.

| Step | Work | Done when |
|---|---|---|
| **VI-1 Kit** | Wrap the pack as `ViRoom` and `ViFurniture`: colliders, layers, 1 m placement, back-anchored footprints, pivots corrected. Measure what §3 lists. One sample room of each finish, rendered. | the kit check passes on every wrapper; §3's measurements are recorded here. **Done 2026-11-07:** 74 wrappers, three sample rooms and a gallery of 298 blocks with no kit fault (`VictorianInteriorsSamples`, `VictorianKitTests`) |
| **VI-2 The seven, ground floors and light** | `VeilmouthInteriors` on the 3 m module with `ViRoom`; §7's rooms dressed from `ViFurniture` and §6; fireplaces, curtains, lamps and candles; daylight through each window's lit pane and spot, from the clock (§5). | the seven build with no kit fault; the portal, people and S1 tests pass; renders at noon and at night |
| **VI-3 Stairs and floors** | The assembler learns storeys: a floor at each 3.5 m plus the ceiling, a stair well, the stair and banister, ceilings that are the next floor, the camera kept under each storey. The Combat Grid reads the storey the fight is on. The Soot & Bell's let rooms go upstairs (owner, §9). | a character walks up and down every stair; the Combat Grid builds on an upper floor; tests |
| **VI-4 Dwellings** | `DwellingPlanner` gains §4's room types and the washing by wealth; rich houses get their storeys from the baked house; furnishing lists move to `ViFurniture`; the Stash keeps its rooms and chances. | the sample homes plan and build with no kit fault, each wealth and kind; the PlayMode walk into each kind and back; renders |
| **VI-5 Play test** | The owner walks the seven and a home of each kind. | the owner's remarks, recorded as in city plan §15 |

**Risks:**

- **Stairs and the Combat Grid.** The stair is steep: 4.69 m over 6.35 m, if all of its height is the rise. The grid has so far read only one level. VI-3 carries this risk, and a fight on a stair may need its own rule.
- **Lights and frame time.** These are measured in VI-2 against §5's budget.
- **Room size.** The 3 m module must not bring back the cramped feeling the owner reported on 2026-11-06. §3's table keeps the floor area, and VI-5 checks it.

## 9. The owner's answers (2026-11-07)

The owner accepted all five recommendations and authorised VI-1. These are now decisions:

1. **Stone rooms.** The cellar's vaults, the cells and the chapel's nave keep stone as themed exceptions. The old stone kitchens and the laboratory take the pack's walls.
2. **Middling houses.** No upper floor in the slice. Only rich houses get their storeys.
3. **Washing by wealth.** A rich house has a bathroom, a middling house a WC, and a poor household a basin and a bucket, with the privy in the court.
4. **Upper floors of the seven.** The Soot & Bell's let rooms go up a stair. The faction's house stays on one floor, and Harrow sleeps in the back of the shop as now.
5. **The view through windows.** A window shows only the sky and the light in the slice.
