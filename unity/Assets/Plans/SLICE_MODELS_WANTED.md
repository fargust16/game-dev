# Slice Models Wanted

Version: 0.1

Status: Draft

Last Updated: 2026-11-06

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

| Model | Where | Size, m | Priority | Notes |
|---|---|---|---|---|
| Garden gate for the street railing | Terrace and villa gardens | 1.2 × 0.1 × 1.1 | B | The railings leave a gap where a gate belongs. |
| Low hedge, clipped | Terrace and villa gardens | 2 × 0.6 × 0.9 | B | Today's polytope shrubs are wild. |
| Flower bed or urn | Gardens, the Palace Forecourt | 1 × 1 × 0.8 | C | |
| Pillar box, street sign, horse trough | High Street, Bell Street | 0.6 × 0.6 × 1.5 | C | |

## How to add one

1. Choose the model or the pack, and add the pack to [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md) with the owner's approval.
2. The agent wraps it in `SliceKitCatalogue` as a kit block. A wall piece uses `Anchor.Back` or `Anchor.Face`.
3. The agent places it in `VeilmouthInteriors.Furnish`, rebuilds by batch, and runs the kit check.
