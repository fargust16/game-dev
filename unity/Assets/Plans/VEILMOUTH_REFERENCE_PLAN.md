# Veilmouth Reference Plan

Version: 0.22

Status: Draft — the agent's plan from the owner's walk of C3 and reference images (2026-11-08). The owner chose D3 (a) and D5 the same day: R1's steps 0–1 are built, every door taking about half a second, R2's steps 1 and 6 are set, the editor and the game at the quality level High, and R2's steps 2–3, the smooth LOD the owner asked for next, are built. For R3 the owner chose D2 (b), Linear, D7 (a), the present trees retinted, and the painted skies of the project's packs; R3 is built, and its views wait for the owner's judgement. The owner's authorization of 2026-11-08 to finish the plan approved the agent's recommendations as they stand, so D1 and D4 are (a); D6 had no recommendation and stays the owner's. R2's steps 4, 5 and 7 are built, and R4's pavements and R5's passages; the wider roads wait for a C4 layout. R6's porches are doorcases of pilasters until the wanted porch pieces come. R1's steps 3 and 4 are built and measured. R7 and R8 are done as far as the project's packs go, so the plan's steps are all taken; what remains waits on wanted pieces, on D6, on a C4 for the wider roads, and on S5c for more people. R1's step 5, the shader warm-up the owner asked for after step 4, is built and measured

Created: 2026-11-08

Last Updated: 2026-11-11 (R3 step 5: the height fog's pass measured in a build, 0.07 ms of GPU at 08:00 and 0.28 ms at 22:00 with the lamps' halos. Earlier: R3 step 2 complete: stains run down the wall under every window's sill, read from a sill map baked from the windows' panes; R8: the clock face stands as a placeholder. Earlier: R3 step 2: soot gathered under the cornice, darkest at each house's wall top and fading 3 m down it, read from an eaves map baked from the houses' body colliders; only the sill stains remain unbuilt. Earlier: R3 step 2's fix: the facade shader measured its plinth dirt and wall-foot moss from world height 0, and Veilmouth lies at 26 m, so neither ever showed; `VicGround` gives it the terrain, so both rise from the ground, on walls and kerbs only; the frame unchanged. Earlier: R3 step 2: soot streaks washed down every wall in the facade shader, a film of soot that greys the face, as strong as the material's dirt; the frame unchanged, 4.23 ms of GPU pooled. Earlier: R8: the works as mills with six placeholder stacks; the cathedral's placeholder in a Gothic dress, a pinnacled tower and a spire to 70 m over the roofs, city plan §35. Earlier: R3 step 9: night windows, each room its own lamp, warmer or whiter, brighter round the lamp, amber rather than cream, going dark one by one from 22:00 to 01:00. Earlier: R3 step 3: the carriageways in coursed granite setts, Modular Victorian City's paving made the city's own, in place of Rough01's cracked flagstones; smoothness 0.50 on the stone tops. Earlier: R3 step 7: the day grade fuller, saturation −15 and contrast 10, and the city green a little greyer so the palette sheet stays under 40 %. Earlier: 2026-11-10: R3 step 6's light shafts built in the height fog's pass: the low sun's and the moon's light through the gaps between roofs and trees; a sixth check view, Long Tongue westward into the evening sun. Earlier: R3 step 5's height fog built, on the owner's grant of autonomous work: a fog lying low over the water and the low streets, lit toward the sun or the moon, with the gas lamps' halos in it by night; the check's five views moved to C5's streets. Earlier: R2 step 8 measured on C5: the kit's shaders DOTS-ready, the GPU Resident Drawer left off by measurement, city plan §29.7. Earlier: R1 step 5, at the owner's request: the shaders' pipelines warmed from a traced collection before the first scene, 189 states in 1.2–1.6 s on a first run, taking about 0.3 s off the city's first frame with no state missing; step 4's one 1.1 s door was not the shaders, since first runs with a cleared driver cache took no door over 0.6 s. Earlier: R7 and R8 as far as the packs go: goods before 32 shop doors, iron rails on the Ridge villas' balconies, the other pieces on the wanted list, more people left to S5c, the skyline wanted and D6 open. Earlier: R1 steps 3 and 4: the rooms behind the windows are plain renderers, so the city's first load fell from 11.8 s to 7.9 s in the editor with the same picture; a development build loads it in 2.0 s (3.9 s on its first run) and takes 0.4–0.5 s a door, once 1.1 s. Earlier: R6 built: the kit's balcony slab, 2 × 0.86 m, cannot roof a porch whose columns stand 1.32 m out, so the columns and bare beam are gone from the city's houses, a raised door keeps a doorcase of two pilasters, and a porch roof, a door hood and an iron-and-glass canopy go on the wanted list with the shop front, the spire and the clock. Earlier: R4 and R5 built: raised pavements in 16 closed outlines, kerbs and rounded corners, carriageway ends carried across, a wet band along the gutters; passages of 4 m, a railing at each back, no gap under 4 m, 93 houses from 103; the wider roads not built, since every block would lose 4–8 m of depth, so they wait for a C4. Earlier: R2 steps 4, 5 and 7 built: every pane told its room, so the far look maps the built room's finish, depth and walls; the painted sky as the city's reflection and 70 street probes baked at noon and dimmed by the clock; shadows of 4096 px over 150 m. §5: D1 and D4 (a) by the owner's authorization to finish the plan. Earlier: R3 built: Linear, the palette to its targets with a palette sheet, the street surfaces' tints, the painted sky, haze, a lower golden sun, SSAO, the HDR grade, olive trees; what the check changed, and the parts not built, under each step; the gutters' wetness moves to R4. Earlier: §5: the owner chose D2 (b) Linear and D7 (a) the present trees retinted, and R3 step 4's painted skies from the packs already in the project; only Free HDR Sky holds sky images there, so the day and dusk skies are its two. Earlier: R2 steps 2–3, at the owner's request: only the houses' small details cross-fade out, below 4 % of the screen; the rooms behind the windows switch by distance, on within 48 m and off beyond 53 m, behind glass already opaque; the pictures that check it found attic rooms standing out through the mansards, now built under the flat only. Earlier: R2 steps 1 and 6: the owner chose D5; the editor, the tests and every desktop build use High, with anisotropic filtering forced on and MSAA 4×, and the kit's textures filter trilinear at 16×. Earlier: R1: the owner chose D3 (a), doors in 1–3 s; the city is kept behind interiors, measured at 0.45–0.62 s a door, so step 2, the preload, is not built. Earlier: first draft: the owner's six remarks traced to their causes in the project, the reference read, and eight workstreams with checks)

