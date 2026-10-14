# Procedural Terrain — Stage 3F Drainage Causal Investigation, Handoff

## 1. Status and scope

**Investigation phase COMPLETE. No production code was written, changed or committed.**

The phase answered one question: *why does Stage 3F produce closed valleys, and which of them are
Stage 3F's to own?* It did so entirely through read-only counterfactual probes that were deleted
after each run. It deliberately did **not** choose a remedy, and the one invariant it isolated is
**not** cleared for implementation — see §7.

Out of scope and untouched: Stage 3C/3D/3E topology, Stage 4A R1, S11-T, `TroughProfile`,
`CorridorIncisionMeters`, `SmoothMax`, basin logic, canonical thresholds, the production authority.

## 2. Architecture and binding constraints

```
seed → Stage 1 (landmass) → Stage 2A/2B (plates, massifs)
     → StructuralElevation   H2  = H1 + U2
     → Stage 4A IslandRelief H4A = H1 + U2 + R4A        ← production terrain authority (R1)
     → Stage 3F CarvedElevation H3F = H4A − C3F          ← not on the production path
     → TerrainAuthorityField → native TerrainData bake
```

The carve, read verbatim from `CarveField` and `CarvedElevation`:

```
C3F = min( SmoothMax_corridors[ (mountain + trough) · Across(1−w) ] · gate,
           (H4A − H1) + CorridorIncisionMeters,
           H4A − SeaLevelMeters − SeaMarginMeters )

mountain = ValleyDepthPerCrest · Λ(head) · U2(p) · RiseProfile(arc/toe)
trough   = CorridorIncisionMeters · TroughProfile(arc/Length)
TroughProfile      = [0, 0.5, 1] → [0, 1, 0]      (non-merge)
TroughToMergeProfile = [0, 0.5, 1] → [0, 1, 1]
AcrossProfile      = [0, 0.55, 0.8, 1] → [0, 0.55, 1, 1], evaluated at 1−w
gate               = smoothstep(clamp01(d / CoastGateMeters)),  CoastGateMeters = 200 m
CorridorIncisionMeters = 18 / 2.5 = 7.2 m            SeaLevelMeters = 24, SeaMarginMeters = 0.5
```

Constraints that must survive any future work: Vista stays retired; one production terrain pipeline
with no fallback; no second terrain authority or parallel valley generator; no `TerrainData` →
TerrainGen feedback; Stage 3D/3E keep declaring basins and outlets on `H2`; Stage 4A must never
depend on Stage 3C geometry or hydrology; never retune canonical seeds or loosen tests to make a
report green.

## 3. Experiments and headline results

Population throughout: seeds 1000–1039, **261 trajectories on 32 massif-bearing seeds**. One
classifier (`Stage3FVerification.Profile`), 20 m stations, `QualifyingDepthMeters = 0.25`,
`TerminalMarginMeters = 30`, `CrossSectionFractions = {−1, −0.5, −0.25, 0, 0.25, 0.5, 1}`,
positional matching tolerance 40 m. Every decomposition below is an identity, not a fit:

```
depth ≡ [H4A(barrier) − H4A(low)] + [C(low) − C(barrier)]     — verified, 0 mismatches / 96 rows
```

### 3.1 Layer decomposition of the shipping surface

| surface | CLOSED | created | removed | deepened inherited | total barrier depth |
|---|---|---|---|---|---|
| `H1` | 43 | 18 | 11 | — | — |
| `H2` | 45 | 21 | 12 | — | — |
| `H4A` | 36 | 0 | 0 | — | 363.7 m |
| `H3F` shipping | 88 | 52 | 0 | 12 | 512.1 m |

Stage 3F is the numerically dominant creator of new closures. Stage 4A is net-negative on count but
roughly **doubles** inherited depth.

### 3.2 Peeling the aggregation defects (counterfactuals, SELF-ONLY baseline)

| step | created | total depth |
|---|---|---|
| shipping (`SmoothMax`) | 62 | 189.0 m |
| − `SmoothMax` inflation | 54 | 167.0 m |
| − foreign corridor | 52 | 158.0 m |
| − trough entirely (T0) | **4** | **6.3 m** |

`SmoothMax(a,b,blend)` sets `k = min(blend, min(a,b))`; with `RangeBlendMeters = 40` and carve
depths ≤ 15 m, `k` is **always** the smaller input, so the function never leaves its maximally
inflating regime: `SmoothMax(7.2, 7.2) = 9.00` (+25 %), three equal 5 m corridors → 6.95 (+39 %).
Measured inflation term **+28.5 m**, fully accounting for 13 of 62 created closures.

