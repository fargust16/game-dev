# 19 Items, Crafting, Inventory, Equipment, and Enchanting

Version: 0.38
Status: Draft — owner-confirmed rules with delegated starting-balance formulas; content tables, operation costs, and selected edge cases remain open
Last Updated: 2026-11-13 (§6.3: the Stash and the purses of named people, Wardens and merchants as built in S5c.5. Earlier: §6.2: the Faction Gift as built in S5c.4. Earlier: §6.2: the Faction Gift offers firearms and armour while melee is withheld; §6.3: the brewing benches' fees and the Hidden Caches, with the owner's answers for S5. Earlier: §6.1 *Supply*: Outfit V1, by the owner's follow-up to COMBAT-7: the allies' fighters supplied by their industry faction's makers, the cult's poorly, the Shop Skills authoring defaults. Earlier: §4.2: the combat rework, 10.2 §5f: light and heavy weapons, a weapon asks only its profile attribute, the trait Heavy, a kind of cartridge a kind of pouch, the Wardens' baton, the shield as Half Cover against shots, items that add Prepared Ability slots. Earlier: §4: two one-handed melee weapons may share the hands, 10.2 §5b *Two weapons*. Earlier: §6.4: by the owner's second direction of 2026-11-11, after the play test of S5a, a cycle lasts as long as its animation and its Reaction Check comes at the key frame with the world paused; a herb or flax patch is one check that empties it; Skinning is a Reaction Check. Earlier: §3, §4.3, §6.4: by the owner's direction of the S5a play test, Item Creation is one Reaction Check at an anvil and a failure loses half of each resource, in place of the rule that it always makes its item; each gathering cycle is a Reaction Check; in the slice gathering tools wear and break, authoring defaults. Earlier: §6.4: the slice's working readings of S5a.6 pointed to, nothing decided: a critical skinning gives the whole hide, a failed one spoils it for good, taking the ingredients takes no time, and a glowing beast leaves the bright glow essence. Earlier: §6.4: the slice's working readings of S5a.5 pointed to, nothing decided: no load multiplier on a cycle's Stamina while 07.2 leaves it open, E again and a beast's warning cancel gathering, a full bag ends it, and the gathering animation still to come. Earlier: §3: the slice's working readings of S5a.4 pointed to, nothing decided: an interrupted piece costs nothing, the work's ticks pass at once, and a piece begun before closing is finished. Earlier: §2.4 and §6.3: the slice's working readings of S5a.3 pointed to, nothing decided: a shield's favoured types, tiers at a higher stage, repeated types, what a shop buys, and a morning's shelf. Earlier: §4.2: the slice's working readings of S5a.2 pointed to, nothing decided: a mixed set's defence by each piece's weight share, fists, the wooden shield, the off hand. Earlier: a dwelling's Stash, by the owner: it reads from who lives there; its chance and money are authoring defaults checked by `tools/balance/burglary_sim.py`. Earlier: the slice's only ore is iron ore, and an emptied node disappears, by the owner. Earlier: money in bronze, silver, and gold, 100 to 1, by the owner; every price recalculated in bronze; a shield goes only with a sword, an axe, or a mace, and spears do not exist, by the owner. Earlier: the owner's answers: the Master of Item Creation teaches Skinning, a beast's ingredients need no lesson, and gathering has no Masters of its own; who sells which tools and ingredients approved. Earlier: gathering, Skinning, learning Item Creation from its Master, the workshop, stage 9 recipes, and the weights of resources, tools and First Aid supplies: the owner's rules, the agent's numbers. Earlier: prices, wages, selling and stock (§6.3), authoring defaults; Shop Skill +1 a day kept as an authoring default. Earlier: durability is off in the slice, by the owner. Earlier: the Attribute Requirement penalty grows with the shortfall. Earlier: Attribute Requirements V0: values and stage scaling in 10.2 §5b. Earlier: Attribute Requirements for weapons and armour, and silver cartridges deal more damage to monsters, by the owner's direction. Earlier: the hand on the Cartridge Pouch loads each shot, with no separate Reload. Earlier: kinds of weapon, the Cartridge Pouch, and two Weapon Sets, by the owner's decision. Earlier: armour splits its defence by the owner's direction: cloth against magic, leather half and half, plate against physical harm. Earlier: a pistol can share the hands with a one-handed weapon, a shield, or a second pistol, and Reload needs a free hand, by the owner's decision. Earlier: bows and the mini-crossbow removed by the owner: weapons are iron, for melee and firearms. Earlier: V1 counts no ammunition, by the owner's decision. Earlier: catalogue gains Magical Damage, Will Power, and magical defence, and critical and defence values rise; crossbows out, rifle and pistol in; crafting has no ceiling, by the owner's decision. Earlier: shops, Masters, Shop Skill, and the Faction Gift added. Earlier: catalogue values reworked, with minor modifiers at half of major ones. Earlier: working Modifier Catalogue added as authoring defaults, and the owner's direction that items decide fights. Earlier: working weapon and armour values referenced. Earlier: terms renamed to Arcana and Limit. Earlier: profession scope now distinguishes equipment crafting from the new potion-only Alchemy profession.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [02 World Laws](02%20World%20Laws.md), [05 Progression](05%20Progression.md), [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [README](README.md)

---

# Purpose and authority

This draft consolidates owner-approved direction for item bases, Item Creation, modifier generation, inventory, equipment, durability, loot, and Enchanting. It replaces conflicting older itemisation notes in [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md) and [99 Open Questions](99%20Open%20Questions.md) for current design work. It remains a Draft and does not authorise implementation.

# 1. Character foundation and item-related skills

- There is no global character level. Attributes and skills have separate progression tracks; ordinary skill progression is distinct from Limits.
- The item/equipment-crafting professions are **Item Creation** and **Enchanting**. **Alchemy** is a separate, potion-only profession defined in [20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md); it does not create equipment or modify items. Luck and Perception are attributes, not professions. No separate crafting profession exists for each material or equipment family.
- **Item Creation** creates equipment from bases and required resources. Its skill is the primary influence on modifier count; the starting probability formula is in §3. By the owner's decision of 2026-11-04 it is learned from its Master, as Alchemy is; gathering needs no lesson (§6.4).
- **Luck** and **Perception** affect modifier count as specified in §3. Neither changes a base's fixed modifier-type weights. Luck also follows the shared probability rule in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md); fixed weighted item tables are not rewritten by Luck.
- **Enchanting** performs the operations in §5. Learning the profession grants the first operation; later skill thresholds unlock the next operations in order. Enchanting skill shifts tier outcomes in its own random operations but does not change modifier-type weights. The exact unlock levels remain open.
- Attribute, skill, and shared probability rules are in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md).

# 2. Item bases, modifiers, and quality

## 2.1 Bases and stages

- Items are created from a defined base with foundational properties. Bases cover armour, weapons, rings, necklaces/amulets, belts, and other accessory slots.
- Armour families currently include cloth, leather, chain/chainmail, and plate/heavy metal. One general Item Creation profession serves every family. By the owner's direction of 2026-11-03 a family's base properties split its defence: cloth gives very high magical and very low physical defence, leather half and half, and plate physical defence only ([10.2](10.2%20Combat%20System%20Draft.md) §5b).
- Item stages run from 9 (lowest) through 0 (highest). Item Creation skill grants access to the minimum stage shown below; a character may still craft any lower-stage base when they supply its required materials.

| Item Creation skill | Highest item stage available |
|---:|---:|
| 1–10 | 9 |
| 11–20 | 8 |
| 21–30 | 7 |
| 31–40 | 6 |
| 41–50 | 5 |
| 51–60 | 4 |
| 61–70 | 3 |
| 71–80 | 2 |
| 81–90 | 1 |
| 91–100 | 0 |

- Higher-stage bases require more varied and harder-to-obtain ordinary resources plus one rare ingredient of the corresponding stage. The rare ingredient is consumed. Any rare ingredient of the required stage is valid; its identity affects the generated name only, not base properties, modifier weights, or stats.
- A compact base catalogue is preferred. Initial count, final families, and visual-variant plan remain open; visual variants are not automatically separate mechanical bases.

## 2.2 Modifier count, weights, tiers, and quality

- An ordinary item has 0–6 modifiers, with no more than 3 **major** and 3 **minor** modifiers. The starting probability model is in §3.
- Major/minor assignment is random, independent of base, item stage, skill, Luck, and Perception, subject to each group's three-slot maximum. Six modifiers must be 3+3; four may use any valid distribution. The probability among valid group splits remains open.
- Every base has a fixed modifier-type weight table. Each base's favored modifier types receive higher weights; all other modifier types receive lower, nonzero weights. For this draft, "favored" means modifier identity/type, independent of the major/minor group; confirm this interpretation because the existing group assignment remains random. These are likelihood weights, not a modifier compatibility gate. Luck, Perception, Item Creation, and Enchanting cannot change the fixed type weights.
- Modifier tier weights depend on the item's base stage. Higher-stage bases favour stronger tiers more often; no hard stage-tier ceiling is introduced, so even a stage-9 item can theoretically receive tier 0. Enchanting skill may shift tier outcomes in its own operations toward stronger tiers, but not type weights.
- Modifier tiers use the 9-to-0 scale, with tier 0 strongest. Numerical values are rolled within the selected tier's range unless an operation changes the value. Exact per-base type weights, stage/tier weights, value ranges, and duplicate rules remain open.
- Quality follows total modifier count: **Normal** = 0; **Magic** = 1–2; **Rare** = 3; **Epic** = 4; **Legendary** = 5–6. **Unique** is a separate, individually authored item type; it is not generated by this count mapping.
- Recalculate quality whenever modifier count changes. The displayed category changes only when the count crosses a boundary (0→1, 2→3, 3→4, 4→5); adding within a band does not change its category.
- Rarity colours: Normal — grey/desaturated; Magic — green; Rare — cyan/light blue; Epic — purple; Legendary — yellow-orange; Unique — red. Exact colour values and final UI treatment remain open.

## 2.3 Capacity and groups

- No item may exceed 3 major, 3 minor, or 6 total modifiers. Any operation that adds a modifier must obey these limits.
- Inserting an extracted modifier requires a free slot in that modifier's own group. A major modifier cannot be inserted where three major modifiers already exist, even if a minor slot remains open.
- If there is no free slot in the required group, insertion is impossible. Random addition also cannot exceed either group cap; how it selects among legal groups remains open.

## 2.4 Working Modifier Catalogue

Owner's direction, 2026-11-02: items strengthen a character significantly, and whole sets, not single items or consumables, decide fights ([10.2](10.2%20Combat%20System%20Draft.md) §5b). The values below are authoring defaults checked by `tools/balance/item_sim.py`, not owner-approved rules. They answer the open items of §2.2 only as far as a simulation needs.

| Type | A major modifier of tier 9 gives |
|---|---:|
| Damage | +11 % |
| Accuracy | +5 |
| Critical chance | +11 percentage points |
| Critical damage | +30 percentage points |
| Maximum HP | +9 % |
| Physical defence | +5 |
| Magical defence | +4 |
| Magical Damage | Magical damage of 11 % of the weapon's base on every blow |
| Maximum Will Power | +9 % |
| Evasion | +4 |
| AP | +0.8 |
| Initiative | +3 |
| Will against control | +4 |

- A minor modifier gives half of a major one. Each modifier is major or minor at even odds while its group has room, which is a working reading of the split left open in §2.2.
- Each tier above 9 adds half of the tier 9 value, so tier 7 gives double. A value rolls within ±20 %.
- At stage 9 a modifier is tier 9 in 70 % of rolls, tier 8 in 25 %, and tier 7 in 5 %.
- A modifier takes one of its base's favoured types in 75 % of rolls, and any type in the rest.

| Base | Favoured types |
|---|---|
| Weapon | Damage, Accuracy, Critical chance, Critical damage, Magical Damage |
| Cartridge Pouch | Damage, Accuracy, Critical chance, Magical Damage (working reading) |
| Head | Accuracy, Critical chance, Will, HP |
| Body | Defence, HP |
| Shoulders | Defence, Magical defence, Will, HP |
| Gloves | Damage, Accuracy, Critical chance |
| Trousers | HP, Defence, Evasion |
| Boots | Evasion, Initiative, AP |
| Belt | HP, AP, Defence |
| Ring | Damage, Critical chance, Critical damage, Magical Damage, Maximum Will Power |
| Necklace | Will, HP, Initiative, Accuracy, Maximum Will Power, Magical defence |

A whole set is 11 pieces. A set from a city shop, at Shop Skill 26 (§6.1), carries about 16 modifiers, and one from a capital Master, at 50, about 26. A character with skill 1–10 averages under one modifier an item. A weapon with Magical Damage is what a magic pistol or a magic rifle is, and it needs no Will Power, unlike Infusion (10.2 §5b). Critical chance and Critical damage here are item modifiers, not the separately levelled attributes that [07.2](07.2%20Derived%20Character%20Statistics.md) excludes.

- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.3): a shield favours the body's types, physical defence and HP; a stage above 9 moves the tier odds up one tier a stage; a piece may carry the same type twice, and the two add; a worn piece's modifiers count in a fight by the share of its Attribute Requirements its wearer meets.

