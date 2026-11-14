# Veilmouth Island — Terrain Polish and Anti-Tiling Research

Version: 1.1

Status: Draft — reviewed by the owner on 2026-11-14: §8 holds the answers. §6 is accepted as the base; §7 is the list the rebuild works from.

Last Updated: 2026-11-14 (§8: the owner's answers. Earlier: first version)

Depends On:
[ISLAND_CONCEPTS.md](ISLAND_CONCEPTS.md), [MICROSPLAT_STAMPIT_RESEARCH.md](MICROSPLAT_STAMPIT_RESEARCH.md), [CURRENT_WORLD_AUDIT.md](CURRENT_WORLD_AUDIT.md)

Referenced By:
[99 Open Questions](../../../99%20Open%20Questions.md) VS-ISLAND

---

# Purpose

The owner asked (2026-11-14) for a stage of visual and technical polish of concept B's ground **before** the city is rebuilt on it: how natural the relief is, how to break the textures' repeat without buying the Anti-Tiling module, trial plots of living cover, the water's edge, and what each costs. This document is the result.

**The city was not moved, and `Veilmouth.unity`, its terrain and its city were not opened.** Everything was built in lab scenes that the tools write again on each run.

Every number of a rule is an authoring default. Every measurement names what was measured and how.

---

# 0. Summary

| Question | Answer |
|---|---|
| Was the relief a set of placed stamps? | No: the stamps added 2 m RMS to a land whose own height spread is 11 m, and their edges fade to nothing. The real fault was the other one: smooth bumps with no drainage logic. 1.7 % of the dry land was closed hollows, and the lines where water gathers lay on unshaped slopes. |
| What was done to the relief | The generator now cuts the land by the water it gathers (`islanderode.py`), breaks the rocky shore at two scales, leaves a wave-cut bench under cliffs, and warps the lone hills. Closed hollows: 8 854 m² → 69 m². The ground along the drainage lines lies 1.7 m under its surroundings on 84 % of their length (before: 1.2 m on 72 %). |
| The texture's repeat | Four ways were built and compared on one patch, with two combinations. **Recommended: macro variation + stochastic tiling on the fine-grained sets + the layers mixed by rule + Core's hybrid blend** (way H). It removes the grid from turf, woodland floor, litter and mud at every distance. It costs about 1 ms at 2560 × 1440 with MSAA 4, no texture memory, and no change to normals or roughness. |
| What the recommended way does **not** solve | Sand, bare earth and gravel keep their repeat from the air: their photographs have patches of tone, and the stochastic read turns those into a honeycomb. The sand set needs replacing, which is a download and so the owner's decision. Steep faces still stretch the texture. |
| Is the Anti-Tiling module needed? | Not on this evidence. The project's own module, `VeilmouthTerrainVariation`, enters MicroSplat through Core's own extension points. |
| Living cover | The placing rules work: stands of one species, groves and glades, belts of shrubs at the woods' edges, stones in clusters. **The models and the way they are drawn do not:** 1 092 kit trees on 12.6 ha cost 8 to 11 ms of a 16.7 ms frame. Woods need instanced trees with far LODs before they can cover the island. |
| The water's edge | The waterline itself is smooth. The stepped line is WaterWorks' **foam band**, whose edge is a per-pixel threshold. With the foam off the shore is clean. A second fault, grain over the open water, is not from screen-space reflections and is not solved. |
| Is the completion criterion met? | **Partly.** The relief now reads as one drained landscape, and its main surfaces have no repeat. It is still bare of cover over most of its area, the beaches still show a grid from the air, and the city's platforms are bare rectangles. §7 lists what remains. |

---

# 1. The island's faults

## 1.1 How it was measured

`tools/slice/island/islandreport.py` reads the height field the generator makes and writes a report and maps. The table compares the island as approved (commit `a9392e9f`) with the island after this stage, both at 2 m a cell.

| Measure | As approved | After | Reading |
|---|---|---|---|
| Closed hollows deeper than 0.3 m | 8 854 m², 1.7 % of dry land, deepest 3.4 m | 69 m², 0.0 % | Water now leaves every part of the land. The old hollows lay behind the cliffs' raised rims. |
| Lines gathering over 0.2 ha: length | 2.61 km | 3.02 km | |
| Ground along them, against its 60 m mean | 1.2 m lower (median), lower at all on 72 % | 1.7 m lower, on 84 % | Water runs in valleys it has cut, not across even slopes. |
| Lines gathering over 1 ha | 1.1 m lower, on 72 % | 1.5 m lower, on 88 % | |
| Stamps: RMS of what they add | 1.97 m, of a land spread of 11.4 m | 1.42 m of 11.9 m | Stamps are a minor part of the relief. |
| Dry land the stamps move by over 3 m | 12 % | 8 % | Two stamps were halved (§1.3). |
| A stamp's last 6 % by its edge | 0.00 to 0.03 m | 0.00 to 0.07 m | No stamp shows a seam. |
| Dry land draining into the tidal creeks | 60 % | 56 % | Unchanged: the valley already gathered the island. |
| Dry land under 5 m | 30 % | 37 % | The valleys' floors are lower. |
| Hypsometric integral | 0.22 | 0.20 | A low, worn island, as intended. |
| Slopes over 45° | 4.1 % | 3.5 % | |
| Waterline's box-count dimension, 4 to 64 m | 1.04 | 1.05 | **No change by this measure**; see §1.4. |

Maps: `Polish/Geography/before/` and `Polish/Geography/polished/` (`hillshade.png`, `drainage.png`, `slope.png`, `stamps.png`, `report.txt`).

## 1.2 The faults found

1. **Smooth bumps.** The ridges were Gaussian sections joined by a p-norm, and the five lone hills were round caps. Between the 100 m form and the 20 m noise the ground had no form of its own.
2. **No drainage logic.** The gullies were a decoration over the finished slopes. Hollows stood closed behind the cliffs.
3. **A coast drawn by sines.** Two sine waves gave the north-east cliff a row of even teeth, and the rocky shore was smooth at the walker's scale.
4. **Two stamps too strong.** *Rough Cliffs 18* at 9 m stood as a row of teeth and *Layered Rocks 15* at 10 m as a quarry face (seen in the engine).
5. **The creek's head in a slot.** Seen in the engine after the hills were warped: the first 60 m of the valley were not cut.
6. **Sand over the whole south.** The surface rule laid sand on everything under 3 m within 110 m of a beach.

## 1.3 What was changed

All of it is in the generator, so the island builds the same every time (`python tools/slice/island/build.py b --export`, 50 s).

- **Erosion** (`tools/slice/island/islanderode.py`, new). Each of 14 passes fills the closed hollows (priority-flood), shares the water among every lower neighbour (multiple flow directions), and cuts each cell toward the one it drains to by the stream-power law, solved implicitly from the mouths upstream. A channel begins only where 230 m² gathers, so hillsides stay whole. The slopes then creep. The rock's resistance varies over the island. The sea and the tidal water are the base level, at the water's surface.
  - Three things were tried and rejected on the way, each seen in a hillshade: single-direction routing (D8) ruled straight slots along the grid's eight directions; no channel threshold gave a comb of rills on every slope; cutting to the creek's bed instead of its surface drowned its banks.
  - What the water cut is spread over 9 m, so a combe is a soft trough in turf and not a slot with rock walls.
  - The city's platforms stand before the water runs, as ground it cannot cut. Valleys find their way round the town and are not dammed by it afterwards.
- **The coast.** Where the shore is rock or cliff the waterline is moved by a noise of 6 to 48 m and a finer one of 3 to 13 m. Sand, marsh and quays keep their line. The sines are turned down to 45 %.
- **A wave-cut bench** of up to 7 m under rock and cliff.
- **The lone hills** are warped by 38 m, out of their round caps.
- **The two stamps** are halved to 4 and 5 m; the creek's valley is cut right up to the mere; sand lies only on the strip the sea reaches.
- **The surface rule** (`islandexport.py`) now mixes a rougher turf in clumps of 10 to 40 m over the meadow, and lays damp turf and mud where the land's water gathers.

## 1.4 What is still wrong with the relief

- **The city's platforms are bare rectangles with aprons**, and the quays' basins are rectangular cuts in the creek. This belongs to the city's rebuild.
- **The combes of the East Ridge run parallel**, seven in a row. Water on an even slope does that, but the row is regular. Varying the ridge's own profile would break it.
- **Cliffs stretch their texture.** Core has no triplanar read. The answer is rock meshes on the steepest faces, or a triplanar read in the project's module.
- **The coast's dimension did not move** by the box-count measure (1.04 → 1.05), though the rocky shore visibly has clefts and stacks now. Two thirds of the waterline are beach, creek and quay, which stay smooth by design, and the measure reads them all together. The measure should be taken on the rocky shore alone before it is quoted again.
- **The island is low.** 37 % of its dry land lies under 5 m. That suits the city, and it is what the concept drew, but the relief's drama is on two ridges only.

---

# 2. The texture's repeat

## 2.1 What MicroSplat Core gives, and how the project adds to it

Core alone has nothing against a repeat. Its shader calls the Anti-Tiling module's functions under that module's keywords, and the module is not installed. Core does have two things that help and were not in use:

- `_HYBRIDHEIGHTBLEND`: layers blend by their height maps near the eye and plainly by their weights from a set distance. A mix of sets is crisp underfoot and soft from the air.
- An extension mechanism. Core finds every `FeatureDescriptor` by reflection, lets it write its properties and functions, and calls its `OnPostGeneration` on the finished shader text (*source*: `MicroSplatShaderGUI_Compiler.cs:244-283, 566-571`).

`Assets/Scripts/Editor/VeilmouthTerrainVariation.cs` is the project's own module, written from the published techniques and not from the vendor's module. It survives a recompile from the material's inspector, because it is part of every compile. It changes two places Core has no call for: the two macros every array read goes through, and the line where the finished albedo is handed over. If Core changes and a place is not found, the compile fails with a message; it never passes silently.

## 2.2 The ways compared

One terrain of 256 m in four strips (turf, woodland floor, mud, stony ground), the island's own texture sets and tiles, drawn in turn by each way (`VeilmouthTilingLab.Run`).

| Way | What it does | Extra reads a pixel | Result |
|---|---|---|---|
| **A** base | One set to a surface, tiles of 6 to 11 m. | — | The grid is plain from 40 m and from the air. |
| **B** macro variation | The finished albedo is made lighter and darker by a value noise of 13, 47 and 173 m, and a little warmer and cooler by one of 97 m. | none (no texture read) | The grid stays, under a mottling. On its own it does not solve the repeat; it gives broad ground its tone. |
| **C** layers mixed | Each surface is two or three sets of its kind in clumps (a noise of 31 m and one of 9 m), with Core's hybrid blend. | more layers a pixel | Patches break the uniformity, but each patch keeps its own grid. |
| **D** stochastic tiling | Each array read is taken on a triangle grid of patches, each with its own offset and, for half, a half turn (Heitz and Neyret 2018; weights sharpened as Mikkelsen 2022). | 1 to 3 for 1; 1.56 on average (52 % of pixels read one patch, 40 % two, 8 % three) | **The grid is gone at every distance** on fine-grained sets. On sets with forms or patches of tone the patches show as a honeycomb. |
| **E** distance resampling | Where a tile is minified past mip 3.2, the same set at 3.7 times its tile takes over across two mips. | 1 near and far, 2 in between | The 8 m grid is gone, a 30 m one remains, and pebbles read as boulders from afar. |
| **F** B + C + E | | | Natural from the air on turf; the stony strip shows giant forms. |
| **G** B + C + D on every set | | | No grid anywhere, but a honeycomb on stones. |
| **H** B + C + D on the fine-grained sets | Stochastic on turf, woodland floor, litter and mud only. | | **Recommended.** |

Sheets, each with A to D along the top and E to H below: `Polish/Tiling/sheet_air.jpg` (from 170 m), `sheet_mid.jpg` (from 24 m), `sheet_eye_turf_wood.jpg`, `sheet_eye_mud_stone.jpg` (from the walker's eye).

## 2.3 Correctness

- **Normals.** A half turn of a texture turns its tangent-space normal by a half turn: x and y are negated. The module does that for every turned patch of the normal array. Rotations by other angles and mirror images were left out on purpose: each needs its own correction of the normal, and offsets with half turns were enough to remove the grid.
- **Roughness and occlusion** are read through the same patches as the normal, since they are packed in the same array, and are not otherwise changed.
- **Macro variation touches the albedo only**, by ±0.46 stops at the very most and under ±0.2 over most ground. Normals, smoothness and occlusion are not touched.
- **A surface that must keep its pattern**, setts and cobbles, is left out by the module's mask of layers. This matters when the city's surfaces join this material.

## 2.4 What failed

- **Sand, bare earth and gravel under the stochastic read.** Their photographs have patches of tone. From 60 m the patches read as a honeycomb, and on the beach at the walker's eye as dark blotches of 3 m (`Polish/Tiling/trial_sand_stochastic_rejected.jpg`). A softer blend between patches did not help. So these sets are read plainly, and their grid shows from the air over the southern flats.
- **Mixing unlike sets.** With the sets in the project, a second set "of the same kind" differs in tone from the first, so a mix reads as patches of two things. For turf that is right (rough turf in a meadow). For a true mix of like with like, a second turf and a second sand are needed.

---

# 3. Living cover

## 3.1 What was built

`VeilmouthNatureLab` lays cover on four plots of the lab's ground, 12.6 ha in all: a valley's wood and meadow, the East Ridge's combes, a bay with its beach and wood, and the marsh by the spit. It reads the planner's zones (`island_zones.bytes`: woodland, marsh, fields, where water gathers, bare ground, sand, rocky shore) and the terrain's own slope and height.

Nothing is strewn evenly:

- **Woods.** A candidate point every 3.2 m is taken with a chance that follows the woodland zone squared and a noise of 46 m, so a wood has groves and glades. A stand keeps to one model, picked by a noise of 60 m, with a stray among them one time in six. Damp ground takes birch, exposed tops take gnarled trees, the rest take beech and hornbeam, and a veteran stands here and there in a wood's heart. Crowns have unequal reach, so the spacing is not an orchard's.
- **Copses.** On open ground a parent point for about each 70 m square, three to eight trees of one species round it, shrubs at their feet.
- **Shrubs.** A belt at a wood's edge, along the damp lines across open ground, and a thin understorey in patches.
- **Stones.** On a rocky shore, on ground of 22 to 44°, and in stream beds; a large one with up to four small ones round it, each tumbled and sunk a little.
- **Grass**, as the terrain's own details (the Overworld's clump; no second grass system): a short sward nearly everywhere turf lies, thinner under trees, and tall grass in clumps of 14 m, along damp lines and at the woods' edges.

Placed: 1 092 trees in woods, 1 in a copse, 112 shrubs, 52 boulders with 99 small stones, 77 871 short and 15 737 tall grass clumps. Views: `Polish/Nature/*.jpg`; counts in `build.txt`.

## 3.2 What the trial shows

**The rules give a believable distribution.** Seen from the air the woods have dense hearts, thin edges and openings, and stands of one kind.

**The cover does not yet look right, and the reasons are not the rules:**

1. **The trees are street and garden trees.** The project's own trees were grown for the city's roads and gardens: slender, clean-stemmed, about 10 m. A wood made of them reads as a plantation. A wood needs broader crowns, low branches at its edge, and three or four more forms.
2. **The grass does not match its ground.** With the Overworld's colour the meadow was a vivid green carpet on olive turf. With the island's own two colours it blends, but it is now too pale and yellow. Its colour should be taken from the turf's own albedo, with the macro variation's tone.
3. **Tall grass ends at a hard edge** from the air, where the details' draw distance ends (80 m).
4. **No cover on most of the island.** The plots are about a quarter of the wild land's area.

---

# 4. The water's edge

`VeilmouthShoreLab` renders six shores with the water's material as it is and with three changes, each a copy of the material in memory. WaterWorks was not changed. Sheets: `Polish/Shore/sheet_*.jpg`; each has the material as it is top left, foam off top right, shore waves short and slow bottom left, foam and screen reflections off bottom right.

| Checked | Finding |
|---|---|
| The shoreline's geometry | **Smooth.** Seen straight down from 45 m with the foam off, the line between sand and water is a clean curve (`sheet_beach_straight_down.jpg`). The terrain's 1 m grid does not step it. |
| The stepped line | It is the **foam band**. WaterWorks' `Wave_Distance` subgraph draws the shore waves as bands of the distance through the water, read from the depth buffer, and thresholds a noise by `Step`. The band's edge is a per-pixel threshold: jagged at any distance, and MSAA cannot smooth it, since it is inside a shader. |
| Foam off (`_UseFoam` 0) | The shore is clean, and the shallow water keeps its colour. |
| Shore waves short and slow (`_WaveDist` 1.5, `_WaveFrequency` 1.2) | One narrow band instead of three. Its edge is as jagged as before. |
| The waves' behaviour | The bands follow lines of equal water depth along the view ray, so they shift with the camera's angle. Under cliffs they stand as two or three parallel white lines (`Polish/Island/04_east_cliffs.jpg`). |
| The ocean's reflections | The baked sky is mirrored. From low angles the open water carries a **grain of bright and dark pixels**. Turning `_ScreenSpaceReflections` off did not remove it, so it is not the reflection's ray march. It is most likely the wave normals' own detail aliasing where a pixel covers many ripples. **Not solved.** |
| From other angles | From above 100 m the grain reads as a dithered grey. At the walker's eye, looking along the shore, the water is good. |

**Recommended, without changing WaterWorks:** a material of the slice's own for the island's sea, a copy of `OverworldWater.mat` with the foam off. The Overworld's material stays as it is.

**If shore foam is wanted,** the band's `Step` has to become a smooth step over a pixel's width. That is a change to a copy of one subgraph, and so a change the owner would have to allow.

**Still to do:** the grain. It needs a trial in play mode, where the water moves, with the normals' strength and tiling turned down at a distance.

---

# 5. Performance

Machine: NVIDIA GeForce RTX 5070 Ti Laptop GPU, Direct3D 12, Unity 6000.6.8f1. Method, as the project's `VeilmouthFrameProbe`: one camera rendered by hand, then one pixel read back so that the GPU has finished; the median of 3 rounds of 12 frames after 3 warm-ups. It is the cost of one camera's render, CPU and GPU together, not a frame of the game.

## 5.1 The ways against the repeat

At 2560 × 1440, MSAA 4, no post-processing, the terrain filling the view. Two runs; the difference from A in ms.

| Way | From the air | From 24 m | Eye, turf and wood | Eye, mud and stone |
|---|---|---|---|---|
| A base (ms) | 3.11 / 2.99 | 3.80 / 3.60 | 4.00 / 4.01 | 3.96 / 3.90 |
| B macro | +0.13 / +0.15 | +0.18 / +0.47 | +0.12 / +0.03 | +0.04 / +0.17 |
| C layers | +0.45 / +0.51 | +0.46 / +0.88 | +0.31 / +0.21 | +0.51 / +1.08 |
| D stochastic | +0.20 / +0.33 | 0.00 / +0.29 | −0.07 / −0.03 | 0.00 / +0.27 |
| E resample | −0.09 / +0.17 | −0.14 / +0.21 | +0.01 / −0.24 | +0.02 / −0.07 |
| F macro + layers + resample | +0.69 / +0.49 | +0.88 / +0.70 | +0.29 / +0.32 | +0.39 / +0.32 |
| G macro + layers + stochastic, every set | +1.02 / +1.04 | +1.23 / +1.36 | +0.92 / +0.85 | +1.07 / +1.19 |
| **H** macro + layers + stochastic, fine sets | +0.90 / +1.02 | +1.07 / +1.13 | +0.93 / +0.88 | +0.58 / +0.56 |

The two runs differ by up to 0.6 ms for one way and view, so a difference under that is noise: resampling and macro variation on their own cost nothing that this method can tell.

H was timed, and its sheets were taken, with bare earth and sand still in the stochastic read. The final choice leaves those two out (§2.4), so H's cost is a little under these figures and its mud and stony strips keep a little more of their grid than the sheets show.

- **Passes:** 8 for every way. None adds a pass.
- **Texture reads.** Core reads up to 4 layers a pixel from 2 arrays, so up to 8 array reads, with 3 control maps for 12 layers. The stochastic read makes each of its reads 1.56 on average, 3 at most. Macro variation reads no texture.
- **Draw calls: the same for every way** by construction, since none changes the terrain's geometry or passes. They could not be read: the editor's frame counters give nothing for a camera rendered by hand in batch mode (`UnityStats` and the profiler's recorders both gave 0).
- **Memory.** The two arrays of 11 sets at 2048 px are 117.4 MB each, 235 MB in all (DXT5 with mips). No way adds a texture. Each further set adds 21 MB; arrays of 1024 px would be a quarter.

## 5.2 Living cover

At 1920 × 1080, MSAA 4, no post-processing, ms.

| View | All the cover | LOD bias halved | No cover shadows | Without grass | Bare ground |
|---|---|---|---|---|---|
| valley wood, air | 6.14 | 6.08 | 5.55 | 5.52 | 4.61 |
| valley wood, eye | 9.44 | 9.51 | 9.10 | 5.28 | 4.42 |
| east combes, air | 15.56 | 15.68 | 12.80 | 16.15 | 4.56 |
| east combes, eye | 16.72 | 16.72 | 11.23 | 15.51 | 4.91 |
| west bay, air | 14.80 | 15.01 | 15.28 | 14.42 | 5.20 |
| west bay, eye | 6.43 | 6.66 | 6.22 | 5.48 | 4.46 |
| marsh and spit, air | 14.51 | 14.54 | 14.68 | 14.88 | 5.80 |
| marsh and spit, eye | 4.57 | 4.63 | 4.54 | 4.49 | 4.62 |
| the island, air | 13.13 | 13.44 | 13.41 | 13.05 | 4.78 |

(`Polish/Nature/frame.txt`. A run before it gave the same picture within 2 ms.)

- **Trees are the cost: 8 to 11 ms where a few hundred are in view.** That alone is over half the 16.7 ms frame, before a city.
- **It is the number of objects, not their triangles.** Halving the LOD bias changes nothing. The 1 092 trees are 17 448 game objects and 8 543 mesh renderers.
- **Shadows are up to a third of it** in one plot (east combes: 3 to 5.5 ms) and nothing measurable in the others.
- **Grass** costs about 4 ms where a meadow fills the view at the walker's eye, and under 1 ms elsewhere.

---

# 6. Recommended implementation

1. **The relief:** the generator as it now stands (concept B with erosion, the broken rocky shore, the bench, the warped hills).
2. **The terrain's material:** MicroSplat Core with the URP 6.3 adapter and the project's own module, way H:
   - `_VEILMACRO` on every surface;
   - `_VEILSTOCHASTIC` on turf, woodland floor, litter and mud; never on stones, rock, sand, bare earth, or the city's setts and cobbles;
   - `_HYBRIDHEIGHTBLEND` from 140 m;
   - the layers mixed by the surface rule.
3. **No Anti-Tiling module.** Its two main techniques, distance resampling and noise, were built and measured here; resampling was the weaker.
4. **The sea:** a material of the slice's own with the foam off.
5. **Cover:** the placing rules of `VeilmouthNatureLab`, but drawn as instanced trees with far LODs, not as kit wrappers. This is a task of its own (§7).

---

# 7. What must change before the city is moved

**Needed first, since the city's own work depends on it:**

1. **The owner's review of this result**, and a choice on §8.
2. **The city's surfaces in the island's material.** Setts, flags and cobbles join the arrays and are left out of the stochastic read by the mask. Their smoothness at night has to be compared with the streets as they are.
3. **The platforms' edges.** Where a platform stands over a slope it needs a retaining wall or a bank, by the city's design, and the quays' basins need their walls.
4. **`VeilmouthSky`'s haze** cooled over open sea, as the lab's is.

**Can follow the city, but must be done before the island is called finished:**

5. **Instanced woodland:** forest-grade tree models with far LODs, drawn instanced (terrain trees or a merged far LOD as the houses have), with a shadow distance of their own. Until then the island cannot carry its 19.6 ha of woodland within the frame.
6. **A sand set without patches of tone**, and a second turf, if the owner allows the download (§8).
7. **Rock on the steepest faces:** meshes, or a triplanar read in the project's module.
8. **The grass's colour** from the turf's own albedo, and its far edge faded.
9. **The water's grain** (§4).
10. **A frame probe of the whole**, when city and cover stand together, against the audit's baseline.

---

# 8. For the owner to decide

| # | Question | The agent leaned to | The owner, 2026-11-14 |
|---|---|---|---|
| 1 | Is the relief natural enough to build the city on? | Yes, with §1.4 known. | **Yes.** The island no longer reads as a set of made hills. Keep the logic of its drainage. The East Ridge's seven parallel combes do not block the work, but are to become more natural later. |
| 2 | Way H for the terrain's material? | Yes. | **Accepted as the base.** Sand is to look natural with no visible grid. Gravel and bare earth keep their own texture. The city's paving is never read stochastically. Check every material by day and by night. |
| 3 | The sea's foam: off, or a smoothed band in a copy of WaterWorks' subgraph? | Off for now. | **Off, in a material of the slice's own.** The water is not held solved: its grain is to be checked in motion in the game's scene. |
| 4 | May the agent download two or three more CC0 sets from Poly Haven: a sand without patches of tone, a second turf? About 21 MB each, under the same manifest and Git LFS. | Yes. | **Allowed.** |
| 5 | Woodland trees: the owner's own models again, or a pack already in the project? | The owner's, as for the city's trees. | **A task of its own:** forest models of five kinds (tall old trees, broadleaves, conifers if the climate suits, young growth, dead trees). Instancing, far LODs and a natural density come first, not many unique models. A wood must not read as a park or an avenue. |

The owner also authorised the city's rebuild, by five principles ([91 Agent Decision Record](../../../91%20Agent%20Decision%20Record.md), *Island redesign*).

---

# 9. How to run it again

Unity must be closed. Each lab needs graphics (no `-nographics`).

| What | Command |
|---|---|
| The relief's report and maps | `python tools/slice/island/islandreport.py b polished` |
| The island's source data | `ISLAND_N=1025 python tools/slice/island/build.py b --export` |
| The island in the engine | `-executeMethod VeilmouthIslandLab.BuildAndRender` |
| The repeat's comparison | `-executeMethod VeilmouthTilingLab.Run` → `Logs/TilingLab/` |
| The living cover | `-executeMethod VeilmouthNatureLab.BuildAndRender` → `Logs/NatureLab/` (after the island) |
| The water's edge | `-executeMethod VeilmouthShoreLab.Run` → `Logs/ShoreLab/` (after the island) |

The lab scenes, their terrains, arrays and shaders are written again by these and stay out of git. The planner's files, the tools, the module and this report are in git.

---

# 10. After the owner's answers, 2026-11-14

Done at once, since each was a decision of §8:

- **Sand.** `coast_sand_03` is out of the slice. `sand_03` (Poly Haven, CC0, 17 MB) takes its place: an even, fine beach sand with no patches of tone, lightened by its tint to a dry beach. It takes the stochastic read. On Kettle Bay's beach at the walker's eye and over the southern flats from the air it shows no grid (`Polish/Island/05_kettle_bay_eye.jpg`, `01_island_from_the_south_east.jpg`).
- **A second turf.** `leafy_grass` (Poly Haven, CC0, 25 MB), a lusher, leafier sward of the meadow's own tone, lies in clumps of 12 to 43 m and on ground that stays damp. It takes the stochastic read. The terrain now has 12 sets; the two arrays are 256 MB.
- **The sea** wears `Assets/Settings/Water/VeilmouthSea.mat`, the project's water material with the foam off. WaterWorks and the Overworld's material are as they were.

**A correction to §2.4.** The dark blotches of the rejected trial on the spit were not only the old sand's own patches. The spit is where sand meets the marsh's mud, and MicroSplat blends two layers by their height maps: under the stochastic read each patch of sand has another height, so the blend flips from patch to patch. With the new sand the same border still shows dark patches of mud in the sand, softer than before. A border between two stochastic layers needs a wider, plainer blend; that is still to do. The mud itself was near black on the flats and is lightened by its tint.
