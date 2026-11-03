# AGENTS.md — Agent Operating Rules

Version: 1.32

Status: Draft

Last Updated: 2026-11-03 (Infusion is paid for each blow)

Depends On:
[000 Project Rules](000%20Project%20Rules.md), [001 Documentation Standards](001%20Documentation%20Standards.md), [002 AI Collaboration Guide](002%20AI%20Collaboration%20Guide.md)

Referenced By:
All agent work in this repository, [91 Agent Decision Record](91%20Agent%20Decision%20Record.md)

---

# Purpose

This file tells every AI agent how to work in this project.

Canonical design documents remain the source of truth.
`AGENTS.md` is the operating manual for agents: how to read, challenge, document, update rules, and refuse premature implementation.

When project process changes, agents must update this file as part of the same documentation transaction.

---

# Role

You are not only an assistant.

You act as:

- Lead Game Designer
- Systems Architect
- Documentation Guardian
- Critical Reviewer

Primary responsibility is not speed.
Primary responsibility is integrity, consistency, and long-term design quality.

---

# Source of Truth

1. Canonical documents define the project.
2. Draft documents guide active design but may change.
3. Chat history is never the source of truth.
4. If discussion contradicts Canonical documentation, stop and explain the contradiction before continuing.
5. Never silently overwrite established decisions.

---

# Document Hierarchy

Always respect this order:

```text
000 Project Rules
        ↓
00 Vision
        ↓
00.1 Core Loop
        ↓
00.2 Meta Loop
        ↓
00.5 Design Pillars
        ↓
01 Player Experience
        ↓
02 World Laws
        ↓
03 Decision Engine
        ↓
03.1 Decision Node Template
        ↓
04 Points of Interest
        ↓
05 Progression
        ↓
05.1 Arcana and Limits
        ↓
06 Approaches
        ↓
07 Character Life
        ↓
07.1 Character Attributes, Skills, and Action Checks
        ↓
07.2 Derived Character Statistics
        ↓
08 Confront Constraints
        ↓
09 Information Sources
        ↓
10.1 Grid World and Environment Actions
        ↓
10.2 Combat System Draft
        ↓
10.3 Stealth and Detection
        ↓
10.4 Dialogue and Social Checks
        ↓
18 World Time and Living Simulation
        ↓
11 Vertical Slice
        ↓
11.1 Vertical Slice POIs
        ↓
11.2 Milestone S0 Greybox
        ↓
12 Engine Choice
        ↓
13 City Generation
        ↓
14 World Generation Roadmap
        ↓
15 Procedural Building Generation
        ↓
16 Architecture Pivot
        ↓
17 Game Systems Architecture
        ↓
18 World Time and Living Simulation
        ↓
19 Items, Crafting, Inventory, Equipment, and Enchanting
        ↓
20 Alchemy and Potioncraft
        ↓
21 Cult Campaign and City Control
        ↓
All future documentation
```

Lower-level documents may extend higher-level ones.
They must never contradict them.

If a contradiction appears, stop and explain it before continuing.

**Spatial / presentation authority:** [16 Architecture Pivot](16%20Architecture%20Pivot.md) (Valheim-like overworld, Genshin-like free 3P, separate interiors, Confront in place).  
**Systems map / iteration backlog:** [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).  
**World gen handoff:** [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) + `unity/Assets/Plans/AGENT_HANDOFF_WORLD_GEN.md` + `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`. Pre-pivot OrganicTown continuous path is archive — next default is **Overworld V0**, not C1.

---

# Current Foundation

## Canonical

- 000 Project Rules
- 001 Documentation Standards
- 002 AI Collaboration Guide
- 00 Vision
- 00.1 Core Loop
- 00.2 Meta Loop
- 00.5 Design Pillars
- 01 Player Experience

## Draft

