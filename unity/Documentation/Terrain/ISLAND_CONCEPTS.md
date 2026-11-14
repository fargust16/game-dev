# Veilmouth Island and City — Constraint Reassessment and Three Concepts

Version: 0.3

Status: Draft — the owner chose concept B (§8); its ground was polished and waits for the owner's review before the city is rebuilt (§11)

Last Updated: 2026-11-14 (§11: the terrain polish and where its report is. Earlier: §8 to §10, the owner's choice of B, B worked out, and its ground in the engine. Earlier: first version: the constraints reassessed after the owner's new direction, the turns a district may take measured on the Combat Grid, and three concepts with plans, height maps and 3D previews)

Depends On:
[CURRENT_WORLD_AUDIT.md](CURRENT_WORLD_AUDIT.md), [MICROSPLAT_STAMPIT_RESEARCH.md](MICROSPLAT_STAMPIT_RESEARCH.md), [VEILMOUTH_CITY_PLAN.md](../../Assets/Plans/VEILMOUTH_CITY_PLAN.md), [SLICE_MODULAR_KIT_STANDARD.md](../../Assets/Plans/SLICE_MODULAR_KIT_STANDARD.md)

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) VS-ISLAND, [TERRAIN_POLISH_REPORT.md](TERRAIN_POLISH_REPORT.md)

---

# Purpose

The owner's direction of 2026-11-13 and 2026-11-14 replaces the first island proposal: the island and the city are designed together, the square city is not kept, and the rule is **a natural island, an organic Victorian city, rectangular modular architecture**. This document is Phase 0 (the constraints, read again), Phase 1 (three concepts) and Phase 2 (their height maps and 3D previews). Nothing in the production scene, terrain or city is changed. Work is on the branch `island-redesign`.

Every size, level and count below is an authoring default.

---

# 1. The owner's answers, recorded

| # | Question | Answer (owner, 2026-11-14) |
|---|---|---|
| 1 | Scope | MicroSplat on the Veilmouth slice's terrain only; the Overworld keeps URP TerrainLit. The integration is kept apart, so another place may take it later |
| 2 | The river | Neither earlier option. The river is designed again with the island and the city: a source in high ground, a valley, a course through or beside the city, the works and the harbour on it, a natural estuary. No ring forced round the city; a partial ring is allowed |
| 3 | The backdrop | Open ocean to the horizon. A far shore only if it truly helps the picture |
| 4 | The West Road | Designed again: it goes where the land and the game give it a purpose |
| 5 | Textures | The eleven CC0 sets from Poly Haven, with a manifest of source, licence and version; displacement only where the installed MicroSplat uses it. The library may be prepared now; where each surface lies waits for the approved geography |
| 6 | Git | Generated files stay out only where a checked way to make them again exists. Authored height maps, scripts and MicroSplat's configuration are versioned; large required files go to Git LFS or reliable storage; a fresh clone must be whole |
| 7 | The editor | Closed for the agent's batch runs |

**Also authorised:** the city's footprint, its districts' places, its blocks' turns, its streets and its public spaces may all be designed again. The kit's rectangular buildings and the established urban rules stay.

---

# 2. Phase 0 — the constraints, read again

Two research agents read the city's rules and the game's bindings to places; the lead checked the Combat Grid by measurement. Sources are in the two agents' reports, summarised here.

## 2.1 What stays (hard)

| Constraint | Source |
|---|---|
| Terrain 1000 × 1000 m at (−500, 0, −500), height map 1025, Sea Level 24 m | the owner's brief §3; audit §3 |
| Buildings are the Victorian Kit's rectangles: fronts of 10 and 15 m, depths of 8 and 10 m; single buildings of fixed size (civic 30 × 15, villa and tavern 15 × 15, chapel 13 × 10, cottage 6 × 7) | `veilmouth_c5_plan.py` `FAMILIES`, `SIZES` |
| A pavement along every road; a carriageway unbroken along its length; a pavement round every block; houses flush on the pavement round an inner yard with a way in | city plan §29.1 (owner) |
| A gap between two houses is nothing or at least 4 m (R5) | reference plan; the planner stops on it |
| Stoop rules; trees only on pavements of about 6 m, none in the slums or the works; wealth falls from the centre | city plan §28.1 (owner) |
| The centre's statue and market, the Town Hall with its clock tower, the Assize with the Wardens; the cathedral with its close, the cemetery and the old chapel; the faction's house; the port with quay, piers, warehouses, Customs House and lighthouse; the works | city plan §28.1 (owner) |
| The three Ritual Site Candidates by kind: a square in Lower Veil, a cellar yard in the works, the old chapel's court | city plan §28.1 item 10 |
| The seven buildings that open, by portal id | `VeilmouthCityTests` |
| Streets at least 3 m clear and alleys 2 m, for the Combat Grid's cells | kit standard §3 |
| The frame budget, 16.7 ms | `AGENTS.md` |

