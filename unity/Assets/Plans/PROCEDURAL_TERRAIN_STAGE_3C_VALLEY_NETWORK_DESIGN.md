# Stage 3C — Valley Network Topology Design

**Design only.** No code, no tests, no scene or package changes. This document is the only file added.

Date: 2026-09-10 · audited against the current Stage 3A/3B/3C implementation
Revision 2: sections 4.2, 5, 6 and 7 were rewritten after review found the chain-closing operation
physically invalid. See section 5.0 for the defect and its proof.
Revision 3: section 5.4 added, resolving the apparent dependency cycle between topology and
trajectories; section 11 tightened; slices restated.

---

## 1. Current Stage 3C problem

The trajectory rewrite fixed the severe defects — no per-strip bowls, no rectangular seams, no toe or
attachment-interval discontinuities, no strip-selector mismatch, carving keyed to the valley's own arc
length, bounded incision. Those are kept.

What remains is a topology failure. Around 9–11 corridors leave one massif, each with its own
continuation, each of equal status, each running outward. The picture is a starfish, and many fingers
stop in the foothills.

Two measurements from the last pass make the point without appealing to the picture. Isotropy sits at
0.52–0.89 against a Stage 2 baseline of 0.83–0.99: the corridors add curvature along a small set of
similar directions. And every corridor receives the same `CorridorIncisionMeters` (7.2 m) regardless of
how much ground drains into it, so nothing in the field distinguishes a major valley from a minor one.

---

## 2. Root cause

The current model is:

```
primary spur  →  one valley corridor  →  independent continuation
```

Every corridor is the root of its own drainage. Nothing subordinates one valley to another, so every
valley is equally entitled to run to the sea, and N valley heads produce N independent exits.

`DownstreamId` and `ValleyTermination.Merge` exist, but they are set *opportunistically*: a walk
terminates as `Merge` when it happens to come within `SpurSampleSpacingMeters` of an earlier walk. That
records a collision, not a relationship. Which corridor yields is decided by build order and proximity
— by accident.

This is not a heading or curvature problem. Bending the fingers leaves N fingers.

---

## 3. The missing topology contract

**Valleys have no rank.**

Without an order on valleys, nothing can decide which yields to which, so no hierarchy can form. That
is the single missing contract, and everything else in this document follows from supplying it.

The contract to add, in three parts that must not be conflated — revision 1 conflated the first two
and that is what made it invalid (§5.0):

> **Adjacency is physical and permanent.** Two corridors are adjacent exactly when they share a
> bounding spur, and that never changes. Adjacency is never closed over, synthesised or inherited.
>
> **A spur toe says when a merge becomes possible; the terrain says whether it happens.** Below a
> spur's toe the divide it carried has ended, which permits its two valleys to join. It does not force
> them to: on a diverging apron they simply run on separately.
>
> **Rank decides only which identity survives a merge.** Every corridor carries a deterministic rank
> derived from the ground it drains. Rank has no position, so it cannot be allowed to decide *who*
> merges or *when* — only which of two joining channels keeps its name and its downstream geometry.

Two consequences follow, and each answers a question §2 of the brief asks:

- **Cycles are impossible.** Rank is a total order fixed before any merge, and a parent is always
  strictly higher-ranked, so the parent pointer strictly increases and can never return.
- **Trunks are an outcome, not a choice.** A channel that never converges with a neighbour before
  reaching a terminal endpoint is a trunk. How many there are is a property of the axis and the apron,
  measured per seed (§5.3, §15 M).

---

## 4. Valley network model

### 4.1 Adjacency is a chain, not a graph

Corridors on one flank are ordered along the crest arc, and `LowerSpurId` / `UpperSpurId` already
express it: two corridors are adjacent exactly when one's `UpperSpurId` equals the other's
`LowerSpurId`. A corridor has at most two neighbours.

That is a strong and welcome restriction. **A corridor can only merge with a neighbour**, because
between any two non-adjacent corridors stands at least one whole spur, and a valley cannot cross a
ridge. So the network is built on a 1-D chain per flank, not on a general graph — which is why no graph
framework is needed (§3 of the brief).

```
flank chain:   V0 ── S0 ── V1 ── S1 ── V2 ── S2 ── V3
               (Vi are corridors, Si the spurs that separate them)
```

### 4.2 Rank is a relief-volume proxy, not a computed catchment

The geographic measure of a valley's importance is how much ground drains into it and how much height
that ground sheds. Both come from existing quantities, but the units and the honest name matter.

**Dimensional audit, from the code.** `SInterval` is a normalised arc interval, so `SWidth` is
dimensionless. `MountainMassif.LengthMeters` is metres. `FlankHalfWidth(HeadS, FlankSign)` —
`HalfWidthAt` times the asymmetry factor — is metres.

```
FlankFootprintSqm  = SWidth · LengthMeters · FlankHalfWidth(HeadS, FlankSign)     [1 · m · m = m²]
ReliefVolumeCum    = FlankFootprintSqm · CrestAt(HeadS)                           [m² · m = m³]
Rank               = ReliefVolumeCum, ties broken by Id
```

