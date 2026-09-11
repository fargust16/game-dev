# Stage 3C — Candidate Geometry Revision Spike

Scope: candidate valley geometry only. No topology rule changes, no carving, no Stage 1/2/3A/3B
change, no Slice C/D/E/F. Slice B2 remains the arbiter of whether two channels merge.

---

## 1. Audit of the existing candidate bed and ray

### 1.1 What is built today

`ValleyTrajectoryBuilder.BuildCandidateBed` walks a transverse depth `t` in `[0,1]` from crest to
flank foot:

```
arc   = FloorArcAt(corridor, spurs, t)          // mean of the two bounding spurs' ArcAt(t)
width = FlankHalfWidth(massif, arc, flankSign)
p(t)  = massif.PointAt(arc) + normal(arc) * (flankSign * t * width)
```

`ValleyNetwork.RayOf` then takes the candidate ray as the tangent of the **last two samples**:
`dir = normalize(bed[n-1] - bed[n-2])`.

### 1.2 Defect 1 — the ray is sampled exactly where the lateral rate is zero

`SpurCurve.ArcAt(t) = AttachS + DriftArc * (3t^2 - 2t^3)`.

Its derivative is `6 * t * (1 - t) * DriftArc`, which is **identically zero at t = 1**.

`FloorArcAt` is the arithmetic mean of the two bounding spurs' `ArcAt`, so the bed's along-axis rate
is zero at `t = 1` as well. The only surviving term in the terminal tangent is the transverse one,
`normal * width`. **Every candidate ray is therefore the local outward flank normal.**

Measured, seed 110201, corridor 2:

| quantity | value | direction |
|---|---|---|
| bed head to toe chord | (1903.9,1318.1) -> (1955.6,1083.3) | -77.6 deg |
| candidate ray (endpoint tangent) | (0.087, -0.996) | -85.0 deg |

The bed carries 7.4 degrees of lateral behaviour that the ray discards. Adjacent normals on a
near-straight axis differ by a fraction of a degree — V2 and V3 differ by **0.3 deg** over a 169 m
arc gap, implying an axis radius of about 32 km. Two rays 128 m apart closing at 0.3 deg need
roughly 24 km. The world is 2800 m across.

### 1.3 Defect 2 — the bed is positioned by divides that do not exist

`PrimarySpurBuilder.DrawDrift` bounds each spur's drift so that adjacent toes **cannot** approach:

```
clearance = (station - lowerNeighbour) * 0.5f;
low = Mathf.Max(low, previousToeArc + clearance - station);
```

Adjacent divides are non-converging by construction. The mid-line of two non-converging divides is
itself non-converging. **Zero merges is a theorem of the current bed construction, not an artefact of
the seed set.**

But the divides are also *finite*, and the bed ignores that. `ArcAt(t)` is a closed-form curve
defined for every `t` in `[0,1]`; the spur it describes stops at `ReachMeters`. Measured toe
fractions, seed 110201 (`ToeFraction = ReachMeters / FlankHalfWidth(AttachS)`, the quantity Slice B2
already computes):

| spur | S0 | S4 | S6 | S3 | S5 | S1 | S2 |
|---|---|---|---|---|---|---|---|
| toe fraction | 0.741 | 0.787 | 0.797 | 0.809 | 0.893 | 0.898 | 1.053 |

**In six of seven cases the divide ends before the flank foot.** For the last 10 to 26 per cent of
the descent the corridor floor is held mid-way between one real ridge and one that Stage 3B never
built.

### 1.4 Consequence

The two defects compound. The bed follows a phantom divide for the last stretch, and the ray is then
read at the one point where the construction has zeroed the lateral term. The measured Slice B2
result follows directly: 21 diverged, 15 met at sea, 4 met past the domain edge, 0 merges — with
meeting points at 462 m, 1036 m, 1246 m and 3280 m against an apron 200 to 500 m deep.

---

## 2. Local physical data already available

No new input is needed. Everything below is Stage 2B or Stage 3B output already in hand where the
bed is built:

- `SpurCurve.ReachMeters` — where the divide actually ends.
- `SpurCurve.AttachS`, `SpurCurve.ArcAt(t)`, `SpurCurve.DriftArc` — where the divide runs.
- `FlankHalfWidth(massif, s, flankSign)` — the flank's depth, hence the divide's depth in the bed's
  own `t` coordinate.
- `ValleyCorridor.LowerSpurId` / `UpperSpurId` / `SInterval` — which divides bound the corridor, and
  which side is an attachment-interval edge rather than a divide at all.

`ToeFraction = ReachMeters / FlankHalfWidth(AttachS)` is the same expression Slice B2 already uses to
order its events. This spike reads it one level earlier, in the geometry that produces the events.

---

## 3. The proposed change

> **A divide stops shaping the valley floor where the divide stops.**

The floor is the mid-line of its two bounding divides while both exist. Where a bounding divide has
ended, the ground it occupied is no longer raised, and the floor descends into it — moving *toward
the dead divide's own line*, not toward the neighbouring channel.

Each bounding side reports where its divide is and how much of its ground it has released:

```
tToe = clamp01(spur.ReachMeters / FlankHalfWidth(massif, spur.AttachS, spur.FlankSign))

if tToe >= 1 or t <= tToe:                     // the divide is still there
    boundArc = spur.ArcAt(t);   vacated = 0
else:                                          // the divide has ended
    u        = (t - tToe) / (1 - tToe)
    boundArc = spur.ArcAt(tToe)                // its line stops advancing where it stops
    vacated  = u * u * (1 - tToe)              // C1 at the join, live slope at the foot
```

and the floor is the mid-line plus the ground released on each side:

```
mid   = (lowerArc + upperArc) / 2
floor = mid + lowerVacated * (lowerArc - mid) + upperVacated * (upperArc - mid)
```

An attachment-interval edge is not a divide: it reports the interval bound and `vacated = 0`.

### 3.1 Why every quantity is forced

- **`tToe`** — an existing measured quantity, not a threshold.
- **the released share, `1 - tToe`** — the lateral response is a descent process, so it is
  proportional to the descent that remains. A divide dying at the foot leaves no room to respond;
  one dying half way leaves half the flank. Both sides of the equation are fractions of the same
  flank.
- **freezing `boundArc` at `spur.ArcAt(tToe)`** — not a choice either: the divide's line cannot go
  on advancing after the divide has ended. This also makes the containment a proof rather than an
  argument (§4).
