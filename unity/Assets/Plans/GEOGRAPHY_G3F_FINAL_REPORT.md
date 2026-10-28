# G3f — Final Report of the Autonomous Research Mission

Version: 1.0

Status: Draft — the agent's report under the owner's G3f mission authorisation (2026-10-25,
`research-data/g3f/gates/mission_authorization_2026-10-25.md`). Research only; it changes no production code, scene,
asset, terrain authority or approved decision. Results are recorded in
[GEOGRAPHY_G3F_TECHNICAL_SPEC.md](GEOGRAPHY_G3F_TECHNICAL_SPEC.md) v0.3 §16, which governs where the two differ.

Last Updated: 2026-10-28

Depends On:
[GEOGRAPHY_G3F_PHASE_PLAN.md](GEOGRAPHY_G3F_PHASE_PLAN.md) v0.5,
[GEOGRAPHY_G3F_TECHNICAL_SPEC.md](GEOGRAPHY_G3F_TECHNICAL_SPEC.md) v0.3,
[AGENTS.md](../../../AGENTS.md)

Referenced By:
[GEOGRAPHY_G3F_PHASE_PLAN.md](GEOGRAPHY_G3F_PHASE_PLAN.md)

---

## 1. Summary and candidate

**Selected candidate: GF-RF — the Geography Plan with planned divides and per-catchment synthesis** (D-4: "crests
set by the plan, synthesis per unit"). After two calibration runs and two named fixes (of 6 allowed) it passed every
hard and process gate on all four calibration variants, was frozen, and on the held-out seeds 5001–5010 passed every
hard and process gate on **8 of 10** variants (short 4 of 5, tall 4 of 5). **Part (1) of the pass rule (≥ 7 of 10)
holds.** Part (2) — the owner's blind review of the sealed sheets — cannot happen inside the mission and has not
happened, so **no candidate is declared passed**. GF-UF (unified emergent solve) reached 4 of 10, GF-C0 (control)
0 of 10. Production is unchanged.

## 2. Verified initial state and commits

At the start (WP0, 2026-10-25): `main` at `871224c` (decisions D-3 … D-12 recorded), working tree clean; the
85-file protected manifest recorded (`research-data/g3f/protected_md5_before.txt`) and checked before and after every
Unity process since (85 / 85 every time); contour A's Stage 1 hash `7599879a71250a06` and `H4A` 110201's pinned hash
`e2bad571f4af76c7` confirmed in every Unity process; preserved inputs verified (run 11 105/105, diag-run6 67/67,
lmc-reference 48/48); seeds 5001–5020 unused (WP0 C0-4). Commits of the mission (all on `main`, pushed, no history
rewritten):

| Commit | Purpose |
|---|---|
| `7344699` | the research package `tools/g3f/` (plan, validator, three laws, readings, views) and `TerrainGenG3fDump.cs` |
| `a5bcdd6` | the technical specification (WP1) v0.1 and the mission record in the status documents |
| `41a15c1` | calibration run 1 recorded (results commit) |
| `3565d78` | named fixes F1 and F2 registered with hypotheses before run 2 (documentation commit) |
| `6122b99` | F1, F2 and the hydro uphill diagnosis (research code commit; the frozen code) |
| `85e202e` | calibration run 2 and the freeze of GF-RF; held-out inputs, procedure and criteria fixed (results commit) |
| `0449a64` | the held-out evaluation (results commit) |
| `c1eebd3` | status records: phase plan v0.5, 14 v0.36, 99 v0.56, AGENTS.md v0.81 |

(`3570564` and `871224c`, the phase plan and the decision record, precede the mission.)

## 3. WP0 and WP1, and every test

- **WP0** (`research-data/g3f/wp0/RESULTS.md`; pre-registered, scripts hashed, run twice, outputs byte-identical):
  C0-1 — of the closed S-LM3 configuration's > 70° land triangles outside its cap bands, 100 % contain a lateral
  edge (the premise of the lateral constraint holds); C0-2 — stress catalogue S1–S12; C0-3 — sector and window
  (513 × 513 from (111, 420), sector land 1.010 km²); C0-4 — 5001–5020 unused; C0-5 — continuity targets.
- **WP1** — the specification ([technical spec](GEOGRAPHY_G3F_TECHNICAL_SPEC.md)); limits fixed before it
  (`gates/limits_before_wp1.md`: fixes 6 per candidate, runs 5, redraws 10, held-out class by position).
- **Analytic checks GF-AC1 … AC9** (spec §14.2): no pit, every unit vertex ends at its own root, no crossing or
  onto-crest step, exact write log, lateral excess ≤ 0, culmination bisection met, axis climbs 0.0000 m, GF-RF
  order-invariant, per-unit = joint and local (0 vertices outside the perturbed unit), GF-UF relabel-invariant but not
  local (as designed). Repeated with F1 (`dev_ac`): all still hold.
- **Continuity** (spec §14.3): run 11's lattices reproduce every C0-5 target within G3e-PM's tolerances; PASS.
- **Validator**: every calibration plan valid at attempt 0; held-out 9 of 10 valid within the redraw cap.

## 4. Architecture of the selected candidate, and why

One Geography Plan per variant gives every land vertex exactly one height owner: a catchment (A, B, C, each with one
outlet run), a coastal fringe unit (with a parent catchment), or a crest record (the Sector crest S, divides D1 and
D2, the fringe crest). Every landform reserves its write footprint (plateau top, band, ramp, rim channels; the
settlement reserve). Synthesis: a receiver tree built once from the plan — since F1 priced against climbing the plan
guide, the 35° cone of the planned crest profiles — then, per catchment and reading only that catchment, a fluvial
increment law bisected to its plan nodes (M1, M2, the pass S1); the plateau sub-law; planned crests; flank fill and
planned ceiling; a lateral bound (tan 35°, only lowering, same-unit) alternated with the declared-axis carve; the
coastal sub-law; one write per owner. A global lattice solve is used for research only (D-8).

Why it is preferable on this evidence: (1) with the divides emergent (GF-UF, GF-C0) each catchment's far end became
its highest ground and the divide rose away from the secondary culmination toward the coast, so M2 was not realised
on most variants (GF-C0 never; GF-UF 4 of 10 passes overall); planned divides fix that by construction; (2) GF-RF's
assembly is local and order-invariant on every valid variant (GF-A), so regions can be synthesised independently and
merged; (3) it is the only candidate whose held-out record meets part (1). Caveat (D-4): GF-UF and GF-RF differ in two
factors (planned divides and per-unit solving), so the comparison does not isolate assembly alone.

## 5. Candidates and criteria

| Criterion | GF-C0 (control) | GF-UF | GF-RF (frozen) | U-P |
|---|---|---|---|---|
| Applicability | control, no law | candidate (D-4 core) | candidate after U-F's analytic checks (D-4) | not applicable: D-3 accepted, U-P only if declined |
| Analytic checks AC1–AC9 | hold | hold (not local, by design) | hold (local, order-invariant) | — |
| Calibration run 1 (4 variants) | 1 of 4 | 2 of 4 | 3 of 4 | — |
| Calibration run 2 (after F1, F2) | 0 of 4 | 2 of 4 | **4 of 4 → frozen** | — |
| Held-out 5001–5010, part (1) | 0 of 10 | 4 of 10 (diagnostic, not frozen) | **8 of 10 — holds** | — |
| GF-R4 settlement | fails 5003 | fails 5003 | fails 5003 | — |
| GF-R5 / R10 drainage to designated outlets, planner | fail 5003 | fail 5003 | fail 5003 | — |
| GF-R7 depressions, axis climbs | fails 5003 | fails 5003 | fails 5003 | — |
| GF-R8 reachability incl. M-1 | fails every valid variant | fails 5001, 5004, 5007, 5009 | holds on every valid variant | — |
| GF-R9 routes, GF-RS ≥ 95 % | hold | hold | hold (RS 99.65–99.86 %) | — |
| GF-O single writer, GF-B no cross-divide flow | hold | hold | hold | — |
| GF-A assembly | — | not a gate (not local) | holds on all 9 valid | — |
| GF-D determinism, GF-P protection | hold | hold | hold | — |
| Plan valid (all candidates share it) | 5006 invalid | 5006 invalid | 5006 invalid | — |
| GF-T targets (diagnostic) | M1 ≈ 296 m below 330–430 m | same | same; pass offset > 80 m on 5003, 5007, 5010 | — |

## 6. Calibration and held-out results, exact seeds

- **Calibration** — seeds **110201** and **1337**, each **short** and **tall** plateau class. Run 1 (`a5bcdd6`): GF-RF
  failed R4, R5, R7, R10 on 110201 tall; GF-UF R8 on both tall; GF-C0 R8 on three. Run 2 (`6122b99`): GF-RF every
  gate on all four; GF-UF R8 on 110201 tall and 1337 short; GF-C0 R8 on all four. Records `research-data/g3f/calib/
  run1/`, `run2/` (gate tables, copy comparisons, manifests).
- **Held-out** — seeds **5001 short, 5002 tall, 5003 short, 5004 tall, 5005 short, 5006 tall, 5007 short, 5008 tall,
  5009 short, 5010 tall** (class by position), after the freeze record (23:35:58 UTC); GF-RF failed only on **5003**
  (R4, R5, R7, R10) and **5006** (plan invalid). Seeds 1005–1024 and 5011–5020 were never generated. Record
  `research-data/g3f/heldout/heldout_table.md`.

## 7. Known failures, limits and open questions

1. *5003 (every candidate): an axis mouth segment across a bay* — invalid input from a plan-builder defect. With O1
   shifted 41 m west the A_trunk's last segment crosses sea vertices; the axis carve then lowers 1 765 land vertices
   below sea level, making an enclosed water body (the planner rejects it), a 24.86 m reading climb and no capital
   room. The validator has no "axis stays on land" check. Not fixed (GF-S7).
2. *5006 (every candidate): plan invalid after 10 attempts* — mostly GF-V4 footprint overlaps. 19 of 28 held-out
   attempts were invalid against 0 of 4 in calibration. A post-freeze counterfactual (plans only) with the pre-F2
   reserve makes 5006, 5002 and 5010 valid earlier: **F2 was fitted to the calibration plans and costs tall plateaus
   room** — a generalisation weakness of a fixture correction.
3. *GF-UF's emergent divide inversion* — the reason GF-UF fails R8; not fixed by design.
4. *Residual geometry (diagnostics, no gate):* 8–13 % of GF-RF's declared lowland is at ≥ 30° (the planned crests'
   35° flank cones reach into it; GF-C0 / GF-UF 1–3 %); up to 18 D1 crest vertices > 30 m above both flanks (max 42 m)
   on calibration; the Sector crest wall in GF-C0 / GF-UF; GF-T: M1 ≈ 296 m on every variant (the D6 band
   330–430 m is not reachable in this sector at the walkability bound).
