# Stage 2A — Candidate Selection Hardening

**Status: APPROVED.** The selection defect the 29-seed feasibility audit measured is closed. No
specification number was changed. Stage 1 is byte-identical, Stage 2B's hardening is intact, and
Stage 3 was not started. Nothing committed or pushed.

Date: 2026-09-08 · Unity 6000.5.7f1

---

## 1. Audit — the old control flow

Everything below is in `MacroAxisBuilder.Build`, and the line numbers are the ones the audit read.

| # | Question | Answer |
|---|---|---|
| 1 | Where are chains generated? | `DiscoverChains` → `Grow`. One chain is grown from **every** convergent boundary as a seed, forward and backward, picking the smallest-turn convergent neighbour at each junction. Chains therefore overlap heavily by construction. |
| 2 | Where were chains ranked? | `Build`, lines 104–108: `chains.Sort` on `LengthMeters` descending, tie-broken by first boundary index. |
| 3 | Where were shorter chains excluded? | `Build`, lines 112–128. A chain any of whose boundaries is already `used` is skipped outright; an accepted chain marks all of its boundaries `used`. **This ran before certification.** |
| 4 | Where is convergence evaluated? | `Build`, line 81 — first, per boundary, against `ConvergenceThreshold`. |
| 5 | Where is coastal clearance evaluated? | Twice. A weak 60 m `TouchesLand` test at line 91 decides candidacy; the real section-edge test lives in `Certify`, which ran at line 150 — **after** exclusion. |
| 6 | Where is `CertifiedCore` calculated? | `Certify` → `TryCertify` → `LongestRun`, reached only from line 150. |
| 7 | Could certification influence which chain survived? | **No.** Ownership was settled before any certification ran. |
| 8 | Could a convergent boundary with room be discarded before certification? | **Yes** — and it was. Measured on seeds 101010 and 20261231. |
| 9 | Can two individually useful boundaries be one chain? | Yes; `Grow` joins adjacent convergent boundaries, so a chain can be long while only part of it certifies. |
| 10 | Did length dominate everything? | **Yes.** `LengthMeters` was the sole ranking key, and the only other input to survival was boundary exclusivity. |

**Root cause.** A chain that certified nothing could still win the length ranking, take exclusive
ownership of its boundaries, and thereby delete a shorter chain that would have certified. The audit
measured **83 of 123 discovered chains discarded before certification (67 %)**; on two worlds that
cost the world its mountain while a convergent boundary with 456 m and 793 m of usable core sat in a
chain nobody evaluated.

---

## 2. The selection contract

> Do not consume or discard potentially viable geological structure before the geometric feasibility
> of the resulting massif has been evaluated.

Stated as an invariant that can be asserted:

```
ChainsDiscovered == ChainsCertified + ChainsWithoutCertifiedRun
ChainsCertified  == ChainsAccepted  + CertifiedChainsDiscardedByOverlap
```

The second line is the fix. A certified chain can only lose to **another certified chain** that
claimed a shared boundary first. It can never lose to a chain that turned out to be unusable.

The three concerns stay separate and are applied in this order:

- **A — geological viability.** Convergence, unchanged, still gates which boundaries may be chained
  at all. Nothing was lowered.
- **B — geometric viability.** Certification: coastal clearance at both section edges, certified core,
  usable half-width, minimum length, aspect ratio. Unchanged in substance, moved earlier in time.
- **C — competition.** Exclusive boundary ownership, resolved last, among candidates already known to
  be viable.

---

## 3. The new control flow

```
discover chains  (unchanged: from convergent boundaries only)
      ↓
fillet + certify EVERY chain          ← certification moved here
      ↓
drop chains that certify nothing      ← they consume no boundaries
      ↓
rank surviving chains by certified outcome
      ↓
resolve exclusive boundary ownership
      ↓
rank candidates by certified outcome, then Judge
```

**The ranking key** is the certified high-ground footprint, `UsableHalfWidth × CoreLength` — both
quantities certification established, and their product is the area of high ground the world actually
gets. Ties fall to axis length, then to mean convergence (geology decides between two chains that
would build the same massif), then to a positional tie-break. Every comparison is a total order on
deterministic quantities, so the outcome repeats exactly.

