# 99 Open Questions

Version: 0.29

Status: Draft

Last Updated: 2026-10-17

Depends On:
All Canonical Documents

Referenced By:
Future Design, [02 World Laws](02%20World%20Laws.md), [03 Decision Engine](03%20Decision%20Engine.md), [03.1 Decision Node Template](03.1%20Decision%20Node%20Template.md), [04 Points of Interest](04%20Points%20of%20Interest.md), [05 Progression](05%20Progression.md), [06 Approaches](06%20Approaches.md), [07 Character Life](07%20Character%20Life.md), [08 Confront Constraints](08%20Confront%20Constraints.md), [09 Information Sources](09%20Information%20Sources.md), [10 Turn Action Economy](10%20Turn%20Action%20Economy.md), [11 Vertical Slice](11%20Vertical%20Slice.md), [11.1 Vertical Slice POIs](11.1%20Vertical%20Slice%20POIs.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), [AGENTS.md](AGENTS.md), [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md)

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

What asset/API provides waterfall meshes if inland hydro returns later? ([HYDROLOGY_GENERATION_PLAN.md](unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md))

## Terrain — Stage 3F valley carve

Research record: [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) (phases 1–10, 2026-10-14 – 17; §22 holds the proposed contracts, §23 the current position). Production terrain is unchanged and Stage 3F stays an experimental downstream carve; these questions block a production design of Stage 3F, not the current bake. D1–D3 below were research requirements set by the owner for the experiments, not approved invariants; they are unrelated to the slope-reading labels D2–D7 of handoff §19. The measured counts are robust to the S8 thresholds and the matching rule (handoff §9), so the answers are design choices, not threshold tuning.

**Working decisions for the research prototype only (owner, 2026-10-16; not production decisions):** canonical `FieldResolution` = 1025; 4-connected band-local priority flood; in corrected cells the corrected lattice is authoritative and point queries are bilinear on it; corrected height ≤ `H4A`; Stage 3C untouched; `Declared` valley ends only. The isolated prototype built on them is recorded in handoff §16. The bilinear point query is contradicted by a later measurement — Unity serves triangles (handoff §18) — and is reopened in 3F-6, not changed here.

**How to read the entries.** Each question keeps its original wording, then lists options, their measured consequences and an agent recommendation. A recommendation is not a decision. Three questions — filling inherited depressions (3F-10), flat-filled pits (3F-11) and jump and barriers (3F-20) — carry no recommendation: they are the owner's call. Tags: **[stage 1]** must be answered before a dump-first stage 1 of E is implemented; **[production]** before E is enabled in the production bake; **[later]** may follow.

**3F-1. Final surface and structural height. [stage 1]** Should `H4A` stay the structural height that Stages 1–4A, the frozen Stage 3C trajectories, Stage 3D/3E topology and structural diagnostics read, with E a separate final surface — the only height written to `TerrainData` — and nothing flowing from E back into TerrainGen? (handoff §22.1)
- *Options:* (a) the separation of handoff §22.1; (b) E replaces `H4A` as `TerrainAuthorityField`'s height.
- *Consequences:* (a) frozen stages stay bit-identical and keep their meaning; two named heights must be kept apart, and `OverworldTerrainValidation`, which today compares tiles with `TerrainAuthorityField`, must compare them with E. (b) changes the authority's meaning, breaks the statement that Stage 4A is the authority and lets downstream data reach the frozen stages.
- *Agent recommendation:* (a).

**3F-2. Drainage invariant (D1). [stage 1]** Is plan-view drainage (D1) a Stage 3F acceptance invariant — no new significant S8 depression relative to `H4A` outside permitted basins and no deepening of inherited ones — with the axial `CLOSED` classifier kept as a diagnostic only? Production `H3F` creates 211 such depressions on seeds 1000–1039; all 36 axial closures of `H4A` are route closures, not basins (handoff §9). The prototype of E meets it on 47 seeds at 1025 (handoff §16).
- *Options:* (a) D1 as the invariant, axial classifier diagnostic; (b) the axial classifier as the invariant; (c) no drainage invariant.
- *Consequences:* (a) E meets it on the lattice and on the served triangle surface (handoff §18); (b) fails on `H4A` itself, whose axial closures are route closures; (c) leaves Stage 3F without an acceptance basis.
- *Agent recommendation:* (a).

