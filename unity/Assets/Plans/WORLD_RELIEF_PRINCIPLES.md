# World Relief Principles

What makes generated terrain read as a place rather than as noise, and the rules we hold ourselves
to. Sources are listed at the bottom; every rule here is either taken from one of them or is the
numeric form we chose for it.

This document is **why**. The Vista-specific *how* — node semantics, resolution limits, the traps —
lives in [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md). When the two disagree about a
number, this file states the intent and the playbook states what the engine can actually deliver.

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

**The separation rule: adjacent tiers must differ by at least 2.5× in height.** T2 tops out at 40 m,
T3 tops out at 12 m — a 3.3× gap. Anything that lands between the tiers muddies both and is
forbidden. This is the rule the terrace stage broke: it produced ~1.7 m steps everywhere, which is
below T3, above nothing, and therefore reads as corduroy rather than as landform.

**Corollary — no infinite staircase.** Heights come from a **fixed table**, never from a continuous
random range, and a form may stack on another only if the parent is a strictly higher tier. T3 on
T2, T2 on the island floor, T1 alone. Two levels of stacking maximum.

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
   we have no lakes in V0 — so no closed basins.
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

The island is `LPB_IslandBase` — a 2400 m plateau biome spanning 43.6–122 m of world height. It is
**T0**: the ground the tiers sit on, and it is not what carries the relief.

| Tier | Implementation | Blend | Order |
|---|---|---|---|
| T0 island floor | `LPB_IslandBase` (unchanged) | Replace | 10 |
| T2 plateau | `LPB_Shelf` ×6–10 | **Raise** | 15 |
| T3 outcrop | `LPB_Shelf` ×12–20, small archetype | **Raise** | 16 |
| T1 massif | `LPB_Mountain` ×1 | Raise | 20 |

**Why separate LPBs and not more nodes in the island graph.** Three measured reasons:

1. **Resolution.** The island biome resolves at 2600 m / 512 px = **5.0 m/px**, so no feature of its
   can be narrower than 5 m. A 300 m shelf with a 40 m falloff resolves at 380 m / 512 px =
   **0.74 m/px** — seven times finer. A 20 m wall over 1–2 px is ~1.5 m horizontal, i.e. essentially
   vertical. The island graph physically cannot produce a cliff; a small biome can.
2. **Height is metres.** `SetBiomeHeightOnSpawn.worldHeight = 25` is 25 m. Inside the island graph the
   same intent passes through four transforms and came out an order of magnitude wrong.
3. **Locality.** A graph operation applies everywhere its mask is non-zero, which is how the terrace
   ended up ringing the whole island. A biome has a boundary by construction.

**Placement rules for the shelves** (the "controlled chaos" of §5, made concrete):

- Blue-noise sample inside the island silhouette, minimum separation **1.6 × footprint**.
- Reject any position within **250 m** of the massif's footprint — T1 owns its own skirt.
- Reject any position where the island floor is below **sea level + 8 m** — no shelves in the surf.
- Heights drawn from the fixed table `{20, 26, 34}` m for T2 and `{6, 9, 12}` m for T3, jittered ±15 %.
- A T3 outcrop may be placed on a T2 top (that is the sanctioned stack); T2 on T2 only where the
  overlap is partial, producing a two-step cascade, never a concentric tower.
- Each shelf's **ramp direction is its own yaw**, drawn per instance from the seed — so ramps do not
  all face the same compass point, which is what a single global dip direction produced.

**Shelf graph shape** (small, one archetype per parameter set):

```
flat top (low-amplitude noise)
  -> Levels: sharp shoulder            = the caprock
  -> hydraulic erosion, short          = gullies in the wall, talus at the foot
  -> SlopeMask < 25 deg  -> grass  + trees   (top)
  -> SlopeMask > 35 deg  -> Overworld_Cliff  (wall)
  -> AxialGradient along local +X, subtracted -> the ramp on one flank
```

All layers are canonical (`Overworld_Grass`, `Overworld_Cliff`), so the per-tile layer count does not
grow. Trees and grass come from the shelf's own graph outputs using the existing
`VistaVegetationTemplates` species, so a plateau top is meadow with trees on it, not bare rock.

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
