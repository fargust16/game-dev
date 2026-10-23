# G2 — Target Geography Brief

Version: 0.5

Status: Draft — decision brief. **One owner decision is recorded as approved here: D6, the target geography
(2026-10-20)**, defined by the owner's concept image and the D6 wording in §9. **D8 set the scope of the
G3c research experiment; the owner reviewed the plan (five points, 2026-10-20), gave the go-ahead on the
pre-run checklist the same day, and G3c ran on 2026-10-20 … 21** (research record §14; neither contour
passed). **Since the post-G3c review (2026-10-21)**, the GEO questions carry decision records in
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §2 and
[99 Open Questions](../../../99%20Open%20Questions.md): GEO-1 and GEO-2 are resolved, GEO-7's classification
and parts of GEO-4, GEO-5 and GEO-8 are resolved, and the rest — production placement (GEO-3), the coastal
contract, the large-plateau contract, every threshold marked provisional (D2) and every architecture
recommendation — stays open.

Last Updated: 2026-10-21

Depends On:
[AGENTS.md](../../../AGENTS.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[99 Open Questions](../../../99%20Open%20Questions.md), [90 Glossary](../../../90%20Glossary.md),
[04 Points of Interest](../../../04%20Points%20of%20Interest.md),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§4, 7, 11, 12, 13),
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md),
[PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md),
[WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md),
[LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md);
reference image [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)

Referenced By:
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §§4.1, 9, 13, 15;
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md);
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md);
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-1 … GEO-8;
[AGENTS.md](../../../AGENTS.md); [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) §§1, 6;
[90 Glossary](../../../90%20Glossary.md) § Geographic Hierarchy

**Revision history.** 0.1 (2026-10-20): first brief — G3/G3b findings, requirements framework, settlement
diagnostic, footprint options, coastal direction, criteria, architecture comparison, G3c outline.
0.2 (2026-10-20): the owner supplied and approved the reference image; **D6 approved** in the owner's
wording; **D8 rewritten** as the G3c scope (two footprints, fixed shorelines); G3c plan specified before
any run (§8.3); the footprint attribution in the research record, 14, 99 and AGENTS.md corrected (§2,
research record §13); gaps recorded (§8.4, GEO-8). No measurement of 0.1 changed.
0.3 (2026-10-20): the owner's review of the G3c plan applied — one contour per footprint for the main
comparison, coastline variety moved to a later stage (G3d); the matrix cut to a calibration screen plus one
frozen configuration on an explicit, single seed list; tier rules proposed as diagnostics inside G3c
(confirmation pending); cliffs assessed as geometric steepness and walking connectivity, never as
"impassable"; the A/B result limited to the two specific contours, not to area alone (§8.3).
0.4 (2026-10-21): G3c ran under the owner's go-ahead; outcome in §8.3, results in research record §14.
0.5 (2026-10-21): the post-G3c review — status corrected (the run no longer awaits a go-ahead), D7 resolved
as a classification, the open parts of D6 annotated, GEO decisions pointed to
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §2. The plan of §8.3 and every measurement are
unchanged.

---

## 0. How to read this

**Tags.** **[MEASURED]** measured for this brief on 2026-10-20 (§10); **[PRIOR]** measured earlier, source
named; **[FACT]** read from code or a document; **[VISUAL]** an agent reading of images, owner review
pending; **[HYPOTHESIS]** believed, not demonstrated, with the check that would settle it; **[OWNER]** the
owner's stated direction or approval (2026-10-20); **[ASSUMPTION]** a working value the owner has not
approved; **[PROPOSAL]** a recommendation; **[PARAMETER]** an experimental variable, never a requirement.