The brief's "main streets 3 m, alleys 2 m" is the kit standard's clear width for a fight, not a street section. C5's narrowest road is a lane of 10 m with its pavements, and the concepts keep C5's sections.

## 2.2 What was fixed and is now open (the owner's authorisation)

| Was | Now |
|---|---|
| Straight streets at right angles, the centre the only round place (city plan §28.1 item 2) | districts turned, diagonal roads between them |
| Blocks "rectangular for now", in one grid (§29.1 item 3) | blocks stay rectangles; their district's frame turns |
| The river round the west and the south, the wall on the north, one gate (§28.1 items 1, 14) | the river, the harbour and the wall follow the land |
| Gentle relief with one low rise (§28.1 item 13) | the land has its own form; the city's ground is level in each district |
| About 155 buildings (§29.1 item 7) | "reduce where appropriate": the concepts hold 106 to 150 |
| The snowy mountain backdrop (§26.2, 12a) | open ocean |

## 2.3 The turns a district may take — measured

The kit standard allows a frame at multiples of 45° only, and `KitLayoutCheck` enforces it. Whether another turn can be allowed is a question for the Combat Grid, whose 1 m cells stay world-aligned. `CombatGridAngleCheck` was given an angle list and run in batch for nine turns (`Logs/GridAngle/report_angles.txt`; the first report of 2026-11-05 is untouched).

**Measured**, a walled street 28 m long, widths 2, 3, 4, 6 and 12 m, on a grid node and off it:

| Turn | Cells a person stands on | One end reaches the other | Moves per metre | Cells beside a wall that read cover |
|---|---|---|---|---|
| 0° | 100 % | always | 0.95 | 100 % on the grid, 50 % off it |
| 15°, 75° | 100 % | always | 0.77–0.96 | 65–91 % |
| 22.5° | 100 % | always | 0.73–0.89 | 64–94 % |
| **26.565° and 63.435°** (a 1 in 2 line) | 100 % | always | 0.66–0.85 | **100 %** at 2, 3 and 12 m; 74–100 % at 4 m; 50–76 % at 6 m |
| 30°, 60° | 100 % | always | 0.64–0.82 | 68–88 % |
| 45° | 100 % | always from 3 m; a 2 m street on a node falls apart | 0.68 | 100 % at most widths; 0 % for a 6 m street on a node, 50 % for 3 and 12 m off it |