Dimensionally correct. **Geographically it is a proxy, not a catchment**, and the earlier name
`CatchmentAreaSqm` overstated it. `FlankFootprintSqm` is the rectangle the corridor's strip occupies on
the massif flank; the real drainage area is a trapezoid widening downslope, and it excludes everything
the corridor collects beyond the massif toe and everything its tributaries bring. The name used from
here is `FlankFootprintSqm`, and the ranking quantity is `ReliefVolumeCum` — the rock volume standing
above base level in the corridor's own strip, which is the standard proxy for erosive capacity.

**One factor is inert, and this must be said.** `HalfWidthAt` tapers only within
`MassifSettings.EndTaperSpan = 0.16` of an axis end, and the attachment interval is `CoreInterval`,
clamped to `[MinRampFraction, 1 − MinRampFraction] = [0.18, 0.82]`. So `[0.18, 0.82] ⊂ [0.16, 0.84]`
and **`FlankHalfWidth` is exactly constant across every attachment interval on every seed** — the same
inertness that made spur spacing uniform in Stage 3B. The footprint therefore ranks by `SWidth` alone,
and the ranking is really *arc width × relief*. Both of those do vary: station jitter gives the arc
widths roughly a 2:1 spread, and `CrestAt` varies about fivefold across the core.

The product is used rather than a weighted sum because a product of an area and a height is a volume —
a quantity with a meaning — whereas a weighted sum would need a weight nobody can derive. Exponents
other than one on either factor would be invented tuning and are not proposed.

### 4.3 The network is a forest of chains

Each flank's chain reduces to one or more trunks; each massif has two flanks; each trunk runs to a
terminal endpoint. `DownstreamId` points from child to parent. The result is a directed forest.

---

## 5. Deterministic hierarchy algorithm

### 5.0 The defect in revision 1, and its proof

Revision 1 said: when `V1` becomes a tributary of `V0`, *"the chain closes over V1: its two neighbours
become adjacent"*. **That operation is not physically valid, and the review is right.**

Take `V0 — S0 — V1 — S1 — V2`. Closing the chain makes `V0` and `V2` adjacent, so a later round may set
`V2 → V0`. Three things then have no answer:

1. **No spur separates `V0` and `V2`.** The confluence rule was "the toe of the spur that separates
   them", and there is no such spur — `S0` separates `V0` from `V1`, `S1` separates `V1` from `V2`. The
   rule is undefined for the pair it just created.
2. **`V1` does not leave the terrain.** Becoming a tributary changes its identity downstream of its own
   confluence; upstream of that it is still a valley occupying ground between `S0` and `S1`.
3. **Any polyline from `V2` to `V0`** must therefore cross `S1`, then the `V1` corridor, then `S0` —
   unless those have already ended, which revision 1 never checked because it ordered merges by rank
   alone and never looked at where a divide actually stops.

The root cause: revision 1 let **rank** decide both *who merges* and *when*. Rank is a scalar with no
position, so it cannot know whether a divide is still there. **Only the geometry can say when two
valleys may join, and only the terrain can say whether they actually do.** Rank must be demoted to
deciding which identity survives.

Adjacency is therefore **never closed and never synthesised**. A corridor's neighbours are its physical
neighbours for the whole construction, and they never change.

### 5.1 Two separate questions

| question | answered by | why |
|---|---|---|
| **When may two valleys join?** | the separating spur's toe | a spur is the divide between them; below its toe that divide has ended |
| **Do they actually join?** | whether their trajectories converge | a divide ending permits a merge, it does not force one |
| **Which identity survives?** | rank (§4.2) | the larger valley keeps its name and its downstream geometry |

The second row is the correction that matters most and revision 1 omitted it entirely.

### 5.2 The algorithm

Per flank, over the corridor chain `V0 … Vn` and the internal spurs `S0 … Sn−1` that separate them:

```
for each internal spur Si, in ascending order of its toe's downstream position:
    A = the channel currently occupying position i        (identity may already be someone else's)
    B = the channel currently occupying position i+1
    if A == B: continue                                   // already one channel

    enable  = Si.Toe                                      // earliest possible confluence
    if the two channels' trajectories diverge downstream of enable:
        continue                                          // no merge; both keep running

    confluence = the later of (enable, the point where they converge)
    winner = the higher-ranked of A, B ; loser = the other
    DownstreamId(loser) = winner
    loser terminates at confluence, on the winner's surviving floor
    position i and i+1 now both hold `winner`
```

Spurs are processed in toe order, so a divide that has been passed is never consulted again and a
divide that is still standing is never crossed. Rank enters only on the last two lines.

**Divergence test.** Structural and threshold-free: take each channel's floor tangent at `enable` and
compare the rate of change of their separation. If the separation is non-decreasing downstream they
diverge and do not merge; if it decreases, the confluence is where it reaches zero. On the concave side
of a curved axis neighbouring valleys converge; on the convex side they splay. That is the correct
geography, and it is decided by the axis Stage 2A certified rather than by a steering weight.