5. *Open:* the owner's blind review (G-5a, G-5b); GEO-10 and GEO-11 (G-6); island scale; whether F2's reserve or a
   plan-builder rule should place reserves; GEO-3's production placement and GEO-8's contract (G4).

## 8. Evidence: determinism, ownership, seams, drainage, geometry

- **Determinism:** each run twice in fresh processes, compared file by file with timing excepted — run 1 92/92,
  run 2 92/92, held-out 209/209 identical; analytic checks run twice identically. **One machine only** (Windows 11,
  Python 3.13, Unity 6000.5.7f1): repeatability is shown, cross-machine determinism is **not** proven.
- **Ownership:** GF-O — 135 560 owned vertices written exactly once, 0 bad writers, 0 strays on every GF-RF lattice;
  the plan validator GF-V1 … V8 before every synthesis.
- **Seams:** no Unity tile seam is involved (research lattices are never baked). Unit seams: GF-B — 0 flows into
  another unit, onto a crest or out of the domain on every lattice; GF-A — perturbing unit A changes 0 vertices
  outside it and the result is order-invariant (all valid held-out variants); the sector–context boundary is a planned
  crest record.
- **Drainage:** S8 closed depressions 0 in the sector on every valid GF-RF variant; the production `HydrologyPlanner`
  succeeds with every mouth at a designated outlet (R5) except 5003; axis climbs 0 except 5003.