**3F-3. Independent oracle. [stage 1]** When a Stage 3F candidate itself uses priority flood, does D1 have to be confirmed by an independent scanner (sort + union-find, separate code) as the test oracle? (handoff §12)
- *Options:* (a) yes; (b) the flood checks itself.
- *Consequences:* (a) a test-only scanner to maintain; a fault in the flood cannot certify itself. (b) cheaper, and a fault in the flood certifies itself.
- *Agent recommendation:* (a), with a triangle-graph scan added, because the served surface is triangles (handoff §18).

**3F-4. Plain incision bound (D3). [stage 1]** Is 7.2 m (`CorridorIncisionMeters`) a bound on plain incision below the massif toe only, with the mountain carve bounded separately (D3)? Deeper plain cuts do not buy drainage — 12 m creates 217 depressions against 222 at 7.2 m — and read as railway cuttings (handoff §9).
- *Options:* (a) plain only, mountain bounded separately; (b) one bound everywhere; (c) a deeper plain bound.
- *Consequences:* (a) as prototyped: 7.200 m on the float lattice, 7.2066 m on the served surface after 16-bit storage (handoff §18), so a check needs one storage step of tolerance; (b) caps the massif valleys; (c) buys no drainage and reads as cuttings.
- *Agent recommendation:* (a), checked on the float lattice with a 1.07 cm tolerance on the served surface.

**3F-5. Band-local correction in production. [stage 1]** Is a band-local raster correction admissible in production TerrainGen — a fill confined to carved cells inside the corridors, with outlet heights taken from `H4A`? Adopted for research only (4-connected, 1025). Measured: it holds on the lattice, on any triangulation, after 16-bit quantisation and under aligned bilinear refinement, deterministically across processes; it is not resolution-invariant (common nodes differ by up to 0.32 m between 513, 1025 and 2049) and costs 0.3 s per seed (handoff §§15–16). Without it the flank must be reshaped (variants A / C / D: 20–23 % less mountain carve, deformed mountain valleys) (handoff §10).
- *Options:* (a) admit it as prototyped; (b) reshape the flank (A / C / D); (c) no correction, accept the depressions.
- *Consequences:* (a) meets D1 on the lattice and on the served surface (handoff §§16, 18), ties the world to its resolution (3F-7) and makes flat-filled pits (3F-11); (b) 20–23 % less mountain carve; (c) fails D1 (3F-2).
- *Agent recommendation:* (a), subject to the owner's answers on 3F-10 and 3F-11.

