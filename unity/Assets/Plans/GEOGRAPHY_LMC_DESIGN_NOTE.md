# GEOGRAPHY_LMC_DESIGN_NOTE.md

**Status:** Step 0 — design proposal  
**Working experiment name:** LMC  
**Official experiment ID:** ~~Not assigned~~ **G3e** (assigned by the owner 2026-10-23, condition C1; agent review note 11)  
**Authorization:** S-LM3 + S-LM1 approved for comparison on a shared LM2 regional layout  
**Current phase:** Design only. No implementation or generation runs authorized by this note.  
**Production status:** Unchanged  
**Git status:** No commit or push authorized

Version: 0.8

Last Updated: 2026-10-23 (v0.8: the official ID G3e in the header field, agent review note 11 — C1, C8, C9 decided;
otherwise the owner's text unchanged. v0.7: agent review note 10 — R8, slope diagnostics and the whale-back settled in the
specification v0.4; the owner's text unchanged. v0.6: agent review note 9 — the note (v0.5) approved as the research plan, Step 1 not
authorised, LMC kept as the working name; the owner's text unchanged. v0.5: agent review note 8 — the specification
approved, the state of §12; v0.4: agent
review note 7 — the owner's position and the technical specification; the owner's text is unchanged since v0.3. v0.3: R5 extended to designated outlets by the owner's decision; v0.2: the owner's
revision, replacing the agent's v0.1, kept as [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md))

Depends On:
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md),
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) (§§14, 15),
[GEOGRAPHY_TARGET_BRIEF_G2.md](GEOGRAPHY_TARGET_BRIEF_G2.md) (D6),
[WORLD_RELIEF_PRINCIPLES.md](WORLD_RELIEF_PRINCIPLES.md),
[PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md](PROCEDURAL_TERRAIN_STAGE_3_DESIGN.md),
[HYDROLOGY_GENERATION_PLAN.md](HYDROLOGY_GENERATION_PLAN.md),
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-1 … GEO-9,
[AGENTS.md](../../../AGENTS.md);
reference image [`References/G2_target_island_concept_2026-10-20.webp`](../../../References/G2_target_island_concept_2026-10-20.webp)

Referenced By:
[GEOGRAPHY_POST_G3C_REVIEW.md](GEOGRAPHY_POST_G3C_REVIEW.md) §§5, 6, 7;
[GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md](GEOGRAPHY_FIRST_TERRAIN_RESEARCH.md) §§9, 15;
[GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md);
[GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md);
[GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md);
[99 Open Questions](../../../99%20Open%20Questions.md) GEO-3, GEO-7, GEO-8;
[14 World Generation Roadmap](../../../14%20World%20Generation%20Roadmap.md); [AGENTS.md](../../../AGENTS.md)

---

## 1. Purpose

LMC investigates alternative representations for constructing a coherent fantasy-island terrain after G3c failed its preregistered acceptance criteria.

G3c demonstrated that settlement candidate sites can be found, but its surface representation repeatedly produced:

- Crescent-shaped ridge systems or a central radial peak.
- Similar, faceted slopes and whale-back ridges.
- Plateau shapes resembling cylinders placed on top of the terrain.
- Coastal cliff bands that were too continuous.
- Hydrology plans rejected because some routes flowed uphill.
- Formally successful river metrics despite some highland regions lacking river sources.

The next experiment must compare different surface-construction methods, not merely tune the parameters of the failed method.

### Objective

Compare two surface-construction methods using the same regional layout:

- **S-LM3:** a steady-state river-erosion-inspired surface model driven by structural slope fields.
- **S-LM1:** a branched ridge-and-valley skeleton with normalized cross-slope profiles.

Both methods consume the same LM2 regional layout. The comparison must reveal whether either method can produce the approved target geography with acceptable visual composition, topology, traversal, settlement capacity, and hydrology.

The experiment does not authorize production adoption.

## 2. Approved target

The approved project concept image is:

`References/G2_target_island_concept_2026-10-20.webp`

It is a project reference inspired by the visual direction of DragonSword: Awakening, not an official map of the game.

The target geography consists of:

- A dominant but branching mountain system with multiple distinct peaks, ridges, spurs, saddles, and valleys.
- Several large plateaus integrated into ridges and foothills, distributed across different regions.
- Plateau tops with readable surfaces, irregular edges, and a combination of escarpments, ridge connections, and natural ramps.
- Valleys and lowlands that form deliberate geographical regions.
- Coastal morphology that varies according to the adjacent hinterland.
- Rivers that originate in appropriate highland regions and follow coherent corridors to designated outlets.
- Routes connecting settlements, important regions, passes, and accessible plateau tops.
- Capacity for 3–5 settlements and numerous varied POIs.
- Visual coherence at island overview, aerial regional, and player scales.

