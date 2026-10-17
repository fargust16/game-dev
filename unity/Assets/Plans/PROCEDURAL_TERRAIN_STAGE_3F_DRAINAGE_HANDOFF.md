# Procedural Terrain — Stage 3F Drainage and Valley Carve, Research Handoff

Version: 0.7

Status: Draft — research record. Nothing in it is an approved design decision; §22 is a set of
proposed contracts awaiting owner review.

Last Updated: 2026-10-17

Depends On:
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md),
[PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md), [OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md)

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Stage 3F valley carve,
[90 Glossary](../../../90%20Glossary.md) § Final Terrain Surface and the terms after it,
[AGENTS.md](../../../AGENTS.md) § Proposed Decisions Awaiting Owner Review,
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md) item 21

---

## 0. Phases at a glance

| phase | date | question | outcome | sections |
|---|---|---|---|---|
| 1 causal investigation | 2026-10-14 | why does Stage 3F produce closed valleys, and which are Stage 3F's to own | mechanisms and owners named; recorded in commit `5a05b13` | §§1–8 |
| 2 landform decision (D1–D3) | 2026-10-14 | which drainage invariant, which valley geometry, where the 7.2 m bound applies | measured against plan-view drainage (S8, research requirement D1): no axial variant satisfies it; depth does not buy it | §9 |
| 3 planar carving | 2026-10-15 – 16 | can a carve that works on the 2-D surface avoid new depressions and keep natural form | E-family hybrid meets D1 (independently verified); conflicts between the requirements recorded | §10 |
| 4 thalweg shift | 2026-10-16 | can the carve floor move to the `H4A` low inside the existing corridor | no — the low lies outside the corridor at 79.5 % of plain stations; F is not worth prototyping | §11 |
| 5 decision memo + technical check | 2026-10-16 | is a band-local raster correction workable; how does it depend on the lattice; what do valley-ending rules cost | 4-connected correction holds on the lattice and on any triangulation; not resolution-invariant; no ending rule tested is a solution | §§14–15 |
| 6 isolated prototype of E | 2026-10-16 | does E with a 4-connected correction on the 1025 lattice meet the acceptance criteria | yes on the lattice; the prototype's mixed point-query contract fails continuity and between-node subtractivity, a lattice-only reading does not | §16 |
| 7 the served surface | 2026-10-16 – 17 | how does Unity read heights between nodes, and does E keep its invariants on that reading | `SampleHeight` and the collider read triangles split SW→NE on the quantised lattice, not bilinear; E keeps every §16 invariant on that reading | §18 |
| 8 slopes and surface rules | 2026-10-17 | which slope each consumer reads, and how far each reading is from the served triangles | seven slope readings in the code; the smoothed ones (D2, D3, D4) are close on gentle ground and far on steep, twisted and E-made geometry; E changes the rules' output 4–10× more than the definitions differ | §19 |
| 9 physical connectivity and tree footing | 2026-10-17 | reachability on the triangle graph, H4A against E; where trees actually stand | E raises T-edge reachability from 94.53 % to 95.04 % of land; single passages within ±1° of 45° decide 17–34 thousand m² on 5 of 8 seeds; D6 over-reports; no tree stands on a triangle steeper than 45° | §20 |
| 10 the production `CharacterController` in Play Mode | 2026-10-17 | which graph model describes the real controller | sharp limit at 45° (44.995° always walks, 45.005° never); T-edge never claimed a passage the controller failed; vertex passages walk in 10 of 11 steep-touching cases; D3 is wrong both ways; jumping climbs every tested slope up to 70° | §21 |

Methods shared by phases 2–4: §12; phases 5–6 reuse them (§15, §16); phases 7–10 state their own
(§§18–21). Proposed contracts: §22. Current position and open decisions: §23 (§13 is the position
after phase 4, §17 after phase 6). Reading order: §8.
Production code, public API, tests, Stage 4A, the frozen Stage 1–3E files and the Stage 3C
trajectories are unchanged through all ten phases.

## 1. Status and scope

**Phase 1 (causal investigation, 2026-10-14) — complete. No production code was written or changed.**
Phases 2–4 are recorded in §§9–11.

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

**Correction (phase 2, verified against the original sweep script and its data).** `A_max` capped the
whole carve, mountain term included: on 1 780 of 3 394 stations above the toe the shipping carve
exceeded 7.2 m and was clipped, so the 7.2 m row describes flattened massif valleys, not the shipping
amplitude. The "carve p50 / p90 / max" columns are the per-trajectory mountain-head maxima
(21.35 / 56.56 / 75.63 m). "Inherited" counts variant lows matched within 40 m, not distinct `H4A`
closures: at 7.2 m the 36 inherited lows match 25 distinct `H4A` closures (11 of them carry more
than one match), which is why inherited + removed is 47 in that row and 37 in the others. The sweep
is one-dimensional — self-only carve, cross-valley minimum approximated by a uniform shift. Phase 2
re-measures the question on the plan-view surface (§9), where all 36 `H4A` closures turn out to be
route closures, not depressions.

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
inherited barrier depth untouched. *(Phase 2: these are axial route closures — the declared
trajectory crossing rising ground — and none of them is a plan-view depression of `H4A`; see §9.)*

## 6. Repository state

```
HEAD                5a05b13  Close Stage 3F drainage causal investigation (phase 1, this handoff)
production          UNCHANGED through phases 1–4 — no file under Assets/Scripts was modified
phases 2–4          no repository file changed; every probe ran outside the repository (§12)
```

Test baseline, last verified at the close of phase 1: **Stage 4A 16/16 · Stage 3F 16/16 ·
F3 7/7 · full EditMode 595/595** (0 failed, 0 skipped). All probes were temporary Editor-only files
(`ZZTemp*Probe.cs`) deleted after each run; `TerrainGenDump/` output was removed from the repository.

Caveat on reproducibility: the session scratchpad holding the earlier dumps was cleared between
phases. The §3.6 sweep and the T0/T1/T3 reference rows were **re-measured** from a fresh dump;
§3.1–3.5 are carried from the earlier measurements and were not re-run. Regenerating them costs one
read-only probe of roughly 20–40 minutes.

## 7. Decisions left open by phase 1

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

**What phases 2–4 measured against these decisions** (still not decisions):

1. The amplitude question is the wrong question. Drainage is a plan-view property; on the plan-view
   S8 scan no axial envelope satisfies it at 7.2 m or 12 m, and 12 m buys nothing there (§9).
   Plan-view drainage is reached at the 7.2 m plain bound by a 2-D construction (§10). The 12–20 m
   "knee" came from a sweep that also clipped the mountain carve (§3.6 correction).
2. `SmoothMax` accounts for about 18 of 205 plan-view depressions of the axial yield variant and
   makes the plain carve exceed 7.25 m on about 13 ha; holding the 7.2 m plain bound needs a hard
   maximum (§§9–10).
3. Every corrected variant of phase 3 leaves no depression within 60 m of the 29 confluences
   (`H3F`: 24). Shifting the trunk floor sideways breaks the confluence again (§11).
4. The axial `CLOSED` classifier is diagnostic only: on `H4A` all 36 are route closures with no
   plan-view depression. Plan-view S8 plus an independent scanner replaces it as the drainage
   measure (§12).
5. Unchanged.

## 8. Entry point for the next agent

