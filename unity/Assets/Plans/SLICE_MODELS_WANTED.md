# Slice Models Wanted

Version: 0.1

Status: Draft

Last Updated: 2026-11-07 (Interiors: Victorian Interiors, chosen by the owner for every interior, covers most of the interior list; what it covers is marked. Earlier: the city: the owner asked what would fill a Victorian town of a fantasy world; the list grew by play value)

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

**2026-11-07:** the owner chose Victorian Interiors for every interior ([VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md)). It covers the bookcase, fireplace, armchair and settee, wardrobe (a tall cupboard), wall clock and mirror, wall lamp, curtains, writing desk, and the bucket and washstand pieces below. Still wanted from this list: an iron bedstead and a cell cot, the apothecary chest, an archive cabinet, a counter with scale and till, the chapel's altar and lectern, sacks, bar stools and cask rack, a pot rail, and a wine rack.

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
| Bar stools; casks on a rack | Soot & Bell taproom and cellar | 0.4 × 0.4 × 0.8; 2 × 0.8 × 1.5 | C | |
| Pots and pans on a hanging rail | Kitchens | 1.5 × 0.2 × 0.6 | C | |
| Bucket, chamber pot, washstand with a jug | Cells, the missing room | 0.4 × 0.4 × 0.4 | C | |
| Wine rack | Cellar under the Soot Stair | 2 × 0.4 × 2 | C | |

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
| Bandstand or gazebo, fountain, statue on a plinth | A square, the Palace Forecourt | 6 × 6 × 5 | C | Row 22 of the asset map |
| Flower bed or urn | Gardens, the Palace Forecourt | 1 × 1 × 0.8 | C | |
| Ivy and moss on walls, as decals | The Ridge's gardens, the old chapel | — | C | |

## How to add one

1. Choose the model or the pack, and add the pack to [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) with the owner's approval.
2. The agent wraps it in `SliceKitCatalogue` as a kit block. A wall piece uses `Anchor.Back` or `Anchor.Face`.
3. The agent places it in `VeilmouthInteriors.Furnish`, rebuilds by batch, and runs the kit check.