# 3. Item Creation

Item Creation combines a selected base with its stage-matched resources. The following starting balance formula is an assistant-selected parameter under the owner's delegation. It defines a smooth modifier-count progression while keeping Item Creation as the main influence, Perception as a modest secondary influence, and Luck on the shared universal multiplier.

Let:

- `I` = Item Creation skill level, 1–100;
- `P` = Perception, 1–100;
- `L` = Luck, 1–100.

First calculate the base chance for each of six potential modifier additions:

`p_base = 0.10 + 0.60 × ((I − 1) / 99)`

Perception adds up to a 10% **relative** increase:

`p_perception = p_base × (1 + 0.10 × ((P − 1) / 99))`

Then apply the shared Luck multiplier and probability cap from [07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks):

`p = min(0.99, p_perception × (1 + L / 100))`

Each of the six potential additions is an independent Bernoulli trial with probability `p`. Therefore the total modifier count `K` follows:

`K ~ Binomial(6, p)`

The starting model yields about 10.1% per potential modifier at Item Creation 1 / Perception 1 / Luck 1, and caps at 99% per potential modifier at 100/100/100. These are initial balance values, not measured outcomes. Group assignment happens after count generation, is random within the 3+3 limits, and keeps its own unresolved valid-split distribution.

For every generated modifier, the selected base supplies the fixed type weights; the base stage supplies tier weights; the tier supplies the numeric range. Item Creation, Luck, and Perception affect modifier count only. Enchanting skill may influence tier outcomes during Enchanting operations, not base type weights.