Depends On:
[VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §24, [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md), [VICTORIAN_STREETS_PLAN.md](VICTORIAN_STREETS_PLAN.md), [SLICE_ASSET_MAP.md](SLICE_ASSET_MAP.md), [16 Architecture Pivot](../../../16%20Architecture%20Pivot.md), [17 Game Systems Architecture](../../../17%20Game%20Systems%20Architecture.md) (I.1–I.2), [99 Open Questions](../../../99%20Open%20Questions.md) (VS-C3, VS-COLOR)

Referenced By:
[VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §24.7, [WORLD_STYLE_GUIDE.md](WORLD_STYLE_GUIDE.md), [99 Open Questions](../../../99%20Open%20Questions.md) (VS-REF)

---

# 0. What the owner asked

After walking C3 (2026-11-08) the owner sent nine screenshots of the build, eleven reference images of the look wanted, and a city map whose proportions the slice should follow. The six remarks:

1. Remove the artificial partitions between buildings that close the alleys.
2. A scene loads in about a minute, so every door between the city and an interior takes a minute, on a machine with an RTX 5070 Ti (12 GB) and 32 GB of RAM. Find the cause with logs and a test of the city's load and a journey into an interior and back. Optimise to 2–3 s if the cause is clear; otherwise consider seamless interiors, with wider rooms and corridors.
3. The porch of columns and a beam before a villa looks out of place. It needs a hood or something else, from references of such porches.
4. Textures change abruptly with distance, half a house can differ at some angles, and the rooms behind the windows appear suddenly as the player runs up, showing that the house was empty. The transitions must be smooth and unnoticeable, with a worked plan.
5. The pavement looks like a texture in the air with no collider, breaks at corners, and crosses the street. A pavement is a continuous raised island of stone along the houses, broken only by roads, never crossing them.
6. The building textures are too contrasty and doll-like. Every colour should sit in one muted range, the sky should not glare, and the whole palette of every texture and material needs reworking, with clouds and a haze over the roofs or other ways to unify colour.

The owner wants one common visual style. The references are stills of a Victorian occult game in the mood the slice names ([11](../../../11%20Vertical%20Slice.md) *Era and Mood*): an autumn town in low warm sun and haze, an evening street in smoke and gaslight, a rainy foggy avenue, a courtyard, the roofs, and an avenue toward a Gothic cathedral.

**The map** is used for its proportions and structure only: wide avenues, large blocks with courtyards, a round central place with radial streets, and a city held by its river. Veilmouth keeps its own plan; the map's streets are not traced, because it is another game's level.

---

# 1. Causes found

| Remark | Cause in the project (2026-11-08) | Where |
|---|---|---|
| 1 Partitions | A terrace span that does not fill with houses is closed by a **yard wall** 0.6 m thick and 4 m high on the front line: at a span's ends when it meets a building (1 m), and in its middle for the remainder. The owner's screenshot shows one such 1 m pier. | `VeilmouthCity.Terrace`, `YardWall` |
| 2 Loading | Every door is a **single-scene swap**: entering unloads the whole city, and coming out loads it again from nothing. The city's 149 775 objects were loaded at Unity's default background priority, about 4 ms a frame over some 1 400 frames: 52 s (measured, §3 R1). | `SceneTransition.Travel` (`LoadSceneMode.Single`) |
| 3 Porch | The kit's porch is two pilasters, two columns and a beam at 3 m (`VicDress.Porch`), meant to carry a balcony or a hood above. Nothing stands on the beam, so it reads as a bare frame. The kit has no porch roof, hood or canopy. | `VicAssembly.cs` `Porch` |
| 4 Pops | (a) **The editor plays at the quality level Very Low** (`m_CurrentQuality: 0`), while a desktop build starts at Ultra. Very Low has `lodBias` 0.3, so every LOD switches at 0.3 of its distance; `globalTextureMipmapLimit` 1, half-size textures; anisotropic filtering off; no anti-aliasing. (b) The houses' LOD group drops the window surrounds, cornices' small parts, pipes and rails at 15 % of the screen with **no cross-fade** (`fadeMode` None), so they vanish at once; two neighbouring houses switch at different moments, which shows as half a row with surrounds and half without. (c) The rooms behind the windows are culled by **screen size** (scaled by `lodBias`), while the glass turns to the mapped room by **distance** (32–46 m): at `lodBias` 0.3 the rooms vanish at about 14 m with the glass still clear, so the house is empty from 14 m to 32 m. (d) Textures use bilinear filtering, so mip levels meet in hard lines across a wall. | `QualitySettings.asset`, `VeilmouthKitHouses.Dress`, `VicRooms.Finish`, `VicWindow.shader`, `VicTextures.Configure` |
| 5 Pavement | The pavement is a **terrain layer painted at the road's level**: no step, no collider of its own. The kerb is a separate 0.3 × 0.15 m stone ribbon on the edge, cut wherever another street's rectangle crosses, so it ends at every corner. Where two streets' rectangles overlap, the later paint wins, so a pavement's paint can cross a carriageway. | `VeilmouthCity.PaintStreets`, `Kerbs`, `Kerb` |
| 6 Palette | The kit's albedos are saturated and bright (red brick, cream stucco, white stone), the sky is a saturated procedural blue, and nothing ties them: the project renders in **Gamma**, colour grading is LDR with no grade by day, the renderer has **no ambient occlusion** (no renderer features), MSAA is off, and the sun's shadows of 2048 px stretch over 350 m. The trees are a low-poly stylised pack in saturated green. | `ProjectSettings.asset` (`m_ActiveColorSpace: 0`), `URP_Pipeline.asset`, `URP_Renderer.asset`, `VicMaterials` |

---

# 2. The reference, read

What makes the reference look the way it does, and where Veilmouth stands:

| Trait of the reference | Veilmouth now |
|---|---|
| **One warm, muted range.** Ochre, umber, sienna, grey-green, soot; no colour above about 40 % saturation; values compressed, nothing pure white or black. | Saturated red brick, bright cream, deep blue sky, bright green trees. |
| **Air.** Strong aerial perspective: the far town melts into a warm or grey haze; light shafts in the haze; the sky is bright and low in contrast, overcast or sun-washed, never a saturated blue. | Clear air to the horizon, a saturated sky. |
| **Low sun and soft, grounded shadow.** Golden-hour light, long shadows, ambient occlusion in every corner and under every sill. | High sun, no occlusion, hard and blurred shadows by turns. |
| **Worn surfaces.** Wet sheen on cobbles, grime and soot streaks, moss at the plinth, leaves in the gutters, ivy on walls. | Clean tiling textures. |
| **Streets as rooms.** Wide carriageways of large setts, raised pavements of slabs with kerbs, trees in the pavement, bollards, lamp posts at the kerb. | Painted pavements at road level, kerb ribbons, narrow streets. |
| **Busy fronts.** Shop fronts of wood and glass on every ground floor, awnings, hanging signs, window boxes, balconies, posters, crates, carts, flower stalls, bunting and strings of lights on squares. | Bare fronts, few props. |
| **Roofs and skyline.** Mansards with dormers, terracotta and slate, chimneys; spires and a cathedral in the haze as landmarks. | Slate only, no spire. |
| **People.** Crowds in period dress. | Dummies and a few townsfolk. |

The first three rows decide most of the look and are cheap: colour space, palette, grade, sky, haze and occlusion (R3). The next two are the streets and the dressing (R4, R7). Detail work comes last.

---

# 3. Workstreams

Each workstream lists its steps and the check that closes it. Numbers are authoring defaults unless marked measured.

## R0 Measure (done with this plan)

- `SceneTransition` logs every journey: darkening, load to Awake and OnEnable, first frame.
- `SceneLoadTimingTests` (PlayMode, run by name, category Diagnostic): Veilmouth cold, into the Soot & Bell and back, each step timed, both scenes counted (game objects, components, rooms), and the longest frames broken down by the profiler's markers. Its report goes to `Logs/SceneLoad/`.

## R1 Doors in seconds (remark 2)

**Measured** (`SceneLoadTimingTests`, editor in batch mode with Direct3D 12, 30 frames a second, 2026-11-08; reports in `Logs/SceneLoad/`):

| Step | Before | Step 0: the load behind the veil at High priority | Step 1: the city kept behind interiors |
|---|---|---|---|
| Veilmouth, cold | 54.1 s (file read 3.5 s, the rest bringing objects to life) | 12.3 s | 12.0 s (once a session) |
| Veilmouth → Soot & Bell | 1.24 s | 1.01 s | 0.62 s, the second time 0.45 s |
| Soot & Bell → Veilmouth | 51.7 s | 10.5 s | 0.52 s, the second time 0.54 s |
| Veilmouth → a dwelling → Veilmouth | — | — | 0.53 s in, 0.59 s out |

Each door's time includes the darkening, about 0.3 s. In step 1 the city's roots switch off in 0.05–0.08 s and on again in 0.18–0.22 s (measured 2026-11-08, `scene_load_20261108-210622.txt`).

- **The interiors are not the problem:** the Soot & Bell holds 1 431 game objects and loads in under a second. Every minute was the city loading again on the way out.
- **Veilmouth holds 149 775 game objects, 91 701 of them (61 %) in the rooms behind the windows.** It has 87 274 mesh renderers, and tens of thousands of components switched off by the rooms: 28 068 kit blocks, 22 316 box colliders, 2 786 mesh colliders, 1 729 lights and 2 183 loose props, with 2 259 bodies.
- **The minute came from Unity's default loading priority.** The profiler showed no long frame: the load was spread over about 1 400 short frames, because the default `Application.backgroundLoadingPriority` (BelowNormal) gives a loading scene about 4 ms of each frame. Behind the black veil the frame rate does not matter, so `SceneTransition` now raises the priority to High for the journey (about 50 ms a frame) and restores it on arrival. This cut the way out from 52 s to 10.5 s.
- What remains, about 10 s, is the city's real cost of reading and bringing 150 000 objects to life.

**Target:** a door takes at most 2–3 s from the key to the lifted veil in the editor, and under 1 s in a build.

Steps, in order; each is measured by R0's test before the next:

0. **Load behind the veil at High priority.** Done and measured, above.
1. **Keep the city loaded.** Chosen by the owner (D3 (a), 2026-11-08), built and measured, above. Interiors stay separate scenes, as [16](../../../16%20Architecture%20Pivot.md) and the accepted Confront entry require, but load **additively**: entering switches the city's roots off, loads the interior beside it and makes the interior the active scene (its light and sky); coming out unloads the interior and switches the city back on. The city is never destroyed and loaded again, so coming out costs only the switch, and going in costs only the interior's own load. As built (`SceneTransition`, 17 I.2):
   - an interior names itself through `InteriorRuntime`; leaving it for the kept city switches the city on, and leaving it for any other scene is the old single swap, so at most one scene is kept;
   - each scene keeps its own player, camera and HUD, switched off with it; the clock and the world driver already lived through scene changes. The kept city's camera snaps behind its player when it is switched on, as a newly loaded one does;
   - `SceneTransition` keeps its gate, the darkening and the `Arriving` event, so portals, the Dwelling and the HUD work unchanged; `DoorTests` check that the city is kept, the interior unloaded, and the camera behind the player.
2. **Preload the door ahead. Not built:** step 1 already brings every door under 0.7 s, and a load held back in Unity blocks every later load until it is let through, so a preload would add risk for a few tenths of a second. It is reconsidered only if a door misses 1 s.
3. **Lighten the city's first load,** which still happens once per session (12 s now). By the census of R0: rooms behind windows built as render-only pieces at bake time, with their colliders, bodies, lights and scripts left out instead of switched off by overrides (this also ends the file-ID fragility of city plan §24.7); pieces of a module wrapper flattened where nothing reads the wrapper at run time; dressing that nobody walks into kept out of the physics scene. The rooms are 61 % of the city's objects, so this step is expected to roughly halve the first load.
   **Done** (2026-11-09) for the rooms: `VicRooms.Put` places each block of a room as plain renderers, one game object for each mesh the block shows with its mesh, materials and layer, and none of the block's wrapper, colliders, body, lights or scripts; a block's LOD group gives its first level. `VeilmouthKitHouses.RedressAll` built the 55 house types' rooms again in place: 13 380 pieces, 357 parts of them under a holder, where a stretch over a turned part would shear. Measured on the city as it stands after R4–R6 (`scene_load_20261109-012510.txt` before, `-012853.txt` after):

   | | Before | After |
   |---|---|---|
   | Cold load, first frame | 11.79 s (read 3.85 s, awake 11.27 s) | **7.86 s** (read 3.05 s, awake 7.44 s) |
   | Game objects, of them in rooms | 138 158, 83 611 | 92 114, 37 567 |
   | Kit blocks, of them off | 27 773, 24 942 | 2 831, 0 |
   | Box colliders, mesh colliders | 21 754, 2 841 | 2 335, 60 |
   | Lights, bodies, loose props | 1 627, 2 257, 2 237 | 109, 76, 56 |
   | Mesh renderers | 80 652 | 80 652 |
   | Doors, in and out | 0.48–0.64 s, 0.26–0.51 s | 0.43–0.73 s, 0.38–0.44 s |

   The renderers are the same, and the room-turn views of the High Street and the market, at noon and at 21:00, drawn before and after on the same city, differ by under 0.01 of 255 on average. The first load fell by a third, not a half: what remains is mostly the 80 652 renderers themselves. The prefabs fell from about 132 MB to 123 MB of text. The other two parts of the step are not built: the façade's pieces are about one game object per renderer already, so flattening their wrappers would save little, and the rooms were the dressing nobody walks into.
4. **Measure a build.** A development build of Veilmouth and one interior with the same diagnostic: the editor merges prefab instances when it loads a scene, a build does not, so the build's numbers are the game's.
   **Done** (2026-11-09): `SliceLoadProbeBuild.Build` makes a development build of an empty first scene, Veilmouth and the eight interiors in `Builds/LoadProbe` (3.0 GB, 25 minutes, most of them compiling shaders), leaving the project's build settings alone. Started with `-sceneLoadTiming <file>`, the player runs the editor diagnostic's walk itself (`GameDev.Slice.Diagnostics.SceneLoadProbe`), writes its report and quits. Windowed at 1600 × 900, Direct3D 12, High (`Logs/SceneLoad/build_load_1.txt`, `_2.txt`):

   | | First run | Second run |
   |---|---|---|
   | Cold load, first frame (read, awake) | 3.87 s (0.99 s, 1.69 s) | **1.97 s** (0.98 s, 1.54 s) |
   | Veilmouth → Soot & Bell | 0.54 s, then 0.38 s | 0.51 s, then 0.39 s |
   | Soot & Bell → Veilmouth | 1.11 s, then 0.46 s | 0.47 s, then 0.46 s |
   | Veilmouth → a dwelling → Veilmouth | 0.47 s, 0.47 s | 0.48 s, 0.47 s |

   The build reads and wakes the city in about 1.5 s where the editor takes 7.4 s. The target of under 1 s a door is met, except once: the first run's first way back took 1.11 s, of which 0.67 s was its first frame, and its cold load's first frame took 2.2 s. On the second run both are gone, so they are most likely the shaders' pipelines built on first use and kept in the driver's cache (not profiled; step 5 found this wrong for the door). A first run after installing would see them once; warming the shader variants during the cold load is the remedy if the owner wants it.
5. **Warm the shaders during the cold load** (owner, 2026-11-09, after step 4). By Unity 6's `GraphicsStateCollection`, which keeps each shader variant with the graphics states Direct3D 12 builds a pipeline for:
   - **trace:** the development build, started with `-traceGraphicsStates <file>`, records every variant and state it draws while it walks step 4's route, stands at every street door of the city by day and at night, and visits every interior the city's doors lead to; the file is kept as an asset in `Resources/GraphicsStates`, one per graphics API and platform;
   - **warm:** every player starts warming the collection that fits its graphics API on worker threads before its first scene loads (`ShaderWarmUp`), so the pipelines are built behind the veil of the cold load instead of on a door's first frame; `-noShaderWarmUp` turns it off for comparison. A development build also records the states the collection lacked, and the diagnostic reports them, the time the warming took and the number warmed;
   - **a first run** is measured by moving the build's own entries in the driver's shader cache aside (NVIDIA `DXCache` and its `D3DSCache` folder, recognised by the times they were made) and putting them back afterwards; nothing else's cache is touched.

   **Check:** on a first run with warming, no door over 1 s and no door's first frame over 0.2 s; the states missing from the collection listed; a second run no slower than step 4's.

   **Done** (2026-11-09). The trace (`Logs/SceneLoad/build_trace_cold.txt`) stood at 98 street doors, four ways each, and made 11 visits to the 8 interiors, by day and at 22:00, in 78 s: 189 graphics states for 174 shader variants, Direct3D 12, High, kept as `Resources/GraphicsStates/Veilmouth_Direct3D12.graphicsstate` (707 KB of text, 85 KB in the build) and checked by `ShaderWarmUpTests`. Measured in the development build at 1600 × 900, a first run with the build's driver cache moved aside, a second with it in place, `-noShaderWarmUp` for the runs without (`Logs/SceneLoad/build_cold_*.txt`, `build_hot_*.txt`):

   | | First run, without | First run, with | Second run, without | Second run, with |
   |---|---|---|---|---|
   | Cold load, first frame | 2.90 s, 2.51 s | 3.10 s, 2.36 s | 2.50 s, 2.63 s | 2.32 s, 1.99 s |
   | of it, the first frame after awake | 0.88 s, 0.66 s | **0.50 s, 0.43 s** | 0.50 s, 0.52 s | 0.48 s, 0.40 s |
   | Warming, on worker threads | — | 1.20 s, 1.55 s | — | 0.56 s, 0.56 s |
   | Doors | 0.40–0.57 s | 0.41–0.60 s | 0.40–0.53 s | 0.38–0.54 s |

   - The warming ends before the city's first frame, and no state was missing on the walk.
   - On a first run it takes about 0.3 s off the city's first frame. The whole cold load varies between runs by more than that (0.4–0.7 s), so no gain in it is claimed.
   - **Step 4's 1.1 s was not the shaders.** Three first runs with a cleared driver cache (the trace and the two without warming) took no door over 0.57 s and no door's first frame over 0.08 s. Its cause is not found: it came once, on the first run after a 25-minute build, and has not come again.
   - The trace is taken again when shaders or materials change much; a stale collection costs only the states it lacks, which a development build lists.
   - The project's player pauses when its window loses focus (`runInBackground` off); the diagnostic runs on in the background without changing the setting.

   **Check met:** doors at most 0.60 s and their first frames at most 0.08 s on a first run with warming; missing states listed (none); a second run with warming 1.99–2.32 s against 2.50–2.63 s without on the same day, step 4's 1.97 s lying within that spread.

**Seamless interiors** (doors that open straight into rooms built in the city scene) were the alternative if steps 1–3 missed the target. They would have changed accepted decisions (interiors as separate scenes, the generated Dwelling scene of the break-in rules), put the seven interiors and every dwelling into the city's load, and needed rooms and corridors rebuilt wider. With step 1 measured they are not needed.

**Check:** R0's test shows, in the editor, city → Soot & Bell ≤ 3 s and back ≤ 1 s; the slice's PlayMode set passes; the door tests (`DoorTests`) pass. **Met** after step 1 (2026-11-08): 0.62 s in and 0.52 s back. Step 3 brought the first load, once a session, from 11.8 s to 7.9 s in the editor (2026-11-09).

**In a build:** `SliceLoadProbeBuild.Build` by batch, then `Builds/LoadProbe/Veilmouth.exe -sceneLoadTiming <report file>`; add `-noShaderWarmUp` to measure without the warm-up, or `-traceGraphicsStates <file>` to trace the collection again, then copy the file over the one in `Assets/Resources/GraphicsStates`.

**For the owner's editor:** R0's test also runs from the Test Runner window (PlayMode, *SceneLoadTimingTests*, run selected). There it includes the editor's own windows, which a batch run does not have, so its numbers are the ones the owner feels.

## R2 Distance without pops (remark 4)

How games hide distance changes, and what applies here:

| Technique | What it does | Here |
|---|---|---|
| One quality level for editor and game | The owner sees what the game shows | the editor at Very Low is the main cause of the pops seen |
| LOD cross-fade, animated | A switch dissolves over about 0.5 s through a dither instead of popping; URP supports it and it is already on in the pipeline asset | the houses' and rooms' LOD groups use none |
| Keep the silhouette and the colour blocks | Only pieces smaller than a few pixels drop; window surrounds, cornices and bands never drop | the houses drop surrounds at 15 % |
| Distance rules, not screen size, where two systems must agree | Rooms and glass switch at one distance whatever the quality level | rooms by screen size, glass by distance |
| Interior mapping that matches the built room | The far window shows the same room the near one does, so the turn is invisible | the mapped room is generic |
| Fresnel reflection on glass | Far and glancing windows show mostly the sky and the street, which hides what is behind | the glass has a fixed tint |
| Trilinear and anisotropic filtering | Mip levels blend instead of meeting in lines | bilinear, anisotropic off at Very Low |
| Shadow cascades sized to the city | Even shadow detail; a far town in haze needs no sharp shadow | 2048 px over 350 m |
| GPU Resident Drawer with GPU occlusion culling (Unity 6) | Draws thousands of modules cheaply, so detail need not be dropped for speed | off |

Steps:

1. **Quality level.** The editor's current level becomes the level the slice ships at (High), with anisotropic filtering forced on, full-size textures, `lodBias` 1 and MSAA 4× or SMAA. One level is used by the editor, the tests and the build. (A project-settings change: the owner's approval, §5 D5.) **Done** (owner, D5, 2026-11-08):
   - `QualitySettings`: the editor's level is High, and High is the default of every desktop and console build (it was Ultra); High forces anisotropic filtering on and keeps `lodBias` 1 and full-size textures. The levels below stay for the owner's choice in a later options menu.
   - The pipeline (`URP_Pipeline.asset`, shared by every level) draws with MSAA 4×; the renderer is Forward+, which takes it.
   - What it changes, from `VeilmouthRenders.RenderQuality` (the same street views at Very Low and High, noon): the houses keep their window surrounds, quoins and chimneys at 60–100 m, where Very Low had dropped them; the rooms behind the windows show at 15–40 m, where Very Low had culled them; the far street is sharp instead of smeared. By calculation from §1 (c), the rooms now vanish at about 47 m (14 m ÷ 0.3), just past the glass's turn to the mapped room at 46 m, so the empty houses of 14–32 m are gone; steps 2–3 make it exact and smooth.
2. **Houses' LOD.** `fadeMode` CrossFade with animated cross-fading on every house; the detail level drops only pipes, brackets, pots and small rails, at 4 % of the screen; surrounds, cornices, bands and sills stay in every level. The window shader (`VicWindow`) gets the cross-fade keyword the facade shader already has. **Done** (owner's request, 2026-11-08), with one change to the step as written:
   - Each house's one LOD group holds only its small details: downpipes and their fittings, brackets, chimney pots and the small rails. They cross-fade out, animated over about 0.5 s, below 4 % of the screen, which with the player's 60° view is 21.7 × the house's largest size, 320–410 m for the city's houses. The group's size and centre are the whole house's, so the share is the house's.
   - Every other piece stands in no LOD level and is drawn at every distance: 710 304 triangles over the 55 types against 60 212 that fade. So no level ever swaps a wall, a frame or a pane, and the window glass, in no level, needs no cross-fade keyword: that part of the step is not built.
   - The 55 baked houses were given this in place (`VeilmouthKitHouses.RedressAll`), not baked again: a bake gives every piece a new file ID, and the strange windows override pieces by ID. All 82 of those overrides were checked to keep their modules.
