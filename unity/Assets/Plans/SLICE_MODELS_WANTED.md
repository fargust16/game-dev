# Slice Models Wanted

Version: 0.6

Status: Draft

Last Updated: 2026-11-08 (the city: statues covered by the owner's three angels and two lions at the palace. Earlier: the city: a statue in a square is covered for now by the owner's angel in Wardley Garden; fountains to come from the owner. Earlier: *Interior detail*: the props that would fill the interiors with life, room by room, asked by the owner after play test VI-8; a low pub partition. Earlier: Interiors: a fixed wall bench or settle, an upright piano and a stage platform, which the furnishing rules §8 and the Soot & Bell's pub plan need (VI-7). Earlier: Interiors: a drawn curtain for the pack's windows, so curtains can close at night (VI-2). Earlier: Interiors: Victorian Interiors, chosen by the owner for every interior, covers most of the interior list; what it covers is marked. Earlier: the city: the owner asked what would fill a Victorian town of a fantasy world; the list grew by play value)

Depends On: [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §8 and §14, [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md)

---

## Purpose

The owner's play test of 2026-11-06 found the seven interiors sparse. The agent has added what BK_AlchemistHouse and the packs of the asset map hold (city plan §14). This file lists what those packs still lack, so the owner can choose models or packs to add.

A pack outside the asset map needs the owner's approval. Nothing here is a decision.

## What every model must meet

- **Era.** Late Victorian, about 1850–1890: industrial and occult, not medieval. Iron, oak, brass, glass, oil and gas light.
- **Style.** Low-poly or stylised, close to BK_AlchemistHouse and Modular Victorian City. One model should not look more realistic than its room.
- **Pipeline.** It works in URP, or its materials can be copied to URP (`SliceUrpMaterials`). The pivot is at the base. It is at true scale in metres.
- **Kit.** It fits the kit standard: a footprint of whole metres, a front on local +Z, and a back that can stand against a wall. A wall model has a flat back.
- **Not wanted.** Bows, crossbows, staffs, wands or spears ([10.2](../../../10.2%20Combat%20System%20Draft.md): no such weapons). Remains in jars.

## Interiors

Priority A means the room reads as empty or wrong without the model. B means it adds life. C is a detail.

**2026-11-07:** the owner chose Victorian Interiors for every interior ([VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md)). It covers the bookcase, fireplace, armchair and settee, wardrobe (a tall cupboard), wall clock and mirror, wall lamp, curtains, writing desk, and the bucket and washstand pieces below. Still wanted from this list: an iron bedstead and a cell cot, the apothecary chest, an archive cabinet, a counter with scale and till, the chapel's altar and lectern, sacks, bar stools and cask rack, a pot rail, a wine rack, a drawn curtain for the pack's windows, and a fixed wall bench, an upright piano and a stage platform (INTERIOR_FURNISHING_RULES §8): the pack has only one, tied back, so the curtains cannot close at night (VICTORIAN_INTERIORS_PLAN §5).

| Model | Where (C1 §8) | Size, m (W × D × H) | Priority | Notes |
|---|---|---|---|---|
| Tall bookcase, full of books | Faction archive, office, Harrow's laboratory | 1–2 × 0.4 × 2.2 | A | BK has shelves only. Its book sets cannot fill a bookcase. |
| Fireplace with a mantelpiece | Soot & Bell taproom, faction hall, office | 2 × 0.6 × 1.4 | A | Only BK's iron stoves stand in for one now. |
| Armchair and settee, upholstered | Faction office and hall, the let room | 1 × 1 × 1; 2 × 1 × 1 | A | BK has no soft seats. |
| Wardrobe | The let room, the missing person's room | 1.2 × 0.6 × 2.1 | A | |
| Iron bedstead and a cell cot | Missing room, the lock-up's cells, poor dwellings (city plan §16) | 1 × 2 × 1 | A | The Tripo bed is ornate and too grand for a tenement. The cells have no bed. |
| Apothecary chest of many small drawers | Harrow's shop | 2 × 0.5 × 2 | A | The shop's signature piece. |
| Writing desk with drawers, or a bureau | Faction office, Wardens' office | 1.4 × 0.7 × 0.8 | B | BK's desk is a plain table. |
| Archive cabinet, or shelves of files and boxes | Faction archive, Wardens' office | 1 × 0.5 × 2 | B | |
| Shop counter with a brass scale and a till | Harrow's shop | 2 × 0.7 × 1 | B | |
| Wall clock and mirror | Every room with plaster walls | 0.5 × 0.1 × 0.8 | B | Hung like a painting (Anchor.Face). |
| Wall sconce, gas or oil | Every room | 0.3 × 0.2 × 0.4 | B | It replaces the hidden point light. |
| Curtains for BK's window | Plaster rooms | 1.3 × 0.1 × 2.6 | B | Fitted to BK's window at ×1.2 × 1.2. |
| Chapel altar, lectern and candelabra | Old chapel | 2 × 1 × 1.1 | B | The rostrum stands in for the altar now. |
| Sacks of grain and of coal | Kitchen, store room, cellar | 0.6 × 0.4 × 0.8 | C | |
| Fixed wall bench or settle, high-backed | Soot & Bell public bar and snugs; kitchens; the chapel | 2–3 × 0.6 × 1.2 | B | The pub plan's benches along the walls (rules §6). The public bar seats on stools and chairs until then |
| Upright piano | Soot & Bell stage; a rich drawing room | 1.5 × 0.6 × 1.3 | B | The stage stands without one (VI-7) |
| Stage platform with a front edge and steps | Soot & Bell taproom | 3 × 3 × 0.4 | C | Stands now as a primitive box with the pack's parquet (VI-8; a piece is never pieced together, furnishing rules G10); a model would give it a finished edge and steps |
| Low partition with etched glass, a pub screen | Soot & Bell taproom, between saloon and public bar | 3 × 0.1 × 1.4 | B | The pub plan's divider (rules §6). The pack's banister stood in for it in VI-7 and read as a lost stair rail (VI-8 remark 2), so it was taken out |
| Bar stools; casks on a rack | Soot & Bell taproom and cellar | 0.4 × 0.4 × 0.8; 2 × 0.8 × 1.5 | C | |
| Pots and pans on a hanging rail | Kitchens | 1.5 × 0.2 × 0.6 | C | |
| Bucket, chamber pot, washstand with a jug | Cells, the missing room | 0.4 × 0.4 × 0.4 | C | |
| Wine rack | Cellar under the Soot Stair | 2 × 0.4 × 2 | C | |

## Interior detail (2026-11-08)

The owner's play test VI-8 found the rooms still bare: furniture alone does not make a room lived in. These are the props that would fill them, room by room. Each small one stands on a surface or hangs on a wall, never on a walk-way (furnishing rules G1, G5); only leaves, papers, a toy in a nursery and the like lie on a floor. **Priority A** makes the room read as its purpose; **B** adds life; **C** is a detail.

**What the pack already has and the agent will place more of** (no model needed): books and book groups, bottles, cups, plates, pots, vases, candles, papers, scrolls, pen and ink, ashtray, soap, sponges, brushes, the telephone, the small clock, mirrors, plants, pictures, carpets, and the pack's six decals (`Alpha_1`–`Alpha_6`). The next furnishing pass sets them on tables, shelves, counters and mantels by each place's surface slots (rules §3).

**The pub** (Soot & Bell)

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Beer engine: a row of three or four pump handles | 0.6 × 0.3 × 0.5 | A | On the bar. The one object that says *pub* |
| Pewter tankards and pint glasses, a glass rack | 0.1 × 0.1 × 0.15 | A | On the bar, the tables, the back bar |
| A brass bell over the bar | 0.4 × 0.4 × 0.5 | A | The house's name: *the Soot & Bell* |
| Spirit casks on a cradle; a cask tap | 0.6 × 0.4 × 0.5 | B | On the back bar |
| A till or a cash drawer | 0.4 × 0.4 × 0.3 | B | On the bar |
| Chalk slate of prices, a licence board | 0.6 × 0.05 × 0.8 | B | Hung on the back wall |
| Playing cards, dominoes, a cribbage board, a clay pipe | 0.2 × 0.1 × 0.05 | B | On the tables |
| A dartboard; a spittoon; sawdust on the public bar's floor (decal) | 0.5 × 0.05 × 0.5 | C | The public bar's plainness |

**Kitchens and sculleries**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Copper pans and a kettle; a hanging pot rail | 1.5 × 0.2 × 0.6 | A | Already wanted as the pot rail; with the pans on it |
| Bread, vegetables in a basket, a cheese, a joint of meat on a hook | 0.3 × 0.3 × 0.2 | A | On the kitchen table, the dresser, the larder shelves |
| Chopping board, rolling pin, mixing bowl, a pestle | 0.4 × 0.3 × 0.1 | B | On the table |
| Coal scuttle and fire irons | 0.4 × 0.3 × 0.5 | B | Beside each hearth (rules §8 lacks it) |
| Wash tub with a washboard; a mangle | 0.7 × 0.7 × 1 | B | Sculleries and poor homes' washrooms |
| Stone jars and crocks; a butter churn | 0.3 × 0.3 × 0.5 | C | Larder, scullery |

**Bedrooms and washrooms**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Ewer and basin on the washstand, a chamber pot | 0.4 × 0.4 × 0.4 | A | Already wanted; every washstand needs one |
| Folding dressing screen | 1.5 × 0.1 × 1.7 | B | A rich bedroom, the let rooms |
| Clothes: a coat and hat on the stand, a dress on a hanger, folded linen, boots by the bed | 0.6 × 0.3 × 1 | B | The stand and the hangers exist but stand bare |
| Candlestick with matches, a pocket watch, spectacles, a prayer book | 0.1 × 0.1 × 0.2 | B | Nightstands |
| Hat boxes, a travelling trunk with labels, a sewing basket | 0.5 × 0.4 × 0.4 | C | On a wardrobe, at a bed's foot |

**Studies, offices and the archive**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Ledgers and stacks of files, a letter rack, a seal with wax | 0.3 × 0.2 × 0.2 | A | On desks and archive shelves |
| A strongbox or a small safe | 0.6 × 0.6 × 0.8 | B | Offices, the lock-up; also a theft target ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6) |
| A wall map of Veilmouth, framed certificates, a notice board with papers | 1 × 0.05 × 0.8 | B | The faction's office, the Wardens' office |
| A magnifying glass, a candle snuffer, a blotter | 0.2 × 0.1 × 0.05 | C | On desks |

