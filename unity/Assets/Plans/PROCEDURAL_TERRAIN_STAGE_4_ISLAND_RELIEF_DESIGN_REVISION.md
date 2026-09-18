# Procedural Terrain — Stage 4A Island Relief: Design Revision R1

**Status:** Change record. **Implemented dump-first 2026-09-16** in `IslandRelief*.cs`,
`IslandElevation.cs`, `TerrainGenIslandReliefDump.cs`, `TerrainGenStage4ATests.cs`. Folded into
[PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md).
Stage 4A stays disconnected from `TerrainAuthorityField`.
**Date:** 2026-09-15 (implementation 2026-09-16)
**Revises:** [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md)
(“the design”, variant C6). Section numbers `§n` without a file name refer to *this* document; `D§n` to the design.
**Baseline:** HEAD `b677f26`; the Stage 4A C6 implementation was read, then R1's §18 delta applied.

Claim tags:

- **[FACT]** read from code;
- **[MEASURED-U]** measured by the Unity dump of the C6 implementation (`dump3_final`, 7 canonical seeds);
- **[MEASURED-H]** measured by the offline harness (below);
- **[DERIVED]** follows mathematically from the code and the stated definitions;
- **[PROPOSAL]** decision of this revision;
- **[OPEN]** deliberately unresolved, with an owner.

**Evidence base.** An offline .NET 8 harness (scratch only, not in the repository) references the compiled
`Assembly-CSharp.dll` and builds the real Stage 1 → 2B → 3A–3E chain. Its clone of `IslandRelief` hashes
**bit-identical** to the real `IslandRelief` on all 7 canonical seeds in the same runtime, and its metrics
reproduce the Unity dump (1337: 0.67 m / 1 555 m², 777: 0.29 m / 1 323 m²; all other dump numbers equal to
rounding). Mono and .NET differ in the last ULP of some libm calls, so harness *hashes* are not Unity
hashes; harness *metrics* are. On top of the 7 canonical seeds the harness ran a **non-canonical sweep of 40
seeds (1000–1039, 32 massif worlds, 8 no-massif worlds; every seed built)**. The sweep is the most important
new evidence in this revision: the canonical set is too small to show the S8 failure rate.

---

## Short answer

1. **The C6 implementation is faithful to the design; the failures are design-level.** Nothing below is
   fixed by tuning the implementation.
