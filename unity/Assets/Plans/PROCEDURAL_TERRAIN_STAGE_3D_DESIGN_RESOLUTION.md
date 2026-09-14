# Stage 3D — Design Resolution

Settles the architecture before any Stage 3D code. Nothing here is implemented. The frozen checkpoint
is `7c882c1`; every file in it is byte-identical at the time of writing.

---

## 1. Frozen Stage 3C boundary

Stage 3D reads the Stage 3B/3C output and writes none of it. Specifically it must not:

- rebuild Stage 3A, 3B or 3C;
- call `RidgeSkeleton.Build(skeleton, settings, worldSeed, stage1)` with a non-null `stage1` — that
  overload runs the legacy `ValleyTrajectoryBuilder.Build`, which mutates every `ValleyCorridor`
  through `SetTrajectory` and runs its own geometric merge test;
- mutate `ValleyCorridor`, `FinalValleyTrajectory`, or any primary `SpurCurve` — including writing
  through the exposed `Points` / `Cumulative` arrays, which `IReadOnlyList` does not protect;
- change primary spur ids or their positional ordering;
- change the corridor set;
- draw from `terrain.ridge.station` or `terrain.ridge.shape`.

**The eight terminal-apron trajectory crossings recorded in
`PROCEDURAL_TERRAIN_STAGE_3D_PREFLIGHT.md` §2 are the Stage 3C baseline.** They are not repaired,
removed, relocated, reinterpreted or hidden by Stage 3D.

## 2. Stage 3D responsibility

A **structural subdivision layer**, one level down. In: the frozen primary ridge output. Out:
depth-2 ridge curves and their child strips. It resolves no topology, builds no trajectory, carves
nothing.

## 3. Input / output data model

### 3.1 Why `SpurCurve` cannot carry a secondary spur

`SpurCurve` is **crest-anchored** in four places, and a secondary spur is anchored to its parent
ridge instead:

| member | documented meaning | true for a secondary? |
|---|---|---|
| `AttachS` | "Where on the crest it attaches, as normalised arc… always inside `MassifFlank.AttachmentInterval`" | no — it attaches to a primary spur |
| `StripInterval` | "The stretch of **crest** this spur commands… the strips of one flank partition the attachment interval exactly" | no — its domain is a sub-region of the parent strip, which is two-dimensional |
| `ArcAt(t)` | `AttachS + DriftArc · smoothstep(t)` — the ridge line in **massif crest arc** as a function of transverse flank depth | no, and dangerously so: this is the exact method `ValleyTrajectoryBuilder.BoundArc` calls to place a valley floor |
| `Origin` | "Where it leaves the crest" | no — it leaves the parent ridge |

Reusing `SpurCurve` would therefore require overloading `AttachS` and `ArcAt` to mean something they
do not, in the one type the frozen valley-floor construction reads. **A separate type is the smallest
clean answer, not the larger one.**

### 3.2 The model, as built

Four new types, one new collection, nothing existing changed. `Termination` is a
`SecondaryTermination` of its own rather than `SpurTermination`: a primary spur ends against the world
- the coast, a neighbouring massif, the edge of the frame - while a rib ends against its parent's own
structure, and sharing one enum would invite the reading that a rib can stop for a world reason, which
is exactly what its domain rule forbids.

