# 90 Glossary

Version: 0.52

Status: Draft

Last Updated: 2026-10-21 (Large Plateau proposed; Declared Basin; Geographic Hierarchy after the post-G3c review)

Depends On:
All Canonical Documents

Referenced By:
All Documentation, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [13 City Generation](13%20City%20Generation.md), [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [AGENTS.md](AGENTS.md), [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md), [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md), [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md)

---

## Curiosity

Primary player motivation.

---

## Knowledge

Player understanding that survives Character Lives and worlds. It unlocks better reasoning and option recognition. It must not act primarily as a success-rate bonus.

---

## Player Knowledge

Synonym emphasis for Knowledge held by the real player across lives and worlds, as distinct from temporary character power.

---

## Legacy

Everything remaining after a character dies.

---

## Character

Playable individual living inside one world.

---

## Player

The real person.

The only entity preserving knowledge between worlds.

---

## World

A finite simulation containing civilizations, factions and history.

---

## World Seed

The generation input that determines a world's starting conditions, faction states, Main Goal details, Side Events, and related variation.

---

## World Cycle

The complete lifespan of one generated world.

---

## Character Life

The playable lifespan of one character.

---

## Meta Loop

Progression connecting multiple characters and worlds.

---

## Core Loop

Repeated gameplay cycle during one character's life.

---

## Pillars

Non-negotiable design principles.

---

## Price of Power

Every gained strength requires sacrifice.

---

## Experiment

Any intentional attempt to discover unknown behaviour.

---

## Consequence

A change caused by player action, failure, or World Engine activity. May be immediate or delayed, visible or hidden, temporary or permanent. Important decisions require at least one Readable Consequence.

---

## Living World

A world evolving independently of player presence.

---

## Finite World

A world that eventually reaches its end.

---

## Balance

Dynamic equilibrium of competing world conditions. It constantly shifts and cannot be permanently preserved by any faction. It changes primarily through Faction Influence.

---

## World Engine

A lightweight system that periodically attempts eligible faction operations and applies their readable consequences. It does not simulate entire civilizations.

---

## Operation

A single faction action with conditions and possible outcomes: success, failure, interruption by the player or another faction, or failure caused by an observable world event.

---

## Main Goal

A faction's primary objective drawn from a limited pool. Seed changes details such as place, time, and actors. Completion raises Faction Influence and reshapes the world.

---

## Side Event

A seed-generated secondary situation. It offers useful but forked outcomes, shifts Influence, may open or close paths, and never exists as pure free power.

---

## Symptoms

Observable changes through which the player reads hidden faction activity and influence instead of seeing progress values. Includes trade, services, encounters, rumours, and other everyday pressure.

---

## Faction Influence

The hidden current capacity of a faction to pursue its long-term goals and make operations available. Higher Influence strengthens followers, attempt frequency, artifacts, and economic niche control. It is communicated through Symptoms, not a player-facing value.

---

## Starting Faction

The faction granted only to the first character in a newly generated world. That character receives an initial understanding of the faction's current Main Goal and may later defect or realign at a real cost. Later lives begin Factionless.

---

## Factionless

Character state with no faction membership. Default for every life after the first in the same world. Joining requires Faction Entry.

---

## Faction Entry

A required trial, quest, or Decision Node chain to join a faction after starting Factionless.

---

## Death Gap

Fixed short span of world-time after character death during which the world advances and resolves outstanding pressure before the next character is created.

---

## Legacy Cache

An intentional stash prepared for a future Character Life in the same world. In V1 it creates a map marker for the next character and is the simple reliable inheritance channel.

---

## Soft Memory

Lightweight world remembrance of a previous character through rumours, dialogue hooks, and opinion shifts. Not bloodline power inheritance.

---

## Difficulty Pressure

World creation rule that adjusts non-player faction advantage by difficulty: Easy favours the player's Starting Faction; Normal slightly favours opponents; Hard and Hell strongly favour opponents.

---

## Yield Threshold

An NPC's internal point at which survival matters enough to offer information or surrender terms before death or total defeat.

---

## World Collapse

The terminal transition of a world after Balance is irreversibly broken and a dominant faction achieves its objective. It is caused by world state, never by a timer.

---

## Decision Engine

The systemic rules that make every important player choice interesting. Foundation for quests, exploration, combat, progression, factions, and world interaction.

---

## Decision Node

One important player choice moment designed with the Decision Node Template. It activates Decision Axes, preserves Partial Uncertainty, produces Readable Consequences, and leaves world residue.

---

## Point of Interest

A seed-driven map opportunity that can host a Decision Node or discovery beat. It stays computationally dormant until the player enters its interaction area or its Active Window expires.

---

## Active Window

The finite lifetime of a Point of Interest measured in player world-time. Missing the window causes unattended resolution.

---

## Lazy Activation

Performance and design rule: full encounter logic for a POI starts only when the player enters its interaction area. Timers and abstract world residue still advance without staging the scene.

---

## Unattended Resolution

Outcome applied when a POI's Active Window ends without player participation. Weighted by Faction Influence and local state, never by opaque meaningless randomness. Leaves Symptoms or other Readable Consequences.

---

## Decision Axis

One of five minimal decision categories: Attention, Approach, Allegiance, Commitment, Bargain. Important nodes activate at least two axes.

---

## Attention

Decision axis: engage the unknown, or defer/ignore it. Primary V1 teaching axis.

---

## Approach

Decision axis: choose how to act when multiple methods remain viable. Immediate companion to Attention in V1 teaching.

Also used for one concrete method from the V1 set in [06 Approaches](06%20Approaches.md): Confront, Convince, Threaten, Exchange, Veil, Withdraw.

---

## Confront

Approach: force the issue openly, including fight or open defiance. Hard constraints live in [08 Confront Constraints](08%20Confront%20Constraints.md).

---

## Break Away

A pressured escape from Confront. A costly form of Withdraw after violence has begun.

---

## Diegetic Loot

Rewards that exist because a target carried or guarded them, not because a fight completion spawned abstract treasure.

---

## Turn-Based Mode

Deliberate paced control mode used only for Confront, on a **Combat Arena** scene ([16 Architecture Pivot](16%20Architecture%20Pivot.md)). Never available voluntarily during free exploration and never begins in the current overworld/interior location. Reference feel from Baldur's Gate 3 / Fallout 1–2 without requiring feature parity.

---

## Turn Action Economy

V1 turn structure: Move + Action + optional short Bonus list. Used in Confront and non-combat Turn-Based Mode.

---

## Action

The main thing a character does on their turn: strike, persuade, threaten, deal, sneak attempt, Break Away, Spare, and other decisive verbs.

---

## Bonus

A small extra act on a turn from a short V1 list, such as a quick item or brief adjacent interact.

---

## Vertical Slice

The smallest playable prototype that proves the game fantasy and core systems without building the full game.

---

## Lower Veil District

Placeholder name for the Vertical Slice district: a poor/industrial urban area of medicine, night routes, factory pressure, and Warden order.

---

## City District

**Pre-pivot / archive:** a named region of the retired seed town scaffold (Ridge, Lower Veil, Undercroft) — radial wedges from a market center. Not a reuse source. Post-pivot, "Lower Veil" survives only as a Vertical Slice fantasy/POI-naming label, not a spatial scaffold. See [13 City Generation](13%20City%20Generation.md) (deprecated).

---

## Anchor Site

**Pre-pivot / archive:** a mandatory landmark / NPC / POI placement that every retired city regenerate had to include. Post-pivot, deterministic POI presence is owned by [W.4 POI Placement](17%20Game%20Systems%20Architecture.md) in [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), not this term.

---

## Grid World

**Pre-pivot term.** Post-pivot, square-grid gameplay truth applies on **Combat Arenas only**. Overworld and interiors are free third-person spaces. See [16 Architecture Pivot](16%20Architecture%20Pivot.md) and [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md).

---

## Architecture Pivot

2026-08-07 presentation split: Valheim-like seeded overworld, Genshin-like free third-person move, separate interior scenes (no grid), Confront on thematic Combat Arenas. Authority: [16 Architecture Pivot](16%20Architecture%20Pivot.md).

---

## Overworld Chunk

Seeded exploration zone of the overworld (biome, terrain, POI slots, creature spawns). **V0 chunk size = 64 m**; the V0 playable world is **2800 × 2800 m** (2×2 Unity Terrain tiles of 1400 m), with the chunk API written as if the world can expand. Streamed/chunked intent like Valheim-shaped worlds. No harvest loop as core progression. See [16 Architecture Pivot](16%20Architecture%20Pivot.md) and [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).

---

## Game Systems Architecture

Draft document [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md): systems map authority — System Cards, dependency DAG, micro-iteration backlog. Does not override spatial locks in [16 Architecture Pivot](16%20Architecture%20Pivot.md).

---

## System Card

Standard description of one gameplay system in [17](17%20Game%20Systems%20Architecture.md): responsibilities, dependencies, public interfaces, data flow, save/load, multiplayer, performance, extensibility, V1 scope, owning folder.

---

## Interior Scene

Separate scene (or equivalent isolated space) entered from an overworld door/portal. Free third-person movement; **no grid**. Generated or templated from Building DNA / recipes. See [15](15%20Procedural%20Building%20Generation.md) and [16](16%20Architecture%20Pivot.md).

---

## Combat Arena

Dedicated Turn-Based Confront scene with square-grid Move + Action + Bonus. Chosen from a Theme Pack matching the source location mood; not a 1:1 clone of exploration geometry. Return applies residue. See [08](08%20Confront%20Constraints.md) and [16](16%20Architecture%20Pivot.md).

---

## Theme Pack

Greybox arena kit tagged by mood (street, tavern, cellar, woods, …) used when Confront starts. See [16 Architecture Pivot](16%20Architecture%20Pivot.md).

---

## BuildingTemplate

**Pre-pivot / archive:** reusable schematic interior cell paint for continuous OrganicTown enterable buildings (D1). Deprecated and not a reuse source — post-pivot interior content targets **Interior Scenes** built fresh by Piece-Based Assembly. See [15](15%20Procedural%20Building%20Generation.md).

---

## Building DNA

Seeded identity of one building instance (archetype, district, wealth, style, LandmarkClass, InteriorPolicy, and interior stubs such as secrets/occult). Upstream driver for **Exterior Shell Grammar** and, when Portal, interior scene assembly. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Exterior Shell Grammar

Ordered, DNA-driven assembly of overworld building exteriors from an Exterior Piece Kit on a modular Bay/Storey grid: foundation → storeys → roof → vertical → trim, constrained by PlotFootprint and StreetContext. Background fabric does **not** build a Room Graph. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Shell Non-Overlap Rule

Hard lock for Exterior Shell Grammar: floor-stack slabs and shell pieces must abut without intersecting. Build bottom-up by storey; party bays emit nothing; step faces use AtticWindow; chimneys sit flush on the roof. Enforced by `ShellOverlapGuard`. Spec: `unity/Assets/Plans/SHELL_NON_OVERLAP_RULE.md`.

---

## AtticWindow

Opening type on a **step face** — the wall of a continuing storey that overlooks a lower roof where another slab ended. Distinct from standard storey `Window` so art/kit can treat dormer/attic lights separately.

---

## StreetContext

Per-face classification of a building plot (`Street`, `Alley`, `Yard`, `Square`, `Party`) used by Exterior Shell Grammar to place doors, shopfronts, windows, and blank party walls. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## PlotFootprint

Building pad for Exterior Shell Grammar: bay width/depth, shape (V1 rectangle / corner), orientation. **Not** the full land lot — that is Plot Parcel. Selected after Property zoning, before archetype/DNA sampling. Distinct from the assembled ShellInstance. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md`.

---

## Plot Parcel

Geometric land lot (frontage × depth, origin, yaw) subdivided from a settlement block. May be larger than the building pad. Owned by Settlement / Property generation (`PlotParcel`). See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md`.

---

## Property (Settlement)

Logical unit on a Plot Parcel: property archetype, zones (front garden, rear yard, service, …), and a BuildingPad (`PlotFootprint`) for Shell. Not ownership simulation. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md`, [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) W.5.

---

## Property Archetype

Immutable fabric recipe (`PropertyArchetypeDef`) that drives lot size, setbacks, and which property zones exist for a `DistrictKind` (e.g. PoorRow, WorkerYard, AffluentGarden). Shell silhouette still comes from DistrictProfile + building DNA. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md`.

---

## Property Boundary

Fence, iron fence, or wall edge segments around a Property lot, optionally with a front gate. Lab uses greybox placeholders (`PropertyBoundarySegment`); production TownDress meshes come later. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S2).

