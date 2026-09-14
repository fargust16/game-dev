# AGENT HANDOFF — World / Town Generation

> **SUPERSEDED AS PRIMARY PATH (2026-08-07).**  
> Spatial / presentation authority: repo-root [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md).  
> Systems map / iterations: [`17 Game Systems Architecture.md`](../../../17%20Game%20Systems%20Architecture.md).  
> Roadmap: [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md).  
> OrganicTown continuous enterable + P0 GridMap code in `unity/` is **pre-pivot prototype archive**. Do **not** extend full C1 continuous narrative by default.  
> **Next default:** [`OVERWORLD_V0_PLAN.md`](OVERWORLD_V0_PLAN.md) (iterations W0.1–W0.9).

**Read first:** [`16 Architecture Pivot.md`](../../../16%20Architecture%20Pivot.md), then [`17 Game Systems Architecture.md`](../../../17%20Game%20Systems%20Architecture.md), then [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md), then [`OVERWORLD_V0_PLAN.md`](OVERWORLD_V0_PLAN.md).

**Full work report (Milestone B + corridor fix):** [MILESTONE_B_WORK_REPORT.md](MILESTONE_B_WORK_REPORT.md)

**Detail plan (OrganicTown v2 — archive):** [procedural-organic-town-generator.md](procedural-organic-town-generator.md)

## Pre-pivot stop point (archive, 2026-08-06)

- OrganicTown v2 Milestone A–D2 + P0 Playable Town were completed under the old continuous-grid locks.
- v1 raster city still exists under `GameDev/City/…`.
- Known issue: HideWalls + Wall cells → “air walls” on P0 GridMap.

## Post-pivot order

```text
Architecture Pivot docs ✅
  → Game Systems Architecture (17) ✅
  → Overworld V0 plan ✅ → Overworld V0 code (W0.1–W0.9)
  → Locomotion V0 → Interior V0 → Arena V0 → Theme Packs V0
    → Vertical Slice content on new rails
```

Do not skip ahead to Valheim-scale polish or collapse phases into one uncontrolled sprint, unless the user explicitly overrides.

## Locked V0 engineering defaults (from 17)

- Chunk size: **64 m**
- Playable overworld: **2800 × 2800 m** (2×2 Unity Terrain tiles of 1400 m, height budget 700 m, sea level 24 m)
- Terrain: TerrainGen relief baked into Unity `TerrainData` — heightmap 513, alphamap 512 per tile ([OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md))
- Interior / Arena load: **single-scene swap** until a later wave

## First commands for a new agent

```bash
# Docs first — then implement OVERWORLD_V0_PLAN.md starting at W0.1
# Optional archive check only:
dotnet run --project tools/TownDebugHost -- 110201
```