```
SecondarySpur                    // a depth-2 ridge curve
    Id                  int      // own identity space
    ParentSpurId        int      // frozen primary SpurCurve.Id
    MassifIndex         int      // copied from the parent
    FlankSign           int      // copied from the parent
    Side                int      // +1 / -1: which half of the parent strip it descends into,
                                 //          in the massif's arc direction
    AttachT             float    // where it leaves the parent, in the PARENT's own transverse depth
    AttachPoint         Vector2  // world XZ, the parent's curve evaluated at AttachT
    ParentInterval      Vector2  // the [t0,t1] of parent depth this secondary commands
    Points              Vector2[]// world XZ, parent first, toe last
    Cumulative          float[]
    LengthMeters        float
    TargetReachMeters   float
    ReachMeters         float
    ReliefBudgetMeters  float    // the structural tier, metres of relief. Metadata, never a height.
    ParentReliefBudgetMeters float
    Termination         SecondaryTermination   // Toe / Valley / Strip

SecondaryStrip                   // the child partition cell - NOT a corridor
    ParentSpurId        int
    Side                int
    ParentInterval      Vector2  // [t0,t1] along the parent's own depth parameter
    LowerSecondaryId    int      // -1 at an end of the parent
    UpperSecondaryId    int

SecondaryRidgeSkeleton           // the Stage 3D result object
    Seed                int
    Settings            RidgeSettings
    Primary             RidgeSkeleton        // held read-only, never written
    Spurs               IReadOnlyList<SecondarySpur>
    Strips              IReadOnlyList<SecondaryStrip>
    Stats               SecondaryRidgeStats
    ChildrenOf(int primarySpurId) -> IReadOnlyList<int>
```

**Parameterising the attachment by the parent's own depth `t` is what makes the domain rule
structural rather than a world-space invention.** `t ∈ (0,1)` evaluates on the parent's polyline,
which ends at the parent's toe, which is inside the flank by `PrimarySpurBuilder.TryBuildSpur`'s
existing reach bound. The apron is outside the domain by construction, with no new termination rule
and no world-space test.

## 4. Explicit "no corridors" rule

**Stage 3D produces no `ValleyCorridor`.** `SecondaryStrip` is a partition cell and must never be
converted into one. Secondary spurs must not be passed to, or inserted into:

- `PrimarySpurBuilder.AddValleys` — the only corridor factory in the codebase;
- `RidgeSkeleton.Spurs` or `RidgeSkeleton.Valleys`;
- `ValleyNetwork`, `ValleyTopology`, `ValleyTrajectoryNetwork`;
- Slice A, B, B2 or C.

This matches the design's own Stage 3D output line — *"depth-2 curves and their strips"* — which
names curves and strips and not corridors.

### 4.1 Every route by which a secondary could leak in

Traced through the current code. Each is a boundary Stage 3D must not cross.

| # | route | what breaks |
|---|---|---|
| 1 | appending to `RidgeSkeleton.Spurs` | `RidgeSkeleton.Build`'s stats loop runs over the whole list: `SpurCount`, `SpurPointCount`, `TotalSpurLengthMeters`, `Min`/`MaxSpurLengthMeters`, `Min`/`MaxStripArcMeters`, `SelfIntersectingSpurs`, `TruncatedSpurs` all change. |
| 2 | the same, via `CrossingSpurPairs` | that loop is `O(n²)` over **all pairs in the world**. A secondary touches its parent at its attachment, so `SpurCurve.Crosses` — which tests every segment pair with no attachment exemption — would report crossings that are by construction correct attachments. |
| 3 | `ValleyTrajectoryBuilder.BoundArc` / `FloorArcAt` | index `spurs[corridor.LowerSpurId]` and call `ArcAt(t)`. A secondary reaching here returns a crest arc it does not have, silently relocating a frozen valley floor. |
| 4 | `ValleyTrajectoryBuilder.Cone` / `TerminalTangent` | index `spurs[spurId]` for the continuation cone. |
| 5 | `ValleyNetwork.BuildToeEvents` | reads `ridges.Spurs[spurId]` for `AttachS`, `ReachMeters`, `FlankSign`, `Toe` — all crest-anchored. |
| 6 | `PrimarySpurBuilder.AddValleys` | computes `firstSpur = spurs.Count - builtCount`, positional arithmetic over the shared list. Any interleaving during the build misassigns corridor spur ids. |
| 7 | `RidgeSkeleton.SpursOn(massif, flank)` | returns both tiers mixed to any caller expecting primaries. |
| 8 | **`TerrainGenValleyTrajectoryTests` test 23** | iterates `skeletons[w].Spurs` asserting no primary trajectory crosses any spur. Appending secondaries silently widens a **frozen Stage 3C test** to cover depth-2 geometry, without anyone editing it. |

