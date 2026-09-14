# Stage 3E Preflight — Structural Basins

Read-only investigation. No Stage 3E code exists and none is written here. Stage 3C remains frozen at
`7c882c1`; Stage 3D is the corrected secondary-ridge layer.

---

## 1. Stage 3D final acceptance

| # | claim | evidence |
|---|---|---|
| 1 | Stage 3C files byte-identical | `git diff HEAD` over all 141 committed files: **0** |
| 2 | Stage 3C tests keep their semantics | no Stage 3C test file edited; suites report 13/14/14/25 as before |
| 3 | Stage 3D tests pass | **18/18** |
| 4 | zero `ValleyCorridor` creation | `new ValleyCorridor` in Stage 3D sources: **0** |
| 5 | Stage 3D does not enter 3A/3B/B2/C | no `ValleyNetwork.Build`, `ValleyTopology.Build` or `ValleyTrajectoryNetwork.Build` call in the builder; it reads `RidgeSkeleton` and `ValleyTrajectoryNetwork` and writes neither |
| 6 | primary spur geometry unchanged | `Stage3COutputIsBitIdenticalWithAndWithoutStage3D` compares every spur sample |
| 7 | primary valley topology unchanged | same test compares `DownstreamOf`, merge count, ranks, candidate rays |
| 8 | primary trajectory geometry unchanged | same test compares every trajectory point and length |
| 9 | Stage 3B RNG unchanged | `Stage3DDoesNotPerturbStage3BRng`: stations, drifts and reaches identical before and after |
| 10 | secondary geometry deterministic | `Stage3DIsDeterministic`, extended to the new drift, reach and clearance draws |
| 11 | clearance >= `SpurSampleSpacingMeters` | measured minimum **24.1 m** against a 20 m bound; `SecondarySpursMaintainStructuralClearanceFromUnrelatedPrimarySpurs` |
| 12 | inside the parent domain | `SecondarySpursRemainInsideParentStrip`, `SecondaryAttachmentIsInsideParentDepth` |
| 13 | never in the apron | `SecondarySpursDoNotEnterApron` — every sample on `MassifOwnerKind.Massif` of its own massif |
| 14 | no primary-valley crossing | `SecondarySpursDoNotCrossPrimaryValleys`, plus one-sample clearance from every centre line |
| 15 | no self-intersection | `SecondarySpursDoNotSelfIntersect` |
| 16 | the eight apron crossings are the baseline and unmoved | `ApronCrossings` per seed equals `{110201:0, 424242:0, 20260905:3, 7:1, 101010:4, 20261231:0}`, total 8, compared A-vs-B |

Corrective-pass geometry, six canonical seeds: **200 ribs**; rejected 81 room / 16 valley / 5
clearance; bend from chord 0.00 / 0.97 / 4.72 m (0.00 / 2.27 % / 4.81 % of length); actual reach over
tier target 0.172 / 0.551 / 0.999; relief budget ratio 2.50 everywhere; toes within 1 m of the
exclusion bound down from 45 % to **7 %**.

Full EditMode: **535 / 535**, 0 failed, 0 skipped. The frozen Stage 3C baseline was 517 and Stage 3D
adds 18, so the suite is purely additive: no existing set changed its count or its result.

**Stage 3D is COMPLETE and FROZEN for the purpose of Stage 3E.** All sixteen acceptance claims hold.
Nothing is committed.

---

## 2. Existing definition of a structural basin

From `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md`, and only from there.

**§13, line 301** — *"A basin is created only where structure encloses ground"*, in exactly two kinds:

- **intermontane** — *"between two massifs whose spur systems face each other across a corridor that
  has no downhill exit except one gap"*;
- **cirque-like** — *"on the concave side of a strongly bent axis, where two spurs and the crest
  enclose a hollow"*.

*"Both are found by walking the ridge structure, not by looking for low places in a field."*

**§14, line 309** — three structural conditions, all required:

1. the perimeter is ridge structure for at least a stated majority of its length;
2. **exactly one** declared outlet — a gap in the enclosing ridge, at a stated elevation;
3. the floor lies below the outlet by a bounded depth, and the outlet connects to a corridor that
   continues downhill.

**The declared data shape**, line 550:

```
Basin { Id, Boundary[], EnclosingRidgeIds[], FloorElevationMeters,
        OutletPointXZ, OutletElevationMeters, OutletCorridorId }
```

So a basin is **a combination**: a geometric region (`Boundary[]`), a set of structural references
(`EnclosingRidgeIds[]`), two scalars describing the hollow, and one topological link
(`OutletCorridorId`). It is **not** a field, **not** a depression found by scanning heights, and its
elevations are declarations about the structure rather than measurements of `H3`.

---

## 3. Stage 3E responsibility

From the stage table, line 688:

> **Stage 3E — structural basins.** In: 3C. Out: `Basin[]` with declared outlets; corridors linked
> through them. Invariants: basin validity; exactly one outlet; no pit outside a declared basin.
> Tests: every declared basin is found by an independent depression scan of the field, with the outlet
> within tolerance; no undeclared pit exists. Diagnostics: basins (4). Non-goals: no lakes, no water
> surface, no drainage.

Note the stage table says *In: 3C* — written before Stage 3D existed as a separate stage. Stage 3D is
interposed and is read-only to Stage 3E's inputs either way.

---

## 4. Stage 3E non-responsibilities

No lakes, no water surface, no drainage (design non-goals). No rebuild of 3A, 3B, 3C or 3D. No
mutation of primary or secondary spurs, of the primary valley network, or of `H3`. No hydrology: §8 of
the design is explicit that *"Stage 3 must therefore not own basins-as-lakes or reaches"* (line 91),
and that Stage 4's `DepressionFill` / `BasinExtraction` are **read-only consumers** (line 702).

---

## 5. Relationship to primary ridges

A basin perimeter is *"ridge structure for at least a stated majority of its length"* and the two
declared kinds are enclosed by *"two massifs whose spur systems face each other"* and by *"two spurs
and the crest"*. So the perimeter is built from primary spurs plus the massif crest, plus — for the
intermontane kind — the inter-massif divide.

Available, frozen, read-only: `SpurCurve.Points`, `MountainMassif.Axis.Points`, `MacroAxis`,
`InterMassifDivide.Points`, `MassifOwnership`. A basin may **reference** these by id; it must not
alter them.

---

## 6. Relationship to secondary ridges

The design names no role for depth-2 curves in a basin perimeter, and the tier table puts a secondary
two tiers below the crest — far too small to enclose ground a basin is measured in. **A basin
boundary should not use secondary spurs.** A secondary may lie *inside* a basin; it may not be part of
the enclosure. Recording that as a rule rather than leaving it open is the safer reading, and it is
the one the tier table supports.

---

## 7. Relationship to primary valleys

- **May a basin contain a valley trajectory?** Yes, and it must: condition 3 requires the outlet to
  *"connect to a corridor that continues downhill"*, and §4.2's corridor table (line 279) says *"A
  corridor entering a declared basin terminates at the basin's shore; the basin's outlet corridor
  continues downstream."*
- **May a basin boundary cross a primary valley?** Only at the outlet. A perimeter that crossed a
  corridor anywhere else would enclose ground the corridor drains, contradicting the single-outlet
  condition.
- **May it touch the terminal apron?** The design does not say. Both declared kinds are enclosed by
  ridge structure, which exists only inside the structural domain, so a basin should be confined the
  same way. See §15.

---

## 8. Relationship to Stage 3C field invariants — and a gap

Stage 3C's invariant list (line 670) contains *"no closed depression outside a declared basin"*. The
critical question was where "declared basin" comes from today.

**It comes from nowhere. There is no basin producer, no basin type, and no depression scan.**

| trace | finding |
|---|---|
| `Basin` type anywhere in `Assets/Scripts` | **does not exist** |
| `RidgeSettings` | *"the basin thresholds belong to later slices and are deliberately absent"* |
| `Stage3ASkeleton` | *"no basins here, and no placeholder for them"* |
| `Stage3FieldStats` | carries slope bands, isotropy, reachability, incision, hashes — **no depression or pit counter** |
| any terrain-gen test scanning `H3` for pits | **none**; the only depression tests in the tree belong to the separate, untracked hydrology workstream |
| `ValleyTermination` | `Coast`, `Merge`, `Divide`, `Unresolved` — **no `Basin`**, although the design's corridor contract (line 547) specifies `Coast \| Basin \| Merge \| Divide` |

So Stage 3C states an invariant it neither produces the vocabulary for nor tests. That is a
documented gap, not a defect introduced here: the design lists the depression scan under Stage 3C's
*Tests*, and it was not written.