3. **Rooms by distance.** The rooms behind the windows switch on and off by distance from the camera through one `CullingGroup` with hysteresis (on within 40 m, off beyond 48 m), independent of `lodBias`; the glass is fully mapped from 46 m, so the switch is never seen. Within 32 m the built room shows through clear glass. **Done** (2026-11-08), with two changes to the step as written:
   - **No `CullingGroup`.** A house's rooms carry `VicRoomsSwitch`, which decides before each camera culls by the distance from the camera to the nearest point of the rooms' box, and turns the renderers off through `forceRenderingOff`, which is never saved. In play the main camera decides; in the editor every game and scene view camera does. 113 distances a frame cost nothing, and the box's nearest point is what matters: the old LOD group measured from the rooms' centre, up to 10 m behind the front, and scaled with the `lodBias`.
   - **On within 48 m, off beyond 53 m, not 40/48.** The rooms must already stand when the nearest glass in front of them turns clear, at 46 m, and the farthest window of the 55 types lies 1.28 m outside its rooms' box (measured, `KitHouseLodTests`), so 48 m. Coming on at 40 m would have shown an empty house through the half-clear glass of 40–46 m. The 5 m gap is an authoring default.
   - **Checked by pictures** (`VeilmouthRenders.RenderRoomsSwitch`, `Logs/SliceSurvey/rooms_switch.txt`): four street views at noon and at night, the houses only, each drawn as the switch leaves it and with every room on.
     - The first pictures differed by up to 3 800 pixels: the rooms stood out through the mansards. Attic partitions ran to the outer wall, and attic rooms stood over pavilions, where the roof has no flat. Attic rooms now stand under the flat deck only, read from the house's own flat tiles, with the knee walls 0.4 m inside its edge (2.4 m from a straight outline, as before), and the 55 houses' rooms were built again. The fault had also shown on every house within 48 m.
     - After the fix seven views match to 2/255. The high street at noon differs in 13 pixels by at most 10/255, at shop fronts 80–135 m away, probably screen-space occlusion reading the depth behind the glass.
   - **Not yet checked:** this step's own check, a recorded walk in the owner's editor, and the frame rate there.