2. **S8 (closed depressions).** Root cause: Stage 1's profile is smoothstepped per span, so `H1'` is exactly
   zero on the rings `d = 30, 100, 200, 440, 800 m`, and *any* Stage 4A term whose value decreases inland at
   such a ring digs a trough of depth ≈ `g²/(2κ)`. Laterally extensive terms (plate edges, junction
   relaxation, apron tail) close the trough. The design's rule “all terms ≥ 0 removes ring lips” is false.
   C6 has depressions on **2/7 canonical and 15/40 sweep seeds (up to 2.32 m)**. Gates (A, C) cannot
   guarantee anything (proof §3.1); edge widening (B) only moves pits (15/40 → 14–15/40).
   **Selected fix: an inland slope budget τ** (§4): a small monotone coastal-floor term `M(d)` that gives
   `H1 + M` an inland slope of at least `τ = InteriorRise / InteriorScale = 0.05`, plate edge widths and
   junction radii derived from τ, and knoll amplitudes capped by the budget left over. This makes
   `H1 + M + Γ·(P + K)` **non-increasing towards the sea along every coast-normal segment — a proof, not a
   tuning** — so no depression can form outside the massif influence zone and its lee. Measured: canonical
   0/7; sweep 15/40 → **6/40, all six with apron or massif uplift at the deepest cell**; plate-class pits
   (no massif, no apron) 7 → 0.
3. **S10 (apron).** Two independent mechanisms: (1) the coastal gate `Γ` rising along inland rays — not a
   defect of the apron surface; (2) the apron's outer reach running past the massif axis reach, where
   `MountainMassif.Project` is no longer unique (s jumps 0.993 → 0.744). **Fix:** effective reach
   `W_eff = clamp(R_axis − δ − toe, 0, W)` with height scaled by `W_eff/W`; the invariant is on the
   **ungated physical apron surface**: exactly 0 beyond `R_axis − δ`, non-increasing along the projection's
   own normals. Measured: beyond-domain 2.2–3.6 m → 0; ray non-monotonicity 0.39–3.01 m → ≤ 0.044 m (test
   ray construction, not the surface).
4. **S11 trajectories.** R4A adverse *sum* is not a cut depth. The physical budget is the **single climb of
   R4A ≤ `CorridorIncisionMeters` (7.2 m)**, plus a **theorem**: because `R4A ≥ 0`, the incision *below
   Stage 1* that Stage 3F needs along any trajectory can never increase (measured max increase 0.000 m on
   all seeds). Plate + apron adverse ≤ 1.5 m is kept. 20260905 V8 fails it in C6 (1.77 m) not because of
   district ranking or a junction but because the H1-based gate amplifies a headland climb that is already
   in H2; the lowland reservation (§9) sets that district to level 0 (→ 0.23 m).
5. **S11 outlets.** “R4A non-increasing downstream” is the wrong quantity. New invariant, compatible with
   Stage 3F carving against H4A: corridor end below outlet on H4A (hard), no increase of below-Stage-1
   incision downstream (hard, theorem), rim-margin non-reversal **after** the Stage 3 carve at the outlet
   (hard, measured in the Unity dump), declared-outlet shift reported. The two C6 margin reversals
   (−0.5 m, −0.4 m) are the apron filling the valley mouth (apron 17.7 m at the outlet-run minimum vs 6.9 m
   at the rim minimum).
6. **Lowland/upland.** Whole-plate rank levels leave 20260905 at 5.6 % lowland / 76.8 % upland and 777 at
   8.1 %. **Selected: bounded rank with lowland reservation** (the lowest-scoring districts share level 0
   until they hold ≥ 20 % of land, the design's own lower bound). **K2 is not needed.**
7. **Knolls.** Knolls exceed the R4A adverse sum only by two separate knolls on one corridor (110201 V6:
   sum 8.04 m, single climb 5.20 m); a single knoll can never exceed 7.2 m by construction. No count,
   placement or kernel change is needed for S11. **For S8 knolls enter the slope budget** (amplitude cap)
   and are **excluded from the massif body**; this lowers knoll heights to ≈ 3–4 m (§11, §16 R3).
8. **Residual, stated honestly:** closed depressions *inside the massif influence zone and its lee*
   (massif foot facing inland onto a Stage 1 ring) remain on 6/40 sweep seeds (0.33–2.32 m) and are not
   guaranteed by Stage 4A; frozen H2 itself has such a depression on 1/40. Ownership: Stage 3F /
   Stage 4B massif-foot drainage **[OPEN]** (§16 R1). All 7 canonical seeds are clean.

---

## Contents

1. Current implementation assessment
2. S8 root cause
3. S8 alternatives
4. Selected S8 correction
5. S10 apron root causes
6. Selected apron correction
7. Correct S11 trajectory invariant
8. Correct S11 outlet invariant
9. Lowland / upland analysis
10. K2 decision
11. Knoll / trajectory interaction
12. Updated formula (with OLD / NEW / REASON / EXPECTED EFFECT per rule)
13. Updated API requirements
14. Updated acceptance thresholds
15. Expected effect on all 7 canonical seeds
16. Risks
17. What remains frozen
18. Exact implementation delta required

---

## 1. Current implementation assessment

**Verdict: CURRENT IMPLEMENTATION = NEEDS REVISION (design-level).** [MEASURED-U] The implementation matches
D§5–§7 formula-for-formula (point/lattice identity, determinism, frozen hashes and tile contract all pass:
S1–S7, S9). It fails S8, S10 and S11 for reasons that live in the design:

| Test | C6 result [MEASURED-U] | Design-level cause | Section |
|---|---|---|---|
| S8 depressions | 1337: 0.67 m / 1 555 m²; 777: 0.29 m / 1 323 m² | positive terms decreasing inland at Stage 1 zero-slope rings; D§12.2's first rule is false | §2 |
| S10 apron rays | 9–20 of 64 rays non-monotone per massif world, worst 3.01 m | (1) gated fill judged instead of the surface; (2) reach beyond the axis reach | §5 |
| S11 trajectories | 110201 R4A adverse 8.04 m; 20260905 plate+apron 1.77 m | adverse *sum* is not a depth; gate amplification of an H2 climb | §7 |
| S11 outlets | R4A rises downstream of outlets (quantity is a fill, not a flow path) | R4A monotonicity is not an outlet property | §8 |

C6 canonical metrics (harness = Unity dump):

| Seed | 70–110 m | p50 / p90 | LR med / p90 | spread | coast-aligned | < 5° | > 45° | reach | pits | lowland / upland | knolls |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 110201 | 4.6 → 24.7 % | 57 / 107 | 4.15 / 8.66 | 7.84 | 48.9 % | 47.3 → 33.9 | 6.04 → 5.58 | 92.34 → 92.80 | 0 | 29.4 / 54.7 | 12 |
| 424242 | 6.7 → 22.9 % | 57 / 120 | 4.85 / 9.52 | 7.90 | 51.0 % | 39.4 → 25.5 | 7.15 → 6.40 | 91.55 → 92.35 | 0 | 28.3 / 54.2 | 20 |
| 20260905 | 7.8 → 22.2 % | 63 / 153 | 5.91 / 10.73 | 8.94 | 61.6 % | 37.4 → 22.5 | 8.01 → 7.14 | 91.27 → 92.16 | 0 | **5.6 / 76.8** | 23 |
| 7 | 5.0 → 15.2 % | 53 / 111 | 4.93 / 9.24 | 9.25 | 56.7 % | 40.4 → 28.4 | 6.02 → 5.41 | 93.62 → 94.23 | 0 | 21.1 / 55.3 | 16 |
| 1337 | 0.2 → 24.8 % | 54 / 77 | 3.82 / 8.67 | 6.01 | 57.5 % | 57.8 → 46.8 | 0 → 0 | 100 → 100 | **1** | 25.8 / 67.6 | 18 |
| 256 | 0.1 → 27.0 % | 53 / 78 | 4.32 / 9.29 | 6.35 | 59.4 % | 53.6 → 40.8 | 0 → 0 | 100 → 100 | 0 | **15.1** / 60.5 | 18 |
| 777 | 0.0 → 25.2 % | 58 / 78 | 3.87 / 8.55 | 5.25 | 67.1 % | 59.3 → 48.1 | 0 → 0 | 100 → 100 | **1** | **8.1** / 56.3 | 19 |

**Visual assessment** [MEASURED-U dump images inspected: 1337 `02_h4a_shaded` + `08_structure`, 110201
`02_h4a_shaded`, 20260905 `02_h4a_shaded` + `08_structure`, 424242 `03_relief_total`; harness renders of the
selected variant: 1337, 110201, 20260905 height maps, 424242 and 777 level maps]:

- **Macro relief reads** on every seed: raised districts, open plains, the massif still the landmark; the
  70–110 m band is filled. Not a noise field.
- **Plate boundaries partly read as “painted”**: straight Voronoi bisector segments are visible as long
  straight contour runs (1337 centre, 110201 north) and the 450 m junction relaxation reads as a circular
  bend (1337). Wider τ-derived edges (§4) soften but do not remove the straightness **[OPEN → §16 R6]**.
- **Apron reads as a ring embankment** on 424242 (a thin continuous band of fill along the steep side in
  `03_relief_total`) and as concentric bands on 20260905; on 110201 the apron outline runs as straight
  lines parallel to the straight axis. Part of this is the beyond-reach tail (fixed in §6); the rest is the
  apron's constant-offset geometry **[OPEN → §16 R4]**.
- **Variation inside plates** comes only from knolls, which are nearly invisible at 5 m contours.
- **Plains / upland balance** is wrong on 20260905 (a single small lowland in the NE arm) and 777.
- **Future valleys / spurs**: nothing in C6 blocks Stage 3C corridors physically (§7), but the apron fills
  valley mouths at Stage 3E outlets (§8).

---

## 2. S8 root cause

### 2.1 Mechanism [FACT] + [DERIVED]

`CoastHeightModel.HeightAt` evaluates `ProfileCurve`s whose spans are each a smoothstep, so the Stage 1
profile has zero slope at every key:

| Ring `d_k` | H1 (m) | seaward curvature `κ⁻` = 6ΔH/L² | inland curvature `κ⁺` | max H1 slope in the inland span |
|---|---|---|---|---|
| 0 (shore) | 24.0 | — | 0.0173 | 0.13 |
| 30 | 26.6 | 0.0173 | 0.0143 | 0.25 |
| 100 | 38.3 | 0.0143 | 0.0070 | 0.18 |
| 200 | 50.0 | 0.0070 | **0.0014** | 0.084 |
| 440 | 63.5 | 0.0014 | **0.00076** | 0.069 |
| 800 | 80.0 | 0.00076 | 0 (flat interior) | 0 |

Two further zero-derivative facts coincide at `d = 200 m`:

- C6's gate `Γ = S((H1 − Sea)/26)` saturates exactly where `H1 = Sea + 26`, i.e. at 200 m, and by the chain
  rule `∂Γ/∂n = Γ'(H1)·H1'(d)` is zero at **every** ring for **any** gate that is a function of `H1`.
- Stage 2B's own uplift gate `S(d/200)` also saturates at 200 m [FACT `MassifUpliftModel`].

Let `n` be the Stage 1 inland direction (`∇d`). Just inland of a ring, `H1 ≈ h_k + ½κ⁺t²`. If a Stage 4A term
decreases inland there at rate `g`, the sum has a minimum along `n` at `t* = g/κ⁺` of depth `g²/(2κ⁺)`;
walking seaward from it climbs `g²/2·(1/κ⁺ + 1/κ⁻)`. With `κ⁺(200) = 0.0014`, a counter-slope of only
`g = 0.03` already gives a 0.3 m trough. A trough becomes a **closed** depression when the term that
produces it is laterally extensive and the ring curves back towards it (a bay-shaped ring tangent to a
straight edge). The design's rule “all terms ≥ 0 ⇒ no lips at the rings” (D§12.2 row 1) is therefore
**false**: positivity says nothing about the *inland derivative*.

### 2.2 The two canonical pits [MEASURED-H]

| Pit | Geometry | Components on a coast-normal profile through the bottom |
|---|---|---|
| 1337 (1419, 585), 0.67 m | plate edge exactly parallel to ring 100 (`cos(n, ∇Λ) = −1.00`), higher district seaward, ring bends seaward on both sides (sd along the tangent 110 → 123 / 186 at ±150 m) | plate 6.56 → 0.09 m over 100 m inland; `H4A` 44.32 (t = −10) → 43.86 (0) → 44.16 (+10): 1-D trough 0.46 m, 2-D fill 0.67 m; `g²/(2κ⁺)` with `g ≈ 0.08` predicts 0.46 m |
| 777 (1086, 2032), 0.29 m | oblique edge (`cos = −0.34`) across ring 200, knoll skirt on the seaward side, closed along the ring by level 0.99 → 0.67 on one side and ring curvature on the other | plate 18.44 → 18.14 and knoll 1.05 → 0.83 per 20 m; `g ≈ 0.025`, `g²/(2κ⁺) = 0.22 m` vs 1-D 0.24 m |

### 2.3 The sweep [MEASURED-H]

C6 on seeds 1000–1039: **15/40 seeds, 18 depressions, max 2.32 m.** Attribution at the deepest cell:

| Class | Pits | Signature | Where |
|---|---|---|---|
| P — plate edge / junction relaxation at a ring | 7 (6 seeds) | `U2 = 0`, `A ≤ 0.1`; broad gentle level gradient (e.g. 1033: 1.9 /km over a 450 m relaxation disc, 1.37 m, 13 212 m²) | rings 200 and 440, sd 218–476 |
| M — massif foot / apron | 11 | `A = 0.6–12.4 m` and/or `U2 > 0`; massif seaward of the pit, inland-facing foot or apron tail descending into a ring flat (1027: apron 16.6 → 0 m over 120 m inland, 2.32 m) | sd 221–678 |

Frozen H2 itself has one depression in the sweep (1019, 0.39 m, massif foot): Stage 2B is not
depression-free either. The canonical set happens to contain only two class-P pits.

**Design-level, not implementation-level.** Every measured pit follows from D§5–§7 as written.

---

## 3. S8 alternatives

All numbers [MEASURED-H]. “Sweep” = seeds with pits / pits / max depth on 1000–1039. Geography columns on
1337 (no massif, most sensitive) unless stated.

| # | Variant | Canonical pits | Sweep | 1337 70–110 / spread / aligned | Verdict |
|---|---|---|---|---|---|
| — | C6 | 1337 0.67, 777 0.29 | 15 / 18 / 2.32 | 24.8 / 6.01 / 57.5 | baseline |
| A1 | variant A: positive monotone gate, distance smoothstep over 200 m | 777 0.30 | — | 24.5 / 6.01 / 57.5 | `Γ' = 0` at 200 m again |
| A2 | variant A: distance linear over 200 m | 777 0.27 | — | 23.8 / 6.01 / 57.5 | kink at 200 m, ring 440 untouched |
| A3 | variant C: distance smoothstep over 450 m | 0 | — | **15.3 / 5.09 / 70.1**, knolls 18 → 5; massif worlds 70–110 −4…−7 pp | destroys relief |
| W1 | variant B: uniform edge half-width 165 m | 1337 0.30 | — | 24.7 / 5.85 / 55.1 | depth ∝ w⁻², no guarantee |
| W2 | uniform 220 m | 0 | 15 / 15 / 2.30 (new pit on 1000) | 24.6 / 5.67 / 53.8 | moves pits |
| W3 | uniform 330 m | 0 | — | 24.4 / 5.31 / 50.0 | — |
| B2 | variant B: counter-slope pair widening ×(1 + 2·score) | 0 | 14 / 15 / 2.28 (new pit on 1004) | 24.8 / 5.44 / 50.8 | moves pits |
| B4 | ×(1 + 4·score) | seed 7: 0.33 (apron at ring 440) | — | 24.8 / 5.01 / 52.6 | creates pits |
| D1 | variant D: plates as a shift of Stage 1's argument, `H1(d + s)` | 0 | — | 24.2 / 5.36 / 51.7, but **no-massif > 45° 0 → 0.21–0.65 %**, R4A climb 12.2 m > 7.2; with a gate slow enough to avoid cliffs 70–110 falls to 12.8 % | exact for plates, rejected on geography |
| D2 | variant D: apron only where its outward direction runs down Stage 1 | 110201 4.68, 424242 4.71, … | **28 / 60 / 8.04** | — | varying apron height along the axis creates closures |
| D3 | no junction relaxation | 1337 ×2, 777 | 13 / 14 / 1.51 | 23.5 / 8.30 / 64.2 | soft-Voronoi corners are steeper |
| D4 | apron domain fix only (§6) | = C6 | 14 / 17 / 2.32 | = C6 | S10 fix, not S8 |
| **D-τ** | **inland slope budget, τ = 0.05 (T05)** | **0** | **7 / 8 / 2.32** — class P eliminated | 27.3 / 5.52 / 55.8 | selected core |
| D-τ+ | τ + strict knoll budget + no knolls on massif body + domain-fixed apron + lowland reservation (**F2L, selected**) | **0** | **6 / 6 / 2.32, all class M** | 26.9 / 5.41 / 60.7 | **selected** |
| D-τ−A | τ, apron only inside the toe, strict knolls (F1) | 0 | 3 / 3 / 0.53 | 26.9 / 5.41 / 60.7; **massif worlds coast-aligned 70–78 % (target ≤ 65)** | best S8, fails geography |
| D-τ0 | τ, no apron at all | 0 | 2 / 3 / 1.45 | = D-τ; massif worlds aligned 62–74 %, 110201 ground climb +4.6 m | fails geography |

