# Terrain banding — investigation plan

**Status:** Active. Goal is to end this defect, not to reduce it again.
**Evidence so far:** [VISTA_TERRAIN_PLAYBOOK.md](VISTA_TERRAIN_PLAYBOOK.md) §2.4a–2.4d
**Related:** [ISLAND_SHAPING_REBUILD.md](ISLAND_SHAPING_REBUILD.md), tech debt items 9 and 10a

---

## Where we actually are

Two separate phenomena were being measured as one, and only one of them is solved.

| | Status |
|---|---|
| **A. One dominant wavelength** — the island had no spectral content below ~100 m, so its last surviving octave *was* its texture | **Solved.** The reference's third noise (scale 200, persistence 0.728) was missing. Periods moved from 101/186 m to 33/52/68 m and now differ per tile; the autocorrelation stencil floor went negative, meaning real cell-scale detail. |
| **B. East-west anisotropy** — curvature ratio 0.39–0.59, same direction on every tile | **Open.** Survived all nine experiments so far. |

Everything below is about **B**.

## The mistake to avoid repeating

Nine experiments were run against B by changing one graph parameter at a time. All either did
nothing or reduced the number by removing relief. That approach has now been falsified as a method,
not just in its individual answers.

Two specific errors to carry forward:

1. **The instrument measures landform and texture together.** Mean |∂²h/∂x²| over a whole tile
   includes the island's own dome. An island elongated east-west produces exactly the observed
   ratio *by shape alone*, with no artifact anywhere. Every anisotropy number recorded so far is
   suspect for this reason.
2. **The control was confounded.** Vista's sample graph was called "clean", but it differs from ours
   in *two* ways at once: a different shaping recipe **and** no island silhouette — it covers the
   whole square with falloff 0. Its isotropy may be entirely due to the second.

## RESULTS — steps 1 and 2 are done, and both hypotheses were wrong

**Step 1 (instrument) — built, and it falsified its own premise.** The dump now reports a residual
ratio from the height field with a 40 m box blur subtracted. On the island it tracks the plain ratio
almost exactly:

| Tile | plain | residual |
|---|---|---|
| a | 0.54 | 0.56 |
| b | 0.39 | 0.38 |
| c | 0.51 | 0.51 |
| d | 0.59 | 0.60 |

So the anisotropy is **not** the island's dome leaking into the measurement. It is in the ridge
texture itself, and every number recorded in the earlier experiments was valid after all.

**Step 2 (confound) — the mask is innocent; it is the graph.** Our island graph baked with a square
world-covering footprint and falloff 0 — the demo graph's exact configuration — is not merely still
anisotropic, it is **more** so:

| | residual ratio, 4 tiles | axis-aligned share |
|---|---|---|
| with silhouette | 0.56 / 0.38 / 0.51 / 0.60 | 17–22 % |
| **without silhouette** | **0.45 / 0.34 / 0.36 / 0.40** | **22–30 %** |

And the orientation histogram sharpens into a spike: bins 5–6 (75–105°) carry 21–25 % each against
~5 % elsewhere. Gradients pointing along Z means slopes facing north–south, which means **ridges
running east–west** — now unambiguous rather than inferred.

Two candidates die with this result: the elongated anchor polygon, and the `reach = Lerp(0.72, 1, …)`
asymmetry built into `BuildIslandSilhouette`. **Step 3 is therefore skipped.** Go to step 4.

One more candidate can be crossed off from data already collected: **biome aspect ratio**. The massif
biome is 1050 × 450 — far more elongated than the island — and bakes *less* anisotropic (0.63–0.91)
than the island alone. Stretching a square canvas over a non-square biome is not the mechanism.

---

## RESULTS — step 4: it is the NoiseNode settings

**The raw noise product is anisotropic on its own.** Baked with no silhouette *and* erosion
bypassed — leaving only `noise x noise x noise -> Levels -> ShelfCurve` — the ratio is 0.37–0.50 with
the same 75–105° orientation spike. `Levels` and `CurveNode` are pointwise and cannot create
direction, so the anisotropy is produced inside `NoiseNode`.

Noise **scale** is not the lever: setting the island to the reference's 750/1000 made it *worse*
(0.28–0.49 against 0.34–0.45). Reverted.

**And the comparison that drove this whole investigation was between three different graphs.** The
graph measured clean is `Demo_MountainRange_AsIs` (from `Samples/Graphs/MountainRange.asset`). The
parameters quoted throughout the playbook as "the reference's own values" were read from
`Mountain_TerrainGraphTemplate`, which was never baked or measured. They are not the same graph:

| | Our island | `Demo_MountainRange` (measured clean) | `Mountain_TerrainGraphTemplate` (never measured) |
|---|---|---|---|
| Noise nodes | 3 | 2 | 3 |
| Octaves | 8 | **4** | 8 |
| Persistence | 0.382 | 0.333 | 0.382 |
| `flipSign` | **true** | **absent** | true |
| `applyRemapPerLayer` | **true** | **absent** | true |
| Erosion node | `HydraulicErosionNode2` | `HydraulicErosionNode` | `HydraulicErosionNode` |

