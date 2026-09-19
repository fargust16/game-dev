# Stage 3F Preflight — From Structure to an Authoritative Terrain Field

Read-only architectural audit. No Stage 3F code exists and none is written here. Nothing in Stage 3C,
3D, E1 or E2 was changed, no carving algorithm was designed, and no parameter was introduced.

> **Status update.** The prior question of §15.1 has since been decided and implemented: TerrainGen is
> the terrain authority, and its relief is baked straight into Unity `TerrainData`
> ([OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md)). The external terrain
> pipeline this audit describes as the owner of the shipping heightmap has been removed. The findings
> below are kept as the state at the time of the audit; the Stage 3F questions about `H3`, basins and
> the carve remain open.
>
> **Update 2026-09-18 — Stage 3F now carves against `H4A`, not `H2`.** A positive relief layer sits
> between Stage 2 and the carve: Stage 4A island relief (coastal floor `M`, plate districts, knolls,
> massif apron), R1 and **the production terrain authority** since 2026-09-18
> ([design](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN.md)). Note that Stage 3D/3E still declare
> their basins and outlets on `StructuralElevation` (`H2`) — reconciling the two levels is Stage 3F's
> job, not a repointing of Stage 3. Three things follow for Stage 3F,
> each measured in R1 and re-checked in Unity:
>
> - **The incision Stage 3F needs below Stage 1 never grows.** `R4A ≥ 0` raises the running minimum
>   along a trajectory, so `max(0, H1 − min H4A)` is ≤ its value on `H2` — measured increase 0.000 m on
>   all seven canonical seeds. What Stage 4A adds above Stage 1 is inside the carve allowance already
>   written in `Stage3Field` (`max(0, above − floor) + CorridorIncisionMeters`).
> - **The corridor depth budget applies to the single climb, not to a sum.** Stage 4A's own contract is
>   `climb(R4A) ≤ CorridorIncisionMeters`, so a corridor crossing Stage 4A relief stays a T3 trough.
> - **Massif-foot drainage is Stage 3F's (or Stage 4B's), not Stage 4A's.** R1 proves Stage 4A cannot
>   dig a closed depression outside the massif influence zone, and measures 6 of 40 seeds with a
>   0.33–2.32 m depression inside it — the inland-facing foot where the frozen Stage 2B flank meets a
>   Stage 1 zero-slope ring. Frozen `H2` has one of its own on seed 1019. A strike trough along the
>   massif foot is the landform; draining it is a carve question.
> - One more measurement worth carrying: on `H2` alone, trajectory V5 of seed 110201 already needs
>   8.57 m of incision below Stage 1, above the 7.2 m bound. That is a Stage 3C/3F matter, not Stage 4A's
>   — the relief layer lowers it to 0.78 m.

---

## 1. Executive summary

One finding dominates every other, and it was not what this audit set out to find.

**The procedural terrain generator is not connected to the terrain the project ships.** Every consumer
of `GameDev.World.TerrainGen` outside the namespace itself lives under `Assets/Scripts/Editor/` — 14
diagnostics and 14 test files, **28 files, zero of them runtime**. The heightmap the world actually
used was produced by the external terrain pipeline, since removed; the bake code of the time said so
in as many words — it only read the heightmap back off the GPU.

Three consequences follow, and together they reframe the Stage 3F question:

1. **There is no authoritative post-Slice-C terrain field.** There is no authoritative terrain-gen
   field at all. `Stage3Field` is built in exactly one place — an editor dump that renders it to PNG.
2. **No depression scan can see a Stage 3E basin.** Production hydrology scans the Unity scene's
   terrain; every hydrology test scans a synthetic analytic surface. Neither references terrain-gen.
3. **The boundary B2 asks me to locate — where structural topology ends and authoritative physical
   terrain begins — does not exist.** It is not mis-drawn or drawn in the wrong place. It has never
   been drawn.

So Stage 3F is not "add basins to the carve". The prior question is whether the generator is to become
the authority for terrain at all, or to remain a design instrument whose output the shipping terrain never sees. That
is an owner decision and it is §15.1.

---

## 2. Stage 3E frozen contract

**Validated** — computed from frozen structure and the Stage 2 structural elevation, nothing else:
`Id`, `CandidateId`, `CorridorId`, `MassifIndex`, `FlankSign`, `EnclosingSpurIds`, `Boundary`,
`Segments`, `OutletPointXZ`, `OutletElevationMeters`, `OutletBoundaryIndex`, `RidgePerimeterFraction`,
`AreaSqm`, `PerimeterMeters`.

**Deferred or absent by decision**: `FloorElevationMeters`, `DepthMeters`, basin tier,
`MaxBasinDepthMeters`, `InletCorridorIds`, `BasinKind`. A reflection test asserts none of these names
exists on the type.

Measured on the canonical set: **28 candidates → 28 basins, 0 rejections**; outlet elevations
61.2–97.4 m; ridge perimeter fraction 67.4–87.9 %.

Stage 3E declares **an enclosure and an outlet**. It makes no claim that a hollow exists — §24 of the
Stage 3E preflight established that the Stage 2 structural surface contains no hollow inside any
enclosure on any canonical seed.

