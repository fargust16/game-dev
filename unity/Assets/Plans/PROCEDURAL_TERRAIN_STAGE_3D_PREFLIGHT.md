# Stage 3D Preflight — Forensic Audit of the Stage 3C Trajectory Crossings

Read-only pass over the frozen checkpoint `7c882c1` (parent `97cfc99`). No Stage 3C file was
modified. One new editor-only measurement file was added,
`Assets/Scripts/Editor/TerrainGenCrossingForensics.cs`, which builds the pipeline, reads the result
and writes a report; it holds no settings, is referenced by nothing that generates, and changes no
output.

**Stage 3C remains frozen: YES.** Every file in the Stage 3C commit is byte-identical to `HEAD`.
Stage 3C tests run unmodified: **66 / 66** across `TerrainGenValleyTopologyTests` (14),
`TerrainGenValleyTrajectoryTests` (25), `TerrainGenValleyNetworkTests` (14) and
`TerrainGenCandidateGeometryTests` (13).

---

## 1. Lifecycle of a trajectory

| step | where |
|---|---|
| **corridor created** | `PrimarySpurBuilder.AddValleys` — one corridor per gap between adjacent primary spur stations, plus the two outer gaps to the attachment-interval edges. |
| **parent / neighbour identity** | `ValleyCorridor.LowerSpurId` / `UpperSpurId`, set at creation. Adjacency is derived in `ValleyNetwork.BuildChains` from `members[i].UpperSpurId == members[i+1].LowerSpurId` and from nothing else. The downstream parent is `ValleyTopology.DownstreamOf`, written only by `ValleyTopology.Build`. |
| **trajectory created** | `ValleyTrajectoryNetwork.Build` — one per corridor, `Id == CorridorId`. Slice C invents no valleys. |
| **curve, inside the massif** | `ValleyTrajectoryBuilder.BuildCandidateBed` → `FloorArcAt` → `BoundArc`. Mid-line of the two bounding divides plus the ground each releases past its toe. |
| **curve, the shared run** | `ValleyTrajectoryNetwork.Advance`, straight along the corridor's own candidate ray (`ValleyNetwork.RayOf`) through each confluence Slice B2 accepted. |
| **curve, the apron** | `ValleyTrajectoryBuilder.ContinueCandidate` → private `Continue`. Cone-constrained descent on Stage 1's surface. |
| **termination decided** | `Continue`, and only there: `ValleyTrajectoryBuilder.cs:316` world edge → `Coast`; `:321` coast signed distance ≤ 0 → `Coast`; `:328` `owner.IsOwned && owner.MassifIndex != corridor.MassifIndex` → `Divide`; `:353` loop exhausted → `Unresolved`. |
| **`Merge` termination** | `ValleyTrajectories.cs:290`, set purely from `topology.DownstreamOf[i] >= 0`. Never decided geometrically in Slice C. |

### Where `Ocean` / `OutsideDomain` live — and where they do not

They are **not trajectory states.** `ValleyTermination` has exactly four values: `Coast`, `Merge`,
`Divide`, `Unresolved`. `Ocean` and `OutsideDomain` are `ConvergenceOutcome` values produced by
`ValleyTopology.Resolve`:

- `Ocean` when `ridges.Ownership.Ownership.At(point).Kind == MassifOwnerKind.Ocean`
- `OutsideDomain` when `!owner.IsStructural`

— where `point` is **the intersection of two straight candidate rays**. The outcome attaches to a
`ToeEventResult` describing a *pair*, evaluated at *one point*. It is recorded nowhere on either
curve.

### Where the trajectory is allowed to continue after that decision

**Everywhere.** `ContinueCandidate` runs for every non-tributary regardless of any B2 rejection, and
`Continue` has **no structural-domain stop**: it never tests `owner.IsStructural`. It halts only at
the world edge, at the coastline, or on another massif's ownership. A corridor leaving its own
massif's structural domain (`BeyondReach`, `BeyondAxisEnd`, `Unclaimed`) keeps walking.

**This is the mechanism in one sentence: a B2 rejection is a statement about a point, not a boundary
on a curve.** Nothing propagates the rejection into either trajectory, because there is no field on a
trajectory for it to propagate into.

---

## 2. The eight crossings

All eight measured by `TerrainGenCrossingForensics`. Ownership at every crossing is `BeyondReach`,
`structural = false`. Every trajectory terminates `Coast`. Distances are arc length along each curve
past its own massif toe.