---

## Property Prop

Dressing placed inside a Property zone (garden lamp, planter, crate, …) via `PropertyPropCatalog` rules. Tagged with PropSemantic so fantasy/industrial/occult pieces have a reason to exist. Street and building-mounted props are separate systems. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S3).

---

## PropSemantic

`Domestic` | `Service` | `Industrial` | `Occult` | `Civic`. Semantic tag on Property Props — prevents random fantasy clutter. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S3).

---

## Settlement Lane

Service / rear lane strip between property frontages inside a settlement block (`SettlementLaneSpec`). Used by double-frontage and rear-lane parcelizers (S4). Not a full StreetNetwork. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S4).

---

## Settlement Street

Primary or cross street ribbon (`SettlementStreetSpec`) on the Settlement Street Spine. Centerline + width only — not a pathfinding graph and not OrganicTown `TownRoadGraph`. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S5).

---

## Street Prop

Dressing on a Settlement Street ribbon (lamp, bollard, curb bench, paving marker) via `StreetPropCatalog` / `StreetPropPlacer`. Distinct from Property Prop (lot zones). Lab greybox; TownDress meshes later. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S8).

---

## Settlement Fabric

Composable settlement layout (`SettlementFabricLayout`): street ribbons + one or more property blocks with aggregated Properties/Plots for Shell. Lab S5 Street Spine; S8 adds `StreetProps`. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S5–S8).