**Consequence for Stage 3E, and it is a hard one.** Linking a corridor to a basin requires either a
`Basin` value in `ValleyTermination` or a `DownstreamId` that can denote a basin — both of which live
in **frozen Stage 3C types** (`ValleyTrajectoryBuilder.ValleyTermination`, `FinalValleyTrajectory`,
`ValleyCorridor`). Stage 3E cannot express *"this corridor ends in a basin"* without either breaking
the freeze or holding the link on its own side. See §11 and §15.

### 8.1 A second finding, material to any future carve work

`Stage3Field` — the only thing that builds `H3` — is constructed in exactly one place,
`TerrainGenStage3CDump.cs:58`, and it builds its skeleton through
`RidgeSkeleton.Build(partition, settings, worldSeed, stage2b.Stage1)`: **the legacy four-argument
overload**, which runs the old `ValleyTrajectoryBuilder.Build` with its own geometric merge test.
`CarveField` then reads `ValleyCorridor.Floor`, which Slice C deliberately never writes.

**The carve field is therefore still cut from pre-Slice-C trajectories**, and no test builds it at
all. Any Stage 3E test of the form "every declared basin is found by a depression scan of the field"
would be scanning a field built from superseded geometry. This must be settled before such a test is
written.

---

## 9. Does Stage 3E modify `H3`?

**Answer: (A) — structural geometry and metadata only.**

The design's Stage 3E line gives *Out: `Basin[]` with declared outlets; corridors linked through
them* — data, not a field. Its test is *"every declared basin is found by an **independent**
depression scan of the field"*, which reads `H3` and does not write it. §4.2 makes the no-pit property
follow from corridor monotonicity in the carve, not from a basin pass.

Against that: a basin floor *"below the outlet by a bounded depth"* cannot appear in `H3` unless
something puts it there, and nothing in the current carve makes hollows. Either the basins are
declarations that the existing carve happens to satisfy, or a later slice must cut them. The design
does not say which. **Reported as an ambiguity (§15.2), not resolved here.**

---

## 10. Does Stage 3E create topology?

Partly, and the part matters.

- **Classification / partition of existing space** — yes: a basin is a region of ground the existing
  structure encloses.
- **New geometric boundaries** — yes, but assembled from existing ridge geometry, not drawn fresh.
- **New valleys or corridors** — **no.** The design's Stage 3E *Out* names `Basin[]` and links, not
  corridors.
- **Altered valley topology** — **no.** Slice B2's merges are frozen.
- **New drainage topology** — one link per basin: `OutletCorridorId`, and the corridor-terminates-at-
  a-basin relation. That is drainage *structure*, not drainage *flow*.
- **Inputs for Stage 3F / Stage 4** — yes; §8 makes the basin declarations a check that
  `BasinExtraction` is run against.

So Stage 3E adds a small, declarative topological layer **on its own side of the boundary**, and
changes none of the topology Stage 3C settled.

---

## 11. Proposed data model

Existing types do not fit. `ValleyCorridor` is a strip between two spurs; `SecondaryStrip` is a cell
of a parent spur's depth; neither is an enclosed region. Three new types, mirroring the Stage 3D
pattern that worked:

```
StructuralBasin
    Id                       int
    Kind                     BasinKind        // Intermontane | Cirque
    MassifIndex              int              // Cirque: the massif; Intermontane: the lower id
    SecondMassifIndex        int              // Intermontane only, else -1
    Boundary                 Vector2[]        // closed ring, world XZ
    EnclosingSpurIds         int[]            // primary spur ids on the perimeter
    UsesCrest                bool             // whether the massif crest forms part of it
    FloorElevationMeters     float
    OutletPointXZ            Vector2
    OutletElevationMeters    float
    OutletCorridorId         int              // the primary corridor the outlet feeds
    InletCorridorIds         int[]            // corridors terminating at the shore
    AreaSqm                  float
    RidgePerimeterFraction   float            // condition 1, measured

StructuralBasinBoundary                        // one perimeter arc and what it came from
    SourceKind               BoundarySource   // Spur | Crest | Divide | Outlet
    SourceId                 int
    Points                   Vector2[]

StructuralBasinSet
    Seed                     int
    Settings                 RidgeSettings
    Primary                  RidgeSkeleton                 // read-only
    Secondary                SecondaryRidgeSkeleton        // read-only
    Trajectories             ValleyTrajectoryNetwork       // read-only
    Basins                   IReadOnlyList<StructuralBasin>
    Stats                    StructuralBasinStats
```

**`InletCorridorIds` and `OutletCorridorId` live on the basin, not on the corridor.** That is the
whole point: the design wants `ValleyTermination.Basin` and a `DownstreamId` that can mean a basin,
and both are in frozen Stage 3C types. Holding the link on the basin side expresses the same relation
without touching the freeze. If the project later wants the link on the corridor, that is a
deliberate un-freezing, not something Stage 3E should do quietly.

---

## 12. Determinism and RNG

**Basin placement should be deterministic geometry, with no RNG at all.** §13 says basins are *"found
by walking the ridge structure"*, and §14's outlet rule is *"the lowest gap on its perimeter, chosen
deterministically as the single lowest point of the enclosing structure. Exactly one outlet, by
construction."* Every input — spur polylines, the crest, the divides, the crest height model — is
already fixed by earlier stages.

The design does reserve a stream, `terrain.basin`, for *"which enclosed candidates become basins, and
outlet selection among ties"* (line 521). Two comments:

- **Tie-breaking does not need randomness.** Ties can break on the existing ids, which are stable, and
  a deterministic tie-break is strictly better than a drawn one for a structure Stage 4 must
  independently rediscover.
- If a draw is genuinely wanted for *"which candidates become basins"*, it must be a new fork
  (`terrain.basin`), per massif or per candidate identity, never a Stage 3A/3B/3C/3D stream.

Existing streams, none of which Stage 3E may touch: `terrain.ridge.station`, `terrain.ridge.shape`
(Stage 3B), `terrain.ridge.secondary.station`, `terrain.ridge.secondary.shape` (Stage 3D), plus the
Stage 1/2 streams.

**Recommendation: build Stage 3E with no RNG, and add `terrain.basin` only if a real tie proves
undecidable from existing identity.**

---

## 13. Stage isolation boundaries

Every route by which Stage 3E could mutate something it must not:

| target | route | rule |
|---|---|---|
| Stage 3B skeleton | `RidgeSkeleton.Spurs` / `.Valleys` are `IReadOnlyList`, but `SpurCurve.Points` and `ValleyCorridor` are writable | read only; never write through an element |
| Stage 3B skeleton | `RidgeSkeleton.Build(..., stage1)` — the legacy overload mutates every corridor and runs a second merge test | never call the four-argument overload |
| corridors | `ValleyCorridor.SetTrajectory` | never call |
| Stage 3D skeleton | `SecondarySpur.Points`, `SecondaryRidgeSkeleton.Spurs` | read only |
| `ValleyNetwork` / `ValleyTopology` / `ValleyTrajectoryNetwork` | their `Build` entry points | never call; Stage 3E receives finished instances |
| `FinalValleyTrajectory` | `Points`, `Cumulative` are writable arrays | read only |
| `Stage3Field` / `H3` | `Stage3Field.Build` | never call from Stage 3E |
| corridor set | `PrimarySpurBuilder.AddValleys` | never call; **zero basin corridors** |
| frozen tests | appending basins to any frozen collection would silently widen a frozen test, as diagnosed for Stage 3D | keep basins in their own collection |

Intended one-way flow:

```
RidgeSkeleton (3B, frozen) ─┐
ValleyTrajectoryNetwork (3C, frozen) ─┤
SecondaryRidgeSkeleton (3D, frozen) ─┤   all read-only
MountainMassif / MacroAxis / InterMassifDivide ─┤
                                      ▼
                      StructuralBasinBuilder.Build(...)
                                      │
                                      ▼
                            StructuralBasinSet
                                      │
                                      ▼
                       tests + diagnostics  (and, later, Stage 4 as a read-only consumer)
```

---

## 14. Proposed tests

1. **Deterministic placement** — same seed, identical basins, ids, boundaries, outlets, bit for bit.
2. **Closure** — every boundary is a closed ring, non-self-intersecting, positive area.
3. **Perimeter validity** — `RidgePerimeterFraction` at or above the stated majority, and every
   `EnclosingSpurIds` entry is a real primary spur id.
4. **Exactly one outlet** — one `OutletPointXZ` per basin, on the boundary, and the lowest point of
   the enclosing structure.
5. **Floor below outlet** — `FloorElevationMeters < OutletElevationMeters`, by a bounded depth.
6. **Outlet connects downhill** — `OutletCorridorId` is a real corridor whose geometry leaves the
   basin.
7. **No illegal ridge crossing** — a boundary crosses no primary spur except where it follows one, and
   crosses no secondary spur.
8. **No illegal valley crossing** — a boundary crosses a primary trajectory only at the outlet.
9. **Domain** — every boundary point is inside structural ownership; no basin reaches the apron
   (pending §15.3).
