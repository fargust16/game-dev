# Stage 2A → Stage 2B — Contract Hardening

**Status: CHANGES REQUIRED** — the contract is landed and the failure class is now impossible, but
seed 1337's island certifies no axis and therefore has no massif. That is a correct refusal, not a
malformed body; whether a world may legitimately have no T1 landmark is a spec decision, stated in
§14. Stage 3 not started. Nothing committed or pushed.

Date: 2026-09-08 · Unity 6000.5.7f1 · generator version 1

---

## 1. Audit

`Assets/Scripts/World/TerrainGen/` is **entirely untracked** (`git ls-files` returns nothing), so git
can restore none of it. Every file was read in full before being edited; no `reset`, `checkout`,
`restore`, `clean`, revert or delete was run.

The ten audit questions, answered by measurement rather than assumption:

| # | Question | Answer |
|---|---|---|
| 1 | How is the macro axis selected? | Convergent macro plate boundaries → chains grown by smallest turn at each junction → sorted by chain length, accepted longest-first without sharing a boundary → filleted → clipped to land → sorted by length → judged. |
| 2 | How are its endpoints selected? | By `ClipToLand`: the run began and ended exactly where `SignedDistance` crossed `CoastSetbackMeters`. Endpoints sat on the 60 m isoline. |
| 3 | How was coast setback measured? | Point-wise `CoastQuery.SignedDistance` **on the axis only**, against one global constant. The width the axis would carry never entered the test. |
| 4 | Can the axis run close to the coast? | Yes. Anything ≥ 60 m inland qualified, including a stretch running parallel to the shore at 60–120 m — which is exactly what seed 1337's axis did for 400 m. |
| 5 | Is the bad axis unavoidable or a bad candidate? | Measured: seed 1337 offers **two** land-reaching convergent chains and neither has room. It is not a ranking failure; the island has no better site. |
| 6 | Are candidates ranked or rejected? | Ranked by length alone. Rejected for short / self-intersecting / narrow / budget / crowding. **Clearance was in neither list.** |
| 7 | Could Stage 2A reject before certifying? | Yes — it already holds the `CoastQuery` and already computes the intended half-width. It had everything it needed and did not use it. |
| 8 | Can the axis be shortened to a valid interval? | Yes; `ClipToLand` already performed that class of operation with the wrong threshold. |
| 9 | Would shortening still satisfy length / L/W / reach / no self-crossing? | Measured — see §9. Certification enforces length and L/W directly; reach and self-intersection are unaffected because the run is a sub-interval of an already-filleted curve. |
| 10 | Is `CoastSetbackMeters = 60` geological? | **No.** Its doc string was one line — "how far inside the coast an axis has to stay" — and it was lowered to 60 during Stage 2A tuning to win axis length. It is an implementation convenience with no derivation. |

---

## 2. Root cause

Stage 2A certified **what the macro geology intended**; Stage 2B had to build **what would physically
fit**. The two were never the same quantity and nothing checked that they agreed.

Stage 2B raises a body reaching its half-width either side of the axis and fades it in from the
shoreline over a coastal gate. Where the gate is part-open, it does not lower a cross-section evenly:
the flank nearer the water is cut while the inland one is not, so the section is **sheared** rather
than scaled, and the body's width stops being a property of the massif. On seed 1337 that removed a
quarter of the high ground and left the rest as wide as it was long — a mesa.

The secondary defect: `MacroAxis.ComputeReach` discarded candidate radii using an absolute
`offAxis < 1e-4` guard. On a near-straight polyline the off-axis component of a chord is float noise,
so the formula returned a huge finite radius — the `reachW_m = 4490485` in the report. Not a
measurement of anything.

---

## 3. Existing contract

```
MacroAxis {
    Points                    // clipped where SignedDistance >= 60 m
    IntendedHalfWidthMeters   // min(target, runLength / 3.2)   <- what geology wanted
    UsableHalfWidthMeters     // min(intended, 0.72 * reach)    <- curvature only
    Accepted                  // length, self-intersection, width, budget, crowding
}
```