### 3.1 Why gates cannot guarantee (variants A and C) [DERIVED]

- Any gate `Γ(H1)` has `∂Γ/∂n = Γ'(H1)·H1'(d) = 0` at every ring, so it cannot offset a counter-slope there.
- A distance gate `Γ(d)` offsets a term `B` only if `Γ'/Γ ≥ −(∂B/∂n)/B`. On the low side of a plate edge
  `B → 0` while `∂B/∂n ≠ 0`, so the required `Γ'/Γ` is unbounded; for a level-0 lowland no gate exists.
  A gate slow enough to help at 200 and 440 m (A3) removes most coastal relief.

### 3.2 Why widening cannot guarantee (variant B) [DERIVED + MEASURED-H]

Trough depth scales as `(U·Δλ·1.5/(2w))²/κ⁺`. For a guaranteed 0.1 m at ring 440 with a full-contrast
coast-parallel edge the required half-width is 800–1 500 m (per-pair analysis on the canonical seeds: up to
1 234 m on 1337 P4|P9) — wider than the districts. Moderate widening lowers depths but shifts where edges
cross rings (sweep: new pits on 1000, 1004, 1012, 1014).

---

## 4. Selected S8 correction — the inland slope budget

### 4.1 Construction [PROPOSAL]

One budget `τ` (m of rise per m inland) is shared by every Stage 4A term outside the massif body:

1. **Coastal floor term.** `M(d) = ∫₀^d max(0, τ − H1'(u)) du` for `d > 0`, `M = 0` at and seaward of the
   shore. `H1 + M` then rises inland at ≥ τ everywhere on land. `M` is a function of `d` only, positive,
   monotone, point-evaluable (1-D table built once from the Stage 1 profile).
   `τ = TerrainGenSettings.InteriorRiseMeters / InteriorScaleMeters = 30/600 = 0.05` — the Stage 1
   interior's own mean gradient, the gentlest slope Stage 1 uses to say “rising inland” (≈ 2.9°, also the
   gradient of a real piedmont). Size [DERIVED]: `M(30) = 0.18`, `M(100) = 0.36`, `M(200) = 0.74`,
   `M(440) = 2.75`, `M(800) = 6.6 m`, then `+0.05 m/m`; land beyond 800 m is 0.00 % on all canonical seeds
   (max coast distance 569–753 m), so the interior tilt never applies there.
2. **Plate edges from the budget.** Pair half-width
   `w_kj = max(PlateEdgeHalfWidthMeters, 1.5·U·m·|λ_k − λ_j| / (2τ))`, so the steepest edge descent is ≤ τ.
   Canonical widths: 110–270 m (massif worlds), 126–378 m (no-massif worlds).
3. **Junctions certified.** Relaxation radius `R_J = max(JunctionRelaxRadiusMeters, 1.5·U·m·max_k|λ_k − λ̄_J| / τ)`
   and a **build-time certificate**: sample `−∂(Γ·P)/∂n` on a 5 m lattice inside `R_J + max w` around each
   on-land junction; if the maximum exceeds τ, scale that junction's radius and pair widths by the ratio and
   repeat (deterministic, ≤ 8 iterations, result stored). This replaces a closed-form bound the normalised
   soft-Voronoi product does not have. [MEASURED-H] without certification the plate term's inland
   derivative is ≥ 0 on 31/40 sweep seeds and reaches −0.001…−0.017 on 9 (none produced a pit); the
   multi-site blend is the source (without any relaxation 1033 reaches −0.022), and with the derived radius
   1033 goes from −0.026 to +0.003.
4. **Knolls inside the budget.** `a_i ≤ (τ − g_P,i) · σ_minor,i / 0.5726`, where `0.5726 a/σ` is the exact
   maximum directional slope of `a(1 − q/9)³` and `g_P,i` is an **analytic upper bound** of the plate term's
   inland descent over the knoll support (from the pair bands and certified junction values the support
   intersects). A knoll with a non-positive cap is not placed. Direction-free on purpose: a bound using the
   knoll's orientation against the local inland direction leaked (sweep: knoll-attributed pits on 4 seeds),
   because the inland direction turns across a knoll's support where rings curve.
5. **No knolls on the massif body.** A candidate whose centroid has `MountainMassif.Evaluate > 0` is not
   placed (a 3–7 m hill on a 250 m flank is not a landform, and it raised the Stage 3E rim in §8).
6. **Apron**: §6 domain fix; shape otherwise unchanged (the τ-limited apron shapes were measured and
   rejected, §3 D2/D-τ−A/D-τ0, §16 R1).

### 4.2 Guarantee [DERIVED]

Let `Z_M = closure{U2 > 0} ∪ closure{A > 0}` (massif influence zone) and `L_M` its **lee**: land points whose
straight segment to their nearest shore point meets `Z_M`. Let `B = H1 + M + Γ·(P + K)`.