10. **Containment** — inlet corridors actually end inside the basin; the outlet corridor starts there.
11. **Stage 3C immutability** — build with and without Stage 3E and compare spurs, corridors,
    `ValleyNetwork`, `ValleyTopology`, trajectories, confluence count and the eight apron crossings.
12. **Stage 3D immutability** — the same for the secondary skeleton.
13. **No new corridors** — `RidgeSkeleton.Valleys` unchanged; `new ValleyCorridor` count zero.
14. **No hydrology coupling** — Stage 3E references no hydrology type and no `Stage3Field`.
15. **RNG isolation** — Stage 3B and 3D draws unchanged; no shared stream consumed.
16. **No undeclared depression** — deferred; see §15.1, because it needs a field this stage does not
    build and a scan that does not exist.

---

## 15. Open ambiguities

Four. None should be resolved by inventing behaviour.

**15.1 The Stage 3C depression scan was never written, and the field it would scan is stale.**
The design lists it under Stage 3C's tests; no terrain-gen test builds `Stage3Field` at all, and the
only code that does is a diagnostic that builds its skeleton through the legacy overload, so the carve
is cut from pre-Slice-C trajectories. Before test 16 above can mean anything, someone must decide
whether `Stage3Field` is rewired to `ValleyTrajectoryNetwork` — which is a change to how `H3` is
produced, not a Stage 3E matter.

**15.2 Whether a basin ever becomes a hollow in `H3`.**
Condition 3 requires a floor below the outlet. Nothing in the current carve makes hollows, and Stage
3E's declared output is data. Either basins are declarations the existing surface already satisfies,
or a later slice cuts them — and if the latter, that slice, not Stage 3E, owns it. The design does not
say.

**15.3 Whether a basin may touch the terminal apron.**
Both declared kinds are enclosed by ridge structure, which exists only inside the structural domain,
so confinement is the natural reading — but the design never states it, exactly as it never stated an
apron contract for trajectories. Same gap, one stage later.

**15.4 The intermontane kind cannot occur on the canonical seed set.**
Measured: **every canonical seed carries exactly one massif** (110201, 424242, 20260905, 7, 101010,
20261231 all report `massif=0` only; 1337, 256 and 777 carry none). An intermontane basin needs two
massifs facing each other across a medial corridor, so that half of §13 has no instance to build or
test against. The cirque kind does have candidates — the crest line through the spur stations turns
71.9° over 743 m on 20260905 (radius ≈ 590 m), 19.9° on 110201 and 10.8° on 101010, while 424242, 7
and 20261231 are effectively straight (radius 15–39 km).

**So Stage 3E may legitimately declare zero basins on three of the six canonical worlds, and can
never exercise the intermontane path there at all.** Either the seed set gains a two-massif world, or
the intermontane kind is implemented against a synthetic fixture and left unexercised on canonical
seeds. That decision should be made before implementation, not discovered during it.

---

## 16. Recommended implementation sequence

1. **Settle §15.2 and §15.4** — whether basins are declarations only, and how the intermontane kind is
   to be exercised. Both change what gets built.
2. **Slice E1 — candidate enumeration.** Walk the ridge structure for enclosed ground; produce
   candidates with their perimeter arcs and provenance. No outlets, no validity, no elevations.
   Tests: closure, provenance, determinism, immutability of 3B/3C/3D.
3. **Slice E2 — validity and outlets.** Apply §14's three conditions; pick the single lowest gap
   deterministically; attach `OutletCorridorId` and `InletCorridorIds` on the basin side. Tests 3–6,
   10.
4. **Slice E3 — diagnostics.** The design's *"basins (4)"* plan view plus a text report, in the shape
   the Stage 3C and 3D diagnostics already take.
5. **Leave test 16 out** until §15.1 is decided. A test that scans a field built from superseded
   geometry would be worse than no test.

---

## 17. Final design resolution

Decisions 1–12 below are settled by the project owner and are not open. §17.2 onward answers the two
technical questions that had to be resolved before E1.

### 17.1 The twelve decisions, recorded

1. **Structural and declarative only.** No `H3`, no `Stage3Field`, no `CarveField`, no carving, no
   painting, no rebuild of 3C, no change to primary valleys, primary spurs or secondary spurs, no
   `ValleyCorridor`, no hydrology, no lakes, no drainage flow. Output is a standalone
   `StructuralBasinSet`.
2. **Floor semantics.** A declared structural floor, derived from the existing elevation model, never
   a measurement or mutation of `H3`. `FloorElevationMeters < OutletElevationMeters` stays a
   structural validity condition; independent field validation is deferred.
3. **Depression scan deferred.** `NoUndeclaredDepression` and any equivalent `H3` scan are out of
   scope. Recorded as architectural debt in §17.9.
4. **Domain.** A basin lies entirely inside the structural domain. It may not enter, touch, or use the
   terminal apron.
5. **Secondary ridges are not enclosure.** Never in `EnclosingSpurIds`, never in perimeter provenance,
   never in the enclosure calculation. They may lie inside a basin.
6. **Intermontane.** The canonical seed set does not change and world generation is not altered to
   manufacture a two-massif world. Production returns zero intermontane basins on one-massif worlds.
   A deterministic test-only synthetic fixture exercises that path — see §17.8 and the blocker in
   §17.10.
7. **Cirque.** Canonical worlds exercise it where the axis is curved enough. Zero basins is a correct
   result. Candidates are discovered from geometry, never from low `H3`.
8. **Topology.** Only `OutletCorridorId` and `InletCorridorIds`, held on the basin. No `Basin` member
   added to `ValleyTermination`; no change to `ValleyCorridor`, `FinalValleyTrajectory`,
   `ValleyTopology` or `ValleyNetwork`.
9. **Outlet.** Exactly one, a gap in the enclosing primary ridge structure, the lowest valid one, ties
   broken by stable structural identity, no RNG. It connects to an existing primary corridor that
   Stage 3E does not create.
10. **Validity.** The eight conditions listed in the brief, with the majority threshold taken from the
    design — see the blocker in §17.10.
11. **No RNG** unless a genuine structural tie proves unresolvable from existing stable identity.
    `terrain.basin` is not to be opened merely because the design reserved the name.
12. **Data model.** The standalone `StructuralBasin` / `StructuralBasinBoundary` /
    `StructuralBasinSet` / `StructuralBasinStats`, independent of frozen Stage 3C types.

### 17.2 Floor elevation — RESOLVED

There is a clean structural elevation source and it needs no addition to the codebase.

`Stage2BMassifGenerator.EvaluateFields` composes the Stage 2 surface as exactly this, per lattice
sample:

```csharp
field.Heights[index] = baseField.Heights[index]
                     + uplift.UpliftAt(worldX, worldZ, stage1.SignedDistance[index]);
```

Every term is reachable analytically and publicly:

- `Stage1Result.HeightAt(worldX, worldZ)` — the Stage 1 island surface, bilinear on its own lattice;
- `MassifUpliftModel.UpliftAt(worldX, worldZ, signedDistanceToCoast)` — public, and the model is
  constructible from `new MassifUpliftModel(skeleton.Massifs, massifSettings)`, both of which Stage 3E
  already holds;
- `CoastQuery.SignedDistance(x, z)` — the same signed distance Stage 1 cached.

So the declared source is:

```
StructuralElevationMeters(x, z)
    = stage1.HeightAt(x, z)
    + new MassifUpliftModel(skeleton.Massifs, massifSettings)
          .UpliftAt(x, z, stage1.Coast.SignedDistance(x, z))
```

**This is H2, reproduced rather than re-invented.** It equals the Stage 2 field exactly at lattice
points and interpolates between them. It is pre-carve, so it depends on no valley trajectory — neither
the legacy ones nor Slice C's — and it touches `H3` nowhere. No new terrain-height system is
introduced: every term already exists and is already the authority for Stage 2.

Then:

- **`FloorElevationMeters`** = the minimum of `StructuralElevationMeters` over the basin interior,
  sampled at the world lattice cells whose centres fall inside the boundary ring. The lattice is
  Stage 1's own 1025 × 1025 at 2.734 m — the resolution everything else in the pipeline is defined at,
  so no sampling density is invented.
- **`OutletElevationMeters`** = the minimum of `StructuralElevationMeters` along the perimeter,
  which by §14 of the design is *"the lowest gap on its perimeter, chosen deterministically as the
  single lowest point of the enclosing structure"*. `OutletPointXZ` is that argmin, taken over the
  perimeter samples in boundary order with the first minimum winning, so an exact tie resolves by
  boundary order rather than by a draw.

Condition 3, `FloorElevationMeters < OutletElevationMeters`, is then a comparison of two numbers from
one function, which is what makes it a structural statement rather than a field measurement.

### 17.3 Cirque boundary — RESOLVED