### 5.3 What this does to the trunk count

Revision 1 claimed the fixed point gives exactly one trunk per flank. **That claim is withdrawn.**
It assumed every spur toe forces a merge. It does not: below the toe the two channels are free, and on
a radially descending apron adjacent channels *diverge*, so the merge never happens. Forcing it would
have invented convergence the terrain does not have.

The trunk count is therefore an **outcome**, not a target: one per divergent group. Its value must be
measured per seed rather than asserted, and gate M is restated accordingly (§15).

**This also changes where the hierarchy comes from.** If few merges occur — which is likely on a small
island with a short apron — the network will still have many exits. That is not by itself wrong: many
short radial streams is the correct drainage of a small mountainous island. What made the current
picture read as a starfish is that every finger is *identical in weight*, and the fix for that is the
rank-scaled incision of §11, which is independent of merging. Merging deepens the hierarchy where the
geometry supports it; ranking makes the hierarchy legible everywhere.

### 5.4 Candidate geometry, and why there is no dependency cycle

The apparent cycle is:

```
topology needs convergence  →  convergence needs candidate continuations  →
final trajectories need topology
```

**It does not close, because the convergence test never needs a continuation.** It needs the
*in-massif* floor, which exists before any topology is known.

#### 5.4.1 What exists before topology

`ValleyTrajectoryBuilder.BuildMassifPart` takes `(massif, corridor, spurs, settings)` and nothing else.
Its output — the corridor's floor from the crest to the massif toe — is the mid-line between the two
bounding ridges:

```
floorⱼ(t) = ( Rⱼ₋₁(t) + Rⱼ(t) ) / 2 ,           t ∈ [0, 1]
world(t)  = PointAt(floorⱼ(t)) + normal(floorⱼ(t)) · ( σ · t · FlankHalfWidth(floorⱼ(t)) )
```

`Rⱼ₋₁` and `Rⱼ` come from `SpurCurve.ArcAt`, fixed by Stage 3B; `PointAt`, `HalfWidthAt`, `Asymmetry`
and `SteepSide` come from Stage 2B. **No merge decision appears anywhere in it.** Every corridor's
in-massif floor is therefore buildable, for all corridors at once, before the first merge is
considered.

**Answers to the audit's questions 1 and 2:** the object is the **in-massif floor**, and it is *derived
by Stage 3C from Stage 3B geometry* rather than published by Stage 3B — but derived with no topology
input, which is the property that matters.

#### 5.4.2 The candidate continuation

```
CandidateRay(channel) = the ray from the last point of the channel's ACTIVE BED,
                        in the direction of that bed's tangent at t = 1
```

The **active bed** of a channel is the in-massif floor of its current root corridor — the winner of
every merge it has been through so far. An un-merged corridor's active bed is its own floor. A merged
channel's active bed is its root's floor; the loser's bed stops being active at its confluence.

This is a *candidate*, used only to decide topology. It is a ray, not a trajectory: it has a start, a
direction and no end, and it is never carved.

#### 5.4.3 The convergence test

| | |
|---|---|
| **input** | the two channels' active beds (polylines) and their `CandidateRay`s; the separating spur's `Toe` |
| **starting point** | the spur toe, expressed as `t_toe = TargetReachMeters / FlankHalfWidth(AttachS)`. With `SpurReliefFloor = 0.58` and `SpurReachJitter = 0.10`, `t_toe ∈ [0.52, 1.10]` — **it can exceed 1**, in which case the in-massif interval is empty and the test runs on the rays alone |
| **downstream interval** | `[t_toe, 1]` on the beds, then unbounded along the rays |
| **tangent** | finite difference over the last two samples of each bed; the beds are sampled at `SpurSampleSpacingMeters`, so this is the same resolution the structure is defined at |
| **separation** | on `[t_toe, 1]`, the world distance between the two beds at matched `t`; beyond, the distance between the rays at matched arc from their starts |
| **criterion** | the rays intersect at a point ahead of both starts. Equivalent to: separation is strictly decreasing and reaches zero at a finite parameter |
| **confluence** | that intersection, moved downstream to the spur toe if it would fall upstream of it |
| **asymptotic approach** | cannot arise. Two rays either intersect at a finite point or they do not; there is no "approaches but never reaches" case for straight rays, and therefore no epsilon to choose |
| **leaving the domain** | if the intersection is offshore, or on ground Stage 3A does not own for this massif, or beyond the structural domain — **no merge**. Both channels remain separate and reach their own endpoints |

Everything in that table is existing geometry or a sign test. There is no steering weight, no random
input and no threshold to invent.

#### 5.4.4 Candidate versus final trajectory

| | candidate (topology) | final (geometry) |
|---|---|---|
| built in | Slice B2 | Slice C |
| what it is | a straight ray from the massif toe | the carved valley floor, head to termination |
| extent | unbounded, never clipped | head → confluence (tributary) or head → coast (trunk) |
| depends on topology | **no** | yes |
| is carved | **no** | yes |
| uses the gradient stepper | **no** | only on terminal trunks |

