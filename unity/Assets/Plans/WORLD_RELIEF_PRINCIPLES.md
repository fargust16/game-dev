# World Relief Principles

What makes generated terrain read as a place rather than as noise, and the rules we hold ourselves
to. Sources are listed at the bottom; every rule here is either taken from one of them or is the
numeric form we chose for it.

This document is **why**. The *how* — the terrain generator's stages and the Unity terrain bake —
lives in [PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md) and
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md). When they disagree about a
number, this file states the intent and those documents state what the pipeline actually delivers.

---

## 0. The shortest version

Relief is not decoration. It is **structure first, detail last**, and every shape has to be
explainable as the result of a process — uplift, then water taking material away, then debris
settling at the bottom. A shape with no story reads as noise no matter how detailed it is.

---

## 1. Scale hierarchy: three tiers, and nothing in between

The single most transferable rule from open-world level design. Deploy **three distinct elevation
scales** — distant landmarks, mid-size climbable forms, and small reference features — and keep
them visibly separate: *"clear differences and contrasts between each scale level are important to
maintain; you don't want muddy shapes."* Small forms next to large ones make the large ones read
bigger, which is free scale for nothing.

**Our numeric form of the rule:**

| Tier | Role | Height above local ground | Footprint | Count per island |
|---|---|---|---|---|
| **T1 Massif** | The landmark. Visible from everywhere, orients the player. | 250–530 m | 1000–2000 m | **1** (2 max) |
| **T2 Plateau** | Climbable shelf. Districts, view points, settlement ground. | 18–40 m | 150–450 m | **6–10** |
| **T3 Outcrop** | Reference detail. Cover, silhouette, "that rock". | 5–12 m | 40–90 m | **12–20** |

~~**Under review 2026-10-18 — the T1 count.**~~ **Resolved 2026-10-21 — the T1 count (GEO-2 (a), owner
direction from the approved target D6).** T1 names the island's **mountain system**: one connected,
branching system with a main divide, several culminations, branch ridges, spurs and passes counts as **one
T1**. Its culminations and ridges are parts of that one form, not separate T1s; the 2.5× separation rule is
a rule between tiers and is not applied among them. The row is unchanged: count 1 (2 max) — D6's target is
one dominant system — and its heights and footprint are a provisional Draft classification, not a production
gate and not re-measured against D6 ([99 Open Questions](../../../99%20Open%20Questions.md) GEO-2;
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §2.2). Research note: G3c's systems topped out at
253–308 m above sea level, near or below the 250 m floor in relief above local ground; whether D6's
dominant mountains need more of the 700 m budget is measured on the next model, not settled by this row.

**Plateaus in the approved target — principle resolved 2026-10-21, contract proposed (GEO-8).** The owner
approved the target geography D6 ([GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) §9): one
dominant branching mountain system and several large plateaus integrated into ridges and foothills, with
readable tops and cliff or escarpment edges; on 2026-10-21 the owner added that traversal is mixed. Such
plateaus may exceed the T2 row (18–40 m) and fall between T2 and T1. **Resolved (owner direction):** a
geography candidate is not rejected solely for that, and a tier label never decides traversal — whether a
face blocks the player is read from steepness and walking connectivity and decided under 3F-20.
**Proposed, not decided:** keep the T2 row for district-scale shelves and treat a large plateau as a
region-level form outside this table, separated from T1 by form and subordinate to the mountain system, with
the 2.5× rule applying to forms standing on it (GEO-8 option (c); 90 Glossary § Large Plateau). Its numbers
are research items. **For production design the table and the 2.5× rule stand as written** until this
document is deliberately revised ([99 Open Questions](../../../99%20Open%20Questions.md) GEO-8;
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §2.7).

**The separation rule: adjacent tiers must differ by at least 2.5× in height.** T2 tops out at 40 m,
T3 tops out at 12 m — a 3.3× gap. Anything that lands between the tiers muddies both and is
forbidden. This is the rule the terrace stage broke: it produced ~1.7 m steps everywhere, which is
below T3, above nothing, and therefore reads as corduroy rather than as landform.

**Corollary — no infinite staircase.** Heights come from a **fixed table**, never from a continuous
random range, and a form may stack on another only if the parent is a strictly higher tier. T3 on
T2, T2 on the island floor, T1 alone. Two levels of stacking maximum.

