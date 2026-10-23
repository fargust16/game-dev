# G3e (LMC) — Step 1: Review of Calibration Run 5 and Decision Notes

Version: 0.23

Status: Draft — **a review for the owner; it authorises nothing and changes no code.** Written on 2026-10-23 after the
fifth calibration run, before any further Unity run. Every number below was read from the preserved run outputs with
read-only scripts; nothing was generated for this review.

Last Updated: 2026-10-24 (v0.23: the report accepted, Step 1 closed, §24. v0.22: T1 kept, §23.5; Step 1 ends not ready — the report, §24. v0.21: run 11 — T1, the harmonic toe — implemented and verified; toe steps and toe pits
gone, gates unchanged, §23. v0.20, 2026-10-23: run 11 — T1, the harmonic toe — pre-registered, not authorised, §22. v0.19: decision note — a continuous toe and how R7(c) reads a valley axis, §21. v0.18: recording run 10 — the mountain stage's climbs and depressions attributed; the toe is
the main cause, §20. v0.17: (b) kept, §18.7; the recording run 10 for the mountain stage pre-registered, not
authorised, §19. v0.16: run 9 — option (b) implemented; its check passes, but protected floors whose outflow
is still raised become new pools, §18. v0.15: rule (b) kept as implemented, §16.7; decision note on plateau placement across
drainage lines and corridors, §17; §16.5's attribution of the valley-8 climb corrected. v0.14: run 8 — ramp rule (b) implemented; its check fails for causes outside the ramp,
§16; C4 confirmed, §15.2. v0.13: owner decisions after run 7 — M-1 accepted with caveats, ramp rule (b) next with a
fourth criterion, §15; §13.3's check extended. v0.12: run 7 — M-1 implemented in the measurement code and verified, §14; §10.4
corrected for the rounded declared positions its check used. v0.11: the full M-1 rule checked on the twelve preserved lattices and three synthetic cases,
§10.4. v0.10: the owner approves M-1 as a specification rule (spec §14); full-rule check pending,
§10.3. v0.9: an outdated statement in §7.3 marked — rule (c) was computed in §10.1. v0.8: the owner accepts ramp rule (b); its exact implementation specification recorded in
§13.3 — not implemented. v0.7: S-LM1 — 110201's mountain-stage depressions located on run-6 data; decision note on the
ramp rule (§13). v0.6: the owner closes S-LM3 in its current configuration as a negative result — option (a),
§12. v0.5: diagnostic run 6 — authorised, run and analysed (§11); the base-surface hypothesis of
§5 refuted. v0.4: checks on preserved data after the decisions — M-1 option (c) computed on twelve
lattices and a precise rule proposed; the 1337 depression located (§10). v0.3: the owner's decisions on PD-1, Q9 #9/#14, M-1 and #12 recorded in §9. v0.2: two statements corrected on re-reading — Q9 #9's elevation targets (§3) and the
run-5 fallback of M-1 (§7.3); facts for the owner's checks added to §7.1–7.3, marked *added in v0.2*; recommendations
unchanged. v0.1: first version)

Depends On:
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (v0.4.1),
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md),
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) (§4, scope C7)

Referenced By:
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) §6

---

## 1. State of Step 1

