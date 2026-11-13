# 20 Alchemy and Potioncraft

Version: 0.14

Status: Draft — owner-approved foundation; skill progression, mini-game, recipes, and balance remain open

Last Updated: 2026-11-13 (§3 *In the slice*: drinking built in S5b.5, read in 05.1 *In the slice*. Earlier: §3 *In the slice*: brewing built in S5b.4: the three benches, the formulas as notes, a potion carried one by one with its brewing's results and the Brewing Factor read from them when it is drunk; the agent's readings in 99 VS-BREW. Earlier: §3: by the owner's answers to S5-2: the checks run with the world paused and 6 ticks pass after them; their difficulty grows with the potion's Limit and stays passable at Limit 0; three places to brew; drinking a Limit potion warns of an hour defenceless, then 60 ticks of Assimilation, Stunned if attacked; the numbers authoring defaults. Earlier: §3: brewing's bar is brought to 07.1 §8a's rule in S5b, by the owner's direction of 2026-11-11. Earlier: the price in bronze, by 19 §6.3. Earlier: an interrupted brewing loses its ordinary ingredients and keeps the magical one, authoring default. Earlier: the brewing mini-game is a Reaction Check once a tick, and its results change the potion's Sanity cost and attribute rise, by the owner. Earlier: the potion shop's Master teaches Alchemy as a working reading. Earlier: the Alchemical Flask rejected by the owner. Earlier: the Alchemical Flask proposal noted. Earlier: the V1 question follows the slice's Limits. Earlier: terms renamed to Arcana and Limit. Earlier: potion-making included in the Vertical Slice.)

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

- A brewing plays its animation and has six Reaction Checks. *Owner, 2026-11-13 (S5-2), replacing "once a tick, every ten real seconds, with the world running":* the checks run with the world paused, each at its stroke's key frame, as every Reaction Check but theft's does ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §8a); after the sixth, 6 ticks pass with the world running, the brewing's time, for a Limit potion and for Основа ритуала's substitute alike.
- Six good results give the standard potion: the Limit's standard Sanity cost and its standard attribute rise.
- Perfect results lower the Sanity cost, and they add one or two points to the Arcana's primary attributes above the standard rise.
- A bad result still gives a potion, but each one raises the Sanity cost. Six bad results take a great deal of Sanity and give almost none of the attribute rise.
- The correct potion still grants its Limit: brewing changes only its price and its rise ([05 Progression](05%20Progression.md)).

Authoring defaults: each perfect result lowers the Limit's mandatory Sanity cost by 10 % and each bad one raises it by 25 %, so six perfect results take 40 % of it and six bad ones 250 %. Three perfect results add 1 to both primary attributes and six add 2. Each bad result takes 15 % of the attribute rise away, so six leave a tenth of it. The old fixed zones (a quarter of the bar, a perfect zone of 6 %, widened by Alchemy to 40 % and 10 %) are replaced by the rule below. Owner's direction, 2026-11-11: every Reaction Check's zone and marker pace follow attribute and skill, with a widest zone and a slowest marker ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §8a); brewing follows it since S5b.

**Difficulty by Limit** (owner, 2026-11-13: the deeper the potion's Limit, the harder its checks, and even a Limit 0 potion stays passable, not necessarily perfect or all good, but never out of reach). Authoring defaults, checked in slice iteration S5b.4: a brewing check is Intelligence and Alchemy against the potion's difficulty on 07.1 §8a's rule, `Δ = (Intelligence + Alchemy) / 2 − D`, with the perfect zone a quarter of the good zone, in its middle. D by the potion's Limit:

| Limit | 9 | 8 | 7 | 6 | 5 | 4 | 3 | 2 | 1 | 0 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| D | 1 | 3 | 6 | 10 | 15 | 21 | 28 | 36 | 45 | 55 |

Brewing keeps its own floor so that the hardest check stays passable: its good zone is never narrower than 12 % of the bar and its marker never faster than 1.0 s across it, so the shortest good window is 0.12 s, applied after every narrowing by Fatigue and Sanity. The perfect window may fall to 0.03 s at the hardest, a feat rather than a norm. Each check is a use of Alchemy for growth ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §3), at the stage of the potion's Limit.

**Interruption.** Since the owner's answer of 2026-11-13 the checks run with the world paused, so nothing interrupts them, and the potion is made with the sixth. A fight that begins in the 6 ticks after stops the time passing, and the potion is kept (authoring default, 2026-11-13). This replaces the default of 2026-11-04, under which an interrupted brewing lost its ordinary ingredients and kept the magical one.