Four ordered segments, every one with explicit structural provenance, no arbitrary edge:

| # | segment | provenance | source |
|---|---|---|---|
| 1 | crest arc, `SInterval.x → SInterval.y` | `Crest` | `MountainMassif.PointAt(s)`, sampled at `SpurSampleSpacingMeters` |
| 2 | the upper spur, crest origin → toe | `Spur` | `SpurCurve.Points`, in order |
| 3 | the outlet gap, toe(upper) → toe(lower) | `Outlet` | the one segment with no ridge under it — this *is* the gap §14 calls the outlet |
| 4 | the lower spur, toe → crest origin | `Spur` | `SpurCurve.Points`, reversed |

**A cirque candidate is therefore exactly one primary corridor**, since a corridor is by construction
the ground between two adjacent spurs over their crest interval. Two consequences fall out for free:

- an outer corridor (`LowerSpurId < 0` or `UpperSpurId < 0`) has no second bounding spur and cannot
  enclose, so it is never a candidate;
- condition 7 — no new valley corridor — holds by construction, because the corridor already exists.
  It is the **outlet corridor**: the hollow sits at its head, and it leaves through the gap.
  `InletCorridorIds` is empty for a cirque.

Enclosure is then decided, not assumed, by two structural tests in order:

1. **convergence** — the gap `|toe(upper) − toe(lower)|` is shorter than the crest span between the
   two attachments. On a straight or convex flank the spurs diverge and this fails immediately, which
   is why a cirque needs *"the concave side of a strongly bent axis"*;
2. **hollow** — `FloorElevationMeters < OutletElevationMeters` by §17.2.

Measured axis curvature, as a proxy through the crest line at the spur stations: 20260905 turns 71.9°
over 743 m (radius ≈ 590 m), 110201 turns 19.9° (≈ 1411 m), 101010 turns 10.8° (≈ 947 m), while
424242, 7 and 20261231 are effectively straight (15–39 km). So three canonical worlds carry plausible
cirque candidates and three do not.

### 17.4 Intermontane boundary — BLOCKER, see §17.10

The design gives the enclosure *condition* (line 433): *"Where the medial corridor is enclosed at both
ends — by the two massifs converging, or by a spur from each reaching across — the enclosed ground
becomes an intermontane basin candidate"*, with a sketch of two ridges, a basin and one outlet.

The two **end** closures are derivable: a spur from massif A and a spur from massif B reaching towards
each other, joined across the gap between their toes. Both segments are `SpurCurve.Points`, and
`Stage3ASkeleton.Divides` supplies the `InterMassifDivide` running between the massifs.

**What the design never states is what bounds the two sides.** Between the two end closures, the basin
is flanked by the two massifs, and the only structural lines available there are:

- the line through consecutive spur toes — but the stretches between toes have no ridge under them, so
  each would be a gap, and a perimeter with many gaps contradicts condition 2's *exactly one* outlet;
- the flank-foot offset curve from `FlankHalfWidth` — structural, but not ridge structure, so it
  cannot count towards condition 1's ridge majority.

Every way of closing the sides either invents an edge with no provenance or breaks a stated condition.
**This is reported rather than resolved**, per the instruction not to create arbitrary polygon edges.

### 17.5 Domain and apron rule

Every boundary sample and every interior sample must satisfy
`skeleton.Ownership.Ownership.At(x, z).IsStructural` and belong to the basin's own massif (for the
intermontane kind, to one of its two). The apron is `BeyondReach` / `BeyondAxisEnd` / `Unclaimed`
ground and is excluded by that test. For a cirque this holds by construction — the crest and both
spurs are inside the massif, and the outlet gap spans between two spur toes, which are themselves
inside — but it is asserted independently so the two can disagree audibly.

### 17.6 Relation to primary valleys

- A basin **contains** the corridor whose head it encloses; that corridor is its outlet.
- A boundary crosses a primary trajectory **only at the outlet gap**. Anywhere else would enclose
  ground the corridor drains, contradicting the single outlet.
- The relation is recorded on the basin (`OutletCorridorId`, `InletCorridorIds`) and nowhere else. No
  frozen Stage 3C type learns about basins.

### 17.7 Data model

As proposed in §11, with `BoundarySource` now fixed to `{ Crest, Spur, Divide, Outlet }` by §17.3 and
§17.4, and `RidgePerimeterFraction` computed as the fraction of perimeter length whose provenance is
`Crest`, `Spur` or `Divide`.

### 17.8 Synthetic intermontane fixture strategy

Test-only, never reachable from production generation, and it must not alter the canonical seeds.
Build a `Stage3ASkeleton` directly through its public constructor from two hand-placed
`MountainMassif` instances whose axes face each other across a medial gap, then run
`RidgeSkeleton.Build(partition, settings, worldSeed)` — the three-argument overload — so the spur
systems are the real builder's output rather than hand-drawn curves. `Stage3ASkeleton.Divides` then
carries a real `InterMassifDivide`.

That gives a genuine two-massif world to exercise candidate enumeration against. It does **not**
unblock §17.4: the fixture can produce the geometry, but there is still no stated rule for what bounds
the basin's sides.

### 17.9 Deferred debt — the authoritative field and the depression scan

Recorded so it is not lost. The chain that must exist before any `NoUndeclaredDepression` test means
anything:

```
frozen Slice C trajectories
         ↓
authoritative carve field
         ↓
H3
         ↓
independent depression scan
```

Today none of that chain is in place. `Stage3Field` is built in exactly one place,
`TerrainGenStage3CDump.cs:58`, through the legacy `RidgeSkeleton.Build(..., stage1)` overload, so its
carve is cut from pre-Slice-C trajectories; no test builds it; and `Stage3FieldStats` carries no
depression counter. **That rewiring is its own task and is not Stage 3E.** Stage 3E must not touch
`Stage3Field` on the way past.

### 17.10 Remaining blockers

Two, both reported rather than invented.

**B1 — the perimeter majority has no number.** The design says *"ridge structure for at least a stated
majority of its length"* (line 310) and *"perimeter is ridge structure over the stated majority"*
(line 577), and states the majority nowhere. Grep confirms those are the only two mentions. The
*measurement* is settled — `RidgePerimeterFraction`, §17.7 — so only the threshold is missing. E2
cannot enforce condition 1 until a number is given.

**B2 — the intermontane side boundaries have no provenance.** §17.4. The end closures are derivable;
the sides are not, and every way of closing them breaks a stated condition or invents an edge.

**Neither blocks E1 for the cirque path**, which is fully resolved by §17.2 and §17.3. The
recommended course is to build E1 and E2 for cirques only, leave the intermontane enumerator
unwritten behind its resolved fixture, and return to B1 and B2 as a documentation decision.

---

## 18. Stage 3D final acceptance, recorded

Final and not to be revisited.

- Full EditMode **535 / 535**, 0 failed, 0 skipped.
- Stage 3C + 3D control run **84 / 84**.
- Stage 3C files byte-identical to `7c882c1`.
- Stage 3D sources unchanged by the acceptance pass.
- No unrelated modification touched; the 17 hydrology/terrain-bake files stand as they were.

**Stage 3D is COMPLETE and FROZEN.**

---

## 19. Slice E1 as implemented

### 19.1 Scope

Cirque candidate enumeration only, by the §17.3 construction. Nothing else: no intermontane path, no
validation, no elevation of any kind, no `H3`, no corridor, no hydrology, no RNG.

### 19.2 The candidate, and why it is not a basin

`CirqueCandidate` carries the enclosure and its provenance: identity, massif, flank, source corridor,
the two bounding primary spur ids, the crest interval, the closed ring, four `BoundarySegment` runs,
both toes, the outlet gap length, the crest span, the convergence ratio, area, perimeter and
`RidgePerimeterFraction`.

It carries **no** `FloorElevationMeters`, **no** `OutletElevationMeters`, **no** `OutletCorridorId` as
a validated relation, **no** `InletCorridorIds` and **no** verdict. A test asserts those properties do
not exist on the type, so the distinction cannot erode by someone adding a convenient field: both
missing numbers are exactly what E2 is blocked on, and a half-validated basin would settle them by
accident.

`ConvergenceRatio` and `RidgePerimeterFraction` are **measurements recorded as metadata**, not
thresholds applied. Nothing compares either against anything.

### 19.3 The enumeration rule, exactly

For each primary corridor in id order:

1. skip it if `IsOuter` — an attachment-interval edge is not a divide and cannot enclose;
2. both bounding spurs must exist, be on the same massif and the same flank as the corridor, and have
   at least two points;
3. the crest interval must be non-degenerate;
4. build the ring as four runs, in order: crest `SInterval.x → SInterval.y`, upper spur crest → toe,
   outlet gap upper toe → lower toe, lower spur toe → crest;
5. reject if the ring has fewer than four points, zero area, a self-intersection, or any sample
   outside its own massif's structural ownership.