**Why certification now participates before irreversible exclusion.** Ownership is irreversible: once
a boundary is marked used, every other chain containing it is gone. An irreversible decision must not
be taken on information that does not predict the outcome, and chain length does not — the audit's
101010 has a 1706 m certified axis on a chain that raw length ranked below one certifying nothing.

**What did not change:** chain discovery, the convergence threshold, the clearance requirement, the
certification algorithm, `Judge`, and every specification number. Stage 2B is untouched; it still
consumes `CertifiedCore` and contains no repair machinery.

---

## 4. Class D — corrected

| Seed | Before | After |
|---|---|---|
| 101010 | 0 axes; 1 convergent boundary with 456 m core **never carried by any candidate**; 4 of 5 chains discarded | **1 axis**, 1706 m, W = 200 m, L/W 8.53; unchained roomy convergent boundaries **0** |
| 20261231 | 0 axes; 2 convergent boundaries with 793 m core **never carried**; 3 of 4 chains discarded | **1 axis**, 892 m, W = 270 m, L/W 3.30; unchained **0** |

Both worlds now carry the geology the audit said was there. The fix is not seed-specific: nothing in
the generator names a seed, and the regression test that guards these two asserts an outcome, not a
branch.

---

## 5. Class C — a legitimate geological refusal

Seeds 1337, 256 and 777 still produce no massif, and the fix makes the reason unambiguous rather than
merely plausible.

| Seed | Best core on a **convergent** boundary | Best core on **any** boundary | Chains certified |
|---|---|---|---|
| 1337 | 358 m | 618 m | **0 of 2** |
| 256 | 330 m | 508 m | **0 of 3** |
| 777 | 356 m | 537 m | **0 of 3** |

The contract requires 400 m of certified core. On each of these worlds the boundaries that converge
do not have the room, and the boundaries that have the room do not converge. Before the fix this was
confounded — chains were discarded unexamined, so "no massif" could have meant either. Now **every
chain is certified and none passes**, which is the measurement that separates the two readings.

This is classification **(3), a legitimate geological refusal under the current specification.** It is
not a construction defect: chains are grown from every convergent boundary, both directions, so no
convergent interpretation is lost. It is not an evaluation-order defect: order is now provably
irrelevant, since nothing is discarded before certification.

Making these worlds pass would require lowering `ConvergenceThreshold` — chaining through boundaries
the plate model says are not converging. That is inventing geology to raise a pass rate, and it was
not done.

Seed 31337 remains class B: its island offers 440 m of core somewhere, but its best plate boundary
reaches 389 m against the 400 m required. Genuinely marginal, one world in twenty-nine.

---

## 6. Results — 29 seeds

| Measure | Before | After |
|---|---|---|
| Worlds with a massif | 23 / 29 (79.3 %) | **25 / 29 (86.2 %)** |
| Chains discovered | 123 | 123 |
| **Chains discarded before certification** | **83** | **0** |
| Chains that own boundaries | 40 (many certifying nothing) | 26 (all certified) |
| Roomy convergent boundaries never carried | 9 | **6** |
| — of those, in worlds with no massif | 3 | **0** |
| Worlds with no valid candidate | 6 | 4 |

The six remaining uncarried roomy boundaries are all in worlds that already have a massif (seeds 2,
202020, 500001, 1234567); they are second sites left unused by `MaxSystems` and the crowding rule, not
material discarded unexamined.

**No world regressed.** Every seed that had a massif still has one.

### Canonical seeds

| Seed | Axis | W | L/W | Reach | Accepted | Change |
|---|---|---|---|---|---|---|
| 110201 | 999 m | 280 | 3.57 | 446 m | yes | none |
| 424242 | 1303 m | 270 | 4.83 | 446 m | yes | none |
| 20260905 | 1704 m | 280 | 6.09 | 449 m | yes | none |
| 7 | 1168 m | 280 | 4.17 | 461 m | yes | same chain, traversed from the other end |
| 1337 | — | — | — | — | no | class C refusal, now with 0 of 2 chains certifying |

Seed 7's chain is the same three plate pairs in reversed order, because a different seed boundary now
ranks first among identical candidates. Length, width, reach and L/W are unchanged; the longitudinal
profile is mirrored, moving its summit offset from 26 m to 11 m and its worst second difference from
1.254 m to 1.221 m. Its Stage 2B field hash therefore changes. Stage 2B hashes were never frozen —
only Stage 1's are — so this is a change, not a regression, and it is recorded here rather than
glossed.