Route 8 is the subtle one: it would not fail loudly, it would change what a frozen test means.

### 4.2 The separation is already clean at the call sites

No entry point into Stage 3C accepts anything but frozen types:

- `ValleyNetwork.Build(RidgeSkeleton)` and `(RidgeSkeleton, RidgeSettings)`
- `ValleyTopology.Build(ValleyNetwork)` and `(ValleyNetwork, RidgeSettings)`
- `ValleyTrajectoryNetwork.Build(ValleyTopology, Stage1Result)` and `(ValleyTopology, RidgeSettings, Stage1Result)`

A `SecondaryRidgeSkeleton` has no way in. **Implementation can proceed without modifying Stage 3C.**

## 5. Secondary-spur domain and termination

- **Domain:** the parent primary spur's strip, on one side of the parent. Expressed as the parent's
  own depth interval `ParentInterval ⊂ (0,1)` and the arc span between the parent's line
  `parent.ArcAt(t)` and the corresponding edge of `parent.StripInterval`.
- **Termination:** at its toe, inside that domain. It inherits no `ValleyTermination`, because a
  `SpurCurve` has no trajectory continuation state and `ValleyTrajectoryBuilder.Continue` is
  corridor-only.
- **No continuation.** A secondary never enters the apron, because the apron is not in its domain.
- **Reach bound**, following the existing shape of `PrimarySpurBuilder.TryBuildSpur`
  (`FlankHalfWidth · ReliefScale · reachJitter`): the lateral room from the parent's line to the strip
  edge at `AttachT`, scaled by the same relief factor and jitter. It must also stop short of the
  primary valley trajectory that floors that half-corridor — a rib dies at the valley floor, it does
  not jump it.
- **Height:** the design's existing tier rule, *"each level must differ from its parent by ≥ 2.5× in
  height"* (§5, line 249). **The 2.5 ratio is an existing constant**; secondary spacing and reach
  should be derived from the primary values through it rather than from new settings fields.

## 6. Crossing baseline and definition

**Baseline:** the eight Stage 3C terminal-apron trajectory crossings, enumerated in
`PROCEDURAL_TERRAIN_STAGE_3D_PREFLIGHT.md` §2. Stage 3D neither inherits blame for them nor is
required to remove them.

**Structural domain for Stage 3D** = the parent strip, as defined in §5. The terminal apron is
outside it. For reference, in the canonical worlds every trajectory leaves its own structural
ownership 140–260 m past the massif toe and all eight crossings sit 300–660 m past it, at
`MassifOwnerKind.BeyondReach`.

**"No new crossings" means:** Stage 3D introduces no structural crossing involving a newly created
secondary ridge curve, inside the structural domain. The test contract must cover at minimum:

| class | rule |
|---|---|
| secondary × primary | no intersection, **except the declared attachment to its own parent** |
| secondary × secondary | no intersection |
| secondary × primary valley trajectory | no intersection |
| secondary self-intersection | none |

The exemption wording is not new: the design's own invariant table already reads *"**No ridge
crossing** — No two ridge curves intersect except at a declared attachment"* (line 571).

## 7. RNG and determinism

- New stream ids, declared on the Stage 3D builder, never on `PrimarySpurBuilder`. Suggested:
  `terrain.ridge.secondary.station` and `terrain.ridge.secondary.shape`.
- **Fork per parent spur, not per flank:** `worldSeed.Fork(stream, parent.Id, sideKey)`.
  `SpurCurve.Id` is globally unique and frozen, so each parent draws from a stream of its own. This is
  strictly stronger than Stage 3B, which forks once per flank and therefore has all spurs on a flank
  sharing one stream in order — there, adding a spur shifts every later one. Per-parent forking makes
  the secondary layer order-independent.
