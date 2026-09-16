# Procedural Terrain — Stage 4 Island Relief Design (Stage 4A)

**Status:** Design. **No production code, test, tool, scene, setting or generated asset was changed.**
**Date:** 2026-09-14
**Baseline:** HEAD `b677f26` ("Bake TerrainGen straight into Unity TerrainData and remove the external
terrain pipeline"), working tree clean. *The task brief names HEAD `7c882c1` with the migration
uncommitted; the migration has since been committed as `b677f26`. Nothing else differs.*
**Answers:** [NATIVE_ISLAND_SHAPE_PREFLIGHT.md](NATIVE_ISLAND_SHAPE_PREFLIGHT.md) §16 step 1.
**Related:** [WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md](PROCEDURAL_TERRAIN_GENERATOR_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md](PROCEDURAL_TERRAIN_STAGE_3C_HANDOFF.md),
[PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md](PROCEDURAL_TERRAIN_STAGE_3F_PREFLIGHT.md),
[OVERWORLD_TERRAIN_ARCHITECTURE.md](OVERWORLD_TERRAIN_ARCHITECTURE.md),
[UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md](UNITY_NATIVE_TERRAIN_MIGRATION_REPORT.md),
[OVERWORLD_TECH_DEBT.md](OVERWORLD_TECH_DEBT.md).

Claim tags as in the other TerrainGen documents: **[FACT]** read from code or measured on the production
path, **[PROTO]** measured on the offline prototype described in §1 (real Stage 1/2A/2B structure,
approximated geometry — read the caveats), **[CONCLUSION]** follows from the above, **[PROPOSAL]** the
design decision, **[OPEN]** deliberately unresolved.

**The question:** what deterministic structural relief must Stage 4A add so that the island stops
reading as an extruded coastline and starts reading as a fantasy island, without becoming procedural
noise?

**Short answer.** Three positive-only relief terms, each owned by one existing geological structure and
living at one scale, gated to exactly zero at the shore:

1. **Plate-block uplift** (macro plates, ~1 km): each land plate stands at a level set by its role in
   the island's collision; block edges follow the plate boundaries. This is what creates **uplands and
   lowlands**, and it works identically on worlds without a massif.
2. **Knolls** (crustal blocks, ~275 m): the resistant cores of selected crustal cells, only on raised
   plates, never taller than the valley incision Stage 3F is allowed. This is the **rolling-hill** layer.
3. **Massif apron** (massif frame): a talus-and-piedmont fill around the foot of the frozen Stage 2B
   body, longer along the axis than across it. This is the **foothill** layer.

`H = H1 + U2 + R4A` today, `− C3F` later. No term lowers land, so no coastline, ocean sample or Stage 1–3
structure can move. Noise has no role in Stage 4A v1. Measured on the prototype, the chosen parameters
fill the empty 70–110 m band (4.6 / 6.7 / 0.2 % → 24.6 / 20.3 / 24.4 %), break the coast-distance
roof (gradient aligned with the coast normal on 95–99 % of the interior → 48–65 %), create no closed
depression on any of the three seeds, and do not regress reachability. What Stage 4A cannot do — and
must not be asked to do — is break the massif's ring wall (Stage 3F / 2B) or give a no-massif world a
landmark (Stage 4B).

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
  that boundary segment, half-width `PlateEdgeHalfWidthMeters = 110 m` (10–90 % over ~200 m).
- **Junction relaxation.** Within `JunctionRelaxRadiusMeters = 450 m` of an on-land plate junction
  (`MacroPlates.Junctions` inside the land polygon), `Λ` relaxes smoothly towards the mean level of the
  plates meeting there: `Λ ← Λ + (1 − S(r/450))·(λ̄_j − Λ)`. [PROTO] Without it 1337 carries a 0.86 m
  depression at the apex of its lowland wedge; with 300 m it is 0.43 m; with 450 m it is gone.
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

Levels are the **rank** of the scores, normalised: lowest `λ = 0`, highest `λ = 1`, the rest evenly
between; ties by plate index. Special cases:

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

### 5.3 Shore fade and the "zero at the shore" proof

```
Γ(x) = S( clamp01( (H1(x) − SeaLevel) / CoastalRiseMeters ) )        S = smoothstep, CoastalRise = 26 m
```

- `Γ` is exactly 0 wherever `H1 ≤ SeaLevel`, i.e. at and seaward of the shoreline, and 1 from the top of
  the coastal rise (200 m inland). It follows Stage 1's own profile, so added slope is proportional to
  the datum's and vanishes where the datum's does.
- [PROTO] The alternative, a linear gate at 60–340 m or 150–400 m, builds a second coast-parallel ramp in
  the interior and cuts flat ground to 15–32 % of land. The chosen gate concentrates the upland's descent
  in the coastal band, where uplands meet the sea as 10–20° bluffs — the raised coast Stage 4C will
  characterise.
- At the vegetation floor (27 m, ~30 m inland) `Γ ≈ 0.04`: the sand band moves by well under a metre.

**Invariants, by construction:** every term is `≥ 0`; `Γ = 0` and `U2 = 0` seaward; therefore
`R4A = 0` exactly at and seaward of the shore, `H4A ≥ H2 > SeaLevel` on all land, the land mask
`{H > SeaLevel}` is bit-identical, and no island can appear or disappear.

### 5.4 Numbers and why

| Setting | Value | Justification |
|---|---|---|
| `PlateUpliftMeters` | **18 m** | ≥ 2.5 × the tallest knoll (7.2 m) so the two never muddy; the Stage 1 interior rise is 30 m, so an upland roughly doubles the interior relief without rivalling a 20–40 m T2 plateau standing on it; [PROTO] puts upland interiors at 80–95 m, i.e. in the empty band |
| `PlateEdgeHalfWidthMeters` | 110 m | maximum added slope `1.5 × 18 / 220 ≈ 0.12` (7°) — an edge you can walk and read, not a scarp |
| `JunctionRelaxRadiusMeters` | 450 m | smallest radius with no depression on the three seeds [PROTO] |
| `NoMassifUpliftScale` | 1.4 | §8 |
| `MinPlateLandAreaSqm` | 150 000 | the prototype's slivers are 0.01–0.09 km²; real districts ≥ 0.22 km² |

**Why no lowering.** [PROTO] Every mean-zero or negative term tried (A, N) creates closed depressions,
and analysis says why: `CoastHeightModel` is smoothstepped per span, so the datum has **zero slope at
200 m and 440 m inland** and along its medial crest; any term that rises towards the coast there makes a
lip. Lowlands therefore stand at the datum and are low *relative to* the raised plates; genuinely lowered
ground is a declared basin and belongs to Stage 3F.

---

## 6. Rolling hills — knolls

### 6.1 The mechanism

```
K(x) = max_i  a_i · (1 − q_i(x)/9)³₊          q_i = (u/σ_major,i)² + (v/σ_minor,i)²
```

One **candidate** per land crust site `i` (`CrustalPartition.SiteCentroid`, diameter `D_i` from its cell
count). A candidate becomes a knoll only if all hold at its centroid:

- `Λ ≥ KnollMinLevel = 0.34` — knolls stand on raised plates; lowland plains stay open (the negative
  space `WORLD_RELIEF_PRINCIPLES` §5 budgets for);
- `Γ ≥ 0.5` — not in the outer coastal band;
- its resistance draw `r_i ≥ 1 − KnollKeepFraction` (keep 0.55–0.65).

Shape and height:

| Property | Rule | Value |
|---|---|---|
| height | `a_i = KnollHeightMeters · (0.6 + 0.4 r_i) · (0.5 + 0.5 Λ)`, clamped to `RidgeSettings.CorridorIncisionMeters` | 6.5 m nominal, ≤ **7.2 m** |
| footprint | `σ_major = 0.22 D`, `σ_minor = 0.14 D`; compact kernel, zero outside `q = 9` | ≈ 60 × 38 m σ → **160 × 100 m at half height**, 360 × 230 m support |
| orientation | fall line of `H1 + Γ·P` at the centroid, from central differences at ±60 m, plus a ±20° draw | elongated **down**slope, like interfluves |
| combination | **max**, not sum | bounded by one knoll; order-independent in floating point |
| maximum slope | `≈ 0.53 a / σ_minor` | ≈ 5° |

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

On uplands knolls add 4–7 m to the 18 m plate top; on the apron they are suppressed where
`Γ·P_A > 0`; they are the **only** Stage 4A term that rises along a Stage 3C trajectory (§9).

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
- `W = lerp(W_gentle 180 m, W_steep 110 m, steepWeight)`, then `lerp(W, W_end 280 m, endness)` with
  `endness` = cosine between the direction from the end point and the end tangent — **constant along
  every outward ray**;
- `h(s) = 0.09 · Crest · (0.55 + 0.45 · CrestAt(s)/Crest)`, `c(s) = 0.18 · Crest · (same factor)`;
- `ApronBlendMeters = 10` using `MountainUpliftModel.SmoothMax` (blend capped by the smaller input, so
  `A = P_A` exactly where `U2 = 0`).

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
(61–97 m). Because the apron is monotone along outward rays, a trajectory leaving the massif descends
the apron and an outlet gap stays an outlet. Stage 3F will cut valleys into it and turn the skirt into
lobes between spurs. The apron does not read Stage 3; §9 and §19 check the interaction.

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

The apron and the plates are valley-compatible by construction; knolls are the only obstacle. A single
knoll never exceeds what 3F may cut; the "knolls alone" sum can pass 7.2 m when a trajectory crosses two
of them, and in the full relief part of it is absorbed by the plate descent. Criterion (§19): along
every trajectory, plate + apron adverse rise ≤ 1.5 m, `R4A` adverse rise ≤ 7.2 m, and no single climb
taller than `CorridorIncisionMeters`. **[OPEN] option 4A-K2**, not in v1: exclude knolls from 3C corridors. That
would make Stage 4A read Stage 3; it is held back so 4A stays independent of the stage 3F will unfreeze.

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

---

## 12. Closed depressions

### 12.1 Contract

1. **Stage 4A creates no closed depression.** Declared basins exist only from Stage 3F.
2. Every land point drains to the sea on `H4A`: the priority-flood fill of `H4A` from the ocean differs
   from `H4A` by at most the micro tolerance.
3. **Tolerance:** a filled component counts if its max fill depth ≥ **0.25 m** *and* its area ≥ **100 m²**.
   The hydrology lake thresholds (`MinLakeAreaSqm = 4 000`, `MinLakeDepthMeters = 1.5`) are an order of
   magnitude looser, so no 4A depression can ever become a lake. Micro components below the tolerance are
   reported (count, max depth), not failed.
4. A failure is **fixed at the source** (a construction rule or a setting) and re-measured. Stage 4A has no
   fill, repair or re-planning pass.

### 12.2 Construction rules (why the contract is expected to hold)

| Rule | Mechanism it removes | Evidence [PROTO] |
|---|---|---|
| all terms ≥ 0 | lips against the datum's zero-slope rings (200 m, 440 m) and medial crest | A: 4–5 depressions; N: 35–40 |
| convergent boundaries are one-sided block edges, not two-sided belts | strike troughs | B/C1: 1337 depression 0.41–0.48 m |
| junction relaxation (450 m) | notch at a lowland wedge's apex | 0.86 → 0.43 → 0 m |
| a level-0 plate must have ≥ 150 m of coast frontage | lowland without outlet | (no case on 3 seeds; structural necessity) |
| apron reach constant along outward rays | apron rising outwards | C3: 0.60 m → 0 |
| knolls: compact kernels, max-combined, ≥ 200 m apart, downslope elongation | rims closing between hills | knolls alone: 0 |
| `NoMassifUpliftScale ≤ 1.4` | junction depression growing with amplitude | ×1.6: 449 m² at 0.27 m |

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

Proposed, **not implemented**. Only what the dump, tests and the later integration need.

```csharp
namespace GameDev.World.TerrainGen
{
    /// Immutable numbers for Stage 4A. Separate struct for the same reason MassifSettings is:
    /// Stage 1 and 2 are frozen by hash and their determinism runs through their own settings types.
    public struct IslandReliefSettings
    {
        public float PlateUpliftMeters;            // 18
        public float NoMassifUpliftScale;          // 1.4
        public float PlateEdgeHalfWidthMeters;     // 110
        public float JunctionRelaxRadiusMeters;    // 450
        public float MinPlateLandAreaSqm;          // 150 000
        public float ConvergentMinLandLengthMeters;// 150
        public float MinLowlandFrontageMeters;     // 150
        public float KnollHeightMeters;            // 6.5, clamped to RidgeSettings.CorridorIncisionMeters
        public float KnollKeepFraction;            // 0.6
        public float KnollMinLevel;                // 0.34
        public float KnollMajorSigmaPerDiameter;   // 0.22
        public float KnollMinorSigmaPerDiameter;   // 0.14
        public float KnollYawJitterDegrees;        // 20
        public float ApronHeightPerCrest;          // 0.09
        public float ApronCapPerCrest;             // 0.18
        public float ApronReachGentleMeters;       // 180
        public float ApronReachSteepMeters;        // 110
        public float ApronReachEndMeters;          // 280
        public float ApronTalusDegrees;            // 22
        public float ApronBlendMeters;             // 10
        public float ApronEndHeightFloor;          // 0.55
        public float MaxReliefMeters;              // 100 — asserted, never clamped
        public static IslandReliefSettings Default { get; }
        public void Clamp();
    }

    public sealed class IslandRelief
    {
        public static IslandRelief Build(
            GameDev.World.Seed.IWorldSeed worldSeed,
            Stage1Result stage1,                    // coast datum, crust partition, land raster
            MacroPlates plates,                     // from Stage2AResult.Plates
            IReadOnlyList<MountainMassif> massifs,  // may be empty
            IslandReliefSettings settings);

        /// Relief in metres, >= 0, exactly 0 at and seaward of the shore.
        /// stage1Height and upliftMeters are the caller's already-evaluated H1 and U2 at (x, z).
        public float At(float worldX, float worldZ, float stage1Height, float upliftMeters);

        /// The same value with its parts, for the dump and tests.
        public IslandReliefSample Sample(float worldX, float worldZ, float stage1Height, float upliftMeters);

        public IReadOnlyList<ReliefPlate> Plates { get; }        // index, land area, frontage, score, level
        public IReadOnlyList<ReliefJunction> Junctions { get; }  // position, member plates, mean level
        public IReadOnlyList<ReliefKnoll> Knolls { get; }        // site index, centre, yaw, sigmas, height
        public bool HasMassif { get; }
    }

    public readonly struct IslandReliefSample
    {
        public float Gate { get; }        // Γ
        public float PlateLevel { get; }  // Λ
        public float Plate { get; }       // Γ·P
        public float Knoll { get; }       // Γ·K
        public float Apron { get; }       // A
        public float Total { get; }       // R4A
    }

    public static class IslandElevation
    {
        /// H4A = StructuralElevation.At + relief, with H1, the coast distance and U2 evaluated once.
        public static float At(Stage1Result stage1, MassifUpliftModel uplift, IslandRelief relief,
                               float worldX, float worldZ);
    }
}
```

Why exactly these:

- `At` takes `stage1Height` and `upliftMeters` because the gate needs `H1` and the apron needs `U2`,
  and the caller has both; recomputing them would double the coast-distance query that already
  dominates the bake (tech debt 16).
- `Sample` exists because tests must assert each part at the shore and the dump must image each part.
  It is not for the bake.
- The three structure lists are what "why is this hill here" is answered from, and what the dump prints.
- **Not proposed:** an interface, a provider, a component registry, a ScriptableObject profile (a later,
  separate authoring step, preflight M8), publishing the structure on `WorldDefinition`, or a breakdown
  exposed to `TerrainSurfaceRules` (the apron is floor, and the rules classify it by slope as they
  should).

---

## 16. Expected visual result

The prototype's top-down relief and hillshade images support the descriptions below. Its untextured
voxel renders did not separate 4A from baseline at eye level or from 300 m, so **visual acceptance
requires the eye-level capture the preflight lists as M9**. The honest expectation: Stage 4A changes
how the island *reads when you walk it* much more than how it looks from the air.

### 110201

- **Silhouette:** unchanged (Stage 1).
- **Shore → lowland:** the southern bays open onto a broad plain at 30–65 m (the lower plate) that runs
  inland to the foot of the massif. The west arm stands a step higher (level 0.33–0.67).
- **Lowland → upland:** crossing to the north, the ground climbs a ~200 m-wide, ~7° edge onto the
  northern upland (the upper plate), whose top sits at 80–95 m with a dozen 4–7 m knolls elongated
  downslope towards the north coast. The north coast becomes a raised shore with 10–20° bluffs.
- **Upland → foothills → massif:** from the north the upland rises into a long piedmont (≈180 m, ≤ 16°),
  then a 22° talus, and the massif's gentle flank emerges from it at 110–130 m instead of from a
  63 m plain. From the south the lowland meets the massif's steep flank with only a short, steep apron —
  the scarp faces the lowland. Along strike, foothill tails reach 280 m past both ends.
- **What stops reading:** the nested coast-parallel rings and the straight medial roof creases across the
  north; the hard corner at the massif's foot.
- **What still reads:** a massif crown ringed by > 45° ground (until 3F/2B).

### 424242

- The massif runs N–S west of centre. **West and south** stand as uplands with ~20 knolls; the
  **north-east** plate is the lowland plain, and the massif's steep eastern flank faces it.
- The apron wraps the whole massif; on the gentle west side it merges into the upland, on the east it is a
  short steep skirt. **Watch item:** in the relief image the apron reads as a continuous ring (§20 R4).
- The islet is untouched in practice (`Γ < 0.5`, no knolls).

### 1337 (no massif)

- A **south-western highland** (25 m block, knolls) and a **north-eastern middle plateau** (12 m) meet a
  **south-eastern lowland** that opens onto three coasts. The highland's eastern edge follows the
  convergent south arm as a straight ~800 m edge (edge-warp candidate, §10).
- The highest ground moves from "wherever the coast is farthest" (72 m) to the highland (86 m).
- **Not delivered by 4A:** a landmark visible from everywhere. That arrives with T2 plateaus (4B).

### The sequence the player walks

| Transition | Before | After 4A |
|---|---|---|
| shore → lowland | beach ramp, then the same ramp | beach ramp, then an open plain at datum |
| lowland → hills | none | an edge onto raised ground, knolls appearing over its rim |
| hills → upland | none | the knolls sit on the upland top; swales between them lead downslope |
| upland → foothills | none | a piedmont that steepens into talus |
| foothills → massif | a 60° wall from a flat plain | the wall emerges from talus 40–70 m above the plain (still a wall above that) |

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
| per-span smoothstep leaves zero-slope rings at 200 m and 440 m | negative relief would lip | removed as a problem by the positive-only rule |
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
| S4 | `IslandRelief.At` equals the bulk lattice value bit for bit (full lattice on one seed, 10 000 samples on the rest) |
| S5 | two builds in one process and six fresh processes give identical relief hashes |
| S6 | Stage 1 golden hashes, Stage 2B field hashes (`1de91e469513054b`, `9d9d7b3c2560bc36`), full Stage 3 suite, and `OverworldTerrainBakeTests`' pinned authority hash unchanged (4A is not yet on the production path) |
| S7 | perturbing only `terrain.relief.knoll` changes only `Knoll`; plates, junctions and apron identical |
| S8 | depressions (≥ 0.25 m and ≥ 100 m²) = 0 via `HydrologyGrid` + `DepressionFill`; micro count reported |
| S9 | every knoll ≤ `CorridorIncisionMeters`; every apron fill ≤ 0.18 × crest |
| S10 | apron monotone non-increasing along 64 outward rays per massif |
| S11 | along every Stage 3C final trajectory: plate + apron adverse rise ≤ 1.5 m, `R4A` adverse rise ≤ 7.2 m, no single climb above `CorridorIncisionMeters`; at every Stage 3E outlet, `R4A` non-increasing in the outlet's downstream direction |

**Geometric (seed-level, from the dump):** the "Target" column of §11 for 110201, 424242, 1337; the
no-massif column for 1337, 256, 777.

**Visual (human review of dump images, then eye-level captures before integration):**

- the interior no longer reads as nested coast-parallel rings; medial creases no longer dominate;
- at least one upland and one lowland are nameable on every seed, with a visible edge between them;
- knolls read as hills on raised ground, not as a texture, and not on the plains;
- the massif rises out of foothills on at least its gentle flank and along strike; no horizontal bench,
  no concentric ring dominating;
- a no-massif world shows a highland / plateau / plain hierarchy;
- no pattern that repeats at a fixed spacing; no ruled straight edge longer than ~800 m without a
  structural reason visible in the plate image.

---

## 20. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | Relief reads as a "raised island" rather than districts | levels are rank-normalised (a lowland always exists); knolls only on uplands; review against §19.2 visual |
| R2 | Depressions appear on seeds the prototype did not cover | seven-seed S8; rules in §12.2; failure fixed at the source |
| R3 | Flat share too low on seeds with long upland coasts (424242: 24.9 %) | levers: `PlateUpliftMeters` 14–20, apron reaches, `KnollKeepFraction`; 4B restores flat tops; island-level ≥ 35 % is re-checked after 4B |
| R4 | The apron reads as a ring and sits at its 25 % area limit (424242) | lever: apron height × `(0.6 + 0.4 (1 − Λ_side))` so the upland side is buried less, or shorter reaches; 3F valleys lobe it |
| R5 | Straight plate edges read as ruled lines (1337 south arm) | optional edge warp (§10), off by default, separate decision |
| R6 | Knolls obstruct 3F valleys | bound ≤ 7.2 m; S11; option 4A-K2 |
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

R4A(x) = Γ(x) · [ P(x) + K(x) ] + A(x)

Γ(x)   = smoothstep( clamp01( (H1(x) − SeaLevel) / CoastalRiseMeters ) )
P(x)   = PlateUpliftMeters · m_world · Λ(x)               m_world = 1 (massif world) | 1.4 (no massif)
Λ(x)   = plate level λ_k blended across boundaries (half-width 110 m), relaxed to the junction mean
         within 450 m of on-land junctions; λ = rank of net overriding length at convergent boundaries
K(x)   = max_i a_i (1 − q_i(x)/9)³₊ over eligible crust sites (Λ ≥ 0.34, Γ ≥ 0.5, r_i ≥ 0.4)
A(x)   = SmoothMax(U2, Γ·P_A, 10 m) − U2
P_A    = h(s)(1 − ρ/W)²  (ρ ≥ 0)   |   min(h(s) + tan 22°·(−ρ), c(s))  (ρ < 0)
         h = 0.09·Crest·f(s), c = 0.18·Crest·f(s), f = 0.55 + 0.45·CrestAt(s)/Crest
         W = 110 (steep) … 180 (gentle) … 280 m (along strike), constant along outward rays
```

### Proposed settings

`IslandReliefSettings.Default` as listed in §15.

### Expected contribution and scales

| Term | Height | Scale | Share of land |
|---|---|---|---|
| plate uplift | 0–18 m (25 m no-massif) | plates 0.4–1 km², edges ~200 m | uplands 40–62 %, lowlands 20–35 % |
| knolls | 4–7.2 m | 160 × 100 m half-height, ≥ 200 m apart, 10–25 per island | 10–20 % |
| apron | fill to 26–52 m | 110 / 180 / 280 m reach | ≤ 25 % |
| total `R4A` | mean 5–14 m, max ≤ 60 m expected, ≤ 100 m asserted | | |

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

**Step 1 — Stage 4A implementation (TerrainGen only, dump-first).** New files above. Nothing on the
production path calls them. Accept on §19.2 for seven seeds plus human review of the dump images.
Production heights, the pinned authority hash and the saved scene stay bit-identical.

**Step 2 — integration (separate, reviewable).**

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
DESIGN:         READY
STAGE 1:        KEEP FROZEN
STAGE 3A–3E:    FROZEN
NATIVE TERRAIN: PASS
STAGE 4A:       READY FOR IMPLEMENTATION

RECOMMENDED NEXT STEP:
Implement Stage 4A as new TerrainGen files only — IslandReliefSettings, IslandRelief, IslandElevation,
TerrainGenIslandReliefDump and TerrainGenStage4ATests — dump-first, without switching
TerrainAuthorityField, and accept it on §19.2 for 110201, 424242, 20260905, 7, 1337, 256 and 777.
```

Owner confirmations requested at the dump review (none blocks Step 1): positive-only relief (§5.4);
the Stage 4A-level flat-ground threshold (§11, "land < 5°"); edge warp on/off (§10); option 4A-K2 (§9.3);
the `GeneratorVersion` bump at integration (§21).
