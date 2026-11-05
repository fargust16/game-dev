# 20 Alchemy and Potioncraft

Version: 0.10

Status: Draft — owner-approved foundation; skill progression, mini-game, recipes, and balance remain open

Last Updated: 2026-11-05 (the price in bronze, by 19 §6.3. Earlier: an interrupted brewing loses its ordinary ingredients and keeps the magical one, authoring default. Earlier: the brewing mini-game is a Reaction Check once a tick, and its results change the potion's Sanity cost and attribute rise, by the owner. Earlier: the potion shop's Master teaches Alchemy as a working reading. Earlier: the Alchemical Flask rejected by the owner. Earlier: the Alchemical Flask proposal noted. Earlier: the V1 question follows the slice's Limits. Earlier: terms renamed to Arcana and Limit. Earlier: potion-making included in the Vertical Slice.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [05 Progression](05%20Progression.md), [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[02 World Laws](02%20World%20Laws.md), [05 Progression](05%20Progression.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [AGENTS.md](AGENTS.md), [README](README.md)

---

# Purpose and authority

This document records the approved conceptual foundation for Alchemy. It does not set numeric balance values or authorise implementation. The exact formulas, recipe values, ingredient quantities, and success curves are reserved for a later balance pass after the gameplay rules are complete.

# 1. Profession scope

Alchemy is a distinct profession whose domain is potions. It does not create equipment or alter item modifiers; those domains belong to Item Creation and Enchanting.

A character must learn Alchemy before advancing to a higher Limit. Every Limit transition requires the correct potion, prepared through Alchemy. A correct potion still grants its target Limit deterministically under [05 Progression](05%20Progression.md); the brewing mini-game is not a second, undocumented transition roll.

The currently approved Alchemy capabilities are:

- discover and decode potion formulas;
- identify alchemical ingredients;
- search for and gather ingredients;
- brew potions.

These are capability areas, not a settled list of separate skill unlocks or action checks.

# 2. Formula and ingredient discovery

Each advancement potion requires its formula and ingredients. Concrete formulas and ingredient requirements vary by World Seed, following the discovery principles in [05 Progression](05%20Progression.md). Player Knowledge may preserve methods for recognising and discovering them, not guarantee an identical recipe in every world.

Alchemy discovery should teach the player to read the world and its materials. The exact information sources, identification rules, discovery actions, and how the alchemical network supports or restricts access remain open.

# 3. Brewing

Brewing a potion takes **six world-time ticks** and includes a mini-game. Each world-time tick is one in-game minute under [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md).

The six-tick duration and the presence of a mini-game are approved.

Owner-approved, 2026-11-03: the mini-game is a series of Reaction Checks. A **Reaction Check** is a timing check: a marker runs once along a bar, the bar has a large good zone and a smaller perfect zone inside it, and the player presses Space. Stopping in the perfect zone is a perfect result, in the good zone a good one, anywhere else or not at all a bad one.

- A brewing plays its animation, and once a tick, every ten real seconds, a Reaction Check appears, so a six-tick brewing has six. The world does not pause.
- Six good results give the standard potion: the Limit's standard Sanity cost and its standard attribute rise.
- Perfect results lower the Sanity cost, and they add one or two points to the Arcana's primary attributes above the standard rise.
- A bad result still gives a potion, but each one raises the Sanity cost. Six bad results take a great deal of Sanity and give almost none of the attribute rise.
- The correct potion still grants its Limit: brewing changes only its price and its rise ([05 Progression](05%20Progression.md)).

Authoring defaults: each perfect result lowers the Limit's mandatory Sanity cost by 10 % and each bad one raises it by 25 %, so six perfect results take 40 % of it and six bad ones 250 %. Three perfect results add 1 to both primary attributes and six add 2. Each bad result takes 15 % of the attribute rise away, so six leave a tenth of it. The good zone is a quarter of the bar and the perfect zone 6 %, and the marker crosses the bar in 1.5 seconds; Alchemy skill widens both zones, to 40 % and 10 % at skill 100 (working reading).

**Interruption.** Authoring default, 2026-11-04: moving away, being attacked, or a fight starting interrupts a brewing. The ordinary ingredients are lost and the magical one is kept, so an interruption costs time and herbs but never the trip for a glowing beast or 15 000 bronze.

# 4. Boundaries

- Alchemy is required before Limit advancement; Item Creation and Enchanting cannot substitute for it.
- The only potion category confirmed by this foundation is the Arcana advancement potion. Other potion categories and effects are not yet specified.
- This document does not introduce HP-restoring potions or supersede current recovery rules.
- No recipe table, formula-decoding formula, ingredient-search probability, potion quality model, or numeric cost is established here.
- The Vertical Slice includes potion-making in a limited form ([11 Vertical Slice](11%20Vertical%20Slice.md)); the V1 player reaches Limits 9–8.

# 5. Open design questions

1. A shop's Master teaches its profession ([19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.1), so a potion shop's Master teaches Alchemy (working reading). Which other lawful and unlawful routes exist?
2. Must a character personally brew each advancement potion, or may they commission or obtain one after learning Alchemy?
3. How does Alchemy skill progress, and which attributes, skills, tools, stations, and conditions affect each capability?
4. How are formulas discovered and decoded, ingredients identified, and ingredient locations found?
5. Resolved, 2026-11-03: Reaction Checks once a tick, which change the Sanity cost and attribute rise (§3). Resolved, 2026-11-04 (authoring default): an interrupted brewing loses its ordinary ingredients and keeps the magical one (§3).
6. Resolved, 2026-11-03: the checks run within the six ticks, and the clock does not pause.
7. Which potion categories beyond Arcana advancement exist, if any, and how do they respect the current prohibition on HP-restoring potions? The owner rejected a thrown Alchemical Flask on 2026-11-02: no consumable may change a battle's result that much (CC-46).
8. Resolved, 2026-11-03: the slice brews only Limit potions and the substitute base for Основа ритуала, and the formulas of Limits 9 and 8 come as [11 Vertical Slice](11%20Vertical%20Slice.md) describes. Limit 7 is for NPCs only.
9. What recipe costs, station requirements, ingredient quantities, and numeric balance values should the later balance pass propose?

---

This draft records only the owner-approved conceptual foundation. Open details must not be filled by implementation assumptions.
