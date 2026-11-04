# 11 Vertical Slice

Version: 0.53

Status: Draft

Last Updated: 2026-11-04 (S0.6a: code creates the slice's ground once as a flat level and never shapes it. Earlier: the Veilmouth map, C0, accepted by the owner: districts, interiors, and where the land's resources lie. Earlier: the slice is one hand-built scene assembled from a modular kit, with real assets from the start, by the owner's decision; the implementation plan is VERTICAL_SLICE_IMPLEMENTATION_PLAN.md. Earlier: no gathering Masters, by the owner: tools and common ingredients come from the Masters of Item Creation, Enchanting, and First Aid. Earlier: design gaps closed by the owner's answers: beast territories, gathering and Skinning, Item Creation from its Master, Sanity, Yield, the Suspend Save, townsfolk schedules, the start, and Ritual Site Candidates; numbers are authoring defaults. Earlier: slice audit: the invasion between 12:00 and 24:00 on day 2, the allies' 21–23 % in S6, the city's name, the Gift and the formula, open items. Earlier: First Aid numbers, authoring defaults. Earlier: prices and wages from 19 §6.3; Harrow teaches Alchemy, with his apprentice when he is lost, authoring defaults. Earlier: CC-42 and CC-45 for the slice: finding the traces, Character Notes, and the local fighter each POI fork can bring, authoring defaults. Earlier: the theft zone's numbers approved by the owner. Earlier: the theft zone's numbers, authoring defaults. Earlier: the times of POI A, B and C, authoring defaults. Earlier: the trial is the fork between the two Arcana; Conduct Progress for 9→8 and the potions' ingredients, authoring defaults. Earlier: staying awake brings Fatigue, by the owner. Earlier: the limited forms of the slice's mechanics, by the owner: initiation, the brewing Reaction Check, beasts outside the city, sleep, theft, tasks, and no durability. Earlier: with the requirement penalty growing with the shortfall the slice stands at 21 %. Earlier: Attribute Requirements move the slice from 24 % to 22 % on the same runs. Earlier: the owner accepts the slice at 23 %. Earlier: after the firearms pass of 10.2 the slice stands at 23 %, open. Earlier: after the Arcana pass of 05.1 and 10.2 the slice stands at 20 %, open. Earlier: the owner accepts the slice at 32 %. Earlier: after the point-blank pass of 10.2, with the rifle at 8, the slice stands at 32 %. Earlier: after the triangle pass of 10.2, with a weaker Infusion, the slice stands at 34 %. Earlier: with Дрёма at 2 AP, waking at 6 AP, and the kinds of weapon, the model's slice stands at about 35 %, which the owner accepts. Earlier: the model's slice falls to 24 % once an ally can wake a sleeper; retuning is open. Earlier: rifles replace crossbows and bows on the fighters of the slice, allies infuse, and crafting has no ceiling; numbers follow. Earlier: shops, Masters, money, First Aid, and theft enter the slice; numbers follow Wait, Defensive Stance, and critical hits at ×2. Earlier: the slice's numbers follow items made as 19 §3 makes them. Earlier: the Alchemical Flask rejected by the owner; the slice's numbers now rest on items. Earlier: group fights checked on the Combat Grid, with Target Rules V0, the cult's edge, and the proposed Alchemical Flask. Earlier: Faction Influence is read from the war, and a Side Event changes the war directly (owner-approved). Earlier: the slice is 40/60 in favour of the cult with no player, by the owner's decision; the allies' group changed. Earlier: traces of the Ritual Weak Points recorded. Earlier: the groups of the slice's fight recorded from the combat simulation. Earlier: the ritual has two Ritual Weak Points. Earlier: the slice's dialogue needs Arguments, and it keeps Reputation and Danger. Earlier: dialogue checks live in 10.4. Earlier: stealth rules live in 10.3. Earlier: the city named; slice Arcana on each side set; abilities live in 05.1. Earlier: allies may win without the player, which is a valid outcome. Earlier: slice forces set: 5 allies, 3 at the ritual, 5–7 in the main force; allied directive recorded. Earlier: allies fight the main force and leave the ritual to the player; Combat Grid forms around the player. Earlier: a ritual lasts one in-game hour. Earlier: combat runs in place across the city; the slice's two Arcana are chosen. Earlier: one or two Arcana with different styles, the first-day trial, and recipes outside the faction recorded. Earlier: balance and start settled: Stage 8 as the player's limit, 8–10 enemies, the six-hour window, a Factionless start, and three named Pathways. Earlier: rewritten for the owner's one-city slice: the Spirit General's invasion on day 1–2, a ritual the player stops or loses to, one joinable faction with two or three Pathways, and the required mechanics. Earlier: owner redefined the slice as one city at Stages 9–7; retired combat references replaced with 10.2.)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [00 Vision](00%20Vision.md), [00.1 Core Loop](00.1%20Core%20Loop.md), [00.2 Meta Loop](00.2%20Meta%20Loop.md), [00.5 Design Pillars](00.5%20Design%20Pillars.md), [01 Player Experience](01%20Player%20Experience.md), [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md), [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)

Referenced By:
[11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [11.2 Milestone S0 Greybox](11.2%20Milestone%20S0%20Greybox.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md), Future prototype milestones and first playable implementation planning.

---

# Purpose

This document defines the **Vertical Slice**: the first version of the game and the smallest playable piece that proves its fantasy.

It is not the full game.
It is a proof that the systems feel like this project, not like a generic RPG with stickers.

Success question:

> After one run, does the player say “I want to try another approach / another lead,” rather than “I need more XP”?

A run lasts one to two game days before the invasion plus the fight for the ritual, about 3–6 real hours.

# Slice Story

The owner approved this story on 2026-11-01.

- **Place:** one city, Veilmouth, not necessarily the capital, and a small area outside it, enough to gather resources, craft, and make potions.
- **Opening:** the player has one to two game days of ordinary life in the city.
- **Invasion:** after those days the Spirit General comes out in the city with its followers, and the ritual starts at once. Authoring default, 2026-11-03: a run starts at 08:00 on the first day, and the seed sets the invasion between 12:00 and 24:00 on the second day, so the first night's POI B never meets it. A seer can predict the hour for a price (19 §6.3).
- **The General leaves:** after 15–20 ticks it returns to the Spirit World and leaves the ritual to its followers. It cannot be hunted afterwards.
- **The final fight:** the main battle is against its followers.
- **Ending:** the player either stops the ritual and wins, or the ritual succeeds and the player loses. The character's death also ends the run, and the player starts again.

The slice uses the campaign's rules from [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md): one rule for how a ritual starts and stops, in a world of one city.

## The General in the slice

The preferred version lets a player who feels ready fight the General on a later run and see how strong it is, and even die to it.

If building a Limit 2 opponent is too much for the first version, the General appears, casts a spell without attacking, and leaves after the same 15–20 ticks. This fallback is approved.

## The fight

- 8–10 enemies take part: 3 stay at the ritual, and the other 5–7 go on towards the city centre. The ritual group is a Limit 8 senior and two Limit 9 conductors. A Limit 7 follower leads the main force, the rest are Limits 9–8, one Limit 9 follower has a rifle, and a cult fighter is a little stronger than an allied fighter of the same Limit (authoring defaults, [10.2](10.2%20Combat%20System%20Draft.md) §5b).
- 5 allied defenders meet that main force, two Limit 9 riflemen and three of Limit 8, and they fill their weapons with magic through Infusion. With no player the slice is about 40/60 in favour of the cult, by the owner's decision of 2026-11-02, and it has no easier or harder variant for now. With waking a sleeper, Дрёма at 2 AP, waking at 6 AP, and the kinds of weapon (owner, 2026-11-03), the model gave about 35 %, and the owner accepted this the same day as fair odds without the player. After the triangle pass of the same day the model gave 34 %, and after the point-blank pass, with the riflemen's rifle at base 8, it gave 32 %, which the owner accepted. After the Arcana pass of the same day, which evens out Жрица and Император, it gave 20 %, and after the firearms pass, with no separate Reload, 23 %, which the owner accepted ([10.2](10.2%20Combat%20System%20Draft.md) §5b). Attribute Requirements move it from 24 % to 21 % on the same runs, because one in five of the allied riflemen, those with Perception 2, has two thirds of the rifle's Perception 3 and hits two thirds as often. Fighters buy their gear in the same shops, and the allies' edge stays with their Limit 8 fighters, who buy from the capital's Master like the cult's Limit 7 follower ([10.2](10.2%20Combat%20System%20Draft.md) §5b). The allies alone stop the ritual in about 23 % of runs, and that is a valid outcome. A Limit 9 hero who fights beside them raises this to 34 % with no items, 39 % in their own work at skill 15, 43 % in a set from the city shop, and 49 % in a capital set, and a Limit 8 hero in a capital set to 60 % (authoring defaults).
- Allies can mark the ritual's approximate place on the map when asked. Their directive is the main force first, so only the player can choose to go for the ritual first.
- The ritual stands at one of three hand-built **Ritual Site Candidates**, picked by the seed's own key (owner's decision, 2026-11-04; [21](21%20Cult%20Campaign%20and%20City%20Control.md) §4). Working examples: a factory yard, a cellar under the Soot Stair, an old chapel.
- If the allies beat the main force in time, the ritual group retreats only if it judges it cannot win (21 §4).
- The city is the battlefield, and the ritual runs in one part of it. The Combat Grid forms around the player, and fights elsewhere are simulated. A combat round is one tick, so its 60-tick window is up to 60 rounds, or ten real minutes of free exploration. If the player stops it in time, the run is won. The player's Limits 9–8 are too low to undo a completed ritual, so here it means defeat.

## Stopping the ritual

- Kill everyone conducting it.
- Kill the senior who started it.
- Exploit one of its two Ritual Weak Points, while the cult has not fully taken the city.

The seed picks the two from the four kinds in [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md), so they differ from run to run. Each needs an Argument or a prepared item. The choice has its own key, so the city stays the same and the next run differs.

## Traces of the Weak Points

Owner-approved on 2026-11-02.

The traces were left by people who joined the General earlier. Every follower was once an ordinary person, and left family, debts, purchases, belongings, and signs behind. No hidden agent of the cult works in the city before the invasion. The traces lie in the past and look like ordinary city life, with no sign of what they are for.

- Each Weak Point has three sources of its Argument: two in the city before the invasion and one during it.
- Any one source opens the option. Each further one adds +1, under the rule of [10.4](10.4%20Dialogue%20and%20Social%20Checks.md).
- Only the run's two Weak Points leave traces. The slice has no false traces, though a trace can be misread.
- The traces use places the slice already has: the Notice Board, the Soot & Bell, Harrow's apothecary, the Soot Stair, and the factory fence. The seed sets the people, items, and places.
- When the invasion starts, witnesses hide and shops close. Items and signs stay where they are.
- A real Посланник does not appear in the slice. The group only waits for one, so the ritual gains no extra timer.
- Found items stay in the bag, and found facts go into **Character Notes**: short records with no hint of what they are for.

| Weak Point | In the city, first source | In the city, second source | During the invasion |
|---|---|---|---|
| Сомневающийся | A notice about a missing person, with the conductor's name and looks | Kin or a creditor at the tavern: whom the conductor fears for, and a letter | Eavesdropping at the circle, searching a dead follower, or Чужой сон |
| Посланник | A sign where those who left used to meet | A token among a missing person's belongings, for example in pawn | Eavesdropping on the word at the circle, or a token from a dead follower |
| Грязная тайна | A receipt or record of the senior's old affair | The same paper, sold by Veinmarket | Чужой сон on a sleeper, or searching a dead follower |
| Основа ритуала | Harrow remembers an odd buyer and what the buyer took | A note with the mixture on the Soot Stair | Seeing the base from Stealth Mode, then brewing a substitute in time: brewing takes 6 of the 60 ticks |

Основа ритуала has no roll, so its further sources only give another way to learn what to make. Its check is the stealth at the circle.

### Finding the traces

Authoring defaults set by the agent on 2026-11-03 (CC-42).

- A run has four traces in the city, two for each of its Weak Points, and never two in one place.
- The seed puts one on the trial's route, one at a place a POI of the first days touches (Harrow's apothecary, the Soot & Bell, or the Soot Stair), and two among the remaining places.
- Each trace takes one conversation, a search, a purchase, or a theft: at most 30 in-game minutes, five real minutes, besides walking. The five places lie close together, so all four fit into the first day, and a run of one to two days leaves room for them.
- A first-time player, who does not know what traces are for, finds about two: the one on the trial's route and one more by chance. A first run therefore usually opens one Weak Point with one Argument, 17 % on a Large Final Check, and shows that the option exists. An attentive player finds three or four. A returning player who knows what to look for finds all four on the first day and may reach 28 % or more.
- An outcome of the first days never closes a source; it moves it. If the Wardens hold Harrow, he can be heard in their lock-up through their favour, a bribe, or Convince. If Harrow is gone, his ledger stays in the sealed shop, reached by breaking in.
- **Character Notes** hold one line for each fact found, in the order found, in the character's own words. They never sort facts by purpose, mark one as important, link two facts, name a place still to visit, or set a task. They hold everything found and need no limit, because nothing in them says what to do.

