# G3e (LMC) — Step 1 Readiness Check

Version: 0.23

Status: Draft — **a checklist for the owner's separate Step 1 authorisation; it authorises nothing.** Prepared on
2026-10-22 after the owner approved the design note (v0.5, as the research plan) and the technical specification
(v0.3), kept Step 1 unauthorised, and kept "LMC" as the working name. It reconciles the plan, the specification and the
metrics, lists what remains unverified, and states the exact conditions under which Step 1 could be authorised. No
code, Unity run, generation or production change was made for it.

Last Updated: 2026-10-24 (v0.23: report accepted; Step 1 closed. v0.22: Step 1 ends not ready — the report. v0.21: run 11 — T1 implemented and verified; gates unchanged. v0.20, 2026-10-23: run 11 — T1 — pre-registered, not authorised. v0.19: decision note on a continuous toe and the R7(c) axis reading. v0.18: recording run 10 done; the toe is the main cause. v0.17: (b) kept; recording run 10 pre-registered, not authorised. v0.16: run 9 — option (b) implemented; check passes; new pools where outflow stays raised. v0.15: rule (b) kept; decision note on plateau placement. v0.14: run 8 — ramp rule (b) implemented; its check fails for causes outside the ramp. v0.13: owner decisions after run 7 — M-1 accepted with caveats; ramp (b) next. v0.12: run 7 — M-1 implemented in the measurement code and verified. v0.11: the full M-1 rule checked on preserved data. v0.10: M-1 approved as a specification rule, spec v0.5 §14. v0.9: S-LM1 ramp rule (b) accepted, not implemented. v0.8: S-LM3 closed with a negative result, option (a). v0.7: diagnostic run 6 authorised, run and analysed. v0.6: the owner's decisions on PD-1, Q9 #9/#14, M-1, #12. v0.5: Step 1 state after five calibration runs — results preserved, run 5 reviewed, three decisions
returned to the owner. v0.4: C1 — ID G3e, C8 — commit decided before the freeze, C9 — reference data copied outside
the repository; every pre-authorisation condition met. v0.3: C2–C4 done — A1, A3 and option (b) approved, spec v0.4; open C1, C8, C9. v0.2: C2–C5 prepared on the owner's request — amendments A1–A3 in spec §13, checks on existing
G3c data, the calibration reference; C9 added; §6 state. v0.1: 2026-10-22)

Depends On:
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (v0.6 — the owner's text of v0.3 plus agent notes),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (v0.3),
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) (§3.3),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§9, 14),
[AGENTS.md](../../../AGENTS.md)

Referenced By:
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md);
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (agent review note 9);
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §15;
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-3; [AGENTS.md](../../../AGENTS.md);
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md)

---

## 1. Decision state

| Decision | Source | State |
|---|---|---|
| Pair S-LM3 + S-LM1 on a shared LM2 layout | owner, 2026-10-21 | approved |
| R5 extended to designated outlets | owner, 2026-10-21 | approved (note §7.1) |
| Design note as the research plan | owner, 2026-10-22 | **approved (v0.5)** |
| Technical specification §12 items 1–4 — world-height datum, K1, S-LM3 as an experimental model, gates with K2, flag levels §8.1, layout validation, S-LM1 ownership | owner, 2026-10-22 | **approved (v0.3)** |
| Elevation targets, plateau edge classes, metric specification, S-LM3 contract (note §12 items 4–7) | owner, via the specification | approved |
| Step 1 — implementation and calibration | owner, 2026-10-22 | **not authorised** |
| Experiment name | owner, 2026-10-22; 2026-10-23 | **LMC is the working name**; the official ID ~~is chosen by the repository's rules before implementation~~ is **G3e** (C1, 2026-10-23) |

Until Step 1 is authorised: no LMC code, no Unity run, no terrain generation, no production change.

---

## 2. Reconciliation — plan, specification and metrics

Every requirement of the design note was traced to the specification. Requirements that trace cleanly are listed in
§2.2; the four that do not are findings F1–F4.

### 2.1 Findings