| # | seed | pair | bounding spurs | shared divide | adjacent | crossing | past toe A / B | axis dist | own-domain exit A / B | B2 verdict on the pair |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | 20260905 | V2 × V4 | [S1,S2] / [S3,S4] | none | no | (1335, 670) | 303 / 300 m | 523 m | 223 / 240 m | event 5, S3 → `OutsideDomain`, rays met (1366,744) @457 m |
| 2 | 20260905 | V2 × V5 | [S1,S2] / [S4,–] | none | no | (1316, 609) | 363 / 320 m | 572 m | 223 / 220 m | **never evaluated as an active pair** |
| 3 | 20260905 | V4 × V5 | [S3,S4] / [S4,–] | S4 | yes | (1196, 533) | 480 / 480 m | 642 m | 240 / 220 m | event 2, S4 → `Ocean`, rays met (1088,496) @677 m |
| 4 | 7 | V4 × V5 | [–,S3] / [S3,S4] | S3 | yes | (2221, 2041) | 500 / 500 m | 714 m | 260 / 260 m | event 1, S3 → `OutsideDomain`, rays met (2320,2016) @812 m |
| 5 | 101010 | V2 × V3 | [S1,S2] / [S2,–] | S2 | yes | (771, 2067) | 460 / 460 m | 639 m | 200 / 200 m | event 0, S2 → `OutsideDomain`, rays met (785,2013) @583 m |
| 6 | 101010 | V4 × V5 | [–,S3] / [S3,S4] | S3 | yes | (1018, 825) | 400 / 380 m | 477 m | 140 / 140 m | event 3, S3 → `OutsideDomain`, rays met (1026,980) @449 m |
| 7 | 101010 | V4 × V6 | [–,S3] / [S4,–] | none | no | (1011, 685) | 540 / 520 m | 503 m | 140 / 140 m | **never evaluated as an active pair** |
| 8 | 101010 | V5 × V6 | [S3,S4] / [S4,–] | S4 | yes | (1036, 551) | 660 / 660 m | 496 m | 140 / 140 m | event 1, S4 → `Ocean`, rays met (1160,13) @623 m |

**Both curves are already outside their own structural domain at every crossing, without exception.**
Domain exit happens 140–260 m past the toe; the crossings happen 300–660 m past it. The margin is
60–520 m.

**No curve crosses another primary structural feature before the crossing.** Zero trajectory–spur
intersections across the whole canonical set. The only axis contact any trajectory makes is a single
one at `pastToe` of −232 to −241 m — that is each bed's own head, which is defined to start at
`massif.PointAt(HeadS)`, a point on the crest. Count is 1 per curve and never positive-side.