- `(H1 + M)'(d) ≥ τ` on land (construction 1);
- `Γ` is a non-decreasing function of `d`, and `P, K ≥ 0`, so `Γ'·(P + K)·∂d/∂n ≥ 0`;
- `−∂P/∂n ≤ g_P ≤ τ` (2, 3) and `−∂K/∂n ≤ τ − g_P` where knolls exist (4), and `Γ ≤ 1`;
- hence `∂B/∂n ≥ τ − Γ·τ ≥ 0` everywhere on land.

The nearest-shore segment of a point is a straight line along which `d` decreases at unit rate, so `B` is
non-increasing along it towards the sea. For `x ∉ Z_M ∪ L_M` the segment never meets `U2 > 0` or `A > 0`,
so `H4A = B` on it: `x` has a non-ascending path to the ocean and **cannot lie in a closed depression**.
The proof does not depend on the seed, the coastline shape, the ring positions or the number of plates.

**Scope.** Depressions can only exist with every cell in `Z_M ∪ L_M` — the massif foot, where the frozen
Stage 2B flank dominates and H2 is itself not depression-free. [MEASURED-H] selected variant: canonical
0/7; sweep 6/40 (1006 0.73 m, 1013 0.39, 1019 0.33, 1026 0.94, 1027 2.32, 1038 0.98), **every one with
`A > 0` or `U2 > 0` at the deepest cell**; plate-class pits (`U2 = 0`, `A ≤ 0.1 m`): 0 (C6: 7).

### 4.3 Classification

**Design-level.** No setting of C6 achieves it; it adds one term (`M`), changes two derived quantities
(pair widths, junction radii), adds one certificate and two knoll rules.

---

## 5. S10 apron root causes

### 5.1 Mechanism #1 — the gate rising along a ray [DERIVED + MEASURED-H]

S10 in C6 tests the **gated fill** `A = SmoothMax(U2, Γ·P_A) − U2`. Along an outward ray that runs inland
while `d < 200 m`, `Γ` rises and `Γ·P_A` can rise even though `P_A` falls. 7–17 of 64 rays per massif world
show it. This is **not** an apron defect: the surface `H4A` along the ray also contains `H1`'s own rise, the
fill never exceeds the physical surface, and whatever the gate does to drainage is S8's business (§4). The
test measured the wrong quantity.

### 5.2 Mechanism #2 — reach beyond the projection domain [FACT + MEASURED-H]

`MountainMassif.Project` is unique only where the offset is below the axis's Federer reach
`R = MacroAxis.ReachMeters` (446–461 m on the canonical massifs) [FACT: `MacroAxis` remarks; the local
search in `MountainMassif.Nearest` is safe “inside the body” only]. The apron's outer edge sits at
`toe + W`, with `toe = HalfWidthAt(s)·lerp(2 − Asymmetry, Asymmetry, steep)` ≈ 353 m on the gentle side and
`W = 180 m`: 533 m > `R`. Past `R` the arc parameter jumps (measured 0.993 → 0.744 between offsets 460 and
465 m), so `h(s)`, `toe(s)` and `W` jump with it: the apron surface *rises outward* on 9–20 rays per world,
by up to 3.01 m, and non-zero apron (2.2–3.6 m) exists beyond `R`.

### 5.3 Effective reach [DERIVED]

Conditions: (i) `P_A = 0` wherever the projection is not unique; (ii) `P_A` is C¹ at its outer edge; (iii)
the steepest outer slope `2h/W` does not grow when the reach is shortened; (iv) no free constant.

- (i) ⇒ the outer edge `toe(s, side) + W_eff(s, side) ≤ R − δ` for every `(s, side, end)`.
  `δ = Δ²/(8R)` is the sagitta error of the 20 m axis sampling that `ReachMeters` is measured on
  (`Δ = 20 m`, `R ≈ 450 m` ⇒ `δ ≈ 0.11 m`); it is the only margin and it is derived.
- ⇒ `W_eff = clamp(R − δ − toe, 0, W)`; if `R − δ − toe ≤ 0` the piedmont is absent on that side.
- (ii) holds because `h·(1 − ρ/W_eff)²` has zero value and slope at `ρ = W_eff`.
- (iii) ⇒ `h_eff = h · W_eff / W`.
- `W_eff` is continuous in `(s, side, end)` inside the domain, so the apron stays continuous; beyond
  `R − δ` it is exactly 0, so the jump in `s` can no longer reach the surface.

On the canonical gentle sides this gives `W_eff ≈ 461 − 353 ≈ 108 m` instead of 180 m.

### 5.4 Classification

Both are **design-level**: #1 is a wrong invariant, #2 a missing domain condition in D§7.2.

---

## 6. Selected apron correction

[PROPOSAL] Apply §5.3; keep every other apron rule. The S10 invariant becomes a property of the
**ungated physical apron surface** `P_A` (§14 S10):

1. `P_A(x) = 0` exactly for `dist(x, axis) ≥ R − δ`;
2. along the projection's own normal at fixed `s` (and along end-cap rays from the end point),
   `P_A` is non-increasing outward, tolerance 1 mm;
