# 99 Open Questions

Version: 0.72

Status: Draft

Last Updated: 2026-10-31 (Ordinary item quality mapping finalized: Normal 0, Magic 1–2, Rare 3, Epic 4, Legendary 5–6 modifiers. Earlier: Crafting skill influences modifier-count probabilities; NPC equipment is assigned as part of NPC builds and only partly recoverable. Earlier: Base-item level affects modifier-tier probabilities without imposing a hard tier cap; base families use weighted rather than exclusive modifier pools. Earlier: Profession mastery affects reforging tier probabilities but imposes no hard modifier-tier cap. Earlier: Full regeneration of modifier type, tier, and value on reforging clarified. Earlier: Profession-mastery unlocks and base-dependent modifier weighting clarified; per-modifier learning requirement rejected. Earlier: Permanent modifier locking, escalating lock costs, and all-unlocked-modifier reforging rules clarified. Earlier: Major/minor modifier categories, tiers, weighting, and profession-based reforging direction clarified. Earlier: Base-item and modifier-driven itemisation direction captured. Earlier: Pathway attribute requirements, deterministic advancement, and sanity-loss creature outcomes documented. Earlier: Universal resource Stage Multiplier and maximum HP/Stamina formulas agreed; ordinary HP recovery confirmed; shared attribute/skill XP principles clarified; derived character statistics, Will Power, and Sanity model documented; GEO-10: G3f's calibration and held-out evaluation ran under the owner's research mission. Earlier: GEO-10: G3f's specification written under the owner's research mission. Earlier: G3f decisions D-3 … D-12 on content: pointers in GEO-3, GEO-10 and GEO-11. Earlier: the G3f phase plan approved — D-1, D-2: a GEO-3 line; GEO-10 and GEO-11 added. 2026-10-24: GEO-3: G3e-PM ran — registered D1 INCONCLUSIVE, H FAIL by §7, DEV-1; the S-LM1 line stopped. Earlier: the post-mortem checks G3e-PM pre-registered, plan approved, not run. Earlier: G3e Step 1 ended not ready; the report accepted, Step 1 closed. Earlier: S-LM3 closed with a negative result. Earlier: G3e diagnostic run 6. Earlier: G3e Step 1 state after five calibration runs — no synthesis accepted. Earlier: LMC = G3e; C8, C9 decided; LMC specification v0.4: A1, A3 and the whale-back option (b) approved; earlier the same day C2–C5 prepared. 2026-10-22: LMC design note approved as the research plan; Step 1 readiness check; LMC technical specification approved, §12 items 1–4; Step 1 not authorised. Earlier: drafted and checked on G3c data. 2026-10-21: LMC R5 extended to designated outlets, GEO-7 discrepancy closed; LMC note v0.2, the owner's revision; LMC pair approved, step 0 written; post-G3c review: GEO-1, GEO-2 resolved; GEO-3 … GEO-8 classified; GEO-9 added. Earlier: G3c evidence; G2: D6 approved)

Depends On:
All Canonical Documents

Referenced By:
Future Design, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [AGENTS.md](AGENTS.md), [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md), [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md), [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md), [GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md), [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md), [GEOGRAPHY_LMC_STEP1_READINESS.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md), [GEOGRAPHY_G3F_PHASE_PLAN.md](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md)

---

# Resolved Decisions

These were answered in design discussion and written into Draft documents. They remain here so agents do not reopen them casually.

## World Laws

**What changes Balance?**  
Faction Influence, raised mainly by completed Main Goals and shifted by Side Events / Allegiance choices. Documented in [02 World Laws](02%20World%20Laws.md).

**How does the World Engine choose the next faction operation?**  
From World Seed starting state plus each faction's current Main Goal, requirements, and eligible Operations. Documented in [02 World Laws](02%20World%20Laws.md).

**How does the player read Influence without a meter?**  
Through everyday Symptoms: trade, services, encounters, rumours, otherworldly pressure, and related world changes. Exact stage-to-symptom tables remain open below.

**Starting faction?**  
Only the first character in a new world begins with a Starting Faction. Later characters begin Factionless and must earn Faction Entry. Documented in [07 Character Life](07%20Character%20Life.md).

**Difficulty direction?**  
Easy favours the player's faction; Normal slightly favours opponents; Hard and Hell strongly favour opponents. Exact numbers remain open below.

## Decision Engine

**Are the five axes accepted?**  
Yes: Attention, Approach, Allegiance, Commitment, Bargain.

**Which axis does V1 teach first?**  
Attention first, Approach immediately with it; Bargain as the second major lesson. Documented in [03 Decision Engine](03%20Decision%20Engine.md).

**Do Side Events give free bonuses?**  
No. They are Allegiance forks with useful different outcomes, hidden Influence residue, and possible path open/close effects.

**What is Knowledge for?**  
Player Knowledge unlocks reasoning, important places, methods, and prediction. It is not primarily a success-rate bonus. Character pathway power is Progression design constrained by Price of Power and Bargain.

**External powers / churches / curses?**  
Always Bargains with readable, often delayed escalating prices. Detailed systems deferred to content lists; rules documented in [05 Progression](05%20Progression.md).

**Two-axis rule for V1 nodes?**  
Important Decision Nodes require at least two axes. Rare one-axis moments are allowed only as non-major onboarding, not as main forks. Documented in [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md).

**How are Side Events delivered in the world?**  
As timed Points of Interest with lazy activation, readable Active Windows, and Influence-weighted unattended resolution if missed. Documented in [04 Points of Interest](04%20Points%20of%20Interest.md).

## Progression

**How many Pathways in V1?**  
Three, distinct by way of acting, expandable later. Documented in [05 Progression](05%20Progression.md).

**Which stages in V1?**  
Only 9 → 8 → 7. Deeper stages later.

**How is a Pathway obtained?**  
Starting Faction soft-nudges toward one Pathway; Pathway may also be gained through Bargain, quest, or discovery. Not a prison. Pathless remains valid.

**What belongs in the first Progression draft?**  
Pathway rules plus churches/entities as External Power Bargains. Lore names, full lists, and stages below 7 come later.

**What preparation is required for a Pathway-stage transition?**  
Each transition requires exactly two primary attributes and one secondary attribute. The two primary attributes are stable across all stages of a Pathway; the secondary attribute is selected per World Seed for each Pathway-stage combination and remains fixed within that world. Requirements are evaluated for the target stage. Primary deficits cause substantially greater additional Sanity loss than secondary deficits; any shortfall causes a penalty, which grows with the gap. See [05 Progression](05%20Progression.md).

**Is Pathway advancement itself random, and what happens when sanity is lost?**  
No transition success roll: consuming the correct potion always grants the target stage. The mandatory transition Sanity cost and any preparation penalty are subtracted from current Sanity. If sanity is lost, advancement remains successful, but the character becomes a game-controlled creature in the world rather than an ordinary player character. At stages 9–6 the creature is human-shaped (zombie-like). At stages 5–0 a Pathway- and stage-specific mystical form becomes possible, but is only a chance; the character may remain human-shaped. See [05 Progression](05%20Progression.md).

## Items, Inventory, and Equipment

**How is ordinary item quality determined?**  
By total modifier count, not by a separate independent rarity roll: Normal = 0 modifiers; Magic = 1–2; Rare = 3; Epic = 4; Legendary = 5–6. Six is the maximum. Unique items are a separate bespoke category.

**How does crafting skill affect item quality?**  
The relevant profession's skill/mastery changes the probability of crafting an item with a given modifier count, and therefore its resulting quality. Exact probability curves remain open. Once the item is created, its modifier count and quality are fixed.

**How is NPC equipment handled?**  
NPCs receive equipment as part of their generated/assigned build, role, and individual characteristics, not through a random loot roll on death. They may have any quality, may be incompletely equipped, and need not follow player crafting progression. Only some equipment may be recoverable after death; alternatively, the player may negotiate or demand an item in exchange for sparing the NPC. Exact recovery rules and non-NPC acquisition sources remain open.

## Items, Inventory, and Equipment

**Must each modifier be separately discovered or learned before a profession can reforge it?**  
No. There is no per-modifier learning requirement. Reforging operations become available through profession mastery: standard reforging is intended to unlock at an advanced mastery stage, and permanent modifier locking at an expert stage. Once unlocked, the character can work with all modifiers eligible for the relevant item base. Exact mastery thresholds remain open.

**Are modifier types restricted to particular armour or weapon bases?**  
No hard class restrictions are intended. Modifier appearance is weighted by base-item family/type: fitting modifiers are much more likely, while off-archetype modifiers remain possible but rarer. Cloth, leather, and heavy/metal armour should each favour different attribute and combat themes; weapon families likewise favour effects suited to their use. Exact weight tables remain open.

**Can professions increase an existing item's rarity or add more modifiers?**  
No. An item's base properties, rarity, and number of modifier slots are established when the item is generated or created. Professions cannot raise its rarity or add modifier slots; they can only alter existing, unlocked modifiers through reforging. A player seeking a different modifier capacity or a higher-potential item must obtain or create another item.



**Does profession mastery impose a hard cap on the modifier tier obtainable through reforging?**  
No. Once the reforging recipe is unlocked, all modifier tiers from 9 through 0 remain possible. Profession mastery changes the probability distribution in favour of stronger tiers; it does not impose a hard tier ceiling. Even a low-mastery crafter may very rarely roll a level-0 modifier. Exact probability curves remain open.

**How does modifier reforging and locking work?**  
A standard reforging operation fully regenerates every currently unlocked modifier: its type, level/tier (9 through 0), and numerical value are all rolled anew according to the applicable probabilities and constraints. It does not preserve any of those properties from the previous modifier. The operation cost is independent of the number of modifiers changed. A separate locking operation permanently and irreversibly protects one chosen modifier. Any number of modifiers may be locked, but each successive lock on the same item costs substantially more resources. There is no single-modifier reroll operation: to reroll only one modifier, the player must lock all the others first. Exact costs, time, and lock-cost growth remain open. See [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md).

## Character Attributes, Skills, and Action Checks

**Are there global character levels or a character-wide XP bar?**  
No. Each attribute and individual skill has its own action-based progression track. Ordinary development is separate from Pathway stages. See [07.1 Character Attributes, Skills, and Action Checks](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md).

**What is the attribute range?**  
Seven attributes use 1–100. Levels 1–99 are ordinary progression; 100 is an exceptional apex requiring special criteria. Attribute bands are not automatically Pathway stages or skill levels.

**How do primary attributes and skills combine for an action check?**  
They have equal importance. Effective primary preparation is their arithmetic mean on a comparable task-difficulty scale. A high attribute can help a character enter a new profession, but it does not make the character a professional master. There is no additional mismatch penalty beyond the average.

**How do secondary attributes affect success?**  
Only positively. All applicable secondary attributes have equal weight; together they can add at most 10% of base success probability, applied relatively, not as percentage points. Low secondary values never reduce the baseline chance.

**How does Luck affect probability?**  
Luck is separate from the secondary-attribute bonus. It modifies existing probabilities relatively, not by adding fixed percentage points. It may separately affect overall success and conditional critical chance. Exact scaling remains open.

**What are the three action outcomes?**  
Failure, normal success, and critical success are mutually exclusive and sum to 100%. Critical chance is conditional on success, not a separate independent roll. Gathering animation cycles are individual attempts.

**What is the derived-statistics structure?**
The agreed register and principles are in [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md). Derived values are calculated from attributes, skills, equipment, Pathways, abilities, conditions, and context; they are not additional independent progression tracks by default.

**How do maximum HP, Stamina, and Will Power scale with Pathway stage?**
All three maximum resource pools use one universal Stage Multiplier S, regardless of Pathway type. A character without a Pathway uses S = 1.00. The agreed formulas are HPmax = (20 + 0.8 × Endurance) × S; Staminamax = (20 + 1.6 × Endurance) × S; and WPmax = (20 + 0.8 × Will) × S. The shared stage table is 9→0: 1.00, 1.15, 1.32, 1.52, 1.75, 2.01, 2.31, 2.66, 3.06, 3.52. Advancing a stage increases maximum capacity but does not automatically refill the current resource amount. See [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md). Equipment/effect stacking order, Stamina action costs, and other detailed interactions remain open.

**How do Will and Will Power work?**
Will is the ordinary 1–100 attribute; Will Power is a separate visible spendable resource. Pathways determine ability access, while Will determines capacity. Maximum Will Power is \((20+0.8\times Will)\times S\), with S determined by Pathway stage (9→0 multipliers: 1.00, 1.15, 1.32, 1.52, 1.75, 2.01, 2.31, 2.66, 3.06, 3.52). Ordinary regeneration is about 1% per 10 world ticks; sleep normally fully restores it. Full exhaustion grants one modest Will XP reward per continuous episode and causes Sanity damage. See [07.2 Derived Character Statistics](07.2%20Derived%20Character%20Statistics.md). Ability costs, exact XP eligibility, D₀, and effect-specific Will damage remain open.

**How does Sanity work?**
Sanity is a separate 0–100 mental-integrity measure, distinct from Will and Will Power. Its bands are 100 Stable, 70–99 Mild disturbance, 40–69 Significant disturbance, 10–39 Severe disturbance, 1–9 Critical, and 0 Loss of sanity. Shared Will mitigation is \(D_{sanity}=D_{base}\times100/(100+Will)\). Full-exhaustion damage is \(D_0\times S\times100/(100+Will)\). Ordinary Will Power recovery and sleep do not restore Sanity. At zero, the character permanently loses human personality and becomes a game-controlled creature in the world; this is distinct from death. See [07.2](07.2%20Derived%20Character%20Statistics.md). Exact band symptoms/penalties, D₀, source-specific mitigation, and recovery methods/rates remain open.

**How is carrying load handled?**
Strength determines Carrying Capacity. Four graduated bands are agreed: Normal, High, Overloaded, and Critical Overload. Thresholds and consequences remain open.

**How is defence represented?**
Two derived defence values are agreed: Physical Defence and Magical Defence. Special mechanics such as blocking, parrying, barriers, absorption, or reflection may come from specific equipment and abilities rather than universal extra stats.

## Approaches

**What methods exist in V1?**  
Six Approaches: Confront, Convince, Threaten, Exchange, Veil, Withdraw. Documented in [06 Approaches](06%20Approaches.md).

**Is combat the default best method?**  
No. Confront is one Approach among several and must not have the universal best payoff.

## Character Life

**When does the next character appear after death?**  
After a fixed short Death Gap during which the world resolves advanced state. Documented in [07 Character Life](07%20Character%20Life.md).

**Can Legacy be prepared intentionally?**  
Yes, via a simple V1 Legacy Cache with a next-life map marker. Ordinary leftovers remain unguaranteed.

**What connects lives besides Knowledge and Legacy?**  
Soft Memory: rumours, dialogue hooks, and opinion shifts. No bloodline power inheritance.

## Confront

**May combat become the main progression engine?**  
No. Confront constraints forbid XP-from-kills, abstract loot tables, and universal best payoff. Documented in [08 Confront Constraints](08%20Confront%20Constraints.md).

**What is V1 Confront presentation?**  
Turn-Based Mode on a **thematic Combat Arena** scene (BG3 / Fallout 1–2 feel). No voluntary Turn-Based Mode during free exploration. Documented in [08 Confront Constraints](08%20Confront%20Constraints.md) and [16 Architecture Pivot](16%20Architecture%20Pivot.md). *(Cancels prior in-location / outside-combat TB lock.)*

**How does the player learn about POIs and world pressure?**  
Through reusable Information Sources with bias and partial truth: boards, newspapers, bartenders, rumours, faction contacts, witnesses, physical traces, Soft Memory. Documented in [09 Information Sources](09%20Information%20Sources.md).

**What is the combat action economy?**  
Combat uses Initiative and an AP pool with variable action costs and retained end-of-turn AP for defensive reactions, as defined in [10.2 Combat System Draft](10.2%20Combat%20System%20Draft.md). The shared world advances by one tick after each complete round, not during individual turns ([18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md)).

**How do sleep, healing, and First Aid work?**  
Sleep advances approximately 6–8 in-game hours while the world continues simulating; it can be interrupted and is not guaranteed safe. It fully restores ordinary HP only when no active condition blocks recovery. Poison and Bleeding may naturally end through per-tick probability checks, but sleep does not guarantee their removal. First Aid treats specific conditions and injuries; doctors and specialists provide more complete treatment. No HP-restoring potions are planned. Exact rates, probabilities, injury categories, comfort bonuses, and treatment recipes remain open in [18 World Time and Living Simulation](18%20World%20Time%20and%20Living%20Simulation.md).

**What is the spatial model?**  
Free third-person overworld and interior scenes; square grid + Environment Actions on arenas only. Documented in [16 Architecture Pivot](16%20Architecture%20Pivot.md) and [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md).

**Which engine?**  
Unity locked. URP, C#, project folder `unity/`, free 3P + custom arena grid TB. Documented in [12 Engine Choice](12%20Engine%20Choice.md).

**What is the first playable proof?**  
A Vertical Slice in Lower Veil fantasy with 2 selectable factions (Wardens / Veinmarket), 3 POIs, Information Sources, arena Confront, one Bargain tease, and Lord of the Mysteries-like industrial-era mood — delivered on overworld → interior → arena rails. Death Gap is out of slice scope. Placeholder names may change. Documented in [11 Vertical Slice](11%20Vertical%20Slice.md) and [16](16%20Architecture%20Pivot.md).

**Where do systems / interfaces / micro-iterations live?**  
[17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) — System Cards + Track backlog. Spatial locks remain in [16](16%20Architecture%20Pivot.md).

**Overworld V0 size / chunk / scene load defaults?**  
Chunk = **64 m**; playable world = **2800 × 2800 m** (2×2 Unity Terrain tiles of 1400 m, height budget 700 m, sea level 24 m) with an infinite-capable chunk API; Interior and Arena V0 use **single-scene swap** (additive later). Documented in [17](17%20Game%20Systems%20Architecture.md) and `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`.

**What generation mechanism produces World / Settlement / Building / Interior content?**  
**Piece-Based Assembly**: small hand-authored Pieces carrying Snap Sockets, snapped together deterministically small → large. **Exterior Shell Grammar** builds overworld fabric (most buildings never get a Room Graph). **Interior Piece Assembly** runs only for `InteriorPolicy.Portal` buildings. Reference model: Valheim's room/dungeon piece system. Not an algorithmic subdivision solver, not the retired OrganicTown continuous-mesh generator. Documented in [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).

**Is the OrganicTown / City Generation prototype a reuse source for the new pipeline?**  
No. [13 City Generation](13%20City%20Generation.md) is Deprecated: not viable (not modular, not optimized, did not read as a real city), not a bridge, not extended. Only generic non-city-specific ideas (deterministic `WorldSeed` fork-by-name) carry forward.

**Overworld terrain height budget?**  
**700 m** — `WorldScale.TerrainHeightBudgetMeters` is the tile `size.y`. Documented in [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md).

**How is the island / ocean silhouette framed?**  
By TerrainGen: the Stage 1 landmass owns the silhouette, coast and seabed, and Stage 2 adds the massifs as uplift on top of it. The terrain bake writes that relief unchanged — **no post-generation coast pass** — and the rim meets `WorldWater` at Sea Level **24 m**. The shore surface is a world-space rule over the five canonical layers: sand below the vegetation floor (27.0 → 28.4 m), seabed rock suppressed at the waterline. Documented in [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md) and [OVERWORLD_VISUAL_DIRECTION.md](unity/Assets/Plans/OVERWORLD_VISUAL_DIRECTION.md) § Terrain surfaces.

**Overworld height / island framing?**  
**700 m** height budget; the island and its massifs come from TerrainGen (peaks around 350 m on the canonical seeds); ocean Sea Level **24 m**; heightmap 513 per 1400 m tile (2.73 m). Documented in [OVERWORLD_TERRAIN_ARCHITECTURE.md](unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md).

**V0 inland lakes / rivers?**  
**Removed from code (2026-08-15).** V0 ships **ocean only** (`WorldWater` at Sea Level). No `LakeDefinition` / `RiverTracer` / lake pipeline steps. Future inland hydro is a redesign. Alpine multi-plane lakes stay deferred; single Sea Level ocean plane is the locked V0 water model.

**May a road cross mountain terrain?**  
**Yes — mountain terrain is traversable, not a hard obstacle.** The planner prefers cheaper ground (passes, contours, switchbacks) over a direct expensive climb and expresses that as price, not refusal; `MountainPenalty` / `MountainCost` stay soft and must not become `+Infinity` because the biome is Mountain. Only water / `+Infinity`, earthwork beyond `MaxCutDepth` / `MaxFillHeight`, and grade at or above `MaxWalkableGrade` block a route outright. `ExistingCorridorDiscount` is construction cost only and must never change passability. Reviewed and approved by the project owner 2026-08-31, from [WORLD_GENERATION_PIPELINE.md](unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md) §3 «Roads are pathfinding, not lines … mountains (passes) — never straight through a mountain». Glossary: [90 Glossary](90%20Glossary.md) § Road Passability. Agent rule: [AGENTS.md](AGENTS.md) § Accepted Draft Decisions. **Approved but not Canonical:** the owner review required by `001 Documentation Standards` § Promotion Rules is granted and every other promotion requirement is met; what is missing is a host, because no Canonical document covers world generation and the document carrying the statement is Draft with unrelated content.

**May the geography-first tooling — the metrics baseline G0 and the Geometry-First Review G1 — be built?**  
**Yes, both** — answered (a) by the project owner on 2026-10-18 (GEO-6). Both are Editor-only and read-only: no production code, bake, production scene or frozen file changes, and every output lands outside `Assets`. Built the same day as `Editor/TerrainGenGeographyDump.cs` (G0) and `Editor/GeometryFirstReview.cs` (G1); measurements in [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §§3.6, 7.6, 10. The answer covers the tooling only: the target geography, the acceptance thresholds (GEO-7) and GEO-1 … GEO-5 stay open.

**Is geography a reason to change the world size or the land fraction now? (GEO-1)**  
**No** — owner direction, 2026-10-21: a reasonable increase is permissible if justified, never merely because G3c failed. The V0 world and Stage 1's land-fraction band stay; today's footprint (contour A in controlled research) is the baseline for landform-model research; contour B is a tested candidate, not evidence that area limits anything. Reopened only when a landform model is promising on the baseline and an experiment that separates area from shape attributes its remaining failures to room, or a gameplay requirement (travel pacing D3, § Exploration size) calls for more land. A 3 × 3-tile world stays a separate travel-pacing decision. Record: [GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2.1.

**How many T1 forms does the target have? (GEO-2)**  
**One branching mountain system, counted as one T1** — option (a), from the owner-approved target D6 (2026-10-20). Its culminations, ridges and spurs are parts of one T1 form; the count "1 (2 max)" and the row's numbers in `WORLD_RELIEF_PRINCIPLES.md` §1 are unchanged, a provisional Draft classification rather than a production gate. Record: review §2.2.

**What do the geography-first acceptance criteria consist of? (GEO-7, classification)**  
Owner direction, 2026-10-20 and 2026-10-21: **hard gates** are requirements with direct gameplay or topological meaning (3–5 settlement sites with a capital-size one, numerous reachable POI sites, walking routes between settlement regions, reachability, no accidental topological defects, elevated rivers to designated outlets, process gates); the owner's **visual review** at three scales judges composition; morphology metrics (Strahler order, basin area, slope shares, M-series, tier conformance) are **diagnostics**; working values are **provisional assumptions**. Pre-registered thresholds are never changed retroactively; flat land is never rewarded for its own sake. The reading set for the next experiment is still a proposal (review §3). Record: review §2.6.

**Policies settled in the post-G3c review (2026-10-21; details in the open entries below):** varied, hinterland-caused coasts are the target (GEO-4, owner direction; the production contract stays open); lakes only in declared basins with one outlet to the ocean, undeclared depressions are defects, no endorheic basins (GEO-5); a candidate is never rejected only because a large plateau exceeds the T2 row, and tier labels never decide traversal (GEO-8, owner direction; the revised tier contract is proposed); research continues under GEO-3 (c) (evidence-backed policy; production placement at G4); geography provides regions, candidate areas and routes, placement chooses sites and never reshapes terrain to meet a count (GEO-9).


---

# High Priority

## World Laws

Can Balance partially recover without player intervention?

What exact Symptoms correspond to each band of Faction Influence?

What determines the concrete winning / terminal condition of each faction?

Can multiple factions unintentionally cooperate?

What are the V1 numeric ranges for Difficulty Pressure?

## Decision Engine

When and how do Hidden consequences become discoverable across one Character Life versus across Legacy?

What is the V1 readable vocabulary for communicating Opportunity Cost without UI overload?

## Points of Interest

What exact in-world durations belong to Brief / Short / Standard / Long / Persistent in V1?

How exactly does the UI/map express urgency without becoming a stressful quest-timer game?

How many active Marked POIs should a V1 district show at once?

## Information Sources

How many tips should a V1 district generate per day from boards / bartenders / rumours?

When does a tip upgrade from Hinted to Marked by rule?

How much false or slanted information is healthy in V1 before trust collapses?

Can a Persistent POI still be finite without feeling like a fake timer?

## Progression

What are the exact V1 Conduct Progress thresholds for stages 9 → 8 and 8 → 7?

What are the exact primary/secondary attribute thresholds for each Pathway-stage transition, including the final secondary-band offset mapping?

What are the mandatory Sanity costs for each transition, the preparation-penalty curve, and the exact rule for retaining sanity at the resulting Sanity value?

For stages 5–0, what is the probability of mystical transformation after sanity loss, and what are the exact creature forms for each Pathway and stage?

How strongly may a Starting Faction nudge before it becomes a soft lock?

How do External Power Bargains interact with Pathway conduct when they pull in opposite directions?

What happens if a character tries to abandon or switch Pathways after stage 9?

## Character Attributes and Action Checks

How are attribute and skill values normalised to a common task-equivalent scale if their progression tracks use different numeric scales?

How are the shared check rules reconciled with combat's attack-specific multi-attribute formulas?

What are the exact numeric values for the shared attribute/skill progression system: base XP by task-difficulty stage, the ten-stage Δ_XP modifier table, the nonlinear XP required for each next track level, the residual floor, and the outcome modifiers for failure, normal success, and critical success? The agreed structure is universal across activity types and independent of Pathway stage; see [07.1](07.1%20Character%20Attributes,%20Skills,%20and%20Action%20Checks.md).

What are the exceptional requirements and unique benefit for attribute 100?

## Derived Character Statistics

What are the exact formulas and scales for Maximum HP, Maximum Stamina, and Maximum Will Power? Maximum Stamina is primarily derived from Endurance; its formula remains open.

What are the exact ability costs and how do they vary among Pathways and ability types? The initial balance bands are approximately 10–20% of maximum Will Power for ordinary abilities, 25–40% for powerful abilities, and 50–80% for exceptional abilities; individual abilities define actual costs.

How do effects damage or suppress Will: temporary effective-attribute reduction, maximum-pool reduction, current-resource loss, or a combination? What are duration, treatment, and recovery rules? What are the exact Will XP reward and meaningful-expenditure eligibility for full exhaustion?

What are the exact Movement Speed formula and separate world/arena units?

What are the Carrying Capacity formula, thresholds for Normal / High / Overloaded / Critical Overload, and consequences of each band? Load increases Stamina expenditure during exertion, substantially so in Overloaded and Critical Overload states; exact thresholds and multipliers remain open.

What are the Accuracy and Evasion formulas and their reconciliation with the shared action-check model?

What are the Physical/Magical Defence conversion curves, caps, stacking, and penetration rules?

What are the exact recovery rules for Will Power and individual negative conditions? Ordinary HP recovery is fixed at 1% of maximum HP per 10 world ticks unless blocked; see [07.2](07.2%20Derived%20Character%20Statistics.md). What exact items, abilities, treatments, safe activities, or rare events restore Sanity, and at what rates? Stamina recovery is fixed at 5% of maximum per world tick while not performing Stamina-consuming exertion; exact modifiers/blockers remain open.

Which derived values are shown to the player at different Intelligence levels?

## Approaches

How much V1 support does each Approach need before it feels real (skills, UI verbs, simple checks)?

When does Threaten escalate into Confront by rule rather than by writer taste?

Should Bound Path get a light Rite/Invoke extension in V1, or remain under Exchange until V2?

## Confront / Turn Economy

How diegetic / readable is the transition into a Combat Arena (fade, cut, warning) without spoiling Attention teaching?

How closely must an arena Theme Pack match the source location before it feels unfair or immersion-breaking?

How often may important nodes offer Confront as one option versus scenes that begin already in Confront?

Exact initiative formula and Move Speed units / cells per Move for V1 arenas?

Diagonal move cost on arenas: 1 or 1.5?

Exact fail chance bands for vault/climb on arenas in V1?

Is Bonus-to-Dash enabled in V1 or deferred?

## Vertical Slice

What exact prop/language checklist best communicates early radio/steam novelty without turning the slice into a tech museum?

How visible should crown/nobility pressure be inside Lower Veil without adding a third faction?

## Items, Inventory, and Equipment

The owner-approved direction is a compact base-item catalogue with compatible, procedurally selected modifiers, inspired by Path of Exile. Base families include cloth, leather, and heavy/metal armour across core equipment slots, plus a limited set of weapon families. The intended modifier structure has up to three primary and three secondary modifiers. Unique-style items are separately authored and may have Pathway-specific, playstyle-changing effects. See [18 RPG Systems Coverage Draft](18%20RPG%20Systems%20Coverage%20Draft.md).

What are the exact definitions and hierarchy of Normal, Magic, Rare, Epic, Unique, and Legendary rarity labels? Which labels are generated from bases/modifiers, and which are reserved for separately authored unique items?

What are the exact rules for primary and secondary modifier slots, compatibility, weighting, and generation constraints per base family?

How many base families and visual variants are required for V1, and which armour slots and weapon families are included?

How are non-NPC item acquisition sources (for example, selected monsters and chests) generated, and how does critical success affect acquisition? Which NPC equipment pieces can survive/be recovered after death, and how is this determined?

What are the exact major/minor modifier pools, modifier tiers and value ranges, and base-specific appearance-weight tables? Which base families should strongly favour particular modifier themes, and how rare should off-archetype combinations be?

How exactly does base-item level shift the probability distribution across modifier tiers without imposing a hard tier ceiling? Separately, what role, if any, do attribute requirements play in modifier generation?

At which exact profession mastery thresholds do standard reforging and permanent modifier locking unlock, and how do those thresholds relate to the existing skill progression stages? What probability curve maps crafting skill/mastery to the number of modifiers and resulting quality of a newly crafted item?

What are the exact probability tables and constraints used when standard reforging regenerates each unlocked modifier's type, level/tier, and numerical value? How do base-item weighting and profession mastery shape the available outcomes? What are the resource costs and time requirements for standard reforging and permanent modifier locking, and what is the growth curve for successive lock costs?

How do item modifiers interact with crafting, enchantment, identification/appraisal, equipment requirements, durability, and Pathway abilities?

How are stackable resources/consumables represented versus individually instantiated equipment and unique items?

## Character

What exact V1 length is the Death Gap in days?

How large may a Legacy Cache be before it undermines death?

What is the minimal Faction Entry pattern reusable by all V1 factions?

How many Soft Memory dialogue hooks are required in V1 versus optional flavour?

---

# Medium Priority

## Magic

Where does mystical power originate?

Can power disappear?

Can the world lose magic?

## Factions

How do factions emerge?

Can factions disappear naturally?

Can civilizations recover?

## Economy

How exactly do Influence-controlled niches set prices and availability?

How do resources disappear?

## Exploration

How are secrets generated?

Can secrets vanish forever?

What is the V1 overworld target size (km / play minutes edge-to-edge) **beyond** the V0 world (2800 × 2800 m / 64 m chunks already locked in [17](17%20Game%20Systems%20Architecture.md))? Note the measured budget: ~2.5–3 km² of dry land fits two large cities; three with real travel between them needs 4–6 km², i.e. 3×3 tiles or 2×2 at 2000 m — growing the world, not just the island.

## Procedural Buildings / Scenes

When does Piece-Based Assembly fully replace the single hand-placed Interior V0 template scene as authority for Interior Scenes? (Mechanism itself is locked — see Resolved Decisions above; only the V0→V1 timing is open.) ([15](15%20Procedural%20Building%20Generation.md))

When does additive interior/arena load replace V0 single-scene swap? (V0 default locked in [17](17%20Game%20Systems%20Architecture.md); V1+ timing open.)

How aggressive may OccultLevel visual “wrongness” be in V1 without spoiling Veil / Attention teaching?

What is the minimum viable Piece Kit size per V1 archetype (how many distinct Pieces per socket type) before repetition becomes visible to the player?

When does live Settlement / overworld chunk streaming consume `IBuildingShellBinder` from the Shell Sandbox lab (parallel to Overworld V0 today)? ([15](15%20Procedural%20Building%20Generation.md), [17](17%20Game%20Systems%20Architecture.md) W.6)

Should Local road stubs be generated as a third Prim pass after Secondary, or only when Settlement / building pads exist? ([LANDSCAPE_PLANNER.md](unity/Assets/Plans/LANDSCAPE_PLANNER.md))

When do `PoiPlacementProfile` C# catalog entries move into ScriptableObject immutable defs for authoring? ([17](17%20Game%20Systems%20Architecture.md) W.4)

What asset/API provides waterfall meshes if inland hydro returns later? ([HYDROLOGY_GENERATION_PLAN.md](unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md))

## Terrain — Stage 3F valley carve

Research record: [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) (phases 1–10, 2026-10-14 – 17; §22 holds the proposed contracts, §23 the current position). Production terrain is unchanged and Stage 3F stays an experimental downstream carve; these questions block a production design of Stage 3F, not the current bake. D1–D3 below were research requirements set by the owner for the experiments, not approved invariants; they are unrelated to the slope-reading labels D2–D7 of handoff §19. The measured counts are robust to the S8 thresholds and the matching rule (handoff §9), so the answers are design choices, not threshold tuning.

**Working decisions for the research prototype only (owner, 2026-10-16; not production decisions):** canonical `FieldResolution` = 1025; 4-connected band-local priority flood; in corrected cells the corrected lattice is authoritative and point queries are bilinear on it; corrected height ≤ `H4A`; Stage 3C untouched; `Declared` valley ends only. The isolated prototype built on them is recorded in handoff §16. The bilinear point query is contradicted by a later measurement — Unity serves triangles (handoff §18) — and is reopened in 3F-6, not changed here.

**How to read the entries.** Each question keeps its original wording, then lists options, their measured consequences and an agent recommendation. A recommendation is not a decision. Four questions — filling inherited depressions (3F-10), flat-filled pits (3F-11), cirques as permitted basins (3F-14) and jump and barriers (3F-20) — carry no recommendation: they are the owner's call. So does one part of 3F-4: which arc classes the 7.2 m bound covers. Tags: **[stage 1]** must be answered before a dump-first stage 1 of E is implemented; **[production]** before E is enabled in the production bake; **[later]** may follow. Of the 25 questions, twelve are **[stage 1]** (3F-1 to 3F-11 and 3F-14), eight **[production]** (3F-12, 3F-17 to 3F-22 and 3F-25) and five **[later]** (3F-13, 3F-15, 3F-16, 3F-23, 3F-24). A *research mode* noted under a question is a proposed way for stage-1 dumps to measure around an open answer; it is not a decision and does not make the question non-blocking.

**3F-1. Final surface and structural height. [stage 1]** Should `H4A` stay the structural height that Stages 1–4A, the frozen Stage 3C trajectories, Stage 3D/3E topology and structural diagnostics read, with E a separate final surface — the only height written to `TerrainData` — and nothing flowing from E back into TerrainGen? (handoff §22.1)
- *Options:* (a) the separation of handoff §22.1; (b) E replaces `H4A` as `TerrainAuthorityField`'s height.
- *Consequences:* (a) frozen stages stay bit-identical and keep their meaning; two named heights must be kept apart, and `OverworldTerrainValidation`, which today compares tiles with `TerrainAuthorityField`, must compare them with E. (b) changes the authority's meaning, breaks the statement that Stage 4A is the authority and lets downstream data reach the frozen stages.
- *Note, pending this answer:* E is not `H3F`. `H3F = H4A − C3F` (`CarvedElevation`, handoff §2) is the subtractive carve along the corridors; E = min(`H4A`, fill(Epre)), where Epre joins the production massif terms to a floor-referenced plain template with a hard maximum and a 60 m blend below the toe (handoff §10.2) and the fill is the band-local correction (handoff §16). Both lie at or below `H4A`. The statements that Stage 3F carves `H = H4A − C3F` — [AGENTS.md](AGENTS.md) § Accepted Draft Decisions, «Terrain guardrails»; [90 Glossary](90%20Glossary.md) § Island Relief (Stage 4A); handoff §2 — describe the existing Stage 3F code and stay as they are until this contract is approved; on approval they are updated in the same documentation transaction. Where E enters the bake is 3F-25.
- *Agent recommendation:* (a).

**3F-2. Drainage invariant (D1). [stage 1]** Is plan-view drainage (D1) a Stage 3F acceptance invariant — no new significant S8 depression relative to `H4A` outside permitted basins and no deepening of inherited ones — with the axial `CLOSED` classifier kept as a diagnostic only? Production `H3F` creates 211 such depressions on seeds 1000–1039; all 36 axial closures of `H4A` are route closures, not basins (handoff §9). The prototype of E meets it on 47 seeds at 1025 (handoff §16).
- *Options:* (a) D1 as the invariant, axial classifier diagnostic; (b) the axial classifier as the invariant; (c) no drainage invariant.
- *Consequences:* (a) E meets it on the lattice and on the served triangle surface (handoff §18); (b) fails on `H4A` itself, whose axial closures are route closures; (c) leaves Stage 3F without an acceptance basis.
- *Agent recommendation:* (a).

**3F-3. Independent oracle. [stage 1]** When a Stage 3F candidate itself uses priority flood, does D1 have to be confirmed by an independent scanner (sort + union-find, separate code) as the test oracle? (handoff §12)
- *Options:* (a) yes; (b) the flood checks itself.
- *Consequences:* (a) a test-only scanner to maintain; a fault in the flood cannot certify itself. (b) cheaper, and a fault in the flood certifies itself.
- *Agent recommendation:* (a), with a triangle-graph scan added, because the served surface is triangles (handoff §18).

**3F-4. Plain incision bound (D3). [stage 1] — arc-class coverage is the owner's call.** Is 7.2 m (`CorridorIncisionMeters`) a bound on plain incision below the massif toe only, with the mountain carve bounded separately (D3)? Deeper plain cuts do not buy drainage — 12 m creates 217 depressions against 222 at 7.2 m — and read as railway cuttings (handoff §9).
- *Options:* (a) plain only, mountain bounded separately; (b) one bound everywhere; (c) a deeper plain bound.
- *Consequences:* (a) as prototyped, measured by arc class (handoff §16): plain (arc ≥ toe + 60 m) ≤ 7.200 m on the float lattice and 7.2066 m on the served surface after 16-bit storage (handoff §18); the 60 m blend below the toe ≤ 7.208 m; cells above the toe with `U2` ≤ 0.1 m up to 7.223 m (50 cells above 7.21 m). Whether the 7.2 m bound covers the blend and those above-toe cells, or only the plain class, is part of this question; a check on the served surface needs one storage step (1.07 cm) of tolerance. (b) caps the massif valleys; (c) buys no drainage and reads as cuttings.
- *Agent recommendation:* (a) for the plain/mountain split, with the acceptance check stated per arc class: the plain class against 7.2 m on the float lattice and within 1.07 cm on the served surface; the blend and the above-toe cells reported against whichever bound this answer assigns them.
- *No recommendation* on which arc classes the 7.2 m bound covers — the plain class only, or also the 60 m blend (7.208 m measured) and the above-toe cells (up to 7.223 m): that is the owner's call.

**3F-5. Band-local correction in production. [stage 1]** Is a band-local raster correction admissible in production TerrainGen — a fill confined to carved cells inside the corridors, with outlet heights taken from `H4A`? Adopted for research only (4-connected, 1025). Measured: it holds on the lattice, on any triangulation, after 16-bit quantisation and under aligned bilinear refinement, deterministically across processes; it is not resolution-invariant (common nodes differ by up to 0.32 m between 513, 1025 and 2049) and costs 0.3 s per seed (handoff §§15–16). Without it the flank must be reshaped (variants A / C / D: 20–23 % less mountain carve, deformed mountain valleys) (handoff §10).
- *Options:* (a) admit it as prototyped; (b) reshape the flank (A / C / D); (c) no correction, accept the depressions.
- *Consequences:* (a) meets D1 on the lattice and on the served surface (handoff §§16, 18), ties the world to its resolution (3F-7) and makes flat-filled pits (3F-11); (b) 20–23 % less mountain carve; (c) fails D1 (3F-2).
- *Agent recommendation:* (a), subject to the owner's answers on 3F-10, 3F-11 and 3F-14.

**3F-6. Point-query contract. [stage 1]** Point-query contract of a corrected surface: keep the mixed reading (bilinear on the corrected lattice in corrected cells, the analytic carve elsewhere — the prototype's working contract) or read the final lattice bilinearly everywhere? The mixed reading jumps by up to 1.09 m across corrected-cell edges (0.53 m on tile seams) and exceeds analytic `H4A` between nodes by up to 6.8 cm; the lattice-only reading has neither defect, creates no depression, leaves `Declared` gaps exactly as the uncorrected lattice has them, and gives up analytic sub-lattice detail a 1025 heightmap cannot store anyway (mean 4 mm, up to 5.36 m on a steep slope). The bake, `OverworldTerrainField` and the terrain collider already read the lattice (handoff §16). **Measured since (handoff §18):** Unity serves neither reading. `Terrain.SampleHeight` and the collider read the quantised lattice as triangles split SW→NE (≤ 0.4 mm); bilinear differs from that by up to 2.59 m at the centres of twisted cells.
- *Options:* (a) mixed reading R1; (b) bilinear on the final lattice; (c) triangle reading of the quantised final lattice.
- *Consequences:* (a) discontinuous, above `H4A` between nodes, and not what the collider serves; (b) continuous, yet up to 2.59 m from the collider, so any consumer reading it disagrees with physics; (c) equal to `SampleHeight` and the collider, keeps every E invariant (0 new depressions, E ≤ `H4A` everywhere, gaps unchanged), gives up sub-lattice detail the heightmap cannot store.
- *Agent recommendation:* (c), with the point query and the lattice evaluator sharing one function (handoff §16, implementation note).

**3F-7. `FieldResolution` as world identity. [stage 1]** Is `FieldResolution` part of a world's identity for Stage 3F in production, as for the prototype? Both the correction and the Stage 3C trajectories (up to 3.7 m) change with it (handoff §15).
- *Options:* (a) yes — 1025 is part of the world's identity, a change is a new `GeneratorVersion`; (b) no.
- *Consequences:* (a) deterministic worlds; changing heightmap resolution means a new world; (b) would need resolution invariance, which was measured absent.
- *Agent recommendation:* (a).

**3F-8. Per-arc data. [stage 1]** Where do the per-arc arrays of a floor-referenced carve (floor and allowed depth along the arc) live: as immutable Stage 3F data next to `FinalValleyTrajectory`, or recomputed on every evaluation? (handoff §13) Stage 3F already cuts along `FinalValleyTrajectory`: the scoped Stage 3C unfreeze asked for in `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §15.2 was granted and spent, API-only, by F1 (preflight §20) — `CarveField` is typed to `IValleyCarveCurve` and keeps its `RidgeSkeleton` constructor, `ValleyTrajectoryNetwork.BuildCarve` builds the carve along `FinalValleyTrajectory`, and no trajectory sample, termination, width or ordering moved. The prototype of E used that binding.
- *Options:* (a) recomputed inside the Stage 3F module from `FinalValleyTrajectory` through its existing API on each evaluation; (b) immutable data stored on or next to `FinalValleyTrajectory`.
- *Consequences:* (a) no new persisted data and no new unfreeze, at an evaluation cost; (b) adds data to a frozen Stage 3C type, beyond the API-only unfreeze F1 spent, so it needs a new scoped unfreeze.
- *Agent recommendation:* (a) for stage 1; revisit only with an approved new unfreeze.

**3F-9. Hard maximum between corridors. [stage 1]** Should `SmoothMax` between overlapping corridors become a hard maximum? It accounts for about 18 depressions and lifts plain incision above 7.25 m on 4.2 % of plain valley length (handoff §§9–10).
- *Options:* (a) a hard maximum inside E's composition, `CarveField.Depth` unchanged; (b) change `CarveField` itself; (c) keep `SmoothMax`.
- *Consequences:* (a) as prototyped; the existing `H3F` dumps and tests keep their meaning; (b) changes existing Stage 3F code and its records; (c) about 18 more depressions for the correction to absorb and plain incision above the bound.
- *Agent recommendation:* (a).

**3F-10. Filling inherited depressions. [stage 1] — owner's call.** May the correction fill an inherited `H4A` depression that lies inside a carved component? D1 forbids deepening, not filling; the prototype fills 3 of the 6 (1013, 1026, 1038) and keeps the other 3 unchanged (handoff §16).
- *Options:* (a) fill, as the prototype does; (b) never fill — inherited depressions are excluded from the correction and keep their own outlets; (c) fill only below a stated size, as a world quantity.
- *Consequences:* (a) simplest; landforms `H4A` had turn into terraces; (b) keeps `H4A`'s pits, and the correction must route around them; (c) needs a justified threshold.
- *No recommendation.*

**3F-11. Flat-filled pits. [stage 1] — owner's call.** Are flat-filled pits an acceptable landform? The correction turns flank pits into flat terraces up to 4.4 m deep (57 370 m³ over 47 seeds, 82 % on the massif flank); with inland water they would be lake sites, which ties this to the Stage 3E basin question above (handoff §16).
- *Options:* (a) accept the terraces; (b) reject them — reshape the flank (A / C / D) or redesign the mountain term (3F-16); (c) accept them only where they become lake sites.
- *Consequences:* (a) flat steps up to 4.4 m deep on the massif flank, more of them if cirque pits are filled too (3F-14); (b) 20–23 % less mountain carve, or a redesign; (c) depends on inland water, which V0 does not have.
- *No recommendation.*

**3F-12. Valley-ending semantics. [production]** Route-barrier policy (valley-ending semantics): where a declared trajectory rises more than the plain bound can cut, does the valley stop and restart downstream (gap), end (termination), or continue as a flat-filled floor? Continuity, no reappearing segments, the 7.2 m bound and D1 cannot all hold there (handoff §10). Measured on 8 seeds: ending at the first barrier is stable and creates no depression but loses 4.2 % of plain valley length and ends valleys 52–342 m inland; the prototype keeps `Declared` ends and shows 41 gaps on 34 of 299 trajectories, 36 of them on coastal turn-backs (handoff §§15–16).
- *Options:* (a) `Declared` gaps; (b) termination at the first barrier; (c) a flat-filled floor.
- *Consequences:* (a) shows 41 gaps on 34 of 299 trajectories, identical on every height reading (handoff §18); (b) stable, 4.2 % less plain valley, valleys end inland; (c) breaks the plain bound or D1.
- *Agent recommendation:* `Declared` for the dump-first stage 1 with every gap reported; decide before production.

**3F-13. Valley mouths. [later]** Is a geometric valley-mouth rule wanted for trajectories that approach the coast and turn back inland? The rule tested — end at the running minimum of the Stage 4A gate — is not a mouth: 8 of 12 such ends lie 60–160 m inland with up to 3–4 m of valley cut off, while the trajectory reaches the sea later anyway (handoff §15). A different rule would be a new variant.
- *Options:* (a) no mouth rule in V0; (b) design a new mouth variant.
- *Agent recommendation:* (a); a new variant is its own research item.

**3F-14. Cirques as permitted basins. [stage 1] — owner's call.** Are declared Stage 3E cirques permitted basins under D1? 50 of the hybrid's 100 pre-fill flank depressions lie inside them. Their depth bound is still B4 (`PROCEDURAL_TERRAIN_STAGE_3E_PREFLIGHT.md` §22.3) and their realisation F2 (`PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §15) (handoff §10).
- *Why stage 1:* the answer decides what the correction does inside declared cirques, so a stage-1 implementation that picks either behaviour would answer this question by itself. It also meets 3F-11: a cirque pit the correction fills becomes a flat terrace.
- *Options:* (a) not permitted — the correction fills them like any carved pit, as the prototype does; (b) permitted — the correction leaves them, which needs their depth bound B4 and realisation F2 decided first.
- *Consequences:* (a) no basins on the flank, more flat-filled terraces (3F-11); (b) the D1 check needs the declared cirques as permitted basins, and E waits for B4 and F2.
- *No recommendation.*
- *Research mode for stage-1 dumps (proposed, not a decision):* compute E both ways — the prototype's fill everywhere, and with depressions inside declared cirques left unfilled — and report for each the depressions inside and outside cirques, their depth, area and volume, and the terraces made. Tests whose verdict depends on the answer stay marked pending. Nothing from either variant reaches the bake.

**3F-15. Natural thalweg. [later]** Natural thalweg against frozen trajectories: `H4A`'s cross-section low lies outside the existing corridor at about 80 % of plain stations, and shifting the floor inside the corridor does not reach it (variant F; the handoff recommends against prototyping it). Accept the axis-centred valley, widen the corridor, reopen the frozen Stage 3C trajectory geometry, or derive plain valleys from `H4A` flow instead of declared trajectories? (handoff §11)
- *Consequences:* every option except the axis-centred valley reopens frozen Stage 3C geometry or replaces declared trajectories.
- *Agent recommendation:* accept the axis-centred valley for V0.

**3F-16. Mountain-term redesign. [later]** Should the mountain term be redesigned so the massif flank drains without a correction step? On its own it creates 59 flank depressions on 27 of 32 massif seeds (handoff §9).
- *Agent recommendation:* keep the correction for V0; revisit if 3F-11 rejects the terraces.

**3F-17. Traversability criterion. [production]** Is the traversability criterion read per seed or over the population? Over 47 seeds E equals `H3F` (land > 45° 4.475 %); per seed it exceeds `H3F` by more than 0.01 percentage points on 1010 (+0.012) and 1034 (+0.015), from the plain template rather than the correction (handoff §16). **Measured since (handoff §§20–21):** the share of land above 45° does not measure reachability — single passages within ±1° of 45° decide 17–34 thousand m² on 5 of 8 seeds.
- *Options:* (a) per seed: the T-edge reachable share and the size of the uncertain zone (3F-18), reported; (b) the population average; (c) a gate on land above 45°.
- *Consequences:* (a) shows where E changes access; (b) hides single-seed effects; (c) measures steepness, not access.
- *Agent recommendation:* (a), reported but not gating until 3F-20 is answered.

**3F-18. Walking-connectivity contract. [production]** Adopt handoff §22.3–22.4: a triangle is walkable when its slope is strictly below 45°; T-edge is the conservative lower bound of guaranteed walking connectivity (both ways); T-vertex is an experimental upper bound; the area between them is an uncertain zone; descent-only links are not part of the contract; the contract applies to the configuration of §22.6, and changing the Unity version, the terrain layout, the controller values or the movement constants voids it until re-measured.
- *Options:* (a) as proposed; (b) T-vertex as the contract; (c) T-edge read as exact — T-edge-unreachable means inaccessible; (d) no physical-connectivity contract.
- *Consequences:* (a) no false "passable" was observed (handoff §21), and the gap to reality is named; (b) one false "passable" observed (a vertex step on 1034 `H4A`); (c) declares about 25 thousand m² on 1010 `H4A` inaccessible that the controller walks; (d) leaves E's access effects and POI decisions without a basis.
- *Agent recommendation:* (a).

**3F-19. Slope readings banned from physical decisions. [production]** Adopt handoff §22.5: slope readings D2 (`GetInterpolatedNormal`), D3 (`OverworldTerrainField.SlopeDegreesAt`), D6 (`OverworldTerrainDiagnostics` reachability) and D7 (the analytic `H4A` gradient) are never used for a character's walkability, an object's footing, spawn or a character's access gating; D4 (`LandscapeCostField`'s 10 m slope) is not used for them either and keeps its own uses. Road passability is not a physical decision in this sense: it stays governed by the approved Road Passability contract ([90 Glossary](90%20Glossary.md) § Road Passability; § Resolved Decisions above), whose grades and slope on `LandscapeCostField` this question does not touch. POI site scoring keeps D4.
- *Options:* (a) adopt; (b) allow D3 as an approximation.
- *Consequences:* (b) judged on identical routes, D3 was wrong 8 times — 5 false "passable", 3 false "not passable" (handoff §21).
- *Agent recommendation:* (a).

**3F-20. Jump and barriers. [production] — owner's call.** The production jump climbs every tested slope up to 70° (handoff §21), so steep walls — E's walls among them — stop walking but not a jumping player. Should steep walls be barriers?
- *Options and consequences:*
  - (A) keep the jump as it is: walls stop walking only; cliffs cannot hide POIs or gate routes; the walking contract describes convenient routes, and E's access effects are informational.
  - (B) no jump from steep footing: walls become real barriers; descent-only links become traps, so a directed check for exits is needed (3F-24); controller code changes and calibration is repeated.
  - (C) limited climbing (stamina, a cap on consecutive jumps or on the height of a climb): short walls passable, long ones not; needs a "height of steep run" measure; calibration is repeated.
  - (D) explicit barrier cliffs (a steeper class or a tag), so relief alone is never a barrier: needs content or generation rules for barriers.
- *No recommendation.*

**3F-21. Classification slope. [production]** Surface and vegetation rules read slope reading D3 recomputed from the final lattice (contract B), and the physical checks of 3F-19 read triangles?
- *Options:* (a) D3 from the final lattice for classification, triangles for footing; (b) triangle slope for classification too; (c) analytic `H4A` slope.
- *Consequences:* (a) D3 and T disagree on 0.16–0.34 % of land at the tree and grass thresholds, and E's own change to the rules dominates (handoff §19); (b) piecewise-constant slope, breaking at the cell diagonals where grass is evaluated, and about 6–7 % more area above 60° than D3 reports; (c) not the stored surface, and wrong wherever E carved.
- *Agent recommendation:* (a). No threshold changes.

**3F-22. Biome slope source. [production]** `BiomeClassifier` reads slope reading D2, which is tile-local and differs across a seam by up to 47.6° for the same point.
- *Options:* (a) keep D2; (b) D3; (c) triangle slope.
- *Consequences:* the choice touches only the Mountain/Highland split above `GreenMaxHeight` at 60°, and land steeper than 60° is about 1 % of the island (handoff §19); (a) keeps the seam discontinuity; (b) world-continuous and consistent with 3F-21; (c) noisy at cell diagonals.
- *Agent recommendation:* (b).

**3F-23. Vertex passages and the uncertain zone. [later]** Vertex steps that touch a face steeper than 45° walked in 10 of 11 cases; the outcome was deterministic but predicted by no tested model (handoff §21).
- *Options:* (a) a systematic vertex-passage experiment before any rule treats "not T-edge-reachable" as inaccessible; (b) treat the uncertain zone as inaccessible; (c) treat it as accessible.
- *Consequences:* (b) under-counts (1010 `H4A`); (c) over-counts (1034 `H4A`).
- *Agent recommendation:* (a); until then the zone is reported as uncertain.

**3F-24. Descent-only links. [later; depends on 3F-20]** Descent passes every tested slope, so one-way links exist.
- *Options:* (a) ignore them while the jump stays as it is; (b) a directed exit check if walls become barriers.
- *Agent recommendation:* follow 3F-20.

**3F-25. Where E enters the bake. [production]** Where does E enter the production bake, and how does that keep one production terrain pipeline and one terrain authority (handoff §2; [AGENTS.md](AGENTS.md) § Accepted Draft Decisions, «Overworld terrain stack» and «Terrain guardrails»)? A dump-first stage 1 does not touch the bake and does not depend on this answer.
- *Options:* (a) TerrainGen's Stage 3F computes E; `TerrainAuthorityField` keeps evaluating `H4A` for structural use, and the bake replaces the world lattice with E after `OverworldTerrainField.FromAuthority`, the way the sanctioned hydrology carve already lowers that lattice; (b) `TerrainAuthorityField` itself evaluates E; (c) a separate final-surface authority object.
- *Consequences:* (a) TerrainGen stays the only authority and nothing flows back into it; in the same change `OverworldTerrainValidation` compares tiles with E, `GeneratorVersion` moves (3F-7), the bake's heightmap resolution (513 per tile) and `FieldResolution` (1025 per world) are tied in code — today they are two unconnected constants — the pending hydrology carve is re-examined because it would lower E (hydrology is outside the measured scope, handoff §22.6), and the statements noted under 3F-1 are updated. (b) conflicts with 3F-1 (a). (c) conflicts with "no second terrain authority" (handoff §2).
- *Agent recommendation:* (a).

## Terrain — Geography-first world shape

Research record: [GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (2026-10-18). Production terrain is unchanged. These questions decide what geography the island is meant to have and where it is generated; they do not reopen the Stage 3F questions above, which keep their wording and owners. Tags name the roadmap step of the research record §9 that the answer must precede. A recommendation is not a decision.

**G2 review, 2026-10-20** ([GEOGRAPHY_TARGET_BRIEF_G2.md](unity/Assets/Plans/GEOGRAPHY_TARGET_BRIEF_G2.md)): the owner **approved the target geography D6** from a concept image ([`References/G2_target_island_concept_2026-10-20.webp`](References/G2_target_island_concept_2026-10-20.webp) — a project reference inspired by DragonSword: Awakening's visual direction, **not an official map of the game**) and set the scope of the research experiment **G3c (D8)**, whose run then awaited the owner's go-ahead (given the same day; G3c ran 2026-10-20 … 21, research record §14). The owner's directions on GEO-1, GEO-4 and GEO-7 are recorded below as *Owner direction* lines; they are directions, not final answers, and no question in this section is marked answered by them. Production terrain stays `H4A = H1 + U2 + R4A`.

**Post-G3c review, 2026-10-21** ([GEOGRAPHY_POST_G3C_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_POST_G3C_REVIEW.md) §2): at the owner's request each question below now carries a *Decision record* line with one of four statuses — **Resolved — owner direction**, **Resolved — evidence-backed policy**, **Proposed — owner confirmation required**, **Research required** — one per part where a question has several parts. GEO-1 and GEO-2 are resolved and summarised in § Resolved Decisions; the others keep open parts. **GEO-3 keeps its scope** (where the mountain system lives); the settlement and POI relationship the request described under that number is the new **GEO-9**. Nothing here authorises production terrain work, a new experiment or G3d.

**GEO-1. Geographic scale. [before G2] — RESOLVED 2026-10-21 (owner direction; moved to § Resolved Decisions).** Must the target geography — a connected mountain system with several ridges, passes, foothills, lowland corridors and varied coasts — fit the locked V0 world (2800 × 2800 m; land 32–40 % of it, measured 2.61–3.16 km²; inscribed radius 410–781 m; 54–59 % of the land within 200 m of the coast)? Linked to the existing Exploration question on the V1 overworld size; this one asks only whether geography is a reason to answer it now.
- *Options:* (a) fit the V0 world, using game-scale compression; (b) grow the world or the land fraction before geography work; (c) decide after G3 shows what fits.
- *Consequences:* (a) one connected system with two to four major ridges looks plausible; two separate ranges with an intermontane basin do not fit the current Stage 2A rules; (b) moves Stage 1 golden hashes and every downstream tuning, and touches the locked V0 defaults in [17](17%20Game%20Systems%20Architecture.md); (c) G3 runs on today's footprint and may answer (a) by measurement.
- *Agent recommendation:* (c).
- *Evidence since (G3, 2026-10-19, research record §11; not an answer):* on today's footprint one connected branched system fits all seven canonical seeds, but it takes 30–38 % of the land and leaves 12–16 % below 5° (25–54 % on `H4A`); the inscribed radius caps crest height on narrow islands (cap binding on up to 54 % of the spine — under G3's own 0.7 × coast-distance cap, an authoring default); the coast gate is not the limit for the mountains.
- *Evidence since (G3b, 2026-10-19 … 20, research record §12; not an answer):* a converging valley tree on the same skeleton — trunks between the spine and each branch, a corridor across the lowest saddle, flat lower floors, side valleys routed on the surface that is written — gives every canonical seed a lowland pass across the range at 81–94 m (18–20 m above the floor surface), but land below 5° rises only to 15.2–17.1 % and no basin exceeds 0.21 km², so no seed drains through a Strahler order ≥ 3 network at the 30 000 m² channel area. ~~The limit is room, not valley topology:~~ **[Corrected 2026-10-20, research record §13.2]** The measured limits are the coastal transition and the construction — the gated coastal ramp is 54–59 % of the land and the mountain flanks are 100–420 m deep — and no run varied the footprint, so whether room is a limit is a hypothesis; "not valley topology" holds only for the one family tested. G3's "the coast gate is not the limit" holds for the mountains, not for drainage (GEO-4).
- *Evidence since (G2 diagnostic, 2026-10-20, brief §4.2; not an answer):* with provisional settlement assumptions (capital 7.1 ha, town 2.5 ha, ≤ 6.84°, 450 m spacing), production `H4A` (110201, 424242) holds a capital and two to three towns; G3 holds no capital site on 7/7 seeds, G3b on 6/7; **no surface measured has a coastal town site**, because the 60–200 m coastal band (33–38 % of the land) has a median slope of 11.5–15.8° and is 19–24 % buildable.
- *Owner direction (2026-10-20, brief D4, D8):* decide by experiment — G3c compares today's footprint (A) with about 40 % more land inside the same 2.8 km world (B), each with a fixed shoreline; a 3 × 3-tile world is a separate travel-scale decision (brief D3) and is not part of G3c. The footprint is a hypothesis under test, not an assumed limit. *Plan review (owner, 2026-10-20):* one contour per footprint is the control; coastline variety is a separate later stage (G3d); because contour B differs in shape as well as area, G3c compares two specific contours and cannot prove that area alone causes a difference — separating the two is G3d's. **Still open:** the footprint itself, after G3c (and G3d if needed).
- *Evidence since (G3c, 2026-10-20 … 21, research record §14; not an answer):* neither contour passed the pre-registered rule (A met every reading on 2 of 10 held-out seeds, B on none), and B did not materially improve on A — it adds 31–43 % buildable lowland but with worse hygiene. Both contours carried a capital plus four further settlement sites with a coastal one on all twenty lattices; B gave larger basins (0.47–0.94 against 0.23–0.42 km²), a stronger central peak and more rivers rising in the mountains. By the rule, the world is not grown on this evidence; the result concerns the two contours, which differ in shape as well as area.
- *Decision record (2026-10-21, review §2.1) — **Resolved — owner direction**:* geography is not a reason to change the world size or the land fraction now ("a reasonable increase in scale is permissible if justified, but the world must not be enlarged merely because G3c failed"). Today's footprint — contour A in controlled research — is the baseline for landform-model research; contour B stays a tested candidate, not evidence that area is the limiting factor. **Reopened only when** a landform model is promising on the baseline and either an experiment that separates area from shape (G3d's design) attributes its remaining failures to room, or a gameplay requirement (D3 travel pacing; the § Exploration size question, which this does not answer) calls for more land. A 3 × 3-tile world stays a separate travel-pacing decision. Authorises no Stage 1 change, no contour B in production, no G3d.

**GEO-2. Mountain budget. [before G3] — RESOLVED 2026-10-21: (a) (owner direction, D6; moved to § Resolved Decisions).** `WORLD_RELIEF_PRINCIPLES.md` §1 sets the T1 massif count at 1 per island (2 max), and Stage 2A accepts at most 2 axes 600 m apart, each needing a 200 m-half-width body 200 m clear of the coast — which in practice yields one unbranched massif (25 of 29 audited worlds) or none (4). Is the target one *connected, branched* mountain system (compatible with a count of 1 if T1 names the system), several separate ranges (the rule and the room both change), or the current single massif with richer realisation?
- *Options:* (a) one branched system counted as one T1; (b) several separate T1 ranges; (c) keep the single massif and add relief only below it.
- *Consequences:* (a) keeps the tier table's count and needs branching that production Stage 2B does not have — branch ranges exist only in the superseded belt model; (b) needs room the island lacks today (GEO-1) and the missing intermontane concept (3F preflight B2a/B2b); (c) leaves the single-dome failure mode in place on every massif world and does nothing for the four massif-free worlds in 29.
- *No recommendation* until G0 has measured the baseline. *G0 has now measured it* (research record §7.6: one separate summit and no pass on the main divide on every canonical seed); a recommendation is not added here — the owner reviews G0 and G1 first (G2).
- *G3 ran before this question was answered*, by the owner's instruction on 2026-10-19, with option (a) as a **test hypothesis only**. *Evidence (research record §11; not an answer):* one connected branched system — spine, two branches, two or three culminations, one or two saddles — fits every canonical seed, the massif-free ones included; no second separate range was attempted.
- *Owner direction (2026-10-20, brief D6):* the approved target has **one dominant branching mountain system** — several peaks, connected ridges, spurs, passes and valleys — with several large plateaus integrated into ridges or foothills in other regions; neither a single smooth dome nor a uniform mountain field. **Still open:** how `WORLD_RELIEF_PRINCIPLES.md` §1 words the T1 count for such a system (its row stays in force), and how the plateaus fit the tier table (GEO-8).
- *Decision record (2026-10-21, review §2.2) — **Resolved — owner direction (D6 point 1)**:* **option (a)**. T1 names the island's mountain system — one connected, branching system with a main divide, several culminations, branch ridges, spurs and passes; its culminations and ridges are parts of one T1 form, not separate T1s, and the 2.5× rule, a rule between tiers, is not applied among them. The count "1 (2 max)" stays as written; D6's target is one dominant system. The row's heights (250–530 m above local ground) and footprint (1 000–2 000 m) are a provisional Draft classification — not a production gate, unchanged, not re-measured. *Research note, not a change:* G3c's systems topped out at 253–308 m above sea level, near or below the row's 250 m floor in relief above local ground; whether D6's dominant mountains need more of the 700 m budget is measured on the next model. Plateaus: GEO-8.

**GEO-3. Where the mountain system lives. [after G3; decided in G4] — research direction set 2026-10-21; production placement open (G4).** If the system grows beyond today's massif: (a) a revision of Stage 2B into a branched system; (b) a new positive stage composed after `H2`, as Stage 4A was; (c) a geography skeleton generating ridges and valleys together for the whole island, of which today's massif plus Stage 3A–3C is a special case.
- *Consequences:* (a) moves every Stage 3 input; needs the 29-seed re-audit and a `GeneratorVersion` bump; (b) keeps frozen stages bit-identical but leaves the new ridges without their dual valleys, because Stage 3C derives valleys from the massif frame only; (c) coherent by construction and the largest change — it supersedes frozen Stage 3A–3C geometry through an owner-approved unfreeze.
- *No recommendation* before G3 measures candidates. Ridges and valleys must be designed as duals under any option (`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §3.1).
- *Evidence since (G3, research record §11.1, §11.8; one candidate, not a decision):* a shared skeleton (c) reached every seed; (a) cannot reach the massif-free worlds while Stage 2A certifies the axis, and (b) would add ridges without valleys. (c)'s cost was confirmed: the candidate had to replace `U2`, the Stage 4A apron and every massif-derived Stage 3A–3E structure. *Agent reading:* the evidence currently favours (c); G4 remains the place to decide, after G3b.
- *Evidence since (G3b, research record §12.6, §12.8; one candidate, not a decision):* trunks, the corridor and the routed side valleys all came from the one skeleton, so the reading in favour of (c) is unchanged. New: a valley network designed on a surface other than the one finally written is not the network that surface drains — before routing moved onto the gated surface, only about half of the links designed to drain 30 000 m² gathered it on the lattice — so wherever the system lives, its valleys must be routed on the final gated surface. G3b itself is not a production candidate (12 shallow S8 depressions on four seeds, 8 genuinely climbing valley axes, new artefacts at branch roots). G4 remains the place to decide.
- *Since G2 (2026-10-20; not a decision):* the G3c plan (brief §8.3) builds a region-first skeleton — highland, plateaus, lowlands with designated outlets and a per-segment coastal profile designed together — as research under (c); valleys by a strip partition (Stage 3 design §3.1), not distance to a polyline, and D8 flow as the check only. A region-first skeleton that owns the coastal profile would, in production, also reach Stage 1's golden-hashed `H1` or need a lowering coastal stage (GEO-4). G4 remains the place to decide.
- *Evidence since (G3c, research record §§14.7–14.9; one construction, not a decision):* a region-first skeleton whose surface is the eikonal distance from a designed drainage tree makes ridges the duals of valleys, forms no closed depression by construction and makes the lowlands and the coast work; it also gives planar facets, whale-back ridges on contour A and drum-like plateaus, and needed eighteen named construction fixes. A production mechanism would need a more expressive landform model than this one.
- *Decision record (2026-10-21, review §2.3).* **Scope kept:** this question is where the mountain system lives; settlement and POI placement is GEO-9. **Research direction — Resolved — evidence-backed policy:** landform-model research continues under **(c)** — regions, ridges, valleys, plateaus and coastal transitions from one structure; (b) alone is not pursued (it violates the duality rule, Stage 3 design §3.1) and (a) is not pursued as the mechanism (Stage 2A certification leaves massif-free worlds without mountains). **Production placement — Proposed — owner confirmation required at G4:** which stages a chosen model supersedes (2B, 4A, 3A–3E, and `H1` inside the land if it owns the coastal profile), the scoped unfreeze, the version bump and the re-audit, decided only after a model passes its evaluation. The recommendation of (c) is the agent's; it approves no unfreeze.
- *Owner decision (2026-10-21, research only):* the landform-model comparison under (c) uses the pair **S-LM3** (structure fields with a fluvial steady state) **+ S-LM1** (a generalised skeleton strip partition) on **one shared LM2 region layout**, contour A only. Its step 0 is written ([GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md), v0.2 — the owner's revision of the agent's draft) and awaits the explicit decisions of its §12; no code or run is authorised, and nothing here decides production placement. *2026-10-22:* the owner supports the note (v0.3) as the research direction, without authorising code or calibration; the technical specification ([GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md)) is drafted and checked on historical G3c data, and Step 1 stays a separate decision. *Later the same day:* the owner approved the specification (§12 items 1–4), including the S-LM3 model contract **as an experimental representation only**; Step 1 is not authorised, and production placement remains this question's G4 part. *Then:* the owner approved the design note (v0.5) as the research plan, kept Step 1 unauthorised (a separate, explicit authorisation; until then no generation, Unity run or production change) and kept **LMC** as the working name, the official ID to be chosen by the repository's rules before implementation. The conditions for Step 1 are listed in [GEOGRAPHY_LMC_STEP1_READINESS.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_READINESS.md). *2026-10-23:* conditions C2–C5 prepared on the owner's request with read-only checks on existing G3c data (spec §13): amendments A1 (R8 covers every declared plateau top) and A3 (slope-clustering and per-region slope diagnostics) await approval; the whale-back measure A2 failed its pre-registered check, so option (b) — the whale-back judged in the review — is recommended; the calibration-grid reference is recorded. No generation, no Unity run. *Then (owner, 2026-10-23):* A1 and A3 approved and option (b) chosen for the whale-back — the specification is v0.4; Step 1 is still not authorised, with C1 (official ID), C8 (commit policy) and C9 (reference data) open. *Later the same day (owner):* **C1** — the official experiment ID is **G3e** (working name LMC); **C8** — no commit of its code during Step 1, the commit question returns before the freeze; **C9** — the reference data is copied, MD5-verified, to `D:\Hobby\game-dev\research-data\lmc-reference\`. Every pre-authorisation condition is met; the Step 1 authorisation itself remains.
- *Step 1 state (2026-10-23, research only; not an answer to this question):* the comparison ran as G3e Step 1 on the calibration seeds — five calibration runs, continuity with G3c's measurements confirmed, **no synthesis passing every hard gate**, and S-LM3's receiver iteration never converged; review and the owner's decisions in [GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) §9. Not evidence for or against (c) yet: the owner asked first to show that a model converges, keeps its elevation targets and routes rivers to the designated outlets. A diagnostic run the same day (review §11) located S-LM3's non-convergence in its hillslope regime and S-LM1's closed depressions in its plateau ramp and mountain stages — construction findings, still not evidence on (c) itself. *Owner, the same day:* S-LM3 is closed in its current configuration with a negative result (non-convergence; causation not proven); G3e continues with S-LM1 alone. A failure of one synthesis is a finding about that representation, not about option (c). *Then (2026-10-24):* after eleven runs Step 1 **ended not ready** — S-LM1 fails R4, R5, R7, R8, R9 on 110201 and R7 on 1337; no candidate for Step 2, no freeze, no held-out evaluation ([GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md)). The remaining failures are traced to the shared layout's plateau placement and ramp bearing and to S-LM1's valley placement against the base; this still answers nothing about option (c) or the island's size. The owner accepted the report and closed Step 1 the same day. *Then (2026-10-24, research only, not an answer to this question):* the owner approved the plan of the post-mortem checks **G3e-PM** — the owner's first look at S-LM1 at three scales, then a ledger of run 11's hard-gate failures with their owners and three read-only checks on the preserved data — pre-registered in [GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md](unity/Assets/Plans/GEOGRAPHY_G3E_POSTMORTEM_CHECKS.md); it changes no code or definition, decides whether the S-LM1 line continues with an exact package, needs contract decisions first or stops, and runs only on the owner's separate authorisation. *Then (2026-10-24, research only, not an answer to this question):* G3e-PM ran (results in §13 of its pre-registration). Three results are kept apart: the **registered D1 result is INCONCLUSIVE** (a validity check of its S8 replica failed); **H is FAIL by §7**, accepted by the owner, because on 110201 all four elevated systems already reach undesignated shores on S-LM1's base surface; and a **one-run deviation (DEV-1)** completed the ledger — outside owners S-LM1's base, its flank coordinate u and its valley cross-profile. The owner kept those contracts, so **the current S-LM1 line stops** — a research-line decision, not a claim that the mechanisms can never be revised. The failure is a finding about the S-LM1 representation as built; it says nothing about option (c) or the island's size. Also recorded: a base on which the coastal surface decides heights beyond the coastal band width is treated as a potential deviation from the specification's §6.
- *Since 2026-10-25 (research only, not an answer to this question):* the owner approved the phase **G3f** — regional geography architecture, working name RGA ([GEOGRAPHY_G3F_PHASE_PLAN.md](unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md), D-1) — and its architecture **as the hypothesis to test** (D-2): a seeded landform grammar instantiated into one authoritative Geography Plan in which every land vertex has one height owner, a declared drainage unit, and every landform reserves what it writes; one drainage-consistent surface synthesis reads it; tested first in a bounded regional prototype on contour A. It stays under option (c) — regions, ridges, valleys, plateaus and coasts from one structure — and production placement remains this question's G4 part. The plan's decisions D-3 … D-12 are open and only its step A (the plan and its pointers) is authorised: no code, generation or Unity run. The ownership model and the authority's form are now GEO-10 and GEO-11. *Later the same day* the owner decided the plan's D-3 … D-12 on content (its decision record, v0.3) — among them D-3: a changed fluvial law of the S-LM3 family may be described in WP1 as a new research line, the closed configuration not resumed and every major divide declared by the plan. Decisions on content authorise no execution: WP0, WP1, code, Unity and generation each need their own authorisation.

**GEO-4. Coast contract. [before Stage 4C and G6] — target direction RESOLVED 2026-10-21 (owner direction); production contract open.** Every relief term today is exactly zero at and seaward of the shore and gated over the 200 m coastal rise, so the land meets the sea on one gentle profile everywhere. The reference shows cliff coasts, terraced coves and headlands. Should relief be allowed to rise steeply immediately inland of an unchanged shoreline?
- *Options:* (a) keep the gate; coastal character only within it (Stage 4C as designed); (b) allow steep relief just inland of the shore with the land mask bit-identical; (c) allow the shoreline itself to move (a Stage 1 reopening).
- *Consequences:* (a) no sea cliffs or headlands; (b) a new coastal contract and new tests that the mask does not move; road, POI and settlement siting near the shore must be re-measured; (c) moves golden hashes and every downstream input.
- *No recommendation.*
- *Evidence since (G3):* the 200 m gate removes only 2–6 % of a central mountain system's volume — it constrains coasts, not the mountains; G3 kept it unchanged.
- *Evidence since (G3b, research record §12.4, §12.8; not an answer):* for drainage the 200 m gate is the binding constraint — the gated ramp sends water straight to the shore and keeps every basin at ≤ 0.21 km². A sensitivity with a 100 m gate (`G3-net-g100`, evidence, not a proposal) roughly doubled tributaries (6–13 per seed) and reached Strahler order 3 on one seed, but left 116 shallow S8 depressions across all seven seeds, because its troughs and floors were built for a 200 m gate; a shorter gate would need its own trough design. The land mask and the ocean stayed unchanged in every variant.
- *Owner direction (2026-10-20, G2 task and brief D5):* spatially varied coastal transitions are the preferred direction — beaches and low shores, river mouths and coastal lowlands, coves and inlets, cliffs and steep coastal mountains — caused by the inland geography, not one uniform ramp. In G3c the shoreline outline is **fixed** within each footprint and the coastal elevation profile varies by segment with its hinterland. **Still open:** the production coastal contract — options (a)–(c) above — and which stage owns the profile, given that `H1` is golden-hashed Stage 1 height (brief §6.3).
- *Evidence since (G3c, research record §14.7; not an answer):* a per-segment coastal profile behind a fixed shoreline — low at outlets and lowland fronts, rocky before uplands, cliffs before the highland and plateaus — gave coastal settlement sites on all twenty lattices, against none on any surface measured in G2, and shore classes consistent with their hinterland on 19 of 20. The shoreline is the contour's by construction (land is written at 24.5 m or above, sea cells are copied from Stage 1).
- *Decision record (2026-10-21, review §2.4).* **Target — Resolved — owner direction (2026-10-20, 2026-10-21):** varied coasts caused by their hinterland — beaches and low shores at lowland fronts and outlets, coves at valley mouths, rocky shores and headlands, cliffs where high ground meets the sea — never one uniform ramp; option (a) as the coast model is therefore not the target. **Production contract — Research required, then Proposed at G4:** (b) against (c) and the owning stage, given `H1`'s golden hashes; default for research **(b)**, with planform variety measured and left to G3d after a model is promising. **Caveat:** G3c's R6 checks classes the construction assigned by hinterland and passed while the views show an over-continuous cliff band, so it is **not** validation of coastline diversity; contours A and B differ in shape and area. Authorises no G3d, no Stage 1 change, no production change.

**GEO-5. Basin and inland-water policy for the geography target. [before G6] — policy RESOLVED 2026-10-21: (b); realisation research required.** The hydrology design, whose MVP the owner approved on 2026-09-03 ([HYDROLOGY_GENERATION_PLAN.md](unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md) §§1, 9, 17), routes every river system to the ocean and fills every closed depression to its spill level (no endorheic basin); V0 water is ocean only. Does the geography target keep that, or does it include lakes in declared basins, endorheic basins, or both? This sits beside 3F-10, 3F-11 and 3F-14 and does not answer them.
- *Options:* (a) keep fill-then-spill and ocean outlets; (b) lakes in declared basins with outlets; (c) also endorheic basins.
- *Consequences:* (a) basins appear only as filled flats or dry hollows; (b) needs B4 (basin depth) and Stage 3F F2, plus inland water rendering, outside V0; (c) also changes the hydrology validator's ocean-outlet invariant.
- *No recommendation.*
- *Since G2 (2026-10-20):* the approved target D6 includes lakes "where the structure holds them" and rivers reaching designated coastal outlets. That makes this question part of the target but does not answer it: G3c builds no lakes and reports lake-site candidates only.
- *Decision record (2026-10-21, review §2.5).* **Principle — Resolved — owner direction (D6 point 6):** lakes are part of the target where the structure holds them; never placed at arbitrary points, never required on every seed. **Policy — Resolved — evidence-backed policy: option (b).** Three classes are kept apart: a **natural closed basin** the geography declares (enclosure, spill point, outlet), which may hold a lake or be realised as a flat (3F-10, 3F-11, 3F-14 decide realisation); an **intentional lake** — a declared basin with a retention rule (surface at the spill level, one outlet to the ocean, uncarved sill, `HYDROLOGY_GENERATION_PLAN.md` §9); and an **accidental pit** — any undeclared closed depression, a defect. Because fill-then-spill turns any pit into a "lake", every lake the analysis finds must lie in a declared basin or it is reported as a defect. The ocean-outlet invariant stays; endorheic basins (c) are not adopted. Term: [90 Glossary](90%20Glossary.md) § Declared Basin. **Research required:** how the chosen model declares basins and their depth (B4, F2); inland water rendering stays outside V0. Authorises no lake generation, no lake experiment, no change to V0's ocean-only water.

**GEO-6. Geometry-first tooling. [before G0 and G1] — ANSWERED 2026-10-18: (a) build both** (project owner; moved to § Resolved Decisions; G0 and G1 built the same day). May the read-only geography metrics dump (G0) and the isolated, heights-only Geometry-First Review tool (G1) be built as Editor-only tooling? Neither changes production code, the bake, the production scene or a frozen file; both write outside `Assets`.
- *Options:* (a) build both; (b) G0 only; (c) neither — keep top-down dumps only.
- *Consequences:* (a) standard oblique and eye-level captures on a neutral material for every candidate; new Editor files to maintain; (b) numbers without views; (c) geography judged from top-down images, the gap the native preflight already recorded (M9).
- *Agent recommendation:* (a).

**GEO-7. Target and acceptance thresholds. [after G0 — G0 has run, baseline in the research record §7.6] — target and classification RESOLVED 2026-10-21 (owner direction; moved to § Resolved Decisions); next reading set proposed.** Is the target geography of the research record §4 the accepted direction, and which of the proposed metrics of §7 become gates, with which thresholds? Until then every proposed target is reported, never gating.
- *Options:* (a) adopt after G0 and G1 evidence; (b) adopt now; (c) revise the target first.
- *Agent recommendation:* (a).
- *Evidence since (G3):* a system that reads as mountains conflicts with two existing Draft targets on today's footprint — land below 5° falls to 12–16 % (target ≥ 35 %) and land above 45° rises to 9.6–21.1 % (target 6–15 %); and three G0 definitions built for `H4A` read a candidate only partly (D-a/D-e use production's declared structure, M3 (a)/(b) use production's massif mask, M5 "pass" needs G0's flow-divide skeleton). Recorded for the threshold decision, not acted on.
- *Evidence since (G3b, research record §12.7, §12.8):* no variant reaches Strahler order ≥ 3 at the 30 000 m² channel area with the 200 m gate — measured order 2 on all seven seeds, and the routed tree's own design order is only 2–3 — and land below 5° stays at 15.2–17.1 % beside a mountain body covering 24–31 % of the land. Whether the D-b target and its channel area fit a ~3 km² island, and whether ≥ 35 % flat land can coexist with a mountain system on it, belongs to the threshold decision; a smaller channel area would change the measurement, not the island. Recorded, not acted on.
- *Owner direction (2026-10-20, G2 task):* the goal is a convincing, explorable world approaching the reference in geographic richness, hierarchy and readability; metrics help evaluate it and must not restrict it. Strahler order ≥ 3, the 30 000 m² area (in `HydrologySettings` a channel-initiation threshold, not a basin target) and ≥ 35 % of land below 5° are **not** approved hard requirements and are reported as diagnostics in G3c. The target itself is now D6 (brief §9), superseding research record §4 where they differ. **Still open:** which criteria become gates, with which values — the brief's §7 classification and framework (D7) await the owner.
- *Evidence since (G3c, research record §14.7):* two readings showed weaknesses for the threshold decision. R3 counts a buildable patch that spans several regions once, so abundant, continuous buildable land fails it although every lattice held its settlement sites; R5 passes vacuously when no river rises in the highland (four of ten seeds on contour A) and fails when the hydrology planner rejects its own network ("reach flows uphill", five of twenty lattices). Recorded, not redefined.
- *Decision record (2026-10-21, review §2.6).* **Target — Resolved — owner direction:** D6. **Classification — Resolved — owner direction (2026-10-20; 2026-10-21: requirements serve the target's ambition, not easy metric passes):** hard gates are the requirements with direct gameplay or topological meaning — 3–5 settlement sites including a capital-size one, numerous varied reachable POI sites (capacity, no quota), walking routes between settlement regions, reachability of intended places (exceptions declared, 3F-20), no accidental topological defects (S8 outside declared basins, climbing valley axes, D-c, a network the production planner accepts), elevated rivers to designated outlets, and process gates; the owner's visual review at three scales judges composition; Strahler order, basin areas, slope shares, bifurcation, Hack exponent, M1–M6 and tier conformance are diagnostics unless a gameplay purpose is recorded; D2's values are provisional assumptions. Pre-registered thresholds are never changed retroactively — G3c's R1–R8 and rule stand as evaluated — and flat land is never rewarded for its own sake. **Next reading set — Proposed — owner confirmation required:** R1–R8 reviewed one by one with a v2 proposal and R9 (routes between regions), R10 (production planner validity), R11 (POI-site capacity), plus report-only form diagnostics Q1–Q9 (review §3), for pre-registration before the next evaluation. **Research required:** values for the new gates. The acceptance table of `WORLD_RELIEF_PRINCIPLES.md` §6 keeps its role as a regression check on today's `H4A` bakes.
- *Since 2026-10-21:* the LMC design note (v0.2, the owner's revision — [GEOGRAPHY_LMC_DESIGN_NOTE.md](unity/Assets/Plans/GEOGRAPHY_LMC_DESIGN_NOTE.md) §7) classes R4, R5, R7, R8, R9 and R10 as hard gates, R1, R2 and R6 as indirect indicators and R3, R11 as diagnostics, with the passage rule "all hard gates on ≥ 7 of 10 held-out seeds and the owner's composition acceptance on ≥ 7 of 10"; exact calculations go into a metrics specification the owner approves before calibration (note §12 item 6). Binding for that experiment only once the note is approved; a proposal for GEO-7 beyond it. *Owner decision (2026-10-21), resolving a discrepancy with this question's classification:* the note's R5 now reads "at least one river originates in a mountain or plateau region, and every such river system ends at a designated outlet" (note v0.3, §7.1; agent review note 1), so the LMC gates carry this question's hard gate on elevated rivers and designated outlets.
- *Since 2026-10-22:* the metrics specification the note asks for is drafted in [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_LMC_TECHNICAL_SPEC.md) §§7–8 and checked on G3c's historical lattices (§11). In line with this question's classification, Q1–Q9 never gate; the specification gives a **review flag** only to the Q measures the check showed to separate a failure (Q1, Q5, Q6, Q7, Q8), at the level of the G3c lattices that showed it. **Approved by the owner on 2026-10-22** (specification §12 items 1–4) for LMC; Step 1 is not authorised. Beyond LMC it stays a proposal for this question. *2026-10-23 (owner, specification v0.4):* two report-only slope diagnostics added (D-cap, D-reg); because no measure separated the whale-back on G3c's data, that failure mode is judged in the owner's visual review and Q3, Q4 and Q4m stay unflagged — a case of this question's rule that composition is judged in the review.

**GEO-8. Plateaus and escarpments in the tier system. [before production design of plateaus; G3c gathers evidence] — principle RESOLVED 2026-10-21 (owner direction); tier contract proposed (c); numbers research required.** The approved target D6 has several large plateaus with readable tops and pronounced cliff or escarpment edges, integrated into ridges and foothills. `WORLD_RELIEF_PRINCIPLES.md` §1 fixes T2 plateaus at 18–40 m above local ground and 150–450 m across, 6–10 per island, and forbids forms between tiers (adjacent tiers differ by ≥ 2.5× in height); its T1 row is the massif. A large plateau whose escarpment is, say, 60–120 m would sit between T2 and T1.
- *Options:* (a) large plateaus stay within the T2 row (18–40 m escarpments, larger tops allowed if the footprint row changes); (b) change the T2 row (heights, footprint, count) for the new target; (c) treat large plateaus as a relief-region form (Geographic Hierarchy level 1) outside the tier table, with the 2.5× rule applying to forms standing on them.
- *Consequences:* (a) keeps the rule; escarpments may read low against a 200–330 m range; (b) re-specifies a Draft rule every future plateau stage reads; (c) introduces a form class the tier table does not name and needs a Glossary entry.
- *No recommendation.* G3c draws each plateau's escarpment from two classes — within the T2 row (18–40 m) and above it (60–120 m) — and reports tier conformance; that is evidence for this question, not an answer. *Owner decision for G3c only (2026-10-20, brief §9 checklist):* inside G3c the tier rules are a **diagnostic**, not a constraint, so that the experiment is not pre-limited by the rule under question; for production they stay binding until this question is answered.
- *Evidence since (G3c, research record §14.5; not an answer):* plateaus of both classes were built and verified (top flat, escarpment on ≥ 20 % of the rim, a walking route to the top) on 19 of 20 lattices; by the tier rule the 60–120 m class falls between T2 and T1, and in the views it reads as the stronger landform. Both classes still read as drum-like, which is a construction matter, not a tier one. Related: whether a cliff face is impassable in play is 3F-20 (the production jump climbs every tested slope up to 70°).
- *Decision record (2026-10-21, review §2.7).* **Principle — Resolved — owner direction (2026-10-21: several large plateaus in different regions, integrated into ridges and foothills; mountains dominant; mixed traversal):** a candidate is not rejected solely because a large plateau falls outside the T2 row or between tiers under the generic 2.5× rule, so option (a) is not the target; **tier labels classify scale and legibility, never traversal** — whether a face blocks the player is read from steepness and walking connectivity and decided under 3F-20. The tier table and the 2.5× rule stay in force for production design until deliberately revised (no plateau stage exists in production). **Revised contract — Proposed — owner confirmation required, default (c):** the T2 row stays for district-scale shelves; a **Large Plateau** is a level-1 relief-region form outside the T1–T3 height table, separated from T1 by form (a readable top behind an escarpment on part of its rim, joined to a ridge or foothill on another) and subordinate to the mountain system (top below its main culminations, never the landmark); the 2.5× rule applies to forms standing on it. (b) is not recommended: widening T2 to ~120 m merges two forms and breaks the T2/T1 separation (120 m × 2.5 = 300 m against T1's 250 m floor) unless T1 moves too. Term: [90 Glossary](90%20Glossary.md) § Large Plateau (proposed). **Research required:** escarpment height range, top area, count and subordination margin; minimum evidence — escarpment and top heights against culmination heights on calibration and held-out seeds of a model whose plateaus are not overlays, the owner's review of regional obliques at several height ratios, and the plateau-integration diagnostic (review §3.3 Q6). G3c's figures are existence evidence, not a basis for numbers.
- *Evidence route (LMC design note v0.2 §§6, 7.3, not run; classes to be confirmed by the owner):* plateau edge height classes **18–40 m and 60–120 m** against main summits of 330–430 m, plateaus built as part of the surface rather than as overlays, tier classification reported as a diagnostic. (The agent's v0.1 ratio classes are superseded.) *Measurement checked (technical specification §§2.4, 11, 2026-10-22):* the plateau edge-height definition, corrected after the check (K1), recovers G3c's built escarpment heights within ± 25 % on 38 of 44 plateaus (absolute error median 3.8 m) — on the same data, so independent confirmation waits for Step 1. *Approved for LMC (owner, 2026-10-22, with the specification's §2):* the classes 18–40 m and 60–120 m and the K1 edge-height definition. The large-plateau contract itself (option (c)) and its numbers stay open.

**GEO-9. Geography and settlement / POI placement — which decides what. [before production design of settlement or POI integration] — policy RESOLVED 2026-10-21 (evidence-backed policy); values open.** Added by the post-G3c review for the relationship the owner's request described under "GEO-3" (review §2.3, §2.8). How do terrain generation and settlement and POI placement divide responsibility so that the island holds 3–5 settlements (one possibly a capital) and numerous varied, reachable POIs without the terrain being distorted to meet a count?
- *Evidence:* G2 — capacity on today's surfaces is set by the coastal ramp and the mountain share (brief §4.3); G3c — settlement sites on all twenty lattices, including those that read worst; W.4's V0 demo requests markers by quota over a 3 × 3 sector map, scored per kind by local slope ([17](17%20Game%20Systems%20Architecture.md) W.4).
- *Decision record (2026-10-21, review §2.8) — **Resolved — evidence-backed policy**:* **geography generation** (TerrainGen) owns regions and their relief, candidate buildable areas as a consequence of designed lowlands, valley floors, plateau tops and coastal plains, routes, passes, barriers and bypasses, coastal forms and outlets, and exposes them as read-only structure with capacity as a diagnostic; **placement** (W.4, W.5, roads under the Road Passability contract) owns site choice, counts, spacing, the capital, pacing and the road network, edits terrain only as its own contracts allow (road earthwork within `MaxCutDepth` / `MaxFillHeight`; building pads, deferred) and never reshapes terrain at region scale to meet a count. When capacity falls short, the fix is in the geography's design; a site count alone never proves geographic success; capacity is evaluated with route quality (reachability, road-feasible links between settlement regions, alternatives, detours).
- *Depends on (open):* D2 (settlement size classes, slope, spacing; terraced towns), D3 (travel pacing), POI pacing, 3F-20, the timed-POI density of [04](04%20Points%20of%20Interest.md); the working terms *settlement site* and *POI site* await D9 before Glossary entries. Authorises no change to W.4, W.5, roads or the POI demo.

**GEO-10. Geographic ownership and regional assembly. [before G3f's specification (G-3); answered at G3f's architecture decision (G-6)] — open; the hypothesis approved for testing 2026-10-25 (G3f D-2).** Who owns the island's heights, and how do regions combine into one surface? Should every land vertex have exactly one height owner — a declared drainage unit whose boundaries are divides — with every landform reserving everything it writes (its write footprint), and should the surface then be synthesised in one unified solve or per unit and merged?
- *Options:* (a) a single-writer Geography Plan and one unified synthesis; (b) the same plan and per-unit synthesis with boundary heights prescribed by the plan, merged by concatenation (regional assembly); (c) layered stages composed by overlays, as G3c and S-LM1 were built.
- *Evidence:* G3c needed 18 named construction fixes, each patching an interaction between independent layers (post-G3c review §1.3, L7); S-LM1 needed 11 named construction fixes and 3 layout fixes and was built as a chain of stages joined by `max` / `min` overlays ([GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md) §9.2; G3f plan §2.2); LM2 reserved less than S-LM1's plateau stage wrote — 2.26–6.06 ha per plateau outside its painted footprint, a corridor raised by up to 123 m, a ramp into a sea cliff ([GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](unity/Assets/Plans/GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) §17.2); S-LM1's base let the coastal surface capture highland drainage on 110201 in every run (G3e-PM D4).
- *State:* the owner approved the G3f plan's architecture as the hypothesis to test, not as a decision. G3f's regional prototype measures (a) against (b) under identical conditions (hypotheses H1 and H3, plan §6.1); the owner decides at G-6. *Agent reading:* (c) is not recommended on the evidence above; no recommendation between (a) and (b) before they are measured.
- *G3f decision D-4 (2026-10-25, the plan's decision record):* the test is C0 against U-F, with R-F implemented only after U-F's analytic checks and compared as "crests set by the plan, synthesis per unit" — U-F and R-F differ in two factors, so their comparison does not isolate assembly; A1 is measured on both, and a tie goes to the simpler candidate. The question itself is still answered at G-6.
- Authorises no code, generation or production change.
- *2026-10-27:* under the owner's research mission (2026-10-25) G3f's specification ([GEOGRAPHY_G3F_TECHNICAL_SPEC.md](unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md), Draft) fixes the test: one owner per vertex (catchments, coastal fringes, crest records), the flow gate GF-B, and the assembly gate GF-A measured on the unified (GF-UF) and per-unit (GF-RF) syntheses. Research only; this question stays open until G-6.
- *2026-10-28 (research evidence, not an answer):* G3f ran under the mission (specification v0.3 §16): calibration run 1 (no candidate qualified), named fixes F1 (a plan-guided tree) and F2 (the reserve moved), run 2: GF-RF passed every hard and process gate on all four calibration variants and was frozen; held-out 5001–5010: GF-RF part (1) of the pass rule holds, 8 of 10 (failures 5003, an axis mouth across a bay, and 5006, plan invalid); GF-UF 4 of 10 (diagnostic), GF-C0 0 of 10; the owner's blind review (part (2)) has not taken place, so nothing is declared passed. GF-RF's assembly was local and order-invariant on every valid variant; GF-UF's emergent divide rose away from the secondary culmination toward the coast. *Agent reading:* the evidence favours a plan with planned divides and per-unit synthesis over the unified emergent solve, but GF-UF and GF-RF differ in two factors (D-4), so assembly alone is not isolated; one fixture on one sector, one machine. The owner's review and G-6 remain.

**GEO-11. The terrain authority's form. [before G3f's synthesis (G-4b) for research; at G4 for production] — open.** Two parts. (1) May the terrain authority be a global solve over the world lattice — as the hydrology analysis already is — instead of an analytic point function, as every production stage has been so far? (2) Does law 11 of [17](17%20Game%20Systems%20Architecture.md) — Piece-Based Assembly, "binding for Track W" and naming "World … generation" — govern the terrain height field, or only placed content (settlements, buildings, interiors, props)?
- *Facts:* production consumes the authority only as a world-spanning lattice — the bake, the validation and the bake tests are the only callers of `TerrainAuthorityField`, and `OverworldTerrainField.FromSamples` samples it once per lattice vertex — so tile seams stay exact whatever computes the lattice; S-LM1, described as a point function, used lattice-wide distance transforms, geodesics and a conjugate-gradient solve as built; a point function keeps small test regions exact (research record §3.5), which a global solve gives up; the terrain authority has been a continuous field since TerrainGen, and the W.2 card of 17 still describes "noise / band rules" (G3f plan §2.2, rows 6, 7 and 10).
- *Options, part 1:* (a) allow a global lattice solve for research now and decide production at G4; (b) allow it for research and production now; (c) require a point function. *Part 2:* (a) clarify that law 11 governs placed content and the terrain height field follows the terrain stack of [AGENTS.md](AGENTS.md); (b) law 11 applies to terrain, so a landform-piece model would be required; (c) leave it as written.
- *Agent recommendation:* part 1 (a) (G3f plan D-8); part 2 (a) (D-9) — law 11 is a locked law, so only the owner can clarify it. Part 1 must be answered before any G3f synthesis runs, because its candidate laws are lattice solves.
- *G3f decisions (2026-10-25, the plan's decision record):* **part 1** — D-8: option (a), a global lattice solve for G3f's research candidates only; any production use is a G4 decision. **Part 2** — D-9: the owner accepted the new wording of law 11 quoted in the plan's decision record (placed content is Piece-Based; the terrain height field follows the terrain stack). Recording it as this question's answer and writing it into 17, 14 and the Glossary is the D-9 documentation package, **not yet authorised**; until then 17 and 14 carry the old text. The W.2 correction of 17 is deferred.
- Authorises no change to 17, to the terrain authority or to production.

---

# Low Priority

Achievements

Exact difficulty number tuning beyond the directional rule

Localization

Accessibility

Mod support

Pathway lore names and full church/entity catalogues

Stages 6 → 0 content

---

# Rule

Whenever a future document depends on one of these questions,
that question must be resolved before continuing.

No design decision should rely on undefined concepts.

Resolved Decisions may be revisited only through the Canonical / Draft change policy, with explicit reason and document updates.