Read, in order: §0 and §17 of this document; §16 for the prototype and §15 for the technical check;
§12 for what every number means; §§9–11 for the evidence behind E; then §§1–7 for the phase-1
causal model. Background: `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md`
(§§19–22 for B4/B7/B8/B9/B10); `OVERWORLD_TECH_DEBT.md` items 19–21 and B10;
`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` line 575 for the invariant phase 1 showed to be wrong;
`PROCEDURAL_TERRAIN_STAGE_3C_VALLEY_FORENSICS.md` §"Three properties" for the proof whose premise
Stage 4A removed. Code: `CarveField.cs`, `CarvedElevation.cs`, `Stage3FVerification.cs`,
`DepressionFill.cs`, and `IslandReliefMetrics` in `TerrainGenIslandReliefDump.cs` (the S8 scan).

Open decisions are listed in [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain —
Stage 3F valley carve. Start from the point-query contract and the valley-ending semantics (§17):
the correction itself is measured; those two decisions are what stands between the prototype and a
production design.

## 9. Phase 2 — landform decision (D1–D3), 2026-10-14

**Research requirements set by the project owner for this phase** (requirements for the experiment,
not approved Stage 3F invariants):

- **D1 — plan-view drainage.** Stage 3F creates no new significant plan-view depression (S8, §12)
  relative to `H4A` outside explicitly permitted basins and does not deepen inherited ones. The
  axial `CLOSED` classifier is an auxiliary diagnostic, not a substitute.
- **D2 — behaviour where the allowed incision is exceeded.** The carve yields to the island instead
  of cutting an unbounded gorge — a hypothesis for visual checking.
- **D3 — scope of the bound.** The 7.2 m `CorridorIncisionMeters` bound applies to incision on the
  plain below the massif toe; it does not bound the mountain carve.

**Surfaces.** `H4A`; `H3F` (production `CarvedElevation`); **P7 / P12** — the axial drainage envelope
`C[i] = min(max(C1[i], C[i−1] + ΔH4A, 0), bound[i])` at 2 m stations, `C1` the production self-only
axis carve, `bound` the production allowance and sea margin, plus `max(A, C1)` only at arc ≥ toe
(`A` = 7.2 / 12 m); **Y7** — P7 followed by a backward yield `G[i] = max(F[i], min(G[i+1], H4A[i] −
Cm[i]))`: the floor never sits below what it drains to along the axis and never above `H4A` minus
the mountain term; a tributary is bounded by its trunk's floor at the confluence. A variant enters
2-D by adding `Across(1 − w) · Δ(arc)` to each corridor's own term inside the unchanged aggregation;
with `Δ = 0` the evaluator reproduces production `CarvedElevation` bit for bit on every seed.
Diagnostics: **Y7max** (Y7 with a hard maximum instead of `SmoothMax`), **Y7fill** (`min(H4A,
priority-flood fill of Y7)`), **M0** (mountain term only: production code with
`CorridorIncisionMeters = 0`).

| surface | S8 created (plain / flank) | depth sum / p50 / max | ≥ 2 m | axial CLOSED (in S8 depression / route only) | interior gaps (trajectories) |
|---|---|---|---|---|---|
| `H4A` | 6 own (S8-M, max 2.32 m) | — | — | 36 (0 / 36) | — |
| `H3F` | 211 (114 / 97) | 252.7 / 0.90 / 5.27 m | 31 | 96 (63 / 33) | 3 (3) |
| P7 | 222 (131 / 91) | 210.9 / 0.71 / 5.31 m | 18 | 110 (66 / 44) | 19 (18) |
| P12 | 217 (126 / 91) | 179.9 / 0.63 / 3.27 m | 11 | 67 (43 / 24) | 19 (18) |
| Y7 | 205 (114 / 91) | 142.7 / 0.55 / 3.27 m | 2 | 34 (12 / 21) | 36 (29) |
| Y7max | 187 (96 / 91) | 125.9 / 0.55 / 3.27 m | 2 | 21 (2 / 19) | 36 (29) |
| Y7fill | 0 by construction | — | — | 21 (0 / 21) | 37 (30) |
| M0 | 59 (0 / 59) | 72.2 / 1.07 / 4.62 m | 10 | 40 (4 / 34) | — |

Inherited depressions deepened by more than 0.05 m: `H3F` 1, M0 3 (all on the flank: 1013, 1026,
1038), every other surface 0.

Measured:

- **Every axial closure of `H4A` is a route closure.** None of the 36 lies in a plan-view
  depression; `H4A` is plan-view drainable except its six S8-M depressions. The inherited closures
  are places where a declared trajectory crosses rising ground, and water leaves sideways.
- **Plan-view creation is robust to the thresholds and the matching rule.** Over depth 0.10–0.50 m
  and area 0–400 m², `H3F` creates 142–246 and Y7 111–256; the three matching rules differ by ±1.
- **Mechanisms.** The mountain term alone creates 59 depressions, all on the flank, on 27 of 32
  massif seeds. The trough floor follows `H4A`'s lateral slope, so the carved cross-section low sits
  5–12 m off the axis (1023/4, 1032/6, 1010/0) while `H4A`'s own low lies outside the corridor.
  `SmoothMax` accounts for about 18 (Y7 → Y7max).
- **Depth does not buy plan-view drainage.** P12 creates 217 against P7's 222 and `H3F`'s 211. It
  spends depth above 7.25 m on 8.9 % of the plain valley length (112 trajectories); `H3F` already
  exceeds 7.25 m on 4.2 % through `SmoothMax`.
- **The yield's gaps sit on trajectories that touch the coast and turn back inland**: 24 of 29
  (all 7 with `dGate` ≥ 0.5, 17 of 36 with 0 < `dGate` < 0.5, 5 of 218 with `dGate` = 0).
- The forward envelope also deepens the flank wherever `H4A` rises along the axis: 183 of 261
  trajectories, p90 2.2 m, max 14.4 m.
- Traversability barely moves: land steeper than 45° and reachable land change by ≤ 0.06 percentage
  points; carve-made plain walls above 45° appear on 0–1 stations per half-width bin. The analytic
  `AcrossProfile` reaches 45° at a depth of about 0.37 half-widths, which the 2.73 m lattice smooths.

Visual judgement (hillshade, not in-engine): 12 m cuts read as straight railway-like cuttings
(1032/8); the yield splits route-barrier valleys into segments that restart abruptly mid-slope.

Phase-2 conclusion: no axial variant satisfies D1. Plan-view drainage is a property of the 2-D
construction, not of the amplitude.

## 10. Phase 3 — planar carving and natural valley preservation, 2026-10-15 – 16

**Research requirements set by the owner for this phase:** (1) no new significant plan-view
depression relative to `H4A` by an independent S8 scan, except explicitly permitted basins; (2) no
deepening of inherited depressions; (3) keep the natural thalweg and cross-section form; (4) keep
valley continuity where the valley should continue; (5) no reappearing segments and no artificial
ditches; (6) keep the 7.2 m plain incision bound and assess the mountain carve separately; (7) do not
worsen traversability or coastal geometry. Whether all seven are compatible was part of the
question.

### 10.1 What Y7fill does, and at what cost

- Priority-flood raises every carve-made depression to its spill level. Because `H4A` is drainable,
  an uncarved cell always has a drainage path, so the fill can only change carved cells. Measured:
  0 uncarved cells changed and 0 cells outside the corridors; every change lies within 0.93
  half-widths (≤ 115 m) of the axis; 28.53 ha, 87 646 m³, raise ≤ 3.27 m.
- It removes 205 depressions at a median of 138 m³ each. 81 % of them have their bottom off the
  valley floor (`w` > 0.2) — the lateral-tilt mechanism.
- **Side effect:** flat, lake-bed floors on the filled patches. Y7's gaps stay (the fill does not
  touch the route).