**Learning and the workshop.** Owner's decision, 2026-11-04: anyone may gather, but only a character who has learned Item Creation from its Master may craft, as with Alchemy. Authoring defaults the same day:

- The lesson costs 1 000 bronze (§6.3).
- Crafting happens at the Master's workshop, which costs 100 bronze a piece and is open while the Master works ([18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)). Owner's direction, 2026-11-11 (the play test of S5a): the character crafts at a crafting station, an anvil, not through the Master; in the slice it is the anvil in the Master's yard, and the fee is paid there.
- A stage 9 piece takes 10 ticks and a stage 8 piece 20. The world advances during the work, and being attacked interrupts it with the resources kept.
- Owner's direction, 2026-11-11, in place of the owner's rule of 2026-11-04 that Item Creation always makes its item: a piece is one Reaction Check at the anvil ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §8a), on Dexterity and Item Creation against 2 at stage 9 and 4 at stage 8. A good result makes the piece, whose modifier count is random as above. A bad one makes nothing and loses half of each resource, rounded up (authoring default); the fee is paid either way. For XP of 07.1 §3 the results count as §8's outcomes.
- The work spends 2 % of maximum Stamina a tick ([07.2](07.2%20Derived%20Character%20Statistics.md) §3.2), and a piece is not begun without enough for all of it (authoring default).
- A crafter reaches about skill 11 to 14 in a run, and 15 is the ceiling, because stage 8 pieces need a rare ingredient ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §3, `tools/balance/progress_sim.py`).
- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.4): the fee and the resources are taken only when a piece is finished, so an interrupted piece costs nothing; the work's ticks pass at once, the world running through each, as a treatment's do; a piece begun before closing is finished after it; stage 8's rare ingredient is the bright glow essence.

**Recipes of stage 9.** Authoring defaults, 2026-11-04.

| Item | Resources |
|---|---|
| Dagger | 2 ore, 1 wood |
| Sword | 3 ore, 1 wood |
| Axe | 3 ore, 2 wood |
| Mace | 4 ore, 1 wood |
| Greatsword | 5 ore, 1 wood |
| Greataxe | 5 ore, 2 wood |
| Maul | 6 ore, 2 wood |
| Pistol | 3 ore, 2 wood |
| Rifle | 4 ore, 3 wood |
| Cartridge Pouch | 2 leather |
| Wooden shield | 4 wood |
| Metal shield | 5 ore, 1 wood |
| Ring | 1 ore |
| Necklace or amulet | 2 ore |
| Belt | 1 leather |

| Armour (head / body / shoulders / gloves / trousers / boots) | Resource |
|---|---|
| Cloth | 2 / 4 / 2 / 1 / 3 / 2 flax |
| Leather | 1 / 3 / 2 / 1 / 2 / 1 leather |
| Chain | 2 / 6 / 3 / 2 / 4 / 2 ore |
| Plate | 3 / 9 / 4 / 2 / 6 / 3 ore |

A stage 8 piece takes half as many resources again, rounded up, plus one rare ingredient of stage 8 (§2.1). In the slice that is a glowing beast's magical ingredient, or the same bought from the Master of Enchanting for 15 000 bronze (§6.3). It competes with the Limit 8 potion for the same ingredient ([05.1](05.1%20Arcana%20and%20Limits.md)).

