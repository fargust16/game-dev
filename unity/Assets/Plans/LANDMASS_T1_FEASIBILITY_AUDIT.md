# Landmass → T1 Massif Feasibility Audit

A spec pass and a 29-seed measurement. No generator behaviour was changed; the only new file is a
read-only editor diagnostic. Stage 3 not started. Nothing committed or pushed.

> **Dated record.** Written 2026-09-08. Stage 3A–3E and Stage 4A have shipped since; the contract and
> the 29-seed measurements below still describe Stage 1 → Stage 2B, which is frozen.

Date: 2026-09-08 · 29 seeds (the canonical five plus the 24-seed robustness spread)

---

## 1. The contract, derived rather than assumed

Every quantity below already exists in the code. Nothing here is a new number.

| Symbol | Meaning | Source | Value |
|---|---|---|---|
| `W_min` | narrowest half-width worth calling a range | `MacroStructureSettings.MinUsableHalfWidthMeters` | 200 m |
| `G` | coastal room a massif needs beyond its half-width | `MassifCoastClearanceMeters` = `MassifSettings.CoastGateMeters` = `WorldScale.CoastShelfWidthMeters` | 200 m |
| `ρ` | length per metre of half-width | `MinLengthToWidthRatio` | 3.2 |
| `L_min` | absolute length floor | `MinAxisLengthMeters` | 700 m |
| `κ` | core length per half-width | `MinCoreLengthPerHalfWidth` | 2 |
| `σ` | axis must be on land | `CoastSetbackMeters` | 60 m |

**An island admits a T1 massif iff there exists a curve `C` inside it such that**

1. `len(C) ≥ L_req = max(L_min, ρ·W_min)` → **700 m**
2. `d(p, coast) ≥ σ` for all `p ∈ C` → **60 m inland**
3. there is a sub-arc `K ⊆ C` with `len(K) ≥ C_req = κ·W_min` → **400 m**
4. `d(p ± W_min·n(p), coast) ≥ G` for all `p ∈ K` → **both section edges 200 m clear**

Condition 4 is the whole of it, and it is worth being precise about what it is **not**. It does not
reduce to `d(p) ≥ G + W_min = 400 m`. That stronger form assumes the coastline runs square across the
section; measured, it certifies **1 of the 5 canonical seeds**, because a 1.7 km island offers about
600 m of such corridor while its axes are 800–1700 m long. The correct test is made at the two edges
of the cross-section, which is where the failure it guards against actually happens.

In plain words: **the island must hold a 700 m curve, 60 m inland, containing a 400 m stretch across
which a 400 m-wide band stays 200 m clear of the water.**

---

## 2. Method

A new read-only diagnostic, `Editor/TerrainGenLandmassFeasibility.cs`, measures each world twice:

- **The island, independently of geology.** Signed distance is rasterised at 10 m, then straight
  corridors are swept in 24 directions × ~200 parallel lines, applying conditions 1–4 directly. A
  straight corridor is a lower bound on what a curved axis could use, which is what a feasibility
  claim needs: if the best straight strip cannot hold a massif, no axis can.
- **Every plate boundary, by the same test** — convergent or not, chained or not, accepted or not.
  The gap between the two measurements is the classification.

It also counts, per world, how many chains Stage 2A discovered against how many its acceptance kept,
and how many boundaries with adequate room were never carried by any candidate at all.

---

## 3. Results — 29 seeds

```
  seed        area  inner  inscribed   bestCore  bestAxis   bndCore  cnvCore  axes  chains  unchained  class
     110201   2.82   1.30        605       1500      2330       590      590     1  1/3   0/1   -
     424242   2.82   1.20        639       1480      2190       803      803     1  1/5   0/1   -
   20260905   3.02   1.50        750       1470      2240       615      615     1  2/4   0/1   -
          7   2.96   1.13        569       1290      2380       667      667     1  1/4   0/0   -
       1337   2.79   1.14        622       1280      2440       618      358     0  2/2   0/1   C
          1   3.02   1.31        626       1410      2380       476      319     1  1/5   0/1   -
          2   3.03   1.34        641       1390      2320       865      865     1  4/5   1/1   -
          3   3.13   1.61        714       1670      2190       765      617     1  1/2   0/1   -
         17   2.97   1.29        665       1260      2530       449      419     1  1/3   0/1   -
         42   2.95   1.33        682       1420      2260       725      725     1  1/6   0/0   -
         99   2.80   1.18        634       1340      2450       447      447     1  2/3   0/0   -
        256   2.81   0.99        601       1250      2190       508      330     0  1/3   0/1   C
        777   2.99   1.29        570       1150      2400       537      356     0  2/3   0/1   C
       1024   2.65   1.26        607       1400      2370       400      400     1  1/4   0/0   -
       4096   2.69   1.19        626       1470      2290       789      789     1  1/6   0/0   -
       9091   2.61   0.97        597       1080      2030       447      447     1  1/5   0/0   -
      12345   2.93   1.19        634       1400      2340       499      499     1  2/3   0/1   -
      31337   2.73   0.74        410        440        760       389      389     0  1/4   0/0   B
      65535   2.71   1.30        699       1450      2430       640      434     1  2/3   0/1   -
     101010   3.06   1.10        602       1250      2380       456      456     0  1/5   1/1   D
     202020   3.01   1.42        729       1390      2290       749      749     1  1/7   2/2   -
     314159   2.87   1.03        556       1130      2440       686      686     1  1/5   0/0   -
     500001   2.87   1.29        718       1300      2180       545      545     1  3/3   2/3   -
     654321   3.16   1.25        587       1470      2190       417      417     1  1/5   0/0   -
     888888   3.12   1.51        727       1450      2210       759      759     1  1/6   0/0   -
    1000003   3.07   1.59        748       1390      2250       507      499     1  1/6   0/1   -
    1234567   2.98   1.54        781       1480      2170       708      657     1  1/4   1/2   -
    7654321   2.72   1.14        665       1260      2170       655      655     1  1/5   0/0   -
   20261231   2.80   1.43        712       1560      2140       793      793     0  1/4   2/2   D
```