---

## 3. Current authoritative terrain pipeline

What actually holds elevation, and who owns it.

```
   TERRAIN-GEN  (editor only, no runtime consumer)          SHIPPING TERRAIN
   ------------------------------------------------          ----------------
   Stage1LandmassGenerator                                    external terrain graph
      -> Stage1Result.Field : WorldHeightField  (H1)             |  GPU generation
      -> Stage1Result.Coast.SignedDistance                    Unity TerrainData heightmaps
             |                                                   ^  OverworldTerrainGrid / Tile
   Stage2BMassifGenerator.EvaluateFields                      OverworldTerrainMenu reads back
      -> Stage2BResult.Field : WorldHeightField (H2=H1+uplift)   |
             |                                                HydrologyTerrainGrid.BuildFromScene()
   Stage3ASkeleton -> RidgeSkeleton (3B)                         |
             |                                                DepressionFill / FlowRouting /
   ValleyNetwork -> ValleyTopology -> ValleyTrajectoryNetwork  BasinExtraction -> HydrologyDefinition
             |                                                   |
   SecondaryRidgeSkeleton (3D)                                HydrologyCarveField -> external pass
             |
   CirqueCandidateSet (E1) -> StructuralBasinSet (E2)
             |
        ***  nothing  ***
```

**The two columns do not touch.** No reference crosses from left to right anywhere in the repository.

Where elevation lives, by owner:

| quantity | authoritative holder | consumed by |
|---|---|---|
| island surface `H1` | `Stage1Result.Field` | terrain-gen stages, dumps, tests |
| massif surface `H2` | `Stage2BResult.Field`, and analytically `StructuralElevation.At` | Stage 2B dumps, E2, the depth probe |
| carved surface `H3` | `Stage3Field.Field` | **`TerrainGenStage3CDump` only** |
| valley floors, structural | `FinalValleyTrajectory.Points` (Slice C) | Stage 3D, E2, dumps, tests |
| valley floors, legacy | `ValleyCorridor.Floor` | `CarveField` only, via the 4-arg `RidgeSkeleton.Build` |
| basin enclosure | `StructuralBasin` | one dump, one test file |
| **shipping heights** | **external pipeline → Unity `TerrainData`** | everything the game and hydrology see |
| water | `WorldDefinition.Hydrology` | meshes, samplers, the road seam |

**Where carving happens.** Twice, in two unrelated systems. `CarveField` cuts corridors into `H2` and
the result is rendered to PNG and discarded. `HydrologyCarveField.Rasterize` fed an external composition
pass and is the only carve that reaches the shipping surface — and it is keyed to
`HydrologyDefinition`, not to structure.

**Where structural geometry becomes physical terrain.** Nowhere.

**What is lost between stages.** Everything left of `***nothing***`: the ridge skeleton, the valley
topology, the trajectories, the ribs, the basins. `H3` itself is lost — rendered and dropped when the
editor process exits.

**What exists only in diagnostics.** `H3`, the carve field, the candidate bed geometry, the trajectory
network's plan views, the secondary ribs, the cirque candidates, and the declared basins.

---

## 4. `Stage3Field` audit — verdict **B, adaptable, but adaptation unfreezes Stage 3C**

**1. Why it uses legacy trajectory construction.** Its only call site,
`Assets/Scripts/Editor/TerrainGenStage3CDump.cs:58`, builds its skeleton through
`RidgeSkeleton.Build(partition, settings, worldSeed, stage2b.Stage1)` — the four-argument overload,
which runs the pre-Slice-C `ValleyTrajectoryBuilder.Build` and writes `ValleyCorridor.Floor`.

This is **not a wiring oversight.** `Stage3Field` constructs a `CarveField`, and `CarveField` reads
`corridor.HasTrajectory` / `corridor.Project` / `corridor.Floor`. Slice C deliberately writes none of
those — the frozen test `SliceCMutatesNothing` asserts it never will. With the three-argument overload
the corridors would be empty and the carve would cut nothing. **`Stage3Field` depends structurally on
the legacy path.**

**2. What Slice C data it gets wrong or not at all.** All of it. Not the revised candidate geometry
(the bend and the released ground), not B2's topology — its merges come from the old geometric distance
test — not the confluences, not the final trajectories, not the ribs, not the basins.

**3. Can it be safely reused as-is?** No. Reusing it would mean continuing to measure a surface built
from geometry the project has already superseded.

**4. Can it be adapted?** Yes. It is a thin driver: build the carve, walk the 1025² lattice, measure
slope bands, isotropy, reachability and hashes. The measurement half is sound and reusable unchanged;
the binding half moves with `CarveField` (§5).

**5. Verdict: B — adaptable**, with two caveats that make it an owner decision rather than a mechanical
one:

- adapting it means editing `CarveField.cs`, a **frozen Stage 3C file**;
- adapting it produces a field that still nothing consumes, unless §15.1 is answered first.

---

## 5. `CarveField` audit — verdict **B, adaptable, with a precisely known cost**

