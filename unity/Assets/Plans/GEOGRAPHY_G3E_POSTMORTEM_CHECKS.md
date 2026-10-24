# G3e-PM — Post-Mortem Coverage Checks and the S-LM1 Look: Pre-Registration

Version: 0.1

Status: Draft — **pre-registration awaiting the owner's authorisation.** The owner approved the plan on 2026-10-24
(checks D1–D5 and the decision table of §8). **Nothing is authorised to run**: no script, no Unity process and no
output exist for it until the separate authorisation of §11. Working name **G3e-PM**; an official ID, if one is wanted,
is the owner's. It changes no definition, threshold, flag level, passage rule, construction code or measurement code.
Once authorised, §§0–12 are frozen (§9); results are appended in §13 and never written into §§0–12.

Last Updated: 2026-10-24 (v0.1: written after the owner approved the plan; not run)

Depends On:
[GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md](GEOGRAPHY_LMC_STEP1_NOT_READY_REPORT.md) (§§3, 4, 7),
[GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md](GEOGRAPHY_LMC_STEP1_RUN5_REVIEW.md) (§§2, 4, 5, 7.4, 10.2, 11.3, 13.3, 15.2, 16–23),
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md) (§§1, 2, 4.2, 6, 7, 8, 9, 14),
[GEOGRAPHY_LMC_DESIGN_NOTE.md](GEOGRAPHY_LMC_DESIGN_NOTE.md) (§§3, 7.5, 8, 9, 11),
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) (C6, C7),
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) (§§1.3, 5.7),
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (D6),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §16,
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-3, GEO-7, GEO-8

Referenced By:
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §16;
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-3;
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md); [AGENTS.md](../../../AGENTS.md)

---

## 0. How to read this

- **Tags.** **[MEASURED]** recorded in a document or in run 11's preserved readings, source named; **[FACT]** read from
  code or a file; **[INTERPRETED]** a reading of recorded evidence; **[HYPOTHESIS]** not established; **[RULE]** fixed
  here before any computation; **[AUTHORING]** a classification threshold chosen here, never tuned afterwards.
- **Paths.** `RD` = `D:\Hobby\game-dev\research-data`; `R11` = `RD\g3e-step1\run11\output`;
  `REF` = `RD\lmc-reference`; `PM` = `RD\g3e-pm` (new; created only after authorisation).
- **Lattice conventions** as spec §1: 1 025 × 1 025 vertices, cell c = 2.734375 m, index `z · 1025 + x`, world
  position `(x c, z c)`, sea level 24 m. Region codes as spec §4.2 (0 sea, 1 lowland, 2 upland, 3 mountain, 4 plateau
  top, 5 escarpment band, 6 ramp, 7 corridor); `layout_instance.u8` holds the plateau id for codes 4–6.
- **What this is.** One pass of read-only checks on the preserved G3e Step 1 data plus one views-only Unity step. It
  builds no terrain and fits nothing. G3e Step 1 stays closed: this is not a Step 1 patch, not Step 2, not a freeze.

---

## 1. The decision it serves

One owner decision: **continue S-LM1 with an exact and complete amendment package**, **settle named contract
questions first**, or **stop the S-LM1 line**.

Why it comes before any construction change [INTERPRETED, from the records cited]:

- The not-ready report §7 recommends (a) an LM2 layout amendment and (b) S-LM1 valley placement as "the smallest change
  that addresses the remaining failures at their source". By the project's own attributions they leave failures
  uncovered: 110201's R5 — four of four elevated systems end at undesignated mouths [MEASURED, run 11 readings], a
  contract question on run 5 (review §§5, 7.4); climbs led by frame blending and by u (review §20.2); a climb grown at
  the pass corridors (§20.4); two mountain-stage pits led by the valley weight (§23.3). And (a) does not see S-LM1's
  valleys (review §17.5 (a)).
- **No image of S-LM1 exists**: the views step was never run, and the Step 1 archive holds no image [FACT]. Half of the
  passage rule is the owner's visual acceptance (design note §7.5). Run 11's report-only diagnostics carry G3c-lineage
  signatures [MEASURED, readings]: Q7 flagged on both seeds (1337: cliff share 54.5 %, one cliff run 53.9 % of the
  shoreline; 110201: longest cliff run 17.1 %); D-cap peaks at configured angles (110201: ratio 8.62 at 10°, 4.14 at 2°
  with K5); Q2 θ = 0.011 and 0.061 against G3c's median 0.49.
