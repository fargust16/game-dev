# 14 World Generation Roadmap

Version: 0.16

Status: Draft

Last Updated: 2026-10-18

Depends On:
[11 Vertical Slice](11%20Vertical%20Slice.md), [12 Engine Choice](12%20Engine%20Choice.md), [16 Architecture Pivot](16%20Architecture%20Pivot.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md)

Referenced By:
[AGENTS.md](AGENTS.md), [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md), [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), Unity plans under `unity/Assets/Plans/`

---

# Purpose

Handoff document for **future agents and humans**: where world generation stopped, what is authoritative after the Architecture Pivot, and the **ordered roadmap** toward a seeded Valheim-shaped overworld with separate interiors and Combat Arenas.

Chat is not the source of truth — update this file when a milestone lands.

Success question for agents:

> Can a new agent open this doc and know exactly what to implement next without continuing the pre-pivot continuous OrganicTown path?

**Presentation / scene authority:** [16 Architecture Pivot](16%20Architecture%20Pivot.md).  
**Systems map / iteration backlog:** [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md).

---

# Where We Stopped (2026-08-07)

## Architecture Pivot ✅ (docs)

Locked in [16](16%20Architecture%20Pivot.md):

- seeded Valheim-like overworld (scoped; no harvest loop);
- free third-person locomotion (Genshin-like feel);
- interiors as separate free-move scenes;
- Confront → thematic Combat Arena (grid TB only there).

## Game Systems Architecture ✅ (docs)

[17](17%20Game%20Systems%20Architecture.md) v0.1 records System Cards, dependency DAG, and micro-iterations (Track W = world gen).  
Overworld V0 code micro-plan: `unity/Assets/Plans/OVERWORLD_V0_PLAN.md`.  
Full target shape for Track W beyond V0 (world layout → macro terrain → hydrology → biomes → materials → vegetation → roads → settlements → streaming, plus the 12-iteration build order): `unity/Assets/Plans/WORLD_GENERATION_PIPELINE.md`. Not a build order override — `OVERWORLD_V0_PLAN.md` and this file's roadmap table below still govern what ships next.

Code under `unity/` is still **pre-pivot prototype archive** until Overworld V0 iterations land.

## Pre-pivot prototype archive (deprecated — not a reuse source)

| Track | Deliverable | Notes |
|-------|-------------|-------|
| OrganicTown v2 A–D2 + P0 | Vector town, greybox, templates, occlusion, radius GridMap | Continuous enterable city — deprecated, not viable, not reused |
| v1 raster city | `CityGenerator` playable Lower Veil | Deprecated, historical reference only ([13](13%20City%20Generation.md)) |

Detail plans remain under `unity/Assets/Plans/` with **Superseded** banners. Neither track feeds the new Piece-Based Assembly pipeline in [15](15%20Procedural%20Building%20Generation.md).

---

# Locked Architecture (post-pivot — do not silently reverse)