The candidate answers one question — do these two converge — and is then discarded. The final
trajectory is built once the tree is fixed, and for a tributary its confluence is the point the
candidate test already determined, so Slice C re-derives nothing.

#### 5.4.5 Spatial safety of the candidate geometry

1. **Cannot cross an active spur.** Spur toes lie at `t ≤ 1.10`, and every ray starts at `t = 1` of its
   own bed. Inside `[t_toe, 1]` the test only *measures* the two beds; it creates no geometry there,
   and the beds are separated by the shared spur, which they cannot cross because each is the mid-line
   of its own strip.
2. **Cannot cross an unrelated valley.** A tributary's segment runs from its own bed to its parent's
   active bed. Every corridor between them has, by construction, already merged into that same parent —
   the merges are processed in spur-toe order, so a bed lying between two merging channels is inactive
   before the segment that passes over it exists. Proof by induction on processing order.
3. **Cannot cross an already-created tributary.** Confluences on one parent are produced in ascending
   arc order (§7), so no two tributary segments on the same parent can interleave.
4. **Cannot cross the coastline.** A confluence that falls offshore is rejected outright, so no
   candidate geometry is ever accepted seaward of the shore.

#### 5.4.6 Worked example, candidate geometry at every step

`V0 — S0 — V1 — S1 — V2`, ranks `V0 > V1 > V2`, spur toes `d0`, `d1`.

**Before B2 (end of Slice B).** Three in-massif floors `bed0, bed1, bed2` exist, each from its own two
spurs. Three candidate rays `r0, r1, r2` exist. Active beds: `bed0, bed1, bed2`. No topology yet.

**Case B — `d1 < d0` (S1 ends first).**

| step | spur | channels | candidates | outcome |
|---|---|---|---|---|
| 1 | `S1` | `V1`, `V2` | `r1`, `r2` | if they converge: `V2 → V1`, confluence on `bed1`. Active beds now `bed0`, `bed1` |
| 2 | `S0` | `V0`, `V1` | `r0`, `r1` | if they converge: `V1 → V0`, confluence on `bed0` |

Result `V2 → V1 → V0`, depth 3. Every candidate used was a bed built in Slice B. Nothing crossed.

**Case A — `d0 < d1` (S0 ends first).**

| step | spur | channels | candidates | outcome |
|---|---|---|---|---|
| 1 | `S0` | `V0`, `V1` | `r0`, `r1` | if they converge: `V1 → V0`, confluence `c0` on `bed0`. Position 1 now holds channel `V0`, whose **active bed is `bed0`** |
| 2 | `S1` | channel at position 1 = `V0`, and `V2` | **`r0`**, `r2` | tested between `bed0`'s ray and `bed2`'s ray |

At step 2 the candidate for position 1 is `r0`, not `r1`, because `bed1` stopped being active at `c0`.
`r0` and `r2` start two corridors apart, so they converge less often — and when they do not, `V2`
remains a trunk, which is the correct reading: `V2` has no neighbour left to join before the coast.

When they do converge, `V2`'s segment runs from `bed2` to `bed0` across ground that used to be `bed1`'s
corridor. It crosses nothing: `S1` is behind its own toe, `S0` ended at `d0 < d1`, and `bed1` is
inactive downstream of `c0`.

#### 5.4.7 Slice B2 is implementable without final trajectories

Everything B2 consumes — in-massif floors, tangents, spur toes, ranks, ownership — is available at the
end of Slice B. It writes only `DownstreamId`, `Termination` and the confluence point. It touches no
continuation, no carve and no height. **The cycle does not exist.**

---

## 6. Tributary / main-stem semantics

**`Valley A → Valley B` means:** A's floor terminates on B's *surviving* floor at their confluence; A's
carve is handed to B; B keeps its identity, its geometry and its downstream course.

| question | answer |
|---|---|
| parent selection | the higher-ranked of the two channels adjacent across the spur being processed |
| when | at or after that spur's toe |
| where | on the winner's surviving floor, at the convergence point |
| downstream progress | the confluence is downstream of both heads and of the spur toe |
| parent after the merge | geometry unchanged; accumulated relief volume increases (§11) |
| what happens to A | `Termination = Merge`, `DownstreamId = B.Id`, floor ends on B's floor |
| cycles | impossible: rank strictly increases along every parent pointer, and rank is a fixed total order computed before any merge |

**A corridor's parent is always a physical neighbour's channel.** It may carry a different corridor's
*identity* — that is what happens when the neighbour has already merged — but the confluence is always
located at the neighbour's position, never teleported across intervening terrain.

### 6.1 Worked example: `V0 — S0 — V1 — S1 — V2`

Let `d0`, `d1` be the downstream positions of `S0.Toe`, `S1.Toe`. Rank: `V0 > V1 > V2`.

**Case A — `d0 < d1` (S0 ends first).**

1. Process `S0`. Channels are `V0` and `V1`. If they converge, `V1 → V0`, confluence `c0` on `V0`'s
   floor at or after `d0`. Positions 0 and 1 now both hold `V0`.