# 4. Inventory, equipment, and durability

## 4.1 Inventory and weight

- Personal inventory is an unlimited bag: there is no item-count cap, grid capacity, or variable footprint. Every item has the same-size inventory entry/icon; weight is the only carrying limit.
- Every carried item has an assigned weight. Total carried weight includes equipped items and equals the sum of item weights; stack weight is the per-unit weight multiplied by quantity. Coins weigh 0 and never change the load.
- Starting weight balance (kg): ring 0.05; necklace/amulet 0.10; belt 0.25. Cloth armour: head 0.5, body 2, shoulders 0.8, gloves 0.4, trousers 1.2, boots 0.8. Leather armour: 1, 4, 1.5, 0.8, 2.5, 1.5 respectively. Chain armour: 1.8, 7.5, 3, 1.4, 4.5, 3 respectively. Plate/heavy-metal armour: 2.8, 11, 4.5, 2, 7, 4.5 respectively.
- Starting hand-item weights (kg): dagger 1; sword 2; axe 2.2; mace 2.5; greatsword 4.5; greataxe 5; maul 6; wooden shield 2.5; metal shield 4; rifle 4; pistol 1.2; Cartridge Pouch 0.5.
- Resources, tools, and supplies (kg, authoring defaults, 2026-11-04): ore 1; wood 1.5; leather 0.5; flax 0.2; herb 0.1; ordinary beast ingredient 0.2; magical ingredient 0.1; woodcutting axe 2; pickaxe 2.5; bandage 0.1; splint 0.5; antidote 0.2; potion 0.3.
- These weights are initial balance values. Specific bases, tools, materials, consumables, and additional item families must receive explicit weights in their content definitions. Cloth should remain lighter than leather, leather lighter than chain, and plate/heavy metal heaviest; jewellery is nearly weightless.
- Working base damage, attack AP, armour values, and block and parry chances are in [10.2](10.2%20Combat%20System%20Draft.md) §5b.
- Stacking, equipment slots, and containers remain distinct from item footprints. A separate home stash is intended, but its capacity and whether it is weight-limited remain open.

## 4.2 Equipment

- The character has two universal hand slots. Compatible one-handed weapons, shields, talismans, books, and similar items may occupy either hand; a two-handed item uses both. A shield goes only with a sword, an axe, or a mace in the other hand, never with a dagger, by the owner's decision of 2026-11-05. A one-handed melee weapon may go in each hand, a dagger, a sword, an axe or a mace, by the owner's decision of 2026-11-11, each blow struck with either hand ([10.2](10.2%20Combat%20System%20Draft.md) §5b *Two weapons*). A pistol uses one hand and a Cartridge Pouch the other. A rifle uses both hands, and its Cartridge Pouch fills the off-hand slot, as a quiver does in Path of Exile. Nothing else may share the hands with a firearm, by the owner's decision of 2026-11-03, which withdraws the earlier decision of that day that let a pistol go with a one-handed weapon, a shield, or a second pistol ([10.2](10.2%20Combat%20System%20Draft.md) §5b). V1 counts no cartridges as items, by the owner's decision of 2026-11-03. Every weapon is iron, a melee weapon or a firearm: staffs and wands do not exist, by the owner's direction of 2026-11-02, and bows and crossbows of any size do not exist, by the owner's decision of 2026-11-03. Spears do not exist either (owner, 2026-11-05).
- **Cartridge Pouch.** Owner-approved, 2026-11-03: an item of its own, needed for every firearm, whose cartridges never run out. It has its own modifiers, it can be enchanted, and it comes in every quality up to Legendary, and as a Unique. Its cartridges are ordinary, against people, or silver, which deal more damage to monsters, by the owner's direction of 2026-11-03. How much more waits for monsters, and the slice uses ordinary cartridges only. The hand on the pouch loads each shot, since 2026-11-03 with no separate Reload ([10.2](10.2%20Combat%20System%20Draft.md) §5b), and changing the pouch in a fight costs 2 AP. Owner, 2026-11-13 (the combat rework's Q8 (c)): a kind of cartridge is a kind of pouch, and enchanted ammunition is the pouch's Magical Damage and its other magical modifiers ([10.2](10.2%20Combat%20System%20Draft.md) §5f).
- **Weapon Sets.** Owner-approved, 2026-11-03: a character carries two Weapon Sets, each filling the hands, and changes between them in a fight for 2 AP, an authoring default within the owner's 1–2 AP.
- **Kinds of weapon.** Owner-approved, 2026-11-03: dagger, sword, axe, and mace in one hand; greatsword, greataxe, and maul in two; pistol and rifle with a Cartridge Pouch. Each has its own trait, and their numbers are in [10.2](10.2%20Combat%20System%20Draft.md) §5b. Owner's direction, 2026-11-03: weapons and armour have Attribute Requirements. Below them a weapon aims worse and armour gives less of its bonuses, about half of what the item would. The penalty grows with the shortfall, by the owner's direction: at half a requirement an item gives half. An item of stage S asks (10 − S) times its values of stage 9; the values are authoring defaults in [10.2](10.2%20Combat%20System%20Draft.md) §5b, *Attribute Requirements*, and since the combat rework §5f. Cloth, a shield, and the Cartridge Pouch ask nothing.
- **Light and heavy weapons** (owner, 2026-11-13, the combat rework; [10.2](10.2%20Combat%20System%20Draft.md) §5f): the dagger, sword, axe, mace, the Wardens' baton, pistol and rifle are light and aim by Dexterity; the greatsword, greataxe and maul are heavy and aim by Strength. A weapon asks only that profile attribute (Q4 (b)). The trait **Heavy**, which halves its carrier's movement while its set is in hand, is a weapon's own and never every two-handed weapon's: working reading, heavy firearms carry it, none of which exists yet, and no melee weapon (99 COMBAT-5).
- **The Wardens' baton** (owner, 2026-11-13, Q9 (a); [10.2](10.2%20Combat%20System%20Draft.md) §5f): light melee, base 5, 4 AP, To hit +5, asks Dexterity 3, and subdues: a target it brings to 0 HP falls unconscious and is arrested. It is the Wardens' second set beside their firearm; whether a shop sells it waits for melee to return to the shops (§6.1).
- **Shields against shots** (designed now, built after the slice; [10.2](10.2%20Combat%20System%20Draft.md) §5f, 99 COMBAT-10): besides blocking melee blows, a held shield is Half Cover, −15, against shots and targeted spells at its carrier, from any side.
- **Prepared Ability slots** ([07.2](07.2%20Derived%20Character%20Statistics.md) §4.5): Intelligence and Will give 3 to 10, and items and artefacts may add more beyond 10. Which items carry the modifier and its values wait for the catalogue's next pass; the slice has none.
- Current body/accessory slot direction: head, body, shoulders, gloves, belt, trousers, boots, two rings, and one necklace/amulet slot. Necklace and amulet share one slot.
- Exact compatibility, requirements, final slot catalogue, and recalculation of derived values remain open. See [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md) and [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md).
- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.2): a worn piece stays in the bag and keeps its weight there; each armour piece gives its family's whole-set defence (10.2 §5b) by its share of that set's weight, so six pieces of one family are the whole set; a character with no weapon fights with fists, the dagger's numbers at half damage; the wooden shield blocks as the iron one does; and the off hand holds a shield or a Cartridge Pouch only, so no two weapons are held at once.

