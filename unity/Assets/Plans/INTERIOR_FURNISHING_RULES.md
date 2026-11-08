# Interior Furnishing Rules

Version: 0.5

Status: Draft — the owner's remarks after play test VI-5 (2026-11-07), formalised by the agent; the owner's four decisions in §9; §7 built for the Dwellings in VI-6 (§7.1), the seven wait for VI-7

Created: 2026-11-07

Last Updated: 2026-11-08 (after play test VI-8: G9's chandelier only under a ceiling higher than a storey; G10, a missing piece is never pieced together; G11, the doorway check; G12, a hearth burns or is out. The earlier note is in the update history)

Authority: [11 Vertical Slice](../../../11%20Vertical%20Slice.md), [15 Procedural Building Generation](../../../15%20Procedural%20Building%20Generation.md), [VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md), [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md)

Related: [10.3 Stealth and Detection](../../../10.3%20Stealth%20and%20Detection.md) §6, [19](../../../19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3, [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §8

---

## 1. Why

The owner's play test of VI-4 (VI-5, remarks 4, 5, 7, 8) found the same fault in almost every room: pieces put wherever a cell was free, a plant in the middle of the floor, a chair far from its table, a hall full of barrels, a basin in a bedroom and no toilet anywhere. A room read as a pile of pieces, not as a place where someone lives or works.

The owner asked for rules, built from the smallest part to the largest, so that every interior, built by hand or generated from a seed, is furnished by its purpose:

1. **A piece** belongs to a place and stands where it stands in life.
2. **A place** is a use of part of a room: sleeping, dressing, washing, cooking, eating, sitting, working, storing, selling, serving, performing, passing.
3. **A room** is made of its places, by its purpose.
4. **A home or a building** is made of its rooms, by its purpose and wealth.

The seed chooses between what the rules allow (which wall the bed takes, which picture, which of two layouts, a mirror image). It never breaks a rule. These rules hold for every building of the slice: dwellings, the seven, and whatever comes later.

## 2. General rules

These hold in every room. A later rule never overrides an earlier one.

**G1. Walk-ways stay clear.** A path one cell wide (1 m) at least runs from every doorway to every other doorway and to the use cell of every place: the side of a bed, the seat of a chair, the front of a counter, a cupboard's doors. The doorway's cell and the cell before it on both sides stay empty. In a hall, landing or corridor nothing stands on the middle line.

**G2. Walls and corners first.** A large piece stands against a wall or in a corner. Only a room's centre group stands free in the middle: a dining table with its chairs, a seating group on a rug before the hearth, a bed's foot towards the room, a bar counter, a shop's display table. A piece is never put in the middle just because a cell is free.

**G3. Symmetry round an anchor.** A place with an anchor is built symmetrically about it where the room allows: the bed centred on its wall with a nightstand each side; the fireplace centred on its wall with an armchair each side; the sideboard centred on the wall opposite the dining table; the counter centred on the shelves behind it. Corners take the tall pieces in pairs: two cupboards, two plants, two lamps.

**G4. Every piece belongs to a place.** A chair stands at its table, desk or hearth and faces it. A lamp stands where something is read, eaten or worked. A rug lies under its group. A piece with no place is not placed.

**G5. Small things lie on surfaces.** A small prop stands on a surface of its place: a table, shelf, mantel, counter, nightstand, desk, washstand, bath's rim. Only what really lies on a floor may lie on one: books and papers in a study, a basket or bucket in a kitchen or scullery, crates, barrels, sacks and firewood in a store, a coal scuttle by a hearth.

**G6. Plants and stands** go in a corner, beside a window, or beside a seat, on the floor or on a small table; never in a walk-way or the middle.

**G7. Order of filling.** Purpose → places → anchors → companions → wall pieces → corners → hung pieces → surface props. A room too small for a place's anchor drops the place, never squeezes it.

**G8. Window and door walls.** A bed's head, a fireplace and a tall cupboard never stand on a window's segment. Nothing stands in a doorway's swing (its cell and the cell before it). A fireplace stands on an inner wall (VICTORIAN_INTERIORS_PLAN §4).

**G9. Light follows use.** A lamp or candle stands at each place that is used at night: by the bed, at the table, at the desk, at the hearth. A chandelier hangs over a centre group only under a ceiling higher than a storey; a room of a storey's height takes wall lamps, two or more when it is large (owner, VI-8).

**G10. A missing piece is never pieced together.** When the pack lacks a piece, it is not made up of other pieces (floor tiles for a stage, a banister for a partition). It is listed in [SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md), or the row added if missing, and the place goes without it; where the owner allows, a primitive with a texture stands in (owner, VI-8).

**G11. The doorway check.** Every build checks two metres before each doorway on both sides: its middle metre stays clear of every piece but the room's own walls, the stair and people. It reports and never moves a piece.

**G12. A hearth burns or is out.** A lit hearth shows its fire and lights the room; a cold one sheds no light. Its light and its flames are lit and put out together (owner, VI-8).

## 3. Places

Each place has an anchor, its companions, where it stands, and the small things that belong on its surfaces. Pieces are named by the pack's prefab (Victorian Interiors) unless marked; *missing* means the pack has none (§8).

| Place | Anchor | Companions | Where | Small things |
|---|---|---|---|---|
| **Sleep** | bed (`Bed`) | a nightstand each side (rich, middling) or one (poor); a rug at the foot (rich) | head centred on a wall without a window segment, not the wall of the door if a better one exists; the open sides and the foot clear (G1) | candle or lamp on a nightstand; a book |
| **Dress** | wardrobe or tall cupboard (`Cupboard_4`) | chest of drawers (`Cupboard_2`) with a mirror over it; a chair; a coat stand in a corner | wall opposite or beside the bed; tall pieces in corners (G3) | brush, small box on the chest |
| **Wash** | basin (`Sink`) with a mirror (`Mirror_*`) over it | toilet (`Toilet_Seat`); bath (`Bathtub`); a wall shelf (`Shelf_4`) or cabinet; towel hook (`Hanger*`); bucket | bath along a wall, its long side to the wall; toilet against a wall beside a corner, its front cell clear; basin centred on its wall | soap and sponge on the basin and the bath's rim; brushes on the shelf |
| **Hearth** | fireplace (`Mantel`) | an armchair each side facing in, or a settee facing it; a rug before it; a mirror or picture above | centred on an inner wall (G8) | clock, vases, candles on the mantel; a coal scuttle (*missing*) |
| **Sit** | settee (`Couch_*`) | a low table before it; armchairs each side facing in; a lamp; a plant in the corner beside | settee against a wall, the group facing the room's middle or the hearth | cups, a book, a vase on the low table |
| **Eat** | table (`Table_*`) | chairs on its sides facing it (G4); a sideboard or glazed cupboard on a wall; a chandelier over it (rich) | centred in the room in a dining room; in a kitchen centred or against a wall when the room is small | plates, cups, cutlery, candles on the table; plates in the glazed cupboard |
| **Cook** | range: the fireplace as hearth (`Mantel`, owner) | a work table; a dresser (shelves with plates); a basin and bucket; a cupboard | the hearth on an inner wall; the dresser and cupboard along the other walls | pots and plates on the dresser and hearth; a bucket by the basin |
| **Work** | desk (`Table_*` desk) | its chair, facing it; bookcases in a symmetric row; a reading armchair with a lamp in a corner | desk against a wall beside a window, the light from the side; bookcases on the wall facing it | ink, pen, papers, scrolls, books, a lamp on the desk |
| **Store** | shelves (`Shelve_*`) | crates, barrels, sacks, baskets (§6 of the interiors plan) | along the walls and in the corners; an aisle of 2 m at least down the middle | what is stored, on the shelves |
| **Sell** | counter (`Counter`) | shelves on the wall behind, the counter centred on them; a display table or chairs on the customers' side, against the walls | across almost the room's whole width, a third of the way in, with a 1 m flap at one end; the customers' half clear | goods on the shelves and the counter; a bell, a ledger |
| **Serve** (bar) | bar counter (themed, §6 of the interiors plan) | back-bar shelves against the back wall, the counter centred on them; barrels at one end | opposite the entrance; 1–2 m for the keeper between counter and shelves | bottles and glasses on the back bar; cups on the counter |
| **Drink** | table with chairs or stools | benches fixed along the walls (*missing*), booths along the side walls | along the walls and in the window bays; a few in the open, never on the path from the door to the bar | cups, bottles, a candle on each table |
| **Perform** | a stage: a platform of 0.4 m (*missing*: built from floor pieces) | a piano (*missing*); chairs facing it | at the end wall, on the room's axis; its front clear of tables by 2 m | a candle row at the front |
| **Pass** (hall, landing, corridor) | — | a coat stand by the door; a hall table with a mirror over it; a long-case clock; a runner rug | against the walls only; nothing on the middle line (G1); a plant only in a corner | a lamp and a tray on the hall table |

## 4. Rooms

A room is a set of places. *Required* places always come; *optional* ones come by wealth and size, in the order given.

| Room | Required | Optional, in order |
|---|---|---|
| Hall | Pass | a second Pass piece (clock, plant in a corner) |
| Kitchen | Cook, Eat | Store (a pantry shelf) |
| Kitchen-living room (poor) | Cook, Eat, Hearth-side seat | a bed recess when the home has no bedroom |
| Parlour | Hearth, Sit | Work (a small bookcase), a display cabinet |
| Drawing room (rich) | Hearth, Sit | a second Sit group, a piano (*missing*), plants in the corners |
| Dining room | Eat | Hearth, a sideboard |
| Study or library | Work | Hearth, Sit (reading chair) |
| Bedroom | Sleep | Dress, Hearth (rich), a chair by the window |
| Dressing room | Dress | a mirror, a second wardrobe |
| Bathroom | Wash with bath, toilet and basin | a wall shelf, towel hooks, a chair |
| WC | Wash with toilet and basin | a wall shelf |
| Washroom (poor) | Wash with a basin, a bucket and a tin bath or toilet | — |
| Nursery | Sleep (small beds), a table with small chairs | toys from the small things |
| Servant's room | Sleep (narrow), a chair, a small table | a candle, a box |
| Scullery, pantry, store | Store | a basin and buckets in a scullery |
| Shop room | Sell | a display table, chairs for customers |
| Back room | the keeper's Sleep or Sit | Store |
| Office | Work | Store (a cupboard) |

## 5. Homes

**A home always has somewhere to sleep, to cook, and to wash** (owner, VI-5). Where the home is small, places share a room, but none is missing. A home is entered into a hall, or into the kitchen-living room in a poor home.

| Wealth | Rooms |
|---|---|
| **Poor** | a kitchen-living room (entered from the street), a bedroom or a bed recess, a washroom; a coal store in a terrace |
| **Middling** | a hall, a parlour, a kitchen with its table, one or two bedrooms, a bathroom (bath, toilet, basin) |
| **Rich** | a hall with the stair; a drawing room, a dining room, a study or library, a kitchen, a scullery and a pantry; three bedrooms at least, the master's with a dressing room; a bathroom on each bedroom floor or two WCs; a nursery, servants' rooms |

**Servants** (owner, VI-5). A rich house may have servants at home. A servant is a resident of 10.3 §6.1: it keeps its schedule, notices with its own cone and meter, and as a witness of a break-in it runs out of the house to the nearest Warden, as 10.3 §6.2 says of every witness. One is at home 06:00–22:00 in every rich house, a second in half of them (§9).

## 6. Trade, public and work buildings

**A shop** greets the customer with its counter across almost the whole width of the shop room and its shelves full of goods behind. Only at one side does a way lead past the counter's flap to the store, a WC, and, in a poor or middling shop, the keeper's bed or chair in the back room.

**A pub** (the Soot & Bell). Victorian pubs were divided into separate rooms round one serving counter: a plain public bar with fixed benches and a hard floor, a more comfortable saloon, and small snugs, each with its own door, and the counter serving them all from a back bar of shelves (CAMRA's Pub Heritage records; see Sources). Singing was the pub's own entertainment: the *free-and-easy* in a tavern room, or a concert room, often on the first floor (see Sources). The Soot & Bell's taproom therefore takes this plan:

- The entrance is in the middle of the street front, and the counter stands opposite it, centred on the back-bar shelves against the back wall. Nothing stands on the path from the door to the counter.
- The public bar takes one side of the room: fixed benches along the walls, tables and stools, a hard floor. The saloon takes the other: a carpet, upholstered chairs, a fireplace. A low partition, not a wall, marks them.
- Snug booths stand along a side wall. A small stage with a piano stands at one end wall, on the room's axis, with chairs facing it.
- The doors to the kitchen and the stair hall stand at the ends of the back wall, beside the counter, never in front of it.
- The taproom is higher than a living room, so nobody reaches its ceiling with a jump, and two or three chandeliers hang over it (§9).

**A warehouse** keeps a 2 m aisle from its doors through the store, racks and stacks along the walls, and an office with its desk and cupboard. **Harrow's apothecary** is a shop whose back room is a laboratory: the lab bench and cauldron are its Work place. **The lock-up** is an office with its desk and a row of cells. **The faction's hall and the chapel** keep their aisle from the door to the rostrum or altar clear, with pews in symmetric rows on both sides.

## 7. How it is built

The rules become data and one planner, used by the Dwelling scene and by the seven alike:

1. **Places as data.** Each place is a small pattern of cells: its anchor, its companions placed relative to the anchor (left, right, before, in a corner), its clearances, and its surface slots for small props. A pattern can be mirrored.
2. **Rooms as data.** Each room type lists its required and optional places by wealth (§4).
3. **Homes and buildings as data.** Each lists its rooms by wealth and the doors between them (§5, §6); the layouts of VI-4 change to give every home its sleep, cook and wash.
4. **The planner** fills a room in the order of G7. It first reserves the walk-ways of G1: the doorway cells and a path between doorways. It then places each place's anchor on the wall or centre that its rules allow, trying the seed's order and keeping the symmetric choice where one fits, then the companions round it, and last the small props on the surfaces the place provides. A place that does not fit is dropped and written to the review list.
5. **A check** proves the rules for every sample: no piece on a reserved path, every chair at a table or hearth, no small prop on the floor except G5's, every home has its sleep, cook and wash, symmetry where it was asked for.
6. **The seven** are rebuilt by the same places, hand-placed where the plan of §6 decides, so a hand-built room and a generated one obey one set of rules.

### 7.1 As built for the Dwellings (VI-6)

`DwellingFurnishing.cs` holds the places and rooms as data and the planner; `DwellingPlanner.cs` keeps the layouts and the stair. Every list and number is an authoring default.

- **Walk-ways by reach, not by reserved paths.** Item 4's reserved path became a check after every piece: the room's ways in (doorways, a stair's foot and head) must reach one another, and every placed piece must keep one of its use cells in reach (a bed its foot or sides, a chair the cells beside it). A piece that breaks reach is taken back, its place tries the next spot. This keeps rooms fuller than a fixed corridor would and still never shuts a door or a place.
- **Order.** In each room: the hearth first where a room has one (a fixed feature, G8), then the required places, then the optional ones, then lamps, pictures, a chandelier and a carpet, and last the Stash.
- **Small things are sets.** A nightstand with its candle, a basin with soap and a brush, a shop's shelves of goods, a kitchen dresser of plates and pots, a counter with its box and papers: the kit wraps each as one block (`SliceKitVictorian`), so nothing small is ever placed on a floor by the planner except G5's boxes, baskets, buckets, barrels and crates.
- **What the kit's measurements changed.** A side wall's face stands 0.16 m into the room, and the toilet's body comes within 0.13 m of its cell's side, the basin's within 0.11 m, so neither fits a corner: both stand in a segment's middle. In a closet of 3 × 3 m (the poor washroom, a flat's WC) the toilet's body reaches 1.07 m from its wall, so the basin's front cell lies under the toilet's footprint while the floor between the door and the toilet's front (1.6 m) stays free; the basin is used from there. Only the palm keeps clear of both walls of a corner, so it is the corner plant. A hearth rug keeps its cells free of later pieces, and a carpet lies under a centre group or on open floor, never under a piece against a wall or on a rug. A kitchen 6 m wide could not hold the hearth with its door in the middle of the inner wall, so the narrow flat's door to the bedroom moved to the wall's end.
- **Seats.** A table moves along its room before it loses a chair, and keeps chairs on both long sides before it seats one side only.
- **The check** (`DwellingPlanner.Audit`, `DwellingTests.SampleHomesKeepTheFurnishingRules`, 84 sample homes): sleep, cook and wash in every home; ways in that reach one another; nothing in a doorway or on a stair's foot; no small thing on a forbidden floor; every seat facing a table, a desk or a hearth within two cells. Symmetry is sought by the placement score, not yet checked.
- **Servants.** A rich house's plan carries its servants, one and in half the houses two (`SecondServantChance`), and its servant's room a bed for each. Their schedule, cones and running for a Warden belong to the break-in work of 10.3 §6 and are not built yet.

### 7.2 As built for the seven (VI-7)

The seven stay hand-placed in `VeilmouthInteriors`, by §6 and the places of §3; the planner's check is not run on them. Their plans are in the interiors plan, §8 (*What VI-7 changed*).

- **The pub.** Every line of §6 holds in the Soot & Bell except the fixed benches and the piano, which the pack lacks (§8): the public bar seats on stools and chairs. The back-bar cabinet stands half a metre off the bar's axis, since a 4 m piece cannot centre on the 1 m grid.
- **The stage** is one box of our own, 0.4 m high, with the pack's parquet, by G10 (VI-8); it was four layers of floor tile in VI-7.
- **After VI-8** the partition between saloon and public bar is out (G10), the chandeliers hang only in the taproom (G9), and a newel stands where two balustrades meet at a corner.
- **Symmetry.** The faction's hall was widened to 21 m so that its rows, hearths, chandeliers and doors mirror about the street door; the chapel already did.

## 8. Pieces the pack lacks

Victorian Interiors has no nightstand, coal scuttle, bench or settle, piano, stage, wall cabinet, towel rail or kitchen range. By the interiors plan's §6, a themed prop from another pack may stand in for a piece a room's purpose needs. Until the owner chooses:

- a nightstand is the pack's smallest cabinet, `Shelve_5`, with a candle on it (VI-6: no `Table_*` fits a bedside cell);
- a towel rail is the pack's standing hanger (`Hanger_2`), and a wall cabinet the wall shelf (`Shelf_4`);
- the range stays the fireplace (owner);
- a bench, a piano, a stage platform and a low pub partition are listed in [SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md); until then the stage is a primitive box with the pack's parquet (G10), and the rest wait.

## 9. The owner's decisions (2026-11-07)

The owner accepted all four recommendations. These are now decisions:

1. **Washing for the poor.** A poor home has a small washroom of 3 × 3 m with a basin, a bucket and a toilet. It replaces the earlier basin-and-bucket answer (interiors plan §9.3).
2. **The taproom's height.** The Soot & Bell's taproom rises through two storeys, 7 m, with no floor over it, and two or three chandeliers hang in the space. The second let room moves over the stair hall.
3. **Servants.** Every rich house has one servant at home in its working hours, 06:00–22:00, and half of them a second; at night they sleep in the servant's room. Authoring defaults. 10.3's burglary numbers are checked again with `tools/balance/burglary_sim.py` in VI-6.
4. **The order of the work.** VI-6 builds the places, rooms and planner of §7 and refurnishes the Dwellings; VI-7 rebuilds the seven by the same rules, the Soot & Bell to §6's plan; VI-8 is the owner's play test. Each step waits for the owner's word.

## Sources

- CAMRA, *Pub Heritage* records of historic pub interiors, e.g. [pubheritage.camra.org.uk/pubs/141](https://pubheritage.camra.org.uk/pubs/141), [/pubs/240](https://pubheritage.camra.org.uk/pubs/240), [/pubs/582](https://pubheritage.camra.org.uk/pubs/582).
- [Music hall](https://en.wikipedia.org/wiki/Music_hall) and [Song and supper room](https://en.wikipedia.org/wiki/Song_and_supper_room), Wikipedia; [Victorian London — precursors of the music hall](https://www.victorianlondon.org/entertainment2/precursors.htm).
- [1900s house plans](https://www.1900s.org.uk/1900s-house-plans.htm) and [the parlour](https://www.1900s.org.uk/1900s-parlour.htm); [The Victorian British Parlour](https://grimsdyke.com/?p=4516).

## Update history

- 2026-11-08 (v0.5): after play test VI-8: G9 (chandeliers), G10 (no pieced-together pieces), G11 (doorway check), G12 (hearths); §7.2 and §8 as rebuilt.
- 2026-11-08 (v0.4): §7.2, as built in VI-7 for the seven.
- 2026-11-07 (v0.3): §7.1, as built in VI-6 for the Dwellings; §8's nightstand and towel rail as measured.
- 2026-11-07 (v0.2): the owner's four decisions in §9.
- 2026-11-07 (v0.1): first draft: the furnishing grammar from building purpose to small props, from the owner's remarks 4, 5, 7 and 8 of VI-5.