4. **Matching mapped rooms.** At bake, a small set of interior cubemaps is captured from built rooms of each kind and wealth (parlour, kitchen, bedroom, shop, stair); each far window maps the cubemap of its own room's kind with its own tint. The near-to-far turn then changes only the parallax, not the room. **Done** (D4 (a), 2026-11-09), with one change to the step as written:
   - **No cubemaps: each pane is told its own room.** The built rooms have three finishes only (dark, panelled, papered) on one floor and one ceiling, so the far look needs the room, not its kind: when the rooms are built (`VicRooms.Panes`), every pane is matched to the room whose window or glazed door it stands in, and its renderer carries the room as a 32-bit value (`VicWindowRooms`, the renderer's shader user value, which the SRP Batcher keeps): the finish, the back wall's depth behind the glass, the walls either side of the pane's middle, and the floor and ceiling from the sill, in tenths of a metre. The window shader maps that box with the finish's, floor's and ceiling's colours, measured from the room blocks' materials (`VicMaterials.RoomTones`). A pane matched to no room keeps the old made-up room. A cubemap would add furniture of two or three pixels at 46 m, for 15 textures and a capture pass.
   - 1 715 of the 55 house types' 1 762 panes are told their room (97 %); the 47 others match no room's opening within 1.5 m before its side and keep the made-up room.
   - The far look keeps the near look's narrow curtains and blind, where it had wider ones of its own. After dark a lit room glows in the lamp's colour as the near look does, whatever its finish.
   - **Checked** (`VeilmouthRenders.RenderRoomTurn`, `Logs/SliceSurvey/roomturn.txt`): two street views with every pane forced to its near look and to its far look, the mean difference over the image (0–255): the High Street 2.72 → 0.92 at noon and 2.77 → 1.71 at 21:00; the Market Place 1.16 → 0.44 and 1.31 → 0.91.
5. **Glass.** Fresnel reflection of the sky and a reflection probe per street, stronger with distance and at grazing angles; dimmer rooms by day, as real rooms are dark against daylight. **Done** (2026-11-09; `VeilmouthReflections.Bake`): the window shader already had the Fresnel term, stronger in the far look; what it reflected was Unity's default sky, since the scene had never baked its environment. Now:
   - the scene's environment reflection is the painted sky drawn alone into a cubemap at noon, with its haze and muted colour;
   - 70 street probes, one in each 30 m cell over the city's quarters at the open point nearest the cell's middle, 3 m over the ground, baked at noon at 128 px, blending over 4 m, with no box projection (a cell's box would not match its street's fronts), so the glass, the water and wet stone show the street and the houses across it;
   - both hold the day, so the sky's clock dims them toward night as it dims the sky's reflection (0.12 at night);
   - the probes stand under their own root, `Veilmouth Reflections`, and are baked again whenever the city is rebuilt.
   The rooms by day stay at the glass's `_RoomDay`, which the room-turn check found close to the built rooms.
6. **Textures.** Trilinear filtering on every kit and street texture, anisotropic 8–16. **Done** with step 1: the kit's 21 maps filter trilinear at 16× (`VicTextures.Configure` sets it on every rebuild); the street's pavement maps already did. The packs' own textures (rooms, props, trees) take the forced anisotropic filtering of step 1; their filter mode stays the packs'.
7. **Shadows.** 4096 px, four cascades over 150 m, cascade borders checked; beyond 150 m the haze of R3 carries the depth. **Done** (2026-11-09; `VeilmouthLook.ApplyShadows`): a 4096 px map over 150 m in four cascades ending at 10, 30, 70 and 150 m, so the nearest holds about 1 cm a texel where it held 4.5 cm; the last 20 % fades out; soft shadows at High quality, which also gives R3 step 6 its softer shadows. The five views of R3 at four hours (`Logs/SliceSurvey/s4_*`) show no cascade border. Reflection probe blending and box projection are on in the pipeline asset.
8. **Optional, after measuring:** GPU Resident Drawer with occlusion culling, once the facade and window shaders take DOTS instancing. **Measured, left off** (2026-11-10, [VEILMOUTH_CITY_PLAN.md](VEILMOUTH_CITY_PLAN.md) §29.7): the facade and window shaders take DOTS instancing, and the drawer takes 97 % of C5's renderers, but it saves 8 % of a development build's frame, which the CPU binds, and slows the editor two to three times.

**Check:** a recorded walk toward a row of houses from 120 m to 2 m in the editor shows no visible switch on any frame, in day and night light; the same with the camera turning; a still of two neighbouring houses at every distance shows the same detail on both.

## R3 Colour and air (remark 6)

The aim is the reference's one muted range: a warm palette with low saturation and compressed values, held together by light, haze and grading rather than by each texture alone.

Steps:

1. **Linear colour space** (§5 D2; resolves VS-COLOR as option b). Physically based light, fog and grading behave as the reference does only in Linear. Every material and light of the slice is checked after the switch. **Done** (owner, D2, 2026-11-08; `VeilmouthLook.ApplyProjectSettings`): `m_ActiveColorSpace: 1`, and the pipeline grades in HDR. Checked by the five views at four hours, the palette sheet and the interiors at noon and at night. What the check changed:
   - the sun is 2, twice the Gamma-era 1.05, so a sunlit facade shows its material's look;
   - the moon's light fell from 0.45 to 0.15 and from pure red to a soft red, because in Linear it flooded streets and facades red;
   - the environment reflection dims with the light, to 0.12 at night: the water and glossy surfaces reflected the day sky at night, as they already had in Gamma;
   - in the interiors the sky's reflection is kept faint (0.3 by day, 0.05 by night, `InteriorLighting`): at full strength it laid a grey sheen over the dark wood, which Gamma had hidden, and the rooms lost their warmth.
   The kit's showcase streets keep their saved Gamma-era light until they are rebuilt (streets plan §7).
2. **Palette pass on the kit's materials** (`VicMaterials`), with no new material slot: each tint and texture set brought into one range, with targets in sRGB albedo:
   - brick 0.25–0.40 value, saturation at most 35 %, browner reds;
   - stucco and stone 0.55–0.68 value, warm grey, never above 0.72;
   - slate 0.18–0.25, blue-grey toward grey;
   - wood and iron dark, 0.08–0.20;
   - grime streaks, sill stains and a soot gradient at the cornice stronger, so no wall reads clean.
   A palette sheet (one swatch per material under the day light) is rendered and checked by eye against the reference before any city is rebuilt.
   **Done** (2026-11-08): every facade tint in `VicMaterials` is set to these targets, the mortar soot-grey, and the dirt from the plinth raised on brick, stucco and the lighter stones. The materials change in place, so no city was rebuilt. The palette sheet (`VeilmouthRenders.RenderPalette`, `Logs/SliceSurvey/palette.png` and `palette.txt`) measures each swatch under the noon light and the grade: at most 35 % saturation (Brick_Rich), every swatch under 40 %. The first sheet showed the warm sun and grade adding about 12 points of saturation to pale surfaces, so stucco and stone now start nearly grey. **Not built:** sill stains and a soot gradient under the cornice, which the facade shader does not have; its dirt rises from the plinth only.
   **Soot streaks** (2026-11-11, on the owner's grant of autonomous work, which asks for rich views and the reference's feel): the facade shader (`VicFacadeInput.hlsl`) now washes soot down every upright face. The streaks follow world-space noise stretched about ten to one down the wall, so a streak is about 0.6 m wide and 4–6 m long. They are patchy from house to house by the shader's large noise. A streak is a film of soot, so it greys the face toward a dark soot of the material's dirt colour, flattening the brick's joints, rather than only darkening it: an earlier darkening by up to a quarter was lost in the brick's own contrast (`soot1`–`soot3`). Their strength is the material's dirt amount times `_Streaks`, 1 by default, so the poor brick and the dirty stone are the most streaked and the even slate and the rich wood the least; level faces, roofs at 45° and the ground take none. Checked by the six views (`soot4_*` against `win3_*`). **Cost:** none measurable. `FrameProbe` gives 4.23 ms of GPU pooled before and after, each view within 0.08 ms, and the frame 9.64 ms pooled (`Logs/SliceSurvey/soot_frames.txt`, city plan §39). Every size and strength is an authoring default. **Still not built:** sill stains and a soot gradient under the cornice, since the shader knows no storey's sills or cornice.
   **The plinth's dirt fixed** (2026-11-11): the shader measured the plinth's 0.60 m, the dirt's rise over it and the moss at a wall's foot from world height 0, as in the lab where the Victorian streets plan built it. Veilmouth's streets lie at 26 m (`VeilmouthScene.StreetLevel`), so neither the dirt nor the moss ever showed in the city, though this step had raised the dirt on 2026-11-08. `VicGround`, on the mystic layer's root, gives the shader the terrain's heightmap as globals, so both now rise from the ground under each wall, over the Penny's rise as on the quays. The rise counts on upright faces only, so the pavements and steps keep their tuned look, and the kerbs' faces take the gutters' grime. A scene with no ground given, such as the lab or an interior, measures from 0 as before. Checked by the six views (`ground1_*` against `soot4_*`) and `VeilmouthCityTests.TheFacadesMeasureTheirPlinthFromTheGround`. **Cost:** none measurable, 4.22 ms of GPU pooled (`Logs/SliceSurvey/ground_frames.txt`).
   **Soot under the cornice** (2026-11-11): soot now gathers under each house's cornice, darkest at its wall's top, fading over 3 m down the wall and over 1.5 m up the gables and stacks above it, as strong as the streaks.
   - **Where the eaves are.** Each kit house's body colliders run from the ground to its wall's top, so `VeilmouthEaves` reads every top exactly. It writes an eaves map: one texel each 0.5 m over every kit house in the scene, the wall's top in steps of 0.25 m from 20 m, 0 where no house stands. It is an 8-bit PNG of 17 KB (`Assets/Materials/Slice/Mystic/Veilmouth_Eaves.png`; 597 bodies, tops 26.8–50.7 m).
   - **The shader** reads the map 0.75 m inside the wall it draws, so a party wall above a lower neighbour takes the taller house's eaves.
   - **Rebaked** with the facades' ground, whenever the mystic layer or the city is built (`VeilmouthMystic.Ground`).
   - **Checked** by the six views (`eaves1_*` against `ground1_*` and `win3_*`). The quay's fronts and gables read weathered from the top down. `VeilmouthCityTests.TheEavesMapHoldsEachHousesWallTop` checks that the map holds each house's wall top.
   - **Cost:** none measurable, 4.25 ms of GPU pooled against 4.22–4.23 before (`Logs/SliceSurvey/eaves_frames.txt`).
   - Every size and strength is an authoring default.
   **Stains under the sills** (2026-11-11): rain running off each window's sill now carries soot down the wall under it. The stain is darkest under the stone and fades over 1.4 m. It narrows as it runs and breaks into drips about 0.2 m wide further down. It is twice as strong as the material's dirt, since at the dirt's own strength it showed only as a line at the stone.
   - **Where the windows are.** `VeilmouthEaves.BakeSills` reads every pane of the kit houses (`VicWindowRooms`): 3827 panes in 145 houses. It writes a sill map: one texel each 0.5 m over the city's windows, as an RGBA PNG of 60 KB (`Assets/Materials/Slice/Mystic/Veilmouth_Sills.png`, 831 × 893). Each texel holds the lowest, the second and the highest sill of the column of windows there, in steps of 0.125 m from 20 m, and how much of the texel the column covers.
   - **Three heights hold a column.** Measured over the panes, every sill above a column's second stands one kit storey, 3.75 m, over the one under it. Only the first gap differs: 4.8 m under a tall ground floor, 7.5 m where a storey has no window. Only 6 of the map's 31 794 column texels fall off that rhythm.
   - **The shader** reads the map 0.75 m inside the wall, as the eaves map is read. It runs a stain from the nearest sill over the point, and the cover's soft edge keeps the stains within the windows' width. A pane's sill is the bottom of its glass, so the stone sill hides where a stain starts.
   - **Rebaked** with the facades' ground and the eaves map (`VeilmouthMystic.Ground`).
   - **Checked** by the six views (`sill2_*` against `eaves1_*`): the quay's brick and the Penny's stucco read stained under their windows. `VeilmouthCityTests.TheSillMapHoldsEachWindowsSill` checks that every 25th pane finds its own sill in the map.
   - **Cost:** about 0.05 ms of GPU, 4.30 ms pooled against 4.22–4.25 before (`Logs/SliceSurvey/sills_frames.txt`). The map takes 2.9 MB of video memory.
   - Every size and strength is an authoring default; the storey of 3.75 m is measured.

   R3 step 2 is complete.
3. **Street surfaces** in the same range: setts and slabs in warm and cool greys, with a wetness mask (smoothness) along gutters and in hollows. **Done in part** (2026-11-08; `VeilmouthLook.TuneStreets`): the slice's three street layers take a tint measured against their textures' means, the carriageways' setts a cool grey (0.34), the slabs a warm grey (0.50), the squares' and lanes' cobbles a grey-brown (0.42). The pass runs once and leaves alone a layer whose tint is no longer the default, since the layers are tuned in the Inspector. **The wetness mask is not built:** the terrain already draws its eight layers, URP's limit, so a wet band along the gutters needs R4's pavement meshes or a decal. It moves to R4. **The setts again** (2026-11-11, on the owner's grant of autonomous work, which asks for rich views): Rough01 at a 4 m tile read at eye level as cracked flagstones half a metre across, in every view down a street. The carriageways now wear coursed granite setts, Modular Victorian City's paving made the city's own by `tools/assets/setts_from_paving.py`: greyed to the setts' 0.34 with a cool cast, the moss in its joints turned to soot and grit, a normal from its occlusion, and its smoothness in the alpha, 0.50 on the stone tops and 0.18 in the joints, so a low sun and the gas lamps catch them as on a damp street. A 2 m tile makes a stone about 0.24 × 0.15 m. Applied once by `VeilmouthLook.SettsFromVictorianPaving`; checked by the six views (`setts1_*`). The numbers are authoring defaults (city plan §34).
4. **Sky.** A painted HDR sky with clouds from the project's sky packs (8K Skybox Pack Free, Free HDR Sky) per time of day, blended by the clock in place of the procedural skies; a warm, washed sky by day, never saturated blue. Ambient light from the sky. **Done** (owner, 2026-11-08), with one change: a project shader, `GameDev/Slice/Sky`, blends Free HDR Sky's day and sunset cubemaps, muted toward grey (day 22 %, dusk 60 % of their saturation), its horizon band in the haze's colour. The sunset's painted sun is turned by the clock to stand where the light comes from. The night is the sunset's clouds, dark and cool, under the crimson moon. 8K Skybox Pack Free holds no sky images. **Change:** the ambient light stays the clock's three colours, tuned to the sky, and is not read from it, so it stays the same in the editor and in the game with no baking.
5. **Haze.** Distance fog in the sky's horizon colour from about 60 m, thicker toward the river and the sea; a height fog over the low town at dusk and night (the fog banks stay for the low layer). The far roofs fade, as in the reference. **Done in part** (2026-11-08): exponential fog in the horizon's colour, about 7 % at 60 m and 30 % at 300 m by day, denser at dusk and at night; the sky's horizon band takes the same colour, so the far roofs melt into it. **The height fog is built** (2026-11-10, on the owner's grant of autonomous work): Unity's fog is one value for the whole scene, so a pass of the project's own does the rest. `HeightFogFeature`, on the project's one URP renderer (`VeilmouthHeightFogSetup.Install`), draws once over the whole screen after the opaque world and the sky and before the transparents. It lays an exponential height fog over the picture:
   - densest at the sea's surface (24 m) and thinner by a factor e every 5 m up, so it lies in the low streets and over the river and the sea, where the rays run long and low; a ray that goes under the water ends on its surface;
   - lit toward the sun by day and toward the crimson moon by night, the glow strongest at dusk;
   - by night, the 16 lit gas lamps nearest the eye glow in it. Each halo is the lamp's light scattered along the ray up to what the ray meets, so a wall in front of a lamp hides its halo. The halo is held within about 1.5 m of the glass, with a faint wider part;
   - uneven in slow drifting patches.
   Its density at the sea's surface is 0.009 per metre by day, 0.012 at dusk and 0.016 by night. These numbers and the halos' strength are authoring defaults in `VeilmouthSky`, which sets them by the clock with the haze. Unity's fog stays the even distance haze, and the fog banks stay. Fog is only seen: nothing in play reads it. **Why before the transparents:** drawn after them, the first check lost some trees' leaves outside the walls at dusk; drawn before them, every leaf stays. The few transparent materials left, shop and cabinet glass and the wards' runes and signs, take the distance haze only (`VeilmouthHeightFogSetup.AuditTransparents` lists them). Plan views from above, previews and reflections are left without it. **Cost, measured** (2026-11-11): `FrameProbe` now has a fourth mode, the SRP Batcher with the height fog off, and with `-frameMinute` it moves the game's clock on before measuring. In a development build at 1920 × 1080 over its seven views, the pass costs 0.07 ms of GPU at 08:00 (4.23 ms against 4.16, `Logs/SliceSurvey/fog_day_frames.txt`) and 0.28 ms at 22:00 with the lamps' halos (4.50 against 4.22, `fog_night_frames.txt`). That is within the estimate of 0.1–0.3 ms, and far inside the frame budget of 16.7 ms.
