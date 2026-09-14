# Procedural Terrain — Stage 3C Handoff

## 1. Status

**COMPLETE / FROZEN.**

Stage 3C is not to be reopened for tuning. The one known concern (§7) is handed to Stage 3D as a
design question, not left as a defect to be patched here.

## 2. Scope

| slice | what it settled |
|---|---|
| Slice A | fixed valley rank, and the physical chains |
| Slice B | candidate geometry: the in-massif bed and its terminal ray |
| Slice B2 | physical convergence and the resulting topology |
| Candidate Geometry Revision | corrected two structural defects in the bed and ray |
| Slice C | final valley trajectories |

## 3. Architectural pipeline

```
Stage 2B physical spurs
       ↓
Stage 3A ownership / frame
       ↓
Stage 3B physical valley corridors
       ↓
Slice A   fixed valley rank
       ↓
Slice B   candidate geometry
       ↓
B2        physical convergence / topology
       ↓
Slice C   final trajectories
       ↓
Stage 3D
```

Each arrow is one-way. Nothing downstream feeds back.

## 4. Key invariants

**Rank is fixed before topology exists.** `ReliefVolumeCum` is
`SWidth · LengthMeters · FlankHalfWidth(HeadS) · CrestAt(HeadS)` — a corridor's own relief and
nothing accumulated. An order that changed while merging would reopen the cycles the design exists
to rule out.

**Candidate geometry is topology-free.** Beds and rays consume only Stage 3B and Stage 2B output.
No merge, no `DownstreamId`, no termination appears anywhere in their construction, which is what
makes the convergence test that consumes them acyclic.

**Adjacency is physical and permanent.** Two corridors are adjacent if and only if they share a
bounding spur. Merging never creates adjacency and never destroys it.

**The division of labour at a divide's end.** Spur toes say *when* a pair may converge; the terrain
says *whether* (on land, inside structural ownership, downstream of the toe); rank says *who
survives*. Rank never creates topology.

**A divide stops shaping the valley floor where the divide stops.** Past a bounding spur's toe the
floor descends into the ground that spur released — towards that ridge's own line, never towards a
neighbouring channel, and never reaching it. The released share is the square of the descent since
the divide ended. No setting and no constant was added for this.

**B2 is the sole arbiter of convergence.** Slice C performs no second convergence test and holds no
merge distance; it enters the continuation through `ValleyTrajectoryBuilder.ContinueCandidate`,
which has the old builder's merge branch closed off.

**A merge means shared downstream geometry, written once.** The tributary's trajectory *ends* at the
confluence; everything below belongs to the surviving channel alone. A confluence is the intersection
of the two candidate rays, so both channels reach it exactly — C0 gap 0.

**Chains never close, and the network is acyclic.** The first and last corridor of a flank are never
joined, and every walk downstream terminates at a trunk.

**Determinism.** One seed fixes topology, trajectory ids, ordering, sampled points, confluence
locations, lengths and terminations, bit for bit. No tile-local randomness; all geometry is in world
coordinates and independent of which tile evaluates it.

## 5. Canonical seed results

| seed | corridors | B2 merges | trajectories | confluences | max depth | max length | min bend radius | repeat |
|---|---|---|---|---|---|---|---|---|
| 110201 | 9 | 1 | 9 | 1 | 1 | 1263 m | 73 m | identical |
| 424242 | 9 | 0 | 9 | 0 | 0 | 989 m | 71 m | identical |
| 20260905 | 11 | 1 | 11 | 1 | 1 | 1163 m | 44 m | identical |
| 7 | 9 | 0 | 9 | 0 | 0 | 1076 m | 207 m | identical |
| 101010 | 7 | 0 | 7 | 0 | 0 | 1132 m | 97 m | identical |
| 20261231 | 7 | 1 | 7 | 1 | 1 | 868 m | 96 m | identical |
| 1337, 256, 777 | 0 | 0 | 0 | 0 | 0 | — | — | identical |

**Three accepted merges across six massif-bearing worlds.** That is an observed outcome, not a
target. 1337, 256 and 777 carry no massif and therefore no corridor network; they are valid worlds.

Terminations across the six: 43 to the coast, 3 by merge, 0 unresolved. Continuity: C0 confluence gap
0.000 m, confluence angles 13.5°–27.4°, tributary turn into the survivor's heading 3.1°–23.9°.

Structurally verified: no synthetic non-adjacent topology, no chain closure, no cycles, no trajectory
crossing a ridge, no fan or starfish, no duplicated shared downstream segment, B2 merge count equals
Slice C confluence count on every seed.

## 6. Test baseline

**517 / 517 EditMode.** No existing test was weakened or removed.

## 7. Known concern — 8 downstream trajectory crossings

Eight pairs of final trajectories that B2 deliberately did **not** merge nevertheless cross.

| seed | crossings |
|---|---|
| 20260905 | 3 |
| 7 | 1 |
| 101010 | 4 |

All of them:

- lie on the apron below the massif foot, 300–660 m past the toe;
- lie outside the structural hold that B2 requires for a merge;
- belong to a pair whose candidate-ray convergence B2 had already rejected as `Ocean` or
  `OutsideDomain`.

**Root cause, known and not in dispute.** B2 decides on straight candidate rays. Slice C consumes the
curved candidate continuation. Two different curves: the walks close on each other sooner than the
straight rays do, so a pair can intersect before the ray intersection B2 judged. "These two do not
meet on valid ground" is true of the rays and false of the walks.

This is **not** to be fixed in Stage 3C. Specifically, do not merge these trajectories in Slice C, do
not weaken B2's domain checks, do not add a second topology algorithm, do not move trajectories
arbitrarily, do not add smoothing or noise, do not change candidate geometry, and do not hide the
crossings. They are reported by the Slice C diagnostic as a `crossings=` line per seed, and test 23
(`TrajectoriesNeverCrossInsideAMassifOrOverARidge`) pins the guarantee that does hold: a crossing
never happens inside a massif and never over a divide.

## 8. For Stage 3D

**Stage 3D must decide how downstream trajectory crossings interact with valley carving and
hydrological semantics.** Two channels that cross on the apron are geographically incoherent once
they are cut into terrain; whether the answer is a carve-time rule, a revised convergence test that
consumes the same curve the trajectory follows, or an explicit apron model is a Stage 3D design
decision and not a Stage 3C repair.

## 9. Stage 3C is frozen

**Stage 3C must not be retroactively modified merely to hide the crossings.** They are a measured,
reproducible consequence of a contract seam between two approved stages, and the measurement is the
useful artefact. Changing Slice C, B2 or the candidate geometry to make the number zero would remove
the evidence without removing the seam.

## 10. Repository state at freeze

- The terrain generator (Stage 1 through Stage 3C), its tests and its plan documents are the body of
  work being frozen here.
- Seventeen tracked files were modified before Stage 3C began and belong to a separate inland
  hydrology / terrain-bake workstream. They are untouched and are not part of this freeze.
- Further uncommitted work in the tree belongs to that hydrology workstream and to a Unity Terrain
  migration spike. Both are out of scope and untouched.
- `TerrainGenDump/` is generated diagnostic output and is not part of the repository.
- One cross-workstream dependency exists and is recorded rather than resolved:
  `TerrainGenStage1Tests.FieldLattice_IsTheLatticeTheUnityTilesShare` reads
  `OverworldTerrainGrid.DefaultHeightmapResolution`, and `OverworldTerrainGrid` belongs to the Unity
  Terrain migration spike. The terrain-gen tests do not compile without it.