---

## 7. Stage 2B regression

All worlds that produce a massif, measured after the change:

```
sheared high ground        0.00   on every seed
ocean samples changed      0
negative uplift            0
ceiling clamped            0
reach clamp bound          False
width fidelity             0.53 - 0.58
Stage 1 hashes             7599879a71250a06  6266075eaf593ee2  4ea1152ce66e0937
                           b85f596c538e0192  bef4ec87b64f3594   (all unchanged)
```

The reach diagnostic prints `inf` for seed 20261231's straight axis rather than an absurd finite
number, which is the earlier fix continuing to hold on a newly reachable world.

**Full EditMode suite: 406 / 406 passed, 0 failed, 171.6 s.**
Stage 1 20/20 · Stage 2 21/21 · Stage 2A **27/27** (was 23) · Stage 2B 21/21 · Robustness 8/8
(21 massifs over 24 seeds, was 19).

---

## 8. Tests added

All existing assertions are intact; none was weakened, and no existing test needed rewriting, because
the change is to ordering rather than to any contract a test encoded.

| Test | What it locks |
|---|---|
| `AnUncertifiableChainConsumesNothing` | The invariant of §2, both lines. A chain that certifies nothing cannot take a boundary from one that does. |
| `EveryCandidateIsCertified` | Every candidate has a non-empty core, a width at or above the usable floor, and clearance — so Stage 2B can only ever receive certified axes. |
| `SelectionAndOverlapResolutionAreDeterministic` | Discovery, certification, ownership and candidate order all repeat exactly. |
| `TheWorldsTheOldRankingLostStillCertifyAMassif` | Regression guard on 101010 and 20261231, asserting an outcome rather than a branch: both certify, and the accepted axis follows converging plates. |
| Stage 2B: extended `EveryMassifStandsOnACertifiedAxis` | A massif is never raised on an axis with an empty certified core. |

---

## 9. Performance

| Phase | Before | After |
|---|---|---|
| Stage 2A total | 132–151 ms | 132–226 ms |
| — axis build | 17–30 ms | 17–107 ms |
| Stage 2B total | 5.8–8.8 s | 5.8–8.8 s |

Certification now runs on every discovered chain rather than on the one or two that won a length
race, which is a three- to five-fold increase on a phase that is two to three per cent of a world's
generation time. It stays curve-level: no raster, no per-pixel work, two `SignedDistance` calls per
sample per probed width along a curve of some ninety points. Stage 2B is unchanged because Stage 1's
1025² field dominates it.

---

## 10. Scope

```
Stage 1: untouched          Stage 2B: untouched        Terrain bake: untouched
Stage 2: untouched          Stage 3: not started       Scene: untouched
Hydrology: untouched        TerrainData: untouched     Packages: untouched
```

Files changed: `World/TerrainGen/MacroAxisBuilder.cs` (selection order, ranking, three report
counters); `Editor/Tests/TerrainGenStage2ATests.cs` (+4 tests);
`Editor/Tests/TerrainGenStage2BTests.cs` (one assertion extended). No commits. No pushes.

---

## 11. Verdict

# APPROVED

- The selection defect is fixed, and the fix is an invariant a test asserts rather than a behaviour
  observed on the seeds that were looked at.
- No certifiable candidate can be discarded by a non-certifiable longer one: chains discarded before
  certification went from 83 to 0.
- Class D is corrected on both worlds, and the boundaries the audit named are now carried by accepted
  candidates.
- Class C is demonstrated to be a legitimate geological refusal: every chain on those worlds is now
  certified and none passes, while the roomy boundaries are below the convergence threshold.
- No specification number was weakened; the convergence threshold, clearance, widths, lengths and
  ratios are untouched.
- Stage 2B's hardening holds: shear 0.00, ocean unchanged, no negative or non-finite values, no
  absurd finite reach, width consistent with the certified width.
- Stage 1 is byte-identical.
- Determinism is asserted for discovery, certification, ranking and overlap resolution.
- 406 / 406 tests pass.

Stage 3 is not started and must not begin from this document.