**Where its valley geometry comes from.** `ValleyCorridor`, exclusively. It reads exactly nine members:
`Bounds`, `HalfWidthMeters`, `HasTrajectory`, `HeadS`, `LengthMeters`, `MassifIndex`, `Project`,
`Termination`, `ToeArcMeters`. Its `CorridorIndex` buckets corridors by `Bounds` at 64 m.

**Does it use frozen Slice C geometry?** No, and it cannot: `FinalValleyTrajectory` is a different type
that `CarveField` has never heard of.

**Is there an authoritative basin input?** No. `CarveField` has no basin concept, and `StructuralBasin`
is referenced by exactly four files — its own two, one dump, one test file.

**Can the carve represent a cirque depression?** Not as written. Its model is

```
C = ( alpha * uplift(p) * Rise(a) + incision * Trough(a) ) * Across(w) * gate
```

— a purely **subtractive** field applied along a corridor's own arc, with `TroughProfile` peaking at
the toe so that everything downstream is monotone decreasing. That is exactly the property
`PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §4.2 uses to conclude **"No pit can form along a corridor."**
A cirque hollow is a pit by construction. **Representing one needs a term the carve does not have**,
and §4.2's guarantee must gain its declared exception rather than be quietly broken.

**Can it represent an intermontane basin?** No, and for a deeper reason — see §9.

**Where should a basin floor appear?** Not in `StructuralBasin`, which was deliberately forbidden to
carry one, and not in `CarveField`'s existing terms, none of which can hold a closed low. It belongs to
a **new additive-negative term** owned by the physical stage, evaluated inside a declared boundary and
zero outside it, whose depth is derived from the outlet elevation and the enclosure rather than drawn.
The depth rule is exactly what B4 deferred and §15.3 still owes.

**Derived from `StructuralBasin`, or a separate physical representation?** Derived — but the derivation
needs one input `StructuralBasin` does not have and was right not to have: a floor elevation. The
sequence is `StructuralBasin` (enclosure + outlet, frozen) → a physical basin record (floor, depth,
derived) → a carve term. That middle record is the separate physical representation, and it belongs to
Stage 3F, not to 3E.

**The precise adaptation cost.** Re-binding from `ValleyCorridor` to `FinalValleyTrajectory` needs three
things the trajectory type does not have:

| needed | `ValleyCorridor` has | `FinalValleyTrajectory` has |
|---|---|---|
| `Project(p, out along, out across)` | yes | **no** — it has `Points`, `Cumulative`, `TangentAt` |
| `Bounds(pad)` for the spatial index | yes | **no** |
| `HeadS` | yes | **no** — it has `HeadPoint`; `HeadS` is readable from the frozen corridor |

All three are mechanical to write against `Points` / `Cumulative`, and all three land in frozen files.
The *carve model itself* — the profiles, `CorridorIncisionMeters = 18f / 2.5f`, the bucket index —
survives re-binding untouched.

---

## 6. `H3` / depression-scan audit — verdict: **no scan sees any terrain-gen field**

| scan | field scanned | before/after carving | resolution | authoritative? | sees a Stage 3E basin? |
|---|---|---|---|---|---|
| `HydrologyPlanner.cs:91` → `DepressionFill.Fill`, fed by `HydrologyBake.cs:75` | `HydrologyTerrainGrid.BuildFromScene(stride)` — the Unity scene's terrain | after the terrain bake, before hydrology's own carve | `settings.AnalysisStride` over the scene | **yes, for the shipping world** | **no** |
| `DepressionFillTests`, `BasinExtractionTests`, `FlowRoutingTests` (12 call sites) | `HydrologyGrid.FromFunction(...)` via `HydrologyTestTerrain` — synthetic analytic surfaces | n/a | fixture-defined | no | **no** |
| `OverworldBatchDiagnostics.cs:245/247` | `BuildFromScene()` before and after | around the hydrology carve | scene | no — a diagnostic | **no** |
| terrain-gen's own `Stage3Field` | `H3` | after its own carve | 1025² world lattice | **no — one editor dump reads it** | **no** — it has no basin concept |

**Assumptions those scans make about valley floors:** none from terrain-gen. `DepressionFill` treats
the heightfield as given and finds pits in it; it has no notion of a declared corridor, a trajectory,
or a declared basin, and no channel through which to be told about one.

**The earlier suspicion, confirmed and then exceeded.** Stage 3E's preflight §15.1 suspected the
existing depression scan ran on superseded trajectory geometry. Traced to code: `Stage3Field` does run
on superseded geometry, but **no depression scan reads `Stage3Field` at all.** The two facts are
independent and the second is the one that matters. This is recorded as §25.3 of the Stage 3E
preflight.

---

## 7. Slice C → physical terrain boundary

**It does not exist.** Stated plainly, as B2 requires.

Production representations, checked one by one:

| representation | exists? | owner | lifecycle |
|---|---|---|---|
| final height | yes | **external pipeline → Unity `TerrainData`** | GPU-generated, read back by `OverworldTerrainMenu`, persisted in the scene |
| valley floor | yes, two, neither authoritative | Slice C `FinalValleyTrajectory` (structural) and legacy `ValleyCorridor.Floor` (carve-only) | rebuilt from seed, never persisted |
| terrain carving | yes, but for water only | `HydrologyCarveField` → an external composition pass | derived from `HydrologyDefinition` |
| basin / depression | **no production representation** | — | — |
| water surface | yes | `WorldDefinition.Hydrology` | written by the hydrology bake |
| coast / ocean | yes | `CoastQuery` in terrain-gen, and the WaterWorks ocean plane in the scene | two separate things that agree by construction |

Note the asymmetry that makes this stage awkward: **a carving path into the shipping terrain already exists**, but it
is owned by hydrology and keyed to `HydrologyDefinition`, not by terrain-gen and not keyed to
structure. Stage 3F would either build a second such path or ask to share that one.

---

## 8. `StructuralBasin` → physical basin contract

A contract only. No algorithm is proposed.

| # | transition | input | output | owner | stage | mutability | determinism |
|---|---|---|---|---|---|---|---|
| 1 | structure → enclosure | frozen 3B/3C/3D | `StructuralBasin` | terrain-gen | **3E, done** | immutable once built | total; no RNG |
| 2 | enclosure → physical depression | `StructuralBasin` + an authoritative pre-basin surface | a floor, a depth, and a carve term | **the physical stage** | 3F | produces new data, mutates no input | must be derived, not drawn |
| 3 | depression → authoritative height field | that carve term + `H2` | one surface that is *the* terrain | **the physical stage** | 3F | the new authority | reproducible from seed alone |
| 4 | height field → depression verification | that surface | pits found, matched against declarations | an independent scanner | 3F | read-only | must share no code with the carve |
| 5 | verified terrain → water | that surface | `HydrologyDefinition` | hydrology | Stage 4 | read-only over terrain | already deterministic |

**The division, stated as a rule.** Stage 3E owns *the enclosure and the outlet declaration*. The
physical stage owns *the depression, its floor and its depth*. Hydrology owns *water and downstream
flow*. A stage that declares a floor is doing the physical stage's job; a stage that decides where
water stands is doing hydrology's.

**The open question transition 2 cannot dodge:** "an authoritative pre-basin surface" is precisely what
§7 says does not exist. Transition 2 is blocked on transition 3's substrate, which is why §16 orders
the re-bind before the basin work.

---

## 9. Intermontane blocker — **two missing things, not one**

Stage 3E recorded B2 as a missing side-boundary provenance. Auditing the code shows a second,
independent gap that is arguably the harder one.

**B2a — the side boundaries have no ridge provenance.** A cirque's four runs are crest + two spurs +
outlet gap, all read off **one massif's** frame. An intermontane basin spans two massifs and the
ground between. Its two *end* closures are derivable: a facing spur from each massif, joined across the
gap between their toes. Its two *sides* are not. The only structural lines available there are

- the line through consecutive spur toes — a point set, whose connecting stretches carry no ridge and
  would each be another gap, contradicting "exactly one outlet"; or
- the flank-foot offset from `FlankHalfWidth` — structural, but **not ridge**, so it cannot count
  toward `RidgePerimeterFraction > 0.5` and an enclosure built from it would fail check 5 by
  construction.

**B2b — there is no inter-massif corridor to be the outlet.** The design's data model says
*"`Corridor[] Corridors` // valley floors, **including inter-massif ones**"*. Verified against code:
`new ValleyCorridor` appears in **exactly one place**, `PrimarySpurBuilder.cs:445`, inside the
per-flank loop. **Inter-massif corridors were never implemented.** `InterMassifDivide` is a *divide* —
the ownership boundary between two massifs — not a corridor, and it follows the low medial ground
rather than a ridge.

So E2's check 4, *"the outlet connects to a corridor that continues downhill"*, would have no object to
connect to. **Even if B2a were solved tomorrow, an intermontane basin could not be validated.**

Both are moot on the current canonical set — every seed carries exactly one massif — which is why
neither has bitten.

**This is a design blocker requiring a new structural concept**, the inter-massif corridor, and it
belongs to whichever stage owns corridors, which is Stage 3B, which is frozen. It is recorded here and
deliberately not solved.

---

## 10. Terminal-apron interaction — clean

| claim | evidence |
|---|---|
| a cirque boundary lies wholly inside the structural domain | asserted per sample in E1 (`CandidatesRemainInsideStructuralDomain`) and again in E2 (`BasinsStayInsideTheStructuralDomain`); zero `Domain` rejections on the canonical set |
| a basin uses no apron geometry | its four runs are crest, two primary spurs and the gap between their toes; all four are inside the massif by construction |
| Stage 3E inherits no trajectory termination semantics | `StructuralBasin` has no termination field; `ValleyTermination` is untouched and a test pins its four members |
| the basin and its outlet create no new apron crossings | E2 writes no geometry — it selects a point on a boundary E1 already built; the 28 boundaries are unchanged by validation |
| the eight Stage 3C apron crossings remain the baseline | pinned per seed by `Stage3COutputIsBitIdenticalWithAndWithoutStage3D`, and untouched by E1 and E2 |

The apron problem is Stage 3C's and stays Stage 3C's. **It is not a Stage 3E defect, and Stage 3E does
not touch it.**

---

## 11. Determinism / RNG

Already deterministic, and available to a physical stage as-is:

| datum | determined by | RNG? |
|---|---|---|
| basin ordering | candidate order = corridor order = frozen skeleton order | none |
| `CorridorId` | the frozen Stage 3B corridor set | none |
| outlet point | first minimum of `StructuralElevation.At` over the `Outlet` run, in boundary order | none |
| boundary ordering | E1's four runs in fixed order, sampled at `SpurSampleSpacingMeters` | none |
| structural elevation | `StructuralElevation.At` — an analytic function of Stage 1 and the massifs | none |

**Stage 3F needs no new RNG for anything derived from these.** A basin's floor, its depth and its carve
term are all functions of geometry that is already fixed; if an implementation reaches for a stream,
that is the signal it is drawing something rather than deriving it. Existing streams, none of which it
may touch: `terrain.ridge.station`, `terrain.ridge.shape`, `terrain.ridge.secondary.station`,
`terrain.ridge.secondary.shape`, plus Stage 1's and Stage 2's.

---

## 12. Proposed Stage 3F responsibility

Deliberately narrow, and conditional on §15.1.

> **Stage 3F owns the first authoritative terrain field derived from frozen structure.** It takes the
> Stage 2 surface and the frozen Slice C trajectories and produces one height field that is *the*
> terrain-gen surface — including the declared basins as real depressions — together with an
> independent verification that the pits in it are exactly the ones declared.

Three sub-slices, in dependency order:

- **F1 — re-bind the carve to Slice C.** Give `FinalValleyTrajectory` the projection and bounds
  operations the carve needs, re-target `CarveField`, rebuild `Stage3Field` from
  `ValleyTrajectoryNetwork`. No new terms, no basins: the same carve, cut from the geometry the project
  actually settled on. This is where the 4-arg `RidgeSkeleton.Build` stops being used.
- **F2 — basin realisation.** Add the one carve term that can hold a declared depression, and with it
  the floor and depth Stage 3E was forbidden to invent. Blocked on §15.3.
- **F3 — independent verification.** A depression scan sharing no code with the carve, asserting that
  every pit is declared and every declaration is found.

---

## 13. Explicit non-goals

No hydrology. No water. No lakes. No terrain graph change. No terrain bake change. No new carving
algorithm before F1's re-bind lands. No intermontane inference. No basin tier. No basin depth parameter
chosen without an owner. No change to Stage 3D or E1. No new RNG. No new tolerance constant. No
`ValleyCorridor` creation. No `ValleyTermination` change. No canonical seed change.

---

## 14. Risks

1. **Unfreezing Stage 3C.** F1 cannot be done without editing `CarveField.cs` and probably
   `ValleyTrajectories.cs`. Both are inside the frozen commit. The freeze was worth having; lifting it
   should be deliberate and scoped, not incidental.
2. **`H3` would change.** Re-binding the carve to different trajectories changes the surface, and with
   it the slope bands, the isotropy ratio and the field hash that Stage 3C's invariant table names.
   Those were measured against the legacy carve and must be re-measured, not assumed to survive.
3. **The no-pit guarantee needs a declared exception.** §4.2 derives "no pit can form along a corridor"
   from corridor monotonicity, while §§9.x of the same document already promise "no pit *outside a
   declared basin*". F2 deliberately creates pits; the implementation must move to the weaker form the
   design always intended, and say so where §4.2 says the stronger one.
4. **Building a second authority.** If the external pipeline keeps owning the shipping heightmap, F1–F3 produce a field
   nothing consumes, and the project carries two terrain authorities that can silently disagree. This
   is the risk §15.1 exists to settle before work starts.
5. **Zero visible basins is still possible.** Nothing guarantees a declared enclosure becomes a visible
   depression once a carve term exists; §24 of the Stage 3E preflight showed the structural surface has
   no hollow there today, so the depth is entirely the new term's to supply — and that depth is exactly
   what B4 deferred.

---

## 15. Required owner decisions

**15.1 — the prior question. Is terrain-gen to become the authority for shipping terrain, or to remain
a design instrument?** Everything else depends on this. If it is to become the authority, F1–F3 are the
first steps of a much larger terrain-bake integration that needs its own preflight. If it is to remain an
instrument, F1–F3 are still worth doing — a self-consistent, verifiable surface is what makes the
structural work checkable — but they must be scoped as *validation*, and nobody should expect the world
to change shape.

**15.2 — may Stage 3C be unfrozen for F1, and how far?** Minimum needed: `CarveField`'s input binding,
plus `Project` and `Bounds` on `FinalValleyTrajectory`. Recommend a scoped unfreeze naming those files,
not a general one.

**15.3 — B4 still stands.** A basin's depth has no authoritative bound. F2 cannot be specified without
one, and the Stage 3E preflight §24 analysis (which rejected inventing a number from observed geometry)
is unchanged.

**15.4 — B2a and B2b.** Intermontane basins need side-boundary ridge provenance *and* an inter-massif
corridor concept. The latter belongs to frozen Stage 3B. Neither should be invented by an implementer.

---

## 16. Recommended implementation sequence

1. Answer §15.1. Nothing below is safe to scope without it.
2. If F1 proceeds: scoped unfreeze per §15.2, then re-bind the carve, then **re-measure** the Stage 3C
   field invariants against the new surface and record how they moved — as a new baseline, not as a
   regression to be argued away.
3. Only then F2, and only after §15.3 is answered.
4. F3 last, written against the field F1/F2 produce, sharing no code with them.
5. Leave B2a/B2b alone until §15.4 is answered.

---

## 17. Acceptance criteria

**F1.** `Stage3Field` builds from `ValleyTrajectoryNetwork`; no call to the 4-arg `RidgeSkeleton.Build`
remains; `ValleyCorridor.Floor` is written by nothing; the Stage 3C invariant table is re-measured and
its new values recorded with the old ones beside them; determinism holds and the frozen structural
stages are bit-identical.

**F2.** Every declared basin appears as a depression in the field; no undeclared pit appears; the depth
rule is the one §15.3 chose, and is cited rather than chosen.

**F3.** The scanner shares no code with the carve and finds exactly the declared set — same count, each
matched to its declaration within a stated tolerance.

---

## 18. Open blockers

| id | blocker | owner decision |
|---|---|---|
| **B2a** | intermontane side boundaries have no ridge provenance | §15.4 |
| **B2b** | no inter-massif corridor exists, though the design's data model says there is one | §15.4 — touches frozen Stage 3B |
| **B4** | a basin's depth has no authoritative bound | §15.3 |
| ~~**B5**~~ **CLOSED** | terrain-gen has no runtime consumer and no path to the shipping surface | §15.1 answered: `TerrainAuthorityField` is the authority and evaluates `IslandElevation` (`H4A`) as of 2026-09-18 |
| ~~**B6**~~ **CLOSED** | F1 cannot proceed without a scoped unfreeze of Stage 3C | §15.2 granted and spent: the unfreeze was API-only, §20 |
| ~~**B7**~~ **CLOSED** | the carvability guarantee and the carve are not on the same curve | F1 put them on one curve. Measured adverse climb along the Slice C trajectories in `H3F`: **1.94 / 1.96 / 2.56 / 2.29 m** against the 7.2 m bound. The remaining depressions are *not* centreline monotonicity failures — see §20 |
| **B8** | a massif-free world has no valleys at all | corridors come from massif spurs only, so 1337 / 256 / 777 carry zero corridors and Stage 3F leaves them bit-identical to `H4A` |
| **B9** *(new)* | 4–18 qualifying depressions survive the correct binding | they persist with Slice C geometry and small centreline climb, so their cause is off-centreline — corridor/corridor `SmoothMax` or the cross-section meeting Stage 4A relief. F3's independent scan is what classifies them against declared basins |

---

## 19. F0 measurement — the carve composed against `H4A` [MEASURED-U]

`CarvedElevation` (`H3F = H4A − C3F`) with `CarveField` bound as Stage 3C binds it today, dump-first,
Unity 6000.5.7f1. No frozen file was edited and nothing production changed. This exists to price the
§15.2 unfreeze before it is spent.

| seed | corridors | carve max | land touched | coast-aligned `H4A` → `H3F` | curvature | pits |
|---|---|---|---|---|---|---|
| 110201 | 9 | 68.2 m | 33.1 % | 73.6 → **52.7** | 0.76 → 1.17 | 19 (≤ 3.00 m) |
| 424242 | 9 | 66.6 m | 33.5 % | 68.1 → **58.9** | 1.30 → 0.96 | 5 (≤ 2.60 m) |
| 20260905 | 11 | 74.8 m | 36.0 % | 63.1 → **46.6** | 0.80 → 0.92 | 6 (≤ 1.31 m) |
| 7 | 9 | 70.1 m | 30.1 % | 73.0 → **59.0** | 1.45 → — | 5 |
| 1337 / 256 / 777 | **0** | 0 | 0 % | unchanged | unchanged | 0 |

**What it settles.** Cutting the declared structure into the island moves the metric Stage 4A could not:
the coast-aligned gradient share falls 9–21 points, and 20260905 clears the ≤ 65 % band Stage 4A failed
on four seeds. The curvature ratio moves *into* the 0.85–1.20 gate on 110201 and 424242, and
reachability improves on every seed. The character change is real, not marginal.

**What it exposes.**

- **B7.** 5–19 qualifying depressions appear where `H4A` had none. Classifying them against declared
  basins is exactly the independent scan F3 specifies and is not done here.
- **B8.** Three of seven canonical seeds get nothing at all.
- **Shape.** Carve max is 66–75 m but the mean over carved ground is 4.5–5.9 m: the depth sits in
  compact excavations at the corridor heads on the massif flanks, with only faint traces downstream
  across the island floor. That distribution is the likely source of B7 and the reason the lowland
  gains less than the massif does. Note `CorridorIncisionMeters` = 7.2 m bounds only the cut *below*
  Stage 1 — the total carve is dominated by the uplift term, which is why it reaches 68 m on a flank.

**What it does not settle.** Whether re-binding to Slice C changes any of the above. That is F1, it needs
§15.2, and this measurement is the argument for spending it rather than a substitute for it.

---

## 20. F1 — the carve re-bound to Slice C [MEASURED-U]

The scoped unfreeze §15.2 asked for, spent and no wider. Stage 3F now cuts along
`FinalValleyTrajectory`; production is still `IslandElevation`.

**What the unfreeze bought, exactly.** `IValleyCarveCurve` names the nine members `CarveField.DepthOf`
reads. `ValleyCorridor` implements it with one alias (`HasCarveGeometry`). `FinalValleyTrajectory`
implements it with `Project` and `Bounds` written over its own `Points`, mirroring
`ValleyCorridor`'s line for line, and carries the corridor's `HeadS` across unchanged so a consumer
cutting along the curve can read the crest above its head. `CarveField` is typed to the interface and
keeps its `RidgeSkeleton` constructor, so Stage 3C's dump and Stage 4A's S11-O are untouched.
**No trajectory sample, termination, width, confluence, ordering or RNG draw moved** — pinned by
`F1_Stage3CTopologyAndTrajectoryGeometryAreUnchanged`.

**Width.** Both paths call the same `ValleyTrajectoryBuilder.CorridorHalfWidthMeters`; Slice C
evaluates it at its own toe arc. F1 uses Slice C's `HalfWidthMeters` rather than borrowing the
corridor's, so no interpretation had to be invented.

**Domain.** `Project` clamps arc into `[0, LengthMeters]` by construction, so the carve cannot run past
a declared endpoint through a projection fallback. Per-trajectory `endCarve` is 0.00 m on every `Coast`
termination and non-zero only on `Merge`, which is the tributary handing over rather than stopping dead.
The eight Stage 3C terminal-apron crossings are untouched.

| seed | trajectories | F1 max | worst \|F1−F0\| | coast-aligned | curvature | pits F0 → F1 | max climb |
|---|---|---|---|---|---|---|---|
| 110201 | 9 | 67.76 m | 5.08 m | 73.6 → 52.7 | 0.76 → 1.17 | 19 → **18** | 1.94 m |
| 424242 | 9 | 66.64 m | **0.00 m** | 68.1 → 58.9 | 1.30 → 0.96 | 5 → 5 | 1.96 m |
| 20260905 | 11 | 74.75 m | 6.28 m | 63.1 → 45.2 | 0.80 → 0.92 | 6 → **4** | 2.56 m |
| 7 | 9 | 69.55 m | 1.87 m | 73.0 → 59.1 | 1.45 → — | 5 → 5 | 2.29 m |
| 1337 / 256 / 777 | 0 | — | 0.00 m | unchanged | unchanged | 0 → 0 | — |

**What it settles.** B7 is closed: the bound and the carve are one curve, and the measured climb is
1.94–2.56 m inside a 7.2 m budget. B6 is closed.

**What it does not.** The re-bind is a *localised* correction — the two geometries are bit-identical on
424242 and differ only where one trajectory's continuation changed elsewhere (on 20260905, a single
region at the south-west massif foot). It is the right binding, not a new visual result, and it does not
remove the depressions: **B9** records that they survive the correct geometry, so their cause is
off-centreline. F2 remains blocked on B4, and B8 is untouched.

### 20.1 Sweep 1000–1039 after F1 [MEASURED-U]

`TerrainGenStage3FDump.RunSweep`, forty seeds, no images, no seed looked at individually.

**Contract violations: 0/40.** No trajectory refused to project, no arc ran past a declared end, no
sample outside every trajectory's own half-width was carved, and nothing was carved at or seaward of
the shore. Stage 4A's own sweep is byte-identical to its pre-F1 run: **S8-A 0/40, S8-M 6/40, max
2.32 m**, so the scoped unfreeze moved nothing Stage 4A depends on.

**One number worth carrying, stated for what it is.** The sweep also walks each trajectory and records
the largest step-to-step rise of the finished `H3F` ground — "does the carved valley actually run
downhill". Worst over forty seeds is **7.66 m** (seed 1032; 1017 is 7.31 m, the other 38 are below
7.20 m). Canonical seeds are 1.94–2.56 m.

This is **not** an S11-T failure and must not be read as one. S11-T bounds
`max(R4A − running min R4A)` — Stage 4A's *relief* contribution along the trajectory — and it is green.
The sweep figure is the *ground's* adverse rise after carving, a different quantity. It is compared
against `CorridorIncisionMeters` only because that is the budget the trough term has to spend, so a
climb above it is ground the corridor cannot cut through. Two seeds in forty are in that state.

Together with B9 — a mean of 4.8 extra qualifying depressions per sweep seed, worst 11 — this says the
same thing twice: **the carve descends correctly along its centreline but does not yet guarantee a
drained valley.** Both belong to F3's independent verification and, where a declared basin is involved,
to F2. Neither is licence to change Stage 3C geometry or Stage 4A.

---

## 21. F3 — independent drainage verification [MEASURED-U]

`Stage3FVerification`, `TerrainGenStage3FVerifyDump`, `TerrainGenStage3FVerificationTests`. Verification
only: no carve, no geometry, no setting and no production file was changed. Production is still
`IslandElevation`.

**Independence.** Nothing in the verifier consults `CarveField` — not its profiles, its budget or its
index. It samples two surfaces, `IslandElevation` for the island and a delegate for the finished ground,
and derives the carve as `H4A − H3F`. The one shared primitive is `FinalValleyTrajectory.Project`,
because the question is about the declared valley and that is what declares it.
`V1_ASyntheticClosedHollowIsReported` proves the verifier can fail its subject: a hollow cut into a
draining ramp must come back `Closed`, and the ramp without it must not.

### 21.1 What it found

| seed | trajectories | drained | drainable | **CLOSED** | basin | worst CLOSED |
|---|---|---|---|---|---|---|
| 110201 | 9 | 3 | 7 | **4** | 0 | **6.32 m** |
| 424242 | 9 | 3 | 4 | **1** | 0 | 0.71 m |
| 20260905 | 11 | 4 | 3 | 0 | 1 | — |
| 7 | 9 | 6 | 8 | 0 | 2 | — |
| 1337 / 256 / 777 | 0 | — | — | — | — | no corridors |

Sweep 1000–1039: **30 of 40 seeds carry a CLOSED low, 96 in total**, worst **42.06 m** (seed 1032,
trajectory 8). `H3F ≤ H4A` holds at **0 violations** everywhere. Only **1 of the 96** has its barrier on
the last sample of a `Merge` trajectory, where the verifier stops without following the trunk — so that
scope limit accounts for one case, not the finding.

### 21.2 Two mechanisms, both measured

**M1 — inherited, and larger than the budget.** 110201 trajectory 5 carries an **8.00 m** adverse rise in
`H4A`; `maxCarve` on it is **7.20 m**, exactly `CorridorIncisionMeters`. The trough spends its whole
budget and **6.32 m** of barrier remains. On seed 1032 trajectory 8 the same story at scale: 48.02 m in
`H4A` against 6.70 m of carve, leaving 42.06 m. The carve cannot remove a rise larger than its own cap.

**M2 — induced by the trough's shape.** `TroughProfile` peaks at the toe and decays downstream, so it
lowers the upstream end of a corridor more than the downstream end and can *create* a barrier where
`H4A` had almost none: 110201 trajectory 3 goes 0.30 → 1.85 m, trajectory 4 goes 1.33 → 2.43 m.

Across the four massif seeds the carve *reduces* the adverse rise far more often than it adds to it
(on 110201, 6 trajectories of 9 improve by 0.79–3.30 m), so M1 dominates: **the barrier is mostly Stage
4A's relief, not the carve's doing.**

### 21.3 Width is what decides

`DrainableWithinTrajectory` is common (7 / 4 / 3 / 8). The separation is clean: where a low drains
around the barrier, the valley floor across its width is well below it (110201 trajectory 7: barrier
67.16 m, cross-valley floor 59.59 m). Where it closes, the cross-section is nearly flat at the barrier
(trajectory 4: 37.89 m against 37.79 m). The narrowest corridor on the seed — trajectory 4, half-width
18 m — is one of the closed ones. **A wide valley routes around its own barrier; a narrow one cannot.**

### 21.4 S11-T on seed 1032 — cross-validated and CONFIRMED

Stage 4A's own harness, run unmodified on seed 1032, prints:

```
S11-T trajectories: 9 checked; C(R4A) 19.64 m (<= 7.2) ... : FAIL
    V7: C(R4A) 7.59 m
    V8: C(R4A) 19.64 m