A prepared Limit 8 character at value 1 against a Limit 9 conductor at value 0, on a Large Final Check ([10.4](10.4%20Dialogue%20and%20Social%20Checks.md) §7):

| Arguments | Chance |
|---:|---:|
| 1 | 17 % |
| 2 | 28 % |
| 3 | 42 % |
| 3, and Reputation or Danger +1 | 58 % |
| 3, Reputation, and Внушение | 72 % |

A planned kill of the senior gives 42 % with the best gear ([10.2](10.2%20Combat%20System%20Draft.md) §5b), so full preparation for dialogue ends up at least as good. A new character has 8 %, with value 0 and one Argument, and still sees that the option exists. That discovery is what the slice wants from a first run.

# Slice Fantasy

The slice takes place in one city, **Veilmouth**, at the mouth of the river Veil. **Lower Veil District** is one of its districts: a dense urban area at the edge of legality, medicine, factories, and night disappearances.

The player is the **first character in a new world**.

No final lore names are locked here. Placeholder faction labels are scaffolding only.

# Era and Mood

Confirmed tone reference: the industrial-occult city feeling associated with *Lord of the Mysteries* — not a copy of its plot, Arcana, or lore names.

The Vertical Slice should feel like a late **18th–19th century transition world**:

- nobility still matters: counts, barons, dukes, a king / crown presence in the background;
- factories and harmful cheap labour already shape the poor districts;
- clear separation between wealth and poverty in streets, clothes, lighting, and access;
- horse-drawn carriages remain common;
- early modern inventions appear as novelty or uneven progress: early bicycles, the first hints of radio/telegraph-like communication, early steam machines;
- gaslight / soot / fog / workshop smell energy over clean high-fantasy sparkle.