---

## Settlement Pose

World placement for a Settlement Fabric: terrain position, cardinal yaw, settlement seed, and source TownApproach POI id (`SettlementPose`). Built by `SettlementApproachBinder` (S6). See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S6).

---

## GardenSquare

Public / semi-public open-space Property (`IsOpenSpace`, zone `OpenSpace`) with civic props and no Exterior Shell. Used in Settlement District Mix (S7) between Commercial and Worker blocks. See `unity/Assets/Plans/SETTLEMENT_PROPERTY_V0.md` (S7).

---

## LandmarkClass

`Background` | `Notable` | `Landmark`. Background/Notable use Exterior Shell Grammar; Landmark uses a handcrafted prefab + DNA stub and skips fabric grammar. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## InteriorPolicy

`None` (overworld shell only) or `Portal` (shell exposes a door link to a separate interior scene). Most fabric buildings are `None`. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Bay / Storey

Exterior modular grid units: Bay = 2 m (plan), Storey = 3 m (height). Locked for V1 Piece authoring and shell assembly. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Room Graph

Graph of rooms and circulation edges for an **interior scene** (Portal buildings only) — the *result* of Interior Piece Assembly, not an algorithmic subdivision. Not used for background overworld shells. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Piece

A small, hand-authored modular unit of geometry (exterior wall/window/roof bay, or interior room segment, or street/block module) carrying Snap Sockets on its boundary. Base unit of Piece-Based Assembly. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Snap Socket

A typed connection point on a Piece's boundary (wall-run, street-frontage, door-bay, corridor-end, …). An assembler may only join two Pieces through compatible sockets. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Piece Kit

The curated library of Pieces available to an archetype or district. **Exterior Piece Kit** dresses shells; **Room Library** is the interior kit for Portal buildings. Never ships whole-building prefabs as fabric authority. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Piece-Based Assembly