**Two of the eight pairs were never judged by B2 at all** (#2, #7). They are non-adjacent, never
became an active pair, and are geometric consequences of their neighbours' crossings rather than
rejected convergences.

Coast proximity at the crossings ranges 40–426 m of signed distance, so three of them (#3 @40 m,
#4 @43 m, #5 @81 m) are effectively at the shoreline.

---

## 3. What the Stage 3C contract actually requires

**Answer: (D) — something else, and narrower than any of A/B/C.** The non-crossing contract in the
design is a **ridge-curve** invariant, and Stage 3C's own invariant list contains no trajectory
non-crossing requirement at all.

Citations, `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md`:

- **§ invariant table, lines 570–571** — the only two non-crossing invariants in the document:
  *"**No self-intersection** — No ridge polyline crosses itself"* and *"**No ridge crossing** — No two
  ridge curves intersect except at a declared attachment"*. Both name ridge curves.
- **line 167** — *"Because the strips partition the flank, **ridges cannot cross — there is nowhere
  for them to cross.**"* The mechanism is the strip partition, which exists only inside the flank.
- **line 257** — *"**7 — how crossings are prevented.** By the strip partition of §3.1."*
- **Stage 3B entry, line 659** — invariants *"partition, spacing bounds, attachment inside
  `CertifiedCore`, no self-intersection, no crossings, spur budget reaching zero"* — the 3B stage,
  whose output is spurs and strips.
- **Stage 3C entry, lines 667–672** — the complete invariant list: *"subtractive; ocean unchanged;
  `H3 >= H1` outside declared corridors and `H3 >= H1 - CorridorIncisionMeters` inside them;
  `H3 > SeaLevelMeters` on land; corridor monotonicity; no closed depression outside a declared basin;
  slope bands; isotropy; Stage 1 and Stage 2 immutability; determinism."* **No non-crossing clause.**
- **line 468** — *"A river crossing a depth-0 divide is a defect in one stage or the other."* This is
  the only downstream coherence statement, it is about a **river versus a divide**, it is framed as a
  Stage 4 check, and it does not address two valleys crossing each other.

`PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md` §7 records the eight and forbids repairing them in 3C; §9
forbids retroactive modification to hide them. Neither states a non-crossing requirement.

So global non-crossing to the world boundary (A) is **not** required; nor is the contract silent
about the interior (B is required but is only part of it); nor is (C) stated, because no terminal
classification exists on a trajectory to switch on. What is enforced is stated in §4.

---

## 4. What the tests guarantee — and what they do not

`TerrainGenValleyTrajectoryTests.TrajectoriesNeverCrossInsideAMassifOrOverARidge` (test 23) is the
only crossing test. It asserts exactly two things:

1. **For every trajectory-pair crossing, both segment indices are strictly greater than that
   trajectory's `ToeIndex`.** No crossing may occur at or inside the massif body.
2. **No trajectory segment intersects any `SpurCurve` segment.** No valley crosses a divide.

It does **not** guarantee, and was never written to guarantee:

- any bound on the **number** of crossings past the toes — zero and eight pass identically;
- any bound on **how far** past the toe a crossing may sit;
- that crossings occur only **outside** structural ownership — that they do is an observed property,
  not an asserted one;
- that a crossing pair was ever **evaluated** by B2 — two of the eight were not;
- that crossing pairs are **adjacent** — three of the eight are not;
- anything about the **massif axis**, the inter-massif divide, or the coastline;
- anything about a trajectory crossing another **massif's** trajectories.

The supporting counters `ValleyTrajectoryStats.MaxConfluenceGapMeters`,
`MaxConfluenceOffRayMeters`, `ConfluencesOutOfOrder` and `MissingConfluences` are asserted elsewhere,
but no stat counts crossings; the count lives only in the `crossings=` diagnostic line.

---

## 5. Classification

**An underspecified contract, which becomes a Stage 3D design constraint. Not a Stage 3C defect, and
not merely an accepted apron artefact.**

- **Not a Stage 3C defect.** No stated Stage 3C invariant is violated. All 66 relevant tests pass
  unmodified. Every crossing is past both toes, outside structural ownership, and crosses no divide.
- **Not simply an accepted artefact.** Design line 468 makes divide-versus-drainage coherence an
  explicit cross-stage check, and three of the eight crossings sit within 100 m of the shoreline where
  a carve will be shallow and two channels will visibly interfere.
- **Underspecified.** The design's non-crossing invariants name ridge curves; the apron — the ground
  past the massif foot, outside structural ownership — has no geometric contract of any kind. The
  crossings are the first thing to occupy that gap.
- **A Stage 3D constraint**, because Stage 3D's own test list (line 684) says *"no new crossings"*
  without defining the baseline, and Stage 3D is the first stage that will add curves into the same
  space.

---

## 6. Stage 3D constraint on secondary spurs

**The existing design already answers this; it is not open.** Secondary spurs **must terminate before
the terminal apron**, and they do so by domain rather than by a new rule.

- **line 244** — *"Each primary spur's own strip is partitioned by the same rule one level down, using
  the primary spur as the parent divide."*
- **line 349** — *"Secondary spurs repeat this with the primary spur as parent and its strip as the
  domain."*
- **Stage 3D entry, line 683** — invariant *"partition holds within the parent strip."*

A primary spur's strip is bounded by the spur itself, and a primary spur never leaves the flank: its
reach is `FlankHalfWidth(station) · ReliefScale · reachJitter` in
`PrimarySpurBuilder.TryBuildSpur`, and its height budget is a `ProfileCurve` reaching zero at the toe
(design §6, "how spurs terminate"). The apron is therefore outside a secondary spur's domain by
construction.

Point by point:

| question | answer | basis |
|---|---|---|
| may enter the terminal apron | **no** | the domain is the parent strip, which ends with the parent spur |
| must terminate before the terminal apron | **yes** | line 683, "partition holds within the parent strip" |
| must inherit the parent trajectory's termination domain | **no, and the question does not apply** | the parent of a secondary spur is a **spur**, not a trajectory. `SpurCurve` has no continuation machinery at all — `Continue` is corridor-only — so there is no termination domain to inherit |
| needs a separate termination rule | **no** | design §6 already gives spurs their termination: the height budget reaching zero at the toe, with reach bounded by the flank |

---

## 7. How Stage 3D could silently move the eight existing trajectories

Seven boundaries, in order of danger.

1. **`RidgeSkeleton.Build(skeleton, settings, worldSeed, stage1 = null)`.** Passing a non-null
   `stage1` runs the **old** `ValleyTrajectoryBuilder.Build` (`RidgeSkeleton.cs:102`), which mutates
   every `ValleyCorridor` through `SetTrajectory` **and** runs its own geometric merge test
   (`built[i].DistanceToFloor(next) < MergeDistanceMeters` → `ValleyTermination.Merge`). That is the
   second topology algorithm Slice C exists to avoid. **Stage 3D must keep using the three-argument
   overload**; every current caller does.
2. **Spur-id positional identity.** `ValleyCorridor.LowerSpurId`/`UpperSpurId`,
   `ValleyTrajectoryBuilder.BoundArc`, `Cone`, `TerminalTangent`, `ValleyNetwork.BuildChains` and
   `BuildToeEvents` all index `spurs[spurId]` by position. Secondary spurs must be **appended** with
   ids above the primary count, or held in a separate collection. Interleaving or renumbering
   silently relocates every candidate bed.
3. **The corridor set.** `PrimarySpurBuilder.AddValleys` creates corridors from the primary station
   list. If secondary spurs feed the same construction, `RidgeSkeleton.Valleys` grows — and that
   changes `ValleyNetwork.RankOrder`, chain membership, the toe-event list and therefore B2's
   processing order and its merge results. Depth-2 curves must not produce depth-2 corridors unless
   that is a deliberate, separately-audited decision (see §8).
4. **The RNG streams.** `PrimarySpurBuilder` forks `terrain.ridge.station` and `terrain.ridge.shape`
   per massif and per flank (`PrimarySpurBuilder.cs:112, 232`). A secondary pass drawing from either
   of those forks — before, after or interleaved — shifts every primary station, drift and reach.
   Stage 3D needs its own stream id.
5. **`RidgeSettings` shared fields.** `SpurSpacingPerHalfWidth`, `SpurStationJitter`,
   `SpurDriftPerGap`, `SpurReachJitter`, `SpurReliefFloor`, `MaxPrimarySpursPerFlank` and
   `SpurSampleSpacingMeters` all feed the primary skeleton. Stage 3D must add fields, never retune
   these.
6. **Mutable arrays behind read-only lists.** `IReadOnlyList<Vector2[]>` protects the list, not the
   elements. `SpurCurve.Points`, `SpurCurve.Cumulative`, `ValleyNetwork.CandidateBeds[i]`,
   `FinalValleyTrajectory.Points` and `.Cumulative` are all directly writable arrays.
7. **`ValleyCorridor` is mutable.** `Floor`, `Cumulative`, `ToeIndex`, `HalfWidthMeters`,
   `Termination` and `DownstreamId` have private setters driven by `SetTrajectory`. Slice C
   deliberately writes none of them, and the test `SliceCMutatesNothing` pins that. Stage 3D should
   stay under the same discipline.

The cheapest regression guard: rerun the Slice C diagnostic and require the `crossings=` line, the
confluence count and every trajectory length to be unchanged on the six massif-bearing seeds.

---

## 8. Ambiguities that must be resolved before Stage 3D implementation

Three, all in `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md`. None should be resolved by inventing a rule.

1. **"No new crossings" has no defined baseline.** Stage 3D's test list (line 684) says *"depth cap;
   tier separation measured from the field; **no new crossings**"*. Against what? If the baseline is
   zero, Stage 3D starts eight in deficit through no act of its own. If the baseline is the eight
   recorded here, the test needs to say so, and needs to say what counts as a crossing — spur × spur,
   spur × trajectory, or trajectory × trajectory. The existing Stage 3C test covers only spur ×
   trajectory and trajectory × trajectory inside the massif.

2. **Whether Stage 3D creates corridors.** Stage 3D's *Out* is *"depth-2 curves and their strips"* —
   curves and strips, not corridors. But Stage 3B's own rule creates one corridor per gap between
   adjacent spurs, and the design says secondary spurs repeat the partition rule. If that rule is
   repeated literally, depth-2 corridors appear, enter Slice A/B/B2/C, and the primary network moves.
   The design does not say. This must be settled before any code.

3. **The apron has no contract.** The ground past the massif foot and outside structural ownership is
   where the corridor continuation runs, where all eight crossings occur, and where a secondary spur
   may not go. The design defines *"corridor continuation"* (line 374, *"Below the massif apron the
   corridor continues as a shallow trough"*) but states no geometric invariant over that region — not
   non-crossing, not separation, not a width. Stage 3D is the first stage that must know whether the
   apron is structural ground with rules or a run-out with none.