2. Process `S1`. It separates position 1 — now holding `V0` — from position 2, holding `V2`. So the
   pair is `V0` and `V2`, and `V2 → V0` **is legitimate**, but only now and only here.
   - **Confluence:** on `V0`'s surviving floor, at or after `d1`.
   - **Which spur defines it:** `S1` — the spur that actually separates position 1 from position 2.
     There was never a need for a spur "between V0 and V2"; the spur is between the *positions*.
   - **Connecting polyline:** from `V2`'s floor at `d1` to that point on `V0`'s floor.
   - **Does it cross anything?** `S1` — no, it starts at `S1`'s toe, below the divide. `S0` — no, `S0`
     ended at `d0 < d1`. `V1`'s floor — no, `V1`'s floor terminated at `c0`, which is at or after `d0`
     and upstream of this segment's whole extent only if `c0 ≤ d1`; the ordering `d0 < d1` plus
     "confluence at or after the toe" does not by itself guarantee `c0 ≤ d1`, so **this is a checkable
     condition, not an assumption** — gate F tests it and the construction orders confluences to
     satisfy it (§7).

**Case B — `d1 < d0` (S1 ends first).**

1. Process `S1`. Channels `V1` and `V2`. If they converge, `V2 → V1` (V1 outranks V2), confluence on
   `V1`'s floor at or after `d1`.
2. Process `S0`. Channels `V0` and `V1`. If they converge, `V1 → V0`.
3. Result: `V2 → V1 → V0`, a chain of depth 3. **Nothing crosses anything**, and this is the case
   revision 1 could never produce because it collapsed `V1` out of the chain.

Case B is the common one when spur reaches are similar and jittered, and it is where the design's
tributary depth actually comes from.

---

## 7. Merge semantics

**Model A stands: the parent survives, the tributary terminates into it.** `DownstreamId` expresses it,
no corridor is created or destroyed, and `valleys = spurs + 1` per flank remains true because corridors
are only reclassified.

Geometry at a confluence:

- The tributary's last floor point is **on** the parent's floor polyline — a point, not a proximity.
- The approach direction is the tributary's own floor tangent; the junction angle is therefore
  determined by the two structures. It is measured (gate L), never bounded by an invented threshold.
- Depths combine by the smooth maximum the carve already uses. The parent outranks the tributary, so
  its trough is at least as deep at the junction, and the tributary's ends at full strength via
  `TroughToMergeProfile`. Continuous with no special case.

**The ordering constraint the worked example exposed.** A tributary's floor must terminate before the
segment of parent floor that a later tributary will join, or two tributary segments could cross. Since
spurs are processed in toe order and each confluence is at or after its own spur's toe, confluences on
one parent's floor are produced in ascending arc order. **Confluence arc positions on a given parent
are therefore monotone in processing order**, which is exactly the condition that prevents crossing.
This is a property of the algorithm, and gate F verifies it rather than trusting it.

## 8. Downstream direction

The brief asks for structural trajectory and elevation ordering to be separated. They are:

**Structural — the skeleton.** The network's nodes are already determined: valley heads (on the crest),
confluences (at spur toes), and terminal endpoints. The trajectory is the polyline through those nodes.
Inside the massif it is the ridge mid-line, which is correct today. Between the toe and a confluence it
is a short structural segment. **Nothing is walked by gradient descent.**

**Elevation — the ordering and the validity test.** Stage 1's field decides only:
- which of two candidate confluence points is downstream (the lower one);
- whether a proposed parent is actually downstream of its child (a validity check, not a search);
- where a terminal trunk's coast point is.

This is the precise correction to the previous attempt: gradient descent was the *stepper*, applied to
every corridor at every step, on a dome — so every corridor became radial. Here elevation is a
comparator applied at a handful of nodes.

**The terminal segment.** After the last confluence, one trunk per flank runs to the coast. That single
segment may follow Stage 1's steepest descent, because there are now one or two of them per massif
rather than eleven — and a radial trunk from an isolated massif is geographically correct. The failure
was never that a trunk runs radially; it was that all eleven did.

---

## 9. Termination semantics

`ValleyTermination` gains no new member for Stage 3C. Its current members map onto the contract:

| member | valid when | reached by |
|---|---|---|
| `Merge` | the corridor is a tributary | its floor ends on its parent's floor |
| `Coast` | the corridor is a terminal trunk | its floor ends on the coastline |
| `Divide` | the trunk met another massif's territory | its floor ends on the inter-massif divide |
| `Unresolved` | **a defect**, never an accepted outcome | — |

**A valley is never complete because a search failed.** Small gradient, zero uplift, entering the
foothills and "no next step found" are all removed as terminating conditions — the previous
implementation had all four and they are what produced corridors stopping in open ground.

`Basin` is *not* added now. Declared basins are Stage 3E's object; adding the member before there is
anything to point at would be speculative. Reserving it is noted in §13.

Interaction of the four concepts:

- `TargetReachMeters` (spur) fixes where a divide between two valleys ends → the confluence.
- `SpurCurve.Termination` says whether that toe was the spur's intended reach or a boundary cutting it
  short → an earlier confluence.