- **Geometry:** lateral bound exact (AC5); RS 99.65–99.86 % of non-barrier land reachable; walls and steep lowland
  as in §7.4.

## 9. Compute cost and the limits of the measurements

Python per variant and copy: 120–170 s (plan 10–11 s, context 5 s, synthesis 7 / 13 / 15–21 s for C0 / UF / RF,
readings 15–18 s each, assembly 26–40 s); a four-variant copy in parallel ≈ 2.5–3 min, a ten-variant held-out copy
7.5–8.5 min. Unity: hydro ≈ 0.3 s per lattice plus process start; views ≈ 3–9 min of rendering per copy, sheet
composition 10–18 min. All on one workstation, timings informational. Limits: one fixture on one north-west sector
of contour A (1.010 km² of land) in a 513 × 513 window; ten held-out variants (one rate estimate, wide uncertainty:
8 of 10 is compatible with a true rate well below 70 %); the agent did not rate any sheet.

## 10. Changed files

| File | Purpose |
|---|---|
| `tools/g3f/` (Python package, runner and tests; fixture, README, .gitignore) | the research package; `synth.py` F1, `fixture/sector_nw.json` F2 |
| `unity/Assets/Scripts/Editor/TerrainGenG3fDump.cs` (+ .meta) | Editor-only Unity steps: contour export, H4A check, hydro (with the report-only uphill diagnosis), views; protected-manifest checks |
| `unity/Assets/Plans/GEOGRAPHY_G3F_TECHNICAL_SPEC.md` (+ .meta) | specification v0.3 with every result |
| `unity/Assets/Plans/GEOGRAPHY_G3F_PHASE_PLAN.md`, `14 World Generation Roadmap.md`, `99 Open Questions.md`, `AGENTS.md` | status records |
| this report (+ .meta) | the final report |

