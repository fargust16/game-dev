# Vista terrain generation is bistable — investigation closed, escalated

Phases 2G–2P. Read this before trusting any full-stack terrain measurement.

## Symptom

The same project, the same seed, a fresh Unity process per run, and no code change
between runs produces **one of exactly two worlds**. It is not noise: independent
anomalous runs are byte-for-byte identical to each other.

```
CANONICAL   0_0 5bb160b43fc3   0_1 618277545584   1_0 ecf2cc8715e4   1_1 339b29d3cb79
ANOMALOUS   0_0 4bde50e84ad5   0_1 eab3485133ae   1_0 b66a9f21a75f   1_1 16f1ffabfd59
```

The anomalous world is **+35.4 … +36.0 m** higher in every tile maximum, its minima are
unchanged to a tenth of a metre, and roughly 37 % of the world's cells are affected in
large connected regions.

Frequency ranges from 0/6 to 6/6 per batch; about **30 %** over ~60 full bakes. The
per-batch spread is real and unexplained — do not treat 30 % as a stable probability.

## Reproduction

```bash
Unity.exe -batchmode -projectPath <project> -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump -overworldSeed 110201 -overworldRebuildGraphs true -overworldExportPng true
```

Every bake is a separate Unity process. No recompilation is required.

**Island-only bakes** (`-overworldMountains false -overworldSkipSeaFloor true`) were
consistent across every phase from 2A to 2E. The bistability has only ever been observed
in full-stack bakes. That is an observation, not a proven property — but it means work
measurable island-only can proceed today, and full-stack measurement cannot be trusted
from a single bake.

## First divergence

Immediately after Vista's GPU generation and heightmap readback, **before**
`MatchVistaSeams` and before any `TerrainData` post-processing. Measured with the
pipeline's own `after-vista[i]` log:

```
CANONICAL   318.3 / 285.7 / 357.1 / 342.9 m      global max 357
ANOMALOUS   354.0 / 321.2 / 393.1 / 378.6 m      global max 393
minima      0.0 / 0.0 / 1.7 / 1.9 m              identical in both
```

## PROVEN FACT — identical in both states

Measured directly, not inferred.

| what | evidence |
|---|---|
| executed per-biome graph `IslandBase_IslandPlateau_TerrainGraph` | id-invariant structure: 105 nodes, 87 edges, node hash `bde85a18a31ed6a2`, edge hash `09d7d3d625684ff2`, one distinct value across 8 runs incl. anomalous |
| geological topology | beds 3, modulation 3, knee 1, clamp 1, fronts [974, 1282, 1590], amplitudes [10, 12, 14] |
| execution plan | 80 nodes, digest `1E691596`, node-for-node identical; one graph execution per bake |
| generation request | 512 px, terrainHeight 700.0, seed 1383877999 |
| biome configuration | order/worldHeight/scaleY/graph/anchors/falloff/seed identical for all three biomes |
| scene, mountain graph, sea-floor graph, Vista temp cache | byte-identical across all runs |

Note on method: the graph asset's **file hash is not an identity** — Vista regenerates
node and edge ids on every rebuild, so it differs between canonical runs too. Only the
id-invariant structural hash is meaningful. An early conclusion in Phase 2J was wrong
until this was corrected.

## PROVEN FACT — experimentally excluded

| hypothesis | how it was excluded |
|---|---|
| geology applied twice in the source graph | detector inside `AddGeologicalStrata`: 24/24 runs, never patched an already-patched graph |
| geology duplicated in the executed graph | id-invariant structure identical in anomalous runs; **one** geological branch, not two |
| `GraphExecutionCache` | one graph execution per fresh process, cache is instance-local — nothing to hit |
| stale render-target contents | `clearContent = true` at 111 of 114 creation sites and `GraphicsUtils.ClearWithZeros` genuinely clears; the 3 exceptions are IO nodes that fully overwrite |
| render-target reuse | with pool reuse **completely disabled**, the anomaly still occurred 3/6 |
| biome height accumulation | island and sea floor are `Replace`, massif is `Raise`; both idempotent |
| rigid vertical biome offset | `Replace` would move the coast; minima do not move |
| `TerrainData` post-processing, seams | anomaly already present at the `after-vista` checkpoint |
| tile traversal order | fixed in Phase 2G; unrelated to the height data |
| missing GPU completion before readback | explicit `GraphicsFence(CPUSynchronisation, AllGPUOperations)` before `SyncHeightmap()`: 5/6 still anomalous vs 6/6 without |