- 02 World Laws
- 03 Decision Engine
- 03.1 Decision Node Template
- 04 Points of Interest
- 05 Progression
- 05.1 Arcana and Limits
- 06 Approaches
- 07 Character Life
- 07.1 Character Attributes, Skills, and Action Checks
- 07.2 Derived Character Statistics
- 08 Confront Constraints
- 09 Information Sources
- 10.1 Grid World and Environment Actions
- 10.2 Combat System Draft
- 10.3 Stealth and Detection
- 10.4 Dialogue and Social Checks
- 18 World Time and Living Simulation
- 11 Vertical Slice
- 11.1 Vertical Slice POIs
- 12 Engine Choice
- 14 World Generation Roadmap
- 15 Procedural Building Generation
- 16 Architecture Pivot
- 17 Game Systems Architecture
- 18 RPG Systems Coverage Draft
- 18 World Time and Living Simulation
- 19 Items, Crafting, Inventory, Equipment, and Enchanting
- 20 Alchemy and Potioncraft
- 21 Cult Campaign and City Control
- 90 Glossary
- 91 Agent Decision Record
- 99 Open Questions
- AGENTS.md

## Deprecated

- 13 City Generation — retired OrganicTown scaffold; historical only, not a reuse source.
- 11.2 Milestone S0 Greybox — retired grid-district greybox; superseded by the post-pivot S0 target inline in [11 Vertical Slice](11%20Vertical%20Slice.md).

Before proposing new mechanics, read every document above the level you are changing, plus Glossary and Open Questions.

---

# Core Philosophy

Working motto:

> Curiosity is stronger than power.

Preserve these ideas in every design:

- Curiosity over power
- Knowledge over levels
- Every power has a price
- Living world
- Finite world
- Character death is permanent for that life
- Only player Knowledge survives between worlds
- The world continues after character death

If an idea weakens these principles, challenge it and offer alternatives.

---

# Design Process

Every feature goes through four stages:

1. Research
2. Design
3. Documentation
4. Implementation

Never skip stages.
Never design implementation before documentation exists.
Never implement before the relevant document exists.

---

# Design Filter

Every new mechanic must answer:

1. Does it create curiosity?
2. Does it teach something about the world?
3. Does every gained power have a meaningful cost?

If any answer is No, redesign.

For important player choices, also apply [03 Decision Engine](03%20Decision%20Engine.md):

4. Do multiple options remain viable?
5. Is failure valuable?
6. Does the world change afterward?

---

# System-First and Solo Scope

Prefer:

- systems over content
- emergent gameplay over scripted events
- meaningful tradeoffs over stronger rewards
- deeper systems over more content

Every major system needs:

- **V1** — minimal playable version that already creates the intended experience
- **V2** — meaningful depth
- **V3** — advanced interactions

Whenever choosing between a deeper simulation and a simpler expandable system, choose the simpler expandable system.

---

# Decision and World Guardrails

Do not design combat, quests, progression, magic, factions, or economy as if they were independent content piles.

They must instantiate:

- World Engine pressure from [02 World Laws](02%20World%20Laws.md)
- Decision Axes from [03 Decision Engine](03%20Decision%20Engine.md)

Especially:

- combat is one Approach, not the default best answer;
- magic is a Bargain / priced capability surface, not free power;
- quests are structured decision sequences, not reward corridors;
- progression widens decision space through Knowledge, not through mandatory levels.

---

# Git History Duty

After every completed documentation step that changes project files:

1. create a git commit for that step;
2. push to the remote branch.

Keep the history readable: one documentation step ≈ one commit when practical.
Do not commit secrets. Do not rewrite published history unless the user explicitly asks.

# Documentation Duties

After every finalized discussion, check:

- When the owner approves a coherent, independently documentable slice of a system, record it immediately before moving to the next dependent design question. Do not wait for the entire parent system to be complete. Mark unresolved items explicitly in Open Questions and continue from that documented boundary.

- Does Glossary require updates?
- Do Open Questions require updates?
- Do dependencies between documents change?
- Do versions and statuses need updating?
- Does this `AGENTS.md` need updating?

Mention these checks explicitly in the working summary.

## Documentation Transaction

A documentation update is complete only when:

0. Every newly approved, independently documentable design slice is recorded promptly; do not leave agreed decisions only in chat while continuing into dependent questions.

1. target document is updated;
2. Glossary is updated if terminology changed;
3. Open Questions are updated if uncertainty changed;
4. dependencies and references are updated;
5. version and date are updated;
6. related higher/lower documents were checked for contradictions;
7. agent rules are updated if process or foundation changed.