**Amendment 2026-09-16 — T0 floor relief is ground, not a tier.** The rule as written left the island
floor perfectly flat, and a flat floor is what made the baked island read as an extruded coastline: the
70–110 m band held 0.1–7.8 % of land. Stage 4A adds *floor relief* — macro-plate districts (uplift 18 m,
25 m on a world without a massif, 0.4–1.0 km² each, tops 0–5°, edges spread over 110–378 m of half-width),
knolls on the raised districts, and an apron at the massif foot. It is **not** a fourth landform tier and
must not become one: it has no wall, no flat-top-behind-a-scarp signature and no fixed height table — it
is the ground's own long-wavelength variation, and what separates it from T2 is **slope**, not height. A
T2 plateau is a flat shelf behind a wall; an 18 m district step spread over a few hundred metres is a
ramp you walk up without noticing. The 2.5× separation rule still governs everything *standing on* the
floor: the district step is ≥ 2.5 × the tallest knoll.
See [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md)
§4. R1 caps knoll height by the same slope budget that keeps the floor drainable; the analytic bound
currently places **no knolls** on the canonical seeds (design residual, not a retune).

---

## 2. Differential erosion is what makes a cliff

A plateau, mesa or butte exists because a **hard cap sits on softer rock**. The soft layer is
undercut, the cap breaks off in vertical blocks, and the cliff face retreats *parallel to itself* —
which is why the wall stays near-vertical for millions of years instead of slumping into a cone.
The debris piles at the foot as **talus**, and talus grades into soil.

**Rules we take from this:**

1. A plateau is **flat top + near-vertical wall + talus apron**. Three parts. A smooth dome is not a
   plateau, and the apron is not optional — a wall meeting flat ground at a hard corner is the
   single clearest "this was generated" tell.
2. **The wall does not run all the way round.** Scarp retreat is uneven; one flank is always further
   along. That uneven flank is where the ramp goes, which is also how the player gets up.
3. Shape ratio distinguishes the forms: **a mesa is wider than it is tall; a butte is taller than it
   is wide.** Our T2 plateaus are mesas (width ≥ 5× height); our T3 outcrops sit between mesa and
   butte (width 3–6× height). Nothing we place is a true butte — at 0.7 m/px we cannot hold a
   vertical wall taller than it is wide without it aliasing into a spike.
4. Erosion runs **after** every uplift, never before. Shape, then take material away. This is the
   uplift/stream-power equilibrium that real landscape-evolution models are built on.

---

## 3. Water writes the terrain, so terrain must be legible to water

Even in a dry landscape, sub-regional relief is shaped by flow. The strongest procedural approaches
generate the **hydrological network first** and grow terrain from it, rather than carving terrain and
adding rivers afterwards.

V0 water is the ocean plane only, so this section is a **constraint on shapes we build now**, not a
feature list:

1. Every slope must drain somewhere. A closed basin with no outlet is a bug unless it is a lake, and
   we have no lakes in V0 — so no closed basins. *Basin policy for the geography target (GEO-5 (b),
   2026-10-21):* a lake may exist only in a **declared basin** — an enclosure the geography declares, with
   a spill point and one outlet to the ocean; any undeclared closed depression stays a defect, and there
   are no endorheic basins (90 Glossary § Declared Basin). V0 water is still the ocean only.
2. **Scarps are future waterfall sites.** A waterfall forms where flow crosses a hard/soft boundary —
   exactly our plateau edges. So plateau rim heights must **vary**: a set of shelves all at the same
   altitude gives every future waterfall the same drop and reads as a tiled asset.
3. Valleys need headwaters. A valley that starts in the middle of a slope with nothing above it is on
   the World Creator guide's list of artificiality tells, alongside **radially symmetric mountains**
   and **ridges of uniform height**. All three are banned.

---

## 4. Legibility: the player has to be able to read the island

Nintendo built Hyrule on Kevin Lynch's five components of a mental map — **paths, edges, districts,
nodes, landmarks** — and that is what made a trackless world navigable.

**Triangle rule.** The pyramidal form is the basic building block: the player can climb it or walk
around it, and it hides what is behind it. Terrain should be composed so that **you cannot see a
landmark on a straight line from A to B** — shapes partially occlude each other, and the world
reveals itself as you move.