Lower Veil sits on the poor/industrial side of that divide, with richer order pressing down through Wardens and night commerce pressing up through Veinmarket.

Implementation rule: art, props, NPC dress, and Information Source wording must reinforce this era before any mystical reveal.

# Design Goals Proven by the Slice

The slice must prove all of the following:

1. Curiosity / Attention — notice a lead and decide whether to chase it.
2. Multiple Approaches — Confront is not the only profitable answer.
3. Timed POIs — missing a window changes the world.
4. Information Sources — board / bartender / rumour / trace beat omniscient markers.
5. Combat in place — Confront turns the surroundings into a Combat Grid and returns to free movement with residue ([16 Architecture Pivot](16%20Architecture%20Pivot.md)).
6. Scene split — overworld free 3P + at least one separate interior scene, with its own grid during Confront.
7. World residue — tomorrow looks different because of today.
8. Priced power tease — at least one Bargain-shaped offer or refusal.
9. The war's core — a General, its followers, and a ritual the player can stop in more than one way.
10. Preparation matters — what the player gathered, crafted, brewed, and learned in the first days changes the fight for the ritual.
11. Solo scope — one developer (with AI) can prototype it without a content army.

# Scope In

## Required mechanics

Each of these is implemented in a limited form:

- free exploration of the city and the area outside it;
- gathering;
- Item Creation;
- Alchemy;
- First Aid;
- shops, gold, and tasks that pay it;
- theft from a person ([10.3 Stealth and Detection](10.3%20Stealth%20and%20Detection.md));
- combat;
- dialogue with checks, which need Arguments, and Reputation and Danger ([10.4 Dialogue and Social Checks](10.4%20Dialogue%20and%20Social%20Checks.md));
- stealth ([10.3 Stealth and Detection](10.3%20Stealth%20and%20Detection.md));
- world time;
- sleep.

