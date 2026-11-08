# Veilmouth Reference Plan

Version: 0.1

Status: Draft — the agent's plan from the owner's walk of C3 and reference images (2026-11-08); waits for the owner's decisions in §5 before any step past R0

Created: 2026-11-08

Last Updated: 2026-11-08 (first draft: the owner's six remarks traced to their causes in the project, the reference read, and eight workstreams with checks)

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

| Step | Before | With the load behind the veil at High priority |
|---|---|---|
| Veilmouth, cold | 54.1 s (file read 3.5 s, the rest bringing objects to life) | 12.3 s |
| Veilmouth → Soot & Bell | 1.24 s | 1.01 s |
| Soot & Bell → Veilmouth | 51.7 s | 10.5 s |

- **The interiors are not the problem:** the Soot & Bell holds 1 431 game objects and loads in under a second. Every minute was the city loading again on the way out.
- **Veilmouth holds 149 775 game objects, 91 701 of them (61 %) in the rooms behind the windows.** It has 87 274 mesh renderers, and tens of thousands of components switched off by the rooms: 28 068 kit blocks, 22 316 box colliders, 2 786 mesh colliders, 1 729 lights and 2 183 loose props, with 2 259 bodies.
- **The minute came from Unity's default loading priority.** The profiler showed no long frame: the load was spread over about 1 400 short frames, because the default `Application.backgroundLoadingPriority` (BelowNormal) gives a loading scene about 4 ms of each frame. Behind the black veil the frame rate does not matter, so `SceneTransition` now raises the priority to High for the journey (about 50 ms a frame) and restores it on arrival. This cut the way out from 52 s to 10.5 s.
- What remains, about 10 s, is the city's real cost of reading and bringing 150 000 objects to life.

**Target:** a door takes at most 2–3 s from the key to the lifted veil in the editor, and under 1 s in a build.

Steps, in order; each is measured by R0's test before the next:

0. **Load behind the veil at High priority.** Done and measured, above.
1. **Keep the city loaded (recommended).** Interiors stay separate scenes, as [16](../../../16%20Architecture%20Pivot.md) and the accepted Confront entry require, but load **additively**: entering loads the interior beside the city, switches the city's roots off and makes the interior the active scene (its light and fog settings); coming out unloads the interior and switches the city back on. The city is never destroyed and loaded again, so coming out costs only the switch, and going in costs only the interior's own load. The player, the camera and the clock are one set that crosses with the traveller instead of one per scene. `SceneTransition` keeps its gate, the darkening and the `Arriving` event, so portals, the Dwelling and the HUD keep working.
2. **Preload the door ahead.** When the player comes within about 6 m of a street door, its interior starts loading in the background with activation held back; the key then only activates it. A Dwelling is one shared scene that builds its rooms on arrival, so it stays loaded once loaded.
3. **Lighten the city's first load,** which still happens once per session (12 s now). By the census of R0: rooms behind windows built as render-only pieces at bake time, with their colliders, bodies, lights and scripts left out instead of switched off by overrides (this also ends the file-ID fragility of city plan §24.7); pieces of a module wrapper flattened where nothing reads the wrapper at run time; dressing that nobody walks into kept out of the physics scene. The rooms are 61 % of the city's objects, so this step is expected to roughly halve the first load.
4. **Measure a build.** A development build of Veilmouth and one interior with the same diagnostic: the editor merges prefab instances when it loads a scene, a build does not, so the build's numbers are the game's.

**Seamless interiors** (doors that open straight into rooms built in the city scene) are the alternative only if steps 1–3 miss the target. They would change accepted decisions (interiors as separate scenes, the generated Dwelling scene of the break-in rules), put the seven interiors and every dwelling into the city's load, and need rooms and corridors rebuilt wider. The plan does not recommend them.

**Check:** R0's test shows, in the editor, city → Soot & Bell ≤ 3 s and back ≤ 1 s; the slice's PlayMode set passes; the door tests (`DoorTests`) pass unchanged.

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

1. **Quality level.** The editor's current level becomes the level the slice ships at (High), with anisotropic filtering forced on, full-size textures, `lodBias` 1 and MSAA 4× or SMAA. One level is used by the editor, the tests and the build. (A project-settings change: the owner's approval, §5 D5.)
2. **Houses' LOD.** `fadeMode` CrossFade with animated cross-fading on every house; the detail level drops only pipes, brackets, pots and small rails, at 4 % of the screen; surrounds, cornices, bands and sills stay in every level. The window shader (`VicWindow`) gets the cross-fade keyword the facade shader already has.
3. **Rooms by distance.** The rooms behind the windows switch on and off by distance from the camera through one `CullingGroup` with hysteresis (on within 40 m, off beyond 48 m), independent of `lodBias`; the glass is fully mapped from 46 m, so the switch is never seen. Within 32 m the built room shows through clear glass.
4. **Matching mapped rooms.** At bake, a small set of interior cubemaps is captured from built rooms of each kind and wealth (parlour, kitchen, bedroom, shop, stair); each far window maps the cubemap of its own room's kind with its own tint. The near-to-far turn then changes only the parallax, not the room.
5. **Glass.** Fresnel reflection of the sky and a reflection probe per street, stronger with distance and at grazing angles; dimmer rooms by day, as real rooms are dark against daylight.
6. **Textures.** Trilinear filtering on every kit and street texture, anisotropic 8–16.
7. **Shadows.** 4096 px, four cascades over 150 m, cascade borders checked; beyond 150 m the haze of R3 carries the depth.
8. **Optional, after measuring:** GPU Resident Drawer with occlusion culling, once the facade and window shaders take DOTS instancing.