Shared endpoints are not duplicated: the crest run ends at the upper spur's origin, and the lower
spur's reversed run stops one short of the ring's first point. Candidate ids are enumeration order,
which is corridor order, which is a function of the frozen skeleton alone.

### 19.4 Measured, canonical seeds

| seed | corridors | candidates | outer skipped | rejected | convergent | area range | ridge perimeter |
|---|---|---|---|---|---|---|---|
| 110201 | 9 | 5 | 4 | 0 | 3 | 21 067 – 64 160 m² | 69 – 87 % |
| 424242 | 9 | 5 | 4 | 0 | 4 | 22 254 – 46 570 m² | 70 – 87 % |
| 20260905 | 11 | 7 | 4 | 0 | 4 | 24 856 – 79 249 m² | 70 – 84 % |
| 7 | 9 | 5 | 4 | 0 | 3 | 22 408 – 60 531 m² | 67 – 82 % |
| 101010 | 7 | 3 | 4 | 0 | 2 | 13 509 – 21 981 m² | 74 – 88 % |
| 20261231 | 7 | 3 | 4 | 0 | 1 | 35 829 – 89 842 m² | 68 – 82 % |
| 1337, 256, 777 | 0 | 0 | 0 | 0 | 0 | — | — |

28 candidates, zero rejections, 17 of 28 with their toes closer together than their crest span.
Convergence ratios span 0.50 to 2.23 across the set.

---

## 20. What remains blocked, and one new question

### 20.1 B1 — the perimeter majority still has no number

`"ridge structure for at least a stated majority of its length"` (design line 310, restated line 577)
never states the majority. **E2 cannot enforce validity condition 1 until a number is given**, and no
number is to be chosen by implementation: not 50 %, not 51 %, not 60 %, not two thirds. E1 measures
`RidgePerimeterFraction` and compares it to nothing. Measured range across the canonical set:
**67 % – 88 %**, which is offered as evidence about the geometry and emphatically not as a proposed
threshold.

### 20.2 B2 — intermontane side boundaries still have no provenance

Unchanged from §17.4, and no intermontane enumeration was written. The synthetic two-massif fixture
strategy of §17.8 stands as documented and was not used to invent side boundaries.

### 20.3 B3 — what `OutletElevationMeters` is measured over (new)

§17.2 proposed *"the minimum of `StructuralElevationMeters` along the perimeter"*. That is **not the
same thing** as the design's outlet, and the difference must be settled before E2 rather than by
whichever is easier to write.

The design says the outlet is *"a gap in the enclosing ridge, at a stated elevation"* and *"the lowest
gap on its perimeter, chosen deterministically as the single lowest point of the enclosing
structure"*. But E1 has now made the structure explicit, and the two readings come apart:

- **Reading A — minimum over the whole ring.** Includes the crest and both spurs. Since the ring
  descends from crest to toes, the minimum will essentially always land on the outlet-gap run anyway,
  but it is defined over ridge that is by definition not a gap.
- **Reading B — minimum over the `BoundarySource.Outlet` run only.** The gap is the one stretch with
  no ridge beneath it, which is precisely what the design calls a gap. This is the reading the
  provenance model supports.
- **Reading C — the lowest of several gaps.** The design's *"the lowest gap"* implies there may be
  more than one to choose among. A cirque as constructed has **exactly one** gap run, so for this kind
  the choice is vacuous; the plural only becomes meaningful for an enclosure with more than one
  opening, which is the intermontane case that B2 blocks.

The three agree numerically for a cirque in the common case and disagree in principle. **Reading B is
the one the structure supports**, but it is recorded here as a question for decision, not adopted:
E1 computes no outlet elevation at all.

---

## 21. Files added by E1

| file | role |
|---|---|
| `Assets/Scripts/World/TerrainGen/CirqueCandidate.cs` | `BoundarySource`, `BoundarySegment`, `CirqueCandidate`, `CirqueCandidateSet`, `CirqueCandidateStats` |
| `Assets/Scripts/World/TerrainGen/CirqueCandidateBuilder.cs` | the enumerator |
| `Assets/Scripts/Editor/TerrainGenCirqueCandidateDump.cs` | the diagnostic |
| `Assets/Scripts/Editor/Tests/TerrainGenStage3ETests.cs` | 14 tests |

`BoundarySource` has three members — `Crest`, `Spur`, `Outlet`. There is deliberately no `Divide`:
the inter-massif divide only bounds an intermontane basin, that path is blocked by B2, and a member
nothing produces would be a decision made before it was taken.

---

## 22. Blocker resolution pass

Documentation only. No geometry changed; E1 is untouched apart from one test that pins a definition.

### 22.1 B1 — RESOLVED: strict majority

`"ridge structure for at least a stated majority of its length"` is resolved by the project owner as

```
RidgePerimeterFraction > 0.5
```

**Strict**, not `>= 0.5`: a perimeter exactly half ridge is not a majority ridge.

No configurable setting is introduced. `RidgeSettings` holds no basin field and its own documentation
says the basin thresholds *"belong to later slices and are deliberately absent"*; adding one now would
be inventing a tuning surface for a rule that has a single stated value.

The measured range across the canonical set, **67 % – 88 %**, is diagnostic evidence about the
geometry. The threshold was not derived from it and must not be tuned to it. **B1 is removed from the
blocker list.**

### 22.2 B3 — RESOLVED for cirques: the outlet run only

```
OutletElevationMeters = min StructuralElevationMeters over the BoundarySource.Outlet run
```

Not over the whole ring. The reason is semantic: the design's outlet is *a gap in the enclosing
ridge*, and the `Crest` and `Spur` runs **are** the enclosing ridge, so they are not gaps and cannot
host an outlet. Taking the minimum over the polygon would let enclosing structure supply the outlet's
elevation, which inverts the definition.

A cirque as constructed has **exactly one** `Outlet` run, so the design's *"the lowest gap"* involves
no choice here: there is one gap and it is the outlet. The plural only becomes meaningful for an
enclosure with more than one opening, which is the intermontane case that B2 blocks.

Ties between equal samples break by boundary order, first minimum winning — deterministic, no RNG.

**B3 is removed from the blocker list for the cirque path.** It remains open in principle for
intermontane enclosures, where selecting among several gaps is a real operation, and is subsumed by
B2 until that path exists.

### 22.3 B4 — NEW BLOCKER: "bounded depth" has no bound

The design's third validity condition is not merely `FloorElevationMeters < OutletElevationMeters`.
Line 312 reads: *"its floor lies below the outlet **by a bounded depth**"*. A strict inequality alone
does not satisfy that; an arbitrarily deep hollow passes `<` and fails "bounded".

**Searched, exhaustively, for an authoritative bound:**

| source | result |
|---|---|
| `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` | line 312 states the requirement and no number. Line 559 lists *"basin thresholds"* among what the settings struct **will** own — future tense, no value. Those are the only two mentions. |
| `RidgeSettings` | no basin field of any kind; its own remark is *"the basin thresholds belong to later slices and are deliberately absent - a field nothing reads is a decision made before it"* |
| `WORLD_RELIEF_PRINCIPLES.md` | the tier table and the 2.5 separation rule, both stated as **"Height above local ground"** — positive relief for a landform that stands up. Nothing about a depression's depth. Its own feature list says *"A closed basin with no outlet is a bug unless it is a lake, and we have no lakes in V0 — so no closed basins"* |
| `WorldScale` | no basin constant. `OceanShelfDepthMeters` is the sea floor; `RoadMaxCutDepthMeters` is earthworks |
| terrain-gen settings and constants | `ValleyDepthPerCrest = 0.30` and `CorridorIncisionMeters = 7.2` are **corridor** quantities — how deep a valley is cut — not a bound on an enclosed hollow |
| every Stage 3 design and resolution document | no occurrence of `MaxBasinDepthMeters` or any equivalent |

**No authoritative bound exists. B4 is a blocker.**

The nearest tempting candidate, recorded so it is visibly considered and visibly rejected: the tier
table's T2 band (18–40 m) with the 2.5 separation could be read as bounding a basin's depth. It does
not. The table measures **height above local ground** for a form that rises; a basin is a hollow, and
mapping a positive-relief tier onto a negative one would be inventing semantics, not reading them.

Consequently:

- the bounded-depth validity rule is **not implemented**;
- no value is chosen from the observed candidate geometry;
- E2 cannot be completed until a bound is stated, because condition 9 of the E2 contract has no
  content without one.

### 22.4 ConvergenceRatio — exact semantics

As implemented, and now pinned by a test:

```
ConvergenceRatio = Segments[2].LengthMeters   (the BoundarySource.Outlet run)
                 / Segments[0].LengthMeters   (the BoundarySource.Crest run)
```