Locked generation mechanism: deterministic seed + DNA selects and snaps compatible Pieces small → large. Exterior path: Piece → storey → Shell → Block/Settlement. Interior path (Portal only): Piece → Room → Floor → Interior Scene. Reference: Valheim room/dungeon pieces. Replaces subdivision solvers and retired OrganicTown continuous-mesh ([13 City Generation](13%20City%20Generation.md), deprecated). See [15](15%20Procedural%20Building%20Generation.md), [17](17%20Game%20Systems%20Architecture.md).

---

## Modular Building Pipeline

DNA → Plot/StreetContext → Exterior Shell Grammar → optional Portal → Interior Piece Assembly (room graph) → dress/secrets/bindings. Whole-building prefabs forbidden as fabric authority; Landmark prefabs and module prefabs allowed. See [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).

---

## Building Socket

A placed NPC or prop anchor inside a building instance / interior scene. Carries a stable role (Keeper, Counter, Guard, …) so the narrative layer can attach content without rescanning geometry. Pre-pivot D1.3 notes: `unity/Assets/Plans/MILESTONE_D1_PLAN.md` (archive).

---

## Building Occlusion

**Pre-pivot (D2):** hide exterior walls while inside a continuous-world building volume. Largely unnecessary when interiors are separate scenes. Archive: `unity/Assets/Plans/MILESTONE_D2_PLAN.md`.

---

## Playable Radius

**Pre-pivot (P0):** limited metres where OrganicTown v2 was rasterized into a walkable `GridMap`. Superseded as world movement model by free 3P overworld. Archive: `unity/Assets/Plans/PLAYABLE_TOWN_P0_PLAN.md`.

---

## TownGridBaker

**Pre-pivot:** baker that painted a square `GridMap` from OrganicTown roads/interiors inside the Playable Radius. Archive only under post-pivot locks.

---

## Procedural Town Asset Library

Long-term dress library: tileable materials, doors, windows, roof elements, trim, and props for exterior shells and interior scenes. Not complete city scenes. Priority: materials → architectural modules → doors/windows/roofs → props → decals → vegetation → FX last. See `unity/Assets/Plans/PROCEDURAL_TOWN_ASSET_LIBRARY.md`.

---

## Environment Action

On **Combat Arenas**, a turn verb that uses tagged scenery — vault/jump, climb, mantle, breach — usually as an Action with readable failure costs. Overworld/interior interacts are diegetic continuous-space actions, not grid turn verbs.

---

## Information Source

A reusable channel through which the player learns partial world or POI information, such as notice boards, newspapers, bartenders, rumours, faction contacts, witnesses, physical traces, or Soft Memory.

---

## Hinted

Discovery state where the player knows a region, type, or urgency of a POI without an exact map pin.

---

## Marked

Discovery state where a POI is visible on the world map.

---

## Convince

Approach: change minds through persuasion, appeal, or shared interest, without fear as the main lever.

---

## Threaten

Approach: compel through fear, pressure, or ultimatum.

---

## Exchange

Approach: trade value for value — items, favours, services, secrets, or debts.

---

## Veil

Approach: act with limited visibility — stalk, eavesdrop, sneak, or misdirect.

---

## Withdraw

Approach: leave, delay, or refuse engagement. Meaningful because the world continues.

---

## Allegiance

Decision axis: choose whose interests take priority when interests conflict. Neutrality counts and is never free.

---

## Commitment

Decision axis: spend or preserve scarce stocks such as resources, trust, safe routes, world-time, or one-shot opportunities.

---

## Bargain

Decision axis: accept a Price of Power, or refuse the advantage. Second major V1 teaching lesson after Attention + Approach. Any external power is a Bargain.

---

## Partial Uncertainty

Information state before important choices: investigation and Knowledge can narrow likely outcomes without collapsing the decision into a solved equation.

---

## Readable Consequence

A consequence the player can eventually interpret as causal. Every important decision must produce at least one.

---

## Failure Value

The requirement that failure produce Knowledge, a World Delta, or a New Opportunity. Failures that only waste time are invalid.

---

## World Delta

A readable change in world state caused by player action, failure, or World Engine activity.

---

## External Power

Capability granted by otherworldly entities, churches, deities, or similar sources. Always treated as a Bargain with a readable, often delayed and escalating price.

---

## Pathway

A staged character-growth track distinct by way of acting. V1 has three Pathways and only stages 9 → 7. Lore names come later.

---

## Stage

A numbered depth on a Pathway. Lower numbers are deeper advancements with sharper prices. V1 uses 9, 8, and 7 only.

---

## Conduct Progress

Between-stage advancement evidence earned by living a Pathway's way of acting. It is not kill-XP and must not become a permanently solved checklist.

---

## Formula

Discoverable advancement knowledge required for a Pathway stage. Concrete formulas vary by World Seed.

---

## Pathless

A character state with no Pathway yet. Valid and sometimes freer, but without staged Pathway advancements.

---

## Canonical

Official project truth.

---

## Draft

Work in progress.

---

## Deprecated

Historical documentation kept for reference.

---

## Road Passability

