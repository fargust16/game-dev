# AGENT HANDOFF — World / Town Generation

**Read first:** repo-root [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md) (authoritative stop-point + roadmap).

**Full work report (Milestone B + corridor fix):** [MILESTONE_B_WORK_REPORT.md](MILESTONE_B_WORK_REPORT.md)

**Detail plan (OrganicTown v2):** [procedural-organic-town-generator.md](procedural-organic-town-generator.md)

## Stopped here (2026-08-06)

- **OrganicTown v2 Milestone A = DONE** (vector `TownModel` + stages + gizmos). Menu: `GameDev/Town/…`
- **OrganicTown v2 Milestone B = DONE** (`TownGreyboxBaker` — street ribbons + exterior shells + landmark from footprints). Auto-bake on generate; menu `GameDev/Town/Bake Greybox`
- **Street corridor topology FIXED (2026-08-06):** `Stage_Streets` places streets on split chords, leaf blocks inset by half-width, node snap/merge. Seed 110201: ~4.5% road samples in buildings, ~5.7% in blocks (was 35% / 99%). Debug: `dotnet run --project tools/TownDebugHost -- 110201` → `Assets/Plans/debug/`.
- **v1 raster city still playable.** Menu: `GameDev/City/…` — do not delete until roadmap **C2**
- **Next default phase: D1** — enterable BuildingTemplates (10–20 types) on plots

## Order

`A ✅ → B ✅ → D1 (enterable templates) → D2 (BG3 occlusion) → C1/C2 (narrative + GridMap) → E (wilderness/caves)`

Do not skip ahead to Valheim wilderness or collapse D1+D2+C into one sprint, unless the user explicitly overrides.

## First commands for a new agent

```bash
dotnet run --project tools/TownDebugHost -- 110201
# then open unity/Assets/Plans/debug/town_seed_110201_report.md
# Unity: GameDev/Town/Generate From Seed (do not rely on a committed baked scene)
```