- `DownstreamId` records the parent.
- `ValleyCorridor.Termination` records which of the four terminal states applies.

---

## 10. Coast and basin semantics

**Coast.** The trunk's floor reaches the shoreline. The coastal gate already takes the carve to zero
there, so the trough fades before the water's edge and the coastline cannot move. No new mechanism.

**Declared lake / basin.** Not available in 3C. A trunk that ought to end in a basin will end at the
coast instead, which is correct in the absence of a basin. When 3E declares basins, a trunk whose path
enters one terminates there and the basin's outlet corridor continues — the model already supports it
because a corridor terminating on another structure is what `Merge` does.

**Closed structural basin.** Same as above; 3E's object.

**Where `HydrologyPlanner` is and is not used.** It is a *validator*, never a producer, and only in 3F:
`DepressionFill` and `BasinExtraction` run read-only on `H3` and their answer is compared with what
Stage 3 declared. A declared basin the planner does not find, or a lake it finds that Stage 3 did not
declare, is a defect in one stage or the other. Stage 3C must not call it, must not seed it, and must
not move drainage ownership into Stage 3.

---

## 11. CarveField handoff

The current carve is kept. Three changes, all consuming the network rather than replacing the model.

**Upstream of a merge — unchanged.** `C = (α · uplift · Rise(a) + incision · Trough(a)) · Across(w)`,
keyed to the corridor's own arc.

**At a merge — already correct.** The tributary uses `TroughToMergeProfile` (ends at full strength);
the parent is at least as deep; `SmoothMax` blends. Continuity holds without a special case.

**Downstream of a merge — the change that matters.** Today every corridor gets the full 7.2 m
regardless of size, which is a large part of why all fingers look equal. Instead:

```
AccumulatedReliefVolumeCum(c) = ReliefVolumeCum(c) + Σ AccumulatedReliefVolumeCum(children of c)
incision(c) = CorridorIncisionMeters · ( AccumulatedReliefVolumeCum(c) / MaxAccumulated )
```

The largest trunk receives the full bound; a small headwater valley receives a small fraction. **No new
number**: the bound is the existing `CorridorIncisionMeters`, the volume is §4.2's, and the scaling is
a normalisation rather than a chosen exponent.

**Four properties, checked rather than assumed:**

1. **Defined in existing terms.** `ReliefVolumeCum` is §4.2's, built from `SWidth`, `LengthMeters`,
   `FlankHalfWidth` and `CrestAt`. The accumulation is a sum over the tree Slice B2 produced.
2. **Units.** m³ summed with m³ gives m³; the ratio is dimensionless; times metres gives metres. ✓
3. **Rank is unaffected by accumulation, and this must stay true.** Ordering uses
   `ReliefVolumeCum` — **own volume only**, computed once before any merge. Depth uses
   `AccumulatedReliefVolumeCum`, computed once after the tree is fixed. **They are two different
   quantities and must never be conflated:** ranking by an accumulated value would make the order
   change as merges happen, which reopens the possibility of a cycle. Revision 2 wrote
   "AccumulatedCatchment" in both places and was wrong to.
4. **Always within the bound.** The ratio lies in `(0, 1]`, so `incision ∈ (0, CorridorIncisionMeters]`,
   with the single largest trunk taking exactly the bound. Guard `MaxAccumulated > 0` for a world with
   no corridors.

**Determinism of the sum.** Floating-point addition is not associative, so children are accumulated in
ascending `Id` and the tree is walked in a fixed post-order. Without that, two runs could differ in the
last bits and the field hash would not repeat.

> **Unresolved parameter (§18-2).** Whether the scaling is linear in catchment or follows a
> Horton-style power law. Linear introduces nothing; a power law would need an exponent that no
> existing project value supplies. **Recommendation: linear**, and revisit only if the measured depth
> spread is too flat.

The contracts stay exactly as they are:

```
H3 <= H2                                    everywhere
H3 >= H1 - CorridorIncisionMeters           only inside a declared corridor
H3 >= H1                                    elsewhere
H3 >  SeaLevelMeters                        on land
ocean unchanged
```

Nothing in this design requires changing them, and scaling incision *down* by catchment can only make
the bound less binding.

---

## 12. Stage 3B relationship

**No Stage 3B change is required.** Everything the hierarchy needs is already published:

| needed | source |
|---|---|
| adjacency | `LowerSpurId` / `UpperSpurId` shared between corridors |
| arc width | `SWidth` |
| flank length | `MountainMassif.HalfWidthAt` + `Asymmetry` + `SteepSide` |
| relief | `MountainMassif.CrestAt(HeadS)` |
| confluence point | `SpurCurve.Toe` |
| confluence validity | `SpurCurve.Termination`, `TargetReachMeters` |
| ordering along the crest | corridors are emitted in ascending arc per flank |

Stage 3B's topology, station generation, `valleys = spurs + 1`, headwaters-on-the-ridge and
non-crossing guarantees are all untouched. Stage 3C reorganises the corridors it is given; it does not
ask for different ones.

---