Foreign-corridor depth is decisive for **9** closures; **11 of 12** foreign owners are declared
tributaries, and the barrier sits where the tributary's support ends (`barToSupEnd` −140…+40 m,
peak contribution exactly 7.20 m). Worked case 1014/2: tributary 3 climbs 0.57 → 3.26 → 5.57 →
**7.19** then collapses to **0.71** over one 20 m step; `H3F` rises 60.55 → 62.91 = the closure
depth 2.36 exactly, while the self-only surface descends monotonically.

### 3.3 Trough shape (T0 none, T1 shipping, T2 ramp `P(a)=a`, T3 full depth from the toe)

| variant | created | `priorState=none` | total depth | inherited deepened |
|---|---|---|---|---|
| T0 | 4 | 0 | 6.3 m | 3 |
| T1 shipping | 52 | 9 | 158.0 m | 12 |
| T2 ramp | 26 | 2 | 75.5 m | — |
| T3 peak at toe, flat after | 50 | **0** | 117.5 m | 1 |

`a_toe` median **0.279**; `a_toe < 0.5` on **234 of 261** trajectories, so the shipping trough's
maximum is nowhere near the toe and the doc comment claiming otherwise is wrong. Over `a ∈ [0.5, 1]`
the trough changes by **T1 −7.20 m, T2 +3.60 m, T3 0.00 m**.

**Two mechanisms hide inside the 52.** T3 removes downstream decay and eliminates *all* clean
closures (9 → 0) and the whole longitudinal trough contribution, yet keeps **50** — of which 48 are
`DrainableWithinTrajectory` on `H4A`: lows the island already had, drained around the axis, which
any sufficiently deep carve closes regardless of shape. Probability of closing an `H4A`-drainable
low rises **0–1 % at zero carve → 51–54 % at C ≥ 5 m**, and nearly identically for T1 and T3, so
that half is driven by **amplitude, not shape**.

### 3.4 The sign of `dC/ds` is the sign of the carve's effect — 1023/4

| variant | carve low→barrier | `dC/ds` | inherited depth |
|---|---|---|---|
| `H4A` | — | — | 3.65 m |
| T0 | 0.00 → 0.00 | 0 | 3.65 m (identical) |
| T3 | 7.20 → 7.20 | 0 | 3.65 m (identical; `H3F ≡ H4A − 7.20` at every station) |
| T1 | 6.21 → 3.45 | −0.0138 | **6.54 m** (worse) |
| T2 | 4.44 → 5.45 | +0.0050 | **2.76 m** (better) |

A constant carve is a rigid vertical translation and cannot change topology. Only a
downstream-deepening carve can remove an inherited closure.

### 3.5 The four residual closures — the broken factorization, measured

With trough, foreign corridor and inflation all removed, four closures remain. All sit **above the
toe** (`distToe` −51…−102 m), all `dGate = 0`, gate = 1.0, and the whole carve there is the
mountain term:

| seed/traj | depth | ΔH2 | ΔH4A | ΔR4A | carve low→bar | `dC/ds` | `dH3F/ds` |
|---|---|---|---|---|---|---|---|
| 1010/0 | 1.88 | −17.01 | +0.28 | **+17.29** | 2.24 → 0.64 | −0.0400 | +0.0470 |
| 1013/2 | 2.05 | −22.15 | −1.12 | **+21.03** | 5.25 → 2.08 | −0.0792 | +0.0513 |
| 1026/6 | 1.29 | −11.12 | +0.20 | **+11.32** | 1.88 → 0.78 | −0.0548 | +0.0646 |
| 1038/0 | 1.12 | −7.86 | +0.27 | **+8.13** | 1.61 → 0.76 | −0.0426 | +0.0560 |

`U2` collapses by 8–22 m, Stage 4A's `R4A` backfills the same 8–21 m, `H4A` stays level, and the
mountain carve — still keyed to `U2` — follows it down. Threshold-insensitive: the same four at
`qual` 0.00 through 0.50.

The legacy Stage 3C proof (`PROCEDURAL_TERRAIN_STAGE_3C_VALLEY_FORENSICS.md` §"Three properties")
held because the carve was `α·U·Rise`, proportional to the *same* `U` the ground was made of, so
`H3 = H1 + U(1 − α·Rise)` factored. The ground is now `H1 + U2 + R4A` while the mountain term still
tracks only `U2`, so the cancellation is gone; the trough tracks nothing at all.

### 3.6 Amplitude sweep of the drainage-constrained envelope — the closing experiment

Envelope: `C[i] = min( max( T1[i], C[i−1] + (H4A[i] − H4A[i−1]), 0 ), budget, A_max )`.