- Design note §11: when the result is inconclusive, propose the smallest additional experiment that resolves it. The
  post-G3c review §5.7 (a proposal the note supersedes where they differ) adds: if neither synthesis passes, revisit
  the families before anything else is built.

---

## 2. Hypothesis

**H [RULE].** Every item of the run-11 failure ledger (§4) has at least one owner inside the bounded set **B** (§5.1),
so that one amendment package inside B could address all of them without changing an approved definition, threshold,
flag level, the passage rule or a contract of the specification.

**Refinement of the approved outline, made before any computation.** The outline asked for *exactly one* owner per
item. An item that two bounded owners could each remove is still covered; what H has to show is that no item's only
owners lie outside B. Each item still receives one **primary** owner (§5.3), so the ledger reads one item, one line.

**Expectation, not a criterion [HYPOTHESIS]:** H fails at least on I-2 … I-5 (110201's R5) and on I-26 and I-30
(1337's climbs led by u and by blending).

**What H does not test:** whether such a package would pass the gates (only a construction run can show that), any
held-out seed, the island's size, or GEO-3's option (c).

---

## 3. Fixed inputs and baseline

| Input | Path | Identity |
|---|---|---|
| run 11 outputs — lattices, readings, layouts, stage dumps 01–09, mountain-stage terms, axes and frames | `R11` | `RD\g3e-step1\SHA256SUMS.run11.txt` (105 entries; the file's SHA-256 `24e73fd4d2f73431d3da2dfcad935452e7213591a086ceed3698e50bab4655c8`), verified before and after every check |
| S-LM1 lattices | `R11\seed_110201\lm1_110201.f32`, `R11\seed_1337\lm1_1337.f32` | MD5 `14cc59d4c2ad5d918ca19bf911a88a77`, `5700a9c9bb86170ed6ed9a6461eae24f` (`R11\hashes.txt`) |
| S-LM3 lattices (closed, views only) | `R11\seed_*\lm3_*.f32` | MD5 `dc33f1eab2b0dcb5975a3733511ab335`, `9f2629acd08f362c23b5b3cecff8789b` |
| layouts | `R11\seed_*\layout_*.u8`, `R11\seed_*\diag\layout_polylines.json` | hashes `868a48908dad70e9`, `ffb87158a06814bc` |
| reference helpers, G3c calibration grids | `REF\lmc_spec\lmcspec.py`, `REF\g3c\screen\g3c_A_s30-med_*.f32` | `REF\MANIFEST.md5` (48 entries; the file's SHA-256 `39ef0e869215bb08ba47410b08675e8e14c9c6993fc88bd1bde4134969790ad6`) |
| earlier replicas, copied byte for byte into `PM\scripts\lib\` | `RD\g3e-step1\diag-run6\checks\diag6_analysis.py` (`depressions`, `flood`), `RD\g3e-step1\prereg-run10\mtn_baseline.py` (`climb`), `RD\g3e-step1\prereg-run10\mtn_analysis.py` (A1–A3 split), `RD\g3e-step1\checks-toe-axis\toe_axis_options.py` (R-c reading), `RD\g3e-step1\checks-ramp-c4\ramp_c4_baseline.py` (`termini`) | SHA-256 recorded at copy time; the copies are never edited. These files run their own analyses at module level, so the G3e-PM scripts quote the needed functions verbatim, citing file and line range; only input paths differ |
| code | `unity/Assets/Scripts/Editor/TerrainGenLmc*.cs`, commit `edca8e7` | SHA-256 of all six equal to `RD\g3e-step1\run11\code-snapshot-run11` (checked 2026-10-24) |
| protected manifest | `RD\g3e-step1\scratchpad-g3e\protected_md5_before.txt` | 73 entries; the file's SHA-256 `41580895184ab261808a457601e258cdb957283f8d998d193e944d15f27aedb5` |
| interpreter | Python 3.13.14, numpy 2.5.3 (user site-packages); no scipy | recorded in every output header |

**Seeds:** 110201 and 1337 only. The held-out seeds 1005–1014 and the reserves 1015–1024 are never generated, read or
viewed.

---

## 4. The failure ledger — closed before any computation

Every hard-gate failure of S-LM1 in run 11, from the readings `R11\seed_*\lm1_*_readings.json` [MEASURED]. An item
found later is reported but does not enter H.

| Id | Seed | Gate | Item | Recorded value |
|---|---|---|---|---|
| I-1 | 110201 | R4, R8, R9 | plateau 1's top not reachable: the site (946, 2302) lies in T-edge component 669; R8 lists the site and "plateau 1 top"; four R9 pairs are disconnected | — |
| I-2 | 110201 | R5 | elevated system ending at the undesignated mouth (1233, 1384) | designated outlets (2023, 517), (2417, 2256) |
| I-3 | 110201 | R5 | elevated system ending at (1159, 1422) | as above |
| I-4 | 110201 | R5 | elevated system ending at (1930, 2338) | as above |
| I-5 | 110201 | R5 | elevated system ending at (1334, 2382) | as above |
| I-6 | 110201 | R7 (a) | S8 at (1482, 1548), region 3 | 0.67 m, 1 548 m² |
| I-7 | 110201 | R7 (a) | S8 at (1583, 2081), region 3 | 1.04 m, 164 m² |
| I-8 | 110201 | R7 (a) | S8 at (1096, 2264), region 7 | 1.08 m, 329 m² |
| I-9 | 110201 | R7 (a) | S8 at (837, 2294), region 5 | 1.12 m, 239 m² |
| I-10 | 110201 | R7 (c) | ridge 0 side 0 valley 3 | 0.918 m |
| I-11 | 110201 | R7 (c) | ridge 0 side 0 valley 15 | 0.757 m |
| I-12 | 110201 | R7 (c) | ridge 0 side 0 valley 17 | 2.137 m |
| I-13 | 110201 | R7 (c) | ridge 0 side 1 valley 0 | 1.304 m |
| I-14 | 110201 | R7 (c) | ridge 0 side 1 valley 2 | 0.580 m |
| I-15 | 110201 | R7 (c) | ridge 0 side 1 valley 6 | 2.854 m |
| I-16 | 110201 | R7 (c) | ridge 0 side 1 valley 12 | 2.961 m |
| I-17 | 110201 | R7 (c) | ridge 0 side 1 valley 18 | 5.833 m |
| I-18 | 110201 | R7 (c) | ridge 1 side 0 valley 1 | 4.459 m |
| I-19 | 110201 | R7 (c) | ridge 1 side 1 valley 0 | 12.885 m |
| I-20 | 110201 | R7 (c) | ridge 2 side 0 valley 2 | 2.493 m |
| I-21 | 110201 | R7 (c) | ridge 3 side 0 valley 3 | 5.513 m |
| I-22 | 1337 | R7 (a) | S8 at (1441, 1463), region 5 | 2.67 m, 2 176 m² |
| I-23 | 1337 | R7 (a) | S8 at (1488, 1605), region 3 | 1.30 m, 2 572 m² |
| I-24 | 1337 | R7 (a) | S8 at (1416, 1632), region 3 | 5.47 m, 1 144 m² |
| I-25 | 1337 | R7 (a) | S8 at (1381, 1747), region 3 | 0.95 m, 209 m² |
| I-26 | 1337 | R7 (c) | ridge 0 side 1 valley 0 | 2.424 m |
| I-27 | 1337 | R7 (c) | ridge 0 side 1 valley 14 | 1.361 m |
| I-28 | 1337 | R7 (c) | ridge 0 side 1 valley 16 | 1.162 m |
| I-29 | 1337 | R7 (c) | ridge 1 side 1 valley 1 | 3.743 m |
| I-30 | 1337 | R7 (c) | ridge 2 side 0 valley 0 | 1.302 m |

**Not in the ledger** — no hard gate fails on them; they are reported beside the look (§6.1): the C4 residuals (0.65 ha
on 110201, 0.83 ha on 1337), R2 on 1337 (an indirect indicator; plateau 0 not verified), the Q7 flags, routes at
44.4–44.9996° on 1337.

**R2 against R8 on plateau 1 — settled by reading the code, no computation [FACT].** R2's `reachable_top` is true when
any vertex of the painted top is vertex-reachable (`TerrainGenLmcMeasure.cs`, R2, the count at line 2303); R8 tests the
single top vertex nearest the top's centroid (`Plateau`, line 890; R8 at line 1713). Both readings stand. Which part of
the top is reachable, and why the centroid is not, is D2's question.

---

## 5. Owners

### 5.1 The bounded set B — what an amendment package may touch

| Id | Owner | What an amendment there changes | Source |
|---|---|---|---|
| **B1** | LM2 plateau siting | where a plateau centre may stand; which ground the layout reserves for the plateau's write footprint; a land-and-shore test on the ramp bearing | review §17.5 (a); report §7 (a) |
| **B2** | S-LM1 plateau stage | where the plateau stage may write (the reserved footprint and declared blend bands, spec §6); its rim profiles, the joint and gradual blend width of Q9 #12 included | review §§4, 17.3 item 4, 17.5 |
| **B3** | S-LM1 valley stations | where S-LM1 places its valley lines along the mountain boundary, relative to the base | review §21.4; report §7 (b) |
| **B4** | S-LM1 axis declaration | R-c — one vertex per distance level, the one nearest the axis's median owner arc; the R7 (c) calculation is unchanged | review §§21.2, 21.5 |

### 5.2 Owners outside B — reaching them takes a contract decision or a broader change

| Id | Owner |
|---|---|
| O1 | S-LM1 base contract: B = min(drainage surface, coastal surface), and how foot drainage is routed (spec §6; review §7.4) |
| O2 | LM2 mountain–coast adjacency and outlet designation |
| O3 | S-LM1 frame blending |
| O4 | S-LM1 flank coordinate u and crest profile |
| O5 | S-LM1 valley cross-profile (valley weight w, depth) |
| O6 | pass corridors (stage 07) |
| O7 | the running minimum and its banks (stage 09; Q9 #11, #16) |
| O8 | not attributable from the preserved data |

### 5.3 Mapping rules [RULE]

1. **Stage of origin.** Stages: 04 base B, 05 toe, 06 after the mountain, 07 after the pass corridors, 08 after the
   plateaus, 09 after the running minimum (= the final lattice, checked bit for bit; a difference is reported and the
   final lattice used).
   - *S8 item:* the first of stages 04, 06, 07, 08, 09 on which `depressions()` returns a depression whose cell set
     contains the vertex nearest the item's recorded position.
   - *Climb item:* the first of stages 04, 05, 06, 07, 08, 09 on which `climb()` of that axis exceeds 0.25 m.
2. **Stage → owner.** 04 → O1 (the base is free of closed depressions by construction, so an item made there is a
   contract matter); 05 or 06 → rule 4; 07 → O6; 08 → B2 primary, plus B1 when the item lies outside the plateau's
   painted codes 4–6 (ground B1 can reserve); 09 → O7.
3. **Ties first (B4).** Before rule 4, a climb item whose final-lattice climb under R-c is ≤ 0.25 m is owned by B4.
4. **Term split for stage 05/06 items.** The midpoint split of `mtn_analysis.py` — A2 for climbs over the climbing
   stretch, A3 from floor to spill for depressions — applied unchanged to run 11's recorded terms with the harmonic toe
   (`lm1_stage_05_toe`); a term carrying ≥ 75 % of the positive contributions is dominant [CARRIED, review §19].
   Term → owner: **toe → B3** (after T1 the toe is the base along the mountain boundary; a toe-led climb is the base
   rising toward the valley mouth, review §§21.4, 23.3); blend → O3; u and crest → O4; w → O5; residual → O8.
   A mixed item is **covered** when its terms that map into B carry ≥ 75 % of its positive contributions; its primary
   owner is its largest term's owner.
5. **I-1** follows §6.3 (D2); **I-2 … I-5** follow §6.5 (D4).
6. **Covered** means: owner in B (rules 2–5). **Uncovered** means: every owner found lies in O1–O7. **Unattributed**
   means: O8, or a validity check for that item failed.

---

## 6. The checks

**Order [RULE]: D5 → the owner's look → D1–D4.** The look is recorded before the owner sees any output of D1–D4.
If the look says *stop*, D1–D4 still run for the record of causes (design note §11) unless the owner waives them then.

### 6.1 D5 — views, and the owner's look

**Command** — run twice, each in a fresh process, with no editor open on the project (`<…>` are the absolute paths of
§0; `{n}` = 1 and 2):

```
"C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe" -batchmode -quit
  -projectPath D:/Hobby/game-dev/game-dev/unity
  -executeMethod GameDev.EditorTools.TerrainGenLmcDump.DumpBatch
  -lmcStep views -lmcSeeds 110201,1337
  -lmcIn <R11> -lmcRef <REF>
  -lmcProtected <RD>/g3e-step1/scratchpad-g3e/protected_md5_before.txt
  -lmcOut <PM>/views_{n} -logFile <PM>/views_{n}/unity.log
```

**What it does [FACT, `TerrainGenLmcDump.ViewsStep`]:** it rebuilds both layouts in memory and compares their hashes
with `R11\hashes.txt` (V-L8 across processes); rebuilds production `H4A` 110201 in memory as a reference panel; copies
each seed's two lattices as X and Y under a seeded blind key written to `sealed/`; renders the layout's camera set on X,
Y, `H4A` and G3c contour A s30-med of the same seed; runs G1 V1–V6 on X, Y and `H4A`; composes sheets with an index. It
synthesises nothing: the only lattices it writes are the copies X and Y in its output folder. It refuses any output
inside `Assets` and any seed outside the calibration list, and checks the protected manifest before and after. **It does not compare the pinned contour-A
and `H4A` hashes** (only the calibration step does); G3e-PM depends on neither, and the manifest covers the production
files.

**Validity [RULE]:** exit code 0 both times; protected manifest 73 / 73 before and after both times; "equal to the
calibration run's: True" for both seeds both times; the two processes' image hashes compared and reported — a
difference is recorded, not a stop.

**Stop on:** a non-zero exit, a protected-file change, a layout-hash mismatch, any exception. A fix to the views code
is a code change and needs its own authorisation.

**Blinding is nominal.** One of X and Y is the closed S-LM3 configuration, whose rough last iterate is easy to
recognise. The key stays sealed until the look is recorded.

**The look.** The owner reviews the sheets (`sheets\index.txt`) at the island, regional and player scales. Beside them
the agent places a one-page table of run 11's report-only readings — Q1–Q7, D-cap, D-reg, R1, R2, R6, R11 and the items
listed under "Not in the ledger" (§4) — copied from the readings, with no new computation. The record follows spec §9's
review form: per seed and per D6 point 1–8, *meets / partly / fails* with a reason; then one overall verdict,
**continue** or **stop** the S-LM1 line, with a reason. The verdict is the owner's; no metric decides it. Calibration
seeds only; not an acceptance, not Step 2; nothing in G3e-PM is tuned on it.

### 6.2 D1 — the ledger and its coverage

**Validity first [RULE]; a failure stops D1 and makes H inconclusive (§7):**

- **V1** — `depressions()` on each final lattice returns exactly the S8 entries of §4 (4 and 4): the deepest vertex
  within 3 m of the recorded position, depth within 0.01 m, area within one cell (7.48 m²).
- **V2** — `climb()` over every axis of `axes_measured` reproduces every climb in `r7_axes` within 0.01 m — all 25 axes
  on 110201 and 22 on 1337, not only the failing ones.
- **V3** — vertex reachability by `lmcspec.TEdge` reproduces R8: on 110201 the site (946, 2302) and plateau 1's
  centroid top vertex unreachable and nothing else among R8's points; on 1337 every R8 point reachable; both pass cols
  — (973, 1794) and (1654, 1725) — reachable.
- **V4** — the R-c reading of `toe_axis_options.py` (lines 128–143) on the final lattices reproduces review §23.3's
  counts of valley axes over 0.25 m: today 12 and 5, lowest per level 7 and 2, one per level 7 and 3.
- **V5** — `mtn_analysis.py`'s check A1 on run 11's recorded terms with the harmonic toe: the mountain stage is
  reproduced within 0.01 m at every mountain vertex with an owner frame. A failure makes only the stage 05/06 items
  unattributed (O8); D1 continues for the rest.

**Then, per item:** the stage of origin (§5.3 rule 1); for stage 05/06 items, R-c first, then the term split; the
primary owner, the covered, uncovered or unattributed status, and the amendment the owner implies, in one line. For
I-1 and I-2 … I-5 the line cites D2 and D4.

**Output:** `PM\checks\d1_ledger.txt` and `d1_ledger.json` — 30 lines, then the coverage summary: items covered,
uncovered (with their owners), unattributed.

### 6.3 D2 — access to plateau 1's top on 110201

On the final lattice of 110201, with `lmcspec.TEdge` and the layout maps:

1. **Top mask T** = region 4 with instance 1. Report |T|; the share of T that is vertex-reachable (spec §1) and where it
   lies — the share within 20 m of T's boundary against the share deeper inside; the centroid vertex c* chosen as in
   `Plateau` (line 888), and the T-edge component K* of a walkable triangle touching c* (or "no walkable triangle at c*").
2. **The cheapest crossing.** A 0–1 breadth-first search over land triangles: walkable cost 0, non-walkable land cost
   1, non-land impassable; sources = K*'s triangles; targets = walkable triangles in shore-reachable components.
   Report n* — the fewest non-walkable triangles crossed — and, along the cheapest path, the crossed triangles' slopes
   (maximum, median) and the region code at each one's south-west vertex. A tie between paths of equal cost is broken
   by the lower target triangle index, then the lower source index.
3. **The ramp strip.** The walkable triangles within 20 m of the layout's ramp centreline (from the plateau centre along
   `ramp_bearing`): whether they lie in K*, and whether their component is shore-reachable.

**Classification [RULE]:**

- **B1 always owns I-1**: the ramp bearing fails the land-and-shore test [MEASURED, review §17.2: about 64 m of ramp,
  then a ~150 m sea cliff; the layout's own ramp length is 330 m]. So I-1 is **covered** whatever D2 shows; D2 decides
  what the package must contain.
- **B2 is added** when the cheapest crossing lies on the joint or gradual side (the majority of crossed triangles in
  codes 2, 3 or 7) with n* ≤ 3 and every crossed triangle under 55° [AUTHORING] — a narrow step just over the walking
  limit, as the narrowed blend of Q9 #12 would leave (review §4).
- **Internal split** — B2 as primary — when the majority of crossed triangles lie in code 4 and K* holds less than
  50 % of T.
- **Walled** — B1 alone — when the cheapest crossing's majority lies in code 5, or n* > 3.

**Output:** `PM\checks\d2_access.txt`.

### 6.4 D3 — can the LM2 amendment be met on contour A?

A necessary-condition estimate on the preserved layout maps of both seeds, before anyone designs B1's rule.

- **Ground.** The run-11 plateau codes 4–6 are reset to upland (2) — they were painted on upland or corridor ground,
  and the original code is not stored, so the reset favours feasibility [INTERPRETED]. **Permitted ground** = codes 2
  and 3 (the joint side meets the mountain by design); forbidden = 0, 1 and 7.
- **Distances.** An exact Euclidean distance transform in numpy (the separable lower-envelope method of Felzenszwalb
  and Huttenlocher), checked against brute force on a 64 × 64 random mask before use; distances between vertex
  positions in metres. dHigh = distance to code 3; dLow = to code 1; Sd = to code 0.
- **Candidate centres.** Every upland vertex (code 2 after the reset) with the layout's own windows
  (`TerrainGenLmcLayout.BuildPlateaus` [FACT]): r0 + 10 ≤ dHigh ≤ r0 + 150, dLow ≥ r0 + 30, Sd ≥ r0 + 40.
- **Cases.** Top area 30 000 and 150 000 m² (r0 = 97.72 and 218.51 m) × edge 18, 40, 60, 120 m; ramp length
  L = 1.15 × edge / tan 18° (64, 142, 212, 425 m).
- **Footprints.**
  - *F-min (lenient):* the disc of radius r0 on permitted ground, and the ramp strip — vertices within 20 m of the ray
    along the ramp bearing, from r0 to r0 + L beyond the centre — all on permitted ground, all land, with the strip's
    far end ≥ 40 m from the sea.
  - *F-full:* the disc of radius 1.20 r0 (the outline's largest wobble, 1 + 0.12 + 0.08) widened by
    W = max(blend, escarpment reach), with blend = max(60 m, 1.5 × edge) (Q9 #12) and escarpment reach =
    edge / tan 70° + 0.35 × edge / tan 33° (PD-7), all on permitted ground, plus the ramp strip as in F-min.
- **Ramp bearing.** Bearings are measured as the layout measures them (`BearingToMask`: from +x toward +z,
  `atan2(dz, dx)`, the nearest masked vertex searched on every second vertex [FACT]). (i) The layout's rule: toward the
  nearest lowland vertex within 1 500 m — or, when that lies within 60° of the bearing to the nearest mountain vertex
  within 600 m, that mountain bearing + 150°. (ii) Free: any bearing in 5° steps at least 60° from the mountain
  bearing. A centre counts as feasible when at least one bearing passes; (i) and (ii) are reported separately.
- **Result per seed and case:** the number of feasible centres, and whether two of them lie ≥ 500 m apart (the exact
  diameter of the feasible set, from its convex hull).

**Classification [RULE]:** a seed is **infeasible** when even F-min with free bearings, area 30 000 m² and edge 18 m has
no pair ≥ 500 m apart — then B1 cannot be applied as review §17.5 (a) states it, and H fails. **Feasible only
leniently** when F-min passes and F-full fails in every case — reported as a risk to the package. Whether the 60–120 m
class fits at all (the edge 60 and 120 cases) is reported as GEO-8 evidence, not a criterion.

**Output:** `PM\checks\d3_feasibility.txt`.

### 6.5 D4 — where 110201's elevated systems turn away

The production planner's network is not reproducible in Python; D8 on the lattice stands in for it, validated per
mouth.

1. **Routing.** `termini()` of `ramp_c4_baseline.py` (`lmcspec.flood_d8`: priority flood, epsilon 1 mm, 8-connected,
   D8 on the filled surface) on the final lattice and on stages 04, 06, 07 and 08.
2. **Catchment of a mouth.** C_k = the land vertices whose final-lattice terminus lies within 150 m of the recorded mouth
   m_k. **Validity:** C_k must contain a vertex of the mountain region (3) or of a plateau top (4); otherwise the item is
   unattributed (O8 — the D8 routing and the planner disagree).
3. **Exit point.** e_k = the vertex of C_k in codes 3 or 4 with the largest final-lattice D8 area.
4. **Stage of misrouting.** For each of stages 04, 06, 07, 08 and the final lattice: is e_k's terminus within 150 m of a
   designated outlet? The stage of misrouting is the earliest stage from which every later stage, the final lattice
   included, is undesignated.
5. **Owner [RULE].**
   - 04 → **O2** when the stage-04 D8 path from e_k leaves the mountain region at a vertex within 100 m of the sea;
     otherwise **O1**. The share of that path outside the mountain region where the coastal surface (stage 02) lies
     below the drainage surface (stage 01) is reported, for information only.
   - 06 → **B3** when the final-lattice path leaves the mountain region within 20 m of a vertex of a declared S-LM1
     valley axis (`axes_measured`, names beginning "ridge"); otherwise O4.
   - 07 → O6. 08 → B2 primary, B1 as well. Final only → O7.

**Output:** `PM\checks\d4_r5.txt` — per mouth: validity, e_k, the termini per stage, the stage of misrouting, the owner.

---

## 7. Outcome of H [RULE]

- **PASS:** every one of the 30 items is covered, and D3 finds no infeasible seed.
- **FAIL:** at least one item is uncovered (its owners lie in O1–O7), or D3 finds a seed infeasible. FAIL takes
  precedence over INCONCLUSIVE.
- **INCONCLUSIVE:** no item is uncovered, but at least one is unattributed, or a validity check of D1 (V1–V4) failed, or
  D5 could not run without a code change.

A FAIL is a finding, not a defect: it names the contract questions an amendment stage would need first.

---

## 8. Decision table — pre-registered, approved with the plan

| The owner's look (§6.1) | H | What follows — each step needs its own authorisation |
|---|---|---|
| **stop** | any | **The S-LM1 line stops.** Next, if the owner plans one: revisit the landform families (post-G3c review §4, §5.7) or a changed S-LM3 law, each with its own plan; G3d stays deferred |
| **continue** | **PASS** | A pre-registration for a new stage carrying exactly the package D1 names — every item's primary owner with an exact rule, written before any code — on the same calibration seeds: at most two construction runs; a not-ready exit declared in advance; the non-regression list below; the comparability protocol below |
| **continue** | **FAIL** | **The owner's contract decisions first**, on the outside owners D1 names (typically: foot drainage in S-LM1's base; plateau ownership under spec §6; the running minimum on valley floors; frame blending). No code until then; if the owner keeps the contracts as they are, the S-LM1 line stops |
| any | **INCONCLUSIVE** | At most one pre-registered recording run for the named item, or the owner treats it as FAIL |

**Non-regression list for any follow-up stage [MEASURED, review §§14–23, run 11 readings]:** LM2 valid by V-L1 … V-L8;
on 1337, R4, R5, R8, R9, R10 passing and both elevated systems at designated outlets; on 110201, R10 passing, the pass
col reachable, the main summit at its target (343.84 m) and the pass offset at its target (69.76 m); no protected vertex
raised by a plateau (K1 = 0); a continuous toe (99th-percentile step 0.58 and 0.25 m); ramp slope ≤ 20°; R1 met; Q1,
Q5 and Q6 unflagged.

**Comparability protocol for a later LM2 amendment.** Run 11 is the frozen baseline. The amendment touches plateau
placement and the ramp bearing only; the divide tree, basins, corridors, outlets and shore intents must stay bit-equal,
checked on the layout polylines and on every region code outside codes 4–6. All other code stays at `edca8e7`. Each
ledger item is reported before and after. The gate history of runs 1–11 does not carry over: it is a new calibration
series.

---

## 9. Reproducibility and integrity [RULE]

- **This document is frozen at authorisation.** Its SHA-256 is computed and quoted in the authorisation record before
  any computation; a copy goes to `PM\prereg\` with `SHA256SUMS.prereg.txt`.
- **Scripts** are written after the authorisation and before any data is read, implement §§5–6 exactly, and are hashed
  before their first execution (`PM\scripts\SHA256SUMS.scripts.txt`). A later change is logged as a deviation with its
  reason in §13 — never silently.
- **Inputs** are verified before and after every check: `SHA256SUMS.run11.txt` (105 / 105), `REF\MANIFEST.md5`
  (48 / 48), the six code files against the run 11 snapshot.
- **Determinism:** every script runs twice and its outputs must be byte-identical; D5 runs twice in fresh processes.
- **Outputs** only under `PM`, with `SHA256SUMS.g3e-pm.txt`. No file is written into the repository during the checks;
  `git status` is checked before and after and must be unchanged. The protected manifest is checked by D5 (73 / 73).
- **Afterwards:** the results go into §13 of this document, and the shared documents are updated in the same
  documentation step.

---

## 10. Stopping rule [RULE]

- One pass of D5, the look and D1–D4. No second look, no rerun with a changed definition.
- No construction or measurement code is changed; no definition, threshold, flag level or passage rule is touched.
- Stop and report on an input-hash mismatch, a protected-file change, a D5 failure, or a V1–V4 failure.
- G3e-PM ends with §13.4, the decision memo; nothing that follows starts by itself.

---

## 11. Authorisation requested

The owner approved the plan on 2026-10-24. Running it needs, explicitly:

| # | What | Scope |
|---|---|---|
| **A0** *(optional, recommended)* | commit this document and the shared-document entries to `main` and push (Git History Duty) | fixes the revision before computation; without A0 the SHA-256 of §9 fixes it |
| **A1** | the D5 views step | exactly the command of §6.1, twice in fresh processes; outputs `PM\views_1`, `PM\views_2` |
| **A2** | the owner's look | recorded per §6.1 before any output of D1–D4 is shown |
| **A3** | the read-only checks D1–D4 | Python on `R11` and `REF` only; scripts `PM\scripts`, outputs `PM\checks` |

**Not authorised by these:** any construction or measurement code change; any calibration or synthesis run; the
held-out or reserve seeds; Step 2; a freeze; a production change; any change to the 73 protected files, the reference
data or their checksums; a commit or push beyond A0.

---

## 12. Documents, Glossary and cost

- **Definitions change nowhere.** The technical specification, the design note and the readiness check are not
  amended by G3e-PM. On completion, §13 here, research §16, 99 Open Questions GEO-3, the roadmap and `AGENTS.md` record
  the outcome.
- **If the outcome is "continue" with a PASS**, the follow-up stage needs, each with the owner's approval: amendments to
  spec §4.1 and §4.3 (V-L6 — plateau siting and the ramp test) and to §6 (where the plateau stage writes; the valley
  station rule), a note under R7 (c) that S-LM1 declares an axis as a line (B4; the calculation unchanged), and a
  readiness check of its own.
- **Glossary:** no new project term. "Write footprint" stays research vocabulary unless a specification amendment
  adopts it; that amendment then checks the Glossary.
- **Cost [HYPOTHESIS]:** D5 — two Unity processes of a few minutes each (run 11's fixtures took 22 s; rendering time
  is unmeasured); D1–D4 — pure-Python priority floods on about 10⁶ vertices, at most a few minutes each; under an hour
  in all.

---

## 13. Results

*Empty until the authorised run. Planned subsections: 13.1 the look (the owner's record); 13.2 D1 ledger and coverage,
with D2, D3 and D4; 13.3 H — PASS, FAIL or INCONCLUSIVE; 13.4 the decision memo (one row of §8); 13.5 deviations, if
any.*