## 13. Stage 3D / 3E / 3F relationship

**3D — secondary spurs.** Subdivides a *flank strip*, which is a spur's object, not a corridor's. The
hierarchy is on valleys and adds no ridge, so the two are orthogonal. The chain shape does change what
3D sees — a tributary's strip is still its strip — so 3D should key off `SpurCurve`, as designed, and
not off the valley network.

**3E — basins.** Needs one addition it must make itself: a `ValleyTermination.Basin` member and the
rule that a trunk entering a declared basin terminates there while the basin's outlet corridor
continues. The network model already expresses "terminates on another structure", so 3E extends it
rather than reworking it. 3E also owns the independent depression scan; gate H below is its early form.

**3F — handoff.** Publishes exactly what §8 of the Stage 3 design specifies: corridor floor polylines
with downstream links. The hierarchy is what makes that artefact meaningful — a flat list of eleven
independent corridors gives Stage 4 nothing to check reaches against, whereas a tree with trunks and
tributaries can be compared with the Strahler orders `RiverNetworkBuilder` already computes.

The chain `primary spur → primary valley → tributary/trunk network → secondary structure` is
compatible: each arrow consumes the previous stage's published output and adds one level.

---

## 14. Determinism contract

Same seed ⇒ identical hierarchy, parents, confluences, terminal states, trajectories and diagnostics.

- **No new seed stream.** The hierarchy is a deterministic function of geometry Stage 3B already fixed;
  `IWorldSeed.Fork` is not needed and must not be used, because a drawn hierarchy would be a drawn
  topology and the design forbids randomness deciding topology.
- **Total order.** Rank ties break on `CrestAt(HeadS)` then on `Id`, so no comparison is ambiguous and
  no sort is unstable.
- **Order-independent passes.** Each round processes corridors in ascending rank, not in list order.
- **No global mutable state**, no dictionary iteration order, no floating-point-dependent set
  membership.

---

## 15. Quality gates

| | gate | how it is measured |
|---|---|---|
| A | every primary valley is a tributary, a trunk, or a valid terminal valley | classify all corridors; the three sets must partition them with none left over |
| B | every tributary reaches its parent | its last floor point lies within a lattice cell of a segment of its parent's floor |
| C | every terminal trunk reaches coast / declared basin / valid structure | `Termination ∈ {Coast, Divide}`; for `Coast`, the last point is within a cell of the coastline |
| D | no cycles | walk every parent chain; assert rank strictly increases and the walk halts |
| E | no disconnected fragments | every corridor reaches a terminal trunk by following `DownstreamId` |
| F | no unintended crossings | pairwise segment intersection over all floors; the only permitted contact is a child's endpoint on its parent |
| G | no premature termination | `Unresolved` count is zero |
| H | no closed artificial depression | **independent** priority-flood over `H3` from the ocean; `fill == H3` everywhere; shares no code with `CarveField` |
| I | no unauthorised lowering below Stage 1 | `H3 ≥ H1 − CorridorIncisionMeters` inside corridors, `H3 ≥ H1` outside; both counted separately |
| J | ocean unchanged | ocean samples byte-identical to Stage 2 |
| K | deterministic topology | rebuild; compare parents, confluences, terminations and floor points exactly |
| L | continuous merge geometry | junction angle measured and reported at every confluence; carve sampled either side of every confluence agrees within the profiles' own second-difference bound |
| M | measurable hierarchy | two independent readings, because merging is an outcome and not a target. **Weight hierarchy:** the ratio of the largest to the smallest corridor incision is at least the ratio of their ranks, so no two corridors are equal-status. **Branch hierarchy:** where merges occur, report trunk count, maximum tree depth and the Strahler order of each trunk, computed as `RiverNetworkBuilder` already defines it. A seed whose terrain supports no merge passes on the first reading alone. |
| N | adjacency is never synthesised | every `DownstreamId` points to a channel occupying a physically adjacent chain position at the moment of the merge; assert that the spur processed for that merge is the one separating those two positions |
| O | confluences are monotone on a parent | for each parent, the arc positions of its tributaries' confluences are strictly increasing in processing order |

Gate M is the one that would have failed the current implementation immediately: trunks = heads = 11,
depth = 1.

---

## 16. Diagnostics

**Network plan** — the picture that makes the topology legible at a glance:

- valley heads as filled discs on the crest, sized by rank;
- corridor floors as polylines, **stroke width by Strahler order** so trunks read heavier than
  tributaries;
- confluences as ringed markers at spur toes;
- downstream arrows every fixed distance;
- terminal endpoints glyphed by `Termination` (coast / divide / basin / **unresolved in a warning
  colour**);
- corridor ids as small labels;
- the spur skeleton beneath, in a muted tone, so the divides that separate the valleys are visible.

A starfish is then unmistakable: eleven equal-weight strokes with eleven terminal glyphs and no
confluence rings.

**Network report** — text, per seed:

