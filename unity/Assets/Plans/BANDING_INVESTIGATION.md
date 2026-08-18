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