Nothing in it says anything about room. Stage 2B discovered the shortfall afterwards and paid for it
downstream — trimming the axis, moving the culminations, re-deriving the width — and each of those
traded one wrong shape for another.

---

## 4. New contract

```
MacroAxis {
    Points                    // clipped where SignedDistance >= CoastSetbackMeters (on land)
    UsableHalfWidthMeters     // certified: room, length, L/W and curvature together
    CertifiedCore             // (s0, s1): the stretch whose whole section clears the coast
    CoastClearanceMeters      // measured minimum clearance along the run
}
```

Two additions to `MacroStructureSettings` carry the geometry, not Stage 2B's internals:

- `MassifCoastClearanceMeters` — the room a massif needs beyond its own half-width. Defaults from
  `TerrainGenSettings.UpliftCoastGateMeters`, which is where the project already records that
  distance; `MassifSettings.CoastGateMeters` defaults from the same place, so neither stage reaches
  into the other and a test asserts they still agree.
- `MinCoreLengthPerHalfWidth` — how long the high ground must be per metre of half-width. Two: the
  crest region is at least as long as the body is wide.

Certification finds the width rather than assuming it: for each on-land stretch it walks the target
half-width downwards and takes the first width whose **core** — the sub-stretch where both edges of
the cross-section clear `MassifCoastClearanceMeters` — is long enough. The scan is monotone (a
narrower body needs less room, so its core can only grow), so the first answer is the largest.

Stage 2B consumes it and decides nothing:

```csharp
var massif = MountainMassif.Build(
    worldSeed, index, axis, terrainSettings, massifSettings, axis.CertifiedCore);
```

Its longitudinal profile is laid out **on the core**: the crest reaches the shoulder value at the core
boundary and every control point above the shoulder lies inside it, so high ground outside the
certified stretch cannot be constructed. `ShearedHighFraction` survives as a verification that the two
stages still agree about the same geometry; it now reads **0.00 on every seed**.

---

## 5. Why this is structural

The old failure required Stage 2B to notice, after the fact, that it had been handed geometry it could
not use — and every repair available to it was a deformation. The new failure mode is a refusal at the
point where the information exists. An axis that cannot carry a massif is never offered, so no
downstream stage has to detect the condition, and there is no code path from "bad axis" to "malformed
body" left to take.

The three pieces of Stage 2B machinery that existed only to pay for the broken contract —
`TrimToBuildable`, `CulminationWindow` and the shear-driven re-narrowing, with their four settings —
are **deleted**, not disabled. That is the test of whether a contract is real: the compensating code
becomes unreachable rather than merely quiet.

---

## 6. Alternatives considered and rejected

| Alternative | Measured outcome | Verdict |
|---|---|---|
| Global setback = gate + half-width ≈ 480 m | Certified **1 of 5** seeds. Corridor on a 1.7 km island is ~600 m; axes are 800–1700 m. | Rejected — destroys valid geography, exactly what §4 forbids. |
| Clearance measured **at the axis** rather than at the section edges | Certified **3 of 5**. Assumes the coast runs square across the section, which is far stronger than the geometry requires. | Rejected — over-strict for no geometric reason. |
| Certify the **whole axis** at the section-edge test | Certified 3 of 5; rejected 110201, whose massif was demonstrably sound (shear 0.00). Its corridor is 600 m and its axis 939 m — the extra 339 m being tapering ends that carry nothing. | Rejected — a massif's ends are allowed to run down to the water. |
| Lower `MinUsableHalfWidthMeters` from 200 m to ~170 m to admit 1337 | Would certify 1337 at W ≈ 160–170. | Rejected — §4 forbids globally reducing width, and it would let every other world certify narrower bodies too. |
| Narrow the coastal gate below 200 m | Would enlarge every corridor. | Rejected — §7 forbids weakening the gate, and it steepens the coastal profile everywhere to rescue one seed. |
| Rank candidates by corridor so a bad axis loses (§6-C) | Measured: seed 1337 has two land-reaching convergent chains and **both** have no room. Ranking cannot help. | Implemented anyway as the corridor probe, but it does not rescue 1337. |