**Reading** (the agent's):

- Any turn is walkable and fightable: every cell stands and every street joins.
- Cover along a wall depends on where the wall's face falls on the cell lattice, at every turn, 0° and 45° included. No turn is perfect off its best offset, and none is worse than 0° off the grid.
- The 1 in 2 turns read as regularly as 45°, because a 1 in 2 line meets grid nodes every √5 m. With 0° and 45° they give **four block orientations**: 0°, 26.6°, 45°, 63.4°. The concepts use only these.
- Not measured yet: a corner and a T-junction at a turn, railings as Half Cover, a passage through a turned row, and `VeilmouthGridCheck` on built houses. These are measured on the chosen concept before its city is built.

To allow the new turns, `KitLayoutCheck`, kit standard §1, style guide §4 and one test change; that is part of Phase 4, not done now.

## 2.4 What the builder can do at a turn today

| Part | Today | For a turned district |
|---|---|---|
| Layout file | every street, block, building and wall is axis-aligned | a frame (place and turn) for each district; streets, blocks and buildings named in their frame |
| The planner's block solver | one-dimensional along each block side | unchanged inside a frame; overlaps checked between frames |
| Placing a house | by compass facing | through its frame, as the unused `Slant` code already does |
| Street paint, pavements' outline, kerbs | take any quad | joining a turned carriageway into another needs `Carry` generalised |
| Lamps, trees, railings, yards, parks | by axis | computed in the frame |
| Walls, quay, embankments, bridges | hard-wired to two river legs and one straight quay | rewritten for the chosen concept |
| Terraces, retaining walls, ramps, stairs between levels | none | new code, only concept C needs it |

## 2.5 What in the game is tied to a place

- **Moves by itself with the layout:** streets, houses, yards, lamps, the market and its people, the Notice Board, the seven portals, POI A's stage, the ritual circles, dwelling doors, walkers' routes (planned by sampling the built scene), far LODs, reflection probes.
- **Needs an edit when its place moves:** the player's start; the POIs' places in three data assets; 40 Hidden Cache points; nine task points; two dwelling ids written into code; hours posts; dogs; glades, beast territories and iron veins; ornaments; the reflection box; about 30 layout ids the builder asks for by name; six test files.
- **Names that texts use:** Draught Lane, Black Puddle, Deaf Boiler Yard, Lower Veil, the Reek, the Sleepy Quarter, Salt End, Parsons' End, the Penny, Nettle Street, Widows' Row, Fishlip Quay and others. The concepts keep every one of these names.
- **Texts that fix a shape:** "coal stairs" with landings by POI C, and Lower Veil lying "below the hill". Concept C makes these literal; A and B keep them as a short stair on Draught Lane.
- **Not rebuilt by the chain:** the player, the terrain, the water sheet, Activity Spots, hours posts, shop signs, POI C's traces. Each has its own step after a rebuild.
- **Old saves** hold a world position and become invalid.

## 2.6 Water and the world's edge

- **A river above the sea.** All three concepts have a river that falls from a source to the tide, as the owner asks. Today one flat sheet at 24 m is the only water: swimming reads one level, the Combat Grid reads water as ground below 24 m, and beasts stand on anything above 24.3 m. A falling river needs its own surface mesh and a rule for the three. It is the largest new piece of run-time work, and it is the same in every concept.
- **The edge.** Nothing bounds the world today but the cliffs and the terrain's east end. An island needs a bound in the sea. The agent's default: the swimmer is turned back about 10 m inside the terrain's edge.

---

# 3. How a concept is made

One generator, one data file for each concept, under `tools/slice/island/`:

| File | Holds |
|---|---|
| `islandgen.py` | the generator |
| `concept_a.py`, `concept_b.py`, `concept_c.py` | each concept as data: its coast, ridges, rivers, stamps, districts, roads and zones |
| `build.py`, `build_all.py` | `python tools/slice/island/build.py a` |
| `preview_blender.py` | the 3D preview, in Blender in the background |

**The land** is built in this order, each step authored and none a general noise:

1. **The coast**: a closed spline through control points, each with a shore type (sand, gravel, rock, cliff, marsh, quay) that sets the ramp from the waterline, the lift of the ground behind, and the sea bed's fall.
2. **The high ground**: ridges as lines with a height and a width at each point, joined so that they meet as one massif, with spurs on the high ground only.
3. **StampIT** forms laid at chosen places with a footprint, a turn and an amplitude, read at 16 bits from the vendor's files.
4. **Rivers**: each a line from its source to its mouth with a bed that only falls, a channel, banks, a floor and valley sides, cut by a smooth minimum.
5. **The shore**: the inland ground brought to the waterline over the shore type's ramp.
6. **Gullies**: water is gathered down the steepest way and cuts where it gathers, so slopes carry drainage lines.
7. **Built ground**: a level platform under each district with an apron, pads for the squares between frames, and roads benched into the slope.
8. **Water**: channels cut upright through built ground, so a quay meets the water as a wall; then the sea bed.

**The city** is a list of districts. A district is a rectangular frame with a turn, cut into rectangular blocks by C5's street sections; a block is filled with the kit's 10 and 15 m fronts round an inner yard with a passage of at least 4 m. The generator reports frames that overlap, built ground over water, and how many beast territories fit.

A concept builds the same every time: its randomness is seeded in its data file.

**Each concept's files** are in `Documentation/Terrain/Concepts/<A|B|C>/`:

| File | Deliverable |
|---|---|
| `map.svg` | the labelled plan at 1 unit = 1 m: coast, relief, river, estuary, beaches, footprint, districts, main and secondary streets, blocks with their turns, plots, parks, works, harbour, forest, beast territories |
| `map.png`, `city.png` | the same as pictures; the city at 3 px to the metre |
| `elevation.png` | heights alone, contours every 5 m |
| `heightmap_1025.png` | the preliminary height map: 1025 px, 16 bits, row 0 the north edge, 0 the terrain's floor and 65535 its 100 m |
| `preview_*.png` | two 3D views at true scale, no vertical exaggeration |
| `stats.txt` | the numbers quoted below |

---

# 4. The three concepts

## 4.1 A — the Horseshoe: a harbour city round a natural bay

**The land.** A horseshoe bay opens to the south between Gull Head, a long arm with the lighthouse at its end, and a shorter western point. The Spine, a ridge of 54 m, fills the north-west and ends in the cliffs of Cold Head; the North Downs close the north-east; Long Strand, an arc of sand about 300 m long, lies under the Spine's south-west side.

**The water.** The Veil rises on the Spine's flank, falls 28 m down a vale for 456 m, passes the city's west side and enters the bay beside the quay; the works stand over its mouth.

**The city.** Five districts, each turned to what it stands by:

| District | Turn | Stands | Holds |
|---|---|---|---|
| Lower Veil and Fishlip Quay | 153.4° | along the bay's north shore, 1.9 m | warehouses on the quay, tenements behind, the Black Puddle |
| The Penny | 0° | the first rise, 4.4 m | the market and statue, the Town Hall, the Assize, rich shops |
| Parsons' End | 26.6° | a knoll above, 11 m | the cathedral in its close, the cemetery and old chapel, the faction's house, the Brothers' House |
| Sleepy Quarter | 45° | the east slope, 7 m | terraces, villas, the Governor's House, a garden |
| The Reek | 0° | the river's far bank, 2 m | the works and Deaf Boiler Yard, a workers' row |

The wedges between the frames are public ground: Weir Green and Bawling Wedge between the Penny and the quay, Bishop's Park between the Penny, the Sleepy Quarter and the quay.

**Out of the city.** The Vale Road leaves by Creaking Gate and follows the river's east bank to Crooked Hamlet and Kettle Cove; the South Road runs from the works to Long Strand; a lane runs down Gull Head to the lighthouse.

**Numbers.** Land 495,000 m², 49 % of the terrain; coast 3.8 km; 150 buildings; public ground 42 % of the frames; 19 beast territories fit, of 17 needed.

## 4.2 B — the Valley: a river city between two ridges

**The land.** The island is long from the north-west to the south-east. The East Ridge, 57 m, stands over cliffs along the east coast; the lower West Ridge faces Kettle Bay. The valley between them runs the island's length.

**The water.** The Veil rises in the north-west and runs 743 m: a brook past Crooked Hamlet, a river through farmland, a weir at the head of the tide, a tidal reach through the city, and Veil Water, an estuary behind the sand spit that carries the lighthouse. The Beck falls from the East Ridge and joins inside the city, so water lies on three sides of the Penny.

**The city** stands astride the river, each district turned to its own reach:

| District | Turn | Stands | Holds |
|---|---|---|---|
| The Penny | 63.4° | the east bank above the weir, between the river and the Beck | market, statue, Town Hall, Assize, an embankment |
| Sleepy Quarter | 63.4° | the west bank, facing it | terraces, villas, the Governor's House, an embankment |
| Lower Veil and Fishlip Quay | 45° | the east bank of the tidal reach | the river quay, warehouses, the Black Puddle |
| The Reek | 45° | the west bank, across from the quay | the works on their own wharf |
| Parsons' End | 0° | the East Ridge's foot, 14 m | the cathedral over the port |

Three bridges: Last Farthing Bridge between the Penny and the Sleepy Quarter, Grubby Bridge to the works, and a bridge over the Beck. Bawling Wedge, the market place, lies at the meeting of the waters.

**Numbers.** Land 560,000 m², 56 %; coast 3.7 km; 106 buildings; public ground 34 %; 21 beast territories fit.

## 4.3 C — the Terraces: a city on three steps under a plateau

**The land.** A tilted block: cliffs of 30–40 m along the whole north, a plateau behind them, and the ground falling south in natural benches to a shore sheltered by the Hook, a rocky spit with the lighthouse. The east is a gentle slope of fields; the west is wood over cliffs, ending in a sand spit.

**The water.** The Veil leaves the plateau at 46 m and falls 722 m in a ravine west of the town, dropping from bench to bench, to the quay's end, where the works stand.

**The city** climbs three benches:

| Bench | Level | Districts |
|---|---|---|
| The quay | 1.9 m | Lower Veil and Fishlip Quay (153.4°), the Reek (0°) over the river's mouth |
| The middle | 10 m | the Penny (0°), the Sleepy Quarter (45°) |
| The upper | 19 m | Parsons' End (26.6°), Salt Rise with the villas and the Governor's House (0°) |

The scarps between the benches are the Hanging Gardens and Drowsy Steps, with ramps for carts and stairs for walkers. The game's texts already speak of coal stairs and of Lower Veil under the hill.

**Numbers.** Land 553,000 m², 55 %; coast 3.5 km; 140 buildings; public ground 37 %; 23 beast territories fit.

---

# 5. Comparison

| | A, the Horseshoe | B, the Valley | C, the Terraces |
|---|---|---|---|
| What gives it its face | the bay and the city round it | the river through the city | the city stacked on a slope |
| The river's part | beside the city, short | **the city's spine**, with a tributary and an estuary | a ravine beside the city, with falls |
| The harbour | a natural bay, a sea quay | a river quay and an estuary | a sea quay behind a rocky hook |
| Buildings | 150 | 106 | 140 |
| Block orientations | 4 | 3 | 4 |
| The city's ground | five levels, 2–11 m, each district level | 2–14 m, each district level | three benches 8–9 m apart |
| New builder work | frames; one straight quay at a turn; one bridge | frames; embankments on four banks at two turns; three bridges | frames; one quay; one bridge; **retaining walls, ramps and stairs, which no code builds today** |
| Risk to the kit's rules | low | medium: more waterside | high: scarps inside the city |
| Fits the game's texts | a short stair is added | a short stair is added | already says stairs and a hill |
| Wild land, beasts | 379,000 m²; 19 fit | 466,000 m²; 21 fit | 417,000 m²; 23 fit |

**What the pictures do not yet show**, in all three: the coast's cliffs still carry a regular scallop from the generator, the relief inland is plainer than the stamps can make it, and a platform cut into a slope leaves a hard bank (the cathedral in B). These are the generator's first pass, and the chosen concept is worked further before anything is built.

---

# 6. The agent's recommendation

**B, the Valley, enlarged toward A's count of buildings.**

- It is the only concept where the river does what the owner asked of it in answer 2: it rises in high ground, runs a valley, passes through the city, carries the works and the quay, and ends in a natural estuary. In A and C the river is beside the city.
- Its angles have a reason a walker can see: each district follows its own reach of the river.
- It has the most wild land and the longest walk, from the estuary to the source.
- Its cost is waterside masonry: embankments on four banks and three bridges. That work is of one kind, which the present builder already does for two banks; C's retaining walls and stairs are a kind it has never done.

**Its weakness is size**: 106 buildings against the owner's earlier "about 155". Before Phase 4 the agent would add a third block to the Penny and to the Sleepy Quarter and a second quay block below the Beck, for about 135–140, and show that plan first.

**If the bay matters more than the river, A** is the safer build and already holds 150 buildings.

**C is the most striking and the dearest.** It could be reached later from A or B by raising Parsons' End further, without the scarps through the middle of the town.

---

# 7. For the owner

1. **Which concept**: A, B or C, or parts of two.
2. **The city's size**: whether about 135–150 buildings is right, or the earlier 155 stands.
3. **The wall**: the concepts keep a wall only where no water or steep ground closes the city, with Creaking Gate on the road out. Whether the "one gate" rule stays is the owner's.
4. **The falling river**: it needs its own water surface and a change to how the Combat Grid, swimming and beasts read water (§2.6). The agent proposes to build it; the alternative is a river that is tidal for its whole length, which no concept shows.

Nothing is built until the owner chooses.

---

# 8. The owner's choice, 2026-11-14

**Concept B, the Valley, is approved**, to be worked out in full by the agent without stops, taking its recommended decisions. The measure of the work is a natural relief and textures of the best look.

| # | Question | Answer |
|---|---|---|
| 1 | The city's size | 135–150 buildings. More is not wanted at the picture's cost |
| 2 | The wall | Where the land closes the city (a ridge, water), nothing more is needed. Elsewhere a wall, with a gate and guards on each main road; more than one gate is allowed |
| 3 | The river | Whatever looks best for its price. If a river on its own falling surface, joined to the sea's sheet, is dear, the river is tidal for its whole length |

**The agent's decision on the river (under the owner's grant): tidal for its whole length.** A falling river needs a water mesh of its own, its joint to the sea's sheet, and a new reading of water in swimming, in the Combat Grid and for the beasts (§2.6): three run-time systems changed for one stream. A tidal creek costs nothing at run time, since the one sheet at Sea Level already serves it. It is also the truer picture: an island a kilometre long has no river of 700 m with a fall, but it can have a drowned valley, a creek that runs far inland on the tide. The high ground still feeds it: dry, stony brooks come down the ridges to its head.