**Rules:**

1. Placement must produce **occlusion**, not a uniform scatter. At least half of the T2 plateaus sit
   between the coast and the massif, so approaching the massif from the beach means going around or
   over something.
2. **Every landmark you can see, you can reach.** This is the BotW promise and it is also our
   hardest measurable constraint — see §6.
3. Plateau tops are **nodes**: flat, distinct, worth standing on. That is what makes them settlement
   and point-of-interest ground later.

---

## 5. Controlled chaos: the seed picks placement, not physics

The failure mode of "make it random" is that every instance is subtly different and none is
characterful. The fix is a **grammar**:

- **A small fixed set of archetypes.** Three T2 shapes, two T3 shapes. Each has authored parameters.
- **The seed chooses which archetype goes where**, from a blue-noise distribution with a minimum
  separation — not a uniform random dart-throw, which clumps and leaves holes.
- **Parameters jitter by at most ±15 %** around the archetype. Rotation is free; height is not.
- The result is the same island grammar every seed, with a different arrangement — recognisable as
  "our world" while never repeating.

**Negative space is a budget.** The meadow is the subject, not the leftover. T2 + T3 footprints cover
**25–35 %** of dry land, hard ceiling **45 %**. The reference screenshots are mostly open grass with
rock at the edges of view, and that ratio is why they read as a place to walk rather than an
obstacle course.

---

## 6. Acceptance criteria — the rules with numbers attached

Measured by `OverworldTerrainDiagnostics` on every bake. A change that breaks one of these is a
regression regardless of how it looks in a screenshot.

**GEO-7 — classification resolved 2026-10-21 (owner direction).** For the geography target, hard gates
are the requirements with direct gameplay or topological meaning (settlement and POI capacity, routes
between regions, reachability, no accidental topological defects, rivers to designated outlets), the
owner's review at three scales judges composition, and morphology figures are diagnostics; flat land is
never rewarded for its own sake. So the "≥ 35 % under 5°" and "6–15 % steeper than 45°" rows below are
**not** targets for geography candidates; reachability stays a gate there (exceptions declared, 3F-20)
([99 Open Questions](../../../99%20Open%20Questions.md) GEO-7;
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §2.6). **For production bakes of today's
`H4A`, this table keeps its role as a regression check, as written.**

| Criterion | Threshold | Reported by |
|---|---|---|
| Dry land reachable from the shore at ≤ 45° | **≥ 95 %** | `reachable:` |
| Every T2 plateau top inside the reached set | **100 %** | `reachable:` + per-shelf probe |
| Dry land steeper than 45° (cliff faces) | **6–15 %** | `slope bands` |
| Dry land under 5° (meadow and tops) | **≥ 35 %** | `slope bands` |
| Distinct `TerrainLayer` per tile | **≤ 7** (URP TerrainLit hard cap 8) | `[LayerDump] layers=` |
| Habitable ground (`slope01 ≤ 0.45`) for settlements | not below today's figure | POI placer |

**First measured baseline** — seed 110201, 13 shelves placed, bake 15 s, per tile:

| tile | 0–5° | >45° | reachable | layers |
|---|---|---|---|---|
| 0_0 | 15.0 % | 15.8 % | 96.3 % | 7 |
| 0_1 | 21.5 % | 4.8 % | 97.7 % | 7 |
| 1_0 | 42.1 % | 3.7 % | 99.7 % | 7 |
| 1_1 | 27.9 % | 12.1 % | 99.0 % | 7 |

Reachability and the layer budget pass. The flat share is under the 35 % target on three tiles and
the cliff share is under 6 % on two — the shelves are landing, but there are not enough of them yet:
the placement disc plus the massif keep-out refused 10 of 23. That is a placement budget to widen,
not a shape to change.

45° is not an aesthetic choice: it is `CharacterController.slopeLimit` and the default NavMesh agent
slope. Below it a slope is floor; above it, it is wall. There is no third option, so the whole
design is built on that one number.

---

## 7. How this lands on our island

The relief tiers belong to **TerrainGen**, and the terrain bake writes whatever TerrainGen produces
without adding relief of its own. **T0** is the Stage 1 island floor and **T1** is the Stage 2 massif
uplift; both exist today. **T2 plateaus and T3 outcrops are not implemented.** When they are, they are
TerrainGen stages that add to the structural field — never a scene-level stamp, a second height writer
or a post-bake pass.