---

## 7. Files changed

| File | Why |
|---|---|
| `World/TerrainGen/MacroAxis.cs` | `CertifiedCore`, `CoastClearanceMeters`, `MaxMeasurableReachMeters`; reach returns `+∞` when nothing inside the world constrains it. |
| `World/TerrainGen/MacroStructureSettings.cs` | `MassifCoastClearanceMeters`, `MinCoreLengthPerHalfWidth`, `CertificationWidthStepMeters`; `CoastSetbackMeters` re-documented as a candidacy filter, not the contract. |
| `World/TerrainGen/MacroAxisBuilder.cs` | `Certify` / `TryCertify` / `LongestRun` replace the fixed-setback clip; corridor probe for the report. |
| `World/TerrainGen/Stage2AMacroStructure.cs` | Corridor and certification lines in the report. |
| `World/TerrainGen/MassifSettings.cs` | Retired `MinBuildableGate`, `MinBuildableLengthMeters`, `MinLengthToWidthRatio`; added `MinCoreSpanForTwoCulminations`; documented the shared gate. |
| `World/TerrainGen/MountainMassif.cs` | Longitudinal profile laid out on the certified core; `CoreInterval` exposed; a second culmination needs a core wide enough to hold one. |
| `World/TerrainGen/Stage2BMassifGenerator.cs` | Consumes `CertifiedCore`; `TrimToBuildable` and `CulminationWindow` deleted; unbounded values printed as `inf`. |
| `Editor/Tests/TerrainGenStage2ATests.cs` | 4 tests added, 1 rewritten. |
| `Editor/Tests/TerrainGenStage2BTests.cs` | 3 tests added, 3 scoped to worlds that have a massif. |
| `Editor/Tests/TerrainGenStage2BRobustnessTests.cs` | Assertions measured against the certified core; builds through the contract. |

---

## 8. Tests added

**Stage 2A** (19 → 23)
- `ACertifiedAxisHasRoomForTheBodyItCertifies` — walks the core and asserts both section edges clear
  `MassifCoastClearanceMeters`. This is the contract, stated as an assertion.
- `TheCertifiedCoreIsDeterministic` — core and certified width repeat exactly.
- `AStraightAxisReportsAnUnboundedReach` — reach is `+∞`, curvature 0, and the certified width is not
  narrowed by it.
- `NoAxisEverReportsAnAbsurdFiniteReach` — a finite reach is never larger than the world.
- `EverySeedEitherCertifiesAnAxisOrSaysWhyItCannot` — **rewritten.** It used to assert every seed
  produced an axis; that was the old contract, and it passed while seed 1337 shipped a mesa. Stage 2A
  now certifies room as well as geology, so "every world has a mountain" is not something it can
  promise. What it still promises, and what this asserts, is that the refusal is never silent.

**Stage 2B** (19 → 21)
- `NoHighGroundStandsInAGateThatCannotCarryIt` — **rewritten** from a 30 % ceiling on a known defect to
  the contract's zero.
- `BothStagesAgreeAboutTheCoastalGate` — the two settings read the same distance.
- `EveryMassifStandsOnACertifiedAxis` — a massif is never wider than its axis certified.

Three Stage 2B tests were scoped to worlds that have a massif (`counted > 0`, the T1 height check, and
the accepted-axis count). Each is a case the contract can no longer promise for a world with no site,
and each is justified in a comment at the assertion.