The experiment must not attempt to reproduce the concept image's exact coastline, region placement, or silhouette. It must reproduce its geographical principles.

## 3. Fixed experimental conditions

### 3.1 Coastline and world footprint

Use **contour A only**, based on the current Stage 1 land mask.

The experiment must not vary island footprint or compare alternative coastline outlines. This prevents the area-versus-shape ambiguity encountered in G3c.

Verify the land mask against the existing approved baseline before any future run. Any mismatch invalidates the comparison until explained.

G3d, coastline-shape diversity, and the 3×3 world-scale question remain outside this experiment.

### 3.2 Seeds

| Role | Seeds | Purpose |
|---|---|---|
| Calibration | `110201`, `1337` | Develop and compare the methods; select one frozen configuration |
| Held-out evaluation | `1005–1014` | Independent evaluation after freeze |
| Reserve | `1015–1024` | Replacement evaluation seeds if the frozen evaluation becomes invalid |

Held-out seeds must not be generated, inspected, or used to make design choices before the freeze record has been created.

Any change to the implementation, configuration, regional layout rules, or other evaluation-relevant inputs after freeze invalidates the held-out evaluation. A replacement evaluation must use previously unseen seeds and be documented as a new evaluation.

### 3.3 Shared LM2 regional layout

Generate one LM2 layout per seed and reuse that exact layout for both surface methods.

The layout describes:

- A mountain-system region occupying a target of 30% of land.
- Lowland regions targeting 30% of land, with designated river outlets.
- Regional corridors and connections.
- Two to four plateau regions.
- Other uplands and transition regions.
- The intended relationship between the mountain system, lowlands, plateau regions, and coast.

The percentages are layout targets, not acceptance criteria by themselves. The layout must be validated as a valid, non-overlapping and connected regional specification before it is consumed by either surface method.

Mountain-core position along the ridge axis must be seed-dependent. Do not force the mountain system into the same central position for every seed.

Both methods must receive identical layout data for a given seed. Differences in regional layout between methods are not allowed.

## 4. Surface method S-LM3

### 4.1 Concept

S-LM3 constructs the terrain using a steady-state river-erosion-inspired height model over structural slope fields.

The intended characteristics are:

- Convex slopes around drainage divides.
- Concave river valleys.
- Drainage following a declared regional graph toward designated outlets.
- No accidental closed depressions, by construction, unless an explicitly declared basin/lake model is later introduced.
- Automatic mapping of the structural model into the target elevation range.

The implementation must explain the mathematical model and its assumptions before coding begins. Calling the method “erosion” does not itself establish physical correctness.

### 4.2 Required structural inputs

The method must consume the shared LM2 layout and its structural relationships, including:

- Highland and lowland regions.
- Ridge and valley directions.
- Declared drainage corridors and outlets.
- Plateau regions and their edge types.
- Regional connectivity constraints.

It must not derive all terrain features from a single distance-to-river or distance-to-coast field.

### 4.3 Plateau integration

Plateaus must be integrated into the surface construction, not added as independent cylindrical height overlays.

Plateau tops must be compatible with the regional skeleton and may be intersected by valleys. Their edges must follow declared types:

- **Ridge connection:** the plateau joins a ridge or elevated region.
- **Escarpment:** the plateau edge has a pronounced elevation drop.
- **Ramp:** the plateau has a natural ascending or descending transition.

These edge types describe structural intent. The generated surface must still be checked for visible seams, artificial walls, and implausible transitions.

### 4.4 Known risks

- A uniform or excessively smoothed appearance.
- Similar slopes across different regions.
- Slopes clustering around the configured maximum.
- A lack of visually distinct mountain peaks.
- Overly regular drainage patterns.
- Plateau boundaries that look detached from the surrounding terrain.

These risks must be evaluated using the shared metrics and visual review, not hidden by post-processing unique to S-LM3.

## 5. Surface method S-LM1

### 5.1 Concept

S-LM1 constructs the surface from a branched ridge-and-valley skeleton and slope bands.

The method generalizes the existing slope-band approach to a branching mountain system. Cross-slope coordinates are normalized between a ridge spur and its associated valley, allowing a profile to be evaluated consistently across different regions.