**Shops and the apothecary**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Labelled jars, phials and powders in rows; a mortar and pestle; brass scales | 0.1 × 0.1 × 0.2 | A | The apothecary's counter and shelves (the chest and the counter's scale are wanted above) |
| Wrapped parcels, string and paper, price cards | 0.3 × 0.2 × 0.1 | B | Every shop counter |
| A glazed display case for a counter's top | 1 × 0.5 × 0.4 | B | Small valuables in view: a theft target |

**The lock-up, the faction's hall, the chapel**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| Wanted posters and notices on the board, a ring of keys on a hook, manacles, a lantern | 0.3 × 0.1 × 0.4 | A | The Wardens' desk and guard room |
| A rifle rack with two rifles | 1 × 0.3 × 1.6 | B | The guard room ([10.2](../../../10.2%20Combat%20System%20Draft.md): rifles and pistols are the era's firearms) |
| The faction's banners and its emblem | 1 × 0.05 × 2 | A | The faction's hall: whose house it is |
| Hymn books on the pews, an offering box, a lectern's open book | 0.2 × 0.15 × 0.05 | B | The chapel and the hall |

**Poor homes, the nursery, the warehouse**

| Model | Size, m | Priority | Notes |
|---|---|---|---|
| A straw mattress, a patched blanket, a rag rug | 2 × 1 × 0.2 | A | Poor bedrooms read poor |
| Tin plates and mugs, a cracked jug | 0.2 × 0.2 × 0.2 | B | Poor kitchens |
| Laundry on a line across a room | 3 × 0.1 × 1 | B | Poor homes, courts |
| Toys: a rocking horse, a doll, tin soldiers, blocks; a cradle | 0.8 × 0.3 × 0.6 | B | The nursery: the one room where small things lie on the floor |
| Coils of rope, a sack truck, a platform scale | 1 × 0.6 × 1.2 | B | Warehouses, store rooms |

**Signs of life everywhere** (C): an umbrella stand by the hall door, a walking cane, a newspaper on a chair, an open letter on a table, a cat asleep by a hearth (rigged), cobwebs and damp stains in poor homes and cellars (decals), soot over the hearths (decals).

## The city

The owner asked on 2026-11-06 which things would make the city read as lived in: a Victorian town of a fantasy world, between the late 18th and the early 20th century. The agent's answer, by what each thing does for play. **A** serves a mechanic or a reading the slice needs. **B** makes a street read as Victorian, not medieval. **C** is polish. A model marked *Tripo* is a single prop the owner can make through the Tripo Bridge, as with the coins. One that moves, such as a horse or a crowd, needs a rigged pack.

**What the streets say about who runs the city** (A)

| Model | Where | Size, m | Priority | Notes |
|---|---|---|---|---|
| Posters and handbills on walls, as decals | Every street; thicker in Lower Veil | 0.5 × 0.7 | A | Diegetic news ([21](../../../21%20Cult%20Campaign%20and%20City%20Control.md): public events come through the world). Notices of the Wardens, the faction's calls, shop adverts. Tripo or flat images |
| Cult marks and signs of a ritual: chalk and paint on walls and paving, as decals | Lanes, the ritual's Weak Point traces | 1 × 1 | A | The traces of a Weak Point ([11](../../../11%20Vertical%20Slice.md)) must be seen in the world. Decals the seed places |
| Wayside shrine of each of the three churches, and a small statue of a saint or god | Street corners, the chapel road | 1 × 0.6 × 2 | A | The three church-associated factions should be seen in the street, not only in buildings |
| Wardens' call box or sentry box, and a notice of the Wardens | The Ridge, the High Street, the docks | 1 × 1 × 2.4 | A | Where a witness runs to fetch a Warden ([10.3](../../../10.3%20Stealth%20and%20Detection.md) §6.2) |
| Hanging shop signs on brackets, one per trade | Over every shop door | 0.8 × 0.1 × 0.6 | A | The player reads a shop from the street: apothecary, smith, tailor, pawnbroker, tavern. Tripo, one per trade |

**The Victorian street** (B)

| Model | Where | Size, m | Priority | Notes |
|---|---|---|---|---|
| Garden gate for the street railing | Terrace and villa gardens | 1.2 × 0.1 × 1.1 | B | The railings leave a gap where a gate belongs. |
| Low hedge, clipped | Terrace and villa gardens | 2 × 0.6 × 0.9 | B | Today's polytope shrubs are wild. |
| Cast-iron bollards, kerb posts | Pavement edges, squares | 0.3 × 0.3 × 0.9 | B | They also give the Combat Grid low obstacles |
| Pillar box, street name plates, horse trough, water pump | High Street, Bell Street, squares, courts | 0.6 × 0.6 × 1.5 | B | |
| Advertising column and a newspaper kiosk | The High Street, the Market | 1.2 × 1.2 × 3 | B | Another diegetic channel for news |
| Awnings and shutters for shop fronts | Shop fronts | 3 × 1.2 | B | Fitted to Modular Victorian City's shop front |
| Window boxes with flowers, curtains seen through windows | Terraces, the Ridge | 1 × 0.2 × 0.3 | B | Rich and poor streets look different |
| Drainpipes, gutters, coal hole covers, drain grates | Every house; the paving | — | B | Small, many; most can be decals |
| Telegraph or gas poles with wires | Main streets | 0.3 × 0.3 × 7 | B | The industrial edge of the era |
| Factory chimneys and a gasometer, far off | The skyline beyond the wall | 6 × 6 × 40 | B | Seen, never entered: the industrial-occult mood |
| Chimney smoke | Every roof | VFX | B | Hovl's or Unity's particles; no model |

**Trade and the docks** (B)

| Model | Where | Size, m | Priority | Notes |
|---|---|---|---|---|
| Handcart, wheelbarrow, delivery cart without a horse | Market, docks, warehouses | 2 × 1 × 1 | B | Tripo |
| Dock crane, mooring bollards, coils of rope, nets, an anchor | The sea front | 4 × 4 × 8 | B | Row 22 of the asset map |
| A moored boat or two, and a steamship far out | The sea front | 8 × 3 | B | A ship far out needs only a silhouette |
| Sacks, baskets of produce, fish on ice, hanging meat, scales | Market stalls | 0.6 × 0.4 × 0.8 | B | Low-Poly Medieval Market has some of these; the rest Tripo |

**Life in the street** (C)

| Model | Where | Size, m | Priority | Notes |
|---|---|---|---|---|
| Washing lines with laundry between houses | Courts, tenements | 6 × 0.1 | C | |
| Rubbish heaps, broken crates, puddles and soot as decals | Lower Veil, lanes | — | C | Poverty read from the ground |
| Pigeons and crows, stray dogs and cats, rats | Squares, roofs, lanes | — | C | Rigged animals: a pack, not Tripo |
| Horse-drawn cab and carriage, with a horse | Crown Avenue, the High Street | 4 × 2 × 2.5 | C | Rigged; parked ones first |
| Bandstand or gazebo, fountain, statue on a plinth | A square, the Palace Forecourt | 6 × 6 × 5 | C | Row 22 of the asset map. Statues: the owner's three angels stand in Wardley Garden, the graveyard and an east villa's garden, two marble lions at the Palace Forecourt, and birdbaths in gardens (city plan §23, 2026-11-08); fountains to come from the owner |
| Flower bed or urn | Gardens, the Palace Forecourt | 1 × 1 × 0.8 | C | |
| Ivy and moss on walls, as decals | The Ridge's gardens, the old chapel | — | C | |

## How to add one

1. Choose the model or the pack, and add the pack to [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) with the owner's approval.
2. The agent wraps it in `SliceKitCatalogue` as a kit block. A wall piece uses `Anchor.Back` or `Anchor.Face`.
3. The agent places it in `VeilmouthInteriors.Furnish`, rebuilds by batch, and runs the kit check.