Equivalently `OutletGapMeters / CrestSpanMeters`. Both numerator and denominator are the length of the
corresponding boundary run; the gap run is straight by construction, so its length is also the
straight distance between the two spur toes.

- `< 1` — convergent: the toes are closer together than the attachments, the flank is concave here;
- `= 1` — the boundary case;
- `> 1` — divergent: the spurs splay.

Measured across the canonical set: **0.50 – 2.23**, with 17 of 28 candidates convergent. This is
metadata. **No convergence threshold is applied anywhere**, and none is proposed: the design states
none, and the E2 contract does not list one.

### 22.5 B2 — unchanged

Intermontane side boundaries still have no provenance. No intermontane enumeration was written, no
`Divide` provenance was added to the cirque path, no side boundaries were invented, no synthetic
production geometry was introduced and the canonical seeds are untouched. The test-only two-massif
fixture strategy of §17.8 remains documented and unused.

### 22.6 Blocker list after this pass

| id | status |
|---|---|
| B1 — perimeter majority | **RESOLVED**: strict `RidgePerimeterFraction > 0.5` |
| B3 — outlet elevation domain | **RESOLVED for cirques**: minimum over the `Outlet` run only |
| B2 — intermontane side boundaries | **BLOCKED** |
| B4 — bounded basin depth | **BLOCKED** — no authoritative bound exists anywhere in the project |

**E2 cannot proceed.** Conditions 4 and 6 of the E2 contract are now determinate, but condition 9 is
not, and it is a stated validity condition rather than an optional refinement.

### 22.7 The E2 contract, recorded but not implemented

For the cirque path only, once B4 is resolved:

1. closed, non-self-intersecting, positive-area boundary;
2. structural-domain containment;
3. no terminal apron;
4. `RidgePerimeterFraction > 0.5` — §22.1;
5. exactly one outlet gap;
6. outlet elevation from the `Outlet` provenance run — §22.2;
7. floor elevation from the Stage 2 structural elevation model — §17.2;
8. floor below outlet;
9. floor-to-outlet difference within the authoritative bounded-depth rule — **blocked by B4**;
10. outlet connects to the existing primary corridor;
11. that corridor continues downhill by the existing corridor and trajectory semantics;
12. no illegal primary valley crossing;
13. no secondary ridge in the enclosure;
14. no new corridor;
15. no mutation of Stage 3C or 3D;
16. no `H3`, no `Stage3Field`, no hydrology;
17. deterministic, no RNG.

---

## 23. B4 Design Analysis

A decision memo, not an implementation. Nothing in E1, Stage 3C or Stage 3D changed; the one code
addition is a read-only probe used as evidence, referenced by nothing that generates.

The question is narrow and must stay narrow. It is **not** "how deep is the actual `H3` depression" —
that remains deferred with the field rewiring. It is: **what structural rule makes a declared basin's
floor-to-outlet elevation difference bounded**, given that both numbers come from the Stage 2
structural elevation model resolved in §17.2.

### 23.A Existing authoritative sources

Every vertical concept in the project, classified. Classes: **1** applicable to a basin, **2**
positive relief only, **3** valleys/corridors only, **4** carving, **5** unrelated.

| concept | value | what it actually means | class |
|---|---|---|---|
| Tier table T1 / T2 / T3 | 250–530 / 18–40 / 5–12 m | *"**Height above local ground**"* for forms that rise | 2 |
| Tier separation | 2.5× | *"adjacent tiers must differ by at least 2.5× **in height**"* | 2 |
| `CorridorIncisionMeters` | 7.2 m | deepest a corridor may cut below Stage 1, *"**only inside a declared valley corridor**"* | 3, 4 |
| `ValleyDepthPerCrest` | 0.30 | deepest a valley cuts, as a fraction of the local crest | 3 |
| `MassifBlendMeters` | 40 m | *"Saddle height between two culminations, relative to the lower of them"* — a blend width in the uplift model | 2 |
| `MassifSettings.MaxUpliftMeters` | 360 m | ceiling on the massif contribution | 2 |
| `CrestHeightMin/MaxMeters` | 200 / 330 m | massif crest heights | 2 |
| `SecondaryHeightMin/Max` | fractions | secondary culmination height relative to the primary | 2 |
| `CoastalRiseMeters` / `InteriorRiseMeters` | 26 / 30 m | Stage 1 surface rise amplitudes | 2 |
| `SpurReliefFloor` | = `WidthTaperFloor` | how far a spur's **reach** shrinks — lateral, not vertical | 5 |
| `WorldScale.TerrainHeightBudgetMeters` | 700 m | the heightmap's total vertical container | 5 |
| `WorldScale.SeaLevelMeters` | 24 m | the datum | 5 |
| `OceanShelfDepthMeters` / `ShelfDropMeters` | 22 m | how deep the **sea floor** sits below sea level | 5 |
| `MinimumSubmersionMeters` | 1.5 m | how deep open ocean must be | 5 |
| `RoadMaxCutDepthMeters` / `RoadAllowedEarthworkDepthMeters` | 1.5 / 1.0 m | **earthworks** for a road | 5 |
| divide/ownership/sample tolerances | various | horizontal | 5 |

**Nothing is class 1.** There is no concept in the project whose subject is the depth of an enclosed
hollow.

Why the previously-rejected candidates are semantically invalid, confirmed on inspection:

- **T2's 18–40 m band and the 2.5× rule** — the table's own column heading is *"Height above local
  ground"*, and every form it names (massif, plateau, outcrop) rises. Reading a band for a rising form
  as a bound for a hollow is an interpretive step the relief principles never take. Their own feature
  list goes the other way: *"A closed basin with no outlet is a bug unless it is a lake, and we have no
  lakes in V0 — so no closed basins."*
- **`ValleyDepthPerCrest`** — a **corridor** quantity: *"Deepest a valley cuts, as a fraction of the
  local crest height."* A basin is not a valley, and the fraction is justified against the slope
  budget for a cut flank, which is a different geometry.
- **`CorridorIncisionMeters`** — scoped in its own documentation to *"only inside a declared valley
  corridor"*, and it bounds the **carve** relative to Stage 1. Stage 3E carves nothing and its floor
  is a structural declaration, not a cut.
- **`OceanShelfDepthMeters`** — marine, below the datum, nothing to do with a land form.
- **`RoadMaxCutDepthMeters`** — earthworks, a different subsystem entirely.
- **`MassifBlendMeters`** — the nearest structural analogue, because a saddle between culminations is
  the one place the project reasons about ground sitting *below* its neighbours. But it is a blend
  width inside the uplift model, a parameter of how two massif culminations are combined, not a
  validity bound on anything. Naming it here so it is visibly considered and visibly rejected.

**What the design does supply is a method, twice, in two different forms.** Neither is about basins,
and they do not agree on shape:

- **Absolute, derived from the tier table.** *"The bound is derived, not chosen:
  `CorridorIncisionMeters = T2 floor / tier separation = 18 / 2.5 = 7.2 m` … so 7.2 m is the largest
  amplitude that is guaranteed to read as a tier below T2"* (design lines 389–399). This is the
  project already performing the positive-to-negative step once, for an incision, with its reasoning
  written down.
- **Relative to the parent's scale.** `ValleyDepthPerCrest` is deliberately *"of
  `MountainMassif.CrestAt` and not of a number in metres: a valley on a 250 m massif and one on a
  530 m massif are both T1 features and neither is a T2 one"*, with the value *"set against the slope
  budget in WORLD_RELIEF_PRINCIPLES, which is the authoritative constraint and is measured rather than
  assumed."*

So the project knows how to derive a vertical bound, and has done it both ways. It has never done it
for a basin, and the two precedents do not indicate which form a basin's bound should take.

### 23.B Candidate interpretations

**Option A — absolute world-space bound.** `0 < OutletElevation − FloorElevation <= MaxBasinDepthMeters`,
with a new authoritative parameter. Precedent exists in form (`CorridorIncisionMeters`), but the
precedent's *value* was derived from a tier band chosen for that feature, and no tier has been chosen
for a basin. **The design supplies the shape of this option and not its number.**

**Option B — scale-relative bound**, e.g. a fraction of the local crest height, as
`ValleyDepthPerCrest` is. Precedent exists in form. But the fraction itself would be new: the existing
0.30 is justified against the slope budget of a *cut flank*, which is not a basin's geometry, and
nothing in the design maps an enclosure's depth to any existing scale. **The design supplies the shape
and not the mapping.**

**Option C — defer the bounded-depth clause out of Stage 3E.** Note that §14's condition 3 is two
clauses: *"its floor lies below the outlet **by a bounded depth**, and the outlet connects to a
corridor that continues downhill."* Only *bounded* is unspecified; *below the outlet* is specified and
testable today. C would keep the strict inequality as a Stage 3E invariant and defer the bound.