Which ground a road may occupy, as opposed to what that ground costs to build on. **Mountain terrain is traversable, not a hard obstacle.** Roads may cross it; the planner is expected to prefer cheaper ground — passes, contours, switchbacks — over a direct expensive climb, and it expresses that preference as **price, not refusal**. `MountainPenalty` and `MountainCost` are soft costs and must never become `+Infinity` merely because the biome is Mountain. Only three conditions block a route outright: water / `+Infinity`, earthwork beyond `MaxCutDepth` / `MaxFillHeight`, and grade at or above `MaxWalkableGrade`.

Passability is a property of the ground and never of what was built beside it. `ExistingCorridorDiscount` expresses corridor-reuse preference in **construction cost only** and must not reach any hard-obstacle predicate — read passability from `LandscapeCostField.TerrainCost` / `TerrainCostAtTerrain` (undiscounted) and construction cost from `Evaluate` / `EvaluateAtTerrain` (discounted, what A* prices). **Approved but not Canonical** — reviewed and approved by the project owner 2026-08-31; awaits only a Canonical host document. Source: [WORLD_GENERATION_PIPELINE.md](unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md) §3; see [AGENTS.md](AGENTS.md) § Accepted Draft Decisions and [99 Open Questions](99%20Open%20Questions.md) § Resolved Decisions.

---

## Primary Road

Overworld road tier connecting TownApproach hubs via a growing Prim network that snaps onto already-accepted corridors. See `RoadTier.Primary` and [LANDSCAPE_PLANNER.md](unity/Assets/Plans/LANDSCAPE_PLANNER.md).

---

## Secondary Road

Overworld road tier attaching non-town POIs to the nearest good point on the existing road network (Primary + accepted Secondary). See `RoadTier.Secondary`.

---

## Local Road

Overworld road tier reserved for short stubs to buildings / props. Enum and widths exist; generation deferred. See `RoadTier.Local`.

---

## TerrainBiomePalette

Static remaps and albedo tile sizes for overworld `TerrainLayer` assets (Fields, Forest, Swamp, Dust, Highland, Rock, Snow, road looks). Applied at bake; read by MicroSplat / URP TerrainLit layers (custom `GameDev/Overworld/Terrain` deleted). Spec: [OVERWORLD_VISUAL_DIRECTION.md](unity/Assets/Plans/OVERWORLD_VISUAL_DIRECTION.md).

---

## RoadSurfaceStyle

Tunables for packed-earth road alphamap paint: soft edge width (0.5–1.5 m), bed/transition coverage, gravel/ruts. Roads append their own `TerrainLayer`s **after** the terrain bake and must never reorder or repaint the canonical biome indices 0–4. See `RoadSurfacePaint`.

---

## Overworld Terrain Shader

**Retired** custom URP TerrainLit fork (`GameDev/Overworld/Terrain`) — deleted. Current SoT: **MicroSplat Core** (full shader needs MicroSplat URP adapter); until then TerrainLit + MicroSplat example textures via `OverworldMicroSplatSetup`. Do not reintroduce the custom fork.

---

## SeaLevelOceanSampler

`IWaterSampler` that reports water when terrain height is at or below Sea Level. Used by roads / POI / vegetation after inland lake/river systems were removed. See `World/Water/SeaLevelOceanSampler.cs`.

---

## Sea Level

World Y of the single **ocean** water plane (`WorldScale.SeaLevelMeters`, default **24 m**). Full-square terrain continues under this as shelf/bathymetry; dry island sits above. WaterWorks `WorldWater` is placed here. Inland lake intersection is **not** generated in V0.

---

## Coastal Height Transition

How the island reaches the waterline. **Entirely TerrainGen, no post-pass** — the Stage 1 landmass owns the coastline and its seabed, and the terrain bake writes that relief unchanged. The shore surface is a world-space rule: ground below the vegetation floor (27.0 → 28.4 m) is sand, and seabed rock is suppressed at the 24 m waterline so coastal land never receives it. The old directed coast height pass (`CoastalBeachShelf`) is **deleted**; do not reintroduce a post-generation coast height pass. See [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md).

---

## Island Occupancy

**Deleted.** Was a post-geography C# height pass enforcing tile-border submersion. Border submersion and the silhouette now belong to TerrainGen's Stage 1 landmass. `IslandNaturalGeography` is deleted with it. Historical term only; do not reintroduce a post-generation height pass.

---

## WorldWater

Single overworld ocean GameObject (WaterWorks `Water_Plane` + `SSR_Water`) under `OverworldWater`, Y = Sea Level, covering the tile. V0 has no inland lake/river meshes.

---

## WaterWorks

Third-party URP water pack (`Assets/WaterWorks/`). Overworld SoT for the single water plane. `Water_Volume` renderer feature is stubbed for Unity 6 — do not enqueue. Not a river/waterfall mesh kit. See [ASSET_LIBRARY_REFERENCE.md](unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md) and [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md).

---

## Terrain Bake

The overworld terrain pipeline: **TerrainGen → Unity `TerrainData` → Unity Terrain**. `OverworldTerrainBake` creates the 2×2 tiles of 1400 m (height budget **700 m**), evaluates TerrainGen's authoritative relief onto one world lattice (**Stage 1 landmass + Stage 2 massifs + Stage 4A island relief**, i.e. `H4A` — Stage 3C–3E topology exists but is not read), applies a pending hydrology carve, and writes heights, surface weights, trees and ground cover through `TerrainData`'s own API. Surface and vegetation are project-owned world-space rules (`TerrainSurfaceRules`, `TerrainVegetationRules`); randomness is stateless per world cell from the world seed. Seams are exact by construction. Terrain shading = five canonical `TerrainLayer`s → **URP TerrainLit**; MicroSplat is out of the pipeline. Water look = `Assets/Settings/Water/OverworldWater.mat` (clone of WaterWorks SSR_Water). See [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md).