| `A_max` | CLOSED | created | inherited left | inherited removed | deepened | total depth | inherited depth | residual 4 | strictly feasible | carve p50 / p90 / max |
|---|---|---|---|---|---|---|---|---|---|---|
| 7.2 m | 75 | 39 | 36 | 11 | **0** | 448.9 | 293.4 | **4/4 drained** | 34/261 | 7.2 / 7.2 / 7.2 |
| 12 m | **29** | 15 | 14 | 23 | **0** | 211.9 | 105.5 | 4/4 | 79/261 | 12 / 12 / 12 |
| 20 m | 21 | 11 | 10 | 27 | **0** | 111.5 | 55.3 | 4/4 | 117/261 | 20 / 20 / 20 |
| 30 m | 17 | 9 | 8 | 29 | **0** | 82.7 | 40.9 | 4/4 | 145/261 | 21.6 / 30 / 30 |
| no cap | 17 | 9 | 8 | 29 | **0** | 82.7 | 40.9 | 4/4 | 249/261 | 21.6 / 56.6 / 75.6 |

What each step of depth buys:

```
 7.2 → 12 m : CLOSED 75 → 29 (−46),  inherited removed 11 → 23 (+12)
12   → 20 m : CLOSED 29 → 21 ( −8),  inherited removed 23 → 27 ( +4)
20   → 30 m : CLOSED 21 → 17 ( −4),  inherited removed 27 → 29 ( +2)
```

Reference, same classifier and population: `H4A` 36 CLOSED / 363.7 m; T1 88 / 512.1 m; T3 86 /
423.1 m. No counterexample exists where the rule holds over the whole low→barrier span and a
longitudinal closure survives — all residual closures are places where the cap clipped the required
carve.

Two results matter more than the counts. **The envelope never deepens an inherited closure at any
cap** (T1 deepens 12). **All four residual closures drain at `A_max = 7.2 m`** — the `U2`/`R4A`
break is fixable inside the existing tier amplitude. And strict feasibility is *not* required for
the benefit: at 30 m only 145/261 satisfy the rule yet the outcome already equals the uncapped one.

## 4. Causal model

| mechanism | role | evidence | owner |
|---|---|---|---|
| trough downstream decay | **creates** longitudinal barriers | T3 removes all 9 clean closures and the whole longitudinal term | Stage 3F |
| carve amplitude vs cross-valley relief | **creates** by destroying the off-axis bypass | 0–1 % → 51–54 % with depth, shape-independent | Stage 3F |
| `U2` ↔ `R4A` factorization break | **creates** (4 residual) | ΔR4A +8…+21 m against ΔH4A ≈ 0 | Stage 3F × Stage 4A |
| `SmoothMax` inflation | **amplifies**, separate arithmetic defect | +28.5 m, 13 closures | Stage 3F |
| tributary support cut-off | **amplifies**, confluence handling | 9 closures, 11/12 declared tributaries | Stage 3F |
| decaying carve over inherited hollows | **amplifies** inherited | 1023/4: 3.65 → 6.54 m | Stage 3F |
| `H1` topology + `R4A` doubling | **owns** the deep inherited closures | 5/6 S11-T already closed on `H1`, roughly doubled by `H4A` | Stage 1 / Stage 4A |
| one-station bypass test | **mis-measures**, minor | 1–2 of 82 Drainable blocked at an intermediate station | classifier |

`DrainableWithinTrajectory` is decided by `AcrossMin[barrierIndex] ≤ H3F[low]` — seven samples at a
single station, with no connectivity and no intermediate station. The correct 1-D reduction is
`max_j AcrossMin[j] ≤ floor` over the span. Structurally unsound, empirically almost always
equivalent. `CLOSED` always implies centreline non-monotonicity by construction, so the two
properties are not independent.

## 5. Confirmed problems, owners, confidence

| # | problem | owner | confidence |
|---|---|---|---|
| 1 | `TroughProfile` peaks at `a = 0.5`, not at the toe, and sheds 7.2 m downstream | Stage 3F | **high** — keys read directly; `a_toe < 0.5` on 234/261 |
| 2 | design contract "carve depth non-increasing downstream ⇒ drainage preserved" is the wrong invariant | Stage 3F design | **high** — 1023/4 is a direct counterexample |
| 3 | `SmoothMax` is not a maximum; adds up to 25 % per pair, compounding | Stage 3F | **high** — closed form plus counterfactual |
| 4 | tributary carve painted at full depth across the trunk, then cut off | Stage 3F | **high** — station-by-station on 1014/2 |
| 5 | mountain term keyed to `U2` while the ground carries `R4A` | Stage 3F × Stage 4A | **high** — 4/4 residual measured |
| 6 | carve amplitude destroys off-axis bypasses on `H4A`-drainable lows | Stage 3F | **medium-high** — population curve; `AcrossMin` for synthetic surfaces is approximated |
| 7 | deep inherited closures are `H1` topology amplified by `R4A` | Stage 1 / Stage 4A | **high** — H1/H2/H4A measured per closure |
| 8 | S11-T is near-orthogonal to CLOSED | Stage 4A / verification | **high** — 67 of 72 closure-bearing trajectories have S11-T ≤ 7.2; 1023/5 fails S11-T with no closure anywhere |
| 9 | one-station bypass test | classifier | **high** on the defect, **high** that its impact is 1–2 cases |
| 10 | 40 m positional matching produces false "removed" | verification | **high** — 1023/4 moved 60 m and was counted as removed |

