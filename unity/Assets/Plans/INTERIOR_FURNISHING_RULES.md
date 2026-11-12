# Interior Furnishing Rules

Version: 0.15

Status: Draft — the owner's remarks after play test VI-5 (2026-11-07), formalised by the agent; the owner's four decisions in §9; §7 built for the Dwellings in VI-6 (§7.1) and the seven in VI-7 (§7.2); the owner's Tripo pieces in §10 (2026-11-09, 2026-11-10); fireplaces and radiators in §10.1 (owner, 2026-11-10); every piece's size checked in §11 (owner's walk, 2026-11-10); the CC0 pieces in §12 (2026-11-11); seat sizes in §13 (owner, 2026-11-12)

Created: 2026-11-07

Last Updated: 2026-11-12 (§13, the owner kept the scaled kit seats after the trial; only the pew and the furniture kit's chair get new models. The earlier note is in the update history)

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

**G5. Small things lie on surfaces.** A small prop stands on a surface of its place: a table, shelf, mantel, counter, nightstand, desk, washstand, bath's rim. Only what really lies on a floor may lie on one: books and papers in a study, a basket or bucket in a kitchen or scullery, crates, barrels and sacks in a store, a log basket and fire irons by a hearth. **Barrels and crates are never filler** (owner, 2026-11-09): they stand in a store, a warehouse, a cellar store, the port, and at a bar's end, never in a hall, a stair hall, a yard or a living room for their own sake; in a home a barrel is rare.

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
| **Hearth** | fireplace (`Mantel`) | an armchair each side facing in, or a settee facing it; a rug before it; a mirror or picture above | centred on an inner wall (G8) | clock, vases, candles on the mantel; a log basket beside it, and fire irons in a middling room and up (Tripo, §10) |
| **Sit** | settee (`Couch_*`) | a low table before it; armchairs each side facing in; a lamp; a plant in the corner beside | settee against a wall, the group facing the room's middle or the hearth | cups, a book, a vase on the low table |
| **Eat** | table (`Table_*`) | chairs on its sides facing it (G4); a sideboard or glazed cupboard on a wall; a chandelier over it (rich) | centred in the room in a dining room; in a kitchen centred or against a wall when the room is small | plates, cups, cutlery, candles on the table; plates in the glazed cupboard |
| **Cook** | range: the fireplace as hearth (`Mantel`, owner) | a work table; a dresser (shelves with plates); a basin and bucket; a cupboard | the hearth on an inner wall; the dresser and cupboard along the other walls | pots and plates on the dresser and hearth; a bucket by the basin |
| **Work** | desk (`Table_*` desk) | its chair, facing it; bookcases in a symmetric row; a reading armchair with a lamp in a corner | desk against a wall beside a window, the light from the side; bookcases on the wall facing it | ink, pen, papers, scrolls, books, a lamp on the desk |
| **Store** | shelves (`Shelve_*`) | crates, barrels, sacks, baskets (§6 of the interiors plan) | along the walls and in the corners; an aisle of 2 m at least down the middle | what is stored, on the shelves |
| **Sell** | counter (`Counter`) | shelves on the wall behind, the counter centred on them; a display table or chairs on the customers' side, against the walls | across almost the room's whole width, a third of the way in, with a 1 m flap at one end; the customers' half clear | goods on the shelves and the counter; a bell, a ledger |
| **Serve** (bar) | bar counter (themed, §6 of the interiors plan) | back-bar shelves against the back wall, the counter centred on them; barrels at one end | opposite the entrance; 1–2 m for the keeper between counter and shelves | bottles and glasses on the back bar; cups on the counter |
| **Drink** | table with chairs or stools | benches fixed along the walls (*missing*), booths along the side walls | along the walls and in the window bays; a few in the open, never on the path from the door to the bar | cups, bottles, a candle on each table |
| **Perform** | a stage: a platform of 0.4 m (*missing*: a primitive box, G10) | a piano (Tripo, §10); chairs facing it | at the end wall, on the room's axis; its front clear of tables by 2 m | a candle row at the front |
| **Pass** (hall, landing, corridor) | — | a coat stand by the door; a hall table with a mirror over it; a long-case clock; a runner rug | against the walls only; nothing on the middle line (G1); a plant only in a corner | a lamp and a tray on the hall table |

## 4. Rooms

A room is a set of places. *Required* places always come; *optional* ones come by wealth and size, in the order given.

| Room | Required | Optional, in order |
|---|---|---|
| Hall | Pass | a second Pass piece (clock, plant in a corner) |
| Kitchen | Cook, Eat | Store (a pantry shelf) |
| Kitchen-living room (poor) | Cook, Eat, Hearth-side seat | a bed recess when the home has no bedroom |
| Parlour | Hearth, Sit | Work (a small bookcase), a display cabinet |
| Drawing room (rich) | Hearth, Sit | a piano, books, a display cabinet, a gramophone, a floor vase (Tripo, §10), plants in the corners |
| Dining room | Eat | Hearth, a sideboard |
| Study or library | Work | Hearth, Sit (reading chair) |
| Bedroom | Sleep | Dress, a chest of drawers (middling and up), a folding screen (rich), Hearth (rich), a chair by the window |
| Dressing room | Dress | a chest of drawers, a second wardrobe, a folding screen, a coat stand |
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

Victorian Interiors has no nightstand, coal scuttle, bench or settle, piano, stage, wall cabinet, towel rail or kitchen range. By the interiors plan's §6, a themed prop from another pack may stand in for a piece a room's purpose needs. The owner's Tripo models (§10) now give the nightstand, a log basket with fire irons for the coal scuttle, and the piano. For the rest, until the owner chooses:

- a poor home's nightstand stays the pack's smallest cabinet, `Shelve_5`, scaled to a 0.80 m top (§11), with a candle on it (VI-6: no `Table_*` fits a bedside cell); a middling or rich one is Tripo's;
- a towel rail is the pack's standing hanger (`Hanger_2`), and a wall cabinet the wall shelf (`Shelf_4`);
- the range stays the fireplace (owner);
- a bench, a stage platform and a low pub partition are listed in [SLICE_MODELS_WANTED.md](SLICE_MODELS_WANTED.md); until then the stage is a primitive box with the pack's parquet (G10), and the rest wait.

## 9. The owner's decisions (2026-11-07)

The owner accepted all four recommendations. These are now decisions:

1. **Washing for the poor.** A poor home has a small washroom of 3 × 3 m with a basin, a bucket and a toilet. It replaces the earlier basin-and-bucket answer (interiors plan §9.3).
2. **The taproom's height.** The Soot & Bell's taproom rises through two storeys, 7 m, with no floor over it, and two or three chandeliers hang in the space. The second let room moves over the stair hall.
3. **Servants.** Every rich house has one servant at home in its working hours, 06:00–22:00, and half of them a second; at night they sleep in the servant's room. Authoring defaults. 10.3's burglary numbers are checked again with `tools/balance/burglary_sim.py` in VI-6.
4. **The order of the work.** VI-6 builds the places, rooms and planner of §7 and refurnishes the Dwellings; VI-7 rebuilds the seven by the same rules, the Soot & Bell to §6's plan; VI-8 is the owner's play test. Each step waits for the owner's word.

## 10. The owner's Tripo pieces (2026-11-09)

The owner: take every piece from TripoModels for the interior scenes, and leave them out of the city's open plan, since each holds up to about 10 000 polygons (the table lamp 14 410, measured). So every interior piece among them furnishes the seven and the Dwellings, and none stands outside (city plan §25.6). Each model is normalised to 1 on its longest side; the wrapper (`SliceKitTripo.cs`) gives it its size at game scale, ×1.2 its true size. The sizes are authoring defaults, read from the survey's sheets (`SliceAssetSurvey.RunTripo`). The models carry no light, so a lamp among them carries a warm light of our own, as the pack's lamps carry theirs (G9).

| Piece (Tripo file) | Key | True size | Where it stands |
|---|---|---|---|
| wardrobe (`antique_wardrobe`) | `TrWardrobe` | 1.9 m high | Dress, middling and rich |
| nightstand with a candle and a book (`antique_wood_nightstand`) | `TrNightstand` | 0.67 m | Sleep, middling, a pair; the Soot & Bell's let rooms |
| nightstand with a table lamp (and `decorative_table_lamp`) | `TrNightstandLamp` | 0.67 m | Sleep, rich, a pair |
| chest of drawers with a brush and a box (`antique_wooden_dresser`) | `TrDresser` | 0.88 m | a bedroom's or dressing room's drawers, middling and up, a small mirror over it; the let room |
| writing desk with a lamp, a parchment, ink and a pen (`antique_wooden_desk`, `aged_parchment`) | `TrDesk` | 0.95 m | Work, rich, a wall clock over it; the faction's and the lock-up's offices |
| cheval glass (`ornate_mirror`) | `TrMirror` | 1.55 m | Dress, rich, beside the wardrobe |
| folding screen (`ornate_wooden_screen`) | `TrScreen` | 1.6 m | a rich bedroom, a dressing room |
| upright piano (`ornate_piano`) | `TrPiano` | 1.2 m | a rich drawing room, a chair at its keys; the Soot & Bell's stage |
| glazed display cabinet (`furniture`) | `TrDisplay` | 1.65 m | Display, rich |
| wing chair (`red_velvet_armchair`) | `TrArmchair` | 1.05 m | the hearth's pair, rich; the Soot & Bell's saloon and back landing, the faction's office |
| coat stand (`wooden_coat_rack`) | `TrCoatRack` | 1.5 m | Coats, rich |
| floor vase (`antique_vase`) | `TrVase` | 0.75 m | a rich hall or drawing room, in a corner (G6) |
| log basket (`log_holder`) | `TrLogHolder` | 0.55 m | beside every hearth and kitchen range; the cellar's store |
| fire irons (`fireplace_tools_set`) | `TrFireTools` | 0.67 m | beside a hearth of a middling room and up |
| gramophone on a pedestal table (`wooden_round_table`, `vintage_gramophone`) | `TrGramophone` | 0.67 m table | a rich drawing room |
| sewing table (the pack's `Table_2`, `sewing_machine`) | `TrSewing` | — | a poor or middling parlour, kitchen or room, with its chair |
| wall clock (`ornate_grandfather_clock`, a regulator without a plinth) | `TrWallClock` | 0.9 m | over a rich desk |
| two-arm sconce (`brass_wall_sconce`) | `TrSconce` | 0.45 m across | a rich room's wall lamps |
| bracket lantern (`antique_wall_lantern`) | `TrLantern` | 0.5 m | the hall's and landing's wall lamps, poor and middling |
| chandelier (`ornate_chandelier`) | `TrChandelier` | 1.2 m | the Soot & Bell's taproom, the one room higher than a storey (G9) |
| nightstand cabinet with a door and a drawer (`antique_wooden_cabinet_3d_model_Clone1`), a candle and a book or a table lamp | `TrCabinet`, `TrCabinetLamp` | 0.67 m | a second kind of nightstand, middling and rich: the bed's pair is one kind or the other, both alike (G3), chosen by the seed (owner, 2026-11-10) |
| ornate floor vase, blue and gold (`ornate_vase`) | `TrVaseOrnate` | 0.75 m | a rich hall, drawing room or landing, in a corner; one of the two floor vases, chosen by the seed |
| bust of a woman on its small base (`classical_bust`), on the pedestal table | `TrBust` | 0.45 m bust | a rich drawing room, study or hall, against a wall (owner: a woman's bust in rich houses) |
| round wall barometer (`antique_barometer`) | `TrBarometer` | 0.38 m across | hung in a middling or rich hall, on the wall nearest the street door (owner: by the entrance) |
| iron umbrella stand (`decorative_vase_3d_model_Clone1`) | `TrUmbrellas` | 0.6 m | a middling or rich hall, in the corner nearest the street door (owner: by the entrance) |
| magnifying glass, laid flat (`magnifying_glass`) | on `TrDesk` | 0.2 m | on the rich writing desk, by the parchment (owner: on desks or in cabinets) |
| inkstand with two wells and a pen (`ornate_ink_desk_3d_model_Clone1`) | on `TrDesk` | 0.25 m | on the rich writing desk, in the place of the pack's ink and pen (owner: rich interiors, on desks) |
| gas radiator (`ornate_radiator`) | `TrRadiator` | 0.96 m high, 1.43 m wide (§11.1) | §10.1: every heated room without a fireplace, against a plain wall |

The bed, the bar counter and its cabinet, the cell bars and door and the Notice Board were the owner's Tripo pieces already (asset map §4). Of the owner's models of 2026-11-10, the street lamp and the bronze statue stand in the city, by the owner's word (city plan §30), and the six people are the asset map's §1. The other models are not furnishing: the townsman, the woman and the dog are people and beasts, the angels, the lion and the birdbath are the garden ornaments of the city plan §23, the coins and firearms are items, the rock is the iron ore, and the hanging sign hangs outside, over the doors of Harrow & Tincture and the Soot & Bell (city plan §25.6, [99 Open Questions](../../../99%20Open%20Questions.md) VS-SIGN (b)).

**Resized after the owner's fifth play test (2026-11-09).** The owner found the piano very small and asked that every Tripo piece fit the world. Measured beside the player (1.8 m) in one lineup, the tall pieces were raised to stand over a man's head as their true ones do. Game sizes: wardrobe 2.4 m, cheval glass 2.0 m, folding screen 2.0 m, display cabinet 2.1 m, coat stand 2.05 m, and wing chair 1.4 m, which is Victorian Interiors' own armchair. The piano is sized by its keys, which stand at 0.41 of its height: 0.82 m, a real upright's 0.72 m at game scale. That makes it 2.0 m over its carved top and two cells deep. The table's true sizes above are those before the play test. A wrapper whose footprint changes when it is rebuilt takes the old one's place, keeping its GUID, so the scenes follow it ([VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §26.1).

**The Office** (owner, fifth play test). A civic building's, the palace's and the clock tower's doors open into an Office, a Dwelling that is no home. It has a reception at the door with a counter where a clerk receives callers, two offices with desks, an archive of shelves and boxes, and a WC; the clock tower has only its keeper's room. The rules of §1–§5 hold in it as in a rich home, with Work as its main purpose and no Sleep or Cook (city plan §26.3).

The seven took them in place (`VeilmouthInteriors.EnrichWithTripo`), so what S2 wrote into their scenes stays. The barrels in the Soot & Bell's stair hall and Harrow's store room went out; a piano, log baskets and fire irons came in; and the pieces above replaced the pack's where the table says.

## 10.1 Fireplaces and radiators (owner, 2026-11-10)

The owner: a house has only one or two fireplaces, on the ground floor; every other room has a radiator.

- **One or two, the range among them.** The kitchen's range is the pack's fireplace (`ViFireplace`), so a home shows it as a fireplace. So the agent reads the rule as at most two fireplaces in a home, the range counted: a home that cooks keeps its range and one hearth more, in its first room by the rules' order (§4: the parlour, the drawing room). A home with no kitchen range may have two hearths.
- **Ground floor only.** No hearth stands on a storey above the ground. A rich house's bedrooms, its study and dining room, which §4 gave a hearth, have a radiator instead.
- **A radiator in every other room.** Each room that people live, work or wash in, and that has no fireplace, has one gas radiator (`TrRadiator`): the hall, parlour, drawing, dining and dressing rooms, study, bedroom, nursery, servant's room, bathroom, back room, shop, office and reception. A kitchen, a scullery, a store, a pantry, a box room, an archive, a landing, a WC and a washroom have none. It stands against a plain wall and never blocks a walk-way (G1). Until the owner's play test of 2026-11-11 it stood under a window where one was free, 0.64 m high, 4 cm under the lowest sill; at ×1.5 it stands 0.96 m high, above every sill (§11.1). The agent applies it to every wealth, the poor included, by the owner's words; that a poor home has gas heat is the owner's world, not a history.
- **The seven** already meet the first two parts: each has at most two fireplaces, all on the ground floor (the Soot & Bell's saloon and kitchen, the faction house's two hearths, the lock-up's guard room, the missing room's one). Radiators join their other rooms where a wall is free.

These counts are authoring defaults.

**As built, 2026-11-10** (`DwellingFurnishing`, `VeilmouthInteriors`):

- **The radiator stands 5 cm off the wall**, on its brackets. Set flush, it ran 3.7 cm into a window's frame in every Dwelling, so 36 faults stood in the kit check.
- **The fourteen sample Dwellings** show no kit fault. Their hearths are at most two and none stands above the ground floor; `DwellingFurnishing.Audit` checks this, and `SampleHomesKeepTheFurnishingRules` runs the check.
- **Radiators in the Dwellings.** A heated room with no hearth gets a radiator where a plain wall is free; at 0.96 m it no longer counts as low enough for a window (§11.1), so it goes under one only when no wall is free. The audit does not yet count the rooms left without one.
- **The seven interiors** have eight radiators, each against the first free stretch of plain wall in its room (under its first free window until §11.1):
  - the Soot & Bell's stair hall and its two let rooms;
  - the faction house's office;
  - the lock-up's front desk and office;
  - Harrow's shop and laboratory.

  A second enrichment finds a room's radiator by its body and adds no second one.

## 11. Sizes (owner's walk, 2026-11-10)

The owner: the radiator and the table are deliberately small; every interior piece should have a sensible size.

**Measured.** `VeilmouthWalkAudit.Furniture` reads every wrapper of Victorian Interiors, Tripo and BK as built (`Logs/SliceSurvey/walk_furniture.txt`):

- its size;
- its working top, the highest level that holds at least 20 % of its upward-facing area, read in 2 cm steps;
- a window's sill.

Furniture stands at game scale, ×1.2 its true size (kit standard §1). A true size is therefore the game size divided by 1.2.

**The usual true sizes** the agent checks against (authoring defaults, from common furniture practice):

| Piece | True height |
|---|---|
| A table's or a desk's top | 0.72–0.78 m |
| A chair's seat | 0.42–0.46 m |
| A bed's mattress | 0.5–0.6 m |
| A nightstand | 0.6–0.7 m |
| A standing lamp | 1.5–1.8 m |

A seat is the exception the seated bodies make: its game size is §13's, its top 0.42 m over the floor, and since 2026-11-12 the kit's seats are scaled to it instead of ×1.2, on the owner's trial, which the owner kept (§13).

**Changed** (game sizes; `SliceKitTripo`, `SliceKitVictorian`):

| Piece | Key | Measured | True | Now | True now |
|---|---|---|---|---|---|
| writing desk (Tripo) | `TrDesk` | top 0.76 m | 0.63 m | top 0.90 m, 1.77 × 0.94 m, scaled whole | 0.75 m |
| gas radiator (Tripo) | `TrRadiator` | 0.69 m wide, 0.58 m high | 0.58 × 0.48 m | 0.95 m wide, 0.64 m high, 0.20 m deep | 0.79 × 0.53 m |
| kitchen table | `ViKitchenTable` | top 0.78 m | 0.65 m | top 0.90 m, its 2.68 × 1.28 m kept | 0.75 m |
| partners' desk | `ViDesk` | 3.20 × 1.21 m, top 1.04 m | 0.87 m | 3.20 × 1.21 m kept, top 0.90 m (height scaled by 0.91/1.04; its gallery 1.44 → 1.26 m) | 0.75 m |
| poor nightstand (the pack's smallest cabinet, §8) | `ViNightstand`, `ViNightstandPoor` | 0.98 × 0.50 m, top 1.16 m | 0.97 m | 0.67 × 0.35 m, top 0.80 m, scaled whole, as the Tripo nightstand | 0.67 m |
| candle stand | `ViStandLamp` | 3.29 m high | 2.74 m | 2.05 m high, 0.30 m across | 1.71 m |

How each was changed:

- **The radiator** is as wide as a window's middle and stays 4 cm under the lowest sill: the paper window's 0.68 m (measured). It may stand under any window (§10.1).
- **The kitchen table and the partners' desk** are lowered or raised alone, since their footprints already fit their rooms. Scaled whole, the partners' desk was 1.06 m deep and reached 0.05 m into Harrow's lab bench, so only its height changes.
- **The Tripo desk and the nightstand** are scaled whole, so their drawers and doors keep their shape.

**How the sizes are applied** (`SliceKitCatalogue`):

- **A turned model.** A size given per axis is read on the world's axes, but it is applied on the model's own axes. Tripo's models are turned −90° about X, so the radiator's height took its depth's ratio, and it stood 0.72 m. Each ratio now goes to the model's axis that lies along its world axis.
- **The cell bars** keep the size the owner has seen, 2.41 m high and 0.35 m deep (measured). That size is now asked for outright, so the corrected sizing does not change them.
- **A resized wrapper keeps its GUID.** A wrapper whose footprint changes is moved to its new name, so the seven's and the Dwellings' pieces follow it: the Tripo desk 2x1 → 2x2, the nightstand 1x2 → 1x1 and the candle stand 1x3 → 1x2. The partners' desk keeps its 4x1.
- **Rebuild before measuring.** A batch that also builds the city rebuilds these wrappers first. The city's measuring pass writes any wrapper it does not find, and it would write one under its new name beside the old one.

## 11.1 The owner's play test of 2026-11-11

The owner: radiators ×1.5; a bigger writing desk, a large work desk.

| Piece | Key | Before | Now (game size) | True now |
|---|---|---|---|---|
| gas radiator (Tripo) | `TrRadiator` | 0.95 m wide, 0.64 m high | 1.43 m wide, 0.96 m high, ×1.5, drawn out on width and height | 1.19 × 0.80 m |
| writing desk (Tripo) | `TrDesk` | 1.77 × 0.94 m, top 0.90 m | 2.3 × 1.15 m, top 0.90 m kept: drawn out on width and depth, no longer scaled whole | 1.92 × 0.96 m, top 0.75 m |

- **The radiator leaves the windows.** At 0.96 m it stands higher than every sill, so it goes against a plain wall: the first free stretch of the room in the seven, wherever a plain wall is free in the Dwellings. A true column radiator under a window is about 0.6 m high, and a tall one against a wall about 0.8–1.0 m, so the owner's size reads as a wall radiator.
- **The desk** keeps its top at the tables' 0.90 m, so the chair still fits it. Its drawers and gallery widen with it, about ×1.3.
- **The trap seen again.** A measuring pass ran after the specs changed, before the rebuild, and wrote the two wrappers under their new sizes' names (`Kit_Tripo_Desk_3x2`, `Kit_Tripo_Radiator_2x1`). The seven kept the old ones (`_2x2`, `_1x1`). The enrichment now swaps a stale wrapper's instances for the current one where they stand (`VeilmouthInteriors.Restale`), takes a stale radiator away for `Radiators` to set anew, and deletes the stale wrapper once nothing holds it (`DropStale`).
- **As built** (2026-11-11): the seven keep their eight radiators, now `_2x1` against plain walls: three in the Soot & Bell, two at Harrow & Tincture, one in the Faction House and two in the Wardens' Lockup. The Faction House's and the Lockup's offices each have one `_3x2` desk. No scene holds a `_2x2` desk or a `_1x1` radiator, and `Kit_Tripo_Radiator_1x1` is deleted. Every interior has 0 kit faults and 0 doorway faults, and the 14 sample Dwellings have 0 kit faults.
- **Seen in the renders** (`Logs/SliceSurvey/roomeye_*.png`): the offices' desks are clearly larger than the chairs before them, and the radiators stand at the walls' foot. The owner has not yet seen them in the game.

**Within the usual sizes** (unchanged, game and true):

- Tables: `ViTable` 0.96 m (0.80), `ViTableB` and `ViHallTable` 0.88 m (0.73), `ViTableRound` and `ViDiningTable` 0.92 m (0.77), `TrSewing` 0.88 m (0.73).
- Seats and beds: `ViChair`'s seat 0.50 m (0.42), `ViSofa`'s 0.54 m (0.45), `TrArmchair`'s 0.56 m (0.47), `ViBed`'s mattress 0.68 m (0.57).
- Nightstands and counters: the Tripo nightstands' tops 0.80 m (0.67), `ViCounter` 1.02 m (0.85).
- Low seats: `ViArmchair` and `ViSettee` seat low, 0.40 and 0.42 m (0.33 and 0.35), as Victorian low chairs and settees do. They are kept.

These sizes are authoring defaults, and the measurements are the audit's.

## 12. The CC0 pieces (2026-11-11)

The owner added four CC0 libraries to Blender and gave the agent the choice and the placing ([SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) §4b). Indoors they fill gaps from §8 and the wanted list; every place below is an authoring default.

**On pieces already standing.** These are part of the wrapper, so every interior and Dwelling that holds the piece gains them with no new placing:

- `ViKitchenTable` keeps a loaf, an apple, a cast-iron pot and a tea kettle on its top, in place of the Victorian Interiors pot.
- `ViNightstand` holds a pocket watch, and `ViDesk` a pair of round spectacles, their glass left out.
- `Chest`, at a bed's foot, is now Poly Haven's iron-bound travelling trunk in place of the medieval chest.

**Placed by the enrichment** (`VeilmouthInteriors.SootAndBellCc0`, `CellarCc0`), into the saved scenes without a rebuild:

- **The Soot & Bell.** The round table by the saloon's sofa becomes `ViTableChess`, the same table with a game of chess laid out. A dartboard hangs on the public bar's back wall, 1.45 m to its middle, on the first segment with a wall behind it and no picture.
- **The Soot Stair cellar.** A small heap of sacks in the east vault, clear of the stair's doorway, and a step ladder in that vault's far corner. Four plates of melted candles round the circle, about 4.5 m out. Damp stains run down the brick from the street above: on each side vault's far wall and on the circle vault's entry wall. On a rebuild the enrichment takes these pieces out and lays them again, so a changed place leaves no old one.

**A hung piece needs a wall.** `HangOnWall` tries the given 3 m segments in turn and keeps the first with a wall within 0.4 m behind it; with none, nothing is hung and it is reported. One hung over a doorway or a gap would stand in the air.

## 13. Seats (owner, 2026-11-12)

The owner asked which benches, chairs and armchairs the player and the NPCs can sit on, and thought new models easier than refitting the kit's. The agent measured the seated bodies and proposed sizes; the owner accepted them by asking that the walker be fitted to them.

**The seated body**, measured in play ([ANIMATION_PIPELINE_PLAN.md](ANIMATION_PIPELINE_PLAN.md) §13, `ActivityPlayTests.MeasureTheSeatedBody`). In the sitting clip, its soles on the floor:

- the hip joint stands 0.54–0.61 m up, and the lowest skin under it 0.35–0.45 m up;
- the back is 0.14–0.21 m behind the hip joint, upright, and the knees 0.40–0.46 m before it;
- with its hands on its thighs a man is 0.58–0.80 m wide and a woman 0.68–0.96 m, her skirts the widest.

**Sizes, in game metres:**

| Part | Size |
|---|---|
| The seat's top over the floor | 0.42 m (0.40–0.45) |
| From the seat's front edge to the back's face | 0.45 m |
| The back's lean | at most 10° |
| Seat to a person | 1.0 m |
| Arms, if any | at most 0.18 m over the seat, at least 0.75 m apart inside |
| Clear floor before the front edge | 0.6 m |
| A pew's row to the next | 0.95–1.0 m |

No high stool: the sitting clip keeps the feet on the floor. A seat's places are read from its own mesh (`SliceActivities.Seats`), so a new model needs no hand-placed points: a place to every metre of seat, at most four, the sitter's hip joint 0.225 m behind the front edge.

**Game scale.** These are game sizes, set by the bodies. Furniture stands at ×1.2 its true size (§11, kit standard §1), so a real chair's 0.42–0.46 m (§11's table) stands 0.50–0.55 m in the game, as the kit's Victorian chair (0.50 m) and street bench (0.54 m) did before the owner's trial below (measured), a body sinking 8–12 cm into them. §11 sets a table's top at 0.90 m in the game: 0.48 m over a 0.42 m seat, where a real table stands about 0.3 m over its chair. A new seat is drawn at true scale, as every wanted model is, and wrapped as a seat, so that `FitSeat` brings its top to 0.42 m like the kit's below. Whether the tables people sit at come down with the seats is the owner's ([99](../../../99%20Open%20Questions.md) VS-SEAT).

**The kit's seats** (the owner's trial, 2026-11-12, kept by the owner the same day): to see whether new models were needed, the kit's seats, and no other furniture, leave the ×1.2 scale and are scaled so that their seat's top stands 0.42 m (`Spec.SeatTop`, `SliceKitCatalogue.FitSeat`). Removing ×1.2 alone would not have fitted them: the street bench would stand 0.45 m, the dark chair 0.47, the furniture kit's chair 0.65 and the pew 0.71 (both fitted by their boxes), and the Victorian pieces and the Tripo armchair carry no ×1.2. So:

- a seat for one person, a chair or an armchair, is scaled whole;
- a seat for several, the street bench or the pew, is scaled in height only, so that it keeps its length, its depth and its cells;
- a seat already within 3 cm of 0.42 m keeps its size: the Victorian armchair, settee and sofas.

Measured after it (`Logs/SliceSurvey/seats.txt`): the street bench, the pew, the furniture kit's chair, the dark chair, the Victorian chair, the Tripo armchair and the settee 0.42 m; the Victorian armchair and sofas 0.40 m. Before it the street bench stood 0.54 m, the Victorian chair 0.50, the dark chair and the Tripo armchair 0.56, the furniture kit's chair 0.78 and the pew 0.85. Not sat on, and not scaled: the dark stool, 0.70 m, and the footstool, 0.34 m. The walker sits on seats 0.36–0.62 m over the ground under its place, and a seated walker's body ignores its seat's colliders, so that it no longer shoves a loose chair aside (`TownsfolkWalker.IgnoreSeat`). The tables stay at §11's 0.90 m. Having seen them, the owner kept the scaled seats, and only the pew and the furniture kit's chair, the Dungeon pack's two blocks, which look squat at that size, get new models (`SLICE_MODELS_WANTED.md`), wrapped and fitted the same way ([99](../../../99%20Open%20Questions.md) VS-SEAT); the renders before and after are `ActivityPlayTests.RenderTheKitSeats` and `RenderTheSeatsInPlace`, diagnostics run by name.

## Sources

- CAMRA, *Pub Heritage* records of historic pub interiors, e.g. [pubheritage.camra.org.uk/pubs/141](https://pubheritage.camra.org.uk/pubs/141), [/pubs/240](https://pubheritage.camra.org.uk/pubs/240), [/pubs/582](https://pubheritage.camra.org.uk/pubs/582).
- [Music hall](https://en.wikipedia.org/wiki/Music_hall) and [Song and supper room](https://en.wikipedia.org/wiki/Song_and_supper_room), Wikipedia; [Victorian London — precursors of the music hall](https://www.victorianlondon.org/entertainment2/precursors.htm).
- [1900s house plans](https://www.1900s.org.uk/1900s-house-plans.htm) and [the parlour](https://www.1900s.org.uk/1900s-parlour.htm); [The Victorian British Parlour](https://grimsdyke.com/?p=4516).

## Update history

- 2026-11-12 (v0.15): §13, the owner kept the scaled kit seats; the pew and the furniture kit's chair get new models; a new seat is fitted by `FitSeat`.
- 2026-11-12 (v0.14): §13, the owner's trial: the kit's seats leave the ×1.2 scale and are scaled so their top stands 0.42 m; the seated walker no longer shoves its seat.
- 2026-11-12 (v0.13): §13, the owner's seat sizes in game metres, measured on the seated bodies; their game scale open as VS-SEAT; §11's table notes the exception.
- 2026-11-11 (v0.12): §12, the CC0 pieces from the owner's four Blender libraries, on the Victorian tables and placed in the Soot & Bell and the cellar; `HangOnWall`.
- 2026-11-11 (v0.11): §11.1, the owner's play test: the radiator ×1.5 against a plain wall, the Tripo desk larger at the same top; §10, §10.1 changed to match.
- 2026-11-10 (v0.10): §11, the owner's walk: every piece measured as built against its usual true size, six set to theirs; §10's radiator row and §10.1's sill changed to match.
- 2026-11-10 (v0.9, later): §10.1 as built: the radiator 5 cm off the wall, the sample Dwellings and the seven's eight radiators.
- 2026-11-10 (v0.9): §10, the owner's ten Tripo pieces of 2026-11-10; §10.1, the owner's rule on fireplaces and radiators.
- 2026-11-09 (v0.8): §10, the owner's fifth play test: the Tripo pieces resized against the player, the piano by its keys; the Office, a civic building's Dwelling.
- 2026-11-09 (v0.7): §10, the hanging sign hangs outside, over two shop doors, by the owner's VS-SIGN (b).
- 2026-11-09 (v0.6): the owner's word: G5's barrels and crates; §10, the owner's Tripo pieces and their places; §3, §4 and §8 changed to match.
- 2026-11-08 (v0.5): after play test VI-8: G9 (chandeliers), G10 (no pieced-together pieces), G11 (doorway check), G12 (hearths); §7.2 and §8 as rebuilt.
- 2026-11-08 (v0.4): §7.2, as built in VI-7 for the seven.
- 2026-11-07 (v0.3): §7.1, as built in VI-6 for the Dwellings; §8's nightstand and towel rail as measured.
- 2026-11-07 (v0.2): the owner's four decisions in §9.
- 2026-11-07 (v0.1): first draft: the furnishing grammar from building purpose to small props, from the owner's remarks 4, 5, 7 and 8 of VI-5.
