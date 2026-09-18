# Procedural Terrain — Stage 4 Island Relief Design (Stage 4A)

**Status:** **Implemented dump-first (revision R1).** Variant C6 was coded 2026-09-16; R1's delta
(inland slope budget, derived plate widths with certified junctions, budgeted knolls, apron projection
domain, corrected S8/S10/S11) is in the same files. The layer is **not** wired into
`TerrainAuthorityField`. Production heights remain `StructuralElevation` (H2).
**Date:** 2026-09-16
**Baseline:** C6 implementation committed as `f7a7c56`; documentation follow-up `9573b0d`.
**Answers:** [NATIVE_ISLAND_SHAPE_PREFLIGHT.md](NATIVE_ISLAND_SHAPE_PREFLIGHT.md) §16 step 1.
**Related:** [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md),
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md),
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md](UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md),
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md).
**Change record:** [PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md](PROCEDURAL_TERRAIN_STAGE_4_ISLAND_RELIEF_DESIGN_REVISION.md)
(R-1…R-10). This file is the living contract; the revision stays as the argument.

> **R1 is folded in.** C6's "all terms ≥ 0 removes ring lips" is false (positivity does not bound the
> inland derivative). The live formula is `R4A = M(d) + Γ·(P + K) + A` with τ-derived pair widths,
> certified junctions, knoll amplitude caps, and an apron domain `W_eff`. S8-A/M/C, S10 (ungated
> `P_A`), S11-T/O and S12 are the acceptance tests. Hierarchy (§4), C6 over A/B/N (§3), knoll/apron
> *shape*, determinism and the tile contract (§13, §14) are unchanged. Production stays disconnected.

Claim tags as in the other TerrainGen documents: **[FACT]** read from code or measured on the production
path, **[PROTO]** measured on the offline prototype described in §1 (real Stage 1/2A/2B structure,
approximated geometry — read the caveats), **[CONCLUSION]** follows from the above, **[PROPOSAL]** the
design decision, **[OPEN]** deliberately unresolved.

**The question:** what deterministic structural relief must Stage 4A add so that the island stops
reading as an extruded coastline and starts reading as a fantasy island, without becoming procedural
noise?