Outside the repository (`research-data/g3f/`): WP0, limits, contour export, calibration runs 1–2, the freeze record,
the held-out evaluation, tools (`calib/compare_runs.py` — timing excepted, views excluded; `evaluate.py`;
`heldout/evaluate_heldout.py`), diagnosis scripts, manifests. No production file, scene, asset or `TerrainData`
changed; the Glossary is unchanged (the G3f terms stay proposed until G-3).

## 11. Proven, hypothesis, untested

- **Proven (on this machine, this fixture):** a single-owner Geography Plan with planned divides and per-catchment
  synthesis can meet every pre-registered hard and process gate on all four calibration variants and on 8 of 10
  held-out variants, with exact single writes, no cross-divide flow, local and order-invariant assembly and
  repeatable output; emergent divides under the same tree law invert the planned divide profile.
- **Hypothesis:** that GF-RF's form meets D6 (the owner's blind review); that it scales to the island; that the
  architecture (GEO-10 (b)-like assembly) is the right production direction (G-6); that the 2 named fixes generalise
  (F2 already shows a weakness).
- **Untested:** cross-machine determinism; island-scale runs; production bake, tiles, vegetation and surface rules on
  a G3f surface; walkability in Unity (the 3F contract); performance at island scale; U-P.

## 12. Recommendation for G4

Before any production decision: (1) the owner's blind review of the sealed calibration and held-out sheets (G-5a,
G-5b) — the pass rule's part (2); (2) repair the plan builder's two defects found here — an "axis stays on land"
validator check and reserve placement as a plan rule rather than a fitted fixture point — and re-evaluate on the
reserve seeds 5011–5020 only under GF-S7's terms; (3) an island-scale run on contour A with several sectors and their
seams; (4) a cross-machine determinism check; (5) a production-path trial off the bake path — G3f lattice → Unity
`TerrainData` tiles, surface and vegetation rules — measuring seams, walkability (3F) and cost; (6) G-6 on GEO-10 and
GEO-11 before G4 is opened.

---

- **Итоговый статус:** недостаточно доказательств.
- **Почему:** GF-RF прошёл все жёсткие и процессные гейты на 4 из 4 калибровочных и на 8 из 10 отложенных вариантов
  (часть (1) правила выполнена); часть (2) — слепой просмотр владельцем — не проводилась, поэтому прохождение не
  объявлено; два отказа (5003, 5006) вызваны дефектом построителя плана и побочным эффектом исправления F2;
  детерминизм проверен только на одной машине; проверен один сектор, не остров.
- **Следующий шаг:** слепой просмотр владельцем запечатанных листов калибровки и отложенной оценки (G-5a/G-5b).
- **Production readiness:** не готов.
