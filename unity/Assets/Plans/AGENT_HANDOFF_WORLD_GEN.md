# AGENT HANDOFF — World / Town Generation

**Read first:** repo-root [`14 World Generation Roadmap.md`](../../../14%20World%20Generation%20Roadmap.md) (authoritative stop-point + roadmap).

**Full work report (Milestone B + corridor fix):** [MILESTONE_B_WORK_REPORT.md](MILESTONE_B_WORK_REPORT.md)

**Detail plan (OrganicTown v2):** [procedural-organic-town-generator.md](procedural-organic-town-generator.md)

## Stopped here (2026-08-06)

- **OrganicTown v2 Milestone A = DONE** (vector `TownModel` + stages + gizmos). Menu: `GameDev/Town/…`
- **OrganicTown v2 Milestone B = DONE** (`TownGreyboxBaker` — street ribbons + exterior shells + landmark from footprints). Auto-bake on generate; menu `GameDev/Town/Bake Greybox`
- **Street corridor topology FIXED (2026-08-06):** `Stage_Streets` places streets on split chords, leaf blocks inset by half-width, node snap/merge. Seed 110201: ~4.5% road samples in buildings, ~5.7% in blocks (was 35% / 99%). Debug: `dotnet run --project tools/TownDebugHost -- 110201` → `Assets/Plans/debug/`.
- **D1.1 DONE** — enterable template foundation (commit `83be757`). Seed 110201: ~26% templated with 5 large starters; roads OK.
- **D1.2 DONE** — catalog 16 types; seed 110201 **55%** templated (was 26%); roads still OK. Plan: [`MILESTONE_D1_PLAN.md`](MILESTONE_D1_PLAN.md)
- **v1 raster city still playable.** Menu: `GameDev/City/…` — do not delete until roadmap **C2**
- **Next default:** **D1.3** NPC/prop socket handoff for C1, or **D2** occlusion if user prioritizes readable interiors

## Order

`A ✅ → B ✅ → D1 (enterable templates; D1.1 underway) → D2 (BG3 occlusion) → C1/C2 (narrative + GridMap) → E (wilderness/caves)`

Do not skip ahead to Valheim wilderness or collapse D1+D2+C into one sprint, unless the user explicitly overrides.

## First commands for a new agent

```bash
dotnet run --project tools/TownDebugHost -- 110201
# then open unity/Assets/Plans/debug/town_seed_110201_report.md
# Unity: GameDev/Town/Generate From Seed (do not rely on a committed baked scene)
```