```
heads, corridors, tributaries, trunks, confluences
max tree depth, Strahler order per trunk
termination histogram (Coast / Merge / Divide / Unresolved)
cycle count (must be 0), disconnected fragments (must be 0)
crossing pairs (must be 0), junction angle min/mean/max
accumulated catchment per trunk, incision per corridor
undeclared depressions from the independent scan (must be 0)
```

---

## 17. Implementation slices

Ordered so topology is validated before any height changes.

**Slice A — rank and adjacency.** `ReliefVolumeCum` per corridor and the flank chain, from existing 3B
output. No geometry change. Testable: total order, adjacency correctness, determinism.

**Slice B — in-massif floors and candidate rays.** Build every corridor's in-massif floor and its
tangent ray; order the internal spurs by toe position; establish channel occupancy over the chain. **No
merging, no continuation, no carve.** This slice is what makes B2 acyclic: it produces all the geometry
B2 consumes, and consumes no topology itself. Testable: floors depend only on Stage 3B input
(rebuild with the topology stripped and compare), gate N, determinism.

**Slice B2 — convergence test and merges.** The test of §5.4.3 over the beds and rays from Slice B;
parent selection by rank; confluence placement; `DownstreamId`, `Termination`. Writes topology only —
no trajectory is extended and no height moves. Testable: gates A, D, E, K, N, O.

**Slice C — final trajectories.** Tributary floors are truncated at the confluence B2 already fixed;
trunk floors continue to the coast. The gradient stepper survives only on terminal trunks. Nothing here
re-decides topology. Testable: gates B, C, F, G, L.

**Slice D — network diagnostics.** The plan render and the report. This is the slice that decides
visually whether the starfish is gone, and it lands **before** any carve change.

**Slice E — CarveField integration.** Catchment-scaled incision; merge continuity verification.
Testable: gates I, J, L, and the existing Stage 3C invariants.

**Slice F — quality gates.** The independent depression scan (gate H), gate M, and the isotropy,
reachability and slope measurements re-run across the canonical and robustness seed sets.

---

## 18. Unresolved questions / contract gaps

1. **~~How many merge rounds~~ — withdrawn.** Revision 1 asked whether to iterate to a fixed point of
   one trunk per flank. The question was malformed: it presupposed that every spur toe forces a merge.
   With the corrected algorithm there are no rounds — spur toes are processed once, in order — and the
   trunk count is an outcome of where divides end and where trajectories converge. Nothing to choose.
   What replaces it is a **measurement**: how many merges actually occur on the canonical seeds. If the
   answer is near zero, the hierarchy rests entirely on rank-scaled incision (§5.3), which is a valid
   outcome for a small island but should be seen and acknowledged rather than discovered later.
2. **Incision scaling with catchment.** Linear normalisation against the existing bound introduces
   nothing; a Horton-style power law would need an exponent no project value supplies. Recommendation:
   linear.
3. **Cross-flank merging beyond the axis ends.** The two flanks' trunks meet past the axis tips, in the
   region Stage 3A classifies `BeyondAxisEnd` and refuses a frame for. Whether the two trunks of a
   massif may merge there, and how, is not determined by any current contract. Not needed for 3C —
   each trunk can reach the coast independently — but it must be answered before 3E declares
   intermontane basins.
4. **Terminal trunk path.** Steepest descent on Stage 1 is proposed for the one or two terminal
   segments. If even that proves too radial when measured, the alternative is a segment aimed at the
   nearest coastline point, which uses only `CoastQuery`. Deferred to measurement in Slice C.
5. **`ValleyTermination.Basin`** is deliberately absent until 3E has something to point at.

No contradiction was found between this design and the Stage 1/2/2A/2B/3A contracts, and none of them
needs to change.

---

## 19. Verdict

Revision 1 contained a real topological defect: closing the chain over a tributary created adjacencies
that do not exist on the ground, for which the confluence rule was undefined and any connecting
polyline would have had to cross a standing divide. Section 5.0 proves it and the algorithm is
replaced, not patched.

The corrected contract is: **adjacency is physical and permanent; spur toes decide when a merge becomes
possible; the terrain decides whether it happens; rank decides only which identity survives.** The
worked example resolves both orderings of `S0` and `S1` without any crossing, and produces the depth-3
chain revision 1 could not represent.

Two claims from revision 1 are withdrawn rather than defended: `CatchmentAreaSqm` was a proxy and is
renamed `FlankFootprintSqm` with its inert factor stated, and the fixed-point reduction to one trunk
per flank was an assumption that would have forced convergence the terrain does not supply.

The design is now implementable from existing Stage 3B geometry with no earlier-stage change and no
invented number, and the one place where hierarchy may turn out to be thin — a seed whose valleys never
converge — is called out in advance with a gate that measures it (§15 M) rather than a claim that it
cannot happen.

Revision 3 closed the last open question the reviewer raised. The suspected dependency cycle does not
exist: the convergence test consumes in-massif floors, which `BuildMassifPart` already derives from
Stage 3B geometry alone, and the candidate continuation is a straight tangent ray that is never carved.
Slice B is the slice that makes this explicit, and its test is that the floors can be rebuilt with the
topology stripped and compare equal.

**APPROVED**
