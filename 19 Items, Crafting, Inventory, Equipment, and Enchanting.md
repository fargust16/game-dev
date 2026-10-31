# 19 Items, Crafting, Inventory, Equipment, and Enchanting

Version: 0.1
Status: Draft — owner-approved rules with unresolved tables, costs, and thresholds
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
- The only confirmed item-crafting professions are **Item Creation** and **Enchanting**. Separate tailoring, leatherworking, blacksmithing, jewellery-making, cooking, and alchemy are not current confirmed item-crafting professions.
- **Item Creation** creates equipment from bases and required resources. Its skill level is the primary character factor in the probability of modifier count.
- **Luck** and **Perception** contribute in addition to Item Creation skill to the probability of modifier count. They do not change a base's fixed modifier-type weights.
- **Enchanting** performs the operations in §5. Its skill progression unlocks those operations in sequence and influences tier outcomes of random Enchanting rolls, but does not change modifier-type weights.
- Exact skill thresholds, probability formulas, scaling, and caps remain open. The shared character progression rules are in [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md).

# 2. Item bases, modifiers, and quality

## 2.1 Bases and stages

- Items are created from a defined base with foundational properties. Bases cover armour, weapons, rings, necklaces/amulets, and other accessory slots.
- Armour families currently include cloth, leather, chain/chainmail, and plate/heavy metal. One general Item Creation profession serves every family.
- Item stages run from 9 through 0. A player may choose a lower-stage base when they provide its required materials; reaching a higher skill stage does not force crafting only the highest-stage item.
- Higher-stage bases require more varied and harder-to-obtain ordinary resources plus one rare ingredient of the corresponding stage. The rare ingredient is consumed. Any rare ingredient of the required stage is valid; its identity affects the generated name only, not base properties, modifier weights, or stats.
- A compact base catalogue is preferred. Initial count, final families, and visual-variant plan remain open; visual variants are not automatically separate mechanical bases.

## 2.2 Modifier count and quality

- An ordinary item has 0–6 modifiers, with no more than 3 **major** and 3 **minor** modifiers.
- Item Creation skill primarily determines the probability distribution for modifier count. Luck and Perception are added as secondary influences to that probability. Exact math is open.
- Major/minor assignment is random, independent of base, stage, skill, Luck, and Perception, subject to each group's three-slot maximum. Six modifiers must be 3+3; four may use any valid distribution. Exact probabilities among valid group combinations are open.
- Every base has fixed modifier-type weights for each applicable item stage. No attribute or skill, including Luck, Perception, Item Creation, and Enchanting, can change those type weights.
- Base and item stage determine the applicable modifier-tier table. Higher-stage items favour stronger tiers, but there is no hard stage-tier ceiling: even a stage-9 item can theoretically receive the strongest tier. Enchanting skill shifts Enchanting tier outcomes toward stronger tiers; it does not change type weights. Exact tables and curves remain open.
- Numerical values are randomly generated within the selected tier's range unless an operation changes the value. Exact ranges remain open.
- Quality follows total modifier count: **Normal** = 0; **Magic** = 1–2; **Rare** = 3; **Epic** = 4; **Legendary** = 5–6. **Unique** items are separately authored.
- Adding a modifier increases count and quality is recalculated from count. The displayed quality label changes only when the count crosses a band boundary under this mapping; whether every successful addition must raise the displayed label remains open.

## 2.3 Capacity

- No item may exceed 3 major, 3 minor, or 6 total modifiers. Any operation that adds a modifier must obey these limits.
- Inserting an extracted modifier requires a free slot in that modifier's own group. For example, a major modifier cannot be inserted where three major modifiers already exist, even if a minor slot remains open.
- If there is no free slot in the required group, insertion is impossible. How random addition chooses a legal group when both groups have capacity remains open; it must never overflow either group.

# 3. Item Creation