| Id | Note requires | Specification says | Kind | What resolves it |
|---|---|---|---|---|
| **F1** | §7.1 R8: "settlement sites, **designated** plateau tops, and the selected pass are reachable" | §7.1 R8: reachability at "each **verified** plateau's top vertex nearest its centroid" — a plateau that fails R2's verification would escape the gate | **discrepancy** (the specification is narrower than the note) | amend the specification's R8 to "each plateau top the layout declares" — the layout gives every plateau at least one ramp (spec §4.1), so every top is meant to be reachable. **Needs the owner**: the specification is approved |
| **F2** | §7.4: Q1–Q9 "must explicitly detect" the G3c failure modes, **whale-back ridge shapes** among them | Q3 failed to separate the whale-back in the historical check (E2) and is report-only; Q4's crest-height variation along the declared divide is unverified (G3c recorded no divide); §8.1 gives neither a flag | **gap** — no verified detector for one named failure mode | (a) define a whale-back measure on the **measured** crest line of the lattice (e.g. the length of crest that stays within a narrow band of its own mean height) and check it on G3c's data now, read-only, as the others were — contour A should score worse than B; or (b) accept that until calibration the whale-back is judged in the visual review only, and verify Q4 on LMC's calibration seeds. Recommendation: **(a)**, because the note names this failure explicitly |
| **F3** | §4.4 risks to be "evaluated using the shared metrics and visual review": **slopes clustering around the configured maximum**, **similar slopes across different regions** | no metric for clustering at a slope cap; slope bands are reported for all land (G0 T-a) and Q1 per region, but no per-region slope distribution | **gap** (minor; report-only metrics) | add two report-only diagnostics to the specification: the share of land within ± 1° of each configured cap (38° and 70° in S-LM3; the wall and ramp angles in S-LM1), and slope bands per region. **Needs the owner** (amendment of an approved specification) |
| **F4** | §9 Step 1: verify that the metrics implementation reproduces the relevant G3c results **on the original G3c calibration grids** | the Python reference implementation (spec §11) ran on G3c's **evaluation** lattices; no reference values exist yet for the calibration grids (`g3c_A_s30-med_110201`, `g3c_A_s30-med_1337`) | **preparation missing** | compute the reference values on those two grids with the same read-only scripts — existing data, no generation — so Step 1's C# can be compared both with G3c's record and with the reference. Can be done before Step 1 |

### 2.2 Requirements that trace cleanly

| Note | Specification | Note | Specification |
|---|---|---|---|
| §2 target, D6 principles | §§2–8 (gates, indicators, Q) | §6 items 1–6 (datum, summit, pass, edge height, class crossing, representation) | §2 |
| §3.1 contour A only, land mask verified | §1, V-L2, process gate | §7.1 R4, R5, R7, R9, R10 | §7.1 |
| §3.2 seeds and reserve | stated in the note only; the specification adds nothing to them | §7.2 R1, R2, R6 | §7.2 |
| §3.3 one layout per seed, shares, validation | §4 | §7.3 diagnostics, tiers diagnostic | §7.3 |
| §4.1–4.3 S-LM3 model, inputs, plateaus | §5 | §7.4 facets, cones, drums, coastal band, seed invariance | Q1, Q5, Q6, Q7, Q8 with flags (§8.1) |
| §5.1–5.3 S-LM1, overlap rule, determinism | §6 | §7.5 passage rule; no retroactive change | §0 and the note |
| §8 review protocol (three scales, X/Y blinding) | §9 | §9 Step 3 freeze record | §9 |
| §10 six Editor-only files, isolation | §10 | §11 outcomes | the note |

---

## 3. What remains unverified

| Item | Why not yet | Verified when |
|---|---|---|
| S-LM3 and S-LM1 behaviour, convergence, height targeting, runtime | no code exists (Step 1) | Step 1 calibration |
| LM2 layout generation and checks V-L1 … V-L8 | no code exists | Step 1 |
| Pass offset definition (spec §2.3) | G3c declared no pass or corridors | Step 1 |
| Q4 crest variation along the declared divide | G3c recorded no divide; its measured-crest form Q4m failed to separate A from B (spec §13.5) | Step 1, report-only; the whale-back is judged in the review if option (b) is chosen |
| Plateau edge height K1 | checked post-hoc on the same G3c data | Step 1, against the layout's declared class heights |
| R9 detour K2 and the alternative-route test | checked on a flat control and four G3c lattices | Step 1 |
| R10 failure diagnosis | G3c did not record failing node positions | Step 1 |
| Views, contact sheets, review form, X/Y key | never exercised | Step 1 (calibration sheets for Step 2) |
| C# against the Python reference | no C# | Step 1 continuity check (needs F4) |
| Q references on production `H4A` | no `H4A` lattice available without a generation run | Step 1 rebuilds `H4A` for its hash check anyway; Q values on it can be reported then |
| Flag levels' meaning on non-failing terrain | only failing terrain (G3c) is labelled | Step 2 and the held-out review |

---