# 9. Concept B worked out

Source: `tools/slice/island/concept_b.py`; plan and previews in `Concepts/B/`.

- **The Veil** is a tidal creek 742 m long: Veil Head, a mere under the hills; a winding upper reach 6–10 m wide past Crooked Hamlet and the fields; 12–16 m through the city; 26–46 m below the Beck's mouth; and Veil Water behind the Sand Tongue. **The Beck** is a tidal side creek 164 m long between the Penny and Lower Veil, with a stony gully above it up the East Ridge. Two more brooks, dry, come down to Veil Head.
- **The city holds 146 buildings** in five districts: the Penny and the Sleepy Quarter each gained a block along the river.
- **The bounds.** The Veil, the Beck and the East Ridge close the east bank's town on three sides; a wall closes its north, with Farthing Gate on the East Lane, and its south-east, with Salt Gate on Spit Lane. The west bank's town and the works have a wall on their land side, with Creaking Gate on the Vale Road, the West Gate on the track to Kettle Bay, and the South Gate on the South Road. Five gates in all.
- **The roads.** The Vale Road runs up the west bank to Crooked Hamlet and over to Kettle Bay; the East Lane runs up the east bank through the fields to a footbridge at Veil Head, so the valley is a round walk; Spit Lane goes to the lighthouse; the South Road to Long Strand; a track climbs the East Ridge from the cathedral.
- **Five bridges**: Last Farthing Bridge, Grubby Bridge, Beck Bridge, Lenten Bridge over the gully, and the footbridge at Veil Head.
- **The ground under each district** was already near its level: the quay 0.9 m against 1.9 m built, the Penny 3.3 against 4.6, the Sleepy Quarter 4.9 against 4.6, the works 2.0 against 2.0. Only Parsons' End is cut into the ridge's foot, 12 m against ground of 3 to 27 m, and needs a retaining bank on its east side.

