# Town debug dumps (agent-readable)

These artifacts diagnose OrganicTown v2 geometry **without** Scene view.

## Produce

```bash
dotnet run --project tools/TownDebugHost -- 110201
```

Or in Unity: `GameDev/Town/Export Debug Report`.

## Outputs

| File | Purpose |
|------|---------|
| `town_seed_<seed>_report.md` | Counts, overlap probes, plain-language findings |
| `town_seed_<seed>_map.svg` | Top-down layers: districts, blocks, buildings, roads, portals |

## How to read viability

1. Open the Markdown **Findings** first.
2. Open the SVG: if amber/orange road strokes cut through brown building fills, streets are not corridors in the data model.
3. Healthy target after corridor fix (seed 110201): roads-in-buildings ≲ 10%, roads-in-blocks ≲ 15%.
4. If those spike again, check `Stage_Streets` (chord streets + leaf inset) and `Geo2.TrySplitChord`.

Greybox bake (`TownGreyboxBaker`) only visualizes this model — it does not invent a second street network.

Re-bake in Unity after regenerating: `GameDev/Town/Generate From Seed`.