The intended structure includes:

- A branching ridge skeleton.
- Distinct peaks, saddles, spurs, and valleys.
- Explicit relationships between ridges and their adjacent valleys.
- Plateaus integrated into the same structural representation.
- Continuous height evaluation at individual surface points.

### 5.2 Structural requirements

The skeleton must support multiple connected ridges and several distinct peaks. It must not degenerate into a single smooth arc or a radial central cone.

Plateau boundaries must be represented in the structural graph or its compatible region representation. They must not be introduced as unrelated geometry after the rest of the surface is complete.

The surface evaluator must be deterministic for identical inputs and must document how it resolves areas where structural influence fields overlap.

### 5.3 Compatibility

S-LM1 is expected to have a closer relationship to the project's existing pointwise terrain-generation architecture. This is a hypothesis to evaluate, not a reason to prefer S-LM1 before visual and topological assessment.

### 5.4 Known risks

- Faceted transitions between slope bands.
- A repeated whale-back profile.
- Excessive dependence on hand-authored ridge curves.
- Unnatural junctions where branches meet.
- Plateau boundaries that do not align with valleys or ridge structure.
- Local conflicts between pointwise surface evaluation and the declared drainage graph.

These risks must be recorded explicitly during calibration.

## 6. Shared elevation targets

The following are provisional design targets for the experiment and require explicit owner confirmation before implementation if they have not already been approved elsewhere:

- **Main summit elevation:** 330–430 m.
- **Pass elevation:** 40–80 m above its associated lowland.
- **Plateau edge height classes:** 18–40 m and 60–120 m.

The ranges describe target relationships, not a requirement that every seed or every feature use the same height.

Before coding, document:

1. The reference datum used for all elevations.
2. How summit height is measured.
3. How the associated lowland for a pass is selected.
4. How plateau edge height is measured.
5. Whether a plateau edge may cross between the two height classes.
6. How target elevations are mapped into the project's actual height representation.

Do not tune these ranges separately for S-LM1 and S-LM3 to favor one method. If calibration demonstrates that the targets are incompatible with the approved design, stop and request an owner decision rather than silently changing them.

## 7. Evaluation criteria

Use four distinct classes of evidence:

1. Hard gameplay and topology gates.
2. Owner visual review at three scales.
3. Diagnostic metrics.
4. Provisional design assumptions requiring later confirmation.

Do not combine these classes into a single opaque score.

### 7.1 Hard gates

The following gates are proposed for the LMC evaluation. Their exact calculations must be specified in the metrics specification before any held-out evaluation.

| ID | Gate | Required evidence |
|---|---|---|
| R4 | Settlement capacity | Required settlement sites exist and all are in one connected pedestrian component |
| R5 | No empty highland drainage | At least one river originates in a mountain or plateau region, and every such river system ends at a designated outlet |
| R7 | Terrain hygiene | No accidental pits, unintended uphill valley axes, or other prohibited topological defects |
| R8 | Accessibility | Settlement sites, designated plateau tops, and the selected pass are reachable according to the declared pedestrian model |
| R9 | Regional routes | Pedestrian routes connect the regions containing settlements |
| R10 | Hydrology compatibility | The production hydrology planner accepts the generated river network |

**Important:** R10 is a compatibility test only. Passing it does not prove that the river network is geographically convincing. R5 prevents a vacuous pass where no highland river exists.

Any rejection by the production planner must be recorded with the failing seed, failure reason, and relevant regional topology. Do not modify the production planner as part of LMC.

### 7.2 Indirect indicators

The following indicators inform the review but are not independently hard gates unless explicitly approved later:

- **R1:** mountain-system composition and distinct peaks.
- **R2:** ridge and valley structure.
- **R6:** coastal morphology and its relationship to the hinterland.

### 7.3 Diagnostics

- **R3:** usable lowland and settlement-space measurements.
- **R11:** additional regional or traversal diagnostics.
- Existing G0 metrics.
- Terrain-tier classification.
- Cliff and wall measurements.
- Drainage and basin statistics.

Tier classification is diagnostic for LMC. A plateau must not be rejected solely because it falls outside the existing T2 range. Production tier rules remain unchanged until the owner explicitly approves a separate change.

### 7.4 Visual quality questions Q1–Q9

Preserve the existing Q1–Q9 definitions in the authoritative research document. They must explicitly detect the known G3c failure modes, including:

- Faceted or planar slope bands.
- Whale-back ridge shapes.
- Radial or conical mountain forms.
- Plateau tops that resemble detached drums or cylinders.
- A continuous, artificial band of coastal cliffs.
- Identical or near-identical regional compositions across different seeds.
- Other existing Q-items in the approved research record.

Do not invent replacements for existing Q1–Q9 definitions during implementation. If any definition is ambiguous, clarify it in the metrics specification before calibration.

### 7.5 Passage rule

A method passes the experiment only if:

- All hard gates pass on at least 7 of the 10 held-out seeds; and
- The owner accepts the visual composition on at least 7 of the 10 held-out seeds.

A candidate failing either condition does not pass.

The numerical thresholds must not be changed after evaluation begins to rescue a candidate.

## 8. Visual review protocol

Use comparable fixed camera conventions for both methods.

The review must include:

1. **Island scale:** mountain silhouette, region distribution, plateau placement, coast variation, and overall composition.
2. **Regional aerial scale:** multiple peaks, branching ridges, valley corridors, plateau integration, and coastal transitions.
3. **Player scale:** cliffs, slopes, plateau edges, natural routes, and the transitions between upper and lower regions.

Camera selection must not favor one method. Include both fixed G1 views where applicable and structure-aware views that reveal each candidate's important features.

The comparison sheets must label the methods **X** and **Y**, with the mapping from X/Y to S-LM1/S-LM3 withheld from the owner during the visual review.

The review record must include:
- Seed and camera identity.
- The D6 aspects being assessed.
- A pass/fail or accepted/rejected judgment.
- A short reason for each judgment.
- Notable artifacts and counterexamples.

Do not use visual acceptance to override a failed hard gate, and do not use aggregate metrics to override a rejected visual composition.

## 9. Calibration, freeze, and held-out evaluation

The experiment must follow these phases:

### Step 0 — Design

This document defines the proposal and the unresolved owner decisions. No code or runs are permitted in this phase.

### Step 1 — Isolated implementation and calibration

Only after explicit owner authorization:

- Implement the shared LM2 layout and the two surface methods in isolated Editor-only research code.
- Use calibration seeds `110201` and `1337` only.
- Verify that the metrics implementation reproduces the relevant G3c results on the original G3c calibration grids before using it to compare LMC candidates.
- Record all calibration changes, failure cases, and parameter choices.
- Do not inspect held-out seeds.

Reproducing G3c metrics is a validation of the measurement harness, not a requirement that LMC reproduce G3c's terrain or results.

### Step 2 — Owner review and method selection

Present the blind comparison on calibration seeds, the metrics, the known defects, and the proposed frozen configuration.

The owner decides whether to proceed, change the design, or stop. A method must not be selected solely because it passes more metrics if its composition is visibly poor.

### Step 3 — Freeze

Before any held-out seed is generated or inspected, create a freeze record that identifies:

- Source revision and hashes of all relevant research code.
- LM2 layout algorithm and configuration.
- Surface method parameters.
- Contour A and its land-mask hash.
- Metric definitions and thresholds.
- Camera definitions and rendering settings.
- Relevant tool and data versions.
- Expected output inventory and deterministic-output checks.

Both methods must use the same layout, evaluation rules, and presentation settings. The selected configuration and method implementations must not be altered after freeze.

### Step 4 — Explicit authorization for evaluation

Held-out evaluation is a separate authorization step. Freeze does not automatically authorize it.

### Step 5 — Held-out evaluation

After explicit authorization, evaluate seeds `1005–1014`, without further tuning. If any evaluation-relevant change occurs after freeze, invalidate the evaluation and document a new evaluation plan using unseen seeds.

## 10. Implementation isolation and production protection

If Step 1 is authorized, research code is expected to use six Editor-only files following the `TerrainGenLmc*` naming convention. The exact file breakdown must be documented before implementation.

Requirements:

- Keep all LMC implementation inside Editor-only research code.
- Do not call or modify G3c research helpers; copy or independently implement the necessary measurement helpers.
- Do not add references from production code to LMC.
- Do not modify production TerrainData, scenes, materials, shaders, assets, or generator stages.
- Verify the approved land-mask hash and production hashes before and after every run.
- Abort and report if any protected hash changes unexpectedly.
- Preserve G3c code, results, and artifacts.
- Do not delete scratchpad evidence.

No code has been authorized by Step 0 itself.

## 11. Decision rules and follow-up

Possible outcomes:

- **One method passes:** recommend it for a separate owner-reviewed research stage; do not automatically promote it to production.
- **Both methods pass:** compare robustness, complexity, compatibility, and visual quality before recommending a candidate.
- **Neither method passes:** identify which failure modes are shared and which are representation-specific. Do not start G3d as a substitute for solving the terrain representation problem.
- **Results are inconclusive:** document the uncertainty and propose the smallest additional experiment that can resolve it.

G3d coastline-shape diversity remains deferred until a promising terrain representation has been identified.

No experiment outcome may be attributed to island area because LMC holds contour A fixed.

## 12. Owner decisions required before Step 1

The following decisions must be recorded explicitly:

1. **Approve this design note:** yes/no.
2. **Authorize Step 1 implementation and calibration:** yes/no. This authorization does not include held-out evaluation.
3. **Experiment naming:** retain LMC as a working name or assign an official experiment ID.
4. **Elevation targets:** approve or revise the summit range of 330–430 m and pass offset of 40–80 m above the associated lowland.
5. **Plateau edge classes:** approve or revise the proposed classes of 18–40 m and 60–120 m.
6. **Metric specification:** approve the detailed calculation and threshold specification for R4, R5, R7, R8, R9, R10, and Q1–Q9 before calibration starts.
7. **S-LM3 model contract:** confirm whether the proposed steady-state river-erosion-inspired surface model is permitted as an experimental representation under the existing Stage 3 design constraints. This is not approval to change production architecture.

If any decision remains unresolved, do not silently infer approval. The agent must report the blocking item and wait for the owner's decision.

## 13. Step 0 completion checklist

Step 0 is complete when:

- The shared LM2 layout and both surface methods are described.
- Their known risks and the comparison protocol are documented.
- The metrics, visual-review method, and passage rule are specified.
- Calibration and held-out seed roles are unambiguous.
- The freeze procedure and production-protection requirements are documented.
- Owner decisions are explicit and unresolved items remain marked as such.

**Step 0 does not include:** writing code, launching Unity, generating terrain, running calibration, inspecting held-out seeds, starting G3d, changing production, or committing/pushing.

**Expected result:** a reviewed and approved research design ready for a separately authorized Step 1.

---

## Agent review notes (2026-10-21) — not part of the owner's text

Recorded when the owner's revision replaced the agent's v0.1. They change no wording above; the one change to
the owner's text since v0.2 — R5 in §7.1 — was made on the owner's decision (note 1).

1. **Resolved 2026-10-21 — R5 and designated outlets.** v0.2's R5 kept only non-vacuity ("at least one river
   originates in a mountain or plateau region"), while the resolved GEO-7 classification
   ([99 Open Questions](../../../99%20Open%20Questions.md) GEO-7; post-G3c review §2.6) lists "rivers with elevated
   sources reaching designated outlets" among the hard gates. **The owner decided** to extend R5: *at least one
   river originates in a mountain or plateau region, and every such river system ends at a designated outlet* —
   now the R5 row of §7.1. The exact calculation (which systems count, how "ends at a designated outlet" is
   measured) belongs to the metrics specification of §12 item 6.
2. **Q1–Q9 and thresholds.** The Q1–Q9 of record are the post-G3c review §3.3: outline definitions, report-only,
   no threshold. §12 item 6 asks for a threshold specification that includes Q1–Q9. Under GEO-7's classification
   a morphology figure gates nothing unless a gameplay purpose is recorded, so the agent's recommendation is that
   any Q threshold the owner approves act as a flag for the visual review, not as a gate. Candidate operational
   forms of Q1–Q9 (and of R4, R5, R7–R10) are in the technical draft §§6.1–6.4, as input to the metrics
   specification only.
3. **Plateau edge classes.** The owner's classes (18–40 m and 60–120 m) replace v0.1's ratio classes. With main
   summits of 330–430 m they still sample several ratios of plateau edge to mountain relief, which GEO-8's research
   item asks for; tier conformance stays a diagnostic (§7.3).
4. **Held-out seeds.** Seeds 1005–1014 were measured on production `H4A` by G0's sweep (2026-10-18). With contour A
   fixed, LMC's islands on those seeds are different terrain, and no G3-family or LMC code has generated them; this
   is not a contamination under §3.2, recorded for completeness.
5. **Formatting.** The standard header fields of
   [001 Documentation Standards](../../../001%20Documentation%20Standards.md) (Version, Last Updated, Depends On,
   Referenced By) were added below the owner's header block, and a stray trailing "\" after the last line was
   removed. No other character of the owner's text was changed.