3. the gated fill `A` is **not** required to be monotone along rays (mechanism #1 is covered by S8).

The combination rejected in the question (“H4A monotone along rays”) would forbid `H1`'s own inland rise.

Measured [MEASURED-H] (margin 10 m used in the harness; the derived `δ` changes nothing measurable):

| Seed | beyond-domain apron | rays with rising surface (worst) | rays with rising gated fill | apron zone | spread | coast-aligned |
|---|---|---|---|---|---|---|
| 110201 | 2.164 → **0** m | 14 (0.996 m) → 12 (**0.013 m**) | 10 → 7 | 17.7 → 15.7 % | 7.84 → 6.55 | 48.9 → 54.0 |
| 424242 | 2.450 → **0** | 9 (1.060) → 6 (**0.011**) | 13 → 10 | 27.5 → 25.3 | 7.90 → 6.26 | 51.0 → 46.2 |
| 20260905 | 2.933 → **0** | 20 (0.393) → 15 (**0.044**) | 17 → 12 | 32.8 → 29.8 | 8.94 → 7.59 | 61.6 → 62.9 |
| 7 | 3.623 → **0** | 13 (3.014) → 9 (**0.023**) | 15 → 12 | 18.7 → 17.0 | 9.25 → 7.67 | 56.7 → 60.4 |

The residual ≤ 0.044 m comes from the test rays (normals from ±0.005 s central differences, not the
projection's interpolated tangent): on an exact normal `Project` returns the same `s`. The spread drop
shows that part of C6's “spread” was the beyond-domain artefact.

---

## 7. Correct S11 trajectory invariant

### 7.1 What Stage 3F can and cannot absorb [FACT]

- `RidgeSettings.CorridorIncisionMeters = 18/2.5 = 7.2 m` bounds how far a corridor may cut **below Stage
  1** (`Stage3Field`: `allowance = max(0, above − floor) + CorridorIncisionMeters`, floor = Stage 1 height).
  Everything above Stage 1 is inside the allowance.
- The carve depth on the island floor is `7.2 · TroughProfile(a)`; a feature standing above the floor is
  cut through by the same allowance.

### 7.2 Two checks

**A — valley corridor budget (R4A metric kept).** Along each Stage 3C final trajectory from its toe to its
end, the deepest cut needed to make `R4A` alone monotone downstream is its **single climb**
`C(R) = max_i (R[i] − min_{j ≤ i} R[j])`. `C(R) ≤ 7.2 m` keeps any cut through Stage 4A relief a T3 trough,
not a T2 gorge. The **adverse sum** counts independent obstacles that are cut independently (110201 V6: two
knolls, sum 8.04 m, deepest cut 5.20 m); it is **reported**, not failed. Plate + apron adverse ≤ 1.5 m is
**kept hard** as required.

**B — actual terrain drainage compatibility.** Define the below-Stage-1 incision requirement along a
trajectory `I₁(H) = max_i max(0, H1[i] − min_{j ≤ i} H[j])`. **Theorem [DERIVED]:** `R4A ≥ 0` ⇒
`min_{j ≤ i} H4A[j] ≥ min_{j ≤ i} H2[j]` ⇒ `I₁(H4A) ≤ I₁(H2)` for every trajectory. Stage 4A can never push a
corridor over the Stage 3 incision bound; the climb it adds is above Stage 1 and inside Stage 3F's
allowance. **Threshold: 0 m of increase** (numeric tolerance 1 mm) — derived, not chosen; 0.5 m or 1 m
would be arbitrary. [MEASURED-H] max increase 0.000 m on all seeds for C6 and for the selected variant.
Ground climb on uncarved `H4A` (`C(H4A)`, `C(H4A) − C(H2)`) is reported only.

Side finding [MEASURED-H, out of scope]: on **frozen H2**, 110201 V5 already needs 8.57 m below Stage 1
(> 7.2 m) — a Stage 3C/3F matter recorded for the 3F preflight; Stage 4A lowers it to 0.78–1.02 m.

### 7.3 20260905 [MEASURED-H]

- 5 districts, levels 0 / 0.25 / 0.5 / 0.75 / 1; V8 ends through district P7 at constant level 0.25.
- Near its mouth V8 crosses a coastal headland that is **already in H2**: `d` 67 → 116 → 67 m, H2 climb
  6.16 m.
- R4A there is `Γ(H1)·18·0.25`; `Γ` follows `H1` over the headland, so R4A multiplies the H2 climb by
  `1 + U·λ·Γ'` ≈ 1.21 → ground climb 7.47 m, plate adverse 1.77 m.
- It is **not** a district-ranking or junction problem, and not owner-level (no plate needs to know a
  corridor). The below-Stage-1 requirement on this seed falls from 7.16 to 6.09 m (theorem, §7.4).
- Fix: the lowland reservation (§9) puts P7 at level 0 → plate + apron adverse 0.21–0.23 m.

### 7.4 Result [MEASURED-H]

| Seed | C6: climb R / adverse R / plate+apron / I₁ H2 → H4A | Selected: climb R / adverse R / plate+apron / I₁ H2 → H4A |
|---|---|---|
| 110201 | 5.20 / 8.04 / 0.79 / 8.57 → 1.02 | 3.45 / 4.53 / 0.90 / 8.57 → 0.78 |
| 424242 | 4.62 / 5.96 / 0.13 / 4.97 → 4.97 | 2.62 / 2.85 / 0.13 / 4.97 → 4.72 |
| 20260905 | 1.88 / 1.88 / **1.77** / 7.16 → 6.09 | 0.37 / 0.37 / 0.23 / 7.16 → 6.29 |
| 7 | 3.13 / 4.19 / 1.44 / 2.08 → 2.08 | 2.01 / 3.07 / 1.44 / 2.08 → 1.84 |

---

## 8. Correct S11 outlet invariant

### 8.1 What was wrong [FACT + MEASURED-H]

- C6 required `R4A` non-increasing downstream of each outlet. `R4A` is a fill, not a flow path; the
  requirement fails wherever the apron or a plate rises downstream while the ground still falls.
- Stage 3E's own semantics [FACT]: the outlet is the first minimum of `StructuralElevation` along the
  `BoundarySource.Outlet` run; the corridor trajectory crosses that run; the corridor end lies below the
  outlet. “Outlet run lower than every ridge run” is **not** a Stage 3E contract: it is already false on
  frozen H2 for 10 of 22 canonical basins.

### 8.2 New invariant [PROPOSAL]

“A Stage 3E outlet remains an outlet on H4A” means:

| # | Check | Hard? | Threshold and derivation |
|---|---|---|---|
| O-1 | corridor end elevation on `H4A` < outlet elevation on `H4A` (outlet = minimum of `H4A` over the outlet run) | hard | strict; Stage 3E's own inequality evaluated on H4A |
| O-2 | for every basin whose rim margin `min(ridge runs) − min(outlet run)` is > 0 on H2: margin on `H4A − C3` > 0, with `C3` = the Stage 3 `CarveField` depth | hard | 0; the carve Stage 3F will apply against H4A at the declared outlet; the valley depth there (`ValleyDepthPerCrest = 0.30 × crest`) dwarfs the measured reversals |
| O-3 | downstream of the outlet along the corridor: `I₁(H4A) ≤ I₁(H2)` and `C(R4A) ≤ 7.2 m` | hard | §7 (theorem + corridor budget) |
| O-4 | uncarved rim margin on H4A, shift of the outlet-run minimum (m) and `H4A(declared) − min H4A(run)` | report | informative; the declared point is owned by Stage 3E |
| — | `R4A` monotone downstream | **dropped** | not a drainage property |

Downstream gradient is covered by O-3 (no new below-Stage-1 requirement), minimum downstream clearance by
O-1, and new barriers by O-3's corridor budget.

### 8.3 Measured [MEASURED-H]

- O-1 passes on every canonical basin for C6 and the selected variant (end ≈ 23–71 m < outlet 84–123 m).
- O-3 theorem part passes everywhere (max increase 0.000 m).
- Uncarved rim reversals: C6 110201 b3 (+2.8 → −0.5 m), 20260905 b3 (+3.8 → −0.4 m); selected variant −0.5 /
  −0.8 m. Attribution at the two minima: **apron 17.7 m at the outlet-run minimum vs 6.9 m at the rim
  minimum** (110201 b3); apron 20.7 vs 20.7 m with a 55 m shift (20260905 b3). The apron fills the valley
  mouth because the outlet sits lower on the flank, where `U2` is smaller. On other basins the same effect
  shrinks margins by 5–18 m without reversing them (e.g. 19.3 → 1.5 m).
- O-2 could not be evaluated in the harness (its `CarveField` wiring returned zero depth); it must be
  measured in the Unity dump, which already builds Stage 3 **[OPEN → §18 step 7]**.
- O-4: outlet-run minimum shift up to 167 m (20260905), declared point up to 3.40 m above the run minimum.

---

## 9. Lowland / upland analysis

### 9.1 Problem [MEASURED-H]

Levels are whole-district ranks spread evenly over [0, 1]. Where the rank-0 district is small, the lowland
share is set by one sliver: 20260905 P3 = 0.252 km² (8 % of land) → 5.6 % lowland, 76.8 % upland; 777 P2 =
0.288 km² → 8.1 %; 256 → 15.1 %. Design target (D§11): lowland 20–35 %, upland 40–62 %.

### 9.2 Comparison (C6 geometry, only the level rule changed)

Lowland = `Λ < 0.25`, upland = `Λ ≥ 0.5`, share of land.

| Seed | A — rank (C6) low / up | B — area-weighted percentile | C — rank + lowland reservation (≥ 20 %) | Selected (C + τ widths) |
|---|---|---|---|---|
| 110201 | 29.4 / 54.7 | 29.3 / 54.5 | 29.4 / 54.7 | 28.4 / 54.2 |
| 424242 | 28.3 / 54.2 | 28.6 / 52.5 | 28.3 / 54.2 | 26.8 / 54.0 |
| 20260905 | **5.6 / 76.8** | 18.3 / 55.3 | 19.7 / 52.7 | 18.2 / 52.5 |
| 7 | 21.1 / 55.3 | 21.2 / 54.8 | 21.1 / 55.3 | 19.1 / 55.3 |
| 1337 | 25.8 / **67.6** | 25.8 / **33.2** | 25.8 / **67.6** | 23.7 / 64.8 |
| 256 | **15.1** / 60.5 | 16.0 / 58.9 | 37.7 / 51.3 | 36.6 / 51.1 |
| 777 | **8.1** / 56.3 | 33.3 / 55.8 | 40.8 / 50.8 | 39.7 / 49.6 |

Other metrics, B / C (C6): 70–110 m on 777 22.1 / 17.3 (25.2); p50 56 / 52 (58); p90 78 / 75 (78);
spread 6.09 / 7.30 (5.25); coast-aligned 69.5 / 66.8 (67.1); LR p90 8.52 / 7.82 (8.55); depressions: B
keeps both canonical pits and 777's moves, C the same (S8 is fixed by §4, not by levels); trajectory plate
+ apron on 20260905: B 1.03, **C 0.21** (C6 1.77).

### 9.3 Decision [PROPOSAL]

**C — bounded rank with lowland reservation.** Order districts by score as today. Walk the order from the
lowest; the **smallest prefix whose land reaches `LowlandReserveShare = 0.20`** (D§11's lower bound) gets
level 0 — the frontage rule still applies to the prefix's first member. The remaining districts are ranked
evenly over (0, 1].

- It changes nothing where the lowest district is already big enough (110201, 424242, 7, 1337).
- B (area percentile) makes “upland” depend on district sizes: 1337's middle district (1.10 km², the
  largest) drops below 0.5 and the upland share collapses to 33 %.
- C overshoots 35 % on 256 (+2.7 pp) and 777 (+5.8 pp) because districts are whole plates; the visual check
  (777) shows one coherent southern plain, not a hole. The band remains a report (§14).
- 1337's 64.8 % upland (+2.8 pp over the band) is the three-district case where the middle rank is exactly
  0.5; not addressed (§16 R5).

---

## 10. K2 decision

**K2 (macro plate warp or any continuous lowland field) is not implemented and not needed for Stage 4A v1.**

- The lowland defect is *quantisation of whole-plate levels*, not a missing field; §9's rule fixes it on all
  three failing seeds without a new structure.
- K2 would add a new spatially varying field whose inland derivative must itself fit the §4 budget — a
  second source of the very counter-slopes S8 had to be cured of.
- Revisit only if Stage 4B's plateaus need sub-plate lowlands.

---

## 11. Knoll / trajectory interaction

### 11.1 Decomposition [MEASURED-H]

| Trajectory | R4A adverse | R4A climb | knoll-only climb | plate + apron adverse | ground climb H2 → H4A |
|---|---|---|---|---|---|
| 110201 V6 (C6) | **8.04** | 5.20 | 5.24 | 0.79 | 4.11 → 0.21 |
| 110201 V7 (C6) | 3.50 | 3.50 | 4.31 | 0.00 | 0.12 → 0.98 |
| 424242 V0 (C6) | 2.48 | 2.48 | 3.25 | 0.13 | 0.12 → 2.10 |
| 7 V0 (C6) | 4.19 | 3.13 | 2.93 | 1.44 | 1.18 → 3.36 |
| 20260905 V8 (C6) | 1.74 | 1.72 | 1.46 | **1.77** | 7.16 → 8.88 |

### 11.2 Cause

- A (count): no — keep fraction 0.5 (K1) removes one of V6's knolls (8.04 → 5.20) by chance, not by rule.
- B (placement): only in the sense of **D (corridor integration)**: a corridor that crosses two knoll
  supports accumulates two adverse runs. Each is cut independently; the single climb stays ≤ the tallest
  knoll ≤ 7.2 m by construction (`KnollHeightCapMeters = CorridorIncisionMeters`).
- C (kernel size): no — the smaller kernel (K3) makes V6 worse (9.13 m).
- A corridor keep-out (K2a) cleans every R4A metric (V6 0.79 m) but couples Stage 4A to Stage 3C trajectories
  and still leaves the physical quantities unchanged; not adopted.

### 11.3 Decision [PROPOSAL]

No knoll rule changes for S11 (single-climb check, §7). Knolls change for **S8**: amplitude from the slope
budget (§4.1 item 4) and no knolls on the massif body (§4.1 item 5). Effect [MEASURED-H]: knolls
12 / 20 / 23 / 16 / 18 / 18 / 19 → 9 / 15 / 10 / 12 / 19 / 17 / 17; height cap `τ·σ_minor/0.5726` ≈ 3.0–4.0 m
for the canonical σ_minor of 34–46 m; R4A adverse maxima 8.04 / 5.96 / 1.88 / 4.19 → 4.53 / 2.85 / 0.37 / 3.07 m.

---

## 12. Updated formula

```
H4A = H1 + U2 + R4A                    (unchanged; future H = H4A − C3F)
R4A = M(d) + Γ·(P + K) + A

M(d) = ∫₀^d max(0, τ − H1'(u)) du      d > 0; 0 at and seaward of the shore
τ    = InteriorRiseMeters / InteriorScaleMeters                                   (= 0.05)
Γ    = S(clamp01((H1 − Sea) / CoastalRiseMeters))                                 (unchanged)
P    = PlateUpliftMeters · m_world · Λ(x)
Λ    : soft Voronoi with pair half-widths w_kj = max(w₀, 1.5·U·m·|λ_k − λ_j| / (2τ)),
       junction relaxation radius R_J = max(R₀, 1.5·U·m·max|λ − λ̄_J| / τ), certified (§4.1 item 3)
λ    : rank with lowland reservation (§9.3)
K    = max_i a_i·(1 − q_i/9)³₊,   a_i = min(a_nominal,i , (τ − g_P,i)·σ_minor,i / 0.5726),
       no knoll where MountainMassif.Evaluate(centroid) > 0
A    = SmoothMax(U2, Γ·P_A, ApronBlendMeters) − U2,
       P_A with W_eff = clamp(R_axis − δ − toe, 0, W), h_eff = h·W_eff/W, P_A ≡ 0 beyond R_axis − δ
```

### 12.1 Changed rules

**R-1 — ring lips.**
- OLD: “all terms ≥ 0” prevents lips against the datum's zero-slope rings (D§12.2).
- NEW: every term's inland descent fits one budget `τ`, and `M` gives `H1 + M` an inland slope ≥ τ.
- REASON: positivity does not bound the inland derivative (§2.1); 15/40 sweep seeds fail under C6.
- EXPECTED EFFECT: no depression outside `Z_M ∪ L_M` on any seed (proof §4.2); canonical 2 → 0; sweep
  class P 7 → 0; `M` adds 0.2–6.6 m radially (70–110 m +1.0…+3.5 pp with `M` and the τ widths alone).

**R-2 — plate edge width.**
- OLD: `PlateEdgeHalfWidthMeters = 110 m` for every pair (“10–90 % over ~200 m”; actual ~134 m).
- NEW: `w_kj = max(110, 1.5·U·m·Δλ/(2τ))` per pair.
- REASON: the steepest edge descent must be ≤ τ.
- EXPECTED EFFECT: widths 110–378 m; softer district edges; lowland/upland shares −1…−2 pp; coast-aligned
  gradient rises (selected variant as a whole: +1.1…+13.4 pp, 110201 48.9 → 62.3 %).

**R-3 — junction relaxation.**
- OLD: fixed 450 m, justified by three prototype seeds.
- NEW: derived radius + build-time certificate.
- REASON: the normalised soft-Voronoi product has no closed-form slope bound at triple points.
- EXPECTED EFFECT: exact monotonicity of the plate term; larger discs where contrasts are high (§16 R2).

**R-4 — knoll amplitude and placement.**
- OLD: `a = 6.5·(0.6 + 0.4r)·(0.5 + 0.5Λ) ≤ 7.2 m`, anywhere with `Γ ≥ 0.5`, `Λ ≥ 0.34`.
- NEW: additionally `a ≤ (τ − g_P)·σ_minor/0.5726`; not on the massif body.
- REASON: S8 proof; knolls on the flank raised Stage 3E rims.
- EXPECTED EFFECT: knoll height ≈ 3–4 m (design tier 4–7.2 m, §16 R3); counts −0…−13; R4A adverse maxima
  roughly halved.

**R-5 — apron domain.**
- OLD: `W` from steep/gentle/end weights only; apron evaluated wherever the support rectangle reaches.
- NEW: `W_eff = clamp(R − δ − toe, 0, W)`, `h_eff = h·W_eff/W`, `P_A ≡ 0` beyond `R − δ`.
- REASON: projection not unique beyond the axis reach (§5.2).
- EXPECTED EFFECT: beyond-domain apron 2.2–3.6 m → 0; apron zone −1.7…−3.0 pp; gentle-side piedmont
  ≈ 108 m instead of 180 m.

**R-6 — S10 invariant.**
- OLD: gated apron fill monotone along 64 rays.
- NEW: ungated `P_A` monotone along exact projection normals and exactly zero beyond `R − δ`; gated fill
  reported.
- REASON: mechanism #1 is the gate, not the apron (§5.1).
- EXPECTED EFFECT: S10 passes with a 1 mm tolerance.

**R-7 — S11 trajectories.**
- OLD: plate + apron adverse ≤ 1.5 m, R4A adverse ≤ 7.2 m, single climb ≤ 7.2 m.
- NEW: plate + apron adverse ≤ 1.5 m (kept), R4A single climb ≤ 7.2 m (hard), R4A adverse (report),
  `I₁(H4A) ≤ I₁(H2)` (hard, theorem).
- REASON: the adverse sum is not a cut depth; `I₁` is what Stage 3's incision bound constrains.
- EXPECTED EFFECT: all canonical trajectories pass (§7.4).

**R-8 — S11 outlets.**
- OLD: `R4A` non-increasing downstream of every outlet.
- NEW: O-1…O-4 (§8.2).
- REASON: Stage 3E's own semantics; compatibility with Stage 3F carving against H4A.
- EXPECTED EFFECT: O-1 and O-3 pass; O-2 to be confirmed in the Unity dump.

**R-9 — plate levels.**
- OLD: rank normalised evenly over [0, 1].
- NEW: lowland reservation to ≥ 20 % of land, the rest ranked over (0, 1].
- REASON: whole-plate quantisation leaves 5.6–15 % lowland on 3/7 seeds.
- EXPECTED EFFECT: 20260905 5.6 → 18.2 %, 256 15.1 → 36.6 %, 777 8.1 → 39.7 %; 20260905 plate+apron
  1.77 → 0.23 m.

**R-10 — S8 scope.**
- OLD: zero depressions ≥ 0.25 m / 100 m², any location.
- NEW: S8-A zero anywhere outside `Z_M ∪ L_M` (hard, every seed, proven); S8-M zero inside on the 7
  canonical seeds (hard) and reported on the sweep.
- REASON: the massif-foot class cannot be guaranteed by Stage 4A alone (§16 R1); frozen H2 has it too.
- EXPECTED EFFECT: canonical 0; sweep 6/40, all attributed.

---

## 13. Updated API requirements

**Implemented 2026-09-16** (see the design §15 for the live surface). The list below is the R1 contract
that was coded.

- `IslandReliefSettings`
  - add `LowlandReserveShare` (0.20);
  - add `InlandSlopeBudget` (`float`; `Default` computes it from `TerrainGenSettings.Default` as
    `InteriorRiseMeters / InteriorScaleMeters`; `Clamp` keeps it in `(0, 0.25]`);
  - add `JunctionCertificationStepMeters` (5 m, world-aligned, independent of the terrain lattice) and
    `JunctionCertificationMaxIterations` (8) — algorithm controls of the certificate, not shape parameters;
  - `PlateEdgeHalfWidthMeters` and `JunctionRelaxRadiusMeters` become **minimums**.
- `IslandRelief.Build(seed, stage1, plates, massifs, settings)` — signature unchanged; builds the `M` table
  from `stage1.Settings`/`stage1.Model`, per-pair widths, certified junctions, knoll caps.
- `IslandRelief` exposes read-only diagnostics: `SlopeBudget`, `PairHalfWidth(k, j)`,
  `JunctionCertificate` (max descent, iterations, final radius per junction), `ReliefKnoll.Cap`,
  `ReliefKnoll.PlateDescentBound`, `ReliefKnoll.Omitted` reason (`Gate`, `Level`, `Resistance`, `Budget`,
  `MassifBody`), `ApronFrame.EffectiveReach(s, side)`.
- Point evaluation needs the coast distance for `M`:
  `float At(float x, float z, float signedDistance, float stage1Height, float upliftMeters)`;
  `IslandElevation.At` already computes `sd` once and passes it. The bulk path uses
  `Stage1Result.SignedDistance`, so point and lattice stay bit-identical (S4).
- `IslandReliefSample` gains `CoastalFloor` (`M`); `Total = CoastalFloor + Gate·(Plate + Knoll) + Apron`.
- `IslandRelief.InfluenceZone(x, z)` → `U2 > 0 || A > 0` for the S8 attribution; the lee test lives in the
  dump/tests (nearest-shore segment), not in TerrainGen.
- No Hydrology reference in `GameDev.World.TerrainGen` (unchanged rule).

---

## 14. Updated acceptance thresholds

Tests S1–S7 and S9 unchanged. Changed and new:

| Test | Definition | Threshold |
|---|---|---|
| **S8-A** | every depression (fill ≥ 0.25 m and ≥ 100 m²) with any cell outside `Z_M ∪ L_M` | **0**, 7 canonical seeds (hard) |
| **S8-M** | depressions entirely inside `Z_M ∪ L_M` | **0 on the 7 canonical seeds** (hard); count, depth, area and attribution reported |
| **S8-C** (new) | monotonicity certificate: on the 1025² lattice, outside `Z_M ∪ L_M`, `∂(H1 + M + Γ(P + K))/∂n` along the coast-distance gradient (cells with `|∇d| ≥ 0.95`, central differences) | `≥ −1·10⁻³` (float32 rounding of ~100 m heights over a 5.47 m stencil is ≈ 4·10⁻⁴) |
| **S8-S** (dump only) | 40-seed sweep 1000–1039 | report; any S8-A failure is a defect |
| **S10** | ungated `P_A`: 0 beyond `R − δ`; non-increasing along exact projection normals (24 s-stations × 2 sides) and 16 end-cap rays per massif | 1 mm |
| **S11-T** | per Stage 3C final trajectory, toe → end: `C(R4A) ≤ 7.2 m`; plate + apron adverse ≤ 1.5 m; `I₁(H4A) ≤ I₁(H2) + 1 mm` | hard; R4A adverse sum and ground climbs reported |
| **S11-O** | O-1, O-2 (with Stage 3 `CarveField`), O-3 hard; O-4 reported | §8.2 |
| **S12** (new) | junction certificate: every on-land junction's max `−∂(ΓP)/∂n` ≤ τ | hard |

Numerical targets (D§11) — changes only:

| Metric | OLD target | NEW target | Why |
|---|---|---|---|
| depressions | 0 anywhere | S8-A / S8-M above | §4.2 scope |
| lowland share (`Λ < 0.25`) | 20–35 % | **report**; gate = the reservation rule is applied (reserved land ≥ 20 %) | whole-plate quantisation (§9.3) |
| upland share | 40–62 % | report | same |
| knolls | 10–25, height 4–7.2 m | 8–25, height ≤ `τ·σ_minor/0.5726` | §11.3, §16 R3 |
| plate edge | ~200 m, ≤ 8° added | derived per pair, added slope ≤ τ | R-2 |
| adverse rise, R4A | ≤ 7.2 m | single climb ≤ 7.2 m; adverse reported | R-7 |
| apron zone | ≤ 25 % | unchanged (still exceeded on 424242 / 20260905, §16 R4) | — |

---

## 15. Expected effect on all 7 canonical seeds

Selected variant (τ = 0.05 budget, strict knoll budget, no knolls on massif body, apron domain fix, lowland
reservation) measured in the harness [MEASURED-H]; junction certification not yet included (§16 R2).

| Seed | 70–110 m | p50 / p90 | LR med / p90 | spread | aligned | < 5° | > 45° | reach | pits | low / up | knolls | apron zone |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 110201 | 24.7 → **26.2** | 58 / 106 | 4.18 / 8.61 | 6.45 | 62.3 | 35.2 | 5.55 | 92.82 | **0** | 28.4 / 54.2 | 9 | 15.7 |
| 424242 | 22.9 → **24.4** | 57 / 119 | 4.84 / 9.47 | 6.17 | 58.0 | 26.6 | 6.38 | 92.32 | **0** | 26.8 / 54.0 | 15 | 25.3 |
| 20260905 | 22.2 → **22.3** | 62 / 155 | 5.23 / 10.76 | 8.12 | 62.1 | 24.5 | 7.26 | 92.03 | **0** | 18.2 / 52.5 | 10 | 29.8 |
| 7 | 15.2 → **16.2** | 54 / 111 | 4.95 / 8.94 | 7.52 | 65.5 | 29.4 | 5.32 | 94.31 | **0** | 19.1 / 55.3 | 12 | 17.0 |
| 1337 | 24.8 → **26.9** | 55 / 79 | 3.95 / 8.54 | 5.41 | 60.7 | 47.7 | 0 | 100 | 1 → **0** | 23.7 / 64.8 | 19 | — |
| 256 | 27.0 → **23.3** | 51 / 78 | 4.02 / 8.87 | 6.57 | 65.7 | 43.4 | 0 | 100 | **0** | 36.6 / 51.1 | 17 | — |
| 777 | 25.2 → **18.7** | 53 / 77 | 3.56 / 7.76 | 6.82 | 70.4 | 51.1 | 0 | 100 | 1 → **0** | 39.7 / 49.6 | 17 | — |

Per seed:

- **110201** — S8 already clean; apron tail beyond reach removed (2.16 m → 0); coast-aligned gradient rises
  48.9 → 62.3 % (wider edges, shorter apron) but stays ≤ 65 %; R4A adverse 8.04 → 4.53 m; outlet b3 margin
  reversal (−0.5 m uncarved) remains for O-2.
- **424242** — ray artefacts gone (1.06 m → 0.011 m); apron zone 27.5 → 25.3 % (still over 25 %); knolls
  20 → 15.
- **20260905** — lowland 5.6 → 18.2 %, upland 76.8 → 52.5 %; V8 plate + apron 1.77 → 0.23 m; knolls 23 → 10
  (district P9 drops from level 0.50 to 0.33, below `KnollMinLevel`; flank knolls removed); p90 155 m remains the massif's (target
  100–135 was never met by C6 either).
- **7** — rays 3.01 m → 0.023 m; coast-aligned 65.5 % (at the limit); knolls 16 → 12.
- **1337** — the ring-100 pit (0.67 m) disappears; spread 6.01 → 5.41; upland 64.8 % (R5).
- **256** — lowland 15.1 → 36.6 % (reservation takes two districts); 70–110 m 27.0 → 23.3 %; p50 51 m (1 m
  under the band).
- **777** — the ring-200 pit (0.29 m) disappears; one broad southern plain (lowland 39.7 %); 70–110 m
  25.2 → 18.7 % (≥ 12 % no-massif target); coast-aligned 70.4 % (0.4 pp over the no-massif limit); LR median
  3.56 m (≥ 3.5).

---

## 16. Risks

| # | Risk | Evidence | Mitigation / owner |
|---|---|---|---|
| R1 | **Massif-foot depressions** (inside `Z_M ∪ L_M`) on non-canonical seeds | sweep 6/40, 0.33–2.32 m; H2 itself 1/40; τ-limited apron shapes did not remove them without breaking coast-alignment targets (D-τ−A: 3/40 but aligned 70–78 %; D-τ0: 2/40) | **[OPEN]** Stage 3F preflight must state massif-foot drainage (strike troughs on inland-facing flanks); Stage 4B piedmont design may replace the apron tail. S8-M stays hard on canonical seeds so no regression enters silently |
| R2 | Junction certification widens discs and blurs districts | derived radius without certification: 1337 spread 5.07 → 3.76 | certify, don't pre-widen; measure in the Unity dump before accepting (§18 step 7) |
| R3 | Knolls drop to ≈ 3–4 m, below the design's 4–7.2 m tier | §11.3 | accepted for v1; alternative inside the same proof: an asymmetric kernel with a long inland tail (steep seaward flank) keeps 4–7 m heights — **not measured** |
| R4 | Apron still reads as a ring; apron zone > 25 % on 424242 / 20260905; apron fills Stage 3E valley mouths | §1, §8.3 | O-2 guards drainage; ring appearance owned by Stage 3F valleys and Stage 4B |
| R5 | Three-district islands put the middle rank exactly at 0.5 (1337 upland 64.8 %) | §9.2 | report; no rule change without a visual case |
| R6 | Straight bisector edges remain visible | §1 visual | structural edge warp (`terrain.relief.edge`, reserved) is a Stage 4A v2 candidate; it must fit the budget |
| R7 | Interior tilt `+τ` per metre beyond 800 m | no canonical land beyond 800 m | S8-C and the sweep report the land share beyond 800 m |
| R8 | Harness ≠ Unity bit-for-bit | libm ULPs | all acceptance numbers re-measured by the Unity dump (§18 step 7) |

---

## 17. What remains frozen

- Stage 1 (`CoastHeightModel`, `ProfileCurve`, coast and distance fields), Stage 2A (macro axis), Stage 2B
  (massif uplift, its coast gate), Stage 3A–3E (skeleton, ridges, valley topology, trajectories, structural
  basins, outlets, `CarveField`) and every pinned golden hash.
- `TerrainAuthorityField`, `OverworldTerrainField`, the Unity-native `TerrainData` bake, scenes,
  `GeneratorVersion`.
- Stage 3F, 4B, 4C: not started.
- Stage 4A invariants kept: no term lowers land; `R4A = 0` at and seaward of the shore (`M(0) = 0`); land mask
  bit-identical; point = lattice; knoll stream `terrain.relief.knoll` keyed by crust site with both draws
  taken for every land site; no Hydrology dependency in TerrainGen; no noise, raster smoothing, fill or
  repair pass (the junction certificate evaluates the analytic level field; it never reads a raster of
  terrain).
- **Stage 4A remains disconnected from `TerrainAuthorityField`.**

---

## 18. Exact implementation delta required

**Done 2026-09-16** on the committed C6 files. Unity 6000.5.7f1: Stage 4A 16/16, EditMode 571/571,
canonical S8-A/M/C S10 S11-T/O S12 green, sweep S8-A 0/40 S8-M 6/40. Residuals (zero knolls, apron
ring/zone, report-only metric misses) recorded in `OVERWORLD_TECH_DEBT.md` item 19. Production path
untouched.

1. **`IslandReliefSettings.cs`** — add `InlandSlopeBudget`, `LowlandReserveShare`,
   `JunctionCertificationStepMeters`, `JunctionCertificationMaxIterations`; defaults and `Clamp` as §13;
   document `PlateEdgeHalfWidthMeters` / `JunctionRelaxRadiusMeters` as minimums.
2. **`IslandRelief.cs`** — build the `M` table (step 0.5 m, cumulative `max(0, τ − H1')` from
   `Stage1Result.Model`); `At`/`Sample` take `signedDistance`; `Total = M + Γ(P + K) + A`; `EvaluateField`
   reads `Stage1Result.SignedDistance`; `IslandReliefSample.CoastalFloor`.
3. **`IslandReliefPlates.cs`** — `AssignLevels`: lowland reservation (§9.3) after sorting and the frontage
   rule; per-pair half-width array `w_kj`; `BuildJunctions`: derived radius and the certification loop
   (5 m lattice inside `R_J + max w`, `−∂(ΓP)/∂n` by central differences of the analytic level field,
   scale radius and incident pair widths by `max/τ`, ≤ 8 iterations, store the certificate); `LevelAt` uses
   the per-pair widths and per-junction radii.
4. **`IslandReliefKnolls.cs`** — after eligibility: skip if `MountainMassif.Evaluate(centroid) > 0` for any
   massif; compute `g_P` as the analytic bound over the support disc (pair bands by `max S'(u)` over the
   disc's `δ` interval, certified junction maxima); `a = min(a, (τ − g_P)·σ_minor/0.5726)`; skip if `a ≤ 0`;
   keep drawing both RNG values for every land site first.
5. **`IslandReliefApron.cs`** — `ApronFrame` stores `R_axis` and `δ`; `W_eff`, `h_eff`; return 0 for
   `dist ≥ R_axis − δ`; expose `EffectiveReach`.
6. **`IslandElevation.cs`** — pass `sd` into `IslandRelief.At`.
7. **`TerrainGenIslandReliefDump.cs`** — add: `M` image; per-pair width and junction certificate report;
   knoll omission reasons; S8 attribution (`Z_M`, lee via nearest-shore segment) and the S8-C certificate
   map; S10 on exact normals + beyond-domain check on `P_A`; S11-T (`C(R4A)`, adverse, plate + apron, `I₁`
   on H2/H4A); S11-O with Stage 3 `CarveField` depth at declared outlets; lowland reservation report;
   `-terrainGenSweep 1000:40` batch option writing the S8 sweep table. Re-measure everything in Unity.
8. **`TerrainGenStage4ATests.cs`** — replace S8, S10, S11 by §14; add S8-C and S12; S1–S7 and S9 unchanged
   (S4 point = lattice must include `M`).
9. **Design document** — after implementation and Unity measurement, fold R-1…R-10 into
   `PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md` (D§5.3, §5.4, §6.1, §7.2, §11, §12, §19) and keep this
   file as the change record.

Not part of the delta: any change to Stage 1–3E, `TerrainAuthorityField`, `TerrainData`, scenes, hashes,
`GeneratorVersion`; Stage 3F / 4B / 4C; commits.