## 4.3 Durability and repair

- Durability applies to hand-slot equipment (right/left hand), armour worn in the armour set, and resource-gathering tools. Jewellery and belts have no durability.
- Partial wear does not progressively reduce item stats or effectiveness. At zero durability an item is **broken and unusable**; it remains in the world/inventory and does not disappear.
- A broken item can be repaired using a portion of the ordinary/basic ingredients required to create that item. Rare ingredients are not required for repair.
- A broken item cannot undergo Enchanting operations, except that one unlocked modifier may still be extracted from it. Extraction still destroys its source item and requires the specified ingredients.
- Exact wear rates, repair fraction, repair method/station, and repair duration remain open.
- Durability was off in the slice (owner-approved, 2026-11-03, [11 Vertical Slice](11%20Vertical%20Slice.md)). Owner's direction, 2026-11-11 (the play test of S5a): a failed gathering cycle wears the tool more than a good one, as [07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §9 says, and a failed lock breaks a pick. So in the slice the gathering tools wear and break; weapons and armour do not (working reading). Authoring defaults: a tool holds 100; a good or perfect cycle wears it 1 and a bad one 3; at 0 it is broken and stays in the bag as a broken tool that gathers nothing, until repair has its own design (§7).

# 5. Enchanting

Enchanting is a separate profession with six cumulative progression steps. Learning Enchanting grants Step 1. Further Enchanting skill thresholds unlock the next step in order; exact numeric thresholds remain open. Costs, ingredients, stations/requirements, and world-time durations are defined per operation in later balancing work.

## Step 1 — Add one random modifier

- Spend resources to add one random modifier selected from the fixed type weights for that item's base.
- The item's base stage determines tier weights; Enchanting skill may shift the tier outcome toward stronger tiers. Numeric value is random within the selected tier's range.
- The target must have an available slot in the randomly assigned group and remain within the 3+3 and six-total limits. When both groups have room, the group-selection distribution is open.
- Recalculate quality after addition; the displayed category changes only if the new count crosses a quality boundary. If no legal group slot is free, addition is impossible.
- The player does not choose the type or copy the result.

## Step 2 — Full reforge of all unlocked modifiers

- Every unlocked modifier is regenerated: type, tier, and numeric value. Locked modifiers remain unchanged.
- Total count and the major/minor group distribution are preserved. Each unlocked group position is rerolled within that same group.
- Type rolls use the fixed base weights. The base stage sets tier weights; Enchanting skill shifts its tier outcomes toward stronger tiers. No hard tier ceiling applies.
- There is no single-modifier reforge; lock other modifiers first to preserve them.

## Step 3 — Lock modifiers

- A chosen modifier can be locked so a full reforge preserves it. Locked modifiers cannot be extracted or changed by operations limited to unlocked modifiers.
- A lock is permanent and irreversible under the approved direction. Successive locks on one item become increasingly expensive; the exact cost and growth curve remain open.
- Whether tier improvement may target a locked modifier remains open.

## Step 4 — Extract and insert one modifier

**Extraction**
- Extract exactly one chosen unlocked modifier; the source item is destroyed. Extraction is allowed even if the source item is broken.
- Extraction requires specific ingredients. This creates one physical, non-copyable modifier instance, not a learned recipe.
- The instance preserves modifier identity, tier, and major/minor group, but not numeric value. Locked modifiers cannot be extracted.

**Insertion**
- The instance can be inserted once into another item and requires specific ingredients. It is consumed after successful insertion and cannot be copied or used again.
- Insertion has no base-compatibility check: any item base may receive the instance if the target has a free slot in that modifier's own group and remains within 3 major, 3 minor, and 6 total. If the matching group has no free slot, insertion is impossible.
- A broken item cannot be the insertion target. Insertion rolls a new numeric value within the retained tier's range. The inserted modifier becomes locked and cannot later be extracted.
- Ingredient sets and costs, duration, and persistence through Character Life changes remain open.

## Step 5 — Raise one modifier's tier

- The Enchanter can raise the tier of one chosen modifier; its type and group remain the same.
- Tier 0 is the strongest tier under the current 9-to-0 numbering. Whether this operation advances exactly one step, whether it can target a locked modifier, and its ingredients/cost/time remain open.

## Step 6 — Change numeric values

- This operation rerolls the numeric value of **every unlocked modifier** on the item. It preserves each modifier's identity, group, and tier; values are rerolled only within their existing tier ranges.
- Locked modifiers remain unchanged. The operation cannot change modifier identity or tier.

There is no separate delete or replace operation. Full reforge is the only current operation that changes modifier identity, affecting every unlocked modifier while preserving count and group distribution.