The argument for C is stronger than "Stage 3E does not own `H3`". It is that the bound is **not
load-bearing for the job §14 gives it**. The design's stated purpose for the three conditions is that
*"An arbitrary depression fails all three"* — and an arbitrary depression already fails condition 1
(its perimeter is not ridge structure) and condition 2 (it has no single declared outlet). The depth
bound is not what excludes arbitrary depressions; it is what stops an otherwise-valid enclosure from
being absurdly deep. Whether that can happen at all is an empirical question about the Stage 2
surface, answered in §23.C.

The argument against C is that dropping a stated Stage 3E invariant is an owner's decision, not an
implementer's, and the design lists *"basin validity"* among Stage 3E's invariants without carving out
part of it.

**Option D — the tier-membership reading.** Extend the `CorridorIncisionMeters` method: a declared
basin's depth must read as a tier and not compete with the tier above it. This has real precedent and
is the most defensible *method*. It still does not close, because it requires choosing which tier a
basin is — T3 (5–12 m), T2 (18–40 m) or T1 — and the design never says. **D supplies a method and not
a value.**

### 23.C Consequences

| | data model | E2 | structural-only preserved | new magic number | Stage 4 can reproduce |
|---|---|---|---|---|---|
| **A** absolute | one new `RidgeSettings` field | condition 9 becomes one comparison | yes | **yes**, unless a tier is chosen and it is derived | yes, if published |
| **B** relative | one new fraction, plus a scale to read | condition 9 becomes a comparison against `CrestAt` | yes | **yes** — a new fraction | yes, if published |
| **C** defer | none | condition 9 removed; conditions 1–8 and 10–17 stand | yes | **no** | yes |
| **D** tier membership | one new field, derived not chosen | as A, with the value derived | yes | **no**, if the tier is stated; yes otherwise | yes |

All four preserve Stage 3E's structural-only responsibility: every one of them compares two numbers
that come from the Stage 2 elevation model, and none of them touches `H3`, the carve, or a trajectory.
That is not what separates them.

What separates them is whether the project already contains the answer. **A and B require a number the
design has never stated. D requires a tier choice the design has never made. C requires the owner to
narrow a stated invariant.** Three of the four need an owner decision; the fourth is an owner decision.

### 23.D Recommendation

**B4 remains an owner-level design decision; no implementation should proceed.**

I am not recommending one of A, B, C or D, because the design does not support one. It supports the
*method* for A, B and D — two worked precedents, in two different forms — and it supports neither a
value nor a tier. Picking any of them would be manufacturing a resolution, which is what this pass was
convened to avoid.

What the owner is being asked to decide is one of:

1. **which tier a structural basin belongs to**, after which Option D derives the bound the way
   `CorridorIncisionMeters` was derived and no number is invented; or
2. **a scale to express the depth against**, after which Option B follows the `ValleyDepthPerCrest`
   precedent; or
3. **that the bounded-depth clause is not a Stage 3E invariant**, after which Option C stands and
   condition 9 leaves the E2 contract with the rest intact.

Evidence bearing on that choice is in §23.E. It does not make the choice.

### 23.E Empirical evidence

Measured by `TerrainGenBasinDepthProbe`, read-only, over all 28 cirque candidates on the six
massif-bearing canonical seeds. Elevation is the §17.2 structural formula — Stage 1's surface plus the
gated massif uplift, which is Stage 2. No `H3`, no carve, no trajectory. `Depth = Outlet − Floor`,
with `Outlet` taken over the `BoundarySource.Outlet` run as B3 resolved, and `Floor` the minimum over
the interior lattice cells.

| seed | cand | corridor | outlet (gap run) | floor (interior) | depth | crest span | convergence | area |
|---|---|---|---|---|---|---|---|---|
| 110201 | C0 | V1 | 79.29 | 74.42 | **+4.872** | 162 m | 0.50 | 21 067 m² |
| 110201 | C1 | V2 | 63.46 | 63.07 | **+0.390** | 171 m | 1.23 | 42 419 m² |
| 110201 | C2 | V3 | 62.27 | 62.37 | −0.096 | 147 m | 0.99 | 31 357 m² |
| 110201 | C3 | V6 | 79.35 | 79.46 | −0.116 | 139 m | 2.23 | 56 556 m² |
| 110201 | C4 | V7 | 77.27 | 74.37 | **+2.900** | 287 m | 0.67 | 64 160 m² |
| 424242 | C0 | V1 | 80.54 | 81.43 | −0.894 | 223 m | 0.60 | 46 570 m² |
| 424242 | C1 | V2 | 76.55 | 73.94 | **+2.608** | 176 m | 0.65 | 38 570 m² |
| 424242 | C2 | V5 | 64.58 | 62.12 | **+2.463** | 186 m | 1.26 | 36 106 m² |
| 424242 | C3 | V6 | 61.15 | 61.35 | −0.200 | 181 m | 0.79 | 30 645 m² |
| 424242 | C4 | V7 | 74.67 | 76.05 | −1.386 | 165 m | 0.55 | 22 254 m² |
| 20260905 | C0 | V1 | 75.06 | 71.41 | **+3.642** | 189 m | 0.98 | 33 178 m² |
| 20260905 | C1 | V2 | 70.56 | 70.68 | −0.119 | 221 m | 0.56 | 33 161 m² |
| 20260905 | C2 | V3 | 77.66 | 77.77 | −0.108 | 130 m | 0.86 | 24 856 m² |
| 20260905 | C3 | V4 | 76.75 | 77.88 | −1.132 | 206 m | 0.71 | 36 276 m² |
| 20260905 | C4 | V7 | 67.29 | 66.12 | **+1.175** | 150 m | 2.15 | 59 883 m² |
| 20260905 | C5 | V8 | 65.46 | 65.86 | −0.409 | 231 m | 1.40 | 79 249 m² |
| 20260905 | C6 | V9 | 65.85 | 63.90 | **+1.953** | 217 m | 1.49 | 74 623 m² |
| 7 | C0 | V1 | 77.24 | 79.19 | −1.944 | 171 m | 1.04 | 49 001 m² |
| 7 | C1 | V2 | 97.36 | 94.67 | **+2.697** | 284 m | 0.65 | 60 531 m² |
| 7 | C2 | V5 | 88.68 | 90.29 | −1.611 | 226 m | 0.61 | 29 293 m² |
| 7 | C3 | V6 | 83.05 | 83.19 | −0.142 | 102 m | 2.18 | 26 840 m² |
| 7 | C4 | V7 | 83.26 | 81.16 | **+2.100** | 145 m | 0.82 | 22 408 m² |
| 101010 | C0 | V1 | 62.29 | 64.73 | −2.443 | 67 m | 1.97 | 14 304 m² |
| 101010 | C1 | V2 | 69.63 | 70.63 | −0.994 | 108 m | 0.81 | 13 509 m² |
| 101010 | C2 | V5 | 78.37 | 79.77 | −1.405 | 149 m | 0.51 | 21 981 m² |
| 20261231 | C0 | V1 | 68.11 | 66.48 | **+1.631** | 173 m | 1.10 | 36 549 m² |
| 20261231 | C1 | V2 | 66.07 | 66.67 | −0.605 | 226 m | 0.60 | 35 829 m² |
| 20261231 | C2 | V5 | 64.89 | 64.93 | −0.042 | 292 m | 1.34 | 89 842 m² |

**Headline: 11 of 28 have `Floor < Outlet`; 17 of 28 do not.** The positive depths run 0.39 – 4.87 m,
median ≈ 2.46 m, on massifs whose crests are 200–330 m. That is 0.1 – 2 % of the parent's height.

#### 23.E.1 And all but one of those eleven are a bookkeeping artifact

The probe also records **where on the ring the lowest point sits**. In **all 11** candidates with a
positive depth, the ring's minimum lies on a **`Spur` run — never on the `Outlet` run**.

The reason is a convention in how the runs were tiled. The outlet gap's two endpoints are the two spur
toes; the builder appends the gap from the upper toe exclusive to the lower toe inclusive, so the
**upper toe belongs to the spur run, not to the gap run**. The two toes are not at similar elevations —
on 110201 C0 they are 73.51 m and 99.36 m, 26 m apart — so excluding one of them moves the gap's
minimum a long way.

Recomputing the same 11 depths against the whole-ring minimum instead:

| candidate | gap-run rule | whole-ring rule |
|---|---|---|
| 110201 C0 | +4.872 | **−0.910** |
| 110201 C1 | +0.390 | **+0.020** |
| 110201 C4 | +2.900 | **−1.050** |
| 424242 C1 | +2.608 | **−1.310** |
| 424242 C2 | +2.463 | **−0.620** |
| 20260905 C0 | +3.642 | **−0.850** |
| 20260905 C4 | +1.175 | **−0.660** |
| 20260905 C6 | +1.953 | **−0.800** |
| 7 C1 | +2.697 | **−0.860** |
| 7 C4 | +2.100 | **−0.640** |
| 20261231 C0 | +1.631 | **−0.410** |