`area` km² · `inner` km² of island ≥ 200 m from the coast · `inscribed` m to the deepest point ·
`bestCore` / `bestAxis` the island's own corridor (needs 400 / 700) · `bndCore` / `cnvCore` best core
on any / any convergent plate boundary · `chains` accepted / discovered · `unchained` roomy convergent
/ roomy boundaries no candidate ever carried.

### Classification

| Class | Count | Seeds |
|---|---|---|
| **A** — genuinely small island | **0** | — |
| **B** — normal island, unlucky shape | **1** | 31337 |
| **C** — a place exists, but no geology points at it | **3** | 1337, 256, 777 |
| **D** — a valid axis exists and is chosen wrongly | **2** | 101010, 20261231 |
| **E** — physically impossible | **0** | — |

---

## 4. What this says

**Every island satisfies the landmass contract.** Areas 2.61–3.16 km², inscribed radius 410–781 m,
island-wide corridor **440–1670 m of core** against the 400 m required and **760–2530 m of axis**
against the 700 m required. Even 31337, the worst world in the spread, clears both thresholds.

**There is therefore no case for reopening Stage 1.** The user's decision rule was: if the failing
worlds are small islands, leave things as they are; if they are normal islands whose geometry cannot
hold a mountain, revisit Stage 1's contract. Neither applies. Class A and class E are both empty.

### Correction to the previous pass

The Stage 2A/2B contract report concluded that seed 1337's island "cannot host a T1 massif" and
escalated it as a specification decision. **That was wrong.** Measured here, 1337's island offers
1280 m of core and 2440 m of axis, and one of its own plate boundaries has **618 m of core** — half
again what a massif needs. What fails is that this boundary's convergence sits below the 0.25
threshold while the convergent ones have only 358 m. The specification is not the problem; the
interaction of two Stage 2A rules is.

### The measured defect (class D)

Chain acceptance sorts candidates by **raw length** and is **exclusive**: the longest chain marks its
boundaries used, and every shorter chain sharing one is discarded — all decided before certification
runs. Across the 29 worlds, **40 of 123 discovered chains survive that ranking (33 %)**.

On the two class-D worlds this costs the mountain outright, and it is measured rather than inferred:

- **101010** — one convergent boundary with 456 m of core, **never carried by any candidate**, while
  4 of 5 chains were discarded.
- **20261231** — two convergent boundaries with 793 m of core, **both never carried by any
  candidate**, while 3 of 4 chains were discarded.

The same loss happens on worlds that keep their mountain anyway: 202020 discards two roomy boundaries
of two, 500001 discards two of three, 1234567 one of two. The ranking is throwing away good geometry
systematically; on most worlds something else happens to survive.

### The other defect (class C)

Convergence and clearance are evaluated independently and nothing reconciles them. On 1337, 256 and
777 the boundary with the room is not convergent enough (`bndCore` 508–618 m against `cnvCore`
330–358 m). Both rules are individually right — a mountain needs converging plates, and a body needs
room — but no step asks for a boundary that has both.

### Class B

**31337** is the one world where the island itself is marginal: 440 m of core against 400 required,
and its best plate boundary reaches 389 m — eleven metres short. This is the case the audit was
looking for, and it is one world in twenty-nine.

---

## 5. Recommendation

**Do not reopen Stage 1. Do not start Stage 3. The next pass is a Stage 2A selection pass.**

Two changes, both inside Stage 2A, both structural:

1. **Rank and accept chains by certified outcome, not by raw length.** A chain that certifies nothing
   should not consume boundaries a chain that certifies something could have used.
2. **Let clearance participate in candidacy alongside convergence**, so a boundary is judged on
   whether it both converges and has room, rather than passing one filter and failing the other in
   isolation.

Expected effect, from the measurements above: the two class-D worlds recover outright; the three
class-C worlds become reachable if a convergent boundary with room exists once chains are no longer
discarded unexamined. Class B (31337) would remain a world without a mountain, which at one in
twenty-nine is a reasonable thing for a procedural world to be.

No specification number needs to move. No Stage 1 change is implied.

---

## 6. Scope

```
Stage 1: untouched          Stage 2B: untouched        Terrain bake: untouched
Stage 2: untouched          Stage 3: not started       Scene: untouched
Stage 2A: untouched         Hydrology: untouched       Packages: untouched
```

One new file: `Assets/Scripts/Editor/TerrainGenLandmassFeasibility.cs` — read-only, editor-only, not
part of generation. No commits. No pushes.

---

## 7. Verdict

The terrain generator's **Stage 2A → 2B contract is sound**; the shear failure it was built to close
stays closed. What the 79 % massif rate measures is **not** a landmass limitation and **not** a
specification gap — it is Stage 2A discarding two thirds of its own candidates on a ranking rule that
predates certification.