- Never `UnityEngine.Random`; `System.Random` from the fork only, as everywhere else in the generator.
- **Prefer deterministic subdivision.** The design is explicit that the draw never decides topology:
  *"how many spurs, where they attach and which strips exist come from spacing and geometry, not from
  the draw"* (line 353). Counts and attachment positions come from the spacing rule; only drift and
  reach jitter come from the fork.
- Stage 3D reads no Stage 3A/3B/3C stream and cannot perturb one, because a fork is a function of the
  world seed and the stream id, not a shared cursor.

## 8. Identity

- Secondary spurs live in `SecondaryRidgeSkeleton.Spurs`, a **separate collection with its own
  identity space**. `SecondarySpur.Id` indexes that list and nothing else.
- Primary ids and ordering are frozen. Nothing is inserted into `RidgeSkeleton.Spurs`.
- In diagnostics and test messages, render a secondary as `S{ParentSpurId}.{Id}` so no reader can
  confuse it with a primary `S{Id}`.

## 9. Data flow

```
  Stage 2B massifs ─┐
                    │   (frozen, read-only)
  Stage 3A ownership┤
                    ▼
        RidgeSkeleton  ── .Spurs   primary SpurCurve[]   (ids frozen)
         (Stage 3B)    ── .Valleys ValleyCorridor[]      (set frozen)
              │
              ├──────────────────────────────► Slice A/B  ValleyNetwork
              │                                      │
              │                                      ▼
              │                                ValleyTopology   (B2)
              │                                      │
              │                                      ▼
              │                            ValleyTrajectoryNetwork  (Slice C)
              │                                      │
              │                                      ▼
              │                              8 baseline crossings
              │                                (recorded, untouched)
              │
              │  read-only: parent SpurCurve, its StripInterval,
              │             the massif, and the primary trajectories
              ▼
   SecondarySpurBuilder.Build(primary: RidgeSkeleton,
                              trajectories: ValleyTrajectoryNetwork,   // read-only, for the
                              settings: RidgeSettings,                 // floor-crossing bound only
                              worldSeed: IWorldSeed)
              │
              ▼
   SecondaryRidgeSkeleton
        .Spurs   SecondarySpur[]
        .Strips  SecondaryStrip[]
        .Stats
              │
              ▼
   Stage 3D tests + a new diagnostic dump
```

**`ValleyTopology` and Stage 3C are not re-entered.** The arrow into `SecondarySpurBuilder` is the
only one, it points one way, and the three Stage 3C entry points accept no Stage 3D type (§4.2). The
read-only `ValleyTrajectoryNetwork` argument exists solely so a rib can be bounded by the valley
floor it dies into; it is never written and never rebuilt.

## 10. Tests Stage 3D must eventually provide

1. **Depth cap** — no curve has a parent that itself has a parent. Depth 2, hard.
2. **Domain containment** — every sample of every secondary lies inside its parent's strip, and its
   attachment depth is strictly inside `(0, 1)` of the parent.
3. **No apron entry** — no secondary sample lies outside `MassifOwnerKind.Massif` ownership of its own
   massif. (Implied by 2; asserted independently so the two can disagree audibly.)
4. **Crossings**, the four classes of §6.
5. **Tier separation** — ≥ 2.5× against the parent. See the ambiguity in §11.2.
6. **Partition** — the secondary strips of one parent side partition that side's parent interval
   exactly, with `secondaries + 1` cells, mirroring the Stage 3B partition test.
7. **Determinism** — same seed, identical secondary geometry, ids and ordering, bit for bit.
8. **Stage 3C immutability** — with Stage 3D built, the primary `RidgeSkeleton`, `ValleyNetwork`,
   `ValleyTopology` and `ValleyTrajectoryNetwork` are identical to the same pipeline built without it;
   the `crossings=` count, confluence count and every trajectory length are unchanged on the six
   massif-bearing seeds.