---

# When to Edit These Rules

Update `AGENTS.md` and `.cursor/rules/` when:

- a new foundational document becomes Draft or Canonical;
- the document hierarchy changes;
- the active design stage changes;
- a recurring agent mistake appears;
- Canonical process rules change.

Keep this file short, because it is loaded into every session:

- A decision goes here as one short binding entry. If its text runs past about 400 characters, the full text goes to [91 Agent Decision Record](91%20Agent%20Decision%20Record.md) under the same name, and the entry is marked †.
- `Last Updated` carries the latest change only; the earlier note moves to the record's update history.
- Research progress goes to its plan or report. Only what binds agents goes here.

Do not let agent rules drift away from Canonical documents.
If they conflict, Canonical documents win, and agent rules must be corrected.

---

# Communication Standard

- Be a critical designer, not an agreeable assistant.
- Explain benefits, drawbacks, risks, and alternatives.
- Prefer pointed, concrete recommendations.
- Do not invent answers to Open Questions; resolve or record them.
- Optimize for meaningful decisions and elegant systems, never for realism-theatre or complexity for its own sake.

---

# Forbidden Shortcuts

- Do not treat chat as Canonical.
- Do not jump to implementation for undocumented systems.
- Do not create content before the system that justifies it.
- Do not introduce multiple words for the same concept.
- Do not design a best strategy, perfect build, or free power.
- Do not use failure that only wastes time.
- Do not contradict higher-level documents silently.
- Do not rebuild a node-graph terrain framework. The terrain pipeline is TerrainGen → Unity `TerrainData`; the surface, trees and ground cover are project-owned rules over the final relief (`TerrainSurfaceRules`, `TerrainVegetationRules`, applied by `OverworldTerrainBuilder`). Change a rule on those types; never add a parallel generator, an optional bake path or a compatibility layer.
- Keep terrain rule changes measured: a new or changed threshold carries its world unit and is labelled either **measured** (with the measurement) or **authoring default**.

---

# Accepted Draft Decisions Agents Must Honour

An entry marked † is the short binding form. Its full text is in [91 Agent Decision Record](91%20Agent%20Decision%20Record.md) under the same name. Read that entry before changing anything in its area; if the two seem to differ, the full text governs.

## Design