Item Creation combines a selected base with its stage-matched resources. Skill is the primary influence on modifier-count probability; Luck and Perception contribute to that probability. None of these factors alters the base's modifier-type weights, stage/tier table, or numeric ranges.

Modifier count determines quality. Major/minor membership is randomly assigned within the 3+3 limits. Modifier types are selected from the selected base's fixed weights; applicable tier and numeric value follow that base/stage rules. The precise generation sequence where group capacity and weighted selection interact remains to be specified.

# 4. Inventory, equipment, and durability

## 4.1 Inventory

- Ordinary inventory uses a grid with variable item footprints; a separate home stash/chest is intended. Currency is separate from the ordinary grid.
- Materials and stackable consumables stack, including potions, keys, lockpicks, bandages, and crafting ingredients. Ordinary equipment and accessories are individual, non-stacking items.
- Inventory weight is evaluated relative to carrying capacity; Strength is its primary source. [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md) governs derived values. Exact formulas, grid dimensions, stash capacity, and final footprints remain open.
- Illustrative footprints include one-handed weapons and one-handed staves: 3 cells; two-handed weapons and staves: 8; shoulders and trousers: 6; belts: 2; shields: 6. This is not a final table.

## 4.2 Equipment

- The character has two universal hand slots. Compatible one-handed weapons, shields, talismans, books, and similar items may occupy either hand; a two-handed item uses both. A bow/crossbow occupies one hand while its arrows/bolts use the other as ammunition.
- Current body/accessory slot direction: head, body, shoulders, gloves, belt, trousers, boots, two rings, and one necklace/amulet slot. Necklace and amulet share one slot.
- Exact compatibility, requirements, final slot catalogue, and recalculation of derived values remain open. See [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md) and [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md).

## 4.3 Durability

- Durability is reduced by use and damage, but partial wear does not progressively reduce item stats or effectiveness.
- At zero durability the item is **broken and unusable**. It does not disappear and can be repaired.
- Which categories use durability, wear rates, repair method, materials, cost, and timing remain open.

# 5. Enchanting

Enchanting is a separate profession with six cumulative progression steps. Learning the profession grants the first operation; each next skill step unlocks the next operation. Exact skill thresholds are open.

## Step 1 — Add one random modifier

- Spend resources to add one random modifier.
- Type is selected using the fixed weights for the item's base and stage. Enchanting skill influences tier outcome, not type weights; numeric value is random within the selected tier.
- The item must have free capacity within the 3+3 and six-total limits. Its major/minor group is assigned subject to legal capacity; the group-selection distribution is open.
- Count increases and quality is recalculated. The player does not choose the type or copy the result. Costs, resource list, duration, and no-capacity handling remain open.

## Step 2 — Full reforge of all unlocked modifiers

- Every unlocked modifier is regenerated: type, tier, and numeric value. Locked modifiers remain unchanged.
- Total count and the major/minor distribution are preserved. Each unlocked group position is rerolled within that same group.
- Type rolls use the fixed base/stage weights. Enchanting skill shifts tier probabilities toward stronger tiers; no hard tier cap applies.
- There is no single-modifier reforge; lock other modifiers first to preserve them. Costs, ingredients, and time remain open.

## Step 3 — Lock modifiers

- A chosen modifier can be locked so a full reforge preserves it. Locked modifiers cannot be extracted or changed by operations limited to unlocked modifiers.
- A lock is permanent and irreversible under the approved direction. Successive locks on one item become increasingly expensive; the exact cost and growth curve remain open.
- Whether tier improvement or other future operations may target a locked modifier remains open.

## Step 4 — Extract and insert one modifier

**Extraction**
- Extract exactly one chosen unlocked modifier; the source item is destroyed.
- Extraction requires specific ingredients. This creates one physical, non-copyable modifier instance, not a learned recipe.
- The instance preserves modifier identity, tier, and major/minor group, but not numeric value. Locked modifiers cannot be extracted.