**Check:** a recorded walk toward a row of houses from 120 m to 2 m in the editor shows no visible switch on any frame, in day and night light; the same with the camera turning; a still of two neighbouring houses at every distance shows the same detail on both.

## R3 Colour and air (remark 6)

The aim is the reference's one muted range: a warm palette with low saturation and compressed values, held together by light, haze and grading rather than by each texture alone.

Steps:

1. **Linear colour space** (§5 D2; resolves VS-COLOR as option b). Physically based light, fog and grading behave as the reference does only in Linear. Every material and light of the slice is checked after the switch.
2. **Palette pass on the kit's materials** (`VicMaterials`), with no new material slot: each tint and texture set brought into one range, with targets in sRGB albedo:
   - brick 0.25–0.40 value, saturation at most 35 %, browner reds;
   - stucco and stone 0.55–0.68 value, warm grey, never above 0.72;
   - slate 0.18–0.25, blue-grey toward grey;
   - wood and iron dark, 0.08–0.20;
   - grime streaks, sill stains and a soot gradient at the cornice stronger, so no wall reads clean.
   A palette sheet (one swatch per material under the day light) is rendered and checked by eye against the reference before any city is rebuilt.
3. **Street surfaces** in the same range: setts and slabs in warm and cool greys, with a wetness mask (smoothness) along gutters and in hollows.
4. **Sky.** A painted HDR sky with clouds from the project's sky packs (8K Skybox Pack Free, Free HDR Sky) per time of day, blended by the clock in place of the procedural skies; a warm, washed sky by day, never saturated blue. Ambient light from the sky.
5. **Haze.** Distance fog in the sky's horizon colour from about 60 m, thicker toward the river and the sea; a height fog over the low town at dusk and night (the fog banks stay for the low layer). The far roofs fade, as in the reference.
6. **Light.** A lower, warmer sun by day (golden from 15:00), softer shadows; ambient occlusion (URP's SSAO renderer feature); bloom kept faint; light shafts only if a cheap screen-space effect looks right.
7. **Grade.** HDR colour grading with the Neutral or ACES tonemapper; a day grade (saturation −15 to −25, warm highlights, cool lifted shadows) and night and dusk grades, blended by the clock as the twilight grade is now.
8. **Trees and greenery** in the same range: olive and autumn tints for the common trees, or a tree pack nearer the reference's realism (another pack needs the owner, §5 D7).

**Check:** five fixed views (the High Street, the Market Place, Bell Street, the Ridge, the river) at 08:00, 16:00, 19:30 and 23:00, side by side with the reference, judged by the owner; no material above 40 % saturation on the palette sheet.

## R4 Streets: continuous pavements and wider roads (remark 5 and the map)

**Pavements as islands.** Each block's pavement is one mesh: the band between the building line and the carriageway, around the whole block, 0.15 m above the carriageway, its top in slabs and its edge a stone kerb, with a mesh collider. Rules:

- continuous along every front of the block, round its corners with a kerb radius of 2 m;
- broken only by carriageways; a lane or passage without carriages does not break it (the pavement runs across its mouth);
- never crosses a carriageway; streets are painted carriageway first, and no pavement paint exists any more;
- door steps and stoops start from the pavement's top; lamps, trees in square planters and bollards stand on it by the kerb;
- the player steps up 0.15 m (the controller's step is 0.4 m), and the Combat Grid reads it as one level with a kerb.

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

## R5 Passages instead of partitions (remark 1)

- A terrace span ends **flush** against the next building where both eaves allow it, or leaves a real **passage of 4 m** to the yard behind, with its own ground and a gate or railing at the back; never a 1 m slot and never a free-standing pier on the front line.
- What does not fill a span becomes a passage (when it is 4 m or more) or is taken up by choosing other house widths; a remainder under 4 m is not allowed by the filler.
- Every house beside a passage shows a faced end (the kit's corner houses), so no open party wall shows.

**Check:** no `YardWall` on a front line in the built city; every gap between houses is 0 or at least 4 m; the kit check finds no fault.

## R6 Porches (remark 3)

Victorian porches before a raised door take one of four forms: a **portico** whose beam carries a balustraded balcony; a **flat porch roof** on columns with a cornice; a **door hood** on two carved brackets without columns, common on terraces; and a **cast-iron and glass canopy** on brackets or slim posts, common on villas and shops.

Steps:

1. Measure the kit's balcony slab, balcony rails, flat roof deck and porch beam (`tools/assets/fbx_sections.py`) against the porch's depth (columns 1.32 m out at ×1).
2. If the slab covers the porch, every porch becomes a portico: the beam carries the balcony slab and its rails, reached from the first-floor window above; villas and civic houses get it.
3. Where it does not fit, the missing piece is named and the porch waits: a porch roof 2 × 1.4 m with a cornice, a door hood on brackets, and an iron-and-glass canopy go on the wanted list (`SLICE_MODELS_WANTED.md`), never pieced together from other modules (the kit's rule).
4. Terraces get door hoods when they exist; until then their doors keep the kit's lintel trims alone.

**Check:** a close-up render of each porch kind with its joints checked; no bare beam stands anywhere.

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

## R8 Skyline and roofs

- The Gothic skyline's spires, a church or cathedral beyond the walls or on the Ridge, and a clock face (asset map §9 rows 23–24) remain wanted; R3's haze makes them read as the reference's landmarks.
- Terracotta roofs in parts of the city, as in the reference's roofscape, would change style guide §2 ("no tiled roof") and need a clay-tile texture set on the slate slot (§5 D6).

---

# 4. Order

| Step | What | Why first | Depends on |
|---|---|---|---|
| 1 | R2.1 quality level, R2.6 filtering | one setting, fixes most of remark 4 at once | D5 |
| 2 | R1.1–R1.2 additive interiors and preload | the owner's worst pain | D3 |
| 3 | R3.1–R3.7 colour, sky, haze, light, grade | decides most of the look, needs no rebuild of the city | D2 |
| 4 | R2.2–R2.5, R2.7 LOD, rooms, glass, shadows | needs the houses baked again | step 1 |
| 5 | R4, R5 streets, pavements, passages (C4) | one rebuild of the city | D1 |
| 6 | R6 porches | joins the house bake of step 4 or 5 | measurement |
| 7 | R1.3–R1.4 lighter first load, build measured | after the city's content settles | step 5 |
| 8 | R7, R8 dressing and skyline | content on a settled city | D6, D7 |

Each step ends with its check, the narrowest test set that covers it, renders for the owner, and a commit.

---

# 5. Decisions for the owner

| # | Question | Options | Recommended |
|---|---|---|---|
| D1 | Wider roads on which area? | (a) C3's area, shallower yards and two fewer lanes; (b) a larger city; and whether a round place at the centre is wanted | (a), without the round place |
| D2 | Colour space (VS-COLOR) | (a) keep Gamma; (b) Linear, every material and light checked again | (b) |
| D3 | How doors load | (a) additive interiors with the city kept loaded, separate scenes as now; (b) seamless interiors in the city scene | (a); (b) only if (a) misses 3 s |
| D4 | Rooms behind windows | (a) built rooms near, matching mapped rooms far (R2.3–R2.4); (b) mapped rooms only, cheaper to load | (a) |
| D5 | The quality level the editor and game use | High for both | High |
| D6 | Tiled roofs | (a) slate only, as now; (b) terracotta in parts of the city | owner's taste |
| D7 | Trees and greenery | (a) the present packs, retinted; (b) a more realistic tree pack | (a) first |

---

# 6. What stays

- The city plan C3's places, axes and districts, the seven buildings, the POIs and the Ritual Site Candidates.
- The kit's rules: modules stay separate, no merged mesh, no atlas, no new mesh or material slot; a missing module is named and waited for.
- The mystic layer of §24.5, retuned in R3's light.
- Interiors as separate scenes (with D3 (a)).