**The reference image — source status.** The approved target is the owner's **concept image**
[`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)
(supplied 2026-10-20, stored outside `unity/Assets` so Unity does not import it). It is a **project
reference inspired by the visual direction of DragonSword: Awakening. It is not an official DragonSword
map, and nothing in it is information about the game's geography.** It communicates a composition, not
numbers: no plateau count, region size, mountain height, river length or settlement position may be read
from it as a requirement. Its scale bar (0–5 km) is illustrative — it would make the island several times
wider than the locked 2 800 m world — and is not a requirement. Version 0.1 of this brief was written
before the image was available; its readings rested on the owner's written description and the public
material of research record §4.1.

**Status of the terrain stack (unchanged by this brief) [FACT].**
- Production terrain is `H4A = H1 + U2 + R4A`; Stage 4A revision R1 is the production authority
  (`TerrainAuthorityField` → bake).
- G3 and G3b were **research prototypes** in an uncommitted Editor-only file; they are not production
  geography and approved nothing.
- The Stage 3F **E-family** prototype is **not in the repository** and has **not been approved**.
- Stage 3F's carve (`H3F = H4A − C3F`) is **experimental** and not on the production path; the separate
  hydrology carve exists in the bake but is **off by default**.
- **G3c is research only**; its results authorise no production implementation.

**Terms.** In this project a **Point of Interest** is the timed, lazily activated gameplay container of
[04](../../../04%20Points%20of%20Interest.md), and W.4 places its markers (`PoiKind`). The geographic
locations able to host them are called **POI sites** here, and a location able to host a W.5 settlement a
**settlement site**. Both are proposed working terms; they get Glossary entries only after approval (D9).

---

## 1. Executive conclusion

1. **The target is now approved (D6):** a regional geography with a dominant branching mountain system,
   several plateaus integrated into ridges and foothills, designed barriers and routes, deliberate valleys,
   lowlands and plains, rivers to designated outlets, caused coasts, room for 3–5 settlements and many POI
   sites — judged at island, regional and player scale.
2. **The mountain half is solved in principle.** A connected, branched system with two to three
   culminations fits the current island on all seven canonical seeds with the shoreline bit-identical (G3).
3. **The rest is not, and the measured reasons are the coastal transition and the construction.** Built
   mountains-first over today's coast-distance datum, the system leaves no drainage network, little flat
   land, **no capital-sized site on 6 of 7 seeds and no coastal settlement site on any surface measured**;
   carving a valley tree afterwards (G3b) adds a pass and tributaries but no lowlands. Between 60 and 200 m
   from the shore, 33–38 % of the land is a uniform ramp with a median slope of 11.5–15.8°, only 19–24 %
   buildable, that drains straight to the sea.
4. **The footprint is a hypothesis, not a finding.** No run has varied it. Even production `H4A`, with no
   mountain system, holds only a capital and two to three towns at 450 m spacing on the two seeds measured.
5. **G3c (D8)** tests the approved geography region-first on two specific contours — today's footprint
   (A) and about 40 % more land in the same 2.8 km world (B) — one contour per footprint for every seed, a
   coastal profile that varies by segment, a calibration screen on two seeds, then one frozen configuration
   on ten evaluation seeds (§8.3). It can say which of the two contours supports D6 better, not that area
   alone is the reason. Coastline variety is a later stage (G3d); a 3 × 3-tile world is a travel-pacing
   decision (D3).
6. **Acceptance:** Strahler order, basin area and the low-slope share stay **diagnostics**. G3c reports
   pre-registered readings against provisional values; nothing gates production.

---

## 2. What G3 and G3b established — verified

Re-verified for this brief **[MEASURED]**: the hashes of every lattice used here were recomputed
independently (FNV-1a as `WorldHeightField.Hash`) and match the pinned or recorded values — `H4A` 110201
`e2bad571f4af76c7`, 424242 `2076ecf78a34483b`; all seven `G3-skel-g200` (research §11.4) and all seven
`G3-net-g200` (§12.9). The slope reading used here reproduces G0's T-a where both exist (110201 `H4A`:
37.2 % below 5°).

| # | Finding | Evidence | Kind | What it is attributable to |
|---|---|---|---|---|
| 1 | One connected branched system fits every canonical seed | single dome 5/7 → 0/7; 2–3 culminations; 1–3 saddles ≥ 15 m; land > 110 m 0–18.7 % → 22.6–29.7 %; mask and ocean unchanged (§11.4) | measured | a skeleton from the island's own spine, not Stage 2A certification |
| 2 | Crests and drainage can agree | crest on flow divides 0.6–2.0 % → 30–49 %; on catchment boundaries 8–28 % vs a 1–4 % control (§11.4) | measured | ridges and valleys from one skeleton |
| 3 | A designed low pass works | lowest crossing of the range 179–247 m → 81–94 m, 18–20 m above the floor surface, all seven seeds (§12.4) | measured | built in by G3b's corridor rule (a design result, not an emergent one) |
| 4 | No drainage network either way | Strahler 2 on all seven; tributaries 0–3 → 2–6; largest basin ≤ 0.21 km² (§§11.6, 12.6) | measured | row 7 |
| 5 | Little flat land, lots of steep land | < 5°: `H4A` 25.4–54.1 % → G3 11.9–15.9 % → G3b 15.2–17.1 %; > 45°: 0–7.4 % → 9.6–21.1 % | measured | G3's own sizing (the system takes 30–38 % of the land) and the non-flat coastal ramp |
| 6 | No room for a capital or a harbour | §4.2: capital site (7.1 ha) on 0/7 G3 and 1/7 G3b seeds; town sites 0–5 per seed; **0 coastal town sites on every surface, `H4A` included** | measured | coastal ramp + mountain share; footprint not varied |
| 7 | Why drainage stays coastal | 54–59 % of the land lies within 200 m of the shore **[PRIOR]** (50–59 % on the five lattices measured here); there `H1` rises along the coast normal and the gate removes the mountain relief, so the land drains straight to the sea; a 100 m gate doubles tributaries (6–13) and reaches order 3 on one seed, at the cost of 116 shallow pits (§12.4) | measured + interpretation | the **coastal transition** (coast-distance datum `H1` + 200 m gate + the 7.2 m incision bound on troughs across the ramp) |
| 8 | Hygiene got worse in G3b | S8 0 → 12 on four seeds; 8 genuinely climbing axes (§12.4) | measured | construction (trough bound, crease tracking) |
| 9 | Artefacts | fan striations and facets (G3), terraced "amphitheatres" at branch roots (G3b) | visual | distance-to-polyline construction — the alternative [PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md) §16 already rejects for its radial artefacts |
| 10 | Appearance | **[VISUAL]** G3/G3b read as one smooth, massive wall-like range filling the island's interior, without a middle tier (foothills, plateaus) and without broad valleys or plains between the range and the sea; `H4A` reads as a low island with nested coast-parallel benches and, on four seeds, one mesa-like massif (`g3b_contact_sheet_V2.png`) | visual | hierarchy levels 1, 4 and 5 (research §4.2) were never built |

**Attribution corrected (2026-10-20).** Research §12.7, and the lines derived from it in 14, 99 and
AGENTS.md, said the limit was "the footprint and the coast gate, not valley topology". The evidence
supports the **coastal transition** (row 7, with the 100 m sensitivity) and the **construction** (rows 5,
8, 9). It does **not** isolate the **footprint** — no run varied it — and "not valley topology" holds only
for the one family tested, a tree routed on the coast-distance datum. Those statements are now corrected
in place, with the original wording kept visible; the full list is research record §13.2.

**Still unresolved:** whether lowlands, plateaus and coastal outlets can be designed rather than left over;
how many settlements the island holds with a mountain system on it; where POI sites concentrate; whether
walls can gate routes at all — the production jump climbs every tested slope up to 70° (3F-20, handoff
§21); whether regions read as distinct places. G3c addresses the first three.

---

## 3. Target geography requirements

The D6 target (§9) translated into assessable requirements. Status: **[OWNER]** approved in D6; **P**
proposed measure; **A** working assumption to approve. "View" means the Geometry-First Review camera set
(research §3.6) plus views anchored on the candidate's own structure (a pass, a plateau rim, a settlement
site, an outlet), because the Stage 1 anchor of V5 lands on slopes once mountains exist (§11.5).

### A. Regional structure and landforms

| Id | Requirement | How it is assessed | Status |
|---|---|---|---|
| A1 | Mountains are the dominant structure: a branching system with several peaks, connected ridges, spurs, passes and valleys; not one smooth dome, not a uniform mountain field | culminations and their height spread; branching (M4); M3 single-dome detector (diagnostic); share of land that is highland; V2/V3 | [OWNER]; measures P |
| A2 | Several large plateaus in different regions, integrated into ridges or foothills, not dominant everywhere | detected plateaus (flat top + rim drop), their regions, adjacency to highland or foothill | [OWNER]; detection thresholds P; count and size [PARAMETER] |
| A3 | Plateaus have readable tops and pronounced edges: cliffs, escarpments, steps | top relief; rim share that is escarpment; escarpment height | [OWNER]; heights tie to GEO-8 |
| A4 | Barriers and routes designed together: some cliff faces directly impassable; important regions have intentional access or meaningful bypasses | two separate readings: **geometric steepness** (slope classes and steep-run heights of each escarpment) and **walking connectivity** (T-edge walking barriers, ascent routes, bypass ratios); "impassable" is never asserted from either | [OWNER]; impassability in play depends on 3F-20 |
| A5 | Valleys, lowlands, plains and foothills are deliberate regions | landform-region map; region areas; buildable share per region | [OWNER] |
| A6 | Coherent spatial distribution, not features pasted onto a height field | region adjacency graph (plateaus beside ridges, lowlands beside outlets); review | [OWNER]; P |

### B. Settlement and exploration capacity

| Id | Requirement | How it is assessed | Status |
|---|---|---|---|
| B1 | Sites for 3–5 settlements in distinct settlement regions, one able to hold a major capital | settlement-site diagnostic (§4.2) | [OWNER]; size classes, slope, spacing A |
| B2 | Each settlement site sits in a setting: usable land around it, water or coast, a route out | per site: buildable hinterland, nearest river or coast, road-feasible link | [OWNER]; hinterland radius A |
| B3 | Numerous POI sites in varied settings, without making every area buildable | POI-site capacity per setting class and region (§4.4) | [OWNER]; pacing A; no quota |
| B4 | Routes between regions, with alternatives and challenges | region route graph; alternative routes; detour ratios; pass routes | [OWNER]; counts P |
| B5 | Intended accessible locations are reachable | T-edge walking connectivity from the shore (proposed 3F-18) | [OWNER]; deliberate exceptions need 3F-20 |

### C. Water and coast

| Id | Requirement | How it is assessed | Status |
|---|---|---|---|
| C1 | Rivers rise in elevated regions, follow valley corridors and reach designated coastal outlets | per system: source region, channel share in declared valleys, outlet designated or not | [OWNER]; P |
| C2 | Lakes where the structure holds them | lake-site candidates (enclosure + outlet on a river course); built only after GEO-5 | [OWNER]; policy GEO-5 |
| C3 | Plausible connectivity: no undeclared closed depressions; every system reaches the ocean | D-c continuity; D-d S8 = 0 outside declared basins | existing rules |
| C4 | Coastal forms follow the hinterland: beaches, coves, rocky shores, cliffs, headlands, offshore islets where appropriate | shoreline share per segment class; class consistent with hinterland; reported by distance band | [OWNER]; P |
| C5 | Coastal settlements and shore access are evaluated explicitly | coastal settlement sites; T-edge access from each low segment | [OWNER]; P |

### D. Visual and traversal quality at three scales

| Id | Requirement | How it is assessed | Status |
|---|---|---|---|
| D1 | Island overview: one coherent composition with distinct regions | V2/V3 and an island overview, owner review | [OWNER] |
| D2 | Aerial regional view: plateaus, cliff transitions, valleys and connections read | structure-anchored oblique views per region | [OWNER] |
| D3 | Player scale: traversal is plausible and landforms interesting | ground views at passes, rims, settlement sites; later a Play Mode walk (handoff §21 method) | [OWNER] |
| D4 | No uniform repetition, no construction artefacts (fans, facets, terraces) | review; escarpment and crest height variation | [OWNER] |

---

## 4. Settlement and POI capacity

### 4.1 Established and assumed

- **Established [OWNER]:** 3–5 settlements, possibly one major capital, in distinct settlement regions;
  numerous POI sites outside them; exploration between settlements; not every area must be buildable.
- **Existing V0 placeholder [FACT]:** `PoiDemoScenario` requests 5 `TownApproach` markers (one main hub) plus
  9 shrines, 9 camps and 9 landmarks, placed by a 3 × 3 world-sector map, not by geography. A demo quota,
  not a requirement.
- **Assumptions [ASSUMPTION], used provisionally by G3c (D2 still open):** settlement core radii — village
  60 m (1.1 ha), town 90 m (2.5 ha; the archived playable-town radius `TownGridBaker` 90 m), capital 150 m
  (7.1 ha); a cell is *buildable* at ≤ 6.84° (the road corridor `maxGrade` 0.12); a site is a disc with no
  sea cell and ≥ 90 % buildable cells; settlements ≥ 450 m apart (≈ 100 s at the controller's 4.5 m/s run
  speed). Terraced hillside towns, if the style includes them, relax the slope rule (D2).
- **Unsourced prior [PRIOR, weak]:** 99 Open Questions § Exploration says "~2.5–3 km² of dry land fits two
  large cities". It predates TerrainGen and names no measurement; this brief does not rely on it.

### 4.2 Measured on the current footprint **[MEASURED]**

Greedy placement (lowest mean slope first) — a lower bound on capacity, not an optimum. `H4A` lattices
exist for 110201 and 424242 only (both massif worlds).

| surface | seeds | buildable land % | largest buildable patch | capital site | town sites at 450 m | capital + towns at 450 m | sites at village size | coastal town sites |
|---|---|---|---|---|---|---|---|---|
| `H4A` | 110201, 424242 | 39.5–46.8 | 47–74 ha | 2/2 | 4–5 | 3–4 | 4–6 (with capital) | **0** |
| G3-skel | 7 | 18.0–23.3 | 19–28 ha | **0/7** | 0–3 | — | 1–5 | **0** |
| G3-net (G3b) | 7 | 22.9–26.7 | 20–29 ha | 1/7 | 1–5 | 1 (seed 7) | 2–6 | **0** |
| G3-net-g100 | 7 | 22.3–26.6 | 26–33 ha | 1/7 | 1–5 | 1 (seed 7) | 2–6 | **0** |

Spacing sensitivity on `H4A`: 7–9 town sites at 300 m, 3 at 600 m. G3b at ≥ 3 town-size sites: 2 of 7
seeds (20260905, 1337); at ≥ 3 village-size sites: 6 of 7.

**Where the buildable land is** (distance to the sea, five lattices) **[MEASURED]**:

| band | share of land | buildable | median slope |
|---|---|---|---|
| 0–60 m (shore strip) | 17–22 % | 44–48 % | 7.0–7.4° |
| 60–200 m (coastal ramp) | 33–38 % | 19–24 % | 11.5–15.8° |
| interior, `H4A` | 43–46 % | 49–65 % | 4.7–7.2° |
| interior, G3b | 41–50 % | 16–24 % | 19.5–33.9° |

Stage 4A's own design records the coastal floor band at 3–14° [FACT, Stage 4 design §4]; this is that band.

### 4.3 What limits settlement capacity

1. **The uniform coastal ramp** removes about a third of the land from settlement use and leaves the shore
   strip too narrow for a town: no coastal or harbour site on any surface (measured).
2. **The mountain system's share.** Where mountains stand, the interior's buildable share falls from
   49–65 % to 16–24 % (measured). G3 sized the system at 30–38 % of the land; that size is G3's choice.
3. **Compactness of the footprint.** Inscribed radius 410–781 m **[PRIOR]**; the island already spans
   2.06–2.22 km of the 2.24 km available inside the 280 m ocean margin **[MEASURED]**.
4. **Untested:** whether a design that reserves lowlands and plateaus recovers enough sites on the same
   footprint — the question G3c answers.

### 4.4 Method for POI-site capacity and density

1. **Setting classes with geographic preconditions** measured on the lattice — summit or viewpoint, pass,
   ridge spur, plateau top and plateau rim, escarpment foot and cave mouth, waterfall (channel over a
   step), valley floor and river bank, lake site, plain or meadow, headland and cliff coast, cove or beach,
   river mouth, offshore islet.
2. **Site capacity per class and per region:** sites meeting the preconditions, thinned by a class spacing.
3. **Density from pacing, not area:** the owner sets seconds of travel between discoveries along routes and
   the off-route share; capacity is sufficient when routes and regions can be filled from distinct sites
   and every region offers several classes.
4. **Refine with views and walks**, not by tuning terrain to a count.

Values to supply or approve (D2): pacing, spacing per class, minimum region size, classes per region.

---

## 5. Footprint — options

Facts **[FACT]**: world 2 800 m, `IslandOceanMarginMeters` 280 m, Stage 1 land fraction 0.32–0.40, so land
can reach at most 0.64 of the world (5.0 km²) without changing the world; Stage 1's golden hashes pin its
height field `H1`, not only the land mask. Travel **[MEASURED]**: island width 2.45–2.95 km, 9–11 min of
running or 5.4–6.5 of sprinting edge to edge in a straight line. Production's default world is seed
110201 (`OverworldTerrainMenu.DefaultSeed`), land 2.82 km².

| Aspect | A. Today's footprint | B. ≈ +40 % land in the same world (≈ 3.9–4.0 km²) | 3 × 3-tile world (out of G3c) |
|---|---|---|---|
| Mountains | fits one branched system (measured) | wider interior, taller crests possible | room for more than one system |
| Lowlands, plateaus | only if designed in and the ramp becomes usable (untested) | more interior room (hypothesis) | ample |
| Drainage | basins ≤ 0.21 km² so far, limited by the coastal transition | larger interior basins likely (hypothesis) | larger basins |
| 3–5 settlements | not met by G3/G3b; marginal on `H4A` | likely with designed lowlands (hypothesis) | yes |
| Travel | 9–11 min crossing | nearly unchanged — the island already fills the margin; it becomes fuller, not longer | ~15 min crossing |
| Coast | coastal band 50–59 % of land | coastal band expected to be a smaller share (bays fill, area grows faster than perimeter); fewer bays | long coast |
| Cost | none | Stage 1 hashes would move in production; same lattice, tiles, bake time | locked V0 defaults, 9 tiles, ~2.25× generation time (estimate) |

**Owner direction (D4, D8):** decide A against B by experiment (G3c); the 3 × 3 world is a separate
travel-scale decision (D3). **[HYPOTHESIS]** A suffices if designed lowlands and varied coasts recover
usable land and the mountain system stays near a quarter of the land; otherwise B is the smallest
credible footprint. G3c does not assume either.

---

## 6. Coastal direction

### 6.1 Desired behaviour **[OWNER]**, made concrete as relationships **[PROPOSAL]**

| What stands behind the shore | Coastal form | Why it matters for play |
|---|---|---|
| a lowland basin or plain draining to the sea | broad low-gradient shore, beach, lagoon, river mouth | harbour and coastal settlement sites, landing beaches |
| a valley between two ridges reaching the coast | cove or narrow inlet at the valley mouth | sheltered landing, cove POI sites |
| a ridge, plateau or upland meeting the sea | cliff, headland, steep coastal mountain, rocky shore | viewpoints, lighthouses, cliff paths, barriers and detours |
| foothills or a stepped plateau edge | terraced or stepped coast | stepped approach from the sea |

Rivers reach the sea only through outlets the region layout designates; steep coasts sit where high ground
meets the sea, never across a river mouth; each segment is caused by its hinterland (research §4.3 rule 6).

### 6.2 Avoiding the ramp as a drainage barrier

Today every shore gets the same profile: `H1` rises along the coast normal over ~200 m and the gate
removes inland relief there, so 50–59 % of the land drains straight to the sea and is too steep for towns
(§4.2). The coastal transition should follow the inland structure: lowlands stay low and slope toward their
outlet; high ground meets the sea steeply.

### 6.3 Architectural implications and risks

- **Ownership.** `H1` is Stage 1's golden-hashed height. A hinterland-driven coast either changes Stage 1's
  height model (mask bit-identical, hashes moved) or needs a later coastal stage that may *lower* land near
  the shore, which Stage 4A's non-negative `R4A` cannot do; Stage 4C as proposed (positive, mask-preserving)
  covers cliffs but not low outlets. Production half of GEO-4, open.
- **Shoreline displacement.** Lowering near the shore must keep every land cell above 24 m.
- **Artificial depressions.** A low plain behind a higher shore strip traps water (G3b's 100 m sensitivity:
  116 pits). Outlets are designed, not left to chance.
- **Hydrology contract.** The ocean mask floods from the world border through cells at or below sea level;
  near-sea-level plains need margin, also against the WaterWorks plane's visual.
- **Traversal.** Cliff coasts change shore access and the reachability promise; whether cliffs are barriers
  depends on 3F-20.
- **Downstream.** Roads, settlements, POI placement and the shore surface rule (sand) assume today's
  profile and must be re-measured.

---

## 7. Acceptance criteria

### 7.1 Existing metrics, reclassified **[PROPOSAL]** (D7 open)

| Metric | Today | Proposed class | Reason |
|---|---|---|---|
| Strahler ≥ 3 on the largest system (D-b) | proposed target | **diagnostic** | depends on the channel-initiation area as much as on geography; replaced by C1 |
| "basin area ≥ 30 000 m²" | in `HydrologySettings` this is `ChannelInitiationAreaSqm`, a channel threshold | **diagnostic parameter** | changing it changes the measurement, not the island |
| ≥ 35 % of land below 5° (T-a) | Draft target (`WORLD_RELIEF_PRINCIPLES.md` §6) | **diagnostic** | conflicts with any mountain identity on this footprint (measured); says nothing about where flat land is |
| 6–15 % of land above 45° | Draft target | **diagnostic** | cliffs belong to coasts, plateaus and ridges by design |
| ≥ 95 % reachable at ≤ 45° (on T-edge) | Draft target; reading proposed in 3F-18 | **candidate hard requirement** | the traversal promise; deliberate exceptions declared (3F-20) |
| S8 = 0 outside declared basins (D-d) | research requirement | **candidate hard requirement** | hygiene |
| D-c continuity; every system reaches the ocean | existing hydrology rule | **hard (existing)** | subject to GEO-5 |
| Land mask unchanged | research control | **control in experiments** | production status follows GEO-4 |
| M1, M2, M4, M6 | proposed | **diagnostics** | M2 sees highlands only |
| M3 single-dome detector | proposed | **diagnostic** | its rule was calibrated after seeing data |
| M5 passes; D-a; D-e | proposed | **geography proxies** | meaningful on the candidate's own structure |
| Determinism, pinned `H4A` hashes | process | **hard (process)** | — |

The owner's direction for G3c (2026-10-20): Strahler order, basin area and the low-slope share are
reported as diagnostics; no criterion gates production.

### 7.2 Evaluation framework

New measures sit **beside** G0's, never replace its definitions. Values marked *provisional* are proposals.

| # | Question | Measures | Method |
|---|---|---|---|
| 1 | Mountain structure and variety | culminations and height spread; branching; single-dome detector; **landform-region map** (highland, plateau, foothill or upland, lowland plain, valley floor, coastal lowland, cliff zone) with area shares | lattice + V2/V3 |
| 2 | Ridge, divide, valley coherence | crest on divides and catchment boundaries vs control; valleys between ridges; no climbing valley axis | lattice |
| 3 | Plateaus and escarpments | plateau count, top area and relief, rim escarpment share and height, region, adjacency to ridge or foothill; escarpment length by height class; height variation | lattice |
| 4 | Drainage | systems with an elevated source; channel share in declared valleys; designated outlet reached; basin-area distribution; D-c; S8 | lattice (hydrology package read-only) |
| 5 | Settlement terrain and capacity | §4.2 diagnostic: buildable share by region and by distance band, largest patch, sites by class at the spacing, coastal sites, sites in distinct regions | lattice |
| 6 | POI sites | sites per setting class and region (§4.4) | lattice + review |
| 7 | Traversability | T-edge reachability from the shore of every settlement site, plateau top, pass and region | served-surface reading (handoff §20 method) |
| 8 | Routes, barriers, challenge | ascent route per plateau and pass (length, gain, max slope); bypass ratio at escarpments; **walking-barrier** length (both sides walkable, no walkable crossing); escarpment **steepness classes** (45–60°, 60–70°, > 70°) and steep-run heights, kept apart from connectivity; road-feasible links between settlement sites under the Road Passability contract; alternative routes | T-edge graph; served-surface slopes; `LandscapeCostField` dry run |
| 9 | Coast | shoreline share per segment class; class consistent with hinterland; every outlet on a low segment; access from the shore per class | lattice, by distance band |
| 10 | Readability | island overview, regional obliques, ground views at structure-anchored points; owner review | G1 where applicable + research views |
| 11 | Robustness | readings on calibration and held-out seeds separately; determinism repeat | process |

**Process rules:** readings and comparison rules are written before a run (§8.3); construction iterations
may fix named defects only and are logged; calibration uses two seeds; no metric definition or threshold
changes to make a candidate pass; failures are recorded with their cause.

---

## 8. Architecture comparison, the G3c plan and the gaps

### 8.1 The three GEO-3 directions, read against G3 and G3b

| | (1) Revise Stage 2B into a branched system | (2) A separate positive-relief stage | (3) A shared geographic skeleton |
|---|---|---|---|
| Benefits | smallest change to an existing authority | frozen stages stay bit-identical; highland on massif-free worlds | ridges, divides, valleys, passes coherent by construction; reaches every seed (G3) |
| Costs | `GeneratorVersion` bump, 29-seed re-audit, every Stage 3 input moves | adds a stage | supersedes massif-derived 2B/3A–3E geometry; if it owns the coast, also Stage 1's `H1` or a lowering coastal stage |
| Drainage and coast | keeps the coast-distance datum and gate | ridges without dual valleys (Stage 3 design §3.1) — G3-skel is this case | can design outlets and coast together, if region-first |
| Settlements, plateaus | mountains take flat land, create no lowland or plateau | measured on G3-skel: no capital site on 7/7 | only if regions are designed in; carving afterwards (G3b) did not create them |
| Risks | Stage 2A certification decides where mountains exist (none on 4 of 29 audited worlds) | becomes (3) once valleys are added | scope; distance-to-polyline artefacts and flow-derived structure, both rejected in Stage 3 design §16 |
| Evidence before production | a candidate passing §7 on massif-free seeds | same | G3c passing on held-out seeds; E-family acceptance on the new relief (G5); views and walks; an integration design (G4) |

**[PROPOSAL]** investigate (3), region-first. GEO-3 (placement) stays open until G4.

### 8.2 Reading of the concept image — composition, not specification **[VISUAL]**

The image places the main mountain system **toward one end of the island** (its north), not along the
middle; a central valley with rivers and lakes holds the capital; plateaus stand on the west and east flanks
and near the high mountains, one carrying a town; foothills and plains lie between the valley and the
south-west coast; rivers run from the mountains through the valley to the sea; cliffs and headlands front
the high ground, beaches and lagoons the plains; a port town sits on a low coast; small islands lie
offshore. G3 put its spine on the island's medial axis, which is one reason it filled the interior. The
G3c plan therefore treats **highland placement** as a parameter (medial or off-centre), not as a template.

### 8.3 G3c — region-first geography on two footprints (plan as authorised; run 2026-10-20 … 21)

**Outcome** (research record §14): the plan below ran as written. Neither contour passed the comparison rule
— A met every reading on 2 of 10 evaluation seeds, B on none — and B did not materially improve on A (more
buildable lowland, worse hygiene), so the world is not grown on this evidence. Settlement sites with a coastal
one, plateaus and a varied coast held on all twenty lattices; the failures trace to construction defects,
two definition weaknesses (R3, R5) and the hydrology planner's own validation (§14.7). Next: the owner's
review (§14.11).

**Hypothesis.** A geography designed region-first — a dominant branching highland system, several plateaus
integrated into ridges and foothills, deliberate lowland and foothill regions each draining to a designated
coastal outlet, and a coastal profile set per shore segment by its hinterland — produces the D6 geography
on a fixed shoreline. **Open question, not assumed:** whether contour B produces it materially better
than contour A.

**What G3c tests, and what it does not** (owner review, 2026-10-20).
- **Tests:** whether the region-first construction produces the D6 interior geography on a fixed contour,
  across interior layouts (the seeds), and which of two specific contours, A or B, supports it better.
- **Does not test:** robustness to different coastline shapes — every seed of a footprint uses the same
  contour, a deliberate control for the interior comparison. Coastline variety is a **separate later stage,
  G3d** (§8.4.6), not part of G3c.
- **Attribution limit:** contour B differs from A in shape as well as area (fuller outline, fewer bays,
  larger inscribed radius, a smaller coastal-band share are expected). G3c can show which of the two
  contours is better; it **cannot prove that the difference comes from area alone**. Separating area from
  shape needs several contours per footprint size — part of G3d.

**Fixed conditions (both footprints).**
- World 2 800 m; lattice 1025² at 2.734375 m; sea level 24 m; height budget 700 m; ocean margin 280 m.
- **One predefined shoreline per footprint, identical for every seed of that footprint** (owner direction,
  confirmed in the plan review: one contour per footprint for the main comparison); land mask and ocean
  bit-identical across a footprint's seeds and configurations; nothing seaward of the shore changes. Seeds
  drive only the research layout streams (`research.g3c.*`); the seed that produced a contour and the seeds
  that drive interiors are separate roles.
  - **A** — production's default world: the Stage 1 coastline of seed 110201 (2.82 km² of land; the land
    mask of the production `H4A` lattice, hash `e2bad571f4af76c7`).
  - **B** — a research-only Stage 1 run of seed 110201 with the land-fraction band raised to ≈ 0.50, target
    3.9–4.0 km² (+40 %). Generated once, checked, frozen as a fixture outside `Assets`. Accepted if one
    landmass with at most the existing islet budget lies inside the ocean margin with 3.8–4.1 km² of land;
    otherwise the pre-registered fallbacks in order: band [0.48, 0.52] on 110201, then 424242, then
    20260905. The shape statistics of A and B (land, inscribed radius, bounding box, crenulation, coastal
    band share) are reported side by side, because shape differs as well as area. B is prepared and
    frozen **before** any interior run (step 0 below), so no construction result can influence its choice.
- The same construction code, the same rule values in world units, and the same region budgets **as
  shares of land** on A and B; absolute sizes reported beside them.
- Production untouched: `H4A` rebuilt and its pinned hash checked in every process; no production file,
  bake, scene, tile asset, frozen stage or pinned hash changes; G0 and G1 unchanged; outputs outside
  `Assets`; nothing committed without the owner.

**Seeds — one list, 12 seeds, two roles.** With a fixed contour a seed only drives the interior layout, so
the old labels "canonical" and "sweep" describe where a number came from, not a different island; a seed's
own Stage 1 island (narrow, massif-free, …) plays no part in G3c.

| Role | Seeds | Used for | Rule |
|---|---|---|---|
| calibration (2) | **110201, 1337** | building the construction, fixing named defects, the configuration screen | the only seeds generated, measured or viewed before the freeze |
| evaluation (10) | **424242, 20260905, 7, 256, 777, 1000, 1001, 1002, 1003, 1004** | the A/B comparison with the frozen construction and configuration | not generated, measured or viewed before the freeze record exists; any later change to the construction voids the evaluation, which is then rerun on ten fresh seeds (**1005–1014**), never on these |

1000–1004 are the first five of the existing G0 sweep range; no G3-family construction has used them.

**Construction method (in principle).**
1. **Region layout** from the fixed land mask: a connected highland region (main divide, culminations,
   branches) sized by P2 and placed by P4; two or more lowland or plain regions in the largest remaining
   compact areas, each assigned a designated outlet on a shore segment reachable without crossing highland,
   at least one of them touching the coast; plateau sites in different regions on upland or foothill
   ground adjoining a ridge or spur; foothill and upland bands between highland and lowland.
2. **Region datum** in place of the coast-distance datum inside the land (research only): lowland floors
   graded gently toward their outlet; upland and plateau levels; the highland envelope above.
3. **Highland and valleys as one structure:** main divide with branches and spurs; tributary valleys
   between spurs by a strip partition of each flank (Stage 3 design §3.1), not distance to a polyline;
   at least one pass on the main divide linking two lowland regions; trunk valleys from the highland
   through each lowland to its outlet; monotone floors widening into plains. D8 flow is the **check**,
   never the generator.
4. **Plateaus:** flat top with low relief; escarpment (wall plus talus) on part of the rim; the wall does
   not run all the way round, and at least one access route — ramp on the uneven flank, link to a ridge or
   a winding ascent — reaches the top (`WORLD_RELIEF_PRINCIPLES.md` §2); the rest of the rim is built as
   a wall meant to block walking. Escarpment heights are drawn per plateau from two classes with equal
   probability — **18–40 m** (the T2 row) and **60–120 m** (above it) — so every evaluation run carries
   evidence for GEO-8 without a separate sensitivity matrix.
5. **Coastal profile per segment**, classified by hinterland: outlet → low gradient (beach, lagoon, river
   mouth); valley mouth between ridges → cove; highland or plateau within a distance of the shore set in
   step 1 and fixed in the freeze record → steep or cliff profile; otherwise a gentle default. Land cells stay above 24 m; nothing seaward.
6. **Hygiene:** no undeclared closed depression, no climbing valley axis, height budget respected.
7. **Lakes:** not built; lake-site candidates reported (GEO-5 open).

**Parameters [PARAMETER] (levels fixed before the run).** Only one of them is the question G3c answers.

| Id | Parameter | Levels | Role |
|---|---|---|---|
| P1 | footprint contour | A, B | **the comparison** — run on every evaluation seed |
| P2 | highland share of land | 0.20, 0.30 (G3 was 0.30–0.38) | composition choice — settled on calibration seeds only |
| P4 | highland placement | medial (as G3), off-centre toward one end | composition choice (§8.2) — settled on calibration seeds only |
| — (P3 of v0.2, folded in) | plateau count, top size, escarpment class | 2–4 plateaus per island; tops 0.03–0.15 km² on A, scaled by land share on B; escarpment 18–40 m or 60–120 m with equal probability | drawn per seed and per plateau, never a target; reported per class |

**Why not the full matrix.** P2 and P4 are composition choices, not the scale question; crossing them with
P1 on all twelve seeds (8 × 12 lattices, plus a separate escarpment sensitivity) would multiply runs
without answering anything G3c is for. They are settled once, on calibration seeds only, and the
escarpment classes ride along inside every run instead of forming their own matrix.

**Steps.**

| Step | What | Seeds | Lattices |
|---|---|---|---|
| 0 | prepare contour B (Stage 1, research only), accept or fall back by the rule above, freeze it | — | — |
| 1 | build the construction; fix named defects only, each logged | calibration | as needed |
| 2 | configuration screen: P2 × P4 on both contours | calibration | 4 × 2 × 2 = 16 |
| 3 | **freeze record**: MD5 of the research code, the chosen configuration, contour fixtures and every rule value, written before step 4 | — | — |
| 4 | evaluation: frozen construction and configuration, contours A and B | evaluation | 2 × 10 = 20 |
| 5 | determinism: step 4 repeated in a fresh process, byte-identical lattices and reports | evaluation | 20 |

**Configuration choice in step 2 (pre-registered).** For each of the four P2 × P4 configurations, count the
readings R1–R8 met over the four calibration cases (2 seeds × 2 contours). Choose the configuration with the
highest **lower** of its two contour counts (so the choice favours neither contour); ties go to the higher
total, then to share 0.30 (closer to "mountains dominant"), then to off-centre. The other three
configurations are reported, never used again. Two seeds give a weak basis — this step chooses a
construction, it proves nothing.

**Tier rules inside G3c — proposed: diagnostic, not binding (to confirm before the run, §9).** The
`WORLD_RELIEF_PRINCIPLES.md` §1 tier table and its 2.5× rule are evaluated and reported for every plateau and
stacked form — height class, footprint, which tier row it fits or falls between — but do not constrain the
construction. Binding them would pre-limit the experiment to the architecture GEO-8 questions; reporting them
gives GEO-8 its evidence. They stay binding for production until GEO-8 is answered.

**Metrics.** §7.2 items 1–11, every one reported per seed and, where it concerns land use, per distance band
(0–60 m, 60–200 m, interior) so the coastal-ramp problem cannot hide in island-wide aggregates; Strahler
order, basin areas and the low-slope share as diagnostics; S8 and climbing axes as hygiene; tier-rule
conformance as a diagnostic.

**Cliffs: two readings, no verdict on impassability** (owner review, 2026-10-20). G3c reports, separately:
- **Geometric steepness** — per escarpment, served-surface slope classes (45–60°, 60–70°, > 70°) and the
  height of continuous steep runs. Walls steeper than 70° lie **outside the range the production jump was
  tested on**; they are reported as such, not as impassable.
- **Walking connectivity** — the T-edge graph (< 45°): walking barriers, ascent routes, bypass ratios,
  reachability. It is a conservative lower bound — the jump is not modelled.

Whether a face is impassable in play is not claimed until 3F-20 is answered and a Play Mode test exists.

**Readings written before the run (provisional values, not gates).** On a seed, a candidate *meets D6* when:

| # | Reading |
|---|---|
| R1 | ≥ 2 separate culminations, branching, ≥ 1 pass on the main divide; single dome not flagged |
| R2 | ≥ 2 plateaus in different regions, each adjoining highland or foothill, each with an escarpment segment (≥ 45° over a continuous steep run) and a walking access route on T-edge |
| R3 | ≥ 2 lowland or foothill regions with buildable land |
| R4 | a capital site plus ≥ 2 further settlement sites at 450 m, ≥ 1 of them coastal, in ≥ 3 regions (§4.1 assumptions) |
| R5 | every river system with an elevated source reaches a designated outlet |
| R6 | ≥ 3 coastal segment classes, each consistent with its hinterland |
| R7 | 0 S8 outside declared basins; 0 genuinely climbing valley axes |
| R8 | every settlement site, plateau top and pass reachable on T-edge from the shore |

**Comparison rule A versus B (pre-registered).** The frozen configuration *passes* on a contour when R1–R8
hold on ≥ 7 of the 10 evaluation seeds and the owner's visual review accepts it. **B materially improves**
on A when it passes where A does not, or — both passing or both failing — when it adds ≥ 1 settlement site
at town size or ≥ 25 % buildable lowland on ≥ 7 of 10 seeds without worse hygiene or review. Decision: A
passes → contour A is viable (the smallest footprint tested); A fails and B passes → contour B is
recommended; both fail → the concept or the coastal transition is wrong, and the world is not grown on that
evidence. Every conclusion is stated about **contour A and contour B**, not about land area as such (the
attribution limit above); the shape statistics are reported beside it.

**Visual assessment.** For each contour and evaluation seed (the frozen configuration; the step 2 screen gets
the same set on its calibration cases): an island overview, V2/V3, and
regional obliques of each plateau rim, the main pass and one outlet, plus ground views at a settlement site
and a pass — identical camera conventions (field of view, resolution, sun, neutral material, anchor rules
relative to the structure) for A and B. G1 is used where its Stage 1 anchors apply (footprint A, seed
110201's coastline); footprint B needs research-side views because G1 anchors on a seed's production
Stage 1 (gap 8.4.4). Each region that works and each failure is recorded with its image id; measurements
and visual judgments are reported separately.

**Cost [HYPOTHESIS].** Construction effort above G3 plus G3b (regions, plateaus, coastal segments, T-edge
routes). Runtime estimated at 15–45 minutes for the 56 lattices of steps 2, 4 and 5 plus views — G3 needed
12.5 minutes for 14 lattices, most of it G1 rebuilding each world; T-edge routing is unmeasured at this
scale.

**Not in scope.** Coastline variety and the area-versus-shape question (G3d); a 3 × 3-tile world; lakes
(GEO-5); changing the controller or the jump (3F-20); Stage 3F integration; production wiring or any
production Stage 1 change; an archipelago beyond Stage 1's islet budget; the placement decision (G4).

### 8.4 Gaps the approved target exposes (research questions, nothing implemented)

1. **Plateau tiers (GEO-8, new).** `WORLD_RELIEF_PRINCIPLES.md` §1 fixes T2 plateaus at 18–40 m above
   local ground and 150–450 m across, and forbids forms between tiers (2.5× rule). D6's "large plateaus"
   with pronounced cliffs may exceed that row. Question: does the T2 row change, or do large plateaus belong
   to another level (relief regions)? G3c reports tier conformance for plateaus of both escarpment classes
   (proposed as a diagnostic inside G3c); the rule stays in force for production.
2. **Impassable cliffs and the controller.** The production jump climbs every tested slope up to 70°;
   lattice walls reach 74–82° and are untested. D6 allows impassable faces; G3c reports steepness and
   walking connectivity separately and never asserts impassability; whether any face is impassable in play
   is 3F-20, then a Play Mode test.
3. **Coastal-profile ownership.** `H1` is golden-hashed Stage 1 height (§6.3) — production half of GEO-4.
4. **Review tooling for a non-production coastline.** G1 anchors its cameras on a seed's production
   Stage 1, so it cannot review footprint B as built; a G1 extension would change approved tooling
   (GEO-6 scope) and needs the owner. G3c uses research-side views with G1's conventions.
5. **G0 definitions tied to production structure** (M3 (a)/(b), D-a, D-e) do not read G3c; its own
   measures sit beside G0, which stays unchanged.
6. **Coastline variety — a separate later stage, G3d** (owner direction, 2026-10-20). G3c keeps one contour
   per footprint as a control. If G3c's construction works, G3d re-runs it frozen on several contours —
   each seed's own Stage 1 coastline, and several contours per footprint size — which tests robustness to
   coastline shape (the narrow islands of G3) and separates the effect of area from the effect of shape
   that G3c cannot. Not planned in detail and not authorised.
7. **Offshore archipelago.** Stage 1 allows at most 2 islets (`MaxIslets`); the image shows more.
   Outside G3c; a Stage 1 question if the owner wants it.
8. **Scale bar of the image** (0–5 km) is illustrative; the world stays 2.8 km (§0).

---

## 9. Owner decisions

| # | Decision | Status |
|---|---|---|
| D1 | Supply the reference image | **done 2026-10-20** — `References/G2_target_island_concept_2026-10-20.webp` |
| D2 | Working assumptions for settlements and POI sites (§4.1, §4.4); whether terraced hillside towns belong to the style | **open**; G3c uses them provisionally |
| D3 | Travel pacing — the only ground for a 3 × 3-tile world | **open**; excluded from G3c |
| D4 | GEO-1 path | **owner direction 2026-10-20:** decide A against B (+40 % in the same world) by G3c; final footprint open. **Resolved 2026-10-21** (owner direction): G3c gives no reason to resize; today's footprint is the research baseline; revisit trigger in post-G3c review §2.1 |
| D5 | GEO-4 for G3c | **owner direction 2026-10-20:** shoreline fixed within each footprint; coastal profile varies by segment; production coastal contract open |
| D6 | Target geography | **approved 2026-10-20** — wording below |
| D7 | GEO-7: adopt §7.1 and §7.2 as the evaluation basis | owner direction: Strahler order, basin area and low-slope share are diagnostics in G3c. **Classification resolved 2026-10-21** (owner direction): hard gates with direct gameplay or topological meaning, the owner's visual review at three scales, diagnostics, provisional assumptions — post-G3c review §2.6; the reading set for the next experiment is proposed there (§3), and §7.1's rows stay as the G3c-era proposal |
| D8 | G3c authorisation and scope | **scope set 2026-10-20 and plan reviewed by the owner** (five points, applied in v0.3) — wording below. **Go-ahead given by the owner on 2026-10-20**, confirming every item of the pre-run checklist below, with the tier rules **diagnostic** inside G3c. **Ran 2026-10-20 … 21**: results in research record §14 |
| D9 | Documentation and housekeeping | pointers and attribution corrections **done in v0.2**; open: Glossary entries for *POI site* and *settlement site*; whether the uncommitted G3/G3b research file stays; whether this documentation is committed (AGENTS.md asks for a commit per step; the G-tasks forbade it) |

Also open and not resolved here: GEO-2's tier-table wording (D6 sets the direction), GEO-3 placement (G4),
GEO-5 lake policy, GEO-8 plateau tiers, 3F-20 jump and barriers. **Since 2026-10-21** (post-G3c review §2):
GEO-2 resolved as (a); GEO-5's policy resolved as (b) with declared basins; GEO-8's principle resolved and
its contract proposed; GEO-3's production placement and 3F-20 still open.

**D6 — Target geography. Approved by the owner, 2026-10-20.**

> The overworld island is a **regional geography with a landform grammar**, not the product of one
> mountain-generation technique. Source: the owner's concept image
> (`References/G2_target_island_concept_2026-10-20.webp`), a project reference inspired by the visual
> direction of DragonSword: Awakening — not an official map of that game.
>
> 1. **Mountains are the dominant structural element**: a branching mountain system with multiple peaks,
>    connected ridges, spurs, passes and valleys. The island is neither one smooth central dome nor a
>    uniform field of mountains.
> 2. **Several large plateaus** appear in different regions, integrated into ridges or foothills. They are
>    not the dominant form everywhere.
> 3. **Plateaus have readable upper surfaces and pronounced edges** — cliffs, escarpments, stepped terrain.
> 4. **Vertical barriers and traversal routes are designed together.** Directly impassable cliff faces are
>    allowed; important regions have intentional access routes — passes, winding ascents, gentler slopes —
>    or meaningful bypasses through neighbouring terrain.
> 5. **Valleys, lowlands, plains and foothills are deliberately formed regions** in their own right, not
>    space left over between mountains.
> 6. **Water and coast follow the regional structure:** rivers rise in elevated regions and follow valley
>    corridors to designated coastal outlets; lakes sit where the structure holds them; coastal forms —
>    beaches, coves, rocky shores, cliffs, headlands and small offshore islands where appropriate — are
>    caused by the terrain behind them.
> 7. **The world supports 3–5 settlements and numerous POIs** while keeping visually strong, varied
>    terrain; not every area has to be suitable for construction.
> 8. **Success is judged at three scales:** island overview, aerial regional view and player-level
>    traversal.
>
> The image is a composition, not a template: plateau counts, region sizes, mountain heights, river
> lengths and settlement positions are parameters to investigate, never inferred requirements.
>
> | Confirmed (owner) | Provisional (to approve) | Open (owner) |
> |---|---|---|
> | points 1–8 above; the image as the conceptual target | settlement size classes, slope and spacing (D2); detection thresholds and readings (§§7–8, D7) | footprint (GEO-1, after G3c); tier wording (GEO-2, GEO-8); placement (GEO-3); production coastal contract (GEO-4); lakes (GEO-5); barriers in play (3F-20); travel pacing (D3) |

*Status of the "Open" column since 2026-10-21 (outside the approved wording; post-G3c review §2):* footprint
— resolved for now, no resizing on G3c's evidence, revisit trigger recorded (GEO-1); T1 wording — resolved
(GEO-2 (a)); plateau tiers — principle resolved, contract proposed (GEO-8); placement — research under (c),
production at G4 (GEO-3); coastal contract — target direction resolved, contract open (GEO-4); lakes —
policy resolved, realisation research (GEO-5); barriers in play and travel pacing — open.

**D8 — G3c authorisation and scope. Scope set by the owner, 2026-10-20; go-ahead given the same day; ran
2026-10-20 … 21** (research record §14).

> G3c is **research only** and authorises no production implementation. It tests whether the D6 geography
> can be generated on two land footprints: **A — today's footprint** (the baseline) and **B — about 40 %
> more land within the same 2.8 km world**. A 3 × 3-tile world is excluded; it is a separate travel-scale
> and world-design decision. Within each footprint the **shoreline outline stays fixed across all seeds**;
> the two footprints may use different predefined contours. The **coastal elevation profile varies by
> shoreline segment** with the adjacent hinterland; no uniform coastal ramp is imposed. Both footprints aim
> at the same D6 geography, and the comparison asks whether B **materially** improves the ability to create
> it and to provide gameplay space. **The footprint is not assumed to be the limiting factor**: G3/G3b
> identified construction and coastal-transition problems and did not test land area. The method,
> parameters, fixed conditions, metrics and comparison rule are fixed before any run (§8.3) and are not
> tuned to make a candidate pass.
>
> *Plan review, 2026-10-20 (owner):* one contour per footprint is the control for the main comparison, and
> coastline variety is a separate later stage; calibration never uses evaluation seeds, and the seed list
> is explicit; cliffs are judged by geometric steepness and route connectivity, without asserting
> impassability before 3F-20; contour B differs in shape as well as area, so G3c compares two specific
> contours and does not prove that area alone causes a difference.

**Pre-run checklist for the go-ahead (D8).** Running G3c needs the owner to confirm:

1. **Contours:** A = the Stage 1 coastline of seed 110201; B = seed 110201's Stage 1 at land fraction ≈ 0.50
   with the stated acceptance and fallbacks, prepared and frozen before any interior run.
2. **Seeds:** calibration 110201, 1337; evaluation 424242, 20260905, 7, 256, 777, 1000–1004; replacement
   evaluation seeds 1005–1014 if the construction ever changes after the freeze.
3. **Matrix:** P2 × P4 screened on calibration seeds only (16 lattices); one frozen configuration on the
   evaluation seeds for A and B (20 lattices) plus a determinism repeat (20).
4. **Tier rules inside G3c:** diagnostic, not binding (proposed in §8.3) — or binding, if the owner prefers
   to keep the experiment within today's tier table.
5. **Readings R1–R8 and the comparison rule** of §8.3, with their provisional values.

---

## 10. Evidence references

| Item | Location |
|---|---|
| Approved concept image | [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp) (MD5 `8244aba229066099ab06cac398c94563`, copied from the G2 task attachment) |
| Production audit, scale budget, public reference reading | research record §§1.2–1.4, 4.1, 4.5 |
| Proposed hierarchy and grammar | research record §§4.2–4.3; [90 Glossary](../../../90%20Glossary.md) § Geographic Hierarchy |
| G0 baseline | research record §7.6 |
| G3 | research record §11 (tables §11.4; visual §11.5; attribution §11.6, corrected §13.2) |
| G3b | research record §12 (tables §12.4; verdict §12.7, corrected §13.2) |
| Corrections and G2 outcome | research record §13 |
| G3 outputs | session scratchpad `672254d5…/scratchpad/g3/final/`: `g3_report.txt`, `table.md`, `g3_contact_sheet_V2_V3.png`, lattices `g3_<seed>_G3-skel-g200.f32`, `review/` |
| G3b outputs | session scratchpad `a8183de8…/scratchpad/g3b/final/` and `…/sens/`: `g3_report.txt`, `tables.txt`, `g3b_contact_sheet_V2.png`, lattices `g3_<seed>_G3-net-g200.f32`, `…G3-net-g100….f32` |
| `H4A` lattices used in §4.2 | session scratchpad `8953f7ab…/scratchpad/k8/data/lattice_{110201,424242}_H4A.bin` (written by the Stage 3F research; hashes verified here) |
| G2 diagnostics | session scratchpad `672254d5…/scratchpad/g2/`: `settlement_capacity.py` and `.txt`, `coastal_band.py` and `.txt`, `lattice_hash.py`, `extent.py` |
| Code facts | `WorldScale` (world 2 800 m, `IslandOceanMarginMeters` 280, road `maxGrade` 0.12); `TerrainGenSettings` (land fraction 0.32–0.40, `MaxIslets` 2); `OverworldTerrainMenu.DefaultSeed` 110201; `OverworldPlayerController` (run 4.5 m/s, sprint 7.5 m/s); `PoiDemoScenario`, `WorldSectorMap`; `TownGridBaker.DefaultPlayableRadiusMetres` 90; `TerrainGenStage2ATests.Stage1HeightField_MatchesItsRecordedHashes` |
| Coastal floor band, lowland reservation | [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md) §§4–5 |
| Ridges and valleys as duals; rejected constructions | [PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md) §§3.1, 16 |
| Walking limit and jump | [PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3F_DRAINAGE_HANDOFF.md) §§20–22 |
| Hydrology rules | [HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md) §1 (every system reaches the ocean), §§9, 11 |
| Relief rules and Draft targets | [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md) §§1–6 |
| Road and settlement pipeline | [WORLD_GENERATION_PIPELINE.md](WORLD_GENERATION_PIPELINE.md) §§1–3; [LANDSCAPE_PLANNER.md](LANDSCAPE_PLANNER.md) |

The scratchpad folders are temporary session storage, not part of the repository; if this evidence must
outlive the sessions, archive it before they are cleaned.