- **`e = u * u`** — the minimal polynomial satisfying the two requirements: `e(0) = e'(0) = 0` (C1
  join at the toe, no kink) and `e'(1) != 0` (the terminal tangent must be alive, which is exactly
  what defect 1 destroys). The codebase's usual smoothstep fails the second: `smoothstep'(1) = 0`
  would reintroduce the defect at the very sample the ray reads.

**No new setting is introduced, and none is needed.**

### 3.2 Why this is convergence and not attraction

- The target is a **divide**, not a channel. Both neighbours move toward the same dead ridge line
  from opposite sides because that ridge is what used to keep them apart. Neither reads the other's
  bed, ray, rank, chain position or topology.
- It is conditional on physics: spur S2 (toe fraction 1.053) runs the whole flank, so the corridors
  either side of it receive **no** migration and stay exactly as straight as they are today.
- It is symmetric and local: a corridor with two dying bounds migrates toward both and the two
  migrations partly cancel — the correct result, and one no attraction force would produce.
- It cannot manufacture a merge. `vacated <= 1 - tToe < 1`, so the floor **never reaches** the dead
  divide's line: the two floors approach, never coincide and never cross. Whether their rays meet,
  and whether they meet on valid ground, remains entirely Slice B2's decision.

The containment is a proof, not a hope. Each term is strictly under one half-gap and the two point
opposite ways, so `floor` stays strictly inside `(lowerArc, upperArc)` no matter how many of the
corridor's bounds have ended.

### 3.3 Magnitude, predicted before measuring

The terminal lateral rate is `2 * share * gap` per unit `t` against a transverse rate of
`flankWidth`, so the added half-angle is `atan(2 * share * gap / flankWidth)`:

| tToe | share | gap (m) | flank (m) | added half-angle |
|---|---|---|---|---|
| 0.741 | 0.259 | 27 | 240 | 3.3 deg |
| 0.797 | 0.203 | 65 | 240 | 6.3 deg |
| 0.898 | 0.102 | 64 | 240 | 3.1 deg |

Six to thirteen degrees per pair on top of what exists — the order that turns a 460 m closure into a
few hundred metres, that is, into the apron. Not enough to force anything; some pairs will still
fail, and they should.

---

## 4. Invariants that must not move

| invariant | why it is safe |
|---|---|
| Slice A rank (`ReliefVolumeCum`, `HeadCrestMeters`, order) | computed from `SWidth`, `LengthMeters`, `FlankHalfWidth(HeadS)`, `CrestAt(HeadS)`. No bed input. |
| Adjacency and chains | built from shared spur ids only. |
| Spur-toe events and their order | `ReachMeters / FlankHalfWidth(AttachS)`. Untouched. |
| Slice B2 topology rules | unchanged; only its geometric input changes. |
| Topology-freeness of the bed | the new term reads `ReachMeters`, `ArcAt`, `AttachS` and massif geometry. No merge, no `DownstreamId`, no termination. |
| Bed head equals `HeadS` | `t = 0 <= tToe` for any live divide, so `e = 0`. |
| Floor stays inside its corridor | each migration is bounded by its own side's gap, so the sum stays strictly inside `(arcLower, arcUpper)`. |
| Single source of truth | the carved trajectory and the candidate use the same function; both move together, by construction. |
| Stage 1, 2A, 2B, 3A, 3B | not read differently and not modified. |

---

## 5. What the implementation actually became

Two things in §3 did not survive contact with the code, and both were caught by the tests rather
than by argument.

**Freezing the ended divide's line broke C1.** Holding `boundArc` at `spur.ArcAt(tToe)` past the toe
also freezes the mid-line, so the floor's along-axis slope dropped from 34.6 m per unit `t` to zero
at the toe — a visible break in the tangent. The bound line therefore stays `spur.ArcAt(t)`
throughout: a spur tapers out along its own line rather than stopping dead, and the release is
measured against the live mid-line. The containment proof is unaffected — each term is still under
one half-gap and they still point opposite ways.

**The first easing cancelled itself.** With `vacated = u^2 * (1 - tToe)`, the derivative at the foot
is `2u`, which is `2` for **every** ended divide regardless of when it ended. A corridor with two
ended bounds therefore contributed `2 * (lower - mid) + 2 * (upper - mid) = 0` — an exact
cancellation, and the terminal tangent stayed as dead as before. Measured on seed 110201 V1: 0.39 m
per unit `t`. Replacing it with the square of the descent since the divide ended,
`vacated = (t - tToe)^2`, gives a terminal slope of `2 * (1 - tToe)`, which remembers when the divide
died, so two divides ending at different depths no longer cancel. It also folds the released share
into the same expression — `(1 - tToe)^2` at the foot — so the separate `share` factor is gone.

Final rule, in full:

```
tToe = clamp01(spur.ReachMeters / FlankHalfWidth(massif, spur.AttachS, spur.FlankSign))
boundArc = spur.ArcAt(t)
vacated  = t > tToe ? (t - tToe)^2 : 0            // an interval edge: boundArc = edge, vacated = 0