## Limited forms (owner-approved, 2026-11-03)

- **Initiation and Limit 8.**
  - **Жрица:** after the first-day trial the faction gives the formula of Limit 9 together with its Faction Gift, and Harrow teaches the Alchemy to brew it. The formula of Limit 8 also comes from the faction.
  - **Император:** the formula of Limit 9 lies among the belongings of someone who left to join the General, and finding it is the path taken alone. Like every advancement it is a Bargain (05); the slice's authored Bargain tease stays the black drop of POI C. The formula of Limit 8 lies in the same cache, because cultists, who alone could teach the next Limit, are not in the city before the invasion.
  - **The fork** (agent's decision, 2026-11-03): The first-day trial is an investigation of a disappearance: the missing person left to join the General. In that person's room lies a cache with the Император formulas of Limits 9 and 8. Handing it to the faction completes the trial, and the faction gives Жрица; keeping it is the path taken alone. A character takes one Arcana, so the trial is the slice's first Allegiance fork.
  - **From 9 to 8:** 8 points of deeds done in the Arcana's own way, then the second potion, whose magical ingredient comes from a glowing beast or costs a great deal of gold. The deeds and numbers are authoring defaults in [05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md).
- **Alchemy.** A **Reaction Check** is a timing check: a marker runs once along a bar, the bar has a large good zone and a smaller perfect zone inside it, and the player presses Space. Stopping in the perfect zone is a perfect result, in the good zone a good one, anywhere else or not at all a bad one ([20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md) §3). A brewing plays its animation, and once a tick, every ten real seconds, a Reaction Check appears, so a six-tick brewing has six. The world does not pause. Six good results give the standard potion: the Limit's standard Sanity cost and its standard attribute rise. Perfect results lower the Sanity cost, and they add one or two points to the Arcana's primary attributes above the standard rise. A bad result still gives a potion, but each one raises the Sanity cost. Six bad results take a great deal of Sanity and give almost none of the attribute rise. The slice brews only Limit potions and the substitute base for Основа ритуала.
- **Outside the city.** Wolves and boars live outside the city. Now and then one of them is supernatural and shows it with a glow, so the player knows to look in it for magical ingredients. Ordinary beasts drop ordinary ingredients only ([19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6). Owner-approved, 2026-11-04: beasts hold territories, warn before they attack, wolves go in packs of two or three, and a boar alone ([10.2](10.2%20Combat%20System%20Draft.md), *Beasts of the slice*). Authoring defaults: the territory holds 6 wolf packs and 4 boars, of which the seed makes 2–3 beasts glowing, each walking alone; nothing comes back during a run.
- **First Aid.** First Aid in the slice: a bandage stops Bleeding, a splint ends a fracture and Immobilisation, and an antidote ends Poison. It restores no HP. It costs AP in a fight and ticks outside one. Authoring defaults, 2026-11-03: a bandage or an antidote costs 3 AP or 1 tick and a splint 6 AP or 3 ticks, each a check on Dexterity and First Aid; Bleeding and Poison come only from beasts, and a broken leg from a fall of 4 m or more ([10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md)).
- **Sleep.** The character sleeps in a tavern room for money, or in the street, where it can be robbed. Sleep moves time on to morning while the world goes on, restores Will Power fully and a little Sanity, and restores HP when no condition blocks it ([18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)). Owner-approved, 2026-11-03: staying awake brings Fatigue: Tired from 18 hours awake, Very tired from 30, cleared by a full sleep ([07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md)).
- **Theft.** Theft from a person is one Reaction Check. Theft skill and Dexterity widen its good zone and the target's Perception narrows it; any result in the good zone takes the item unseen, and a bad one means the target notices. The zone is 20 % of the bar at even odds, between 5 % and 60 % (owner-approved, [10.3](10.3%20Stealth%20and%20Detection.md) §6).
- **Tasks.** Tasks that pay gold come from the Notice Board, the Masters, and the faction.
- **Durability.** Durability is off in the slice: no item wears or breaks.
- **Gathering.** Owner-approved, 2026-11-04: anyone may gather herbs, wood, and ore; gathering repeats cycle after cycle until cancelled or the node runs out; Skinning a beast needs a lesson from a Master. Authoring defaults: about 40 trees, 12 ore veins, 30 herb patches, and 10 flax patches around the city; cycles, yields, and checks in [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4.
- **Item Creation.** Owner-approved, 2026-11-04: crafting needs a lesson from the Master of Item Creation, as Alchemy does. The workshop, recipes, and pace are in [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §3, and skill growth in [07.1](07.1%20Character%20Attributes%2C%20Skills%2C%20and%20Action%20Checks.md) §3.
- **Sanity.** The Limit potions, full exhaustion, and the world take Sanity, and its symptoms and penalties grow as it falls ([07.2](07.2%20Derived%20Character%20Statistics.md)). The slice's costs of Limits 9 and 8 are in [05.1](05.1%20Arcana%20and%20Limits.md). Sanity 0 ends the run.
- **Defeat and Yield.** A character can yield to people, never to beasts or the cult ([10.2](10.2%20Combat%20System%20Draft.md) §9): the Wardens lock it up and fine it, and Veinmarket or robbers take its gold. For Император a Yield counts against the Arcana.
- **Saving.** One Suspend Save, written on save-and-quit outside a fight or dialogue and deleted on load; the game autosaves into it after each full sleep and when the invasion starts ([17](17%20Game%20Systems%20Architecture.md) X.3).
- **Townsfolk.** Named NPCs follow schedules of home, work, tavern, and sleep, which events interrupt; Masters and their shops keep 08:00–20:00, and at the invasion ordinary NPCs hide ([18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)).
- **The start.** The character starts in plain clothes with no defence and 5 gold, with no weapon or tool (authoring default, 2026-11-04).

Every number these forms need, from the Conduct thresholds to prices, is the agent's to set as an authoring default, by the owner's direction of 2026-11-03.

## World

- **One hand-built scene** (owner's decision, 2026-11-04): one small city and a little land around it, a forest for gathering and for fighting beasts. It is not generated and does not stand on the Overworld V0 island; the seeded Overworld of [16](16%20Architecture%20Pivot.md) stays the full game's path. The city is assembled by hand from modular blocks the owner's side prepares, with real assets from the start ([SLICE_MODULAR_KIT_STANDARD.md](unity/Assets/Plans/SLICE_MODULAR_KIT_STANDARD.md)). The ground is shaped by hand with Unity's own tools; no code shapes it (code only creates it once as a flat level, kit standard §7).
- One city with a small territory outside it. It does not need the 3×3 target world of [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md).
- **The Veilmouth map (C0)**, accepted by the owner on 2026-11-04, is in [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](unity/Assets/Plans/VERTICAL_SLICE_IMPLEMENTATION_PLAN.md) §2: the sea to the south, the river Veil along the city's east side with one bridge, the Ridge, the Market, and Lower Veil joined by the Soot Stair, fields to the west, forest to the north and west, the ore on a hill across the river, and the three Ritual Site Candidates. Seven buildings open into separate interior scenes: the Soot & Bell, Harrow's apothecary, the faction's house, the Wardens' lock-up, the missing person's room, the cellar under the Soot Stair, and the old chapel. Shops serve over street counters.
- 1 World Seed for the slice (fixed seed for prototype reliability). The two Ritual Weak Points are chosen by their own key.
- Lightweight Symptoms only (patrol mood, shop open/closed, rumour tone).
- Pre-pivot continuous OrganicTown grid district is **not** the slice delivery target.

## Shops and Masters

Owner's direction, 2026-11-02. The slice does not balance territories by stage or difficulty. It has shops at a single level, as [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.1 describes:

- a trader;
- a Master of Item Creation, who also teaches Skinning and sells tools and crafting resources;
- a Master of Enchanting, who sells ingredients. Enchanting itself stays out of the slice;
- a Master of First Aid, who teaches the profession and sells bandages, splints, and antidotes;
- no gathering Masters: gathering needs no lesson, and tools and common ingredients are sold by the three Masters above (owner's decision, 2026-11-04; who sells what is in [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.4).

Harrow's apothecary is the slice's potion shop and teaches Alchemy (authoring default, 2026-11-03). If Harrow is lost in POI A, his apprentice opens the shop the next morning and teaches at double price. The Master of Enchanting stays in the slice as the seller of magical ingredients. Prices, wages, and stock are in [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.3: a day's plain work is about 10 gold, a Magic piece 15, and the Limit 9 ingredient 20.

A player reaches gear by any of these routes, and all of them are possible in a run: gather resources and craft, take tasks for gold, buy resources or finished items, or steal money from NPCs and buy. A player can gather a great deal in two days and craft their own gear. A Legendary from their own hands is very unlikely but possible: 0.1 % an item at skill 15 (19 §3). Enchanting stays out.

Crafting has no ceiling, by the owner's decision of 2026-11-02. A player who wants to craft pays in time and effort: finding resources and Masters, guessing that gathering exists, earning the first pickaxe or axe, walking there and back, and fighting what lives there. A Limit 9 hero who spends ten attempts on each piece reaches 72 %, a little above a capital set at 70 %.

## Factions

- **One joinable faction:** the internal-order faction of [02 World Laws](02%20World%20Laws.md), the hidden police of practitioners.
- **Wardens** (order / patrol / seizure framing) and **Veinmarket** (soft underworld / mixture / night-route framing) stay as local groups.
- **The cult** appears through the Spirit General and its followers, all of the Император Arcana in the slice. Allied followers are all of the Жрица Arcana.
- Members of other factions are optional.

## Character / Progression

- The character starts without a faction. On the first day it can pass a trial to join the faction and receive a Faction Gift, or stay out and search alone. It sets its attributes as [07 Character Life](07%20Character%20Life.md) describes.
- Limit 9 is a certain goal in a run. Limit 8 takes directed effort. Limit 7 is for NPCs only. Allies and enemies may be deeper.
- The slice has two Arcana with different styles of play ([05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md)). The faction offers Жрица, for dialogue and stealth. The character can find Император alone, for confrontation.
- The church has no next Limit for Император, so a character on it must seek the next Limit among the cultists. Both Arcana have an occupied Limit 0, so the player starts on a fork that can never reach Limit 0.
- Each Limit grants two abilities. An NPC at Limit 7 has six, the same abilities the player would have. They are listed in [05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md).

## The first-day trial

The slice has no backstory: the character has come of age and decides to become an adventurer. Someone offers it a job, or it overhears talk of an investigation into a disappearance. This is the trial.

The character starts with almost nothing, so fighting is hard at first. It can find a weapon and try force, or use attention and dialogue. It learns about supernatural forces during the trial or later. The trial always leads past one trace of a Ritual Weak Point, with no sign of what it is for.
- One Bargain offer remains as a priced tease.

## Information Sources

Must include:

- Notice Board;
- Bartender;
- Street Rumour;
- at least one Physical Trace;
- at least one Witness or involved NPC.

Newspaper and Soft Memory are optional stretch goals.

## Points of Interest

The three authored POIs of the first days stay as side events. Full cards live in [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md).

Times, authoring defaults of 2026-11-03: POI A's notice is pinned at 14:00 on the first day and its window ends at the second-shift whistle at 20:00, six hours or an hour of real play. POI B runs from the eleventh bell, 23:00, to 02:00 the same night. POI C runs from its appearance to the invasion. A and B do not overlap, so a player can take both, while the trial and the tasks compete with them for the first day. The window classes, tips, and marking follow [04 Points of Interest](04%20Points%20of%20Interest.md) and [09 Information Sources](09%20Information%20Sources.md).

### Side events and the invasion

A side event of the first days changes the city's forces at the invasion, because a Side Event changes the war directly ([02 World Laws](02%20World%20Laws.md) § What changes Balance). Wardens and Veinmarket are local groups and have no Faction Influence.

On the Combat Grid, with the slice's items, a main force of 5, 6, or 7 gives the allies alone 59 %, 38 %, or 18 %, and one more defender with a club and no items takes them from 41 % to 53 % ([10.2](10.2%20Combat%20System%20Draft.md) §5b). Authoring defaults set by the agent on 2026-11-03 (CC-45). The cult comes from outside the city, so no event of the first days changes its forces. A side event changes who stands with the defenders instead. Wardens and Veinmarket each send one fighter to the defence at the invasion, an uninitiated fighter with a club and no items, if the group owes the player by then:

- **POI A:** defending Harrow puts Veinmarket in the player's debt; helping the Wardens seize him puts the Wardens in it.
- **POI B:** signalling the Wardens puts them in the player's debt. Protecting the victims turns Veinmarket against the player when the seed made the collectors Veinmarket's men, and changes nothing for either group otherwise. Following the coach changes neither.
- An unattended outcome puts nobody in debt. A crime against a group that someone notices ([10.3](10.3%20Stealth%20and%20Detection.md) §6) cancels its debt.
- Each group sends one fighter at most.

With `item_sim.py`, at 4,000 fights a line, the allies alone stop the ritual in 21 % of runs, with one local fighter in 29 %, and with both in 36 %. Both together are worth about as much as a Limit 9 hero with no items, 34 % ([10.2](10.2%20Combat%20System%20Draft.md) §5b). The first days therefore matter, and the player's own fight in good gear still matters more: a Limit 9 hero in a city set gives 43 %.

### POI A — Brass-Number Arrest at Harrow & Tincture

- Type: Street Conflict
- Window: Short (until second-shift whistle)
- Axes: Allegiance + Approach (+ Attention)
- Teaches: public fork, hidden residue in the city's forces, non-Confront options
- Wardens vs Apothecary Harrow / Veinmarket pressure; "crowned buyer" hint

### POI B — Eleventh-Bell Collectors

- Type: Street Conflict
- Window: Brief (eleventh bell to ~02:00)
- Axes: Allegiance + Approach (+ Attention/Commitment)
- Teaches: rescue-vs-trail tension, urgency, Turn-Based Confront likely
- Loom Lane mill-girl abductions; links to black tincture / crowned demand

### POI C — Soot Stair Trace

- Type: Trace / Clue
- Window: until the invasion
- Axes: Attention + Approach (+ Bargain tease)
- Appears after A or B resolves (participated or missed)
- Teaches: world continues; A+B linkage; Physical Trace reading; priced black-drop Bargain

### The invasion

- Type: Operation Echo
- Window: the ritual's window
- Axes: Attention + Approach + Commitment
- Teaches: the world's threat, preparation, and more than one way to stop a ritual

## Approaches and Confront

All six Approaches must appear at least once across the slice:

- Confront
- Convince
- Threaten
- Exchange
- Veil
- Withdraw

Turn-Based Mode requirements:

- Confront always runs **in place** on a Combat Grid around the player;
- no voluntary Turn-Based Mode during free exploration;
- the Initiative and AP economy of [10.2](10.2%20Combat%20System%20Draft.md) during Confront only;
- Break Away and Spare/Yield available in the abduction and/or arrest Confront;
- cover, concealment in vegetation, and leaving the fight by moving away;
- a fight that follows the player through a door into an interior scene;
- transparent walls around a street fight.

## Decision / Teaching Order

1. Early: Attention + Approach via board/bartender → choose a lead.
2. Mid: Allegiance fork in POI A or B.
3. Late tease: one Bargain offer (refuse remains valid).
4. Final test: the invasion asks the player to use what they prepared.

# Scope Out

Not in the Vertical Slice:

- Enchanting;
- Death Gap and successor characters;
- Legacy Cache;
- Arcana change;
- Factionless later-life flow;
- several cities, City Control between cities, and the Capital Gate;
- manifestation and World Collapse;
- economy simulation beyond shop prices, stock, and access locks;
- newspaper network;
- full church pantheon;
- procedural POI generation (hand-authored only);
- perfect BG3 feature parity.

# Player Journey

Recommended critical path for proving the fantasy:

```text
Arrive in the city
        ↓
Read board + talk to bartender; gather, craft, brew, join the faction or not
        ↓
Choose POI A or B first (Attention / Commitment)
        ↓
Resolve with any Approach set (not only Confront)
        ↓
See residue next day / later that night
        ↓
Follow POI C Trace; meet one Bargain tease and accept or refuse
        ↓
Day 1–2: the Spirit General comes out in the city and the ritual starts
        ↓
15–20 ticks later the General leaves
        ↓
Fight, sneak, or talk through the followers to the ritual
        ↓
Stop the ritual and win, or lose to it
```

Side optional: do both A and B if windows allow; missing one still completes the run if residue + Trace teach the lesson.

# Success Criteria

The slice passes if playtesters can honestly say most of:

1. “I saw a lead and chose whether to chase it.”
2. “I could solve something without fighting.”
3. “When I waited, the world changed.”
4. “I wanted to know what the trace meant.”
5. “Fighting felt deliberate in turns, not like a grind.”
6. “The power offer felt risky to take.”
7. “What I prepared changed how the ritual fight went.”
8. “I want to try stopping the ritual another way.”

Hard fail conditions:

- Confront is clearly the best reward path;
- markers spoil every mystery from minute one;
- missing a POI feels like empty nothing rather than Aftermath;
- the player asks for XP/level-up as the main missing piece.

# Implementation Milestones

## Milestone S0 — Scene, movement, doors, clock

Detailed plan for every milestone: [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](unity/Assets/Plans/VERTICAL_SLICE_IMPLEMENTATION_PLAN.md). It also moves stealth into S3 and splits S5 into S5a–S5c; where it is more detailed than the lists below, it governs. Each milestone starts only on the owner's authorisation. [11.2](11.2%20Milestone%20S0%20Greybox.md) is historical.

S0 target (owner's decisions, 2026-11-04):

- the hand-built scene of Veilmouth and its forest, from the modular kit, with stand-ins where a block is missing;
- free third-person walk;
- one interior scene (tavern) through a door and back;
- the shared clock of [18](18%20World%20Time%20and%20Living%20Simulation.md).

**Implementation note:** Pre-pivot OrganicTown / CityGenerator greybox remains in `unity/` as prototype archive, not the delivery target.

## Milestone S1 — Information + POI shells

- board/bartender tips create Hinted/Marked states;
- POI A/B Active Windows tick in world-time;
- unattended resolution stub produces Aftermath flags.

**Implementation note (2026-08-06):** runtime stubs live under `unity/Assets/Scripts/World/` (`WorldClock`, `PoiDirector`, `TipGiver`). Full Decision Nodes remain S2.

## Milestone S2 — Decision Nodes

- POI A and B fully fill Decision Node cards in data;
- Convince / Threaten / Exchange / Veil / Withdraw stubs;
- residue flags change shop/patrol/rumour.

**Implementation note (2026-08-06):** `DecisionNodeCatalog` + `DecisionNodeSession` (keys 1–5 / Esc). Six Approaches covered across A/B options. Confront remains a stub until S3 TB combat. Residue via `DistrictResidue` (labels + Merrin follow-up lines).

## Milestone S3 — Combat Grid

- Confront builds a Combat Grid around the player;
- the Initiative and AP economy of [10.2](10.2%20Combat%20System%20Draft.md) on that grid only;
- one simple enemy type;
- Break Away + Spare/Yield stubs;
- free movement returns with a residue stub.
- no voluntary TB outside Confront.

## Milestone S4 — Trace + Bargain tease

- POI C from Aftermath;
- one Bargain offer node.

## Milestone S5 — Preparation loop

- gathering, Item Creation, Alchemy, First Aid, shops, and theft in a limited form;
- sleep and the shared clock across the first days;
- the joinable faction, its trial, and the slice's Arcana.

## Milestone S6 — Invasion

- the Spirit General comes out on day 1–2 and leaves after 15–20 ticks;
- the ritual site, its guards, and its senior;
- 8–10 enemies and allied groups that leave the allies about 21–23 % with no player, as the owner accepted on 2026-11-03;
- the ways to stop the ritual, including stealth and dialogue checks;
- win and loss endings.

## Milestone S7 — Polish pass

- readability of urgency;
- fail/valuable failure checks;
- strip anything that pushes toward grind.

# Content Budget

Keep intentionally tiny:

| Piece | V1 slice count |
|-------|----------------|
| Cities | 1 |
| Districts with authored POIs | 1 (Lower Veil) |
| Joinable factions | 1 |
| Local groups | 2 |
| Main authored POIs | 3 + the invasion |
| Named talk NPCs | 6–10 |
| Enemy archetypes | 2–3 |
| Generals | 1 |
| Player Arcana | 2 |
| Player Limits | 9–8, with Limit 7 for NPCs |
| Bargain teases | 1 |

If the budget grows, cut content before cutting systems proof.

# Confirmed Slice Decisions

1. **Place:** one city, Veilmouth, with Lower Veil as one of its districts. Both names are placeholders.
2. **Mood/era:** Lord of the Mysteries-like industrial-occult transition (18–19th century social structure + early machines), not a plot/lore copy.
3. **Faction:** only the internal-order faction is joinable, through a first-day trial; Wardens and Veinmarket are local groups.
4. **Timing:** the Spirit General comes out after one to two game days.
5. **Death Gap, Legacy Cache, Arcana change, and Enchanting:** out of this Vertical Slice.
6. **Names:** all placeholder names may be renamed later.

# Documentation Review

- **Architecture Pivot:** Slice delivery follows overworld → interior → combat in place.
- **Vision / Player Experience:** Slice targets curiosity, tense choice, agency, residue, priced power tease.
- **POI / Information / Approaches / Combat:** Directly implemented in miniature (TB in place during Confront only).
- **Progression / Character Life:** One Factionless life per run. The player reaches Limits 9–8; Limit 7 is for NPCs, and allies and enemies may be deeper. Later-life systems are excluded.
- **Cult Campaign:** The slice uses the campaign's ritual rules in one city ([21](21%20Cult%20Campaign%20and%20City%20Control.md)).
- **Era mood:** Compatible with priced mystery and social inequality without requiring high-fantasy gloss.
- **Solo scope:** The required mechanics are many, so each stays in a limited form, and content stays small. Its numbers are the agent's authoring defaults of 2026-11-03, and what stays open for the slice is listed in [99 Open Questions](99%20Open%20Questions.md) § Vertical Slice.
- **Glossary / Open Questions:** Confirmations recorded as resolved.

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md)
- [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)
- [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md)
- [04 Points of Interest](04%20Points%20of%20Interest.md)
- [08 Confront Constraints](08%20Confront%20Constraints.md)
- [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md)
- [09 Information Sources](09%20Information%20Sources.md)
- [99 Open Questions](99%20Open%20Questions.md)