# 6. Acquisition, loot, and character life

- Ordinary monsters yield ingredients only; they do not drop finished equipment or other ordinary items.
- Whether transformed NPCs can exist as monsters is undecided. If they exist, they may drop items; their exact item-drop rules remain open.
- On NPC death, each piece of the NPC's current equipment is independently checked for recovery. Base recovery chance is 35%; apply the shared Luck multiplier from [07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md): `p = min(0.99, 0.35 × (1 + Luck / 100))`. Perception does not affect this roll. At Luck 1 the chance is about 35.35%; at Luck 100 it is 70%. Any number of items from none to all may be recovered; there is no fixed half, forced rounding, or all-or-nothing roll.
- The same per-item rule applies if a former player character exists as a world actor and is defeated: its current equipment may be recovered using the current looter's Luck. A new Character Life does not automatically inherit the previous character's carried or equipped inventory or extracted modifier instances.
- Chests and other placed world containers may contain equipment, notes, ingredients, materials, money, and other appropriate items. Their rarity and loot tables remain open; combat must not create a completion chest solely because a fight ended.
- A Legacy Cache is a separate intentional storage/inheritance system. This inventory rule does not resolve which item types, especially extracted modifier instances, may be placed in or inherited from it; see [07 Character Life](07%20Character%20Life.md) and §7.

## 6.1 Shops and Masters

Owner's direction, 2026-11-02. Numbers are authoring defaults checked by `tools/balance/item_sim.py`, and readings marked as working are not owner decisions.

- Every city has sellers of equipment, potions, First Aid supplies, ingredients, and crafting resources. There are no separate armourers: a shop is a shop, allied and cult fighters buy in the same ones, and where a fighter's items came from needs no other explanation. Since the owner's follow-up of 2026-11-13 the fighters' gear also has its own source: see *Supply* below.
- A shop is kept by a **Master** of its trade, who also teaches that profession. The capital's Masters are stronger and stock more. The Major Faction of industry and artifact research has a more advanced Master, with a shop of their own.
- **Stock** is made as any item is made (§3), at the **Shop Skill** of its Master, with no Perception or Luck. Quality therefore needs no table of its own, and a Legendary is always far rarer than anything else at the start:

| Shop Skill | Who | Normal | Magic | Rare | Epic | Legendary |
|---:|---|---:|---:|---:|---:|---:|
| 26 | City shop, first day | 17.6 % | 65.2 % | 13.3 % | 3.4 % | 0.5 % |
| 36 | City shop, day 10 | 10.6 % | 61.6 % | 19.8 % | 6.7 % | 1.3 % |
| 46 | City shop, day 20 | 6.1 % | 54.0 % | 25.6 % | 11.4 % | 3.0 % |
| 50 | Capital's Master, first day | 4.8 % | 50.3 % | 27.4 % | 13.6 % | 4.0 % |
| 70 | Capital's Master, day 20 | 1.3 % | 29.8 % | 31.1 % | 25.1 % | 12.7 % |
| 75 | Faction's Master, first day | 0.9 % | 24.9 % | 30.4 % | 27.7 % | 16.2 % |
| 95 | Faction's Master, day 20 | 0.1 % | 9.6 % | 21.7 % | 32.9 % | 35.7 % |

- **World Age.** The longer a world lives, the better its shelves. Shop Skill rises by 1 a game day (authoring default, 2026-11-03). Higher stages also reach the shelves as the world ages; the pace is open (CC-48). Because both sides buy from the same shelves, this barely tilts a fight: every shop +20 moved the slice by 2 points with Outfit V0 ([10.2](10.2%20Combat%20System%20Draft.md) §5b).
- **Supply** (owner, 2026-11-13, the follow-up to COMBAT-7 in [99](99%20Open%20Questions.md)): the cult lags by slightly lower base attributes, but the lion's share is items. The allies have the better items, since by the lore one of their factions, the Major Faction of industry and artifact research, makes them, and those items widen the gap of the attributes further. The cultists are poorly supplied, but there are more of them. **Outfit V1** ([10.2](10.2%20Combat%20System%20Draft.md) §5f *The balance after the answers*; authoring defaults): an allied fighter's set is made at Shop Skill 26 at Limit 9, 50 at Limit 8 and 75, the faction's Master's, at Limit 7; a cult follower's at 10, makeshift, at Limit 9, 36 at Limit 8 and 50 at Limit 7. The shops stay open to everyone, and the player buys in them. Working readings: both sides' supply rises with World Age by the same points as the shelves; "widen the gap of the attributes" is read as the catalogue's modifiers, not items that add to attributes (COMBAT-14).
- Ingredients for sale depend on which monsters prevail around the city, and crafting resources on the trees, metal veins, and other sources around it, so cities differ. The capital is a first-level city, so beginner resources prevail around it, and farther cities differ by the difficulty of their zones. Territories must be balanced by stage and difficulty, which the slice does not need (CC-48).
- **Crafting has no ceiling,** by the owner's decision of 2026-11-02. A player who wants to craft may craft without limit. The price is the time and effort it takes: find resources, find Masters, guess that gathering exists, earn the first pickaxe or axe, walk there and back, and fight the monsters that live there.
- The Equipment Quality slider of [02](02%20World%20Laws.md) sets the base Shop Skill, and the Economy slider and Faction Influence set prices, stock, and access, as 02 requires (working reading).

## 6.2 Faction Gift

Owner's direction, 2026-11-02: a character who joins a faction may receive a **Faction Gift**, an item or several to choose from, weapons or armour, random and set by the World Seed; while melee is withheld ([10.2](10.2%20Combat%20System%20Draft.md) §5d) the weapons are firearms only (owner-confirmed reading, 2026-11-13). It is Magic or Rare, and Epic in very rare cases.

Authoring defaults, 2026-11-03: the offer comes every time a character joins, and it is three items of which the character takes one, at Magic 70 %, Rare 28 %, Epic 2 %. In the slice one Gift adds about 1 point to a Limit 9 hero, so it is a welcome, not a set.