## STRONG EVIDENCE — the observer effect

Three independent probe designs each collapsed the bistability into **the same third
stable state** `cdb816ac2e2e`:

```
VistaDebugger node capture (blit 128² + readback per node)       6/6
GPU min/max probe, buffer allocated + uploaded per checkpoint    8/8
same probe, all buffers preallocated, no sync inside the loop    8/8
```

The contrast that makes this informative: disabling render-target reuse also adds GPU
work (fresh allocations and clears) and the anomaly **survived**. What the three probes
share is not extra work but **reading each node's output**.

This is strong evidence that the result is sensitive to internal GPU execution or
resource ordering. It is not proof of any particular mechanism.

## UNPROVEN HYPOTHESIS — do not repeat as fact

- No specific GPU race has been proven. The producer/consumer pair was never localized.
- `GenerationTask.isCompleted` is a CPU-side flag with no GPU completion semantics
  (`ProgressiveTask.cs:40`, set by `MarkCompleted()`). This is a real contract weakness,
  but an explicit final fence does not fix the anomaly, so it is **not** the root cause.
- The per-batch frequency swing (0 % to 100 %) has no explanation.

## Why there is no local fix or workaround

- **A minimal synchronization fix** was the only candidate with evidence behind it. It
  was tested and refuted.
- **An output validator with retry** requires an invariant that separates the two worlds
  without encoding today's terrain. Three were tried:
  - per-tile maximum height: separates perfectly (gap ~36 m) but is as brittle as a hash;
  - a global maximum threshold: fails — the lowest anomalous tile max (321.2 m) is below
    the highest canonical one (357.1 m);
  - the island's contract ceiling `24 + 140 × 0.70 = 122.0 m`, measured as the share of
    dry land above it: fails — canonical tile 0_0 gives 28.2 %, anomalous 0_1 gives 14.0 %,
    because the massif legitimately lifts land above the island's ceiling and its footprint
    differs per tile.
  - A fourth idea — cells **outside the massif's declared footprint** above 122.0 m —
    could not be evaluated: the Mountain biome's transform is not recorded in any log and
    the spawned LPB is not persisted in the scene. Reconstructing the footprint would mean
    inventing it.
- Modifying the Vista package permanently was ruled out.

## Status

**P0 — OPEN / EXTERNAL DEPENDENCY.** Escalated to the vendor. No local workaround
accepted. See `VISTA_BISTABLE_BUG_REPORT.md` for the vendor-facing draft.

## Frozen production baseline

```
IslandPlateauGraphBuilder.cs   0e9bc4a6c53e1f4c
VistaGraphBuild.cs             bbe68210a8554d59
OverworldTerrainMenu.cs        2930a734229c6c21
OverworldBatchDiagnostics.cs   84dcd39de2f8b487
Vista package                  git diff = 0, no untracked modifications

graph        beds 3, modulation 3, knee 1, clamp 1
base         Levels [0.05, 1.00], worldHeight 140
geology      10 / 12 / 14 m, warp 0.03, seed 8821, offsets 0 / 450 / 900 m
clamp        max 1.0000
micro erosion  rate 0.0, deposition 0.1
macro erosion  rate 10.0, deposition 0.3
```

## Evidence corpus

Session scratchpad, nothing moved or renamed. Complete four-tile 16-bit dump sets:

```
CANONICAL_REFERENCE      24 runs   (determinism/, ordered/, hunt/, pass/, bakecycles/)
ANOMALOUS_REFERENCE       8 runs   (hunt/run3, run7, run9; pass/run1, run2, run8;
                                    bakecycles/run1; ordered/run1)
THIRD_STATE_REFERENCE    14 runs   (capture/, fp/)  — probe-perturbed state cdb816ac2e2e
```

Plus 108 bake logs across `hunt`, `pass`, `bakecycles`, `determinism`, `ordered`,
`fenceab`, `poolab`, `capture`, `fp`, `trigger`, `matrix`.

## Working rule until the vendor responds

A single full-stack bake is not evidence. Any full-stack measurement must either be
repeated until the tile hashes match `CANONICAL_REFERENCE`, or be reported for both
states. Island-only bakes have been stable and are the preferred measurement surface.
