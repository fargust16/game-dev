# 19 Items, Crafting, Inventory, Equipment, and Enchanting

Version: 0.2
Status: Draft — owner-confirmed rules with delegated starting-balance formulas; content tables, operation costs, and selected edge cases remain open
Last Updated: 2026-10-31

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [02 World Laws](02%20World%20Laws.md), [05 Progression](05%20Progression.md), [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [README](README.md)

---

# Purpose and authority

This draft consolidates owner-approved direction for item bases, Item Creation, modifier generation, inventory, equipment, durability, loot, and Enchanting. It replaces conflicting older itemisation notes in [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md) and [99 Open Questions](99%20Open%20Questions.md) for current design work. It remains a Draft and does not authorise implementation.

# 1. Character foundation and item-related skills

- There is no global character level. Attributes and skills have separate progression tracks; ordinary skill progression is distinct from Pathway stages.
- The only confirmed item-crafting professions are **Item Creation** and **Enchanting**. Luck and Perception are attributes, not professions. No separate crafting profession exists for each material or equipment family.
- **Item Creation** creates equipment from bases and required resources. Its skill is the primary influence on modifier count; the starting probability formula is in §3.
- **Luck** and **Perception** affect modifier count as specified in §3. Neither changes a base's fixed modifier-type weights. Luck also follows the shared probability rule in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md); fixed weighted item tables are not rewritten by Luck.
- **Enchanting** performs the operations in §5. Learning the profession grants the first operation; later skill thresholds unlock the next operations in order. Enchanting skill shifts tier outcomes in its own random operations but does not change modifier-type weights. The exact unlock levels remain open.
- Attribute, skill, and shared probability rules are in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md).

# 2. Item bases, modifiers, and quality

## 2.1 Bases and stages

- Items are created from a defined base with foundational properties. Bases cover armour, weapons, rings, necklaces/amulets, belts, and other accessory slots.
- Armour families currently include cloth, leather, chain/chainmail, and plate/heavy metal. One general Item Creation profession serves every family.
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

# 4. Inventory, equipment, and durability

## 4.1 Inventory and weight

- Personal inventory is an unlimited bag: there is no item-count cap, grid capacity, or variable footprint. Every item has the same-size inventory entry/icon; weight is the only carrying limit.
- Every carried item has an assigned weight. Total carried weight includes equipped items and equals the sum of item weights; stack weight is the per-unit weight multiplied by quantity. Currency/gold weighs 0 and never changes the load.
- Starting weight balance (kg): ring 0.05; necklace/amulet 0.10; belt 0.25. Cloth armour: head 0.5, body 2, shoulders 0.8, gloves 0.4, trousers 1.2, boots 0.8. Leather armour: 1, 4, 1.5, 0.8, 2.5, 1.5 respectively. Chain armour: 1.8, 7.5, 3, 1.4, 4.5, 3 respectively. Plate/heavy-metal armour: 2.8, 11, 4.5, 2, 7, 4.5 respectively.
- Starting hand-item weights (kg): one-handed wooden weapon 1; one-handed metal/iron weapon 2; two-handed wooden weapon 2.5; two-handed metal/iron weapon 5; wooden shield 2.5; metal shield 4; bow 1.5; crossbow 3.
- These weights are initial balance values. Specific bases, tools, materials, consumables, and additional item families must receive explicit weights in their content definitions. Cloth should remain lighter than leather, leather lighter than chain, and plate/heavy metal heaviest; jewellery is nearly weightless.
- Stacking, equipment slots, and containers remain distinct from item footprints. A separate home stash is intended, but its capacity and whether it is weight-limited remain open.

## 4.2 Equipment

- The character has two universal hand slots. Compatible one-handed weapons, shields, talismans, books, and similar items may occupy either hand; a two-handed item uses both. A bow/crossbow occupies one hand while its arrows/bolts use the other as ammunition.
- Current body/accessory slot direction: head, body, shoulders, gloves, belt, trousers, boots, two rings, and one necklace/amulet slot. Necklace and amulet share one slot.
- Exact compatibility, requirements, final slot catalogue, and recalculation of derived values remain open. See [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md) and [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md).

## 4.3 Durability and repair

- Durability applies to hand-slot equipment (right/left hand), armour worn in the armour set, and resource-gathering tools. Jewellery and belts have no durability.
- Partial wear does not progressively reduce item stats or effectiveness. At zero durability an item is **broken and unusable**; it remains in the world/inventory and does not disappear.
- A broken item can be repaired using a portion of the ordinary/basic ingredients required to create that item. Rare ingredients are not required for repair.
- A broken item cannot undergo Enchanting operations, except that one unlocked modifier may still be extracted from it. Extraction still destroys its source item and requires the specified ingredients.
- Exact wear rates, repair fraction, repair method/station, and repair duration remain open.

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

# 7. Open design questions

1. Confirm whether base-favored modifier types are independent of the major/minor group (this draft treats them independently). What is the final base catalogue and its fixed favored-type weights? What are the exact item-stage/tier weights, numeric ranges, and duplicate constraints?
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

Starting modifier-count probabilities, Item Creation stage gates, initial weight values/carry thresholds, NPC equipment-recovery formula, quality bands/colour directions, insertion capacity and compatibility, numeric-value operation scope, and durability categories/repair ingredient type are defined in this draft. Their initial balance values may be tuned through later playtesting.

---

This is an owner-approved design direction with assistant-selected starting balance values under the owner's delegation. It is not a complete content catalogue or implementation specification.