1. **Overworld** is the authoritative exploration space: world seed → biomes / zones / POIs / creature spawns ([16](16%20Architecture%20Pivot.md)).
2. **Gameplay movement** on overworld and interiors is **free third-person**, not GridMap.
3. **Interiors** are **separate scenes** (free move, no grid).
4. **Confront** always uses a **Combat Arena** scene; grid + Turn Action Economy apply only there ([10](10%20Turn%20Action%20Economy.md), [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).
5. **“Valheim-level”** means deterministic seeded world + biome diversity + streamed/chunked intent — **not** survival harvest, not AAA biomes art in V0.
6. Pre-pivot OrganicTown continuous fabric is **deprecated** — not the next implementation default, and not a reuse source for the new pipeline ([13](13%20City%20Generation.md)).
7. **Piece-Based Assembly, small → large.** World, settlement, and building generation compose from small hand-authored Pieces with Snap Sockets (Valheim-style), never from a whole hand-tuned continuous mesh or an algorithmic subdivision solver. Detail: [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md).
8. Implement **one micro-plan at a time** from the AI backlog in [16](16%20Architecture%20Pivot.md).

---

# Roadmap (mandatory order — post-pivot)

```text
Architecture Pivot docs ✅
    → Overworld V0
        → Locomotion V0
            → Interior V0
                → Arena V0
                    → Theme Packs V0
                        → Vertical Slice content on new rails
                            → richer biomes / interior DNA / monsters
```

| Phase | Goal | Outcome | Depends on |
|-------|------|---------|------------|
| **Pivot docs** ✅ | Spatial split locked | [16](16%20Architecture%20Pivot.md) + related doc updates | — |
| **Overworld V0** | Seeded stub world | Biome bands + terrain + 3–5 POI markers + spawn points; iterations **W0.1–W0.9** in [17](17%20Game%20Systems%20Architecture.md) / `OVERWORLD_V0_PLAN.md` | Pivot + systems map |
| **Locomotion V0** | Free 3P feel | Controller + follow/orbit camera | Overworld V0 |
| **Interior V0** | Scene transition | Door → interior → exit; free move | Locomotion V0 |
| **Arena V0** | Confront presentation | Theme arena + TB skeleton + return + residue stub | Interior V0 |
| **Theme Packs V0** | Arena variety | Street / tavern / woods greybox kits | Arena V0 |
| **Slice content** | Lower Veil fantasy | POIs / tips / Decision Nodes on new rails | Theme Packs V0 |

Each phase = its own plan + commit series. Prefer updating this file’s “Where We Stopped” when a phase completes.

---

# Island Shaping Rebuild (superseded)

The directional-banding investigation belonged to the previous external terrain pipeline's island
shaping stage. That stage no longer exists: the overworld relief is TerrainGen's authoritative field,
written straight into Unity `TerrainData`
(`unity/Assets/Plans/OVERWORLD_TERRAIN_ARCHITECTURE.md`). Whether the TerrainGen relief has banding
of its own has not been measured; judge it on a bake, not by analogy with the removed pipeline.

---

# Island Relief — TerrainGen Stage 4A (the terrain)

**What replaced the shaping stage.** Measuring the baked island showed the flaw the old recipe had hidden
with noise: the relief was a function of coast distance. 95–99 % of the interior had its height gradient
aligned with the coast normal, and the 70–110 m band — the whole middle of the island — held 0.1–7.8 % of
the land. TerrainGen Stage 4A adds the missing floor relief from structures that already exist: a 1-D
coastal floor `M(d)`, macro-plate districts (uplands and lowlands, with a 20 % lowland reservation),
knolls on the raised districts from crustal cell cores (slope-budget capped; none placed on the
canonical seeds), and an apron at the foot of the Stage 2B massif, domain-limited to the axis reach.
Every term is gated so the land mask cannot move.

**Status 2026-09-18.** Revision R1 is **the production terrain authority**: `TerrainAuthorityField`
evaluates `IslandElevation` (`H4A = H1 + U2 + R4A`) at `GeneratorVersion` 2. `TerrainGenStage4ATests`
16/16, full EditMode 572/572, canonical S8-A/S8-M/S8-C/S10/S11/S12 green, sweep 1000–1039 S8-A 0/40 and
S8-M 6/40. The production field reproduces the dump-first `H4A` hash exactly on all seven canonical
seeds, which is what closed the question of whether Stage 2A's plates and Stage 2B's are the same
plates. Residuals (zero knolls, apron ring/zone, coast-aligned gradient, curvature ratio, massif-foot
pits) are in `unity/Assets/Plans/OVERWORLD_TECH_DEBT.md` item 19, owned by Stage 3F / Stage 4B. Living
contract: `unity/Assets/Plans/PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md`.

**Next:** Stage 3F (the structural valley carve, `H = H4A − C3F`), then Stage 4B/4C. Stage 3D/3E keep
declaring their basins and outlets on `StructuralElevation` (`H2`) — the surface they were frozen
against — so Stage 3F is what reconciles the two levels.

---

# Geography-first research (2026-10-18) — proposals, nothing approved

The baked island reads as a flat island with one convex elevation, and that is what the production
composition produces: `H1` is a function of coast distance only (a 24 → ~73 m dome), `U2` is at most one
unbranched massif (none on 4 of 29 audited worlds), and Stage 4A adds bounded floor relief with no
ridge, valley or second high region. Ridges, valleys and basins exist only as Stage 3 structure or in the
experimental Stage 3F carve. Measured on seed 110201: land p50 58 m, p90 108 m, ~10 % of land above
110 m; ~90 % of the bake time is TerrainGen evaluation, so a neutral review material is a legibility
change, not a speed change.

Research record, target geography, stage responsibility map, acceptance suite and experiment roadmap:
`unity/Assets/Plans/GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md`. Owner questions: [99 Open Questions](99%20Open%20Questions.md)
§ Terrain — Geography-first world shape (GEO-1 … GEO-7).

What this changes in the order above: **nothing yet.** The research concludes that most of the missing
geography (a mountain system, lowland trunk valleys, relief on massif-free worlds, cliff coasts) is
upstream of Stage 3F, which carves drainage into a coherent relief but cannot create one. The next steps
it proposes are read-only and isolated — a geography metrics baseline (G0) and a heights-only
Geometry-First Review tool (G1). The owner approved both (GEO-6 (a), 2026-10-18) and both are built as
Editor-only tooling; neither changes the order above. Their baseline (research record §7.6) is the
evidence for the owner's review of the target (G2). Stage 3F research
continues on the parts that survive any upstream decision (research record §6.3).

---

# Hydrology / River Generation

**Designed and built 2026-09-03; the carve now lands directly in the terrain lattice.** Full design:
`unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md`.

The drainage analysis ships and is measured: rivers with tributaries, terrain-derived lakes,
waterfall candidates, water meshes and an `IWaterSampler` seam for roads. It runs as the `Hydrology`
pipeline step between Mountains and Markers, as a **two-pass** bake — the analysis reads the finished
relief off `TerrainData`, and the second terrain bake subtracts the planned carve from the terrain
lattice before the surface and vegetation are derived. TerrainGen never reads the carve back.

**`RunHydrology` is off by default.** The earlier blocker — the carve not landing — belonged to the
removed external terrain pipeline; the direct lattice carve replaces it and is verified on seed 110201
(delivered cut within 0.02 m of the plan, nothing outside it moved by more than 0.02 m). A carved world
has not yet been reviewed visually, so turning the step on by default is a separate decision.

The constraints below were the brief the design had to satisfy, and all of them held; they are kept
because any future change to hydrology still has to satisfy them.

## The previous attempts were discarded, not archived

Several inland-hydrology implementations were built and removed: lake site selection and basin
digging, river tracing and spline/ribbon meshes, per-lake water objects, `WaterZone_*` scene
hierarchies, and the settings that fed them. They repeatedly produced architectural complexity,
hidden dependencies, duplicated systems and bugs, and they made terrain generation harder to change.

They are **not an architectural foundation**. Do not resurrect `LakeDefinition`, `RiverTracer`, the
water-zone menu architecture, or any of the deleted constants because they look convenient. Starting
from that code is what made every previous attempt fail the same way.

## What the system models

Worked out from first principles, along this chain:

```text
source → flow direction → drainage basin → terrain valley → river channel
       → lake / depression relationship → water surface
       → downstream terrain and ecology consequences
       → roads, bridges, crossings → POI placement
```

The quality bar is the one already stated in `WORLD_GENERATION_PIPELINE.md`: **rivers reshape
terrain**. Path → valley → depression → bed → water, in that order. A blue spline laid on unmodified
terrain is not hydrology.

## Constraints any design must respect

- **TerrainGen owns terrain relief; the terrain bake owns the carve.** A river lowers the terrain
  lattice inside the bake (`HydrologyBake.ApplyPendingCarve`), after TerrainGen and before the surface
  and vegetation are derived — never as a pass over finished `TerrainData`, and never fed back into
  TerrainGen.
- **The heightmap resolves 2.73 m per vertex** (513 over 1400 m). A channel narrower than that cannot
  be represented as terrain height — that constraint has to be part of the design.
- Determinism: same seed → same watercourses, and re-seeding hydrology must not move the mountains.
- Roads already plan around water through `SeaLevelOceanSampler`; any inland water must enter road
  cost and bridging through that same interface rather than a parallel one.

Written before the code: `unity/Assets/Plans/HYDROLOGY_GENERATION_PLAN.md`.

## What is still out

Riverbed material and clearing vegetation from the channel are V2 — the carve only lowers the
lattice, so surface and vegetation rules treat a channel like any other low ground.
Waterfall **rendering** is out; only the candidates are derived. Bridges, moisture and
distance-to-water remain future consumers.

---

# Superseded pre-pivot order (archive)

```text
A → B → D1 → D2 → P0 → C1 continuous narrative → C2 full GridMap → E wilderness
```

Do **not** resume this order unless the user explicitly overrides [16](16%20Architecture%20Pivot.md).

---

# Agent Instructions (quick)

1. Read [16 Architecture Pivot](16%20Architecture%20Pivot.md), then [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), then this file.
2. **Next implementation default:** `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` (W0.1→W0.9). Do not extend OrganicTown C1 continuous path.
3. Keep style: no inheritance tricks; deterministic `WorldSeed` forks; human-scale metres; chunk **64 m**; playable overworld **2800 × 2800 m**.
4. Confirm design forks with the user before changing locked architecture above.
5. After a documentation or milestone step: commit when the user asks (human steers git).

---

# Explicitly Out (until later phases)

- Riverbed material, vegetation clearing in the channel, and waterfall rendering
  (see § Hydrology / River Generation)
- Survival harvest / base-building loop
- Full Valheim-scale map
- Marketplace tactics kit as architecture
- Continuous enterable city GridMap as world truth
- Voluntary Turn-Based Mode during exploration
- Full World Engine Influence sim
- Multi-city overworld map
- Shipping deep combat AI as part of Overworld V0

---

# Related Documents

- [16 Architecture Pivot](16%20Architecture%20Pivot.md) — spatial / presentation authority
- [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md) — systems map + Track W iterations
- [15 Procedural Building Generation](15%20Procedural%20Building%20Generation.md) — exterior shells + interior scenes
- [13 City Generation](13%20City%20Generation.md) — deprecated, historical only
- [12 Engine Choice](12%20Engine%20Choice.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- Unity plans: `OVERWORLD_V0_PLAN.md`, `WORLD_GENERATION_PIPELINE.md`, archive under `unity/Assets/Plans/`