# 10. The ground in the engine, 2026-11-14

The approved ground was laid on a terrain of its own in a lab scene (`VeilmouthIslandLab.BuildAndRender`, batch with graphics; `Assets/Scenes/Slice/Lab/IslandLab.unity`). Veilmouth.unity, its terrain and its city were not opened. Views: `Concepts/B/01_…` to `09_…`.

**What ran, on Unity 6000.6.8f1 and URP 17.6.0:**

- **MicroSplat compiled and draws.** The shader was generated by the URP 6.3 adapter, named by its keyword; it reports supported, with 8 passes and three warnings of duplicate keyword lines, no error. This settles the first risk of the research (§7 there).
- **The texture arrays packed**: 11 surfaces at 2048 px, colour with height in one array and normal with smoothness and occlusion in the other, DXT5. Each surface has its own colour, normal, roughness, occlusion and displacement map from Poly Haven.
- **The splat maps are the planner's**, read by MicroSplat as Unity's own alphamaps; the terrain's layers after MicroSplat's sync are the island's own eleven, in order.
- **The sea is one sheet** of the scene's own WaterWorks material, 8 km across, with a bed below it beyond the terrain.

**What the views showed, and what was changed for it:**

| Seen | Change |
|---|---|
| The turf's photograph is dry and brown | a tint over each surface toward the slice's palette: the turf to a damp olive green, earth and mud down from orange (`VeilmouthIslandLab.Tint`) |
| At a tile of 3–4.5 m the repeats read as a net from 40 m off; Core has nothing against it | tiles of 6–11 m, about three times the photographed patch, and arrays of 2048 px, which keeps 230–340 px to the metre |
| The wood's floor, with no trees on it yet, read as bare brown hills | half grass kept under the trees |
| The creek at 0.8–1.4 m of water showed the water shader's foam over its whole width | 1.8 m of water and more everywhere, which also lets the walker swim it |
| The city's warm haze turned the far sea brown | the lab's haze is taken toward the sky's horizon; `VeilmouthSky`'s haze needs the same |
| With no reflection the sea was black | the sky baked as the scene's reflection, as `VeilmouthReflections` does for the city |