**Robustness** (8, 24 seeds) — assertions re-measured against `CoreInterval` rather than the raw
certified interval, and the culmination-separation bound replaced with the construction's own
guarantee (0.64 − 0.36 = 0.28) instead of a guessed 0.2. This suite found four real defects during the
pass, including the narrow-core cliff fixed by `MinCoreSpanForTwoCulminations`.

---

## 9. Canonical seed metrics, before and after

| Seed | | length | nominal W | measured W | L/W | crest | culms | saddle | peak off | d2 | **sheared** | fidelity |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 110201 | before | 939 | 280 | 319 | 3.36 | 284 | 1 | — | 37 | 1.254 | 0.00 | 0.56–0.57 |
| | **after** | **999** | 280 | 320 | **3.57** | 284 | 1 | — | 37 | **0.982** | 0.00 | 0.56–0.57 |
| 424242 | before | 1283 | 280 | 319 | 4.58 | 293 | 2 | 0.47 | 4 | 0.657 | **0.09** | 0.56–0.58 |
| | **after** | **1303** | 270 | 306 | **4.83** | 293 | 2 | 0.47 | 27 | 1.341 | **0.00** | 0.56–0.57 |
| 20260905 | before | 1644 | 280 | 319 | 5.87 | 318 | 2 | 0.40 | 5 | 0.682 | 0.00 | 0.56–0.58 |
| | **after** | **1705** | 280 | 317 | **6.09** | 318 | 2 | 0.40 | 52 | 0.776 | 0.00 | 0.56–0.57 |
| 7 | before | 1067 | 280 | 318 | 3.81 | 302 | 1 | — | 26 | 0.843 | 0.00 | 0.55–0.57 |
| | **after** | **1168** | 280 | 315 | **4.17** | 302 | 2 | 0.43 | 17 | 1.254 | 0.00 | 0.47–0.57 |
| 1337 | before | 784 | 245 | **231** | 3.20 | 323 | 1 | — | 27 | 1.981 | **0.24** | **0.24**–0.57 |
| | **after** | — | — | — | — | — | — | — | — | — | 0.00 | — (no massif certified) |

Every surviving massif is **longer** than before at the same or near-same width. The contract did not
cost the healthy worlds anything; certification let 110201 grow from 939 m to 999 m because the old
60 m clip, not the geology, had been ending it.

**Corridor measured per island** (longest run whose whole section at half-width W clears 200 m):

| Seed | W=280 | W=240 | W=200 | W=160 | W=120 | W=0 |
|---|---|---|---|---|---|---|
| 110201 | 600 | 600 | 600 | 580 | 580 | 620 |
| 424242 | 301 | 682 | 762 | 862 | 902 | 1023 |
| 20260905 | 983 | 1183 | 1263 | 1384 | 1424 | 1404 |
| 7 | 825 | 785 | 765 | 725 | 705 | 765 |
| **1337** | **342** | **342** | **342** | **362** | **382** | **402** |

Seed 1337 needs a core of `2 × 200 = 400 m` at the narrowest permitted width and has **342 m**. It is
short by 58 m, and no width in the permitted band closes the gap.

---

## 10. Visual verdict

| Seed | Verdict | Reading |
|---|---|---|
| 110201 | **PASS** | 999 m body, one broad culmination well inland, long even skirts, no shear. |
| 424242 | **PASS** | Two culminations with a 0.47 saddle, high ground fully inside the corridor. |
| 20260905 | **PASS** | The reference: 1705 m, L/W 6.09, S-curved band of constant width along the axis. |
| 7 | **PASS** | Two culminations, high ground inland, coastal skirt tapering to the water. |
| **1337** | **NO MASSIF** | An island with no mountain. Correct under the contract and honestly reported — but it is not "a plausible broad mountain structure", which is why this pass is not APPROVED. |

---

## 11. Performance

| Phase | Before | After |
|---|---|---|
| Stage 2A total | 122–148 ms | 132–151 ms |
| — axis build | 5–9 ms | 17–30 ms |
| Stage 2B total | 6.5–8.8 s | 5.8–8.8 s |
| — massif build | 0–2 ms | 0–1 ms |