**3F-6. Point-query contract. [stage 1]** Point-query contract of a corrected surface: keep the mixed reading (bilinear on the corrected lattice in corrected cells, the analytic carve elsewhere — the prototype's working contract) or read the final lattice bilinearly everywhere? The mixed reading jumps by up to 1.09 m across corrected-cell edges (0.53 m on tile seams) and exceeds analytic `H4A` between nodes by up to 6.8 cm; the lattice-only reading has neither defect, creates no depression, leaves `Declared` gaps exactly as the uncorrected lattice has them, and gives up analytic sub-lattice detail a 1025 heightmap cannot store anyway (mean 4 mm, up to 5.36 m on a steep slope). The bake, `OverworldTerrainField` and the terrain collider already read the lattice (handoff §16). **Measured since (handoff §18):** Unity serves neither reading. `Terrain.SampleHeight` and the collider read the quantised lattice as triangles split SW→NE (≤ 0.4 mm); bilinear differs from that by up to 2.59 m at the centres of twisted cells.
- *Options:* (a) mixed reading R1; (b) bilinear on the final lattice; (c) triangle reading of the quantised final lattice.
- *Consequences:* (a) discontinuous, above `H4A` between nodes, and not what the collider serves; (b) continuous, yet up to 2.59 m from the collider, so any consumer reading it disagrees with physics; (c) equal to `SampleHeight` and the collider, keeps every E invariant (0 new depressions, E ≤ `H4A` everywhere, gaps unchanged), gives up sub-lattice detail the heightmap cannot store.
- *Agent recommendation:* (c), with the point query and the lattice evaluator sharing one function (handoff §16, implementation note).

**3F-7. `FieldResolution` as world identity. [stage 1]** Is `FieldResolution` part of a world's identity for Stage 3F in production, as for the prototype? Both the correction and the Stage 3C trajectories (up to 3.7 m) change with it (handoff §15).
- *Options:* (a) yes — 1025 is part of the world's identity, a change is a new `GeneratorVersion`; (b) no.
- *Consequences:* (a) deterministic worlds; changing heightmap resolution means a new world; (b) would need resolution invariance, which was measured absent.
- *Agent recommendation:* (a).

**3F-8. Per-arc data. [stage 1]** Where do the per-arc arrays of a floor-referenced carve (floor and allowed depth along the arc) live: as immutable Stage 3F data next to `FinalValleyTrajectory`, or recomputed on every evaluation? (handoff §13) `CarveField` is bound to the frozen `ValleyCorridor` trajectories; binding to `FinalValleyTrajectory` is a scoped Stage 3C unfreeze (`PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §15.2).
- *Options:* (a) recomputed inside the Stage 3F module from the frozen trajectories on each evaluation; (b) immutable data next to `FinalValleyTrajectory`; (c) immutable data stored with the frozen trajectories.
- *Consequences:* (a) no new persisted data and no unfreeze, at an evaluation cost; (b) requires the re-binding; (c) attaches new data to frozen code.
- *Agent recommendation:* (a) for stage 1; revisit only with an approved re-binding.

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
- *Consequences:* (a) flat steps up to 4.4 m deep on the massif flank; (b) 20–23 % less mountain carve, or a redesign; (c) depends on inland water, which V0 does not have.
- *No recommendation.*

**3F-12. Valley-ending semantics. [production]** Route-barrier policy (valley-ending semantics): where a declared trajectory rises more than the plain bound can cut, does the valley stop and restart downstream (gap), end (termination), or continue as a flat-filled floor? Continuity, no reappearing segments, the 7.2 m bound and D1 cannot all hold there (handoff §10). Measured on 8 seeds: ending at the first barrier is stable and creates no depression but loses 4.2 % of plain valley length and ends valleys 52–342 m inland; the prototype keeps `Declared` ends and shows 41 gaps on 34 of 299 trajectories, 36 of them on coastal turn-backs (handoff §§15–16).
- *Options:* (a) `Declared` gaps; (b) termination at the first barrier; (c) a flat-filled floor.
- *Consequences:* (a) shows 41 gaps on 34 of 299 trajectories, identical on every height reading (handoff §18); (b) stable, 4.2 % less plain valley, valleys end inland; (c) breaks the plain bound or D1.
- *Agent recommendation:* `Declared` for the dump-first stage 1 with every gap reported; decide before production.

**3F-13. Valley mouths. [later]** Is a geometric valley-mouth rule wanted for trajectories that approach the coast and turn back inland? The rule tested — end at the running minimum of the Stage 4A gate — is not a mouth: 8 of 12 such ends lie 60–160 m inland with up to 3–4 m of valley cut off, while the trajectory reaches the sea later anyway (handoff §15). A different rule would be a new variant.
- *Options:* (a) no mouth rule in V0; (b) design a new mouth variant.
- *Agent recommendation:* (a); a new variant is its own research item.

**3F-14. Cirques as permitted basins. [later]** Are declared Stage 3E cirques permitted basins under D1? 50 of the hybrid's 100 pre-fill flank depressions lie inside them. Their depth bound is still B4 (`PROCEDURAL_TERRAIN_STAGE_3E_PREFLIGHT.md` §22.3) and their realisation F2 (`PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §15) (handoff §10).
- *Options:* (a) not permitted in V0 — the correction fills them like any carved pit; (b) permitted — the correction leaves them, and their depth bound B4 and realisation F2 must be decided first.
- *Agent recommendation:* (a) for V0, decided together with the Stage 3E basin question.

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

**3F-19. Slope readings banned from physical decisions. [production]** Adopt handoff §22.5: slope readings D2 (`GetInterpolatedNormal`), D3 (`OverworldTerrainField.SlopeDegreesAt`) and D6 (`OverworldTerrainDiagnostics` reachability) are never used for walkability, footing, spawn or gating.
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
