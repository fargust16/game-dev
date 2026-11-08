# Victorian Streets — Showcase Plan

Version: 0.4

Status: Draft — the owner's brief of 2026-11-08 (in chat), formalised by the agent before implementation; the streets accepted by the owner the same day

Created: 2026-11-08

Last Updated: 2026-11-08 (§4.4: the rooms switch by distance instead of an LOD group, and attic rooms stand under the mansard's flat only (VEILMOUTH_REFERENCE_PLAN R2 steps 2–3). Earlier: §4.4: rooms behind every window, from Victorian Interiors, and clear glass, at the owner's request after accepting the streets. Earlier: after the first renders: tints in linear space, a linear mortar mask, panes drawn from both sides, light set for the project's Gamma colour space, a night sky)

Authority: the owner's brief of 2026-11-08; the kit's rules `victorian_kit/СБОРКА.md` (§ Правила first) and the package README (`Packages/com.victorian.kit`, a local `file:` package)

Related: [SLICE_MODULAR_KIT_STANDARD.md](SLICE_MODULAR_KIT_STANDARD.md) (kit wrappers in the slice; this showcase is not the slice)

---

## 1. The task and its limits

Two show streets in their own scene, `Assets/Scenes/VictorianKit/VictorianStreets.unity`, assembled from the owner's Victorian Kit. A rich street first, then a poor one, then light on both, and then a stop: the city is not expanded until these two streets read.

Nothing new is added to the kit: no new meshes, no new material slots, no houses from other packs. Each house stays a set of module prefab instances. It is never merged into one mesh or baked into a unique atlas.

A corner that cannot close face to face is reported with the missing module's name, and the work on it stops; no hole is covered by another slope.

## 2. The kit as Unity imports it (measured 2026-11-08)

Measured by `VicKitProbe` (`Logs/VictorianKit/modules.tsv`):

- every module prefab's root is turned 270° about X and scaled 100;
- the mesh holds the Blender point (x, y, z) as (−x, y, z) / 100;
- so the prefab shows a Blender point as (−x, z, −y): the importer flips handedness.

A kit point keeps its Blender look in Unity only as **position (x, z, y)**. A turn θ about Blender Z is a turn −θ about Unity Y, and the module's own import turn must be kept. The assembler uses `localRotation = Euler(0, 180° − yaw, 0) × importRotation`.

**This differs from the brief.** The brief and the README give the conversion as position (x, z, −y) with `Quaternion.Euler(0, yaw, 0)`. The package's **Victorian Kit / Build Prefabs** follows that text and sets the turn to `Euler(0, yaw, 0)`, which drops the import turn. Its house prefabs therefore lie flat with every module mirrored (render `probe_u_*` of 2026-11-08). The package is the owner's; the agent does not edit it.

The suggested fix to `VictorianKitBuildPrefabs.cs` is:

- position `new Vector3(x, z, y)`;
- rotation `Quaternion.Euler(0, 180 − yaw, 0) * modulePrefab.transform.localRotation`;
- scale `modulePrefab.transform.localScale`.

## 3. The assembler

Code: `Assets/Scripts/VictorianKit/Editor/` (assembly `GameDev.VictorianKit.Editor`). It ports the kit's own placement rules from `victorian_kit/*.py`.

- **Plan.** `VicPlan` is a footprint of 2 × 2 m cells. It traces one outline counter-clockwise, with no holes, and refuses a wing under 4 m. On every 2 m edge the facade faces local −Y and the house lies on local +Y.
- **Shell.** `VicRoofs.Shell` builds each storey round that outline:
  - `Corner_Out` on street corners, with `Quoin_Out` where the facade asks;
  - `Reentry` / `Reentry_Y` in courts, with their own `Corner_In[_Y]` bands;
  - 2 m bays keyed per storey, using wall, window, door, bay, oriel and plain-metre keys;
  - foundation, string course, cornice and frieze bands by storey.
- **Mansard.** `VicRoofs.Mansard` follows § Правила: one spring for the whole outline, a 2 m inset and 42° slopes.
  - **Corners and ring.** `MansardCorner` goes on street corners and `Valley_D4` on court corners. `Mansard_Deck` fills the ring between them.
  - **Flat top.** `Flat_Deck` covers every cell whose eight neighbours are inside the outline. Where a 4 m wing leaves no flat, `RidgeCap_2m` closes it. The `MansardEdge` parapet runs round the flat top.
  - **Eaves.** They run round the outline. A court corner takes `Eave_Corner` and `Gutter_Corner` but no `Soffit_42_Corner`.
  - **Chimneys and vanes.** `Chimney_4` stands only on a flat tile, whose centre is 1.0 m from the parapet, which meets the 0.90 m rule. The vane also stands on a flat tile.
  - **Oriel and lucarnes.** The oriel cap replaces three deck segments. A lucarne sits 0.40 m up a mansard slope, as the kit's proof house `_pi` sets it, or 0.05 m up an oriel cap, as `_court` sets it.
- **Hip and gable.** `VicRoofs.Hip` and `VicRoofs.Gable` cover a W × D rectangle with D 4 or 6 m, following `proof_houses.hip_roof` and `gable_roof`.

**Check.** `VicHousePlans.HouseU()` builds the kit's reference house with these rules. It must equal `House_U_Modular.json` piece for piece. On 2026-11-08 both had 709 pieces, none missing and none extra (`Logs/VictorianKit/house_u_compare.txt`). The EditMode tests keep it so.

## 4. Materials

### 4.1 One tiling set per slot

Each of the seven slots has one tiling PBR set, made by `VicTextures` in C# and written to `Assets/Art/VictorianKit/Textures`. The set has three maps:

- **base** (sRGB);
- **normal**;
- **mask**: R roughness, G the slot's second-tint mask, B cavity.

The kit's UVs are metres from each module's pivot (measured: a wall runs u 0..2, v 0..3; a slope's v runs up the slope from the wall line). Tiling is therefore 1 / rapport. The second-tint mask carries mortar, stucco scoring, bare wood, rust or glass grime, so one texture set takes many tints.

| Slot | Rapport (u × v) | Pixels | Pattern |
|---|---|---|---|
| Brick | 2.0 × 1.5 m | 2048 × 1536 (1024 px/m) | Stretcher bond, brick 0.25 m with a half-bond of 0.125 m, courses 0.075 m: 40 courses a storey, 20 in the rapport |
| Stucco | 2.0 × 2.0 m | 2048² | Render scored as ashlar, 1.0 × 0.5 m blocks, half-bond |
| Stone | 2.0 × 2.0 m | 2048² | Fine-grained limestone, pitting, no joints (the meshes make the blocks) |
| Slate | 2.0 × 2.0 m | 2048² | U along the eave, V up the slope: slates 0.25 m wide with a 0.20 m exposure and alternate rows half-bonded |
| Wood | 2.0 × 2.0 m | 2048² | Painted boards with grain along U; G gives bare-wood chips |
| Iron | 2.0 × 2.0 m | 2048² | Cast-iron pitting; G gives rust |
| Glass | 2.0 × 2.0 m | 2048² | Faint ripple and smudges; G gives grime |

**Why the brick joints stay whole at module seams.** A wall's u starts at 0 at every 2 m pivot. A corner's legs are 1 m, and a mirrored leg runs u backwards. The joint pattern repeats every 0.25 m across and every 0.15 m up, so it divides 1 m and 3 m and is symmetric under u → −u. Every pivot lies on a whole metre, and every storey starts 0.6 + 3k m up. So the joints meet at every 2 m seam, at every 1 m corner leg and at every storey. `VicSeamCheck` measures this on the built House U from mesh UVs, for brick-slot faces, and a render checks it by eye.

Every set is 1024 px/m (2048 px over 2 m), above the brief's street range of 256–512 px/m and inside its close-up range.

### 4.2 Two shaders

- **`GameDev/VictorianKit/Facade`** is opaque and SRP Batcher compatible. All its properties sit in `UnityPerMaterial`, and it uses no `MaterialPropertyBlock`. It has:
  - the base map times a tint, with a second tint through the mask;
  - roughness scale and the second tint's roughness;
  - metallic;
  - dirt from world height above the 0.60 m plinth, broken up by large world-space noise;
  - moss on up-facing surfaces and near the ground.

  The dirt and moss amounts are set per material and are stronger on poor variants.

  **Tints are worked out in linear space.** The shader multiplies the linear texture by the linear tint. So a variant's look, given in sRGB, is turned linear, divided by the set's linear mean (brick 0.74, mortar 0.88, stucco 0.86, stone 0.74, slate 0.70, wood 0.85, iron 0.8), and stored back as sRGB. The first build divided in sRGB, and every variant came out too light.

  **The second-tint mask is read linearly** (sharpness 1) on brick, stucco and the ground, so a distant mip level mixes brick and mortar by area. A sharper threshold painted whole distant walls in mortar colour (render `seam_u_front` of the first build).
- **`GameDev/VictorianKit/Window`** is a glass face. It has:
  - near the eye, clear glass: premultiplied transparency (`Blend One OneMinusSrcAlpha`, no depth write) that lets through `_Transmit` of the light (0.85 less the grime: 0.77 rich, 0.50 poor), so the real room of §4.4 shows behind it;
  - far from the eye, a room behind the pane by interior mapping in the pane's tangent space, which takes over between `_RoomNear` 32 m and `_RoomFar` 46 m, inside the rooms' cull distance of 48 m;
  - the reflection probe through a Fresnel term;
  - grime over both;
  - lit rooms after dark, from the global `_VicNight`: near, a warm glow behind the glass; far, the mapped room lit.

  Each window's curtains, blind and lamp come from a hash of the window's own anchor (world position minus u·T + v·B), so variety survives static batching. Curtains stand at the pane's edges near the eye and close further at a distance.

  **The pane is drawn from both sides.** The kit's upper sash pane faces into the house, and with back faces culled the street looked through it into the hollow house (render `seam_u_2m` of the first build). The shader takes T and B from the mesh's UV tangents whichever way the face points, then turns only the normal toward the viewer. No mesh changes.

### 4.3 Variants

There are nineteen kit variants and two ground materials in `Assets/Art/VictorianKit/Materials`; tint and roughness live in each asset:

- **Brick:** Rich (warm), Common (red), Poor (dark, rough), Stock (yellow-grey).
- **Stucco:** Cream, White (grey-white).
- **Stone:** Light, Medium, Dirty.
- **Slate:** Even, Common, Rough.
- **Wood:** Rich (dark painted sash), Brown, Green (dull, chipped), Oxblood (dull, chipped).
- **Iron:** one.
- **Glass:** Rich, Poor.
- **Ground:** Pave (the stucco set's scoring as 1.1 × 0.55 m flagstones) and Setts (the brick set as 0.20 × 0.12 m granite setts), both with world-space UVs on Unity's built-in plane. They are street ground, not kit slots, and add no mesh.

A house's **palette** maps each kit slot to one variant. The first three are:

| Palette | Brick or stucco | Stone | Slate | Wood | Iron | Glass |
|---|---|---|---|---|---|---|
| Rich | Brick Rich, or Stucco Cream / White | Light | Even | Rich | Iron | Rich |
| Common | Brick Common | Medium | Common | Brown | Iron | Rich |
| Poor | Brick Poor | Dirty | Rough | Green or Oxblood | Iron | Poor |

The poor street also uses Brick Stock and Wood Oxblood, so that neighbours differ. Stucco goes only on the rich street, on wall modules' brick slot, as the kit's proof houses apply it.

No trim sheet in this pass. Signs, ivy and broken panes are later decals or small meshes and are not in this pass.

### 4.4 Rooms behind the windows

The owner, accepting the streets (2026-11-08): a look through a window must find floors, walls, furniture and stair flights, not an empty box. Every house therefore holds simple rooms of Victorian Interiors, built by `VicRooms` from the house's own plan and openings. They are **dressing**: nobody enters them, every collider and light of the pack's blocks is off, and the seven interiors and the Dwellings stay separate scenes ([VICTORIAN_INTERIORS_PLAN.md](VICTORIAN_INTERIORS_PLAN.md)).

- **Rooms.** The plan is cut into rectangles row by row. A hall one cell wide runs back from the street door where the rectangle is at least 4 cells deep and 3 wide; deeper rectangles split into front and back bands, and each band into rooms of about 5 m. Every storey has the same cut, and lucarnes open an attic under the mansard: its rooms stand under the flat deck only, read from the house's flat tiles, with the knee walls 0.4 m inside its edge, so nothing of them stands out through a slope (2026-11-08). A room is a front room when any part of its street side looks onto the street.
- **Kinds by wealth** (authoring defaults): rich houses have a dining room, study and parlour at the front of the ground floor, kitchen and scullery behind, a drawing room and bedrooms above, a bathroom on each upper storey and a servant's room in the attic; common houses have a parlour and dining room, a kitchen and scullery, bedrooms; poor houses have living rooms, a washroom, bedrooms. Halls, kitchens and living rooms are dark-panelled, a rich study panelled, the rest papered.
- **Shell.** Floors and ceilings are the pack's 3 m tiles stretched to the room. Linings stand on the inside of the outer walls with gaps for the windows and doors; partitions stand on the cell edges. Each room has **one doorway**, to the hall or landing where it touches it and otherwise to the neighbour nearer it, by the longest shared wall, with the doorway by the wall's corner, as Victorian rooms have it, so the other walls stay free for furniture (rules G2). The stair rises along the hall's wall, or along the back wall in a shallow house, with balustrades round the well on every upper storey.
- **Furniture** by kind follows [INTERIOR_FURNISHING_RULES.md](INTERIOR_FURNISHING_RULES.md) in short: an anchor (a fireplace, bed, sideboard, bookcases, bath) centred on the longest plain wall's clear stretch, or failing that on any other wall; its companions round it; nothing taller than the sill before a window; wall lamps on the plain walls. A piece taller than the room shrinks to fit under its ceiling. The pack's one bed is 3.1 m long with its steps and its fireplace 2.9 m wide, so a small room with doors and windows on every wall stays bare or keeps only a dresser; no smaller piece is made up (G10).
- **Cost.** The rooms take light probes, cast no shadows, use no reflection probe and are static. They are drawn only near: `VicRoomsSwitch` turns them on within 48 m of the nearest point of their box and off beyond 53 m, whatever the quality level, behind glass that is opaque from 46 m (VEILMOUTH_REFERENCE_PLAN R2 step 3). The show streets keep the older LOD group by screen size until they are built again.
- **Measured, 2026-11-08** (`Logs/VictorianKit/rooms.txt`, a line per room with its kind, openings, what stands and what did not fit): of 125 rooms seen through a window on the two streets, 103 are furnished with 179 pieces; the 22 bare ones are attic rooms under 1.5 m wide and the smallest rooms of the poor houses.

In Veilmouth the kit stands at ×1.25 and the furniture keeps its own size, so the same rooms are a quarter larger round the same pieces.

## 5. The rich street

Three storeys each, a mansard over the whole outline, quoins on every street corner, string courses and the cornice. Gaps of 4 m on the 2 m grid. The street facade is the plan's Y = 0, facing Unity −Z.

| House | Plan (m) | Palette | Features |
|---|---|---|---|
| R1 Court House | U 20 × 12, wings 6 m, a court 8 × 4 open to the street | Rich, Stucco Cream | Porch with columns in the court's middle bay; ground-floor bay windows on both wing fronts; stone balconies above them; double windows on the first floor; arched top windows; lucarnes over the wings and over the porch; two `Chimney_4` on the wings' flat |
| R2 Oriel House | L: 18 × 8 with a 6 × 6 rear wing on the right | Rich, Brick Rich | A full-height oriel over bays 3–4 with its cap; an arched lucarne on the cap; arched double door with stone rails and a stone balcony above; arched top windows; rect lucarnes at 3 m and 15 m; chimneys on the flat |
| R3 Pavilion House | 16 × 10 with an 8 × 2 front pavilion (risalit) | Rich, Stucco White | Porch with columns at the pavilion's centre and a stone balcony over it; bay windows on the side bays; arched top windows; a wide flat top with two `Chimney_4` and the weather vane |

The 6 m wings of R1 and R2 each leave a `Flat_Deck` strip, which the rule requires. No plan is H or X, and none uses a plinth.

## 6. The poor street

Each house is one rectangle under one roof of its own height. Fronts are 6, 8, 10 or 12 m and depths 4 or 6 m. Houses have 2 or 3 storeys and gable or hip roofs. Gaps are 2 m. There is no stucco, and only two houses carry quoins, on their street corners. Windows are rectangular with an occasional arch.

| House | Front × depth | Storeys | Roof | Ridge (m) | Door bay | Palette |
|---|---|---|---|---|---|---|
| P1 | 8 × 4 | 2 | gable D4 | 8.85 | 0 | Poor, Wood Green |
| P2 | 10 × 6 | 3 | hip D6 | 12.75 | 2 (middle) | Common, quoins |
| P3 | 6 × 4 | 2 | hip D4 | 8.85 | 1 | Poor, Brick Stock, Wood Oxblood |
| P4 | 12 × 6 | 2 | gable D6 | 9.75 | 0 and 4 (two doors), an arched window | Common |
| P5 | 8 × 6 | 3 | gable D6 | 12.75 | 2 (last) | Poor, Wood Oxblood |
| P6 | 10 × 4 | 3 | gable D4 | 11.85 | 1 (arched) | Common, Brick Stock, quoins |
| P7 | 6 × 6 | 2 | hip D6 (pyramid) | 9.75 | 0 | Poor, Wood Green |
| P8 | 12 × 4 | 3 | hip D4 | 11.85 | 3 | Common, Wood Oxblood |

No two neighbours share a ridge height, a plan or a palette, and no plan repeats, so neither the repeat rule nor the mirroring rule is touched. Chimneys follow the kit's rules: on a gable 0.85 m up the slope and off the facade's middle; on a hip in the middle of the front slope, 0.55 m (D4) or 1.0 m (D6) up. None stands on a ridge, and `Chimney_1` is never used.

## 7. Light

- **Static.** Both streets are marked Static. Walls, corners, frieze and roof decks get lightmaps. Small pieces take light probes and leave the lightmap: trims, inserts, rails, pipes, pots and caps.
- **Baked Indirect.** The sun is a Mixed light, so direct light and shadows stay realtime and the bounce light is baked. Each street has its own light probe group and its own baked box-projected reflection probe. The bake (`VIC_StreetsLighting`) uses 8 texels per metre, 512 indirect samples and a Gaussian filter. With 128 samples, green and pink noise patches showed on pale stucco.
- **Day and night.** `VicStreetLighting` sets the sun, the ambient, the fog, the sky (a day and a night procedural sky, `VIC_SkyDay` and `VIC_SkyNight`) and `_VicNight`. At night the facade shader damps the baked bounce light and the windows light their rooms.
- **Gamma colour space (measured 2026-11-08).** The project renders in Gamma colour space (`ProjectSettings.asset`, `m_ActiveColorSpace: 0`). Light therefore multiplies gamma albedo directly, and the first build's sun of 2.2 washed every facade out. The showcase's light is set for Gamma: a sun of 1.05 and an ambient of about 0.4, with Neutral tone mapping. These are authoring defaults. The material tints hold in either space, because G(L(t) / m) ≈ t / G(m) for the sRGB curve. Moving the project to Linear would suit PBR better, but it changes the slice as well, so it is the owner's call; the showcase's light would then need retuning.
- **No vertex AO.** It is not needed: the kit's meshes and the mask's cavity carry the contact shading.
- **Post.** One global volume (`VIC_StreetsVolume`) with Neutral tone mapping, light bloom, contrast 6 and no exposure offset. The street build rewrites its values each time.

## 8. Checks

- **House U.** It equals its manifest piece for piece (EditMode test).
- **Brick seams.** On House U every brick-slot vertex shared by two modules agrees on the joint phase: u mod 0.25 (or −u), and v against world height mod 0.15 (EditMode test and close-up render).
- **Street rules (EditMode test):**
  - rich houses differ in plan kind;
  - no two poor neighbours share a plan, a ridge height or a palette;
  - every gap is at least 0.6 m and every house sits on the 2 m grid;
  - every chimney stands on a flat tile or on a slope off the ridge.
- **Windows and slopes.** No window sticks out of a slope, and no slope covers an upper window. Every lucarne sits by the proof houses' offsets, and no wing meets a taller wall, so § Правила's low-wing joint never arises. Renders check it.
- **Rooms.** `Logs/VictorianKit/rooms.txt` (§4.4), renders `rooms_*.png` through windows by day and night, and plan sections `plan_rich_*.png`, `plan_poor_*.png` cut 2.2 m above each floor.
- **Renders.** `Logs/VictorianKit/street_*.png` and `seam_*.png`.

Done when:

- the rich houses are complex and differ in plan;
- the poor houses are not copies;
- the brickwork is unbroken at 2 m joints;
- no window sticks out of a slope and no slope covers an upper window.

## 9. Out of scope

- Expanding the city past these two streets.
- Trim sheets, decals for signs, ivy and broken panes.
- Interiors one enters: they stay separate scenes; the rooms of §4.4 are seen only through the glass.
- Changes to the owner's package, including the Build Prefabs fix of §2: that is the owner's call.
