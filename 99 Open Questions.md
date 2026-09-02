# 99 Open Questions

Version: 0.26

Status: Draft

Last Updated: 2026-08-31

Depends On:
All Canonical Documents

Referenced By:
Future Design, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [AGENTS.md](AGENTS.md)

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

**What is the V1 turn structure?**  
Move + Action + optional short Bonus **on Combat Arenas only**. Documented in [10 Turn Action Economy](10%20Turn%20Action%20Economy.md).

**What is the spatial model?**  
Free third-person overworld and interior scenes; square grid + Environment Actions on arenas only. Documented in [16 Architecture Pivot](16%20Architecture%20Pivot.md) and [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md).

**Which engine?**  
Unity locked. URP, C#, project folder `unity/`, free 3P + custom arena grid TB. Documented in [12 Engine Choice](12%20Engine%20Choice.md).

**What is the first playable proof?**  
A Vertical Slice in Lower Veil fantasy with 2 selectable factions (Wardens / Veinmarket), 3 POIs, Information Sources, arena Confront, one Bargain tease, and Lord of the Mysteries-like industrial-era mood — delivered on overworld → interior → arena rails. Death Gap is out of slice scope. Placeholder names may change. Documented in [11 Vertical Slice](11%20Vertical%20Slice.md) and [16](16%20Architecture%20Pivot.md).

**Where do systems / interfaces / micro-iterations live?**  
[17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) — System Cards + Track backlog. Spatial locks remain in [16](16%20Architecture%20Pivot.md).

**Overworld V0 size / chunk / scene load defaults?**  
Chunk = **64 m**; playable world = **2800 × 2800 m** (2×2 Vista tiles of 1400 m, height budget 700 m, sea level 24 m) with an infinite-capable chunk API; Interior and Arena V0 use **single-scene swap** (additive later). Documented in [17](17%20Game%20Systems%20Architecture.md) and `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`.

**What generation mechanism produces World / Settlement / Building / Interior content?**  
**Piece-Based Assembly**: small hand-authored Pieces carrying Snap Sockets, snapped together deterministically small → large. **Exterior Shell Grammar** builds overworld fabric (most buildings never get a Room Graph). **Interior Piece Assembly** runs only for `InteriorPolicy.Portal` buildings. Reference model: Valheim's room/dungeon piece system. Not an algorithmic subdivision solver, not the retired OrganicTown continuous-mesh generator. Documented in [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).

**Is the OrganicTown / City Generation prototype a reuse source for the new pipeline?**  
No. [13 City Generation](13%20City%20Generation.md) is Deprecated: not viable (not modular, not optimized, did not read as a real city), not a bridge, not extended. Only generic non-city-specific ideas (deterministic `WorldSeed` fork-by-name) carry forward.

**Overworld terrain height budget?**  
**700 m** — `WorldScale.TerrainHeightBudgetMeters` matches VistaManager `terrainMaxHeight` and tile `size.y`. Documented in [VISTA_OVERWORLD_REFERENCE.md](unity/Assets/Plans/VISTA_OVERWORLD_REFERENCE.md).

**How is the island / ocean silhouette framed?**  
36 harmonic asymmetric LPB **Anchors** + **Outer Falloff (180 m)** + **SoftFalloff BiomeMaskGraph** on `LPB_IslandBase` (order 10, IslandPlateau graph, Height Replace, `worldHeight` 140 m, biome Y at sea level), sitting on `LPB_SeaFloor` (order 0). The coast is graph shaping + falloff with **no post-Vista pass** — the rim fades under `WorldWater` at Sea Level **24 m**. Surface paint comes from the graphs' Weight Blend over the five canonical layers; sand is a normalized low-altitude band × noise, not an ASL metre band. Mountains are **separate** LPBs (order 20+) with 28 elongated ellipse anchors, wide falloff (520) and **Raise** height delta — not circular KeepHigher stamps and not one Mountain graph for the whole island. Documented in [VISTA_OVERWORLD_REFERENCE.md](unity/Assets/Plans/VISTA_OVERWORLD_REFERENCE.md) and [OVERWORLD_VISUAL_DIRECTION.md](unity/Assets/Plans/OVERWORLD_VISUAL_DIRECTION.md) § Terrain surfaces.

**Overworld height / island framing?**  
**700 m** height budget; `LPB_IslandBase` at 140 m over a 20 m seabed, plus mountain Raise LPBs at 530 m; ocean Sea Level **24 m**. Biome canvas capped at **512 px** by Vista Personal. Documented in the Vista / NATURAL_ISLAND plans.

**V0 inland lakes / rivers?**  
**Removed from code (2026-08-15).** V0 ships **ocean only** (`WorldWater` at Sea Level). No `LakeDefinition` / `RiverTracer` / lake pipeline steps. Future inland hydro is a redesign. Alpine multi-plane lakes stay deferred; single Sea Level ocean plane is the locked V0 water model.

**May a road cross mountain terrain?**  
**Yes — mountain terrain is traversable, not a hard obstacle.** The planner prefers cheaper ground (passes, contours, switchbacks) over a direct expensive climb and expresses that as price, not refusal; `MountainPenalty` / `MountainCost` stay soft and must not become `+Infinity` because the biome is Mountain. Only water / `+Infinity`, earthwork beyond `MaxCutDepth` / `MaxFillHeight`, and grade at or above `MaxWalkableGrade` block a route outright. `ExistingCorridorDiscount` is construction cost only and must never change passability. Reviewed and approved by the project owner 2026-08-31, from [WORLD_GENERATION_PIPELINE.md](unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md) §3 «Roads are pathfinding, not lines … mountains (passes) — never straight through a mountain». Glossary: [90 Glossary](90%20Glossary.md) § Road Passability. Agent rule: [AGENTS.md](AGENTS.md) § Accepted Draft Decisions. **Approved but not Canonical:** the owner review required by `001 Documentation Standards` § Promotion Rules is granted and every other promotion requirement is met; what is missing is a host, because no Canonical document covers world generation and the document carrying the statement is Draft with unrelated content.


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

How strongly may a Starting Faction nudge before it becomes a soft lock?

How do External Power Bargains interact with Pathway conduct when they pull in opposite directions?

What happens if a character tries to abandon or switch Pathways after stage 9?

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

What asset/API provides waterfall meshes if inland hydro returns later? (Deferred with lake/river redesign.) ([NATURAL_ISLAND_GENERATOR.md](unity/Assets/Plans/NATURAL_ISLAND_GENERATOR.md))

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