Certification costs ~15 ms a seed: eight width probes over ~90 curve points, two `SignedDistance`
calls each. It runs once per curve piece, allocates two arrays per piece, and does no per-pixel work.
Stage 2B lost the trim and window scans it no longer needs. Both stages remain dominated by Stage 1's
1025² field, which is untouched.

---

## 12. Determinism

Same seed, same output: `TheCertifiedCoreIsDeterministic` (Stage 2A), `SameSeed_ProducesTheSameField`
(Stage 2B, sample-by-sample) and `TheModelIsDeterministic` (robustness) all pass. No
`UnityEngine.Random`; every draw is a named `IWorldSeed` fork. The certification scan is a fixed
descending ladder with no early exit that depends on iteration order.

**Stage 1 hashes** — `7599879a71250a06`, `6266075eaf593ee2`, `4ea1152ce66e0937`, `b85f596c538e0192`,
`bef4ec87b64f3594`. All five identical, and Stage 2B additionally compares the base field
sample-by-sample.

**Full EditMode suite: 402 / 402 passed, 0 failed, 160.8 s.**
Stage 1 20/20 · Stage 2 21/21 · Stage 2A 23/23 · Stage 2B 21/21 · Robustness 8/8 (19 massifs over 24
seeds, lengths 915–2076 m, half-widths 208–280 m, curvature 0–0.00341 1/m, 7 single / 12 twin
culmination).

---

## 13. Scope audit

```
Stage 1: untouched          Stage 3: not started        Vegetation: untouched
Stage 2: untouched          Hydrology: untouched        Roads / POIs: untouched
Stage 2A: contract only     TerrainData: untouched      Settlements: untouched
Stage 2B: consumes it       Terrain bake: untouched     Scene: untouched
                            Materials: untouched        Packages: untouched
```

No commits. No pushes. No file overwritten without being read first.

---

## 14. Remaining risks and the decision this pass cannot make

1. **Seed 1337 has no massif, and that is a spec question.** Its island offers 342 m of corridor where
   the narrowest permitted body needs 400 m. Three levers exist and each changes a specification:
   - lower `MinUsableHalfWidthMeters` from 200 m (admits narrower ranges everywhere);
   - lower `MassifCoastClearanceMeters` from 200 m (steepens every coastal massif);
   - have Stage 1 produce larger islands (Stage 1 is frozen).

   None is a Stage 2A/2B bug, and choosing one silently would be the fudge this pass exists to remove.

2. **How often does a world get no massif?** Measured on 24 unrelated seeds: 19 massifs from 24 worlds,
   so roughly one world in five certifies nothing. If that rate is unacceptable, it is the same spec
   decision as (1), now with a frequency attached.

3. **The high ground is more compact than before.** Binding the crest to the certified core shortens
   the region above half crest and lengthens the skirts. That is the correct trade — the alternative
   is high ground the coast deforms — but Stage 3 should expect a shorter ridge region than the
   massif's full length.

4. **`MinCoreSpanForTwoCulminations = 0.40` was set from a measured failure** (a 13 m step in the crest
   line on a narrow core) and not from theory. It is the one number in this pass chosen empirically.

---

## 15. Verdict

# CHANGES REQUIRED

The contract is landed and the failure class is closed: Stage 2A certifies room, length, aspect and
curvature together; Stage 2B builds inside what it was given and has no machinery left to compensate
with; the shear that produced seed 1337's mesa reads 0.00 on every world; the reach diagnostic reports
unbounded instead of six million metres; and the four healthy canonical seeds came out longer than
before at the same width. 402 / 402 tests pass and Stage 1 is byte-identical.

It is not APPROVED because acceptance criterion L is not met: seed 1337 produces no mountain at all.
That is the contract answering correctly, not a defect to patch — but it needs the specification
decision in §14 before the five canonical seeds all show a plausible massif.
