# 20 Alchemy and Potioncraft

Version: 0.1

Status: Draft — owner-approved foundation; skill progression, mini-game, recipes, and balance remain open

Last Updated: 2026-10-31

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [05 Progression](05%20Progression.md), [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)

Referenced By:
[02 World Laws](02%20World%20Laws.md), [05 Progression](05%20Progression.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md), [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md), [19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [90 Glossary](90%20Glossary.md), [99 Open Questions](99%20Open%20Questions.md), [AGENTS.md](AGENTS.md), [README](README.md)

---

# Purpose and authority

This document records the approved conceptual foundation for Alchemy. It does not set numeric balance values or authorise implementation. The exact formulas, recipe values, ingredient quantities, and success curves are reserved for a later balance pass after the gameplay rules are complete.

# 1. Profession scope

Alchemy is a distinct profession whose domain is potions. It does not create equipment or alter item modifiers; those domains belong to Item Creation and Enchanting.

A character must learn Alchemy before advancing to a higher Pathway stage. Every Pathway-stage transition requires the correct potion, prepared through Alchemy. A correct potion still grants its target stage deterministically under [05 Progression](05%20Progression.md); the brewing mini-game is not a second, undocumented transition roll.

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

The six-tick duration and the presence of a mini-game are approved. Whether the mini-game runs within those six ticks or adds time, whether player input pauses the shared clock, and what interruption or failure does to the ingredients and resulting potion are not yet defined.

# 4. Boundaries

- Alchemy is required before Pathway-stage advancement; Item Creation and Enchanting cannot substitute for it.
- The only potion category confirmed by this foundation is the Pathway advancement potion. Other potion categories and effects are not yet specified.
- This document does not introduce HP-restoring potions or supersede current recovery rules.
- No recipe table, formula-decoding formula, ingredient-search probability, potion quality model, or numeric cost is established here.
- Alchemy's inclusion in the Vertical Slice is not yet approved; V1 progression currently covers Stages 9–7.

# 5. Open design questions

1. What teaches or grants access to Alchemy, and which lawful and unlawful routes exist?
2. Must a character personally brew each advancement potion, or may they commission or obtain one after learning Alchemy?
3. How does Alchemy skill progress, and which attributes, skills, tools, stations, and conditions affect each capability?
4. How are formulas discovered and decoded, ingredients identified, and ingredient locations found?
5. What is the brewing mini-game, and how do its outcomes affect ingredient loss, potion validity, and any potion quality?
6. Does the mini-game occur within the six world ticks or extend the elapsed time? Does it pause, advance, or otherwise interact with the shared clock?
7. Which potion categories beyond Pathway advancement exist, if any, and how do they respect the current prohibition on HP-restoring potions?
8. Which parts of Alchemy are required in V1, including the 9→8 and 8→7 transitions?
9. What recipe costs, station requirements, ingredient quantities, and numeric balance values should the later balance pass propose?

---

This draft records only the owner-approved conceptual foundation. Open details must not be filled by implementation assumptions.