**Ten of eleven flip negative. The survivor is +0.020 m**, which is below the interpolation error of
the lattice the elevation is read on.

#### 23.E.2 What this means

**The Stage 2 structural surface does not contain a hollow inside any E1 enclosure.**

That is what the model predicts on inspection. `MountainMassif.Evaluate` takes uplift from the
distance to the **nearest** point of the axis, so a concave pocket — where the axis wraps around you —
is uplifted from two sides and is therefore *high*, not hollow. The surface descends monotonically
outward, and the lowest ground in an enclosure is on its outer edge. There is nothing for a depth
bound to bound.

**This reaches further than B4.** It is not only the *bound* that has no referent on this surface: the
clause `FloorElevationMeters < OutletElevationMeters` does not discriminate either. Under one endpoint
convention it passes 11 of 28; under the other it passes 1 of 28 by two centimetres. A validity
condition whose verdict is decided by which run owns a shared endpoint is not measuring geography.

---

## 24. OWNER DECISION — the bounded-depth clause

### 24.1 Final comparison

| option | what Stage 3E guarantees | new parameter | coupling to future carving | design change |
|---|---|---|---|---|
| **A** absolute bound | enclosure + a depth inside `MaxBasinDepthMeters` | **yes** — one metre value, currently unstated | medium | adds a rule |
| **B** scale-relative bound | enclosure + a depth inside a fraction of an existing scale | **yes** — one fraction, plus the scale | medium | adds a rule and a formula |
| **C** structural below-outlet only | **enclosure**; the physical hollow is downstream's | **no** | low, with explicit downstream ownership | narrows the Stage 3E contract |
| **D** tier-derived bound | enclosure + a depth read off a tier | a **tier choice**, then derived | medium | adds a classification |

### 24.2 Is `Floor < Outlet` still meaningful under C?

Asked directly, and the honest answer is **not on the surface Stage 3E can see today**. §23.E.1 shows
the clause flipping on an endpoint convention at the 0.02 – 1 m level. Keeping it as a Stage 3E
invariant would keep a condition that passes or fails for bookkeeping reasons.

Two ways to hold that, and the difference is worth stating because it changes what E2 does:

- **C-strict** — keep `Floor < Outlet` as a validity condition. Consequence: on the canonical set,
  between 1 and 11 of 28 candidates become basins depending on a convention nobody has ruled on, and
  the convention would have to be ruled on first.
- **C-structural** — Stage 3E guarantees the **enclosure** and the **outlet topology**, and *both*
  the hollow and its bound are deferred to the stage that owns an authoritative field. Consequence:
  conditions 5, 8 and 9 leave the E2 contract together; conditions 1–4, 6, 7 and 10–17 stand
  unchanged, and every one of them is decidable on frozen structure today.

**C-structural is the coherent version.** "The floor is below the outlet" and "the basin is not
absurdly deep" are both statements about a hollow, and neither has a referent until a field exists
that can hold one. Splitting them — deferring the bound while keeping the inequality — keeps the half
that measures nothing.

### 24.3 Compatibility check for C

| against | verdict |
|---|---|
| stage responsibility model | **compatible.** Stage 3E's declared output is *"`Basin[]` with declared outlets; corridors linked through them"* — enclosure and topology, which is exactly what C guarantees |
| Stage 3E structural/declarative | **compatible, and more so.** C removes the one part of the contract that needed a surface to evaluate |
| Stage 4 read-only consumption | **compatible.** §8 wants the declared outlet to be what `BasinExtraction` independently finds as `SpillCell`. That is a comparison of a declared *point*, not of a depth |
| the deferred `H3` / depression-scan debt | **compatible, and it merges with it.** The debt already says an authoritative post-Slice-C field must exist before any depression claim can be checked. The hollow and its bound join that same queue rather than forming a second one |
| Stage 3E does not own carving | **compatible.** Under A, B or D, Stage 3E would assert a depth that nothing in the pipeline produces — a declaration about a hollow no stage cuts |

### 24.4 Recommendation

**Option C, in its C-structural form.**

The recommendation rests on §23.E, not on convenience: the quantity the bound would bound **does not
exist** on the surface Stage 3E is allowed to read, and neither does the inequality that would precede
it. A, B and D would each have Stage 3E assert something about a hollow that no stage currently makes.
C is the only option under which every condition Stage 3E enforces is decidable from the frozen
structure it actually owns.

This is not a recommendation to delete the design's clause. It is a recommendation about **where the
clause is evaluated**.

### 24.5 Proposed owner decision, exact wording

> The clause *"its floor lies below the outlet by a bounded depth"* is **not** a Stage 3E validity
> invariant. Stage 3E declares structural enclosure and outlet topology: a closed ridge-majority
> perimeter, exactly one outlet gap, an outlet elevation read from that gap, and an outlet that
> connects to an existing downhill primary corridor. Both the existence of the hollow and its bound
> are deferred to the stage that owns an authoritative post-Slice-C carved field, and are recorded
> against the same debt as the independent depression scan. No `MaxBasinDepthMeters` is introduced, no
> scale-relative depth formula is introduced, and no tier is assigned to a basin. The design text is
> not edited; this preflight records where the clause is evaluated.

### 24.6 Status of B4

**B4 is not resolved by this pass. It awaits explicit owner approval of §24.5.**

Nothing was adopted silently: no bound was chosen, no parameter added, no formula invented, no tier
assigned. If §24.5 is approved, B4 closes as *deferred with an owner* rather than as *answered*, E2
proceeds on conditions 1–4, 6, 7 and 10–17, and B2 remains the only blocker on the intermontane path.

---

## 25. Contract corrections found by E2

E2 falsified two sentences written earlier in this document. They are corrected here rather than left
standing, and neither correction changes any frozen Stage 3C behaviour - both are about what the
contract *said*, not about what the geometry *does*.

### 25.1 §17.6 was wrong about where a boundary may meet a trajectory

§17.6 said: *"A boundary crosses a primary trajectory **only at the outlet**."* That is false, in two
independent ways, and E2 found both by failing.

**A cirque encloses the head of its own corridor.** `HeadPoint` is `massif.PointAt(HeadS)` and `HeadS`
is the midpoint of the corridor's crest interval, which is the middle of the enclosure's crest run. So
the corridor begins **on** the boundary and leaves through the gap: two contacts, both declared by the
construction, not one.

**The crest belongs to both flanks.** Every corridor on the far flank also has its head on the same
crest, so a candidate's crest run contains the heads of opposite-flank corridors whose `HeadS` falls
inside its interval. Measured: twelve of twelve foreign contacts on the canonical set were exactly
that, each a single sample, each at the crest.

The corrected statement:

> A basin boundary may be met by a primary trajectory at the crest, and only at the crest, because the
> crest is structure shared by both flanks and every corridor on either flank begins on it. Its **own**
> corridor additionally leaves through the outlet gap. Neither contact is an illegal crossing. What is
> forbidden is a corridor other than its own **running through** the enclosed ground.

### 25.2 Interior is decided at the skeleton's own resolution

"Running through" needs a test, and a naive point-in-polygon is not it. A far-flank corridor's head
lies on the true crest arc while the crest run is a chord-sampled polyline, so the head falls a few
centimetres to one side of it: measured, every foreign corridor that tested "inside" did so with
**one** sample, at the crest.

So a foreign corridor counts as inside only when a sample is inside **by more than
`RidgeSettings.SpurSampleSpacingMeters`**. That is not a new tolerance - it is the resolution the whole
structure is defined at, the same quantity Stage 3C uses to decide two floors are one floor and Stage
3D uses to keep a rib clear of a ridge. **No new constant was introduced.**

With that rule, no candidate on the canonical set is rejected.

### 25.3 The depression-scan debt is larger than §15.1 said

§15.1 said a depression scan of `Stage3Field` would be reading a field built from superseded
trajectories. True, and understated. Traced through the repository:

- `Stage3Field` is referenced by **one** file, `TerrainGenStage3CDump.cs`, an editor diagnostic that
  renders it to PNG. Nothing else in the repository builds or reads it.
- The production hydrology grid comes from `HydrologyTerrainGrid.BuildFromScene()` - **the Unity
  scene's terrain**, which the terrain bake writes.
- Every hydrology and depression test builds its grid from `HydrologyGrid.FromFunction(...)` -
  **synthetic analytic surfaces**.
- No depression test, and no production depression scan, references `GameDev.World.TerrainGen` at all.

So a declared Stage 3E basin is not merely invisible to a stale scan. **It is invisible to every
depression scan that exists, because none of them looks at any terrain-gen field.** The debt is not
"rewire `Stage3Field`"; it is "there is no path from the generator to a scanned surface". That is the
subject of the Stage 3F preflight.