mid   = (lowerArc + upperArc) / 2
floor = mid + lowerVacated * (lowerArc - mid) + upperVacated * (upperArc - mid)
```

One function, `ValleyTrajectoryBuilder.BoundArc`, and one changed line in `FloorArcAt`. No new
setting, no new constant, nothing else in the pipeline touched.

---

## 6. Result

### 6.1 Topology, per canonical seed

| seed | corridors | events | **merged** | diverged | ocean | outside domain | trunks | max depth |
|---|---|---|---|---|---|---|---|---|
| 110201 | 9 | 7 | **1** | 3 | 3 | 0 | 8 | 1 |
| 424242 | 9 | 7 | 0 | 3 | 4 | 0 | 9 | 0 |
| 20260905 | 11 | 9 | **1** | 4 | 3 | 1 | 10 | 1 |
| 7 | 9 | 7 | 0 | 4 | 2 | 1 | 9 | 0 |
| 101010 | 7 | 5 | 0 | 2 | 1 | 2 | 7 | 0 |
| 20261231 | 7 | 5 | **1** | 2 | 2 | 0 | 6 | 1 |
| 1337, 256, 777 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Before: 0 merged, 21 diverged, 15 ocean, 4 outside domain. After: **3 merged**, 18 diverged,
15 ocean, 4 outside domain. `parallel`, `upstreamOfToe`, `foreign`, `nonMonotone`, `noGeometry` and
`alreadyOne` remain zero throughout.

The three confluences:

| seed | merge | at | distance from axis |
|---|---|---|---|
| 110201 | V0 into V1 | (1858, 916) | 397 m |
| 20260905 | V3 into V2 | (1365, 770) | 434 m |
| 20261231 | V3 into V2 | (923, 2261) | 749 m |

All three sit on dry land inside structural ownership, downstream of the separating spur's toe, and
the loser is in every case the lower-ranked corridor. No rule was relaxed to admit them: the same
four rejections that fired before still fire, and the meeting points they reject are still out at
sea (623 m to 2424 m from the axis) or past the domain edge (449 m to 812 m).

### 6.2 Geometry, 52 corridors

| measure | min | median | max |
|---|---|---|---|
| candidate ray off the flank normal | 0.19 deg | 3.66 deg | 14.87 deg |
| bend radius / corridor half-width | 0.90 | 7.63 | — |

The rays have come off the normal, which is the whole point; the tightest bend is a sustained
circular arc of 103 m radius inside a corridor 115 m half-wide, not a corner. Every bed descends
monotonically and none self-intersects.

### 6.3 Fan and starfish audit

Signed turn of each candidate ray off its own bed's chord, read along the crest, per flank:

| seed | flank -1 | flank +1 |
|---|---|---|
| 110201 | -4.4, -9.8, -11.2, -4.9, -5.3 | -6.1, 9.6, 0.1, -1.7 |
| 424242 | -4.1, -3.7, 3.5, 5.4 | -2.7, 20.3, 8.1, -3.4, -0.2 |
| 20260905 | 6.5, -2.3, 20.0, 12.1, 4.7, -2.0 | 1.0, 16.4, 1.3, 12.5, 3.0 |
| 7 | -1.0, -3.4, -0.5, 3.7 | -1.8, -4.1, -4.5, 7.6, 6.5 |
| 101010 | 3.7, -2.6, 3.1, -1.0 | 4.5, 2.3, 0.3 |
| 20261231 | -1.0, -16.6, 2.9, -1.6 | -4.6, -14.3, 4.8 |

A fan would step monotonically outward across a whole flank. Two flanks of twelve are monotone, and
both of those close rather than spread (-4.1 to +5.4 crosses zero; 4.5 to 0.3 decreases). The signs
are mixed and follow which of a corridor's own divides ended, which is what the rule says they
should do.

### 6.4 Tests

13 new tests in `TerrainGenCandidateGeometryTests`, full EditMode suite **492/492**. Nothing was
weakened or deleted; the 479 that existed before still pass unchanged, Slice B2's rules included.

---

## 7. Verdict

The straight terminal-tangent assumption was one of the two reasons topology degenerated to zero
merges, and it was a defect rather than a modelling choice: the ray was read at the one point where
`ArcAt`'s smoothstep has zeroed the lateral term. The other reason was that the bed was positioned by
divides that had already ended.

Correcting both produces genuine convergence in the space available, without weakening a single
validation and without any steering that reads a neighbouring channel. It does **not** produce a
merge at most divides: 37 of 40 adjacencies still resolve to separate channels, and they fail for
terrain reasons that are visible in the numbers. Whether three trunk confluences across six worlds
is the density the world wants is a separate question, and one for the stage that carves.
