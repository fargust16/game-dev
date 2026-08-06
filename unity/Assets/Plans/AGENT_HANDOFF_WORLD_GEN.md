# AGENT HANDOFF — World / Town Generation

**Read first:** repo-root [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md) (authoritative stop-point + roadmap).

**Detail plan (OrganicTown v2):** [procedural-organic-town-generator.md](procedural-organic-town-generator.md)

## Stopped here (2026-08-06)

- **OrganicTown v2 Milestone A = DONE** (vector `TownModel` + stages + gizmos). Menu: `GameDev/Town/…`
- **v1 raster city still playable.** Menu: `GameDev/City/…` — do not delete until roadmap **C2**
- **Next default phase: B** — `TownGreyboxBaker` (shells from footprints)

## Order

`A ✅ → B → D1 (enterable templates) → D2 (BG3 occlusion) → C1/C2 (narrative + GridMap) → E (wilderness/caves)`

Do not skip ahead to Valheim wilderness or 20 interior prefabs before B exists, unless the user explicitly overrides.