*In the slice* (S5c.4, 2026-11-13; [SLICE_TRIAL.md](unity/Assets/Plans/SLICE_TRIAL.md) §1.6): the three are different bases at stage 9: the pistol, the rifle, or one of the 24 armour pieces. They are drawn from the World Seed's own stream, so a load offers the same three. Magic has one or two modifiers at even odds, Rare three, and Epic four. A firearm comes with a plain Cartridge Pouch, without which it cannot shoot. The agent's readings are in [99](99%20Open%20Questions.md) VS-TRIAL.

## 6.3 Money, prices, and wages

**Money** (owner's decision, 2026-11-05). There are three coins: bronze, silver, and gold. 100 bronze make 1 silver, and 100 silver make 1 gold. Every price, wage, purse, and fine is one whole number of bronze, and the interface shows it in coins: the bronze below 100, the silver below 100, and the gold above. So 375 bronze shows as 3 silver 75 bronze, and 25 000 bronze as 2 gold 50 silver. Authoring default: a coin whose count is zero is not shown. Coins weigh nothing (§3).

**Recalculation** (agent, 2026-11-05, authoring default). Every earlier price in gold became the same number of silver, that is ×100 in bronze, so no balance changes. Silver is therefore the everyday coin. Bronze comes from purses, from what a shop pays (a quarter of its price), and from small goods, and gold only from the most valuable things.

Authoring defaults set by the agent on 2026-11-03 (CC-48), at the middle Economy setting, which may later scale them. The unit is bronze, and the anchor is a day's plain work in a city: about 1 000 bronze (10 silver).

**Equipment**, any base, by quality, in bronze:

| Normal | Magic | Rare | Epic | Legendary |
|---:|---:|---:|---:|---:|
| 500 | 1 500 | 4 000 | 10 000 | 25 000 |

A city set of 11 pieces at Shop Skill 26 costs about 23 000 bronze (2 gold 30 silver).

**Everything else:**

| Goods or service | Bronze |
|---|---:|
| Herb, ordinary ingredient from a beast, wood | 100 |
| Ore, leather | 200–300 |
| Pickaxe or axe | 600–800 |
| Bandage / splint / antidote | 200 / 400 / 600 |
| Magical ingredient for the Limit 9 potion, from the Master of Enchanting | 2 000 |
| Magical ingredient for the Limit 8 potion, the same Master's price | 15 000 |
| A lesson that teaches a profession from its Master | 1 000 |
| A tavern room for a night | 300 |
| Harrow's alchemy bench, one brewing | 100 |
| Veinmarket's den, one brewing ([20](20%20Alchemy%20and%20Potioncraft.md) §3) | 300 |
| A seer's prediction | 3 000 |

**What money comes from:**

| Source | Bronze |
|---|---:|
| Starting purse | 500 |
| Notice Board task, an hour or two of work | 500–1 500 |
| A Master's order, often for something gathered or made | 1 000–2 500 |
| Faction task, for members | 2 000–4 000 |
| A townsman's / a guard's / a merchant's purse, by theft | 100–600 / 300–800 / 1 000–3 000 |
| A poor / middling / rich dwelling's Stash, by breaking in | 100–400 / 300–1 000 / 1 000–3 000 |
| A Hidden Cache, found by Perception ([11](11%20Vertical%20Slice.md)) | 100–1 500, or an item of that worth |
| A glowing beast's magical ingredient, sold to the Master of Enchanting | 4 000 |

A purse holds any whole number of bronze in its range.

**Stash.** Owner-approved, 2026-11-06: a dwelling broken into ([10.3](10.3%20Stealth%20and%20Detection.md) §6.1) may hold one Stash, a chest or a hidden box, and what it holds reads from who lives there, never from a lottery. It holds money, or an item of the same shop value that fits the household: a Master's home holds the materials of the craft, a Warden's a weapon. Authoring defaults, checked by `tools/balance/burglary_sim.py`: a Stash is there in half the poor homes, 60 % of the middling ones and 70 % of the rich ones; one Stash in seven also holds papers, a letter or a note of debt, which are Information Sources and may give an Argument ([09](09%20Information%20Sources.md), [10.4](10.4%20Dialogue%20and%20Social%20Checks.md) §1). Nothing fills again during a run. The Ridge is rich, the High Street, the terraces and the shops are middling, and Lower Veil, the docks and the tenements are poor.

- *In the slice (S5c.5)*, working readings built as authoring defaults ([99](99%20Open%20Questions.md) VS-BREAKIN (f), (i)): E takes the Stash whole in one tick. It holds bronze in its wealth's range, drawn by the seed for that dwelling; a named household with a trade keeps its goods instead one time in two, as much as the bronze would buy (ore, bandages, lockpicks or herbs). The papers are one of three notes shown on the screen and give no Argument in the slice. What a night's Stashes gave is counted, so an Arrest's Deception can ask whether stolen goods are on the character and a Surrender can give them back ([10.4](10.4%20Dialogue%20and%20Social%20Checks.md) §6 *Arrest*). A Warden carries a guard's purse, 300–800, and the merchants of the slice a merchant's, 1 000–3 000; every other named person a townsman's.

**Selling.** A shop pays a quarter of its own price. Selling is possible, but buying and selling back never pays, and crafting for sale is slow money.

**The budget of a run.** A player who works through two days earns about 10 000 bronze (1 gold). That buys the Limit 9 ingredient, lessons, First Aid supplies, and two or three Magic pieces, or one Rare weapon, but not a whole set. So a set comes from mixing the routes of 11: buying, crafting, tasks, and theft. The Limit 8 ingredient at 15 000 bronze is within reach only for a player who saves and steals; the glowing beast costs a trip instead.

**Stock.** A shop holds one to three of each thing it sells. Each morning it fills again to that count, so the Shop Skill of that day makes the new pieces. Working reading for the full game: buyers share one shelf, so what the player buys is missing for others until morning. The slice's fighters bring their own gear and do not shop during the run.

- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.3): each morning a shelf is made anew from the World Seed, one to three of each good and one piece of each equipment base; a shop buys what it sells, the trader any piece not worn or held; what the character sells goes on that shelf until morning; the stalls are open 08:00–20:00.