## 4. Conditions for the Step 1 authorisation

Step 1 can be authorised when every condition below is met or explicitly waived by the owner.

| # | Condition | Who | Possible before Step 1 without generation |
|---|---|---|---|
| **C1** | **Official experiment ID** chosen by the repository's naming rules. The experiment roadmap names steps G0–G6, with letter suffixes inside G3 for successive upstream-relief candidates (G3b, G3c) and **G3d reserved** for coastline variety; the next free ID in that line is **G3e** (recommended). | owner | yes |
| **C2** | **F1** resolved: the specification's R8 amended to cover every declared plateau top, or the narrower wording confirmed. *2026-10-23: prepared as amendment **A1** (spec §13.1) — awaits the owner's approval* | owner | yes |
| **C3** | **F2** resolved: option (a) carried out and its result accepted, or option (b) accepted. *2026-10-23: option (a) carried out as **A2** (Q4m, spec §13.2) and **failed its pre-registered check** (E10: 6 of 10, needs 7); recommendation now **option (b)** — whale-back judged in the owner's review, Q4/Q4m report-only — awaits the owner* | owner | yes |
| **C4** | **F3** resolved: the two report-only diagnostics added, or waived. *2026-10-23: prepared as amendment **A3** (D-cap with flank rule K5, D-reg; spec §13.3), checked on G3c (5.7° clustering on 20 of 20 after K5, post-hoc) — awaits the owner's approval* | owner | yes |
| **C5** | **F4** done: Python reference values on the ~~two~~ G3c calibration grids recorded with their input hashes. *2026-10-23: **done** on the four `s30-med` grids (A and B × 110201, 1337); continuity with G3c's record 4 of 4; reference table in spec §13.5* | agent, read-only | yes — done |
| **C6** | **Protected-file manifest** fixed: the 64 files G3c hashed (tile assets and `Settings/Terrain/`, the production scene, its folder and `.meta`, URP pipeline and renderer, every `ProjectSettings/*.asset`, `packages-lock.json`, the G0, G1 and G3 sources) plus G3c's four research files and their `.meta`; MD5 before and after every run; abort on any change. `git status` may show only LMC's six files, their `.meta` and documentation. | agent; owner confirms | yes |
| **C7** | **Step 1 scope** stated in the authorisation (below) | owner | — |
| **C8** | **Commit policy** for LMC's code (D9 is open; every G-series research file so far stayed uncommitted) | owner | yes |
| **C9** *(added 2026-10-23)* | **Keep the reference data**: the Python reference scripts and raw results (`…/scratchpad/lmc_spec/`, `…/scratchpad/lmc_c2c5/`) and the G3c grids they read (`…/scratchpad/g3c/eval/`, `…/screen/`) live in a temporary session folder; Step 1's continuity check needs them. Either the owner names a persistent place outside `Assets` to copy them to, or the check relies only on the values written into spec §§11.4, 13.5 | owner | yes |

**Step 1 scope to authorise (C7)** — exactly:

1. create the six Editor-only files of spec §10 (and Unity's `.meta`), namespace `GameDev.EditorTools`, referenced by
   nothing, copying any G3c helper rather than calling or editing G3c's files;
2. compile and run Unity in batch mode with no editor open on the project, Unity 6000.5.7f1 as in G0–G3c;
3. run the continuity check against G3c's calibration grids (C5) — read from `D:\Hobby\game-dev\research-data\lmc-reference\`
   (C9), compared with spec §13.5 — before any LMC candidate is compared;
4. build layouts and both syntheses on **calibration seeds 110201 and 1337 only** — the code refuses any other seed in
   calibration mode; held-out seeds 1005–1014 and reserves 1015–1024 are never generated in Step 1;
5. fix named construction defects only, each logged (Q9); never change a definition, threshold, flag level or the
   passage rule;
6. write every output outside `Assets`; never save a scene, a `TerrainData` or a production asset;
7. repeat the calibration runs in a fresh process for determinism;
8. stop and report on: a protected-hash change, a compile error outside LMC's files, a seed outside the calibration
   list, or a target the construction cannot reach without changing an approved definition.

**What Step 1 delivers for Step 2** (note §9): blinded X/Y contact sheets for both seeds at three scales, every reading
and diagnostic for both syntheses, the continuity-check result, the defect log, the determinism result and a proposed
frozen configuration. Step 2 — the owner's review and choice — and the freeze, the held-out evaluation and its
authorisation stay separate steps.

**Cost [HYPOTHESIS].** Writing the six files is the bulk of Step 1. Each calibration run (two seeds, two syntheses, views)
is estimated at minutes; the Python reference took about 20 s per lattice.

---

## 5. Recommended order

1. C1 (name) and C8 (commit policy) — owner decisions, no work.
2. ~~C5 and, if chosen, C3 (a) — read-only checks on existing G3c data.~~ **Done 2026-10-23** (spec §13.5): C5 recorded;
   C3 (a) failed its check.
3. C2, C3 and C4 — the owner's decisions on amendments A1 and A3 and on option (b) for the whale-back; on approval the
   specification becomes v0.4.
4. C9 — where the reference data is kept.
5. C6 confirmed with the authorisation.
6. The Step 1 authorisation, stating the scope of §4.

---

## 6. State on 2026-10-23

*Updated the same day after the owner's decisions on C2–C4, then on C1, C8 and C9.* ~~Open before the Step 1
authorisation: C1 (official ID), C8 (commit policy), C9 (where the reference data is kept);~~ All decided; C6 and C7 are
confirmed with the authorisation itself.

| Condition | State |
|---|---|
| C1 official ID | **done** — **G3e**, assigned 2026-10-23 (working name LMC) |
| C2 R8 scope | ~~prepared (A1) — owner~~ **done** — A1 approved 2026-10-23, in spec v0.4 §7.1 |
| C3 whale-back | ~~option (a) failed; option (b) recommended — owner~~ **done** — option (b) chosen 2026-10-23: judged in the visual review, Q4/Q4m unflagged (spec v0.4 §8.1) |
| C4 slope diagnostics | ~~prepared (A3 with K5) — owner~~ **done** — A3 approved 2026-10-23, in spec v0.4 §7.3 |
| C5 calibration reference | **done** |
| C6 protected-file manifest | listed; confirmed with the authorisation |
| C7 Step 1 scope | written (§4); stated in the authorisation |
| C8 commit policy | **done** — no commit of LMC's code during Step 1; before Step 3 (the freeze) the owner decides whether code and documentation go to a separate branch (2026-10-23) |
| C9 reference data | **done** — copied, MD5-verified (48 files, 21 MB), to `D:\Hobby\game-dev\research-data\lmc-reference\` with `MANIFEST.md5` and `README.md`; the archived scripts import and read the grids (2026-10-23) |

*After C1, C8 and C9 (2026-10-23):* every condition that can be met before Step 1 is met. What remains is the Step 1
authorisation itself, which confirms **C6** (the protected-file manifest of §4) and states **C7** (the scope of §4).

**Step 1 authorised by the owner on 2026-10-23** — "within the approved bounds C6–C7". The protected-file manifest was
recorded before any LMC file existed: 73 files (G3c's 64, G3c's four research files and their `.meta`, the G3 file's
`.meta`), HEAD `ff79ffb`.

**Step 1 state, 2026-10-23 (after five calibration runs).** The six files exist and compile; continuity with G3c holds
(408 / 408 within pre-registered tolerances); the LM2 layout is valid on both seeds since run 2; **no synthesis passes
every hard gate**; 16 named construction fixes are logged. Every run left the 73 protected files unchanged. All outputs,
the defect log and a snapshot of run 5's code are preserved, SHA-256-verified, at
`D:\Hobby\game-dev\research-data\g3e-step1\`. The review of run 5 and the three decisions returned to the owner —
PD-1, Q9 #9/#14, M-1 — are in [GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md); no further Unity
run before those decisions.

**Owner decisions, the same day** (review §9): PD-1 accepted only as a working interpretation, with conditions;
Q9 #9 not accepted as a solution; Q9 #14 rejected; the instrumented diagnostic run recommended but not authorised; M-1
option (c) chosen for checking, not implemented; Q9 #12 not to be reverted before its cause is established; Step 1
not complete, freeze not authorised. Checks on preserved data may continue; any code change, run or Unity run needs a
separate authorisation.

**Diagnostic run 6, the same day** (authorised by the owner: no model change, recording only, #14 reverted; review §11):
the four lattices reproduce runs 4 (S-LM3) and 5 (S-LM1) bit for bit, so the recording is valid. S-LM3's whole drainage
tree reorganises every iteration with a two-iteration alternation, 96–97 % of the changes in the hillslope regime;
S-LM1's base is free of closed depressions, the 1337 hollow is dammed by plateau 1's ramp, and 110201's four
depressions come from the mountain stage. No fix was made; the next decisions are the owner's.

**S-LM3 closed, the same day** (owner, option (a); review §12): its non-convergence is recorded as the negative result of
the current model; no further fitting of S-LM3 in this calibration; a changed law would be a separate research line.
G3e continues with S-LM1 alone — ramp rule, 110201's depressions and M-1 settled separately — and ends in a not-ready
report if S-LM1 also fails the hard gates. *Then:* the owner accepted S-LM1's ramp rule (b) — a 40 m strip of constant
width along the ramp bearing, with a pre-registered check (review §13.3); its implementation and run await a separate
authorisation. The owner also approved **M-1** — the pass col rule — as a rule of the specification (spec v0.5 §14),
not yet as a verified implementation; a Python-only check of the full rule on the preserved lattices comes first.
That check ran the same day (review §10.4): every expectation held, all three outcomes occur, R8 never skips the pass
target. *Then* the owner authorised the code change and one run: **run 7** (review §14) — M-1 in the measurement code,
lattices bit-equal to run 6, protected files 73 / 73, pass readings equal to the Python rule once both use the exact
declared positions (the pre-registered reference had used metre-rounded ones; reported, not tuned). Gates unchanged; no
synthesis accepted; Step 1 incomplete. *Then* (owner, review §15): M-1's implementation accepted with caveats, the 150 m
radius kept; the next priority is ramp rule (b) with a fourth criterion — no new valley damming or disruption of drainage
to the designated outlets (exact form C4 proposed, §15.2) — never widened automatically; then the recording run for
110201's two depressions, then the limited S-LM1 calibration ending in a freeze package or a not-ready report. None of
these is authorised yet. *Then* the owner confirmed C4 and authorised the ramp patch and one run: **run 8** (review §16)
— the sector's dam on 1337 is gone (outlet 0's catchment kept whole) and S-LM1 1337 now fails only R7, but the
pre-registered check fails (C1 on both seeds, C2 on 110201, C4 on 1337) for causes outside the ramp — plateau tops and
flanks across drainage lines, a band, an unreachable top — and 110201's R10 now fails. Strip not widened; (c) not
started; the next step is the owner's. *Then* the owner kept rule (b) as implemented and asked for a decision note on
plateau placement (review §17, read-only): the layout reserves a smaller footprint than S-LM1's plateau stage writes;
options (a) layout reserves the write footprint, (b) S-LM1 never raises declared drainage, (c) bypass routing, (d) record
and stop; recommended (b) as the next separate patch, (a)'s layout tests as an amendment for the owner. Nothing
authorised. *Then* the owner authorised (b) and one run: **run 9** (review §18) — no declared floor or corridor raised,
110201's R10 passes again, 1337's valley-8 climb back to 4.27 m; the pre-registered check passes. But protected floors
whose outflow still crosses raised plateau ground became three new pools; 1337 still fails only R7, whose remaining
climbs come from the stages before the plateaus. The next step is the owner's. *Then* the owner kept (b) and asked
for the recording run of §13.1 — u, v and the owning frame for the mountain stage's axis climbs and depressions —
pre-registered first: done (review §19). *Then* authorised and run as **run 10** (review §20): recording only, every
file bit-equal to run 9, the recorded terms reproduce the mountain stage exactly. The toe — base at the nearest boundary
vertex, piecewise constant — carries about 70 % of the climbs and makes three of the four pits; the frame blend and a
sideways reading of the axis band account for most of the rest; §13.1's u candidate explains one item. Nothing
changed; the next step is the owner's. *Then* the decision note on a continuous toe and the R7(c) axis reading
(review §21, read-only): recommended a harmonic toe (T1) and S-LM1 declaring a valley line (R-c), each as a separate
patch; the draft's running minimum on valley floors is unusable with today's banks; neither option makes a seed pass
R7(c), because the long climbs toward the valley mouths remain. Nothing authorised. *Then* the owner asked for the
pre-registration of T1 and one run: written (review §22) with a Python reference of the harmonic toe. *Then*
authorised and run as **run 11** (review §23): T1 implemented as specified (toe equal to the reference within
0.0000 m), toe steps and toe pits gone, 1337's valley 8 within the limit; long climbs toward valley mouths, tie climbs
and plateau pools remain, and no gate changed state. The next step is the owner's. *Then* the owner kept T1 and
asked for the not-ready report: **[GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md)** — Step 1 ends without a candidate for Step 2; no freeze, no
held-out evaluation; the commit (C8) and any follow-up are the owner's decisions. *Then* the owner accepted the
report and **closed Step 1 as not ready** (2026-10-24); the backlog was committed to `main` step by step.