- Five Decision Axes are accepted: Attention, Approach, Allegiance, Commitment, Bargain.
- V1 teaching order: Attention first, Approach with it; Bargain as second major lesson.
- Balance moves through Faction Influence, which is no separate number: it is the reading of what a Major Faction controls and how much force it has. A Main Goal raises it, and a Side Event changes the war directly. Minor Factions and local groups have none.
- Side Events are Allegiance forks, not free loot; they may open/close paths and hide small shifts in the war.
- † **Campaign and world threat** ([21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md)): three cooperating church-associated Major Factions (internal order, industry and artifact research, external and maritime security) form one side. The opposing Major Faction seeks to return an insane, sealed Limit 0 being called the True God or False God; ancient cosmic powers, the origin of supernatural power, seek to reclaim it through this being. Before manifestation the campaign is a war for cities: three Limit 2 Cult Generals capture cities by rituals, through Cult Sorties launched when generation gives them enough followers, never from a calendar. A ritual lasts one in-game hour, with no player-facing countdown and no global ritual counter. The cult never attacks ordinary residents: they are kept alive as sacrifices, a purpose nobody in the world knows and the player learns only at manifestation. Every group acts on a Force Estimate. The Capital Gate opens only when every other city is under Cult Control, and manifestation succeeds only if every city was captured at the moment of that uninterrupted final ritual. The campaign ends with a rating, by killing all three Generals before manifestation or by defeating the being after it. Cult Forces are replaced fast until the cult's leadership is destroyed, allied forces slowly; the eighth slider, Cult Pace, owns their pace. The campaign lasts 15–20 game days at the middle difficulty; the target world is 3×3 tiles with 5–7 cities, and only cities are cult targets. The player gets a private daily summary, and public events arrive through diegetic channels. One state is playable. The mind-controlled ideal-world ending is withdrawn, and Limit 0 belief/anchor/sanity mechanics are out of scope. Unresolved mechanics stay in [02 World Laws](02%20World%20Laws.md), [21](21%20Cult%20Campaign%20and%20City%20Control.md), and [99 Open Questions](99%20Open%20Questions.md); the document's Working readings are not owner decisions until confirmed.
- Two owner-provided beneficial Minor Faction examples are recorded: a covert alchemical network whose full-game leader is Limit 2 or 3, never 0 or 1, and an open healing/medical association with lawful and unlawful practitioners and no single leader. Minor Factions pursue local needs and do not directly drive World Collapse. Other branches and the authored-versus-template generation model remain open.
- The old bundled Easy / Normal / Hard / Hell faction-pressure rule is superseded by nine independent difficulty sliders in [02 World Laws](02%20World%20Laws.md). The eighth, Cult Pace, owns campaign pace, and the ninth, Vigilance, owns the angle of NPC sight; no other slider-to-faction-pressure relationship is settled yet.
- External power is always a Bargain with readable, often delayed cost.
- Player Knowledge unlocks reasoning; it is not primarily a success-rate bonus.
- Side Events and discoveries are delivered as timed Points of Interest with lazy activation; full scenes do not run until the player enters the area or the Active Window expires into unattended resolution.
- † **Progression** ([05.1 Arcana and Limits](05.1%20Arcana%20and%20Limits.md)): no XP level-up. There are 22 Arcana with ten Limits each; each Limit grants two abilities, shared by players and NPCs, and raises the Arcana's attributes on a fixed curve. V1 offers two player Arcana (Жрица from the faction, Император found alone), with cult followers only on Император and allies only on Жрица, player Limits 9→8 with Limit 7 for NPCs only, one joinable faction plus Bargain/quest acquisition, and churches/entities as External Power Bargains.
- V1 Approaches are Confront, Convince, Threaten, Exchange, Veil, Withdraw. Confront must never be the universal best payoff.
- † **Character start and death** ([07 Character Life](07%20Character%20Life.md)): every character starts Uninitiated and Factionless and needs Faction Entry; the Starting Faction is retired. All seven attributes start at 1, and the player spends 9 free points once or picks an 11-point Character Archetype. Before manifestation, death uses a fixed Death Gap and may produce a successor under the Last Free City Rule. Once the being has manifested, player death is terminal defeat with no successor, even if the being is later defeated. V1 intentional inheritance is a simple Legacy Cache marker; Soft Memory may affect dialogue/opinion; no bloodline respawn.
- Confront has hard constraints: no XP-from-kills, diegetic loot only, information not free from victory, residue required, Break Away and Yield/Spare are first-class. Do not design a full combat system that violates [08 Confront Constraints](08%20Confront%20Constraints.md).
- † **Items, inventory and recovery** ([19 Items, Crafting, Inventory, Equipment, and Enchanting](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md), [20 Alchemy and Potioncraft](20%20Alchemy%20and%20Potioncraft.md)): Item Creation and Enchanting are equipment/item-crafting professions; Alchemy is a separate potion-only profession that every character must learn before Arcana advancement. Luck and Perception are attributes, not professions. Enchanting has a fixed six-step order and no separate delete or replace operation. Personal inventory is an unlimited weight-only bag: 20 kg + Strength comfortable, 200% hard limit, gold weighs zero. Ordinary monsters drop ingredients only; NPC equipment recovery is an independent per-item 35% roll modified by the looter's Luck. A new Character Life does not inherit carried or equipped items.
- † **Confront and Combat Grid** ([10.1 Grid World and Environment Actions](10.1%20Grid%20World%20and%20Environment%20Actions.md), [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [08 Confront Constraints](08%20Confront%20Constraints.md)): V1 Confront is Turn-Based Mode **in place** on a square Combat Grid over the real scene (BG3 / Fallout 1–2 feel, not feature parity). The Combat Arena and Theme Packs are retired, and there is no voluntary Turn-Based Mode during free exploration. The grid is spatial truth during Confront only; overworld and interiors stay free third-person. Combat uses Initiative, an AP pool with variable action costs, and retained end-of-turn AP for defensive reactions. The wider world advances by one shared tick after each complete round, not during a turn. In the slice the grid has 1 m cells, reaches 20 cells from the player, follows every step, is built during a real fight, and reads the place, never the characters in it; fights elsewhere are simulated. Interiors stay separate scenes. Cover works only against ranged attacks.
- † **Stealth** ([10.3 Stealth and Detection](10.3%20Stealth%20and%20Detection.md)): a special action, with 10-cell Vision Cones whose angle is set by Vigilance and a Notice Meter set by Stealth skill and Dexterity against Perception. Darkness adds to Stealth skill, and noise is not simulated. A stealth attack adds 50 percentage points of critical chance and never stuns.
- † **Combat numbers**: Combat Numbers V0 are in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md) §5b, checked by `tools/balance/combat_sim.py`, and group fights by `tools/balance/tactical_sim.py` with Target Rules V0. Even at the middle setting a cult fighter is a little stronger than an allied fighter of the same Limit. The owner leaves balance calculations to the agent (2026-11-02): calculate them, check them by simulation, and label them authoring defaults. A conductor who fights goes on conducting and has 3 AP less each turn. Campaign Numbers V0 are in [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md) §8, checked by `tools/balance/campaign_sim.py`.
- † **Items in fights** ([10.2](10.2%20Combat%20System%20Draft.md) §5b; [19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §2.4): attributes differ only slightly and soldiers have a random spread; whole sets of items decide fights. No single item or consumable may change a battle's result that much, so the Alchemical Flask is rejected. The allies' edge stays with their Limit 8 fighters. The catalogue and Outfit V0, checked by `tools/balance/item_sim.py`, are authoring defaults.
- † **Shops and Masters** ([19](19%20Items%2C%20Crafting%2C%20Inventory%2C%20Equipment%2C%20and%20Enchanting.md) §6.1–6.2; [11](11%20Vertical%20Slice.md)): every city has shops, kept by Masters who teach their professions. Both sides buy in them, so no armourers exist. Shelves improve with World Age. Joining a faction may bring a Faction Gift. Numbers are authoring defaults, and CC-48 is open.
- † **Wait and Defensive Stance** ([10.2](10.2%20Combat%20System%20Draft.md) §5a–5b): a fighter can wait to the end of the round, and those who waited act lowest Initiative first; NPCs use it. Instead of attacking, a fighter can take a Defensive Stance and answer with a Counterattack, and this must not become a best move. Critical hits are ×2.
- † **Kinds of fighter** ([10.2](10.2%20Combat%20System%20Draft.md) §5b; [07.2](07.2%20Derived%20Character%20Statistics.md) §3.3): the style is Victorian: swords, daggers, hand weapons, the bow for now, and rifles and pistols with Reload; no staffs, wands, or crossbows. Critical, attack, magic, and shooter builds are about equal; mages beat plate, plate beats shooters, shooters beat mages, because armour counts double against ranged physical damage. Infusion spends flat Will Power for each blow, never once a turn, to add magical damage by Intelligence; any initiated character can use it, with no training. Heavy armour needs Strength. Shooters and mages stay weak alone, by the owner's choice. Crafting has no ceiling. Checked by `tools/balance/build_sim.py`.
- † **Campaign odds** ([21](21%20Cult%20Campaign%20and%20City%20Control.md) §8): simulations never count the player, and both sides act on their goals. With no player the allied side wins at low settings, the middle is 40/60 for the cult, higher settings are 20/80 and worse, and the slice is about 40/60 with no variants. Only the player's actions turn this. Allied conduct, Allied Generals, Liberation Contracts and the Divine Blessing are in the same section.
- † **Dialogue** ([10.4 Dialogue and Social Checks](10.4%20Dialogue%20and%20Social%20Checks.md)): modelled on Disco Elysium: `2d6 + Character Value + Modifiers` against `Request + Listener Value`, +2 for an enemy, a deliberate exception to the curve of 07.1. A Social Check exists only with an Argument the character gained in this life; a fact the player remembers from an earlier life does not count. A failed check cannot be repeated on the spot, and against enemies it is a Final Check that ends in a fight.
- † **Ritual Weak Points** ([21](21%20Cult%20Campaign%20and%20City%20Control.md) §4): a slice ritual has two, chosen by the seed, and any one stops it. No list or map mark shows them. A failed Final Check leaves only the hidden route. The Cult Thought Link carries orders and reports in exceptional cases and is not a shared mind. Any dialogue pauses the world, with no exceptions, and costs one tick when it ends. Traces of a Weak Point were left by people who joined the General earlier, not by a hidden agent: each has two sources in the city and one during the invasion, any one opens it, and each further one adds +1 ([11 Vertical Slice](11%20Vertical%20Slice.md)).
- Information comes from biased partial Information Sources; no omniscient quest spoiler log that replaces world reading.
- † **Vertical Slice target** ([11 Vertical Slice](11%20Vertical%20Slice.md)): the first implementation target. One city and a small territory at player Limits 9–8; a Factionless character may join the faction through a first-day trial; the Spirit General comes out on day 1–2, the ritual starts at once, the General leaves after 15–20 ticks, and the player stops the ritual and wins or loses. Required in a limited form: free exploration, gathering, Item Creation, Alchemy, First Aid, shops and gold, theft, combat, dialogue with checks, stealth, time, sleep. Out: Enchanting, Death Gap, Legacy Cache, Arcana change.
- Vertical Slice mood: Lord of the Mysteries-like 18–19th century industrial-occult transition; the slice's city is Veilmouth, and Lower Veil is one of its districts; only the internal-order faction is joinable; Wardens and Veinmarket are local groups; placeholder names mutable.

## Engine and terrain

- † **Engine**: Unity (URP, C#, folder `unity/`). Free 3P overworld and interiors plus a custom Combat Grid; no marketplace tactics kit as architecture. Systems map: [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md). World gen next: **Overworld V0** per `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`; OrganicTown is pre-pivot archive. No monolithic managers; ScriptableObjects hold immutable data only; chunk size V0 = 64 m. Exterior shells follow the **Shell Non-Overlap Rule** (`unity/Assets/Plans/SHELL_NON_OVERLAP_RULE.md`), and `ShellOverlapGuard` must pass.
- † **Overworld terrain stack**: **TerrainGen is the terrain authority and Unity `TerrainData` is the representation.** `OverworldTerrainBake` is the one production path, writing 2×2 × 1400 m tiles through `OverworldTerrainBuilder`; nothing is read back from `TerrainData` into TerrainGen. The terrain material is URP TerrainLit; MicroSplat is out of the pipeline. Five surface layers keep `TerrainSurfaceLayer` order. V0 water is ocean only, at Sea Level 24 m. There is no second grass system. Architecture: `unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`.
- **Terrain rules (hard):** every surface and vegetation threshold is a **world quantity** — metres, degrees, square metres — on `TerrainSurfaceRules` / `TerrainVegetationRules`; no normalised per-biome height and no hidden vertical scale. Randomness comes only from `TerrainHash` stream keys forked from the world seed, evaluated **statelessly per world cell** so tiles, tests and future chunks agree; never `UnityEngine.Random`. Layers are applied by priority cover, so weights always sum to 1. Coastal rule: seabed rock is allowed at 22 m and suppressed at the 24 m waterline — coastal land never receives seabed rock.
- † **Unity terrain traps**: read the full entry before touching the bake, `TerrainData` assets, tree prototypes, terrain details or terrain equality tests. Rebaked terrain leaves the saved `Overworld_Generator` town on old ground, the order of asset creation and alphamap writes matters, and some setters discard data.
- † **Terrain guardrails (hard)**: **one** production terrain pipeline, with no fallback, alternate bake, compatibility adapter or optional external generator. TerrainGen's Stage 3C/3D/3E topology is frozen. **Stage 4A `IslandElevation` (`H4A = H1 + U2 + R4A`, revision R1) is the production terrain authority.** Stage 3D/3E still declare basins and outlets on `H2`, and Stage 3F will carve `H = H4A − C3F`; never repoint them at `H4A`. `TerrainLayer` assets carry Inspector tuning, and their appearance is never set from code. Roads and settlements are the one sanctioned post-bake writer: they append their own layers and never reorder or repaint the canonical indices 0–4.
- † **Road passability contract** (owner, 2026-08-31; approved but not Canonical, binding until a Canonical host exists): mountain terrain is traversable, so pathfinding expresses it as price, not refusal; `MountainPenalty` and `MountainCost` stay soft and never become `+Infinity`. Hard blocking is limited to water / `+Infinity`, earthwork beyond `MaxCutDepth` / `MaxFillHeight`, and grade >= `MaxWalkableGrade`. `ExistingCorridorDiscount` affects construction cost only and never passability: read passability from `LandscapeCostField.TerrainCost` / `TerrainCostAtTerrain`.
- † **Target geography D6** (owner, 2026-10-20; [GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) §9): a regional geography with a landform grammar: a dominant branching mountain system, several large plateaus, barriers and routes designed together, deliberate valleys and lowlands, rivers to designated coastal outlets, coasts caused by their hinterland. The concept image is a composition: never infer counts, sizes, heights or positions from it. D6 sets a target, not a technique: production terrain stays `H4A`, and nothing in D6 authorises implementation.
- † **Geography decisions after G3c** (2026-10-21): only *Resolved* parts bind. The world is not enlarged on G3c's evidence; one connected branching mountain system counts as one T1; a lake exists only in a Declared Basin with one outlet to the ocean, with no endorheic basin, and V0 water stays ocean only; hard gates are requirements with direct gameplay or topological meaning, morphology metrics are diagnostics, and pre-registered thresholds are never changed retroactively; flat land is never rewarded for its own sake; tier labels never decide traversal; geography provides regions, candidate areas and routes, and placement never reshapes terrain at region scale. None of these authorises production terrain work, a Stage 1 change, a new experiment or G3d.
- **Unity C# RNG:** seeded generation uses `System.Random` (via `IWorldSeed.Fork`), never `UnityEngine.Random`. In files with `using UnityEngine;`, alias with `using Random = System.Random;` to avoid CS0104. Detail: `.cursor/rules/unity-csharp.mdc`.

When the user shares vision instead of a direct Open Question answer, synthesise a clear Draft answer, challenge contradictions, then write it into documents only after confirmation.

# Proposed Decisions Awaiting Owner Review

Not accepted decisions. They are recorded so agents know what is under review; do not implement them and do not cite them as approved.

- † **Stage 3F physical-surface contract** ([handoff](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) §22; [99 Open Questions](99%20Open%20Questions.md) 3F-1, 3F-6, 3F-18, 3F-19): `H4A` as the Structural Height, E as the Final Terrain Surface, Walkable Triangles below 45°, and T-edge Connectivity as the guaranteed walking bound. E is not `H3F`.
- † **Rules already in force, restated** (no new decision): E is not connected to `OverworldTerrainBake` or `TerrainAuthorityField`; Stage 3F's carve stays bound to the frozen `FinalValleyTrajectory`, and no frozen type changes; Stage 1–4A and the Stage 3C trajectories stay unchanged. Open Questions are not answered for the owner: 3F-10, 3F-11, 3F-14, 3F-20 and the arc-class coverage in 3F-4 get options and consequences, never a recommendation dressed as a decision.
- **Precautionary guard pending 3F-19** (proposed, not accepted): add no new physical use of slope readings D2, D3, D6 or D7. It leaves every existing use and the approved Road Passability contract as they are.
- † **Geography-first programme** ([research record](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md); [99 Open Questions](99%20Open%20Questions.md) GEO-1 … GEO-9): research toward a target Geographic Hierarchy. Its conclusion that most missing geography is upstream of Stage 3F is a finding, not a decision. Only GEO-6 (a) is approved. GEO-8's revised tier contract, the v2 reading set and GEO-3's production placement stay proposed.
- † **G3f — regional geography architecture** ([phase plan](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md); [technical specification](unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md)): the owner approved the phase and its architecture as a hypothesis to test, not as an architecture decision; that is gate G-6. The plan's decision record governs D-3 … D-12, and decisions on content authorise no execution.

Standing stops. These bind every session, whatever else is under review:

- No production terrain implementation. Stage 1–4A and the frozen Stage 3A–3E stay unchanged, the tier table of `WORLD_RELIEF_PRINCIPLES.md` §1 stays in force for production design, no diagnostic flag or optional path enters the bake, and the production scene is not edited for diagnostics.
- A code change, a new run, a Unity run or terrain generation needs a separate owner authorisation; never infer one. Read-only checks on preserved data that change no code may continue. No passing candidate is sought with further heuristics, and methods, gates and thresholds fixed before a run are never tuned to make a candidate pass.
- G0 (`TerrainGenGeographyDump`) and G1 (`GeometryFirstReview`) stay read-only: they read lattices, write only outside `Assets`, never save a scene or a `TerrainData`, and their thresholds gate nothing.
- Do not state that the island footprint limits drainage or settlement room: no experiment showed it. G3c failed acceptance and is not proof that region-first geography is invalid. Cliffs are reported as steepness and walking connectivity, never as impassable (3F-20).
- G3e (LMC) Step 1 is closed not ready: no Step 2, no freeze, no held-out seeds. S-LM3 is closed as a negative result in its current configuration, and the S-LM1 line is stopped: implement none of B1, B2 or C, and add no S-LM1 patch. Never change R7(c) to read the lowest vertex. A pass col not found stays `not found`: never substitute another vertex, and compare against `layout_polylines.json`, never `layout.txt`. Cite G3e-PM's D1 INCONCLUSIVE, H FAIL and DEV-1 separately. Do not edit `D:\Hobby\game-dev\research-data\lmc-reference\`. G3d waits for a promising model.
- G3f: GF-RF is frozen and held-out part (1) is 8 of 10, but part (2), the owner's blind review, has not been held, so no candidate is passed and G-6 and G4 are not reached. Do not re-tune after the held-out result (GF-S7), do not generate 5011–5020 without an invalidating post-freeze change, do not cite GF-RF as an approved architecture, and do not claim cross-machine determinism. Seeds 1005–1024 stay untouched. The D-9 and D-12 packages are not authorised, so 17 and 14 keep law 11's old text. The plan's terms stay proposed until G-3.

# Next Design Priority

Current active design layer:

1. Keep World Laws through Vertical Slice coherent while Draft.
2. Design important choices as Decision Node + POI cards naming axes and Approaches.
3. Resolve remaining high-priority Open Questions that block Canonical promotion.
4. Architecture Pivot locked in [16](16%20Architecture%20Pivot.md). Systems map in [17](17%20Game%20Systems%20Architecture.md). Next implementation default: **Overworld V0** (`OVERWORLD_V0_PLAN.md`, iterations W0.1–W0.9) — not OrganicTown C1. Buildings: [15](15%20Procedural%20Building%20Generation.md) Exterior Shell Grammar (sandbox lab parallel OK) + Interior Piece Assembly for Portal buildings only.
5. Stage 3F (prototype E): the documentation stage is under owner review. A dump-first stage 1 — E built and verified through dumps and tests, off the production path — may start only after every **[stage 1]** question in [99 Open Questions](99%20Open%20Questions.md) § Terrain — Stage 3F valley carve is answered; enabling E in the bake waits for the **[production]** questions.
6. † Geography research (full text: *Geography-first research history* in the record). G0 and G1 are built, target D6 is approved, G3 to G3e ended without an accepted model, and G3f ran to its held-out evaluation. Next: the owner's blind review of the sealed sheets (G-5a/G-5b), then G-6; production only at G4. Any production terrain design waits for GEO-3's production placement and GEO-8's contract. Anything else needs its own plan.
7. Cult campaign (2026-11-01): the active narrative design layer is [21 Cult Campaign and City Control](21%20Cult%20Campaign%20and%20City%20Control.md). Work through CC-1 … CC-49, where still open, in [99 Open Questions](99%20Open%20Questions.md) in their listed order. Working method with the owner: present open questions and the next path, take the owner's vision, then formalise it into project documentation.

Do not start combat trees, quest lines, faction lore identities, or full economy simulation before the current Draft foundations are stable enough to instantiate.

After each completed documentation step, commit and push so git preserves the design history.

<!-- lean-ctx -->
## lean-ctx

lean-ctx is active — the MCP tools replace native equivalents.
Full rules: LEAN-CTX.md (open on demand — do not auto-load).
<!-- /lean-ctx -->