**Short answer.** Three positive-only structural terms plus a 1-D coastal floor, each owned by one
existing geological structure (or by Stage 1's own profile), gated so the land mask cannot move:

1. **Coastal floor `M(d)`** (Stage 1 profile): a monotone 1-D integral that gives `H1 + M` an inland
   slope of at least `τ = InteriorRise / InteriorScale = 0.05`. This is the **ring-lip** proof, not a
   landform.
2. **Plate-block uplift** (macro plates, ~1 km): each land plate stands at a rank level with a
   **lowland reservation** (≥ 20 % of land at level 0); pair edge widths and junction radii are derived
   from τ and certified. This is what creates **uplands and lowlands**.
3. **Knolls** (crustal blocks, ~275 m): resistant cores of selected crustal cells, only on raised
   plates, off the massif body, amplitude-capped by the slope budget left over. This is the
   **rolling-hill** layer. The analytic `g_P` bound currently leaves **zero knolls** on the seven
   canonical seeds (report, not a retune).
4. **Massif apron** (massif frame): a talus-and-piedmont fill around the Stage 2B foot, domain-limited
   to `R_axis − δ` so `MountainMassif.Project` stays unique. This is the **foothill** layer.

`H4A = H1 + U2 + R4A`, `R4A = M + Γ·(P + K) + A`; future `H = H4A − C3F`. No term lowers land. Noise
has no role in Stage 4A v1. Unity dump on the seven canonical seeds: 70–110 m band 15–26 %, S8-A/S8-M
= 0, S10 beyond-domain = 0, S11-T/O and S12 pass. What Stage 4A cannot do — and must not be asked to
do — is break the massif's ring wall (Stage 3F / 2B), drain massif-foot pits inside `Z_M ∪ L_M`
(sweep residual, Stage 3F / 4B), or give a no-massif world a landmark (Stage 4B).

---

## 0. Contents

1. Method and evidence
2. Existing structures as causal sources
3. Candidate architectures, compared on real structure
4. Chosen design: the geographic hierarchy
5. Lowlands and uplands — plate-block uplift
6. Rolling hills — knolls
7. Massif foothills — the apron
8. Worlds without a massif
9. The boundary with Stage 3F
10. The role of noise
11. Numerical targets
12. Closed depressions
13. Determinism and streams
14. Tile and lattice contract
15. API
16. Expected visual result
17. Does Stage 1 have to change?
18. Contradictions in the current documents
19. Acceptance criteria
20. Risks
21. Final recommendation

---

## 1. Method and evidence

| Evidence | How it was obtained |
|---|---|
| Code | `CrustalPartition`, `LandmassBuilder`, `Stage1LandmassGenerator`, `CoastHeightModel`, `ProfileCurve`, `TerrainGenSettings`, `MacroPlates`, `MacroStructureSettings`, `Stage2AMacroStructure`, `MountainMassif`, `MassifUpliftModel`, `MountainUpliftModel.SmoothMax`, `MassifSettings`, `StructuralElevation`, `RidgeSettings`, `ValleyTrajectories`, `TerrainAuthorityField`, `HydrologySettings`, `DepressionFill`, `HydrologyGrid` |
| Documents | all listed in the header, plus `PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md` §6–§8 |
| Native baseline | the preflight's raw `TerrainData` dumps for 110201 and 424242; a new `OverworldTerrainValidation.RunBatch -terrainRaw` run for **1337** (signature `12c54a355b46`, massifs 0, authority 23.6 s) |
| Structure | fresh batch runs of existing tools only: `TerrainGenMacroDump.DumpMacroBatch` (110201, 424242, 1337, 256, 777), `TerrainGenDump.DumpStage1Batch` (1337), `TerrainGenValleyTrajectoryDump.DumpBatch` (110201, 424242); the preflight's Stage 1 and Stage 2B reports |
| Prototype | an offline numpy model, outside the repository, of every candidate in §3 on the real structure (below) |

**Repository hygiene.** The one bake overwrote the four gitignored tile assets. They were backed up
first and restored byte for byte (MD5 of all four assets and of `Overworld_Generator.unity` identical
before and after). `git status --porcelain` was empty before and after every run. All scripts, dumps
and images live in the session scratchpad, not in the project.

**The prototype, and what it is not.** It rebuilds, per seed, on the 1025² lattice:

- `H1` exactly from `CoastHeightModel` over an exact Euclidean distance transform of the baked land
  mask (reconstruction error on the island floor: p99 0.38–0.40 m);
- `U2 = H_baked − H1` (the massif, as shipped);
- the **real** crustal cells (48 / 47 / 45 land cells, mean ⌀ 273–281 m) and the **real** macro plates
  and convergent boundaries, decoded from the Stage 1 and Stage 2A dump images at lattice precision;
- the massif frame from the Stage 2B station table; the **real** Stage 3C final trajectories from the
  trajectory dump.

It then evaluates each candidate relief and measures the preflight's metrics plus four new ones (§11),
with a priority-flood depression scan. It reproduces the preflight baseline exactly (110201: land
p5…p99 25/36/50/61/87/314 m, 70–110 m 4.6 %, >45° 6.1 %, reachable 92.3 %).

Caveats: plate levels and junctions come from a raster, not from `MacroPlates` segments; blends are
Gaussian approximations of the closed-form functions proposed below; the knoll stream is a stand-in;
only three seeds have raw heights. **Every number tagged [PROTO] is design evidence, not an
acceptance result.** The implementation step re-measures everything in C# through its own dump.
A simple voxel-space renderer was also tried for eye-level views; untextured, it was not informative
and is not used as evidence (§16).

---

## 2. Existing structures as causal sources

What TerrainGen already publishes, whether each piece exists on every world, and what it can honestly
*cause*.

| Structure | Owner / type | Scale on the canonical islands | On no-massif worlds | Can cause | Stage 4A use |
|---|---|---|---|---|---|
| Coast distance, Stage 1 datum `H1` | `CoastQuery`, `CoastHeightModel` | 0–640 m inland; datum 24 → ~73 m | yes | the T0 datum, the shore gate | **gate only** |
| Coastal band | Stage 1 profile | `d < 200 m` is **54–59 % of all land** [PROTO] | yes | — (a constraint) | the gate's width |
| Crustal partition | `CrustalPartition` (sites, centroids, cell counts, adjacency) | 200 m spacing; 45–48 land cells, ⌀ ~275 m | yes | hill units ("resistant block cores"), block mosaic | **knoll anchors** |
| Land set, land raster | `LandmassBuilder.Result` | 10 m raster | yes | plate land area and coast frontage | plate statistics |
| Growth order of the landmass | private to `LandmassBuilder.Grow` | — | — | craton vs accreted terrane | **rejected**: not published; reproducing it duplicates Stage 1 |
| Macro plates | `MacroPlates` (sites, drift, boundaries with `Convergence`, junctions, stress axis) | 1 100 m spacing; 3–4 plates ≥ 0.15 km² of land per island; 1–2 on-land plate junctions on each of the three measured islands | **yes** — 1337: 4 plates on land, 2 convergent boundaries reach land | regional uplands and lowlands, block edges, the island's grain | **plate levels, edges, junctions** |
| Macro axes | `MacroAxis` | 999 / 1 303 m | none accepted | massif location | through the massif only |
| Massif geometry | `MountainMassif` (`Project`, `HalfWidthAt`, `Asymmetry`, `SteepSide`, `CrestAt`) | L 1.0–1.3 km, W ~280 m, crest 284–293 m | absent | apron shape and anisotropy | **apron frame** |
| Massif uplift `U2` | `MassifUpliftModel.UpliftAt` | 0–293 m; flank 58–68° | 0 | the surface the apron fills against | **read-only** |
| Primary spurs (3B) | `RidgeSkeleton` | 7 per massif world | none | future foothill ridges, ramps through the wall | **not read** (Stage 3F's) |
| Valley corridors, final trajectories (3C) | `ValleyTrajectoryNetwork` | 9 per world, 481–1 263 m, half-width 18–115 m, 43 of 46 end at the coast | none | future valleys | **validation only** (§9) |
| Secondary spurs (3D) | `SecondaryRidgeSkeleton` | ribs | none | flank texture | not read |
| Structural basins (3E) | `StructuralBasinSet` | 5 per world, outlets 61–97 m | none | future basin floors | not read; outlets validated (§9) |

### Crust blocks versus macro plates

| | A — crust-block relief | B — macro-plate relief |
|---|---|---|
| Natural scale | 275 m cells | ~1 km plates, 3–4 per island |
| What it can mean | a hill, a block core | a district: an upland, a lowland |
| Information it carries beyond position | **none** — a per-cell value has to be *drawn*, so a smooth blend of it is value noise on a blue-noise lattice | drift, convergence, boundary type, land share, junctions — a value can be *derived* |
| Regions it can express | none larger than a few cells without a second rule (which is noise or plates) | exactly the 2–4 regions an island this size needs |
| Present without a massif | yes | yes |
| Alone, measured [PROTO] | ±12 m blocks: **4–5 closed depressions** per seed (up to 1.74 m), 70–110 m unchanged (5.7 / 6.2 / 0.4 %), up to 14 m of adverse rise along Stage 3C trajectories | plates + convergent belts: 70–110 m 17.5 / 18.8 / 23.2 %, but floor local relief only +0.7–1.0 m and one strike-trough depression on 1337 |

**[CONCLUSION] The combination is right, with one owner per scale.** Plates are the only structure that
can *cause* uplands and lowlands; used alone they leave the interior smooth. Crust cells are the only
structure at hill scale; used alone they are noise. So plates decide *where the ground stands high*,
and crust cells decide *where, on high ground, a knoll stands* — the crust layer never decides a
region, and the plate layer never makes a hill.

---

## 3. Candidate architectures, compared on real structure

All candidates share the shore gate. Numbers are 110201 / 424242 / 1337. Depressions count components
at least 0.25 m deep and 100 m² in area.

| | A — crust-block | B — macro-plate | C — hybrid structural (chosen, final form C6) | D — base-level distance | N — FBM control |
|---|---|---|---|---|---|
| **Mechanism** | per-cell offset ±12 m, smooth blend | per-plate levels + two-sided uplift belts along convergent boundaries | one-sided plate-block steps + knolls on crust cells + massif apron fill | height = `g(weighted geodesic distance to the coast)`, weight = rock resistance | 3 octaves at 600/300/150 m, ±15 m |
| **Advantages** | fine hill scale | legible regions, causal, cheap | every scale owned by a structure; pit-free by construction rules; works without a massif | no closed depression by construction; drainage-first | trivial |
| **Disadvantages** | value noise; enclosures | interior stays smooth; two-sided belts dig strike troughs | more parts; calibration needed per scale | global raster solve (not closed-form); faceted creases; eikonal anisotropy threatens isotropy (G7); re-profiles the Stage 1 datum in disguise | is noise |
| **Determinism** | needs a per-cell stream deciding shape | none needed | one bounded stream (knolls) | none needed, but raster-order sensitive | stream decides everything |
| **Visual character** | hummocky patchwork | broad ramps, "raised island" | districts with edges, rolling uplands, foothills | ruled roof facets | oatmeal |
| **Stage 3F compatibility** | adverse rise 11 / 14 m along trajectories | 0.2 / 0.1 m | 5.7 / 3.4 m (knolls only, ≤ 7.2 m incision) | good | 25 / 18 m |
| **Complexity** | low | low | medium | high | trivial |
| **Noise-appearance risk** | high | low | low | low | certain |
| **70–110 m** [PROTO] | 5.7 / 6.2 / 0.4 % | 17.5 / 18.8 / 23.2 % | **24.6 / 20.3 / 24.4 %** | not prototyped | 4.9 / 6.8 / 0.2 % |
| **Depressions** [PROTO] | 4 / 4 / 5 | 0 / 0 / 1 | **0 / 0 / 0** | 0 by construction | 36 / 35 / 40 (to 6.1 m) |
| **Coast-aligned gradient** (baseline 97 / 95 / 99 %) | 48 / 47 / 41 % | 65 / 65 / 51 % | **52 / 48 / 65 %** | — | 31 / 28 / 25 % |

**Why C, and why not D now.** D is the most principled answer to "no closed depression" and is kept as
the fallback if C fails the coast-alignment criterion in C#. Today it would add a global solver, a new
isotropy risk and a hidden rewrite of the Stage 1 datum, for a guarantee C already meets by construction
rules plus a scan. **N** confirms the generator design's ban numerically: it is the only candidate that
"breaks the roof" without structure, and it pays with dozens of pits and 25 m of adverse rise along the
valleys Stage 3F must carve.

### How C reached its final form (what the prototype taught)

| Step | Change | Finding |
|---|---|---|
| C1 | belts along convergent boundaries (two-sided), per-plate levels, knolls on every cell, radial apron | a belt off the island's medial axis digs a **strike trough** between itself and the datum's crest (the 1337 depression sat exactly there); apron contours concentric; 47 knolls — an egg carton |
| C2 | belts removed: convergent boundaries become the **edge of an uplifted block** (one high side, no trough); gate moved to 150–400 m | a second, coast-parallel ramp inland; flat ground (< 5°) fell to 18–32 % of land |
| C3 | gate back to the Stage 1 coastal rise; talus apron; causal plate levels | 45–47 knolls; one depression on 110201 (apron), one on 1337 (plates) |
| C4 | attribution → two construction rules: apron reach **constant along outward rays**; plate contrast **relaxed at on-land junctions**; knolls only on raised plates | depressions 0 / 0 / 1 (0.43 m); knolls 15–22 |
| C5–C6 | narrower apron, smaller knoll footprint, 450 m junction relaxation, plate uplift 18 m | **0 / 0 / 0**; flat share partly restored; adverse rise along trajectories traced to knolls alone |
| C6-nm | no-massif amplitude ×1.4 (25 m) on 1337 | 70–110 m 24.4 %, still 0 depressions; at ×1.6 (29 m) a 449 m² / 0.27 m depression appears |

---

## 4. Chosen design: the geographic hierarchy

```
T1   massif (Stage 2B)                      crest 200–330 m over 1.0–1.3 km          FROZEN
 │
 ├── foothills / apron (4A)                 +25–60 m at the toe, 110–280 m reach     massif frame
 │
T0+  uplands (4A)                           +18 m (+25 m without a massif)           macro plates
 │     └── knolls (4A)                      4–7.2 m, 160 × 100 m at half height      crust cells
 │
T0+  lowlands (4A)                          the datum itself, plates left unraised   macro plates
 │
T0   coastal / base floor (Stage 1)         24 → 73 m over 0–640 m                   FROZEN
```

Per level, as proposed (numbers justified in §5–§7 and §11):

| Level | Spatial scale | Height above the datum | Share of land | Slope range | Structural cause | Forbidden | Transition to the neighbour |
|---|---|---|---|---|---|---|---|
| **Coastal floor** (T0) | 0–200 m band along every shore | 0 (Stage 1 only) | the part of the 54–59 % coastal band on lowland plates | 3–14° (unchanged) | coast datum | any relief term at or seaward of the shore | the gate `Γ`, following Stage 1's own coastal rise |
| **Broad lowland** | a whole plate, 0.4–1.0 km², opening onto its own coast | 0–6 m (level ≤ 0.34) | 25–35 % | 0–5° interior, datum slope | lower plate(s) of the collision | a plate with no coast frontage; any lowering | block edge, 150–250 m wide, ≤ 8° added |
| **Upland** | a whole plate, 0.5–1.0 km² | 9–18 m (25 m on a no-massif world) | 40–60 % | 0–5° on top, 5–15° on edges, 10–20° on its coastal band | upper plate(s) of the collision | inside the coastal gate at full strength; a notch at an interior junction | edge to lowland; apron against the massif; coastal rise to the sea |
| **Rolling knolls** | one per selected crust cell, 160 × 100 m at half height, ≥ 200 m apart | 4.0–7.2 m | footprints (> 1 m) ≈ 10–20 % [PROTO 10 / 18 / 11 %] | ≤ 6° added | resistant core of a crust block | lowland plates, the outer coastal band (`Γ < 0.5`), taller than 7.2 m | compact kernel, zero slope at its rim |
| **Foothill apron** | 110 m (steep flank) · 180 m (gentle flank) · 280 m (along strike, beyond the ends) | 0.09 × crest at the toe (~26 m), fill capped at 0.18 × crest (~52 m) | ≤ 25 % [PROTO 17 / 25 %] | 22° talus at the wall → ≤ 16° (gentle) / ≤ 25° (steep side) → 0 | massif frame and flank asymmetry | above 0.18 × crest; any shape that is not monotone along outward rays | concave fillet into the flank (10 m blend), zero slope at the outer edge |
| **Massif** (T1) | Stage 2B | — | 10–13 % | 45–68° | Stage 2B | any 4A change to its body | — |

**Tier separation.** `WORLD_RELIEF_PRINCIPLES` §1 asks adjacent tiers to differ by ≥ 2.5× and to leave
nothing in between. Stage 4A adds **no landform tier**: nothing it makes has a wall, a shoulder or a flat
top standing on lower ground. It varies the floor T2 and T3 stand on, with slopes bounded far below the
angle of repose. Inside the floor relief the ratios still hold: upland step 18–25 m ≥ 2.5 × knoll
≤ 7.2 m; T1 crest 284 m ≥ 5 × apron fill ≤ 52 m. A T2 plateau (18–40 m, 60–80° wall) and a T3 outcrop
(5–12 m, steep) remain distinguishable from uplands and knolls by *slope*, which is what the eye reads.
§18 records the clarification the principles need.

---

## 5. Lowlands and uplands — plate-block uplift

### 5.1 The mechanism

```
P(x) = PlateUpliftMeters · m_world · Λ(x)
```

- `Λ(x) ∈ [0, 1]` is the **level field**: every land plate `k` carries a level `λ_k`; inside a plate
  `Λ = λ_k`; across a plate boundary the level blends with a smoothstep of the perpendicular distance to
  that boundary segment. Pair half-width
  `w_kj = max(PlateEdgeHalfWidthMeters, 1.5·U·m·|λ_k − λ_j|/(2τ))` so the steepest edge descent is
  ≤ τ (**R-2**). `PlateEdgeHalfWidthMeters = 110 m` is the **minimum**.
- **Junction relaxation.** Each on-land junction uses
  `R_J = max(JunctionRelaxRadiusMeters, 1.5·U·m·max|λ − λ̄|/τ)`, then a 5 m world-aligned certificate
  of `−∂(ΓP)/∂n` (≤ 8 iterations, scale radius and incident widths by `max/τ`) so the discrete inland
  derivative stays ≤ τ (**R-3**). `JunctionRelaxRadiusMeters = 450 m` is the **minimum**. [PROTO] Without
  any relaxation 1337 carries a 0.86 m depression at the apex of its lowland wedge.
- `m_world = 1` on a world with at least one massif, `NoMassifUpliftScale = 1.4` on a world without
  (§8).
- Plates with less than `MinPlateLandAreaSqm = 150 000 m²` of land are merged into the neighbour they
  share the longest on-land boundary with (they are slivers of the partition, not districts).

### 5.2 Where the levels come from (no RNG)

For every macro boundary `b` that is **convergent** (`Convergence ≥ MacroStructureSettings.ConvergenceThreshold`)
and runs at least `ConvergentMinLandLengthMeters = 150 m` over land:

- the **upper plate** is the one with more land (buoyant continental crust overrides); ties go to the
  lower plate index;
- `score_upper += L_b`, `score_lower −= L_b`, where `L_b` is the boundary's on-land length.

Levels are a **bounded rank with lowland reservation** (**R-9**): walk the sorted scores; the smallest
prefix whose land share reaches `LowlandReserveShare = 0.20` takes level 0 (the frontage rule still
applies to that prefix's first member); the remainder is ranked evenly over `(0, 1]`. Ties by plate
index. Special cases:

| Case | Rule | Why |
|---|---|---|
| no convergent boundary on land | score = land area ("craton rule": the largest plate stands highest) | an island still has an interior block |
| a single land plate | `P ≡ 0` | one block has nothing to stand above; inventing a level would only raise the datum |
| the level-0 plate has < 150 m of coast frontage | it takes the next rank up | a lowland with no outlet is a basin, and basins are Stage 3F's |

Measured [PROTO]:

| Seed | Plates (land km² → level) | Convergent on land | Upland (Λ ≥ 0.5) / lowland (Λ < 0.25) share |
|---|---|---|---|
| 110201 | 0.96 → 1.0 · 0.53 → 0.67 · 0.43 → 0.33 · 0.86 → 0.0 | massif boundary 908 m; 446 m | 55.7 % / 27.8 % |
| 424242 | 0.94 → 1.0 · 0.67 → 0.67 · 0.22 → 0.33 · 0.83 → 0.0 | 1 241 m (massif), 790 m, 429 m | 57.2 % / 27.8 % |
| 1337 | 0.89 → 1.0 · 1.02 → 0.5 · 0.81 → 0.0 | 1 006 m (south arm) | 60.8 % / 27.0 % |

On both massif worlds the rule puts the massif on the edge between an upland and a lowland, with the
massif's own steep flank (Stage 2B `SteepSide`) facing the lowland — a coherent tilted-block reading that
nobody authored.

### 5.3 Shore fade, coastal floor, and the "zero at the shore" proof

```
Γ(x) = S( clamp01( (H1(x) − SeaLevel) / CoastalRiseMeters ) )        S = smoothstep, CoastalRise = 26 m
M(d) = ∫₀^d max(0, τ − H1'(u)) du     d > 0;  M(d ≤ 0) = 0 exactly
τ    = InteriorRiseMeters / InteriorScaleMeters                       = 0.05
```

- `Γ` is exactly 0 wherever `H1 ≤ SeaLevel`, i.e. at and seaward of the shoreline, and 1 from the top of
  the coastal rise (200 m inland). It follows Stage 1's own profile.
- `M` is a 1-D table (step 0.5 m from `CoastHeightModel.HeightAt`) so `H1 + M` has inland slope ≥ τ
  wherever `H1` is already above sea. **Applied coastal floor is zero wherever `H1 ≤ SeaLevel`**, so `M`
  cannot lift a seaward or lagoon cell across the land mask (S1, S2). **R-1.**
- [PROTO] The alternative, a linear gate at 60–340 m or 150–400 m, builds a second coast-parallel ramp in
  the interior and cuts flat ground to 15–32 % of land. The chosen gate concentrates the upland's descent
  in the coastal band, where uplands meet the sea as 10–20° bluffs — the raised coast Stage 4C will
  characterise.
- At the vegetation floor (27 m, ~30 m inland) `Γ ≈ 0.04`: the sand band moves by well under a metre.

**Invariants, by construction:** `M(d ≤ 0) = 0`; applied `M = 0` wherever `H1 ≤ SeaLevel`; `Γ = 0` and
`U2 = 0` seaward; every other term is `≥ 0`; therefore `R4A = 0` exactly at and seaward of the shore,
`H4A ≥ H2` on land that `H2` already holds above sea, the land mask `{H > SeaLevel}` is bit-identical,
and no island can appear or disappear. Positivity alone does **not** prevent ring lips — that is `M` and
the τ budget.

### 5.4 Numbers and why

| Setting | Value | Justification |
|---|---|---|
| `PlateUpliftMeters` | **18 m** | ≥ 2.5 × the tallest knoll cap (7.2 m) so the two never muddy; the Stage 1 interior rise is 30 m, so an upland roughly doubles the interior relief without rivalling a 20–40 m T2 plateau standing on it |
| `PlateEdgeHalfWidthMeters` | **110 m minimum** | pair width is `max(110, 1.5·U·m·Δλ/(2τ))` so added slope ≤ τ (**R-2**) |
| `JunctionRelaxRadiusMeters` | **450 m minimum** | derived `R_J` plus a 5 m certificate ≤ 8 iterations (**R-3**) |
| `InlandSlopeBudget` τ | **0.05** | `InteriorRise / InteriorScale`; `Clamp` to `(0, 0.25]` |
| `LowlandReserveShare` | **0.20** | design's own lower bound on open plains (**R-9**) |
| `NoMassifUpliftScale` | 1.4 | §8 |
| `MinPlateLandAreaSqm` | 150 000 | the prototype's slivers are 0.01–0.09 km²; real districts ≥ 0.22 km² |

**Why no lowering.** [PROTO] Every mean-zero or negative term tried (A, N) creates closed depressions,
and analysis says why: `CoastHeightModel` is smoothstepped per span, so the datum has **zero slope at
200 m and 440 m inland** and along its medial crest; any term that rises towards the coast there makes a
lip. Lowlands therefore stand at the datum and are low *relative to* the raised plates; genuinely lowered
ground is a declared basin and belongs to Stage 3F.

**Why positivity is not enough (R-1).** The C6 rule "all terms ≥ 0 removes ring lips" is false: a
positive term that *falls inland* at a zero-slope ring digs a trough of depth ≈ `g²/(2κ)`. C6 measured
that on 1337 (0.67 m) and 777 (0.29 m) and on 15/40 sweep seeds. The inland slope budget (`M`, τ-derived
widths, knoll cap) makes `H1 + M + Γ·(P + K)` non-increasing towards the sea along every coast-normal
segment **outside the massif influence zone and its lee** — a proof, not a tuning. Massif-foot pits
inside `Z_M ∪ L_M` stay Stage 3F / 4B (**R-10**).

---

## 6. Rolling hills — knolls

### 6.1 The mechanism

```
K(x) = max_i  a_i · (1 − q_i(x)/9)³₊          q_i = (u/σ_major,i)² + (v/σ_minor,i)²
```

One **candidate** per land crust site `i` (`CrustalPartition.SiteCentroid`, diameter `D_i` from its cell
count). **Both RNG draws are taken for every land site before any eligibility test** (stream
`terrain.relief.knoll`). A candidate becomes a knoll only if all hold at its centroid:

- `Λ ≥ KnollMinLevel = 0.34` — knolls stand on raised plates; lowland plains stay open;
- `Γ ≥ 0.5` — not in the outer coastal band;
- its resistance draw `r_i ≥ 1 − KnollKeepFraction` (keep 0.55–0.65);
- `MountainMassif.Evaluate(centroid) == 0` — not on the massif body (**R-4**);
- amplitude after the slope-budget cap is `> 0`.

Omission reasons recorded on the candidate: `Gate` / `Level` / `Resistance` / `Budget` / `MassifBody`.

Shape and height:

| Property | Rule | Value |
|---|---|---|
| height | `a_i = KnollHeightMeters · (0.6 + 0.4 r_i) · (0.5 + 0.5 Λ)`, then `a ≤ (τ − g_P)·σ_minor/0.5726`, clamped to `RidgeSettings.CorridorIncisionMeters` | 6.5 m nominal, ≤ **7.2 m** hard, **τ-capped** (**R-4**) |
| footprint | `σ_major = 0.22 D`, `σ_minor = 0.14 D`; compact kernel, zero outside `q = 9` | ≈ 60 × 38 m σ → **160 × 100 m at half height**, 360 × 230 m support |
| orientation | fall line of `H1 + Γ·P` at the centroid, from central differences at ±60 m, plus a ±20° draw | elongated **down**slope, like interfluves |
| combination | **max**, not sum | bounded by one knoll; order-independent in floating point |
| maximum slope | `≈ 0.5726 a / σ_minor` (the cap coefficient) | the cap keeps knoll descent inside the leftover budget |

`g_P` is the analytic plate-descent bound over the knoll's support disc (pair bands plus certified
junction maxima). **Unity R1:** this bound left `a ≤ 0` on every canonical candidate (45–49 per seed,
0 placed). That is a residual of the proof, not a per-seed retune; the rolling-hill visual is absent
until a later kernel (asymmetric inland tail, R1 §16 R3) is designed.

### 6.2 Why this reads as hills and not noise

- **Countable and explainable:** 12 / 20 / 15 knolls [PROTO], each "the core of crustal block #n on the
  upland plate". A noise field has no answer to "which hill is that".
- **Distribution is structural:** none on lowland plates, none in the surf zone; density follows the
  plates, not a frequency.
- **Band-limited by construction:** one kernel, one size class per block, blue-noise spacing inherited
  from the 200 m crust spacing. No octaves.
- **Visible at eye level:** 4–7 m is 2.5–4 eye heights. A knoll 100 m away hides ground-level objects
  for roughly 150–250 m behind it — the occlusion the triangle rule asks for.
- **Helps drainage:** downslope elongation sends water along the swales between knolls. [PROTO] With
  knolls alone the depression scan finds nothing ≥ 0.25 m. An isolated compact bump on a slope makes a
  saddle upslope, not a pit; pits need overlapping rims, which the max-combination and 200 m spacing
  prevent.
- **Not a mush:** [PROTO] C1 put a hill on every cell (47) and read as an egg carton; the selection rules
  cut that to 12–22.

### 6.3 Interaction

On uplands knolls would add a few metres to the 18 m plate top; on the apron they are suppressed where
`Γ·P_A > 0`; they are the **only** Stage 4A term that can rise along a Stage 3C trajectory (§9). With
zero knolls placed, plate + apron adverse is the whole S11-T story on the canonical seeds.

---

## 7. Massif foothills — the apron

### 7.1 What is wrong today, precisely

[FACT] The Stage 2B cross-section holds full crest over a crown (`CrownFraction 0.16`) and falls in one
smoothstep: 284 m over ~200 m on the steep side (58–68° mid-flank) and ~350 m on the gentle side, then
meets flat ground at a hard corner (110201: 115 → 87 → 69 → 64 → 64 m in 25 m steps). Slopes above 45°
run from ~21 m to ~263 m above the floor, all the way round — the "mesa on a plain".

### 7.2 The mechanism — a fill, not a bump

```
A(x)  = SmoothMax( U2(x), Γ(x)·P_A(x), ApronBlendMeters ) − U2(x)          ≥ 0

         ⎧ h(s) · (1 − ρ/W)²                           ρ ≥ 0   (piedmont, outside the toe)
P_A  =   ⎨
         ⎩ min( h(s) + tan(22°)·(−ρ),  c(s) )           ρ < 0   (talus, climbing into the flank)
```

In the massif's own frame (`MountainMassif.Project`, read-only):

- `ρ` — metres beyond the toe: `|offset| − HalfWidthAt(s) · sideFactor`, with `sideFactor` the same
  steep/gentle blend `MountainMassif.Evaluate` uses; beyond the ends, distance past the end point;
- `W` = lerp(`W_gentle` 180 m, `W_steep` 110 m, steepWeight), then lerp(`W`, `W_end` 280 m, endness);
- **Domain (R-5).** `δ = Δ² / (8 R_axis)` from the axis sample spacing; `W_eff = clamp(R_axis − δ − toe, 0, W)`,
  `h_eff = h · W_eff / W`; `P_A ≡ 0` for `dist ≥ R_axis − δ`. Talus keeps unscaled `h`. This is the
  uniqueness domain of `MountainMassif.Project`.
- `h(s) = 0.09 · Crest · (0.55 + 0.45 · CrestAt(s)/Crest)`, `c(s) = 0.18 · Crest · (same factor)`;
- `ApronBlendMeters = 10` using `MountainUpliftModel.SmoothMax` (blend capped by the smaller input, so
  `A = P_A` exactly where `U2 = 0`).

**S10 (R-6)** is on the **ungated physical profile** `ApronProfileAt(s, offset, endness)` along exact
projection normals (24 stations × 2 sides + 16 end-caps), 1 mm, and exact zero beyond the domain. Gated
fill along world-space rays is report-only (the gate itself can rise inland).

It is a **fill**: wherever the wall stands above the apron surface, `A = 0` and the massif is untouched;
where the apron surface stands above the wall's lower flank, the ground is raised to it. That is the
talus-and-piedmont geometry of `WORLD_RELIEF_PRINCIPLES` §2 rule 1 — debris at the foot — rather than a
second body stacked beside the first.

### 7.3 Why it is anisotropic

- **Along the axis:** 280 m past the ends vs 110–180 m across — the range dies out along strike into
  foothill tails, which is how longitudinal structure reads; elongation 1.6–2.5.
- **Across the axis:** the steep flank gets a short, steep apron (≤ 25°), the gentle flank a long
  piedmont (≤ 16°), reusing Stage 2B's own `SteepSide` and `Asymmetry`.
- **Along the crest:** apron height follows the longitudinal profile (0.55 at the ends → 1 at a
  culmination), so the apron top is never one contour.

### 7.4 How the "second wall" is avoided

| Failure | Where the prototype met it | Rule |
|---|---|---|
| a flat bench at apron height | C2: apron flat inside the footprint → a horizontal ledge along the flank | inside the toe the apron keeps rising at the talus angle; no flat term anywhere |
| a closed trough outside the apron | C3: reach `W` varied with distance along a ray → apron rose outwards on some rays → 0.60 m depression | `W` constant along outward rays (above) |
| a ring of contours | C1: radial falloff | anisotropy §7.3; any residual ring is cut into lobes by Stage 3F valleys |
| burying the massif | talus climbing unbounded (72 m on 424242 in C3) | fill capped at 0.18 × crest |
| a scarp at the outer edge | — | `(1 − ρ/W)²` has zero slope at `ρ = W` |

[PROTO] 110201, section through the culmination, north (gentle) side, 25 m steps from 225 m off the axis:
`165 → 134 → 123 → 113 → 103 → 98 → 93 → 88 → 85 → 83 → 81` m (base: `149 → 115 → 87 → 69 → 64 → 64`).
A concave talus-to-piedmont run, then the flat upland top at 81 m. South (steep, lowland) side:
`133 → 106 → 95 → 83 → 74 → 68 → 65 → 64`.

### 7.5 What the apron does **not** do

It does not break the ring wall: [PROTO] ground > 45° goes 6.1 → 5.4 % and 7.2 → 6.3 %, reachability
92.3 → 92.9 % and 91.5 → 92.5 %. The ≥ 95 % criterion needs a *gap* in the wall — spur ramps and valley
heads (Stage 3F) or a wider, lower Stage 2B body (preflight C7). Stage 4A must not fake either:
**no apron fill ever exceeds 0.18 × crest**.

### 7.6 Stage 3 spurs and basins

The apron sits exactly where Stage 3C trajectories leave the massif and where Stage 3E outlets lie
(61–97 m). The ungated apron is monotone along projection normals and zero beyond `R_axis − δ` (S10);
a trajectory leaving the massif can still climb the **gated fill** as Γ rises, which is why S11 uses
the physical single-climb of `R4A` and the incision theorem, not "apron monotone on H4A". Stage 3F will
cut valleys into the skirt. The apron does not read Stage 3; §9 and §19 check the interaction.

---

## 8. Worlds without a massif

Three of the nine Stage 3E audit seeds (1337, 256, 777) certify no massif. [FACT] Stage 2A still builds
their plates: 1337 has 4 plates on land and 2 convergent boundaries reaching land; 256 and 777 have 3
each. The refusal is about *room for a T1 body*, not about the absence of a collision.

**Rule.** On a world with no accepted axis, `m_world = NoMassifUpliftScale = 1.4` (18 → 25 m).
Geological reading: the crust that was shortened but never certified into a narrow range is carried as a
broad highland instead. It is a derived consequence of Stage 2A's refusal — no new decision, no stream.

[PROTO] 1337 at 25 m:

| | baseline | Stage 4A |
|---|---|---|
| highest point | 72 m | 86 m |
| land 70–110 m | 0.2 % | 24.4 % |
| floor local relief median / p90 | 3.0 / 4.8 m | 3.7 / 8.6 m |
| height spread at fixed coast distance | 0.7 m | 5.8 m |
| coast-aligned gradient | 99 % | 65 % |
| land < 5° | 57.7 % | 49.3 % |
| depressions | 0 | 0 (micro ≤ 0.21 m) |
| knolls | — | 15 |

At ×1.6 a 449 m², 0.27 m depression appears at the junction: 1.4 keeps a margin.

**What a no-massif world looks like after 4A:** a south-western highland with knolls, a north-eastern
middle plateau, a south-eastern lowland plain opening on three coasts, an edge you can see between them,
and the island's highest ground in the interior rather than wherever the coast is farthest. **What it
still lacks:** a landmark. The highest point gains 14 m; nothing is visible from everywhere. That is
Stage 4B's job (T2 plateaus 18–40 m with walls), and §19 does not ask 4A to fake it.

**No-massif acceptance** (1337 plus 256 and 777 at implementation): §19 table, "no-massif" column.

---

## 9. The boundary with Stage 3F

### 9.1 Ownership by scale and sign

| | Stage 4A | Stage 3F |
|---|---|---|
| Sign | **positive only** | **negative** (carve), plus positive linear features *on the massif* (spur realisation) |
| Horizontal scale | areal: plates 0.4–1 km², edges 150–250 m, knolls ≥ 100 m wide and ≥ 200 m apart, apron 110–280 m | linear: corridors 18–115 m half-width, 400–1 300 m long; basin enclosures |
| Vertical scale | +18/25 m plates, ≤ 7.2 m knolls, ≤ 0.18 × crest apron | valleys to 0.30 × crest in the massif, 7.2 m on the floor; basin depth (owner rule B4) |
| Where | island floor, massif foot | massif flanks, corridors, declared basins |
| May change the ring wall | **no** | yes |

Stage 4A never makes a linear negative feature, never makes a linear positive feature on the massif,
and never lowers anything.

### 9.2 Composition

```
H1           Stage 1 datum                         FROZEN
+ U2         Stage 2B massif uplift                FROZEN    = StructuralElevation (H2)
+ R4A        Stage 4A island relief     ≥ 0                  = IslandElevation (H4A)
− C3F        Stage 3F carve             ≥ 0                  (future)
```

**This is the right direction**, with the carve applied last because Stage 3's structures were
declared on `H2`, its valleys must descend through whatever relief stands above `H2`, and a carve that
preceded 4A would be filled in again.

Stage 3F invariants, re-based on `H4A` (to be written into the 3F design, §18):

- `C3F ≥ 0`; `H ≤ H4A` everywhere; `H > SeaLevel` on land;
- outside declared corridors and basins `C3F = 0` (today's "`H3 ≥ H1` elsewhere" becomes "`H = H4A`
  elsewhere", which is stronger because `H4A ≥ H1`);
- inside a floor corridor the carve may reach `H1 − CorridorIncisionMeters`, i.e. it cuts through all
  4A relief plus 7.2 m. Because knolls are ≤ 7.2 m and plates/apron descend along trajectories, **no
  valley needs to cut deeper than today's bound + 18 m of plate step**; whether the bound should scale
  with `R4A` is a 3F design item;
- Stage 3E enclosures stay declared on `H2`; a basin's physical floor and outlet are realised against
  `H4A`.

### 9.3 Leaving room for valleys — measured

[PROTO] Adverse rise — the sum of positive increments of a term along a trajectory, downstream of the
toe — on the real Stage 3C final trajectories, worst trajectory per seed:

| | plate | knolls alone | apron | plate + apron | `R4A` |
|---|---|---|---|---|---|
| 110201 | 1.33 m | 7.75 m | **0.00** | 0.84 m | 5.70 m |
| 424242 | 0.87 m | 4.82 m | **0.00** | 0.17 m | 3.40 m |

The apron and the plates are valley-compatible by construction; knolls are the only obstacle when they
exist. A single knoll never exceeds what 3F may cut; the C6 "knolls alone" sum can pass 7.2 m when a
trajectory crosses two of them. Criterion (**R-7**, S11-T): along every trajectory, plate + apron
adverse rise ≤ 1.5 m, **single climb** `C(R4A) ≤ 7.2 m`, and `I₁(H4A) ≤ I₁(H2)` (theorem: `R4A ≥ 0`
cannot increase the incision Stage 3F needs below Stage 1). The R4A *adverse sum* is report-only — it
is not a cut depth. **[OPEN] option 4A-K2**, not in v1: exclude knolls from 3C corridors. That would
make Stage 4A read Stage 3; it is held back so 4A stays independent of the stage 3F will unfreeze.

Outlet checks (**R-8**, S11-O) are physical, not "`R4A` non-increasing downstream": O-1 corridor end
below the outlet on H4A; O-2 rim-margin after `CarveField` at the declared outlet; O-3 `I₁`/`C` along
the outlet run from `max(ClosestIndex, ToeIndex)`; O-4 declared-min shift reported.

### 9.4 Keeping 3F visible

A 7.2 m floor valley next to 4–7 m knolls reads as a swale, not a valley. On uplands with an 18 m edge it
reads as a channel. The massif valleys (0.30 × crest ≈ 85 m) dwarf a 26–52 m apron. So 4A does not make
3F useless — but the floor incision bound, derived when the floor was flat, is the number 3F should
revisit once it sees `H4A`.

---

## 10. The role of noise

**Stage 4A v1 contains no noise function.** The only randomness:

| Draw | Stream | What it decides | What it cannot decide |
|---|---|---|---|
| knoll resistance `r_i ∈ [0,1)` | `terrain.relief.knoll`, per crust site | whether an *eligible* block keeps a knoll, and its height within 60–100 % | where knolls may stand, how many blocks are eligible, shape class, spacing |
| knoll yaw jitter ±20° | same fork, second draw | orientation around the fall line | the fall line |

Plate levels, junctions, edges, the gate and the apron draw nothing: they are derived.

Noise may enter later only as:

1. **An edge warp** — a bounded, seeded coordinate displacement (≤ 25 m at λ ≥ 250 m, stream reserved as
   `terrain.relief.edge`) applied to the *distance-to-boundary coordinate* of plate edges, if dump review
   finds 800 m straight block edges read as ruled lines (1337's south arm is the candidate). It never
   adds height. **Off by default**; a separate decision.
2. **Refinement** as `PROCEDURAL_TERRAIN_GENERATOR_DESIGN` defines it (≤ 3 m, 20–120 m, structure-
   modulated) — a later pass after 3F, not part of Stage 4.

---

## 11. Numerical targets

Four metrics are added to the preflight's, because the preflight's local relief (112 m window std) is
dominated by the coastal ramp — 54–59 % of land rises 26 m over 200 m, which alone gives ~3 m of std:

- **coast-aligned gradient** — share of interior floor (`d > 250 m`) whose height gradient is within 18°
  of the coast-distance gradient (`cos > 0.95`): the direct measure of "height is a function of coast
  distance";
- **height spread at fixed coast distance** — median over 50 m distance bands (`d > 200 m`) of the
  floor height std;
- **adverse rise along Stage 3C trajectories** (§9.3);
- **apron zone share** — land where `A > 1 m`.

Baseline (preflight method) → prototype C6 (110201 / 424242; 1337 at ×1.4) → target:

| Metric | 110201 | 424242 | 1337 | Target after 4A | Why this number |
|---|---|---|---|---|---|
| land 70–110 m | 4.6 → **24.6 %** | 6.7 → **20.3 %** | 0.2 → **24.4 %** | **15–32 %** (no-massif ≥ 12 %) | the band should hold upland interiors (uplands 55–61 % of land, of which ~40 % lies beyond the coastal band); above ~35 % the island reads as one plateau |
| land p50 | 50 → 57 m | 47 → 55 m | 46 → 54 m | 52–65 m | lowland half stays at datum |
| land p90 | 87 → 108 m | 102 → 122 m | 63 → 76 m | 100–135 m (no-massif 72–95 m) | upland tops + apron, below the massif |
| floor local relief, 112 m std, median | 3.2 → 4.1 m | 3.6 → 4.7 m | 3.0 → 3.7 m | **≥ 4.0 m** (no-massif ≥ 3.5 m) | median is the ramp; it moves substantially only with 3F valleys and 4B walls |
| floor local relief, p90 | 4.9 → 8.4 m | 4.9 → 9.2 m | 4.8 → 8.6 m | **≥ 7.0 m** | edges, knolls, apron: the district/hill signal |
| height spread at fixed coast distance | 0.8 → 7.7 m | 0.8 → 7.2 m | 0.7 → 5.8 m | **≥ 4.0 m** | more than half a knoll: the interior is no longer a function of `d` |
| coast-aligned gradient | 97 → 52 % | 95 → 48 % | 99 → 65 % | **≤ 65 %** (no-massif ≤ 70 %) | the coastal band stays coast-aligned by design; the interior must not |
| floor slope 0–5° | 56 → 41 % | 49 → 29 % | 58 → 49 % | ≥ 28 % (no-massif ≥ 40 %) | tops stay flat; edges and bluffs take the rest |
| land < 5° | 47.2 → 34.9 % | 39.3 → 24.9 % | 57.7 → 49.3 % | **≥ 24 % and ≥ baseline − 15 pp**; the island-level ≥ 35 % is re-checked after 4B | 4A converts flat land to gentle slope; T2 tops return flat land |
| floor slope 15–30° | 0.4 → 14.0 % | 0.3 → 18.2 % | 0.0 → 15.0 % | ≤ 20 % | bluffs and apron only |
| floor slope > 30° | 0.0 → 0.6 % | 0.0 → 1.0 % | 0.0 → 1.0 % | ≤ 2 % | 4A makes no cliffs |
| all land > 45° | 6.1 → 5.4 % | 7.2 → 6.3 % | 0 → 0 % | **≤ baseline + 0.5 pp** | 4A makes no walls; 6–15 % is 4B's and 2B's |
| reachable at ≤ 45° | 92.3 → 92.9 % | 91.5 → 92.5 % | 100 → 100 % | **≥ baseline − 0.5 pp** | ≥ 95 % needs a gap in the wall (3F / 2B) |
| lowland regions | — | — | — | **1–2 plates, 20–35 % of land** [PROTO 27–28 %] | an island needs open plains; > 35 % leaves the interior at datum |
| upland regions | — | — | — | **1–3 plates, 40–62 % of land** [PROTO 56–61 %] | the interior should stand above the coast |
| knolls | → 12 | → 20 | → 15 | **10–25**, height 4–7.2 m, ≥ 200 m apart, footprint (> 1 m) ≤ 20 % of land | between T3's 12–20 count and a mush; ≤ 3F incision |
| plate uplift | — | — | — | 18 m (25 m no-massif) | §5.4 |
| apron: height at toe / fill cap / zone (`A > 1 m`) | → 17.3 % | → 24.8 % | — | 0.09 / 0.18 × crest / **≤ 25 % of land** | the massif and its skirt together stay under ~40 % of land; 424242's 1.3 km massif sits at the limit (§20 R4) |
| `R4A` max / mean on land | → 49 / 9.3 m | → 52 / 10.8 m | → 30 / 8.7 m | ≤ 100 m hard; mean 5–14 m | |
| highest point change | 350 → 361 m | 359 → 371 m | 72 → 86 m | ≤ +30 m (no-massif ≤ +30 m) | massif stays the landmark |
| depressions (≥ 0.25 m and ≥ 100 m²) | 0 → **0** | 0 → **0** | 0 → **0** | **0** | §12 |
| micro-depressions | 0 → 0 | 0 → 0 | 0 → 4 (≤ 0.21 m) | reported, each ≤ 0.25 m | lattice flats |
| adverse rise along 3C trajectories, `R4A` | → 5.7 m | → 3.4 m | — | **≤ 7.2 m**; plate + apron ≤ 1.5 m [PROTO 0.84 / 0.17 m] | §9 |
| curvature ratio (isotropy) | 0.98 → 1.00 | 1.01 → 0.96 | 1.00 → 1.01 | 0.85–1.20, no grid period | generator design G7 |
| authority time | 23.2 s | 23.8 s | 23.6 s | ≤ +15 % | preflight §18 |

**Unity R1 dump** (`D:\Hobby\game-dev\stage4a-dumps\canonical`, 2026-09-16). Structural tests S1–S12
are green. Geometric targets that R1 §15 already called residuals stay report-only — not retuned.

| Seed | 70–110 m | p50 / p90 | LR med / p90 | spread | aligned | low / up | knolls | apron zone | pits |
|---|---|---|---|---|---|---|---|---|---|
| 110201 | 4.6 → **25.3** | 58 / 108 | 4.2 / 8.8 | 6.9 | 73.6 | 28.4 / 54.2 | **0** | 16.0 | 0 |
| 424242 | 6.7 → **23.1** | 58 / 120 | 4.9 / 9.4 | 7.9 | 68.1 | 26.8 / 54.0 | **0** | **25.7** | 0 |
| 20260905 | 7.8 → **21.3** | 63 / 155 | 5.2 / 11.5 | 10.0 | 62.7 | 18.2 / 52.5 | **0** | **30.3** | 0 |
| 7 | 5.0 → **15.4** | 54 / 111 | 4.9 / 9.0 | 8.1 | 73.1 | 19.1 / 55.3 | **0** | 17.3 | 0 |
| 1337 | 0.2 → **26.5** | 55 / 79 | 3.9 / 8.3 | 5.3 | 67.1 | 23.6 / **64.8** | **0** | — | 0 |
| 256 | 0.1 → **22.9** | **51** / 78 | 4.0 / 8.7 | 6.4 | 67.8 | 36.5 / 51.1 | **0** | — | 0 |
| 777 | 0.0 → **17.9** | 53 / 77 | **3.50** / 7.5 | 5.3 | **77.2** | 35.0 / 49.3 | **0** | — | 0 |

Report-only misses vs the C6 bands (R1 §15 predicted most of them): coast-aligned 110201 / 424242 / 7 /
777; apron zone 424242 / 20260905; 1337 upland 64.8 %; 256 p50 51 m; 777 LR median 3.50 m (hard band
≥ 3.5, reports as fail at equality in the dump); **zero knolls on every seed** (soft 8–25) — harsher
than R1's harness prediction of 9–19 because junction certification was not in those numbers.
Junction certificates: all PASS, 1 iteration, radii stayed at the 450 m minimum.

Changed numeric targets vs C6:

| Metric | C6 target | R1 target |
|---|---|---|
| depressions | 0 anywhere | **S8-A** 0 outside `Z_M ∪ L_M` (every seed); **S8-M** 0 inside on the 7 canonical seeds |
| lowland / upland share | 20–35 / 40–62 % | **report**; gate = reserved land ≥ 20 % |
| knolls | 10–25, height 4–7.2 m | 8–25, height ≤ `τ·σ_minor/0.5726`; **measured 0** |
| plate edge | ~200 m | derived per pair, added slope ≤ τ |
| adverse rise, R4A | ≤ 7.2 m sum | **single climb** ≤ 7.2 m; sum reported |
| apron zone | ≤ 25 % | unchanged (still exceeded on 424242 / 20260905) |

---

## 12. Closed depressions

### 12.1 Contract

1. **S8-A (hard, every seed).** No closed depression (≥ 0.25 m and ≥ 100 m²) with any cell outside the
   massif influence zone and its lee (`Z_M ∪ L_M`, `U2 > 0 || A > 0`, lee = nearest-shore segment).
   Proven by the inland slope budget, not measured into existence (**R-10**).
2. **S8-M (hard on the 7 canonical seeds; reported on the sweep).** Depressions entirely inside
   `Z_M ∪ L_M` are massif-foot / apron class. Frozen H2 has one itself on sweep seed 1019. Owner:
   Stage 3F / Stage 4B.
3. **S8-C (hard).** On the 1025² lattice, outside `Z_M ∪ L_M`, `∂(H1 + M + Γ(P + K))/∂n` along the
   coast-distance gradient (`|∇d| ≥ 0.95`) is `≥ −1·10⁻³`.
4. **Tolerance:** a filled component counts if its max fill depth ≥ **0.25 m** *and* its area ≥ **100 m²**.
   Micro components below that are reported, not failed.
5. A failure is **fixed at the source**. Stage 4A has no fill, repair or re-planning pass. Junction
   certification samples the analytic level field; it never reads a raster of terrain.

### 12.2 Construction rules (R-1…R-10)

| Rule | What it does |
|---|---|
| **R-1** inland slope budget `M(d)` | `H1 + M` has inland slope ≥ τ; ring lips from falling terms are absorbed |
| **R-2** pair half-width `w_kj = max(w₀, 1.5·U·m·Δλ/(2τ))` | steepest plate edge descent ≤ τ |
| **R-3** derived `R_J` + 5 m certificate ≤ 8 iterations | discrete `−∂(ΓP)/∂n` ≤ τ at junctions (S12) |
| **R-4** knoll cap `a ≤ (τ − g_P)·σ_minor/0.5726`; no knoll on massif body | knolls stay inside leftover budget |
| **R-5** `W_eff = clamp(R_axis − δ − toe, 0, W)`, `P_A ≡ 0` beyond `R − δ` | projection uniqueness |
| **R-6** S10 on ungated `P_A` along exact normals, 1 mm | gate rise is not an apron defect |
| **R-7** S11-T: `C(R4A) ≤ 7.2`, plate+apron ≤ 1.5, `I₁(H4A) ≤ I₁(H2)` | adverse *sum* is not a cut depth |
| **R-8** S11-O O-1…O-4 with real `CarveField` | outlets are H4A + carve properties, not R4A monotonicity |
| **R-9** lowland reservation ≥ 20 % of land at level 0 | whole-plate rank left 5.6–15 % lowland |
| **R-10** S8 scope = S8-A / S8-M | massif-foot pits are not Stage 4A's to guarantee |

Kept from C6: convergent boundaries are one-sided; level-0 needs ≥ 150 m coast frontage; knolls
max-combined and ≥ 200 m apart; `NoMassifUpliftScale ≤ 1.4`. The struck C6 rule "all terms ≥ 0
removes ring lips" is not a construction rule.

### 12.3 How it is checked (no hydrology dependency in 4A)

Stage 4A never references `GameDev.World.Hydrology`. Only its **dump and tests** do:
`H4A` is sampled onto the 1025² lattice, wrapped in `new HydrologyGrid(...)`, and filled with the
existing `DepressionFill.Fill` (ocean mask from the grid). Filled-minus-original components are labelled
and compared against §12.1.3. That scanner shares no code with Stage 4A, which is what makes it a check.
Run on every canonical seed with raw coverage (110201, 424242, 20260905, 7, 1337, 256, 777).

---

## 13. Determinism and streams

| Stream | Fork | Draws, in order | Used by |
|---|---|---|---|
| `terrain.relief.knoll` | `Fork("terrain.relief.knoll", siteIndex)` | resistance `NextDouble()`, yaw jitter `NextDouble()` | knolls |
| `terrain.relief.edge` | reserved, **unused in 4A v1** | — | optional edge warp (§10) |
| `terrain.relief.plateau.{i}`, `terrain.relief.outcrop.{i}`, `terrain.relief.coast.{segment}` | reserved for 4B / 4C | — | — |

Naming: `terrain.relief.*` mirrors `terrain.massif` and `terrain.ridge.*`, tells the reader which stage
owns the draw, and cannot collide with any stream in use (`terrain.crust*`, `terrain.coast*`,
`terrain.macro.*`, `terrain.range.*`, `terrain.massif`, `terrain.ridge.*`, `terrain.surface.*`,
`terrain.vegetation.*`, `hydrology*`, `poi*`, `settlement`, `player.spawn`).

Rules:

- `System.Random` through `GameDev.World.Seed.WorldSeed.Fork` — **fully qualified**, because
  `GameDev.World.WorldSeed` shadows it inside `GameDev.World.*`. No `UnityEngine.Random`, no time.
- **Keyed by crust site index**, drawn for *every* land site whether or not it becomes a knoll, so an
  eligibility change never shifts another site's draw.
- All structure lists are built once and **sorted by stable index** (plate index, site index, massif
  index, junction position with index tie-break). No `HashSet`/`Dictionary` iteration reaches output.
- Floating-point order independence: plate blend summed in plate index order; knolls combined by `max`;
  junction relaxation applied in junction order.
- `R4A` is a **pure function** of `(x, z, H1(x,z), U2(x,z))` and immutable structure; the bulk lattice
  calls the same function, so point query and bulk are bit-identical.
- Existing streams and Stage 1, 2A, 2B, 3A–3E outputs are untouched: Stage 4A reads them after they
  exist and writes nothing back. Tests prove it with the pinned hashes (§19).

---

## 14. Tile and lattice contract

- Stage 4A is defined in **world coordinates** only. It knows nothing of tiles, the 513² windows, the
  alphamap or `TerrainData`.
- Evaluated through the existing path: `TerrainAuthorityField` → `OverworldTerrainField` samples the
  1025 × 1025 lattice at 2.734375 m; tile-edge vertices are one sample; seams stay 0.0000 m.
- Plates (~1 km), apron and knolls cross tile boundaries freely; the tile seams at x = 1 400 m and
  z = 1 400 m run through the middle of all three measured islands, so this is exercised on every
  canonical seed.
- Spatial acceleration, if needed (knolls: ≤ 48 candidates; plates: ~27 boundary segments), is
  world-aligned bucketing (64 m, like `InlandWaterSampler`), independent of tile layout.
- Smallest feature: a knoll's minor half-height width ≈ 100 m = 37 lattice steps; a plate edge ≈ 200 m
  = 73 steps; the apron's shortest reach 110 m = 40 steps. All far above the 3-vertex floor; relief
  amplitudes far above the 2.14 cm height step.

---

## 15. API

**Implemented** in `IslandRelief*.cs` / `IslandElevation.cs`. Dump, tests and (later) integration consume
this surface. Production `TerrainAuthorityField` does not.

```csharp
namespace GameDev.World.TerrainGen
{
    public struct IslandReliefSettings
    {
        public float PlateUpliftMeters;                 // 18
        public float NoMassifUpliftScale;               // 1.4
        public float PlateEdgeHalfWidthMeters;          // 110 — minimum (R-2)
        public float JunctionRelaxRadiusMeters;         // 450 — minimum (R-3)
        public float InlandSlopeBudget;                 // τ = InteriorRise/InteriorScale, Clamp (0, 0.25]
        public float LowlandReserveShare;               // 0.20 (R-9)
        public float JunctionCertificationStepMeters;   // 5
        public float JunctionCertificationMaxIterations;// 8
        public float MinPlateLandAreaSqm;               // 150 000
        public float ConvergentMinLandLengthMeters;     // 150
        public float MinLowlandFrontageMeters;          // 150
        public float KnollHeightMeters;                 // 6.5
        public float KnollKeepFraction;                 // 0.6
        public float KnollMinLevel;                     // 0.34
        public float KnollMajorSigmaPerDiameter;        // 0.22
        public float KnollMinorSigmaPerDiameter;        // 0.14
        public float KnollYawJitterDegrees;             // 20
        public float ApronHeightPerCrest;               // 0.09
        public float ApronCapPerCrest;                  // 0.18
        public float ApronReachGentleMeters;            // 180
        public float ApronReachSteepMeters;             // 110
        public float ApronReachEndMeters;               // 280
        public float ApronTalusDegrees;                 // 22
        public float ApronBlendMeters;                  // 10
        public float ApronEndHeightFloor;               // 0.55
        public float MaxReliefMeters;                   // 100 — asserted, never clamped
        public static IslandReliefSettings Default { get; }
        public void Clamp();
    }

    public sealed class IslandRelief
    {
        public static IslandRelief Build(
            GameDev.World.Seed.IWorldSeed worldSeed,
            Stage1Result stage1,
            MacroPlates plates,
            IReadOnlyList<MountainMassif> massifs,
            IslandReliefSettings settings);

        // signedDistance is Stage 1 signed coast distance (positive inland). Applied M is 0 when H1 ≤ sea.
        public float At(float worldX, float worldZ, float signedDistance, float stage1Height, float upliftMeters);
        public IslandReliefSample Sample(float worldX, float worldZ, float signedDistance, float stage1Height, float upliftMeters);
        public WorldHeightField EvaluateField(WorldHeightField stage1Field, WorldHeightField upliftField, float[] signedDistance);

        public float SlopeBudget { get; }
        public float PairHalfWidth(int k, int j);
        public bool InfluenceZone(float worldX, float worldZ, float upliftMeters); // U2 > 0 || A > 0
        public float CoastalFloorAt(float signedDistance);                         // raw 1-D M(d)

        public IReadOnlyList<ReliefPlate> Plates { get; }
        public IReadOnlyList<ReliefJunction> Junctions { get; }   // Certificate: initial/final R, widths, max adverse, iters, pass
        public IReadOnlyList<ReliefKnoll> Knolls { get; }
        public IReadOnlyList<ReliefKnoll> KnollCandidates { get; } // Rejection: Gate/Level/Resistance/Budget/MassifBody
        public bool HasMassif { get; }

        public float EffectiveReach(int massifIndex, float s, float side);
        public float ApronProfileAt(int massifIndex, float s, float signedOffset, float endness); // S10
    }

    public readonly struct IslandReliefSample
    {
        public float Gate { get; }          // Γ
        public float PlateLevel { get; }    // Λ
        public float CoastalFloor { get; }  // applied M
        public float Plate { get; }         // P (ungated)
        public float Knoll { get; }         // K (ungated)
        public float Apron { get; }         // A
        public float Total { get; }         // M + Γ·(P+K) + A
    }
}
```

`IslandElevation.At` computes signed distance once and passes it into `relief.At`. Point and lattice
share `Stage1Result.SignedDistance` (S4 includes `M`).

---

## 16. Expected visual result

**Unity R1 dump images** (canonical 13 maps × 7 seeds, 2026-09-16). Eye-level capture (preflight M9)
is still required before integration. Stage 4A changes how the island *reads when you walk it* more
than how it looks from the air; without knolls the walk currently lacks the rolling-hill beat.

### 110201

- **Silhouette:** unchanged (Stage 1).
- **Plates:** reserved southern lowland (level 0, 32 % of land) vs NE upland (level 1). Pair widths
  110–270 m; both junctions certified at 450 m, 1 iteration, PASS.
- **Massif / apron:** the massif still landmarks the east-centre. The apron is a closed fill around
  the foot (zone 16.0 %, `W_eff` mid 123/110 m, domain 445.8 m). Transition from lowland (south) to
  the steep flank is readable; the north piedmont is shorter than C6's 180 m gentle reach.
- **Knolls:** none. `06_knolls_gated` is empty. Interior variation is plate levels + `M` rings only.
- **What still reads:** coast-parallel Stage 1 rings (now with `M`); massif crown wall; junction discs
  as circular blends on the structure map.

### 424242

- Massif N–S west of centre. Apron is a **closed ring** (zone **25.7 %**, R1 residual R4). Islet
  untouched. Interior plates readable; no knoll blobs.

### 20260905

- Larger massif, apron zone **30.3 %**, concentric bands around the body. Lowland 18.2 % (reserved
  land 23.9 % — the reservation fired; gate is reserved share, not the visual Λ<0.25 share). No knolls.

### 7

- Horseshoe island, massif north-centre, apron zone 17.3 %. Plate terraces on the SW/SE arms. Traj
  plate+apron 1.44 m (under 1.5). No knolls.

### 1337 (no massif)

- SW highland vs NE middle vs SE lowland, reservation applied, upland **64.8 %** (three-district
  middle rank at 0.5 — R1 residual R5). Straight bisector edges remain (R6). Highest point 72 → 85 m.
- Ring-100 pit of C6 is gone (S8-A/M = 0).

### 256 / 777 (no massif)

- 256: interior plateau with plate facets, reserved lowland east/south (36.5 %), p50 51 m.
- 777: broad southern/eastern plain (lowland 35.0 %), western highland; coast-aligned **77.2 %**
  (no-massif limit 70 %). C6 ring-200 pit gone.

### The sequence the player walks

| Transition | Before | After R1 |
|---|---|---|
| shore → lowland | beach ramp, then the same ramp | beach ramp, then an open plain at datum (`M` is a few metres, not a landform) |
| lowland → upland | none | a τ-width edge onto a raised district |
| upland interior | coast-distance roof | plate top; **no knolls yet** |
| upland → foothills | none | a domain-limited piedmont that still reads as a ring on some seeds |
| foothills → massif | a 60° wall from a flat plain | the wall emerges from talus (still a wall above that) |

---

## 17. Does Stage 1 have to change?

**No — keep Stage 1 frozen for Stage 4A.**

- Stage 4A needs from Stage 1 only the datum `H1`, the crust sites and the land raster, all read-only.
  The prototype reached every Stage 4A target without touching any of them.
- The Stage 1 properties that limit the island are real but orthogonal:

| Stage 1 property | Effect | Owner |
|---|---|---|
| silhouette is a cell-growth union with a blocky rhythm | outline has no macro intent | D1 (Stage 1 reopening), after 4A images |
| one onshore profile on every shore | uniform coast | Stage 4C (additive, zero at the shore) |
| 200 m coastal transition = 54–59 % of land | 4A relief is partial over half the island | acceptable; revisit in D1 only if 4C still reads ramp-dominated |
| per-span smoothstep leaves zero-slope rings at 200 m and 440 m | a falling 4A term would lip | inland slope budget `M` + τ (**R-1**); Stage 1 stays frozen |
| constant 36 % land fraction | similar island sizes | D1 |

- Reopening Stage 1 later costs 5 golden hashes and a Stage 2A–3E re-audit; Stage 4A re-evaluates
  automatically on any new Stage 1 because it consumes Stage 1 output, so D1 does not invalidate 4A.

---

## 18. Contradictions in the current documents

Recorded for correction **after** this design is accepted. Nothing was edited.

| # | Document, place | Says | Conflict |
|---|---|---|---|
| 1 | `WORLD_RELIEF_PRINCIPLES` §1 | three tiers "and nothing in between" | no place for floor relief (uplands, knolls, apron); needs "T0 floor relief is slope-bounded variation of the floor, not a landform tier" |
| 2 | `WORLD_RELIEF_PRINCIPLES` §2.3 | "at 0.7 m/px" | native lattice is 2.73 m |
| 3 | `WORLD_RELIEF_PRINCIPLES` §3.1 | "no closed basins" | Stage 3E declares basins, 3F realises them: should read "no *undeclared* closed basins" |
| 4 | `WORLD_RELIEF_PRINCIPLES` §6 | "first measured baseline — 13 shelves, 15 s" table | removed pipeline; the acceptance criteria (≥ 35 % < 5°, 6–15 % > 45°, ≥ 95 % reachable) should say they apply to the finished island, not per stage |
| 5 | `WORLD_RELIEF_PRINCIPLES` §7 | T0 = Stage 1; T2/T3 "a future stage"; "T1 owns its own skirt" (250 m keep-out) | T0 becomes Stage 1 + 4A; T2/T3 are 4B; the skirt is now the 4A apron, and 4B's keep-out should read apron extents |
| 6 | `PROCEDURAL_TERRAIN_GENERATOR_DESIGN` "Recommended Implementation Stages" | Stage 4 = terraces and basins, 5 = refinement, 6 = drainage, 7 = carve | collides with **Stage 4 = island relief**; basins already built as 3E; drainage and carve are now downstream hydrology |
| 7 | same, Executive Summary #2 and "Generation Order" ③–⑤ | hydrology "is the arbiter", drainage and carve inside TerrainGen | superseded by the owner direction (preflight §12) |
| 8 | same, "Terrain Representation" | "Stage 3 onwards joins the field through `StructuralElevation`" | `StructuralElevation` is frozen; later stages compose after it (`IslandElevation`, then the 3F carve) |
| 9 | same, "Terrain Refinement" | the only place noise appears | consistent with 4A (no noise) — but 4A's knolls must not be mistaken for refinement: they are structure at 100–360 m |
| 10 | same, "Mountain Systems" | half-width 500–1000 m, crest 250–530 m | historical; Stage 2A/2B settled 280 m and 200–330 m |
| 11 | `PROCEDURAL_TERRAIN_STAGE_3_DESIGN` §8 | "Stage 4 = drainage (`HydrologyPlanner`)", "Stage 5 = T2 plateaus, T3 outcrops" | **second numbering collision**: drainage is not a TerrainGen stage; T2/T3 are 4B |
| 12 | same §6 and `RidgeSettings.CorridorIncisionMeters` doc | "`H3 ≥ H1` elsewhere", "`H3 ≤ H2`" | re-base on `H4A` (§9.2) |
| 13 | `PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT` §8 row 5 | hydrology "Stage 4" | same collision as #11 |
| 14 | same §12 | 3F "takes the Stage 2 surface" | 3F takes `H4A`; basin floors and outlets are realised against `H4A` (enclosures stay declared on `H2`) |
| 15 | same §3, §7 | shipping heights from the external pipeline | historical and bannered; no action beyond the banner |
| 16 | `NATIVE_ISLAND_SHAPE_PREFLIGHT` header, §1 | "HEAD `7c882c1` plus the uncommitted migration" | the migration is commit `b677f26` |
| 17 | same §8, §10 | "lowland basins", "±10–25 m around the datum" | this design chose positive-only relief with evidence (§5.4, §12) |
| 18 | `TerrainGenSettings` comment on `CoastalRiseMeters` | "WORLD_RELIEF_PRINCIPLES puts that floor in the 43–122 m band" | no such band exists in the current principles (comment only, frozen file) |
| 19 | `OVERWORLD_TERRAIN_ARCHITECTURE` §1 | TerrainGen relief = "Stage 1 + Stage 2 massif uplift" | becomes Stage 1 + 2 + 4A at integration (update then, not now) |
| 20 | `TerrainSurfaceRules` "measured" sand band 27.0–28.4 m | measured on the removed pipeline's island | 4A moves it by < 1 m (`Γ ≈ 0.04` there); stays a 4C item |

**Proposed naming resolution:** keep **Stage 4 = island relief (4A floor relief, 4B plateaus/outcrops,
4C coastal character)** as the preflight named it; refer to drainage as "hydrology (downstream of
TerrainGen)" everywhere, and to refinement as "refinement (after 3F)".

---

## 19. Acceptance criteria

### 19.1 Design is good enough to code — checklist

| Criterion | Status in this document |
|---|---|
| deterministic, one bounded stream, stream isolation specified | §13 |
| exactly zero at and seaward of the shore; land never lowered; no new coastline | §5.3 (by construction) |
| no Stage 1 / 2A / 2B / 3A–3E change | §2, §21 file lists |
| no undeclared closed depressions: contract, rules, independent check | §12 |
| bounded height | `MaxReliefMeters = 100`, prototype 23–52 m |
| Stage 3F compatible: sign/scale split, composition, re-based invariants, measured trajectory rise | §9 |
| every scale caused by a named structure; noise role explicit | §2, §4, §10 |
| numbers with reasons, and evidence on 3 seeds including a no-massif world | §11, §3, §8 |

### 19.2 Implementation acceptance (dump + tests, before integration)

**Structural (hard, per seed: 110201, 424242, 20260905, 7, 1337, 256, 777):**

| # | Check |
|---|---|
| S1 | `R4A == 0` (exact) at every lattice sample with signed coast distance ≤ 0 |
| S2 | land mask of `H4A` bit-identical to that of `H2`; land components unchanged |
| S3 | `0 ≤ R4A ≤ MaxReliefMeters`; `H4A ≤ 700 m`; no NaN / infinity |
| S4 | `IslandRelief.At` equals the bulk lattice value bit for bit, **including applied `M`** (full lattice on one seed, 10 000 samples on the rest) |
| S5 | two builds in one process and six fresh processes give identical relief hashes |
| S6 | Stage 1 golden hashes, Stage 2B field hashes (`1de91e469513054b`, `9d9d7b3c2560bc36`), full Stage 3 suite, and `OverworldTerrainBakeTests`' pinned authority hash unchanged (4A is not yet on the production path) |
| S7 | perturbing only `terrain.relief.knoll` changes only `Knoll`; plates, junctions, apron and **coastal floor** identical |
| S8-A | depressions with any cell outside `Z_M ∪ L_M` = 0 (hard, every seed) |
| S8-M | depressions entirely inside `Z_M ∪ L_M` = 0 on the 7 canonical seeds (hard); sweep reported |
| S8-C | `∂(H1 + M + Γ(P+K))/∂n ≥ −1·10⁻³` outside `Z_M ∪ L_M` (`|∇d| ≥ 0.95`) |
| S9 | every knoll ≤ `CorridorIncisionMeters`; every apron fill ≤ 0.18 × crest |
| S10 | ungated `P_A`: 0 beyond `R − δ`; non-increasing along exact projection normals, 1 mm (**R-6**) |
| S11-T | per Stage 3C trajectory: `C(R4A) ≤ 7.2 m`; plate + apron adverse ≤ 1.5 m; `I₁(H4A) ≤ I₁(H2) + 1 mm` (**R-7**) |
| S11-O | O-1, O-2 (`CarveField`), O-3 hard; O-4 reported (**R-8**) |
| S12 | every on-land junction certificate: max `−∂(ΓP)/∂n` ≤ τ (**R-3**) |

Unity 2026-09-16: `TerrainGenStage4ATests` **16/16**; full EditMode **571/571**; Stage 2B hashes unchanged.

**Geometric (seed-level, from the dump):** §11 Unity R1 table. Soft/report-only misses are not defects
to tune away.

**Visual (human review of dump images, then eye-level captures before integration):**

- at least one upland and one lowland are nameable on every seed, with a visible edge between them;
- knolls *would* read as hills on raised ground — **currently 0 placed**;
- the massif rises out of an apron; the apron still reads as a ring on 424242 / 20260905 (Stage 3F / 4B);
- a no-massif world shows a highland / plateau / plain hierarchy;
- no pattern that repeats at a fixed spacing; straight bisector edges remain (R6).

---

## 20. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | Relief reads as a "raised island" rather than districts | levels are rank-normalised (a lowland always exists); knolls only on uplands; review against §19.2 visual |
| R2 | Massif-foot depressions inside `Z_M ∪ L_M` on non-canonical seeds | S8-M hard on canonical; sweep reported; owner Stage 3F / 4B (R1 §16 R1) |
| R3 | Flat share too low on seeds with long upland coasts | 4B restores flat tops; island-level ≥ 35 % is re-checked after 4B |
| R4 | The apron reads as a ring; zone 25.7 / 30.3 % on 424242 / 20260905 | O-2 guards drainage; ring appearance owned by Stage 3F valleys and Stage 4B |
| R5 | Straight plate edges read as ruled lines (1337) | optional edge warp (§10), off by default |
| R6 | Analytic knoll bound places **zero knolls** | accepted residual of the S8 proof; later asymmetric kernel (R1 §16 R3), not a 4A retune |
| R7 | Owners expect 4A to fix reachability or give 1337 a landmark | stated as non-goals (§7.5, §8); owners: 3F / 2B / 4B |
| R8 | Downstream tuning drifts (POI `MinHeight01`, settlement flatness, surface bands) | integration step runs markers and settlements and compares with the 32 POIs placed today |
| R9 | Authority time grows (massif `Project` evaluated again for the apron) | ≤ +15 % budget; reuse the uplift's projection only if measured necessary, never at the cost of the pinned hash |
| R10 | Prototype numbers do not transfer to C# (raster plates, blurred blends) | the implementation dump re-measures everything; §11 targets have ranges, not the prototype's point values |

---

## 21. Final recommendation

### Chosen variant

**C — hybrid structural relief:** plate-block uplift (macro plates) + knolls (crust blocks) + massif
apron (massif frame). Positive only, shore-gated, no noise, one stream.

### Exact composition

```
H4A(x) = H1(x) + U2(x) + R4A(x)                          (future: H(x) = H4A(x) − C3F(x))

R4A(x) = M(d) + Γ(x) · [ P(x) + K(x) ] + A(x)            applied M = 0 wherever H1 ≤ Sea

M(d)   = ∫₀^d max(0, τ − H1'(u)) du     d > 0;  0 at and seaward of the shore
τ      = InteriorRiseMeters / InteriorScaleMeters        (= 0.05)
Γ(x)   = smoothstep( clamp01( (H1(x) − SeaLevel) / CoastalRiseMeters ) )
P(x)   = PlateUpliftMeters · m_world · Λ(x)               m_world = 1 | 1.4
Λ(x)   = reserved rank; pair widths w_kj = max(w₀, 1.5·U·m·Δλ/(2τ));
         junction R_J derived and certified (S12)
K(x)   = max_i a_i (1 − q_i(x)/9)³₊ , a ≤ (τ − g_P)·σ_minor/0.5726,
         no knoll on massif body or when a ≤ 0
A(x)   = SmoothMax(U2, Γ·P_A, 10 m) − U2
P_A    = h_eff(s)(1 − ρ/W_eff)²  (ρ ≥ 0)   |   talus (ρ < 0, unscaled h)
         W_eff = clamp(R_axis − δ − toe, 0, W);  P_A ≡ 0 beyond R_axis − δ
```

### Proposed settings

`IslandReliefSettings.Default` as listed in §15.

### Expected contribution and scales

| Term | Height | Scale | Share of land |
|---|---|---|---|
| coastal floor `M` | 0–4.7 m on canonical land | 1-D in `d`, rings of Stage 1 | everywhere inland of the shore (zero where H1 ≤ sea) |
| plate uplift | 0–18 m (25 m no-massif) | plates 0.4–1 km², edges 110–270 m | uplands / lowlands **reported**; reserved land ≥ 20 % |
| knolls | τ-capped; **0 placed** on canonical | 160 × 100 m half-height when present | 0 % |
| apron | fill to 26–52 m | `W_eff` ≤ `R_axis − δ` | 16–30 % (424242 / 20260905 over 25 %) |
| total `R4A` | mean 7–13 m, max ≤ 53 m measured, ≤ 100 m asserted | | |

### Deterministic streams

`terrain.relief.knoll` (per crust site). Reserved, unused: `terrain.relief.edge`,
`terrain.relief.plateau.{i}`, `terrain.relief.outcrop.{i}`, `terrain.relief.coast.{segment}`.

### Acceptance thresholds

§11 target column and §19.2.

### Files that appear at the implementation step

- `Assets/Scripts/World/TerrainGen/IslandReliefSettings.cs`
- `Assets/Scripts/World/TerrainGen/IslandRelief.cs` (split into `IslandReliefPlates.cs`,
  `IslandReliefKnolls.cs`, `IslandReliefApron.cs` only if a file exceeds the size of `MountainMassif.cs`)
- `Assets/Scripts/World/TerrainGen/IslandElevation.cs`
- `Assets/Scripts/Editor/TerrainGenIslandReliefDump.cs` (per-part relief, height, hillshade, 10 m
  contours, slope classes, depressions, stats line; batch entry like the other dumps, output outside
  `Assets`)
- `Assets/Scripts/Editor/Tests/TerrainGenStage4ATests.cs`
- their `.meta` files (Unity-generated)

### Files that must not change

- Stage 1: `CrustalPartition`, `LandmassBuilder`, `CoastOutline`, `CoastQuery`, `CoastHeightModel`,
  `ProfileCurve`, `Stage1LandmassGenerator`, `WorldStructure`, `WorldHeightField`, `TerrainGenSettings`
- Stage 2A/2B: `MacroPlates`, `MacroAxis`, `MacroAxisBuilder`, `MacroStructureSettings`,
  `Stage2AMacroStructure`, `MountainMassif`, `MassifUpliftModel`, `MassifSettings`,
  `Stage2BMassifGenerator`, `MountainUpliftModel`, `StructuralElevation`
- Stage 3A–3E: `Stage3ASkeleton`, `MassifOwnership`, `InterMassifDivide`, `PrimarySpurBuilder`,
  `SpurCurve`, `RidgeSettings`, `RidgeSkeleton`, `ValleyNetwork`, `ValleyTopology`,
  `ValleyTrajectoryBuilder`, `ValleyTrajectories`, `CarveField`, `Stage3Field`, `SecondarySpurBuilder`,
  `SecondaryRidgeSkeleton`, `CirqueCandidate`, `CirqueCandidateBuilder`, `StructuralBasin`,
  `StructuralBasinBuilder`
- Bake and consumers: `TerrainAuthorityField` (until integration), `OverworldTerrainField`,
  `OverworldTerrainBuilder`, `OverworldTerrainBake`, `TerrainSurfaceRules`, `TerrainVegetationRules`,
  everything under `World/Hydrology`
- `Assets/Scenes/Overworld_Generator.unity`, every existing test file, every existing dump tool

### Implementation, then integration — two separate steps

**Step 1 — Stage 4A implementation (TerrainGen only, dump-first).** **Done (C6 then R1).** Nothing on
the production path calls these files. Accept on §19.2 for seven seeds plus human review of the dump
images. Production heights, the pinned authority hash and the saved scene stay bit-identical.

**Step 2 — integration (separate, reviewable). Not started.**

1. `TerrainAuthorityField` builds `IslandRelief` from the Stage 1, Stage 2A plates and massifs it
   already has, and `HeightAt` / `Sample` return `IslandElevation.At` (`includeMassifs: false` keeps the
   Stage 1-only reference surface).
2. `TerrainGenSettings.Default.GeneratorVersion` 1 → 2 — the field is informational (it enters no hash),
   so the Stage 1 golden hashes must stay green; if the owner prefers not to touch a Stage 1 file, an
   authority-level version constant is the alternative.
3. Re-pin the authority hash in `OverworldTerrainBakeTests`; record new world signatures for 110201 and
   424242 with the reason; six fresh-process runs per seed identical.
4. Run markers and settlements; compare POI placement (32 today) and settlement siting.
5. Regenerate `Overworld_Generator.unity` through `GenerateAllAndSave`; update
   `OVERWORLD_TERRAIN_ARCHITECTURE.md` §1 and the §18 contradictions.
6. Eye-level capture review before declaring the island shape accepted.

---

## Result

```
CURRENT STATE:  PASS
DESIGN:         R1 FOLDED IN
STAGE 1:        KEEP FROZEN
STAGE 3A–3E:    FROZEN
NATIVE TERRAIN: PASS
STAGE 4A:       IMPLEMENTED DUMP-FIRST (R1); NOT ON TerrainAuthorityField
                tests 16/16, EditMode 571/571, canonical S8-A/M/C S10 S11-T/O S12 green

RECOMMENDED NEXT STEP:
Do not wire TerrainAuthorityField. Residuals (zero knolls, apron ring/zone, massif-foot sweep pits,
report-only metric misses) are owned by Stage 3F / 4B or a later knoll-kernel revision, not by a
4A retune. Integration remains the separate Step 2.
```

Owner confirmations still open at integration (none blocks remaining disconnected): positive-only
relief (§5.4); the Stage 4A-level flat-ground threshold (§11, "land < 5°"); edge warp on/off (§10);
option 4A-K2 (§9.3); the `GeneratorVersion` bump at integration (§21); whether a later knoll kernel
may relax the analytic `g_P` bound (R1 §16 R3).
