# 14 World Generation Roadmap

Version: 0.10

Status: Draft

Last Updated: 2026-08-07

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

Code under `unity/` is still **pre-pivot prototype archive** until Overworld V0 iterations land.

## Pre-pivot prototype archive (do not extend by default)

| Track | Deliverable | Notes |
|-------|-------------|-------|
| OrganicTown v2 A–D2 + P0 | Vector town, greybox, templates, occlusion, radius GridMap | Continuous enterable city — superseded as primary path |
| v1 raster city | `CityGenerator` playable Lower Veil | Legacy mood / reference |

Detail plans remain under `unity/Assets/Plans/` with **Superseded** banners.

---

# Locked Architecture (post-pivot — do not silently reverse)

1. **Overworld** is the authoritative exploration space: world seed → biomes / zones / POIs / creature spawns ([16](16%20Architecture%20Pivot.md)).
2. **Gameplay movement** on overworld and interiors is **free third-person**, not GridMap.
3. **Interiors** are **separate scenes** (free move, no grid).
4. **Confront** always uses a **Combat Arena** scene; grid + Turn Action Economy apply only there ([10](10%20Turn%20Action%20Economy.md), [10.1](10.1%20Grid%20World%20and%20Environment%20Actions.md)).
5. **“Valheim-level”** means deterministic seeded world + biome diversity + streamed/chunked intent — **not** survival harvest, not AAA biomes art in V0.
6. Pre-pivot OrganicTown continuous fabric is **not** the next implementation default.
7. Implement **one micro-plan at a time** from the AI backlog in [16](16%20Architecture%20Pivot.md).

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

# Superseded pre-pivot order (archive)

```text
A → B → D1 → D2 → P0 → C1 continuous narrative → C2 full GridMap → E wilderness
```

Do **not** resume this order unless the user explicitly overrides [16](16%20Architecture%20Pivot.md).

---

# Agent Instructions (quick)

1. Read [16 Architecture Pivot](16%20Architecture%20Pivot.md), then [17 Game Systems Architecture](17%20Game%20Systems%20Architecture.md), then this file.
2. **Next implementation default:** `unity/Assets/Plans/OVERWORLD_V0_PLAN.md` (W0.1→W0.9). Do not extend OrganicTown C1 continuous path.
3. Keep style: no inheritance tricks; deterministic `WorldSeed` forks; human-scale metres; chunk **64 m**; stub **~512 m**.
4. Confirm design forks with the user before changing locked architecture above.
5. After a documentation or milestone step: commit when the user asks (human steers git).

---

# Explicitly Out (until later phases)

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
- [13 City Generation](13%20City%20Generation.md) — legacy / mood reference
- [12 Engine Choice](12%20Engine%20Choice.md)
- [11 Vertical Slice](11%20Vertical%20Slice.md)
- Unity plans: `OVERWORLD_V0_PLAN.md`, archive under `unity/Assets/Plans/`
