# Island Shaping Rebuild — open iteration

**Status:** Not started. Diagnosis complete, fix is an art decision.
**Created:** 2026-08-17
**Evidence:** [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md) §2.4a–2.4d (every number below is recorded there with the run that produced it)
**Applies to:** `Assets/Scripts/Editor/IslandPlateauGraphBuilder.cs`, `VistaGraphBuild.AddDetailNoise` / `AddMassNoise`

---

## The problem, stated precisely

The overworld island shows directional banding: ridges consistently elongated east-west, with a
repeating ~101 m / ~186 m spacing north-south. Measured, not eyeballed — `Dump Terrain Geometry`
reports a curvature ratio of **0.57–0.71** on all four tiles (1.0 would be isotropic) and Z-period
peaks above the noise floor.

Vista's own sample graph, baked over the same tiles through the same pipeline and measured by the
same code, reports **0.84 / 0.87 / 0.97 / 1.50** with every period peak below the noise floor. That
is the target, and it proves the target is reachable on this pipeline.

---

## What has already been ruled out — do not re-test these

Eight experiments, each a full bake. All recorded with numbers in the playbook.

| Hypothesis | Verdict |
|---|---|
| Erosion sim clamped by the 512 px cap | **Dead.** Macro asks 313 px, micro 418 px; nothing clamps. |
| Biome-canvas facets showing through | **Dead.** Canvases are 5.01 / 3.19 / 10.94 m/px; every real peak is 20–60x coarser. |
| The massifs' `Raise` blend | **Dead.** Banding is present *and stronger* with `IncludeMountains` off. |
| Erosion under-iterated (200 → 500) | **Dead.** Every number moved < 0.02. |
| Erosion at all (nodes bypassed) | **Dead.** Banding survives bypass and gets slightly stronger. |
| 16-bit heightmap quantisation (700 m budget) | **Dead.** 400 m budget did not move the floor as predicted and made the ratio *worse*. |
| The biome→tile resample, or the measurement itself | **Dead.** Vista's sample graph is clean through the identical path. |
| One mis-set shaping parameter | **Dead.** See below — every lever helps partially, none fixes it. |

## What was found instead

Two levers move the number, both by removing relief rather than by removing an artifact:

| Change | Ratio (4 tiles) | Peak height |
|---|---|---|
| baseline, island only | 0.57 / 0.59 / 0.67 / 0.71 | 115 m |
| `layerDerivativeMode = Pow` | 0.65 / 0.78 / 0.89 / 0.92 | 112 m |
| `ShelfCurve` → identity | 0.64 / 0.65 / 0.76 / 0.78 | 94 m |
| Vista sample graph (target) | 0.84 / 0.87 / 0.97 / 1.50 | — |

`Pow` collapses scale and amplitude by `lacunarity^i` at every step, so only 2–3 of 8 octaves
survive — it deletes the detail that carried the banding. `ShelfCurve` is what makes the island a
plateau; removing it costs 21 m of relief and the plateau character.

**The banding is emergent from the shaping recipe as a whole**, which is why every single-cause
hypothesis died and every single-parameter sweep returned a partial improvement proportional to how
much high-frequency content it destroyed.

---

## What the work actually is

Re-derive the island's shaping stage from Vista's reference graph instead of from our own recipe.
Today's recipe is:

```text
Noise(scale 1200, 8 oct, persistence 0.382, flipSign, applyRemapPerLayer (0.1,0)->(1,1))
  x Noise(scale 1900, 1 oct)          <- Combine(Multiply)
  -> Levels(outLow 0.05)
  -> ShelfCurve (hand-drawn plateau bias)
  -> HydraulicErosion2 x2
```

Only the erosion pair is known-good (it is the reference's, and bypassing it made things worse). The
noise pair, the multiply, and the curve are ours.

Open questions the rebuild has to answer, in this order:

1. What does the reference actually do between its noise nodes and its erosion, node for node?
   Read `Mountain_TerrainGraphTemplate` and `Samples/Graphs/MountainRange.asset` directly — the
   Session Viewer shows every node's output as an image.
2. Can the plateau character survive that recipe, or does a plateau island fundamentally need a
   distribution bias? If it does, the bias has to be introduced somewhere that does not compress the
   band the relief lives in.
3. Does the answer change the island silhouette or only its surface? If the silhouette moves, POI,
   settlement and road placement all shift with it.

**This changes how every island on every seed looks.** Agree the target look with the user before
building, and show a bake before committing — not a diff.

---

## Verification, and the tooling that already exists

Everything needed is built and permanent:

```
Unity.exe -batchmode -projectPath <project> \
  -executeMethod GameDev.EditorTools.OverworldBatchDiagnostics.BakeAndDump \
  -overworldSeed 110201 -overworldMountains false -overworldRebuildGraphs true \
  -overworldExportPng true -logFile out.log
```

Never pass `-nographics` (Vista's compute shaders silently produce a flat world) and never pass
`-quit` (it kills the bake mid-coroutine). Both traps are documented in the playbook §2.4b.

Accept the rebuild when, on **three seeds**:

- curvature ratio is within roughly 0.85–1.20 on every tile, with no consistent direction across
  tiles;
- no Z- or X-period peak sits above the stencil floor;
- the axis-aligned orientation share stays near the 17 % even-spread baseline;
- the curvature PNG shows branching drainage rather than scan-lines;
- peak height and dry-land fraction stay in the range the settlement and road systems were tuned
  against (~115 m island-only, ~330 m with massifs, 26–52 % land per tile);
- vegetation survives on all four tiles (`Dump Vegetation State`).

## Unrelated defect found in the same images — cheap, do it any time

The coastline breaks into visible straight chords: `OverworldLpbBake.IslandAnchorCount` is 36 for a
~2.2 km island, so each anchor spans ~60 m of shore and the Chaikin smoothing cannot hide the
polygon. Raising the count is a one-constant change with no interaction with the banding above.
