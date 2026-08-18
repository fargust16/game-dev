# Vista reference baseline — the numbers every step is compared against

**Measured:** 2026-08-18, seed 110201, `OverworldBatchDiagnostics.BakeAndDump -overworldSkipBake true`
**Subject:** `VistaManager -> MountainBiome (Cloned from Template)` in `Assets/Scenes/Overworld_Generator.unity`,
created through `3D -> Vista -> Wizard` (2x2 tiles, 1400 m tile, biome = Mountains).
**Source graph:** `Assets/PinwheelStudio/Vista/Personal/BiomeTemplates/Mountain/Mountain_TerrainGraphTemplate.asset`
**Images:** [VistaReferenceBaseline/](VistaReferenceBaseline/) — height, slope and curvature per tile.

This terrain has no banding. It is the target for the additive rebuild, and the only reason the
numbers below matter is that they let a regression be caught at the step that caused it instead of
five steps later.

---

## Why this file exists

Fifteen experiments tried to remove the banding by changing our own graph, and every one failed.
Two were briefly reported as successes because destroying the relief also hides the artifact — a
clipped mesa reads as "dark and smooth" on a slope PNG and as a palisade in the scene.

So: measure the known-good reference first, add one thing at a time, and compare after every step.
A metric that regresses names the step that broke it.

---

## Per-tile geometry

Tiles are `Terrain_*` (wizard-created), 1400 x **500** x 1400 m. Note the 500 m height budget —
the overworld pipeline uses 700 m, so vertical scale is not directly comparable.

| Tile | Height range | Residual ratio | Axis-aligned share | Flat runs X / Z |
|---|---|---|---|---|
| `Terrain_1_0` | 25.0 – 317.8 m | **0.99** | 16 % | 1.05 / 1.07 |
| `Terrain_0_0` | 24.6 – 204.3 m | **1.02** | 18 % | 1.04 / 1.04 |
| `Terrain_0_1` | 25.0 – 327.7 m | **0.96** | 19 % | 1.10 / 1.08 |
| `Terrain_1_1` | 24.6 – 212.3 m | **0.95** | 19 % | 1.21 / 1.20 |

Mean residual ratio **0.98**. Even spread is 17 %; 1.00 is isotropic.

For contrast, our island graph measures **0.699** mean over ten seeds, with the orientation spike in
bins 5–6 and a 101 m Z-period sitting above the noise floor.

## Periodicity — every peak is far below its stencil floor

The floor is printed with each scan; a peak below it is noise, not structure.

| Tile | Strongest X peak | Strongest Z peak | Floor X / Z |
|---|---|---|---|
| `Terrain_1_0` | 8.2 m, r=0.105 | 8.2 m, r=0.087 | 0.377 / 0.355 |
| `Terrain_0_0` | 8.2 m, r=0.033 | 32.8 m, r=0.033 | 0.274 / 0.271 |
| `Terrain_0_1` | 8.2 m, r=0.117 | 8.2 m, r=0.067 | 0.394 / 0.330 |
| `Terrain_1_1` | 8.2 m, r=0.088 | 73.8 m, r=0.029 | 0.352 / 0.262 |

Nothing is close. The strongest peak anywhere reaches 30 % of its floor.

## Orientation histograms, 12 bins x 15 deg

```
Terrain_1_0   8  8  9 10  9  8  9  9  8  8  8  8
Terrain_0_0   9 10 10 10 10  9  9  8  7  6  6  6
Terrain_0_1  12 11  8  6  6  6  7  8  8  9  9 11
Terrain_1_1  13 10  8  7  6  6  6  7  8  8 10 11
```

Flat within a few points. No bin dominates.

## Distribution and lattice

| Tile | p50 | p95 | max | mean | Distinct heights |
|---|---|---|---|---|---|
| `Terrain_1_0` | 0.1461 | 0.4902 | 0.6357 | 0.1892 | 7.13 % |
| `Terrain_0_0` | 0.0963 | 0.2822 | 0.4086 | 0.1210 | 4.32 % |
| `Terrain_0_1` | 0.1179 | 0.4653 | 0.6554 | 0.1733 | 7.00 % |
| `Terrain_1_1` | 0.0897 | 0.2887 | 0.4246 | 0.1203 | 4.50 % |

`p95 < max` on every tile — the signal never saturates against the ceiling. This is the check that
would have caught the flipSign/applyRemapPerLayer experiment immediately: there `p75 = p95 = max`,
a quarter of the island pinned flat, which is what produced the palisade.

Height lattice is the usual Unity 16-bit store (median step 1/32768 = 0.015 m at 500 m). Flat runs
of 1.04–1.21 cells confirm no terracing; 1.0 means every cell lands on a new level.

## Canvas resolution

| Surface | Resolution |
|---|---|
| Tile heightmap | 513 px over 1400 m -> 2.73 m/px |
| `MountainBiome` canvas | 512 px over ~4700 m -> **9.18 m/px** (falloff 50) |

Our island LPB runs 512 px over ~2566 m -> 5.01 m/px, so the reference works at roughly **half** our
canvas density and still resolves cleanly. Whatever causes our banding, a coarser canvas is not it.

## What the curvature images show

Branching dendritic drainage across the whole tile — see
[VistaReferenceBaseline/tile1_Terrain_0_0_curvature.png](VistaReferenceBaseline/tile1_Terrain_0_0_curvature.png).
No scan-lines, no directional preference. Our equivalent images show a dense axis-aligned hatch.

---

## Regression thresholds for every later step

A step fails and gets reported if any of these trips:

- residual ratio falls more than **0.10** below 0.98 on the tile mean;
- any X or Z period peak rises **above its printed stencil floor**;
- axis-aligned share rises more than **5 points** above the 16–19 % band;
- `p95` reaches `max` on any tile (saturation against the biome ceiling);
- flat-run mean exceeds **1.5** cells (terracing).

Numbers alone are not sufficient — every step is also looked at in the scene. That is the lesson
that cost two days.

## How to re-measure

```
Unity.exe -batchmode -projectPath <project> \
  -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump \
  -overworldSeed 110201 -overworldRebuildGraphs true -overworldExportPng true -logFile step-N.log
```

Never `-nographics` (Vista's compute shaders silently produce a flat world) and never `-quit` (it
kills the bake coroutine). Close the Unity editor first — an open editor holds the project lock and
the batch run exits immediately with code 1 and a 28-line log.