**Refuted hypotheses.** (a) That the trough peak's *position* relative to the toe is the problem —
T3 puts the peak at the toe and still leaves 50 of 52. (b) That the six >7.2 m created closures are
the same mechanism at larger amplitude — 5 of 6 are island-made with the carve supplying ≈5 %.
(c) That `aLow ≈ 0.5` indicates the trough pins the lows — `H4A`'s own closures sit at 0.520 against
0.511, no signal. (d) That `SmoothMax` or the foreign corridor explains most of B9 — together they
account for 10 of 62 closures and 31 of 189 m. (e) That the one-station bypass test materially
distorts the counts.

**Open questions.** (1) Must Stage 3F *remove* inherited depressions, or only not worsen them? The
whole amplitude question turns on this. (2) Can an envelope constrained by `dH4A/ds` be expressed
without making the carve stateful along the curve — it is currently a pure function of position,
while the envelope is a function of upstream history, so it needs a precomputed per-arc array
alongside `FinalValleyTrajectory`. (3) Does the T3-tier amplitude of 7.2 m survive at all, given
that the knee of the drainage curve sits at 12 m.

**Not fixable within the current depth limit.** The deep inherited closures: 1032/8 needs 26.9 m of
carve to satisfy the rule and still reads 40.8 m at `A_max = 7.2`; 1017/2 needs 55.8 m; 1023/5 needs
68.8 m and has no closure to fix. At 7.2 m the envelope leaves 36 inherited closures and 293.4 m of
inherited barrier depth untouched.

## 6. Repository state

```
HEAD                e9c69d0  Diagnose Stage 4A B10 contract failure
production          UNCHANGED — no file under Assets/Scripts was modified in this phase
working tree        clean;  git diff --check clean
commits / pushes    none
```

Test baseline, last verified at the close of this phase: **Stage 4A 16/16 · Stage 3F 16/16 ·
F3 7/7 · full EditMode 595/595** (0 failed, 0 skipped). All probes were temporary Editor-only files
(`ZZTemp*Probe.cs`) deleted after each run; `TerrainGenDump/` output was removed from the repository.

Caveat on reproducibility: the session scratchpad holding the earlier dumps was cleared between
phases. The §3.6 sweep and the T0/T1/T3 reference rows were **re-measured** from a fresh dump;
§3.1–3.5 are carried from the earlier measurements and were not re-run. Regenerating them costs one
read-only probe of roughly 20–40 minutes.

## 7. Decisions for the next phase

1. **Whether the drainage invariant is adopted at all, and at what amplitude.** The sweep makes the
   trade explicit: 7.2 m buys 75 CLOSED and all four residual fixes but leaves every inherited
   closure; 12 m buys 29 CLOSED and 23 inherited removals at a median valley depth of 12 m; 20 m
   buys 21 for 20 m. This is a landform decision, not a drainage one — `CorridorIncisionMeters` was
   derived from the tier rule, and 12–20 m breaks tier T3.
2. **Whether `SmoothMax` stays in `CarveField.Depth`.** It is a separate arithmetic defect and can
   be decided independently of the envelope question.
3. **Whether confluence handling hands depth over continuously** instead of painting a tributary's
   full incision across its trunk and cutting it off.
4. **Whether the classifier's bypass test becomes a path test**, and whether positional matching is
   replaced by something that does not report a 60 m displacement as a removal.
5. **Whether S11-T is restated** as a relief metric rather than reported alongside drainage.

Nothing in 1–5 is settled, and no implementation path for 1 has been shown to be architecturally
admissible.

## 8. Entry point for the next agent

Read, in order: this document; `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` (§§19–22 for B4/B7/B8/B9/
B10); `OVERWORLD_TECH_DEBT.md` items 19–21 and B10; `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` line 575
for the invariant that this phase showed to be wrong; `PROCEDURAL_TERRAIN_STAGE_3C_VALLEY_FORENSICS.md`
§"Three properties" for the proof whose premise Stage 4A removed. Then read `CarveField.cs`,
`CarvedElevation.cs` and `Stage3FVerification.cs` — the formulas in §2 are quoted from them and the
classifier's decision tree is what every count in §3 means.

Start from decision 1 in §7, phrased as a landform question: **is a valley of 12–20 m incision
acceptable for tier T3, or does drainage have to be bought some other way than depth?** Everything
else in §7 is cheaper and can follow.