```

**The 19.64 m is Stage 4A's own number, not an F3 artefact.** The same run also reports
`plate+apron adverse 21.34 m` against a 1.5 m bound and `S11-O O-3: FAIL`. S11-T, S11-O and the
plate+apron bound are asserted by test on the **seven canonical seeds only**, where they are green, so
none of this was ever caught off that set.

**Why F3 first differed, isolated by experiment.** Four combinations were measured — Stage 4A's stations
and F3's, each with Stage 4A's `Relief.Sample().Total` and F3's `H4A − H2`. The R4A *source* changed
nothing on any trajectory of 1032, 110201 or 7. The **station set** was the whole difference: post-toe
vertices are spaced from the toe arc, F3 resampled from the head, and the two grids coincide only when
the toe arc is a whole number of steps. Seed 1032 trajectory 8 has `toeArc = 220 m` against a 20 m step,
so the grids coincided and both read 19.64 m exactly; seed 7 trajectory 0 has `toeArc = 352 m`, an 8 m
shift, which is the 1.06 → 1.78 m discrepancy.

`Stage3FVerification.ReliefClimbAtVertices` now samples Stage 4A's own stations, so the reproduction is
exact by construction and `V7_S11TReproducesStage4AsOwnPerTrajectoryValues` pins all nine trajectories
of 110201 against the published values.

One observation recorded and deliberately not acted on: F3's interpolated stations found a **higher**
peak between Stage 4A's vertices on seed 7 (1.78 m against 1.06 m), so sampling only at vertices can
under-report the continuous maximum. That is a property of Stage 4A's harness, which is frozen.
