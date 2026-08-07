# OVERWORLD V0 — Micro-Plan

Status: Ready for implementation  
Created: 2026-08-07  
Authority: [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)

---

## Goal

Ship a **seeded overworld stub** that proves the post-pivot world-gen rails:

> WorldSeed → biome bands + walkable greybox terrain + 3–5 POI markers + simple creature spawn points (no harvest)

Chunk API must behave as if the world can expand; V0 playable area is ~512 m with **64 m** chunks.

---

## Non-goals

- Free 3P locomotion polish (→ Locomotion V0)
- Interior / arena scene transitions
- Continuous enterable OrganicTown / TownGridBaker path
- Harvest / base-building
- Full biome art, rivers, Valheim-scale map
- Monolithic `OverworldManager` owning all domains

---

## Locked defaults

| Item | Value |
|------|-------|
| Chunk size | 64 m |
| Stub extent | 8×8 chunks (512 m) centered on origin (or documented origin chunk) |
| Load model | Resident ring around focus; teardown outside ring |
| Determinism | Same seed → same heights, biomes, POIs, spawn points |
| SO usage | Immutable defs only (biome colors, POI kind defs); no runtime state in SO |
| Code budget | One iteration ≤ ~500 LOC; W0.1–W0.9 separately |

---

## Acceptance criteria

1. Editor (or Play) path: set seed → generate → clear.
2. Same seed twice → identical POI positions, spawn points, biome samples at probe points (EditMode tests).
3. Different seed → observable difference in markers or height/biome.
4. Focus moves → chunks load/unload without requiring full world rebuild.
5. At least **3** and at most **5** POI markers in the stub from kinds: ruin, camp, shrine, town approach, cave mouth.
6. Spawn points exist as data/debug visuals only (no AI, no combat).
7. No GridMap gameplay truth on overworld.
8. OrganicTown continuous menus remain untouched as archive (optional read-only reference).

---

## System cards in play

See Track W in [17](../../../17%20Game%20Systems%20Architecture.md): WorldSeed, Terrain/Biome, Chunk Streaming, POI Placement, Spawn Tables. Settlement/Building/Interior are **out** of V0 code (markers only).

---

## Iterations

### W0.1 — WorldSeed service

- Promote/wrap existing seed helper behind `IWorldSeed` with `Fork(string streamId)`.
- EditMode: same seed + stream → same sequence of N floats/ints.
- Folder: `unity/Assets/Scripts/World/Seed/`

### W0.2 — ChunkCoord math

- `ChunkCoord`, world↔chunk↔local for 64 m.
- Tests for edge alignment and negative coords.
- Folder: `unity/Assets/Scripts/World/Streaming/`

### W0.3 — IBiomeSampler stub

- Height + biome band from seed + XZ (simple noise / bands).
- 2–4 biome ids enough.
- Tests: determinism at fixed probes.
- Folder: `unity/Assets/Scripts/World/Terrain/`

### W0.4 — ITerrainChunkBuilder greybox

- Build a walkable greybox mesh/colliders per chunk from sampler.
- Teardown removes instance cleanly.
- Folder: `unity/Assets/Scripts/World/Terrain/`

### W0.5 — IChunkStreamer

- Thin coordinator: compute desired set from focus + radius; call build/teardown.
- Compose builders via interfaces — no god object.
- Folder: `unity/Assets/Scripts/World/Streaming/`

### W0.6 — IPoiPlacer

- Place 3–5 markers across stub using seed fork `"poi"` + biome/height constraints.
- Stable `PoiId`; debug gizmos or simple markers in scene.
- Folder: `unity/Assets/Scripts/World/Poi/`

### W0.7 — ISpawnTable stub

- Spawn points per chunk/biome; debug markers only.
- Folder: `unity/Assets/Scripts/World/Spawns/`

### W0.8 — Lab scene + menu

- Scene e.g. `Overworld_Generator.unity` (name flexible if documented).
- Menu under `GameDev/Overworld/…`: Open, Generate, Clear.
- Wire seed field + generate-on-demand (not a silent Play trap unless intentional).

### W0.9 — Handoff

- Update [14](../../../14%20World%20Generation%20Roadmap.md) “Where We Stopped”.
- Update [AGENT_HANDOFF_WORLD_GEN.md](AGENT_HANDOFF_WORLD_GEN.md) to mark Overworld V0 code status.

---

## Suggested tests

| Test | Asserts |
|------|---------|
| SeedForkDeterminism | Identical sequences |
| ChunkCoordRoundTrip | World points map stably |
| BiomeSamplerDeterminism | Same seed/xz → same height/biome |
| PoiPlacerCountAndStability | Marker count in [3,5]; positions stable |
| StreamerResidentSet | Focus move changes residency predictably |

---

## Dependencies / reuse

- Prefer existing `WorldSeed` fork style from OrganicTown archive.
- Do **not** depend on `TownGridBaker`, HideWalls, or playable GridMap radius.
- Dress/asset library docs remain available for later shells; unused in V0.

---

## Done definition

All acceptance criteria green; W0.1–W0.8 merged; W0.9 docs updated; next agent default becomes **Locomotion V0** micro-plan (not yet written until Overworld V0 lands).