9. **Identity separation** — no secondary appears in `RidgeSkeleton.Spurs`; no corridor exists whose
   bounding spur id refers to a secondary.
10. **Stream isolation** — building Stage 3D does not change any Stage 3B draw; asserted by 8.

## 11. The three ambiguities, resolved

All three were decided by the project owner before implementation. They are no longer open.

### 11.1 Corridors are forbidden — RESOLVED

`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` line 244 says a primary spur's strip is partitioned "by the
same rule one level down". In Stage 3B that rule has two halves: place spurs on stations, **and**
create one `ValleyCorridor` per gap. **Stage 3D repeats only the first half.**

It repeats: station placement, secondary spur construction, child-strip partitioning.
It does not repeat: valley corridor construction, `ValleyNetwork`, `ValleyTopology`,
`ValleyTrajectoryNetwork`.

**There are zero depth-2 `ValleyCorridor`s.** The corridor set remains exactly the frozen Stage 3B/3C
set. `SecondaryStrip` is the complement cell at depth 2 and is a different type precisely so it cannot
be mistaken for one.

### 11.2 Skeleton-only, no `H3` coupling — RESOLVED

Stage 3D does not modify `H3`. It does not carve, does not paint, does not touch `Stage3Field` or
`CarveField`, and introduces no secondary geometry into Stage 3C.

The design's Stage 3D test line — "tier separation measured from the field" — is **superseded**.
Spurs do not enter the height field at all: `CarveField.cs` and `Stage3Field.cs` contain no reference
to `SpurCurve` or to any spur list, and ridges are implicit, being what is left between carved
corridors. Measuring a rib from `H3` is therefore not a stricter test, it is an impossible one.

**Tier separation is measured from the structural relief budget carried by the Stage 3D geometry**,
`SecondarySpur.ReliefBudgetMeters` against `SecondarySpur.ParentReliefBudgetMeters`. That budget is
metadata describing the tier. Nothing in Stage 3C reads it, and no coupling from `SecondarySpur` into
terrain height generation was introduced to satisfy the old wording.

### 11.3 Termination at the valley boundary — RESOLVED

A rib descends through its parent's strip towards the primary valley bounding that strip. It may
terminate at that valley's boundary and must never enter it. `FinalValleyTrajectory.HalfWidthMeters`
is the corridor's own scale, and the rules are:

- no rib sample lies inside the corridor;
- no rib crosses the trajectory centre line;
- the toe may sit on the boundary, within the existing geometric tolerance.

**One finding must be recorded, because it changes how the rule had to be written.**
`FinalValleyTrajectory.HalfWidthMeters` is `CorridorHalfWidthMeters`, which is *half the arc distance
between the corridor's two bounding spurs*. The distance from a parent spur's line to the adjacent
valley centre line is that same half-width, by construction — the floor is the mid-line of the two
spurs. **Using `HalfWidthMeters` as an exclusion radius around the centre line therefore consumes the
entire room a rib has, leaving exactly zero.** It is not a conservative bound; it is a degenerate one.

What is used instead is the tolerance that already exists for this exact question:
`RidgeSettings.SpurSampleSpacingMeters`, the resolution the whole skeleton is defined at, and the same
quantity Stage 3C already uses as `ValleyTrajectoryBuilder.MergeDistanceMeters` on the stated grounds
that "two floors closer than this are the same floor". A rib stops one sample short of the centre
line. No new constant was introduced, and `HalfWidthMeters` still serves as the corridor scale the
room is measured against.

---

## 12. What was built

| file | role |
|---|---|
| `Assets/Scripts/World/TerrainGen/SecondaryRidgeSkeleton.cs` | `SecondaryTermination`, `SecondarySpur`, `SecondaryStrip`, `SecondaryRidgeSkeleton`, `SecondaryRidgeStats` |
| `Assets/Scripts/World/TerrainGen/SecondarySpurBuilder.cs` | the builder, its two streams and the tier ratio |
| `Assets/Scripts/Editor/TerrainGenSecondarySpurDump.cs` | the diagnostic, identity rendered `S{parent}.{child}` |
| `Assets/Scripts/Editor/Tests/TerrainGenStage3DTests.cs` | 15 tests |