The two settings our documentation insists on — `flipSign` and `applyRemapPerLayer`, described as
"what gives the reference its ridged character" — are serialized **true** in the template we copied
and **absent** from the only graph that measured clean. That is consistent with the one earlier
result that moved the number: `layerDerivativeMode = Pow` improved it to 0.65–0.92, and `Pow` is what
an absent field deserializes to.

### Both named suspects tested — and the second one named the real mechanism

| Test (no silhouette + erosion bypassed) | Residual ratio | Orientation peak |
|---|---|---|
| baseline | 0.37 / 0.50 / 0.50 / 0.40 | bins 5–6 (75–105°) |
| `applyRemapPerLayer` off | 0.38 / 0.36 / 0.51 / 0.50 | bins 5–6, unchanged |
| **`flipSign` off** | 0.37 / 0.39 / **0.74 / 0.58** | **bins 0 and 11 on two tiles — rotated 90°** |

`applyRemapPerLayer` is not the cause: nothing moved.

`flipSign` is not the cause either, but it did something far more informative — it **rotated the
preferred direction**. Two of four tiles flipped from east–west ridges to north–south ones, and the
curvature magnitude collapsed by an order of magnitude (|d2x| 0.09–0.41 → 0.022–0.026).

A setting that *rotates* an anisotropy is not creating it. It is selecting which axis an anisotropy
that is already there lands on. That points at the one thing common to every octave regardless of
settings: **Perlin gradient noise on a square lattice**. Stacked octaves on a shared lattice align to
its axes, and `flipSign` decides which.

### Domain warp tested — helps, does not solve

`warpMode = Angular`, `warpIntensity 1`, same clean configuration: **0.49 / 0.62 / 0.48 / 0.58**
against a 0.37 / 0.50 / 0.50 / 0.40 baseline. A real improvement of roughly +0.1, and the orientation
peak stays in bins 5-6. Worth keeping in mind as a partial mitigation, but it is not the mechanism,
and a higher intensity was not tried.

### The strongest signal in all the data is octave count — and it is still untested

Ranking every experiment by how much it moved the ratio:

| Change | Ratio | Note |
|---|---|---|
| `layerDerivativeMode = Pow` | **0.65 – 0.92** | best result of the whole investigation |
| domain warp Angular | 0.49 – 0.62 | modest |
| `flipSign` off | 0.37 – 0.74 | rotates rather than removes |
| `applyRemapPerLayer` off | 0.36 – 0.51 | nothing |
| reference noise scales | 0.28 – 0.49 | worse |

`Pow` divides scale and amplitude by `lacunarity^i` at *every* step, so only the first two or three
octaves contribute anything — it is, in effect, an octave-count reduction. And the one graph that
measured isotropic uses **`layerCount = 4`**, not 8.

That is the same variable arriving from two independent directions, and nothing has tested it
directly. **Next: `layerCount = 4` on the detail and fine noises, clean configuration.** If the ratio
lands near `Pow`'s 0.65-0.92 while keeping the amplitude that `Pow` throws away, the mechanism is
octaves stacking on a shared lattice and the fix is to stop at four.

### Octave count tested — falsified, and it reframes the whole table

`layerCount = 4` on the multi-octave noises, clean configuration: **0.23 / 0.26 / 0.32 / 0.32**
against a 0.37 / 0.50 / 0.50 / 0.40 baseline, with the axis-aligned share rising to 24–36 %.

**Fewer octaves make it worse, not better.** The prediction was the opposite, and it was wrong.

That forces a re-reading of the one result everything was hanging on. `Pow` was treated as "octave
count reduction under another name" — it is not. Both change how many octaves matter, but they
change the *scale progression* differently:

| | Octave scales from 1200 m |
|---|---|
| Linear (ours), 8 octaves | 1200, 533, 237, 105, 47, 21, 9.3, 4.1 — evenly spaced, ratio 2.25 |
| Linear, 4 octaves | 1200, 533, 237, 105 — same spacing, high frequencies simply gone |
| **Pow** | 1200, 533, 105, 9.2, 0.18 — ratios 2.25, 5.1, 11.4, 51: **rapidly diverging** |

Cutting to four keeps the even spacing and removes the fine detail; the ratio worsens. `Pow` keeps
widely separated scales; the ratio improves. So the variable that matters is not *how many* octaves
but **how their scales are spaced** — evenly spaced octaves on a shared lattice reinforce its axes,
widely separated ones do not.

### What to test next, given that

Change the spacing without changing anything else: **raise `lacunarity`** from 2.25 toward 3–4 on the
multi-octave noises, keeping 8 octaves and `Linear`. That separates the octaves the way `Pow` does
but without collapsing amplitude, which is what made `Pow` unusable.

If a higher lacunarity reaches Pow's 0.65–0.92 while keeping the relief, that is the fix.

### Superseded next-test note