**Still wrong, in the agent's own judgement:**

- **Repeats still show from the air** over the wood's floor. From the walker's eye they do not. A distance resample would end it; that is the Anti-Tiling module's work, or a module of the project's own (MicroSplat documents how one is written).
- **Steep faces stretch their texture**, since Core has no triplanar projection. It shows on the sea cliffs and the valley's head. The answer is rock meshes on the steepest faces, not a shader.
- **The water's waves alias** into horizontal lines near the shore in these captures. It is the scene's own material, unchanged; whether it shows in play is to be seen.
- **The city's platforms are bare** level rectangles with banks, until the city stands on them.
- **No tree, grass or rock stands yet.** The views are of the ground alone.

**Not measured yet:** the frame. The lab holds a terrain and a sea; `FrameProbe` is run when the city stands on the ground, against the table in the audit (§7 there).

**The scene's source data** is written by `ISLAND_N=1025 python tools/slice/island/build.py b --export` into `Assets/Data/Slice/Island/`: the heights at the terrain's own 1025 samples, and the weights of eleven surfaces at 1024, painted by rule from the finished relief (`islandexport.py`). Share of the land: grass 43 %, forest floor 25 %, leaf litter 8 %, mud 6 %, sand 5 %, rock 4 %, earth 3 %, gravel 2 %, rocky shore 2 %, river stones 1 %, mossy rock 1 %.