---

## Island Relief (Stage 4A)

TerrainGen's floor relief: what makes the island's middle read as geography instead of as an extruded coastline. Composition `H4A = H1 + U2 + R4A` with `R4A = M(d) + Γ·(P + K) + A`: a 1-D **coastal floor** `M` that gives the Stage 1 profile an inland slope of at least τ = 0.05, **plate-block uplift** (macro-plate districts with a 20 % lowland reservation), **knolls** (resistant crustal cores, slope-budget capped; none placed on the canonical seeds under the analytic bound) and the **massif apron** (talus and piedmont, domain-limited to the massif axis reach). All gated so the land mask cannot move. Noise has no role. **The production terrain authority since 2026-09-18** (`GeneratorVersion` 2): `TerrainAuthorityField` evaluates `IslandElevation`, so the bake, the surface rules and vegetation all read `H4A`. Stage 3D/3E still declare their basins and outlets on `StructuralElevation` (`H2`), the surface they were frozen against; Stage 3F carves `H = H4A − C3F` after this layer. See [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md) (R1 folded in) and [the revision change record](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md).

---

## Structural Height

**Proposed — owner review pending.** The TerrainGen surfaces that structural stages are built and frozen against: `H1`, `H2` (`StructuralElevation`) and `H4A` (`IslandElevation`, what `TerrainAuthorityField` evaluates). Stages 1–4A, the frozen Stage 3C trajectories, Stage 3D/3E topology and structural diagnostics read structural height only — never the **Final Terrain Surface**. See [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) §22.1 and [99 Open Questions](99%20Open%20Questions.md) § Terrain — Stage 3F, 3F-1.

---

## Final Terrain Surface

**Proposed — owner review pending.** The one height written to `TerrainData`: a lattice on the canonical world grid, at or below `H4A` at every node, read as a **Triangle Reading**. Stage 3F's E would be it; today the bake writes `H4A`, and E is a research prototype not connected to the bake. Nothing flows from it back into TerrainGen. E is not `H3F`: `H3F = H4A − C3F` is the existing Stage 3F carve, E = min(`H4A`, fill(Epre)) adds a floor-referenced plain template and a band-local correction; the statement under **Island Relief (Stage 4A)** that Stage 3F carves `H = H4A − C3F` after it describes the existing code and changes only if 3F-1 is approved. Handoff §22.1–22.2; 3F-1, 3F-6, 3F-25.

---

## Triangle Reading

How Unity serves the height between heightmap nodes, measured (handoff §18): nodes as `TerrainData` stores them (integer steps of `size.y / 32766`, 2.136 cm on a 700 m tile), each cell split along its **SW→NE** diagonal, (x0, z0) → (x0+1, z0+1) — for local coordinates u ≥ v the triangle (h00, h10, h11), otherwise (h00, h11, h01) — with tile-edge vertices shared exactly. World x points east and z north; with lattice spacing `step`, x0 = ⌊x / step⌋, z0 = ⌊z / step⌋, u = x / step − x0, v = z / step − z0, and h_ij is the stored height of node (x0 + i, z0 + j). `Terrain.SampleHeight` and the `TerrainCollider` match it within 0.4 mm; bilinear interpolation does not (up to 2.59 m at twisted cell centres). **Proposed** as the point-query contract of the **Final Terrain Surface** (3F-6).

---

## Walkable Triangle

**Proposed — owner review pending.** A triangle of the **Triangle Reading** whose slope is **strictly below 45°**. Measured with the production `CharacterController` (handoff §21): planar 44.995° always walks, 45.005° never, exactly 45.000° depends on the direction of approach. Descent works on any tested slope; this term is about walking up. 3F-18.

---

## T-edge Connectivity

**Proposed — owner review pending.** Walking connectivity on the graph of **Walkable Triangle**s of land, moving only across shared edges, from walkable land triangles that share an edge with the ocean. Undirected: it describes routes walkable both ways. The proposed **conservative lower bound of guaranteed walking connectivity** in the verified configuration — no false "passable" was observed (handoff §§20–21) — not a proof for every island. Not to be read as exact: the controller also walks some vertex passages T-edge excludes (**Uncertain Connectivity Zone**). 3F-18.

---

## T-vertex Connectivity

**Proposed — owner review pending.** **T-edge Connectivity** plus moves through a shared vertex. An **experimental upper bound**: it does not guarantee walkability — one vertex step adjacent to a 45.41° face stopped every ascent (handoff §21). 3F-18, 3F-23.

---

## Uncertain Connectivity Zone

**Proposed — owner review pending.** Land reachable by **T-vertex Connectivity** and not by **T-edge Connectivity**. Read as neither accessible nor inaccessible until vertex passages are studied (3F-23); measured example: about 25 thousand m² on seed 1010 that T-edge excludes and the controller walks.

---

## Jump Accessibility

