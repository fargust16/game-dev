# Current World Audit — Veilmouth before the island overhaul

Version: 0.1

Status: Draft

Last Updated: 2026-11-13 (first version: the state of the slice's world before any terrain change)

Depends On:
[VEILMOUTH_CITY_PLAN.md](../../Assets/Plans/VEILMOUTH_CITY_PLAN.md), [VERTICAL_SLICE_IMPLEMENTATION_PLAN.md](../../Assets/Plans/VERTICAL_SLICE_IMPLEMENTATION_PLAN.md), [AGENTS.md](../../../AGENTS.md)

Referenced By:
[MICROSPLAT_STAMPIT_RESEARCH.md](MICROSPLAT_STAMPIT_RESEARCH.md)

---

# Purpose

This is the record of the world as it stands on 2026-11-13, commit `0e50c388`, taken before the terrain and environment overhaul. Nothing was changed to write it.

Each fact is marked by how it was read:

- **code**: read from the builder's source, which writes the scene deterministically;
- **file**: read from an asset or scene file on disk;
- **plan**: read from the city plan, and matching the code where checked;
- **not read**: the value lives in a binary asset. It is dumped by a Unity batch run as the first step of the implementation, because the owner's editor was open during the audit (`Temp/UnityLockfile`), and two editors cannot open one project.

---

# 1. Which world this is

The project holds two separate worlds.

| World | Scene | Terrain | Status |
|---|---|---|---|
| The Vertical Slice: Veilmouth | `Assets/Scenes/Slice/Veilmouth.unity` | one hand-authored Unity Terrain, 1000 × 1000 m | the game's active target; S5 accepted |
| Overworld V0 | `Assets/Scenes/Overworld_Generator.unity` | 2 × 2 tiles of 1400 m, baked by TerrainGen | shipped; under the standing stops of `AGENTS.md` |

The request describes an existing Victorian city on a map of fixed size, so this audit reads it as **Veilmouth**. The Overworld is not touched, and its rules stay as they are. This reading is the first question for the owner (research document, §8).

---

# 2. Engine and render state

| Item | Value | Source |
|---|---|---|
| Unity | 6000.6.8f1 (the owner's upgrade from 6000.5.7f1, not committed) | file: `ProjectSettings/ProjectVersion.txt` |
| Render pipeline | URP 17.6.0 | file: `Packages/manifest.json` |
| Colour space | Linear (owner, 2026-11-08) | `AGENTS.md` |
| Rendering path | Forward+ (`m_RenderingMode: 2`), depth priming Auto | file: `Assets/URP_Renderer.asset` |
| Renderer features | `HeightFog` (the project's own), `ScreenSpaceAmbientOcclusion` | file: `Assets/URP_Renderer.asset` |
| HDR, MSAA | HDR on, MSAA 4× | file: `Assets/URP_Pipeline.asset` |
| Depth and opaque textures | both on (the water shader reads them) | file |
| Main light shadows | 4096 px, 4 cascades, 150 m, soft, quality 3 | file |
| Additional lights | per pixel, 4 per object, no shadows; light cookies on; rendering layers off | file |
| SRP Batcher | on | file |
| GPU Resident Drawer | mode 1 with GPU occlusion culling in the working copy (the owner's uncommitted change; the agent had left it off, city plan §29.7) | file |
| Terrain holes | supported | file |
| Light probes | legacy Light Probe Groups (`m_LightProbeSystem: 0`), no APV | file |

**What this means for the terrain's shader.** The carriageways are terrain surfaces, lit at night by the gas lamps, which are additional lights. A terrain shader must therefore take additional lights under Forward+, write depth and normals for SSAO and depth priming, and receive four shadow cascades. A shader that fails any of these changes the city's streets, not only the land outside.

---

# 3. The terrain

## 3.1 Object and data

| Item | Value | Source |
|---|---|---|
| Terrains in the scene | one, the GameObject `Ground` on the kit's `Ground` layer, static | code: `VeilmouthScene.CreateTerrain`; tests take `All<Terrain>().Single()` |
| Data asset | `Assets/Data/Slice/Veilmouth_Terrain.asset`, 13.4 MB, binary | file |
| Size | 1000 × 100 × 1000 m (X, height budget, Z) | code: `VeilmouthScene.Extent`, `HeightBudget` |
| World position | (−500, 0, −500); the scene's origin is the area's centre, north is +Z | code; `VeilmouthSceneTests` |
| World extent | X −500…500, Z −500…500, Y 0…100 | derived |
| Heightmap | 1025 × 1025, one sample every 0.977 m | code: `VeilmouthGround.HeightmapResolution` |
| Weights (alphamap) | 1024 × 1024, 0.977 m a texel, repainted by the city builder | code: `VeilmouthCity.AlphamapResolution` |
| Material | `Assets/Settings/OverworldTerrain.mat`, URP `Terrain/Lit` | file: the scene's `m_MaterialTemplate`; the material's shader GUID |
| Draw instanced | off | file: scene |
| Pixel error | 5 | file: scene |
| Shadow casting | two-sided | file: scene |
| Detail distance, density | 80 m, 1 | file: scene |
| Tree distance | 5000 m | file: scene |
| Terrain trees, detail (grass) prototypes | none expected: no code adds any, the forest is kit GameObjects, and the grass detail layer of S3b.0 was never built | code; **not read** in the asset |
| Collider | a `TerrainCollider` on the same object | file: scene |

## 3.2 Coordinates

The plan, the layout file and the builder use plan metres: `px` runs east and `py` runs south from the area's north-west corner. World X = px − 500, world Z = 500 − py.

## 3.3 Heights

All are authoring defaults in `VeilmouthGround` and `VeilmouthScene`.

| Surface | Height, m | Note |
|---|---|---|
| Sea Level | 24.0 | one water plane for the sea and the river; the Combat Grid reads every surface below it as water |
| The built ground (city and works) | 25.9 | level; `VeilmouthScene.TerrainLevel` |
| A street tile's top | 26.0 | kit standard §2 |
| The rise under the Penny and the cathedral | up to +1.5 | full within 30 m of the line between them, gone at 120 m |
| River bed | 21.0 | 3 m of water |
| Sea bed | 19.5 at the quay, falling to 17.0 over 200 m | |
| Land outside the walls | 26.2 to about 37 | rolls by up to 5 m (three octaves of Perlin noise over 120 m), rises 6 m inland, never below 26.2 |
| The north, west and south edges | +45 within 45 m of the boundary | steeper than 45°, so no walker climbs out; a pass for the West Road |
| Sand | below 25.2 | paint rule |

The relief outside the walls is one noise field plus a ramp. It has no named landforms: no hill, valley, headland or bay.

## 3.4 Layers

Eight layers, the most URP's `Terrain/Lit` draws in the project's setup (`VeilmouthCityTests.TheStreetsAreAppendedWithinTheTerrainLayerBudget` asserts ≤ 8).

| # | Layer asset | Tile, m | Maps |
|---|---|---|---|
| 0 | `Overworld_Grass` | 11 | colour, normal |
| 1 | `Overworld_Sand` | 14 | colour, normal |
| 2 | `Overworld_Soil` | 12 | colour, normal |
| 3 | `Overworld_Cliff` | 12 | colour, normal |
| 4 | `Overworld_Rock` | 12 | colour, normal |
| 5 | `Slice_Setts` (carriageways) | 2 | colour, normal; smoothness 1 from the layer |
| 6 | `Slice_Flags` | 2 | colour, normal; no longer painted since the raised pavements (R4) |
| 7 | `Slice_Cobbles` (lanes, yards, squares) | 4 | colour, normal |

- No layer has a mask map, a height map, an occlusion map or a roughness map.
- Layers 0–4 are the Overworld's five canonical `TerrainLayer` assets. Veilmouth reads them and never changes them, and the Overworld's terrain uses the same assets. A change to their look for the slice would change the Overworld too.
- Tiles of 11–14 m on natural layers are large: at eye height the texture reads soft and its repeat shows on open ground.

## 3.5 How the weights are painted

`VeilmouthGround.Paint` writes the five canonical layers by rule, with one layer or a fixed pair at each texel and no blend band between rules:

- soil on built ground, which the city then paints over;
- cliff or rock where the slope is over 40°;
- rock with a fifth of grass within 16 m of the open coast;
- sand below 25.2 m;
- soil on the West Road;
- rock over 50 m;
- grass with 25–50 % of soil inside the forest's rectangles;
- grass elsewhere.

`VeilmouthCity.PaintStreets` then sets the alphamap's resolution to 1024, paints the canonical five again, and writes each street's quad as 100 % of its layer, carriageways last.

---

# 4. The city and what stands on the ground

## 4.1 Where it is

| Place | Plan (px, py) | World (X, Z) | Size, m |
|---|---|---|---|
| The city inside its walls | 420–680 × 260–550 | −80…180 × −50…240 | 260 × 290 |
| The works (the Reek), over the river | 420–680 × 586–702 | −80…180 × −202…−86 | 260 × 116 |
| The Penny's centre, Stone Grandad | 540, 440 | 40, 60 | |
| The cathedral's west front | 540, 300 | 40, 200 | |
| The player's start, Powder Embankment | 620, 541 | 120, −41 | |
| Town wall, west | 418–420 × 258–550 | −82…−80 × −50…242 | 6 m high |
| Creaking Gate, the one way in from the land | 418 × 431–449 | −82 × 51…69 | |
| Town wall, north (no gate) | 418–680 × 258–260 | −82…180 × 240…242 | 6 m high |
| Works walls, west and south | 418–420 × 586–704; 418–680 × 702–704 | | 4 m high |
| Last Farthing Bridge (the West Road) | 378–418 × 433–447 | −122…−82 × 53…67 | |
| Grubby Bridge (to the works) | 532–548 × 550–588 | 32…48 × −88…−50 | |
| The city's quay wall | px 680, py 250–710 | X 180, Z −210…250 | |
| The open coast | px 700 | X 200 | |

The city stands at the **east** side of the land, on the sea. Its extent is taken from the walls in the layout, not from the scene's bounds, so it is exact.

## 4.2 Water as it is

| Water | Plan | World | Note |
|---|---|---|---|
| River Veil, west reach | 380–416 × 0–586 | −120…−84 × −86…500 | 36 m wide, dead straight for 586 m, running off the terrain's north edge |
| River Veil, south reach | 380–700 × 550–586 | −120…200 × −86…−50 | 36 m wide, straight, between the city's and the works' embankments |
| The sea | px ≥ 680 for py 250–710; px ≥ 700 elsewhere | X ≥ 180 or 200 | 300 m of sea to the terrain's east edge |

- **The water object** is one Unity plane named `Water`, 1000 × 1000 m at Y = 24, on the kit's `NoGrid` layer, with no collider. Wherever the ground is dug below 24 m, water shows.
- **Its material** is `Assets/Settings/Water/OverworldWater.mat` on WaterWorks' `SSR_Water` shader graph: transparent, with depth-based edge colour, foam, refraction and screen-space reflections. It reads the camera's depth and opaque textures.
- **Swimming** reads the water's level from the object's height and its plan from its renderer's bounds (`SwimWater`). The Combat Grid reads water by height alone.
- **The river already rounds the city** on the west and the south and meets the sea on the east. The north is closed by the wall, with no water.
- **The land north of the city** lies on the city's bank of the river, cut off from the city by the wall and from the West Road by the river. It is reached only by swimming.
- **The river's mouth** lies between two stone quays, so it cannot widen there.
- **Beyond the terrain's edges nothing exists.** The water plane ends with the terrain.

## 4.3 Outside the walls

| Thing | Where | Count | Source |
|---|---|---|---|
| The West Road | 0–378 × 434–446, to the terrain's west edge through a pass | 1 | layout |
| Crooked Hamlet | fronts on py 428 and 452, px 256–350 | 8 cottages, 6 farm yards | layout |
| Gloomwood, the forest | five rectangles covering nearly all land outside the walls, from 20 m inside the edges | 4013 cover trees as kit GameObjects | layout; plan §28.9 |
| Glades | (120,120) (300,100) (500,120) (640,150) (150,300) (260,560) (120,800) (520,860) | 8, with 40 fellable trees and 30 herbs | code |
| Wolf territories, 25 m round each den | (200,150) (420,80) (600,90) (90,330) (200,700) (450,900) (320,200) (520,200) (300,640) (90,900) | 10 | code |
| Boar territories | (330,300) (100,600) (300,880) (620,800) (240,260) (480,760) (330,760) | 7 | code |
| Iron veins | 36–46 m inside the north, west and south edges, at the foot of the rising ground | 12 | code |

Three territories and one glade lie within 120 m of the west edge, and the veins are defined by the edge that the island removes.

## 4.4 What depends on the ground's height

The scene is written by code. `VeilmouthCity.RebuildStreets` builds the whole city again on the ground as it stands, and then runs the passes beside it: people and their routes, the world driver, dogs, ornaments, the mystic layer, reflection probes, the POIs, the far LODs and the views. So an object's height is not stored by hand: it is read from the terrain at build time.

| Dependent | How it reads the ground | Consequence of a change |
|---|---|---|
| Every house, wall, tower, lamp, tree and prop | `Ground(px, py)`: a deck's top, a raised pavement's top, or `Terrain.SampleHeight` | placed again by the rebuild |
| The raised pavements, 40 mesh chunks | their heights are sampled from the terrain | built again by the rebuild |
| The stone embankments, quay walls, piers and bridge piers | fixed levels from `VeilmouthGround.RiverBed`, `SeaBed` and the built level | the river and sea beds beside built ground must keep their depths, or the masonry floats or is buried |
| The facades' plinth-dirt shader | reads the terrain's heightmap through global shader values | follows the terrain by itself |
| Townsfolk routes | world-space points planned in the editor over walkable ground | planned again by the rebuild; all lie inside the walls |
| Beasts | placed in the territories by `SliceBeastsSetup.Populate`; at run time they test ground above Sea Level + 0.3 m | territories in the sea would hold no beasts |
| Reflection probes | baked EXR files, `Assets/Scenes/Slice/VeilmouthReflections/` | baked again by the rebuild; needs graphics |
| The Combat Grid | reads the place during a fight: colliders, and height against Sea Level | no stored data |
| Tests | `VeilmouthSceneTests` (heights, the edge's slope, the road's pass), `VeilmouthCityTests` (layers ≤ 8, lamps, doors), the slice's PlayMode tests | three assertions encode the present edge and layer count |

**There is no NavMesh.** The scene's `NavMeshSettings` hold no data, and no script uses `NavMesh`. Movement is the character controller, the Combat Grid and planned routes.

---

# 5. What may change and what may not

## 5.1 Must stay exactly as it is

- The ground inside the city and the works, and under every wall's footing with 1 m either side: the area `VeilmouthGround.Built` returns true for. Its level, 25.9 m with the rise, carries about 155 buildings, 26,000 m² of pavement and every street.
- The river's bed and width wherever a stone bank stands: the west reach from py 258 to 586 on its city side, and the whole south reach from px 418 to 700.
- The river's width under both bridges, and the West Road's level at Last Farthing Bridge.
- The sea's bed along the quay wall, the piers and the moored pinnace, py 250–710.
- Sea Level, 24 m.
- The terrain's size, position and heightmap resolution. Any of them moves every sample under the city.

## 5.2 Free to reshape

- All land outside the walls: the west bank, the north and the south.
- The river's west bank along the city, which is natural today.
- The river north of the north wall (py < 258), which may bend, narrow and end.
- The open coast north of py 250 and south of py 710.
- The three land edges, whose cliffs exist only to stop the walker.
- The sea bed away from the quays.

## 5.3 Moves with the land

Authoring defaults of the agent, so they may move, and tests follow them: the forest's rectangles, the glades, the beast territories, the iron veins, the hamlet's dressing and the West Road's far end.

---

# 6. Light and atmosphere

| Item | Value | Source |
|---|---|---|
| Sun and moon | one directional light, driven by the game clock; the sun at most 42° high, intensity 2 (Linear); a crimson moon by night, 0.15 | code: `VeilmouthSky` |
| Sky | painted skies (Free HDR Sky) blended by the hour on the project's `GameDev/Slice/Sky` shader; no skybox in `RenderSettings` | code; file |
| Ambient | three-colour gradient by the hour | code |
| Distance haze | exponential fog, density 0.0012 by day (about 30 % at 300 m), 0.0022 by night, in the haze's colour | code; file: scene |
| Height fog | the `HeightFog` renderer feature: densest at Sea Level, thinning by e every 5 m | code |
| Grade | HDR, Neutral tone mapper, saturation down by about a seventh by day | `WORLD_STYLE_GUIDE.md` §5 |
| Cameras' far planes | 1500 m in the render and probe tools; the player's camera **not read** | code |

The haze matters to the island: at 500 m it hides about 45 % of the far shore by day, so the horizon reads as sea into haze, and the terrain's far edge needs no sharp treatment.

---

# 7. Frame cost before the change

Measured by `FrameProbe` in a development build at 1920 × 1080, High, on the owner's machine (city plan §39, 2026-11-11). The owner's budget is 16.7 ms for each view's median frame.

| View | Median frame, ms |
|---|---|
| down Long Tongue | 10.21 |
| down Salt Gullet (the worst) | 11.92 |
| across the Penny | 6.82 |
| down Thimble Street | 10.06 |
| Draught Lane | 10.01 |
| the West Road | 8.47 |
| in Gloomwood | 5.17 |
| pooled | 9.61 |

- The GPU works about 4.2 ms of the frame, and the main thread holds it.
- The margin is at least 4.6 ms in every view.
- A difference under about 1.5 ms between two runs on this machine is not a finding (the plan's own reading).

The same probe is run after the overhaul, and the two tables are compared.

---

# 8. Tools the project already has

| Need | Tool | Note |
|---|---|---|
| Shape the ground | `VeilmouthGround.Shape`, `ShapeReplacing` | refuses to run twice by menu; the batch entry replaces |
| Build the city on the ground | `VeilmouthCity.RebuildStreets` | the whole chain; needs graphics, never `-nographics` |
| Views | `VeilmouthRenders` (`Eye`, `Top`, `RenderC5`, `RenderC4Ground`) | writes PNGs under `Logs/SliceSurvey/` |
| Walk defects | `VeilmouthWalkAudit.Run` | slits at the water's edge among them |
| Frame cost | `FrameProbe`, `VeilmouthFrameProbe` | development build |
| Tests | `slice`, `slice-play` (`TEST_RUNS.md`) | one Unity at a time, the owner's editor closed |
| Water | WaterWorks, already the scene's water | no new water asset is needed |

---

# 9. Findings that shape the plan

1. **The ground is authored by code, and the scene is rebuilt by code.** The overhaul belongs in `VeilmouthGround` and the builder's outskirts, followed by one rebuild. Sculpting the saved scene by hand would be lost at the next rebuild.
2. **The carriageways are terrain.** The new terrain material draws the city's streets as well as the land, so it is judged first on the streets at night.
3. **The land outside has no landforms** and is fenced by a cliff. That is where the visual gain is.
4. **The river already rounds two sides of the city** on fixed stone banks with two bridges. It can be made natural upstream and on its outer bank, but not moved along the city.
5. **The planned land pass, S3b.0, was never built** (99 VS-LAND). The overhaul replaces it and settles its open pieces, among them the mountain backdrop, which an island contradicts.
6. **Layers 0–4 are shared with the Overworld.** The slice needs its own layer assets before any look is changed.
7. **No layer has height, occlusion or roughness.** Height blending and wetness need real PBR sets, which the project does not hold.
8. **The frame has a margin of 4.6 ms**, held by the main thread, not the GPU. A heavier terrain shader spends GPU time, where there is room, but it is measured, not assumed.