6. **Light.** A lower, warmer sun by day (golden from 15:00), softer shadows; ambient occlusion (URP's SSAO renderer feature); bloom kept faint; light shafts only if a cheap screen-space effect looks right. **Done in part** (2026-11-08): the sun is at most 42° high, golden from 15:00 to full at 19:00 and in the morning until 08:00; URP's SSAO on the renderer (intensity 1.4, radius 0.35 m, a third on direct light, fading out by 80 m); bloom only over HDR highlights, at 0.5. **Not built:** softer shadows, as the shadow settings were left as they are. **Light shafts are built** (2026-11-10, on the owner's grant of autonomous work) in the height fog's pass (step 5). A second draw at half size marches from each pixel toward the sun or the moon on the screen, 24 steps over 0.8 of the way, each step counting 0.95 of the one before. It counts the open sky near the light that shows past the roofs and the trees. The fog then adds the light's colour there, more where the air is thicker. Their strength is 0.25 by day, 1.6 at dusk and 3 by night, the moon's light being dim. How tightly the sky round the light counts is a cosine power of 24 by day, 16 at dusk and 60 for the small moon. All are authoring defaults in `VeilmouthSky`. The shafts are drawn only while the light stands ahead of the eye, fading out as it turns away. With a low sun they read as warm light pouring down a street's far end and through the trees' gaps. **Cost:** by estimate under 0.1 ms, 24 depth reads a pixel at half size.
7. **Grade.** HDR colour grading with the Neutral or ACES tonemapper; a day grade (saturation −15 to −25, warm highlights, cool lifted shadows) and night and dusk grades, blended by the clock as the twilight grade is now. **Done** (2026-11-08): Neutral, saturation −22, contrast 6, split toning with warm highlights and cool shadows, white balance +2. The twilight grade blends in by the clock as before, its saturation now −8 instead of +8. Night has no grade of its own; the dark sky, the moon and the lamps make it. **Fuller** (2026-11-11, on the owner's grant of autonomous work, which asks for rich, full views): with the height fog of step 5 greying the distance, the day grade takes saturation to −15, the plan's lightest, and contrast to 10, so the near street keeps its colour and depth while the far one fades. The palette sheet then read the city's leaves (`City Foliage 001`) at 49 %, so the city green's middle and light keys came down to about 33 % in sRGB (`VeilmouthLook.CityGreen`, repainted in place by `VeilmouthLook.RepaintCityGreen`); the sheet reads 39 % for them and at most 35 % for every other swatch. `VeilmouthMystic.Regrade` writes both grade profiles again. All are authoring defaults.
8. **Trees and greenery** in the same range: olive and autumn tints for the common trees, or a tree pack nearer the reference's realism (another pack needs the owner, §5 D7). **Done in part** (owner, D7 (a), 2026-11-08; `VeilmouthLook.RetintTrees`): the fruit trees, pines and shrubs wear project copies of Polytope's leaves, using the pack's own two-colour gradient in olive and dark olive, with less translucency. The catalogue swaps them in on every wrap. **Not built:** autumn tints, which need a second wrapper per tree and a rule for where they stand. The grass keeps its own material.
9. **Night windows** (2026-11-11, on the owner's grant of autonomous work). Every lit room glowed the same even cream, because the tonemapper flattened its glow, against the style guide's warm amber hearth light (§3). The kit's window shader (`VicWindow`) now gives each room its own lamp and hours, all by the window's hash and with no new material:
   - the near glow on the pane is 0.24 of the lamp's strength, not 0.35, so a lit window stays amber;
   - a lit room's lamp runs from a warm candle to a whiter oil lamp, from half to 1.2 times the material's strength;
   - the lamp stands 1.0–2.6 m behind the pane, so its light is brightest round it and moves as the eye moves, from 0.4 to 1.7 times;
   - from 22:00 to 01:00 the rooms go dark one by one, until 45 % of the evening's are lit, and stay so until dawn. `VeilmouthSky` sets this through the global `_VicLate` (`VicStreetLighting.Late`).
   - The strange windows (`VeilmouthMystic`), lit in every pane, keep their even light and their hours.
   Checked by the six views (`win3_*`, against `setts1_*`). All are authoring defaults.

**Check:** five fixed views at 08:00, 16:00, 19:30 and 23:00, side by side with the reference, judged by the owner; no material above 40 % saturation on the palette sheet. Since 2026-11-10 the views are C5's (`VeilmouthRenders.RenderLook`): Long Tongue Street from the west gate, the Penny's market, the quay with the sea on its right, the city from a pier's end, the river north from the west gate's bridge, and, a sixth, Long Tongue westward into the evening sun. The earlier five (the High Street, the Market Place, Bell Street, the Ridge, the river) stood in C3's streets.

## R4 Streets: continuous pavements and wider roads (remark 5 and the map)

**Pavements as islands.** Each block's pavement is one mesh: the band between the building line and the carriageway, around the whole block, 0.15 m above the carriageway, its top in slabs and its edge a stone kerb, with a mesh collider. Rules:

- continuous along every front of the block, round its corners with a kerb radius of 2 m;
- broken only by carriageways; a lane or passage without carriages does not break it (the pavement runs across its mouth);
- never crosses a carriageway; streets are painted carriageway first, and no pavement paint exists any more;
- door steps and stoops start from the pavement's top; lamps, trees in square planters and bollards stand on it by the kerb;
- the player steps up 0.15 m (the controller's step is 0.4 m), and the Combat Grid reads it as one level with a kerb;
- a wet band along the gutter, a strip of the carriageway at the kerb with a higher smoothness, comes with the pavement meshes (moved from R3 step 3: the terrain's eight layers are full).

**Wider roads**, from the map's proportions (§5 D1). Proposed sections, replacing §24.2's:

| Street | C3 | Proposed | Section |
|---|---|---|---|
| High Street | 18 m | 24 m | 5 m pavement, 14 m carriageway, 5 m pavement |
| Crown Avenue | 24 m | 30 m | 4 m, 8 m, a 6 m mall, 8 m, 4 m |
| The Parade | 12 m | 16 m | 4 m pavement, 8 m carriageway, 4 m promenade |
| Byelaw streets | 12 m | 16 m | 4 + 8 + 4 m |
| Lanes | 6 m | 8 m | shared, cobbles |
| Passages | 3 m | 4 m | through a row, to a yard |

On C3's area the widening takes about 5 % more ground, met by shallower yards and two fewer lanes; the blocks keep their courts. A round place at the High Street's crossing with the Crown Axis, as the map's centre, is an option for the owner (§5 D1).

**Check:** a top view shows every block ringed by an unbroken pavement; no pavement crosses a carriageway; the player walks round a block without a fall or a gap; the slice's tests pass; the Combat Grid probe reads the kerb as a step.

**Pavements done** (2026-11-09, `VeilmouthPavements`, built by `VeilmouthCity.RebuildStreets`):

- The pavement is the walk strips of §24.2's sections taken together, less every carriageway.
- A carriageway that ends against another street is carried on to that street's middle, so the pavement stops at its mouth: 11 ends. The 27 corners where two carriageways meet are rounded at 2 m.
- Lanes and passages carry no carriages and do not break it. Where the ground across a mouth differs by 1 m or more, the mouth is left as it is.
- **Built:** 16 closed outlines, 6 525 m², 0.15 m above the carriageway.
  - The top is the kit's paving: whole 1 m slabs where they fit (3 547) and 0.25 m cells along the curves.
  - Every edge has a kerb face in the kit's light stone, 3 292 m in all, and the islands carry one mesh collider on the Ground layer. A kerb face against a house is left out.
  - The mesh has 161 722 triangles, in two separate assets under `Works`; the kerb ribbons of C3 are deleted.
- **Wet band:** a transparent, glossy band 0.5 m wide along 2 079 m of gutter, on the NoGrid layer, with no shadow (R3 step 3's wetness).
- **Standing on it:** a house's floor, its door and its stoop rise to the pavement's top when it stands on one. Lamps, props and walkers stand on its top. A corner lamp that would stand where the kerb's curve cuts its cell moves to the pavement.
- **Paint:** the walk strips are painted as setts beneath the islands. No pavement paint is seen any more.
- **Checked:**
  - the top views `r4_top_*` show every block ringed and no pavement across a carriageway;
  - the views at eye height `r4_eye_*` show the kerb, the rounded corner and the wet band;
  - the kit check finds no fault;
  - the slice's EditMode tests pass, 103 of 103, and its PlayMode tests 38 of 39, with no failure: `RenderTheMap` skips itself without graphics.
- **Not checked:** a walk round a block in play, and a Combat Grid probe on the kerb. The collider is continuous by construction and on the layer the grid reads.

**The wider roads are not built.** To widen the streets as the table asks on C3's area, every block would lose 4–8 m of depth. The blocks cannot give it: their yards are 2–6 m deep, and the yard doors need their clearances. So the widening needs a new layout of the city, a C4, which is more than this step's rebuild. D1 (a) stands as the choice of area, and the widening waits until the owner asks for a C4. Only the passages are widened, from 3 m to 4 m (R5).

## R5 Passages instead of partitions (remark 1)

- A terrace span ends **flush** against the next building where both eaves allow it, or leaves a real **passage of 4 m** to the yard behind, with its own ground and a gate or railing at the back; never a 1 m slot and never a free-standing pier on the front line.
- What does not fill a span becomes a passage (when it is 4 m or more) or is taken up by choosing other house widths; a remainder under 4 m is not allowed by the filler.
- Every house beside a passage shows a faced end (the kit's corner houses), so no open party wall shows.

**Check:** no `YardWall` on a front line in the built city; every gap between houses is 0 or at least 4 m; the kit check finds no fault.

**Done** (2026-11-09, `VeilmouthCity.Terrace`):

- `YardWall` is removed.
- **Where a span meets a building,** or comes within 4 m of one, it leaves a passage that makes the gap 4 m.
- **What no house fills** joins an end's passage, or stands as a middle passage, with each run of houses keeping its faced ends. A remainder under 4 m is filled again with 4 m less of house.
- **A span no house fits** stays open.
- **Ground:** a passage and an open span take their ground as walk, so no later row builds into them.
- **The back:** each passage, and each open span of 4 m or more, has a railing on the houses' back line with a 2 m opening to the yard; the kit has no gate.
  - The railing is placed after every house stands, so it keeps clear of the corner houses' side stoops.
- **Widened to 4 m:** the passages to Wardley Garden, the two to the Tenement Court, and Ridge Alley.
- **Built:**
  - 22 passages between houses, and 18 spans left open, 2–12 m;
  - 93 houses with 101 street doors. C3 had 103 houses with 111.
  - The ten houses lost were in spans between two buildings, which now need 8 m of passages besides a house. The open spans read as gaps in the row with the yard behind.
- **Checked:** the city's report counts the gaps under 4 m between buildings that share a front or back line, and finds none. The kit check finds no fault.

## R6 Porches (remark 3)

Victorian porches before a raised door take one of four forms: a **portico** whose beam carries a balustraded balcony; a **flat porch roof** on columns with a cornice; a **door hood** on two carved brackets without columns, common on terraces; and a **cast-iron and glass canopy** on brackets or slim posts, common on villas and shops.

Steps:

1. Measure the kit's balcony slab, balcony rails, flat roof deck and porch beam (`tools/assets/fbx_sections.py`) against the porch's depth (columns 1.32 m out at ×1).
2. If the slab covers the porch, every porch becomes a portico: the beam carries the balcony slab and its rails, reached from the first-floor window above; villas and civic houses get it.
3. Where it does not fit, the missing piece is named and the porch waits: a porch roof 2 × 1.4 m with a cornice, a door hood on brackets, and an iron-and-glass canopy go on the wanted list (`SLICE_MODELS_WANTED.md`), never pieced together from other modules (the kit's rule).
4. Terraces get door hoods when they exist; until then their doors keep the kit's lintel trims alone.

**Check:** a close-up render of each porch kind with its joints checked; no bare beam stands anywhere.

**Done** (2026-11-09):

- **Measured:** the kit's balcony slab is 2 × 0.86 m and 0.12 m thick, and a porch's columns stand 1.32 m out (both at ×1). The slab cannot roof the porch, so step 2's portico is not built. The kit has no porch roof, hood or canopy.
- **The doorcase:** every raised door of the city's houses (rows, detached houses, villas, civic houses and the palace) keeps a doorcase of two 3 m pilasters beside the door (`VicDress.Doorcase`), its stoop, and its lintel trims. Villas and civic houses keep the stone balcony on brackets above the door. The kit's own rich street (`VicHousePlans`) keeps its porch, which its tests assert.
- **In place:** `VeilmouthKitHouses.StripPorches` took 24 columns and beams out of the 8 baked house types that had them, keeping every other piece's file ID, so the strange windows' overrides hold; `VeilmouthCity.RebuildPorches` then rebuilt the streets and baked the 70 probes again.
- **Wanted:** a porch roof with a cornice, a door hood on brackets and an iron-and-glass canopy, with the shop front, the spire and the clock of R7–R8 (`SLICE_MODELS_WANTED.md`, *Porches, fronts and the skyline*). Terraces wait for the hood, as step 4 says.
- **Checked:** close-ups of a stucco and a white villa, a civic house, the palace and a detached house, front and side (`r6_porch_*`); no bare beam stands, the pilasters meet the stoop and the lintel. The kit check finds no fault in 2 830 blocks and 101 doors.

## R7 Street life and dressing

The reference's streets are full; the slice's are bare. In order of effect per cost:

1. **Ground floors:** shop fronts of wood and glass on every shop house (the kit's shop front where it exists; a wanted piece where it does not, asset map §9 row 24), awnings over shop windows, hanging signs on brackets.
2. **Pavement furniture:** trees in square planters along the High Street and Crown Avenue, bollards at corners, double gas lamps on squares, benches, posters on blank walls.
3. **Fronts:** window boxes with flowers on the middling and rich streets, ivy on a few walls, balconies with iron rails on the Ridge.
4. **Squares:** the Market Place with bunting and strings of lamps between posts, stalls, a flower cart, crates and barrels.
5. **Ground:** leaves gathered in gutters and corners, puddles by the kerb, as decals.
6. **People:** more townsfolk on the High Street and the market by day.

What the packs lack goes on the wanted list; another pack needs the owner.

**Check:** the five views of R3 hold something at every few metres of front, as the reference does.

**Done** (2026-11-09), as far as the packs go:

1. **Goods before the shops** (`VeilmouthCity.ShopGoods`): 32 of the 37 shop doors have a crate, a barrel or a box beside their stoop, 2.6 m from the door's middle, in the second metre from the front. Blocks stand on whole cells, and the shop front stands proud into the first: the kit check found all 32 touching it there. The other 5 pavements are too narrow to leave a metre beyond. Shop fronts, awnings and hanging signs are on the wanted list.
2. **Pavement furniture:** a street tree's planter and a double gas lamp are added to the wanted list; bollards and posters were on it. The benches and trees of C3's squares and Crown Avenue stay.
3. **Fronts:** the Ridge's villas now have iron rails on the balcony over the door, the kit's own iron balcony in the stone one's place. Only the two villa types were baked again (`VeilmouthKitHouses.BakeOnlyBatch -bakeOnly Villa`), since no override in the city reaches them. Window boxes and ivy are on the wanted list.
4. **Squares:** the Market Place keeps C3's stalls, crates and barrels. Bunting, a string of lamps and a flower cart are added to the wanted list.
5. **Ground:** fallen leaves are added to the wanted list as decals, beside the puddles already on it.
6. **People: not built.** The townsfolk are stand-ins for the schedules of S5c, so more of them wait for S5c rather than standing before the system that will move them.

**Checked:** `r7_shops_north`, `r7_shops_south` and `r7_villa_balcony` at noon, and the five views of R3 again (`r7look_*`). The kit check finds no fault in 2 862 blocks. Most fronts still stand bare, because most of what fills the reference's fronts is a piece the packs lack; the check is met only when those pieces come.

## R8 Skyline and roofs

- The Gothic skyline's spires, a church or cathedral beyond the walls or on the Ridge, and a clock face (asset map §9 rows 23–24) remain wanted; R3's haze makes them read as the reference's landmarks.
- Terracotta roofs in parts of the city, as in the reference's roofscape, would change style guide §2 ("no tiled roof") and need a clay-tile texture set on the slate slot (§5 D6).

**State** (2026-11-09): nothing is built, since the packs hold none of it. The church with its spire and the clock face are on the wanted list, with the factory chimneys and the gasometer that were on it. Clay tiles are listed only for D6 (b); D6 stays the owner's, and the city keeps slate.

**The cathedral** (2026-11-11): C4's cathedral placeholder now wears a Gothic dress of the kit's own materials, with a pinnacled tower and an octagonal spire to 70 m (city plan §35). So the skyline has its landmark from the pier until the model comes. The clock tower wears four placeholder clock faces, whose dials glow at night (city plan §38); a real clock face is still wanted. The works' boxes stand as mills with sawtooth roofs, and six placeholder stacks of 30–36 m stand over them, with no smoke (city plan §36).

---

# 4. Order

| Step | What | Why first | Depends on |
|---|---|---|---|
| 1 | R2.1 quality level, R2.6 filtering (**done** 2026-11-08) | one setting, fixes most of remark 4 at once | D5, chosen |
| 2 | R1.1 additive interiors (**done** 2026-11-08; R1.2, the preload, not needed) | the owner's worst pain | D3 (a), chosen |
| 3 | R3.1–R3.8 colour, sky, haze, light, grade, trees | decides most of the look, needs no rebuild of the city | D2 and D7, chosen |
| 4 | R2.2–R2.5, R2.7 LOD, rooms, glass, shadows (**done** 2026-11-09) | needs the houses baked again | step 1 |
| 5 | R4, R5 streets, pavements, passages (**done** 2026-11-09, without the wider roads, which need a C4) | one rebuild of the city | D1 |
| 6 | R6 porches (**done** 2026-11-09: doorcases; the porch pieces wanted) | joins the house bake of step 4 or 5 | measurement |
| 7 | R1.3–R1.4 lighter first load, build measured (**done** 2026-11-09: 11.8 s → 7.9 s in the editor, 2.0–3.9 s in a build) | after the city's content settles | step 5 |
| 8 | R7, R8 dressing and skyline (**done** 2026-11-09 as far as the packs go: shop goods, iron balconies on the Ridge, the rest wanted; people wait for S5c; D6 open) | content on a settled city | D6, D7 |

Each step ends with its check, the narrowest test set that covers it, renders for the owner, and a commit.

---

# 5. Decisions for the owner

| # | Question | Options | Recommended |
|---|---|---|---|
| D1 | Wider roads on which area? | (a) C3's area, shallower yards and two fewer lanes; (b) a larger city; and whether a round place at the centre is wanted | (a), without the round place; **approved by the owner's authorization of 2026-11-08**. The pavements and passages are built; the wider roads need a C4 layout (R4) |
| D2 | Colour space (VS-COLOR) | (a) keep Gamma; (b) Linear, every material and light checked again | **Decided by the owner, 2026-11-08: (b) Linear**, R3 step 1 |
| D3 | How doors load | (a) additive interiors with the city kept loaded, separate scenes as now; (b) seamless interiors in the city scene | **Decided by the owner, 2026-11-08: (a)**, built in R1 |
| D4 | Rooms behind windows | (a) built rooms near, matching mapped rooms far (R2.3–R2.4); (b) mapped rooms only, cheaper to load | (a); **approved by the owner's authorization of 2026-11-08**, built in R2 step 4 |
| D5 | The quality level the editor and game use | High for both | **Decided by the owner, 2026-11-08: High**, set in R2 step 1 |
| D6 | Tiled roofs | (a) slate only, as now; (b) terracotta in parts of the city | owner's taste |
| D7 | Trees and greenery | (a) the present packs, retinted; (b) a more realistic tree pack | **Decided by the owner, 2026-11-08: (a)**, R3 step 8 |

The owner also chose, for R3 step 4, the painted skies of the packs already in the project over the procedural ones (2026-11-08). Of the two packs the step names, only Free HDR Sky (`Assets/Skybox/`) holds sky images in the project, a day and a sunset; 8K Skybox Pack Free holds only its example scenes' lighting settings. So the day and dusk skies are Free HDR Sky's, and the night stays the agent's own dark sky with the crimson moon.

---

# 6. What stays

- The city plan C3's places, axes and districts, the seven buildings, the POIs and the Ritual Site Candidates.
- The kit's rules: modules stay separate, no merged mesh, no atlas, no new mesh or material slot; a missing module is named and waited for.
- The mystic layer of §24.5, retuned in R3's light.
- Interiors as separate scenes (with D3 (a)).