| Tier | Source | State |
|---|---|---|
| T0 island floor | TerrainGen Stage 1 landmass | built |
| T1 massif | TerrainGen Stage 2 massif uplift | built |
| T2 plateau | a future TerrainGen stage | not built |
| T3 outcrop | a future TerrainGen stage | not built |

**Placement rules for plateaus and outcrops** (the "controlled chaos" of §5, made concrete), for the
stage that implements them:

- Blue-noise sample inside the island silhouette, minimum separation **1.6 × footprint**.
- Reject any position within **250 m** of the massif's footprint — T1 owns its own skirt.
- Reject any position where the island floor is below **sea level + 8 m** — no shelves in the surf.
- Heights drawn from the fixed table `{20, 26, 34}` m for T2 and `{6, 9, 12}` m for T3, jittered ±15 %.
- A T3 outcrop may be placed on a T2 top (that is the sanctioned stack); T2 on T2 only where the
  overlap is partial, producing a two-step cascade, never a concentric tower.
- Each shelf's **ramp direction is its own yaw**, drawn per instance from the seed — so ramps do not
  all face the same compass point, which is what a single global dip direction produced.

**Shelf shape intent:**

```
flat top (low-amplitude relief)
  -> sharp shoulder                    = the caprock
  -> short erosion                     = gullies in the wall, talus at the foot
  -> a ramp on one flank, along the shelf's own yaw
```

The surface needs no new layer: the terrain bake's world-space rules already give a gentle top grass
and trees and a wall past the angle of repose rock or cliff, over the five canonical layers.

---

## Sources

- [Radiator Blog — Open world level design: spatial composition and flow in Breath of the Wild](https://www.blog.radiator.debacle.us/2017/10/open-world-level-design-spatial.html) — the triangle rule, three elevation scales, sight-line gating, "you don't want muddy shapes".
- [Game Developer — Realism and Legibility in Open-World Level Design](https://www.gamedeveloper.com/design/realism-and-legibility-in-open-world-level-design) — legibility versus realism as competing goals.
- [GMTK — How Nintendo Solved Zelda's Open World Problem](https://gmtk.substack.com/p/how-nintendo-solved-zeldas-open-world) — Kevin Lynch's paths/edges/districts/nodes/landmarks applied to Hyrule; every visible landmark is reachable.
- [World Creator — Digital Terrain Creation Guide](https://www.world-creator.com/en/learn/guides/digital-terrain-creation/digital-terrain-creation.phtml) — block out broad forms first; the artificiality tells (radial symmetry, uniform ridge heights, valleys without headwaters); masks as the shared currency between terrain, vegetation and gameplay; texel-density planning.
- [Map Effects — Plateaus, Mesas & Buttes: What's the Difference?](https://www.mapeffects.co/tutorials/plateau-mesa-butte) and [Wikipedia — Butte](https://en.wikipedia.org/wiki/Butte) — differential erosion, caprock over soft rock, scarp retreat parallel to itself, talus aprons, the mesa/butte width-to-height rule.
- [Nick McDonald — Procedural Hydrology: Dynamic Lake and River Simulation](https://nickmcd.me/2020/04/15/procedural-hydrology/) and [Red Blob Games — Procedural river drainage basins](https://www.redblobgames.com/x/1723-procedural-river-growing/) — flow must be monotonically downhill, pool into lakes, and drain to the sea; drainage-first generation.
- [Terrain Generation Using Procedural Models Based on Hydrology](https://www.researchgate.net/publication/248703095_Terrain_Generation_Using_Procedural_Models_Based_on_Hydrology) and [Large Scale Terrain Generation from Tectonic Uplift and Fluvial Erosion](https://www.researchgate.net/publication/292192025_Large_Scale_Terrain_Generation_from_Tectonic_Uplift_and_Fluvial_Erosion) — the uplift/stream-power equilibrium; hydrology network first, terrain grown from it.
- [Wikipedia — Waterfall](https://en.wikipedia.org/wiki/Waterfall) — waterfalls form where flow crosses a resistant/soft boundary, i.e. at scarps.