**Where brewing is done** (owner, 2026-11-13). Every bench needs the Alchemy lesson.

- **Harrow's apothecary:** the alchemist's bench, for a fee, during the shop's hours, as Item Creation's anvil is in its Master's yard; 100 bronze a brewing (authoring default).
- **The faction's house:** a bench for its members, free.
- **Veinmarket's den** in Lower Veil: half-lawful, found by word of mouth, open at night, at a higher fee, 300 bronze a brewing (authoring default), and closed to a character who went against Veinmarket in POI B (authoring default).

**In the slice** (S5b.4, 2026-11-13; [99](99%20Open%20Questions.md) VS-BREW). Harrow teaches Alchemy and sells nothing. His bench is the lab bench in the apothecary's laboratory, open while his shop is; the faction's is the reading table of its house's archive, the house having one storey, 07:00–23:00; the den's is the office desk of the warehouse by Tipsy Pier, 22:00–05:00 (authoring defaults, [SLICE_TRIAL.md](unity/Assets/Plans/SLICE_TRIAL.md) §4). E within 1.2 m of a bench's edge opens its page of the formulas carried, and a click brews: the character works the bench with Quaternius' Interact, six strokes, each check at the stroke's key frame with the world paused, and the potion is made with the sixth before its 6 ticks pass. Between two checks the clip runs with the world going, and a fight that begins then breaks the brewing off with nothing spent. A formula is a note of weight 0 that stays in the bag. A potion is carried one by one and keeps its brewing's perfect and bad results; the **Brewing Factor**, `1 − 0.10 × perfect + 0.25 × bad` on the mandatory Sanity cost, the perfect results' bonus and the bad ones' share of the rise are read from them when it is drunk (S5b.5). Each check trains Alchemy at the potion's Limit, Intelligence its primary attribute ([07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §3). Drinking and Assimilation are built since S5b.5 ([05.1](05.1%20Arcana%20and%20Limits.md) *In the slice*).

**Drinking a Limit potion** (owner, 2026-11-13). The character is asked first, with the warning that it will be defenceless for an hour. Once it drinks, 60 ticks of **Assimilation** pass with the world running, as a sleep's ticks do ([18](18%20World%20Time%20and%20Living%20Simulation.md) §3), the character sitting where it drank. An attack in them, or a fight that takes it in, finds it **Stunned** ([10.2](10.2%20Combat%20System%20Draft.md)): it neither moves, attacks, uses abilities nor reacts, its evasion is zero, and everyone else chooses targets as against a stunned fighter, until the 60 ticks are spent, each round counting as a tick. Authoring defaults, 2026-11-13: the Limit, its attribute rise and its Sanity cost take effect when Assimilation ends; a Limit potion is never drunk in a fight; the Suspend Save is refused during Assimilation; and Assimilation is waking time for Fatigue ([07.2](07.2%20Derived%20Character%20Statistics.md) §3.3).

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
5. Resolved, 2026-11-03: Reaction Checks once a tick, which change the Sanity cost and attribute rise (§3). The rule of 2026-11-04 that an interrupted brewing loses its ordinary ingredients is retired: since the owner's answer of 2026-11-13 (S5-2) the six checks run with the world paused and nothing interrupts them, and 6 ticks pass after them (§3).
6. Resolved, 2026-11-03: the checks run within the six ticks, and the clock does not pause. Superseded by the owner, 2026-11-13 (S5-2): the checks pause the world, and the six ticks pass after them (§3).
7. Which potion categories beyond Arcana advancement exist, if any, and how do they respect the current prohibition on HP-restoring potions? The owner rejected a thrown Alchemical Flask on 2026-11-02: no consumable may change a battle's result that much (CC-46).
8. Resolved, 2026-11-03: the slice brews only Limit potions and the substitute base for Основа ритуала, and the formulas of Limits 9 and 8 come as [11 Vertical Slice](11%20Vertical%20Slice.md) describes. Limit 7 is for NPCs only.
9. What recipe costs, station requirements, ingredient quantities, and numeric balance values should the later balance pass propose? The slice's stations and fees are in §3 *Where brewing is done* (owner and authoring defaults, 2026-11-13).

---

This draft records only the owner-approved conceptual foundation. Open details must not be filled by implementation assumptions.