`NoiseNode` has `warpMode` (None / Angular / Directional) with `warpIntensity`, and **every noise in
this project uses None** — as does the graph that measured isotropic. Domain warping is the standard
remedy for lattice-aligned gradient noise: it displaces the sample position by another noise field,
so features stop following the lattice.

Our own playbook rules it out on cost grounds — *"Warp is the most expensive knob on the node, and
the plateau/massif profile comes from the shaping curve"* — a decision made about performance that
was never tested against this defect.

Test `warpMode = Angular` at a modest `warpIntensity` on the no-silhouette + erosion-bypassed
configuration. If the ratio moves toward 1.0 and the orientation histogram flattens, that is the
answer, and the remaining question is only what it costs per bake.

### Earlier next-test note, now superseded

Disable `applyRemapPerLayer` (and its remap curve), then `flipSign`, one at a time, on the
no-silhouette + erosion-bypassed configuration — the cleanest signal available. Expect the ratio to
move toward 1.0 if either is the cause.

If it is `applyRemapPerLayer`: the per-octave curve clips the bottom 10 % of every octave before the
sum, which is a strongly non-linear per-octave operation and the most plausible way an isotropic
Perlin field acquires a preferred direction.

**Do not treat the playbook's noise-defaults rule as settled while this is open.** It was written
from a graph nobody measured.

---

## Step 1 — Fix the instrument before running any more bakes

Until this is done, no result about B means anything.

Add to `OverworldTerrainDiagnostics` a **high-passed** anisotropy measure: subtract a blurred copy of
the height field (box blur, radius ~40 m) and compute the curvature ratio on the residual. That
separates "ridges run east-west" from "the island is wider than it is tall".

Report both numbers side by side. Acceptance for this step: on the demo graph, which has no
silhouette, both numbers should agree; on our island they should diverge if the dome is what the
old number was seeing.

## Step 2 — Break the confound

One bake, and it splits the remaining space in half.

**Bake our island graph with no silhouette**: `SnapBiomeToTilesOnSpawn` instead of
`SetBiomeAreaOnSpawn`, falloff 0, exactly the seabed's configuration, so the biome covers the whole
square like the demo graph does.

| Result | Conclusion |
|---|---|
| Anisotropy gone | It is the **mask**, not the graph — the elongated polygon and its falloff. Go to step 3. |
| Anisotropy remains | It is the **graph**. Skip to step 4. |

## Step 3 — If it is the mask

The visible surface is `mask × graphHeight`. Multiplying by an elongated mask imposes that
elongation on every derivative, and our silhouette is *deliberately* asymmetric:
`BuildIslandSilhouette` uses `reach = Lerp(0.72, 1, cliffFacing)` about a seeded yaw, so one side
reaches 28 % less far than the other by design.

Tests, in order, each one bake:

1. **Circular silhouette** — force `reach = 1` and drop the harmonics. If anisotropy goes, the
   asymmetry is the cause and the question becomes how much asymmetry is acceptable.
2. **Falloff sweep** — 180 m (current) vs 60 m. The falloff ring is where `mask × height` has its
   strongest gradient; a narrow ring confines it.
3. **`SoftFalloff_BiomeMaskGraph`** — never examined. Read it. It shapes the mask edge and nothing
   in this investigation has looked at it.

If the answer is "the island's shape is the anisotropy", then **B is not a defect** and the item
closes as *by design* — with the measurement recorded so nobody reopens it.

## Step 4 — If it is the graph

Remaining differences from the reference, now that the third noise is in:

| Ours | Reference |
|---|---|
| detail 1200 m / mass 1900 m | 750 m / 1000 m |
| `ShelfCurve` (hand-drawn plateau bias) | none |
| `HydraulicErosionNode2` ×2 | `HydraulicErosionNode` ×2 — **a different node** |

The erosion node type is the one difference never tested; erosion *bypass* was tested, which is not
the same thing as erosion *implemented differently*. Test in this order: scales to the reference's,
then erosion node type, then the shelf curve.

## Step 5 — Compensate the height loss

Independent of B, and needed either way. Adding the third noise multiplied a third bell-shaped
distribution into the product and cost peak height 115 m → 77 m. Either raise
`SetBiomeHeightOnSpawn.worldHeight` on `LPB_IslandBase.prefab`, or blend the fine noise at reduced
opacity instead of multiplying it at full strength.

Do this **after** B is settled, so it does not confound the measurements.

## Step 6 — Re-tune vegetation

Tech debt item 10a. The grass floor is at 0.30 to keep grass off sand; the fragmentation it causes
is B surfacing through the surface weights. Once B is resolved, drop the floor to ~0.20 and
re-measure the density map.

---

## Rules for this investigation

- **One variable per bake.** Nine experiments were lost to changing a parameter and a hypothesis at
  the same time.
- **Record every negative result** in the playbook table with its numbers. The four that are already
  there are worth more than the fixes.
- **A change that reduces the number by removing relief is not a fix.** `Pow`, the shelf curve
  removal and the iteration sweep all did that.
- **Compare against a control that differs in exactly one way.** That is the error that cost the
  most here.