## 6.4 Gathering and Skinning

Owner's rules, 2026-11-04:

- Anyone may gather herbs, wood, and ore; no lesson is needed.
- Gathering repeats cycle after cycle, with its animation, until the player cancels it or the node runs out.
- Skinning a beast needs a lesson from a Master.

Owner's decisions, 2026-11-05: the slice's only ore is iron ore. A node that runs out is removed from the world at once, with no stump, spent rock, or other trace; a herb or flax patch appears again where it was when it comes back.

Authoring defaults the same day:

| Node | Units | Tool | Skill / attribute | D | Comes back |
|---|---:|---|---|---:|---|
| Herb patch | 3 | hands | Herbalism / Perception | 1 | 24 hours after it was emptied |
| Flax patch | 3 | hands | Herbalism / Dexterity | 1 | 24 hours after it was emptied |
| Tree | 6 | axe | Woodcutting / Strength | 2 | not within a run |
| Ore vein | 8 | pickaxe | Mining / Strength | 3 | not within a run |

- A cycle takes 1 tick. Owner's direction, 2026-11-11 (the play test of S5a): each cycle is a Reaction Check ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §8a) on the node's skill and attribute against its D: a perfect result gives 2 units, a good one 1, and a bad one none and wears the tool three times as much (§4.3). Until then it was an action check of 07.1 §4 with the same three outcomes.
- **By animation.** Owner's second direction, 2026-11-11: a cycle lasts as long as its animation, not a tick, and its Reaction Check comes at the animation's key frame with the world paused (07.1 §8a): one check a swing at a tree or a vein. A herb or flax patch is gathered in one go, one check that empties it: a good result gives its 3 units, a perfect one 4, and a bad one none. For patches this replaces the owner's rule of 2026-11-04 that gathering repeats cycle after cycle. Each check costs 3 % of maximum Stamina, the 4 units and the 3 % being authoring defaults.
- Each cycle costs 3 % of the character's maximum Stamina. Gathering stops at 0 Stamina.
- Moving, opening the bag, being noticed by a beast, or being attacked cancels it.
- **Skinning** takes 3 ticks and is a check on (Dexterity + Skinning) / 2 against D 2. A wolf gives 1 leather and a boar 2; a failure gives half, rounded down. The lesson costs 1 000 bronze. Since the owner's second direction of 2026-11-11 the check is a Reaction Check at the knife's cut (07.1 §8a), after the 3 ticks.
- Owner's decisions, 2026-11-04: the Master of Item Creation teaches Skinning, and taking a beast's other parts, its ingredients, needs no lesson.
- **No gathering Masters.** Owner's decision, 2026-11-04: Herbalism, Woodcutting, and Mining have no Masters, because gathering needs no lesson. Tools and common ingredients are sold by the Masters of the crafting professions, Item Creation and Enchanting, and by the Master of First Aid. Who sells what, owner-approved the same day: the Master of Item Creation sells axes, pickaxes, ore, wood, leather, and flax; the Master of Enchanting sells magical ingredients (§6.3); the Master of First Aid sells herbs and ordinary beast ingredients beside its supplies.
- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.5): a cycle costs 3 % at any load while [07.2](07.2%20Derived%20Character%20Statistics.md) §3.2 leaves the load bands' multipliers open; the player cancels with E again; a beast's warning counts as being noticed; a bag that can carry no more ends gathering with the unit left in the node; the slice's flax grows in Crooked Hamlet's beds. The gathering animation is not yet built.
- The slice's working readings, nothing decided (`VERTICAL_SLICE_IMPLEMENTATION_PLAN.md`, S5a.6): a critical skinning gives the beast's whole hide, no more; a failed one takes the hide off for good, spoiled by half; taking a beast's ingredients takes no time; a glowing beast's magical ingredient is the bright glow essence of §3 and §6.3.

# 7. Open design questions

1. Confirm whether base-favored modifier types are independent of the major/minor group (this draft treats them independently). What is the final base catalogue and its fixed favored-type weights? What are the exact item-stage/tier weights, numeric ranges, and duplicate constraints? §2.4 holds working values only.
2. What probability distribution assigns major/minor groups among valid 3+3 splits? How does random Enchanting addition choose among groups that have free slots?
3. Which Enchanting skill levels unlock Steps 2–6 after learning unlocks Step 1?
4. What costs, ingredient lists, stations/requirements, and world-time durations apply to each Enchanting operation?
5. Does tier raising advance exactly one tier, and may it target a locked modifier?
6. What are the exact durability loss rates, repair method/station, fraction of ordinary crafting ingredients consumed, and repair duration?
7. What is the home-stash capacity and storage/weight policy? What are the final item weights for every base, tool, material, and consumable?
8. What are the final equipment compatibility rules, requirements, and complete slot catalogue?
9. Which item types may be placed in or inherited from a Legacy Cache? Can an extracted modifier instance be stored there and survive into another Character Life?
10. Will transformed NPCs become monsters, and what equipment/items may they drop? What are the rules for non-equipment carried goods and other non-combat item acquisition?
11. What exact colour values, icons, and accessible presentation accompany the agreed quality colour categories?
12. Resolved, 2026-11-04 (owner): the Master of Item Creation teaches Skinning (§6.4).
13. Resolved, 2026-11-04 (owner): a beast's ingredients need no lesson (§6.4).
14. Resolved, 2026-11-04 (owner): there are no gathering Masters; tools and common ingredients are sold by the Masters of Item Creation, Enchanting, and First Aid (§6.4).

Starting modifier-count probabilities, Item Creation stage gates, initial weight values/carry thresholds, NPC equipment-recovery formula, quality bands/colour directions, insertion capacity and compatibility, numeric-value operation scope, and durability categories/repair ingredient type are defined in this draft. Their initial balance values may be tuned through later playtesting.

---

This is an owner-approved design direction with assistant-selected starting balance values under the owner's delegation. It is not a complete content catalogue or implementation specification.