Nothing in the frozen Stage 3C commit was touched.

### 12.1 Construction, in order

1. Parents are walked in id order; sides in the fixed order −1 then +1, so identity is a function of
   the world and not of iteration.
2. Stations are placed along the parent's own realised depth by the same shape of rule the flank uses:
   the domain is walked, the local room consulted at each step, the count taken from the spacing rule
   alone, and the draw only nudges a station inside its own gap. Stations are strictly interior.
3. The rib leaves the parent across it, along the crest direction, straight in plan.
4. Its reach is the lesser of the tier reach (`parent.TargetReachMeters / 2.5`, jittered by the
   existing `SpurReachJitter`) and the room that exists (the arc to the strip edge, or the distance to
   the valley centre line, less one sample).
5. Its budget is `massif.CrestAt(parent.AttachS) / 2.5 / 2.5` — the crest, one tier down to the
   parent, one tier down again to the rib. Depth 2 spends both levels.
6. Every candidate is checked: self-intersection, strip containment, structural ownership,
   crossing a primary ridge (except the attachment segment on its own parent), crossing another rib,
   crossing or entering a primary valley. A failure drops the candidate once and records the reason.
   There is no retry loop.
7. The surviving ribs partition the side: N ribs leave N+1 cells, contiguous over `(0, realised)`.

### 12.2 Measured, canonical seeds

| seed | primary spurs | ribs | child strips | rejected | reach | tier budget |
|---|---|---|---|---|---|---|
| 110201 | 7 | 34 | 48 | 21 | 20–106 m | 12.0–41.2 m |
| 424242 | 7 | 42 | 56 | 9 | 20–95 m | 14.7–42.0 m |
| 20260905 | 9 | 57 | 75 | 12 | 21–121 m | 15.5–46.2 m |
| 7 | 7 | 35 | 49 | 14 | 22–106 m | 18.9–42.7 m |
| 101010 | 5 | 14 | 24 | 31 | 22–48 m | 24.4–42.8 m |
| 20261231 | 5 | 28 | 38 | 5 | 21–129 m | 18.7–43.4 m |
| 1337, 256, 777 | 0 | 0 | 0 | 0 | — | — |

Every rejection is either `room` (the room left under one sample) or `valley` (the rib would have come
within one sample of a centre line). **Zero rejections for crossing a primary ridge, crossing another
rib, leaving the strip, leaving the structural domain, or self-intersection** — the construction is
right by design and the checks confirm it rather than repair it.

---

## 13. Documentation conflict left unresolved, deliberately

`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` is **inside the frozen Stage 3C commit**. Editing it to carry
the §11.1 and §11.2 wording would break the standing requirement that every Stage 3C file remain
byte-identical, which is checked at the end of every pass.

The corrected wording is therefore recorded here and not applied there. When the freeze is
deliberately lifted for a documentation pass, line 244 should read:

> **4 — secondary spurs.** Each primary spur's own strip is partitioned one level down by the same
> *station and strip* rule, using the primary spur as the parent divide. A secondary spur is shorter,
> lower and narrower by the same ratio the tier table uses between levels. The complement cells are
> strips, not corridors: depth-2 produces no `ValleyCorridor`, because the corridor set is what the
> primary valley topology is built on and it is frozen at depth 1.

and the Stage 3D test line (684) should read "tier separation measured from the structural relief
budget" rather than "from the field".

---

## 14. Verdict

**Implementation proceeded without modifying Stage 3C.** Three new types, one builder, one diagnostic
and one test suite, all additive. The three Stage 3C entry points accept no Stage 3D type, the carve
reads no spur list, and the frozen commit is untouched.