- **Local equivalence:** filling each carved component from its own uncarved ring (outlet height =
  `H4A`'s own spill level) reproduces the global fill exactly — 0 cells differ.
- **Achievable without any fill:** variant A removes 204 of the 205 at the same places.

### 10.2 Variants

- **A — planar floor reference.** `c(p) = Across(1 − w) · min(D(arc), max(0, H4A(p) − F(arc)))`,
  `D` / `F` the Y7 axis carve and floor, hard maximum between corridors: the carve never cuts below
  the axis floor at its own arc.
- **B1 / B2 — `H3F` plus band-local correction.** B1 fills each carved component locally. B2 first
  breaches: a least-cost path from each pit bottom to lower ground inside the carved component,
  lowered linearly, refused where the plain bound or the allowance would be exceeded; the rest is
  filled.
- **C — A plus valley termination.** Below the toe the carve ends at the first stretch under 1 m that
  would reappear (≥ 1 m) further down; nothing downstream is carved. Trajectories are not changed.
- **D (diagnostic)** — production terms bounded by the production axis floor, hard maximum, then a
  band-local fill.
- **E — hybrid.** Production terms on the massif, A's template on the plain, blended by smoothstep
  over 60 m below the toe, hard maximum, then a band-local fill. **Epre** is E before the fill.

| | S8 created (IDS) | massif carve vs `H3F` | plain > 7.25 m | interior gaps (trajectories) | correction |
|---|---|---|---|---|---|
| `H3F` | 211 (209) | — | 13.0 ha | 3 (3) | — |
| A | 2 (2) | −23 %, up to 35.7 m shallower | 0 | 44 (35) | none |
| B1 | 0 (0) | unchanged | 12.1 ha | 3 (3) | fill 336 171 m³ |
| B2 | 0 (0) | unchanged | 12.2 ha | 3 (3) | 564 breaches (248 refused by the bound), 12 581 m³ lowered (≤ 4.95 m), fill 218 795 m³ |
| C | 2 (2) | −23 % | 0 | 13 (12) | 30 trajectories terminated, 13.3 km = 8.5 % of plain valley length |
| D | 0 (0); 183 before fill | −20 % | 0 | 15 (12) | fill 380 846 m³ |
| **E** | **0 (0)**; 100 before fill (2 plain, 98 flank) | unchanged (77.97 vs 78.06 Mm³) | 0 | 39 (30) | fill 41 261 m³ |

Further measurements:

- A's two residual depressions (0.26 and 0.27 m) sit on an outer valley wall where the corridor
  contains a lateral `H4A` crest — the one case the floor-reference argument does not cover.
- All 30 terminations of C are inland (none within 30 m of the coast); 26 of them are on trajectories
  with `dGate` > 0.
- 50 of E's 100 pre-fill flank depressions lie inside declared Stage 3E cirques (`H3F`: 49 of 211).
- **Natural low:** `H4A`'s cross-section minimum within ±1.5 half-widths lies inside the corridor at
  19 % of plain stations. The carved low sits off the valley floor (`w` > 0.2) at 71–72 % of stations
  for `H3F` and B, 52–54 % for A, C and E.
- **Confluences (29):** `H3F` 24 depressions within 60 m, every corrected variant 0. **Coast:** `H3F`
  31 within 200 m of the shore, corrected variants 0. **Plate-level crossings:** `H3F` 6, corrected 0.
- **Traversability:** B and E unchanged; A, C and D +0.09 percentage points of land steeper than
  45° and −0.4 points of minimum reachability.
- Axial `CLOSED` (diagnostic): `H4A` 36, `H3F` 97, A 16, C 22, E 21, B1 57, B2 64.

Visual judgement (hillshade, not in-engine): A and C leave a thin dark step at the massif toe where
the floor rule cuts the Stage 4A apron ring, and they change the flank grooves; B2's breach channels
read as narrow zig-zag notches; B1 leaves flat "bulbs" at tributary ends; Y7, A and E split
route-barrier valleys into segments that restart abruptly; C ends them mid-plain.

**Conflicts recorded.** (a) A natural thalweg and frozen trajectories (measured in §11). (b) Where a
declared route rises more than the plain bound can cut, only a gap, a termination or a flat-filled
continuation exists: continuity, no reappearance, the 7.2 m bound and D1 cannot all hold there.
(c) Reshaping the flank structurally (A, C, D) removes the mountain-term depressions but deforms
the mountain valleys; keeping the production flank needs a correction step.

Phase-3 recommendation (not approved): an **E-family hybrid** for the next prototype.

## 11. Phase 4 — thalweg shift inside the corridor (E against F), 2026-10-16

**F.** On the plain the cross-section centre moves toward the `H4A` minimum within the existing
half-width; the profile becomes asymmetric, each side spanning from the shifted line to its own
corridor edge; the 7.2 m forward envelope and the yield are recomputed along the shifted line on
analytic `H4A`; the massif part is E's. Shift ≤ 0.6 half-widths, ±50 m moving average, lateral drift
≤ 1:8 (authoring defaults). **G** tests sensitivity: shift ≤ 0.8, ±10 m, drift ≤ 1:4. With zero shift
F reproduces E on every metric. Fpre and Gpre contain no fill; F and G apply E's band-local fill for
a like-for-like D1 comparison.

**Where the natural low is** (2 m plain stations): within 0.6 half-widths of the axis 13.5 %; inside
the corridor beyond 0.6 7.0 %; **outside the corridor 79.5 %**. At half of the stations the
in-corridor minimum is the corridor edge itself. The low line is coherent (20 m change: median 0;
side flips 2.6 %). 222 of 261 trajectories have most plain stations with the low outside; 245 reach
the 0.6 limit.

| | E | F | G |
|---|---|---|---|
| S8 created before fill, plain / flank (IDS) | 100: 2 / 98 (98) | 106: 8 / 98 (104) | 105: 7 / 98 (103) |
| after the fill, S8 / IDS | 0 / 0 | 0 / 0 | 0 / 0 |
| fill on the plain | 6 435 m³ | 10 379 m³ | 11 165 m³ |
| carved low from the axis, median | 0.26 half-widths | 0.64 | 0.80 |
| carve cut into the natural low where it is reachable | 31 % | 25 % | 29 % |
| shifted line within 0.1 half-widths of the low where reachable | — | 23 % | 36 % |
| plain carve volume | 38.34 Mm³ | +3.2 % | +5.4 % |
| relocation against E, raised / lowered | — | 8.97 / 10.19 Mm³ | 10.08 / 12.14 Mm³ |
| interior gaps (trajectories) | 39 (30) | 52 (41) | 55 (44) |
| carve-made plain walls > 45° | 2 | 61 (45 at half-width < 25 m) | 236 |
| depressions within 60 m of confluences before fill | 0 | 6 | 7 |

All three keep the plain bound (max 7.22 m, nothing above 7.25 m). Land steeper than 45°: 4.278 /
4.281 / 4.293 %; minimum reachability 91.297 / 91.279 / 91.277 %.

Visual judgement: F and G move the groove toward the corridor edge instead of reshaping it. In
narrow corridors it becomes an edge trench with a serrated wall (1010/0: 47–50° against 33° for E).
At 1014/2 the shifted trunk floor leaves the tributary's junction. The segmentation of 1032/8 is
unchanged.

**Conclusion.** Inside the existing corridor the natural thalweg is out of reach at about 80 % of
plain stations, and where it is reachable F does not cut into it more often than E. F is not worth
prototyping. **Conflict recorded:** keeping the natural thalweg, keeping the Stage 3C trajectories and
keeping the corridor width are mutually exclusive on this data.

## 12. Methods and reproducibility (phases 2–4)

- **Environment.** Unity 6000.5.7f1 in batch mode on a minimal standalone project outside the
  repository. It holds copies of `World/TerrainGen`, `World/Seed`, `World/Town/Geometry`,
  `WorldScale.cs` and `HydrologyGrid` / `DepressionFill` / `HydrologySettings` at HEAD `5a05b13`, an
  empty package manifest, and the probe under `Assets/Editor`. Before every run the seven canonical
  `H4A` hashes match: 110201 `e2bad571f4af76c7`, 424242 `2076ecf78a34483b`, 20260905
  `4b7ae0f5a6cfaae8`, 7 `6a7ad6edd08db7d9`, 1337 `f86e8c15d7105be2`, 256 `a218e07fe1b2cd74`,
  777 `48012b8c997a54df`.
- **A .NET build of the same sources is not a substitute.** Stage 1 already differs by 1–3 cm, and
  three sweep seeds (1014, 1035, 1037) get different trajectories. It is useful only for debugging
  probe code.
- **Population.** Seeds 1000–1039: 32 carry a massif, 261 trajectories. Focus trajectories 1023/4,
  1032/8, 1014/2, 1010/0, 1032/6 and 1010/1; the last two were picked by rules fixed before the run
  (largest S11-T with `dGate` = 0; longest plain without any closure, gap or depression).
- **S8 scan** — `IslandReliefMetrics.FillDepth` + `ScanDepressions`: priority flood (ε 1 mm,
  8-neighbour, outlets = the connectivity-checked ocean and the world border); depth = fill − height on
  land; 4-connected components deeper than 0.05 m; significant at ≥ 0.25 m and ≥ 100 m²; lattice
  1025² at 2.734 m.
- **IDS, the independent scan.** Cells sorted by height and merged with a union-find; when a component
  without an outlet joins one with an outlet, the merging cell's height becomes the spill level of
  all its members. Same connectivity and component rule, no epsilon, separate code. S8 and IDS agree
  within ±4 on every surface. A variant built with priority flood (B, D, E, F, G) counts as meeting
  D1 only through IDS.
- **Matching against `H4A`.** By overlap of component cells: inherited when a significant `H4A`
  component is overlapped, deepened when deeper by more than 0.05 m. A 2-cell dilation and a
  deepest-cell distance of 40 m change the counts by ±1.
- **Plain and massif.** Along a trajectory, plain means arc ≥ `FinalValleyTrajectory.ToeArcMeters`;
  for volumes, a cell is plain when `U2` ≤ 0.1 m. Ambiguities: overlapping corridors at confluences,
  the Stage 4A apron beyond the toe counts as plain, and `SmoothMax` can exceed a per-corridor cap.
- **Integrity.** Carve on the axis every 5 m; under 1 m counts as absent; a gap is an absent stretch
  with carve ≥ 1 m on both sides. Phase 2 sampled analytic surfaces, phases 3–4 the lattice
  (bilinear) — hence Y7fill 37 / 36 gaps and `H3F` 96 / 97 axial closures.
- **Thalweg.** Plain stations every 20 m; the cross-section is sampled at 2 m over ±1.5 half-widths.
- **Walls.** The steepest lattice slope (central differences) between 0.1 and 1.0 half-widths; a wall
  is carve-made when `H4A` is at or below 45° there.
- **Probes.** The probe sources and their data were temporary and are not kept in the repository; the
  definitions above are enough to rebuild them. Phase 3's sweep ran as two Unity runs with identical
  code and identical hashes, after a session interruption.

## 13. Position after phase 4 (superseded by §17)

- Production is unchanged; Stage 3F remains an experimental downstream carve.
- **Measured:** plan-view drainage at the 7.2 m plain bound is reachable without raising the bound —
  a planar floor reference on the plain plus a band-local correction on the massif flank (E),
  verified by an independent scanner. Axial envelopes, extra depth and an in-corridor thalweg shift
  do not reach it.
- **Recommended next prototype (not approved): the E family.**
  - Plain: the floor-referenced template with a hard maximum, driven by per-arc arrays.
  - Massif: the production carve with a band-local correction.
  - An explicit policy for routes that rise more than the bound allows.
- Decisions the owner has to take first are listed in
  [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Stage 3F valley carve.

## 14. Phase 5a — decision memo and the owner's working decisions, 2026-10-16

The memo compared three ways to make the E-family drain in plan view and three ways to treat routes
that cannot keep descending:

- **Correction.** A band-local raster correction on a fixed lattice before the bake (recommended);
  an analytic correction (not demonstrated: the closest analytic constructions, A and D, still leave
  depressions or reshape the flank, §10); no correction with a weaker D1 (would need the
  "no closed basins" rule of `WORLD_RELIEF_PRINCIPLES.md` amended). Argued, then measured in §15: a
  4-connected priority flood stays drainable on any triangulation of the lattice and under
  aligned bilinear refinement, an 8-connected one does not have to; the fill is 1-Lipschitz in the
  heights, and a monotone 16-bit quantisation cannot create a depression.
- **Routes.** Gap, termination or flat-filled continuation, with coastal turn-backs treated
  separately. Ending a valley's geometry is a Stage 3F realisation fact; it does not change Stage 3C
  topology.

**Working decisions for the research prototype (owner, 2026-10-16; research only, not production):**
canonical `FieldResolution` = 1025; 4-connected band-local priority flood; in corrected cells the
corrected lattice is authoritative and point queries are bilinear on it; corrected height ≤ `H4A`;
Stage 3C untouched; no automatic `Mouth` or `Barrier` — `Declared` ends, gaps measured and shown.

## 15. Phase 5b — technical check, 2026-10-16

Unity 6000.5.7f1, sources at HEAD (production code as at `5a05b13`), canonical hashes matched. The
probe reproduced phase-3 E bit for bit. Seeds 110201, 424242, 20260905, 7, 1010, 1014, 1023, 1032
(70 trajectories, 41.9 km of plain valley).

**Connectivity, 1025.** E8 (phase 3) and E4 both create 0 significant depressions under S8, an
8- and a 4-connected independent scan, and both uniform triangulations; also after 16-bit
quantisation and after aligned bilinear refinement to 2049. Counterexamples: E8 on a 513 lattice read
4-connected — 4 depressions (0.29–0.41 m); either variant read as "analytic Epre at 2049 plus the
interpolated raise" — 2 (8-connected) / 3 (4-connected) depressions of 0.25–0.32 m, all on the flank.
E4 fills 15 577 m³ against E8's 14 191 m³ (+9.8 %): 89 places where only a diagonal drained,
≤ 0.18 m, never lower than E8.

**Resolution** (structures fixed from the 1025 build, lattices point-evaluated). D1 holds when the
correction is recomputed natively at 513, 1025 and 2049. The surfaces do not converge: E8 fills
12 243 / 14 191 / 16 928 m³, E4 13 932 / 15 577 / 19 599 m³; common nodes differ by up to 0.30–0.32 m
(513 → 1025) and 0.17–0.23 m (1025 → 2049); raised components exist at one resolution only (E4
1025 → 2049: 11 coarse-only, 26 fine-only). Changing `FieldResolution` literally also moves Stage 3C
trajectories by up to 3.7 m (the trajectory builder reads the Stage 1 lattice gradient); `H4A` itself
does not change. The correction costs 0.3 s per seed at 1025 and 1.4 s at 2049, against 11 s and 43 s
to evaluate the lattice.

**Valley ends** (E8, 1025; 19 / 12 / 8 trajectories have `dGate` > 0 / 0.01 / 0.05):

| rule | ends (Mouth / Barrier) | gaps on the lattice (stations) | valley length lost vs E | ends ≤ 30 m / > 50 m from the shore |
|---|---|---|---|---|
| Declared | 0 | 13 on 11 trajectories, 1 615 m (11) | 0 | — |
| Mouth, > 0.01 | 12 / 0 | 4 (3) | 1 930 m (4.6 %) | 3 / 8 |
| Barrier | 0 / 10 | 4 (0) | 1 765 m (4.2 %) | 0 / 10 |
| Mouth + Barrier, > 0.01 | 6 / 9 | 3 (0) | 2 645 m (6.3 %) | 0 / 15 |

- `Mouth` as defined (the running minimum of the Stage 4A gate before it recovers) is not a mouth:
  8 of the 12 ends lie 60–160 m inland on the coastal slope, cutting off a valley up to 3–4 m deep,
  while the trajectory reaches the sea later at its declared end; only 3 ends lie within 30 m of the
  shore.
- `Barrier` is stable (no end moved under a ±2 cm perturbation of `H4A`) and creates no depression,
  but turns gaps into lost length and ends valleys 52–342 m inland. Worst case: 110201/5, a 95 m gap
  becomes 610 m lost.
- The lattice gaps left after an ending rule (3–4) are reading artefacts — neighbouring corridors
  re-carving the axis, or the carve hovering at 1 m — with 0 gaps on the corridor's own stations.
- Plain incision by arc class: ≤ 7.200 m on the plain (arc ≥ toe + 60 m). The 7.218 m maximum seen on
  cells with `U2` ≤ 0.1 m lies above the toe by arc, where the production allowance applies.

## 16. Phase 6 — isolated prototype of E, 2026-10-16

**Prototype.** E's carve with `Declared` ends; a 4-connected band-local priority flood on the 1025
lattice (carved components only, outlets at `H4A`'s own spill level, result `min(H4A, fill)`); a cell
is *corrected* when any corner was raised; the point query is bilinear on the corrected lattice in
corrected cells and E's analytic carve elsewhere (R1, the working contract). Built dump-first in a
standalone Unity project, outside the repository. Population: seeds 1000–1039 plus the seven
canonical seeds — 47 seeds, 36 with valleys, 299 trajectories.

| check | result |
|---|---|
| Canonical `H4A` hashes | 8 / 8 match |
| Corrected lattice, rebuilt in the same process / in a second Unity process | identical hash on 47 / 47 and 47 / 47 |
| New significant depressions vs `H4A` at 1025 (S8, 8- and 4-connected IDS, both triangulations) | 0; 0 deepened. Before correction: 117 (S8); `H3F`: 243 |
| Point query sampled at 2049 (S8, IDS8, IDS4) | 0 new |
| Inherited `H4A` depressions (6) | 3 kept unchanged (1006, 1019, 1027), 3 filled (1013, 1026, 1038) — they lie inside carved components |
| Nodes above `H4A` / land–sea mask changes / changed sea nodes | 0 / 0 / 0; the `min(H4A, ·)` clamp acted on 4 nodes |
| Point query at every node vs the lattice | 0 bit mismatches |
| Plain incision by arc class | plain (arc ≥ toe + 60 m) ≤ 7.200 m, blend ≤ 7.208 m; cells above the toe with `U2` ≤ 0.1 m up to 7.223 m (50 cells > 7.21 m; `H3F` there up to 8.55 m) |
| Massif carve volume | 91.638 vs `H3F` 91.755 Mm³ (−0.13 %) |
| Correction volume | 57 370 m³ (seeds 1000–1039: 46 714 m³ vs 41 261 m³ for phase-3 E8, +13.2 %); max raise 4.37 m; flank 47 191 m³, plain 10 026 m³, blend 154 m³; within 200 m of the shore 2 245 m³ (max 0.45 m); within 60 m of junctions 253 m³ (max 0.16 m) |
| Against the technical check (8 seeds) | volume, max raise and all 78 raised components identical |
| Traversability | land > 45°: 4.475 % (`H3F`, Epre and E alike); minimum reachability 91.297 % vs `H3F` 91.301 %; carve-made plain walls 2 (`H3F` 3). Per seed, E exceeds `H3F` by more than 0.01 percentage points on 1010 (+0.012) and 1034 (+0.015) — from the plain template, before correction |
| Junctions (31) | 0 depressions within 60 m; 3 tributaries reach their junction with < 1 m of carve on E, Epre and `H3F` alike |
| Toe transition | axis-carve step per 5 m in the 60 m blend ≤ 0.68 m (Epre 0.67, `H3F` 0.42); the correction adds none |
| `Declared` gaps | 41 on 34 trajectories, 4 645 m, all on the plain, 36 on coastal turn-backs; shown, not removed |

**Gaps are not masked.** On 16 seeds the lattice before and after correction gives identical gaps on
every trajectory. Gap lists differ only between *readings*: analytic against lattice changes 17
trajectories — boundaries move 5–25 m, two short gaps (10–20 m) appear, and 1016/5's 250 m gap reads
as a valley tail on the lattice.

**The working point-query contract (R1) fails two checks between nodes.**

- Continuity: across the edges of corrected cells the bilinear side and the analytic side differ by
  up to 1.09 m (seed 1026); 73 of 112 440 edge samples exceed 25 cm; tile seams (x, z = 1 400 m) up to
  0.53 m where a corrected edge lies on the seam. The jump is the analytic carve's curvature along an
  edge whose two nodes were not raised — it does not depend on the size of the correction.
- Subtractivity: 28 samples of the 2049 sampling lie more than 1 mm above analytic `H4A`, up to
  6.8 cm (1038) — bilinear on a raised node set against an analytic surface that is concave between
  nodes.

**Diagnostic readings beside R1** (16 seeds: the eight above plus 1000, 1006, 1016, 1020, 1026, 1030,
1037, 1038; the prototype itself unchanged):

| reading | new depressions at 2049 | jumps at corrected-cell edges | above its own `H4A` reading | Declared gaps vs the uncorrected lattice |
|---|---|---|---|---|
| R1 — prototype contract | 0 | up to 1.09 m | 28 samples > 1 mm (analytic `H4A`) | — (mixed reading) |
| R2 — bilinear on the final lattice everywhere | 0 | 0 by construction | 0 (bilinear `H4A`) | identical on every trajectory |
| R4 — R1 clamped by analytic `H4A` | 0 | up to 1.09 m | 0 | — |

R2 gives up the analytic sub-lattice detail outside corrected cells — mean 4 mm, worst-seed p99
5.5 cm, up to 5.36 m on a steep slope in seed 7 — which a 2.73 m heightmap cannot store anyway; the bake,
`OverworldTerrainField` and the terrain collider already read the lattice. Visually, a filled pit is a
flat terrace (up to 4.4 m deep, on the massif flank); under R1 its outline shows as a step, under R2 it
blends.

**Implementation note.** Point/lattice bit-identity under Unity's Mono depends on code shape: reading
a value back from an `out` parameter changed float rounding by 1–2 ulp on ~1.4 % of nodes. Production
`IslandElevation.At` is unaffected; a production point query must share one function with the lattice
evaluator, as `CarvedElevation` already does.

**Method.** Unity 6000.5.7f1 standalone project outside the repository, sources copied at HEAD; S8 and
the independent scanner as in §12, extended with a 4-connected scan and two uniform triangulations;
the 2049 sampling places one sample at each lattice node, edge midpoint and cell centre. Probe
sources and data were temporary and are not kept in the repository.

## 17. Position after phase 6 (superseded by §23)

- Production is unchanged; Stage 3F remains an experimental downstream carve, and E is a research
  prototype.
- **Measured on the canonical 1025 lattice:** E with a 4-connected band-local correction creates no
  new planar depression (independent scanner), deepens none, stays at or below `H4A`, keeps the land
  mask, keeps the plain incision at 7.200 m, moves the massif carve by −0.13 % and is deterministic
  across processes. It is not resolution-invariant; 1025 is canonical by decision.
- **Not met as specified:** the mixed point-query contract (R1) is discontinuous at corrected-cell
  edges (up to 1.09 m) and exceeds analytic `H4A` between nodes (up to 6.8 cm). A lattice-only reading
  (R2) meets both and is what the heightmap stores — it is a contract change, not adopted.
- **Still open by design:** `Declared` gaps (41 on 34 of 299 trajectories) until the valley-ending
  semantics is decided; `Mouth` as defined is rejected by measurement, `Barrier` has a measured price.
- Decisions for the owner: [99 Open Questions](../../../99%20Open%20Questions.md) § Terrain — Stage 3F
  valley carve.

## 18. Phase 7 — the surface Unity actually serves, 2026-10-16 – 17

**Question.** Between lattice nodes, what height does Unity serve to `Terrain.SampleHeight` and to the
`TerrainCollider` — and does E keep the §16 invariants on that reading?

**Method.** Standalone Unity 6000.5.7f1 project outside the repository, sources copied at HEAD. `H4A`
and E written through `OverworldTerrainBuilder.WriteHeights` onto tiles made by
`OverworldTerrainGrid.Create`. Readers compared: `Terrain.SampleHeight`, `TerrainCollider.Raycast`,
bilinear interpolation, and the two triangle splits of a cell. Samples on lattice nodes, edges, both
diagonals, steep and twisted cells, tile seams and random points; seeds 110201, 424242, 20260905, 7,
1010, 1026, 1034, 1038 (169–184 thousand points per seed and surface). Depressions scanned with S8,
4- and 8-connected IDS and a triangle-graph scanner (TriA) on the quantised lattice, and on the
triangle surface sampled at twice the lattice resolution.

| check | `H4A` | E |
|---|---|---|
| `SampleHeight` − triangle reading (SW→NE split) | ≤ 0.364 mm | ≤ 0.364 mm |
| collider raycast − triangle reading | ≤ 0.319 mm | ≤ 0.323 mm |
| collider raycast − `SampleHeight` | ≤ 0.389 mm | ≤ 0.389 mm |
| `SampleHeight` − bilinear | up to 2.590 m (centres of twisted cells) | up to 2.585 m |
| stored node − float lattice | ≤ 1.070 cm on every seed | ≤ 1.070 cm |
| shared tile-edge vertices differing between tiles | 0 | 0 |
| E above `H4A`: quantised nodes / triangle reading / `SampleHeight` > 0.1 mm / raycast > 1 mm | — | 0 / 0 / 0 / 0 |
| new or deepened depressions (S8, IDS4, IDS8, TriA; lattice and triangle surface at 2×) | — | 0 / 0; two inherited `H4A` depressions removed (1026, 1038) |
| `Declared` gaps on the triangle, bilinear and `SampleHeight` readings | — | 6 on 6 trajectories, 520 m, identical |
| maximum plain carve on the served surface | — | 7.2066 m (7.200 m before quantisation) |
| E lattice hash over two builds; production `FromAuthority` vs prototype `H4A`; E field vs prototype E | — | equal; 0 and 0 bit mismatches |

**Storage step.** Unity stores a height as an integer of `size.y / 32766`: normalised values `m / 32766`
survive `SetHeights` → `GetHeights` exactly, other denominators do not. On a 700 m tile that is
2.136 cm, which is the ≤ 1.07 cm rounding above.

**Result.** The served surface is the **triangle reading of the quantised lattice**: each cell split
along its SW→NE diagonal, (x0, z0) → (x0+1, z0+1); for local coordinates u ≥ v the triangle
(h00, h10, h11), otherwise (h00, h11, h01); tile-edge vertices shared exactly. Notation: world x points
east and z north (Unity's +x and +z); `step` is the lattice spacing (1400 m / 512 = 2.734375 m);
x0 = ⌊x / step⌋, z0 = ⌊z / step⌋, u = x / step − x0, v = z / step − z0; h_ij is the stored height of node
(x0 + i, z0 + j), so h10 is the eastern neighbour of h00 and h01 the northern one. A point with u = v
lies on the shared diagonal, where both triangles give the same height. A point on the seam
gives identical heights from both tiles. The bilinear reading (R2 in §16) is not what Unity serves; the
mixed reading (R1) is neither. On the triangle reading E keeps every §16 invariant, and the R1
discontinuity cannot occur, because a lattice-only reading has no analytic side to jump to.

## 19. Phase 8 — slopes, normals and surface rules on the served surface, 2026-10-17

**Slope readings in the code.** The labels below are used from here on. They are unrelated to the
research requirements D1–D3 of §9 (drainage, geometry, plain bound); where both could be meant, this
record writes "slope reading D2" and "requirement D1".

| label | where it is read | computation | what it describes |
|---|---|---|---|
| T | `TerrainCollider` → `CharacterController` | gradient of the served triangle | the surface a character stands on |
| D2 | `TerrainData.GetInterpolatedNormal` → `OverworldTerrainSampler.SampleSlope01` → `BiomeClassifier` (Mountain at `1 − n.y` ≥ 0.5, i.e. 60°) | Sobel 3×3 vertex normals of the quantised tile heightmap, bilinear between vertices (matches to p99 < 0.001° inside a tile); tile-local | smoothed; discontinuous at tile seams |
| D3 | `OverworldTerrainField.SlopeDegreesAt` → `TerrainSurfaceClassifier`, `TreeChance`, `GrassCount` | central differences on the float lattice (5.47 m base), node magnitudes interpolated bilinearly | smoothed; world-continuous |
| D4 | `LandscapeCostField` slope → POI `MaxSlope01`, road cost | central differences of `SampleHeight` at 10 m cell centres, grade / 0.47 | the 10 m scale, by design |
| D5 | `WorldHeightField.Slope01`, `Stage3Field.Reachable` | central differences on TerrainGen lattices | structural diagnostics |
| D6 | `OverworldTerrainDiagnostics.ReportReachability` | per tile, 4-connected flood over nodes, each axis step limited to `cell · tan 45°` | passes faces up to 54.7° (arctan √2); truncated at seams |
| D7 | analytic `H4A` gradient | — | read by no consumer; differs from the lattice by p95 55–58° in steep cells |

`WorldHeightField.Slope01`'s comment says it matches `SampleSlope01` "to the resampling error"; it uses
central differences against D2's Sobel kernel, and neither is T.

**Measured against T** (8 seeds, ~176 thousand points per seed and surface). A point on a cell edge,
diagonal or node touches 2–6 triangles; the reference there is every incident triangle weighted by the
share of the neighbourhood it covers. |Δ slope|, degrees, mean / worst-seed p95 / p99:

| reading | random points, `H4A` | random points, E | steep cells | E walls | corrected-zone edges (E) |
|---|---|---|---|---|---|
| D3 | 0.21 / 0.65 / 1.50 | 0.23 / 0.74 / 2.09 | p95 ≈ 26 | p95 7.1 | p95 17.8 |
| D2 | 0.20 / 0.66 / 1.65 | 0.23 / 0.76 / 2.26 | p95 ≈ 26 | p95 7.6 | p95 17.2 |
| D4 | 0.35 / 1.34 / 4.09 | 0.42 / 1.69 / 5.20 | p95 ≈ 37 | p95 14.9 | p95 33.0 |

- Collider normal against the incident triangles: ≤ 0.001° everywhere.
- D2 at a seam: the same point read from the two tiles differs by p95 2.7–3.3°, p99 7.5–7.9°,
  up to 47.6°.
- The two triangles of one cell meet at mean 0.34–0.38°, p99 3.1–4.2°, up to 80°. Grass detail cells
  are evaluated exactly on that diagonal.
- **Exhaustive land area** where D3 and T fall on opposite sides of a threshold: 28° 0.25 % (`H4A`) /
  0.34 % (E); 35° 0.16 / 0.21 %; 45° 0.107 / 0.155 %; 60° 0.081 / 0.101 %. The share of land above 45°
  agrees within 0.03 percentage points; D3 under-reports the steepest tail (≈ 7 % of the area above
  60°).
- **Production rules on the `H4A` field against the E field**, every land alphamap texel, grass cell
  and tree cell, same stream keys: the dominant surface layer changes on 1.22 % of texels (4.38 % within
  two cells of an E change), the grass count on 1.50 % (4.46 %), tree placement on 0.415 % (1.26 %).
  That is 4–10× the D3-against-T disagreement.
- The vegetation height gate (27.0 → 28.4 m) read bilinearly against the triangle reading differs by
  more than 0.1 on 6 of ~1.6 million samples.

**Result.** D2, D3 and D4 are smoothed readings: close to T on gentle ground, far from it on steep,
twisted and E-made geometry, and none of them describes what a character stands on. The change E
makes to the surface rules is a real relief change and dominates the definition difference.

## 20. Phase 9 — physical connectivity on the triangle graph, and tree footing, 2026-10-17

**Definitions, fixed before the runs.** One graph node per triangle of the quantised lattice (2 097 152
per world). Land: triangle centroid above 24 m. Walkable: slope ≤ 45° in this phase (§21 makes the
limit strict). **T-edge:** moves across shared edges; **T-vertex:** also across shared vertices.
Sources: walkable land triangles sharing an edge with ocean, the water connected to the world border.
Undirected — mutual reachability. Seam vertices compared bit for bit in all 16 seed-surface
combinations: 0 differences, so a seam edge is an ordinary shared edge.

| share of land reachable (8 seeds, area-weighted) | T-edge | T-vertex | D3 on the same graph | D6 per tile (production) | D6 on the world lattice |
|---|---|---|---|---|---|
| `H4A` | 94.53 % | 94.64 % | 94.63 % | 98.00 % of dry nodes | 98.96 % |
| E | 95.04 % | 95.05 % | 94.91 % | 98.04 % | 98.82 % |

- D6 on the world lattice reaches 3.5–4.2 % of dry nodes that T-edge does not, and never the reverse;
  per tile it also misses 0.33 % at seams.
- **`H4A` → E (T-edge):** no land lost to water; new obstacles 95 324 m² over 8 seeds — 91.8 % on E-wall
  cells (carve ≥ 2 m, E slope ≥ 25°) in carve-only zones, 0.3 % within two cells of a corrected-zone
  boundary, none inside corrected zones or outside the carve; removed obstacles 130 506 m². Isolated
  walkable area shrinks on 7 of 8 seeds while the count of small pockets grows on 6; every component E
  strands touches a new obstacle.
- **Robustness:** a 2×2 and 4×4 pixel raster per cell agrees with the graph within 0.013 percentage
  points on 7 seeds; land as "all vertices above 24 m" moves the share by −0.02 to −0.07 points;
  D6-style seeding (all land below 28 m) moves it by 0.
- **Bottleneck search** (minimax path from the shore): passages whose steepest triangle lies within
  ±1° of 45° decide 17–34 thousand m² on 5 of 8 seeds — 110201 E 44.907° (32 214 m², E wall, carve
  14.4 m), 424242 E 44.926° (17 077 m²), 424242 `H4A` 46.064° (23 687 m²), 1010 `H4A` 45.005° (25 174 m²;
  44.497° through a vertex), 1010 E 46.664° (carve 0.07 m at the passage), 1026 E 45.460° (17 327 m²),
  1034 `H4A` 44.139° (30 038 m²). A 0.1° change of the limit moves up to 1.2 % of a seed's land.
- **Tree footing** (production `PlaceTrees`, unchanged rules; dummy prototypes without colliders):
  snapped base against the triangle ≤ 0.03 mm, against the collider ≤ 0.08 mm; collider normal against
  the triangle ≤ 0.03°. Trees on triangles steeper than 45°: none on either surface. Steeper than 28°:
  15 of 104 585 (`H4A`), 58 of 105 078 (E), 40 of them on E-wall cells, all with D3 below 28° as the rule
  requires. `TreeInstance` carries yaw only, so trunks stand vertical by API. Ground within 0.5 m of a
  base lies up to 0.45 m below it.

## 21. Phase 10 — the production `CharacterController` in Play Mode, 2026-10-17

**Method.** Play Mode in a batch-mode editor; terrain written through the production path
(`OverworldTerrainGrid.Create` + `OverworldTerrainBuilder.WriteHeights`). `OverworldPlayerController.Update`
copied line by line — run 4.5 m/s, sprint 7.5 m/s, `speedDamping` 5, `jumpHeight` 1.2 m, `gravity` −18,
grounded stick −2 m/s, one `Move` per `Update` — with scripted world-space input toward waypoints in
place of camera-relative WASD. The controller as `OverworldPlayerMenu` builds it: height 1.8 m, radius
0.3 m, centre 0.9 m, `skinWidth` 0.03 m, `stepOffset` 0.4 m, `slopeLimit` and `minMoveDistance` at the
Unity defaults 45° and 0.001. Variants: run, sprint, and run with a jump when progress stalls. Time steps
1/30, 1/60, 1/120 s and a repeat. Calibration ramps put every node on an exact multiple of the stored
step, so each ramp triangle has its designed slope exactly.

**Calibration.**

| surface | walking (run, sprint) | jumping | descent |
|---|---|---|---|
| planar 44.0–44.995° (three grid directions, oblique 30° and 60°, three start offsets) | always | always | always |
| exactly 45.000° | depends on the direction: stops at 0, 10.8 or 23–24 m; oblique on the diagonal passes; identical on every repeat | always | always |
| planar 45.005–70° | never | always (28 jumps for 38 m at 45.1°, 71 for 105 m at 70°) | always |
| folds of 30–80° with faces ≤ 40°, on the cell diagonal or a node column, along and across | always | always | always |
| one or two rows of cells at 46–70° on the production lattice | never | always | always |
| short faces on a 0.25 m grid (not production geometry) | ≤ 0.41 m high always; ≥ 0.45 m never | always | always |

Contact normals on 44.9° ramps reach 46.8–48.6° at triangle edges without stopping the walker, so the
reported contact normal is no predictor. Outcomes are identical across all time steps and repeats.

**Critical passages and model paths** (seeds 110201, 424242, 1010, 1026, 1034; the seven passages of
§20; seven start offsets per run). Round 1 walked one route per passage on both surfaces. Round 2 walked
each model's own minimax path within 15 m of that route — 56 paths, vertex steps through the shared
vertex — in 7 056 runs. A claim "passable" counts as met when walking reaches the goal both ways in
every run; "not passable" when some direction never succeeds.

| model | own path: claims met | passable claims met | same route, judged by each criterion: agree / false "passable" / false "not passable" |
|---|---|---|---|
| T-edge | 14 / 14 | 4 / 4 | 48 / 0 / 6 |
| T-vertex | 13 / 14 | 4 / 5 | 53 / 1 / 0 |
| D3 on edges | 13 / 14 | 3 / 4 | 46 / 5 / 3 (D3 over both graphs) |
| D3 with vertex steps | 11 / 14 | 2 / 5 | |

Two same-route cases stay undetermined: descent on paths steeper than 45° succeeds from some start
offsets only (14 / 42 and 15 / 42); both are "not passable" claims and stand.

- **1010 `H4A`, explained.** The T-edge path must climb a 45.005° triangle, and the controller stops
  there, as calibration predicts. The T-vertex path passes it through vertices — touching faces of
  45.005–45.46°, the 45.005° triangle among them — and walks both ways in 42 of 42 runs. D3's paths cross 45.003° and 45.005° triangles that
  D3 reads as 44.76–44.89°. The region is reachable on foot; T-edge's "not passable" is the model's
  exclusion of vertex steps.
- **1034 on E, explained.** The round-1 route was built on `H4A`; on E it crosses 27.6 m of triangles
  steeper than 45° (up to 46.65°). Every model's own path on E (steepest 42.9–43.9°) walks both ways in
  42 of 42 runs.
- **Vertex steps, open.** Eleven vertex steps on walked paths touch a triangle steeper than 45°
  (45.02–45.89°). Ten pass both ways; one (1034 `H4A`, 45.413°) stops every ascent and lets every
  descent through. Neither the touched slope nor the heading against its uphill direction separates
  the two outcomes. The outcome is deterministic but no tested model predicts it.
- **Jumping:** every one of the 56 paths, both ways.
- **Stability:** outcomes identical across 1/30, 1/60 and 1/120 s and the repeat; end positions within
  1 mm on the passages.

## 22. Proposed contracts — owner review pending, nothing here is approved

**22.1 Structural height and final surface.**
- `H4A` (`IslandElevation`, evaluated by `TerrainAuthorityField`) stays the **structural height**.
  Stages 1–4A, the frozen Stage 3C trajectories and Stage 3D/3E topology (declared on `H2`) read
  structural surfaces only and never E.
- E is the **final surface**: a lattice on the canonical world grid with E ≤ `H4A` at every node. It is
  the only height written to `TerrainData`.
- Nothing flows from E or `TerrainData` back into TerrainGen.
- The structural channels the surface rules read (`UpliftAt`, signed coast distance) stay structural
  and are declared as such.
- E reads the valley geometry through the existing binding: Stage 3F cuts along the frozen
  `FinalValleyTrajectory` via `ValleyTrajectoryNetwork.BuildCarve`, the API-only Stage 3C unfreeze that
  F1 already spent (`PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md` §20). The contract needs no new unfreeze
  and changes no frozen type.
- **E is not `H3F`, pending approval.** `H3F = H4A − C3F` (`CarvedElevation`, §2) is the subtractive
  carve along the corridors. E = min(`H4A`, fill(Epre)): Epre joins the production massif terms to a
  floor-referenced plain template with a hard maximum and a 60 m blend below the toe (§10.2), and the
  fill is the band-local correction (§16). Both lie at or below `H4A`. The statements that Stage 3F carves
  `H = H4A − C3F` — [AGENTS.md](../../../AGENTS.md) § Accepted Draft Decisions, «Terrain guardrails»;
  [90 Glossary](../../../90%20Glossary.md) § Island Relief (Stage 4A); §2 above — describe the existing Stage 3F code; they stay unchanged until
  this contract is approved and are updated in the same documentation transaction (99 Open Questions,
  3F-1). Where E enters the bake is 3F-25.

**22.2 Height query.** The final surface is read as the **triangle reading of the quantised final
lattice** (§18): node heights as `TerrainData` stores them, each cell split SW→NE, tile-edge vertices
shared. `Terrain.SampleHeight` and the collider are this reading. Bilinear and analytic readings are not
the surface; a production point query and the lattice evaluator share one function (§16).

**22.3 Walkable triangle.** Slope strictly below 45° (§21: 44.995° always walks, 45.005° never, 45.000°
depends on the direction).

**22.4 Walking connectivity.**
- **T-edge** — conservative lower bound of guaranteed walking connectivity, both ways, in the
  configuration of §22.6. Observed on the tested sample, not proved island-wide.
- **T-vertex** — experimental upper bound; it does not guarantee walkability.
- **Uncertain zone** — reachable by T-vertex and not by T-edge; read as neither accessible nor
  inaccessible.
- Descent-only links exist (descent passes every tested slope) and are not part of the contract.

**22.5 Prohibited for physical decisions** — a character's walkability, an object's footing, spawn
and a character's access gating: slope readings D2, D3, D6 and D7; D4 is not used for them either and
keeps its own uses, road cost and POI site scoring. **Road passability is not a physical decision in
this sense** and stays governed by the approved Road Passability contract ([90 Glossary](../../../90%20Glossary.md)
§ Road Passability; [AGENTS.md](../../../AGENTS.md) § Accepted Draft Decisions), which reads grades and slope on
`LandscapeCostField`; this contract does not touch it. D3 remains the candidate slope for surface and
vegetation classification (99 Open Questions, 3F-21). This contract changes no rule threshold.

**22.6 Applicability.** Unity 6000.5.7f1, Windows Editor, Mono, Play Mode in batch mode; 2×2 tiles of
1400 m, heightmap 513, height budget 700 m, `TerrainCollider`; the controller values and movement
constants of §21; time steps 1/30–1/120 s; scripted input. Graph statistics on 8 seeds; Play Mode on 5
seeds, 7 passages and 56 paths plus the calibration surfaces. **Not covered:** player builds and IL2CPP,
other platforms and Unity versions, analog input and human route choice, tree and object colliders,
roads, hydrology, water and swimming, rendered LOD. A change to any listed parameter voids the contract
until it is re-measured.

**22.7 Jump accessibility.** A separate notion: what the production jump reaches. Measured
unconstrained by slope up to 70° on every tested surface; no graph model describes it. Whether steep
walls should stop a jumping player is a game-design decision (99 Open Questions, 3F-20).

## 23. Current position and open decisions (supersedes §17)

- Production is unchanged; Stage 3F remains an experimental downstream carve and E a research
  prototype. E is not connected to the bake. Stage 3F's carve still cuts along the frozen
  `FinalValleyTrajectory` through `ValleyTrajectoryNetwork.BuildCarve` (preflight §20), and E uses that
  binding unchanged.
- **Measured since §17:** the served surface is the triangle reading (§18), E keeps its invariants on
  it, and the R1 defect has no counterpart on a lattice-only reading; slope readings are mapped and
  measured (§19); physical connectivity and footing are measured (§20); the production controller
  agrees with T-edge wherever T-edge claims a passage (§21).
- **Proposed for approval:** §22.
- **Twelve questions block a dump-first stage 1:** the final-surface separation (3F-1), the drainage
  invariant (requirement D1, 3F-2) and its oracle (3F-3), the plain incision bound by arc class (3F-4),
  band-local correction in production (3F-5), the point-query contract (3F-6), `FieldResolution` as
  world identity (3F-7), per-arc data (3F-8), hard maximum against `SmoothMax` (3F-9), filling of
  inherited depressions (3F-10), flat-filled pits (3F-11) and cirques as permitted basins (3F-14).
  3F-10, 3F-11 and 3F-14, and the arc-class coverage of the bound in 3F-4, are the owner's call;
  3F-14 carries a proposed research mode for stage-1 dumps, which is not a decision.
- **Eight questions block enabling E in production:** valley-ending semantics (3F-12), the traversability
  criterion (3F-17), the walking-connectivity contract (3F-18) and the prohibition of §22.5 (3F-19),
  jump and barriers (3F-20, the owner's call), the classification and biome slope source (3F-21,
  3F-22), and where E enters the bake (3F-25).
- **Five questions may follow later:** valley mouths (3F-13), natural thalweg (3F-15), mountain-term
  redesign (3F-16), vertex passages and the uncertain zone (3F-23), descent-only links (3F-24).
- Options, consequences and recommendations for each: [99 Open Questions](../../../99%20Open%20Questions.md)
  § Terrain — Stage 3F valley carve.