**Proposed — owner review pending** as a notion separate from walking connectivity (handoff §22.7); the measurement below is recorded, not proposed. What the production jump (`jumpHeight` 1.2 m, gravity −18, jump while grounded) reaches — separate from walking connectivity. Measured unconstrained by slope up to 70° on every tested surface; no graph model describes it (handoff §21). Whether steep walls should stop a jumping player is open: [99 Open Questions](99%20Open%20Questions.md) 3F-20.

---

## Slope Reading

Labels for the ways the code reads slope (handoff §19), unrelated to the Stage 3F research requirements D1–D3: **T** — the served triangle's gradient (collider, `CharacterController`); **D2** — `TerrainData.GetInterpolatedNormal` (Sobel vertex normals, tile-local; `OverworldTerrainSampler.SampleSlope01`, biome); **D3** — `OverworldTerrainField.SlopeDegreesAt` (central differences, 5.47 m base; surface and vegetation rules); **D4** — `LandscapeCostField`'s 10 m slope (road cost, POI site scoring); **D5** — TerrainGen diagnostics (`WorldHeightField.Slope01`, `Stage3Field.Reachable`); **D6** — `OverworldTerrainDiagnostics` reachability (per-axis node steps, passes faces up to 54.7°); **D7** — the analytic `H4A` gradient. **Proposed:** only T decides a character's walkability, an object's footing, spawn or a character's access gating; D2, D3, D6 and D7 never do, and D4 keeps only its own uses, road cost and POI site scoring (3F-19). Road passability is not affected: it stays under the approved **Road Passability** contract.

---

## Geographic Hierarchy