**Insertion**
- The instance can be inserted once into another item and requires specific ingredients. It is consumed after successful insertion and cannot be copied or used again.
- The target must have a free slot in the same group and remain within 3 major, 3 minor, and 6 total. Without that group slot, insertion is impossible.
- Insertion rolls a new numeric value within the retained tier's range. The inserted modifier becomes locked and cannot later be extracted.
- Ingredient sets, costs, duration, cross-base compatibility, and persistence through character death remain open.

## Step 5 — Raise one modifier's tier

- The Enchanter can raise the tier of one chosen modifier; its type and group remain the same.
- Tier 0 is the strongest tier under the current 9-to-0 numbering. Whether this operation advances exactly one step, whether it can target a locked modifier, and its ingredients/cost/time remain open.

## Step 6 — Change modifier numerical values

- Change numerical values without replacing modifier identity. Locked modifiers remain unchanged; only unlocked modifiers are eligible.
- Whether this affects one chosen modifier or all unlocked modifiers, whether the tier is held, and costs, ingredients, and timing remain open. A value-only operation must not silently change type.

There is no separate delete or replace operation. Full reforge is the only current operation that changes modifier identity, affecting every unlocked modifier while preserving count and group distribution.

# 6. Acquisition, loot, and character life

- Ordinary monsters yield ingredients only; they do not drop finished equipment or other ordinary items.
- Whether transformed NPCs can exist as monsters is undecided. If they exist, they may drop items; their exact item-drop rules and relation to former equipment remain open.
- On an NPC's death, a randomly selected half of its **current equipment** remains recoverable. Other equipment is unavailable. Rounding for an odd number and selection method remain open. Non-equipment carried goods follow diegetic Confront rules unless separately specified.
- Chests and other placed world containers may contain equipment, notes, ingredients, materials, money, and other appropriate items. Their rarity and loot tables remain open; combat must not create a completion chest solely because a fight ended.
- Inventory belongs to that Character Life. Death/body loot/Legacy Cache rules are in [07 Character Life](07%20Character%20Life.md); the fate of an extracted modifier instance at death remains open.

# 7. Open design questions

1. What Item Creation skill levels/stages permit each base stage, and what is the modifier-count probability curve from 0 to 6?
2. How do Luck and Perception add to that curve, including scaling, caps, and diminishing returns?
3. What is the probability distribution among valid major/minor assignments? How does random Enchanting addition choose a legal group?
4. What are the item base catalogue, base/stage modifier-type weights, modifier value ranges, and compatibility rules?
5. How do base/stage and Enchanting skill combine in tier probabilities? Are duplicate modifier types allowed?
6. Which Enchanting skill level/stage unlocks each of the six operations?
7. What costs, ingredients, stations/requirements, and world-time duration apply to each operation?
8. Does tier raising move exactly one tier step, and can it target a locked modifier?
9. Does value-only change affect one selected modifier or all unlocked modifiers? Does it preserve tier and reroll only within that tier's range?
10. Must every added modifier raise the displayed rarity label, or does the existing count-band mapping remain authoritative?
11. Can a transferred modifier be inserted into any base if its group slot is free, or is there a base-compatibility restriction?
12. Can broken items be enchanted or used for extraction? Which categories use durability, and what repair costs/timing apply?
13. What happens to an extracted instance when its owner dies, stores it, or changes Character Life?
14. For NPCs with an odd number of equipped items, how is the recoverable half rounded? What happens to other carried items?
15. Will transformed NPC-monsters exist, and if so, what item drops may they yield?
16. What are final inventory dimensions, stash capacity, footprints, hand compatibility, equipment requirements, and the carry-capacity formula?
17. What acquisition rules apply to shops, quests, player death, and non-combat rewards beyond the NPC and ordinary-monster rules above?

---

This is an owner-approved design direction, not a complete catalogue, balance pass, or implementation specification.