6. **Technical draft.** The agent's v0.1 is kept as
   [GEOGRAPHY_LMC_TECHNICAL_DRAFT.md](GEOGRAPHY_LMC_TECHNICAL_DRAFT.md) — not approved, not governing — as source
   material for the documents §§4.1, 6, 10 and 12 require before code (the S-LM3 mathematical model, the elevation
   definitions, the file breakdown, the metrics specification). The owner may discard it.
7. **2026-10-22 — the owner's position and the technical specification.** The owner supports this note (v0.3) as
   the research direction and has **not** authorised code or calibration; the next step set was to complete the
   technical specification and check it on historical G3c data without new generation. Done as
   [GEOGRAPHY_LMC_TECHNICAL_SPEC.md](GEOGRAPHY_LMC_TECHNICAL_SPEC.md): elevation definitions (§6 items 1–6, with a
   world-height datum), the S-LM3 model contract (§12 item 7), the S-LM1 ownership rule (§5.2), the gate and Q1–Q9
   calculations with flag levels (§12 item 6), the file breakdown (§10); checked on G3c's twenty lattices and analytic
   surfaces, with three corrections (K1–K3). It awaits the owner's approval; Step 1 stays a separate decision.
8. **2026-10-22 — the specification approved; Step 1 not authorised.** The owner approved the technical
   specification's §12 items 1–4. Against §12 of this note:

   | Item | State |
   |---|---|
   | 1 Approve this design note | supported as the research direction (2026-10-22); a formal yes is not recorded — **open** |
   | 2 Step 1 implementation and calibration | **not authorised** |
   | 3 Experiment naming | **open** (LMC remains the working name) |
   | 4 Elevation targets | **approved** with the specification's §2: main summit 330–430 m **world height** (306–406 m above the sea), pass 40–80 m above its associated lowland |
   | 5 Plateau edge classes | **approved** with the specification's §2: 18–40 m and 60–120 m |
   | 6 Metric specification | **approved**: the specification's §§7–8, with K1, K2 and the flag levels of §8.1 |
   | 7 S-LM3 model contract | **approved** as an experimental representation (specification §5); production is a G4 question |
9. **2026-10-22 — the remaining §12 decisions recorded (owner).** (1) This design note, **v0.5, is approved as the
   research plan**; the technical specification v0.3 and its §12 were approved earlier the same day. (2) **Step 1 is
   not authorised**: implementation and calibration are a separate stage that needs the owner's explicit
   authorisation; until then no new generation, no Unity run and no production change. (3) **LMC stays the working
   name**; no official experiment ID is assigned yet — one is to be chosen by the repository's naming rules **before
   implementation starts**. The final state of §12:

   | Item | State |
   |---|---|
   | 1 Approve this design note | **approved** — v0.5 as the research plan |
   | 2 Step 1 | **not authorised** — a separate authorisation; its conditions are in [GEOGRAPHY_LMC_STEP1_READINESS.md](GEOGRAPHY_LMC_STEP1_READINESS.md) |
   | 3 Naming | **LMC as the working name**; the official ID is chosen by the repository's rules before implementation |
   | 4–7 | approved (note 8) |
10. **2026-10-23 — readiness findings settled (owner).** The specification became v0.4: R8 covers every plateau top
    the layout declares (A1 — this note's §7.1 wording "designated plateau tops" now holds in the specification too);
    report-only slope diagnostics D-cap and D-reg answer the risks of §4.4 (A3); and, because no measure separated the
    whale-back on G3c's data, the **whale-back ridge shapes of §7.4 are judged in the owner's visual review** — Q3, Q4
    and Q4m are reported without a flag (option (b)). Step 1 is still not authorised.
11. **2026-10-23 — conditions C1, C8, C9 decided (owner).** **C1:** the official experiment ID is **G3e** — the next
    free letter in the G3 line of upstream-relief candidates (G3b, G3c; G3d stays reserved for coastline variety);
    "LMC" remains its working name. The header's ID field records it. **C8:** LMC's code is **not committed during
    Step 1**; before Step 3 (the freeze) the owner decides whether code and documentation go to a separate branch,
    because §9's freeze record asks for a source revision. **C9:** the reference data — the Python reference
    implementation, its results and G3c's four calibration grids with their report — is copied, MD5-verified, to
    `D:\Hobby\game-dev\research-data\lmc-reference\` outside the repository. Step 1 itself is still not authorised.
