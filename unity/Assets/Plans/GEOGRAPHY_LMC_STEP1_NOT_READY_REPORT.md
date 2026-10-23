# G3e (LMC) — Step 1 Not-Ready Report

Version: 0.2

Status: **Accepted by the owner on 2026-10-24 — G3e Step 1 is closed as not ready.** Written the same day from the
preserved Step 1 data only; no new run and no generation. It authorises nothing further.

Last Updated: 2026-10-24 (v0.2: accepted by the owner; Step 1 closed; the backlog committed to `main` step by step)

Depends On:
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (§§7.5, 9, 11),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (§7.1, §14),
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md),
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) (the run-by-run record, §§1–23)

Referenced By:
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) §24,
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §16,
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-3, [14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md),
[AGENTS.md](../../../AGENTS.md)

---

## 1. Verdict

**G3e Step 1 ends not ready.** After eleven runs on the calibration seeds 110201 and 1337, neither synthesis passes
every hard gate on either seed. S-LM3 was closed with a negative result; S-LM1, after four separately authorised fixes,
fails five gates on 110201 and one (R7) on 1337. There is therefore no candidate for Step 2 (the owner's blind review and
method selection), no freeze (Step 3) and no held-out evaluation (Steps 4–5).

- This is a **Step 1 outcome on calibration seeds, not a held-out failure**: the passage rule of the design note §7.5
  (all hard gates on ≥ 7 of 10 held-out seeds and the owner's visual acceptance on ≥ 7) was never applied.
- The held-out seeds 1005–1014 and the reserves 1015–1024 were **never generated or inspected**.
- Following the design note §11 for "neither method passes", §4 separates the shared failure modes from the
  representation-specific ones. G3d is not started as a substitute; no result is attributed to island area — contour A
  was fixed throughout (GEO-1).

## 2. What Step 1 did

| Run(s) | Date | What | Outcome |
|---|---|---|---|
| continuity | 2026-10-23 | the C# metrics against G3c's four calibration grids | 408 / 408 checks within the pre-registered tolerances |
| 1–5 | 2026-10-23 | calibration of S-LM3 and S-LM1; named fixes Q9 #1 … #16 (defect log) | no synthesis accepted; S-LM3's receiver iteration never converged |
| — | 2026-10-23 | owner decisions on PD-1, Q9 #9 / #14, M-1, #12 (review §9) | PD-1 a working interpretation; #9 not a solution; #14 rejected; M-1 option (c) checked |
| 6 | 2026-10-23 | diagnostic recording, #14 reverted (review §11) | S-LM3's changes are 96–97 % in the hillslope regime; S-LM1's 1337 hollow dammed by a plateau ramp |
| — | 2026-10-23 | owner: **S-LM3 closed with a negative result** (review §12) | S-LM1 continues alone |
| 7 | 2026-10-23 | M-1 in the measurement code (review §14) | pass readings equal the Python rule; gates unchanged |
| 8 | 2026-10-23 | S-LM1 ramp rule (b), a 40 m strip (review §16) | the 22.6 m dam and the 52.5 ha diversion gone; check failed for causes outside the ramp |
| 9 | 2026-10-23 | option (b): S-LM1 never raises declared drainage (review §18) | check passed; 110201's R10 passes again; new pools where outflow stays raised |
| 10 | 2026-10-23 | recording of the mountain stage's terms (review §20) | the toe causes ~70 % of the climbs and three of four mountain pits |
| 11 | 2026-10-24 | T1, the harmonic toe (review §23) | toe equal to the Python reference within 0.0000 m; toe steps and toe pits gone; no gate changed state |

Every code change after run 5 had its own owner authorisation and a check written before the run; every check was
validated on earlier data before use. All four S-LM1 fixes stay in the code (run 11's state).

## 3. Final state (run 11)

| Synthesis, seed | R4 | R5 | R7 | R8 | R9 | R10 | Why it fails |
|---|---|---|---|---|---|---|---|
| S-LM1 110201 | n | n | n | n | n | Y | R4, R8, R9: one site stands on plateau 1's top, which is unreachable — its ramp runs into a ~150 m sea cliff (review §17.2). R5: four elevated systems end at undesignated mouths (not analysed in Step 1). R7: 4 S8 depressions; 12 valley axes climb, worst 12.88 m |
| S-LM1 1337 | Y | Y | **n** | Y | Y | Y | R7 only: 4 S8 pools at plateau 1 (and its band); 5 valley axes climb 1.16–3.74 m. R9 holds, but several routes rely on triangles of 44.4–44.99°, just under the 45° walking limit |
| S-LM3 110201, 1337 | n | n | n | n | n | n | the receiver iteration never converged (≈ 359 000 vertices still changing after 30 iterations); culminations not realised (M-1); planner reaches flow uphill; plateau ramps miss their tops; pass targets missed. Closed (review §12) |

## 4. Failure modes — shared and representation-specific

### 4.1 S-LM3 only

The receiver iteration of its fluvial steady state does not converge; the reorganisation sits in the hillslope regime.
The cause is probable, not proven. Every gate failure of S-LM3 follows from an unconverged surface. A changed law is a
separate research line (review §12).

### 4.2 S-LM1 only

Resolved in the code, each with a passed or explained check:

- the widening ramp sector that dammed valleys → ramp rule (b) (run 8);
- plateaus raising corridors and declared valley floors → option (b) (run 9);
- the piecewise-constant toe, stepping between neighbouring vertices → T1 (run 11).

Remaining:

- **Long climbs toward valley mouths.** Valley stations are placed by arc and jitter, not where the base is low along
  the mountain boundary; where the base rises toward a valley's mouth, the valley floor climbs — with T1 smoothly, and on
  110201 ridge 1 side 1 valley 0 up to 12.88 m (review §§21.4, 23.3). Frame blending adds climbs near the outer ends.
- **Valley troughs that stop.** Two shallow mountain pits on 110201 (0.64 m, 1.04 m) come from the valley weight and the
  flank, not the toe (review §23.3).
- **Plateau profiles beyond the painted footprint.** The escarpment talus, the gradual/joint blend and the ramp act on
  any ground within reach; protection (b) keeps declared lines open but not their outflow beyond them (review §§17, 18).

### 4.3 Shared — the LM2 layout, input to both syntheses

- **Plateaus stand at the mountain foot across drainage lines**, because placement puts them within 150 m of the
  highland. On 1337, plateau 1 causes all four S8 pools; plateau 0's top lies on a route and diverts 0.83 ha to an
  undesignated shore (C4).
- **110201 plateau 1's ramp bearing points at a sea cliff**: the bearing rule ("toward the nearest lowland") checks
  neither the land along it nor the shore at its end. One placement fact accounts for three of 110201's gate failures
  (R4, R8, R9).
- **110201 pass corridor 1 crosses plateau 0's top.**

The layout amendment that would address these (review §17.5 (a)) was not authorised in Step 1.

### 4.4 Measurement and contract

- **R7(c) and the axis band.** S-LM1 declares a valley axis as a band of one to three vertices per distance level, in no
  particular order within a level; part of R7(c)'s climbs are sideways differences inside the band (110201: 12 climbing
  axes, 7 if read as a line). The approved calculation stands; reading the lowest vertex (R-b) was rejected as fitting
  the gate; declaring a line (R-c) was recommended and not implemented (review §21.2).
- **"Every valley floor a running minimum downstream"** (technical draft §5, adopted by the specification §6) is not
  applied to S-LM1's valley axes, on Q9 #11's premise that they are monotone by construction. Run 10 refutes the premise;
  with today's banks the rule would cut culminations (review §21.3). Open contract item.

## 5. What Step 1 established

- **The measurement harness is sound**: continuity with G3c (408 / 408); M-1 implemented and equal to its Python
  computation; every recording run reproduced the construction exactly (0.0000 m).
- **The layout LM2 is valid** on both calibration seeds from run 2 on.
- **Three construction techniques work on S-LM1**, each verified by a pre-registered check: a constant-width ramp strip
  (no sector damming), protection of declared drainage (no plateau raises a corridor or valley floor), and a harmonic toe
  (continuous, boundary-exact, no toe pits).
- **S-LM1 on 1337 comes close**: every gate except R7, whose remaining parts are named (§3, §4).
- **The causes are located**, not guessed — each remaining failure in §4 is traced to a stage and a term.

## 6. What this report does not show

- Not a held-out result: no passage rule was applied.
- Not evidence that region-first geography or GEO-3's option (c) is invalid — a synthesis's failure is a finding about
  that representation (99 Open Questions, GEO-3).
- Nothing about island size: contour A was fixed.
- No visual judgement: no blind X/Y review was held, because there was no candidate.
- Not that S-LM3's law cannot converge: the cause of its non-convergence is probable, not proven.

## 7. Decisions for the owner

1. **Close G3e Step 1 as not ready** on this report — no Step 2, no freeze, no held-out evaluation.
2. **The commit.** C8 kept LMC's code uncommitted during Step 1, "the question returns before the freeze"; with no
   freeze it returns now. The six Editor-only `TerrainGenLmc*` files (referenced by nothing) and every G3e document are
   uncommitted — and so is everything after the last commit of 2026-10-19 (G3, G3b, G2, G3c, the post-G3c review and
   their documents). Options: commit the backlog step by step as the history (the project's Git History Duty), commit it
   on a separate research branch, or leave it uncommitted. **Recommendation:** commit it — documents and research code
   both, in the order of the steps, since the code is Editor-only and isolated and the documents cite it.
3. **What follows, if anything** — each a separate plan and authorisation; none starts by itself:
   - (a) **an LM2 layout amendment**: plateau write footprints kept off corridors, lowland and coast, and a ramp bearing
     with enough land and no cliff at its end (review §17.5 (a)) — addresses 110201's R4 / R8 / R9 and 1337's plateau
     pools; new layouts, so no comparison with runs 1–11 carries over;
   - (b) **S-LM1 valley placement** at the base's low points along the mountain boundary (review §21.4) — addresses the
     long climbs;
   - (c) **R-c**, S-LM1 declaring a valley line (review §21.5) — the measurement question;
   - (d) **S-LM3 with a changed law** — a separate research line (review §12);
   - G3d stays deferred until a model is promising.
   **Recommendation:** no further Step 1 patches. If the line continues, (a) and (b) together are the smallest change
   that addresses the remaining failures at their source; they change the shared layout and the valley construction, so
   they belong in a new, separately planned stage with its own pre-registration on the same calibration seeds. (c) is
   worth settling in that plan before any result is read.

## 8. Completion checklist

| Item | State |
|---|---|
| Results preserved with SHA-256 | **done** — `D:\Hobby\game-dev\research-data\g3e-step1\`: `scratchpad-g3e` (runs 1–5, continuity), `code-snapshot-run5`, `checks-2026-10-23`, `diag-run6`, `checks-m1-full`, `run7`, `checks-ramp-c4`, `run8`, `checks-plateau-placement`, `run9`, `prereg-run10`, `run10`, `checks-toe-axis`, `prereg-run11`, `run11`, `final-2026-10-24` (the final defect log), each with its `SHA256SUMS.*.txt` |
| Final code state | `run11/code-snapshot-run11` — the six `TerrainGenLmc*.cs` as run 11 compiled them |
| Every code change authorised and checked | **done** — from run 6 on, each with a pre-registration written and validated before the run; runs 1–5 recorded in the defect log |
| Protected manifest | **73 / 73 unchanged before and after every run**; contour A's Stage 1 hash `7599879a71250a06` and production `H4A` 110201 `e2bad571f4af76c7` matched every time; G3c's research files are in the manifest and unchanged |
| Reference data | `lmc-reference` untouched (MD5 manifest, 48 / 48) |
| Held-out seeds | **never generated or inspected** (only 110201 and 1337 appear in any output) |
| Production | no change to the production generator, `TerrainData`, scenes, materials, shaders or assets; LMC code Editor-only, referenced by nothing |
| Determinism check | **not done** — due only after a justified candidate; none exists |
| Blind X/Y visual review | **not held** — no candidate |
| Freeze package | **not prepared** — Step 1 is not ready |
| Commit | **done on the owner's decision (2026-10-24)**: the backlog since 2026-10-19 committed to `main` step by step and pushed — G3/G3b code, G2 brief and concept reference, G3c code, the post-G3c review, the LMC design documents, the G3e code, the G3e record, then the shared documents |
| Documentation | the run-by-run review (§§1–24), the specification (§14), the readiness check, `AGENTS.md`, 99 Open Questions (GEO-3), the roadmap and the research record updated; no new project term, so the Glossary is unchanged |