**Vocabulary proposed — owner review pending; the target it serves was approved on 2026-10-20** as D6 in [GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md) §9 (a regional geography with a landform grammar, from the owner's concept image — a project reference, not an official DragonSword: Awakening map). The levels the overworld's geography is described and checked in, each caused by and placed relative to the level above it: **level 0** island footprint, **level 1** relief regions (highlands, uplands, lowland plains and corridors), **level 2** mountain system (main divide, culminations, saddles, major and subsidiary ridges, spur-toe foothills), **level 3** valley and drainage system, **level 4** landforms (plateaus, escarpments, outcrops, knolls, basins), **level 5** coast, **level 6** water. Not a set of generators and not a new terrain authority: a vocabulary for what TerrainGen's stages must jointly produce, with relationship rules such as "one divide tree", "valleys are complements of ridges" and "coasts are caused by what stands behind them". Its **single-dome failure** is an island whose height is essentially a function of coast distance plus one mound. Distinct from the relief *tiers* T1–T3 of `WORLD_RELIEF_PRINCIPLES.md`, which classify forms by height and footprint, and from the heights `H1`, `H2`, `H4A`, which are surfaces, not levels. Since the post-G3c review (2026-10-21): the level-2 mountain system is what the relief tier T1 names — one branching system counts as one T1 (GEO-2, resolved); whether D6's large plateaus become a region-level class outside the tier table is proposed, not decided (**Large Plateau**, GEO-8). See [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §§4, 13, [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2 and [99 Open Questions](99%20Open%20Questions.md) GEO-2, GEO-7, GEO-8.

---

## Large Plateau

**Proposed — owner confirmation required (GEO-8 option (c), 2026-10-21).** A plateau of the approved target D6 that is a **relief-region form** (Geographic Hierarchy level 1), not a relief tier: a readable top behind an escarpment on part of its rim, joined to a ridge or foothill on another part, subordinate to the mountain system (its top below the system's main culminations; never the landmark). Separated from T1 by form rather than by the 2.5× height rule, as T0 floor relief is separated from T2 by slope; the 2.5× rule applies to forms standing on it. Distinct from a **T2** plateau, the district-scale climbable shelf of `WORLD_RELIEF_PRINCIPLES.md` §1, whose row is unchanged. A tier or form label says nothing about traversal: whether its escarpment blocks the player is read from steepness and walking connectivity and decided under 3F-20. Its height range, top area, count and subordination margin are research items. See [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2.7 and [99 Open Questions](99%20Open%20Questions.md) GEO-8.

---

## Declared Basin

**Policy resolved 2026-10-21 (GEO-5 (b), evidence-backed policy).** A closed basin the geography itself declares — its enclosure, spill point and one outlet continuing to the ocean — such as an intermontane hollow, a cirque or a plateau-top hollow. Only a declared basin may hold a lake (surface at the spill level, uncarved outlet sill, `HYDROLOGY_GENERATION_PLAN.md` §9) or be realised as a filled flat (3F-10, 3F-11, 3F-14 decide realisation). Any closed depression that is not declared is a defect — an accidental pit under the S8 rule — even when the hydrology analysis's fill-then-spill would report it as a lake. Stage 3E's structural basins are declared basins of today's structure. No endorheic basin: the ocean-outlet invariant stays. V0 water remains ocean only. See [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2.5 and [99 Open Questions](99%20Open%20Questions.md) GEO-5.

---

## Geometry-First Review

**Tooling approved (GEO-6 (a), 2026-10-18); built as `Editor/GeometryFirstReview.cs`.** A diagnostic presentation of a terrain lattice for judging geography without materials: heights only, written into tiles that are never saved; a neutral material; no vegetation, props, roads, water surface, fog or post-processing; one fixed sun; a fixed camera set anchored on Stage 1 so captures of different algorithm versions on one seed are comparable; overlays for slope, contours, watersheds, drainage and declared structure. It reads any lattice (production `H4A`, a Stage 3F candidate, an experiment) and decides no height. It is not a bake mode and not a second terrain path. Its camera set and presentation as built are in [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §3.6; the geography target it serves is still proposed. See §3.

---

## Terrain Surface Stack

The five canonical overworld `TerrainLayer` assets in `Assets/Settings/TerrainLayers/Overworld/`, in `TerrainSurfaceLayer` order: **0 Grass, 1 Sand, 2 Soil, 3 Cliff, 4 Rock**. The terrain bake assigns the same array to every tile and `TerrainSurfaceClassifier` writes weights in the same order: shore → sand, open land → grass, drainage → soil, ridges / high massif ground / bare steep ground → rock, sheer faces → cliff, each applied by priority cover so the weights sum to 1. Rendered by **URP TerrainLit** — MicroSplat is out of the pipeline. Roads and settlements may append their own layers afterwards but must never reorder or repaint indices 0–4. Diagnostics: `GameDev/Overworld/Terrain/Dump Terrain Geometry` and `OverworldTerrainValidation`. Operator rules: [OVERWORLD_VISUAL_DIRECTION.md](unity/Assets/Plans/OVERWORLD_VISUAL_DIRECTION.md) § Terrain surfaces.

---

## Snow Line

Metres **above Sea Level** where snow paint may start (`WorldScale.SnowLineMeters`, default 95 m) — not a raw 0–1 heightmap fraction. Steep faces stay rock.

---

## One Click Add Water

Third-party stylized water pack (OCW). **Demoted** — unused stylized fallback. Prefer WaterWorks `WorldWater`. Do not spawn per-lake OCW planes. Custom `LakeWater` removed. See [ASSET_LIBRARY_REFERENCE.md](unity/Assets/Plans/ASSET_LIBRARY_REFERENCE.md).

---

## Inland Hydrology

**Retired.** Inland lake and river generation was removed from V0 on 2026-08-15, and the constants, placers, scorers, basin diggers, water-zone meshes and their glossary entries were deleted with it. V0 water is **one ocean plane** (see **Sea Level**).

The deleted implementations are **not** a foundation to build on — that is the explicit lesson recorded in [14 World Generation Roadmap](14%20World%20Generation%20Roadmap.md) § Future Hydrology / River Generation, which also states what a future system would have to model. Do not reintroduce `LakeDefinition`, `RiverTracer`, `WaterZone_*` hierarchies, or `WorldScale.Lake*` / `River*` / water-inset constants.

---

## POI Placement Profile

Per-`PoiKind` hard constraints, soft preferences, relation rules, and optional footprint scoring used by `PoiPlacementEngine`. Adding a new marker type means adding a profile, not rewriting the engine. See [17](17%20Game%20Systems%20Architecture.md) W.4.

---

## World Definition

Aggregate snapshot of one overworld generation: `WorldSeed`, `WorldGenSettings`, POIs (stable sorted markers), roads (`RoadDefinition[]`), and stats (road km, POI count). Scene holder: `OverworldWorldState`. Downstream systems read this instead of re-running placers. See [LANDSCAPE_PLANNER.md](unity/Assets/Plans/LANDSCAPE_PLANNER.md).

---

## World Sector Map

Equal-size 3×3 partition of the overworld tile used for TownApproach and secondary POI quotas. Preferred towns: four corners + center hub; unsuitable sectors fall back to the nearest habitable neighbor. See `WorldSectorMap` / [LANDSCAPE_PLANNER.md](unity/Assets/Plans/LANDSCAPE_PLANNER.md).

---

## Shape Irregularity

0–1 knob scaling low-frequency organic shore harmonics on a lake ellipse (default ~0.35). 0 ≈ perfect ellipse; ~0.45 = noticeable smooth bends; never fine tooth-noise.

---

## Terrain Conformity

0–1 knob scaling how strongly macro terrain (smoothed height / basin) pulls lake shore radius and orientation (default ~0.75). Limited so lakes stay elongated ellipses, not copies of heightmap noise.

---

## Pipeline Step

One Generate/Clear procedure in the overworld Editor pipeline: Surface, Zones, Mountains, Markers, Settlements, Roads, Trees, Undergrowth, Grass, Player (plus ensure-ocean). Lakes/WaterMeshes steps **removed** 2026-08-15. Output is a pure function of `WorldSeed.Fork(step)`, upstream artifacts, and `WorldGenSettings`. See [WORLD_GENERATION_PIPELINE.md](unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md) §5.

---

## Part-Gen

Editor menu under `GameDev/Overworld/Part-Gen/<Step>/` that runs the same Generate or Clear method as Generate All for one pipeline step. Used to iterate a single concern without rebuilding the whole tile (except Surface/Mountains, which cascade-clear hydrology and later steps).

---

## Mountain Occupancy

**(Retired V0 — inland lake generation removed 2026-08-15; kept for historical reading.)**

Hard rule for lake placement: no water silhouette cell may sit on `BiomeId.Mountain` (or steep highland). Soft-pass and min-count helpers must not violate it — fewer lakes beat a sheet through a ridge. Water meshes also safety-clip mountain cells above the water plane. See [LANDSCAPE_PLANNER.md](unity/Assets/Plans/LANDSCAPE_PLANNER.md).