- **Preserved.** The whole working folder (five calibration runs with layouts, lattices, readings, settings and logs;
  the continuity check; the defect log; the protected manifest) is copied to
  `D:\Hobby\game-dev\research-data\g3e-step1\` — 109 / 109 files equal by SHA-256 to the source — with a snapshot of the
  six `TerrainGenLmc*` files exactly as run 5 compiled them and a `README.md`. The code of runs 1–4 is not kept as
  files; the defect log describes each change.
- **Code.** The six Editor-only files exist and compile; referenced by nothing.
- **Continuity with G3c holds** — 408 / 408 readings within tolerances written before the first C# run.
- **No synthesis is accepted.** No candidate passes every hard gate on either seed (§2).
- **Production unchanged.** Every run checked the 73 protected files before and after (73 / 73 each time), contour A's
  Stage 1 hash and production `H4A` 110201. Held-out and reserve seeds were never generated.
- **No commit, no push** (C8).

## 2. Hard gates by run

Run 1 is absent: its layout failed V-L6 / V-L7 on both seeds, so nothing was synthesised. `mask` (V-L2 on the
lattice) held in every candidate.

| Run | Seed | S-LM3: R4 R5 R7 R8 R9 R10 | S-LM1: R4 R5 R7 R8 R9 R10 |
|---|---|---|---|
| 2 | 110201 | n n n n Y Y | Y n n Y Y Y |
| 2 | 1337 | n n n n n n | Y n n Y Y n |
| 3 | 110201 | n **Y** n n n Y | Y n n Y Y Y |
| 3 | 1337 | n **Y** n n n Y | Y n n Y Y n |
| 4 | 110201 | n n n n n n | n n n n n Y |
| 4 | 1337 | n n n n n n | Y n n Y Y n |
| 5 | 110201 | n n n n n Y | n n n n n Y |
| 5 | 1337 | n n n n n n | Y n n Y Y n |

## 3. Priority A — S-LM3's receiver iteration

**Convergence criterion (spec §5.3, unchanged):** the receiver set identical in two consecutive iterations, at most 30
iterations; on failure the last iterate is used and the failure recorded.

| Run | Update | Changes per iteration (last 5) | Returning to the receiver two iterations back | Land triangles > 70° | Land walkable from the shore | Highland median slope | Main summit (target) |
|---|---|---|---|---|---|---|---|
| 3 | all at once (contract) | 282 000 – 298 000 | 30 % | 8.8 – 10.4 % | 52 – 55 % | 17.7° – 22.2° | 343 m (344); 387 m (332) |
| 4 | ordered sweep (Q9 #9) | 348 000 – 363 000 | 62 – 75 % | 9.4 – 9.9 % | 45 – 59 % | 22.3° – 27.7° | 344 m (344); 308 m (332) |
| 5 | sweep + damped area (Q9 #14) | 300 000 – 308 000 | 27 % | 1.2 % | 96 % | 2.1° | **128 m (344); 131 m (332)** |

Land is about 378 000 vertices, so in every variant **75–96 % of land changes receiver on every iteration**, on both
seeds alike. The change is not a clean two-cycle (only 27–75 % return to the receiver of two iterations back).

**What does not converge.** Both the numerical iteration and the drainage topology, and they are coupled through the
drainage area A:
- within one ordered sweep (run 4) receivers and heights are consistent for the areas held fixed, yet receivers change
  again as soon as the areas are recomputed — the per-step numerics are not the cause;
- damping the area (run 5) did not reduce the changes (≈ 300 000) but made the area the law reads differ from the
  real drainage area by a median factor of 2.7 (90th percentile ≈ 10).

**Hypothesis, not established:** most land is in the hillslope regime (A < A_c = 2 000 m² ≈ 267 cells), where the slope
is proportional to the vertex's own area; one upstream receiver flip changes that area by a whole cell — a 100 % slope
change for a vertex draining two cells — so the choice between a straight and a diagonal neighbour flips everywhere.
Roughness concentrates where hillslopes dominate: the median |Laplacian| of heights is 3.4–4.7 m in the mountain region
and 0.9–1.7 m in uplands against 0.1–0.2 m in lowlands (runs 3–4). Receivers are not stored per iteration, so the
split by regime, direction and region needs one instrumented run (§6, item 4).

**Do the last fixes break the declared geometry?** Q9 #14 does: run 5's S-LM3 surface is nearly flat — main summit
128–131 m against the approved 330–430 m, mountain slopes 2.1°, plateau targets missed — and the law no longer reads
the real drainage area. Its smoothness is not convergence. ~~Q9 #9 alone (run 4) kept the elevation targets~~ *(corrected in v0.2)*
Q9 #9 alone (run 4) kept the main summit on 110201 (344 m for 344 m) but missed it on 1337 (308 m for 332 m) and missed
the pass target on 1337 (route col 103.6 m above the lowland for 58.7 m); it changed nothing about convergence or
roughness.

**S-LM3's R7** fails only on the layout's declared trunks (climbs of 0.5–18 m along the trunk polylines); S8 = 0 and the
synthesis' own channels never climb. Its water does not follow the declared trunk line exactly.

## 4. Priority B — walking connectivity and R4 / R8 / R9 (S-LM1)

- **110201, runs 4–5:** R4, R8 and R9 fail for **one** reason: the 60–120 m plateau 1 is not reachable on foot. An R4
  site lies on its top (952, 2302) in a component not reachable from the shore; all four disconnected R9 pairs
  include that site; R8 lists the same site and the top. In run 3 the top was reachable. Between runs 3 and 4 Q9 #12
  narrowed the joint and gradual rim blend from max(100 m, 3 × class height) to max(60 m, 1.5 × class height) — for the
  93 m class from 279 m to 140 m, a smoothstep blend whose steepest part exceeds 45°. **Q9 #12 is the likely cause**, and
  it was a misdiagnosis: the depression it targeted (1337, below) is still there in runs 4 and 5.
- R8 also lists **the pass col** on 110201 since run 4; the cause is not established (#12 or the trough banks of #16).
- **1337:** R4, R8 and R9 pass in every run; walking connectivity is not the problem there.

## 5. R10, R7 and R5 — separate causes

- **S-LM1 R10 (1337)** fails in the lowland pocket at (1282, 1758): the planner's bed rises 5 m where the lattice jumps
  from 43 to 75 m. The same pocket is the 22.7 m deep, 3.6 ha closed depression of R7 (a). **Cause:** the base's flow
  distance to the designated outlets (Q9 #6) is computed over every land vertex, the mountain region included, and the
  pass corridors are cheap axes even inside it; the pocket's shortest flow path runs through the mountain, whose
  surface later replaces the base there. The path disappears and the pocket closes. R10 here is a consequence of R7.
- **S-LM1 R5:** the undesignated elevated mouths lie 134–331 m from the mountain region, not at its foot (mountain
  within 100 m of the sea on 12 % of the shoreline). Elevated water leaves the mountain and is captured by the coastal
  surface of the base — min(drainage surface, coastal surface) — wherever the coastal surface, rising from the shore at
  its intent's angle, stays below the drainage surface. On the final lattices 3–8 % (110201) and 45–46 % (1337) of the
  mountain region drains to a designated outlet. S-LM1's contract (spec §6) has no rule that routes flank drainage;
  only the base can. **This is a contract question, not a defect to patch** (§7.4).
- **S-LM3 R5** passed in run 3 (one elevated system per seed, at a designated outlet; 48–94 % of the mountain region
  drains to an outlet on the final surface) and was not measurable in run 4 (planner failed) or meaningful in run 5
  (flattened surface).

## 6. Proposed next fixes — not made; each awaits the decisions of §7

One block at a time; both seeds; all gates and protected hashes; compared with runs 3–5. At most two more diagnostic
runs; if no synthesis passes all hard gates after them, calibration stops with a not-ready report.

| # | Block | Change | Expected effect | Check |
|---|---|---|---|---|
| 1 | S-LM3 | revert Q9 #14 (damped area) | elevation targets reachable again (as run 4); roughness returns | main summit within ± 2 m; mountain median slope ≥ 15° |
| 2 | S-LM1 | ~~revert Q9 #12 (rim blend back to max(100 m, 3 × class height))~~ **not to be done before the cause is established (§9)** | plateau 1 on 110201 reachable; R4, R8, R9 back to run 3 | R8 list; site components |
| 3 | S-LM1 | ~~compute the base's flow distance without the mountain region and without axis discounts inside it~~ **dropped: the hypothesis behind it is refuted by diagnostic run 6 (§11.3)** | the 1337 lowland pocket drains; R10 on 1337 likely passes | S8 list; R10 |
| 4 | S-LM3 (diagnostic only; **recommended, not authorised — §9**) | record per iteration the receiver changes by area regime (< / ≥ A_c), by direction change (≤ 45° / > 45°) and by region; no model change | confirms or rejects the hypothesis of §3 | the split itself |

Items 2 and 3 touch different rules at different places (a plateau rim; a lowland pocket) and are read from different
gates, so their effects stay separable.

## 7. Decision notes for the owner

### 7.1 PD-1 — which drainage moves S-LM3 allows

- **Rule.** Spec §5.1: the drainage tree's structure is "constrained by the layout". §5.3 lists allowed moves: within a
  region; across a gradual or joint boundary either way; out of a plateau top only through its rim outlets; from an
  escarpment band only away from the top; into the sea only from shore vertices; "the coastal fringe of H and uplands
  may drain straight to the sea". Design note §4.1: drainage follows a declared regional graph to designated outlets.
- **Implemented.** Shore within 100 m of a designated outlet is that outlet's base level; other shore is a coastal base
  level that reaches only ground within 100 m of the coast (never a plateau); water may not pass from an outlet unit
  into a coastal unit. The start distance is weighted per region.
- **Observed.** With it, outlet units cover 72 % of land, 48–96 % of the mountain region drains to an outlet on the final
  surface, and R5 passed on both seeds in run 3. When a bug (Q9 #4) let coastal units take 90 % of land (run 2), every
  elevated system ended elsewhere.
- **Alternatives.** (a) accept PD-1 as the reading of §5.1/§5.3; (b) the literal move list without units — the mountain
  drains to the nearest coast, R5 expected to fail as in run 2; (c) units from an explicit region order (mountain →
  corridor → lowland), closer to the technical draft's §4.2 but more rules.
- **Recommendation: (a)**, the 100 m fringe kept as an authoring default and the rule recorded as an interpretation.
- *Added in v0.2 — facts for the owner's checks.*
  - *Exact rule as run:* outlet reach 100 m; coastal fringe 100 m of contour A's signed distance; plateau vertices never
    coastal; start distance weights axis 0.5, corridor 0.75, lowland 1.0, upland 1.25, highland and plateaus 1.5; land no
    outlet unit reaches joins the coastal units (3 680 vertices on 110201, 4 227 on 1337); forbidden move: outlet unit →
    coastal unit (coastal → outlet allowed).
  - *Acyclicity:* PD-1 only removes moves. The start tree and the ordered sweep (runs 4–5) are acyclic by construction.
    The contract's all-at-once update formed cycles in run 3 (110201 twice, 1337 once); they were detected and the
    previous receivers kept. The record does not show whether PD-1 contributed to them.
  - *Downhill:* every vertex is integrated above its receiver (d·s > 0) and S8 = 0 in runs 3–5, **but** 2 (110201) and
    6 (1337) inland vertices have no allowed path to base level and were set to base level (runs 4–5; not counted
    before run 4) — a defect PD-1's restrictions leave open.
  - *Designated outlets:* guaranteed for the constrained receivers only. The planner follows the surface's own steepest
    descent, which disagrees with the receivers at unit boundaries: on run 3's final surfaces 48 % (110201) and 94 % (1337)
    of the mountain region drains to a designated outlet by D8. R5 passed in run 3, but PD-1 alone does not make the
    surface drain to the outlets.

### 7.2 Q9 #9 and #14 — how §5.3's iteration is solved

- **Rule.** §5.3: receivers = the allowed neighbour of steepest descent on the current heights; iterate receivers →
  topological order → area → heights; stop when unchanged or after 30; otherwise record and use the last iterate.
- **Observed** (§3). The contract's update never converged and its last iterate is rough (R4, R8, R9 fail). #9 (an
  ordered sweep) keeps the rule on each sweep's heights but neither converges nor smooths. #14 (damped area) smooths by
  flattening the relief to 128–131 m and decoupling the law from the real drainage area.
- **Alternatives.** (a) return to the contract's update and report S-LM3's non-convergence as its result; (b) keep #9
  without #14 — same behaviour, more machinery; (c) an owner-approved amendment of the law (for example a hillslope
  regime that does not read a pixel-count area, or a different area routing) — a model change needing its own
  pre-registered check; (d) first one instrumented diagnostic run (§6 item 4) to establish the cause.
- **Recommendation:** revert #14 now; run (d); then choose between (a) and (c). Not (b).
- *Added in v0.2 — facts for the owner's checks.* Elevation targets per run (main summit; pass route col above the
  associated lowland): run 3 — 343 / 344 m, 73.3 / 69.8 m (110201); 387 / 332 m, 60.0 / 58.7 m (1337); run 4 (#9) —
  344 / 344 m, 71.5 / 69.8 m (110201); 308 / 332 m, 103.6 / 58.7 m (1337); run 5 (#9 + #14) — 128 / 344 m, 65.0 / 69.8 m
  (110201); 131 / 332 m, 20.3 / 58.7 m (1337). No variant converged (§3).

### 7.3 M-1 — which col is the pass col

- **Rule.** Spec §2.3: "the lattice key col of the declared pass (the merge-tree saddle within 150 m of the declared
  position)".
- **Observed.** On a rough surface (S-LM3, run 4) the nearest col within 150 m belonged to a noise peak: offset 1.9 m
  while the col on the declared basin–pass–basin route stood 71.5 m above the lowland. S-LM1 gives the same col either
  way (69.8 m).
- **Alternatives.** (a) the literal reading — the nearest col of any peak, noise-sensitive; (b) M-1 as applied in run 5 —
  only cols of peaks with G0's saddle prominence (≥ 15 m), the nearest; (c) the col where the lower of the two
  culminations flanking the pass has its prominence measured, if it lies within 150 m, otherwise "no col found".
- **Recommendation: (c)**, the closest to "key col of the declared pass"; (b) is acceptable and simpler. Both report a
  missing col instead of substituting one.
- *Added in v0.2 — facts for the owner's checks.*
  - *Correction:* the run-5 code of (b) does **not** report a missing col; when no col qualifies it substitutes the
    vertex at the declared pass position (as the literal reading's code did too). In run 5 no col qualified for S-LM3
    on either seed, so its offsets there (65.0 m, 20.3 m) are readings at the declared point, not at a col. Adopting
    (b) or (c) as recommended means changing that code.
  - *Both seeds:* S-LM1 gives the same col under the literal reading and under (b) on both seeds (109.8 m, offset
    69.8 m on 110201; 89.4 m, offset 58.7 m on 1337). S-LM3: literal reading — no col on 110201 in run 3, a noise col
    in run 4 (offset 1.9 m); (b) — no col on either seed in run 5. ~~Rule (c) has not been computed on any lattice.~~ *(outdated: computed on twelve
    lattices in §10.1, read-only, after this note was written.)*
  - *Reproducibility:* (a), (b) and (c) read the deterministic merge tree; the run-5 code breaks a tie between equally
    distant cols by the merge tree's peak order, which is deterministic for a given lattice.
  - *Plateaus and walking routes:* M-1 changes no terrain. It changes which vertex R8 tests as the pass and the pass
    offset reading. S-LM1 110201's "pass col unreachable" (runs 4–5) is the same col as in run 3, where it was
    reachable, so that failure comes from the terrain changes of runs 4–5, not from the col rule.

### 7.4 Open point (no decision requested yet) — S-LM1 and R5

S-LM1 routes mountain drainage only through its base surface. Making every elevated system reach a designated outlet
needs ground that turns mountain-foot drainage away from the sea — in practice high coastal rims, which raises the
cliff share that Q7 already flags (67–75 % in run 3). Options for a later decision: accept R5 failure as S-LM1's
result; or allow a construction rule for longitudinal foot drainage. Neither is proposed for the next run.

## 8. Boundaries kept

No held-out evaluation; no change to the production generator, `TerrainData`, scenes, materials, shaders or assets; no
change to protected files; no commit or push before the freeze decision; Step 2 not started.

## 9. Owner decisions, 2026-10-23

Recorded from the owner's answer to §7. They bind further Step 1 work.

| Item | Status | Conditions |
|---|---|---|
| **PD-1** | **accepted as a working interpretation** of §5.1/§5.3 — not as proof of correct hydrology | the outlet unit → coastal unit prohibition stays; base heights assigned to vertices with no allowed path (2 on 110201, 6 on 1337 in runs 4–5) are **not** an acceptable fix; reaching the designated outlets is checked on the actual surface, not only on the receiver graph; the 100 m reach and fringe and the region weights are **temporary authoring parameters**, not confirmed constants |
| **Q9 #9** | **not accepted** as a solution to non-convergence | it kept the targets better on 110201 only |
| **Q9 #14** | **rejected** | lower roughness bought with an almost complete collapse of the relief |
| **Diagnostic run (d)** | **recommended, not authorised** | needs a separate authorisation for the code change and the run; it must establish why hundreds of thousands of receivers change and at which stage the instability arises. If it confirms a fundamental problem of the iteration scheme, option (c) — a model change — needs its own approval and a pre-registered check |
| **M-1** | **option (c) chosen as the design rule to check; its implementation not approved; M-1 not closed** | before implementation, define: which two culminations flank the pass; how they relate to the declared route; what happens when no col lies within 150 m; the effect on R8 and on the pass height reading. A missing col is reported as **"not found"**, never replaced by the height of an arbitrary vertex |
| **Q9 #12** | **not to be reverted as a fix** until the defect's cause is established | first reproduce the defect on the saved lattice and determine at which stage the mountain surface replaces the base; the explanation of §5 is one case, not a proven general cause |
| **Step 1** | **not complete; freeze not authorised** | — |

**Working rule from the owner.** Checks on existing data that change no code may continue on the preserved material. A
new run, a code change or a new Unity run needs a separate authorisation. No passing candidate is to be sought with
further heuristics: the model has to be shown to converge, to keep its elevation targets and to route rivers to the
designated outlets before a freeze makes sense.

## 10. Checks on preserved data after the decisions (2026-10-23)

Read-only Python on the preserved lattices (`research-data\g3e-step1\`); no code change, no Unity run. Scripts and
results: `research-data\g3e-step1\checks-2026-10-23\` (SHA-256 listed there).

### 10.1 M-1 option (c) on twelve lattices

**Rule as computed (stated before computing).** The flanking culminations are the nearest declared culminations of the
main divide on each side of the declared pass, by arc length; each one's vertex is the highest mountain-region vertex
within 150 m of its declared point (spec §2). (c1) is the col at which the two vertices' components first join in G0's
merge tree (8-connectivity, descending height, ascending index); (c2) is the key col of the lower of the two, as worded
in §7.3. A col counts as found only within 150 m of the declared pass. Walking reachability (R8) by the T-edge rule.

| Run | Seed | S-LM3: c1 (distance to the declared pass; offset; reachable) | S-LM1: c1 (distance; offset; reachable) |
|---|---|---|---|
| 3 | 110201 | not found (308 m) | 0.5 m; 69.8 m (target 69.8); yes |
| 3 | 1337 | not found (186 m) | 0.7 m; 58.7 m (target 58.7); yes |
| 4 | 110201 | not found (331 m; a col in upland) | 0.5 m; 69.8 m; **no** |
| 4 | 1337 | not found (207 m) | 0.7 m; 58.7 m; yes |
| 5 | 110201 | not found (164 m) | 0.5 m; 69.8 m; **no** |
| 5 | 1337 | "found" at 67 m — **degenerate**: the col is the lower culmination's own vertex (55.4 m) | 0.7 m; 58.7 m; yes |

- c1 and c2 coincide on 11 of 12 lattices; they differ only in the degenerate case.
- S-LM1's col is the same under the literal reading, under (b) and under (c), on both seeds.
- S-LM3's lowest link between the two culminations lies 164–331 m from the declared pass in every run: its pass is not
  where the layout declares it. Run 4 / run 5 offsets reported earlier for S-LM3 (1.9 m; 65.0 m, 20.3 m) were readings
  at a noise col or at the declared point, not at this link.
- The degenerate case arises because, on run 5's flattened surface, the highest vertex near the declared culmination is
  not a local maximum.

**Proposed precise rule for M-1 — for the owner's review, not implemented:**
1. *Flanking culminations:* the nearest declared main-divide culminations on each side of the declared pass, by arc. If
   one side has none, the pass is "not declared between two culminations" (the layout's V-L7 already requires both).
2. *Culmination realised:* the highest mountain-region vertex within 150 m of the declared point must be a merge-tree
   peak with G0's summit prominence (≥ 30 m); otherwise "culmination not realised" and no col is sought.
3. *Pass col:* c1 — the merge-tree col joining the two realised culminations.
4. *Relation to the declared route:* found only if c1 lies within 150 m of the declared pass; otherwise "not found",
   with its distance reported. No other vertex is substituted.
5. *Readings:* pass offset = c1 height − the associated lowland level (§2.3) when found; otherwise "not found".
   **R8:** when the col is not found, the pass target is unmet — the declared pass is not realised — and R8 fails with
   that reason (an owner decision: the alternative is to skip the pass target).

### 10.2 The 1337 depression (Q9 #12)

| Run | Depth | Area | Extent by region (vertices) | Rim by region | Spill |
|---|---|---|---|---|---|
| 3 (before #12) | 22.8 m (floor 42.2 m, fill 65.0 m) | 0.88 ha | mountain 954, upland 135, plateau band 69, lowland 14 | mountain 164, band 25, ramp 12, lowland 11, upland 3 | (1280, 1851), mountain, 65.0 m |
| 4 | 22.6 m (fill 64.8 m) | 3.54 ha | mountain 4 516, upland 135, band 66, lowland 14 | mountain 536, band 24, ramp 12, lowland 8, upland 2 | (1277, 1851), mountain, 64.2 m |
| 5 | as run 4 | 3.54 ha | as run 4 | as run 4 | as run 4 |

- The depression is a hollow **inside the mountain region at its edge**: its floor (42.2 m) sits where lowland, mountain
  and plateau 1's band and ramp meet; it is closed by the mountain surface (92 % of the rim in runs 4–5) and by the
  plateau's band and ramp to the south, and spills through the mountain at 64–65 m.
- It **existed before #12** with the same floor and almost the same fill level; after run 4 its area grew fourfold at the
  same level. #12 neither caused nor removed it; the growth between runs 3 and 4 is consistent with the narrower rim
  blend no longer raising mountain-edge ground above the fill level, but runs 3→4 also changed #11, #13 and #15, so the
  growth is not attributed to one change.
- **The stage at which it forms is not established from preserved data.** The lattices hold only the final surface; the
  base, the mountain surface, the plateau aprons and the running minimum are not saved separately, and the trunk and
  pass polylines are not saved. Locating the stage — the agreed precondition for any fix — needs those intermediate
  surfaces from one diagnostic run.

### 10.3 Owner decision on M-1 (2026-10-23)

**Approved as a rule of the specification** (spec v0.5 §14), **not as a verified implementation**: the twelve-lattice
computation above did not apply item 2 (prominence ≥ 30 m) or the new R8 behaviour. Next: a separate Python-only
computation of the full rule on the same twelve preserved lattices — no project code, no Unity, no generation —
covering the three outcomes and confirming that R8 never skips the pass target. Until then the implementation of M-1
does not change and Step 1 stays incomplete.

### 10.4 The full M-1 rule on the preserved lattices (2026-10-23)

Python only, read-only, on the twelve preserved lattices and three synthetic cases; no project code, no Unity, no
generation. Pre-registration `m1_full_preregistration.md` (SHA-256 `f0264e93…`, 09:04Z); script, results and the
pre-registration in `research-data\g3e-step1\checks-m1-full\`.

| Synthesis | Lattices | M-1 outcome | Pass target / R8 |
|---|---|---|---|
| S-LM1 | runs 3, 4, 5 × 110201, 1337 (6) | both culminations realised (prominence 134–141 m and 308–320 m); col **found** 0.0–0.8 m from the declared pass; offset equal to the target (69.8 m, 58.7 m) | met on runs 3 and on 1337 in runs 4–5; on 110201 in runs 4–5 **col unreachable** → R8 fails with that reason |
| S-LM3 | run 3, 1337 | both culminations realised (35.2 m, 362.7 m); col 186 m from the declared pass → **not found** | unmet → R8 fails ("not found") |
| S-LM3 | run 3 110201; runs 4–5 both seeds (5) | **culmination not realised** — for at least one flanking culmination the highest mountain vertex within 150 m of its declared point is not a merge-tree peak (prominence 0); the other one is realised on run 3 110201 (prominence 319.2 m) and on run 4 1337 (64.8 m) | unmet → R8 fails ("culmination not realised") |

*Corrected 2026-10-23 after run 7 (§14.3).* The check above first read the declared positions from `layout.txt`, which
rounds them to whole metres. Repeated with the exact positions (layouts byte-identical in runs 3–7): every status, pass
target and R8 verdict is unchanged; the col distances moved by up to 0.5 m (now 0.0–0.8 m), and on run 4 1337 S-LM3's
second culmination became realised (262.7 m, prominence 64.8 m) instead of not (263.8 m, 0 m). The table shows the
corrected values; the first results file is kept unchanged in the archive.

Synthetic surface (two Gaussian peaks joined by a saddle): (a) pass declared at the saddle — **found**, 0 m; (b) declared
300 m away — **not found**, distance 300 m reported; (c) a culmination declared on a slope without a peak —
**culmination not realised**.

- **Every pre-registered expectation held**, and all three outcomes occur in the real data as well as in the synthetic
  cases.
- **R8 never passes with the pass target unmet** — asserted on every lattice, never triggered. R8 under M-1 equals the
  recorded R8 on all twelve lattices (the earlier substitution did not flip any gate here); the rule now states the
  reason.
- **A property of the rule to keep in mind:** a culmination counts as realised only if the highest vertex within
  150 m of its declared point is itself a peak; a real summit just outside 150 m does not count. On S-LM1 the
  culminations sit exactly at their declared points; on S-LM3 the strictness is what exposes the declared
  culminations as unrealised.
- M-1 is therefore **checked on the preserved data**; the Step 1 code still carries the earlier reading and its
  substitution until a separately authorised change, which would alter readings only — no lattice — and can be verified
  against this computation.

## 11. Diagnostic run 6 (authorised by the owner, 2026-10-23)

**Scope as authorised:** one run on 110201 and 1337, no model change; only intermediate data recorded; Q9 #14 reverted;
no other fix. The pre-registration (`diag_preregistration.md`, SHA-256 `5c7000c9…`, 08:35:55Z) was written before the
code change. Code changes: #14 removed from `TerrainGenLmcFluvial.cs` (the law reads the exact area of the previous
iterate again, as in run 4; the ordered sweep #9 stays); read-only recording added to `TerrainGenLmcFluvial.cs`,
`TerrainGenLmcSkeleton.cs` and `TerrainGenLmcDump.cs` behind the flag `-lmcDiag true`. Outputs, the code as run and the
analysis scripts are preserved at `research-data\g3e-step1\diag-run6\` (63 files, SHA-256 manifest).

### 11.1 Validity — the recording changed nothing

| Candidate | Expected (pre-registered) | Run 6 |
|---|---|---|
| S-LM3 110201 | run 4 `dc33f1ea…` | `dc33f1ea…` — equal |
| S-LM3 1337 | run 4 `9f2629ac…` | `9f2629ac…` — equal |
| S-LM1 110201 | run 5 `e8be6482…` | `e8be6482…` — equal |
| S-LM1 1337 | run 5 `84df8638…` | `84df8638…` — equal |

Layouts `868a4890…` and `ffb87158…` equal; the 73 protected files unchanged before and after. The revert of #14 is
exact, the recording does not interfere, and the four lattices reproduce bit for bit in a fresh process.

### 11.2 S-LM3 — what does not converge

Per iteration 2–30 (both phases; land ≈ 378 000 vertices); exact ranges over those iterations:

| Reading | 110201 | 1337 |
|---|---|---|
| receivers changed per iteration | 345 840 – 365 597 (91–97 % of land) | 343 449 – 364 367 (91–96 %) |
| of them, back to the receiver of two iterations before (iterations 10–30) | 59–75 % | 54–77 % |
| of them, in the hillslope regime (A < A_c at the start of the iteration) | 96–97 % | 96–97 % |
| turn of the receiver direction 45° / 90° / 135° / 180° | 20–48 / 23–37 / 11–36 / 4–20 % | 17–47 / 27–39 / 11–33 / 4–23 % |
| changes by region (lowland / upland / mountain / plateau top), about | 29 / 31 / 31 / 3 % | 28 / 33 / 31 / 6 % |
| land vertices whose receiver changed in ≥ 20 of 30 iterations | 98 % | 96–97 % |
| median change of drainage area per iteration | × 5–8 (median abs Δ ln A 1.67–2.08) | × 5–8 (1.61–2.08) |
| vertices whose terminal shore vertex changed per iteration | 101 849 – 350 976; almost all within one outlet's shore stretch or among coastal shore vertices; outlet switches 0–4; outlet ↔ coastal 210 – 16 533 | 81 644 – 336 197; **18 806 – 261 805 switch between the two outlets**; outlet ↔ coastal 451 – 11 609 |

**Answer to question 1.** It is neither a local flicker of directions nor a few unstable patches: almost the whole
drainage tree reorganises every iteration, with a strong two-iteration alternation (≈ 70 % of changed vertices return to
the receiver of two iterations back), turns of 135–180° making up 15–56 % of the changes, drainage
areas changing five- to eightfold, and on 1337 between 19 000 and 262 000 vertices switching outlet basin every iteration.
Changes spread over the regions in proportion to their area and do not decline over 30 iterations. Both the numerical
iteration and the drainage topology are unstable, and the instability sits where the hillslope regime governs (96–97 %
of changes).

**Interpretation — consistent with the data, not proven by a controlled test.** In the hillslope regime the slope is
proportional to the vertex's own drainage area (spec §5.2). Under a full receiver update this is a negative feedback: a
vertex that lost upstream area gets a small slope, stands low in the next sweep and attracts flow back; one that gained
area gets steep and sheds it. In the channel regime the slope falls with area (positive feedback, ordinary capture),
and only 3–4 % of the changes are there. The run used the ordered sweep (#9); runs 2–3 with the contract's all-at-once
update showed the same volume of changes (282 000 – 298 000) but were not recorded in this detail.

**Consequence for the decision of §9 (owner's).** The data point to the law's hillslope regime rather than to the
ordering of the update: no ordering of the same law is expected to converge, and damping (#14) only hid the
alternation by flattening the relief. The remaining options are (a) report S-LM3's non-convergence as its result, or (c)
amend the hillslope regime — a model change needing the owner's approval and a pre-registered check of convergence,
the elevation targets and the routing to the designated outlets. **Recommendation:** (c) only with a precisely stated
alternative and its pre-registered check; otherwise (a).

### 11.3 S-LM1 — at which stage the closed depressions appear

S8 depressions (fill depth ≥ 0.25 m over ≥ 100 m²) on each stage surface:

| Stage | 110201 | 1337 | 1337 hollow at (1282, 1758) |
|---|---|---|---|
| base B = min(drainage surface, coastal surface) | 0 | 0 | absent (42.2 m, drains) |
| after the mountain | 4 | 0 | absent |
| after the pass corridors | 22 | 5 | absent |
| after the plateaus (before the running minimum) | 23 | 8 | **22.6 m deep** |
| after the running minimum = final | 4 | 3 | 22.6 m, 3.54 ha |

- **The base is free of closed depressions on both seeds**, as constructed. The hypothesis of §5 — that the base's flow
  path through the mountain region closes the hollow — **is refuted**: the hollow does not exist until the plateau stage.
- **The 1337 hollow is dammed by plateau 1's ramp.** Following steepest descent from the hollow's floor on the surface
  before the plateau stage, the water leaves through lowland toward the sea; the plateau stage raises 59 of the path's
  232 vertices, by up to 52 m (42 → 94 m), all on lowland 208–319 m from the plateau's centre and 7–25° from its ramp
  bearing — inside the ramp sector. The ramp is applied as max(current, rim height − distance × tan 18°) over every
  non-top vertex of its sector, lowland included, and the layout points the ramp at the nearest lowland — so here it is
  laid across the valley that drains the mountain edge. Q9 #12 (rim blend width) concerns joint and gradual rims and
  does not touch the ramp; it neither caused nor could remove this dam.
- **On 110201 the four final depressions come from the mountain construction** (present after the mountain stage, at
  the same places in the final surface). The data do not separate the parts of that stage (frames, their blend, the
  valley strips); finding which needs finer recording.
- The pass corridors create most intermediate pits (22 and 5); the running minimum removes them again.

**Open for the owner — not fixed:** (1) S-LM1's ramp rule — for example, the ramp may not raise ground outside the
layout's ramp region, or may not rise above a drainage line it crosses; (2) the mountain-stage depressions on 110201 —
finer recording of that stage before any fix.

## 12. Owner decision after diagnostic run 6 (2026-10-23) — S-LM3 closed with a negative result

The owner chose **option (a)**: S-LM3's non-convergence is recorded as the result of the current model, and the law is
**not** changed within this calibration. This does not abandon S-LM3; it closes the study of this version with an
honest negative result instead of adding fixes until the metrics pass by chance.

| Item | Decision |
|---|---|
| Current S-LM3 | non-convergence within 30 iterations — **a negative result of the experiment** |
| Q9 #9 | does not remove the oscillation |
| Q9 #14 | rejected: stabilisation at the cost of an unacceptable loss of relief (reverted in code for run 6) |
| Cause | a **probable instability of the hillslope regime**; **causation not proven by a controlled experiment** — it may lie in the law's interaction with flow routing or with the surface's discretisation rather than in the law alone |
| Next variant | a separate research line with a changed law, its own plan and pre-defined checks |
| Current calibration | **no further parameter fitting of S-LM3** |

**The closed configuration.** Code as run in diagnostic run 6 (`research-data\g3e-step1\diag-run6\code-snapshot-run6\`,
SHA-256 in `SHA256SUMS.diag-run6.txt`; `TerrainGenLmcFluvial.cs` `6261cc25…`): the ordered sweep (#9), the law reading the
exact drainage area of the previous iterate, PD-1 as a working interpretation; lattices 110201 `dc33f1ea…`, 1337
`9f2629ac…` (equal to run 4). The contract's all-at-once update (runs 2–3) behaved the same way (282 000 – 298 000
changes per iteration, no convergence).

**What follows for G3e (owner).**
1. S-LM3 in this configuration is closed with its recorded results and limits.
2. S-LM1 continues on its own: the ramp rule, the source of 110201's four depressions and M-1 are settled **separately**
   — never in one patch.
3. After those rules are agreed, a limited calibration of S-LM1.
4. If S-LM1 also fails the hard gates, a **not-ready report for G3e** — not a freeze.
5. No held-out evaluation, Step 2 or production change without a separate authorisation.

## 13. S-LM1 — the next separate items (2026-10-23)

Read-only checks on diagnostic run 6's stage surfaces (`research-data\g3e-step1\diag-run6\checks\`); no code change, no
run. Each item below is settled on its own; none is combined with another in one patch.

### 13.1 The four depressions on 110201 — located, cause partly established

Depressions on the surface after the mountain stage (the base before it has none):

| Depression | Depth, area | Nearest ridges (frame blend zone when within 100 m of each other) | Toe spread inside | To a measured valley line | To the pass | In the final surface |
|---|---|---|---|---|---|---|
| (1531, 1788) | 7.2 m, 105 m² | ridge 0 at 43 m, ridge 3 at 77 m — **blend zone** | 0.3 m | 184 m | 558 m | yes |
| (1017, 1772) | 6.6 m, 1 159 m² | ridge 0 at 45 m, next 312 m | 4.3 m | 142 m | **49 m** | no — the pass corridor carves it away |
| (1840, 1430) | 4.4 m, 217 m² | ridge 0 at 33 m, next 518 m | 0.3 m | **6 m** | 940 m | yes |
| (1575, 2084) | 2.2 m, 464 m² | ridge 3 at 87 m, next 238 m | 1.1 m | **38 m** | 668 m | yes |

The final surface's fourth depression is a plateau band's (834, 2294), created at the plateau stage.

- One depression lies in the **blend zone between two ridge frames** — the averaging of two flank surfaces with
  different crests (§6 ownership rule, Q9 #8 keeps only the crest itself out of the blend).
- Two lie **on or beside a valley strip of a single frame**, with an even toe — not a blend and not a toe jump. A
  plausible cause, not established: the normalised flank coordinate u = d / (d + distance to the mountain boundary) need
  not grow monotonically down a valley line where the boundary is irregular, so the flank height need not fall along
  it. Confirming it needs the u, v and owning-frame fields, which run 6 did not record — **a further recording run,
  only on the owner's authorisation**. *Answered by run 10 (§20): (1840, 1430) is a toe step between neighbouring vertices; (1575, 2084) is not a toe
  effect — its spill lies upslope, and (post hoc) its downhill side is closed by the valley weight. Neither is u falling
  while d grows.*
- One is carved away by the pass corridors and does not reach the final surface.

### 13.2 Decision note — S-LM1's ramp rule

- **Rule as built.** Technical draft §5.1: "ramps grade at ≤ 20° to the foot". The layout marks the ramp as a rim
  segment of ±25° around a bearing toward the nearest lowland (the layout's rule) and paints a ramp region only on
  upland and corridor ground. S-LM1 applies max(current, rim height − distance × tan 18°) to **every** non-top land vertex
  of that ±25° sector up to the profile's reach, lowland included.
- **Observed** (§11.3). On 1337, plateau 1 (class 60–120 m, edge 65 m, ramp bearing 117°) reaches about 200 m beyond its
  rim; ±25° spans roughly 200–280 m across at that distance. The wedge lies across the valley that drains the mountain
  edge into the lowland and raises its floor by up to 52 m: a 22.6 m, 3.5 ha closed depression, and the planner's R10
  failure in the same pocket.
- **Options.** (a) Raise ground only inside the layout's ramp region — no damming of lowland, but where the region stops
  short of the foot the ramp ends in a step. (b) A ramp **strip of constant width** along the ramp bearing (the layout's
  minimum ramp width of 40 m, or 60 m) instead of a widening sector — keeps the access, leaves room for valleys beside
  it; it can still cross a valley that runs straight across its line. (c) Choose the ramp bearing in the layout so the
  strip does not cross a drainage line of the base — a layout change (both syntheses' shared input; S-LM3 is closed,
  so only S-LM1 is affected) with more rules.
- **Pre-registered check for whichever is chosen:** no S8 depression created by the plateau stage (stage 08 against
  stage 07, both seeds); every plateau top reachable (R8); ramp slope ≤ 20°; the layout and all other stages unchanged.
- **Recommendation: (b)**, 40 m wide (the layout's minimum, V-L6), with the check above; (c) only if (b) still dams.

### 13.3 Owner decision — ramp rule (b), 2026-10-23

**Accepted:** a ramp strip of constant width 40 m along the ramp bearing, with the pre-registered check of §13.2.
**Implementation and its run need a separate authorisation** — nothing is implemented yet.

**Implementation specification** (what the code will do once authorised; S-LM1's plateau stage only):

1. *Centreline:* the ray from the plateau's centre along its layout ramp bearing.
2. *Strip:* land vertices whose perpendicular distance from the centreline is ≤ 20 m (half of 40 m) and which lie beyond
   the rim along the ray; ρ = distance along the ray beyond the rim point at the ramp bearing.
3. *Profile inside the strip:* max(current, rim height − ρ × tan 18°) for 0 < ρ ≤ (top height − foot reference) / tan 18°
   + 10 m — the same profile and reach as today, now confined to the strip.
4. *Outside the strip, nothing is raised by the ramp.* The rim bearings the layout marks as ramp but which lie outside the
   strip take the escarpment profile (70° wall, then talus at 33° up to 0.35 × class height), as the escarpment arcs do.
5. *Consequences that follow from "constant width" and are not new decisions:* the strip's sides are as steep as the
   lattice makes them where the strip stands above the ground — walls along the ramp, highest near the rim and vanishing
   toward the foot; they are reported by the walls diagnostic and judged in the visual review. Where the strip itself
   crosses a drainage line straight across, it can still dam it; the check below reveals that.
6. *Setting:* `RampWidth = 40 m` (the layout's minimum ramp width, V-L6) — an authoring default of S-LM1.

**Pre-registered check (from §13.2), both calibration seeds:**
- no S8 depression created by the plateau stage — stage 08 against stage 07 of the same run;
- every plateau top reachable from the shore (R8);
- ramp slope ≤ 20° — along the strip's centreline, between consecutive samples 10 m apart, where the strip raised the
  ground;
- non-interference: S-LM1 stages 01–07 and the layout bit-equal to diagnostic run 6; only stage 08 onward may change;
  S-LM3 untouched (closed, §12);
- *(added by the owner on 2026-10-23, before any implementation)* no new valley damming and no disruption of drainage to
  the designated outlets — operational form **C4** in §15.2, *confirmed by the owner on 2026-10-23*.

If the 40 m strip fails this check it is **not widened automatically**; option (c) is then considered separately (§15.1).

## 14. Run 7 — M-1 in the measurement code (authorised by the owner, 2026-10-23)

**Authorisation.** M-1 (spec §14) in the measurement code as a separate patch, one run on 110201 and 1337; the lattices
must equal run 6, the pass readings the Python computation; no other change.

### 14.1 What changed

- `TerrainGenLmcMeasure.cs` only: the pass reading of `LmcReadingsBuilder.Measure` now follows M-1 — flanking
  culminations by arc, each one's vertex the highest mountain-region vertex within 150 m of its exact declared point, a
  copied G0 merge tree (descending height, ascending index, 8-connectivity) for prominence and c1, the col found only
  within 150 m, no substitution — and R8's pass part lists the reason (`pass: col not found (… m)`, `pass: culmination
  not realised`, `pass: col unreachable`, `pass: pass not declared`). New readings: `pass_m1_status`,
  `pass_flanking_culminations`, `pass_col_xz`, `pass_col_distance_m`, `pass_col_reachable`, `pass_target_met`,
  `pass_failure_reason`; `pass_col_height` and `pass_offset` are null when the col is not found.
- The other five `TerrainGenLmc*.cs` files are byte-identical to diagnostic run 6's snapshot. The run used `-lmcDiag` off.
- Pre-registration `run7_preregistration.md` (SHA-256 `afbdd56e…`), written before the code change.

### 14.2 Validity — no lattice changed

- All four lattices bit-equal to run 6 (MD5 S-LM3 `dc33f1ea…` / `9f2629ac…`, S-LM1 `e8be6482…` / `84df8638…`); layouts
  `868a48908dad70e9`, `ffb87158a06814bc`, valid.
- Protected manifest 73 / 73 unchanged before and after; contour A's Stage 1 hash `7599879a71250a06` and production
  `H4A` 110201 `e2bad571f4af76c7` matched.
- Every reading outside the pass fields is equal to run 6.

### 14.3 The pass readings against Python — the pre-registered comparison failed, the cause is in the reference

| Candidate | Status | Culminations: height / prominence / realised | Col: distance; offset; reachable | Pass target | R8 |
|---|---|---|---|---|---|
| S-LM3 110201 | culmination not realised | 226.28 / 0 / no; 313.46 / 0 / no | — | unmet (culmination not realised) | fails |
| S-LM3 1337 | culmination not realised | 227.75 / 0 / no; **262.74 / 64.77 / yes** | — | unmet (culmination not realised) | fails |
| S-LM1 110201 | found | 243.80 / 134.01 / yes; 343.84 / 319.84 / yes | 0.04 m; 69.76 m; **no** | unmet (col unreachable) | fails |
| S-LM1 1337 | found | 230.02 / 140.58 / yes; 332.09 / 308.09 / yes | 0.81 m; 58.68 m; yes | met | passes |

- **Pre-registered comparison: 5 values outside its 0.05 m tolerance** — the S-LM1 col distances (0.04 against 0.5 m on
  110201, 0.81 against 0.7 m on 1337) and S-LM3 1337's second culmination (262.74 m, prominence 64.77 m, realised against
  263.8 m, 0 m, not realised). Statuses, reachability, offsets, pass target and every gate verdict matched. Reported as
  a failed comparison, not tuned.
- **Cause (established after the comparison, not pre-registered).** The Python reference read the declared culmination
  and pass positions from `layout.txt`, which rounds them to whole metres; the C# uses the exact positions. On S-LM3 every
  chosen culmination vertex lies at the edge of the 150 m window (149.5–149.95 m from the declared point — the ground
  still rises beyond it), so a sub-metre shift of the declared point selects a different vertex.
- **Recomputed with the exact positions** (`layout_polylines.json` of run 6, float32; layouts byte-identical in runs 3–7),
  rounding disabled: **every field equal**, statuses and booleans identical, largest numeric difference 1.9 × 10⁻⁶ m
  (float32 against double). The C# implementation equals the rule as computed in Python.
- The same correction applied to §10.4's twelve lattices changes no status, pass target or R8 verdict; its table is
  corrected in place (§10.4 note).

### 14.4 Findings

- **M-1 is implemented and verified in the measurement code.** R8 now states its pass reason on every candidate:
  S-LM3 "culmination not realised" on both seeds, S-LM1 110201 "col unreachable", S-LM1 1337 passes R8.
- **Gates are unchanged from run 6:** S-LM3 fails R4, R5, R7, R8, R9, R10 on both seeds; S-LM1 110201 fails R4, R5, R7,
  R8, R9; S-LM1 1337 fails R5, R7, R10. No synthesis is accepted.
- **A property of the rule, recorded and not acted on:** a culmination's realisation depends on whether a summit lies
  just inside or just outside 150 m of its declared point. Where the vertex sits at the window's edge, the declared
  culmination is not where the relief culminates; a sub-metre change of the declared point can flip such a culmination.
  In these data no verdict depended on it (each S-LM3 status is fixed by the other culmination). Any change to the rule
  is the owner's.
- Every comparison input, script and result is in `research-data\g3e-step1\run7\` (`SHA256SUMS.run7.txt`).

### 14.5 What remains (each a separate authorisation)

- S-LM1's ramp rule (b) — implementation and its pre-registered check (§13.3).
- A recording run for 110201's depressions on a single frame's valley strip (u, v and owning-frame fields, §13.1).
- The limited S-LM1 calibration, then a not-ready report or a freeze package.
- Not started: held-out evaluation, Step 2, any production change. No commit (C8).

## 15. Owner decisions after run 7 (2026-10-23)

### 15.1 Decisions

- **M-1 implementation accepted with caveats.** Grounds as recorded in §14: run 7's lattices are bit-equal to run 6; with
  the exact declared positions every pass field equals the Python reference (largest difference 1.9 × 10⁻⁶ m); the five
  differences of the first comparison were reported, not hidden or tuned, and come from `layout.txt`'s rounding; M-1
  changed no hard-gate verdict, and R8 now states its reason.
- **The 150 m radius stays.** The window-edge sensitivity (§14.4) is a known property of the rule; on the data in hand it
  changed no verdict, so it is not a reason to correct the rule now.
- **Next priority: ramp rule (b)** — the 40 m constant-width strip of §13.3, as a separate stage, to remove the valley
  damming caused by the widening ramp sector without changing any other stage. Its criteria, both seeds 110201 and 1337:
  1. the plateau stage creates no new closed depression;
  2. every declared plateau top is reachable (R8);
  3. the ramp slope is ≤ 20°;
  4. no new valley damming and no disruption of drainage to the designated outlets.
- **If 40 m does not solve it, the strip is not widened automatically**; option (c) — choosing the ramp bearing in the
  layout — is considered separately.
- **Order after the ramp:** a separate recording run with the u, v and owning-frame fields for 110201's two unresolved
  depressions (§13.1), to find the stage where the flank coordinate stops being monotone; then the limited S-LM1
  calibration, ending honestly in one of two outcomes — a freeze package if the hard gates pass, a not-ready report if
  they do not.
- **Not authorised by these decisions:** the ramp implementation, its run, and every later run — each needs its own
  explicit authorisation.

### 15.2 Criterion 4 in measurable form — proposed, with a baseline on run 6

Criteria 1–3 are already exact (§13.3). Criterion 4 needs an exact form before the run that tests it; proposed:

- **C4.** Route the stage-07 and stage-08 surfaces of the same run with the specification's helper (`lmcspec.flood_d8`:
  priority-flood with a 1 mm epsilon, 8-connected, D8 steepest descent on the filled surface). A land vertex's
  *terminus* is the shore vertex its path enters the sea from; it drains to a designated outlet when the terminus lies
  within 150 m of it (R5's tolerance). **C4 holds when no land vertex outside the ground the plateau stage changed drains
  to a designated outlet on stage 07 and elsewhere on stage 08.** Reported beside it: each designated outlet's catchment
  on both stages, and the area that leaves or joins one. Damming that closes a depression is criterion 1; C4 adds the
  case where raised ground diverts a valley to another shore.

Baseline — read-only, on diagnostic run 6's S-LM1 stage surfaces (the current sector ramp); script and output in
`research-data\g3e-step1\checks-ramp-c4\`:

| Seed | Ground changed by the plateau stage | Designated outlet catchments, stage 07 → 08 | C4 |
|---|---|---|---|
| 110201 | 20.6 ha | outlet 0: 69.3 → 68.2 ha (the loss lies inside the changed ground); outlet 1: 2.0 → 2.0 ha | **holds** — nothing outside the changed ground leaves an outlet |
| 1337 | 30.1 ha | outlet 0: **93.3 → 28.6 ha**; outlet 1: 99.6 → 99.1 ha | **fails** — 53.3 ha outside the changed ground leave outlet 0, almost all of it for an undesignated shore at (634, 1832) |

- The form separates the known defect: 1337's damming (§11.3) shows up not only as the 22.6 m, 3.5 ha closed depression
  but as a diversion of most of outlet 0's catchment. That the whole diversion comes from plateau 1's ramp and not from
  another part of the plateau stage is **not established** by this check — it compares stages, not parts of a stage.
- C4 is read on stage 08 against stage 07, so it isolates the plateau stage; R5 and R10 on the final surface stay the
  hard gates they are and are reported as usual.
- ~~**Awaiting the owner:** confirmation of C4 as criterion 4's exact form (or a different form) before the ramp run.~~
  **Confirmed by the owner on 2026-10-23**, together with the authorisation of the ramp patch and one run (§16).

## 16. Run 8 — S-LM1 ramp rule (b) (authorised by the owner, 2026-10-23)

**Authorisation.** C4 confirmed as criterion 4's exact form; rule (b), 40 m wide, as a separate patch; one run on 110201
and 1337 with the check of §13.3 and C4; no other change.

### 16.1 What changed

- `TerrainGenLmcSkeleton.cs` only, S-LM1's plateau stage: the ramp profile max(current, rim − ρ × tan 18°) applies inside
  a strip — non-top land vertices within 20 m of the ray from the plateau's centre along its ramp bearing, beyond the rim
  point at that bearing, for 0 < ρ ≤ (top − foot reference) / tan 18° + 10 m; rim bearings the layout marks as ramp but
  outside the strip take the escarpment profile. New setting `RampWidth = 40 m` (authoring default). The other five
  `TerrainGenLmc*.cs` files are byte-identical to run 7; the layout, including its painted ramp region, is untouched.
- Pre-registration `run8_preregistration.md` (SHA-256 `124e1278…`, 09:34Z), with the check script validated on run 6
  before the code change. **Validation changed C1's form before the run:** the first form (no overlap with any stage-07
  pit) missed the known 22.6 m dam, because a sub-S8 pit lay inside it; the registered form counts the growth of fill depth
  from stage 07 with S8's thresholds.
- The run used `-lmcDiag true` for the stage dumps the check reads.

### 16.2 Validity

- **NI holds** on both seeds: S-LM1 stages 01–07, the layout maps, `layout_polylines.json` and the S-LM3 lattices are
  bit-equal to run 6.
- Protected manifest 73 / 73 before and after; contour A's Stage 1 hash and production `H4A` 110201 matched.
- S-LM1 lattices: 110201 `86450165…`, 1337 `b1040273…`.

### 16.3 The pre-registered check — fails

| Seed | C1 — depressions created by the plateau stage | C2 — plateau tops | C3 — ramp slope, stage 08 / final | C4 — drainage to designated outlets |
|---|---|---|---|---|
| 110201 | **fails** — (834, 2294) 1.17 m, 239 m², plateau 1's band, 79° off its ramp bearing — unchanged from run 6 | **fails** — plateau 1's top unreachable, as in run 6 | holds — 18.91° / 18.91° | holds |
| 1337 | **fails** — (1488, 1605) 3.78 m, 0.72 ha; (1441, 1463) 2.64 m, band, as in run 6; (1381, 1747) 0.78 m, 120 m². **The 22.6 m dam and (1269, 1854) are gone** | holds | holds — 19.17° / 19.17° | **fails** — 0.83 ha (run 6: 53.3 ha); outlet 0's catchment 93.26 → **93.26 ha** (run 6: → 28.62 ha) |

By the pre-registered rule, **rule (b) does not pass its check.** The strip was not widened and option (c) was not
started (§15.1).

### 16.4 Where the remaining failures come from (read-only attribution, after the check)

- **None lies in the strip or on the ramp-marked bearings.**
- 1337, (1488, 1605) and (1381, 1747): both lay inside run 6's 22.6 m hollow (fill depth 9.0 m and 10.7 m there); removing
  the dam lowered the fill level from 64.8 m to 59.6 m and 54.9 m and left them standing. Their route on the filled
  stage-07 surface runs along plateau 1's flank, 12–28 m beyond its top outline at 27–99° off the ramp bearing, where the
  plateau stage raises the ground by 55–69 m in both runs: **plateau 1 stands across the drainage line.**
- 1337, C4's 0.83 ha: the same vertices as in run 6 (1 104 then, 1 108 now). Their stage-07 route crosses **plateau 0's
  top** (first raised vertex: top region, +2.5 m, 29° off its ramp bearing), which sends them to an undesignated shore at
  (2420, 703) instead of outlet 1.
- 110201, C1 and C2: identical to run 6 — plateau 1's band (79° off its ramp) and its top's reachability.
- Option (c) changes only the ramp bearing; no remaining failure is attributed to the ramp, so these data do not point
  to (c).

### 16.5 Gates against run 7 (S-LM1)

- **1337: R4, R5, R8, R9, R10 pass** (run 7 failed R5 and R10 — the planner's uphill reach lay in the dammed hollow).
  **R7 fails**: three S8 depressions (§16.3), one lake found by the analysis, and axis climbs up to 44.2 m (ridge 0 side 1
  valley 8, unchanged from run 7) — ~~the mountain stage~~ *corrected in §17.4: 4.3 m of that climb come from the stages
  before the plateaus, the rest from plateau 1's flank.*
- **110201: R10 now fails** (run 7 passed): reach_11 flows uphill at node 18, beds 69.99 → 79.93 m, at (866.7, 2113.2) — a
  corridor vertex that plateau 1's profile raises from 74.1 m to 126.3 m (152° off its ramp bearing). Within 100 m of the
  node the lattice changed by at most 0.2 m from run 7, and the raised vertex was there in run 7 too: the planner's
  network changed because the lattice changed elsewhere; **the exact cause is not established.** R5 fails with it (a
  planner failure fails R5; run 7 failed R5 too, four elevated systems ending at undesignated mouths). R4, R7, R8, R9 fail
  as in run 7.
- S-LM3 unchanged (closed, §12).

### 16.6 Findings

- Rule (b) removed what it was meant to remove: on 1337 the widening sector's dam (22.6 m, 3.5 ha) and the diversion of
  52.5 ha away from outlet 0. The ramp slope stays ≤ 20° on both seeds, and every earlier stage is bit-equal.
- The check still fails because it reads the whole plateau stage: plateau tops and flanks standing across drainage lines
  (1337), and a band depression plus an unreachable top (110201). These belong to the plateau stage's placement and
  profiles, not to the ramp.
- 110201's new R10 failure sits at a corridor vertex raised 52 m by plateau 1's flank — the same kind of placement
  problem — although why the planner now routes through it is not established.
- **Open for the owner:** how rule (b) is treated after a failed check whose failures all lie outside the ramp; and
  whether plateau placement across drainage lines and corridors is examined next — before or after the recording run of
  §13.1.

### 16.7 Owner decision after run 8 (2026-10-23)

**Rule (b) stays in the code as implemented**: its target — the widening sector's dam — is met, and the remaining failures
lie outside the ramp. The owner asked for a decision note on plateau placement across drainage lines and corridors
(1337 plateaus 0 and 1; 110201 plateau 1 and R10), on existing data only, without code or runs: §17.

## 17. Decision note — plateau placement across drainage lines and corridors (2026-10-23)

Read-only on run 8's outputs (S-LM1 stage dumps 07–09, final lattices, layout maps, readings); scripts and outputs in
`research-data\g3e-step1\checks-plateau-placement\`. No code, no run.

### 17.1 How plateaus are placed and built today

- **Layout (LM2).** A plateau's centre lies on upland, between r0 + 10 m and r0 + 150 m from the highland (so every
  plateau stands at the mountain foot, its joint facing the highland — D6's "integrated into ridges or foothills"), at
  least 30 m beyond r0 from lowland and 40 m from the coast, plateaus 500 m apart. Its top, band (escarpment bearings,
  edge / tan 65° + 10 m wide) and ramp region (1.15 × edge / tan 18° long) are painted only on upland **or corridor**
  ground; corridors are the 50 m half-width strips painted along trunks and pass corridors. The ramp bearing points to
  the nearest lowland; nothing checks the land along it or the shore at its end. The layout knows no drainage line.
- **S-LM1's plateau stage.** max(current, profile) on every non-top land vertex within reach, **whatever its region**:
  escarpment — a 70° wall, then 33° talus that keeps descending as long as the ground is lower; the gradual/joint blend
  over max(60 m, 1.5 × edge); the ramp strip (rule (b)). It does not consult corridors, declared axes, drainage or the
  coast.
- **So the layout reserves a smaller footprint than the synthesis writes**, and every case below sits in that gap.

### 17.2 Measurements

| Plateau | Class edge | Raised outside its painted footprint | Of it in corridor / mountain / lowland | Raised > 1 m up to, beyond the top outline | By profile (> 1 m) | Stage-07 drainage it intercepts → stage 08 | Harm |
|---|---|---|---|---|---|---|---|
| 110201 p0 | 30.8 m | 2.26 ha | 0.23 / 0.75 / 0.26 ha | 84 m | blend 1.35, talus 0.20, ramp 0.24 ha | 5.09 ha → 5.07 ha keep their shore | pass corridor 1's axis raised up to +37.6 m (climb 36.7 m at stage 08, removed along the axis line by the running minimum); 0.07 ha of corridor still > 5 m — no gate fails |
| 110201 p1 | 93.1 m | 6.06 ha | **1.52** / 3.17 / 0 ha | 134 m | blend 4.69, talus 0.93 ha | 10.44 ha (none to a designated outlet) → 10.30 ha move to another undesignated shore | **pass corridor 0 raised up to +123 m** (axis climb 122.6 m at stage 08, removed along the axis line only); 0.58 ha of corridor still > 5 m in the final lattice, among it R10's node (+52 m, 42 m from the axis); **the ramp runs into a sea cliff** (C2) |
| 1337 p0 | 20.8 m | 3.39 ha | 0 / 0.49 / 0 ha (upland 2.91 ha, up to +72 m) | 126 m | talus 1.32, blend 1.51 ha | 12.91 ha (10.66 to a designated outlet) → 10.04 keep, **0.83 ha to an undesignated shore** (C4), 2.04 ha move between undesignated shores | **its top stands on a route** to outlet 1 |
| 1337 p1 | 65.1 m | 4.51 ha | 0 / 2.13 / 1.07 ha | 212 m (ramp strip), 92 m (blend) | blend 2.59, talus 0.86, ramp 0.71 ha | 50.26 ha (50.13 to a designated outlet) → 50.17 keep, **0.72 ha pooled** (C1) | **its flank closes the lower end of valley 8** (36 nodes raised up to +51.8 m, 144–210 m from the centre): that axis's climb 4.3 m → 44.2 m (R7(c)); all three of 1337's S8 depressions |

- Profile attribution matches S-LM1's formulas to the stage-08 heights; every vertex raised by more than 1 m outside the
  painted footprint matched one profile within 3 m.
- The ground raised outside 110201 p1's painted footprint is steep: 32 % of its quads have a triangle ≥ 45°, median 39.7°.
- 110201 p1's ramp: the centreline leaves the top at about 110 m from the centre, falls at the ramp angle to 151 m height
  at 172 m and meets the sea at 174 m, where the layout's shore intent is cliff — the top's only gentle access ends at a
  ~150 m sea cliff. The ramp would need about 300 m of land.

### 17.3 What the measurements show

1. **Interception is normal; harm is local.** Every plateau intercepts mountain-edge drainage (5–50 ha), because placement
   puts it at the mountain foot; on three of the four almost all of it still reaches its shore around the plateau. Harm
   appears where (a) a **top stands on a route** (1337 p0), (b) a **flank closes a valley's lower end** with no way round
   (1337 p1), (c) a **flank fills a corridor** (110201 p1), (d) a **ramp leads off the land** (110201 p1).
2. The **running minimum** restores trunks and pass corridors along their axis line only; S-LM1's valley axes are not in
   it, and corridor width beyond the axis line stays raised.
3. **No plateau fix alone makes a seed pass.** On 1337 the plateau stage causes all three S8 depressions and the 44.2 m
   climb, but R7(c) also fails on five axis climbs made before the plateau stage (1.04–3.78 m — the §13.1 question). On
   110201 R4, R5, R7, R8 and R9 fail for other reasons, and the pass col is unreachable already on stage 07.
4. **A reading of §6 for the owner.** The specification's ownership rule gives the plateau the vertices of its outline
   and a partition of unity across a declared blend band (60–150 m) where it meets a neighbour. The gradual/joint blend
   fits that reading. The talus that keeps descending past the painted band (up to 126 m beyond the outline) and the
   max() overlay on flank-frame, corridor and lowland ground are not described by §6. Whether this is a deviation from §6
   or a permitted reading is the owner's decision.

### 17.4 Corrections to §16

- 1337, R7(c): the 44.2 m climb on ridge 0 side 1 valley 8 is **4.3 m before the plateau stage and 44.2 m after it** —
  plateau 1's flank, not the mountain stage (§16.5 corrected).
- 110201: the pass col is unreachable on stage 07, before any plateau; the plateaus are not its cause.
- 110201, C2: the cause is located — the ramp bearing toward the nearest lowland leaves about 64 m of ramp before a sea
  cliff.

### 17.5 Options

- **(a) The layout reserves what S-LM1 writes** (a layout change). A plateau candidate is feasible only if its write
  footprint — the outline plus the blend width, the talus reach and the ramp length along the ramp bearing — lies on
  upland (no corridor, trunk, lowland or coast inside it), and the ramp bearing has at least the ramp length of land and
  does not end at a cliff shore. *Removes:* 110201 p1's corridor fill and its cliff ramp (C2). *Does not see:* drainage
  lines that exist only in S-LM1 (1337 p0's route, 1337 p1's valley 8). *Costs:* new layouts on both seeds — new hashes,
  so stages 01–07 and every earlier comparison no longer hold; possibly fewer plateaus (V-L6 needs 2–4); an amendment to
  LM2 in the specification.
- **(b) S-LM1 never raises declared drainage** (a synthesis change). The plateau stage leaves corridor ground and the
  floors of declared axes — trunks, pass corridors, S-LM1's valley axes — at their stage-07 height, with banks at the
  bank angle already used for lowered floors (30° over up to 40 m). *Removes:* the corridor fill and R10's raised node on
  110201; the valley-8 climb on 1337. *Unknown:* whether 1337's pools drain once valley 8 is open — the largest pool lies
  75–79 m from the axis's raised segment, the band depression 219 m from any declared axis. *Does not remove:* 110201's
  cliff ramp, 1337 p0's top on a route. *Costs:* notches and banks where a plateau flank meets a corridor or valley — a
  gorge between plateau and mountain at valley 8's mouth (up to ~52 m deep); the layout and stages 01–07 unchanged.
- **(c) S-LM1 routes undeclared drainage around plateaus.** Stage-07 drainage lines above a catchment threshold that
  meet a plateau's write footprint get a running-minimum bypass along the foot to their own continuation. *Removes:* the
  flank cases, possibly the band depression. *Does not remove:* a top standing on a route (only lowering or moving the
  top would). *Costs:* a new threshold and a new routing rule — the most complex option, closest to the drainage
  simulation the project avoids.
- **(d) Record and stop.** Leave the plateau stage as it is and carry these cases as residuals into the not-ready report.

### 17.6 Recommendation

**(b) as the next separate S-LM1 patch**, with a pre-registered check: no plateau raises a corridor or a declared axis
floor (stage 08 against stage 07); the valley-8 climb back to its stage-07 value; C1–C4 of §16 reported again; stages
01–07 and the layout bit-equal to run 8. It keeps the layout — and with it every comparison so far — and acts on the
declared routes the gates measure. **(a)'s two layout tests** (footprint off corridors; ramp bearing with enough land and
no cliff) are the right fix for 110201's C2 and corridor, but they change LM2 for both seeds; I recommend recording them
as a layout amendment for the owner rather than a Step 1 patch. **(c) is not recommended** — its complexity outweighs one
plateau case. Whatever is chosen, 1337 cannot pass R7 without the stages before the plateaus (§17.3 item 3), so the
recording run of §13.1 stays needed.

## 18. Run 9 — option (b): S-LM1 never raises declared drainage (authorised by the owner, 2026-10-23)

**Authorisation.** Option (b) of §17.5 as a separate S-LM1 patch; one run on 110201 and 1337 with the check of §17.6;
no layout change (option (a) not to be done); no other change.

### 18.1 What changed

- `TerrainGenLmcSkeleton.cs` only; no new setting. A protected set P — corridor ground and the floor vertices of the
  declared axes (trunks and pass corridors at half-cell resampling, S-LM1's valley axes), minus plateau tops — is built
  before the plateau stage; the plateau stage never raises a vertex of P, and ground a plateau raises within 40 m of
  protected land is capped at that ground + distance × tan 30°, never below its height before the plateau. S-LM1's
  valley axes are computed once, before the plateau stage, and reused by the running minimum (identical axes).
- **Tops exempt**, recorded in the pre-registration before the run as a narrowing of §17.6: on 110201 pass corridor 1
  crosses plateau 0's top (29 floor vertices); lowering a top along an axis would cut the outline §6 gives the plateau,
  and moving the top is option (a).
- Pre-registration `run9_preregistration.md` (SHA-256 `c5d23f3e…`, 13:04Z), check scripts validated on run 8 first.

### 18.2 Validity

NI holds on both seeds (S-LM1 stages 01–07, layout files, S-LM3 lattices bit-equal to run 8); protected manifest 73 / 73
before and after; contour A's Stage 1 hash and production `H4A` matched. S-LM1 lattices: 110201 `4a34fea4…`, 1337
`0dbe3cc3…`.

### 18.3 The pre-registered check — passes

| Seed | K1 — protected vertices raised by the plateau stage | K2 — climbs | NI |
|---|---|---|---|
| 110201 | **0** (run 8: 2 379, up to +122.8 m) | pass corridor 0 no longer raised (run 8: 23.39 → 122.58 m); pass corridor 1 13.47 → 38.35 m inside plateau 0's top — the pre-registered exemption | holds |
| 1337 | **0** (run 8: 36, up to +51.8 m) | **valley 8: 4.27 m on stages 07, 08, 09 and the final lattice** (run 8: 44.21 m) | holds |

By the pre-registered rule, **option (b) passes its check.**

### 18.4 Reported beside the check

| Seed | C1 — depressions created by the plateau stage | C2 | C3 | C4 | S-LM1 gates |
|---|---|---|---|---|---|
| 110201 | 3: (837, 2294) plateau 1's band, as before; **new** (1096, 2264) 1.07 m, 322 m², corridor next to plateau 1; **new** (1512, 1222) 3.93 m, 224 m², plateau 0's band — cut away by the running minimum at stage 09 | fails — plateau 1 top (cliff ramp), unchanged | holds | holds | **R10 passes again**; R4, R5, R7, R8, R9 fail as in run 8 |
| 1337 | 4: the three of run 8 unchanged; **new** (1411, 1627) 7.62 m, 0.16 ha at the lower end of valley 8 | holds | holds | fails — the same 0.83 ha at plateau 0's top | R4, R5, R8, R9, R10 pass; **R7 fails** |

R7's parts on the final lattice, run 8 → run 9: 110201 — S8 4 → 5 (the corridor pocket), lakes —/0, axis climbs
unchanged (worst 14.33 m, all from the stages before the plateaus); 1337 — S8 3 → 4, lakes 1 → 1, **worst axis climb
44.21 → 4.27 m**, and the remaining climbs (4.27, 3.78, 2.41, 1.46, 1.39, 1.04 m) all come from the stages before the
plateaus.

### 18.5 Where the new depressions come from (read-only attribution, after the check)

All three are the same mechanism: **a protected floor stays at its stage-07 height, but its outflow beyond the
protected ground still runs through ground the plateau raises**, so the protected ground becomes a closed pocket.

- 1337 (1411, 1627): 3 m from the lower end of valley 8. The axis is now open, but its continuation beyond the axis end
  crosses upland that plateau 1 raises by up to 65 m, exactly as in run 8 — the climb of run 8 became a 7.6 m pool.
- 110201 (1096, 2264): corridor ground beside plateau 1; its stage-07 route leaves the corridor sideways through upland
  and band raised by 16–54 m.
- 110201 (1512, 1222): pass corridor 1's floor inside plateau 0's band, now kept low; its route runs on through banked
  band ground and into the exempt top — the pre-registered top crossing.

### 18.6 Findings

- Option (b) does what it was specified to do: no declared floor and no corridor is raised by a plateau; 110201's R10
  passes again; the plateau-made valley-8 climb on 1337 is gone.
- It protects the declared lines, not their outflow. Where a plateau stands across the continuation of a declared line —
  beyond an axis end, sideways out of a corridor, through a top — the blockage moves downstream and a climb becomes a
  pool. Removing that needs either routing the outflow around the plateau (option (c) of §17.5) or not placing the
  plateau there (option (a)); a wider protection would be (c) by another name.
- 1337 still fails only R7, and every remaining axis climb there comes from the stages before the plateaus — the
  question of §13.1 — so no plateau change alone makes 1337 pass.
- **Open for the owner:** whether (b) stays in the code; and the next step among the recording run of §13.1, option (c),
  option (a) as a layout amendment, or the not-ready report.

### 18.7 Owner decision after run 9 (2026-10-23)

**Option (b) stays in the code.** Next: the recording run with u, v and the owning frame for the axis climbs and the
mountain-stage depressions (§13.1) — its pre-registration first; the run only after the owner's authorisation.

## 19. Recording run 10 — pre-registered, not authorised (2026-10-23)

Pre-registration `run10_preregistration.md` (SHA-256 `f9bc6dff…`, 14:40Z), with the baseline and the analysis script,
in `research-data\g3e-step1\prereg-run10\`. The recording code is described there and not written.

- **Items.** The 18 axis climbs over 0.25 m that the stages before the plateaus leave on the final lattice (12 on 110201,
  6 on 1337) and the four mountain-stage depressions on 110201. All the climbs appear at the mountain stage; one (110201
  ridge 2 side 0 valley 2) grows at the pass corridors. Eleven are a jump between two consecutive axis nodes, seven a long
  climb toward the outer end — four of those where the base B itself climbs along the axis by 13.9–29.1 m. Three of the
  four depressions spill through the vertex next to their floor: a step in the height field between neighbours.
- **What is recorded** (`-lmcDiag true` only, nothing written into the construction): for every mountain vertex both
  nearest frames, their distances and blend weight, both frame heights, and frame 1's terms — owner arc, side, u, dB,
  crest, R, flank, v, w, depth — plus the boundary vertex each toe comes from.
- **Hypotheses**, none assumed: u falling while d grows (§13.1's candidate); toe jumps or a rising toe; frame blending;
  the valley weight; the crest; a frame or side switch; the pass corridors for the one stage-07 climb.
- **Analysis fixed in advance**: validity (bit-equal to run 9; recorded terms reproduce stage 06 within 0.01 m), then a
  midpoint split of every climbing stretch and of every floor-to-spill rise into toe, crest, u, w, blend and residual;
  a term with ≥ 75 % of the positive contributions is dominant, otherwise "mixed". The depression geometry was validated
  on run 9 (depths 7.24, 6.57, 4.37, 2.14 m); the split itself needs the recorded fields.
- The run attributes causes and fixes nothing.

## 20. Recording run 10 — the mountain stage's climbs and depressions (authorised by the owner, 2026-10-23)

**Authorisation.** The recording code and one run on 110201 and 1337, exactly as pre-registered (§19): recording under
`-lmcDiag` only, no model change; no other change.

### 20.1 What changed and validity

- `TerrainGenLmcSkeleton.cs` only: under `-lmcDiag`, `Mountain`, `FrameHeight` and `ToeField` record their terms into
  `seed_*/diag/lm1_mtn_*.f32` (18 fields per vertex). The other five files equal run 9's.
- **A1 holds on both seeds:** every lattice, stage dump 01–09 and layout file bit-equal to run 9; protected manifest
  73 / 73 before and after; the recorded terms reproduce stage 06 exactly (largest difference 0.0000 m over 113 373 and
  113 317 mountain vertices), and G(u, w) reproduces flank × (1 − depth) to 4 × 10⁻⁸.

### 20.2 Axis climbs (A2) — the split of each climbing stretch

| Seed | Axis | Climb | Toe | Crest | u | Blend | Attribution |
|---|---|---|---|---|---|---|---|
| 110201 | ridge 0 side 1 valley 6 | 14.33 | +20.11 | 0 | −5.78 | 0 | **toe** |
| 110201 | ridge 1 side 0 valley 1 | 10.48 | +9.01 | 0 | −4.09 | +5.55 | mixed: toe 62 %, blend 38 % |
| 110201 | ridge 3 side 0 valley 3 | 7.26 | +3.72 | 0 | −1.85 | +5.38 | mixed: blend 59 %, toe 41 % |
| 110201 | ridge 0 side 1 valley 12 | 6.77 | +7.58 | 0 | −0.81 | 0 | **toe** |
| 110201 | ridge 0 side 1 valley 18 | 5.85 | +0.08 | +0.04 | +5.73 | 0 | **u** |
| 110201 | ridge 0 side 1 valley 10 | 5.43 | +9.59 | 0 | −4.16 | 0 | **toe** |
| 110201 | ridge 1 side 1 valley 0 | 3.80 | +2.30 | 0 | +0.37 | +1.12 | mixed: toe 61 %, blend 30 % |
| 110201 | ridge 0 side 0 valley 17 | 2.11 | 0 | +0.41 | +1.70 | 0 | **u** |
| 110201 | ridge 2 side 0 valley 2 | 1.86 (06) | +2.45 | +0.03 | −0.62 | 0 | **toe** |
| 110201 | ridge 0 side 1 valley 0 | 1.41 | +0.11 | +0.35 | +0.96 | 0 | mixed: u 68 %, crest 24 % |
| 110201 | ridge 0 side 0 valley 3 | 1.05 | +0.13 | +0.47 | +0.45 | 0 | mixed: crest 45 %, u 43 % |
| 110201 | ridge 0 side 0 valley 15 | 0.70 | 0 | +0.70 | 0 | 0 | **crest** |
| 1337 | ridge 0 side 1 valley 8 | 4.27 | +7.66 | 0 | −3.39 | 0 | **toe** |
| 1337 | ridge 1 side 1 valley 1 | 3.78 | +7.35 | 0 | −4.55 | +0.99 | **toe** (88 %) |
| 1337 | ridge 0 side 1 valley 0 | 2.41 | +0.12 | 0 | +2.30 | 0 | **u** — the only item where u falls while d grows |
| 1337 | ridge 2 side 0 valley 0 | 1.46 | +0.46 | +0.21 | −0.56 | +1.35 | mixed: blend 67 %, toe 23 % |
| 1337 | ridge 0 side 1 valley 14 | 1.39 | −0.07 | +0.47 | +0.99 | 0 | mixed: u 68 %, crest 32 % |
| 1337 | ridge 0 side 1 valley 16 | 1.04 | −0.16 | 0 | +1.20 | 0 | **u** |

The valley-weight term and the residual are zero on every stretch. Of all positive contributions (≈ 101 m), the toe
carries ≈ 70 %, the frame blend ≈ 14 %, u ≈ 14 %, the crest ≈ 3 %. The toe changes source along every toe-led stretch
(1–30 switches).

### 20.3 Depressions (A3) — floor to spill

| Depression | Floor → spill | Toe | u | Blend | Other | Attribution |
|---|---|---|---|---|---|---|
| (1531, 1788), 7.24 m | neighbouring vertices, toe source differs | +11.62 | −8.32 | +3.94 | — | mixed: toe 75 %, blend 25 % (just below the 75 % line) |
| (1017, 1772), 6.57 m | neighbouring vertices, toe source differs | +7.46 | −0.88 | 0 | — | **toe** |
| (1840, 1430), 4.37 m | neighbouring vertices, toe source differs | +6.29 | −1.92 | 0 | — | **toe** |
| (1575, 2084), 2.14 m | spill 34 m away, upslope | −0.53 | +3.55 | 0 | crest +0.93, w −1.82 | **u** (79 %) |

### 20.4 Descriptive checks after the analysis (not pre-registered)

- **Most of the small u and crest climbs are a sideways difference inside the axis.** In 6 of the 7 such single-step
  climbs checked, the two nodes are at the same distance from the crest, 2.7–3.9 m apart across the axis: a valley axis
  is a band of vertices around a station, ordered by distance with ties, and u (through dB) and the crest (through the
  owner arc) differ across the band. Only 1337 ridge 0 side 1 valley 0 is a rise while d grows.
- **110201 ridge 2 side 0 valley 2's stage-07 growth** (1.86 → 3.52 m): the pass corridor lowers an upstream part of the
  axis by 4.66 m while the downstream peak stays — the climb is measured from that lowered point.
- **(1575, 2084)'s downhill side** — its lowest rim vertex farther from the crest stands at the fill level; from the floor
  to it the valley weight w adds 7.29 m: the valley trough does not continue downstream there.

### 20.5 Findings

- **The toe is the main cause.** The toe is the base at the boundary vertex nearest by geodesic — piecewise constant
  over a partition of the mountain boundary. Where the base differs along the boundary, neighbouring vertices take
  different toes, the height steps by metres, and valley lines climb (six toe-led climbs, among them the four largest)
  or close into one-vertex pits (three of the four mountain-stage depressions).
- The frame blend adds climbs near the outer ends where two frames meet; u falling while d grows — §13.1's candidate —
  explains one item of 22; the valley weight explains the downhill side of one depression.
- A share of the small climbs measures the axis's own width (ties inside the band), which is a question about how R7(c)
  reads an axis, not about the surface.
- These are attributions; nothing was changed. A change to the toe, the blend or the axis reading is a separate decision.

## 21. Decision note — a continuous toe, and how R7(c) reads a valley axis (2026-10-23)

Asked for by the owner after run 10: options for a continuous toe instead of today's piecewise-constant one, and for
how R7(c) reads a valley axis that is a band of vertices. Read-only modelling on run 10's records; scripts and outputs in
`research-data\g3e-step1\checks-toe-axis\`. No code, no run.

**Limits of the modelling.** Only the mountain stage (06) can be rebuilt from the records; the later stages (pass
corridors, plateaus, running minimum) cannot, so every surface number below is a stage-06 number. Where two frames
blend (30–33 % of the mountain), frame 2's terms were not recorded and its height is shifted by the toe change × (1 −
frame 1's G) — an approximation; the rebuild of today's stage 06 from the records is exact (0.0000 m).

### 21.1 Toe options

- **T0 — today.** The base at the boundary vertex nearest by geodesic: piecewise constant, exact on the boundary.
- **T1 — harmonic toe.** The solution of Laplace's equation inside the mountain with the base on its boundary: exact on
  the boundary, continuous, and with no maximum or minimum of its own inside the mountain.
- **T2 — frame-aligned toe.** Per frame and side, the base on the boundary interpolated along the owner arc, so the toe
  is constant along each valley line.

| Seed | Option | Toe step between neighbours, 99th pct / max | Toe − base on the boundary | Stage-06 S8 depressions | Mountain heights changed, mean / max |
|---|---|---|---|---|---|
| 110201 | T0 | 4.65 / 34.83 m | 0 | 4 | — |
| 110201 | T1 | **0.58** / 30.88 m | 0 | **2** | 3.70 / 21.3 m |
| 110201 | T2 | 5.58 / 29.99 m | up to 29.66 m, > 1 m on 23 % | 1 | 2.86 / 30.6 m |
| 1337 | T0 | 1.26 / 37.98 m | 0 | 0 | — |
| 1337 | T1 | **0.25** / 0.62 m | 0 | 0 | 2.16 / 10.1 m |
| 1337 | T2 | 2.05 / 29.59 m | up to 10.34 m, > 1 m on 19 % | **2** (new) | 2.53 / 12.9 m |

The remaining large T1 steps on 110201 are where the base itself is steep along the boundary.

Stage-06 climbs of the 18 axes of §20 (m; * = blended nodes, T1/T2 approximate there):

| Seed | Axis | T0 | T1 | T2 |
|---|---|---|---|---|
| 110201 | ridge 0 side 1 valley 6 * | 14.33 | 2.88 | 0.00 |
| 110201 | ridge 1 side 0 valley 1 * | 10.48 | 4.52 | 4.87 |
| 110201 | ridge 3 side 0 valley 3 * | 7.26 | 5.48 | 4.70 |
| 110201 | ridge 0 side 1 valley 12 | 6.77 | 2.96 | 2.42 |
| 110201 | ridge 0 side 1 valley 18 | 5.85 | 5.83 | 5.93 |
| 110201 | ridge 0 side 1 valley 10 * | 5.43 | **0.00** | 0.00 |
| 110201 | ridge 1 side 1 valley 0 * | 3.80 | 12.74 | 1.95 |
| 110201 | ridge 0 side 0 valley 17 | 2.11 | 2.14 | 2.20 |
| 110201 | ridge 2 side 0 valley 2 | 1.86 | 1.45 | 1.12 |
| 110201 | ridge 0 side 1 valley 0 | 1.41 | 1.31 | 1.45 |
| 110201 | ridge 0 side 0 valley 3 | 1.05 | 0.92 | 0.94 |
| 110201 | ridge 0 side 0 valley 15 | 0.70 | 0.76 | 0.89 |
| 1337 | ridge 0 side 1 valley 8 | 4.27 | **0.24** | 0.00 |
| 1337 | ridge 1 side 1 valley 1 * | 3.78 | 3.74 | 0.25 |
| 1337 | ridge 0 side 1 valley 0 | 2.41 | 2.42 | 2.30 |
| 1337 | ridge 2 side 0 valley 0 * | 1.46 | 1.30 | 1.28 |
| 1337 | ridge 0 side 1 valley 14 | 1.39 | 1.36 | 1.34 |
| 1337 | ridge 0 side 1 valley 16 | 1.04 | 1.16 | 1.27 |

- **T1** removes the step-type toe climbs and halves the stage-06 pits on 110201, without touching the boundary. It does
  not remove the long climbs toward an axis's outer end: there the base itself rises toward the valley mouth (§19), and a
  smooth toe still rises. On blended axes its numbers are approximate — one (110201 ridge 1 side 1 valley 0, 12.74 m)
  rises and cannot be trusted either way without frame 2's terms.
- **T2** clears more toe climbs along the axes, but moves the discontinuity to the mountain's edge (up to 30 m against
  the ground outside, on a fifth of the boundary) and creates two new pits on 1337.
- Neither touches the u, crest and tie climbs.

### 21.2 How R7(c) reads a valley axis

Today the specification samples a declared axis at its nodes in declared order (§7.1, R7(c)). S-LM1 declares a valley axis
as every vertex its frame owns within 0.6 cell of a valley station's arc, ordered by distance from the crest — a band
of one to three vertices per distance level, ties in no particular order (§20.4).

- **R-a — today.** Nodes in declared order, ties included.
- **R-b — the gate reads the lowest vertex of each distance level.** A change to an approved calculation in the middle
  of calibration, and one that picks the most favourable vertex — it would read as fitting the gate to the candidate.
- **R-c — S-LM1 declares a line.** One vertex per distance level — the one nearest the station's arc — so the declared
  valley line is a line; the gate is unchanged.

| Seed | Valley axes climbing > 0.25 m on the final lattice | Today (R-a) | R-b | R-c |
|---|---|---|---|---|
| 110201 | of 21 | 12 | 7 | 7 |
| 1337 | of 18 | 6 | 4 | 5 |

Under both readings the tie climbs of §20.4 disappear (110201 valleys 3, 15, 17, 18, side 1 valley 0); the toe, blend and
long climbs stay. On stage 06, T1 together with the lowest-per-level reading still leaves 7 of 21 axes on 110201 and 2 of
18 on 1337 above 0.25 m (T1 approximate on blended nodes).

### 21.3 A rule already in the contract: valley floors as a running minimum

The technical draft §5, which the specification's §6 adopts, says "every valley floor a running minimum downstream".
The code applies the running minimum to trunks and pass corridors only; valley axes were left out on Q9 #11's premise
that the mountain's valley lines are "monotone by construction and only measured". **Run 10 refutes that premise.**
Simulated on the final lattice with Q9 #16's banks (30° over 40 m), the running minimum on the valley axes brings every
valley-axis climb to 0, but the banks cut far beyond the floors: no single floor cut exceeds 5.9 m, yet 838 vertices on
110201 and 727 on 1337 are lowered by more than 30 m, and on 1337 the culmination at (1583, 1796) is cut from 230 m to
158 m — valley axes start next to the crest. With these banks the rule is not usable in the mountains; without banks it
leaves one-vertex slots (Q9 #11's original problem). This is a contract item the owner should know about, not a
proposal.

### 21.4 What none of these options removes

The long climbs toward an axis's outer end on 110201 (ridges 1 and 3) and 1337 (ridge 1) follow the base rising toward
the valley mouth along the mountain boundary, plus frame blending: the valley stations are placed by arc and jitter, not
where the base is low along the boundary. That is a question of where S-LM1 puts its valleys, outside this note's two
questions.

### 21.5 Recommendation

- **T1 (harmonic toe)** as the next separate S-LM1 patch, pre-registered: it addresses the main cause of §20 at its
  source, keeps the boundary exact, and changes only stage 06 onward. Not T2.
- **R-c (S-LM1 declares a valley line)** as its own separate patch, if the owner wants the axis question settled: it
  keeps the approved gate and corrects what S-LM1 declares; report today's reading beside it for every run. Not R-b.
- **Do not** add valley axes to the running minimum with today's banks.
- **Expect neither to make a seed pass R7(c):** the long climbs of §21.4 remain. If the owner does not want to open where
  S-LM1 places its valleys, these two patches are the last ones before the not-ready report.

## 22. Run 11 — T1, the harmonic toe: pre-registered, not authorised (2026-10-23)

The owner asked for the pre-registration of the T1 patch and one run; the code and the run wait for the owner's
authorisation. Pre-registration `run11_preregistration.md` (SHA-256 `bd78f8b7…`, 18:06Z), with the reference, the check
script and its validation, in `research-data\g3e-step1\prereg-run11\`.

- **Patch** (`TerrainGenLmcSkeleton.cs`, `ToeField` only): boundary vertices keep their own base; interior mountain
  vertices take the solution of the discrete Laplace equation with those boundary values — conjugate gradients in double
  precision, today's toe as the start, stopping at a relative residual of 10⁻¹² or 50 000 iterations, the result and any
  non-convergence written into the construction report. Everything that reads the toe follows; of the crest profile's
  end heights only ridge 0's start moves noticeably (toe −0.81 m on 110201, +0.31 m on 1337).
- **Reference**, computed before the run on run 10's base: the harmonic toe to 10⁻¹²; it differs from the decision note's
  10⁻⁷ solution by ≤ 0.0001 m, satisfies the mean-value property to 6 × 10⁻¹² m, has no interior extremum and equals the
  base on the boundary.
- **Check**: NI (stages 01–04, layout, S-LM3 bit-equal to run 10); K1 (toe = base on the boundary, exactly); K2 (toe
  within 0.01 m of the reference); K3 (where §21's modelling was exact, stage 06 reproduces it within 0.01 m). **T1 is
  implemented as specified when NI, K1 and K2 hold**; K3 tests the note. Validated on run 10, where K2 and K3 fail for
  today's toe (27.7 and 21.3 m on 110201), as they must.
- **Expectations, not criteria**: smaller toe steps and fewer stage-06 pits on 110201; the toe-led climbs fall; the long
  climbs toward valley mouths and the tie climbs stay; R7(c) does not pass on either seed; every gate may move either way
  because the later stages act on a different mountain.

## 23. Run 11 — T1, the harmonic toe (authorised by the owner, 2026-10-24)

**Authorisation.** The T1 code and one run on 110201 and 1337, exactly as pre-registered (§22); no other change.

### 23.1 What changed

`TerrainGenLmcSkeleton.cs` only: `ToeField` keeps each mountain-boundary vertex's own base and solves the discrete Laplace
equation for the interior by conjugate gradients (double precision, today's toe as the start, relative residual 10⁻¹²);
its call writes the iterations and residual into the construction report. `lm1_mtn_toe_src` now records −1. The other
five files equal run 10's. Conjugate gradients converged in **1 430 and 1 422 iterations** (relative residual 9.97 ×
10⁻¹³ and 9.83 × 10⁻¹³) — the same counts as the Python reference.

### 23.2 The pre-registered check — T1 is implemented as specified

| Seed | NI | K1 — toe = base on the boundary | K2 — toe against the reference | K3 — §21's modelling where it was exact |
|---|---|---|---|---|
| 110201 | holds | holds (1 306 vertices, exactly) | **max 0.0000 m** | holds, max 0.0001 m (32 351 vertices) |
| 1337 | holds | holds (1 296 vertices, exactly) | **max 0.0000 m** | holds, max 0.0001 m (55 107 vertices) |

At blended vertices the note's approximation erred by median 0.05 / 0.02 m, 95th percentile 0.78 / 0.58 m, at most
1.21 / 2.37 m. The crest moved at most 0.57 m (110201) and 0.22 m (1337), from ridge 0's start (post hoc).

### 23.3 What T1 changed

| Measure | 110201: run 10 → run 11 | 1337: run 10 → run 11 |
|---|---|---|
| Toe step between neighbours, 99th pct | 4.65 → **0.58 m** | 1.26 → **0.25 m** |
| Stage-06 S8 depressions | 4 → 2 — all four old pits gone; two shallow new ones, (1482, 1548) 0.64 m and (1583, 2081) 1.04 m, led by the valley weight and the flank (post hoc), not the toe | 0 → 0 |
| Toe-step climbs (stage 06 = final) | valley 6 14.33 → 2.85; valley 10 5.43 → **0.00**; valley 12 6.77 → 2.96 m | valley 8 4.27 → **0.24 m** |
| Long climbs toward valley mouths | ridge 1 side 0 valley 1 10.48 → 4.46; ridge 3 side 0 valley 3 7.26 → 5.51; **ridge 1 side 1 valley 0 3.80 → 12.88 m** (toe-led, 84 % — post hoc) | ridge 1 side 1 valley 1 3.78 → 3.74 m |
| Tie climbs (§20.4) | unchanged (5.83, 2.14, 1.30, 0.92, 0.76 m) | unchanged (2.42, 1.36, 1.16 m) |
| Valley axes > 0.25 m on the final lattice (today / lowest per level / one per level) | 12 / 7 / 7 → **12 / 7 / 7** | 6 / 4 / 5 → **5 / 2 / 3** |
| R7 on the final lattice | S8 5 → 4; lakes 0 → 0; worst climb 14.33 → 12.88 m | S8 4 → 4; lakes 1 → **0**; worst climb 4.27 → 3.74 m |
| R8 | still fails (a site, plateau 1's top), but **the pass col is now reachable** | passes |
| Plateau checks | protection K1 holds; C1 3 (as run 9); C2 plateau 1's top; **C4 now fails, 0.65 ha** — outlet 0's catchment 67.30 → 65.46 ha on the changed mountain | protection K1 holds, valley 8 0.24 m on every stage; C1 4, shallower (the valley-8-end pocket 7.62 → 5.45 m, (1488, 1605) 3.78 → 1.28 m); C4 0.83 ha at plateau 0's top, as before |
| S-LM1 gates | unchanged: R4, R5, R7, R8, R9 fail | unchanged: only R7 fails |

### 23.4 Findings

- T1 does what §21 predicted, and the modelling is confirmed where it was exact: the toe is continuous, the boundary is
  untouched, the three toe pits of 110201 are gone, and the step-type toe climbs fall (1337's valley 8 now within the
  0.25 m limit).
- It does not touch the long climbs toward valley mouths — a smooth toe carries a higher base at the mouth inward, so one
  of them grew to 12.88 m — nor the tie climbs, nor the plateau pools; and the changed mountain moved 0.65 ha of 110201's
  drainage away from outlet 0.
- No gate changed state. 1337 still fails only R7 — four plateau pools and five axis climbs (1.16–3.74 m); 110201 fails R4,
  R5, R7, R8 and R9.
- **Open for the owner:** whether T1 stays; and the next step — R-c (§21.5), where S-LM1 places its valleys (§21.4), or
  the not-ready report.

### 23.5 Owner decision after run 11 (2026-10-24)

**T1 stays in the code.** The owner asked for the not-ready report of G3e Step 1, from the existing data and without a
new run: §24.

## 24. Step 1 ends not ready (2026-10-24)

The report is [GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md). In short: after eleven runs neither synthesis passes every hard gate on either
calibration seed — S-LM3 closed with a negative result; S-LM1 fails R4, R5, R7, R8 and R9 on 110201 and R7 on 1337 — so
there is no candidate for Step 2, no freeze and no held-out evaluation; the held-out seeds were never generated. The
report separates the shared failure modes (the LM2 layout's plateau placement and ramp bearing) from the
representation-specific ones (S-LM3's non-convergence; S-LM1's valley placement against the base, frame blending, valley
troughs, plateau profiles beyond their footprint), records the measurement and contract items (R7(c)'s axis band; the
running minimum on valley floors), lists what Step 1 established, and puts the commit and any follow-up to the owner.

**Owner decision (2026-10-24):** the report is accepted and **G3e Step 1 is closed as not ready**. The documents and the
research code accumulated since 2026-10-19 are committed to `main` step by step and pushed.