---

# 11. The polish of the ground, 2026-11-14

Before the city is rebuilt the owner asked for a stage of polish: the relief's naturalness, the textures' repeat, trial plots of living cover, the water's edge, and what each costs. Its result is [TERRAIN_POLISH_REPORT.md](TERRAIN_POLISH_REPORT.md), and it waits for the owner's review. The city is not rebuilt, and the production scene is untouched.

What it changed in concept B, all in the generator:

- The land is cut by the water it gathers (`tools/slice/island/islanderode.py`): no closed hollows, valleys with their heads and tributaries, combes on the ridges.
- The rocky shore is broken at two scales and has a wave-cut bench; sand, marsh and quays keep their line.
- The lone hills are warped out of their round caps; two stamps are halved; the creek's valley reaches the mere.
- The surfaces mix a rougher turf into the meadow and lay damp ground where water gathers; sand lies only on the strip the sea reaches.

§10's three faults stand as follows. The repeat over turf and woodland floor is gone, by the project's own MicroSplat module (`VeilmouthTerrainVariation`); over sand, bare earth and gravel it remains. Steep faces still stretch the texture. The water's stepped line is its foam band, and with the foam off the shore is clean.

The engine's views in `Concepts/B/` (§10) are of the ground before this stage; the plan, the elevation and the height map there are the planner's and show it after. The engine's views after it are in `Polish/Island/`.
